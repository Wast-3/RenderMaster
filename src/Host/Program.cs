using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using OpenTK.Mathematics;
using System;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderMaster.Engine;
using RenderMaster.src.Physics;
using BepuPhysics;

namespace RenderMaster;

public class Game : GameWindow
{


    IUserInterface userInterface = null!; // initialized in OnLoad

    Scene mainScene;
    OpenGLStateStack openGLState;

    const double FixedUpdateRate = 1.0 / 60.0;
    double updateAccumulator = 0.0;

    PhysicsEngine physicsEngine = new PhysicsEngine();
    List<PhysicsBinding> physicsBindings = new List<PhysicsBinding>();

    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {

        ClientSize = (width, height),
        Title = title
    })
    {
        this.mainScene = new Scene("main testing scene", width, height);

        /* mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "UVTest\\cyl.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "UVTest\\uv_check2.png")));
           mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "TexturedCylinder\\cylinder.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "TexturedCylinder\\uv_check2.png")));
           mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "MonkeyTime\\monkey.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "MonkeyTime\\Cum.png")));
           mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "HouseThing\\house.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "HouseThing\\House.png")));
           mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "GroundTerrain\\mountain.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "GroundTerrain\\mountain.png"))); */

        Material tableMaterial = new Material(
            TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table.jpg")),
            TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table_specular.jpg"))
        );

        Material lampMaterial = new Material(
            TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\lamp.jpg")),
            TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\lamp_specular.jpg"))
        );

        mainScene.AddModel(new Model(VertType.VertColorNormal, ModelShaderType.VertColorNormal,
            Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table.verttxt"), tableMaterial)); // table

        mainScene.AddModel(new Model(VertType.VertColorNormal, ModelShaderType.VertColorNormal,
            Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\lamp.verttxt"), lampMaterial)); // lamp

        mainScene.sceneModels[1].Position = new Vector3(0, 1.5f, 0); // move lamp up

        //generate a grid of physics cubes
        int gridSize = 10;
        float spacing = 2.0f;

        physicsEngine.Setup();

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                Material cubeMaterial = new Material(
                    TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "UVTest\\uv_check2.png")),
                    TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "UVTest\\uv_check2.png"))
                );
                Model cube = new Model(VertType.VertColorNormal, ModelShaderType.VertColorNormal,
                    Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table.verttxt"), cubeMaterial, physicsPreset: 1);

                var randomFloat = new Random().Next(0, 100) / 100.0f;

                cube.Position = new Vector3(
                    (i - gridSize / 2) * spacing + randomFloat,
                    5.0f + (j * spacing),
                    0 + randomFloat
                );
                
                var shape = new BepuPhysics.Collidables.Sphere(2);
                var inertia = shape.ComputeInertia(1f);
                var reference = physicsEngine.simulation.Shapes.Add(shape);

                var bodyDescription = BodyDescription.CreateDynamic(
                    new BepuPhysics.RigidPose(new System.Numerics.Vector3(cube.Position.X, cube.Position.Y, cube.Position.Z)),
                    inertia,
                    reference,
                    0.01f);
                var bodyHandle = physicsEngine.simulation.Bodies.Add(bodyDescription);

                mainScene.AddModel(cube);

                physicsBindings.Add(new PhysicsBinding(bodyHandle, cube));
            }
        }

        openGLState = new OpenGLStateStack();
    }


    static void Main(string[] args)
    {
        Game game = new Game(2560, 1440, "RENDERMASTER ENGINE");
        game.Run();
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Logger.Log("RENDERMASTER START: ", LogLevel.Info);

        mainScene.RenderSceneSetup();
        openGLState.PushState();
        userInterface = new UI();
        openGLState.PopState();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        updateAccumulator += args.Time;

        while (updateAccumulator >= FixedUpdateRate)
        {
            mainScene.Update(FixedUpdateRate);
            physicsEngine.simulation.Timestep((float)FixedUpdateRate);
            updateAccumulator -= FixedUpdateRate;
        }

        physicsEngine.syncModelsToPhysics(physicsBindings);

        mainScene.camera.ProcessKeyboard(KeyboardState, (float)args.Time);

        userInterface.Update(args, this.mainScene.camera);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        openGLState.PushState();
        mainScene.RenderScene(args);
        userInterface.Bind();
        userInterface.Render();
        userInterface.Unbind();
        SwapBuffers();

        openGLState.PopState();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        var io = ImGui.GetIO();

        ImGuiKey key = ImGuiKeyMapper.MapOpenTKKeyToImGuiKey(e.Key);
        io.AddKeyEvent(key, true);


        io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
        io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);


        io.AddKeyEvent(ImGuiKey.ModSuper, e.Modifiers.HasFlag(KeyModifiers.Super));
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        var io = ImGui.GetIO();

        ImGuiKey key = ImGuiKeyMapper.MapOpenTKKeyToImGuiKey(e.Key);
        io.AddKeyEvent(key, false);


        io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
        io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);


        io.AddKeyEvent(ImGuiKey.ModSuper, e.Modifiers.HasFlag(KeyModifiers.Super));
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddInputCharacter(e.Unicode);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        var io = ImGui.GetIO();
        float scaleFactor = io.DisplayFramebufferScale.Y;

        io.MousePos = new System.Numerics.Vector2(MouseState.X * scaleFactor, MouseState.Y * scaleFactor);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddMouseButtonEvent((int)e.Button, true);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddMouseButtonEvent((int)e.Button, false);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddMouseWheelEvent(e.OffsetX, e.OffsetY);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);


        userInterface?.Resize(e);


        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(e.Width, e.Height);


        var framebufferWidth = (int)(e.Width * io.DisplayFramebufferScale.X);
        var framebufferHeight = (int)(e.Height * io.DisplayFramebufferScale.Y);


        GL.Viewport(0, 0, framebufferWidth, framebufferHeight);
    }

}
