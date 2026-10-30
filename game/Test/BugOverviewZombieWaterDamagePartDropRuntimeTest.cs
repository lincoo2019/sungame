using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewZombieWaterDamagePartDropRuntimeTest : Node
{
	private readonly record struct DropResult(bool Recycled, float MaxWorldY, bool PhysicsEnabledAtSpawn, bool PhysicsDisabledAfterRecycle);

	private static readonly Vector2I LandGrid = new Vector2I(3, 2);

	private static readonly Vector2I WaterGrid = new Vector2I(4, 2);

	private static readonly Vector2 LandSpawnPosition = new Vector2(250f, 80f);

	private static readonly Vector2 WaterSpawnPosition = new Vector2(350f, 80f);

	private const float DropHeight = 50f;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ZombieWaterDamagePartDropControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 6;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ObjectManager.Instance), "TowerDefenseManager and ObjectManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					goto end_IL_00de;
				}
				control = new ZombieWaterDamagePartDropControlStub
				{
					Name = "ZombieWaterDamagePartDropControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseCellInstance waterCell = TowerDefenseManager.GetMapCell(WaterGrid);
				TowerDefenseCellInstance landCell = TowerDefenseManager.GetMapCell(LandGrid);
				Check(GodotObject.IsInstanceValid(landCell) && !landCell.isWater, "The landing fixture must expose a real land map cell.");
				Check(GodotObject.IsInstanceValid(waterCell) && waterCell.isWater, "The landing fixture must expose a real WATER map cell.");
				DamagePartDrop landDamagePart = SpawnDrop(control.characterNode, landCell, LandSpawnPosition, inheritMirroredBasis: false, "LandDamagePart");
				DropResult result = await WaitForRecycle(landDamagePart);
				CheckDropResult("LandDamagePart", landDamagePart, landCell, LandSpawnPosition, expectMirroredBasis: false, result);
				await WaitFrames(2);
				DamagePartDrop landArmor = SpawnDrop(control.characterNode, landCell, LandSpawnPosition, inheritMirroredBasis: false, "LandArmor");
				Check(SameInstance(landDamagePart, landArmor), "Land Armor must reuse the DamagePart carrier returned by the previous pooled lease.");
				DropResult result2 = await WaitForRecycle(landArmor);
				CheckDropResult("LandArmor", landArmor, landCell, LandSpawnPosition, expectMirroredBasis: false, result2);
				await WaitFrames(2);
				DamagePartDrop waterDamagePart = SpawnDrop(control.characterNode, waterCell, WaterSpawnPosition, inheritMirroredBasis: true, "WaterDamagePart");
				Check(SameInstance(landArmor, waterDamagePart), "Water DamagePart must reuse the carrier after the two land leases.");
				DropResult result3 = await WaitForRecycle(waterDamagePart);
				CheckDropResult("WaterDamagePart", waterDamagePart, waterCell, WaterSpawnPosition, expectMirroredBasis: true, result3);
				await WaitFrames(2);
				DamagePartDrop waterArmor = SpawnDrop(control.characterNode, waterCell, WaterSpawnPosition, inheritMirroredBasis: true, "WaterArmor");
				Check(SameInstance(waterDamagePart, waterArmor), "Water Armor must reuse the carrier returned by the preceding water lease.");
				DropResult result4 = await WaitForRecycle(waterArmor);
				CheckDropResult("WaterArmor", waterArmor, waterCell, WaterSpawnPosition, expectMirroredBasis: true, result4);
				goto end_IL_00b7;
				end_IL_00de:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[{"BugOverviewZombieWaterDamagePartDropRuntimeTest"}] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			return;
			end_IL_00b7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(4);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
		}
		bool flag = _failures == 0 && _checks == 38;
		GD.Print($"ZOMBIE_WATER_DAMAGE_PART_DROP_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static DamagePartDrop SpawnDrop(Node2D parent, TowerDefenseCellInstance cell, Vector2 spawnPosition, bool inheritMirroredBasis, string visualName)
	{
		DamagePartDrop damagePartDrop = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.damagePart, parent) as DamagePartDrop;
		if (!GodotObject.IsInstanceValid(damagePartDrop))
		{
			return null;
		}
		damagePartDrop.Init(new Node2D
		{
			Name = visualName
		}, 50.0, new Vector2(0f, -140f));
		damagePartDrop.GlobalTransform = new Transform2D(Vector2.Right, inheritMirroredBasis ? Vector2.Up : Vector2.Down, spawnPosition);
		damagePartDrop.gridPos = cell.gridPos;
		damagePartDrop.cell = cell;
		return damagePartDrop;
	}

	private async Task<DropResult> WaitForRecycle(DamagePartDrop drop)
	{
		if (!GodotObject.IsInstanceValid(drop))
		{
			return new DropResult(Recycled: false, 1f / 0f, PhysicsEnabledAtSpawn: false, PhysicsDisabledAfterRecycle: false);
		}
		bool physicsEnabledAtSpawn = drop.IsPhysicsProcessing();
		Node2D movingSprite = drop.GetNodeOrNull<Node2D>("Sprite");
		float maxWorldY = (GodotObject.IsInstanceValid(movingSprite) ? movingSprite.GlobalPosition.Y : (1f / 0f));
		for (int frame = 0; frame < 180; frame++)
		{
			if (drop.over && !GodotObject.IsInstanceValid(drop.GetParent()))
			{
				return new DropResult(Recycled: true, maxWorldY, physicsEnabledAtSpawn, !drop.IsPhysicsProcessing());
			}
			await WaitFrames(1);
			if (GodotObject.IsInstanceValid(movingSprite) && movingSprite.IsInsideTree())
			{
				maxWorldY = Mathf.Max(maxWorldY, movingSprite.GlobalPosition.Y);
			}
		}
		return new DropResult(drop.over && !GodotObject.IsInstanceValid(drop.GetParent()), maxWorldY, physicsEnabledAtSpawn, !drop.IsPhysicsProcessing());
	}

	private static bool SameInstance(DamagePartDrop first, DamagePartDrop second)
	{
		if (GodotObject.IsInstanceValid(first) && GodotObject.IsInstanceValid(second))
		{
			return first.GetInstanceId() == second.GetInstanceId();
		}
		return false;
	}

	private void CheckDropResult(string label, DamagePartDrop drop, TowerDefenseCellInstance expectedCell, Vector2 spawnPosition, bool expectMirroredBasis, DropResult result)
	{
		Check(GodotObject.IsInstanceValid(drop), label + " must use the live pooled DamagePartDrop carrier.");
		float num = (expectMirroredBasis ? (-1f) : 1f);
		Check(GodotObject.IsInstanceValid(drop) && drop.Transform.Y.Dot(Vector2.Down) * num > 0f, label + " must retain its inherited visual basis through the pooled lease.");
		Check(GodotObject.IsInstanceValid(drop) && drop.cell == expectedCell, label + " must resolve its real landing cell after pooled reuse.");
		Check(result.PhysicsEnabledAtSpawn, label + " Init must re-enable its own landing detector for this pooled lease.");
		Check(result.Recycled, label + " must finish landing and return to the pool instead of falling forever.");
		Check(GodotObject.IsInstanceValid(drop) && drop.over, label + " must stop its active fall before deferred pool removal.");
		Check(result.PhysicsDisabledAfterRecycle, label + " Recycle must disable its own landing detector while the carrier is idle in the pool.");
		Check(result.MaxWorldY <= spawnPosition.Y + 50f + 20f, $"{label} must stop at the world-space landing plane; maxY={result.MaxWorldY}.");
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(100f, 76f),
				plantOffset = 50.0
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
				if (i == WaterGrid.X && j == WaterGrid.Y)
				{
					towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.WATER,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					};
				}
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(towerDefenseCellConfig);
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
			GD.PushError("[BugOverviewZombieWaterDamagePartDropRuntimeTest] " + message);
		}
	}
}
