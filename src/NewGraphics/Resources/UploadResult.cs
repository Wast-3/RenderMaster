using System;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Resources
{
    using MeshGPUHandle = Handle<GPUResourceTable.MeshGPU>;
    using TextureGPUHandle = Handle<GPUResourceTable.TextureGPU>;
    using MaterialGPUHandle = Handle<GPUResourceTable.MaterialGPU>;
    using CpuMeshHandle = Handle<PreparedMeshBuffer>;
    using CpuTexHandle = Handle<PreparedTexture>;
    using CpuMatHandle = Handle<MaterialCPU>;

    sealed class UploadResult
    {
        public int[] CpuToGpu_Mesh  = Array.Empty<int>();
        public int[] CpuToGpu_Tex   = Array.Empty<int>();
        public int[] CpuToGpu_Mat   = Array.Empty<int>();

        public MeshGPUHandle Map(CpuMeshHandle h) =>
            h.IsValid && h.Id < CpuToGpu_Mesh.Length && CpuToGpu_Mesh[h.Id] >= 0
                ? new MeshGPUHandle(CpuToGpu_Mesh[h.Id])
                : MeshGPUHandle.Invalid;

        public TextureGPUHandle Map(CpuTexHandle h) =>
            h.IsValid && h.Id < CpuToGpu_Tex.Length && CpuToGpu_Tex[h.Id] >= 0
                ? new TextureGPUHandle(CpuToGpu_Tex[h.Id])
                : TextureGPUHandle.Invalid;

        public MaterialGPUHandle Map(CpuMatHandle h) =>
            h.IsValid && h.Id < CpuToGpu_Mat.Length && CpuToGpu_Mat[h.Id] >= 0
                ? new MaterialGPUHandle(CpuToGpu_Mat[h.Id])
                : MaterialGPUHandle.Invalid;
    }
}
