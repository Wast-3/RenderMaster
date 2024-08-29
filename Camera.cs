using OpenTK.Windowing.Common;  // Provides common types used in OpenTK windowing, such as FrameEventArgs
using OpenTK.Mathematics;       // Provides mathematical types such as Vector3 and Matrix4
using OpenTK.Windowing.GraphicsLibraryFramework; // Provides access to keyboard and mouse input events

namespace RenderMaster
{
    // The Camera class represents a camera in 3D space. It manages the camera's position, 
    // the direction it is looking at, and provides methods to control the view and projection matrices.
    public class Camera
    {
        // Public properties to get and set the camera's position and the point it's looking at
        public Vector3 Position { get; set; }      // Camera position in 3D space
        public Vector3 LookingAt { get; set; }     // The point in 3D space the camera is looking at
        public Matrix4 View { get; set; }          // The view matrix, representing the camera's orientation and position
        public Matrix4 Projection { get; set; }    // The projection matrix, defining how 3D objects are projected onto the 2D screen

        // A vector pointing upwards, used to maintain the camera's orientation
        Vector3 Up = new Vector3(0f, 1f, 0f);      // Default up vector pointing along the positive Y-axis

        // Constructor for the Camera class.
        // Initializes the camera's position, the point it is looking at, and sets up the view and projection matrices.
        public Camera(Vector3 position, Vector3 lookingAt, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
        {
            Position = position;
            LookingAt = lookingAt;

            // Initialize the view matrix based on the camera's position and target
            UpdateViewMatrix();

            // Set up a perspective projection matrix with the given parameters
            SetPerspectiveProjection(fieldOfView, aspectRatio, nearPlane, farPlane);
        }

        // Method to update the view matrix.
        // The view matrix is recalculated based on the current camera position and the point it's looking at.
        public void UpdateViewMatrix()
        {
            Vector3 up = Vector3.UnitY; // Standard up vector, assuming the world's Y-axis is "up"
            View = Matrix4.LookAt(Position, LookingAt, up); // Create a look-at view matrix
        }

        // Method to set up the perspective projection matrix.
        // This matrix controls how the 3D scene is projected onto the 2D screen.
        public void SetPerspectiveProjection(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
        {
            Projection = Matrix4.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlane, farPlane);
            // The CreatePerspectiveFieldOfView method creates a projection matrix based on the field of view, aspect ratio, and clipping planes
        }

        // Method to handle camera movement based on keyboard input.
        // This method processes key events and moves the camera accordingly.
        public void ProcessKeyEvents(KeyboardKeyEventArgs e)
        {
            float moveSpeed = 0.2f; // Base movement speed

            // If the Shift key is held down, increase movement speed
            if (e.Shift)
            {
                moveSpeed *= 5.0f; // Increase speed by a factor of 5 when Shift is held
            }

            // Calculate the forward direction vector by subtracting the camera position from the looking-at point
            Vector3 forward = LookingAt - Position;

            // Normalize the forward direction to ensure consistent movement
            if (forward.Length > 0)
            {
                forward = Vector3.Normalize(forward);

                // Calculate the right direction vector using the cross product of forward and up vectors
                Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Up));

                // Handle different key presses to move the camera
                if (e.Key == Keys.W)
                {
                    Position += moveSpeed * forward; // Move forward
                }
                if (e.Key == Keys.S)
                {
                    Position -= moveSpeed * forward; // Move backward
                }
                if (e.Key == Keys.A)
                {
                    Position -= moveSpeed * right; // Move left
                }
                if (e.Key == Keys.D)
                {
                    Position += moveSpeed * right; // Move right
                }
                if (e.Key == Keys.Space) // Move up if space key is pressed
                {
                    Position += moveSpeed * Up; // Move up
                }
                if (e.Key == Keys.LeftControl || e.Key == Keys.RightControl) // Move down if either control key is pressed
                {
                    Position -= moveSpeed * Up; // Move down
                }

                // Update the view matrix after moving the camera
                UpdateViewMatrix();
            }
        }
    }
}
