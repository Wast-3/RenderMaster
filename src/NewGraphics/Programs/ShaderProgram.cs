using OpenTK.Graphics.OpenGL4;
namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class ShaderProgram : System.IDisposable
    {
        public int Handle { get; }
        public ShaderProgram(string vertSrc, string fragSrc)
        {
            int vs = Compile(ShaderType.VertexShader, vertSrc);
            int fs = Compile(ShaderType.FragmentShader, fragSrc);
            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vs); GL.AttachShader(Handle, fs);
            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int ok);
            if (ok == 0) throw new System.Exception(GL.GetProgramInfoLog(Handle));
            GL.DeleteShader(vs); GL.DeleteShader(fs);
        }
        static int Compile(ShaderType t, string src)
        {
            int s = GL.CreateShader(t);
            GL.ShaderSource(s, src); GL.CompileShader(s);
            GL.GetShader(s, ShaderParameter.CompileStatus, out int ok);
            if (ok == 0) throw new System.Exception($"{t}:\n{GL.GetShaderInfoLog(s)}");
            return s;
        }
        public void Dispose() => GL.DeleteProgram(Handle);
    }
}
