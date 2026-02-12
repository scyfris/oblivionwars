using Godot;

#if TOOLS
[Tool]
#endif
public partial class Checkpoint : Interactable, ISaveableObject
{
    [ExportGroup("Identification")]
    [Export] public string UniqueId { get; set; } = "";

    [ExportGroup("Editor Tools")]
    [Export]
    private bool GenerateUniqueId
    {
        get => false; // Always false so checkbox resets
        set
        {
            if (value)
            {
#if TOOLS
                if (Engine.IsEditorHint())
                {
                    SaveableObjectHelper.GenerateUniqueId(this, this);
                }
#endif
            }
        }
    }

    [ExportGroup("Configuration")]
    [Export] public Node2D RespawnPosition;

    private bool _isActivated = false;
    private bool _isUpgraded = false;

    public SaveableLevelObjectType GetObjectType() => SaveableLevelObjectType.Checkpoint;

    public LevelObjectSaveDataEntry SaveState()
    {
        return new CheckpointSaveData
        {
            IsActivated = _isActivated,
            IsUpgraded = _isUpgraded
        };
    }

    public void LoadState(LevelObjectSaveDataEntry data)
    {
        if (data is CheckpointSaveData checkpointData)
        {
            _isActivated = checkpointData.IsActivated;
            _isUpgraded = checkpointData.IsUpgraded;
        }
    }

    public override void _Ready()
    {
        // Skip setup in editor mode
        if (Engine.IsEditorHint()) return;

        GD.Print($"━━━ CHECKPOINT READY START ━━━");
        GD.Print($"Checkpoint UniqueId: '{UniqueId}'");
        GD.Print($"Checkpoint Name: '{Name}'");
        PromptText = "Save";
        AddToGroup("checkpoints");

        // Connect body signals for interaction tracking
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        GD.Print($"Signals connected! Monitoring={Monitoring}, Monitorable={Monitorable}");
        GD.Print($"CollisionLayer={CollisionLayer}, CollisionMask={CollisionMask}");

        // Load saved state if it exists
        var savedData = LevelState.Instance?.LoadObjectState(UniqueId);
        if (savedData != null)
        {
            LoadState(savedData);
        }

        GD.Print($"━━━ CHECKPOINT READY END ━━━");
        // Future: change visual to show activated vs inactive
    }

    public override void Interact(PlayerCharacterBody2D player)
    {
        if (string.IsNullOrEmpty(UniqueId))
        {
            GD.PrintErr("Checkpoint: UniqueId is not set! Cannot save.");
            return;
        }

        // 1. Update local state
        _isActivated = true;

        // 2. Save state using new generic system
        var state = SaveState();
        LevelState.Instance?.SaveObjectState(UniqueId, state);

        // 3. Also add to legacy ActivatedCheckpointIds for backward compatibility
        LevelState.Instance?.ActivateCheckpoint(UniqueId);

        // 4. Update PlayerState with this checkpoint
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.LastCheckpointId = UniqueId;
            PlayerState.Instance.LastCheckpointLevelId =
                LevelState.Instance?.CurrentLevel?.LevelId ?? "";
            PlayerState.Instance.CurrentHealth = player.RuntimeData.CurrentHealth;
        }

        // 5. Save everything to disk
        SaveManager.Instance?.Save();

        // 6. Raise event for UI
        EventBus.Instance?.Raise(new SaveCompletedEvent
        {
            SlotIndex = SaveManager.Instance?.ActiveSlotIndex ?? -1
        });

        GD.Print($"Checkpoint {UniqueId}: Saved!");
    }

    private void OnBodyEntered(Node2D body)
    {
        GD.PrintErr($"▶▶▶ CHECKPOINT BODY ENTERED: {body?.Name ?? "null"} (Type: {body?.GetType().Name ?? "null"})");
        if (body is PlayerCharacterBody2D player)
        {
            GD.PrintErr($"▶▶▶ IT'S THE PLAYER! Setting nearest interactable");
            player.SetNearestInteractable(this);
        }
        else
        {
            GD.PrintErr($"▶▶▶ Not the player, body type is: {body?.GetType().Name ?? "null"}");
        }
    }

    private void OnBodyExited(Node2D body)
    {
        GD.PrintErr($"◀◀◀ CHECKPOINT BODY EXITED: {body?.Name ?? "null"}");
        if (body is PlayerCharacterBody2D player)
        {
            GD.PrintErr($"◀◀◀ Player left, clearing interactable");
            player.ClearInteractable(this);
        }
    }
}
