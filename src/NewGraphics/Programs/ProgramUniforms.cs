using System;

namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class ProgramUniforms : IDisposable
    {
        public readonly UboRing<ObjectBlock> ObjectRing;
        public readonly UboRing<MaterialBlock> MaterialRing;
        public readonly UniformBuffer<FrameBlock> Frame;
        public readonly UniformBuffer<LightsBlock> Lights;

        public ProgramUniforms(int objectPerFrame = 65_536, int materialsPerFrame = 4_096)
        {
            ObjectRing = new UboRing<ObjectBlock>(objectPerFrame);
            MaterialRing = new UboRing<MaterialBlock>(materialsPerFrame);
            Frame = new UniformBuffer<FrameBlock>();
            Lights = new UniformBuffer<LightsBlock>();
        }

        public void BeginFrame() { ObjectRing.BeginFrame(); MaterialRing.BeginFrame(); }

        public void Dispose()
        {
            ObjectRing.Dispose();
            MaterialRing.Dispose();
            Frame.Dispose();
            Lights.Dispose();
        }
    }
}
