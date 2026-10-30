using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewButterArmorLossRenderOrderRuntimeTest : Node
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		BugOverviewButterArmorLossControlStub control = null;
		TowerDefenseZombieNormal zombie = null;
		TowerDefenseCharacterBuffButter butter = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_007b;
				}
				control = new BugOverviewButterArmorLossControlStub
				{
					Name = "ButterArmorLossControl",
					isGameRunning = false,
					isInit = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				zombie = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(zombie), "The fixture must instantiate the real ordinary Zombie scene.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_007b;
				}
				zombie.editorPreviewMode = true;
				zombie.inGame = false;
				control.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(zombie.instance) && GodotObject.IsInstanceValid(zombie.sprite) && GodotObject.IsInstanceValid(zombie.headSlot), "The real Zombie runtime, animation root, and authored HeadSlot must initialize.");
				if (!GodotObject.IsInstanceValid(zombie.instance) || !GodotObject.IsInstanceValid(zombie.sprite) || !GodotObject.IsInstanceValid(zombie.headSlot))
				{
					goto end_IL_007b;
				}
				TowerDefenseArmorRegistry.Init();
				Check(TowerDefenseArmorRegistry.GetArmorType("Helmet") != null, "The production armor registry must provide the ordinary Helmet armor.");
				zombie.instance.ArmorAdd("Helmet");
				await WaitFrames(3);
				TowerDefenseArmorInstance towerDefenseArmorInstance = FindArmor(zombie, "Helmet");
				Check(towerDefenseArmorInstance != null && zombie.instance.ArmorHas("Helmet"), "The real ordinary Helmet must be active before the regression transition.");
				Check(towerDefenseArmorInstance != null && GodotObject.IsInstanceValid(towerDefenseArmorInstance.sprite) && towerDefenseArmorInstance.sprite.GetParent() == zombie.headSlot, "The ordinary Helmet fixture must exercise a direct Atlas Part armor attached to HeadSlot.");
				int num = -1;
				AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = zombie.sprite.GetManagedSlotSpritesForRender();
				for (int i = 0; i < managedSlotSpritesForRender.Length; i++)
				{
					if (managedSlotSpritesForRender[i].AtlasPart == towerDefenseArmorInstance.sprite && managedSlotSpritesForRender[i].Slot == zombie.headSlot)
					{
						num = i;
						break;
					}
				}
				bool flag = num >= 0;
				Check(flag, "The direct Atlas Part Helmet must join the HeadSlot managed render queue.");
				Check(flag && !towerDefenseArmorInstance.sprite.Visible && AdobeAnimateManagedSprite2D.GetLogicalVisible(towerDefenseArmorInstance.sprite), "The managed Helmet must suppress native drawing while retaining logical visibility.");
				AdobeAnimateGpuRenderGraphDefinition graph;
				AdobeAnimateSprite[] ownerSprites;
				string failureReason;
				bool flag2 = AdobeAnimateGpuRenderGraphBuilder.TryBuild(zombie.sprite, out graph, out ownerSprites, out failureReason);
				AdobeAnimateGpuManagedVisualBinding adobeAnimateGpuManagedVisualBinding = default(AdobeAnimateGpuManagedVisualBinding);
				bool flag3 = false;
				if (flag2)
				{
					for (int j = 0; j < graph.ManagedVisualBindings.Length; j++)
					{
						AdobeAnimateGpuManagedVisualBinding adobeAnimateGpuManagedVisualBinding2 = graph.ManagedVisualBindings[j];
						if (adobeAnimateGpuManagedVisualBinding2.SourceKind == AdobeAnimateGpuManagedVisualSourceKind.SlotSprite2D && adobeAnimateGpuManagedVisualBinding2.VisualIndex == num && adobeAnimateGpuManagedVisualBinding2.OwnerIndex >= 0 && adobeAnimateGpuManagedVisualBinding2.OwnerIndex < ownerSprites.Length && ownerSprites[adobeAnimateGpuManagedVisualBinding2.OwnerIndex] == zombie.sprite)
						{
							adobeAnimateGpuManagedVisualBinding = adobeAnimateGpuManagedVisualBinding2;
							flag3 = true;
							break;
						}
					}
				}
				Check(flag2 && flag3, "The direct Helmet must be encoded as a managed visual in the production GPU Graph; failure=" + failureReason + ".");
				AdobeAnimateGpuManagedVisualState state = default(AdobeAnimateGpuManagedVisualState);
				string failureReason2 = "managed Helmet binding unavailable";
				bool condition = flag3 && zombie.sprite.TryBuildManagedSlotGpuStateForRender(adobeAnimateGpuManagedVisualBinding.VisualIndex, graph.Owners[adobeAnimateGpuManagedVisualBinding.OwnerIndex].Definition.AtlasTextureArrayRid, out state, out failureReason2) && state.Visible && state.SourceSize.X > 0f && state.SourceSize.Y > 0f;
				Check(condition, "The production GPU state must publish visible geometry for the direct Helmet; failure=" + failureReason2 + ".");
				AdobeAnimateRenderSnapshot snapshot;
				bool flag4 = zombie.sprite.TryBuildRenderSnapshot(out snapshot, allowUnchanged: false);
				List<AdobeAnimateDrawItem> list = new List<AdobeAnimateDrawItem>();
				int num2 = ((flag4 && snapshot.Definition != null) ? AdobeAnimateDrawItemBuilder.Build(snapshot, list, snapshot.Definition.GpuPoseTextureArray, 9399L) : 0);
				Check(flag4 && num2 > 0, "The real Zombie must build a non-empty Crowd draw list with Helmet active.");
				AdobeAnimateExternalTextureAtlasAllocation allocation = default(AdobeAnimateExternalTextureAtlasAllocation);
				bool flag5 = flag4 && snapshot.Definition != null && TryGetAtlasPartAllocation(towerDefenseArmorInstance.sprite, out allocation) && allocation.TextureArrayRid == snapshot.Definition.AtlasTextureArrayRid;
				Check(flag5, "The direct Helmet must resolve into the same shared atlas array as the Zombie.");
				AdobeAnimateDrawItem atlasItem = default(AdobeAnimateDrawItem);
				bool flag6 = flag5 && TryFindAtlasDrawItem(list, zombie.sprite, allocation, out atlasItem);
				Check(flag6, "The active Helmet shared-atlas entry must appear in the managed Crowd draw list.");
				int num3 = Math.Max(0, zombie.headSlot.ResolveDrawLayerId() - 1);
				Check(flag6 && atlasItem.SortPath.LayerOrder == num3, $"Helmet must sort at the authored HeadSlot animation layer; actual={atlasItem.SortPath.LayerOrder}, expected={num3}.");
				AdobeAnimateSlot headSlotBeforeArmorLoss = zombie.headSlot;
				butter = new TowerDefenseCharacterBuffButter
				{
					character = zombie
				};
				butter.Enter();
				await WaitFrames(4);
				AdobeAnimateExternalVisualSnapshot visual;
				bool flag7 = TryFindExternalVisual(zombie.sprite, butter.butterSprite, out visual);
				Check(GodotObject.IsInstanceValid(butter.butterSprite) && flag7, "Butter must register as a real external Crowd visual before armor loss.");
				Check(flag7 && visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot && visual.Descriptor.Slot == headSlotBeforeArmorLoss, "Butter must initially bind to the same persistent HeadSlot as the Helmet.");
				Check(headSlotBeforeArmorLoss.drawLayerId == -2, $"The real scene must author HeadSlot at Top before armor loss; actual={headSlotBeforeArmorLoss.drawLayerId}.");
				zombie.instance.ArmorDelete("Helmet", createDamagePart: false);
				await WaitFrames(4);
				Check(!zombie.instance.ArmorHas("Helmet") && FindArmor(zombie, "Helmet") == null, "The production ArmorDelete path must fully remove the ordinary Helmet.");
				Check(zombie.headSlot == headSlotBeforeArmorLoss && GodotObject.IsInstanceValid(headSlotBeforeArmorLoss), "Removing Helmet must preserve the exact HeadSlot node used by Butter.");
				Check(headSlotBeforeArmorLoss.drawLayerId == -2, $"HeadSlot must retain the Top sentinel after armor loss; actual={headSlotBeforeArmorLoss.drawLayerId}.");
				int num4 = headSlotBeforeArmorLoss.ResolveDrawLayerId();
				Check(num4 == zombie.sprite.layerVisible.Count + 1, $"HeadSlot Top must resolve dynamically above every Zombie animation layer after armor loss; resolved={num4}, layers={zombie.sprite.layerVisible.Count}.");
				Check(GodotObject.IsInstanceValid(butter.butterSprite), "Removing Helmet must not release the still-active Butter visual.");
				AdobeAnimateExternalVisualSnapshot visual2;
				bool flag8 = TryFindExternalVisual(zombie.sprite, butter.butterSprite, out visual2);
				Check(flag8 && visual2.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot && visual2.Descriptor.Slot == headSlotBeforeArmorLoss, "Butter must remain registered against the same HeadSlot after Helmet removal.");
				AdobeAnimateRenderSnapshot snapshot2;
				bool flag9 = zombie.sprite.TryBuildRenderSnapshot(out snapshot2, allowUnchanged: false);
				List<AdobeAnimateDrawItem> list2 = new List<AdobeAnimateDrawItem>();
				int num5 = ((flag9 && snapshot2.Definition != null) ? AdobeAnimateDrawItemBuilder.Build(snapshot2, list2, snapshot2.Definition.GpuPoseTextureArray, 9401L) : 0);
				Check(flag9 && num5 > 0, "The real Zombie must build a non-empty Crowd draw list after armor loss.");
				AdobeAnimateExternalTextureAtlasAllocation allocation2 = default(AdobeAnimateExternalTextureAtlasAllocation);
				bool flag10 = flag9 && snapshot2.Definition != null && flag8 && TryGetVisualAllocation(visual2, out allocation2) && allocation2.TextureArrayRid == snapshot2.Definition.AtlasTextureArrayRid;
				Check(flag10, "Butter must resolve into the same production atlas array as the Zombie Crowd snapshot.");
				AdobeAnimateDrawItem atlasItem2 = default(AdobeAnimateDrawItem);
				bool flag11 = flag10 && TryFindAtlasDrawItem(list2, zombie.sprite, allocation2, out atlasItem2);
				Check(flag11, "The post-removal Crowd draw list must contain the registered Butter atlas item.");
				int num6 = Math.Max(0, num4 - 1);
				Check(flag11 && atlasItem2.SortPath.LayerOrder == num6, $"Butter must use the resolved HeadSlot Top layer after armor loss; actual={atlasItem2.SortPath.LayerOrder}, expected={num6}.");
				int num7 = FindMaxAnimatedBodyLayer(list2);
				Check(flag11 && num7 >= 0 && atlasItem2.SortPath.LayerOrder > num7, $"Butter must sort above every shader-pose Zombie body layer after armor loss; butter={atlasItem2.SortPath.LayerOrder}, bodyMax={num7}.");
				goto end_IL_0060;
				end_IL_007b:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewButterArmorLossRenderOrderRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0060;
			}
			return;
			end_IL_0060:;
		}
		finally
		{
			butter?.Exit();
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.instance?.ArmorDelete("Helmet", createDamagePart: false);
				if (!zombie.IsQueuedForDeletion())
				{
					zombie.QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
		bool flag12 = _failures == 0 && _checks == 28;
		GD.Print($"BUTTER_ARMOR_LOSS_RENDER_ORDER_RESULT passed={flag12} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag12) ? 2 : 0);
	}

	private static TowerDefenseArmorInstance FindArmor(TowerDefenseCharacter character, string armorName)
	{
		if (character?.instance?.armorList == null)
		{
			return null;
		}
		foreach (TowerDefenseArmorInstance armor in character.instance.armorList)
		{
			if (armor?.slotConfig?.armorName == armorName && !armor.isRemove)
			{
				return armor;
			}
		}
		return null;
	}

	private static bool TryFindExternalVisual(AdobeAnimateSprite owner, Sprite2D visualSprite, out AdobeAnimateExternalVisualSnapshot visual)
	{
		visual = default(AdobeAnimateExternalVisualSnapshot);
		if (!GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(visualSprite))
		{
			return false;
		}
		ReadOnlySpan<int> activeExternalVisualIndicesForRender = owner.GetActiveExternalVisualIndicesForRender();
		for (int i = 0; i < activeExternalVisualIndicesForRender.Length; i++)
		{
			if (owner.TryGetExternalVisualForRender(activeExternalVisualIndicesForRender[i], out visual) && visual.Sprite == visualSprite)
			{
				return true;
			}
		}
		visual = default(AdobeAnimateExternalVisualSnapshot);
		return false;
	}

	private static bool TryGetVisualAllocation(AdobeAnimateExternalVisualSnapshot visual, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		allocation = default(AdobeAnimateExternalTextureAtlasAllocation);
		if (!GodotObject.IsInstanceValid(visual.Sprite) || !GodotObject.IsInstanceValid(visual.Texture))
		{
			return false;
		}
		Rect2 localSourceRect = (visual.Sprite.RegionEnabled ? visual.Sprite.RegionRect : new Rect2(Vector2.Zero, visual.Texture.GetSize()));
		return AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(visual.Texture, localSourceRect, out allocation);
	}

	private static bool TryGetAtlasPartAllocation(AdobeAnimatePart atlasPart, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		allocation = default(AdobeAnimateExternalTextureAtlasAllocation);
		if (GodotObject.IsInstanceValid(atlasPart) && !string.IsNullOrWhiteSpace(atlasPart.externalAtlasTexturePath))
		{
			return AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasPart.externalAtlasTexturePath, out allocation);
		}
		return false;
	}

	private static bool TryFindAtlasDrawItem(List<AdobeAnimateDrawItem> drawItems, AdobeAnimateSprite owner, AdobeAnimateExternalTextureAtlasAllocation allocation, out AdobeAnimateDrawItem atlasItem)
	{
		atlasItem = default(AdobeAnimateDrawItem);
		if (allocation.TextureArraySize.X <= 0f || allocation.TextureArraySize.Y <= 0f)
		{
			return false;
		}
		Rect2 rect = new Rect2(allocation.Rect.Position / allocation.TextureArraySize, allocation.Rect.Size / allocation.TextureArraySize);
		for (int i = 0; i < drawItems.Count; i++)
		{
			AdobeAnimateDrawItem adobeAnimateDrawItem = drawItems[i];
			if (adobeAnimateDrawItem.Owner == owner && !adobeAnimateDrawItem.UseShaderPose && adobeAnimateDrawItem.AtlasLayer == Math.Max(0, allocation.AtlasPage) && adobeAnimateDrawItem.Size.IsEqualApprox(allocation.Rect.Size) && adobeAnimateDrawItem.UvRect.Position.IsEqualApprox(rect.Position) && adobeAnimateDrawItem.UvRect.Size.IsEqualApprox(rect.Size))
			{
				atlasItem = adobeAnimateDrawItem;
				return true;
			}
		}
		return false;
	}

	private static int FindMaxAnimatedBodyLayer(List<AdobeAnimateDrawItem> drawItems)
	{
		int num = -1;
		for (int i = 0; i < drawItems.Count; i++)
		{
			if (drawItems[i].UseShaderPose)
			{
				num = Math.Max(num, drawItems[i].SortPath.LayerOrder);
			}
		}
		return num;
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
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
			GD.PushError("[BugOverviewButterArmorLossRenderOrderRuntimeTest] " + message);
		}
	}
}
