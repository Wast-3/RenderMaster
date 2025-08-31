using System;
using System.Collections.Generic;
using System.Linq;
using SharpGLTF.Schema2;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Loading
{
    using MeshHandle = Handle<PreparedMeshBuffer>;
    using TextureHandle = Handle<PreparedTexture>;
    using MaterialHandle = Handle<MaterialCPU>;

    class LoadFromGltfFile : ILoadsResourceTable
    {
        public string filepath { get; init; }
        public CPUResourceTable ResourceTable { get; } = new CPUResourceTable();

        public void LoadResources(CPUResourceTable table)
        {
            var model = ModelRoot.Load(filepath);

            var texMap = new Dictionary<SharpGLTF.Schema2.Texture, TextureHandle>();
            foreach (var tex in model.LogicalTextures)
            {
                var bytes = tex.PrimaryImage?.Content?.Content.ToArray() ?? Array.Empty<byte>();
                var prepared = new PreparedTexture(bytes);
                var handle = table.AddTexture(prepared);
                texMap[tex] = handle;
            }

            var matMap = new Dictionary<SharpGLTF.Schema2.Material, MaterialHandle>();
            foreach (var mat in model.LogicalMaterials)
            {
                var material = new MaterialCPU();

                material.DoubleSided = mat.DoubleSided;

                var pbr = mat.PbrMetallicRoughness;
                if (pbr != null)
                {
                    material.BaseColorFactor = pbr.BaseColorFactor;
                    material.MetallicFactor = pbr.MetallicFactor;
                    material.RoughnessFactor = pbr.RoughnessFactor;

                    var bc = pbr.BaseColorTexture?.Texture;
                    if (bc != null && texMap.TryGetValue(bc, out var bcHandle))
                        material.Textures["BaseColorTexture"] = bcHandle;

                    var mr = pbr.MetallicRoughnessTexture?.Texture;
                    if (mr != null && texMap.TryGetValue(mr, out var mrHandle))
                        material.Textures["MetallicRoughnessTexture"] = mrHandle;
                }

                var normal = mat.FindChannel("Normal")?.Texture;
                if (normal != null && texMap.TryGetValue(normal, out var nHandle))
                    material.Textures["NormalTexture"] = nHandle;

                var mHandle = table.AddMaterial(material);
                matMap[mat] = mHandle;
            }

            foreach (var mesh in model.LogicalMeshes)
            {
                var prepared = new PreparedMeshBuffer(mesh);
                var meshHandle = table.AddMeshBuffer(prepared);

                var prim = mesh.Primitives.FirstOrDefault();
                var matHandle = prim != null && prim.Material != null && matMap.TryGetValue(prim.Material, out var mh)
                    ? mh
                    : new MaterialHandle(0);

                var node = new SceneNode
                {
                    mesh = meshHandle,
                    material = matHandle
                };
            }
        }
    }
}

