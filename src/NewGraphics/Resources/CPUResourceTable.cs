using System.Collections.Generic;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Resources
{
    using MeshHandle = Handle<PreparedMeshBuffer>;
    using TextureHandle = Handle<PreparedTexture>;
    using MaterialHandle = Handle<MaterialCPU>;

    class CPUResourceTable
    {
        List<PreparedMeshBuffer> meshBuffers = new List<PreparedMeshBuffer>();
        List<PreparedTexture> textures = new List<PreparedTexture>();
        List<MaterialCPU> materials = new List<MaterialCPU>();

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
            return new MaterialHandle(materials.Count - 1);
        }
    }
}

