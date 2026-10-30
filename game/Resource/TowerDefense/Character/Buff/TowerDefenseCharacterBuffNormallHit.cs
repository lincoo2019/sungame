using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffNormallHit : TowerDefenseCharacterBuffConfig
{
	public override void _Init()
	{
		key = "NormalHit";
	}

	public override void Enter()
	{
		character.buff.DeleteBuff("IceSpeedDown");
		character.buff.DeleteBuff("Frozen");
		character.buff.DeleteBuff("RedHeat");
		character.buff.DeleteBuff("Cherry");
		character.buff.DeleteBuff("Poisoning");
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
