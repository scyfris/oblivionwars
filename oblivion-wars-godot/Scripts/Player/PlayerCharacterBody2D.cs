using Godot;

public partial class PlayerCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new PlayerDefinition _definition;

    [Export] private HoldableSystem _holdableSystem;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;
    [Export] private AnimatedSprite2D _spriteNode;
    [Export] private string _idleAnimation = "default";
    [Export] private string _walkFacingDirAnimation = "walk-facingdir";
    [Export] private string _walkNonFacingDirAnimation = "walk-nonfacingdir";

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
    private Vector2 _aimTarget;
    private bool _facingRight = true;

    // Interaction (body stores reference since it's the physical object that overlaps)
    private Interactable _nearestInteractable;
    public Interactable NearestInteractable => _nearestInteractable;

    public override void _Ready()
    {
        AddToGroup(Groups.Entities.Player);

        base._definition = _definition;
        base._Ready();

        if (_wallSlideDustPosition != null && _wallSlideDustScene != null)
        {
            _wallSlideDust = _wallSlideDustScene.Instantiate<CpuParticles2D>();
            _wallSlideDust.Emitting = false;
            _wallSlideDustPosition.AddChild(_wallSlideDust);
        }
    }

    /// <summary>
    /// Called by PlayerController to initialize the holdable system.
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

        UpdateAnimation();
        UpdateInvincibility(delta);
        _holdableSystem?.Update(delta);
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

    // ── Holdable API ──────────────────────────────────────

    public void SwapLeftHoldable(PackedScene scene)
    {
        _holdableSystem?.SwapLeft(scene);
    }

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

    // ── Invincibility ─────────────────────────────────────

    public void StartInvincibility()
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

    // ── Animation ─────────────────────────────────────────

    private void UpdateAnimation()
    {
        if (_spriteNode == null) return;

        if (_moveDirection != 0)
            _facingRight = _moveDirection > 0;

        if (_flipRoot != null)
            _flipRoot.Scale = new Vector2(_facingRight ? 1 : -1, 1);

        if (_moveDirection != 0 && IsOnFloor())
        {
            Vector2 horizontalDir = new Vector2(_gravityDirection.Y, -_gravityDirection.X);
            float aimDot = (_aimTarget - GlobalPosition).Dot(horizontalDir);
            bool aimToLocalRight = aimDot > 0;
            bool movingTowardAim = _facingRight == aimToLocalRight;

            _spriteNode.Play(movingTowardAim ? _walkFacingDirAnimation : _walkNonFacingDirAnimation);
        }
        else
        {
            _spriteNode.Play(_idleAnimation);
        }
    }

    // ── Interaction (thin storage only) ───────────────────

    public void SetNearestInteractable(Interactable interactable)
    {
        _nearestInteractable = interactable;
    }

    public void ClearInteractable(Interactable interactable)
    {
        if (_nearestInteractable == interactable)
            _nearestInteractable = null;
    }
}
