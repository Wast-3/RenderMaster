using OpenTK.Graphics.OpenGL4;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class ProgramLibrary
    {
        readonly Dictionary<ProgramKey, ShaderProgram> _cache = new();
        readonly Dictionary<ProgramKey, string[]> _deps = new();
        readonly ConcurrentQueue<string> _changes = new();
        readonly FileSystemWatcher _watcher;

        public ProgramLibrary()
        {
            _watcher = new FileSystemWatcher(EngineConfig.ShaderDirectory)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += (s, e) =>
            {
                _changes.Enqueue(e.FullPath);
                _changes.Enqueue(e.OldFullPath);
            };
        }

        void OnFileChanged(object? sender, FileSystemEventArgs e) => _changes.Enqueue(e.FullPath);

        public ShaderProgram Get(ProgramKey key)
        {
            if (_cache.TryGetValue(key, out var p)) return p;
            RenderMaster.Engine.Logger.Log($"Program cache miss: {key}", RenderMaster.Engine.LogLevel.Debug);
            var (v, f, deps) = ShaderSources.ForWithDeps(key);
            var prog = new ShaderProgram(v, f);
            BindStaticLayouts(prog.Handle);
            _cache[key] = prog;
            _deps[key] = deps;
            return prog;
        }

        public void PumpHotReload()
        {
            if (_changes.IsEmpty) return;

            HashSet<ProgramKey> dirty = new();
            while (_changes.TryDequeue(out var path))
            {
                foreach (var (key, deps) in _deps)
                {
                    if (Array.IndexOf(deps, path) >= 0)
                        dirty.Add(key);
                }
            }

            foreach (var key in dirty)
            {
                if (!_cache.TryGetValue(key, out var oldProg))
                    continue;
                try
                {
                    var (v, f, deps) = ShaderSources.ForWithDeps(key);
                    var prog = new ShaderProgram(v, f);
                    BindStaticLayouts(prog.Handle);
                    _cache[key] = prog;
                    _deps[key] = deps;
                    oldProg.Dispose();
                    RenderMaster.Engine.Logger.Log($"Hot reloaded {key}", RenderMaster.Engine.LogLevel.Info);
                }
                catch (System.Exception ex)
                {
                    RenderMaster.Engine.Logger.Log($"Failed to hot reload {key}: {ex.Message}", RenderMaster.Engine.LogLevel.Error);
                }
            }
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
            Bind("LightBlock",    BindingPoints.Lights);

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
