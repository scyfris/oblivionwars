using Godot;

public partial class CoinPickup : Pickup
{
    [Export] public int Value = 1;

    protected override void OnCollected(PlayerController player)
    {
        player.PlayerStateCurrent.Coins += Value;

        EventBus.Instance?.Raise(new ItemCollectedEvent
        {
            ItemType = "coin",
            Quantity = Value
        });
    }
}
