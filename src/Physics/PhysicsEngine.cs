using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using OpenTK.Windowing.Common;
using static RenderMaster.src.Physics.PhysicsCallbacks;

namespace RenderMaster.src.Physics
{
    class PhysicsEngine
    {
        private BufferPool bufferPool;
        public Simulation simulation;
        private narrowPhase narrowPhase = new();
        private poseIntegrator poseIntegrator = new(new System.Numerics.Vector3(0f, -3.81f, 0f), 0.01f, 0.02f);

        public PhysicsEngine()
        {
            bufferPool = new BufferPool();
            simulation = Simulation.Create(bufferPool, narrowPhase, poseIntegrator, new SolveDescription(velocityIterationCount: 8, substepCount: 1));
        }

        public void Setup()
        {
            simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, -0.5f, 0), simulation.Shapes.Add(new Box(2500, 1, 2500))));
        }

        public void Update(FrameEventArgs args, float deltaTime)
        {
            // Advance simulation by the timestep. Callers can integrate other systems before or after as needed.
        }
    }
}
