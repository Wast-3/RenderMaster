using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Scene
{
    using MeshHandle = Handle<PreparedMeshBuffer>;
    using MaterialHandle = Handle<MaterialCPU>;

    interface INodeComponent { }

    class TransformComponent : INodeComponent
    {
        public Matrix4x4 Transform { get; set; }
        public TransformComponent(Matrix4x4 transform)
        {
            Transform = transform;
        }
    }

    class MeshComponent : INodeComponent
    {
        public MeshHandle Mesh { get; }
        public MaterialHandle Material { get; }
        public SubmeshSpan Submesh { get; }

        public MeshComponent(MeshHandle mesh, MaterialHandle material, SubmeshSpan submesh)
        {
            Mesh = mesh;
            Material = material;
            Submesh = submesh;
        }
    }

    class Node
    {
        readonly List<INodeComponent> _components = new();

        public void AddComponent(INodeComponent comp) => _components.Add(comp);

        public IEnumerable<T> GetComponents<T>() where T : class, INodeComponent
            => _components.OfType<T>();

        public T? GetComponent<T>() where T : class, INodeComponent
            => _components.OfType<T>().FirstOrDefault();
    }
}
