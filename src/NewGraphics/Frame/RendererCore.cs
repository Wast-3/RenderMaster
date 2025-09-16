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

            var preparedLights = LightExtractor.Build(nodes);
            var lightBlock = LightEncoder.BuildBlock(preparedLights);
            uniforms.Lights.Update(lightBlock);
            uniforms.Lights.Bind(BindingPoints.Lights);

            //returns a list of draws, classified by technique and pass, for best drawing order
            var draws = DrawExtractor.Build(nodes, cpu, map);
            if (draws.Count == 0)
                RenderMaster.Engine.Logger.Log("No draws extracted this frame (0). Check glTF load/map or camera frustum.", RenderMaster.Engine.LogLevel.Warning);
            else
            {
                int opaque = 0, transp = 0, depth = 0, shadow = 0;
                foreach (var d in draws)
                    switch (d.Pass)
                    {
                        case PassKind.ForwardOpaque:      opaque++; break;
                        case PassKind.ForwardTransparent: transp++; break;
                        case PassKind.DepthOnly:          depth++;  break;
                        case PassKind.Shadow:             shadow++; break;
                    }

                RenderMaster.Engine.Logger.Log(
                    $"Draws this frame: total={draws.Count} opaque={opaque} transp={transp} depth={depth} shadow={shadow}",
                    RenderMaster.Engine.LogLevel.Debug);
            }

            DrawSorter.SortInPlace(draws);

            DrawEncoder.EncodeAndDraw(draws, gpu, programs, uniforms, map, cpu.MaterialBlocks);
        }
    }
}

