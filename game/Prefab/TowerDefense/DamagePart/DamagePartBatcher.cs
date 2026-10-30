using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed partial class DamagePartBatcher : Node2D, IAdobeAnimateInterpolatedElementSink
{
	private sealed class Entry
	{
		public readonly List<Rid> VisualItems = new List<Rid>(8);

		public Rid RootItem;

		public Rid ShadowItem;

		public Transform2D FlightTransform;

		public Vector2 Position;

		public Vector2 Velocity;

		public float LandingWorldY;

		public float JumpSpeed;

		public float SettledSeconds;

		public int JumpCount;

		public Vector2I GridPosition;

		public bool Settled;
	}

	private const float DropGravity = 1960f;

	private const float LandingHoldSeconds = 0.5f;

	private const float LandingFadeSeconds = 0.1f;

	private const int InitialJumpCount = 3;

	private const float InitialJumpSpeed = 300f;

	private const int MinimumDamagePartZIndex = -4095;

	private const string ArrayShaderPath = "res://addons/AdobeAnimateEditor/Rendering/AdobeAnimatePart.gdshader";

	private const string ShadowTexturePath = "res://Asset/Texture/Character/Effect/Shadow.png";

	private static readonly int[] QuadIndices = new int[6] { 0, 1, 2, 0, 2, 3 };

	private static readonly int[] EmptyBones = System.Array.Empty<int>();

	private static readonly float[] EmptyWeights = System.Array.Empty<float>();

	private static DamagePartBatcher _instance;

	private readonly List<Entry> _active = new List<Entry>(32);

	private readonly Stack<Entry> _free = new Stack<Entry>(32);

	private readonly System.Collections.Generic.Dictionary<ulong, ShaderMaterial> _arrayMaterials = new System.Collections.Generic.Dictionary<ulong, ShaderMaterial>();

	private readonly Vector2[] _quadPoints = new Vector2[4];

	private readonly Vector2[] _quadUvs = new Vector2[4];

	private readonly Color[] _quadColors = new Color[4];

	private Shader _arrayShader;

	private Texture2D _shadowTexture;

	private Entry _streamCaptureEntry;

	private Transform2D _streamCaptureTransform;

	private Color _streamCaptureModulate;

	private int _streamCaptureDrawIndex;

	public int ActiveCount => _active.Count;

	internal int RetainedEntryCountForTest => _active.Count + _free.Count;

	internal int VisualRidCountForTest
	{
		get
		{
			int num = 0;
			for (int i = 0; i < _active.Count; i++)
			{
				num += _active[i].VisualItems.Count;
			}
			return num;
		}
	}

	public static DamagePartBatcher GetOrCreate()
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode) || !characterNode.IsInsideTree())
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(_instance) && _instance.IsInsideTree() && !_instance.IsQueuedForDeletion() && _instance.GetParent() == characterNode)
		{
			return _instance;
		}
		if (GodotObject.IsInstanceValid(_instance))
		{
			_instance.QueueFree();
			_instance = null;
		}
		DamagePartBatcher nodeOrNull = characterNode.GetNodeOrNull<DamagePartBatcher>("DamagePartBatcher");
		if (GodotObject.IsInstanceValid(nodeOrNull) && !nodeOrNull.IsQueuedForDeletion())
		{
			_instance = nodeOrNull;
			return nodeOrNull;
		}
		DamagePartBatcher damagePartBatcher = new DamagePartBatcher
		{
			Name = "DamagePartBatcher",
			TopLevel = true
		};
		characterNode.AddChild(damagePartBatcher, forceReadableName: false, InternalMode.Disabled);
		_instance = damagePartBatcher;
		return damagePartBatcher;
	}

	internal bool ActiveLandingOffsetsWithinForTest(float absoluteLimit)
	{
		if (!float.IsFinite(absoluteLimit) || absoluteLimit < 0f)
		{
			return false;
		}
		for (int i = 0; i < _active.Count; i++)
		{
			Entry entry = _active[i];
			if (!float.IsFinite(entry.LandingWorldY) || !float.IsFinite(entry.Position.Y) || Mathf.Abs(entry.LandingWorldY - entry.Position.Y) >= absoluteLimit)
			{
				return false;
			}
		}
		return true;
	}

	internal int GetActiveRenderZIndexForTest(int index)
	{
		if ((uint)index >= (uint)_active.Count)
		{
			return -2147483648;
		}
		return ResolveRenderZIndex(_active[index].GridPosition);
	}

	internal bool IsActiveSettledForTest(int index)
	{
		if ((uint)index < (uint)_active.Count)
		{
			return _active[index].Settled;
		}
		return false;
	}

	internal bool HasArrayTextureBindingForTest(TextureLayered textureArray)
	{
		if (!GodotObject.IsInstanceValid(textureArray) || !_arrayMaterials.TryGetValue(textureArray.GetInstanceId(), out var value) || !GodotObject.IsInstanceValid(value))
		{
			return false;
		}
		GodotObject godotObject = value.GetShaderParameter("atlas_texture_array").AsGodotObject();
		if (GodotObject.IsInstanceValid(godotObject))
		{
			return godotObject.GetInstanceId() == textureArray.GetInstanceId();
		}
		return false;
	}

	internal static int ResolveRenderZIndexForTest(Vector2I gridPosition)
	{
		return ResolveRenderZIndex(gridPosition);
	}

	public override void _Ready()
	{
		base.TopLevel = true;
		base.GlobalTransform = Transform2D.Identity;
		_shadowTexture = ResourceLoader.Load<Texture2D>("res://Asset/Texture/Character/Effect/Shadow.png", null, ResourceLoader.CacheMode.Reuse);
		SetPhysicsProcess(_active.Count > 0);
	}

	public override void _ExitTree()
	{
		for (int num = _active.Count - 1; num >= 0; num--)
		{
			ReleaseEntry(_active[num], retainRoot: false);
		}
		while (_free.Count > 0)
		{
			ReleaseEntry(_free.Pop(), retainRoot: false);
		}
		_active.Clear();
		foreach (ShaderMaterial value in _arrayMaterials.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.Dispose();
			}
		}
		_arrayMaterials.Clear();
		_arrayShader = null;
		_shadowTexture = null;
		if (_instance == this)
		{
			_instance = null;
		}
	}

	public bool TrySpawn(Node2D visual, Transform2D rootWorldTransform, double height, Vector2 velocity, Vector2I gridPosition)
	{
		return TrySpawnNodeSnapshot(visual, GodotObject.IsInstanceValid(visual) ? visual.Transform : Transform2D.Identity, rootWorldTransform, height, velocity, gridPosition, releaseVisual: true);
	}

	public bool TrySpawnNodeSnapshot(Node2D visual, Transform2D visualTransform, Transform2D rootWorldTransform, double height, Vector2 velocity, Vector2I gridPosition, bool releaseVisual)
	{
		if (!GodotObject.IsInstanceValid(visual) || !TryRentEntry(rootWorldTransform, height, velocity, gridPosition, out var entry))
		{
			return false;
		}
		int drawIndex = 0;
		bool captured = CaptureNode(entry, visual, visualTransform, Colors.White, ref drawIndex, includeNodeTransform: false);
		if (!TryActivateEntry(entry, captured))
		{
			return false;
		}
		if (releaseVisual)
		{
			visual.Visible = false;
			visual.QueueFree();
		}
		return true;
	}

	public bool TrySpawnAdobeElements(AdobeAnimateData animeData, Array<Godot.Collections.Array> elements, Array<Texture2D> mediaReplace, Array<string> mediaReplaceAtlasPaths, Vector2 offset, Transform2D visualTransform, Color modulate, Transform2D rootWorldTransform, double height, Vector2 velocity, Vector2I gridPosition)
	{
		if (!TryRentEntry(rootWorldTransform, height, velocity, gridPosition, out var entry))
		{
			return false;
		}
		int drawIndex = 0;
		bool captured = CaptureAdobeElements(entry, animeData, elements, mediaReplace, mediaReplaceAtlasPaths, offset, visualTransform, modulate, ref drawIndex);
		return TryActivateEntry(entry, captured);
	}

	public bool TrySpawnAdobeSlot(AdobeAnimateSlot slot, Array<StringName> filters, Vector2 offset, Transform2D visualTransform, Color modulate, Transform2D rootWorldTransform, double height, Vector2 velocity, Vector2I gridPosition)
	{
		if (!GodotObject.IsInstanceValid(slot) || !TryRentEntry(rootWorldTransform, height, velocity, gridPosition, out var entry))
		{
			return false;
		}
		_streamCaptureEntry = entry;
		_streamCaptureTransform = visualTransform.Translated(-offset);
		_streamCaptureModulate = modulate;
		_streamCaptureDrawIndex = 0;
		bool captured;
		try
		{
			captured = slot.TryAppendPartElementsForRender(filters, this);
		}
		finally
		{
			_streamCaptureEntry = null;
			_streamCaptureTransform = Transform2D.Identity;
			_streamCaptureModulate = Colors.White;
			_streamCaptureDrawIndex = 0;
		}
		return TryActivateEntry(entry, captured);
	}

	bool IAdobeAnimateInterpolatedElementSink.TryAppendInterpolatedElement(AdobeAnimateRuntimeDefinition definition, AdobeAnimateSprite source, int mediaId, Transform2D transform, Color color)
	{
		if (_streamCaptureEntry == null || definition == null || !GodotObject.IsInstanceValid(source))
		{
			return false;
		}
		return CaptureAdobeElement(_streamCaptureEntry, definition, mediaId, _streamCaptureTransform * transform, _streamCaptureModulate * color, source.GetActiveMediaReplacementForRender(mediaId), source.GetActiveMediaReplacementAtlasPathForRender(mediaId), ref _streamCaptureDrawIndex);
	}

	public bool TrySpawnAdobeMedia(AdobeAnimateData animeData, int mediaId, Transform2D mediaTransform, Color color, Array<Texture2D> mediaReplace, Array<string> mediaReplaceAtlasPaths, Transform2D rootWorldTransform, double height, Vector2 velocity, Vector2I gridPosition)
	{
		if (!TryRentEntry(rootWorldTransform, height, velocity, gridPosition, out var entry))
		{
			return false;
		}
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(animeData);
		int drawIndex = 0;
		bool captured = CaptureAdobeElement(entry, orBuild, mediaId, mediaTransform, color, mediaReplace, mediaReplaceAtlasPaths, ref drawIndex);
		return TryActivateEntry(entry, captured);
	}

	public bool TrySpawnExternalAtlas(string texturePath, bool centered, Transform2D visualTransform, Color modulate, Transform2D rootWorldTransform, double height, Vector2 velocity, Vector2I gridPosition)
	{
		if (string.IsNullOrWhiteSpace(texturePath) || !TryRentEntry(rootWorldTransform, height, velocity, gridPosition, out var entry))
		{
			return false;
		}
		int drawIndex = 0;
		bool captured = AddExternalAtlasPrimitive(entry, texturePath, centered, visualTransform, modulate, ref drawIndex);
		return TryActivateEntry(entry, captured);
	}

	private bool TryRentEntry(Transform2D rootWorldTransform, double height, Vector2 velocity, Vector2I gridPosition, out Entry entry)
	{
		entry = null;
		if (!IsInsideTree() || !rootWorldTransform.IsFinite() || !velocity.IsFinite() || !double.IsFinite(height))
		{
			return false;
		}
		entry = ((_free.Count > 0) ? _free.Pop() : new Entry());
		EnsureRetainedItems(entry);
		RenderingServer.CanvasItemSetVisible(entry.RootItem, visible: false);
		RenderingServer.CanvasItemSetVisible(entry.ShadowItem, visible: false);
		ResetVisualItems(entry);
		entry.FlightTransform = rootWorldTransform;
		entry.Position = rootWorldTransform.Origin;
		entry.Velocity = velocity;
		entry.LandingWorldY = rootWorldTransform.Origin.Y + (float)height;
		entry.JumpSpeed = 300f;
		entry.JumpCount = 3;
		entry.SettledSeconds = 0f;
		entry.GridPosition = ResolveInitialGridPosition(gridPosition, new Vector2(entry.Position.X, entry.LandingWorldY));
		entry.Settled = false;
		return true;
	}

	private bool TryActivateEntry(Entry entry, bool captured)
	{
		if (!captured || entry.VisualItems.Count == 0)
		{
			ReleaseEntry(entry, retainRoot: true);
			return false;
		}
		ConfigureRenderOrder(entry);
		RenderingServer.CanvasItemSetModulate(entry.RootItem, Colors.White);
		UpdateRetainedTransforms(entry);
		RenderingServer.CanvasItemSetVisible(entry.RootItem, visible: true);
		RenderingServer.CanvasItemSetVisible(entry.ShadowItem, GodotObject.IsInstanceValid(_shadowTexture));
		_active.Add(entry);
		SetPhysicsProcess(enable: true);
		return true;
	}

	public override void _PhysicsProcess(double delta)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		float num = (float)delta;
		for (int num2 = _active.Count - 1; num2 >= 0; num2--)
		{
			Entry entry = _active[num2];
			if (entry.Settled)
			{
				entry.SettledSeconds += num;
				if (entry.SettledSeconds >= 0.6f)
				{
					RemoveActiveAt(num2);
				}
				else if (entry.SettledSeconds > 0.5f)
				{
					float value = 1f - (entry.SettledSeconds - 0.5f) / 0.1f;
					RenderingServer.CanvasItemSetModulate(entry.RootItem, new Color(1f, 1f, 1f, Mathf.Clamp(value, 0f, 1f)));
				}
				continue;
			}
			entry.Velocity.Y += 1960f * num;
			entry.Position += entry.Velocity * num;
			if (entry.Position.Y > entry.LandingWorldY)
			{
				Vector2 shadowWorldPosition = new Vector2(entry.Position.X, entry.LandingWorldY);
				entry.GridPosition = ResolveGridPosition(shadowWorldPosition);
				ConfigureRenderOrder(entry);
				if (IsWaterCell(entry.GridPosition))
				{
					CreateSplash(entry.GridPosition, shadowWorldPosition);
					RemoveActiveAt(num2);
					continue;
				}
				entry.Position.Y = entry.LandingWorldY - 1f;
				entry.JumpCount--;
				if (entry.JumpCount > 0 && entry.JumpSpeed >= 10f)
				{
					entry.Velocity = new Vector2(entry.Velocity.X * 0.5f, 0f - entry.JumpSpeed);
					entry.JumpSpeed *= 0.5f;
				}
				else
				{
					entry.Velocity = Vector2.Zero;
					entry.Settled = true;
					entry.SettledSeconds = 0f;
				}
			}
			else if (entry.Velocity.X != 0f)
			{
				RotateBasis(ref entry.FlightTransform, entry.Velocity.X / 40f * num);
			}
			entry.FlightTransform.Origin = entry.Position;
			UpdateRetainedTransforms(entry);
		}
		if (_active.Count == 0)
		{
			SetPhysicsProcess(enable: false);
		}
		TowerDefensePerfProfiler.End("damagePart.batch.physics", startTicks, _active.Count);
	}

	private void EnsureRetainedItems(Entry entry)
	{
		Rid canvasItem = GetCanvasItem();
		if (!entry.RootItem.IsValid)
		{
			entry.RootItem = RenderingServer.CanvasItemCreate();
			RenderingServer.CanvasItemSetVisible(entry.RootItem, visible: false);
			RenderingServer.CanvasItemSetParent(entry.RootItem, canvasItem);
			RenderingServer.CanvasItemSetZAsRelativeToParent(entry.RootItem, enabled: false);
		}
		if (!entry.ShadowItem.IsValid)
		{
			entry.ShadowItem = RenderingServer.CanvasItemCreate();
			RenderingServer.CanvasItemSetVisible(entry.ShadowItem, visible: false);
			RenderingServer.CanvasItemSetParent(entry.ShadowItem, canvasItem);
			RenderingServer.CanvasItemSetZAsRelativeToParent(entry.ShadowItem, enabled: false);
			if (GodotObject.IsInstanceValid(_shadowTexture))
			{
				Vector2 vector = _shadowTexture.GetSize() * 0.75f;
				RenderingServer.CanvasItemAddTextureRect(entry.ShadowItem, new Rect2(-vector * 0.5f, vector), _shadowTexture.GetRid());
			}
		}
	}

	private bool CaptureNode(Entry entry, Node node, Transform2D parentTransform, Color parentModulate, ref int drawIndex, bool includeNodeTransform = true)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		Transform2D transform2D = parentTransform;
		Color color = parentModulate;
		bool flag = true;
		if (node is CanvasItem canvasItem)
		{
			flag = canvasItem.Visible;
			color *= canvasItem.Modulate * canvasItem.SelfModulate;
		}
		if (!flag)
		{
			return false;
		}
		if (includeNodeTransform && node is Node2D node2D)
		{
			transform2D *= node2D.Transform;
		}
		bool flag2 = ((node is AdobeAnimatePart part) ? CaptureAdobePart(entry, part, transform2D, color, ref drawIndex) : ((node is AdobeAnimateSprite sprite) ? CaptureAdobeSprite(entry, sprite, transform2D, color, ref drawIndex) : (node is Sprite2D sprite2 && CaptureSprite(entry, sprite2, transform2D, color, ref drawIndex))));
		bool flag3 = flag2;
		int childCount = node.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			flag3 |= CaptureNode(entry, node.GetChild(i), transform2D, color, ref drawIndex);
		}
		return flag3;
	}

	private bool CaptureAdobeSprite(Entry entry, AdobeAnimateSprite sprite, Transform2D spriteTransform, Color spriteModulate, ref int drawIndex)
	{
		if (!GodotObject.IsInstanceValid(sprite) || !sprite.TryCreateVisibleFrameElementsForRender(out var animeData, out var mediaReplace, out var mediaReplaceAtlasPaths, out var elements))
		{
			return false;
		}
		return CaptureAdobeElements(entry, animeData, elements, mediaReplace, mediaReplaceAtlasPaths, Vector2.Zero, spriteTransform, spriteModulate, ref drawIndex);
	}

	private bool CaptureAdobePart(Entry entry, AdobeAnimatePart part, Transform2D partTransform, Color partModulate, ref int drawIndex)
	{
		if (!string.IsNullOrWhiteSpace(part.externalAtlasTexturePath))
		{
			return AddExternalAtlasPrimitive(entry, part.externalAtlasTexturePath, part.externalAtlasCentered, partTransform, partModulate, ref drawIndex);
		}
		return CaptureAdobeElements(entry, part.flashAnimeData, part.elementList, part.mediaReplace, part.mediaReplaceAtlasPaths, part.offset, partTransform, partModulate, ref drawIndex);
	}

	private bool CaptureAdobeElements(Entry entry, AdobeAnimateData animeData, Array<Godot.Collections.Array> elements, Array<Texture2D> mediaReplace, Array<string> mediaReplaceAtlasPaths, Vector2 offset, Transform2D partTransform, Color partModulate, ref int drawIndex)
	{
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(animeData);
		if (orBuild == null || orBuild.MediaRects == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < (elements?.Count ?? 0); i++)
		{
			Godot.Collections.Array array = elements[i];
			if (array != null && array.Count >= 3)
			{
				int mediaId = (int)array[0];
				Transform2D transform2D = ((Transform2D)array[1]).Translated(-offset);
				flag |= CaptureAdobeElement(entry, orBuild, mediaId, partTransform * transform2D, partModulate * (Color)array[2], mediaReplace, mediaReplaceAtlasPaths, ref drawIndex);
			}
		}
		return flag;
	}

	private bool CaptureAdobeElement(Entry entry, AdobeAnimateRuntimeDefinition definition, int mediaId, Transform2D drawTransform, Color color, Array<Texture2D> mediaReplace, Array<string> mediaReplaceAtlasPaths, ref int drawIndex)
	{
		if (definition == null || definition.MediaRects == null || mediaId == 65535 || (uint)mediaId >= (uint)definition.MediaRects.Length)
		{
			return false;
		}
		_ = ref definition.MediaRects[mediaId];
		Texture2D replacement = ((mediaId < (mediaReplace?.Count ?? 0)) ? mediaReplace[mediaId] : null);
		string replacementAtlasPath = ((mediaId < (mediaReplaceAtlasPaths?.Count ?? 0)) ? mediaReplaceAtlasPaths[mediaId] : string.Empty);
		return CaptureAdobeElement(entry, definition, mediaId, drawTransform, color, replacement, replacementAtlasPath, ref drawIndex);
	}

	private bool CaptureAdobeElement(Entry entry, AdobeAnimateRuntimeDefinition definition, int mediaId, Transform2D drawTransform, Color color, Texture2D replacement, string replacementAtlasPath, ref int drawIndex)
	{
		if (definition == null || definition.MediaRects == null || mediaId == 65535 || (uint)mediaId >= (uint)definition.MediaRects.Length)
		{
			return false;
		}
		Rect2 rect = definition.MediaRects[mediaId];
		if (GodotObject.IsInstanceValid(replacement))
		{
			Vector2 size = replacement.GetSize();
			if (size.X <= 0f || size.Y <= 0f)
			{
				size = rect.Size;
			}
			return AddTexturePrimitive(entry, drawTransform, new Rect2(Vector2.Zero, size), new Rect2(Vector2.Zero, size), replacement.GetRid(), color, ref drawIndex);
		}
		if (!string.IsNullOrWhiteSpace(replacementAtlasPath))
		{
			return AddExternalAtlasPrimitive(entry, replacementAtlasPath, centered: false, drawTransform, color, ref drawIndex);
		}
		if (definition.UsesAtlasTextureArrayLayout && GodotObject.IsInstanceValid(definition.AtlasTextureArray) && definition.AtlasTextureArrayRid.IsValid)
		{
			int layer = definition.BaseAtlasPage;
			if (definition.MediaAtlasPages != null && (uint)mediaId < (uint)definition.MediaAtlasPages.Length)
			{
				layer = definition.MediaAtlasPages[mediaId];
			}
			return AddArrayTexturePrimitive(entry, drawTransform, rect, definition.AtlasTextureArray, definition.AtlasTextureArraySize, layer, color, centered: false, ref drawIndex);
		}
		Texture2D texture2D = ResolveAtlasTexture(definition, mediaId);
		if (GodotObject.IsInstanceValid(texture2D))
		{
			return AddTexturePrimitive(entry, drawTransform, new Rect2(Vector2.Zero, rect.Size), rect, texture2D.GetRid(), color, ref drawIndex);
		}
		return false;
	}

	private bool CaptureSprite(Entry entry, Sprite2D sprite, Transform2D transform, Color modulate, ref int drawIndex)
	{
		Texture2D texture = sprite.Texture;
		if (!GodotObject.IsInstanceValid(texture))
		{
			return false;
		}
		Rect2 source = (sprite.RegionEnabled ? sprite.RegionRect : new Rect2(Vector2.Zero, texture.GetSize()));
		Vector2 size = source.Size;
		Vector2 position = (sprite.Centered ? (-size * 0.5f) : Vector2.Zero);
		position += sprite.Offset;
		if (sprite.FlipH)
		{
			position.X += size.X;
			size.X = 0f - size.X;
		}
		if (sprite.FlipV)
		{
			position.Y += size.Y;
			size.Y = 0f - size.Y;
		}
		return AddTexturePrimitive(entry, transform, new Rect2(position, size), source, texture.GetRid(), modulate, ref drawIndex);
	}

	private bool AddTexturePrimitive(Entry entry, Transform2D transform, Rect2 destination, Rect2 source, Rid texture, Color color, ref int drawIndex)
	{
		if (!texture.IsValid || !transform.IsFinite())
		{
			return false;
		}
		RenderingServer.CanvasItemAddTextureRectRegion(CreateVisualItem(entry, transform, ref drawIndex), destination, texture, source, color);
		return true;
	}

	private bool AddArrayTexturePrimitive(Entry entry, Transform2D transform, Rect2 atlasRect, TextureLayered textureArray, Vector2 atlasSize, int layer, Color color, bool centered, ref int drawIndex)
	{
		if (!GodotObject.IsInstanceValid(textureArray) || atlasSize.X <= 0f || atlasSize.Y <= 0f || atlasRect.Size.X <= 0f || atlasRect.Size.Y <= 0f)
		{
			return false;
		}
		Rid item = CreateVisualItem(entry, transform, ref drawIndex);
		ShaderMaterial arrayMaterial = GetArrayMaterial(textureArray);
		if (!GodotObject.IsInstanceValid(arrayMaterial))
		{
			return false;
		}
		RenderingServer.CanvasItemSetMaterial(item, arrayMaterial.GetRid());
		Vector2 size = atlasRect.Size;
		Vector2 vector = (centered ? (-size * 0.5f) : Vector2.Zero);
		_quadPoints[0] = vector;
		_quadPoints[1] = vector + new Vector2(0f, size.Y);
		_quadPoints[2] = vector + size;
		_quadPoints[3] = vector + new Vector2(size.X, 0f);
		float x = atlasRect.Position.X / atlasSize.X;
		float num = atlasRect.Position.Y / atlasSize.Y;
		float x2 = (atlasRect.Position.X + size.X) / atlasSize.X;
		float num2 = Mathf.Min((atlasRect.Position.Y + size.Y) / atlasSize.Y, 0.999999f);
		_quadUvs[0] = new Vector2(x, (float)layer + num);
		_quadUvs[1] = new Vector2(x, (float)layer + num2);
		_quadUvs[2] = new Vector2(x2, (float)layer + num2);
		_quadUvs[3] = new Vector2(x2, (float)layer + num);
		_quadColors[0] = color;
		_quadColors[1] = color;
		_quadColors[2] = color;
		_quadColors[3] = color;
		RenderingServer.CanvasItemAddTriangleArray(item, QuadIndices, _quadPoints, _quadColors, _quadUvs, EmptyBones, EmptyWeights, default(Rid), 2);
		return true;
	}

	private bool AddExternalAtlasPrimitive(Entry entry, string texturePath, bool centered, Transform2D transform, Color color, ref int drawIndex)
	{
		if (string.IsNullOrWhiteSpace(texturePath) || !AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(texturePath, out var allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || !GodotObject.IsInstanceValid(allocation.TextureArray))
		{
			return false;
		}
		return AddArrayTexturePrimitive(entry, transform, allocation.Rect, allocation.TextureArray, allocation.TextureArraySize, allocation.AtlasPage, color, centered, ref drawIndex);
	}

	private Rid CreateVisualItem(Entry entry, Transform2D transform, ref int drawIndex)
	{
		int num = drawIndex;
		Rid rid;
		if ((uint)num < (uint)entry.VisualItems.Count)
		{
			rid = entry.VisualItems[num];
		}
		else
		{
			rid = RenderingServer.CanvasItemCreate();
			RenderingServer.CanvasItemSetParent(rid, entry.RootItem);
			entry.VisualItems.Add(rid);
		}
		RenderingServer.CanvasItemClear(rid);
		RenderingServer.CanvasItemSetMaterial(rid, default(Rid));
		RenderingServer.CanvasItemSetTransform(rid, transform);
		RenderingServer.CanvasItemSetDrawIndex(rid, drawIndex++);
		return rid;
	}

	private ShaderMaterial GetArrayMaterial(TextureLayered textureArray)
	{
		ulong instanceId = textureArray.GetInstanceId();
		if (_arrayMaterials.TryGetValue(instanceId, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		if (_arrayShader == null)
		{
			_arrayShader = ResourceLoader.Load<Shader>("res://addons/AdobeAnimateEditor/Rendering/AdobeAnimatePart.gdshader", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(_arrayShader))
		{
			return null;
		}
		value = new ShaderMaterial
		{
			Shader = _arrayShader
		};
		value.SetShaderParameter("atlas_texture_array", textureArray);
		_arrayMaterials[instanceId] = value;
		return value;
	}

	private static Texture2D ResolveAtlasTexture(AdobeAnimateRuntimeDefinition definition, int mediaId)
	{
		int num = definition.BaseAtlasPage;
		if (definition.MediaAtlasPages != null && (uint)mediaId < (uint)definition.MediaAtlasPages.Length)
		{
			num = definition.MediaAtlasPages[mediaId];
		}
		if (definition.AtlasPages != null && (uint)num < (uint)definition.AtlasPages.Length)
		{
			return definition.AtlasPages[num];
		}
		return definition.BaseAtlas;
	}

	private void ConfigureRenderOrder(Entry entry)
	{
		int num = ResolveRenderZIndex(entry.GridPosition);
		RenderingServer.CanvasItemSetZIndex(entry.RootItem, num);
		RenderingServer.CanvasItemSetZIndex(entry.ShadowItem, Math.Clamp(num - 10, -4096, 4096));
	}

	private static int ResolveRenderZIndex(Vector2I gridPosition)
	{
		int num = 15;
		return (int)Math.Clamp((long)gridPosition.Y * (long)num + 8, -4095L, 4096L);
	}

	private static void UpdateRetainedTransforms(Entry entry)
	{
		RenderingServer.CanvasItemSetTransform(entry.RootItem, entry.FlightTransform);
		RenderingServer.CanvasItemSetTransform(entry.ShadowItem, new Transform2D(0f, new Vector2(entry.Position.X, entry.LandingWorldY)));
	}

	private static void RotateBasis(ref Transform2D transform, float radians)
	{
		float num = Mathf.Sin(radians);
		float num2 = Mathf.Cos(radians);
		Vector2 x = transform.X;
		Vector2 y = transform.Y;
		transform.X = new Vector2(x.X * num2 - x.Y * num, x.X * num + x.Y * num2);
		transform.Y = new Vector2(y.X * num2 - y.Y * num, y.X * num + y.Y * num2);
	}

	private static Vector2I ResolveGridPosition(Vector2 shadowWorldPosition)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return Vector2I.Zero;
		}
		return instance.GetMapGridPos(shadowWorldPosition - new Vector2(0f, 20f));
	}

	private static Vector2I ResolveInitialGridPosition(Vector2I gridPosition, Vector2 landingWorldPosition)
	{
		if (gridPosition.X < 0 || gridPosition.Y < 0)
		{
			return ResolveGridPosition(landingWorldPosition);
		}
		return gridPosition;
	}

	private static bool IsWaterCell(Vector2I gridPosition)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPosition);
		if (GodotObject.IsInstanceValid(mapCell))
		{
			return mapCell.isWater;
		}
		return false;
	}

	private static void CreateSplash(Vector2I gridPosition, Vector2 shadowWorldPosition)
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(characterNode))
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.PARTICLES_SPLASH, characterNode) as TowerDefenseEffectSpriteOnce;
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				towerDefenseEffectSpriteOnce.gridPos = gridPosition;
				towerDefenseEffectSpriteOnce.GlobalPosition = shadowWorldPosition - new Vector2(0f, 20f);
			}
		}
	}

	private void RemoveActiveAt(int index)
	{
		Entry entry = _active[index];
		int index2 = _active.Count - 1;
		_active[index] = _active[index2];
		_active.RemoveAt(index2);
		ReleaseEntry(entry, retainRoot: true);
	}

	private void ReleaseEntry(Entry entry, bool retainRoot)
	{
		if (entry.RootItem.IsValid)
		{
			RenderingServer.CanvasItemSetVisible(entry.RootItem, visible: false);
		}
		if (entry.ShadowItem.IsValid)
		{
			RenderingServer.CanvasItemSetVisible(entry.ShadowItem, visible: false);
		}
		if (retainRoot)
		{
			ResetVisualItems(entry);
			_free.Push(entry);
			return;
		}
		FreeVisualItems(entry);
		if (entry.RootItem.IsValid)
		{
			RenderingServer.FreeRid(entry.RootItem);
		}
		if (entry.ShadowItem.IsValid)
		{
			RenderingServer.FreeRid(entry.ShadowItem);
		}
		entry.RootItem = default(Rid);
		entry.ShadowItem = default(Rid);
	}

	private static void ResetVisualItems(Entry entry)
	{
		for (int i = 0; i < entry.VisualItems.Count; i++)
		{
			Rid item = entry.VisualItems[i];
			if (item.IsValid)
			{
				RenderingServer.CanvasItemClear(item);
				RenderingServer.CanvasItemSetMaterial(item, default(Rid));
			}
		}
	}

	private static void FreeVisualItems(Entry entry)
	{
		for (int i = 0; i < entry.VisualItems.Count; i++)
		{
			Rid rid = entry.VisualItems[i];
			if (rid.IsValid)
			{
				RenderingServer.FreeRid(rid);
			}
		}
		entry.VisualItems.Clear();
	}
}
