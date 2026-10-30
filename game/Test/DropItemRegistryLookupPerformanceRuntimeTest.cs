using System;
using System.Globalization;
using Godot;

public sealed partial class DropItemRegistryLookupPerformanceRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly byte[] _lookupKinds = new byte[1000];

	private readonly StringName[] _names = new StringName[1000];

	private readonly ObjectManagerConfig.OBJECT[] _ids = new ObjectManagerConfig.OBJECT[1000];

	private readonly int[] _coinObjectIds = new int[1000];

	private readonly DropItemConfig[] _expected = new DropItemConfig[1000];

	private long _sink;

	private int _checks;

	private int _failures;

	private bool _functionalPassed;

	private bool _baselineEquivalent;

	private OptimizationBatchResult _baselineResult;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		try
		{
			PrepareLookupRequests();
			ValidateLookupSemantics();
			_baselineResult = Measure(ExecuteLegacyBatch);
			OptimizationBatchResult result = Measure(ExecuteOptimizedBatch);
			bool flag = OptimizationPerformanceGate.IsBareResultPassed(in result, _functionalPassed, 1000, 240, 1000, 1000, CreateIdentity());
			GD.Print(FormatBaseline(in _baselineResult));
			GD.Print(OptimizationPerformanceGate.FormatBareResult(CreateIdentity(), in result, _functionalPassed, flag, 1000, 240, 1000, 1000));
			Check(flag, "Drop-item registry optimized lookups must pass the strict 1000-operation gate.");
		}
		catch (Exception ex)
		{
			_failures++;
			GD.PushError(ex.ToString());
		}
		finally
		{
			bool flag2 = _failures == 0;
			GD.Print($"DROP_ITEM_REGISTRY_LOOKUP_RESULT passed={flag2} checks={_checks} failures={_failures} functional={_functionalPassed} baselineEquivalent={_baselineEquivalent} sink={_sink}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
	}

	private void PrepareLookupRequests()
	{
		DropItemRegistry.Init();
		StringName stringName = new StringName("Sun");
		StringName stringName2 = new StringName("__drop_item_registry_lookup_missing__");
		DropItemConfig dropItem = DropItemRegistry.GetDropItem(stringName);
		DropItemConfig byId = DropItemRegistry.GetById(ObjectManagerConfig.OBJECT.COIN_GOLD);
		DropItemConfig byCoinObjectId = DropItemRegistry.GetByCoinObjectId(1);
		Check(dropItem != null && byId != null && byCoinObjectId != null, "Production drop-item registry fixtures must initialize.");
		for (int i = 0; i < 1000; i++)
		{
			byte b = (byte)(i % 3);
			bool flag = (i & 3) != 3;
			_lookupKinds[i] = b;
			switch (b)
			{
			case 0:
				_names[i] = (flag ? stringName : stringName2);
				_expected[i] = (flag ? dropItem : null);
				break;
			case 1:
				_ids[i] = (flag ? ObjectManagerConfig.OBJECT.COIN_GOLD : ObjectManagerConfig.OBJECT.NOONE);
				_expected[i] = (flag ? byId : null);
				break;
			default:
				_coinObjectIds[i] = (flag ? 1 : (-2147483648));
				_expected[i] = (flag ? byCoinObjectId : null);
				break;
			}
		}
	}

	private void ValidateLookupSemantics()
	{
		bool flag = true;
		bool flag2 = true;
		for (int i = 0; i < 1000; i++)
		{
			DropItemConfig dropItemConfig = Lookup(i, legacy: false);
			DropItemConfig dropItemConfig2 = Lookup(i, legacy: true);
			flag = flag && dropItemConfig == dropItemConfig2;
			flag2 &= dropItemConfig == _expected[i];
		}
		_baselineEquivalent = flag;
		_functionalPassed = flag && flag2;
		Check(flag, "TryGetValue lookups must preserve legacy hit and miss behavior.");
		Check(flag2, "All 1000 mixed production lookup requests must return the expected config identity.");
	}

	private OptimizationBatchResult Measure(Action dispatch)
	{
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			dispatch();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			dispatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		return optimizationBatchSampler.Complete();
	}

	private void ExecuteLegacyBatch()
	{
		long num = _sink;
		for (int i = 0; i < 1000; i++)
		{
			if (Lookup(i, legacy: true) != null)
			{
				num++;
			}
		}
		_sink = num;
	}

	private void ExecuteOptimizedBatch()
	{
		long num = _sink;
		for (int i = 0; i < 1000; i++)
		{
			if (Lookup(i, legacy: false) != null)
			{
				num++;
			}
		}
		_sink = num;
	}

	private DropItemConfig Lookup(int index, bool legacy)
	{
		return _lookupKinds[index] switch
		{
			0 => legacy ? DropItemRegistry.GetDropItemLegacyForTests(_names[index]) : DropItemRegistry.GetDropItem(_names[index]), 
			1 => legacy ? DropItemRegistry.GetByIdLegacyForTests(_ids[index]) : DropItemRegistry.GetById(_ids[index]), 
			_ => legacy ? DropItemRegistry.GetByCoinObjectIdLegacyForTests(_coinObjectIds[index]) : DropItemRegistry.GetByCoinObjectId(_coinObjectIds[index]), 
		};
	}

	private static OptimizationResultIdentity CreateIdentity()
	{
		return new OptimizationResultIdentity("drop-item-registry-single-probe", OptimizationWorkloadKind.BareFunction, "drop-item-config", "res://Test/DropItemRegistryLookupPerformanceRuntimeTest.tscn", "DropItemRegistry", "builtin-drop-item-configs", "mixed-name-id-coin-id", "none", "none", "steady-hit-and-miss", OptimizationScheduleKind.BackToBack, "headless-mobile", 60, 240);
	}

	private static string FormatBaseline(in OptimizationBatchResult result)
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		return "DROP_ITEM_REGISTRY_LEGACY_BASELINE " + $"instances={1000} activeInstances={1000} " + $"dispatchedInstances={1000} " + $"warmupSamples={240} " + $"samples={result.SampleCount} " + "batchMeanMs=" + result.MeanMilliseconds.ToString("F6", invariantCulture) + " batchP50Ms=" + result.P50Milliseconds.ToString("F6", invariantCulture) + " batchP95Ms=" + result.P95Milliseconds.ToString("F6", invariantCulture) + " batchP99Ms=" + result.P99Milliseconds.ToString("F6", invariantCulture) + " batchMaxMs=" + result.MaximumMilliseconds.ToString("F6", invariantCulture) + " " + $"overBudgetSamples={result.OverBudgetSamples} " + $"maxSpikeSample={result.MaximumSampleIndex + 1} " + $"allocatedBytes={result.AllocatedBytes} " + $"gen0={result.Gen0Collections} " + $"gen1={result.Gen1Collections} " + $"gen2={result.Gen2Collections} accepted=False";
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError(message);
		}
	}
}
