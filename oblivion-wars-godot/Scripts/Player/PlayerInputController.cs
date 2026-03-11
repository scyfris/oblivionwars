using Godot;

public partial class PlayerInputController : Node
{
    [Export] private PlayerController _controller;

    [Export] private string _moveInputLeftAction = InputMapConstants.MoveLeft;
    [Export] private string _moveInputRightAction = InputMapConstants.MoveRight;
    [Export] private string _jumpAction = InputMapConstants.Jump;
    [Export] private string _useLeftAction = InputMapConstants.Shoot;
    [Export] private string _useRightAction = InputMapConstants.ShootRight;
    [Export] private string _rotateGravityClockwiseAction = InputMapConstants.RotateGravityCW;
    [Export] private string _rotateGravityCounterClockwiseAction = InputMapConstants.RotateGravityCCW;
    [Export] private string _interactAction = InputMapConstants.Interact;

    [ExportGroup("Weapon Switching")]
    [Export] private string[] _weaponSlotActions = new[] {
        InputMapConstants.WeaponSlot1, InputMapConstants.WeaponSlot2, InputMapConstants.WeaponSlot3, InputMapConstants.WeaponSlot4,
        InputMapConstants.WeaponSlot5, InputMapConstants.WeaponSlot6, InputMapConstants.WeaponSlot7
    };
    [Export] private string _weaponNextAction = InputMapConstants.WeaponNext;
    [Export] private string _weaponPrevAction = InputMapConstants.WeaponPrev;

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
            _controller.UseHoldablePressed(true);
        if (@event.IsActionReleased(_useLeftAction))
            _controller.UseHoldableReleased(true);
        if (@event.IsActionPressed(_useRightAction))
            _controller.UseHoldablePressed(false);
        if (@event.IsActionReleased(_useRightAction))
            _controller.UseHoldableReleased(false);

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
            _controller.UseHoldableHeld(true);
        if (Input.IsActionPressed(_useRightAction))
            _controller.UseHoldableHeld(false);
    }
}
