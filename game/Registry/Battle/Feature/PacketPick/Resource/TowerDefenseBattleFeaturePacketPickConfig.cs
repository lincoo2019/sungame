using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeaturePacketPickConfig : Resource
{
	[Export(PropertyHint.Range, "1,120,0.5,or_greater")]
	public double pendingRequestTimeoutSeconds = 15.0;

	[Export(PropertyHint.Range, "0.05,5,0.05,or_greater")]
	public double pendingRequestSweepIntervalSeconds = 0.5;

	[Export(PropertyHint.Range, "0,30,1,or_greater")]
	public int packetSelectionDebounceFrames = 5;

	[Export(PropertyHint.Range, "0,30,1,or_greater")]
	public int toolActivationGraceFrames = 5;

	[Export(PropertyHint.Range, "0.05,2,0.05,or_greater")]
	public double characterTargetRadiusScale = 0.6;

	public void Init(Dictionary data)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		pendingRequestTimeoutSeconds = ClampFinite(data.GetValueOrDefault("PendingRequestTimeoutSeconds", 15.0).AsDouble(), 1.0, 120.0, 15.0);
		pendingRequestSweepIntervalSeconds = ClampFinite(data.GetValueOrDefault("PendingRequestSweepIntervalSeconds", 0.5).AsDouble(), 0.05, 5.0, 0.5);
		packetSelectionDebounceFrames = Math.Clamp(data.GetValueOrDefault("PacketSelectionDebounceFrames", 5).AsInt32(), 0, 30);
		toolActivationGraceFrames = Math.Clamp(data.GetValueOrDefault("ToolActivationGraceFrames", 5).AsInt32(), 0, 30);
		characterTargetRadiusScale = ClampFinite(data.GetValueOrDefault("CharacterTargetRadiusScale", 0.6).AsDouble(), 0.05, 2.0, 0.6);
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["PendingRequestTimeoutSeconds"] = pendingRequestTimeoutSeconds,
			["PendingRequestSweepIntervalSeconds"] = pendingRequestSweepIntervalSeconds,
			["PacketSelectionDebounceFrames"] = packetSelectionDebounceFrames,
			["ToolActivationGraceFrames"] = toolActivationGraceFrames,
			["CharacterTargetRadiusScale"] = characterTargetRadiusScale
		};
	}

	private static double ClampFinite(double value, double minimum, double maximum, double fallback)
	{
		if (!double.IsFinite(value))
		{
			return fallback;
		}
		return Math.Clamp(value, minimum, maximum);
	}
}
