using System;
using Godot;
using Godot.Collections;

public partial class GravebusterComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly GravebusterComponent[] _components = new GravebusterComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private GravebusterComponentDefinition _definition;

	private TowerDefenseGravestone _graveStone;

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
			GD.Print($"GRAVEBUSTER_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("GRAVEBUSTER_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = ResourceLoader.Load<GravebusterComponentDefinition>("res://Script/Component/TowerDefense/Character/GravebusterComponent/GravebusterComponentDefinition.tres", null, ResourceLoader.CacheMode.Reuse);
		if (_definition == null)
		{
			throw new InvalidOperationException("Gravebuster definition resource failed to load.");
		}
		_graveStone = new TowerDefenseGravestone
		{
			syncId = 4321
		};
		for (int i = 0; i < 1000; i++)
		{
			GravebusterSyncProbeOwner gravebusterSyncProbeOwner = new GravebusterSyncProbeOwner
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
				instance = new TowerDefenseCharacterInstance(),
				ProcessMode = ProcessModeEnum.Disabled
			};
			gravebusterSyncProbeOwner.instance.character = gravebusterSyncProbeOwner;
			AddChild(gravebusterSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			GravebusterComponent gravebusterComponent = new GravebusterComponent();
			gravebusterComponent.Bind(componentManager, gravebusterSyncProbeOwner, _definition);
			gravebusterComponent.Activate();
			gravebusterComponent.RegisterStateRuntime();
			gravebusterComponent.graveStone = _graveStone;
			gravebusterComponent.SyncDeserialize(new Dictionary
			{
				["drop_velocity_x"] = 12.5f,
				["drop_velocity_y"] = -30f
			});
			_owners[i] = gravebusterSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = gravebusterComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		GravebusterComponent gravebusterComponent = _components[0];
		Dictionary dictionary = gravebusterComponent.SyncSerialize();
		bool num = dictionary.Count == 4 && dictionary.ContainsKey("state") && dictionary["state"].AsString() == "gravebuster.idle" && dictionary.ContainsKey("grave_stone_sync_id") && dictionary["grave_stone_sync_id"].AsInt32() == 4321 && dictionary.ContainsKey("drop_velocity_x") && dictionary.ContainsKey("drop_velocity_y");
		dictionary["_alive"] = true;
		Dictionary dictionary2 = gravebusterComponent.SyncSerialize();
		bool flag = dictionary == dictionary2 && dictionary2.Count == 4 && !dictionary2.ContainsKey("_alive");
		dictionary2["foreign"] = true;
		Dictionary dictionary3 = gravebusterComponent.SyncSerialize();
		bool flag2 = dictionary2 == dictionary3 && dictionary3.Count == 4 && !dictionary3.ContainsKey("foreign");
		bool num2 = gravebusterComponent.SendStateEvent("ToGravebuster");
		Dictionary dictionary4 = gravebusterComponent.SyncSerialize();
		bool flag3 = num2 && dictionary4["state"].AsString() == "gravebuster.consume" && dictionary4["grave_stone_sync_id"].AsInt32() == 4321;
		gravebusterComponent.SyncDeserialize(new Dictionary
		{
			["drop_velocity_x"] = 0f,
			["drop_velocity_y"] = 0f
		});
		Dictionary dictionary5 = gravebusterComponent.SyncSerialize();
		bool flag4 = dictionary5.Count == 2 && dictionary5["state"].AsString() == "gravebuster.consume" && dictionary5["grave_stone_sync_id"].AsInt32() == 4321;
		Dictionary dictionary6 = gravebusterComponent.ExportComponentSave();
		bool flag5 = dictionary6.Count > 0 && !dictionary6.ContainsKey("_alive") && dictionary6 != dictionary5;
		gravebusterComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag6 = gravebusterComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && gravebusterComponent.SyncSerialize().Count == 1;
		ComponentManager manager = new ComponentManager();
		GravebusterSyncProbeOwner gravebusterSyncProbeOwner = new GravebusterSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		gravebusterSyncProbeOwner.instance.character = gravebusterSyncProbeOwner;
		AddChild(gravebusterSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		gravebusterComponent.Bind(manager, gravebusterSyncProbeOwner, _definition);
		gravebusterComponent.Activate();
		gravebusterComponent.RegisterStateRuntime();
		bool flag7 = gravebusterComponent.IsAttached && !gravebusterComponent.IsReleased && gravebusterComponent.SyncSerialize().Count == 1;
		gravebusterComponent.Release();
		bool isReleased = gravebusterComponent.IsReleased;
		gravebusterSyncProbeOwner.Free();
		return num && flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && isReleased;
	}

	private void RestoreBenchmarkFixture()
	{
		GravebusterComponent gravebusterComponent = new GravebusterComponent();
		gravebusterComponent.Bind(_managers[0], _owners[0], _definition);
		gravebusterComponent.Activate();
		gravebusterComponent.RegisterStateRuntime();
		gravebusterComponent.graveStone = _graveStone;
		gravebusterComponent.SyncDeserialize(new Dictionary
		{
			["drop_velocity_x"] = 12.5f,
			["drop_velocity_y"] = -30f
		});
		_components[0] = gravebusterComponent;
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
			GravebusterComponent obj = _components[i];
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
		_graveStone?.Free();
		_graveStone = null;
	}
}
