using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;

public partial class FootballFumeShroomBiteRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	private int _checks;

	private FootballFumeShroomBiteRuntimeControl _control;

	private TowerDefenseBattleFeatureMap _map;

	private TowerDefenseMapControl _mapControl;

	private string _captureDirectory;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNumber = manager.gridNum;
		try
		{
			_ = 2;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				SetupBattle(manager);
				_captureDirectory = System.Environment.GetEnvironmentVariable("PVZHE_FUME_BITE_CAPTURE_DIR");
				string[] array = new string[2] { "ZombieFootballFumeShroom", "ZombieFootballBlackFumeShroom" };
				foreach (string packetName in array)
				{
					await VerifyBiting(packetName, isNight: true);
					await VerifyBiting(packetName, isNight: false);
				}
			}
			catch (Exception value)
			{
				_failures.Add($"运行回归异常：{value}");
			}
		}
		finally
		{
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
			GD.PushError("[FootballFumeShroomBiteRuntimeTest] " + failure);
		}
		bool flag = _checks == 44 && _failures.Count == 0;
		GD.Print($"FOOTBALL_FUME_BITE_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyBiting(string packetName, bool isNight)
	{
		_map.config.isNight = isNight;
		string label = packetName + "/" + (isNight ? "夜晚" : "白天");
		TowerDefensePlant front = TowerDefenseManager.GetPacketConfig("PlantWallnut").Plant(new Vector2I(6, 3), playAudio: false) as TowerDefensePlant;
		TowerDefensePlant rear = TowerDefenseManager.GetPacketConfig("PlantWallnut").Plant(new Vector2I(4, 3), playAudio: false) as TowerDefensePlant;
		TowerDefensePlant adjacent = TowerDefenseManager.GetPacketConfig("PlantWallnut").Plant(new Vector2I(4, 4), playAudio: false) as TowerDefensePlant;
		Vector2 pos = front.GetLogicalGlobalPosition() + new Vector2(40f, 0f);
		TowerDefenseZombie zombie = TowerDefenseManager.GetPacketConfig(packetName).Create(pos, front.gridPos) as TowerDefenseZombie;
		_control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		AttackComponent ranged = zombie.componentManager.GetRuntime<AttackComponent>("character.attack.1");
		BulletField field = BulletField.EnsureMountedOnCharacterNode();
		GpuParticles2D particles = zombie.GetNode<GpuParticles2D>("%FireParticles");
		ranged.timer = 1.0;
		int shots = 0;
		bool allShotsWhileBiting = true;
		bool allShotsHavePuff = true;
		bool allShotsHaveParticles = true;
		ulong firstShotFrame = 0uL;
		ulong secondShotFrame = 0uL;
		ranged.OnAttack += OnShot;
		try
		{
			await WaitFrames(6);
			zombie.Walk();
			for (int frame = 0; frame < 120; frame++)
			{
				if (zombie.startAttack)
				{
					break;
				}
				await WaitFrames(1);
			}
			GD.Print($"FOOTBALL_FUME_BITE_START case={label} state={zombie.CurrentStateHandle?.StableId} zombiePos={zombie.GetLogicalGlobalPosition()} frontPos={front.GetLogicalGlobalPosition()} running={_control.isGameRunning} inGame={zombie.inGame} paused={zombie.isPause}/{zombie.sprite.pause} speed={zombie.timeScale} biteAlive={zombie.attackComponent.alive} rangedAlive={ranged.alive} biteTargets={zombie.attackComponent.GetTargetList().Count} rangedTargets={ranged.GetTargetList().Count} frontAlive={!front.die && !front.isDestroy} frontHealth={front.instance.hitpoints} camps={zombie.camp}/{front.camp}");
			Check(zombie.CurrentStateHandle?.StableId == "zombie.attack" && zombie.startAttack && zombie.attackComponent.target == front, label + " 必须自然进入啃咬前排坚果的状态。");
			Check(Math.Abs(ranged.timeScale - (isNight ? 1.0 : 0.5)) < 0.001, label + " 必须保留夜晚正常、白天减半的喷射速度。");
			double frontHealth = front.instance.hitpoints;
			double rearHealth = rear.instance.hitpoints;
			double adjacentHealth = adjacent.instance.hitpoints;
			bool captured = false;
			ulong timeoutFrame = Engine.GetPhysicsFrames() + (ulong)(Engine.PhysicsTicksPerSecond * 10);
			while (Engine.GetPhysicsFrames() < timeoutFrame)
			{
				await WaitFrames(1);
				if (!captured && shots > 0 && !string.IsNullOrWhiteSpace(_captureDirectory))
				{
					await WaitFrames(6);
					await CaptureFrame(packetName, isNight);
					captured = true;
				}
				if (shots >= 2 && field.ActiveCount == 0)
				{
					break;
				}
			}
			Check(shots >= 2, $"{label} 持续啃咬时必须连续喷射，实际次数 {shots}。");
			Check(shots > 0 && allShotsWhileBiting, label + " 孢子喷射必须发生在持续啃咬期间。");
			Check(shots > 0 && allShotsHavePuff, label + " 每次动画事件必须生成真实的 20 伤害穿透孢子。");
			Check(shots > 0 && allShotsHaveParticles, label + " 每次喷射必须启动孢子粒子效果。");
			Check(front.instance.hitpoints < frontHealth - (double)shots * 20.0, label + " 喷射期间必须继续造成啃咬伤害。");
			Check(shots >= 2 && Math.Abs(rearHealth - rear.instance.hitpoints - (double)shots * 20.0) < 0.01, $"{label} 孢子必须穿透到后排坚果且每次只造成 20 伤害，实际伤害 {rearHealth - rear.instance.hitpoints}。");
			Check(adjacent.instance.hitpoints == adjacentHealth, label + " 孢子不得伤害邻行植物。");
			double shotInterval = (double)(secondShotFrame - firstShotFrame) / (double)Engine.PhysicsTicksPerSecond;
			Check(shots >= 2 && shotInterval >= (isNight ? 1.0 : 2.0) && shotInterval <= (isNight ? 2.5 : 5.0), $"{label} 持续喷射必须遵守原有冷却，实际间隔 {shotInterval:F3} 秒。");
			int beforePause = shots;
			zombie.isPause = true;
			await WaitFrames(120);
			Check(shots == beforePause, label + " 暂停僵尸后必须停止喷射。");
			GD.Print($"FOOTBALL_FUME_BITE case={label} shots={shots} interval={shotInterval:F3} biteDamage={frontHealth - front.instance.hitpoints:F3} rearDamage={rearHealth - rear.instance.hitpoints:F3} state={zombie.CurrentStateHandle?.StableId}");
		}
		finally
		{
			ranged.OnAttack -= OnShot;
			field.ClearActiveBullets();
			TowerDefenseCharacter[] array = new TowerDefenseCharacter[4] { zombie, front, rear, adjacent };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Destroy();
			}
			await WaitFrames(6);
		}
		void OnShot()
		{
			shots++;
			allShotsWhileBiting &= zombie.CurrentStateHandle?.StableId == "zombie.attack" && zombie.startAttack;
			allShotsHaveParticles &= particles.Emitting;
			int lastSpawnedIndex = field.LastSpawnedIndex;
			if (lastSpawnedIndex >= 0 && field.IsBulletActive(lastSpawnedIndex))
			{
				ref BulletData bulletDataRef = ref field.GetBulletDataRef(lastSpawnedIndex);
				allShotsHavePuff &= bulletDataRef.config?.name == "Puff" && Math.Abs(bulletDataRef.damage - 20.0) < 0.001 && bulletDataRef.penetrateNum == -1 && bulletDataRef.fireLength == 4;
			}
			else
			{
				allShotsHavePuff = false;
			}
			if (shots == 1)
			{
				firstShotFrame = Engine.GetPhysicsFrames();
			}
			if (shots == 2)
			{
				secondShotFrame = Engine.GetPhysicsFrames();
			}
		}
	}

	private void SetupBattle(TowerDefenseManager manager)
	{
		RenderingServer.SetDefaultClearColor(new Color(0.15f, 0.2f, 0.16f));
		_control = new FootballFumeShroomBiteRuntimeControl
		{
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig(),
			characterNode = new Node2D()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
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
		_map = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			mapControl = _mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			rect = TowerDefenseBattleFeatureMap.BuildProjectileBoundaryRect(towerDefenseMapConfig)
		};
		_mapControl.mapFeature = _map;
		_map.PlantGridInit();
		_control.featureDictionary["Map"] = _map;
	}

	private async Task CaptureFrame(string packetName, bool isNight)
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		string text = Path.Combine(_captureDirectory, packetName + "-" + (isNight ? "night" : "day") + ".png");
		if (image.SavePng(text) != Error.Ok)
		{
			throw new InvalidOperationException("保存回归截图失败：" + text);
		}
		GD.Print("FOOTBALL_FUME_BITE_CAPTURE " + text);
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
