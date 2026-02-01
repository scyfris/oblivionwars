using Godot;
using System;
using System.ComponentModel.DataAnnotations.Schema;

public partial class PlayerCharacterBody2D : CharacterBody2D
{
    /// <summary>
    /// Horizontal movement speed in pixels per second
    /// </summary>
    [Export] float _speed = 500.0f;

    /// <summary>
    /// Gravity acceleration in pixels per second squared
    /// </summary>
    [Export] float _gravity = 2000.0f;

    /// <summary>
    /// Initial upward velocity when jumping
    /// </summary>
    [Export] float _jumpStrength = 800.0f;

    /// <summary>
    /// Upward velocity applied when wall jumping
    /// </summary>
    [Export] float _wallJumpStrength = 700.0f;


    /// <summary>
    /// Internal movement direction (-1 = left, 0 = stop, 1 = right). Set by input controller.
    /// </summary>
    [Export] int _moveDirection = 0;

    /// <summary>
    /// Reference to the holdable system that manages weapons and items
    /// </summary>
    [Export] private HoldableSystem _holdableSystem;

    /// <summary>
    /// Wall slide speed as a fraction of normal gravity (0.5 = half speed)
    /// </summary>
    [Export] float _wallSlideSpeedFraction = .5f;

    /// <summary>
    /// Horizontal force pushing player away from wall during wall jump
    /// </summary>
    [Export] float _wallJumpPushAwayForce = 500.0f;

    /// <summary>
    /// Duration in seconds that horizontal push-away force is applied after wall jump (can be longer than input lock)
    /// </summary>
    [Export] float _wallJumpPushAwayDuration = 0.2f;

    /// <summary>
    /// Duration in seconds that horizontal input is locked after wall jump. Moving opposite direction cancels remaining lock time.
    /// </summary>
    [Export] float _wallJumpInputLockDuration = 0.2f;

    [ExportGroup("Wall Slide Effects")]

    /// <summary>
    /// Node2D marker in the scene that defines where wall slide dust particles emit from. Reposition in editor to adjust.
    /// </summary>
    [Export] private Node2D _wallSlideDustPosition;

    /// <summary>
    /// Particle scene to instantiate for wall slide dust. Assign a CpuParticles2D scene to customize the effect in the editor.
    /// </summary>
    [Export] private PackedScene _wallSlideDustScene;

    private bool _isWallSliding = false;
    private Vector2 _wallNormal = Vector2.Zero;
    private float _wallJumpInputLockTimer = 0f;
    private float _wallJumpPushAwayDurationTimer = 0f;
    private CpuParticles2D _wallSlideDust;

    [ExportGroup("Gravity Flip")]

    /// <summary>
    /// How quickly the player sprite rotates visually when gravity changes (higher = faster rotation)
    /// </summary>
    [Export] private float _gravityFlipRotationSpeed = 10.0f;

    /// <summary>
    /// Delay in seconds before the player sprite begins rotating after a gravity flip
    /// </summary>
    [Export] private float _bodyFlipDelay = 0.0f;

    /// <summary>
    /// Whether to rotate the velocity vector when gravity flips to maintain momentum in the new orientation
    /// </summary>
    [Export] private bool _maintainMomentumOnFlip = true;

    // Gravity state tracking
    private int _gravityRotation = 0; // 0, 90, 180, 270 degrees
    private Vector2 _gravityDirection = Vector2.Down; // Current gravity pull direction
    private Vector2 _upDirection = Vector2.Up; // Current up direction
    private float _targetRotation = 0.0f; // Target rotation in radians
    private bool _isRotatingGravity = false; // Currently animating rotation
    private float _bodyFlipDelayTimer = 0.0f; // Countdown before rotation starts

    public override void _Ready()
    {
        if (_wallSlideDustPosition != null && _wallSlideDustScene != null)
        {
            _wallSlideDust = _wallSlideDustScene.Instantiate<CpuParticles2D>();
            _wallSlideDust.Emitting = false;
            _wallSlideDustPosition.AddChild(_wallSlideDust);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
    //    base._PhysicsProcess(delta);

        // Smooth visual rotation toward target (with optional delay)
        if (_isRotatingGravity)
        {
            if (_bodyFlipDelayTimer > 0)
            {
                _bodyFlipDelayTimer -= (float)delta;
            }
            else
            {
                float angularDistance = Mathf.Abs(Mathf.AngleDifference(GlobalRotation, _targetRotation));

                if (angularDistance > 0.01f)
                {
                    float direction = Mathf.Sign(Mathf.AngleDifference(GlobalRotation, _targetRotation));
                    float stepAmount = _gravityFlipRotationSpeed * (float)delta;

                    if (angularDistance <= stepAmount)
                    {
                        // Close enough - snap to target
                        GlobalRotation = _targetRotation;
                        _isRotatingGravity = false;
                    }
                    else
                    {
                        GlobalRotation += direction * stepAmount;
                    }
                }
                else
                {
                    GlobalRotation = _targetRotation;
                    _isRotatingGravity = false;
                }
            }
        }

        // Set CharacterBody2D's up direction (Godot internal)
        UpDirection = _upDirection;

        // Update wall jump input lock timer
        if (_wallJumpInputLockTimer > 0)
        {
            _wallJumpInputLockTimer -= (float)delta;
        }

        if (_wallJumpPushAwayDurationTimer > 0)
        {
            _wallJumpPushAwayDurationTimer -= (float)delta;


            if (_moveDirection < 0 || _moveDirection > 0)
            {
                // Can cancel this one out. The behaviour we want is that
                // the player can cancel horizontal momentum after wall jumping
                // if the press the opposite direction, BUT if they don't then 
                // the horizontal momentum will still continue for a slightly longer time
                // before stopping.  This matches HollowKnight a bit and allows player time to
                // press the opposite arrow key, or if they press the arrow key back towards the
                // wall it will cancel earlier.
                _wallJumpPushAwayDurationTimer = 0;
            }
        }

        // Check for wall sliding
        UpdateWallSliding();

        // Apply appropriate gravity based on state
        float currentGravity = _isWallSliding ? _gravity*_wallSlideSpeedFraction : _gravity;

        // Calculate horizontal direction (perpendicular to gravity)
        // When gravity is down (0,1), horizontal should be right (1,0)
        Vector2 horizontalDirection = new Vector2(_gravityDirection.Y, -_gravityDirection.X);

        // Determine horizontal velocity based on wall jump lock state
        Vector2 horizontalVelocity = new Vector2(0, 0);
        if (_wallJumpInputLockTimer > 0 || _wallJumpPushAwayDurationTimer > 0)
        {
            // During wall jump lock, maintain current horizontal velocity
            // Project current velocity onto horizontal direction
            float currentHorizontalSpeed = Velocity.Dot(horizontalDirection);
            horizontalVelocity = horizontalDirection * currentHorizontalSpeed;
        }
        else
        {
            // Normal movement control along horizontal direction
            horizontalVelocity = horizontalDirection * _moveDirection * _speed;
        }

        // Start with horizontal velocity
        Vector2 newVel = horizontalVelocity;

        // Add current velocity along gravity direction (for falling/jumping)
        float velocityAlongGravity = Velocity.Dot(_gravityDirection);
        newVel += _gravityDirection * velocityAlongGravity;

        // Apply gravity acceleration
        if (!_isWallSliding)
        {
            newVel += _gravityDirection * currentGravity * (float)delta;
        }
        else
        {
            // Wall sliding: clamp velocity along gravity to slide speed
            newVel = horizontalVelocity + _gravityDirection * (_gravity*_wallSlideSpeedFraction);
        }

        this.Velocity = newVel;

        // Move and slide along the floor with the current velocity. Updates velocity based on collisions and such.
        MoveAndSlide();

        // Zero out velocity along gravity when on floor to prevent jittering
        if (IsOnFloor())
        {
            float gravityVelocity = Velocity.Dot(_gravityDirection);
            if (gravityVelocity > 0) // Moving with gravity (falling down)
            {
                // Remove the gravity component from velocity
                Velocity -= _gravityDirection * gravityVelocity;
            }
        }

        // Update holdable system (handles use cooldowns)
        _holdableSystem?.Update(delta);

        // MoveAndCollide - this is more basic and returns collision, but we can create a custom MoveAndSlide with this in the future if
        // we want more functionality.
    }

    public void Jump()
    {
        // Wall jump
        if (_isWallSliding)
        {
            // Jump perpendicular to gravity and away from wall
            Vector2 jumpDirection = -_gravityDirection; // Up (opposite to gravity)
            Vector2 pushDirection = _wallNormal; // Away from wall

            Velocity = pushDirection * _wallJumpPushAwayForce + jumpDirection * _wallJumpStrength;
            _isWallSliding = false;

            // Lock horizontal input briefly to ensure the push-away takes effect
            _wallJumpInputLockTimer = _wallJumpInputLockDuration;
            _wallJumpPushAwayDurationTimer = _wallJumpPushAwayDuration;
            return;
        }

        // Ground jump
        if (!IsOnFloor()) { return; }

        // Jump opposite to gravity direction
        Velocity -= _gravityDirection * _jumpStrength;
    }

    public void CancelJump()
    {
        // Check velocity against gravity, not hardcoded Y
        float velocityAlongGravity = Velocity.Dot(_gravityDirection);
        if (!IsOnFloor() && velocityAlongGravity < 0.0f) // Moving against gravity
        {
            // Cancel upward movement component
            Velocity -= _gravityDirection * velocityAlongGravity;
        }
    }

    public void MoveLeft()
    {
        _moveDirection = -1;
    }
    public void MoveRight()
    {
        _moveDirection = 1;
    }

    public void Stop()
    {
        _moveDirection = 0;
    }

    /// <summary>
    /// Use the currently held item targeting the given position.
    /// For player: target comes from mouse. For NPC: target comes from AI.
    /// </summary>
    public void UseHoldable(Vector2 targetPosition)
    {
        _holdableSystem?.Use(targetPosition);
    }

    /// <summary>
    /// Switch to next holdable in the inventory
    /// </summary>
    public void NextHoldable()
    {
        _holdableSystem?.NextHoldable();
    }

    /// <summary>
    /// Check if player should be wall sliding and update state accordingly.
    /// Wall slide conditions:
    /// - Player is touching a wall (IsOnWall)
    /// - Player is in the air (not on floor)
    /// - Player is moving downward or stationary
    /// - Player is not actively moving away from the wall
    /// </summary>
    private void UpdateWallSliding()
    {
        // Can't wall slide if on the ground
        if (IsOnFloor())
        {
            _isWallSliding = false;
            if (_wallSlideDust != null)
                _wallSlideDust.Emitting = false;
            return;
        }

        // Check if touching a wall
        if (IsOnWall())
        {
            _wallNormal = GetWallNormal();

            // Calculate horizontal direction (perpendicular to gravity)
            Vector2 horizontalDirection = new Vector2(_gravityDirection.Y, -_gravityDirection.X);

            // Determine if player is moving away from wall in gravity-relative space
            // Wall normal points away from wall
            // Check if input direction (along horizontal) matches wall normal direction
            float wallHorizontalDirection = _wallNormal.Dot(horizontalDirection);

            bool movingAwayFromWall = false;
            if (wallHorizontalDirection > 0.1f && _moveDirection > 0) // Wall on "left" (relative to gravity), moving "right"
            {
                movingAwayFromWall = true;
            }
            else if (wallHorizontalDirection < -0.1f && _moveDirection < 0) // Wall on "right" (relative to gravity), moving "left"
            {
                movingAwayFromWall = true;
            }

            // Start wall sliding if moving along gravity and not actively moving away
            float velocityAlongGravity = Velocity.Dot(_gravityDirection);
            if (velocityAlongGravity >= 0 && !movingAwayFromWall)
            {
                _isWallSliding = true;

                // Update dust particle direction to point away from wall
                if (_wallSlideDust != null)
                {
                    _wallSlideDust.Emitting = true;
                    _wallSlideDust.Direction = _wallNormal;
                }
            }
            else
            {
                _isWallSliding = false;
                if (_wallSlideDust != null)
                    _wallSlideDust.Emitting = false;
            }
        }
        else
        {
            _isWallSliding = false;
            if (_wallSlideDust != null)
                _wallSlideDust.Emitting = false;
        }
    }

    /// <summary>
    /// Rotate gravity clockwise 90° (0 -> 90 -> 180 -> 270 -> 0)
    /// </summary>
    public void RotateGravityClockwise()
    {
        RotateGravity(90);
    }

    /// <summary>
    /// Rotate gravity counter-clockwise 90° (0 -> 270 -> 180 -> 90 -> 0)
    /// </summary>
    public void RotateGravityCounterClockwise()
    {
        RotateGravity(-90);
    }

    /// <summary>
    /// Internal gravity rotation handler. Degrees must be a multiple of 90.
    /// </summary>
    private void RotateGravity(int degrees)
    {
        // Snap to nearest 90° and normalize to 0, 90, 180, 270
        _gravityRotation = ((_gravityRotation + degrees + 360) % 360 / 90) * 90;

        // Hardcode all values from the 4 possible gravity states
        // No trig functions - all values are exact integers/constants
        switch (_gravityRotation)
        {
            case 0:   // Gravity down
                _gravityDirection = new Vector2(0, 1);
                _targetRotation = 0;
                break;
            case 90:  // Gravity right
                _gravityDirection = new Vector2(1, 0);
                _targetRotation = -Mathf.Pi / 2;
                break;
            case 180: // Gravity up
                _gravityDirection = new Vector2(0, -1);
                _targetRotation = Mathf.Pi;
                break;
            case 270: // Gravity left
                _gravityDirection = new Vector2(-1, 0);
                _targetRotation = Mathf.Pi / 2;
                break;
            default:  // Should never happen, but clamp to nearest 90
                GD.PrintErr("ERror - incorrect gravity rotation");
                _gravityRotation = (_gravityRotation + 45) / 90 * 90 % 360;
                RotateGravity(0); // Re-enter with clamped value
                return;
        }
        _upDirection = -_gravityDirection;

        _isRotatingGravity = true;
        _bodyFlipDelayTimer = _bodyFlipDelay;

        // Rotate velocity vector to maintain momentum
        if (_maintainMomentumOnFlip)
        {
            Velocity = Velocity.Rotated(Mathf.DegToRad(degrees));
        }

        GD.Print($"Gravity rotated to {_gravityRotation}°, direction: {_gravityDirection}");
    }

    /// <summary>
    /// Get current gravity rotation for camera
    /// </summary>
    public int GetGravityRotation()
    {
        return _gravityRotation;
    }
}
