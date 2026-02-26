using Godot;

public abstract partial class Holdable : Node2D
{
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

    protected virtual float GetUseCooldown() => 0.2f;

    public bool CanUse()
    {
        return _timeSinceLastUse >= GetUseCooldown();
    }

    protected void ResetCooldown()
    {
        _timeSinceLastUse = 0f;
    }

    public virtual void OnUsePressed() { }
    public virtual void OnUseReleased() { }
    public virtual void OnUseHeld() { }

    public virtual void UpdateAim(Vector2 targetPosition) { }

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
}
