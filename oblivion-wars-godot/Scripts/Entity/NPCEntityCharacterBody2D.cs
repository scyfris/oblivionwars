using Godot;

public partial class NPCEntityCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new NPCDefinition _definition;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;

    private bool _facingRight = true;
    private float _contactDamageCooldown = 0f;
    private const float ContactDamageCooldownTime = 0.5f;

    public new NPCDefinition Definition => _definition;

    public override void _Ready()
    {
        base._definition = _definition;
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        UpdateFacing();
        CheckContactDamage(delta);
    }

    // ── Contact Damage ──────────────────────────────────

    private void CheckContactDamage(double delta)
    {
        _contactDamageCooldown -= (float)delta;
        if (_contactDamageCooldown > 0 || _definition == null || _definition.ContactDamage <= 0) return;

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is PlayerCharacterBody2D player)
            {
                Vector2 hitDir = (player.GlobalPosition - GlobalPosition).Normalized();
                EventBus.Instance.Raise(new HitEvent
                {
                    TargetInstanceId = player.GetInstanceId(),
                    SourceInstanceId = GetInstanceId(),
                    BaseDamage = _definition.ContactDamage,
                    HitDirection = hitDir,
                    HitPosition = collision.GetPosition(),
                    Projectile = null
                });
                _contactDamageCooldown = ContactDamageCooldownTime;
                break;
            }
        }
    }

    // ── Visuals ─────────────────────────────────────────

    private void UpdateFacing()
    {
        if (_flipRoot == null) return;

        if (_moveDirection != 0)
            _facingRight = _moveDirection > 0;

        _flipRoot.Scale = new Vector2(_facingRight ? 1 : -1, 1);
    }
}
