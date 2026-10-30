using System;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPScript : Resource
{
	[Export(PropertyHint.None, "")]
	public StringName ExtendsClass { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Array<XWBPGraphSerializeData> Graphs { get; set; } = new Array<XWBPGraphSerializeData>();

	[Export(PropertyHint.None, "")]
	public Array<XWBPFunctionSerializeData> Functions { get; set; } = new Array<XWBPFunctionSerializeData>();

	[Export(PropertyHint.None, "")]
	public Array<XWBPVariableSerializeData> Variables { get; set; } = new Array<XWBPVariableSerializeData>();

	[Export(PropertyHint.None, "")]
	public Array<XWBPSignalSerializeData> Signals { get; set; } = new Array<XWBPSignalSerializeData>();

	[Export(PropertyHint.None, "")]
	public int NextGraphId { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public int NextFunctionId { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public int NextVariableId { get; set; } = 1;

	[Export(PropertyHint.None, "")]
	public int NextSignalId { get; set; } = 1;

	public static XWBPScript Create()
	{
		XWBPScript xWBPScript = new XWBPScript();
		XWBPGraphSerializeData item = new XWBPGraphSerializeData
		{
			Id = 1,
			Lock = true
		};
		xWBPScript.Graphs.Add(item);
		xWBPScript.NextGraphId = 2;
		return xWBPScript;
	}

	public void Serialize(XWBPScriptData data)
	{
		Clear();
		ExtendsClass = data.ExtendsClass;
		foreach (XWBPGraphData value in data.Graphs.Values)
		{
			XWBPGraphSerializeData xWBPGraphSerializeData = new XWBPGraphSerializeData();
			xWBPGraphSerializeData.Serialize(value);
			Graphs.Add(xWBPGraphSerializeData);
		}
		foreach (XWBPFunctionData value2 in data.Functions.Values)
		{
			XWBPFunctionSerializeData xWBPFunctionSerializeData = new XWBPFunctionSerializeData();
			xWBPFunctionSerializeData.Serialize(value2);
			Functions.Add(xWBPFunctionSerializeData);
		}
		foreach (XWBPVariableData value3 in data.Variables.Values)
		{
			XWBPVariableSerializeData xWBPVariableSerializeData = new XWBPVariableSerializeData();
			xWBPVariableSerializeData.Serialize(value3);
			Variables.Add(xWBPVariableSerializeData);
		}
		foreach (XWBPSignalData value4 in data.SignalDatas.Values)
		{
			XWBPSignalSerializeData xWBPSignalSerializeData = new XWBPSignalSerializeData();
			xWBPSignalSerializeData.Serialize(value4);
			Signals.Add(xWBPSignalSerializeData);
		}
		NextGraphId = data.NextGraphId;
		NextFunctionId = data.NextFunctionId;
		NextVariableId = data.NextVariableId;
		NextSignalId = data.NextSignalId;
	}

	public XWBPScriptData Deserialize()
	{
		XWBPScriptData xWBPScriptData = new XWBPScriptData
		{
			ExtendsClass = ExtendsClass,
			NextGraphId = NextGraphId,
			NextFunctionId = NextFunctionId,
			NextVariableId = NextVariableId,
			NextSignalId = NextSignalId
		};
		foreach (XWBPGraphSerializeData graph in Graphs)
		{
			XWBPGraphData xWBPGraphData = graph.Deserialize();
			xWBPGraphData.Owner = xWBPScriptData;
			xWBPScriptData.Graphs[xWBPGraphData.Id] = xWBPGraphData;
		}
		foreach (XWBPFunctionSerializeData function in Functions)
		{
			XWBPFunctionData xWBPFunctionData = (XWBPFunctionData)function.Deserialize();
			xWBPFunctionData.Owner = xWBPScriptData;
			xWBPScriptData.Functions[xWBPFunctionData.Id] = xWBPFunctionData;
		}
		foreach (XWBPVariableSerializeData variable in Variables)
		{
			XWBPVariableData xWBPVariableData = variable.Deserialize();
			xWBPVariableData.Owner = xWBPScriptData;
			xWBPScriptData.Variables[xWBPVariableData.Id] = xWBPVariableData;
		}
		foreach (XWBPSignalSerializeData signal in Signals)
		{
			XWBPSignalData xWBPSignalData = signal.Deserialize();
			xWBPSignalData.Owner = xWBPScriptData;
			xWBPScriptData.SignalDatas[xWBPSignalData.Id] = xWBPSignalData;
		}
		return xWBPScriptData;
	}

	public void Clear()
	{
		NextGraphId = 1;
		NextFunctionId = 1;
		NextVariableId = 1;
		NextSignalId = 1;
		Graphs.Clear();
		Functions.Clear();
		Variables.Clear();
		Signals.Clear();
	}

	public string GenerateCode()
	{
		return new XWBPCodeGenerator(Deserialize())
		{
			ClassName = XWBPCodeGenerator.BuildGeneratedClassName(base.ResourcePath),
			BlueprintSourcePath = base.ResourcePath
		}.Generate();
	}

	public Error SaveCode(string path)
	{
		if (string.IsNullOrWhiteSpace(base.ResourcePath))
		{
			return Error.InvalidParameter;
		}
		string text = base.ResourcePath.GetBaseDir().PathJoin(base.ResourcePath.GetFile().GetBaseName() + ".generated.cs");
		if (!string.Equals((path ?? "").Replace('\\', '/'), text.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
		{
			return Error.InvalidParameter;
		}
		string text2 = XWBlueprintGeneratedCSharpPolicy.AttachMetadata(GenerateCode(), base.ResourcePath, text);
		FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (!GodotObject.IsInstanceValid(fileAccess))
		{
			return Error.CantOpen;
		}
		fileAccess.StoreString(text2);
		fileAccess.Close();
		return Error.Ok;
	}
}
