using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;
using System.Numerics;

using RenderMaster.src.NewGraphics.Frame;
using RenderMaster.src.NewGraphics.Loading;
using RenderMaster.src.NewGraphics.Programs;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.Engine;

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

        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback((src, type, id, severity, len, msg, user) =>
        {
            var txt = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(msg, len);
            RenderMaster.Engine.Logger.Log($"GL DEBUG [{severity}] {type}/{src} #{id}: {txt}", RenderMaster.Engine.LogLevel.Debug);
        }, IntPtr.Zero);

        RenderMaster.Engine.Logger.Log(
            $"GL_VENDOR={GL.GetString(StringName.Vendor)} GL_RENDERER={GL.GetString(StringName.Renderer)} GL_VERSION={GL.GetString(StringName.Version)}",
            RenderMaster.Engine.LogLevel.Info);

        // Initialize GL-dependent uniform buffers once context is ready
        uniforms = new ProgramUniforms();

        // Load a glTF scene if available
        var modelPath = Path.Combine(EngineConfig.ModelDirectory, "gltfWorkflowTesting\\scene.glb");
        if (File.Exists(modelPath))
        {
            var loader = new LoadFromGltfFile { filepath = modelPath };
            loader.LoadResources(cpu);
            map = uploader.UploadIncremental(cpu, gpu);
            nodes = loader.Nodes;
        }
        else
        {
            throw new FileNotFoundException($"No model found at {modelPath}");
        }

        GL.Enable(EnableCap.DepthTest);

        // Ensure the initial frame uses the correct framebuffer size and aspect
        // ratio. On some high-DPI systems the first render can occur before a
        // resize event fires, leaving the camera with the logical window size
        // instead of the actual framebuffer dimensions.
        var winSize = ClientSize;
        userInterface.Resize(new ResizeEventArgs(winSize.X, winSize.Y));
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(winSize.X, winSize.Y);

        unsafe
        {
            GLFW.GetFramebufferSize(WindowPtr, out int fbWidth, out int fbHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2((float)fbWidth / winSize.X,
                (float)fbHeight / winSize.Y);
            GL.Viewport(0, 0, fbWidth, fbHeight);
            camera.UpdateAspectRatio((float)fbWidth / fbHeight);
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        input.Update(args);
        userInterface.Update(args, camera, input.MouseGrabbed);

        if ((int)GLFW.GetTime() % 5 == 0)
        {
            var p = camera.Position;
            RenderMaster.Engine.Logger.Log(
                $"Camera pos=({p.X:F2},{p.Y:F2},{p.Z:F2}) yaw={camera.Yaw:F1} pitch={camera.Pitch:F1}",
                RenderMaster.Engine.LogLevel.Debug);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        GL.ClearColor(0.4f, 0.4f, 0.4f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Build frame constants and render scene
        static Matrix4x4 ToNum(OpenTK.Mathematics.Matrix4 m) =>
            new(m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);

        var view = ToNum(camera.View);
        var proj = ToNum(camera.Projection);

        static bool MatrixHasNaN(in Matrix4x4 m) =>
            !(float.IsFinite(m.M11) && float.IsFinite(m.M22) && float.IsFinite(m.M33) && float.IsFinite(m.M44));

        if (MatrixHasNaN(view) || MatrixHasNaN(proj))
            RenderMaster.Engine.Logger.Log($"NaN in view/proj! view={view} proj={proj}", RenderMaster.Engine.LogLevel.Error);

        // correct for GLSL column-major consuming VP in the shader
        // GL expects column-major with projection preceding view
        var viewProj = Matrix4x4.Transpose(proj * view);

        var frame = new FrameBlock
        {
            ViewProj = viewProj,
            CameraWS = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z),
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
            camera.UpdateAspectRatio((float)fbWidth / fbHeight);
        }

        RenderMaster.Engine.Logger.Log(
            $"Resize: win=({e.Width}x{e.Height}) fb=({io.DisplayFramebufferScale.X * e.Width}x{io.DisplayFramebufferScale.Y * e.Height}) scale=({io.DisplayFramebufferScale.X:F2},{io.DisplayFramebufferScale.Y:F2})",
            RenderMaster.Engine.LogLevel.Debug);
    }
}
