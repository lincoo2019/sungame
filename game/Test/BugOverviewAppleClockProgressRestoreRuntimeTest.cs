using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewAppleClockProgressRestoreRuntimeTest : Node
{
	private const string AppleKey = "PlantApple";

	private const string ApplePacketPath = "res://Asset/Anime/Character/Plant/Star/Apple/Packet/PlantApple.tres";

	private const string AppleScenePath = "res://Asset/Anime/Character/Plant/Star/Apple/Scene/TowerDefensePlantApple.tscn";

	private const double SavedRemainingSeconds = 0.35;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		ResourceManager resources = ResourceManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Resource previousPacket = null;
		Resource previousCharacter = null;
		bool hadPreviousPacket = resources?.TOWERDEFENSE_PACKETS.TryGetValue("PlantApple", out previousPacket) ?? false;
		bool hadPreviousCharacter = resources?.TOWERDEFENSE_CHARCATERS.TryGetValue("PlantApple", out previousCharacter) ?? false;
		bool previousPausePacket = manager?.pausePacket ?? false;
		bool previousPauseZombie = manager?.pauseZombie ?? false;
		AppleClockProgressRestoreRuntimeControlStub control = null;
		TowerDefensePlantApple original = null;
		TowerDefensePlantApple restored = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(resources), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(resources))
				{
					throw new InvalidOperationException("Required battle autoloads are missing.");
				}
				control = new AppleClockProgressRestoreRuntimeControlStub
				{
					Name = "AppleClockProgressRestoreRuntimeControl",
					isGameRunning = true,
					isInit = false,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Star/Apple/Packet/PlantApple.tres", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Star/Apple/Scene/TowerDefensePlantApple.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(packedScene), "The production Apple Clock packet and scene must load.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene))
				{
					throw new InvalidOperationException("Production Apple Clock fixtures did not load.");
				}
				resources.TOWERDEFENSE_PACKETS["PlantApple"] = towerDefensePacketConfig;
				resources.TOWERDEFENSE_CHARCATERS["PlantApple"] = packedScene;
				original = towerDefensePacketConfig.Create(Vector2.Zero, Vector2I.Zero) as TowerDefensePlantApple;
				Check(GodotObject.IsInstanceValid(original), "The production packet must create a real Apple Clock.");
				if (!GodotObject.IsInstanceValid(original))
				{
					throw new InvalidOperationException("Production Apple Clock creation failed.");
				}
				control.characterNode.AddChild(original, forceReadableName: false, InternalMode.Disabled);
				await WaitPhysicsFrames(5);
				Check(original.run && manager.pausePacket && manager.pauseZombie, "The original Apple Clock must activate packet and zombie pause.");
				original.runTimer = 0.35;
				TowerDefenseCharacterSaveConfigCSharp characterSave = new TowerDefenseCharacterSaveConfigCSharp();
				characterSave.SaveCharacter(original);
				Check(characterSave.variantSave.GetValueOrDefault("run", false).AsBool() && Math.Abs(characterSave.variantSave.GetValueOrDefault("runTimer", 0.0).AsDouble() - 0.35) < 0.0001, "Progress must capture the active effect and its remaining duration.");
				original.QueueFree();
				await WaitPhysicsFrames(3);
				original = null;
				Check(!manager.pausePacket && !manager.pauseZombie, "Removing the active Apple Clock during level exit must release its pause lease.");
				manager.pausePacket = false;
				manager.pauseZombie = false;
				control.isGameRunning = false;
				TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = (characterSave.owner = new TowerDefenseLevelSaveConfigCSharp());
				restored = characterSave.InstantiateCharacterForRestore() as TowerDefensePlantApple;
				Check(GodotObject.IsInstanceValid(restored), "The production progress path must recreate the saved Apple Clock.");
				if (!GodotObject.IsInstanceValid(restored))
				{
					throw new InvalidOperationException("Progress Apple Clock creation failed.");
				}
				towerDefenseLevelSaveConfigCSharp.charcterDicionary[characterSave.nodeName] = restored;
				characterSave.RestoreCharacter(restored);
				manager.pausePacket = true;
				manager.pauseZombie = true;
				Check(restored.run && restored.runTimer > 0.0 && restored.runTimer <= 0.35009999999999997, "Restore must preserve the saved remaining duration instead of restarting eight seconds.");
				control.isGameRunning = true;
				restored.ProcessMode = ProcessModeEnum.Inherit;
				restored.SetMainStateMachineDispatchEnabled(enabled: false);
				await WaitPhysicsFrames(75);
				Check(!GodotObject.IsInstanceValid(restored) || restored.IsQueuedForDeletion(), "The restored Apple Clock must disappear when its saved remaining duration expires.");
				Check(!manager.pausePacket && !manager.pauseZombie, "The final restored Apple Clock must release packet and zombie pause on expiry.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewAppleClockProgressRestoreRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(original) && !original.IsQueuedForDeletion())
			{
				original.QueueFree();
			}
			if (GodotObject.IsInstanceValid(restored) && !restored.IsQueuedForDeletion())
			{
				restored.QueueFree();
			}
			await WaitPhysicsFrames(3);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.pausePacket = previousPausePacket;
				manager.pauseZombie = previousPauseZombie;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			if (GodotObject.IsInstanceValid(resources))
			{
				if (hadPreviousPacket)
				{
					resources.TOWERDEFENSE_PACKETS["PlantApple"] = previousPacket;
				}
				else
				{
					resources.TOWERDEFENSE_PACKETS.Remove("PlantApple");
				}
				if (hadPreviousCharacter)
				{
					resources.TOWERDEFENSE_CHARCATERS["PlantApple"] = previousCharacter;
				}
				else
				{
					resources.TOWERDEFENSE_CHARCATERS.Remove("PlantApple");
				}
			}
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"APPLE_CLOCK_PROGRESS_RESTORE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewAppleClockProgressRestoreRuntimeTest] " + message);
		}
	}
}
