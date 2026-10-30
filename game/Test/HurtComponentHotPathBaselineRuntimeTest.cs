using System;
using System.Reflection;
using Godot;

public partial class HurtComponentHotPathBaselineRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private const float HealAmount = 1f;

	private readonly HurtComponent[] _components = new HurtComponent[1000];

	private readonly HurtComponentHotPathProbeOwner[] _owners = new HurtComponentHotPathProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private TowerDefenseManager _manager;

	private TowerDefenseControlNew _control;

	private HurtComponentDefinition _definition;

	private int _measuredDispatched;

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
			CreateGameRunningContext();
			CreateWorkload();
			flag = RunFunctionalContract();
			ReplaceReleasedFunctionalProbe();
			RunWarmup();
			optimizationBatchResult = Benchmark();
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			bool flag2 = flag && optimizationBatchResult.AllocatedBytes == 0L && optimizationBatchResult.Gen0Collections == 0 && optimizationBatchResult.Gen1Collections == 0 && optimizationBatchResult.Gen2Collections == 0 && optimizationBatchResult.P99Milliseconds < 0.2 && num == 1000 && num2 == 1000 && _measuredDispatched == 1200000;
			GD.Print($"HURT_COMPONENT_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} warmupSamples={240} measuredSamples={1200} treeOwners={num} activeInstances={num2} dispatchedInstances={_measuredDispatched} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("HURT_COMPONENT_HOT_PATH_EXCEPTION " + ex);
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
		_manager = new TowerDefenseManager
		{
			currentControl = new TowerDefenseControlNew
			{
				isGameRunning = true
			},
			damagePipeline = new DamagePipeline()
		};
		_control = _manager.currentControl;
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
		_definition = new HurtComponentDefinition
		{
			ComponentTypeId = "HurtComponent",
			DefinitionId = "hurt.hotpath.runtime",
			InstanceId = "hurt.hotpath.runtime",
			WireIndex = 0,
			maxHealthEffectCount = 0,
			healthEffectAnimation = new StringName(),
			flashOnDamage = false,
			flashOnHeal = false,
			markHealthBarDirty = false
		};
		for (int i = 0; i < 1000; i++)
		{
			HurtComponentHotPathProbeOwner hurtComponentHotPathProbeOwner = new HurtComponentHotPathProbeOwner
			{
				Name = "HurtComponentHotPathOwner" + i,
				camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
				instance = new TowerDefenseCharacterInstance
				{
					hitpointsBase = 100.0,
					hitpointsSave = 100.0,
					hitpoints = 100.0
				}
			};
			hurtComponentHotPathProbeOwner.instance.character = hurtComponentHotPathProbeOwner;
			AddChild(hurtComponentHotPathProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			HurtComponent hurtComponent = new HurtComponent();
			hurtComponent.Bind(componentManager, hurtComponentHotPathProbeOwner, _definition);
			hurtComponent.Activate();
			_owners[i] = hurtComponentHotPathProbeOwner;
			_managers[i] = componentManager;
			_components[i] = hurtComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		HurtComponent hurtComponent = _components[0];
		TowerDefenseCharacterInstance instance = _owners[0].instance;
		double hitpoints = instance.hitpoints;
		hurtComponent.Health(1f);
		bool flag = Math.Abs(instance.hitpoints - hitpoints - 1.0) < 1E-06;
		hurtComponent.SetAlive(alive: false);
		hitpoints = instance.hitpoints;
		hurtComponent.Health(1f);
		bool flag2 = Math.Abs(instance.hitpoints - hitpoints) < 1E-06;
		hurtComponent.SetAlive(alive: true);
		hurtComponent.Release();
		if (flag && flag2)
		{
			return hurtComponent.IsReleased;
		}
		return false;
	}

	private void ReplaceReleasedFunctionalProbe()
	{
		HurtComponent hurtComponent = new HurtComponent();
		hurtComponent.Bind(_managers[0], _owners[0], _definition);
		hurtComponent.Activate();
		_components[0] = hurtComponent;
	}

	private void RunWarmup()
	{
		for (int i = 0; i < 240; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				_components[j].Health(1f);
			}
		}
		OptimizationBatchSampler.PrepareForWarmup();
	}

	private OptimizationBatchResult Benchmark()
	{
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		_measuredDispatched = 0;
		optimizationBatchSampler.BeginMeasurement();
		for (int i = 0; i < 1200; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			for (int j = 0; j < 1000; j++)
			{
				_components[j].Health(1f);
				_measuredDispatched++;
			}
			optimizationBatchSampler.EndSample(startTicks);
		}
		return optimizationBatchSampler.Complete();
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
			HurtComponent obj = _components[i];
			if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active && _components[i].Alive)
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
