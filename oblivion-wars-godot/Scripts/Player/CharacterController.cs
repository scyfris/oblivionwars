using Godot;
using System;

public partial class CharacterController : Node
{
    [Export] PlayerCharacterBody2D _playerCharacter;

    [Export] string _moveInputLeftAction = "move_left";
    [Export] string _moveInputRightAction = "move_right";
    [Export] string _jumpAction = "jump";
    [Export] string _useAction = "shoot"; // "shoot" for historical reasons, but it's "use" conceptually
    [Export] string _switchHoldableAction = "switch_weapon";
    [Export] string _rotateGravityClockwiseAction = "rotate_gravity_cw";
    [Export] string _rotateGravityCounterClockwiseAction = "rotate_gravity_ccw";

    public override void _UnhandledInput(InputEvent @event)
    {
//        base._UnhandledInput(@event);

        // Jump
        if (@event.IsActionPressed(_jumpAction))
        {
            _playerCharacter.Jump();
        }
        else if (@event.IsActionReleased(_jumpAction))
        {
            _playerCharacter.CancelJump();
        }

        // Move left/right
        if (@event.IsActionPressed(_moveInputLeftAction))
        {
            _playerCharacter.MoveLeft();
        }
        else if (@event.IsActionPressed(_moveInputRightAction))
        {
            _playerCharacter.MoveRight();
        }
        
        // Cancel movement if needed
        if (@event.IsActionReleased(_moveInputLeftAction) && !Input.IsActionPressed(_moveInputRightAction))
        {
            _playerCharacter.Stop();
        }
        else if (@event.IsActionReleased(_moveInputRightAction) && !Input.IsActionPressed(_moveInputLeftAction))
        {
            _playerCharacter.Stop();
        }

        // Use current holdable (weapon/tool/gadget)
        if (@event.IsActionPressed(_useAction))
        {
            // Get mouse position in world space (using player's CanvasItem method)
            var targetPos = _playerCharacter.GetGlobalMousePosition();
            _playerCharacter.UseHoldable(targetPos);
        }

        // Switch to next holdable
        if (@event.IsActionPressed(_switchHoldableAction))
        {
            _playerCharacter.NextHoldable();
        }

        // Gravity rotation inputs
        if (@event.IsActionPressed(_rotateGravityClockwiseAction))
        {
            _playerCharacter.RotateGravityClockwise();
        }
        else if (@event.IsActionPressed(_rotateGravityCounterClockwiseAction))
        {
            _playerCharacter.RotateGravityCounterClockwise();
        }
    }

}
