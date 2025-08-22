using OpenTK.Windowing.Common;

namespace RenderMaster
{

    public interface IRenderer
    {
        public void Render(FrameEventArgs e, Camera camera);
    }
}
