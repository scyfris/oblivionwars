using Godot;
using System.Linq;

public partial class PlayerController : Node
{
    [Export] private PlayerCharacterBody2D _characterBody;
    [Export] private PlayerDefinition _definition;
    public PlayerDefinition Definition => _definition;
    [Export] private HoldableSystem _holdableSystem;

    private AnimatedSprite2D _spriteNode => _characterBody.SpriteNode;

    public PlayerCharacterBody2D CharacterBody => _characterBody;

    // helper for player state
    public PlayerState PlayerStateCurrent => GlobalStateManager.Instance.Player;

    private string _currentWeaponId = "";

    // Aim
    private Vector2 _aimTarget;

    private bool _isInvincibleFromDamage = true;
    private float _invincibilityTimer = 0f;
    private float _flashTimer = 0f;
    private const float FlashInterval = 0.1f;

    public override void _Ready()
    {
        if (_definition.EntityId == null || _definition.EntityId == "")
        {
            GD.PrintErr($"{Name}: No EntityID definition on EntytCharacterBody2d resource");
        }

        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance.Subscribe<ForceWeaponSelectEvent>(OnForceWeaponSelect);
        EventBus.Instance.Subscribe<HitEvent>(OnHit);
        EventBus.Instance.Subscribe<HazardContactEvent>(OnHazardContact);

        // Initialize holdables
        if (_holdableSystem != null)
            _holdableSystem.Initialize(_characterBody, _definition);

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
//        if (SaveManager.Instance?.IsRespawning == true)
//        {
//            var checkpoint = FindCheckpointById(GlobalStateManager.Instance.Player.LastCheckpointId);
//            if (checkpoint != null)
//                _characterBody.GlobalPosition = checkpoint.RespawnPosition.GlobalPosition;
//
//            RuntimeData.CurrentHealth = RuntimeData.MaxHealth;
//            if (GlobalStateManager.Instance.Player != null)
//                GlobalStateManager.Instance.Player.CurrentHealth = RuntimeData.MaxHealth;
//
//            SaveManager.Instance.IsRespawning = false;
//            GD.Print($"Player respawned at checkpoint {GlobalStateManager.Instance.Player?.LastCheckpointId}");
//        }
//        else if (GlobalStateManager.Instance.Player != null)
//        {
//            RuntimeData.CurrentHealth = GlobalStateManager.Instance.Player.CurrentHealth;
//        }
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Unsubscribe<ForceWeaponSelectEvent>(OnForceWeaponSelect);
    }

    public override void _PhysicsProcess(double delta)
    {
        CheckContactDamage(delta);

        _holdableSystem?.Update(delta);
        UpdateInvincibility(delta);
    }

    private float _contactDamageCooldown = 0f;
    private const float ContactDamageCooldownTime = 0.5f;


    private void CheckContactDamage(double delta)
    {
        _contactDamageCooldown -= (float)delta;

        for (int i = 0; i < _characterBody.GetSlideCollisionCount(); i++)
        {
            var collision = _characterBody.GetSlideCollision(i);
            if (collision.GetCollider() is NPCEntityCharacterBody2D enemy)
            {
                NPCDefinition _npcDef = (collision.GetCollider() as NPCEntityCharacterBody2D).Controller.Definition;

                if (_contactDamageCooldown > 0 || _npcDef == null || _npcDef.ContactDamage <= 0) return;

                Vector2 hitDir = (enemy.GlobalPosition - _characterBody.GlobalPosition).Normalized();
                EventBus.Instance.Raise(new HitEvent
                {
                    TargetInstanceId =enemy.GetInstanceId(),
                    SourceInstanceId = GetInstanceId(),
                    BaseDamage = _npcDef.ContactDamage,
                    HitDirection = hitDir,
                    HitPosition = collision.GetPosition()
                });
                _contactDamageCooldown = ContactDamageCooldownTime;
                break;
            }
        }
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

    public void UseHoldablePressed(bool isLeft)
    {
        if (isLeft) _holdableSystem?.PressLeft();
        else _holdableSystem?.PressRight();
    }

    public void UseHoldableReleased(bool isLeft)
    {
        if (isLeft) _holdableSystem?.ReleaseLeft();
        else _holdableSystem?.ReleaseRight();
    }

    public void UseHoldableHeld(bool isLeft)
    {
        if (isLeft) _holdableSystem?.HeldLeft();
        else _holdableSystem?.HeldRight();
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

        var entry = GlobalDefinitions.Instance?.FindWeaponEntry(weaponId);
        if (entry?.Scene == null) return;

        var prev = _currentWeaponId;
        _currentWeaponId = weaponId;
        _holdableSystem?.SwapLeft(entry);
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

    private void StartDamageInvincibility()
    {
        var definition = _definition;
        if (definition == null) return;

        _isInvincibleFromDamage = true;
        _invincibilityTimer = definition.HazardDmgInvincibilityDuration;
        _flashTimer = 0f;
    }

    private void UpdateInvincibility(double delta)
    {
        if (!_isInvincibleFromDamage) return;

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
            _isInvincibleFromDamage = false;
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

        // later can add some effects or something.
    }

    // ── Helpers ─────────────────────────────────────────────

    private Checkpoint FindCheckpointById(string checkpointId)
    {
        var checkpoints = GetTree().GetNodesInGroup(GroupConstants.Level.Checkpoint);
        return checkpoints
            .OfType<Checkpoint>()
            .FirstOrDefault(cp => cp.UniqueId == checkpointId);
    }

    int cntr = 0;
    private void OnHit(HitEvent evt)
    {
        if (evt.TargetInstanceId != _characterBody.GetInstanceId())
            return;

        if (_isInvincibleFromDamage)
            return;

        float finalDamage = evt.BaseDamage;

        PlayerStateCurrent.CurrentHealth -= finalDamage;
        
        EventBus.Instance.Raise(new DamageAppliedEvent
        {
            TargetInstanceId = evt.TargetInstanceId,
            FinalDamage = finalDamage,
        });

        StartDamageInvincibility();


        // Handle death case.
        if (PlayerStateCurrent.CurrentHealth < 0)
            PlayerStateCurrent.CurrentHealth = 0;

        if (PlayerStateCurrent.CurrentHealth <= 0)
        {
            EventBus.Instance.Raise(new EntityDiedEvent
            {
                EntityInstanceId = evt.TargetInstanceId,
                KillerInstanceId = 0
            });
        }
    }

    private void OnHazardContact(HazardContactEvent evt)
    {
        float damage = GlobalDefinitions.Instance.HazardDefs.GetDamage(evt.HazardType);
        if (damage <= 0) return;

        EventBus.Instance.Raise(new HitEvent
        {
            TargetInstanceId = evt.EntityInstanceId,
            SourceInstanceId = 0,
            BaseDamage = damage,
            HitDirection = Godot.Vector2.Zero,
            HitPosition = evt.Position
        });
    }
}
