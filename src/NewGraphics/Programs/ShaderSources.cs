using System.IO;
namespace RenderMaster.src.NewGraphics.Programs
{
    static class ShaderSources
    {
        public static (string vert, string frag) For(ProgramKey key)
        {
            var defs =
$@"#version 450 core
#define TECH_{key.Tech} 1
#define PASS_{key.Pass} 1
{(key.Variants.HasFlag(ProgramVariants.DoubleSided) ? "#define VAR_DOUBLESIDED 1" : "")}
{(key.Variants.HasFlag(ProgramVariants.AlphaBlend)  ? "#define VAR_ALPHABLEND 1" : "")}
";
            string dir = EngineConfig.ShaderDirectory;
            string common = File.ReadAllText(Path.Combine(dir, "common_blocks.glsl"));
            string vert   = File.ReadAllText(Path.Combine(dir, "standard.vert"));
            string frag   = File.ReadAllText(Path.Combine(dir, "unlit.frag"));
            return ($"{defs}\n{common}\n{vert}", $"{defs}\n{common}\n{frag}");
        }
    }
}
