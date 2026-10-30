using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventWakeUp : TowerDefenseCharacterEventBase
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

	public override void Init(Dictionary valueDictionary)
	{
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "WakeUp",
			["Value"] = new Dictionary()
		};
	}

	public static void Run(TowerDefenseCharacter target)
	{
		target.WakeUp();
	}
}
