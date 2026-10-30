using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewPumpkinPeaMuzzleRuntimeTest : Node
{
	private const string ShootingLevelPath = "res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_14.tres";

	private const string PumpkinPeaScenePath = "res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/Scene/TowerDefensePlantPumpkinPea.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	private PumpkinPeaMuzzleRuntimeControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	private TowerDefensePlantPumpkinPea _plant;

	private BulletField _bulletField;

	private bool _ownsBulletField;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ProjectileUpdateManager projectileUpdateManager = null;
		ProcessModeEnum previousProjectileProcessMode = ProcessModeEnum.Inherit;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager is unavailable.");
				}
				SetupBattleFixture(manager);
				TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_14.tres", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig) && towerDefenseLevelConfig.levelName == "双鬼拍门", "The regression must load the real Shooting Level 1-14 Double Gate resource.");
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindPacket(towerDefenseLevelConfig, "PlantPumpkinPea", (TowerDefenseLevelPacketConfig packet) => HasPropertyValue(packet.@override?.characterOverride, "fireNum", (Variant variant) => variant.AsInt32() == 6) && HasPropertyValue(packet.@override?.characterOverride, "projectileName", (Variant variant) => variant.AsString() == "IceFirePea"));
				Check(GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig), "Double Gate's real upgrade chain must contain the six-shot IceFirePea PumpkinPea override.");
				TowerDefenseCharacterOverride characterOverride = towerDefenseLevelPacketConfig?.@override?.characterOverride;
				Check(GodotObject.IsInstanceValid(characterOverride), "The real PlantPumpkinPea level packet must carry its character override.");
				TowerDefenseProjectileRegistry.Init();
				_bulletField = BulletField.Instance;
				if (!GodotObject.IsInstanceValid(_bulletField))
				{
					_bulletField = new BulletField
					{
						Name = "PumpkinPeaMuzzleBulletField"
					};
					AddChild(_bulletField, forceReadableName: false, InternalMode.Disabled);
					_ownsBulletField = true;
					await WaitFrames(1);
				}
				Check(GodotObject.IsInstanceValid(_bulletField), "A real BulletField must be available for the muzzle regression.");
				if (!GodotObject.IsInstanceValid(_bulletField) || !GodotObject.IsInstanceValid(characterOverride))
				{
					throw new InvalidOperationException("The real level override or BulletField is unavailable.");
				}
				projectileUpdateManager = ProjectileUpdateManager.Instance;
				if (GodotObject.IsInstanceValid(projectileUpdateManager))
				{
					previousProjectileProcessMode = projectileUpdateManager.ProcessMode;
					projectileUpdateManager.ProcessMode = ProcessModeEnum.Disabled;
				}
				_bulletField.ClearActiveBullets();
				await SpawnRealPumpkinPea(characterOverride);
				FireComponent fire = _plant?.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fire != null && !fire.IsReleased, "The real PlantPumpkinPea must expose its resource-backed FireComponent.");
				if (fire == null || fire.IsReleased)
				{
					throw new InvalidOperationException("PlantPumpkinPea FireComponent is unavailable.");
				}
				Check(_plant.fireNum == 6 && _plant.projectileName == "IceFirePea" && Math.Abs(_plant.fireInterval - 0.25) < 0.0001, "The real Double Gate override must reach the live PlantPumpkinPea FireComponent facade.");
				FireComponentCheckConfig fireComponentCheckConfig = ((fire.fireCheckList.Count > 0) ? fire.fireCheckList[0] : null);
				TowerDefenseProjectileCreateData projectileData = fireComponentCheckConfig?.projectile?.GetProjectile();
				Check(projectileData?.projectileName.ToString() == "IceFirePea", "The live PumpkinPea FireComponent must build the real Double Gate IceFirePea projectile.");
				if (projectileData == null)
				{
					throw new InvalidOperationException("The real IceFirePea create data is unavailable.");
				}
				int collisionFlags = fireComponentCheckConfig.GetCollisionFlags();
				await VerifyMuzzleShot(fire, projectileData, collisionFlags, "SpriteGroup/TransformPoint/PumpkinPea/Marker2D2", 57f, 1, new Vector2(300f, 0f), "right");
				await VerifyMuzzleShot(fire, projectileData, collisionFlags, "SpriteGroup/TransformPoint/PumpkinPea/Marker2D", -53f, 0, new Vector2(-300f, 0f), "left");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPumpkinPeaMuzzleRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_bulletField))
			{
				_bulletField.ClearActiveBullets();
			}
			if (GodotObject.IsInstanceValid(projectileUpdateManager))
			{
				projectileUpdateManager.ProcessMode = previousProjectileProcessMode;
			}
			if (GodotObject.IsInstanceValid(_plant))
			{
				_plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			_mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			if (_ownsBulletField && GodotObject.IsInstanceValid(_bulletField))
			{
				_bulletField.QueueFree();
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0;
		GD.Print($"BUG_OVERVIEW_I89_PUMPKIN_PEA_MUZZLE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void SetupBattleFixture(TowerDefenseManager manager)
	{
		_control = new PumpkinPeaMuzzleRuntimeControlStub
		{
			Name = "PumpkinPeaMuzzleRuntimeControl",
			isGameRunning = false,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		manager.gridBeginPos = new Vector2(100f, 100f);
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		_mapControl = new TowerDefenseMapControl
		{
			Name = "MapControl"
		};
		_mapFeature = CreateMapFeature(_mapControl, manager.gridNum);
		_mapFeature.control = _control;
		_control.featureDictionary[new StringName("Map")] = _mapFeature;
	}

	private async Task SpawnRealPumpkinPea(TowerDefenseCharacterOverride characterOverride)
	{
		_plant = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter7/PumpkinPea/Scene/TowerDefensePlantPumpkinPea.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefensePlantPumpkinPea>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(_plant) && _plant.config?.name == "PlantPumpkinPea", "The regression must instantiate the real PlantPumpkinPea scene.");
		if (!GodotObject.IsInstanceValid(_plant))
		{
			throw new InvalidOperationException("The real PlantPumpkinPea scene did not instantiate.");
		}
		_plant.inGame = false;
		_plant.editorPreviewMode = true;
		_plant.gridPos = TestGrid;
		_plant.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TestGrid);
		_control.characterNode.AddChild(_plant, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(3);
		_plant.ProcessMode = ProcessModeEnum.Disabled;
		_plant.gridPos = TestGrid;
		_plant.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TestGrid);
		_plant.cell = TowerDefenseManager.GetMapCell(TestGrid);
		characterOverride.ExecuteCharacter(_plant);
	}

	private async Task VerifyMuzzleShot(FireComponent fire, TowerDefenseProjectileCreateData projectileData, int collisionFlags, NodePath markerPath, float expectedX, int firePosId, Vector2 velocity, string label)
	{
		Marker2D nodeOrNull = _plant.GetNodeOrNull<Marker2D>(markerPath);
		Check(GodotObject.IsInstanceValid(nodeOrNull), "The real PumpkinPea " + label + " muzzle Marker2D must resolve.");
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			throw new InvalidOperationException("The " + label + " muzzle marker is unavailable.");
		}
		Check(Mathf.IsEqualApprox(nodeOrNull.Position.X, expectedX), $"The {label} muzzle must keep its authored horizontal side; x={nodeOrNull.Position.X}.");
		Check(nodeOrNull.Position.Y >= 18f && nodeOrNull.Position.Y <= 20f, $"The {label} projectile must originate from the mouth band Y=18..20, not above it; y={nodeOrNull.Position.Y}.");
		_bulletField.ClearActiveBullets();
		TowerDefenseProjectile towerDefenseProjectile = fire.CreateProjectile(firePosId, velocity, projectileData, collisionFlags, _plant.camp);
		int lastSpawnedIndex = _bulletField.LastSpawnedIndex;
		bool flag = towerDefenseProjectile == null && _bulletField.ActiveCount == 1 && _bulletField.IsBulletActive(lastSpawnedIndex);
		Check(flag, "The real " + label + " IceFirePea shot must route through the live BulletField.");
		if (!flag)
		{
			throw new InvalidOperationException("The " + label + " IceFirePea did not enter BulletField.");
		}
		ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(lastSpawnedIndex);
		Check(bulletDataRef.pos.DistanceTo(nodeOrNull.GlobalPosition) <= 0.01f, $"The {label} IceFirePea must spawn at its real muzzle marker; bullet={bulletDataRef.pos}, marker={nodeOrNull.GlobalPosition}.");
		Check(bulletDataRef.height >= -20.0, $"The mouth-aligned {label} IceFirePea must remain above the normal shooter floor; height={bulletDataRef.height}.");
		_bulletField.Update(1.0 / 60.0, Engine.GetPhysicsFrames() + 1);
		Check(_bulletField.IsBulletActive(lastSpawnedIndex), "The mouth-aligned " + label + " IceFirePea must remain active after a real BulletField update.");
		_bulletField.ClearActiveBullets();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private static TowerDefenseLevelPacketConfig FindPacket(TowerDefenseLevelConfig level, string packetName, Func<TowerDefenseLevelPacketConfig, bool> predicate)
	{
		if (!GodotObject.IsInstanceValid(level))
		{
			return null;
		}
		HashSet<ulong> visited = new HashSet<ulong>();
		foreach (Variant packetBank in level.packetBankList)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindPacket(packetBank.AsGodotObject() as TowerDefenseLevelPacketConfig, packetName, predicate, visited);
			if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig))
			{
				return towerDefenseLevelPacketConfig;
			}
		}
		return null;
	}

	private static TowerDefenseLevelPacketConfig FindPacket(TowerDefenseLevelPacketConfig packet, string packetName, Func<TowerDefenseLevelPacketConfig, bool> predicate, HashSet<ulong> visited)
	{
		if (!GodotObject.IsInstanceValid(packet) || !visited.Add(packet.GetInstanceId()))
		{
			return null;
		}
		if (packet.packetName == packetName && predicate(packet))
		{
			return packet;
		}
		if (!GodotObject.IsInstanceValid(packet.@override))
		{
			return null;
		}
		foreach (CardActionBehaviorDefinition useSucceededAction in packet.@override.useSucceededActions)
		{
			if (useSucceededAction is CardActionBehaviorChangePacket cardActionBehaviorChangePacket)
			{
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindPacket(cardActionBehaviorChangePacket.levelPacketConfig, packetName, predicate, visited);
				if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig))
				{
					return towerDefenseLevelPacketConfig;
				}
			}
		}
		return null;
	}

	private static bool HasPropertyValue(TowerDefenseCharacterOverride characterOverride, string propertyName, Func<Variant, bool> predicate)
	{
		if (!GodotObject.IsInstanceValid(characterOverride))
		{
			return false;
		}
		foreach (TowerDefenseCharacterPropertyChangeConfig item in characterOverride.propertyChange)
		{
			if (GodotObject.IsInstanceValid(item) && item.propertyName == propertyName)
			{
				return predicate(item.value);
			}
		}
		return false;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = new Vector2(100f, 100f),
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewPumpkinPeaMuzzleRuntimeTest] " + message);
		}
	}
}
