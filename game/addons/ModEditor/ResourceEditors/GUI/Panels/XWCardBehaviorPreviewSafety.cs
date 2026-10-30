using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public static class XWCardBehaviorPreviewSafety
{
	public static XWCardBehaviorPreviewSnapshot Capture(TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(packet))
		{
			return new XWCardBehaviorPreviewSnapshot(isLoaded: false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, packetFlip: false, Vector2.Zero, Vector2.Zero, null, TowerDefenseEnum.PACKET_TYPE.NOONE, hypnoses: false, -1, XWCardPreviewValueSource.Missing, -1, XWCardPreviewValueSource.Missing, -1.0, XWCardPreviewValueSource.Missing, -1.0, XWCardPreviewValueSource.Missing, -1.0, XWCardPreviewValueSource.Missing, 0, 0, 0, 0);
		}
		XWCardPreviewValueSource source;
		int cost = ResolveBaseOverrideCost(packet, out source);
		XWCardPreviewValueSource source2;
		int costRise = ResolveBaseOverrideCostRise(packet, out source2);
		XWCardPreviewValueSource source3;
		double costMultiple = ResolveBaseOverrideCostMultiple(packet, out source3);
		XWCardPreviewValueSource source4;
		double cooldown = ResolveBaseOverrideCooldown(packet, out source4);
		XWCardPreviewValueSource source5;
		double startingCooldown = ResolveBaseOverrideStartingCooldown(packet, out source5);
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		return new XWCardBehaviorPreviewSnapshot(isLoaded: true, packet.ResourceName, packet.saveKey, packet.name, packet.describe, packet.packetAnimeClip, packet.packetFlip, packet.packetAnimeOffset, packet.packetAnimeScale, IsValid(packet.characterConfig) ? packet.characterConfig : null, ResolveBaseOverrideType(packet), ResolveBaseOverrideHypnoses(packet), cost, source, costRise, source2, costMultiple, source3, cooldown, source4, startingCooldown, source5, Count(packet.behaviorIds) + Count(packet.behaviors), ResolveEffectiveActionCount(packet.pressedActions, towerDefensePacketOverride?.pressedActions), ResolveEffectiveActionCount(packet.useSucceededActions, towerDefensePacketOverride?.useSucceededActions), packet.changeCostList?.Count ?? 0);
	}

	public static int ResolveBaseOverrideCost(TowerDefensePacketConfig packet, out XWCardPreviewValueSource source)
	{
		if (!TryGetCharacter(packet, out var character))
		{
			source = XWCardPreviewValueSource.Missing;
			return -1;
		}
		int result = character.cost;
		source = XWCardPreviewValueSource.CharacterBase;
		if (packet.overrideCost >= 0)
		{
			result = packet.overrideCost;
			source = XWCardPreviewValueSource.PacketOverride;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		if (towerDefensePacketOverride != null && towerDefensePacketOverride.cost >= 0)
		{
			result = towerDefensePacketOverride.cost;
			source = XWCardPreviewValueSource.OverrideResource;
		}
		return result;
	}

	public static int ResolveBaseOverrideCostRise(TowerDefensePacketConfig packet, out XWCardPreviewValueSource source)
	{
		if (!TryGetCharacter(packet, out var character))
		{
			source = XWCardPreviewValueSource.Missing;
			return -1;
		}
		int result = character.costRise;
		source = XWCardPreviewValueSource.CharacterBase;
		if (packet.overrideCostRise >= 0)
		{
			result = packet.overrideCostRise;
			source = XWCardPreviewValueSource.PacketOverride;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		if (towerDefensePacketOverride != null && towerDefensePacketOverride.costRise >= 0)
		{
			result = towerDefensePacketOverride.costRise;
			source = XWCardPreviewValueSource.OverrideResource;
		}
		return result;
	}

	public static double ResolveBaseOverrideCostMultiple(TowerDefensePacketConfig packet, out XWCardPreviewValueSource source)
	{
		if (!TryGetCharacter(packet, out var character))
		{
			source = XWCardPreviewValueSource.Missing;
			return -1.0;
		}
		double costMultiple = character.costMultiple;
		source = XWCardPreviewValueSource.CharacterBase;
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		if (towerDefensePacketOverride != null && towerDefensePacketOverride.costMultiple >= 0.0)
		{
			costMultiple = towerDefensePacketOverride.costMultiple;
			source = XWCardPreviewValueSource.OverrideResource;
		}
		return costMultiple;
	}

	public static double ResolveBaseOverrideCooldown(TowerDefensePacketConfig packet, out XWCardPreviewValueSource source)
	{
		if (!TryGetCharacter(packet, out var character))
		{
			source = XWCardPreviewValueSource.Missing;
			return -1.0;
		}
		double result = character.packetCooldown;
		source = XWCardPreviewValueSource.CharacterBase;
		if (packet.overridePacketCooldown >= 0.0)
		{
			result = packet.overridePacketCooldown;
			source = XWCardPreviewValueSource.PacketOverride;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		if (towerDefensePacketOverride != null && towerDefensePacketOverride.packetCooldown >= 0.0)
		{
			result = towerDefensePacketOverride.packetCooldown;
			source = XWCardPreviewValueSource.OverrideResource;
		}
		return result;
	}

	public static double ResolveBaseOverrideStartingCooldown(TowerDefensePacketConfig packet, out XWCardPreviewValueSource source)
	{
		if (!TryGetCharacter(packet, out var character))
		{
			source = XWCardPreviewValueSource.Missing;
			return -1.0;
		}
		double result = character.startingCooldown;
		source = XWCardPreviewValueSource.CharacterBase;
		if (packet.overrideStartingCooldown >= 0.0)
		{
			result = packet.overrideStartingCooldown;
			source = XWCardPreviewValueSource.PacketOverride;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		if (towerDefensePacketOverride != null && towerDefensePacketOverride.startingCooldown >= 0.0)
		{
			result = towerDefensePacketOverride.startingCooldown;
			source = XWCardPreviewValueSource.OverrideResource;
		}
		return result;
	}

	public static TowerDefenseEnum.PACKET_TYPE ResolveBaseOverrideType(TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(packet))
		{
			return TowerDefenseEnum.PACKET_TYPE.NOONE;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		if (towerDefensePacketOverride == null || towerDefensePacketOverride.type == TowerDefenseEnum.PACKET_TYPE.NOONE)
		{
			return packet.type;
		}
		return towerDefensePacketOverride.type;
	}

	public static bool ResolveBaseOverrideHypnoses(TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(packet))
		{
			return false;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = GetOverride(packet);
		if (towerDefensePacketOverride == null || !towerDefensePacketOverride.hypnoses)
		{
			return packet.overrideHypnoses;
		}
		return true;
	}

	private static bool TryGetCharacter(TowerDefensePacketConfig packet, out TowerDefenseCharacterConfig character)
	{
		character = ((GodotObject.IsInstanceValid(packet) && IsValid(packet.characterConfig)) ? packet.characterConfig : null);
		return character != null;
	}

	private static TowerDefensePacketOverride GetOverride(TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(packet) || !IsValid(packet._override))
		{
			return null;
		}
		return packet._override;
	}

	private static bool IsValid(GodotObject value)
	{
		return GodotObject.IsInstanceValid(value);
	}

	private static int Count(Array<StringName> values)
	{
		return values?.Count ?? 0;
	}

	private static int Count(Array<CardBehaviorDefinition> values)
	{
		return values?.Count ?? 0;
	}

	private static int ResolveEffectiveActionCount(Array<CardActionBehaviorDefinition> authored, Array<CardActionBehaviorDefinition> overridden)
	{
		if (overridden == null || overridden.Count <= 0)
		{
			return authored?.Count ?? 0;
		}
		return overridden.Count;
	}
}
