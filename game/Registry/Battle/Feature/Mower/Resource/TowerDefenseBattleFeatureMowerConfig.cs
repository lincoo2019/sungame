using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureMowerConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string mowerPacketName = "";

	[Export(PropertyHint.None, "")]
	public string waterMowerPacketName = "MowerPoolCleaner";

	[Export(PropertyHint.None, "")]
	public string targetZombiePacketName = "ZombieTarget";

	[Export(PropertyHint.None, "")]
	public string previewSpriteName = "MowerDefault";

	[Export(PropertyHint.None, "")]
	public double mowerSpawnOffsetX = 10.0;

	[Export(PropertyHint.None, "")]
	public double targetSpawnOffsetX = 40.0;

	[Export(PropertyHint.None, "")]
	public double previewGroundOffset = 10.0;

	[Export(PropertyHint.Range, "0,5,0.05")]
	public double previewTweenDuration = 0.5;

	[Export(PropertyHint.Range, "0.1,5,0.1")]
	public double previewScale = 1.0;

	public void Init(Dictionary data)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		mowerPacketName = data.GetValueOrDefault("MowerPacketName", "").AsString();
		waterMowerPacketName = ReadRequiredName(data, "WaterMowerPacketName", "MowerPoolCleaner");
		targetZombiePacketName = ReadRequiredName(data, "TargetZombiePacketName", "ZombieTarget");
		previewSpriteName = ReadRequiredName(data, "PreviewSpriteName", "MowerDefault");
		mowerSpawnOffsetX = ReadFinite(data, "MowerSpawnOffsetX", 10.0, -200.0, 200.0);
		targetSpawnOffsetX = ReadFinite(data, "TargetSpawnOffsetX", 40.0, -500.0, 500.0);
		previewGroundOffset = ReadFinite(data, "PreviewGroundOffset", 10.0, -200.0, 200.0);
		previewTweenDuration = ReadFinite(data, "PreviewTweenDuration", 0.5, 0.0, 5.0);
		previewScale = ReadFinite(data, "PreviewScale", 1.0, 0.1, 5.0);
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["MowerPacketName"] = mowerPacketName,
			["WaterMowerPacketName"] = waterMowerPacketName,
			["TargetZombiePacketName"] = targetZombiePacketName,
			["PreviewSpriteName"] = previewSpriteName,
			["MowerSpawnOffsetX"] = mowerSpawnOffsetX,
			["TargetSpawnOffsetX"] = targetSpawnOffsetX,
			["PreviewGroundOffset"] = previewGroundOffset,
			["PreviewTweenDuration"] = previewTweenDuration,
			["PreviewScale"] = previewScale
		};
	}

	private static string ReadRequiredName(Dictionary data, string key, string fallback)
	{
		string text = data.GetValueOrDefault(key, fallback).AsString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return fallback;
	}

	private static double ReadFinite(Dictionary data, string key, double fallback, double minimum, double maximum)
	{
		double num = data.GetValueOrDefault(key, fallback).AsDouble();
		if (!double.IsFinite(num))
		{
			return fallback;
		}
		return Math.Clamp(num, minimum, maximum);
	}
}
