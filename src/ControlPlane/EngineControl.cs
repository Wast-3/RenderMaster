// RenderMaster.ControlPlane/EngineControl.cs
using System;
using RenderMaster.Contracts;

namespace RenderMaster.ControlPlane;

public sealed class EngineControl : IDisposable
{
    public ICommandBus Commands { get; }
    public IQueryBus Queries { get; }
    public IEventBus Events { get; }
    public CommandPipeline Pipeline { get; }

    // Engine references you’ll need inside handlers
    private readonly RenderMaster.src.NewGraphics.Programs.ProgramLibrary _programs;
    private readonly RenderMaster.src.NewGraphics.Scene.LoadedNodes _nodes;
    private readonly RenderMaster.src.NewGraphics.Resources.CPUResourceTable _cpu;
    private readonly RenderMaster.src.NewGraphics.Resources.GPUResourceTable _gpu;
    private readonly RenderMaster.src.NewGraphics.Resources.UploadResult _map;

    internal EngineControl(
        RenderMaster.src.NewGraphics.Programs.ProgramLibrary programs,
        RenderMaster.src.NewGraphics.Scene.LoadedNodes nodes,
        RenderMaster.src.NewGraphics.Resources.CPUResourceTable cpu,
        RenderMaster.src.NewGraphics.Resources.GPUResourceTable gpu,
        RenderMaster.src.NewGraphics.Resources.UploadResult map)
    {
        Commands = new CommandBus();
        Queries = new QueryBus();
        Events = new EventBus();

        _programs = programs;
        _nodes = nodes;
        _cpu = cpu;
        _gpu = gpu;
        _map = map;

        // Build dispatcher + pipeline
        var dispatcher = new CommandDispatcher();
        RegisterCommandHandlers(dispatcher);

        Pipeline = new CommandPipeline(
            mw: new ICommandMiddleware[] { new LoggingMiddleware() }, // add more later
            dispatcher: dispatcher);

        RegisterQueryHandlers((QueryBus)Queries);
    }

    public int DrainCommands() => ((CommandBus)Commands).Drain(Pipeline.Execute);

    private void RegisterCommandHandlers(CommandDispatcher d)
    {
        // NOTE: Real implementations in Part 3; for now, safe stubs.

        d.Register<ReloadShaders>(cmd =>
        {
            // You call _programs.PumpHotReload() each frame already; this is a 'nudge' command.
            RenderMaster.Engine.Logger.Log("ReloadShaders requested.", RenderMaster.Engine.LogLevel.Info);
        });

        d.Register<SelectNode>(cmd =>
        {
            // Publish event; selection state will be maintained in a small read model later.
            Events.Publish(new NodeSelected(cmd.CorrelationId, Events.NextSeq(), new NodeId(cmd.Node.Value)));
        });

        // …we’ll add ChangeMaterial/SetMaterialParam handlers in Part 3
    }

    private void RegisterQueryHandlers(QueryBus q)
    {
        // Part 2 will wire real producers. For now, return empty snapshots safely.
        q.Register<GetSceneGraph, SceneGraphSnapshot>(_ => new SceneGraphSnapshot(Generation: 0, Nodes: Array.Empty<NodeRow>()));
        q.Register<GetMaterials, MaterialsSnapshot>(_ => new MaterialsSnapshot(Generation: 0, Materials: Array.Empty<MaterialRow>()));
        q.Register<GetNodeSnapshot, NodeSnapshot>(qry => new NodeSnapshot(qry.Node, "Unknown", System.Numerics.Matrix4x4.Identity, System.Numerics.Matrix4x4.Identity, Array.Empty<ComponentDescriptor>()));
        q.Register<RayPickPx, PickHit?>(_ => null);
    }

    public void Dispose() { /* nothing yet */ }
}
