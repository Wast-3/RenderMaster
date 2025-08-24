using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using RenderMaster.Engine;

namespace RenderMaster;

public class BasicImageTexture(string path, TextureUnit unit) : ATexture(path)
{
    public int TextureId { get; private set; }
    public TextureUnit textureUnit = unit;
    public String texturePath = path;


    public BasicImageTexture
    {
        GL.ActiveTexture(textureUnit);


        GL.GenTextures(1, out int id);
        GL.BindTexture(TextureTarget.Texture2D, id);


        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);


        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureImage.Width, textureImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureImage.Data);


        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);


        GL.BindTexture(TextureTarget.Texture2D, 0);


        TextureId = id;
    }


    public override void Bind()
    {
        Logger.Log("Binding texture " + TextureId + " to texture unit " + textureUnit + " Texture path: " + texturePath, LogLevel.Debug);
        GL.ActiveTexture(textureUnit);
        GL.BindTexture(TextureTarget.Texture2D, TextureId);
    }


    public override void Unbind()
    {
        Logger.Log("Unbinding texture " + TextureId + " from texture unit " + textureUnit + " Texture path: " + texturePath, LogLevel.Debug);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}
