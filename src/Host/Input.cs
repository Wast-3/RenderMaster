using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RenderMaster;

public class Input
{
    private readonly GameWindow window;
    private readonly Camera camera;

    public bool MouseGrabbed { get; private set; } = false;

    public Input(GameWindow window, Camera camera)
    {
        this.window = window;
        this.camera = camera;
    }

    public void Update(FrameEventArgs args)
    {
        if (MouseGrabbed)
        {
            camera.ProcessKeyboard(window.KeyboardState, (float)args.Time);
        }
    }

    public void OnKeyDown(KeyboardKeyEventArgs e)
    {
        var io = ImGui.GetIO();
        ImGuiKey key = ImGuiKeyMapper.MapOpenTKKeyToImGuiKey(e.Key);
        io.AddKeyEvent(key, true);

        io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
        io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);
        io.AddKeyEvent(ImGuiKey.ModSuper, e.Modifiers.HasFlag(KeyModifiers.Super));

        if (e.Key == Keys.Z && !e.IsRepeat)
        {
            ToggleMouseGrab();
        }
    }

    public void OnKeyUp(KeyboardKeyEventArgs e)
    {
        var io = ImGui.GetIO();
        ImGuiKey key = ImGuiKeyMapper.MapOpenTKKeyToImGuiKey(e.Key);
        io.AddKeyEvent(key, false);

        io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
        io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
        io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);
        io.AddKeyEvent(ImGuiKey.ModSuper, e.Modifiers.HasFlag(KeyModifiers.Super));
    }

    public void OnTextInput(TextInputEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddInputCharacter((uint)e.Unicode);
    }

    public void OnMouseMove(MouseMoveEventArgs e)
    {
        var io = ImGui.GetIO();
        float scaleFactorY = io.DisplayFramebufferScale.Y;
        float scaleFactorX = io.DisplayFramebufferScale.X;
        io.MousePos = new System.Numerics.Vector2(window.MouseState.X * scaleFactorX, window.MouseState.Y * scaleFactorY);

        if (MouseGrabbed)
        {
            camera.ProcessMouseMovement(e.DeltaX, e.DeltaY);
        }
    }

    public void OnMouseDown(MouseButtonEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddMouseButtonEvent((int)e.Button, true);
    }

    public void OnMouseUp(MouseButtonEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddMouseButtonEvent((int)e.Button, false);
    }

    public void OnMouseWheel(MouseWheelEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddMouseWheelEvent(e.OffsetX, e.OffsetY);
        camera.ProcessMouseScroll(e.OffsetY);
    }

    private void ToggleMouseGrab()
    {
        MouseGrabbed = !MouseGrabbed;
        window.CursorGrabbed = MouseGrabbed;
        window.CursorVisible = !MouseGrabbed;
    }
}

