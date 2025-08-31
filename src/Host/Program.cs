using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

using RenderMaster.src.NewGraphics.Frame;
using RenderMaster.src.NewGraphics.Loading;
using RenderMaster.src.NewGraphics.Programs;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;

namespace RenderMaster;

public class Game : GameWindow
{
    IUserInterface userInterface = null!; // initialized in OnLoad
    Camera camera;
    Input input;

    // Renderer resources
    CPUResourceTable cpu = new();
    GPUResourceTable gpu = new();
    ResourceUploader uploader = new(new SamplerDesc(TextureMinFilter.Linear, TextureMagFilter.Linear,
        TextureWrapMode.Repeat, TextureWrapMode.Repeat));
    UploadResult map = new();
    ProgramLibrary programs = new();
    ProgramUniforms uniforms = null!;
    LoadedNodes nodes = new();

    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = title
    })
    {
        camera = new Camera(new OpenTK.Mathematics.Vector3(2, 0, 0), new OpenTK.Mathematics.Vector3(0, 0, 0),
            0.8f, (float)width / height, 1, 4000);
        input = new Input(this, camera);
    }

    static void Main(string[] args)
    {
        Game game = new Game(2560, 1440, "RENDERMASTER ENGINE");
        game.Run();
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        userInterface = new UI();

        // Initialize GL-dependent uniform buffers once context is ready
        uniforms = new ProgramUniforms();

        // Load a glTF scene if available
        var modelPath = Path.Combine(EngineConfig.ModelDirectory, "scene.gltf");
        if (File.Exists(modelPath))
        {
            var loader = new LoadFromGltfFile { filepath = modelPath };
            loader.LoadResources(cpu);
            map = uploader.UploadIncremental(cpu, gpu);
            nodes = loader.Nodes;
        }

        GL.Enable(EnableCap.DepthTest);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        input.Update(args);
        userInterface.Update(args, camera, input.MouseGrabbed);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.ClearColor(0.4f, 0.4f, 0.4f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Build frame constants and render scene
        static System.Numerics.Matrix4x4 ToNum(OpenTK.Mathematics.Matrix4 m) =>
            new(m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);

        var view = ToNum(camera.View);
        var proj = ToNum(camera.Projection);
        var viewProj = System.Numerics.Matrix4x4.Transpose(view * proj);

        var frame = new FrameBlock
        {
            ViewProj = viewProj,
            CameraWS = new System.Numerics.Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z),
            Time = (float)GLFW.GetTime()
        };

        RendererCore.Render(nodes, cpu, map, gpu, programs, uniforms, in frame);

        userInterface.Render();
        SwapBuffers();
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e) => input.OnKeyDown(e);
    protected override void OnKeyUp(KeyboardKeyEventArgs e) => input.OnKeyUp(e);
    protected override void OnTextInput(TextInputEventArgs e) => input.OnTextInput(e);
    protected override void OnMouseMove(MouseMoveEventArgs e) => input.OnMouseMove(e);
    protected override void OnMouseDown(MouseButtonEventArgs e) => input.OnMouseDown(e);
    protected override void OnMouseUp(MouseButtonEventArgs e) => input.OnMouseUp(e);
    protected override void OnMouseWheel(MouseWheelEventArgs e) => input.OnMouseWheel(e);

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        userInterface?.Resize(e);

        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(e.Width, e.Height);

        unsafe
        {
            GLFW.GetFramebufferSize(WindowPtr, out int fbWidth, out int fbHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2((float)fbWidth / e.Width, (float)fbHeight / e.Height);
            GL.Viewport(0, 0, fbWidth, fbHeight);
        }
    }
}
