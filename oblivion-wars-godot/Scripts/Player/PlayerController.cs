using Godot;
using System.Linq;

public partial class PlayerController : Node
{
    [Export] private PlayerCharacterBody2D _characterBody;
    [Export] private HoldableSystem _holdableSystem;
    [Export] private AnimatedSprite2D _spriteNode;

    public PlayerCharacterBody2D CharacterBody => _characterBody;

    private string _currentWeaponId = "";

    // Aim
    private Vector2 _aimTarget;

    // Invincibility (state lives on RuntimeData.IsInvincible, timers here)
    private float _invincibilityTimer = 0f;
    private float _flashTimer = 0f;
    private const float FlashInterval = 0.1f;

    public override void _Ready()
    {
        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance.Subscribe<ForceWeaponSelectEvent>(OnForceWeaponSelect);

        // Initialize holdables
        if (_holdableSystem != null)
        {
            if (_holdableSystem.UseDefinitionWeapons)
                _holdableSystem.InitializeWithDefinition(_characterBody, _characterBody.Definition);
            else
                _holdableSystem.Initialize(_characterBody);
        }

        // Ensure weapons are unlocked (handles testing without save system / new-game flow)
        var playerState = GlobalStateManager.Instance?.Player;
        if (playerState != null && playerState.GetUnlockedWeapons().Length == 0)
        {
            var allWeapons = GlobalDefinitions.Instance?.GetAllWeaponNames();
            if (allWeapons != null)
            {
                foreach (var name in allWeapons)
                    playerState.UnlockWeapon(name, -1);
            }
        }

        // Equip saved weapon (or default)
        var savedId = GlobalStateManager.Instance?.Player?.CurrentWeaponId ?? "";
        if (string.IsNullOrEmpty(savedId))
            savedId = GlobalDefinitions.Instance?.GetDefaultWeaponName() ?? "";
        SelectWeapon(savedId);

        // Checkpoint respawn logic
        if (SaveManager.Instance?.IsRespawning == true)
        {
            var checkpoint = FindCheckpointById(GlobalStateManager.Instance.Player.LastCheckpointId);
            if (checkpoint != null)
                _characterBody.GlobalPosition = checkpoint.RespawnPosition.GlobalPosition;

            _characterBody.RuntimeData.CurrentHealth = _characterBody.RuntimeData.MaxHealth;
            if (GlobalStateManager.Instance.Player != null)
                GlobalStateManager.Instance.Player.CurrentHealth = _characterBody.RuntimeData.MaxHealth;

            SaveManager.Instance.IsRespawning = false;
            GD.Print($"Player respawned at checkpoint {GlobalStateManager.Instance.Player?.LastCheckpointId}");
        }
        else if (GlobalStateManager.Instance.Player != null)
        {
            _characterBody.RuntimeData.CurrentHealth = GlobalStateManager.Instance.Player.CurrentHealth;
        }
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Unsubscribe<ForceWeaponSelectEvent>(OnForceWeaponSelect);
    }

    public override void _PhysicsProcess(double delta)
    {
        _holdableSystem?.Update(delta);
        UpdateInvincibility(delta);
    }

    // ── Movement Pass-Through (called by PlayerInputController) ──

    public void Jump() => _characterBody.Jump();
    public void CancelJump() => _characterBody.CancelJump();
    public void MoveLeft() => _characterBody.StartMoveLeft();
    public void MoveRight() => _characterBody.StartMoveRight();
    public void Stop() => _characterBody.Stop();
    public void RotateGravityClockwise() => _characterBody.RotateGravityClockwise();
    public void RotateGravityCounterClockwise() => _characterBody.RotateGravityCounterClockwise();

    public Vector2 GetGlobalMousePosition() => _characterBody.GetGlobalMousePosition();

    // ── Aim & Holdable API ──────────────────────────────────

    public void UpdateAim(Vector2 targetPosition)
    {
        _aimTarget = targetPosition;
        _characterBody.AimTarget = targetPosition;
        _holdableSystem?.UpdateAim(targetPosition);
    }

    public void UseHoldablePressed(Vector2 target, bool isLeft)
    {
        if (isLeft) _holdableSystem?.PressLeft(target);
        else _holdableSystem?.PressRight(target);
    }

    public void UseHoldableReleased(Vector2 target, bool isLeft)
    {
        if (isLeft) _holdableSystem?.ReleaseLeft(target);
        else _holdableSystem?.ReleaseRight(target);
    }

    public void UseHoldableHeld(Vector2 target, bool isLeft)
    {
        if (isLeft) _holdableSystem?.HeldLeft(target);
        else _holdableSystem?.HeldRight(target);
    }

    // ── Interaction ────────────────────────────────────────

    public void TryInteract()
    {
        _characterBody.NearestInteractable?.Interact(_characterBody);
    }

    // ── Weapon Switching ─────────────────────────────────────

    public void SelectWeapon(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId) || weaponId == _currentWeaponId) return;
        if (!GlobalStateManager.Instance.Player.IsWeaponUnlocked(weaponId)) return;

        var scene = GlobalDefinitions.Instance?.GetWeaponScene(weaponId);
        if (scene == null) return;

        var prev = _currentWeaponId;
        _currentWeaponId = weaponId;
        _holdableSystem?.SwapLeft(scene);
        GlobalStateManager.Instance.Player.CurrentWeaponId = weaponId;

        EventBus.Instance?.Raise(new WeaponSwitchedEvent
        {
            NewWeaponId = weaponId,
            PreviousWeaponId = prev
        });
    }

    public void SelectWeaponSlot(int slotIndex)
    {
        var name = GlobalDefinitions.Instance?.GetWeaponNameBySlot(slotIndex);
        if (name != null) SelectWeapon(name);
    }

    public void CycleWeapon(int direction)
    {
        var unlocked = GlobalStateManager.Instance.Player.GetUnlockedWeapons();
        if (unlocked.Length == 0) return;

        int idx = System.Array.IndexOf(unlocked, _currentWeaponId);
        if (idx < 0) idx = 0;

        int next = (idx + direction + unlocked.Length) % unlocked.Length;
        SelectWeapon(unlocked[next]);
    }

    // ── Invincibility ─────────────────────────────────────

    private void StartInvincibility()
    {
        var definition = _characterBody.Definition as PlayerDefinition;
        if (definition == null) return;

        _characterBody.RuntimeData.IsInvincible = true;
        _invincibilityTimer = definition.InvincibilityDuration;
        _flashTimer = 0f;
    }

    private void UpdateInvincibility(double delta)
    {
        if (!_characterBody.RuntimeData.IsInvincible) return;

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
            _characterBody.RuntimeData.IsInvincible = false;
            if (_spriteNode != null)
                _spriteNode.Visible = true;
        }
    }

    // ── Event Handlers ─────────────────────────────────────

    private void OnForceWeaponSelect(ForceWeaponSelectEvent evt)
    {
        _currentWeaponId = ""; // Clear so SelectWeapon doesn't skip same-id check
        SelectWeapon(evt.WeaponId);
    }

    private void OnEntityDied(EntityDiedEvent evt)
    {
        if (evt.EntityInstanceId != _characterBody.GetInstanceId()) return;

        GD.Print("Player died! Respawning from checkpoint...");

        if (SaveManager.Instance != null && SaveManager.Instance.ActiveSlotIndex >= 0)
        {
            SaveManager.Instance.ReloadLastSave();
            SaveManager.Instance.IsRespawning = true;

            string levelScene = SaveManager.Instance.GetLevelScenePath(
                GlobalStateManager.Instance.Player?.LastCheckpointLevelId ?? ""
            );

            if (!string.IsNullOrEmpty(levelScene))
            {
                GetTree().ChangeSceneToFile(levelScene);
                return;
            }
        }

        GetTree().ReloadCurrentScene();
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (evt.TargetInstanceId != _characterBody.GetInstanceId()) return;

        StartInvincibility();

        // Sync health to PlayerState
        if (GlobalStateManager.Instance.Player != null)
            GlobalStateManager.Instance.Player.CurrentHealth = _characterBody.RuntimeData.CurrentHealth;
    }

    // ── Helpers ─────────────────────────────────────────────

    private Checkpoint FindCheckpointById(string checkpointId)
    {
        var checkpoints = GetTree().GetNodesInGroup(Groups.Level.Checkpoint);
        return checkpoints
            .OfType<Checkpoint>()
            .FirstOrDefault(cp => cp.UniqueId == checkpointId);
    }
}
