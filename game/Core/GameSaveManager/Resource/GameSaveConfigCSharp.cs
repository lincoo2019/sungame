using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GameSaveConfigCSharp : Resource
{
	[Export(PropertyHint.None, "")]
	public string userCurrent = "";

	[Export(PropertyHint.None, "")]
	public Array<string> userList = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Dictionary saveDictionary = new Dictionary();

	public void _Init()
	{
		userCurrent = "";
		userList = new Array<string>();
		saveDictionary = new Dictionary();
	}

	public void Init(Dictionary data)
	{
		userCurrent = data["Current"].AsString();
		Variant variant = Json.ParseString(data["List"].AsString());
		userList = new Array<string>();
		foreach (Variant item in variant.AsGodotArray())
		{
			userList.Add(item.AsString());
		}
		saveDictionary = Json.ParseString(data["Dictionary"].AsString()).AsGodotDictionary();
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["Current"] = userCurrent,
			["List"] = Json.Stringify(userList),
			["Dictionary"] = Json.Stringify(saveDictionary)
		};
	}

	public void InitUser(string user)
	{
		Dictionary dictionary = new Dictionary();
		TowerDefensePacketDictionaryInit(dictionary);
		FeatureDictionaryInit(dictionary);
		TutorialDictionaryInit(dictionary);
		LevelDictionaryInit(dictionary);
		KeyDictionaryInit(dictionary);
		userList.Add(user);
		saveDictionary[user] = dictionary;
		string path = "user://Csharp/Progress/" + user;
		if (!DirAccess.DirExistsAbsolute(path))
		{
			DirAccess.MakeDirRecursiveAbsolute(path);
		}
	}

	public void RenameUser(string user, string newName)
	{
		saveDictionary[newName] = saveDictionary[user];
		saveDictionary.Remove(user);
		userList.Remove(user);
		userList.Add(newName);
		string text = "user://Csharp/Progress/" + user + "/";
		string to = "user://Csharp/Progress/" + newName + "/";
		if (DirAccess.DirExistsAbsolute(text))
		{
			DirAccess.RenameAbsolute(text, to);
		}
	}

	public void DeleteUser(string user)
	{
		userList.Remove(user);
		saveDictionary.Remove(user);
		string text = "user://Csharp/Progress/" + user + "/";
		if (DirAccess.DirExistsAbsolute(text))
		{
			string[] filesAt = DirAccess.GetFilesAt(text);
			foreach (string text2 in filesAt)
			{
				DirAccess.RemoveAbsolute(text + text2);
			}
			DirAccess.RemoveAbsolute(text);
		}
	}

	public void TowerDefensePacketDictionaryInit(Dictionary dictionary)
	{
		dictionary["TowerDefensePacket"] = new Dictionary();
		Dictionary dictionary2 = GameSaveManager.TOWER_DEFENSE_PACKET_INIT.Data.AsGodotDictionary();
		foreach (Variant key in dictionary2.Keys)
		{
			dictionary["TowerDefensePacket"].AsGodotDictionary()[key] = TowerDefensePacketDictionaryInitData(dictionary2[key].AsBool());
		}
	}

	public Dictionary TowerDefensePacketDictionaryInitData(bool unlock)
	{
		return new Dictionary
		{
			["Unlock"] = unlock,
			["Love"] = false,
			["Key"] = new Dictionary { ["Custom"] = "" }
		};
	}

	public void FeatureDictionaryInit(Dictionary dictionary)
	{
		dictionary["Feature"] = new Dictionary();
		Dictionary dictionary2 = GameSaveManager.FEATURE_INIT.Data.AsGodotDictionary();
		foreach (Variant key in dictionary2.Keys)
		{
			dictionary["Feature"].AsGodotDictionary()[key] = dictionary2[key];
		}
	}

	public void TutorialDictionaryInit(Dictionary dictionary)
	{
		dictionary["Tutorial"] = new Dictionary();
		Dictionary dictionary2 = GameSaveManager.TUTORIAL_INIT.Data.AsGodotDictionary();
		foreach (Variant key in dictionary2.Keys)
		{
			dictionary["Tutorial"].AsGodotDictionary()[key] = dictionary2[key];
		}
	}

	public void LevelDictionaryInit(Dictionary dictionary)
	{
		dictionary["Level"] = new Dictionary();
		foreach (Variant key9 in GameSaveManager.LEVEL_INIT.Data.AsGodotDictionary().Keys)
		{
			dictionary["Level"].AsGodotDictionary()[key9] = new Dictionary
			{
				["Normal"] = false,
				["Difficult"] = false,
				["Ultimate"] = false,
				["Mower"] = false,
				["Key"] = new Dictionary
				{
					["Like"] = -1,
					["Played"] = 0,
					["Finish"] = 0
				}
			};
		}
	}

	public void KeyDictionaryInit(Dictionary dictionary)
	{
		dictionary["Key"] = new Dictionary();
		Dictionary dictionary2 = GameSaveManager.KEY_INIT.Data.AsGodotDictionary();
		foreach (Variant key in dictionary2.Keys)
		{
			dictionary["Key"].AsGodotDictionary()[key] = dictionary2[key];
		}
	}
}
