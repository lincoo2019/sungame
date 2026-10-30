using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class PlantCharacterFunctionalRuntimeProbe : Node
{
	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private readonly List<string> _failures = new List<string>();

	private readonly HashSet<string> _runtimeTypes = new HashSet<string>(StringComparer.Ordinal);

	private readonly HashSet<string> _mainStateTypes = new HashSet<string>(StringComparer.Ordinal);

	private int _definitionsChecked;

	private int _runtimesChecked;

	private int _mainStatesChecked;

	private int _animationClipsChecked;

	private int _animationTicksChecked;

	private int _runtimePhysicsDispatches;

	private int _stateProcessDispatches;

	private int _statePhysicsDispatches;

	private int _publicCallbacksChecked;

	private int _detachedCallbacksChecked;

	private int _cleanupChecks;

	public override async void _Ready()
	{
		List<string> scenePaths = CollectPlantScenePaths();
		int start = ReadNonNegativeEnvironmentInt("PLANT_FUNCTION_BATCH_START", 0);
		int num = ReadNonNegativeEnvironmentInt("PLANT_FUNCTION_BATCH_COUNT", scenePaths.Count);
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
		catch (Exception ex)
		{
			_failures.Add("probe-exception:" + ex.GetType().Name + ":" + ex.Message);
		}
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.PrintErr("PLANT_FUNCTION_FAILURE " + _failures[i]);
		}
		bool flag = start < scenePaths.Count && tested == end - start && _failures.Count == 0;
		List<string> list = new List<string>(_runtimeTypes);
		list.Sort(StringComparer.Ordinal);
		List<string> list2 = new List<string>(_mainStateTypes);
		list2.Sort(StringComparer.Ordinal);
		GD.Print($"PLANT_FUNCTION_RESULT passed={flag} sceneCount={scenePaths.Count} batchStart={start} batchEnd={end} tested={tested} definitions={_definitionsChecked} runtimes={_runtimesChecked} mainStates={_mainStatesChecked} animationClips={_animationClipsChecked} animationTicks={_animationTicksChecked} runtimePhysics={_runtimePhysicsDispatches} stateProcess={_stateProcessDispatches} statePhysics={_statePhysicsDispatches} publicCallbacks={_publicCallbacksChecked} detachedCallbacks={_detachedCallbacksChecked} cleanup={_cleanupChecks} mainStateTypes={string.Join(',', list2)} runtimeTypes={string.Join(',', list)} failures={_failures.Count}");
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
		Node instance = null;
		List<CharacterComponentRuntime> runtimes = null;
		try
		{
			instance = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (!(instance is TowerDefensePlant plant))
			{
				_failures.Add("root-type:" + scenePath + ":" + (instance?.GetType().Name ?? "null"));
				return;
			}
			plant.inGame = false;
			plant.editorPreviewMode = false;
			AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			ComponentManager componentManager = ValidateInitializedPlant(scenePath, plant);
			if (componentManager != null)
			{
				runtimes = new List<CharacterComponentRuntime>(componentManager.ResourceComponents);
				ValidateAnimation(scenePath, plant);
				ExerciseCallbacks(scenePath, plant, componentManager, runtimes);
			}
			if (plant.IsInsideTree())
			{
				RemoveChild(plant);
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (runtimes != null)
			{
				ValidateDetachedCallbacks(scenePath, plant, runtimes);
			}
			plant.Free();
			instance = null;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (runtimes != null)
			{
				ValidateReleasedRuntimes(scenePath, runtimes);
				runtimes = null;
			}
		}
		catch (Exception ex)
		{
			_failures.Add($"runtime:{scenePath}:{ex.GetType().Name}:{ex.Message}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				if (instance.IsInsideTree())
				{
					RemoveChild(instance);
				}
				instance.Free();
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (runtimes != null)
		{
			ValidateReleasedRuntimes(scenePath, runtimes);
		}
	}

	private ComponentManager ValidateInitializedPlant(string scenePath, TowerDefensePlant plant)
	{
		if (!plant.HasValidRuntimeConfiguration)
		{
			_failures.Add("invalid-runtime-config:" + scenePath);
		}
		if (!GodotObject.IsInstanceValid(plant.config))
		{
			_failures.Add("config:" + scenePath);
		}
		if (!GodotObject.IsInstanceValid(plant.sprite) || !GodotObject.IsInstanceValid(plant.sprite.flashAnimeData))
		{
			_failures.Add("animation-resource:" + scenePath);
		}
		StateHandle currentStateHandle = plant.CurrentStateHandle;
		if (plant.MainStateMachineDefinition != null)
		{
			IStateMachineController stateMachine = plant.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized && currentStateHandle != null && currentStateHandle.IsValid)
			{
				StateHandle stateById = plant.StateMachine.GetStateById(currentStateHandle.StableId);
				if (stateById != null && stateById.IsValid)
				{
					_mainStatesChecked++;
					_mainStateTypes.Add(currentStateHandle.StableId);
					goto IL_0121;
				}
			}
		}
		_failures.Add("main-state:" + scenePath + ":" + (currentStateHandle?.StableId ?? "null"));
		goto IL_0121;
		IL_0121:
		ComponentManager componentManager = plant.componentManager;
		if (componentManager?.ComponentSet == null)
		{
			_failures.Add("component-manager-or-set:" + scenePath);
			return null;
		}
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions = componentManager.ComponentSet.GetFlattenedDefinitions();
		if (flattenedDefinitions.Count == 0 || componentManager.ResourceComponents.Count != flattenedDefinitions.Count)
		{
			_failures.Add($"runtime-count:{scenePath}:definitions={flattenedDefinitions.Count}:runtimes={componentManager.ResourceComponents.Count}");
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < flattenedDefinitions.Count; i++)
		{
			CharacterComponentDefinition characterComponentDefinition = flattenedDefinitions[i];
			if (characterComponentDefinition == null || string.IsNullOrWhiteSpace(characterComponentDefinition.ComponentTypeId) || string.IsNullOrWhiteSpace(characterComponentDefinition.DefinitionId) || string.IsNullOrWhiteSpace(characterComponentDefinition.InstanceId) || !hashSet.Add(characterComponentDefinition.InstanceId))
			{
				_failures.Add($"definition-identity:{scenePath}:index={i}");
				continue;
			}
			_definitionsChecked++;
			CharacterComponentRuntime runtime = componentManager.GetRuntime<CharacterComponentRuntime>(characterComponentDefinition.InstanceId);
			if (runtime == null || runtime.Lifecycle != ComponentRuntimeLifecycle.Active || runtime.ComponentDefinition != characterComponentDefinition || runtime.Manager != componentManager || runtime.Owner != plant)
			{
				_failures.Add("runtime-binding:" + scenePath + ":" + characterComponentDefinition.InstanceId);
				continue;
			}
			if (characterComponentDefinition.StateMachineDefinition != null)
			{
				IStateMachineController stateMachine2 = runtime.StateMachine;
				if (stateMachine2 != null && stateMachine2.IsInitialized)
				{
					StateHandle currentStateHandle2 = runtime.StateMachine.CurrentStateHandle;
					if (currentStateHandle2 != null && currentStateHandle2.IsValid)
					{
						goto IL_035d;
					}
				}
				_failures.Add("component-state:" + scenePath + ":" + characterComponentDefinition.InstanceId);
			}
			goto IL_035d;
			IL_035d:
			_runtimesChecked++;
			_runtimeTypes.Add(characterComponentDefinition.ComponentTypeId);
		}
		PlantAnimeComponent runtime2 = componentManager.GetRuntime<PlantAnimeComponent>();
		WaterInteractionComponent runtime3 = componentManager.GetRuntime<WaterInteractionComponent>();
		PuzzleShaderComponent runtime4 = componentManager.GetRuntime<PuzzleShaderComponent>();
		if (runtime2 == null || plant.plantAnimeComponent != runtime2)
		{
			_failures.Add("plant-anime-facade:" + scenePath);
		}
		if (runtime3 == null || plant.waterInteractionComponent != runtime3)
		{
			_failures.Add("water-facade:" + scenePath);
		}
		if (runtime4 == null || plant.puzzleShaderComponent != runtime4)
		{
			_failures.Add("puzzle-facade:" + scenePath);
		}
		return componentManager;
	}

	private void ValidateAnimation(string scenePath, TowerDefensePlant plant)
	{
		AdobeAnimateSprite sprite = plant.sprite;
		if (!GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(sprite.flashAnimeData))
		{
			return;
		}
		ValidateConfiguredClip(scenePath, sprite, plant.idleAnimeClip, "idle");
		ValidateConfiguredClip(scenePath, sprite, plant.plantAnimeClip, "plant");
		string text = FindPlayableClip(sprite, plant);
		if (string.IsNullOrEmpty(text))
		{
			_failures.Add("no-playable-clip:" + scenePath);
			return;
		}
		sprite.SetAnimation(text);
		double num = Math.Max(1.0, sprite.frameRate);
		sprite.RunBatchedProcessUpdate(1.1 / num);
		if (!string.Equals(sprite.clip, text, StringComparison.Ordinal) || !sprite.HasClip(sprite.clip) || sprite.clipRange == Vector2I.One * -1 || sprite.frameIndex < sprite.clipRange.X || sprite.frameIndex > sprite.clipRange.Y || !double.IsFinite(sprite.elapsedTimer))
		{
			_failures.Add($"animation-playback:{scenePath}:clip={text}:active={sprite.clip}:frame={sprite.frameIndex}:range={sprite.clipRange}");
		}
		else
		{
			_animationTicksChecked++;
		}
	}

	private void ValidateConfiguredClip(string scenePath, AdobeAnimateSprite sprite, string configuredClip, string label)
	{
		if (string.IsNullOrWhiteSpace(configuredClip))
		{
			return;
		}
		string[] array = configuredClip.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length == 0)
		{
			_failures.Add("empty-" + label + "-clip:" + scenePath);
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (!sprite.HasClip(array[i]))
			{
				_failures.Add($"missing-{label}-clip:{scenePath}:{array[i]}");
			}
			else
			{
				_animationClipsChecked++;
			}
		}
	}

	private static string FindPlayableClip(AdobeAnimateSprite sprite, TowerDefensePlant plant)
	{
		string[] array = new string[2] { plant.plantAnimeClip, plant.idleAnimeClip };
		foreach (string text in array)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			string[] array2 = text.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			for (int j = 0; j < array2.Length; j++)
			{
				if (sprite.HasClip(array2[j]))
				{
					return array2[j];
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(sprite.clip) && sprite.HasClip(sprite.clip))
		{
			return sprite.clip;
		}
		if (sprite.flashAnimeData?.clips == null)
		{
			return string.Empty;
		}
		foreach (Variant key in sprite.flashAnimeData.clips.Keys)
		{
			string text2 = key.AsString();
			if (!string.IsNullOrWhiteSpace(text2) && sprite.HasClip(text2))
			{
				return text2;
			}
		}
		return string.Empty;
	}

	private void ExerciseCallbacks(string scenePath, TowerDefensePlant plant, ComponentManager manager, List<CharacterComponentRuntime> runtimes)
	{
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime.ExportComponentSave() == null || characterComponentRuntime.SyncSerialize() == null)
			{
				_failures.Add($"public-state-callback:{scenePath}:index={i}");
			}
			else
			{
				_publicCallbacksChecked += 2;
			}
		}
		PlantAnimeComponent runtime = manager.GetRuntime<PlantAnimeComponent>();
		if (runtime != null)
		{
			runtime.PlantProcessing(0f);
			runtime.PlantExited();
			runtime.PlantEntered();
			if (runtime.AnimeCompleted("__plant_probe_non_matching_clip__"))
			{
				_failures.Add("plant-anime-nonmatch:" + scenePath);
			}
			_publicCallbacksChecked += 4;
		}
		WaterInteractionComponent runtime2 = manager.GetRuntime<WaterInteractionComponent>();
		if (runtime2 != null)
		{
			runtime2.InWaterDiscardSet();
			runtime2.OutWaterDiscardSet();
			_publicCallbacksChecked += 2;
		}
		PuzzleShaderComponent runtime3 = manager.GetRuntime<PuzzleShaderComponent>();
		if (runtime3 != null)
		{
			runtime3.RefreshConfiguration();
			runtime3.IdleExited();
			_publicCallbacksChecked += 2;
		}
		manager.TickStateMachineProcess(0.0);
		_stateProcessDispatches++;
		bool canDispatchOwnerGameplay = manager.CanDispatchOwnerGameplay;
		manager.TickStateMachinePhysics(0.0, canDispatchOwnerGameplay);
		_statePhysicsDispatches++;
		manager.TickRuntimePhysics(0.0, Engine.GetPhysicsFrames(), canDispatchOwnerGameplay);
		_runtimePhysicsDispatches++;
		plant.BatchUpdate(0.0);
		_publicCallbacksChecked++;
	}

	private void ValidateReleasedRuntimes(string scenePath, List<CharacterComponentRuntime> runtimes)
	{
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime == null || characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Released || characterComponentRuntime.Owner != null || characterComponentRuntime.Manager != null || characterComponentRuntime.ComponentDefinition != null)
			{
				_failures.Add($"runtime-cleanup:{scenePath}:index={i}");
			}
			else
			{
				_cleanupChecks++;
			}
		}
	}

	private void ValidateDetachedCallbacks(string scenePath, TowerDefensePlant plant, List<CharacterComponentRuntime> runtimes)
	{
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime == null || characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Detached || characterComponentRuntime.Owner != null || characterComponentRuntime.Manager != null || characterComponentRuntime.ComponentDefinition == null)
			{
				_failures.Add($"runtime-detach:{scenePath}:index={i}");
			}
		}
		PlantAnimeComponent plantAnimeComponent = plant.plantAnimeComponent;
		if (plantAnimeComponent != null)
		{
			plantAnimeComponent.PlantProcessing(0f);
			plantAnimeComponent.PlantExited();
			plantAnimeComponent.PlantEntered();
			plantAnimeComponent.AnimeCompleted("__plant_probe_detached_clip__");
			_detachedCallbacksChecked += 4;
		}
		WaterInteractionComponent waterInteractionComponent = plant.waterInteractionComponent;
		if (waterInteractionComponent != null)
		{
			waterInteractionComponent.InWaterDiscardSet();
			waterInteractionComponent.OutWaterDiscardSet();
			_detachedCallbacksChecked += 2;
		}
		PuzzleShaderComponent puzzleShaderComponent = plant.puzzleShaderComponent;
		if (puzzleShaderComponent != null)
		{
			puzzleShaderComponent.RefreshConfiguration();
			puzzleShaderComponent.IdleExited();
			_detachedCallbacksChecked += 2;
		}
	}

	private static List<string> CollectPlantScenePaths()
	{
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(json))
		{
			throw new InvalidOperationException("character registry could not load path=res://Asset/Config/Character/CharacterResource.json");
		}
		List<string> list = new List<string>(FullGameplayResourceManifest.GetRegistryRoots(json).UniqueSceneRoots);
		List<string> list2 = new List<string>();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (text.Contains("/Plant/", StringComparison.OrdinalIgnoreCase) && !text.Contains("/Vase/", StringComparison.OrdinalIgnoreCase))
			{
				list2.Add(text);
			}
		}
		return list2;
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
