using System;
using Godot;
using Godot.Collections;

public partial class TargetRegistrationComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly TargetRegistrationComponent[] _components = new TargetRegistrationComponent[1000];

	private readonly TargetRegistrationSyncProbeOwner[] _owners = new TargetRegistrationSyncProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private TargetRegistrationComponentDefinition _definition;

	public override void _Ready()
	{
		Run();
	}

	private void Run()
	{
		bool flag = false;
		OptimizationBatchResult optimizationBatchResult = default(OptimizationBatchResult);
		try
		{
			CreateWorkload();
			flag = RunFunctionalContract() && RestoreBenchmarkFixture();
			PrimePayloadCapacity();
			OptimizationBatchSampler.PrepareForWarmup();
			optimizationBatchResult = Benchmark();
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			bool flag2 = flag && num == 1000 && num2 == 1000 && optimizationBatchResult.AllocatedBytes == 0L && optimizationBatchResult.Gen0Collections == 0 && optimizationBatchResult.Gen1Collections == 0 && optimizationBatchResult.Gen2Collections == 0 && optimizationBatchResult.P99Milliseconds < 0.2;
			GD.Print($"TARGET_REGISTRATION_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("TARGET_REGISTRATION_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new TargetRegistrationComponentDefinition
		{
			ComponentTypeId = "TargetRegistrationComponent",
			DefinitionId = "target_registration.sync.hotpath.runtime",
			InstanceId = "target_registration.sync.hotpath.runtime",
			WireIndex = 0,
			allLineCheck = false,
			canProjectileCheck = true,
			canCarry = true
		};
		for (int i = 0; i < 1000; i++)
		{
			TargetRegistrationSyncProbeOwner targetRegistrationSyncProbeOwner = CreateOwner();
			AddChild(targetRegistrationSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			TargetRegistrationComponent targetRegistrationComponent = new TargetRegistrationComponent();
			targetRegistrationComponent.Bind(componentManager, targetRegistrationSyncProbeOwner, _definition);
			targetRegistrationComponent.Activate();
			_owners[i] = targetRegistrationSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = targetRegistrationComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		TargetRegistrationComponent targetRegistrationComponent = _components[0];
		Dictionary dictionary = targetRegistrationComponent.SyncSerialize();
		bool flag = HasState(dictionary, allLine: false, projectile: true, carry: true);
		dictionary["_alive"] = true;
		Dictionary dictionary2 = targetRegistrationComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasState(dictionary2, allLine: false, projectile: true, carry: true);
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = targetRegistrationComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasState(dictionary3, allLine: false, projectile: true, carry: true);
		targetRegistrationComponent.allLineCheck = true;
		targetRegistrationComponent.canProjectileCheck = false;
		targetRegistrationComponent.canCarry = false;
		bool flag4 = HasState(targetRegistrationComponent.SyncSerialize(), allLine: true, projectile: false, carry: false);
		targetRegistrationComponent.SyncDeserialize(new Dictionary
		{
			["all_line"] = false,
			["projectile"] = true,
			["carry"] = true
		});
		bool flag5 = HasState(targetRegistrationComponent.SyncSerialize(), allLine: false, projectile: true, carry: true);
		if (flag && flag2 && flag3 && flag4 && flag5)
		{
			return RunLifecycleContract();
		}
		return false;
	}

	private bool RunLifecycleContract()
	{
		TargetRegistrationSyncProbeOwner targetRegistrationSyncProbeOwner = CreateOwner();
		AddChild(targetRegistrationSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		TargetRegistrationComponent targetRegistrationComponent = new TargetRegistrationComponent();
		targetRegistrationComponent.Bind(manager, targetRegistrationSyncProbeOwner, _definition);
		targetRegistrationComponent.Activate();
		targetRegistrationComponent.allLineCheck = true;
		targetRegistrationComponent.canProjectileCheck = false;
		targetRegistrationComponent.canCarry = false;
		targetRegistrationComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool num = targetRegistrationComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasState(targetRegistrationComponent.SyncSerialize(), allLine: true, projectile: false, carry: false);
		TargetRegistrationSyncProbeOwner targetRegistrationSyncProbeOwner2 = CreateOwner();
		AddChild(targetRegistrationSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		targetRegistrationComponent.Bind(manager2, targetRegistrationSyncProbeOwner2, _definition);
		targetRegistrationComponent.Activate();
		bool flag = targetRegistrationComponent.IsAttached && !targetRegistrationComponent.IsReleased && HasState(targetRegistrationComponent.SyncSerialize(), allLine: true, projectile: false, carry: false);
		targetRegistrationComponent.Release();
		bool flag2 = targetRegistrationComponent.IsReleased && HasState(targetRegistrationComponent.SyncSerialize(), allLine: true, projectile: false, carry: false);
		targetRegistrationSyncProbeOwner.Free();
		targetRegistrationSyncProbeOwner2.Free();
		return num && flag && flag2;
	}

	private bool RestoreBenchmarkFixture()
	{
		for (int i = 0; i < 1000; i++)
		{
			TargetRegistrationComponent targetRegistrationComponent = _components[i];
			targetRegistrationComponent.allLineCheck = false;
			targetRegistrationComponent.canProjectileCheck = true;
			targetRegistrationComponent.canCarry = true;
			if (targetRegistrationComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasState(targetRegistrationComponent.SyncSerialize(), allLine: false, projectile: true, carry: true))
			{
				return false;
			}
		}
		return true;
	}

	private void PrimePayloadCapacity()
	{
		for (int i = 0; i < 240; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				_components[j].SyncSerialize();
			}
		}
	}

	private OptimizationBatchResult Benchmark()
	{
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int i = 0; i < 1200; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			for (int j = 0; j < 1000; j++)
			{
				_components[j].SyncSerialize();
			}
			optimizationBatchSampler.EndSample(startTicks);
		}
		return optimizationBatchSampler.Complete();
	}

	private static TargetRegistrationSyncProbeOwner CreateOwner()
	{
		TargetRegistrationSyncProbeOwner targetRegistrationSyncProbeOwner = new TargetRegistrationSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		targetRegistrationSyncProbeOwner.instance.character = targetRegistrationSyncProbeOwner;
		return targetRegistrationSyncProbeOwner;
	}

	private static bool HasState(Dictionary payload, bool allLine, bool projectile, bool carry)
	{
		if (payload.Count == 3 && payload.TryGetValue("all_line", out var value) && value.VariantType == Variant.Type.Bool && value.AsBool() == allLine && payload.TryGetValue("projectile", out var value2) && value2.VariantType == Variant.Type.Bool && value2.AsBool() == projectile && payload.TryGetValue("carry", out var value3) && value3.VariantType == Variant.Type.Bool)
		{
			return value3.AsBool() == carry;
		}
		return false;
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			TargetRegistrationSyncProbeOwner obj = _owners[i];
			if (obj != null && obj.IsInsideTree())
			{
				num++;
			}
		}
		return num;
	}

	private int CountActiveInstances()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			TargetRegistrationComponent obj = _components[i];
			if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				num++;
			}
		}
		return num;
	}

	private void ReleaseWorkload()
	{
		for (int i = 0; i < 1000; i++)
		{
			_components[i]?.Release();
			_owners[i]?.Free();
			_components[i] = null;
			_owners[i] = null;
			_managers[i] = null;
		}
	}
}
