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
            float clampedInner = Clamp(inner, 0f, MathF.PI * 0.5f);
            float clampedOuter = Clamp(outer, 0f, MathF.PI * 0.5f);
            if (clampedOuter < clampedInner)
                clampedOuter = clampedInner;
            return (MathF.Cos(clampedInner), MathF.Cos(clampedOuter));
        }

        static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
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

            for (int i = 0; i < pointCount; i++)
            {
                var src = lights.Points[i];
                WritePoint(ref block, i, src);
            }

            ClearPointTail(ref block, pointCount);

            for (int i = 0; i < spotCount; i++)
            {
                var src = lights.Spots[i];
                WriteSpot(ref block, i, src);
            }

            ClearSpotTail(ref block, spotCount);

            return block;
        }

        static void WritePoint(ref LightBlock block, int index, in PreparedPointLight src)
        {
            float range = src.Range > 0f ? src.Range : -1f;
            var colorIntensity = src.Color * src.Intensity;
            int baseIndex = index * LightBlock.PointLightFloatCount;

            block.PointLights[baseIndex + 0] = src.Position.X;
            block.PointLights[baseIndex + 1] = src.Position.Y;
            block.PointLights[baseIndex + 2] = src.Position.Z;
            block.PointLights[baseIndex + 3] = range;

            block.PointLights[baseIndex + 4] = colorIntensity.X;
            block.PointLights[baseIndex + 5] = colorIntensity.Y;
            block.PointLights[baseIndex + 6] = colorIntensity.Z;
            block.PointLights[baseIndex + 7] = src.Intensity;
        }

        static void ClearPointTail(ref LightBlock block, int startIndex)
        {
            int start = startIndex * LightBlock.PointLightFloatCount;
            int end = LightingLimits.MaxPointLights * LightBlock.PointLightFloatCount;
            for (int i = start; i < end; i++)
                block.PointLights[i] = 0f;
        }

        static void WriteSpot(ref LightBlock block, int index, in PreparedSpotLight src)
        {
            float range = src.Range > 0f ? src.Range : -1f;
            var colorIntensity = src.Color * src.Intensity;
            int baseIndex = index * LightBlock.SpotLightFloatCount;

            block.SpotLights[baseIndex + 0] = src.Position.X;
            block.SpotLights[baseIndex + 1] = src.Position.Y;
            block.SpotLights[baseIndex + 2] = src.Position.Z;
            block.SpotLights[baseIndex + 3] = range;

            block.SpotLights[baseIndex + 4] = src.Direction.X;
            block.SpotLights[baseIndex + 5] = src.Direction.Y;
            block.SpotLights[baseIndex + 6] = src.Direction.Z;
            block.SpotLights[baseIndex + 7] = src.InnerConeCos;

            block.SpotLights[baseIndex + 8] = colorIntensity.X;
            block.SpotLights[baseIndex + 9] = colorIntensity.Y;
            block.SpotLights[baseIndex + 10] = colorIntensity.Z;
            block.SpotLights[baseIndex + 11] = src.OuterConeCos;
        }

        static void ClearSpotTail(ref LightBlock block, int startIndex)
        {
            int start = startIndex * LightBlock.SpotLightFloatCount;
            int end = LightingLimits.MaxSpotLights * LightBlock.SpotLightFloatCount;
            for (int i = start; i < end; i++)
                block.SpotLights[i] = 0f;
        }
    }
}
