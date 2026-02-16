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
