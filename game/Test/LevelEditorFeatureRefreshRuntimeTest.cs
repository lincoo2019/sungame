using System;
using Godot;
using Godot.Collections;

public partial class LevelEditorFeatureRefreshRuntimeTest : Node
{
	private const int ExpectedChecks = 36;

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			VerifyEditedSnapshotsAreRemoved();
			VerifyFeatureSpecificOptionsSurviveEdits();
			VerifyUntouchedExplicitSnapshotsArePreserved();
			VerifyFieldBackedProcessRefresh();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[LevelEditorFeatureRefreshRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 36;
		GD.Print($"LEVEL_EDITOR_FEATURE_REFRESH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifyEditedSnapshotsAreRemoved()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = LoadConfig(BuildMirroredLevelData());
		Check(towerDefenseLevelConfig.eventInit.Count == 1, "The mirrored fixture must expose its legacy EventInit entry.");
		Check(towerDefenseLevelConfig.preSpawnList.Count == 1, "The mirrored fixture must expose its legacy PreSpawn entry.");
		Check(towerDefenseLevelConfig.featureData.ContainsKey(new StringName("Event")), "The mirrored fixture must begin with an explicit Event feature.");
		Check(towerDefenseLevelConfig.featureData.ContainsKey(new StringName("PreSpawn")), "The mirrored fixture must begin with an explicit PreSpawn feature.");
		towerDefenseLevelConfig.eventInit.Clear();
		towerDefenseLevelConfig.eventReady.Clear();
		towerDefenseLevelConfig.eventStart.Clear();
		towerDefenseLevelConfig.MarkEventDataEditedFromEditor();
		towerDefenseLevelConfig.preSpawnList.Clear();
		towerDefenseLevelConfig.MarkPreSpawnDataEditedFromEditor();
		towerDefenseLevelConfig.PrepareForEditorSave();
		Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig.data), "Preparing an editor-owned change must replace the imported JSON snapshot.");
		Check(FindFeature(towerDefenseLevelConfig.data.Data.AsGodotDictionary(), "Event") == null && FindFeature(towerDefenseLevelConfig.data.Data.AsGodotDictionary(), "PreSpawn") == null, "The refreshed editor JSON snapshot must not retain deleted domains.");
		Check(!towerDefenseLevelConfig.featureData.ContainsKey(new StringName("Event")), "An editor-cleared Event domain must remove the old Event feature snapshot.");
		Check(!towerDefenseLevelConfig.featureData.ContainsKey(new StringName("PreSpawn")), "An editor-cleared PreSpawn domain must remove the old PreSpawn feature snapshot.");
		Check(towerDefenseLevelConfig.featureData.TryGetValue(new StringName("CustomEditorFeature"), out var value) && value.GetValueOrDefault("KeepMarker", 0).AsInt32() == 47, "Editor synchronization must preserve unrelated custom features.");
		Check(towerDefenseLevelConfig.processName == (StringName)"Wave" && towerDefenseLevelConfig.processData.GetValueOrDefault("CustomProcessMarker", 0).AsInt32() == 77, "Editor synchronization must preserve untouched explicit process data.");
		Check(!towerDefenseLevelConfig.processData.GetValueOrDefault("StormOpen", true).AsBool() && towerDefenseLevelConfig.processData.GetValueOrDefault("MowerUse", false).AsBool(), "Editor synchronization must refresh generated Process fields instead of restoring stale explicit values.");
		Dictionary dictionary = towerDefenseLevelConfig.Export();
		Check(FindFeature(dictionary, "Event") == null, "JSON export must not resurrect an editor-cleared Event feature.");
		Check(FindFeature(dictionary, "PreSpawn") == null, "JSON export must not resurrect an editor-cleared PreSpawn feature.");
		Dictionary dictionary2 = FindFeature(dictionary, "CustomEditorFeature");
		Check(dictionary2 != null && dictionary2.GetValueOrDefault("KeepMarker", 0).AsInt32() == 47, "JSON export must retain unrelated custom features.");
		Check(FindFeature(dictionary, "Map")?.GetValueOrDefault("MapName", "").AsString() == "FrontlawnSuperBig", "Editor preparation must replace a stale Map Feature with the selected top-level map.");
		Check(GetEventArray(dictionary, "EventInit").Count == 0 && GetEventArray(dictionary, "EventReady").Count == 0 && GetEventArray(dictionary, "EventStart").Count == 0, "JSON export must keep the legacy event mirrors empty.");
		Check(dictionary.GetValueOrDefault("PreSpawn", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Packet", new Godot.Collections.Array())
			.AsGodotArray()
			.Count == 0, "JSON export must keep the legacy PreSpawn mirror empty.");
		Error error = ResourceSaver.Save(towerDefenseLevelConfig, "user://level_editor_feature_refresh.tres", ResourceSaver.SaverFlags.None);
		Check(error == Error.Ok, $"The prepared editor resource must save successfully: {error}.");
		TowerDefenseLevelConfig towerDefenseLevelConfig2 = ResourceLoader.Load<TowerDefenseLevelConfig>("user://level_editor_feature_refresh.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
		Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig2), "The prepared editor resource must reload.");
		if (GodotObject.IsInstanceValid(towerDefenseLevelConfig2))
		{
			Check(!towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("Event")), "The saved .tres must not retain the deleted Event feature.");
			Check(!towerDefenseLevelConfig2.featureData.ContainsKey(new StringName("PreSpawn")), "The saved .tres must not retain the deleted PreSpawn feature.");
			Check(towerDefenseLevelConfig2.eventInit.Count == 0 && towerDefenseLevelConfig2.eventReady.Count == 0 && towerDefenseLevelConfig2.eventStart.Count == 0 && towerDefenseLevelConfig2.preSpawnList.Count == 0, "The saved .tres must retain the cleared legacy editor arrays.");
			Dictionary exportedLevel = towerDefenseLevelConfig2.Export();
			Check(FindFeature(exportedLevel, "Event") == null && FindFeature(exportedLevel, "PreSpawn") == null, "A reload followed by another test/export refresh must not resurrect deleted data.");
			Dictionary dictionary3 = FindFeature(exportedLevel, "CustomEditorFeature");
			Check(dictionary3 != null && dictionary3.GetValueOrDefault("KeepMarker", 0).AsInt32() == 47, "A reload followed by export must still preserve unrelated custom features.");
		}
	}

	private void VerifyFeatureSpecificOptionsSurviveEdits()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = LoadConfig(BuildMirroredLevelData());
		towerDefenseLevelConfig.featureData[new StringName("Event")]["EventEntry"] = new Godot.Collections.Array
		{
			new Dictionary
			{
				["EventName"] = "TipsPlay",
				["Value"] = new Dictionary
				{
					["Text"] = "hidden entry",
					["Duration"] = 1.0
				}
			}
		};
		towerDefenseLevelConfig.featureData[new StringName("PreSpawn")]["MaxRetryPasses"] = 13;
		towerDefenseLevelConfig.eventInit.Clear();
		towerDefenseLevelConfig.MarkEventDataEditedFromEditor();
		towerDefenseLevelConfig.preSpawnList.Clear();
		towerDefenseLevelConfig.MarkPreSpawnDataEditedFromEditor();
		towerDefenseLevelConfig.PrepareForEditorSave();
		Dictionary dictionary = towerDefenseLevelConfig.featureData[new StringName("Event")];
		Check(dictionary.GetValueOrDefault("EventInit", new Godot.Collections.Array()).AsGodotArray().Count == 0 && dictionary.GetValueOrDefault("EventEntry", new Godot.Collections.Array()).AsGodotArray().Count == 1, "Editing visible events must preserve the hidden EventEntry phase.");
		Dictionary dictionary2 = towerDefenseLevelConfig.featureData[new StringName("PreSpawn")];
		Check(!dictionary2.ContainsKey("Packet") && dictionary2.GetValueOrDefault("MaxRetryPasses", 0).AsInt32() == 13, "Editing placements must preserve non-Packet PreSpawn options.");
		Dictionary exportedLevel = towerDefenseLevelConfig.data.Data.AsGodotDictionary();
		Dictionary dictionary3 = FindFeature(exportedLevel, "Event");
		int condition;
		if (dictionary3 != null && dictionary3.GetValueOrDefault("EventEntry", new Godot.Collections.Array()).AsGodotArray().Count == 1)
		{
			Dictionary dictionary4 = FindFeature(exportedLevel, "PreSpawn");
			condition = ((dictionary4 != null && dictionary4.GetValueOrDefault("MaxRetryPasses", 0).AsInt32() == 13) ? 1 : 0);
		}
		else
		{
			condition = 0;
		}
		Check((byte)condition != 0, "The refreshed JSON snapshot must retain Feature-specific options.");
	}

	private void VerifyUntouchedExplicitSnapshotsArePreserved()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = LoadConfig(BuildFeatureOnlyLevelData());
		Check(towerDefenseLevelConfig.eventInit.Count == 0 && towerDefenseLevelConfig.preSpawnList.Count == 0, "The Feature-only fixture must not rely on legacy mirror arrays.");
		towerDefenseLevelConfig.PrepareForEditorSave();
		Dictionary dictionary = towerDefenseLevelConfig.Export();
		Dictionary dictionary2 = FindFeature(dictionary, "Event");
		Check(dictionary2 != null && dictionary2.GetValueOrDefault("EventInit", new Godot.Collections.Array()).AsGodotArray().Count == 1, "Untouched Feature-only Event data must remain lossless.");
		Dictionary dictionary3 = FindFeature(dictionary, "PreSpawn");
		Check(dictionary3 != null && dictionary3.GetValueOrDefault("Packet", new Godot.Collections.Array()).AsGodotArray().Count == 1, "Untouched Feature-only PreSpawn data must remain lossless.");
		Check(dictionary.GetValueOrDefault("Process", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Data", new Dictionary())
			.AsGodotDictionary()
			.GetValueOrDefault("CustomProcessMarker", 0)
			.AsInt32() == 77, "Untouched Feature-only process data must remain lossless.");
		Check(FindFeature(dictionary, "Map")?.GetValueOrDefault("MapName", "").AsString() == "FrontlawnSuperBig", "The selected top-level map must remain authoritative over a stale explicit Map Feature.");
	}

	private void VerifyFieldBackedProcessRefresh()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig
		{
			finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE,
			waveManager = new TowerDefenseLevelWaveManagerConfig(),
			processName = "",
			processData = new Dictionary()
		};
		Check(towerDefenseLevelConfig.SetMapFromEditor("FrontlawnSuperBig") && towerDefenseLevelConfig.map == "FrontlawnSuperBig", "The in-game editor map setter must update the top-level map.");
		towerDefenseLevelConfig.PrepareForEditorSave();
		Check(towerDefenseLevelConfig.featureData.TryGetValue(new StringName("Map"), out var value) && value.GetValueOrDefault("MapName", "").AsString() == "FrontlawnSuperBig", "The in-game editor map setter must update the battle Map Feature.");
		Check(towerDefenseLevelConfig.processName == (StringName)"Wave", "Preparing a field-backed editor resource must refresh its Process name.");
		Check(towerDefenseLevelConfig.processData.ContainsKey("StormOpen") && towerDefenseLevelConfig.processData.ContainsKey("MowerUse"), "Preparing a field-backed editor resource must refresh Process orchestration data.");
	}

	private static Dictionary BuildMirroredLevelData()
	{
		Dictionary dictionary = BuildEventData();
		Dictionary dictionary2 = BuildPreSpawnData();
		return BuildBaseLevelData(dictionary.Duplicate(deep: true), dictionary2.Duplicate(deep: true), dictionary, dictionary2);
	}

	private static Dictionary BuildFeatureOnlyLevelData()
	{
		return BuildBaseLevelData(null, null, BuildEventData(), BuildPreSpawnData());
	}

	private static Dictionary BuildBaseLevelData(Dictionary legacyEvent, Dictionary legacyPreSpawn, Dictionary eventFeature, Dictionary preSpawnFeature)
	{
		Dictionary dictionary = new Dictionary
		{
			["Name"] = "LevelEditorFeatureRefreshProbe",
			["Map"] = "FrontlawnSuperBig",
			["FinishMethod"] = "WAVE",
			["PacketBank"] = new Dictionary
			{
				["Method"] = "CHOOSE",
				["Value"] = new Godot.Collections.Array()
			},
			["Feature"] = new Godot.Collections.Array
			{
				new Dictionary
				{
					["Name"] = "Map",
					["Data"] = new Dictionary { ["MapName"] = "FrontlawnNight" }
				},
				new Dictionary
				{
					["Name"] = "Event",
					["Data"] = eventFeature
				},
				new Dictionary
				{
					["Name"] = "PreSpawn",
					["Data"] = preSpawnFeature
				},
				new Dictionary
				{
					["Name"] = "CustomEditorFeature",
					["Data"] = new Dictionary { ["KeepMarker"] = 47 }
				}
			},
			["Process"] = new Dictionary
			{
				["Name"] = "Wave",
				["Data"] = new Dictionary
				{
					["CustomProcessMarker"] = 77,
					["StormOpen"] = true,
					["MowerUse"] = false
				}
			}
		};
		if (legacyEvent != null)
		{
			dictionary["Event"] = legacyEvent;
		}
		if (legacyPreSpawn != null)
		{
			dictionary["PreSpawn"] = legacyPreSpawn;
		}
		return dictionary;
	}

	private static Dictionary BuildEventData()
	{
		return new Dictionary
		{
			["EventInit"] = new Godot.Collections.Array
			{
				new Dictionary
				{
					["EventName"] = "TipsPlay",
					["Value"] = new Dictionary
					{
						["Text"] = "stale event",
						["Duration"] = 1.0
					}
				}
			},
			["EventEntry"] = new Godot.Collections.Array(),
			["EventReady"] = new Godot.Collections.Array(),
			["EventStart"] = new Godot.Collections.Array()
		};
	}

	private static Dictionary BuildPreSpawnData()
	{
		return new Dictionary { ["Packet"] = new Godot.Collections.Array
		{
			new Dictionary
			{
				["Name"] = "PlantPeaShooterSingle",
				["GridPos"] = new Godot.Collections.Array { 1, 1 }
			}
		} };
	}

	private static TowerDefenseLevelConfig LoadConfig(Dictionary data)
	{
		Json json = new Json();
		Error error = json.Parse(Json.Stringify(data));
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Unable to parse focused level JSON: {error}");
		}
		return new TowerDefenseLevelConfig
		{
			data = json
		};
	}

	private static Dictionary FindFeature(Dictionary exportedLevel, string name)
	{
		foreach (Variant item in exportedLevel.GetValueOrDefault("Feature", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			if (dictionary.GetValueOrDefault("Name", "").AsString() == name)
			{
				return dictionary.GetValueOrDefault("Data", new Dictionary()).AsGodotDictionary();
			}
		}
		return null;
	}

	private static Godot.Collections.Array GetEventArray(Dictionary exportedLevel, string name)
	{
		return exportedLevel.GetValueOrDefault("Event", new Dictionary()).AsGodotDictionary().GetValueOrDefault(name, new Godot.Collections.Array())
			.AsGodotArray();
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[LevelEditorFeatureRefreshRuntimeTest] " + message);
		}
	}
}
