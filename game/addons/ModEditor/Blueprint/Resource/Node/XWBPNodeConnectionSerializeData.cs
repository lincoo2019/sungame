using Godot;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPNodeConnectionSerializeData : Resource
{
	[Export(PropertyHint.None, "")]
	public int FromNodeId { get; set; }

	[Export(PropertyHint.None, "")]
	public int FromPortIndex { get; set; }

	[Export(PropertyHint.None, "")]
	public int ToNodeId { get; set; }

	[Export(PropertyHint.None, "")]
	public int ToPortIndex { get; set; }

	public void Serialize(XWBPNodeConnectionData data)
	{
		if (data != null)
		{
			FromNodeId = data.FromNodeId;
			FromPortIndex = data.FromPortIndex;
			ToNodeId = data.ToNodeId;
			ToPortIndex = data.ToPortIndex;
		}
	}

	public XWBPNodeConnectionData Deserialize()
	{
		return new XWBPNodeConnectionData(FromNodeId, FromPortIndex, ToNodeId, ToPortIndex);
	}
}
