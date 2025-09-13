using System.Numerics;
using System.Runtime.InteropServices;

namespace RenderMaster.src.NewGraphics.Programs
{
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
    struct GpuLight
    {
        public Vector4 PositionType;
        public Vector4 DirectionRange;
        public Vector4 ColorIntensity;
        public Vector4 SpotAngles;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    unsafe struct LightsBlock
    {
        public const int MaxLights = 16;
        public int Count;
        private Vector3 _pad;
        public fixed float Lights[MaxLights * 16];
    }
}
