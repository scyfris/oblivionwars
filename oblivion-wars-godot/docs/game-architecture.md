# Oblivion Wars — Game Architecture Document

## Overview

Data-oriented, event-driven architecture for a Metroidvania side-scrolling platformer built in Godot 4 C#. Systems process events and act on entity data. Entities own their data. Events decouple systems from each other.

### Core Principles

- **Events are for cross-system communication.** Use events when multiple independent systems need to react, or when the source shouldn't know about the consumers. Don't use events for internal mechanics within a single entity.
- **Systems own game-wide state logic** (damage calculation, health tracking, status effects). They listen to events and modify runtime data.
- **Nodes own their own mechanics** (movement, physics, animation, particles, input). They raise events when something gameplay-relevant happens. They can also listen to events for visual responses (flash sprite on damage).
- **No GetNode for singletons.** All global systems use static `Instance` pattern.
- **No premature abstractions.** Base classes over interfaces until we need the flexibility.

### Terminology

| Term | What it is | Examples |
|------|-----------|----------|
| **Entity** | Any game character with health, stats, and movement. Derives from `EntityCharacterBody2D`. | Player, enemies, NPCs |
| **System** | Autoload singleton that processes events and modifies entity data. Derives from `GameSystem`. | CombatSystem, HealthSystem, HazardSystem |
| **Definition** | Read-only `.tres` resource defining template stats. Derives from `Resource`. | PlayerDefinition, EnemyDefinition, ProjectileDefinition |
| **Interactable** | World object that responds to HitEvents but isn't a character. No base class yet — just nodes that subscribe to events. | Doors, levers, breakable walls |

---

## 1. EventBus

**File:** `Scripts/Core/EventBus.cs` — Autoload singleton

### Design

```csharp
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }
    public enum EventTiming { Immediate, NextFrame }

    public void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent;
    public void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent;
    public void Raise<T>(T evt, EventTiming timing = EventTiming.Immediate) where T : struct, IGameEvent;
}
```

### Event Interface

```csharp
public interface IGameEvent { }
```

### Implementation Notes

- Dictionary of `Type → List<Delegate>` internally
- Immediate events invoke handlers synchronously
- Deferred events stored in `Queue<Action>`, flushed at start of `_PhysicsProcess`
- Systems subscribe in `_Ready()`, unsubscribe in `_ExitTree()`

---

## 2. Events

### CombatEvents.cs (`Scripts/Core/Events/CombatEvents.cs`)

```csharp
// Unified hit event — used for ALL hits (projectiles, hazards, melee, etc.)
// Multiple systems filter by what they care about:
//   CombatSystem: "Is target an EntityCharacterBody2D? Calculate damage."
//   DoorSystem: "Is target a door? Open it."
//   AudioSystem: "Play impact sound based on ProjectileDefinition."
public struct HitEvent : IGameEvent
{
    public ulong TargetInstanceId;          // What got hit (entity, door, anything)
    public ulong SourceInstanceId;          // What did the hitting (projectile, entity, 0 for environment)
    public float BaseDamage;                // Raw damage before modifiers
    public Vector2 HitDirection;            // Normalized direction of impact
    public Vector2 HitPosition;             // World position of impact
    public ProjectileDefinition Projectile; // null if not a projectile hit (e.g. hazard)
}

// Raised by CombatSystem after damage calculation
public struct DamageAppliedEvent : IGameEvent
{
    public ulong TargetInstanceId;
    public float FinalDamage;               // After all modifiers
}

// Raised by HealthSystem when health reaches 0
public struct EntityDiedEvent : IGameEvent
{
    public ulong EntityInstanceId;
    public ulong KillerInstanceId;
}
```

### EnvironmentEvents.cs (`Scripts/Core/Events/EnvironmentEvents.cs`)

```csharp
// Raised by EntityCharacterBody2D when it collides with a hazard tile
// HazardSystem listens and raises HitEvent with appropriate damage
public struct HazardContactEvent : IGameEvent
{
    public ulong EntityInstanceId;          // Who touched the hazard
    public TileHazardType HazardType;       // What type of hazard
    public Vector2 Position;                // Where it happened
}
```

### StatusEvents.cs (`Scripts/Core/Events/StatusEvents.cs`)

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

---

## 3. Entity Data Model

### Layer 1: Definition Data (read-only .tres)

**CharacterDefinition.cs** — Base stats for all characters

```csharp
[GlobalClass]
public partial class CharacterDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EntityId = "";

    [ExportGroup("Stats")]
    [Export] public float MaxHealth = 100.0f;
    [Export] public float MoveSpeed = 300.0f;
    [Export] public float KnockbackResistance = 0.0f;
    [Export] public float Mass = 1.0f;

    [ExportGroup("Movement")]
    [Export] public float JumpStrength = 800.0f;
    [Export] public float WallJumpStrength = 700.0f;

    [ExportGroup("Physics")]
    [Export] public float Gravity = 2000.0f;
    [Export] public float WallSlideSpeedFraction = 0.5f;
    [Export] public float WallJumpPushAwayForce = 500.0f;
    [Export] public float WallJumpPushAwayDuration = 0.2f;
    [Export] public float WallJumpInputLockDuration = 0.2f;

    [ExportGroup("Persistence")]
    [Export] public PersistenceMode Persistence = PersistenceMode.None;
}
```

**PlayerDefinition.cs** — Player-specific stats (extends CharacterDefinition)

```csharp
[GlobalClass]
public partial class PlayerDefinition : CharacterDefinition
{
    [ExportGroup("Movement")]
    [Export] public float DashSpeed = 600.0f;

    [ExportGroup("Combat")]
    [Export] public float InvincibilityDuration = 1.0f;
}
```

**EnemyDefinition.cs** — Enemy-specific stats

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

**ProjectileDefinition.cs** — Defines projectile behavior

```csharp
[GlobalClass]
public partial class ProjectileDefinition : Resource
{
    [Export] public string ProjectileId = "";

    [ExportGroup("Behavior")]
    [Export] public float Speed = 800.0f;          // 0 = instant hit (raycast internally)
    [Export] public float Lifetime = 3.0f;         // ignored if Speed == 0
    [Export] public float Damage = 10.0f;          // base projectile damage
    [Export] public bool BounceOffWalls = false;
    [Export] public int MaxBounces = 0;
    [Export] public bool AffectedByGravity = false;
    [Export] public float GravityScale = 1.0f;

    [ExportGroup("Raycast")]
    [Export] public float HitscanRange = 1000.0f;  // max range when Speed == 0
    [Export] public float TrailDuration = 0.1f;    // visual trail duration for instant-hit

    [ExportGroup("Explosion")]
    [Export] public float ExplosionRadius = 0.0f;  // 0 = no explosion
    [Export] public float FuseTime = 0.0f;         // 0 = on contact, >0 = timed

    [ExportGroup("Visuals")]
    [Export] public PackedScene ProjectileScene;    // the .tscn for this projectile's visuals
}
```

**WeaponDefinition.cs** — Defines weapon behavior

Weapons reference a ProjectileDefinition. Damage is calculated as `ProjectileDefinition.Damage * WeaponDefinition.DamageScale`, so the same projectile can deal different damage from different weapons. Screen shake values are scale factors applied to the camera's base shake strength and duration.

```csharp
[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public string WeaponId = "";
    [Export] public float UseCooldown = 0.2f;
    [Export] public bool IsAutomatic = true;            // hold to repeat fire vs. click per shot
    [Export] public float DamageScale = 1.0f;           // multiplier on projectile's base damage
    [Export] public float Knockback = 100.0f;
    [Export] public float ScreenShakeScale = 1.0f;      // multiplier on camera's base shake strength
    [Export] public float ScreenShakeDurationScale = 1.0f; // multiplier on camera's base shake duration

    [ExportGroup("Projectile")]
    [Export] public ProjectileDefinition Projectile;     // reference to projectile definition

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;                // 1 = single, >1 = shotgun
    [Export] public float SpreadAngle = 15.0f;          // total arc in degrees
}
```

**HazardDefinition.cs** — Tile hazard damage values

```csharp
[GlobalClass]
public partial class HazardDefinition : Resource
{
    [Export] public float SpikeDamage = 10.0f;
    [Export] public float LavaDamage = 20.0f;
    [Export] public float AcidDamage = 15.0f;

    public float GetDamage(TileHazardType type) => type switch
    {
        TileHazardType.Spikes => SpikeDamage,
        TileHazardType.Lava => LavaDamage,
        TileHazardType.Acid => AcidDamage,
        _ => 0f
    };
}
```

**StatusEffectDefinition.cs** — Status effect data

```csharp
[GlobalClass]
public partial class StatusEffectDefinition : Resource
{
    [Export] public string EffectId = "";
    [Export] public string DisplayName = "";
    [Export] public float DefaultDuration = 3.0f;
    [Export] public bool Stackable = false;
    [Export] public int MaxStacks = 1;
    [Export] public float TickInterval = 0.0f;
    [Export] public float TickDamage = 0.0f;
    [Export] public float SpeedMultiplier = 1.0f;
    [Export] public float DamageMultiplier = 1.0f;
}
```

### Layer 2: Runtime Data (C# class, not saved)

**EntityRuntimeData.cs**

```csharp
public class EntityRuntimeData
{
    public string EntityId;
    public ulong RuntimeInstanceId;
    public float CurrentHealth;
    public float MaxHealth;
    public List<ActiveStatusEffect> StatusEffects = new();
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

### Enums

**TileEnums.cs**

```csharp
public enum TileSurfaceType { Normal, Slippery, Sticky, Bouncy }
public enum TileHazardType { None, Spikes, Lava, Acid }
```

**PersistenceMode** (in CharacterDefinition.cs)

```csharp
public enum PersistenceMode { None, FlagsOnly, Full }
```

---

## 4. EntityCharacterBody2D — Base Character Class

**File:** `Scripts/Entity/EntityCharacterBody2D.cs`

Base class for all characters (player, enemies, NPCs). Systems check `is EntityCharacterBody2D` to find damageable entities.

### Responsibilities

- Owns `EntityRuntimeData` (initialized from definition in `_Ready`)
- Owns `CharacterDefinition` reference
- Movement: `MoveLeft()`, `MoveRight()`, `Stop()`, `Jump()`, `CancelJump()`
- Physics: gravity, velocity calculation, `MoveAndSlide()`
- Wall sliding and wall jumping
- Gravity rotation (arbitrary gravity directions)
- Hazard tile detection (raises `HazardContactEvent`, does NOT handle damage)

### Public API

```csharp
public partial class EntityCharacterBody2D : CharacterBody2D
{
    [Export] protected CharacterDefinition _definition;

    public EntityRuntimeData RuntimeData => _runtimeData;
    public CharacterDefinition Definition => _definition;

    // Movement commands (called by controllers or AI)
    public void MoveLeft();
    public void MoveRight();
    public void Stop();
    public virtual void Jump();
    public void CancelJump();

    // Gravity
    public void RotateGravityClockwise();
    public void RotateGravityCounterClockwise();
    public int GetGravityRotation();

    // State queries
    public bool IsWallSliding { get; }
}
```

### PlayerCharacterBody2D

**File:** `Scripts/Player/PlayerCharacterBody2D.cs`

Player-specific behavior. Extends EntityCharacterBody2D.

#### FlipRoot Pattern

All visual/positional children (sprite, dust, weapon position) live under a `FlipRoot` Node2D. The character faces left or right by setting `FlipRoot.Scale.X = ±1` based on the aim target (mouse position), not movement direction. This ensures the sprite, weapons, particles, and any future child nodes all flip together automatically.

```
PlayerCharacterBody2D (CharacterBody2D)
  ├─ CollisionShape2D           ← stays at root (physics shouldn't flip)
  ├─ FlipRoot (Node2D)          ← Scale.X = ±1 based on mouse position
  │   ├─ AnimatedSprite2D       ← walk/idle animations from spritesheet
  │   ├─ WallSlideDustPosition  ← dust particle spawn point
  │   └─ WeaponPosition         ← weapons are instantiated here
  ├─ HoldableSystem (Node)      ← manages holdable slots, references WeaponPosition
  └─ AnimationPlayer
```

#### Animation

Uses `AnimatedSprite2D` with `SpriteFrames` from a spritesheet (atlas textures). Animation names are exported properties (`_idleAnimation`, `_walkAnimation`) for configurability. Walk animation plays when moving on the floor; idle plays otherwise. The `AnimationPlayer` node is available for future use with `AnimationTree` state machines.

```csharp
public partial class PlayerCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new PlayerDefinition _definition;
    [Export] private HoldableSystem _holdableSystem;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;
    [Export] private AnimatedSprite2D _spriteNode;
    [Export] private string _idleAnimation = "default";
    [Export] private string _walkAnimation = "walk";

    [ExportGroup("Wall Slide Effects")]
    [Export] private Node2D _wallSlideDustPosition;
    [Export] private PackedScene _wallSlideDustScene;

    private Vector2 _aimTarget;

    // Aim — stores target and forwards to holdable system
    public void UpdateAim(Vector2 targetPosition);

    // Holdable routing — press/release/held API
    public void UseHoldablePressed(Vector2 target, bool isLeft);
    public void UseHoldableReleased(Vector2 target, bool isLeft);
    public void UseHoldableHeld(Vector2 target, bool isLeft);

    // Facing is based on aim (mouse) position, not movement direction.
    // This allows running one way while aiming/facing another.
    private void UpdateAnimation()
    {
        if (_flipRoot != null)
        {
            bool aimingLeft = _aimTarget.X < GlobalPosition.X;
            _flipRoot.Scale = new Vector2(aimingLeft ? -1 : 1, 1);
        }
        // Animation state: walk when moving on floor, idle otherwise
    }

    // Overrides CheckHazardTiles to skip while invincible
    // Subscribes to EntityDiedEvent → ReloadCurrentScene()
    // Subscribes to DamageAppliedEvent → starts invincibility timer + sprite flashing
}
```

### NPCEntityCharacterBody2D

**File:** `Scripts/Entity/NPCEntityCharacterBody2D.cs`

NPC/enemy behavior. Extends EntityCharacterBody2D. Stationary for now (no AI controller).

```csharp
public partial class NPCEntityCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new EnemyDefinition _definition;
    [Export] private Label _healthLabel;  // debug health above head

    // Subscribes to DamageAppliedEvent → updates health label
    // Subscribes to EntityDiedEvent → QueueFree()
}
```

---

## 5. Game Systems

### Base Class

**File:** `Scripts/Core/GameSystem.cs`

```csharp
public abstract partial class GameSystem : Node
{
    public override void _Ready() { Initialize(); }
    protected abstract void Initialize();
}
```

### Singleton Pattern (all systems)

```csharp
public static MySystem Instance { get; private set; }

public override void _Ready()
{
    if (Instance != null) { GD.PrintErr("Duplicate"); QueueFree(); return; }
    Instance = this;
    base._Ready();
}

public override void _ExitTree()
{
    if (Instance == this) Instance = null;
}
```

**Rules:**
- No `GetNode` calls to find singletons. Always use `MySystem.Instance`.
- `_Ready()` sets `Instance` and checks for duplicates (logs error + frees duplicate).
- `_ExitTree()` clears `Instance` only if it's still the current one.

### Autoloads (registered in project.godot)

- `EventBus` — `Scripts/Core/EventBus.cs`
- `CombatSystem` — `Scripts/Systems/CombatSystem.cs`
- `HealthSystem` — `Scripts/Systems/HealthSystem.cs`
- `HazardSystem` — `Scripts/Systems/HazardSystem.cs`

### CombatSystem

**File:** `Scripts/Systems/CombatSystem.cs`

- Listens for: `HitEvent`
- Filters: only processes hits on `EntityCharacterBody2D` targets
- Reads: target's status effects for damage modifiers
- Calculates: `finalDamage = baseDamage * statusModifiers`
- Raises: `DamageAppliedEvent`

```csharp
private void OnHit(HitEvent evt)
{
    var target = GodotObject.InstanceFromId(evt.TargetInstanceId);
    if (target is not EntityCharacterBody2D entity) return;

    float finalDamage = evt.BaseDamage;
    foreach (var effect in entity.RuntimeData.StatusEffects)
        finalDamage *= effect.Definition.DamageMultiplier;

    EventBus.Instance.Raise(new DamageAppliedEvent
    {
        TargetInstanceId = evt.TargetInstanceId,
        FinalDamage = finalDamage
    });
}
```

### HealthSystem

**File:** `Scripts/Systems/HealthSystem.cs`

- Listens for: `DamageAppliedEvent`
- Modifies: entity's `RuntimeData.CurrentHealth`
- Raises: `EntityDiedEvent` when health reaches 0

```csharp
private void OnDamageApplied(DamageAppliedEvent evt)
{
    var target = GodotObject.InstanceFromId(evt.TargetInstanceId);
    if (target is not EntityCharacterBody2D entity) return;

    entity.RuntimeData.CurrentHealth -= evt.FinalDamage;
    if (entity.RuntimeData.CurrentHealth < 0)
        entity.RuntimeData.CurrentHealth = 0;

    if (entity.RuntimeData.CurrentHealth <= 0)
    {
        EventBus.Instance.Raise(new EntityDiedEvent
        {
            EntityInstanceId = evt.TargetInstanceId,
            KillerInstanceId = 0
        });
    }
}
```

### HazardSystem

**File:** `Scripts/Systems/HazardSystem.cs`

- Owns: `[Export] HazardDefinition` resource
- Listens for: `HazardContactEvent`
- Looks up damage from definition by hazard type
- Raises: `HitEvent` with the appropriate damage

```csharp
private void OnHazardContact(HazardContactEvent evt)
{
    float damage = _hazardDefinition.GetDamage(evt.HazardType);
    if (damage <= 0) return;

    EventBus.Instance.Raise(new HitEvent
    {
        TargetInstanceId = evt.EntityInstanceId,
        SourceInstanceId = 0,  // environment
        BaseDamage = damage,
        HitDirection = Vector2.Zero,
        HitPosition = evt.Position,
        Projectile = null
    });
}
```

### StatusEffectSystem

**File:** `Scripts/Systems/StatusEffectSystem.cs`

- Loads StatusEffectDefinition .tres files from `Resources/Data/StatusEffects/` at startup
- Ticks active status effects each frame
- Removes expired effects
- Provides methods: `ApplyEffect()`, `RemoveEffect()`, `HasEffect()`, `TickEffects()`
- Raises: `StatusEffectAppliedEvent`, `StatusEffectRemovedEvent`

---

## 6. Holdable / Weapon System

### Holdable Base Class

**File:** `Scripts/Combat/Holdables/Holdable.cs`

```csharp
public abstract partial class Holdable : Node2D
{
    protected float _useCooldown = 0.2f;
    protected float _timeSinceLastUse = 999f;
    protected Node2D _owner;

    public virtual void InitOwner(Node2D owner) { _owner = owner; }
    public virtual void Update(double delta) { _timeSinceLastUse += (float)delta; }
    public bool CanUse() => _timeSinceLastUse >= _useCooldown;
    protected void ResetCooldown() { _timeSinceLastUse = 0f; }

    // Input-driven API: controller tells holdable about button state
    public virtual void OnUsePressed(Vector2 targetPosition) { }
    public virtual void OnUseReleased(Vector2 targetPosition) { }
    public virtual void OnUseHeld(Vector2 targetPosition) { }

    // Aim — called every physics frame with the current aim target (mouse position).
    // Weapons override to rotate toward target. Can be driven by animation scripts later.
    public virtual void UpdateAim(Vector2 targetPosition) { }

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
}
```

### Weapon Class

**File:** `Scripts/Combat/Weapons/Weapon.cs`

Single class for all weapon types. Behavior driven by `WeaponDefinition` + `ProjectileDefinition`.

Each weapon scene has a `ProjectileSpawn` Node2D child that defines the muzzle position. The weapon references this via `[Export] private Node2D _projectileSpawn`. All projectile spawning and hitscan raycasts originate from this node's GlobalPosition, so the spawn point flips correctly with the character via the FlipRoot hierarchy.

Weapons rotate to face the aim target every frame via `UpdateAim`. When under a parent with negative X scale (FlipRoot facing left), the weapon corrects the vertical flip by checking the parent's transform determinant.

```csharp
public partial class Weapon : Holdable
{
    [Export] private WeaponDefinition _weaponDefinition;
    [Export] private Node2D _projectileSpawn;      // muzzle position (child Node2D)
    [Export] private AnimationPlayer _animationPlayer;

    private bool _hasFiredThisPress = false;

    public override void UpdateAim(Vector2 targetPosition)
    {
        LookAt(targetPosition);
        // Correct vertical flip when parent has negative X scale
        var pt = GetParent<Node2D>().GlobalTransform;
        bool parentFlipped = (pt.X.X * pt.Y.Y - pt.X.Y * pt.Y.X) < 0;
        Scale = new Vector2(1, parentFlipped ? -1 : 1);
    }

    private Vector2 GetSpawnPosition()
    {
        return _projectileSpawn != null ? _projectileSpawn.GlobalPosition : _owner.GlobalPosition;
    }

    private void TryFire(Vector2 targetPosition)
    {
        if (!CanUse() || _weaponDefinition?.Projectile == null) return;
        _hasFiredThisPress = true;

        var projDef = _weaponDefinition.Projectile;
        float damage = projDef.Damage * _weaponDefinition.DamageScale;

        if (projDef.Speed == 0)
            FireInstant(targetPosition, damage, projDef);   // raycast from ProjectileSpawn
        else
            FireProjectile(targetPosition, damage, projDef); // physical from ProjectileSpawn

        ResetCooldown();

        if (CameraController.Instance != null && _weaponDefinition.ScreenShakeScale > 0)
            CameraController.Instance.Shake(_weaponDefinition.ScreenShakeScale, _weaponDefinition.ScreenShakeDurationScale);
    }
}
```

### Projectile

**File:** `Scripts/Combat/Projectiles/Projectile.cs`

Base class for projectile scenes. Receives `ProjectileDefinition` + calculated damage in `Initialize()`.

```csharp
public abstract partial class Projectile : Area2D
{
    protected ProjectileDefinition _projectileDefinition;
    protected float _damage;
    protected Vector2 _direction;
    protected float _timeAlive = 0f;
    protected Node2D _shooter;

    public virtual void Initialize(Vector2 direction, float damage,
        ProjectileDefinition definition, Node2D shooter = null)
    {
        _direction = direction.Normalized();
        _damage = damage;
        _projectileDefinition = definition;
        _shooter = shooter;
        Rotation = direction.Angle();
    }
}
```

**File:** `Scripts/Combat/Projectiles/StandardBullet.cs`

```csharp
protected override void OnHit(Node2D body)
{
    EventBus.Instance.Raise(new HitEvent
    {
        TargetInstanceId = body.GetInstanceId(),
        SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
        BaseDamage = _damage,
        HitDirection = _direction,
        HitPosition = GlobalPosition,
        Projectile = _projectileDefinition
    });
}
```

### HoldableSystem

**File:** `Scripts/Combat/Holdables/HoldableSystem.cs`

Routes press/release/held/aim calls from the character to the equipped holdables. Manages left and right holdable slots. Weapons are instantiated as children of a `WeaponPosition` Node2D (located under FlipRoot) so they inherit the character's facing direction and flip automatically.

```csharp
public partial class HoldableSystem : Node
{
    [Export] private PackedScene _leftHoldableScene;
    [Export] private PackedScene _rightHoldableScene;
    [Export] private Node2D _weaponPosition;     // under FlipRoot — weapons are children of this

    public void UpdateAim(Vector2 target)
    {
        _leftHoldable?.UpdateAim(target);
        _rightHoldable?.UpdateAim(target);
    }

    public void PressLeft(Vector2 target)   { _leftHoldable?.OnUsePressed(target); }
    public void PressRight(Vector2 target)  { _rightHoldable?.OnUsePressed(target); }
    public void HeldLeft(Vector2 target)    { _leftHoldable?.OnUseHeld(target); }
    public void HeldRight(Vector2 target)   { _rightHoldable?.OnUseHeld(target); }
    public void ReleaseLeft(Vector2 target) { _leftHoldable?.OnUseReleased(target); }
    public void ReleaseRight(Vector2 target){ _rightHoldable?.OnUseReleased(target); }

    private Holdable InstantiateHoldable(PackedScene scene)
    {
        var instance = scene.Instantiate<Holdable>();
        var parent = _weaponPosition != null ? (Node)_weaponPosition : this;
        parent.AddChild(instance);  // added under WeaponPosition for transform inheritance
        instance.InitOwner(_owner);
        instance.OnEquip();
        return instance;
    }
}
```

### CharacterController

**File:** `Scripts/Player/CharacterController.cs`

- `_UnhandledInput`: detect `IsActionPressed` → call `UseHoldablePressed`, detect `IsActionReleased` → call `UseHoldableReleased`
- `_PhysicsProcess`: calls `UpdateAim(mousePosition)` every frame for weapon rotation and character facing; while action is held (`Input.IsActionPressed`), call `UseHoldableHeld` every frame
- The holdable/weapon handles cooldown and automatic vs. semi-auto internally
- Mouse position is obtained via `_playerCharacter.GetGlobalMousePosition()` and passed through the aim pipeline — this decouples the input source, allowing AI controllers to feed a different target position later

---

## 7. Event Flow Diagrams

### Projectile Damage Flow

```
Weapon.TryFire()
  → Speed > 0: spawns Projectile scene at muzzle
      → Projectile travels, hits body
      → Raises HitEvent
  → Speed == 0: does raycast from muzzle
      → Spawns Projectile scene at hit point (for VFX)
      → Raises HitEvent
  ↓
CombatSystem.OnHit()
  → target is EntityCharacterBody2D? → calculate damage → DamageAppliedEvent
  → target is door/other? → CombatSystem ignores, DoorSystem handles
  ↓
HealthSystem.OnDamageApplied()
  → subtract health from RuntimeData
  → health <= 0? → EntityDiedEvent
  ↓
PlayerCharacterBody2D.OnEntityDied() → ReloadCurrentScene()
NPCEntityCharacterBody2D.OnEntityDied() → QueueFree()
```

### Hazard Damage Flow

```
EntityCharacterBody2D._PhysicsProcess()
  → MoveAndSlide()
  → CheckHazardTiles() — iterates slide collisions
  → Finds hazard tile → raises HazardContactEvent
  ↓
HazardSystem.OnHazardContact()
  → looks up damage from HazardDefinition
  → raises HitEvent (SourceInstanceId = 0, Projectile = null)
  ↓
CombatSystem → DamageAppliedEvent → HealthSystem → etc.
  ↓
PlayerCharacterBody2D.OnDamageApplied()
  → starts invincibility timer + sprite flashing (player-only)
```

### Holdable Input Flow

```
CharacterController._UnhandledInput()
  → IsActionPressed("shoot") → PlayerCharacterBody2D.UseHoldablePressed(target, left)
  → IsActionReleased("shoot") → PlayerCharacterBody2D.UseHoldableReleased(target, left)

CharacterController._PhysicsProcess()
  → Get mouse world position
  → PlayerCharacterBody2D.UpdateAim(mousePos)
      → stores _aimTarget (used for character facing in UpdateAnimation)
      → HoldableSystem.UpdateAim(mousePos)
          → Weapon.UpdateAim(mousePos) — LookAt + flip correction
  → Input.IsActionPressed("shoot") → PlayerCharacterBody2D.UseHoldableHeld(target, left)
  ↓
PlayerCharacterBody2D → HoldableSystem → Holdable.OnUsePressed/Held/Released
  ↓
Weapon.OnUseHeld()
  → IsAutomatic && hasFiredThisPress? skip (semi-auto)
  → CanUse()? → TryFire() → spawn projectile or raycast from ProjectileSpawn node
  → CameraController.Instance.Shake(strengthScale, durationScale)
```

---

## 8. Camera

**File:** `Scripts/Camera/CameraController.cs` — Singleton (static `Instance`)

Follows the player with smoothing, look-ahead, and screen shake. Also handles camera rotation for gravity flips.

### Screen Shake

Camera owns the base shake values. Weapons (and other systems) request shake with scale factors, not absolute overrides. This keeps weapon data simple and lets designers tune the base feel globally.

```csharp
[ExportGroup("Screen Shake")]
[Export] private float _baseShakeStrength = 5.0f;   // base intensity in pixels
[Export] private float _baseShakeDuration = 0.3f;    // base duration in seconds

// Strength and duration are scaled from the base values.
public void Shake(float strengthScale = 1.0f, float durationScale = 1.0f)
{
    float strength = _baseShakeStrength * strengthScale;
    float duration = _baseShakeDuration * durationScale;
    _shakeStrength = Mathf.Max(_shakeStrength, strength);
    _shakeDecayRate = duration > 0 ? strength / duration : strength / 0.01f;
}
```

### Other Features

- **Follow smoothing** with look-ahead based on movement direction
- **Director offset** for cutscenes/level reveals (`DirectAttention()`)
- **Gravity rotation** — camera rotates to match entity gravity, with configurable delay and speed

---

## 9. File Structure & Scene Hierarchy

### Player Scene Hierarchy

```
PlayerCharacterBody2D (CharacterBody2D)
  ├─ CollisionShape2D
  ├─ FlipRoot (Node2D)              ← Scale.X = ±1 for facing
  │   ├─ AnimatedSprite2D            ← spritesheet animations (walk, idle)
  │   ├─ WallSlideDustPosition       ← dust particle spawn marker
  │   └─ WeaponPosition (Node2D)     ← weapons instantiated here
  │       ├─ Pistol (Weapon)          ← runtime child
  │       │   ├─ ColorRect            ← placeholder visual
  │       │   └─ ProjectileSpawn      ← muzzle position
  │       └─ Shotgun (Weapon)         ← runtime child
  │           ├─ ColorRect
  │           └─ ProjectileSpawn
  ├─ HoldableSystem (Node)           ← manages holdable slots
  └─ AnimationPlayer                  ← for future AnimationTree integration
```

### Scripts

```
Scripts/
├── Core/
│   ├── EventBus.cs
│   ├── GameSystem.cs
│   └── Events/
│       ├── CombatEvents.cs        (HitEvent, DamageAppliedEvent, EntityDiedEvent)
│       ├── EnvironmentEvents.cs   (HazardContactEvent)
│       └── StatusEvents.cs        (StatusEffectAppliedEvent, StatusEffectRemovedEvent)
├── Entity/
│   ├── EntityCharacterBody2D.cs   (base class: movement, physics, hazard detection)
│   └── NPCEntityCharacterBody2D.cs (enemy/NPC, health label, death cleanup)
├── Systems/
│   ├── CombatSystem.cs
│   ├── HealthSystem.cs
│   ├── HazardSystem.cs
│   └── StatusEffectSystem.cs
├── Data/
│   ├── Definitions/
│   │   ├── CharacterDefinition.cs
│   │   ├── PlayerDefinition.cs
│   │   ├── EnemyDefinition.cs
│   │   ├── WeaponDefinition.cs
│   │   ├── ProjectileDefinition.cs
│   │   ├── HazardDefinition.cs
│   │   └── StatusEffectDefinition.cs
│   ├── Runtime/
│   │   └── EntityRuntimeData.cs
│   ├── SaveData/
│   │   ├── PlayerSaveData.cs      (future)
│   │   └── GlobalSaveData.cs      (future)
│   └── Enums/
│       └── TileEnums.cs
├── Player/
│   ├── PlayerCharacterBody2D.cs   (extends EntityCharacterBody2D)
│   └── CharacterController.cs     (input → player commands)
├── Combat/
│   ├── Holdables/
│   │   ├── Holdable.cs            (base: press/release/held API)
│   │   └── HoldableSystem.cs      (left/right slots, routes input)
│   ├── Weapons/
│   │   └── Weapon.cs              (unified: projectile + instant-hit)
│   └── Projectiles/
│       ├── Projectile.cs          (base: movement, lifetime, collision)
│       └── StandardBullet.cs      (linear bullet, raises HitEvent)
├── Camera/
│   └── CameraController.cs
└── UI/
    └── DebugHUD.cs

Resources/
├── Data/
│   ├── Characters/
│   │   ├── Player/
│   │   │   └── player.tres         (PlayerDefinition)
│   │   └── Enemies/
│   │       └── target_dummy.tres   (EnemyDefinition)
│   ├── Weapons/
│   │   ├── pistol.tres             (WeaponDefinition)
│   │   ├── shotgun.tres            (WeaponDefinition)
│   │   └── hitscan_test_weapon.tres (WeaponDefinition)
│   ├── Projectiles/
│   │   └── bullet.tres             (ProjectileDefinition)
│   ├── Hazards/
│   │   └── hazards.tres            (HazardDefinition)
│   └── StatusEffects/
│       └── (future .tres files)
└── Tilemaps/

Scenes/
├── Characters/
│   ├── PlayerCharacterBody2D.tscn
│   └── TargetDummy.tscn
├── Weapons/
│   ├── Pistol.tscn                 (Weapon node + ColorRect + ProjectileSpawn)
│   └── Shotgun.tscn                (Weapon node + ColorRect + ProjectileSpawn)
├── Projectiles/
│   └── StandardBullet.tscn         (projectile scene)
└── Levels/
    ├── MainLevel.tscn
    └── TestLevel.tscn
```

---

## 9. What Is and Isn't an Event (Design Guidelines)

### USE events for:
| Scenario | Event | Why |
|----------|-------|-----|
| Something got hit | HitEvent | Multiple systems react: combat, doors, audio, particles |
| Damage was calculated | DamageAppliedEvent | Health system, player invincibility, UI, screen shake |
| Entity died | EntityDiedEvent | Player reload, enemy cleanup, score, achievements |
| Status effect changed | StatusEffectApplied/Removed | UI, audio, visual effects |
| Touched hazard tile | HazardContactEvent | Crosses entity→system boundary |

### DON'T use events for:
| Scenario | Why not |
|----------|---------|
| Character jumped | Internal to entity, one consumer (animation). Add event later if audio/particles need it. |
| Movement input | Internal to entity controller. |
| Animation triggers | Internal to entity visual layer. |
| Physics calculations | Tight-loop, one consumer. |
| Cooldown checks | Internal to holdable. |

### Rule of thumb:
- Does it cross a system boundary? → Event.
- Do multiple independent things need to react? → Event.
- Is it internal mechanics with one consumer? → Direct method call.
- Not sure yet? → Don't add the event. Add it when a second consumer appears.

### NPC AI and events:
- AI controllers call entity methods directly (`MoveLeft`, `Jump`, etc.) — same as CharacterController but with AI logic instead of input.
- AI *listens* to events to react: `DamageAppliedEvent` → aggro/flee, `EntityDiedEvent` → stop processing, `StatusEffectApplied` → adjust behavior.
- AI does NOT raise movement events. Movement is a direct command, not a broadcast.

---

## 11. Save/Load (Future)

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
