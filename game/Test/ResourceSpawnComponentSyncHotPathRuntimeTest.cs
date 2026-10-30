using System;
using Godot;
using Godot.Collections;

public partial class ResourceSpawnComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly ResourceSpawnComponent[] _components = new ResourceSpawnComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private ResourceSpawnComponentDefinition _definition;

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
			GD.Print($"RESOURCE_SPAWN_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("RESOURCE_SPAWN_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new ResourceSpawnComponentDefinition
		{
			ComponentTypeId = "ResourceSpawnComponent",
			DefinitionId = "resource.spawn.sync.hotpath.runtime",
			InstanceId = "resource.spawn.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			ResourceSpawnSyncProbeOwner resourceSpawnSyncProbeOwner = new ResourceSpawnSyncProbeOwner
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
				instance = new TowerDefenseCharacterInstance(),
				ProcessMode = ProcessModeEnum.Disabled
			};
			resourceSpawnSyncProbeOwner.instance.character = resourceSpawnSyncProbeOwner;
			AddChild(resourceSpawnSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			ResourceSpawnComponent resourceSpawnComponent = new ResourceSpawnComponent();
			resourceSpawnComponent.Bind(componentManager, resourceSpawnSyncProbeOwner, _definition);
			resourceSpawnComponent.Activate();
			resourceSpawnComponent._syncLastSunVelocity = new Vector2(1f, -2f);
			resourceSpawnComponent._syncLastCoinVelocity = new Vector2(-3f, -4f);
			_owners[i] = resourceSpawnSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = resourceSpawnComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		ResourceSpawnComponent resourceSpawnComponent = _components[0];
		Dictionary dictionary = resourceSpawnComponent.SyncSerialize();
		bool num = dictionary.Count == 4 && dictionary.ContainsKey("sun_velocity_x") && dictionary.ContainsKey("sun_velocity_y") && dictionary.ContainsKey("coin_velocity_x") && dictionary.ContainsKey("coin_velocity_y");
		dictionary["_alive"] = true;
		Dictionary dictionary2 = resourceSpawnComponent.SyncSerialize();
		bool flag = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive");
		dictionary2["foreign"] = true;
		Dictionary dictionary3 = resourceSpawnComponent.SyncSerialize();
		bool flag2 = dictionary2 == dictionary3 && !dictionary3.ContainsKey("foreign");
		resourceSpawnComponent.SyncDeserialize(new Dictionary
		{
			["sun_velocity_x"] = 7f,
			["sun_velocity_y"] = -8f,
			["coin_velocity_x"] = 9f,
			["coin_velocity_y"] = -10f
		});
		Dictionary dictionary4 = resourceSpawnComponent.SyncSerialize();
		bool flag3 = dictionary4["sun_velocity_x"].AsSingle() == 7f && dictionary4["sun_velocity_y"].AsSingle() == -8f && dictionary4["coin_velocity_x"].AsSingle() == 9f && dictionary4["coin_velocity_y"].AsSingle() == -10f;
		bool syncDeserializing = resourceSpawnComponent._syncDeserializing;
		resourceSpawnComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag4 = resourceSpawnComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && !resourceSpawnComponent._syncDeserializing;
		ComponentManager manager = new ComponentManager();
		ResourceSpawnSyncProbeOwner resourceSpawnSyncProbeOwner = new ResourceSpawnSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		resourceSpawnSyncProbeOwner.instance.character = resourceSpawnSyncProbeOwner;
		AddChild(resourceSpawnSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		resourceSpawnComponent.Bind(manager, resourceSpawnSyncProbeOwner, _definition);
		resourceSpawnComponent.Activate();
		bool flag5 = resourceSpawnComponent.IsAttached && !resourceSpawnComponent.IsReleased && resourceSpawnComponent.SyncSerialize().Count == 4;
		bool flag6 = resourceSpawnComponent.ExportComponentSave().Count == 0;
		resourceSpawnComponent.Release();
		bool isReleased = resourceSpawnComponent.IsReleased;
		resourceSpawnSyncProbeOwner.Free();
		return num && flag && flag2 && flag3 && syncDeserializing && flag4 && flag5 && flag6 && isReleased;
	}

	private void RestoreBenchmarkFixture()
	{
		ResourceSpawnComponent resourceSpawnComponent = new ResourceSpawnComponent();
		resourceSpawnComponent.Bind(_managers[0], _owners[0], _definition);
		resourceSpawnComponent.Activate();
		resourceSpawnComponent._syncLastSunVelocity = new Vector2(1f, -2f);
		resourceSpawnComponent._syncLastCoinVelocity = new Vector2(-3f, -4f);
		_components[0] = resourceSpawnComponent;
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
			ResourceSpawnComponent obj = _components[i];
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
