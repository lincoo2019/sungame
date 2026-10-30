using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewPogoballDeathMotionRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Zombie/Challenge/Pogoball/Scene/TowerDefenseZombiePogoball.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseZombiePogoball zombie = null;
		try
		{
			_ = 4;
			try
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/Pogoball/Scene/TowerDefenseZombiePogoball.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The real Pogoball zombie scene must load.");
				zombie = packedScene?.Instantiate<TowerDefenseZombiePogoball>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie), "The real Pogoball zombie must instantiate.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_004e;
				}
				zombie.editorPreviewMode = true;
				zombie.inGame = false;
				AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				TowerDefenseZombiePogoball towerDefenseZombiePogoball = zombie;
				if (towerDefenseZombiePogoball.sprite == null)
				{
					towerDefenseZombiePogoball.sprite = zombie.GetNodeOrNull<AdobeAnimateSprite>("SpriteGroup/TransformPoint/ZombiePogoball");
				}
				Check(GodotObject.IsInstanceValid(zombie.sprite), "The real Pogoball animation sprite must bind before exercising its state callback.");
				if (!GodotObject.IsInstanceValid(zombie.sprite))
				{
					goto end_IL_004e;
				}
				AttackComponent attackComponent = zombie.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				AttackComponent attackComponent2 = zombie.componentManager?.GetRuntime<AttackComponent>("character.attack.1");
				zombie.attackComponent = attackComponent;
				typeof(TowerDefenseZombiePogoball).GetField("_attackComponent2", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(zombie, attackComponent2);
				Check(attackComponent != null && !attackComponent.IsReleased && attackComponent2 != null && !attackComponent2.IsReleased, "The real Pogoball primary and jump attack components must be available.");
				if ((attackComponent?.IsReleased ?? true) || (attackComponent2?.IsReleased ?? true))
				{
					goto end_IL_004e;
				}
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				zombie.die = true;
				zombie.hasPogo = true;
				zombie.GlobalPosition = new Vector2(600f, 200f);
				zombie.ySpeed = 0.0;
				zombie.pogoPlant = false;
				zombie.quake = false;
				Vector2 globalPosition = zombie.GlobalPosition;
				zombie.PogoProcessing(1.0);
				Check(zombie.GlobalPosition.IsEqualApprox(globalPosition), $"Dead Pogoball corpse must not keep advancing; before={globalPosition}, after={zombie.GlobalPosition}.");
				double speedBefore = zombie.ySpeed;
				bool jumpBefore = zombie.isJump;
				bool pogoPlantBefore = zombie.pogoPlant;
				bool quakeBefore = zombie.quake;
				zombie.Land();
				await WaitFrames(2);
				Check(Math.Abs(zombie.ySpeed - speedBefore) < 0.0001, $"Dead Pogoball corpse must not restart an upward bounce; before={speedBefore}, after={zombie.ySpeed}.");
				Check(zombie.isJump == jumpBefore && zombie.pogoPlant == pogoPlantBefore && zombie.quake == quakeBefore, "Dead Pogoball landing must not restart jump-state transitions.");
				zombie.ProcessMode = ProcessModeEnum.Always;
				zombie.die = false;
				zombie.nearDie = false;
				zombie.GlobalPosition = new Vector2(600f, 200f);
				zombie.ySpeed = 0.0;
				zombie.pogoPlant = true;
				zombie.quake = false;
				zombie.isJump = false;
				zombie.jumpWait = 0;
				zombie.jumpToPos = 400.0;
				zombie.Land();
				await WaitSeconds(0.1);
				Check(zombie.GlobalPosition.X < 599f, "Focused fixture must enter the authored horizontal pogo tween before death.");
				zombie.die = true;
				zombie.DieEntered();
				Vector2 positionAtDeath = zombie.GlobalPosition;
				await WaitSeconds(0.6);
				Check(GodotObject.IsInstanceValid(zombie) && zombie.GlobalPosition.IsEqualApprox(positionAtDeath), $"A pogo tween already in progress must stop at death; atDeath={positionAtDeath}, after={zombie.GlobalPosition}.");
				Check(Math.Abs(zombie.ySpeed) < 0.0001, $"Death during a pogo tween must clear upward bounce speed; after={zombie.ySpeed}.");
				Check(!zombie.isJump && !zombie.pogoPlant && !zombie.quake, "Death during a pogo tween must clear pending jump transitions.");
				RemoveChild(zombie);
				AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				bool condition = (bool)(typeof(TowerDefenseZombiePogoball).GetField("_stateSignalsConnected", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(zombie) ?? ((object)false));
				Check(condition, "A temporary tree exit/re-entry must retain Pogoball state callbacks when Ready does not rerun.");
				goto end_IL_002f;
				end_IL_004e:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewPogoballDeathMotionRuntimeTest] Unexpected exception: {value}");
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
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"POGOBALL_DEATH_MOTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewPogoballDeathMotionRuntimeTest] " + message);
		}
	}
}
