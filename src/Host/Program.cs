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


    IUserInterface userInterface = null!;

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
        openGLState = new OpenGLStateStack();
        
        this.mainScene = new Scene("main testing scene", width, height);

        BasicImageTexture diffuse = new BasicImageTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table.jpg"));
        BasicImageTexture specular = new BasicImageTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table_specular.jpg"));

        var tableMaterial = new Material(diffuse, specular);

        mainScene.AddModel(new Model(VertType.VertColorNormal, ModelShaderType.VertColorNormal,
            Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table.verttxt"), tableMaterial)); // table

        mainScene.sceneModels[0].Position = new Vector3(0, 1.5f, 0); // move table up

        physicsEngine.Setup();
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
        mainScene.camera.ProcessKeyEvents(e);
        var io = ImGui.GetIO();

        ImGuiKey key = ImGuiKeyMapper.MapOpenTKKeyToImGuiKey(e.Key);
        io.AddKeyEvent(key, true);


        io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
        io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);


        io.AddKeyEvent(ImGuiKey.ModSuper, e.Modifiers.HasFlag(KeyModifiers.Super));


        if (e.Key >= Keys.D0 && e.Key <= Keys.Z)
        {
            io.AddInputCharacter((uint)e.Key);
        }
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
