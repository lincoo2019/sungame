using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPFunctionSerializeData : XWBPGraphSerializeData
{
	[Export(PropertyHint.None, "")]
	public int FunctionNodeId { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<XWBPNodePortSerializeData> Inputs { get; set; } = new Array<XWBPNodePortSerializeData>();

	[Export(PropertyHint.None, "")]
	public Array<XWBPNodePortSerializeData> Outputs { get; set; } = new Array<XWBPNodePortSerializeData>();

	[Export(PropertyHint.None, "")]
	public bool StateMachineCallbackEnabled { get; set; }

	[Export(PropertyHint.None, "")]
	public string StateMachineCallbackLocalKey { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public StateMachineCallbackPhase StateMachineCallbackPhase { get; set; }

	public override void Serialize(XWBPGraphData data)
	{
		Clear();
		base.Serialize(data);
		if (!(data is XWBPFunctionData xWBPFunctionData))
		{
			return;
		}
		FunctionNodeId = xWBPFunctionData.FunctionNodeId;
		StateMachineCallbackEnabled = xWBPFunctionData.StateMachineCallbackEnabled;
		StateMachineCallbackLocalKey = xWBPFunctionData.StateMachineCallbackLocalKey ?? "";
		StateMachineCallbackPhase = xWBPFunctionData.StateMachineCallbackPhase;
		foreach (XWBPNodePortData input in xWBPFunctionData.Inputs)
		{
			if (input != null)
			{
				XWBPNodePortSerializeData xWBPNodePortSerializeData = new XWBPNodePortSerializeData();
				xWBPNodePortSerializeData.Serialize(input);
				Inputs.Add(xWBPNodePortSerializeData);
			}
		}
		foreach (XWBPNodePortData output in xWBPFunctionData.Outputs)
		{
			if (output != null)
			{
				XWBPNodePortSerializeData xWBPNodePortSerializeData2 = new XWBPNodePortSerializeData();
				xWBPNodePortSerializeData2.Serialize(output);
				Outputs.Add(xWBPNodePortSerializeData2);
			}
		}
	}

	public override XWBPGraphData Deserialize()
	{
		XWBPFunctionData xWBPFunctionData = new XWBPFunctionData
		{
			Id = base.Id,
			Name = base.Name,
			NextNodeId = base.NextNodeId,
			Lock = base.Lock,
			FunctionNodeId = FunctionNodeId,
			StateMachineCallbackEnabled = StateMachineCallbackEnabled,
			StateMachineCallbackLocalKey = (StateMachineCallbackLocalKey ?? ""),
			StateMachineCallbackPhase = StateMachineCallbackPhase
		};
		int num = 0;
		foreach (XWBPNodeSerializeData nodesDatum in base.NodesData)
		{
			if (nodesDatum == null)
			{
				continue;
			}
			XWBPNodeData xWBPNodeData = nodesDatum.Deserialize();
			if (xWBPNodeData != null && xWBPNodeData.Id != 0 && !xWBPFunctionData.Nodes.ContainsKey(xWBPNodeData.Id))
			{
				xWBPFunctionData.Nodes[xWBPNodeData.Id] = xWBPNodeData;
				if (xWBPNodeData.Id > num)
				{
					num = xWBPNodeData.Id;
				}
			}
		}
		foreach (XWBPNodeConnectionSerializeData connectionsDatum in base.ConnectionsData)
		{
			if (connectionsDatum != null)
			{
				XWBPNodeConnectionData xWBPNodeConnectionData = connectionsDatum.Deserialize();
				if (xWBPNodeConnectionData != null)
				{
					xWBPFunctionData.Connections.Add(xWBPNodeConnectionData);
				}
			}
		}
		if (xWBPFunctionData.NextNodeId <= num)
		{
			xWBPFunctionData.NextNodeId = num + 1;
		}
		if (xWBPFunctionData.NextNodeId < 1)
		{
			xWBPFunctionData.NextNodeId = 1;
		}
		xWBPFunctionData.SanitizeConnections();
		foreach (XWBPNodePortSerializeData input in Inputs)
		{
			if (input != null)
			{
				XWBPNodePortData xWBPNodePortData = input.Deserialize();
				if (xWBPNodePortData != null)
				{
					xWBPFunctionData.Inputs.Add(xWBPNodePortData);
				}
			}
		}
		foreach (XWBPNodePortSerializeData output in Outputs)
		{
			if (output != null)
			{
				XWBPNodePortData xWBPNodePortData2 = output.Deserialize();
				if (xWBPNodePortData2 != null)
				{
					xWBPFunctionData.Outputs.Add(xWBPNodePortData2);
				}
			}
		}
		return xWBPFunctionData;
	}

	public override void Clear()
	{
		base.Clear();
		FunctionNodeId = 0;
		StateMachineCallbackEnabled = false;
		StateMachineCallbackLocalKey = "";
		StateMachineCallbackPhase = StateMachineCallbackPhase.Enter;
		Inputs.Clear();
		Outputs.Clear();
	}
}
