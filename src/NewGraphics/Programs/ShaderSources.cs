using System.Collections.Generic;
using System.IO;

namespace RenderMaster.src.NewGraphics.Programs
{
    static class ShaderSources
    {
        static string ReadWithLine(string path)
        {
            string name = Path.GetFileName(path).Replace("\\", "/");
            return $"#line 1 \"{name}\"\n{File.ReadAllText(path)}";
        }

        public static (string vert, string frag) For(ProgramKey key)
        {
            var (v, f, _) = ForWithDeps(key);
            return (v, f);
        }

        public static (string vert, string frag, string[] deps) ForWithDeps(ProgramKey key)
        {
            var defs =
$@"#version 450 core
#define TECH_{key.Tech} 1
#define PASS_{key.Pass} 1
{(key.Variants.HasFlag(ProgramVariants.DoubleSided) ? "#define VAR_DOUBLESIDED 1" : "")}
{(key.Variants.HasFlag(ProgramVariants.AlphaBlend)  ? "#define VAR_ALPHABLEND 1" : "")}
";

            string dir = EngineConfig.ShaderDirectory;
            List<string> deps = new();

            string commonPath = Path.Combine(dir, "common_blocks.glsl");
            deps.Add(commonPath);
            string common = ReadWithLine(commonPath);

            string vertPath = Path.Combine(dir, "standard.vert");
            deps.Add(vertPath);
            string vertBody = ReadWithLine(vertPath);

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
            deps.Add(fragPath);
            string fragBody = ReadWithLine(fragPath);

            string vert = $"{defs}\n{common}\n{vertBody}";
            string frag = $"{defs}\n{common}\n{fragBody}";

            return (vert, frag, deps.ToArray());
        }
    }
}

