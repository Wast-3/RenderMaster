using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SharpGLTF.Schema2;

namespace RenderMaster;

/// <summary>
/// Loads a glTF file and creates <see cref="Model"/> instances that can be added to a <see cref="Scene"/>.
/// This class focuses on the subset of the glTF spec needed by the engine: meshes with
/// POSITION, NORMAL and TEXCOORD_0 vertex attributes and a single base color texture.
/// </summary>
public class GltfSceneRenderer
{
    private readonly List<Model> _models = new();

    /// <summary>Gets the models created after a call to <see cref="Load"/>.</summary>
    public IReadOnlyList<Model> Models => _models;

    /// <summary>
    /// Loads all meshes from a glTF asset and converts them into engine <see cref="Model"/> objects.
    /// </summary>
    /// <param name="assetPath">Absolute path to the glTF file.</param>
    public void Load(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException("Asset path must be valid", nameof(assetPath));
        }

        var modelRoot = ModelRoot.Load(assetPath);
        var baseDirectory = Path.GetDirectoryName(assetPath)!;

        foreach (var node in modelRoot.LogicalNodes)
        {
            if (node.Mesh == null) continue;

            // SharpGLTF nodes expose transformation through matrices. Decompose the
            // world matrix so we can extract translation, rotation and scale values
            // for use with our engine's model representation.
            var world = node.WorldMatrix;
            Matrix4x4.Decompose(world, out var scale, out var rotation, out var translation);

            foreach (var prim in node.Mesh.Primitives)
            {
                var verts = BuildVertices(prim);
                var material = BuildMaterial(prim.Material, baseDirectory);

                // create model and override the vertex data with the primitive specific vertices
                var model = new Model(VertType.VertColorNormal, ModelShaderType.BasicTextured, assetPath, material)
                {
                    Position = translation,
                    Rotation = QuaternionToEuler(rotation),
                    Scale = scale,
                    verts = verts,
                    vertexConfiguration = new VertColorNormalUVConfiguration(verts)
                };

                _models.Add(model);
            }
        }
    }

    private static float[] BuildVertices(MeshPrimitive prim)
    {
        var vertices = new List<float>();

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

        return vertices.ToArray();
    }

    private static Material BuildMaterial(SharpGLTF.Schema2.Material? gltfMaterial, string baseDirectory)
    {
        var channel = gltfMaterial?.FindChannel("BaseColor");
        var texPath = channel?.Texture?.PrimaryImage?.Uri;
        BasicImageTexture diffuseTexture;

        if (!string.IsNullOrEmpty(texPath))
        {
            var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, texPath));
            diffuseTexture = TextureCache.Instance.GetTexture(fullPath);
        }
        else
        {
            var fallback = Path.Combine(EngineConfig.TextureDirectory, "uv_check2.png");
            diffuseTexture = TextureCache.Instance.GetTexture(fallback);
        }

        // Use the same texture for specular if none is provided
        var specularTexture = diffuseTexture;
        return new Material(diffuseTexture, specularTexture);
    }

    private static Vector3 QuaternionToEuler(Quaternion q)
    {
        // Convert quaternion to Euler angles (in radians)
        var sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        var cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        var roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        var sinp = 2 * (q.W * q.Y - q.Z * q.X);
        float pitch;
        if (MathF.Abs(sinp) >= 1)
            pitch = MathF.CopySign(MathF.PI / 2, sinp); // use 90 degrees if out of range
        else
            pitch = MathF.Asin(sinp);

        var siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        var yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        return new Vector3(roll, pitch, yaw);
    }
}

