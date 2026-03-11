using Godot;

[GlobalClass]
public partial class SpawnerDefinition : Resource
{
    [ExportGroup("Enemy Table")]
    [Export] public SpawnEntry[] SpawnTable;

    [ExportGroup("Limits")]
    /// <summary>Max enemies alive from this spawner at once. Spawner pauses until one dies.</summary>
    [Export] public int MaxConcurrent = 3;
    /// <summary>Total enemies this spawner will ever produce. 0 = infinite.</summary>
    [Export] public int MaxTotalSpawns = 0;

    [ExportGroup("Timing")]
    /// <summary>Seconds between each spawn.</summary>
    [Export] public float SpawnInterval = 2.0f;
    /// <summary>Delay before first spawn after activation.</summary>
    [Export] public float InitialDelay = 0.5f;

    [ExportGroup("Activation")]
    /// <summary>If true, spawner activates when player is within ActivationRange. If false, spawner is always active.</summary>
    [Export] public bool RequirePlayerProximity = true;
    /// <summary>Distance at which the spawner activates (if RequirePlayerProximity is true).</summary>
    [Export] public float ActivationRange = 600.0f;
}
