using Godot;
using System.Collections.Generic;

/// <summary>
/// Spawns enemies at child Marker2D positions based on a SpawnerDefinition.
/// Tracks alive spawned enemies via EntityDiedEvent (event-driven, no polling).
/// Add Marker2D children as spawn points; if none exist, spawns at the spawner's position.
/// </summary>
public partial class EnemySpawner : Node2D
{
    [Export] public SpawnerDefinition _definition;

    [Signal]
    public delegate void AllEnemiesDeadEventHandler();

    private readonly List<SpawnPoint> _spawnPoints = new();

    // Track spawned enemies by instance ID → node reference (for force-kill)
    private readonly Dictionary<ulong, Node> _aliveEnemies = new();

    private PlayerCharacterBody2D _cachedPlayer;
    private float _spawnTimer;
    private int _totalSpawned;
    private bool _active;
    private bool _stopped;
    private int _nextSpawnPointIndex;

    public override void _Ready()
    {
        if (_definition == null)
        {
            GD.PrintErr($"{Name}: No SpawnerDefinition assigned!");
            return;
        }

        // Collect child SpawnPoint nodes as spawn points
        foreach (var child in GetChildren())
        {
            if (child is SpawnPoint sp)
                _spawnPoints.Add(sp);
        }

        _spawnTimer = _definition.InitialDelay;

        // Subscribe to death events for tracking
        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);

        if (!_definition.RequirePlayerProximity)
            _active = true;
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_definition == null || _stopped) return;

        if (!_active)
        {
            CheckPlayerProximity();
            return;
        }

        // Check if we've hit the total spawn limit
        if (_definition.MaxTotalSpawns > 0 && _totalSpawned >= _definition.MaxTotalSpawns)
            return;

        // Check concurrency limit
        if (_aliveEnemies.Count >= _definition.MaxConcurrent)
            return;

        _spawnTimer -= (float)delta;
        if (_spawnTimer <= 0f)
        {
            SpawnEnemy();
            _spawnTimer = _definition.SpawnInterval;
        }
    }

    // ── Public API ──────────────────────────────────────────

    /// <summary>Stop spawning. Existing enemies remain alive.</summary>
    public void Stop()
    {
        _stopped = true;
    }

    /// <summary>Resume spawning after a Stop() call.</summary>
    public void Resume()
    {
        _stopped = false;
    }

    /// <summary>Stop spawning and kill all alive enemies through their normal death flow.</summary>
    public void StopAndKillAll()
    {
        _stopped = true;

        // Copy keys since Die() will modify _aliveEnemies via the EntityDiedEvent callback
        var ids = new List<ulong>(_aliveEnemies.Keys);
        foreach (var id in ids)
        {
            if (!_aliveEnemies.TryGetValue(id, out var enemy)) continue;
            if (!GodotObject.IsInstanceValid(enemy) || enemy.IsQueuedForDeletion()) continue;

            // Find NPCController and call Die() for normal death flow
            var controller = FindNPCController(enemy);
            if (controller != null)
                controller.Die();
            else
                enemy.QueueFree(); // Fallback if no controller found
        }
        _aliveEnemies.Clear();
    }

    public int AliveCount => _aliveEnemies.Count;
    public bool IsStopped => _stopped;
    public bool IsFinished => _definition.MaxTotalSpawns > 0
        && _totalSpawned >= _definition.MaxTotalSpawns
        && _aliveEnemies.Count == 0;

    // ── Internal ────────────────────────────────────────────

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (!_aliveEnemies.Remove(evt.EntityInstanceId)) return;

        // All spawned enemies dead and we've hit the limit
        if (_aliveEnemies.Count == 0 && _definition.MaxTotalSpawns > 0 && _totalSpawned >= _definition.MaxTotalSpawns)
            EmitSignal(SignalName.AllEnemiesDead);
    }

    private void CheckPlayerProximity()
    {
        var player = GetPlayer();
        if (player == null) return;

        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
        if (distance <= _definition.ActivationRange)
            _active = true;
    }

    private void SpawnEnemy()
    {
        var point = GetNextSpawnPoint();
        var table = point != null && point.HasCustomTable ? point.EnemyTable : _definition.SpawnTable;
        var entry = PickRandomEntry(table);
        if (entry?.EnemyScene == null) return;

        var enemy = entry.EnemyScene.Instantiate<Node2D>();
        enemy.GlobalPosition = point?.GlobalPosition ?? GlobalPosition;

        // Add as sibling so enemy lives in the level, not under the spawner
        GetParent().AddChild(enemy);

        _aliveEnemies[enemy.GetInstanceId()] = enemy;
        _totalSpawned++;
    }

    private SpawnPoint GetNextSpawnPoint()
    {
        if (_spawnPoints.Count == 0)
            return null;

        var point = _spawnPoints[_nextSpawnPointIndex];
        _nextSpawnPointIndex = (_nextSpawnPointIndex + 1) % _spawnPoints.Count;
        return point;
    }

    private static SpawnEntry PickRandomEntry(SpawnEntry[] table)
    {
        if (table == null || table.Length == 0)
            return null;

        float totalWeight = 0f;
        foreach (var entry in table)
            totalWeight += entry.Weight;

        float roll = (float)GD.RandRange(0, totalWeight);
        float cumulative = 0f;

        foreach (var entry in table)
        {
            cumulative += entry.Weight;
            if (roll <= cumulative)
                return entry;
        }

        return table[^1];
    }

    private PlayerCharacterBody2D GetPlayer()
    {
        if (_cachedPlayer == null || !GodotObject.IsInstanceValid(_cachedPlayer))
            _cachedPlayer = GetTree().GetFirstNodeInGroup(GroupConstants.Entities.Player) as PlayerCharacterBody2D;
        return _cachedPlayer;
    }

    private static NPCController FindNPCController(Node enemy)
    {
        // NPC_Base structure: root CharacterBody2D has NPCController as a child
        foreach (var child in enemy.GetChildren())
        {
            if (child is NPCController controller)
                return controller;
        }
        // Also check if the node itself is the controller's parent (NPC_Base root)
        if (enemy is NPCEntityCharacterBody2D npcBody)
            return npcBody.Controller;
        return null;
    }
}
