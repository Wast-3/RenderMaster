// RenderMaster.Contracts/Commands.cs
using RenderMaster.src.Contracts;
using System;

namespace RenderMaster.Contracts;

public sealed record ChangeMaterial(Guid CorrelationId, NodeId Node, int SubmeshIndex, MaterialId Material)
    : ICommand<Unit>;

public sealed record SetMaterialParam(Guid CorrelationId, MaterialId Material, string Param, object Value)
    : ICommand<Unit>;

public sealed record SelectNode(Guid CorrelationId, NodeId Node)
    : ICommand<Unit>;

public sealed record ReloadShaders(Guid CorrelationId)
    : ICommand<Unit>;

public sealed record FocusCamera(Guid CorrelationId, NodeId Target, float Distance)
    : ICommand<Unit>;

public readonly record struct Unit;
