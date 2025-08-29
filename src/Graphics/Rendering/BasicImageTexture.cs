using System;
using OpenTK.Graphics.OpenGL4;

namespace RenderMaster;

public class BasicImageTexture : ATexture, IDisposable
{
    public int TextureId { get; private set; }
    public int? BoundUnit { get; internal set; }
    public string TexturePath { get; private set; } = string.Empty;

    public BasicImageTexture(string path) : base(path)
    {
        TexturePath = path;

        GL.CreateTextures(TextureTarget.Texture2D, 1, out int id);
        TextureId = id;

        GL.TextureParameter(TextureId, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(TextureId, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(TextureId, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TextureParameter(TextureId, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        GL.TextureStorage2D(TextureId, 1, SizedInternalFormat.Rgba8, textureImage.Width, textureImage.Height);
        GL.TextureSubImage2D(TextureId, 0, 0, 0, textureImage.Width, textureImage.Height, PixelFormat.Rgba, PixelType.UnsignedByte, textureImage.Data);

        GL.GenerateTextureMipmap(TextureId);
    }

    public override void Bind()
    {
        if (!BoundUnit.HasValue)
        {
            TextureCache.Instance.AcquireUnit(this);
        }

        if (BoundUnit.HasValue)
        {
            GL.BindTextureUnit(BoundUnit.Value, TextureId);
        }
    }

    public override void Unbind()
    {
        if (BoundUnit.HasValue)
        {
            GL.BindTextureUnit(BoundUnit.Value, 0);
            TextureCache.Instance.ReleaseUnit(this);
        }
    }

    public void Dispose()
    {
        Unbind();
        GL.DeleteTexture(TextureId);
    }
}

