using System;
using System.Diagnostics;
using Godot;

public partial class AttackComponentPhysicsHotPathRuntimeTest : Node
{
	private readonly struct BenchmarkResult(bool passed, int activeInstances, int dispatched, double mean, double p50, double p95, double p99, double max, long allocatedBytes, int gen0, int gen1, int gen2, bool functional)
	{
		public readonly bool Passed = passed;

		public readonly int ActiveInstances = activeInstances;

		public readonly int Dispatched = dispatched;

		public readonly double Mean = mean;

		public readonly double P50 = p50;

		public readonly double P95 = p95;

		public readonly double P99 = p99;

		public readonly double Max = max;

		public readonly long AllocatedBytes = allocatedBytes;

		public readonly int Gen0 = gen0;

		public readonly int Gen1 = gen1;

		public readonly int Gen2 = gen2;

		public readonly bool Functional = functional;
	}

	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private const double PhysicsDelta = 0.0005;

	private const double ActiveTimer = 1000.0;

	private readonly AttackComponent[] _components = new AttackComponent[1000];

	private readonly AttackComponentHotPathProbeOwner[] _owners = new AttackComponentHotPathProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private readonly double[] _activeSamples = new double[1200];

	private readonly double[] _idleSamples = new double[1200];

	private BenchmarkResult _activeResult;

	private BenchmarkResult _idleResult;

	public override void _Ready()
	{
		try
		{
			CreateWorkload();
			RunActiveWarmup();
			RunActiveBenchmark();
			RunIdleBenchmark();
		}
		catch (Exception ex)
		{
			GD.PrintErr("ATTACK_COMPONENT_PHYSICS_HOT_PATH_EXCEPTION " + ex);
			Finish(passed: false, 0, 0, 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0L, 0, 0, 0, 0, 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0L);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		AttackComponentDefinition attackComponentDefinition = GD.Load<AttackComponentDefinition>("res://Script/Component/TowerDefense/Character/AttackComponent/AttackComponentZombieDefinition.tres");
		if (attackComponentDefinition == null)
		{
			throw new InvalidOperationException("Shipped zombie AttackComponent Definition could not be loaded.");
		}
		for (int i = 0; i < 1000; i++)
		{
			AttackComponentHotPathProbeOwner attackComponentHotPathProbeOwner = new AttackComponentHotPathProbeOwner
			{
				Name = "AttackComponentHotPathOwner" + i
			};
			AddChild(attackComponentHotPathProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			AttackComponent attackComponent = new AttackComponent();
			attackComponent.Bind(componentManager, attackComponentHotPathProbeOwner, attackComponentDefinition);
			attackComponent.Activate();
			attackComponent.timer = 1000.0;
			_components[i] = attackComponent;
			_owners[i] = attackComponentHotPathProbeOwner;
			_managers[i] = componentManager;
		}
	}

	private void RunActiveWarmup()
	{
		for (int i = 0; i < 240; i++)
		{
			ulong physicsFrame = (ulong)i;
			for (int j = 0; j < 1000; j++)
			{
				_components[j].PhysicsProcess(0.0005, physicsFrame);
			}
		}
		CollectGarbage();
	}

	private void RunActiveBenchmark()
	{
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		int num4 = 0;
		for (int i = 0; i < 1200; i++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			ulong physicsFrame = (ulong)(240 + i);
			for (int j = 0; j < 1000; j++)
			{
				_components[j].PhysicsProcess(0.0005, physicsFrame);
				num4++;
			}
			_activeSamples[i] = ElapsedMilliseconds(timestamp);
		}
		long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		int gen = GC.CollectionCount(0) - num;
		int gen2 = GC.CollectionCount(1) - num2;
		int gen3 = GC.CollectionCount(2) - num3;
		Array.Sort(_activeSamples);
		int num5 = CountPhysicsWorkInstances();
		long num6 = 1440L;
		double num7 = 1000.0 - (double)num6 * 0.0005;
		bool functional = num5 == 1000 && _components[0].Lifecycle == ComponentRuntimeLifecycle.Active && Math.Abs(_components[0].timer - num7) < 1E-06 && num4 == 1200000;
		_activeResult = BuildResult(_activeSamples, num4, num5, allocatedBytes, gen, gen2, gen3, functional);
	}

	private void RunIdleBenchmark()
	{
		for (int i = 0; i < 1000; i++)
		{
			_components[i].timer = 0.0;
		}
		for (int j = 0; j < 240; j++)
		{
			ulong physicsFrame = (ulong)j;
			for (int k = 0; k < 1000; k++)
			{
				_components[k].PhysicsProcess(0.0005, physicsFrame);
			}
		}
		CollectGarbage();
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		int num4 = 0;
		for (int l = 0; l < 1200; l++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			ulong physicsFrame2 = (ulong)(240 + l);
			for (int m = 0; m < 1000; m++)
			{
				_components[m].PhysicsProcess(0.0005, physicsFrame2);
				num4++;
			}
			_idleSamples[l] = ElapsedMilliseconds(timestamp);
		}
		long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		int gen = GC.CollectionCount(0) - num;
		int gen2 = GC.CollectionCount(1) - num2;
		int gen3 = GC.CollectionCount(2) - num3;
		Array.Sort(_idleSamples);
		int num5 = CountPhysicsWorkInstances();
		bool functional = num5 == 0 && _components[0].Lifecycle == ComponentRuntimeLifecycle.Active && _components[0].timer == 0.0 && num4 == 1200000;
		_idleResult = BuildResult(_idleSamples, num4, num5, allocatedBytes, gen, gen2, gen3, functional);
		Finish(_activeResult.Passed && _idleResult.Passed, _activeResult, _idleResult);
	}

	private int CountPhysicsWorkInstances()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			if (_components[i].Alive && _components[i].WantsPhysicsProcess)
			{
				num++;
			}
		}
		return num;
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			if (GodotObject.IsInstanceValid(_owners[i]) && _owners[i].IsInsideTree())
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
			AttackComponent obj = _components[i];
			if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active && _components[i].Alive)
			{
				num++;
			}
		}
		return num;
	}

	private static void CollectGarbage()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private static double ElapsedMilliseconds(long started)
	{
		return (double)(Stopwatch.GetTimestamp() - started) * 1000.0 / (double)Stopwatch.Frequency;
	}

	private static BenchmarkResult BuildResult(double[] samples, int dispatched, int activeInstances, long allocatedBytes, int gen0, int gen1, int gen2, bool functional)
	{
		double num = 0.0;
		for (int i = 0; i < samples.Length; i++)
		{
			num += samples[i];
		}
		num /= (double)samples.Length;
		double p = Percentile(samples, 0.5);
		double p2 = Percentile(samples, 0.95);
		double num2 = Percentile(samples, 0.99);
		double max = samples[^1];
		return new BenchmarkResult(functional && allocatedBytes == 0L && gen0 == 0 && gen1 == 0 && gen2 == 0 && num2 < 0.2, activeInstances, dispatched, num, p, p2, num2, max, allocatedBytes, gen0, gen1, gen2, functional);
	}

	private static double Percentile(double[] samples, double fraction)
	{
		int num = Math.Clamp((int)Math.Ceiling(fraction * (double)samples.Length) - 1, 0, samples.Length - 1);
		return samples[num];
	}

	private void Finish(bool passed, BenchmarkResult active, BenchmarkResult idle)
	{
		int num = CountTreeOwners();
		int num2 = CountActiveInstances();
		passed = passed && num == 1000 && num2 == 1000;
		GD.Print($"ATTACK_COMPONENT_PHYSICS_HOT_PATH_RESULT passed={passed} instances={1000} warmupSamples={240} measuredSamples={1200} treeOwners={num} activeInstances={num2} activePassed={active.Passed} activeFunctional={active.Functional} activeWorkInstances={active.ActiveInstances} activeDispatchedInstances={active.Dispatched} activeMeanMs={active.Mean:F6} activeP50Ms={active.P50:F6} activeP95Ms={active.P95:F6} activeP99Ms={active.P99:F6} activeMaxMs={active.Max:F6} activeAllocatedBytes={active.AllocatedBytes} activeGen0={active.Gen0} activeGen1={active.Gen1} activeGen2={active.Gen2} idlePassed={idle.Passed} idleFunctional={idle.Functional} idleWorkInstances={idle.ActiveInstances} idleDispatchedInstances={idle.Dispatched} idleMeanMs={idle.Mean:F6} idleP50Ms={idle.P50:F6} idleP95Ms={idle.P95:F6} idleP99Ms={idle.P99:F6} idleMaxMs={idle.Max:F6} idleAllocatedBytes={idle.AllocatedBytes} idleGen0={idle.Gen0} idleGen1={idle.Gen1} idleGen2={idle.Gen2}");
		GetTree().Quit((!passed) ? 2 : 0);
	}

	private void Finish(bool passed, int instances, int activeInstances, int activeDispatched, double activeMean, double activeP50, double activeP95, double activeP99, double activeMax, long activeAllocated, int activeGen0, int activeGen1, int activeGen2, int idleInstances, int idleDispatched, double idleMean, double idleP50, double idleP95, double idleP99, double idleMax, long idleAllocated)
	{
		GD.PrintErr($"ATTACK_COMPONENT_PHYSICS_HOT_PATH_EXCEPTION_RESULT passed={passed} instances={instances} activeInstances={activeInstances} activeDispatchedInstances={activeDispatched} activeMeanMs={activeMean:F6} activeP50Ms={activeP50:F6} activeP95Ms={activeP95:F6} activeP99Ms={activeP99:F6} activeMaxMs={activeMax:F6} activeAllocatedBytes={activeAllocated} activeGen0={activeGen0} activeGen1={activeGen1} activeGen2={activeGen2} idleInstances={idleInstances} idleDispatchedInstances={idleDispatched} idleMeanMs={idleMean:F6} idleP50Ms={idleP50:F6} idleP95Ms={idleP95:F6} idleP99Ms={idleP99:F6} idleMaxMs={idleMax:F6} idleAllocatedBytes={idleAllocated}");
		GetTree().Quit(2);
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
