using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using static RenderMaster.src.Physics.PhysicsCallbacks;

namespace RenderMaster.src.Physics;

class PhysicsEngine
{
    private readonly BufferPool _bufferPool;
    public Simulation Simulation { get; }
    private narrowPhase _narrowPhase;
    private readonly poseIntegrator _poseIntegrator;

    public PlayerController Player { get; }

    public PhysicsEngine(System.Numerics.Vector3 initialPlayerPosition)
    {
        _bufferPool = new BufferPool();
        _narrowPhase = new narrowPhase { Up = System.Numerics.Vector3.UnitY };
        _poseIntegrator = new poseIntegrator(new System.Numerics.Vector3(0f, -9.81f, 0f), 0.01f, 0.03f);

        Simulation = Simulation.Create(_bufferPool, _narrowPhase, _poseIntegrator, new SolveDescription(velocityIterationCount: 8, substepCount: 1));

        Player = new PlayerController(Simulation, initialPlayerPosition, totalHeight: 1.8f, radius: 0.35f);

        _narrowPhase.Player = Player;
        _narrowPhase.PlayerBodyHandle = Player.BodyHandle;
        Simulation.NarrowPhase.Callbacks.Player = Player;
        Simulation.NarrowPhase.Callbacks.PlayerBodyHandle = Player.BodyHandle;
        Simulation.NarrowPhase.Callbacks.Up = System.Numerics.Vector3.UnitY;
    }

    public void Setup()
    {
        Simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, -0.5f, 0), Simulation.Shapes.Add(new Box(2500, 1, 2500))));
    }

    public void Update(float deltaTime, InputState input, OpenTK.Mathematics.Vector3 cameraFront, OpenTK.Mathematics.Vector3 cameraRight)
    {
        var front = new System.Numerics.Vector3(cameraFront.X, cameraFront.Y, cameraFront.Z);
        var right = new System.Numerics.Vector3(cameraRight.X, cameraRight.Y, cameraRight.Z);

        if (input.Mode == MovementMode.Character && input.CaptureActive)
        {
            Player.ApplyInput(deltaTime, input.Movement, input.SprintHeld, input.JumpPressed, front, right);
        }
        else
        {
            Player.ApplyInput(deltaTime, System.Numerics.Vector2.Zero, false, false, front, right);
        }

        Player.BeginStep();
        Simulation.Timestep(deltaTime);
        Player.EndStep();
    }
}
