using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster.src.Scene
{
    interface ISceneLoader
    {
        // Given a path to a scene asset, load each model, apply transforms, and then return a list of Models

        List<Model> LoadScene(string assetPath);
    }
}
