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

    private bool _isWallSliding = false;
    private Vector2 _wallNormal = Vector2.Zero;
    private float _wallJumpInputLockTimer = 0f;
    private float _wallJumpPushAwayDurationTimer = 0f;

    [ExportGroup("Gravity Flip")]

    /// <summary>
    /// How quickly the player sprite rotates visually when gravity changes (higher = faster rotation)
    /// </summary>
    [Export] private float _gravityFlipRotationSpeed = 10.0f;

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

    public override void _PhysicsProcess(double delta)
    {
    //    base._PhysicsProcess(delta);

        // Smooth visual rotation toward target
        if (_isRotatingGravity)
        {
            GlobalRotation = Mathf.LerpAngle(GlobalRotation, _targetRotation,
                                             _gravityFlipRotationSpeed * (float)delta);

            if (Mathf.Abs(GlobalRotation - _targetRotation) < 0.01f)
            {
                GlobalRotation = _targetRotation;
                _isRotatingGravity = false;
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
            return;
        }

        // Check if touching a wall
        if (IsOnWall())
        {
            _wallNormal = GetWallNormal();

            // Determine if player is moving away from wall
            // Wall normal points away from wall, so if move direction matches wall normal direction, player is moving away
            bool movingAwayFromWall = false;
            if (_wallNormal.X > 0 && _moveDirection > 0) // Wall on left, moving right (away)
            {
                movingAwayFromWall = true;
            }
            else if (_wallNormal.X < 0 && _moveDirection < 0) // Wall on right, moving left (away)
            {
                movingAwayFromWall = true;
            }

            // Start wall sliding if moving along gravity and not actively moving away
            float velocityAlongGravity = Velocity.Dot(_gravityDirection);
            if (velocityAlongGravity >= 0 && !movingAwayFromWall)
            {
                _isWallSliding = true;
            }
            else
            {
                _isWallSliding = false;
            }
        }
        else
        {
            _isWallSliding = false;
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
    /// Internal gravity rotation handler
    /// </summary>
    private void RotateGravity(int degrees)
    {
        // Update rotation state (normalize to 0-359)
        _gravityRotation = (_gravityRotation + degrees + 360) % 360;

        // Calculate new directions from rotation
        float radians = Mathf.DegToRad(_gravityRotation);
        float gravX = Mathf.Sin(radians);
        float gravY = Mathf.Cos(radians);
        gravX = Mathf.IsZeroApprox(gravX) ? 0 : Mathf.IsEqualApprox(gravX, 1) ? 1 : -1;
        gravY = Mathf.IsZeroApprox(gravY) ? 0 : Mathf.IsEqualApprox(gravY, 1) ? 1 : -1;
        _gravityDirection = new Vector2(gravX, gravY);
        _upDirection = -_gravityDirection;

        // Set target for smooth visual rotation
        _targetRotation = radians;
        _isRotatingGravity = true;

        // Rotate velocity vector to maintain momentum
        if (_maintainMomentumOnFlip)
        {
            float rotationRad = Mathf.DegToRad(degrees);
            Velocity = Velocity.Rotated(rotationRad);
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
