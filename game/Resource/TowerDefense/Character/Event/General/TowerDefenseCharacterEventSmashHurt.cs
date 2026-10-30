using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventSmashHurt : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double num = 1800.0;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, num);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, num * delta);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, target, num);
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, double _num = 20.0)
	{
		Vector2 velocity = new Vector2((target.GetLogicalGlobalPosition().X - pos.X) * (float)GD.RandRange(3.0, 6.0), -1000f);
		target.SmashHurt(_num, playSplatAudio: true, velocity);
		target.AttackDeal(null, "Smash", _num);
	}
}
