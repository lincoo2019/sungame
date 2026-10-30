using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWWindowBPNodeSelector : XWWindowSelector
{
	[Signal]
	public delegate void NodeTypeSelectedEventHandler(XWBPNodeType nodeType);

	private sealed class VisualNodeChoice
	{
		public TreeItem Item { get; init; }

		public XWBPNodeType NodeType { get; set; }

		public string Category { get; init; }
	}

	private sealed class DeferredNodeChoice
	{
		public Func<XWBPNodeType> Factory { get; init; }

		public DeferredPortCatalog Ports { get; init; }

		public XWBPNodeType NodeType { get; set; }
	}

	private sealed class DeferredPortCatalog
	{
		public List<DeferredPortShape> Inputs { get; } = new List<DeferredPortShape>();

		public List<DeferredPortShape> Outputs { get; } = new List<DeferredPortShape>();
	}

	private readonly struct DeferredPortShape(XWBPNodePortData.PortType portType, string className = "")
	{
		public XWBPNodePortData.PortType PortType { get; } = portType;

		public string ClassName { get; } = className ?? "";
	}

	private enum MemberSource
	{
		ClassRegistry,
		CSharpRegistry
	}

	private enum MemberKind
	{
		Method,
		Property,
		Signal
	}

	private const int MethodFlagVirtualClassDb = 8;

	private const int MethodFlagStaticClassDb = 32;

	private const int MethodFlagVirtualCSharp = 64;

	private const int PropertyUsageStorage = 2;

	private const int VisualPageSize = 36;

	private const string ProjectApiCategoryName = "当前 Mod C# API";

	private static PackedScene _scene;

	private static Texture2D _memberMethodIcon;

	private static Texture2D _memberSignalIcon;

	private static Texture2D _variantIcon;

	private HFlowContainer _nodeCardGrid;

	private HBoxContainer _categoryChips;

	private TabContainer _workspaceTabs;

	private Label _catalogCount;

	private Label _catalogEmpty;

	private Label _pageLabel;

	private Button _previousPageButton;

	private Button _nextPageButton;

	private ButtonGroup _categoryButtonGroup;

	private ButtonGroup _cardButtonGroup;

	private readonly List<VisualNodeChoice> _visualChoices = new List<VisualNodeChoice>();

	private readonly System.Collections.Generic.Dictionary<Button, TreeItem> _visibleCardItems = new System.Collections.Generic.Dictionary<Button, TreeItem>();

	private readonly System.Collections.Generic.Dictionary<string, Texture2D> _categoryIcons = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

	private readonly System.Collections.Generic.Dictionary<TreeItem, DeferredNodeChoice> _deferredNodeChoices = new System.Collections.Generic.Dictionary<TreeItem, DeferredNodeChoice>();

	private string _activeVisualCategory = "";

	private string _visualQuery = "";

	private int _visualPage;

	private TreeItem _projectApiCategory;

	private bool _projectApiPopulated;

	private string _catalogProjectRoot = "";

	private int _catalogProjectContextVersion;

	private XWBPScriptData _catalogScriptData;

	private XWBPGraphData _catalogGraph;

	private bool _projectIndexRequested;

	private XWBPNodeRegistry _registry;

	private XWBPCSharpMemberRegistry _csharpRegistry;

	private static PackedScene Scene => _scene ?? (_scene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Blueprint/GUI/Window/NodeSelector/XWWindowBPNodeSelector.tscn", null, ResourceLoader.CacheMode.Reuse));

	public XWBPEditor Editor { get; set; }

	public XWBPGraphData CurrentGraph { get; set; }

	public XWBPNodePortData FilterPortData { get; set; }

	public bool FilterSourceIsOutput { get; set; } = true;

	private bool IsCSharpCatalogReady
	{
		get
		{
			if (_csharpRegistry != null && _csharpRegistry.IsProjectScanReady)
			{
				return _csharpRegistry.IsHostScanReady;
			}
			return false;
		}
	}

	public static XWWindowBPNodeSelector Create()
	{
		return Scene.Instantiate<XWWindowBPNodeSelector>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_registry = XWBPNodeRegistry.Instance;
		_csharpRegistry = XWBPCSharpMemberRegistry.Instance;
		_csharpRegistry.ConfigureProjectRoot(Editor?.ActiveModProjectRoot);
		if (_memberMethodIcon == null)
		{
			_memberMethodIcon = LoadIconSafe("res://addons/ModEditor/Icons/MemberMethod.svg");
		}
		if (_memberSignalIcon == null)
		{
			_memberSignalIcon = LoadIconSafe("res://addons/ModEditor/Icons/MemberSignal.svg");
		}
		if (_variantIcon == null)
		{
			_variantIcon = LoadIconSafe("res://addons/ModEditor/Icons/TypeIcon/Variant.svg");
		}
		base._Ready();
		_nodeCardGrid = GetNode<HFlowContainer>("%NodeCardGrid");
		_categoryChips = GetNode<HBoxContainer>("%CategoryChips");
		_workspaceTabs = GetNode<TabContainer>("%WorkspaceTabs");
		_catalogCount = GetNode<Label>("%CatalogCount");
		_catalogEmpty = GetNode<Label>("%CatalogEmpty");
		_pageLabel = GetNode<Label>("%PageLabel");
		_previousPageButton = GetNode<Button>("%PreviousPageButton");
		_nextPageButton = GetNode<Button>("%NextPageButton");
		_categoryButtonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		_cardButtonGroup = new ButtonGroup
		{
			AllowUnpress = true
		};
		_workspaceTabs.SetTabTitle(0, "视觉节点");
		_workspaceTabs.SetTabIcon(0, LoadIconSafe("res://addons/ModEditor/Icons/GraphEdit.svg"));
		_workspaceTabs.SetTabTitle(1, "树状目录");
		_workspaceTabs.SetTabIcon(1, LoadIconSafe("res://addons/ModEditor/Icons/ExpandTree.svg"));
		_workspaceTabs.SetTabTitle(2, "收藏 / 最近");
		_workspaceTabs.SetTabIcon(2, LoadIconSafe("res://addons/ModEditor/Icons/Favorites.svg"));
		if (_workspaceTabs.GetTabControl(2) is TabContainer tabContainer)
		{
			tabContainer.SetTabTitle(0, "收藏");
			tabContainer.SetTabIcon(0, LoadIconSafe("res://addons/ModEditor/Icons/Favorites.svg"));
			tabContainer.SetTabTitle(1, "最近使用");
			tabContainer.SetTabIcon(1, LoadIconSafe("res://addons/ModEditor/Icons/History.svg"));
		}
		_previousPageButton.Pressed += delegate
		{
			ChangeVisualPage(-1);
		};
		_nextPageButton.Pressed += delegate
		{
			ChangeVisualPage(1);
		};
		GetOkButton().Disabled = true;
		SetProcess(enable: false);
		SetPhysicsProcess(enable: false);
		RebuildCategoryChips();
		ApplyNodeCatalogFilter("");
		RebuildVisualCards();
		RestyleCollectionButtons(_favoritesContainer);
		RestyleCollectionButtons(_recentContainer);
		CaptureProjectContext();
		_searchLineEdit.GrabFocus();
		BeginProjectIndexWarmup();
	}

	public void SetPortFilter(XWBPNodePortData portData, bool sourceIsOutput)
	{
		FilterPortData = portData;
		FilterSourceIsOutput = sourceIsOutput;
	}

	private void RebuildCategoryChips()
	{
		if (!GodotObject.IsInstanceValid(_categoryChips) || !GodotObject.IsInstanceValid(Root))
		{
			return;
		}
		ClearChildrenNow(_categoryChips);
		AddCategoryChip("", "全部", LoadIconSafe("res://addons/ModEditor/Icons/GraphEdit.svg"));
		TreeItem treeItem = Root.GetFirstChild();
		while (GodotObject.IsInstanceValid(treeItem))
		{
			string text = treeItem.GetText(0);
			if (!string.IsNullOrWhiteSpace(text))
			{
				AddCategoryChip(text, text, ResolveCategoryIcon(text));
			}
			treeItem = treeItem.GetNext();
		}
	}

	private void AddCategoryChip(string category, string label, Texture2D icon)
	{
		Button button = new Button
		{
			Text = label,
			Icon = icon,
			ToggleMode = true,
			ButtonGroup = _categoryButtonGroup,
			ButtonPressed = string.Equals(_activeVisualCategory, category, StringComparison.Ordinal),
			CustomMinimumSize = new Vector2(96f, 34f),
			TooltipText = (string.IsNullOrEmpty(category) ? "显示全部蓝图节点" : ("只显示 " + category + " 节点"))
		};
		button.AddThemeConstantOverride("icon_max_width", 22);
		button.Pressed += delegate
		{
			_activeVisualCategory = category;
			_visualPage = 0;
			if (string.Equals(category, "当前 Mod C# API", StringComparison.Ordinal))
			{
				EnsureProjectApiCategoryPopulated();
			}
			RebuildVisualCards();
		};
		_categoryChips.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void RebuildVisualCards()
	{
		if (!GodotObject.IsInstanceValid(_nodeCardGrid) || !GodotObject.IsInstanceValid(Root))
		{
			return;
		}
		ClearChildrenNow(_nodeCardGrid);
		_visibleCardItems.Clear();
		_visualChoices.Clear();
		string lowerQuery = (_visualQuery ?? "").Trim().ToLowerInvariant();
		TreeItem treeItem = Root.GetFirstChild();
		while (GodotObject.IsInstanceValid(treeItem))
		{
			string text = treeItem.GetText(0);
			if (string.IsNullOrEmpty(_activeVisualCategory) || string.Equals(_activeVisualCategory, text, StringComparison.Ordinal))
			{
				CollectVisualChoices(treeItem.GetFirstChild(), text, lowerQuery);
			}
			treeItem = treeItem.GetNext();
		}
		int num = Math.Max(1, (_visualChoices.Count + 36 - 1) / 36);
		_visualPage = Math.Clamp(_visualPage, 0, num - 1);
		int num2 = _visualPage * 36;
		int num3 = Math.Min(_visualChoices.Count, num2 + 36);
		for (int i = num2; i < num3; i++)
		{
			AddVisualCard(_visualChoices[i]);
		}
		_catalogCount.Text = $"{_visualChoices.Count} 个可用节点";
		_catalogEmpty.Visible = _visualChoices.Count == 0;
		_pageLabel.Text = $"{_visualPage + 1} / {num}";
		_previousPageButton.Disabled = _visualPage <= 0;
		_nextPageButton.Disabled = _visualPage >= num - 1;
	}

	private void CollectVisualChoices(TreeItem item, string category, string lowerQuery)
	{
		TreeItem treeItem = item;
		while (GodotObject.IsInstanceValid(treeItem))
		{
			bool num = _deferredNodeChoices.ContainsKey(treeItem);
			if (num)
			{
				bool num2 = string.IsNullOrEmpty(lowerQuery) || treeItem.GetText(0).ToLowerInvariant().Contains(lowerQuery) || treeItem.GetTooltipText(0).ToLowerInvariant().Contains(lowerQuery);
				DeferredNodeChoice deferredNodeChoice = _deferredNodeChoices[treeItem];
				if (num2 && IsDeferredNodeCompatible(deferredNodeChoice))
				{
					_visualChoices.Add(new VisualNodeChoice
					{
						Item = treeItem,
						NodeType = deferredNodeChoice.NodeType,
						Category = category
					});
				}
			}
			Variant metadata = treeItem.GetMetadata(0);
			if (!num && metadata.VariantType == Variant.Type.Object)
			{
				XWBPNodeType xWBPNodeType = metadata.As<XWBPNodeType>();
				if (xWBPNodeType != null && CanFilterTreeItem(lowerQuery, treeItem))
				{
					_visualChoices.Add(new VisualNodeChoice
					{
						Item = treeItem,
						NodeType = xWBPNodeType,
						Category = category
					});
				}
			}
			TreeItem firstChild = treeItem.GetFirstChild();
			if (GodotObject.IsInstanceValid(firstChild))
			{
				CollectVisualChoices(firstChild, category, lowerQuery);
			}
			treeItem = treeItem.GetNext();
		}
	}

	private void AddVisualCard(VisualNodeChoice choice)
	{
		XWBPNodeType nodeType = choice.NodeType ?? MaterializeDeferredNode(choice.Item);
		choice.NodeType = nodeType;
		if (nodeType == null)
		{
			return;
		}
		bool flag = FavoriteTypes.Contains(nodeType.TypeId);
		string value = $"{nodeType.InputPorts.Count} 入 · {nodeType.OutputPorts.Count} 出";
		string value2 = choice.Item.GetText(0);
		if (string.IsNullOrWhiteSpace(value2))
		{
			value2 = nodeType.DisplayName;
		}
		Button button = new Button
		{
			Name = "NodeCard_" + SanitizeNodeName(nodeType.TypeId.ToString()),
			Text = $"{(flag ? "★" : "☆")} {value2}\n{choice.Category} · {value}",
			TooltipText = nodeType.Description + "\n\n左键选择 · 双击添加 · 右键收藏",
			Icon = GetChoiceIcon(choice),
			Alignment = HorizontalAlignment.Left,
			IconAlignment = HorizontalAlignment.Left,
			ToggleMode = true,
			ButtonGroup = _cardButtonGroup,
			CustomMinimumSize = new Vector2(184f, 76f),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsStretchRatio = 1f
		};
		button.AddThemeConstantOverride("icon_max_width", 34);
		button.AddThemeFontSizeOverride("font_size", 14);
		AddCardStyles(button, nodeType.Color);
		button.Pressed += delegate
		{
			SelectVisualChoice(choice);
		};
		button.GuiInput += delegate(InputEvent input)
		{
			if (input is InputEventMouseButton { Pressed: not false } inputEventMouseButton)
			{
				if (inputEventMouseButton.ButtonIndex == MouseButton.Right)
				{
					ToggleFavorite(nodeType.TypeId);
					RestyleCollectionButtons(_favoritesContainer);
					RebuildVisualCards();
					GetViewport().SetInputAsHandled();
				}
				else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.DoubleClick)
				{
					SelectVisualChoice(choice);
					OnConfirmed();
					GetViewport().SetInputAsHandled();
				}
			}
		};
		_nodeCardGrid.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		_visibleCardItems[button] = choice.Item;
	}

	private void SelectVisualChoice(VisualNodeChoice choice)
	{
		if (GodotObject.IsInstanceValid(choice?.Item))
		{
			if (choice.NodeType == null)
			{
				XWBPNodeType xWBPNodeType = (choice.NodeType = MaterializeDeferredNode(choice.Item));
			}
			if (choice.NodeType != null)
			{
				choice.Item.Select(0);
				_tree.EnsureCursorIsVisible();
				OnItemSelected();
			}
		}
	}

	private void ChangeVisualPage(int delta)
	{
		_visualPage += delta;
		RebuildVisualCards();
	}

	private Texture2D GetChoiceIcon(VisualNodeChoice choice)
	{
		Texture2D icon = choice.Item.GetIcon(0);
		if (!GodotObject.IsInstanceValid(icon))
		{
			return ResolveCategoryIcon(choice.Category);
		}
		return icon;
	}

	private Texture2D ResolveCategoryIcon(string category)
	{
		string text = category ?? "";
		if (_categoryIcons.TryGetValue(text, out var value))
		{
			return value;
		}
		string text2 = text;
		string path = (text2.Contains("事件", StringComparison.OrdinalIgnoreCase) ? "res://addons/ModEditor/Icons/MemberSignal.svg" : (text2.Contains("信号", StringComparison.OrdinalIgnoreCase) ? "res://addons/ModEditor/Icons/Signals.svg" : (text2.Contains("变量", StringComparison.OrdinalIgnoreCase) ? "res://addons/ModEditor/Icons/MemberProperty.svg" : (text2.Contains("属性", StringComparison.OrdinalIgnoreCase) ? "res://addons/ModEditor/Icons/MemberProperty.svg" : (text2.Contains("函数", StringComparison.OrdinalIgnoreCase) ? "res://addons/ModEditor/Icons/MemberMethod.svg" : (text2.Contains("方法", StringComparison.OrdinalIgnoreCase) ? "res://addons/ModEditor/Icons/MemberMethod.svg" : (text2.Contains("流程", StringComparison.OrdinalIgnoreCase) ? "res://addons/ModEditor/Icons/FlowPort.svg" : ((!text2.Contains("资源", StringComparison.OrdinalIgnoreCase)) ? "res://addons/ModEditor/Icons/Node.svg" : "res://addons/ModEditor/Icons/ResourceGUI.svg"))))))));
		Texture2D texture2D = LoadIconSafe(path) ?? _variantIcon;
		_categoryIcons[text] = texture2D;
		return texture2D;
	}

	private static void AddCardStyles(Button card, Color accent)
	{
		if (accent.A < 0.1f)
		{
			accent = new Color("73c64b");
		}
		card.AddThemeStyleboxOverride("normal", CreateCardStyle(accent, 0.12f, 1));
		card.AddThemeStyleboxOverride("hover", CreateCardStyle(accent, 0.22f, 2));
		card.AddThemeStyleboxOverride("pressed", CreateCardStyle(accent, 0.3f, 3));
		card.AddThemeStyleboxOverride("focus", CreateCardStyle(accent, 0.26f, 2));
	}

	private static StyleBoxFlat CreateCardStyle(Color accent, float mix, int borderWidth)
	{
		Color color = new Color(0.075f, 0.09f, 0.072f);
		return new StyleBoxFlat
		{
			BgColor = color.Lerp(new Color(accent.R, accent.G, accent.B), mix),
			BorderColor = accent.Lightened(0.18f),
			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomRight = 8,
			CornerRadiusBottomLeft = 8,
			ContentMarginLeft = 10f,
			ContentMarginRight = 10f,
			ContentMarginTop = 6f,
			ContentMarginBottom = 6f
		};
	}

	private void RestyleCollectionButtons(VBoxContainer container)
	{
		if (!GodotObject.IsInstanceValid(container))
		{
			return;
		}
		foreach (Node child in container.GetChildren())
		{
			if (!(child is Button button))
			{
				continue;
			}
			string typeId = (button.HasMeta("xw_bp_node_type_id") ? button.GetMeta("xw_bp_node_type_id").AsString() : button.Text);
			XWBPNodeType xWBPNodeType = FindNodeTypeById(Root?.GetFirstChild(), typeId);
			if (xWBPNodeType != null)
			{
				button.SetMeta("xw_bp_node_type_id", typeId);
				button.Text = xWBPNodeType.DisplayName;
				button.TooltipText = xWBPNodeType.Description;
				button.Icon = ResolveCategoryIcon(xWBPNodeType.Category);
				AddCardStyles(button, xWBPNodeType.Color);
				if (!button.HasMeta("xw_bp_visual_collection_bound"))
				{
					button.SetMeta("xw_bp_visual_collection_bound", true);
					button.Pressed += delegate
					{
						SelectVisualTypeById(typeId);
						RestyleCollectionButtons(container);
					};
				}
			}
			button.CustomMinimumSize = new Vector2(0f, 40f);
			Button button2 = button;
			if (button2.Icon == null)
			{
				Texture2D texture2D = (button2.Icon = LoadIconSafe("res://addons/ModEditor/Icons/Node.svg"));
			}
			button.AddThemeConstantOverride("icon_max_width", 24);
		}
	}

	private void SelectVisualTypeById(string typeId)
	{
		TreeItem treeItem = FindNodeItemById(Root?.GetFirstChild(), typeId);
		if (GodotObject.IsInstanceValid(treeItem))
		{
			treeItem.Select(0);
			_tree.EnsureCursorIsVisible();
			OnItemSelected();
		}
	}

	private static TreeItem FindNodeItemById(TreeItem item, string typeId)
	{
		TreeItem treeItem = item;
		while (GodotObject.IsInstanceValid(treeItem))
		{
			Variant metadata = treeItem.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object)
			{
				XWBPNodeType xWBPNodeType = metadata.As<XWBPNodeType>();
				if (xWBPNodeType != null && string.Equals(xWBPNodeType.TypeId.ToString(), typeId, StringComparison.Ordinal))
				{
					return treeItem;
				}
			}
			TreeItem treeItem2 = FindNodeItemById(treeItem.GetFirstChild(), typeId);
			if (GodotObject.IsInstanceValid(treeItem2))
			{
				return treeItem2;
			}
			treeItem = treeItem.GetNext();
		}
		return null;
	}

	private static XWBPNodeType FindNodeTypeById(TreeItem item, string typeId)
	{
		TreeItem treeItem = item;
		while (GodotObject.IsInstanceValid(treeItem))
		{
			Variant metadata = treeItem.GetMetadata(0);
			if (metadata.VariantType == Variant.Type.Object)
			{
				XWBPNodeType xWBPNodeType = metadata.As<XWBPNodeType>();
				if (xWBPNodeType != null && string.Equals(xWBPNodeType.TypeId.ToString(), typeId, StringComparison.Ordinal))
				{
					return xWBPNodeType;
				}
			}
			XWBPNodeType xWBPNodeType2 = FindNodeTypeById(treeItem.GetFirstChild(), typeId);
			if (xWBPNodeType2 != null)
			{
				return xWBPNodeType2;
			}
			treeItem = treeItem.GetNext();
		}
		return null;
	}

	private static void ClearChildrenNow(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			parent.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void RegisterDeferredNode(TreeItem item, Func<XWBPNodeType> factory, DeferredPortCatalog ports)
	{
		if (GodotObject.IsInstanceValid(item) && factory != null && ports != null)
		{
			DeferredNodeChoice deferredNodeChoice = new DeferredNodeChoice
			{
				Factory = factory,
				Ports = ports
			};
			_deferredNodeChoices[item] = deferredNodeChoice;
			item.SetSelectable(0, IsDeferredNodeCompatible(deferredNodeChoice));
		}
	}

	private XWBPNodeType MaterializeDeferredNode(TreeItem item)
	{
		if (!GodotObject.IsInstanceValid(item) || !_deferredNodeChoices.TryGetValue(item, out var value))
		{
			Variant variant = (GodotObject.IsInstanceValid(item) ? item.GetMetadata(0) : default(Variant));
			if (variant.VariantType != Variant.Type.Object)
			{
				return null;
			}
			return variant.As<XWBPNodeType>();
		}
		if (value.NodeType != null)
		{
			return value.NodeType;
		}
		value.NodeType = value.Factory?.Invoke();
		if (value.NodeType != null)
		{
			item.SetMetadata(0, value.NodeType);
		}
		return value.NodeType;
	}

	private bool IsDeferredNodeCompatible(DeferredNodeChoice deferred)
	{
		if (deferred == null || !GodotObject.IsInstanceValid(FilterPortData))
		{
			return deferred != null;
		}
		foreach (DeferredPortShape item in (IEnumerable<DeferredPortShape>)(FilterSourceIsOutput ? deferred.Ports.Inputs : deferred.Ports.Outputs))
		{
			if (FilterSourceIsOutput ? CanPortShapesConnect(FilterPortData.PortTypeValue, FilterPortData.ClassName.ToString(), item.PortType, item.ClassName) : CanPortShapesConnect(item.PortType, item.ClassName, FilterPortData.PortTypeValue, FilterPortData.ClassName.ToString()))
			{
				return true;
			}
		}
		return false;
	}

	private static bool CanPortShapesConnect(XWBPNodePortData.PortType sourceType, string sourceClass, XWBPNodePortData.PortType targetType, string targetClass)
	{
		if (sourceType == XWBPNodePortData.PortType.Any || targetType == XWBPNodePortData.PortType.Any)
		{
			return true;
		}
		if (sourceType == targetType)
		{
			if (!XWBPNodePortData.IsObjectPortType(sourceType))
			{
				return true;
			}
			if (string.Equals(sourceClass, targetClass, StringComparison.Ordinal))
			{
				return true;
			}
			return XWClassRegistry.Instance.IsParentClass(sourceClass ?? "", targetClass ?? "");
		}
		if (XWBPNodePortData.IsObjectPortType(sourceType) && XWBPNodePortData.IsObjectPortType(targetType))
		{
			if (string.Equals(sourceClass, targetClass, StringComparison.Ordinal))
			{
				return true;
			}
			return XWClassRegistry.Instance.IsClassInstanceOf(sourceClass ?? "", targetClass ?? "");
		}
		if ((sourceType != XWBPNodePortData.PortType.Integer || targetType != XWBPNodePortData.PortType.Float) && (sourceType != XWBPNodePortData.PortType.Float || targetType != XWBPNodePortData.PortType.Integer) && (sourceType != XWBPNodePortData.PortType.Integer || targetType != XWBPNodePortData.PortType.Bool))
		{
			if (sourceType == XWBPNodePortData.PortType.Bool)
			{
				return targetType == XWBPNodePortData.PortType.Integer;
			}
			return false;
		}
		return true;
	}

	private static DeferredPortCatalog DescribeCallMethod(Dictionary methodData, XWBPNodeCallMethod.Type methodType)
	{
		DeferredPortCatalog deferredPortCatalog = new DeferredPortCatalog();
		Variant value;
		Godot.Collections.Array arguments = (methodData.TryGetValue("args", out value) ? value.As<Godot.Collections.Array>() : new Godot.Collections.Array());
		switch (methodType)
		{
		case XWBPNodeCallMethod.Type.SignalEvent:
			deferredPortCatalog.Outputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
			AddArgumentPortShapes(arguments, deferredPortCatalog.Outputs);
			return deferredPortCatalog;
		case XWBPNodeCallMethod.Type.SignalEmit:
			deferredPortCatalog.Inputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
			AddArgumentPortShapes(arguments, deferredPortCatalog.Inputs);
			deferredPortCatalog.Outputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
			return deferredPortCatalog;
		default:
		{
			deferredPortCatalog.Inputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
			deferredPortCatalog.Outputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
			Variant value2;
			bool flag = methodData.TryGetValue("is_static", out value2) && value2.AsBool();
			Variant value3;
			bool flag2 = methodData.TryGetValue("xw_self_member", out value3) && value3.AsBool();
			if (methodType != XWBPNodeCallMethod.Type.Super && !flag && !flag2)
			{
				Variant value4;
				string className = (methodData.TryGetValue("base_class_name", out value4) ? value4.AsString() : "");
				deferredPortCatalog.Inputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Object, className));
			}
			AddArgumentPortShapes(arguments, deferredPortCatalog.Inputs);
			if (methodData.TryGetValue("return", out var value5))
			{
				Dictionary dictionary = value5.As<Dictionary>();
				if (ReadInt(dictionary, "type") != 0)
				{
					deferredPortCatalog.Outputs.Add(CreatePortShape(dictionary));
				}
			}
			return deferredPortCatalog;
		}
		}
	}

	private static DeferredPortCatalog DescribeVirtualMethodEntry(Dictionary methodData)
	{
		DeferredPortCatalog deferredPortCatalog = new DeferredPortCatalog();
		deferredPortCatalog.Outputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
		AddArgumentPortShapes(methodData.TryGetValue("args", out var value) ? value.As<Godot.Collections.Array>() : new Godot.Collections.Array(), deferredPortCatalog.Outputs);
		return deferredPortCatalog;
	}

	private static DeferredPortCatalog DescribeProperty(Dictionary propertyData, bool setter)
	{
		DeferredPortCatalog deferredPortCatalog = new DeferredPortCatalog();
		Variant value;
		bool num = propertyData.TryGetValue("is_static", out value) && value.AsBool();
		Variant value2;
		bool flag = propertyData.TryGetValue("xw_self_member", out value2) && value2.AsBool();
		Variant value3;
		string className = (propertyData.TryGetValue("base_class_name", out value3) ? value3.AsString() : "");
		DeferredPortShape item = CreatePortShape(propertyData);
		if (setter)
		{
			deferredPortCatalog.Inputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
			deferredPortCatalog.Outputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Flow));
		}
		if (!num && !flag)
		{
			deferredPortCatalog.Inputs.Add(new DeferredPortShape(XWBPNodePortData.PortType.Object, className));
		}
		if (setter)
		{
			deferredPortCatalog.Inputs.Add(item);
		}
		deferredPortCatalog.Outputs.Add(item);
		return deferredPortCatalog;
	}

	private static void AddArgumentPortShapes(Godot.Collections.Array arguments, List<DeferredPortShape> ports)
	{
		foreach (Variant argument in arguments)
		{
			ports.Add(CreatePortShape(argument.As<Dictionary>()));
		}
	}

	private static DeferredPortShape CreatePortShape(Dictionary typeData)
	{
		int num = ReadInt(typeData, "type");
		return new DeferredPortShape(className: (num == 24) ? ReadString(typeData, "class_name") : "", portType: XWBPNodeCallMethod.ToPortType(num));
	}

	private static string SanitizeNodeName(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "Unknown";
		}
		char[] array = value.ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (!char.IsLetterOrDigit(array[i]) && array[i] != '_')
			{
				array[i] = '_';
			}
		}
		return new string(array);
	}

	protected override void OnCloseRequested()
	{
		QueueFree();
	}

	protected override void OnConfirmed()
	{
		if (!IsProjectContextCurrent())
		{
			XWEditorInterface.Instance?.ShowToast("Mod 或 C# API 已变化，旧蓝图拼图选择已取消，请重新打开节点目录。", 2);
			QueueFree();
			return;
		}
		TreeItem selected = _tree.GetSelected();
		if (!GodotObject.IsInstanceValid(selected))
		{
			return;
		}
		if (!IsNodeItemPortCompatible(selected))
		{
			GetOkButton().Disabled = true;
			return;
		}
		MaterializeDeferredNode(selected);
		Variant metadata = selected.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object)
		{
			XWBPNodeType xWBPNodeType = metadata.As<XWBPNodeType>();
			if (xWBPNodeType != null)
			{
				EmitSignal(SignalName.NodeTypeSelected, xWBPNodeType);
				AddToRecent(xWBPNodeType.TypeId);
				QueueFree();
			}
		}
	}

	protected override void BuildTree()
	{
		_tree.Clear();
		Root = _tree.CreateItem();
		AllCategories.Clear();
		_deferredNodeChoices.Clear();
		_projectApiCategory = null;
		_projectApiPopulated = false;
		if (Editor?.BpScriptData == null)
		{
			BuildProjectApiCategoryPlaceholder();
			BuildContextClassCategories();
			BuildBuiltinCategories();
			return;
		}
		BuildFunctionCategory();
		BuildVariableCategory();
		BuildSignalCategory();
		BuildParentSignalCategory();
		BuildMethodCategory();
		BuildPropertyCategory();
		BuildProjectApiCategoryPlaceholder();
		BuildContextClassCategories();
		BuildBuiltinCategories();
	}

	private void BuildProjectApiCategoryPlaceholder()
	{
		if (!string.IsNullOrWhiteSpace(_csharpRegistry.ActiveProjectRoot))
		{
			_projectApiCategory = _tree.CreateItem(Root);
			_projectApiCategory.Collapsed = false;
			_projectApiCategory.SetText(0, "当前 Mod C# API");
			_projectApiCategory.SetSelectable(0, selectable: false);
			_projectApiCategory.SetIcon(0, XWClassRegistry.Instance.GetUIIcon("Script"));
			TreeItem treeItem = _tree.CreateItem(_projectApiCategory);
			treeItem.SetText(0, "选择该分类或搜索后加载 API 拼图");
			treeItem.SetTooltipText(0, "C# 文件只在需要时索引，避免每次打开节点目录都创建全部拼图。");
			treeItem.SetSelectable(0, selectable: false);
		}
	}

	private void EnsureProjectApiCategoryPopulated()
	{
		if (_projectApiPopulated || !GodotObject.IsInstanceValid(_projectApiCategory))
		{
			return;
		}
		if (!IsCSharpCatalogReady)
		{
			TreeItem firstChild = _projectApiCategory.GetFirstChild();
			if (GodotObject.IsInstanceValid(firstChild))
			{
				firstChild.SetText(0, "正在后台索引当前 Mod C# API…");
			}
			BeginProjectIndexWarmup();
		}
		else
		{
			BuildProjectApiCategory();
			_projectApiPopulated = true;
		}
	}

	private async void BeginProjectIndexWarmup()
	{
		if (_projectIndexRequested || _csharpRegistry == null || IsCSharpCatalogReady || string.IsNullOrWhiteSpace(_csharpRegistry.ActiveProjectRoot))
		{
			return;
		}
		string rootSnapshot = _csharpRegistry.ActiveProjectRoot;
		int versionSnapshot = _csharpRegistry.ProjectContextVersion;
		XWBPScriptData scriptSnapshot = Editor?.BpScriptData;
		XWBPGraphData graphSnapshot = CurrentGraph;
		_projectIndexRequested = true;
		bool ready = false;
		try
		{
			ready = await _csharpRegistry.EnsureProjectScannedAsync();
		}
		catch (Exception ex)
		{
			GD.PushWarning("当前 Mod C# API 后台索引失败：" + ex.Message);
		}
		finally
		{
			_projectIndexRequested = false;
		}
		if (ready && GodotObject.IsInstanceValid(this) && IsInsideTree() && string.Equals(rootSnapshot, _csharpRegistry.ActiveProjectRoot, StringComparison.OrdinalIgnoreCase) && versionSnapshot == _csharpRegistry.ProjectContextVersion && scriptSnapshot == Editor?.BpScriptData && graphSnapshot == CurrentGraph)
		{
			string activeVisualCategory = _activeVisualCategory;
			string text = _visualQuery ?? "";
			BuildTree();
			_activeVisualCategory = activeVisualCategory;
			if (!string.IsNullOrWhiteSpace(text) || string.Equals(activeVisualCategory, "当前 Mod C# API", StringComparison.Ordinal))
			{
				EnsureProjectApiCategoryPopulated();
			}
			RebuildCategoryChips();
			ApplyNodeCatalogFilter(text);
			RebuildVisualCards();
			RestyleCollectionButtons(_favoritesContainer);
			RestyleCollectionButtons(_recentContainer);
			GetOkButton().Disabled = true;
		}
	}

	private void BuildBuiltinCategories()
	{
		foreach (KeyValuePair<string, List<string>> category in _registry.GetCategories())
		{
			TreeItem treeItem = _tree.CreateItem(Root);
			treeItem.Collapsed = true;
			treeItem.SetText(0, category.Key);
			treeItem.SetSelectable(0, selectable: false);
			foreach (XWBPNodeType item in _registry.GetNodeTypesByCategory(category.Key))
			{
				TreeItem treeItem2 = _tree.CreateItem(treeItem);
				treeItem2.Collapsed = true;
				treeItem2.SetText(0, item.DisplayName);
				treeItem2.SetTooltipText(0, item.Description);
				treeItem2.SetMetadata(0, item);
				SetReturnTypeIcon(treeItem2, item.OutputPorts);
			}
		}
	}

	private void BuildProjectApiCategory()
	{
		IReadOnlyList<XWBPCSharpMemberRegistry.CSharpClassDescriptor> projectClassDescriptors = _csharpRegistry.GetProjectClassDescriptors();
		TreeItem treeItem = _projectApiCategory.GetFirstChild();
		while (GodotObject.IsInstanceValid(treeItem))
		{
			TreeItem next = treeItem.GetNext();
			treeItem.Free();
			treeItem = next;
		}
		if (projectClassDescriptors.Count == 0)
		{
			TreeItem treeItem2 = _tree.CreateItem(_projectApiCategory);
			treeItem2.SetText(0, "当前 Mod 尚无可用的 C# API");
			treeItem2.SetSelectable(0, selectable: false);
			return;
		}
		string a = Editor?.BpScriptData?.ExtendsClass.ToString() ?? "";
		foreach (XWBPCSharpMemberRegistry.CSharpClassDescriptor item in projectClassDescriptors)
		{
			if (string.Equals(a, item.Name, StringComparison.Ordinal) || string.Equals(a, item.QualifiedName, StringComparison.Ordinal))
			{
				continue;
			}
			List<Dictionary> classMethodList = _csharpRegistry.GetClassMethodList(item.QualifiedName, includeBase: false, includeLifecycle: false);
			List<Dictionary> classPropertyList = _csharpRegistry.GetClassPropertyList(item.QualifiedName, includeBase: false);
			List<Dictionary> classSignalList = _csharpRegistry.GetClassSignalList(item.QualifiedName, includeBase: false);
			int instanceMemberCount = 0;
			classMethodList.RemoveAll(delegate(Dictionary method)
			{
				bool num = IsStaticMemberData(method);
				if (!num)
				{
					instanceMemberCount++;
				}
				return !num;
			});
			classPropertyList.RemoveAll(delegate(Dictionary property)
			{
				bool num = IsStaticMemberData(property);
				if (!num)
				{
					instanceMemberCount++;
				}
				return !num;
			});
			instanceMemberCount += classSignalList.Count;
			if (classMethodList.Count != 0 || classPropertyList.Count != 0 || instanceMemberCount != 0)
			{
				TreeItem treeItem3 = _tree.CreateItem(_projectApiCategory);
				treeItem3.Collapsed = true;
				treeItem3.SetText(0, item.Name);
				treeItem3.SetTooltipText(0, item.QualifiedName + "\n" + item.ScriptPath + "\n" + (item.CanInherit ? "静态 API 可直接使用；实例 API 请继承该类，或从该类型对象端口拖出。" : item.InheritanceBlockReason) + "\n自定义 C# 会在编译运行时执行；受限安全预览不会执行。");
				treeItem3.SetSelectable(0, selectable: false);
				treeItem3.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(item.BaseClass));
				BuildProjectApiMethodItems(treeItem3, item, classMethodList);
				BuildProjectApiPropertyItems(treeItem3, item, classPropertyList);
				if (instanceMemberCount > 0)
				{
					TreeItem treeItem4 = _tree.CreateItem(treeItem3);
					treeItem4.SetText(0, $"{instanceMemberCount} 个实例 API · 从对象端口拖出");
					treeItem4.SetTooltipText(0, "为避免创建没有对象目标、运行时变成 null.Method() 的无效拼图，这里仅直接展示静态 API。实例 API 可设为蓝图父类，或从精确类型对象端口拖出。");
					treeItem4.SetSelectable(0, selectable: false);
					treeItem4.SetIcon(0, XWClassRegistry.Instance.GetUIIcon("Info"));
				}
			}
		}
	}

	private void BuildProjectApiMethodItems(TreeItem classItem, XWBPCSharpMemberRegistry.CSharpClassDescriptor descriptor, List<Dictionary> methods)
	{
		foreach (Dictionary methodData in methods)
		{
			string text = ReadString(methodData, "name");
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			string displayName = BuildMethodChoiceName(descriptor.Name, text, methodData);
			string description = $"调用当前 Mod 静态 API：{descriptor.QualifiedName}.{text}\n" + "编译运行可用；受限安全预览不会执行自定义 C#。";
			TreeItem treeItem = _tree.CreateItem(classItem);
			treeItem.Collapsed = true;
			treeItem.SetText(0, displayName);
			treeItem.SetTooltipText(0, description);
			treeItem.SetIcon(0, _memberMethodIcon);
			RegisterDeferredNode(treeItem, delegate
			{
				if (!(_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod))
				{
					return (XWBPNodeType)null;
				}
				xWBPNodeCallMethod.MethodData = methodData;
				xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.Script;
				xWBPNodeCallMethod.BuildMethod();
				xWBPNodeCallMethod.SetCatalogPresentation(displayName, description);
				return xWBPNodeCallMethod;
			}, DescribeCallMethod(methodData, XWBPNodeCallMethod.Type.Script));
		}
	}

	private void BuildProjectApiPropertyItems(TreeItem classItem, XWBPCSharpMemberRegistry.CSharpClassDescriptor descriptor, List<Dictionary> properties)
	{
		foreach (Dictionary propertyData in properties)
		{
			string text = ReadString(propertyData, "name");
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			int num = ReadInt(propertyData, "type");
			string className = ((num == 24) ? ReadString(propertyData, "class_name") : "");
			if (!propertyData.TryGetValue("has_get", out var value) || value.AsBool())
			{
				string displayName = "获取 " + descriptor.Name + "." + text;
				string description = $"读取当前 Mod 静态 API 属性：{descriptor.QualifiedName}.{text}\n" + "编译运行可用；受限安全预览不会执行自定义 C#。";
				TreeItem treeItem = _tree.CreateItem(classItem);
				treeItem.Collapsed = true;
				treeItem.SetText(0, displayName);
				treeItem.SetTooltipText(0, description);
				SetReturnTypeIconByVariantType(treeItem, num, className);
				RegisterDeferredNode(treeItem, delegate
				{
					if (!(_registry.CreateNodeType("__XWBPGraphNode_GetProperty") is XWBPNodeGetProperty xWBPNodeGetProperty))
					{
						return (XWBPNodeType)null;
					}
					xWBPNodeGetProperty.PropertyData = propertyData;
					xWBPNodeGetProperty.MethodType = XWBPNodeGetProperty.Type.Script;
					xWBPNodeGetProperty.BuildProperty();
					xWBPNodeGetProperty.SetCatalogPresentation(displayName, description);
					return xWBPNodeGetProperty;
				}, DescribeProperty(propertyData, setter: false));
			}
			if (propertyData.TryGetValue("has_set", out var value2) && !value2.AsBool())
			{
				continue;
			}
			string displayName2 = "设置 " + descriptor.Name + "." + text;
			string description2 = $"写入当前 Mod 静态 API 属性：{descriptor.QualifiedName}.{text}\n" + "编译运行可用；受限安全预览不会执行自定义 C#。";
			TreeItem treeItem2 = _tree.CreateItem(classItem);
			treeItem2.Collapsed = true;
			treeItem2.SetText(0, displayName2);
			treeItem2.SetTooltipText(0, description2);
			SetReturnTypeIconByVariantType(treeItem2, num, className);
			RegisterDeferredNode(treeItem2, delegate
			{
				if (!(_registry.CreateNodeType("__XWBPGraphNode_SetProperty") is XWBPNodeSetProperty xWBPNodeSetProperty))
				{
					return (XWBPNodeType)null;
				}
				xWBPNodeSetProperty.PropertyData = propertyData;
				xWBPNodeSetProperty.MethodType = XWBPNodeSetProperty.Type.Script;
				xWBPNodeSetProperty.BuildProperty();
				xWBPNodeSetProperty.SetCatalogPresentation(displayName2, description2);
				return xWBPNodeSetProperty;
			}, DescribeProperty(propertyData, setter: true));
		}
	}

	private void BuildProjectApiSignalItems(TreeItem classItem, XWBPCSharpMemberRegistry.CSharpClassDescriptor descriptor, List<Dictionary> signals, bool isBlueprintParent)
	{
		foreach (Dictionary signal in signals)
		{
			string text = ReadString(signal, "name");
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (!isBlueprintParent)
			{
				TreeItem treeItem = _tree.CreateItem(classItem);
				treeItem.SetText(0, "信号 " + text + " · 继承后可用");
				treeItem.SetTooltipText(0, "将蓝图父类设为 " + descriptor.QualifiedName + " 后，可创建事件与发射信号拼图。");
				treeItem.SetSelectable(0, selectable: false);
				treeItem.SetIcon(0, _memberSignalIcon);
				continue;
			}
			if (!(CurrentGraph is XWBPFunctionData) && _registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod)
			{
				xWBPNodeCallMethod.MethodData = signal;
				xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.SignalEvent;
				xWBPNodeCallMethod.BuildSignalEvent();
				xWBPNodeCallMethod.SetCatalogPresentation("事件 " + descriptor.Name + "." + text, "监听父类信号 " + descriptor.QualifiedName + "." + text);
				TreeItem treeItem2 = _tree.CreateItem(classItem);
				treeItem2.SetText(0, xWBPNodeCallMethod.DisplayName);
				treeItem2.SetTooltipText(0, xWBPNodeCallMethod.Description);
				treeItem2.SetMetadata(0, xWBPNodeCallMethod);
				treeItem2.SetIcon(0, _memberSignalIcon);
			}
			if (_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod2)
			{
				xWBPNodeCallMethod2.MethodData = signal;
				xWBPNodeCallMethod2.MethodType = XWBPNodeCallMethod.Type.SignalEmit;
				xWBPNodeCallMethod2.BuildSignalEmit();
				xWBPNodeCallMethod2.SetCatalogPresentation("发射 " + descriptor.Name + "." + text, "发射父类信号 " + descriptor.QualifiedName + "." + text);
				TreeItem treeItem3 = _tree.CreateItem(classItem);
				treeItem3.SetText(0, xWBPNodeCallMethod2.DisplayName);
				treeItem3.SetTooltipText(0, xWBPNodeCallMethod2.Description);
				treeItem3.SetMetadata(0, xWBPNodeCallMethod2);
				treeItem3.SetIcon(0, _memberSignalIcon);
			}
		}
	}

	private static string BuildMethodChoiceName(string className, string methodName, Dictionary methodData)
	{
		Variant value;
		Godot.Collections.Array array = (methodData.TryGetValue("args", out value) ? value.As<Godot.Collections.Array>() : new Godot.Collections.Array());
		if (array.Count == 0)
		{
			return className + "." + methodName + "()";
		}
		List<string> list = new List<string>();
		foreach (Variant item in array)
		{
			Dictionary data = item.As<Dictionary>();
			string text = ReadString(data, "name", "参数");
			string text2 = ReadString(data, "class_name");
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = ((Variant.Type)ReadInt(data, "type")/*cast due to .constrained prefix*/).ToString();
			}
			list.Add(text + ": " + text2);
		}
		return $"{className}.{methodName}({string.Join(", ", list)})";
	}

	private void BuildFunctionCategory()
	{
		System.Collections.Generic.Dictionary<int, XWBPFunctionData> functions = Editor.BpScriptData.Functions;
		if (functions.Count == 0)
		{
			return;
		}
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		treeItem.SetText(0, "调用函数");
		treeItem.SetSelectable(0, selectable: false);
		foreach (KeyValuePair<int, XWBPFunctionData> item in functions)
		{
			XWBPFunctionData value = item.Value;
			if (_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod)
			{
				xWBPNodeCallMethod.FunctionId = value.Id;
				xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.Bp;
				xWBPNodeCallMethod.BuildFunction(value);
				TreeItem treeItem2 = _tree.CreateItem(treeItem);
				treeItem2.Collapsed = true;
				treeItem2.SetText(0, value.Name);
				treeItem2.SetTooltipText(0, xWBPNodeCallMethod.Description);
				treeItem2.SetMetadata(0, xWBPNodeCallMethod);
				SetReturnTypeIcon(treeItem2, value.Outputs);
			}
		}
	}

	private void BuildVariableCategory()
	{
		System.Collections.Generic.Dictionary<int, XWBPVariableData> variables = Editor.BpScriptData.Variables;
		if (variables.Count == 0)
		{
			return;
		}
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		treeItem.SetText(0, "控制变量");
		treeItem.SetSelectable(0, selectable: false);
		foreach (KeyValuePair<int, XWBPVariableData> item in variables)
		{
			XWBPVariableData value = item.Value;
			Variant.Type type = value.Type;
			if (_registry.CreateNodeType("__XWBPGraphNode_GetProperty") is XWBPNodeGetProperty xWBPNodeGetProperty)
			{
				xWBPNodeGetProperty.VariableId = value.Id;
				xWBPNodeGetProperty.MethodType = XWBPNodeGetProperty.Type.Bp;
				xWBPNodeGetProperty.BuildVariable(value);
				TreeItem treeItem2 = _tree.CreateItem(treeItem);
				treeItem2.Collapsed = true;
				treeItem2.SetText(0, "获取 " + value.Name);
				treeItem2.SetTooltipText(0, xWBPNodeGetProperty.Description);
				treeItem2.SetMetadata(0, xWBPNodeGetProperty);
				SetVariableIcon(treeItem2, type, value.ClassName);
			}
			if (_registry.CreateNodeType("__XWBPGraphNode_SetProperty") is XWBPNodeSetProperty xWBPNodeSetProperty)
			{
				xWBPNodeSetProperty.VariableId = value.Id;
				xWBPNodeSetProperty.MethodType = XWBPNodeSetProperty.Type.Bp;
				xWBPNodeSetProperty.BuildVariable(value);
				TreeItem treeItem3 = _tree.CreateItem(treeItem);
				treeItem3.Collapsed = true;
				treeItem3.SetText(0, "设置 " + value.Name);
				treeItem3.SetTooltipText(0, xWBPNodeSetProperty.Description);
				treeItem3.SetMetadata(0, xWBPNodeSetProperty);
				SetVariableIcon(treeItem3, type, value.ClassName);
			}
		}
	}

	private void BuildSignalCategory()
	{
		System.Collections.Generic.Dictionary<int, XWBPSignalData> signalDatas = Editor.BpScriptData.SignalDatas;
		if (signalDatas.Count == 0)
		{
			return;
		}
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		treeItem.SetText(0, "信号");
		treeItem.SetSelectable(0, selectable: false);
		foreach (KeyValuePair<int, XWBPSignalData> item in signalDatas)
		{
			XWBPSignalData value = item.Value;
			if (!(CurrentGraph is XWBPFunctionData) && _registry.CreateNodeType("__XWBPGraphNode_SignalEvent") is XWBPNodeSignalEvent xWBPNodeSignalEvent)
			{
				xWBPNodeSignalEvent.SignalId = value.Id;
				TreeItem treeItem2 = _tree.CreateItem(treeItem);
				treeItem2.Collapsed = true;
				treeItem2.SetText(0, "事件 " + value.Name);
				treeItem2.SetTooltipText(0, "当 " + value.Name + " 信号触发时执行");
				treeItem2.SetMetadata(0, xWBPNodeSignalEvent);
				treeItem2.SetIcon(0, _memberSignalIcon);
			}
			if (_registry.CreateNodeType("__XWBPGraphNode_EmitSignal") is XWBPNodeEmitSignal xWBPNodeEmitSignal)
			{
				xWBPNodeEmitSignal.SignalId = value.Id;
				TreeItem treeItem3 = _tree.CreateItem(treeItem);
				treeItem3.Collapsed = true;
				treeItem3.SetText(0, "发射 " + value.Name);
				treeItem3.SetTooltipText(0, "发射 " + value.Name + " 信号");
				treeItem3.SetMetadata(0, xWBPNodeEmitSignal);
				treeItem3.SetIcon(0, _memberSignalIcon);
			}
		}
	}

	private void BuildParentSignalCategory()
	{
		string text = Editor.BpScriptData.ExtendsClass?.ToString();
		if (string.IsNullOrEmpty(text) || IsProjectClassIndexPending(text))
		{
			return;
		}
		List<Dictionary> mergedSignalList = GetMergedSignalList(text);
		if (mergedSignalList.Count == 0)
		{
			return;
		}
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		treeItem.SetText(0, "父类信号");
		treeItem.SetSelectable(0, selectable: false);
		System.Collections.Generic.Dictionary<string, TreeItem> dictionary = new System.Collections.Generic.Dictionary<string, TreeItem>();
		foreach (Dictionary signalData in mergedSignalList)
		{
			Variant value;
			string text2 = (signalData.TryGetValue("base_class_name", out value) ? value.AsString() : "");
			Variant value2;
			string text3 = (signalData.TryGetValue("name", out value2) ? value2.AsString() : "");
			if (string.IsNullOrEmpty(text3))
			{
				continue;
			}
			if (!dictionary.TryGetValue(text2, out var value3))
			{
				value3 = _tree.CreateItem(treeItem);
				value3.Collapsed = true;
				value3.SetText(0, text2);
				value3.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text2));
				dictionary[text2] = value3;
			}
			if (!(CurrentGraph is XWBPFunctionData))
			{
				TreeItem treeItem2 = _tree.CreateItem(value3);
				treeItem2.Collapsed = true;
				treeItem2.SetText(0, "事件 " + text3);
				treeItem2.SetTooltipText(0, "当 " + text3 + " 信号触发时执行");
				treeItem2.SetIcon(0, _memberSignalIcon);
				RegisterDeferredNode(treeItem2, delegate
				{
					if (!(_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod))
					{
						return (XWBPNodeType)null;
					}
					xWBPNodeCallMethod.MethodData = signalData;
					xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.SignalEvent;
					xWBPNodeCallMethod.BuildSignalEvent();
					return xWBPNodeCallMethod;
				}, DescribeCallMethod(signalData, XWBPNodeCallMethod.Type.SignalEvent));
			}
			TreeItem treeItem3 = _tree.CreateItem(value3);
			treeItem3.Collapsed = true;
			treeItem3.SetText(0, "发射 " + text3);
			treeItem3.SetTooltipText(0, "发射 " + text3 + " 信号");
			treeItem3.SetIcon(0, _memberSignalIcon);
			RegisterDeferredNode(treeItem3, delegate
			{
				if (!(_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod))
				{
					return (XWBPNodeType)null;
				}
				xWBPNodeCallMethod.MethodData = signalData;
				xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.SignalEmit;
				xWBPNodeCallMethod.BuildSignalEmit();
				return xWBPNodeCallMethod;
			}, DescribeCallMethod(signalData, XWBPNodeCallMethod.Type.SignalEmit));
		}
	}

	private void BuildMethodCategory()
	{
		string text = Editor.BpScriptData.ExtendsClass?.ToString();
		if (string.IsNullOrEmpty(text) || IsProjectClassIndexPending(text))
		{
			return;
		}
		List<Dictionary> mergedMethodList = GetMergedMethodList(text);
		if (mergedMethodList.Count == 0)
		{
			return;
		}
		TreeItem treeItem = null;
		TreeItem treeItem2 = null;
		System.Collections.Generic.Dictionary<string, TreeItem> dictionary = new System.Collections.Generic.Dictionary<string, TreeItem>();
		System.Collections.Generic.Dictionary<string, TreeItem> dictionary2 = new System.Collections.Generic.Dictionary<string, TreeItem>();
		foreach (Dictionary methodData in mergedMethodList)
		{
			Variant value;
			string text2 = (methodData.TryGetValue("base_class_name", out value) ? value.AsString() : "");
			Variant value2;
			string text3 = (methodData.TryGetValue("name", out value2) ? value2.AsString() : "");
			if (string.IsNullOrEmpty(text3))
			{
				continue;
			}
			Variant value3;
			bool num = methodData.TryGetValue("xw_is_virtual", out value3) && value3.AsBool();
			Variant value4;
			Dictionary dictionary3 = (methodData.TryGetValue("return", out value4) ? value4.As<Dictionary>() : new Dictionary());
			Variant value5;
			int variantType = (dictionary3.TryGetValue("type", out value5) ? value5.AsInt32() : 0);
			if (num)
			{
				if (CurrentGraph is XWBPFunctionData || HasVirtualMethodEntry(text3))
				{
					continue;
				}
				if (treeItem2 == null)
				{
					treeItem2 = _tree.CreateItem(Root);
				}
				treeItem2.Collapsed = true;
				treeItem2.SetText(0, "重写方法");
				treeItem2.SetSelectable(0, selectable: false);
				if (!dictionary2.TryGetValue(text2, out var value6))
				{
					value6 = _tree.CreateItem(treeItem2);
					value6.Collapsed = true;
					value6.SetText(0, text2);
					value6.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text2));
					dictionary2[text2] = value6;
				}
				TreeItem treeItem3 = _tree.CreateItem(dictionary2[text2]);
				treeItem3.Collapsed = true;
				treeItem3.SetText(0, text3);
				treeItem3.SetTooltipText(0, "重写 " + text3 + " 方法");
				SetReturnTypeIconByVariantType(treeItem3, variantType, dictionary3.TryGetValue("class_name", out var value7) ? value7.AsString() : "");
				RegisterDeferredNode(treeItem3, delegate
				{
					if (!(_registry.CreateNodeType("__XWBPGraphNode_MethodEntry") is XWBPNodeMethodEntry xWBPNodeMethodEntry))
					{
						return (XWBPNodeType)null;
					}
					xWBPNodeMethodEntry.MethodData = methodData;
					xWBPNodeMethodEntry.MethodType = XWBPNodeMethodEntry.Type.Virtual;
					xWBPNodeMethodEntry.BuildVirtualMethod();
					return xWBPNodeMethodEntry;
				}, DescribeVirtualMethodEntry(methodData));
				if ((methodData.TryGetValue("xw_is_abstract", out var value8) && value8.AsBool()) || !DoesClassImplementMethod(text, text3))
				{
					continue;
				}
				TreeItem treeItem4 = _tree.CreateItem(dictionary2[text2]);
				treeItem4.Collapsed = true;
				treeItem4.SetText(0, "Super " + text3);
				treeItem4.SetTooltipText(0, "调用父类的 " + text3 + " 方法");
				treeItem4.SetIcon(0, _memberMethodIcon);
				RegisterDeferredNode(treeItem4, delegate
				{
					if (!(_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod))
					{
						return (XWBPNodeType)null;
					}
					xWBPNodeCallMethod.MethodData = methodData;
					xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.Super;
					xWBPNodeCallMethod.BuildMethod();
					return xWBPNodeCallMethod;
				}, DescribeCallMethod(methodData, XWBPNodeCallMethod.Type.Super));
				continue;
			}
			if (treeItem == null)
			{
				treeItem = _tree.CreateItem(Root);
			}
			treeItem.Collapsed = true;
			treeItem.SetText(0, "方法");
			treeItem.SetSelectable(0, selectable: false);
			if (!dictionary.TryGetValue(text2, out var value9))
			{
				value9 = _tree.CreateItem(treeItem);
				value9.Collapsed = true;
				value9.SetText(0, text2);
				value9.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text2));
				dictionary[text2] = value9;
			}
			methodData["xw_self_member"] = true;
			TreeItem treeItem5 = _tree.CreateItem(dictionary[text2]);
			treeItem5.Collapsed = true;
			treeItem5.SetText(0, text3);
			treeItem5.SetTooltipText(0, $"调用 {text2}.{text3} 方法");
			SetReturnTypeIconByVariantType(treeItem5, variantType, dictionary3.TryGetValue("class_name", out var value10) ? value10.AsString() : "");
			RegisterDeferredNode(treeItem5, delegate
			{
				if (!(_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod))
				{
					return (XWBPNodeType)null;
				}
				xWBPNodeCallMethod.MethodData = methodData;
				xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.Script;
				xWBPNodeCallMethod.BuildMethod();
				return xWBPNodeCallMethod;
			}, DescribeCallMethod(methodData, XWBPNodeCallMethod.Type.Script));
		}
	}

	private void BuildPropertyCategory()
	{
		string text = Editor.BpScriptData.ExtendsClass?.ToString();
		if (string.IsNullOrEmpty(text) || IsProjectClassIndexPending(text))
		{
			return;
		}
		List<Dictionary> mergedPropertyList = GetMergedPropertyList(text);
		if (mergedPropertyList.Count == 0)
		{
			return;
		}
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		treeItem.SetText(0, "属性");
		treeItem.SetSelectable(0, selectable: false);
		System.Collections.Generic.Dictionary<string, TreeItem> dictionary = new System.Collections.Generic.Dictionary<string, TreeItem>();
		foreach (Dictionary propertyData in mergedPropertyList)
		{
			Variant value;
			string text2 = (propertyData.TryGetValue("base_class_name", out value) ? value.AsString() : "");
			Variant value2;
			string text3 = (propertyData.TryGetValue("name", out value2) ? value2.AsString() : "");
			if (string.IsNullOrEmpty(text3) || text3.StartsWith("_") || ((propertyData.TryGetValue("usage", out var value3) ? value3.AsInt32() : 0) & 2) == 0)
			{
				continue;
			}
			if (!dictionary.TryGetValue(text2, out var value4))
			{
				value4 = _tree.CreateItem(treeItem);
				value4.Collapsed = true;
				value4.SetText(0, text2);
				value4.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text2));
				dictionary[text2] = value4;
			}
			Variant value5;
			int num = (propertyData.TryGetValue("type", out value5) ? value5.AsInt32() : 0);
			Variant value6;
			string className = ((num != 24) ? "" : (propertyData.TryGetValue("class_name", out value6) ? value6.AsString() : ""));
			Variant value7;
			bool num2 = !propertyData.TryGetValue("has_get", out value7) || value7.AsBool();
			propertyData["xw_self_member"] = true;
			if (num2)
			{
				TreeItem treeItem2 = _tree.CreateItem(value4);
				treeItem2.Collapsed = true;
				treeItem2.SetText(0, "获取 " + text3);
				treeItem2.SetTooltipText(0, "获取 " + text3 + " 属性");
				SetReturnTypeIconByVariantType(treeItem2, num, className);
				RegisterDeferredNode(treeItem2, delegate
				{
					if (!(_registry.CreateNodeType("__XWBPGraphNode_GetProperty") is XWBPNodeGetProperty xWBPNodeGetProperty))
					{
						return (XWBPNodeType)null;
					}
					xWBPNodeGetProperty.PropertyData = propertyData;
					xWBPNodeGetProperty.MethodType = XWBPNodeGetProperty.Type.Script;
					xWBPNodeGetProperty.BuildProperty();
					return xWBPNodeGetProperty;
				}, DescribeProperty(propertyData, setter: false));
			}
			if (propertyData.TryGetValue("has_set", out var value8) && !value8.AsBool())
			{
				continue;
			}
			TreeItem treeItem3 = _tree.CreateItem(value4);
			treeItem3.Collapsed = true;
			treeItem3.SetText(0, "设置 " + text3);
			treeItem3.SetTooltipText(0, "设置 " + text3 + " 属性");
			SetReturnTypeIconByVariantType(treeItem3, num, className);
			RegisterDeferredNode(treeItem3, delegate
			{
				if (!(_registry.CreateNodeType("__XWBPGraphNode_SetProperty") is XWBPNodeSetProperty xWBPNodeSetProperty))
				{
					return (XWBPNodeType)null;
				}
				xWBPNodeSetProperty.PropertyData = propertyData;
				xWBPNodeSetProperty.MethodType = XWBPNodeSetProperty.Type.Script;
				xWBPNodeSetProperty.BuildProperty();
				return xWBPNodeSetProperty;
			}, DescribeProperty(propertyData, setter: true));
		}
	}

	private void BuildContextClassCategories()
	{
		string filterObjectClassName = GetFilterObjectClassName();
		if (!string.IsNullOrEmpty(filterObjectClassName))
		{
			BuildContextMethodCategory(filterObjectClassName);
			BuildContextPropertyCategory(filterObjectClassName);
		}
	}

	private string GetFilterObjectClassName()
	{
		if (!FilterSourceIsOutput || !GodotObject.IsInstanceValid(FilterPortData))
		{
			return "";
		}
		if (!XWBPNodePortData.IsObjectPortType(FilterPortData.PortTypeValue))
		{
			return "";
		}
		string text = FilterPortData.ClassName.ToString();
		if (string.IsNullOrEmpty(text))
		{
			text = Editor?.BpScriptData?.ExtendsClass.ToString() ?? "";
		}
		if (IsProjectClassIndexPending(text))
		{
			BeginProjectIndexWarmup();
			return "";
		}
		if (string.IsNullOrEmpty(text) || (!XWClassRegistry.Instance.HasClass(text) && (!IsCSharpCatalogReady || !_csharpRegistry.HasClass(text))))
		{
			return "";
		}
		return text;
	}

	private bool IsProjectClassIndexPending(string className)
	{
		if (!string.IsNullOrWhiteSpace(className) && !XWClassRegistry.Instance.HasClass(className) && !string.IsNullOrWhiteSpace(_csharpRegistry.ActiveProjectRoot))
		{
			return !IsCSharpCatalogReady;
		}
		return false;
	}

	private void BuildContextMethodCategory(string contextClassName)
	{
		List<Dictionary> mergedMethodList = GetMergedMethodList(contextClassName);
		if (mergedMethodList.Count == 0)
		{
			return;
		}
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		treeItem.SetText(0, "调用类方法");
		treeItem.SetSelectable(0, selectable: false);
		treeItem.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(contextClassName));
		System.Collections.Generic.Dictionary<string, TreeItem> dictionary = new System.Collections.Generic.Dictionary<string, TreeItem>();
		foreach (Dictionary item in mergedMethodList)
		{
			Variant value;
			string text = (item.TryGetValue("base_class_name", out value) ? value.AsString() : "");
			Variant value2;
			string text2 = (item.TryGetValue("name", out value2) ? value2.AsString() : "");
			if (!string.IsNullOrEmpty(text2) && !text2.StartsWith("_") && (!item.TryGetValue("xw_is_virtual", out var value3) || !value3.AsBool()))
			{
				if (!dictionary.TryGetValue(text, out var value4))
				{
					value4 = _tree.CreateItem(treeItem);
					value4.Collapsed = true;
					value4.SetText(0, text);
					value4.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text));
					dictionary[text] = value4;
				}
				if (_registry.CreateNodeType("__XWBPGraphNode_CallMethod") is XWBPNodeCallMethod xWBPNodeCallMethod)
				{
					xWBPNodeCallMethod.MethodData = item;
					xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.Script;
					xWBPNodeCallMethod.BuildMethod();
					Variant value5;
					Dictionary dictionary2 = (item.TryGetValue("return", out value5) ? value5.As<Dictionary>() : new Dictionary());
					Variant value6;
					int variantType = (dictionary2.TryGetValue("type", out value6) ? value6.AsInt32() : 0);
					TreeItem treeItem2 = _tree.CreateItem(dictionary[text]);
					treeItem2.Collapsed = true;
					treeItem2.SetText(0, text2);
					treeItem2.SetTooltipText(0, $"调用 {text}.{text2} 方法");
					treeItem2.SetMetadata(0, xWBPNodeCallMethod);
					SetReturnTypeIconByVariantType(treeItem2, variantType, dictionary2.TryGetValue("class_name", out var value7) ? value7.AsString() : "");
				}
			}
		}
	}

	private void BuildContextPropertyCategory(string contextClassName)
	{
		List<Dictionary> mergedPropertyList = GetMergedPropertyList(contextClassName);
		if (mergedPropertyList.Count == 0)
		{
			return;
		}
		TreeItem treeItem = _tree.CreateItem(Root);
		treeItem.Collapsed = true;
		treeItem.SetText(0, "类属性");
		treeItem.SetSelectable(0, selectable: false);
		treeItem.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(contextClassName));
		System.Collections.Generic.Dictionary<string, TreeItem> dictionary = new System.Collections.Generic.Dictionary<string, TreeItem>();
		foreach (Dictionary item in mergedPropertyList)
		{
			Variant value;
			string text = (item.TryGetValue("base_class_name", out value) ? value.AsString() : "");
			Variant value2;
			string text2 = (item.TryGetValue("name", out value2) ? value2.AsString() : "");
			if (!string.IsNullOrEmpty(text2) && !text2.StartsWith("_") && ((item.TryGetValue("usage", out var value3) ? value3.AsInt32() : 0) & 2) != 0)
			{
				if (!dictionary.TryGetValue(text, out var value4))
				{
					value4 = _tree.CreateItem(treeItem);
					value4.Collapsed = true;
					value4.SetText(0, text);
					value4.SetIcon(0, XWClassRegistry.Instance.GetClassIcon(text));
					dictionary[text] = value4;
				}
				Variant value5;
				int num = (item.TryGetValue("type", out value5) ? value5.AsInt32() : 0);
				Variant value6;
				string className = ((num != 24) ? "" : (item.TryGetValue("class_name", out value6) ? value6.AsString() : ""));
				if (((!item.TryGetValue("has_get", out var value7) || value7.AsBool()) ? _registry.CreateNodeType("__XWBPGraphNode_GetProperty") : null) is XWBPNodeGetProperty xWBPNodeGetProperty)
				{
					xWBPNodeGetProperty.PropertyData = item;
					xWBPNodeGetProperty.MethodType = XWBPNodeGetProperty.Type.Script;
					xWBPNodeGetProperty.BuildProperty();
					TreeItem treeItem2 = _tree.CreateItem(dictionary[text]);
					treeItem2.Collapsed = true;
					treeItem2.SetText(0, "获取 " + text2);
					treeItem2.SetTooltipText(0, "获取 " + text2 + " 属性");
					treeItem2.SetMetadata(0, xWBPNodeGetProperty);
					SetReturnTypeIconByVariantType(treeItem2, num, className);
				}
				if (((!item.TryGetValue("has_set", out var value8) || value8.AsBool()) ? _registry.CreateNodeType("__XWBPGraphNode_SetProperty") : null) is XWBPNodeSetProperty xWBPNodeSetProperty)
				{
					xWBPNodeSetProperty.PropertyData = item;
					xWBPNodeSetProperty.MethodType = XWBPNodeSetProperty.Type.Script;
					xWBPNodeSetProperty.BuildProperty();
					TreeItem treeItem3 = _tree.CreateItem(dictionary[text]);
					treeItem3.Collapsed = true;
					treeItem3.SetText(0, "设置 " + text2);
					treeItem3.SetTooltipText(0, "设置 " + text2 + " 属性");
					treeItem3.SetMetadata(0, xWBPNodeSetProperty);
					SetReturnTypeIconByVariantType(treeItem3, num, className);
				}
			}
		}
	}

	private bool HasVirtualMethodEntry(string methodName)
	{
		if (Editor?.BpScriptData == null)
		{
			return false;
		}
		foreach (XWBPGraphData value in Editor.BpScriptData.Graphs.Values)
		{
			if (value != null)
			{
				XWBPGraphData xWBPGraphData = value;
				if (xWBPGraphData.HasVirtualMethodEntry(methodName))
				{
					return true;
				}
			}
		}
		return false;
	}

	private List<Dictionary> GetMergedSignalList(string className)
	{
		List<Dictionary> result = new List<Dictionary>();
		HashSet<string> seen = new HashSet<string>();
		AppendMemberList(result, seen, XWClassRegistry.Instance.GetClassSignalList(className), MemberSource.ClassRegistry, MemberKind.Signal);
		bool num = !XWClassRegistry.Instance.HasClass(className);
		if (num)
		{
			AppendMemberList(result, seen, _csharpRegistry.GetClassSignalList(className), MemberSource.CSharpRegistry, MemberKind.Signal);
		}
		string text = (num ? _csharpRegistry.GetExternalBaseClassName(className) : "");
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, className, StringComparison.Ordinal))
		{
			AppendMemberList(result, seen, XWClassRegistry.Instance.GetClassSignalList(text), MemberSource.ClassRegistry, MemberKind.Signal);
		}
		return result;
	}

	private List<Dictionary> GetMergedMethodList(string className)
	{
		List<Dictionary> result = new List<Dictionary>();
		HashSet<string> seen = new HashSet<string>();
		AppendMemberList(result, seen, XWClassRegistry.Instance.GetClassMethodList(className), MemberSource.ClassRegistry, MemberKind.Method);
		bool num = !XWClassRegistry.Instance.HasClass(className);
		if (num)
		{
			AppendMemberList(result, seen, _csharpRegistry.GetClassMethodList(className), MemberSource.CSharpRegistry, MemberKind.Method);
		}
		string text = (num ? _csharpRegistry.GetExternalBaseClassName(className) : "");
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, className, StringComparison.Ordinal))
		{
			AppendMemberList(result, seen, XWClassRegistry.Instance.GetClassMethodList(text), MemberSource.ClassRegistry, MemberKind.Method);
		}
		return result;
	}

	private List<Dictionary> GetMergedPropertyList(string className)
	{
		List<Dictionary> result = new List<Dictionary>();
		HashSet<string> seen = new HashSet<string>();
		AppendMemberList(result, seen, XWClassRegistry.Instance.GetClassPropertyList(className), MemberSource.ClassRegistry, MemberKind.Property);
		bool num = !XWClassRegistry.Instance.HasClass(className);
		if (num)
		{
			AppendMemberList(result, seen, _csharpRegistry.GetClassPropertyList(className), MemberSource.CSharpRegistry, MemberKind.Property);
		}
		string text = (num ? _csharpRegistry.GetExternalBaseClassName(className) : "");
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, className, StringComparison.Ordinal))
		{
			AppendMemberList(result, seen, XWClassRegistry.Instance.GetClassPropertyList(text), MemberSource.ClassRegistry, MemberKind.Property);
		}
		return result;
	}

	private static void AppendMemberList(List<Dictionary> result, HashSet<string> seen, List<Dictionary> sourceList, MemberSource source, MemberKind kind)
	{
		foreach (Dictionary source2 in sourceList)
		{
			Dictionary dictionary = NormalizeClassMemberData(source2, source, kind);
			string item = BuildMemberKey(dictionary, kind);
			if (seen.Add(item))
			{
				result.Add(dictionary);
			}
		}
	}

	private static Dictionary NormalizeClassMemberData(Dictionary source, MemberSource sourceKind, MemberKind memberKind)
	{
		Dictionary dictionary = new Dictionary();
		if (source != null)
		{
			foreach (Variant key in source.Keys)
			{
				dictionary[key] = source[key];
			}
		}
		string text = ReadString(dictionary, "name");
		string text2 = ReadString(dictionary, "base_class_name");
		if (!string.IsNullOrEmpty(text) && !dictionary.ContainsKey("cs_name"))
		{
			dictionary["cs_name"] = ((sourceKind == MemberSource.ClassRegistry) ? ToCsMemberName(text) : text);
		}
		if (!string.IsNullOrEmpty(text2))
		{
			if (!dictionary.ContainsKey("cs_class_name"))
			{
				dictionary["cs_class_name"] = ToCsClassName(text2);
			}
			if (!dictionary.ContainsKey("cs_qualified_class_name"))
			{
				dictionary["cs_qualified_class_name"] = ToCsClassName(text2);
			}
		}
		int num = ReadInt(dictionary, "flags");
		if (memberKind == MemberKind.Method)
		{
			if (!dictionary.ContainsKey("is_static"))
			{
				dictionary["is_static"] = sourceKind == MemberSource.ClassRegistry && (num & 0x20) != 0;
			}
			dictionary["xw_is_virtual"] = ((sourceKind == MemberSource.ClassRegistry) ? ((num & 8) != 0) : ((num & 0x40) != 0));
			dictionary["xw_is_abstract"] = sourceKind == MemberSource.CSharpRegistry && dictionary.TryGetValue("is_abstract", out var value) && value.AsBool();
		}
		else
		{
			dictionary["xw_is_virtual"] = false;
		}
		return dictionary;
	}

	private static string BuildMemberKey(Dictionary member, MemberKind kind)
	{
		string value = ReadString(member, "base_class_name");
		string value2 = ReadString(member, "cs_name", ReadString(member, "name"));
		if (member.TryGetValue("args", out var value3))
		{
			Godot.Collections.Array array = value3.As<Godot.Collections.Array>();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Variant item in array)
			{
				Dictionary data = item.As<Dictionary>();
				stringBuilder.Append(ReadInt(data, "type")).Append(':').Append(ReadString(data, "class_name"))
					.Append(':')
					.Append(ReadString(data, "cs_type_name"))
					.Append(';');
			}
			return $"{kind}:{value}:{value2}({stringBuilder})";
		}
		return $"{kind}:{value}:{value2}";
	}

	private static bool DoesClassImplementMethod(string className, string methodName)
	{
		if (XWClassRegistry.Instance.DoesClassImplementMethod(className, methodName))
		{
			return true;
		}
		if (XWClassRegistry.Instance.DoesClassImplementMethod(className, ToGodotMethodName(methodName)))
		{
			return true;
		}
		if (XWClassRegistry.Instance.HasClass(className))
		{
			return false;
		}
		return XWBPCSharpMemberRegistry.Instance.DoesClassImplementMethod(className, methodName);
	}

	private static string ToCsMemberName(string godotName)
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

	private static string ToPascalCase(string snakeName)
	{
		if (string.IsNullOrEmpty(snakeName))
		{
			return snakeName;
		}
		string text = "";
		bool flag = true;
		foreach (char c in snakeName)
		{
			if (c == '_')
			{
				flag = true;
				continue;
			}
			text += (flag ? char.ToUpperInvariant(c) : c);
			flag = false;
		}
		return text;
	}

	private static string ToGodotMethodName(string csName)
	{
		if (string.IsNullOrEmpty(csName))
		{
			return csName;
		}
		string text = (csName.StartsWith("_") ? csName.Substring(1) : csName);
		string text2 = "";
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (char.IsUpper(c) && i > 0)
			{
				text2 += "_";
			}
			text2 += char.ToLowerInvariant(c);
		}
		if (!csName.StartsWith("_"))
		{
			return text2;
		}
		return "_" + text2;
	}

	private static string ToCsClassName(string className)
	{
		if (!(className == "Object"))
		{
			return className;
		}
		return "GodotObject";
	}

	private static string ReadString(Dictionary data, string key, string fallback = "")
	{
		if (data == null || !data.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return value.AsString();
	}

	private static int ReadInt(Dictionary data, string key, int fallback = 0)
	{
		if (data == null || !data.TryGetValue(key, out var value))
		{
			return fallback;
		}
		return value.AsInt32();
	}

	private static bool IsStaticMemberData(Dictionary data)
	{
		if (data != null && data.TryGetValue("is_static", out var value))
		{
			return value.AsBool();
		}
		return false;
	}

	private void SetReturnTypeIcon(TreeItem item, List<XWBPNodePortData> outputPorts)
	{
		if (outputPorts == null)
		{
			item.SetIcon(0, _memberMethodIcon);
			return;
		}
		foreach (XWBPNodePortData outputPort in outputPorts)
		{
			if (outputPort.PortTypeValue != XWBPNodePortData.PortType.Flow)
			{
				Texture2D typeIcon = XWBPNodePortData.GetTypeIcon(outputPort.PortTypeValue, outputPort.ClassName);
				item.SetIcon(0, GodotObject.IsInstanceValid(typeIcon) ? typeIcon : _memberMethodIcon);
				return;
			}
		}
		item.SetIcon(0, _memberMethodIcon);
	}

	private void SetReturnTypeIconByVariantType(TreeItem item, int variantType, string className)
	{
		switch (variantType)
		{
		case 24:
		{
			Texture2D texture2D = ((!string.IsNullOrEmpty(className)) ? XWClassRegistry.Instance.GetClassIcon(className) : null);
			item.SetIcon(0, GodotObject.IsInstanceValid(texture2D) ? texture2D : _memberMethodIcon);
			break;
		}
		case 0:
			item.SetIcon(0, _variantIcon ?? _memberMethodIcon);
			break;
		default:
		{
			Texture2D typeIcon = XWTypeRegistry.Instance.GetTypeIcon((Variant.Type)variantType);
			item.SetIcon(0, GodotObject.IsInstanceValid(typeIcon) ? typeIcon : _memberMethodIcon);
			break;
		}
		}
	}

	private void SetVariableIcon(TreeItem item, Variant.Type variableType, StringName className)
	{
		switch (variableType)
		{
		case Variant.Type.Object:
		{
			Texture2D texture2D = ((!string.IsNullOrEmpty(className?.ToString())) ? XWClassRegistry.Instance.GetClassIcon(className.ToString()) : null);
			item.SetIcon(0, GodotObject.IsInstanceValid(texture2D) ? texture2D : _variantIcon);
			break;
		}
		case Variant.Type.Nil:
			item.SetIcon(0, _variantIcon ?? _memberMethodIcon);
			break;
		default:
		{
			Texture2D typeIcon = XWTypeRegistry.Instance.GetTypeIcon(variableType);
			item.SetIcon(0, GodotObject.IsInstanceValid(typeIcon) ? typeIcon : _memberMethodIcon);
			break;
		}
		}
	}

	protected override void OnItemSelected()
	{
		TreeItem selected = _tree.GetSelected();
		if (!GodotObject.IsInstanceValid(selected))
		{
			return;
		}
		if (!IsNodeItemPortCompatible(selected))
		{
			_nameLabel.Text = "";
			_describeLabel.Text = "";
			GetOkButton().Disabled = true;
			return;
		}
		MaterializeDeferredNode(selected);
		Variant metadata = selected.GetMetadata(0);
		ShowDescription(metadata);
		GetOkButton().Disabled = metadata.VariantType != Variant.Type.Object || metadata.As<XWBPNodeType>() == null;
		foreach (KeyValuePair<Button, TreeItem> visibleCardItem in _visibleCardItems)
		{
			if (GodotObject.IsInstanceValid(visibleCardItem.Key))
			{
				visibleCardItem.Key.ButtonPressed = visibleCardItem.Value == selected;
			}
		}
	}

	protected override void OnSearchTextChanged(string text)
	{
		_visualQuery = text ?? "";
		_visualPage = 0;
		if (!string.IsNullOrWhiteSpace(_visualQuery))
		{
			EnsureProjectApiCategoryPopulated();
		}
		ApplyNodeCatalogFilter(_visualQuery);
		RebuildVisualCards();
	}

	private void ApplyNodeCatalogFilter(string query)
	{
		if (!GodotObject.IsInstanceValid(Root))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(FilterPortData))
		{
			FilterTree(query ?? "");
			return;
		}
		string text = (query ?? "").ToLowerInvariant();
		TreeItem treeItem = Root;
		while (GodotObject.IsInstanceValid(treeItem))
		{
			bool flag = (treeItem.Visible = CanFilterTreeItem(text, treeItem));
			if (_deferredNodeChoices.TryGetValue(treeItem, out var value))
			{
				treeItem.SetSelectable(0, IsDeferredNodeCompatible(value));
			}
			if (flag)
			{
				treeItem.Collapsed = !string.IsNullOrEmpty(text);
				TreeItem parent = treeItem.GetParent();
				while (GodotObject.IsInstanceValid(parent))
				{
					parent.Visible = true;
					parent = parent.GetParent();
				}
				if (!string.IsNullOrEmpty(text))
				{
					treeItem.UncollapseTree();
				}
			}
			treeItem = treeItem.GetNextInTree();
		}
	}

	private void CaptureProjectContext()
	{
		_catalogProjectRoot = _csharpRegistry.ActiveProjectRoot;
		_catalogProjectContextVersion = _csharpRegistry.ProjectContextVersion;
		_catalogScriptData = Editor?.BpScriptData;
		_catalogGraph = CurrentGraph;
	}

	private bool IsProjectContextCurrent()
	{
		_csharpRegistry.ConfigureProjectRoot(Editor?.ActiveModProjectRoot);
		if (string.Equals(_catalogProjectRoot, _csharpRegistry.ActiveProjectRoot, StringComparison.OrdinalIgnoreCase) && _catalogProjectContextVersion == _csharpRegistry.ProjectContextVersion && _catalogScriptData == Editor?.BpScriptData)
		{
			return _catalogGraph == CurrentGraph;
		}
		return false;
	}

	protected override void ShowDescription(Variant meta)
	{
		if (meta.VariantType != Variant.Type.Object)
		{
			_nameLabel.Text = "";
			_describeLabel.Text = "";
			return;
		}
		XWBPNodeType xWBPNodeType = meta.As<XWBPNodeType>();
		if (xWBPNodeType == null)
		{
			_nameLabel.Text = "";
			_describeLabel.Text = "";
		}
		else
		{
			_nameLabel.Text = xWBPNodeType.DisplayName + "\n";
			_describeLabel.Text = "[color=gray]描述:[/color]\n" + xWBPNodeType.Description + "\n";
		}
	}

	protected override bool CanFilterTreeItem(string lowerQuery, TreeItem treeItem)
	{
		string text = treeItem.GetText(0);
		bool flag = string.IsNullOrEmpty(lowerQuery) || text.ToLower().Contains(lowerQuery);
		if (_deferredNodeChoices.TryGetValue(treeItem, out var value))
		{
			if (!string.IsNullOrEmpty(lowerQuery) && treeItem.GetTooltipText(0).ToLowerInvariant().Contains(lowerQuery))
			{
				flag = true;
			}
			if (flag)
			{
				return IsDeferredNodeCompatible(value);
			}
			return false;
		}
		Variant metadata = treeItem.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object)
		{
			XWBPNodeType xWBPNodeType = metadata.As<XWBPNodeType>();
			if (xWBPNodeType != null)
			{
				if (!string.IsNullOrEmpty(lowerQuery))
				{
					if ((xWBPNodeType.TypeId?.ToString() ?? "").ToLower().Contains(lowerQuery))
					{
						flag = true;
					}
					if ((xWBPNodeType.Description ?? "").ToLower().Contains(lowerQuery))
					{
						flag = true;
					}
				}
				if (GodotObject.IsInstanceValid(FilterPortData))
				{
					bool flag2 = false;
					if (FilterSourceIsOutput)
					{
						foreach (XWBPNodePortData inputPort in xWBPNodeType.InputPorts)
						{
							if (FilterPortData.CanConnectTo(inputPort))
							{
								flag2 = true;
								break;
							}
						}
					}
					else
					{
						foreach (XWBPNodePortData outputPort in xWBPNodeType.OutputPorts)
						{
							if (outputPort.CanConnectTo(FilterPortData))
							{
								flag2 = true;
								break;
							}
						}
					}
					return flag && flag2;
				}
				return flag;
			}
		}
		if (GodotObject.IsInstanceValid(FilterPortData) && string.IsNullOrEmpty(lowerQuery))
		{
			return false;
		}
		return flag;
	}

	private bool IsNodeItemPortCompatible(TreeItem item)
	{
		if (!GodotObject.IsInstanceValid(FilterPortData))
		{
			return true;
		}
		if (_deferredNodeChoices.TryGetValue(item, out var value))
		{
			return IsDeferredNodeCompatible(value);
		}
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object)
		{
			XWBPNodeType xWBPNodeType = metadata.As<XWBPNodeType>();
			if (xWBPNodeType != null)
			{
				if (FilterSourceIsOutput)
				{
					foreach (XWBPNodePortData inputPort in xWBPNodeType.InputPorts)
					{
						if (FilterPortData.CanConnectTo(inputPort))
						{
							return true;
						}
					}
				}
				else
				{
					foreach (XWBPNodePortData outputPort in xWBPNodeType.OutputPorts)
					{
						if (outputPort.CanConnectTo(FilterPortData))
						{
							return true;
						}
					}
				}
				return false;
			}
		}
		return false;
	}

	private static Texture2D LoadIconSafe(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}
}
