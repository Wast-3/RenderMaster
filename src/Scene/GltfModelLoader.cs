using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using SharpGLTF.Schema2;

namespace RenderMaster;

public class GltfModelLoader : IModelLoader
{
    public float[] loadModel(string assetPath)
    {
        var ext = Path.GetExtension(assetPath).ToLowerInvariant();
        return ext switch
        {
            ".obj" => LoadObj(assetPath),
            _ => LoadGltf(assetPath)
        };
    }

    private static float[] LoadGltf(string assetPath)
    {
        var model = ModelRoot.Load(assetPath);
        var vertices = new List<float>();

        foreach (var mesh in model.LogicalMeshes)
        {
            foreach (var prim in mesh.Primitives)
            {
                var posAccessor = prim.GetVertexAccessor("POSITION");
                var normAccessor = prim.GetVertexAccessor("NORMAL");
                var uvAccessor = prim.GetVertexAccessor("TEXCOORD_0");

                IReadOnlyList<Vector3> positions = posAccessor != null ? posAccessor.AsVector3Array() : Array.Empty<Vector3>();
                IReadOnlyList<Vector3>? normals = normAccessor != null ? normAccessor.AsVector3Array() : null;
                IReadOnlyList<Vector2>? texcoords = uvAccessor != null ? uvAccessor.AsVector2Array() : null;

                var indices = prim.GetIndices()?.Select(i => (int)i) ?? Enumerable.Range(0, positions.Count);

                var color = prim.Material?.FindChannel("BaseColor")?.Color ?? new Vector4(1, 1, 1, 1);

                foreach (var idx in indices)
                {
                    var p = positions[idx];
                    var n = normals != null && idx < normals.Count ? normals[idx] : new Vector3(0, 0, 1);
                    var uv = texcoords != null && idx < texcoords.Count ? texcoords[idx] : Vector2.Zero;

                    vertices.Add(p.X);
                    vertices.Add(p.Y);
                    vertices.Add(p.Z);

                    vertices.Add(color.X);
                    vertices.Add(color.Y);
                    vertices.Add(color.Z);

                    vertices.Add(n.X);
                    vertices.Add(n.Y);
                    vertices.Add(n.Z);

                    vertices.Add(uv.X);
                    vertices.Add(uv.Y);
                }
            }
        }

        return vertices.ToArray();
    }

    private static float[] LoadObj(string assetPath)
    {
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var texcoords = new List<Vector2>();
        var faces = new List<(int v, int vt, int vn)[]>();
        var culture = CultureInfo.InvariantCulture;

        foreach (var line in File.ReadLines(assetPath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0])
            {
                case "v":
                    positions.Add(new Vector3(
                        float.Parse(parts[1], culture),
                        float.Parse(parts[2], culture),
                        float.Parse(parts[3], culture)));
                    break;
                case "vn":
                    normals.Add(new Vector3(
                        float.Parse(parts[1], culture),
                        float.Parse(parts[2], culture),
                        float.Parse(parts[3], culture)));
                    break;
                case "vt":
                    texcoords.Add(new Vector2(
                        float.Parse(parts[1], culture),
                        float.Parse(parts[2], culture)));
                    break;
                case "f":
                    var verts = new List<(int, int, int)>();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var comps = parts[i].Split('/');
                        int v = int.Parse(comps[0]) - 1;
                        int vt = comps.Length > 1 && comps[1].Length > 0 ? int.Parse(comps[1]) - 1 : -1;
                        int vn = comps.Length > 2 && comps[2].Length > 0 ? int.Parse(comps[2]) - 1 : -1;
                        verts.Add((v, vt, vn));
                    }
                    for (int i = 1; i + 1 < verts.Count; i++)
                    {
                        faces.Add(new[] { verts[0], verts[i], verts[i + 1] });
                    }
                    break;
            }
        }

        var result = new List<float>();
        var color = new Vector3(1, 1, 1);

        foreach (var tri in faces)
        {
            foreach (var (v, vt, vn) in tri)
            {
                var p = positions[v];
                var n = vn >= 0 ? normals[vn] : new Vector3(0, 0, 1);
                var uv = vt >= 0 ? texcoords[vt] : Vector2.Zero;

                result.Add(p.X);
                result.Add(p.Y);
                result.Add(p.Z);

                result.Add(color.X);
                result.Add(color.Y);
                result.Add(color.Z);

                result.Add(n.X);
                result.Add(n.Y);
                result.Add(n.Z);

                result.Add(uv.X);
                result.Add(uv.Y);
            }
        }

        return result.ToArray();
    }
}

