// RenderMaster.ControlPlane/EngineControl.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RenderMaster.src.Contracts;

namespace RenderMaster.src.ControlPlane;

public sealed class EngineControl : IDisposable
{
    public ICommandBus Commands { get; }
    public IQueryBus Queries { get; }
    public IEventBus Events { get; }
    public CommandPipeline Pipeline { get; }

    // Shared registries so Commands/Queries/Events agree on stable ids
    private readonly StableIdRegistry<NewGraphics.Scene.Node> _nodeIds = new();
    private readonly StableIdRegistry<NewGraphics.Resources.MaterialCPU> _matIds = new();

    // Reverse indexes (rebuilt each tick from registries)
    private Dictionary<int, NewGraphics.Scene.Node> _nodeById = new();
    private Dictionary<int, int> _matIdToCpuIndex = new();

    private readonly DebugProjection _debug;
    public IDebugProjectionReader Debug => _debug;

    private readonly IProjection[] _projections;

    // Engine references used by handlers
    private readonly NewGraphics.Programs.ProgramLibrary _programs;
    private readonly NewGraphics.Scene.LoadedNodes _nodes;
    private readonly NewGraphics.Resources.CPUResourceTable _cpu;
    private readonly NewGraphics.Resources.GPUResourceTable _gpu;
    private readonly NewGraphics.Resources.UploadResult _map;

    internal EngineControl(
        NewGraphics.Programs.ProgramLibrary programs,
        NewGraphics.Scene.LoadedNodes nodes,
        NewGraphics.Resources.CPUResourceTable cpu,
        NewGraphics.Resources.GPUResourceTable gpu,
        NewGraphics.Resources.UploadResult map)
    {
        Commands = new CommandBus();
        Queries = new QueryBus();
        Events = new EventBus();

        _programs = programs;
        _nodes = nodes;
        _cpu = cpu;
        _gpu = gpu;
        _map = map;

        // Projections (share registries so ids match write-side)
        _debug = new DebugProjection(_nodeIds, _matIds);
        _projections = new IProjection[] { _debug };

        // Build dispatcher + pipeline
        var dispatcher = new CommandDispatcher();
        RegisterCommandHandlers(dispatcher);

        Pipeline = new CommandPipeline(
            mw: new ICommandMiddleware[] { new LoggingMiddleware() },
            dispatcher: dispatcher);

        // Projections own their query wiring
        foreach (var p in _projections)
            p.RegisterQueries((QueryBus)Queries);

        // Build reverse indexes once so early commands can resolve ids
        RebuildReverseIndexes();
    }

    public int DrainCommands() => ((CommandBus)Commands).Drain(Pipeline.Execute);

    public void RebuildProjections()
    {
        // Keep reverse maps in sync with current scene/material lists
        RebuildReverseIndexes();

        // Rebuild all read models
        foreach (var p in _projections)
            p.Rebuild(_nodes, _cpu, _map);
    }

    private void RebuildReverseIndexes()
    {
        var nodeMap = new Dictionary<int, NewGraphics.Scene.Node>(_nodes.All.Count);
        foreach (var n in _nodes.All)
            nodeMap[_nodeIds.GetOrAdd(n)] = n;
        _nodeById = nodeMap;

        var matMap = new Dictionary<int, int>(_cpu.Materials.Count);
        for (int i = 0; i < _cpu.Materials.Count; i++)
            matMap[_matIds.GetOrAdd(_cpu.Materials[i])] = i;
        _matIdToCpuIndex = matMap;
    }

    private void RegisterCommandHandlers(CommandDispatcher d)
    {
        d.Register<ReloadShaders>(cmd =>
        {
            // Nudge: do a pump immediately (you also call PumpHotReload() each frame)
            _programs.PumpHotReload();
            Engine.Logger.Log("ReloadShaders requested.", Engine.LogLevel.Info);
        });

        d.Register<SelectNode>(cmd =>
        {
            // Publish event; adapters/UI decide what to do
            Events.Publish(new NodeSelected(cmd.CorrelationId, Events.NextSeq(), cmd.Node));
        });

        d.Register<ChangeMaterial>(cmd =>
        {
            if (!TryGetNode(cmd.Node, out var node))
            {
                Engine.Logger.Log($"ChangeMaterial: node {cmd.Node.Value} not found", Engine.LogLevel.Warning);
                return;
            }

            if (!TryGetMaterialCpuIndex(cmd.Material, out var cpuMatIndex))
            {
                Engine.Logger.Log($"ChangeMaterial: material {cmd.Material.Value} not found", Engine.LogLevel.Warning);
                return;
            }

            // Choose the Nth MeshComponent = "submeshIndex" (one component per glTF primitive)
            var meshComps = node.GetComponents<NewGraphics.Scene.MeshComponent>().ToList();
            if ((uint)cmd.SubmeshIndex >= (uint)meshComps.Count)
            {
                Engine.Logger.Log($"ChangeMaterial: submesh index {cmd.SubmeshIndex} out of range (count={meshComps.Count})", Engine.LogLevel.Warning);
                return;
            }

            var mc = meshComps[cmd.SubmeshIndex];
            mc.Material = new NewGraphics.Types.Handle<NewGraphics.Resources.MaterialCPU>(cpuMatIndex);

            // No GPU reupload required: material factors are read from CPU each frame for UBOs.
            Engine.Logger.Log($"ChangeMaterial: node={cmd.Node.Value} sm={cmd.SubmeshIndex} -> matCpu#{cpuMatIndex}", Engine.LogLevel.Info);
        });

        d.Register<SetMaterialParam>(cmd =>
        {
            if (!TryGetMaterialCpuIndex(cmd.Material, out var cpuMatIndex))
            {
                Engine.Logger.Log($"SetMaterialParam: material {cmd.Material.Value} not found", Engine.LogLevel.Warning);
                return;
            }

            var mat = _cpu.Materials[cpuMatIndex];
            if (!TrySetMaterialParam(mat, cmd.Param, cmd.Value))
            {
                Engine.Logger.Log($"SetMaterialParam: unsupported '{cmd.Param}' valueType={cmd.Value?.GetType().Name}", Engine.LogLevel.Warning);
                return;
            }

            // Render path pulls factors from CPU into the MaterialBlock UBO next frame.
            Engine.Logger.Log($"SetMaterialParam: matCpu#{cpuMatIndex} {cmd.Param} = {cmd.Value}", Engine.LogLevel.Info);
        });

        d.Register<FocusCamera>(cmd =>
        {
            // Keep camera control in the host (adapter); if you add a CameraFocusRequested event,
            // publish it here. For now, just log.
            Engine.Logger.Log($"FocusCamera requested: target={cmd.Target.Value} distance={cmd.Distance:F2}", Engine.LogLevel.Info);
        });
    }

    private bool TryGetNode(NodeId id, out NewGraphics.Scene.Node node)
        => _nodeById.TryGetValue(id.Value, out node);

    private bool TryGetMaterialCpuIndex(MaterialId id, out int cpuIndex)
        => _matIdToCpuIndex.TryGetValue(id.Value, out cpuIndex);

    private static bool TrySetMaterialParam(NewGraphics.Resources.MaterialCPU m, string param, object value)
    {
        switch (param)
        {
            case "BaseColorFactor":
            case "BaseColor":
            case "baseColor":
                if (value is Vector4 v4) { m.BaseColorFactor = v4; return true; }
                if (value is float[] fa && fa.Length >= 4) { m.BaseColorFactor = new Vector4(fa[0], fa[1], fa[2], fa[3]); return true; }
                return false;

            case "Metallic":
            case "MetallicFactor":
                if (ToFloat(value, out var met)) { m.MetallicFactor = met; return true; }
                return false;

            case "Roughness":
            case "RoughnessFactor":
                if (ToFloat(value, out var rgh)) { m.RoughnessFactor = rgh; return true; }
                return false;

            case "DoubleSided":
                if (value is bool b) { m.DoubleSided = b; return true; }
                return false;

            default:
                return false;
        }

        static bool ToFloat(object o, out float f)
        {
            switch (o)
            {
                case float x: f = x; return true;
                case double d: f = (float)d; return true;
                case int i: f = i; return true;
                case string s when float.TryParse(s, out var p): f = p; return true;
                default: f = 0; return false;
            }
        }
    }

    public void Dispose()
    {
        // nothing yet
    }
}
