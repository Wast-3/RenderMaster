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

    class NameComponent : INodeComponent
    {
        public string Name { get; }
        public NameComponent(string name) => Name = string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name;
    }

    class TransformComponent : INodeComponent
    {
        public Matrix4x4 LocalTransform { get; set; }
        public Matrix4x4 WorldTransform { get; set; }

        public TransformComponent(Matrix4x4 localTransform)
        {
            LocalTransform = localTransform;
            WorldTransform = localTransform;
        }
    }

    class MeshComponent : INodeComponent
    {
        public MeshHandle Mesh { get; }
        public MaterialHandle Material { get; set; }
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
        readonly List<Node> _children = new();

        public Node? Parent { get; private set; }
        public IReadOnlyList<Node> Children => _children;

        public void AddComponent(INodeComponent comp) => _components.Add(comp);

        public void AddChild(Node child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        public IEnumerable<T> GetComponents<T>() where T : class, INodeComponent
            => _components.OfType<T>();

        public T? GetComponent<T>() where T : class, INodeComponent
            => _components.OfType<T>().FirstOrDefault();
    }
}
