using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ImGuiNET;
using SharpGLTF.Schema2;

namespace RenderMaster;

public class DebugMenu : IUIElement
{
    public string FpsString { get; set; } = string.Empty;

    // list of loaded glTFs are stored in this list along with their JSON text
    private readonly List<(string path, ModelRoot model, string json)> gltfList = new();
    // list of all glTF files discovered on startup
    private readonly List<string> foundGltfPaths = new();
    private string gltfPath = string.Empty;
    private string gltfLoadMessage = string.Empty;
    private System.Numerics.Vector4 gltfLoadMessageColor = new(1, 1, 1, 1);

    public DebugMenu()
    {
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
                    var timingsList = entry.Value.Values.ToList();
                    var averageTiming = timingsList.Average();
                    var latestTiming = timingsList.LastOrDefault();

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
                        ImGui.Text($"Average Execution Time (last 100): {averageTiming} ms");
                        ImGui.Text($"Latest Execution Time: {latestTiming} ms");

                        float[] timingsArray = timingsList.Select(t => (float)t).ToArray();

                        if (timingsArray.Length > 0)
                        {
                            ImGui.PlotLines("Timings", ref timingsArray[0], timingsArray.Length, 0, null, 0.0f, float.MaxValue, new System.Numerics.Vector2(0, 80));
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

            ImGui.EndTabBar();
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
