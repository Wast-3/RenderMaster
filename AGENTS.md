# Proposals

## Command architecture proposal:
short answer up front:

* The Ari Aldo Martini piece (“You probably don’t need MediatR”) argues that generic mediator libraries add indirection and magic that obscure control flow, weaken type locality, and couple you to a tool instead of your domain. It recommends domain-specific ports (interfaces) and a tiny, explicit in-process dispatcher over a catch-all mediator package. That advice absolutely applies here: we want the engine core to expose a narrow, explicit API (commands, queries, events) and keep the debug UI as just another adapter hung off those ports—no UI-specific code in the core, no global service-locator-style mediators. ([GitHub][1])

* Fowler’s CQRS article says: splitting reads from writes can be powerful, but only in the right bounded contexts; otherwise it adds risky complexity. For our engine, CQRS fits brilliantly for “debug/inspector” concerns (read-heavy, task-based UI, low write rate), but we should not apply it to every subsystem (e.g., hot inner loops) by default. ([martinfowler.com][2])

Below is a proposal you can actually implement.

# Proposal document

## Pattern description

Adopt **Ports & Adapters (Hexagonal Architecture)** as the macro-architecture, and inside the “Application” boundary provide a **domain-specific command bus, query bus, and event bus**. UI (debug or otherwise), input devices, hot-reload file watchers, and tests are **adapters** that talk to the core **only via commands, queries, and events**.

* **Ports** (interfaces):

  * `IRenderMasterCommands` (write-side): `ChangeMaterial`, `SelectNode`, `ToggleWireframe`, `ReloadShaders`, `FocusCamera`, …
  * `IRenderMasterQueries` (read-side): `GetSceneGraph`, `RayPick`, `GetMaterialSchema`, `GetNodeSnapshot`, …
  * `IRenderMasterEvents` (pub/sub): `NodeSelected`, `MaterialChanged`, `ResourcesReloaded`, …

* **Adapters**:

  * ImGui Debug UI adapter implements presenters and binds buttons to commands and views to queries.
  * GL main loop adapter pumps the buses at deterministic points (start/end of frame) to keep thread/context safety.
  * Hot-reload adapter posts `ReloadShaders`.

* **Read model** (query side): a **frame-snapshotted, immutable** “DebugProjection” built once per frame from authoritative state. Queries read this snapshot (lock-free). Commands update authoritative state by enqueuing work on the render/sim thread.

This is **CQRS within a bounded context** (“Debug & Tooling”) sitting on top of a hexagonal core. We avoid mediator frameworks; we keep a minimal in-process dispatcher so flow stays obvious and fast. ([alistair.cockburn.us][3], [martinfowler.com][2])

## Benefits (why this helps your goals)

* **Engine doesn’t know about UI**: the engine exposes ports that any adapter can use; the ImGui adapter is just one. (That’s the point of ports & adapters.) ([alistair.cockburn.us][3])
* **Refactor safety**: UI depends on stable **contracts** (command/query DTOs) in a `RenderMaster.Contracts` assembly. You can refactor internal types freely; handlers do the mapping. Contract changes are intentionally versioned and rare.
* **Task-based debug UI**: CQRS naturally encourages “do a thing” commands and “show a view” queries, which maps well to inspector actions and panels. (Fowler calls this out.) ([martinfowler.com][2])
* **Performance isolation**: Reads come from a cheap snapshot; writes are serialized on the GL/Sim thread via commands. You can throttle/merge debug events without touching the core loop.
* **Testable**: Command handlers and query projectors are pure(ish) units you can run headless; you can load a glTF and assert that `GetSceneGraph` returns the expected tree.
* **No Mediator magic**: You keep explicit types and explicit dispatch; fewer surprises, easier to debug. (This is precisely the Mediatr critique.) ([GitHub][1])

## Where in the engine it will be useful (concrete examples)

1. **Material editing**

   * `ChangeMaterial { NodeId, SubmeshSpan?, MaterialKey }`
   * `GetMaterialSchema { MaterialKey } -> { editable params, ranges }`
   * `SetMaterialParam { MaterialKey, ParamName, Value }`
   * UI: list of materials via query; sliders bound to `SetMaterialParam`.

2. **Picking & selection**

   * `RayPick { ScreenX, ScreenY } -> Hit { NodeId, MeshId, Submesh, Barycentrics, WorldPos, Normal }`
   * Implement picking with a small ID buffer pass or CPU BVH depending on your needs; either way, return a DTO, not internal types.
   * `SelectNode { NodeId }` + `NodeSelected` event updates UI focus pane.

3. **Scene graph browser**

   * `GetSceneGraph { Depth?, Page? } -> Tree<NodeDto>` (stable `NodeId`, name, component summaries)
   * `GetNodeSnapshot { NodeId } -> NodeIntrospection { components[], transforms, bounds, material refs }`

4. **Shader hot-reload & program variants**

   * `ReloadShaders { }` and `GetProgramVariants { } -> stats`
   * Your existing `ProgramLibrary.PumpHotReload()` keeps working; the command just flips the switch.

5. **Telemetry**

   * `GetTimings { MethodName? } -> rolling avg, last N` (you already capture in `TimingAspect`)
   * Stream `MaterialChanged`, `HotReloaded`, etc., on `IRenderMasterEvents`.

## Potential pitfalls & downsides

* **Over-CQRSing**: splitting read/write everywhere can fragment the model. Keep CQRS to the **debug/tooling bounded context** and any other read-heavy surfaces (e.g., external telemetry), not inside hot inner loops. (Fowler warns of risky complexity.) ([martinfowler.com][2])
* **Stale reads**: your snapshot is at most one frame old; the UI may show N-1 frame data. That’s fine for tooling; document it.
* **Thread context & GL**: all commands that touch GL must execute on the GL thread. Use a single queue processed at a known point in the frame.
* **Contract drift**: if you change the contract DTOs frequently, you’ll lose the “UI doesn’t break” property. Treat contracts as public API and version them.
* **Event storms**: if you broadcast domain events for every micro change, the UI can get spammed. Add coalescing and backpressure (e.g., only emit `MaterialChanged` once per frame per material).

## Nuance of implementation (how to avoid antipatterns)

* **Use Hexagonal first, CQRS where it earns its keep**. Define ports, then decide which ports are commands vs queries. Don’t force dual models where the read/write semantics are identical. ([alistair.cockburn.us][3], [martinfowler.com][2])
* **No generic mediator package**. Build a tiny dispatcher:

  * `ICommand<TResponse>` / `ICommandHandler<TCommand,TResponse>`
  * `IQuery<TResponse>` / `IQueryHandler<TQuery,TResponse>`
  * A `CommandBus`/`QueryBus` that are just `Dictionary<Type, Func<object,object>>` plus a small decorator pipeline (logging, validation).
  * Register handlers explicitly in composition root. (This matches “you probably don’t need MediatR”.) ([GitHub][1])
* **Read models are projections, not live objects**. Build a `DebugProjection` once per frame from your actual scene state (`LoadedNodes`, materials, programs). It’s immutable (records), stored in a swap buffer; `GetSceneGraph` etc. just read.

  * This echoes the “reporting model / read model” from CQRS without forcing event sourcing. ([martinfowler.com][2])
* **Task-based UI**. Prefer buttons like “Assign Material to Selection” over CRUDy “edit entity”. This lines up with command semantics and keeps the UI decoupled. ([martinfowler.com][2])
* **Introspection via schema/metadata, not reflection everywhere**.

  * Add `[Inspectable]` attributes on component fields OR (better) generate an **Introspection Schema** with a Roslyn Source Generator that emits descriptors `{ name, type, range, getter, setter? }`.
  * `GetNodeSnapshot` returns a bag of component descriptors + current values. The UI renders editors from descriptors—so changing internal types usually doesn’t break the UI compilation.
* **Bounded consistency**. We don’t need distributed consistency tricks here. Reads are eventually-consistent at frame granularity; writes are serialized on the render thread; done.

## Overall engine architecture (before → after)

**Before (simplified)**

```
[ImGui UI]  --> calls into engine classes & fields directly
        \-> touches Camera/Input/Nodes, Logger, ProgramLibrary, etc.
[GL Main Loop] tightly coupled to UI code paths
```

**After**

```
          +--------------------------------------+
          |           Application Core           |
          |  (Hexagon: no idea about UI/GLFW)   |
          |                                      |
          |  Ports:                              |
Drivers ->|  IRenderMasterCommands (write)       |<- Driven
          |  IRenderMasterQueries  (read)        |   ports
          |  IRenderMasterEvents   (pub/sub)     |
          +------------------^-------------------+
                             | adapters
     [ImGui Debug UI]   [CLI/Test]   [HotReloadFSWatcher]
            |                |                  |
            v                v                  v
        CommandBus       CommandBus         CommandBus
        QueryBus         QueryBus           QueryBus
            \_________  /       \__________/
                      \/
            [GL/Sim thread processes command queue]
                     [DebugProjection snapshot per frame]
```

* Rendering, resource tables, GL programs remain in the core, unaware of any adapter. The **only** way in is commands; the **only** way out for state is queries/events.

## Implementation advice (step-by-step, concrete)

1. **Create contracts assembly** `RenderMaster.Contracts`

   * Records:

     * Commands: `ChangeMaterial(Guid NodeId, string MaterialKey) : ICommand<Unit>`
       `SetMaterialParam(string MaterialKey, string Param, ParamValue Value) : ICommand<Unit>`
       `SelectNode(Guid NodeId) : ICommand<Unit>`
       `ReloadShaders() : ICommand<Unit>`
     * Queries:  `GetSceneGraph() : IQuery<SceneGraphDto>`
       `RayPick(int X, int Y) : IQuery<PickHitDto?>`
       `GetMaterialSchema(string Key) : IQuery<MaterialSchemaDto>`
       `GetNodeSnapshot(Guid NodeId) : IQuery<NodeSnapshotDto>`
     * Events:   `NodeSelected(Guid NodeId)`, `MaterialChanged(string Key)`
   * DTOs are simple, UI-safe shapes (no OpenTK types). Provide adapters to convert (e.g., `Vector3` ⇄ tuples).

2. **Add minimal buses** in a small `RenderMaster.App` assembly

   * `ICommandBus` with `Send<TCommand,TResponse>(TCommand)`
   * `IQueryBus` with `Ask<TQuery,TResponse>(TQuery)`
   * Register handlers in a composition root (manual or with `Microsoft.Extensions.DependencyInjection`).
   * Decorators: logging (`Logger.Log`), validation (optional), metrics (your `TimingAspect` already covers handlers if you annotate).

3. **Write handlers** inside the core (but depending only on contracts)

   * **Command handlers** run on the GL/Sim thread. Build a single-producer queue (`ConcurrentQueue`) but drain it **synchronously** at the start of `OnRenderFrame` or `OnUpdateFrame` to ensure GL context correctness.
   * **Query handlers** read from `DebugProjection` (described next). Do not touch live mutable engine objects.

4. **Build the read model** `DebugProjection`

   * At end of `nodes.UpdateWorldTransforms()` (already in `OnUpdateFrame`), build or update an immutable snapshot:

     * A tree of `{ NodeId, Name, Children[], Summary }`
     * Map from `NodeId -> NodeSnapshotDto` (transforms, bounds, component descriptors, material refs)
     * A catalog of materials `{ Key, parameters }`
   * Use double buffering: `projectionWrite` built this frame, `projectionRead` swapped for queries.

5. **Picking service**

   * Option A (GPU ID buffer): render a tiny offscreen pass that encodes `NodeId` (or mesh id) per pixel; `RayPick` reads one pixel under the cursor (remember OpenGL’s Y up vs down). Fast and robust.
   * Option B (CPU): maintain a BVH over meshes’ world AABBs (update per `UpdateWorldTransforms()`), perform ray tests for debug. Simpler to integrate first.

6. **Hook UI to ports only**

   * Replace current direct calls (e.g., accessing `Camera`/`Nodes`) with queries/commands.
   * Panels:

     * Scene Graph: `Ask(GetSceneGraph)`; on select → `Send(SelectNode)`.
     * Inspector: `Ask(GetNodeSnapshot)`; render properties by introspection descriptors; on value change → specific `Set*` command.
     * Materials: `Ask(GetMaterialSchema)`; slider changes → `SetMaterialParam`.
     * Shader: `Send(ReloadShaders)`, `Ask(GetProgramVariants)`.
   * Keep ImGui rendering code dumb; all “engine thoughts” live behind handlers.

7. **Introspection metadata**

   * Add `[Inspectable]` attributes for components, or a source generator that emits `ComponentDescriptor` per component with fields, labels, ranges, and delegates `Get(object)`/`Set(object, value)`.
   * `GetNodeSnapshot` packages `{ descriptor, value }` pairs so the UI can render editors generically.

8. **Events to keep UI reactive**

   * When selection changes or materials update, push a message on `IRenderMasterEvents`. The UI subscribes and invalidates local caches. Use a ring buffer and poll each frame to avoid threading.

9. **Versioning & testing**

   * Version contracts (e.g., `v1` namespace). Prefer **extending** DTOs over changing; add optional fields.
   * Headless tests: construct core, load a small glTF (`LoadFromGltfFile`), run `UploadIncremental`, pump `RenderMasterCore.Update`, then assert `GetSceneGraph` content; send `ChangeMaterial` and verify `GetNodeSnapshot` reflects it.

## Examples of command & query shapes (pseudo-C#)

```csharp
// Contracts
public interface ICommand<out TRes> {}
public interface IQuery<out TRes> {}

public record ChangeMaterial(Guid NodeId, string MaterialKey) : ICommand<Unit>;
public record GetSceneGraph() : IQuery<SceneGraphDto>;
public record RayPick(int X, int Y) : IQuery<PickHitDto?>;

// App buses
public interface ICommandBus { TRes Send<TRes>(ICommand<TRes> cmd); }
public interface IQueryBus   { TRes Ask<TRes>(IQuery<TRes> qry); }

// Handler registration (composition root)
services.AddHandler<ChangeMaterial, Unit, ChangeMaterialHandler>();
services.AddHandler<GetSceneGraph, SceneGraphDto, GetSceneGraphHandler>();

// Drain commands on GL thread each frame
while (_commandQueue.TryDequeue(out var work)) work();
```

(Keep the buses \~100–200 LOC each with a simple decorator chain. That’s it. No generic mediator library needed. ([GitHub][1]))

## When *not* to use CQRS in this engine

* In the **hot rendering path**, mesh extraction, draw sorting, and encoder: don’t split reads/writes—those are performance-critical, and there’s no distinct “read model” benefit.
* For internal math/data types that the UI never sees. Keep those private; only surface DTOs through queries.

## Documentation, sources, further reading (why these)

* **CQRS caution + scope** — Martin Fowler: pattern, benefits, and explicit warnings to constrain to the right bounded contexts. This directly informs our “debug/tooling only” choice. ([martinfowler.com][2])
* **Ports & Adapters (Hexagonal)** — Alistair Cockburn: original pattern; the core should run without a UI or a database; the UI is just an adapter hung off named ports. That’s our macro-shape. ([alistair.cockburn.us][3], [alistaircockburn.com][4])
* **Event-Driven & CQRS relations** — Fowler’s “What do you mean by event-driven?” clarifies that CQRS doesn’t require events; we’re choosing snapshots rather than full event sourcing. ([martinfowler.com][5])
* **Event Sourcing** — Fowler overview / Azure pattern doc, if you later want undo/redo timelines for edits. Useful for tooling (command history), but optional now. ([martinfowler.com][6], [Microsoft Learn][7])
* **“You probably don’t need MediatR”** — the thrust is: avoid generic mediators; prefer explicit domain ports and simple dispatch. Exactly what we’re doing. Also see Jimmy Bogard’s (MediatR author) hand-rolled registration post for pragmatic patterns around mediator-style wiring. ([GitHub][1], [Reddit][8])

---

[1]: https://github.com/arialdomartini/arialdomartini.github.io/discussions/7?utm_source=chatgpt.com "You probably don't need MediatR #7"
[2]: https://martinfowler.com/bliki/CQRS.html?utm_source=chatgpt.com "CQRS"
[3]: https://alistair.cockburn.us/hexagonal-architecture?utm_source=chatgpt.com "hexagonal-architecture - Alistair Cockburn"
[4]: https://alistaircockburn.com/hexarch%20v1.1b%20DIFFS%2020250420-1012%20paper%2Bepub.docx.pdf?utm_source=chatgpt.com "Hexagonal Architecture Explained"
[5]: https://martinfowler.com/articles/201701-event-driven.html?utm_source=chatgpt.com "What do you mean by “Event-Driven”?"
[6]: https://martinfowler.com/eaaDev/EventSourcing.html?utm_source=chatgpt.com "Event Sourcing"
[7]: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing?utm_source=chatgpt.com "Event Sourcing pattern - Azure Architecture Center"
[8]: https://www.reddit.com/r/csharp/comments/162npnl/reasons_to_implement_cqrs/?utm_source=chatgpt.com "Reasons to implement CQRS : r/csharp"
