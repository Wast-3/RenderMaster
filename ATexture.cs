using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StbImageSharp;

namespace RenderMaster;


public abstract class ATexture
{
    public ImageResult textureImage;
    public ATexture(string path)
    {
        this.textureImage = loadImageFromPath(path);
    }

    public abstract void Bind();

    public abstract void Unbind();

    public ImageResult loadImageFromPath(string path)
    {
        using (var stream = File.OpenRead(path))
        {
            return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
    }
}
