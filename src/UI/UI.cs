using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using ImGuiNET;
using AspectInjector.Broker;
using RenderMaster.src.NewGraphics.Programs;

namespace RenderMaster;

public interface IUserInterface
{
    void Update(FrameEventArgs args, Camera camera, bool mouseGrabbed);
    void Render();
    void Resize(ResizeEventArgs e);
}

public class UI : IUserInterface
{
    private ShaderProgram shader = null!;
    private int vao;
    private int vbo;
    private int ebo;
    private IntPtr context;
    private int fontTexture;
    private ImDrawDataPtr drawData;
    private DebugMenu debugMenu = new();

    public UI() => Setup();

    private void Setup()
    {
        shader = new ShaderProgram(
            File.ReadAllText(Path.Combine(EngineConfig.ShaderDirectory, "imguirendervert.vert")),
            File.ReadAllText(Path.Combine(EngineConfig.ShaderDirectory, "imguirenderfrag.frag")));

        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();
        ebo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);

        int stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);
        GL.EnableVertexAttribArray(2);
        GL.BindVertexArray(0);

        GL.UseProgram(shader.Handle);
        GL.Uniform1(GL.GetUniformLocation(shader.Handle, "in_fontTexture"), 0);
        GL.UseProgram(0);

        context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.Fonts.Flags = ImFontAtlasFlags.None;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.KeyRepeatDelay = 0.4f;
        io.KeyRepeatRate = 0.1f;

        ImGui.StyleColorsDark();
        RecreateFontDeviceTexture();
    }

    public void Resize(ResizeEventArgs e)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(e.Width, e.Height);
    }

    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bpp);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

        if (fontTexture != 0)
            GL.DeleteTexture(fontTexture);

        fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, fontTexture);
        GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);

        io.Fonts.SetTexID((IntPtr)fontTexture);
        io.Fonts.ClearTexData();
    }

    [MeasureExecutionTime]
    public void Update(FrameEventArgs args, Camera camera, bool mouseGrabbed)
    {
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        ImGui.NewFrame();
        ImGui.ShowDemoWindow();

        double frameRate = 1.0 / args.Time;
        string fpsString = frameRate.ToString("F2");

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground;

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(io.DisplaySize.X - 100, 0));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(100, 20));
        if (ImGui.Begin("FPS Counter", windowFlags))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 1, 1, 1));
            ImGui.Text(fpsString);
            ImGui.PopStyleColor();
        }
        ImGui.End();

        if (mouseGrabbed)
        {
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(io.DisplaySize.X - 150, 20));
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(150, 20));
            if (ImGui.Begin("MouseGrabState", windowFlags))
                ImGui.Text("(mouseGrabbed)");
            ImGui.End();
        }

        if (ImGui.Begin("Debug Window"))
        {
            debugMenu.FpsString = fpsString;
            debugMenu.AfterBegin();
        }
        ImGui.End();

        ImGui.Render();
        drawData = ImGui.GetDrawData();
    }

    public void Render()
    {
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();

        drawData.ScaleClipRects(io.DisplayFramebufferScale);
        if (!drawData.Valid)
            return;

        GL.UseProgram(shader.Handle);
        GL.BindVertexArray(vao);
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0f,
            -1f,
            1f);
        GL.UniformMatrix4(GL.GetUniformLocation(shader.Handle, "projection_matrix"), false, ref mvp);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            int vtxSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vtxSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vtxSize, cmdList.VtxBuffer.Data);

            int idxSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, idxSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, idxSize, cmdList.IdxBuffer.Data);

            for (int cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException();

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                var clip = pcmd.ClipRect;
                GL.Scissor(
                    (int)clip.X,
                    (int)(io.DisplaySize.Y - clip.W),
                    (int)(clip.Z - clip.X),
                    (int)(clip.W - clip.Y));

                if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                {
                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                }
                else
                {
                    GL.DrawElements(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (IntPtr)(pcmd.IdxOffset * sizeof(ushort)));
                }
            }
        }

        GL.Disable(EnableCap.ScissorTest);
        GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }
}
