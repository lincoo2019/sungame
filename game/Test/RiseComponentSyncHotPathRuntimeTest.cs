using System;
using Godot;
using Godot.Collections;

public partial class RiseComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly RiseComponent[] _components = new RiseComponent[1000];

	private readonly TowerDefenseZombie[] _owners = new TowerDefenseZombie[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private RiseComponentDefinition _definition;

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
			GD.Print($"RISE_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("RISE_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new RiseComponentDefinition
		{
			ComponentTypeId = "RiseComponent",
			DefinitionId = "rise.sync.hotpath.runtime",
			InstanceId = "rise.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			RiseSyncProbeOwner riseSyncProbeOwner = CreateOwner();
			AddChild(riseSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			RiseComponent riseComponent = new RiseComponent();
			riseComponent.Bind(componentManager, riseSyncProbeOwner, _definition);
			riseComponent.Activate();
			_owners[i] = riseSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = riseComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		RiseComponent riseComponent = _components[0];
		Dictionary dictionary = riseComponent.SyncSerialize();
		bool flag = HasState(dictionary, isRise: false, hasDuration: false, -1f);
		dictionary["_alive"] = true;
		Dictionary dictionary2 = riseComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasState(dictionary2, isRise: false, hasDuration: false, -1f);
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = riseComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasState(dictionary3, isRise: false, hasDuration: false, -1f);
		riseComponent.SyncDeserialize(new Dictionary
		{
			["isRise"] = true,
			["duration"] = 0.75f
		});
		bool flag4 = riseComponent.parent.isRise && riseComponent._syncDeserializing && HasState(riseComponent.SyncSerialize(), isRise: true, hasDuration: true, 0.75f);
		Dictionary dictionary4 = riseComponent.ExportComponentSave();
		bool flag5 = dictionary4.Count == 1 && dictionary4["isRise"].AsBool();
		riseComponent.ImportComponentSave(new Dictionary { ["isRise"] = false }, null);
		flag5 = flag5 && !riseComponent.parent.isRise;
		bool flag6 = RunLifecycleContract();
		return flag && flag2 && flag3 && flag4 && flag5 && flag6;
	}

	private bool RunLifecycleContract()
	{
		RiseSyncProbeOwner riseSyncProbeOwner = CreateOwner();
		AddChild(riseSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		RiseComponent riseComponent = new RiseComponent();
		riseComponent.Bind(manager, riseSyncProbeOwner, _definition);
		riseComponent.Activate();
		riseComponent.SyncDeserialize(new Dictionary
		{
			["isRise"] = true,
			["duration"] = 0.5f
		});
		riseComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool num = riseComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && riseComponent._syncDeserializing && riseComponent._syncDuration == 0.5f && HasState(riseComponent.SyncSerialize(), isRise: false, hasDuration: true, 0.5f);
		RiseSyncProbeOwner riseSyncProbeOwner2 = CreateOwner();
		AddChild(riseSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		riseComponent.Bind(manager2, riseSyncProbeOwner2, _definition);
		riseComponent.Activate();
		bool flag = riseComponent.IsAttached && !riseComponent.IsReleased && riseComponent._syncDeserializing && riseComponent._syncDuration == 0.5f && HasState(riseComponent.SyncSerialize(), isRise: false, hasDuration: true, 0.5f);
		riseComponent.Release();
		bool flag2 = riseComponent.IsReleased && HasState(riseComponent.SyncSerialize(), isRise: false, hasDuration: true, 0.5f);
		riseSyncProbeOwner.Free();
		riseSyncProbeOwner2.Free();
		return num && flag && flag2;
	}

	private bool RestoreBenchmarkFixture()
	{
		for (int i = 0; i < 1000; i++)
		{
			RiseComponent riseComponent = _components[i];
			_owners[i].isRise = false;
			riseComponent._syncDuration = -1f;
			riseComponent._syncDeserializing = false;
			riseComponent.SetAlive(alive: true);
			if (riseComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasState(riseComponent.SyncSerialize(), isRise: false, hasDuration: false, -1f))
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

	private static RiseSyncProbeOwner CreateOwner()
	{
		RiseSyncProbeOwner riseSyncProbeOwner = new RiseSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		riseSyncProbeOwner.instance.character = riseSyncProbeOwner;
		return riseSyncProbeOwner;
	}

	private static bool HasState(Dictionary payload, bool isRise, bool hasDuration, float duration)
	{
		int num = ((!hasDuration) ? 1 : 2);
		if (payload.Count != num || !payload.TryGetValue("isRise", out var value) || value.VariantType != Variant.Type.Bool || value.AsBool() != isRise)
		{
			return false;
		}
		if (!hasDuration)
		{
			return !payload.ContainsKey("duration");
		}
		if (payload.TryGetValue("duration", out var value2) && value2.VariantType == Variant.Type.Float)
		{
			return value2.AsSingle() == duration;
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
			RiseComponent obj = _components[i];
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
