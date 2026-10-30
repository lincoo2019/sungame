using Godot;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPNodeConnectionData : RefCounted
{
	public int FromNodeId { get; set; }

	public int FromPortIndex { get; set; }

	public int ToNodeId { get; set; }

	public int ToPortIndex { get; set; }

	public XWBPNodeConnectionData()
	{
	}

	public XWBPNodeConnectionData(int fromNodeId, int fromPortIndex, int toNodeId, int toPortIndex)
	{
		FromNodeId = fromNodeId;
		FromPortIndex = fromPortIndex;
		ToNodeId = toNodeId;
		ToPortIndex = toPortIndex;
	}

	public bool Equals(XWBPNodeConnectionData other)
	{
		if (other == null)
		{
			return false;
		}
		if (FromNodeId == other.FromNodeId && FromPortIndex == other.FromPortIndex && ToNodeId == other.ToNodeId)
		{
			return ToPortIndex == other.ToPortIndex;
		}
		return false;
	}
}
