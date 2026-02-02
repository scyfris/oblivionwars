# Oblivion Wars — Game Architecture Document

## Overview

Data-oriented, event-driven architecture for a Metroidvania side-scrolling platformer built in Godot 4 C#. Systems process events and act on entity data. Entities own their data. Events decouple systems from each other.

Scope of initial implementation: **EventBus, HealthSystem, CombatSystem, StatusEffectSystem**, plus the data model foundations (definitions, save data, enums).

---

## 1. EventBus

**File:** `Scripts/Core/EventBus.cs`
**Scene:** Autoload singleton (registered in project.godot)

### Design

```csharp
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    // Processing mode
    public enum EventTiming { Immediate, NextFrame }

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("EventBus: Duplicate instance detected, removing this one.");
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

    // Subscribe/unsubscribe with typed events
    public void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent;
    public void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent;

    // Raise events
    public void Raise<T>(T evt, EventTiming timing = EventTiming.Immediate) where T : struct, IGameEvent;

    // Called in _PhysicsProcess to flush deferred events
    public override void _PhysicsProcess(double delta);
}
```

### Event Interface

```csharp
public interface IGameEvent { }
```

### Initial Event Structs

**File:** `Scripts/Core/Events/CombatEvents.cs`

```csharp
public struct HitEvent : IGameEvent
{
    public ulong TargetInstanceId;    // Godot instance ID of entity hit
    public ulong SourceInstanceId;    // Godot instance ID of attacker/projectile
    public float BaseDamage;
    public Vector2 HitDirection;      // Normalized direction of impact
    public Vector2 HitPosition;       // World position of impact
}

public struct EntityDiedEvent : IGameEvent
{
    public ulong EntityInstanceId;
    public ulong KillerInstanceId;
}

public struct DamageAppliedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public float FinalDamage;         // After all modifiers
    public float RemainingHealth;
}
```

**File:** `Scripts/Core/Events/StatusEvents.cs`

```csharp
public struct StatusEffectAppliedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public string EffectId;
}

public struct StatusEffectRemovedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public string EffectId;
}
```

### Singleton Pattern (used by all global systems)

Every global system (EventBus, CombatSystem, HealthSystem, StatusEffectSystem, SaveManager) follows the same static Instance pattern:

```csharp
public static MySystem Instance { get; private set; }

public override void _Ready()
{
    if (Instance != null)
    {
        GD.PrintErr("MySystem: Duplicate instance detected, removing this one.");
        QueueFree();
        return;
    }
    Instance = this;
    // ... rest of init
}

public override void _ExitTree()
{
    if (Instance == this)
        Instance = null;
}
```

**Rules:**
- No `GetNode` calls to find singletons. Always use `MySystem.Instance`.
- `_Ready()` sets `Instance` and checks for duplicates (logs error + frees duplicate).
- `_ExitTree()` clears `Instance` only if it's still the current one.
- Callers access via `EventBus.Instance.Raise(...)`, `StatusEffectSystem.Instance.HasEffect(...)`, etc.

### EventBus Implementation Notes

- Dictionary of `Type → List<Delegate>` internally
- Immediate events invoke handlers synchronously in subscriber order
- Deferred events stored in `Queue<Action>`, flushed at start of `_PhysicsProcess`
- Systems subscribe in `_Ready()`, unsubscribe in `_ExitTree()`

---

## 2. Game Systems

### Base Class

**File:** `Scripts/Core/GameSystem.cs`

```csharp
public abstract partial class GameSystem : Node
{
    public override void _Ready()
    {
        Initialize();
    }

    /// <summary>
    /// Called after _Ready. Subscribe to events here via EventBus.Instance.
    /// </summary>
    protected abstract void Initialize();
}
```

Systems access the EventBus via `EventBus.Instance` (static singleton) — no GetNode calls.

Systems are **scene singletons** — child nodes of the main level scene (or an autoload Systems node). Processing order is determined by node order in the tree. Each system that needs global access follows the same static Instance pattern as EventBus.

### Systems Node Layout (in MainLevel.tscn)

```
Systems (Node)
├── CombatSystem       (processes hits, calculates damage)
├── HealthSystem       (applies damage, tracks health, emits death)
├── StatusEffectSystem (ticks effects, applies/removes)
└── ... (future systems)
```

---

### CombatSystem

**File:** `Scripts/Systems/CombatSystem.cs`

**Responsibilities:**
- Listens for: `HitEvent`
- Reads: target's `CharacterDefinition` (armor, resistances), attacker's weapon damage, target's active status effects
- Calculates: final damage after modifiers (armor, buffs, status effects)
- Raises: `DamageAppliedEvent`

```csharp
public partial class CombatSystem : GameSystem
{
    public static CombatSystem Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("CombatSystem: Duplicate instance detected, removing this one.");
            QueueFree();
            return;
        }
        Instance = this;
        base._Ready();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    protected override void Initialize()
    {
        EventBus.Instance.Subscribe<HitEvent>(OnHit);
    }

    private void OnHit(HitEvent evt)
    {
        // Get target node from instance ID
        // Read CharacterDefinition for resistances
        // Read active status effects (e.g., "vulnerable" increases damage)
        // Calculate final damage
        // Raise DamageAppliedEvent
    }
}
```

### HealthSystem

**File:** `Scripts/Systems/HealthSystem.cs`

**Responsibilities:**
- Listens for: `DamageAppliedEvent`
- Reads/writes: entity's runtime health data
- Raises: `EntityDiedEvent` when health reaches 0

```csharp
public partial class HealthSystem : GameSystem
{
    public static HealthSystem Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("HealthSystem: Duplicate instance detected, removing this one.");
            QueueFree();
            return;
        }
        Instance = this;
        base._Ready();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    protected override void Initialize()
    {
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        // Get entity's EntityRuntimeData
        // Subtract damage from CurrentHealth
        // Clamp to 0
        // If health <= 0, raise EntityDiedEvent
    }
}
```

### StatusEffectSystem

**File:** `Scripts/Systems/StatusEffectSystem.cs`

**Responsibilities:**
- Ticks active status effects on all entities each frame
- Removes expired effects
- Raises `StatusEffectAppliedEvent` / `StatusEffectRemovedEvent`
- Provides query method: `HasEffect(ulong entityId, string effectId)`

```csharp
public partial class StatusEffectSystem : GameSystem
{
    public static StatusEffectSystem Instance { get; private set; }

    // Registry of all known status effect definitions (loaded at startup)
    private Dictionary<string, StatusEffectDefinition> _registry;

    public override void _Ready()
    {
        if (Instance != null)
        {
            GD.PrintErr("StatusEffectSystem: Duplicate instance detected, removing this one.");
            QueueFree();
            return;
        }
        Instance = this;
        base._Ready();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    protected override void Initialize()
    {
        // Load status effect definitions from Resources/Data/StatusEffects/
    }

    public override void _PhysicsProcess(double delta)
    {
        // Iterate all entities with active status effects
        // Tick durations, apply tick damage if applicable
        // Remove expired effects
    }

    public void ApplyEffect(ulong targetInstanceId, string effectId, float duration = -1f);
    public void RemoveEffect(ulong targetInstanceId, string effectId);
    public bool HasEffect(ulong entityInstanceId, string effectId);
}
```

---

## 3. Entity Data Model

### Layer 1: Definition Data (read-only .tres)

**File:** `Scripts/Data/Definitions/CharacterDefinition.cs`

```csharp
[GlobalClass]
public partial class CharacterDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EntityId = "";  // Unique string ID: "knight_basic", "boss_gravity_king"

    [ExportGroup("Stats")]
    [Export] public float MaxHealth = 100.0f;
    [Export] public float MoveSpeed = 300.0f;
    [Export] public float KnockbackResistance = 0.0f;  // 0 = full knockback, 1 = immune
    [Export] public float Mass = 1.0f;

    [ExportGroup("Persistence")]
    [Export] public PersistenceMode Persistence = PersistenceMode.None;
}

public enum PersistenceMode
{
    None,       // Normal enemies — no save data, reset on checkpoint
    FlagsOnly,  // Bosses — save defeated/phase flags only
    Full        // Player — save everything
}
```

**File:** `Scripts/Data/Definitions/PlayerDefinition.cs`

```csharp
[GlobalClass]
public partial class PlayerDefinition : CharacterDefinition
{
    [ExportGroup("Movement")]
    [Export] public float JumpStrength = 800.0f;
    [Export] public float WallJumpStrength = 700.0f;
    [Export] public float DashSpeed = 600.0f;

    [ExportGroup("Combat")]
    [Export] public float InvincibilityDuration = 1.0f;  // After taking a hit
}
```

**File:** `Scripts/Data/Definitions/EnemyDefinition.cs`

```csharp
[GlobalClass]
public partial class EnemyDefinition : CharacterDefinition
{
    [ExportGroup("Combat")]
    [Export] public float ContactDamage = 10.0f;
    [Export] public float AggroRange = 200.0f;

    [ExportGroup("Flags")]
    [Export] public bool IsBoss = false;

    [ExportGroup("Drops")]
    [Export] public Godot.Collections.Array<Resource> DropTable;
}
```

**File:** `Scripts/Data/Definitions/WeaponDefinition.cs`

```csharp
[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public string WeaponId = "";
    [Export] public float Damage = 10.0f;
    [Export] public float FireRate = 0.2f;       // Cooldown between shots
    [Export] public float Knockback = 100.0f;    // Force applied to target
    [Export] public float ScreenShake = 1.5f;
    [Export] public PackedScene ProjectileScene;  // null for hitscan
    [Export] public float HitscanRange = 1000.0f; // Only for hitscan weapons
}
```

**File:** `Scripts/Data/Definitions/StatusEffectDefinition.cs`

```csharp
[GlobalClass]
public partial class StatusEffectDefinition : Resource
{
    [Export] public string EffectId = "";          // "freeze", "poison", "slow"
    [Export] public string DisplayName = "";
    [Export] public float DefaultDuration = 3.0f;
    [Export] public bool Stackable = false;
    [Export] public int MaxStacks = 1;
    [Export] public float TickInterval = 0.0f;     // 0 = no tick, just duration
    [Export] public float TickDamage = 0.0f;       // Damage per tick (poison)
    [Export] public float SpeedMultiplier = 1.0f;  // 0.5 = slow, 1.0 = normal
    [Export] public float DamageMultiplier = 1.0f; // 1.5 = vulnerable
}
```

### Layer 2: Persistent Save Data (serializable .tres)

**File:** `Scripts/Data/SaveData/PlayerSaveData.cs`

```csharp
[GlobalClass]
public partial class PlayerSaveData : Resource
{
    [ExportGroup("Health")]
    [Export] public float Sav_CurrentHealth = 100.0f;

    [ExportGroup("Position")]
    [Export] public string Sav_CheckpointId = "";
    [Export] public Vector2 Sav_CheckpointPosition = Vector2.Zero;

    [ExportGroup("Progression")]
    [Export] public Godot.Collections.Array<string> Sav_DefeatedBossIds = new();
    [Export] public Godot.Collections.Array<string> Sav_UnlockedAbilities = new();

    [ExportGroup("Inventory")]
    [Export] public Godot.Collections.Array<string> Sav_HeldWeaponIds = new();
    [Export] public string Sav_ActiveWeaponId = "";
}
```

**File:** `Scripts/Data/SaveData/GlobalSaveData.cs`

```csharp
[GlobalClass]
public partial class GlobalSaveData : Resource
{
    [ExportGroup("World State")]
    [Export] public Godot.Collections.Array<string> Sav_DefeatedEntityIds = new();
    [Export] public Godot.Collections.Dictionary<string, int> Sav_EntityStateFlags = new();
}
```

### Layer 3: Runtime Data (C# class, on entity, not saved)

**File:** `Scripts/Data/Runtime/EntityRuntimeData.cs`

```csharp
public class EntityRuntimeData
{
    // Identity
    public string EntityId;
    public ulong RuntimeInstanceId;  // Unique per-spawn, assigned at runtime

    // Health (initialized from definition, modified by systems)
    public float CurrentHealth;
    public float MaxHealth;

    // Active status effects (managed by StatusEffectSystem)
    public List<ActiveStatusEffect> StatusEffects = new();

    // Reference to definition
    public CharacterDefinition Definition;
}

public class ActiveStatusEffect
{
    public string EffectId;
    public float RemainingDuration;
    public float TickTimer;
    public int CurrentStacks;
    public StatusEffectDefinition Definition;
}
```

### Entity Interface

**File:** `Scripts/Core/IGameEntity.cs`

Entities (player, enemies) implement this so systems can query them uniformly:

```csharp
public interface IGameEntity
{
    EntityRuntimeData RuntimeData { get; }
    CharacterDefinition Definition { get; }
    Node2D EntityNode { get; }  // The actual Godot node
}
```

---

## 4. Enums & Registries

### Tile Enums

**File:** `Scripts/Data/Enums/TileEnums.cs`

```csharp
public enum TileSurfaceType
{
    Normal,
    Slippery,
    Sticky,
    Bouncy
}

public enum TileHazardType
{
    None,
    Spikes,
    Lava,
    Acid
}
```

These map to TileSet custom data layers. Validation at level load checks that tile data only uses values from these enums.

### Status Effect Registry

**File:** `Resources/Data/StatusEffects/` (directory of .tres files)

Each status effect is a `StatusEffectDefinition.tres`. The `StatusEffectSystem` loads all `.tres` files from this directory at startup and validates that no duplicate IDs exist.

### Entity Definition Registry

**File:** `Resources/Data/Characters/` (directory)

```
Resources/Data/Characters/
├── Player/
│   └── player.tres           (PlayerDefinition)
├── Enemies/
│   ├── knight_basic.tres     (EnemyDefinition)
│   ├── slime_green.tres      (EnemyDefinition)
│   └── boss_gravity_king.tres (EnemyDefinition)
└── ...
```

---

## 5. Integration: How Existing Code Connects

### Projectile → EventBus

Currently `StandardBullet.OnHit()` logs a message. After this architecture:

```csharp
// In Projectile.cs OnHit():
EventBus.Instance.Raise(new HitEvent
{
    TargetInstanceId = body.GetInstanceId(),
    SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
    BaseDamage = _damage,
    HitDirection = _direction,
    HitPosition = GlobalPosition
});
```

### Player Entity

`PlayerCharacterBody2D` implements `IGameEntity`. It holds:
- `[Export] PlayerDefinition _definition;` (read-only template)
- `EntityRuntimeData _runtimeData;` (created in `_Ready()` from definition)

The player's `_Ready()` initializes runtime data:
```csharp
_runtimeData = new EntityRuntimeData
{
    EntityId = _definition.EntityId,
    RuntimeInstanceId = GetInstanceId(),
    CurrentHealth = _definition.MaxHealth,
    MaxHealth = _definition.MaxHealth,
    Definition = _definition
};
```

### Knockback Flow Example

1. `HitEvent` raised by projectile
2. `CombatSystem` handles it: calculates final damage, reads knockback from weapon definition, reads target's knockback resistance from CharacterDefinition, calculates final knockback force
3. `CombatSystem` raises `DamageAppliedEvent` AND calls `target.ApplyKnockback(direction, force, stunDuration)` directly on the physics body
4. `HealthSystem` handles `DamageAppliedEvent`: subtracts health
5. Physics body executes knockback mechanically (it doesn't know why)

---

## 6. Save/Load

**File:** `Scripts/Core/SaveManager.cs` (autoload singleton, static Instance pattern)

### Save Flow
1. Collect `PlayerSaveData` from player entity
2. Collect `GlobalSaveData` (defeated bosses, world flags)
3. `ResourceSaver.Save(playerData, "user://save_player.tres")`
4. `ResourceSaver.Save(globalData, "user://save_global.tres")`

### Load Flow
1. `ResourceLoader.Load<PlayerSaveData>("user://save_player.tres")`
2. `ResourceLoader.Load<GlobalSaveData>("user://save_global.tres")`
3. Apply player data to player entity
4. Apply global data (check defeated boss list before spawning)
5. All normal enemies spawn fresh from their definitions

### Checkpoint
Same as save. Normal enemies in the area reset. Boss defeated flags persist.

---

## 7. File Structure

```
Scripts/
├── Core/
│   ├── EventBus.cs
│   ├── GameSystem.cs
│   ├── IGameEntity.cs
│   ├── SaveManager.cs
│   └── Events/
│       ├── CombatEvents.cs
│       └── StatusEvents.cs
├── Systems/
│   ├── CombatSystem.cs
│   ├── HealthSystem.cs
│   └── StatusEffectSystem.cs
├── Data/
│   ├── Definitions/
│   │   ├── CharacterDefinition.cs
│   │   ├── PlayerDefinition.cs
│   │   ├── EnemyDefinition.cs
│   │   ├── WeaponDefinition.cs
│   │   └── StatusEffectDefinition.cs
│   ├── SaveData/
│   │   ├── PlayerSaveData.cs
│   │   └── GlobalSaveData.cs
│   ├── Runtime/
│   │   └── EntityRuntimeData.cs
│   └── Enums/
│       └── TileEnums.cs
├── Player/           (existing)
├── Camera/           (existing)
├── Combat/           (existing, modified)
└── Utils/            (existing)

Resources/
├── Data/
│   ├── Characters/
│   │   ├── Player/
│   │   │   └── player.tres
│   │   └── Enemies/
│   │       └── (enemy .tres files)
│   ├── Weapons/
│   │   └── (weapon .tres files)
│   └── StatusEffects/
│       └── (status effect .tres files)
└── Tilemaps/         (existing)
```

---

## 8. Implementation Order

### Phase 1: Foundations
1. Create `Scripts/Core/EventBus.cs` — event bus with immediate/deferred modes
2. Register EventBus as autoload in project.godot
3. Create `Scripts/Core/GameSystem.cs` — base class
4. Create `Scripts/Core/IGameEntity.cs` — entity interface
5. Create `Scripts/Core/Events/CombatEvents.cs` — event structs

### Phase 2: Data Model
6. Create `Scripts/Data/Definitions/CharacterDefinition.cs`
7. Create `Scripts/Data/Definitions/PlayerDefinition.cs`
8. Create `Scripts/Data/Definitions/EnemyDefinition.cs`
9. Create `Scripts/Data/Definitions/WeaponDefinition.cs`
10. Create `Scripts/Data/Definitions/StatusEffectDefinition.cs`
11. Create `Scripts/Data/Runtime/EntityRuntimeData.cs`
12. Create `Scripts/Data/Enums/TileEnums.cs`

### Phase 3: Systems
13. Create `Scripts/Systems/CombatSystem.cs`
14. Create `Scripts/Systems/HealthSystem.cs`
15. Create `Scripts/Systems/StatusEffectSystem.cs`

### Phase 4: Integration
16. Add `IGameEntity` to `PlayerCharacterBody2D.cs`
17. Modify `Projectile.cs` to raise `HitEvent` via EventBus
18. Add Systems node to `MainLevel.tscn`
19. Create initial `.tres` definition files for player and test weapon

### Phase 5: Save System (deferred)
20. Create `Scripts/Data/SaveData/PlayerSaveData.cs`
21. Create `Scripts/Data/SaveData/GlobalSaveData.cs`
22. Create `Scripts/Core/SaveManager.cs`

---

## 9. Verification

1. **EventBus**: Raise a test event, verify subscriber receives it. Test deferred mode processes next frame.
2. **CombatSystem**: Fire projectile at player, verify `HitEvent` → `DamageAppliedEvent` chain fires correctly.
3. **HealthSystem**: Verify player health decreases when `DamageAppliedEvent` is received. Verify `EntityDiedEvent` fires at 0 health.
4. **StatusEffectSystem**: Apply "poison" effect, verify it ticks damage and expires after duration.
5. **Data Model**: Create a `PlayerDefinition.tres` in editor, verify all exports appear with correct grouping. Verify runtime data initializes from definition.
6. **Integration**: Full flow — shoot projectile → hits entity → combat calculates damage → health decreases → death event fires.

---

## 10. Stats Migration — Move [Export] Stats to Definition Resources

### Goal

Remove hardcoded stat [Export] fields from scripts. Scripts read from definition .tres resources instead. To test different values, duplicate the .tres in the editor and swap it.

### PlayerDefinition — Add Missing Fields

Add to `Scripts/Data/Definitions/PlayerDefinition.cs`:

```csharp
[ExportGroup("Physics")]
[Export] public float Gravity = 2000.0f;
[Export] public float WallSlideSpeedFraction = 0.5f;
[Export] public float WallJumpPushAwayForce = 500.0f;
[Export] public float WallJumpPushAwayDuration = 0.2f;
[Export] public float WallJumpInputLockDuration = 0.2f;
```

### PlayerCharacterBody2D.cs Changes

1. Add `[Export] private PlayerDefinition _definition;`
2. Remove these [Export] fields (replace reads with `_definition.X`):
   - `_speed` → `_definition.MoveSpeed`
   - `_gravity` → `_definition.Gravity`
   - `_jumpStrength` → `_definition.JumpStrength`
   - `_wallJumpStrength` → `_definition.WallJumpStrength`
   - `_wallSlideSpeedFraction` → `_definition.WallSlideSpeedFraction`
   - `_wallJumpPushAwayForce` → `_definition.WallJumpPushAwayForce`
   - `_wallJumpPushAwayDuration` → `_definition.WallJumpPushAwayDuration`
   - `_wallJumpInputLockDuration` → `_definition.WallJumpInputLockDuration`
3. Keep these as [Export] on the script (scene/visual concerns, not stats):
   - `_holdableSystem` (node reference)
   - `_wallSlideDustPosition` (node reference)
   - `_wallSlideDustScene` (scene reference)
   - `_gravityFlipRotationSpeed` (visual parameter)
   - `_bodyFlipDelay` (visual parameter)
   - `_maintainMomentumOnFlip` (physics behavior toggle)
   - `_moveDirection` (runtime input state)

### WeaponDefinition — Add Missing Fields

Add to `Scripts/Data/Definitions/WeaponDefinition.cs`:

```csharp
[Export] public float UseCooldown = 0.2f;           // was Holdable._useCooldown
[Export] public Vector2 ProjectileSpawnOffset = new Vector2(20, 0);  // was ProjectileWeapon._projectileSpawnOffset
[Export] public float TrailDuration = 0.1f;          // was HitscanWeapon._trailDuration
```

### Weapon Script Changes

**Holdable.cs:**
- Remove `[Export] _useCooldown`
- Add `protected WeaponDefinition _definition;`
- Add `public void SetDefinition(WeaponDefinition def) { _definition = def; _useCooldown = def.UseCooldown; }` or read `_definition.UseCooldown` directly in `CanUse()`
- Actually simpler: keep `_useCooldown` as a private field (not exported), set it from definition in Initialize

**Weapon.cs:**
- Add `[Export] private WeaponDefinition _definition;`
- Remove `[Export] _damage`
- Read `_definition.Damage` where `_damage` was used
- Pass definition down: override `Initialize` to set base class fields from definition

**ProjectileWeapon.cs:**
- Remove `[Export] _projectileScene` → `_definition.ProjectileScene`
- Remove `[Export] _projectileSpawnOffset` → `_definition.ProjectileSpawnOffset`
- Remove `[Export] _screenShakeStrength` → `_definition.ScreenShake`

**HitscanWeapon.cs:**
- Remove `[Export] _range` → `_definition.HitscanRange`
- Remove `[Export] _screenShakeStrength` → `_definition.ScreenShake`
- Remove `[Export] _trailDuration` → `_definition.TrailDuration`

**Projectile.cs:**
- Keep `_damage` as private (not exported), set via `Initialize(direction, damage, shooter)` — already works this way
- Keep `_speed` and `_lifetime` as [Export] on the projectile (projectile scene properties, not weapon stats)

### Files Modified

1. `Scripts/Data/Definitions/PlayerDefinition.cs` — add Physics export group
2. `Scripts/Data/Definitions/WeaponDefinition.cs` — add UseCooldown, ProjectileSpawnOffset, TrailDuration
3. `Scripts/Player/PlayerCharacterBody2D.cs` — add _definition, remove stat exports
4. `Scripts/Combat/Holdables/Holdable.cs` — remove _useCooldown export, read from definition
5. `Scripts/Combat/Weapons/Weapon.cs` — add [Export] WeaponDefinition, remove _damage export
6. `Scripts/Combat/Weapons/ProjectileWeapon.cs` — remove stat exports, read from definition
7. `Scripts/Combat/Weapons/HitscanWeapon.cs` — remove stat exports, read from definition

### Verification

1. Build succeeds with zero errors
2. Open PlayerCharacterBody2D.tscn — _definition export should appear, no more inline stat fields
3. Open weapon nodes in HoldableSystem — _definition export should appear
4. Create a `player.tres` PlayerDefinition in editor with current default values
5. Assign it in inspector, run game, verify movement/jumping/wall slide all work identically

---

## 11. Holdable / Weapon System (Revised Design)

### Overview

Data-driven holdable system. No per-weapon subclasses (no ShotgunWeapon, PistolWeapon, etc.). Weapon behavior is driven entirely by `WeaponDefinition` data. The class hierarchy has only two levels of holdable subclasses: `Weapon` and `Item`.

### Architecture

```
HoldableSystem (Node, on player/NPC)
├── Manages left + right holdable slots
├── Instantiates holdable scene from definition's PackedScene
├── Positions holdable relative to owner
├── Routes Use() from input (left click → left slot, right click → right slot)
│
Holdable (abstract base, Node2D, root of holdable scenes)
├── Weapon : Holdable
│   ├── [Export] WeaponDefinition (assigned in the weapon .tscn scene)
│   ├── Use() reads definition data to determine behavior:
│   │   ├── ProjectileScene != null → projectile weapon
│   │   │   ├── SpreadCount > 1 → spawn multiple in spread pattern
│   │   │   └── else → spawn single projectile
│   │   └── ProjectileScene == null → hitscan raycast using HitscanRange
│   ├── Calls AnimationPlayer.Play("shoot") from Use()
│   └── Single class handles all weapon types via data
│
└── Item : Holdable
    ├── [Export] ItemDefinition (future)
    ├── Use() reads definition data to determine behavior
    │   ├── Potion → heal
    │   ├── Gravity cube → flip gravity
    │   └── etc. — behavior determined by data, not subclasses
    └── Gravity cube, consumables, utility items all use this class

```

### Holdable Base Class

```csharp
public abstract partial class Holdable : Node2D
{
    protected Node2D _owner;
    protected float _useCooldown;
    protected float _timeSinceLastUse = 999f;

    public virtual void InitOwner(Node2D owner) { _owner = owner; }
    public bool CanUse() => _timeSinceLastUse >= _useCooldown;
    protected void ResetCooldown() { _timeSinceLastUse = 0f; }

    public abstract void Use(Vector2 targetPosition);
    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
    public virtual void Update(double delta) { _timeSinceLastUse += (float)delta; }
}
```

### Weapon Class (single class, all weapon types)

```csharp
public partial class Weapon : Holdable
{
    [Export] private WeaponDefinition _weaponDefinition;

    public override void _Ready()
    {
        if (_weaponDefinition != null)
            _useCooldown = _weaponDefinition.UseCooldown;
    }

    public override void Use(Vector2 targetPosition)
    {
        if (!CanUse()) return;

        if (_weaponDefinition.ProjectileScene != null)
            FireProjectile(targetPosition);
        else
            FireHitscan(targetPosition);

        ResetCooldown();

        // Trigger animation — AnimationPlayer reacts to this
        GetNodeOrNull<AnimationPlayer>("AnimationPlayer")?.Play("shoot");
    }

    private void FireProjectile(Vector2 targetPosition) { /* spawn based on definition data */ }
    private void FireHitscan(Vector2 targetPosition) { /* raycast based on definition data */ }
}
```

### WeaponDefinition (updated)

Fields to add for data-driven spread/multi-shot:

```csharp
[ExportGroup("Spread")]
[Export] public int SpreadCount = 1;          // 1 = single shot, >1 = shotgun spread
[Export] public float SpreadAngle = 15.0f;    // Total spread arc in degrees
```

### HoldableSystem (updated)

```csharp
public partial class HoldableSystem : Node
{
    [Export] private PackedScene _leftHoldableScene;
    [Export] private PackedScene _rightHoldableScene;

    private Holdable _leftHoldable;
    private Holdable _rightHoldable;
    private Node2D _owner;

    public void Initialize(Node2D owner) { _owner = owner; /* instantiate scenes */ }
    public void UseLeft(Vector2 target) { _leftHoldable?.Use(target); }
    public void UseRight(Vector2 target) { _rightHoldable?.Use(target); }

    public void SwapLeft(PackedScene newScene) { /* free old, instantiate new, call OnEquip */ }
    public void SwapRight(PackedScene newScene) { /* free old, instantiate new, call OnEquip */ }
}
```

### Weapon Scene Structure (e.g. shotgun.tscn)

```
Weapon (Node2D, script: Weapon.cs)
├── [Export] WeaponDefinition = shotgun.tres  ← assigned in editor
├── Sprite2D (weapon art)
├── AnimationPlayer
│   ├── "shoot" → sprite recoil, muzzle flash, etc.
│   ├── "equip" → draw animation
│   └── "idle"
├── MuzzleFlash (Node2D position marker)
└── ProjectileSpawn (Node2D position marker)
```

The scene is self-contained and testable in the editor. Drag shotgun.tres onto the export, preview animations, adjust sprite positions — all without running the game.

### Animation Flow

1. Player clicks → HoldableSystem.UseLeft(mousePos)
2. → Weapon.Use(mousePos) → spawns projectiles/raycast based on definition data
3. → Weapon calls AnimationPlayer.Play("shoot")
4. → AnimationPlayer plays recoil, muzzle flash, sound effects
5. Animations react to gameplay events, not the other way around

### Implementation Plan

#### Step 1: Update WeaponDefinition

**File:** `Scripts/Data/Definitions/WeaponDefinition.cs`

Add spread/multi-shot fields:
```csharp
[ExportGroup("Spread")]
[Export] public int SpreadCount = 1;          // 1 = single, >1 = shotgun
[Export] public float SpreadAngle = 15.0f;    // Total arc in degrees
```

#### Step 2: Rewrite Holdable base class

**File:** `Scripts/Combat/Holdables/Holdable.cs`

- Change base from `Node` → `Node2D` (holdables are positioned in world space)
- Rename `Initialize(Node2D owner)` → `InitOwner(Node2D owner)`
- Add `virtual OnEquip()` / `OnUnequip()` stubs
- Keep `Use()`, `CanUse()`, `ResetCooldown()`, `Update()` as-is

#### Step 3: Collapse Weapon into single class

**File:** `Scripts/Combat/Weapons/Weapon.cs`

- Remove `abstract` — Weapon is now concrete
- Unseal `Use()` — was `sealed override`, now just `override`
- Move projectile logic from ProjectileWeapon inline: `FireProjectile()`
- Move hitscan logic from HitscanWeapon inline: `FireHitscan()`
- `Use()` checks `_weaponDefinition.ProjectileScene != null` to pick path
- Support spread: if `SpreadCount > 1`, spawn multiple projectiles in arc
- Bullet trail handled internally (Line2D child named "BulletTrail")
- Camera shake via `CameraController.Instance` (or tree lookup)

#### Step 4: Delete old weapon subclasses

- Delete `Scripts/Combat/Weapons/ProjectileWeapon.cs`
- Delete `Scripts/Combat/Weapons/HitscanWeapon.cs`

#### Step 5: Rewrite HoldableSystem

**File:** `Scripts/Combat/Holdables/HoldableSystem.cs`

- Two slots: `_leftHoldable` and `_rightHoldable`
- Two exported PackedScene fields: `_leftHoldableScene`, `_rightHoldableScene`
- `Initialize(Node2D owner)` — instantiate scenes, add as children, call `InitOwner()`
- `UseLeft(Vector2 target)` / `UseRight(Vector2 target)`
- `Update(double delta)` — ticks both slots
- `SwapLeft(PackedScene scene)` / `SwapRight(PackedScene scene)` — free old, instantiate new
- Remove old array-cycling logic (NextHoldable, SwitchHoldable)

#### Step 6: Update CharacterController for left/right click

**File:** `Scripts/Player/CharacterController.cs`

- `_useAction = "shoot"` stays (left mouse → left holdable)
- Add `_useRightAction = "shoot_right"` (right mouse → right holdable)
- Remove `_switchHoldableAction` (no more cycling)
- Left click → `_playerCharacter.UseHoldableLeft(targetPos)`
- Right click → `_playerCharacter.UseHoldableRight(targetPos)`

#### Step 7: Add shoot_right input action

**File:** `project.godot`

Add `shoot_right` input action mapped to right mouse button (button_index 2).

#### Step 8: Update PlayerCharacterBody2D

**File:** `Scripts/Player/PlayerCharacterBody2D.cs`

- Replace `UseHoldable(Vector2)` with `UseHoldableLeft(Vector2)` and `UseHoldableRight(Vector2)`
- Remove `NextHoldable()`
- Route to `_holdableSystem.UseLeft()` / `_holdableSystem.UseRight()`

#### Step 9: Create placeholder weapon .tres definitions

**Files:**
- `Resources/Data/Weapons/pistol.tres` — single projectile weapon (SpreadCount=1)
- `Resources/Data/Weapons/shotgun.tres` — spread projectile weapon (SpreadCount=5, SpreadAngle=30)

These are WeaponDefinition resources with placeholder values. Created via code using `ResourceSaver`.

#### Step 10: Create placeholder weapon .tscn scenes

**Files:**
- `Scenes/Weapons/Pistol.tscn` — Weapon node with ColorRect child (blue rectangle)
- `Scenes/Weapons/Shotgun.tscn` — Weapon node with ColorRect child (red rectangle)

Each scene:
```
Weapon (Node2D, script: Weapon.cs)
├── [Export] WeaponDefinition = pistol.tres (assigned in .tscn)
├── ColorRect (placeholder sprite — colored rectangle)
└── ProjectileSpawn (Node2D position marker)
```

For hitscan weapons, add a Line2D "BulletTrail" child.

#### Step 11: Update PlayerCharacterBody2D.tscn

- Remove old weapon child nodes from HoldableSystem
- Set HoldableSystem's `_leftHoldableScene` = Pistol.tscn
- Set HoldableSystem's `_rightHoldableScene` = Shotgun.tscn (or leave null)

#### Step 12: Build and verify

1. `dotnet build` succeeds with zero errors
2. Run game — left click fires pistol projectile, right click fires shotgun spread
3. Bullet trails display for hitscan if tested
4. Screen shake works on fire
5. Projectiles ignore shooter collision

### Files Summary

**Modified:**
- `Scripts/Data/Definitions/WeaponDefinition.cs`
- `Scripts/Combat/Holdables/Holdable.cs`
- `Scripts/Combat/Weapons/Weapon.cs`
- `Scripts/Combat/Holdables/HoldableSystem.cs`
- `Scripts/Player/CharacterController.cs`
- `Scripts/Player/PlayerCharacterBody2D.cs`
- `project.godot` (add shoot_right input)
- `Scenes/Characters/PlayerCharacterBody2D.tscn` (update HoldableSystem)

**Deleted:**
- `Scripts/Combat/Weapons/ProjectileWeapon.cs`
- `Scripts/Combat/Weapons/HitscanWeapon.cs`

**Created:**
- `Resources/Data/Weapons/pistol.tres`
- `Resources/Data/Weapons/shotgun.tres`
- `Scenes/Weapons/Pistol.tscn`
- `Scenes/Weapons/Shotgun.tscn`
