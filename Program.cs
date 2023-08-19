using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using OpenTK.Compute.OpenCL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RenderMaster
{

    public class Game : GameWindow
    {
        public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            this.mainScene = new Scene("main testing scene");
            /*            mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "UVTest\\cyl.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "UVTest\\uv_check2.png")));
                        mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "TexturedCylinder\\cylinder.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "TexturedCylinder\\uv_check2.png")));
                        mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "MonkeyTime\\monkey.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "MonkeyTime\\Cum.png")));
                        mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "HouseThing\\house.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "HouseThing\\House.png")));
                        mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "GroundTerrain\\mountain.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "GroundTerrain\\mountain.png")));
            */
            mainScene.AddModel(new Model(VertType.VertColorNormal, ModelShaderType.VertColorNormal, Path.Combine(EngineConfig.ModelDirectory, "LightingTest\\testiso.verttxt")));

            openGLState = new OpenGLStateStack();
            
        }

        Scene mainScene;
        IUserInterface userInterface;
        OpenGLStateStack openGLState;

        static void Main(string[] args)
        {
            Game game = new Game(2560, 1440, "RENDERMASTER ENGINE");
            game.Run();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            mainScene.RenderSceneSetup();
            openGLState.PushState();
            userInterface = new UI();
            openGLState.PopState();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            //Reset user interface to initially recorded state:
            //OpenGLState.ResetState();

            openGLState.PushState();

            mainScene.sceneModels[0].Position = new Vector3(-2, 0, 0);
            mainScene.RenderScene(args);
            userInterface.Bind();
            userInterface.Render(args, this.mainScene.camera);
            userInterface.Unbind();
            SwapBuffers();

            openGLState.PopState();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            mainScene.camera.ProcessKeyEvents(e);
            var io = ImGui.GetIO();
            io.KeysDown[(int)e.Key] = true;
            io.AddInputCharacter((uint)e.Key);
            io.KeyCtrl = e.Control;
            io.KeyShift = e.Shift;
            io.KeyAlt = e.Alt;
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            var io = ImGui.GetIO();
            float scaleFactor = io.DisplayFramebufferScale.Y; // Or Y if you need to scale by the Y axis
            
            io.MousePos = new System.Numerics.Vector2(MouseState.X * scaleFactor, MouseState.Y * scaleFactor);
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var io = ImGui.GetIO();
            io.MouseDown[(int)e.Button] = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var io = ImGui.GetIO();
            io.MouseDown[(int)e.Button] = false;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var io = ImGui.GetIO();
            io.MouseWheel = e.OffsetY;
            io.MouseWheelH = e.OffsetX;
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            var io = ImGui.GetIO();
            io.KeysDown[(int)e.Key] = false; // Set the state of the key to "not pressed."
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Resize user interface if applicable
            userInterface.Resize(e);

            // Update ImGui display size
            var io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(e.Width, e.Height);

            // If you have a high-DPI setting, you may need to calculate the framebuffer size accordingly
            var framebufferWidth = (int)(e.Width * io.DisplayFramebufferScale.X);
            var framebufferHeight = (int)(e.Height * io.DisplayFramebufferScale.Y);

            // Set the OpenGL viewport to cover the entire framebuffer
            GL.Viewport(0, 0, framebufferWidth, framebufferHeight);
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
            this.camera = new Camera(new Vector3(2, 0, 0), new Vector3(0, 0, 0), 2560.0f/1440.0f, 1, 1, 100000000);

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
            GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            foreach (Model model in sceneModels)
            {
                model.Render(args, camera);
                
            }
        }

        public void RenderSceneSetup()
        {
            GL.Enable(EnableCap.DepthTest);
        }

    }

    public enum VertType
    {
        VertColor,
        VertColorTexture,
        VertColorNormal
    }

    public enum ModelShaderType
    {
        BasicTextured,
        BasicVertColor,
        VertColorNormal
    }
}