using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster
{
    internal class Material
    {
        private BasicImageTexture Diffuse;
        private BasicImageTexture Specular;
        
        public Material(BasicImageTexture diffuse, BasicImageTexture specular) {
            this.Diffuse = diffuse;
            this.Specular = specular;
        }

        public bool BindAllTextures()
        {
            Diffuse.Bind();
            Specular.Bind();
            return true;
        }
    }
}
