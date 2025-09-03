using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using ImGuiNET;
using SharpGLTF.Schema2;
using RenderMaster.src.ControlPlane;
using RenderMaster.src.Contracts;

namespace RenderMaster;

public class DebugMenu : IUIElement
{
    public string FpsString { get; set; } = string.Empty;

    private readonly ICommandBus _commands;
    private readonly IQueryBus _queries;
    private NodeId? _selectedNode;

    // ImGui 1.92+ exposes hierarchy lines via ImGuiTreeNodeFlags_DrawLinesFull (bit 19).
    // Define the constant here for compatibility with older ImGui.NET versions.
    private const ImGuiTreeNodeFlags TreeLineFlag = (ImGuiTreeNodeFlags)(1 << 19);

    // list of loaded glTFs are stored in this list along with their JSON text
    private readonly List<(string path, ModelRoot model, string json)> gltfList = new();
    // list of all glTF files discovered on startup
    private readonly List<string> foundGltfPaths = new();
    private string gltfPath = string.Empty;
    private string gltfLoadMessage = string.Empty;
    private System.Numerics.Vector4 gltfLoadMessageColor = new(1, 1, 1, 1);
    private readonly float[] _plotBuffer = new float[300];

    public DebugMenu(ICommandBus commands, IQueryBus queries)
    {
        _commands = commands;
        _queries = queries;
        findGltfs();
    }

    public void AfterBegin()
    {
        if (ImGui.BeginTabBar("Tabs"))
        {
            if (ImGui.BeginTabItem("Function Timings"))
            {
                ImGui.Text($"RENDERMASTER");
                ImGui.Text($"Current FPS: {FpsString}");

                bool isOddRow = false;

                foreach (var entry in TimingAspect.Timings)
                {
                    double sum = 0;
                    double latestTiming = 0;
                    int count = 0;

                    foreach (var timing in entry.Value.Values)
                    {
                        sum += timing;
                        latestTiming = timing;
                        if (count < _plotBuffer.Length)
                        {
                            _plotBuffer[count] = (float)timing;
                        }
                        count++;
                    }

                    var averageTiming = count > 0 ? sum / count : 0;

                    if (isOddRow)
                    {
                        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Separator));
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Border));
                    }

                    if (ImGui.TreeNode($"Method: {entry.Key}"))
                    {
                        ImGui.Text($"Average Execution Time (last {count}): {averageTiming:F4} ms");
                        ImGui.Text($"Latest Execution Time: {latestTiming:F4} ms");

                        if (count > 0)
                        {
                            ImGui.PlotLines("Timings", ref _plotBuffer[0], count, 0, null, 0.0f, float.MaxValue,
                                new System.Numerics.Vector2(0, 80));
                        }

                        ImGui.TreePop();
                    }

                    ImGui.PopStyleColor();

                    isOddRow = !isOddRow;
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Memory"))
            {
                Process currentProcess = Process.GetCurrentProcess();
                long totalMemoryUsageKB = currentProcess.WorkingSet64 / 1024;
                double totalMemoryUsageGB = totalMemoryUsageKB / 1024.0 / 1024.0;
                long privateMemoryUsageKB = currentProcess.PrivateMemorySize64 / 1024;
                double privateMemoryUsageGB = privateMemoryUsageKB / 1024.0 / 1024.0;

                ImGui.Text($"Total Memory Usage: {totalMemoryUsageKB} KB ({totalMemoryUsageGB:0.##} GB)");
                ImGui.Text($"Private Memory Usage: {privateMemoryUsageKB} KB ({privateMemoryUsageGB:0.##} GB)");

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("glTF loadDebugger"))
            {
                ImGui.Text("Test glTF loading");

                ImGui.InputText("Path", ref gltfPath, 260);
                ImGui.SameLine();
                if (ImGui.Button("Load") && File.Exists(gltfPath))
                {
                    LoadGltf(gltfPath);
                }

                if (ImGui.TreeNode("Discovered glTFs"))
                {
                    for (int i = 0; i < foundGltfPaths.Count; i++)
                    {
                        var path = foundGltfPaths[i];
                        bool openFound = ImGui.TreeNode($"{Path.GetFileName(path)}##found{i}");
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Copy Name##foundName{i}"))
                        {
                            ImGui.SetClipboardText(Path.GetFileName(path));
                        }
                        if (openFound)
                        {
                            ImGui.Text($"Filename: {Path.GetFileName(path)}");
                            ImGui.SameLine();
                            if (ImGui.SmallButton($"Copy##foundNameInner{i}"))
                            {
                                ImGui.SetClipboardText(Path.GetFileName(path));
                            }
                            ImGui.Text($"Full path: {path}");
                            ImGui.SameLine();
                            if (ImGui.SmallButton($"Copy##foundPath{i}"))
                            {
                                ImGui.SetClipboardText(path);
                            }
                            if (ImGui.Button($"Load##foundBtn{i}"))
                            {
                                LoadGltf(path);
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.TreePop();
                }

                if (!string.IsNullOrEmpty(gltfLoadMessage))
                {
                    ImGui.TextColored(gltfLoadMessageColor, gltfLoadMessage);
                }

                for (int i = 0; i < gltfList.Count; i++)
                {
                    var (path, model, json) = gltfList[i];
                    bool open = ImGui.TreeNode($"{System.IO.Path.GetFileName(path)}##{i}");
                    ImGui.SameLine();
                    if (ImGui.SmallButton($"Copy##loadedName{i}"))
                    {
                        ImGui.SetClipboardText(Path.GetFileName(path));
                    }
                    if (open)
                    {
                        ImGui.Text($"Model name: {Path.GetFileName(path)}");
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Copy##modelName{i}"))
                        {
                            ImGui.SetClipboardText(Path.GetFileName(path));
                        }
                        ImGui.Text($"Full path: {path}");
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Copy##loadedPath{i}"))
                        {
                            ImGui.SetClipboardText(path);
                        }

                        ImGui.Text($"Scenes: {model.LogicalScenes.Count()}");
                        ImGui.Text($"Nodes: {model.LogicalNodes.Count()}");
                        ImGui.Text($"Meshes: {model.LogicalMeshes.Count()}");
                        ImGui.Text($"Materials: {model.LogicalMaterials.Count()}");

                        bool jsonOpen = ImGui.TreeNode($"JSON##json{i}");
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Copy##jsonCopy{i}"))
                        {
                            ImGui.SetClipboardText(json);
                        }
                        if (jsonOpen)
                        {
                            ImGui.BeginChild($"jsonChild{i}", new System.Numerics.Vector2(0, 200), ImGuiChildFlags.ResizeY, ImGuiWindowFlags.HorizontalScrollbar);
                            ImGui.PushTextWrapPos();
                            ImGui.TextUnformatted(json);
                            ImGui.PopTextWrapPos();
                            ImGui.EndChild();
                            ImGui.TreePop();
                        }

                        bool nodeGraphOpen = ImGui.TreeNode($"Node Graph##nodeGraph{i}");

                        if (nodeGraphOpen)
                        {
                            //assume we only have one scene for now
                            var scene = model.LogicalScenes.FirstOrDefault();
                            if (scene != null) 
                            {
                                ImGui.Text($"Scene: {scene.Name}");
                                ImGui.SameLine();
                                if (ImGui.SmallButton($"Copy##sceneName{i}"))
                                {
                                    ImGui.SetClipboardText(scene.Name);
                                }
                                foreach (var node in scene.VisualChildren)
                                {
                                    ImGui.Text($"Node: {node.Name}");
                                    ImGui.SameLine();
                                    if (ImGui.SmallButton($"Copy##nodeName{i}_{node.Name}"))
                                    {
                                        ImGui.SetClipboardText(node.Name);
                                    }
                                }  
                            } 

                            ImGui.Separator();

                            var textures = model.LogicalTextures;
                            if (textures.Any())
                            {
                                ImGui.Text($"Textures: {textures.Count()}");
                                foreach (var texture in textures)
                                {
                                    ImGui.Text($"Texture: {texture.Name}");
                                    ImGui.SameLine();
                                    if (ImGui.SmallButton($"Copy##textureName{i}_{texture.Name}"))
                                    {
                                        ImGui.SetClipboardText(texture.Name);
                                    }
                                }
                            }

                            ImGui.Separator();

                            var materials = model.LogicalMaterials;

                            if (materials.Any()) {
                                ImGui.Text($"Materials: {materials.Count()}");
                                foreach (var material in materials)
                                {
                                    ImGui.Text($"Material: {material.Name}");
                                    ImGui.SameLine();
                                    if (ImGui.SmallButton($"Copy##materialName{i}_{material.Name}"))
                                    {
                                        ImGui.SetClipboardText(material.Name);
                                    }
                                    ImGui.Text($"Base Color: {material.FindChannel("BaseColor")?.Color}");
                                }
                            }

                            var buffers = model.LogicalBuffers;
                            if (buffers.Any()) {
                                ImGui.Separator();
                                ImGui.Text($"Buffers: {buffers.Count()}");
                                foreach (var buffer in buffers)
                                {
                                    ImGui.Text($"Buffer: {buffer.Name}");
                                    ImGui.SameLine();
                                    if (ImGui.SmallButton($"Copy##bufferName{i}_{buffer.Name}"))
                                    {
                                        ImGui.SetClipboardText(buffer.Name);
                                    }
                                    ImGui.Text($"Size: {buffer.Content.Length} bytes");
                                }
                            }

                            ImGui.TreePop();
                        }

                        ImGui.Separator();

                        

                        if (ImGui.Button($"Free##{i}"))
                        {
                            gltfList.RemoveAt(i);
                            i--;
                            ImGui.TreePop();
                            continue;
                        }

                        ImGui.TreePop();
                    }
                }

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("NodeGraph Inspector"))
            {
                RenderSceneGraph();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void RenderSceneGraph()
    {
        SceneGraphSnapshot graph;
        MaterialsSnapshot mats;
        try
        {
            graph = _queries.Ask(new GetSceneGraph());
            mats = _queries.Ask(new GetMaterials());
        }
        catch (Exception ex)
        {
            ImGui.Text($"Query failed: {ex.Message}");
            return;
        }

        var childMap = new Dictionary<NodeId, List<NodeRow>>();
        foreach (var row in graph.Nodes)
        {
            if (row.ParentId.HasValue)
            {
                if (!childMap.TryGetValue(row.ParentId.Value, out var list))
                    childMap[row.ParentId.Value] = list = new List<NodeRow>();
                list.Add(row);
            }
        }

        foreach (var row in graph.Nodes.Where(n => !n.ParentId.HasValue))
            RenderNodeRow(row, childMap);

        if (_selectedNode.HasValue)
        {
            var snap = _queries.Ask(new GetNodeSnapshot(_selectedNode.Value));
            ImGui.Separator();
            ImGui.Text($"Selected: {snap.Name} ({snap.Id.Value})");

            int sub = 0;
            foreach (var comp in snap.Components)
            {
                if (comp.Kind == "MaterialBinding")
                {
                    var prop = comp.Properties.FirstOrDefault(p => p.Name == "MaterialId");
                    int currentId = prop?.Value is int v ? v : -1;
                    string currentName = mats.Materials.FirstOrDefault(m => m.Id.Value == currentId)?.Name ?? "[none]";

                    if (ImGui.BeginCombo($"Submesh {sub}", currentName))
                    {
                        foreach (var mat in mats.Materials)
                        {
                            bool sel = mat.Id.Value == currentId;
                            if (ImGui.Selectable(mat.Name, sel))
                            {
                                _commands.Post(new ChangeMaterial(Guid.NewGuid(), snap.Id, sub, mat.Id));
                            }
                            if (sel) ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    sub++;
                }
            }
        }
    }

    private void RenderNodeRow(NodeRow row, Dictionary<NodeId, List<NodeRow>> childMap)
    {
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth | TreeLineFlag;
        if (_selectedNode.HasValue && _selectedNode.Value.Equals(row.Id))
            flags |= ImGuiTreeNodeFlags.Selected;
        if (!childMap.ContainsKey(row.Id))
            flags |= ImGuiTreeNodeFlags.Leaf;

        bool open = ImGui.TreeNodeEx($"{row.Name}##{row.Id.Value}", flags);
        if (ImGui.IsItemClicked())
        {
            _selectedNode = row.Id;
            _commands.Post(new SelectNode(Guid.NewGuid(), row.Id));
        }

        if (open)
        {
            if (childMap.TryGetValue(row.Id, out var children))
                foreach (var ch in children)
                    RenderNodeRow(ch, childMap);
            ImGui.TreePop();
        }
    }

    private void findGltfs()
    {
        var modelDir = EngineConfig.ModelDirectory;
        if (!Directory.Exists(modelDir))
        {
            return;
        }

        var files = Directory
            .EnumerateFiles(modelDir, "*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".glb", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            foundGltfPaths.Add(file);
        }
    }

    private void LoadGltf(string path)
    {
        try
        {
            var model = ModelRoot.Load(path);
            var json = ExtractGltfJson(path);
            gltfList.Add((path, model, json));
            gltfLoadMessage =
                $"Loaded {Path.GetFileName(path)} (Scenes: {model.LogicalScenes.Count()}, Nodes: {model.LogicalNodes.Count()}, Meshes: {model.LogicalMeshes.Count()}, Materials: {model.LogicalMaterials.Count()})";
            gltfLoadMessageColor = new System.Numerics.Vector4(0, 1, 0, 1);
        }
        catch (Exception ex)
        {
            gltfLoadMessage = $"Failed: {ex.Message}";
            gltfLoadMessageColor = new System.Numerics.Vector4(1, 0, 0, 1);
        }
    }

    private static string ExtractGltfJson(string path)
    {
        if (path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);
            fs.Position = 12; // skip header
            var chunkLength = br.ReadInt32();
            var chunkType = br.ReadUInt32(); // JSON chunk
            var jsonBytes = br.ReadBytes(chunkLength);
            return Encoding.UTF8.GetString(jsonBytes);
        }

        return File.ReadAllText(path);
    }
}
