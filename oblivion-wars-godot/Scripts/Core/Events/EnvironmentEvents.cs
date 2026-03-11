using Godot;

public struct HazardContactEvent : IGameEvent
{
    public ulong EntityInstanceId;
    public TileHazardType HazardType;
    public Vector2 Position;
}
