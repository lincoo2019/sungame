using System;
using System.Diagnostics;
using System.Reflection;
using Godot;
using Godot.Collections;

public partial class ProduceComponentHotPathBaselineRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private const double PhysicsDelta = 0.0005;

	private readonly ProduceComponent[] _components = new ProduceComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private readonly double[] _samples = new double[1200];

	private TowerDefenseManager _manager;

	private TowerDefenseControlNew _control;

	public override void _Ready()
	{
		RunAsync();
	}

	private void RunAsync()
	{
		bool flag = false;
		try
		{
			CreateGameRunningContext();
			CreateWorkload();
			flag = RunFunctionalContract();
			Benchmark("notDue", 10000f, due: false, out var mean, out var p, out var p2, out var p3, out var max, out var allocatedBytes, out var gen, out var gen2, out var gen3, out var dispatched);
			Benchmark("due", 0.0005f, due: true, out var mean2, out var p4, out var p5, out var p6, out var max2, out var allocatedBytes2, out var gen4, out var gen5, out var gen6, out var dispatched2);
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			bool flag2 = flag && num == 1000 && num2 == 1000 && dispatched == 1200000 && dispatched2 == 1200000 && allocatedBytes == 0L && gen == 0 && gen2 == 0 && gen3 == 0 && allocatedBytes2 == 0L && gen4 == 0 && gen5 == 0 && gen6 == 0 && p3 < 0.2 && p6 < 0.2;
			GD.Print($"PRODUCE_COMPONENT_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} warmupSamples={240} measuredSamples={1200} treeOwners={num} activeInstances={num2} notDueMeanMs={mean:F6} notDueP50Ms={p:F6} notDueP95Ms={p2:F6} notDueP99Ms={p3:F6} notDueMaxMs={max:F6} notDueAllocatedBytes={allocatedBytes} notDueGen0={gen} notDueGen1={gen2} notDueGen2={gen3} notDueDispatchedInstances={dispatched} dueMeanMs={mean2:F6} dueP50Ms={p4:F6} dueP95Ms={p5:F6} dueP99Ms={p6:F6} dueMaxMs={max2:F6} dueAllocatedBytes={allocatedBytes2} dueGen0={gen4} dueGen1={gen5} dueGen2={gen6} dueDispatchedInstances={dispatched2}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("PRODUCE_COMPONENT_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
			SetGameRunningContext(null, null);
		}
	}

	private void CreateGameRunningContext()
	{
		_manager = new TowerDefenseManager();
		_control = new TowerDefenseControlNew
		{
			isGameRunning = true
		};
		_manager.currentControl = _control;
		SetGameRunningContext(_manager, _control);
	}

	private static void SetGameRunningContext(TowerDefenseManager manager, TowerDefenseControlNew control)
	{
		typeof(TowerDefenseManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(null, manager);
		if (manager != null)
		{
			manager.currentControl = control;
		}
	}

	private void CreateWorkload()
	{
		ProduceComponentDefinition definition = new ProduceComponentDefinition
		{
			ComponentTypeId = "ProduceComponent",
			DefinitionId = "produce.hotpath.baseline",
			InstanceId = "produce.hotpath.baseline",
			WireIndex = 0,
			produceType = "Sun",
			produceInterval = 10000f,
			num = 25,
			onlyEmit = true,
			maxCatchUpProductions = 1
		};
		for (int i = 0; i < 1000; i++)
		{
			ProduceComponentHotPathProbeOwner produceComponentHotPathProbeOwner = new ProduceComponentHotPathProbeOwner
			{
				Name = "ProduceHotPathOwner" + i,
				ProcessMode = ProcessModeEnum.Disabled,
				instance = new TowerDefenseCharacterInstance
				{
					hitpoints = 200.0,
					hitpointsSave = 200.0
				}
			};
			produceComponentHotPathProbeOwner.instance.character = produceComponentHotPathProbeOwner;
			AddChild(produceComponentHotPathProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			ProduceComponent produceComponent = new ProduceComponent();
			produceComponent.Bind(componentManager, produceComponentHotPathProbeOwner, definition);
			produceComponent.Activate();
			produceComponent.produceGlowStarted = true;
			_owners[i] = produceComponentHotPathProbeOwner;
			_managers[i] = componentManager;
			_components[i] = produceComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		ProduceComponent obj = _components[0];
		int callbackCount = 0;
		int callbackIndex = -2147483648;
		int callbackAmount = -2147483648;
		obj.OnProduct += delegate(int index, int amount)
		{
			callbackCount++;
			callbackIndex = index;
			callbackAmount = amount;
		};
		obj.Create(Vector2.Zero, 7);
		obj.timer = obj.produceInterval;
		obj.produceGlowStarted = true;
		obj.PhysicsProcess(0.0005, 1uL);
		Dictionary dictionary = obj.ExportComponentSave();
		Dictionary dictionary2 = obj.SyncSerialize();
		bool flag = dictionary.ContainsKey("timer") && dictionary.ContainsKey("hpNext") && dictionary.ContainsKey("hpNextInterval") && dictionary.ContainsKey("produceType") && dictionary.ContainsKey("num") && dictionary2.ContainsKey("timer") && dictionary2.ContainsKey("hpNext") && dictionary2.ContainsKey("hpNextInterval") && dictionary2.ContainsKey("produceType") && dictionary2.ContainsKey("num");
		return callbackCount == 1 && callbackIndex == -1 && callbackAmount == 25 && flag;
	}

	private void Benchmark(string label, float interval, bool due, out double mean, out double p50, out double p95, out double p99, out double max, out long allocatedBytes, out int gen0, out int gen1, out int gen2, out int dispatched)
	{
		for (int i = 0; i < 1000; i++)
		{
			_components[i].produceInterval = interval;
			_components[i].timer = (due ? interval : 0f);
			_components[i].produceGlowStarted = true;
		}
		for (int j = 0; j < 240; j++)
		{
			ulong physicsFrame = (ulong)j;
			for (int k = 0; k < 1000; k++)
			{
				_components[k].PhysicsProcess(0.0005, physicsFrame);
			}
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		dispatched = 0;
		for (int l = 0; l < 1200; l++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			ulong physicsFrame2 = (ulong)(240 + l);
			for (int m = 0; m < 1000; m++)
			{
				_components[m].PhysicsProcess(0.0005, physicsFrame2);
				dispatched++;
			}
			_samples[l] = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		}
		allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		gen0 = GC.CollectionCount(0) - num;
		gen1 = GC.CollectionCount(1) - num2;
		gen2 = GC.CollectionCount(2) - num3;
		System.Array.Sort(_samples);
		mean = 0.0;
		for (int n = 0; n < _samples.Length; n++)
		{
			mean += _samples[n];
		}
		mean /= _samples.Length;
		p50 = Percentile(0.5);
		p95 = Percentile(0.95);
		p99 = Percentile(0.99);
		max = _samples[^1];
		GD.Print($"PRODUCE_COMPONENT_{label.ToUpperInvariant()}_DETAIL meanMs={mean:F6} p50Ms={p50:F6} p95Ms={p95:F6} p99Ms={p99:F6} maxMs={max:F6} allocatedBytes={allocatedBytes} gen0={gen0} gen1={gen1} gen2={gen2}");
	}

	private double Percentile(double fraction)
	{
		int num = Math.Clamp((int)Math.Ceiling(fraction * (double)_samples.Length) - 1, 0, _samples.Length - 1);
		return _samples[num];
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
			ProduceComponent obj = _components[i];
			if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active && _components[i].Alive && _components[i].WantsPhysicsProcess)
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
		_manager?.Free();
		_control?.Free();
		_manager = null;
		_control = null;
	}
}
