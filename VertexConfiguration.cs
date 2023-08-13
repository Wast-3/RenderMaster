using OpenTK.Graphics.OpenGL4;

namespace RenderMaster
{
    public abstract class VertexConfiguration
    {
        protected int VAO;
        protected int VBO;

        public VertexConfiguration(float[] vertices)
        {
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            SetupAttributes();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        protected abstract void SetupAttributes(); // Different implementations will handle different vertex configurations

        public void Bind()
        {
            GL.BindVertexArray(VAO);
        }

        public void Unbind()
        {
            GL.BindVertexArray(0);
        }
    }
}