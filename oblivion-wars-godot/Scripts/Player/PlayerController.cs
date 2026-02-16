using Godot;
using System.Linq;

public partial class PlayerController : Node
{
    [Export] private PlayerCharacterBody2D _characterBody;

    public PlayerCharacterBody2D CharacterBody => _characterBody;

    public override void _Ready()
    {
        EventBus.Instance.Subscribe<EntityDiedEvent>(OnEntityDied);
        EventBus.Instance.Subscribe<DamageAppliedEvent>(OnDamageApplied);

        // Initialize weapons from Definition or scene based on flag
        _characterBody.InitializeHoldables();

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
    }

    // ── Movement Pass-Through (called by PlayerInputController) ──

    public void Jump() => _characterBody.Jump();
    public void CancelJump() => _characterBody.CancelJump();
    public void MoveLeft() => _characterBody.MoveLeft();
    public void MoveRight() => _characterBody.MoveRight();
    public void Stop() => _characterBody.Stop();
    public void RotateGravityClockwise() => _characterBody.RotateGravityClockwise();
    public void RotateGravityCounterClockwise() => _characterBody.RotateGravityCounterClockwise();

    public void UpdateAim(Vector2 targetPosition) => _characterBody.UpdateAim(targetPosition);
    public void UseHoldablePressed(Vector2 target, bool isLeft) => _characterBody.UseHoldablePressed(target, isLeft);
    public void UseHoldableReleased(Vector2 target, bool isLeft) => _characterBody.UseHoldableReleased(target, isLeft);
    public void UseHoldableHeld(Vector2 target, bool isLeft) => _characterBody.UseHoldableHeld(target, isLeft);

    public Vector2 GetGlobalMousePosition() => _characterBody.GetGlobalMousePosition();

    // ── Interaction ────────────────────────────────────────

    public void TryInteract()
    {
        _characterBody.NearestInteractable?.Interact(_characterBody);
    }

    // ── Event Handlers ─────────────────────────────────────

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

        _characterBody.StartInvincibility();

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
