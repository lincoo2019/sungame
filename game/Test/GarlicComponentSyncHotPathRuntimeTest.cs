using System;
using Godot;
using Godot.Collections;

public partial class GarlicComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly GarlicComponent[] _components = new GarlicComponent[1000];

	private readonly TowerDefenseZombie[] _owners = new TowerDefenseZombie[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private GarlicComponentDefinition _definition;

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
			GD.Print($"GARLIC_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("GARLIC_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new GarlicComponentDefinition
		{
			ComponentTypeId = "GarlicComponent",
			DefinitionId = "garlic.sync.hotpath.runtime",
			InstanceId = "garlic.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			GarlicSyncProbeOwner garlicSyncProbeOwner = new GarlicSyncProbeOwner
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
				instance = new TowerDefenseCharacterInstance(),
				ProcessMode = ProcessModeEnum.Disabled
			};
			garlicSyncProbeOwner.instance.character = garlicSyncProbeOwner;
			AddChild(garlicSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			GarlicComponent garlicComponent = new GarlicComponent();
			garlicComponent.Bind(componentManager, garlicSyncProbeOwner, _definition);
			garlicComponent.Activate();
			garlicComponent._syncMoveDir = 1;
			_owners[i] = garlicSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = garlicComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		GarlicComponent garlicComponent = _components[0];
		Dictionary dictionary = garlicComponent.SyncSerialize();
		bool num = dictionary.Count == 2 && dictionary.ContainsKey("move_dir") && dictionary["move_dir"].AsInt32() == 1 && dictionary.ContainsKey("is_change_line") && !dictionary["is_change_line"].AsBool();
		dictionary["_alive"] = true;
		Dictionary dictionary2 = garlicComponent.SyncSerialize();
		bool flag = dictionary == dictionary2 && dictionary2.Count == 2 && !dictionary2.ContainsKey("_alive");
		dictionary2["foreign"] = true;
		Dictionary dictionary3 = garlicComponent.SyncSerialize();
		bool flag2 = dictionary2 == dictionary3 && dictionary3.Count == 2 && !dictionary3.ContainsKey("foreign");
		garlicComponent.SyncDeserialize(new Dictionary
		{
			["move_dir"] = -1,
			["is_change_line"] = true
		});
		Dictionary dictionary4 = garlicComponent.SyncSerialize();
		bool flag3 = dictionary4["move_dir"].AsInt32() == -1 && dictionary4["is_change_line"].AsBool() && garlicComponent.parent.isChangeLine;
		bool flag4 = garlicComponent.ExportComponentSave().Count == 0;
		garlicComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag5 = garlicComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && garlicComponent.SyncSerialize().Count == 2 && !garlicComponent.SyncSerialize()["is_change_line"].AsBool();
		ComponentManager manager = new ComponentManager();
		GarlicSyncProbeOwner garlicSyncProbeOwner = new GarlicSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		garlicSyncProbeOwner.instance.character = garlicSyncProbeOwner;
		AddChild(garlicSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		garlicComponent.Bind(manager, garlicSyncProbeOwner, _definition);
		garlicComponent.Activate();
		bool flag6 = garlicComponent.IsAttached && !garlicComponent.IsReleased && garlicComponent.SyncSerialize().Count == 2;
		garlicComponent.Release();
		bool isReleased = garlicComponent.IsReleased;
		garlicSyncProbeOwner.Free();
		return num && flag && flag2 && flag3 && flag4 && flag5 && flag6 && isReleased;
	}

	private void RestoreBenchmarkFixture()
	{
		_owners[0].isChangeLine = false;
		GarlicComponent garlicComponent = new GarlicComponent
		{
			_syncMoveDir = 1
		};
		garlicComponent.Bind(_managers[0], _owners[0], _definition);
		garlicComponent.Activate();
		_components[0] = garlicComponent;
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
			TowerDefenseZombie obj = _owners[i];
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
			GarlicComponent obj = _components[i];
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
