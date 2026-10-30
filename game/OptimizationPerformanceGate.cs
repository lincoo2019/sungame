using System;
using System.Globalization;

public static class OptimizationPerformanceGate
{
	public const int SchemaVersion = 1;

	public const int RequiredInstanceCount = 1000;

	public const int MinimumWarmupSamples = 240;

	public const int MinimumMeasuredSamples = 600;

	public const double StateP99BudgetMilliseconds = 0.2;

	public static bool IsBareResultPassed(in OptimizationBatchResult result, bool functionalPassed, int instanceCount, int warmupSamples, int activeInstances, int dispatchedInstances, in OptimizationResultIdentity identity, double budgetMilliseconds = 0.2)
	{
		if (functionalPassed && result.AllocatedBytes == 0L && result.P99Milliseconds < budgetMilliseconds && instanceCount == 1000 && activeInstances == 1000 && dispatchedInstances == 1000 && warmupSamples >= 240 && result.SampleCount >= 600 && identity.PhysicsHz > 0 && identity.RenderTargetFps >= 240 && double.IsFinite(result.MeanMilliseconds) && double.IsFinite(result.P50Milliseconds) && double.IsFinite(result.P95Milliseconds) && double.IsFinite(result.P99Milliseconds))
		{
			return double.IsFinite(result.MaximumMilliseconds);
		}
		return false;
	}

	public static string FormatBareResult(in OptimizationResultIdentity identity, in OptimizationBatchResult result, bool functionalPassed, bool passed, int instanceCount, int warmupSamples, int activeInstances, int dispatchedInstances, double budgetMilliseconds = 0.2)
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		int num = Math.Max(1, activeInstances);
		double num2 = result.P50Milliseconds * 1000.0 / (double)num;
		double num3 = result.P95Milliseconds * 1000.0 / (double)num;
		double num4 = result.P99Milliseconds * 1000.0 / (double)num;
		double num5 = result.MeanMilliseconds * 1000.0 / (double)num;
		double num6 = ((result.SampleCount > 0) ? ((double)result.AllocatedBytes / (double)result.SampleCount) : 0.0);
		double num7 = num6 / (double)num;
		return "OPTIMIZATION_BARE_RESULT " + $"schemaVersion={1} " + "task=" + NormalizeToken(identity.Task) + " workload=" + FormatWorkload(identity.Workload) + " roleId=" + NormalizeToken(identity.RoleId) + " scene=" + NormalizeToken(identity.Scene) + " componentTypeId=" + NormalizeToken(identity.ComponentTypeId) + " componentDefinitionId=" + NormalizeToken(identity.ComponentDefinitionId) + " componentInstanceId=" + NormalizeToken(identity.ComponentInstanceId) + " stateMachineDefinitionId=" + NormalizeToken(identity.StateMachineDefinitionId) + " stateId=" + NormalizeToken(identity.StateId) + " phase=" + NormalizeToken(identity.Phase) + " schedule=" + FormatSchedule(identity.Schedule) + " renderer=" + NormalizeToken(identity.Renderer) + " " + $"instances={instanceCount} activeInstances={activeInstances} " + $"dispatchedInstances={dispatchedInstances} " + $"warmupSamples={warmupSamples} " + $"samples={result.SampleCount} " + $"physicsHz={identity.PhysicsHz} " + $"renderTargetFps={identity.RenderTargetFps} " + "batchMeanMs=" + result.MeanMilliseconds.ToString("F6", invariantCulture) + " batchP50Ms=" + result.P50Milliseconds.ToString("F6", invariantCulture) + " batchP95Ms=" + result.P95Milliseconds.ToString("F6", invariantCulture) + " batchP99Ms=" + result.P99Milliseconds.ToString("F6", invariantCulture) + " batchMaxMs=" + result.MaximumMilliseconds.ToString("F6", invariantCulture) + " perOwnerMeanUs=" + num5.ToString("F6", invariantCulture) + " perOwnerP50Us=" + num2.ToString("F6", invariantCulture) + " perOwnerP95Us=" + num3.ToString("F6", invariantCulture) + " perOwnerP99Us=" + num4.ToString("F6", invariantCulture) + " " + $"overBudgetSamples={result.OverBudgetSamples} " + $"maxSpikeSample={result.MaximumSampleIndex + 1} " + $"allocatedBytes={result.AllocatedBytes} " + "allocatedBytesPerBatch=" + num6.ToString("F6", invariantCulture) + " allocatedBytesPerOwner=" + num7.ToString("F6", invariantCulture) + " " + $"gen0={result.Gen0Collections} gen1={result.Gen1Collections} " + $"gen2={result.Gen2Collections} " + $"functionalPassed={functionalPassed} " + "budgetMs=" + budgetMilliseconds.ToString("F3", invariantCulture) + " " + $"passed={passed}";
	}

	private static string FormatWorkload(OptimizationWorkloadKind workload)
	{
		return workload switch
		{
			OptimizationWorkloadKind.HeadlessContract => "headless_contract", 
			OptimizationWorkloadKind.BareFunction => "bare_function", 
			OptimizationWorkloadKind.BareComponent => "bare_component", 
			OptimizationWorkloadKind.RealCharacterIsolatedState => "real_character_isolated_state", 
			OptimizationWorkloadKind.RealCharacterEndToEnd => "real_character_e2e", 
			_ => "unknown", 
		};
	}

	private static string FormatSchedule(OptimizationScheduleKind schedule)
	{
		return schedule switch
		{
			OptimizationScheduleKind.BackToBack => "back_to_back", 
			OptimizationScheduleKind.PhysicsFrame => "physics_frame", 
			OptimizationScheduleKind.RenderFrame => "render_frame", 
			_ => "unknown", 
		};
	}

	private static string NormalizeToken(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "unknown";
		}
		return value.Trim().Replace(' ', '_').Replace('\t', '_')
			.Replace('\r', '_')
			.Replace('\n', '_');
	}
}
