using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardActionBehaviorChangeCost : CardActionBehaviorDefinition
{
	public enum METHOD
	{
		ADD,
		MULTIPLY
	}

	[Export(PropertyHint.None, "")]
	public METHOD method;

	[Export(PropertyHint.None, "")]
	public double value;

	[Export(PropertyHint.None, "")]
	public int _min = -1;

	[Export(PropertyHint.None, "")]
	public int _max = -1;

	public override void ImportConfiguration(Dictionary data)
	{
		if (Enum.TryParse<METHOD>(data.GetValueOrDefault("Method", "ADD").AsString().ToUpper(), out var result))
		{
			method = result;
		}
		value = data.GetValueOrDefault("Value", 0.0).AsDouble();
		_min = data.GetValueOrDefault("Min", -1).AsInt32();
		_max = data.GetValueOrDefault("Max", -1).AsInt32();
	}

	public override void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
		switch (method)
		{
		case METHOD.ADD:
			packet.baseItemCost += (long)value;
			break;
		case METHOD.MULTIPLY:
			packet.baseItemCost = (long)Mathf.Floor((double)packet.baseItemCost * value);
			break;
		}
		if (_min != -1 && packet.baseItemCost < _min)
		{
			packet.baseItemCost = _min;
		}
		if (_max != -1 && packet.baseItemCost > _max)
		{
			packet.baseItemCost = _max;
		}
	}

	public override Dictionary ExportConfiguration()
	{
		return new Dictionary
		{
			["ActionId"] = "ChangeCost",
			["Configuration"] = new Dictionary
			{
				["Method"] = Enum.GetName(typeof(METHOD), method),
				["Value"] = value,
				["Min"] = _min,
				["Max"] = _max
			}
		};
	}
}
