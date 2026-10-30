using System;
using System.Collections.Generic;
using Godot;

public sealed partial class StateMachineIdleDispatchPerformanceRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSampleCount = 240;

	private const int SampleCount = 1200;

	private const double TickDelta = 0.0001;

	private const double LongDelaySeconds = 1000.0;

	private static readonly StringName DelayEvent = new StringName("Delay");

	private static readonly StringName GoSilentEvent = new StringName("GoSilent");

	private readonly List<string> _failures = new List<string>();

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		if (!TryCompileProgram(CreateDefinition("probe.optimization.state_machine.idle", StateMachineProcessFlags.None, 1000.0), out var program) || !TryCompileProgram(CreateDefinition("probe.optimization.state_machine.pending", StateMachineProcessFlags.None, 1000.0), out var program2))
		{
			FinishEarly();
			return;
		}
		StateMachineRuntime[] runtimes = CreateRuntimes(program, StateMachineDelayClock.Process, schedulePending: false);
		StateMachineRuntime[] runtimes2 = CreateRuntimes(program, StateMachineDelayClock.Physics, schedulePending: false);
		StateMachineRuntime[] runtimes3 = CreateRuntimes(program2, StateMachineDelayClock.Process, schedulePending: true);
		StateMachineRuntime[] runtimes4 = CreateRuntimes(program2, StateMachineDelayClock.Physics, schedulePending: true);
		long warmupTicks;
		OptimizationBatchResult result = Measure(runtimes, physics: false, out warmupTicks);
		long warmupTicks2;
		OptimizationBatchResult result2 = Measure(runtimes2, physics: true, out warmupTicks2);
		long warmupTicks3;
		OptimizationBatchResult result3 = Measure(runtimes3, physics: false, out warmupTicks3);
		long warmupTicks4;
		OptimizationBatchResult result4 = Measure(runtimes4, physics: true, out warmupTicks4);
		bool flag = VerifyRuntimes(runtimes, expectPending: false, 0.0);
		bool flag2 = VerifyRuntimes(runtimes2, expectPending: false, 0.0);
		double expectedRemaining = 999.856;
		bool flag3 = VerifyRuntimes(runtimes3, expectPending: true, expectedRemaining);
		bool flag4 = VerifyRuntimes(runtimes4, expectPending: true, expectedRemaining);
		bool flag5 = RunCallbackSchedulesDelaySameTick(physics: false);
		bool flag6 = RunCallbackSchedulesDelaySameTick(physics: true);
		bool flag7 = RunIdleReentryRejected(physics: false);
		bool flag8 = RunIdleReentryRejected(physics: true);
		Check(flag, "Idle process runtimes changed state.");
		Check(flag2, "Idle physics runtimes changed state.");
		Check(flag3, "Process-clock pending delays did not remain stable.");
		Check(flag4, "Physics-clock pending delays did not remain stable.");
		Check(flag5, "A process callback-created delay did not advance in the same tick.");
		Check(flag6, "A physics callback-created delay did not advance in the same tick.");
		Check(flag7, "Process re-entry did not throw after switching to an idle state.");
		Check(flag8, "Physics re-entry did not throw after switching to an idle state.");
		Check(warmupTicks >= 0 && warmupTicks2 >= 0 && warmupTicks3 >= 0 && warmupTicks4 >= 0, "Warmup timing overflowed.");
		bool functionalPassed = _failures.Count == 0;
		bool flag9 = PrintResult("idle_without_process_callbacks", "process", in result, functionalPassed);
		bool flag10 = PrintResult("idle_without_physics_callbacks", "physics", in result2, functionalPassed);
		bool flag11 = PrintResult("pending_process_delay", "process", in result3, functionalPassed);
		bool flag12 = PrintResult("pending_physics_delay", "physics", in result4, functionalPassed);
		GD.Print("STATE_MACHINE_IDLE_DISPATCH_SEMANTICS " + $"idleProcessStable={flag} " + $"idlePhysicsStable={flag2} " + $"pendingProcessStable={flag3} " + $"pendingPhysicsStable={flag4} " + "callbackScheduledProcessDelaySameTick=" + $"{flag5} " + "callbackScheduledPhysicsDelaySameTick=" + $"{flag6} " + $"processIdleReentryRejected={flag7} " + $"physicsIdleReentryRejected={flag8} " + $"failures={_failures.Count}");
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.PrintErr(_failures[i]);
		}
		DisposeRuntimes(runtimes);
		DisposeRuntimes(runtimes2);
		DisposeRuntimes(runtimes3);
		DisposeRuntimes(runtimes4);
		bool flag13 = flag9 && flag10 && flag11 && flag12;
		GetTree().Quit((!flag13) ? 2 : 0);
	}

	private OptimizationBatchResult Measure(StateMachineRuntime[] runtimes, bool physics, out long warmupTicks)
	{
		OptimizationBatchSampler.PrepareForWarmup();
		warmupTicks = 0L;
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			TickBatch(runtimes, physics);
			warmupTicks += OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			TickBatch(runtimes, physics);
			optimizationBatchSampler.EndSample(startTicks2);
		}
		return optimizationBatchSampler.Complete();
	}

	private static void TickBatch(StateMachineRuntime[] runtimes, bool physics)
	{
		if (physics)
		{
			for (int i = 0; i < runtimes.Length; i++)
			{
				runtimes[i].TickPhysics(0.0001);
			}
		}
		else
		{
			for (int j = 0; j < runtimes.Length; j++)
			{
				runtimes[j].TickProcess(0.0001);
			}
		}
	}

	private StateMachineRuntime[] CreateRuntimes(StateMachineProgram program, StateMachineDelayClock delayClock, bool schedulePending)
	{
		StateMachineRuntime[] array = new StateMachineRuntime[1000];
		for (int i = 0; i < array.Length; i++)
		{
			StateMachineRuntime stateMachineRuntime = new StateMachineRuntime();
			stateMachineRuntime.Initialize(program, delayClock);
			Check(stateMachineRuntime.EnterInitialState(), $"Runtime {i} could not enter its initial state.");
			if (schedulePending)
			{
				Check(stateMachineRuntime.SendEvent(DelayEvent), $"Runtime {i} could not schedule its pending delay.");
				Check(stateMachineRuntime.HasPendingTransition, $"Runtime {i} did not retain its pending delay.");
			}
			array[i] = stateMachineRuntime;
		}
		return array;
	}

	private static bool VerifyRuntimes(StateMachineRuntime[] runtimes, bool expectPending, double expectedRemaining)
	{
		foreach (StateMachineRuntime stateMachineRuntime in runtimes)
		{
			if (!stateMachineRuntime.IsActive("Idle") || stateMachineRuntime.IsActive("Done") || stateMachineRuntime.HasPendingTransition != expectPending)
			{
				return false;
			}
			if (expectPending && Math.Abs(stateMachineRuntime.PendingDelayRemaining - expectedRemaining) > 0.001)
			{
				return false;
			}
		}
		return true;
	}

	private bool RunCallbackSchedulesDelaySameTick(bool physics)
	{
		StateMachineProcessFlags idleProcessFlags = ((!physics) ? StateMachineProcessFlags.Process : StateMachineProcessFlags.PhysicsProcess);
		if (!TryCompileProgram(CreateDefinition(physics ? "probe.optimization.state_machine.callback.physics" : "probe.optimization.state_machine.callback.process", idleProcessFlags, 0.05), out var program))
		{
			return false;
		}
		StateMachineRuntime stateMachineRuntime = new StateMachineRuntime();
		stateMachineRuntime.Initialize(program, physics ? StateMachineDelayClock.Physics : StateMachineDelayClock.Process);
		StateHandle idle = stateMachineRuntime.GetStateHandle("Idle");
		bool requested = false;
		bool accepted = false;
		if (physics)
		{
			idle.PhysicsProcessing += delegate
			{
				if (!requested)
				{
					requested = true;
					accepted = idle.SendEvent(DelayEvent);
				}
			};
		}
		else
		{
			idle.Processing += delegate
			{
				if (!requested)
				{
					requested = true;
					accepted = idle.SendEvent(DelayEvent);
				}
			};
		}
		bool num = stateMachineRuntime.EnterInitialState();
		if (physics)
		{
			stateMachineRuntime.TickPhysics(0.02);
		}
		else
		{
			stateMachineRuntime.TickProcess(0.02);
		}
		bool flag = requested && accepted && stateMachineRuntime.HasPendingTransition && Math.Abs(stateMachineRuntime.PendingDelayRemaining - 0.03) < 1E-06 && stateMachineRuntime.IsActive("Idle");
		if (physics)
		{
			stateMachineRuntime.TickPhysics(0.031);
		}
		else
		{
			stateMachineRuntime.TickProcess(0.031);
		}
		bool flag2 = stateMachineRuntime.IsActive("Done") && !stateMachineRuntime.HasPendingTransition;
		stateMachineRuntime.Dispose();
		return num && flag && flag2;
	}

	private bool RunIdleReentryRejected(bool physics)
	{
		StateMachineProcessFlags processFlags = ((!physics) ? StateMachineProcessFlags.Process : StateMachineProcessFlags.PhysicsProcess);
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = (physics ? "probe.optimization.state_machine.reentry.physics" : "probe.optimization.state_machine.reentry.process"),
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "Active"
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Active",
			DisplayName = "Active",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "Root",
			ProcessFlags = processFlags
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Silent",
			DisplayName = "Silent",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "Root"
		});
		stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
		{
			StableId = "ActiveToSilent",
			SourceStateId = "Active",
			TargetStateId = "Silent",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = GoSilentEvent,
			DeclarationOrder = 0
		});
		if (!TryCompileProgram(stateMachineDefinition, out var program))
		{
			return false;
		}
		StateMachineRuntime runtime = new StateMachineRuntime();
		runtime.Initialize(program, physics ? StateMachineDelayClock.Physics : StateMachineDelayClock.Process);
		StateHandle active = runtime.GetStateHandle("Active");
		bool transitioned = false;
		bool rejected = false;
		if (physics)
		{
			active.PhysicsProcessing += AttemptReentry;
		}
		else
		{
			active.Processing += AttemptReentry;
		}
		bool num = runtime.EnterInitialState();
		if (physics)
		{
			runtime.TickPhysics(0.0001);
		}
		else
		{
			runtime.TickProcess(0.0001);
		}
		bool result = num && transitioned && rejected && runtime.IsActive("Silent");
		runtime.Dispose();
		return result;
		void AttemptReentry(double _)
		{
			transitioned = active.SendEvent(GoSilentEvent);
			try
			{
				if (physics)
				{
					runtime.TickPhysics(0.0001);
				}
				else
				{
					runtime.TickProcess(0.0001);
				}
			}
			catch (InvalidOperationException)
			{
				rejected = true;
			}
		}
	}

	private bool PrintResult(string stateId, string phase, in OptimizationBatchResult result, bool functionalPassed)
	{
		OptimizationResultIdentity identity = new OptimizationResultIdentity("state_machine_idle_dispatch_fast_path", OptimizationWorkloadKind.BareFunction, "state_machine_runtime", "res://Test/StateMachineIdleDispatchPerformanceRuntimeTest.tscn", "StateMachineRuntime", "none", "none", "probe.optimization.state_machine", stateId, phase, OptimizationScheduleKind.BackToBack, "headless", Math.Max(1, Engine.PhysicsTicksPerSecond), 240);
		bool flag = OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed, 1000, 240, 1000, 1000, in identity);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, functionalPassed, flag, 1000, 240, 1000, 1000));
		return flag;
	}

	private bool TryCompileProgram(StateMachineDefinition definition, out StateMachineProgram program)
	{
		if (StateMachineCompiler.TryCompile(definition, out program, out var validation))
		{
			return true;
		}
		string text = ((validation == null) ? "unknown" : validation.ToString());
		Check(condition: false, "Could not compile '" + definition?.DefinitionId + "': " + text);
		return false;
	}

	private static StateMachineDefinition CreateDefinition(string definitionId, StateMachineProcessFlags idleProcessFlags, double delaySeconds)
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = definitionId,
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "Idle"
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Idle",
			DisplayName = "Idle",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "Root",
			ProcessFlags = idleProcessFlags
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Done",
			DisplayName = "Done",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "Root"
		});
		if (delaySeconds > 0.0)
		{
			stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
			{
				StableId = "IdleToDoneDelayed",
				SourceStateId = "Idle",
				TargetStateId = "Done",
				TriggerKind = StateMachineTriggerKind.Event,
				EventName = DelayEvent,
				DelaySeconds = delaySeconds,
				DeclarationOrder = 0
			});
		}
		return stateMachineDefinition;
	}

	private static void DisposeRuntimes(StateMachineRuntime[] runtimes)
	{
		for (int i = 0; i < runtimes.Length; i++)
		{
			runtimes[i].Dispose();
		}
	}

	private void FinishEarly()
	{
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.PrintErr(_failures[i]);
		}
		GetTree().Quit(2);
	}

	private void Check(bool condition, string failure)
	{
		if (!condition)
		{
			_failures.Add(failure);
		}
	}
}
