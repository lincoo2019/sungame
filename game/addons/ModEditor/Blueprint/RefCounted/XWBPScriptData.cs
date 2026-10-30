using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPScriptData : RefCounted
{
	[Signal]
	public delegate void GraphRemoveEventHandler(int id);

	[Signal]
	public delegate void FunctionRemoveEventHandler(int id);

	[Signal]
	public delegate void VariableRemoveEventHandler(int id);

	[Signal]
	public delegate void SignalRemoveEventHandler(int id);

	[Signal]
	public delegate void ExtendsClassChangedEventHandler(StringName oldClass, StringName newClass);

	[Signal]
	public delegate void BlueprintChangedEventHandler();

	private StringName _extendsClass = "";

	public StringName ExtendsClass
	{
		get
		{
			return _extendsClass;
		}
		set
		{
			if (_extendsClass != value)
			{
				StringName extendsClass = _extendsClass;
				_extendsClass = value;
				EmitSignal(SignalName.ExtendsClassChanged, extendsClass, value);
				EmitSignal(SignalName.BlueprintChanged);
			}
		}
	}

	public Dictionary<int, XWBPGraphData> Graphs { get; set; } = new Dictionary<int, XWBPGraphData>();

	public Dictionary<int, XWBPFunctionData> Functions { get; set; } = new Dictionary<int, XWBPFunctionData>();

	public Dictionary<int, XWBPVariableData> Variables { get; set; } = new Dictionary<int, XWBPVariableData>();

	public Dictionary<int, XWBPSignalData> SignalDatas { get; set; } = new Dictionary<int, XWBPSignalData>();

	public int NextGraphId { get; set; } = 1;

	public int NextFunctionId { get; set; } = 1;

	public int NextVariableId { get; set; } = 1;

	public int NextSignalId { get; set; } = 1;

	public void AddGraph(XWBPGraphData graphData, int id = 0)
	{
		if (id == 0)
		{
			graphData.Id = NextGraphId;
			Graphs[NextGraphId] = graphData;
			NextGraphId++;
		}
		else
		{
			graphData.Id = id;
			Graphs[id] = graphData;
			if (id >= NextGraphId)
			{
				NextGraphId = id + 1;
			}
		}
		EmitSignal(SignalName.BlueprintChanged);
	}

	public void RemoveGraph(int id)
	{
		EmitSignal(SignalName.GraphRemove, id);
		if (Graphs.TryGetValue(id, out var value))
		{
			value.RemoveSelf();
		}
		Graphs.Remove(id);
		EmitSignal(SignalName.BlueprintChanged);
	}

	public void AddFunction(XWBPFunctionData functionData, int id = 0)
	{
		if (id == 0)
		{
			XWBPNodeData xWBPNodeData = functionData.Nodes[-100000];
			xWBPNodeData.SetMetaData("FunctionId", NextFunctionId);
			xWBPNodeData.SetMetaData("MethodType", 0);
			functionData.Id = NextFunctionId;
			Functions[NextFunctionId] = functionData;
			NextFunctionId++;
		}
		else
		{
			XWBPNodeData xWBPNodeData2 = functionData.Nodes[-100000];
			xWBPNodeData2.SetMetaData("FunctionId", id);
			xWBPNodeData2.SetMetaData("MethodType", 0);
			functionData.Id = id;
			Functions[id] = functionData;
			if (id >= NextFunctionId)
			{
				NextFunctionId = id + 1;
			}
		}
		EmitSignal(SignalName.BlueprintChanged);
	}

	public void RemoveFunction(int id)
	{
		EmitSignal(SignalName.FunctionRemove, id);
		if (Functions.TryGetValue(id, out var value))
		{
			value.RemoveSelf();
		}
		Functions.Remove(id);
		EmitSignal(SignalName.BlueprintChanged);
	}

	public void AddVariable(XWBPVariableData variableData, int id = 0)
	{
		if (id == 0)
		{
			variableData.Id = NextVariableId;
			Variables[NextVariableId] = variableData;
			NextVariableId++;
		}
		else
		{
			variableData.Id = id;
			Variables[id] = variableData;
			if (id >= NextVariableId)
			{
				NextVariableId = id + 1;
			}
		}
		EmitSignal(SignalName.BlueprintChanged);
	}

	public void RemoveVariable(int id)
	{
		EmitSignal(SignalName.VariableRemove, id);
		if (Variables.TryGetValue(id, out var value))
		{
			value.RemoveSelf();
		}
		Variables.Remove(id);
		EmitSignal(SignalName.BlueprintChanged);
	}

	public void AddSignal(XWBPSignalData signalData, int id = 0)
	{
		if (id == 0)
		{
			signalData.Id = NextSignalId;
			SignalDatas[NextSignalId] = signalData;
			NextSignalId++;
		}
		else
		{
			signalData.Id = id;
			SignalDatas[id] = signalData;
			if (id >= NextSignalId)
			{
				NextSignalId = id + 1;
			}
		}
		EmitSignal(SignalName.BlueprintChanged);
	}

	public void RemoveSignal(int id)
	{
		EmitSignal(SignalName.SignalRemove, id);
		if (SignalDatas.TryGetValue(id, out var value))
		{
			value.RemoveSelf();
		}
		SignalDatas.Remove(id);
		EmitSignal(SignalName.BlueprintChanged);
	}

	public string GenerateUniqueGraphName(string baseName = "新图表", string excludeName = "")
	{
		List<string> list = new List<string>();
		foreach (XWBPGraphData value in Graphs.Values)
		{
			list.Add(value.Name);
		}
		return GenerateUniqueName(baseName, list, excludeName);
	}

	public string GenerateUniqueFunctionName(string baseName = "新函数", string excludeName = "")
	{
		List<string> list = new List<string>();
		foreach (XWBPFunctionData value in Functions.Values)
		{
			list.Add(value.Name);
		}
		return GenerateUniqueName(baseName, list, excludeName);
	}

	public string GenerateUniqueVariableName(string baseName = "新变量", string excludeName = "")
	{
		List<string> list = new List<string>();
		foreach (XWBPVariableData value in Variables.Values)
		{
			list.Add(value.Name);
		}
		return GenerateUniqueName(baseName, list, excludeName);
	}

	public string GenerateUniqueSignalName(string baseName = "新信号", string excludeName = "")
	{
		List<string> list = new List<string>();
		foreach (XWBPSignalData value in SignalDatas.Values)
		{
			list.Add(value.Name);
		}
		return GenerateUniqueName(baseName, list, excludeName);
	}

	private static string GenerateUniqueName(string baseName, List<string> existingNames, string excludeName = "")
	{
		if (!string.IsNullOrEmpty(excludeName))
		{
			existingNames.Remove(excludeName);
		}
		if (!existingNames.Contains(baseName))
		{
			return baseName;
		}
		int num = 1;
		string text = baseName + num;
		while (existingNames.Contains(text))
		{
			num++;
			text = baseName + num;
		}
		return text;
	}
}
