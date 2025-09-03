// RenderMaster.Contracts/Events.cs
using RenderMaster.src.Contracts;
using System;

namespace RenderMaster.Contracts;

public sealed record NodeSelected(Guid CorrelationId, long Sequence, NodeId Node)
    : IEngineEvent;

public sealed record MaterialChanged(Guid CorrelationId, long Sequence, MaterialId Material)
    : IEngineEvent;

public sealed record ShadersReloaded(Guid CorrelationId, long Sequence)
    : IEngineEvent;
