====== Oblivion Wars ======

Main repo for the sidescroller Oblivion Wars.

Oblivion Wars is a 2D side-scrolling metroidvania with boomer-shooter elements (mouse-aimed weapons, weapon/ammo/armor pickups) and metroidvania elements (ability-based progression and gates). The game follows a lone soldier landing on an enemy planet, where the story of the planet, the enemy, and perhaps other larger foes unfolds as the player progresses through the world. There is a central hub the player can return to from checkpoints if they have unlocked the checkpoint fast travel for that location. The game contains standard metroidvania-style abilities, but also allows the player to upgrade certain stats to make the player stronger and the game easier.

**Inspirations:**
  * Super Metroid
  * Doom/Quake series (older games and newer Doom games)
  * Hollow Knight
  * Platformers such as Celeste
  * Hades / roguelike-style fights

----

====== Core Systems Design ======

===== Genre & Feel =====

2D side-scrolling metroidvania with aim-with-mouse shooter mechanics. Interconnected world with metroidvania exploration and ability gating. Combat and pacing inspired by Hollow Knight, Super Metroid, Nine Sols, and Hades. Goal is to feel arcadey and action-focused while retaining metroidvania depth. Some Celeste-style navigation challenges.

===== World & Travel =====

Interconnected world with checkpoints and fast travel between them. Enemies scale in difficulty by region, creating natural progression walls. Ammo restocks at checkpoints — no mandatory hub trip after death. Death returns player to last checkpoint with ammo depleted but all progression retained.

===== The Hub =====

Narratively significant location with characters whose dialogue and presence evolves with the story. Later becomes a mini area to explore and fight through as part of story progression. Hub is the only place to install mods and purchase upgrades. Players are pulled back organically by story, characters, and the desire to upgrade — not forced back by mechanics.

===== Weapons =====

Many weapons available. Players carry and switch between all weapons freely. Ammo capacity upgrades over the course of the game. Enemies drop ammo but full restock only at hub or checkpoints.

===== Progression — Two Tier System =====

**Permanent Upgrades:** Stat mods like strength and speed, found in special locations in the world as exploration rewards. Installed at hub, never lost. These are the metroidvania progression layer — slow, meaningful, cumulative power growth.

**Consumable Mods:** Special effect mods like double bullet, explosive rounds, gravity bullets, bouncing bullets, etc. Dropped by enemies during runs. Collected in unlimited quantity during a run and carried back to hub. Cannot be installed mid-run, only at hub. Lost on death. These are the roguelite layer — exciting, run-defining choices.

===== Mod Rarity =====

Consumable mods have rarity tiers. Common mods are less impactful, rare mods like double bullet are powerful and infrequent. Rarity naturally limits broken stacking without hard caps. Wacky physics mods (gravity bullets etc.) are encouraged and celebrated — broken runs are fun achievements.

===== The Economy — Coins =====

Dropped by enemies throughout the world. Used to purchase permanent upgrades at hub. Used to install consumable mods onto weapons at hub — this is the primary limiter on how many mods a player equips per run. Used to purchase other temporary run buffs at hub (shields, speed boosts, temporary ammo capacity increases etc.). Consumable mods can be sold for coins, creating a constant decision — install this mod or sell it for coins to spend elsewhere.

===== Hub Ritual =====

Before each push into the world the player visits the hub, reviews collected mods, decides what to install on which weapons (paying coins per installation), purchases any temporary buffs, and heads out. This feels ceremonial and preparatory — the player is intentionally loading up for the run ahead.

===== Metroidvania Integrity =====

World progression is protected by ability gates (traversal abilities like wall jump, grapple, dash etc.) not weapon power. Broken weapon loadouts make combat easier but cannot skip the game. Navigation and platforming challenges remain a core difficulty pillar regardless of weapon power.

----

====== Features ======

//(T) indicates will be in initial test world prototype//

===== Abilities =====

Abilities are permanent or temporary upgrades that modify what the player can do. This allows the developer to progress the story and world while giving the player a sense of world continuity (open world instead of level-based), which increases immersion. The metroidvania format works because of this trick to directing a player through an open world linearly without it feeling linear.

**Example progression with an ability:**
  - Player is blocked from a part of the level because they can't jump high enough
  - Player keeps exploring the parts of the world they can get to
  - Player finds a boss, can't get past until they defeat them
  - Player defeats boss
  - Player gets rewarded with the "high jump" ability
  - Player can now jump higher to get to that ledge
  - Player continues down that path and the story continues

**3 ways to toggle abilities:**
  - Debug/runtime for testing
  - Permanent pickups (saved with player)
  - Temporary (timed challenges and temp items in game — could hint at what ability is coming in the current level)

**Ability List** //(T = for sure, "default" = base ability)//

  * Jump (T) — default
  * Move left/right — default
  * Fire weapon and aim — default
  * High jump (T)
  * Wall jump (T)
  * Double jump (T)
  * Spider wall walk — player can walk up walls and ceilings on most surfaces (unless surface is specifically smooth like glass). Like gravity flip but player cannot jump while in this state (can still shoot). Less freeform version of gravity cube.
  * Infini-jump (T) — basically flying
  * Dash (T)
  * Climb (T)
  * Speed run — similar to Samus dash
  * Glide — slow fall
  * Preview aim — shows trajectory of bullets

===== Items and Pickups =====

Items can be picked up in the game. Some are permanent, some are simple pickups (health, ammo), and some are single-use holdables.

**Item types:**
  * **Pickups** — Health, Ammo, Armor
  * **Holdables** — Gravity cube (perm), Shield (perm or breakable)
  * **Quest items** — Keys, Checkpoint fast-travel unlocks (could be a microchip or something)
  * **Collectables**

===== Skills / Stats =====

Skills and stats are permanent player modifications that can be upgraded, purchased, or trained throughout the game.

**Natural Talent Point System:** Permanent upgrades to the character that help with things like steadying aim etc. Similar to Hollow Knight's charm system but using a natural talent point system to overcome "impurities."

**Examples of impurities:**
  * When running, aim could be a bit wobbly or the aim hitbox could be wider
  * Can improve these over time through talent point investment

**Skill list:**
  * Aim variability reduction
  * Laser sight (draws line to what will be hit)
  * Prediction (predict bounces)
  * Steady hands
  * Faster shooting
  * Ammo regen mods

**Armor upgrades:** Improve things like health, and/or add abilities, or help with defense.

===== Weapon Mods =====

  * Laser scope
  * Speed up bullets (even turning them into "hitscan")
  * Strength mods
  * Explosive projectiles
  * Bouncy bullets
  * Extra bullets
  * Exploding bullets
  * Different projectiles (toilets from pistol?)

**Two types of weapon upgrades:**
  * **Permanent upgrades** — level-up type mods, found throughout the world (like a "+damage" mod)
  * **Consumable mods** — lost on death, dropped from enemies with rarity tiers

===== Combat =====

**IDEA:** Bullet randomness when coming out — not same as spread but adds variation to each bullet. Related to the player's aim skill; the aim skill might tighten that variability.

**Weapons:**
  * Pistol (T) — default, similar to Quake 2's base gun, single fire
  * Shotgun (T) — spread multi-bullet
  * Machine gun (T) — rapid fire
  * Rocket launcher (T) — straight projectile, explodes on contact
  * Grenade launcher (T) — bounces off surfaces, explodes on enemy contact, timed if doesn't hit enemy
  * Laser gun (T) — constant raycast
  * Rail gun (T) — single raycast

**Other combat topics:**
  * Aim variability — bullets don't go straight toward crosshair, some randomness to simulate real fire
  * Crosshair size indicates width of variability
  * Variability can be tightened over time depending on skills
  * Gain ammo from level (NPC drops or world), based on Metroid-style ammo drops
  * FX similar to Doom where items drop out
  * Enemies could use the same weapons as player, or enemy-specific weapons
  * Enemy-specific weapons could be obtainable as secrets

----

====== World ======

Connected world with "zones" that can be loaded into memory. Each zone contains a sequence of connected rooms. Zones are also connected to other zones.

===== World Gating =====

//TODO: Cross-reference with abilities to see which abilities gate what and identify overlap.//

  * Height
  * Gap
  * Timing
  * Damage avoidance
  * Combat challenge
  * Item-based

===== Interaction =====

Pull, interact, open, speak.

===== Room Type Ideas =====

  * Movement test rooms
  * Vertical traversal rooms
  * Combat arenas
  * Ability stress-test rooms
  * Boss / pressure test rooms

===== Room Connectivity =====

  * Doors/portals are data-driven
  * One-way and conditional connections supported
  * Connections can be toggled at runtime (debug)

===== Biome Ideas =====

  * Beginning zone, bottom of hill
  * Lab / factory
  * Cliff side with cyber temple — waterfall to other side of world
  * Church (demons and cult)
  * Bottom side, leads to middle of the world
  * Middle of the world — connection back to beginning half after falling over waterfall
  * The Castle (end-game) — more like a corporation
  * The Hub — talk to characters, upgrade, etc. Gets ransacked after phase 2 of the boss (spider guy)
  * Village (rain from waterfall, other side of world)
  * City — skyscrapers and such

----

====== Ideas — Brainstorm ======

===== Enemy Scaling =====

  * As weapons get more powerful, enemies should scale too
  * Damage should FEEL crunchy the more power behind it (shotgun screenshake + extra damage is a good example)
  * Enemies should blow up into parts if damage is really high compared to their health
  * Enemies with high health don't blow up as easily
  * Weapons should continue scaling throughout game using MODs to keep progression and enemy scaling going

===== Enemy Telegraphing =====

  * Wind up before shooting
  * Shoot for a period of time
  * Stop shooting, maybe move closer etc.
  * Start shooting again with wind up

**Types of telegraph:**
  * **Animation Telegraph** — Wind-up before a punch
  * **Visual Telegraph** — Glow, flash, ground marker, laser line
  * **Audio Telegraph** — Distinct sound before an ability
  * **Spatial Telegraph** — Enemy moves into a specific position before acting

===== Tutorial Level — Airship =====

All basic elements, create a very straightforward graphical version (like "red" for damage surfaces, or a single rectangle that fades out for collapsible platforms). The graphics will be skinned on top and will depend on the level, but this lets us create basic tests without worrying about graphics.

See: [[https://www.youtube.com/watch?v=gIdHTL18kTU|Ori level design reference]]

===== Lore =====

  * Enemy is a transhuman race. They branched off humanity, or maybe humanity left them, like pilgrims, and formed their own colony away from the trans. Perhaps the player is from this colony of humanity.
  * What is the purpose of the conflict?
  * How can the story use the trans / non-trans conflict, based on Catholic morals?

===== Misc Ideas =====

  * Some item cards have the item inside the card border, others (more powerful items) might have it overlapping outside the border
  * Deflection mechanics like Nine Sols?
  * Camera follow speed slower under water — gives sense of floating, until player has water suit
  * **Adversarial game idea:** Dungeon levels up too. AI training to kill player. Each player levels up. AI and player both being "played" to create either perfect AI or perfect player for the corporation — but which one is it? This is the question that drives the story. The AI's dialogue also reflects this realization.

===== Boss Ideas =====

  * Main boss — 3 stages, second stage spider legs, 3rd stage big motherbrain
  * A boss with face similar to Doom guy face, floating around. Also opens mouth up with guns in it, similar to [[https://www.youtube.com/shorts/zX-kysK4-0A|this reference]]

===== Development Approach =====

Build a small game similar to Oblivion Wars first, with gated areas and progression:
  * Test out systems, art, etc. in a nice test environment
  * Directly applicable to Oblivion Wars
  * The AI adversarial version would focus more on procedural generation — might over-complicate things right now
  * Lets us put out a tech demo for fundraising
  * Can build out "trials" to get used to spacing and mechanics for the big game
  * Trials can go into the final version as well — could be a whole game in itself (similar to Snavi). Trials are unlocked via collectables, perhaps part of a "training" area in the hub.

----

====== Debug Tools ======

  * Grant/revoke abilities at runtime
  * Teleport to any room
  * Reset room state
  * Toggle invulnerability
  * Item toggling
  * Door toggling
  * Weapon toggling

----

====== Test World ======

===== Goals =====

Prove the core gameplay loop and combat mechanics work in a small polished test world. Create a fully playable demo similar to a small game. Have a test bed for features and abilities as they're added, as well as being able to quickly update/modify the world to test new implementations.

**Primary goals:**
  * Validate player movement & combat feel
  * Validate initial set of abilities
  * Validate room/world structure (large map similar to Hollow Knight?)
  * Validate art pipeline and style (hand-drawn → in-game)
  * Validate Godot + C# architecture (modular components, save state, etc.)
  * Identify technical debt early (enemy AI, combat)

**Non-goals (for now):**
  * No full story
  * No advanced AI variety

===== Trials =====

A series of challenges based around abilities. Each trial is self-contained and abilities are temporary. For example, a trial that tests the player's platforming ability with dash — the dash ability is picked up in the trial level and only lasts for a certain time before wearing off. These are single-level minigames similar to Snavi. Trials could be part of a "training" area in the hub, unlocked via collectables.

----

====== Task List ======

  * Fix hazards — right now there's a global hazards definition; hazards need to be case-by-case. Top-level resource that maps tile property names to enum values and maps hazard types to strings instead of ints.
  * Main menu
  * Level design with Tiled, including entry/exit
  * Get filler art in place
  * Data for enemies, weapons, etc. should be under resource files
  * Enemy AI logic (shooting bullets, etc.) — Hollow Knight used a visual node style
  * UI for raycast guns to show shooting (style similar to Arc Raiders)
  * Event system (switches, end of level, etc.)
  * Reorganize level components (camera zones, etc.) into drag/drop folders
  * Remove trail length from projectile definition — trails should only be defined in the projectile scene
  * Basic AI movement, aiming, and shooting (ground and flying)

----

====== Technical Notes ======

  * 64px tile size
  * 17 tiles high
  * 30 tiles wide

----

====== References ======

  * [[https://ozzbit-games.itch.io/action-platformer-character-template|Platformer character animation template]]
  * [[https://emanueleferonato.com/2012/05/24/the-guide-to-implementing-2d-platformers/|Overview of 2D platformer character sizes]]
  * [[http://higherorderfun.com/blog/2012/05/20/the-guide-to-implementing-2d-platformers/|Original blog — 2D platformer guide]]
  * [[https://www.youtube.com/watch?v=gIdHTL18kTU|Ori — level design]]
  * [[https://www.youtube.com/watch?v=Mw0h9WmBlsw|Skullgirls animations]]
  * [[https://ludonauta.itch.io/platformer-essentials/devlog/1069670/hollow-knight-inspired-movement-with-the-moving-character-recipe|Hollow Knight character movement recipe]]
  * [[https://www.youtube.com/watch?v=OouOhIJL1i4|Ori early animation prototype]]

===== Games for Research =====

  * **Abuse** (old DOS game) — [[https://www.youtube.com/watch?v=i6_2ZGBZ0ZE|Video]]. Sprite hand-drawn, aiming direction hand-drawn but using mouse to aim. Good animation style, especially while running and aiming backwards.
  * **Hollow Knight** — Level/world layout, Megaman-style controls including wall jumping, art style.
  * **Teeworlds** — [[https://www.youtube.com/watch?v=Ff-Pi7RD9pM|Video]]
  * **Nine Sols** — [[https://store.steampowered.com/app/1809540/Nine_Sols/|Steam page]]. Similar to Hollow Knight but with crisp hand-drawn art. Good aesthetic reference for Oblivion Wars (sketched style).
