using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed partial class XWExplosionStageOverlay : Node2D
{
	private enum DragKind
	{
		None,
		Range,
		Offset
	}

	private static readonly Vector2 GridSize = new Vector2(80f, 98f);

	private const float HandleHitRadius = 22f;

	private ExplodeComponentDefinition _definition;

	private Node _owner;

	private Action _rangeEditStarted;

	private Action<Vector2> _rangeEditPreviewed;

	private Action<Vector2> _rangeEditCommitted;

	private Action<int> _offsetEditStarted;

	private Action<int, int> _offsetEditPreviewed;

	private Action<int, int> _offsetEditCommitted;

	private DragKind _dragKind;

	private int _dragOffsetIndex = -1;

	public int RedrawRevision { get; private set; }

	public string LastDrawnMethod { get; private set; } = string.Empty;

	public int LastDrawnOffsetCount { get; private set; }

	public Rect2 LastRangeRect { get; private set; }

	public Vector2 LastEffectPosition { get; private set; }

	public Vector2 LastRangeHandlePosition { get; private set; }

	public XWExplosionStageOverlay()
	{
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		SetProcessInput(enable: false);
	}

	public void Bind(ExplodeComponentDefinition definition, Node owner)
	{
		_definition = definition;
		_owner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		RedrawRevision++;
		QueueRedraw();
	}

	public void ConfigureEditing(Action rangeEditStarted, Action<Vector2> rangeEditPreviewed, Action<Vector2> rangeEditCommitted, Action<int> offsetEditStarted, Action<int, int> offsetEditPreviewed, Action<int, int> offsetEditCommitted)
	{
		_rangeEditStarted = rangeEditStarted;
		_rangeEditPreviewed = rangeEditPreviewed;
		_rangeEditCommitted = rangeEditCommitted;
		_offsetEditStarted = offsetEditStarted;
		_offsetEditPreviewed = offsetEditPreviewed;
		_offsetEditCommitted = offsetEditCommitted;
	}

	public Vector2 GetOffsetHandlePosition(int index)
	{
		if (index < 0 || index >= (_definition?.explodeJalaOffset?.Count).GetValueOrDefault())
		{
			return ResolveOwnerCenter();
		}
		return ResolveOwnerCenter() + new Vector2((0f - GridSize.X) * 4.55f, (float)_definition.explodeJalaOffset[index] * GridSize.Y);
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!base.Visible || base.ProcessMode == ProcessModeEnum.Disabled || !IsProcessingInput() || !GodotObject.IsInstanceValid(_definition))
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			Vector2 local = ToOverlayPosition(inputEventMouseButton.Position);
			if (inputEventMouseButton.Pressed)
			{
				if (TryBeginDrag(local))
				{
					GetViewport()?.SetInputAsHandled();
				}
			}
			else if (_dragKind != DragKind.None)
			{
				CommitDrag(local);
				GetViewport()?.SetInputAsHandled();
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragKind != DragKind.None)
		{
			PreviewDrag(ToOverlayPosition(inputEventMouseMotion.Position));
			GetViewport()?.SetInputAsHandled();
		}
	}

	private bool TryBeginDrag(Vector2 local)
	{
		if (string.Equals(_definition.explodeMethod, "Range", StringComparison.Ordinal))
		{
			Vector2 vector = ResolveOwnerCenter();
			Vector2 vector2 = new Vector2(Mathf.Abs(_definition.explodeRange.X) * GridSize.X, Mathf.Abs(_definition.explodeRange.Y) * GridSize.Y);
			LastRangeHandlePosition = vector + vector2;
			if (local.DistanceTo(LastRangeHandlePosition) <= 22f)
			{
				_dragKind = DragKind.Range;
				_rangeEditStarted?.Invoke();
				return true;
			}
			return false;
		}
		if (!XWExplosionStagePresenter.IsSupportedMethod(_definition.explodeMethod))
		{
			return false;
		}
		for (int i = 0; i < (_definition.explodeJalaOffset?.Count ?? 0); i++)
		{
			if (!(local.DistanceTo(GetOffsetHandlePosition(i)) > 22f))
			{
				_dragKind = DragKind.Offset;
				_dragOffsetIndex = i;
				_offsetEditStarted?.Invoke(i);
				return true;
			}
		}
		return false;
	}

	private void PreviewDrag(Vector2 local)
	{
		if (_dragKind == DragKind.Range)
		{
			_rangeEditPreviewed?.Invoke(SnapRange(local));
		}
		else if (_dragKind == DragKind.Offset && _dragOffsetIndex >= 0)
		{
			_offsetEditPreviewed?.Invoke(_dragOffsetIndex, SnapOffset(local));
		}
	}

	private void CommitDrag(Vector2 local)
	{
		DragKind dragKind = _dragKind;
		int dragOffsetIndex = _dragOffsetIndex;
		_dragKind = DragKind.None;
		_dragOffsetIndex = -1;
		switch (dragKind)
		{
		case DragKind.Range:
		{
			Vector2 obj = SnapRange(local);
			_rangeEditPreviewed?.Invoke(obj);
			_rangeEditCommitted?.Invoke(obj);
			break;
		}
		case DragKind.Offset:
			if (dragOffsetIndex >= 0)
			{
				int arg = SnapOffset(local);
				_offsetEditPreviewed?.Invoke(dragOffsetIndex, arg);
				_offsetEditCommitted?.Invoke(dragOffsetIndex, arg);
			}
			break;
		}
	}

	private Vector2 SnapRange(Vector2 local)
	{
		Vector2 vector = local - ResolveOwnerCenter();
		return new Vector2(Mathf.Clamp((float)Math.Round(Mathf.Abs(vector.X) / GridSize.X), 0f, 64f), Mathf.Clamp((float)Math.Round(Mathf.Abs(vector.Y) / GridSize.Y), 0f, 64f));
	}

	private int SnapOffset(Vector2 local)
	{
		return Math.Clamp((int)Math.Round((local.Y - ResolveOwnerCenter().Y) / GridSize.Y), -64, 64);
	}

	private Vector2 ToOverlayPosition(Vector2 viewportPosition)
	{
		return GetGlobalTransformWithCanvas().AffineInverse() * viewportPosition;
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			Vector2 center = ResolveOwnerCenter();
			LastDrawnMethod = _definition.explodeMethod ?? string.Empty;
			LastDrawnOffsetCount = _definition.explodeJalaOffset?.Count ?? 0;
			DrawGrid(center);
			switch (LastDrawnMethod)
			{
			case "Range":
				DrawRange(center);
				break;
			case "Line":
				DrawLineMode(center);
				break;
			case "Row":
				DrawRowMode(center);
				break;
			case "Cross":
				DrawCrossMode(center);
				break;
			case "Slash":
				DrawSlashMode(center);
				break;
			default:
				DrawUnknown(center);
				break;
			}
			if (XWExplosionStagePresenter.IsSupportedMethod(LastDrawnMethod) && !string.Equals(LastDrawnMethod, "Range", StringComparison.Ordinal))
			{
				DrawOffsetHandles();
			}
			DrawPresentationAnchors(center);
		}
	}

	private void DrawOffsetHandles()
	{
		for (int i = 0; i < (_definition.explodeJalaOffset?.Count ?? 0); i++)
		{
			Vector2 offsetHandlePosition = GetOffsetHandlePosition(i);
			DrawCircle(offsetHandlePosition, 11f, new Color("fff1c7"));
			DrawCircle(offsetHandlePosition, 6f, new Color("ff704d"));
			DrawCaption(offsetHandlePosition + new Vector2(14f, 4f), $"#{i + 1}", new Color("fff0c8"));
		}
	}

	private Vector2 ResolveOwnerCenter()
	{
		if (_owner is Node2D node2D && node2D.IsInsideTree())
		{
			return ToLocal(node2D.GlobalPosition);
		}
		return Vector2.Zero;
	}

	private void DrawGrid(Vector2 center)
	{
		for (int i = -5; i <= 5; i++)
		{
			float x = center.X + (float)i * GridSize.X;
			DrawLine(new Vector2(x, center.Y - GridSize.Y * 3.2f), new Vector2(x, center.Y + GridSize.Y * 3.2f), new Color("526273", (i == 0) ? 0.4f : 0.16f), (i == 0) ? 1.5f : 1f);
		}
		for (int j = -3; j <= 3; j++)
		{
			float y = center.Y + (float)j * GridSize.Y;
			DrawLine(new Vector2(center.X - GridSize.X * 5.2f, y), new Vector2(center.X + GridSize.X * 5.2f, y), new Color("526273", (j == 0) ? 0.4f : 0.16f), (j == 0) ? 1.5f : 1f);
		}
	}

	private void DrawRange(Vector2 center)
	{
		Vector2 vector = new Vector2(Mathf.Abs(_definition.explodeRange.X) * GridSize.X, Mathf.Abs(_definition.explodeRange.Y) * GridSize.Y);
		LastRangeRect = new Rect2(center - vector, vector * 2f);
		LastRangeHandlePosition = center + vector;
		DrawRect(LastRangeRect, new Color("ff694d", 0.18f));
		DrawRect(LastRangeRect, new Color("ff9c68"), filled: false, 3f);
		DrawCircle(center, 7f, new Color("ffe2a6"));
		DrawCircle(LastRangeHandlePosition, 11f, new Color("fff1c7"));
		DrawCircle(LastRangeHandlePosition, 6f, new Color("ff704d"));
		DrawCaption(center + new Vector2(-120f, 0f - vector.Y - 12f), $"Range  {Mathf.Abs(_definition.explodeRange.X):0.##} × {Mathf.Abs(_definition.explodeRange.Y):0.##} 格", new Color("ffc18d"));
	}

	private void DrawLineMode(Vector2 center)
	{
		foreach (int offset in GetOffsets())
		{
			float y = center.Y + (float)offset * GridSize.Y;
			DrawDirectionalLine(new Vector2(center.X - GridSize.X * 5f, y), new Vector2(center.X + GridSize.X * 5f, y), $"Y {offset:+#;-#;0}");
		}
	}

	private void DrawRowMode(Vector2 center)
	{
		DrawDirectionalLine(new Vector2(center.X, center.Y - GridSize.Y * 3f), new Vector2(center.X, center.Y + GridSize.Y * 3f), "Row / 当前列");
		foreach (int offset in GetOffsets())
		{
			Vector2 vector = center + new Vector2(0f, (float)offset * GridSize.Y);
			DrawCircle(vector, 6f, new Color("ffd06d"));
			DrawCaption(vector + new Vector2(10f, -8f), $"起点 Y {offset:+#;-#;0}", new Color("ffdca0"));
		}
	}

	private void DrawCrossMode(Vector2 center)
	{
		DrawDirectionalLine(new Vector2(center.X, center.Y - GridSize.Y * 3f), new Vector2(center.X, center.Y + GridSize.Y * 3f), "当前列");
		foreach (int offset in GetOffsets())
		{
			float y = center.Y + (float)offset * GridSize.Y;
			DrawDirectionalLine(new Vector2(center.X - GridSize.X * 5f, y), new Vector2(center.X + GridSize.X * 5f, y), $"Cross Y {offset:+#;-#;0}");
		}
	}

	private void DrawSlashMode(Vector2 center)
	{
		foreach (int offset in GetOffsets())
		{
			Vector2 vector = center + new Vector2(0f, (float)offset * GridSize.Y);
			DrawDirectionalLine(vector + new Vector2((0f - GridSize.X) * 3.5f, GridSize.Y * 3f), vector + new Vector2(GridSize.X * 3.5f, (0f - GridSize.Y) * 3f), $"Slash Y {offset:+#;-#;0}");
		}
	}

	private IEnumerable<int> GetOffsets()
	{
		if (_definition.explodeJalaOffset == null)
		{
			yield break;
		}
		foreach (int item in _definition.explodeJalaOffset)
		{
			yield return item;
		}
	}

	private void DrawDirectionalLine(Vector2 from, Vector2 to, string label)
	{
		Color color = new Color("ff784f", 0.92f);
		DrawLine(from, to, color, 5f);
		Vector2 vector = (to - from).Normalized();
		Vector2 vector2 = new Vector2(0f - vector.Y, vector.X);
		DrawColoredPolygon(new Vector2[3]
		{
			to,
			to - vector * 18f + vector2 * 9f,
			to - vector * 18f - vector2 * 9f
		}, color);
		DrawCaption(from + new Vector2(8f, -10f), label, new Color("ffc092"));
	}

	private void DrawUnknown(Vector2 center)
	{
		DrawCircle(center, 42f, new Color("b72727", 0.42f));
		DrawLine(center + new Vector2(-28f, -28f), center + new Vector2(28f, 28f), new Color("ff6464"), 6f);
		DrawLine(center + new Vector2(28f, -28f), center + new Vector2(-28f, 28f), new Color("ff6464"), 6f);
		DrawCaption(center + new Vector2(-90f, -58f), "未知爆炸方法", new Color("ff7b70"));
	}

	private void DrawPresentationAnchors(Vector2 center)
	{
		LastEffectPosition = ResolveEffectPosition(center);
		DrawLine(center, LastEffectPosition, new Color("d98cff"), 2f);
		DrawCircle(LastEffectPosition, 9f, new Color("d98cff", 0.7f));
		DrawCaption(LastEffectPosition + new Vector2(12f, -8f), "Effect  -30Y", new Color("e6b6ff"));
		if (_definition.craterCreateUse)
		{
			Rect2 rect = new Rect2(center - GridSize * 0.36f, GridSize * 0.72f);
			DrawRect(rect, new Color("5c351f", 0.52f));
			DrawRect(rect, new Color("d08a55"), filled: false, 2f);
			DrawCaption(rect.End + new Vector2(5f, -5f), "Crater", new Color("dca071"));
		}
		if (_definition.cameraShakeUse)
		{
			Vector2 vector = new Vector2(Mathf.Abs(_definition.cameraShakeOffset.X), Mathf.Abs(_definition.cameraShakeOffset.Y));
			Rect2 rect2 = new Rect2(center - vector, vector * 2f);
			DrawRect(rect2, new Color("72b9ff", 0.16f));
			DrawRect(rect2, new Color("72b9ff"), filled: false, 1.5f);
		}
		if (_definition.screenColorBlinkUse)
		{
			Rect2 rect3 = new Rect2(center - new Vector2(GridSize.X * 5.25f, GridSize.Y * 3.25f), new Vector2(GridSize.X * 10.5f, GridSize.Y * 6.5f));
			Color screenColorBlinkColor = _definition.screenColorBlinkColor;
			screenColorBlinkColor.A = Mathf.Clamp(screenColorBlinkColor.A, 0.18f, 0.56f);
			DrawRect(rect3, screenColorBlinkColor, filled: false, 5f);
		}
	}

	private Vector2 ResolveEffectPosition(Vector2 fallbackCenter)
	{
		if (_owner is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter.transformPoint) && towerDefenseCharacter.transformPoint.IsInsideTree())
		{
			return ToLocal(towerDefenseCharacter.transformPoint.GlobalPosition - new Vector2(0f, 30f));
		}
		return fallbackCenter - new Vector2(0f, 30f);
	}

	private void DrawCaption(Vector2 position, string text, Color color)
	{
		DrawString(ThemeDB.FallbackFont, position, text, HorizontalAlignment.Left, -1f, 12, color, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}
}
