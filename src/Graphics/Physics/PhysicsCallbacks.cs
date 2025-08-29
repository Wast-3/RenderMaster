using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepuUtilities;
using System.Numerics;

namespace RenderMaster.src.Graphics.Physics
{
    class PhysicsCallbacks
    {
        public struct narrowPhase : INarrowPhaseCallbacks
        {
            public void Initialize(Simulation simulation)
            {
                // Optionally stash references to Simulation/BufferPool/etc.
            }

            // High-level pair filter + chance to tweak speculative margin.
            public bool AllowContactGeneration(
                int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
                => true; // allow all pairs

            // Configure materials (friction / recovery / spring) for any manifold type (convex or nonconvex).
            public bool ConfigureContactManifold<TManifold>(
                int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial)
                where TManifold : unmanaged, IContactManifold<TManifold>
            {
                pairMaterial = new PairMaterialProperties(
                    frictionCoefficient: 0.01f,
                    maximumRecoveryVelocity: 2f,
                    springSettings: new SpringSettings(30f, 1f)); // (stiffness, damping)
                return true; // create a constraint for this manifold
            }

            // Child-level filter (when compounds/meshes are involved). Return false to skip a particular child-child pair.
            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
                => true;

            // Child-level manifold hook (convex only). Return false to exclude this child manifold from parent aggregation.
            public bool ConfigureContactManifold(
                int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
                => true;

            public void Dispose()
            {
                // Free anything you grabbed in Initialize (usually nothing).
            }
        }

        public struct poseIntegrator : IPoseIntegratorCallbacks
        {
            // Constants you choose.
            private Vector3 _gravity;
            private float _linearDamping;
            private float _angularDamping;

            // Cached per PrepareForIntegration.
            private Vector3Wide _gravityWide;   // gravity broadcast to SIMD lanes

            public poseIntegrator(Vector3 gravity, float linearDamping = 0.03f, float angularDamping = 0.03f)
            {
                _gravity = gravity;
                _linearDamping = linearDamping;
                _angularDamping = angularDamping;
                _gravityWide = default; // will be set in PrepareForIntegration
            }

            // Choose a mode (match the samples if in doubt).
            public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.ConserveMomentum;

            // If true, unconstrained bodies take a single full step (fast); otherwise they substep like constrained bodies.
            public bool AllowSubstepsForUnconstrainedBodies => true;

            // Most games leave this false.
            public bool IntegrateVelocityForKinematics => false;

            public void Initialize(Simulation simulation) { /* optional: hold refs */ }

            public void PrepareForIntegration(float dt)
            {
                // Broadcast gravity to all SIMD lanes once; we’ll scale by dt per-lane in IntegrateVelocity.
                Vector3Wide.Broadcast(_gravity, out _gravityWide);
            }

            public void IntegrateVelocity(
                Vector<int> bodyIndices,
                Vector3Wide position,
                QuaternionWide orientation,
                BodyInertiaWide localInertia,
                Vector<int> integrationMask,
                int workerIndex,
                Vector<float> dt,
                ref BodyVelocityWide velocity)
            {
                // velocity.Linear += gravity * dt (per lane)
                Vector3Wide gravityDt;
                Vector3Wide.Scale(_gravityWide, dt, out gravityDt);
                Vector3Wide.Add(velocity.Linear, gravityDt, out velocity.Linear);

                // Exponential-ish damping: v *= max(0, 1 - d*dt)
                var one = new Vector<float>(1f);
                var zero = new Vector<float>(0f);

                var ld = Vector.Max(zero, one - new Vector<float>(_linearDamping) * dt);
                var ad = Vector.Max(zero, one - new Vector<float>(_angularDamping) * dt);

                Vector3Wide.Scale(velocity.Linear, ld, out velocity.Linear);
                Vector3Wide.Scale(velocity.Angular, ad, out velocity.Angular);

                // No need to manually apply integrationMask here: the engine discards inactive lanes;
                // but if you do custom per-lane branching, AND your writes with the mask.
            }
        }

        
    }
}
