using Godot;

/// <summary>
/// Temporary placeholder explosion visual. Draws a red circle that fades out over 1 second.
/// Replace with particles/animation later.
/// </summary>
public partial class ExplosionEffect : Node2D
{
    private const float Duration = 0.4f;

    public float Radius = 100f;

    private float _timer;
    private float _alpha = 0.6f;

    public override void _Ready()
    {
        _timer = Duration;
        ZIndex = 10;
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer <= 0f)
        {
            QueueFree();
            return;
        }

        _alpha = 0.6f * (_timer / Duration);
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, Radius, new Color(1f, 0.15f, 0.1f, _alpha));
    }
}
