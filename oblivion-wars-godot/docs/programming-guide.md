# Oblivion Wars — Programming Guide

**Last Updated:** February 2026

This guide explains the game's architecture, how systems work together, and how to add new features. Use this when you've been away from the project and need to remember how everything flows.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Concepts](#core-concepts)
3. [How Events Flow](#how-events-flow)
4. [NPC AI System](#npc-ai-system)
5. [Adding New Features](#adding-new-features)
6. [System Reference](#system-reference)
7. [Common Patterns](#common-patterns)
8. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### The Big Picture

Oblivion Wars uses an **event-driven, data-oriented architecture**:

- **Entities** (player, enemies) handle their own physics and movement
- **Systems** (CombatSystem, HealthSystem) process events and modify data
- **EventBus** connects systems without tight coupling
- **Definitions** (.tres resources) are the source of truth for stats and behavior
- **RuntimeData** tracks the current state of each entity

Think of it like this:
- **Entities** say "something happened" by raising events
- **Systems** listen and respond: "I heard someone got hit, let me calculate damage"
- **EventBus** is the messenger that delivers these notifications

### Why This Architecture?

**Decoupling:** Systems don't need to know about each other. CombatSystem calculates damage, HealthSystem subtracts health, and neither knows the other exists.

**Flexibility:** Want a new system that reacts to damage? Just subscribe to DamageAppliedEvent. No need to modify existing code.

**Data-driven:** Create new enemies, weapons, and abilities by making .tres resource files, not by writing code.

### The Three Layers

1. **Entity Layer** (movement, physics, input)
   - EntityCharacterBody2D does movement and wall sliding
   - PlayerCharacterBody2D handles input and animations
   - NPCEntityCharacterBody2D handles AI

2. **System Layer** (game logic, rules)
   - CombatSystem calculates damage
   - HealthSystem manages health and death
   - StatusEffectSystem handles buffs/debuffs

3. **Definition Layer** (data)
   - CharacterDefinition defines stats
   - WeaponDefinition defines weapon behavior
   - All stored as .tres resources

---

## Core Concepts

### Events Are For Cross-System Communication

**Use events when:**
- Multiple independent things need to react (damage affects health, UI, camera shake, audio)
- You want to decouple systems (CombatSystem doesn't know HealthSystem exists)
- Something crosses a boundary (entity → system, or system → system)

**Don't use events for:**
- Internal entity mechanics (jumping, moving)
- Single consumer (if only one thing cares, just call it directly)
- Tight loops (physics calculations, cooldown checks)

**Rule of thumb:** If you're not sure, don't add an event yet. Wait until you have 2+ consumers.

### Definitions vs RuntimeData

**Definitions (.tres)** are read-only templates:
- PlayerDefinition: MaxHealth = 100, MoveSpeed = 300
- Never modified during gameplay
- Shared by all instances (multiple enemies can use the same EnemyDefinition)

**RuntimeData** is mutable per-instance:
- CurrentHealth = 75 (player took damage)
- StatusEffects = [Poison, Slow] (temporary effects)
- Created fresh when entity spawns

Think of it like classes vs objects in programming: Definition is the class, RuntimeData is the object instance.

### Singletons Are Global Systems

Every major system (EventBus, CombatSystem, HealthSystem) is a singleton:
- Only one instance exists
- Accessed via `MySystem.Instance`
- Never use GetNode to find them
- Registered as autoloads in project settings

### FlipRoot Pattern For Character Facing

Characters flip horizontally by setting `FlipRoot.Scale.X = ±1`:
- All visuals (sprite, weapons, particles) are children of FlipRoot
- When FlipRoot flips, everything flips with it automatically
- Player faces based on aim (mouse), not movement
- Enemies face based on target or movement

This is cleaner than manually flipping every child node.

---

## How Events Flow

### The Damage Pipeline

This is the most important flow to understand:

1. **Something causes a hit** (projectile, hazard, melee)
   - Weapon fires → spawns projectile OR does raycast
   - Entity touches hazard tile

2. **HitEvent is raised**
   - Contains: target, source, damage, position, direction
   - Generic event used for ALL hits (projectiles, melee, hazards, environment)

3. **CombatSystem processes hit**
   - Filters: Is target an EntityCharacterBody2D? (yes = calculate damage, no = ignore)
   - Applies damage modifiers from status effects
   - Raises DamageAppliedEvent with final damage

4. **HealthSystem subtracts health**
   - Modifies entity's RuntimeData.CurrentHealth
   - If health <= 0, raises EntityDiedEvent

5. **Multiple systems react**
   - PlayerCharacterBody2D: starts invincibility + flashing
   - NPCEntityCharacterBody2D: spawns drops, cleans up
   - GameHUD: updates health display
   - CameraController: screen shake

**Key insight:** Each system only does ONE thing. CombatSystem calculates damage but doesn't subtract health. HealthSystem subtracts health but doesn't know what caused it. This separation makes the code easy to modify and extend.

### The Input Pipeline

How a mouse click becomes a projectile:

1. **CharacterController detects input**
   - _UnhandledInput: mouse button pressed
   - Gets mouse world position

2. **Controller calls player**
   - `player.UseHoldablePressed(mousePosition, isLeftClick)`

3. **Player forwards to HoldableSystem**
   - `_holdableSystem.PressLeft(mousePosition)` or `PressRight()`

4. **HoldableSystem routes to weapon**
   - `_leftHoldable.OnUsePressed(mousePosition)`

5. **Weapon checks cooldown and fires**
   - If cooldown ready, spawns projectile OR does raycast
   - Raises HitEvent when projectile hits something

6. **Damage pipeline continues** (see above)

**Why so many layers?** Each layer has a specific job:
- CharacterController: knows about input
- PlayerCharacterBody2D: knows about the entity
- HoldableSystem: manages multiple holdables (left/right slots)
- Weapon: knows about firing mechanics

This makes it easy to replace input with AI later (AI controller just calls the same methods).

### The Spawn-to-Death Lifecycle

When an enemy spawns:

1. **Scene instantiates**
   - NPCEntityCharacterBody2D.tscn loads
   - _Ready() is called

2. **Entity initializes**
   - Reads Definition (MaxHealth, MoveSpeed, etc.)
   - Creates RuntimeData (CurrentHealth = MaxHealth)
   - Initializes weapons from Definition or scene
   - Subscribes to DamageAppliedEvent and EntityDiedEvent

3. **Entity lives**
   - _PhysicsProcess: movement, gravity, collision
   - AIController (if present) drives movement and shooting
   - Takes damage → DamageAppliedEvent → health decreases

4. **Entity dies**
   - Health reaches 0 → EntityDiedEvent raised
   - NPCEntityCharacterBody2D hears event → spawns drops → QueueFree()
   - Unsubscribes from events in _ExitTree()

---

## NPC AI System

### Architecture: LimboHSM + LimboState Subclasses

NPC AI uses three layers working together:

1. **NPCController** — evaluates conditions and dispatches transition events (the "brain"), handles movement/combat internals
2. **LimboHSM** — routes transition events to states based on the transition table
3. **LimboState subclasses** — implement behavior within each state (the "body")

States never worry about *when* to transition — they only implement *what to do*. NPCController owns all transition logic in one place, dispatching events like `"PlayerInRange"` or `"HealthLow"`. The HSM routes those events to the correct state based on transitions defined at startup.

### Design Philosophy

Most NPCs use the same **Basic** state scripts (`NPCStateIdle_Basic`, `NPCStatePatrol_Basic`, `NPCStateAttack_Basic`, `NPCStateFlee_Basic`). These delegate movement and combat to NPCController, which handles the internals (ground vs flying movement, weapon aiming, etc.). Behavioral variety comes from different settings `.tres` files and different NPCDefinitions, not different state scripts.

Only create a new state script when an enemy needs genuinely different behavior logic — e.g., `NPCStateAttack_Teleporter` for an enemy that teleports while attacking. No state inheritance; keep state scripts flat and independent.

### Scene Structure

The NPC scene root is `NPCEntityCharacterBody2D`. NPCController, HSM, and states are children:

```
TargetDummy (NPCEntityCharacterBody2D)   ← scene root, handles physics/movement
├── CollisionShape2D
├── FlipRoot/
│   ├── WeaponPosition
│   └── AnimatedSprite2D
├── HealthLabel
├── HoldableSystem
├── NPCController                        ← AI coordinator, holds refs + shared logic
│   └── RangeGizmo
└── LimboHSM                             ← state machine, lives in scene tree
    ├── IdleState       (NPCStateIdle_Basic)
    ├── PatrolState     (NPCStatePatrol_Basic + patrol_settings_basic.tres)
    ├── AttackState     (NPCStateAttack_Basic + attack_settings_basic.tres)
    └── FleeState       (NPCStateFlee_Basic + flee_settings_basic.tres)
```

### LimboState Subclasses as Behaviors

LimboState provides virtual methods for the state lifecycle:

- **`_Setup()`** — called once during initialization
- **`_Enter()`** — called when the state becomes active
- **`_Update(delta)`** — called every frame while active
- **`_Exit()`** — called when transitioning away

Each behavior is a LimboState subclass. State-specific tuning params live on a **settings Resource** exported by the state:

```csharp
// Settings resource — lives on disk as a .tres, reusable across enemies
// Default values in the class serve as the baseline for all enemies.
// Only create a custom .tres when an enemy needs non-default values.
[GlobalClass]
public partial class AttackSettings : Resource
{
    [Export] public float AttackRange = 200f;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public AimMode AimMode = AimMode.TrackPlayer;
}

// The state script — reads settings from its resource
[GlobalClass]
public partial class NPCStateAttack_Basic : LimboState
{
    [Export] private AttackSettings _settings;

    private NPCController _controller;

    public override void _Setup()
    {
        _controller = GetAgent() as NPCController;
    }

    public override void _Enter()
    {
        // TODO: Start aiming at player, begin attack cooldown
    }

    public override void _Update(double delta)
    {
        // TODO: Aim at player, shoot on cooldown, chase if needed
    }

    public override void _Exit()
    {
        _controller.StopShooting();
    }
}
```

### Data Organization

Two levels of settings, cleanly separated:

**AIBehaviorDataDefinition** — NPC-wide params that control *state transitions* (when do I change state?):
- DetectionRange, AggroRange
- FleeHealthThreshold
- Aggressive flag

**Per-state settings Resources** — params that control *behavior within a state* (what do I do here?):
- `AttackSettings` — AttackRange, AttackCooldown, AimMode
- `PatrolSettings` — PatrolRadius, IdlePauseMin, IdlePauseMax
- `FleeSettings` — FleeHealthThreshold

This means you can have two enemies with the same attack behavior but different detection ranges, or the same detection range but completely different attack styles.

```
Scripts/AI/NPCBehaviorScripts/
├── Idle/
│   └── NPCStateIdle_Basic.cs
├── Patrol/
│   └── NPCStatePatrol_Basic.cs
├── Attack/
│   ├── NPCStateAttack_Basic.cs
│   └── NPCStateAttack_Teleporter.cs      (future: special variant)
└── Flee/
    └── NPCStateFlee_Basic.cs

Scripts/Data/Definitions/StateSettings/
├── AttackSettings.cs
├── PatrolSettings.cs
└── FleeSettings.cs

Resources/Data/Characters/Enemies/
├── CommonData/
│   ├── npc_patrol_common_data.tres                (AIBehaviorDataDefinition)
│   └── Behaviors/StateSettingsTemplates/
│       ├── patrol_settings_basic.tres             (PatrolSettings)
│       ├── attack_settings_basic.tres             (AttackSettings)
│       └── flee_settings_basic.tres               (FleeSettings)
├── test_dummy_def.tres                            (NPCDefinition)
└── (future enemy defs...)
```

### NPC States

Standard states for regular NPCs:

| State | Basic Script | Description |
|---|---|---|
| **Idle** | `NPCStateIdle_Basic` | Standing still, waiting for idle timer to expire |
| **Patrol** | `NPCStatePatrol_Basic` | Moving within patrol area using NPCController movement |
| **Attack** | `NPCStateAttack_Basic` | Engaging player via NPCController (aim, shoot, chase) |
| **Flee** | `NPCStateFlee_Basic` | Running from player via NPCController |

### Standard Transitions

Defined in `NPCController.SetupHSM()` using the `NPCTransitionEvent` enum:

```
Idle     → Patrol     (IdleTimeout)
Idle     → Attack     (PlayerInRange)
Patrol   → Idle       (PatrolDone)
Patrol   → Attack     (PlayerInRange)
Attack   → Patrol     (PlayerLost)
Flee     → Patrol     (PlayerLost)
Any      → Flee       (HealthLow)
```

### Centralized Transition Logic

**States never evaluate transition conditions.** All transition logic lives in `NPCController.EvaluateTransitions()`, which dispatches events to the HSM every physics frame. This prevents bugs from forgetting to check conditions in new state variants:

```csharp
private void EvaluateTransitions()
{
    if (_stateTree == null) return;

    float healthPercent = NPCRuntimeData.MaxHealth > 0
        ? NPCRuntimeData.CurrentHealth / NPCRuntimeData.MaxHealth
        : 1f;

    // Priority order — highest priority first
    if (healthPercent <= _behavior.FleeHealthThreshold)
        _stateTree.Dispatch(Event(NPCTransitionEvent.HealthLow));
    else if (IsPlayerInAggroRange())
        _stateTree.Dispatch(Event(NPCTransitionEvent.PlayerInRange));
    else if (!IsPlayerInDetectRange())
        _stateTree.Dispatch(Event(NPCTransitionEvent.PlayerLost));
}
```

Transition event names use `EnumStringNames<NPCTransitionEvent>` to convert enum values to `StringName` without string literals. The HSM only acts on a dispatch if a matching transition exists from the current state.

### HSM Setup

Transitions are registered in `NPCController.SetupHSM()`, called from `_Ready()`. State nodes are exported and wired via the inspector (no GetNode):

```csharp
[Export] private LimboHsm _stateTree;

[ExportGroup("HSM States")]
[Export] private LimboState _idleState;
[Export] private LimboState _patrolState;
[Export] private LimboState _attackState;
[Export] private LimboState _fleeState;

private void SetupHSM()
{
    _stateTree.AddTransition(_idleState, _patrolState, Event(NPCTransitionEvent.IdleTimeout));
    _stateTree.AddTransition(_idleState, _attackState, Event(NPCTransitionEvent.PlayerInRange));
    _stateTree.AddTransition(_patrolState, _idleState, Event(NPCTransitionEvent.PatrolDone));
    _stateTree.AddTransition(_patrolState, _attackState, Event(NPCTransitionEvent.PlayerInRange));
    _stateTree.AddTransition(_attackState, _patrolState, Event(NPCTransitionEvent.PlayerLost));
    _stateTree.AddTransition(_fleeState, _patrolState, Event(NPCTransitionEvent.PlayerLost));
    _stateTree.AddTransition(_stateTree.Anystate(), _fleeState, Event(NPCTransitionEvent.HealthLow));

    _stateTree.Initialize(this);   // pass NPCController as the agent
    _stateTree.SetActive(true);
}
```

### Building Different Enemy Types

Most enemies use the Basic state scripts. Only create a new state script for genuinely different behavior:

| Enemy | States | Settings | Scene |
|---|---|---|---|
| Grunt | All Basic | attack_settings_basic.tres | NPC.tscn |
| Fast Grunt | All Basic | attack_settings_fast.tres (custom) | NPC.tscn |
| Teleporter | Basic + `NPCStateAttack_Teleporter` | attack_teleport.tres | NPC_Teleporter.tscn |
| Turret | No patrol, Attack only | attack_settings_basic.tres | NPC_Turret.tscn |

For tuning-only variants (fast grunt vs slow grunt), reuse the same scene and swap the settings `.tres` in the inspector. Only create a new inherited scene when the state scripts differ.

### Adding Special Behaviors

When an enemy needs behavior that the Basic scripts can't handle, create a new state script:

1. Create `NPCStateAttack_Teleporter.cs` in `Scripts/AI/NPCBehaviorScripts/Attack/`
2. Create `AttackTeleporterSettings.cs` with the extra params (teleport interval, radius)
3. Create an inherited scene, swap the AttackState script to the new one
4. Assign the settings `.tres`

Keep NPCController as the workhorse — the special state script should still delegate movement/combat to NPCController and only add the unique behavior (teleportation logic).

### Animations

Each state script triggers its own animations. The state calls into NPCController or the character body directly, so animation names live with the behavior that uses them.

### Bosses

Bosses use **LimboAI Behavior Trees** instead of the shared HSM. Boss fights have complex sequencing that BTs handle well:

- Multi-phase behavior with unique phase transitions
- Telegraph → attack → recovery → vulnerability windows
- Interruptible combos (stagger mid-sequence)
- Parallel behaviors (summon adds while attacking)

Each boss gets its own BT `.tres` file and can subclass `NPCController` as `BossController` to expose boss-specific actions as BT tasks.

### Where Things Live

| Concern | Location |
|---|---|
| Transition conditions (when to switch states) | `NPCController.EvaluateTransitions()` |
| Transition routing (which state follows which) | `NPCController.SetupHSM()` via `AddTransition()` |
| Transition event names | `NPCTransitionEvent` enum + `EnumStringNames` |
| What each state does | LimboState subclass script on the state node |
| State-specific tuning (cooldowns, forces) | Per-state settings Resource `.tres` |
| NPC-wide tuning (detect range, aggro range) | AIBehaviorDataDefinition `.tres` |
| Movement/combat internals | NPCController (shared across all state scripts) |
| Per-enemy composition | Scene inheritance (swap state scripts + settings) |

### Future Escape Hatch

If you end up with 30+ enemy variants and scene inheritance feels heavy, you can switch to runtime HSM construction — store state script references in `AIBehaviorDataDefinition` and build the HSM in `_Ready()`. You'd lose editor-time visual editing but keep runtime inspection (the constructed HSM is visible in Godot's Remote scene tree during play). Not worth it yet, but the architecture doesn't prevent it.

---

## Adding New Features

### How to Add a New Enemy

**Quick version:** Create a new EnemyDefinition .tres file. Done.

**Detailed version:**

1. **Decide on stats**
   - Health, speed, damage, aggro range
   - Which weapon(s) to use
   - Drop table (coins, items)

2. **Create the Definition**
   - Duplicate `Resources/Data/Characters/Enemies/target_dummy.tres`
   - Rename to `archer_enemy.tres` or whatever
   - Set EntityId = "archer_enemy"
   - Set stats (MaxHealth, MoveSpeed, ContactDamage)
   - Assign weapons: LeftHoldable = Bow.tscn (or leave blank)
   - Configure drop table

3. **Option A: Use existing scene** (recommended)
   - Open level scene
   - Drag in `TargetDummy.tscn`
   - In inspector, set Definition = `archer_enemy.tres`
   - Set UseDefinitionWeapons = true on HoldableSystem
   - Done! The enemy will use the new stats and weapons

4. **Option B: Create custom scene** (for unique visuals)
   - Duplicate `TargetDummy.tscn`
   - Rename to `ArcherEnemy.tscn`
   - Replace sprite, adjust animations
   - Set Definition = `archer_enemy.tres`
   - Save scene

5. **Test**
   - Run level
   - Shoot enemy, verify health
   - Let enemy die, verify drops spawn
   - Check weapon fires correctly

**Common customizations:**
- Flying enemy: Disable gravity, tweak AI detection
- Boss enemy: Set IsBoss = true, increase stats, custom sprite
- Turret enemy: No movement, only shoots
- Melee enemy: No weapons, increase ContactDamage

### How to Add a New Weapon

1. **Create ProjectileDefinition first**
   - Duplicate `Resources/Data/Projectiles/bullet.tres`
   - Set Speed (0 = hitscan, >0 = physical projectile)
   - Set Damage, Lifetime, behavior flags

2. **Create WeaponDefinition**
   - Duplicate `Resources/Data/Weapons/pistol.tres`
   - Set UseCooldown, IsAutomatic, DamageScale
   - Set SpreadCount for shotguns (1 = single shot, 5+ = spread)
   - Assign Projectile = your ProjectileDefinition
   - Set ScreenShakeScale (1.0 = normal, 2.0 = double shake)

3. **Create weapon scene**
   - Duplicate `Scenes/Weapons/Pistol.tscn`
   - Rename to `MachineGun.tscn`
   - Adjust visual (ColorRect or sprite)
   - Move ProjectileSpawn node to adjust muzzle position
   - Set WeaponDefinition in inspector

4. **Assign to character**
   - **For player:** Edit PlayerDefinition, set LeftHoldable or RightHoldable
   - **For enemy:** Edit EnemyDefinition, set LeftHoldable or RightHoldable
   - Make sure entity's HoldableSystem has UseDefinitionWeapons = true

5. **Test**
   - Fire weapon, check projectile spawns at correct position
   - Check damage amount (see health labels on enemies)
   - Verify screen shake feels right
   - Test auto vs semi-auto behavior

**Weapon types:**
- **Hitscan** (instant): Set Projectile.Speed = 0
- **Physical** (travels): Set Projectile.Speed > 0
- **Shotgun**: Set WeaponDefinition.SpreadCount = 5-8
- **Explosive**: Set Projectile.ExplosionRadius > 0

### How to Add a New Status Effect

1. **Create StatusEffectDefinition**
   - Create new .tres in `Resources/Data/StatusEffects/`
   - Set EffectId = "poison", DisplayName = "Poison"
   - Set DefaultDuration = 5.0 (seconds)
   - Set TickInterval = 1.0, TickDamage = 5.0 (for DoT)
   - OR set SpeedMultiplier = 0.5 (for slow)
   - OR set DamageMultiplier = 1.5 (for vulnerability)

2. **Apply effect to entity**
   - From code: `StatusEffectSystem.Instance.ApplyEffect(targetInstanceId, "poison")`
   - From projectile: (future) Set ProjectileDefinition.ApplyStatusEffect = "poison"

3. **Effect automatically ticks**
   - StatusEffectSystem ticks all active effects each frame
   - Applies damage if TickDamage > 0
   - Modifies speed/damage multipliers
   - Raises StatusEffectRemovedEvent when expired

**Common effects:**
- **Poison/Burn** (DoT): TickInterval = 1.0, TickDamage = 5.0
- **Slow**: SpeedMultiplier = 0.5
- **Vulnerability**: DamageMultiplier = 1.5
- **Stun**: (future) Add StunFlag or set SpeedMultiplier = 0

### How to Add a New Ability

**Not yet implemented, but here's the plan:**

1. **Create AbilityDefinition**
   - AbilityId = "double_jump", DisplayName = "Double Jump"
   - Unlocked = false (player must acquire it)
   - Cooldown, energy cost, etc.

2. **Add logic to entity**
   - PlayerCharacterBody2D: Check if player has ability
   - Add jump counter for double jump
   - Add dash velocity for dash ability

3. **Gate progression**
   - Doors check for ability: RequiredAbilityId = "wall_climb"
   - Save system tracks unlocked abilities

4. **UI feedback**
   - Show ability icon when unlocked
   - Gray out if on cooldown

### How to Add a Checkpoint

1. **Drag scene into level**
   - Instantiate `Scenes/Interaction/Checkpoint.tscn` (once you create the template)
   - OR copy existing checkpoint from MainLevel

2. **Position it**
   - Move checkpoint to desired location
   - Adjust RespawnPosition child node (where player spawns)

3. **Generate unique ID**
   - In the checkpoint's inspector, find "Generate Unique Id" checkbox
   - Check it once — it will auto-generate a unique ID and uncheck itself
   - The UniqueId field will be populated (e.g., "checkpoint_MainLevel_A3F2")
   - Save scene (Ctrl+S)

4. **Test**
   - Run level, interact with checkpoint (press E when near)
   - Check console for confirmation message
   - Die and verify respawn at correct position

**Important:** Each checkpoint MUST have a unique UniqueId. Never duplicate checkpoints without regenerating IDs.

### How to Create a New Saveable Object

The save system is **data-driven** — each object defines its own custom save data without modifying core systems.

**Step 1: Add object type to enum**

Edit `Scripts/Data/SaveData/SaveableLevelObjectType.cs`:
```csharp
public enum SaveableLevelObjectType
{
    Unknown = 0,
    Checkpoint = 1,
    Door = 2,
    YourNewObject = 9,  // Add here
}
```

**Step 2: Create custom save data class**

Create `Scripts/Data/SaveData/YourObjectSaveData.cs`:
```csharp
using Godot;

[GlobalClass]
public partial class YourObjectSaveData : LevelObjectSaveDataEntry
{
    [Export] public bool IsActivated = false;
    [Export] public float CustomValue = 0f;

    public YourObjectSaveData()
    {
        ObjectType = SaveableLevelObjectType.YourNewObject;
    }
}
```

**Step 3: Implement ISaveableObject on your node**

```csharp
using Godot;

#if TOOLS
[Tool]
#endif
public partial class YourObject : Area2D, ISaveableObject
{
    [ExportGroup("Identification")]
    [Export] public string UniqueId { get; set; } = "";

    [ExportGroup("Editor Tools")]
    [Export]
    private bool GenerateUniqueId
    {
        get => false;
        set
        {
            if (value)
            {
#if TOOLS
                if (Engine.IsEditorHint())
                {
                    SaveableObjectHelper.GenerateUniqueId(this, this);
                }
#endif
            }
        }
    }

    private bool _isActivated = false;

    public SaveableLevelObjectType GetObjectType() =>
        SaveableLevelObjectType.YourNewObject;

    public LevelObjectSaveDataEntry SaveState()
    {
        return new YourObjectSaveData
        {
            IsActivated = _isActivated
        };
    }

    public void LoadState(LevelObjectSaveDataEntry data)
    {
        if (data is YourObjectSaveData objData)
        {
            _isActivated = objData.IsActivated;
        }
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;

        // Load saved state if it exists
        var savedData = LevelState.Instance?.LoadObjectState(UniqueId);
        if (savedData != null)
        {
            LoadState(savedData);
        }
    }

    private void DoSomething()
    {
        _isActivated = true;

        // Save state
        var state = SaveState();
        LevelState.Instance?.SaveObjectState(UniqueId, state);
        SaveManager.Instance?.Save();
    }
}
```

**Step 4: Use in editor**

1. Add your object to level
2. Click "Generate Unique Id" checkbox in inspector
3. UniqueId is auto-generated and displayed
4. Save scene

**Key points:**
- Object type enum must be unique
- SaveData inherits from `LevelObjectSaveDataEntry`
- Constructor sets `ObjectType` enum
- `SaveState()` returns your custom data
- `LoadState()` restores from your custom data
- Call `LevelState.SaveObjectState()` when state changes

### How to Add a Camera Zone

1. **Create CameraSettings resource**
   - Duplicate `Resources/Data/Camera/default_camera.tres`
   - Adjust settings (zoom, offset, constraints)
   - Save as `boss_room_camera.tres`

2. **Add CameraZone to level**
   - Add Area2D node
   - Attach CameraZone.cs script
   - Add CollisionShape2D (rectangle covering the room)
   - Set collision_layer = 0, collision_mask = 1 (player layer)
   - Assign Settings = `boss_room_camera.tres`

3. **Test**
   - Walk into zone, camera should transition to new settings
   - Walk out, camera should revert to default

**Use cases:**
- Boss rooms: Zoom out, lock axes
- Tight corridors: Zoom in, increase follow offset
- Cinematic moments: Lock to specific position

---

## System Reference

### EventBus

**What it does:** Routes events between systems without direct coupling.

**When to use it:**
- Raise events: `EventBus.Instance.Raise(new HitEvent { ... })`
- Subscribe: `EventBus.Instance.Subscribe<HitEvent>(OnHit)`
- Unsubscribe: `EventBus.Instance.Unsubscribe<HitEvent>(OnHit)` (in _ExitTree)

**Event timing:**
- Immediate: Processed synchronously (use for most things)
- NextFrame: Queued until next _PhysicsProcess (use if order matters)

**Common events:**
- **HitEvent**: Something took a hit (projectile, melee, hazard)
- **DamageAppliedEvent**: Damage was calculated and applied
- **EntityDiedEvent**: Entity health reached 0
- **HazardContactEvent**: Entity touched a hazard tile
- **StatusEffectApplied/Removed**: Status effect changed

### CombatSystem

**What it does:** Calculates damage, applies modifiers.

**Flow:**
1. Listens for HitEvent
2. Checks if target is an entity (filters out non-entities)
3. Reads status effect multipliers from RuntimeData
4. Calculates final damage
5. Raises DamageAppliedEvent

**When to modify:**
- Add armor/defense calculations
- Add critical hit system
- Add damage types (physical, elemental)

### HealthSystem

**What it does:** Manages entity health, raises death event.

**Flow:**
1. Listens for DamageAppliedEvent
2. Subtracts health from RuntimeData.CurrentHealth
3. Clamps to 0
4. If 0, raises EntityDiedEvent

**When to modify:**
- Add healing events
- Add max health changes (level up, buffs)
- Add damage immunity

### StatusEffectSystem

**What it does:** Manages buffs, debuffs, damage-over-time.

**How it works:**
- Loads all StatusEffectDefinitions at startup
- ApplyEffect() adds effect to entity's RuntimeData
- Ticks all effects each frame
- Applies TickDamage via HitEvent (goes through normal damage pipeline)
- Removes expired effects

**Multipliers:**
- SpeedMultiplier: Affects entity movement (future)
- DamageMultiplier: Used by CombatSystem when calculating damage

### HazardSystem

**What it does:** Converts hazard tile collisions into damage.

**Flow:**
1. Listens for HazardContactEvent (raised by entity)
2. Looks up damage from HazardDefinition
3. Raises HitEvent with damage (SourceInstanceId = 0 for environment)

**Tile setup:**
- TileMapLayer has custom data layer: "hazard_type"
- Set to 1 = Spikes, 2 = Lava, 3 = Acid
- Entity checks tiles during movement, raises event if hazard

### SaveManager

**What it does:** Saves and loads player state, level state.

**When to save:**
- Checkpoint interaction (calls SaveManager.Instance.Save())
- Manual save from menu (future)

**What gets saved:**
- Player health, position, inventory (PlayerSaveData)
- Checkpoint activation states (LevelSaveData)
- Generic object states (LevelSaveData.ObjectStates)
- Boss defeated flags (future)
- Ability unlocks (future)

**What doesn't get saved:**
- Normal enemy positions (respawn fresh)
- Projectiles, effects (transient)

**Save files:**
- Stored in user:// directory (AppData on Windows)
- Format: .tres resources (human-readable text)
- Location: `user://saves/slot_0/`, `slot_1/`, `slot_2/`

**Important fixes:**
- Use `FileAccess.FileExists()` to check if saves exist, NOT `ResourceLoader.Exists()` (cache issues)
- Delete refreshes UI automatically via `RefreshSlotDisplay()`

### LevelState

**What it does:** Tracks level-specific state (checkpoints, flags, object states).

**Legacy Methods (still supported):**
- `ActivateCheckpoint(checkpointId)`: Mark checkpoint as activated
- `IsCheckpointActivated(checkpointId)`: Check if activated
- `UnlockDoor(doorId)`: Mark door as unlocked
- `IsDoorUnlocked(doorId)`: Check if door unlocked

**New Generic Methods (preferred):**
- `SaveObjectState(uniqueId, saveData)`: Save any object's custom state
- `LoadObjectState(uniqueId)`: Load object state (returns LevelObjectSaveDataEntry)
- `QueryObjectsByType(objectType)`: Find all saved objects of a type
- `HasObjectState(uniqueId)`: Check if object has saved state
- `RemoveObjectState(uniqueId)`: Delete saved state (e.g., object destroyed)

**Architecture:**
- `LevelSaveData.ObjectStates` is a dictionary: UniqueId → LevelObjectSaveDataEntry
- Each object type defines its own subclass (DoorSaveData, CheckpointSaveData, etc.)
- Objects call `SaveObjectState()` when their state changes
- Objects call `LoadObjectState()` in `_Ready()` to restore state

**Lifetime:** Cleared on level load, restored from save file.

### PlayerState

**What it does:** Tracks player-specific state (health, inventory, abilities).

**Methods:**
- AddCoins(amount): Increase coin count
- HasUnlock(unlockId): Check if player has ability/item
- GrantUnlock(unlockId): Give player ability/item

**Lifetime:** Persists across levels, saved to disk.

---

## Common Patterns

### The Singleton Pattern

Every global system follows this pattern:

```csharp
public static MySystem Instance { get; private set; }

public override void _Ready()
{
    if (Instance != null)
    {
        GD.PrintErr("Duplicate MySystem detected!");
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
```

**Rules:**
- Set Instance in _Ready, check for duplicates
- Clear Instance in _ExitTree (only if it's still this instance)
- Never use GetNode to find singletons
- Access via MySystem.Instance

### The Subscribe/Unsubscribe Pattern

When a node wants to listen to events:

```csharp
public override void _Ready()
{
    EventBus.Instance.Subscribe<HitEvent>(OnHit);
}

public override void _ExitTree()
{
    EventBus.Instance?.Unsubscribe<HitEvent>(OnHit);
}

private void OnHit(HitEvent evt)
{
    if (evt.TargetInstanceId != GetInstanceId()) return;
    // React to hit
}
```

**Rules:**
- Always subscribe in _Ready
- Always unsubscribe in _ExitTree
- Use null-safe operator (Instance?) in case system is already freed
- Filter events by InstanceId to only process relevant ones

### The Definition/Runtime Pattern

Entities hold both a Definition (read-only) and RuntimeData (mutable):

**Definition (CharacterDefinition.cs):**
- MaxHealth = 100
- MoveSpeed = 300
- Never changes

**RuntimeData (EntityRuntimeData):**
- CurrentHealth = 75 (player took damage)
- StatusEffects = [Poison] (temporary debuff)
- Created fresh in _Ready

**Why separate?**
- Multiple enemies share the same Definition (memory efficient)
- RuntimeData is instance-specific
- Easy to reset: just recreate RuntimeData from Definition

### The FlipRoot Pattern

All character visuals live under a FlipRoot node:

**Hierarchy:**
```
CharacterBody2D (root)
  ├─ CollisionShape2D (stays at root, no flip)
  └─ FlipRoot (Node2D)
      ├─ Sprite
      ├─ WeaponPosition
      └─ Particles
```

**Flipping:**
```csharp
bool facingRight = aimTarget.X > GlobalPosition.X;
_flipRoot.Scale = new Vector2(facingRight ? 1 : -1, 1);
```

**Result:** Everything under FlipRoot flips automatically. No need to manually flip each child.

### The Export NodePath Pattern

When a script needs to reference another node:

```csharp
[Export] private NodePath _playerPath;
private PlayerCharacterBody2D _player;

public override void _Ready()
{
    _player = GetNode<PlayerCharacterBody2D>(_playerPath);
}
```

**Why?** Allows level designers to configure references in the inspector without touching code.

**When to use:**
- Cross-entity references (HUD → Player)
- Optional child nodes (WeaponPosition)
- Editor-configurable wiring

**When NOT to use:**
- Singletons (use MySystem.Instance)
- Parent-child relationships (use GetNode directly)

### The Scene vs Definition Weapons Pattern

Entities can get weapons two ways:

**Scene Weapons (UseDefinitionWeapons = false):**
- Weapons are children in the scene file
- Good for visual positioning, testing
- Used during development

**Definition Weapons (UseDefinitionWeapons = true):**
- Weapons spawned from CharacterDefinition at runtime
- Good for data-driven enemy variants
- Used in production

**Workflow:**
1. Set UseDefinitionWeapons = false
2. Add weapons to scene, position them
3. Test, adjust positions
4. Set UseDefinitionWeapons = true
5. Assign weapons in Definition .tres

### The Inspector Button Pattern

For one-time actions in the editor (generating IDs, baking data):

```csharp
#if TOOLS
[Tool]
#endif
public partial class MyObject : Node2D
{
    [ExportGroup("Editor Tools")]
    [Export]
    private bool GenerateUniqueId
    {
        get => false;  // Always returns false so checkbox resets
        set
        {
            if (value)
            {
#if TOOLS
                if (Engine.IsEditorHint())
                {
                    // Do the action
                    UniqueId = GenerateId();
                    GD.Print($"Generated ID: {UniqueId}");
                }
#endif
            }
        }
    }
}
```

**How it works:**
- Property setter fires immediately when checkbox is clicked
- `get` returns false, so checkbox auto-unchecks after click
- `set` executes the action when value is true
- Requires `[Tool]` attribute and `Engine.IsEditorHint()` check

**Why not _Process?**
- `_Process` doesn't run reliably in editor without explicit `SetProcess(true)`
- Property setters fire instantly, more responsive
- Cleaner UX: checkbox resets automatically

**Use cases:**
- Generate unique IDs
- Bake navigation mesh
- Pre-calculate data
- Validate configuration

### The Editor vs Runtime Check Pattern

**Wrong way (doesn't work in editor play mode):**
```csharp
public override void _Ready()
{
#if !TOOLS
    // This code won't run when playing from editor!
    ConnectSignals();
#endif
}
```

**Right way (works everywhere except actual editor editing):**
```csharp
public override void _Ready()
{
    // Skip setup when editing in inspector
    if (Engine.IsEditorHint()) return;

    // This runs in play mode (F5) AND exported game
    ConnectSignals();
}
```

**Key difference:**
- `#if !TOOLS`: Compile-time check, excludes code from editor builds entirely
- `Engine.IsEditorHint()`: Runtime check, only skips when actively editing in inspector
- Play mode (F5) has `IsEditorHint() == false`, so runtime checks work correctly

**When to use each:**
- `#if TOOLS`: For editor-only utilities that should NEVER run in game
- `Engine.IsEditorHint()`: For game code that should skip during inspector editing but run in play mode

---

## Troubleshooting

### "Weapon isn't firing"

**Check:**
1. Is UseCooldown too high? Lower it for testing
2. Is WeaponDefinition assigned in inspector?
3. Is ProjectileDefinition assigned in WeaponDefinition?
4. Does weapon have a ProjectileSpawn child node?
5. Is HoldableSystem initialized? (check _Ready logs)

### "Enemy doesn't take damage"

**Check:**
1. Is entity on the correct collision layer? (layer 1 for player, layer 4 for enemies)
2. Does projectile collision mask include enemy layer?
3. Is CombatSystem autoload registered?
4. Check logs: HitEvent raised? DamageAppliedEvent raised?
5. Is entity's RuntimeData initialized? (check MaxHealth)

### "Camera isn't following player"

**Check:**
1. Is CameraController a child of the scene root?
2. Is _target NodePath set in inspector?
3. Is Camera2D a child of CameraController?
4. Check _followSpeed isn't 0

### "Checkpoint doesn't save"

**Check:**
1. Is checkpoint in "checkpoints" group? (should auto-add in _Ready)
2. Does checkpoint have a UniqueId? (not CheckpointId anymore)
3. Is SaveManager autoload registered?
4. Check user:// directory for save files
5. Is PlayerState singleton initialized?

### "Checkpoint interaction prompt doesn't show"

**Check:**
1. Is checkpoint's `Monitoring` enabled on Area2D? (should be true)
2. Are `CollisionLayer` and `CollisionMask` set correctly?
   - Checkpoint collision_mask should include player layer (layer 1)
3. Is `_Ready()` being called? Add debug prints to verify
4. Are you using `Engine.IsEditorHint()` check, NOT `#if !TOOLS`?
   - `#if !TOOLS` can block code when running from editor
   - Use runtime check instead: `if (Engine.IsEditorHint()) return;`
5. Check console for "CHECKPOINT READY" debug messages
6. Verify player is `PlayerCharacterBody2D` type (not just CharacterBody2D)

### "Delete save slot doesn't work / slot still shows as full"

**Check:**
1. Is `FileAccess.FileExists()` being used, NOT `ResourceLoader.Exists()`?
   - ResourceLoader caches resources and returns true even after deletion
   - FileAccess checks actual filesystem
2. Is `RefreshSlotDisplay()` being called after delete?
3. Check console for "Directory exists? False" message
4. Verify slot directory path is correct (user://saves/slot_X/)

### "Bullet crashes when hitting dead enemy"

**Error:** `ObjectDisposedException: Cannot access a disposed object`

**Fix:** Check if object is valid before accessing:
```csharp
protected override void OnHit(Node2D body)
{
    // Check if body is still valid (not disposed/freed)
    if (body == null || !GodotObject.IsInstanceValid(body))
    {
        return;
    }

    // Safe to access body now
    var instanceId = body.GetInstanceId();
}
```

**Why this happens:** Enemy dies and calls `QueueFree()`, but bullet's `OnHit` callback fires after the enemy is already disposed. Always check validity before accessing.

### "Event isn't firing"

**Check:**
1. Is EventBus autoload registered?
2. Are you subscribing before the event is raised?
3. Are you unsubscribing too early?
4. Is your event a struct implementing IGameEvent?
5. Add GD.Print in event handler to verify it's called

### "Weapon position is wrong after flipping"

**Check:**
1. Is weapon a child of WeaponPosition?
2. Is WeaponPosition a child of FlipRoot?
3. Does weapon's UpdateAim() handle parent flip correctly?
4. Check weapon's local position and rotation

### "Unique ID generation isn't working"

**Check:**
1. Is UniqueIdGenerator a [Tool] script?
2. Are you running in editor (not play mode)?
3. Did you check the "Generate Ids" box?
4. Did you save the scene after generating?
5. Are checkpoints/objects properly typed (inheritance)?

### "Definition changes aren't applying"

**Check:**
1. Did you save the .tres file?
2. Is entity referencing the correct .tres?
3. Are you in play mode? (reload scene after changes)
4. Check Definition vs RuntimeData (changes only apply to Definition)
5. Try reimporting the resource (Godot → reimport)

---

## Quick Reference

### File Locations

**Scripts:**
- Core systems: `Scripts/Core/` (EventBus, GameSystem)
- Events: `Scripts/Core/Events/` (CombatEvents, StatusEvents)
- Entity base: `Scripts/Entity/` (EntityCharacterBody2D, NPCEntityCharacterBody2D)
- Player: `Scripts/Player/` (PlayerCharacterBody2D, CharacterController)
- Combat: `Scripts/Combat/` (Weapons, Projectiles, Holdables)
- Systems: `Scripts/Systems/` (CombatSystem, HealthSystem)
- Data: `Scripts/Data/Definitions/` (all Definition classes)

**Resources:**
- Character defs: `Resources/Data/Characters/`
- Weapon defs: `Resources/Data/Weapons/`
- Projectile defs: `Resources/Data/Projectiles/`
- Status effects: `Resources/Data/StatusEffects/`

**Scenes:**
- Characters: `Scenes/Characters/`
- Weapons: `Scenes/Weapons/`
- Projectiles: `Scenes/Projectiles/`
- Levels: `Scenes/Levels/`

### Collision Layers

- **Layer 1:** Player (default)
- **Layer 2:** Enemies/NPCs
- **Layer 4:** Pickups (RigidBody2D)

**Projectile setup:**
- collision_layer = 0 (projectile itself has no layer)
- collision_mask = 1 + 2 (can hit player and enemies)

**Pickup setup:**
- collision_layer = 4 (physics collision)
- PickupArea collision_mask = 1 (only player can pick up)

### Autoload Order

Order matters for initialization:
1. EventBus (must be first)
2. CombatSystem
3. HealthSystem
4. HazardSystem
5. StatusEffectSystem
6. PlayerState
7. LevelState
8. SaveManager

**Rule:** If system A needs system B, B must load first.

### Naming Conventions

- **Classes:** PascalCase (CharacterDefinition, CombatSystem)
- **Files:** Match class name (CharacterDefinition.cs)
- **Scenes:** PascalCase (PlayerCharacterBody2D.tscn)
- **Resources:** snake_case (player_definition.tres, pistol.tres)
- **Events:** PascalCase + "Event" suffix (HitEvent, EntityDiedEvent)
- **Private fields:** _camelCase with underscore (_holdableSystem)
- **Public properties:** PascalCase (CurrentHealth, MaxHealth)

### Common Export Groups

Use these consistently:

```csharp
[ExportGroup("Identity")]
[Export] public string EntityId = "";

[ExportGroup("Stats")]
[Export] public float MaxHealth = 100.0f;

[ExportGroup("Visuals")]
[Export] private Node2D _flipRoot;

[ExportGroup("Combat")]
[Export] private HoldableSystem _holdableSystem;

[ExportGroup("Debug")]
[Export] private Label _debugLabel;
```

---

## Design Principles

### When to Use Events vs Direct Calls

**Use events when:**
- Multiple systems need to react independently
- You want loose coupling (caller doesn't know about listeners)
- Something crosses a system boundary

**Use direct calls when:**
- Single consumer (only one thing cares)
- Tight loop (performance critical)
- Internal to one system/entity

**Example:**
- HitEvent → event (CombatSystem, HealthSystem, UI, Audio all care)
- entity.Jump() → direct call (only entity needs to know)

### When to Create a New System vs Extending an Entity

**Create a system when:**
- Logic affects multiple entities
- Logic is stateless (pure processing)
- You want to separate concerns

**Extend entity when:**
- Logic is specific to one entity type
- Logic needs tight coupling to physics/movement
- Logic is stateful per-instance

**Example:**
- Damage calculation → System (affects all entities)
- Wall sliding → Entity (specific to movement)

### When to Create a New Definition vs Hardcoding

**Use Definition when:**
- You want multiple instances with different values
- Designers need to tweak values without code
- You might add more variants later

**Hardcode when:**
- Value never changes (Pi, tile size)
- Only used in one place
- Temporary/debug value

**Example:**
- Enemy stats → Definition (many enemy types)
- Camera shake decay rate → Hardcode (one global value)

---

## Future Expansion Points

Things that aren't implemented yet but have architectural slots:

### Ability System
- AbilityDefinition resource (double jump, dash, wall climb)
- PlayerState tracks unlocked abilities
- Doors/obstacles check for required abilities
- UI shows ability icons, cooldowns

### Inventory System
- InventoryDefinition resource (key items, quest items)
- PlayerState tracks inventory
- Interactables check for required items
- Drag-and-drop inventory UI

### Quest System
- QuestDefinition resource (objectives, rewards)
- QuestManager tracks active quests
- NPCs give/complete quests
- Event-driven: quest progress updates via events

### Dialog System
- DialogDefinition resource (NPC dialog trees)
- DialogManager handles branching choices
- Integrates with quest system
- Text box UI, typewriter effect

### Audio System
- AudioManager singleton
- Listens to events: HitEvent → impact sound, EntityDiedEvent → death sound
- Plays music based on room/situation
- Handles 2D spatial audio for projectiles

### Particle/VFX System
- VFXManager spawns effects at positions
- Listens to events: HitEvent → spark, DamageAppliedEvent → blood
- Object pooling for performance
- Camera-relative particles for UI feedback

---

**End of Programming Guide**

This document will evolve as the game grows. Add new sections as systems are implemented. Keep it human-readable and conceptual, not a code dump.
