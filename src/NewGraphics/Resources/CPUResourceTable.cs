using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Numerics;
using RenderMaster.src.NewGraphics.Types;
using RenderMaster.src.NewGraphics.Programs;
[assembly: InternalsVisibleTo("RenderMaster.src.ControlPlane")]
namespace RenderMaster.src.NewGraphics.Resources
{
    using MeshHandle = Handle<PreparedMeshBuffer>;
    using TextureHandle = Handle<PreparedTexture>;
    using MaterialHandle = Handle<MaterialCPU>;

    internal class CPUResourceTable
    {
        List<PreparedMeshBuffer> meshBuffers = new List<PreparedMeshBuffer>();
        List<PreparedTexture> textures = new List<PreparedTexture>();
        List<MaterialCPU> materials = new List<MaterialCPU>();
        List<MaterialBlock> materialBlocks = new List<MaterialBlock>();

        public IReadOnlyList<PreparedMeshBuffer> MeshBuffers => meshBuffers;
        public IReadOnlyList<PreparedTexture> Textures => textures;
        public IReadOnlyList<MaterialCPU> Materials => materials;
        public IReadOnlyList<MaterialBlock> MaterialBlocks => materialBlocks;

        public MeshHandle AddMeshBuffer(PreparedMeshBuffer buffer)
        {
            meshBuffers.Add(buffer);
            return new MeshHandle(meshBuffers.Count - 1);
        }

        public TextureHandle AddTexture(PreparedTexture tex)
        {
            textures.Add(tex);
            return new TextureHandle(textures.Count - 1);
        }

        public MaterialHandle AddMaterial(MaterialCPU mat)
        {
            materials.Add(mat);
            materialBlocks.Add(ToBlock(mat));
            return new MaterialHandle(materials.Count - 1);
        }

        public void UpdateMaterialBlock(int index)
        {
            materialBlocks[index] = ToBlock(materials[index]);
        }

        static MaterialBlock ToBlock(MaterialCPU m) => new MaterialBlock
        {
            BaseColorFactor = m.BaseColorFactor ?? new Vector4(1, 1, 1, 1),
            Metallic = m.MetallicFactor ?? 1f,
            Roughness = m.RoughnessFactor ?? 1f,
            AlphaCutoff = 0.5f,
            Flags = 0f,
        };
    }
}

