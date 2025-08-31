using System.Collections.Generic;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;

namespace RenderMaster.src.NewGraphics.Frame
{
    // High-level glue: Extract → Sort → Encode
    static class RendererCore
    {
        public static void Render(LoadedNodes nodes, CPUResourceTable cpu, UploadResult map, GPUResourceTable gpu)
        {
            // Extract + classify
            List<ClassifiedDraw> draws = DrawExtractor.Build(nodes, cpu, map);

            // Sort (state-friendly ordering)
            DrawSorter.SortInPlace(draws);

            // Encode + submit
            DrawEncoder.EncodeAndDraw(draws, gpu);
        }
    }
}

