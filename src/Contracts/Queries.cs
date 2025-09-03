// RenderMaster.Contracts/Queries.cs
using RenderMaster.src.Contracts;

namespace RenderMaster.Contracts;

public sealed record GetSceneGraph() : IQuery<SceneGraphSnapshot>;
public sealed record GetNodeSnapshot(NodeId Node) : IQuery<NodeSnapshot>;
public sealed record GetMaterials() : IQuery<MaterialsSnapshot>;

// Cursor → pick; we return hit via queries (or you can prefer events; both patterns are fine)
public sealed record RayPickPx(int X, int Y) : IQuery<PickHit?>;
