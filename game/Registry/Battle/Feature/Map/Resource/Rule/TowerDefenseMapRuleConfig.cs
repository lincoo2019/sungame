using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseMapRuleConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public StringName id = "";

	[Export(PropertyHint.None, "")]
	public bool preventsSleep;

	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public double attackDpsLifestealRatio;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseMapPacketRuleConfig> packetRules = new Array<TowerDefenseMapPacketRuleConfig>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseMapCharacterRuleConfig> characterRules = new Array<TowerDefenseMapCharacterRuleConfig>();

	[ExportGroup("Zombie Column Speed", "")]
	[Export(PropertyHint.Range, "0,50,1")]
	public int zombieColumnSpeedMinCol;

	[Export(PropertyHint.Range, "0,50,1")]
	public int zombieColumnSpeedMaxCol;

	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public double zombieColumnSpeedMultiplier = 1.0;

	public bool TryValidateRuntime(out string reason)
	{
		if (id.IsEmpty)
		{
			reason = "map rule has no id";
			return false;
		}
		if (!double.IsFinite(attackDpsLifestealRatio) || attackDpsLifestealRatio < 0.0)
		{
			reason = $"map rule '{id}' has an invalid DPS lifesteal ratio";
			return false;
		}
		bool flag = zombieColumnSpeedMinCol > 0 || zombieColumnSpeedMaxCol > 0;
		if (flag && (zombieColumnSpeedMinCol <= 0 || zombieColumnSpeedMaxCol <= 0 || zombieColumnSpeedMinCol > zombieColumnSpeedMaxCol || zombieColumnSpeedMaxCol > 50))
		{
			reason = $"map rule '{id}' has an invalid zombie column speed range (min={zombieColumnSpeedMinCol}, max={zombieColumnSpeedMaxCol})";
			return false;
		}
		if (flag && (!double.IsFinite(zombieColumnSpeedMultiplier) || zombieColumnSpeedMultiplier < 0.0))
		{
			reason = $"map rule '{id}' has an invalid zombie column speed multiplier";
			return false;
		}
		if (packetRules != null)
		{
			foreach (TowerDefenseMapPacketRuleConfig packetRule in packetRules)
			{
				if (!GodotObject.IsInstanceValid(packetRule))
				{
					reason = $"map rule '{id}' contains a null or freed packet rule";
					return false;
				}
				if (!packetRule.TryValidateRuntime(out reason))
				{
					reason = $"map rule '{id}': {reason}";
					return false;
				}
			}
		}
		if (characterRules != null)
		{
			foreach (TowerDefenseMapCharacterRuleConfig characterRule in characterRules)
			{
				if (!GodotObject.IsInstanceValid(characterRule))
				{
					reason = $"map rule '{id}' contains a null or freed character rule";
					return false;
				}
				if (!characterRule.TryValidateRuntime(out reason))
				{
					reason = $"map rule '{id}': {reason}";
					return false;
				}
			}
		}
		reason = "";
		return true;
	}
}
