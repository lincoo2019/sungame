using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class ExplosionDispatchBudgetRuntimeTest : Node
{
	private const int BlastCount = 600;

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ExplosionDispatchBudgetControlStub control = null;
		TowerDefenseZombie target = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00c2;
				}
				control = new ExplosionDispatchBudgetControlStub
				{
					Name = "ExplosionDispatchBudgetControl",
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
				target = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(target), "The production normal-zombie fixture must instantiate.");
				if (!GodotObject.IsInstanceValid(target))
				{
					goto end_IL_00c2;
				}
				target.Name = "ExplosionDispatchTarget";
				target.inGame = false;
				target.editorPreviewMode = true;
				target.gridPos = new Vector2I(5, 3);
				target.GlobalPosition = new Vector2(500f, 228f);
				control.characterNode.AddChild(target, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				manager.CharacterRegister(target);
				Check(manager.characterRegistry.GetActiveCharacters().Contains(target) && target.HasHitBox && target.IsHitBoxMonitorable, $"The real target must be registered with an active explosion hitbox; registered={manager.characterRegistry.GetActiveCharacters().Contains(target)}, hasHitBox={target.HasHitBox}, monitorable={target.IsHitBoxMonitorable}.");
				Check(target.CanReceiveExplosionHit(), "The initialized target must populate the explosion hitbox eligibility cache.");
				target.SetHitBoxMonitorable(monitorable: false);
				Check(!target.CanReceiveExplosionHit(), "Disabling hitbox monitoring must invalidate explosion hit eligibility immediately.");
				target.SetHitBoxMonitorable(monitorable: true);
				target.SetHitBoxMonitorSuppressed(TowerDefenseCharacter.HitBoxSuppressionReason.Paused, suppressed: true);
				Check(!target.CanReceiveExplosionHit(), "Monitor suppression must invalidate explosion hit eligibility immediately.");
				target.SetHitBoxMonitorSuppressed(TowerDefenseCharacter.HitBoxSuppressionReason.Paused, suppressed: false);
				Check(target.CanReceiveExplosionHit(), "Clearing monitor suppression must restore explosion hit eligibility.");
				TowerDefenseCharacterEventExplodeHurt towerDefenseCharacterEventExplodeHurt = new TowerDefenseCharacterEventExplodeHurt
				{
					type = "Jala"
				};
				Check(towerDefenseCharacterEventExplodeHurt.DamageKind == TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.JALA, "Jala resources must compile their authored string before target dispatch.");
				towerDefenseCharacterEventExplodeHurt.type = "Mine";
				Check(towerDefenseCharacterEventExplodeHurt.DamageKind == TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.MINE, "Mine resources must refresh their compiled damage kind when edited.");
				ExplosionDispatchCountingEvent explosionDispatchCountingEvent = new ExplosionDispatchCountingEvent();
				Array<TowerDefenseCharacterEventBase> array = new Array<TowerDefenseCharacterEventBase> { explosionDispatchCountingEvent };
				ExplosionDispatchCountingEvent.Reset(explosionDispatchCountingEvent);
				ulong schedulingFrame = Engine.GetPhysicsFrames();
				for (int i = 0; i < 600; i++)
				{
					TowerDefenseExplode.CreateExplode(target.GlobalPosition, new Vector2(0.5f, 0.5f), array, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, -1);
				}
				array.Clear();
				Check(ExplosionDispatchCountingEvent.TotalHits == 0, "Explosion damage must not execute in the scheduling physics frame.");
				for (int frame = 0; frame < 60; frame++)
				{
					if (ExplosionDispatchCountingEvent.TotalHits >= 600)
					{
						break;
					}
					await WaitFrames(1);
				}
				int num = 0;
				ulong num2 = 18446744073709551615uL;
				foreach (KeyValuePair<ulong, int> item in ExplosionDispatchCountingEvent.HitsByPhysicsFrame)
				{
					num = Math.Max(num, item.Value);
					num2 = Math.Min(num2, item.Key);
				}
				Check(num2 > schedulingFrame, "The dispatcher must preserve the one-physics-frame delay.");
				Check(ExplosionDispatchCountingEvent.TotalHits == 600, $"All queued blasts must eventually execute; hits={ExplosionDispatchCountingEvent.TotalHits}.");
				Check(ExplosionDispatchCountingEvent.HitsByPhysicsFrame.Count == 1, "All ready explosions and their targets must settle in one physics frame.");
				Check(num == 600, $"The atomic explosion frame must contain every queued hit; maxHits={num}.");
				Check(ExplosionDispatchCountingEvent.PreservedReference, "Queued events must retain the original Resource reference without a deep copy.");
				goto end_IL_00b0;
				end_IL_00c2:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[ExplosionDispatchBudgetRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b0;
			}
			return;
			end_IL_00b0:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(target))
			{
				manager.CharacterUnregister(target);
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
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"EXPLOSION_ATOMIC_DISPATCH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
			GD.PushError("[ExplosionDispatchBudgetRuntimeTest] " + message);
		}
	}
}
