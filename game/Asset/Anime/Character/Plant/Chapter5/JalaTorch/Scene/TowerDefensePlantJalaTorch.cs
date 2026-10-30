using Godot;

[Tool]
public partial class TowerDefensePlantJalaTorch : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ItemMegaFire");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		for (int i = 0; i < mapGridNum.Y; i++)
		{
			bool flag = true;
			if (i == 0)
			{
				PlantMegaFire(base.gridPos);
				flag = false;
			}
			else
			{
				if (base.gridPos.Y - i > 0)
				{
					PlantMegaFire(base.gridPos - new Vector2I(0, i));
					flag = false;
				}
				if (base.gridPos.Y + i <= mapGridNum.Y)
				{
					PlantMegaFire(base.gridPos + new Vector2I(0, i));
					flag = false;
				}
			}
			if (flag)
			{
				break;
			}
		}
		Destroy();
		void PlantMegaFire(Vector2I targetGridPos)
		{
			TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? packetConfig.Plant(base.EconomyOwnerAccountId, targetGridPos) : packetConfig.Plant(targetGridPos));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				TowerDefenseManager.PublishSpawnedCharacter("ItemMegaFire", towerDefenseCharacter, useCreate: false);
			}
		}
	}
}
