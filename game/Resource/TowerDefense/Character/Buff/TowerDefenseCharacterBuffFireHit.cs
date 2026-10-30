using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffFireHit : TowerDefenseCharacterBuffConfig
{
	public override void _Init()
	{
		key = "FireHit";
	}

	public override void Enter()
	{
		character.buff.DeleteBuff("IceSpeedDown");
		character.buff.DeleteBuff("EMSpeedDown");
		character.buff.DeleteBuff("Frozen");
	}

	public override bool Step(double delta)
	{
		return true;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
	}
}
