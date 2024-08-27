using OpenTK.Graphics.OpenGL4;

namespace RenderMaster
{
    public class VertColorNormalUVConfiguration : VertexConfiguration
    {
        public VertColorNormalUVConfiguration(float[] vertices) : base(vertices) { }

        protected override void SetupAttributes()
        {
            // Position attribute (3 components, starting at index 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute (3 components, starting at index 3)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Normal attribute (3 components, starting at index 6)
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            // UV attribute (2 components, starting at index 9)
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 11 * sizeof(float), 9 * sizeof(float));
            GL.EnableVertexAttribArray(3);
        }
    }
}