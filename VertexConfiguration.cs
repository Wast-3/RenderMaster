using ImGuiNET;  // Provides bindings for ImGui, a graphical user interface library often used for debug tools in games
using OpenTK.Graphics.OpenGL4;  // Provides OpenGL 4.0+ bindings
using System.Runtime.CompilerServices;  // Provides functionality to interact with low-level details like buffer sizes in memory

namespace RenderMaster
{
    // Abstract base class that defines the basic setup and management of vertex configurations in OpenGL.
    // Handles the creation and management of Vertex Array Objects (VAO), Vertex Buffer Objects (VBO),
    // and optionally Index Buffers for rendering with vertex arrays.
    public abstract class VertexConfiguration
    {
        // Protected fields for storing the OpenGL identifiers for the VAO, VBO, and index buffer
        protected int VAO;  // Vertex Array Object, which stores the vertex attribute configuration
        protected int VBO;  // Vertex Buffer Object, which stores the vertex data
        protected int indexBuffer;  // Optional index buffer for indexed drawing, defaulted to -1 (not used)

        // Constructor that initializes the vertex configuration with given vertex data.
        // This constructor sets up the VAO and VBO and binds the vertex data to the buffer.
        public VertexConfiguration(float[] vertices)
        {
            VAO = GL.GenVertexArray();  // Generate a new VAO ID
            VBO = GL.GenBuffer();  // Generate a new VBO ID

            GL.BindVertexArray(VAO);  // Bind the VAO, which will store the vertex attribute configuration

            // Bind the VBO and upload the vertex data to the GPU
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

            // Setup vertex attributes, such as position, color, normals, etc.
            SetupAttributes();

            // Unbind the VBO and VAO to avoid accidental modifications
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            indexBuffer = -1;  // Initialize the index buffer to -1, indicating it's not in use
        }

        // Overloaded constructor for initializing the vertex configuration without immediate vertex data.
        // This is primarily used for ImGui, where buffer sizes are pre-allocated.
        public VertexConfiguration()
        {
            // For ImGui usage, manually set the size of the vertex and index buffers
            int vertexBufferSize = 100000 * Unsafe.SizeOf<ImDrawVert>();  // Size for vertex buffer based on ImGui vertex size
            int indexBufferSize = 200000 * sizeof(ushort);  // Size for index buffer based on ImGui index size

            VAO = GL.GenVertexArray();  // Generate a new VAO ID
            VBO = GL.GenBuffer();  // Generate a new VBO ID
            indexBuffer = GL.GenBuffer();  // Generate a new index buffer ID

            GL.BindVertexArray(VAO);  // Bind the VAO, preparing to store the configuration

            // Allocate memory for the vertex buffer without uploading data yet (using IntPtr.Zero)
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            // Allocate memory for the index buffer without uploading data yet
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            // Setup vertex attributes, depending on the specific implementation
            SetupAttributes();
        }

        // Abstract method to be implemented by derived classes, which will define the specific layout of vertex attributes.
        protected abstract void SetupAttributes();  // Method to set up vertex attribute pointers, must be implemented by subclasses

        // Method to bind the vertex configuration for rendering.
        // This method binds the VAO and, if available, the index buffer, making the configuration active for drawing.
        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);  // Bind the VBO to make the vertex data active
            if (indexBuffer != -1)  // If the index buffer is being used
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);  // Bind the index buffer
            }
            GL.BindVertexArray(VAO);  // Bind the VAO, enabling the vertex attribute configuration
        }

        // Method to unbind the vertex configuration after rendering.
        // This method unbinds the VAO and VBO to prevent further modifications.
        public void Unbind()
        {
            GL.BindVertexArray(0);  // Unbind the VAO, deactivating the vertex attribute configuration
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);  // Unbind the VBO
            GL.BindVertexArray(0);  // Unbind the VAO again for safety
        }
    }
}
