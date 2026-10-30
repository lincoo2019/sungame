using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelIZMManagerConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool shuffle = true;

	[Export(PropertyHint.Range, "1,64,1")]
	public int preSpawnMaxRetryPasses = 8;

	[Export(PropertyHint.Range, "1,120,1")]
	public int failureCheckIntervalFrames = 6;

	[Export(PropertyHint.Range, "0,5,0.05")]
	public double packetBankExitDelay = 0.5;

	[Export(PropertyHint.Range, "0,5,0.05")]
	public double enterHouseFadeDuration = 1.0;

	[Export(PropertyHint.None, "")]
	public string failureIgnoredZombieName = "ZombieTarget";

	public void Init(Dictionary data)
	{
		shuffle = data.GetValueOrDefault("Shuffle", true).AsBool();
		preSpawnMaxRetryPasses = Math.Clamp(data.GetValueOrDefault("PreSpawnMaxRetryPasses", 8).AsInt32(), 1, 64);
		failureCheckIntervalFrames = Math.Clamp(data.GetValueOrDefault("FailureCheckIntervalFrames", 6).AsInt32(), 1, 120);
		packetBankExitDelay = ReadDuration(data, "PacketBankExitDelay", 0.5);
		enterHouseFadeDuration = ReadDuration(data, "EnterHouseFadeDuration", 1.0);
		failureIgnoredZombieName = data.GetValueOrDefault("FailureIgnoredZombieName", "ZombieTarget").AsString();
		if (string.IsNullOrWhiteSpace(failureIgnoredZombieName))
		{
			failureIgnoredZombieName = "ZombieTarget";
		}
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["Shuffle"] = shuffle,
			["PreSpawnMaxRetryPasses"] = preSpawnMaxRetryPasses,
			["FailureCheckIntervalFrames"] = failureCheckIntervalFrames,
			["PacketBankExitDelay"] = packetBankExitDelay,
			["EnterHouseFadeDuration"] = enterHouseFadeDuration,
			["FailureIgnoredZombieName"] = failureIgnoredZombieName
		};
	}

	private static double ReadDuration(Dictionary data, string key, double fallback)
	{
		double num = data.GetValueOrDefault(key, fallback).AsDouble();
		if (!double.IsFinite(num))
		{
			return fallback;
		}
		return Math.Clamp(num, 0.0, 5.0);
	}
}
