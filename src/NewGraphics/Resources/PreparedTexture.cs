using StbImageSharp;
using System;
using System.IO;

namespace RenderMaster.src.NewGraphics.Resources
{
    class PreparedTexture
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; } = Array.Empty<byte>();

        // Indicates whether the texture data is encoded in sRGB color space.
        // Color textures like albedo/base color maps should be sRGB, while
        // data textures (normal maps, metallic-roughness, etc.) should remain
        // linear.
        public bool IsSrgb { get; set; }

        public PreparedTexture(byte[] bytes, bool isSrgb = true)
        {
            using var ms = new MemoryStream(bytes);
            var img = ImageResult.FromStream(ms, ColorComponents.RedGreenBlueAlpha);
            Width = img.Width;
            Height = img.Height;
            Pixels = img.Data;
            IsSrgb = isSrgb;
        }
    }
}

