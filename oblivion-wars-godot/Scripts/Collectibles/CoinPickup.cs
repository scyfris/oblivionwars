using Godot;

public partial class CoinPickup : Pickup
{
    [Export] public int Value = 1;

    protected override void OnCollected(PlayerCharacterBody2D player)
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.Coins += Value;

        EventBus.Instance?.Raise(new ItemCollectedEvent
        {
            ItemType = "coin",
            Quantity = Value
        });
    }
}
