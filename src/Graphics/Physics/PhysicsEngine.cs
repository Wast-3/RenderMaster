using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using static RenderMaster.src.Graphics.Physics.PhysicsCallbacks;
using OpenTK.Windowing.Common;


namespace RenderMaster.src.Graphics.Physics
{
    class PhysicsBinding
    {
        public BodyHandle bodyHandle;
        public Model model;
        public PhysicsBinding(BodyHandle bodyHandle, Model model)
        {
            this.bodyHandle = bodyHandle;
            this.model = model;
        }
    }

    class PhysicsEngine
    {
        private BufferPool bufferPool;
        public Simulation simulation;
        private narrowPhase narrowPhase = new narrowPhase();
        private poseIntegrator poseIntegrator = new poseIntegrator();

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

        }

        public void syncModelsToPhysics(List<PhysicsBinding> bindings)
        {
            foreach (var binding in bindings)
            {
                var bodyhandle = simulation.Bodies.GetBodyReference(binding.bodyHandle);
                //sync model position/rotation to physics body
                binding.model.Position = new OpenTK.Mathematics.Vector3(bodyhandle.Pose.Position.X, bodyhandle.Pose.Position.Y, bodyhandle.Pose.Position.Z);
                var q = bodyhandle.Pose.Orientation;
                binding.model.Rotation = new OpenTK.Mathematics.Vector3(0, (float)Math.Atan2(2.0 * (q.W * q.Y + q.X * q.Z), 1.0 - 2.0 * (q.Y * q.Y + q.X * q.X)), 0);
            }
        }
    }
}
