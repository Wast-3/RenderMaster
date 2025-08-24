using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;

namespace RenderMaster;

public class BasicTexturedModelRenderer(Model model, BasicTexturedShader shader, BasicImageTexture texture) : IRenderer
{
    private Model model = model;
    private BasicTexturedShader shader = shader;
    private BasicImageTexture texture = texture;
    private VertexConfiguration vertexConfiguration = model.vertexConfiguration;

    [MeasureExecutionTime]
    public void Render(FrameEventArgs e, Camera camera)
    {

        shader.Bind();
        texture.Bind();
        vertexConfiguration.Bind();
        Matrix4 modelMatrix = model.GetModelMatrix();
        shader.SetUniformMatrix4("model", modelMatrix);
        shader.SetUniformMatrix4("view", camera.View);
        shader.SetUniformMatrix4("projection", camera.Projection);




        GL.DrawArrays(PrimitiveType.Triangles, 0, model.verts.Length / 8);


        vertexConfiguration.Unbind();
        texture.Unbind();
        shader.Unbind();
    }
}
