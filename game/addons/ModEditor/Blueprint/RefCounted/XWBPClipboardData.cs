using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPClipboardData : RefCounted
{
	public List<XWBPNodeData> Nodes { get; set; } = new List<XWBPNodeData>();

	public List<XWBPNodeConnectionData> Connections { get; set; } = new List<XWBPNodeConnectionData>();

	public Vector2 CenterPosition { get; set; } = Vector2.Zero;

	public bool IsEmpty()
	{
		return Nodes.Count == 0;
	}

	public bool HasNodes()
	{
		return Nodes.Count > 0;
	}
}
