using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewVehicleAshDeathAnimationRuntimeTest : Node
{
	private readonly record struct VehicleCase(string Label, string ScenePath, string LiveClip, string DeathClip);

	private static readonly VehicleCase[] VehicleCases = new VehicleCase[3]
	{
		new VehicleCase("Zamboni", "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/Normal/TowerDefenseZombieZamboni.tscn", "Drive", "Wheelie&Wheelie2"),
		new VehicleCase("Catapult", "res://Asset/Anime/Character/Zombie/Chapter5/Catapult/Scene/TowerDefenseZombieCatapult.tscn", "Walk", "Bounce"),
		new VehicleCase("GargantuarZamboni", "res://Asset/Anime/Character/Zombie/Challenge/GargantuarZamboni/Scene/Base/TowerDefenseZombieGargantuarZamboni.tscn", "Drive", "Wheelie")
	};

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		_ = 1;
		try
		{
			VehicleCase[] vehicleCases = VehicleCases;
			foreach (VehicleCase vehicleCase in vehicleCases)
			{
				await VerifyAshDeathKeepsCurrentPose(vehicleCase);
			}
			await VerifyOrdinaryDeathStillUsesAuthoredClip(VehicleCases[1]);
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewVehicleAshDeathAnimationRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 26;
		GD.Print($"VEHICLE_ASH_DEATH_ANIMATION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyAshDeathKeepsCurrentPose(VehicleCase vehicleCase)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(vehicleCase.ScenePath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(packedScene), "The real " + vehicleCase.Label + " scene must load.");
		TowerDefenseZombie zombie = packedScene?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(zombie), "The real " + vehicleCase.Label + " zombie must instantiate.");
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		zombie.inGame = false;
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(3);
		Check(GodotObject.IsInstanceValid(zombie.sprite), "The real " + vehicleCase.Label + " animation sprite must bind.");
		BugOverviewVehicleAshDeathAnimationRuntimeTest bugOverviewVehicleAshDeathAnimationRuntimeTest = this;
		ZombieDeathComponent zombieDeathComponent = zombie.zombieDeathComponent;
		bugOverviewVehicleAshDeathAnimationRuntimeTest.Check(zombieDeathComponent != null && !zombieDeathComponent.IsReleased, "The real " + vehicleCase.Label + " death component must be active.");
		Check(zombie.config?.ashScene == null && zombie.dieAnimeClip == vehicleCase.DeathClip, vehicleCase.Label + " must use its authored vehicle death clip and shader-based ash fallback.");
		if (GodotObject.IsInstanceValid(zombie.sprite))
		{
			ZombieDeathComponent zombieDeathComponent2 = zombie.zombieDeathComponent;
			if (zombieDeathComponent2 != null && !zombieDeathComponent2.IsReleased)
			{
				zombie.sprite.SetAnimation(vehicleCase.LiveClip);
				Check(zombie.sprite.clip == vehicleCase.LiveClip, $"{vehicleCase.Label} fixture must start from {vehicleCase.LiveClip}; got {zombie.sprite.clip}.");
				zombie.isExplode = true;
				zombie.die = true;
				zombie.nearDie = true;
				zombie.zombieDeathComponent.DieEntered();
				Check(zombie.sprite.clip == vehicleCase.LiveClip, $"Ash death must keep {vehicleCase.Label} on {vehicleCase.LiveClip} instead of starting {vehicleCase.DeathClip}; got {zombie.sprite.clip}.");
			}
		}
		zombie.QueueFree();
		await WaitFrames(2);
	}

	private async Task VerifyOrdinaryDeathStillUsesAuthoredClip(VehicleCase vehicleCase)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(vehicleCase.ScenePath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(packedScene), "The ordinary-death control scene must load.");
		TowerDefenseZombie zombie = packedScene?.Instantiate<TowerDefenseZombie>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(zombie), "The ordinary-death control zombie must instantiate.");
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		zombie.inGame = false;
		zombie.ProcessMode = ProcessModeEnum.Disabled;
		AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(3);
		BugOverviewVehicleAshDeathAnimationRuntimeTest bugOverviewVehicleAshDeathAnimationRuntimeTest = this;
		int condition;
		if (GodotObject.IsInstanceValid(zombie.sprite))
		{
			ZombieDeathComponent zombieDeathComponent = zombie.zombieDeathComponent;
			condition = ((zombieDeathComponent != null && !zombieDeathComponent.IsReleased) ? 1 : 0);
		}
		else
		{
			condition = 0;
		}
		bugOverviewVehicleAshDeathAnimationRuntimeTest.Check((byte)condition != 0, "The ordinary-death control must bind its real sprite and death component.");
		if (GodotObject.IsInstanceValid(zombie.sprite))
		{
			ZombieDeathComponent zombieDeathComponent2 = zombie.zombieDeathComponent;
			if (zombieDeathComponent2 != null && !zombieDeathComponent2.IsReleased)
			{
				zombie.sprite.SetAnimation(vehicleCase.LiveClip);
				Check(zombie.sprite.clip == vehicleCase.LiveClip, "The ordinary-death control must start from its live animation.");
				zombie.isExplode = false;
				zombie.die = true;
				zombie.nearDie = true;
				zombie.zombieDeathComponent.DieEntered();
				Check(zombie.zombieDeathComponent.IsDeathAnimationClip(zombie.sprite.clip), "Ordinary death must still start the authored death clip; got " + zombie.sprite.clip + ".");
			}
		}
		zombie.QueueFree();
		await WaitFrames(2);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
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
			GD.PushError("[BugOverviewVehicleAshDeathAnimationRuntimeTest] " + message);
		}
	}
}
