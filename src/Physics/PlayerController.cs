using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;

namespace RenderMaster.src.Physics;

public sealed class PlayerController
{
    readonly Simulation _simulation;
    readonly BodyHandle _bodyHandle;
    readonly TypedIndex _shapeIndex;
    readonly float _halfLength;
    readonly float _eyeHeight;

    bool _grounded;
    bool _supportThisStep;
    float _bestSupportDepth;
    Vector3 _supportNormal;

    public float MaxGroundSpeed { get; set; } = 8.5f;
    public float MaxAirSpeed { get; set; } = 8.5f;
    public float GroundAcceleration { get; set; } = 65f;
    public float AirAcceleration { get; set; } = 35f;
    public float GroundFriction { get; set; } = 8.5f;
    public float JumpSpeed { get; set; } = 7.5f;
    public float CosMaximumSlope { get; set; } = MathF.Cos(MathF.PI * 0.45f);

    public Vector3 Up => Vector3.UnitY;
    public BodyHandle BodyHandle => _bodyHandle;
    public bool IsGrounded => _grounded;
    public Vector3 SupportNormal => _supportNormal;
    public float EyeHeight => _eyeHeight;

    public PlayerController(Simulation simulation, Vector3 initialPosition, float totalHeight, float radius, float mass = 80f)
    {
        _simulation = simulation;
        var cylinderLength = MathF.Max(0f, totalHeight - 2f * radius);
        _halfLength = cylinderLength * 0.5f;
        _eyeHeight = _halfLength + radius * 0.9f;

        var capsule = new Capsule(radius, cylinderLength);
        var bodyDescription = BodyDescription.CreateDynamic(
            new RigidPose(initialPosition),
            new BodyInertia { InverseMass = 1f / mass },
            new CollidableDescription(simulation.Shapes.Add(capsule), radius * 0.02f, float.MaxValue, ContinuousDetection.Passive),
            new BodyActivityDescription(0.01f));

        // Prevent the controller from tipping over when colliding with geometry.
        bodyDescription.LocalInertia.InverseInertiaTensor = default;

        _bodyHandle = simulation.Bodies.Add(bodyDescription);
        _shapeIndex = bodyDescription.Collidable.Shape;

        ref var body = ref simulation.Bodies.GetBodyReference(_bodyHandle);
        body.Pose.Orientation = Quaternion.Identity;
    }

    public void Dispose()
    {
        _simulation.Shapes.Remove(_shapeIndex);
        _simulation.Bodies.Remove(_bodyHandle);
    }

    public void TeleportToCenter(Vector3 position)
    {
        ref var body = ref _simulation.Bodies.GetBodyReference(_bodyHandle);
        body.Pose.Position = position;
        body.Pose.Orientation = Quaternion.Identity;
        body.Velocity.Linear = Vector3.Zero;
        body.Velocity.Angular = Vector3.Zero;
        _grounded = false;
    }

    public void TeleportToMatchCamera(Vector3 cameraPosition)
    {
        var center = cameraPosition - new Vector3(0f, _eyeHeight - 0.1f, 0f);
        TeleportToCenter(center);
    }

    public Vector3 Position
    {
        get
        {
            return _simulation.Bodies.GetBodyReference(_bodyHandle).Pose.Position;
        }
    }

    public Vector3 EyeWorldPosition => Position + new Vector3(0f, _eyeHeight, 0f);

    public void ApplyInput(float dt, Vector2 movementInput, bool sprintHeld, bool jumpPressed, Vector3 cameraForward, Vector3 cameraRight)
    {
        ref var body = ref _simulation.Bodies.GetBodyReference(_bodyHandle);
        body.Awake = true;
        body.Pose.Orientation = Quaternion.Identity;
        body.Velocity.Angular = Vector3.Zero;

        var velocity = body.Velocity.Linear;
        var horizontalVelocity = new Vector3(velocity.X, 0f, velocity.Z);

        var forward = ProjectToPlane(cameraForward);
        var right = ProjectToPlane(cameraRight);

        Vector3 desiredDirection = Vector3.Zero;
        if (movementInput.LengthSquared() > 0f)
        {
            desiredDirection = forward * movementInput.Y + right * movementInput.X;
            if (desiredDirection.LengthSquared() > 0f)
                desiredDirection = Vector3.Normalize(desiredDirection);
        }

        var targetSpeed = MaxGroundSpeed * (sprintHeld ? 1.6f : 1f);
        var maxAirSpeed = MaxAirSpeed * (sprintHeld ? 1.25f : 1f);

        if (_grounded)
        {
            if (horizontalVelocity.LengthSquared() > 0f && desiredDirection == Vector3.Zero && !jumpPressed)
            {
                ApplyFriction(ref horizontalVelocity, GroundFriction, dt);
            }

            if (desiredDirection != Vector3.Zero)
            {
                Accelerate(ref horizontalVelocity, desiredDirection, targetSpeed, GroundAcceleration, dt, targetSpeed);
            }

            if (jumpPressed)
            {
                velocity.Y = JumpSpeed;
                _grounded = false;
                _supportThisStep = false;
            }
            else if (velocity.Y < 0f)
            {
                velocity.Y = MathF.Max(velocity.Y, -1f);
            }
        }
        else
        {
            if (desiredDirection != Vector3.Zero)
            {
                Accelerate(ref horizontalVelocity, desiredDirection, maxAirSpeed, AirAcceleration, dt, maxAirSpeed);
            }
        }

        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        body.Velocity.Linear = velocity;
    }

    static Vector3 ProjectToPlane(Vector3 v)
    {
        var projected = new Vector3(v.X, 0f, v.Z);
        var lengthSquared = projected.LengthSquared();
        if (lengthSquared < 1e-6f)
        {
            return Vector3.UnitX;
        }

        return projected / MathF.Sqrt(lengthSquared);
    }

    static void ApplyFriction(ref Vector3 velocity, float friction, float dt)
    {
        var speed = velocity.Length();
        if (speed < 1e-6f)
            return;

        var drop = speed * friction * dt;
        var newSpeed = MathF.Max(0f, speed - drop);
        velocity *= newSpeed / speed;
    }

    static void Accelerate(ref Vector3 velocity, Vector3 wishDir, float wishSpeed, float acceleration, float dt, float maxSpeed)
    {
        if (wishSpeed <= 0f)
            return;

        var currentSpeed = Vector3.Dot(velocity, wishDir);
        var addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f)
            return;

        var accelSpeed = acceleration * dt * wishSpeed;
        if (accelSpeed > addSpeed)
            accelSpeed = addSpeed;

        velocity += wishDir * accelSpeed;

        var horizontal = new Vector2(velocity.X, velocity.Z);
        var horizontalSpeed = horizontal.Length();
        if (horizontalSpeed > maxSpeed && horizontalSpeed > 0f)
        {
            var scale = maxSpeed / horizontalSpeed;
            velocity.X *= scale;
            velocity.Z *= scale;
        }
    }

    public void BeginStep()
    {
        _supportThisStep = false;
        _bestSupportDepth = float.MinValue;
        _supportNormal = Vector3.UnitY;
    }

    public void EndStep()
    {
        _grounded = _supportThisStep;
        if (!_grounded)
        {
            _supportNormal = Vector3.UnitY;
        }
    }

    internal void RegisterSupport(Vector3 normal, float depth)
    {
        if (depth >= _bestSupportDepth)
        {
            _bestSupportDepth = depth;
            _supportNormal = normal;
        }
        _supportThisStep = true;
    }
}
