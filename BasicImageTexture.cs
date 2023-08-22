using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace RenderMaster
{
    public class BasicImageTexture : ATexture
    {
        public int TextureId { get; private set; }
        private readonly TextureUnit textureUnit; // Field to store the texture unit for this texture

        // Constructor now takes a texture unit as a parameter
        public BasicImageTexture(string path, TextureUnit unit) : base(path)
        {
            
            
            textureUnit = unit; // Assign texture unit

            //Immedeatly activate the texture unit, so that the state set here is recorded on the OpenGL side through this state tracking tool.
            GL.ActiveTexture(textureUnit);

            // Generate texture ID
            GL.GenTextures(1, out int id);
            GL.BindTexture(TextureTarget.Texture2D, id);

            // Set texture parameters for wrapping and filtering
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Copy the image data to the GPU
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textureImage.Width, textureImage.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureImage.Data);

            // Generate mipmaps for better image quality at different distances
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            // Unbind the texture for cleanup
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Store the texture ID for later use
            TextureId = id;
        }

        // Bind method now uses the texture unit field
        public override void Bind()
        {
            GL.ActiveTexture(textureUnit); // Activate the specified texture unit
            GL.BindTexture(TextureTarget.Texture2D, TextureId); // Bind the texture to the active unit
        }

        // Unbind the texture
        public override void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
