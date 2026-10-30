using System;
using System.Diagnostics;
using System.Reflection;
using Godot;

public partial class TargetRegistrationComponentHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly TargetRegistrationComponent[] _components = new TargetRegistrationComponent[1000];

	private readonly TowerDefenseCharacter[] _owners = new TowerDefenseCharacter[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private readonly double[] _samples = new double[1200];

	private readonly double[] _baselineSamples = new double[1200];

	private TargetRegistrationComponentDefinition _definition;

	private Node2D _ownerMount;

	public override void _Ready()
	{
		bool flag = false;
		try
		{
			if (!CreateWorkload())
			{
				throw new InvalidOperationException("TargetRegistration runtime workload could not be created.");
			}
			if (!RunRebindAndManagerReplacementContract())
			{
				throw new InvalidOperationException("TargetRegistration cache rebind/manager replacement contract failed.");
			}
			RunMutationWarmup();
			BenchmarkOriginalNotify(out var mean, out var p, out var allocatedBytes, out var gen);
			RunMutationWarmup();
			BenchmarkMutation(out var mean2, out var p2, out var p3, out var p4, out var max, out var allocatedBytes2, out var gen2, out var activeInstances, out var dispatched, out var functional);
			int num = CountTreeOwners();
			flag = functional && num == 1000 && activeInstances == 1000 && dispatched == 1200000 && allocatedBytes2 == 0L && gen2 == 0 && p4 < 0.2;
			GD.Print($"TARGET_REGISTRATION_COMPONENT_HOT_PATH_RESULT passed={flag} instances={1000} warmupSamples={240} measuredSamples={1200} treeOwners={num} functional={functional} activeInstances={activeInstances} dispatchedInstances={dispatched} meanMs={mean2:F6} p50Ms={p2:F6} p95Ms={p3:F6} p99Ms={p4:F6} maxMs={max:F6} allocatedBytes={allocatedBytes2} gen0={gen2} baselineMeanMs={mean:F6} baselineP99Ms={p:F6} baselineAllocatedBytes={allocatedBytes} baselineGen0={gen}");
		}
		catch (Exception ex)
		{
			GD.PrintErr("TARGET_REGISTRATION_COMPONENT_HOT_PATH_EXCEPTION " + ex);
		}
		finally
		{
			ReleaseWorkload();
		}
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private bool CreateWorkload()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return false;
		}
		_ownerMount = new Node2D
		{
			Name = "TargetRegistrationOwnerMount"
		};
		AddChild(_ownerMount, forceReadableName: false, InternalMode.Disabled);
		_definition = new TargetRegistrationComponentDefinition
		{
			ComponentTypeId = "TargetRegistrationComponent",
			DefinitionId = "target-registration.hotpath.runtime",
			InstanceId = "target-registration.hotpath.runtime",
			WireIndex = 0,
			allLineCheck = false,
			canProjectileCheck = true,
			canCarry = true
		};
		for (int i = 0; i < 1000; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = new TargetRegistrationHotPathProbeOwner
			{
				Name = "TargetRegistrationHotPathOwner" + i,
				ProcessMode = ProcessModeEnum.Disabled,
				camp = ((i % 2 != 0) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			};
			_ownerMount.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			TargetRegistrationComponent targetRegistrationComponent = new TargetRegistrationComponent();
			targetRegistrationComponent.Bind(componentManager, towerDefenseCharacter, _definition);
			targetRegistrationComponent.Activate();
			towerDefenseCharacter.targetRegistrationComponent = targetRegistrationComponent;
			instance.CharacterRegister(towerDefenseCharacter);
			_components[i] = targetRegistrationComponent;
			_owners[i] = towerDefenseCharacter;
			_managers[i] = componentManager;
		}
		return true;
	}

	private bool RunRebindAndManagerReplacementContract()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return false;
		}
		TowerDefenseBattleCharacterRegistry characterRegistry = instance.characterRegistry;
		TowerDefenseManager towerDefenseManager = new TowerDefenseManager();
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = (towerDefenseManager.characterRegistry = new TowerDefenseBattleCharacterRegistry());
		PropertyInfo property = typeof(TowerDefenseManager).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (property == null)
		{
			return false;
		}
		ulong queryRevision = characterRegistry.QueryRevision;
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry2 = null;
		try
		{
			towerDefenseManager.CharacterRegister(_owners[0]);
			property.SetValue(null, towerDefenseManager);
			_components[0].allLineCheck = !_components[0].allLineCheck;
			bool num = towerDefenseBattleCharacterRegistry.QueryRevision != 0;
			bool flag = instance.characterRegistry.QueryRevision == queryRevision;
			_components[0].Detach(ComponentDetachReason.TemporaryTreeExit);
			_components[0].Bind(_managers[0], _owners[0], _definition);
			_components[0].Activate();
			ulong queryRevision2 = towerDefenseBattleCharacterRegistry.QueryRevision;
			_components[0].attackGridColumnAliasOffset++;
			bool flag2 = towerDefenseBattleCharacterRegistry.QueryRevision > queryRevision2;
			property.SetValue(null, instance);
			towerDefenseBattleCharacterRegistry2 = (instance.characterRegistry = new TowerDefenseBattleCharacterRegistry());
			towerDefenseBattleCharacterRegistry2.Register(_owners[0]);
			ulong queryRevision3 = towerDefenseBattleCharacterRegistry2.QueryRevision;
			_components[0].allLineCheck = !_components[0].allLineCheck;
			bool flag3 = towerDefenseBattleCharacterRegistry2.QueryRevision > queryRevision3;
			bool flag4 = characterRegistry.QueryRevision == queryRevision;
			instance.characterRegistry = characterRegistry;
			_components[0].Detach(ComponentDetachReason.TemporaryTreeExit);
			_components[0].Bind(_managers[0], _owners[0], _definition);
			_components[0].Activate();
			ulong queryRevision4 = characterRegistry.QueryRevision;
			_components[0].attackGridColumnAliasOffset++;
			bool flag5 = characterRegistry.QueryRevision > queryRevision4;
			return num && flag && flag2 && flag3 && flag4 && flag5 && _components[0].Lifecycle == ComponentRuntimeLifecycle.Active;
		}
		finally
		{
			property.SetValue(null, instance);
			instance.characterRegistry = characterRegistry;
			if (GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry2))
			{
				towerDefenseBattleCharacterRegistry2.Free();
			}
			if (GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry))
			{
				towerDefenseBattleCharacterRegistry.Free();
			}
			if (GodotObject.IsInstanceValid(towerDefenseManager))
			{
				towerDefenseManager.Free();
			}
		}
	}

	private void RunMutationWarmup()
	{
		for (int i = 0; i < 240; i++)
		{
			bool flag = (i & 1) == 0;
			int attackGridColumnAliasOffset = i & 1;
			for (int j = 0; j < 1000; j++)
			{
				TargetRegistrationComponent obj = _components[j];
				obj.allLineCheck = flag;
				obj.attackGridColumnAliasOffset = attackGridColumnAliasOffset;
				obj.canProjectileCheck = !flag;
				obj.canCarry = flag;
			}
		}
		CollectGarbage();
	}

	private void BenchmarkOriginalNotify(out double mean, out double p99, out long allocatedBytes, out int gen0)
	{
		for (int i = 0; i < 240; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				TowerDefenseManager instance = TowerDefenseManager.Instance;
				TowerDefenseCharacter towerDefenseCharacter = _owners[j];
				if (GodotObject.IsInstanceValid(instance?.characterRegistry) && GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					instance.characterRegistry.NotifyCharacterTargetingChanged(towerDefenseCharacter);
					instance.characterRegistry.NotifyCharacterTargetingChanged(towerDefenseCharacter);
				}
			}
		}
		CollectGarbage();
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		for (int k = 0; k < 1200; k++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			for (int l = 0; l < 1000; l++)
			{
				TowerDefenseManager instance2 = TowerDefenseManager.Instance;
				TowerDefenseCharacter towerDefenseCharacter2 = _owners[l];
				if (GodotObject.IsInstanceValid(instance2?.characterRegistry) && GodotObject.IsInstanceValid(towerDefenseCharacter2))
				{
					instance2.characterRegistry.NotifyCharacterTargetingChanged(towerDefenseCharacter2);
					instance2.characterRegistry.NotifyCharacterTargetingChanged(towerDefenseCharacter2);
				}
			}
			_baselineSamples[k] = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		}
		allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		gen0 = GC.CollectionCount(0) - num;
		Array.Sort(_baselineSamples);
		mean = 0.0;
		for (int m = 0; m < _baselineSamples.Length; m++)
		{
			mean += _baselineSamples[m];
		}
		mean /= _baselineSamples.Length;
		int num2 = Math.Clamp((int)Math.Ceiling(0.99 * (double)_baselineSamples.Length) - 1, 0, _baselineSamples.Length - 1);
		p99 = _baselineSamples[num2];
	}

	private void BenchmarkMutation(out double mean, out double p50, out double p95, out double p99, out double max, out long allocatedBytes, out int gen0, out int activeInstances, out int dispatched, out bool functional)
	{
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		dispatched = 0;
		for (int i = 0; i < 1200; i++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			bool flag = (i & 1) == 0;
			int attackGridColumnAliasOffset = i & 1;
			for (int j = 0; j < 1000; j++)
			{
				TargetRegistrationComponent obj = _components[j];
				obj.allLineCheck = flag;
				obj.attackGridColumnAliasOffset = attackGridColumnAliasOffset;
				obj.canProjectileCheck = !flag;
				obj.canCarry = flag;
				dispatched++;
			}
			_samples[i] = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		}
		allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		gen0 = GC.CollectionCount(0) - num;
		Array.Sort(_samples);
		mean = 0.0;
		for (int k = 0; k < _samples.Length; k++)
		{
			mean += _samples[k];
		}
		mean /= _samples.Length;
		p50 = Percentile(0.5);
		p95 = Percentile(0.95);
		p99 = Percentile(0.99);
		max = _samples[^1];
		activeInstances = 0;
		for (int l = 0; l < 1000; l++)
		{
			if (_components[l].Alive && _components[l].Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				activeInstances++;
			}
		}
		bool flag2 = false;
		bool flag3 = _components[0].allLineCheck == flag2;
		bool flag4 = _components[0].attackGridColumnAliasOffset == 1;
		functional = flag3 && flag4 && _components[0].canProjectileCheck && !_components[0].canCarry;
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
			TowerDefenseCharacter obj = _owners[i];
			if (obj != null && obj.IsInsideTree())
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

	private void ReleaseWorkload()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		for (int i = 0; i < 1000; i++)
		{
			TargetRegistrationComponent obj = _components[i];
			TowerDefenseCharacter towerDefenseCharacter = _owners[i];
			if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				instance.CharacterUnregister(towerDefenseCharacter);
			}
			obj?.Release();
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.Free();
			}
			_components[i] = null;
			_owners[i] = null;
			_managers[i] = null;
		}
		if (GodotObject.IsInstanceValid(_ownerMount))
		{
			RemoveChild(_ownerMount);
			_ownerMount.Free();
		}
		_ownerMount = null;
		_definition = null;
	}
}
