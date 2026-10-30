using System;
using Godot;
using Godot.Collections;

public partial class WaterEnvironmentComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly WaterEnvironmentComponent[] _components = new WaterEnvironmentComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private WaterEnvironmentComponentDefinition _definition;

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
			PrimePayloadCapacity();
			OptimizationBatchSampler.PrepareForWarmup();
			optimizationBatchResult = Benchmark();
			bool flag2 = flag && optimizationBatchResult.AllocatedBytes == 0L && optimizationBatchResult.Gen0Collections == 0 && optimizationBatchResult.Gen1Collections == 0 && optimizationBatchResult.Gen2Collections == 0 && optimizationBatchResult.P99Milliseconds < 0.2;
			GD.Print($"WATER_ENVIRONMENT_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("WATER_ENVIRONMENT_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new WaterEnvironmentComponentDefinition
		{
			ComponentTypeId = "WaterEnvironmentComponent",
			DefinitionId = "water-environment.sync.hotpath.runtime",
			InstanceId = "water-environment.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = new TowerDefenseCharacter
			{
				camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
				instance = new TowerDefenseCharacterInstance(),
				syncId = i + 1,
				inGame = false
			};
			towerDefenseCharacter.instance.character = towerDefenseCharacter;
			towerDefenseCharacter.spriteGroup = new Node2D
			{
				Name = "WaterSyncSpriteGroup"
			};
			towerDefenseCharacter.AddChild(towerDefenseCharacter.spriteGroup, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			WaterEnvironmentComponent waterEnvironmentComponent = new WaterEnvironmentComponent();
			waterEnvironmentComponent.Bind(componentManager, towerDefenseCharacter, _definition);
			waterEnvironmentComponent.Activate();
			waterEnvironmentComponent.SetInWater(value: true);
			towerDefenseCharacter.groundHeight = -35.0;
			_owners[i] = towerDefenseCharacter;
			_managers[i] = componentManager;
			_components[i] = waterEnvironmentComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		WaterEnvironmentComponent waterEnvironmentComponent = _components[0];
		Dictionary dictionary = waterEnvironmentComponent.SyncSerialize();
		bool flag = dictionary.Count == 2 && dictionary.ContainsKey("water") && dictionary["water"].AsBool() && dictionary.ContainsKey("height") && Math.Abs(dictionary["height"].AsDouble() + 35.0) < 0.0001;
		dictionary["foreign"] = true;
		Dictionary dictionary2 = waterEnvironmentComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && dictionary2.Count == 2 && !dictionary2.ContainsKey("foreign");
		waterEnvironmentComponent.SyncDeserialize(new Dictionary
		{
			["water"] = false,
			["height"] = 0.0
		});
		waterEnvironmentComponent.SetInWater(value: false);
		waterEnvironmentComponent.parent.groundHeight = 0.0;
		Dictionary dictionary3 = waterEnvironmentComponent.SyncSerialize();
		bool flag3 = !dictionary3["water"].AsBool() && Math.Abs(dictionary3["height"].AsDouble()) < 0.0001;
		bool flag4 = waterEnvironmentComponent.ExportComponentSave().Count == 0;
		waterEnvironmentComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag5 = waterEnvironmentComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && waterEnvironmentComponent.SyncSerialize().Count == 0;
		ComponentManager manager = new ComponentManager();
		TowerDefenseCharacter towerDefenseCharacter = new TowerDefenseCharacter
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			syncId = 8001,
			inGame = false
		};
		towerDefenseCharacter.instance.character = towerDefenseCharacter;
		waterEnvironmentComponent.Bind(manager, towerDefenseCharacter, _definition);
		waterEnvironmentComponent.Activate();
		bool flag6 = waterEnvironmentComponent.IsAttached && !waterEnvironmentComponent.IsReleased && waterEnvironmentComponent.SyncSerialize().Count == 2;
		waterEnvironmentComponent.Release();
		bool isReleased = waterEnvironmentComponent.IsReleased;
		towerDefenseCharacter.Free();
		bool flag7 = flag && flag2 && flag3 && flag4 && flag5 && flag6 && isReleased;
		GD.Print($"WATER_ENVIRONMENT_SYNC_FUNCTIONAL_DIAGNOSTIC keysPresent={flag} reusedAndCleaned={flag2} changedValues={flag3} saveIsTransient={flag4} detachedCleared={flag5} rebound={flag6} released={isReleased} passed={flag7}");
		return flag7;
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
