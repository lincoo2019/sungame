using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseLevelNewConfig : TowerDefenseLevelBaseConfig
{
	private static class EnumCache<T> where T : struct, Enum
	{
		private static readonly System.Collections.Generic.Dictionary<string, T> _map = BuildMap();

		private static System.Collections.Generic.Dictionary<string, T> BuildMap()
		{
			System.Collections.Generic.Dictionary<string, T> dictionary = new System.Collections.Generic.Dictionary<string, T>(StringComparer.Ordinal);
			T[] values = Enum.GetValues<T>();
			for (int i = 0; i < values.Length; i++)
			{
				T value = values[i];
				dictionary[value.ToString().ToUpperInvariant()] = value;
			}
			return dictionary;
		}

		public static T Parse(string value, T defaultValue)
		{
			if (string.IsNullOrEmpty(value))
			{
				return defaultValue;
			}
			if (!_map.TryGetValue(value.ToUpperInvariant(), out var value2))
			{
				return defaultValue;
			}
			return value2;
		}
	}

	private Json _data;

	[Export(PropertyHint.None, "")]
	public StringName version = "1.0";

	[Export(PropertyHint.None, "")]
	public Godot.Collections.Dictionary<StringName, Dictionary> featureData = new Godot.Collections.Dictionary<StringName, Dictionary>();

	[Export(PropertyHint.None, "")]
	public StringName processName;

	[Export(PropertyHint.None, "")]
	public Dictionary processData = new Dictionary();

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
		}
	}

	private static bool HasProcessName(StringName value)
	{
		if (value != null)
		{
			return !value.IsEmpty;
		}
		return false;
	}

	private static Dictionary GetDict(Dictionary src, string key)
	{
		if (src != null && src.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Dictionary)
		{
			return value.AsGodotDictionary();
		}
		return new Dictionary();
	}

	private static Godot.Collections.Array GetArray(Dictionary src, string key)
	{
		if (src != null && src.TryGetValue(key, out var value) && value.VariantType == Variant.Type.Array)
		{
			return (Godot.Collections.Array)value;
		}
		return new Godot.Collections.Array();
	}

	public override void Init()
	{
		if (!GodotObject.IsInstanceValid(data))
		{
			return;
		}
		Dictionary dictionary = data.Data.AsGodotDictionary();
		name = dictionary.GetValueOrDefault("Name", "").AsString();
		levelName = dictionary.GetValueOrDefault("LevelName", "").AsString();
		description = dictionary.GetValueOrDefault("Description", "").AsString();
		levelNumber = dictionary.GetValueOrDefault("LevelNumber", 0).AsInt32();
		nextLevel = dictionary.GetValueOrDefault("NextLevel", "").AsString();
		homeWorld = EnumCache<GeneralEnum.HOMEWORLD>.Parse(dictionary.GetValueOrDefault("HomeWorld", "NOONE").AsString(), GeneralEnum.HOMEWORLD.NOONE);
		version = dictionary.GetValueOrDefault("Version", "1.0").AsString();
		Dictionary dict = GetDict(dictionary, "Process");
		if (dict.Count > 0)
		{
			processName = dict.GetValueOrDefault("Name", "").AsString();
			processData = GetDict(dict, "Data");
		}
		else
		{
			processName = "";
			processData = new Dictionary();
		}
		featureData.Clear();
		foreach (Variant item in GetArray(dictionary, "Feature"))
		{
			Dictionary dictionary2 = item.AsGodotDictionary();
			StringName key = new StringName(dictionary2.GetValueOrDefault("Name", "").AsString());
			Dictionary dict2 = GetDict(dictionary2, "Data");
			featureData[key] = dict2;
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Name"] = name,
			["LevelName"] = levelName,
			["Description"] = description,
			["LevelNumber"] = levelNumber,
			["NextLevel"] = nextLevel,
			["HomeWorld"] = Enum.GetName(typeof(GeneralEnum.HOMEWORLD), homeWorld),
			["Version"] = version,
			["Feature"] = new Godot.Collections.Array(),
			["Process"] = new Dictionary()
		};
		foreach (StringName key10 in featureData.Keys)
		{
			((Godot.Collections.Array)dictionary["Feature"]).Add(new Dictionary
			{
				["Name"] = key10,
				["Data"] = featureData[key10]
			});
		}
		if (HasProcessName(processName))
		{
			dictionary["Process"] = new Dictionary
			{
				["Name"] = processName,
				["Data"] = processData
			};
		}
		return dictionary;
	}
}
