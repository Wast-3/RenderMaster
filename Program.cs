using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using OpenTK.Compute.OpenCL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

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
        }

        Scene mainScene;

        static void Main(string[] args)
        {
            Game game = new Game(2560, 1440, "RENDERMASTER ENGINE");
            game.Run();
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            mainScene.RenderSceneSetup();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            mainScene.sceneModels[0].Position = new Vector3(-2, 0, 0);
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

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
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