using System;
using Godot;

public partial class BuffComponentIdleHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private const double Delta = 1.0 / 60.0;

	private readonly BuffComponent[] _components = new BuffComponent[1000];

	private readonly BuffIdleHotPathProbeOwner[] _owners = new BuffIdleHotPathProbeOwner[1000];

	private readonly AdobeAnimateSprite[] _sprites = new AdobeAnimateSprite[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private BuffComponentDefinition _definition;

	public override void _Ready()
	{
		Run();
	}

	private void Run()
	{
		try
		{
			CreateWorkload();
			bool flag = RunFunctionalContract();
			Warmup();
			OptimizationBatchResult optimizationBatchResult = BenchmarkBaseline();
			OptimizationBatchResult optimizationBatchResult2 = BenchmarkCandidate();
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			bool flag2 = optimizationBatchResult2.P99Milliseconds < 0.2 && optimizationBatchResult2.MeanMilliseconds < optimizationBatchResult.MeanMilliseconds && optimizationBatchResult2.P50Milliseconds < optimizationBatchResult.P50Milliseconds && optimizationBatchResult2.AllocatedBytes == 0L && optimizationBatchResult2.Gen0Collections == 0 && optimizationBatchResult2.Gen1Collections == 0 && optimizationBatchResult2.Gen2Collections == 0;
			bool flag3 = flag && flag2 && num == 1000 && num2 == 1000;
			double value = ((optimizationBatchResult.MeanMilliseconds <= 0.0) ? 0.0 : ((optimizationBatchResult.MeanMilliseconds - optimizationBatchResult2.MeanMilliseconds) / optimizationBatchResult.MeanMilliseconds * 100.0));
			GD.Print($"BUFF_COMPONENT_IDLE_HOT_PATH_RESULT passed={flag3} functionalPassed={flag} performancePassed={flag2} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} baselineMeanMs={optimizationBatchResult.MeanMilliseconds:F6} baselineP50Ms={optimizationBatchResult.P50Milliseconds:F6} baselineP99Ms={optimizationBatchResult.P99Milliseconds:F6} candidateMeanMs={optimizationBatchResult2.MeanMilliseconds:F6} candidateP50Ms={optimizationBatchResult2.P50Milliseconds:F6} candidateP99Ms={optimizationBatchResult2.P99Milliseconds:F6} meanReductionPercent={value:F2} allocatedBytes={optimizationBatchResult2.AllocatedBytes} gen0={optimizationBatchResult2.Gen0Collections} gen1={optimizationBatchResult2.Gen1Collections} gen2={optimizationBatchResult2.Gen2Collections}");
			GetTree().Quit((!flag3) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("BUFF_COMPONENT_IDLE_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new BuffComponentDefinition
		{
			ComponentTypeId = "BuffComponent",
			DefinitionId = "buff.idle.hotpath.runtime",
			InstanceId = "buff.idle.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = new AdobeAnimateSprite
			{
				ProcessMode = ProcessModeEnum.Disabled
			};
			BuffIdleHotPathProbeOwner buffIdleHotPathProbeOwner = new BuffIdleHotPathProbeOwner
			{
				instance = new TowerDefenseCharacterInstance(),
				sprite = adobeAnimateSprite,
				ProcessMode = ProcessModeEnum.Disabled
			};
			buffIdleHotPathProbeOwner.instance.character = buffIdleHotPathProbeOwner;
			AddChild(buffIdleHotPathProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			BuffComponent buffComponent = new BuffComponent();
			buffComponent.Bind(componentManager, buffIdleHotPathProbeOwner, _definition);
			buffComponent.Activate();
			_sprites[i] = adobeAnimateSprite;
			_owners[i] = buffIdleHotPathProbeOwner;
			_managers[i] = componentManager;
			_components[i] = buffComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		BuffComponent buffComponent = _components[0];
		AdobeAnimateSprite adobeAnimateSprite = _sprites[0];
		adobeAnimateSprite.meshColor = Colors.Red;
		RunCandidateFrame(buffComponent, adobeAnimateSprite);
		bool num = adobeAnimateSprite.meshColor == Colors.White;
		buffComponent.buffDictionary["runtime-probe"] = new TowerDefenseCharacterBuffConfig
		{
			key = "runtime-probe",
			character = _owners[0]
		};
		bool hasFrameUpdateWork = buffComponent.HasFrameUpdateWork;
		buffComponent.SetAlive(alive: false);
		bool flag = !buffComponent.HasFrameUpdateWork;
		buffComponent.SetAlive(alive: true);
		buffComponent.buffDictionary.Clear();
		if (num && hasFrameUpdateWork && flag)
		{
			return !buffComponent.HasFrameUpdateWork;
		}
		return false;
	}

	private void Warmup()
	{
		for (int i = 0; i < 240; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				_components[j].BuffUpdate(1.0 / 60.0);
			}
			for (int k = 0; k < 1000; k++)
			{
				RunCandidateFrame(_components[k], _sprites[k]);
			}
		}
		OptimizationBatchSampler.PrepareForWarmup();
	}

	private OptimizationBatchResult BenchmarkBaseline()
	{
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int i = 0; i < 1200; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			for (int j = 0; j < 1000; j++)
			{
				_components[j].BuffUpdate(1.0 / 60.0);
			}
			optimizationBatchSampler.EndSample(startTicks);
		}
		return optimizationBatchSampler.Complete();
	}

	private OptimizationBatchResult BenchmarkCandidate()
	{
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int i = 0; i < 1200; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			for (int j = 0; j < 1000; j++)
			{
				RunCandidateFrame(_components[j], _sprites[j]);
			}
			optimizationBatchSampler.EndSample(startTicks);
		}
		return optimizationBatchSampler.Complete();
	}

	private static void RunCandidateFrame(BuffComponent component, AdobeAnimateSprite sprite)
	{
		if (component != null && component.HasFrameUpdateWork)
		{
			component.BuffUpdate(1.0 / 60.0);
		}
		else if (sprite.meshColor != Colors.White)
		{
			sprite.meshColor = Colors.White;
		}
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			BuffIdleHotPathProbeOwner obj = _owners[i];
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
			BuffComponent obj = _components[i];
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
			_sprites[i]?.Free();
			_owners[i]?.Free();
			_components[i] = null;
			_owners[i] = null;
			_sprites[i] = null;
			_managers[i] = null;
		}
		_definition?.Dispose();
		_definition = null;
	}
}
