using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewImpScaredyUndergroundRecoveryRuntimeTest : Node
{
	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Challenge/ImpScaredy/Scene/TowerDefenseZombieImpScaredy.tscn";

	private const string TimerInstanceId = "character.timer";

	private const double HiddenTickHeal = 25.0;

	private int _checks;

	private int _failures;

	private TowerDefenseManager _manager;

	private TowerDefenseControlNew _control;

	public override async void _Ready()
	{
		TowerDefenseZombieImpScaredy zombie = null;
		try
		{
			_ = 1;
			try
			{
				CreateGameRunningContext();
				zombie = await CreateFixture();
				Check(GodotObject.IsInstanceValid(zombie), "真实胆小鬼僵尸场景必须能实例化。");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_003e;
				}
				CharacterTimerComponent runtime = zombie.componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
				Check(runtime != null && !runtime.IsReleased, "胆小鬼僵尸必须挂接专用计时器组件。");
				Check(zombie.StateMachine?.IsInitialized ?? false, "胆小鬼僵尸状态机必须初始化。");
				Check(zombie.SendStateEvent("ToWalk") && zombie.CurrentStateHandle?.StableId == "zombie.walk", "胆小鬼僵尸必须先进入行走状态；当前 " + (zombie.CurrentStateHandle?.StableId ?? "<null>") + "。");
				zombie.ScareEntered();
				zombie.ScareProcessing(1.0 / 60.0);
				Check(zombie.scare, "隐藏处理必须激活 scare 标记。");
				Check(zombie.instance.maskFlags == 16, "陆地隐藏状态必须切换到地下碰撞层。");
				zombie.instance.hitpoints = zombie.instance.hitpointsNearDeath - 10.0;
				zombie.instance.nearDie = true;
				zombie.nearDie = true;
				double beforeHeal = zombie.instance.hitpoints;
				runtime.Run("Spawn", 0.0);
				runtime.PhysicsProcess(1.0 / 60.0, 1uL);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				Check(Mathf.IsEqualApprox(zombie.instance.hitpoints, beforeHeal + 25.0), $"nearDie 隐藏恢复必须加血；当前 {zombie.instance.hitpoints}，期望 {beforeHeal + 25.0}。");
				Check(!zombie.nearDie && !zombie.instance.nearDie, "隐藏恢复必须解除 nearDie，避免基类持续扣血把僵尸卡在地下。");
				Check(zombie.scare && zombie.instance.maskFlags == 16, "未满血时胆小鬼仍应保持地下隐藏。");
				for (int i = 0; i < 32; i++)
				{
					if (!(zombie.instance.hitpoints < zombie.instance.hitpointsSave))
					{
						break;
					}
					zombie.Timeout("Spawn");
					zombie.ScareProcessing(1.0 / 60.0);
				}
				Check(Mathf.IsEqualApprox(zombie.instance.hitpoints, zombie.instance.hitpointsSave), "胆小鬼隐藏恢复满血时必须封顶到最大血量。");
				Check(zombie.CurrentStateHandle?.StableId == "zombie.imp_scaredy.up", "满血后必须进入出土状态；当前 " + (zombie.CurrentStateHandle?.StableId ?? "<null>") + "。");
				zombie.ScareExited();
				Check(!zombie.scare && zombie.instance.maskFlags == 9, "出土时必须恢复地面碰撞层并清理 scare 标记。");
				goto end_IL_002f;
				end_IL_003e:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewImpScaredyUndergroundRecoveryRuntimeTest] 未预期异常：{value}");
				goto end_IL_002f;
			}
			return;
			end_IL_002f:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			SetGameRunningContext(null, null);
		}
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"BUG_OVERVIEW_IMP_SCAREDY_UNDERGROUND_RECOVERY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void CreateGameRunningContext()
	{
		_manager = new TowerDefenseManager();
		_control = new TowerDefenseControlNew
		{
			isGameRunning = true
		};
		SetGameRunningContext(_manager, _control);
	}

	private static void SetGameRunningContext(TowerDefenseManager manager, TowerDefenseControlNew control)
	{
		typeof(TowerDefenseManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(null, manager);
		if (manager != null)
		{
			manager.currentControl = control;
		}
	}

	private async Task<TowerDefenseZombieImpScaredy> CreateFixture()
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/ImpScaredy/Scene/TowerDefenseZombieImpScaredy.tscn", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return null;
		}
		TowerDefenseZombieImpScaredy zombie = packedScene.Instantiate<TowerDefenseZombieImpScaredy>(PackedScene.GenEditState.Disabled);
		zombie.inGame = false;
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		zombie.skipDestroySet = true;
		AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		for (int frame = 0; frame < 3; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		if (zombie.hurtComponent != null)
		{
			zombie.hurtComponent.healthEffectScene = null;
		}
		return zombie;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewImpScaredyUndergroundRecoveryRuntimeTest] " + message);
		}
	}
}
