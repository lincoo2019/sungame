using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class CharacterComponentCreationPlanRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private CharacterComponentSet _performanceSet;

	private long _lookupSink;

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(RunAsync).CallDeferred();
	}

	private async void RunAsync()
	{
		try
		{
			PreparePerformanceSet();
			RunInitialBuildDiagnostic();
			RunLookupPerformanceGate();
			await RunNodeManagerIsolationAsync();
			RunInvalidationScenarios();
		}
		catch (Exception ex)
		{
			_failures++;
			GD.PushError(ex.ToString());
		}
		finally
		{
			bool flag = _failures == 0;
			GD.Print($"CHARACTER_COMPONENT_CREATION_PLAN_RESULT passed={flag} checks={_checks} failures={_failures}");
			GetTree().Quit((!flag) ? 2 : 0);
		}
	}

	private void PreparePerformanceSet()
	{
		CharacterComponentSet characterComponentSet = new CharacterComponentSet();
		characterComponentSet.Components.Add(CreateDefinition("probe.creation.effect.base", 0));
		CharacterComponentSet characterComponentSet2 = new CharacterComponentSet
		{
			ParentSet = characterComponentSet
		};
		characterComponentSet2.Components.Add(CreateDefinition("probe.creation.effect.second", 1));
		_performanceSet = characterComponentSet2;
	}

	private void RunInitialBuildDiagnostic()
	{
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		CharacterComponentCreationPlan creationPlan = _performanceSet.GetCreationPlan();
		long value = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread);
		CharacterComponentCreationPlan creationPlan2 = _performanceSet.GetCreationPlan();
		Check(creationPlan == creationPlan2 && creationPlan.Count == 2 && creationPlan.RuntimeCapacityHint == 2 && creationPlan.InstanceIdCapacityHint == 2 && creationPlan.ComponentTypeCapacityHint == 1 && creationPlan[0].WireSlotCapacityHint == 2 && creationPlan[1].WireSlotCapacityHint == 2, "Valid immutable graphs must publish one reusable capacity-aware plan.");
		GD.Print($"CHARACTER_COMPONENT_CREATION_PLAN_INITIAL_DIAGNOSTIC entries={creationPlan.Count} allocatedBytes={value} allocationContract=diagnostic_only");
	}

	private void RunLookupPerformanceGate()
	{
		OptimizationResultIdentity identity = new OptimizationResultIdentity("character-component-creation-plan-lookup", OptimizationWorkloadKind.BareFunction, "shared-component-set", "res://Test/CharacterComponentCreationPlanRuntimeTest.tscn", "CharacterComponentCreationPlan", "none", "none", "none", "none", "steady-cache-hit", OptimizationScheduleKind.BackToBack, "headless", 60, 240);
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			ExecuteLookupBatch();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		long lookupSink = _lookupSink;
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			ExecuteLookupBatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		long num = 0L;
		CharacterComponentCreationPlan creationPlan = _performanceSet.GetCreationPlan();
		for (int k = 0; k < 1000; k++)
		{
			num += creationPlan.Count;
			num += creationPlan[k & 1].WireIndex;
		}
		bool functionalPassed = _lookupSink - lookupSink == num * 1200 && result.Gen0Collections == 0 && result.Gen1Collections == 0 && result.Gen2Collections == 0;
		bool flag = OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed, 1000, 240, 1000, 1000, in identity);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, functionalPassed, flag, 1000, 240, 1000, 1000));
		Check(flag, "1000 steady CreationPlan lookups must stay below 0.2 ms P99 with zero allocation and GC.");
	}

	private void ExecuteLookupBatch()
	{
		long num = 0L;
		for (int i = 0; i < 1000; i++)
		{
			CharacterComponentCreationPlan creationPlan = _performanceSet.GetCreationPlan();
			num += creationPlan.Count;
			num += creationPlan[i & 1].WireIndex;
		}
		_lookupSink += num;
	}

	private async Task RunNodeManagerIsolationAsync()
	{
		CharacterComponentSet characterComponentSet = new CharacterComponentSet();
		CharacterComponentCreationPlanProbeDefinition item = CreateProbeDefinition(" probe.creation.effect.node ", "probe.creation.node.initial");
		characterComponentSet.Components.Add(item);
		Node node = new Node
		{
			Name = "CreationPlanNodeManagerOwners"
		};
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
		ComponentManager[] array = new ComponentManager[1000];
		CharacterComponentCreationPlanProbeRuntime[] runtimes = new CharacterComponentCreationPlanProbeRuntime[1000];
		HashSet<CharacterComponentCreationPlanProbeRuntime> hashSet = new HashSet<CharacterComponentCreationPlanProbeRuntime>(ReferenceEqualityComparer.Instance);
		bool flag = true;
		for (int i = 0; i < 1000; i++)
		{
			CharacterComponentCreationPlanProbeCharacter characterComponentCreationPlanProbeCharacter = new CharacterComponentCreationPlanProbeCharacter
			{
				Name = $"CreationPlanOwner{i}"
			};
			ComponentManager componentManager = new ComponentManager
			{
				Name = "ComponentManager",
				ComponentSet = characterComponentSet
			};
			ComponentBase componentBase = new ComponentBase
			{
				Name = "LegacyNodeComponent"
			};
			characterComponentCreationPlanProbeCharacter.componentManager = componentManager;
			componentManager.AttachOwner(characterComponentCreationPlanProbeCharacter);
			componentManager.AddChild(componentBase);
			node.AddChild(characterComponentCreationPlanProbeCharacter, forceReadableName: false, InternalMode.Disabled);
			componentManager.ApplyOrQueueNetworkState("CharacterComponentCreationPlanProbeRuntime", new Dictionary { ["value"] = i + 1000 });
			componentManager.InitializeResourceComponents();
			componentManager.ActivateResourceComponents();
			CharacterComponentCreationPlanProbeRuntime runtime = componentManager.GetRuntime<CharacterComponentCreationPlanProbeRuntime>("probe.creation.effect.node");
			array[i] = componentManager;
			runtimes[i] = runtime;
			flag &= runtime != null && hashSet.Add(runtime) && runtime.Owner == characterComponentCreationPlanProbeCharacter && runtime.Manager == componentManager && runtime.Lifecycle == ComponentRuntimeLifecycle.Active && runtime.Value == i + 1000 && runtime.SyncSerialize()["value"].AsInt32() == i + 1000 && runtime.ExportComponentSave()["value"].AsInt32() == i + 1000 && componentManager.ResourceComponents.Count == 1 && componentManager.componentList.Count == 1 && componentManager.GetComponentFromType("ComponentBase") == componentBase && componentManager.TryGetWireKey(runtime, out var wireKey) && wireKey == "CharacterComponentCreationPlanProbeRuntime";
		}
		Check(flag && hashSet.Count == 1000, "1000 real owner Nodes and ComponentManager Resources must isolate runtime and legacy ComponentBase child state.");
		runtimes[0].SetAlive(alive: false);
		array[0].ApplyOrQueueNetworkState("CharacterComponentCreationPlanProbeRuntime", new Dictionary { ["value"] = 31415 });
		Check(!runtimes[0].Alive && runtimes[1].Alive && runtimes[0].Value == 31415 && runtimes[1].Value == 1001 && runtimes[0].ExportComponentSave()["value"].AsInt32() == 31415 && runtimes[1].ExportComponentSave()["value"].AsInt32() == 1001 && runtimes[0] != runtimes[1] && array[1].GetRuntime<CharacterComponentCreationPlanProbeRuntime>("probe.creation.effect.node") == runtimes[1], "Pending/live sync and save state must stay isolated to one manager-owned runtime.");
		runtimes[0].SetAlive(alive: true);
		CharacterComponentCreationPlanProbeRuntime characterComponentCreationPlanProbeRuntime = runtimes[1];
		TowerDefenseCharacter owner = array[1].GetParent() as TowerDefenseCharacter;
		array[1].DetachOwner();
		array[1].AttachOwner(owner);
		array[1].InitializeResourceComponents();
		array[1].ActivateResourceComponents();
		Check(characterComponentCreationPlanProbeRuntime == array[1].GetRuntime<CharacterComponentCreationPlanProbeRuntime>("probe.creation.effect.node") && characterComponentCreationPlanProbeRuntime.Lifecycle == ComponentRuntimeLifecycle.Active, "Temporary tree exit/rebind with the same plan must preserve the runtime instance.");
		CharacterComponentCreationPlanProbeRuntime characterComponentCreationPlanProbeRuntime2 = runtimes[0];
		CharacterComponentCreationPlanProbeDefinition characterComponentCreationPlanProbeDefinition = CreateProbeDefinition(" probe.creation.effect.node ", "probe.creation.node.replacement");
		characterComponentSet.Components[0] = characterComponentCreationPlanProbeDefinition;
		characterComponentSet.InvalidateFlattenedDefinitions();
		array[0].InitializeResourceComponents();
		array[0].ActivateResourceComponents();
		CharacterComponentCreationPlanProbeRuntime runtime2 = array[0].GetRuntime<CharacterComponentCreationPlanProbeRuntime>("probe.creation.effect.node");
		Check(characterComponentCreationPlanProbeRuntime2.IsReleased && runtime2 != null && characterComponentCreationPlanProbeRuntime2 != runtime2 && runtime2.ComponentDefinition == characterComponentCreationPlanProbeDefinition && runtime2.Lifecycle == ComponentRuntimeLifecycle.Active, "A changed plan on the same ComponentSet must release and recreate its manager runtime.");
		runtimes[0] = runtime2;
		node.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		bool flag2 = true;
		for (int j = 0; j < runtimes.Length; j++)
		{
			flag2 &= runtimes[j]?.IsReleased ?? false;
		}
		Check(flag2, "Freeing the 1000 Node owners must release every manager-owned runtime.");
	}

	private void RunInvalidationScenarios()
	{
		CharacterComponentSet characterComponentSet = new CharacterComponentSet();
		EffectCreateComponentDefinition item = CreateDefinition("probe.creation.parent.original", 0);
		characterComponentSet.Components.Add(item);
		CharacterComponentSet obj = new CharacterComponentSet
		{
			ParentSet = characterComponentSet
		};
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions = obj.GetFlattenedDefinitions();
		CharacterComponentCreationPlan creationPlan = obj.GetCreationPlan();
		EffectCreateComponentDefinition effectCreateComponentDefinition = CreateDefinition("probe.creation.parent.replacement", 0);
		characterComponentSet.Components[0] = effectCreateComponentDefinition;
		characterComponentSet.InvalidateFlattenedDefinitions();
		CharacterComponentCreationPlan creationPlan2 = obj.GetCreationPlan();
		IReadOnlyList<CharacterComponentDefinition> flattenedDefinitions2 = obj.GetFlattenedDefinitions();
		Check(creationPlan != creationPlan2 && creationPlan2[0].Definition == effectCreateComponentDefinition && flattenedDefinitions != flattenedDefinitions2 && flattenedDefinitions2[0] == effectCreateComponentDefinition, "Parent-set edits must invalidate derived flattened data and its CreationPlan.");
		obj.Components.Add(CreateDefinition("probe.creation.child.local", 1));
		obj.InvalidateFlattenedDefinitions();
		CharacterComponentCreationPlan creationPlan3 = obj.GetCreationPlan();
		Check(creationPlan2 != creationPlan3 && creationPlan3.Count == 2, "Child-set edits must invalidate its own CreationPlan.");
		StringName stringName = new StringName($"probe.creation.registry.{Guid.NewGuid():N}");
		CharacterComponentSet characterComponentSet2 = new CharacterComponentSet();
		characterComponentSet2.BehaviorIds.Add(stringName);
		EffectCreateComponentDefinition definition = CreateDefinition("probe.creation.registry.first", 0);
		EffectCreateComponentDefinition effectCreateComponentDefinition2 = CreateDefinition("probe.creation.registry.second", 0);
		try
		{
			Check(TowerDefenseBehaviorRegistry.RegisterBehavior(stringName, definition), "Registry fixture must register its first definition.");
			CharacterComponentCreationPlan creationPlan4 = characterComponentSet2.GetCreationPlan();
			Check(TowerDefenseBehaviorRegistry.RegisterBehavior(stringName, effectCreateComponentDefinition2), "Registry fixture must replace its definition.");
			CharacterComponentCreationPlan creationPlan5 = characterComponentSet2.GetCreationPlan();
			Check(creationPlan4 != creationPlan5 && creationPlan5[0].Definition == effectCreateComponentDefinition2, "BehaviorRegistry revision changes must invalidate registry-backed plans.");
		}
		finally
		{
			TowerDefenseBehaviorRegistry.UnregisterBehavior(stringName);
		}
		CharacterComponentSet characterComponentSet3 = new CharacterComponentSet();
		characterComponentSet3.ParentSet = characterComponentSet3;
		CharacterComponentCreationPlan creationPlan6 = characterComponentSet3.GetCreationPlan();
		CharacterComponentCreationPlan creationPlan7 = characterComponentSet3.GetCreationPlan();
		Check(!creationPlan6.IsGraphValid && !creationPlan7.IsGraphValid && creationPlan6 != creationPlan7, "Cyclic component-set graphs must never enter the CreationPlan cache.");
		CharacterComponentSet obj2 = new CharacterComponentSet
		{
			Components = { (CharacterComponentDefinition)new EffectCreateComponentDefinition
			{
				ComponentTypeId = "EffectCreateComponent",
				InstanceId = " ",
				WireIndex = 0
			} }
		};
		CharacterComponentCreationPlan creationPlan8 = obj2.GetCreationPlan();
		CharacterComponentCreationPlan creationPlan9 = obj2.GetCreationPlan();
		Check(!creationPlan8.IsGraphValid && !creationPlan9.IsGraphValid && creationPlan8 != creationPlan9, "Invalid component-set graphs must never enter the CreationPlan cache.");
		CharacterComponentSet obj3 = new CharacterComponentSet
		{
			Components = 
			{
				(CharacterComponentDefinition)CreateDefinition("probe.creation.wire.auto", -1),
				(CharacterComponentDefinition)CreateDefinition("probe.creation.wire.explicit", 0)
			}
		};
		CharacterComponentCreationPlan creationPlan10 = obj3.GetCreationPlan();
		CharacterComponentCreationPlan creationPlan11 = obj3.GetCreationPlan();
		Check(!creationPlan10.IsGraphValid && !creationPlan11.IsGraphValid && creationPlan10 != creationPlan11, "Wire-slot-conflicting component graphs must never enter the CreationPlan cache.");
	}

	private static EffectCreateComponentDefinition CreateDefinition(string instanceId, int wireIndex)
	{
		return new EffectCreateComponentDefinition
		{
			ComponentTypeId = " EffectCreateComponent ",
			DefinitionId = instanceId.Trim(),
			InstanceId = instanceId,
			WireIndex = wireIndex
		};
	}

	private static CharacterComponentCreationPlanProbeDefinition CreateProbeDefinition(string instanceId, string definitionId)
	{
		return new CharacterComponentCreationPlanProbeDefinition
		{
			ComponentTypeId = " CharacterComponentCreationPlanProbeRuntime ",
			DefinitionId = definitionId,
			InstanceId = instanceId,
			WireIndex = 0
		};
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError(message);
		}
	}
}
