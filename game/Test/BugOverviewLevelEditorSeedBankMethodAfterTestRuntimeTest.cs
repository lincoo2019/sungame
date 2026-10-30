using System;
using Godot;
using Godot.Collections;

public partial class BugOverviewLevelEditorSeedBankMethodAfterTestRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		try
		{
			TowerDefenseLevelConfig towerDefenseLevelConfig = LoadConfig(new Dictionary
			{
				["FinishMethod"] = "VASE",
				["Feature"] = new Godot.Collections.Array
				{
					new Dictionary
					{
						["Name"] = "RainMode",
						["Data"] = new Dictionary { ["OldRainMarker"] = 31 }
					},
					new Dictionary
					{
						["Name"] = "ScreenEffect",
						["Data"] = new Dictionary
						{
							["StormOpen"] = false,
							["PacketBankMethod"] = 4
						}
					},
					new Dictionary
					{
						["Name"] = "CustomEditorFeature",
						["Data"] = new Dictionary { ["KeepMarker"] = 47 }
					}
				},
				["Process"] = new Dictionary
				{
					["Name"] = "Vase",
					["Data"] = new Dictionary { ["PacketBankMethod"] = 4 }
				},
				["PacketBank"] = new Dictionary
				{
					["Method"] = "RAIN",
					["Value"] = new Godot.Collections.Array(),
					["RainPreset"] = new Dictionary()
				}
			});
			TowerDefenseLevelConfig towerDefenseLevelConfig2 = towerDefenseLevelConfig;
			if (towerDefenseLevelConfig2.conveyorData == null)
			{
				towerDefenseLevelConfig2.conveyorData = new TowerDefenseConveyorConfig();
			}
			towerDefenseLevelConfig2 = towerDefenseLevelConfig;
			if (towerDefenseLevelConfig2.rainData == null)
			{
				towerDefenseLevelConfig2.rainData = new TowerDefenseRainModeConfig();
			}
			Check(towerDefenseLevelConfig.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN, "A tested/reloaded Rain level must expose its explicit SeedBank method.");
			Check(!towerDefenseLevelConfig.SetPacketBankMethodFromEditor(TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN), "Selecting the existing SeedBank method must not discard explicit data.");
			Dictionary exportedLevel = towerDefenseLevelConfig.Export();
			Dictionary dictionary = FindFeature(exportedLevel, "RainMode");
			Check(dictionary != null && dictionary.GetValueOrDefault("OldRainMarker", 0).AsInt32() == 31, "Selecting the existing Rain method must preserve its explicit snapshot.");
			Dictionary dictionary2 = FindFeature(exportedLevel, "CustomEditorFeature");
			Check(dictionary2 != null && dictionary2.GetValueOrDefault("KeepMarker", 0).AsInt32() == 47, "Selecting the existing method must preserve unrelated custom features.");
			VerifySwitch(towerDefenseLevelConfig, TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE, expectedRain: false, expectedConveyor: false, expectedSeedBank: true, expectedPacketBank: true);
			VerifySwitch(towerDefenseLevelConfig, TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR, expectedRain: false, expectedConveyor: true, expectedSeedBank: false, expectedPacketBank: false);
			VerifySwitch(towerDefenseLevelConfig, TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN, expectedRain: true, expectedConveyor: false, expectedSeedBank: false, expectedPacketBank: false);
			VerifySwitch(towerDefenseLevelConfig, TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET, expectedRain: false, expectedConveyor: false, expectedSeedBank: true, expectedPacketBank: true);
			TowerDefenseLevelConfig towerDefenseLevelConfig3 = LoadConfig(VerifySwitch(towerDefenseLevelConfig, TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE, expectedRain: false, expectedConveyor: false, expectedSeedBank: false, expectedPacketBank: false));
			Check(towerDefenseLevelConfig3.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE, "The final no-card mode must survive the save/reload cycle.");
			Check(!HasFeature(towerDefenseLevelConfig3.featureData, "RainMode") && !HasFeature(towerDefenseLevelConfig3.featureData, "ConveyorBelt") && !HasFeature(towerDefenseLevelConfig3.featureData, "SeedBank") && !HasFeature(towerDefenseLevelConfig3.featureData, "PacketBank"), "Reloading must not resurrect any stale SeedBank-mode feature.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewLevelEditorSeedBankMethodAfterTestRuntimeTest] Unexpected exception: {value}");
		}
		bool flag = _failures == 0 && _checks == 36;
		GD.Print($"LEVEL_EDITOR_SEEDBANK_METHOD_AFTER_TEST_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private Dictionary VerifySwitch(TowerDefenseLevelConfig config, TowerDefenseEnum.LEVEL_SEEDBANK_METHOD method, bool expectedRain, bool expectedConveyor, bool expectedSeedBank, bool expectedPacketBank)
	{
		Check(config.SetPacketBankMethodFromEditor(method), $"Switching to {method} must be recognized as an editor mode change.");
		Dictionary dictionary = config.Export();
		Check(dictionary.GetValueOrDefault("PacketBank", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Method", "")
			.AsString() == method.ToString(), $"The legacy PacketBank section must export {method}.");
		Check(FindFeature(dictionary, "RainMode") != null == expectedRain && FindFeature(dictionary, "ConveyorBelt") != null == expectedConveyor && FindFeature(dictionary, "SeedBank") != null == expectedSeedBank && FindFeature(dictionary, "PacketBank") != null == expectedPacketBank, $"{method} must export only the Feature set owned by that SeedBank mode.");
		Check(FindFeature(dictionary, "ScreenEffect")?.GetValueOrDefault("PacketBankMethod", -1).AsInt32() == (int?)method, $"ScreenEffect must receive the new {method} value.");
		Dictionary dictionary2 = dictionary.GetValueOrDefault("Process", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Data", new Dictionary())
			.AsGodotDictionary();
		Check(dictionary2.GetValueOrDefault("PacketBankMethod", -1).AsInt32() == (int)method, $"The preserved Vase process must receive the new {method} value.");
		Dictionary dictionary3 = FindFeature(dictionary, "CustomEditorFeature");
		Check(dictionary3 != null && dictionary3.GetValueOrDefault("KeepMarker", 0).AsInt32() == 47, $"Switching to {method} must preserve unrelated custom features.");
		return dictionary;
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

	private static bool HasFeature(Dictionary<StringName, Dictionary> featureData, string name)
	{
		return featureData?.ContainsKey(new StringName(name)) ?? false;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewLevelEditorSeedBankMethodAfterTestRuntimeTest] " + message);
		}
	}
}
