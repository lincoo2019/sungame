using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventCurrentMapFunctionExecute : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public string functionName = "";

	[Export(PropertyHint.None, "")]
	public Array value = new Array();

	public override string _GetName()
	{
		return "LEVLE_EVENT_CURRENTMAP_FUNCTION_EXECUTE";
	}

	public override void Execute()
	{
		TowerDefenseMap currentMap = TowerDefenseManager.Instance.GetCurrentMap();
		if (GodotObject.IsInstanceValid(currentMap))
		{
			currentMap.FunctionExecute(functionName, value);
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (mapFeature == null)
		{
			return;
		}
		string capturedName = functionName;
		Array capturedValue = value.Duplicate(deep: true);
		mapFeature.TryEnqueuePendingMapAction(delegate
		{
			TowerDefenseMap currentMap2 = TowerDefenseManager.Instance.GetCurrentMap();
			if (GodotObject.IsInstanceValid(currentMap2))
			{
				currentMap2.FunctionExecute(capturedName, capturedValue);
			}
		});
	}

	public override void Init(Dictionary valueDictionary)
	{
		functionName = valueDictionary.GetValueOrDefault("FunctionName", "").AsString();
		value = (valueDictionary.ContainsKey("Value") ? ((Array)valueDictionary["Value"]) : new Array());
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "CurrentMapFunctionExecute",
			["Value"] = new Dictionary
			{
				["FunctionName"] = functionName,
				["Value"] = value
			}
		};
	}
}
