using StbImageSharp;
using OpenTK.Mathematics;
using static System.Formats.Asn1.AsnWriter;
using OpenTK.Windowing.Common;

namespace RenderMaster
{
    public interface IRenderer
    {
        public void Render(FrameEventArgs e, Camera camera);
    }

    public class Model : IModel
    {
        VertType vertType;
        ModelShaderType modelShaderType;
        public VertexConfiguration vertexConfiguration;
        string modelPath;
        string? imagePath;
        public float[] verts;
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; } = new Vector3(1f, 1f, 1f);
        IRenderer renderer;

        [MeasureExecutionTime]
        public void Render(FrameEventArgs args, Camera camera)
        {
            renderer.Render(args, camera);
        }

        public Model(VertType vertType, ModelShaderType modelShaderType, string modelPath)
        {
            //This should be a color based vert layout
            this.vertType = vertType;
            this.modelPath = modelPath;
            this.verts = loadVerticesTextureFromPath(modelPath, vertType);
            //not implemented yet, but we'll have a vertex color configuration here
            this.vertexConfiguration = new VertColorNormalConfiguration(this.verts);
            this.modelShaderType = modelShaderType;
            this.renderer = new BasicLightingRenderer(
                this,
                new BasicTexturedShader(Path.Combine(EngineConfig.ShaderDirectory, "lightingtest.vert"), Path.Combine(EngineConfig.ShaderDirectory, "lightingtest.frag"))
                );
        }

        public Model(VertType vertType, ModelShaderType modelShaderType, string modelPath, string imagePath)
        {
            this.vertType = vertType;
            this.modelPath = modelPath;
            this.imagePath = imagePath;
            this.verts = loadVerticesTextureFromPath(modelPath, vertType);
            this.vertexConfiguration = new VertColorTextureConfiguration(this.verts);

            if (modelShaderType == ModelShaderType.BasicTextured)
            {
                this.renderer = new BasicTexturedModelRenderer(
                    this,
                    new BasicTexturedShader(Path.Combine(EngineConfig.ShaderDirectory, "texturedmodel.vert"), Path.Combine(EngineConfig.ShaderDirectory, "texturedmodel.frag")),
                    new BasicImageTexture(imagePath)
                );
            }

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

        private float[] loadVerticesTextureFromPath(string path, VertType vertType)
        {
            List<float> vertices = new List<float>();

            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(' ');

                    Console.WriteLine("Parsing... Counted " + parts.Length + "vert parts");

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
}