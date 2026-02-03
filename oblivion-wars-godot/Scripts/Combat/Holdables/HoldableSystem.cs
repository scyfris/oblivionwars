using Godot;

public partial class HoldableSystem : Node
{
    [Export] private WeaponDefinition _leftWeaponDefinition;
    [Export] private WeaponDefinition _rightWeaponDefinition;

    private Holdable _leftHoldable;
    private Holdable _rightHoldable;
    private Node2D _owner;

    public void Initialize(Node2D owner)
    {
        _owner = owner;

        if (_leftWeaponDefinition != null)
            _leftHoldable = InstantiateWeapon(_leftWeaponDefinition);

        if (_rightWeaponDefinition != null)
            _rightHoldable = InstantiateWeapon(_rightWeaponDefinition);
    }

    public void Update(double delta)
    {
        _leftHoldable?.Update(delta);
        _rightHoldable?.Update(delta);
    }

    public void PressLeft(Vector2 target) { _leftHoldable?.OnUsePressed(target); }
    public void PressRight(Vector2 target) { _rightHoldable?.OnUsePressed(target); }
    public void HeldLeft(Vector2 target) { _leftHoldable?.OnUseHeld(target); }
    public void HeldRight(Vector2 target) { _rightHoldable?.OnUseHeld(target); }
    public void ReleaseLeft(Vector2 target) { _leftHoldable?.OnUseReleased(target); }
    public void ReleaseRight(Vector2 target) { _rightHoldable?.OnUseReleased(target); }

    public void SwapLeft(WeaponDefinition newDefinition)
    {
        if (_leftHoldable != null)
        {
            _leftHoldable.OnUnequip();
            _leftHoldable.QueueFree();
            _leftHoldable = null;
        }

        if (newDefinition != null)
        {
            _leftWeaponDefinition = newDefinition;
            _leftHoldable = InstantiateWeapon(newDefinition);
        }
    }

    public void SwapRight(WeaponDefinition newDefinition)
    {
        if (_rightHoldable != null)
        {
            _rightHoldable.OnUnequip();
            _rightHoldable.QueueFree();
            _rightHoldable = null;
        }

        if (newDefinition != null)
        {
            _rightWeaponDefinition = newDefinition;
            _rightHoldable = InstantiateWeapon(newDefinition);
        }
    }

    private Holdable InstantiateWeapon(WeaponDefinition definition)
    {
        if (definition.WeaponScene == null)
        {
            GD.PrintErr($"HoldableSystem: WeaponDefinition '{definition.WeaponId}' has no WeaponScene assigned.");
            return null;
        }

        var instance = definition.WeaponScene.Instantiate<Weapon>();
        AddChild(instance);
        instance.InitWeapon(definition);
        instance.InitOwner(_owner);
        instance.OnEquip();
        return instance;
    }
}
