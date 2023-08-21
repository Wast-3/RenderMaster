using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace RenderMaster
{
    public class BasicTexturedShader : AShader
    { 
        //holy fuck my tooth (jaw?) hurts SO BAD
        // The constructor that was already in your code
        public BasicTexturedShader(string vertexPath, string fragmentPath)
        {
            Load(vertexPath, fragmentPath);
        }

        public override void Load(string vertexPath, string fragmentPath)
        {
            
            // Read the contents of the shader files
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            // Compile the shaders
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(vertexShader);
            GL.CompileShader(fragmentShader);

            // Check for compilation errors
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int isVertexCompiled);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int isFragmentCompiled);
            if (isVertexCompiled == 0 || isFragmentCompiled == 0)
            {
                // Error handling if shaders did not compile
                GL.GetShaderInfoLog(vertexShader, out string vertexInfo);
                GL.GetShaderInfoLog(fragmentShader, out string fragmentInfo);
                throw new Exception($"Vertex Shader Error: {vertexInfo}\nFragment Shader Error: {fragmentInfo}");
            }

            // Link the shaders into a program
            programID = GL.CreateProgram();
            GL.AttachShader(programID, vertexShader);
            GL.AttachShader(programID, fragmentShader);
            GL.LinkProgram(programID);

            // Check for linking errors
            GL.GetProgram(programID, GetProgramParameterName.LinkStatus, out int isProgramLinked);
            if (isProgramLinked == 0)
            {
                // Error handling if program did not link
                GL.GetProgramInfoLog(programID, out string programInfo);
                throw new Exception($"Program Linking Error: {programInfo}");
            }

            // Delete the shaders as they're no longer needed
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        public override void Bind()
        {

            // Simply use the program
            GL.UseProgram(programID);
        }

        public override void Unbind()
        {
            // Unbind the program by setting the active program to 0
            GL.UseProgram(0);
        }

    }
}