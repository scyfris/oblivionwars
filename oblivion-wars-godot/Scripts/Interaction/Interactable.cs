using Godot;

public abstract partial class Interactable : Area2D
{
    [Export] public string PromptText = "Interact";

    public abstract void Interact(PlayerCharacterBody2D player);
}
