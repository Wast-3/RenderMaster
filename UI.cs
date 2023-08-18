using OpenTK.Windowing.Common;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Runtime.CompilerServices;


namespace RenderMaster
{
    public interface IUserInterface
    {
        //Does the actual drawing
        public void Render(FrameEventArgs args, Camera camera);

        //Gets called a single time when the UI is first created
        public void Setup();
        
        //This gets called when the UI needs to set up its state before a render. Probably called every frame
        public void Bind();

        //This gets called when the UI needs to clean up its state after a render. Probably called every frame
        public void Unbind();

        public void Resize(ResizeEventArgs e);
    }

    public class UI : IUserInterface
    {
        private ImGuiBufferConfig bufferConfig;
        private BasicTexturedShader shader;
        private IntPtr context;
        private int fontTexture;

        public UI()
        {
            Setup();
        }

        public void Bind()
        {
            bufferConfig.Bind();
            shader.Bind();
            RecreateFontDeviceTexture();
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

            // Restore state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);

            io.Fonts.SetTexID((IntPtr)fontTexture);

            io.Fonts.ClearTexData();
        }


        public void Render(FrameEventArgs args, Camera camera)
        {
            ImGui.SetCurrentContext(context);
            ImGui.NewFrame();
            ImGui.ShowDemoWindow();
            ImGui.Render();
            var drawData = ImGui.GetDrawData();
            
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



            // Render command lists
            if (drawData.Valid == false)
            {
                return;
            }
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = drawData.CmdListsRange[n];

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

                        // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                        var clip = pcmd.ClipRect;
                        //GL.Scissor((int)clip.X, (int)io.DisplaySize.Y - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

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
            
            //Set style?
            ImGui.StyleColorsDark();
        }

        public void Unbind()
        {
            bufferConfig.Unbind();
            shader.Unbind();
            
        }
    }
}
