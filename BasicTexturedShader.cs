using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace RenderMaster
{


    public class BasicTexturedShader : AShader
    {


        public BasicTexturedShader(string vertexPath, string fragmentPath)
        {
            Load(vertexPath, fragmentPath);
        }


        public override void Load(string vertexPath, string fragmentPath)
        {

            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);


            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);


            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);


            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int isVertexCompiled);
            if (isVertexCompiled == 0)
            {
                GL.GetShaderInfoLog(vertexShader, out string vertexInfo);
                throw new Exception($"Vertex Shader Error: {vertexInfo}");
            }


            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int isFragmentCompiled);
            if (isFragmentCompiled == 0)
            {
                GL.GetShaderInfoLog(fragmentShader, out string fragmentInfo);
                throw new Exception($"Fragment Shader Error: {fragmentInfo}");
            }


            programID = GL.CreateProgram();
            GL.AttachShader(programID, vertexShader);
            GL.AttachShader(programID, fragmentShader);
            GL.LinkProgram(programID);


            GL.GetProgram(programID, GetProgramParameterName.LinkStatus, out int isProgramLinked);
            if (isProgramLinked == 0)
            {
                GL.GetProgramInfoLog(programID, out string programInfo);
                throw new Exception($"Program Linking Error: {programInfo}");
            }


            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }



        public override void Bind()
        {
            GL.UseProgram(programID);
        }



        public override void Unbind()
        {
            GL.UseProgram(0);
        }
    }
}
