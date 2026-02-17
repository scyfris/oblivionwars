using Godot;

public partial class PlayerInputController : Node
{
    [Export] private PlayerController _controller;

    [Export] private string _moveInputLeftAction = "move_left";
    [Export] private string _moveInputRightAction = "move_right";
    [Export] private string _jumpAction = "jump";
    [Export] private string _useLeftAction = "shoot";
    [Export] private string _useRightAction = "shoot_right";
    [Export] private string _rotateGravityClockwiseAction = "rotate_gravity_cw";
    [Export] private string _rotateGravityCounterClockwiseAction = "rotate_gravity_ccw";
    [Export] private string _interactAction = "interact";

    [ExportGroup("Weapon Switching")]
    [Export] private string[] _weaponSlotActions = new[] {
        "weapon_slot_1", "weapon_slot_2", "weapon_slot_3", "weapon_slot_4",
        "weapon_slot_5", "weapon_slot_6", "weapon_slot_7"
    };
    [Export] private string _weaponNextAction = "weapon_next";
    [Export] private string _weaponPrevAction = "weapon_prev";

    public override void _UnhandledInput(InputEvent @event)
    {
        // Jump
        if (@event.IsActionPressed(_jumpAction))
            _controller.Jump();
        else if (@event.IsActionReleased(_jumpAction))
            _controller.CancelJump();

        // Move left/right
        if (@event.IsActionPressed(_moveInputLeftAction))
            _controller.MoveLeft();
        else if (@event.IsActionPressed(_moveInputRightAction))
            _controller.MoveRight();

        // Cancel movement
        if (@event.IsActionReleased(_moveInputLeftAction) && !Input.IsActionPressed(_moveInputRightAction))
            _controller.Stop();
        else if (@event.IsActionReleased(_moveInputRightAction) && !Input.IsActionPressed(_moveInputLeftAction))
            _controller.Stop();

        // Holdable press/release
        if (@event.IsActionPressed(_useLeftAction))
        {
            var targetPos = _controller.GetGlobalMousePosition();
            _controller.UseHoldablePressed(targetPos, true);
        }
        if (@event.IsActionReleased(_useLeftAction))
        {
            var targetPos = _controller.GetGlobalMousePosition();
            _controller.UseHoldableReleased(targetPos, true);
        }
        if (@event.IsActionPressed(_useRightAction))
        {
            var targetPos = _controller.GetGlobalMousePosition();
            _controller.UseHoldablePressed(targetPos, false);
        }
        if (@event.IsActionReleased(_useRightAction))
        {
            var targetPos = _controller.GetGlobalMousePosition();
            _controller.UseHoldableReleased(targetPos, false);
        }

        // Gravity rotation
        if (@event.IsActionPressed(_rotateGravityClockwiseAction))
            _controller.RotateGravityClockwise();
        else if (@event.IsActionPressed(_rotateGravityCounterClockwiseAction))
            _controller.RotateGravityCounterClockwise();

        // Interact
        if (@event.IsActionPressed(_interactAction))
            _controller.TryInteract();

        // Weapon slot selection
        for (int i = 0; i < _weaponSlotActions.Length; i++)
        {
            if (@event.IsActionPressed(_weaponSlotActions[i]))
            {
                _controller.SelectWeaponSlot(i);
                break;
            }
        }

        // Weapon cycling
        if (@event.IsActionPressed(_weaponNextAction))
            _controller.CycleWeapon(1);
        else if (@event.IsActionPressed(_weaponPrevAction))
            _controller.CycleWeapon(-1);
    }

    public override void _PhysicsProcess(double delta)
    {
        var targetPos = _controller.GetGlobalMousePosition();

        _controller.UpdateAim(targetPos);

        // Call held every frame while button is pressed (for automatic weapons, charged items, etc.)
        if (Input.IsActionPressed(_useLeftAction))
            _controller.UseHoldableHeld(targetPos, true);
        if (Input.IsActionPressed(_useRightAction))
            _controller.UseHoldableHeld(targetPos, false);
    }
}
