using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventTipsPlay : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public string text = "";

	[Export(PropertyHint.None, "")]
	public double duration = 2.0;

	public override string _GetName()
	{
		return "LEVLE_EVENT_TIPS_PLAY";
	}

	public override void Execute()
	{
		TowerDefenseManager.Instance.TipsPlay(text, duration);
	}

	public override void Init(Dictionary valueDictionary)
	{
		text = valueDictionary.GetValueOrDefault("Text", "").AsString();
		duration = valueDictionary.GetValueOrDefault("Duration", 0.0).AsDouble();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "TipsPlay",
			["Value"] = new Dictionary
			{
				["Text"] = text,
				["Duration"] = duration
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["播放警告"] = new Dictionary
		{
			["文本"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "MultilineString",
				["Property"] = "text",
				["Rest"] = ""
			},
			["播放时间"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Float",
				["Property"] = "duration",
				["Rest"] = 2.0
			}
		};
		return property;
	}
}
