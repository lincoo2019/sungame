using Godot;

public partial class GoldMelonLuckyDrawProbeEvent : TowerDefenseCharacterEventBase
{
	public int ProjectDispatchCount { get; private set; }

	public int PositionDispatchCount { get; private set; }

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		PositionDispatchCount++;
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		ProjectDispatchCount++;
	}
}
