using Godot;

[GlobalClass]
public partial class WeaponDefinition : Resource
{
    // Unique ID string assigned by the designer.
    [Export] public string WeaponId = "";

    // Amount of damage the weapon does per bullet
    // TODO: For projectile weapons, the damage needs to be
    //       accessible by the projectile during hit to calculate
    //       damage.  For hitscan, ok.
    [Export] public float Damage = 10.0f;

    // Cooldown between uses
    [Export] public float UseCooldown = 0.2f;

    // If true, player can just hold button down. Else they have to reclick.
    [Export] public bool isAutomatic = true;

    // How much this weapon knocks the player back.
    // TODO: Unimplemented.
    [Export] public float Knockback = 100.0f;
    [Export] public float ScreenShake = 1.5f;
    [Export] public PackedScene ProjectileScene;
    [Export] public float HitscanRange = 1000.0f;
    [Export] public Vector2 ProjectileSpawnOffset = new Vector2(20, 0);
    [Export] public float TrailDuration = 0.1f;

    [ExportGroup("Spread")]
    [Export] public int SpreadCount = 1;
    [Export] public float SpreadAngle = 15.0f;
}
