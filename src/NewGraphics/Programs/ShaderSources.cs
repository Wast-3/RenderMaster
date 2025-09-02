using System.Collections.Generic;
using System.IO;

namespace RenderMaster.src.NewGraphics.Programs
{
    static class ShaderSources
    {
        public static (string vert, string frag, List<string> dependencies) For(ProgramKey key)
        {
            var defs =
$@"#version 450 core
#define TECH_{key.Tech} 1
#define PASS_{key.Pass} 1
{(key.Variants.HasFlag(ProgramVariants.DoubleSided) ? "#define VAR_DOUBLESIDED 1" : "")}
{(key.Variants.HasFlag(ProgramVariants.AlphaBlend)  ? "#define VAR_ALPHABLEND 1" : "")}
";

            string dir = EngineConfig.ShaderDirectory;

            var dependencies = new List<string>();

            string commonPath = Path.Combine(dir, "common_blocks.glsl");
            dependencies.Add(commonPath);
            string common = File.ReadAllText(commonPath);

            string vertPath = Path.Combine(dir, "standard.vert");
            dependencies.Add(vertPath);
            string vert = File.ReadAllText(vertPath);

            string fragFile;
            switch (key.Tech)
            {
                case Frame.TechniqueKind.PBR_MetalRough:
                    fragFile = "pbr.frag";
                    break;

                case Frame.TechniqueKind.Unlit:
                default:
                    fragFile = "unlit.frag";
                    break;
            }

            string fragPath = Path.Combine(dir, fragFile);
            dependencies.Add(fragPath);
            string frag = File.ReadAllText(fragPath);

            //literally return a tuple containing the full shader
            return ($"{defs}\n{common}\n{vert}", $"{defs}\n{common}\n{frag}", dependencies);
        }
    }
}
