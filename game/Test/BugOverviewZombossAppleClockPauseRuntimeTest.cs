using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewZombossAppleClockPauseRuntimeTest : Node
{
	private const string BossScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn";

	private const string BossDaveScenePath = "res://Asset/Anime/Character/Zombie/Boss/BossDave/Scene/TowerDefenseZombieBossDave.tscn";

	private const int ExpectedChecks = 15;

	private int _checks;

	private readonly List<string> _failures = new List<string>();

	private ZombossAppleClockPauseRuntimeControlStub _control;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousPausePacket = manager?.pausePacket ?? false;
		bool previousPauseZombie = manager?.pauseZombie ?? false;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "测试必须能够使用正式战斗管理器。");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("缺少测试所需的战斗管理器。");
				}
				TowerDefenseZombie.UseBatch = false;
				SetupBattleFixture(manager);
				await VerifyBossPause(manager, "res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn", isDave: false, "普通僵王");
				await VerifyBossPause(manager, "res://Asset/Anime/Character/Zombie/Boss/BossDave/Scene/TowerDefenseZombieBossDave.tscn", isDave: true, "戴夫博士");
			}
			catch (Exception value)
			{
				_failures.Add($"运行测试出现异常：{value}");
			}
		}
		finally
		{
			TowerDefenseZombie.UseBatch = previousUseBatch;
			await CleanupBattleFixture(manager, previousControl, previousGridBegin, previousGridSize, previousGridNum, previousPausePacket, previousPauseZombie);
		}
		Finish();
	}

	private void SetupBattleFixture(TowerDefenseManager manager)
	{
		_control = new ZombossAppleClockPauseRuntimeControlStub
		{
			Name = "ZombossAppleClockPauseRuntimeControl",
			isGameRunning = true,
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
		manager.gridBeginPos = Vector2.Zero;
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		manager.pausePacket = false;
		manager.pauseZombie = false;
	}

	private async Task VerifyBossPause(TowerDefenseManager manager, string scenePath, bool isDave, string label)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(packedScene), label + "必须加载正式角色场景。");
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return;
		}
		TowerDefenseZombie boss = (isDave ? ((TowerDefenseZombie)packedScene.Instantiate<TowerDefenseZombieBossDave>(PackedScene.GenEditState.Disabled)) : ((TowerDefenseZombie)packedScene.Instantiate<TowerDefenseZombieBoss>(PackedScene.GenEditState.Disabled)));
		Check(GodotObject.IsInstanceValid(boss), label + "必须从正式角色场景实例化。");
		if (GodotObject.IsInstanceValid(boss))
		{
			boss.Name = label + "AppleClockPauseRuntime";
			boss.inGame = true;
			boss.ProcessMode = ProcessModeEnum.Disabled;
			_control.characterNode.AddChild(boss, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(8);
			Check(GodotObject.IsInstanceValid(boss.sprite), label + "必须完成正式动画根节点初始化。");
			if (!GodotObject.IsInstanceValid(boss.sprite))
			{
				await CleanupCharacters(boss);
				return;
			}
			VerifySchedulerFreeze(manager, boss, isDave, label);
			VerifyHeadAttackPauseFreeze(manager, boss, isDave, label);
			await CleanupCharacters(boss);
		}
	}

	private void VerifySchedulerFreeze(TowerDefenseManager manager, TowerDefenseZombie boss, bool isDave, string label)
	{
		ConfigureRestProbe(boss, isDave);
		manager.pauseZombie = true;
		boss.BatchUpdate(0.5);
		Check(IsRestProbeFrozen(boss, isDave) && boss.sprite.pause, label + "在苹果闹钟暂停期间不能推进 Boss 调度队列。");
		manager.pauseZombie = false;
	}

	private void VerifyHeadAttackPauseFreeze(TowerDefenseManager manager, TowerDefenseZombie boss, bool isDave, string label)
	{
		ResetBossScheduler(boss, isDave);
		manager.pauseZombie = false;
		boss.spritePause = false;
		InvokeHeadAttackPauseStart(boss);
		Check(boss.spritePause, label + "吐球事件必须立刻持有动画暂停。");
		manager.pauseZombie = true;
		AdvanceBossManually(boss, 75, 1.0 / 60.0);
		Check(boss.spritePause && boss.sprite.pause, label + "苹果闹钟暂停超过一秒时，吐球等待不能提前释放。");
		manager.pauseZombie = false;
		AdvanceBossManually(boss, 75, 1.0 / 60.0);
		Check(!boss.spritePause, label + "苹果闹钟解除后，吐球等待必须按剩余时间释放。");
	}

	private static void ConfigureRestProbe(TowerDefenseZombie boss, bool isDave)
	{
		if (isDave && boss is TowerDefenseZombieBossDave towerDefenseZombieBossDave)
		{
			towerDefenseZombieBossDave.stateMethodList = new Godot.Collections.Array { "HeadIdle" };
			towerDefenseZombieBossDave.isRest = true;
			towerDefenseZombieBossDave.restTime = 0.1;
			towerDefenseZombieBossDave.restTimer = 0.2;
			towerDefenseZombieBossDave.stateNow = "";
			return;
		}
		if (boss is TowerDefenseZombieBoss towerDefenseZombieBoss)
		{
			towerDefenseZombieBoss.stateMethodList = new Godot.Collections.Array { "HeadIdle" };
			towerDefenseZombieBoss.isRest = true;
			towerDefenseZombieBoss.restTime = 0.1;
			towerDefenseZombieBoss.restTimer = 0.2;
			towerDefenseZombieBoss.stateNow = "";
			return;
		}
		throw new InvalidOperationException("测试收到未知僵王类型。");
	}

	private static bool IsRestProbeFrozen(TowerDefenseZombie boss, bool isDave)
	{
		if (isDave && boss is TowerDefenseZombieBossDave towerDefenseZombieBossDave)
		{
			if (towerDefenseZombieBossDave.isRest && towerDefenseZombieBossDave.stateNow == "" && towerDefenseZombieBossDave.stateMethodList.Count == 1)
			{
				return (string)towerDefenseZombieBossDave.stateMethodList[0] == "HeadIdle";
			}
			return false;
		}
		if (boss is TowerDefenseZombieBoss towerDefenseZombieBoss)
		{
			if (towerDefenseZombieBoss.isRest && towerDefenseZombieBoss.stateNow == "" && towerDefenseZombieBoss.stateMethodList.Count == 1)
			{
				return (string)towerDefenseZombieBoss.stateMethodList[0] == "HeadIdle";
			}
			return false;
		}
		return false;
	}

	private static void ResetBossScheduler(TowerDefenseZombie boss, bool isDave)
	{
		if (isDave && boss is TowerDefenseZombieBossDave towerDefenseZombieBossDave)
		{
			towerDefenseZombieBossDave.stateMethodList = new Godot.Collections.Array();
			towerDefenseZombieBossDave.isRest = false;
			towerDefenseZombieBossDave.restTimer = 0.0;
			towerDefenseZombieBossDave.stateNow = "";
			return;
		}
		if (boss is TowerDefenseZombieBoss towerDefenseZombieBoss)
		{
			towerDefenseZombieBoss.stateMethodList = new Godot.Collections.Array();
			towerDefenseZombieBoss.isRest = false;
			towerDefenseZombieBoss.restTimer = 0.0;
			towerDefenseZombieBoss.stateNow = "";
			return;
		}
		throw new InvalidOperationException("测试收到未知僵王类型。");
	}

	private static void InvokeHeadAttackPauseStart(TowerDefenseZombie boss)
	{
		MethodInfo? method = boss.GetType().GetMethod("BeginHeadAttackPause", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new MissingMethodException(boss.GetType().FullName, "BeginHeadAttackPause");
		}
		method.Invoke(boss, null);
	}

	private static void AdvanceBossManually(TowerDefenseZombie boss, int frameCount, double delta)
	{
		for (int i = 0; i < frameCount; i++)
		{
			boss.BatchUpdate(delta);
		}
	}

	private async Task CleanupCharacters(params TowerDefenseCharacter[] characters)
	{
		foreach (TowerDefenseCharacter towerDefenseCharacter in characters)
		{
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.IsQueuedForDeletion())
			{
				towerDefenseCharacter.QueueFree();
			}
		}
		await WaitFrames(5);
	}

	private async Task CleanupBattleFixture(TowerDefenseManager manager, TowerDefenseControlNew previousControl, Vector2 previousGridBegin, Vector2 previousGridSize, Vector2I previousGridNum, bool previousPausePacket, bool previousPauseZombie)
	{
		if (GodotObject.IsInstanceValid(_control?.characterNode))
		{
			foreach (Node child in _control.characterNode.GetChildren())
			{
				if (GodotObject.IsInstanceValid(child) && !child.IsQueuedForDeletion())
				{
					child.QueueFree();
				}
			}
		}
		await WaitFrames(5);
		if (GodotObject.IsInstanceValid(manager))
		{
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousGridBegin;
			manager.gridSize = previousGridSize;
			manager.gridNum = previousGridNum;
			manager.pausePacket = previousPausePacket;
			manager.pauseZombie = previousPauseZombie;
		}
		if (GodotObject.IsInstanceValid(_control) && !_control.IsQueuedForDeletion())
		{
			_control.QueueFree();
		}
		await WaitFrames(3);
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

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[BugOverviewZombossAppleClockPauseRuntimeTest] " + failure);
		}
		bool flag = _failures.Count == 0 && _checks == 15;
		GD.Print($"ZOMBOSS_APPLE_CLOCK_PAUSE_RESULT passed={flag} checks={_checks} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
