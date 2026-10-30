using System;
using Godot;
using Godot.Collections;

public partial class BlowBackComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly BlowBackComponent[] _components = new BlowBackComponent[1000];

	private readonly TowerDefenseZombie[] _owners = new TowerDefenseZombie[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private BlowBackComponentDefinition _definition;

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
			GD.Print($"BLOW_BACK_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("BLOW_BACK_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new BlowBackComponentDefinition
		{
			ComponentTypeId = "BlowBackComponent",
			DefinitionId = "blow_back.sync.hotpath.runtime",
			InstanceId = "blow_back.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			BlowBackSyncProbeOwner blowBackSyncProbeOwner = CreateOwner();
			AddChild(blowBackSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			BlowBackComponent blowBackComponent = new BlowBackComponent();
			blowBackComponent.Bind(componentManager, blowBackSyncProbeOwner, _definition);
			blowBackComponent.Activate();
			_owners[i] = blowBackSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = blowBackComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		BlowBackComponent blowBackComponent = _components[0];
		Dictionary dictionary = blowBackComponent.SyncSerialize();
		bool flag = HasState(dictionary, active: false, 0.0, 0);
		dictionary["_alive"] = true;
		Dictionary dictionary2 = blowBackComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasState(dictionary2, active: false, 0.0, 0);
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = blowBackComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasState(dictionary3, active: false, 0.0, 0);
		blowBackComponent.SyncDeserialize(new Dictionary
		{
			["blowBack"] = true,
			["blowBackNum"] = 2.5,
			["blowBackTweens"] = 2
		});
		bool flag4 = HasState(blowBackComponent.SyncSerialize(), active: true, 2.5, 2);
		bool flag5 = blowBackComponent.ExportComponentSave().Count == 0;
		blowBackComponent.SetAlive(alive: false);
		bool flag6 = HasState(blowBackComponent.SyncSerialize(), active: false, 0.0, 0);
		blowBackComponent.SetAlive(alive: true);
		bool flag7 = RunLifecycleContract();
		return flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7;
	}

	private bool RunLifecycleContract()
	{
		BlowBackSyncProbeOwner blowBackSyncProbeOwner = CreateOwner();
		AddChild(blowBackSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		BlowBackComponent blowBackComponent = new BlowBackComponent();
		blowBackComponent.Bind(manager, blowBackSyncProbeOwner, _definition);
		blowBackComponent.Activate();
		blowBackComponent.SyncDeserialize(new Dictionary
		{
			["blowBack"] = true,
			["blowBackNum"] = 1.25,
			["blowBackTweens"] = 1
		});
		blowBackComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool num = blowBackComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasState(blowBackComponent.SyncSerialize(), active: false, 0.0, 0);
		BlowBackSyncProbeOwner blowBackSyncProbeOwner2 = CreateOwner();
		AddChild(blowBackSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		blowBackComponent.Bind(manager2, blowBackSyncProbeOwner2, _definition);
		blowBackComponent.Activate();
		bool flag = blowBackComponent.IsAttached && !blowBackComponent.IsReleased && HasState(blowBackComponent.SyncSerialize(), active: false, 0.0, 0);
		blowBackComponent.Release();
		bool flag2 = blowBackComponent.IsReleased && HasState(blowBackComponent.SyncSerialize(), active: false, 0.0, 0);
		blowBackSyncProbeOwner.Free();
		blowBackSyncProbeOwner2.Free();
		return num && flag && flag2;
	}

	private bool RestoreBenchmarkFixture()
	{
		Dictionary data = new Dictionary
		{
			["blowBack"] = false,
			["blowBackNum"] = 0.0,
			["blowBackTweens"] = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			BlowBackComponent blowBackComponent = _components[i];
			blowBackComponent.SetAlive(alive: true);
			blowBackComponent.SyncDeserialize(data);
			if (blowBackComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasState(blowBackComponent.SyncSerialize(), active: false, 0.0, 0))
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

	private static BlowBackSyncProbeOwner CreateOwner()
	{
		BlowBackSyncProbeOwner blowBackSyncProbeOwner = new BlowBackSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		blowBackSyncProbeOwner.instance.character = blowBackSyncProbeOwner;
		return blowBackSyncProbeOwner;
	}

	private static bool HasState(Dictionary payload, bool active, double amount, int leases)
	{
		if (payload.Count == 3 && payload.TryGetValue("blowBack", out var value) && value.VariantType == Variant.Type.Bool && value.AsBool() == active && payload.TryGetValue("blowBackNum", out var value2) && value2.VariantType == Variant.Type.Float && value2.AsDouble() == amount && payload.TryGetValue("blowBackTweens", out var value3) && value3.VariantType == Variant.Type.Int)
		{
			return value3.AsInt32() == leases;
		}
		return false;
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
			BlowBackComponent obj = _components[i];
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
