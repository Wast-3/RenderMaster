using OpenTK.Mathematics;  // Provides mathematical types like Vector3 and Matrix4, essential for 3D transformations
using OpenTK.Graphics.OpenGL4;  // Provides OpenGL 4.0+ bindings for graphics programming
using OpenTK.Windowing.Common;  // Provides common types used in OpenTK windowing, such as FrameEventArgs
using ImGuiNET;  // Provides bindings for ImGui, a graphical user interface library often used for debug tools in games
using System.IO;  // Required for Path.Combine, useful for constructing file paths

namespace RenderMaster
{
    // The BasicLightingRenderer class is responsible for rendering a 3D model with lighting effects using a specific shader.
    // It implements the IRenderer interface, meaning it must provide a Render method.
    public class BasicLightingRenderer : IRenderer
    {
        private Model model;  // The model to be rendered
        private BasicTexturedShader shader;  // The shader program used to render the model
        private VertexConfiguration vertexConfiguration;  // The vertex configuration used to set up vertex attributes
        private double timeSoFar;  // Keeps track of the elapsed time for dynamic effects

        // Constructor that initializes the renderer with a model and a shader.
        // It sets up the necessary references to the model's vertex configuration and the shader program.
        public BasicLightingRenderer(Model model, BasicTexturedShader shader)
        {
            this.model = model;  // Store the model to be rendered
            this.shader = shader;  // Store the shader program
            this.vertexConfiguration = model.vertexConfiguration;  // Retrieve the vertex configuration from the model
        }

        // Implementation of the Render method from the IRenderer interface.
        // This method handles the entire rendering process, including setting up shaders, binding resources, and drawing the model.
        [MeasureExecutionTime]
        public void Render(FrameEventArgs e, Camera camera)
        {
            shader.Bind();  // Bind the shader program for rendering
            vertexConfiguration.Bind();  // Bind the vertex configuration (VAO/VBO)

            // Calculate the model transformation matrix based on position, rotation, and scale
            Matrix4 modelMatrix = model.GetModelMatrix();

            // Update the elapsed time for dynamic lighting effects
            timeSoFar += e.Time;

            // Calculate dynamic light color based on time
            Vector3 lightColor = new Vector3(
                (float)Math.Sin(timeSoFar * 10.12),
                (float)Math.Sin(timeSoFar * 5.3),
                (float)Math.Sin(timeSoFar * 3.6)
            );

            // Calculate dynamic object color based on time
            Vector3 objectColor = new Vector3(
                (float)Math.Sin(timeSoFar * 3.2) * (float)Math.Tan(timeSoFar),
                (float)Math.Sin(timeSoFar * 2.5),
                (float)Math.Sin(timeSoFar * 1.7)
            );

            // Get the camera's position in the scene
            var viewPos = camera.Position;

            // Spin the camera around the model by updating it's position over time
            /*camera.Position = new Vector3(
                (float)Math.Sin(timeSoFar * 5.5) * 3 * (float)Math.Tan(timeSoFar) * (float)Math.Cos(timeSoFar * 7) * 2 + (float)Math.Sin(timeSoFar * 100) * 2,
                (float)Math.Sin(timeSoFar * 5.5) * 3 * (float)Math.Tan(timeSoFar) * (float)Math.Cos(timeSoFar) * 2 * (float)Math.Tan(timeSoFar * 3),
                (float)Math.Cos(timeSoFar * 5.5) * 3 * (float)Math.Cos(timeSoFar) * (float)Math.Tan(timeSoFar) * (float)Math.Cos(timeSoFar) * 3
            );*/

            /*model.Scale = new Vector3(2f * (float)Math.Sin(timeSoFar * 2), 2f * (float)Math.Sin(timeSoFar * 2), 2f * (float)Math.Sin(timeSoFar * 2));*/

            camera.UpdateViewMatrix();  // Update the camera's view matrix

            // Set the model, view, and projection matrices in the shader
            shader.SetUniformMatrix4("model", modelMatrix);
            shader.SetUniformMatrix4("view", camera.View);
            shader.SetUniformMatrix4("projection", camera.Projection);

            // Pass the camera's position to the shader (useful for specular lighting calculations)
            shader.SetUniformVec3("viewPos", viewPos);

            // Load and bind the diffuse texture using a texture cache for efficiency
            var texturePath = Path.Combine(EngineConfig.TextureDirectory, "uv_check2.png");
            var diffuseMapTexture = TextureCache.Instance.GetTexture(texturePath);

            var specularTexturePath = Path.Combine(EngineConfig.TextureDirectory, "uv_check2.png");
            var specularMapTexture = TextureCache.Instance.GetTexture(specularTexturePath);

            specularMapTexture.Bind();
            diffuseMapTexture.Bind();  // Bind the texture to a texture unit
            shader.SetSampler2D("material.diffuse", diffuseMapTexture.textureUnit);  // Set the diffuse texture sampler in the shader
            shader.SetSampler2D("material.specular", specularMapTexture.textureUnit);  // Set the specular texture sampler in the shader

            // Set additional material properties in the shader
            shader.SetUniformVec3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));  // Specular color
            shader.SetUniformFloat("material.shininess", 32.0f);  // Shininess factor for specular reflection

            // Set light properties in the shader
            shader.SetUniformVec3("light.direction", new Vector3(0.1f, -1.0f, 0.0f));  // Light position
            shader.SetUniformVec3("light.ambient", objectColor);  // Ambient light color
            shader.SetUniformVec3("light.diffuse", lightColor);  // Diffuse light color
            shader.SetUniformVec3("light.specular", new Vector3(1.0f, 1.0f, 1.0f));  // Specular light color

            // Create random positions for multiple models
            List<Vector3> randomPositions = new List<Vector3>();

            // Loop and add random positions that don't change over time
            for (int i = 0; i < 20; i++)
            {
                randomPositions.Add(new Vector3(
                    (float)(Math.Sin(i * 10.0) * 5.0),
                    (float)(Math.Cos(i * 10.0) * 5.0),
                    (float)(Math.Sin(i * 10.0) * 5.0)
                ));
            }

            //Loop through, rendering the 10 models in our randomPositions list
            for (int i = 0; i < 20; i++)
            {
                // Set the model's position to the random position
                model.Position = randomPositions[i];

                // calculate the new model matrix
                modelMatrix = model.GetModelMatrix();


                // Set the model, view, and projection matrices in the shader
                shader.SetUniformMatrix4("model", modelMatrix);

                // Draw the model using the shader
                GL.DrawArrays(PrimitiveType.Triangles, 0, model.verts.Length / 11);
            }

            // Unbind the resources after rendering to avoid affecting subsequent rendering operations
            vertexConfiguration.Unbind();  // Unbind the vertex configuration (VAO/VBO)
            shader.Unbind();  // Unbind the shader program
            diffuseMapTexture.Unbind();  // Unbind the texture
        }
    }
}
