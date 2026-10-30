using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class NpcTalkConfig : Resource
{
	private Json _data;

	[Export(PropertyHint.None, "")]
	public string saveKey = "";

	[Export(PropertyHint.None, "")]
	public Array<NpcTalkBaseConfig> talkList = new Array<NpcTalkBaseConfig>();

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
		talkList.Clear();
		if (GodotObject.IsInstanceValid(data))
		{
			Load(data.Data.AsGodotDictionary());
		}
	}

	public void Load(Dictionary _data)
	{
		talkList.Clear();
		saveKey = _data.GetValueOrDefault("SaveKey", "").AsString();
		Array array = _data.GetValueOrDefault("Talk", new Array()).AsGodotArray();
		for (int i = 0; i < array.Count; i++)
		{
			Dictionary dictionary = array[i].AsGodotDictionary();
			switch (dictionary.GetValueOrDefault("Mode", "Default").AsString())
			{
			case "Default":
			{
				NpcTalkBaseConfig npcTalkBaseConfig = new NpcTalkBaseConfig();
				npcTalkBaseConfig.Init(dictionary);
				talkList.Add(npcTalkBaseConfig);
				break;
			}
			case "Hand":
			{
				NpcTalkHandConfig npcTalkHandConfig = new NpcTalkHandConfig();
				npcTalkHandConfig.Init(dictionary);
				talkList.Add(npcTalkHandConfig);
				break;
			}
			case "Tutorial":
			{
				NpcTalkTutorialConfig npcTalkTutorialConfig = new NpcTalkTutorialConfig();
				npcTalkTutorialConfig.Init(dictionary);
				talkList.Add(npcTalkTutorialConfig);
				break;
			}
			}
		}
	}
}
