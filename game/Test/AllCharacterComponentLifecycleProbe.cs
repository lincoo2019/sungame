using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class AllCharacterComponentLifecycleProbe : Node
{
	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		List<string> scenePaths = CollectCharacterScenePaths();
		int start = ReadNonNegativeEnvironmentInt("CHARACTER_LIFECYCLE_BATCH_START", 0);
		int num = ReadNonNegativeEnvironmentInt("CHARACTER_LIFECYCLE_BATCH_COUNT", scenePaths.Count);
		int end = Math.Min(scenePaths.Count, start + num);
		int tested = 0;
		try
		{
			for (int index = start; index < end; index++)
			{
				await ValidateScene(scenePaths[index]);
				tested++;
			}
		}
		catch (Exception value)
		{
			_failures.Add($"probe-exception:{value}");
		}
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.PrintErr("CHARACTER_LIFECYCLE_FAILURE " + _failures[i]);
		}
		bool flag = start < scenePaths.Count && tested == end - start && _failures.Count == 0;
		GD.Print($"CHARACTER_LIFECYCLE_RESULT passed={flag} sceneCount={scenePaths.Count} batchStart={start} batchEnd={end} tested={tested} failures={_failures.Count}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task ValidateScene(string scenePath)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			_failures.Add("load:" + scenePath);
			return;
		}
		TowerDefenseCharacter character = null;
		List<CharacterComponentRuntime> runtimes = null;
		try
		{
			character = packedScene.Instantiate(PackedScene.GenEditState.Disabled) as TowerDefenseCharacter;
			if (character == null)
			{
				_failures.Add("root-type:" + scenePath);
				return;
			}
			character.inGame = false;
			character.editorPreviewMode = false;
			AddChild(character, forceReadableName: false, InternalMode.Disabled);
			await WaitForDeferredRuntimeSetup();
			ComponentManager componentManager = ResolveManager(scenePath, character);
			if (componentManager == null)
			{
				return;
			}
			runtimes = new List<CharacterComponentRuntime>(componentManager.ResourceComponents);
			ValidateActiveRuntimeState(scenePath, character, componentManager, runtimes, "initial");
			ExerciseRuntimeCallbacks(scenePath, character, componentManager, runtimes, "initial");
			RemoveChild(character);
			ValidateDetachedRuntimeState(scenePath, runtimes);
			AddChild(character, forceReadableName: false, InternalMode.Disabled);
			await WaitForDeferredRuntimeSetup();
			componentManager = ResolveManager(scenePath, character);
			if (componentManager == null)
			{
				return;
			}
			ValidateSameRuntimeInstances(scenePath, componentManager, runtimes);
			ValidateActiveRuntimeState(scenePath, character, componentManager, runtimes, "reentry");
			ExerciseRuntimeCallbacks(scenePath, character, componentManager, runtimes, "reentry");
		}
		catch (Exception ex)
		{
			_failures.Add($"runtime:{scenePath}:{ex.GetType().Name}:{ex.Message}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(character))
			{
				if (character.IsInsideTree())
				{
					RemoveChild(character);
				}
				character.Free();
			}
			if (runtimes != null)
			{
				ValidateReleasedRuntimeState(scenePath, runtimes);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task WaitForDeferredRuntimeSetup()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private ComponentManager ResolveManager(string scenePath, TowerDefenseCharacter character)
	{
		if (!character.HasValidRuntimeConfiguration)
		{
			_failures.Add("runtime-config:" + scenePath);
		}
		IStateMachineController stateMachine = character.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			StateHandle currentStateHandle = character.CurrentStateHandle;
			if (currentStateHandle != null && currentStateHandle.IsValid)
			{
				goto IL_0062;
			}
		}
		_failures.Add("main-state:" + scenePath);
		goto IL_0062;
		IL_0062:
		ComponentManager componentManager = character.componentManager;
		if (componentManager == null)
		{
			_failures.Add("component-manager:" + scenePath);
		}
		return componentManager;
	}

	private void ValidateActiveRuntimeState(string scenePath, TowerDefenseCharacter character, ComponentManager manager, List<CharacterComponentRuntime> runtimes, string phase)
	{
		if (manager.ResourceComponents.Count != runtimes.Count)
		{
			_failures.Add($"runtime-count-{phase}:{scenePath}:expected={runtimes.Count}:actual={manager.ResourceComponents.Count}");
		}
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			string value = RuntimeIdentity(characterComponentRuntime, i);
			if (characterComponentRuntime == null || characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Active || characterComponentRuntime.Manager != manager || characterComponentRuntime.Owner != character || characterComponentRuntime.ComponentDefinition == null)
			{
				_failures.Add($"runtime-active-{phase}:{scenePath}:{value}");
			}
			else
			{
				if (characterComponentRuntime.ComponentDefinition.StateMachineDefinition == null)
				{
					continue;
				}
				IStateMachineController stateMachine = characterComponentRuntime.StateMachine;
				if (stateMachine != null && stateMachine.IsInitialized)
				{
					StateHandle currentStateHandle = characterComponentRuntime.StateMachine.CurrentStateHandle;
					if (currentStateHandle != null && currentStateHandle.IsValid)
					{
						continue;
					}
				}
				_failures.Add($"runtime-state-{phase}:{scenePath}:{value}");
			}
		}
	}

	private void ExerciseRuntimeCallbacks(string scenePath, TowerDefenseCharacter character, ComponentManager manager, List<CharacterComponentRuntime> runtimes, string phase)
	{
		try
		{
			manager.TickStateMachineProcess(1.0 / 60.0);
			bool canDispatchOwnerGameplay = manager.CanDispatchOwnerGameplay;
			manager.TickStateMachinePhysics(1.0 / 60.0, canDispatchOwnerGameplay);
			manager.TickRuntimePhysics(1.0 / 60.0, Engine.GetPhysicsFrames() + 1, canDispatchOwnerGameplay);
			manager.DispatchRuntimeInput(new InputEventMouseMotion());
			character.BatchProcessUpdate(1.0 / 60.0);
			character.BatchUpdate(1.0 / 60.0);
		}
		catch (Exception ex)
		{
			_failures.Add($"callback-{phase}:{scenePath}:{ex.GetType().Name}:{ex.Message}");
			return;
		}
		try
		{
			Dictionary dictionary = character.ExportVariantSave();
			if (dictionary == null)
			{
				_failures.Add("variant-save-null-" + phase + ":" + scenePath);
			}
			else
			{
				character.ImportVariantSave(dictionary.Duplicate(deep: true));
			}
		}
		catch (Exception ex2)
		{
			_failures.Add($"variant-roundtrip-{phase}:{scenePath}:{ex2.GetType().Name}:{ex2.Message}");
		}
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime == null || characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Active)
			{
				continue;
			}
			string value = RuntimeIdentity(characterComponentRuntime, i);
			try
			{
				Dictionary dictionary2 = characterComponentRuntime.ExportComponentSave();
				Dictionary dictionary3 = characterComponentRuntime.SyncSerialize();
				if (dictionary2 == null)
				{
					_failures.Add($"save-null-{phase}:{scenePath}:{value}");
				}
				else if (dictionary2.Count > 0)
				{
					CharacterComponentDefinition componentDefinition = characterComponentRuntime.ComponentDefinition;
					if (componentDefinition == null || !characterComponentRuntime.CanImportComponentSave(componentDefinition.DefinitionId, componentDefinition.SchemaVersion))
					{
						_failures.Add($"save-incompatible-{phase}:{scenePath}:{value}");
					}
					else
					{
						characterComponentRuntime.ImportComponentSave(dictionary2.Duplicate(deep: true), null);
					}
				}
				if (dictionary3 == null)
				{
					_failures.Add($"sync-null-{phase}:{scenePath}:{value}");
				}
				else if (dictionary3.Count > 0)
				{
					characterComponentRuntime.ApplyAuthoritativeSync(dictionary3.Duplicate(deep: true));
				}
			}
			catch (Exception ex3)
			{
				_failures.Add($"serialize-{phase}:{scenePath}:{value}:{ex3.GetType().Name}:{ex3.Message}");
			}
		}
	}

	private void ValidateDetachedRuntimeState(string scenePath, List<CharacterComponentRuntime> runtimes)
	{
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime == null || characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Detached || characterComponentRuntime.Manager != null || characterComponentRuntime.Owner != null || characterComponentRuntime.IsReleased)
			{
				_failures.Add("runtime-detached:" + scenePath + ":" + RuntimeIdentity(characterComponentRuntime, i));
			}
		}
	}

	private void ValidateSameRuntimeInstances(string scenePath, ComponentManager manager, List<CharacterComponentRuntime> runtimes)
	{
		if (manager.ResourceComponents.Count != runtimes.Count)
		{
			return;
		}
		for (int i = 0; i < runtimes.Count; i++)
		{
			if (manager.ResourceComponents[i] != runtimes[i])
			{
				_failures.Add($"runtime-replaced-on-reentry:{scenePath}:index={i}");
			}
		}
	}

	private void ValidateReleasedRuntimeState(string scenePath, List<CharacterComponentRuntime> runtimes)
	{
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime == null || characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Released || characterComponentRuntime.Manager != null || characterComponentRuntime.Owner != null || characterComponentRuntime.ComponentDefinition != null)
			{
				_failures.Add("runtime-released:" + scenePath + ":" + RuntimeIdentity(characterComponentRuntime, i));
			}
		}
	}

	private static string RuntimeIdentity(CharacterComponentRuntime runtime, int index)
	{
		if (runtime?.ComponentDefinition != null)
		{
			return $"index={index}:type={runtime.ComponentDefinition.ComponentTypeId}:instance={runtime.ComponentDefinition.InstanceId}";
		}
		return $"index={index}:definition=null";
	}

	private static List<string> CollectCharacterScenePaths()
	{
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(json))
		{
			throw new InvalidOperationException("character registry could not load path=res://Asset/Config/Character/CharacterResource.json");
		}
		return new List<string>(FullGameplayResourceManifest.GetRegistryRoots(json).UniqueSceneRoots);
	}

	private static int ReadNonNegativeEnvironmentInt(string name, int fallback)
	{
		if (!int.TryParse(System.Environment.GetEnvironmentVariable(name), out var result) || result < 0)
		{
			return fallback;
		}
		return result;
	}
}
