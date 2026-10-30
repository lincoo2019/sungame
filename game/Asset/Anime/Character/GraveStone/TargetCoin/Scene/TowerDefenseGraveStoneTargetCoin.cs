using Godot;

[Tool]
public partial class TowerDefenseGraveStoneTargetCoin : TowerDefenseGravestone
{
	public override async void DestroySet()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		double height = GetGroundHeight(logicalGlobalPosition.Y);
		if ((Global.IsEditor && Global.Instance.enterLevelMode == "DiyLevel") || Global.Instance.enterLevelMode == "LoadLevel" || Global.Instance.enterLevelMode == "OnlineLevel")
		{
			return;
		}
		if ((double)GD.Randf() > 0.0025)
		{
			if ((double)GD.Randf() > 0.5)
			{
				TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
				if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
				{
					towerDefenseGroundItemBase.gridPos = base.gridPos;
				}
			}
			else
			{
				for (int i = 0; i < 5; i++)
				{
					TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
					if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase2))
					{
						towerDefenseGroundItemBase2.gridPos = base.gridPos;
					}
				}
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
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
