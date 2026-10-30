using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class CharacterCustomData : Resource
{
	private Array<CharacterCustomConfig> _customList = new Array<CharacterCustomConfig>();

	private Action _initAction;

	[Export(PropertyHint.None, "")]
	public Dictionary customDictionary = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Array<string> fliterOpenAll = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<string> fliterCloseAll = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<CharacterCustomConfig> customList
	{
		get
		{
			return _customList;
		}
		set
		{
			if (_initAction == null)
			{
				_initAction = Init;
			}
			foreach (CharacterCustomConfig custom in _customList)
			{
				if (custom != null && custom.IsConnected(Resource.SignalName.Changed, Callable.From(_initAction)))
				{
					custom.Changed -= _initAction;
				}
			}
			_customList = value;
			foreach (CharacterCustomConfig custom2 in _customList)
			{
				if (custom2 != null && !custom2.IsConnected(Resource.SignalName.Changed, Callable.From(_initAction)))
				{
					custom2.Changed += _initAction;
				}
			}
			Init();
			NotifyPropertyListChanged();
		}
	}

	public void Init()
	{
		fliterOpenAll.Clear();
		fliterCloseAll.Clear();
		customDictionary.Clear();
		foreach (CharacterCustomConfig custom in customList)
		{
			if (custom == null)
			{
				continue;
			}
			Array<string> array = SplitFliter(custom.animeFliterOpen);
			Array<string> array2 = SplitFliter(custom.animeFliterClose);
			customDictionary[custom.customName] = new Dictionary
			{
				["Config"] = custom,
				["Open"] = array,
				["Close"] = array2
			};
			if (array.Count > 0)
			{
				foreach (string item in array)
				{
					fliterOpenAll.Add(item);
				}
			}
			if (array2.Count <= 0)
			{
				continue;
			}
			foreach (string item2 in array2)
			{
				fliterCloseAll.Add(item2);
			}
		}
	}

	private Array<string> SplitFliter(string fliter)
	{
		Array<string> array = new Array<string>();
		if (string.IsNullOrEmpty(fliter))
		{
			return array;
		}
		string[] array2 = fliter.Split("&", StringSplitOptions.RemoveEmptyEntries);
		foreach (string item in array2)
		{
			array.Add(item);
		}
		return array;
	}

	public void ClearCustomFliters(AdobeAnimateSprite sprite)
	{
		sprite.SetFlitersRecursive((Godot.Collections.Array?)fliterOpenAll, open: false);
		sprite.SetFlitersRecursive((Godot.Collections.Array?)fliterCloseAll, open: true);
	}

	public void SetCustomFliters(AdobeAnimateSprite sprite, string customName)
	{
		if (customDictionary.ContainsKey(customName))
		{
			Dictionary dictionary = (Dictionary)customDictionary[customName];
			if (dictionary.ContainsKey("Open") && dictionary.ContainsKey("Close"))
			{
				Array<string> array = (Array<string>)dictionary["Open"];
				Array<string> array2 = (Array<string>)dictionary["Close"];
				sprite.SetFlitersRecursive((Godot.Collections.Array?)array, open: true);
				sprite.SetFlitersRecursive((Godot.Collections.Array?)array2, open: false);
			}
		}
	}

	public void SetDamagePoint(AdobeAnimateSprite sprite, string customName, int index)
	{
		if (!customDictionary.ContainsKey(customName))
		{
			return;
		}
		Dictionary dictionary = (Dictionary)customDictionary[customName];
		if (dictionary.ContainsKey("Config"))
		{
			CharacterCustomConfig characterCustomConfig = (CharacterCustomConfig)(GodotObject)dictionary["Config"];
			if (index >= 0 && index < characterCustomConfig.damagePointChangeMediaTexturePaths.Count)
			{
				sprite.SetAtlasReplace(characterCustomConfig.damagePointChangeMediaName, characterCustomConfig.damagePointChangeMediaTexturePaths[index]);
			}
		}
	}

	public void ClearDamagePoint(AdobeAnimateSprite sprite, string customName)
	{
		if (!customDictionary.ContainsKey(customName))
		{
			return;
		}
		Dictionary dictionary = (Dictionary)customDictionary[customName];
		if (dictionary.ContainsKey("Config"))
		{
			CharacterCustomConfig characterCustomConfig = (CharacterCustomConfig)(GodotObject)dictionary["Config"];
			if (characterCustomConfig.damagePointChangeMediaName != "")
			{
				sprite.SetAtlasReplace(characterCustomConfig.damagePointChangeMediaName, string.Empty);
			}
		}
	}
}
