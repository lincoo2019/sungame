using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventMapChange : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public string mapName = "Frontlawn";

	[Export(PropertyHint.None, "")]
	public double duration;

	[Export(PropertyHint.None, "")]
	public double delay;

	public override string _GetName()
	{
		return "LEVLE_EVENT_MAP_CHANGE";
	}

	public override void Execute()
	{
		TowerDefenseManager.Instance.MapChange(mapName, duration, delay);
	}

	public override void Init(Dictionary valueDictionary)
	{
		mapName = valueDictionary.GetValueOrDefault("MapName", "").AsString();
		duration = valueDictionary.GetValueOrDefault("Duration", 0.0).AsDouble();
		delay = valueDictionary.GetValueOrDefault("Delay", 0.0).AsDouble();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "MapChange",
			["Value"] = new Dictionary
			{
				["MapName"] = mapName,
				["Duration"] = duration,
				["Delay"] = delay
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["改变地图"] = new Dictionary
		{
			["地图"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Enum",
				["Property"] = "mapName",
				["Hint"] = LevelEditorInformationEditor.Instance.mapDictionary,
				["Rest"] = "Frontlawn"
			},
			["改变时间"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Float",
				["Property"] = "duration",
				["Rest"] = 0.0
			},
			["延迟时间"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Float",
				["Property"] = "delay",
				["Rest"] = 0.0
			}
		};
		return property;
	}
}
