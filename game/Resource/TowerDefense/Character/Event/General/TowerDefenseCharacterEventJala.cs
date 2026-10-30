using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventJala : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double num = 200.0;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventTargetList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> allEventList = new Array<TowerDefenseCharacterEventBase>();

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		}
		Run(target, num, camp, eventTargetList, allEventList);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		if (target.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		}
		Run(target, num, camp, eventTargetList, allEventList);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, num, projectile.camp, eventTargetList, allEventList);
	}

	public static void Run(TowerDefenseCharacter target, double num = 200.0, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT, Array<TowerDefenseCharacterEventBase> eventTargetList = null, Array<TowerDefenseCharacterEventBase> allEventList = null)
	{
		if (eventTargetList == null)
		{
			eventTargetList = new Array<TowerDefenseCharacterEventBase>();
		}
		if (allEventList == null)
		{
			allEventList = new Array<TowerDefenseCharacterEventBase>();
		}
		TowerDefenseCharacter.CreateJalapenoFire(camp, target.gridPos, num, eventTargetList, allEventList);
	}
}
