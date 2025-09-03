// RenderMaster.Contracts/Commands.cs
using System;
using System.Numerics;

namespace RenderMaster.src.Contracts;

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

public sealed record FireProjectile(Guid CorrelationId, Vector3 Origin, Vector3 Direction)
    : ICommand<Unit>;
