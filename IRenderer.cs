using OpenTK.Windowing.Common;  // Provides types used in OpenTK windowing, such as FrameEventArgs

namespace RenderMaster
{
    // Interface for rendering objects. Any class implementing this interface must provide a Render method.
    public interface IRenderer
    {
        public void Render(FrameEventArgs e, Camera camera);  // Method to render the object using the provided FrameEventArgs and Camera
    }
}
