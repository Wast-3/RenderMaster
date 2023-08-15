using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using OpenTK.Compute.OpenCL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RenderMaster
{

    public class Game : GameWindow
    {
        public EngineConfig engineConfig;
        public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            this.engineConfig = new EngineConfig();
            this.mainScene = new Scene("main testing scene");
            mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(engineConfig.ModelDirectory, "UVTest\\cyl.verttxt"), Path.Combine(engineConfig.ModelDirectory, "UVTest\\uv_check2.png")));
            mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(engineConfig.ModelDirectory, "TexturedCylinder\\cylinder.verttxt"), Path.Combine(engineConfig.ModelDirectory, "TexturedCylinder\\uv_check2.png")));
            mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(engineConfig.ModelDirectory, "MonkeyTime\\monkey.verttxt"), Path.Combine(engineConfig.ModelDirectory, "MonkeyTime\\Cum.png")));
            mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(engineConfig.ModelDirectory, "HouseThing\\house.verttxt"), Path.Combine(engineConfig.ModelDirectory, "HouseThing\\House.png")));
            mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(engineConfig.ModelDirectory, "GroundTerrain\\mountain.verttxt"), Path.Combine(engineConfig.ModelDirectory, "GroundTerrain\\mountain.png")));
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
            mainScene.sceneModels[2].Rotation = new Vector3(30, 0, 90);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            mainScene.sceneModels[0].Position = new OpenTK.Mathematics.Vector3(-10, 0, 0);
            mainScene.sceneModels[1].Position = new Vector3(-10, 3, 1);
            mainScene.sceneModels[2].Position = new Vector3(-200, 1, -5);
            mainScene.sceneModels[2].Rotation = new Vector3(4.7f, 0, 0);

            mainScene.sceneModels[4].Scale = new Vector3(5);
            mainScene.sceneModels[4].Position = new Vector3(0, -5, 0);

            mainScene.sceneModels[2].Scale = new Vector3(3);
            mainScene.RenderScene(args);
            SwapBuffers();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            mainScene.camera.ProcessKeyEvents(e);
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
        Vector3 Up = new Vector3(0f, 1f, 0f);

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

        public void ProcessKeyEvents(KeyboardKeyEventArgs e)
        {
            float moveSpeed = 0.2f;

            // Check if Shift is being held down
            if (e.Shift)
            {
                moveSpeed *= 5.0f; // 5 times faster if Shift is held down
            }

            Vector3 forward = LookingAt - Position;

            // Ensure forward direction is normalized, if not zero
            if (forward.Length > 0)
            {
                forward = Vector3.Normalize(forward);

                // Calculate the right direction based on the cross product of forward and up vectors
                Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Up));

                if (e.Key == Keys.W)
                {
                    Position += moveSpeed * forward; // Move forward
                }
                if (e.Key == Keys.S)
                {
                    Position -= moveSpeed * forward; // Move backward
                }
                if (e.Key == Keys.A)
                {
                    Position -= moveSpeed * right; // Move left
                }
                if (e.Key == Keys.D)
                {
                    Position += moveSpeed * right; // Move right
                }
                if (e.Key == Keys.Space) // Assuming you're using Keys.Space for the space key
                {
                    Position += moveSpeed * Up; // Move up
                }
                if (e.Key == Keys.LeftControl || e.Key == Keys.RightControl) // Using either left or right control key
                {
                    Position -= moveSpeed * Up; // Move down
                }

                UpdateViewMatrix(); // Update the view matrix after changing the camera's position
            }
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
            this.camera = new Camera(new Vector3(2, 0, 0), new Vector3(0, 0, 0), 00.90f, 1, 1, 100000000);

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
                
            }
            sceneModels[0].Rotation += new Vector3(0, 0, (float)(0.5f * args.Time));
            sceneModels[1].Rotation += new Vector3(0, 0, (float)(0.5f * args.Time));
            sceneModels[2].Rotation += new Vector3((float)(0.8f * args.Time), 0, 0);
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