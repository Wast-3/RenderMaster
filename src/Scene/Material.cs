using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster;

public class Material
{
    public BasicImageTexture Diffuse;
    public BasicImageTexture Specular;

    public Material(BasicImageTexture diffuse, BasicImageTexture specular) {
        this.Diffuse = diffuse;
        this.Specular = specular;
    }
}
