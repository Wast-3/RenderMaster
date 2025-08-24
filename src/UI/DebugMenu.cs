using System.Diagnostics;
using System.Linq;
using ImGuiNET;

namespace RenderMaster;

public class DebugMenu : IUIElement
{
    public string FpsString { get; set; } = string.Empty;

    // list of loaded glTFs are stored in this list
    private List<unsureWhichTypeIsBest> gltfs;

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

                // Some kind of path to a glTF file
                // a load button

                // here, we'd loop over all the loaded gltfs, and for each, display basic information. 
                // For now, just show a simple model / scene overview with scene count, node/mesh/material counts. 

                // include a button to free each gltf in the list from memory
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }
}
