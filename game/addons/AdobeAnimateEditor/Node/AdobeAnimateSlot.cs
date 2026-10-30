using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class AdobeAnimateSlot : Node2D
{
	public const int EmptyMediaId = 65535;

	public const int TopDrawLayerId = -2;

	private const int ChildOrderChangedNotification = 24;

	private List<CanvasItem> _runtimeDependencyVisibilityWatchers;

	private bool _runtimeInsideTree;

	private bool _runtimeFollowVisibilityKnown;

	private bool _runtimeFollowVisibilityValue;

	private HashSet<int> _partSnapshotLayerIds;

	private int _mode;

	private int _followSlotId;

	private int _drawLayerId = -1;

	private bool _updateAllFrame;

	private Transform2D _runtimeBaseTransform = Transform2D.Identity;

	private Vector2 _runtimeVisualOffset = Vector2.Zero;

	public AdobeAnimateSprite sprite { get; set; }

	[Export(PropertyHint.Enum, "Follow,Drive")]
	public int mode
	{
		get
		{
			return _mode;
		}
		set
		{
			if (_mode != value)
			{
				_mode = value;
				MarkManagedSpriteChildrenDirty();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int followSlotId
	{
		get
		{
			return _followSlotId;
		}
		set
		{
			if (_followSlotId != value)
			{
				_followSlotId = value;
				MarkManagedSpriteChildrenDirty();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int drawLayerId
	{
		get
		{
			return _drawLayerId;
		}
		set
		{
			if (_drawLayerId != value)
			{
				_drawLayerId = value;
				MarkManagedSpriteChildrenDirty();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Vector2 offset { get; set; } = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public bool useRotate { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool useScale { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool useSkew { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool useFollowVisible { get; set; }

	[Export(PropertyHint.None, "")]
	public bool updateAllFrame
	{
		get
		{
			return _updateAllFrame;
		}
		set
		{
			if (_updateAllFrame != value)
			{
				_updateAllFrame = value;
				RefreshRuntimeUpdateRequirement();
			}
		}
	}

	internal bool RequiresRuntimeUpdate { get; private set; }

	internal bool RuntimeUpdateVisualOnly { get; private set; }

	internal bool IsRuntimeInsideTree => _runtimeInsideTree;

	public Vector2 CachedPosition { get; private set; } = Vector2.Zero;

	public int RuntimePositionVersion { get; private set; }

	public override void _EnterTree()
	{
		_runtimeInsideTree = true;
	}

	public override void _Ready()
	{
		ResetRuntimeFollowVisibilityCache();
		if (GetParent() is AdobeAnimateSprite adobeAnimateSprite)
		{
			sprite = adobeAnimateSprite;
		}
		if (drawLayerId == -1)
		{
			drawLayerId = followSlotId;
		}
		_runtimeBaseTransform = base.Transform;
		CachedPosition = base.Position;
		RefreshRuntimeUpdateRequirement(refreshVisibilityWatchers: true);
	}

	public override void _ExitTree()
	{
		if (_runtimeVisualOffset != Vector2.Zero)
		{
			SetRuntimeVisualOffset(Vector2.Zero);
		}
		_runtimeInsideTree = false;
		ResetRuntimeFollowVisibilityCache();
		DisconnectRuntimeDependencyVisibilityWatchers();
	}

	public override void _Notification(int what)
	{
		if (what == 24)
		{
			RefreshRuntimeUpdateRequirement(refreshVisibilityWatchers: true);
			MarkManagedSpriteChildrenDirty();
		}
		else if ((long)what == 31)
		{
			OnRuntimeDependencyVisibilityChanged();
		}
	}

	internal void RefreshRuntimeUpdateRequirement(bool refreshVisibilityWatchers = false)
	{
		if (refreshVisibilityWatchers)
		{
			RefreshRuntimeDependencyVisibilityWatchers();
		}
		AnalyzeRuntimeDependentChildren(out var requiresRuntimeUpdate, out var visualOnly);
		if (RequiresRuntimeUpdate != requiresRuntimeUpdate || RuntimeUpdateVisualOnly != visualOnly)
		{
			RequiresRuntimeUpdate = requiresRuntimeUpdate;
			RuntimeUpdateVisualOnly = visualOnly;
			if (sprite != null && GodotObject.IsInstanceValid(sprite))
			{
				sprite.MarkSlotRuntimeUpdateCacheDirty();
			}
			if (GetParent() is AdobeAnimateSlot adobeAnimateSlot && GodotObject.IsInstanceValid(adobeAnimateSlot))
			{
				adobeAnimateSlot.RefreshRuntimeUpdateRequirement();
			}
		}
	}

	internal void ApplyRuntimeFollowVisibility(bool visible)
	{
		if (!_runtimeFollowVisibilityKnown || _runtimeFollowVisibilityValue != visible)
		{
			_runtimeFollowVisibilityKnown = true;
			_runtimeFollowVisibilityValue = visible;
			if (base.Visible != visible)
			{
				base.Visible = visible;
			}
		}
	}

	private void ResetRuntimeFollowVisibilityCache()
	{
		_runtimeFollowVisibilityKnown = false;
	}

	private void AnalyzeRuntimeDependentChildren(out bool requiresRuntimeUpdate, out bool visualOnly)
	{
		requiresRuntimeUpdate = false;
		visualOnly = true;
		int childCount = GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = GetChild(i);
			if (ChildRequiresRuntimeUpdate(child, out var visualOnly2))
			{
				requiresRuntimeUpdate = true;
				if (!visualOnly2)
				{
					visualOnly = false;
				}
			}
		}
		visualOnly = requiresRuntimeUpdate & visualOnly;
	}

	private bool ChildRequiresRuntimeUpdate(Node child, out bool visualOnly)
	{
		visualOnly = true;
		if (child == null || !GodotObject.IsInstanceValid(child))
		{
			return false;
		}
		if (child is Sprite2D)
		{
			return false;
		}
		if (child is AdobeAnimatePart adobeAnimatePart && !string.IsNullOrWhiteSpace(adobeAnimatePart.externalAtlasTexturePath))
		{
			return false;
		}
		if (child is AdobeAnimateSprite adobeAnimateSprite)
		{
			return adobeAnimateSprite.Visible;
		}
		if (child is AdobeAnimateSlot adobeAnimateSlot)
		{
			adobeAnimateSlot.RefreshRuntimeUpdateRequirement();
			if (!adobeAnimateSlot.Visible && !adobeAnimateSlot.useFollowVisible)
			{
				return false;
			}
			visualOnly = adobeAnimateSlot.RuntimeUpdateVisualOnly;
			return adobeAnimateSlot.RequiresRuntimeUpdate;
		}
		visualOnly = false;
		return true;
	}

	private void RefreshRuntimeDependencyVisibilityWatchers()
	{
		DisconnectRuntimeDependencyVisibilityWatchers();
		CollectRuntimeDependencyVisibilityWatchers(this);
	}

	private void CollectRuntimeDependencyVisibilityWatchers(Node parent)
	{
		int childCount = parent.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = parent.GetChild(i);
			if (child is CanvasItem canvasItem && !(child is Sprite2D))
			{
				ConnectRuntimeDependencyVisibilityWatcher(canvasItem);
			}
			if (!(child is Sprite2D) && child.GetChildCount() > 0)
			{
				CollectRuntimeDependencyVisibilityWatchers(child);
			}
		}
	}

	private void ConnectRuntimeDependencyVisibilityWatcher(CanvasItem canvasItem)
	{
		if (canvasItem == null || !GodotObject.IsInstanceValid(canvasItem))
		{
			return;
		}
		List<CanvasItem> runtimeDependencyVisibilityWatchers = _runtimeDependencyVisibilityWatchers;
		if (runtimeDependencyVisibilityWatchers == null || !runtimeDependencyVisibilityWatchers.Contains(canvasItem))
		{
			canvasItem.VisibilityChanged += OnRuntimeDependencyVisibilityChanged;
			if (_runtimeDependencyVisibilityWatchers == null)
			{
				_runtimeDependencyVisibilityWatchers = new List<CanvasItem>();
			}
			_runtimeDependencyVisibilityWatchers.Add(canvasItem);
		}
	}

	private void DisconnectRuntimeDependencyVisibilityWatchers()
	{
		if (_runtimeDependencyVisibilityWatchers == null)
		{
			return;
		}
		for (int i = 0; i < _runtimeDependencyVisibilityWatchers.Count; i++)
		{
			CanvasItem canvasItem = _runtimeDependencyVisibilityWatchers[i];
			if (GodotObject.IsInstanceValid(canvasItem))
			{
				canvasItem.VisibilityChanged -= OnRuntimeDependencyVisibilityChanged;
			}
		}
		_runtimeDependencyVisibilityWatchers.Clear();
	}

	private void OnRuntimeDependencyVisibilityChanged()
	{
		if (_runtimeFollowVisibilityKnown && base.Visible != _runtimeFollowVisibilityValue)
		{
			base.Visible = _runtimeFollowVisibilityValue;
			return;
		}
		RefreshRuntimeUpdateRequirement();
		if (sprite != null && GodotObject.IsInstanceValid(sprite))
		{
			sprite.MarkManagedSlotVisualStateChanged();
		}
	}

	internal void MarkManagedSpriteChildrenDirty()
	{
		if (sprite != null && GodotObject.IsInstanceValid(sprite))
		{
			sprite.MarkManagedSlotSpriteCacheDirty();
		}
	}

	internal bool TryCollectManagedSprite2DChildren(List<AdobeAnimateManagedSlotSprite> output)
	{
		bool result = false;
		int childCount = GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = GetChild(i);
			if (child is Sprite2D sprite2D && GodotObject.IsInstanceValid(sprite2D))
			{
				AdobeAnimateManagedSprite2D.MarkManaged(sprite2D);
				output.Add(new AdobeAnimateManagedSlotSprite(this, sprite2D));
				result = true;
			}
			else if (child is AdobeAnimatePart adobeAnimatePart && GodotObject.IsInstanceValid(adobeAnimatePart) && !string.IsNullOrWhiteSpace(adobeAnimatePart.externalAtlasTexturePath))
			{
				AdobeAnimateManagedSprite2D.MarkManaged(adobeAnimatePart);
				output.Add(new AdobeAnimateManagedSlotSprite(this, adobeAnimatePart));
				result = true;
			}
		}
		return result;
	}

	internal int ResolveDrawLayerId()
	{
		if (drawLayerId == -2)
		{
			int num = (GodotObject.IsInstanceValid(sprite) ? sprite.GetLayerVisibleCountForRender() : 0);
			if (num > 0)
			{
				return num + 1;
			}
			return Mathf.Max(1, followSlotId);
		}
		if (drawLayerId < 0)
		{
			return followSlotId;
		}
		return drawLayerId;
	}

	public void ApplyRuntimeTransform(Transform2D transform)
	{
		_runtimeBaseTransform = transform;
		transform.Origin += _runtimeVisualOffset;
		CommitRuntimeTransform(transform);
	}

	public void AddRuntimeVisualOffset(Vector2 offset)
	{
		SetRuntimeVisualOffset(_runtimeVisualOffset + offset);
	}

	public void SetRuntimeVisualOffset(Vector2 offset)
	{
		if (!(_runtimeVisualOffset == offset))
		{
			_runtimeBaseTransform = base.Transform;
			_runtimeBaseTransform.Origin -= _runtimeVisualOffset;
			_runtimeVisualOffset = offset;
			Transform2D runtimeBaseTransform = _runtimeBaseTransform;
			runtimeBaseTransform.Origin += _runtimeVisualOffset;
			CommitRuntimeTransform(runtimeBaseTransform);
		}
	}

	private void CommitRuntimeTransform(Transform2D transform)
	{
		if (!(base.Transform == transform))
		{
			base.Transform = transform;
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.NotifySlotOwnedSpriteTransformChangedForRender(this);
			}
			Vector2 origin = transform.Origin;
			if (!(origin == CachedPosition))
			{
				CachedPosition = origin;
				RuntimePositionVersion++;
			}
		}
	}

	public void Update()
	{
		if (mode != 0 || sprite == null || !sprite.TryGetInterpolatedSlotPose(followSlotId - 1, out var mediaId, out var transform) || mediaId == 65535)
		{
			return;
		}
		transform = transform.Translated(sprite.offset);
		if (!(transform != Transform2D.Identity))
		{
			return;
		}
		Transform2D transform2 = transform.TranslatedLocal(offset);
		if (!useRotate)
		{
			transform2 = transform2.RotatedLocal(0f - transform2.Rotation);
		}
		if (!useScale)
		{
			Vector2 scale = transform2.Scale;
			if (scale.X != 0f && scale.Y != 0f)
			{
				transform2 = transform2.ScaledLocal(Vector2.One / scale);
			}
		}
		ApplyRuntimeTransform(transform2);
		if (!useSkew)
		{
			base.Skew = 0f;
		}
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		Array<Dictionary> array = new Array<Dictionary>();
		if (sprite == null || sprite.flashAnimeData == null)
		{
			return array;
		}
		if (sprite.flashAnimeData.layerDictionary.Count > 0)
		{
			array.Add(new Dictionary
			{
				{ "name", "Layer" },
				{ "type", 2 },
				{ "hint", 2 },
				{
					"hint_string",
					sprite.flashAnimeData.GetLayerHintString(includeNoone: true)
				},
				{ "usage", num }
			});
			array.Add(new Dictionary
			{
				{ "name", "Draw Layer" },
				{ "type", 2 },
				{ "hint", 2 },
				{
					"hint_string",
					$"{sprite.flashAnimeData.GetLayerHintString(includeNoone: true)},Top:{-2}"
				},
				{ "usage", num }
			});
		}
		return array;
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property == (StringName)"Layer")
		{
			followSlotId = value.AsInt32();
			if (drawLayerId == -1)
			{
				drawLayerId = followSlotId;
			}
			return true;
		}
		if (property == (StringName)"Draw Layer")
		{
			drawLayerId = value.AsInt32();
			MarkManagedSpriteChildrenDirty();
			return true;
		}
		return false;
	}

	public override Variant _Get(StringName property)
	{
		if (property == (StringName)"Layer")
		{
			return followSlotId;
		}
		if (property == (StringName)"Draw Layer")
		{
			return (drawLayerId == -2) ? (-2) : ResolveDrawLayerId();
		}
		return default(Variant);
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		if (property == (StringName)"Layer")
		{
			return true;
		}
		if (property == (StringName)"Draw Layer")
		{
			return true;
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if (property == (StringName)"Layer")
		{
			return 0;
		}
		if (property == (StringName)"Draw Layer")
		{
			return followSlotId;
		}
		return default(Variant);
	}

	public AdobeAnimatePart CreatePart(Array<StringName> layerList)
	{
		if (!TryCreatePartSnapshot(layerList, out var animeData, out var mediaReplace, out var mediaReplaceAtlasPaths, out var elementOutputList))
		{
			return null;
		}
		AdobeAnimatePart adobeAnimatePart = new AdobeAnimatePart
		{
			flashAnimeData = animeData,
			mediaReplace = mediaReplace,
			mediaReplaceAtlasPaths = mediaReplaceAtlasPaths,
			offset = Vector2.Zero,
			elementList = elementOutputList
		};
		sprite.OnPartSnapshotCreated(this, layerList, adobeAnimatePart);
		return adobeAnimatePart;
	}

	public bool TryCreatePartSnapshot(Array<StringName> layerList, out AdobeAnimateData animeData, out Array<Texture2D> mediaReplace, out Array<string> mediaReplaceAtlasPaths, out Array<Array> elementOutputList)
	{
		animeData = null;
		mediaReplace = null;
		mediaReplaceAtlasPaths = null;
		elementOutputList = null;
		if (sprite == null || !GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(sprite.flashAnimeData))
		{
			return false;
		}
		Update();
		(_partSnapshotLayerIds ?? (_partSnapshotLayerIds = new HashSet<int>())).Clear();
		if (layerList != null)
		{
			foreach (StringName layer in layerList)
			{
				if (sprite.flashAnimeData.layerDictionary.TryGetValue(layer, out var value))
				{
					_partSnapshotLayerIds.Add((int)value);
				}
			}
		}
		animeData = sprite.flashAnimeData;
		mediaReplace = sprite.CreateActiveMediaReplaceSnapshotForRender();
		mediaReplaceAtlasPaths = sprite.CreateActiveMediaReplaceAtlasPathSnapshotForRender();
		elementOutputList = new Array<Array>();
		sprite.AppendInterpolatedLayerElementsForRender(_partSnapshotLayerIds, base.Transform.Origin, elementOutputList);
		return elementOutputList.Count > 0;
	}

	internal bool TryAppendPartElementsForRender(Array<StringName> layerList, IAdobeAnimateInterpolatedElementSink sink)
	{
		if (sprite == null || !GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(sprite.flashAnimeData) || sink == null)
		{
			return false;
		}
		Update();
		(_partSnapshotLayerIds ?? (_partSnapshotLayerIds = new HashSet<int>())).Clear();
		if (layerList != null)
		{
			foreach (StringName layer in layerList)
			{
				if (sprite.flashAnimeData.layerDictionary.TryGetValue(layer, out var value))
				{
					_partSnapshotLayerIds.Add((int)value);
				}
			}
		}
		return sprite.AppendInterpolatedLayerElementsForRender(_partSnapshotLayerIds, base.Transform.Origin, sink) > 0;
	}
}
