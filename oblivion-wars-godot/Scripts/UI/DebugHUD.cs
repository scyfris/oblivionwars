using Godot;

public partial class DebugHUD : Label
{
    [Export] private PlayerController _playerController;

    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        if (_playerController?.PlayerStateCurrent == null)
        {
            Text = "Health: --";
            return;
        }

        Text = $"Health: {_playerController.PlayerStateCurrent.CurrentHealth:F0} / {_playerController.PlayerStateCurrent.MaxHealth:F0}";
    }
}
