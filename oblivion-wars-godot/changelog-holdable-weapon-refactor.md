# Holdable / Weapon System Refactor

## Overview

Refactored the holdable and weapon system from a subclass-per-weapon-type hierarchy (`ProjectileWeapon`, `HitscanWeapon`) into a single data-driven `Weapon` class. Weapon behavior is now determined entirely by `WeaponDefinition` resource data. Added dual holdable slots (left/right) mapped to left/right mouse buttons.

## Architecture

```
HoldableSystem (Node, on player/NPC)
├── Manages left + right holdable slots
├── Instantiates holdable scenes from exported PackedScenes
├── Routes Use() from input (left click → left slot, right click → right slot)
│
Holdable (abstract base, Node2D)
├── Weapon : Holdable
│   ├── [Export] WeaponDefinition (assigned in weapon .tscn scene)
│   ├── Use() reads definition data to determine behavior:
│   │   ├── ProjectileScene != null → projectile weapon
│   │   │   ├── SpreadCount > 1 → spawn multiple in spread pattern
│   │   │   └── else → spawn single projectile
│   │   └── ProjectileScene == null → hitscan raycast
│   └── Single class handles all weapon types via data
│
└── Item : Holdable (future)
    └── Behavior determined by data, not subclasses
```

### Weapon Scene Structure

```
Weapon (Node2D, script: Weapon.cs)
├── [Export] WeaponDefinition = weapon.tres
├── ColorRect / Sprite2D (weapon visual)
├── AnimationPlayer (future — shoot, equip, idle)
├── BulletTrail (Line2D, optional — for hitscan)
└── ProjectileSpawn (Node2D position marker)
```

Scenes are self-contained and testable in the editor.

## Changes

### Modified Files

#### `Scripts/Data/Definitions/WeaponDefinition.cs`
- Added `SpreadCount` (int) and `SpreadAngle` (float) export fields for multi-shot/shotgun support

#### `Scripts/Combat/Holdables/Holdable.cs`
- Changed base class from `Node` → `Node2D` (holdables are positioned in world space)
- Renamed `Initialize(Node2D owner)` → `InitOwner(Node2D owner)`
- Added `virtual OnEquip()` / `OnUnequip()` lifecycle hooks

#### `Scripts/Combat/Weapons/Weapon.cs`
- Collapsed `ProjectileWeapon` + `HitscanWeapon` into single concrete class
- `Use()` checks `_weaponDefinition.ProjectileScene != null` to pick projectile vs hitscan path
- `FireProjectile()` supports spread: distributes `SpreadCount` projectiles evenly across `SpreadAngle`
- `FireHitscan()` performs raycast, shows bullet trail via optional `Line2D` child
- Screen shake applied via `CameraController` tree lookup

#### `Scripts/Combat/Holdables/HoldableSystem.cs`
- Rewritten for left/right dual slots
- Two exported `PackedScene` fields: `_leftHoldableScene`, `_rightHoldableScene`
- `Initialize(Node2D owner)` instantiates scenes, adds as children, calls `InitOwner()`
- `UseLeft()` / `UseRight()` route to respective holdables
- `SwapLeft(PackedScene)` / `SwapRight(PackedScene)` for runtime weapon swapping
- Removed old array-cycling logic (`NextHoldable`, `SwitchHoldable`)

#### `Scripts/Player/CharacterController.cs`
- Replaced single `_useAction` with `_useLeftAction` ("shoot") and `_useRightAction` ("shoot_right")
- Removed `_switchHoldableAction` (no more weapon cycling)
- Left click → `UseHoldableLeft()`, right click → `UseHoldableRight()`

#### `Scripts/Player/PlayerCharacterBody2D.cs`
- Replaced `UseHoldable()` / `NextHoldable()` with `UseHoldableLeft()` / `UseHoldableRight()`
- Added `_holdableSystem.Initialize(this)` call in `_Ready()`

#### `project.godot`
- Added `shoot_right` input action mapped to right mouse button (button_index 2)

#### `Scenes/Characters/PlayerCharacterBody2D.tscn`
- Removed old `ProjectileWeapon` and `HitscanWeapon` child nodes from HoldableSystem
- HoldableSystem now has `_leftHoldableScene = Pistol.tscn` and `_rightHoldableScene = Shotgun.tscn`

### Deleted Files

- `Scripts/Combat/Weapons/ProjectileWeapon.cs` — logic merged into `Weapon.cs`
- `Scripts/Combat/Weapons/HitscanWeapon.cs` — logic merged into `Weapon.cs`

### Created Files

#### `Resources/Data/Weapons/pistol.tres`
- `WeaponDefinition` resource: single projectile, Damage=10, UseCooldown=0.2, SpreadCount=1

#### `Resources/Data/Weapons/shotgun.tres`
- `WeaponDefinition` resource: spread projectile, Damage=5, UseCooldown=0.6, SpreadCount=5, SpreadAngle=30, ScreenShake=3

#### `Scenes/Weapons/Pistol.tscn`
- Weapon scene with blue `ColorRect` placeholder and `ProjectileSpawn` marker
- References `pistol.tres` definition

#### `Scenes/Weapons/Shotgun.tscn`
- Weapon scene with red `ColorRect` placeholder and `ProjectileSpawn` marker
- References `shotgun.tres` definition

## Runtime Flow

1. `PlayerCharacterBody2D._Ready()` calls `_holdableSystem.Initialize(this)`
2. `HoldableSystem` instantiates `Pistol.tscn` (left) and `Shotgun.tscn` (right) from exported PackedScenes
3. Each weapon's `_Ready()` reads its `WeaponDefinition` to set cooldown
4. `InitOwner()` finds `CameraController` for screen shake
5. Left click → `HoldableSystem.UseLeft()` → `Weapon.Use()` → checks definition → `FireProjectile()` (single shot)
6. Right click → `HoldableSystem.UseRight()` → `Weapon.Use()` → checks definition → `FireProjectile()` (5-pellet spread)

## Verification

- `dotnet build` succeeds with 0 errors, 0 warnings
- Left click fires single pistol projectile
- Right click fires 5 shotgun pellets in 30° spread
- Screen shake applies on fire
- Projectiles ignore shooter collision
