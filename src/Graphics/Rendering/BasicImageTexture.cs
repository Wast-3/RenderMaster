using System;
using OpenTK.Graphics.OpenGL4;

namespace RenderMaster;

public class BasicImageTexture : ATexture, IDisposable
{
    public int TextureId { get; private set; }
    public string TexturePath { get; private set; } = string.Empty;

    public BasicImageTexture(string path) : base(path)
    {
        TexturePath = path;

        int levels = 1 + (int)Math.Floor(Math.Log2(Math.Max(textureImage.Width, textureImage.Height)));
        GL.CreateTextures(TextureTarget.Texture2D, 1, out int id);
        TextureId = id;

        GL.TextureParameter(TextureId, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(TextureId, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(TextureId, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TextureParameter(TextureId, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TextureStorage2D(TextureId, levels, SizedInternalFormat.Srgb8Alpha8, textureImage.Width, textureImage.Height);
        GL.TextureSubImage2D(TextureId, 0, 0, 0, textureImage.Width, textureImage.Height, PixelFormat.Rgba, PixelType.UnsignedByte, textureImage.Data);
        GL.GenerateTextureMipmap(TextureId);
        GL.TextureParameter(TextureId, TextureParameterName.TextureMaxLevel, levels - 1);
    }

    public void BindToUnit(int unit)
    {
        GL.BindTextureUnit(unit, TextureId);
    }

    public void Dispose()
    {
        GL.DeleteTexture(TextureId);
    }
}

