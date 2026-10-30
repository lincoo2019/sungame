using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantGoldCat : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private CharacterTimerComponent _timerComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "CoinGold";

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
			BulletField.OnLandOver += LandOverStruct;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
		BulletField.OnLandOver -= LandOverStruct;
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn")
		{
			if (!base.sprite.pause)
			{
				CreateProjectile();
			}
			_timerComponent.Run("Spawn", 30.0);
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 30.0);
		}
	}

	public void CreateProjectile()
	{
		AudioManager.Instance.AudioPlay("Prize");
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
			Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
			float num = 600f;
			ulong instanceId = GetInstanceId();
			double num2 = GetGroundHeight(GetLogicalGlobalPosition().Y);
			for (int i = 1; i <= mapGridNum.X; i++)
			{
				int num3 = GD.RandRange(1, mapGridNum.Y);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(i, num3));
				Vector2 pos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(i, num3)) - new Vector2(150f, 0f) + new Vector2((float)GD.RandRange((0f - mapGridSize.X) / 2f, mapGridSize.X / 2f), 0f);
				float num4 = GD.RandRange(0, 200);
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("CoinGold");
				towerDefenseProjectileCreateData.baseDamage = 500.0;
				towerDefenseProjectileCreateData.collisionFlags = 11;
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					useFall = true,
					gridYOverride = num3,
					zOverride = num + num4,
					ySpeedOverride = 400.0,
					landOverSubscribed = true,
					spawnSourceInstanceId = instanceId
				};
				FireComponent.CreateProjectilePositionByData(null, null, (float)(num2 + 30.0 - mapCell.GetGroundHeight()), pos, new Vector2(GD.RandRange(50, 100), 0f), towerDefenseProjectileCreateData, -1, base.camp, default(Vector2), overrides);
			}
		}
	}

	public void LandOverStruct(Vector2 pos2, Vector2I gridPos2, ulong sourceInstanceId)
	{
		if (sourceInstanceId == GetInstanceId())
		{
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData("CoinGold");
			towerDefenseProjectileCreateData.baseDamage = 500.0;
			towerDefenseProjectileCreateData.fireMethodFlags = 32;
			towerDefenseProjectileCreateData.collisionFlags = 11;
			FireComponent.CreateProjectilePositionByData(null, null, 0.0, pos2, new Vector2(300f, 0f), towerDefenseProjectileCreateData, -1, base.camp, default(Vector2), new BulletFieldSpawnOverrides
			{
				gridYOverride = gridPos2.Y
			});
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "CoinGold");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 3.0);
	}
}
