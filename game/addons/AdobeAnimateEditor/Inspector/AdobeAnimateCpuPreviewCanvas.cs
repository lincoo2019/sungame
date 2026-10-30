using System.Collections.Generic;
using Godot;

namespace PVZHE.AdobeAnimateEditor.Inspector;

[Tool]
public partial class AdobeAnimateCpuPreviewCanvas : Control
{
	private readonly List<AdobeAnimateDrawItem> _drawItems = new List<AdobeAnimateDrawItem>();

	private readonly List<AdobeAnimateDrawItem> _boundsDrawItems = new List<AdobeAnimateDrawItem>();

	private readonly Dictionary<int, Texture2D> _arrayLayerTextures = new Dictionary<int, Texture2D>();

	private AdobeAnimateSprite _source;

	private bool _previewActive = true;

	public bool HasRenderableSnapshot { get; private set; }

	public int LastDrawItemCount { get; private set; }

	public override void _Ready()
	{
		UpdateProcessingState();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 31 && IsNodeReady())
		{
			UpdateProcessingState();
		}
	}

	public override void _Process(double delta)
	{
		if (GodotObject.IsInstanceValid(_source))
		{
			QueueRedraw();
		}
	}

	public void BindSource(AdobeAnimateSprite source)
	{
		if (_source != source)
		{
			_arrayLayerTextures.Clear();
		}
		_source = (GodotObject.IsInstanceValid(source) ? source : null);
		UpdateProcessingState();
		QueueRedraw();
	}

	public void SetPreviewActive(bool active)
	{
		if (_previewActive != active)
		{
			_previewActive = active;
			UpdateProcessingState();
		}
	}

	public bool TryGetLocalRenderBounds(out Rect2 bounds)
	{
		bounds = default(Rect2);
		if (!GodotObject.IsInstanceValid(_source) || !_source.TryBuildRenderSnapshot(out var snapshot) || snapshot.Definition == null)
		{
			return false;
		}
		_boundsDrawItems.Clear();
		AdobeAnimateDrawItemBuilder.Build(snapshot, _boundsDrawItems);
		if (_boundsDrawItems.Count == 0)
		{
			return false;
		}
		Transform2D transform2D = snapshot.GlobalTransform.AffineInverse();
		bool hasPoint = false;
		Vector2 min = Vector2.Zero;
		Vector2 max = Vector2.Zero;
		for (int i = 0; i < _boundsDrawItems.Count; i++)
		{
			AdobeAnimateDrawItem adobeAnimateDrawItem = _boundsDrawItems[i];
			if (!(adobeAnimateDrawItem.Color.A <= 0.001f))
			{
				Transform2D transform2D2 = transform2D * adobeAnimateDrawItem.Transform;
				IncludeBoundsPoint(ref hasPoint, ref min, ref max, transform2D2 * Vector2.Zero);
				IncludeBoundsPoint(ref hasPoint, ref min, ref max, transform2D2 * Vector2.Right);
				IncludeBoundsPoint(ref hasPoint, ref min, ref max, transform2D2 * Vector2.Down);
				IncludeBoundsPoint(ref hasPoint, ref min, ref max, transform2D2 * Vector2.One);
			}
		}
		if (!hasPoint)
		{
			return false;
		}
		Vector2 size = max - min;
		if (size.X <= 0.001f || size.Y <= 0.001f)
		{
			return false;
		}
		bounds = new Rect2(min, size);
		return true;
	}

	private static void IncludeBoundsPoint(ref bool hasPoint, ref Vector2 min, ref Vector2 max, Vector2 point)
	{
		if (float.IsFinite(point.X) && float.IsFinite(point.Y))
		{
			if (!hasPoint)
			{
				min = point;
				max = point;
				hasPoint = true;
			}
			else
			{
				min = new Vector2(Mathf.Min(min.X, point.X), Mathf.Min(min.Y, point.Y));
				max = new Vector2(Mathf.Max(max.X, point.X), Mathf.Max(max.Y, point.Y));
			}
		}
	}

	private void UpdateProcessingState()
	{
		SetProcess(_previewActive && IsVisibleInTree() && GodotObject.IsInstanceValid(_source));
	}

	public override void _Draw()
	{
		HasRenderableSnapshot = false;
		LastDrawItemCount = 0;
		if (!GodotObject.IsInstanceValid(_source) || !_source.TryBuildRenderSnapshot(out var snapshot) || snapshot.Definition == null)
		{
			return;
		}
		_drawItems.Clear();
		AdobeAnimateDrawItemBuilder.Build(snapshot, _drawItems);
		if (snapshot.NeedsDrawItemSort)
		{
			_drawItems.Sort(AdobeAnimateDrawItemComparer.Instance);
		}
		LastDrawItemCount = _drawItems.Count;
		HasRenderableSnapshot = _drawItems.Count > 0;
		for (int i = 0; i < _drawItems.Count; i++)
		{
			AdobeAnimateDrawItem adobeAnimateDrawItem = _drawItems[i];
			Texture2D texture2D = ResolveAtlas(snapshot.Definition, adobeAnimateDrawItem.AtlasLayer);
			if (GodotObject.IsInstanceValid(texture2D) && !(adobeAnimateDrawItem.Color.A <= 0.001f))
			{
				Vector2 vector = ResolveAtlasSize(snapshot.Definition, texture2D);
				Rect2 srcRect = new Rect2(adobeAnimateDrawItem.UvRect.Position * vector, adobeAnimateDrawItem.UvRect.Size * vector);
				if (!(srcRect.Size.X <= 0.001f) && !(srcRect.Size.Y <= 0.001f))
				{
					DrawSetTransformMatrix(adobeAnimateDrawItem.Transform);
					DrawTextureRectRegion(texture2D, new Rect2(Vector2.Zero, Vector2.One), srcRect, adobeAnimateDrawItem.Color);
				}
			}
		}
		DrawSetTransformMatrix(Transform2D.Identity);
	}

	private Texture2D ResolveAtlas(AdobeAnimateRuntimeDefinition definition, int layer)
	{
		if (definition?.AtlasPages != null && layer >= 0 && layer < definition.AtlasPages.Length && GodotObject.IsInstanceValid(definition.AtlasPages[layer]))
		{
			return definition.AtlasPages[layer];
		}
		if (definition != null && definition.UsesAtlasTextureArrayLayout && GodotObject.IsInstanceValid(definition.AtlasTextureArray))
		{
			if (_arrayLayerTextures.TryGetValue(layer, out var value) && GodotObject.IsInstanceValid(value))
			{
				return value;
			}
			try
			{
				Image layerData = definition.AtlasTextureArray.GetLayerData(layer);
				if (GodotObject.IsInstanceValid(layerData) && !layerData.IsEmpty())
				{
					ImageTexture imageTexture = ImageTexture.CreateFromImage(layerData);
					_arrayLayerTextures[layer] = imageTexture;
					return imageTexture;
				}
			}
			catch
			{
			}
		}
		return definition?.BaseAtlas;
	}

	private static Vector2 ResolveAtlasSize(AdobeAnimateRuntimeDefinition definition, Texture2D atlas)
	{
		if (definition != null && definition.UsesAtlasTextureArrayLayout && definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f)
		{
			return definition.AtlasTextureArraySize;
		}
		if (!GodotObject.IsInstanceValid(atlas))
		{
			return Vector2.One;
		}
		return atlas.GetSize();
	}
}
