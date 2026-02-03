using Godot;

public abstract partial class Holdable : Node2D
{
    protected float _useCooldown = 0.2f;
    protected float _timeSinceLastUse = 999f;
    protected Node2D _owner;

    public virtual void InitOwner(Node2D owner)
    {
        _owner = owner;
    }

    public virtual void Update(double delta)
    {
        _timeSinceLastUse += (float)delta;
    }

    public bool CanUse()
    {
        return _timeSinceLastUse >= _useCooldown;
    }

    protected void ResetCooldown()
    {
        _timeSinceLastUse = 0f;
    }

    public virtual void OnUsePressed(Vector2 targetPosition) { }
    public virtual void OnUseReleased(Vector2 targetPosition) { }
    public virtual void OnUseHeld(Vector2 targetPosition) { }

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
}
