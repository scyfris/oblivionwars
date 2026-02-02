using Godot;

public partial class DebugHUD : Label
{
    [Export] private NodePath _playerPath;

    private IGameEntity _player;

    public override void _Ready()
    {
        if (_playerPath != null)
        {
            var node = GetNode(_playerPath);
            if (node is IGameEntity entity)
                _player = entity;
        }
    }

    public override void _Process(double delta)
    {
        if (_player?.RuntimeData == null)
        {
            Text = "Health: --";
            return;
        }

        Text = $"Health: {_player.RuntimeData.CurrentHealth:F0} / {_player.RuntimeData.MaxHealth:F0}";
    }
}
