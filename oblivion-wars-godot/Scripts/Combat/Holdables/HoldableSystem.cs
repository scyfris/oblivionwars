using Godot;

public partial class HoldableSystem : Node
{
    [ExportGroup("Debug scene Weapons (to visualize - override for tesing)")]
    [Export] public bool UseDebugWeaponScenes = false;
    [Export] private PackedScene _leftHoldableSceneDebug;
    [Export] private PackedScene _rightHoldableSceneDebug;

    [ExportGroup("Positioning")]
    [Export] private Node2D _weaponPositionNode;
    public Vector2 WeaponGlobalPosition => _weaponPositionNode != null ? _weaponPositionNode.GlobalPosition : _owner.GlobalPosition;


    private Holdable _leftHoldable;
    private Holdable _rightHoldable;
    private Node2D _owner;

    public void Initialize(Node2D owner)
    {
        _owner = owner;

        // If UseDebugWeaponScenes is true, use scene weapons for visual testing/positioning
        // Otherwise, InitializeWithDefinition will be called by the entity
        if (UseDebugWeaponScenes)
        {
            if (_leftHoldableSceneDebug == null && _rightHoldableSceneDebug == null)
            {
                GD.Print($"[HoldableSystem] '{owner.Name}': No holdable scenes assigned in the inspector. Weapons may be assigned at runtime via SwapLeft/SwapRight.");
                return;
            }

            if (_leftHoldableSceneDebug != null)
                _leftHoldable = InstantiateHoldable(_leftHoldableSceneDebug);

            if (_rightHoldableSceneDebug != null)
                _rightHoldable = InstantiateHoldable(_rightHoldableSceneDebug);
        }
    }

    public void InitializeWithDefinition(Node2D owner, CharacterDefinition definition)
    {
        _owner = owner;

        if (definition == null)
        {
            GD.PrintErr($"[HoldableSystem] '{owner.Name}': InitializeWithDefinition called with null definition. Assign a CharacterDefinition to the entity.");
            return;
        }

        if (definition.LeftWeapon == null && definition.RightWeapon == null)
        {
            GD.PrintErr($"[HoldableSystem] '{owner.Name}': CharacterDefinition '{definition.ResourceName}' has no weapons assigned (LeftWeapon / RightWeapon). Assign WeaponRegistryEntry resources in the definition.");
            return;
        }

        if (definition.LeftWeapon != null)
        {
            if (definition.LeftWeapon.Scene == null)
                GD.PrintErr($"[HoldableSystem] '{owner.Name}': LeftWeapon '{definition.LeftWeapon.Name}' has no Scene assigned in its WeaponRegistryEntry.");
            else
                _leftHoldable = InstantiateHoldable(definition.LeftWeapon.Scene);
        }

        if (definition.RightWeapon != null)
        {
            if (definition.RightWeapon.Scene == null)
                GD.PrintErr($"[HoldableSystem] '{owner.Name}': RightWeapon '{definition.RightWeapon.Name}' has no Scene assigned in its WeaponRegistryEntry.");
            else
                _rightHoldable = InstantiateHoldable(definition.RightWeapon.Scene);
        }
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

    public void SwapLeft(PackedScene newScene)
    {
        if (_leftHoldable != null)
        {
            _leftHoldable.OnUnequip();
            _leftHoldable.QueueFree();
            _leftHoldable = null;
        }

        if (newScene != null)
        {
            _leftHoldableSceneDebug = newScene;
            _leftHoldable = InstantiateHoldable(newScene);
        }
    }

    public void SwapRight(PackedScene newScene)
    {
        if (_rightHoldable != null)
        {
            _rightHoldable.OnUnequip();
            _rightHoldable.QueueFree();
            _rightHoldable = null;
        }

        if (newScene != null)
        {
            _rightHoldableSceneDebug = newScene;
            _rightHoldable = InstantiateHoldable(newScene);
        }
    }

    private Holdable InstantiateHoldable(PackedScene scene)
    {
        var instance = scene.Instantiate<Holdable>();
        var parent = _weaponPositionNode != null ? (Node)_weaponPositionNode : this;
        parent.AddChild(instance);
        instance.InitOwner(_owner);
        instance.OnEquip();
        return instance;
    }
}
