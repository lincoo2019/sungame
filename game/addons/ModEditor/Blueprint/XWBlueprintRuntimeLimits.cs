namespace PVZHE.ModEditor.Blueprint;

public sealed class XWBlueprintRuntimeLimits
{
	public int MaxSteps { get; init; } = 2048;

	public int MaxLoopIterations { get; init; } = 256;

	public int MaxCollectionItems { get; init; } = 1024;

	public int MaxStringLength { get; init; } = 16384;

	public int MaxOutputMessages { get; init; } = 128;

	public int MaxNodeVisitsPerNode { get; init; } = 512;

	public int MaxExecutionMilliseconds { get; init; } = 2000;

	public int MaxValueDepth { get; init; } = 16;

	public int MaxClonedValues { get; init; } = 8192;

	public int MaxCallArguments { get; init; } = 32;

	public int MaxCallDepth { get; init; } = 16;
}
