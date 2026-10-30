using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ChomperPotTeleportRuntimeTest : Node
{
	private readonly List<string> _failures = new List<string>();

	private int _checks;

	private ChomperPotTeleportRuntimeControl _control;

	private TowerDefenseBattleFeatureMap _map;

	private TowerDefenseMapControl _mapControl;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousBegin = manager.gridBeginPos;
		Vector2 previousSize = manager.gridSize;
		Vector2I previousNumber = manager.gridNum;
		try
		{
			_ = 10;
			try
			{
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				SetupBattle(manager);
				TowerDefensePlantChomperPot entrance = TowerDefenseManager.GetPacketConfig("PlantChomperPot").Plant(new Vector2I(6, 2), playAudio: false) as TowerDefensePlantChomperPot;
				TowerDefensePlantChomperPot exit = TowerDefenseManager.GetPacketConfig("PlantChomperPot").Plant(new Vector2I(3, 4), playAudio: false) as TowerDefensePlantChomperPot;
				await WaitFrames(6);
				await Feed(entrance);
				TowerDefenseZombie first = SpawnZombie("ZombieNormal", entrance);
				Vector2 firstPosition = first.GetLogicalGlobalPosition();
				double firstHealth = first.instance.hitpoints;
				float firstShadowY = first.shadowComponent.saveShadowPosition.Y;
				await WaitFrames(6);
				Check(first.GetLogicalGlobalPosition().IsEqualApprox(firstPosition), "没有其他消化中的花盆时不得传送或传送到自身。");
				await Feed(exit);
				await WaitFrames(6);
				Vector2 expectedExit = exit.GetLogicalGlobalPosition() - new Vector2(manager.gridSize.X * 0.5f, 0f);
				Check(first.GetLogicalGlobalPosition().DistanceTo(expectedExit) < 0.1f, $"消化回调必须真实传送经过的僵尸：{firstPosition} -> {first.GetLogicalGlobalPosition()}，应为 {expectedExit}。");
				Check(first.gridPos == exit.gridPos && first.cell == exit.cell, "传送后必须同步目标格子与地图格引用。");
				Check(first.instance.hitpoints == firstHealth && !first.die, "传送不得伤害或吞掉经过的僵尸。");
				Check(Math.Abs(first.shadowComponent.saveShadowPosition.Y - (firstShadowY + expectedExit.Y - firstPosition.Y)) < 0.1f, "跨行传送必须同步阴影基线。");
				Check(entrance.chomperComponent.isChew && exit.chomperComponent.isChew, "成功传送后两端仍必须保持消化状态。");
				await WaitFrames(12);
				Check(first.GetLogicalGlobalPosition().DistanceTo(expectedExit) < 0.1f, "出口偏移必须阻止僵尸立即被传回入口。");
				TowerDefenseZombie second = SpawnZombie("ZombieNormal", entrance);
				double secondHealth = second.instance.hitpoints;
				await WaitFrames(6);
				Check(second.GetLogicalGlobalPosition().DistanceTo(expectedExit) < 0.1f, "紧接着经过的第二只僵尸也必须传送，不受攻击冷却阻断。");
				Check(second.gridPos == exit.gridPos && second.cell == exit.cell && second.instance.hitpoints == secondHealth && !second.die, $"连续传送必须同步新目标的格子并保持生命值：格子 {second.gridPos}/{exit.gridPos}，生命值 {second.instance.hitpoints}/{secondHealth}。");
				GD.Print($"CHOMPER_POT_TELEPORT first={first.GetLogicalGlobalPosition()} second={second.GetLogicalGlobalPosition()} expected={expectedExit} firstHealth={firstHealth}/{first.instance.hitpoints} secondHealth={secondHealth}/{second.instance.hitpoints} timer={entrance.attackComponent2.timer}");
				await VerifyExcludedTargets(entrance);
				for (int frame = 0; frame < 120; frame++)
				{
					if (!(exit.sprite.clip != exit.chomperComponent.chewAnimeClips))
					{
						break;
					}
					await WaitFrames(1);
				}
				exit.chomperComponent.chewTimer = exit.chomperComponent.currentChewTime;
				exit.chomperComponent.ChewProcessing(0.0);
				Check(!exit.chomperComponent.isChew, "消化结束后出口必须退出可传送状态。");
				TowerDefenseZombie blocked = SpawnZombie("ZombieNormal", entrance);
				Vector2 blockedPosition = blocked.GetLogicalGlobalPosition();
				await WaitFrames(6);
				Check(blocked.GetLogicalGlobalPosition().IsEqualApprox(blockedPosition), "出口消化结束后新经过的僵尸不得继续传送。");
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
			GD.PushError("[ChomperPotTeleportRuntimeTest] " + failure);
		}
		bool flag = _checks == 20 && _failures.Count == 0;
		GD.Print($"CHOMPER_POT_TELEPORT_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task Feed(TowerDefensePlantChomperPot pot)
	{
		TowerDefenseZombie food = SpawnZombie("ZombieNormal", pot);
		for (int frame = 0; frame < 300; frame++)
		{
			if (pot.chomperComponent.isChew)
			{
				break;
			}
			await WaitFrames(1);
		}
		Check(pot.chomperComponent.isChew && pot.chomperComponent.currentChewTime == 30f, "正式吞咬动画必须令大嘴花盆进入 30 秒消化状态。");
		Check(!GodotObject.IsInstanceValid(food) || food.isDestroy, "作为食物的普通僵尸必须由真实吞咬流程移除。");
	}

	private async Task VerifyExcludedTargets(TowerDefensePlantChomperPot entrance)
	{
		TowerDefenseZombie towerDefenseZombie = SpawnZombie("ZombieGargantuar", entrance);
		TowerDefenseZombie towerDefenseZombie2 = SpawnZombie("ZombieZamboni", entrance);
		TowerDefenseZombie towerDefenseZombie3 = SpawnZombie("ZombieNormal", entrance);
		towerDefenseZombie3.instance.zombiePhysique = TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		TowerDefenseZombie towerDefenseZombie4 = SpawnZombie("ZombieNormal", entrance);
		towerDefenseZombie4.Hypnoses();
		TowerDefenseZombie towerDefenseZombie5 = SpawnZombie("ZombieNormal", entrance);
		towerDefenseZombie5.SetLogicalGlobalPosition(towerDefenseZombie5.GetLogicalGlobalPosition() + new Vector2(0f, TowerDefenseManager.Instance.gridSize.Y));
		towerDefenseZombie5.gridPos += Vector2I.Down;
		TowerDefenseZombie[] excluded = new TowerDefenseZombie[5] { towerDefenseZombie, towerDefenseZombie2, towerDefenseZombie3, towerDefenseZombie4, towerDefenseZombie5 };
		Vector2[] positions = new Vector2[excluded.Length];
		for (int i = 0; i < excluded.Length; i++)
		{
			positions[i] = excluded[i].GetLogicalGlobalPosition();
		}
		await WaitFrames(6);
		for (int j = 0; j < excluded.Length; j++)
		{
			Check(excluded[j].GetLogicalGlobalPosition().IsEqualApprox(positions[j]), $"排除目标 {j}（{excluded[j].config.name}）不得被管道传送。");
		}
	}

	private TowerDefenseZombie SpawnZombie(string packetName, TowerDefensePlantChomperPot pot)
	{
		Vector2 pos = pot.GetLogicalGlobalPosition() + new Vector2(12f, 0f);
		TowerDefenseZombie towerDefenseZombie = TowerDefenseManager.GetPacketConfig(packetName).Create(pos, pot.gridPos) as TowerDefenseZombie;
		towerDefenseZombie.ProcessMode = ProcessModeEnum.Disabled;
		_control.characterNode.AddChild(towerDefenseZombie, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseZombie;
	}

	private void SetupBattle(TowerDefenseManager manager)
	{
		_control = new ChomperPotTeleportRuntimeControl
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
			mapConfig = towerDefenseMapConfig
		};
		_mapControl.mapFeature = _map;
		_map.PlantGridInit();
		_control.featureDictionary["Map"] = _map;
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
