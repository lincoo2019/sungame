using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class PlantAttackProgressRestoreRuntimeTest : Node
{
	private const string PeaShooterKey = "PlantPeaShooter";

	private const string PeaShooterPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres";

	private const string PeaShooterScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ResourceManager resources = ResourceManager.Instance;
		Resource previousPacket = null;
		Resource previousCharacter = null;
		bool hadPreviousPacket = resources?.TOWERDEFENSE_PACKETS.TryGetValue("PlantPeaShooter", out previousPacket) ?? false;
		bool hadPreviousCharacter = resources?.TOWERDEFENSE_CHARCATERS.TryGetValue("PlantPeaShooter", out previousCharacter) ?? false;
		PlantAttackProgressRestoreRuntimeControlStub control = null;
		TowerDefensePlantPeaShooter original = null;
		TowerDefensePlantPeaShooter restored = null;
		TowerDefensePlantPeaShooter legacyRestored = null;
		try
		{
			_ = 6;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(resources), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(resources))
				{
					throw new InvalidOperationException("Required battle autoloads are missing.");
				}
				control = new PlantAttackProgressRestoreRuntimeControlStub
				{
					Name = "PlantAttackProgressRestoreRuntimeControl",
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
				manager.gridBeginPos = new Vector2(256f, 45f);
				manager.gridSize = new Vector2(80f, 98f);
				manager.gridNum = new Vector2I(9, 5);
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Packet/PlantPeaShooter.tres", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(packedScene), "The production Peashooter packet and scene must load.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene))
				{
					throw new InvalidOperationException("Production Peashooter fixtures did not load.");
				}
				resources.TOWERDEFENSE_PACKETS["PlantPeaShooter"] = towerDefensePacketConfig;
				resources.TOWERDEFENSE_CHARCATERS["PlantPeaShooter"] = packedScene;
				original = TowerDefenseManager.GetPacketConfig("PlantPeaShooter")?.Create(new Vector2(416f, 241f), new Vector2I(2, 3)) as TowerDefensePlantPeaShooter;
				Check(GodotObject.IsInstanceValid(original), "The production packet must create a real Peashooter.");
				if (!GodotObject.IsInstanceValid(original))
				{
					throw new InvalidOperationException("Production Peashooter creation failed.");
				}
				control.characterNode.AddChild(original, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(5);
				FireComponent fireComponent = original.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fireComponent != null && fireComponent.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(fireComponent.sprite), "The production Peashooter FireComponent and its Head sprite must be active.");
				if (fireComponent == null || fireComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(fireComponent.sprite))
				{
					throw new InvalidOperationException("Production FireComponent did not activate.");
				}
				original.Component();
				bool flag = fireComponent.SendStateEvent(fireComponent.attackStateEvent);
				TowerDefenseCharacterSaveConfigCSharp characterSave = new TowerDefenseCharacterSaveConfigCSharp();
				characterSave.SaveCharacter(original);
				Dictionary savedFire = FindSavedFireComponent(characterSave.componentSaveList);
				Dictionary savedAnimation = savedFire.GetValueOrDefault("animationState", new Dictionary()).AsGodotDictionary();
				Check(flag && original.componentRunning && fireComponent.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && savedFire.ContainsKey("_stateMachine"), "The fixture must capture a real in-progress Fire attack state.");
				Check(savedAnimation.Count > 0 && savedAnimation.GetValueOrDefault("clip", "").AsString() == "HeadFire" && original.sprite != fireComponent.sprite, "Character progress must include the independently animated Head sprite.");
				original.QueueFree();
				await WaitFrames(5);
				original = null;
				TowerDefenseLevelSaveConfigCSharp levelSave = (characterSave.owner = new TowerDefenseLevelSaveConfigCSharp());
				restored = characterSave.InstantiateCharacterForRestore() as TowerDefensePlantPeaShooter;
				Check(GodotObject.IsInstanceValid(restored), "The production progress path must recreate the saved Peashooter.");
				if (!GodotObject.IsInstanceValid(restored))
				{
					throw new InvalidOperationException("Progress character creation failed.");
				}
				levelSave.charcterDicionary[characterSave.nodeName] = restored;
				await WaitFrames(5);
				characterSave.RestoreCharacter(restored);
				ResumeRestoredEntryCharacter(restored);
				FireComponent restoredFire = restored.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(restoredFire != null && restoredFire.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(restoredFire.sprite), "The restored Peashooter FireComponent must be active.");
				if (restoredFire == null || restoredFire.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(restoredFire.sprite))
				{
					throw new InvalidOperationException("Restored FireComponent did not activate.");
				}
				Dictionary actual = restoredFire.sprite.ExportSpriteSave();
				Check(restored.componentRunning && restoredFire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack", "Progress restore must retain the in-progress Fire state without replaying entry effects.");
				Check(AnimationProgressMatches(savedAnimation, actual), "Progress restore must resume the nested Head animation at the saved frame.");
				await WaitFrames(90);
				Dictionary dictionary = restoredFire.sprite.ExportSpriteSave();
				Check(restoredFire.StateMachine?.CurrentStateHandle?.StableId == "fire.idle", $"The restored Head animation must emit its boundary callback and leave Fire attack state; state={restoredFire.StateMachine?.CurrentStateHandle?.StableId}, clip={dictionary.GetValueOrDefault("clip", "").AsString()}, frame={dictionary.GetValueOrDefault("frameIndex", -1).AsInt32()}, elapsed={dictionary.GetValueOrDefault("elapsedTimer", -1.0).AsDouble():F4}, over={dictionary.GetValueOrDefault("clipOver", false).AsBool()}, pause={restoredFire.sprite.pause}, timeScale={restoredFire.sprite.timeScale:F3}, runtimeActive={restoredFire.sprite.IsRuntimeActive}, runtimePaused={restoredFire.sprite.IsRuntimeTickPaused}, insideTree={restoredFire.sprite.IsRuntimeInsideTree}, managerDispatch={restoredFire.sprite.RuntimeManagerDispatchActive}, displayTick={restoredFire.sprite.IsRuntimeDisplayTickActive}, ownerBatch={restored.IsOwnerBatchDispatchActive}.");
				restored.QueueFree();
				await WaitFrames(5);
				restored = null;
				savedFire.Remove("animationState");
				savedFire.Remove("spliceAnimationStates");
				legacyRestored = characterSave.InstantiateCharacterForRestore() as TowerDefensePlantPeaShooter;
				Check(GodotObject.IsInstanceValid(legacyRestored), "The legacy progress fixture must recreate the saved Peashooter.");
				if (!GodotObject.IsInstanceValid(legacyRestored))
				{
					throw new InvalidOperationException("Legacy progress character creation failed.");
				}
				levelSave.charcterDicionary[characterSave.nodeName] = legacyRestored;
				await WaitFrames(5);
				characterSave.RestoreCharacter(legacyRestored);
				ResumeRestoredEntryCharacter(legacyRestored);
				await WaitFrames(5);
				Check((legacyRestored.componentManager?.GetRuntime<FireComponent>("character.fire"))?.StateMachine?.CurrentStateHandle?.StableId == "fire.idle" && !legacyRestored.componentRunning, "An old save without component animation progress must leave the stuck attack safely and resume idle targeting.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PlantAttackProgressRestoreRuntimeTest] Unexpected exception: {value}");
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
			if (GodotObject.IsInstanceValid(legacyRestored) && !legacyRestored.IsQueuedForDeletion())
			{
				legacyRestored.QueueFree();
			}
			await WaitFrames(5);
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
			if (GodotObject.IsInstanceValid(resources))
			{
				if (hadPreviousPacket)
				{
					resources.TOWERDEFENSE_PACKETS["PlantPeaShooter"] = previousPacket;
				}
				else
				{
					resources.TOWERDEFENSE_PACKETS.Remove("PlantPeaShooter");
				}
				if (hadPreviousCharacter)
				{
					resources.TOWERDEFENSE_CHARCATERS["PlantPeaShooter"] = previousCharacter;
				}
				else
				{
					resources.TOWERDEFENSE_CHARCATERS.Remove("PlantPeaShooter");
				}
			}
		}
		bool flag2 = _failures == 0 && _checks == 13;
		GD.Print($"PLANT_ATTACK_PROGRESS_RESTORE_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private static Dictionary FindSavedFireComponent(Array<Dictionary> componentSaves)
	{
		foreach (Dictionary componentSafe in componentSaves)
		{
			if (componentSafe.GetValueOrDefault("_componentInstanceId", "").AsString() == "character.fire")
			{
				return componentSafe;
			}
		}
		return new Dictionary();
	}

	private static bool AnimationProgressMatches(Dictionary expected, Dictionary actual)
	{
		if (expected.GetValueOrDefault("clip", "").AsString() == actual.GetValueOrDefault("clip", "").AsString() && expected.GetValueOrDefault("frameIndex", -1).AsInt32() == actual.GetValueOrDefault("frameIndex", -2).AsInt32() && Math.Abs(expected.GetValueOrDefault("elapsedTimer", 0.0).AsDouble() - actual.GetValueOrDefault("elapsedTimer", 1.0).AsDouble()) < 0.0001)
		{
			return expected.GetValueOrDefault("clipOver", true).AsBool() == actual.GetValueOrDefault("clipOver", false).AsBool();
		}
		return false;
	}

	private static void ResumeRestoredEntryCharacter(TowerDefenseCharacter character)
	{
		character.ProcessMode = ProcessModeEnum.Inherit;
		IStateMachineController stateMachine = character.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			character.SetMainStateMachineDispatchEnabled(enabled: true);
		}
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
			GD.PushError("[PlantAttackProgressRestoreRuntimeTest] " + message);
		}
	}
}
