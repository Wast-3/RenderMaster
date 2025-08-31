using RenderMaster.src.NewGraphics.Frame;

namespace RenderMaster.src.NewGraphics.Programs
{
    [System.Flags]
    enum ProgramVariants : uint { None = 0, DoubleSided = 1<<0, AlphaBlend = 1<<1 }

    readonly record struct ProgramKey(TechniqueKind Tech, PassKind Pass, ProgramVariants Variants, int VertexLayoutVersion);
}
