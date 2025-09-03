using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderMaster.src.ControlPlane;
using RenderMaster.src.Contracts;
using System;
using System.Numerics;

namespace RenderMaster;

public class Input
{
    private readonly GameWindow window;
    private readonly Camera camera;
    private ICommandBus? _commands;

    private float _accumulatedDeltaX;
    private float _accumulatedDeltaY;

    public bool MouseGrabbed { get; private set; } = false;

    public Input(GameWindow window, Camera camera)
    {
        this.window = window;
        this.camera = camera;
    }

    public void Bind(ICommandBus commands) => _commands = commands;

    public void Update(FrameEventArgs args)
    {
        if (MouseGrabbed)
        {
            camera.ProcessKeyboard(window.KeyboardState, (float)args.Time);

            if (_accumulatedDeltaX != 0 || _accumulatedDeltaY != 0)
            {
                camera.ProcessMouseMovement(_accumulatedDeltaX, _accumulatedDeltaY);
                _accumulatedDeltaX = 0f;
                _accumulatedDeltaY = 0f;
            }
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
            _accumulatedDeltaX += e.DeltaX;
            _accumulatedDeltaY += e.DeltaY;
        }
    }

    public void OnMouseDown(MouseButtonEventArgs e)
    {
        var io = ImGui.GetIO();
        io.AddMouseButtonEvent((int)e.Button, true);
        if (e.Button == MouseButton.Left && _commands != null && !io.WantCaptureMouse)
        {
            var pos = camera.Position;
            var origin = new Vector3(pos.X, pos.Y, pos.Z);
            var f = camera.Front;
            var dir = Vector3.Normalize(new Vector3(f.X, f.Y, f.Z));
            _commands.Post(new FireProjectile(Guid.NewGuid(), origin, dir));
        }
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
        window.CursorState = MouseGrabbed ? CursorState.Grabbed : CursorState.Normal;

        // Raw mouse works only when grabbed; guard by capability.
        if (window.SupportsRawMouseInput)
            window.RawMouseInput = MouseGrabbed;

        // Optional: when grabbing, clear any accumulated deltas so we don't
        // apply a big jump from the last OS-accelerated movement.
        _accumulatedDeltaX = 0f;
        _accumulatedDeltaY = 0f;
    }
}

