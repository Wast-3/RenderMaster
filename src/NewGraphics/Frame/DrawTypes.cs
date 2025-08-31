using System;
using System.Numerics;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Frame
{
    using MeshHandle = Handle<GPUResourceTable.MeshGPU>;
    using MaterialHandle = Handle<GPUResourceTable.MaterialGPU>;

    // Core immutable draw payload produced by extraction. References GPU handles only.
    readonly record struct DrawPacket(
        MeshHandle Mesh,
        SubmeshSpan Span,
        MaterialHandle Material,
        Matrix4x4 World
    );

    // Rendering technique selection (kept small; extend as features land)
    enum TechniqueKind : int
    {
        Unlit = 0,
        PBR_MetalRough = 1,
    }

    // High-level pass classification (order encodes constraints like transparency)
    enum PassKind : int
    {
        DepthOnly = 0,
        ForwardOpaque = 10,
        ForwardTransparent = 20,
        Shadow = 30,
    }

    // A draw with classification + sort key for efficient submission.
    readonly struct ClassifiedDraw
    {
        public readonly DrawPacket Packet;
        public readonly TechniqueKind Technique;
        public readonly PassKind Pass;
        public readonly uint PipelineId; // Program/shader variant id (future-proof)
        public readonly ulong SortKey;   // Precomputed sort key for hot-path sorting

        public ClassifiedDraw(DrawPacket packet, TechniqueKind tech, PassKind pass, uint pipelineId)
        {
            Packet = packet;
            Technique = tech;
            Pass = pass;
            PipelineId = pipelineId;
            SortKey = SortKeyUtil.Build(pass, pipelineId, (uint)Math.Max(0, packet.Material.Id), (uint)Math.Max(0, packet.Mesh.Id));
        }
    }

    internal static class SortKeyUtil
    {
        // Sort key layout (MSB → LSB):
        // [ Pass : 8 ][ PipelineId : 20 ][ MaterialId : 20 ][ MeshId : 16 ] = 64 bits
        public static ulong Build(PassKind pass, uint pipelineId, uint materialId, uint meshId)
        {
            const int PassBits = 8;
            const int PipelineBits = 20;
            const int MaterialBits = 20;
            const int MeshBits = 16;

            ulong p = ((ulong)(int)pass) & ((1UL << PassBits) - 1);
            ulong pl = pipelineId & ((1U << PipelineBits) - 1);
            ulong m = materialId & ((1U << MaterialBits) - 1);
            ulong me = meshId & ((1U << MeshBits) - 1);

            return (p << (PipelineBits + MaterialBits + MeshBits)) |
                   (pl << (MaterialBits + MeshBits)) |
                   (m << MeshBits) |
                   me;
        }
    }
}
