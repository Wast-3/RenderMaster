using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Resources
{
    using MeshHandle = Handle<GPUResourceTable.MeshGPU>;
    using TextureHandle = Handle<GPUResourceTable.TextureGPU>;
    using MaterialHandle = Handle<GPUResourceTable.MaterialGPU>;

    sealed class GPUResourceTable
    {
        public struct MeshGPU
        {
            public int vao;
            public int vbo;
            public int ebo;
            public DrawElementsType indexType;
            public int indexCount;
            public SubmeshSpan[] submeshes;
        }

        public struct TextureGPU
        {
            public int id;
            public int sampler;
        }

        public struct MaterialGPU
        {
            public Dictionary<string, (int textureId, int samplerId)> textures;
        }

        readonly List<MeshGPU> meshes = new();
        readonly List<TextureGPU> textures = new();
        readonly List<MaterialGPU> materials = new();

        public ref readonly MeshGPU GetMesh(MeshHandle h) => ref meshes[h.Id];
        public ref readonly TextureGPU GetTexture(TextureHandle h) => ref textures[h.Id];
        public ref readonly MaterialGPU GetMaterial(MaterialHandle h) => ref materials[h.Id];

        public MeshHandle CreateMesh(PreparedMeshBuffer cpu)
        {
            GL.CreateVertexArrays(1, out int vao);
            GL.CreateBuffers(1, out int vbo);
            GL.NamedBufferStorage(vbo, cpu.Vertices.Length * sizeof(float), cpu.Vertices, BufferStorageFlags.None);
            GL.VertexArrayVertexBuffer(vao, 0, vbo, IntPtr.Zero, cpu.VertexStrideBytes);

            int offset = 0;
            GL.EnableVertexArrayAttrib(vao, 0);
            GL.VertexArrayAttribBinding(vao, 0, 0);
            GL.VertexArrayAttribFormat(vao, 0, 3, VertexAttribType.Float, false, offset);
            offset += 3 * sizeof(float);

            GL.EnableVertexArrayAttrib(vao, 1);
            GL.VertexArrayAttribBinding(vao, 1, 0);
            GL.VertexArrayAttribFormat(vao, 1, 3, VertexAttribType.Float, false, offset);
            offset += 3 * sizeof(float);

            GL.EnableVertexArrayAttrib(vao, 2);
            GL.VertexArrayAttribBinding(vao, 2, 0);
            GL.VertexArrayAttribFormat(vao, 2, 4, VertexAttribType.Float, false, offset);
            offset += 4 * sizeof(float);

            GL.EnableVertexArrayAttrib(vao, 3);
            GL.VertexArrayAttribBinding(vao, 3, 0);
            GL.VertexArrayAttribFormat(vao, 3, 2, VertexAttribType.Float, false, offset);

            int ebo = 0;
            DrawElementsType indexType = DrawElementsType.UnsignedInt;
            if (cpu.IndexElementSize != 0)
            {
                GL.CreateBuffers(1, out ebo);
                GL.NamedBufferStorage(ebo, cpu.Indices.Length, cpu.Indices, BufferStorageFlags.None);
                GL.VertexArrayElementBuffer(vao, ebo);
                indexType = cpu.IndexElementSize switch
                {
                    1 => DrawElementsType.UnsignedByte,
                    2 => DrawElementsType.UnsignedShort,
                    4 => DrawElementsType.UnsignedInt,
                    _ => DrawElementsType.UnsignedInt
                };
            }

            var meshGpu = new MeshGPU
            {
                vao = vao,
                vbo = vbo,
                ebo = ebo,
                indexType = indexType,
                indexCount = cpu.IndexCount,
                submeshes = cpu.Submeshes.ToArray()
            };
            meshes.Add(meshGpu);
            return new MeshHandle(meshes.Count - 1);
        }

        public TextureHandle CreateTexture(PreparedTexture cpu, SamplerDesc sampler)
        {
            GL.CreateTextures(TextureTarget.Texture2D, 1, out int tex);
            GL.TextureStorage2D(tex, 1, SizedInternalFormat.Rgba8, cpu.Width, cpu.Height);
            GL.TextureSubImage2D(tex, 0, 0, 0, cpu.Width, cpu.Height, PixelFormat.Rgba, PixelType.UnsignedByte, cpu.Pixels);

            GL.CreateSamplers(1, out int samp);
            GL.SamplerParameter(samp, SamplerParameterName.TextureMinFilter, (int)sampler.MinFilter);
            GL.SamplerParameter(samp, SamplerParameterName.TextureMagFilter, (int)sampler.MagFilter);
            GL.SamplerParameter(samp, SamplerParameterName.TextureWrapS, (int)sampler.WrapS);
            GL.SamplerParameter(samp, SamplerParameterName.TextureWrapT, (int)sampler.WrapT);

            var texGpu = new TextureGPU { id = tex, sampler = samp };
            textures.Add(texGpu);
            return new TextureHandle(textures.Count - 1);
        }

        public MaterialHandle CreateMaterialResolved(Dictionary<string, (int textureId, int samplerId)> texDict)
        {
            var mat = new MaterialGPU { textures = texDict };
            materials.Add(mat);
            return new MaterialHandle(materials.Count - 1);
        }

        public void Destroy(MeshHandle h)
        {
            var mesh = meshes[h.Id];
            if (mesh.ebo != 0) GL.DeleteBuffer(mesh.ebo);
            GL.DeleteBuffer(mesh.vbo);
            GL.DeleteVertexArray(mesh.vao);
            meshes[h.Id] = default;
        }

        public void Destroy(TextureHandle h)
        {
            var tex = textures[h.Id];
            GL.DeleteTexture(tex.id);
            GL.DeleteSampler(tex.sampler);
            textures[h.Id] = default;
        }

        public void Destroy(MaterialHandle h)
        {
            materials[h.Id] = default;
        }
    }
}
