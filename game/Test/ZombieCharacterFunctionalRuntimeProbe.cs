using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class ZombieCharacterFunctionalRuntimeProbe : Node
{
	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private readonly List<string> _failures = new List<string>();

	private bool _previousUseBatch;

	private int _statefulRuntimeCount;

	private int _entryRuntimeCount;

	private int _animationCheckCount;

	private int _physicsDispatchCount;

	public override async void _Ready()
	{
		_previousUseBatch = TowerDefenseZombie.UseBatch;
		TowerDefenseZombie.UseBatch = false;
		List<string> scenePaths = CollectZombieScenePaths();
		int start = ReadNonNegativeEnvironmentInt("ZOMBIE_SCENE_BATCH_START", 0);
		int num = ReadNonNegativeEnvironmentInt("ZOMBIE_SCENE_BATCH_COUNT", scenePaths.Count);
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
		finally
		{
			TowerDefenseZombie.UseBatch = _previousUseBatch;
		}
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.PrintErr("ZOMBIE_FUNCTION_FAILURE " + _failures[i]);
		}
		bool flag = start < scenePaths.Count && tested == end - start && _failures.Count == 0;
		GD.Print($"ZOMBIE_FUNCTION_RESULT passed={flag} sceneCount={scenePaths.Count} batchStart={start} batchEnd={end} tested={tested} stateful={_statefulRuntimeCount} entry={_entryRuntimeCount} animations={_animationCheckCount} physics={_physicsDispatchCount} failures={_failures.Count}");
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
		TowerDefenseZombie zombie = null;
		List<CharacterComponentRuntime> runtimes = new List<CharacterComponentRuntime>();
		bool addedToTree = false;
		try
		{
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (!(node is TowerDefenseZombie towerDefenseZombie))
			{
				_failures.Add("root-type:" + scenePath + ":" + (node?.GetType().Name ?? "null"));
				node?.Free();
				return;
			}
			zombie = towerDefenseZombie;
			zombie.inGame = false;
			zombie.editorPreviewMode = false;
			zombie.GlobalPosition = new Vector2(1000f, 100f);
			AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			addedToTree = true;
			await WaitProcessAndPhysicsFrames(3);
			ValidateCharacterCore(scenePath, zombie, runtimes);
			await ValidateMainStateAndAnimation(scenePath, zombie);
			ValidateComponentCallbacks(scenePath, zombie, runtimes);
			RemoveChild(zombie);
			addedToTree = false;
			ValidateDetached(scenePath, runtimes);
			zombie.Free();
			zombie = null;
			ValidateReleased(scenePath, runtimes);
		}
		catch (Exception ex)
		{
			_failures.Add($"runtime-exception:{scenePath}:{ex.GetType().Name}:{ex.Message}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				if (addedToTree && zombie.IsInsideTree())
				{
					RemoveChild(zombie);
				}
				zombie.Free();
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void ValidateCharacterCore(string scenePath, TowerDefenseZombie zombie, List<CharacterComponentRuntime> runtimes)
	{
		if (!zombie.HasValidRuntimeConfiguration)
		{
			_failures.Add("runtime-config:" + scenePath);
		}
		IStateMachineController stateMachine = zombie.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			_failures.Add("main-state:" + scenePath + ":uninitialized");
		}
		string[] array = new string[4] { "character.root", "zombie.walk", "zombie.attack", "zombie.die" };
		foreach (string text in array)
		{
			StateHandle stateById = zombie.GetStateById(text);
			if (stateById == null || !stateById.IsValid)
			{
				_failures.Add("base-state:" + scenePath + ":" + text);
			}
		}
		ComponentManager componentManager = zombie.componentManager;
		if (componentManager == null)
		{
			_failures.Add("component-manager:" + scenePath);
			return;
		}
		CharacterComponentSet componentSet = componentManager.ComponentSet;
		if (componentSet == null)
		{
			_failures.Add("component-set:" + scenePath);
			return;
		}
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions = componentSet.GetFlattenedDefinitions();
		for (int j = 0; j < componentManager.ResourceComponents.Count; j++)
		{
			runtimes.Add(componentManager.ResourceComponents[j]);
		}
		if (flattenedDefinitions.Count != runtimes.Count)
		{
			_failures.Add($"runtime-count:{scenePath}:definitions={flattenedDefinitions.Count}:runtimes={runtimes.Count}");
		}
		Dictionary<string, CharacterComponentRuntime> dictionary = new Dictionary<string, CharacterComponentRuntime>(StringComparer.Ordinal);
		for (int k = 0; k < runtimes.Count; k++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[k];
			if (characterComponentRuntime == null || characterComponentRuntime.ComponentDefinition == null)
			{
				_failures.Add($"runtime-binding:{scenePath}:index={k}:null");
				continue;
			}
			string instanceId = characterComponentRuntime.ComponentDefinition.InstanceId;
			if (string.IsNullOrWhiteSpace(instanceId) || !dictionary.TryAdd(instanceId, characterComponentRuntime))
			{
				_failures.Add("runtime-binding:" + scenePath + ":duplicate-or-empty:" + instanceId);
			}
			if (characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Active || characterComponentRuntime.Manager != componentManager || characterComponentRuntime.Owner != zombie)
			{
				_failures.Add($"runtime-binding:{scenePath}:{instanceId}:lifecycle={characterComponentRuntime.Lifecycle}");
			}
			if (characterComponentRuntime.ComponentDefinition.StateMachineDefinition == null)
			{
				continue;
			}
			_statefulRuntimeCount++;
			IStateMachineController stateMachine2 = characterComponentRuntime.StateMachine;
			if (stateMachine2 != null && stateMachine2.IsInitialized)
			{
				StateHandle currentStateHandle = characterComponentRuntime.StateMachine.CurrentStateHandle;
				if (currentStateHandle != null && currentStateHandle.IsValid)
				{
					continue;
				}
			}
			_failures.Add("stateful-component:" + scenePath + ":" + instanceId);
		}
		for (int l = 0; l < flattenedDefinitions.Count; l++)
		{
			CharacterComponentDefinition characterComponentDefinition = flattenedDefinitions[l];
			if (characterComponentDefinition == null || string.IsNullOrWhiteSpace(characterComponentDefinition.InstanceId) || !dictionary.TryGetValue(characterComponentDefinition.InstanceId, out var value) || value.ComponentDefinition != characterComponentDefinition)
			{
				_failures.Add($"definition-runtime:{scenePath}:index={l}:{characterComponentDefinition?.InstanceId ?? "null"}");
			}
		}
		ValidateRequiredComponents(scenePath, zombie, dictionary);
	}

	private void ValidateRequiredComponents(string scenePath, TowerDefenseZombie zombie, Dictionary<string, CharacterComponentRuntime> runtimeByInstanceId)
	{
		if (runtimeByInstanceId.TryGetValue("character.ground_move", out var value))
		{
			if (!(value is GroundMoveComponent groundMoveComponent) || zombie.groundMoveComponent != groundMoveComponent || groundMoveComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !groundMoveComponent.HasMovementSource)
			{
				_failures.Add("ground-move:" + scenePath);
			}
		}
		else if (zombie.groundMoveComponent != null)
		{
			_failures.Add("ground-move:" + scenePath + ":stale-facade");
		}
		if (!runtimeByInstanceId.TryGetValue("character.attack.0", out var value2) || !(value2 is AttackComponent attackComponent) || zombie.attackComponent != attackComponent || attackComponent.Lifecycle != ComponentRuntimeLifecycle.Active)
		{
			_failures.Add("attack:" + scenePath);
		}
		else if (zombie is TowerDefenseZombieGargantuarBase && (!string.Equals(attackComponent.attackType, "Smash", StringComparison.Ordinal) || !attackComponent.useParentHitBox || attackComponent.checkGrid || !attackComponent.checkLine || !attackComponent.checkVase || !zombie.HasHitBox))
		{
			_failures.Add($"gargantuar-attack:{scenePath}:type={attackComponent.attackType}:parentHitBox={attackComponent.useParentHitBox}:grid={attackComponent.checkGrid}:line={attackComponent.checkLine}:vase={attackComponent.checkVase}:ownerHitBox={zombie.HasHitBox}");
		}
		foreach (KeyValuePair<string, CharacterComponentRuntime> item in runtimeByInstanceId)
		{
			if (item.Value is AttackComponent attackComponent2 && string.Equals(attackComponent2.attackType, "Smash", StringComparison.Ordinal) && attackComponent2.CheckAreaShapeCount <= 0 && (!attackComponent2.useParentHitBox || !zombie.HasHitBox))
			{
				_failures.Add($"smash-geometry:{scenePath}:instance={item.Key}:parentHitBox={attackComponent2.useParentHitBox}:ownerHitBox={zombie.HasHitBox}:shapes={attackComponent2.CheckAreaShapeCount}");
			}
		}
		if (!runtimeByInstanceId.TryGetValue("character.zombie_death", out var value3) || !(value3 is ZombieDeathComponent zombieDeathComponent) || zombie.zombieDeathComponent != zombieDeathComponent || zombieDeathComponent.Lifecycle != ComponentRuntimeLifecycle.Active)
		{
			_failures.Add("death:" + scenePath);
		}
		if (runtimeByInstanceId.TryGetValue("character.entry_animation", out var value4))
		{
			_entryRuntimeCount++;
			if (!(value4 is EntryAnimationComponent) || value4.Lifecycle != ComponentRuntimeLifecycle.Active)
			{
				_failures.Add("entry:" + scenePath);
			}
		}
	}

	private async Task ValidateMainStateAndAnimation(string scenePath, TowerDefenseZombie zombie)
	{
		IStateMachineController stateMachine = zombie.StateMachine;
		if (stateMachine == null || stateMachine.CurrentStateHandle?.IsValid != true)
		{
			_failures.Add("main-state:" + scenePath + ":no-current-state");
		}
		GroundMoveComponent groundMoveComponent = zombie.groundMoveComponent;
		bool expectsGroundMovement = groundMoveComponent != null && !groundMoveComponent.IsReleased;
		if (expectsGroundMovement && (!zombie.SendStateEvent("ToWalk") || !string.Equals(zombie.CurrentStateHandle?.StableId, "zombie.walk", StringComparison.Ordinal)))
		{
			_failures.Add("main-state:" + scenePath + ":walk-transition:" + (zombie.CurrentStateHandle?.StableId ?? "null"));
		}
		await WaitProcessAndPhysicsFrames(2);
		AdobeAnimateSprite sprite = zombie.sprite;
		if (!GodotObject.IsInstanceValid(sprite))
		{
			_failures.Add("animation:" + scenePath + ":missing-sprite");
			return;
		}
		if (expectsGroundMovement)
		{
			string[] array = (zombie.walkAnimeClip ?? string.Empty).Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			for (int i = 0; i < array.Length; i++)
			{
				_animationCheckCount++;
				if (!sprite.HasClip(array[i]))
				{
					_failures.Add("animation:" + scenePath + ":missing-walk-clip:" + array[i]);
				}
			}
		}
		_animationCheckCount++;
		if (string.IsNullOrWhiteSpace(sprite.clip) || !sprite.HasClip(sprite.clip))
		{
			_failures.Add("animation:" + scenePath + ":active-clip:" + (sprite.clip ?? "null"));
		}
	}

	private void ValidateComponentCallbacks(string scenePath, TowerDefenseZombie zombie, List<CharacterComponentRuntime> runtimes)
	{
		ComponentManager componentManager = zombie.componentManager;
		if (componentManager == null)
		{
			return;
		}
		try
		{
			for (int i = 0; i < runtimes.Count; i++)
			{
				CharacterComponentRuntime characterComponentRuntime = runtimes[i];
				if (characterComponentRuntime != null)
				{
					if (characterComponentRuntime.ExportComponentSave() == null)
					{
						_failures.Add("component-save:" + scenePath + ":" + characterComponentRuntime.ComponentDefinition?.InstanceId);
					}
					if (characterComponentRuntime.SyncSerialize() == null)
					{
						_failures.Add("component-sync:" + scenePath + ":" + characterComponentRuntime.ComponentDefinition?.InstanceId);
					}
				}
			}
			zombie.attackComponent?.CanAttack();
			bool canDispatchOwnerGameplay = componentManager.CanDispatchOwnerGameplay;
			componentManager.TickStateMachinePhysics(0.0, canDispatchOwnerGameplay);
			componentManager.TickRuntimePhysics(0.0, Engine.GetPhysicsFrames(), canDispatchOwnerGameplay);
			_physicsDispatchCount++;
		}
		catch (Exception ex)
		{
			_failures.Add($"component-callback:{scenePath}:{ex.GetType().Name}:{ex.Message}");
		}
	}

	private void ValidateDetached(string scenePath, List<CharacterComponentRuntime> runtimes)
	{
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime != null && (characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Detached || characterComponentRuntime.Manager != null || characterComponentRuntime.Owner != null))
			{
				_failures.Add($"detach:{scenePath}:{characterComponentRuntime.ComponentDefinition?.InstanceId}:lifecycle={characterComponentRuntime.Lifecycle}");
			}
		}
	}

	private void ValidateReleased(string scenePath, List<CharacterComponentRuntime> runtimes)
	{
		for (int i = 0; i < runtimes.Count; i++)
		{
			CharacterComponentRuntime characterComponentRuntime = runtimes[i];
			if (characterComponentRuntime != null && (characterComponentRuntime.Lifecycle != ComponentRuntimeLifecycle.Released || characterComponentRuntime.Manager != null || characterComponentRuntime.Owner != null || characterComponentRuntime.ComponentDefinition != null))
			{
				_failures.Add($"release:{scenePath}:index={i}:lifecycle={characterComponentRuntime.Lifecycle}");
			}
		}
	}

	private async Task WaitProcessAndPhysicsFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static List<string> CollectZombieScenePaths()
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
			if (text.Contains("/Zombie/", StringComparison.OrdinalIgnoreCase) && !text.Contains("/Vase/", StringComparison.OrdinalIgnoreCase))
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
