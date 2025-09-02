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
            string frag = File.ReadAllText(Path.Combine(dir, fragFile));
            //literally return a tuple containing the full shader
            return ($"{defs}\n{common}\n{vert}", $"{defs}\n{common}\n{frag}");
        }
    }
}
//		frag	"in VS_OUT { vec3 P; vec3 N; vec2 UV; } fs;\r\nlayout(location=0) out vec4 outColor;\r\n\r\nuniform sampler2D uBaseColorTex;\r\n\r\nvoid main()\r\n{\r\n#ifdef VAR_ALPHABLEND\r\n    if (texture(uBaseColorTex, fs.UV).a < uAlphaCutoff) discard;\r\n#endif\r\n    vec4 bc = texture(uBaseColorTex, fs.UV) * uBaseColorFactor;\r\n    outColor = bc;\r\n}\r\n"	string
