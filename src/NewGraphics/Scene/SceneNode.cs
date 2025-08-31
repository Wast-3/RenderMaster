using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Scene
{
    using MeshHandle = Handle<PreparedMeshBuffer>;
    using MaterialHandle = Handle<MaterialCPU>;

    class SceneNode
    {
        public required MeshHandle mesh { get; init; }
        public required MaterialHandle material { get; init; }
    }
}

