using System;
using System.Threading.Tasks;
using Godot;

public partial class SwanRiderZeroHealthDeathRuntimeTest : Node
{
	private const string SwanScenePath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseZombieSwanRider swan = null;
		try
		{
			try
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The real SwanRider scene must load.");
				swan = packedScene?.Instantiate<TowerDefenseZombieSwanRider>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(swan), "The real SwanRider must instantiate.");
				if (!GodotObject.IsInstanceValid(swan))
				{
					goto end_IL_003f;
				}
				swan.inGame = false;
				swan.ProcessMode = ProcessModeEnum.Disabled;
				swan.skipDestroySet = true;
				AddChild(swan, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				Check(swan.StateMachine?.IsInitialized ?? false, "The SwanRider state machine must initialize.");
				Check(swan.SendStateEvent("ToWalk"), "The fixture must enter the normal walking state.");
				Check(swan.StateMachine?.CurrentStateHandle?.StableId == "zombie.walk", "The fixture must start walking; got " + swan.StateMachine?.CurrentStateHandle?.StableId + ".");
				double num = swan.Hurt(swan.instance.hitpoints + 1.0, playSplatAudio: false);
				Check(num > 0.0 && swan.instance.hitpoints <= 0.0, $"The real hit must be lethal; damage={num}, hp={swan.instance.hitpoints}.");
				Check(swan.die && swan.instance.die, "A zero-health SwanRider must be marked dead in both runtime layers.");
				Check(swan.StateMachine?.CurrentStateHandle?.StableId == "zombie.die", "Lethal damage must enter Die; got " + swan.StateMachine?.CurrentStateHandle?.StableId + ".");
				swan.Walk();
				Check(swan.StateMachine?.CurrentStateHandle?.StableId == "zombie.die", "A stale Walk callback must preserve Die; got " + swan.StateMachine?.CurrentStateHandle?.StableId + ".");
				Check(swan.sprite.clip == swan.dieAnimeClip, "A stale Walk callback must preserve the death clip; got " + swan.sprite.clip + ".");
				Check(swan.die && swan.instance.hitpoints <= 0.0, "The SwanRider must remain terminal after the stale callback.");
				goto end_IL_0036;
				end_IL_003f:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[SwanRiderZeroHealthDeathRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0036;
			}
			return;
			end_IL_0036:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(swan))
			{
				swan.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 11;
		GD.Print($"SWAN_RIDER_ZERO_HEALTH_DEATH_RESULT passed={flag} checks={_checks} failures={_failures}");
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[SwanRiderZeroHealthDeathRuntimeTest] " + message);
		}
	}
}
