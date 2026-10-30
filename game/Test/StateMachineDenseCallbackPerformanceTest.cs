using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class StateMachineDenseCallbackPerformanceTest : Node
{
	private sealed class ProbeHost
	{
		public long ProcessCalls;

		public long PhysicsCalls;

		public bool RequestTransition;

		public bool RecordProcessOrder;

		public bool RecordPhysicsOrder;

		public readonly int[] ProcessOrder = new int[9];

		public readonly int[] PhysicsOrder = new int[9];

		public int ProcessOrderCount;

		public int PhysicsOrderCount;
	}

	private const int RuntimeCount = 1000;

	private const int ActiveCallbackCount = 8;

	private const int DormantCallbackCount = 56;

	private const int WarmupFrames = 240;

	private const int MeasuredFrames = 1200;

	private const string SuccessMarker = "STATE_MACHINE_DENSE_CALLBACK_RESULT";

	public override void _Ready()
	{
		bool flag = false;
		try
		{
			flag = RunBenchmark();
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"STATE_MACHINE_DENSE_CALLBACK_RESULT"} passed=False error={value}");
		}
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private bool RunBenchmark()
	{
		if (!StateMachineCompiler.TryCompile(CreateDefinition(), out var program, out var validation) || !validation.IsValid)
		{
			GD.PrintErr($"{"STATE_MACHINE_DENSE_CALLBACK_RESULT"} passed=False compile={validation?.Diagnostics.Count ?? (-1)}");
			return false;
		}
		if (program.ProcessStateIndices.Length != 64 || program.PhysicsStateIndices.Length != 64)
		{
			GD.PrintErr($"{"STATE_MACHINE_DENSE_CALLBACK_RESULT"} passed=False callbackCapacity={program.ProcessStateIndices.Length}/{program.PhysicsStateIndices.Length}");
			return false;
		}
		StateMachineRuntime[] array = new StateMachineRuntime[1000];
		ProbeHost[] array2 = new ProbeHost[1000];
		for (int i = 0; i < 1000; i++)
		{
			ProbeHost probeHost = new ProbeHost();
			StateMachineCallbackBinding callbackBinding = CreateBinding(program, probeHost);
			StateMachineRuntime stateMachineRuntime = new StateMachineRuntime();
			stateMachineRuntime.Initialize(program, StateMachineDelayClock.Process, callbackBinding);
			if (!stateMachineRuntime.EnterInitialState())
			{
				return false;
			}
			array[i] = stateMachineRuntime;
			array2[i] = probeHost;
		}
		bool flag = VerifyOrder(array[0], array2[0], physics: false);
		bool flag2 = VerifyOrder(array[0], array2[0], physics: true);
		bool flag3 = RunTransitionScenario();
		StateMachineSnapshot stateMachineSnapshot = array[0].CaptureSnapshot();
		ProbeHost host = new ProbeHost();
		StateMachineCallbackBinding callbackBinding2 = CreateBinding(program, host);
		StateMachineRuntime stateMachineRuntime2 = new StateMachineRuntime();
		stateMachineRuntime2.Initialize(program, StateMachineDelayClock.Process, callbackBinding2);
		bool flag4 = stateMachineRuntime2.EnterInitialState() && stateMachineSnapshot != null && stateMachineRuntime2.RestoreSnapshot(stateMachineSnapshot, suppressEntryEffects: true);
		for (int j = 0; j < 240; j++)
		{
			TickAll(array);
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		double[] array3 = new double[1200];
		double[] array4 = new double[1200];
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		for (int k = 0; k < 1200; k++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			for (int l = 0; l < 1000; l++)
			{
				array[l].TickProcess(1.0 / 240.0);
			}
			array3[k] = ToMilliseconds(Stopwatch.GetTimestamp() - timestamp);
			long timestamp2 = Stopwatch.GetTimestamp();
			for (int m = 0; m < 1000; m++)
			{
				array[m].TickPhysics(1.0 / 240.0);
			}
			array4[k] = ToMilliseconds(Stopwatch.GetTimestamp() - timestamp2);
		}
		long allocatedBytesForCurrentThread2 = GC.GetAllocatedBytesForCurrentThread();
		int num4 = GC.CollectionCount(0);
		int num5 = GC.CollectionCount(1);
		int num6 = GC.CollectionCount(2);
		long num7 = 0L;
		long num8 = 0L;
		for (int n = 0; n < 1000; n++)
		{
			num7 += array2[n].ProcessCalls;
			num8 += array2[n].PhysicsCalls;
		}
		long num9 = 11520008L;
		double num10 = BenchmarkStatistics.Percentile(array3, 1200, 99.0);
		double num11 = BenchmarkStatistics.Percentile(array4, 1200, 99.0);
		double num12 = num10 / 1000.0 / 8.0;
		double num13 = num11 / 1000.0 / 8.0;
		double value = BenchmarkStatistics.Average(array3, 1200);
		double value2 = BenchmarkStatistics.Average(array4, 1200);
		double value3 = BenchmarkStatistics.Percentile(array3, 1200, 95.0);
		double value4 = BenchmarkStatistics.Percentile(array4, 1200, 95.0);
		double value5 = BenchmarkStatistics.Maximum(array3, 1200);
		double value6 = BenchmarkStatistics.Maximum(array4, 1200);
		long num14 = allocatedBytesForCurrentThread2 - allocatedBytesForCurrentThread;
		int num15 = num4 - num + (num5 - num2) + (num6 - num3);
		bool flag5 = num7 == num9 && num8 == num9 && num14 == 0L && num15 == 0 && flag && flag2 && flag3 && flag4 && num12 < 0.2 && num13 < 0.2;
		GD.Print($"{"STATE_MACHINE_DENSE_CALLBACK_RESULT"} passed={flag5} runtimes={1000} activeCallbacks={8} dormantCallbacks={56} warmup={240} measured={1200} processMeanMs={value:F6} processP95Ms={value3:F6} processP99Ms={num10:F6} processMaxMs={value5:F6} physicsMeanMs={value2:F6} physicsP95Ms={value4:F6} physicsP99Ms={num11:F6} physicsMaxMs={value6:F6} processStateP99Ms={num12:F6} physicsStateP99Ms={num13:F6} processCalls={num7}/{num9} physicsCalls={num8}/{num9} allocatedBytes={num14} gcDelta={num15} processOrder={flag} physicsOrder={flag2} multiActiveTransition={flag3} snapshotRoundTrip={flag4}");
		for (int num16 = 0; num16 < 1000; num16++)
		{
			array[num16].Dispose();
		}
		stateMachineRuntime2.Dispose();
		return flag5;
	}

	private static bool VerifyOrder(StateMachineRuntime runtime, ProbeHost host, bool physics)
	{
		host.ProcessOrderCount = 0;
		host.PhysicsOrderCount = 0;
		host.RecordProcessOrder = !physics;
		host.RecordPhysicsOrder = physics;
		if (physics)
		{
			runtime.TickPhysics(1.0 / 240.0);
		}
		else
		{
			runtime.TickProcess(1.0 / 240.0);
		}
		host.RecordProcessOrder = false;
		host.RecordPhysicsOrder = false;
		int[] array = (physics ? host.PhysicsOrder : host.ProcessOrder);
		if ((physics ? host.PhysicsOrderCount : host.ProcessOrderCount) != 8)
		{
			return false;
		}
		for (int i = 0; i < 8; i++)
		{
			if (array[i] != i)
			{
				return false;
			}
		}
		return true;
	}

	private static bool RunTransitionScenario()
	{
		if (!StateMachineCompiler.TryCompile(CreateTransitionDefinition(), out var program, out var validation) || !validation.IsValid)
		{
			GD.Print($"STATE_MACHINE_DENSE_TRANSITION_COMPILE valid={validation?.IsValid} diagnostics={validation?.Diagnostics.Count}");
			return false;
		}
		ProbeHost probeHost = new ProbeHost
		{
			RequestTransition = true,
			RecordProcessOrder = true
		};
		StateMachineRuntime stateMachineRuntime = new StateMachineRuntime();
		stateMachineRuntime.Initialize(program, StateMachineDelayClock.Process, CreateBinding(program, probeHost));
		if (!stateMachineRuntime.EnterInitialState())
		{
			stateMachineRuntime.Dispose();
			return false;
		}
		stateMachineRuntime.TickProcess(1.0 / 240.0);
		probeHost.RecordProcessOrder = false;
		bool flag = stateMachineRuntime.IsActive("Replacement0") && !stateMachineRuntime.IsActive("Active0");
		for (int i = 1; i < 8; i++)
		{
			flag &= stateMachineRuntime.IsActive($"Active{i}");
		}
		bool flag2 = probeHost.ProcessOrderCount == 9 && probeHost.ProcessOrder[0] == 0 && probeHost.ProcessOrder[1] == 0;
		for (int j = 2; j <= 8; j++)
		{
			flag2 &= probeHost.ProcessOrder[j] == j - 1;
		}
		string text = string.Empty;
		for (int k = 0; k < Math.Min(probeHost.ProcessOrderCount, 9); k++)
		{
			text = text + ((k == 0) ? string.Empty : ",") + probeHost.ProcessOrder[k];
		}
		GD.Print($"STATE_MACHINE_DENSE_TRANSITION_TRACE active={flag} ordered={flag2} count={probeHost.ProcessOrderCount} request={probeHost.RequestTransition} active0={stateMachineRuntime.IsActive("Active0")} replacement={stateMachineRuntime.IsActive("Replacement0")} order={text}");
		stateMachineRuntime.Dispose();
		return flag && flag2;
	}

	private static void TickAll(StateMachineRuntime[] runtimes)
	{
		for (int i = 0; i < runtimes.Length; i++)
		{
			runtimes[i].TickProcess(1.0 / 240.0);
			runtimes[i].TickPhysics(1.0 / 240.0);
		}
	}

	private static StateMachineCallbackBinding CreateBinding(StateMachineProgram program, ProbeHost host)
	{
		StateMachineDeltaCallback[] array = new StateMachineDeltaCallback[program.StateCount];
		StateMachineDeltaCallback[] array2 = new StateMachineDeltaCallback[program.StateCount];
		for (int i = 0; i < program.StateCount; i++)
		{
			if ((program.States[i].ProcessFlags & StateMachineProcessFlags.Process) != StateMachineProcessFlags.None)
			{
				array[i] = OnProcess;
			}
			if ((program.States[i].ProcessFlags & StateMachineProcessFlags.PhysicsProcess) != StateMachineProcessFlags.None)
			{
				array2[i] = OnPhysics;
			}
		}
		return new StateMachineCallbackBinding(host, Array.Empty<StateMachineLifecycleCallback>(), Array.Empty<StateMachineLifecycleCallback>(), array, array2, Array.Empty<StateMachineGuardCallback>(), new List<Action>());
	}

	private static void OnProcess(in StateMachineCallbackContext context, double delta)
	{
		ProbeHost probeHost = (ProbeHost)context.Host;
		probeHost.ProcessCalls++;
		string stableId = context.State.StableId;
		if (probeHost.RequestTransition && stableId == "Active0")
		{
			probeHost.RequestTransition = false;
			context.SendEvent(new StringName("Replace"));
		}
		if (probeHost.RecordProcessOrder && probeHost.ProcessOrderCount < 9)
		{
			probeHost.ProcessOrder[probeHost.ProcessOrderCount++] = stableId[stableId.Length - 1] - 48;
		}
	}

	private static void OnPhysics(in StateMachineCallbackContext context, double delta)
	{
		ProbeHost probeHost = (ProbeHost)context.Host;
		probeHost.PhysicsCalls++;
		if (probeHost.RecordPhysicsOrder && probeHost.PhysicsOrderCount < 9)
		{
			string stableId = context.State.StableId;
			probeHost.PhysicsOrder[probeHost.PhysicsOrderCount++] = stableId[stableId.Length - 1] - 48;
		}
	}

	private static StateMachineDefinition CreateDefinition()
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "probe.state-machine.dense-callbacks",
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
			ParentId = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "Parallel"
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Parallel",
			DisplayName = "Parallel",
			ParentId = "Active",
			Kind = StateMachineStateKind.Parallel
		});
		for (int i = 0; i < 8; i++)
		{
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = $"Active{i}",
				DisplayName = $"Active{i}",
				ParentId = "Parallel",
				ProcessFlags = (StateMachineProcessFlags.Process | StateMachineProcessFlags.PhysicsProcess)
			});
		}
		for (int j = 0; j < 56; j++)
		{
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = $"Dormant{j:00}",
				DisplayName = $"Dormant{j:00}",
				ParentId = "Root",
				ProcessFlags = (StateMachineProcessFlags.Process | StateMachineProcessFlags.PhysicsProcess)
			});
		}
		return stateMachineDefinition;
	}

	private static StateMachineDefinition CreateTransitionDefinition()
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "probe.state-machine.dense-callback-transition",
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Parallel
		});
		for (int i = 0; i < 8; i++)
		{
			string text = $"Region{i}";
			string text2 = $"Active{i}";
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = text,
				DisplayName = text,
				Kind = StateMachineStateKind.Compound,
				ParentId = "Root",
				InitialChildId = text2
			});
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = text2,
				DisplayName = text2,
				ParentId = text,
				ProcessFlags = (StateMachineProcessFlags.Process | StateMachineProcessFlags.PhysicsProcess)
			});
			if (i == 0)
			{
				stateMachineDefinition.States.Add(new StateMachineStateDefinition
				{
					StableId = "Replacement0",
					DisplayName = "Replacement0",
					ParentId = text,
					ProcessFlags = (StateMachineProcessFlags.Process | StateMachineProcessFlags.PhysicsProcess)
				});
			}
		}
		stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
		{
			StableId = "Replace",
			SourceStateId = "Active0",
			TargetStateId = "Replacement0",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = new StringName("Replace")
		});
		return stateMachineDefinition;
	}

	private static double ToMilliseconds(long ticks)
	{
		return (double)ticks * 1000.0 / (double)Stopwatch.Frequency;
	}
}
