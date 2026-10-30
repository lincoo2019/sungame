using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventConditionRandom : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public double percentage = 0.3;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, eventList, percentage);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, eventList, percentage);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		if ((double)GD.Randf() > percentage)
		{
			return;
		}
		foreach (TowerDefenseCharacterEventBase @event in eventList)
		{
			@event?.ExecuteProject(projectile, target);
		}
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, Array<TowerDefenseCharacterEventBase> eventList, double percentage)
	{
		if ((double)GD.Randf() > percentage)
		{
			return;
		}
		foreach (TowerDefenseCharacterEventBase @event in eventList)
		{
			@event.Execute(pos, target);
		}
	}
}
