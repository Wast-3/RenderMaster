using OpenTK.Graphics.OpenGL4;

namespace RenderMaster
{
    public class VertColorTextureConfiguration : VertexConfiguration
    {
        public VertColorTextureConfiguration(float[] vertices) : base(vertices) { }

        protected override void SetupAttributes()
        {
            // Position attribute (3 components, starting at index 0)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute (3 components, starting at index 3)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Texture coordinate attribute (2 components, starting at index 6)
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);
        }
    }
}