using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace RenderMaster;



public class Camera
{

    public Vector3 Position { get; set; }
    public Vector3 LookingAt { get; set; }
    public Matrix4 View { get; set; }
    public Matrix4 Projection { get; set; }

    Vector3 Up = new Vector3(0f, 1f, 0f);

    public Camera(Vector3 position, Vector3 lookingAt, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        Position = position;
        LookingAt = lookingAt;


        UpdateViewMatrix();


        SetPerspectiveProjection(fieldOfView, aspectRatio, nearPlane, farPlane);
    }



    public void UpdateViewMatrix()
    {
        Vector3 up = Vector3.UnitY;
        View = Matrix4.LookAt(Position, LookingAt, up);
    }



    public void SetPerspectiveProjection(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        Projection = Matrix4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlane, farPlane);

    }



    public void ProcessKeyboard(KeyboardState input, float deltaTime)
    {
        float moveSpeed = 5f * deltaTime;

        if (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift))
        {
            moveSpeed *= 5.0f;
        }

        Vector3 forward = LookingAt - Position;

        if (forward.Length > 0)
        {
            forward = Vector3.Normalize(forward);

            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Up));

            if (input.IsKeyDown(Keys.W))
            {
                Position += moveSpeed * forward;
            }
            if (input.IsKeyDown(Keys.S))
            {
                Position -= moveSpeed * forward;
            }
            if (input.IsKeyDown(Keys.A))
            {
                Position -= moveSpeed * right;
            }
            if (input.IsKeyDown(Keys.D))
            {
                Position += moveSpeed * right;
            }
            if (input.IsKeyDown(Keys.Space))
            {
                Position += moveSpeed * Up;
            }
            if (input.IsKeyDown(Keys.LeftControl) || input.IsKeyDown(Keys.RightControl))
            {
                Position -= moveSpeed * Up;
            }

            UpdateViewMatrix();
        }
    }
}
