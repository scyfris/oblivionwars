using Godot;

public partial class PlayerCharacterBody2D : EntityCharacterBody2D
{
    [Export]
    private PlayerController _playerController;
    public PlayerController Controller => _playerController;

    // Interaction (body stores reference since it's the physical object that overlaps)
    private Interactable _nearestInteractable;
    public Interactable NearestInteractable => _nearestInteractable;

    public override void _Ready()
    {
        if (_playerController == null)
        {
            GD.PrintErr("Player Character Body2d must have a reference to its player controller");
        }
        AddToGroup(Groups.Entities.Player);

        base._Ready();

    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }

    // ── Interaction (thin storage only) ───────────────────

    public void SetNearestInteractable(Interactable interactable)
    {
        _nearestInteractable = interactable;
    }

    public void ClearInteractable(Interactable interactable)
    {
        if (_nearestInteractable == interactable)
            _nearestInteractable = null;
    }
}
