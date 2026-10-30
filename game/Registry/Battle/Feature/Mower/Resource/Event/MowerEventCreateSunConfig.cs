using Godot;

[GlobalClass]
public partial class MowerEventCreateSunConfig : MowerEventConfig
{
	[Export(PropertyHint.None, "")]
	public int num = 25;

	public override void Execute(TowerDefenseCharacter character)
	{
		if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
		{
			character.BrainSunCreate(character.GetLogicalGlobalPosition(), num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
		}
		else
		{
			character.SunCreate(character.GetLogicalGlobalPosition(), num, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
		}
	}
}
