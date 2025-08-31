using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common.Input;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace RenderMaster.src.NewGraphics
{
    using MeshHandle = Handle<PreparedMeshBuffer>;
    using TextureHandle = Handle<TextureGPU>;
    using MaterialHandle = Handle<MaterialGPU>;


    readonly record struct Handle<T>(int Id);

    class CPUResourceTable
    {
        List<PreparedMeshBuffer> meshBuffers = new List<PreparedMeshBuffer>();

        public MeshHandle AddMeshBuffer(PreparedMeshBuffer buffer)
        {
            meshBuffers.Add(buffer);
            return new MeshHandle(meshBuffers.Count - 1);
        }
    }

    class LoadedNodes
    {
        List<SceneNode> nodes = new List<SceneNode>();
        public void AddNode(SceneNode node)
        {
            nodes.Add(node);
        }
    }

    class SceneNode
    {
        public required MeshHandle mesh { get; init; }
        public required MaterialHandle material { get; init; }
    }

    interface ILoadsResourceTable
    {
        CPUResourceTable ResourceTable { get; }

        void LoadResources(CPUResourceTable table);
    }

    class LoadFromGltfFile : ILoadsResourceTable
    {
        public string filepath { get; init; }
        public CPUResourceTable ResourceTable { get; } = new CPUResourceTable();

        public void LoadResources(CPUResourceTable table)
        {
            var model = ModelRoot.Load(filepath);
            //create node graph

            foreach (var mesh in model.LogicalMeshes)
            {
                // convert to prepared mesh buffer
                var prepared = PreparedMeshBuffer(mesh);
                var handle = table.AddMeshBuffer(prepared);

                // create scene node if it doesn't exist with references to loaded materials
                var node = new SceneNode
                {
                    mesh = handle,
                    material = new MaterialHandle(0) // placeholder
                };
            }
        }
    }

    class PreparedMeshBuffer
    {
        public required SharpGLTF.Schema2.Mesh mesh { get; init; }
        public float[] Vertices { get; private set; } = Array.Empty<float>();
        public byte[] Indices { get; private set; } = Array.Empty<byte>();
        public PrimitiveType Primitive { get; private set; }
        public int VertexStrideBytes => sizeof(float) * 12;
        public int VertexCount { get; private set; }
        public int IndexCount { get; private set; }

        public PreparedMeshBuffer(SharpGLTF.Schema2.Mesh mesh)
        {
            foreach (var prim in mesh.Primitives)
            {
                if (prim.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                {
                    throw new NotSupportedException("Only triangle meshes are supported");
                }

                // get accessors for positions, normals, texcoords, indices, etc

                var positionAccessor = prim.GetVertexAccessor("POSITION");
                var normalAccessor = prim.GetVertexAccessor("NORMAL");
                var tangentAccessor = prim.GetVertexAccessor("TANGENT");
                var texcoordAccessor = prim.GetVertexAccessor("TEXCOORD_0");
                var indexAccessor = prim.IndexAccessor;

                if (positionAccessor == null || normalAccessor == null || texcoordAccessor == null || indexAccessor == null)
                {
                    throw new Exception($"Mesh is missing required attributes: {positionAccessor} {normalAccessor} {texcoordAccessor} {indexAccessor} ");
                }

                var positions = positionAccessor.AsVector3Array();

                IReadOnlyList<Vector3>? normals = normalAccessor != null ? nrmA.AsVector3Array() : null;
                IReadOnlyList<Vector4>? tangents = tangentAccessor != null ? tangentAccessor.AsVector4Array() : null;
                IReadOnlyList<Vector2>? uvs = uvA != null ? uvA.AsVector2Array() : null;

            }
        }
    }

}

    
