using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWFireVolleyOverlay : Node2D
{
	private const float MarkerHitRadius = 18f;

	private readonly List<Vector2> _markerPositions = new List<Vector2>();

	private readonly List<(Marker2D Marker, NodePath Path)> _markerCandidates = new List<(Marker2D, NodePath)>();

	private FireComponentDefinition _definition;

	private Node _owner;

	private int _dragMarkerIndex = -1;

	public int RedrawRevision { get; private set; }

	public int MarkerHandleCount => _markerPositions.Count;

	public int TrajectoryCount => (_definition?.fireProjectileList?.Count).GetValueOrDefault();

	public event Action<int, NodePath> MarkerPathRequested;

	public XWFireVolleyOverlay()
	{
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		SetProcessInput(enable: false);
	}

	public void Bind(FireComponentDefinition definition, Node owner)
	{
		_definition = definition;
		_owner = ((GodotObject.IsInstanceValid(owner) && owner.IsInsideTree()) ? owner : null);
		RebuildMarkerPositions();
		RedrawRevision++;
		QueueRedraw();
	}

	private void RebuildMarkerPositions()
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
		int valueOrDefault = (_definition?.firePosMarkerPaths?.Count).GetValueOrDefault();
		for (int i = 0; i < valueOrDefault; i++)
		{
			Vector2 item = new Vector2(-24 + i * 28, -62f);
			if (GodotObject.IsInstanceValid(_owner))
			{
				Marker2D nodeOrNull = _owner.GetNodeOrNull<Marker2D>(_definition.firePosMarkerPaths[i]);
				if (GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.IsInsideTree())
				{
					item = ToLocal(nodeOrNull.GlobalPosition);
				}
			}
			_markerPositions.Add(item);
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!base.Visible || !IsProcessingInput())
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left)
		{
			Vector2 vector = ToLocal(inputEventMouseButton.Position);
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
					RebuildMarkerPositions();
				}
				QueueRedraw();
				GetViewport()?.SetInputAsHandled();
			}
		}
		else if (inputEvent is InputEventMouseMotion inputEventMouseMotion && _dragMarkerIndex >= 0)
		{
			_markerPositions[_dragMarkerIndex] = ToLocal(inputEventMouseMotion.Position);
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

	private NodePath FindNearestCandidate(Vector2 local)
	{
		float num = 54f;
		NodePath result = null;
		foreach (var (marker2D, nodePath) in _markerCandidates)
		{
			if (GodotObject.IsInstanceValid(marker2D) && marker2D.IsInsideTree())
			{
				float num2 = ToLocal(marker2D.GlobalPosition).DistanceTo(local);
				if (num2 < num)
				{
					num = num2;
					result = nodePath;
				}
			}
		}
		return result;
	}

	public bool DropMarkerAt(int index, Vector2 localPosition)
	{
		if (index < 0 || index >= _markerPositions.Count)
		{
			return false;
		}
		NodePath nodePath = FindNearestCandidate(localPosition);
		if (nodePath == null || nodePath.IsEmpty)
		{
			return false;
		}
		_markerPositions[index] = localPosition;
		this.MarkerPathRequested?.Invoke(index, nodePath);
		QueueRedraw();
		return true;
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			DrawTargetDummies();
			DrawTrajectories();
			DrawMarkers();
		}
	}

	private void DrawTargetDummies()
	{
		Vector2[] array = new Vector2[3]
		{
			new Vector2(165f, -18f),
			new Vector2(235f, -18f),
			new Vector2(305f, -18f)
		};
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = _definition.randomChoose || (_definition.catapultFirstFar ? (i == array.Length - 1) : (i == 0));
			Color color = (flag ? new Color("ffca67") : new Color("5d7588"));
			DrawCircle(array[i], 17f, new Color(color, 0.2f));
			DrawArc(array[i], 17f, 0f, (float)Math.PI * 2f, 24, color, flag ? 3f : 1.5f);
			DrawLine(array[i] + new Vector2(0f, 17f), array[i] + new Vector2(0f, 37f), color, 2f);
		}
		Font fallbackFont = ThemeDB.FallbackFont;
		DrawString(fallbackFont, new Vector2(145f, 40f), _definition.randomChoose ? "随机候选" : (_definition.catapultFirstFar ? "最远目标" : "最近目标"), HorizontalAlignment.Left, 180f, 14, new Color("e1c289"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}

	private void DrawTrajectories()
	{
		int num = _definition.fireProjectileList?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			FireComponentFireProjectileConfig fireComponentFireProjectileConfig = _definition.fireProjectileList[i];
			if (GodotObject.IsInstanceValid(fireComponentFireProjectileConfig))
			{
				Vector2 vector = ((fireComponentFireProjectileConfig.firePosId >= 0 && fireComponentFireProjectileConfig.firePosId < _markerPositions.Count) ? _markerPositions[fireComponentFireProjectileConfig.firePosId] : Vector2.Zero);
				float num2 = Mathf.Clamp(fireComponentFireProjectileConfig.speed * 0.55f, 70f, 330f);
				Vector2 vector2 = Vector2.FromAngle(Mathf.DegToRad(fireComponentFireProjectileConfig.dir));
				Vector2 vector3 = new Vector2(0f, fireComponentFireProjectileConfig.offsetLine * 56);
				Vector2 vector4 = vector + vector2 * num2 + vector3;
				Color color = Color.FromHsv(((float)i * 0.17f + 0.08f) % 1f, 0.62f, 1f);
				for (int j = 0; j < 12; j += 2)
				{
					float weight = (float)j / 12f;
					float weight2 = (float)(j + 1) / 12f;
					DrawLine(vector.Lerp(vector4, weight), vector.Lerp(vector4, weight2), color, 2.5f);
				}
				Vector2 vector5 = vector2.Orthogonal().Normalized();
				DrawLine(vector4, vector4 - vector2 * 15f + vector5 * 7f, color, 2.5f);
				DrawLine(vector4, vector4 - vector2 * 15f - vector5 * 7f, color, 2.5f);
				DrawCircle(vector.Lerp(vector4, 0.48f), 5f, color);
			}
		}
	}

	private void DrawMarkers()
	{
		Font fallbackFont = ThemeDB.FallbackFont;
		for (int i = 0; i < _markerPositions.Count; i++)
		{
			Vector2 vector = _markerPositions[i];
			Color color = ((i == _dragMarkerIndex) ? new Color("fff19a") : new Color("ff9f43"));
			DrawCircle(vector, 12f, new Color(color, 0.28f));
			DrawArc(vector, 12f, 0f, (float)Math.PI * 2f, 24, color, 3f);
			DrawLine(vector + new Vector2(-17f, 0f), vector + new Vector2(17f, 0f), color, 1f);
			DrawLine(vector + new Vector2(0f, -17f), vector + new Vector2(0f, 17f), color, 1f);
			DrawString(fallbackFont, vector + new Vector2(15f, -10f), $"枪口 {i}", HorizontalAlignment.Left, 70f, 13, color, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}
}
