using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed class XWCardBehaviorPreviewSnapshot
{
	public bool IsLoaded { get; }

	public string ResourceName { get; }

	public string SaveKey { get; }

	public string DisplayName { get; }

	public string Description { get; }

	public string PacketAnimeClip { get; }

	public bool PacketFlip { get; }

	public Vector2 PacketAnimeOffset { get; }

	public Vector2 PacketAnimeScale { get; }

	public TowerDefenseCharacterConfig CharacterConfig { get; }

	public TowerDefenseEnum.PACKET_TYPE PacketType { get; }

	public bool Hypnoses { get; }

	public int Cost { get; }

	public XWCardPreviewValueSource CostSource { get; }

	public int CostRise { get; }

	public XWCardPreviewValueSource CostRiseSource { get; }

	public double CostMultiple { get; }

	public XWCardPreviewValueSource CostMultipleSource { get; }

	public double Cooldown { get; }

	public XWCardPreviewValueSource CooldownSource { get; }

	public double StartingCooldown { get; }

	public XWCardPreviewValueSource StartingCooldownSource { get; }

	public int SuppressedBehaviorCount { get; }

	public int SuppressedPressedActionCount { get; }

	public int SuppressedUseSucceededActionCount { get; }

	public int SuppressedChangeCostRuleCount { get; }

	public bool SuppressesGlobalCostRules => true;

	public bool IsBehaviorFree => true;

	public int SuppressedLocalRuleCount => SuppressedBehaviorCount + SuppressedPressedActionCount + SuppressedUseSucceededActionCount + SuppressedChangeCostRuleCount;

	internal XWCardBehaviorPreviewSnapshot(bool isLoaded, string resourceName, string saveKey, string displayName, string description, string packetAnimeClip, bool packetFlip, Vector2 packetAnimeOffset, Vector2 packetAnimeScale, TowerDefenseCharacterConfig characterConfig, TowerDefenseEnum.PACKET_TYPE packetType, bool hypnoses, int cost, XWCardPreviewValueSource costSource, int costRise, XWCardPreviewValueSource costRiseSource, double costMultiple, XWCardPreviewValueSource costMultipleSource, double cooldown, XWCardPreviewValueSource cooldownSource, double startingCooldown, XWCardPreviewValueSource startingCooldownSource, int suppressedBehaviorCount, int suppressedPressedActionCount, int suppressedUseSucceededActionCount, int suppressedChangeCostRuleCount)
	{
		IsLoaded = isLoaded;
		ResourceName = resourceName ?? string.Empty;
		SaveKey = saveKey ?? string.Empty;
		DisplayName = displayName ?? string.Empty;
		Description = description ?? string.Empty;
		PacketAnimeClip = packetAnimeClip ?? string.Empty;
		PacketFlip = packetFlip;
		PacketAnimeOffset = packetAnimeOffset;
		PacketAnimeScale = packetAnimeScale;
		CharacterConfig = characterConfig;
		PacketType = packetType;
		Hypnoses = hypnoses;
		Cost = cost;
		CostSource = costSource;
		CostRise = costRise;
		CostRiseSource = costRiseSource;
		CostMultiple = costMultiple;
		CostMultipleSource = costMultipleSource;
		Cooldown = cooldown;
		CooldownSource = cooldownSource;
		StartingCooldown = startingCooldown;
		StartingCooldownSource = startingCooldownSource;
		SuppressedBehaviorCount = suppressedBehaviorCount;
		SuppressedPressedActionCount = suppressedPressedActionCount;
		SuppressedUseSucceededActionCount = suppressedUseSucceededActionCount;
		SuppressedChangeCostRuleCount = suppressedChangeCostRuleCount;
	}
}
