using Godot;
using System;

public partial class PlayerCharacterBody2D : CharacterBody2D
{
    [Export] float _speed = 500.0f;
    [Export] float _gravity = 2000.0f;
    [Export] float _jumpStrength = 800.0f;
    [Export] int _moveDirection = 0;
    [Export] private HoldableSystem _holdableSystem;

    // Wall sliding and wall jump
    [Export] float _wallSlideSpeed = 100.0f;
    [Export] float _wallJumpStrength = 700.0f;
    [Export] float _wallJumpPushAwayForce = 500.0f;
    [Export] float _wallJumpInputLockDuration = 0.2f; // Time in seconds to lock horizontal input after wall jump

    private bool _isWallSliding = false;
    private Vector2 _wallNormal = Vector2.Zero;
    private float _wallJumpInputLockTimer = 0f;

    public override void _PhysicsProcess(double delta)
    {
    //    base._PhysicsProcess(delta);

        // Update wall jump input lock timer
        if (_wallJumpInputLockTimer > 0)
        {
            _wallJumpInputLockTimer -= (float)delta;
        }

        // Check for wall sliding
        UpdateWallSliding();

        // Apply appropriate gravity based on state
        float currentGravity = _isWallSliding ? _wallSlideSpeed : _gravity;

        // Determine horizontal velocity based on wall jump lock state
        float horizontalVelocity;
        if (_wallJumpInputLockTimer > 0)
        {
            // During wall jump lock, maintain current horizontal velocity (don't apply player input)
            horizontalVelocity = Velocity.X;
        }
        else
        {
            // Normal movement control
            horizontalVelocity = _moveDirection * _speed;
        }

        // Update velocity
        Vector2 newVel = new Vector2(
            horizontalVelocity,
            _isWallSliding ? _wallSlideSpeed : Velocity.Y + currentGravity * (float)delta
        );
        this.Velocity = newVel;

        // Move and slide along the floor with the current velocity. Updates velocity based on collisions and such.
        MoveAndSlide();

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
            // Jump up and away from wall
            float pushDirection = _wallNormal.X; // Wall normal points away from wall
            Velocity = new Vector2(
                pushDirection * _wallJumpPushAwayForce,
                -_wallJumpStrength
            );
            _isWallSliding = false;

            // Lock horizontal input briefly to ensure the push-away takes effect
            _wallJumpInputLockTimer = _wallJumpInputLockDuration;
            return;
        }

        // Ground jump
        if (!IsOnFloor()) { return; }

        // XXX - Need to check the "up" direction
        Velocity = Velocity with { Y = -_jumpStrength };
    }

    public void CancelJump()
    {
        if (!IsOnFloor() && Velocity.Y < 0.0)
        {
            // XXX - Need to check the "up" direction
            Velocity = Velocity with { Y = 0.0f };
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

            // Start wall sliding if moving down and not actively moving away
            if (Velocity.Y >= 0 && !movingAwayFromWall)
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
}
