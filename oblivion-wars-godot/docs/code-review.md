Let's enter planning mode.  Here are my notes reviewing some code:
* I don't like _hazardDefinition hanging off the character (doesn't feel right).  
* Hazard damage could happen to non-player characters too. 
Right now, there is logic being done in player like "Hazard -> ApplyHazardDamage -> StartInvincibility", but I'm wondering, Does it make since that the "StartInvincibility()" happens as a result of the HitEvent?  Maybe PlayerCharacterBody2D actually needs to listen for DamageAppliedEvents and has an OnDamageApplied that checks if damage was done and then does the invincibility thing.
* I would like to separate some of the common movementcode out of
  PlayerCharacterBody2D into an EntityCharacter2D (things like MoveLEft,
  jumping, etc).  Then derrive PlayerCharacterBody2D from that.  This allows me
  to reuse the movement code for my NPC characters when controlling them with
  AI.

  - If we do that, I think it might also make since to fold IGameEntity into
    the EntityCharacter2D.
* CharacterController.cs -> Need to make it so that the "UseHoldable" continually get's recalled as long as it's pressed.  Could have pressed/released as a flag in UseHoldable* , as well as a continue flag?  Or maybe just call UseHoldable over and over, and the code inside the UseHoldable is responsible for checking cooldown?  
* What types of things should be Events?  What types of things should not be
  events? Is it better to keep it simple and treat as much stuff as possible as
  events (like character jumping is an event), or only major things?  Will the
  NPC AI use the evnets?
  Keep in mind - SYSTEMS are the ones that do most of the heavy-lifting by
  listening to events.  Systems should be the ones actually modifying most of
  the runtime data.  The individual scenes, liek PlayerCharacterBody2D, are
  really just containers as well as Godot-specific stuff (like character
  animations/sprites, effects, movement/input code, etc).  But anything that
  affects runtime stats like health and such should be done by SYSTEMS. 
  OR... should the controllers themselves be doing this?  
  THIS IS A BIG DESIGN POINT, need to figure out now.  How much work is done in
  scenes/nodes, and how much work is done in the global systems, and how does
  this all get related in the event system??  My desire is toplevel game
  systems are global and event based, and the nodes themselves are reserved for
  mechanics of various things, as well as data storage for that thing, not for game-wide system/player state.  The systems reference the dat.
* Projectile _speed and such never gets set from the weapon definitions??
* StandardBullet - OnHit - Should deal damage if hit with another Entity, BUT
  if not it should still raise a BulletCollide event with a position so that we
  can spawn some dust or blood or something (should that be here?).  
* FireHitscan - raycast should raise a hit as well as position in case we need
  to spawn dust or blood.
* We should impement a dummy NPCEntityCharacter2D that derives from the new
  base EntityCharacter2D that has health and such (an eneme defintion) so that
  we can do traget practed.  Right now it just stays where we want it spawned.
  Once its health hits 0 it is deleted from the world.  Also no invincibility
  frames or anything like that for enemies (only for player)... this will let
  us use our new EnemyDefinition and test it!

  ------

  Point-by-Point Response
1. _hazardDefinition on the character — doesn't feel right
Agree. Hazard detection is a gameplay system concern, not a character concern. The character shouldn't know about hazard damage values. Two options:

Option A: Move hazard detection + damage lookup into a HazardSystem (autoload). It listens for a new HazardContactEvent that entities raise when they collide with a hazard tile. The system looks up damage from the HazardDefinition and raises HitEvent.
Option B: Keep hazard tile detection on the entity (it's physics-coupled — you need GetSlideCollision results), but the entity only raises a HazardContactEvent with the tile type. A HazardSystem maps type → damage and raises HitEvent.
I'd recommend Option B — the entity detects the collision (it has to, it owns the physics body), but doesn't know about damage. The HazardDefinition lives on the system.

2. Hazard damage for non-player characters too
Agree. If we extract movement code into EntityCharacterBody2D (point 4), hazard detection goes there too. Any entity with physics can detect hazard tiles. The HazardSystem handles it uniformly.

3. Invincibility as a response to DamageAppliedEvent, not inline
Agree, strongly. Right now ApplyHazardDamage() calls StartInvincibility() directly, which is wrong — it means the invincibility starts even if the damage was somehow negated by the combat system. The correct flow:


Hazard tile collision → HazardContactEvent → HazardSystem → HitEvent → CombatSystem → DamageAppliedEvent → Player listens, starts invincibility
PlayerCharacterBody2D subscribes to DamageAppliedEvent, checks if the target is itself, and triggers invincibility. This also means projectile hits would trigger invincibility too — which is correct.

4. Extract EntityCharacterBody2D base class
Agree. PlayerCharacterBody2D is ~350 lines, and most of it is reusable movement mechanics. Good candidates for the base class:

Move to EntityCharacterBody2D:

IGameEntity implementation (RuntimeData, Definition, EntityNode)
_runtimeData initialization from definition
Movement: _moveDirection, MoveLeft(), MoveRight(), Stop()
Physics: gravity application, MoveAndSlide(), velocity calculations
Jumping: Jump(), CancelJump()
Wall sliding: UpdateWallSliding(), wall jump logic
Gravity rotation: RotateGravity(), RotateGravityClockwise/CCW()
Hazard tile detection (raises event, doesn't handle damage)
Stay on PlayerCharacterBody2D:

Invincibility (player-specific — you said enemies don't get invincibility)
_holdableSystem reference and UseHoldableLeft/Right
Sprite flashing
DamageAppliedEvent subscription for invincibility
EntityDiedEvent subscription for scene reload
Wall slide dust particles (visual effect, player-specific for now)
Fold IGameEntity into EntityCharacterBody2D: Yes, makes sense. Every entity that moves and has health implements the same interface — just bake it into the base class.

5. CharacterController continuous fire
Keep it simple: call UseHoldable every frame while pressed. The holdable already has CanUse() with cooldown. The WeaponDefinition already has isAutomatic. Proposed approach:

In _PhysicsProcess (not _UnhandledInput), check Input.IsActionPressed for shoot actions and call UseHoldable* every frame
The weapon's Use() already returns early via CanUse() if on cooldown
For non-automatic weapons, check Input.IsActionJustPressed instead
This means CharacterController needs to know if the holdable is automatic — either query it, or just always call Use and let the weapon decide (simpler)
Actually, simplest: always call Use every frame while held. The Weapon checks CanUse() AND isAutomatic. If not automatic, Use() sets a flag that prevents re-firing until the button is released. CharacterController tells the player UseHoldablePressed / UseHoldableReleased, and the player passes press state to the holdable system.

6. What should be events vs. not?
This is the key architectural question. Here's my take:

Events are for cross-system communication. Use events when:

Multiple independent systems need to react to the same thing
The source shouldn't know about the consumers
It crosses system boundaries (combat → health → death → UI → audio)
Don't use events for:

Internal mechanics within a single entity (movement, input, animation triggers)
Tight-loop physics calculations
Things with only one consumer (just call the method directly)
Concrete guidance:

Thing	Event?	Why
Hit/damage/death	Yes	Multiple systems react (health, UI, audio, particles, AI)
Status effect applied/removed	Yes	Multiple systems react
Hazard contact	Yes	Crosses entity → system boundary
Player jumped	Maybe later	Could be useful for audio/particles, but not needed yet. Don't add until needed.
Movement/input	No	Internal to entity, one consumer
Animation triggers	No	Internal to entity's visual layer
NPC AI: AI controllers would replace CharacterController and call the same methods on EntityCharacterBody2D (MoveLeft, Jump, etc). AI doesn't need events for movement — it calls methods directly. AI listens to events to react (e.g., DamageAppliedEvent to trigger aggro, EntityDiedEvent to stop processing).

Systems vs. nodes: Your instinct is right. Systems own game-wide state logic (damage calculation, health tracking, status effects). Nodes own their own mechanics (movement, physics, animation, particles). Nodes raise events when something gameplay-relevant happens. Systems process those events and modify runtime data. Nodes can also listen to events to trigger visual responses (flash sprite on damage).

7. Projectile _speed never set from WeaponDefinition
Correct — intentional per the plan. _speed and _lifetime are [Export] on the Projectile scene, not on WeaponDefinition. This is because different projectile scenes (bullet, rocket, arrow) have different speeds/lifetimes as inherent properties of that projectile type. The weapon definition picks which projectile scene to spawn and sets the damage, but the projectile's own scene defines how it moves.

However, if you want per-weapon speed overrides (same bullet scene but faster for one weapon), we'd need to add optional speed/lifetime to WeaponDefinition and have Initialize() accept them. I'd defer this unless you need it now.

8. StandardBullet.OnHit — raise HitEvent + collision event
Agree. OnHit should:

If the body is an IGameEntity → raise HitEvent
Always raise a ProjectileHitEvent (new event) with position, normal, surface type — for spawning particles/effects regardless of what was hit

protected override void OnHit(Node2D body)
{
    if (body is IGameEntity || body is CollisionObject2D)
    {
        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = body.GetInstanceId(),
            SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
            BaseDamage = _damage,
            HitDirection = _direction,
            HitPosition = GlobalPosition
        });
    }

    EventBus.Instance.Raise(new ProjectileHitEvent
    {
        Position = GlobalPosition,
        Normal = -_direction,  // approximate
        HitEntityInstanceId = body.GetInstanceId()
    });
}
9. FireHitscan — same treatment
Agree. Should raise HitEvent for entities and a HitscanHitEvent for VFX.

10. Dummy NPC entity for target practice
Agree. A minimal NPCEntityCharacterBody2D that:

Extends EntityCharacterBody2D
Has [Export] EnemyDefinition _definition
Initializes EntityRuntimeData from definition in _Ready()
Subscribes to EntityDiedEvent → calls QueueFree()
No movement, no AI, no invincibility — just stands there, takes damage, dies
Placed in the test level as a target dummy
Planned Changes
Phase 1: EntityCharacterBody2D extraction
Create Scripts/Entity/EntityCharacterBody2D.cs

Move from PlayerCharacterBody2D: IGameEntity, RuntimeData, movement, jumping, wall sliding, gravity rotation, hazard tile detection (without damage logic)
Base class takes CharacterDefinition (not PlayerDefinition) — player-specific fields accessed via cast in player subclass
Hazard detection raises HazardContactEvent (new) instead of directly calling damage
Modify Scripts/Player/PlayerCharacterBody2D.cs

Inherit from EntityCharacterBody2D instead of CharacterBody2D
Keep: holdable system, invincibility, sprite flashing, death handling, wall slide dust
Subscribe to DamageAppliedEvent for invincibility (instead of inline call)
Remove: all movement code, IGameEntity boilerplate, hazard damage logic
Delete _hazardDefinition from PlayerCharacterBody2D

Phase 2: HazardSystem
Create Scripts/Systems/HazardSystem.cs

Autoload singleton
Owns [Export] HazardDefinition
Subscribes to HazardContactEvent → looks up damage → raises HitEvent
Create Scripts/Core/Events/EnvironmentEvents.cs

HazardContactEvent struct (entity instance ID, hazard type, position)
Register HazardSystem as autoload in project.godot

Phase 3: Fix weapon damage pipeline
Modify Scripts/Combat/Projectiles/StandardBullet.cs

OnHit() raises HitEvent for entities
Raises ProjectileHitEvent always (for VFX)
Modify Scripts/Combat/Weapons/Weapon.cs

FireHitscan() raises HitEvent for entities hit
Raises HitscanHitEvent always (for VFX)
Create Scripts/Core/Events/ProjectileEvents.cs

ProjectileHitEvent struct
HitscanHitEvent struct
Phase 4: Continuous fire
Modify Scripts/Player/CharacterController.cs

Move shoot handling from _UnhandledInput to _PhysicsProcess
Check Input.IsActionPressed each frame → call UseHoldable*
Holdable system / Weapon handles cooldown and isAutomatic internally
Modify Scripts/Combat/Weapons/Weapon.cs

Add tracking for non-automatic weapons: require button release before re-fire
Phase 5: NPC target dummy
Create Scripts/Entity/NPCEntityCharacterBody2D.cs

Extends EntityCharacterBody2D
[Export] EnemyDefinition for definition
Subscribes to EntityDiedEvent → QueueFree()
No movement, no AI
Create Scenes/Characters/TargetDummy.tscn

NPCEntityCharacterBody2D with a CollisionShape2D and placeholder sprite
Assigned an EnemyDefinition .tres
Create Resources/Data/Characters/Enemies/target_dummy.tres

EnemyDefinition with basic stats (100 HP, no contact damage)
Phase 6: Cleanup
Fix typo "HIT EBENT" → "HitEvent received" in CombatSystem.cs
Fix typo "ERror" in PlayerCharacterBody2D.cs
Remove unused using System.Reflection.Emit and using System.Runtime from HealthSystem.cs
Remove unused using System from CameraController.cs
Does this plan align with your vision? Any points you want to adjust before I write it up formally?

---- my response ----

1. Option A is good
2. ok
3. ok
4. ok
5. yes i like that approach. the holdable knows when press/release/holding is done . some items/holdables might require holding down button (not just weapons)
6. this is great - we should update the design document with these details.
7. Actually, I want to decouple weapon from projectile.  Let's create a projectile definition.  This will contain things like "IsRaycast", or "speed" or whatever.  The weapon definition itself contains a reference to the projectile definition.  Then we can code up different projectile scenes for the visuals, similar to how we do with the weapons.  The weapon would just spawn the projectile scene specified in the referenced projectile definition file. This lets us create bullets, trash can projectiles, whatever.  Properties, such as whether they bounce off walls and go off on a timer (like grenades), or hit immediately (like normal bullets) will all be properties of the projectile itself.  BUT the projectile has a dmage, the weapon will have a projectile damage scale parameter (default is 1.0, but different items/stats etc can modify this modifier in order to scale whatever projectile damage is coming out of the weapon without having to know which project it is - can also reuse same projectiles (for visuals/behavior) with different weapons by just modifying this damage scale parameter on the weapon definition itself.
8. Actually now that I'm thinking about it, is this really needed - the Projectile.cs is going to be the one checking collisions and raising the OnProjectileHit event, so it can mange it's own animation etc on destroy (and it will determine how it gets destroyed based on the new projectile definition we are adding).  But it should still raise a projectile hit event, maybe with the definition, in case other systems need it (for example perhaps a door responds to being hit and opens, it would look for projectile hit event on itself and open).
9. For VFX, if projectile is coded as a scene with a projectile definition, would it need to raise the HitscanHitEvent separately from HitEvent?  I think vfx would happen in the projectile scene itself ?
10. ok, but please add a debug text of the enemy's health above it's head. (ex. 50/90, similar to player's debug health text).

What do you think about these ?  Still planning, no changes yet.

---- ai response ----

Good feedback. Let me respond to each:

Point 1 (HazardSystem Option A) — Agreed, no changes.
Point 7 — ProjectileDefinition decoupling
This is a solid design. The separation makes sense:

WeaponDefinition — what the weapon does to modify output (damage scale, fire rate, spread, cooldown, which projectile to use)
ProjectileDefinition — what the projectile is (speed, lifetime, damage, bounce behavior, gravity-affected, explosion radius, is-raycast)
Projectile scene (.tscn) — what the projectile looks like and its collision shape
The DamageScale on the weapon is elegant — a shotgun and a turret can fire the same bullet scene with the same ProjectileDefinition, but the turret scales damage up via its weapon definition.

One consideration: IsRaycast on the projectile definition means raycasts and physical projectiles share the same definition type. That works well since conceptually they're both "things the weapon emits." The weapon doesn't need to know the difference — it just spawns whatever the definition says. If IsRaycast is true, the Weapon spawns a raycast-type projectile (instant hit, line trace). If false, it spawns a physical projectile scene. Both are configured via the same definition.

Proposed ProjectileDefinition:


[GlobalClass]
public partial class ProjectileDefinition : Resource
{
    [Export] public string ProjectileId = "";

    [ExportGroup("Behavior")]
    [Export] public bool IsRaycast = false;       // true = instant hit, false = physical projectile
    [Export] public float Speed = 800.0f;          // ignored if IsRaycast
    [Export] public float Lifetime = 3.0f;         // ignored if IsRaycast
    [Export] public float Damage = 10.0f;
    [Export] public bool BounceOffWalls = false;
    [Export] public int MaxBounces = 0;
    [Export] public bool AffectedByGravity = false;
    [Export] public float GravityScale = 1.0f;

    [ExportGroup("Raycast")]
    [Export] public float HitscanRange = 1000.0f;  // only if IsRaycast
    [Export] public float TrailDuration = 0.1f;    // visual trail for hitscan

    [ExportGroup("Explosion")]
    [Export] public float ExplosionRadius = 0.0f;  // 0 = no explosion
    [Export] public float FuseTime = 0.0f;         // 0 = explode on contact, >0 = timed

    [ExportGroup("Visuals")]
    [Export] public PackedScene ProjectileScene;    // the .tscn for this projectile
}
And WeaponDefinition changes:

Remove: ProjectileScene, HitscanRange, TrailDuration, ProjectileSpawnOffset
Add: [Export] ProjectileDefinition Projectile;
Add: [Export] float DamageScale = 1.0f;
Keep: ProjectileSpawnOffset (this is weapon-specific — where on the weapon the projectile spawns from)
Actually, ProjectileSpawnOffset stays on the weapon since different weapons have muzzles at different positions. Makes sense.

Point 8 — Projectile manages its own destruction/VFX
Right. The projectile scene handles its own visual effects (explosion animation, particle burst, etc.) since those are tied to the specific projectile type. The projectile:

Detects collision (via Area2D body entered, or raycast)
Raises HitEvent if it hit an IGameEntity (for the damage pipeline)
Raises ProjectileHitEvent with position + definition (for other systems — doors, breakable walls, triggers)
Plays its own destruction VFX (part of the scene — AnimationPlayer, particles, etc.)
Calls QueueFree() after VFX (or immediately if no VFX)
So ProjectileHitEvent doesn't need to be about VFX — it's about world interaction. The VFX are self-contained in the scene. The event is for systems that care about "something got hit at this location" — doors, destructible terrain, sound system, etc.


public struct ProjectileHitEvent : IGameEvent
{
    public ulong ProjectileInstanceId;
    public ulong HitEntityInstanceId;  // 0 if hit terrain/wall
    public Vector2 HitPosition;
    public Vector2 HitNormal;
    public ProjectileDefinition Definition;  // so listeners know what hit them
}
Point 9 — Do we need separate HitscanHitEvent?
No. If IsRaycast is a property of the projectile definition, then the weapon creates a "raycast projectile" that resolves instantly. It still raises the same ProjectileHitEvent. The only difference is timing (instant vs. traveled). No need for a separate event type. The listener can check evt.Definition.IsRaycast if it cares.

So the event model simplifies to:

HitEvent — an entity took a hit (feeds into combat/damage pipeline)
ProjectileHitEvent — a projectile hit something in the world (for world interactions, doors, triggers)
Both can be raised from the same collision. A single projectile hitting an entity raises both.

Point 10 — Enemy health debug text
Simple addition. The NPCEntityCharacterBody2D scene gets a Label child positioned above the sprite. Updated in _Process from the entity's RuntimeData, same pattern as DebugHUD but local to the entity. Something like:


// In NPCEntityCharacterBody2D
[Export] private Label _healthLabel;

public override void _Process(double delta)
{
    if (_healthLabel != null && _runtimeData != null)
        _healthLabel.Text = $"{_runtimeData.CurrentHealth:F0}/{_runtimeData.MaxHealth:F0}";
}
Point 5 — Holdable press/release/hold clarification
Updated approach for the Holdable API:


public abstract class Holdable : Node2D
{
    public virtual void OnUsePressed(Vector2 targetPosition) { }   // Button down
    public virtual void OnUseReleased(Vector2 targetPosition) { }  // Button up
    public virtual void OnUseHeld(Vector2 targetPosition) { }      // Called every frame while held
    // ...
}
For a weapon: OnUsePressed fires immediately (if CanUse). OnUseHeld fires again each frame (if automatic and CanUse). OnUseReleased does nothing.

For a charged item (gravity cube, bow): OnUsePressed starts charging. OnUseHeld continues charge. OnUseReleased releases the charge.

For a toggle item: OnUsePressed toggles on/off. Others unused.

The CharacterController tracks press/release state and the player entity routes to the holdable system, which calls the appropriate method each frame.

Point 6 — Update design document
I'll update the plan document with all these architectural decisions after we finalize.

Revised Plan Summary
Phase 1: EntityCharacterBody2D extraction
Create Scripts/Entity/EntityCharacterBody2D.cs — base class with IGameEntity, movement, physics, jumping, wall sliding, gravity rotation, hazard detection (raises HazardContactEvent)
Refactor PlayerCharacterBody2D to inherit from it — keeps invincibility, holdables, death reload, dust VFX
Phase 2: HazardSystem
Create Scripts/Core/Events/EnvironmentEvents.cs — HazardContactEvent
Create Scripts/Systems/HazardSystem.cs — autoload, owns HazardDefinition, listens for contact events, raises HitEvent
Register in project.godot
Phase 3: ProjectileDefinition + Weapon refactor
Create Scripts/Data/Definitions/ProjectileDefinition.cs
Update WeaponDefinition — replace ProjectileScene/HitscanRange/TrailDuration with ProjectileDefinition Projectile reference, add DamageScale
Update Projectile.cs — accept ProjectileDefinition in Initialize, use it for speed/lifetime/behavior
Update StandardBullet.cs — raise HitEvent + ProjectileHitEvent in OnHit
Update Weapon.cs — read from ProjectileDefinition, handle IsRaycast, pass damage as definition.Damage * weapon.DamageScale
Create Scripts/Core/Events/ProjectileEvents.cs — ProjectileHitEvent
Remove separate HitscanHitEvent concept — unified via ProjectileHitEvent
Phase 4: Holdable press/release/hold API
Update Holdable.cs — add OnUsePressed, OnUseReleased, OnUseHeld virtual methods
Update Weapon.cs — implement press/held for automatic vs semi-auto
Update HoldableSystem.cs — route press/release/held calls
Update PlayerCharacterBody2D — expose press/release/held to controller
Update CharacterController.cs — track press state, call held each frame in _PhysicsProcess
Phase 5: Invincibility via DamageAppliedEvent
PlayerCharacterBody2D subscribes to DamageAppliedEvent → triggers invincibility
Remove direct StartInvincibility() call from hazard code
Phase 6: NPC target dummy
Create Scripts/Entity/NPCEntityCharacterBody2D.cs — extends EntityCharacterBody2D, EnemyDefinition, dies on EntityDiedEvent, health label
Create Scenes/Characters/TargetDummy.tscn — with collision, sprite, health label
Create Resources/Data/Characters/Enemies/target_dummy.tres
Place in test level
Phase 7: Cleanup
Fix typos ("HIT EBENT", "ERror")
Remove unused usings
Create placeholder .tres files for projectile definitions (bullet, etc.)
Update design document with all architectural decisions
Does this revised plan look right? Any further adjustments before I write it into the plan file?

---- my response ----

For point 7 - i like sharing the same projectile definition for raycast and nonraycast - but internally, i think it might be even nicer if even if we are doing "raycast", it just treats it as bullet speed of infinity (i.e. uses the same bullet physical prameters, just does it instantly - this could mean actually using raycasthit instead of normal physics time updates, but I want to still treat it the same as a normal bullet just with speed of infinity, this will simplify alot.  The projectil scene design itself can use a trail regardless of raycast or not, and it should just work ).
For point 8 - Okay, actually can we merge ProjectileHitEvent and HitEvent?  Just make them like HitEvent or something, can work on both entity characters, doors, anything really.  The event itself has parameters like (ProjectileHitWith, etc) which indicate the type of hit, and damage.  This can be listend to by enemies, player, inanimate objects, etc.

Also - what is the definition of "Entity"? Is it characters?  Will other things, like levers and doors be entities, if not what will those naming schemes be (proposal?)

---- ai response ----

Good points. Let me address each:

Raycast as infinite-speed bullet
This is clean conceptually. Instead of a boolean IsRaycast, you just set Speed = 0 (or a sentinel like float.PositiveInfinity) and the weapon code detects "this is instant" and uses a raycast internally. The projectile scene still gets instantiated at the hit point for VFX (trail, impact particles). From the definition's perspective, there's no "raycast mode" — just a very fast bullet.

However, there's a practical nuance: a physical projectile uses Area2D collision detection over time, while an instant-hit uses PhysicsRayQueryParameters2D. These are fundamentally different Godot APIs. So internally the code still branches — but the definition doesn't need to know. The weapon checks if speed is infinity (or above some threshold) and picks the raycast path.

I'd propose:

Speed = 0 means "instant hit, use raycast"
Speed > 0 means "physical projectile, travel over time"
No IsRaycast bool needed
The projectile scene is still spawned either way — at origin for physical, at hit point for instant — for VFX
The trail works naturally: for a physical projectile the scene draws its own trail as it moves. For instant-hit, the weapon can spawn a brief trail effect from muzzle to hit point (or the projectile scene itself handles it in _Ready given start/end positions).

Merging ProjectileHitEvent and HitEvent
I'd push back slightly here. The issue is that HitEvent currently means "something took damage, feed it into the combat pipeline." If we merge, then every listener on HitEvent (CombatSystem, etc.) has to filter out hits on doors/levers/terrain. That adds filtering logic to every system.

Counter-proposal: keep HitEvent for the damage pipeline, but rename it to be clearer about what it is. Actually — let me reconsider. Your instinct to unify makes sense if we think about it differently:

What if HitEvent is just "something got hit by something" and the CombatSystem is the one that filters — "is the target an entity with health? If yes, calculate damage and raise DamageAppliedEvent." If not, it ignores it. Meanwhile, a DoorSystem listens to the same HitEvent and checks "is the target a door? If yes, open it."

This actually works well and is simpler. One event type, multiple systems filter by what they care about. The event just describes what happened — systems decide what to do with it.


public struct HitEvent : IGameEvent
{
    public ulong TargetInstanceId;     // What got hit (entity, door, anything)
    public ulong SourceInstanceId;     // What did the hitting (projectile, hazard, etc.)
    public float BaseDamage;           // Raw damage (0 for non-damaging hits)
    public Vector2 HitDirection;
    public Vector2 HitPosition;
    // Optional context about what caused the hit
    public ProjectileDefinition ProjectileDefinition;  // null if not a projectile hit
}
CombatSystem: "Is target an IGameEntity? Process damage."
DoorSystem: "Is target a door? Open it."
AudioSystem: "Play impact sound based on ProjectileDefinition."

This is clean. One event, many consumers. Agreed — merge them.

What is an "Entity"?
This is an important naming question. Right now IGameEntity implies "thing with health and runtime data." Let me propose a hierarchy:

Entity = anything in the game world that has an identity and can be interacted with. This includes characters, doors, levers, breakable objects, pickups.

But that's very broad. If IGameEntity means "has health, has RuntimeData, has a Definition" then doors and levers don't fit — they don't have health pools or CharacterDefinitions.

Proposal:

Term	What it is	Interface/Base	Examples
Entity	Any interactive world object with a Godot instance ID	No interface needed — everything is a Node with GetInstanceId()	Characters, doors, pickups, breakables
GameCharacter	An entity with health, stats, movement	EntityCharacterBody2D (base class, implements IGameEntity)	Player, enemies, NPCs
Interactable	An entity that responds to hits/interactions but isn't a character	IInteractable interface (or just listens to HitEvent on itself)	Doors, levers, breakable walls
The unified HitEvent works for all of them because TargetInstanceId is just a Godot instance ID. Any node can listen for HitEvent and check if it's the target.

For interactables like doors, they'd do:


// In Door.cs
EventBus.Instance.Subscribe<HitEvent>(OnHit);

private void OnHit(HitEvent evt)
{
    if (evt.TargetInstanceId != GetInstanceId()) return;
    Open();
}
No special interface needed — they just subscribe to HitEvent and self-filter. This is consistent with the event-driven architecture.

So the naming scheme becomes:

IGameEntity → rename to IGameCharacter (or keep as-is, since "entity" in gamedev typically means characters anyway)
EntityCharacterBody2D → base class for all characters
EntityRuntimeData → stays as-is, it's character runtime data
Doors/levers/interactables → plain nodes that subscribe to events, no special base class needed unless we find shared behavior later
Actually, I'd lean toward keeping IGameEntity as-is rather than renaming. "Entity" in your codebase already means "thing with health and combat stats." Doors and levers are just "nodes that listen to events." No need to over-formalize the interactable pattern yet — just let them subscribe to HitEvent. We can extract a base class later if we see repeated patterns.

Updated plan adjustments
Based on this discussion:

ProjectileDefinition — no IsRaycast bool. Speed = 0 means instant-hit (raycast). Speed > 0 means physical projectile. Projectile scene always spawned for VFX.

Unified HitEvent — single event for all hits. Add ProjectileDefinition field (nullable). Remove separate ProjectileHitEvent. CombatSystem filters for IGameEntity targets. Other systems (doors, etc.) filter for their own targets.

Entity naming — keep IGameEntity for characters with health/stats. Doors/levers are just nodes that subscribe to HitEvent. No new interfaces needed yet.

Weapon → Projectile flow for instant-hit: Weapon checks projectileDefinition.Speed == 0 → does raycast → spawns projectile scene at hit point (for VFX) → raises HitEvent. For physical: spawns projectile scene at muzzle → projectile travels → on collision raises HitEvent.

Does this all land right? If so I'll write the full plan to the plan file.


---- my response ----

Raycast as infinite-speed bullet discussion - Okay that sounds good of Raycast as infinite-speed bullet.
projectilehitevent/hitevent - YES! I like your proposal.

For IGameEntity - do we need to rename, as I mentioned earlier couldn't these just be on EntityCharacterBody2D instead of interface?

Same with IInteractable - couldnt these just be from base class InteractiableEntity ?

I think at this point, reprint out the full plan so i can review before we implement.

---- ai response ----

On the interface question — you're right, for now all damageable things will be EntityCharacterBody2D subclasses. YAGNI applies. We can remove IGameEntity and have systems check is EntityCharacterBody2D directly. If a non-character thing needs health later (destructible crate as StaticBody2D), we can extract an interface at that point.

Same logic for interactables — no interface yet, just a base class when we need one.

Let me write the full plan to the plan file.

Read C:\Users\Scyfris\.claude\plans\silly-wibbling-cosmos.md
Write C:\Users\Scyfris\.claude\plans\silly-wibbling-cosmos.md
924 lines
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

    [ExportGroup("Persistence")]
    [Export] public PersistenceMode Persistence = PersistenceMode.None;
}
```

**PlayerDefinition.cs** — Player-specific stats

```csharp
[GlobalClass]
public partial class PlayerDefinition : CharacterDefinition
{
    [ExportGroup("Movement")]
    [Export] public float JumpStrength = 800.0f;
    [Export] public float WallJumpStrength = 700.0f;
    [Export] public float DashSpeed = 600.0f;

    [ExportGroup("Physics")]
    [Export] public float Gravity = 2000.0f;
    [Export] public float WallSlideSpeedFraction = 0.5f;
    [Export] public float WallJumpPushAwayForce = 500.0f;
    [Export] public float WallJumpPushAwayDuration = 0.2f;
    [Export] public float WallJumpInputLockDuration = 0.2f;

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

**ProjectileDefinition.cs** — Defines projectile behavior (NEW)

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

**WeaponDefinition.cs** — Defines weapon behavior (UPDATED)

```csharp
[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public string WeaponId = "";
    [Export] public float UseCooldown = 0.2f;
    [Export] public bool IsAutomatic = true;        // hold to repeat fire vs. click per shot
    [Export] public float DamageScale = 1.0f;       // multiplier on projectile's base damage
    [Export] public float Knockback = 100.0f;
    [Export] public float ScreenShake = 1.5f;
    [Export] public Vector2 ProjectileSpawnOffset = new Vector2(20, 0);

    [ExportGroup("Projectile")]
    [Export] public ProjectileDefinition Projectile; // reference to projectile definition

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;            // 1 = single, >1 = shotgun
    [Export] public float SpreadAngle = 15.0f;      // total arc in degrees
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

Base class for all characters (player, enemies, NPCs). Replaces `IGameEntity` interface — systems check `is EntityCharacterBody2D` to find damageable entities.

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

    // Runtime data (replaces IGameEntity)
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

### What stays on PlayerCharacterBody2D

```csharp
public partial class PlayerCharacterBody2D : EntityCharacterBody2D
{
    // Player-specific: override _definition type to PlayerDefinition
    [Export] private new PlayerDefinition _definition;

    // Holdable system
    [Export] private HoldableSystem _holdableSystem;

    // Invincibility (player-only)
    private bool _isInvincible;
    [Export] private Node2D _spriteNode;

    // Subscribes to DamageAppliedEvent → starts invincibility
    // Subscribes to EntityDiedEvent → reloads scene

    // Visual effects
    [Export] private Node2D _wallSlideDustPosition;
    [Export] private PackedScene _wallSlideDustScene;

    // Holdable routing
    public void UseHoldablePressed(Vector2 target, bool isLeft);
    public void UseHoldableReleased(Vector2 target, bool isLeft);
    public void UseHoldableHeld(Vector2 target, bool isLeft);
}
```

### What goes on NPCEntityCharacterBody2D

```csharp
public partial class NPCEntityCharacterBody2D : EntityCharacterBody2D
{
    // Enemy-specific
    [Export] private new EnemyDefinition _definition;
    [Export] private Label _healthLabel;  // debug health above head

    // Subscribes to EntityDiedEvent → QueueFree()
    // No invincibility, no holdables, no input
    // Stationary for now (no AI controller)
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

### CombatSystem

**File:** `Scripts/Systems/CombatSystem.cs` — Autoload

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

**File:** `Scripts/Systems/HealthSystem.cs` — Autoload

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

**File:** `Scripts/Systems/HazardSystem.cs` — Autoload (NEW)

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

**File:** `Scripts/Systems/StatusEffectSystem.cs` — Autoload (stub, not implemented this phase)

- Ticks active status effects each frame
- Removes expired effects
- Provides query: `HasEffect(ulong entityId, string effectId)`

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

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
}
```

### Weapon Class

**File:** `Scripts/Combat/Weapons/Weapon.cs`

Single class for all weapon types. Behavior driven by `WeaponDefinition` + `ProjectileDefinition`.

```csharp
public partial class Weapon : Holdable
{
    [Export] private WeaponDefinition _weaponDefinition;

    private bool _hasFiredThisPress = false;  // for non-automatic weapons

    public override void OnUsePressed(Vector2 targetPosition)
    {
        _hasFiredThisPress = false;
        TryFire(targetPosition);
    }

    public override void OnUseHeld(Vector2 targetPosition)
    {
        if (!_weaponDefinition.IsAutomatic && _hasFiredThisPress) return;
        TryFire(targetPosition);
    }

    public override void OnUseReleased(Vector2 targetPosition)
    {
        _hasFiredThisPress = false;
    }

    private void TryFire(Vector2 targetPosition)
    {
        if (!CanUse()) return;
        _hasFiredThisPress = true;

        var projDef = _weaponDefinition.Projectile;
        float damage = projDef.Damage * _weaponDefinition.DamageScale;

        if (projDef.Speed == 0)
            FireInstant(targetPosition, damage);    // raycast
        else
            FireProjectile(targetPosition, damage); // physical

        ResetCooldown();
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
    // Raise HitEvent for anything that was hit
    EventBus.Instance.Raise(new HitEvent
    {
        TargetInstanceId = body.GetInstanceId(),
        SourceInstanceId = _shooter?.GetInstanceId() ?? 0,
        BaseDamage = _damage,
        HitDirection = _direction,
        HitPosition = GlobalPosition,
        Projectile = _projectileDefinition
    });

    // Projectile scene handles its own destruction VFX
}
```

### HoldableSystem

**File:** `Scripts/Combat/Holdables/HoldableSystem.cs`

Routes press/release/held calls from the character to the equipped holdables.

```csharp
public partial class HoldableSystem : Node
{
    [Export] private PackedScene _leftHoldableScene;
    [Export] private PackedScene _rightHoldableScene;

    public void PressLeft(Vector2 target)   { _leftHoldable?.OnUsePressed(target); }
    public void PressRight(Vector2 target)  { _rightHoldable?.OnUsePressed(target); }
    public void HeldLeft(Vector2 target)    { _leftHoldable?.OnUseHeld(target); }
    public void HeldRight(Vector2 target)   { _rightHoldable?.OnUseHeld(target); }
    public void ReleaseLeft(Vector2 target) { _leftHoldable?.OnUseReleased(target); }
    public void ReleaseRight(Vector2 target){ _rightHoldable?.OnUseReleased(target); }
}
```

### CharacterController Updates

**File:** `Scripts/Player/CharacterController.cs`

- `_UnhandledInput`: detect `IsActionPressed` → call `UseHoldablePressed`, detect `IsActionReleased` → call `UseHoldableReleased`
- `_PhysicsProcess`: while action is held (`Input.IsActionPressed`), call `UseHoldableHeld` every frame
- The holdable/weapon handles cooldown and automatic vs. semi-auto internally

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
  → Input.IsActionPressed("shoot") → PlayerCharacterBody2D.UseHoldableHeld(target, left)
  ↓
PlayerCharacterBody2D → HoldableSystem → Holdable.OnUsePressed/Held/Released
  ↓
Weapon.OnUseHeld()
  → IsAutomatic && hasFiredThisPress? skip (semi-auto)
  → CanUse()? → TryFire() → spawn projectile or raycast
```

---

## 8. File Structure

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
│   ├── EntityCharacterBody2D.cs   (base class: movement, physics, IGameEntity replacement)
│   └── NPCEntityCharacterBody2D.cs (enemy target dummy)
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
│   │   ├── ProjectileDefinition.cs  (NEW)
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
│   │   └── pistol.tres             (WeaponDefinition)
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
│   └── TargetDummy.tscn            (NEW)
├── Weapons/
│   └── Pistol.tscn                 (Weapon node with visual)
├── Projectiles/
│   └── StandardBullet.tscn         (projectile scene)
└── Levels/
    ├── MainLevel.tscn
    └── TestLevel.tscn
```

---

## 9. Implementation Order

### Phase 1: EntityCharacterBody2D extraction
1. Create `Scripts/Entity/EntityCharacterBody2D.cs` — extract movement, physics, jumping, wall sliding, gravity rotation, hazard detection (raises HazardContactEvent), RuntimeData from PlayerCharacterBody2D
2. Refactor `PlayerCharacterBody2D` to extend `EntityCharacterBody2D` — keep invincibility, holdables, death reload, dust VFX, sprite flashing
3. Remove `IGameEntity` interface — replace all `is IGameEntity` checks with `is EntityCharacterBody2D`
4. Delete `Scripts/Core/IGameEntity.cs`

### Phase 2: HazardSystem + invincibility fix
5. Create `Scripts/Core/Events/EnvironmentEvents.cs` — HazardContactEvent struct
6. Create `Scripts/Systems/HazardSystem.cs` — autoload, owns HazardDefinition, listens HazardContactEvent → raises HitEvent
7. Register HazardSystem as autoload in project.godot
8. Update `PlayerCharacterBody2D` — subscribe to DamageAppliedEvent for invincibility (instead of inline StartInvincibility call). Remove _hazardDefinition export.
9. Update `EntityCharacterBody2D` — hazard detection raises HazardContactEvent instead of HitEvent

### Phase 3: ProjectileDefinition + weapon refactor
10. Create `Scripts/Data/Definitions/ProjectileDefinition.cs`
11. Update `WeaponDefinition.cs` — replace ProjectileScene/HitscanRange/TrailDuration with ProjectileDefinition reference, add DamageScale, rename isAutomatic → IsAutomatic
12. Update `Projectile.cs` — accept ProjectileDefinition in Initialize, use it for speed/lifetime
13. Update `StandardBullet.cs` — raise HitEvent in OnHit
14. Update `Weapon.cs` — read from ProjectileDefinition, handle Speed==0 as instant-hit (raycast), calculate damage as projDef.Damage * weapon.DamageScale, raise HitEvent for instant-hit
15. Update `HitEvent` struct — add ProjectileDefinition field

### Phase 4: Holdable press/release/held API
16. Update `Holdable.cs` — replace abstract Use() with virtual OnUsePressed/OnUseReleased/OnUseHeld
17. Update `Weapon.cs` — implement OnUsePressed (fire + set flag), OnUseHeld (check IsAutomatic), OnUseReleased (clear flag)
18. Update `HoldableSystem.cs` — route press/release/held calls instead of single Use()
19. Update `PlayerCharacterBody2D` — expose press/release/held methods for holdables
20. Update `CharacterController.cs` — track press state in _UnhandledInput, call held each frame in _PhysicsProcess

### Phase 5: NPC target dummy
21. Create `Scripts/Entity/NPCEntityCharacterBody2D.cs` — extends EntityCharacterBody2D, EnemyDefinition, subscribes EntityDiedEvent → QueueFree, health label above head
22. Create `Scenes/Characters/TargetDummy.tscn` — NPCEntityCharacterBody2D with CollisionShape2D, placeholder sprite (ColorRect), Label for health
23. Create `Resources/Data/Characters/Enemies/target_dummy.tres` — EnemyDefinition (100 HP)
24. Create `Resources/Data/Projectiles/bullet.tres` — ProjectileDefinition for standard bullet
25. Update existing weapon .tres to reference bullet.tres ProjectileDefinition
26. Place target dummy in TestLevel.tscn

### Phase 6: Cleanup
27. Fix typo "HIT EBENT" → "HitEvent received" in CombatSystem.cs
28. Fix typo "ERror" in PlayerCharacterBody2D.cs (will be in EntityCharacterBody2D after extraction)
29. Remove unused usings: System.Reflection.Emit, System.Runtime from HealthSystem.cs; System from CameraController.cs
30. Update design document with final architecture

### Verification

After each phase, run `dotnet build` and verify zero errors. After Phase 5:
1. Run game — player moves, jumps, wall slides as before
2. Left click fires pistol → projectile hits target dummy → damage applied → health label updates
3. Target dummy at 0 HP → disappears (QueueFree)
4. Walk into spikes → HazardContactEvent → HazardSystem → HitEvent → damage → invincibility flashing
5. Player at 0 HP → scene reloads

---

## 10. What Is and Isn't an Event (Design Guidelines)

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
The full plan is written to the plan file. Ready for your review.


