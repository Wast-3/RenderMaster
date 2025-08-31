using System.Collections.Generic;

namespace RenderMaster.src.NewGraphics.Scene
{
    class LoadedNodes
    {
        List<SceneNode> nodes = new List<SceneNode>();
        public void AddNode(SceneNode node)
        {
            nodes.Add(node);
        }
    }
}

