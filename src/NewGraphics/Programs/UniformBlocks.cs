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
}
