using System;
using System.Runtime.CompilerServices;
using Godot;

internal static class AdobeAnimateManagedSprite2D
{
	private sealed class ManagedVisibilityState
	{
		public bool LogicalVisible;
	}

	private const string BatchBlinkShaderCode = "shader_typecanvas_item;uniformboolblink;voidfragment(){if(blink){floatblinkColor=0.75+sin(TIME*15.0)*0.25;COLOR*=vec4(blinkColor,blinkColor,blinkColor,1.0);}}";

	private const ulong HashOffset = 1469598103934665603uL;

	private const ulong HashPrime = 1099511628211uL;

	private const string ManagedMetaName = "_adobe_animate_managed_sprite2d";

	private const string ManagedVisibleMetaName = "_adobe_animate_managed_sprite2d_visible";

	private const string CpuNativeOrderMetaName = "_adobe_animate_cpu_native_order";

	private const string CpuNativeZIndexMetaName = "_adobe_animate_cpu_native_z_index";

	private const string CpuNativeZRelativeMetaName = "_adobe_animate_cpu_native_z_relative";

	private const string CpuNativeDrawIndexMetaName = "_adobe_animate_cpu_native_draw_index";

	private static readonly ConditionalWeakTable<CanvasItem, ManagedVisibilityState> ManagedVisibilityStates = new ConditionalWeakTable<CanvasItem, ManagedVisibilityState>();

	public static void MarkManaged(CanvasItem visual)
	{
		CommitCrowdTakeover(visual);
	}

	public static void SetLogicalVisible(CanvasItem visual, bool visible)
	{
		if (GodotObject.IsInstanceValid(visual))
		{
			if (TryGetManagedVisibilityState(visual, out var state))
			{
				state.LogicalVisible = visible;
				visual.SetMeta("_adobe_animate_managed_sprite2d_visible", visible);
				visual.Visible = false;
			}
			else
			{
				visual.Visible = visible;
			}
		}
	}

	public static bool GetLogicalVisible(CanvasItem visual)
	{
		return IsManagedVisible(visual);
	}

	public static bool IsCrowdManaged(CanvasItem visual)
	{
		ManagedVisibilityState state;
		if (GodotObject.IsInstanceValid(visual))
		{
			return TryGetManagedVisibilityState(visual, out state);
		}
		return false;
	}

	public static void CommitCrowdTakeover(CanvasItem visual)
	{
		if (!GodotObject.IsInstanceValid(visual))
		{
			return;
		}
		if (visual is Sprite2D sprite)
		{
			RestoreCpuNativeOrdering(sprite);
		}
		if (!TryGetManagedVisibilityState(visual, out var state))
		{
			state = ManagedVisibilityStates.GetValue(visual, (CanvasItem _) => new ManagedVisibilityState());
			state.LogicalVisible = visual.Visible;
			visual.SetMeta("_adobe_animate_managed_sprite2d", true);
			visual.SetMeta("_adobe_animate_managed_sprite2d_visible", state.LogicalVisible);
		}
		else if (visual.Visible)
		{
			state.LogicalVisible = true;
			visual.SetMeta("_adobe_animate_managed_sprite2d_visible", true);
		}
		visual.Visible = false;
	}

	public static void RestoreNative(CanvasItem visual)
	{
		if (GodotObject.IsInstanceValid(visual))
		{
			if (visual is Sprite2D sprite)
			{
				RestoreCpuNativeOrdering(sprite);
			}
			ManagedVisibilityState state;
			bool visible = (TryGetManagedVisibilityState(visual, out state) ? state.LogicalVisible : visual.Visible);
			ManagedVisibilityStates.Remove(visual);
			visual.Visible = visible;
			if (visual.HasMeta("_adobe_animate_managed_sprite2d_visible"))
			{
				visual.RemoveMeta("_adobe_animate_managed_sprite2d_visible");
			}
			if (visual.HasMeta("_adobe_animate_managed_sprite2d"))
			{
				visual.RemoveMeta("_adobe_animate_managed_sprite2d");
			}
		}
	}

	public static void CommitCpuNative(Sprite2D sprite, in AdobeAnimateSortPath sortPath, int nativeDrawIndex)
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			RestoreNative(sprite);
			int num = ((sprite.GetParent() != null) ? Math.Max(0, sprite.GetIndex()) : 0);
			sprite.SetMeta("_adobe_animate_cpu_native_order", true);
			sprite.SetMeta("_adobe_animate_cpu_native_z_index", sprite.ZIndex);
			sprite.SetMeta("_adobe_animate_cpu_native_z_relative", sprite.ZAsRelative);
			sprite.SetMeta("_adobe_animate_cpu_native_draw_index", num);
			sprite.ZAsRelative = false;
			sprite.ZIndex = sortPath.ZIndex;
			Rid canvasItem = sprite.GetCanvasItem();
			if (canvasItem.IsValid)
			{
				RenderingServer.CanvasItemSetZIndex(canvasItem, sortPath.ZIndex);
				RenderingServer.CanvasItemSetZAsRelativeToParent(canvasItem, enabled: false);
				RenderingServer.CanvasItemSetDrawIndex(canvasItem, nativeDrawIndex);
			}
		}
	}

	public static void RestoreCpuNativeOrdering(Sprite2D sprite)
	{
		if (GodotObject.IsInstanceValid(sprite) && sprite.HasMeta("_adobe_animate_cpu_native_order"))
		{
			int zIndex = (sprite.HasMeta("_adobe_animate_cpu_native_z_index") ? sprite.GetMeta("_adobe_animate_cpu_native_z_index").AsInt32() : sprite.ZIndex);
			bool zAsRelative = (sprite.HasMeta("_adobe_animate_cpu_native_z_relative") ? sprite.GetMeta("_adobe_animate_cpu_native_z_relative").AsBool() : sprite.ZAsRelative);
			int index = (sprite.HasMeta("_adobe_animate_cpu_native_draw_index") ? sprite.GetMeta("_adobe_animate_cpu_native_draw_index").AsInt32() : 0);
			sprite.ZIndex = zIndex;
			sprite.ZAsRelative = zAsRelative;
			Rid canvasItem = sprite.GetCanvasItem();
			if (canvasItem.IsValid)
			{
				RenderingServer.CanvasItemSetDrawIndex(canvasItem, index);
			}
			sprite.RemoveMeta("_adobe_animate_cpu_native_order");
			if (sprite.HasMeta("_adobe_animate_cpu_native_z_index"))
			{
				sprite.RemoveMeta("_adobe_animate_cpu_native_z_index");
			}
			if (sprite.HasMeta("_adobe_animate_cpu_native_z_relative"))
			{
				sprite.RemoveMeta("_adobe_animate_cpu_native_z_relative");
			}
			if (sprite.HasMeta("_adobe_animate_cpu_native_draw_index"))
			{
				sprite.RemoveMeta("_adobe_animate_cpu_native_draw_index");
			}
		}
	}

	public static bool ShouldRender(CanvasItem visual)
	{
		if (!GodotObject.IsInstanceValid(visual))
		{
			return false;
		}
		if (!TryGetManagedVisibilityState(visual, out var state))
		{
			return visual.Visible;
		}
		if (visual.Visible)
		{
			state.LogicalVisible = true;
			visual.SetMeta("_adobe_animate_managed_sprite2d_visible", true);
			visual.Visible = false;
		}
		return state.LogicalVisible;
	}

	public static bool IsManagedVisible(CanvasItem visual)
	{
		if (!GodotObject.IsInstanceValid(visual))
		{
			return false;
		}
		if (!TryGetManagedVisibilityState(visual, out var state))
		{
			return visual.Visible;
		}
		if (visual.Visible)
		{
			return true;
		}
		return state.LogicalVisible;
	}

	private static bool TryGetManagedVisibilityState(CanvasItem visual, out ManagedVisibilityState state)
	{
		if (ManagedVisibilityStates.TryGetValue(visual, out state))
		{
			return true;
		}
		if (!visual.HasMeta("_adobe_animate_managed_sprite2d"))
		{
			return false;
		}
		state = ManagedVisibilityStates.GetValue(visual, (CanvasItem _) => new ManagedVisibilityState());
		state.LogicalVisible = (visual.HasMeta("_adobe_animate_managed_sprite2d_visible") ? visual.GetMeta("_adobe_animate_managed_sprite2d_visible").AsBool() : visual.Visible);
		return true;
	}

	public static bool GetBlinkEnabled(Sprite2D sprite)
	{
		if (!GodotObject.IsInstanceValid(sprite) || !(sprite.Material is ShaderMaterial shaderMaterial))
		{
			return false;
		}
		Variant shaderParameter = shaderMaterial.GetShaderParameter("blink");
		if (shaderParameter.VariantType == Variant.Type.Bool)
		{
			return shaderParameter.AsBool();
		}
		return false;
	}

	public static bool IsBatchMaterialCompatible(Sprite2D sprite)
	{
		if (!GodotObject.IsInstanceValid(sprite) || sprite.Material == null)
		{
			return true;
		}
		if (!(sprite.Material is ShaderMaterial shaderMaterial) || !GodotObject.IsInstanceValid(shaderMaterial.Shader))
		{
			return false;
		}
		return string.Equals((shaderMaterial.Shader.Code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).Replace("\t", string.Empty, StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal)
			.Replace("\n", string.Empty, StringComparison.Ordinal), "shader_typecanvas_item;uniformboolblink;voidfragment(){if(blink){floatblinkColor=0.75+sin(TIME*15.0)*0.25;COLOR*=vec4(blinkColor,blinkColor,blinkColor,1.0);}}", StringComparison.Ordinal);
	}

	public static ulong BuildTopologySignature(in AdobeAnimateManagedSlotSprite pair)
	{
		AdobeAnimateSlot slot = pair.Slot;
		CanvasItem visual = pair.Visual;
		if (!GodotObject.IsInstanceValid(slot) || !GodotObject.IsInstanceValid(visual))
		{
			return 0uL;
		}
		ulong hash = 1469598103934665603uL;
		Add(ref hash, slot.Name.ToString());
		Add(ref hash, visual.Name.ToString());
		Add(ref hash, pair.AtlasPart != null);
		Add(ref hash, slot.mode);
		Add(ref hash, slot.followSlotId);
		Add(ref hash, slot.ResolveDrawLayerId());
		return hash;
	}

	public static ulong BuildGpuStateSignature(in AdobeAnimateManagedSlotSprite pair)
	{
		AdobeAnimateSlot slot = pair.Slot;
		AdobeAnimatePart atlasPart = pair.AtlasPart;
		Sprite2D sprite = pair.Sprite;
		CanvasItem visual = pair.Visual;
		if (!GodotObject.IsInstanceValid(slot) || !GodotObject.IsInstanceValid(visual))
		{
			return 0uL;
		}
		ulong hash = 1469598103934665603uL;
		if (GodotObject.IsInstanceValid(atlasPart))
		{
			Add(ref hash, atlasPart.externalAtlasTexturePath);
			Add(ref hash, atlasPart.externalAtlasCentered);
		}
		else
		{
			Texture2D texture = sprite.Texture;
			Add(ref hash, GodotObject.IsInstanceValid(texture) ? texture.GetInstanceId() : 0);
		}
		Add(ref hash, slot.mode);
		Add(ref hash, slot.followSlotId);
		Add(ref hash, slot.useRotate);
		Add(ref hash, slot.useScale);
		Add(ref hash, slot.useSkew);
		Add(ref hash, slot.useFollowVisible);
		Add(ref hash, slot.offset);
		Add(ref hash, slot.Modulate);
		Add(ref hash, slot.Visible || slot.useFollowVisible);
		Add(ref hash, IsManagedVisible(visual));
		Add(ref hash, (visual is Node2D node2D) ? node2D.Transform : Transform2D.Identity);
		Add(ref hash, visual.Modulate);
		if (GodotObject.IsInstanceValid(sprite))
		{
			Add(ref hash, sprite.Offset);
			Add(ref hash, sprite.Centered);
			Add(ref hash, sprite.FlipH);
			Add(ref hash, sprite.FlipV);
			Add(ref hash, sprite.RegionEnabled);
			Add(ref hash, sprite.RegionRect);
			Add(ref hash, sprite.Hframes);
			Add(ref hash, sprite.Vframes);
			Add(ref hash, sprite.Frame);
			Add(ref hash, GetBlinkEnabled(sprite));
		}
		return hash;
	}

	public static ulong BuildGpuStateFastSignature(in AdobeAnimateManagedSlotSprite pair)
	{
		AdobeAnimateSlot slot = pair.Slot;
		CanvasItem visual = pair.Visual;
		if (!GodotObject.IsInstanceValid(slot) || !GodotObject.IsInstanceValid(visual))
		{
			return 0uL;
		}
		ulong hash = 1469598103934665603uL;
		Add(ref hash, slot.mode);
		Add(ref hash, slot.followSlotId);
		Add(ref hash, slot.useRotate);
		Add(ref hash, slot.useScale);
		Add(ref hash, slot.useSkew);
		Add(ref hash, slot.useFollowVisible);
		Add(ref hash, slot.offset);
		Add(ref hash, slot.Modulate);
		Add(ref hash, slot.Visible || slot.useFollowVisible);
		Add(ref hash, IsManagedVisible(visual));
		Add(ref hash, visual.Modulate);
		if (GodotObject.IsInstanceValid(pair.Sprite))
		{
			Add(ref hash, GetBlinkEnabled(pair.Sprite));
		}
		return hash;
	}

	public static ulong BuildExternalGpuStateSignature(in AdobeAnimateExternalVisualSnapshot visual, in Transform2D localTransform)
	{
		Sprite2D sprite = visual.Sprite;
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return 0uL;
		}
		ulong hash = 1469598103934665603uL;
		Texture2D texture = visual.Texture;
		Add(ref hash, GodotObject.IsInstanceValid(texture) ? texture.GetInstanceId() : 0);
		Add(ref hash, (int)visual.Descriptor.AttachmentMode);
		Add(ref hash, localTransform);
		Add(ref hash, visual.Visible);
		Add(ref hash, visual.Modulate);
		Add(ref hash, sprite.Offset);
		Add(ref hash, sprite.Centered);
		Add(ref hash, sprite.FlipH);
		Add(ref hash, sprite.FlipV);
		Add(ref hash, sprite.RegionEnabled);
		Add(ref hash, sprite.RegionRect);
		Add(ref hash, sprite.Hframes);
		Add(ref hash, sprite.Vframes);
		Add(ref hash, sprite.Frame);
		Add(ref hash, GetBlinkEnabled(sprite));
		AdobeAnimateSlot slot = visual.Descriptor.Slot;
		if (visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot && GodotObject.IsInstanceValid(slot))
		{
			Add(ref hash, slot.mode);
			Add(ref hash, slot.followSlotId);
			Add(ref hash, slot.useRotate);
			Add(ref hash, slot.useScale);
			Add(ref hash, slot.useSkew);
			Add(ref hash, slot.useFollowVisible);
			Add(ref hash, slot.offset);
			Add(ref hash, slot.Modulate);
			Add(ref hash, slot.Visible || slot.useFollowVisible);
		}
		return hash;
	}

	public static bool TryBuildGpuState(in AdobeAnimateManagedSlotSprite pair, Rid expectedAtlasArrayRid, out AdobeAnimateGpuManagedVisualState state, out string failureReason)
	{
		state = default(AdobeAnimateGpuManagedVisualState);
		failureReason = "";
		AdobeAnimateSlot slot = pair.Slot;
		AdobeAnimatePart atlasPart = pair.AtlasPart;
		Sprite2D sprite = pair.Sprite;
		if (!GodotObject.IsInstanceValid(slot) || !GodotObject.IsInstanceValid(pair.Visual) || pair.Visual.GetParent() != slot)
		{
			failureReason = "managed Slot visual is invalid or detached";
			return false;
		}
		if (slot.mode != 0 || slot.followSlotId <= 0)
		{
			failureReason = $"managed Slot visual {pair.Visual.Name} requires unsupported slot mode={slot.mode} follow={slot.followSlotId}";
			return false;
		}
		if (GodotObject.IsInstanceValid(atlasPart))
		{
			return TryBuildAtlasPartGpuState(atlasPart, slot, expectedAtlasArrayRid, out state, out failureReason);
		}
		Texture2D texture = sprite.Texture;
		if (!GodotObject.IsInstanceValid(texture))
		{
			failureReason = $"managed Sprite2D {sprite.Name} has no texture";
			return false;
		}
		Rect2 localSourceRect = AdobeAnimateDrawItemBuilder.ResolveSpriteSourceRect(sprite, texture);
		if (localSourceRect.Size.X <= 0f || localSourceRect.Size.Y <= 0f || !AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture, localSourceRect, out var allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || allocation.TextureArrayRid != expectedAtlasArrayRid || allocation.TextureArraySize.X <= 0f || allocation.TextureArraySize.Y <= 0f)
		{
			failureReason = $"managed Sprite2D {sprite.Name} texture is not in the owner's shared atlas array";
			return false;
		}
		Rect2 rect = allocation.Rect;
		Vector2 size = rect.Size;
		state = new AdobeAnimateGpuManagedVisualState(size, AdobeAnimateDrawItemBuilder.NormalizeRect(rect, allocation.TextureArraySize), allocation.AtlasPage, AdobeAnimateDrawItemBuilder.ResolveSpriteDrawOrigin(sprite, size), sprite.Transform, AdobeAnimateDrawItemBuilder.Multiply(slot.Modulate, sprite.Modulate), slot.offset, sprite.FlipH, sprite.FlipV, slot.useRotate, slot.useScale, slot.useSkew, ShouldRender(sprite) && (slot.Visible || slot.useFollowVisible), GetBlinkEnabled(sprite));
		return true;
	}

	private static bool TryBuildAtlasPartGpuState(AdobeAnimatePart atlasPart, AdobeAnimateSlot slot, Rid expectedAtlasArrayRid, out AdobeAnimateGpuManagedVisualState state, out string failureReason)
	{
		state = default(AdobeAnimateGpuManagedVisualState);
		failureReason = "";
		if (string.IsNullOrWhiteSpace(atlasPart.externalAtlasTexturePath) || !AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasPart.externalAtlasTexturePath, out var allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || allocation.TextureArrayRid != expectedAtlasArrayRid || allocation.TextureArraySize.X <= 0f || allocation.TextureArraySize.Y <= 0f || allocation.Rect.Size.X <= 0f || allocation.Rect.Size.Y <= 0f)
		{
			failureReason = $"managed Atlas Part {atlasPart.Name} texture is not in the owner's shared atlas array";
			return false;
		}
		Vector2 size = allocation.Rect.Size;
		state = new AdobeAnimateGpuManagedVisualState(size, AdobeAnimateDrawItemBuilder.NormalizeRect(allocation.Rect, allocation.TextureArraySize), allocation.AtlasPage, atlasPart.externalAtlasCentered ? (-size * 0.5f) : Vector2.Zero, atlasPart.Transform, AdobeAnimateDrawItemBuilder.Multiply(slot.Modulate, atlasPart.Modulate), slot.offset, flipH: false, flipV: false, slot.useRotate, slot.useScale, slot.useSkew, ShouldRender(atlasPart) && (slot.Visible || slot.useFollowVisible), blink: false);
		return true;
	}

	public static bool TryBuildExternalGpuState(in AdobeAnimateExternalVisualSnapshot visual, in Transform2D localTransform, Rid expectedAtlasArrayRid, out AdobeAnimateGpuManagedVisualState state, out string failureReason)
	{
		state = default(AdobeAnimateGpuManagedVisualState);
		failureReason = "";
		Sprite2D sprite = visual.Sprite;
		if (!GodotObject.IsInstanceValid(sprite))
		{
			failureReason = "external Sprite2D is invalid";
			return false;
		}
		AdobeAnimateSlot slot = visual.Descriptor.Slot;
		bool flag = visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot;
		if (flag && (!GodotObject.IsInstanceValid(slot) || slot.mode != 0 || slot.followSlotId <= 0))
		{
			failureReason = $"external Sprite2D {sprite.Name} has an invalid slot attachment";
			return false;
		}
		Texture2D texture = visual.Texture;
		if (!GodotObject.IsInstanceValid(texture))
		{
			failureReason = $"external Sprite2D {sprite.Name} has no texture";
			return false;
		}
		Rect2 localSourceRect = AdobeAnimateDrawItemBuilder.ResolveSpriteSourceRect(sprite, texture);
		if (localSourceRect.Size.X <= 0f || localSourceRect.Size.Y <= 0f || !AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture, localSourceRect, out var allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || allocation.TextureArrayRid != expectedAtlasArrayRid || allocation.TextureArraySize.X <= 0f || allocation.TextureArraySize.Y <= 0f)
		{
			failureReason = $"external Sprite2D {sprite.Name} texture is not in the owner's shared atlas array";
			return false;
		}
		Rect2 rect = allocation.Rect;
		Vector2 size = rect.Size;
		Color modulate = (flag ? AdobeAnimateDrawItemBuilder.Multiply(slot.Modulate, visual.Modulate) : visual.Modulate);
		state = new AdobeAnimateGpuManagedVisualState(size, AdobeAnimateDrawItemBuilder.NormalizeRect(rect, allocation.TextureArraySize), allocation.AtlasPage, AdobeAnimateDrawItemBuilder.ResolveSpriteDrawOrigin(sprite, size), localTransform, modulate, flag ? slot.offset : Vector2.Zero, sprite.FlipH, sprite.FlipV, !flag || slot.useRotate, !flag || slot.useScale, !flag || slot.useSkew, visual.Visible && (!flag || slot.Visible || slot.useFollowVisible), GetBlinkEnabled(sprite));
		return true;
	}

	public static void RestoreDetachedNode(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			if (node is CanvasItem visual)
			{
				RestoreNative(visual);
			}
			int childCount = node.GetChildCount();
			for (int i = 0; i < childCount; i++)
			{
				RestoreDetachedNode(node.GetChild(i));
			}
		}
	}

	private static void Add(ref ulong hash, int value)
	{
		Add(ref hash, (uint)value);
	}

	private static void Add(ref ulong hash, bool value)
	{
		Add(ref hash, (ulong)(value ? 1 : 0));
	}

	private static void Add(ref ulong hash, float value)
	{
		Add(ref hash, (uint)BitConverter.SingleToInt32Bits(value));
	}

	private static void Add(ref ulong hash, Vector2 value)
	{
		Add(ref hash, value.X);
		Add(ref hash, value.Y);
	}

	private static void Add(ref ulong hash, Rect2 value)
	{
		Add(ref hash, value.Position);
		Add(ref hash, value.Size);
	}

	private static void Add(ref ulong hash, Color value)
	{
		Add(ref hash, value.R);
		Add(ref hash, value.G);
		Add(ref hash, value.B);
		Add(ref hash, value.A);
	}

	private static void Add(ref ulong hash, Transform2D value)
	{
		Add(ref hash, value.X);
		Add(ref hash, value.Y);
		Add(ref hash, value.Origin);
	}

	private static void Add(ref ulong hash, string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			Add(ref hash, 0uL);
			return;
		}
		for (int i = 0; i < value.Length; i++)
		{
			Add(ref hash, value[i]);
		}
	}

	private static void Add(ref ulong hash, ulong value)
	{
		hash ^= value;
		hash *= 1099511628211uL;
	}
}
