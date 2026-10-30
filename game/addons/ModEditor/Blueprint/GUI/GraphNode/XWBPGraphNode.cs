using System;
using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPGraphNode : GraphNode
{
	[Signal]
	public delegate void NodeChangedEventHandler(XWBPGraphNode node);

	[Signal]
	public delegate void PortValueChangedEventHandler(XWBPGraphNode node, string portName, Variant value);

	private const string ScenePath = "res://addons/ModEditor/Blueprint/GUI/GraphNode/XWBPGraphNode.tscn";

	private static PackedScene _scene;

	private static readonly Dictionary<string, StyleBoxFlat> _styleCache = new Dictionary<string, StyleBoxFlat>();

	private Vector2 _resizeStartSize;

	private bool _resizeInProgress;

	public XWBPEditor Editor { get; set; }

	public XWBPNodeData NodeData { get; private set; }

	public XWBPNodeType NodeType { get; private set; }

	public Dictionary<string, Control> PortWidgets { get; } = new Dictionary<string, Control>();

	public static XWBPGraphNode Create()
	{
		if (_scene == null)
		{
			_scene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Blueprint/GUI/GraphNode/XWBPGraphNode.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		return _scene.Instantiate<XWBPGraphNode>(PackedScene.GenEditState.Disabled);
	}

	public void Init(XWBPNodeData nodeData, XWBPNodeType nodeType)
	{
		NodeData = nodeData;
		NodeType = nodeType;
		nodeData.NodeType = nodeType;
		base.Title = nodeType.DisplayName;
		base.PositionOffset = nodeData.Position;
		if (nodeData.Size != Vector2.Zero)
		{
			base.Size = nodeData.Size;
		}
		base.PositionOffsetChanged += OnPositionOffsetChanged;
		base.Dragged += OnDragged;
		base.ResizeRequest += OnResizeRequest;
		base.ResizeEnd += OnResizeEnd;
		InitStyle(nodeType.Color);
		nodeType.GraphNodeInit(this);
		BuildPorts();
		UpdateTooltip();
	}

	private static string GetStyleKey(Color nodeColor, string styleName)
	{
		return $"{styleName}_{nodeColor.R:F3}_{nodeColor.G:F3}_{nodeColor.B:F3}";
	}

	private static StyleBoxFlat GetOrCreateStyle(string key, Func<StyleBoxFlat> createFunc)
	{
		if (_styleCache.TryGetValue(key, out var value))
		{
			return value;
		}
		value = createFunc();
		_styleCache[key] = value;
		return value;
	}

	private void InitStyle(Color nodeColor)
	{
		StyleBoxFlat orCreateStyle = GetOrCreateStyle(GetStyleKey(nodeColor, "titlebar"), () => new StyleBoxFlat
		{
			BgColor = nodeColor,
			BorderColor = new Color(nodeColor.R * 0.5f, nodeColor.G * 0.5f, nodeColor.B * 0.5f, 0.6f),
			BorderWidthBottom = 1,
			ExpandMarginLeft = 4f,
			ExpandMarginRight = 4f,
			ExpandMarginTop = 4f,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			ContentMarginLeft = 8f,
			ContentMarginTop = 4f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 4f
		});
		AddThemeStyleboxOverride("titlebar", orCreateStyle);
		StyleBoxFlat orCreateStyle2 = GetOrCreateStyle(GetStyleKey(nodeColor, "panel"), () => new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.06f, 0.068f, 0.98f),
			BorderColor = new Color(0.04f, 0.54f, 1f, 0.08f),
			BorderWidthBottom = 1,
			BorderWidthLeft = 1,
			BorderWidthRight = 1,
			BorderWidthTop = 1,
			ExpandMarginLeft = 4f,
			ExpandMarginRight = 4f,
			ExpandMarginBottom = 4f,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
			ContentMarginLeft = 6f,
			ContentMarginTop = 4f,
			ContentMarginRight = 6f,
			ContentMarginBottom = 6f
		});
		AddThemeStyleboxOverride("panel", orCreateStyle2);
		StyleBoxFlat orCreateStyle3 = GetOrCreateStyle(GetStyleKey(nodeColor, "panel_selected"), () => new StyleBoxFlat
		{
			BgColor = new Color(0.06f, 0.06f, 0.068f, 0.98f),
			BorderColor = new Color(0.04f, 0.54f, 1f, 0.9f),
			BorderWidthBottom = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			BorderWidthTop = 2,
			ExpandMarginLeft = 4f,
			ExpandMarginRight = 4f,
			ExpandMarginBottom = 4f,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
			ShadowColor = new Color(0.04f, 0.54f, 1f, 0.25f),
			ShadowSize = 8,
			ContentMarginLeft = 6f,
			ContentMarginTop = 4f,
			ContentMarginRight = 6f,
			ContentMarginBottom = 6f
		});
		AddThemeStyleboxOverride("panel_selected", orCreateStyle3);
		StyleBoxFlat orCreateStyle4 = GetOrCreateStyle(GetStyleKey(nodeColor, "titlebar_selected"), () => new StyleBoxFlat
		{
			BgColor = nodeColor,
			BorderColor = new Color(0.04f, 0.54f, 1f, 0.9f),
			BorderWidthBottom = 2,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			BorderWidthTop = 2,
			ExpandMarginLeft = 4f,
			ExpandMarginRight = 4f,
			ExpandMarginBottom = 0f,
			ExpandMarginTop = 4f,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 0,
			CornerRadiusBottomRight = 0,
			ShadowColor = new Color(0.04f, 0.54f, 1f, 0.25f),
			ShadowSize = 8,
			ContentMarginLeft = 8f,
			ContentMarginTop = 4f,
			ContentMarginRight = 8f,
			ContentMarginBottom = 4f
		});
		AddThemeStyleboxOverride("titlebar_selected", orCreateStyle4);
	}

	public async void BuildPorts()
	{
		ClearAllSlots();
		foreach (Node child in GetChildren())
		{
			if (child != null)
			{
				Node node = child;
				node.QueueFree();
			}
		}
		PortWidgets.Clear();
		NodeType.GraphNodeBuildRefresh(this);
		BuildBasePorts();
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		if (GodotObject.IsInstanceValid(this) && GodotObject.IsInstanceValid(NodeData) && NodeData.Size != Vector2.Zero)
		{
			base.Size = new Vector2(base.Size.X, NodeData.Size.Y);
		}
	}

	private int BuildBasePorts()
	{
		int count = NodeData.InputPorts.Count;
		int count2 = NodeData.OutputPorts.Count;
		int num = Math.Max(count, count2);
		for (int i = 0; i < num; i++)
		{
			XWBPGraphNodePortContainer xWBPGraphNodePortContainer = XWBPGraphNodePortContainer.Create();
			AddChild(xWBPGraphNodePortContainer, forceReadableName: false, InternalMode.Disabled);
			bool num2 = i < count;
			bool flag = i < count2;
			XWBPNodePortData xWBPNodePortData = (num2 ? NodeData.InputPorts[i] : null);
			XWBPNodePortData xWBPNodePortData2 = (flag ? NodeData.OutputPorts[i] : null);
			if (num2)
			{
				xWBPGraphNodePortContainer.InputSet(xWBPNodePortData);
				if (xWBPGraphNodePortContainer.InputEditor != null)
				{
					PortWidgets[xWBPNodePortData.Name] = xWBPGraphNodePortContainer.InputEditor;
				}
				xWBPGraphNodePortContainer.InputValueChange += OnInputPortValueChanged;
			}
			if (flag)
			{
				xWBPGraphNodePortContainer.OutputSet(xWBPNodePortData2);
			}
			bool flag2 = num2;
			int typeLeft = (int)(num2 ? xWBPNodePortData.PortTypeValue : XWBPNodePortData.PortType.Unknown);
			Color colorLeft = (num2 ? XWBPNodePortData.GetTypeColor(xWBPNodePortData.PortTypeValue, xWBPNodePortData.ClassName) : Colors.White);
			bool flag3 = flag;
			int typeRight = (int)(flag ? xWBPNodePortData2.PortTypeValue : XWBPNodePortData.PortType.Unknown);
			Color colorRight = (flag ? XWBPNodePortData.GetTypeColor(xWBPNodePortData2.PortTypeValue, xWBPNodePortData2.ClassName) : Colors.White);
			SetSlot(i, flag2, typeLeft, colorLeft, flag3, typeRight, colorRight);
			if (flag2 && xWBPNodePortData.PortTypeValue == XWBPNodePortData.PortType.Flow)
			{
				SetSlotCustomIconLeft(i, XWBPNodePortData.GetTypeIcon(XWBPNodePortData.PortType.Flow));
			}
			if (flag3 && xWBPNodePortData2.PortTypeValue == XWBPNodePortData.PortType.Flow)
			{
				SetSlotCustomIconRight(i, XWBPNodePortData.GetTypeIcon(XWBPNodePortData.PortType.Flow));
			}
		}
		return num;
	}

	private void OnInputPortValueChanged(XWBPNodePortData portData, Variant oldValue, Variant newValue)
	{
		if (GodotObject.IsInstanceValid(portData))
		{
			Editor?.CommitGraphPortValue(portData, oldValue, newValue);
			EmitSignal(SignalName.PortValueChanged, this, portData.Name, newValue);
		}
	}

	private void OnPositionOffsetChanged()
	{
		if (GodotObject.IsInstanceValid(NodeData))
		{
			NodeData.Position = base.PositionOffset;
		}
		EmitSignal(SignalName.NodeChanged, this);
	}

	private void OnDragged(Vector2 from, Vector2 to)
	{
		if (GodotObject.IsInstanceValid(NodeData) && !from.IsEqualApprox(to))
		{
			Editor?.CommitGraphNodePosition(NodeData, from, to);
		}
	}

	private void OnResizeRequest(Vector2 newSize)
	{
		if (!_resizeInProgress)
		{
			_resizeStartSize = ((GodotObject.IsInstanceValid(NodeData) && NodeData.Size != Vector2.Zero) ? NodeData.Size : base.Size);
			_resizeInProgress = true;
		}
		base.Size = newSize;
	}

	private void OnResizeEnd(Vector2 newSize)
	{
		Vector2 oldSize = (_resizeInProgress ? _resizeStartSize : (NodeData?.Size ?? base.Size));
		_resizeInProgress = false;
		if (GodotObject.IsInstanceValid(NodeData) && !oldSize.IsEqualApprox(newSize))
		{
			Editor?.CommitGraphNodeSize(NodeData, oldSize, newSize);
		}
	}

	public void RemoveSelf()
	{
		if (GetParent() is XWBPGraphEdit xWBPGraphEdit)
		{
			xWBPGraphEdit.RemoveGraphNodeWithName(base.Name);
		}
	}

	public void RenameSelf(string newName)
	{
		if (NodeType.NodeTypeEnumValue == XWBPNodeType.NodeTypeEnum.Function)
		{
			base.Title = "调用 " + newName;
		}
		else if (NodeType.NodeTypeEnumValue == XWBPNodeType.NodeTypeEnum.FunctionEntry)
		{
			base.Title = newName;
		}
	}

	public void PortChange()
	{
		BuildPorts();
	}

	public void UpdateTooltip()
	{
		string text = "";
		text += $"节点 ID: {NodeData.Id}\n";
		text += $"类型 ID: {NodeType.TypeId}\n";
		text = text + "显示名称: " + NodeType.DisplayName + "\n";
		if (!string.IsNullOrEmpty(NodeType.Category))
		{
			text = text + "分类: " + NodeType.Category + "\n";
		}
		text = text + "节点类型: " + GetNodeTypeName(NodeType.NodeTypeEnumValue) + "\n";
		if (!string.IsNullOrEmpty(NodeType.Description))
		{
			text = text + "\n描述:\n" + NodeType.Description;
		}
		base.TooltipText = text;
	}

	private static string GetNodeTypeName(XWBPNodeType.NodeTypeEnum type)
	{
		return type switch
		{
			XWBPNodeType.NodeTypeEnum.General => "通用", 
			XWBPNodeType.NodeTypeEnum.Entry => "入口", 
			XWBPNodeType.NodeTypeEnum.Function => "函数", 
			XWBPNodeType.NodeTypeEnum.FunctionEntry => "函数入口", 
			XWBPNodeType.NodeTypeEnum.PropertyGet => "属性获取", 
			XWBPNodeType.NodeTypeEnum.PropertySet => "属性设置", 
			_ => "未知", 
		};
	}
}
