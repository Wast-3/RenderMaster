using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.CompilerServices;

namespace RenderMaster
{
    public class ImGuiBufferConfig : VertexConfiguration
    {
        public ImGuiBufferConfig() : base() { }

        protected override void SetupAttributes()
        {
            var stride = Unsafe.SizeOf<ImDrawVert>();


            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);


            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
            GL.EnableVertexAttribArray(1);


            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);
            GL.EnableVertexAttribArray(2);
        }
    }
}