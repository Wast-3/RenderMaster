using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RenderMaster;

public class Game : GameWindow
{
    IUserInterface userInterface = null!; // initialized in OnLoad
    Camera camera;
    Input input;

    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = title
    })
    {
        camera = new Camera(new OpenTK.Mathematics.Vector3(2, 0, 0), new OpenTK.Mathematics.Vector3(0, 0, 0),
            0.8f, (float)width / height, 1, 4000);
        input = new Input(this, camera);
    }

    static void Main(string[] args)
    {
        Game game = new Game(2560, 1440, "RENDERMASTER ENGINE");
        game.Run();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        userInterface = new UI();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        input.Update(args);
        userInterface.Update(args, camera, input.MouseGrabbed);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.ClearColor(0.4f, 0.4f, 0.4f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        userInterface.Render();
        SwapBuffers();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e) => input.OnKeyDown(e);
    protected override void OnKeyUp(KeyboardKeyEventArgs e) => input.OnKeyUp(e);
    protected override void OnTextInput(TextInputEventArgs e) => input.OnTextInput(e);
    protected override void OnMouseMove(MouseMoveEventArgs e) => input.OnMouseMove(e);
    protected override void OnMouseDown(MouseButtonEventArgs e) => input.OnMouseDown(e);
    protected override void OnMouseUp(MouseButtonEventArgs e) => input.OnMouseUp(e);
    protected override void OnMouseWheel(MouseWheelEventArgs e) => input.OnMouseWheel(e);

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
