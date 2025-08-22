using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using System.IO;
using OpenTK.Mathematics;
using System;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using RenderMaster.Engine;

namespace RenderMaster
{
    public class Game : GameWindow
    {


        IUserInterface userInterface = null!; // initialized in OnLoad

        Scene mainScene;
        OpenGLStateStack openGLState;

        public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
        {

            ClientSize = (width, height),
            Title = title
        })
        {
            this.mainScene = new Scene("main testing scene", width, height);

            /* mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "UVTest\\cyl.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "UVTest\\uv_check2.png")));
               mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "TexturedCylinder\\cylinder.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "TexturedCylinder\\uv_check2.png")));
               mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "MonkeyTime\\monkey.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "MonkeyTime\\Cum.png")));
               mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "HouseThing\\house.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "HouseThing\\House.png")));
               mainScene.AddModel(new Model(VertType.VertColorTexture, ModelShaderType.BasicTextured, Path.Combine(EngineConfig.ModelDirectory, "GroundTerrain\\mountain.verttxt"), Path.Combine(EngineConfig.ModelDirectory, "GroundTerrain\\mountain.png"))); */

            Material tableMaterial = new Material(
                TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table.jpg")),
                TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table_specular.jpg"))
            );

            Material lampMaterial = new Material(
                TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\lamp.jpg")),
                TextureCache.Instance.GetTexture(Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\lamp_specular.jpg"))
            );

            mainScene.AddModel(new Model(VertType.VertColorNormal, ModelShaderType.VertColorNormal,
                Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\table.verttxt"), tableMaterial)); // table

            mainScene.AddModel(new Model(VertType.VertColorNormal, ModelShaderType.VertColorNormal,
                Path.Combine(EngineConfig.ModelDirectory, "TableAndLamp\\lamp.verttxt"), lampMaterial)); // lamp

            mainScene.sceneModels[1].Position = new Vector3(0, 1.5f, 0); // move lamp up

            openGLState = new OpenGLStateStack();
        }


        static void Main(string[] args)
        {
            Game game = new Game(2560, 1440, "RENDERMASTER ENGINE");
            game.Run();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            Logger.Log("RENDERMASTER START: ", LogLevel.Info);

            mainScene.RenderSceneSetup();
            mainScene.sceneModels[0].Position = new Vector3(0, 0, 0);
            openGLState.PushState();
            userInterface = new UI();
            openGLState.PopState();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            openGLState.PushState();
            mainScene.RenderScene(args);
            userInterface.Bind();
            userInterface.Render(args, this.mainScene.camera);
            userInterface.Unbind();
            SwapBuffers();

            openGLState.PopState();
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            mainScene.camera.ProcessKeyEvents(e);
            var io = ImGui.GetIO();

            ImGuiKey key = MapOpenTKKeyToImGuiKey(e.Key);
            io.AddKeyEvent(key, true);


            io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
            io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
            io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);


            io.AddKeyEvent(ImGuiKey.ModSuper, e.Modifiers.HasFlag(KeyModifiers.Super));


            if (e.Key >= Keys.D0 && e.Key <= Keys.Z)
            {
                io.AddInputCharacter((uint)e.Key);
            }
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            var io = ImGui.GetIO();

            ImGuiKey key = MapOpenTKKeyToImGuiKey(e.Key);
            io.AddKeyEvent(key, false);


            io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
            io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
            io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);


            io.AddKeyEvent(ImGuiKey.ModSuper, e.Modifiers.HasFlag(KeyModifiers.Super));
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            var io = ImGui.GetIO();
            float scaleFactor = io.DisplayFramebufferScale.Y;

            io.MousePos = new System.Numerics.Vector2(MouseState.X * scaleFactor, MouseState.Y * scaleFactor);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            var io = ImGui.GetIO();
            io.AddMouseButtonEvent((int)e.Button, true);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            var io = ImGui.GetIO();
            io.AddMouseButtonEvent((int)e.Button, false);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var io = ImGui.GetIO();
            io.AddMouseWheelEvent(e.OffsetX, e.OffsetY);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);


            userInterface?.Resize(e);


            var io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(e.Width, e.Height);


            var framebufferWidth = (int)(e.Width * io.DisplayFramebufferScale.X);
            var framebufferHeight = (int)(e.Height * io.DisplayFramebufferScale.Y);


            GL.Viewport(0, 0, framebufferWidth, framebufferHeight);
        }

        private ImGuiKey MapOpenTKKeyToImGuiKey(Keys key)
        {

            return key switch
            {
                Keys.Tab => ImGuiKey.Tab,
                Keys.Left => ImGuiKey.LeftArrow,
                Keys.Right => ImGuiKey.RightArrow,
                Keys.Up => ImGuiKey.UpArrow,
                Keys.Down => ImGuiKey.DownArrow,
                Keys.PageUp => ImGuiKey.PageUp,
                Keys.PageDown => ImGuiKey.PageDown,
                Keys.Home => ImGuiKey.Home,
                Keys.End => ImGuiKey.End,
                Keys.Insert => ImGuiKey.Insert,
                Keys.Delete => ImGuiKey.Delete,
                Keys.Backspace => ImGuiKey.Backspace,
                Keys.Space => ImGuiKey.Space,
                Keys.Enter => ImGuiKey.Enter,
                Keys.Escape => ImGuiKey.Escape,
                Keys.Apostrophe => ImGuiKey.Apostrophe,
                Keys.Comma => ImGuiKey.Comma,
                Keys.Minus => ImGuiKey.Minus,
                Keys.Period => ImGuiKey.Period,
                Keys.Slash => ImGuiKey.Slash,
                Keys.Semicolon => ImGuiKey.Semicolon,
                Keys.Equal => ImGuiKey.Equal,
                Keys.LeftBracket => ImGuiKey.LeftBracket,
                Keys.Backslash => ImGuiKey.Backslash,
                Keys.RightBracket => ImGuiKey.RightBracket,
                Keys.GraveAccent => ImGuiKey.GraveAccent,
                Keys.CapsLock => ImGuiKey.CapsLock,
                Keys.ScrollLock => ImGuiKey.ScrollLock,
                Keys.NumLock => ImGuiKey.NumLock,
                Keys.PrintScreen => ImGuiKey.PrintScreen,
                Keys.Pause => ImGuiKey.Pause,
                Keys.KeyPad0 => ImGuiKey.Keypad0,
                Keys.KeyPad1 => ImGuiKey.Keypad1,
                Keys.KeyPad2 => ImGuiKey.Keypad2,
                Keys.KeyPad3 => ImGuiKey.Keypad3,
                Keys.KeyPad4 => ImGuiKey.Keypad4,
                Keys.KeyPad5 => ImGuiKey.Keypad5,
                Keys.KeyPad6 => ImGuiKey.Keypad6,
                Keys.KeyPad7 => ImGuiKey.Keypad7,
                Keys.KeyPad8 => ImGuiKey.Keypad8,
                Keys.KeyPad9 => ImGuiKey.Keypad9,
                Keys.KeyPadDecimal => ImGuiKey.KeypadDecimal,
                Keys.KeyPadDivide => ImGuiKey.KeypadDivide,
                Keys.KeyPadMultiply => ImGuiKey.KeypadMultiply,
                Keys.KeyPadSubtract => ImGuiKey.KeypadSubtract,
                Keys.KeyPadAdd => ImGuiKey.KeypadAdd,
                Keys.KeyPadEnter => ImGuiKey.KeypadEnter,
                Keys.KeyPadEqual => ImGuiKey.KeypadEqual,
                Keys.LeftShift => ImGuiKey.LeftShift,
                Keys.LeftControl => ImGuiKey.LeftCtrl,
                Keys.LeftAlt => ImGuiKey.LeftAlt,
                Keys.LeftSuper => ImGuiKey.LeftSuper,
                Keys.RightShift => ImGuiKey.RightShift,
                Keys.RightControl => ImGuiKey.RightCtrl,
                Keys.RightAlt => ImGuiKey.RightAlt,
                Keys.RightSuper => ImGuiKey.RightSuper,
                Keys.Menu => ImGuiKey.Menu,
                Keys.D0 => ImGuiKey._0,
                Keys.D1 => ImGuiKey._1,
                Keys.D2 => ImGuiKey._2,
                Keys.D3 => ImGuiKey._3,
                Keys.D4 => ImGuiKey._4,
                Keys.D5 => ImGuiKey._5,
                Keys.D6 => ImGuiKey._6,
                Keys.D7 => ImGuiKey._7,
                Keys.D8 => ImGuiKey._8,
                Keys.D9 => ImGuiKey._9,
                Keys.A => ImGuiKey.A,
                Keys.B => ImGuiKey.B,
                Keys.C => ImGuiKey.C,
                Keys.D => ImGuiKey.D,
                Keys.E => ImGuiKey.E,
                Keys.F => ImGuiKey.F,
                Keys.G => ImGuiKey.G,
                Keys.H => ImGuiKey.H,
                Keys.I => ImGuiKey.I,
                Keys.J => ImGuiKey.J,
                Keys.K => ImGuiKey.K,
                Keys.L => ImGuiKey.L,
                Keys.M => ImGuiKey.M,
                Keys.N => ImGuiKey.N,
                Keys.O => ImGuiKey.O,
                Keys.P => ImGuiKey.P,
                Keys.Q => ImGuiKey.Q,
                Keys.R => ImGuiKey.R,
                Keys.S => ImGuiKey.S,
                Keys.T => ImGuiKey.T,
                Keys.U => ImGuiKey.U,
                Keys.V => ImGuiKey.V,
                Keys.W => ImGuiKey.W,
                Keys.X => ImGuiKey.X,
                Keys.Y => ImGuiKey.Y,
                Keys.Z => ImGuiKey.Z,
                Keys.F1 => ImGuiKey.F1,
                Keys.F2 => ImGuiKey.F2,
                Keys.F3 => ImGuiKey.F3,
                Keys.F4 => ImGuiKey.F4,
                Keys.F5 => ImGuiKey.F5,
                Keys.F6 => ImGuiKey.F6,
                Keys.F7 => ImGuiKey.F7,
                Keys.F8 => ImGuiKey.F8,
                Keys.F9 => ImGuiKey.F9,
                Keys.F10 => ImGuiKey.F10,
                Keys.F11 => ImGuiKey.F11,
                Keys.F12 => ImGuiKey.F12,
                _ => ImGuiKey.None
            };
        }
    }
}