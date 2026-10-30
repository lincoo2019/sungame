using System;
using Godot;
using Godot.Collections;

public partial class BugOverviewLevelEditorFinishMethodAfterTestRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig = LoadConfig(new Dictionary
			{
				["FinishMethod"] = "WAVE",
				["Feature"] = new Godot.Collections.Array
				{
					new Dictionary
					{
						["Name"] = "Wave",
						["Data"] = new Dictionary { ["CustomWaveMarker"] = 31 }
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
					["Data"] = new Dictionary { ["CustomMarker"] = 77 }
				},
				["PacketBank"] = new Dictionary
				{
					["Method"] = "CHOOSE",
					["Value"] = new Godot.Collections.Array()
				}
			});
			Check(towerDefenseLevelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE && towerDefenseLevelConfig.processName == (StringName)"Wave", "A tested/reloaded Wave level must expose its explicit process.");
			towerDefenseLevelConfig.SetFinishMethodFromEditor(TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE);
			Dictionary dictionary = towerDefenseLevelConfig.Export();
			Dictionary dictionary2 = dictionary["Process"].AsGodotDictionary();
			Check(dictionary2.GetValueOrDefault("Name", "").AsString() == "Wave", "Selecting the existing editor mode must keep the Wave process.");
			Check(dictionary2.GetValueOrDefault("Data", new Dictionary()).AsGodotDictionary().GetValueOrDefault("CustomMarker", 0)
				.AsInt32() == 77, "Selecting the existing mode must preserve explicit process data.");
			Dictionary dictionary3 = FindFeature(dictionary, "Wave");
			Check(dictionary3 != null && dictionary3.GetValueOrDefault("CustomWaveMarker", 0).AsInt32() == 31, "Selecting the existing mode must preserve the explicit Wave feature snapshot.");
			towerDefenseLevelConfig.vaseManager = new TowerDefenseLevelVaseManagerConfig();
			towerDefenseLevelConfig.SetFinishMethodFromEditor(TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE);
			Dictionary dictionary4 = towerDefenseLevelConfig.Export();
			Dictionary dictionary5 = dictionary4["Process"].AsGodotDictionary();
			Check(dictionary4.GetValueOrDefault("FinishMethod", "").AsString() == "VASE", "Editor mode change must export the Vase finish method.");
			Check(dictionary5.GetValueOrDefault("Name", "").AsString() == "Vase", "Old explicit Wave process must not overwrite the selected Vase process.");
			Check(!dictionary5.GetValueOrDefault("Data", new Dictionary()).AsGodotDictionary().ContainsKey("CustomMarker"), "Mode change must not carry old Wave-only process data into Vase.");
			Check(FindFeature(dictionary4, "Wave") == null, "Mode change to Vase must remove the tested level's stale Wave feature snapshot.");
			Dictionary dictionary6 = FindFeature(dictionary4, "CustomEditorFeature");
			Check(dictionary6 != null && dictionary6.GetValueOrDefault("KeepMarker", 0).AsInt32() == 47, "Mode change must preserve unrelated explicit/custom features.");
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = LoadConfig(dictionary4);
			Check(towerDefenseLevelConfig2.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE && towerDefenseLevelConfig2.processName == (StringName)"Vase", "A saved Vase edit must survive the same reload that follows level testing.");
			towerDefenseLevelConfig2.izmManager = new TowerDefenseLevelIZMManagerConfig();
			towerDefenseLevelConfig2.SetFinishMethodFromEditor(TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM);
			Dictionary dictionary7 = towerDefenseLevelConfig2.Export();
			Dictionary dictionary8 = dictionary7["Process"].AsGodotDictionary();
			Check(dictionary7.GetValueOrDefault("FinishMethod", "").AsString() == "IZM", "A second editor mode change must export IZM.");
			Check(dictionary8.GetValueOrDefault("Name", "").AsString() == "IZM", "Reloaded explicit Vase process must not pin the editor to Vase.");
			Check(FindFeature(dictionary7, "Wave") == null, "A reloaded mode edit must not resurrect the old Wave feature snapshot.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewLevelEditorFinishMethodAfterTestRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 13;
		GD.Print($"LEVEL_EDITOR_FINISH_METHOD_AFTER_TEST_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewLevelEditorFinishMethodAfterTestRuntimeTest] " + message);
		}
	}
}
