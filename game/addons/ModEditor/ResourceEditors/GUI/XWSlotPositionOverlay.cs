using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWSlotPositionOverlay : Control
{
	[Signal]
	public delegate void MarkerPathRequestedEventHandler(NodePath path);

	private SlotComponentDefinition _definition;

	private Node _owner;

	private SlotComponent _runtime;

	private Marker2D _marker;

	private bool _dragging;

	private Vector2 _dragPosition;

	public Vector2 MarkerHandlePosition { get; private set; }

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		base.MouseFilter = MouseFilterEnum.Ignore;
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
	}

	public void Bind(SlotComponentDefinition definition, Node owner, SlotComponent runtime, Marker2D marker)
	{
		_definition = definition;
		_owner = owner;
		_runtime = runtime;
		_marker = marker;
		if (GodotObject.IsInstanceValid(marker))
		{
			Transform2D transform2D = GetGlobalTransformWithCanvas().AffineInverse();
			MarkerHandlePosition = transform2D * marker.GetGlobalTransformWithCanvas().Origin;
			_dragPosition = MarkerHandlePosition;
		}
		else
		{
			MarkerHandlePosition = Vector2.Zero;
			_dragPosition = Vector2.Zero;
			_dragging = false;
		}
		QueueRedraw();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!base.Visible || !GodotObject.IsInstanceValid(_marker))
		{
			return;
		}
		Transform2D transform2D = GetGlobalTransformWithCanvas().AffineInverse();
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			Vector2 vector = transform2D * inputEventMouseButton.Position;
			if (inputEventMouseButton.Pressed)
			{
				_dragging = vector.DistanceTo(MarkerHandlePosition) <= 22f;
				if (_dragging)
				{
					_dragPosition = vector;
					GetViewport()?.SetInputAsHandled();
				}
			}
			else if (_dragging)
			{
				_dragging = false;
				_dragPosition = vector;
				Marker2D marker2D = FindNearestMarker(vector);
				if (GodotObject.IsInstanceValid(marker2D) && GodotObject.IsInstanceValid(_owner))
				{
					EmitSignal(SignalName.MarkerPathRequested, _owner.GetPathTo(marker2D));
				}
				QueueRedraw();
				GetViewport()?.SetInputAsHandled();
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragging)
		{
			_dragPosition = transform2D * inputEventMouseMotion.Position;
			QueueRedraw();
			GetViewport()?.SetInputAsHandled();
		}
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_marker))
		{
			Vector2 vector = (_dragging ? _dragPosition : MarkerHandlePosition);
			Color color = new Color("64cef1");
			DrawLine(new Vector2(10f, vector.Y), new Vector2(Math.Max(20f, base.Size.X - 10f), vector.Y), color, 2f);
			DrawCircle(vector, 13f, new Color("1e6680"));
			DrawCircle(vector, 8f, new Color("8de7ff"));
			DrawLine(vector, vector + new Vector2(0f, 44f), color, 2f);
			DrawOccupantGhost(vector + new Vector2(-42f, -24f), slot: true);
			DrawOccupantGhost(vector + new Vector2(42f, -24f), slot: false);
			Font fallbackFont = ThemeDB.FallbackFont;
			DrawString(fallbackFont, vector + new Vector2(-78f, 62f), "同一目标高度 · 两个独立绑定层", HorizontalAlignment.Left, -1f, 13, new Color("bcecff"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}

	private void DrawOccupantGhost(Vector2 center, bool slot)
	{
		bool flag = (slot ? GodotObject.IsInstanceValid(_runtime?.slotCharacter) : GodotObject.IsInstanceValid(_runtime?.surroundCharacter));
		Color c = (slot ? new Color("55b7e5") : new Color("9a82dc"));
		Rect2 rect = new Rect2(center - new Vector2(24f, 32f), new Vector2(48f, 54f));
		DrawRect(rect, new Color(c, flag ? 0.78f : 0.3f));
		DrawCircle(center - new Vector2(0f, 38f), 15f, new Color(c, flag ? 0.92f : 0.42f));
		SlotComponentDefinition definition = _definition;
		if (definition != null && !definition.hideShadow)
		{
			DrawEllipse(center + new Vector2(0f, 26f), new Vector2(24f, 7f), new Color(0f, 0f, 0f, 0.45f));
		}
		Font fallbackFont = ThemeDB.FallbackFont;
		DrawString(fallbackFont, center + new Vector2(-25f, 45f), slot ? "槽内" : "环绕", HorizontalAlignment.Left, -1f, 12, new Color("e5f6ff"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private void DrawEllipse(Vector2 center, Vector2 radius, Color color)
	{
		Vector2[] array = new Vector2[25];
		for (int i = 0; i < array.Length; i++)
		{
			float s = (float)Math.PI * 2f * (float)i / (float)(array.Length - 1);
			array[i] = center + new Vector2(Mathf.Cos(s) * radius.X, Mathf.Sin(s) * radius.Y);
		}
		DrawPolyline(array, color, 2f);
	}

	private Marker2D FindNearestMarker(Vector2 local)
	{
		Marker2D result = null;
		float num = 70f;
		foreach (Marker2D item in EnumerateMarkers(_owner))
		{
			Vector2 to = GetGlobalTransformWithCanvas().AffineInverse() * item.GetGlobalTransformWithCanvas().Origin;
			float num2 = local.DistanceTo(to);
			if (num2 < num)
			{
				num = num2;
				result = item;
			}
		}
		return result;
	}

	private static IEnumerable<Marker2D> EnumerateMarkers(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			yield break;
		}
		if (root is Marker2D marker2D)
		{
			yield return marker2D;
		}
		foreach (Node child in root.GetChildren())
		{
			foreach (Marker2D item in EnumerateMarkers(child))
			{
				yield return item;
			}
		}
	}
}
