using Godot;
using System.Collections.Generic;

public partial class DebugOverlay : Control
{
    [Export] private string _toggleAction = "debug_menu";

    private VBoxContainer _weaponSection;
    private readonly Dictionary<string, CheckBox> _weaponCheckboxes = new();
    private Input.MouseModeEnum _savedMouseMode;

    public override void _Ready()
    {
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        BuildUI();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed(_toggleAction))
        {
            SetOpen(!Visible);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("debug_mode_toggle"))
        {
            var global = GlobalStateManager.Instance.Global;
            global.IsDebugModeEnabled = !global.IsDebugModeEnabled;
            GD.Print($"Debug mode: {(global.IsDebugModeEnabled ? "ON" : "OFF")}");
            GetViewport().SetInputAsHandled();
        }
    }

    private void SetOpen(bool open)
    {
        if (open)
        {
            _savedMouseMode = Input.MouseMode;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            Input.MouseMode = _savedMouseMode;
        }

        Visible = open;
        GetTree().Paused = open;

        if (open)
            RefreshAll();
    }

    // ── UI Construction ──────────────────────────────────────

    private void BuildUI()
    {
        // Dim background
        var bg = new ColorRect();
        bg.Color = new Color(0, 0, 0, 0.5f);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(bg);

        // Centered panel
        var panel = new PanelContainer();
        panel.AnchorLeft = 0.5f;
        panel.AnchorTop = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.OffsetLeft = -160;
        panel.OffsetTop = -200;
        panel.OffsetRight = 160;
        panel.OffsetBottom = 200;
        panel.GrowHorizontal = GrowDirection.Both;
        panel.GrowVertical = GrowDirection.Both;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        style.SetBorderWidthAll(2);
        style.BorderColor = new Color(0.4f, 0.4f, 0.4f);
        style.SetCornerRadiusAll(6);
        style.SetContentMarginAll(14);
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        panel.AddChild(root);

        // Title
        var title = new Label();
        title.Text = "Debug Menu";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(title);

        root.AddChild(new HSeparator());

        // Weapons section
        var weaponsLabel = new Label();
        weaponsLabel.Text = "Weapons";
        root.AddChild(weaponsLabel);

        _weaponSection = new VBoxContainer();
        _weaponSection.AddThemeConstantOverride("separation", 2);
        root.AddChild(_weaponSection);

        // Spacer
        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(spacer);

        // Close hint
        var hint = new Label();
        hint.Text = "Press = to close";
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        root.AddChild(hint);
    }

    // ── Refresh ──────────────────────────────────────────────

    private void RefreshAll()
    {
        RefreshWeapons();
    }

    private void RefreshWeapons()
    {
        foreach (var child in _weaponSection.GetChildren())
            child.QueueFree();
        _weaponCheckboxes.Clear();

        var weapons = GlobalDefinitions.Instance?.Weapons;
        if (weapons == null) return;

        var playerState = GlobalStateManager.Instance?.Player;
        if (playerState == null) return;

        foreach (var entry in weapons)
        {
            if (entry == null) continue;

            var cb = new CheckBox();
            cb.Text = entry.Name;
            cb.ButtonPressed = playerState.IsWeaponUnlocked(entry.Name);
            var weaponName = entry.Name;
            cb.Toggled += (pressed) => OnWeaponToggled(weaponName, pressed);
            _weaponSection.AddChild(cb);
            _weaponCheckboxes[entry.Name] = cb;
        }
    }

    // ── Weapon Toggle ────────────────────────────────────────

    private void OnWeaponToggled(string weaponName, bool unlocked)
    {
        var playerState = GlobalStateManager.Instance?.Player;
        if (playerState == null) return;

        if (unlocked)
        {
            playerState.UnlockWeapon(weaponName, -1);
        }
        else
        {
            // If locking the current weapon, switch to another first
            if (playerState.CurrentWeaponId == weaponName)
            {
                string fallback = "";
                foreach (var w in playerState.GetUnlockedWeapons())
                {
                    if (w != weaponName) { fallback = w; break; }
                }

                if (string.IsNullOrEmpty(fallback))
                {
                    // Can't lock the last weapon — revert checkbox
                    if (_weaponCheckboxes.TryGetValue(weaponName, out var cb))
                        cb.SetPressedNoSignal(true);
                    return;
                }

                EventBus.Instance?.Raise(new ForceWeaponSelectEvent { WeaponId = fallback });
            }

            playerState.LockWeapon(weaponName);
        }
    }
}
