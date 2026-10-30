using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

public static class ResourceTextScriptMigration
{
	private static readonly Regex UidPathRegex = new Regex("uid=\"(uid://[^\"]+)\"\\s+path=\"(res://[^\"]+)\\.gd\"", RegexOptions.Compiled);

	private static readonly Regex PathOnlyRegex = new Regex("path=\"(res://[^\"]+)\\.gd\"", RegexOptions.Compiled);

	private static readonly Regex UidCSharpPathRegex = new Regex("uid=\"uid://[^\"]+\"\\s+path=\"(?<path>res://[^\"]+\\.cs)\"", RegexOptions.Compiled);

	private static readonly KeyValuePair<string, string>[] LevelEditorMovedCSharpScriptPaths = new KeyValuePair<string, string>[13]
	{
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/ConveyorBelt/Resource/TowerDefenseConveyorConfig.cs", "res://Resource/TowerDefense/Conveyor/TowerDefenseConveyorConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/Event/Resource/Map/TowerDefenseLevelEventCurrentMapUseStripe.cs", "res://Resource/TowerDefense/Level/Event/Map/TowerDefenseLevelEventCurrentMapUseStripe.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/Fog/Resource/TowerDefenseLevelFogManagerConfig.cs", "res://Resource/TowerDefense/Level/Fog/TowerDefenseLevelFogManagerConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Process/IZM/Resource/TowerDefenseLevelIZMManagerConfig.cs", "res://Resource/TowerDefense/Level/IZM/TowerDefenseLevelIZMManagerConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/LookStar/Resource/TowerDefenseLevelLookStarManagerConfig.cs", "res://Resource/TowerDefense/Level/LookStar/TowerDefenseLevelLookStarManagerConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/SeedBank/Resource/TowerDefenseLevelPacketConfig.cs", "res://Resource/TowerDefense/Level/Packet/TowerDefenseLevelPacketConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/PreSpawn/Resource/TowerDefenseLevelPreSpawnConfig.cs", "res://Resource/TowerDefense/Level/PreSpawn/TowerDefenseLevelPreSpawnConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/RainMode/Resource/TowerDefenseRainModeConfig.cs", "res://Resource/TowerDefense/Level/RainMode/TowerDefenseRainModeConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/Wave/Resource/TowerDefenseLevelSpawnConfig.cs", "res://Resource/TowerDefense/Level/Spawn/TowerDefenseLevelSpawnConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/Wave/Resource/TowerDefenseLevelSpawnDynamicConfig.cs", "res://Resource/TowerDefense/Level/Spawn/TowerDefenseLevelSpawnDynamicConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/Sun/Resource/TowerDefenseLevelSunManagerConfig.cs", "res://Resource/TowerDefense/Level/Sun/TowerDefenseLevelSunManagerConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/Wave/Resource/TowerDefenseLevelWaveConfig.cs", "res://Resource/TowerDefense/Level/Wave/TowerDefenseLevelWaveConfig.cs"),
		new KeyValuePair<string, string>("res://Registry/Battle/Feature/Wave/Resource/TowerDefenseLevelWaveManagerConfig.cs", "res://Resource/TowerDefense/Level/Wave/TowerDefenseLevelWaveManagerConfig.cs")
	};

	public static bool MigrateGdScriptResourceToCSharpIfNeeded(string filePath)
	{
		return MigrateScriptResourceIfNeeded(filePath, migrateMovedCSharpPaths: false);
	}

	public static bool MigrateLevelEditorResourceForLoadIfNeeded(string filePath)
	{
		return MigrateScriptResourceIfNeeded(filePath, migrateMovedCSharpPaths: true);
	}

	private static bool MigrateScriptResourceIfNeeded(string filePath, bool migrateMovedCSharpPaths)
	{
		if (string.IsNullOrEmpty(filePath) || !FileAccess.FileExists(filePath))
		{
			return false;
		}
		string text = filePath.GetExtension().ToLowerInvariant();
		if (text != "tres" && text != "tscn")
		{
			return false;
		}
		string fileAsString = FileAccess.GetFileAsString(filePath);
		if (string.IsNullOrEmpty(fileAsString))
		{
			return false;
		}
		if (!fileAsString.TrimStart().StartsWith("[gd_resource", StringComparison.Ordinal) && !fileAsString.TrimStart().StartsWith("[gd_scene", StringComparison.Ordinal))
		{
			return false;
		}
		string text2 = fileAsString;
		if (text2.Contains(".gd\""))
		{
			Dictionary<string, string> uidMap = new Dictionary<string, string>();
			text2 = UidPathRegex.Replace(text2, delegate(Match match)
			{
				string value2 = match.Groups[1].Value;
				string text5 = match.Groups[2].Value + ".cs";
				string value3 = ReadResourceUid(text5);
				if (!string.IsNullOrEmpty(value3))
				{
					uidMap[value2] = value3;
					return $"uid=\"{value3}\" path=\"{text5}\"";
				}
				return "path=\"" + text5 + "\"";
			});
			text2 = PathOnlyRegex.Replace(text2, delegate(Match match)
			{
				string text5 = match.Groups[1].Value + ".cs";
				return "path=\"" + text5 + "\"";
			});
			foreach (KeyValuePair<string, string> item in uidMap)
			{
				item.Deconstruct(out var key, out var value);
				string text3 = key;
				string text4 = value;
				text2 = text2.Replace("metadata/_custom_type_script = \"" + text3 + "\"", "metadata/_custom_type_script = \"" + text4 + "\"");
			}
		}
		if (migrateMovedCSharpPaths)
		{
			KeyValuePair<string, string>[] levelEditorMovedCSharpScriptPaths = LevelEditorMovedCSharpScriptPaths;
			for (int num = 0; num < levelEditorMovedCSharpScriptPaths.Length; num++)
			{
				KeyValuePair<string, string> keyValuePair = levelEditorMovedCSharpScriptPaths[num];
				text2 = text2.Replace(keyValuePair.Key, keyValuePair.Value);
			}
			text2 = UidCSharpPathRegex.Replace(text2, RemoveMovedCSharpScriptUid);
		}
		if (text2 == fileAsString)
		{
			return false;
		}
		FileAccess fileAccess = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			return false;
		}
		fileAccess.StoreString(text2);
		fileAccess.Close();
		return true;
	}

	private static string RemoveMovedCSharpScriptUid(Match match)
	{
		string value = match.Groups["path"].Value;
		KeyValuePair<string, string>[] levelEditorMovedCSharpScriptPaths = LevelEditorMovedCSharpScriptPaths;
		foreach (KeyValuePair<string, string> keyValuePair in levelEditorMovedCSharpScriptPaths)
		{
			if (value == keyValuePair.Value)
			{
				return "path=\"" + value + "\"";
			}
		}
		return match.Value;
	}

	private static string ReadResourceUid(string resPath)
	{
		string path = resPath + ".uid";
		if (!FileAccess.FileExists(path))
		{
			return null;
		}
		string text = FileAccess.GetFileAsString(path).Trim();
		if (!text.StartsWith("uid://", StringComparison.Ordinal))
		{
			return null;
		}
		return text;
	}
}
