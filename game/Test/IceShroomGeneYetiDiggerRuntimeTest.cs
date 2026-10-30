using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class IceShroomGeneYetiDiggerRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	private int _checks;

	private IceShroomGeneYetiDiggerRuntimeControl _control;

	private TowerDefenseBattleFeatureMap _map;

	private TowerDefenseMapControl _mapControl;

	private IceShroomGeneYetiDiggerRuntimeProcess _process;

	private readonly MethodInfo _checkArea = typeof(TowerDefenseControlNew).GetMethod("ProcessZombieCheckArea", BindingFlags.Instance | BindingFlags.NonPublic);

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNumber = manager.gridNum;
		try
		{
			_ = 6;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				SetupBattle(manager);
				await VerifyFreshSpawn();
				await VerifyOutsideSpawnWithParentTransform();
				await VerifyDiggingOnLawn();
				await VerifyEmergence();
				await VerifyHypnosis();
				await VerifyRealHouseEntry();
			}
			catch (Exception value)
			{
				_failures.Add($"回归运行异常：{value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.isGameRunning = false;
				_control.QueueFree();
			}
			await WaitFrames(6, checkHouse: false);
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousBegin;
			manager.gridSize = previousSize;
			manager.gridNum = previousNumber;
			_map?.Destroy();
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			ObjectManager.Instance.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		foreach (string failure in _failures)
		{
			GD.PushError("[IceShroomGeneYetiDiggerRuntimeTest] " + failure);
		}
		bool flag = _checks == 54 && _failures.Count == 0;
		GD.Print($"ICE_SHROOM_GENE_YETI_DIGGER_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyFreshSpawn()
	{
		TowerDefensePlantIceShroomGene mushroom = await PrepareMushroom();
		TowerDefenseZombieYetiDigger towerDefenseZombieYetiDigger = SpawnYeti();
		Check(towerDefenseZombieYetiDigger.gridPos.X == -1, "正式波次出生必须保留尚未入场的负一列标记。");
		Check((double)towerDefenseZombieYetiDigger.GetLogicalGlobalPosition().X > TowerDefenseManager.Instance.GetMapGroundRight(), "出生时的矿工实际位于地图右侧场外。");
		await MutateAndVerify(mushroom, towerDefenseZombieYetiDigger, "出生同帧");
	}

	private async Task VerifyOutsideSpawnWithParentTransform()
	{
		_control.characterNode.Position = new Vector2(37f, 12f);
		_control.characterNode.Scale = new Vector2(1.2f, 1.2f);
		TowerDefensePlantIceShroomGene mushroom = await PrepareMushroom();
		TowerDefenseZombieYetiDigger yeti = SpawnYeti();
		await WaitFrames(6);
		await MutateAndVerify(mushroom, yeti, "场外挖地及父节点变换");
		_control.characterNode.Transform = Transform2D.Identity;
	}

	private async Task VerifyDiggingOnLawn()
	{
		TowerDefensePlantIceShroomGene mushroom = await PrepareMushroom();
		TowerDefenseZombieYetiDigger yeti = SpawnYeti();
		yeti.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(new Vector2I(5, 3)) + new Vector2(21f, 7f));
		await WaitFrames(6);
		await MutateAndVerify(mushroom, yeti, "草坪内挖地");
	}

	private async Task VerifyEmergence()
	{
		TowerDefensePlantIceShroomGene mushroom = await PrepareMushroom();
		TowerDefenseZombieYetiDigger towerDefenseZombieYetiDigger = SpawnYeti();
		float x = (float)TowerDefenseManager.Instance.GetMapGroundLeft() + TowerDefenseManager.Instance.GetMapGridSize().X - 2f;
		towerDefenseZombieYetiDigger.SetLogicalGlobalPosition(new Vector2(x, (float)TowerDefenseManager.GetMapLineY(3)));
		towerDefenseZombieYetiDigger.gridPos = new Vector2I(1, 3);
		towerDefenseZombieYetiDigger.DigProcessing(0.0);
		Check(towerDefenseZombieYetiDigger.digOver && towerDefenseZombieYetiDigger.Scale.X < 0f, "正式边界触发必须令矿工出土并朝右。");
		await MutateAndVerify(mushroom, towerDefenseZombieYetiDigger, "自然出土转身");
	}

	private async Task VerifyHypnosis()
	{
		TowerDefensePlantIceShroomGene mushroom = await PrepareMushroom();
		mushroom.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
		TowerDefenseZombieYetiDigger yeti = SpawnYeti();
		yeti.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(new Vector2I(6, 3)) + new Vector2(-17f, 0f));
		yeti.Hypnoses();
		await WaitFrames(6);
		await MutateAndVerify(mushroom, yeti, "魅惑矿工");
	}

	private async Task VerifyRealHouseEntry()
	{
		TowerDefenseZombie node = TowerDefenseManager.GetPacketConfig("ZombieNormal").Create(new Vector2(136f, (float)TowerDefenseManager.GetMapLineY(3)), new Vector2I(-1, 3)) as TowerDefenseZombie;
		_control.characterNode.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(3);
		Check(_process.houseEntries > 0, "真实进入房屋区域的普通僵尸必须仍然触发进家事件。");
		await ClearCharacters();
	}

	private async Task<TowerDefensePlantIceShroomGene> PrepareMushroom()
	{
		TowerDefensePlantIceShroomGene mushroom = TowerDefenseManager.GetPacketConfig("PlantIceShroomGene").Plant(new Vector2I(7, 2), playAudio: false) as TowerDefensePlantIceShroomGene;
		mushroom.ProcessMode = ProcessModeEnum.Disabled;
		await WaitFrames(4);
		return mushroom;
	}

	private TowerDefenseZombieYetiDigger SpawnYeti()
	{
		TowerDefenseZombieYetiDigger obj = TowerDefenseManager.GetPacketConfig("ZombieYetiDigger").Spawn(3, 0.0, isIdle: true) as TowerDefenseZombieYetiDigger;
		obj.instance.hitpoints = 420.0;
		obj.Walk();
		return obj;
	}

	private async Task MutateAndVerify(TowerDefensePlantIceShroomGene mushroom, TowerDefenseZombieYetiDigger source, string label)
	{
		Vector2 sourcePosition = source.GetLogicalGlobalPosition();
		float sourceFacing = Mathf.Sign(source.Scale.X);
		double sourceHealth = source.instance.hitpoints;
		TowerDefenseEnum.CHARACTER_CAMP sourceCamp = source.camp;
		bool sourceHypnosis = source.instance.hypnoses;
		Vector2I sourceGrid = source.gridPos;
		ExplodeComponent runtime = mushroom.componentManager.GetRuntime<ExplodeComponent>();
		runtime.AnimeCompleted(runtime.explodeAnimeClips);
		await WaitFrames(6);
		TowerDefenseZombie normal = null;
		int num = 0;
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie.config.name == "ZombieNormal")
			{
				normal = towerDefenseZombie;
				num++;
			}
		}
		Check(num == 1, label + "：必须只生成一个普通僵尸。");
		if (!GodotObject.IsInstanceValid(normal))
		{
			throw new InvalidOperationException(label + "：没有生成普通僵尸。");
		}
		Check(!GodotObject.IsInstanceValid(source), label + "：变身后必须移除原矿工。");
		Check(normal.GetLogicalGlobalPosition().DistanceTo(sourcePosition) < 1f, $"{label}：必须原地变身，位置 {sourcePosition} -> {normal.GetLogicalGlobalPosition()}，源格子={sourceGrid}。");
		Check((float)Mathf.Sign(normal.Scale.X) == sourceFacing, label + "：必须保持原朝向。");
		Check(Math.Abs(normal.instance.hitpoints - (sourceHealth - 20.0)) < 0.01 && Math.Abs(normal.instance.hitpointsSave - sourceHealth) < 0.01, label + "：必须继承本体血量且只承受一次 20 点寒冰伤害。");
		Check(normal.camp == sourceCamp && normal.instance.hypnoses == sourceHypnosis, label + "：必须保持原阵营和魅惑状态。");
		Check(normal.buff.BuffGet("Frozen") is TowerDefenseCharacterBuffFrozen && normal.timeScale == 0.0, label + "：新普通僵尸必须实际冰冻停止移动。");
		Check(_process.houseEntries == 0, $"{label}：变身不能触发进家，实际次数={_process.houseEntries}。");
		GD.Print($"GENE_YETI_TRANSFORM case={label} source={sourcePosition} grid={sourceGrid} facing={sourceFacing} target={normal.GetLogicalGlobalPosition()} facing={normal.Scale.X} hp={normal.instance.hitpoints}");
		normal.buff.DeleteBuff("Frozen");
		normal.buff.DeleteBuff("IceSpeedDown");
		float beforeWalk = normal.GetLogicalGlobalPosition().X;
		await WaitFrames(40);
		float num2 = normal.GetLogicalGlobalPosition().X - beforeWalk;
		Check(num2 * sourceFacing < -0.05f, $"{label}：解冻后必须按原朝向真实行走，位移={num2}，朝向={sourceFacing}。");
		Check(_process.houseEntries == 0, label + "：解冻后不能因变身位置错误进家。");
		await ClearCharacters();
	}

	private async Task ClearCharacters()
	{
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter)
			{
				towerDefenseCharacter.SilentlyRemoveForTransformation();
			}
			else
			{
				child.QueueFree();
			}
		}
		await WaitFrames(4);
		_process.houseEntries = 0;
	}

	private void SetupBattle(TowerDefenseManager manager)
	{
		_control = new IceShroomGeneYetiDiggerRuntimeControl
		{
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig(),
			characterNode = new Node2D()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			isNight = true
		};
		manager.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
		manager.gridSize = towerDefenseMapConfig.gridSize;
		manager.gridNum = towerDefenseMapConfig.gridNum;
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, towerDefenseMapConfig.gridNum.X, towerDefenseMapConfig.gridNum.Y)
		});
		for (int i = 1; i <= towerDefenseMapConfig.gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		_mapControl = new TowerDefenseMapControl();
		_map = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			mapControl = _mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		};
		_mapControl.mapFeature = _map;
		_map.PlantGridInit();
		_control.featureDictionary["Map"] = _map;
		_process = new IceShroomGeneYetiDiggerRuntimeProcess
		{
			control = _control
		};
		_control.process = _process;
		TowerDefenseControlNew towerDefenseControlNew = GD.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn").Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
		AabbArea2D node = towerDefenseControlNew.GetNode<AabbArea2D>("CharacterLayer/ZombieCheckArea");
		node.Owner = null;
		node.GetParent().RemoveChild(node);
		_control.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		_control.zombieCheckArea = node;
		towerDefenseControlNew.Free();
	}

	private async Task WaitFrames(int count, bool checkHouse = true)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (checkHouse)
			{
				_checkArea.Invoke(_control, null);
			}
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
