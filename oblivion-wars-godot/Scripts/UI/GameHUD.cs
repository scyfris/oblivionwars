using Godot;

public partial class GameHUD : CanvasLayer
{
    [Export] private Label _healthLabel;
    [Export] private Label _coinLabel;
    [Export] private Label _weaponLabel;
    [Export] private Label _interactionPrompt;
    [Export] private SaveIndicator _saveIndicator;

    [Export] private NodePath _playerPath;
    private PlayerCharacterBody2D _player;

    public override void _Ready()
    {
        if (_playerPath != null)
        {
            var node = GetNode(_playerPath);
            if (node is PlayerCharacterBody2D player)
                _player = player;
        }

        if (_interactionPrompt != null)
            _interactionPrompt.Visible = false;

        EventBus.Instance?.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Subscribe<ItemCollectedEvent>(OnItemCollected);
        EventBus.Instance?.Subscribe<WeaponSwitchedEvent>(OnWeaponSwitched);

        UpdateHealthDisplay();
        UpdateCoinDisplay();
        UpdateWeaponDisplay(GlobalStateManager.Instance?.Player?.CurrentWeaponId ?? "");
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Unsubscribe<ItemCollectedEvent>(OnItemCollected);
        EventBus.Instance?.Unsubscribe<WeaponSwitchedEvent>(OnWeaponSwitched);
    }

    public override void _Process(double delta)
    {
        UpdateInteractionPrompt();
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
        if (_player != null && evt.TargetInstanceId == _player.GetInstanceId())
            UpdateHealthDisplay();
    }

    private void OnItemCollected(ItemCollectedEvent evt)
    {
        if (evt.ItemType == "coin")
            UpdateCoinDisplay();
    }

    private void UpdateHealthDisplay()
    {
        if (_healthLabel == null) return;

        if (_player?.RuntimeData != null)
            _healthLabel.Text = $"HP: {_player.RuntimeData.CurrentHealth:F0}/{_player.RuntimeData.MaxHealth:F0}";
        else
            _healthLabel.Text = "HP: --";
    }

    private void UpdateCoinDisplay()
    {
        if (_coinLabel == null) return;
        _coinLabel.Text = $"Coins: {GlobalStateManager.Instance.Player?.Coins ?? 0}";
    }

    private void UpdateInteractionPrompt()
    {
        if (_interactionPrompt == null || _player == null) return;

        var interactable = _player.NearestInteractable;
        if (interactable != null)
        {
            _interactionPrompt.Visible = true;
            _interactionPrompt.Text = $"Press E to {interactable.PromptText}";
        }
        else
        {
            _interactionPrompt.Visible = false;
        }
    }

    // ── Weapon Display ──────────────────────────────────────

    private void OnWeaponSwitched(WeaponSwitchedEvent evt)
    {
        UpdateWeaponDisplay(evt.NewWeaponId);
    }

    private void UpdateWeaponDisplay(string weaponId)
    {
        if (_weaponLabel == null) return;

        if (string.IsNullOrEmpty(weaponId))
        {
            _weaponLabel.Text = "";
            return;
        }

        int ammo = GlobalStateManager.Instance?.Player?.GetAmmo(weaponId) ?? 0;
        string ammoText = ammo == -1 ? "INF" : ammo.ToString();
        _weaponLabel.Text = $"{weaponId} | {ammoText}";
    }

    public void ShowInteractionPrompt(string text)
    {
        if (_interactionPrompt == null) return;
        _interactionPrompt.Text = text;
        _interactionPrompt.Visible = true;
    }

    public void HideInteractionPrompt()
    {
        if (_interactionPrompt != null)
            _interactionPrompt.Visible = false;
    }
}
