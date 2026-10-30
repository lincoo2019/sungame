using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TutorialStepConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool broadCastUse;

	[Export(PropertyHint.None, "")]
	public BroadCastConfig broadCastConfig;

	[Export(PropertyHint.None, "")]
	public Array<TutorialConditionConfig> conditionList = new Array<TutorialConditionConfig>();

	public void Init(Dictionary data)
	{
		broadCastUse = false;
		broadCastConfig = null;
		conditionList.Clear();
		if (data.ContainsKey("BroadCast"))
		{
			broadCastUse = true;
			broadCastConfig = new BroadCastConfig();
			Dictionary dictionary = data["BroadCast"].AsGodotDictionary();
			broadCastConfig.broadCastString = dictionary.GetValueOrDefault("Text", "").AsString();
			Variant valueOrDefault = dictionary.GetValueOrDefault("Time", -1.0);
			broadCastConfig.broadCastTime = ((valueOrDefault.VariantType == Variant.Type.Nil) ? (-1.0) : valueOrDefault.AsDouble());
		}
		foreach (Variant item in data.GetValueOrDefault("Condition", new Array()).AsGodotArray())
		{
			Dictionary dictionary2 = item.AsGodotDictionary();
			TutorialConditionConfig condition = TutorialEnum.GetCondition(dictionary2["Name"].AsString());
			condition.Init(dictionary2["Data"].AsGodotDictionary());
			conditionList.Add(condition);
		}
	}

	public void Enter()
	{
		foreach (TutorialConditionConfig condition in conditionList)
		{
			condition.Enter();
		}
	}

	public void Exit()
	{
	}

	public bool Step()
	{
		foreach (TutorialConditionConfig condition in conditionList)
		{
			if (!condition.Step())
			{
				return false;
			}
		}
		return true;
	}
}
