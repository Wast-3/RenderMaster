using System;
using System.Collections.Generic;
using RenderMaster.Contracts;
using RenderMaster.src.Contracts;

namespace RenderMaster.ControlPlane;

public interface IQueryBus
{
    TRes Ask<TRes>(IQuery<TRes> query);
}

public sealed class QueryBus : IQueryBus
{
    // concrete query type → handler
    private readonly Dictionary<Type, Func<object, object>> _handlers = new();
    //Registers a handler for one specific query type TQ that returns TR.
    public void Register<TQ, TR>(Func<TQ, TR> handler)
        where TQ : IQuery<TR> => _handlers[typeof(TQ)] = q => handler((TQ)q);

    public TRes Ask<TRes>(IQuery<TRes> query)
    {
        if (_handlers.TryGetValue(query.GetType(), out var h))
            return (TRes)h(query);
        throw new InvalidOperationException($"No query handler for {query.GetType().Name}");
    }
}
