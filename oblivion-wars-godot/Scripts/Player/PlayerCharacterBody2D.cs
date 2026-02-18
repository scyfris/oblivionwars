using Godot;

public partial class PlayerCharacterBody2D : EntityCharacterBody2D
{
    [Export] private new PlayerDefinition _definition;

    [ExportGroup("Visuals")]
    [Export] private Node2D _flipRoot;
    [Export] private AnimatedSprite2D _spriteNode;
    [Export] private string _idleAnimation = "default";
    [Export] private string _walkFacingDirAnimation = "walk-facingdir";
    [Export] private string _walkNonFacingDirAnimation = "walk-nonfacingdir";

    [ExportGroup("Wall Slide Effects")]
    [Export] private Node2D _wallSlideDustPosition;
    [Export] private PackedScene _wallSlideDustScene;

    private CpuParticles2D _wallSlideDust;
    private bool _facingRight = true;

    // Set by controller each frame for animation
    public Vector2 AimTarget { get; set; }

    // Interaction (body stores reference since it's the physical object that overlaps)
    private Interactable _nearestInteractable;
    public Interactable NearestInteractable => _nearestInteractable;

    public override void _Ready()
    {
        AddToGroup(Groups.Entities.Player);

        base._definition = _definition;
        base._Ready();

        if (_wallSlideDustPosition != null && _wallSlideDustScene != null)
        {
            _wallSlideDust = _wallSlideDustScene.Instantiate<CpuParticles2D>();
            _wallSlideDust.Emitting = false;
            _wallSlideDustPosition.AddChild(_wallSlideDust);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateAnimation();
    }

    // Wall slide dust particle management
    protected override void SetWallSliding(bool sliding)
    {
        base.SetWallSliding(sliding);
        if (_wallSlideDust != null)
        {
            _wallSlideDust.Emitting = sliding;
            if (sliding)
                _wallSlideDust.Direction = _wallNormal;
        }
    }

    // ── Animation ─────────────────────────────────────────

    private void UpdateAnimation()
    {
        if (_spriteNode == null) return;

        if (_moveDirection != 0)
            _facingRight = _moveDirection > 0;

        if (_flipRoot != null)
            _flipRoot.Scale = new Vector2(_facingRight ? 1 : -1, 1);

        if (_moveDirection != 0 && IsOnFloor())
        {
            Vector2 horizontalDir = new Vector2(_gravityDirection.Y, -_gravityDirection.X);
            float aimDot = (AimTarget - GlobalPosition).Dot(horizontalDir);
            bool aimToLocalRight = aimDot > 0;
            bool movingTowardAim = _facingRight == aimToLocalRight;

            _spriteNode.Play(movingTowardAim ? _walkFacingDirAnimation : _walkNonFacingDirAnimation);
        }
        else
        {
            _spriteNode.Play(_idleAnimation);
        }
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
