using Godot;

[GlobalClass]
public partial class HazardDefinition : Resource
{
	[Export] public float SpikeDamage = 10.0f;
	[Export] public float LavaDamage = 20.0f;
	[Export] public float AcidDamage = 15.0f;

	public float GetDamage(TileHazardType type)
	{
		return type switch
		{
			TileHazardType.Spikes => SpikeDamage,
			TileHazardType.Lava => LavaDamage,
			TileHazardType.Acid => AcidDamage,
			_ => 0f
		};
	}
}
