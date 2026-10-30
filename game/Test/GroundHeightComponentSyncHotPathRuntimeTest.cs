using System;
using Godot;
using Godot.Collections;

public partial class GroundHeightComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly GroundHeightComponent[] _components = new GroundHeightComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private GroundHeightComponentDefinition _definition;

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
			GD.Print($"GROUND_HEIGHT_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("GROUND_HEIGHT_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new GroundHeightComponentDefinition
		{
			ComponentTypeId = "GroundHeightComponent",
			DefinitionId = "ground-height.sync.hotpath.runtime",
			InstanceId = "ground-height.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			GroundHeightSyncProbeOwner groundHeightSyncProbeOwner = new GroundHeightSyncProbeOwner
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
				instance = new TowerDefenseCharacterInstance(),
				syncId = i + 1,
				inWater = true,
				groundHeight = -25.0,
				ProcessMode = ProcessModeEnum.Disabled
			};
			groundHeightSyncProbeOwner.instance.character = groundHeightSyncProbeOwner;
			AddChild(groundHeightSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			GroundHeightComponent groundHeightComponent = new GroundHeightComponent
			{
				onLadder = true
			};
			groundHeightComponent.Bind(componentManager, groundHeightSyncProbeOwner, _definition);
			groundHeightComponent.Activate();
			_owners[i] = groundHeightSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = groundHeightComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		GroundHeightComponent groundHeightComponent = _components[0];
		Dictionary dictionary = groundHeightComponent.SyncSerialize();
		bool num = dictionary.Count == 3 && dictionary.ContainsKey("height") && Math.Abs(dictionary["height"].AsDouble() + 25.0) < 0.0001 && dictionary["water"].AsBool() && dictionary["ladder"].AsBool();
		dictionary["_alive"] = true;
		Dictionary dictionary2 = groundHeightComponent.SyncSerialize();
		bool flag = dictionary == dictionary2 && dictionary2.Count == 3 && !dictionary2.ContainsKey("_alive");
		dictionary2["foreign"] = true;
		Dictionary dictionary3 = groundHeightComponent.SyncSerialize();
		bool flag2 = dictionary2 == dictionary3 && dictionary3.Count == 3 && !dictionary3.ContainsKey("foreign");
		groundHeightComponent.SyncDeserialize(new Dictionary
		{
			["height"] = 0.0,
			["water"] = false,
			["ladder"] = false
		});
		Dictionary dictionary4 = groundHeightComponent.SyncSerialize();
		bool flag3 = Math.Abs(dictionary4["height"].AsDouble()) < 0.0001 && !dictionary4["water"].AsBool() && !dictionary4["ladder"].AsBool();
		bool flag4 = groundHeightComponent.ExportComponentSave().Count == 0;
		groundHeightComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag5 = groundHeightComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && groundHeightComponent.SyncSerialize().Count == 0;
		ComponentManager manager = new ComponentManager();
		GroundHeightSyncProbeOwner groundHeightSyncProbeOwner = new GroundHeightSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			syncId = 9001,
			ProcessMode = ProcessModeEnum.Disabled
		};
		groundHeightSyncProbeOwner.instance.character = groundHeightSyncProbeOwner;
		AddChild(groundHeightSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		groundHeightComponent.Bind(manager, groundHeightSyncProbeOwner, _definition);
		groundHeightComponent.Activate();
		bool flag6 = groundHeightComponent.IsAttached && !groundHeightComponent.IsReleased && groundHeightComponent.SyncSerialize().Count == 3;
		groundHeightComponent.Release();
		bool isReleased = groundHeightComponent.IsReleased;
		groundHeightSyncProbeOwner.Free();
		return num && flag && flag2 && flag3 && flag4 && flag5 && flag6 && isReleased;
	}

	private void RestoreBenchmarkFixture()
	{
		GroundHeightComponent groundHeightComponent = new GroundHeightComponent
		{
			onLadder = true
		};
		groundHeightComponent.Bind(_managers[0], _owners[0], _definition);
		groundHeightComponent.Activate();
		_components[0] = groundHeightComponent;
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
			GroundHeightComponent obj = _components[i];
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
