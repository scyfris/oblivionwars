using Godot;

public partial class SaveIndicator : Control
{
    [Export] private float _displayDuration = 1.5f;

    private Tween _tween;

    public override void _Ready()
    {
        Modulate = new Color(1, 1, 1, 0);
        EventBus.Instance?.Subscribe<SaveCompletedEvent>(OnSaveCompleted);
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<SaveCompletedEvent>(OnSaveCompleted);
    }

    private void OnSaveCompleted(SaveCompletedEvent evt)
    {
        ShowSaveIcon();
    }

    private void ShowSaveIcon()
    {
        _tween?.Kill();
        _tween = CreateTween();

        // Fade in
        _tween.TweenProperty(this, "modulate:a", 1.0f, 0.2f);
        // Hold
        _tween.TweenInterval(_displayDuration);
        // Fade out
        _tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
    }
}
