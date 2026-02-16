using Godot;

public partial class NPCEntityCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new EnemyDefinition _definition;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;

    [ExportGroup("Combat")]
    [Export] private HoldableSystem _holdableSystem;

    private Vector2 _aimTarget;
    private bool _facingRight = true;
    private float _contactDamageCooldown = 0f;
    private const float ContactDamageCooldownTime = 0.5f;

    public new EnemyDefinition Definition => _definition;

    public override void _Ready()
    {
        base._definition = _definition;
        base._Ready();
    }

    /// <summary>
    /// Called by NPCController to initialize the holdable system.
    /// </summary>
    public void InitializeHoldables()
    {
        if (_holdableSystem == null) return;

        if (_holdableSystem.UseDefinitionWeapons)
            _holdableSystem.InitializeWithDefinition(this, _definition);
        else
            _holdableSystem.Initialize(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        UpdateFacing();
        _holdableSystem?.Update(delta);
        CheckContactDamage(delta);
    }

    // ── Holdable API (called by NPCController) ──────────

    public void UpdateAim(Vector2 targetPosition)
    {
        _aimTarget = targetPosition;
        _holdableSystem?.UpdateAim(targetPosition);
    }

    public void UseHoldablePressed(Vector2 targetPosition, bool isLeft)
    {
        if (isLeft)
            _holdableSystem?.PressLeft(targetPosition);
        else
            _holdableSystem?.PressRight(targetPosition);
    }

    public void UseHoldableReleased(Vector2 targetPosition, bool isLeft)
    {
        if (isLeft)
            _holdableSystem?.ReleaseLeft(targetPosition);
        else
            _holdableSystem?.ReleaseRight(targetPosition);
    }

    public void UseHoldableHeld(Vector2 targetPosition, bool isLeft)
    {
        if (isLeft)
            _holdableSystem?.HeldLeft(targetPosition);
        else
            _holdableSystem?.HeldRight(targetPosition);
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
