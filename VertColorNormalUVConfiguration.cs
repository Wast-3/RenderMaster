using OpenTK.Graphics.OpenGL4;  // Provides OpenGL 4.0+ bindings for graphics programming

namespace RenderMaster
{
    // This class defines a specific vertex configuration that includes position, color, normal, and UV coordinates.
    // It extends the VertexConfiguration base class and sets up the vertex attributes for this particular layout.
    public class VertColorNormalUVConfiguration : VertexConfiguration
    {
        // Constructor that initializes the vertex configuration with the provided vertex data.
        // It passes the vertex data to the base class constructor for standard setup.
        public VertColorNormalUVConfiguration(float[] vertices) : base(vertices) { }

        // Overrides the abstract method SetupAttributes in VertexConfiguration to define the layout of vertex attributes.
        // The attributes are defined as:
        // - Position: 3 components (x, y, z)
        // - Color: 3 components (r, g, b)
        // - Normal: 3 components (nx, ny, nz)
        // - UV: 2 components (u, v)
        protected override void SetupAttributes()
        {
            // Setup the position attribute (index 0 in the shader)
            // - 3 components (x, y, z)
            // - Type: Float
            // - Stride: 11 floats (each vertex has 11 floats: 3 for position, 3 for color, 3 for normal, 2 for UV)
            // - Offset: 0 (starts at the beginning of each vertex)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);  // Enable the position attribute

            // Setup the color attribute (index 1 in the shader)
            // - 3 components (r, g, b)
            // - Type: Float
            // - Stride: 11 floats
            // - Offset: 3 floats (position comes first, so color starts after 3 floats)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);  // Enable the color attribute

            // Setup the normal attribute (index 2 in the shader)
            // - 3 components (nx, ny, nz)
            // - Type: Float
            // - Stride: 11 floats
            // - Offset: 6 floats (position and color come first, so normal starts after 6 floats)
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 11 * sizeof(float), 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);  // Enable the normal attribute

            // Setup the UV attribute (index 3 in the shader)
            // - 2 components (u, v)
            // - Type: Float
            // - Stride: 11 floats
            // - Offset: 9 floats (position, color, and normal come first, so UV starts after 9 floats)
            GL.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, 11 * sizeof(float), 9 * sizeof(float));
            GL.EnableVertexAttribArray(3);  // Enable the UV attribute
        }
    }
}
