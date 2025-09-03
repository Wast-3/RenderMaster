using System;
using System.Collections.Generic;
using System.Numerics;
using RenderMaster.src.Contracts;
using RenderMaster.src.NewGraphics.Resources;
using RenderMaster.src.NewGraphics.Scene;
using RenderMaster.src.NewGraphics.Types;

namespace RenderMaster.src.ControlPlane;

sealed class DebugProjection : IProjection, IDebugProjectionReader
{
    private readonly StableIdRegistry<Node> _nodeIds;
    private readonly StableIdRegistry<MaterialCPU> _matIds;

    public DebugProjection(StableIdRegistry<Node> nodeIds, StableIdRegistry<MaterialCPU> matIds)
    { _nodeIds = nodeIds; _matIds = matIds; }

    private volatile SceneGraphSnapshot _scene = new SceneGraphSnapshot(0, Array.Empty<NodeRow>());
    private volatile MaterialsSnapshot _mats = new MaterialsSnapshot(0, Array.Empty<MaterialRow>());
    private volatile Dictionary<NodeId, NodeSnapshot> _nodeSnaps =
        new Dictionary<NodeId, NodeSnapshot>();

    private int _generation;

    public string Name => "DebugProjection";
    public int Generation => _generation;

    // Reader surface (IDebugProjectionReader)
    public SceneGraphSnapshot SceneGraph => _scene;
    public MaterialsSnapshot Materials => _mats;
    public NodeSnapshot GetNodeSnapshot(NodeId id)
        => _nodeSnaps.TryGetValue(id, out var s)
           ? s
           : new NodeSnapshot(id, "[missing]", Matrix4x4.Identity, Matrix4x4.Identity, Array.Empty<ComponentDescriptor>());

    // Query wiring (IProjection)
    public void RegisterQueries(QueryBus q)
    {
        q.Register<GetSceneGraph, SceneGraphSnapshot>(_ => SceneGraph);
        q.Register<GetMaterials, MaterialsSnapshot>(_ => Materials);
        q.Register<GetNodeSnapshot, NodeSnapshot>(qry => GetNodeSnapshot(qry.Node));
        q.Register<RayPickPx, PickHit?>(_ => null); // fill later
    }

    public void Rebuild(LoadedNodes nodes, CPUResourceTable cpu, UploadResult map)
    {
        _generation++;

        var rows = new List<NodeRow>(nodes.All.Count);
        var inspector = new Dictionary<NodeId, NodeSnapshot>(nodes.All.Count);

        foreach (var n in nodes.All)
        {
            var id = new NodeId(_nodeIds.GetOrAdd(n));
            NodeId? parentId = n.Parent != null ? new NodeId(_nodeIds.GetOrAdd(n.Parent)) : (NodeId?)null;

            var tc = n.GetComponent<TransformComponent>();
            var local = tc?.LocalTransform ?? Matrix4x4.Identity;
            var world = tc?.WorldTransform ?? local;

            // If you add NameComponent in the loader, use it here
            var nameComp = n.GetComponent<NameComponent>();
            string name = nameComp?.Name ?? $"node#{id.Value}";

            rows.Add(new NodeRow(id, parentId, name, local, world, ComponentTags(n)));
            inspector[id] = new NodeSnapshot(id, name, local, world, BuildDescriptors(n, cpu, map));
        }

        var mats = new List<MaterialRow>(cpu.Materials.Count);
        for (int i = 0; i < cpu.Materials.Count; i++)
        {
            var m = cpu.Materials[i];
            var mid = new MaterialId(_matIds.GetOrAdd(m));
            var bc = m.BaseColorFactor ?? new Vector4(1, 1, 1, 1);
            var metallic = m.MetallicFactor ?? 1f;
            var rough = m.RoughnessFactor ?? 1f;
            var ds = m.DoubleSided ?? false;
            string name = $"mat#{mid.Value}";
            mats.Add(new MaterialRow(mid, name, bc, metallic, rough, ds));
        }

        // publish
        _scene = new SceneGraphSnapshot(_generation, rows);
        _mats = new MaterialsSnapshot(_generation, mats);
        _nodeSnaps = inspector;
    }

    private static string[] ComponentTags(Node n)
    {
        var tags = new List<string>(3);
        if (n.GetComponent<TransformComponent>() != null) tags.Add("Transform");
        foreach (var _ in n.GetComponents<MeshComponent>()) tags.Add("Mesh");
        return tags.ToArray();
    }

    private IReadOnlyList<ComponentDescriptor> BuildDescriptors(Node n, CPUResourceTable cpu, UploadResult map)
    {
        var list = new List<ComponentDescriptor>(3);

        var t = n.GetComponent<TransformComponent>();
        if (t != null)
        {
            list.Add(new ComponentDescriptor(
                "Transform",
                new[]
                {
                    new PropertyDescriptor("Local", "mat4", t.LocalTransform),
                    new PropertyDescriptor("World", "mat4", t.WorldTransform),
                }));
        }

        foreach (var mc in n.GetComponents<MeshComponent>())
        {
            list.Add(new ComponentDescriptor(
                "Mesh",
                new[]
                {
                    new PropertyDescriptor("MeshCpu", "int", mc.Mesh.IsValid ? mc.Mesh.Id : -1),
                    new PropertyDescriptor("SubmeshStart", "int", mc.Submesh.IndexStart),
                    new PropertyDescriptor("SubmeshCount", "int", mc.Submesh.IndexCount)
                }));

            MaterialId matId = ToMatId(cpu, mc.Material);
            list.Add(new ComponentDescriptor(
                "MaterialBinding",
                new[]
                {
                    new PropertyDescriptor("MaterialId", "material", matId.Value),
                    new PropertyDescriptor("HasBaseColorTex", "bool", HasTex(cpu, mc.Material, "BaseColorTexture")),
                    new PropertyDescriptor("HasNormalTex", "bool", HasTex(cpu, mc.Material, "NormalTexture")),
                    new PropertyDescriptor("HasMetalRoughTex", "bool", HasTex(cpu, mc.Material, "MetallicRoughnessTexture")),
                }));
        }

        return list;
    }

    private MaterialId ToMatId(CPUResourceTable cpu, Handle<MaterialCPU> h)
    {
        if (!h.IsValid || h.Id < 0 || h.Id >= cpu.Materials.Count) return new MaterialId(-1);
        return new MaterialId(_matIds.GetOrAdd(cpu.Materials[h.Id]));
    }

    private static bool HasTex(CPUResourceTable cpu, Handle<MaterialCPU> h, string key)
    {
        if (!h.IsValid || h.Id < 0 || h.Id >= cpu.Materials.Count) return false;
        return cpu.Materials[h.Id].Textures.ContainsKey(key);
    }
}
