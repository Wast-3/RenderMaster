using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using System.Numerics;
using System.IO;
using RenderMaster;
using RenderMaster.src.NewGraphics.Frame;
using RenderMaster.src.NewGraphics.Loading;
using RenderMaster.src.NewGraphics.Programs;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.Engine;
using RenderMaster.src.Contracts;
using RenderMaster.src.ControlPlane;
using RenderMaster.src.Physics;

namespace RenderMaster.src.Core;

// This class owns all engine systems and logic.
// It translates host window events into engine updates.
public sealed class Engine : IDisposable
{
    private readonly GameWindow _hostWindow;

    private readonly IUserInterface _userInterface;
    private readonly Camera _camera;
    private readonly Input _input;

    // Renderer resources
    private readonly CPUResourceTable _cpu = new();
    private readonly GPUResourceTable _gpu = new();
    private readonly ResourceUploader _uploader = new(new SamplerDesc(TextureMinFilter.Linear, TextureMagFilter.Linear,
        TextureWrapMode.Repeat, TextureWrapMode.Repeat));
    private UploadResult _map = new();
    private readonly ProgramLibrary _programs = new();
    private ProgramUniforms _uniforms = null!;
    private LoadedNodes _nodes = new();

    // Control Plane
    private readonly EngineControl _control;

    // Game Systems
    private readonly PhysicsEngine _physics;

    public Engine(GameWindow hostWindow)
    {
        _hostWindow = hostWindow;

        _camera = new Camera(new OpenTK.Mathematics.Vector3(2, 0, 0), new OpenTK.Mathematics.Vector3(0, 0, 0),
            0.8f, (float)hostWindow.ClientSize.X / hostWindow.ClientSize.Y, 1, 4000);
        _input = new Input(hostWindow, _camera);

        _uniforms = new ProgramUniforms();

        _physics = new PhysicsEngine();

        _control = new EngineControl(
            _programs, _nodes, _cpu, _gpu, _map);

        _userInterface = new UI(_control.Commands, _control.Queries);
    }

    // Logic formerly in OnLoad
    public void Initialize()
    {
        Logger.Log(
            $"GL_VENDOR={GL.GetString(StringName.Vendor)} GL_RENDERER={GL.GetString(StringName.Renderer)} GL_VERSION={GL.GetString(StringName.Version)}",
            LogLevel.Info);

        var modelPath = Path.Combine(EngineConfig.ModelDirectory, "AdvancedScene\\dungeon.glb");
        if (File.Exists(modelPath))
        {
            var loader = new LoadFromGltfFile { filepath = modelPath };
            loader.LoadResources(_cpu);
            _map = _uploader.UploadIncremental(_cpu, _gpu);
            _nodes = loader.Nodes;
        }
        else
        {
            throw new FileNotFoundException($"No model found at {modelPath}");
        }

        _physics.Setup();

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.FramebufferSrgb);

        OnResize(new ResizeEventArgs(_hostWindow.ClientSize.X, _hostWindow.ClientSize.Y));

        var caps = new EngineCapabilities(
            ApiMajor: 1,
            SupportedCommands: new[] { nameof(ReloadShaders), nameof(SelectNode), nameof(ChangeMaterial), nameof(SetMaterialParam) },
            AvailableQueries: new[] { nameof(GetSceneGraph), nameof(GetMaterials), nameof(GetNodeSnapshot) });
    }

    // Logic formerly in OnUpdateFrame
    public void Update(FrameEventArgs args)
    {
        _programs.PumpHotReload();

        int processed = _control.DrainCommands();
        if (processed > 0)
            Logger.Log($"Processed {processed} commands", LogLevel.Debug);

        ProcessEngineEvents();

        _input.Update(args);

        _physics.Update(args, (float)args.Time);

        _userInterface.Update(args, _camera, _input.MouseGrabbed);

        _nodes.UpdateWorldTransforms();
        _control.RebuildProjections();

        if ((int)GLFW.GetTime() % 5 == 0)
        {
            var p = _camera.Position;
            Logger.Log(
                $"Camera pos=({p.X:F2},{p.Y:F2},{p.Z:F2}) yaw={_camera.Yaw:F1} pitch={_camera.Pitch:F1}",
                LogLevel.Debug);
        }
    }

    // Logic formerly in OnRenderFrame
    public void Render(FrameEventArgs args)
    {
        var view = ToNum(_camera.View);
        var proj = ToNum(_camera.Projection);

        if (MatrixHasNaN(view) || MatrixHasNaN(proj))
            Logger.Log($"NaN in view/proj! view={view} proj={proj}", LogLevel.Error);

        var viewProj = view * proj;

        var frame = new FrameBlock
        {
            ViewProj = viewProj,
            CameraWS = new Vector3(_camera.Position.X, _camera.Position.Y, _camera.Position.Z),
            Time = (float)GLFW.GetTime()
        };

        RendererCore.Render(_nodes, _cpu, _map, _gpu, _programs, _uniforms, in frame);

        _userInterface.Render();
    }

    private void ProcessEngineEvents()
    {
        while (_control.Events.TryRead(out var ev))
        {
            switch (ev)
            {
                case NodeSelected:
                    break;

                case CameraFocusRequested cf:
                {
                    var snap = _control.Debug.GetNodeSnapshot(cf.Target);

                    var tx = snap.World.M41; var ty = snap.World.M42; var tz = snap.World.M43;
                    var target = new OpenTK.Mathematics.Vector3(tx, ty, tz);

                    var forward = new OpenTK.Mathematics.Vector3(
                        (float)System.Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(_camera.Yaw)) *
                        (float)System.Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(_camera.Pitch)),
                        (float)System.Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(_camera.Pitch)),
                        (float)System.Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(_camera.Yaw)) *
                        (float)System.Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(_camera.Pitch)));

                    var pos = target - forward.Normalized() * cf.Distance;
                    _camera.Position = pos;
                    _camera.UpdateViewMatrix();
                    break;
                }
            }
        }
    }

    #region Input and Host Event Delegation
    public void OnKeyDown(KeyboardKeyEventArgs e) => _input.OnKeyDown(e);
    public void OnKeyUp(KeyboardKeyEventArgs e) => _input.OnKeyUp(e);
    public void OnTextInput(TextInputEventArgs e) => _input.OnTextInput(e);
    public void OnMouseMove(MouseMoveEventArgs e) => _input.OnMouseMove(e);
    public void OnMouseDown(MouseButtonEventArgs e) => _input.OnMouseDown(e);
    public void OnMouseUp(MouseButtonEventArgs e) => _input.OnMouseUp(e);
    public void OnMouseWheel(MouseWheelEventArgs e) => _input.OnMouseWheel(e);

    public void OnResize(ResizeEventArgs e)
    {
        _userInterface?.Resize(e);

        var io = ImGuiNET.ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(e.Width, e.Height);

        unsafe
        {
            GLFW.GetFramebufferSize(_hostWindow.WindowPtr, out int fbWidth, out int fbHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2((float)fbWidth / e.Width, (float)fbHeight / e.Height);
            GL.Viewport(0, 0, fbWidth, fbHeight);
            _camera.UpdateAspectRatio((float)fbWidth / fbHeight);
        }

        Logger.Log(
            $"Resize: win=({e.Width}x{e.Height}) fb=({io.DisplayFramebufferScale.X * e.Width}x{io.DisplayFramebufferScale.Y * e.Height}) scale=({io.DisplayFramebufferScale.X:F2},{io.DisplayFramebufferScale.Y:F2})",
            LogLevel.Debug);
    }
    #endregion

    #region Helpers
    static Matrix4x4 ToNum(OpenTK.Mathematics.Matrix4 m) =>
        new(m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44);

    static bool MatrixHasNaN(in Matrix4x4 m) =>
        !(float.IsFinite(m.M11) && float.IsFinite(m.M22) && float.IsFinite(m.M33) && float.IsFinite(m.M44));
    #endregion

    public void Dispose()
    {
        _control.Dispose();
        _uniforms.Dispose();
    }
}

