using Godot;

/// <summary>
/// A spawn location for EnemySpawner. Place as a child of an EnemySpawner node.
/// Optionally override the spawner's enemy table with a per-point list
/// (e.g., flying enemies at elevated points, grounded enemies on platforms).
/// If EnemyTable is empty, the spawner's default table is used.
/// </summary>
[GlobalClass]
public partial class SpawnPoint : Marker2D
{
    [Export] public SpawnEntry[] EnemyTable;

    public bool HasCustomTable => EnemyTable != null && EnemyTable.Length > 0;
}
