// RenderMaster.ControlPlane/CommandBus.cs
using System;
using System.Threading.Channels;
using RenderMaster.Contracts;
using RenderMaster.src.Contracts;

namespace RenderMaster.ControlPlane;

public interface ICommandBus
{
    void Post<TRes>(ICommand<TRes> cmd);
    int Drain(Action<object> dispatch); // called on engine thread
}

public sealed class CommandBus : ICommandBus
{
    private readonly Channel<object> _ch =
        Channel.CreateBounded<object>(new BoundedChannelOptions(4096)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public void Post<TRes>(ICommand<TRes> cmd) => _ch.Writer.TryWrite(cmd);

    public int Drain(Action<object> dispatch)
    {
        int n = 0;
        while (_ch.Reader.TryRead(out var cmd))
        {
            dispatch(cmd);
            n++;
        }
        return n;
    }
}
