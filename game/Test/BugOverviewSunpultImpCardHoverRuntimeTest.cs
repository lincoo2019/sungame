using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewSunpultImpCardHoverRuntimeTest : Node
{
	private const string PacketShowScenePath = "res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn";

	private const string PacketConfigPath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/Sunpult/ZombieImpSunpult.tres";

	private const string SpriteScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Sprite/Sunpult/ZombieImpSunpult.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseInGamePacketShow packetShow = null;
		Resource previousSprite = null;
		bool spriteWasMissing = true;
		try
		{
			_ = 4;
			try
			{
				ResourceManager instance = ResourceManager.Instance;
				Check(GodotObject.IsInstanceValid(instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(instance))
				{
					goto end_IL_005c;
				}
				TowerDefensePacketConfig packetConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/Sunpult/ZombieImpSunpult.tres", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Imp/Sprite/Sunpult/ZombieImpSunpult.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packetConfig) && packetConfig.saveKey == "ZombieImpSunpult" && packetConfig.packetAnimeClip == "Walk", "The fixture must load the real Sunpult Imp card and its Walk preview clip.");
				Check(GodotObject.IsInstanceValid(packedScene) && GodotObject.IsInstanceValid(packedScene2), "The real Sunpult Imp sprite and packet-show scenes must load.");
				if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(packedScene2))
				{
					goto end_IL_005c;
				}
				spriteWasMissing = !instance.CHARCTAER_SPRITE.TryGetValue(packetConfig.saveKey, out previousSprite);
				instance.CHARCTAER_SPRITE[packetConfig.saveKey] = packedScene;
				packetShow = packedScene2.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
				AddChild(packetShow, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				packetShow.Init(packetConfig);
				await WaitFrames(4);
				ZombieImpSunpultSprite root = packetShow.sprite as ZombieImpSunpultSprite;
				AdobeAnimateSpriteBase head = root?.head;
				Check(GodotObject.IsInstanceValid(root) && root.clip == "Walk", "The card must instantiate the real ZombieImpSunpultSprite in Walk.");
				Check(GodotObject.IsInstanceValid(head) && head.clip == "Idle", "The real card must retain its nested Sunpult head in Idle.");
				Check(root.IsFrozenPreview && head.IsFrozenPreview, "The idle card must begin as a visible frozen root-and-head preview.");
				int count = 0;
				int count2 = 0;
				Check(TryCountDrawItems(root, out count) && count > 0 && TryCountDrawItems(head, out count2) && count2 > 0, $"The frozen card must submit both body and head draw items; root={count}, head={count2}.");
				packetShow.OnMouseEntered();
				await WaitFrames(2);
				Check(packetShow.IsVisibleInTree() && packetShow.sprite.Visible && head.Visible, "Hover must keep the packet, root Imp, and nested Sunpult head visible.");
				Check(!root.IsFrozenPreview && !head.IsFrozenPreview, "Hover must unfreeze both the root Imp and nested Sunpult animation.");
				int rootStartFrame = root.frameIndex;
				int headStartFrame = head.frameIndex;
				int minimumRootItems = 2147483647;
				int minimumHeadItems = 2147483647;
				bool allSnapshotsVisible = true;
				for (int sample = 0; sample < 24; sample++)
				{
					await WaitFrames(1);
					int count3;
					bool flag = TryCountDrawItems(root, out count3);
					int count4;
					bool flag2 = TryCountDrawItems(head, out count4);
					minimumRootItems = Math.Min(minimumRootItems, count3);
					minimumHeadItems = Math.Min(minimumHeadItems, count4);
					allSnapshotsVisible &= flag && flag2 && count3 > 0 && count4 > 0 && packetShow.sprite.Visible && head.Visible;
				}
				Check(root.frameIndex != rootStartFrame, $"The hovered Imp Walk clip must advance; start={rootStartFrame}, end={root.frameIndex}.");
				Check(head.frameIndex != headStartFrame, $"The hovered nested Sunpult Idle clip must advance; start={headStartFrame}, end={head.frameIndex}.");
				Check(allSnapshotsVisible, $"Body and head must retain visible draw items throughout hover; minRoot={minimumRootItems}, minHead={minimumHeadItems}.");
				packetShow.OnMouseExited();
				await WaitFrames(2);
				Check(root.IsFrozenPreview && head.IsFrozenPreview, "Mouse exit must return both animations to the frozen preview state.");
				int count5 = 0;
				int count6 = 0;
				Check(packetShow.sprite.Visible && head.Visible && TryCountDrawItems(root, out count5) && count5 > 0 && TryCountDrawItems(head, out count6) && count6 > 0, $"Mouse exit must retain a visible body and head; root={count5}, head={count6}.");
				goto end_IL_003d;
				end_IL_005c:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSunpultImpCardHoverRuntimeTest] Unexpected exception: {value}");
				goto end_IL_003d;
			}
			return;
			end_IL_003d:;
		}
		finally
		{
			ResourceManager instance2 = ResourceManager.Instance;
			if (GodotObject.IsInstanceValid(instance2))
			{
				if (spriteWasMissing)
				{
					instance2.CHARCTAER_SPRITE.Remove("ZombieImpSunpult");
				}
				else
				{
					instance2.CHARCTAER_SPRITE["ZombieImpSunpult"] = previousSprite;
				}
			}
			if (GodotObject.IsInstanceValid(packetShow))
			{
				packetShow.QueueFree();
			}
			await WaitFrames(2);
		}
		bool flag3 = _failures == 0 && _checks == 14;
		GD.Print($"SUNPULT_IMP_CARD_HOVER_RESULT passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
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
			GD.PushError("[BugOverviewSunpultImpCardHoverRuntimeTest] " + message);
		}
	}
}
