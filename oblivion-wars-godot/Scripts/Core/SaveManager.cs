using Godot;

public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }

    public const int MaxSlots = 3;

    public int ActiveSlotIndex = -1;
    public bool IsRespawning = false;

    private GlobalSaveData _globalData = new();

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("SaveManager: Duplicate instance detected, removing this one.");
            QueueFree();
            return;
        }
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── Core Operations ────────────────────────────────────

    public void Save()
    {
        if (ActiveSlotIndex < 0)
        {
            GD.PrintErr("SaveManager: No active slot, cannot save.");
            return;
        }

        string slotDir = GetSlotDirectory(ActiveSlotIndex);
        EnsureDirectoryExists(slotDir);
        EnsureDirectoryExists(slotDir + "levels/");

        // Save player data
        var playerData = PlayerState.Instance?.ToSaveData() ?? new PlayerSaveData();
        ResourceSaver.Save(playerData, slotDir + "player.tres");

        // Save global data
        ResourceSaver.Save(_globalData, slotDir + "global.tres");

        // Save current level data
        if (LevelState.Instance?.CurrentLevel != null)
        {
            var levelId = LevelState.Instance.CurrentLevel.LevelId;
            SaveLevelData(levelId, LevelState.Instance.SaveData);
        }

        GD.Print($"SaveManager: Saved to slot {ActiveSlotIndex}");
    }

    public void Load(int slot)
    {
        string slotDir = GetSlotDirectory(slot);

        // Load player data
        string playerPath = slotDir + "player.tres";
        if (ResourceLoader.Exists(playerPath))
        {
            var playerData = ResourceLoader.Load<PlayerSaveData>(playerPath);
            PlayerState.Instance?.LoadFromSaveData(playerData);
        }

        // Load global data
        string globalPath = slotDir + "global.tres";
        if (ResourceLoader.Exists(globalPath))
            _globalData = ResourceLoader.Load<GlobalSaveData>(globalPath);
        else
            _globalData = new GlobalSaveData();

        ActiveSlotIndex = slot;
        GD.Print($"SaveManager: Loaded slot {slot}");
    }

    public void CreateNewGame(int slot, string startingLevelId, uint startingCheckpointId)
    {
        // Delete any existing save in this slot
        DeleteSlot(slot);

        ActiveSlotIndex = slot;
        _globalData = new GlobalSaveData();

        // Reset PlayerState to defaults
        // Note: PlayerDefinition reference not available here — caller can set MaxHealth after if needed
        PlayerState.Instance?.ResetToDefaults(null, startingLevelId, startingCheckpointId);

        // Create the slot directory and save initial state
        Save();

        GD.Print($"SaveManager: Created new game in slot {slot}, level={startingLevelId}, checkpoint={startingCheckpointId}");
    }

    public void DeleteSlot(int slot)
    {
        string slotDir = GetSlotDirectory(slot);
        if (!DirAccess.DirExistsAbsolute(slotDir)) return;

        // Delete all files in the slot recursively
        DeleteDirectoryRecursive(slotDir);
        GD.Print($"SaveManager: Deleted slot {slot}");
    }

    public bool SlotExists(int slot)
    {
        string playerPath = GetSlotDirectory(slot) + "player.tres";
        return ResourceLoader.Exists(playerPath);
    }

    public PlayerSaveData GetSlotPreview(int slot)
    {
        string playerPath = GetSlotDirectory(slot) + "player.tres";
        if (!ResourceLoader.Exists(playerPath)) return null;
        return ResourceLoader.Load<PlayerSaveData>(playerPath);
    }

    public void ReloadLastSave()
    {
        if (ActiveSlotIndex >= 0)
            Load(ActiveSlotIndex);
    }

    // ── Level-Specific Save Management ─────────────────────

    public LevelSaveData LoadLevelData(string levelId)
    {
        if (ActiveSlotIndex < 0) return null;

        string path = GetSlotDirectory(ActiveSlotIndex) + $"levels/level_{levelId}.tres";
        if (!ResourceLoader.Exists(path)) return null;

        return ResourceLoader.Load<LevelSaveData>(path);
    }

    public void SaveLevelData(string levelId, LevelSaveData data)
    {
        if (ActiveSlotIndex < 0) return;

        string levelsDir = GetSlotDirectory(ActiveSlotIndex) + "levels/";
        EnsureDirectoryExists(levelsDir);

        data.LevelId = levelId;
        ResourceSaver.Save(data, levelsDir + $"level_{levelId}.tres");
    }

    /// <summary>
    /// Get the scene path for a level by its ID. Looks up the LevelDefinition resource.
    /// </summary>
    public string GetLevelScenePath(string levelId)
    {
        // Try to load a LevelDefinition resource by convention
        string defPath = $"res://Resources/Data/Levels/{levelId}.tres";
        if (ResourceLoader.Exists(defPath))
        {
            var def = ResourceLoader.Load<LevelDefinition>(defPath);
            if (def != null && !string.IsNullOrEmpty(def.ScenePath))
                return def.ScenePath;
        }

        GD.PrintErr($"SaveManager: Could not find scene path for level '{levelId}'");
        return "";
    }

    // ── Helpers ────────────────────────────────────────────

    private string GetSlotDirectory(int slot)
    {
        return $"user://saves/slot_{slot}/";
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!DirAccess.DirExistsAbsolute(path))
            DirAccess.MakeDirRecursiveAbsolute(path);
    }

    private void DeleteDirectoryRecursive(string path)
    {
        using var dir = DirAccess.Open(path);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            string fullPath = path + fileName;
            if (dir.CurrentIsDir())
            {
                DeleteDirectoryRecursive(fullPath + "/");
                DirAccess.RemoveAbsolute(fullPath);
            }
            else
            {
                DirAccess.RemoveAbsolute(fullPath);
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        DirAccess.RemoveAbsolute(path);
    }
}
