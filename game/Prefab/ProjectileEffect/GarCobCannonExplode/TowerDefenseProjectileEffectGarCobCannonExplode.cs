using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectGarCobCannonExplode : TowerDefenseProjectileEffectBase
{
	private static readonly (string packetName, int weight)[] GiantZombies = new(string, int)[5]
	{
		("ZombieGargantuar", 60),
		("ZombieGargantuarRedEyes", 20),
		("ZombieFootballGargantuar", 13),
		("ZombieFootballGargantuarBlack", 5),
		("ZombieDiscoGargantuar", 2)
	};

	public override async void _Ready()
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("VaseZombie");
		if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(packetConfig))
		{
			TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig(SelectGiantZombie());
			if (GodotObject.IsInstanceValid(packetConfig2))
			{
				TowerDefensePacketConfig hypnotizedConfig = packetConfig2.Duplicate(deep: true) as TowerDefensePacketConfig;
				if (GodotObject.IsInstanceValid(hypnotizedConfig))
				{
					hypnotizedConfig.coldDownDecreaseDictionary = new Dictionary();
					hypnotizedConfig.overrideHypnoses = true;
					TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(gridPos, playAudio: true, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
					if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter is TowerDefenseVase zombieVase)
					{
						await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
						if (GodotObject.IsInstanceValid(zombieVase))
						{
							zombieVase.SetContentConfig(hypnotizedConfig);
						}
					}
				}
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		QueueFree();
	}

	private string SelectGiantZombie()
	{
		int num = 0;
		(string, int)[] giantZombies = GiantZombies;
		for (int i = 0; i < giantZombies.Length; i++)
		{
			int item = giantZombies[i].Item2;
			num += item;
		}
		int num2 = GD.RandRange(0, num - 1);
		int num3 = 0;
		giantZombies = GiantZombies;
		for (int i = 0; i < giantZombies.Length; i++)
		{
			(string, int) tuple = giantZombies[i];
			string item2 = tuple.Item1;
			int item3 = tuple.Item2;
			num3 += item3;
			if (num2 < num3)
			{
				return item2;
			}
		}
		return GiantZombies[0].packetName;
	}
}
