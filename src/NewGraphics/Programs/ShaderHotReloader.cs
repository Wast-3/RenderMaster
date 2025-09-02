using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace RenderMaster.src.NewGraphics.Programs
{
    class ShaderHotReloader : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ConcurrentQueue<string> _changedFiles = new();

        public ShaderHotReloader(string shaderDirectory)
        {
            _watcher = new FileSystemWatcher(shaderDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.Name.EndsWith(".glsl") || e.Name.EndsWith(".vert") || e.Name.EndsWith(".frag"))
            {
                if (!_changedFiles.Contains(e.FullPath))
                {
                    _changedFiles.Enqueue(e.FullPath);
                    RenderMaster.Engine.Logger.Log($"Shader file changed: {e.Name}", RenderMaster.Engine.LogLevel.Info);
                }
            }
        }

        public void ProcessChanges(ProgramLibrary programLibrary)
        {
            while (_changedFiles.TryDequeue(out var filePath))
            {
                programLibrary.InvalidateProgramsUsingFile(filePath);
            }
        }

        public void Dispose() => _watcher.Dispose();
    }
}
