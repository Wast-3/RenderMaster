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

        public PreparedTexture(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var img = ImageResult.FromStream(ms, ColorComponents.RedGreenBlueAlpha);
            Width = img.Width;
            Height = img.Height;
            Pixels = img.Data;
        }
    }
}

