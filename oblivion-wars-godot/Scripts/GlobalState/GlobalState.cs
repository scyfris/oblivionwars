using Godot;

/// <summary>
/// Holds global game-wide state that persists across all levels.
/// Examples: defeated bosses, global world flags, story progression, etc.
/// Expand this as needed for your game-specific global state.
/// </summary>
public class GlobalState : ISaveableState<GlobalSaveData>
{
    // ── Transient Runtime State (not serialized) ──────────────
    public bool IsDebugModeEnabled { get; set; } = false;

    // ── Saveable State ──────────────────────────────────────────

    // Global flags for world events (e.g., "bridge_destroyed", "boss_intro_seen", etc.)
    private Godot.Collections.Dictionary<string, bool> _globalFlags = new();

    // Defeated bosses (for multi-level boss tracking)
    private Godot.Collections.Array<string> _defeatedBossIds = new();

    // ── Global Flags ───────────────────────────────────────────

    public void SetGlobalFlag(string flagId, bool value)
    {
        _globalFlags[flagId] = value;
    }

    public bool GetGlobalFlag(string flagId, bool defaultValue = false)
    {
        if (_globalFlags.TryGetValue(flagId, out var value))
            return value;
        return defaultValue;
    }

    public bool HasGlobalFlag(string flagId)
    {
        return _globalFlags.ContainsKey(flagId);
    }

    // ── Boss Tracking ──────────────────────────────────────────

    public void MarkBossDefeated(string bossId)
    {
        if (!_defeatedBossIds.Contains(bossId))
        {
            _defeatedBossIds.Add(bossId);
            GD.Print($"Boss defeated: {bossId}");
        }
    }

    public bool IsBossDefeated(string bossId)
    {
        return _defeatedBossIds.Contains(bossId);
    }

    // ── Serialization ──────────────────────────────────────────

    public GlobalSaveData ToSaveData()
    {
        return new GlobalSaveData
        {
            GlobalFlags = new(_globalFlags),
            DefeatedBossIds = new(_defeatedBossIds)
        };
    }

    public void LoadFromSaveData(GlobalSaveData data)
    {
        if (data == null) return;

        _globalFlags = new(data.GlobalFlags);
        _defeatedBossIds = new(data.DefeatedBossIds);
    }

    public void ResetToDefaults()
    {
        _globalFlags.Clear();
        _defeatedBossIds.Clear();
    }

    Resource ISaveableState.ToSaveData() => ToSaveData();
    void ISaveableState.LoadFromSaveData(Resource data) => LoadFromSaveData((GlobalSaveData)data);
}
