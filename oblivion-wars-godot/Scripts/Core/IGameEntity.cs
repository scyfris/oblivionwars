using Godot;

public interface IGameEntity
{
    EntityRuntimeData RuntimeData { get; }
    CharacterDefinition Definition { get; }
    Node2D EntityNode { get; }
}
