using Godot;

public partial class HealthPickup : Pickup
{
    [Export] public float HealAmount = 25f;

    protected override void OnCollected(PlayerCharacterBody2D player)
    {
        if (player.RuntimeData != null)
        {
            player.RuntimeData.CurrentHealth = Mathf.Min(
                player.RuntimeData.CurrentHealth + HealAmount,
                player.RuntimeData.MaxHealth
            );

            if (PlayerState.Instance != null)
                PlayerState.Instance.CurrentHealth = player.RuntimeData.CurrentHealth;
        }

        EventBus.Instance?.Raise(new ItemCollectedEvent
        {
            ItemType = "health",
            Quantity = (int)HealAmount
        });
    }
}
