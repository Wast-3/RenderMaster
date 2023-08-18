using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.CompilerServices;

namespace RenderMaster
{
    public abstract class VertexConfiguration
    {
        protected int VAO;
        protected int VBO;
        protected int indexBuffer;

        public VertexConfiguration(float[] vertices)
        {
            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            SetupAttributes();

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            indexBuffer = -1;
        }

        public VertexConfiguration()
        {
            //Right now this overload is only being used by ImGui. Anyways, I'm manually setting the size of these buffers here
            int vertexBufferSize = 1000 * Unsafe.SizeOf<ImDrawVert>();
            int indexBufferSize = 2000 * sizeof(ushort);

            VAO = GL.GenVertexArray();
            VBO = GL.GenBuffer();
            indexBuffer = GL.GenBuffer();

            GL.BindVertexArray(VAO);

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            SetupAttributes();

        }

        protected abstract void SetupAttributes(); // Different implementations will handle different vertex configurations

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            if (indexBuffer != -1)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            }
            GL.BindVertexArray(VAO);
        }

        public void Unbind()
        {
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
    }
}