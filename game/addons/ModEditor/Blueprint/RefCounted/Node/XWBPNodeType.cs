using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public abstract partial class XWBPNodeType : RefCounted
{
	public enum NodeTypeEnum
	{
		General,
		Entry,
		Function,
		FunctionEntry,
		PropertyGet,
		PropertySet
	}

	public StringName TypeId { get; protected set; } = "";

	public NodeTypeEnum NodeTypeEnumValue { get; protected set; }

	public string Category { get; protected set; } = "";

	public string DisplayName { get; protected set; } = "";

	public string Description { get; protected set; } = "";

	public Color Color { get; protected set; } = new Color(0.04f, 0.2f, 0.4f);

	public List<XWBPNodePortData> InputPorts { get; protected set; } = new List<XWBPNodePortData>();

	public List<XWBPNodePortData> OutputPorts { get; protected set; } = new List<XWBPNodePortData>();

	public GodotObject Editor { get; set; }

	public void SetCatalogPresentation(string displayName, string description)
	{
		if (!string.IsNullOrWhiteSpace(displayName))
		{
			DisplayName = displayName;
		}
		Description = description ?? "";
	}

	public virtual XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = new XWBPNodeData
		{
			TypeId = TypeId
		};
		foreach (XWBPNodePortData inputPort in InputPorts)
		{
			if (inputPort != null)
			{
				xWBPNodeData.InputPorts.Add(new XWBPNodePortData(inputPort.Name, inputPort.PortDirection, inputPort.PortTypeValue, inputPort.ClassName, inputPort.DefaultValue));
			}
		}
		foreach (XWBPNodePortData outputPort in OutputPorts)
		{
			if (outputPort != null)
			{
				xWBPNodeData.OutputPorts.Add(new XWBPNodePortData(outputPort.Name, outputPort.PortDirection, outputPort.PortTypeValue, outputPort.ClassName, outputPort.DefaultValue));
			}
		}
		xWBPNodeData.RebuildPortMap();
		return xWBPNodeData;
	}

	public virtual void BuildNodeData(XWBPNodeData nodeData)
	{
		if (nodeData == null)
		{
			return;
		}
		if (nodeData.MetaData == null)
		{
			Dictionary dictionary = (nodeData.MetaData = new Dictionary());
		}
		nodeData.InputPorts.Clear();
		nodeData.OutputPorts.Clear();
		nodeData.MetaData.Clear();
		nodeData.TypeId = TypeId;
		foreach (XWBPNodePortData inputPort in InputPorts)
		{
			if (inputPort != null)
			{
				nodeData.InputPorts.Add(new XWBPNodePortData(inputPort.Name, inputPort.PortDirection, inputPort.PortTypeValue, inputPort.ClassName, inputPort.DefaultValue));
			}
		}
		foreach (XWBPNodePortData outputPort in OutputPorts)
		{
			if (outputPort != null)
			{
				nodeData.OutputPorts.Add(new XWBPNodePortData(outputPort.Name, outputPort.PortDirection, outputPort.PortTypeValue, outputPort.ClassName, outputPort.DefaultValue));
			}
		}
		nodeData.RebuildPortMap();
	}

	public void ClearAllPort()
	{
		ClearInput();
		ClearOutput();
	}

	public void ClearInput()
	{
		InputPorts.Clear();
	}

	public void AddInput(string portName, XWBPNodePortData.PortType portType, StringName className = null, Variant defaultValue = default(Variant))
	{
		InputPorts.Add(new XWBPNodePortData(portName, XWBPNodePortData.Direction.Input, portType, className, defaultValue));
	}

	public void ClearOutput()
	{
		OutputPorts.Clear();
	}

	public void AddOutput(string portName, XWBPNodePortData.PortType portType, StringName className = null, Variant defaultValue = default(Variant))
	{
		OutputPorts.Add(new XWBPNodePortData(portName, XWBPNodePortData.Direction.Output, portType, className, defaultValue));
	}

	public void AddFlowInput(string portName = "执行")
	{
		AddInput(portName, XWBPNodePortData.PortType.Flow);
	}

	public void AddFlowOutput(string portName = "执行")
	{
		AddOutput(portName, XWBPNodePortData.PortType.Flow);
	}

	public virtual void LoadNodeData(XWBPNodeData nodeData)
	{
	}

	public virtual void GraphNodeBuildRefresh(GodotObject graphNode)
	{
	}

	public virtual void GraphNodeInit(GodotObject graphNode)
	{
	}

	public virtual void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		if (!HasFlowOutput(node))
		{
			return;
		}
		throw new InvalidOperationException($"Blueprint node type '{TypeId}' does not implement C# code generation.");
	}

	public virtual void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		if (!HasFlowOutput(node))
		{
			return;
		}
		throw new InvalidOperationException($"Blueprint node type '{TypeId}' does not implement C# graph code generation.");
	}

	public virtual string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (generator == null || node == null)
		{
			return "null";
		}
		if (generator.NodeVariables.TryGetValue(node.Id, out var value))
		{
			return value + "_result";
		}
		XWBPNodePortData xWBPNodePortData = node.FindOutputPortByName(portName);
		if (xWBPNodePortData == null)
		{
			return "null";
		}
		return generator.GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
	}

	public virtual string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (generator == null || node == null)
		{
			return "null";
		}
		if (generator.NodeVariables.TryGetValue(node.Id, out var value))
		{
			return value + "_result";
		}
		XWBPNodePortData xWBPNodePortData = node.FindOutputPortByName(portName);
		if (xWBPNodePortData == null)
		{
			return "null";
		}
		return generator.GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
	}

	private static bool HasFlowOutput(XWBPNodeData node)
	{
		if (node == null)
		{
			return false;
		}
		foreach (XWBPNodePortData outputPort in node.OutputPorts)
		{
			if (outputPort != null && outputPort.PortTypeValue == XWBPNodePortData.PortType.Flow)
			{
				return true;
			}
		}
		return false;
	}
}
