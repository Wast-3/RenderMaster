using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            RenderMaster.Engine.Logger.Log(
                $"Loaded glTF: tex={model.LogicalTextures.Count} mats={model.LogicalMaterials.Count} meshes={model.LogicalMeshes.Count} nodes={model.LogicalNodes.Count}",
                RenderMaster.Engine.LogLevel.Info);

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

                var baseColor = mat.FindChannel("BaseColor");
                if (baseColor != null)
                {
                    material.BaseColorFactor = baseColor.Value.Color;
                    var bc = baseColor.Value.Texture;
                    if (bc != null && texMap.TryGetValue(bc, out var bcHandle))
                    {
                        material.Textures["BaseColorTexture"] = bcHandle;
                        // Base color textures are encoded in sRGB space
                        table.Textures[bcHandle.Id].IsSrgb = true;
                    }
                }

                var mrChan = mat.FindChannel("MetallicRoughness");
                if (mrChan != null)
                {
                    material.MetallicFactor = mrChan.Value.GetFactor("MetallicFactor");
                    material.RoughnessFactor = mrChan.Value.GetFactor("RoughnessFactor");

                    var mr = mrChan.Value.Texture;
                    if (mr != null && texMap.TryGetValue(mr, out var mrHandle))
                    {
                        material.Textures["MetallicRoughnessTexture"] = mrHandle;
                        // Metallic-roughness textures should remain in linear space
                        table.Textures[mrHandle.Id].IsSrgb = false;
                    }
                }

                var normal = mat.FindChannel("Normal")?.Texture;
                if (normal != null && texMap.TryGetValue(normal, out var nHandle))
                {
                    material.Textures["NormalTexture"] = nHandle;
                    // Normal maps also use linear color space
                    table.Textures[nHandle.Id].IsSrgb = false;
                }

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

            int pointLightCount = 0;
            int spotLightCount = 0;
            int skippedLightCount = 0;

            SceneNode ConvertNode(SharpGLTF.Schema2.Node src)
            {
                var local = src.LocalMatrix;
                var node = new SceneNode();
                node.AddComponent(new NameComponent(src.Name ?? "(unnamed)"));
                node.AddComponent(new TransformComponent(local));

                var punctual = src.PunctualLight;
                if (punctual != null)
                {
                    var color = punctual.Color;
                    var intensity = punctual.Intensity;
                    var range = NormalizeLightRange(punctual.Range);

                    switch (punctual.LightType)
                    {
                        case SharpGLTF.Schema2.PunctualLightType.Point:
                            node.AddComponent(new PointLightComponent(color, intensity, range));
                            pointLightCount++;
                            break;

                        case SharpGLTF.Schema2.PunctualLightType.Spot:
                        {
                            var spot = punctual.Spot;
                            float inner = spot?.InnerConeAngle ?? 0f;
                            float outer = spot?.OuterConeAngle ?? (MathF.PI / 4f);
                            node.AddComponent(new SpotLightComponent(color, intensity, range, inner, outer));
                            spotLightCount++;
                            break;
                        }

                        default:
                            skippedLightCount++;
                            RenderMaster.Engine.Logger.Log(
                                $"Skipping unsupported light '{punctual.LightType}' on node '{src.Name ?? "(unnamed)"}'",
                                RenderMaster.Engine.LogLevel.Warning);
                            break;
                    }
                }

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

                foreach (var child in src.VisualChildren)
                {
                    var c = ConvertNode(child);
                    node.AddChild(c);
                }

                return node;
            }

            foreach (var root in model.LogicalNodes.Where(n => n.VisualParent == null))
            {
                var n = ConvertNode(root);
                Nodes.AddNode(n);
            }

            if (pointLightCount > 0 || spotLightCount > 0 || skippedLightCount > 0)
            {
                RenderMaster.Engine.Logger.Log(
                    $"Loaded lights: point={pointLightCount} spot={spotLightCount} skipped={skippedLightCount}",
                    RenderMaster.Engine.LogLevel.Info);
            }
        }

        static float NormalizeLightRange(float rawRange)
        {
            if (rawRange > 0f && !float.IsNaN(rawRange) && !float.IsInfinity(rawRange))
                return rawRange;
            return -1f;
        }
    }
}
