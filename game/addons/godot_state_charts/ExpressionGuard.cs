using Godot;

[GlobalClass]
[Icon("res://addons/godot_state_charts/expression_guard.svg")]
public partial class ExpressionGuard : Guard
{
	[Export(PropertyHint.Expression, "")]
	public string expression { get; set; } = "";

	public override bool IsSatisfied(Transition contextTransition, StateChartState contextState)
	{
		StateChart chart = contextState._chart;
		if (!GodotObject.IsInstanceValid(chart))
		{
			GD.PushError("Could not find root state chart node, cannot evaluate expression");
			return false;
		}
		Variant variant = ExpressionUtil.EvaluateExpression("guard in " + DebugUtil.PathOf(contextTransition), chart, expression, false);
		if (variant.VariantType != Variant.Type.Bool)
		{
			string[] obj = new string[5] { "Expression: ", expression, " result: ", null, null };
			Variant variant2 = variant;
			obj[3] = variant2.ToString();
			obj[4] = " is not a boolean. Returning false.";
			GD.PushError(string.Concat(obj));
			return false;
		}
		return variant.AsBool();
	}

	public override int GetSupportedTriggerTypes()
	{
		return 4;
	}
}
