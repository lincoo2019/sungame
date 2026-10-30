using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessQuizConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool autoStripColumn = true;

	[Export(PropertyHint.Range, "1,64,1")]
	public int stripColumn = 5;

	[Export(PropertyHint.None, "")]
	public string potPacketName = "PlantPot";

	[Export(PropertyHint.None, "")]
	public string lilyPadPacketName = "PlantLilyPad";

	[Export(PropertyHint.None, "")]
	public string presentBoxPacketName = "PlantPresentBox";

	[Export(PropertyHint.None, "")]
	public string zombieVasePacketName = "VaseZombie";

	[Export(PropertyHint.None, "")]
	public string presentBoxPacketBank = "PresentBoxNoAshPlant";

	[Export(PropertyHint.None, "")]
	public string zombieVasePacketBank = "QuizZombie";

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double settlementLineDelay = 1.0;

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double settlementSummaryDelay = 2.0;

	[Export(PropertyHint.Range, "0,2,0.01")]
	public double coinSpawnInterval = 0.1;

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double coinFlightDelay = 1.0;

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double coinCollectDelay = 0.5;

	public void Init(Dictionary data)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		autoStripColumn = data.GetValueOrDefault("AutoStripColumn", true).AsBool();
		stripColumn = Math.Clamp(data.GetValueOrDefault("StripColumn", 5).AsInt32(), 1, 64);
		potPacketName = ReadName(data, "PotPacketName", "PlantPot");
		lilyPadPacketName = ReadName(data, "LilyPadPacketName", "PlantLilyPad");
		presentBoxPacketName = ReadName(data, "PresentBoxPacketName", "PlantPresentBox");
		zombieVasePacketName = ReadName(data, "ZombieVasePacketName", "VaseZombie");
		presentBoxPacketBank = ReadName(data, "PresentBoxPacketBank", "PresentBoxNoAshPlant");
		zombieVasePacketBank = ReadName(data, "ZombieVasePacketBank", "QuizZombie");
		settlementLineDelay = ReadDuration(data, "SettlementLineDelay", 1.0, 10.0);
		settlementSummaryDelay = ReadDuration(data, "SettlementSummaryDelay", 2.0, 10.0);
		coinSpawnInterval = ReadDuration(data, "CoinSpawnInterval", 0.1, 2.0);
		coinFlightDelay = ReadDuration(data, "CoinFlightDelay", 1.0, 10.0);
		coinCollectDelay = ReadDuration(data, "CoinCollectDelay", 0.5, 10.0);
	}

	public int ResolveStripColumn(int mapColumnCount)
	{
		if (mapColumnCount <= 0)
		{
			return 1;
		}
		return Math.Clamp(autoStripColumn ? (Mathf.FloorToInt((float)mapColumnCount / 2f) + 1) : stripColumn, 1, mapColumnCount);
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["AutoStripColumn"] = autoStripColumn,
			["StripColumn"] = stripColumn,
			["PotPacketName"] = potPacketName,
			["LilyPadPacketName"] = lilyPadPacketName,
			["PresentBoxPacketName"] = presentBoxPacketName,
			["ZombieVasePacketName"] = zombieVasePacketName,
			["PresentBoxPacketBank"] = presentBoxPacketBank,
			["ZombieVasePacketBank"] = zombieVasePacketBank,
			["SettlementLineDelay"] = settlementLineDelay,
			["SettlementSummaryDelay"] = settlementSummaryDelay,
			["CoinSpawnInterval"] = coinSpawnInterval,
			["CoinFlightDelay"] = coinFlightDelay,
			["CoinCollectDelay"] = coinCollectDelay
		};
	}

	private static string ReadName(Dictionary data, string key, string fallback)
	{
		string text = data.GetValueOrDefault(key, fallback).AsString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return fallback;
	}

	private static double ReadDuration(Dictionary data, string key, double fallback, double maximum)
	{
		double num = data.GetValueOrDefault(key, fallback).AsDouble();
		if (!double.IsFinite(num))
		{
			return fallback;
		}
		return Math.Clamp(num, 0.0, maximum);
	}
}
