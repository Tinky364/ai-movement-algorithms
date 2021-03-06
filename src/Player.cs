using Ai;
using Godot;
using Manager;

public class Player : KinematicBody
{
    [Export(PropertyHint.Range, "0,100,or_greater")]
    public int MoveSpeed = 20;
    [Export(PropertyHint.Range, "0,500,or_greater")]
    public int MoveAcceleration = 80;
    [Export(PropertyHint.Range, "0,500,or_greater")]
    public int FallAcceleration = 60;
    [Export(PropertyHint.Range, "0,500,or_greater")]
    public int JitterAcceleration = 20;
    [Export(PropertyHint.Range, "0,500,or_greater")]
    public int RotationAcceleration = 40;
    
    public Vector3 Forward { get; private set; }
    public Vector3 Velocity => _velocity;
    private Spatial _pivot;
    private Vector3 _velocity;
    private Vector3 _inputAxis;
    
    public AiInfo AiInfo;

    public override void _EnterTree()
    {
        AiInfo = new AiInfo();
    }

    public override void _Ready()
    {
        _pivot= GetNode<Spatial>("Pivot");
    }

    public override void _Process(float delta)
    {
        Drawer.S.DrawLine(this, Vector3.Zero, Forward);
    }
    
    public override void _PhysicsProcess(float delta)
    {
        _inputAxis = CalculateAxisInput();
        _velocity = CalculateVelocity(_velocity, _inputAxis, MoveSpeed, MoveAcceleration, delta);
        _velocity = CalculateGravity(_velocity, FallAcceleration, delta);
        _pivot.RotationDegrees = new Vector3
        {
            x = _pivot.RotationDegrees.x,
            y = NewOrientation(_pivot.RotationDegrees.y, _inputAxis, RotationAcceleration, delta),
            z = _pivot.RotationDegrees.z
        };
        Forward = _pivot.Transform.basis.z;
        _velocity = MoveAndSlide(_velocity, Vector3.Up);
        
        AiInfo.Sync(GlobalTransform.origin, _pivot.RotationDegrees.y, _velocity, 0);
    }

    private Vector3 CalculateAxisInput()
    {
        Vector3 inputAxis = new Vector3
        {
            x = InputManager.GetAxis("move_right", "move_left"),
            y = 0,
            z = InputManager.GetAxis("move_back", "move_forward")
        };
        if (inputAxis != Vector3.Zero) inputAxis = inputAxis.Normalized();
        return inputAxis;
    }

    private float NewOrientation(float current, Vector3 direction, float acceleration, float delta)
    {
        if (direction == Vector3.Zero) return current;
        float targetOrientation = Mathff.DirectionToOrientation(direction);
        float diff = Mathff.DeltaAngle(current, targetOrientation);
        if (Mathf.Abs(diff) <= 0.1f) return current;
        current = Mathf.MoveToward(current, current + diff, acceleration * delta);
        return current;
    }

    private Vector3 CalculateVelocity(
        Vector3 velocity, Vector3 direction, float maxMoveSpeed, float moveAcceleration, float delta
    )
    {
        Vector3 desiredVelocity = maxMoveSpeed * direction;
        velocity.x = Mathf.MoveToward(velocity.x, desiredVelocity.x, moveAcceleration * delta);
        velocity.z = Mathf.MoveToward(velocity.z, desiredVelocity.z, moveAcceleration * delta);
        return velocity;
    }

    private Vector3 CalculateGravity(Vector3 velocity, float fallAcceleration, float delta)
    {
        if (IsOnFloor()) velocity.y = -fallAcceleration * delta;
        else velocity.y -= fallAcceleration * delta;
        return velocity;
    }

    private void InterpolateTranslate(float delta)
    {
        float fps = GM.FramesPerSecond;
        var lerpInterval = _velocity / fps;
        var lerpPosition = GlobalTransform.origin + lerpInterval;
        if (Mathf.FloorToInt(fps) > GM.PhysicsFramesPerSecond + 2f)
        {
            _pivot.SetAsToplevel(true);
            Transform globalTransform = _pivot.GlobalTransform;
            globalTransform.origin = _pivot.GlobalTransform.origin.LinearInterpolate(
                lerpPosition, JitterAcceleration * delta
            );
            _pivot.GlobalTransform = globalTransform;
        }
        else
        {
            _pivot.GlobalTransform = GlobalTransform;
            _pivot.SetAsToplevel(false);
        }
    }
    
    /*private void CalculateRot(float delta)
    {
        if (_inputAxis == Vector3.Zero) return;
        
        float targetRotationY = Mathf.Rad2Deg(Mathf.Atan2(_inputAxis.x, _inputAxis.z));
        var targetQuat = new Quat(Vector3.Up, targetRotationY);
        
        float curRotationY = _pivot.RotationDegrees.y;
        if (Math.Abs(curRotationY - targetRotationY) <= 0.1f) return;

        var curQuat = Transform.basis.Quat();
        curQuat = RotateToward(
            curQuat, targetQuat, RotationAcceleration * delta
        );
        Transform = new Transform(curQuat, Transform.origin);
    }
    
    private static Quat RotateToward(Quat from, Quat to, float delta)
    {
        float angle = from.AngleTo(to);
        if (angle == 0.0f) return to;
        return from.Slerp(to, Mathf.Min(1.0f, delta / angle));
    }*/
}