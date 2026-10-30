using System;
using Godot;

public partial class WaveInvisibleZombieSettlementRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	private WaveInvisibleZombieSettlementControlStub _control;

	private WaveInvisibleZombieSettlementProbeZombie _zombie;

	public override void _Ready()
	{
		bool flag = false;
		try
		{
			RunSettlementContract();
			flag = _failures == 0;
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[WaveInvisibleZombieSettlement] 未预期异常：{value}");
		}
		finally
		{
			ReleaseFixture();
		}
		GD.Print($"WAVE_INVISIBLE_ZOMBIE_SETTLEMENT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void RunSettlementContract()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			throw new InvalidOperationException("塔防管理器或角色注册表不可用。");
		}
		_control = new WaveInvisibleZombieSettlementControlStub
		{
			Name = "WaveInvisibleZombieSettlementControl",
			isGameRunning = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		_control.levelControl = new WaveInvisibleZombieSettlementLevelControlStub
		{
			Name = "LevelControl"
		};
		_control.AddChild(_control.levelControl, forceReadableName: false, InternalMode.Disabled);
		instance.currentControl = _control;
		TowerDefenseBattleProcessWave towerDefenseBattleProcessWave = new TowerDefenseBattleProcessWave
		{
			control = _control,
			levelControl = _control.levelControl
		};
		_control.process = towerDefenseBattleProcessWave;
		_zombie = new WaveInvisibleZombieSettlementProbeZombie
		{
			Name = "UnregisteredInvisibleZombie",
			inGame = true,
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
			invisible = true,
			instance = new TowerDefenseCharacterInstance()
		};
		_control.characterNode.AddChild(_zombie, forceReadableName: false, InternalMode.Disabled);
		_zombie.AddToGroup("Zombie", persistent: true);
		instance.CharacterRegister(_zombie);
		bool condition = instance.characterRegistry.TryGetFinalWaveTarget(TowerDefenseEnum.CHARACTER_CAMP.PLANT, out var target, out var hasPendingDestroyZombie);
		Check(condition, "已注册的存活敌对隐身僵尸必须进入最终波目标集。");
		Check(!towerDefenseBattleProcessWave.CheckFinal(), "已注册的存活敌对隐身僵尸必须阻止 Wave 结算。");
		instance.CharacterUnregister(_zombie);
		condition = instance.characterRegistry.TryGetFinalWaveTarget(TowerDefenseEnum.CHARACTER_CAMP.PLANT, out target, out hasPendingDestroyZombie);
		Check(!condition, "测试前提要求注销后的隐身僵尸暂时不在角色注册表目标集中。");
		Check(!towerDefenseBattleProcessWave.CheckFinal(), "场景组内仍有存活敌对隐身僵尸时，Wave 不得判定游戏结束。");
	}

	private void ReleaseFixture()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance) && instance.currentControl == _control)
		{
			instance.currentControl = null;
		}
		if (GodotObject.IsInstanceValid(_zombie))
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.CharacterUnregister(_zombie);
			}
			_zombie.RemoveFromGroup("Zombie");
			_zombie.Free();
		}
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.Free();
		}
		_zombie = null;
		_control = null;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[WaveInvisibleZombieSettlement] " + message);
		}
	}
}
