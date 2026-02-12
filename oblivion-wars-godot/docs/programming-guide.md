# Oblivion Wars — Programming Guide

**Last Updated:** February 2026

This guide explains the game's architecture, how systems work together, and how to add new features. Use this when you've been away from the project and need to remember how everything flows.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Concepts](#core-concepts)
3. [How Events Flow](#how-events-flow)
4. [Adding New Features](#adding-new-features)
5. [System Reference](#system-reference)
6. [Common Patterns](#common-patterns)
7. [Troubleshooting](#troubleshooting)

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
   - Add `UniqueIdGenerator` node to scene (temporarily)
   - Check "Generate Ids" in inspector
   - Save scene (Ctrl+S)
   - Remove UniqueIdGenerator node

4. **Test**
   - Run level, interact with checkpoint
   - Die and verify respawn at correct position

**Important:** Don't manually set CheckpointId anymore. Use UniqueId and the generator tool. This prevents duplicate IDs.

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
- Player health, position, inventory
- Checkpoint activation states
- Boss defeated flags (future)
- Ability unlocks (future)

**What doesn't get saved:**
- Normal enemy positions (respawn fresh)
- Projectiles, effects (transient)

**Save files:**
- Stored in user:// directory (AppData on Windows)
- Format: .tres resources (human-readable text)

### LevelState

**What it does:** Tracks level-specific state (checkpoints, flags).

**Methods:**
- ActivateCheckpoint(checkpointId): Mark checkpoint as activated
- IsCheckpointActivated(checkpointId): Check if activated
- SetFlag(flagId, value): Set arbitrary flag (door opened, boss defeated)

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
2. Does checkpoint have a CheckpointId? (or UniqueId)
3. Is SaveManager autoload registered?
4. Check user:// directory for save files
5. Is PlayerState singleton initialized?

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
