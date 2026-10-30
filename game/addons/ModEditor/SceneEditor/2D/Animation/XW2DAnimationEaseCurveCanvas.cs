using System;
using Godot;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DAnimationEaseCurveCanvas : Control
{
	private const int CurveSampleCount = 48;

	private const float MinimumDragMagnitude = 0.125f;

	private const float MaximumTransitionMagnitude = 1000000f;

	private const float GraphPadding = 14f;

	private const float HandleSample = 0.64f;

	private float _transition = 1f;

	private float _dragStartTransition = 1f;

	private Vector2 _dragStartPosition;

	private bool _editable;

	private bool _dragging;

	public float Transition => _transition;

	public bool Editable => _editable;

	public bool IsDragging => _dragging;

	public int DrawSampleCount => 48;

	public int RedrawRevision { get; private set; }

	public event Action<float> TransitionPreviewChanged;

	public event Action<float, float> TransitionCommitRequested;

	public override void _Ready()
	{
		base.CustomMinimumSize = new Vector2(220f, 132f);
		base.MouseDefaultCursorShape = CursorShape.PointingHand;
		base.MouseFilter = MouseFilterEnum.Stop;
		base.FocusMode = FocusModeEnum.All;
		base.ClipContents = true;
		SetProcess(enable: false);
		QueueCurveRedraw();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 40 && IsNodeReady())
		{
			QueueCurveRedraw();
		}
		else if ((long)what == 31 && !IsVisibleInTree())
		{
			CancelDrag();
		}
	}

	public void SetTransition(float transition)
	{
		float num = NormalizeTransition(transition);
		if (!Mathf.IsEqualApprox(_transition, num))
		{
			_transition = num;
			QueueCurveRedraw();
		}
	}

	public void SetEditable(bool editable)
	{
		if (_editable != editable)
		{
			_editable = editable;
			if (!editable)
			{
				CancelDrag();
			}
			base.MouseDefaultCursorShape = (CursorShape)(editable ? 2 : 8);
			QueueCurveRedraw();
		}
	}

	public void CancelPendingDrag()
	{
		CancelDrag();
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (!_editable)
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			if (inputEventMouseButton.Pressed)
			{
				BeginDrag(inputEventMouseButton.Position);
			}
			else
			{
				FinishDrag();
			}
			AcceptEvent();
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragging)
		{
			UpdateDrag(inputEventMouseMotion.Position);
			AcceptEvent();
		}
	}

	public override void _Draw()
	{
		Rect2 graphRect = GetGraphRect();
		Color color = (_editable ? new Color("13241d") : new Color("151c19"));
		Color color2 = (_editable ? new Color("5d8f5b") : new Color("3e4b43"));
		DrawRect(new Rect2(Vector2.Zero, base.Size), color);
		DrawRect(new Rect2(Vector2.Zero, base.Size), color2, filled: false, 1.5f);
		for (int i = 0; i <= 4; i++)
		{
			float weight = (float)i / 4f;
			float x = Mathf.Lerp(graphRect.Position.X, graphRect.End.X, weight);
			float y = Mathf.Lerp(graphRect.End.Y, graphRect.Position.Y, weight);
			DrawLine(new Vector2(x, graphRect.Position.Y), new Vector2(x, graphRect.End.Y), new Color("38504666"), 1f);
			DrawLine(new Vector2(graphRect.Position.X, y), new Vector2(graphRect.End.X, y), new Color("38504666"), 1f);
		}
		DrawLine(new Vector2(graphRect.Position.X, graphRect.End.Y), new Vector2(graphRect.End.X, graphRect.Position.Y), new Color("6e827577"), 1f);
		Vector2[] array = new Vector2[49];
		for (int j = 0; j <= 48; j++)
		{
			float x2 = (float)j / 48f;
			float y2 = Evaluate(x2, _transition);
			array[j] = GraphPoint(graphRect, x2, y2);
		}
		DrawPolyline(array, _editable ? new Color("f1c94f") : new Color("718078"), 2.5f, antialiased: true);
		Vector2 position = GraphPoint(graphRect, 0.64f, Evaluate(0.64f, _transition));
		DrawCircle(position, _dragging ? 7.5f : 6f, _editable ? new Color("ffdd69") : new Color("68736d"));
		DrawCircle(position, _dragging ? 7.5f : 6f, Colors.White, filled: false, 1.5f, antialiased: true);
		Font themeDefaultFont = GetThemeDefaultFont();
		int fontSize = Math.Max(10, GetThemeDefaultFontSize() - 2);
		DrawString(themeDefaultFont, new Vector2(graphRect.Position.X, base.Size.Y - 3f), $"曲率 {_transition:0.###} · 拖动金色节点", HorizontalAlignment.Left, graphRect.Size.X, fontSize, _editable ? new Color("dcebd6") : new Color("7f8b84"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private void BeginDrag(Vector2 position)
	{
		_dragging = true;
		_dragStartPosition = position;
		_dragStartTransition = _transition;
		GrabFocus();
		QueueCurveRedraw();
	}

	private void UpdateDrag(Vector2 position)
	{
		float num = Math.Max(48f, GetGraphRect().Size.Y);
		float y = (_dragStartPosition.Y - position.Y) / num * 3.2f;
		float num2 = Math.Clamp(Math.Clamp(Math.Abs(_dragStartTransition), 0.125f, 1000000f) * Mathf.Pow(2f, y), 1E-06f, 1000000f);
		float num3 = NormalizeTransition(((_dragStartTransition < 0f) ? (-1f) : 1f) * num2);
		if (!Mathf.IsEqualApprox(num3, _transition))
		{
			_transition = num3;
			this.TransitionPreviewChanged?.Invoke(_transition);
			QueueCurveRedraw();
		}
	}

	private void FinishDrag()
	{
		if (_dragging)
		{
			_dragging = false;
			float dragStartTransition = _dragStartTransition;
			float transition = _transition;
			QueueCurveRedraw();
			if (!Mathf.IsEqualApprox(dragStartTransition, transition))
			{
				this.TransitionCommitRequested?.Invoke(dragStartTransition, transition);
			}
		}
	}

	private void CancelDrag()
	{
		if (_dragging)
		{
			float transition = _transition;
			_dragging = false;
			_transition = _dragStartTransition;
			if (!Mathf.IsEqualApprox(transition, _transition))
			{
				this.TransitionPreviewChanged?.Invoke(_transition);
			}
			QueueCurveRedraw();
		}
	}

	private void QueueCurveRedraw()
	{
		RedrawRevision++;
		QueueRedraw();
	}

	private Rect2 GetGraphRect()
	{
		float width = Math.Max(1f, base.Size.X - 28f);
		float height = Math.Max(1f, base.Size.Y - 28f - 16f);
		return new Rect2(14f, 14f, width, height);
	}

	private static Vector2 GraphPoint(Rect2 graph, float x, float y)
	{
		return new Vector2(Mathf.Lerp(graph.Position.X, graph.End.X, Mathf.Clamp(x, 0f, 1f)), Mathf.Lerp(graph.End.Y, graph.Position.Y, Mathf.Clamp(y, 0f, 1f)));
	}

	private static float Evaluate(float x, float transition)
	{
		float num = Mathf.Ease(Mathf.Clamp(x, 0f, 1f), transition);
		if (!float.IsFinite(num))
		{
			return x;
		}
		return Mathf.Clamp(num, 0f, 1f);
	}

	private static float NormalizeTransition(float transition)
	{
		if (!float.IsFinite(transition))
		{
			return 1f;
		}
		float num = ((transition < 0f) ? (-1f) : 1f);
		float num2 = Math.Min(Math.Abs(transition), 1000000f);
		if (transition != 0f)
		{
			return num * num2;
		}
		return 0f;
	}
}
