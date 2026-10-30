using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Layout;

public partial class XWDockOverlay : Control
{
	public enum Edge
	{
		Left,
		Right,
		Top,
		Bottom,
		None
	}

	private const float EdgeThreshold = 30f;

	private int _highlightZone = -1;

	private Edge _highlightEdge = Edge.None;

	private Rect2 _highlightRect;

	private Rect2 _leftRect;

	private Rect2 _rightRect;

	private Rect2 _centerRect;

	private Rect2 _bottomRect;

	private readonly Dictionary<int, Rect2> _subZones = new Dictionary<int, Rect2>();

	private readonly Dictionary<int, Dictionary<Edge, int>> _edgeAdjacency = new Dictionary<int, Dictionary<Edge, int>>();

	public bool CenterZoneEnabled = true;

	public override void _Ready()
	{
		base.MouseFilter = MouseFilterEnum.Ignore;
		base.AnchorRight = 1f;
		base.AnchorBottom = 1f;
		InitEdgeAdjacency();
	}

	private void InitEdgeAdjacency()
	{
		_edgeAdjacency[4] = new Dictionary<Edge, int>
		{
			{
				Edge.Right,
				6
			},
			{
				Edge.Bottom,
				5
			}
		};
		_edgeAdjacency[5] = new Dictionary<Edge, int>
		{
			{
				Edge.Top,
				4
			},
			{
				Edge.Right,
				7
			},
			{
				Edge.Bottom,
				3
			}
		};
		_edgeAdjacency[6] = new Dictionary<Edge, int>
		{
			{
				Edge.Left,
				4
			},
			{
				Edge.Right,
				2
			},
			{
				Edge.Bottom,
				7
			}
		};
		_edgeAdjacency[7] = new Dictionary<Edge, int>
		{
			{
				Edge.Top,
				6
			},
			{
				Edge.Left,
				5
			},
			{
				Edge.Right,
				2
			},
			{
				Edge.Bottom,
				3
			}
		};
		_edgeAdjacency[8] = new Dictionary<Edge, int>
		{
			{
				Edge.Left,
				2
			},
			{
				Edge.Right,
				10
			},
			{
				Edge.Bottom,
				9
			}
		};
		_edgeAdjacency[9] = new Dictionary<Edge, int>
		{
			{
				Edge.Top,
				8
			},
			{
				Edge.Left,
				2
			},
			{
				Edge.Right,
				11
			},
			{
				Edge.Bottom,
				3
			}
		};
		_edgeAdjacency[10] = new Dictionary<Edge, int>
		{
			{
				Edge.Left,
				8
			},
			{
				Edge.Bottom,
				11
			}
		};
		_edgeAdjacency[11] = new Dictionary<Edge, int>
		{
			{
				Edge.Top,
				10
			},
			{
				Edge.Left,
				9
			},
			{
				Edge.Bottom,
				3
			}
		};
		_edgeAdjacency[2] = new Dictionary<Edge, int>
		{
			{
				Edge.Left,
				6
			},
			{
				Edge.Right,
				8
			},
			{
				Edge.Bottom,
				3
			}
		};
		_edgeAdjacency[3] = new Dictionary<Edge, int>
		{
			{
				Edge.Top,
				2
			},
			{
				Edge.Left,
				4
			},
			{
				Edge.Right,
				8
			}
		};
	}

	public void UpdateZones(Rect2 left, Rect2 right, Rect2 center, Rect2 bottom, Dictionary<int, Rect2> subZones = null)
	{
		_leftRect = left;
		_rightRect = right;
		_centerRect = center;
		_bottomRect = bottom;
		_subZones.Clear();
		if (subZones == null)
		{
			return;
		}
		foreach (KeyValuePair<int, Rect2> subZone in subZones)
		{
			_subZones[subZone.Key] = subZone.Value;
		}
	}

	public void UpdateHighlight(Vector2 globalMousePos)
	{
		Vector2 pos = globalMousePos - base.GlobalPosition;
		var (num, edge, highlightRect) = GetZoneAndEdgeAtPosition(pos);
		if (num != _highlightZone || edge != _highlightEdge)
		{
			_highlightZone = num;
			_highlightEdge = edge;
			_highlightRect = highlightRect;
			QueueRedraw();
		}
	}

	public void ClearHighlight()
	{
		if (_highlightZone != -1)
		{
			_highlightZone = -1;
			_highlightEdge = Edge.None;
			_highlightRect = default(Rect2);
			QueueRedraw();
		}
	}

	public int GetHighlightZone()
	{
		return _highlightZone;
	}

	public override void _Draw()
	{
		if (_highlightZone == -1)
		{
			return;
		}
		Rect2 highlightRect = _highlightRect;
		if (!(highlightRect.Size.X <= 0f) && !(highlightRect.Size.Y <= 0f))
		{
			Color color;
			Color color2;
			Color color3;
			float width;
			float width2;
			if (_highlightEdge != Edge.None)
			{
				color = new Color(0.04f, 0.54f, 1f, 0.18f);
				color2 = new Color(0.04f, 0.54f, 1f, 0.9f);
				color3 = new Color(0.04f, 0.54f, 1f, 0.15f);
				width = 2f;
				width2 = 1f;
			}
			else
			{
				color = new Color(0.04f, 0.54f, 1f, 0.1f);
				color2 = new Color(0.04f, 0.54f, 1f, 0.7f);
				color3 = new Color(0.04f, 0.54f, 1f, 0.1f);
				width = 2f;
				width2 = 1f;
			}
			DrawRect(highlightRect, color);
			Rect2 rect = highlightRect.Grow(-4f);
			if (rect.Size.X > 0f && rect.Size.Y > 0f)
			{
				Color color4 = ((_highlightEdge != Edge.None) ? new Color(0.04f, 0.54f, 1f, 0.08f) : new Color(0.04f, 0.54f, 1f, 0.05f));
				DrawRect(rect, color4);
			}
			DrawRect(highlightRect, color2, filled: false, width);
			Rect2 rect2 = highlightRect.Grow((_highlightEdge != Edge.None) ? 4 : 6);
			DrawRect(rect2, color3, filled: false, width2);
		}
	}

	private (int zone, Edge edge, Rect2 rect) GetZoneAndEdgeAtPosition(Vector2 pos)
	{
		foreach (KeyValuePair<int, Rect2> subZone in _subZones)
		{
			int key = subZone.Key;
			Rect2 value = subZone.Value;
			if (value.HasPoint(pos) && (key != 2 || CenterZoneEnabled))
			{
				var (num, item, item2) = CheckEdges(pos, value, key);
				if (num >= 0)
				{
					return (zone: num, edge: item, rect: item2);
				}
				return (zone: key, edge: Edge.None, rect: value);
			}
		}
		if (_bottomRect.HasPoint(pos))
		{
			var (num2, item3, item4) = CheckEdges(pos, _bottomRect, 3);
			if (num2 >= 0)
			{
				return (zone: num2, edge: item3, rect: item4);
			}
			return (zone: 3, edge: Edge.None, rect: _bottomRect);
		}
		if (CenterZoneEnabled && _centerRect.HasPoint(pos))
		{
			var (num3, item5, item6) = CheckEdges(pos, _centerRect, 2);
			if (num3 >= 0)
			{
				return (zone: num3, edge: item5, rect: item6);
			}
			return (zone: 2, edge: Edge.None, rect: _centerRect);
		}
		return (zone: -1, edge: Edge.None, rect: default(Rect2));
	}

	private (int zone, Edge edge, Rect2 rect) CheckEdges(Vector2 pos, Rect2 zoneRect, int dockPos)
	{
		if (!_edgeAdjacency.TryGetValue(dockPos, out var value))
		{
			return (zone: -1, edge: Edge.None, rect: default(Rect2));
		}
		float num = Mathf.Min(30f, zoneRect.Size.X * 0.3f);
		float num2 = Mathf.Min(30f, zoneRect.Size.Y * 0.3f);
		Edge edge = Edge.None;
		float num3 = 3.4028235E+38f;
		if (value.ContainsKey(Edge.Left))
		{
			float num4 = pos.X - zoneRect.Position.X;
			if (num4 < num && num4 < num3)
			{
				num3 = num4;
				edge = Edge.Left;
			}
		}
		if (value.ContainsKey(Edge.Right))
		{
			float num5 = zoneRect.End.X - pos.X;
			if (num5 < num && num5 < num3)
			{
				num3 = num5;
				edge = Edge.Right;
			}
		}
		if (value.ContainsKey(Edge.Top))
		{
			float num6 = pos.Y - zoneRect.Position.Y;
			if (num6 < num2 && num6 < num3)
			{
				num3 = num6;
				edge = Edge.Top;
			}
		}
		if (value.ContainsKey(Edge.Bottom))
		{
			float num7 = zoneRect.End.Y - pos.Y;
			if (num7 < num2 && num7 < num3)
			{
				num3 = num7;
				edge = Edge.Bottom;
			}
		}
		if (edge != Edge.None)
		{
			int num8 = value[edge];
			if (num8 == 2 && !CenterZoneEnabled)
			{
				return (zone: -1, edge: Edge.None, rect: default(Rect2));
			}
			Rect2 item = ComputeEdgeRect(zoneRect, edge, num, num2);
			return (zone: num8, edge: edge, rect: item);
		}
		return (zone: -1, edge: Edge.None, rect: default(Rect2));
	}

	private static Rect2 ComputeEdgeRect(Rect2 zoneRect, Edge edge, float hThreshold, float vThreshold)
	{
		return edge switch
		{
			Edge.Left => new Rect2(zoneRect.Position.X, zoneRect.Position.Y, hThreshold, zoneRect.Size.Y), 
			Edge.Right => new Rect2(zoneRect.End.X - hThreshold, zoneRect.Position.Y, hThreshold, zoneRect.Size.Y), 
			Edge.Top => new Rect2(zoneRect.Position.X, zoneRect.Position.Y, zoneRect.Size.X, vThreshold), 
			Edge.Bottom => new Rect2(zoneRect.Position.X, zoneRect.End.Y - vThreshold, zoneRect.Size.X, vThreshold), 
			_ => default(Rect2), 
		};
	}
}
