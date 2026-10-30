using System;
using System.Threading.Tasks;
using Godot;

public partial class SleepComponentHypnotistFastPathRuntimeTest : Node
{
	private const string CactusScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn";

	private const string HypnotistScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefensePlantCactus cactus = null;
		TowerDefenseZombieHypnotist hypnotist = null;
		try
		{
			_ = 1;
			try
			{
				cactus = Instantiate<TowerDefensePlantCactus>("res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn");
				hypnotist = Instantiate<TowerDefenseZombieHypnotist>("res://Asset/Anime/Character/Zombie/Chapter5/Hypnotist/Scene/TowerDefenseZombieHypnotist.tscn");
				Check(GodotObject.IsInstanceValid(cactus) && GodotObject.IsInstanceValid(hypnotist), "The real Cactus and Hypnotist scenes must instantiate.");
				if (!GodotObject.IsInstanceValid(cactus) || !GodotObject.IsInstanceValid(hypnotist))
				{
					goto end_IL_0048;
				}
				cactus.inGame = false;
				cactus.ProcessMode = ProcessModeEnum.Disabled;
				AddChild(cactus, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				SleepComponent sleep = cactus.componentManager?.GetRuntime<SleepComponent>("character.sleep");
				Check(sleep != null && !sleep.IsReleased, "The real Cactus must bind its production SleepComponent.");
				SleepComponentHypnotistFastPathRuntimeTest sleepComponentHypnotistFastPathRuntimeTest = this;
				int condition;
				if (string.Equals(cactus.config?.sleepTime, "Never", StringComparison.OrdinalIgnoreCase))
				{
					condition = ((sleep != null && !sleep.CanSleep()) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				sleepComponentHypnotistFastPathRuntimeTest.Check((byte)condition != 0, "A Never-configured Cactus without buffs must remain awake.");
				TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff = null;
				foreach (TowerDefenseCharacterEventBase item in hypnotist.rangeEvent)
				{
					if (item is TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff2)
					{
						towerDefenseCharacterEventAddBuff = towerDefenseCharacterEventAddBuff2;
						break;
					}
				}
				Check(towerDefenseCharacterEventAddBuff != null && towerDefenseCharacterEventAddBuff.buffList.Count > 0 && towerDefenseCharacterEventAddBuff.buffList[0] is TowerDefenseCharacterBuffSleep, "The real Hypnotist range event must carry the production Sleep buff.");
				if (sleep == null || sleep.IsReleased || towerDefenseCharacterEventAddBuff == null)
				{
					goto end_IL_0048;
				}
				towerDefenseCharacterEventAddBuff.Execute(Vector2.Zero, cactus);
				await WaitFrames(1);
				Check(cactus.buff.HasActiveBuffs && cactus.buff.BuffHas("Sleep"), "The Hypnotist event must publish the Sleep buff on the Cactus.");
				Check(sleep.CanSleep(), "The Never fast path must yield to the active Hypnotist Sleep buff.");
				Check(cactus.IsSleep() && !cactus.componentAlive, "The real Never-configured Cactus must sleep after Hypnotist applies the buff.");
				cactus.buff.DeleteBuff("Sleep");
				sleep.SleepProcessing(0f);
				Check(!cactus.IsSleep() && cactus.componentAlive, "Removing the Hypnotist Sleep buff must restore the awake state.");
				goto end_IL_0036;
				end_IL_0048:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[SleepComponentHypnotistFastPathRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0036;
			}
			return;
			end_IL_0036:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(cactus))
			{
				cactus.QueueFree();
			}
			if (GodotObject.IsInstanceValid(hypnotist))
			{
				hypnotist.QueueFree();
			}
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 8;
		GD.Print($"SLEEP_COMPONENT_HYPNOTIST_FAST_PATH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T Instantiate<T>(string scenePath) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
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
			GD.PushError("[SleepComponentHypnotistFastPathRuntimeTest] " + message);
		}
	}
}
