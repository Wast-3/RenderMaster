using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster;

public class Material(BasicImageTexture diffuse, BasicImageTexture specular)
{
    public BasicImageTexture Diffuse = diffuse;
    public BasicImageTexture Specular = specular;

    public bool BindAllTextures()
    {
        Diffuse.Bind();
        Specular.Bind();
        return true;
    }

    public bool UnbindAllTextures()
    {
        Diffuse.Unbind();
        Specular.Unbind();
        return true;
    }
}
