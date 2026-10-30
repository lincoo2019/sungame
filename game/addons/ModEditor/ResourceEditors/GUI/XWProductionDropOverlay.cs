using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWProductionDropOverlay : Node2D
{
	private const float MarkerHitRadius = 18f;

	private readonly List<Vector2> _markerPositions = new List<Vector2>();

	private readonly List<(Marker2D Marker, NodePath Path)> _markerCandidates = new List<(Marker2D, NodePath)>();

	private ProduceComponentDefinition _definition;

	private Node _owner;

	private int _dragMarkerIndex = -1;

	public int MarkerPinCount => _markerPositions.Count;

	public int RedrawRevision { get; private set; }

	public event Action<int, NodePath> MarkerPathRequested;

	public XWProductionDropOverlay()
	{
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		SetProcessInput(enable: false);
	}

	public void Bind(ProduceComponentDefinition definition, Node owner)
	{
		_definition = definition;
		_owner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		RebuildPositions();
		RedrawRevision++;
		QueueRedraw();
	}

	private void RebuildPositions()
	{
		_markerPositions.Clear();
		_markerCandidates.Clear();
		if (GodotObject.IsInstanceValid(_owner))
		{
			foreach (Node item2 in _owner.FindChildren("*", "Marker2D", recursive: true, owned: false))
			{
				if (item2 is Marker2D marker2D && marker2D.IsInsideTree())
				{
					_markerCandidates.Add((marker2D, _owner.GetPathTo(marker2D)));
				}
			}
		}
		int valueOrDefault = (_definition?.markerPaths?.Count).GetValueOrDefault();
		for (int i = 0; i < valueOrDefault; i++)
		{
			Vector2 item = new Vector2(-24 + i * 28, -54f);
			if (GodotObject.IsInstanceValid(_owner))
			{
				Marker2D nodeOrNull = _owner.GetNodeOrNull<Marker2D>(_definition.markerPaths[i]);
				if (GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.IsInsideTree())
				{
					item = ToLocal(nodeOrNull.GlobalPosition);
				}
				else if (_owner is Node2D node2D)
				{
					item = ToLocal(node2D.GlobalPosition) + new Vector2(i * 22, -32f);
				}
			}
			_markerPositions.Add(item);
		}
		if (valueOrDefault == 0 && _owner is Node2D node2D2 && GodotObject.IsInstanceValid(node2D2))
		{
			_markerPositions.Add(ToLocal(node2D2.GlobalPosition));
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!base.Visible || !IsProcessingInput() || (_definition?.markerPaths?.Count).GetValueOrDefault() == 0)
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			Vector2 vector = GetGlobalTransformWithCanvas().AffineInverse() * inputEventMouseButton.Position;
			if (inputEventMouseButton.Pressed)
			{
				_dragMarkerIndex = FindMarker(vector);
				if (_dragMarkerIndex >= 0)
				{
					GetViewport()?.SetInputAsHandled();
				}
			}
			else if (_dragMarkerIndex >= 0)
			{
				int dragMarkerIndex = _dragMarkerIndex;
				_dragMarkerIndex = -1;
				if (!DropMarkerAt(dragMarkerIndex, vector))
				{
					RebuildPositions();
				}
				QueueRedraw();
				GetViewport()?.SetInputAsHandled();
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragMarkerIndex >= 0)
		{
			_markerPositions[_dragMarkerIndex] = GetGlobalTransformWithCanvas().AffineInverse() * inputEventMouseMotion.Position;
			QueueRedraw();
			GetViewport()?.SetInputAsHandled();
		}
	}

	private int FindMarker(Vector2 position)
	{
		for (int num = _markerPositions.Count - 1; num >= 0; num--)
		{
			if (_markerPositions[num].DistanceTo(position) <= 18f)
			{
				return num;
			}
		}
		return -1;
	}

	public bool DropMarkerAt(int index, Vector2 localPosition)
	{
		if (index < 0 || index >= (_definition?.markerPaths?.Count).GetValueOrDefault())
		{
			return false;
		}
		NodePath nodePath = FindNearestMarker(localPosition);
		if (nodePath == null || nodePath.IsEmpty)
		{
			return false;
		}
		_markerPositions[index] = localPosition;
		this.MarkerPathRequested?.Invoke(index, nodePath);
		QueueRedraw();
		return true;
	}

	public Vector2 GetMarkerHandlePosition(int index)
	{
		if (index < 0 || index >= _markerPositions.Count)
		{
			return Vector2.Zero;
		}
		return _markerPositions[index];
	}

	private NodePath FindNearestMarker(Vector2 localPosition)
	{
		float num = 54f;
		NodePath result = null;
		foreach (var (marker2D, nodePath) in _markerCandidates)
		{
			if (GodotObject.IsInstanceValid(marker2D) && marker2D.IsInsideTree())
			{
				float num2 = ToLocal(marker2D.GlobalPosition).DistanceTo(localPosition);
				if (num2 < num)
				{
					num = num2;
					result = nodePath;
				}
			}
		}
		return result;
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			Font fallbackFont = ThemeDB.FallbackFont;
			int num = Math.Max(0, _definition.num);
			int num2 = Math.Max(1, _markerPositions.Count);
			int value = num * num2;
			for (int i = 0; i < _markerPositions.Count; i++)
			{
				Vector2 vector = _markerPositions[i];
				DrawCircle(vector, 13f, new Color(0.95f, 0.78f, 0.2f, 0.28f));
				DrawCircle(vector, 7f, new Color("ffe45c"));
				DrawLine(vector + new Vector2(0f, 8f), vector + new Vector2(0f, 24f), new Color("ffe45c"), 2f);
				DrawString(fallbackFont, vector + new Vector2(12f, -10f), $"{i + 1} × {num}", HorizontalAlignment.Left, -1f, 13, new Color("fff2a8"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
			Vector2 pos = ((_markerPositions.Count > 0) ? (_markerPositions[0] + new Vector2(-30f, 48f)) : new Vector2(-45f, 40f));
			string a = _definition.produceType?.Trim() ?? string.Empty;
			string text = (_definition.onlyEmit ? $"每轮事件总量 {value}" : (string.Equals(a, "Packet", StringComparison.OrdinalIgnoreCase) ? $"每轮最多 {num2} 张" : ((string.Equals(a, "Coin", StringComparison.OrdinalIgnoreCase) && _definition.coinRandom) ? $"每轮 {num2} 枚随机金币" : $"每轮总量 {value}")));
			DrawString(fallbackFont, pos, text, HorizontalAlignment.Left, -1f, 14, new Color("ffe28a"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}
}
