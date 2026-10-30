using System;
using System.Threading.Tasks;
using Godot;

public partial class BuffHardControlLifecycleRuntimeTest : Node
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const double StepSeconds = 1.0 / 60.0;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		BuffHardControlLifecycleControlStub control = null;
		TowerDefenseZombie zombie = null;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager 自动加载必须可用。");
				if (!GodotObject.IsInstanceValid(manager))
				{
					throw new InvalidOperationException("TowerDefenseManager 不可用。");
				}
				control = new BuffHardControlLifecycleControlStub
				{
					Name = "BuffHardControlLifecycleControl",
					isGameRunning = true,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "正式普通僵尸场景必须能够加载。");
				zombie = packedScene?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie), "正式普通僵尸必须能够实例化。");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					throw new InvalidOperationException("普通僵尸实例化失败。");
				}
				zombie.editorPreviewMode = false;
				zombie.inGame = true;
				zombie.gridPos = new Vector2I(4, 2);
				zombie.GlobalPosition = new Vector2(400f, 252f);
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				BuffHardControlLifecycleRuntimeTest buffHardControlLifecycleRuntimeTest = this;
				BuffComponent buff = zombie.buff;
				buffHardControlLifecycleRuntimeTest.Check(buff != null && !buff.IsReleased && GodotObject.IsInstanceValid(zombie.sprite), "正式普通僵尸必须装配 Buff 与动画运行时。");
				if ((zombie.buff?.IsReleased ?? true) || !GodotObject.IsInstanceValid(zombie.sprite))
				{
					throw new InvalidOperationException("普通僵尸硬控依赖未初始化。");
				}
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				await WaitFrames(1);
				zombie.Walk();
				Check(zombie.timeScaleInit > 0.0, "普通僵尸基础时间倍率必须为正数。");
				TowerDefenseCharacterBuffButter butter = new TowerDefenseCharacterBuffButter
				{
					time = 4.0
				};
				zombie.buff.AddBuff(butter);
				await ManualPhysicsStep(zombie);
				Check(zombie.buff.BuffHas("Butter"), "黄油必须在完整持续时间内保持生效。");
				Check(IsStopped(zombie.timeScale) && zombie.sprite.IsRuntimeTickPaused, "黄油生效的同一个物理步必须同时停止玩法与动画时钟。");
				butter.currentTime = butter.time - 1.0 / 120.0;
				await ManualPhysicsStep(zombie);
				Check(!zombie.buff.BuffHas("Butter"), "黄油达到持续时间后必须在当前物理步移除。");
				Check(zombie.timeScale > 0.0 && zombie.sprite.timeScale > 0.0 && !zombie.sprite.IsRuntimeTickPaused, "黄油到期的同一个物理步必须同时恢复玩法与动画时钟。");
				TowerDefenseCharacterBuffDizziness expiringDizziness = new TowerDefenseCharacterBuffDizziness
				{
					time = 4.0
				};
				zombie.buff.AddBuff(expiringDizziness);
				await ManualPhysicsStep(zombie);
				Check(zombie.buff.BuffHas("Dizziness"), "眩晕必须在完整持续时间内保持生效。");
				Check(GodotObject.IsInstanceValid(expiringDizziness.dizzinessSprite) && zombie.CharacterAnimatedStatusVisualCount > 0, "眩晕生效时必须把星星贴图接入正式角色动画树。");
				Check(IsStopped(zombie.timeScale) && zombie.sprite.IsRuntimeTickPaused, "眩晕生效的同一个物理步必须同时停止玩法与动画时钟。");
				expiringDizziness.currentTime = expiringDizziness.time - 1.0 / 120.0;
				await ManualPhysicsStep(zombie);
				Check(!zombie.buff.BuffHas("Dizziness"), "眩晕达到持续时间后必须在当前物理步移除。");
				Check((!GodotObject.IsInstanceValid(expiringDizziness.dizzinessSprite) || expiringDizziness.dizzinessSprite.IsQueuedForDeletion()) && zombie.CharacterAnimatedStatusVisualCount == 0, "眩晕自然到期时必须从角色动画树移除星星贴图。");
				Check(zombie.timeScale > 0.0 && zombie.sprite.timeScale > 0.0 && !zombie.sprite.IsRuntimeTickPaused, "眩晕到期的同一个物理步必须同时恢复玩法与动画时钟。");
				TowerDefenseCharacterBuffDizziness removedDizziness = new TowerDefenseCharacterBuffDizziness
				{
					time = 4.0
				};
				zombie.buff.AddBuff(removedDizziness);
				await ManualPhysicsStep(zombie);
				Check(zombie.buff.BuffHas("Dizziness") && IsStopped(zombie.timeScale) && zombie.sprite.IsRuntimeTickPaused, "主动移除回归必须从真实眩晕停播状态开始。");
				zombie.buff.DeleteBuff("Dizziness");
				Check(!zombie.buff.BuffHas("Dizziness") && (!GodotObject.IsInstanceValid(removedDizziness.dizzinessSprite) || removedDizziness.dizzinessSprite.IsQueuedForDeletion()) && zombie.CharacterAnimatedStatusVisualCount == 0, "主动移除眩晕必须立即清除 Buff 与星星贴图。");
				Check(zombie.sprite.timeScale > 0.0 && !zombie.sprite.IsRuntimeTickPaused, "普通轮询被禁用时，主动移除眩晕仍必须立即恢复动画时钟。");
				TowerDefenseCharacterBuffFrozen buffConfig = new TowerDefenseCharacterBuffFrozen
				{
					time = 8.0,
					iceSpeedDownTime = 15.0
				};
				zombie.buff.AddBuff(buffConfig);
				TowerDefenseCharacterBuffButter deathButter = new TowerDefenseCharacterBuffButter
				{
					time = 8.0
				};
				zombie.buff.AddBuff(deathButter);
				await ManualPhysicsStep(zombie);
				Check(zombie.buff.BuffHas("Frozen") && zombie.buff.BuffHas("Butter"), "死亡回归前必须同时保留冰冻与黄油。");
				Check(IsStopped(zombie.timeScale) && zombie.sprite.IsRuntimeTickPaused, "死亡回归必须从真实停播状态开始。");
				zombie.instance.EmitHitpointsNearDie();
				Check(!zombie.buff.BuffHas("Frozen") && !zombie.buff.BuffHas("Butter") && !zombie.buff.BuffHas("IceSpeedDown"), "死亡信号必须立即清除冰冻与黄油，且不能触发冰冻自然到期的减速。");
				BuffHardControlLifecycleRuntimeTest buffHardControlLifecycleRuntimeTest2 = this;
				Sprite2D icetrapSprite = zombie.icetrapSprite;
				buffHardControlLifecycleRuntimeTest2.Check((icetrapSprite == null || !icetrapSprite.Visible) && (!GodotObject.IsInstanceValid(deathButter.butterSprite) || deathButter.butterSprite.IsQueuedForDeletion()), "死亡信号必须立即隐藏冰块并释放黄油贴图。");
				Check(zombie.sprite.timeScale > 0.0 && !zombie.sprite.IsRuntimeTickPaused, "普通轮询被禁用时，死亡信号仍必须立即恢复死亡动画播放。");
				zombie.buff.AddBuff(new TowerDefenseCharacterBuffFrozen());
				zombie.buff.AddBuff(new TowerDefenseCharacterBuffButter());
				Check(!zombie.buff.BuffHas("Frozen") && !zombie.buff.BuffHas("Butter"), "死亡清理完成后到达的冰冻或黄油必须被直接拒绝。");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BuffHardControlLifecycleRuntimeTest] 未预期异常：{value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 24;
		GD.Print($"BUFF_HARD_CONTROL_LIFECYCLE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task ManualPhysicsStep(TowerDefenseCharacter character)
	{
		character.BatchUpdate(1.0 / 60.0);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static bool IsStopped(double value)
	{
		return Math.Abs(value) <= 0.0001;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BuffHardControlLifecycleRuntimeTest] " + message);
		}
	}
}
