using Godot;

internal static class AdobeAnimateCpuVisualClassifier
{
	public static AdobeAnimateCpuVisualClassification ClassifyManagedSlot(in AdobeAnimateManagedSlotSprite pair, Rid expectedAtlasArrayRid, in AdobeAnimateDrawItem drawItem, in AdobeAnimateSortPath sortPath, bool? logicallyVisibleOverride = null)
	{
		AdobeAnimateSlot slot = pair.Slot;
		CanvasItem visual = pair.Visual;
		Sprite2D sprite = pair.Sprite;
		string resourceIdentity = GetResourceIdentity(visual, "managed-slot");
		if (!GodotObject.IsInstanceValid(slot) || !GodotObject.IsInstanceValid(visual) || visual.GetParent() != slot)
		{
			return Invalid("managed-slot-unavailable", resourceIdentity, "The managed Slot visual is detached or unavailable.");
		}
		bool? flag = logicallyVisibleOverride;
		bool num;
		if (!flag.HasValue)
		{
			if (!AdobeAnimateManagedSprite2D.GetLogicalVisible(visual))
			{
				goto IL_007f;
			}
			if (slot.Visible)
			{
				goto IL_0085;
			}
			num = slot.useFollowVisible;
		}
		else
		{
			num = flag == true;
		}
		if (!num)
		{
			goto IL_007f;
		}
		goto IL_0085;
		IL_0085:
		if (GodotObject.IsInstanceValid(pair.AtlasPart))
		{
			return ClassifyAtlasPart(pair.AtlasPart, expectedAtlasArrayRid, resourceIdentity, in drawItem);
		}
		Texture2D texture = sprite.Texture;
		Rect2 sourceRect = (GodotObject.IsInstanceValid(texture) ? AdobeAnimateDrawItemBuilder.ResolveSpriteSourceRect(sprite, texture) : default(Rect2));
		return ClassifySprite(sprite, texture, sourceRect, expectedAtlasArrayRid, resourceIdentity, in drawItem, in sortPath);
		IL_007f:
		return Ignored();
	}

	private static AdobeAnimateCpuVisualClassification ClassifyAtlasPart(AdobeAnimatePart atlasPart, Rid expectedAtlasArrayRid, string resourceIdentity, in AdobeAnimateDrawItem drawItem)
	{
		if (!GodotObject.IsInstanceValid(atlasPart) || string.IsNullOrWhiteSpace(atlasPart.externalAtlasTexturePath))
		{
			return Invalid("atlas-part-unavailable", resourceIdentity, "The managed Atlas Part has no shared-atlas texture reference.");
		}
		if (!AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasPart.externalAtlasTexturePath, out var allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || allocation.TextureArrayRid != expectedAtlasArrayRid || allocation.TextureArraySize.X <= 0f || allocation.TextureArraySize.Y <= 0f || allocation.Rect.Size.X <= 0f || allocation.Rect.Size.Y <= 0f)
		{
			return Invalid("atlas-part-allocation-invalid", resourceIdentity, "The managed Atlas Part is not available in the owner's shared atlas array.");
		}
		return new AdobeAnimateCpuVisualClassification(AdobeAnimateCpuVisualDisposition.MergedMesh, drawItem, default(AdobeAnimateCpuNativeSpriteItem), default(AdobeAnimateCpuVisualFailure), allocation);
	}

	public static AdobeAnimateCpuVisualClassification ClassifyExternalVisual(in AdobeAnimateExternalVisualSnapshot visual, Rid expectedAtlasArrayRid, in AdobeAnimateDrawItem drawItem, in AdobeAnimateSortPath sortPath)
	{
		Sprite2D sprite = visual.Sprite;
		string resourceIdentity = GetResourceIdentity(sprite, "external-visual");
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return Invalid("sprite-unavailable", resourceIdentity, "The registered external Sprite2D is unavailable.");
		}
		if (!visual.Visible)
		{
			return Ignored();
		}
		if (visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot && !GodotObject.IsInstanceValid(visual.Descriptor.Slot))
		{
			return Invalid("external-slot-unavailable", resourceIdentity, "The registered external visual has no valid Slot attachment.");
		}
		Texture2D texture = visual.Texture;
		Rect2 sourceRect = (GodotObject.IsInstanceValid(texture) ? AdobeAnimateDrawItemBuilder.ResolveSpriteSourceRect(sprite, texture) : default(Rect2));
		return ClassifySprite(sprite, texture, sourceRect, expectedAtlasArrayRid, resourceIdentity, in drawItem, in sortPath);
	}

	private static AdobeAnimateCpuVisualClassification ClassifySprite(Sprite2D sprite, Texture2D texture, Rect2 sourceRect, Rid expectedAtlasArrayRid, string resourceIdentity, in AdobeAnimateDrawItem drawItem, in AdobeAnimateSortPath sortPath)
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return Invalid("sprite-unavailable", resourceIdentity, "The Sprite2D is unavailable.");
		}
		if (!AdobeAnimateManagedSprite2D.GetLogicalVisible(sprite))
		{
			return Ignored();
		}
		if (!GodotObject.IsInstanceValid(texture) || sourceRect.Size.X <= 0f || sourceRect.Size.Y <= 0f)
		{
			return Invalid("texture-or-source-rect-invalid", resourceIdentity, "The Sprite2D has no valid texture source rectangle.");
		}
		if (sprite.Material != null && !AdobeAnimateManagedSprite2D.IsBatchMaterialCompatible(sprite))
		{
			return NativeSprite(sprite, in sortPath, resourceIdentity);
		}
		if (!AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture, sourceRect, out var allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || allocation.TextureArrayRid != expectedAtlasArrayRid)
		{
			return NativeSprite(sprite, in sortPath, resourceIdentity);
		}
		return new AdobeAnimateCpuVisualClassification(AdobeAnimateCpuVisualDisposition.MergedMesh, drawItem, default(AdobeAnimateCpuNativeSpriteItem), default(AdobeAnimateCpuVisualFailure), allocation);
	}

	private static AdobeAnimateCpuVisualClassification NativeSprite(Sprite2D sprite, in AdobeAnimateSortPath sortPath, string resourceIdentity)
	{
		return new AdobeAnimateCpuVisualClassification(AdobeAnimateCpuVisualDisposition.NativeSprite, default(AdobeAnimateDrawItem), new AdobeAnimateCpuNativeSpriteItem(sprite, sortPath, resourceIdentity, AdobeAnimateCpuNativeDrawBand.Unassigned, 0), default(AdobeAnimateCpuVisualFailure), default(AdobeAnimateExternalTextureAtlasAllocation));
	}

	private static AdobeAnimateCpuVisualClassification Ignored()
	{
		return new AdobeAnimateCpuVisualClassification(AdobeAnimateCpuVisualDisposition.IgnoredInvisible, default(AdobeAnimateDrawItem), default(AdobeAnimateCpuNativeSpriteItem), default(AdobeAnimateCpuVisualFailure), default(AdobeAnimateExternalTextureAtlasAllocation));
	}

	private static AdobeAnimateCpuVisualClassification Invalid(string code, string identity, string detail)
	{
		return new AdobeAnimateCpuVisualClassification(AdobeAnimateCpuVisualDisposition.Invalid, default(AdobeAnimateDrawItem), default(AdobeAnimateCpuNativeSpriteItem), new AdobeAnimateCpuVisualFailure(code, identity, detail), default(AdobeAnimateExternalTextureAtlasAllocation));
	}

	private static string GetResourceIdentity(CanvasItem visual, string fallback)
	{
		if (!GodotObject.IsInstanceValid(visual))
		{
			return fallback;
		}
		string text = (visual.IsInsideTree() ? visual.GetPath().ToString() : string.Empty);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return fallback + ":" + visual.Name;
	}
}
