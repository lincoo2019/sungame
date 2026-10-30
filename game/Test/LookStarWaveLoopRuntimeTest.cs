using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class LookStarWaveLoopRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly List<int> _waves = new List<int>();

	private int _checks;

	private int _finalEvents;

	private LookStarWaveLoopRuntimeControl _control;

	private TowerDefenseBattleFeatureMap _map;

	private TowerDefenseMapControl _mapControl;

	private TowerDefenseBattleFeatureWave _wave;

	private TowerDefenseBattleFeatureLookStar _lookStar;

	private LookStarWaveLoopRuntimeProcess _process;

	private TowerDefenseInGameLevelControl _levelControl;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNumber = manager.gridNum;
		try
		{
			_ = 5;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				SetupBattle(manager);
				await VerifyLoop(21, 7, 14, "常规三旗");
				await VerifyLoop(2, 5, 0, "不足一旗");
				await VerifyLoop(5, 3, 3, "末旗不完整");
				await VerifyPendingSpawn();
				await VerifyCompletedFormation();
			}
			catch (Exception value)
			{
				_failures.Add($"运行回归异常：{value}");
			}
		}
		finally
		{
			_wave?.Destroy();
			_lookStar?.Destroy();
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.isGameRunning = false;
				_control.QueueFree();
			}
			await WaitFrames(6);
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
			GD.PushError("[LookStarWaveLoopRuntimeTest] " + failure);
		}
		bool flag = _checks == 38 && _failures.Count == 0;
		GD.Print($"LOOK_STAR_WAVE_LOOP_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyLoop(int waveCount, int flagInterval, int expectedLoopStart, string label)
	{
		CreateWave(waveCount, flagInterval);
		_wave.currentWave = waveCount - 1;
		_lookStar.FreshGround();
		Check(!_lookStar.isfinish && _process.finishCount == 0, label + "：未摆阵型不能判定完成。");
		await AdvanceWave();
		Check(_waves.Count == 1 && _waves[0] == waveCount && ZombieCount() == 1, label + "：必须完整刷出真实最后一波。");
		Check(!_wave.waveFinal && _wave.currentWave == expectedLoopStart, $"{label}：末波后必须回到合法循环起点，实际={_wave.currentWave}。");
		Check(_wave.spawnOver && !_wave.IsSpawnPipelineActive, label + "：末波异步出生完成后必须发布刷怪完成状态。");
		Check(_wave.CanSaveProgress(out var reason), label + "：循环等待期间必须能正常保存进度，原因=" + reason + "。");
		Check(_finalEvents == 0 && _process.finishCount == 0, label + "：阵型未完成不得派发最终波结束或胜利。");
		await ClearZombies();
		if (_wave.currentWave < 0)
		{
			return;
		}
		Dictionary data = _wave.SaveFeature();
		_wave.LoadFeature(data, new TowerDefenseLevelSaveConfigCSharp());
		await AdvanceWave();
		Check(_waves.Count == 2 && _waves[1] == expectedLoopStart + 1 && ZombieCount() == 1, label + "：存档恢复后必须刷出循环的第一波。");
		Check(_wave.spawnOver && !_wave.awaitSpawn, label + "：循环第一波必须正常完成刷怪。");
		for (int expectedWave = expectedLoopStart + 2; expectedWave <= waveCount; expectedWave++)
		{
			await ClearZombies();
			await AdvanceWave();
		}
		LookStarWaveLoopRuntimeTest lookStarWaveLoopRuntimeTest = this;
		int condition;
		if (_waves.Count == waveCount - expectedLoopStart + 1)
		{
			List<int> waves = _waves;
			if (waves[waves.Count - 1] == waveCount)
			{
				condition = ((ZombieCount() == 1) ? 1 : 0);
				goto IL_04cb;
			}
		}
		condition = 0;
		goto IL_04cb;
		IL_04cb:
		lookStarWaveLoopRuntimeTest.Check((byte)condition != 0, label + "：第二轮必须依序刷完最后一旗并再次刷新末波僵尸。");
		Check(!_wave.waveFinal && _wave.currentWave == expectedLoopStart && _wave.spawnOver, label + "：多次循环必须保持合法波次和已完成的生成状态。");
		GD.Print($"LOOK_STAR_LOOP case={label} waves={string.Join(",", _waves)} next={_wave.currentWave + 1} spawnOver={_wave.spawnOver}");
		await ClearZombies();
	}

	private async Task VerifyPendingSpawn()
	{
		CreateWave(2, 5);
		_wave.config.wave[1].spawn[0].num = 16;
		_wave.currentWave = 1;
		_wave.timer = _wave.nextWaveTime;
		_wave.WavePhysicsProcess(0.0);
		Check(_wave.IsSpawnPipelineActive && _waves.Count == 1, "复现用末波必须正在分帧生成 16 只正式僵尸。");
		_wave.timer = _wave.nextWaveTime;
		_wave.WavePhysicsProcess(0.0);
		Check(_waves.Count == 1, "生成未完成时，即使循环间隔已到也不能启动下一波。");
		for (int frame = 0; frame < 120; frame++)
		{
			if (!_wave.IsSpawnPipelineActive)
			{
				break;
			}
			await WaitFrames(1);
		}
		Check(_wave.spawnOver && !_wave.IsSpawnPipelineActive && ZombieCount() == 16, "分帧末波必须完整生成 16 只僵尸并正确收尾。");
		await ClearZombies();
	}

	private async Task VerifyCompletedFormation()
	{
		TowerDefensePlant plant = TowerDefenseManager.GetPacketConfig("PlantPeaShooter").Plant(new Vector2I(2, 2), playAudio: false) as TowerDefensePlant;
		await WaitFrames(4);
		plant.ProcessMode = ProcessModeEnum.Disabled;
		_lookStar.FreshGround();
		Check(_lookStar.isfinish && _process.finishCount == 1, "种下要求植物后必须通过正式格子检查触发完成。");
		_lookStar.Process(1.0);
		Check(_process.finishCount == 1, "已完成观星不能重复触发胜利。");
		CreateWave(1, 5);
		await AdvanceWave();
		Check(_wave.waveFinal && _wave.currentWave == 1 && _finalEvents == 1 && _wave.spawnOver, "阵型完成后末波必须正常结束，不再循环。");
		await ClearZombies();
		_lookStar.isfinish = false;
		_lookStar.config.open = false;
		CreateWave(1, 5);
		await AdvanceWave();
		Check(_wave.waveFinal && _wave.currentWave == 1 && _finalEvents == 1, "关闭观星时普通波次必须正常结束。");
		Check(_wave.CanSaveProgress(out var _), "普通末波完成后仍须允许保存稳定进度。");
		await ClearZombies();
	}

	private void CreateWave(int waveCount, int flagInterval)
	{
		_wave?.Destroy();
		_waves.Clear();
		_finalEvents = 0;
		TowerDefenseLevelWaveManagerConfig towerDefenseLevelWaveManagerConfig = new TowerDefenseLevelWaveManagerConfig
		{
			flagZombieUse = false,
			flagWaveInterval = flagInterval,
			spawnColStart = 0.1,
			spawnColEnd = 0.2,
			spawnMaxCharactersPerFrame = 1
		};
		for (int i = 0; i < waveCount; i++)
		{
			TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig = new TowerDefenseLevelWaveConfig();
			towerDefenseLevelWaveConfig.spawn.Add(new TowerDefenseLevelSpawnConfig
			{
				zombie = "ZombieNormal",
				line = 3,
				num = 1
			});
			towerDefenseLevelWaveManagerConfig.wave.Add(towerDefenseLevelWaveConfig);
		}
		_wave = new TowerDefenseBattleFeatureWave
		{
			control = _control,
			levelControl = _levelControl,
			mapFeature = _map,
			progressFeature = new TowerDefenseBattleFeatureProgress(),
			config = towerDefenseLevelWaveManagerConfig,
			currentDynamic = new TowerDefenseLevelDynamicConfig(),
			isRunning = true,
			readySetPlantOver = true,
			waveStart = true
		};
		_wave.OnWaveBegin += delegate(int id, bool huge, bool final)
		{
			_waves.Add(id);
		};
		_wave.OnFinal += delegate
		{
			_finalEvents++;
		};
		_control.featureDictionary["Wave"] = _wave;
		_process.waveFeature = _wave;
	}

	private async Task AdvanceWave()
	{
		_wave.timer = _wave.nextWaveTime;
		_wave.WavePhysicsProcess(0.0);
		for (int frame = 0; frame < 420; frame++)
		{
			await WaitFrames(1);
			if (!_wave.IsSpawnPipelineActive)
			{
				break;
			}
		}
		await WaitFrames(2);
	}

	private int ZombieCount()
	{
		int num = 0;
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie)
			{
				num++;
			}
		}
		return num;
	}

	private async Task ClearZombies()
	{
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie towerDefenseZombie)
			{
				towerDefenseZombie.SilentlyRemoveForTransformation();
			}
		}
		await WaitFrames(3);
	}

	private void SetupBattle(TowerDefenseManager manager)
	{
		_control = new LookStarWaveLoopRuntimeControl
		{
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig(),
			characterNode = new Node2D()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		_levelControl = new LookStarWaveLoopRuntimeLevelControl();
		_control.AddChild(_levelControl, forceReadableName: false, InternalMode.Disabled);
		_control.levelControl = _levelControl;
		manager.currentControl = _control;
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig();
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
		_mapControl.mapIceCap = new Node2D();
		_control.AddChild(_mapControl.mapIceCap, forceReadableName: false, InternalMode.Disabled);
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
		_process = new LookStarWaveLoopRuntimeProcess
		{
			control = _control,
			levelControl = _levelControl
		};
		_control.process = _process;
		_lookStar = new TowerDefenseBattleFeatureLookStar
		{
			control = _control,
			config = new TowerDefenseLevelLookStarManagerConfig
			{
				open = true
			}
		};
		_lookStar.config.checkList.Add(new TowerDefenseLevelLookStarCheckConfig
		{
			gridPos = new Vector2I(2, 2),
			packetName = "PlantPeaShooter"
		});
		_lookStar.GameInitFromProgress().GetAwaiter().GetResult();
		_control.featureDictionary["LookStar"] = _lookStar;
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
			_failures.Add(message);
		}
	}
}
