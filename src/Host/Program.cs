using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;
using System.Numerics;
using System.Collections.Generic;

using RenderMaster.src.NewGraphics.Frame;
using RenderMaster.src.NewGraphics.Loading;
using RenderMaster.src.NewGraphics.Programs;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.Engine;
using RenderMaster.src.Contracts;
using RenderMaster.src.ControlPlane;
using RenderMaster.src.NewGraphics.Types;

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
    private EngineControl _control = null!;

    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = title,
        Flags = ContextFlags.ForwardCompatible,
        Profile = ContextProfile.Core,
        APIVersion = new Version(4, 5)
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

        //if we're on debug build, otherwise disable
#if DEBUG
        {
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            GL.DebugMessageCallback((src, type, id, severity, len, msg, user) =>
            {
                var txt = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(msg, len);
                RenderMaster.Engine.Logger.Log($"GL DEBUG [{severity}] {type}/{src} #{id}: {txt}", RenderMaster.Engine.LogLevel.Debug);
            }, IntPtr.Zero);
        }
#endif
        RenderMaster.Engine.Logger.Log(
            $"GL_VENDOR={GL.GetString(StringName.Vendor)} GL_RENDERER={GL.GetString(StringName.Renderer)} GL_VERSION={GL.GetString(StringName.Version)}",
            RenderMaster.Engine.LogLevel.Info);

        // Initialize GL-dependent uniform buffers once context is ready
        uniforms = new ProgramUniforms();

        // Load a glTF scene if available
        var modelPath = Path.Combine(EngineConfig.ModelDirectory, "AdvancedScene\\dungeon.glb");
        if (File.Exists(modelPath))
        {
            var loader = new LoadFromGltfFile { filepath = modelPath };
            loader.LoadResources(cpu);

            var projectileMesh = BuildProjectileMesh(0.25f, 16, 16);
            var projMeshHandle = cpu.AddMeshBuffer(projectileMesh);
            var projectileMaterial = new MaterialCPU { BaseColorFactor = new Vector4(1f, 1f, 1f, 1f) };
            var projMatHandle = cpu.AddMaterial(projectileMaterial);
            var projSpan = projectileMesh.Submeshes[0];

            map = uploader.UploadIncremental(cpu, gpu);
            nodes = loader.Nodes;

            _control = new EngineControl(
                programs, nodes, cpu, gpu, map, projMeshHandle, projMatHandle, projSpan);
            input.Bind(_control.Commands);
        }
        else
        {
            throw new FileNotFoundException($"No model found at {modelPath}");
        }
        GL.Enable(EnableCap.DepthTest);
        // Enable automatic sRGB conversion when writing to the default framebuffer
        GL.Enable(EnableCap.FramebufferSrgb);

        userInterface = new UI(_control.Commands, _control.Queries);

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

        // Optionally: expose capabilities for adapters (if you have a bridge)
        var caps = new EngineCapabilities(
            ApiMajor: 1,
            SupportedCommands: new[] { nameof(ReloadShaders),
                                   nameof(SelectNode),
                                   nameof(ChangeMaterial),
                                   nameof(SetMaterialParam) },
            AvailableQueries: new[] { nameof(GetSceneGraph),
                                nameof(GetMaterials),
                                nameof(GetNodeSnapshot) });
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        programs.PumpHotReload();

        // Process all posted commands deterministically on this thread.
        int processed = _control.DrainCommands();
        if (processed > 0)
            RenderMaster.Engine.Logger.Log($"Processed {processed} commands", RenderMaster.Engine.LogLevel.Debug);

        while (_control.Events.TryRead(out var ev))
        {
            switch (ev)
            {
                case NodeSelected ns:
                    break;

                case CameraFocusRequested cf:
                    {
                        var snap = _control.Debug.GetNodeSnapshot(cf.Target);

                        var tx = snap.World.M41; var ty = snap.World.M42; var tz = snap.World.M43;
                        var target = new OpenTK.Mathematics.Vector3(tx, ty, tz);

                        var forward = new OpenTK.Mathematics.Vector3(
                            (float)System.Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(camera.Yaw)) *
                            (float)System.Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(camera.Pitch)),
                            (float)System.Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(camera.Pitch)),
                            (float)System.Math.Sin(OpenTK.Mathematics.MathHelper.DegreesToRadians(camera.Yaw)) *
                            (float)System.Math.Cos(OpenTK.Mathematics.MathHelper.DegreesToRadians(camera.Pitch)));

                        var pos = target - forward.Normalized() * cf.Distance;
                        camera.Position = pos;
                        camera.UpdateViewMatrix();
                        break;
                    }
            }
        }
        // === end event pump ===

        input.Update(args);
        if (input.Mode == Input.ControlMode.Character)
        {
            _control.EnsurePlayerBody(new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            var p = _control.UpdatePlayer(input.CharacterMovement, input.CharacterJump, (float)args.Time);
            camera.Position = new OpenTK.Mathematics.Vector3(p.X, p.Y + 1f, p.Z);
            camera.UpdateViewMatrix();
        }

        _control.Simulate((float)args.Time);
        userInterface.Update(args, camera, input.MouseGrabbed);

        // Update scene graph transforms so other systems see current world matrices.
        nodes.UpdateWorldTransforms();
        _control.RebuildProjections();

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
        // GL expects column-major matrices; transpose each part then
        // multiply in projection * view order
        var viewProj = view * proj;

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

    private static PreparedMeshBuffer BuildProjectileMesh(float radius, int latSegments, int lonSegments)
    {
        var verts = new List<float>();
        var indices = new List<int>();
        for (int lat = 0; lat <= latSegments; lat++)
        {
            float theta = lat * MathF.PI / latSegments;
            float sinTheta = MathF.Sin(theta);
            float cosTheta = MathF.Cos(theta);
            for (int lon = 0; lon <= lonSegments; lon++)
            {
                float phi = lon * 2 * MathF.PI / lonSegments;
                float sinPhi = MathF.Sin(phi);
                float cosPhi = MathF.Cos(phi);
                float x = cosPhi * sinTheta;
                float y = cosTheta;
                float z = sinPhi * sinTheta;
                var normal = new System.Numerics.Vector3(x, y, z);
                var tangent = Vector3.Normalize(new System.Numerics.Vector3(-sinPhi, 0, cosPhi));
                float u = (float)lon / lonSegments;
                float v = (float)lat / latSegments;

                verts.Add(radius * x); verts.Add(radius * y); verts.Add(radius * z);
                verts.Add(normal.X); verts.Add(normal.Y); verts.Add(normal.Z);
                verts.Add(tangent.X); verts.Add(tangent.Y); verts.Add(tangent.Z); verts.Add(1f);
                verts.Add(u); verts.Add(v);
            }
        }
        for (int lat = 0; lat < latSegments; lat++)
        {
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int first = lat * (lonSegments + 1) + lon;
                int second = first + lonSegments + 1;
                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);
                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }
        return new PreparedMeshBuffer(verts.ToArray(), indices.ToArray());
    }
}
