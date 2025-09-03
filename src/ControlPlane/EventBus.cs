// RenderMaster.ControlPlane/EventBus.cs
using System;
using System.Threading.Channels;
using RenderMaster.src.Contracts;

namespace RenderMaster.src.ControlPlane;

public interface IEventBus
{
    long NextSeq(); // monotonic per-run
    void Publish(IEngineEvent ev);
    bool TryRead(out IEngineEvent ev);
}

public sealed class EventBus : IEventBus
{
    private long _seq = 0;
    private readonly Channel<IEngineEvent> _ch =
        Channel.CreateUnbounded<IEngineEvent>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public long NextSeq() => Interlocked.Increment(ref _seq);

    public void Publish(IEngineEvent ev) => _ch.Writer.TryWrite(ev);

    public bool TryRead(out IEngineEvent ev) => _ch.Reader.TryRead(out ev);
}
