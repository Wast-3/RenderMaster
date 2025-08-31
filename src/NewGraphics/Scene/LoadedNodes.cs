using System.Collections.Generic;

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
    }
}
