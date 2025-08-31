using System;
using System.Collections.Generic;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Resources
{
    sealed class ResourceUploader
    {
        private int uploadedTexCount  = 0;
        private int uploadedMeshCount = 0;
        private int uploadedMatCount  = 0;

        private int[] texRemap  = Array.Empty<int>();
        private int[] meshRemap = Array.Empty<int>();
        private int[] matRemap  = Array.Empty<int>();

        private readonly SamplerDesc defaultSampler;

        public ResourceUploader(SamplerDesc defaultSampler) => this.defaultSampler = defaultSampler;

        public UploadResult UploadIncremental(CPUResourceTable cpu, GPUResourceTable gpu)
        {
            EnsureSize(ref texRemap,  cpu.Textures.Count);
            EnsureSize(ref meshRemap, cpu.MeshBuffers.Count);
            EnsureSize(ref matRemap,  cpu.Materials.Count);

            for (int i = uploadedTexCount; i < cpu.Textures.Count; i++)
            {
                var cpuTex = cpu.Textures[i];
                var gpuTex = gpu.CreateTexture(cpuTex, defaultSampler);
                texRemap[i] = gpuTex.Id;
            }
            uploadedTexCount = cpu.Textures.Count;

            for (int i = uploadedMeshCount; i < cpu.MeshBuffers.Count; i++)
            {
                var cpuMesh = cpu.MeshBuffers[i];
                var gpuMesh = gpu.CreateMesh(cpuMesh);
                meshRemap[i] = gpuMesh.Id;
            }
            uploadedMeshCount = cpu.MeshBuffers.Count;

            for (int i = uploadedMatCount; i < cpu.Materials.Count; i++)
            {
                var cpuMat = cpu.Materials[i];

                var texDict = new Dictionary<string, (int textureId, int samplerId)>();
                foreach (var (semantic, cpuTexHandle) in cpuMat.Textures)
                {
                    var gpuTexHandle = RemapTexture(cpuTexHandle);
                    if (!gpuTexHandle.IsValid)
                    {
                        continue;
                    }
                    var tex = gpu.GetTexture(gpuTexHandle);
                    texDict[semantic] = (tex.id, tex.sampler);
                }

                var gpuMat = gpu.CreateMaterialResolved(texDict);
                matRemap[i] = gpuMat.Id;
            }
            uploadedMatCount = cpu.Materials.Count;

            return new UploadResult
            {
                CpuToGpu_Tex  = (int[])texRemap.Clone(),
                CpuToGpu_Mesh = (int[])meshRemap.Clone(),
                CpuToGpu_Mat  = (int[])matRemap.Clone()
            };

            Handle<GPUResourceTable.TextureGPU> RemapTexture(Handle<PreparedTexture> h) =>
                h.IsValid && h.Id < texRemap.Length && texRemap[h.Id] >= 0
                    ? new Handle<GPUResourceTable.TextureGPU>(texRemap[h.Id])
                    : Handle<GPUResourceTable.TextureGPU>.Invalid;
        }

        private static void EnsureSize(ref int[] arr, int size)
        {
            if (arr.Length >= size) return;
            var old = arr;
            arr = new int[size];
            Array.Fill(arr, -1);
            Array.Copy(old, arr, old.Length);
        }
    }
}
