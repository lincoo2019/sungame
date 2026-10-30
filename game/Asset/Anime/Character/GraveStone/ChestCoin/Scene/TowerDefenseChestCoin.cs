using Godot;

[Tool]
public partial class TowerDefenseChestCoin : TowerDefenseGravestone
{
	private const string CHEST_COIN_WATER_1 = "uid://bdcvatet7ks35";

	private const string CHEST_COIN_WATER_2 = "uid://fmoke6o3c88f";

	private const string CHEST_COIN_WATER_3 = "uid://1vj81vk4dvil";

	private const string CHEST_COIN_WATER_4 = "uid://dtfupjwagghmo";

	private const string CHEST_COIN_WATER_5 = "uid://o6x68dbqcw44";

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
				base.sprite.SetAtlasReplace("CoinChest1_1.png", "uid://bdcvatet7ks35");
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
				base.sprite.SetAtlasReplace("CoinChest1_1.png", "uid://bdcvatet7ks35");
				break;
			case "Damage1":
				base.sprite.SetAtlasReplace("CoinChest1_1.png", "uid://fmoke6o3c88f");
				break;
			case "Damage2":
				base.sprite.SetAtlasReplace("CoinChest1_1.png", "uid://1vj81vk4dvil");
				break;
			case "Damage3":
				base.sprite.SetAtlasReplace("CoinChest1_1.png", "uid://dtfupjwagghmo");
				break;
			case "Damage4":
				base.sprite.SetAtlasReplace("CoinChest1_1.png", "uid://o6x68dbqcw44");
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
			for (int i = 0; i < 8; i++)
			{
				TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
				if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
				{
					towerDefenseGroundItemBase.gridPos = base.gridPos;
				}
			}
		}
		else
		{
			for (int j = 0; j < 2; j++)
			{
				TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0);
				if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase2))
				{
					towerDefenseGroundItemBase2.gridPos = base.gridPos;
				}
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
