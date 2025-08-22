using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using RenderMaster.Engine;

namespace RenderMaster
{


    public abstract class AShader
    {
        public int programID;



        public abstract void Load(string vertexPath, string fragmentPath);



        public abstract void Bind();



        public abstract void Unbind();



        public void SetUniformMatrix4(string name, Matrix4 value)
        {

            int location = GL.GetUniformLocation(programID, name);


            if (location != -1)
            {

                GL.UniformMatrix4(location, false, ref value);
            }
            else
            {

                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }



        public void SetUniformVec3(string name, Vector3 value)
        {

            int location = GL.GetUniformLocation(programID, name);


            if (location != -1)
            {
                GL.Uniform3(location, ref value);
            }
            else
            {

                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }



        public void SetUniformFloat(string name, float value)
        {

            int location = GL.GetUniformLocation(programID, name);


            if (location != -1)
            {
                GL.Uniform1(location, value);
            }
            else
            {

                Console.WriteLine($"Warning: Uniform '{name}' not found in shader.");
            }
        }



        public void SetSampler2D(string name, TextureUnit textureUnit)
        {
            Logger.Log("Setting texture unit to " + textureUnit, LogLevel.Debug);

            int textureUnitOffset = (int)textureUnit - (int)TextureUnit.Texture0;


            int location = GL.GetUniformLocation(programID, name);


            GL.Uniform1(location, textureUnitOffset);
        }
    }
}
