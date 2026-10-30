using System;
using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Blueprint;

namespace PVZHE.ModEditor.Registry.BP;

public sealed partial class XWBPNodeRegistry : RefCounted
{
	private static XWBPNodeRegistry _instance;

	private readonly Dictionary<string, XWBPNodeType> _nodeTypes = new Dictionary<string, XWBPNodeType>();

	private readonly Dictionary<string, List<string>> _categories = new Dictionary<string, List<string>>();

	private readonly Dictionary<string, Type> _nodeFactories = new Dictionary<string, Type>();

	public static XWBPNodeRegistry Instance
	{
		get
		{
			if (_instance == null || !GodotObject.IsInstanceValid(_instance))
			{
				_instance = new XWBPNodeRegistry();
			}
			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}

	public XWBPNodeRegistry()
	{
		RegisterBuiltInNodeTypes();
	}

	public void RegisterBuiltInNodeTypes()
	{
		RegisterNodeType(new XWBPNodeEntry());
		RegisterNodeType(new XWBPNodeMethodEntry());
		RegisterNodeType(new XWBPNodeSignalEvent());
		RegisterNodeType(new XWBPNodeEmitSignal());
		RegisterNodeType(new XWBPNodeBranch());
		RegisterNodeType(new XWBPNodeSequence());
		RegisterNodeType(new XWBPNodeForLoop());
		RegisterNodeType(new XWBPNodeForEach());
		RegisterNodeType(new XWBPNodeWhileLoop());
		RegisterNodeType(new XWBPNodeReturn());
		RegisterNodeType(new XWBPNodeBreak());
		RegisterNodeType(new XWBPNodeContinue());
		RegisterNodeType(new XWBPNodeDelay());
		RegisterNodeType(new XWBPNodeCallMethod());
		RegisterNodeType(new XWBPNodeGetProperty());
		RegisterNodeType(new XWBPNodeSetProperty());
		RegisterNodeType(new XWBPNodeArrayGet());
		RegisterNodeType(new XWBPNodeDictionaryGet());
		RegisterNodeType(new XWBPNodeAnd());
		RegisterNodeType(new XWBPNodeOr());
		RegisterNodeType(new XWBPNodeNot());
		RegisterNodeType(new XWBPNodeEqual());
		RegisterNodeType(new XWBPNodeGreater());
		RegisterNodeType(new XWBPNodeLess());
		RegisterNodeType(new XWBPNodeAddFloat());
		RegisterNodeType(new XWBPNodeSubtractFloat());
		RegisterNodeType(new XWBPNodeMultiplyFloat());
		RegisterNodeType(new XWBPNodeDivideFloat());
		RegisterNodeType(new XWBPNodeAddInt());
		RegisterNodeType(new XWBPNodeSubtractInt());
		RegisterNodeType(new XWBPNodeMultiplyInt());
		RegisterNodeType(new XWBPNodeDivideInt());
		RegisterNodeType(new XWBPNodeModulo());
		RegisterNodeType(new XWBPNodeClamp());
		RegisterNodeType(new XWBPNodeAbs());
		RegisterNodeType(new XWBPNodeRound());
		RegisterNodeType(new XWBPNodeFloor());
		RegisterNodeType(new XWBPNodeCeil());
		RegisterNodeType(new XWBPNodeLerp());
		RegisterNodeType(new XWBPNodeMin());
		RegisterNodeType(new XWBPNodeMax());
		RegisterNodeType(new XWBPNodeRandomFloat());
		RegisterNodeType(new XWBPNodeRandomInt());
		RegisterNodeType(new XWBPNodeSin());
		RegisterNodeType(new XWBPNodeCos());
		RegisterNodeType(new XWBPNodeTan());
		RegisterNodeType(new XWBPNodePow());
		RegisterNodeType(new XWBPNodeSqrt());
		RegisterNodeType(new XWBPNodeLog());
		RegisterNodeType(new XWBPNodeDegToRad());
		RegisterNodeType(new XWBPNodeRadToDeg());
		RegisterNodeType(new XWBPNodeVector2Add());
		RegisterNodeType(new XWBPNodeVector2Subtract());
		RegisterNodeType(new XWBPNodeVector2Multiply());
		RegisterNodeType(new XWBPNodeVector2Length());
		RegisterNodeType(new XWBPNodeVector2Normalize());
		RegisterNodeType(new XWBPNodeVector2Dot());
		RegisterNodeType(new XWBPNodeVector3Add());
		RegisterNodeType(new XWBPNodeVector3Length());
		RegisterNodeType(new XWBPNodeVector3Normalize());
		RegisterNodeType(new XWBPNodeVector3Dot());
		RegisterNodeType(new XWBPNodeStringConcat());
		RegisterNodeType(new XWBPNodeStringSplit());
		RegisterNodeType(new XWBPNodeStringReplace());
		RegisterNodeType(new XWBPNodeStringLength());
		RegisterNodeType(new XWBPNodeStringContains());
		RegisterNodeType(new XWBPNodeStringSubstring());
		RegisterNodeType(new XWBPNodeStringToUpper());
		RegisterNodeType(new XWBPNodeStringToLower());
		RegisterNodeType(new XWBPNodeComment());
		RegisterNodeType(new XWBPNodeLiteral());
		RegisterNodeType(new XWBPNodeLoadResource());
		RegisterNodeType(new XWBPNodePrint());
		RegisterNodeType(new XWBPNodeGetSelf());
		RegisterNodeType(new XWBPNodeFrame());
		RegisterNodeType(new XWBPNodeGetType());
		RegisterNodeType(new XWBPNodeBreakpoint());
		RegisterNodeType(new XWBPNodePrintFormatted());
		RegisterNodeType(new XWBPNodeToString());
		RegisterNodeType(new XWBPNodeToFloat());
		RegisterNodeType(new XWBPNodeToInt());
		RegisterNodeType(new XWBPNodeToBool());
		RegisterNodeType(new XWBPNodeStringToInt());
		RegisterNodeType(new XWBPNodeStringToFloat());
		RegisterNodeType(new XWBPNodeFloatToInt());
		RegisterNodeType(new XWBPNodeMakeVector2());
		RegisterNodeType(new XWBPNodeMakeVector3());
		RegisterNodeType(new XWBPNodeBreakVector2());
		RegisterNodeType(new XWBPNodeBreakVector3());
		RegisterNodeType(new XWBPNodeVector2ToVector3());
		RegisterNodeType(new XWBPNodeVector3ToVector2());
		RegisterNodeType(new XWBPNodeXor());
		RegisterNodeType(new XWBPNodeNor());
		RegisterNodeType(new XWBPNodeNand());
		RegisterNodeType(new XWBPNodeCompareInt());
		RegisterNodeType(new XWBPNodeCompareFloat());
		RegisterNodeType(new XWBPNodeCompareString());
		RegisterNodeType(new XWBPNodeIsNull());
		RegisterNodeType(new XWBPNodeIsValid());
		RegisterNodeType(new XWBPNodeSelectBool());
		RegisterNodeType(new XWBPNodeMatch());
		RegisterNodeType(new XWBPNodeCustomEvent());
		RegisterNodeType(new XWBPNodeOnStart());
		RegisterNodeType(new XWBPNodeOnDestroy());
		RegisterNodeType(new XWBPNodeTriggerEvent());
		RegisterNodeType(new XWBPNodeTimerEvent());
		RegisterNodeType(new XWBPNodeVector3Subtract());
		RegisterNodeType(new XWBPNodeVector3Multiply());
		RegisterNodeType(new XWBPNodeLocalVariable());
		RegisterNodeType(new XWBPNodeMakeArray());
		RegisterNodeType(new XWBPNodeArrayAdd());
		RegisterNodeType(new XWBPNodeArrayContains());
		RegisterNodeType(new XWBPNodeArrayClear());
		RegisterNodeType(new XWBPNodeArrayFind());
		RegisterNodeType(new XWBPNodeArrayInsert());
		RegisterNodeType(new XWBPNodeArrayRemove());
		RegisterNodeType(new XWBPNodeArrayLength());
		RegisterNodeType(new XWBPNodeArraySet());
		RegisterNodeType(new XWBPNodeMakeDict());
		RegisterNodeType(new XWBPNodeDictGet());
		RegisterNodeType(new XWBPNodeDictSet());
		RegisterNodeType(new XWBPNodeDictKeys());
		RegisterNodeType(new XWBPNodeDictValues());
		RegisterNodeType(new XWBPNodeDictContains());
		RegisterNodeType(new XWBPNodeDictRemove());
		RegisterNodeType(new XWBPNodeDictClear());
		RegisterNodeType(new XWBPNodeDictSize());
	}

	public void RegisterNodeType(string typeId, XWBPNodeType nodeType)
	{
		if (string.IsNullOrEmpty(typeId) || nodeType == null)
		{
			return;
		}
		_nodeTypes[typeId] = nodeType;
		_nodeFactories[typeId] = nodeType.GetType();
		string text = nodeType.Category ?? "";
		if (!string.IsNullOrEmpty(text))
		{
			if (!_categories.TryGetValue(text, out var value))
			{
				value = new List<string>();
				_categories[text] = value;
			}
			if (!value.Contains(typeId))
			{
				value.Add(typeId);
			}
		}
	}

	public void RegisterNodeType(XWBPNodeType nodeType)
	{
		if (nodeType != null)
		{
			RegisterNodeType(nodeType.TypeId, nodeType);
		}
	}

	public void UnregisterNodeType(string typeId)
	{
		if (string.IsNullOrEmpty(typeId) || !_nodeTypes.TryGetValue(typeId, out var value))
		{
			return;
		}
		string text = value.Category ?? "";
		if (!string.IsNullOrEmpty(text) && _categories.TryGetValue(text, out var value2))
		{
			value2.Remove(typeId);
			if (value2.Count == 0)
			{
				_categories.Remove(text);
			}
		}
		_nodeTypes.Remove(typeId);
		_nodeFactories.Remove(typeId);
	}

	public XWBPNodeType CreateNodeType(string typeId)
	{
		if (string.IsNullOrEmpty(typeId))
		{
			return null;
		}
		if (!_nodeFactories.TryGetValue(typeId, out var value))
		{
			return null;
		}
		try
		{
			return (XWBPNodeType)Activator.CreateInstance(value);
		}
		catch
		{
			return null;
		}
	}

	public XWBPNodeType GetNodeType(string typeId)
	{
		return CreateNodeType(typeId);
	}

	public IReadOnlyDictionary<string, XWBPNodeType> GetAllNodeTypes()
	{
		return _nodeTypes;
	}

	public IReadOnlyDictionary<string, List<string>> GetCategories()
	{
		return _categories;
	}

	public List<XWBPNodeType> GetNodeTypesByCategory(string category)
	{
		List<XWBPNodeType> list = new List<XWBPNodeType>();
		if (string.IsNullOrEmpty(category) || !_categories.TryGetValue(category, out var value))
		{
			return list;
		}
		foreach (string item in value)
		{
			if (_nodeTypes.TryGetValue(item, out var value2))
			{
				list.Add(value2);
			}
		}
		return list;
	}

	public List<XWBPNodeType> SearchTypes(string query)
	{
		List<XWBPNodeType> list = new List<XWBPNodeType>();
		if (string.IsNullOrEmpty(query))
		{
			return list;
		}
		string value = query.ToLowerInvariant();
		foreach (KeyValuePair<string, XWBPNodeType> nodeType in _nodeTypes)
		{
			XWBPNodeType value2 = nodeType.Value;
			if (value2 != null)
			{
				string text = (value2.DisplayName ?? "").ToLowerInvariant();
				string text2 = (value2.TypeId?.ToString() ?? "").ToLowerInvariant();
				string text3 = (value2.Description ?? "").ToLowerInvariant();
				if (text.Contains(value) || text2.Contains(value) || text3.Contains(value))
				{
					list.Add(value2);
				}
			}
		}
		return list;
	}

	internal static void ResetInstance()
	{
		_instance = null;
	}
}
