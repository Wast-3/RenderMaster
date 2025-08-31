using RenderMaster.src.NewGraphics.Resources;

namespace RenderMaster.src.NewGraphics.Loading
{
    interface ILoadsResourceTable
    {
        CPUResourceTable ResourceTable { get; }

        void LoadResources(CPUResourceTable table);
    }
}

