using Godot;

public partial class PlayerCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new PlayerDefinition _definition;

    [Export] private HoldableSystem _holdableSystem;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;
    [Export] private AnimatedSprite2D _spriteNode;
    [Export] private string _idleAnimation = "default";
    [Export] private string _walkAnimation = "walk";

    [ExportGroup("Wall Slide Effects")]
    [Export] private Node2D _wallSlideDustPosition;
    [Export] private PackedScene _wallSlideDustScene;

    // Invincibility state (player-only)
    private bool _isInvincible = false;
    private float _invincibilityTimer = 0f;
    private float _flashTimer = 0f;
    private const float FlashInterval = 0.1f;
    public bool IsInvincible => _isInvincible;

    private CpuParticles2D _wallSlideDust;

    public override void _Ready()
    {
        // Set the base class _definition so base code works
        base._definition = _definition;
        base._Ready();

        if (_wallSlideDustPosition != null && _wallSlideDustScene != null)
        {
            _wallSlideDust = _wallSlideDustScene.Instantiate<CpuParticles2D>();
            _wallSlideDust.Emitting = false;
            _wallSlideDustPosition.AddChild(_wallSlideDust);
        }

        _holdableSystem?.Initialize(this);

        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        UpdateAnimation();
        UpdateInvincibility(delta);
        _holdableSystem?.Update(delta);
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.EntityInstanceId != GetInstanceId()) return;

        GD.Print("Player died! Reloading scene...");
        GetTree().ReloadCurrentScene();
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.TargetInstanceId != GetInstanceId()) return;

        StartInvincibility();
    }

    // Override hazard check to skip while invincible
    protected override void CheckHazardTiles()
    {
        if (_isInvincible) return;
        base.CheckHazardTiles();
    }

    // Wall slide dust particle management
    protected override void SetWallSliding(bool sliding)
    {
        base.SetWallSliding(sliding);
        if (_wallSlideDust != null)
        {
            _wallSlideDust.Emitting = sliding;
            if (sliding)
                _wallSlideDust.Direction = _wallNormal;
        }
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

    private void UpdateAnimation()
    {
        if (_spriteNode == null) return;

        if (_moveDirection != 0 && _flipRoot != null)
            _flipRoot.Scale = new Vector2(_moveDirection < 0 ? -1 : 1, 1);

        if (_moveDirection != 0 && IsOnFloor())
            _spriteNode.Play(_walkAnimation);
        else
            _spriteNode.Play(_idleAnimation);
    }

    private void StartInvincibility()
    {
        _isInvincible = true;
        _invincibilityTimer = _definition.InvincibilityDuration;
        _flashTimer = 0f;
    }

    private void UpdateInvincibility(double delta)
    {
        if (!_isInvincible) return;

        _invincibilityTimer -= (float)delta;
        _flashTimer += (float)delta;

        if (_flashTimer >= FlashInterval)
        {
            _flashTimer -= FlashInterval;
            if (_spriteNode != null)
                _spriteNode.Visible = !_spriteNode.Visible;
        }

        if (_invincibilityTimer <= 0)
        {
            _isInvincible = false;
            if (_spriteNode != null)
                _spriteNode.Visible = true;
        }
    }
}
