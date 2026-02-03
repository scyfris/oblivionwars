using Godot;

public partial class AutoFreeParticle : CpuParticles2D
{
    public override void _Ready()
    {
        Emitting = true;
        OneShot = true;
    }

    public override void _Process(double delta)
    {
        if (!Emitting)
            QueueFree();
    }
}
