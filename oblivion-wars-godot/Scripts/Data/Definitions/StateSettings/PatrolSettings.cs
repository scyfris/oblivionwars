using Godot;

[GlobalClass]
public partial class PatrolSettings : Resource
{
    [Export] public float PatrolRadius = 150f;
    [Export] public float IdlePauseMin = 0.5f;
    [Export] public float IdlePauseMax = 2.0f;
}
