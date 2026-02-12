using Godot;

public partial class MainMenu : Control
{
    [Export] private string _startingLevelId = "test";
    [Export] private uint _startingCheckpointId = 0;
    [Export] private string _startingLevelScenePath = "res://Scenes/Levels/MainLevel.tscn";

    [Export] private Control _mainPanel;
    [Export] private Control _slotPanel;
    [Export] private Button _playButton;
    [Export] private Button _quitButton;

    [Export] private Button _slot0Button;
    [Export] private Button _slot1Button;
    [Export] private Button _slot2Button;
    [Export] private Button _deleteSlot0Button;
    [Export] private Button _deleteSlot1Button;
    [Export] private Button _deleteSlot2Button;
    [Export] private Button _backButton;

    public override void _Ready()
    {
        _playButton?.Connect("pressed", Callable.From(OnPlayPressed));
        _quitButton?.Connect("pressed", Callable.From(OnQuitPressed));
        _backButton?.Connect("pressed", Callable.From(OnBackPressed));

        _slot0Button?.Connect("pressed", Callable.From(() => OnSlotPressed(0)));
        _slot1Button?.Connect("pressed", Callable.From(() => OnSlotPressed(1)));
        _slot2Button?.Connect("pressed", Callable.From(() => OnSlotPressed(2)));

        _deleteSlot0Button?.Connect("pressed", Callable.From(() => OnDeleteSlotPressed(0)));
        _deleteSlot1Button?.Connect("pressed", Callable.From(() => OnDeleteSlotPressed(1)));
        _deleteSlot2Button?.Connect("pressed", Callable.From(() => OnDeleteSlotPressed(2)));

        ShowMainPanel();
    }

    private void OnPlayPressed()
    {
        ShowSlotPanel();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnBackPressed()
    {
        ShowMainPanel();
    }

    private void OnSlotPressed(int slot)
    {
        if (SaveManager.Instance == null) return;

        if (SaveManager.Instance.SlotExists(slot))
        {
            // Load existing save
            SaveManager.Instance.Load(slot);
            string levelId = PlayerState.Instance?.LastCheckpointLevelId ?? _startingLevelId;
            string scenePath = SaveManager.Instance.GetLevelScenePath(levelId);

            if (string.IsNullOrEmpty(scenePath))
                scenePath = _startingLevelScenePath;

            SaveManager.Instance.IsRespawning = true;
            GetTree().ChangeSceneToFile(scenePath);
        }
        else
        {
            // Create new game
            SaveManager.Instance.CreateNewGame(slot, _startingLevelId, _startingCheckpointId);
            GetTree().ChangeSceneToFile(_startingLevelScenePath);
        }
    }

    private void OnDeleteSlotPressed(int slot)
    {
        SaveManager.Instance?.DeleteSlot(slot);
        RefreshSlotDisplay();
    }

    private void ShowMainPanel()
    {
        if (_mainPanel != null) _mainPanel.Visible = true;
        if (_slotPanel != null) _slotPanel.Visible = false;
    }

    private void ShowSlotPanel()
    {
        if (_mainPanel != null) _mainPanel.Visible = false;
        if (_slotPanel != null) _slotPanel.Visible = true;
        RefreshSlotDisplay();
    }

    private void RefreshSlotDisplay()
    {
        for (int i = 0; i < SaveManager.MaxSlots; i++)
        {
            var button = i switch { 0 => _slot0Button, 1 => _slot1Button, _ => _slot2Button };
            var deleteButton = i switch { 0 => _deleteSlot0Button, 1 => _deleteSlot1Button, _ => _deleteSlot2Button };

            if (button == null) continue;

            if (SaveManager.Instance?.SlotExists(i) == true)
            {
                var preview = SaveManager.Instance.GetSlotPreview(i);
                button.Text = $"Slot {i + 1} - Coins: {preview?.Coins ?? 0}";
                if (deleteButton != null) deleteButton.Visible = true;
            }
            else
            {
                button.Text = $"Slot {i + 1} - Empty";
                if (deleteButton != null) deleteButton.Visible = false;
            }
        }
    }
}
