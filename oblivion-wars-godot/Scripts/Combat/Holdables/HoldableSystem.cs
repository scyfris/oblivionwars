using Godot;

public partial class HoldableSystem : Node
{
    [ExportGroup("Positioning")]
    [Export] private Node2D _weaponPositionNode;
    public Vector2 WeaponGlobalPosition => _weaponPositionNode != null ? _weaponPositionNode.GlobalPosition : _owner.GlobalPosition;

    private Holdable _leftHoldable;
    private Holdable _rightHoldable;
    private Node2D _owner;

    public void Initialize(Node2D owner, CharacterDefinition definition)
    {
        _owner = owner;

        if (definition == null)
        {
            GD.PrintErr($"[HoldableSystem] '{owner.Name}': Initialize called with null definition. Assign a CharacterDefinition to the entity.");
            return;
        }

        if (definition.LeftWeapon == null && definition.RightWeapon == null)
        {
            GD.PrintErr($"[HoldableSystem] '{owner.Name}': CharacterDefinition '{definition.ResourceName}' has no weapons assigned (LeftWeapon / RightWeapon). Assign WeaponRegistryEntry resources in the definition.");
            return;
        }

        if (definition.LeftWeapon != null)
            _leftHoldable = InstantiateHoldable(definition.LeftWeapon);

        if (definition.RightWeapon != null)
            _rightHoldable = InstantiateHoldable(definition.RightWeapon);
    }

    public void Update(double delta)
    {
        _leftHoldable?.Update(delta);
        _rightHoldable?.Update(delta);
    }

    public void UpdateAim(Vector2 target)
    {
        _leftHoldable?.UpdateAim(target);
        _rightHoldable?.UpdateAim(target);
    }

    public void PressLeft() { _leftHoldable?.OnUsePressed(); }
    public void PressRight() { _rightHoldable?.OnUsePressed(); }
    public void HeldLeft() { _leftHoldable?.OnUseHeld(); }
    public void HeldRight() { _rightHoldable?.OnUseHeld(); }
    public void ReleaseLeft() { _leftHoldable?.OnUseReleased(); }
    public void ReleaseRight() { _rightHoldable?.OnUseReleased(); }

    public void SwapLeft(WeaponRegistryEntry entry)
    {
        if (_leftHoldable != null)
        {
            _leftHoldable.OnUnequip();
            _leftHoldable.QueueFree();
            _leftHoldable = null;
        }

        if (entry != null)
            _leftHoldable = InstantiateHoldable(entry);
    }

    public void SwapRight(WeaponRegistryEntry entry)
    {
        if (_rightHoldable != null)
        {
            _rightHoldable.OnUnequip();
            _rightHoldable.QueueFree();
            _rightHoldable = null;
        }

        if (entry != null)
            _rightHoldable = InstantiateHoldable(entry);
    }

    private Holdable InstantiateHoldable(WeaponRegistryEntry entry)
    {
        if (entry.Scene == null)
        {
            GD.PrintErr($"[HoldableSystem] '{_owner.Name}': WeaponRegistryEntry '{entry.Name}' has no Scene assigned.");
            return null;
        }

        if (entry.Definition == null)
        {
            GD.PrintErr($"[HoldableSystem] '{_owner.Name}': WeaponRegistryEntry '{entry.Name}' has no Definition assigned.");
            return null;
        }

        var instance = entry.Scene.Instantiate<Holdable>();
        var parent = _weaponPositionNode != null ? (Node)_weaponPositionNode : this;
        parent.AddChild(instance);
        instance.InitOwner(_owner);

        if (instance is Weapon weapon)
            weapon.SetDefinition(entry.Definition);

        instance.OnEquip();
        return instance;
    }
}
