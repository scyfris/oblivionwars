# oblivionwars

Main repo for the sidescroller Oblivion Wars 

===== Oblivion Wars / Side scroller shooter =====

Oblivion wars is a side-scrolling metroidvania being developedl.  The game
features boomer-shooter elemetns (mouse used to aim weapons and
weapon/ammo/armor pickups), as well as metroidvania elements (ability-based
progression system and gates).  The game revlovles around a story of a lone
soldier landing on an enemy plant, and the story of the planet, the enemy, and
perhaps other larger foes unfold as the player progresses through the world.
There is a central hub that the player can go back to from checkpoints if they
have unlocked the checkpoint fast travel for that particular location.  The
game contains standard metroidvania-style abilities, but also allows the player
to upgrade certain stats to make the player stronger and the game easier.

Inspirations for game:
  * Super Metroid
  * Doom/quake series (older games, as well as newer doom games)
  * Hollow Knight
  * Platformers such as celeste
  * Hades/roguelike style fights.

==== Features  (T indicates will be in initial test world prototype) ====

=== Abilities===

Abilities are permenant or tempory upgrades that modify what the player can do
during the game.  This allows us (the developer) to have a way to progress the story and world
while still giving htep layer a sense of world continuity (i.e. open world
instead of level-based) which helps increase immersion.  The whole reason
metroidvania works so well is because of this trick to directing a player
through an open world linearly without it feeling linear.

Example progression with an ability:
  * Player is blocked from a part of the level because they can't jump high enough for example.
  * Player keeps exploring the parts of the world they can get to.
  * Player finds a boss, can't get past him until they defeat him.
  * Player defeats boss.
  * Player gets rewarded with the "high jump" ability.
  * Player can now jump higher to get to that one ledge
  * Player continues down that path, and the story continues.


3 ways to toggle abilityes:
  * Debug/runtime for testing
  * Permenant pickups (perm, saved with player)
  * Temp (timed challenges and temp items in game, could be used to hint at
    what ability is coming in the current level...)
  * Unlocked (perm)

== Ability List ==

Anthing with (T) is for sure.  Anything with "default" is a base ability that
doesn't need to be picked up.

  * Ability pickups (perm or temp) 
    * Jump (T) - default
    * Move left/right - default
    * Fire weapon and aim - default
    * High jump (T)
    * Wall jump (T)
    * Double Jump (T)
    * Infini Jump (T)
      * Basically flying
    * Dash (T)
    * Climb (T)
    * Speed run
      * Similar to Samus dash
    * Glide
      * Slow fall

=== Items and pickups ===

Items can be picked up in the game, some items can be permanent (don't get
used), some items could be simple pickups (health, ammo), and some items could
be single-use holdables etc.

== Item types ==

  * Pickups
    * Health
    * Ammo
    * Armor
  * Holdables
    * Gravity cube (perm)
    * Shield (perm or breakable)
  * Quest items
    * Keys
    * Checkpoint fast-travel unlocks (could be a microchip or something, we'll
      see).
    * Other - depends on level/world design.
  * Collectables

=== Skills / Stats ===

Skills and stats are permanent player modifications that can be upgraded,
purchased, or trained throughout the game.  These 

  * Aim variability reduction

=== Combat ===

  * IDEA: Bullet randomness when coming out, not same as spread but adds a bit of variation to each bullet coming out.
    * This is related to the player's aim skill, the aim skill might tighten that variability - but for now just keep it random and parameterizable.
    * TODO: Figure out if ray cast or projectile is better (or mix of both?)
  
== Weapons ==

  * Pistol (T) - default (similar to Quake 2's base gun)
    * Single fire

  * Shotgun (T)
    * Spread multi bullet

  * Machine gun (T)
    * Rapid fire

  * Rocket launcher (T)
    * Projectile - straight, explodes on contact

  * Grenade launcher (T)
    * Projectile - bounces off surfaces, explodes on contact on enemies, timed if doesn't explode on enemy.

  * Lazer gun (T)
    * Constant raycast

  * Rail gun (T)
    * Single raycast

== Other Topics ==

  * Aim variability - pullets don't go straight towards crosshair, there is
    some randomness to similate real fire.
    * The crosshair size indicates width of the variability ?
    * Variability can be tightend over time depending on skills.

  * Gain ammo from level (either NPC drops or the world)
    * TODO: Figure out how ammo is obtained
      * Can base it on metroid where NPCs drop ammo for weapons the player has
        already obtained, with certain rarities associated
        * IDEA: FX similar to doom where items drop out , I like that.
      * Or could be designed drops in the world
      * Or recharge statges

  * Enemies could be using the same weapons as player, or enemy specific
    weapons.

  * IDEA: Enemy specific weapons could be obtainable in the game as secrets?


=== World ===

  * Connected world, with "zones" that cane be loaded into memory.  Each zone contains a sequence of connected rooms.  The zone itself is also connected to other zones.

== World gating ideas ==

TODO: Move this up to abilities, maybe what kinds of things they gate.
TODO: Actually enumerating these is good - it lets me see which abilities gate
what so that I can know if there is some overlap.

  * Height
  * Gap
  * Timing
  * Damage avoidance
  * Combat challenge
  * Item-based 
  * etc.

== Interaction with World ==
  * Interaction
    * Pull, interact, open, speak

== Room Type ideas ==
  * Movement test rooms
  * Vertical traversal rooms
  * Combat arenas
  * Ability stress-test rooms
  * Boss/pressure test rooms

== Room Connectivity ==
  * Requirements
  * Doors/portals are data-driven
  * One-way and conditional connections supported
  * Connections can be toggled at runtime (debug)

=== Debug tools ===

  * Grant/revoke abilities at runtime
  * Teleport to any room
  * Reset room state
  * Toggle invulnerability
  * Item toggling
  * Door toggling
  * Weapon toggling, etc

=== Test World ===

== Goals ==

This should let me know if thigns are okay to proceed. I will need to get
feedback from others for this to know how everything "feels" and what needs to
be tweaked/modified.

Primary goals:
  * Prove the core gameplay loop and combat mechanics work in a small polished test world
  * Create a fully playable demo, similar to a small game.
  * Have a test for testing features and abilities as I add them, as well as
    being able to quickly update/modify the world to test new impementation
    features.

Other goals:

  * Validate player movement & combat feel
  * Validate initial set of abilities
    * TODO: Choose which ones for scope of this test world.
  * Validate room/world structure
    * Best way should fit together - large map similar to Hollow knight?
  * Validate art pipeline and style (hand-drawn → in-game)
  * Validate Godot + C# architecture
    * Modular components
    * Save state
    * etc.
  * Identify technical debt early
  * Enemy AI and combat.
 
Non-Goals (for now)

  * No full story
  * No advanced AI variety


==== Task List ====

 * Top-level resource that maps tile property names to enum value (enum used in code, tile property used in tilesets).
  * I think also the same toplevel resource that maps hazard types to strings instead of ints, makes it easier.
  * Main Menu
  * Level design with TILED, including entry/exit
  * Get some filler art in there
  * Data for enemies, weapons, etc should be under resource files (or txt config files)
    * How to do AI logic for enemies?  Hallow knight had kind of a visual node style.
  * Enemies and ai (shooting bullets, etc)
    * UI for raycast guns to show shooting, think of style similar to Arc Raiders
  * An event system (switches, end of level, etc).
  * Fix camera to use a shared Resource for properties.  Camera and camera
    trigger ares just reference the resource instead of having shared
    parameters.

=== Testworld checklist ===

  * Main menu
  * Checkpoint which player can use to save progress
  * Interaction mechanics with checkpoint as well as on-screen GUI (ex. Press "E" to interact)
  * Saving state (need to work through the requirements here)
  * 1 Enemy NPC that walks for now.  Can be extended later with flying NPCs, wall crawlers, etc, but right now I want to get basic AI movement and shooting down.
  * A little gui icon that shows in bottom right when saving for player feedback that it's been saved (even if it saves fast, gui icon should play some little animation for 1 sec or something).
  * Collectable coins to get from npc.  
  * GUI to indicate colelctable coins
  * Death - when player's health is 0, loads from last checkpoint save data,
    include position.


==== Features ====

 
  * Reorganize so all the level components, such as a camerazone, can just be in a folder that can be dragged/dropped from to build the leve.
  * Remove traillength from projectile definition.  TRails should only be defined in the projectile scene.
  * Basic AI movement and aiming and shooting, nothing fancy.  Ground ai and flying.


  * Trials - a series of challenges based around the abilities, but instead of a progression system each trial is self-contained and the abilities are temorary.  For example, there coudl be a trial that tests the players platforming ability with dash.  The dash ability is an item picked up by the player in hte trial level and only lasts for a certain amount of time before wearing off, they need to get through the trial before it wears off.  In this way, these are single-level minigames, similar to Snavi.



==== Resources ====

  * Platformer character animation template
    * https://ozzbit-games.itch.io/action-platformer-character-template
  * Overview of 2d platformer character sizes and such
    * https://emanueleferonato.com/2012/05/24/the-guide-to-implementing-2d-platformers/
    * Smae thing from original blog: http://higherorderfun.com/blog/2012/05/20/the-guide-to-implementing-2d-platformers/

==== Ideas ====
  * Have been thinking about how to start development, with oblivion wars or the ai game.  I'm thinking to do this:
    * Build a small game similar to oblivion wars, with gated areas and progression.
    * This lets you test out systems, art, etc in a nice test environment.
    * This will directly be applicable to oblivion wars.
    * The AI version will focus more on procedural generation, which might over-complicate things right now.
    * This will also let me put out a tech demo for fundraising or whatever.
    * Can build out "trials" to get used to spacing and mechanics for the big game.  Trials can go into the final version as well.
      * Could be a whole game in itself (similar to Snavi), mechanics same.  But will be part of Oblivion Wars (trials open up as the player goes through the game, maybe trials are unlocked via collectables.  Perhaps it can be part of the "training" area in the hub.

  * Natural talent point system
    * perm upgrades to character, helps with things like steadying aim etc.
  * Impurities (similar to Hallow knight, they use charms to overcome but we could use a natural talent point system)
    * Example: when running , the aim could be a bit wobbly, or the aim hitbox could be wider.  Can do a bunch of things like that that you can improve over time.
  * Armor upgrades - Improves things like health, and/or adds abilities, or just helps with defense ?
  * Perm upgrades - abilities (to get through levels and such , such as double/infini-jump, etc)
  * — other game idea Adverserial - dungeon levels up too. AI training to kill player. Each player levels up. Ai and player both being “played” to create either perfect AI or perfect player for the corporation - but which one is it? This is the question that drives the story - the AI’dialogue also reflects this realization
  * Idea: Camera follow speed slower under water - gives sense of floating in water, until player has water suit.


==== References ====

  * Hallow Knight character movement recipe
    * https://ludonauta.itch.io/platformer-essentials/devlog/1069670/hollow-knight-inspired-movement-with-the-moving-character-recipe#:~:text=Note%20that%20the%20movement%20is,in%20contrast%20with%20Mario%20Bros.

==== Games like it for research ====

  * Abuse (old DOS game)
    * https://www.youtube.com/watch?v=i6_2ZGBZ0ZE
    * Sprite hand drawn, the aiming direction hand drawn but using mouse to aim.  I like the animation stule, especially while running and aiming backwards, etc.
  * Hallow Knight
    * So much to love - the level/world layout
    * Controls (they implemented megaman style controls), including the wall jumping etc.
    * Art - love
  * Teeworld
    * https://www.youtube.com/watch?v=Ff-Pi7RD9pM
  * Nine sols
    * https://store.steampowered.com/app/1809540/Nine_Sols/
    * Similar to Hallow Knight , but the art is crispt hand drawn , I really like the aesthetic for maybe oblivion wars (although will be different since maybe sketched)


