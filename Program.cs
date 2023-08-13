using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using OpenTK.Compute.OpenCL;
using OpenTK.Mathematics;

namespace RenderMaster
{
    public class Game : GameWindow
    {
        public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title }) 
        {
            this.mainScene = new Scene("main testing scene");
            mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, "H:\\Google Drive Sync\\dev\\Development\\RenderMaster\\Assets\\Models\\TexturedCylinder\\cylinder.verttxt", "H:\\Google Drive Sync\\dev\\Development\\RenderMaster\\Assets\\Models\\TexturedCylinder\\uv_check.png"));
        }

        Scene mainScene;

        static void Main(string[] args)
        {
            Game game = new Game(800, 600, "OpenTK testing");            
            game.Run();
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            mainScene.RenderSceneSetup();
        }
        
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            mainScene.sceneModels[0].Position = new OpenTK.Mathematics.Vector3(-10, 0, 0);
            mainScene.RenderScene(args);
            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
        }
    }

    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 LookingAt { get; set; }
        public Matrix4 View { get; set; }
        public Matrix4 Projection { get; set; }

        public Camera(Vector3 position, Vector3 lookingAt, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
        {
            Position = position;
            LookingAt = lookingAt;
            UpdateViewMatrix();
            SetPerspectiveProjection(fieldOfView, aspectRatio, nearPlane, farPlane);
        }

        public void UpdateViewMatrix()
        {
            Vector3 up = Vector3.UnitY; // Standard up vector
            View = Matrix4.LookAt(Position, LookingAt, up);
        }

        public void SetPerspectiveProjection(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
        {
            Projection = Matrix4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlane, farPlane);
        }
    }

    public class Scene
    {
        string name;

        public List<Model> sceneModels;

        public Camera camera;
        
        //The scene class is responsible for holding the models that are going to be rendered and their respective data
        public Scene(string name)
        {
            this.name = name;
            this.sceneModels = new List<Model>();
            this.camera = new Camera(new Vector3(2,0,0), new Vector3(0,0,0), 0.90f, 1, 1, 100000);

        }

        public void AddModel(Model model)
        {
            sceneModels.Add(model);
        }

        public void RemoveModel(Model model)
        {
            sceneModels.Remove(model);
        }

        public void RenderScene(FrameEventArgs args)
        {
            
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            foreach (Model model in sceneModels)
            {
                model.Render(args, camera);
                model.Rotation += new Vector3(0, 0, (float)(0.5f * args.Time));
            }
        }

        public void RenderSceneSetup()
        {
            GL.Enable(EnableCap.DepthTest);
        }


    }

    public interface IShader
    {
        //Classes that implement IShader are responsible for loading and compiling shaders, and then passing the shader data to the GPU when needed.
        void Load(string vertexPath, string fragmentPath);
        void Bind();
        void Unbind();
    }

    public enum VertType
    {
        VertColor,
        VertColorTexture
    }

    public enum ModelShaderType
    {
        BasicTextured,
        BasicVertColor
    }
}