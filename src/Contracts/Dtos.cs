// RenderMaster.Contracts/Dtos.cs
using System;
using System.Numerics;
using System.Collections.Generic;

namespace RenderMaster.src.Contracts;

// Ids are plain ints or Guids. (We’ll add a registry to assign them; Part 2.)
public readonly record struct NodeId(int Value);
public readonly record struct MaterialId(int Value);

// Scene graph snapshot for tree views.
public sealed record SceneGraphSnapshot(
    int Generation,
    IReadOnlyList<NodeRow> Nodes);

public sealed record NodeRow(
    NodeId Id,
    NodeId? ParentId,
    string Name,
    Matrix4x4 Local,
    Matrix4x4 World,
    string[] Components);

// Focused node snapshot for inspector panes.
public sealed record NodeSnapshot(
    NodeId Id,
    string Name,
    Matrix4x4 Local,
    Matrix4x4 World,
    IReadOnlyList<ComponentDescriptor> Components);

// Introspection metadata for components (no reflection leaks).
public sealed record ComponentDescriptor(
    string Kind,                       // e.g., "Transform", "Mesh", "MaterialBinding"
    IReadOnlyList<PropertyDescriptor> Properties);

public sealed record PropertyDescriptor(
    string Name,
    string Type,                       // "float", "vec3", "color", "enum", ...
    object? Value,                     // current value (boxed)
    object? Min = null,
    object? Max = null,
    string?[]? EnumLabels = null);

// Materials view
public sealed record MaterialsSnapshot(
    int Generation,
    IReadOnlyList<MaterialRow> Materials);

public sealed record MaterialRow(
    MaterialId Id,
    string Name,
    Vector4 BaseColor,
    float Metallic,
    float Roughness,
    bool DoubleSided);

// Picking result
public sealed record PickHit(
    NodeId Node,
    int? SubmeshIndex,
    Vector3 WorldPosition,
    Vector3 WorldNormal);
