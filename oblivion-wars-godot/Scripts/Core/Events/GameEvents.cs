public struct SaveCompletedEvent : IGameEvent
{
    public int SlotIndex;
}

public struct ItemCollectedEvent : IGameEvent
{
    public string ItemType;
    public int Quantity;
}
