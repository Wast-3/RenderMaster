using System.Numerics;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.src.NewGraphics.Programs;

namespace RenderMaster.src.NewGraphics.Frame
{
    static class RendererCore
    {
        public static void Render(
            LoadedNodes nodes, CPUResourceTable cpu, UploadResult map, GPUResourceTable gpu,
            ProgramLibrary programs, ProgramUniforms uniforms,
            in FrameBlock frame)
        {
            uniforms.BeginFrame();
            uniforms.Frame.Update(frame);
            uniforms.Frame.Bind(BindingPoints.Frame);

            var draws = DrawExtractor.Build(nodes, cpu, map);
            DrawSorter.SortInPlace(draws);

            DrawEncoder.EncodeAndDraw(
                draws, gpu, programs, uniforms,
                materialBlockOf: h =>
                {
                    var mb = new MaterialBlock
                    {
                        BaseColorFactor = cpu.Materials[map.CpuToGpu_Mat[h.Id] >= 0 ? h.Id : 0].BaseColorFactor ?? new Vector4(1,1,1,1),
                        Metallic   = cpu.Materials[h.Id].MetallicFactor ?? 1f,
                        Roughness  = cpu.Materials[h.Id].RoughnessFactor ?? 1f,
                        AlphaCutoff = 0.5f,
                        Flags = 0f
                    };
                    return mb;
                },
                computeNormalWorld: w => DrawEncoder.ComputeNormalWorld(w));
        }
    }
}

