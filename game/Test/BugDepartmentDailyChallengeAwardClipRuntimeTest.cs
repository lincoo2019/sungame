using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentDailyChallengeAwardClipRuntimeTest : Node
{
	private const string DialogScenePath = "res://Prefab/GUI/DialogBox/DailyChallenge/Award/DailyChallengeAward.tscn";

	private const int ExpectedAwardCount = 4;

	private static readonly (string Key, string PacketPath, string SpritePath)[] AwardFixtures = new(string, string, string)[4]
	{
		("PlantFirenut", "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Packet/PlantFirenut.tres", "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Firenut.tscn"),
		("PlantSunBomb", "res://Asset/Anime/Character/Plant/Chapter1/SunBomb/Packet/PlantSunBomb.tres", "res://Asset/Anime/Character/Plant/Chapter1/SunBomb/SunBomb.tscn"),
		("PlantSunMine", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/Packet/PlantSunMine.tres", "res://Asset/Anime/Character/Plant/Chapter1/SunMine/SunMine.tscn"),
		("PlantReCactus", "res://Asset/Anime/Character/Plant/Chapter1/ReCactus/Packet/PlantReCactus.tres", "res://Asset/Anime/Character/Plant/Chapter1/ReCactus/ReCactus.tscn")
	};

	private int _checks;

	private int _failures;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly Dictionary<string, Resource> _previousSprites = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingSprites = new HashSet<string>();

	private readonly Dictionary<string, TowerDefensePacketConfig> _previousPacketCache = new Dictionary<string, TowerDefensePacketConfig>();

	private readonly HashSet<string> _missingPacketCache = new HashSet<string>();

	private readonly List<Resource> _fixtureResources = new List<Resource>();

	public override async void _Ready()
	{
		DailyChallengeAward dialog = null;
		PackedScene packedDialog = null;
		List<List<AdobeAnimateSprite>> rowSprites = new List<List<AdobeAnimateSprite>>();
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(GameSaveManager.Instance), "Required game autoloads must be available.");
				if (_failures > 0)
				{
					goto end_IL_008b;
				}
				bool flag = RegisterRealFixtures();
				Check(flag, "The four production July 2025 packet and preview resources must load.");
				if (!flag)
				{
					goto end_IL_008b;
				}
				packedDialog = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/DialogBox/DailyChallenge/Award/DailyChallengeAward.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(packedDialog), "The production Daily Challenge award dialog scene must load.");
				if (!GodotObject.IsInstanceValid(packedDialog))
				{
					goto end_IL_008b;
				}
				dialog = packedDialog.Instantiate<DailyChallengeAward>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(dialog), "The production Daily Challenge award dialog must instantiate.");
				if (!GodotObject.IsInstanceValid(dialog))
				{
					goto end_IL_008b;
				}
				AddChild(dialog, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				dialog.InitDialog(2025, 7, 31, 31);
				await WaitUntil(delegate
				{
					VBoxContainer itemContainer2 = dialog.itemContainer;
					return itemContainer2 != null && itemContainer2.GetChildCount() == 4;
				}, 300);
				ScrollContainer scroll = dialog.GetNodeOrNull<ScrollContainer>("Layer/BackgroundTexture/ScrollContainer");
				VBoxContainer itemContainer = dialog.itemContainer;
				Check(GodotObject.IsInstanceValid(scroll) && GodotObject.IsInstanceValid(itemContainer), "The production reward list must retain its ScrollContainer and ItemContainer.");
				Check(itemContainer != null && itemContainer.GetChildCount() == 4, $"The real July 2025 reward data must create {4} skin rows; got {itemContainer?.GetChildCount()}.");
				if (!GodotObject.IsInstanceValid(scroll) || !GodotObject.IsInstanceValid(itemContainer) || itemContainer.GetChildCount() != 4)
				{
					goto end_IL_008b;
				}
				await WaitFrames(20);
				Check(itemContainer.Size.Y > scroll.Size.Y, $"The real reward list must exceed the visible window so top/bottom clipping is exercised; content={itemContainer.Size}, viewport={scroll.Size}.");
				FieldInfo clipField = typeof(AdobeAnimateSprite).GetField("_renderClipControl", BindingFlags.Instance | BindingFlags.NonPublic);
				Check(clipField != null, "The runtime probe must be able to inspect the configured local-render clip owner.");
				for (int num = 0; num < 4; num++)
				{
					DailyChallengeAwardItem dailyChallengeAwardItem = itemContainer.GetChild(num) as DailyChallengeAwardItem;
					Check(GodotObject.IsInstanceValid(dailyChallengeAwardItem), $"Reward row {num} must use the production DailyChallengeAwardItem scene.");
					Check(dailyChallengeAwardItem?.data != null && !string.IsNullOrEmpty(dailyChallengeAwardItem.data.GetValueOrDefault("ShowCharacter", "").AsString()) && dailyChallengeAwardItem.data.GetValueOrDefault("ShowCustom", "").AsString() == "Custom0", $"Reward row {num} must show its authored plant Custom0 skin.");
					List<AdobeAnimateSprite> list = new List<AdobeAnimateSprite>();
					CollectAnimateSprites(dailyChallengeAwardItem, list);
					rowSprites.Add(list);
					Check(list.Count > 0, $"Reward row {num} must instantiate at least one real Adobe Animate plant sprite.");
					Check(list.Count > 0 && list.TrueForAll((AdobeAnimateSprite sprite) => sprite.forceLocalRender), $"Every animation in reward row {num} must opt out of the shared CanvasLayer mount.");
					Check(clipField != null && list.Count > 0 && list.TrueForAll((AdobeAnimateSprite sprite) => clipField.GetValue(sprite) == scroll), $"Every animation in reward row {num} must mount under the reward ScrollContainer clip.");
				}
				scroll.ScrollVertical = 0;
				await WaitFrames(3);
				DailyChallengeAwardItem dailyChallengeAwardItem2 = itemContainer.GetChild(3) as DailyChallengeAwardItem;
				Check(dailyChallengeAwardItem2.GetGlobalRect().End.Y > scroll.GetGlobalRect().End.Y, "At the top scroll position, the last plant row must extend below the visible window.");
				Node renderMount;
				bool flag2 = TryGetRenderMount(rowSprites[0][0], out renderMount);
				Check(flag2, "The visible first reward skin must build a real render snapshot.");
				Check(flag2 && renderMount == scroll, "The visible first reward skin snapshot must be clipped by the reward ScrollContainer.");
				scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
				await WaitFrames(4);
				Check(scroll.ScrollVertical > 0, "The production reward list must scroll far enough to exercise the upper boundary.");
				DailyChallengeAwardItem dailyChallengeAwardItem3 = itemContainer.GetChild(0) as DailyChallengeAwardItem;
				Check(dailyChallengeAwardItem3.GetGlobalRect().Position.Y < scroll.GetGlobalRect().Position.Y, "At the bottom scroll position, the first plant row must extend above the visible window.");
				Node renderMount2;
				bool flag3 = TryGetRenderMount(rowSprites[3][0], out renderMount2);
				Check(flag3, "The visible last reward skin must build a real render snapshot.");
				Check(flag3 && renderMount2 == scroll, "The visible last reward skin snapshot must be clipped by the reward ScrollContainer.");
				goto end_IL_006c;
				end_IL_008b:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentDailyChallengeAwardClipRuntimeTest] Unexpected exception: {value}");
				goto end_IL_006c;
			}
			return;
			end_IL_006c:;
		}
		finally
		{
			foreach (List<AdobeAnimateSprite> item in rowSprites)
			{
				foreach (AdobeAnimateSprite item2 in item)
				{
					if (GodotObject.IsInstanceValid(item2))
					{
						item2.Visible = false;
						item2.pause = true;
						item2.ClearRenderClipControl();
						item2.ReleaseForcedCpuPoseData();
						AdobeAnimateRenderManager.ReleaseImmediateSubmission(item2);
					}
				}
				item.Clear();
			}
			rowSprites.Clear();
			if (GodotObject.IsInstanceValid(dialog))
			{
				List<AdobeAnimateMultiMeshBatcher> list2 = new List<AdobeAnimateMultiMeshBatcher>();
				CollectNodes(dialog, list2);
				foreach (AdobeAnimateMultiMeshBatcher item3 in list2)
				{
					FreeNodeImmediately(item3);
				}
				List<AdobeAnimateRenderManager> list3 = new List<AdobeAnimateRenderManager>();
				CollectNodes(dialog, list3);
				foreach (AdobeAnimateRenderManager item4 in list3)
				{
					FreeNodeImmediately(item4);
				}
				await WaitFrames(2);
			}
			if (GodotObject.IsInstanceValid(dialog))
			{
				dialog.Free();
				dialog = null;
			}
			packedDialog?.Dispose();
			ReleaseStaticResource(typeof(DailyChallengeAward), "_dailyChallengeAwardItem");
			ReleaseStaticResource(typeof(DailyChallengeAwardProgressBar), "_rewardProgressTick");
			ReleaseStaticResource(typeof(ResourceManager), "_dailyLevelAward");
			RestoreRealFixtures();
			ObjectManager.Instance?.Clear();
			ResourceManager.Instance?.ReleaseTransientResources();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await WaitFrames(16);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WaitFrames(8);
		}
		bool flag4 = _failures == 0 && _checks == 35;
		GD.Print($"DAILY_CHALLENGE_AWARD_CLIP_RESULT passed={flag4} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag4) ? 2 : 0);
	}

	private bool RegisterRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		RememberPacketReferenceCache();
		bool flag = true;
		(string, string, string)[] awardFixtures = AwardFixtures;
		for (int i = 0; i < awardFixtures.Length; i++)
		{
			(string, string, string) tuple = awardFixtures[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(item2, null, ResourceLoader.CacheMode.Ignore);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>(item3, null, ResourceLoader.CacheMode.Ignore);
			flag &= GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(packedScene);
			if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
			{
				RememberAndReplace(instance.TOWERDEFENSE_PACKETS, _previousPackets, _missingPackets, item, towerDefensePacketConfig);
				_fixtureResources.Add(towerDefensePacketConfig);
			}
			if (GodotObject.IsInstanceValid(packedScene))
			{
				RememberAndReplace(instance.CHARCTAER_SPRITE, _previousSprites, _missingSprites, item, packedScene);
				_fixtureResources.Add(packedScene);
			}
		}
		return flag;
	}

	private static void RememberAndReplace(Dictionary<string, Resource> registry, Dictionary<string, Resource> previous, HashSet<string> missing, string key, Resource replacement)
	{
		if (registry.TryGetValue(key, out var value))
		{
			previous[key] = value;
		}
		else
		{
			missing.Add(key);
		}
		registry[key] = replacement;
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			foreach (string missingPacket in _missingPackets)
			{
				instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
			}
			foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
			{
				instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
			}
			foreach (string missingSprite in _missingSprites)
			{
				instance.CHARCTAER_SPRITE.Remove(missingSprite);
			}
			foreach (KeyValuePair<string, Resource> previousSprite in _previousSprites)
			{
				instance.CHARCTAER_SPRITE[previousSprite.Key] = previousSprite.Value;
			}
		}
		RestorePacketReferenceCache();
		foreach (Resource fixtureResource in _fixtureResources)
		{
			if (GodotObject.IsInstanceValid(fixtureResource))
			{
				fixtureResource.Dispose();
			}
		}
		_fixtureResources.Clear();
		_previousPackets.Clear();
		_missingPackets.Clear();
		_previousSprites.Clear();
		_missingSprites.Clear();
	}

	private void RememberPacketReferenceCache()
	{
		Dictionary<string, TowerDefensePacketConfig> packetReferenceCache = GetPacketReferenceCache();
		if (packetReferenceCache == null)
		{
			return;
		}
		(string, string, string)[] awardFixtures = AwardFixtures;
		for (int i = 0; i < awardFixtures.Length; i++)
		{
			string item = awardFixtures[i].Item1;
			if (packetReferenceCache.TryGetValue(item, out var value))
			{
				_previousPacketCache[item] = value;
			}
			else
			{
				_missingPacketCache.Add(item);
			}
		}
	}

	private void RestorePacketReferenceCache()
	{
		Dictionary<string, TowerDefensePacketConfig> packetReferenceCache = GetPacketReferenceCache();
		if (packetReferenceCache != null)
		{
			foreach (string item in _missingPacketCache)
			{
				packetReferenceCache.Remove(item);
			}
			foreach (KeyValuePair<string, TowerDefensePacketConfig> item2 in _previousPacketCache)
			{
				packetReferenceCache[item2.Key] = item2.Value;
			}
		}
		_previousPacketCache.Clear();
		_missingPacketCache.Clear();
	}

	private static Dictionary<string, TowerDefensePacketConfig> GetPacketReferenceCache()
	{
		return typeof(TowerDefenseManager).GetField("_packetConfigRefCache", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as Dictionary<string, TowerDefensePacketConfig>;
	}

	private async Task WaitUntil(Func<bool> condition, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (condition())
			{
				break;
			}
			await WaitFrames(1);
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

	private static void CollectAnimateSprites(Node node, List<AdobeAnimateSprite> result)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite item)
		{
			result.Add(item);
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			CollectAnimateSprites(child, result);
		}
	}

	private static void CollectNodes<T>(Node node, List<T> result) where T : Node
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is T item)
		{
			result.Add(item);
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			CollectNodes(child, result);
		}
	}

	private static void FreeNodeImmediately(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			Node parent = node.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(node);
			}
			node.Free();
		}
	}

	private static bool TryGetRenderMount(AdobeAnimateSprite sprite, out Node renderMount)
	{
		renderMount = null;
		if (!GodotObject.IsInstanceValid(sprite) || !sprite.TryBuildRenderSnapshot(out var snapshot))
		{
			return false;
		}
		renderMount = snapshot.RenderMountParent;
		return true;
	}

	private static void ReleaseStaticResource(Type ownerType, string fieldName)
	{
		FieldInfo field = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
		if (field?.GetValue(null) is Resource resource)
		{
			field.SetValue(null, null);
			resource.Dispose();
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[DailyChallengeAwardClip] " + message);
		}
	}
}
