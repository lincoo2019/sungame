using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPValidator : RefCounted
{
	public XWBPScriptData ScriptData { get; set; }

	public List<XWBPValidationResult> Results { get; set; } = new List<XWBPValidationResult>();

	public int MaxLoopDepth { get; set; } = 100;

	public XWBPValidator()
	{
	}

	public XWBPValidator(XWBPScriptData scriptData)
	{
		ScriptData = scriptData;
	}

	public List<XWBPValidationResult> Validate()
	{
		Results.Clear();
		ValidateAllGraphs();
		ValidateAllFunctions();
		ValidateDuplicateNames();
		ValidateGeneratedCode();
		return Results;
	}

	public void ValidateAllGraphs()
	{
		foreach (XWBPGraphData value in ScriptData.Graphs.Values)
		{
			ValidateGraph(value);
		}
	}

	public void ValidateAllFunctions()
	{
		foreach (XWBPFunctionData value in ScriptData.Functions.Values)
		{
			ValidateGraph(value);
		}
	}

	public void ValidateGraph(XWBPGraphData graphData)
	{
		ValidateEntryNode(graphData);
		ValidateConnections(graphData);
		ValidateUnsupportedNodes(graphData);
		ValidateDeadLoop(graphData);
		ValidateUnconnectedPorts(graphData);
		ValidateMissingMethods(graphData);
		ValidateMissingVariables(graphData);
		ValidateMissingSignals(graphData);
		ValidateOrphanNodes(graphData);
		ValidateMissingReturn(graphData);
	}

	public void ValidateEntryNode(XWBPGraphData graphData)
	{
		List<XWBPNodeData> list = new List<XWBPNodeData>();
		List<XWBPNodeData> list2 = new List<XWBPNodeData>();
		List<XWBPNodeData> list3 = new List<XWBPNodeData>();
		List<XWBPNodeData> list4 = new List<XWBPNodeData>();
		foreach (XWBPNodeData value in graphData.Nodes.Values)
		{
			if (value.TypeId == (StringName)"__XWBPGraphNode_Entry")
			{
				list.Add(value);
			}
			else if (value.TypeId == (StringName)"__XWBPGraphNode_MethodEntry")
			{
				switch (value.GetMetaData("MethodType").AsInt32())
				{
				case 0:
					list2.Add(value);
					break;
				case 2:
					list3.Add(value);
					break;
				}
			}
			else if (IsEventEntryNode(value))
			{
				list4.Add(value);
			}
		}
		List<XWBPNodeData> list5 = new List<XWBPNodeData>();
		list5.AddRange(list);
		list5.AddRange(list2);
		if (list5.Count == 0 && list3.Count == 0 && list4.Count == 0)
		{
			Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingEntry, "图表 '" + graphData.Name + "' 缺少入口节点").SetGraphData(graphData));
		}
		else if (list5.Count > 1)
		{
			for (int i = 1; i < list5.Count; i++)
			{
				Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.DuplicateEntry, "图表 '" + graphData.Name + "' 存在多个入口节点", XWBPValidationResult.Severity.Warning).SetNodeId(list5[i].Id).SetGraphData(graphData));
			}
		}
	}

	public void ValidateConnections(XWBPGraphData graphData)
	{
		HashSet<string> hashSet = new HashSet<string>();
		System.Collections.Generic.Dictionary<string, XWBPNodeConnectionData> dictionary = new System.Collections.Generic.Dictionary<string, XWBPNodeConnectionData>();
		System.Collections.Generic.Dictionary<string, XWBPNodeConnectionData> dictionary2 = new System.Collections.Generic.Dictionary<string, XWBPNodeConnectionData>();
		foreach (XWBPNodeConnectionData connection in graphData.Connections)
		{
			string connectionKey = GetConnectionKey(connection);
			if (!hashSet.Add(connectionKey))
			{
				AddConnectionError(graphData, connection, "存在重复连接", XWBPValidationResult.Severity.Warning);
				continue;
			}
			if (connection.FromNodeId == connection.ToNodeId)
			{
				AddConnectionError(graphData, connection, "节点不能连接到自身");
				continue;
			}
			XWBPNodeData node = graphData.GetNode(connection.FromNodeId);
			XWBPNodeData node2 = graphData.GetNode(connection.ToNodeId);
			if (node == null || node2 == null)
			{
				AddConnectionError(graphData, connection, "连接引用了不存在的节点");
				continue;
			}
			XWBPNodePortData outputPort = node.GetOutputPort(connection.FromPortIndex);
			XWBPNodePortData inputPort = node2.GetInputPort(connection.ToPortIndex);
			if (outputPort == null || inputPort == null)
			{
				AddConnectionError(graphData, connection, $"连接端口索引无效: {connection.FromPortIndex} -> {connection.ToPortIndex}");
				continue;
			}
			if (outputPort.PortDirection != XWBPNodePortData.Direction.Output || inputPort.PortDirection != XWBPNodePortData.Direction.Input)
			{
				AddConnectionError(graphData, connection, "连接方向无效");
				continue;
			}
			if (!outputPort.CanConnectTo(inputPort))
			{
				AddConnectionError(graphData, connection, "端口类型不兼容: " + FormatPort(outputPort) + " -> " + FormatPort(inputPort));
				continue;
			}
			string key = $"{connection.ToNodeId}:{connection.ToPortIndex}";
			if (dictionary.TryGetValue(key, out var value))
			{
				AddConnectionError(graphData, connection, $"输入端口已经连接: 节点 {value.FromNodeId} -> 节点 {connection.ToNodeId}");
			}
			else
			{
				dictionary[key] = connection;
			}
			if (outputPort.PortTypeValue == XWBPNodePortData.PortType.Flow)
			{
				string key2 = $"{connection.FromNodeId}:{connection.FromPortIndex}";
				if (dictionary2.TryGetValue(key2, out var value2))
				{
					AddConnectionError(graphData, connection, $"流程输出端口已经连接: 节点 {connection.FromNodeId} -> 节点 {value2.ToNodeId}");
				}
				else
				{
					dictionary2[key2] = connection;
				}
			}
		}
	}

	private void AddConnectionError(XWBPGraphData graphData, XWBPNodeConnectionData connection, string message, XWBPValidationResult.Severity severity = XWBPValidationResult.Severity.Error)
	{
		XWBPNodeData xWBPNodeData = graphData.GetNode(connection?.FromNodeId ?? (-1)) ?? graphData.GetNode(connection?.ToNodeId ?? (-1));
		Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.InvalidConnection, message, severity).SetNodeId(xWBPNodeData?.Id ?? (-1)).SetGraphData(graphData).SetPosition(xWBPNodeData?.Position ?? Vector2.Zero));
	}

	private static string GetConnectionKey(XWBPNodeConnectionData connection)
	{
		return $"{connection.FromNodeId}:{connection.FromPortIndex}->{connection.ToNodeId}:{connection.ToPortIndex}";
	}

	private static string FormatPort(XWBPNodePortData port)
	{
		string text = port.ClassName.ToString();
		string text2 = port.PortTypeValue.ToString();
		if (!string.IsNullOrEmpty(text))
		{
			text2 = text2 + "<" + text + ">";
		}
		return port.Name + "(" + text2 + ")";
	}

	private static bool IsEventEntryNode(XWBPNodeData node)
	{
		if (!(node.TypeId == (StringName)"__XWBPGraphNode_OnStart") && !(node.TypeId == (StringName)"__XWBPGraphNode_SignalEvent") && !(node.TypeId == (StringName)"__XWBPGraphNode_CustomEvent"))
		{
			return node.TypeId == (StringName)"__XWBPGraphNode_TimerEvent";
		}
		return true;
	}

	public void ValidateDeadLoop(XWBPGraphData graphData)
	{
		HashSet<int> visited = new HashSet<int>();
		HashSet<int> recursionStack = new HashSet<int>();
		int currentFunctionId = -1;
		if (graphData is XWBPFunctionData xWBPFunctionData && xWBPFunctionData.Nodes.TryGetValue(-100000, out var value))
		{
			currentFunctionId = value.GetMetaData("FunctionId").AsInt32();
		}
		foreach (XWBPNodeData value2 in graphData.Nodes.Values)
		{
			if ((value2.TypeId == (StringName)"__XWBPGraphNode_Entry" || value2.TypeId == (StringName)"__XWBPGraphNode_MethodEntry" || IsEventEntryNode(value2)) && DetectDeadLoop(graphData, value2.Id, visited, recursionStack, 0, currentFunctionId))
			{
				break;
			}
		}
	}

	public bool DetectDeadLoop(XWBPGraphData graphData, int nodeId, HashSet<int> visited, HashSet<int> recursionStack, int depth, int currentFunctionId = -1)
	{
		XWBPNodeData node = graphData.GetNode(nodeId);
		if (depth > MaxLoopDepth)
		{
			Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.DeadLoop, "检测到可能的死循环或过深的执行路径", XWBPValidationResult.Severity.Warning).SetNodeId(nodeId).SetGraphData(graphData).SetPosition(node?.Position ?? Vector2.Zero));
			return true;
		}
		if (recursionStack.Contains(nodeId))
		{
			Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.DeadLoop, "检测到死循环").SetNodeId(nodeId).SetGraphData(graphData).SetPosition(node?.Position ?? Vector2.Zero));
			return true;
		}
		if (visited.Contains(nodeId))
		{
			return false;
		}
		visited.Add(nodeId);
		recursionStack.Add(nodeId);
		if (node != null && node.TypeId == (StringName)"__XWBPGraphNode_CallMethod" && node.GetMetaData("MethodType").AsInt32() == 0 && node.GetMetaData("FunctionId").AsInt32() == currentFunctionId && currentFunctionId >= 0)
		{
			Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.DeadLoop, "检测到函数递归调用，可能导致死循环").SetNodeId(nodeId).SetGraphData(graphData).SetPosition(node.Position));
			return true;
		}
		foreach (XWBPNodeConnectionData item in graphData.GetConnectionsFromNode(nodeId))
		{
			XWBPNodeData node2 = graphData.GetNode(item.FromNodeId);
			if (node2 != null)
			{
				XWBPNodePortData outputPort = node2.GetOutputPort(item.FromPortIndex);
				if (outputPort != null && outputPort.PortTypeValue == XWBPNodePortData.PortType.Flow && DetectDeadLoop(graphData, item.ToNodeId, visited, recursionStack, depth + 1, currentFunctionId))
				{
					return true;
				}
			}
		}
		recursionStack.Remove(nodeId);
		return false;
	}

	public void ValidateUnconnectedPorts(XWBPGraphData graphData)
	{
		foreach (XWBPNodeData value in graphData.Nodes.Values)
		{
			if (value.TypeId == (StringName)"__XWBPGraphNode_Comment" || value.TypeId == (StringName)"__XWBPGraphNode_Break" || value.TypeId == (StringName)"__XWBPGraphNode_Continue" || value.TypeId == (StringName)"__XWBPGraphNode_Return")
			{
				continue;
			}
			for (int i = 0; i < value.InputPorts.Count; i++)
			{
				XWBPNodePortData xWBPNodePortData = value.InputPorts[i];
				if (graphData.GetInputConnections(value.Id, i).Count != 0)
				{
					continue;
				}
				if (xWBPNodePortData.PortTypeValue == XWBPNodePortData.PortType.Flow)
				{
					if (value.TypeId != (StringName)"__XWBPGraphNode_Entry" && value.TypeId != (StringName)"__XWBPGraphNode_MethodEntry")
					{
						Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.UnconnectedPort, $"节点 '{value.TypeId}' 的流程输入端口 '{xWBPNodePortData.Name}' 未连接", XWBPValidationResult.Severity.Warning).SetNodeId(value.Id).SetPortIndex(i).SetGraphData(graphData)
							.SetPosition(value.Position));
					}
				}
				else if (xWBPNodePortData.Value.VariantType == Variant.Type.Nil && xWBPNodePortData.DefaultValue.VariantType == Variant.Type.Nil)
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.UnconnectedPort, $"节点 '{value.TypeId}' 的数据输入端口 '{xWBPNodePortData.Name}' 未连接且无默认值", XWBPValidationResult.Severity.Warning).SetNodeId(value.Id).SetPortIndex(i).SetGraphData(graphData)
						.SetPosition(value.Position));
				}
			}
		}
	}

	public void ValidateMissingMethods(XWBPGraphData graphData)
	{
		foreach (XWBPNodeData value3 in graphData.Nodes.Values)
		{
			if (!(value3.TypeId == (StringName)"__XWBPGraphNode_CallMethod"))
			{
				continue;
			}
			switch (value3.GetMetaData("MethodType").AsInt32())
			{
			case 0:
			{
				int key = value3.GetMetaData("FunctionId").AsInt32();
				if (!ScriptData.Functions.ContainsKey(key))
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingMethod, "调用的蓝图函数不存在").SetNodeId(value3.Id).SetGraphData(graphData).SetPosition(value3.Position));
				}
				break;
			}
			case 1:
			case 2:
			{
				Dictionary dictionary = value3.GetMetaData("MethodData").As<Dictionary>();
				if (dictionary == null || dictionary.Count == 0)
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingMethod, "脚本方法节点缺少方法元数据，请重新选择方法节点").SetNodeId(value3.Id).SetGraphData(graphData).SetPosition(value3.Position));
					break;
				}
				Variant value;
				string text = (dictionary.TryGetValue("name", out value) ? value.AsStringName().ToString() : "");
				Variant value2;
				string text2 = (dictionary.TryGetValue("base_class_name", out value2) ? value2.AsStringName().ToString() : "");
				if (!CSharpMethodExists(text2, text, dictionary))
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingMethod, $"类 '{text2}' 不存在，无法调用方法 '{text}'").SetNodeId(value3.Id).SetGraphData(graphData).SetPosition(value3.Position));
				}
				break;
			}
			}
		}
	}

	public void ValidateMissingVariables(XWBPGraphData graphData)
	{
		foreach (XWBPNodeData value4 in graphData.Nodes.Values)
		{
			if (!(value4.TypeId == (StringName)"__XWBPGraphNode_GetProperty") && !(value4.TypeId == (StringName)"__XWBPGraphNode_SetProperty"))
			{
				continue;
			}
			switch (value4.GetMetaData("MethodType").AsInt32())
			{
			case 0:
			{
				int key = value4.GetMetaData("VariableId").AsInt32();
				if (!ScriptData.Variables.ContainsKey(key))
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingVariable, "引用的蓝图变量不存在").SetNodeId(value4.Id).SetGraphData(graphData).SetPosition(value4.Position));
				}
				break;
			}
			case 1:
			{
				Dictionary dictionary = value4.GetMetaData("PropertyData").As<Dictionary>();
				if (dictionary == null || dictionary.Count == 0)
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingVariable, "脚本属性节点缺少属性元数据，请重新选择属性节点").SetNodeId(value4.Id).SetGraphData(graphData).SetPosition(value4.Position));
					break;
				}
				Variant value;
				string text = (dictionary.TryGetValue("name", out value) ? value.AsStringName().ToString() : "");
				Variant value2;
				string text2 = (dictionary.TryGetValue("base_class_name", out value2) ? value2.AsStringName().ToString() : "");
				if (!CSharpPropertyExists(text2, text, dictionary))
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingVariable, $"类 '{text2}' 不存在，无法访问属性 '{text}'").SetNodeId(value4.Id).SetGraphData(graphData).SetPosition(value4.Position));
				}
				if (value4.TypeId == (StringName)"__XWBPGraphNode_SetProperty" && dictionary.TryGetValue("has_set", out var value3) && !value3.AsBool())
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingVariable, "属性 '" + text + "' 不可写，不能使用设置属性节点").SetNodeId(value4.Id).SetGraphData(graphData).SetPosition(value4.Position));
				}
				break;
			}
			}
		}
	}

	public void ValidateMissingSignals(XWBPGraphData graphData)
	{
		foreach (XWBPNodeData value in graphData.Nodes.Values)
		{
			if (!(value.TypeId != (StringName)"__XWBPGraphNode_SignalEvent") || !(value.TypeId != (StringName)"__XWBPGraphNode_EmitSignal"))
			{
				int key = value.GetMetaData("SignalId").AsInt32();
				if (!ScriptData.SignalDatas.ContainsKey(key))
				{
					Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingSignal, (value.TypeId == (StringName)"__XWBPGraphNode_SignalEvent") ? "信号事件引用的蓝图信号不存在" : "发射信号节点引用的蓝图信号不存在").SetNodeId(value.Id).SetGraphData(graphData).SetPosition(value.Position));
				}
			}
		}
	}

	public void ValidateUnsupportedNodes(XWBPGraphData graphData)
	{
		foreach (XWBPNodeData value in graphData.Nodes.Values)
		{
			XWBPNodeType nodeType = XWBPNodeRegistry.Instance.GetNodeType(value.TypeId.ToString());
			if (nodeType == null)
			{
				Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.UnsupportedNode, $"节点类型 '{value.TypeId}' 未注册，无法生成 C# 代码").SetNodeId(value.Id).SetGraphData(graphData).SetPosition(value.Position));
			}
			else if (HasFlowPort(value) && (!OverridesFunctionCodeGeneration(nodeType) || !OverridesGraphCodeGeneration(nodeType)))
			{
				Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.UnsupportedNode, $"节点类型 '{value.TypeId}' 缺少流程代码生成实现").SetNodeId(value.Id).SetGraphData(graphData).SetPosition(value.Position));
			}
		}
	}

	private static bool HasFlowPort(XWBPNodeData node)
	{
		foreach (XWBPNodePortData inputPort in node.InputPorts)
		{
			if (inputPort != null && inputPort.PortTypeValue == XWBPNodePortData.PortType.Flow)
			{
				return true;
			}
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

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ModEditor validates blueprint node type overrides at runtime by design.")]
	private static bool OverridesFunctionCodeGeneration(XWBPNodeType nodeType)
	{
		MethodInfo method = nodeType.GetType().GetMethod("GenerateCode", new Type[3]
		{
			typeof(XWBPCodeGenerator),
			typeof(XWBPNodeData),
			typeof(XWBPFunctionData)
		});
		if (method != null)
		{
			return method.DeclaringType != typeof(XWBPNodeType);
		}
		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ModEditor validates blueprint node type overrides at runtime by design.")]
	private static bool OverridesGraphCodeGeneration(XWBPNodeType nodeType)
	{
		MethodInfo method = nodeType.GetType().GetMethod("GenerateCodeFromGraph", new Type[3]
		{
			typeof(XWBPCodeGenerator),
			typeof(XWBPNodeData),
			typeof(XWBPGraphData)
		});
		if (method != null)
		{
			return method.DeclaringType != typeof(XWBPNodeType);
		}
		return false;
	}

	private static bool CSharpMethodExists(string className, string methodName, Dictionary methodData)
	{
		Variant value;
		string text = (methodData.TryGetValue("cs_name", out value) ? value.AsString() : methodName);
		foreach (Dictionary classMethod in XWBPCSharpMemberRegistry.Instance.GetClassMethodList(className))
		{
			Variant value2;
			string text2 = (classMethod.TryGetValue("name", out value2) ? value2.AsString() : "");
			Variant value3;
			string text3 = (classMethod.TryGetValue("cs_name", out value3) ? value3.AsString() : text2);
			if (text2 == methodName || text3 == text)
			{
				return true;
			}
		}
		return false;
	}

	private static bool CSharpPropertyExists(string className, string propertyName, Dictionary propertyData)
	{
		Variant value;
		string text = (propertyData.TryGetValue("cs_name", out value) ? value.AsString() : propertyName);
		foreach (Dictionary classProperty in XWBPCSharpMemberRegistry.Instance.GetClassPropertyList(className))
		{
			Variant value2;
			string text2 = (classProperty.TryGetValue("name", out value2) ? value2.AsString() : "");
			Variant value3;
			string text3 = (classProperty.TryGetValue("cs_name", out value3) ? value3.AsString() : text2);
			if (text2 == propertyName || text3 == text)
			{
				return true;
			}
		}
		return false;
	}

	public void ValidateGeneratedCode()
	{
		try
		{
			string code = new XWBPCodeGenerator(ScriptData).Generate();
			XWCodeErrorCheckerRegistry.Init();
			foreach (XWCodeErrorChecker.ErrorData item in XWCodeErrorCheckerRegistry.CheckCode(code, "cs"))
			{
				XWBPValidationResult.Severity severity = ((item.SeverityLevel != XWCodeErrorChecker.Severity.Error) ? ((item.SeverityLevel == XWCodeErrorChecker.Severity.Warning) ? XWBPValidationResult.Severity.Warning : XWBPValidationResult.Severity.Info) : XWBPValidationResult.Severity.Error);
				Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.GeneratedCode, $"生成 C# 第 {item.Line + 1} 行: {item.Message}", severity));
			}
		}
		catch (Exception ex)
		{
			Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.GeneratedCode, "生成 C# 失败: " + ex.Message));
		}
	}

	public void ValidateOrphanNodes(XWBPGraphData graphData)
	{
		HashSet<int> hashSet = new HashSet<int>();
		List<XWBPNodeData> list = new List<XWBPNodeData>();
		foreach (XWBPNodeData value in graphData.Nodes.Values)
		{
			if (value.TypeId == (StringName)"__XWBPGraphNode_Entry")
			{
				list.Add(value);
			}
			else if (value.TypeId == (StringName)"__XWBPGraphNode_MethodEntry")
			{
				int num = value.GetMetaData("MethodType").AsInt32();
				if (num == 0 || num == 2)
				{
					list.Add(value);
				}
			}
			else if (IsEventEntryNode(value))
			{
				list.Add(value);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		foreach (XWBPNodeData item in list)
		{
			FindFlowReachableNodes(graphData, item.Id, hashSet);
		}
		FindDataReachableNodes(graphData, hashSet);
		foreach (XWBPNodeData value2 in graphData.Nodes.Values)
		{
			if (!hashSet.Contains(value2.Id) && value2.TypeId != (StringName)"__XWBPGraphNode_Comment" && value2.TypeId != (StringName)"__XWBPGraphNode_Break" && value2.TypeId != (StringName)"__XWBPGraphNode_Continue" && value2.TypeId != (StringName)"__XWBPGraphNode_Return")
			{
				Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.OrphanNode, $"节点 '{value2.TypeId}' 无法从入口节点到达", XWBPValidationResult.Severity.Warning).SetNodeId(value2.Id).SetGraphData(graphData).SetPosition(value2.Position));
			}
		}
	}

	private void FindFlowReachableNodes(XWBPGraphData graphData, int nodeId, HashSet<int> reachable)
	{
		if (reachable.Contains(nodeId))
		{
			return;
		}
		reachable.Add(nodeId);
		foreach (XWBPNodeConnectionData item in graphData.GetConnectionsFromNode(nodeId))
		{
			XWBPNodeData node = graphData.GetNode(item.FromNodeId);
			if (node != null)
			{
				XWBPNodePortData outputPort = node.GetOutputPort(item.FromPortIndex);
				if (outputPort != null && outputPort.PortTypeValue == XWBPNodePortData.PortType.Flow)
				{
					FindFlowReachableNodes(graphData, item.ToNodeId, reachable);
				}
			}
		}
	}

	private void FindDataReachableNodes(XWBPGraphData graphData, HashSet<int> reachable)
	{
		HashSet<int> hashSet = new HashSet<int>();
		List<int> list = new List<int>(reachable);
		while (list.Count > 0)
		{
			int num = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			if (hashSet.Contains(num))
			{
				continue;
			}
			hashSet.Add(num);
			foreach (XWBPNodeConnectionData item in graphData.GetConnectionsToNode(num))
			{
				XWBPNodeData node = graphData.GetNode(item.FromNodeId);
				if (node == null)
				{
					continue;
				}
				XWBPNodePortData outputPort = node.GetOutputPort(item.FromPortIndex);
				if (outputPort != null && outputPort.PortTypeValue != XWBPNodePortData.PortType.Flow)
				{
					if (!reachable.Contains(item.FromNodeId))
					{
						reachable.Add(item.FromNodeId);
					}
					if (!hashSet.Contains(item.FromNodeId))
					{
						list.Add(item.FromNodeId);
					}
				}
			}
		}
	}

	public void ValidateMissingReturn(XWBPGraphData graphData)
	{
		if (!(graphData is XWBPFunctionData xWBPFunctionData) || xWBPFunctionData.Outputs.Count == 0)
		{
			return;
		}
		bool flag = false;
		foreach (XWBPNodeData value in graphData.Nodes.Values)
		{
			if (value.TypeId == (StringName)"__XWBPGraphNode_Return")
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.MissingReturn, "函数 '" + xWBPFunctionData.Name + "' 有返回值但缺少 Return 节点", XWBPValidationResult.Severity.Warning).SetGraphData(graphData));
		}
	}

	public void ValidateDuplicateNames()
	{
		List<(string, int, XWBPGraphData)> list = new List<(string, int, XWBPGraphData)>();
		foreach (int key in ScriptData.SignalDatas.Keys)
		{
			XWBPSignalData xWBPSignalData = ScriptData.SignalDatas[key];
			list.Add((xWBPSignalData.Name, key, null));
		}
		ValidateDuplicateNames(list, XWBPValidationResult.ErrorType.DuplicateSignalName, "信号名");
		List<(string, int, XWBPGraphData)> list2 = new List<(string, int, XWBPGraphData)>();
		foreach (int key2 in ScriptData.Variables.Keys)
		{
			XWBPVariableData xWBPVariableData = ScriptData.Variables[key2];
			list2.Add((xWBPVariableData.Name, key2, null));
		}
		ValidateDuplicateNames(list2, XWBPValidationResult.ErrorType.DuplicateVariableName, "变量名");
		List<(string, int, XWBPGraphData)> list3 = new List<(string, int, XWBPGraphData)>();
		foreach (int key3 in ScriptData.Functions.Keys)
		{
			XWBPFunctionData xWBPFunctionData = ScriptData.Functions[key3];
			list3.Add((xWBPFunctionData.Name, key3, xWBPFunctionData));
		}
		ValidateDuplicateNames(list3, XWBPValidationResult.ErrorType.DuplicateFunctionName, "函数名");
		ValidateDuplicateFunctionParameterNames();
	}

	private void ValidateDuplicateNames(List<(string Name, int Id, XWBPGraphData Data)> entries, XWBPValidationResult.ErrorType errorType, string label)
	{
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>();
		System.Collections.Generic.Dictionary<string, List<(string, int, XWBPGraphData)>> dictionary2 = new System.Collections.Generic.Dictionary<string, List<(string, int, XWBPGraphData)>>();
		foreach (var entry in entries)
		{
			if (dictionary.TryGetValue(entry.Name, out var value))
			{
				dictionary[entry.Name] = value + 1;
				dictionary2[entry.Name].Add(entry);
			}
			else
			{
				dictionary[entry.Name] = 1;
				dictionary2[entry.Name] = new List<(string, int, XWBPGraphData)> { entry };
			}
		}
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			string key = item.Key;
			if (item.Value <= 1)
			{
				continue;
			}
			foreach (var item2 in dictionary2[key])
			{
				XWBPValidationResult xWBPValidationResult = new XWBPValidationResult(errorType, label + " '" + key + "' 重复");
				if (item2.Item3 != null)
				{
					xWBPValidationResult.SetGraphData(item2.Item3);
				}
				Results.Add(xWBPValidationResult);
			}
		}
	}

	public void ValidateDuplicateFunctionParameterNames()
	{
		foreach (int key in ScriptData.Functions.Keys)
		{
			XWBPFunctionData functionData = ScriptData.Functions[key];
			ValidateDuplicateParametersInFunction(functionData);
		}
	}

	public void ValidateDuplicateParametersInFunction(XWBPFunctionData functionData)
	{
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>();
		System.Collections.Generic.Dictionary<string, int> dictionary2 = new System.Collections.Generic.Dictionary<string, int>();
		foreach (XWBPNodePortData input in functionData.Inputs)
		{
			string name = input.Name;
			dictionary[name] = ((!dictionary.TryGetValue(name, out var value)) ? 1 : (value + 1));
		}
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			if (item.Value > 1)
			{
				Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.DuplicateParameterName, $"函数 '{functionData.Name}' 的输入参数名 '{item.Key}' 重复").SetGraphData(functionData));
			}
		}
		foreach (XWBPNodePortData output in functionData.Outputs)
		{
			string name2 = output.Name;
			dictionary2[name2] = ((!dictionary2.TryGetValue(name2, out var value2)) ? 1 : (value2 + 1));
		}
		foreach (KeyValuePair<string, int> item2 in dictionary2)
		{
			if (item2.Value > 1)
			{
				Results.Add(new XWBPValidationResult(XWBPValidationResult.ErrorType.DuplicateParameterName, $"函数 '{functionData.Name}' 的输出参数名 '{item2.Key}' 重复").SetGraphData(functionData));
			}
		}
	}

	public int GetErrorCount()
	{
		int num = 0;
		foreach (XWBPValidationResult result in Results)
		{
			if (result.ResultSeverity == XWBPValidationResult.Severity.Error)
			{
				num++;
			}
		}
		return num;
	}

	public int GetWarningCount()
	{
		int num = 0;
		foreach (XWBPValidationResult result in Results)
		{
			if (result.ResultSeverity == XWBPValidationResult.Severity.Warning)
			{
				num++;
			}
		}
		return num;
	}

	public bool HasErrors()
	{
		return GetErrorCount() > 0;
	}

	public void PrintResults()
	{
		GD.Print("=== 蓝图校验结果 ===");
		GD.Print($"错误: {GetErrorCount()}, 警告: {GetWarningCount()}");
		foreach (XWBPValidationResult result in Results)
		{
			GD.Print(result.ToString());
		}
	}
}
