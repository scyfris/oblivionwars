using Godot;
using System;

public partial class CharacterController : Node
{
    /// <summary>
    /// Reference to the player character that this controller will send input commands to
    /// </summary>
    [Export] PlayerCharacterBody2D _playerCharacter;

    /// <summary>
    /// Input action name for moving left (configured in Project Settings > Input Map)
    /// </summary>
    [Export] string _moveInputLeftAction = "move_left";

    /// <summary>
    /// Input action name for moving right (configured in Project Settings > Input Map)
    /// </summary>
    [Export] string _moveInputRightAction = "move_right";

    /// <summary>
    /// Input action name for jumping (configured in Project Settings > Input Map)
    /// </summary>
    [Export] string _jumpAction = "jump";

    /// <summary>
    /// Input action name for using held item/weapon (configured in Project Settings > Input Map)
    /// </summary>
    [Export] string _useAction = "shoot";

    /// <summary>
    /// Input action name for switching to next weapon/item (configured in Project Settings > Input Map)
    /// </summary>
    [Export] string _switchHoldableAction = "switch_weapon";

    /// <summary>
    /// Input action name for rotating gravity clockwise 90° (configured in Project Settings > Input Map)
    /// </summary>
    [Export] string _rotateGravityClockwiseAction = "rotate_gravity_cw";

    /// <summary>
    /// Input action name for rotating gravity counter-clockwise 90° (configured in Project Settings > Input Map)
    /// </summary>
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
