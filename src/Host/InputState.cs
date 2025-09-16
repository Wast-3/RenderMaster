using System.Numerics;

namespace RenderMaster;

public enum MovementMode
{
    Noclip,
    Character
}

public readonly struct InputState
{
    public MovementMode Mode { get; }
    public bool CaptureActive { get; }
    public Vector2 Movement { get; }
    public bool JumpPressed { get; }
    public bool JumpHeld { get; }
    public bool SprintHeld { get; }

    public InputState(MovementMode mode, bool captureActive, Vector2 movement, bool jumpPressed, bool jumpHeld, bool sprintHeld)
    {
        Mode = mode;
        CaptureActive = captureActive;
        Movement = movement;
        JumpPressed = jumpPressed;
        JumpHeld = jumpHeld;
        SprintHeld = sprintHeld;
    }
}
