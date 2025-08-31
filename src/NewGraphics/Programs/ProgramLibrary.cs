using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class ProgramLibrary
    {
        readonly Dictionary<ProgramKey, ShaderProgram> _cache = new();
        public ShaderProgram Get(ProgramKey key)
        {
            if (_cache.TryGetValue(key, out var p)) return p;
            var (v, f) = ShaderSources.For(key);
            var prog = new ShaderProgram(v, f);
            BindStaticLayouts(prog.Handle);
            _cache[key] = prog;
            return prog;
        }

        static void BindStaticLayouts(int prog)
        {
            void Bind(string block, int binding)
            {
                int idx = GL.GetUniformBlockIndex(prog, block);
                if (idx >= 0) GL.UniformBlockBinding(prog, idx, binding);
            }
            Bind("FrameBlock",    BindingPoints.Frame);
            Bind("ObjectBlock",   BindingPoints.Object);
            Bind("MaterialBlock", BindingPoints.Material);

            void SetSampler(string name, int unit)
            {
                int loc = GL.GetUniformLocation(prog, name);
                if (loc >= 0) GL.ProgramUniform1(prog, loc, unit);
            }
            SetSampler("uBaseColorTex",         TextureUnits.BaseColor);
            SetSampler("uNormalTex",            TextureUnits.Normal);
            SetSampler("uMetalRoughTex",        TextureUnits.MetallicRoughness);
        }
    }
}
