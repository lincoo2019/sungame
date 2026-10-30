using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPNodeData : RefCounted
{
	private readonly System.Collections.Generic.Dictionary<string, XWBPNodePortData> _inputPortMap = new System.Collections.Generic.Dictionary<string, XWBPNodePortData>();

	private readonly System.Collections.Generic.Dictionary<string, XWBPNodePortData> _outputPortMap = new System.Collections.Generic.Dictionary<string, XWBPNodePortData>();

	public int Id { get; set; }

	public bool Lock { get; set; }

	public StringName TypeId { get; set; } = "";

	public Vector2 Position { get; set; } = Vector2.Zero;

	public Vector2 Size { get; set; } = Vector2.Zero;

	public List<XWBPNodePortData> InputPorts { get; set; } = new List<XWBPNodePortData>();

	public List<XWBPNodePortData> OutputPorts { get; set; } = new List<XWBPNodePortData>();

	public Dictionary MetaData { get; set; } = new Dictionary();

	public XWBPNodeType NodeType { get; set; }

	public XWBPNodeData()
	{
	}

	public XWBPNodeData(int id, StringName typeId)
	{
		Id = id;
		TypeId = typeId;
	}

	public void RebuildPortMap()
	{
		_inputPortMap.Clear();
		foreach (XWBPNodePortData inputPort in InputPorts)
		{
			if (inputPort != null)
			{
				_inputPortMap[inputPort.Name] = inputPort;
			}
		}
		_outputPortMap.Clear();
		foreach (XWBPNodePortData outputPort in OutputPorts)
		{
			if (outputPort != null)
			{
				_outputPortMap[outputPort.Name] = outputPort;
			}
		}
	}

	public XWBPNodePortData GetInputPort(int index)
	{
		if (index >= 0 && index < InputPorts.Count)
		{
			return InputPorts[index];
		}
		return null;
	}

	public XWBPNodePortData GetOutputPort(int index)
	{
		if (index >= 0 && index < OutputPorts.Count)
		{
			return OutputPorts[index];
		}
		return null;
	}

	public XWBPNodePortData FindInputPortByName(string portName)
	{
		if (_inputPortMap.TryGetValue(portName, out var value))
		{
			return value;
		}
		foreach (XWBPNodePortData inputPort in InputPorts)
		{
			if (inputPort.Name == portName)
			{
				_inputPortMap[portName] = inputPort;
				return inputPort;
			}
		}
		return null;
	}

	public XWBPNodePortData FindOutputPortByName(string portName)
	{
		if (_outputPortMap.TryGetValue(portName, out var value))
		{
			return value;
		}
		foreach (XWBPNodePortData outputPort in OutputPorts)
		{
			if (outputPort.Name == portName)
			{
				_outputPortMap[portName] = outputPort;
				return outputPort;
			}
		}
		return null;
	}

	public void SetInputValue(string portName, Variant val)
	{
		XWBPNodePortData xWBPNodePortData = FindInputPortByName(portName);
		if (xWBPNodePortData != null)
		{
			xWBPNodePortData.Value = val;
		}
	}

	public Variant GetInputValue(string portName)
	{
		return FindInputPortByName(portName)?.Value ?? default(Variant);
	}

	public void SetOutputValue(string portName, Variant val)
	{
		XWBPNodePortData xWBPNodePortData = FindOutputPortByName(portName);
		if (xWBPNodePortData != null)
		{
			xWBPNodePortData.Value = val;
		}
	}

	public Variant GetOutputValue(string portName)
	{
		return FindOutputPortByName(portName)?.Value ?? default(Variant);
	}

	public Variant GetOutputValueByIndex(int index)
	{
		return GetOutputPort(index)?.Value ?? default(Variant);
	}

	public void SetOutputValueByIndex(int index, Variant val)
	{
		XWBPNodePortData outputPort = GetOutputPort(index);
		if (outputPort != null)
		{
			outputPort.Value = val;
		}
	}

	public void SetMetaData(StringName key, Variant value)
	{
		if (MetaData == null)
		{
			Dictionary dictionary = (MetaData = new Dictionary());
		}
		MetaData[key] = value;
	}

	public Variant GetMetaData(StringName key)
	{
		if (MetaData == null || !MetaData.TryGetValue(key, out var value))
		{
			return default(Variant);
		}
		return value;
	}
}
