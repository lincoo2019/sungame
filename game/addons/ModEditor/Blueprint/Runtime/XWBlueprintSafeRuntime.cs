using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Debugging;
using PVZHE.ModEditor.Registry.BP;

namespace PVZHE.ModEditor.Blueprint;

public sealed class XWBlueprintSafeRuntime
{
	private sealed class RuntimeFrame
	{
		public XWBPGraphData Graph { get; init; }

		public int FunctionId { get; init; }

		public string FunctionName { get; init; } = string.Empty;

		public System.Collections.Generic.Dictionary<(int NodeId, int PortIndex), Variant> DynamicOutputs { get; } = new System.Collections.Generic.Dictionary<(int, int), Variant>();

		public HashSet<(int NodeId, int PortIndex)> ValueStack { get; } = new HashSet<(int, int)>();

		public System.Collections.Generic.Dictionary<string, Variant> Variables { get; } = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.Ordinal);

		public Variant ReturnValue { get; set; }
	}

	private enum FlowSignal
	{
		Continue,
		Break,
		ContinueLoop,
		Return,
		Stop
	}

	private XWBPGraphData _graph;

	private readonly XWBlueprintRuntimeContext _context;

	private readonly XWBlueprintRuntimeResult _result = new XWBlueprintRuntimeResult();

	private System.Collections.Generic.Dictionary<(int NodeId, int PortIndex), Variant> _dynamicOutputs = new System.Collections.Generic.Dictionary<(int, int), Variant>();

	private HashSet<(int NodeId, int PortIndex)> _valueStack = new HashSet<(int, int)>();

	private System.Collections.Generic.Dictionary<string, Variant> _variables = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<int, Variant> _blueprintVariables = new System.Collections.Generic.Dictionary<int, Variant>();

	private readonly HashSet<ulong> _safeObjectIds = new HashSet<ulong>();

	private readonly System.Collections.Generic.Dictionary<string, int> _nodeVisitCounts = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

	private readonly HashSet<int> _activeFunctionIds = new HashSet<int>();

	private readonly List<XWModDebugStackFrame> _debugCallStack = new List<XWModDebugStackFrame>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private readonly XWBlueprintSafeObject _selfObject;

	private RuntimeFrame _currentFrame;

	private int _clonedValueCount;

	private int _loopIterationCount;

	private int _currentFunctionId;

	private string _currentFunctionName = string.Empty;

	private long _debugWaitMilliseconds;

	private bool _stopped;

	private XWBlueprintSafeRuntime(XWBPGraphData graph, XWBlueprintRuntimeContext context)
	{
		_context = context ?? new XWBlueprintRuntimeContext();
		SwitchToFrame(new RuntimeFrame
		{
			Graph = graph
		});
		_selfObject = CreateSelfObject();
		RegisterSafeObject(_selfObject);
		if (_context.BlueprintVariables == null)
		{
			return;
		}
		foreach (KeyValuePair<int, Variant> blueprintVariable in _context.BlueprintVariables)
		{
			blueprintVariable.Deconstruct(out var key, out var value);
			int key2 = key;
			Variant value2 = value;
			Variant value3 = CloneSafe(value2);
			if (_stopped)
			{
				break;
			}
			_blueprintVariables[key2] = value3;
		}
	}

	public static async Task<XWBlueprintRuntimeResult> ExecuteGraphAsync(XWBPGraphData graph, XWBlueprintRuntimeContext context = null)
	{
		return await new XWBlueprintSafeRuntime(graph, context).ExecuteAsync();
	}

	private async Task<XWBlueprintRuntimeResult> ExecuteAsync()
	{
		if (_stopped)
		{
			return Finish();
		}
		if (_graph == null)
		{
			Stop("No Blueprint graph is open.");
			return Finish();
		}
		XWBPNodeData xWBPNodeData = FindEntryNode();
		if (xWBPNodeData == null)
		{
			Stop(string.IsNullOrWhiteSpace(_context.EntryEventName) ? "The graph has no supported entry node." : ("The graph has no entry event named '" + _context.EntryEventName + "'."));
			return Finish();
		}
		FlowSignal flowSignal = await ExecuteFlowAsync(xWBPNodeData.Id);
		if (!_stopped && flowSignal == FlowSignal.Stop)
		{
			Stop("Blueprint preview stopped unexpectedly.");
		}
		return Finish();
	}

	private XWBlueprintRuntimeResult Finish()
	{
		_result.StepCount = Math.Max(_result.StepCount, 0);
		_result.Success = !_stopped && !_result.WasCancelled && _result.Diagnostics.Count == 0;
		return _result;
	}

	private XWBPNodeData FindEntryNode()
	{
		string text = _context.EntryEventName?.Trim() ?? string.Empty;
		IEnumerable<XWBPNodeData> enumerable = from node in _graph.Nodes.Values
			where node != null
			orderby node.Id
			select node;
		if (!string.IsNullOrEmpty(text))
		{
			foreach (XWBPNodeData item in enumerable)
			{
				if (!(item.TypeId.ToString() != "__XWBPGraphNode_CustomEvent") && string.Equals(item.GetMetaData("EventName").AsString(), text, StringComparison.Ordinal))
				{
					return item;
				}
			}
			return null;
		}
		string[] array = new string[4] { "__XWBPGraphNode_OnStart", "__XWBPGraphNode_Entry", "__XWBPGraphNode_MethodEntry", "__XWBPGraphNode_CustomEvent" };
		foreach (string typeId in array)
		{
			XWBPNodeData xWBPNodeData = enumerable.FirstOrDefault((XWBPNodeData node) => node.TypeId.ToString() == typeId);
			if (xWBPNodeData != null)
			{
				return xWBPNodeData;
			}
		}
		return null;
	}

	private async Task<FlowSignal> ExecuteFlowAsync(int nodeId)
	{
		if (_stopped)
		{
			return FlowSignal.Stop;
		}
		if (!_graph.Nodes.TryGetValue(nodeId, out var value) || value == null)
		{
			Stop($"Flow points to missing node {nodeId}.");
			return FlowSignal.Stop;
		}
		if (_context.DebugController == null)
		{
			return await ExecuteFlowNodeAsync(value, null);
		}
		IReadOnlyDictionary<string, XWModDebugVariable> readOnlyDictionary = CreateDebugVariableSnapshot();
		_debugCallStack.Add(new XWModDebugStackFrame(GetDebugNodeId(value.Id), GetNodeDisplayName(value), GetBlueprintSourcePath(), 0, GetDebugNodeId(value.Id), readOnlyDictionary));
		try
		{
			return await ExecuteFlowNodeAsync(value, readOnlyDictionary);
		}
		finally
		{
			_debugCallStack.RemoveAt(_debugCallStack.Count - 1);
		}
	}

	private async Task<FlowSignal> ExecuteFlowNodeAsync(XWBPNodeData node, IReadOnlyDictionary<string, XWModDebugVariable> debugVariables)
	{
		if (!(await EnterNodeAsync(node, debugVariables)))
		{
			return FlowSignal.Stop;
		}
		string text = node.TypeId.ToString();
		switch (text)
		{
		case "__XWBPGraphNode_OnStart":
		case "__XWBPGraphNode_Entry":
		case "__XWBPGraphNode_MethodEntry":
		case "__XWBPGraphNode_CustomEvent":
		case "__XWBPGraphNode_SignalEvent":
		case "__XWBPGraphNode_TimerEvent":
		case "__XWBPGraphNode_OnDestroy":
			return await ExecuteNextAsync(node, 0);
		case "__XWBPGraphNode_Branch":
		{
			bool flag = ToBool(await EvaluateDataInputAsync(node, 0));
			return await ExecuteNextAsync(node, (!flag) ? 1 : 0);
		}
		case "__XWBPGraphNode_Sequence":
			foreach (int item in FlowOutputIndexes(node))
			{
				FlowSignal flowSignal = await ExecuteNextByPortAsync(node, item);
				if (flowSignal != FlowSignal.Continue)
				{
					return flowSignal;
				}
			}
			return FlowSignal.Continue;
		case "__XWBPGraphNode_Print":
			WriteOutput(FormatValue(await EvaluateDataInputAsync(node, 0)));
			return await ExecuteNextAsync(node, 0);
		case "__XWBPGraphNode_PrintFormatted":
		{
			string format = FormatValue(await EvaluateDataInputAsync(node, 0));
			object[] args = new object[3];
			for (int index = 0; index < args.Length; index++)
			{
				object[] array = args;
				int i = index;
				array[i] = ToPlainObject(await EvaluateDataInputAsync(node, index + 1));
			}
			string message;
			try
			{
				message = string.Format(CultureInfo.InvariantCulture, format, args);
			}
			catch (FormatException ex)
			{
				Stop("Print format is invalid: " + ex.Message);
				return FlowSignal.Stop;
			}
			WriteOutput(message);
			return await ExecuteNextAsync(node, 0);
		}
		case "__XWBPGraphNode_EmitSignal":
		case "__XWBPGraphNode_TriggerEvent":
		{
			string obj = FormatValue(await EvaluateDataInputAsync(node, 0));
			_context.EventTriggered?.Invoke(obj);
			return await ExecuteNextAsync(node, 0);
		}
		case "__XWBPGraphNode_Delay":
		{
			double arg = Math.Clamp(ToDouble(await EvaluateDataInputAsync(node, 0)), 0.0, 10.0);
			if (_context.DelayAsync == null)
			{
				await YieldOnceAsync();
			}
			else
			{
				await _context.DelayAsync(arg);
			}
			return await ExecuteNextAsync(node, 0);
		}
		case "__XWBPGraphNode_ForLoop":
			return await ExecuteForLoopAsync(node);
		case "__XWBPGraphNode_ForEach":
			return await ExecuteForEachAsync(node);
		case "__XWBPGraphNode_WhileLoop":
			return await ExecuteWhileLoopAsync(node);
		case "__XWBPGraphNode_Match":
			return await ExecuteMatchAsync(node);
		case "__XWBPGraphNode_ArrayInsert":
		case "__XWBPGraphNode_ArrayRemove":
		case "__XWBPGraphNode_ArrayClear":
		case "__XWBPGraphNode_ArrayAdd":
		case "__XWBPGraphNode_ArraySet":
			if (!(await MutateArrayAsync(node, text)))
			{
				return FlowSignal.Stop;
			}
			return await ExecuteNextAsync(node, 0);
		case "__XWBPGraphNode_DictSet":
		case "__XWBPGraphNode_DictRemove":
		case "__XWBPGraphNode_DictClear":
			if (!(await MutateDictionaryAsync(node, text)))
			{
				return FlowSignal.Stop;
			}
			return await ExecuteNextAsync(node, 0);
		case "__XWBPGraphNode_SetProperty":
			if (!(await ExecuteSetPropertyAsync(node)))
			{
				return FlowSignal.Stop;
			}
			return await ExecuteNextAsync(node, 0);
		case "__XWBPGraphNode_CallMethod":
			if (!(await ExecuteCallMethodAsync(node)))
			{
				return FlowSignal.Stop;
			}
			return await ExecuteNextAsync(node, 0);
		case "__XWBPGraphNode_Return":
		{
			List<Variant> values = new List<Variant>();
			int index = DataInputIndexes(node).Count;
			for (int i = 0; i < index; i++)
			{
				List<Variant> list = values;
				list.Add(CloneSafe(await EvaluateDataInputAsync(node, i)));
			}
			Variant returnValue = values.Count switch
			{
				0 => default(Variant), 
				1 => values[0], 
				_ => Variant.From<Godot.Collections.Array>(new Godot.Collections.Array(values)), 
			};
			if (_currentFunctionId == 0)
			{
				_result.ReturnValue = returnValue;
			}
			else
			{
				_currentFrame.ReturnValue = returnValue;
			}
			return FlowSignal.Return;
		}
		case "__XWBPGraphNode_Break":
			return FlowSignal.Break;
		case "__XWBPGraphNode_Continue":
			return FlowSignal.ContinueLoop;
		case "__XWBPGraphNode_Breakpoint":
		case "__XWBPGraphNode_LocalVariable":
			return await ExecuteNextAsync(node, 0);
		case "__XWBPGraphNode_Comment":
		case "__XWBPGraphNode_Frame":
			return FlowSignal.Continue;
		default:
			Stop("Blueprint flow node '" + text + "' is not supported by safe preview yet.");
			return FlowSignal.Stop;
		}
	}

	private async Task<FlowSignal> ExecuteForLoopAsync(XWBPNodeData node)
	{
		long start = ToLong(await EvaluateDataInputAsync(node, 0));
		long end = ToLong(await EvaluateDataInputAsync(node, 1));
		long count = Math.Max(0L, end - start);
		if (!ConsumeLoopIterations(count, node.Id))
		{
			return FlowSignal.Stop;
		}
		int indexPort = DataOutputIndexes(node).FirstOrDefault(-1);
		for (long index = start; index < end; index++)
		{
			if (indexPort >= 0)
			{
				_dynamicOutputs[(node.Id, indexPort)] = Variant.From(in index);
			}
			FlowSignal flowSignal = await ExecuteNextAsync(node, 0);
			switch (flowSignal)
			{
			case FlowSignal.Return:
			case FlowSignal.Stop:
				return flowSignal;
			default:
				continue;
			case FlowSignal.Break:
				break;
			}
			break;
		}
		return await ExecuteNextAsync(node, 1);
	}

	private async Task<FlowSignal> ExecuteForEachAsync(XWBPNodeData node)
	{
		Variant variant = await EvaluateDataInputAsync(node, 0);
		if (variant.VariantType != Variant.Type.Array)
		{
			Stop($"ForEach node {node.Id} expects an Array.");
			return FlowSignal.Stop;
		}
		Godot.Collections.Array array = variant.AsGodotArray();
		if (array.Count > _context.Limits.MaxCollectionItems)
		{
			Stop($"ForEach node {node.Id} exceeds the collection preview limit.");
			return FlowSignal.Stop;
		}
		if (!ConsumeLoopIterations(array.Count, node.Id))
		{
			return FlowSignal.Stop;
		}
		List<int> outputs = DataOutputIndexes(node);
		for (int index = 0; index < array.Count; index++)
		{
			if (outputs.Count > 0)
			{
				_dynamicOutputs[(node.Id, outputs[0])] = CloneSafe(array[index]);
			}
			if (outputs.Count > 1)
			{
				_dynamicOutputs[(node.Id, outputs[1])] = Variant.From<long>((long)index);
			}
			FlowSignal flowSignal = await ExecuteNextAsync(node, 0);
			switch (flowSignal)
			{
			case FlowSignal.Return:
			case FlowSignal.Stop:
				return flowSignal;
			default:
				continue;
			case FlowSignal.Break:
				break;
			}
			break;
		}
		return await ExecuteNextAsync(node, 1);
	}

	private async Task<FlowSignal> ExecuteWhileLoopAsync(XWBPNodeData node)
	{
		while (ToBool(await EvaluateDataInputAsync(node, 0)))
		{
			if (!ConsumeLoopIterations(1L, node.Id))
			{
				return FlowSignal.Stop;
			}
			FlowSignal flowSignal = await ExecuteNextAsync(node, 0);
			if (flowSignal == FlowSignal.Return || flowSignal == FlowSignal.Stop)
			{
				return flowSignal;
			}
			if (flowSignal == FlowSignal.Break)
			{
				break;
			}
		}
		return await ExecuteNextAsync(node, 1);
	}

	private async Task<FlowSignal> ExecuteMatchAsync(XWBPNodeData node)
	{
		Variant left = await EvaluateDataInputAsync(node, 0);
		Variant metaData = node.GetMetaData("Cases");
		if (metaData.VariantType == Variant.Type.Array)
		{
			Godot.Collections.Array array = metaData.AsGodotArray();
			if (array.Count > _context.Limits.MaxCollectionItems)
			{
				Stop($"Match node {node.Id} exceeds the case preview limit.");
				return FlowSignal.Stop;
			}
			foreach (Variant item in array)
			{
				if (item.VariantType == Variant.Type.Dictionary)
				{
					Dictionary dictionary = item.AsGodotDictionary();
					if (dictionary.TryGetValue("value", out var value) && ValuesEqual(left, value))
					{
						Variant value2;
						int outputPortIndex = (dictionary.TryGetValue("portIndex", out value2) ? value2.AsInt32() : 0);
						return await ExecuteNextByPortAsync(node, outputPortIndex);
					}
				}
			}
		}
		return await ExecuteNextAsync(node, 0);
	}

	private async Task<bool> MutateArrayAsync(XWBPNodeData node, string typeId)
	{
		Godot.Collections.Array array = await GetMutableArrayInputAsync(node, 0);
		if (_stopped)
		{
			return false;
		}
		switch (typeId)
		{
		case "__XWBPGraphNode_ArrayAdd":
		{
			if (array.Count >= _context.Limits.MaxCollectionItems)
			{
				Stop($"ArrayAdd node {node.Id} exceeds the collection preview limit.");
				return false;
			}
			Godot.Collections.Array array2 = array;
			array2.Add(CloneSafe(await EvaluateDataInputAsync(node, 1)));
			break;
		}
		case "__XWBPGraphNode_ArrayClear":
			array.Clear();
			break;
		case "__XWBPGraphNode_ArrayInsert":
		{
			long num2 = ToLong(await EvaluateDataInputAsync(node, 1));
			if (num2 < 0 || num2 > array.Count)
			{
				Stop($"Array insert index {num2} is outside the bounds on node {node.Id}.");
				return false;
			}
			if (array.Count >= _context.Limits.MaxCollectionItems)
			{
				Stop($"ArrayInsert node {node.Id} exceeds the collection preview limit.");
				return false;
			}
			Godot.Collections.Array array2 = array;
			int index = (int)num2;
			array2.Insert(index, CloneSafe(await EvaluateDataInputAsync(node, 2)));
			break;
		}
		case "__XWBPGraphNode_ArrayRemove":
		{
			Variant right = await EvaluateDataInputAsync(node, 1);
			for (int i = 0; i < array.Count; i++)
			{
				if (ValuesEqual(array[i], right))
				{
					array.RemoveAt(i);
					break;
				}
			}
			break;
		}
		case "__XWBPGraphNode_ArraySet":
		{
			long num = ToLong(await EvaluateDataInputAsync(node, 1));
			if (num < 0 || num >= array.Count)
			{
				Stop($"Array set index {num} is outside the bounds on node {node.Id}.");
				return false;
			}
			Godot.Collections.Array array2 = array;
			int index = (int)num;
			array2[index] = CloneSafe(await EvaluateDataInputAsync(node, 2));
			break;
		}
		}
		return !_stopped;
	}

	private async Task<bool> MutateDictionaryAsync(XWBPNodeData node, string typeId)
	{
		Dictionary dictionary = await GetMutableDictionaryInputAsync(node, 0);
		if (_stopped)
		{
			return false;
		}
		switch (typeId)
		{
		case "__XWBPGraphNode_DictClear":
			dictionary.Clear();
			break;
		case "__XWBPGraphNode_DictRemove":
		{
			Dictionary dictionary2 = dictionary;
			dictionary2.Remove(await EvaluateDataInputAsync(node, 1));
			break;
		}
		case "__XWBPGraphNode_DictSet":
		{
			Variant key = CloneSafe(await EvaluateDataInputAsync(node, 1));
			Variant value = CloneSafe(await EvaluateDataInputAsync(node, 2));
			if (!dictionary.ContainsKey(key) && dictionary.Count >= _context.Limits.MaxCollectionItems)
			{
				Stop($"DictSet node {node.Id} exceeds the collection preview limit.");
				return false;
			}
			dictionary[key] = value;
			break;
		}
		}
		return !_stopped;
	}

	private async Task<bool> ExecuteSetPropertyAsync(XWBPNodeData node)
	{
		int metaInt = GetMetaInt(node, "MethodType");
		Variant value;
		switch (metaInt)
		{
		case 0:
		{
			int variableId = GetMetaInt(node, "VariableId");
			if (!_blueprintVariables.ContainsKey(variableId))
			{
				Stop($"Blueprint variable {variableId} is not available in the isolated preview snapshot.");
				return false;
			}
			value = CloneSafe(await EvaluateDataInputAsync(node, 0));
			if (_stopped)
			{
				return false;
			}
			_blueprintVariables[variableId] = value;
			break;
		}
		case 1:
		{
			Dictionary propertyData = GetMetadataDictionary(node, "PropertyData");
			if (IsStaticMember(propertyData))
			{
				Stop($"Static property access on node {node.Id} is not exposed by the preview adapter.");
				return false;
			}
			if (!TryGetSafeObject(await EvaluateDataInputAsync(node, 0), out var target))
			{
				Stop($"Property target on node {node.Id} is not an isolated preview object.");
				return false;
			}
			string propertyName = GetMemberName(propertyData);
			if (!target.MatchesClass(GetDictionaryString(propertyData, "base_class_name")))
			{
				Stop($"Property class on node {node.Id} does not match its isolated preview object.");
				return false;
			}
			value = CloneSafe(await EvaluateDataInputAsync(node, 1));
			if (_stopped)
			{
				return false;
			}
			if (!target.TrySetProperty(propertyName, value))
			{
				Stop("Property '" + propertyName + "' is not writable in the preview whitelist.");
				return false;
			}
			break;
		}
		default:
			Stop($"Property mode {metaInt} on node {node.Id} is not supported by safe preview.");
			return false;
		}
		int num = DataOutputIndexes(node).FirstOrDefault(-1);
		if (num >= 0)
		{
			_dynamicOutputs[(node.Id, num)] = value;
		}
		return true;
	}

	private Task<bool> ExecuteCallMethodAsync(XWBPNodeData node)
	{
		if (GetMetaInt(node, "MethodType") != 0)
		{
			return ExecuteWhitelistedMethodAsync(node);
		}
		return ExecuteBlueprintFunctionAsync(node);
	}

	private async Task<bool> ExecuteBlueprintFunctionAsync(XWBPNodeData callNode)
	{
		int functionId = GetMetaInt(callNode, "FunctionId");
		XWBPScriptData xWBPScriptData = _context.ScriptData ?? _graph?.Owner;
		if (xWBPScriptData?.Functions == null)
		{
			Stop($"CallMethod node {callNode.Id} cannot resolve Blueprint function {functionId}: the current XWBPScript data is not attached to the safe preview.");
			return false;
		}
		if (functionId <= 0 || !xWBPScriptData.Functions.TryGetValue(functionId, out var function) || !GodotObject.IsInstanceValid(function))
		{
			Stop($"CallMethod node {callNode.Id} references missing Blueprint function {functionId} in the current XWBPScript.");
			return false;
		}
		string functionName = (string.IsNullOrWhiteSpace(function.Name) ? $"Function {functionId}" : function.Name.Trim());
		if (_activeFunctionIds.Contains(functionId))
		{
			Stop($"CallMethod node {callNode.Id} was denied because Blueprint function '{functionName}' ({functionId}) creates a recursive call cycle.");
			return false;
		}
		int num = Math.Max(1, _context.Limits.MaxCallDepth);
		if (_activeFunctionIds.Count >= num)
		{
			Stop($"CallMethod node {callNode.Id} cannot enter Blueprint function '{functionName}' ({functionId}): the {num} call-depth preview limit was reached.");
			return false;
		}
		if (function.Inputs.Count > _context.Limits.MaxCallArguments)
		{
			Stop($"CallMethod node {callNode.Id} cannot enter Blueprint function '{functionName}' ({functionId}): it exceeds the {_context.Limits.MaxCallArguments} argument preview limit.");
			return false;
		}
		List<int> list = DataInputIndexes(callNode);
		if (list.Count != function.Inputs.Count)
		{
			Stop($"CallMethod node {callNode.Id} has {list.Count} data inputs, but Blueprint function '{functionName}' ({functionId}) requires {function.Inputs.Count}. Refresh the call node.");
			return false;
		}
		List<int> callOutputs = DataOutputIndexes(callNode);
		if (callOutputs.Count != function.Outputs.Count)
		{
			Stop($"CallMethod node {callNode.Id} has {callOutputs.Count} data outputs, but Blueprint function '{functionName}' ({functionId}) declares {function.Outputs.Count}. Refresh the call node.");
			return false;
		}
		List<Variant> arguments = new List<Variant>(function.Inputs.Count);
		for (int index = 0; index < function.Inputs.Count; index++)
		{
			List<Variant> list2 = arguments;
			list2.Add(CloneSafe(await EvaluateDataInputAsync(callNode, index)));
			if (_stopped)
			{
				return false;
			}
		}
		if (!function.Nodes.TryGetValue(-100000, out var value) || value == null)
		{
			Stop($"CallMethod node {callNode.Id} cannot enter Blueprint function '{functionName}' ({functionId}): its MethodEntry node is missing.");
			return false;
		}
		RuntimeFrame functionFrame = new RuntimeFrame
		{
			Graph = function,
			FunctionId = functionId,
			FunctionName = functionName
		};
		List<int> list3 = DataOutputIndexes(value);
		if (list3.Count != arguments.Count)
		{
			Stop($"CallMethod node {callNode.Id} cannot enter Blueprint function '{functionName}' ({functionId}): MethodEntry exposes {list3.Count} arguments instead of {arguments.Count}.");
			return false;
		}
		RuntimeFrame callerFrame = _currentFrame;
		_activeFunctionIds.Add(functionId);
		SwitchToFrame(functionFrame);
		try
		{
			for (int i = 0; i < list3.Count; i++)
			{
				_dynamicOutputs[(value.Id, list3[i])] = CloneSafe(arguments[i]);
				if (_stopped)
				{
					return false;
				}
			}
			FlowSignal flowSignal = await ExecuteFlowAsync(value.Id);
			if (flowSignal == FlowSignal.Stop || _stopped)
			{
				return false;
			}
			if ((uint)(flowSignal - 1) <= 1u)
			{
				Stop($"Blueprint function '{functionName}' ({functionId}) escaped with a loop-only flow signal.");
				return false;
			}
		}
		finally
		{
			SwitchToFrame(callerFrame);
			_activeFunctionIds.Remove(functionId);
		}
		if (function.Outputs.Count == 1)
		{
			_dynamicOutputs[(callNode.Id, callOutputs[0])] = CloneSafe(functionFrame.ReturnValue);
		}
		else if (function.Outputs.Count > 1)
		{
			if (functionFrame.ReturnValue.VariantType != Variant.Type.Array)
			{
				Stop($"Blueprint function '{functionName}' ({functionId}) must return {function.Outputs.Count} values.");
				return false;
			}
			Godot.Collections.Array array = functionFrame.ReturnValue.AsGodotArray();
			if (array.Count != function.Outputs.Count)
			{
				Stop($"Blueprint function '{functionName}' ({functionId}) returned {array.Count} values instead of {function.Outputs.Count}.");
				return false;
			}
			for (int j = 0; j < array.Count; j++)
			{
				_dynamicOutputs[(callNode.Id, callOutputs[j])] = CloneSafe(array[j]);
			}
		}
		return !_stopped;
	}

	private void SwitchToFrame(RuntimeFrame frame)
	{
		_currentFrame = frame ?? throw new ArgumentNullException("frame");
		_graph = frame.Graph;
		_currentFunctionId = frame.FunctionId;
		_currentFunctionName = frame.FunctionName;
		_dynamicOutputs = frame.DynamicOutputs;
		_valueStack = frame.ValueStack;
		_variables = frame.Variables;
	}

	private async Task<bool> ExecuteWhitelistedMethodAsync(XWBPNodeData node)
	{
		int metaInt = GetMetaInt(node, "MethodType");
		if (metaInt != 1)
		{
			Stop($"CallMethod mode {metaInt} on node {node.Id} is not exposed by a preview adapter.");
			return false;
		}
		Dictionary methodData = GetMetadataDictionary(node, "MethodData");
		if (IsStaticMember(methodData))
		{
			Stop($"Static method calls on node {node.Id} are not exposed by the preview adapter.");
			return false;
		}
		if (!TryGetSafeObject(await EvaluateDataInputAsync(node, 0), out var target))
		{
			Stop($"Method target on node {node.Id} is not an isolated preview object.");
			return false;
		}
		if (!target.MatchesClass(GetDictionaryString(methodData, "base_class_name")))
		{
			Stop($"Method class on node {node.Id} does not match its isolated preview object.");
			return false;
		}
		string methodName = GetMemberName(methodData);
		int argumentCount = Math.Max(0, DataInputIndexes(node).Count - 1);
		if (argumentCount > _context.Limits.MaxCallArguments)
		{
			Stop($"CallMethod node {node.Id} exceeds the {_context.Limits.MaxCallArguments} argument preview limit.");
			return false;
		}
		List<Variant> arguments = new List<Variant>(argumentCount);
		for (int index = 0; index < argumentCount; index++)
		{
			List<Variant> list = arguments;
			list.Add(CloneSafe(await EvaluateDataInputAsync(node, index + 1)));
			if (_stopped)
			{
				return false;
			}
		}
		if (!CheckTimeBudget())
		{
			return false;
		}
		if (!target.TryInvoke(methodName, arguments, out var result))
		{
			Stop("Method '" + methodName + "' is not in the preview adapter whitelist or its arguments are invalid.");
			return false;
		}
		if (!CheckTimeBudget())
		{
			return false;
		}
		int num = DataOutputIndexes(node).FirstOrDefault(-1);
		if (num >= 0)
		{
			_dynamicOutputs[(node.Id, num)] = CloneSafe(result);
		}
		return !_stopped;
	}

	private async Task<Godot.Collections.Array> GetMutableArrayInputAsync(XWBPNodeData node, int dataOrdinal)
	{
		Variant variant = await GetMutableCollectionInputAsync(node, dataOrdinal);
		if (variant.VariantType == Variant.Type.Array)
		{
			return variant.AsGodotArray();
		}
		Stop($"Array mutation node {node.Id} received a non-array value.");
		return new Godot.Collections.Array();
	}

	private async Task<Dictionary> GetMutableDictionaryInputAsync(XWBPNodeData node, int dataOrdinal)
	{
		Variant variant = await GetMutableCollectionInputAsync(node, dataOrdinal);
		if (variant.VariantType == Variant.Type.Dictionary)
		{
			return variant.AsGodotDictionary();
		}
		Stop($"Dictionary mutation node {node.Id} received a non-dictionary value.");
		return new Dictionary();
	}

	private async Task<Variant> GetMutableCollectionInputAsync(XWBPNodeData node, int dataOrdinal)
	{
		List<int> list = DataInputIndexes(node);
		if (dataOrdinal < 0 || dataOrdinal >= list.Count)
		{
			return default(Variant);
		}
		int inputPortIndex = list[dataOrdinal];
		XWBPNodeConnectionData xWBPNodeConnectionData = _graph.Connections.FirstOrDefault((XWBPNodeConnectionData candidate) => candidate != null && candidate.ToNodeId == node.Id && candidate.ToPortIndex == inputPortIndex);
		(int, int) stateKey = ((xWBPNodeConnectionData == null) ? (node.Id, -(inputPortIndex + 1)) : (xWBPNodeConnectionData.FromNodeId, xWBPNodeConnectionData.FromPortIndex));
		if (_dynamicOutputs.TryGetValue(stateKey, out var value))
		{
			return value;
		}
		Variant variant = ((xWBPNodeConnectionData != null) ? (await EvaluateOutputAsync(xWBPNodeConnectionData.FromNodeId, xWBPNodeConnectionData.FromPortIndex)) : (await EvaluateDataInputAsync(node, dataOrdinal)));
		Variant value2 = variant;
		Variant variant2 = CloneSafe(value2);
		_dynamicOutputs[stateKey] = variant2;
		return variant2;
	}

	private async Task<FlowSignal> ExecuteNextAsync(XWBPNodeData node, int flowOrdinal)
	{
		List<int> list = FlowOutputIndexes(node);
		if (flowOrdinal < 0 || flowOrdinal >= list.Count)
		{
			return FlowSignal.Continue;
		}
		return await ExecuteNextByPortAsync(node, list[flowOrdinal]);
	}

	private async Task<FlowSignal> ExecuteNextByPortAsync(XWBPNodeData node, int outputPortIndex)
	{
		XWBPNodeConnectionData xWBPNodeConnectionData = _graph.Connections.FirstOrDefault((XWBPNodeConnectionData candidate) => candidate != null && candidate.FromNodeId == node.Id && candidate.FromPortIndex == outputPortIndex);
		return (xWBPNodeConnectionData != null) ? (await ExecuteFlowAsync(xWBPNodeConnectionData.ToNodeId)) : FlowSignal.Continue;
	}

	private async Task<Variant> EvaluateDataInputAsync(XWBPNodeData node, int dataOrdinal)
	{
		List<int> list = DataInputIndexes(node);
		if (dataOrdinal < 0 || dataOrdinal >= list.Count)
		{
			return default(Variant);
		}
		int inputPortIndex = list[dataOrdinal];
		XWBPNodeConnectionData xWBPNodeConnectionData = _graph.Connections.FirstOrDefault((XWBPNodeConnectionData candidate) => candidate != null && candidate.ToNodeId == node.Id && candidate.ToPortIndex == inputPortIndex);
		if (xWBPNodeConnectionData != null)
		{
			return await EvaluateOutputAsync(xWBPNodeConnectionData.FromNodeId, xWBPNodeConnectionData.FromPortIndex);
		}
		XWBPNodePortData inputPort = node.GetInputPort(inputPortIndex);
		Variant value = inputPort?.Value ?? default(Variant);
		if (value.VariantType == Variant.Type.Nil && inputPort != null)
		{
			value = inputPort.DefaultValue;
		}
		return CloneSafe(value);
	}

	private async Task<Variant> EvaluateOutputAsync(int nodeId, int outputPortIndex)
	{
		if (_dynamicOutputs.TryGetValue((nodeId, outputPortIndex), out var value))
		{
			return CloneSafe(value);
		}
		if (!_graph.Nodes.TryGetValue(nodeId, out var node) || node == null)
		{
			Stop($"Value connection points to missing node {nodeId}.");
			return default(Variant);
		}
		(int nodeId, int outputPortIndex) key = (nodeId: nodeId, outputPortIndex: outputPortIndex);
		if (!_valueStack.Add(key))
		{
			Stop($"Value dependency cycle detected at node {nodeId}.");
			return default(Variant);
		}
		try
		{
			if (!(await EnterNodeAsync(node)))
			{
				return default(Variant);
			}
			string text = node.TypeId.ToString();
			Variant variant;
			switch (text)
			{
			case "__XWBPGraphNode_Literal":
				variant = ReadOutputPort(node, outputPortIndex);
				break;
			case "__XWBPGraphNode_LoadResource":
				variant = EvaluateLoadResource(node, outputPortIndex);
				break;
			case "__XWBPGraphNode_GetSelf":
				variant = Variant.From(in _selfObject);
				break;
			case "__XWBPGraphNode_IsValid":
			{
				variant = Variant.From<bool>(TryGetSafeObject(await EvaluateDataInputAsync(node, 0), out var _));
				break;
			}
			case "__XWBPGraphNode_GetProperty":
				variant = await EvaluateWhitelistedPropertyAsync(node);
				break;
			case "__XWBPGraphNode_AddInt":
			{
				long num2 = ToLong(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<long>(num2 + ToLong(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_SubtractInt":
			{
				long num2 = ToLong(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<long>(num2 - ToLong(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_MultiplyInt":
			{
				long num2 = ToLong(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<long>(num2 * ToLong(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_DivideInt":
				variant = DivideInt(await EvaluateDataInputAsync(node, 0), await EvaluateDataInputAsync(node, 1), node.Id);
				break;
			case "__XWBPGraphNode_Modulo":
				variant = Modulo(await EvaluateDataInputAsync(node, 0), await EvaluateDataInputAsync(node, 1), node.Id);
				break;
			case "__XWBPGraphNode_AddFloat":
			{
				double num = ToDouble(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<double>(num + ToDouble(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_SubtractFloat":
			{
				double num = ToDouble(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<double>(num - ToDouble(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_MultiplyFloat":
			{
				double num = ToDouble(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<double>(num * ToDouble(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_DivideFloat":
				variant = DivideFloat(await EvaluateDataInputAsync(node, 0), await EvaluateDataInputAsync(node, 1), node.Id);
				break;
			case "__XWBPGraphNode_Abs":
				variant = Variant.From<double>(Math.Abs(ToDouble(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Min":
			{
				double num = ToDouble(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<double>(Math.Min(num, ToDouble(await EvaluateDataInputAsync(node, 1))));
				break;
			}
			case "__XWBPGraphNode_Max":
			{
				double num = ToDouble(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<double>(Math.Max(num, ToDouble(await EvaluateDataInputAsync(node, 1))));
				break;
			}
			case "__XWBPGraphNode_Clamp":
			{
				double num = ToDouble(await EvaluateDataInputAsync(node, 0));
				double num3 = ToDouble(await EvaluateDataInputAsync(node, 1));
				variant = Variant.From<double>(Math.Clamp(num, num3, ToDouble(await EvaluateDataInputAsync(node, 2))));
				break;
			}
			case "__XWBPGraphNode_Pow":
			{
				double num3 = ToDouble(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<double>(Math.Pow(num3, ToDouble(await EvaluateDataInputAsync(node, 1))));
				break;
			}
			case "__XWBPGraphNode_Sqrt":
				variant = Variant.From<double>(Math.Sqrt(Math.Max(0.0, ToDouble(await EvaluateDataInputAsync(node, 0)))));
				break;
			case "__XWBPGraphNode_Floor":
				variant = Variant.From<double>(Math.Floor(ToDouble(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Ceil":
				variant = Variant.From<double>(Math.Ceiling(ToDouble(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Round":
				variant = Variant.From<double>(Math.Round(ToDouble(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Sin":
				variant = Variant.From<double>(Math.Sin(ToDouble(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Cos":
				variant = Variant.From<double>(Math.Cos(ToDouble(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Tan":
				variant = Variant.From<double>(Math.Tan(ToDouble(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_DegToRad":
				variant = Variant.From<double>(ToDouble(await EvaluateDataInputAsync(node, 0)) * Math.PI / 180.0);
				break;
			case "__XWBPGraphNode_RadToDeg":
				variant = Variant.From<double>(ToDouble(await EvaluateDataInputAsync(node, 0)) * 180.0 / Math.PI);
				break;
			case "__XWBPGraphNode_Lerp":
			{
				double num3 = ToDouble(await EvaluateDataInputAsync(node, 0));
				double num = ToDouble(await EvaluateDataInputAsync(node, 1));
				variant = Variant.From<double>(Mathf.Lerp(num3, num, ToDouble(await EvaluateDataInputAsync(node, 2))));
				break;
			}
			case "__XWBPGraphNode_Log":
				variant = SafeLog(await EvaluateDataInputAsync(node, 0), node.Id);
				break;
			case "__XWBPGraphNode_RandomFloat":
				variant = await RandomFloatAsync(node);
				break;
			case "__XWBPGraphNode_RandomInt":
				variant = await RandomIntAsync(node);
				break;
			case "__XWBPGraphNode_Greater":
				variant = Variant.From<bool>(Compare(await EvaluateDataInputAsync(node, 0), await EvaluateDataInputAsync(node, 1)) > 0);
				break;
			case "__XWBPGraphNode_Less":
				variant = Variant.From<bool>(Compare(await EvaluateDataInputAsync(node, 0), await EvaluateDataInputAsync(node, 1)) < 0);
				break;
			case "__XWBPGraphNode_Equal":
				variant = Variant.From<bool>(ValuesEqual(await EvaluateDataInputAsync(node, 0), await EvaluateDataInputAsync(node, 1)));
				break;
			case "__XWBPGraphNode_CompareFloat":
			case "__XWBPGraphNode_CompareString":
			case "__XWBPGraphNode_CompareInt":
				variant = Variant.From<bool>(ValuesEqual(await EvaluateDataInputAsync(node, 0), await EvaluateDataInputAsync(node, 1)));
				break;
			case "__XWBPGraphNode_And":
			{
				bool flag = ToBool(await EvaluateDataInputAsync(node, 0));
				if (flag)
				{
					flag = ToBool(await EvaluateDataInputAsync(node, 1));
				}
				variant = Variant.From(in flag);
				break;
			}
			case "__XWBPGraphNode_Or":
			{
				bool flag = ToBool(await EvaluateDataInputAsync(node, 0));
				if (!flag)
				{
					flag = ToBool(await EvaluateDataInputAsync(node, 1));
				}
				variant = Variant.From(in flag);
				break;
			}
			case "__XWBPGraphNode_Xor":
			{
				bool flag2 = ToBool(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<bool>(flag2 ^ ToBool(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_Nand":
			{
				bool flag = ToBool(await EvaluateDataInputAsync(node, 0));
				if (flag)
				{
					flag = ToBool(await EvaluateDataInputAsync(node, 1));
				}
				variant = Variant.From<bool>(!flag);
				break;
			}
			case "__XWBPGraphNode_Nor":
			{
				bool flag = ToBool(await EvaluateDataInputAsync(node, 0));
				if (!flag)
				{
					flag = ToBool(await EvaluateDataInputAsync(node, 1));
				}
				variant = Variant.From<bool>(!flag);
				break;
			}
			case "__XWBPGraphNode_Not":
				variant = Variant.From<bool>(!ToBool(await EvaluateDataInputAsync(node, 0)));
				break;
			case "__XWBPGraphNode_SelectBool":
			{
				XWBPNodeData node2 = node;
				variant = await EvaluateDataInputAsync(node2, ToBool(await EvaluateDataInputAsync(node, 0)) ? 1 : 2);
				break;
			}
			case "__XWBPGraphNode_ToString":
				variant = Variant.From<string>(FormatValue(await EvaluateDataInputAsync(node, 0)));
				break;
			case "__XWBPGraphNode_StringToInt":
			case "__XWBPGraphNode_ToInt":
			case "__XWBPGraphNode_FloatToInt":
				variant = Variant.From<long>(ToLong(await EvaluateDataInputAsync(node, 0)));
				break;
			case "__XWBPGraphNode_ToFloat":
			case "__XWBPGraphNode_StringToFloat":
				variant = Variant.From<double>(ToDouble(await EvaluateDataInputAsync(node, 0)));
				break;
			case "__XWBPGraphNode_ToBool":
				variant = Variant.From<bool>(ToBool(await EvaluateDataInputAsync(node, 0)));
				break;
			case "__XWBPGraphNode_StringConcat":
			{
				string text2 = FormatValue(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<string>(LimitString(text2 + FormatValue(await EvaluateDataInputAsync(node, 1))));
				break;
			}
			case "__XWBPGraphNode_StringLength":
				variant = Variant.From<long>((long)FormatValue(await EvaluateDataInputAsync(node, 0)).Length);
				break;
			case "__XWBPGraphNode_StringContains":
			{
				string text2 = FormatValue(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<bool>(text2.Contains(FormatValue(await EvaluateDataInputAsync(node, 1)), StringComparison.Ordinal));
				break;
			}
			case "__XWBPGraphNode_StringToLower":
				variant = Variant.From<string>(FormatValue(await EvaluateDataInputAsync(node, 0)).ToLowerInvariant());
				break;
			case "__XWBPGraphNode_StringToUpper":
				variant = Variant.From<string>(FormatValue(await EvaluateDataInputAsync(node, 0)).ToUpperInvariant());
				break;
			case "__XWBPGraphNode_StringReplace":
				variant = await StringReplaceAsync(node);
				break;
			case "__XWBPGraphNode_StringSplit":
				variant = await StringSplitAsync(node);
				break;
			case "__XWBPGraphNode_StringSubstring":
				variant = await StringSubstringAsync(node);
				break;
			case "__XWBPGraphNode_MakeArray":
				variant = await MakeArrayAsync(node);
				break;
			case "__XWBPGraphNode_ArrayLength":
				variant = Variant.From<long>((long)ArrayValue(await EvaluateDataInputAsync(node, 0), node.Id).Count);
				break;
			case "__XWBPGraphNode_ArrayContains":
			{
				Godot.Collections.Array array = ArrayValue(await EvaluateDataInputAsync(node, 0), node.Id);
				variant = Variant.From<bool>(ArrayContains(array, await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_ArrayGet":
			{
				Godot.Collections.Array array = ArrayValue(await EvaluateDataInputAsync(node, 0), node.Id);
				variant = ArrayGet(array, ToLong(await EvaluateDataInputAsync(node, 1)), node.Id);
				break;
			}
			case "__XWBPGraphNode_ArrayFind":
			{
				Godot.Collections.Array array = ArrayValue(await EvaluateDataInputAsync(node, 0), node.Id);
				variant = Variant.From<long>((long)ArrayFind(array, await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_MakeDict":
				variant = await MakeDictionaryAsync(node);
				break;
			case "__XWBPGraphNode_DictContains":
				variant = await DictionaryContainsAsync(node);
				break;
			case "__XWBPGraphNode_DictGet":
			case "__XWBPGraphNode_DictionaryGet":
				variant = await DictionaryGetAsync(node);
				break;
			case "__XWBPGraphNode_DictKeys":
				variant = await DictionaryListAsync(node, keys: true);
				break;
			case "__XWBPGraphNode_DictValues":
				variant = await DictionaryListAsync(node, keys: false);
				break;
			case "__XWBPGraphNode_DictSize":
				variant = Variant.From<long>((long)DictionaryValue(await EvaluateDataInputAsync(node, 0), node.Id).Count);
				break;
			case "__XWBPGraphNode_MakeVector2":
			{
				float x = (float)ToDouble(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<Vector2>(new Vector2(x, (float)ToDouble(await EvaluateDataInputAsync(node, 1))));
				break;
			}
			case "__XWBPGraphNode_MakeVector3":
			{
				float x = (float)ToDouble(await EvaluateDataInputAsync(node, 0));
				float y = (float)ToDouble(await EvaluateDataInputAsync(node, 1));
				variant = Variant.From<Vector3>(new Vector3(x, y, (float)ToDouble(await EvaluateDataInputAsync(node, 2))));
				break;
			}
			case "__XWBPGraphNode_BreakVector2":
				variant = BreakVector2(await EvaluateDataInputAsync(node, 0), outputPortIndex);
				break;
			case "__XWBPGraphNode_BreakVector3":
				variant = BreakVector3(await EvaluateDataInputAsync(node, 0), outputPortIndex);
				break;
			case "__XWBPGraphNode_Vector2Add":
			{
				Vector2 vector2 = ToVector2(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<Vector2>(vector2 + ToVector2(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_Vector2Subtract":
			{
				Vector2 vector2 = ToVector2(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<Vector2>(vector2 - ToVector2(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_Vector2Multiply":
			{
				Vector2 vector2 = ToVector2(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<Vector2>(vector2 * (float)ToDouble(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_Vector2Dot":
				variant = Variant.From<double>((double)ToVector2(await EvaluateDataInputAsync(node, 0)).Dot(ToVector2(await EvaluateDataInputAsync(node, 1))));
				break;
			case "__XWBPGraphNode_Vector2Normalize":
				variant = Variant.From<Vector2>(ToVector2(await EvaluateDataInputAsync(node, 0)).Normalized());
				break;
			case "__XWBPGraphNode_Vector2Length":
				variant = Variant.From<double>((double)ToVector2(await EvaluateDataInputAsync(node, 0)).Length());
				break;
			case "__XWBPGraphNode_Vector2ToVector3":
				variant = Variant.From<Vector3>(ToVector3(ToVector2(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Vector3ToVector2":
				variant = Variant.From<Vector2>(ToVector2(ToVector3(await EvaluateDataInputAsync(node, 0))));
				break;
			case "__XWBPGraphNode_Vector3Add":
			{
				Vector3 vector = ToVector3(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<Vector3>(vector + ToVector3(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_Vector3Subtract":
			{
				Vector3 vector = ToVector3(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<Vector3>(vector - ToVector3(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_Vector3Multiply":
			{
				Vector3 vector = ToVector3(await EvaluateDataInputAsync(node, 0));
				variant = Variant.From<Vector3>(vector * (float)ToDouble(await EvaluateDataInputAsync(node, 1)));
				break;
			}
			case "__XWBPGraphNode_Vector3Dot":
				variant = Variant.From<double>((double)ToVector3(await EvaluateDataInputAsync(node, 0)).Dot(ToVector3(await EvaluateDataInputAsync(node, 1))));
				break;
			case "__XWBPGraphNode_Vector3Normalize":
				variant = Variant.From<Vector3>(ToVector3(await EvaluateDataInputAsync(node, 0)).Normalized());
				break;
			case "__XWBPGraphNode_Vector3Length":
				variant = Variant.From<double>((double)ToVector3(await EvaluateDataInputAsync(node, 0)).Length());
				break;
			case "__XWBPGraphNode_LocalVariable":
				variant = await EvaluateLocalVariableAsync(node);
				break;
			case "__XWBPGraphNode_IsNull":
				variant = Variant.From<bool>((await EvaluateDataInputAsync(node, 0)).VariantType == Variant.Type.Nil);
				break;
			case "__XWBPGraphNode_GetType":
				variant = Variant.From<string>((await EvaluateDataInputAsync(node, 0)).VariantType.ToString());
				break;
			default:
				variant = UnsupportedValue(text, node.Id);
				break;
			}
			Variant value2 = variant;
			return CloneSafe(value2);
		}
		finally
		{
			_valueStack.Remove(key);
		}
	}

	private async Task<Variant> EvaluateLocalVariableAsync(XWBPNodeData node)
	{
		string name = node.GetMetaData("VarName").AsString();
		if (string.IsNullOrWhiteSpace(name))
		{
			name = $"local_{node.Id}";
		}
		if (!_variables.TryGetValue(name, out var value))
		{
			value = CloneSafe(await EvaluateDataInputAsync(node, 0));
			_variables[name] = value;
		}
		return value;
	}

	private async Task<Variant> MakeArrayAsync(XWBPNodeData node)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		int count = Math.Min(DataInputIndexes(node).Count, _context.Limits.MaxCollectionItems);
		for (int index = 0; index < count; index++)
		{
			Godot.Collections.Array array2 = array;
			array2.Add(CloneSafe(await EvaluateDataInputAsync(node, index)));
		}
		return Variant.From(in array);
	}

	private Variant SafeLog(Variant source, int nodeId)
	{
		double num = ToDouble(source);
		if (num <= 0.0)
		{
			Stop($"Log node {nodeId} requires a value greater than zero.");
			return default(Variant);
		}
		return Variant.From<double>(Math.Log(num));
	}

	private async Task<Variant> RandomFloatAsync(XWBPNodeData node)
	{
		double minimum = ToDouble(await EvaluateDataInputAsync(node, 0));
		double num = ToDouble(await EvaluateDataInputAsync(node, 1));
		if (num < minimum)
		{
			double num2 = num;
			num = minimum;
			minimum = num2;
		}
		Random random = new Random((node.Id * 397) ^ 0x51A7);
		return Variant.From<double>(minimum + random.NextDouble() * (num - minimum));
	}

	private async Task<Variant> RandomIntAsync(XWBPNodeData node)
	{
		long minimum = ToLong(await EvaluateDataInputAsync(node, 0));
		long num = ToLong(await EvaluateDataInputAsync(node, 1));
		if (num < minimum)
		{
			long num2 = num;
			num = minimum;
			minimum = num2;
		}
		Random random = new Random((node.Id * 397) ^ 0x19D3);
		double num3 = (double)num - (double)minimum + 1.0;
		return Variant.From<long>(Math.Min(minimum + (long)Math.Floor(random.NextDouble() * num3), num));
	}

	private async Task<Variant> StringReplaceAsync(XWBPNodeData node)
	{
		string source = FormatValue(await EvaluateDataInputAsync(node, 0));
		string find = FormatValue(await EvaluateDataInputAsync(node, 1));
		string newValue = FormatValue(await EvaluateDataInputAsync(node, 2));
		if (string.IsNullOrEmpty(find))
		{
			return Variant.From(in source);
		}
		return Variant.From<string>(LimitString(source.Replace(find, newValue, StringComparison.Ordinal)));
	}

	private async Task<Variant> StringSplitAsync(XWBPNodeData node)
	{
		string source = FormatValue(await EvaluateDataInputAsync(node, 0));
		string text = FormatValue(await EvaluateDataInputAsync(node, 1));
		string[] array = ((!string.IsNullOrEmpty(text)) ? source.Split(new string[1] { text }, StringSplitOptions.None) : new string[1] { source });
		if (array.Length > _context.Limits.MaxCollectionItems)
		{
			Stop($"StringSplit node {node.Id} exceeds the collection preview limit.");
			return default(Variant);
		}
		Godot.Collections.Array from = new Godot.Collections.Array();
		string[] array2 = array;
		foreach (string value in array2)
		{
			from.Add(LimitString(value));
		}
		return Variant.From(in from);
	}

	private async Task<Variant> StringSubstringAsync(XWBPNodeData node)
	{
		string source = FormatValue(await EvaluateDataInputAsync(node, 0));
		long start = ToLong(await EvaluateDataInputAsync(node, 1));
		long num = ToLong(await EvaluateDataInputAsync(node, 2));
		if (start < 0 || start > source.Length)
		{
			Stop($"Substring start {start} is outside the bounds on node {node.Id}.");
			return default(Variant);
		}
		if (num < 0)
		{
			string text = source;
			int num2 = (int)start;
			return Variant.From<string>(text.Substring(num2, text.Length - num2));
		}
		if (num > source.Length - start)
		{
			Stop($"Substring length {num} is outside the bounds on node {node.Id}.");
			return default(Variant);
		}
		return Variant.From<string>(source.Substring((int)start, (int)num));
	}

	private async Task<Variant> MakeDictionaryAsync(XWBPNodeData node)
	{
		Dictionary dictionary = new Dictionary();
		int pairCount = Math.Min(DataInputIndexes(node).Count / 2, _context.Limits.MaxCollectionItems);
		for (int pair = 0; pair < pairCount; pair++)
		{
			Variant key = CloneSafe(await EvaluateDataInputAsync(node, pair * 2));
			Variant value = CloneSafe(await EvaluateDataInputAsync(node, pair * 2 + 1));
			dictionary[key] = value;
		}
		return Variant.From(in dictionary);
	}

	private async Task<Variant> DictionaryContainsAsync(XWBPNodeData node)
	{
		Dictionary dictionary = DictionaryValue(await EvaluateDataInputAsync(node, 0), node.Id);
		return Variant.From<bool>(dictionary.ContainsKey(await EvaluateDataInputAsync(node, 1)));
	}

	private async Task<Variant> DictionaryGetAsync(XWBPNodeData node)
	{
		Dictionary dictionary = DictionaryValue(await EvaluateDataInputAsync(node, 0), node.Id);
		if (!dictionary.TryGetValue(await EvaluateDataInputAsync(node, 1), out var value))
		{
			Stop($"Dictionary key was not found on node {node.Id}.");
			return default(Variant);
		}
		return CloneSafe(value);
	}

	private async Task<Variant> DictionaryListAsync(XWBPNodeData node, bool keys)
	{
		Dictionary dictionary = DictionaryValue(await EvaluateDataInputAsync(node, 0), node.Id);
		Godot.Collections.Array from = new Godot.Collections.Array();
		foreach (Variant item in keys ? dictionary.Keys : dictionary.Values)
		{
			from.Add(CloneSafe(item));
		}
		return Variant.From(in from);
	}

	private static int ArrayFind(Godot.Collections.Array array, Variant value)
	{
		for (int i = 0; i < array.Count; i++)
		{
			if (ValuesEqual(array[i], value))
			{
				return i;
			}
		}
		return -1;
	}

	private static Variant BreakVector2(Variant value, int outputPortIndex)
	{
		Vector2 vector = ToVector2(value);
		return Variant.From<double>((double)((outputPortIndex == 0) ? vector.X : vector.Y));
	}

	private static Variant BreakVector3(Variant value, int outputPortIndex)
	{
		Vector3 vector = ToVector3(value);
		return Variant.From<double>((double)(outputPortIndex switch
		{
			0 => vector.X, 
			1 => vector.Y, 
			_ => vector.Z, 
		}));
	}

	private Variant ReadOutputPort(XWBPNodeData node, int outputPortIndex)
	{
		XWBPNodePortData outputPort = node.GetOutputPort(outputPortIndex);
		Variant value = outputPort?.Value ?? default(Variant);
		if (value.VariantType == Variant.Type.Nil && outputPort != null)
		{
			value = outputPort.DefaultValue;
		}
		return CloneSafe(value);
	}

	private XWBlueprintSafeObject CreateSelfObject()
	{
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.Ordinal);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (_context.SelfProperties != null)
		{
			foreach (var (text2, value) in _context.SelfProperties)
			{
				if (XWBlueprintSafeObject.IsSafeIdentifier(text2))
				{
					Variant value2 = CloneSafe(value);
					if (_stopped)
					{
						break;
					}
					dictionary[text2] = value2;
					hashSet.Add(text2);
				}
			}
		}
		string from = (string.IsNullOrWhiteSpace(_context.SelfClassName) ? "BlueprintSelf" : _context.SelfClassName.Trim());
		dictionary["preview_class"] = Variant.From(in from);
		return new XWBlueprintSafeObject(from, string.Empty, dictionary, hashSet, new string[3] { "has_property", "get_property_count", "get_type" });
	}

	private void RegisterSafeObject(XWBlueprintSafeObject safeObject)
	{
		if (GodotObject.IsInstanceValid(safeObject))
		{
			_safeObjectIds.Add(safeObject.GetInstanceId());
		}
	}

	private bool TryGetSafeObject(Variant value, out XWBlueprintSafeObject safeObject)
	{
		safeObject = null;
		if (value.VariantType != Variant.Type.Object)
		{
			return false;
		}
		safeObject = value.AsGodotObject() as XWBlueprintSafeObject;
		if (GodotObject.IsInstanceValid(safeObject))
		{
			return _safeObjectIds.Contains(safeObject.GetInstanceId());
		}
		return false;
	}

	private Variant EvaluateLoadResource(XWBPNodeData node, int outputPortIndex)
	{
		if (!XWBlueprintSafeResourceLoader.TryLoadSnapshot(node.GetMetaData("ResourcePath").AsString(), _context.ModProjectRoot, _context.Limits, out var safeObject, out var diagnostic))
		{
			Stop($"LoadResource node {node.Id} was denied: {diagnostic}");
			return default(Variant);
		}
		RegisterSafeObject(safeObject);
		Variant variant = Variant.From(in safeObject);
		_dynamicOutputs[(node.Id, outputPortIndex)] = variant;
		return variant;
	}

	private async Task<Variant> EvaluateWhitelistedPropertyAsync(XWBPNodeData node)
	{
		int metaInt = GetMetaInt(node, "MethodType");
		switch (metaInt)
		{
		case 0:
		{
			int metaInt2 = GetMetaInt(node, "VariableId");
			if (!_blueprintVariables.TryGetValue(metaInt2, out var value2))
			{
				Stop($"Blueprint variable {metaInt2} is not available in the isolated preview snapshot.");
				return default(Variant);
			}
			return CloneSafe(value2);
		}
		default:
			Stop($"Property mode {metaInt} on node {node.Id} is not supported by safe preview.");
			return default(Variant);
		case 1:
		{
			Dictionary propertyData = GetMetadataDictionary(node, "PropertyData");
			if (IsStaticMember(propertyData))
			{
				Stop($"Static property access on node {node.Id} is not exposed by the preview adapter.");
				return default(Variant);
			}
			if (!TryGetSafeObject(await EvaluateDataInputAsync(node, 0), out var safeObject))
			{
				Stop($"Property target on node {node.Id} is not an isolated preview object.");
				return default(Variant);
			}
			if (!safeObject.MatchesClass(GetDictionaryString(propertyData, "base_class_name")))
			{
				Stop($"Property class on node {node.Id} does not match its isolated preview object.");
				return default(Variant);
			}
			string memberName = GetMemberName(propertyData);
			if (!safeObject.TryGetProperty(memberName, out var value))
			{
				Stop("Property '" + memberName + "' is not in the preview adapter whitelist.");
				return default(Variant);
			}
			return CloneSafe(value);
		}
		}
	}

	private static int GetMetaInt(XWBPNodeData node, string name)
	{
		Variant metaData = node.GetMetaData(name);
		if (metaData.VariantType != Variant.Type.Int)
		{
			return 0;
		}
		return metaData.AsInt32();
	}

	private static Dictionary GetMetadataDictionary(XWBPNodeData node, string name)
	{
		Variant metaData = node.GetMetaData(name);
		if (metaData.VariantType != Variant.Type.Dictionary)
		{
			return new Dictionary();
		}
		return metaData.AsGodotDictionary();
	}

	private static bool IsStaticMember(Dictionary data)
	{
		if (data.TryGetValue("is_static", out var value))
		{
			return value.AsBool();
		}
		return false;
	}

	private static string GetMemberName(Dictionary data)
	{
		string[] array = new string[3] { "name", "method_name", "cs_name" };
		foreach (string key in array)
		{
			string dictionaryString = GetDictionaryString(data, key);
			if (!string.IsNullOrWhiteSpace(dictionaryString))
			{
				return dictionaryString;
			}
		}
		return string.Empty;
	}

	private static string GetDictionaryString(Dictionary data, string key)
	{
		if (!data.TryGetValue(key, out var value))
		{
			return string.Empty;
		}
		return value.AsString();
	}

	private bool CheckTimeBudget()
	{
		int num = Math.Max(1, _context.Limits.MaxExecutionMilliseconds);
		if (Math.Max(0L, _stopwatch.ElapsedMilliseconds - _debugWaitMilliseconds) <= num)
		{
			return true;
		}
		Stop($"Blueprint preview exceeded the {num} ms execution time limit.");
		return false;
	}

	private bool ConsumeLoopIterations(long count, int nodeId)
	{
		int num = Math.Max(1, _context.Limits.MaxLoopIterations);
		if (count < 0 || count > num - _loopIterationCount)
		{
			Stop($"Loop on node {nodeId} exceeds the shared {num} iteration preview limit.");
			return false;
		}
		_loopIterationCount += (int)count;
		return true;
	}

	private Variant UnsupportedValue(string typeId, int nodeId)
	{
		Stop($"Blueprint value node '{typeId}' on node {nodeId} is not supported by safe preview yet.");
		return default(Variant);
	}

	private Task<bool> EnterNodeAsync(XWBPNodeData node)
	{
		return EnterNodeAsync(node, null, debugFlowNode: false);
	}

	private async Task<bool> EnterNodeAsync(XWBPNodeData node, IReadOnlyDictionary<string, XWModDebugVariable> debugVariables, bool debugFlowNode = true)
	{
		if (_context.CancellationToken.IsCancellationRequested)
		{
			_result.WasCancelled = true;
			Stop("Blueprint preview was cancelled.");
			return false;
		}
		if (!CheckTimeBudget())
		{
			return false;
		}
		_result.StepCount++;
		if (_result.StepCount > _context.Limits.MaxSteps)
		{
			Stop($"Blueprint preview exceeded the {_context.Limits.MaxSteps} step limit.");
			return false;
		}
		string nodeLocation = GetCurrentNodeLocation(node.Id);
		int value;
		int num = ((!_nodeVisitCounts.TryGetValue(nodeLocation, out value)) ? 1 : (value + 1));
		_nodeVisitCounts[nodeLocation] = num;
		if (num > _context.Limits.MaxNodeVisitsPerNode)
		{
			Stop($"Blueprint node {nodeLocation} exceeded the {_context.Limits.MaxNodeVisitsPerNode} visit limit.");
			return false;
		}
		if (debugFlowNode && _context.DebugController != null)
		{
			XWModDebugCheckpoint checkpoint = XWModDebugCheckpoint.ForBlueprint(GetBlueprintSourcePath(), GetDebugNodeId(node.Id), GetNodeDisplayName(node), node.TypeId.ToString(), _debugCallStack.ToArray(), debugVariables, node.TypeId.ToString() == "__XWBPGraphNode_Breakpoint");
			Stopwatch debugWait = Stopwatch.StartNew();
			try
			{
				if (await _context.DebugController.CheckpointAsync(checkpoint, _context.CancellationToken) == XWModDebugCheckpointResult.Stopped)
				{
					_result.WasCancelled = true;
					Stop("Blueprint preview was stopped by the debugger.");
					return false;
				}
			}
			catch (OperationCanceledException) when (_context.CancellationToken.IsCancellationRequested)
			{
				_result.WasCancelled = true;
				Stop("Blueprint preview was cancelled.");
				return false;
			}
			finally
			{
				debugWait.Stop();
				_debugWaitMilliseconds += debugWait.ElapsedMilliseconds;
			}
		}
		_result.TraceNodeIds.Add(node.Id);
		_result.TraceLocations.Add(nodeLocation);
		if (_context.NodeLocationVisitedAsync != null)
		{
			await _context.NodeLocationVisitedAsync(new XWBlueprintRuntimeTracePoint
			{
				Graph = _graph,
				GraphId = (_graph?.Id ?? 0),
				FunctionId = _currentFunctionId,
				FunctionName = _currentFunctionName,
				NodeId = node.Id,
				Location = nodeLocation
			});
		}
		else if (_context.NodeVisitedAsync != null)
		{
			await _context.NodeVisitedAsync(node.Id);
		}
		return !_stopped;
	}

	private IReadOnlyDictionary<string, XWModDebugVariable> CreateDebugVariableSnapshot()
	{
		System.Collections.Generic.Dictionary<string, XWModDebugVariable> dictionary = new System.Collections.Generic.Dictionary<string, XWModDebugVariable>(StringComparer.Ordinal);
		Variant value;
		foreach (KeyValuePair<int, Variant> item3 in _blueprintVariables.OrderBy((KeyValuePair<int, Variant> pair) => pair.Key))
		{
			item3.Deconstruct(out var key, out value);
			int value2 = key;
			Variant value3 = value;
			string text = $"蓝图变量[{value2}]";
			dictionary[text] = CreateDebugVariable(text, value3, 0);
		}
		foreach (KeyValuePair<string, Variant> item4 in _variables.OrderBy<KeyValuePair<string, Variant>, string>((KeyValuePair<string, Variant> pair) => pair.Key, StringComparer.Ordinal))
		{
			item4.Deconstruct(out var key2, out value);
			string text2 = key2;
			Variant value4 = value;
			string text3 = "局部变量." + text2;
			dictionary[text3] = CreateDebugVariable(text3, value4, 0);
		}
		foreach (KeyValuePair<(int, int), Variant> item5 in from pair in _dynamicOutputs
			orderby pair.Key.NodeId, pair.Key.PortIndex
			select pair)
		{
			item5.Deconstruct(out var key3, out value);
			(int, int) tuple = key3;
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			Variant value5 = value;
			string text4 = $"动态值[{item}:{item2}]";
			dictionary[text4] = CreateDebugVariable(text4, value5, 0);
		}
		return dictionary;
	}

	private XWModDebugVariable CreateDebugVariable(string name, Variant value, int depth)
	{
		if (depth >= 3)
		{
			return new XWModDebugVariable(name, value.VariantType.ToString(), FormatValue(value));
		}
		List<XWModDebugVariable> list = null;
		if (value.VariantType == Variant.Type.Array)
		{
			Godot.Collections.Array array = value.AsGodotArray();
			list = new List<XWModDebugVariable>(Math.Min(array.Count, 32));
			for (int i = 0; i < Math.Min(array.Count, 32); i++)
			{
				list.Add(CreateDebugVariable($"[{i}]", array[i], depth + 1));
			}
		}
		else if (value.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = value.AsGodotDictionary();
			list = new List<XWModDebugVariable>(Math.Min(dictionary.Count, 32));
			int num = 0;
			foreach (Variant key in dictionary.Keys)
			{
				if (num++ >= 32)
				{
					break;
				}
				string name2 = "[" + FormatValue(key) + "]";
				list.Add(CreateDebugVariable(name2, dictionary[key], depth + 1));
			}
		}
		return new XWModDebugVariable(name, value.VariantType.ToString(), FormatValue(value), list);
	}

	private string GetBlueprintSourcePath()
	{
		string text = ((!string.IsNullOrWhiteSpace(_context.BlueprintSourcePath)) ? _context.BlueprintSourcePath : _graph?.Name);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "blueprint://runtime";
	}

	private string GetCurrentNodeLocation(int nodeId)
	{
		if (_currentFunctionId != 0)
		{
			return $"function:{_currentFunctionName}#{_currentFunctionId}/node:{nodeId}";
		}
		return $"graph:{_graph?.Id ?? 0}/node:{nodeId}";
	}

	private string GetDebugNodeId(int nodeId)
	{
		if (_currentFunctionId != 0)
		{
			return $"{_currentFunctionId}:{nodeId}";
		}
		return nodeId.ToString(CultureInfo.InvariantCulture);
	}

	private string GetNodeDisplayName(XWBPNodeData node)
	{
		string text = node?.NodeType?.DisplayName;
		if (string.IsNullOrWhiteSpace(text) && node != null)
		{
			text = XWBPNodeRegistry.Instance.GetNodeType(node.TypeId.ToString())?.DisplayName;
		}
		text = ((!string.IsNullOrWhiteSpace(text)) ? text : (node?.TypeId.ToString() ?? string.Empty));
		if (_currentFunctionId != 0)
		{
			return _currentFunctionName + " · " + text;
		}
		return text;
	}

	private void WriteOutput(string message)
	{
		if (_result.OutputMessages.Count >= _context.Limits.MaxOutputMessages)
		{
			Stop($"Blueprint preview exceeded the {_context.Limits.MaxOutputMessages} message limit.");
		}
		else
		{
			message = LimitString(message ?? string.Empty);
			_result.OutputMessages.Add(message);
			_context.Output?.Invoke(message);
		}
	}

	private void Stop(string reason)
	{
		if (!_stopped)
		{
			_stopped = true;
			if (reason == null)
			{
				reason = "Blueprint preview stopped.";
			}
			_result.StopReason = ((_currentFunctionId == 0) ? reason : $"[Blueprint function '{_currentFunctionName}' ({_currentFunctionId})] {reason}");
			_result.Diagnostics.Add(_result.StopReason);
		}
	}

	private static List<int> DataInputIndexes(XWBPNodeData node)
	{
		return (from pair in node.InputPorts.Select((XWBPNodePortData port, int index) => (port: port, index: index))
			where pair.port != null && pair.port.PortTypeValue != XWBPNodePortData.PortType.Flow
			select pair.index).ToList();
	}

	private static List<int> DataOutputIndexes(XWBPNodeData node)
	{
		return (from pair in node.OutputPorts.Select((XWBPNodePortData port, int index) => (port: port, index: index))
			where pair.port != null && pair.port.PortTypeValue != XWBPNodePortData.PortType.Flow
			select pair.index).ToList();
	}

	private static List<int> FlowOutputIndexes(XWBPNodeData node)
	{
		return (from pair in node.OutputPorts.Select((XWBPNodePortData port, int index) => (port: port, index: index))
			where pair.port != null && pair.port.PortTypeValue == XWBPNodePortData.PortType.Flow
			select pair.index).ToList();
	}

	private Variant CloneSafe(Variant value)
	{
		if (XWBlueprintSafeValue.TryClone(value, _context.Limits, (XWBlueprintSafeObject safeObject) => GodotObject.IsInstanceValid(safeObject) && _safeObjectIds.Contains(safeObject.GetInstanceId()), ref _clonedValueCount, 0, out var clone, out var diagnostic))
		{
			return clone;
		}
		Stop(diagnostic);
		return default(Variant);
	}

	private string LimitString(string value)
	{
		if (value == null)
		{
			value = string.Empty;
		}
		if (value.Length <= _context.Limits.MaxStringLength)
		{
			return value;
		}
		Stop($"String exceeds the {_context.Limits.MaxStringLength} character preview limit.");
		return value.Substring(0, _context.Limits.MaxStringLength);
	}

	private Godot.Collections.Array ArrayValue(Variant value, int nodeId)
	{
		if (value.VariantType == Variant.Type.Array)
		{
			return value.AsGodotArray();
		}
		Stop($"Array node {nodeId} received a non-array value.");
		return new Godot.Collections.Array();
	}

	private Dictionary DictionaryValue(Variant value, int nodeId)
	{
		if (value.VariantType == Variant.Type.Dictionary)
		{
			return value.AsGodotDictionary();
		}
		Stop($"Dictionary node {nodeId} received a non-dictionary value.");
		return new Dictionary();
	}

	private Variant ArrayGet(Godot.Collections.Array array, long index, int nodeId)
	{
		if (index < 0 || index >= array.Count)
		{
			Stop($"Array index {index} is outside the bounds on node {nodeId}.");
			return default(Variant);
		}
		return CloneSafe(array[(int)index]);
	}

	private static bool ArrayContains(Godot.Collections.Array array, Variant value)
	{
		foreach (Variant item in array)
		{
			if (ValuesEqual(item, value))
			{
				return true;
			}
		}
		return false;
	}

	private Variant DivideInt(Variant left, Variant right, int nodeId)
	{
		long num = ToLong(right);
		if (num == 0L)
		{
			Stop($"Integer division by zero on node {nodeId}.");
			return default(Variant);
		}
		return Variant.From<long>(ToLong(left) / num);
	}

	private Variant Modulo(Variant left, Variant right, int nodeId)
	{
		long num = ToLong(right);
		if (num == 0L)
		{
			Stop($"Modulo by zero on node {nodeId}.");
			return default(Variant);
		}
		return Variant.From<long>(ToLong(left) % num);
	}

	private Variant DivideFloat(Variant left, Variant right, int nodeId)
	{
		double num = ToDouble(right);
		if (Math.Abs(num) <= 5E-324)
		{
			Stop($"Floating-point division by zero on node {nodeId}.");
			return default(Variant);
		}
		return Variant.From<double>(ToDouble(left) / num);
	}

	private static bool ValuesEqual(Variant left, Variant right)
	{
		if (IsNumeric(left) && IsNumeric(right))
		{
			return Math.Abs(ToDouble(left) - ToDouble(right)) <= 1E-06;
		}
		return left.Equals(right);
	}

	private static int Compare(Variant left, Variant right)
	{
		if (IsNumeric(left) && IsNumeric(right))
		{
			return ToDouble(left).CompareTo(ToDouble(right));
		}
		return string.Compare(FormatValue(left), FormatValue(right), StringComparison.Ordinal);
	}

	private static bool IsNumeric(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)(variantType - 2) <= 1uL)
		{
			return true;
		}
		return false;
	}

	private static long ToLong(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 3uL)
		{
			switch ((int)num)
			{
			case 0:
				return value.AsBool() ? 1 : 0;
			case 1:
				return value.AsInt64();
			case 2:
				return checked((long)value.AsDouble());
			case 3:
			{
				if (long.TryParse(value.AsString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
				{
					return result;
				}
				break;
			}
			}
		}
		return 0L;
	}

	private static double ToDouble(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 3uL)
		{
			switch ((int)num)
			{
			case 0:
				return value.AsBool() ? 1.0 : 0.0;
			case 1:
				return value.AsInt64();
			case 2:
				return value.AsDouble();
			case 3:
			{
				if (double.TryParse(value.AsString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					return result;
				}
				break;
			}
			}
		}
		return 0.0;
	}

	private static bool ToBool(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 1:
				return value.AsBool();
			case 2:
				return value.AsInt64() != 0;
			case 3:
				return Math.Abs(value.AsDouble()) > 5E-324;
			case 4:
			{
				bool result;
				return !string.IsNullOrEmpty(value.AsString()) && (!bool.TryParse(value.AsString(), out result) || result);
			}
			case 0:
				return false;
			}
		}
		return true;
	}

	private static Vector2 ToVector2(Variant value)
	{
		if (value.VariantType != Variant.Type.Vector2)
		{
			return Vector2.Zero;
		}
		return value.AsVector2();
	}

	private static Vector3 ToVector3(Variant value)
	{
		if (value.VariantType != Variant.Type.Vector3)
		{
			return Vector3.Zero;
		}
		return value.AsVector3();
	}

	private static Vector3 ToVector3(Vector2 value)
	{
		return new Vector3(value.X, value.Y, 0f);
	}

	private static Vector2 ToVector2(Vector3 value)
	{
		return new Vector2(value.X, value.Y);
	}

	private static string FormatValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return string.Empty;
			case 1:
				return value.AsBool() ? "true" : "false";
			case 2:
				return value.AsInt64().ToString(CultureInfo.InvariantCulture);
			case 3:
				return value.AsDouble().ToString(CultureInfo.InvariantCulture);
			case 4:
				return value.AsString();
			}
		}
		if (variantType == Variant.Type.StringName)
		{
			return value.AsStringName().ToString();
		}
		return value.ToString();
	}

	private static object ToPlainObject(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return null;
			case 1:
				return value.AsBool();
			case 2:
				return value.AsInt64();
			case 3:
				return value.AsDouble();
			case 4:
				return value.AsString();
			}
		}
		return FormatValue(value);
	}

	private static async Task YieldOnceAsync()
	{
		await Task.Yield();
	}
}
