using System;
using Godot;
using Godot.Collections;

public partial class RecycleComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly RecycleComponent[] _components = new RecycleComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private RecycleComponentDefinition _definition;

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
			GD.Print($"RECYCLE_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("RECYCLE_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new RecycleComponentDefinition
		{
			ComponentTypeId = "RecycleComponent",
			DefinitionId = "recycle.sync.hotpath.runtime",
			InstanceId = "recycle.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			RecycleSyncProbeOwner recycleSyncProbeOwner = new RecycleSyncProbeOwner
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
				instance = new TowerDefenseCharacterInstance(),
				ProcessMode = ProcessModeEnum.Disabled
			};
			recycleSyncProbeOwner.instance.character = recycleSyncProbeOwner;
			AddChild(recycleSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			RecycleComponent recycleComponent = new RecycleComponent();
			recycleComponent.Bind(componentManager, recycleSyncProbeOwner, _definition);
			recycleComponent.Activate();
			recycleComponent.SyncDeserialize(new Dictionary
			{
				["sun_velocity_x"] = 1f,
				["sun_velocity_y"] = -2f
			});
			_owners[i] = recycleSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = recycleComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		RecycleComponent recycleComponent = _components[0];
		Dictionary dictionary = recycleComponent.SyncSerialize();
		bool num = dictionary.Count == 2 && dictionary.ContainsKey("sun_velocity_x") && dictionary.ContainsKey("sun_velocity_y");
		dictionary["_alive"] = true;
		Dictionary dictionary2 = recycleComponent.SyncSerialize();
		bool flag = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive");
		dictionary2["foreign"] = true;
		Dictionary dictionary3 = recycleComponent.SyncSerialize();
		bool flag2 = dictionary2 == dictionary3 && !dictionary3.ContainsKey("foreign");
		recycleComponent.SyncDeserialize(new Dictionary
		{
			["sun_velocity_x"] = 7f,
			["sun_velocity_y"] = -8f
		});
		Dictionary dictionary4 = recycleComponent.SyncSerialize();
		bool flag3 = dictionary4["sun_velocity_x"].AsSingle() == 7f && dictionary4["sun_velocity_y"].AsSingle() == -8f;
		recycleComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag4 = recycleComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && recycleComponent.SyncSerialize().Count == 0;
		ComponentManager manager = new ComponentManager();
		RecycleSyncProbeOwner recycleSyncProbeOwner = new RecycleSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		recycleSyncProbeOwner.instance.character = recycleSyncProbeOwner;
		AddChild(recycleSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		recycleComponent.Bind(manager, recycleSyncProbeOwner, _definition);
		recycleComponent.Activate();
		bool flag5 = recycleComponent.IsAttached && recycleComponent.SyncSerialize().Count == 0;
		bool flag6 = recycleComponent.ExportComponentSave().Count == 0;
		recycleComponent.Release();
		bool isReleased = recycleComponent.IsReleased;
		recycleSyncProbeOwner.Free();
		return num && flag && flag2 && flag3 && flag4 && flag5 && flag6 && isReleased;
	}

	private void RestoreBenchmarkFixture()
	{
		RecycleComponent recycleComponent = new RecycleComponent();
		recycleComponent.Bind(_managers[0], _owners[0], _definition);
		recycleComponent.Activate();
		recycleComponent.SyncDeserialize(new Dictionary
		{
			["sun_velocity_x"] = 1f,
			["sun_velocity_y"] = -2f
		});
		_components[0] = recycleComponent;
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
			RecycleComponent obj = _components[i];
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
