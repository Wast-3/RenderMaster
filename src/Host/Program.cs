using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using EngineCore = RenderMaster.src.Core.Engine;

namespace RenderMaster;

public class Game : GameWindow
{
    private EngineCore _engine = null!;

    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = title,
        Flags = ContextFlags.ForwardCompatible,
        Profile = ContextProfile.Core,
        APIVersion = new Version(4, 5)
    })
    {
    }

    static void Main(string[] args)
    {
        Game game = new Game(2560, 1440, "RENDERMASTER ENGINE");
        game.Run();
    }

    protected override void OnLoad()
    {
        base.OnLoad();

#if DEBUG
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageCallback((src, type, id, severity, len, msg, user) =>
            {
                var txt = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(msg, len);
                RenderMaster.Engine.Logger.Log($"GL DEBUG [{severity}] {type}/{src} #{id}: {txt}", RenderMaster.Engine.LogLevel.Debug);
            }, IntPtr.Zero);
        }
#endif

        _engine = new EngineCore(this);
        _engine.Initialize();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        _engine.Update(args);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.ClearColor(0.4f, 0.4f, 0.4f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _engine.Render(args);

        SwapBuffers();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e) => _engine.OnKeyDown(e);
    protected override void OnKeyUp(KeyboardKeyEventArgs e) => _engine.OnKeyUp(e);
    protected override void OnTextInput(TextInputEventArgs e) => _engine.OnTextInput(e);
    protected override void OnMouseMove(MouseMoveEventArgs e) => _engine.OnMouseMove(e);
    protected override void OnMouseDown(MouseButtonEventArgs e) => _engine.OnMouseDown(e);
    protected override void OnMouseUp(MouseButtonEventArgs e) => _engine.OnMouseUp(e);
    protected override void OnMouseWheel(MouseWheelEventArgs e) => _engine.OnMouseWheel(e);

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        _engine.OnResize(e);
    }
}
