using OpenTK.Graphics.OpenGL4;  // Provides OpenGL 4.0+ bindings for graphics programming
using OpenTK.Mathematics;       // Provides mathematical types like Vector3 and Matrix4, essential for 3D transformations
using StbImageSharp;            // Library for image loading, often used for texture handling in OpenGL applications

namespace RenderMaster
{
    // The BasicTexturedShader class is a concrete implementation of the AShader abstract class.
    // It handles the loading, compiling, and linking of vertex and fragment shaders that are used for basic textured rendering.
    public class BasicTexturedShader : AShader
    {
        // Constructor that takes paths to the vertex and fragment shader files.
        // It calls the Load method to compile and link the shaders into a usable shader program.
        public BasicTexturedShader(string vertexPath, string fragmentPath)
        {
            Load(vertexPath, fragmentPath);  // Load the shaders upon instantiation
        }

        // Implementation of the Load method, which reads, compiles, and links the vertex and fragment shaders.
        public override void Load(string vertexPath, string fragmentPath)
        {
            // Read the shader source code from the provided file paths
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            // Compile the vertex shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);  // Create a new vertex shader
            GL.ShaderSource(vertexShader, vertexShaderSource);  // Set the source code of the vertex shader
            GL.CompileShader(vertexShader);  // Compile the vertex shader

            // Compile the fragment shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);  // Create a new fragment shader
            GL.ShaderSource(fragmentShader, fragmentShaderSource);  // Set the source code of the fragment shader
            GL.CompileShader(fragmentShader);  // Compile the fragment shader

            // Check for compilation errors in the vertex shader
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int isVertexCompiled);
            if (isVertexCompiled == 0)  // If compilation failed, get the error log
            {
                GL.GetShaderInfoLog(vertexShader, out string vertexInfo);
                throw new Exception($"Vertex Shader Error: {vertexInfo}");  // Throw an exception with the error details
            }

            // Check for compilation errors in the fragment shader
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int isFragmentCompiled);
            if (isFragmentCompiled == 0)  // If compilation failed, get the error log
            {
                GL.GetShaderInfoLog(fragmentShader, out string fragmentInfo);
                throw new Exception($"Fragment Shader Error: {fragmentInfo}");  // Throw an exception with the error details
            }

            // Create a shader program and link the compiled vertex and fragment shaders
            programID = GL.CreateProgram();  // Create a new program ID
            GL.AttachShader(programID, vertexShader);  // Attach the vertex shader to the program
            GL.AttachShader(programID, fragmentShader);  // Attach the fragment shader to the program
            GL.LinkProgram(programID);  // Link the shaders into a complete program

            // Check for linking errors in the shader program
            GL.GetProgram(programID, GetProgramParameterName.LinkStatus, out int isProgramLinked);
            if (isProgramLinked == 0)  // If linking failed, get the error log
            {
                GL.GetProgramInfoLog(programID, out string programInfo);
                throw new Exception($"Program Linking Error: {programInfo}");  // Throw an exception with the error details
            }

            // Once linked, the individual shaders are no longer needed, so delete them
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        // Binds the shader program for use in rendering.
        // This method makes the shader program active, meaning it will be used for subsequent rendering commands.
        public override void Bind()
        {
            GL.UseProgram(programID);  // Use the compiled and linked shader program
        }

        // Unbinds the shader program.
        // This method deactivates the current shader program by setting the active program to 0.
        public override void Unbind()
        {
            GL.UseProgram(0);  // Unbind the shader program by setting the active program to 0
        }
    }
}
