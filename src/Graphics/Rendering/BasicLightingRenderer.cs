using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using ImGuiNET;
using System.IO;
using RenderMaster.Engine;

namespace RenderMaster;



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

        Vector3 lightColor = new Vector3(
            0.3f, 0.3f, 0.3f
        );


        var viewPos = camera.Position;






        camera.UpdateViewMatrix();


        shader.SetUniformMatrix4("model", modelMatrix);
        shader.SetUniformMatrix4("view", camera.View);
        shader.SetUniformMatrix4("projection", camera.Projection);


        shader.SetUniformVec3("viewPos", viewPos);


        var diffuseMapTexture = model.material.Diffuse;
        var specularMapTexture = model.material.Specular;

        model.material.BindAllTextures();

        var diffuseUnit = (TextureUnit)((int)TextureUnit.Texture0 + diffuseMapTexture.BoundUnit.GetValueOrDefault());
        var specularUnit = (TextureUnit)((int)TextureUnit.Texture0 + specularMapTexture.BoundUnit.GetValueOrDefault());

        shader.SetSampler2D("material.diffuse", diffuseUnit);
        shader.SetSampler2D("material.specular", specularUnit);


        Logger.Log("Inside Renderer: Current texture unit: " + diffuseMapTexture.BoundUnit + " Current Model: " + model.modelPath, LogLevel.Info);
        Logger.Log("Current Model: " + model.modelPath, LogLevel.Info);


        shader.SetUniformVec3("material.specularTint", new Vector3(0.5f, 0.5f, 0.5f));
        shader.SetUniformFloat("material.shininess", 32.0f);


        shader.SetUniformVec3("light.direction", new Vector3(0.1f, -1.0f, 0.0f));
        shader.SetUniformVec3("light.ambient", lightColor);
        shader.SetUniformVec3("light.diffuse", lightColor);
        shader.SetUniformVec3("light.specular", new Vector3(1.0f, 1.0f, 1.0f));


        GL.DrawArrays(PrimitiveType.Triangles, 0, model.verts.Length / 11);


        vertexConfiguration.Unbind();
        shader.Unbind();
    }
}
