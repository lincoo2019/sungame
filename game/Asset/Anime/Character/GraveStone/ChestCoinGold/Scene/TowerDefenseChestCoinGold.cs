using Godot;

[Tool]
public partial class TowerDefenseChestCoinGold : TowerDefenseGravestone
{
	private const string CHEST_COIN_GOLD_WATER_1 = "uid://d1wt56mp0hydp";

	private const string CHEST_COIN_GOLD_WATER_2 = "uid://d1o4b7kg3vm02";

	private const string CHEST_COIN_GOLD_WATER_3 = "uid://nf58xu68y4iy";

	private const string CHEST_COIN_GOLD_WATER_4 = "uid://cuj1fsexq1mdg";

	private const string CHEST_COIN_GOLD_WATER_5 = "uid://wgwkgft45log";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			AddToGroup("ChestCoin", persistent: true);
			RemoveFromGroup("Gravestone");
			if (GodotObject.IsInstanceValid(cell) && cell.isWater)
			{
				shadowSprite.Visible = false;
				base.sprite.SetAtlasReplace("CoinChest2_1.png", "uid://d1wt56mp0hydp");
			}
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (GodotObject.IsInstanceValid(cell) && cell.isWater)
		{
			switch (damagePointName)
			{
			case "Damage0":
				base.sprite.SetAtlasReplace("CoinChest2_1.png", "uid://d1wt56mp0hydp");
				break;
			case "Damage1":
				base.sprite.SetAtlasReplace("CoinChest2_1.png", "uid://d1o4b7kg3vm02");
				break;
			case "Damage2":
				base.sprite.SetAtlasReplace("CoinChest2_1.png", "uid://nf58xu68y4iy");
				break;
			case "Damage3":
				base.sprite.SetAtlasReplace("CoinChest2_1.png", "uid://cuj1fsexq1mdg");
				break;
			case "Damage4":
				base.sprite.SetAtlasReplace("CoinChest2_1.png", "uid://wgwkgft45log");
				break;
			}
		}
	}

	public override async void DestroySet()
	{
		if ((Global.IsEditor && Global.Instance.enterLevelMode == "DiyLevel") || Global.Instance.enterLevelMode == "LoadLevel" || Global.Instance.enterLevelMode == "OnlineLevel")
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		double height = GetGroundHeight(logicalGlobalPosition.Y);
		if ((double)GD.Randf() > 0.0025)
		{
			for (int i = 0; i < 4; i++)
			{
				TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
				if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
				{
					towerDefenseGroundItemBase.gridPos = base.gridPos;
				}
			}
		}
		else
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase2))
			{
				towerDefenseGroundItemBase2.gridPos = base.gridPos;
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
