using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using RenderMaster.Engine;

namespace RenderMaster;

public class BasicImageTexture : ATexture
{
    public int TextureId { get; private set; }
    public TextureUnit textureUnit;
    public String texturePath = "";


    public BasicImageTexture(string path) : base(path)
    {
        GL.GenTextures(1, out int id);
        this.texturePath = path;

        //I don't understand. Is it not the case that, here, I would "activate my texture object", which i could then bind to a texture unit? Or is my understanding of texture units wrong?
        GL.ActiveTexture(id);

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
        //At time of binding, fetch an available texture unit from the texture unit cache

    }


    public override void Unbind()
    {
        Logger.Log("Unbinding texture " + TextureId + " from texture unit " + textureUnit + " Texture path: " + texturePath, LogLevel.Debug);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}
