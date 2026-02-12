using Godot;

#if TOOLS
[Tool]
#endif
public partial class Door : Area2D, ISaveableObject
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

    [ExportGroup("Door Configuration")]
    [Export] public string DestinationLevelId = "";
    [Export] public string DestinationDoorId = "";
    [Export] public bool StartsLocked = false;
    [Export] public string RequiredKeyId = "";  // Ability or item needed to unlock

    private bool _isLocked;

    public SaveableLevelObjectType GetObjectType() => SaveableLevelObjectType.Door;

    public LevelObjectSaveDataEntry SaveState()
    {
        return new DoorSaveData
        {
            IsUnlocked = !_isLocked
        };
    }

    public void LoadState(LevelObjectSaveDataEntry data)
    {
        if (data is DoorSaveData doorData)
        {
            _isLocked = !doorData.IsUnlocked;
        }
    }

    public override void _Ready()
    {
#if !TOOLS
        // Load saved state if it exists
        var savedData = LevelState.Instance?.LoadObjectState(UniqueId);
        if (savedData != null)
        {
            LoadState(savedData);
        }
        else
        {
            // No saved state, use default
            _isLocked = StartsLocked;
        }

        // Connect signals
        BodyEntered += OnBodyEntered;
#endif
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerCharacterBody2D player) return;

        if (_isLocked)
        {
            // Check if player has required key/ability
            if (!string.IsNullOrEmpty(RequiredKeyId) && !PlayerState.Instance.HasUnlock(RequiredKeyId))
            {
                GD.Print($"Door {UniqueId} is locked. Need: {RequiredKeyId}");
                return;
            }

            // Unlock the door
            Unlock();
        }

        // Transition to destination level
        TransitionToDestination();
    }

    private void Unlock()
    {
        _isLocked = false;

        // Save state using new generic system
        var state = SaveState();
        LevelState.Instance?.SaveObjectState(UniqueId, state);

        // Also add to legacy UnlockedDoorIds for backward compatibility
        LevelState.Instance?.UnlockDoor(UniqueId);

        SaveManager.Instance?.Save();
        GD.Print($"Door {UniqueId} unlocked!");
    }

    private void TransitionToDestination()
    {
        // TODO: Implement level transition
        // This would raise a LevelTransitionEvent that LevelManager handles
        GD.Print($"Transitioning to {DestinationLevelId} via door {DestinationDoorId}");
    }
}
