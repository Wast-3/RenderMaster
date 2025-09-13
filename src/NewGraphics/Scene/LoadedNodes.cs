using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("RenderMaster.src.ControlPlane")]
namespace RenderMaster.src.NewGraphics.Scene
{
    readonly record struct LightEntry(LightComponent Light, Matrix4x4 World);

    internal class LoadedNodes
    {
        readonly List<Node> _roots = new();
        readonly List<Node> _all = new();

        public void AddNode(Node node)
        {
            _roots.Add(node);
            Register(node);
        }

        public void RemoveNode(Node node)
        {
            if (_roots.Remove(node))
                Unregister(node);
        }

        void Register(Node node)
        {
            _all.Add(node);
            foreach (var child in node.Children)
                Register(child);
        }

        void Unregister(Node node)
        {
            _all.Remove(node);
            foreach (var child in node.Children)
                Unregister(child);
        }

        // Flat list of every node for systems that need random access.
        public IReadOnlyList<Node> All => _all;

        // Only the top-level nodes. Begin traversals here to avoid duplicates.
        public IReadOnlyList<Node> Roots => _roots;

        public void UpdateWorldTransforms()
        {
            foreach (var root in _roots)
                UpdateRecursive(root, Matrix4x4.Identity);
        }

        static void UpdateRecursive(Node node, Matrix4x4 parentWorld)
        {
            var tc = node.GetComponent<TransformComponent>();
            var world = parentWorld;
            if (tc != null)
            {
                // In System.Numerics (row-major), apply local then parent.
                world = tc.LocalTransform * parentWorld;
                tc.WorldTransform = world;
            }

            foreach (var child in node.Children)
                UpdateRecursive(child, world);
        }

        public List<LightEntry> GatherLights()
        {
            List<LightEntry> lights = new();
            foreach (var node in _all)
            {
                var lc = node.GetComponent<LightComponent>();
                if (lc != null)
                {
                    var tc = node.GetComponent<TransformComponent>();
                    var world = tc?.WorldTransform ?? Matrix4x4.Identity;
                    lights.Add(new LightEntry(lc, world));
                }
            }
            return lights;
        }
    }
}
