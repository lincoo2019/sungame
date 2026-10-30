using Godot;

[GlobalClass]
public partial class TowerDefenseMapPacketRuleConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.PACKET_TYPE packetType = TowerDefenseEnum.PACKET_TYPE.NOONE;

	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public double cooldownMultiplier = 1.0;

	[Export(PropertyHint.None, "")]
	public bool ignoreDynamicCostGrowth;

	public bool Matches(TowerDefenseEnum.PACKET_TYPE candidateType)
	{
		if (packetType != TowerDefenseEnum.PACKET_TYPE.NOONE)
		{
			return packetType == candidateType;
		}
		return true;
	}

	public bool TryValidateRuntime(out string reason)
	{
		if (!double.IsFinite(cooldownMultiplier) || cooldownMultiplier < 0.0)
		{
			reason = $"packet cooldown multiplier must be finite and non-negative, got {cooldownMultiplier}";
			return false;
		}
		reason = "";
		return true;
	}
}
