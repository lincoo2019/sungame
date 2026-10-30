using System;
using System.Globalization;
using System.Text;
using System.Threading;
using Godot;
using Godot.Collections;

public partial class CharacterTimerComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly CharacterTimerComponent[] _components = new CharacterTimerComponent[1000];

	private readonly CharacterTimerSyncProbeOwner[] _owners = new CharacterTimerSyncProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private CharacterTimerComponentDefinition _definition;

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
		OptimizationBatchResult optimizationBatchResult = default(OptimizationBatchResult);
		OptimizationBatchResult optimizationBatchResult2 = default(OptimizationBatchResult);
		OptimizationBatchResult optimizationBatchResult3 = default(OptimizationBatchResult);
		OptimizationBatchResult optimizationBatchResult4 = default(OptimizationBatchResult);
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
			flag = RunFunctionalContract() && RestoreBenchmarkFixture();
			optimizationBatchResult = BenchmarkPreparedRound("hostRound1", out var diagnosticEvidence);
			optimizationBatchResult2 = BenchmarkPreparedRound("hostRound2", out var diagnosticEvidence2);
			MultiPlayerManager.Instance.isHost = false;
			flag2 = RunClientAuthorityContract() && RestoreBenchmarkFixture();
			optimizationBatchResult3 = BenchmarkPreparedRound("clientRound1", out var diagnosticEvidence3);
			optimizationBatchResult4 = BenchmarkPreparedRound("clientRound2", out var diagnosticEvidence4);
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			bool flag3 = flag && num == 1000 && num2 == 1000 && flag2 && ResultPassed(optimizationBatchResult) && ResultPassed(optimizationBatchResult2) && ResultPassed(optimizationBatchResult3) && ResultPassed(optimizationBatchResult4);
			double value = Math.Max(Math.Max(optimizationBatchResult.P99Milliseconds, optimizationBatchResult2.P99Milliseconds), Math.Max(optimizationBatchResult3.P99Milliseconds, optimizationBatchResult4.P99Milliseconds));
			long value2 = optimizationBatchResult.AllocatedBytes + optimizationBatchResult2.AllocatedBytes + optimizationBatchResult3.AllocatedBytes + optimizationBatchResult4.AllocatedBytes;
			int value3 = optimizationBatchResult.Gen0Collections + optimizationBatchResult2.Gen0Collections + optimizationBatchResult3.Gen0Collections + optimizationBatchResult4.Gen0Collections;
			int value4 = optimizationBatchResult.Gen1Collections + optimizationBatchResult2.Gen1Collections + optimizationBatchResult3.Gen1Collections + optimizationBatchResult4.Gen1Collections;
			int value5 = optimizationBatchResult.Gen2Collections + optimizationBatchResult2.Gen2Collections + optimizationBatchResult3.Gen2Collections + optimizationBatchResult4.Gen2Collections;
			GD.Print($"CHARACTER_TIMER_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag3} functionalPassed={flag} authorityFunctionalPassed={flag2} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} hostRound1P99Ms={optimizationBatchResult.P99Milliseconds:F6} hostRound2P99Ms={optimizationBatchResult2.P99Milliseconds:F6} clientRound1P99Ms={optimizationBatchResult3.P99Milliseconds:F6} clientRound2P99Ms={optimizationBatchResult4.P99Milliseconds:F6} p99Ms={value:F6} allocatedBytes={value2} gen0={value3} gen1={value4} gen2={value5}");
			GD.Print(diagnosticEvidence);
			GD.Print(diagnosticEvidence2);
			GD.Print(diagnosticEvidence3);
			GD.Print(diagnosticEvidence4);
			GetTree().Quit((!flag3) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("CHARACTER_TIMER_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isMultiplayerMode = isMultiplayerMode;
			}
			if (GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
			{
				MultiPlayerManager.Instance.isHost = isHost;
			}
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new CharacterTimerComponentDefinition
		{
			ComponentTypeId = "CharacterTimerComponent",
			DefinitionId = "character_timer.sync.hotpath.runtime",
			InstanceId = "character_timer.sync.hotpath.runtime",
			WireIndex = 0,
			timerDictionary = new Dictionary { ["ready"] = 2.0 },
			timeScale = 1.0
		};
		for (int i = 0; i < 1000; i++)
		{
			CharacterTimerSyncProbeOwner characterTimerSyncProbeOwner = CreateOwner();
			AddChild(characterTimerSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			CharacterTimerComponent characterTimerComponent = new CharacterTimerComponent();
			characterTimerComponent.Bind(componentManager, characterTimerSyncProbeOwner, _definition);
			characterTimerComponent.Activate();
			_owners[i] = characterTimerSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = characterTimerComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		CharacterTimerComponent characterTimerComponent = _components[0];
		Dictionary dictionary = characterTimerComponent.SyncSerialize();
		bool flag = HasTimer(dictionary, running: false, 2.0, 0.0, 1.0);
		Dictionary dictionary2 = characterTimerComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasTimer(dictionary2, running: false, 2.0, 0.0, 1.0);
		characterTimerComponent.Run("ready", 3.5);
		characterTimerComponent.timerCurrent["ready"] = 1.25;
		characterTimerComponent.timeScale = 0.5;
		bool flag3 = HasTimer(characterTimerComponent.SyncSerialize(), running: true, 3.5, 1.25, 0.5);
		Dictionary payload = characterTimerComponent.ExportComponentSave();
		characterTimerComponent.SyncDeserialize(CreateTimerPayload(running: false, 4.0, 0.75, 2.0));
		bool flag4 = HasTimer(characterTimerComponent.SyncSerialize(), running: false, 4.0, 0.75, 2.0);
		characterTimerComponent.ImportComponentSave(CreateTimerPayload(running: true, 5.0, 1.5, 1.25), null);
		bool flag5 = HasTimer(payload, running: true, 3.5, 1.25, 0.5) && HasTimer(characterTimerComponent.SyncSerialize(), running: true, 5.0, 1.5, 1.25);
		characterTimerComponent.SetAlive(alive: false);
		bool flag6 = !characterTimerComponent.Alive && HasTimer(characterTimerComponent.SyncSerialize(), running: true, 5.0, 1.5, 1.25);
		characterTimerComponent.SetAlive(alive: true);
		if (flag && flag2 && flag3 && flag4 && flag5 && flag6 && RunLifecycleContract())
		{
			return RunNetworkEnvelopeContract();
		}
		return false;
	}

	private bool RunNetworkEnvelopeContract()
	{
		CharacterComponentSet characterComponentSet = new CharacterComponentSet();
		characterComponentSet.Components.Add(_definition);
		CharacterTimerSyncProbeOwner characterTimerSyncProbeOwner = CreateOwner();
		ComponentManager componentManager = (characterTimerSyncProbeOwner.componentManager = new ComponentManager
		{
			Name = "ComponentManager",
			ComponentSet = characterComponentSet
		});
		componentManager.AttachOwner(characterTimerSyncProbeOwner);
		AddChild(characterTimerSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		try
		{
			componentManager.InitializeResourceComponents();
			CharacterTimerComponent runtime = componentManager.GetRuntime<CharacterTimerComponent>(_definition.InstanceId);
			runtime?.Activate();
			if (runtime == null || runtime.Lifecycle != ComponentRuntimeLifecycle.Active || componentManager.ResourceComponents.Count != 1)
			{
				return false;
			}
			Dictionary dictionary = runtime.SyncSerialize();
			Dictionary dictionary2 = new Dictionary();
			Dictionary dictionary3 = NetworkCharacterStateSnapshots.BuildComponentDelta(characterTimerSyncProbeOwner, dictionary2, 701, force: false);
			Dictionary envelope;
			bool num = dictionary3.Count == 1 && TryGetEnvelope(dictionary3, "CharacterTimerComponent", out envelope) && envelope.GetValueOrDefault("_alive", false).AsBool() && HasTimer(envelope, running: false, 2.0, 0.0, 1.0) && dictionary == runtime.SyncSerialize() && !dictionary.ContainsKey("_alive");
			Variant value;
			bool flag = dictionary2.TryGetValue("701:CharacterTimerComponent", out value) && HasTimer(value.AsGodotDictionary(), running: false, 2.0, 0.0, 1.0) && !value.AsGodotDictionary().ContainsKey("_alive") && dictionary2.GetValueOrDefault("701:CharacterTimerComponent:alive", false).AsBool();
			Dictionary dictionary4 = NetworkCharacterStateSnapshots.BuildComponentDelta(characterTimerSyncProbeOwner, dictionary2, 701, force: false);
			runtime.SetAlive(alive: false);
			Dictionary dictionary5 = NetworkCharacterStateSnapshots.BuildComponentDelta(characterTimerSyncProbeOwner, dictionary2, 701, force: false);
			Dictionary envelope2;
			bool flag2 = dictionary5.Count == 1 && TryGetEnvelope(dictionary5, "CharacterTimerComponent", out envelope2) && envelope2.ContainsKey("_alive") && !envelope2["_alive"].AsBool() && HasTimer(envelope2, running: false, 2.0, 0.0, 1.0) && !dictionary.ContainsKey("_alive");
			bool flag3 = dictionary2.ContainsKey("701:CharacterTimerComponent") && dictionary2.ContainsKey("701:CharacterTimerComponent:alive") && !dictionary2["701:CharacterTimerComponent:alive"].AsBool() && !dictionary2["701:CharacterTimerComponent"].AsGodotDictionary().ContainsKey("_alive");
			Dictionary dictionary6 = NetworkCharacterStateSnapshots.BuildComponentDelta(characterTimerSyncProbeOwner, dictionary2, 701, force: false);
			return num && flag && dictionary4.Count == 0 && flag2 && flag3 && dictionary6.Count == 0;
		}
		finally
		{
			characterTimerSyncProbeOwner.Free();
		}
	}

	private static bool TryGetEnvelope(Dictionary delta, string wireKey, out Dictionary envelope)
	{
		if (delta.TryGetValue(wireKey, out var value))
		{
			envelope = value.AsGodotDictionary();
			return true;
		}
		envelope = null;
		return false;
	}

	private bool RunLifecycleContract()
	{
		CharacterTimerSyncProbeOwner characterTimerSyncProbeOwner = CreateOwner();
		AddChild(characterTimerSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		CharacterTimerComponent characterTimerComponent = new CharacterTimerComponent();
		characterTimerComponent.Bind(manager, characterTimerSyncProbeOwner, _definition);
		characterTimerComponent.Activate();
		characterTimerComponent.Run("ready", 3.0);
		characterTimerComponent.timerCurrent["ready"] = 0.75;
		characterTimerComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool num = characterTimerComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasTimer(characterTimerComponent.SyncSerialize(), running: true, 3.0, 0.75, 1.0);
		CharacterTimerSyncProbeOwner characterTimerSyncProbeOwner2 = CreateOwner();
		AddChild(characterTimerSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		characterTimerComponent.Bind(manager2, characterTimerSyncProbeOwner2, _definition);
		characterTimerComponent.Activate();
		bool flag = characterTimerComponent.IsAttached && !characterTimerComponent.IsReleased && HasTimer(characterTimerComponent.SyncSerialize(), running: true, 3.0, 0.75, 1.0);
		characterTimerComponent.Release();
		bool flag2 = characterTimerComponent.IsReleased && HasNoTimers(characterTimerComponent.SyncSerialize(), 1.0);
		characterTimerSyncProbeOwner.Free();
		characterTimerSyncProbeOwner2.Free();
		return num && flag && flag2;
	}

	private bool RestoreBenchmarkFixture()
	{
		Dictionary data = CreateTimerPayload(running: false, 2.0, 0.0, 1.0);
		for (int i = 0; i < 1000; i++)
		{
			CharacterTimerComponent characterTimerComponent = _components[i];
			characterTimerComponent.SyncDeserialize(data);
			if (characterTimerComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasTimer(characterTimerComponent.SyncSerialize(), running: false, 2.0, 0.0, 1.0))
			{
				return false;
			}
		}
		return true;
	}

	private bool RunClientAuthorityContract()
	{
		CharacterTimerComponent characterTimerComponent = _components[0];
		characterTimerComponent.SyncDeserialize(CreateTimerPayload(running: true, 6.0, 2.0, 0.75));
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return HasTimer(characterTimerComponent.SyncSerialize(), running: true, 6.0, 2.0, 0.75);
		}
		return false;
	}

	private OptimizationBatchResult BenchmarkPreparedRound(string roundId, out string diagnosticEvidence)
	{
		PrimePayloadCapacity();
		OptimizationBatchSampler.PrepareForWarmup();
		ulong processFrames = Engine.GetProcessFrames();
		ulong physicsFrames = Engine.GetPhysicsFrames();
		int managedThreadId = Thread.CurrentThread.ManagedThreadId;
		string sampleEvidence;
		OptimizationBatchResult result = Benchmark(out sampleEvidence);
		int managedThreadId2 = Thread.CurrentThread.ManagedThreadId;
		ulong processFrames2 = Engine.GetProcessFrames();
		ulong physicsFrames2 = Engine.GetPhysicsFrames();
		diagnosticEvidence = "CHARACTER_TIMER_COMPONENT_SYNC_DIAGNOSTIC " + $"round={roundId} processFrameBefore={processFrames} " + $"processFrameAfter={processFrames2} " + $"physicsFrameBefore={physicsFrames} " + $"physicsFrameAfter={physicsFrames2} " + $"threadBefore={managedThreadId} threadAfter={managedThreadId2} " + $"gen0={result.Gen0Collections} gen1={result.Gen1Collections} " + $"gen2={result.Gen2Collections} allocatedBytes={result.AllocatedBytes} " + sampleEvidence;
		return result;
	}

	private static bool ResultPassed(OptimizationBatchResult result)
	{
		if (result.AllocatedBytes == 0L && result.Gen0Collections == 0 && result.Gen1Collections == 0 && result.Gen2Collections == 0)
		{
			return result.P99Milliseconds < 0.2;
		}
		return false;
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

	private OptimizationBatchResult Benchmark(out string sampleEvidence)
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
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		sampleEvidence = FormatSampleEvidence(optimizationBatchSampler);
		return result;
	}

	private static string FormatSampleEvidence(OptimizationBatchSampler sampler)
	{
		int[] array = new int[16];
		double[] array2 = new double[16];
		System.Array.Fill(array, -1);
		int num = 0;
		int num2 = -1;
		int num3 = -1;
		int num4 = 0;
		int num5 = 0;
		int num6 = -1;
		int[] array3 = new int[4];
		for (int i = 0; i < sampler.SampleCount; i++)
		{
			double sampleMilliseconds = sampler.GetSampleMilliseconds(i);
			if (sampleMilliseconds >= 0.2)
			{
				num++;
				num2 = ((num2 < 0) ? i : num2);
				num3 = i;
				num4++;
				if (num4 > num5)
				{
					num5 = num4;
					num6 = i;
				}
				int num7 = Math.Min(array3.Length - 1, i * array3.Length / sampler.SampleCount);
				array3[num7]++;
			}
			else
			{
				num4 = 0;
			}
			for (int j = 0; j < 16; j++)
			{
				if (!(sampleMilliseconds <= array2[j]))
				{
					for (int num8 = 15; num8 > j; num8--)
					{
						array2[num8] = array2[num8 - 1];
						array[num8] = array[num8 - 1];
					}
					array2[j] = sampleMilliseconds;
					array[j] = i;
					break;
				}
			}
		}
		StringBuilder stringBuilder = new StringBuilder(512);
		stringBuilder.Append("overBudgetSamples=").Append(num).Append(" firstOverBudgetSample=")
			.Append(num2 + 1)
			.Append(" lastOverBudgetSample=")
			.Append(num3 + 1)
			.Append(" longestOverBudgetCluster=")
			.Append(num5)
			.Append(" longestClusterStart=")
			.Append(num6 - num5 + 2)
			.Append(" longestClusterEnd=")
			.Append(num6 + 1)
			.Append(" overBudgetQuartiles=")
			.Append(array3[0])
			.Append(',')
			.Append(array3[1])
			.Append(',')
			.Append(array3[2])
			.Append(',')
			.Append(array3[3])
			.Append(" slowestSamples=");
		for (int k = 0; k < 16 && array[k] >= 0; k++)
		{
			if (k > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(array[k] + 1).Append(':').Append(array2[k].ToString("F6", CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}

	private static CharacterTimerSyncProbeOwner CreateOwner()
	{
		CharacterTimerSyncProbeOwner characterTimerSyncProbeOwner = new CharacterTimerSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		characterTimerSyncProbeOwner.instance.character = characterTimerSyncProbeOwner;
		return characterTimerSyncProbeOwner;
	}

	private static Dictionary CreateTimerPayload(bool running, double waitTime, double current, double timeScale)
	{
		return new Dictionary
		{
			["timerRunning"] = new Dictionary { ["ready"] = running },
			["timerWaitTime"] = new Dictionary { ["ready"] = waitTime },
			["timerCurrent"] = new Dictionary { ["ready"] = current },
			["timeScale"] = timeScale
		};
	}

	private static bool HasTimer(Dictionary payload, bool running, double waitTime, double current, double timeScale)
	{
		int num = (payload.ContainsKey("_alive") ? 5 : 4);
		if (payload.Count != num || !payload.TryGetValue("timerRunning", out var value) || !payload.TryGetValue("timerWaitTime", out var value2) || !payload.TryGetValue("timerCurrent", out var value3) || !payload.TryGetValue("timeScale", out var value4))
		{
			return false;
		}
		Dictionary dictionary = value.AsGodotDictionary();
		Dictionary dictionary2 = value2.AsGodotDictionary();
		Dictionary dictionary3 = value3.AsGodotDictionary();
		if (dictionary.Count == 1 && dictionary2.Count == 1 && dictionary3.Count == 1 && dictionary.GetValueOrDefault("ready", !running).AsBool() == running && dictionary2.GetValueOrDefault("ready", -1.0).AsDouble() == waitTime && dictionary3.GetValueOrDefault("ready", -1.0).AsDouble() == current)
		{
			return value4.AsDouble() == timeScale;
		}
		return false;
	}

	private static bool HasNoTimers(Dictionary payload, double timeScale)
	{
		if (payload.Count == 4 && payload.GetValueOrDefault("timerRunning").AsGodotDictionary().Count == 0 && payload.GetValueOrDefault("timerWaitTime").AsGodotDictionary().Count == 0 && payload.GetValueOrDefault("timerCurrent").AsGodotDictionary().Count == 0)
		{
			return payload.GetValueOrDefault("timeScale").AsDouble() == timeScale;
		}
		return false;
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			CharacterTimerSyncProbeOwner obj = _owners[i];
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
			CharacterTimerComponent obj = _components[i];
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
