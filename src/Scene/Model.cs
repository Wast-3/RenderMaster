using StbImageSharp;
using OpenTK.Mathematics;
using static System.Formats.Asn1.AsnWriter;
using OpenTK.Windowing.Common;

namespace RenderMaster;




public class Model(VertType vertType, ModelShaderType modelShaderType, string modelPath, Material material) : IModel // represents a renderable 3D model
{
    VertType vertType = vertType;
    ModelShaderType modelShaderType = modelShaderType;
    public VertexConfiguration vertexConfiguration;
    public string modelPath = modelPath;
    string? imagePath;

    public Material material = material;

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



    public Model
    {
        verts = loadVerticesFromPath(modelPath);
        vertexConfiguration = new VertColorNormalUVConfiguration(verts);
        renderer = new BasicLightingRenderer(
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



    private float[] loadVerticesFromPath(string path) // read whitespace-separated floats
    {
        List<float> vertices = new List<float>();


        using (var stream = File.OpenRead(path))
        using (var reader = new StreamReader(stream))
        {

            int totalLines = File.ReadLines(path).Count();
            int currentLine = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                currentLine++;
                string[] parts = line.Split(' ');


                if (currentLine % 5 == 0)
                {
                }


                for (int i = 0; i < parts.Length; i++)
                {
                    vertices.Add(float.Parse(parts[i]));
                }
            }
        }

        return vertices.ToArray();
    }
}


public interface IModel
{
    public void Render(FrameEventArgs args, Camera camera);
}
