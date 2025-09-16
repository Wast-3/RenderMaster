using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RenderMaster;

public class Camera
{
    public Vector3 Position { get; set; }
    public Matrix4 View { get; private set; }
    public Matrix4 Projection { get; private set; }

    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public bool InvertY { get; set; } = false;

    public float MovementSpeed { get; private set; } = 5f;
    public float MouseSensitivity { get; private set; } = 0.2f;

    public float FieldOfView { get; private set; }
    public float NearPlane   { get; private set; }
    public float FarPlane    { get; private set; }
    public float AspectRatio { get; private set; }

    Vector3 front = -Vector3.UnitZ;
    Vector3 up = Vector3.UnitY;
    Vector3 right = Vector3.UnitX;

    public Vector3 Front => front;
    public Vector3 Up => up;
    public Vector3 Right => right;

    public Camera(Vector3 position, Vector3 lookingAt, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        Position = position;
        front = Vector3.Normalize(lookingAt - position);
        Yaw = MathHelper.RadiansToDegrees(MathF.Atan2(front.Z, front.X));
        Pitch = MathHelper.RadiansToDegrees(MathF.Asin(front.Y));
        UpdateCameraVectors();
        SetPerspectiveProjection(fieldOfView, aspectRatio, nearPlane, farPlane);
        UpdateViewMatrix();
    }

    public void UpdateViewMatrix()
    {
        View = Matrix4.LookAt(Position, Position + front, up);
    }

    public void SetPerspectiveProjection(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        FieldOfView = fieldOfView;
        AspectRatio = aspectRatio;
        NearPlane   = nearPlane;
        FarPlane    = farPlane;
        UpdateProjection();
    }

    public void UpdateProjection()
    {
        Projection = Matrix4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
    }

    public void UpdateAspectRatio(float aspectRatio)
    {
        AspectRatio = aspectRatio;
        UpdateProjection();
    }

    void UpdateCameraVectors()
    {
        Vector3 f;
        f.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
        f.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
        f.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
        front = Vector3.Normalize(f);
        right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
        up = Vector3.Normalize(Vector3.Cross(right, front));
    }

    public void ProcessKeyboard(KeyboardState input, float deltaTime)
    {
        float velocity = MovementSpeed * deltaTime;
        if (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift))
            velocity *= 5f;

        if (input.IsKeyDown(Keys.W))
            Position += front * velocity;
        if (input.IsKeyDown(Keys.S))
            Position -= front * velocity;
        if (input.IsKeyDown(Keys.A))
            Position -= right * velocity;
        if (input.IsKeyDown(Keys.D))
            Position += right * velocity;
        if (input.IsKeyDown(Keys.Space))
            Position += up * velocity;
        if (input.IsKeyDown(Keys.LeftControl) || input.IsKeyDown(Keys.RightControl))
            Position -= up * velocity;

        UpdateViewMatrix();
    }

    public void ProcessMouseMovement(float deltaX, float deltaY)
    {
        deltaX *= MouseSensitivity;
        deltaY *= MouseSensitivity;

        Yaw += deltaX;
        Pitch += InvertY ? deltaY : -deltaY;
        Pitch = MathHelper.Clamp(Pitch, -89f, 89f);

        UpdateCameraVectors();
        UpdateViewMatrix();
    }

    public void ProcessMouseScroll(float offset)
    {
        MovementSpeed = MathF.Max(0.1f, MovementSpeed + offset);
    }
}
