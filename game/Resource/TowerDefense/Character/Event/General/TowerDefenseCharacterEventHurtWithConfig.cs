using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventHurtWithConfig : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public AttackConfig AttackConfig = new AttackConfig();

	[Export(PropertyHint.None, "")]
	public bool playSplatAudio = true;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, AttackConfig, playSplatAudio);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, AttackConfig, playSplatAudio);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, target, AttackConfig, playSplatAudio);
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, AttackConfig AttackConfig, bool playSplatAudio = true)
	{
		target.HurtWithAttackConfig(AttackConfig, playSplatAudio);
		target.AttackDeal(null, "Default", AttackConfig.num);
	}
}
