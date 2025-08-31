using System.Collections.Generic;
using System.Numerics;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.NewGraphics.Resources
{
    using TextureHandle = Handle<PreparedTexture>;

    class MaterialCPU
    {
        public Dictionary<string, TextureHandle> Textures { get; } = new();

        // PBR material factors. Null indicates the value was not specified
        // in the source glTF and defaults should be used.
        public Vector4? BaseColorFactor { get; set; }
        public float? MetallicFactor { get; set; }
        public float? RoughnessFactor { get; set; }
        public bool? DoubleSided { get; set; }
    }
}

