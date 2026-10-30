using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPGraphSerializeData : Resource
{
	[Export(PropertyHint.None, "")]
	public int Id { get; set; }

	[Export(PropertyHint.None, "")]
	public string Name { get; set; } = "图表";

	[Export(PropertyHint.None, "")]
	public int NextNodeId { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public Array<XWBPNodeSerializeData> NodesData { get; set; } = new Array<XWBPNodeSerializeData>();

	[Export(PropertyHint.None, "")]
	public Array<XWBPNodeConnectionSerializeData> ConnectionsData { get; set; } = new Array<XWBPNodeConnectionSerializeData>();

	[Export(PropertyHint.None, "")]
	public bool Lock { get; set; }

	public virtual void Serialize(XWBPGraphData data)
	{
		Clear();
		if (data == null)
		{
			return;
		}
		Id = data.Id;
		Name = data.Name ?? "";
		NextNodeId = data.NextNodeId;
		foreach (XWBPNodeData value in data.Nodes.Values)
		{
			if (value != null)
			{
				XWBPNodeSerializeData xWBPNodeSerializeData = new XWBPNodeSerializeData();
				xWBPNodeSerializeData.Serialize(value);
				NodesData.Add(xWBPNodeSerializeData);
			}
		}
		foreach (XWBPNodeConnectionData connection in data.Connections)
		{
			if (connection != null)
			{
				XWBPNodeConnectionSerializeData xWBPNodeConnectionSerializeData = new XWBPNodeConnectionSerializeData();
				xWBPNodeConnectionSerializeData.Serialize(connection);
				ConnectionsData.Add(xWBPNodeConnectionSerializeData);
			}
		}
		Lock = data.Lock;
	}

	public virtual XWBPGraphData Deserialize()
	{
		XWBPGraphData xWBPGraphData = new XWBPGraphData
		{
			Id = Id,
			Name = Name,
			NextNodeId = NextNodeId,
			Lock = Lock
		};
		int num = 0;
		foreach (XWBPNodeSerializeData nodesDatum in NodesData)
		{
			if (nodesDatum == null)
			{
				continue;
			}
			XWBPNodeData xWBPNodeData = nodesDatum.Deserialize();
			if (xWBPNodeData != null && xWBPNodeData.Id != 0 && !xWBPGraphData.Nodes.ContainsKey(xWBPNodeData.Id))
			{
				xWBPGraphData.Nodes[xWBPNodeData.Id] = xWBPNodeData;
				if (xWBPNodeData.Id > num)
				{
					num = xWBPNodeData.Id;
				}
			}
		}
		foreach (XWBPNodeConnectionSerializeData connectionsDatum in ConnectionsData)
		{
			if (connectionsDatum != null)
			{
				XWBPNodeConnectionData xWBPNodeConnectionData = connectionsDatum.Deserialize();
				if (xWBPNodeConnectionData != null)
				{
					xWBPGraphData.Connections.Add(xWBPNodeConnectionData);
				}
			}
		}
		if (xWBPGraphData.NextNodeId <= num)
		{
			xWBPGraphData.NextNodeId = num + 1;
		}
		if (xWBPGraphData.NextNodeId < 1)
		{
			xWBPGraphData.NextNodeId = 1;
		}
		xWBPGraphData.SanitizeConnections();
		return xWBPGraphData;
	}

	public virtual void Clear()
	{
		NextNodeId = 1;
		NodesData.Clear();
		ConnectionsData.Clear();
	}
}
