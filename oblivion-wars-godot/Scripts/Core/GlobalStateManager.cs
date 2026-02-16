using Godot;

/// <summary>
/// Centralized state manager that holds all global, player, and level state.
/// This is an autoloaded singleton that persists across the entire game.
/// Access via GlobalStateManager.Instance.Player, .Level, .Global
/// </summary>
public partial class GlobalStateManager : Node
{
    public static GlobalStateManager Instance { get; private set; }

    // State nodes (assigned as children in the scene)
    public PlayerState Player { get; private set; }
    public LevelState Level { get; private set; }
    public GlobalState Global { get; private set; }

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("GlobalStateManager: Duplicate instance detected! Only one should exist.");
            QueueFree();
            return;
        }
        Instance = this;

        // Get child state nodes
        Player = GetNode<PlayerState>("PlayerState");
        Level = GetNode<LevelState>("LevelState");
        Global = GetNode<GlobalState>("GlobalState");

        if (Player == null || Level == null || Global == null)
        {
            GD.PrintErr("GlobalStateManager: Missing required child state nodes!");
        }
        else
        {
            GD.Print("GlobalStateManager: Initialized successfully");
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── Save/Load Helpers ──────────────────────────────────────

    /// <summary>
    /// Create a complete save data snapshot
    /// </summary>
    public CompleteSaveData CreateSaveSnapshot()
    {
        return new CompleteSaveData
        {
            PlayerData = Player.ToSaveData(),
            LevelData = Level.SaveData,
            GlobalData = Global.ToSaveData()
        };
    }

    /// <summary>
    /// Load a complete save data snapshot
    /// </summary>
    public void LoadSaveSnapshot(CompleteSaveData saveData)
    {
        if (saveData == null)
        {
            GD.PrintErr("GlobalStateManager: Cannot load null save data");
            return;
        }

        Player.LoadFromSaveData(saveData.PlayerData);
        Level.SaveData = saveData.LevelData ?? new LevelSaveData();
        Global.LoadFromSaveData(saveData.GlobalData);

        GD.Print("GlobalStateManager: Save data loaded successfully");
    }

    /// <summary>
    /// Reset all state to defaults (new game)
    /// </summary>
    public void ResetToDefaults(PlayerDefinition playerDefinition, string startingLevelId, string startingCheckpointId)
    {
        Player.ResetToDefaults(playerDefinition, startingLevelId, startingCheckpointId);
        Level.Reset();
        Global.ResetToDefaults();

        GD.Print("GlobalStateManager: All state reset to defaults");
    }
}

/// <summary>
/// Complete save data containing all game state
/// </summary>
[GlobalClass]
public partial class CompleteSaveData : Resource
{
    [Export] public PlayerSaveData PlayerData { get; set; }
    [Export] public LevelSaveData LevelData { get; set; }
    [Export] public GlobalSaveData GlobalData { get; set; }
}
