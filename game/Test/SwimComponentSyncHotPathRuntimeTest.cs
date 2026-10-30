using System;
using Godot;
using Godot.Collections;

public partial class SwimComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly SwimComponent[] _components = new SwimComponent[1000];

	private readonly TowerDefenseZombie[] _owners = new TowerDefenseZombie[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private SwimComponentDefinition _definition;

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
			GD.Print($"SWIM_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("SWIM_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new SwimComponentDefinition
		{
			ComponentTypeId = "SwimComponent",
			DefinitionId = "swim.sync.hotpath.runtime",
			InstanceId = "swim.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			SwimSyncProbeOwner swimSyncProbeOwner = new SwimSyncProbeOwner
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
				instance = new TowerDefenseCharacterInstance(),
				ProcessMode = ProcessModeEnum.Disabled
			};
			swimSyncProbeOwner.instance.character = swimSyncProbeOwner;
			AddChild(swimSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			SwimComponent swimComponent = new SwimComponent();
			swimComponent.Bind(componentManager, swimSyncProbeOwner, _definition);
			swimComponent.Activate();
			_owners[i] = swimSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = swimComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		SwimComponent swimComponent = _components[0];
		Dictionary dictionary = swimComponent.SyncSerialize();
		bool num = dictionary.Count == 3 && dictionary.ContainsKey("inSwimPlay") && dictionary.ContainsKey("inWater") && dictionary.ContainsKey("outFromWater") && !dictionary["inSwimPlay"].AsBool() && !dictionary["inWater"].AsBool() && !dictionary["outFromWater"].AsBool();
		dictionary["_alive"] = true;
		Dictionary dictionary2 = swimComponent.SyncSerialize();
		bool flag = dictionary == dictionary2 && dictionary2.Count == 3 && !dictionary2.ContainsKey("_alive");
		dictionary2["foreign"] = true;
		Dictionary dictionary3 = swimComponent.SyncSerialize();
		bool flag2 = dictionary2 == dictionary3 && dictionary3.Count == 3 && !dictionary3.ContainsKey("foreign");
		swimComponent.parent.waterInteractionComponent = new WaterInteractionComponent
		{
			outFromWater = true
		};
		swimComponent.SyncDeserialize(new Dictionary
		{
			["inSwimPlay"] = true,
			["inWater"] = true,
			["outFromWater"] = true
		});
		Dictionary dictionary4 = swimComponent.SyncSerialize();
		bool flag3 = dictionary4["inSwimPlay"].AsBool() && dictionary4["inWater"].AsBool() && dictionary4["outFromWater"].AsBool();
		bool flag4 = swimComponent.ExportComponentSave().Count == 1;
		swimComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		Dictionary dictionary5 = swimComponent.SyncSerialize();
		bool flag5 = swimComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && dictionary5.Count == 3 && !dictionary5["inSwimPlay"].AsBool() && !dictionary5["inWater"].AsBool() && !dictionary5["outFromWater"].AsBool();
		ComponentManager manager = new ComponentManager();
		SwimSyncProbeOwner swimSyncProbeOwner = new SwimSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		swimSyncProbeOwner.instance.character = swimSyncProbeOwner;
		AddChild(swimSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		swimComponent.Bind(manager, swimSyncProbeOwner, _definition);
		swimComponent.Activate();
		bool flag6 = swimComponent.IsAttached && !swimComponent.IsReleased && swimComponent.SyncSerialize().Count == 3;
		swimComponent.Release();
		bool isReleased = swimComponent.IsReleased;
		swimSyncProbeOwner.Free();
		return num && flag && flag2 && flag3 && flag4 && flag5 && flag6 && isReleased;
	}

	private void RestoreBenchmarkFixture()
	{
		_owners[0].inSwimPlay = false;
		_owners[0].inWater = false;
		_owners[0].waterInteractionComponent = null;
		SwimComponent swimComponent = new SwimComponent();
		swimComponent.Bind(_managers[0], _owners[0], _definition);
		swimComponent.Activate();
		_components[0] = swimComponent;
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
			SwimComponent obj = _components[i];
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
