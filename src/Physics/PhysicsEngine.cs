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
using static RenderMaster.src.Physics.PhysicsCallbacks;
using OpenTK.Windowing.Common;


namespace RenderMaster.src.Physics
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
        private poseIntegrator poseIntegrator = new poseIntegrator(new System.Numerics.Vector3(0f, -3.81f, 0f), 0.01f, 0.02f);

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
                //sync model position/rotation to physics body. keep in mind bepuphysics uses a right-handed coordinate system with Y up, while opentk uses a right-handed coordinate system with Z up.
                binding.model.Position = new OpenTK.Mathematics.Vector3(bodyhandle.Pose.Position.X, bodyhandle.Pose.Position.Y, bodyhandle.Pose.Position.Z);

                var q = bodyhandle.Pose.Orientation;
                binding.model.Rotation = ToEulerXYZ((OpenTK.Mathematics.Quaternion)q);
            }
        }

        static OpenTK.Mathematics.Vector3 ToEulerXYZ(OpenTK.Mathematics.Quaternion q)
        {
            // standard quaternion->euler (XYZ) conversion
            float sinr_cosp = 2f * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
            float roll = MathF.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2f * (q.W * q.Y - q.Z * q.X);
            float pitch = MathF.Abs(sinp) >= 1f ? MathF.CopySign(MathF.PI / 2f, sinp) : MathF.Asin(sinp);

            float siny_cosp = 2f * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

            return new OpenTK.Mathematics.Vector3(roll, yaw, pitch);
        }


    }

}
