using OpenTK.Windowing.Common;  // Provides common types used in OpenTK windowing, such as FrameEventArgs
using OpenTK.Graphics.OpenGL4;  // Provides OpenGL 4.0+ bindings
using OpenTK.Mathematics;       // Provides mathematical types such as Vector3 and Matrix4

namespace RenderMaster
{
    // The Scene class is responsible for managing all the models within a specific scene,
    // handling their rendering, and managing the camera that views the scene.
    public class Scene
    {
        // Private field to hold the name of the scene
        string name;

        // Public list to hold all the models that are part of this scene
        public List<Model> sceneModels;

        // Public camera used to view the scene
        public Camera camera;

        // Constructor for the Scene class.
        // Initializes the scene with a name, an empty list of models, and a camera positioned at (2, 0, 0).
        // The camera is looking towards the origin (0, 0, 0) with a specified aspect ratio and clipping planes.
        public Scene(string name, int width, int height)
        {
            this.name = name;
            this.sceneModels = new List<Model>();

            // The camera is positioned at (2, 0, 0), looking at (0, 0, 0).
            // The aspect ratio is set to window sizes, and the near and far clipping planes are 1 and 100,000,000 respectively.
            this.camera = new Camera(new Vector3(2, 0, 0), new Vector3(0, 0, 0), 0.8f,(float)width / (float)height, 1, 100000000);
        }

        // Method to add a model to the scene.
        // The model is added to the sceneModels list, which stores all models to be rendered.
        public void AddModel(Model model)
        {
            sceneModels.Add(model);
        }

        // Method to remove a model from the scene.
        // The model is removed from the sceneModels list.
        public void RemoveModel(Model model)
        {
            sceneModels.Remove(model);
        }

        // Method to render the scene.
        // Clears the screen with a specified background color and clears the depth buffer.
        // Then iterates over all models in the scene and calls their Render method, passing in the current camera.
        public void RenderScene(FrameEventArgs args)
        {

            // Set the clear color to a dark gray (R=0.2, G=0.2, B=0.2, A=1.0)
            GL.ClearColor(0.4f, 0.4f, 0.4f, 1.0f);

            // Clear both the color buffer and the depth buffer to prepare for rendering
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Iterate over each model in the scene and render it using the current camera
            foreach (Model model in sceneModels)
            {
                model.Render(args, camera);
            }
        }

        // Method to set up the scene before rendering.
        // Enables depth testing to ensure that objects closer to the camera are rendered in front of objects farther away.
        public void RenderSceneSetup()
        {
            GL.Enable(EnableCap.DepthTest); // Enable depth testing to handle object occlusion correctly
        }
    }
}
