using StbImageSharp;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using static System.Formats.Asn1.AsnWriter;
using OpenTK.Windowing.Common;
 
namespace RenderMaster
{
    public interface IRenderer
    {
        public void Render(Camera camera);
    }

    public class BasicTexturedModelRenderer : IRenderer
    {
        private Model model;
        private BasicTexturedShader shader;
        private BasicImageTexture texture;
        private VertexConfiguration vertexConfiguration;

        public BasicTexturedModelRenderer(Model model, BasicTexturedShader shader, BasicImageTexture texture)
        {
            this.model = model;
            this.shader = shader;
            this.texture = texture;
            this.vertexConfiguration = model.vertexConfiguration; // Assuming vertexConfiguration is publicly accessible

        }

        public void Render(Camera camera)
        {
            // Bind necessary components
            shader.Bind();
            texture.Bind();
            vertexConfiguration.Bind();
            Matrix4 modelMatrix = model.GetModelMatrix();
            shader.SetUniformMatrix4("model", modelMatrix);
            shader.SetUniformMatrix4("view", camera.View);
            shader.SetUniformMatrix4("projection", camera.Projection);

            // Apply transformations, if any (this could include model.position and model.rotation)

            // Render the model
            GL.DrawArrays(PrimitiveType.Triangles, 0, model.verts.Length / 8); // Divided by 8, assuming 3 for position, 3 for color, 2 for texture coordinates

            // Unbind everything
            vertexConfiguration.Unbind();
            texture.Unbind();
            shader.Unbind();
        }
    }

    public class Model : IModel
    {
        VertType vertType;
        ModelShaderType modelShaderType;
        public VertColorTextureConfiguration vertexConfiguration;
        string modelPath;
        string? imagePath;
        public float[] verts;
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; } = new Vector3(1f, 1f, 1f);
        IRenderer renderer;

        public void Render(FrameEventArgs args, Camera camera)
        {
            renderer.Render(camera);
        }

        public Model(VertType vertType, string modelPath)
        {
            //This should be a color based vert layout
            this.vertType = vertType;
            this.modelPath = modelPath;
            this.verts = loadVerticesTextureFromPath(modelPath, vertType);
            //not implemented yet, but we'll have a vertex color configuration here
        }

        public Model(VertType vertType, ModelShaderType modelShaderType, string modelPath, string imagePath)
        {
            this.vertType = vertType;
            this.modelPath = modelPath;
            this.imagePath = imagePath;
            this.verts = loadVerticesTextureFromPath(modelPath, vertType);
            this.vertexConfiguration = new VertColorTextureConfiguration(this.verts);

            this.renderer = new BasicTexturedModelRenderer(
                this,
                new BasicTexturedShader(Path.Combine(EngineConfig.ShaderDirectory, "texturedmodel.vert"), Path.Combine(EngineConfig.ShaderDirectory, "texturedmodel.frag")),
                new BasicImageTexture(imagePath)
            );
        }

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