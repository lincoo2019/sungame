using Godot;

[Tool]
public partial class TowerDefenseItemRake : TowerDefenseItemRakeBase
{
	protected override void ApplyHitEffect()
	{
		attackComponent.AttackExecute(10000.0);
	}
}
