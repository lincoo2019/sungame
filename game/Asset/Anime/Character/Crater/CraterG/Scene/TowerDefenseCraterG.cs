using Godot;

[Tool]
public partial class TowerDefenseCraterG : TowerDefenseCrater
{
	private CharacterTimerComponent timerComponent;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			timerComponent.OnTimeout += Timeout;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent characterTimerComponent = timerComponent;
		if (characterTimerComponent != null && !characterTimerComponent.IsReleased)
		{
			timerComponent.OnTimeout -= Timeout;
		}
	}

	public void Timeout(string timerName)
	{
		if (!(timerName == "Spawn"))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 logicalGlobalPosition2 = GetLogicalGlobalPosition(spriteGroup);
		double height = GetGroundHeight(logicalGlobalPosition.Y);
		double num = GD.Randf();
		if (num < 0.45)
		{
			SunCreate(logicalGlobalPosition2, 15L);
		}
		else if (num < 0.65)
		{
			SunCreate(logicalGlobalPosition2, 25L);
		}
		else if (num < 0.7)
		{
			SunCreate(logicalGlobalPosition2, 50L);
		}
		else if (num < 0.94)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
			{
				towerDefenseGroundItemBase.gridPos = base.gridPos;
			}
		}
		else if (num < 0.99)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase2))
			{
				towerDefenseGroundItemBase2.gridPos = base.gridPos;
			}
		}
		else
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase3 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase3))
			{
				towerDefenseGroundItemBase3.gridPos = base.gridPos;
			}
		}
		timerComponent.Run("Spawn", 3.0);
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !timerComponent.IsRunning("Spawn"))
		{
			timerComponent.Run("Spawn", 3.0);
		}
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			double num = GD.Randf();
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(spriteGroup);
			if (num < 0.8)
			{
				SpawnPacket(TowerDefenseManager.GetPacketConfig("PlantSunBomb"), logicalGlobalPosition, 15.0, isFall: false);
			}
			else
			{
				string packetName = (string)TowerDefenseManager.GetPacketBankData("GeneralPlant").GetCategory("Gold").PickRandom();
				SpawnPacket(TowerDefenseManager.GetPacketConfig(packetName), logicalGlobalPosition, 15.0, isFall: false);
			}
			Destroy();
		}
	}
}
