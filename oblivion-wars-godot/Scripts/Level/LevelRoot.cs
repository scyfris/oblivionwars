using Godot;

public partial class LevelRoot : Node
{
    [Export] private LevelDefinition _levelDefinition;

    public override void _Ready()
    {
        if (_levelDefinition == null)
        {
            GD.PrintErr("LevelRoot: No LevelDefinition assigned!");
            return;
        }

        // Load level save data into LevelState
        var levelData = SaveManager.Instance?.LoadLevelData(_levelDefinition.LevelId);
        LevelState.Instance?.LoadForLevel(
            _levelDefinition.LevelId,
            levelData ?? new LevelSaveData()
        );

        if (LevelState.Instance != null)
            LevelState.Instance.CurrentLevel = _levelDefinition;

        GD.Print($"LevelRoot: Initialized level '{_levelDefinition.LevelDisplayName}' (ID: {_levelDefinition.LevelId})");
    }
}
