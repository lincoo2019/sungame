using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGhostFourHypnotizedFootballCardRuntimeTest : Node
{
	private const string LevelPath = "res://Asset/Config/Level/TowerDefense/Challenge/Diamond/Challenge_Level_Diamond12_4.tres";

	private const string PacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootball.tres";

	private const string SpritePath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Sprite/ZombieFootball.tscn";

	private const string PacketShowPath = "res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn";

	private const string ZombieBackgroundPath = "res://Asset/Texture/TowerDefense/Packet/PC/PacketZombie.png";

	private const string FootballKey = "ZombieFootball";

	private static readonly Color HypnosisTint = new Color(0.72f, 0.62f, 1f);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		ResourceManager resources = ResourceManager.Instance;
		TowerDefenseInGamePacketShow packetShow = null;
		Resource previousPacket = null;
		Resource previousSprite = null;
		bool hadPreviousPacket = false;
		bool hadPreviousSprite = false;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(resources) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance), "ResourceManager and TowerDefenseManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(resources) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
				{
					goto end_IL_0071;
				}
				TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Challenge/Diamond/Challenge_Level_Diamond12_4.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig) && towerDefenseLevelConfig.name == "Challenge_Level_Diamond12_4" && towerDefenseLevelConfig.levelNumber == 4, "The scene must load the real Ghost Hypno-shroom challenge 4 level resource.");
				if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
				{
					goto end_IL_0071;
				}
				towerDefenseLevelConfig.Init();
				Check(towerDefenseLevelConfig.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET && towerDefenseLevelConfig.featureData.ContainsKey(new StringName("SeedBank")), "Challenge 4 must retain its authored PRESET SeedBank feature.");
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindLevelPacket(towerDefenseLevelConfig, "ZombieFootball");
				Check(GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig) && towerDefenseLevelPacketConfig.packetName == "ZombieFootball", "Challenge 4 must contain its real ZombieFootball preset card.");
				Check(GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig?.@override) && towerDefenseLevelPacketConfig.@override.hypnoses, "The real Challenge 4 ZombieFootball card must carry Override.Hypnoses=true.");
				if (!GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig?.@override))
				{
					goto end_IL_0071;
				}
				TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootball.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Football/Sprite/ZombieFootball.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Texture2D zombieBackground = ResourceLoader.Load<Texture2D>("res://Asset/Texture/TowerDefense/Packet/PC/PacketZombie.png", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && towerDefensePacketConfig.saveKey == "ZombieFootball" && GodotObject.IsInstanceValid(towerDefensePacketConfig.characterConfig), "The regression must use the authored ZombieFootball packet and character config.");
				Check(GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate() && GodotObject.IsInstanceValid(packedScene2) && packedScene2.CanInstantiate() && GodotObject.IsInstanceValid(zombieBackground), "The real Football sprite, PacketShow, and Zombie card background must load.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(packedScene2) || !GodotObject.IsInstanceValid(zombieBackground))
				{
					goto end_IL_0071;
				}
				hadPreviousPacket = resources.TOWERDEFENSE_PACKETS.TryGetValue("ZombieFootball", out previousPacket);
				hadPreviousSprite = resources.CHARCTAER_SPRITE.TryGetValue("ZombieFootball", out previousSprite);
				resources.TOWERDEFENSE_PACKETS["ZombieFootball"] = towerDefensePacketConfig;
				resources.CHARCTAER_SPRITE["ZombieFootball"] = packedScene;
				TowerDefensePacketConfig runtimePacket = towerDefenseLevelPacketConfig.GetPacket();
				Check(GodotObject.IsInstanceValid(runtimePacket) && runtimePacket.saveKey == "ZombieFootball" && runtimePacket.characterConfig?.name == "ZombieFootball", "The real level-card path must resolve a live ZombieFootball runtime packet.");
				Check(GodotObject.IsInstanceValid(runtimePacket?._override) && runtimePacket != towerDefensePacketConfig && runtimePacket._override != towerDefenseLevelPacketConfig.@override, "Level packet resolution must duplicate both the base card and its level override.");
				Check(!towerDefensePacketConfig.GetHypnoses() && (runtimePacket?.GetHypnoses() ?? false), "GetHypnoses must distinguish the base Football card from Challenge 4's override.");
				if (!GodotObject.IsInstanceValid(runtimePacket))
				{
					goto end_IL_0071;
				}
				packetShow = packedScene2.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(packetShow), "The production TowerDefenseInGamePacketShow scene must instantiate.");
				if (!GodotObject.IsInstanceValid(packetShow))
				{
					goto end_IL_0071;
				}
				packetShow.setPcLayout = true;
				packetShow.onlyDraw = true;
				AddChild(packetShow, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				packetShow.Init(runtimePacket);
				await WaitFrames(4);
				Check(packetShow.IsNodeReady() && packetShow.config == runtimePacket, "The real PacketShow must initialize with Challenge 4's runtime card.");
				Check(GodotObject.IsInstanceValid(packetShow.backgroundTexture) && packetShow.backgroundTexture.Visible && TextureMatches(packetShow.backgroundTexture.Texture, zombieBackground), "The Challenge 4 card slot must display the authored Zombie background texture.");
				AdobeAnimateSprite footballSprite = packetShow.sprite;
				Check(GodotObject.IsInstanceValid(footballSprite) && GodotObject.IsInstanceValid(footballSprite.flashAnimeData), "The card slot must instantiate the real Adobe Animate ZombieFootball preview.");
				Check(GodotObject.IsInstanceValid(footballSprite) && footballSprite.GetParent() == packetShow.previewSpriteNode && footballSprite.Visible, "The real Football preview must be mounted and visible inside the card slot.");
				Check(GodotObject.IsInstanceValid(footballSprite) && ColorMatches(footballSprite.Modulate, HypnosisTint), "Challenge 4's Override.Hypnoses must apply the authored purple card tint.");
				Check(GodotObject.IsInstanceValid(footballSprite) && footballSprite.clip == runtimePacket.packetAnimeClip && footballSprite.IsFrozenPreview, "The idle card must show the packet's real authored clip as a frozen preview.");
				Check(TryCountDrawItems(footballSprite, out var count) && count > 0, $"The visible Football slot must submit real preview draw items; count={count}.");
				packetShow.OnMouseEntered();
				await WaitFrames(2);
				Check(footballSprite.Visible && !footballSprite.IsFrozenPreview && ColorMatches(footballSprite.Modulate, HypnosisTint), "Hover must animate the visible Football preview without losing its hypnosis tint.");
				packetShow.OnMouseExited();
				await WaitFrames(2);
				int count2 = 0;
				Check(TryCountDrawItems(footballSprite, out count2) && count2 > 0 && footballSprite.Visible && footballSprite.IsFrozenPreview, $"Mouse exit must retain the textured frozen Football preview; count={count2}.");
				goto end_IL_0056;
				end_IL_0071:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGhostFourHypnotizedFootballCardRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0056;
			}
			return;
			end_IL_0056:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(packetShow))
			{
				packetShow.QueueFree();
			}
			if (GodotObject.IsInstanceValid(resources))
			{
				RestoreResource(resources.TOWERDEFENSE_PACKETS, "ZombieFootball", hadPreviousPacket, previousPacket);
				RestoreResource(resources.CHARCTAER_SPRITE, "ZombieFootball", hadPreviousSprite, previousSprite);
			}
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 20;
		GD.Print($"GHOST_FOUR_HYPNOTIZED_FOOTBALL_CARD_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseLevelPacketConfig FindLevelPacket(TowerDefenseLevelConfig level, string packetName)
	{
		foreach (Variant packetBank in level.packetBankList)
		{
			if (packetBank.VariantType == Variant.Type.Object && packetBank.AsGodotObject() is TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig && towerDefenseLevelPacketConfig.packetName == packetName)
			{
				return towerDefenseLevelPacketConfig;
			}
		}
		return null;
	}

	private static bool TryCountDrawItems(AdobeAnimateSprite sprite, out int count)
	{
		count = 0;
		if (!GodotObject.IsInstanceValid(sprite) || !sprite.TryBuildRenderSnapshot(out var snapshot, allowUnchanged: false))
		{
			return false;
		}
		List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
		AdobeAnimateDrawItemBuilder.Build(snapshot, list, snapshot.Definition?.GpuPoseTextureArray);
		count = list.Count;
		return true;
	}

	private static bool TextureMatches(Texture2D actual, Texture2D expected)
	{
		if (!GodotObject.IsInstanceValid(actual) || !GodotObject.IsInstanceValid(expected))
		{
			return false;
		}
		if (actual != expected && !(actual.GetRid() == expected.GetRid()))
		{
			if (!string.IsNullOrWhiteSpace(actual.ResourcePath))
			{
				return actual.ResourcePath == expected.ResourcePath;
			}
			return false;
		}
		return true;
	}

	private static bool ColorMatches(Color actual, Color expected)
	{
		if (Mathf.IsEqualApprox(actual.R, expected.R) && Mathf.IsEqualApprox(actual.G, expected.G) && Mathf.IsEqualApprox(actual.B, expected.B))
		{
			return Mathf.IsEqualApprox(actual.A, expected.A);
		}
		return false;
	}

	private static void RestoreResource(Dictionary<string, Resource> dictionary, string key, bool hadPrevious, Resource previous)
	{
		if (hadPrevious)
		{
			dictionary[key] = previous;
		}
		else
		{
			dictionary.Remove(key);
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewGhostFourHypnotizedFootballCardRuntimeTest] " + message);
		}
	}
}
