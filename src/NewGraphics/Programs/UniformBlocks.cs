using System.Numerics;
using System.Runtime.InteropServices;

namespace RenderMaster.src.NewGraphics.Programs
{
    internal static class LightingLimits
    {
        public const int MaxPointLights = 32;
        public const int MaxSpotLights = 16;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct FrameBlock
    {
        public Matrix4x4 ViewProj;
        public Vector3 CameraWS; public float Time;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct ObjectBlock
    {
        public Matrix4x4 World;
        public Matrix4x4 NormalWorld;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct MaterialBlock
    {
        public Vector4 BaseColorFactor;
        public float Metallic;
        public float Roughness;
        public float AlphaCutoff;
        public float Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct PointLightGpu
    {
        public Vector4 PositionRange;
        public Vector4 ColorIntensity;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct SpotLightGpu
    {
        public Vector4 PositionRange;
        public Vector4 DirectionInner;
        public Vector4 ColorOuter;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    unsafe struct LightBlock
    {
        public Vector4 Counts;
        public fixed PointLightGpu PointLights[LightingLimits.MaxPointLights];
        public fixed SpotLightGpu SpotLights[LightingLimits.MaxSpotLights];
    }
}
