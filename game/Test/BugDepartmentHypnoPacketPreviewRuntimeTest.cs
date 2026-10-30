using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentHypnoPacketPreviewRuntimeTest : Node
{
	private const string ResultMarker = "BUG_DEPARTMENT_HYPNO_PACKET_PREVIEW_RESULT";

	private const string PacketShowPath = "res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn";

	private const string HypnoImpPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/HypnoShroom/ZombieImpHypnoShroom.tres";

	private const string HypnoImpSpritePath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Sprite/HypnoShroom/ZombieImpHypnoShroom.tscn";

	private const string HypnoImpAnimePath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/ZombieImp.tres";

	private const string HypnoShroomAnimePath = "res://Asset/Anime/Character/Plant/Chapter0/HypnoShroom/HypnoShroom.tres";

	private const string FootballPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootball.tres";

	private const string FootballSpritePath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/Sprite/ZombieFootball.tscn";

	private const string FootballAnimePath = "res://Asset/Anime/Character/Zombie/Chapter1/Football/ZombieFootball.tres";

	private const string HypnoImpKey = "ZombieImpHypnoShroom";

	private const string FootballKey = "ZombieFootball";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		ResourceManager resources = ResourceManager.Instance;
		TowerDefenseInGamePacketShow hypnoImpShow = null;
		TowerDefenseInGamePacketShow footballShow = null;
		Resource previousHypnoImpSprite = null;
		Resource previousFootballSprite = null;
		bool hadPreviousHypnoImpSprite = false;
		bool hadPreviousFootballSprite = false;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(resources) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance), "ResourceManager and TowerDefenseManager autoloads must be available.");
				Require(GodotObject.IsInstanceValid(resources) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance), "Required autoloads are unavailable.");
				TowerDefensePacketConfig hypnoImpPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/HypnoShroom/ZombieImpHypnoShroom.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				TowerDefensePacketConfig footballPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Football/Packet/ZombieFootball.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				PackedScene hypnoImpSpriteScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Imp/Sprite/HypnoShroom/ZombieImpHypnoShroom.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				PackedScene footballSpriteScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Football/Sprite/ZombieFootball.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				bool condition = GodotObject.IsInstanceValid(hypnoImpPacket) && GodotObject.IsInstanceValid(footballPacket) && GodotObject.IsInstanceValid(hypnoImpSpriteScene) && hypnoImpSpriteScene.CanInstantiate() && GodotObject.IsInstanceValid(footballSpriteScene) && footballSpriteScene.CanInstantiate() && GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate();
				Check(condition, "Both authored packet resources, both authored Sprite scenes, and PacketShow must load.");
				Require(condition, "One or more authored preview resources failed to load.");
				Check(hypnoImpPacket.saveKey == "ZombieImpHypnoShroom" && hypnoImpPacket.characterConfig?.name == "ZombieImpHypnoShroom" && hypnoImpPacket.packetAnimeClip == "Walk", "The real Hypno-shroom Imp card must retain its dedicated key, character, and Walk clip.");
				Check(footballPacket.saveKey == "ZombieFootball" && footballPacket.characterConfig?.name == "ZombieFootball" && footballPacket.initArmor.Contains("Helmet"), "The real Football card must retain its dedicated key, character, and Helmet armor.");
				Check(ResourcePathMatches(hypnoImpSpriteScene, "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Sprite/HypnoShroom/ZombieImpHypnoShroom.tscn") && ResourcePathMatches(footballSpriteScene, "res://Asset/Anime/Character/Zombie/Chapter1/Football/Sprite/ZombieFootball.tscn") && hypnoImpSpriteScene != footballSpriteScene, "The two cards must load distinct authored character Sprite scenes.");
				hadPreviousHypnoImpSprite = resources.CHARCTAER_SPRITE.TryGetValue("ZombieImpHypnoShroom", out previousHypnoImpSprite);
				hadPreviousFootballSprite = resources.CHARCTAER_SPRITE.TryGetValue("ZombieFootball", out previousFootballSprite);
				resources.CHARCTAER_SPRITE["ZombieImpHypnoShroom"] = hypnoImpSpriteScene;
				resources.CHARCTAER_SPRITE["ZombieFootball"] = footballSpriteScene;
				Check(TowerDefenseManager.GetPacketSpriteScene(hypnoImpPacket) == hypnoImpSpriteScene, "Packet preview lookup must select the Hypno-shroom Imp Sprite by packet saveKey.");
				Check(TowerDefenseManager.GetPacketSpriteScene(footballPacket) == footballSpriteScene, "Packet preview lookup must select the Football Sprite by packet saveKey.");
				hypnoImpShow = packedScene.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
				footballShow = packedScene.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(hypnoImpShow) && GodotObject.IsInstanceValid(footballShow), "Two independent production PacketShow controls must instantiate.");
				Require(GodotObject.IsInstanceValid(hypnoImpShow) && GodotObject.IsInstanceValid(footballShow), "PacketShow instantiation failed.");
				hypnoImpShow.setPcLayout = true;
				hypnoImpShow.onlyDraw = true;
				footballShow.setPcLayout = true;
				footballShow.onlyDraw = true;
				AddChild(hypnoImpShow, forceReadableName: false, InternalMode.Disabled);
				AddChild(footballShow, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				hypnoImpShow.Init(hypnoImpPacket);
				footballShow.Init(footballPacket);
				await WaitFrames(4);
				Check(hypnoImpShow.config == hypnoImpPacket && footballShow.config == footballPacket, "Each real card slot must retain its own packet configuration.");
				AdobeAnimateSprite sprite = hypnoImpShow.sprite;
				AdobeAnimateSprite sprite2 = footballShow.sprite;
				Check(sprite is ZombieImpHypnoShroomSprite && sprite.Name == (StringName)"ZombieImpHypnoShroom" && sprite.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Sprite/HypnoShroom/ZombieImpHypnoShroom.tscn", "The Hypno-shroom Imp slot must instantiate its dedicated preview role, not the base Imp.");
				Check(GodotObject.IsInstanceValid(sprite) && sprite.GetParent() == hypnoImpShow.previewSpriteNode && sprite.Visible && sprite.IsFrozenPreview && sprite.clip == hypnoImpPacket.packetAnimeClip, "The Hypno-shroom Imp preview must be mounted, visible, frozen, and use the authored clip.");
				Check(GodotObject.IsInstanceValid(sprite) && ResourcePathMatches(sprite.flashAnimeData, "res://Asset/Anime/Character/Zombie/Chapter1/Imp/ZombieImp.tres"), "The Hypno-shroom Imp preview must use the authored Imp animation data.");
				AdobeAnimateSpriteBase adobeAnimateSpriteBase = sprite?.GetNodeOrNull<AdobeAnimateSpriteBase>("%Head");
				Check(GodotObject.IsInstanceValid(adobeAnimateSpriteBase) && ResourcePathMatches(adobeAnimateSpriteBase.flashAnimeData, "res://Asset/Anime/Character/Plant/Chapter0/HypnoShroom/HypnoShroom.tres"), "The dedicated Imp preview must visibly include the authored Hypno-shroom head child.");
				int count = 0;
				Check(TryCountDrawItems(sprite, out count) && count > 0, $"The Hypno-shroom Imp card must submit real preview draw items; count={count}.");
				Check(GodotObject.IsInstanceValid(sprite2) && !(sprite2 is ZombieImpHypnoShroomSprite) && sprite2.Name == (StringName)"ZombieFootball" && sprite2.SceneFilePath == "res://Asset/Anime/Character/Zombie/Chapter1/Football/Sprite/ZombieFootball.tscn", "The Football slot must instantiate its dedicated preview role, not the Hypno-shroom Imp.");
				Check(GodotObject.IsInstanceValid(sprite2) && sprite2.GetParent() == footballShow.previewSpriteNode && sprite2.Visible && sprite2.IsFrozenPreview && sprite2.clip == footballPacket.packetAnimeClip, "The Football preview must be mounted, visible, frozen, and use the authored clip.");
				Check(GodotObject.IsInstanceValid(sprite2) && ResourcePathMatches(sprite2.flashAnimeData, "res://Asset/Anime/Character/Zombie/Chapter1/Football/ZombieFootball.tres"), "The Football preview must use the authored Football animation data.");
				Check(GodotObject.IsInstanceValid(sprite2) && sprite2.GetFliter("zombie_football_helmet"), "The real Football packet's Helmet armor layer must be visible in the card preview.");
				int count2 = 0;
				Check(TryCountDrawItems(sprite2, out count2) && count2 > 0, $"The Football card must submit real preview draw items; count={count2}.");
				Check(sprite != sprite2 && sprite?.flashAnimeData != sprite2?.flashAnimeData, "The two simultaneous card slots must keep distinct preview nodes and animation data.");
				Check(resources.GetCharacterSprite("ZombieImpHypnoShroom") == hypnoImpSpriteScene && resources.GetCharacterSprite("ZombieFootball") == footballSpriteScene, "ResourceManager must retain both exact packet-to-Sprite mappings after both slots initialize.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[{"BugDepartmentHypnoPacketPreviewRuntimeTest"}] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(hypnoImpShow))
			{
				hypnoImpShow.QueueFree();
			}
			if (GodotObject.IsInstanceValid(footballShow))
			{
				footballShow.QueueFree();
			}
			if (GodotObject.IsInstanceValid(resources))
			{
				RestoreResource(resources.CHARCTAER_SPRITE, "ZombieImpHypnoShroom", hadPreviousHypnoImpSprite, previousHypnoImpSprite);
				RestoreResource(resources.CHARCTAER_SPRITE, "ZombieFootball", hadPreviousFootballSprite, previousFootballSprite);
			}
			await WaitFrames(3);
		}
		bool flag = _checks == 21 && _failures == 0;
		GD.Print($"{"BUG_DEPARTMENT_HYPNO_PACKET_PREVIEW_RESULT"} passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool ResourcePathMatches(Resource resource, string expectedPath)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			return string.Equals(resource.ResourcePath, expectedPath, StringComparison.Ordinal);
		}
		return false;
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
			GD.PushError("[BugDepartmentHypnoPacketPreviewRuntimeTest] " + message);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
