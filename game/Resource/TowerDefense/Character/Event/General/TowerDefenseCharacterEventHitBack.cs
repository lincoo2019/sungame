using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventHitBack : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double length = 1.0;

	[Export(PropertyHint.None, "")]
	public double time;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, length, time);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, length * delta, time * delta);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, target, length, time);
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, double length, double time)
	{
		target.BlowBack((target.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE) ? length : (0.0 - length), time);
	}
}
