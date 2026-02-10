using Godot;

public partial class CharacterController : Node
{
    [Export] PlayerCharacterBody2D _playerCharacter;

    [Export] string _moveInputLeftAction = "move_left";
    [Export] string _moveInputRightAction = "move_right";
    [Export] string _jumpAction = "jump";
    [Export] string _useLeftAction = "shoot";
    [Export] string _useRightAction = "shoot_right";
    [Export] string _rotateGravityClockwiseAction = "rotate_gravity_cw";
    [Export] string _rotateGravityCounterClockwiseAction = "rotate_gravity_ccw";

    public override void _UnhandledInput(InputEvent @event)
    {
        // Jump
        if (@event.IsActionPressed(_jumpAction))
            _playerCharacter.Jump();
        else if (@event.IsActionReleased(_jumpAction))
            _playerCharacter.CancelJump();

        // Move left/right
        if (@event.IsActionPressed(_moveInputLeftAction))
            _playerCharacter.MoveLeft();
        else if (@event.IsActionPressed(_moveInputRightAction))
            _playerCharacter.MoveRight();

        // Cancel movement
        if (@event.IsActionReleased(_moveInputLeftAction) && !Input.IsActionPressed(_moveInputRightAction))
            _playerCharacter.Stop();
        else if (@event.IsActionReleased(_moveInputRightAction) && !Input.IsActionPressed(_moveInputLeftAction))
            _playerCharacter.Stop();

        // Holdable press/release (detected on input event, not polled)
        if (@event.IsActionPressed(_useLeftAction))
        {
            var targetPos = _playerCharacter.GetGlobalMousePosition();
            _playerCharacter.UseHoldablePressed(targetPos, true);
        }
        if (@event.IsActionReleased(_useLeftAction))
        {
            var targetPos = _playerCharacter.GetGlobalMousePosition();
            _playerCharacter.UseHoldableReleased(targetPos, true);
        }
        if (@event.IsActionPressed(_useRightAction))
        {
            var targetPos = _playerCharacter.GetGlobalMousePosition();
            _playerCharacter.UseHoldablePressed(targetPos, false);
        }
        if (@event.IsActionReleased(_useRightAction))
        {
            var targetPos = _playerCharacter.GetGlobalMousePosition();
            _playerCharacter.UseHoldableReleased(targetPos, false);
        }

        // Gravity rotation
        if (@event.IsActionPressed(_rotateGravityClockwiseAction))
            _playerCharacter.RotateGravityClockwise();
        else if (@event.IsActionPressed(_rotateGravityCounterClockwiseAction))
            _playerCharacter.RotateGravityCounterClockwise();
    }

    public override void _PhysicsProcess(double delta)
    {
        var targetPos = _playerCharacter.GetGlobalMousePosition();

        _playerCharacter.UpdateAim(targetPos);

        // Call held every frame while button is pressed (for automatic weapons, charged items, etc.)
        if (Input.IsActionPressed(_useLeftAction))
            _playerCharacter.UseHoldableHeld(targetPos, true);
        if (Input.IsActionPressed(_useRightAction))
            _playerCharacter.UseHoldableHeld(targetPos, false);
    }
}
