using StbImageSharp;
using OpenTK.Mathematics;
using static System.Formats.Asn1.AsnWriter;
using OpenTK.Windowing.Common;
using BepuPhysics;

namespace RenderMaster;




public class Model : IModel // represents a renderable 3D model
{
    VertType vertType;
    ModelShaderType modelShaderType;
    public VertexConfiguration vertexConfiguration;
    public string modelPath;
    string? imagePath;

    public Material material;

    public float[] verts;
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scale { get; set; } = new Vector3(1f, 1f, 1f);
    IRenderer renderer;

    [MeasureExecutionTime]
    public void Render(FrameEventArgs args, Camera camera)
    {
        renderer.Render(args, camera); // delegate rendering to configured renderer
    }

    public Model(VertType vertType, ModelShaderType modelShaderType, string modelPath, Material material, int? physicsPreset = null)
    {
        this.vertType = vertType;
        this.modelPath = modelPath;
        IModelLoader loader;
        string ext = Path.GetExtension(modelPath).ToLowerInvariant();
        if (ext == ".gltf" || ext == ".glb" || ext == ".obj")
        {
            loader = new GltfModelLoader();
        }
        else
        {
            loader = new LegacyModelLoader();
        }
        this.verts = loader.loadModel(modelPath);
        this.material = material;

        this.vertexConfiguration = new VertColorNormalUVConfiguration(this.verts);

        this.modelShaderType = modelShaderType;

        this.renderer = new BasicLightingRenderer(
            this,
            new BasicTexturedShader(Path.Combine(EngineConfig.ShaderDirectory, "material_based_lighting.vert"), Path.Combine(EngineConfig.ShaderDirectory, "material_based_lighting.frag"))
            );
    }

    [MeasureExecutionTime]
    public Matrix4 GetModelMatrix()
    {
        Matrix4 model = Matrix4.Identity;
        model *= Matrix4.CreateScale(Scale);
        model *= Matrix4.CreateRotationX(Rotation.X);
        model *= Matrix4.CreateRotationY(Rotation.Y);
        model *= Matrix4.CreateRotationZ(Rotation.Z);
        model *= Matrix4.CreateTranslation(Position);
        return model;
    }


}

public interface IModel
{
    public void Render(FrameEventArgs args, Camera camera);
}
