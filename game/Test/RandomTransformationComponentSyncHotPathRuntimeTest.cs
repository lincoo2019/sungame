using System;
using Godot;
using Godot.Collections;

public partial class RandomTransformationComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly RandomTransformationComponent[] _components = new RandomTransformationComponent[1000];

	private readonly RandomTransformationSyncProbeOwner[] _owners = new RandomTransformationSyncProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private RandomTransformationComponentDefinition _definition;

	public override async void _Ready()
	{
		for (int frame = 0; frame < 30; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		Run();
	}

	private void Run()
	{
		bool flag = false;
		bool flag2 = false;
		OptimizationBatchResult[] array = new OptimizationBatchResult[14];
		bool isMultiplayerMode = Global.Instance?.isMultiplayerMode ?? false;
		bool isHost = MultiPlayerManager.Instance?.isHost ?? false;
		try
		{
			if (!GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
			{
				throw new InvalidOperationException("Multiplayer authority autoloads are unavailable.");
			}
			Global.Instance.isMultiplayerMode = true;
			MultiPlayerManager.Instance.isHost = true;
			CreateWorkload();
			flag = RunFunctionalContract();
			bool flag3 = BenchmarkStateTwice(new Array<string>(), array, 0);
			bool flag4 = BenchmarkStateTwice(new Array<string> { "pea" }, array, 2);
			bool flag5 = BenchmarkStateTwice(new Array<string> { "pea", "sun", "star" }, array, 4);
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			ReleaseWorkload();
			MultiPlayerManager.Instance.isHost = false;
			CreateWorkload();
			flag2 = RunClientPendingAuthorityContract();
			array[6] = BenchmarkPreparedState();
			array[7] = BenchmarkPreparedState();
			bool flag6 = BenchmarkStateTwice(new Array<string>(), array, 8);
			bool flag7 = BenchmarkStateTwice(new Array<string> { "pea" }, array, 10);
			bool flag8 = BenchmarkStateTwice(new Array<string> { "pea", "sun", "star" }, array, 12);
			int num3 = CountTreeOwners();
			int num4 = CountActiveInstances();
			bool flag9 = true;
			double num5 = 0.0;
			long num6 = 0L;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			for (int i = 0; i < array.Length; i++)
			{
				flag9 &= ResultPassed(array[i]);
				num5 = Math.Max(num5, array[i].P99Milliseconds);
				num6 += array[i].AllocatedBytes;
				num7 += array[i].Gen0Collections;
				num8 += array[i].Gen1Collections;
				num9 += array[i].Gen2Collections;
			}
			bool flag10 = flag && flag2 && num == 1000 && num2 == 1000 && num3 == 1000 && num4 == 1000 && flag3 && flag4 && flag5 && flag6 && flag7 && flag8 && flag9;
			GD.Print($"RANDOM_TRANSFORMATION_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag10} functionalPassed={flag} authorityFunctionalPassed={flag2} instances={1000} hostTreeOwners={num} hostActiveInstances={num2} clientTreeOwners={num3} clientActiveInstances={num4} warmupSamples={240} measuredSamples={1200} hostEmptyRound1P99Ms={array[0].P99Milliseconds:F6} hostEmptyRound2P99Ms={array[1].P99Milliseconds:F6} hostSingleRound1P99Ms={array[2].P99Milliseconds:F6} hostSingleRound2P99Ms={array[3].P99Milliseconds:F6} hostMultiRound1P99Ms={array[4].P99Milliseconds:F6} hostMultiRound2P99Ms={array[5].P99Milliseconds:F6} clientPendingRound1P99Ms={array[6].P99Milliseconds:F6} clientPendingRound2P99Ms={array[7].P99Milliseconds:F6} clientEmptyRound1P99Ms={array[8].P99Milliseconds:F6} clientEmptyRound2P99Ms={array[9].P99Milliseconds:F6} clientSingleRound1P99Ms={array[10].P99Milliseconds:F6} clientSingleRound2P99Ms={array[11].P99Milliseconds:F6} clientMultiRound1P99Ms={array[12].P99Milliseconds:F6} clientMultiRound2P99Ms={array[13].P99Milliseconds:F6} p99Ms={num5:F6} allocatedBytes={num6} gen0={num7} gen1={num8} gen2={num9}");
			GetTree().Quit((!flag10) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("RANDOM_TRANSFORMATION_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isMultiplayerMode = isMultiplayerMode;
			}
			if (GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
			{
				MultiPlayerManager.Instance.isHost = isHost;
			}
		}
	}

	private void CreateWorkload()
	{
		_definition = new RandomTransformationComponentDefinition
		{
			ComponentTypeId = "RandomTransformationComponent",
			DefinitionId = "random_transformation.sync.hotpath.runtime",
			InstanceId = "random_transformation.sync.hotpath.runtime",
			WireIndex = 0,
			includePacketNameList = new Array<string> { "pea", "sun" }
		};
		for (int i = 0; i < 1000; i++)
		{
			RandomTransformationSyncProbeOwner randomTransformationSyncProbeOwner = CreateOwner();
			AddChild(randomTransformationSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			RandomTransformationComponent randomTransformationComponent = new RandomTransformationComponent();
			randomTransformationComponent.Bind(componentManager, randomTransformationSyncProbeOwner, _definition);
			randomTransformationComponent.Activate();
			_owners[i] = randomTransformationSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = randomTransformationComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		RandomTransformationComponent randomTransformationComponent = _components[0];
		Dictionary dictionary = randomTransformationComponent.SyncSerialize();
		bool flag = HasNames(dictionary, "pea", "sun");
		dictionary["_alive"] = true;
		Dictionary dictionary2 = randomTransformationComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasNames(dictionary2, "pea", "sun");
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = randomTransformationComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasNames(dictionary3, "pea", "sun");
		randomTransformationComponent.packetList.Add("star");
		bool flag4 = HasNames(randomTransformationComponent.SyncSerialize(), "pea", "sun", "star");
		randomTransformationComponent.SyncDeserialize(new Dictionary { ["packetList"] = new Array<string>
		{
			"moon",
			"moon",
			string.Empty
		} });
		bool flag5 = HasNames(randomTransformationComponent.SyncSerialize(), "moon");
		Dictionary payload = randomTransformationComponent.ExportComponentSave();
		randomTransformationComponent.ImportComponentSave(new Dictionary { ["packetList"] = new Array<string> { "pea", "star" } }, null);
		bool flag6 = HasNames(payload, "moon") && HasNames(randomTransformationComponent.SyncSerialize(), "pea", "star");
		randomTransformationComponent.SetAlive(alive: false);
		bool flag7 = !randomTransformationComponent.Alive && HasNames(randomTransformationComponent.SyncSerialize(), "pea", "star");
		randomTransformationComponent.SetAlive(alive: true);
		if (flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7)
		{
			return RunLifecycleContract();
		}
		return false;
	}

	private bool RunLifecycleContract()
	{
		RandomTransformationSyncProbeOwner randomTransformationSyncProbeOwner = CreateOwner();
		AddChild(randomTransformationSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		RandomTransformationComponent randomTransformationComponent = new RandomTransformationComponent();
		randomTransformationComponent.Bind(manager, randomTransformationSyncProbeOwner, _definition);
		randomTransformationComponent.Activate();
		randomTransformationComponent.SyncDeserialize(new Dictionary { ["packetList"] = new Array<string> { "moon" } });
		randomTransformationComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag = randomTransformationComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasNames(randomTransformationComponent.SyncSerialize(), "moon");
		RandomTransformationSyncProbeOwner randomTransformationSyncProbeOwner2 = CreateOwner();
		AddChild(randomTransformationSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		randomTransformationComponent.Bind(manager2, randomTransformationSyncProbeOwner2, _definition);
		randomTransformationComponent.Activate();
		bool flag2 = randomTransformationComponent.IsAttached && !randomTransformationComponent.IsReleased && HasNames(randomTransformationComponent.SyncSerialize(), "moon");
		randomTransformationComponent.Release();
		bool flag3 = randomTransformationComponent.IsReleased && HasNames(randomTransformationComponent.SyncSerialize());
		randomTransformationSyncProbeOwner.Free();
		randomTransformationSyncProbeOwner2.Free();
		return flag && flag2 && flag3;
	}

	private bool RestoreBenchmarkFixture(Array<string> names)
	{
		Dictionary data = new Dictionary { ["packetList"] = names };
		string[] expected = ToManagedNames(names);
		for (int i = 0; i < 1000; i++)
		{
			RandomTransformationComponent randomTransformationComponent = _components[i];
			randomTransformationComponent.SyncDeserialize(data);
			if (randomTransformationComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasNames(randomTransformationComponent.SyncSerialize(), expected))
			{
				return false;
			}
		}
		return true;
	}

	private bool BenchmarkStateTwice(Array<string> names, OptimizationBatchResult[] results, int resultOffset)
	{
		if (!RestoreBenchmarkFixture(names))
		{
			return false;
		}
		results[resultOffset] = BenchmarkPreparedState();
		results[resultOffset + 1] = BenchmarkPreparedState();
		return true;
	}

	private bool RunClientPendingAuthorityContract()
	{
		RandomTransformationSyncProbeOwner randomTransformationSyncProbeOwner = CreateOwner();
		AddChild(randomTransformationSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		RandomTransformationComponent randomTransformationComponent = new RandomTransformationComponent();
		randomTransformationComponent.Bind(manager, randomTransformationSyncProbeOwner, _definition);
		randomTransformationComponent.Activate();
		Dictionary dictionary = randomTransformationComponent.SyncSerialize();
		bool flag = Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && HasNames(dictionary) && randomTransformationComponent.GetRandomPacketName() == string.Empty && randomTransformationComponent.includePacketNameList.Count == 2;
		dictionary["_alive"] = true;
		bool flag2 = dictionary == randomTransformationComponent.SyncSerialize() && !dictionary.ContainsKey("_alive") && HasNames(dictionary);
		randomTransformationComponent.SetAlive(alive: false);
		bool flag3 = !randomTransformationComponent.Alive && HasNames(randomTransformationComponent.SyncSerialize());
		randomTransformationComponent.SetAlive(alive: true);
		randomTransformationComponent.SyncDeserialize(new Dictionary { ["packetList"] = new Array<string> { "moon" } });
		bool flag4 = HasNames(randomTransformationComponent.SyncSerialize(), "moon");
		randomTransformationComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool flag5 = randomTransformationComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasNames(randomTransformationComponent.SyncSerialize(), "moon");
		RandomTransformationSyncProbeOwner randomTransformationSyncProbeOwner2 = CreateOwner();
		AddChild(randomTransformationSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		randomTransformationComponent.Bind(manager2, randomTransformationSyncProbeOwner2, _definition);
		randomTransformationComponent.Activate();
		bool flag6 = randomTransformationComponent.IsAttached && HasNames(randomTransformationComponent.SyncSerialize(), "moon");
		randomTransformationComponent.Release();
		bool flag7 = randomTransformationComponent.IsReleased && HasNames(randomTransformationComponent.SyncSerialize());
		randomTransformationSyncProbeOwner.Free();
		randomTransformationSyncProbeOwner2.Free();
		return flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7;
	}

	private OptimizationBatchResult BenchmarkPreparedState()
	{
		PrimePayloadCapacity();
		OptimizationBatchSampler.PrepareForWarmup();
		return Benchmark();
	}

	private static bool ResultPassed(OptimizationBatchResult result)
	{
		if (result.AllocatedBytes == 0L && result.Gen0Collections == 0 && result.Gen1Collections == 0 && result.Gen2Collections == 0)
		{
			return result.P99Milliseconds < 0.2;
		}
		return false;
	}

	private static string[] ToManagedNames(Array<string> names)
	{
		string[] array = new string[names.Count];
		for (int i = 0; i < names.Count; i++)
		{
			array[i] = names[i];
		}
		return array;
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

	private static RandomTransformationSyncProbeOwner CreateOwner()
	{
		RandomTransformationSyncProbeOwner randomTransformationSyncProbeOwner = new RandomTransformationSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		randomTransformationSyncProbeOwner.instance.character = randomTransformationSyncProbeOwner;
		return randomTransformationSyncProbeOwner;
	}

	private static bool HasNames(Dictionary payload, params string[] expected)
	{
		if (payload.Count != 1 || !payload.TryGetValue("packetList", out var value))
		{
			return false;
		}
		Array<string> array = value.AsGodotArray<string>();
		if (array.Count != expected.Length)
		{
			return false;
		}
		for (int i = 0; i < expected.Length; i++)
		{
			if (array[i] != expected[i])
			{
				return false;
			}
		}
		return true;
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			RandomTransformationSyncProbeOwner obj = _owners[i];
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
			RandomTransformationComponent obj = _components[i];
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
