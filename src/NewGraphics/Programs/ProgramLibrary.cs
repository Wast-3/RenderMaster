using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using System.IO;

namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class ProgramLibrary
    {
        readonly Dictionary<ProgramKey, ShaderProgram> _cache = new();
        readonly Dictionary<string, HashSet<ProgramKey>> _fileDependencies = new();

        public ShaderProgram Get(ProgramKey key)
        {
            if (_cache.TryGetValue(key, out var p)) return p;
            RenderMaster.Engine.Logger.Log($"Program cache miss, compiling: {key}", RenderMaster.Engine.LogLevel.Debug);

            var (v, f, deps) = ShaderSources.For(key);
            var prog = new ShaderProgram(v, f);
            BindStaticLayouts(prog.Handle);
            _cache[key] = prog;

            foreach (var depPath in deps)
            {
                if (!_fileDependencies.TryGetValue(depPath, out var set))
                {
                    set = new HashSet<ProgramKey>();
                    _fileDependencies[depPath] = set;
                }
                set.Add(key);
            }

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

        public void InvalidateProgramsUsingFile(string filePath)
        {
            if (_fileDependencies.TryGetValue(filePath, out var affectedKeys))
            {
                RenderMaster.Engine.Logger.Log(
                    $"Invalidating {affectedKeys.Count} programs due to change in {Path.GetFileName(filePath)}",
                    RenderMaster.Engine.LogLevel.Info);

                foreach (var key in affectedKeys)
                {
                    if (_cache.TryGetValue(key, out var program))
                    {
                        program.Dispose();
                        _cache.Remove(key);
                    }
                }

                affectedKeys.Clear();
            }
        }
    }
}
