using Godot;

/// <summary>
/// Centralized group names for Oblivion Wars.
/// Use these constants instead of hardcoded strings to avoid typos and enable refactoring.
/// Add groups to nodes via Inspector -> Node -> Groups, then reference them here in code.
/// </summary>
public static class Groups
{
    // ========================================
    // ENTITIES
    // ========================================

    public static class Entities
    {
        public const string Player = "player";
        public const string Enemy = "enemy";
        public const string EnemyGround = "enemy_ground";
        public const string EnemyFlying = "enemy_flying";
        public const string Boss = "boss";
        public const string NPC = "npc";
    }

    // ========================================
    // COMBAT
    // ========================================

    public static class Combat
    {
        public const string Projectile = "projectile";
        public const string PlayerProjectile = "player_projectile";
        public const string EnemyProjectile = "enemy_projectile";
        public const string Explosion = "explosion";
        public const string Damageable = "damageable";
        public const string Hurtbox = "hurtbox";
        public const string Hitbox = "hitbox";
    }

    // ========================================
    // PICKUPS
    // ========================================

    public static class Pickups
    {
        public const string Pickup = "pickup";
        public const string WeaponPickup = "weapon_pickup";
        public const string AmmoPickup = "ammo_pickup";
        public const string HealthPickup = "health_pickup";
        public const string ArmorPickup = "armor_pickup";
        public const string AbilityPickup = "ability_pickup";
        public const string ItemPickup = "item_pickup";
        public const string QuestItem = "quest_item";
        public const string Coin = "coin";
    }

    // ========================================
    // LEVEL ELEMENTS
    // ========================================

    public static class Level
    {
        public const string Door = "door";
        public const string Checkpoint = "checkpoint";
        public const string SpawnPoint = "spawn_point";
        public const string Hazard = "hazard";
        public const string HazardTile = "hazard_tile";
        public const string Platform = "platform";
        public const string MovingPlatform = "moving_platform";
        public const string CrumblingPlatform = "crumbling_platform";
        public const string OneWayPlatform = "one_way_platform";
        public const string Trigger = "trigger";
        public const string InteractiveObject = "interactive_object";
    }

    // ========================================
    // CAMERA & ZONES
    // ========================================

    public static class Camera
    {
        public const string CameraZone = "camera_zone";
        public const string CameraBounds = "camera_bounds";
        public const string NoShakeZone = "no_shake_zone";
    }

    // ========================================
    // SYSTEMS
    // ========================================

    public static class Systems
    {
        public const string Saveable = "saveable";
        public const string Themeable = "themeable";
        public const string Persistent = "persistent";
        public const string DontSave = "dont_save";
    }

    // ========================================
    // UI & HUD
    // ========================================

    public static class UI
    {
        public const string HUD = "hud";
        public const string Menu = "menu";
        public const string Dialog = "dialog";
        public const string Tooltip = "tooltip";
    }

    // ========================================
    // UTILITY METHODS
    // ========================================

    /// <summary>
    /// Get all group names as an array (useful for debugging/editor tools)
    /// </summary>
    public static string[] GetAllGroups()
    {
        return new[]
        {
            // Entities
            Entities.Player,
            Entities.Enemy,
            Entities.EnemyGround,
            Entities.EnemyFlying,
            Entities.Boss,
            Entities.NPC,

            // Combat
            Combat.Projectile,
            Combat.PlayerProjectile,
            Combat.EnemyProjectile,
            Combat.Explosion,
            Combat.Damageable,
            Combat.Hurtbox,
            Combat.Hitbox,

            // Pickups
            Pickups.Pickup,
            Pickups.WeaponPickup,
            Pickups.AmmoPickup,
            Pickups.HealthPickup,
            Pickups.ArmorPickup,
            Pickups.AbilityPickup,
            Pickups.ItemPickup,
            Pickups.QuestItem,
            Pickups.Coin,

            // Level
            Level.Door,
            Level.Checkpoint,
            Level.SpawnPoint,
            Level.Hazard,
            Level.HazardTile,
            Level.Platform,
            Level.MovingPlatform,
            Level.CrumblingPlatform,
            Level.OneWayPlatform,
            Level.Trigger,
            Level.InteractiveObject,

            // Camera
            Camera.CameraZone,
            Camera.CameraBounds,
            Camera.NoShakeZone,

            // Systems
            Systems.Saveable,
            Systems.Themeable,
            Systems.Persistent,
            Systems.DontSave,

            // UI
            UI.HUD,
            UI.Menu,
            UI.Dialog,
            UI.Tooltip,
        };
    }

    /// <summary>
    /// Print all groups to console (debugging)
    /// </summary>
    public static void PrintAllGroups()
    {
        GD.Print("=== Oblivion Wars Groups ===");
        foreach (var group in GetAllGroups())
        {
            GD.Print($"  - {group}");
        }
    }
}
