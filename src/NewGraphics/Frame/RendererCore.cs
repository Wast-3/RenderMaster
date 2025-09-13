using System.Numerics;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.src.NewGraphics.Programs;
using System.Runtime.CompilerServices;

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

            var gathered = nodes.GatherLights();
            var lb = new LightsBlock();
            unsafe
            {
                int count = System.Math.Min(gathered.Count, LightsBlock.MaxLights);
                lb.Count = count;
                LightsBlock* plb = &lb;
                float* dest = plb->Lights;
                for (int i = 0; i < count; i++)
                {
                    var (light, world) = gathered[i];
                    var pos = new Vector3(world.M41, world.M42, world.M43);
                    var dir = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, world));
                    var g = new GpuLight
                    {
                        PositionType = new Vector4(pos, (float)light.Kind),
                        DirectionRange = new Vector4(dir, light.Range),
                        ColorIntensity = new Vector4(light.Color * light.Intensity, light.Intensity),
                        SpotAngles = new Vector4(
                            System.MathF.Cos(light.InnerConeAngle),
                            System.MathF.Cos(light.OuterConeAngle), 0, 0)
                    };
                    Unsafe.CopyBlockUnaligned(
                        (byte*)dest + i * Unsafe.SizeOf<GpuLight>(),
                        Unsafe.AsPointer(ref g),
                        (uint)Unsafe.SizeOf<GpuLight>());
                }
            }
            uniforms.Lights.Update(lb);
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

            DrawEncoder.EncodeAndDraw(
                draws, gpu, programs, uniforms,
                materialBlockOf: h =>
                {
                    int cpuId = (h.Id < map.GpuToCpu_Mat.Length && map.GpuToCpu_Mat[h.Id] >= 0)
                        ? map.GpuToCpu_Mat[h.Id]
                        : 0;
                    var m = cpu.Materials[cpuId];
                    var mb = new MaterialBlock
                    {
                        BaseColorFactor = m.BaseColorFactor ?? new Vector4(1, 1, 1, 1),
                        Metallic = m.MetallicFactor ?? 1f,
                        Roughness = m.RoughnessFactor ?? 1f,
                        AlphaCutoff = 0.5f,
                        Flags = 0f
                    };
                    return mb;
                });
        }
    }
}

