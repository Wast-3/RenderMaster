using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;

namespace RenderMaster.src.ControlPlane;

/// Minimal lifecycle for any read model that is rebuilt once per tick.
internal interface IProjection
{
    string Name { get; }
    int Generation { get; }             // monotonic per rebuild
    void Rebuild(LoadedNodes nodes, CPUResourceTable cpu, UploadResult map);
    void RegisterQueries(QueryBus q);   // lets the projection own its query wiring
}

/// The read-only surface area the UI / adapters will consume for the “debug” view.
public interface IDebugProjectionReader
{
    RenderMaster.src.Contracts.SceneGraphSnapshot SceneGraph { get; }
    RenderMaster.src.Contracts.MaterialsSnapshot Materials { get; }
    RenderMaster.src.Contracts.NodeSnapshot GetNodeSnapshot(RenderMaster.src.Contracts.NodeId id);
}
