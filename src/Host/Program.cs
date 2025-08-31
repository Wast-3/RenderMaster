using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderMaster.Engine;
using RenderMaster.src.Physics;

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
    Input input;

    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {

        ClientSize = (width, height),
        Title = title
    })
    {
        this.mainScene = new Scene("main testing scene", width, height);
        input = new Input(this, mainScene.camera);

        physicsEngine.Setup();

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

        input.Update(args);

        userInterface.Update(args, this.mainScene.camera, input.MouseGrabbed);
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
        input.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyboardKeyEventArgs e)
    {
        input.OnKeyUp(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        input.OnTextInput(e);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        input.OnMouseMove(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        input.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        input.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        input.OnMouseWheel(e);
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

        unsafe
        {
            GLFW.GetFramebufferSize(WindowPtr, out int fbWidth, out int fbHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2((float)fbWidth / e.Width, (float)fbHeight / e.Height);
            GL.Viewport(0, 0, fbWidth, fbHeight);
        }
    }

}
