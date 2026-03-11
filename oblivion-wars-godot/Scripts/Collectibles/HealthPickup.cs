using Godot;

public partial class HealthPickup : Pickup
{
    [Export] public float HealAmount = 25f;

    protected override void OnCollected(PlayerController player)
    {
        if (player != null)
        {
            player.PlayerStateCurrent.CurrentHealth = Mathf.Min(
                player.PlayerStateCurrent.CurrentHealth + HealAmount,
                player.PlayerStateCurrent.MaxHealth
            );

            if (GlobalStateManager.Instance.Player != null)
                GlobalStateManager.Instance.Player.CurrentHealth = player.PlayerStateCurrent.CurrentHealth;
        }

        EventBus.Instance?.Raise(new ItemCollectedEvent
        {
            ItemType = "health",
            Quantity = (int)HealAmount
        });
    }
}
