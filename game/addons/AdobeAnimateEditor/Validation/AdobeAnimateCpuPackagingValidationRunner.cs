using System;
using Godot;

public partial class AdobeAnimateCpuPackagingValidationRunner : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_CPU_PACKAGING_VALIDATION_RESULT";

	public override void _Ready()
	{
		try
		{
			ParseArguments(out var reportPath, out var fault);
			AdobeAnimateCpuPackagingReport adobeAnimateCpuPackagingReport = AdobeAnimateCpuPackagingValidator.ValidateProject(new AdobeAnimateCpuValidationOptions(reportPath, fault));
			GD.Print($"{"ADOBE_ANIMATE_CPU_PACKAGING_VALIDATION_RESULT"} passed={adobeAnimateCpuPackagingReport.Passed} definitions={adobeAnimateCpuPackagingReport.DefinitionCount} scenes={adobeAnimateCpuPackagingReport.SceneCount} clips={adobeAnimateCpuPackagingReport.ClipCount} frames={adobeAnimateCpuPackagingReport.FrameCount} expectedVisibleItems={adobeAnimateCpuPackagingReport.ExpectedVisibleItems} cpuMeshItems={adobeAnimateCpuPackagingReport.CpuMeshItems} nativeSpriteItems={adobeAnimateCpuPackagingReport.NativeSpriteItems} cpuFallbackRoots={adobeAnimateCpuPackagingReport.CpuFallbackRoots} poseArrayDefinitions={adobeAnimateCpuPackagingReport.PoseArrayDefinitions} renderGraphs={adobeAnimateCpuPackagingReport.RenderGraphCount} maxRenderSlots={adobeAnimateCpuPackagingReport.MaxRenderSlots} maxMeshCapacity={adobeAnimateCpuPackagingReport.MaxMeshCapacity} stateTexels={adobeAnimateCpuPackagingReport.StateTexels} managedVisualItems={adobeAnimateCpuPackagingReport.ManagedVisualItems} nativeBehindItems={adobeAnimateCpuPackagingReport.NativeBehindItems} nativeFrontItems={adobeAnimateCpuPackagingReport.NativeFrontItems} cpuValidationFailures={adobeAnimateCpuPackagingReport.CpuValidationFailures} errors={adobeAnimateCpuPackagingReport.Errors.Count} inputSignature={adobeAnimateCpuPackagingReport.InputSignature}");
			for (int i = 0; i < adobeAnimateCpuPackagingReport.Errors.Count; i++)
			{
				AdobeAnimateCpuValidationError adobeAnimateCpuValidationError = adobeAnimateCpuPackagingReport.Errors[i];
				GD.PrintErr($"{"ADOBE_ANIMATE_CPU_PACKAGING_VALIDATION_RESULT"} error code={adobeAnimateCpuValidationError.FailureCode} frame={adobeAnimateCpuValidationError.Frame} resource={adobeAnimateCpuValidationError.ResourcePath} clip={adobeAnimateCpuValidationError.Clip} detail={adobeAnimateCpuValidationError.Detail}");
			}
			GetTree().Quit((!adobeAnimateCpuPackagingReport.Passed) ? 1 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_CPU_PACKAGING_VALIDATION_RESULT"} runner-error={value}");
			GetTree().Quit(2);
		}
	}

	private static void ParseArguments(out string reportPath, out AdobeAnimateCpuValidationFault fault)
	{
		reportPath = "res://.godot/adobe-animate-cpu-validation/report.json";
		fault = AdobeAnimateCpuValidationFault.None;
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			if (text.StartsWith("--report=", StringComparison.Ordinal))
			{
				string text2 = text;
				int length = "--report=".Length;
				reportPath = text2.Substring(length, text2.Length - length);
			}
			else if (text.StartsWith("--fault=", StringComparison.Ordinal))
			{
				string text2 = text;
				int length = "--fault=".Length;
				string text3 = text2.Substring(length, text2.Length - length);
				fault = text3 switch
				{
					"none" => AdobeAnimateCpuValidationFault.None, 
					"missing-frame" => AdobeAnimateCpuValidationFault.MissingFrame, 
					"missing-texture" => AdobeAnimateCpuValidationFault.MissingTexture, 
					"invalid-atlas-page" => AdobeAnimateCpuValidationFault.InvalidAtlasPage, 
					"invalid-pose-array" => AdobeAnimateCpuValidationFault.InvalidPoseArray, 
					"invalid-render-graph" => AdobeAnimateCpuValidationFault.InvalidRenderGraph, 
					"render-slot-capacity" => AdobeAnimateCpuValidationFault.RenderSlotCapacity, 
					"unclassified-visual" => AdobeAnimateCpuValidationFault.UnclassifiedVisual, 
					"native-order-interleave-unsupported" => AdobeAnimateCpuValidationFault.NativeOrderInterleaveUnsupported, 
					"suspended-reference" => AdobeAnimateCpuValidationFault.SuspendedReference, 
					_ => throw new ArgumentException("Unknown Adobe Animate CPU validation fault: " + text3), 
				};
			}
		}
		if (string.IsNullOrWhiteSpace(reportPath))
		{
			throw new ArgumentException("--report must not be empty.");
		}
	}
}
