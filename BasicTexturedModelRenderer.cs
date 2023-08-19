using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;

namespace RenderMaster
{
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

        [MeasureExecutionTime]
        public void Render(FrameEventArgs e, Camera camera)
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
}