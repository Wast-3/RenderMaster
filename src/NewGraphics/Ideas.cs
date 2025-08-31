using BepuPhysics.Collidables;
using OpenTK.Windowing.Common.Input;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
                var prepared = new PreparedMeshBuffer(mesh);
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

    // A contiguous index range you can draw with one material (one glTF primitive)
    public readonly struct SubmeshSpan
    {
        public readonly int IndexStart;   // into the global index buffer (in elements, not bytes)
        public readonly int IndexCount;   // number of indices to draw
        public SubmeshSpan(int start, int count) { IndexStart = start; IndexCount = count; }
    }

    class PreparedMeshBuffer
    {
        // Interleaved layout: P(3) N(3) T(3)+handedness(1) UV(2) => 12 floats
        public const int FloatsPerVertex = 12;
        public int VertexStrideBytes => sizeof(float) * FloatsPerVertex;

        public float[] Vertices { get; private set; } = Array.Empty<float>();

        // Raw index bytes + the element width so GL can pick UnsignedByte/UnsignedShort/UnsignedInt.
        // (Keeping it as bytes avoids boxing/generics when uploading to GL.)
        public byte[] Indices { get; private set; } = Array.Empty<byte>();
        public int IndexElementSize { get; private set; } = 0; // 0 == non-indexed, else 1/2/4 bytes

        public int VertexCount { get; private set; }
        public int IndexCount { get; private set; }

        // Triangles only for now (glTF allows other topologies, but you already guard for TRIANGLES)
        public PrimitiveType Primitive { get; private set; } = PrimitiveType.TRIANGLES;

        // For binding correct materials per-primitive without duplicating verts
        public IReadOnlyList<SubmeshSpan> Submeshes => _submeshes;
        private readonly List<SubmeshSpan> _submeshes = new();

        public PreparedMeshBuffer(SharpGLTF.Schema2.Mesh mesh)
        {
            var verts = new List<float>(mesh.Primitives.Sum(p => (p.GetVertexAccessor("POSITION")?.Count ?? 0)) * FloatsPerVertex);
            var idx32 = new List<uint>(mesh.Primitives.Sum(p => p.GetIndices()?.Count ?? (p.GetVertexAccessor("POSITION")?.Count ?? 0)));

            uint baseVertex = 0;

            foreach (var prim in mesh.Primitives)
            {
                if (prim.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                    throw new NotSupportedException("Only triangle primitives are supported.");

                // Accessors (POSITION is required by glTF for a renderable primitive)
                var posAcc = prim.GetVertexAccessor("POSITION") ?? throw new Exception("POSITION missing");
                var norAcc = prim.GetVertexAccessor("NORMAL");
                var tanAcc = prim.GetVertexAccessor("TANGENT");
                var uv0Acc = prim.GetVertexAccessor("TEXCOORD_0");
                var idxAcc = prim.IndexAccessor;

                var positions = posAcc.AsVector3Array(); // IAccessorArray<Vector3>
                IReadOnlyList<Vector3>? normals = norAcc != null ? norAcc.AsVector3Array() : null;
                IReadOnlyList<Vector4>? tangents = tanAcc != null ? tanAcc.AsVector4Array() : null;
                IReadOnlyList<Vector2>? uvs = uv0Acc != null ? uv0Acc.AsVector2Array() : null;

                // Build fallback attributes if missing
                if (normals == null) normals = Enumerable.Repeat(new Vector3(0, 0, 1), positions.Count).ToArray(); // sane default
                if (uvs == null) uvs = Enumerable.Repeat(new Vector2(0, 0), positions.Count).ToArray();
                if (tangents == null) tangents = GenerateTangents(positions, normals, uvs, idxAcc != null ? prim.GetIndices().Select(i => (int)i).ToList()
                                                                                                         : Enumerable.Range(0, positions.Count).ToList());

                // Sanity: all attribute arrays must match vertex count
                if (positions.Count != normals.Count || positions.Count != tangents.Count || positions.Count != uvs.Count)
                    throw new Exception("Attribute arrays have mismatched lengths.");

                // Interleave
                for (int i = 0; i < positions.Count; i++)
                {
                    var p = positions[i];
                    var n = normals[i];
                    var t4 = tangents[i];
                    var uv = uvs[i];

                    verts.Add(p.X); verts.Add(p.Y); verts.Add(p.Z);
                    verts.Add(n.X); verts.Add(n.Y); verts.Add(n.Z);
                    verts.Add(t4.X); verts.Add(t4.Y); verts.Add(t4.Z);
                    verts.Add(t4.W); // handedness
                    verts.Add(uv.X); verts.Add(uv.Y);
                }

                // Indices (offset by baseVertex). If none provided, synthesize 0..N-1
                var start = idx32.Count;
                if (idxAcc != null)
                {
                    var src = prim.GetIndices(); // IList<uint>
                    for (int i = 0; i < src.Count; i++)
                        idx32.Add(src[i] + baseVertex);
                }
                else
                {
                    for (uint i = 0; i < (uint)positions.Count; i++)
                        idx32.Add(i + baseVertex);
                }

                var count = idx32.Count - start;
                _submeshes.Add(new SubmeshSpan(start, count));

                baseVertex += (uint)positions.Count;
            }

            // Finalize
            Vertices = verts.ToArray();
            VertexCount = Vertices.Length / FloatsPerVertex;

            // Choose tightest index width; pack into byte[] once
            IndexCount = idx32.Count;
            if (IndexCount == 0)
            {
                Indices = Array.Empty<byte>();
                IndexElementSize = 0;
            }
            else
            {
                uint maxIndex = 0;
                for (int i = 0; i < idx32.Count; i++) if (idx32[i] > maxIndex) maxIndex = idx32[i];

                if (maxIndex <= byte.MaxValue)
                {
                    IndexElementSize = 1;
                    Indices = new byte[IndexCount];
                    for (int i = 0; i < IndexCount; i++) Indices[i] = (byte)idx32[i];
                }
                else if (maxIndex <= ushort.MaxValue)
                {
                    IndexElementSize = 2;
                    Indices = new byte[IndexCount * 2];
                    var span = Indices.AsSpan();
                    for (int i = 0; i < IndexCount; i++)
                        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(i * 2, 2), (ushort)idx32[i]);
                }
                else
                {
                    IndexElementSize = 4;
                    Indices = new byte[IndexCount * 4];
                    var span = Indices.AsSpan();
                    for (int i = 0; i < IndexCount; i++)
                        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(i * 4, 4), idx32[i]);
                }
            }

            Primitive = PrimitiveType.TRIANGLES;
        }

        // MikkTSpace-ish tangent build (good enough for most cases; relies on UV0)
        private static IReadOnlyList<Vector4> GenerateTangents(
            IAccessorArray<Vector3> positions,
            IReadOnlyList<Vector3> normals,
            IReadOnlyList<Vector2> uvs,
            List<int> idx)
        {
            var tan1 = new Vector3[positions.Count];
            var tan2 = new Vector3[positions.Count];

            for (int t = 0; t + 2 < idx.Count; t += 3)
            {
                int i0 = idx[t], i1 = idx[t + 1], i2 = idx[t + 2];

                var p0 = positions[i0]; var p1 = positions[i1]; var p2 = positions[i2];
                var w0 = uvs[i0]; var w1 = uvs[i1]; var w2 = uvs[i2];

                var e1 = p1 - p0; var e2 = p2 - p0;
                float du1 = w1.X - w0.X, dv1 = w1.Y - w0.Y;
                float du2 = w2.X - w0.X, dv2 = w2.Y - w0.Y;
                float r = du1 * dv2 - du2 * dv1;
                if (Math.Abs(r) < 1e-8f) continue;
                r = 1.0f / r;

                var sdir = (e1 * dv2 - e2 * dv1) * r;
                var tdir = (e2 * du1 - e1 * du2) * r;

                tan1[i0] += sdir; tan1[i1] += sdir; tan1[i2] += sdir;
                tan2[i0] += tdir; tan2[i1] += tdir; tan2[i2] += tdir;
            }

            var outT = new Vector4[positions.Count];
            for (int i = 0; i < positions.Count; i++)
            {
                var n = normals[i];
                var t = tan1[i];

                var tOrtho = Vector3.Normalize(t - n * Vector3.Dot(n, t)); // Gram-Schmidt
                float w = Vector3.Dot(Vector3.Cross(n, tOrtho), tan2[i]) < 0.0f ? -1.0f : 1.0f;
                outT[i] = new Vector4(tOrtho, w);
            }
            return outT;
        }
    }

}

    
