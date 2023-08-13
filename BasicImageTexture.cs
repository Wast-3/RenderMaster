using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace RenderMaster
{
    public class BasicImageTexture : ATexture
    {
        public int TextureId { get; private set; }

        public BasicImageTexture(string path) : base(path)
        {
            // Generate texture
            GL.GenTextures(1, out int id);
            GL.BindTexture(TextureTarget.Texture2D, id);

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear); // Added mipmap
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Set texture data using image from base class
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureImage.Width, textureImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureImage.Data);

            // Generate mipmaps
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            // Unbind texture
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Set texture id
            TextureId = id;
        }

        public override void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, TextureId);
        }

        public override void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

    }
}
