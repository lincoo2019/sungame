using System;
using Godot;
using Godot.Collections;

public partial class MagnetCoinComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly MagnetCoinComponent[] _components = new MagnetCoinComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private MagnetCoinComponentDefinition _definition;

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
			GD.Print($"MAGNET_COIN_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("MAGNET_COIN_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new MagnetCoinComponentDefinition
		{
			ComponentTypeId = "MagnetCoinComponent",
			DefinitionId = "magnet_coin.sync.hotpath.runtime",
			InstanceId = "magnet_coin.sync.hotpath.runtime",
			WireIndex = 0,
			useRegistryCoinTypes = false,
			objectList = new int[1],
			magnetTime = 5f
		};
		for (int i = 0; i < 1000; i++)
		{
			MagnetCoinSyncProbeOwner magnetCoinSyncProbeOwner = CreateOwner();
			AddChild(magnetCoinSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			MagnetCoinComponent magnetCoinComponent = new MagnetCoinComponent();
			magnetCoinComponent.Bind(componentManager, magnetCoinSyncProbeOwner, _definition);
			magnetCoinComponent.Activate();
			_owners[i] = magnetCoinSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = magnetCoinComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		MagnetCoinComponent magnetCoinComponent = _components[0];
		Dictionary dictionary = magnetCoinComponent.SyncSerialize();
		bool flag = HasTimer(dictionary, 0f);
		dictionary["_alive"] = true;
		Dictionary dictionary2 = magnetCoinComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasTimer(dictionary2, 0f);
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = magnetCoinComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasTimer(dictionary3, 0f);
		magnetCoinComponent.SyncDeserialize(new Dictionary { ["timer"] = 2f });
		bool flag4 = magnetCoinComponent.timer == 2f && HasTimer(magnetCoinComponent.SyncSerialize(), 2f);
		Dictionary dictionary4 = magnetCoinComponent.ExportComponentSave();
		bool flag5 = dictionary4.Count == 1 && dictionary4["timer"].AsSingle() == 2f;
		magnetCoinComponent.ImportComponentSave(new Dictionary { ["timer"] = 3f }, null);
		flag5 = flag5 && magnetCoinComponent.timer == 3f;
		bool flag6 = RunLifecycleContract();
		return flag && flag2 && flag3 && flag4 && flag5 && flag6;
	}

	private bool RunLifecycleContract()
	{
		MagnetCoinSyncProbeOwner magnetCoinSyncProbeOwner = CreateOwner();
		AddChild(magnetCoinSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		MagnetCoinComponent magnetCoinComponent = new MagnetCoinComponent();
		magnetCoinComponent.Bind(manager, magnetCoinSyncProbeOwner, _definition);
		magnetCoinComponent.Activate();
		magnetCoinComponent.SyncDeserialize(new Dictionary { ["timer"] = 4f });
		magnetCoinComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool num = magnetCoinComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && magnetCoinComponent.timer == 4f && HasTimer(magnetCoinComponent.SyncSerialize(), 4f);
		MagnetCoinSyncProbeOwner magnetCoinSyncProbeOwner2 = CreateOwner();
		AddChild(magnetCoinSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		magnetCoinComponent.Bind(manager2, magnetCoinSyncProbeOwner2, _definition);
		magnetCoinComponent.Activate();
		bool flag = magnetCoinComponent.IsAttached && !magnetCoinComponent.IsReleased && magnetCoinComponent.timer == 4f && HasTimer(magnetCoinComponent.SyncSerialize(), 4f);
		magnetCoinComponent.Release();
		bool flag2 = magnetCoinComponent.IsReleased && magnetCoinComponent.timer == 4f && HasTimer(magnetCoinComponent.SyncSerialize(), 4f);
		magnetCoinSyncProbeOwner.Free();
		magnetCoinSyncProbeOwner2.Free();
		return num && flag && flag2;
	}

	private bool RestoreBenchmarkFixture()
	{
		for (int i = 0; i < 1000; i++)
		{
			MagnetCoinComponent magnetCoinComponent = _components[i];
			magnetCoinComponent.timer = 1f;
			magnetCoinComponent.SetAlive(alive: true);
			if (magnetCoinComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasTimer(magnetCoinComponent.SyncSerialize(), 1f))
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

	private static MagnetCoinSyncProbeOwner CreateOwner()
	{
		MagnetCoinSyncProbeOwner magnetCoinSyncProbeOwner = new MagnetCoinSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		magnetCoinSyncProbeOwner.instance.character = magnetCoinSyncProbeOwner;
		return magnetCoinSyncProbeOwner;
	}

	private static bool HasTimer(Dictionary payload, float timer)
	{
		if (payload.Count == 1 && payload.TryGetValue("timer", out var value) && value.VariantType == Variant.Type.Float)
		{
			return value.AsSingle() == timer;
		}
		return false;
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
			MagnetCoinComponent obj = _components[i];
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
