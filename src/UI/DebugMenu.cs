using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ImGuiNET;
using SharpGLTF.Schema2;

namespace RenderMaster;

public class DebugMenu : IUIElement
{
    public string FpsString { get; set; } = string.Empty;

    // list of loaded glTFs are stored in this list
    private readonly List<(string path, ModelRoot model)> gltfList = new();
    // list of all glTF files discovered on startup
    private readonly List<string> foundGltfs = new();
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
                    for (int i = 0; i < foundGltfs.Count; i++)
                    {
                        var path = foundGltfs[i];
                        if (ImGui.TreeNode($"{Path.GetFileName(path)}##found{i}"))
                        {
                            ImGui.Text($"Filename: {Path.GetFileName(path)}");
                            ImGui.Text($"Full path: {path}");
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
                    var (path, model) = gltfList[i];
                    if (ImGui.TreeNode($"{System.IO.Path.GetFileName(path)}##{i}"))
                    {
                        ImGui.Text($"Scenes: {model.LogicalScenes.Count()}");
                        ImGui.Text($"Nodes: {model.LogicalNodes.Count()}");
                        ImGui.Text($"Meshes: {model.LogicalMeshes.Count()}");
                        ImGui.Text($"Materials: {model.LogicalMaterials.Count()}");

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
        if (Directory.Exists(modelDir))
        {
            var files = Directory.EnumerateFiles(modelDir, "*.gltf", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                foundGltfs.Add(file);
            }
        }
    }

    private void LoadGltf(string path)
    {
        try
        {
            var model = ModelRoot.Load(path);
            gltfList.Add((path, model));
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
}
