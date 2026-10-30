using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventArmorClear : TowerDefenseCharacterEventBase
{
	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public static void Run(TowerDefenseCharacter target)
	{
		target.instance.ArmorClear();
	}
}
