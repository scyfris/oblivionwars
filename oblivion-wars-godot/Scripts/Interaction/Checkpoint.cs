using Godot;

public partial class Checkpoint : Interactable
{
    [Export] public uint CheckpointId;
    [Export] public Node2D RespawnPosition;

    public override void _Ready()
    {
        PromptText = "Save";
        AddToGroup("checkpoints");

        // Connect body signals for interaction tracking
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        // Read own state from LevelState
        bool activated = LevelState.Instance?.IsCheckpointActivated(CheckpointId) ?? false;
        // Future: change visual to show activated vs inactive
    }

    public override void Interact(PlayerCharacterBody2D player)
    {
        // 1. Activate in LevelState
        LevelState.Instance?.ActivateCheckpoint(CheckpointId);

        // 2. Update PlayerState with this checkpoint
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.LastCheckpointId = CheckpointId;
            PlayerState.Instance.LastCheckpointLevelId =
                LevelState.Instance?.CurrentLevel?.LevelId ?? "";
            PlayerState.Instance.CurrentHealth = player.RuntimeData.CurrentHealth;
        }

        // 3. Save everything to disk
        SaveManager.Instance?.Save();

        // 4. Raise event for UI
        EventBus.Instance?.Raise(new SaveCompletedEvent
        {
            SlotIndex = SaveManager.Instance?.ActiveSlotIndex ?? -1
        });

        GD.Print($"Checkpoint {CheckpointId}: Saved!");
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is PlayerCharacterBody2D player)
            player.SetNearestInteractable(this);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is PlayerCharacterBody2D player)
            player.ClearInteractable(this);
    }
}
