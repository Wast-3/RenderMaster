using OpenTK.Graphics.OpenGL4;
using RenderMaster.Engine;

namespace RenderMaster;

public class BasicImageTexture : ATexture
{
    public int TextureId { get; private set; }
    public TextureUnit textureUnit;
    public string texturePath = "";

    public BasicImageTexture(string path, TextureUnit unit = TextureUnit.Texture0) : base(path)
    {
        texturePath = path;
        textureUnit = unit;

        // Create texture object without binding (Direct State Access)
        GL.CreateTextures(TextureTarget.Texture2D, 1, out int id);

        // Set parameters directly on the texture object
        GL.TextureParameter(id, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(id, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(id, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TextureParameter(id, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // Allocate immutable storage and upload the image data
        GL.TextureStorage2D(id, 1, SizedInternalFormat.Rgba8, textureImage.Width, textureImage.Height);
        GL.TextureSubImage2D(id, 0, 0, 0, textureImage.Width, textureImage.Height,
            PixelFormat.Rgba, PixelType.UnsignedByte, textureImage.Data);

        // Generate mipmaps for the texture
        GL.GenerateTextureMipmap(id);

        TextureId = id;
    }

    public override void Bind()
    {
        Logger.Log($"Binding texture {TextureId} to texture unit {textureUnit} Texture path: {texturePath}", LogLevel.Debug);
        int unit = (int)textureUnit - (int)TextureUnit.Texture0;
        GL.BindTextureUnit(unit, TextureId);
    }

    public override void Unbind()
    {
        Logger.Log($"Unbinding texture {TextureId} from texture unit {textureUnit} Texture path: {texturePath}", LogLevel.Debug);
        int unit = (int)textureUnit - (int)TextureUnit.Texture0;
        GL.BindTextureUnit(unit, 0);
    }
}

