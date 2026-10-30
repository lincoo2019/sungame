using System;
using Godot;

public partial class VasePacketChildClipProbe : Node
{
	private const string ResultMarker = "VASE_PACKET_CHILD_CLIP_RESULT";

	private const string PacketScenePath = "res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn";

	private const string PacketConfigPath = "res://Asset/Anime/Character/Plant/Chapter0/ThreePeater/Packet/PlantThreePeater.tres";

	private const string CharacterSpritePath = "res://Asset/Anime/Character/Plant/Chapter0/ThreePeater/ThreePeater.tscn";

	public override async void _Ready()
	{
		bool passed = false;
		string failure = string.Empty;
		try
		{
			PackedScene packedScene = GD.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn");
			TowerDefensePacketConfig packetConfig = GD.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/ThreePeater/Packet/PlantThreePeater.tres");
			PackedScene packedScene2 = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/ThreePeater/ThreePeater.tscn");
			if (packedScene == null || packetConfig == null || packedScene2 == null)
			{
				throw new InvalidOperationException("Unable to load the packet, config, or ThreePeater sprite scene.");
			}
			if (ResourceManager.Instance == null)
			{
				throw new InvalidOperationException("ResourceManager autoload is unavailable.");
			}
			ResourceManager.Instance.CHARCTAER_SPRITE[packetConfig.saveKey] = packedScene2;
			TowerDefenseInGamePacketShow packet = packedScene.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
			AddChild(packet, forceReadableName: false, InternalMode.Disabled);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			packet.Init(packetConfig);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			AdobeAnimateSprite sprite = packet.sprite;
			AdobeAnimateSprite adobeAnimateSprite = sprite?.FindChild("Head1", recursive: true, owned: false) as AdobeAnimateSprite;
			AdobeAnimateSprite adobeAnimateSprite2 = sprite?.FindChild("Head2", recursive: true, owned: false) as AdobeAnimateSprite;
			AdobeAnimateSprite adobeAnimateSprite3 = sprite?.FindChild("Head3", recursive: true, owned: false) as AdobeAnimateSprite;
			passed = GodotObject.IsInstanceValid(sprite) && sprite.clip == "BodyIdle" && GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.clip == "HeadIdle1" && GodotObject.IsInstanceValid(adobeAnimateSprite2) && adobeAnimateSprite2.clip == "HeadIdle2" && GodotObject.IsInstanceValid(adobeAnimateSprite3) && adobeAnimateSprite3.clip == "HeadIdle3";
			if (!passed)
			{
				failure = $"root={sprite?.clip ?? "<missing>"} head1={adobeAnimateSprite?.clip ?? "<missing>"} head2={adobeAnimateSprite2?.clip ?? "<missing>"} head3={adobeAnimateSprite3?.clip ?? "<missing>"}";
			}
		}
		catch (Exception ex)
		{
			failure = ex.ToString();
		}
		GD.Print(passed ? "VASE_PACKET_CHILD_CLIP_RESULT passed=True" : ("VASE_PACKET_CHILD_CLIP_RESULT passed=False failure=" + failure));
		GetTree().Quit((!passed) ? 2 : 0);
	}
}
