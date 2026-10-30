using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using Godot;

public partial class OwnerGameplayActivationSparseDispatchRuntimeTest : Node
{
	private const string NormalComponentSetPath = "res://Prefab/TowerDefense/Character/ComponentSets/TowerDefenseZombieComponentSet.tres";

	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private readonly OwnerGameplayActivationBareCharacter[] _owners = new OwnerGameplayActivationBareCharacter[1000];

	private readonly ShadowComponent[] _shadows = new ShadowComponent[1000];

	private bool _baselineMode;

	private long _dispatchSink;

	private long _legacyDispatchSink;

	private OptimizationBatchResult _legacyRoutingResult;

	private int _checks;

	private int _failures;

	private bool _productionManagersValid;

	private bool _routingFixtureValid;

	private bool _legacyRoutingValid;

	private bool _routingImproved;

	private bool _productionCallbackPassed;

	private bool _reflectionContractPassed;

	private bool _sparseOrderPassed;

	private bool _temporaryReentryPassed;

	private bool _dynamicLifecyclePassed;

	private bool _reentrantAppendPassed;

	private bool _modCompatibilityPassed;

	private bool _rebuildPassed;

	private bool _cleanupPassed;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		_baselineMode = string.Equals(System.Environment.GetEnvironmentVariable("PVZHE_OWNER_ACTIVATION_BASELINE"), "1", StringComparison.Ordinal);
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		try
		{
			PrepareNormalManagers();
			if (!_baselineMode)
			{
				RunReflectionCapabilityContract();
				ValidateProductionSparseOrder();
			}
			RunProductionCallbackDiagnostic();
			DisableProductionCallbackWorkForRoutingGate();
			RunLegacyFullScanRoutingBaseline();
			RunBarePerformanceGate();
			RunProductionActivationFunctionalContract();
			RunDynamicOrderAndLifecycleContract();
		}
		catch (Exception ex)
		{
			_failures++;
			GD.PushError(ex.ToString());
		}
		finally
		{
			bool flag = _failures == 0;
			GD.Print($"OWNER_GAMEPLAY_ACTIVATION_SPARSE_RESULT passed={flag} baselineMode={_baselineMode} checks={_checks} failures={_failures} productionManagersValid={_productionManagersValid} routingFixtureValid={_routingFixtureValid} legacyRoutingValid={_legacyRoutingValid} routingImproved={_routingImproved} productionCallback={_productionCallbackPassed} reflectionContract={_reflectionContractPassed} sparseOrder={_sparseOrderPassed} temporaryReentry={_temporaryReentryPassed} dynamicLifecycle={_dynamicLifecyclePassed} reentrantAppend={_reentrantAppendPassed} modCompatibility={_modCompatibilityPassed} rebuild={_rebuildPassed} cleanup={_cleanupPassed}");
			GetTree().Quit((!flag) ? 2 : 0);
		}
	}

	private void PrepareNormalManagers()
	{
		CharacterComponentSet characterComponentSet = GD.Load<CharacterComponentSet>("res://Prefab/TowerDefense/Character/ComponentSets/TowerDefenseZombieComponentSet.tres");
		Check(characterComponentSet != null, "Normal production CharacterComponentSet must load.");
		if (characterComponentSet != null)
		{
			int count = characterComponentSet.GetCreationPlan().Count;
			Node node = new Node
			{
				Name = "NormalOwnerGameplayActivationManagers"
			};
			AddChild(node, forceReadableName: false, InternalMode.Disabled);
			bool flag = count > 0;
			for (int i = 0; i < 1000; i++)
			{
				OwnerGameplayActivationBareCharacter ownerGameplayActivationBareCharacter = new OwnerGameplayActivationBareCharacter
				{
					Name = $"NormalOwner{i}"
				};
				Node2D node2D = new Node2D
				{
					Name = "SpriteGroup"
				};
				TowerDefenseCharacterTransformPoint towerDefenseCharacterTransformPoint = new TowerDefenseCharacterTransformPoint
				{
					Name = "TransformPoint"
				};
				Sprite2D sprite2D = new Sprite2D
				{
					Name = "ShadowSprite"
				};
				ComponentManager componentManager = new ComponentManager
				{
					Name = "ComponentManager",
					ComponentSet = characterComponentSet
				};
				node2D.AddChild(towerDefenseCharacterTransformPoint, forceReadableName: false, InternalMode.Disabled);
				ownerGameplayActivationBareCharacter.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
				ownerGameplayActivationBareCharacter.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				ownerGameplayActivationBareCharacter.spriteGroup = node2D;
				ownerGameplayActivationBareCharacter.transformPoint = towerDefenseCharacterTransformPoint;
				ownerGameplayActivationBareCharacter.shadowSprite = sprite2D;
				ownerGameplayActivationBareCharacter.componentManager = componentManager;
				componentManager.AttachOwner(ownerGameplayActivationBareCharacter);
				node.AddChild(ownerGameplayActivationBareCharacter, forceReadableName: false, InternalMode.Disabled);
				componentManager.InitializeResourceComponents();
				componentManager.ActivateResourceComponents();
				ShadowComponent runtime = componentManager.GetRuntime<ShadowComponent>("character.shadow");
				flag &= componentManager.ResourceComponents.Count == count && runtime != null && runtime.Manager == componentManager && runtime.Owner == ownerGameplayActivationBareCharacter && runtime.Lifecycle == ComponentRuntimeLifecycle.Active;
				_managers[i] = componentManager;
				_owners[i] = ownerGameplayActivationBareCharacter;
				_shadows[i] = runtime;
			}
			_productionManagersValid = flag;
			Check(flag, "Exactly 1000 ComponentManager Resources must own isolated active runtimes from the Normal production set.");
		}
	}

	private void RunProductionCallbackDiagnostic()
	{
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			DispatchProductionBatch();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			DispatchProductionBatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		OptimizationBatchResult optimizationBatchResult = optimizationBatchSampler.Complete();
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		GD.Print("OWNER_GAMEPLAY_ACTIVATION_CALLBACK_DIAGNOSTIC " + $"instances={1000} warmupSamples={240} " + $"samples={optimizationBatchResult.SampleCount} " + "batchMeanMs=" + optimizationBatchResult.MeanMilliseconds.ToString("F6", invariantCulture) + " batchP50Ms=" + optimizationBatchResult.P50Milliseconds.ToString("F6", invariantCulture) + " batchP95Ms=" + optimizationBatchResult.P95Milliseconds.ToString("F6", invariantCulture) + " batchP99Ms=" + optimizationBatchResult.P99Milliseconds.ToString("F6", invariantCulture) + " batchMaxMs=" + optimizationBatchResult.MaximumMilliseconds.ToString("F6", invariantCulture) + " " + $"overBudgetSamples={optimizationBatchResult.OverBudgetSamples} " + $"maxSpikeSample={optimizationBatchResult.MaximumSampleIndex + 1} " + $"allocatedBytes={optimizationBatchResult.AllocatedBytes} " + $"gen0={optimizationBatchResult.Gen0Collections} " + $"gen1={optimizationBatchResult.Gen1Collections} " + $"gen2={optimizationBatchResult.Gen2Collections} accepted=False " + "followup=shadow-callback-optimization contract=diagnostic_only");
	}

	private void DisableProductionCallbackWorkForRoutingGate()
	{
		bool flag = true;
		for (int i = 0; i < 1000; i++)
		{
			ShadowComponent shadowComponent = _shadows[i];
			shadowComponent?.SetAlive(alive: false);
			flag &= shadowComponent != null && !shadowComponent.Alive && shadowComponent.Lifecycle == ComponentRuntimeLifecycle.Active;
		}
		_routingFixtureValid = flag;
		Check(flag, "The routing gate must retain 1000 active Normal runtimes while suppressing callback-body work.");
	}

	private void RunLegacyFullScanRoutingBaseline()
	{
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			DispatchLegacyFullScanBatch();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		long legacyDispatchSink = _legacyDispatchSink;
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			DispatchLegacyFullScanBatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		_legacyRoutingResult = optimizationBatchSampler.Complete();
		_legacyRoutingValid = _legacyDispatchSink - legacyDispatchSink == 1200000 && _legacyRoutingResult.AllocatedBytes == 0L && _legacyRoutingResult.Gen0Collections == 0 && _legacyRoutingResult.Gen1Collections == 0 && _legacyRoutingResult.Gen2Collections == 0;
		Check(_legacyRoutingValid, "The same-condition legacy full scan must dispatch exactly 1000 Normal managers with zero steady allocation and GC.");
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		GD.Print("OWNER_GAMEPLAY_ACTIVATION_LEGACY_ROUTE_BASELINE " + $"instances={1000} activeInstances={1000} " + $"dispatchedInstances={1000} " + $"warmupSamples={240} " + $"samples={_legacyRoutingResult.SampleCount} " + "batchMeanMs=" + _legacyRoutingResult.MeanMilliseconds.ToString("F6", invariantCulture) + " batchP50Ms=" + _legacyRoutingResult.P50Milliseconds.ToString("F6", invariantCulture) + " batchP95Ms=" + _legacyRoutingResult.P95Milliseconds.ToString("F6", invariantCulture) + " batchP99Ms=" + _legacyRoutingResult.P99Milliseconds.ToString("F6", invariantCulture) + " batchMaxMs=" + _legacyRoutingResult.MaximumMilliseconds.ToString("F6", invariantCulture) + " " + $"overBudgetSamples={_legacyRoutingResult.OverBudgetSamples} " + $"maxSpikeSample={_legacyRoutingResult.MaximumSampleIndex + 1} " + $"allocatedBytes={_legacyRoutingResult.AllocatedBytes} " + $"gen0={_legacyRoutingResult.Gen0Collections} " + $"gen1={_legacyRoutingResult.Gen1Collections} " + $"gen2={_legacyRoutingResult.Gen2Collections} " + $"functionalPassed={_legacyRoutingValid} " + "accepted=False contract=baseline_only");
	}

	private void RunReflectionCapabilityContract()
	{
		Type typeFromHandle = typeof(CharacterComponentRuntime);
		MethodInfo? method = typeFromHandle.GetMethod("OnOwnerGameplayActivated", BindingFlags.Instance | BindingFlags.NonPublic);
		PropertyInfo property = typeFromHandle.GetProperty("HasOwnerGameplayActivationWork", BindingFlags.Instance | BindingFlags.NonPublic);
		bool flag = method != null && property != null && property.PropertyType == typeof(bool);
		if (flag)
		{
			Type[] types = typeFromHandle.Assembly.GetTypes();
			foreach (Type type in types)
			{
				if (type.IsAbstract || !typeFromHandle.IsAssignableFrom(type) || type == typeof(OwnerGameplayActivationModCompatibilityProbeRuntime))
				{
					continue;
				}
				MethodInfo method2 = type.GetMethod("OnOwnerGameplayActivated", BindingFlags.Instance | BindingFlags.NonPublic);
				PropertyInfo property2 = type.GetProperty("HasOwnerGameplayActivationWork", BindingFlags.Instance | BindingFlags.NonPublic);
				if (method2 == null || property2 == null)
				{
					flag = false;
					continue;
				}
				bool flag2 = method2.DeclaringType != typeFromHandle;
				bool flag3 = false;
				try
				{
					object obj = Activator.CreateInstance(type);
					object value = property2.GetValue(obj);
					flag3 = value is bool && (bool)value;
				}
				catch
				{
					flag = false;
					continue;
				}
				if (flag2 != flag3)
				{
					flag = false;
				}
			}
		}
		_reflectionContractPassed = flag;
		Check(flag, "Reflection contract must reject every gameplay-activation callback override that forgets its explicit capability flag, and every useless true flag.");
	}

	private void ValidateProductionSparseOrder()
	{
		FieldInfo field = typeof(ComponentManager).GetField("_ownerGameplayActivationRuntimes", BindingFlags.Instance | BindingFlags.NonPublic);
		FieldInfo field2 = typeof(ComponentManager).GetField("_singleOwnerGameplayActivationRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
		PropertyInfo property = typeof(CharacterComponentRuntime).GetProperty("HasOwnerGameplayActivationWork", BindingFlags.Instance | BindingFlags.NonPublic);
		bool flag = field != null && field2 != null && property != null;
		int num = 0;
		while (flag && num < 1000)
		{
			ComponentManager componentManager = _managers[num];
			IList list = field.GetValue(componentManager) as IList;
			CharacterComponentRuntime characterComponentRuntime = field2.GetValue(componentManager) as CharacterComponentRuntime;
			if (list == null)
			{
				flag = false;
				break;
			}
			int num2 = 0;
			for (int i = 0; i < componentManager.ResourceComponents.Count; i++)
			{
				CharacterComponentRuntime characterComponentRuntime2 = componentManager.ResourceComponents[i];
				object value = property.GetValue(characterComponentRuntime2);
				if (value is bool && (bool)value)
				{
					flag &= num2 < list.Count && list[num2] == characterComponentRuntime2;
					num2++;
				}
			}
			bool flag2 = flag;
			flag = (byte)((flag2 ? 1u : 0u) & (uint)(num2 switch
			{
				0 => (characterComponentRuntime == null && list.Count == 0) ? 1 : 0, 
				1 => (list.Count == 1 && characterComponentRuntime == list[0]) ? 1 : 0, 
				_ => (characterComponentRuntime == null && list.Count == num2) ? 1 : 0, 
			})) != 0;
			num++;
		}
		_sparseOrderPassed = flag;
		Check(flag, "All 1000 Normal managers must use the singleton slot for one callback and preserve production resource order for multiple callbacks.");
	}

	private void RunBarePerformanceGate()
	{
		OptimizationResultIdentity identity = new OptimizationResultIdentity("manager_owner_activation_routing", OptimizationWorkloadKind.BareComponent, "builtin.zombie.normal", "res://Prefab/TowerDefense/Character/ComponentSets/TowerDefenseZombieComponentSet.tres", "CharacterComponentRuntime", "normal-production-component-set", "normal-owner-manager", "none", "none", "owner-gameplay-activated", OptimizationScheduleKind.BackToBack, "headless-mobile", 60, 240);
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			DispatchProductionBatch();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		long dispatchSink = _dispatchSink;
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			DispatchProductionBatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		bool flag = _productionManagersValid && _routingFixtureValid && _legacyRoutingValid && _dispatchSink - dispatchSink == 1200000;
		_routingImproved = result.P99Milliseconds < _legacyRoutingResult.P99Milliseconds;
		flag &= _routingImproved;
		bool flag2 = OptimizationPerformanceGate.IsBareResultPassed(in result, flag, 1000, 240, 1000, 1000, in identity);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, flag, flag2, 1000, 240, 1000, 1000));
		if (!_baselineMode)
		{
			Check(flag2, "1000 Normal ComponentManager owner-activation dispatches must remain below 0.2 ms P99 with zero steady allocation and GC.");
		}
	}

	private void RunProductionActivationFunctionalContract()
	{
		bool flag = true;
		for (int i = 0; i < 1000; i++)
		{
			OwnerGameplayActivationBareCharacter ownerGameplayActivationBareCharacter = _owners[i];
			ShadowComponent obj = _shadows[i];
			Vector2 position = new Vector2((float)i + 1f, (float)(-i) - 1f);
			ownerGameplayActivationBareCharacter.shadowSprite.Position = position;
			obj.SetAlive(alive: true);
		}
		for (int j = 0; j < 1000; j++)
		{
			_managers[j].NotifyOwnerGameplayActivated();
		}
		for (int k = 0; k < 1000; k++)
		{
			OwnerGameplayActivationBareCharacter ownerGameplayActivationBareCharacter2 = _owners[k];
			ShadowComponent shadowComponent = _shadows[k];
			flag &= shadowComponent.Alive && shadowComponent.Lifecycle == ComponentRuntimeLifecycle.Active && shadowComponent.saveShadowPosition.IsEqualApprox(ownerGameplayActivationBareCharacter2.shadowSprite.GlobalPosition) && shadowComponent.saveTransformPointScale.IsEqualApprox(ownerGameplayActivationBareCharacter2.transformPoint.Scale);
		}
		_productionCallbackPassed = flag;
		Check(flag, "All 1000 real Normal Shadow callbacks must recapture their changed owner pose outside the isolated routing timer.");
	}

	private void DispatchProductionBatch()
	{
		for (int i = 0; i < 1000; i++)
		{
			_managers[i].NotifyOwnerGameplayActivated();
		}
		_dispatchSink += 1000L;
	}

	private void DispatchLegacyFullScanBatch()
	{
		for (int i = 0; i < 1000; i++)
		{
			_managers[i].NotifyOwnerGameplayActivatedLegacyFullScanForTests();
		}
		_legacyDispatchSink += 1000L;
	}

	private void RunDynamicOrderAndLifecycleContract()
	{
		OwnerGameplayActivationBareCharacter ownerGameplayActivationBareCharacter = new OwnerGameplayActivationBareCharacter
		{
			Name = "DynamicOwnerGameplayActivationOwner"
		};
		ComponentManager componentManager = (ownerGameplayActivationBareCharacter.componentManager = new ComponentManager
		{
			Name = "ComponentManager",
			ComponentSet = new CharacterComponentSet()
		});
		componentManager.AttachOwner(ownerGameplayActivationBareCharacter);
		AddChild(ownerGameplayActivationBareCharacter, forceReadableName: false, InternalMode.Disabled);
		componentManager.InitializeResourceComponents();
		componentManager.ActivateResourceComponents();
		OwnerGameplayActivationProbeDefinition definition = CreateProbeDefinition("probe.owner-activation.first", 1, 0);
		OwnerGameplayActivationProbeDefinition definition2 = CreateProbeDefinition("probe.owner-activation.second", 2, 1);
		OwnerGameplayActivationProbeDefinition definition3 = CreateProbeDefinition("probe.owner-activation.third", 3, 2);
		OwnerGameplayActivationProbeRuntime ownerGameplayActivationProbeRuntime = componentManager.AddRuntimeComponent(definition) as OwnerGameplayActivationProbeRuntime;
		OwnerGameplayActivationProbeRuntime ownerGameplayActivationProbeRuntime2 = componentManager.AddRuntimeComponent(definition2) as OwnerGameplayActivationProbeRuntime;
		OwnerGameplayActivationProbeRuntime.ResetSequence();
		componentManager.NotifyOwnerGameplayActivated();
		bool flag = ownerGameplayActivationProbeRuntime != null && ownerGameplayActivationProbeRuntime2 != null && ownerGameplayActivationProbeRuntime.CallCount == 1 && ownerGameplayActivationProbeRuntime2.CallCount == 1 && ownerGameplayActivationProbeRuntime.LastSequence == 1 && ownerGameplayActivationProbeRuntime2.LastSequence == 2;
		int num = ownerGameplayActivationProbeRuntime?.CallCount ?? (-1);
		int num2 = ownerGameplayActivationProbeRuntime2?.CallCount ?? (-1);
		componentManager.DetachOwner();
		componentManager.NotifyOwnerGameplayActivated();
		bool flag2 = ownerGameplayActivationProbeRuntime != null && ownerGameplayActivationProbeRuntime2 != null && ownerGameplayActivationProbeRuntime.Lifecycle == ComponentRuntimeLifecycle.Detached && ownerGameplayActivationProbeRuntime2.Lifecycle == ComponentRuntimeLifecycle.Detached && ownerGameplayActivationProbeRuntime.CallCount == num && ownerGameplayActivationProbeRuntime2.CallCount == num2;
		componentManager.AttachOwner(ownerGameplayActivationBareCharacter);
		componentManager.ActivateResourceComponents();
		OwnerGameplayActivationProbeRuntime.ResetSequence();
		componentManager.NotifyOwnerGameplayActivated();
		_temporaryReentryPassed = flag2 && ownerGameplayActivationProbeRuntime.Lifecycle == ComponentRuntimeLifecycle.Active && ownerGameplayActivationProbeRuntime2.Lifecycle == ComponentRuntimeLifecycle.Active && ownerGameplayActivationProbeRuntime.CallCount == num + 1 && ownerGameplayActivationProbeRuntime2.CallCount == num2 + 1 && ownerGameplayActivationProbeRuntime.LastSequence == 1 && ownerGameplayActivationProbeRuntime2.LastSequence == 2;
		Check(_temporaryReentryPassed, "Temporary manager tree exit must suppress detached callbacks and preserve sparse order after rebind.");
		int num3 = ownerGameplayActivationProbeRuntime?.CallCount ?? (-1);
		int num4 = ownerGameplayActivationProbeRuntime2?.CallCount ?? (-1);
		ownerGameplayActivationProbeRuntime?.SetAlive(alive: false);
		OwnerGameplayActivationProbeRuntime.ResetSequence();
		componentManager.NotifyOwnerGameplayActivated();
		bool flag3 = ownerGameplayActivationProbeRuntime != null && ownerGameplayActivationProbeRuntime2 != null && ownerGameplayActivationProbeRuntime.CallCount == num3 && ownerGameplayActivationProbeRuntime2.CallCount == num4 + 1 && ownerGameplayActivationProbeRuntime2.LastSequence == 1;
		ownerGameplayActivationProbeRuntime?.SetAlive(alive: true);
		bool num5 = componentManager.RemoveRuntimeComponent(ownerGameplayActivationProbeRuntime2);
		int num6 = ownerGameplayActivationProbeRuntime?.CallCount ?? (-1);
		int num7 = ownerGameplayActivationProbeRuntime2?.CallCount ?? (-1);
		OwnerGameplayActivationProbeRuntime.ResetSequence();
		componentManager.NotifyOwnerGameplayActivated();
		bool flag4 = num5 && ownerGameplayActivationProbeRuntime != null && ownerGameplayActivationProbeRuntime2 != null && ownerGameplayActivationProbeRuntime2.IsReleased && ownerGameplayActivationProbeRuntime.CallCount == num6 + 1 && ownerGameplayActivationProbeRuntime2.CallCount == num7 && ownerGameplayActivationProbeRuntime.LastSequence == 1;
		OwnerGameplayActivationProbeRuntime ownerGameplayActivationProbeRuntime3 = componentManager.AddRuntimeComponent(definition3) as OwnerGameplayActivationProbeRuntime;
		int num8 = ownerGameplayActivationProbeRuntime?.CallCount ?? (-1);
		int num9 = ownerGameplayActivationProbeRuntime2?.CallCount ?? (-1);
		OwnerGameplayActivationProbeRuntime.ResetSequence();
		componentManager.NotifyOwnerGameplayActivated();
		bool flag5 = num5 && ownerGameplayActivationProbeRuntime != null && ownerGameplayActivationProbeRuntime2 != null && ownerGameplayActivationProbeRuntime3 != null && ownerGameplayActivationProbeRuntime.CallCount == num8 + 1 && ownerGameplayActivationProbeRuntime2.CallCount == num9 && ownerGameplayActivationProbeRuntime3.CallCount == 1 && ownerGameplayActivationProbeRuntime.LastSequence == 1 && ownerGameplayActivationProbeRuntime3.LastSequence == 2;
		_dynamicLifecyclePassed = flag && flag3 && flag4 && flag5;
		Check(_dynamicLifecyclePassed, "Dynamic add/remove, singleton contraction, re-expansion, inactive filtering and callback order must match the old resource-list traversal.");
		CharacterComponentSet characterComponentSet = new CharacterComponentSet();
		characterComponentSet.Components.Add(CreateProbeDefinition("probe.owner-activation.second", 2, 1));
		characterComponentSet.Components.Add(CreateProbeDefinition("probe.owner-activation.first", 1, 0));
		componentManager.ComponentSet = characterComponentSet;
		componentManager.InitializeResourceComponents();
		componentManager.ActivateResourceComponents();
		OwnerGameplayActivationProbeRuntime runtime = componentManager.GetRuntime<OwnerGameplayActivationProbeRuntime>("probe.owner-activation.second");
		OwnerGameplayActivationProbeRuntime runtime2 = componentManager.GetRuntime<OwnerGameplayActivationProbeRuntime>("probe.owner-activation.first");
		OwnerGameplayActivationProbeRuntime.ResetSequence();
		componentManager.NotifyOwnerGameplayActivated();
		_rebuildPassed = runtime != null && runtime2 != null && runtime.CallCount == 1 && runtime2.CallCount == 1 && runtime.LastSequence == 1 && runtime2.LastSequence == 2;
		Check(_rebuildPassed, "Definition rebuild must recreate the sparse list in creation-plan order.");
		int num10 = runtime?.CallCount ?? (-1);
		int num11 = runtime2?.CallCount ?? (-1);
		componentManager.ComponentSet = null;
		componentManager.InitializeResourceComponents();
		componentManager.NotifyOwnerGameplayActivated();
		_cleanupPassed = componentManager.ResourceComponents.Count == 0 && runtime != null && runtime2 != null && runtime.IsReleased && runtime2.IsReleased && runtime.CallCount == num10 && runtime2.CallCount == num11;
		Check(_cleanupPassed, "Definition release must clear owner-activation dispatch without retaining released runtimes.");
		ownerGameplayActivationBareCharacter.QueueFree();
		RunReentrantAppendContract();
		RunModCompatibilityContract();
	}

	private void RunReentrantAppendContract()
	{
		OwnerGameplayActivationBareCharacter ownerGameplayActivationBareCharacter = new OwnerGameplayActivationBareCharacter
		{
			Name = "ReentrantOwnerGameplayActivationOwner"
		};
		ComponentManager manager = new ComponentManager
		{
			Name = "ComponentManager",
			ComponentSet = new CharacterComponentSet()
		};
		ownerGameplayActivationBareCharacter.componentManager = manager;
		manager.AttachOwner(ownerGameplayActivationBareCharacter);
		AddChild(ownerGameplayActivationBareCharacter, forceReadableName: false, InternalMode.Disabled);
		manager.InitializeResourceComponents();
		manager.ActivateResourceComponents();
		OwnerGameplayActivationProbeRuntime first = manager.AddRuntimeComponent(CreateProbeDefinition("probe.owner-activation.reentrant-first", 11, 0)) as OwnerGameplayActivationProbeRuntime;
		OwnerGameplayActivationProbeRuntime second = null;
		if (first != null)
		{
			first.Callback = delegate
			{
				first.Callback = null;
				second = manager.AddRuntimeComponent(CreateProbeDefinition("probe.owner-activation.reentrant-second", 12, 1)) as OwnerGameplayActivationProbeRuntime;
			};
		}
		OwnerGameplayActivationProbeRuntime.ResetSequence();
		manager.NotifyOwnerGameplayActivated();
		_reentrantAppendPassed = first != null && second != null && manager.ResourceComponents.Count == 2 && first.CallCount == 1 && second.CallCount == 1 && first.LastSequence == 1 && second.LastSequence == 2;
		Check(_reentrantAppendPassed, "A callback appended from the singleton slot must execute later in the same notification, matching the legacy list traversal.");
		ownerGameplayActivationBareCharacter.QueueFree();
	}

	private void RunModCompatibilityContract()
	{
		OwnerGameplayActivationBareCharacter ownerGameplayActivationBareCharacter = new OwnerGameplayActivationBareCharacter
		{
			Name = "ModOwnerGameplayActivationOwner"
		};
		ComponentManager componentManager = (ownerGameplayActivationBareCharacter.componentManager = new ComponentManager
		{
			Name = "ComponentManager",
			ComponentSet = new CharacterComponentSet()
		});
		componentManager.AttachOwner(ownerGameplayActivationBareCharacter);
		AddChild(ownerGameplayActivationBareCharacter, forceReadableName: false, InternalMode.Disabled);
		componentManager.InitializeResourceComponents();
		componentManager.ActivateResourceComponents();
		try
		{
			int num = CharacterComponentRuntimeTypeRegistry.RegisterAssembly("test.owner-gameplay-activation.mod-compatibility", typeof(OwnerGameplayActivationModCompatibilityProbeRuntime).Assembly);
			ModCharacterComponentDefinition definition = new ModCharacterComponentDefinition
			{
				ComponentTypeId = "OwnerGameplayActivationModCompatibilityProbeRuntime",
				DefinitionId = "test.owner-gameplay-activation.mod-compatibility",
				InstanceId = "probe.owner-activation.mod-compatibility",
				WireIndex = 0,
				RuntimeTypeName = typeof(OwnerGameplayActivationModCompatibilityProbeRuntime).FullName
			};
			OwnerGameplayActivationModCompatibilityProbeRuntime ownerGameplayActivationModCompatibilityProbeRuntime = componentManager.AddRuntimeComponent(definition) as OwnerGameplayActivationModCompatibilityProbeRuntime;
			componentManager.NotifyOwnerGameplayActivated();
			_modCompatibilityPassed = num > 0 && ownerGameplayActivationModCompatibilityProbeRuntime != null && !ownerGameplayActivationModCompatibilityProbeRuntime.HasOwnerGameplayActivationWork && ownerGameplayActivationModCompatibilityProbeRuntime.CallCount == 1;
		}
		finally
		{
			CharacterComponentRuntimeTypeRegistry.UnregisterOwner("test.owner-gameplay-activation.mod-compatibility");
		}
		Check(_modCompatibilityPassed, "A Mod proxy runtime must retain conservative owner-activation dispatch even when it cannot opt into the host-internal capability.");
		ownerGameplayActivationBareCharacter.QueueFree();
	}

	private static OwnerGameplayActivationProbeDefinition CreateProbeDefinition(string instanceId, int orderId, int wireIndex)
	{
		return new OwnerGameplayActivationProbeDefinition
		{
			ComponentTypeId = "OwnerGameplayActivationProbeRuntime",
			DefinitionId = $"test.owner-gameplay-activation.{orderId}",
			InstanceId = instanceId,
			WireIndex = wireIndex,
			OrderId = orderId
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
