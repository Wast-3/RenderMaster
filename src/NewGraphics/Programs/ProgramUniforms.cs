using System;

namespace RenderMaster.src.NewGraphics.Programs
{
    sealed class ProgramUniforms : IDisposable
    {
        public readonly UboRing<ObjectBlock> ObjectRing;
        public readonly UniformBuffer<FrameBlock> Frame;

        public ProgramUniforms(int objectPerFrame = 65_536)
        {
            ObjectRing = new UboRing<ObjectBlock>(objectPerFrame);
            Frame = new UniformBuffer<FrameBlock>();
        }

        public void BeginFrame() => ObjectRing.BeginFrame();

        public void Dispose()
        {
            ObjectRing.Dispose();
            Frame.Dispose();
        }
    }
}
