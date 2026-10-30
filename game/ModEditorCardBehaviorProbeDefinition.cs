using Godot;

public partial class ModEditorCardBehaviorProbeDefinition : CardBehaviorDefinition
{
	private sealed class ProbeRuntime : CardBehaviorRuntime
	{
		protected override void OnBound()
		{
			BoundCalls++;
		}

		public override void OnPressed()
		{
			PressedCalls++;
		}

		public override void OnUseSucceeded(TowerDefenseCharacter createdCharacter)
		{
			UseSucceededCalls++;
		}
	}

	[Export(PropertyHint.None, "")]
	public string eventName = "示例行为中文名";

	public static int CreateRuntimeCalls;

	public static int ModifyCostCalls;

	public static int BoundCalls;

	public static int PressedCalls;

	public static int UseSucceededCalls;

	public static void ResetCounters()
	{
		CreateRuntimeCalls = 0;
		ModifyCostCalls = 0;
		BoundCalls = 0;
		PressedCalls = 0;
		UseSucceededCalls = 0;
	}

	public override CardBehaviorRuntime CreateRuntime()
	{
		CreateRuntimeCalls++;
		return new ProbeRuntime();
	}

	public override void ModifyCost(ref CardCostContext context)
	{
		ModifyCostCalls++;
		context.Cost += 777;
	}
}
