using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPGraphData : RefCounted
{
	[Signal]
	public delegate void RemoveEventHandler();

	[Signal]
	public delegate void RenameEventHandler(string name);

	[Signal]
	public delegate void GraphChangeEventHandler();

	private readonly System.Collections.Generic.Dictionary<int, List<XWBPNodeConnectionData>> _connectionsFrom = new System.Collections.Generic.Dictionary<int, List<XWBPNodeConnectionData>>();

	private readonly System.Collections.Generic.Dictionary<int, List<XWBPNodeConnectionData>> _connectionsTo = new System.Collections.Generic.Dictionary<int, List<XWBPNodeConnectionData>>();

	public XWBPScriptData Owner { get; set; }

	public int Id { get; set; }

	public string Name { get; set; } = "新图表";

	public System.Collections.Generic.Dictionary<int, XWBPNodeData> Nodes { get; set; } = new System.Collections.Generic.Dictionary<int, XWBPNodeData>();

	public List<XWBPNodeConnectionData> Connections { get; set; } = new List<XWBPNodeConnectionData>();

	public int NextNodeId { get; set; } = 1;

	public bool Lock { get; set; }

	public void AddNode(XWBPNodeData nodeData)
	{
		if (nodeData != null)
		{
			if (NextNodeId < 1)
			{
				NextNodeId = 1;
			}
			while (Nodes.ContainsKey(NextNodeId))
			{
				NextNodeId++;
			}
			nodeData.Id = NextNodeId;
			NextNodeId++;
			nodeData.RebuildPortMap();
			Nodes[nodeData.Id] = nodeData;
			EmitSignal(SignalName.GraphChange);
		}
	}

	public void AddNodePreserveId(XWBPNodeData nodeData)
	{
		if (nodeData == null)
		{
			return;
		}
		if (nodeData.Id <= 0 || Nodes.ContainsKey(nodeData.Id))
		{
			AddNode(nodeData);
			return;
		}
		nodeData.RebuildPortMap();
		Nodes[nodeData.Id] = nodeData;
		if (NextNodeId <= nodeData.Id)
		{
			NextNodeId = nodeData.Id + 1;
		}
		EmitSignal(SignalName.GraphChange);
	}

	public void RemoveNode(int nodeId)
	{
		int count = Connections.Count;
		RemoveNodeConnection(nodeId);
		if (Nodes.Remove(nodeId) || count != Connections.Count)
		{
			EmitSignal(SignalName.GraphChange);
		}
	}

	public XWBPNodeData GetNode(int nodeId)
	{
		if (!Nodes.TryGetValue(nodeId, out var value))
		{
			return null;
		}
		return value;
	}

	public bool AddConnection(int fromNodeId, int fromPortIndex, int toNodeId, int toPortIndex)
	{
		if (!CanConnect(fromNodeId, fromPortIndex, toNodeId, toPortIndex))
		{
			return false;
		}
		XWBPNodeConnectionData xWBPNodeConnectionData = new XWBPNodeConnectionData(fromNodeId, fromPortIndex, toNodeId, toPortIndex);
		Connections.Add(xWBPNodeConnectionData);
		AddConnectionIndex(xWBPNodeConnectionData);
		EmitSignal(SignalName.GraphChange);
		return true;
	}

	private void AddConnectionIndex(XWBPNodeConnectionData connection)
	{
		if (connection != null)
		{
			if (!_connectionsFrom.TryGetValue(connection.FromNodeId, out var value))
			{
				value = new List<XWBPNodeConnectionData>();
				_connectionsFrom[connection.FromNodeId] = value;
			}
			value.Add(connection);
			if (!_connectionsTo.TryGetValue(connection.ToNodeId, out var value2))
			{
				value2 = new List<XWBPNodeConnectionData>();
				_connectionsTo[connection.ToNodeId] = value2;
			}
			value2.Add(connection);
		}
	}

	public void RemoveNodeConnection(int nodeId)
	{
		Connections = Connections.Where((XWBPNodeConnectionData c) => c != null && c.FromNodeId != nodeId && c.ToNodeId != nodeId).ToList();
		RebuildConnectionIndex();
	}

	public void RemoveConnection(int fromNodeId, int fromPortIndex, int toNodeId, int toPortIndex)
	{
		XWBPNodeConnectionData target = new XWBPNodeConnectionData(fromNodeId, fromPortIndex, toNodeId, toPortIndex);
		Connections = Connections.Where((XWBPNodeConnectionData c) => c != null && !c.Equals(target)).ToList();
		RebuildConnectionIndex();
		EmitSignal(SignalName.GraphChange);
	}

	public void RebuildConnectionIndex()
	{
		_connectionsFrom.Clear();
		_connectionsTo.Clear();
		foreach (XWBPNodeConnectionData connection in Connections)
		{
			if (connection != null)
			{
				AddConnectionIndex(connection);
			}
		}
	}

	private bool ContainsConnection(XWBPNodeConnectionData target)
	{
		if (target == null)
		{
			return false;
		}
		foreach (XWBPNodeConnectionData connection in Connections)
		{
			if (connection != null && connection.Equals(target))
			{
				return true;
			}
		}
		return false;
	}

	public List<XWBPNodeConnectionData> GetConnectionsToNode(int nodeId)
	{
		if (_connectionsTo.TryGetValue(nodeId, out var value))
		{
			return new List<XWBPNodeConnectionData>(value);
		}
		return new List<XWBPNodeConnectionData>();
	}

	public List<XWBPNodeConnectionData> GetConnectionsFromNode(int nodeId)
	{
		if (_connectionsFrom.TryGetValue(nodeId, out var value))
		{
			return new List<XWBPNodeConnectionData>(value);
		}
		return new List<XWBPNodeConnectionData>();
	}

	public List<XWBPNodeConnectionData> GetInputConnections(int nodeId, int portIndex)
	{
		List<XWBPNodeConnectionData> list = new List<XWBPNodeConnectionData>();
		if (_connectionsTo.TryGetValue(nodeId, out var value))
		{
			foreach (XWBPNodeConnectionData item in value)
			{
				if (item != null && item.ToPortIndex == portIndex)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public List<XWBPNodeConnectionData> GetOutputConnections(int nodeId, int portIndex)
	{
		List<XWBPNodeConnectionData> list = new List<XWBPNodeConnectionData>();
		if (_connectionsFrom.TryGetValue(nodeId, out var value))
		{
			foreach (XWBPNodeConnectionData item in value)
			{
				if (item != null && item.FromPortIndex == portIndex)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public bool CanConnect(int fromNodeId, int fromPortIndex, int toNodeId, int toPortIndex)
	{
		XWBPNodeConnectionData xWBPNodeConnectionData = new XWBPNodeConnectionData(fromNodeId, fromPortIndex, toNodeId, toPortIndex);
		if (ContainsConnection(xWBPNodeConnectionData))
		{
			return false;
		}
		return IsConnectionStructurallyValid(xWBPNodeConnectionData);
	}

	public bool IsConnectionStructurallyValid(XWBPNodeConnectionData connection, bool ignoreOccupiedPorts = false)
	{
		if (connection == null)
		{
			return false;
		}
		if (connection.FromNodeId == connection.ToNodeId)
		{
			return false;
		}
		XWBPNodeData node = GetNode(connection.FromNodeId);
		XWBPNodeData node2 = GetNode(connection.ToNodeId);
		if (node == null || node2 == null)
		{
			return false;
		}
		XWBPNodePortData outputPort = node.GetOutputPort(connection.FromPortIndex);
		XWBPNodePortData inputPort = node2.GetInputPort(connection.ToPortIndex);
		if (outputPort == null || inputPort == null)
		{
			return false;
		}
		if (outputPort.PortDirection != XWBPNodePortData.Direction.Output || inputPort.PortDirection != XWBPNodePortData.Direction.Input)
		{
			return false;
		}
		if (!outputPort.CanConnectTo(inputPort))
		{
			return false;
		}
		if (ignoreOccupiedPorts)
		{
			return true;
		}
		if (GetInputConnections(connection.ToNodeId, connection.ToPortIndex).Count > 0)
		{
			return false;
		}
		if (outputPort.PortTypeValue == XWBPNodePortData.PortType.Flow && GetOutputConnections(connection.FromNodeId, connection.FromPortIndex).Count > 0)
		{
			return false;
		}
		return true;
	}

	public void SanitizeConnections()
	{
		List<XWBPNodeConnectionData> connections = Connections;
		Connections = new List<XWBPNodeConnectionData>();
		_connectionsFrom.Clear();
		_connectionsTo.Clear();
		foreach (XWBPNodeConnectionData item in connections)
		{
			if (IsConnectionStructurallyValid(item) && !ContainsConnection(item))
			{
				Connections.Add(item);
				AddConnectionIndex(item);
			}
		}
	}

	public void Clear()
	{
		Nodes.Clear();
		Connections.Clear();
		_connectionsFrom.Clear();
		_connectionsTo.Clear();
		NextNodeId = 1;
		EmitSignal(SignalName.GraphChange);
	}

	public XWBPNodeData FindEntryNode()
	{
		foreach (XWBPNodeData value in Nodes.Values)
		{
			if (value != null && value.TypeId == (StringName)"__XWBPGraphNode_Entry")
			{
				return value;
			}
		}
		return null;
	}

	public bool HasVirtualMethodEntry(StringName methodName, int excludeNodeId = -1)
	{
		foreach (XWBPNodeData value2 in Nodes.Values)
		{
			if (value2 != null && value2.Id != excludeNodeId && value2.TypeId == (StringName)"__XWBPGraphNode_MethodEntry" && value2.GetMetaData("MethodType").AsInt32() == 2 && value2.GetMetaData("MethodData").As<Dictionary>().TryGetValue("name", out var value) && value.AsStringName() == methodName)
			{
				return true;
			}
		}
		return false;
	}

	public void RemoveSelf()
	{
		EmitSignal(SignalName.Remove);
	}

	public void RenameSelf(string newName)
	{
		Name = newName;
		EmitSignal(SignalName.Rename, newName);
	}

	public void NameSet()
	{
		EmitSignal(SignalName.Rename, Name);
	}

	public virtual List<XWInspectorProperty> GetProperties()
	{
		return new List<XWInspectorProperty>();
	}
}
