using Godot;

public abstract partial class Pickup : RigidBody2D
{
    [Export] public float LifetimeSeconds = 30f;

    private float _lifetimeTimer;
    private Area2D _pickupArea;

    public override void _Ready()
    {
        _lifetimeTimer = LifetimeSeconds;

        // Find the child Area2D for player detection
        foreach (var child in GetChildren())
        {
            if (child is Area2D area)
            {
                _pickupArea = area;
                _pickupArea.BodyEntered += OnPickupBodyEntered;
                break;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _lifetimeTimer -= (float)delta;
        if (_lifetimeTimer <= 0)
            QueueFree();
    }

    private void OnPickupBodyEntered(Node2D body)
    {
        if (body is PlayerCharacterBody2D player)
        {
            OnCollected(player);
            QueueFree();
        }
    }

    protected abstract void OnCollected(PlayerCharacterBody2D player);
}
