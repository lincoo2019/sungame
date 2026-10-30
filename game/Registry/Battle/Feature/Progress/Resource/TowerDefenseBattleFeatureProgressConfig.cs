using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureProgressConfig : Resource
{
	public const string DefaultProgressText = "{value}/{max}";

	[Export(PropertyHint.None, "")]
	public TowerDefenseProgressMode mode;

	[Export(PropertyHint.None, "")]
	public string levelName = "";

	[Export(PropertyHint.None, "")]
	public string difficultyText = "";

	[Export(PropertyHint.None, "")]
	public string survivalText = "";

	[Export(PropertyHint.None, "")]
	public string progressText = "{value}/{max}";

	[Export(PropertyHint.None, "")]
	public double progressValue;

	[Export(PropertyHint.None, "")]
	public double progressMax = 1.0;

	[Export(PropertyHint.None, "")]
	public double autoProgressResponse = -1.0;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProgressVisibility progressItemsVisibility;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProgressVisibility difficultyVisibility;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProgressVisibility levelNameVisibility;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProgressVisibility survivalVisibility;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProgressVisibility progressVisibility;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProgressVisibility progressTextVisibility;

	public bool HasProgressText { get; private set; }

	public bool HasHideProgressItems
	{
		get
		{
			if (progressItemsVisibility == TowerDefenseProgressVisibility.Auto)
			{
				return mode == TowerDefenseProgressMode.Manual;
			}
			return true;
		}
	}

	public bool HideProgressItems => progressItemsVisibility switch
	{
		TowerDefenseProgressVisibility.Show => false, 
		TowerDefenseProgressVisibility.Hide => true, 
		_ => mode == TowerDefenseProgressMode.Manual, 
	};

	public void Init(Dictionary data)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		mode = ParseMode(data);
		levelName = ReadString(data, "LevelName");
		difficultyText = ReadString(data, "DifficultyText", "DifficultText");
		survivalText = ReadString(data, "SurvivalText");
		HasProgressText = data.ContainsKey("ProgressText");
		progressText = data.GetValueOrDefault("ProgressText", "{value}/{max}").AsString();
		progressValue = data.GetValueOrDefault("ProgressValue", 0.0).AsDouble();
		double num = data.GetValueOrDefault("ProgressMax", 1.0).AsDouble();
		progressMax = ((num > 0.0) ? num : 1.0);
		autoProgressResponse = data.GetValueOrDefault("AutoProgressResponse", -1.0).AsDouble();
		progressItemsVisibility = ReadVisibility(data, "ProgressItemsVisibility", true, "HideProgressItems");
		difficultyVisibility = ReadVisibility(data, "DifficultyVisibility", false, "ShowDifficulty", "ShowDifficult");
		levelNameVisibility = ReadVisibility(data, "LevelNameVisibility", false, "ShowLevelName");
		survivalVisibility = ReadVisibility(data, "SurvivalVisibility", false, "ShowSurvival");
		progressVisibility = ReadVisibility(data, "ProgressVisibility", false, "ShowProgress");
		progressTextVisibility = ReadVisibility(data, "ProgressTextVisibility", false, "ShowProgressText");
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Mode"] = mode.ToString(),
			["ManualProgress"] = mode == TowerDefenseProgressMode.Manual,
			["ProgressValue"] = progressValue,
			["ProgressMax"] = ((progressMax > 0.0) ? progressMax : 1.0)
		};
		if (autoProgressResponse >= 0.0)
		{
			dictionary["AutoProgressResponse"] = autoProgressResponse;
		}
		AddText(dictionary, "LevelName", levelName);
		if (!string.IsNullOrEmpty(difficultyText))
		{
			dictionary["DifficultyText"] = difficultyText;
			dictionary["DifficultText"] = difficultyText;
		}
		AddText(dictionary, "SurvivalText", survivalText);
		if (HasProgressText || progressText != "{value}/{max}" || progressTextVisibility != TowerDefenseProgressVisibility.Auto)
		{
			dictionary["ProgressText"] = progressText ?? "";
		}
		AddVisibility(dictionary, "HideProgressItems", progressItemsVisibility, invert: true);
		AddVisibility(dictionary, "ShowDifficulty", difficultyVisibility, invert: false);
		AddVisibility(dictionary, "ShowDifficult", difficultyVisibility, invert: false);
		AddVisibility(dictionary, "ShowLevelName", levelNameVisibility, invert: false);
		AddVisibility(dictionary, "ShowSurvival", survivalVisibility, invert: false);
		AddVisibility(dictionary, "ShowProgress", progressVisibility, invert: false);
		AddVisibility(dictionary, "ShowProgressText", progressTextVisibility, invert: false);
		AddVisibilityMode(dictionary, "ProgressItemsVisibility", progressItemsVisibility);
		AddVisibilityMode(dictionary, "DifficultyVisibility", difficultyVisibility);
		AddVisibilityMode(dictionary, "LevelNameVisibility", levelNameVisibility);
		AddVisibilityMode(dictionary, "SurvivalVisibility", survivalVisibility);
		AddVisibilityMode(dictionary, "ProgressVisibility", progressVisibility);
		AddVisibilityMode(dictionary, "ProgressTextVisibility", progressTextVisibility);
		return dictionary;
	}

	public static bool TryGetVisibility(TowerDefenseProgressVisibility visibility, out bool visible)
	{
		visible = visibility == TowerDefenseProgressVisibility.Show;
		return visibility != TowerDefenseProgressVisibility.Auto;
	}

	private static TowerDefenseProgressMode ParseMode(Dictionary data)
	{
		if (data.TryGetValue("Mode", out var value) && Enum.TryParse<TowerDefenseProgressMode>(value.AsString(), ignoreCase: true, out var result))
		{
			return result;
		}
		if (!data.GetValueOrDefault("ManualProgress", false).AsBool())
		{
			return TowerDefenseProgressMode.Auto;
		}
		return TowerDefenseProgressMode.Manual;
	}

	private static string ReadString(Dictionary data, params string[] keys)
	{
		foreach (string text in keys)
		{
			if (data.TryGetValue(text, out var value))
			{
				return value.AsString();
			}
		}
		return "";
	}

	private static TowerDefenseProgressVisibility ReadVisibility(Dictionary data, string modeKey, bool invert, params string[] keys)
	{
		if (data.TryGetValue(modeKey, out var value) && Enum.TryParse<TowerDefenseProgressVisibility>(value.AsString(), ignoreCase: true, out var result))
		{
			return result;
		}
		foreach (string text in keys)
		{
			if (data.TryGetValue(text, out var value2))
			{
				bool flag = value2.AsBool();
				if (invert)
				{
					flag = !flag;
				}
				if (!flag)
				{
					return TowerDefenseProgressVisibility.Hide;
				}
				return TowerDefenseProgressVisibility.Show;
			}
		}
		return TowerDefenseProgressVisibility.Auto;
	}

	private static void AddText(Dictionary data, string key, string value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			data[key] = value;
		}
	}

	private static void AddVisibility(Dictionary data, string key, TowerDefenseProgressVisibility visibility, bool invert)
	{
		if (TryGetVisibility(visibility, out var visible))
		{
			data[key] = (invert ? (!visible) : visible);
		}
	}

	private static void AddVisibilityMode(Dictionary data, string key, TowerDefenseProgressVisibility visibility)
	{
		if (visibility != TowerDefenseProgressVisibility.Auto)
		{
			data[key] = visibility.ToString();
		}
	}
}
