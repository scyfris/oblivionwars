using Godot;

[GlobalClass]
public partial class WeaponRegistryEntry : Resource
{
    [Export] public string Name = "";
    [Export] public PackedScene Scene;
    [Export] public string Description = ""; // TODO: use in weapon info menus
    [Export] public Texture2D Icon; // TODO: use in HUD weapon bar / inventory
}
