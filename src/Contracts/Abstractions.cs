using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderMaster.src.Contracts;

public interface ICommand<out TResponse> { Guid CorrelationId { get; } }
public interface IQuery<out TResponse> { }

public interface IEngineEvent
{
    Guid CorrelationId { get; }
    long Sequence { get; }
}

public sealed record EngineCapabilities(
    int ApiMajor,
    string[] SupportedCommands,
    string[] AvailableQueries
);