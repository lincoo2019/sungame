using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TutorialConfig : Resource
{
	private Json _data;

	[Export(PropertyHint.None, "")]
	public string saveKey = "";

	[Export(PropertyHint.None, "")]
	public Array<TutorialStepConfig> step = new Array<TutorialStepConfig>();

	[Export(PropertyHint.None, "")]
	public Json data
	{
		get
		{
			return _data;
		}
		set
		{
			_data = value;
			Init();
			NotifyPropertyListChanged();
		}
	}

	public void Init()
	{
		saveKey = "";
		step.Clear();
		if (GodotObject.IsInstanceValid(data))
		{
			Load(data.Data.AsGodotDictionary());
		}
	}

	public void Load(Dictionary _data)
	{
		saveKey = _data.GetValueOrDefault("SaveKey", "").AsString();
		foreach (Variant item in _data.GetValueOrDefault("Step", new Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			TutorialStepConfig tutorialStepConfig = new TutorialStepConfig();
			tutorialStepConfig.Init(dictionary);
			step.Add(tutorialStepConfig);
		}
	}

	public TutorialStepConfig GetTutorialStep(int _step)
	{
		return step[_step];
	}

	public int GetStepNum()
	{
		return step.Count;
	}
}
