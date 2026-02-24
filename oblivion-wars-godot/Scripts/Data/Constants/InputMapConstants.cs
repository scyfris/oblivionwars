/// <summary>
/// Centralized input action names matching project.godot input map.
/// Use these constants instead of hardcoded strings.
/// </summary>
public static class InputMapConstants
{
    // ── Movement ─────────────────────────────────────────────
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Jump = "jump";

    // ── Combat ───────────────────────────────────────────────
    public const string Shoot = "shoot";
    // Right click , doesn't mean shooting to the right...
    public const string ShootRight = "shoot_right";
    public const string SwitchWeapon = "switch_weapon";
    public const string WeaponNext = "weapon_next";
    public const string WeaponPrev = "weapon_prev";
    public const string WeaponSlot1 = "weapon_slot_1";
    public const string WeaponSlot2 = "weapon_slot_2";
    public const string WeaponSlot3 = "weapon_slot_3";
    public const string WeaponSlot4 = "weapon_slot_4";
    public const string WeaponSlot5 = "weapon_slot_5";
    public const string WeaponSlot6 = "weapon_slot_6";
    public const string WeaponSlot7 = "weapon_slot_7";

    // ── Interaction ──────────────────────────────────────────
    public const string Interact = "interact";

    // ── Gravity ──────────────────────────────────────────────
    public const string RotateGravityCW = "rotate_gravity_cw";
    public const string RotateGravityCCW = "rotate_gravity_ccw";

    // ── Debug ────────────────────────────────────────────────
    public const string DebugMenu = "debug_menu";
    public const string DebugModeToggle = "debug_mode_toggle";
}
