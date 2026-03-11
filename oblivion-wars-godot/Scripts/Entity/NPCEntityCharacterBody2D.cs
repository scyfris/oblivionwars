using System.ComponentModel;
using Godot;

public partial class NPCEntityCharacterBody2D : EntityCharacterBody2D
{
    [Export]
    private NPCController _controller;
    public NPCController Controller => _controller;
    protected override IEntityController EntityController => _controller;

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }
}
