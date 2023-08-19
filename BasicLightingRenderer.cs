using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using ImGuiNET;

namespace RenderMaster
{
    public class BasicLightingRenderer : IRenderer
    {
        private Model model;
        private BasicTexturedShader shader;
        private VertexConfiguration vertexConfiguration;
        private Vector3 currentLightColor;
        private Vector3 currentObjectColor;
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
            
            timeSoFar = timeSoFar + e.Time;

            Vector3 lightColor = new Vector3(
                (float)Math.Sin(timeSoFar * 0.12),
                (float)Math.Sin(timeSoFar * 0.3),
                (float)Math.Sin(timeSoFar * 0.6)
            );

            shader.SetUniformMatrix4("model", modelMatrix);
            shader.SetUniformMatrix4("view", camera.View);
            shader.SetUniformMatrix4("projection", camera.Projection);
            shader.SetUniformVec3("lightColor", new Vector3(1,1,1));
            shader.SetUniformVec3("lightPos", new Vector3(0, 1, 0));
            shader.SetUniformVec3("objectColor", lightColor);

            

            GL.DrawArrays(PrimitiveType.Triangles, 0, model.verts.Length / 9); // Divided by 8, assuming 3 for position, 3 for color, 2 for texture coordinates

            // Unbind everything
            vertexConfiguration.Unbind();
            shader.Unbind();
        }


    }
}