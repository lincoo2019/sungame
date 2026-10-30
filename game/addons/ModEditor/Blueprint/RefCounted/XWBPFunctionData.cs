using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPFunctionData : XWBPGraphData
{
	[Signal]
	public delegate void PortChangeEventHandler();

	public const int EntryNodeId = -100000;

	public int FunctionNodeId { get; set; }

	public List<XWBPNodePortData> Inputs { get; set; } = new List<XWBPNodePortData>();

	public List<XWBPNodePortData> Outputs { get; set; } = new List<XWBPNodePortData>();

	public int PreviousOutputCount { get; set; }

	public bool StateMachineCallbackEnabled { get; set; }

	public string StateMachineCallbackLocalKey { get; set; } = "";

	public StateMachineCallbackPhase StateMachineCallbackPhase { get; set; }

	public static XWBPFunctionData Create()
	{
		XWBPFunctionData xWBPFunctionData = new XWBPFunctionData();
		XWBPNodeMethodEntry xWBPNodeMethodEntry = new XWBPNodeMethodEntry
		{
			MethodType = XWBPNodeMethodEntry.Type.Bp
		};
		XWBPNodeData xWBPNodeData = xWBPNodeMethodEntry.CreateNodeData();
		xWBPNodeData.NodeType = xWBPNodeMethodEntry;
		xWBPNodeData.Id = -100000;
		xWBPNodeData.Lock = true;
		xWBPFunctionData.Nodes[-100000] = xWBPNodeData;
		return xWBPFunctionData;
	}

	public XWBPFunctionData Duplicate()
	{
		XWBPFunctionSerializeData xWBPFunctionSerializeData = new XWBPFunctionSerializeData();
		xWBPFunctionSerializeData.Serialize(this);
		return (XWBPFunctionData)xWBPFunctionSerializeData.Deserialize();
	}

	public override List<XWInspectorProperty> GetProperties()
	{
		List<XWInspectorProperty> properties = base.GetProperties();
		properties.Add(new XWInspectorProperty(this, "inputs", default(Variant), "函数", "", PropertyHint.None, "", Callable.From(InputsSet)));
		properties.Add(new XWInspectorProperty(this, "outputs", default(Variant), "函数", "", PropertyHint.None, "", Callable.From(OutputsSet)));
		return properties;
	}

	public void InputsSet()
	{
		foreach (XWBPNodePortData input in Inputs)
		{
			input.PortDirection = XWBPNodePortData.Direction.Input;
		}
		EnsureUniquePortNames(Inputs);
		PortChangeEmit();
	}

	public void OutputsSet()
	{
		foreach (XWBPNodePortData output in Outputs)
		{
			output.PortDirection = XWBPNodePortData.Direction.Output;
		}
		while (Outputs.Count > 1)
		{
			Outputs.RemoveAt(Outputs.Count - 1);
		}
		EnsureUniquePortNames(Outputs);
		if (PreviousOutputCount == 0 && Outputs.Count > 0)
		{
			AutoAddReturnNode();
		}
		else if (PreviousOutputCount > 0 && Outputs.Count == 0)
		{
			AutoRemoveReturnNodes();
		}
		PreviousOutputCount = Outputs.Count;
		PortChangeEmit();
	}

	public void AutoAddReturnNode()
	{
		bool flag = false;
		foreach (XWBPNodeData value2 in base.Nodes.Values)
		{
			if (value2.TypeId == (StringName)"__XWBPGraphNode_Return")
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			XWBPNodeData value;
			XWBPNodeData xWBPNodeData = (base.Nodes.TryGetValue(-100000, out value) ? value : null);
			Vector2 position = new Vector2(400f, 0f);
			if (xWBPNodeData != null)
			{
				position = xWBPNodeData.Position + new Vector2(800f, 0f);
			}
			XWBPNodeData xWBPNodeData2 = new XWBPNodeReturn().CreateNodeData();
			xWBPNodeData2.Position = position;
			AddNode(xWBPNodeData2);
		}
	}

	public void AutoRemoveReturnNodes()
	{
		List<int> list = new List<int>();
		foreach (XWBPNodeData value in base.Nodes.Values)
		{
			if (value.TypeId == (StringName)"__XWBPGraphNode_Return")
			{
				list.Add(value.Id);
			}
		}
		foreach (int item in list)
		{
			RemoveNode(item);
		}
	}

	public static void EnsureUniquePortNames(List<XWBPNodePortData> ports)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < ports.Count; i++)
		{
			XWBPNodePortData xWBPNodePortData = ports[i];
			string name = xWBPNodePortData.Name;
			if (dictionary.TryGetValue(name, out var value))
			{
				string text = name + value;
				while (dictionary.ContainsKey(text))
				{
					value++;
					text = name + value;
				}
				xWBPNodePortData.Name = text;
				dictionary[name] = value + 1;
				dictionary[text] = 1;
			}
			else
			{
				dictionary[name] = 1;
			}
		}
	}

	public void PortChangeEmit()
	{
		EmitSignal(SignalName.PortChange);
	}
}
