// RenderMaster.ControlPlane/CommandDispatcher.cs
using System;
using System.Collections.Generic;
using RenderMaster.src.Contracts;

namespace RenderMaster.src.ControlPlane;

public interface ICommandMiddleware { void Invoke(object command, Action next); }

public sealed class CommandDispatcher
{
    private readonly Dictionary<Type, Action<object>> _handlers = new();

    public void Register<T>(Action<T> handler) where T : ICommand<Unit>
        => _handlers[typeof(T)] = c => handler((T)c);

    //Dispatch looks up the concrete runtime type of command and invokes its handler.
    public void Dispatch(object command)
    {
        if (_handlers.TryGetValue(command.GetType(), out var h)) h(command);
        else Engine.Logger.Log($"No command handler for {command.GetType().Name}", Engine.LogLevel.Warning);
    }
}

// CommandPipeline chains multiple middleware components and finally the dispatcher allowing pre- and post-processing around command handling.
// Any middleware may short-circuit by not calling Next() (e.g., GuardAgainstDuplicate, RejectWhenPaused, etc.)
public sealed class CommandPipeline
{
    private readonly ICommandMiddleware[] _mw;
    private readonly CommandDispatcher _dispatcher;

    public CommandPipeline(IEnumerable<ICommandMiddleware> mw, CommandDispatcher dispatcher)
    { _mw = mw as ICommandMiddleware[] ?? new List<ICommandMiddleware>(mw).ToArray(); _dispatcher = dispatcher; }

    public void Execute(object command)
    {
        int i = -1;
        void Next()
        {
            i++;
            if (i < _mw.Length) _mw[i].Invoke(command, Next);
            else _dispatcher.Dispatch(command);
        }
        Next();
    }
}

// Example middleware (optional): log each command
public sealed class LoggingMiddleware : ICommandMiddleware
{
    public void Invoke(object command, Action next)
    {
        Engine.Logger.Log($"CMD {command.GetType().Name}", Engine.LogLevel.Debug);
        next();
    }
}
