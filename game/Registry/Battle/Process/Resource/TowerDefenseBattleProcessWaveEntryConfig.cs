using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessWaveEntryConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool mowerUse;

	[Export(PropertyHint.Range, "0,15,0.05")]
	public double entryBroadcastDuration = 4.0;

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double cameraTravelDuration = 1.5;

	[Export(PropertyHint.Range, "0,5,0.05")]
	public double packetBankExitDelay = 0.5;

	[Export(PropertyHint.Range, "0,10,0.05")]
	public double entryLabelDuration = 2.0;

	[Export(PropertyHint.Range, "0,5,0.05")]
	public double debugEnterHouseFadeDuration = 1.0;

	public virtual void Init(Dictionary data)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		mowerUse = data.GetValueOrDefault("MowerUse", false).AsBool();
		entryBroadcastDuration = ReadDuration(data, "EntryBroadcastDuration", 4.0, 15.0);
		cameraTravelDuration = ReadDuration(data, "CameraTravelDuration", 1.5, 10.0);
		packetBankExitDelay = ReadDuration(data, "PacketBankExitDelay", 0.5, 5.0);
		entryLabelDuration = ReadDuration(data, "EntryLabelDuration", 2.0, 10.0);
		debugEnterHouseFadeDuration = ReadDuration(data, "DebugEnterHouseFadeDuration", 1.0, 5.0);
	}

	public virtual Dictionary Export()
	{
		return new Dictionary
		{
			["MowerUse"] = mowerUse,
			["EntryBroadcastDuration"] = entryBroadcastDuration,
			["CameraTravelDuration"] = cameraTravelDuration,
			["PacketBankExitDelay"] = packetBankExitDelay,
			["EntryLabelDuration"] = entryLabelDuration,
			["DebugEnterHouseFadeDuration"] = debugEnterHouseFadeDuration
		};
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
