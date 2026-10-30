using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewSeaDemonSummonStallRuntimeTest : Node
{
	private const string ScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/SeaDemon/Scene/Base/TowerDefenseZombieSeaDemon.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseZombieSeaDemon zombie = null;
		try
		{
			_ = 4;
			try
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter3/SeaDemon/Scene/Base/TowerDefenseZombieSeaDemon.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The real Sea Demon scene must load.");
				zombie = packedScene?.Instantiate<TowerDefenseZombieSeaDemon>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie), "The real Sea Demon must instantiate.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_004e;
				}
				zombie.editorPreviewMode = true;
				zombie.inGame = false;
				AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				Check(zombie.StateMachine?.IsInitialized ?? false, "The real Sea Demon state machine must initialize.");
				Check(GodotObject.IsInstanceValid(zombie.sprite) && zombie.sprite.HasClip("Point"), "The real Sea Demon Point clip must be available.");
				zombie.Walk();
				await WaitFrames(1);
				Check(zombie.CurrentStateHandle?.StableId == "zombie.walk", "Fixture must start from walk; current=" + zombie.CurrentStateHandle?.StableId + ".");
				Check(zombie.SendStateEvent("ToPoint"), "The authored ToPoint transition must be accepted.");
				Check(zombie.CurrentStateHandle?.StableId == "zombie.sea_demon.point" && zombie.isPointing && zombie.sprite.clip == "Point", $"Fixture must enter the real Point state; current={zombie.CurrentStateHandle?.StableId}, isPointing={zombie.isPointing}, clip={zombie.sprite.clip}.");
				zombie.spawnNext = true;
				zombie.AnimeCompleted("Point");
				await WaitFrames(1);
				Check(zombie.CurrentStateHandle?.StableId == "zombie.walk" && !zombie.isPointing && zombie.spawnNext, $"Point completion must return to walk without consuming overlapping summon readiness; current={zombie.CurrentStateHandle?.StableId}, isPointing={zombie.isPointing}, spawnNext={zombie.spawnNext}.");
				zombie.AnimeCompleted("Walk");
				await WaitFrames(1);
				Check(zombie.CurrentStateHandle?.StableId == "zombie.sea_demon.point" && zombie.isPointing && !zombie.spawnNext, $"The retained summon readiness must retry from locomotion; current={zombie.CurrentStateHandle?.StableId}, isPointing={zombie.isPointing}, spawnNext={zombie.spawnNext}.");
				zombie.AnimeCompleted("Point");
				await WaitFrames(1);
				Check(zombie.CurrentStateHandle?.StableId == "zombie.walk" && !zombie.isPointing, $"A completed summon must recover locomotion; current={zombie.CurrentStateHandle?.StableId}, isPointing={zombie.isPointing}.");
				goto end_IL_002f;
				end_IL_004e:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSeaDemonSummonStallRuntimeTest] Unexpected exception: {value}");
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
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"SEA_DEMON_SUMMON_STALL_RESULT passed={flag} checks={_checks} failures={_failures}");
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
			GD.PushError("[BugOverviewSeaDemonSummonStallRuntimeTest] " + message);
		}
	}
}
