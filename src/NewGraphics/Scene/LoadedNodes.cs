using System.Collections.Generic;
using System.Numerics;

namespace RenderMaster.src.NewGraphics.Scene
{
    class LoadedNodes
    {
        readonly List<Node> _roots = new();
        readonly List<Node> _all = new();

        public void AddNode(Node node)
        {
            _roots.Add(node);
            Register(node);
        }

        void Register(Node node)
        {
            _all.Add(node);
            foreach (var child in node.Children)
                Register(child);
        }

        // Flat list of every node for systems that need random access.
        public IReadOnlyList<Node> All => _all;

        // Top-level roots for hierarchy traversal.
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
    }
}
