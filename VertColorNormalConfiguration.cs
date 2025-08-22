using OpenTK.Graphics.OpenGL4;

namespace RenderMaster
{
    public class VertColorNormalConfiguration : VertexConfiguration
    {
        public VertColorNormalConfiguration(float[] vertices) : base(vertices) { }

        protected override void SetupAttributes()
        {

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);


            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);


            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 9 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
        }
    }
}