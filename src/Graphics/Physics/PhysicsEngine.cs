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


namespace RenderMaster.src.Graphics.Physics
{
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

        }

        public void Update(float deltaTime)
        {

        }

    }


}
