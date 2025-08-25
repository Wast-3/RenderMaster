using RenderMaster.Engine;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster.src.Scene
{
    class GltfSceneRenderer : ISceneLoader
    {
        public List<Model> LoadScene(string assetPath)
        {
            var ext = System.IO.Path.GetExtension(assetPath).ToLowerInvariant();
            var modelRoot = ModelRoot.Load(assetPath);

            Logger.Log($"Loaded GLTF model: {assetPath} with {modelRoot.LogicalMeshes.Count} meshes, {modelRoot.LogicalMaterials.Count} materials, and {modelRoot.LogicalNodes.Count} nodes", LogLevel.Info);

            return new List<Model>();
        }
    }
}
