using System;
using System.Collections.Generic;
using System.Numerics;
using RenderMaster.Engine;
using RenderMaster.src.NewGraphics.Programs;
using RenderMaster.src.NewGraphics.Scene;

namespace RenderMaster.src.NewGraphics.Frame
{
    readonly record struct PreparedPointLight(Vector3 Position, Vector3 Color, float Intensity, float Range);

    readonly record struct PreparedSpotLight(
        Vector3 Position,
        Vector3 Direction,
        Vector3 Color,
        float Intensity,
        float Range,
        float InnerConeCos,
        float OuterConeCos);

    readonly struct PreparedLights
    {
        public PreparedLights(List<PreparedPointLight> points, List<PreparedSpotLight> spots)
        {
            Points = points;
            Spots = spots;
        }

        public IReadOnlyList<PreparedPointLight> Points { get; }
        public IReadOnlyList<PreparedSpotLight> Spots { get; }
    }

    static class LightExtractor
    {
        public static PreparedLights Build(LoadedNodes nodes)
        {
            var points = new List<PreparedPointLight>();
            var spots = new List<PreparedSpotLight>();

            foreach (var node in nodes.All)
            {
                var world = node.GetComponent<TransformComponent>()?.WorldTransform ?? Matrix4x4.Identity;
                var position = world.Translation;

                foreach (var pl in node.GetComponents<PointLightComponent>())
                {
                    points.Add(new PreparedPointLight(position, pl.Color, pl.Intensity, pl.Range));
                }

                foreach (var sl in node.GetComponents<SpotLightComponent>())
                {
                    var direction = ExtractDirection(world);
                    var (innerCos, outerCos) = ComputeCone(sl.InnerConeAngle, sl.OuterConeAngle);
                    spots.Add(new PreparedSpotLight(position, direction, sl.Color, sl.Intensity, sl.Range, innerCos, outerCos));
                }
            }

            return new PreparedLights(points, spots);
        }

        static Vector3 ExtractDirection(Matrix4x4 world)
        {
            var dir = Vector3.TransformNormal(-Vector3.UnitZ, world);
            if (dir.LengthSquared() < 1e-6f)
                return -Vector3.UnitZ;
            return Vector3.Normalize(dir);
        }

        static (float innerCos, float outerCos) ComputeCone(float inner, float outer)
        {
            float clampedInner = MathF.Clamp(inner, 0f, MathF.PI * 0.5f);
            float clampedOuter = MathF.Clamp(outer, 0f, MathF.PI * 0.5f);
            if (clampedOuter < clampedInner)
                clampedOuter = clampedInner;
            return (MathF.Cos(clampedInner), MathF.Cos(clampedOuter));
        }
    }

    static class LightEncoder
    {
        static bool pointWarningIssued;
        static bool spotWarningIssued;

        public static LightBlock BuildBlock(in PreparedLights lights)
        {
            var block = new LightBlock();

            int pointCount = Math.Min(lights.Points.Count, LightingLimits.MaxPointLights);
            int spotCount = Math.Min(lights.Spots.Count, LightingLimits.MaxSpotLights);

            if (lights.Points.Count > LightingLimits.MaxPointLights && !pointWarningIssued)
            {
                Logger.Log(
                    $"Truncating point lights: supported={LightingLimits.MaxPointLights} requested={lights.Points.Count}",
                    LogLevel.Warning);
                pointWarningIssued = true;
            }

            if (lights.Spots.Count > LightingLimits.MaxSpotLights && !spotWarningIssued)
            {
                Logger.Log(
                    $"Truncating spot lights: supported={LightingLimits.MaxSpotLights} requested={lights.Spots.Count}",
                    LogLevel.Warning);
                spotWarningIssued = true;
            }

            block.Counts = new Vector4(pointCount, spotCount, 0f, 0f);

            unsafe
            {
                fixed (float* pointPtr = block.PointLights)
                {
                    var dest = (PointLightGpu*)pointPtr;
                    for (int i = 0; i < pointCount; i++)
                    {
                        var src = lights.Points[i];
                        float range = src.Range > 0f ? src.Range : -1f;
                        var colorIntensity = src.Color * src.Intensity;
                        dest[i] = new PointLightGpu
                        {
                            PositionRange = new Vector4(src.Position, range),
                            ColorIntensity = new Vector4(colorIntensity, src.Intensity)
                        };
                    }

                    for (int i = pointCount; i < LightingLimits.MaxPointLights; i++)
                        dest[i] = default;
                }

                fixed (float* spotPtr = block.SpotLights)
                {
                    var dest = (SpotLightGpu*)spotPtr;
                    for (int i = 0; i < spotCount; i++)
                    {
                        var src = lights.Spots[i];
                        float range = src.Range > 0f ? src.Range : -1f;
                        var colorIntensity = src.Color * src.Intensity;
                        var dir = src.Direction;
                        dest[i] = new SpotLightGpu
                        {
                            PositionRange = new Vector4(src.Position, range),
                            DirectionInner = new Vector4(dir, src.InnerConeCos),
                            ColorOuter = new Vector4(colorIntensity, src.OuterConeCos)
                        };
                    }

                    for (int i = spotCount; i < LightingLimits.MaxSpotLights; i++)
                        dest[i] = default;
                }
            }

            return block;
        }
    }
}
