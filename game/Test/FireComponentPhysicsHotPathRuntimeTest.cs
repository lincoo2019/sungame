using System;
using System.Diagnostics;
using Godot;

public partial class FireComponentPhysicsHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private const double PhysicsDelta = 0.0005;

	private readonly FireComponent[] _components = new FireComponent[1000];

	private readonly FireComponentHotPathProbeOwner[] _owners = new FireComponentHotPathProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private readonly double[] _samples = new double[1200];

	public override void _Ready()
	{
		try
		{
			CreateWorkload();
			RunWarmup();
			Benchmark();
		}
		catch (Exception ex)
		{
			GD.PrintErr("FIRE_COMPONENT_PHYSICS_HOT_PATH_EXCEPTION " + ex);
			Finish(passed: false, 0, 0, 0, 0, 0.0, 0.0, 0.0, 0.0, 0.0, 0L, 0, 0, 0);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		FireComponentDefinition definition = new FireComponentDefinition
		{
			ComponentTypeId = "FireComponent",
			DefinitionId = "fire.hotpath.runtime",
			InstanceId = "fire.hotpath.runtime",
			WireIndex = 0,
			checkUse = false,
			fireInterval = 1.5f,
			fireIntervalBase = 1.5f
		};
		for (int i = 0; i < 1000; i++)
		{
			FireComponentHotPathProbeOwner fireComponentHotPathProbeOwner = new FireComponentHotPathProbeOwner
			{
				Name = "FireComponentHotPathOwner" + i
			};
			AddChild(fireComponentHotPathProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			FireComponent fireComponent = new FireComponent();
			fireComponent.Bind(componentManager, fireComponentHotPathProbeOwner, definition);
			fireComponent.timer = 1000f;
			fireComponent.isCheck = false;
			fireComponent.Activate();
			_owners[i] = fireComponentHotPathProbeOwner;
			_managers[i] = componentManager;
			_components[i] = fireComponent;
		}
	}

	private void RunWarmup()
	{
		for (int i = 0; i < 240; i++)
		{
			ulong physicsFrame = (ulong)i;
			for (int j = 0; j < 1000; j++)
			{
				_components[j].PhysicsProcess(0.0005, physicsFrame);
			}
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private void Benchmark()
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
			long num5 = Stopwatch.GetTimestamp() - timestamp;
			_samples[i] = (double)num5 * 1000.0 / (double)Stopwatch.Frequency;
		}
		long num6 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		int num7 = GC.CollectionCount(0) - num;
		int num8 = GC.CollectionCount(1) - num2;
		int num9 = GC.CollectionCount(2) - num3;
		Array.Sort(_samples);
		double num10 = 0.0;
		for (int k = 0; k < _samples.Length; k++)
		{
			num10 += _samples[k];
		}
		num10 /= (double)_samples.Length;
		double p = Percentile(0.5);
		double p2 = Percentile(0.95);
		double num11 = Percentile(0.99);
		double max = _samples[^1];
		int num12 = 0;
		int num13 = 0;
		for (int l = 0; l < 1000; l++)
		{
			if (_components[l].Alive && _components[l].Lifecycle == ComponentRuntimeLifecycle.Active && _components[l].WantsPhysicsProcess)
			{
				num12++;
			}
			if (GodotObject.IsInstanceValid(_owners[l]) && _owners[l].IsInsideTree())
			{
				num13++;
			}
		}
		bool passed = num4 == 1200000 && num12 == 1000 && num13 == 1000 && num6 == 0L && num7 == 0 && num8 == 0 && num9 == 0 && num11 < 0.2;
		Finish(passed, 1000, num13, num12, num4, num10, p, p2, num11, max, num6, num7, num8, num9);
	}

	private double Percentile(double fraction)
	{
		int num = Math.Clamp((int)Math.Ceiling(fraction * (double)_samples.Length) - 1, 0, _samples.Length - 1);
		return _samples[num];
	}

	private void Finish(bool passed, int instances, int treeOwners, int activeInstances, int dispatched, double mean, double p50, double p95, double p99, double max, long allocatedBytes, int gen0, int gen1, int gen2)
	{
		GD.Print($"FIRE_COMPONENT_PHYSICS_HOT_PATH_RESULT passed={passed} instances={instances} treeOwners={treeOwners} activeInstances={activeInstances} dispatchedInstances={dispatched} warmupSamples={240} measuredSamples={1200} meanMs={mean:F6} p50Ms={p50:F6} p95Ms={p95:F6} p99Ms={p99:F6} maxMs={max:F6} allocatedBytes={allocatedBytes} gen0={gen0} gen1={gen1} gen2={gen2}");
		GetTree().Quit((!passed) ? 2 : 0);
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
