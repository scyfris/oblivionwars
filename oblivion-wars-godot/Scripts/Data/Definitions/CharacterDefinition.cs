using Godot;


[GlobalClass]
public partial class CharacterDefinition : Resource
{
    [ExportGroup("Identity")]
    [Export] public string EntityId = "";


    [ExportGroup("Stats")]
    [Export] public float MaxHealth = 100.0f;
    [Export] public float MoveSpeed = 150.0f;
//    [Export] public float KnockbackResistance = 0.0f;

    [ExportGroup("Movement")]
    [Export] public float JumpStrength = 800.0f;
    [Export] public float WallJumpStrength = 700.0f;
    [Export] public float WallJumpPushAwayForce = 500.0f;
    [Export] public float WallJumpPushAwayDuration = 0.2f;
    [Export] public float WallJumpInputLockDuration = 0.2f;
    [Export] public float WallSlideSpeedFraction = 0.5f;

    [ExportGroup("Loadout")]
    [Export] public PackedScene LeftHoldableScene;
    [Export] public PackedScene RightHoldableScene;

    [ExportGroup("Physics")]
    [Export] public CommonPhysicsDef PhysicsDef;
}
