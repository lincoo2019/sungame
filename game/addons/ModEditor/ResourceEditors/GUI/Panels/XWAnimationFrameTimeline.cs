using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWAnimationFrameTimeline : Control
{
	private readonly struct CellRecord(int layer, int[] sliceKeys)
	{
		public int Layer { get; } = layer;

		public int[] SliceKeys { get; } = sliceKeys ?? Array.Empty<int>();
	}

	private const float RulerHeight = 32f;

	private const float LayerRailWidth = 158f;

	private const float RowHeight = 34f;

	private const float CellWidth = 28f;

	private const float DiamondRadius = 6.5f;

	private AdobeAnimateData _animation;

	private AdobeAnimateRuntimeDefinition _definition;

	private string[] _layerNames = Array.Empty<string>();

	private readonly Dictionary<long, int[]> _cellSliceKeys = new Dictionary<long, int[]>();

	private readonly Dictionary<int, List<CellRecord>> _cellsByFrame = new Dictionary<int, List<CellRecord>>();

	private int _selectedFrame = -1;

	private int _selectedLayer = -1;

	private int _selectedSliceKey = -1;

	private int _playheadFrame;

	private bool _dragging;

	private bool _dragMoved;

	private int _dragOriginFrame = -1;

	private int _dragOriginLayer = -1;

	private int _dragSliceKey = -1;

	private int _dragTargetFrame = -1;

	public int SelectedFrame => _selectedFrame;

	public int SelectedLayer => _selectedLayer;

	public int SelectedSliceKey => _selectedSliceKey;

	public int PlayheadFrame => _playheadFrame;

	public int OccupiedCellCount => _cellSliceKeys.Count;

	public int TimelineIndexRebuildCount { get; private set; }

	public int LastDrawVisitedFrameCount { get; private set; }

	public int LastDrawVisitedCellCount { get; private set; }

	public int VisibleRedrawRequestCount { get; private set; }

	public event Action<int, int, int> KeyframeSelected;

	public event Action<int, int, int, int> KeyframeMoveRequested;

	public override void _Ready()
	{
		base.MouseFilter = MouseFilterEnum.Stop;
		base.FocusMode = FocusModeEnum.All;
		base.ClipContents = true;
		base.CustomMinimumSize = new Vector2(300f, 220f);
		QueueTimelineRedraw();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 31 && IsNodeReady())
		{
			if (IsVisibleInTree())
			{
				QueueTimelineRedraw();
				return;
			}
			_dragging = false;
			ResetDrag();
		}
	}

	public void BindAnimation(AdobeAnimateData animation)
	{
		_animation = animation;
		_selectedFrame = -1;
		_selectedLayer = -1;
		_selectedSliceKey = -1;
		_playheadFrame = 0;
		RefreshFromAnimation();
	}

	public void RefreshFromAnimation()
	{
		_definition = ((GodotObject.IsInstanceValid(_animation) && _animation.frameMax > 0) ? AdobeAnimateDefinitionCache.GetOrBuild(_animation) : null);
		_layerNames = BuildLayerNames(_animation, _definition);
		RebuildCellIndex();
		int frameCount = GetFrameCount();
		_selectedFrame = ((_selectedFrame < 0) ? (-1) : Math.Clamp(_selectedFrame, 0, Math.Max(0, frameCount - 1)));
		_playheadFrame = Math.Clamp(_playheadFrame, 0, Math.Max(0, frameCount - 1));
		base.CustomMinimumSize = new Vector2(Math.Max(300f, 158f + (float)frameCount * 28f + 2f), Math.Max(220f, 32f + (float)Math.Max(1, _layerNames.Length) * 34f + 2f));
		QueueTimelineRedraw();
	}

	public void SetPlayhead(int frame)
	{
		int num = Math.Clamp(frame, 0, Math.Max(0, GetFrameCount() - 1));
		if (_playheadFrame != num)
		{
			_playheadFrame = num;
			QueueTimelineRedraw();
		}
	}

	public bool SelectKeyframe(int frame, int layer, int sliceKey = -1, bool notify = true)
	{
		if (!TryResolveCellSliceKey(frame, layer, sliceKey, out var sliceKey2))
		{
			return false;
		}
		_selectedFrame = frame;
		_selectedLayer = layer;
		_selectedSliceKey = sliceKey2;
		_playheadFrame = frame;
		QueueTimelineRedraw();
		if (notify)
		{
			this.KeyframeSelected?.Invoke(frame, layer, sliceKey2);
		}
		return true;
	}

	public bool SelectFirstOccupiedCell(bool notify = true)
	{
		int frameCount = GetFrameCount();
		for (int i = 0; i < frameCount; i++)
		{
			for (int j = 0; j < _layerNames.Length; j++)
			{
				if (SelectKeyframe(i, j, -1, notify))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool SelectSliceKey(int frame, int sliceKey, bool notify = true)
	{
		if (!_cellsByFrame.TryGetValue(frame, out var value))
		{
			return false;
		}
		foreach (CellRecord item in value)
		{
			if (Array.IndexOf(item.SliceKeys, sliceKey) >= 0)
			{
				return SelectKeyframe(frame, item.Layer, sliceKey, notify);
			}
		}
		return false;
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			if (inputEventMouseButton.Pressed)
			{
				GrabFocus();
				if (TryGetCell(inputEventMouseButton.Position, out var frame, out var layer) && SelectKeyframe(frame, layer))
				{
					_dragging = true;
					_dragMoved = false;
					_dragOriginFrame = frame;
					_dragOriginLayer = layer;
					_dragSliceKey = _selectedSliceKey;
					_dragTargetFrame = frame;
					AcceptEvent();
				}
			}
			else if (_dragging)
			{
				_dragging = false;
				if (_dragMoved && _dragTargetFrame >= 0 && _dragTargetFrame != _dragOriginFrame)
				{
					this.KeyframeMoveRequested?.Invoke(_dragOriginFrame, _dragTargetFrame, _dragOriginLayer, _dragSliceKey);
				}
				ResetDrag();
				QueueTimelineRedraw();
				AcceptEvent();
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragging)
		{
			if (TryGetCell(inputEventMouseMotion.Position, out var frame2, out var layer2) && layer2 == _dragOriginLayer)
			{
				_dragTargetFrame = frame2;
				_dragMoved |= frame2 != _dragOriginFrame;
				QueueTimelineRedraw();
			}
			AcceptEvent();
		}
	}

	public override void _Draw()
	{
		if (!IsVisibleInTree())
		{
			LastDrawVisitedFrameCount = 0;
			LastDrawVisitedCellCount = 0;
			return;
		}
		Rect2 visibleLocalRect = GetVisibleLocalRect();
		int frameCount = GetFrameCount();
		int num = Math.Clamp(Mathf.FloorToInt((visibleLocalRect.Position.X - 158f) / 28f) - 1, 0, Math.Max(0, frameCount - 1));
		int num2 = Math.Clamp(Mathf.CeilToInt((visibleLocalRect.End.X - 158f) / 28f) + 1, 0, Math.Max(0, frameCount - 1));
		int num3 = Math.Clamp(Mathf.FloorToInt((visibleLocalRect.Position.Y - 32f) / 34f) - 1, 0, Math.Max(0, _layerNames.Length - 1));
		int num4 = Math.Clamp(Mathf.CeilToInt((visibleLocalRect.End.Y - 32f) / 34f) + 1, 0, Math.Max(0, _layerNames.Length - 1));
		LastDrawVisitedFrameCount = ((frameCount > 0) ? Math.Max(0, num2 - num + 1) : 0);
		LastDrawVisitedCellCount = 0;
		DrawRect(visibleLocalRect, new Color(0.024f, 0.034f, 0.028f));
		DrawRect(new Rect2(visibleLocalRect.Position.X, 0f, visibleLocalRect.Size.X, 32f), new Color(0.057f, 0.083f, 0.063f));
		Font themeDefaultFont = GetThemeDefaultFont();
		int themeDefaultFontSize = GetThemeDefaultFontSize();
		for (int i = num; i <= num2; i++)
		{
			if (frameCount <= 0)
			{
				break;
			}
			float num5 = 158f + (float)i * 28f;
			bool flag = i % 5 == 0;
			Color color = (flag ? new Color(0.26f, 0.39f, 0.29f, 0.72f) : new Color(0.15f, 0.23f, 0.17f, 0.55f));
			DrawLine(new Vector2(num5, 32f), new Vector2(num5, base.Size.Y), color, flag ? 1.2f : 1f);
			if (flag)
			{
				DrawString(themeDefaultFont, new Vector2(num5 + 4f, 21f), i.ToString(), HorizontalAlignment.Left, 56f, themeDefaultFontSize - 1, new Color(0.66f, 0.76f, 0.64f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
		}
		for (int j = num3; j <= num4; j++)
		{
			if (_layerNames.Length == 0)
			{
				break;
			}
			float num6 = 32f + (float)j * 34f;
			Color color2 = ((j % 2 == 0) ? new Color(0.035f, 0.048f, 0.039f, 0.82f) : new Color(0.027f, 0.04f, 0.032f, 0.82f));
			DrawRect(new Rect2(158f, num6, Math.Max(0f, base.Size.X - 158f), 34f), color2);
			DrawLine(new Vector2(0f, num6 + 34f), new Vector2(base.Size.X, num6 + 34f), new Color(0.15f, 0.25f, 0.17f, 0.7f));
		}
		for (int k = num; k <= num2; k++)
		{
			if (frameCount <= 0)
			{
				break;
			}
			if (!_cellsByFrame.TryGetValue(k, out var value))
			{
				continue;
			}
			foreach (CellRecord item in value)
			{
				if (item.Layer >= num3 && item.Layer <= num4)
				{
					LastDrawVisitedCellCount++;
					float num7 = 158f + (float)k * 28f + 14f;
					float num8 = 32f + (float)item.Layer * 34f + 17f;
					bool flag2 = k == _selectedFrame && item.Layer == _selectedLayer && Array.IndexOf(item.SliceKeys, _selectedSliceKey) >= 0;
					Color color3 = (flag2 ? new Color(1f, 0.83f, 0.28f) : LayerColor(item.Layer));
					Vector2[] array = new Vector2[4]
					{
						new Vector2(num7, num8 - 6.5f),
						new Vector2(num7 + 6.5f, num8),
						new Vector2(num7, num8 + 6.5f),
						new Vector2(num7 - 6.5f, num8)
					};
					DrawColoredPolygon(array, color3);
					DrawPolyline(new Vector2[5]
					{
						array[0],
						array[1],
						array[2],
						array[3],
						array[0]
					}, flag2 ? Colors.White : color3.Lightened(0.28f), 1.4f);
					if (item.SliceKeys.Length > 1)
					{
						DrawCircle(new Vector2(num7 + 7f, num8 - 7f), 3f, new Color(0.98f, 0.62f, 0.26f));
					}
				}
			}
		}
		if (_dragging && _dragMoved && _dragTargetFrame >= 0 && _dragOriginLayer >= 0)
		{
			Rect2 rect = CellRect(_dragTargetFrame, _dragOriginLayer).Grow(-2f);
			DrawRect(rect, new Color(1f, 0.75f, 0.24f, 0.18f));
			DrawRect(rect, new Color(1f, 0.78f, 0.3f), filled: false, 2f);
		}
		float num9 = 158f + (float)_playheadFrame * 28f + 14f;
		DrawLine(new Vector2(num9, 0f), new Vector2(num9, base.Size.Y), new Color(1f, 0.42f, 0.18f, 0.92f), 2f);
		DrawColoredPolygon(new Vector2[3]
		{
			new Vector2(num9 - 6f, 0f),
			new Vector2(num9 + 6f, 0f),
			new Vector2(num9, 8f)
		}, new Color(1f, 0.58f, 0.2f));
		float x = visibleLocalRect.Position.X;
		DrawRect(new Rect2(x, visibleLocalRect.Position.Y, 158f, visibleLocalRect.Size.Y), new Color(0.045f, 0.067f, 0.052f, 0.98f));
		DrawRect(new Rect2(x, 0f, 158f, 32f), new Color(0.057f, 0.083f, 0.063f));
		DrawString(themeDefaultFont, new Vector2(x + 12f, 21f), "图层 / 完整帧状态", HorizontalAlignment.Left, 138f, themeDefaultFontSize, new Color(0.73f, 0.88f, 0.66f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		for (int l = num3; l <= num4; l++)
		{
			if (_layerNames.Length == 0)
			{
				break;
			}
			float num10 = 32f + (float)l * 34f;
			Color color4 = ((l % 2 == 0) ? new Color(0.045f, 0.067f, 0.052f, 0.98f) : new Color(0.038f, 0.058f, 0.045f, 0.98f));
			DrawRect(new Rect2(x, num10, 158f, 34f), color4);
			Color color5 = LayerColor(l);
			DrawCircle(new Vector2(x + 14f, num10 + 17f), 5.5f, color5);
			DrawString(themeDefaultFont, new Vector2(x + 27f, num10 + 22f), _layerNames[l], HorizontalAlignment.Left, 124f, themeDefaultFontSize, new Color(0.82f, 0.9f, 0.79f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawLine(new Vector2(x, num10 + 34f), new Vector2(x + 158f, num10 + 34f), new Color(0.15f, 0.25f, 0.17f, 0.7f));
		}
		DrawRect(visibleLocalRect, new Color(0.28f, 0.48f, 0.23f), filled: false, 1.5f);
	}

	private void RebuildCellIndex()
	{
		_cellSliceKeys.Clear();
		_cellsByFrame.Clear();
		TimelineIndexRebuildCount++;
		if (_definition?.Frames == null || _definition.SliceMetadata == null)
		{
			return;
		}
		Dictionary<long, List<int>> dictionary = new Dictionary<long, List<int>>();
		for (int i = 0; i < _definition.Frames.Length; i++)
		{
			PackedFrame packedFrame = _definition.Frames[i];
			int num = Math.Max(0, packedFrame.Offset);
			int num2 = Math.Min(_definition.SliceMetadata.Length, num + Math.Max(0, packedFrame.Count));
			for (int j = num; j < num2; j++)
			{
				PackedSliceMetadata packedSliceMetadata = _definition.SliceMetadata[j];
				int layerId = packedSliceMetadata.LayerId;
				if (layerId >= 0 && layerId < _layerNames.Length)
				{
					long key = EncodeCellKey(i, layerId);
					if (!dictionary.TryGetValue(key, out var value))
					{
						value = (dictionary[key] = new List<int>());
					}
					if (!value.Contains(packedSliceMetadata.SliceKey))
					{
						value.Add(packedSliceMetadata.SliceKey);
					}
				}
			}
		}
		foreach (KeyValuePair<long, List<int>> item in dictionary)
		{
			int[] array = item.Value.ToArray();
			_cellSliceKeys[item.Key] = array;
			DecodeCellKey(item.Key, out var frame, out var layer);
			if (!_cellsByFrame.TryGetValue(frame, out var value2))
			{
				value2 = (_cellsByFrame[frame] = new List<CellRecord>());
			}
			value2.Add(new CellRecord(layer, array));
		}
		foreach (List<CellRecord> value3 in _cellsByFrame.Values)
		{
			value3.Sort((CellRecord left, CellRecord right) => left.Layer.CompareTo(right.Layer));
		}
	}

	private bool TryResolveCellSliceKey(int frame, int layer, int requestedSliceKey, out int sliceKey)
	{
		sliceKey = -1;
		if (!_cellSliceKeys.TryGetValue(EncodeCellKey(frame, layer), out var value) || value.Length == 0)
		{
			return false;
		}
		if (requestedSliceKey >= 0 && Array.IndexOf(value, requestedSliceKey) >= 0)
		{
			sliceKey = requestedSliceKey;
		}
		else if (_selectedFrame == frame && _selectedLayer == layer && value.Length > 1)
		{
			int num = Array.IndexOf(value, _selectedSliceKey);
			sliceKey = value[(num + 1 + value.Length) % value.Length];
		}
		else
		{
			sliceKey = value[0];
		}
		return true;
	}

	private bool TryGetCell(Vector2 position, out int frame, out int layer)
	{
		frame = Mathf.FloorToInt((position.X - 158f) / 28f);
		layer = Mathf.FloorToInt((position.Y - 32f) / 34f);
		if (position.X >= 158f && position.Y >= 32f && frame >= 0 && frame < GetFrameCount() && layer >= 0)
		{
			return layer < _layerNames.Length;
		}
		return false;
	}

	private Rect2 CellRect(int frame, int layer)
	{
		return new Rect2(158f + (float)frame * 28f, 32f + (float)layer * 34f, 28f, 34f);
	}

	private Rect2 GetVisibleLocalRect()
	{
		Node parent = GetParent();
		while (GodotObject.IsInstanceValid(parent))
		{
			if (parent is ScrollContainer scrollContainer)
			{
				Vector2 vector = base.GlobalPosition - scrollContainer.GlobalPosition;
				return new Rect2(new Vector2(scrollContainer.ScrollHorizontal, scrollContainer.ScrollVertical) - vector, new Vector2(Math.Max(1f, scrollContainer.Size.X), Math.Max(1f, scrollContainer.Size.Y)));
			}
			parent = parent.GetParent();
		}
		return new Rect2(Vector2.Zero, base.Size);
	}

	private void QueueTimelineRedraw()
	{
		if (IsInsideTree() && IsVisibleInTree())
		{
			VisibleRedrawRequestCount++;
			QueueRedraw();
		}
	}

	private int GetFrameCount()
	{
		if (_definition?.Frames != null && _definition.Frames.Length != 0)
		{
			return _definition.Frames.Length;
		}
		return Math.Max(0, _animation?.frameMax ?? 0);
	}

	private void ResetDrag()
	{
		_dragMoved = false;
		_dragOriginFrame = -1;
		_dragOriginLayer = -1;
		_dragSliceKey = -1;
		_dragTargetFrame = -1;
	}

	private static string[] BuildLayerNames(AdobeAnimateData animation, AdobeAnimateRuntimeDefinition definition)
	{
		int num = Math.Max((animation?.layerDictionary?.Count).GetValueOrDefault(), definition?.RuntimeLayerCount ?? 0);
		if (num <= 0)
		{
			return Array.Empty<string>();
		}
		string[] array = new string[num];
		if (animation?.layerDictionary != null)
		{
			foreach (Variant key in animation.layerDictionary.Keys)
			{
				int num2 = animation.layerDictionary[key].AsInt32();
				if ((uint)num2 < (uint)array.Length)
				{
					array[num2] = key.AsString();
				}
			}
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (string.IsNullOrWhiteSpace(array[i]))
			{
				array[i] = $"图层 {i + 1}";
			}
		}
		return array;
	}

	private static Color LayerColor(int layer)
	{
		return Color.FromHsv(Mathf.PosMod((float)layer * 0.137f + 0.23f, 1f), 0.52f, 0.94f);
	}

	private static long EncodeCellKey(int frame, int layer)
	{
		return ((long)frame << 32) | (uint)layer;
	}

	private static void DecodeCellKey(long key, out int frame, out int layer)
	{
		frame = (int)(key >> 32);
		layer = (int)(key & 0xFFFFFFFFu);
	}
}
