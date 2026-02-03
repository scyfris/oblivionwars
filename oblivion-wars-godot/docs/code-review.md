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
