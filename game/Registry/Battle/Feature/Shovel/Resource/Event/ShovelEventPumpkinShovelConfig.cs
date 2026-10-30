using Godot;

[GlobalClass]
public partial class ShovelEventPumpkinShovelConfig : ShovelEventConfig
{
	public override void Execute(TowerDefenseCharacter character)
	{
		bool flag = false;
		bool flag2 = false;
		TowerDefenseCharacter towerDefenseCharacter = null;
		if (GodotObject.IsInstanceValid(character.cell))
		{
			towerDefenseCharacter = character.cell.GetSurround();
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				flag2 = true;
				if (towerDefenseCharacter == character)
				{
					flag = true;
				}
			}
		}
		if (!flag)
		{
			double transferHP = character.instance.hitpoints;
			if (flag2 && towerDefenseCharacter.config.name == "PlantPumpkin")
			{
				towerDefenseCharacter.instance.Health(transferHP);
			}
			else if (!flag2)
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantPumpkin");
				TowerDefenseCharacter pumpkin = packetConfig.Plant(character.gridPos);
				if (GodotObject.IsInstanceValid(pumpkin))
				{
					Callable.From(delegate
					{
						if (GodotObject.IsInstanceValid(pumpkin) && GodotObject.IsInstanceValid(pumpkin.instance))
						{
							pumpkin.instance.hitpoints = transferHP;
							pumpkin.instance.RefreshDamagePoint();
						}
					}).CallDeferred();
				}
			}
			character.ShovelDestroy();
		}
		else if (towerDefenseCharacter.config.name == "PlantPumpkin")
		{
			double percentage = Mathf.Min(1.0, 0.5 * (towerDefenseCharacter.instance.hitpoints / towerDefenseCharacter.config.hitpoints));
			towerDefenseCharacter.Recycle(percentage);
		}
	}
}
