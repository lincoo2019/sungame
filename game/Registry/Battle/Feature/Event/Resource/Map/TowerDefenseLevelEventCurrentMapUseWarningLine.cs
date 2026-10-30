using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventCurrentMapUseWarningLine : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public int column = 5;

	public override string _GetName()
	{
		return "LEVLE_EVENT_CURRENTMAP_USE_WARNINGLINE";
	}

	public override void Execute()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance.GetCurrentMap()))
		{
			TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
			if (mapFeature != null)
			{
				int capturedColumn = column;
				mapFeature.TryEnqueuePendingMapAction(delegate
				{
					ExecuteWithWarningLineFeature(capturedColumn);
				});
			}
		}
		else
		{
			ExecuteWithWarningLineFeature(column);
		}
	}

	private void ExecuteWithWarningLineFeature(int columnValue)
	{
		TowerDefenseBattleFeatureWarningLine warningLineFeature = GetWarningLineFeature();
		if (warningLineFeature == null)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				currentControl.AddFeature("WarningLine", new Dictionary());
				warningLineFeature = GetWarningLineFeature();
			}
		}
		if (GodotObject.IsInstanceValid(warningLineFeature))
		{
			warningLineFeature.AddWarningColumn(columnValue);
		}
	}

	public override void Init(Dictionary valueDictionary)
	{
		column = valueDictionary.GetValueOrDefault("Column", valueDictionary.GetValueOrDefault("Row", 5)).AsInt32();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "CurrentMapUseWarningLine",
			["Value"] = new Dictionary
			{
				["Column"] = column,
				["Row"] = column
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["使用警戒线"] = new Dictionary { ["列"] = new Dictionary
		{
			["Object"] = this,
			["Type"] = "Int",
			["Property"] = "column",
			["Rest"] = 5
		} };
		return property;
	}

	public TowerDefenseBattleFeatureWarningLine GetWarningLineFeature()
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (GodotObject.IsInstanceValid(currentControl))
		{
			return currentControl.GetFeature("WarningLine") as TowerDefenseBattleFeatureWarningLine;
		}
		return null;
	}
}
