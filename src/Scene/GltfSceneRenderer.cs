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

            //We're going to want to find each root node, then traverse its children recursively... For each child, we'll need to pull it's mesh, material, and transform, create a model
            //object, and then add it to our list of models to return.

            // We'll have to convert the verts in the mesh into something our engine can handle for now
            // We'll have to find how to take the base image from the material and load it as a texture in our engine

            // For now, instead of respecting the fact that there are potentially multiple scenes, we're just going to worry about all the listed assets.

            // Since we don't currently have well-defined parent-child relationships between models, we'll have to just translate each model to its world position, because gltfs define local transforms

            // For the RTS, for rotation, we'll have to translate the quaternion for our model class.

            // For now, if a node has no mesh, we'll skip it.

            // If multiple nodes share the same mesh and material, we'll create separate model instances for each, since they may have different transforms.

            return new List<Model>();
        }
    }
}
