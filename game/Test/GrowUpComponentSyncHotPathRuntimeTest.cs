using System;
using Godot;
using Godot.Collections;

public partial class GrowUpComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly GrowUpComponent[] _components = new GrowUpComponent[1000];

	private readonly GrowUpSyncProbeOwner[] _owners = new GrowUpSyncProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private GrowUpComponentDefinition _definition;

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
			GD.Print($"GROW_UP_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("GROW_UP_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new GrowUpComponentDefinition
		{
			ComponentTypeId = "GrowUpComponent",
			DefinitionId = "grow_up.sync.hotpath.runtime",
			InstanceId = "grow_up.sync.hotpath.runtime",
			WireIndex = 0,
			growUpTime = new Array<float> { 60f },
			growUpSize = new Array<float> { 1.25f },
			growTweenDuration = 0.0,
			instantGrowInIZM = false
		};
		for (int i = 0; i < 1000; i++)
		{
			GrowUpSyncProbeOwner growUpSyncProbeOwner = CreateOwner();
			AddChild(growUpSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			GrowUpComponent growUpComponent = new GrowUpComponent();
			growUpComponent.Bind(componentManager, growUpSyncProbeOwner, _definition);
			growUpComponent.Activate();
			_owners[i] = growUpSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = growUpComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		GrowUpComponent growUpComponent = _components[0];
		Dictionary dictionary = growUpComponent.SyncSerialize();
		bool flag = HasState(dictionary, 0f, 0);
		dictionary["_alive"] = true;
		Dictionary dictionary2 = growUpComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasState(dictionary2, 0f, 0);
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = growUpComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasState(dictionary3, 0f, 0);
		growUpComponent.SyncDeserialize(new Dictionary
		{
			["timer"] = 2f,
			["growUpReach"] = 1
		});
		bool flag4 = growUpComponent.timer == 2f && growUpComponent.growUpReach == 1 && _owners[0].transformPoint.Scale == Vector2.One * 1.25f && HasState(growUpComponent.SyncSerialize(), 2f, 1);
		bool flag5 = RunLifecycleContract();
		return flag && flag2 && flag3 && flag4 && flag5;
	}

	private bool RunLifecycleContract()
	{
		GrowUpSyncProbeOwner growUpSyncProbeOwner = CreateOwner();
		AddChild(growUpSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		GrowUpComponent growUpComponent = new GrowUpComponent();
		growUpComponent.Bind(manager, growUpSyncProbeOwner, _definition);
		growUpComponent.Activate();
		growUpComponent.SyncDeserialize(new Dictionary
		{
			["timer"] = 3f,
			["growUpReach"] = 1
		});
		Dictionary dictionary = growUpComponent.ExportComponentSave();
		growUpComponent.ImportComponentSave(dictionary, null);
		bool num = HasState(dictionary, 3f, 1) && growUpComponent.timer == 3f && growUpComponent.growUpReach == 1;
		growUpComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag = growUpComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasState(growUpComponent.SyncSerialize(), 3f, 1);
		GrowUpSyncProbeOwner growUpSyncProbeOwner2 = CreateOwner();
		AddChild(growUpSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		growUpComponent.Bind(manager2, growUpSyncProbeOwner2, _definition);
		growUpComponent.Activate();
		bool flag2 = growUpComponent.IsAttached && !growUpComponent.IsReleased && HasState(growUpComponent.SyncSerialize(), 3f, 1);
		growUpComponent.Release();
		bool flag3 = growUpComponent.IsReleased && HasState(growUpComponent.SyncSerialize(), 3f, 1);
		growUpSyncProbeOwner.Free();
		growUpSyncProbeOwner2.Free();
		return num && flag && flag2 && flag3;
	}

	private bool RestoreBenchmarkFixture()
	{
		for (int i = 0; i < 1000; i++)
		{
			GrowUpComponent growUpComponent = _components[i];
			growUpComponent.timer = 1f;
			growUpComponent.growUpReach = 0;
			growUpComponent.SetAlive(alive: true);
			if (!growUpComponent.IsAttached || growUpComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasState(growUpComponent.SyncSerialize(), 1f, 0))
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

	private static GrowUpSyncProbeOwner CreateOwner()
	{
		GrowUpSyncProbeOwner growUpSyncProbeOwner = new GrowUpSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		growUpSyncProbeOwner.instance.character = growUpSyncProbeOwner;
		growUpSyncProbeOwner.transformPoint = new Marker2D();
		growUpSyncProbeOwner.AddChild(growUpSyncProbeOwner.transformPoint, forceReadableName: false, InternalMode.Disabled);
		return growUpSyncProbeOwner;
	}

	private static bool HasState(Dictionary payload, float timer, int reach)
	{
		if (payload.Count == 2 && payload.TryGetValue("timer", out var value) && value.VariantType == Variant.Type.Float && value.AsSingle() == timer && payload.TryGetValue("growUpReach", out var value2) && value2.VariantType == Variant.Type.Int)
		{
			return value2.AsInt32() == reach;
		}
		return false;
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			GrowUpSyncProbeOwner obj = _owners[i];
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
			GrowUpComponent obj = _components[i];
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
