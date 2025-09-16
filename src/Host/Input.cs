using ImGuiNET;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Numerics;

namespace RenderMaster;

public class Input
{
    private readonly GameWindow window;
    private readonly Camera camera;

    private float _accumulatedDeltaX;
    private float _accumulatedDeltaY;

    public bool MouseGrabbed { get; private set; } = false;

    public MovementMode Mode { get; private set; } = MovementMode.Noclip;
    public bool RequestedCharacterSnap { get; private set; }
    bool _previousJumpDown;
    Vector2 _movement;
    bool _jumpPressed;
    bool _jumpHeld;
    bool _sprintHeld;
    InputState _state;

    public Input(GameWindow window, Camera camera)
    {
        this.window = window;
        this.camera = camera;
    }

    public void Update(FrameEventArgs args)
    {
        if (MouseGrabbed)
        {
            if (Mode == MovementMode.Noclip)
            {
                camera.ProcessKeyboard(window.KeyboardState, (float)args.Time);
                _movement = Vector2.Zero;
                _jumpPressed = false;
                _jumpHeld = false;
                _sprintHeld = false;
            }
            else
            {
                GatherCharacterInput();
            }

            if (_accumulatedDeltaX != 0 || _accumulatedDeltaY != 0)
            {
                camera.ProcessMouseMovement(_accumulatedDeltaX, _accumulatedDeltaY);
                _accumulatedDeltaX = 0f;
                _accumulatedDeltaY = 0f;
            }
        }
        else
        {
            _movement = Vector2.Zero;
            _jumpPressed = false;
            _jumpHeld = false;
            _sprintHeld = false;
            _previousJumpDown = false;
        }

        RebuildState();
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
            switch (e.Key)
            {
                case Keys.Z:
                    Mode = Mode == MovementMode.Noclip ? MovementMode.Character : MovementMode.Noclip;
                    if (!MouseGrabbed)
                        ToggleMouseGrab();
                    break;
                case Keys.X:
                    RequestedCharacterSnap = true;
                    Mode = MovementMode.Character;
                    break;
                case Keys.Escape:
                    if (MouseGrabbed)
                        ToggleMouseGrab();
                    break;
            }
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

    public void AcknowledgeCharacterSnap()
    {
        RequestedCharacterSnap = false;
    }

    public void SetMode(MovementMode mode)
    {
        Mode = mode;
        RebuildState();
    }

    public void SetMouseGrab(bool grab)
    {
        if (MouseGrabbed != grab)
        {
            ToggleMouseGrab();
        }
        RebuildState();
    }

    public InputState State => _state;

    public void RebuildState()
    {
        _state = new InputState(Mode, MouseGrabbed, _movement, _jumpPressed, _jumpHeld, _sprintHeld);
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

    void GatherCharacterInput()
    {
        var keyboard = window.KeyboardState;
        Vector2 movement = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W))
            movement += new Vector2(0, 1);
        if (keyboard.IsKeyDown(Keys.S))
            movement += new Vector2(0, -1);
        if (keyboard.IsKeyDown(Keys.A))
            movement += new Vector2(-1, 0);
        if (keyboard.IsKeyDown(Keys.D))
            movement += new Vector2(1, 0);

        var lengthSquared = movement.LengthSquared();
        if (lengthSquared > 1f)
        {
            movement /= MathF.Sqrt(lengthSquared);
        }

        var jumpDown = keyboard.IsKeyDown(Keys.Space);
        _jumpPressed = jumpDown && !_previousJumpDown;
        _jumpHeld = jumpDown;
        _previousJumpDown = jumpDown;

        _sprintHeld = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        _movement = movement;
    }
}

