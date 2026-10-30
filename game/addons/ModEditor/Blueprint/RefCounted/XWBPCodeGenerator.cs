using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPCodeGenerator : RefCounted
{
	public XWBPScriptData ScriptData { get; set; }

	public int IndentLevel { get; set; }

	public List<string> CodeLines { get; set; } = new List<string>();

	public System.Collections.Generic.Dictionary<int, string> NodeVariables { get; set; } = new System.Collections.Generic.Dictionary<int, string>();

	public bool HasReadyFunction { get; set; }

	public Dictionary SignalConnections { get; set; } = new Dictionary();

	public Dictionary SignalCallbackNames { get; set; } = new Dictionary();

	public string ClassName { get; set; } = "BPScriptGenerated";

	public string BlueprintSourcePath { get; set; } = "";

	public XWBPCodeGenerator()
	{
	}

	public XWBPCodeGenerator(XWBPScriptData scriptData)
	{
		ScriptData = scriptData;
	}

	public static string BuildGeneratedClassName(string blueprintPath, string projectRoot = "")
	{
		string text = (blueprintPath ?? "").Replace('\\', '/').Trim();
		string text2 = text;
		try
		{
			if (!string.IsNullOrWhiteSpace(projectRoot))
			{
				text2 = Path.GetRelativePath(Path.GetFullPath(projectRoot), Path.GetFullPath(text)).Replace('\\', '/');
			}
		}
		catch
		{
			text2 = text;
		}
		string? fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
		StringBuilder stringBuilder = new StringBuilder();
		string text3 = fileNameWithoutExtension;
		foreach (char c in text3)
		{
			stringBuilder.Append((char.IsLetterOrDigit(c) || c == '_') ? c : '_');
		}
		if (stringBuilder.Length == 0)
		{
			stringBuilder.Append("Blueprint");
		}
		if (char.IsDigit(stringBuilder[0]))
		{
			stringBuilder.Insert(0, "Blueprint_");
		}
		if (!stringBuilder.ToString().EndsWith("Blueprint", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.Append("Blueprint");
		}
		byte[] inArray = SHA256.HashData(Encoding.UTF8.GetBytes(text2.ToLowerInvariant()));
		return $"{stringBuilder}_{Convert.ToHexString(inArray, 0, 4)}";
	}

	public string Generate()
	{
		CodeLines.Clear();
		NodeVariables.Clear();
		IndentLevel = 0;
		HasReadyFunction = false;
		SignalConnections.Clear();
		SignalCallbackNames.Clear();
		CollectSignalConnections();
		GenerateUsings();
		GenerateClassHeader();
		IndentLevel++;
		GenerateCSharpValueAdapter();
		GenerateSignals();
		GenerateVariables();
		GenerateFunctions();
		IndentLevel--;
		AddLine("}");
		return string.Join("\n", CodeLines);
	}

	public void AddLine(string line = "")
	{
		string text = new string('\t', IndentLevel);
		CodeLines.Add(text + line);
	}

	public void GenerateUsings()
	{
		AddLine("using System;");
		AddLine("using System.Runtime.CompilerServices;");
		AddLine("using Godot;");
		AddLine();
	}

	public void GenerateClassHeader()
	{
		string text = ScriptData.ExtendsClass.ToString();
		if (string.IsNullOrEmpty(text))
		{
			text = "RefCounted";
		}
		string text2 = ToCsClassName(text);
		AddLine("public partial class " + ClassName + " : " + text2);
		AddLine("{");
	}

	private void GenerateCSharpValueAdapter()
	{
		AddLine("private static T __XWBlueprintConvertValue<T>(object value)");
		AddLine("{");
		IndentLevel++;
		AddLine("if (value is T typedValue) return typedValue;");
		AddLine("if (value == null) return default;");
		AddLine("Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);");
		AddLine("if (targetType.IsEnum)");
		AddLine("{");
		IndentLevel++;
		AddLine("return (T)Enum.ToObject(targetType, Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture));");
		IndentLevel--;
		AddLine("}");
		AddLine("return (T)Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture);");
		IndentLevel--;
		AddLine("}");
		AddLine();
		AddLine("private object __XWStateMachineRuntimeHost;");
		AddLine("private object __XWResolveSelf() => __XWStateMachineRuntimeHost ?? this;");
		AddLine();
	}

	public string ConvertToCSharpMemberValue(string expression, Dictionary typeData)
	{
		string text = (string.IsNullOrWhiteSpace(expression) ? "null" : expression);
		if (typeData == null || !typeData.TryGetValue("cs_type_name", out var value))
		{
			return text;
		}
		string text2 = value.AsString().Trim();
		if (string.IsNullOrWhiteSpace(text2) || string.Equals(text2, "void", StringComparison.Ordinal))
		{
			return text;
		}
		return $"__XWBlueprintConvertValue<{text2}>({text})";
	}

	public void GenerateSignals()
	{
		if (ScriptData.SignalDatas.Count == 0)
		{
			return;
		}
		AddLine("// Signals");
		foreach (XWBPSignalData value3 in ScriptData.SignalDatas.Values)
		{
			string value = ToPascalCase(value3.Name);
			List<string> list = new List<string>();
			int argIndex = 0;
			foreach (XWBPNodePortData input in value3.Inputs)
			{
				string typeName = GetTypeName((int)input.PortTypeValue, input.ClassName);
				string text = ToValidIdentifier(input.Name, ref argIndex);
				list.Add(typeName + " " + text);
			}
			string value2 = ((list.Count == 0) ? "" : string.Join(", ", list));
			AddLine($"[Signal] public delegate void {value}EventHandler({value2});");
		}
		AddLine();
	}

	public void GenerateVariables()
	{
		if (ScriptData.Variables.Count == 0)
		{
			return;
		}
		AddLine("// Variables");
		foreach (XWBPVariableData value2 in ScriptData.Variables.Values)
		{
			string value = ToPascalCase(value2.Name);
			int type = (int)value2.Type;
			Variant defaultValue = value2.DefaultValue;
			string typeName = GetTypeName(type, value2.ClassName);
			string defaultValueString = GetDefaultValueString(defaultValue, type);
			AddLine($"public {typeName} {value} = {defaultValueString};");
		}
		AddLine();
	}

	public void GenerateFunctions()
	{
		foreach (int key in ScriptData.Graphs.Keys)
		{
			XWBPGraphData graph = ScriptData.Graphs[key];
			GenerateGraph(graph);
		}
		GenerateSignalConnections();
		foreach (int key2 in ScriptData.Functions.Keys)
		{
			XWBPFunctionData function = ScriptData.Functions[key2];
			GenerateFunction(function);
		}
		GenerateStateMachineCallbackBridges();
	}

	private void GenerateStateMachineCallbackBridges()
	{
		List<int> list = new List<int>(ScriptData.Functions.Keys);
		list.Sort();
		bool flag = false;
		foreach (int item in list)
		{
			XWBPFunctionData xWBPFunctionData = ScriptData.Functions[item];
			if (xWBPFunctionData != null && xWBPFunctionData.StateMachineCallbackEnabled)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		AddLine("private static readonly ConditionalWeakTable<object, " + ClassName + "> __XWStateMachineInstances = new();");
		AddLine("private static " + ClassName + " __XWGetStateMachineInstance(object host)");
		AddLine("{");
		IndentLevel++;
		AddLine("if (host == null) throw new InvalidOperationException(\"State-machine Blueprint callback received a null runtime host.\");");
		AddLine("if (host is " + ClassName + " generatedHost) return generatedHost;");
		AddLine("return __XWStateMachineInstances.GetValue(host, static _ => new " + ClassName + "());");
		IndentLevel--;
		AddLine("}");
		AddLine();
		HashSet<(string, StateMachineCallbackPhase)> hashSet = new HashSet<(string, StateMachineCallbackPhase)>();
		foreach (int item2 in list)
		{
			XWBPFunctionData xWBPFunctionData2 = ScriptData.Functions[item2];
			if (xWBPFunctionData2 != null && xWBPFunctionData2.StateMachineCallbackEnabled)
			{
				string text = (xWBPFunctionData2.StateMachineCallbackLocalKey ?? "").Trim();
				if (!StateMachineCallbackKey.TryBuild("blueprint", text, out var _, out var error))
				{
					throw new InvalidOperationException("蓝图函数“" + xWBPFunctionData2.Name + "”的状态机动作键无效：" + error);
				}
				if (!TryValidateStateMachineCallbackSignature(ScriptData, xWBPFunctionData2, xWBPFunctionData2.StateMachineCallbackPhase, out var error2))
				{
					throw new InvalidOperationException($"蓝图函数“{xWBPFunctionData2.Name}”不能绑定为{FormatStateMachineCallbackPhase(xWBPFunctionData2.StateMachineCallbackPhase)}：" + error2);
				}
				if (!hashSet.Add((text, xWBPFunctionData2.StateMachineCallbackPhase)))
				{
					throw new InvalidOperationException("蓝图状态机动作“" + text + "”的" + FormatStateMachineCallbackPhase(xWBPFunctionData2.StateMachineCallbackPhase) + "阶段重复。");
				}
				string text2 = ToCsMethodName(xWBPFunctionData2.Name);
				string text3 = $"__XWStateMachineCallback_{item2}_" + xWBPFunctionData2.StateMachineCallbackPhase;
				string value = (string.IsNullOrWhiteSpace(xWBPFunctionData2.Name) ? text : xWBPFunctionData2.Name.Trim());
				string value2 = (BlueprintSourcePath ?? "").Replace('\\', '/').Trim();
				AddLine($"[StateMachineCallback(\"{EscapeCSharpString(text)}\", StateMachineCallbackPhase.{xWBPFunctionData2.StateMachineCallbackPhase}, " + "SourceKind = StateMachineCallbackSourceKind.Blueprint, DisplayName = \"" + EscapeCSharpString(value) + "\", SourcePath = \"" + EscapeCSharpString(value2) + "\")]");
				switch (xWBPFunctionData2.StateMachineCallbackPhase)
				{
				case StateMachineCallbackPhase.Enter:
				case StateMachineCallbackPhase.Exit:
					AddLine("public static void " + text3 + "(in StateMachineCallbackContext context)");
					AddLine("{");
					IndentLevel++;
					AddLine("var target = __XWGetStateMachineInstance(context.Host);");
					AddLine("object previousHost = target.__XWStateMachineRuntimeHost;");
					AddLine("target.__XWStateMachineRuntimeHost = context.Host;");
					AddLine("try");
					AddLine("{");
					IndentLevel++;
					AddLine("target." + text2 + "();");
					IndentLevel--;
					AddLine("}");
					AddLine("finally");
					AddLine("{");
					IndentLevel++;
					AddLine("target.__XWStateMachineRuntimeHost = previousHost;");
					IndentLevel--;
					AddLine("}");
					IndentLevel--;
					AddLine("}");
					break;
				case StateMachineCallbackPhase.Process:
				case StateMachineCallbackPhase.PhysicsProcess:
					AddLine("public static void " + text3 + "(in StateMachineCallbackContext context, double delta)");
					AddLine("{");
					IndentLevel++;
					AddLine("var target = __XWGetStateMachineInstance(context.Host);");
					AddLine("object previousHost = target.__XWStateMachineRuntimeHost;");
					AddLine("target.__XWStateMachineRuntimeHost = context.Host;");
					AddLine("try");
					AddLine("{");
					IndentLevel++;
					AddLine("target." + text2 + "(delta);");
					IndentLevel--;
					AddLine("}");
					AddLine("finally");
					AddLine("{");
					IndentLevel++;
					AddLine("target.__XWStateMachineRuntimeHost = previousHost;");
					IndentLevel--;
					AddLine("}");
					IndentLevel--;
					AddLine("}");
					break;
				case StateMachineCallbackPhase.Guard:
					AddLine("public static bool " + text3 + "(in StateMachineGuardContext context)");
					AddLine("{");
					IndentLevel++;
					AddLine("var target = __XWGetStateMachineInstance(context.Host);");
					AddLine("object previousHost = target.__XWStateMachineRuntimeHost;");
					AddLine("target.__XWStateMachineRuntimeHost = context.Host;");
					AddLine("try");
					AddLine("{");
					IndentLevel++;
					AddLine("return target." + text2 + "();");
					IndentLevel--;
					AddLine("}");
					AddLine("finally");
					AddLine("{");
					IndentLevel++;
					AddLine("target.__XWStateMachineRuntimeHost = previousHost;");
					IndentLevel--;
					AddLine("}");
					IndentLevel--;
					AddLine("}");
					break;
				}
				AddLine();
			}
		}
	}

	public static bool TryValidateStateMachineCallbackSignature(XWBPFunctionData function, StateMachineCallbackPhase phase, out string error)
	{
		error = "";
		if (function != null)
		{
			foreach (XWBPNodeData value in function.Nodes.Values)
			{
				string text = value?.TypeId ?? ((StringName)"");
				if (text == "__XWBPGraphNode_Delay")
				{
					error = "状态机动作必须同步完成，不能使用“延迟”节点。";
					return false;
				}
				bool flag = phase == StateMachineCallbackPhase.Guard;
				if (flag)
				{
					bool flag2;
					switch (text)
					{
					case "__XWBPGraphNode_RandomFloat":
					case "__XWBPGraphNode_RandomInt":
					case "__XWBPGraphNode_SetProperty":
					case "__XWBPGraphNode_LoadResource":
					case "__XWBPGraphNode_EmitSignal":
					case "__XWBPGraphNode_TriggerEvent":
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					flag = flag2;
				}
				if (flag)
				{
					error = "条件蓝图必须可重复且无副作用，不能使用随机、写属性、加载资源或发送事件节点。";
					return false;
				}
			}
		}
		return TryValidateStateMachineCallbackPortSignature(function, phase, out error);
	}

	public static bool TryValidateStateMachineCallbackSignature(XWBPScriptData scriptData, XWBPFunctionData function, StateMachineCallbackPhase phase, out string error)
	{
		if (!TryValidateStateMachineCallbackSignature(function, phase, out error))
		{
			return false;
		}
		if (scriptData?.Functions == null)
		{
			return true;
		}
		int num = ResolveFunctionId(scriptData, function);
		if (num <= 0)
		{
			return true;
		}
		System.Collections.Generic.Dictionary<int, byte> visitStates = new System.Collections.Generic.Dictionary<int, byte>();
		List<int> callPath = new List<int>();
		int inspectedNodeCount = 0;
		return TryValidateReachableStateMachineCallbackFunction(scriptData, num, phase, visitStates, callPath, ref inspectedNodeCount, out error);
	}

	public static bool TryValidateStateMachineCallbackPortSignature(XWBPFunctionData function, StateMachineCallbackPhase phase, out string error)
	{
		error = "";
		if (function == null || string.IsNullOrWhiteSpace(function.Name))
		{
			error = "函数必须有名称。";
			return false;
		}
		switch (phase)
		{
		case StateMachineCallbackPhase.Enter:
		case StateMachineCallbackPhase.Exit:
			if (function.Inputs.Count == 0 && function.Outputs.Count == 0)
			{
				return true;
			}
			error = "进入和退出动作必须是无输入、无返回值的函数。";
			return false;
		case StateMachineCallbackPhase.Process:
		case StateMachineCallbackPhase.PhysicsProcess:
			if (function.Inputs.Count == 1)
			{
				XWBPNodePortData xWBPNodePortData2 = function.Inputs[0];
				if (xWBPNodePortData2 != null && xWBPNodePortData2.PortTypeValue == XWBPNodePortData.PortType.Float && function.Outputs.Count == 0)
				{
					return true;
				}
			}
			error = "每帧动作必须只有一个浮点输入（delta），且没有返回值。";
			return false;
		case StateMachineCallbackPhase.Guard:
			if (function.Inputs.Count == 0 && function.Outputs.Count == 1)
			{
				XWBPNodePortData xWBPNodePortData = function.Outputs[0];
				if (xWBPNodePortData != null && xWBPNodePortData.PortTypeValue == XWBPNodePortData.PortType.Bool)
				{
					return true;
				}
			}
			error = "条件动作必须无输入，并且只返回一个布尔值。";
			return false;
		default:
			error = "阶段不受支持。";
			return false;
		}
	}

	public static bool TryGetBlueprintFunctionCallTarget(XWBPNodeData node, out int functionId)
	{
		functionId = 0;
		if (node == null || node.TypeId != (StringName)"__XWBPGraphNode_CallMethod" || node.GetMetaData("MethodType").AsInt32() != 0)
		{
			return false;
		}
		functionId = node.GetMetaData("FunctionId").AsInt32();
		return true;
	}

	private static int ResolveFunctionId(XWBPScriptData scriptData, XWBPFunctionData function)
	{
		if (function == null)
		{
			return 0;
		}
		if (function.Id > 0 && scriptData.Functions.TryGetValue(function.Id, out var value) && value == function)
		{
			return function.Id;
		}
		foreach (var (result, xWBPFunctionData2) in scriptData.Functions)
		{
			if (xWBPFunctionData2 == function)
			{
				return result;
			}
		}
		return 0;
	}

	private static bool TryValidateReachableStateMachineCallbackFunction(XWBPScriptData scriptData, int functionId, StateMachineCallbackPhase phase, System.Collections.Generic.Dictionary<int, byte> visitStates, List<int> callPath, ref int inspectedNodeCount, out string error)
	{
		error = "";
		if (visitStates.TryGetValue(functionId, out var value))
		{
			if (value == 2)
			{
				return true;
			}
			int val = callPath.IndexOf(functionId);
			List<string> list = new List<string>();
			for (int i = Math.Max(0, val); i < callPath.Count; i++)
			{
				list.Add(FormatFunctionName(scriptData, callPath[i]));
			}
			list.Add(FormatFunctionName(scriptData, functionId));
			error = "[BP_CALLBACK_RECURSION] 状态机回调的蓝图函数调用链存在递归或循环：" + string.Join(" -> ", list) + "。请先断开循环调用，避免游戏主线程卡死。";
			return false;
		}
		if (callPath.Count >= 128)
		{
			error = $"[BP_CALLBACK_COMPLEXITY] 状态机回调的函数调用深度超过 {128} 层，已拒绝生成。";
			return false;
		}
		if (!scriptData.Functions.TryGetValue(functionId, out var value2) || value2 == null)
		{
			error = $"[BP_CALLBACK_MISSING_FUNCTION] 状态机回调调用了不存在的蓝图函数 #{functionId}。";
			return false;
		}
		visitStates[functionId] = 1;
		callPath.Add(functionId);
		foreach (XWBPNodeData value3 in value2.Nodes.Values)
		{
			inspectedNodeCount++;
			if (inspectedNodeCount > 10000)
			{
				error = $"[BP_CALLBACK_COMPLEXITY] 状态机回调可达节点超过 {10000} 个，已拒绝生成。";
				return false;
			}
			string text = value3?.TypeId ?? ((StringName)"");
			if (text == "__XWBPGraphNode_Delay")
			{
				error = "[BP_CALLBACK_DELAY] 状态机回调必须同步完成；可达函数“" + FormatFunctionName(scriptData, functionId) + "”中包含“延迟”节点。";
				return false;
			}
			bool flag = phase == StateMachineCallbackPhase.Guard;
			if (flag)
			{
				bool flag2;
				switch (text)
				{
				case "__XWBPGraphNode_RandomFloat":
				case "__XWBPGraphNode_RandomInt":
				case "__XWBPGraphNode_SetProperty":
				case "__XWBPGraphNode_LoadResource":
				case "__XWBPGraphNode_EmitSignal":
				case "__XWBPGraphNode_TriggerEvent":
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				error = "[BP_CALLBACK_GUARD_SIDE_EFFECT] 条件蓝图必须可重复且无副作用；" + $"可达函数“{FormatFunctionName(scriptData, functionId)}”中包含不安全节点 {text}。";
				return false;
			}
			if (TryGetBlueprintFunctionCallTarget(value3, out var functionId2) && !TryValidateReachableStateMachineCallbackFunction(scriptData, functionId2, phase, visitStates, callPath, ref inspectedNodeCount, out error))
			{
				return false;
			}
		}
		callPath.RemoveAt(callPath.Count - 1);
		visitStates[functionId] = 2;
		return true;
	}

	private static string FormatFunctionName(XWBPScriptData scriptData, int functionId)
	{
		if (scriptData.Functions.TryGetValue(functionId, out var value) && !string.IsNullOrWhiteSpace(value?.Name))
		{
			return $"{value.Name}#{functionId}";
		}
		return $"函数#{functionId}";
	}

	public static string FormatStateMachineCallbackPhase(StateMachineCallbackPhase phase)
	{
		return phase switch
		{
			StateMachineCallbackPhase.Enter => "进入", 
			StateMachineCallbackPhase.Exit => "退出", 
			StateMachineCallbackPhase.Process => "每帧", 
			StateMachineCallbackPhase.PhysicsProcess => "物理帧", 
			StateMachineCallbackPhase.Guard => "条件", 
			_ => phase.ToString(), 
		};
	}

	private static string EscapeCSharpString(string value)
	{
		return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r")
			.Replace("\n", "\\n");
	}

	public void GenerateGraph(XWBPGraphData graph)
	{
		NodeVariables.Clear();
		List<XWBPNodeData> list = new List<XWBPNodeData>();
		List<XWBPNodeData> list2 = new List<XWBPNodeData>();
		List<XWBPNodeData> list3 = new List<XWBPNodeData>();
		foreach (XWBPNodeData value12 in graph.Nodes.Values)
		{
			if (value12.TypeId == (StringName)"__XWBPGraphNode_MethodEntry")
			{
				if (value12.GetMetaData("MethodType").AsInt32() == 2)
				{
					list.Add(value12);
				}
			}
			else if (value12.TypeId == (StringName)"__XWBPGraphNode_SignalEvent")
			{
				list2.Add(value12);
			}
			else if (value12.TypeId == (StringName)"__XWBPGraphNode_CallMethod" && value12.GetMetaData("MethodType").AsInt32() == 4)
			{
				list3.Add(value12);
			}
		}
		if (list.Count == 0 && list2.Count == 0 && list3.Count == 0)
		{
			return;
		}
		foreach (XWBPNodeData item in list)
		{
			NodeVariables.Clear();
			Dictionary dictionary = item.GetMetaData("MethodData").As<Dictionary>();
			Variant value;
			string text = (dictionary.TryGetValue("name", out value) ? value.AsStringName().ToString() : "");
			Variant value2;
			Godot.Collections.Array args = (dictionary.TryGetValue("args", out value2) ? value2.As<Godot.Collections.Array>() : new Godot.Collections.Array());
			string cSharpMemberName = GetCSharpMemberName(dictionary, text);
			int num;
			if (!(text == "_ready"))
			{
				num = ((cSharpMemberName == "_Ready") ? 1 : 0);
				if (num == 0)
				{
					goto IL_01ce;
				}
			}
			else
			{
				num = 1;
			}
			HasReadyFunction = true;
			goto IL_01ce;
			IL_01ce:
			Variant value3;
			Dictionary obj = (dictionary.TryGetValue("return", out value3) ? value3.As<Dictionary>() : new Dictionary());
			Variant value4;
			int num2 = (obj.TryGetValue("type", out value4) ? value4.AsInt32() : 0);
			Variant value5;
			StringName returnClassName = (obj.TryGetValue("class_name", out value5) ? value5.AsStringName() : ((StringName)""));
			Variant value6;
			string returnCsTypeName = (obj.TryGetValue("cs_type_name", out value6) ? value6.AsString() : "");
			int count = CodeLines.Count;
			GenerateVirtualMethodHeader(cSharpMemberName, args, num2, returnClassName, returnCsTypeName);
			IndentLevel++;
			if (num != 0 && SignalConnections.Count > 0)
			{
				foreach (Variant key2 in SignalConnections.Keys)
				{
					Dictionary dictionary2 = SignalConnections[key2].As<Dictionary>();
					string snakeName = dictionary2["signal_name"].AsString();
					string godotName = dictionary2["callback_name"].AsString();
					string text2 = ToPascalCase(snakeName);
					string value7 = ToCsMethodName(godotName);
					AddLine("EmitSignal(SignalName." + text2 + ");");
					AddLine($"Connect(SignalName.{text2}, new Callable(this, MethodName.{value7}));");
				}
			}
			GenerateNodeCodeFromGraph(item, graph);
			if (num2 != 0)
			{
				AddLine("return default;");
			}
			_ = CodeLines.Count - count;
			_ = 1;
			IndentLevel--;
			AddLine("}");
			AddLine();
		}
		foreach (XWBPNodeData item2 in list2)
		{
			NodeVariables.Clear();
			int key = item2.GetMetaData("SignalId").AsInt32();
			if (ScriptData.SignalDatas.ContainsKey(key))
			{
				XWBPSignalData xWBPSignalData = ScriptData.SignalDatas[key];
				Variant value8;
				string godotName2 = (SignalCallbackNames.TryGetValue(item2.Id, out value8) ? value8.AsString() : ("_on_" + xWBPSignalData.Name));
				int count2 = CodeLines.Count;
				GenerateSignalCallbackHeader(ToCsMethodName(godotName2), xWBPSignalData);
				IndentLevel++;
				GenerateNodeCodeFromGraph(item2, graph);
				_ = CodeLines.Count;
				IndentLevel--;
				AddLine("}");
				AddLine();
			}
		}
		foreach (XWBPNodeData item3 in list3)
		{
			NodeVariables.Clear();
			Dictionary dictionary3 = item3.GetMetaData("MethodData").As<Dictionary>();
			Variant value9;
			string text3 = (dictionary3.TryGetValue("name", out value9) ? value9.AsStringName().ToString() : "");
			Variant value10;
			Godot.Collections.Array args2 = (dictionary3.TryGetValue("args", out value10) ? value10.As<Godot.Collections.Array>() : new Godot.Collections.Array());
			Variant value11;
			string godotName3 = (SignalCallbackNames.TryGetValue(item3.Id, out value11) ? value11.AsString() : ("_on_" + text3));
			int count3 = CodeLines.Count;
			GenerateParentSignalCallbackHeader(ToCsMethodName(godotName3), args2);
			IndentLevel++;
			GenerateNodeCodeFromGraph(item3, graph);
			_ = CodeLines.Count;
			IndentLevel--;
			AddLine("}");
			AddLine();
		}
	}

	public void GenerateFunction(XWBPFunctionData function)
	{
		NodeVariables.Clear();
		XWBPNodeData value;
		XWBPNodeData xWBPNodeData = (function.Nodes.TryGetValue(-100000, out value) ? value : null);
		if (xWBPNodeData == null)
		{
			return;
		}
		int num = xWBPNodeData.GetMetaData("MethodType").AsInt32();
		if (num == 2)
		{
			return;
		}
		string text = "";
		int count = CodeLines.Count;
		if (num != 0 && num != 1)
		{
			return;
		}
		text = ToCsMethodName(function.Name);
		GenerateBPFunctionHeader(text, function.Inputs, function.Outputs);
		IndentLevel++;
		GenerateNodeCode(xWBPNodeData, function);
		bool flag = false;
		foreach (XWBPNodeData value2 in function.Nodes.Values)
		{
			if (value2.TypeId == (StringName)"__XWBPGraphNode_Return")
			{
				flag = true;
				break;
			}
		}
		if (function.Outputs.Count > 0 && !flag)
		{
			List<string> list = new List<string>();
			foreach (XWBPNodePortData output in function.Outputs)
			{
				list.Add(GetOutputValue(xWBPNodeData, output.Name, function));
			}
			if (list.Count == 1)
			{
				AddLine("return " + list[0] + ";");
			}
			else
			{
				AddLine("return new object[] { " + string.Join(", ", list) + " };");
			}
		}
		else if (function.Outputs.Count > 0)
		{
			AddLine("return default;");
		}
		_ = CodeLines.Count;
		IndentLevel--;
		AddLine("}");
		AddLine();
	}

	public void GenerateVirtualMethodHeader(string methodName, Godot.Collections.Array args, int returnType = 0, StringName returnClassName = null, string returnCsTypeName = "")
	{
		List<string> list = new List<string>();
		int argIndex = 0;
		foreach (Variant arg in args)
		{
			Dictionary dictionary = arg.As<Dictionary>();
			Variant value;
			string name = (dictionary.TryGetValue("name", out value) ? value.AsString() : "arg");
			Variant value2;
			int type = (dictionary.TryGetValue("type", out value2) ? value2.AsInt32() : 0);
			Variant value3;
			string text = (dictionary.TryGetValue("class_name", out value3) ? value3.AsStringName().ToString() : "");
			Variant value4;
			string text2 = ((dictionary.TryGetValue("cs_type_name", out value4) && !string.IsNullOrWhiteSpace(value4.AsString())) ? value4.AsString() : GetTypeName(type, text));
			string text3 = ToValidIdentifier(name, ref argIndex);
			list.Add(text2 + " " + text3);
		}
		string value5 = ((returnType == 0) ? "void" : ((!string.IsNullOrWhiteSpace(returnCsTypeName)) ? returnCsTypeName : GetTypeName(returnType, returnClassName)));
		AddLine($"public override {value5} {methodName}({string.Join(", ", list)})");
		AddLine("{");
	}

	public void GenerateSignalCallbackHeader(string callbackName, XWBPSignalData signalData)
	{
		List<string> list = new List<string>();
		int argIndex = 0;
		foreach (XWBPNodePortData input in signalData.Inputs)
		{
			string typeName = GetTypeName((int)input.PortTypeValue, input.ClassName);
			string text = ToValidIdentifier(input.Name, ref argIndex);
			list.Add(typeName + " " + text);
		}
		AddLine($"public void {callbackName}({string.Join(", ", list)})");
		AddLine("{");
	}

	public void GenerateParentSignalCallbackHeader(string callbackName, Godot.Collections.Array args)
	{
		List<string> list = new List<string>();
		int argIndex = 0;
		foreach (Variant arg in args)
		{
			Dictionary dictionary = arg.As<Dictionary>();
			Variant value;
			string name = (dictionary.TryGetValue("name", out value) ? value.AsString() : "arg");
			Variant value2;
			int type = (dictionary.TryGetValue("type", out value2) ? value2.AsInt32() : 0);
			Variant value3;
			string text = (dictionary.TryGetValue("class_name", out value3) ? value3.AsStringName().ToString() : "");
			Variant value4;
			string text2 = ((dictionary.TryGetValue("cs_type_name", out value4) && !string.IsNullOrWhiteSpace(value4.AsString())) ? value4.AsString() : GetTypeName(type, text));
			string text3 = ToValidIdentifier(name, ref argIndex);
			list.Add(text2 + " " + text3);
		}
		AddLine($"public void {callbackName}({string.Join(", ", list)})");
		AddLine("{");
	}

	public void GenerateSignalConnections()
	{
		if (SignalConnections.Count == 0 || HasReadyFunction)
		{
			return;
		}
		AddLine("public override void _Ready()");
		AddLine("{");
		IndentLevel++;
		foreach (Variant key in SignalConnections.Keys)
		{
			Dictionary dictionary = SignalConnections[key].As<Dictionary>();
			string snakeName = dictionary["signal_name"].AsString();
			string godotName = dictionary["callback_name"].AsString();
			string value = ToPascalCase(snakeName);
			string value2 = ToCsMethodName(godotName);
			AddLine($"Connect(SignalName.{value}, new Callable(this, MethodName.{value2}));");
		}
		IndentLevel--;
		AddLine("}");
		AddLine();
	}

	public void CollectSignalConnections()
	{
		SignalConnections.Clear();
		SignalCallbackNames.Clear();
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>();
		foreach (int key4 in ScriptData.Graphs.Keys)
		{
			foreach (XWBPNodeData value4 in ScriptData.Graphs[key4].Nodes.Values)
			{
				if (value4.TypeId == (StringName)"__XWBPGraphNode_SignalEvent")
				{
					int key = value4.GetMetaData("SignalId").AsInt32();
					if (ScriptData.SignalDatas.ContainsKey(key))
					{
						string name = ScriptData.SignalDatas[key].Name;
						int value = (dictionary[name] = (dictionary.TryGetValue(name, out value) ? (value + 1) : 0));
						string text = "_on_" + name;
						if (value > 0)
						{
							text += $"_{value}";
						}
						Dictionary dictionary2 = new Dictionary
						{
							["signal_name"] = name,
							["callback_name"] = text
						};
						SignalConnections[value4.Id] = dictionary2;
						SignalCallbackNames[value4.Id] = text;
					}
				}
				else if (value4.TypeId == (StringName)"__XWBPGraphNode_CallMethod" && value4.GetMetaData("MethodType").AsInt32() == 4)
				{
					Variant value2;
					string text2 = (value4.GetMetaData("MethodData").As<Dictionary>().TryGetValue("name", out value2) ? value2.AsStringName().ToString() : "");
					int value3 = (dictionary[text2] = (dictionary.TryGetValue(text2, out value3) ? (value3 + 1) : 0));
					string text3 = "_on_" + text2;
					if (value3 > 0)
					{
						text3 += $"_{value3}";
					}
					Dictionary dictionary3 = new Dictionary
					{
						["signal_name"] = text2,
						["callback_name"] = text3
					};
					SignalConnections[value4.Id] = dictionary3;
					SignalCallbackNames[value4.Id] = text3;
				}
			}
		}
	}

	public void GenerateBPFunctionHeader(string functionName, List<XWBPNodePortData> inputs, List<XWBPNodePortData> outputs)
	{
		List<string> list = new List<string>();
		int argIndex = 0;
		foreach (XWBPNodePortData input in inputs)
		{
			string text = ToValidIdentifier(input.Name, ref argIndex);
			string typeName = GetTypeName((int)input.PortTypeValue, input.ClassName);
			string text2 = "";
			if (input.DefaultValue.VariantType != Variant.Type.Nil)
			{
				text2 = " = " + GetDefaultValueString(input.DefaultValue, (int)input.PortTypeValue);
			}
			list.Add(typeName + " " + text + text2);
		}
		string value = "void";
		if (outputs.Count > 0)
		{
			value = ((outputs.Count != 1) ? "object[]" : GetTypeName((int)outputs[0].PortTypeValue, outputs[0].ClassName));
		}
		AddLine($"public {value} {functionName}({string.Join(", ", list)})");
		AddLine("{");
	}

	public bool ShouldCallSuper(StringName methodName)
	{
		string text = ScriptData.ExtendsClass.ToString();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		foreach (Dictionary classMethod in XWClassRegistry.Instance.GetClassMethodList(text))
		{
			if (classMethod.TryGetValue("name", out var value) && value.AsStringName() == methodName)
			{
				return true;
			}
			if (classMethod.TryGetValue("cs_name", out var value2) && value2.AsString() == methodName.ToString())
			{
				return true;
			}
			Variant value3;
			string text2 = (classMethod.TryGetValue("name", out value3) ? value3.AsString() : "");
			if (!string.IsNullOrEmpty(text2) && ToCsMethodName(text2) == methodName.ToString())
			{
				return true;
			}
		}
		foreach (Dictionary classMethod2 in XWBPCSharpMemberRegistry.Instance.GetClassMethodList(text))
		{
			if (classMethod2.TryGetValue("name", out var value4) && value4.AsStringName() == methodName)
			{
				return true;
			}
			if (classMethod2.TryGetValue("cs_name", out var value5) && value5.AsString() == methodName.ToString())
			{
				return true;
			}
		}
		return false;
	}

	public string GetCSharpMemberName(Dictionary memberData, string fallbackName)
	{
		if (memberData.TryGetValue("cs_name", out var value))
		{
			string text = value.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		return ToCsMethodName(fallbackName);
	}

	public void GenerateSuperCall(StringName methodName, Godot.Collections.Array args)
	{
		string text = ToCsMethodName(methodName.ToString());
		if (args.Count == 0)
		{
			AddLine("base." + text + "();");
			return;
		}
		List<string> list = new List<string>();
		int argIndex = 0;
		foreach (Variant arg in args)
		{
			Variant value;
			string name = (arg.As<Dictionary>().TryGetValue("name", out value) ? value.AsString() : "arg");
			string item = ToValidIdentifier(name, ref argIndex);
			list.Add(item);
		}
		AddLine($"base.{text}({string.Join(", ", list)});");
	}

	public void GenerateNodeCode(XWBPNodeData node, XWBPFunctionData function)
	{
		if (node != null && function != null && !NodeVariables.ContainsKey(node.Id))
		{
			int id = node.Id;
			StringName typeId = node.TypeId;
			string value = $"var_{id}";
			NodeVariables[id] = value;
			XWBPNodeType nodeType = XWBPNodeRegistry.Instance.GetNodeType(typeId.ToString());
			if (nodeType == null)
			{
				throw new InvalidOperationException($"Unsupported blueprint node type '{typeId}' (node id {id}).");
			}
			nodeType.GenerateCode(this, node, function);
		}
	}

	public void GenerateNodeCodeFromGraph(XWBPNodeData node, XWBPGraphData graph)
	{
		if (node != null && graph != null && !NodeVariables.ContainsKey(node.Id))
		{
			int id = node.Id;
			StringName typeId = node.TypeId;
			string value = $"var_{id}";
			NodeVariables[id] = value;
			XWBPNodeType nodeType = XWBPNodeRegistry.Instance.GetNodeType(typeId.ToString());
			if (nodeType == null)
			{
				throw new InvalidOperationException($"Unsupported blueprint node type '{typeId}' (node id {id}).");
			}
			nodeType.GenerateCodeFromGraph(this, node, graph);
		}
	}

	public XWBPNodeData GetNextNode(XWBPNodeData node, int outputIndex, XWBPFunctionData function)
	{
		if (node == null || function == null)
		{
			return null;
		}
		foreach (XWBPNodeConnectionData connection in function.Connections)
		{
			XWBPNodeData value;
			if (connection != null && function.IsConnectionStructurallyValid(connection, ignoreOccupiedPorts: true) && connection.FromNodeId == node.Id && connection.FromPortIndex == outputIndex)
			{
				return function.Nodes.TryGetValue(connection.ToNodeId, out value) ? value : null;
			}
		}
		return null;
	}

	public XWBPNodeData GetNextNodeFromGraph(XWBPNodeData node, int outputIndex, XWBPGraphData graph)
	{
		if (node == null || graph == null)
		{
			return null;
		}
		foreach (XWBPNodeConnectionData connection in graph.Connections)
		{
			XWBPNodeData value;
			if (connection != null && graph.IsConnectionStructurallyValid(connection, ignoreOccupiedPorts: true) && connection.FromNodeId == node.Id && connection.FromPortIndex == outputIndex)
			{
				return graph.Nodes.TryGetValue(connection.ToNodeId, out value) ? value : null;
			}
		}
		return null;
	}

	public string GetInputValue(XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (node == null || function == null)
		{
			return "null";
		}
		XWBPNodePortData xWBPNodePortData = node.FindInputPortByName(portName);
		if (xWBPNodePortData == null)
		{
			return "null";
		}
		int num = node.InputPorts.IndexOf(xWBPNodePortData);
		foreach (XWBPNodeConnectionData connection in function.Connections)
		{
			if (connection == null || !function.IsConnectionStructurallyValid(connection, ignoreOccupiedPorts: true) || connection.ToNodeId != node.Id || connection.ToPortIndex != num)
			{
				continue;
			}
			XWBPNodeData value;
			XWBPNodeData xWBPNodeData = (function.Nodes.TryGetValue(connection.FromNodeId, out value) ? value : null);
			if (xWBPNodeData != null)
			{
				if (!NodeVariables.ContainsKey(xWBPNodeData.Id))
				{
					GenerateNodeCode(xWBPNodeData, function);
				}
				XWBPNodePortData outputPort = xWBPNodeData.GetOutputPort(connection.FromPortIndex);
				if (outputPort != null)
				{
					return GetOutputValue(xWBPNodeData, outputPort.Name, function);
				}
			}
		}
		return GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
	}

	public string GetOutputValue(XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (node == null)
		{
			return "null";
		}
		XWBPNodePortData xWBPNodePortData = node.FindOutputPortByName(portName);
		if (xWBPNodePortData == null)
		{
			return "null";
		}
		if (node.TypeId == (StringName)"__XWBPGraphNode_MethodEntry")
		{
			return ToValidIdentifier(portName);
		}
		XWBPNodeType nodeType = XWBPNodeRegistry.Instance.GetNodeType(node.TypeId.ToString());
		if (nodeType != null)
		{
			return nodeType.GetOutputValue(this, node, portName, function);
		}
		return GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
	}

	public string GetInputValueFromGraph(XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (node == null || graph == null)
		{
			return "null";
		}
		XWBPNodePortData xWBPNodePortData = node.FindInputPortByName(portName);
		if (xWBPNodePortData == null)
		{
			return "null";
		}
		int num = node.InputPorts.IndexOf(xWBPNodePortData);
		foreach (XWBPNodeConnectionData connection in graph.Connections)
		{
			if (connection == null || !graph.IsConnectionStructurallyValid(connection, ignoreOccupiedPorts: true) || connection.ToNodeId != node.Id || connection.ToPortIndex != num)
			{
				continue;
			}
			XWBPNodeData value;
			XWBPNodeData xWBPNodeData = (graph.Nodes.TryGetValue(connection.FromNodeId, out value) ? value : null);
			if (xWBPNodeData != null)
			{
				if (!NodeVariables.ContainsKey(xWBPNodeData.Id))
				{
					GenerateNodeCodeFromGraph(xWBPNodeData, graph);
				}
				XWBPNodePortData outputPort = xWBPNodeData.GetOutputPort(connection.FromPortIndex);
				if (outputPort != null)
				{
					return GetOutputValueFromGraph(xWBPNodeData, outputPort.Name, graph);
				}
			}
		}
		return GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
	}

	public string GetOutputValueFromGraph(XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (node == null)
		{
			return "null";
		}
		XWBPNodePortData xWBPNodePortData = node.FindOutputPortByName(portName);
		if (xWBPNodePortData == null)
		{
			return "null";
		}
		if (node.TypeId == (StringName)"__XWBPGraphNode_MethodEntry")
		{
			return ToValidIdentifier(portName);
		}
		XWBPNodeType nodeType = XWBPNodeRegistry.Instance.GetNodeType(node.TypeId.ToString());
		if (nodeType != null)
		{
			return nodeType.GetOutputValueFromGraph(this, node, portName, graph);
		}
		return GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
	}

	public string GetTypeName(int type, StringName className = null)
	{
		if (type == 24)
		{
			string text = className.ToString();
			if (string.IsNullOrEmpty(text))
			{
				return "GodotObject";
			}
			return ToCsClassName(text);
		}
		string typeName = XWTypeRegistry.Instance.GetTypeName((Variant.Type)type);
		if (string.IsNullOrEmpty(typeName))
		{
			return "Variant";
		}
		return ToCsTypeName(typeName);
	}

	public string GetDefaultValueString(Variant value, int type)
	{
		if (value.VariantType == Variant.Type.Nil)
		{
			return XWTypeRegistry.Instance.GetTypeDefaultValueString((Variant.Type)type);
		}
		switch (type)
		{
		case 4:
			return "\"" + value.AsString().Replace("\"", "\\\"") + "\"";
		case 21:
			return "new StringName(\"" + value.AsStringName().ToString().Replace("\"", "\\\"") + "\")";
		case 1:
			if (!value.AsBool())
			{
				return "false";
			}
			return "true";
		case 2:
			return value.AsInt64().ToString();
		case 3:
			return value.AsDouble().ToString(CultureInfo.InvariantCulture) + "d";
		case 5:
			return $"new Vector2({value.AsVector2().X.ToString(CultureInfo.InvariantCulture)}f, {value.AsVector2().Y.ToString(CultureInfo.InvariantCulture)}f)";
		case 6:
			return $"new Vector2I({value.AsVector2I().X}, {value.AsVector2I().Y})";
		case 9:
		{
			Vector3 vector2 = value.AsVector3();
			return $"new Vector3({vector2.X.ToString(CultureInfo.InvariantCulture)}f, {vector2.Y.ToString(CultureInfo.InvariantCulture)}f, {vector2.Z.ToString(CultureInfo.InvariantCulture)}f)";
		}
		case 10:
		{
			Vector3I vector3I = value.AsVector3I();
			return $"new Vector3I({vector3I.X}, {vector3I.Y}, {vector3I.Z})";
		}
		case 12:
		{
			Vector4 vector = value.AsVector4();
			return $"new Vector4({vector.X.ToString(CultureInfo.InvariantCulture)}f, {vector.Y.ToString(CultureInfo.InvariantCulture)}f, {vector.Z.ToString(CultureInfo.InvariantCulture)}f, {vector.W.ToString(CultureInfo.InvariantCulture)}f)";
		}
		case 13:
		{
			Vector4I vector4I = value.AsVector4I();
			return $"new Vector4I({vector4I.X}, {vector4I.Y}, {vector4I.Z}, {vector4I.W})";
		}
		case 20:
		{
			Color color = value.AsColor();
			return $"new Color({color.R.ToString(CultureInfo.InvariantCulture)}f, {color.G.ToString(CultureInfo.InvariantCulture)}f, {color.B.ToString(CultureInfo.InvariantCulture)}f, {color.A.ToString(CultureInfo.InvariantCulture)}f)";
		}
		case 7:
		{
			Rect2 rect = value.AsRect2();
			return $"new Rect2({rect.Position.X.ToString(CultureInfo.InvariantCulture)}f, {rect.Position.Y.ToString(CultureInfo.InvariantCulture)}f, {rect.Size.X.ToString(CultureInfo.InvariantCulture)}f, {rect.Size.Y.ToString(CultureInfo.InvariantCulture)}f)";
		}
		case 8:
		{
			Rect2I rect2I = value.AsRect2I();
			return $"new Rect2I({rect2I.Position.X}, {rect2I.Position.Y}, {rect2I.Size.X}, {rect2I.Size.Y})";
		}
		case 28:
			return "new Godot.Collections.Array()";
		case 27:
			return "new Godot.Collections.Dictionary()";
		default:
			return value.ToString();
		}
	}

	public string ToCsTypeName(string godotTypeName)
	{
		return godotTypeName switch
		{
			"bool" => "bool", 
			"int" => "long", 
			"float" => "double", 
			"String" => "string", 
			"StringName" => "StringName", 
			"NodePath" => "NodePath", 
			"Variant" => "Variant", 
			"Object" => "GodotObject", 
			"Vector2" => "Vector2", 
			"Vector2i" => "Vector2I", 
			"Vector3" => "Vector3", 
			"Vector3i" => "Vector3I", 
			"Vector4" => "Vector4", 
			"Vector4i" => "Vector4I", 
			"Rect2" => "Rect2", 
			"Rect2i" => "Rect2I", 
			"Color" => "Color", 
			"Array" => "Godot.Collections.Array", 
			"Dictionary" => "Godot.Collections.Dictionary", 
			"Transform2D" => "Transform2D", 
			"Transform3D" => "Transform3D", 
			"Basis" => "Basis", 
			"Quaternion" => "Quaternion", 
			"AABB" => "Aabb", 
			"Plane" => "Plane", 
			"RID" => "Rid", 
			"Callable" => "Callable", 
			"Signal" => "Signal", 
			"PackedByteArray" => "byte[]", 
			"PackedInt32Array" => "int[]", 
			"PackedInt64Array" => "long[]", 
			"PackedFloat32Array" => "float[]", 
			"PackedFloat64Array" => "double[]", 
			"PackedStringArray" => "string[]", 
			"PackedVector2Array" => "Vector2[]", 
			"PackedVector3Array" => "Vector3[]", 
			"PackedColorArray" => "Color[]", 
			"PackedVector4Array" => "Vector4[]", 
			_ => godotTypeName, 
		};
	}

	public string ToCsClassName(string godotClassName)
	{
		if (godotClassName == "Object")
		{
			return "GodotObject";
		}
		return godotClassName;
	}

	public string ToCsMethodName(string godotName)
	{
		if (string.IsNullOrEmpty(godotName))
		{
			return godotName;
		}
		if (godotName.StartsWith("_") && godotName.Length > 1)
		{
			return "_" + ToPascalCase(godotName.Substring(1));
		}
		return ToPascalCase(godotName);
	}

	public string ToPascalCase(string snakeName)
	{
		if (string.IsNullOrEmpty(snakeName))
		{
			return snakeName;
		}
		StringBuilder stringBuilder = new StringBuilder(snakeName.Length);
		bool flag = true;
		foreach (char c in snakeName)
		{
			if (c == '_')
			{
				flag = true;
			}
			else if (flag)
			{
				stringBuilder.Append(char.ToUpperInvariant(c));
				flag = false;
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	public string ToValidIdentifier(string name)
	{
		int argIndex = 0;
		return ToValidIdentifier(name, ref argIndex);
	}

	public string ToValidIdentifier(string name, ref int argIndex)
	{
		if (string.IsNullOrEmpty(name))
		{
			name = "arg";
		}
		if (!char.IsLetter(name[0]) && name[0] != '_')
		{
			name = "_" + name;
		}
		StringBuilder stringBuilder = new StringBuilder(name.Length);
		string text = name;
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c) || c == '_')
			{
				stringBuilder.Append(c);
			}
			else
			{
				stringBuilder.Append('_');
			}
		}
		string text2 = stringBuilder.ToString();
		switch (text2)
		{
		case "bool":
		case "byte":
		case "base":
		case "uint":
		case "long":
		case "lock":
		case "char":
		case "case":
		case "void":
		case "null":
		case "true":
		case "this":
		case "else":
		case "enum":
		case "goto":
		case "when":
		case "into":
		case "join":
		case "from":
		case "sbyte":
		case "short":
		case "ulong":
		case "using":
		case "float":
		case "false":
		case "fixed":
		case "while":
		case "where":
		case "break":
		case "yield":
		case "catch":
		case "class":
		case "const":
		case "throw":
		case "event":
		case "value":
		case "async":
		case "await":
		case "group":
		case "ushort":
		case "double":
		case "string":
		case "struct":
		case "static":
		case "object":
		case "switch":
		case "return":
		case "select":
		case "public":
		case "params":
		case "typeof":
		case "sizeof":
		case "unsafe":
		case "extern":
		case "global":
		case "equals":
		case "int":
		case "var":
		case "for":
		case "try":
		case "new":
		case "ref":
		case "out":
		case "get":
		case "set":
		case "decimal":
		case "foreach":
		case "partial":
		case "default":
		case "finally":
		case "private":
		case "checked":
		case "orderby":
		case "if":
		case "in":
		case "is":
		case "do":
		case "as":
		case "by":
		case "on":
		case "continue":
		case "delegate":
		case "internal":
		case "implicit":
		case "readonly":
		case "operator":
		case "explicit":
		case "namespace":
		case "protected":
		case "unchecked":
		case "ascending":
		case "interface":
		case "descending":
		case "stackalloc":
			text2 = "@" + text2;
			break;
		}
		return text2;
	}
}
