using Godot;
using Godot.Collections;

public static class ExpressionUtil
{
	public static Variant EvaluateExpression(string context, Node stateChart, string expression, Variant defaultValue)
	{
		Expression expression2 = new Expression();
		Dictionary dictionary = ((!(stateChart is StateChart stateChart2)) ? ((Dictionary)stateChart.Get("_expression_properties")) : stateChart2._expressionProperties);
		Array array = new Array();
		foreach (Variant key in dictionary.Keys)
		{
			array.Add(key);
		}
		string[] array2 = new string[array.Count];
		for (int i = 0; i < array.Count; i++)
		{
			array2[i] = array[i].AsString();
		}
		if (expression2.Parse(expression, array2) != Error.Ok)
		{
			GD.PushError("(" + context + ") Expression parse error. Tried to parse expression: '" + expression + "' but got error: '" + expression2.GetErrorText() + "'");
			return defaultValue;
		}
		Array array3 = new Array();
		foreach (Variant item in array)
		{
			array3.Add(dictionary[item]);
		}
		Variant result = expression2.Execute(array3, null, showError: false);
		if (expression2.HasExecuteFailed())
		{
			GD.PushError("(" + context + ") Expression execute error. Tried to run expression: '" + expression + "' but got error: '" + expression2.GetErrorText() + "'");
			return defaultValue;
		}
		return result;
	}
}
