using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using ImGuiNET;
using System.IO; // Required for Path.Combine

namespace RenderMaster
{
    public class BasicLightingRenderer : IRenderer
    {
        private Model model;
        private BasicTexturedShader shader;
        private VertexConfiguration vertexConfiguration;
        private double timeSoFar;

        public BasicLightingRenderer(Model model, BasicTexturedShader shader)
        {
            this.model = model;
            this.shader = shader;
            this.vertexConfiguration = model.vertexConfiguration;
        }

        [MeasureExecutionTime]
        public void Render(FrameEventArgs e, Camera camera)
        {
            shader.Bind();
            vertexConfiguration.Bind();
            Matrix4 modelMatrix = model.GetModelMatrix();

            timeSoFar += e.Time;

            // Dynamic lighting effects
            Vector3 lightColor = new Vector3(
                (float)Math.Sin(timeSoFar * 0.12),
                (float)Math.Sin(timeSoFar * 0.3),
                (float)Math.Sin(timeSoFar * 0.6)
            );

            Vector3 objectColor = new Vector3(
                (float)Math.Sin(timeSoFar * 0.2),
                (float)Math.Sin(timeSoFar * 0.5),
                (float)Math.Sin(timeSoFar * 0.7)
            );

            var viewPos = camera.Position;

            shader.SetUniformMatrix4("model", modelMatrix);
            shader.SetUniformMatrix4("view", camera.View);
            shader.SetUniformMatrix4("projection", camera.Projection);
            shader.SetUniformVec3("viewPos", viewPos);

            // Using the texture cache
            var texturePath = Path.Combine(EngineConfig.TextureDirectory, "wall.jpg");
            var diffuseMapTexture = TextureCache.Instance.GetTexture(texturePath, TextureUnit.Texture0);

            diffuseMapTexture.Bind();
            shader.SetSampler2D("material.diffuse", diffuseMapTexture.TextureId);

            shader.SetUniformVec3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            shader.SetUniformFloat("material.shininess", 32.0f);
            shader.SetUniformVec3("light.position", new Vector3(1.0f));
            shader.SetUniformVec3("light.ambient", new Vector3(0.2f, 0.2f, 0.2f));
            shader.SetUniformVec3("light.diffuse", new Vector3(0.5f, 0.5f, 0.5f));
            shader.SetUniformVec3("light.specular", new Vector3(1.0f, 1.0f, 1.0f));

            GL.DrawArrays(PrimitiveType.Triangles, 0, model.verts.Length / 11); // Assuming verts array structure

            // Unbind everything
            vertexConfiguration.Unbind();
            shader.Unbind();
            diffuseMapTexture.Unbind(); // Ensure texture is also unbound
        }
    }
}
