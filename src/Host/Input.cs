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

    public enum ControlMode { None, Noclip, Character }
    public ControlMode Mode { get; private set; } = ControlMode.None;
    public Vector3 CharacterMovement { get; private set; }
    public bool CharacterJump { get; private set; }

    public Input(GameWindow window, Camera camera)
    {
        this.window = window;
        this.camera = camera;
    }

    public void Bind(ICommandBus commands) => _commands = commands;

    public void Update(FrameEventArgs args)
    {
        if (!MouseGrabbed)
            return;

        if (_accumulatedDeltaX != 0 || _accumulatedDeltaY != 0)
        {
            camera.ProcessMouseMovement(_accumulatedDeltaX, _accumulatedDeltaY);
            _accumulatedDeltaX = 0f;
            _accumulatedDeltaY = 0f;
        }

        switch (Mode)
        {
            case ControlMode.Noclip:
                camera.ProcessKeyboard(window.KeyboardState, (float)args.Time);
                break;
            case ControlMode.Character:
                ProcessCharacterInput();
                break;
        }
    }

    private void ProcessCharacterInput()
    {
        var ks = window.KeyboardState;
        Vector2 move = Vector2.Zero;
        if (ks.IsKeyDown(Keys.W)) move.Y += 1f;
        if (ks.IsKeyDown(Keys.S)) move.Y -= 1f;
        if (ks.IsKeyDown(Keys.A)) move.X -= 1f;
        if (ks.IsKeyDown(Keys.D)) move.X += 1f;

        var f = camera.Front; f.Y = 0; f = f.LengthSquared > 0 ? f.Normalized() : f;
        var r = OpenTK.Mathematics.Vector3.Normalize(OpenTK.Mathematics.Vector3.Cross(f, OpenTK.Mathematics.Vector3.UnitY));
        var dir = f * move.Y + r * move.X;
        dir = dir.LengthSquared > 0 ? dir.Normalized() : OpenTK.Mathematics.Vector3.Zero;
        CharacterMovement = new System.Numerics.Vector3(dir.X, 0, dir.Z);
        CharacterJump = ks.IsKeyDown(Keys.Space);
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

        if (!e.IsRepeat)
        {
            if (e.Key == Keys.Z)
                SetMode(Mode == ControlMode.Noclip ? ControlMode.None : ControlMode.Noclip);
            else if (e.Key == Keys.X)
                SetMode(Mode == ControlMode.Character ? ControlMode.None : ControlMode.Character);
        }
    }

    private void SetMode(ControlMode mode)
    {
        Mode = mode;
        MouseGrabbed = mode != ControlMode.None;
        window.CursorState = MouseGrabbed ? CursorState.Grabbed : CursorState.Normal;
        if (window.SupportsRawMouseInput)
            window.RawMouseInput = MouseGrabbed;
        _accumulatedDeltaX = 0f;
        _accumulatedDeltaY = 0f;
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
}

