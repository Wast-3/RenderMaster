using OpenTK.Mathematics;  // Provides mathematical types like Vector3 and Matrix4, essential for 3D transformations
using OpenTK.Graphics.OpenGL4;  // Provides OpenGL 4.0+ bindings for graphics programming

namespace RenderMaster
{
    // Abstract base class for handling shader programs in OpenGL.
    // This class defines the interface for loading, binding, and unbinding shaders, as well as setting uniform variables.
    public abstract class AShader
    {
        public int programID;  // Stores the ID of the shader program, which is used to manage the shader in OpenGL

        // Abstract method to load shaders from specified vertex and fragment shader paths.
        // Subclasses are responsible for implementing this method to compile shaders and link them into a shader program.
        public abstract void Load(string vertexPath, string fragmentPath);

        // Abstract method to bind the shader program for use.
        // Subclasses will implement this to make the shader program active for rendering.
        public abstract void Bind();

        // Abstract method to unbind the shader program.
        // Subclasses will implement this to deactivate the shader program after rendering.
        public abstract void Unbind();

        // Method to set a uniform Matrix4 variable in the shader.
        // This is used to pass 4x4 matrices (such as transformation matrices) to the shader program.
        public void SetUniformMatrix4(string name, Matrix4 value)
        {
            // Get the location of the uniform variable in the shader program
            int location = GL.GetUniformLocation(programID, name);

            // If the uniform location is valid, set the matrix value
            if (location != -1)
            {
                // The second parameter is 'false' meaning the matrix should not be transposed
                GL.UniformMatrix4(location, false, ref value);
            }
            else
            {
                // If the uniform is not found, log a warning message
                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }

        // Method to set a uniform Vector3 variable in the shader.
        // This is used to pass 3-component vectors (e.g., color, position) to the shader program.
        public void SetUniformVec3(string name, Vector3 value)
        {
            // Get the location of the uniform variable in the shader program
            int location = GL.GetUniformLocation(programID, name);

            // If the uniform location is valid, set the vector value
            if (location != -1)
            {
                GL.Uniform3(location, ref value);
            }
            else
            {
                // If the uniform is not found, log a warning message
                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }

        // Method to set a uniform float variable in the shader.
        // This is used to pass single float values (e.g., opacity, intensity) to the shader program.
        public void SetUniformFloat(string name, float value)
        {
            // Get the location of the uniform variable in the shader program
            int location = GL.GetUniformLocation(programID, name);

            // If the uniform location is valid, set the float value
            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
            else
            {
                // If the uniform is not found, log a warning message
                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }

        // Method to set a texture sampler (sampler2D) uniform in the shader.
        // This is used to bind a texture unit to a sampler2D uniform in the shader program.
        public void SetSampler2D(string name, TextureUnit textureUnit)
        {
            // Get the location of the sampler uniform in the shader program
            int location = GL.GetUniformLocation(programID, name);

            // Set the sampler to use the specified texture unit
            GL.Uniform1(location, 0);
        }
    }
}
