using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using ImGuiNET;
using System.Runtime.CompilerServices;
using AspectInjector.Broker;
using System.Diagnostics;

namespace RenderMaster;

public interface IUserInterface
{
    public void Update(FrameEventArgs args, Camera camera);

    public void Render();

    public void Setup();

    public void Bind();

    public void Unbind();
    public void Resize(ResizeEventArgs e);
}

public class UI : IUserInterface
{
    private ImGuiBufferConfig bufferConfig = null!;
    private BasicTexturedShader shader = null!;
    private IntPtr context;
    private int fontTexture;
    private ImDrawDataPtr drawData;

    public UI()
    {
        Setup();
    }

    public void Bind()
    {
        bufferConfig.Bind();
        shader.Bind();
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.ScissorTest);
    }

    public void Resize(ResizeEventArgs e)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(e.Width, e.Height);
    }

    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

        if (fontTexture != 0)
        {
            GL.DeleteTexture(fontTexture);
        }

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

    private List<Type> GetAllLoadedTypes()
    {
        List<Type> types = new List<Type>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            types.AddRange(assembly.GetTypes());
        }
        return types;
    }

    [MeasureExecutionTime]
    public void Update(FrameEventArgs args, Camera camera)
    {
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();
        ImGui.NewFrame();
        ImGui.ShowDemoWindow();

        double frameRate = 1.0 / args.Time;
        string fpsString = frameRate.ToString("F2");

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(io.DisplaySize.X - 100, 0));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(100, 20));

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground;
        if (ImGui.Begin("FPS Counter", windowFlags))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            ImGui.Text(fpsString);
            ImGui.PopStyleColor();
        }
        ImGui.End();

        if (ImGui.Begin("Debug Window"))
        {
            if (ImGui.BeginTabBar("Tabs"))
            {
                if (ImGui.BeginTabItem("Function Timings"))
                {
                    ImGui.Text($"RENDERMASTER");
                    ImGui.Text($"Current FPS: {fpsString}");

                    bool isOddRow = false;

                    foreach (var entry in TimingAspect.Timings)
                    {
                        var timingsList = entry.Value.Values.ToList();
                        var averageTiming = timingsList.Average();
                        var latestTiming = timingsList.LastOrDefault();

                        if (isOddRow)
                        {
                            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Separator));
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.Border));
                        }

                        if (ImGui.TreeNode($"Method: {entry.Key}"))
                        {
                            ImGui.Text($"Average Execution Time (last 100): {averageTiming} ms");
                            ImGui.Text($"Latest Execution Time: {latestTiming} ms");

                            float[] timingsArray = timingsList.Select(t => (float)t).ToArray();

                            if (timingsArray.Length > 0)
                            {
                                ImGui.PlotLines("Timings", ref timingsArray[0], timingsArray.Length, 0, null, 0.0f, float.MaxValue, new System.Numerics.Vector2(0, 80));
                            }

                            ImGui.TreePop();
                        }

                        ImGui.PopStyleColor();

                        isOddRow = !isOddRow;
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Memory"))
                {
                    Process currentProcess = Process.GetCurrentProcess();
                    long totalMemoryUsageKB = currentProcess.WorkingSet64 / 1024;
                    double totalMemoryUsageGB = totalMemoryUsageKB / 1024.0 / 1024.0;
                    long privateMemoryUsageKB = currentProcess.PrivateMemorySize64 / 1024;
                    double privateMemoryUsageGB = privateMemoryUsageKB / 1024.0 / 1024.0;

                    ImGui.Text($"Total Memory Usage: {totalMemoryUsageKB} KB ({totalMemoryUsageGB:0.##} GB)");
                    ImGui.Text($"Private Memory Usage: {privateMemoryUsageKB} KB ({privateMemoryUsageGB:0.##} GB)");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        ImGui.Render();
        drawData = ImGui.GetDrawData();
    }

    public void Render()
    {
        ImGui.SetCurrentContext(context);
        ImGuiIOPtr io = ImGui.GetIO();

        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);
        shader.SetUniformMatrix4("projection_matrix", mvp);
        shader.SetSampler2D("in_fontTexture", 0);

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        if (drawData.Valid == false)
        {
            return;
        }
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = drawData.CmdLists[n];

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);

            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);

            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                    var clip = pcmd.ClipRect;

                    if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                    {
                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                    }
                    else
                    {
                        GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                    }
                }
            }
        }
    }

    public void Setup()
    {
        bufferConfig = new ImGuiBufferConfig();
        shader = new BasicTexturedShader(Path.Combine(EngineConfig.ShaderDirectory, "imguirendervert.vert"), Path.Combine(EngineConfig.ShaderDirectory, "imguirenderfrag.frag"));

        context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.Fonts.Flags = ImFontAtlasFlags.None;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;


        ImGui.StyleColorsDark();
        RecreateFontDeviceTexture();
    }

    public void Unbind()
    {
        bufferConfig.Unbind();
        shader.Unbind();
    }
}
