using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace RenderMaster
{
    public abstract class AShader
    {
        public int programID; // This will hold the shader program ID
        //Classes that implement IShader are responsible for loading and compiling shaders, and then passing the shader data to the GPU when needed.
        public abstract void Load(string vertexPath, string fragmentPath);
        public abstract void Bind();
        public abstract void Unbind();
        public void SetUniformMatrix4(string name, Matrix4 value)
        {
            // First, get the location of the uniform variable within the shader
            int location = GL.GetUniformLocation(programID, name);

            // Check if the uniform was found
            if (location != -1)
            {
                // Transpose the matrix if necessary and set the uniform value
                GL.UniformMatrix4(location, false, ref value);
            }
            else
            {
                // Optionally, warn about the uniform not being found
                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }
        public void SetUniformVec3(string name, Vector3 value)
        {
            // First, get the location of the uniform variable within the shader
            int location = GL.GetUniformLocation(programID, name);

            // Check if the uniform was found
            if (location != -1)
            {
                // Set the uniform value
                GL.Uniform3(location, value);
            }
            else
            {
                // Optionally, warn about the uniform not being found
                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }

    }


}