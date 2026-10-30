using System;
using System.Threading.Tasks;
using Godot;

public partial class ZombieZeroHealthDeathGuardRuntimeTest : Node
{
	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string AxeZombieScenePath = "res://Asset/Anime/Character/Zombie/Challenge/Axe/Scene/TowerDefenseZombieAxe.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseZombieNormal zombie = null;
		TowerDefenseZombieNormal survivor = null;
		TowerDefenseZombieAxe axeZombie = null;
		try
		{
			_ = 2;
			try
			{
				zombie = await CreateFixture<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(zombie), "The real normal zombie fixture must instantiate.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_00bf;
				}
				Check(zombie.StateMachine?.IsInitialized ?? false, "The zombie state machine must initialize.");
				Check(zombie.SendStateEvent("ToWalk"), "The fixture must accept its initial walk event.");
				Check(zombie.CurrentStateHandle?.StableId == "zombie.walk", "The fixture must start walking; got " + (zombie.CurrentStateHandle?.StableId ?? "<null>") + ".");
				zombie.instance.hitpoints = 0.0;
				zombie.BatchUpdate(1.0 / 60.0);
				Check(zombie.instance.die && zombie.die, "Fixed update must commit zero-health death in both runtime layers.");
				Check(zombie.instance.nearDie && zombie.nearDie, "Fixed update must also converge the near-death flags.");
				Check(zombie.CurrentStateHandle?.StableId == "zombie.die", "Zero health must enter zombie.die; got " + (zombie.CurrentStateHandle?.StableId ?? "<null>") + ".");
				Check(zombie.zombieDeathComponent?.IsDeathAnimationClip(zombie.sprite.clip) ?? false, "Zero health must play the death clip; got " + zombie.sprite.clip + ".");
				zombie.Walk();
				zombie.Attack();
				zombie.SendStateEvent("ToIdle");
				Check(zombie.CurrentStateHandle?.StableId == "zombie.die", "Live-state callbacks must preserve zombie.die; got " + (zombie.CurrentStateHandle?.StableId ?? "<null>") + ".");
				Check(zombie.zombieDeathComponent?.IsDeathAnimationClip(zombie.sprite.clip) ?? false, "Live-state callbacks must preserve the death clip; got " + zombie.sprite.clip + ".");
				Check(zombie.instance.die && zombie.die && zombie.instance.hitpoints <= 0.0, "The zero-health zombie must remain terminal after stale callbacks.");
				survivor = await CreateFixture<TowerDefenseZombieNormal>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
				Check(GodotObject.IsInstanceValid(survivor), "The keepAlive fixture must instantiate.");
				if (!GodotObject.IsInstanceValid(survivor))
				{
					goto end_IL_00bf;
				}
				survivor.instance.keepAlive = true;
				survivor.instance.hitpoints = 0.0;
				survivor.BatchUpdate(1.0 / 60.0);
				survivor.Walk();
				Check(!survivor.instance.die && !survivor.die, "keepAlive must remain exempt from forced zero-health death.");
				Check(survivor.CurrentStateHandle?.StableId == "zombie.walk", "A keepAlive zombie may still walk; got " + (survivor.CurrentStateHandle?.StableId ?? "<null>") + ".");
				axeZombie = await CreateFixture<TowerDefenseZombieAxe>("res://Asset/Anime/Character/Zombie/Challenge/Axe/Scene/TowerDefenseZombieAxe.tscn");
				Check(GodotObject.IsInstanceValid(axeZombie), "The axe zombie fixture must instantiate.");
				if (!GodotObject.IsInstanceValid(axeZombie))
				{
					goto end_IL_00bf;
				}
				axeZombie.instance.hitpoints = 0.0;
				axeZombie.BatchUpdate(1.0 / 60.0);
				Check(axeZombie.instance.die && axeZombie.die, "Axe zero-health convergence must finish without recursive damage events.");
				Check(axeZombie.CurrentStateHandle?.StableId == "zombie.die", "Axe zero health must enter zombie.die; got " + (axeZombie.CurrentStateHandle?.StableId ?? "<null>") + ".");
				goto end_IL_004c;
				end_IL_00bf:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[ZombieZeroHealthDeathGuardRuntimeTest] Unexpected exception: {value}");
				goto end_IL_004c;
			}
			return;
			end_IL_004c:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(survivor))
			{
				survivor.QueueFree();
			}
			if (GodotObject.IsInstanceValid(axeZombie))
			{
				axeZombie.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 17;
		GD.Print($"ZOMBIE_ZERO_HEALTH_DEATH_GUARD_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task<T> CreateFixture<T>(string scenePath) where T : TowerDefenseZombie
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return null;
		}
		T zombie = packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
		zombie.inGame = false;
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		zombie.skipDestroySet = true;
		AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		for (int frame = 0; frame < 3; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		return zombie;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ZombieZeroHealthDeathGuardRuntimeTest] " + message);
		}
	}
}
