using Godot;
using System.Linq;

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

    // Interaction
    private Interactable _nearestInteractable;

    public Interactable NearestInteractable => _nearestInteractable;

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

        // Initialize from PlayerState if respawning
        if (SaveManager.Instance?.IsRespawning == true)
        {
            var checkpoint = FindCheckpointById(PlayerState.Instance.LastCheckpointId);
            if (checkpoint != null)
                GlobalPosition = checkpoint.RespawnPosition.GlobalPosition;

            _runtimeData.CurrentHealth = _runtimeData.MaxHealth;
            if (PlayerState.Instance != null)
                PlayerState.Instance.CurrentHealth = _runtimeData.MaxHealth;

            SaveManager.Instance.IsRespawning = false;
            GD.Print($"Player respawned at checkpoint {PlayerState.Instance?.LastCheckpointId}");
        }
        else if (PlayerState.Instance != null)
        {
            // Normal load — sync health from PlayerState
            _runtimeData.CurrentHealth = PlayerState.Instance.CurrentHealth;
        }
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

        GD.Print("Player died! Respawning from checkpoint...");

        if (SaveManager.Instance != null && SaveManager.Instance.ActiveSlotIndex >= 0)
        {
            SaveManager.Instance.ReloadLastSave();
            SaveManager.Instance.IsRespawning = true;

            string levelScene = SaveManager.Instance.GetLevelScenePath(
                PlayerState.Instance?.LastCheckpointLevelId ?? ""
            );

            if (!string.IsNullOrEmpty(levelScene))
            {
                GetTree().ChangeSceneToFile(levelScene);
                return;
            }
        }

        // Fallback: no save system active, just reload
        GetTree().ReloadCurrentScene();
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.TargetInstanceId != GetInstanceId()) return;

        StartInvincibility();

        // Sync health to PlayerState
        if (PlayerState.Instance != null)
            PlayerState.Instance.CurrentHealth = _runtimeData.CurrentHealth;
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

    private void UpdateAnimation()
    {
        if (_spriteNode == null) return;

        // Update facing direction only when moving — persists when idle
        if (_moveDirection != 0)
            _facingRight = _moveDirection > 0;

        if (_flipRoot != null)
            _flipRoot.Scale = new Vector2(_facingRight ? 1 : -1, 1);

        if (_moveDirection != 0 && IsOnFloor())
        {
            // Project aim direction onto the entity's local horizontal axis
            // so the comparison works regardless of gravity rotation
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

    // ── Interaction ────────────────────────────────────────

    public void SetNearestInteractable(Interactable interactable)
    {
        _nearestInteractable = interactable;
    }

    public void ClearInteractable(Interactable interactable)
    {
        if (_nearestInteractable == interactable)
            _nearestInteractable = null;
    }

    public void TryInteract()
    {
        _nearestInteractable?.Interact(this);
    }

    // ── Checkpoint Lookup ──────────────────────────────────

    private Checkpoint FindCheckpointById(uint checkpointId)
    {
        var checkpoints = GetTree().GetNodesInGroup("checkpoints");
        return checkpoints
            .OfType<Checkpoint>()
            .FirstOrDefault(cp => cp.CheckpointId == checkpointId);
    }
}
