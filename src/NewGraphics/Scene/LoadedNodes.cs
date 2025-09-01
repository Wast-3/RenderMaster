using System.Collections.Generic;
using System.Numerics;

namespace RenderMaster.src.NewGraphics.Scene
{
    class LoadedNodes
    {
        List<Node> nodes = new List<Node>();
        public void AddNode(Node node)
        {
            nodes.Add(node);
        }

        // Expose a read-only view for traversal during extraction.
        public IReadOnlyList<Node> All => nodes;

        public void UpdateWorldTransforms()
        {
            foreach (var root in nodes)
                UpdateRecursive(root, Matrix4x4.Identity);
        }

        static void UpdateRecursive(Node node, Matrix4x4 parentWorld)
        {
            var tc = node.GetComponent<TransformComponent>();
            var world = parentWorld;
            if (tc != null)
            {
                world = parentWorld * tc.LocalTransform;
                tc.WorldTransform = world;
            }

            foreach (var child in node.Children)
                UpdateRecursive(child, world);
        }
    }
}
