using System;
using Godot;
using Godot.Collections;

public partial class ImpThrowerComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly ImpThrowerComponent[] _components = new ImpThrowerComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private ImpThrowerComponentDefinition _definition;

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
			flag = RunFunctionalContract();
			RestoreBenchmarkFixture();
			PrimePayloadCapacity();
			OptimizationBatchSampler.PrepareForWarmup();
			optimizationBatchResult = Benchmark();
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			bool flag2 = flag && num == 1000 && num2 == 1000 && optimizationBatchResult.AllocatedBytes == 0L && optimizationBatchResult.Gen0Collections == 0 && optimizationBatchResult.Gen1Collections == 0 && optimizationBatchResult.Gen2Collections == 0 && optimizationBatchResult.P99Milliseconds < 0.2;
			GD.Print($"IMP_THROWER_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("IMP_THROWER_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new ImpThrowerComponentDefinition
		{
			ComponentTypeId = "ImpThrowerComponent",
			DefinitionId = "imp.thrower.sync.hotpath.runtime",
			InstanceId = "imp.thrower.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			ImpThrowerSyncProbeOwner impThrowerSyncProbeOwner = new ImpThrowerSyncProbeOwner
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
				instance = new TowerDefenseCharacterInstance(),
				ProcessMode = ProcessModeEnum.Disabled
			};
			impThrowerSyncProbeOwner.instance.character = impThrowerSyncProbeOwner;
			AddChild(impThrowerSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			ImpThrowerComponent impThrowerComponent = new ImpThrowerComponent();
			impThrowerComponent.Bind(componentManager, impThrowerSyncProbeOwner, _definition);
			impThrowerComponent.Activate();
			impThrowerComponent.SyncDeserialize(new Dictionary { ["land_pos_x"] = 12.5 });
			_owners[i] = impThrowerSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = impThrowerComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		ImpThrowerComponent impThrowerComponent = _components[0];
		Dictionary dictionary = impThrowerComponent.SyncSerialize();
		bool num = dictionary.Count == 1 && dictionary.ContainsKey("land_pos_x") && Math.Abs(dictionary["land_pos_x"].AsDouble() - 12.5) < 1E-06;
		dictionary["_alive"] = true;
		Dictionary dictionary2 = impThrowerComponent.SyncSerialize();
		bool flag = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive");
		dictionary2["foreign"] = true;
		Dictionary dictionary3 = impThrowerComponent.SyncSerialize();
		bool flag2 = dictionary2 == dictionary3 && !dictionary3.ContainsKey("foreign");
		impThrowerComponent.SyncDeserialize(new Dictionary { ["land_pos_x"] = 0.0 });
		bool flag3 = impThrowerComponent.SyncSerialize()["land_pos_x"].AsDouble() == 0.0;
		impThrowerComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag4 = impThrowerComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && impThrowerComponent.SyncSerialize().Count == 1;
		ComponentManager manager = new ComponentManager();
		ImpThrowerSyncProbeOwner impThrowerSyncProbeOwner = new ImpThrowerSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		impThrowerSyncProbeOwner.instance.character = impThrowerSyncProbeOwner;
		AddChild(impThrowerSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		impThrowerComponent.Bind(manager, impThrowerSyncProbeOwner, _definition);
		impThrowerComponent.Activate();
		bool flag5 = impThrowerComponent.IsAttached && impThrowerComponent.SyncSerialize().Count == 1;
		impThrowerComponent.Release();
		bool isReleased = impThrowerComponent.IsReleased;
		impThrowerSyncProbeOwner.Free();
		return num && flag && flag2 && flag3 && flag4 && flag5 && isReleased;
	}

	private void RestoreBenchmarkFixture()
	{
		ImpThrowerComponent impThrowerComponent = new ImpThrowerComponent();
		impThrowerComponent.Bind(_managers[0], _owners[0], _definition);
		impThrowerComponent.Activate();
		impThrowerComponent.SyncDeserialize(new Dictionary { ["land_pos_x"] = 12.5 });
		_components[0] = impThrowerComponent;
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

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			TowerDefenseCharacter obj = _owners[i];
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
			ImpThrowerComponent obj = _components[i];
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
