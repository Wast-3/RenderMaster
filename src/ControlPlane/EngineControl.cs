// RenderMaster.ControlPlane/EngineControl.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities.Memory;
using RenderMaster.src.Contracts;
using RenderMaster.src.Physics;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Types;
using RenderMaster.src.NewGraphics.Programs;

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
    private readonly ProgramLibrary _programs;
    private readonly LoadedNodes _nodes;
    private readonly CPUResourceTable _cpu;
    private readonly GPUResourceTable _gpu;
    private readonly UploadResult _map;

    private readonly BufferPool _bufferPool = new();
    private readonly Simulation _simulation;
    private sealed class ProjectileState
    {
        public NodeId Node;
        public float Ttl;
        public ProjectileState(NodeId node, float ttl)
        {
            Node = node;
            Ttl = ttl;
        }
    }
    private readonly Dictionary<BodyHandle, ProjectileState> _projectiles = new();
    private readonly Dictionary<StaticHandle, MeshComponent> _staticToMesh = new();
    private readonly Random _rng = new();
    private readonly Handle<PreparedMeshBuffer> _projMesh;
    private readonly Handle<MaterialCPU> _projMaterial;
    private readonly SubmeshSpan _projSpan;
    private readonly TypedIndex _projShape;
    private BodyHandle _playerBody;
    private bool _playerExists;

    private bool IsPlayerGrounded()
    {
        if (!_playerExists)
            return false;
        var body = _simulation.Bodies.GetBodyReference(_playerBody);
        var ray = new Ray(body.Pose.Position, new Vector3(0, -1, 0));
        return _simulation.RayCast(ray, 0.6f, out RayHit _);
    }

    internal EngineControl(
        ProgramLibrary programs,
        LoadedNodes nodes,
        CPUResourceTable cpu,
        GPUResourceTable gpu,
        UploadResult map,
        Handle<PreparedMeshBuffer> projectileMesh,
        Handle<MaterialCPU> projectileMaterial,
        SubmeshSpan projectileSpan)
    {
        Commands = new CommandBus();
        Queries = new QueryBus();
        Events = new EventBus();

        _programs = programs;
        _nodes = nodes;
        _cpu = cpu;
        _gpu = gpu;
        _map = map;
        _projMesh = projectileMesh;
        _projMaterial = projectileMaterial;
        _projSpan = projectileSpan;

        var narrow = new PhysicsCallbacks.narrowPhase(OnCollision);
        var pose = new PhysicsCallbacks.poseIntegrator(new Vector3(0f, -9.81f, 0f));
        _simulation = Simulation.Create(_bufferPool, narrow, pose, new SolveDescription(8, 1));
        _projShape = _simulation.Shapes.Add(new Sphere(0.25f));
        AddSceneMeshStatics();

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

    public void EnsurePlayerBody(Vector3 start)
    {
        if (_playerExists)
            return;
        var capsule = new Capsule(0.5f, 1f);
        var shape = _simulation.Shapes.Add(capsule);
        var body = BodyDescription.CreateDynamic(new RigidPose(start), capsule.ComputeInertia(1f), new CollidableDescription(shape, 0.1f), new BodyActivityDescription(0.01f));
        _playerBody = _simulation.Bodies.Add(body);
        _playerExists = true;
    }

    public Vector3 UpdatePlayer(Vector3 move, bool jump, float dt)
    {
        if (!_playerExists)
            return Vector3.Zero;
        var body = _simulation.Bodies.GetBodyReference(_playerBody);
        ref var vel = ref body.Velocity;
        body.Pose.Orientation = Quaternion.Identity;
        vel.Angular = Vector3.Zero;

        var desired = move * 7f;
        vel.Linear.X = desired.X;
        vel.Linear.Z = desired.Z;
        if (jump && IsPlayerGrounded())
            vel.Linear.Y = 5f;
        return body.Pose.Position;
    }

    public void Simulate(float dt)
    {
        _simulation.Timestep(dt);
        var toRemove = new List<BodyHandle>();
        foreach (var kv in _projectiles)
        {
            var pose = _simulation.Bodies.GetBodyReference(kv.Key).Pose;
            var proj = kv.Value;
            if (_nodeById.TryGetValue(proj.Node.Value, out var node))
            {
                var tc = node.GetComponent<TransformComponent>();
                if (tc != null)
                {
                    var rot = Matrix4x4.CreateFromQuaternion(pose.Orientation);
                    var trans = Matrix4x4.CreateTranslation(pose.Position);
                    tc.LocalTransform = rot * trans;
                }
            }

            proj.Ttl -= dt;
            if (proj.Ttl <= 0f)
                toRemove.Add(kv.Key);
        }

        foreach (var handle in toRemove)
        {
            var proj = _projectiles[handle];
            if (_nodeById.TryGetValue(proj.Node.Value, out var node))
            {
                _nodes.RemoveNode(node);
                _nodeById.Remove(proj.Node.Value);
            }
            _simulation.Bodies.Remove(handle);
            _projectiles.Remove(handle);
        }
    }

    private void OnCollision(CollidableReference a, CollidableReference b)
    {
        if (a.Mobility == CollidableMobility.Dynamic && b.Mobility == CollidableMobility.Static)
            ApplyHit(a.BodyHandle, b.StaticHandle);
        else if (b.Mobility == CollidableMobility.Dynamic && a.Mobility == CollidableMobility.Static)
            ApplyHit(b.BodyHandle, a.StaticHandle);
    }

    private void ApplyHit(BodyHandle body, StaticHandle stat)
    {
        if (!_projectiles.ContainsKey(body) || !_staticToMesh.TryGetValue(stat, out var mc))
            return;
        int idx = _rng.Next(_cpu.Materials.Count);
        mc.Material = new Handle<MaterialCPU>(idx);
    }

    private void AddSceneMeshStatics()
    {
        _nodes.UpdateWorldTransforms();
        foreach (var node in _nodes.All)
        {
            var tc = node.GetComponent<TransformComponent>();
            Vector3 scale = Vector3.One;
            Quaternion rotation = Quaternion.Identity;
            Vector3 translation = Vector3.Zero;
            if (tc != null)
                Matrix4x4.Decompose(tc.WorldTransform, out scale, out rotation, out translation);

            foreach (var mc in node.GetComponents<MeshComponent>())
            {
                var mesh = _cpu.MeshBuffers[mc.Mesh.Id];
                var span = mc.Submesh;
                int triCount = span.IndexCount / 3;
                _bufferPool.Take<Triangle>(triCount, out var tris);
                for (int t = 0; t < triCount; t++)
                {
                    int i0 = ReadIndex(mesh, span.IndexStart + t * 3);
                    int i1 = ReadIndex(mesh, span.IndexStart + t * 3 + 1);
                    int i2 = ReadIndex(mesh, span.IndexStart + t * 3 + 2);
                    int b0 = i0 * PreparedMeshBuffer.FloatsPerVertex;
                    int b1 = i1 * PreparedMeshBuffer.FloatsPerVertex;
                    int b2 = i2 * PreparedMeshBuffer.FloatsPerVertex;
                    tris[t] = new Triangle(
                        new Vector3(mesh.Vertices[b0], mesh.Vertices[b0 + 1], mesh.Vertices[b0 + 2]) * scale,
                        new Vector3(mesh.Vertices[b1], mesh.Vertices[b1 + 1], mesh.Vertices[b1 + 2]) * scale,
                        new Vector3(mesh.Vertices[b2], mesh.Vertices[b2 + 1], mesh.Vertices[b2 + 2]) * scale);
                }
                var meshShape = new Mesh(tris, Vector3.One, _bufferPool);
                var shapeHandle = _simulation.Shapes.Add(meshShape);
                var sh = _simulation.Statics.Add(new StaticDescription(translation, rotation, shapeHandle));
                _staticToMesh[sh] = mc;
            }
        }
    }

    private static int ReadIndex(PreparedMeshBuffer mesh, int element)
    {
        if (mesh.IndexElementSize == 0)
            return element;
        var indices = mesh.Indices.AsSpan();
        return mesh.IndexElementSize switch
        {
            1 => indices[element],
            2 => BinaryPrimitives.ReadUInt16LittleEndian(indices.Slice(element * 2, 2)),
            _ => BinaryPrimitives.ReadInt32LittleEndian(indices.Slice(element * 4, 4))
        };
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

        d.Register<FireProjectile>(cmd =>
        {
            var bodyDesc = new BodyDescription
            {
                Pose = new RigidPose(cmd.Origin),
                Velocity = new BodyVelocity(cmd.Direction * 120f),
                LocalInertia = new Sphere(0.25f).ComputeInertia(1f),
                Collidable = new CollidableDescription(_projShape, 0.1f),
                Activity = new BodyActivityDescription(0.01f)
            };
            var handle = _simulation.Bodies.Add(bodyDesc);

            var node = new Node();
            node.AddComponent(new TransformComponent(Matrix4x4.Identity));
            node.AddComponent(new MeshComponent(_projMesh, _projMaterial, _projSpan));
            _nodes.AddNode(node);
            var nodeId = new NodeId(_nodeIds.GetOrAdd(node));
            _nodeById[nodeId.Value] = node;
            _projectiles[handle] = new ProjectileState(nodeId, 10f);
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
        _simulation.Dispose();
        _bufferPool.Clear();
    }
}
