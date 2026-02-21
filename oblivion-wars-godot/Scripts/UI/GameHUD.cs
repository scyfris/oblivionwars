using Godot;

public partial class GameHUD : CanvasLayer
{
    [Export] private Label _healthLabel;
    [Export] private Label _coinLabel;
    [Export] private Label _weaponLabel;
    [Export] private Label _interactionPrompt;
    [Export] private SaveIndicator _saveIndicator;

    private PlayerController _playerController;

    public override void _Ready()
    {
        if (_interactionPrompt != null)
            _interactionPrompt.Visible = false;

        // todo - just stubs for now, they could trigger affects or something...
        EventBus.Instance?.Subscribe<DamageAppliedEvent>(OnDamageApplied);
        EventBus.Instance?.Subscribe<ItemCollectedEvent>(OnItemCollected);
        EventBus.Instance?.Subscribe<WeaponSwitchedEvent>(OnWeaponSwitched);

        PlayerCharacterBody2D playerbody = GetTree().GetFirstNodeInGroup(Groups.Entities.Player) as PlayerCharacterBody2D;

        if (playerbody == null)
        {
            GD.PrintErr("Can't find player node!");
        }
        _playerController = playerbody.Controller;

        UpdateHealthDisplay();
        UpdateCoinDisplay();
        UpdateWeaponDisplay();
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
        UpdateHealthDisplay();
        UpdateCoinDisplay();
        UpdateWeaponDisplay();
    }

    private void OnDamageApplied(DamageAppliedEvent evt)
    {
    }

    private void OnItemCollected(ItemCollectedEvent evt)
    {
    }

    private void UpdateHealthDisplay()
    {
        if (_healthLabel == null) return;

        _healthLabel.Text = $"HP: {_playerController.PlayerStateCurrent.CurrentHealth:F0}/{_playerController.PlayerStateCurrent.MaxHealth:F0}";
    }

    private void UpdateCoinDisplay()
    {
        if (_coinLabel == null) return;
        _coinLabel.Text = $"Coins: {GlobalStateManager.Instance.Player?.Coins ?? 0}";
    }

    private void UpdateInteractionPrompt()
    {
        if (_interactionPrompt == null || _playerController == null) return;

        var interactable = _playerController.CharacterBody.NearestInteractable;
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
//        UpdateWeaponDisplay(evt.NewWeaponId);
    }

    private void UpdateWeaponDisplay()
    {

        if (_weaponLabel == null) return;

        string weaponId = GlobalStateManager.Instance.Player.CurrentWeaponId;

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
