using System;
using System.Collections.Generic;
using System.Linq;
using SharpGLTF.Schema2;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using SceneNode = RenderMaster.src.NewGraphics.Scene.Node;
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
        public LoadedNodes Nodes { get; } = new LoadedNodes();

        public void LoadResources(CPUResourceTable table)
        {
            var model = ModelRoot.Load(filepath);

            var texMap = new Dictionary<SharpGLTF.Schema2.Texture, TextureHandle>();
            foreach (var tex in model.LogicalTextures)
            {
                var bytes = tex.PrimaryImage != null ? tex.PrimaryImage.Content.Content.ToArray() : Array.Empty<byte>();
                var prepared = new PreparedTexture(bytes);
                var handle = table.AddTexture(prepared);
                texMap[tex] = handle;
            }

            var matMap = new Dictionary<SharpGLTF.Schema2.Material, MaterialHandle>();
            foreach (var mat in model.LogicalMaterials)
            {
                var material = new MaterialCPU();

                material.DoubleSided = mat.DoubleSided;

                var pbr = mat.PBRMetallicRoughness;
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

            var meshMap = new Dictionary<SharpGLTF.Schema2.Mesh, (MeshHandle handle, SubmeshSpan[] spans)>();
            foreach (var mesh in model.LogicalMeshes)
            {
                var prepared = new PreparedMeshBuffer(mesh);
                var meshHandle = table.AddMeshBuffer(prepared);
                meshMap[mesh] = (meshHandle, prepared.Submeshes.ToArray());
            }

            void ConvertNode(SharpGLTF.Schema2.Node src)
            {
                var node = new SceneNode();
                node.AddComponent(new TransformComponent(src.LocalMatrix));

                if (src.Mesh != null && meshMap.TryGetValue(src.Mesh, out var meshInfo))
                {
                    var (meshHandle, spans) = meshInfo;
                    for (int i = 0; i < src.Mesh.Primitives.Count; i++)
                    {
                        var prim = src.Mesh.Primitives[i];
                        var matHandle = prim.Material != null && matMap.TryGetValue(prim.Material, out var mh)
                            ? mh
                            : default;
                        var span = spans[i];
                        node.AddComponent(new MeshComponent(meshHandle, matHandle, span));
                    }
                }

                Nodes.AddNode(node);

                foreach (var child in src.VisualChildren)
                    ConvertNode(child);
            }

            foreach (var root in model.LogicalNodes.Where(n => n.VisualParent == null))
                ConvertNode(root);
        }
    }
}
