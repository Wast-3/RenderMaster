using OpenTK.Graphics.OpenGL4;

namespace RenderMaster.src.NewGraphics.Resources
{
    struct SamplerDesc
    {
        public TextureMinFilter MinFilter;
        public TextureMagFilter MagFilter;
        public TextureWrapMode WrapS;
        public TextureWrapMode WrapT;

        public SamplerDesc(
            TextureMinFilter minFilter,
            TextureMagFilter magFilter,
            TextureWrapMode wrapS,
            TextureWrapMode wrapT)
        {
            MinFilter = minFilter;
            MagFilter = magFilter;
            WrapS = wrapS;
            WrapT = wrapT;
        }
    }
}
