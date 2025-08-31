using System.Collections.Generic;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Resources
{
    using TextureHandle = Handle<PreparedTexture>;

    class MaterialCPU
    {
        public Dictionary<string, TextureHandle> Textures { get; } = new();
    }
}

