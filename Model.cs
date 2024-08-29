using StbImageSharp;  // Library for image loading, often used for texture handling in OpenGL applications
using OpenTK.Mathematics;  // Provides mathematical types such as Vector3 and Matrix4, essential for 3D transformations
using static System.Formats.Asn1.AsnWriter;  // This seems to be mistakenly included; typically not used in rendering code
using OpenTK.Windowing.Common;  // Provides types used in OpenTK windowing, such as FrameEventArgs

namespace RenderMaster
{

    // Model class representing a 3D model in the scene.
    // It implements the IModel interface and contains information about the model's geometry, shaders, and rendering logic.
    public class Model : IModel
    {
        VertType vertType;  // Specifies the type of vertex data (e.g., colored, textured, with normals)
        ModelShaderType modelShaderType;  // Specifies the type of shader to be used for rendering the model
        public VertexConfiguration vertexConfiguration;  // Configuration for how vertex data is structured
        string modelPath;  // Path to the file containing model vertex data
        string? imagePath;  // Path to the texture image file (if any)
        
        public float[] verts;  // Array of vertex data loaded from the model file
        public Vector3 Position { get; set; }  // Position of the model in the 3D space
        public Vector3 Rotation { get; set; }  // Rotation of the model along X, Y, and Z axes
        public Vector3 Scale { get; set; } = new Vector3(1f, 1f, 1f);  // Scale of the model, defaulting to no scaling
        IRenderer renderer;  // The renderer responsible for drawing this model

        // Method to render the model. It delegates the rendering to the IRenderer instance associated with this model.
        // The [MeasureExecutionTime] attribute suggests that the execution time of this method is measured (likely a custom attribute).
        [MeasureExecutionTime]
        public void Render(FrameEventArgs args, Camera camera)
        {
            renderer.Render(args, camera);  // Call the renderer's Render method, passing the frame event arguments and the camera
        }

        // Constructor for the Model class.
        // Initializes the model with vertex type, shader type, and the path to the model data file.
        public Model(VertType vertType, ModelShaderType modelShaderType, string modelPath)
        {
            this.vertType = vertType;  // Store the vertex type (e.g., VertColor, VertColorNormal)
            this.modelPath = modelPath;  // Store the path to the model file
            this.verts = loadVerticesTextureFromPath(modelPath);  // Load vertex data from the model file

            // Initialize the vertex configuration based on the vertex type.
            // For now, it seems to default to a color normal UV configuration, which may involve color, normal, and texture data.
            this.vertexConfiguration = new VertColorNormalUVConfiguration(this.verts);

            this.modelShaderType = modelShaderType;  // Store the shader type (e.g., BasicTextured, VertColorNormal)

            // Initialize the renderer with a basic lighting renderer and associate it with a shader program.
            // The shader paths are hard-coded for now, pointing to a vertex and fragment shader.
            this.renderer = new BasicLightingRenderer(
                this,
                new BasicTexturedShader(Path.Combine(EngineConfig.ShaderDirectory, "lightingtest.vert"), Path.Combine(EngineConfig.ShaderDirectory, "lightingtest.frag"))
                );
        }

        // Method to get the model matrix, which is used to transform the model's vertices in the world space.
        // The [MeasureExecutionTime] attribute suggests that this method's execution time is also measured.
        [MeasureExecutionTime]
        public Matrix4 GetModelMatrix()
        {
            Matrix4 model = Matrix4.Identity;  // Start with an identity matrix (no transformation)
            model *= Matrix4.CreateScale(Scale);  // Apply scaling transformation
            model *= Matrix4.CreateRotationX(Rotation.X);  // Apply rotation around the X-axis
            model *= Matrix4.CreateRotationY(Rotation.Y);  // Apply rotation around the Y-axis
            model *= Matrix4.CreateRotationZ(Rotation.Z);  // Apply rotation around the Z-axis
            model *= Matrix4.CreateTranslation(Position);  // Apply translation to move the model to its position in the scene
            return model;  // Return the final transformation matrix
        }

        // Private method to load vertex data from a model file.
        // The file is expected to contain vertices formatted in a specific way, which is parsed into a float array.
        private float[] loadVerticesTextureFromPath(string path)
        {
            List<float> vertices = new List<float>();  // Initialize a list to hold the vertex data

            // Open the file at the specified path for reading
            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream))
            {
                // Get the total number of lines in the file, which helps in tracking the progress of parsing
                int totalLines = File.ReadLines(path).Count();
                int currentLine = 0;  // Track the current line number during parsing
                string line;
                while ((line = reader.ReadLine()) != null)  // Read each line from the file
                {
                    currentLine++;
                    string[] parts = line.Split(' ');  // Split the line into parts based on spaces (assumes space-delimited vertex data)

                    // Log progress every 5th line to reduce CPU load from excessive logging
                    if (currentLine % 5 == 0)
                    {
                        Console.WriteLine("Parsing... Counted " + parts.Length + " vert parts");
                        Console.WriteLine("Current line: " + currentLine + " out of " + totalLines + "\n");
                    }

                    // Parse each part into a float and add it to the vertices list
                    for (int i = 0; i < parts.Length; i++)
                    {
                        vertices.Add(float.Parse(parts[i]));
                    }
                }
            }

            return vertices.ToArray();  // Convert the list of vertices to an array and return it
        }
    }

    // Interface for 3D models. Any class implementing this interface must provide a Render method.
    public interface IModel
    {
        public void Render(FrameEventArgs args, Camera camera);  // Method to render the model using the provided FrameEventArgs and Camera
    }
}
