using System;
using System.Collections.Generic;
using System.Numerics;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Frame
{
    using CpuMeshHandle = Handle<PreparedMeshBuffer>;
    using CpuMatHandle = Handle<MaterialCPU>;

    static class DrawExtractor
    {
        // Extract + classify in one pass to avoid re-lookups.
        public static List<ClassifiedDraw> Build(LoadedNodes nodes, CPUResourceTable cpu, UploadResult map)
        {
            nodes.UpdateWorldTransforms();
            var draws = new List<ClassifiedDraw>(capacity: 256);

            void Traverse(Node node)
            {
                var xf = node.GetComponent<TransformComponent>()?.WorldTransform ?? Matrix4x4.Identity;
                foreach (var mc in node.GetComponents<MeshComponent>())
                {
                    var meshGpu = map.Map(mc.Mesh);
                    if (!meshGpu.IsValid) continue;

                    var matGpu = map.Map(mc.Material);

                    var packet = new DrawPacket(meshGpu, mc.Submesh, matGpu, xf);

                    // Classify based on CPU-side material semantics
                    var (tech, pass) = Classify(cpu, mc.Material);
                    var pipeline = ComputePipelineId(tech, pass);

                    draws.Add(new ClassifiedDraw(packet, tech, pass, pipeline));
                }

                foreach (var child in node.Children)
                    Traverse(child);
            }

            foreach (var root in nodes.All)
                Traverse(root);

            return draws;
        }

        private static (TechniqueKind, PassKind) Classify(CPUResourceTable cpu, CpuMatHandle matHandle)
        {
            // Defaults: treat as opaque PBR if information is missing.
            TechniqueKind tech = TechniqueKind.PBR_MetalRough;
            PassKind pass = PassKind.ForwardOpaque;

            if (matHandle.IsValid)
            {
                var mat = cpu.Materials[matHandle.Id];

                // Heuristic: if base color factor alpha < 1, mark transparent.
                if (mat.BaseColorFactor.HasValue && mat.BaseColorFactor.Value.W < 0.999f)
                {
                    pass = PassKind.ForwardTransparent;
                }

                // Heuristic: if no PBR factors are present, fall back to unlit (for future unlit pipeline)
                bool hasPbr = mat.MetallicFactor.HasValue || mat.RoughnessFactor.HasValue ||
                               mat.Textures.ContainsKey("MetallicRoughnessTexture") ||
                               mat.Textures.ContainsKey("NormalTexture");
                if (!hasPbr)
                {
                    tech = TechniqueKind.Unlit;
                }
            }

            return (tech, pass);
        }

        private static uint ComputePipelineId(TechniqueKind tech, PassKind pass)
        {
            // Stable, compact encoding reserving future space for variants.
            // Layout: [tech:16][pass:8]
            return ((uint)tech << 8) | ((uint)pass & 0xFF);
        }
    }
}

