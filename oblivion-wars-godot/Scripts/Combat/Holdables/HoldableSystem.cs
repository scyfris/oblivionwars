using Godot;

public partial class HoldableSystem : Node
{
    [ExportGroup("Scene Weapons (For Visual Testing)")]
    [Export] private PackedScene _leftHoldableScene;
    [Export] private PackedScene _rightHoldableScene;
    [Export] private Node2D _weaponPosition;

    [ExportGroup("Runtime Behavior")]
    [Export] public bool UseDefinitionWeapons = false;

    private Holdable _leftHoldable;
    private Holdable _rightHoldable;
    private Node2D _owner;

    public void Initialize(Node2D owner)
    {
        _owner = owner;

        // If UseDefinitionWeapons is true, Initialize will be called with definition by the entity
        // Otherwise, use scene weapons for visual testing/positioning
        if (!UseDefinitionWeapons)
        {
            if (_leftHoldableScene != null)
                _leftHoldable = InstantiateHoldable(_leftHoldableScene);

            if (_rightHoldableScene != null)
                _rightHoldable = InstantiateHoldable(_rightHoldableScene);
        }
    }

    public void InitializeWithDefinition(Node2D owner, CharacterDefinition definition)
    {
        _owner = owner;

        if (definition == null) return;

        if (definition.LeftHoldableScene != null)
            _leftHoldable = InstantiateHoldable(definition.LeftHoldableScene);

        if (definition.RightHoldableScene != null)
            _rightHoldable = InstantiateHoldable(definition.RightHoldableScene);
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

    public void PressLeft(Vector2 target) { _leftHoldable?.OnUsePressed(target); }
    public void PressRight(Vector2 target) { _rightHoldable?.OnUsePressed(target); }
    public void HeldLeft(Vector2 target) { _leftHoldable?.OnUseHeld(target); }
    public void HeldRight(Vector2 target) { _rightHoldable?.OnUseHeld(target); }
    public void ReleaseLeft(Vector2 target) { _leftHoldable?.OnUseReleased(target); }
    public void ReleaseRight(Vector2 target) { _rightHoldable?.OnUseReleased(target); }

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
            _leftHoldableScene = newScene;
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
            _rightHoldableScene = newScene;
            _rightHoldable = InstantiateHoldable(newScene);
        }
    }

    private Holdable InstantiateHoldable(PackedScene scene)
    {
        var instance = scene.Instantiate<Holdable>();
        var parent = _weaponPosition != null ? (Node)_weaponPosition : this;
        parent.AddChild(instance);
        instance.InitOwner(_owner);
        instance.OnEquip();
        return instance;
    }
}
