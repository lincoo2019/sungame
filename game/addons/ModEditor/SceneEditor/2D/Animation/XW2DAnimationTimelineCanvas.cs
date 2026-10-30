using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DAnimationTimelineCanvas : Control
{
	private sealed class TrackRecord
	{
		public int TrackIndex;

		public Animation.TrackType Type;

		public bool Enabled;

		public string Path = "";

		public string TypeLabel = "";

		public Color Color = Colors.White;

		public Animation.InterpolationType Interpolation;

		public Animation.UpdateMode UpdateMode;

		public bool InterpolationLoopWrap;

		public double[] KeyTimes = Array.Empty<double>();

		public float[] KeyTransitions = Array.Empty<float>();
	}

	private const float RulerHeight = 34f;

	private const float TrackRailWidth = 238f;

	private const float TrackRowHeight = 42f;

	private const float KeyRadius = 6.5f;

	private const float KeyHitRadius = 11f;

	private const double MinimumTimeScale = 24.0;

	private const double MaximumTimeScale = 960.0;

	private const double DefaultTimeScale = 96.0;

	private const double TimeEpsilon = 1E-06;

	private readonly List<TrackRecord> _tracks = new List<TrackRecord>();

	private readonly Vector2[][] _transitionPointBuffers = new Vector2[19][];

	private Animation _animation;

	private Action _animationChangedAction;

	private bool _timelineIndexDirty;

	private double _playhead;

	private double _timeScale = 96.0;

	private double _timeWindowStart;

	private float _trackScroll;

	private int _selectedTrack = -1;

	private double _selectedKeyTime = -1.0;

	private bool _scrubbing;

	private bool _draggingKey;

	private bool _dragMoved;

	private int _dragTrack = -1;

	private double _dragOriginTime = -1.0;

	private double _dragTargetTime = -1.0;

	private bool _panningTime;

	public Animation BoundAnimation => _animation;

	public double Playhead => _playhead;

	public double TimeScale => _timeScale;

	public double VisibleTimeStart => _timeWindowStart;

	public int SelectedTrack => _selectedTrack;

	public double SelectedKeyTime => _selectedKeyTime;

	public int LastDrawVisitedTrackCount { get; private set; }

	public int LastDrawVisitedKeyCount { get; private set; }

	public int TimelineIndexRebuildCount { get; private set; }

	public event Action<double> PlayheadChanged;

	public event Action<int, double> KeySelected;

	public event Action<int, double, double> KeyMoveRequested;

	public override void _Ready()
	{
		base.MouseFilter = MouseFilterEnum.Stop;
		base.FocusMode = FocusModeEnum.All;
		base.ClipContents = true;
		base.CustomMinimumSize = new Vector2(420f, 220f);
		if (_timelineIndexDirty && ShouldVisitTimeline())
		{
			EnsureTimelineIndex();
		}
		QueueTimelineRedraw();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 31 && IsNodeReady())
		{
			if (ShouldVisitTimeline())
			{
				EnsureTimelineIndex();
				QueueTimelineRedraw();
			}
			else
			{
				CancelPointerOperation();
				LastDrawVisitedTrackCount = 0;
				LastDrawVisitedKeyCount = 0;
			}
		}
		else if ((long)what == 40 && IsNodeReady())
		{
			ClampViewport();
			QueueTimelineRedraw();
		}
		else if ((long)what == 1)
		{
			DisconnectAnimation();
		}
	}

	public void BindAnimation(Animation animation)
	{
		if (_animation == animation)
		{
			MarkTimelineIndexDirty();
			return;
		}
		DisconnectAnimation();
		_animation = animation;
		if (GodotObject.IsInstanceValid(_animation))
		{
			if (_animationChangedAction == null)
			{
				_animationChangedAction = OnAnimationChanged;
			}
			_animation.Changed += _animationChangedAction;
		}
		_selectedTrack = -1;
		_selectedKeyTime = -1.0;
		_playhead = 0.0;
		_timeWindowStart = 0.0;
		_trackScroll = 0f;
		MarkTimelineIndexDirty();
	}

	public void SetPlayhead(double time)
	{
		SetPlayheadInternal(time, notify: false);
	}

	public void SetTimeScale(double pixelsPerSecond)
	{
		double num = Math.Clamp(pixelsPerSecond, 24.0, 960.0);
		if (!(Math.Abs(num - _timeScale) <= 1E-06))
		{
			_timeScale = num;
			ClampViewport();
			QueueTimelineRedraw();
		}
	}

	public bool SelectKey(int trackIndex, double time)
	{
		EnsureTimelineIndex();
		TrackRecord trackRecord = FindTrack(trackIndex);
		if (trackRecord == null || FindNearestKey(trackRecord.KeyTimes, time, 0.0005) < 0)
		{
			return false;
		}
		_selectedTrack = trackIndex;
		_selectedKeyTime = time;
		QueueTimelineRedraw();
		return true;
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (!ShouldVisitTimeline())
		{
			return;
		}
		EnsureTimelineIndex();
		if (inputEvent is InputEventMouseButton inputEventMouseButton)
		{
			if (HandleWheel(inputEventMouseButton))
			{
				return;
			}
			if (inputEventMouseButton.ButtonIndex == MouseButton.Middle)
			{
				_panningTime = inputEventMouseButton.Pressed;
				if (inputEventMouseButton.Pressed)
				{
					GrabFocus();
				}
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Left)
			{
				if (inputEventMouseButton.Pressed)
				{
					BeginLeftPointerOperation(inputEventMouseButton.Position);
				}
				else
				{
					FinishLeftPointerOperation();
				}
				AcceptEvent();
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion)
		{
			if (_panningTime)
			{
				_timeWindowStart = Math.Max(0.0, _timeWindowStart - (double)inputEventMouseMotion.Relative.X / _timeScale);
				ClampViewport();
				QueueTimelineRedraw();
				AcceptEvent();
			}
			else if (_draggingKey)
			{
				double val = XToTime(inputEventMouseMotion.Position.X);
				_dragTargetTime = Math.Max(0.0, val);
				_dragMoved |= Math.Abs(_dragTargetTime - _dragOriginTime) > 1E-06;
				QueueTimelineRedraw();
				AcceptEvent();
			}
			else if (_scrubbing)
			{
				SetPlayheadInternal(XToTime(inputEventMouseMotion.Position.X), notify: true);
				AcceptEvent();
			}
		}
	}

	public override void _Draw()
	{
		if (!ShouldVisitTimeline())
		{
			LastDrawVisitedTrackCount = 0;
			LastDrawVisitedKeyCount = 0;
			return;
		}
		EnsureTimelineIndex();
		LastDrawVisitedTrackCount = 0;
		LastDrawVisitedKeyCount = 0;
		DrawRect(new Rect2(Vector2.Zero, base.Size), new Color("111821"));
		DrawRect(new Rect2(238f, 0f, Math.Max(0f, base.Size.X - 238f), 34f), new Color("1b2930"));
		DrawTimeRulerAndGrid();
		DrawTracksAndKeys();
		DrawPlayhead();
		DrawFixedTrackRail();
		DrawRect(new Rect2(Vector2.Zero, base.Size), new Color("52705e"), filled: false, 1f);
	}

	private void DrawTimeRulerAndGrid()
	{
		if (base.Size.X <= 238f)
		{
			return;
		}
		double num = ChooseMajorTickStep(_timeScale);
		double num2 = num / 5.0;
		double num3 = XToTime(base.Size.X);
		double num4 = Math.Ceiling(_timeWindowStart / num2) * num2;
		Font themeDefaultFont = GetThemeDefaultFont();
		int fontSize = Math.Max(10, GetThemeDefaultFontSize() - 1);
		for (double num5 = num4; num5 <= num3 + 1E-06; num5 += num2)
		{
			float num6 = TimeToX(num5);
			double num7 = num5 / num;
			bool flag = Math.Abs(num7 - Math.Round(num7)) < 0.0001;
			Color color = (flag ? new Color("49605b99") : new Color("31443f66"));
			DrawLine(new Vector2(num6, flag ? 18f : 26f), new Vector2(num6, base.Size.Y), color, flag ? 1.2f : 1f);
			if (flag)
			{
				DrawString(themeDefaultFont, new Vector2(num6 + 4f, 17f), FormatSeconds(num5), HorizontalAlignment.Left, 82f, fontSize, new Color("c7d8c9"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
		}
		DrawLine(new Vector2(238f, 34f), new Vector2(base.Size.X, 34f), new Color("52665f"));
	}

	private void DrawTracksAndKeys()
	{
		if (_tracks.Count == 0)
		{
			DrawString(GetThemeDefaultFont(), new Vector2(262f, 76f), (_animation == null) ? "尚未绑定动画" : "动画中没有可显示的轨道", HorizontalAlignment.Left, Math.Max(1f, base.Size.X - 238f - 32f), GetThemeDefaultFontSize(), new Color("8fa39a"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			return;
		}
		GetVisibleRowRange(out var firstRow, out var lastRow);
		double timeWindowStart = _timeWindowStart;
		double num = XToTime(base.Size.X);
		for (int i = firstRow; i <= lastRow; i++)
		{
			TrackRecord trackRecord = _tracks[i];
			LastDrawVisitedTrackCount++;
			float num2 = TrackRowY(i);
			Color color = ((i % 2 == 0) ? new Color("172229") : new Color("141e25"));
			if (!trackRecord.Enabled)
			{
				color = color.Darkened(0.18f);
			}
			DrawRect(new Rect2(238f, num2, Math.Max(0f, base.Size.X - 238f), 42f), color);
			DrawLine(new Vector2(238f, num2 + 42f), new Vector2(base.Size.X, num2 + 42f), new Color("31423e99"));
			DrawTransitionSegments(trackRecord, num2 + 21f, timeWindowStart, num);
			for (int j = LowerBound(trackRecord.KeyTimes, timeWindowStart); j < trackRecord.KeyTimes.Length; j++)
			{
				double num3 = trackRecord.KeyTimes[j];
				if (num3 > num + 1E-06)
				{
					break;
				}
				LastDrawVisitedKeyCount++;
				DrawKey(trackRecord, num3, num2 + 21f);
			}
		}
		if (_draggingKey && _dragMoved && _dragTrack >= 0)
		{
			int num4 = FindTrackRow(_dragTrack);
			if (num4 >= firstRow && num4 <= lastRow)
			{
				Vector2 center = new Vector2(TimeToX(_dragTargetTime), TrackRowY(num4) + 21f);
				DrawDiamond(center, 8.5f, new Color("ffd66b66"), new Color("fff0ab"), 2f);
			}
		}
	}

	private void DrawKey(TrackRecord track, double time, float centerY)
	{
		float x = TimeToX(time);
		bool flag = track.TrackIndex == _selectedTrack && Math.Abs(time - _selectedKeyTime) <= 0.0005;
		Color fill = (track.Enabled ? track.Color : new Color("66716c"));
		if (flag)
		{
			fill = new Color("ffd55e");
		}
		DrawDiamond(new Vector2(x, centerY), 6.5f, fill, flag ? Colors.White : fill.Lightened(0.28f), flag ? 2f : 1.2f);
	}

	private void DrawTransitionSegments(TrackRecord track, float centerY, double visibleStart, double visibleEnd)
	{
		if (track.KeyTimes.Length < 2)
		{
			return;
		}
		int num = Math.Max(0, LowerBound(track.KeyTimes, visibleStart) - 1);
		Color color = (track.Enabled ? new Color(track.Color, 0.72f).Darkened(0.12f) : new Color("59635e88"));
		for (int i = num; i < track.KeyTimes.Length - 1; i++)
		{
			double num2 = track.KeyTimes[i];
			double num3 = track.KeyTimes[i + 1];
			if (num2 > visibleEnd + 1E-06)
			{
				break;
			}
			if (num3 < visibleStart - 1E-06)
			{
				continue;
			}
			float num4 = TimeToX(num2);
			float num5 = TimeToX(num3);
			if (num5 - num4 < 2f)
			{
				continue;
			}
			if (track.UpdateMode == Animation.UpdateMode.Discrete || track.Interpolation == Animation.InterpolationType.Nearest)
			{
				DrawLine(new Vector2(num4, centerY + 6f), new Vector2(num5, centerY + 6f), color, 1.6f, antialiased: true);
				DrawLine(new Vector2(num5, centerY + 6f), new Vector2(num5, centerY - 6f), color, 1.6f, antialiased: true);
				continue;
			}
			int num6 = Math.Clamp(Mathf.CeilToInt((num5 - num4) / 14f), 4, 18);
			Vector2[][] transitionPointBuffers = _transitionPointBuffers;
			int num7 = num6;
			Vector2[] array = transitionPointBuffers[num7] ?? (transitionPointBuffers[num7] = new Vector2[num6 + 1]);
			float curve = ((i < track.KeyTransitions.Length) ? track.KeyTransitions[i] : 1f);
			for (int j = 0; j <= num6; j++)
			{
				float num8 = (float)j / (float)num6;
				float num9 = Mathf.Ease(num8, curve);
				if (!float.IsFinite(num9))
				{
					num9 = num8;
				}
				array[j] = new Vector2(Mathf.Lerp(num4, num5, num8), Mathf.Lerp(centerY + 6f, centerY - 6f, Mathf.Clamp(num9, 0f, 1f)));
			}
			DrawPolyline(array, color, 1.6f, antialiased: true);
		}
	}

	private void DrawPlayhead()
	{
		float num = TimeToX(_playhead);
		if (!(num < 237f) && !(num > base.Size.X + 1f))
		{
			Color color = new Color("ff7958");
			DrawLine(new Vector2(num, 0f), new Vector2(num, base.Size.Y), color, 2f);
			DrawColoredPolygon(new Vector2[3]
			{
				new Vector2(num - 6f, 0f),
				new Vector2(num + 6f, 0f),
				new Vector2(num, 9f)
			}, color);
		}
	}

	private void DrawFixedTrackRail()
	{
		Font themeDefaultFont = GetThemeDefaultFont();
		int num = Math.Max(11, GetThemeDefaultFontSize());
		DrawRect(new Rect2(0f, 0f, 238f, base.Size.Y), new Color("17241f"));
		DrawRect(new Rect2(0f, 0f, 238f, 34f), new Color("21352a"));
		DrawString(themeDefaultFont, new Vector2(12f, 22f), "轨道 / 类型与状态", HorizontalAlignment.Left, 218f, num, new Color("d3e8c9"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		if (_tracks.Count == 0)
		{
			DrawLine(new Vector2(238f, 0f), new Vector2(238f, base.Size.Y), new Color("607468"));
			return;
		}
		GetVisibleRowRange(out var firstRow, out var lastRow);
		for (int i = firstRow; i <= lastRow; i++)
		{
			TrackRecord trackRecord = _tracks[i];
			float num2 = TrackRowY(i);
			Color color = ((i % 2 == 0) ? new Color("1c2d25") : new Color("192820"));
			DrawRect(new Rect2(0f, num2, 238f, 42f), color);
			DrawCircle(new Vector2(13f, num2 + 13f), 5f, trackRecord.Enabled ? new Color("71d47c") : new Color("6f7873"));
			DrawString(themeDefaultFont, new Vector2(25f, num2 + 17f), EllipsizeTrackPath(trackRecord.Path), HorizontalAlignment.Left, 207f, num, new Color("e0e9df"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			string value = (trackRecord.Enabled ? "启用" : "停用");
			Color value2 = (trackRecord.Enabled ? trackRecord.Color.Lightened(0.22f) : new Color("89938e"));
			string interpolationBadge = GetInterpolationBadge(trackRecord.Interpolation);
			string value3 = ((trackRecord.Type == Animation.TrackType.Value) ? (" · " + GetUpdateModeBadge(trackRecord.UpdateMode)) : "");
			string value4 = (trackRecord.InterpolationLoopWrap ? " · ↻" : "");
			DrawString(themeDefaultFont, new Vector2(25f, num2 + 35f), $"{trackRecord.TypeLabel} · {value} · {interpolationBadge}{value3}{value4}", HorizontalAlignment.Left, 207f, Math.Max(10, num - 2), value2, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawLine(new Vector2(0f, num2 + 42f), new Vector2(238f, num2 + 42f), new Color("3b5045"));
		}
		DrawLine(new Vector2(238f, 0f), new Vector2(238f, base.Size.Y), new Color("607468"), 1.5f);
	}

	private bool HandleWheel(InputEventMouseButton button)
	{
		if (!button.Pressed || (button.ButtonIndex != MouseButton.WheelUp && button.ButtonIndex != MouseButton.WheelDown))
		{
			return false;
		}
		double num = ((button.ButtonIndex == MouseButton.WheelUp) ? 1.0 : (-1.0));
		double num2 = Math.Max(1.0, button.Factor);
		if (button.CtrlPressed)
		{
			double num3 = XToTime(button.Position.X);
			double num4 = Math.Pow(1.14, num * num2);
			double num5 = Math.Clamp(_timeScale * num4, 24.0, 960.0);
			if (button.Position.X >= 238f)
			{
				_timeWindowStart = Math.Max(0.0, num3 - (double)(button.Position.X - 238f) / num5);
			}
			_timeScale = num5;
		}
		else if (button.ShiftPressed)
		{
			_timeWindowStart = Math.Max(0.0, _timeWindowStart - num * num2 * 96.0 / _timeScale);
		}
		else
		{
			_trackScroll -= (float)(num * num2 * 42.0 * 2.0);
		}
		ClampViewport();
		QueueTimelineRedraw();
		AcceptEvent();
		return true;
	}

	private void BeginLeftPointerOperation(Vector2 position)
	{
		GrabFocus();
		if (!(position.X < 238f))
		{
			if (TryHitKey(position, out var track, out var keyTime))
			{
				_selectedTrack = track.TrackIndex;
				_selectedKeyTime = keyTime;
				_playhead = keyTime;
				_draggingKey = true;
				_dragMoved = false;
				_dragTrack = track.TrackIndex;
				_dragOriginTime = keyTime;
				_dragTargetTime = keyTime;
				this.KeySelected?.Invoke(track.TrackIndex, keyTime);
				this.PlayheadChanged?.Invoke(keyTime);
				QueueTimelineRedraw();
			}
			else
			{
				_scrubbing = true;
				SetPlayheadInternal(XToTime(position.X), notify: true);
			}
		}
	}

	private void FinishLeftPointerOperation()
	{
		if (_draggingKey && _dragMoved && _dragTrack >= 0 && Math.Abs(_dragTargetTime - _dragOriginTime) > 1E-06)
		{
			this.KeyMoveRequested?.Invoke(_dragTrack, _dragOriginTime, _dragTargetTime);
		}
		CancelPointerOperation();
		QueueTimelineRedraw();
	}

	private bool TryHitKey(Vector2 position, out TrackRecord track, out double keyTime)
	{
		track = null;
		keyTime = -1.0;
		int num = Mathf.FloorToInt((position.Y - 34f + _trackScroll) / 42f);
		if (num < 0 || num >= _tracks.Count)
		{
			return false;
		}
		float num2 = TrackRowY(num);
		if (position.Y < num2 || position.Y > num2 + 42f)
		{
			return false;
		}
		TrackRecord trackRecord = _tracks[num];
		double target = XToTime(position.X);
		double tolerance = 11.0 / _timeScale;
		int num3 = FindNearestKey(trackRecord.KeyTimes, target, tolerance);
		if (num3 < 0)
		{
			return false;
		}
		track = trackRecord;
		keyTime = trackRecord.KeyTimes[num3];
		return true;
	}

	private void SetPlayheadInternal(double time, bool notify)
	{
		double num = Math.Max(0.0, time);
		if (!(Math.Abs(num - _playhead) <= 1E-06))
		{
			_playhead = num;
			QueueTimelineRedraw();
			if (notify)
			{
				this.PlayheadChanged?.Invoke(_playhead);
			}
		}
	}

	private void OnAnimationChanged()
	{
		MarkTimelineIndexDirty();
	}

	private void MarkTimelineIndexDirty()
	{
		_timelineIndexDirty = true;
		if (ShouldVisitTimeline())
		{
			EnsureTimelineIndex();
		}
		QueueTimelineRedraw();
	}

	private void EnsureTimelineIndex()
	{
		if (_timelineIndexDirty)
		{
			_timelineIndexDirty = false;
			RebuildTimelineIndex();
		}
	}

	private void RebuildTimelineIndex()
	{
		_tracks.Clear();
		TimelineIndexRebuildCount++;
		if (!GodotObject.IsInstanceValid(_animation))
		{
			ClampViewport();
			return;
		}
		int trackCount = _animation.GetTrackCount();
		for (int i = 0; i < trackCount; i++)
		{
			try
			{
				Animation.TrackType trackType = _animation.TrackGetType(i);
				int val = _animation.TrackGetKeyCount(i);
				double[] array = new double[Math.Max(0, val)];
				float[] array2 = new float[Math.Max(0, val)];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = _animation.TrackGetKeyTime(i, j);
					array2[j] = _animation.TrackGetKeyTransition(i, j);
				}
				_tracks.Add(new TrackRecord
				{
					TrackIndex = i,
					Type = trackType,
					Enabled = _animation.TrackIsEnabled(i),
					Path = _animation.TrackGetPath(i).ToString(),
					TypeLabel = GetTrackTypeLabel(trackType),
					Color = GetTrackTypeColor(trackType),
					Interpolation = _animation.TrackGetInterpolationType(i),
					UpdateMode = ((trackType == Animation.TrackType.Value) ? _animation.ValueTrackGetUpdateMode(i) : Animation.UpdateMode.Continuous),
					InterpolationLoopWrap = _animation.TrackGetInterpolationLoopWrap(i),
					KeyTimes = array,
					KeyTransitions = array2
				});
			}
			catch (Exception ex)
			{
				GD.PushWarning($"2D 动画时间轴跳过无法读取的轨道 {i}: {ex.Message}");
			}
		}
		ClampViewport();
	}

	private void DisconnectAnimation()
	{
		if (GodotObject.IsInstanceValid(_animation) && _animationChangedAction != null)
		{
			_animation.Changed -= _animationChangedAction;
		}
	}

	private void ClampViewport()
	{
		float num = (float)_tracks.Count * 42f;
		float num2 = Math.Max(0f, base.Size.Y - 34f);
		_trackScroll = Math.Clamp(_trackScroll, 0f, Math.Max(0f, num - num2));
		double timelineEnd = GetTimelineEnd();
		double num3 = Math.Max(0.0, base.Size.X - 238f) / _timeScale;
		_timeWindowStart = Math.Clamp(_timeWindowStart, 0.0, Math.Max(0.0, timelineEnd - num3 * 0.1));
	}

	private double GetTimelineEnd()
	{
		double num = (GodotObject.IsInstanceValid(_animation) ? Math.Max(0.0, _animation.Length) : 0.0);
		foreach (TrackRecord track in _tracks)
		{
			if (track.KeyTimes.Length != 0)
			{
				num = Math.Max(num, track.KeyTimes[^1]);
			}
		}
		return Math.Max(1.0, num);
	}

	private void GetVisibleRowRange(out int firstRow, out int lastRow)
	{
		if (_tracks.Count == 0 || base.Size.Y <= 34f)
		{
			firstRow = 0;
			lastRow = -1;
		}
		else
		{
			firstRow = Math.Clamp(Mathf.FloorToInt(_trackScroll / 42f), 0, _tracks.Count - 1);
			int num = Mathf.CeilToInt((base.Size.Y - 34f) / 42f) + 1;
			lastRow = Math.Min(_tracks.Count - 1, firstRow + num);
		}
	}

	private float TrackRowY(int row)
	{
		return 34f + (float)row * 42f - _trackScroll;
	}

	private float TimeToX(double time)
	{
		return 238f + (float)((time - _timeWindowStart) * _timeScale);
	}

	private double XToTime(float x)
	{
		return Math.Max(0.0, _timeWindowStart + (double)(x - 238f) / _timeScale);
	}

	private TrackRecord FindTrack(int trackIndex)
	{
		foreach (TrackRecord track in _tracks)
		{
			if (track.TrackIndex == trackIndex)
			{
				return track;
			}
		}
		return null;
	}

	private int FindTrackRow(int trackIndex)
	{
		for (int i = 0; i < _tracks.Count; i++)
		{
			if (_tracks[i].TrackIndex == trackIndex)
			{
				return i;
			}
		}
		return -1;
	}

	private void CancelPointerOperation()
	{
		_scrubbing = false;
		_draggingKey = false;
		_dragMoved = false;
		_dragTrack = -1;
		_dragOriginTime = -1.0;
		_dragTargetTime = -1.0;
		_panningTime = false;
	}

	private bool ShouldVisitTimeline()
	{
		if (base.Visible)
		{
			if (IsInsideTree())
			{
				return IsVisibleInTree();
			}
			return true;
		}
		return false;
	}

	private void QueueTimelineRedraw()
	{
		if (IsInsideTree() && IsVisibleInTree())
		{
			QueueRedraw();
		}
	}

	private static int LowerBound(double[] values, double target)
	{
		int num = 0;
		int num2 = values.Length;
		while (num < num2)
		{
			int num3 = num + (num2 - num) / 2;
			if (values[num3] < target)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private static int FindNearestKey(double[] values, double target, double tolerance)
	{
		int num = LowerBound(values, target);
		int result = -1;
		double num2 = tolerance + 1E-06;
		if (num < values.Length)
		{
			double num3 = Math.Abs(values[num] - target);
			if (num3 <= num2)
			{
				result = num;
				num2 = num3;
			}
		}
		if (num > 0 && Math.Abs(values[num - 1] - target) <= num2)
		{
			result = num - 1;
		}
		return result;
	}

	private static double ChooseMajorTickStep(double pixelsPerSecond)
	{
		double[] array = new double[10] { 0.05, 0.1, 0.2, 0.5, 1.0, 2.0, 5.0, 10.0, 20.0, 60.0 };
		double[] array2 = array;
		foreach (double num in array2)
		{
			if (num * pixelsPerSecond >= 72.0)
			{
				return num;
			}
		}
		return array[^1];
	}

	private static string FormatSeconds(double time)
	{
		if (!(time >= 10.0) && !(Math.Abs(time - Math.Round(time)) < 0.0001))
		{
			return $"{time:0.##}秒";
		}
		return $"{time:0}秒";
	}

	private static string EllipsizeTrackPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "未命名轨道";
		}
		if (path.Length > 27)
		{
			int length = path.Length;
			int num = length - 26;
			return "…" + path.Substring(num, length - num);
		}
		return path;
	}

	private static string GetTrackTypeLabel(Animation.TrackType type)
	{
		Animation.TrackType num = type;
		if ((ulong)num <= 8uL)
		{
			switch ((int)num)
			{
			case 0:
				return "数值 Value";
			case 6:
				return "曲线 Bezier";
			case 5:
				return "方法 Method";
			case 7:
				return "音频 Audio";
			case 8:
				return "动画 Animation";
			case 1:
				return "位置 Position3D";
			case 2:
				return "旋转 Rotation3D";
			case 3:
				return "缩放 Scale3D";
			case 4:
				return "混合形状 BlendShape";
			}
		}
		return type.ToString();
	}

	private static Color GetTrackTypeColor(Animation.TrackType type)
	{
		if ((ulong)type <= 8uL)
		{
			switch ((int)type)
			{
			case 0:
				return new Color("6fc3ff");
			case 6:
				return new Color("b68cff");
			case 5:
				return new Color("ffad63");
			case 7:
				return new Color("55d6af");
			case 8:
				return new Color("ff789e");
			case 1:
				return new Color("69c5df");
			case 2:
				return new Color("df9ddd");
			case 3:
				return new Color("98d36e");
			case 4:
				return new Color("e6ca66");
			}
		}
		return new Color("aab9b0");
	}

	private static string GetInterpolationBadge(Animation.InterpolationType interpolation)
	{
		Animation.InterpolationType num = interpolation;
		if ((ulong)num <= 4uL)
		{
			switch ((int)num)
			{
			case 0:
				return "▥保持";
			case 1:
				return "／线性";
			case 2:
				return "∿平滑";
			case 3:
				return "↻／";
			case 4:
				return "↻∿";
			}
		}
		return interpolation.ToString();
	}

	private static string GetUpdateModeBadge(Animation.UpdateMode updateMode)
	{
		Animation.UpdateMode num = updateMode;
		if ((ulong)num <= 2uL)
		{
			switch ((int)num)
			{
			case 0:
				return "连续";
			case 1:
				return "离散";
			case 2:
				return "捕获";
			}
		}
		return updateMode.ToString();
	}

	private void DrawDiamond(Vector2 center, float radius, Color fill, Color outline, float outlineWidth)
	{
		Vector2[] array = new Vector2[4]
		{
			new Vector2(center.X, center.Y - radius),
			new Vector2(center.X + radius, center.Y),
			new Vector2(center.X, center.Y + radius),
			new Vector2(center.X - radius, center.Y)
		};
		DrawColoredPolygon(array, fill);
		DrawPolyline(new Vector2[5]
		{
			array[0],
			array[1],
			array[2],
			array[3],
			array[0]
		}, outline, outlineWidth, antialiased: true);
	}
}
