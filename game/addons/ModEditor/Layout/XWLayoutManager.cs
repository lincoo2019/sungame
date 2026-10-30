using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Layout;

public partial class XWLayoutManager : RefCounted
{
	private const string LayoutDir = "user://ModEditorLayouts";

	private const string CurrentLayoutPath = "user://ModEditorLayouts/current_layout.cfg";

	private static readonly int[] SideDockPositions = new int[8] { 4, 5, 6, 7, 8, 9, 10, 11 };

	private static readonly HashSet<string> AutoHideParentNames = new HashSet<string> { "LeftCol", "LeftColRight", "LeftPanel", "RightCol", "RightColRight", "RightPanel" };

	private Control _root;

	private readonly System.Collections.Generic.Dictionary<int, TabContainer> _containers = new System.Collections.Generic.Dictionary<int, TabContainer>();

	private readonly System.Collections.Generic.Dictionary<string, XWEditorDock> _docksByKey = new System.Collections.Generic.Dictionary<string, XWEditorDock>();

	private readonly List<XWWindowWrapper> _floatingWindows = new List<XWWindowWrapper>();

	private readonly HashSet<TabContainer> _observedTabContainers = new HashSet<TabContainer>();

	private XWPanelRegistry _panelRegistry;

	private XWLayoutData _currentLayout = new XWLayoutData();

	public static XWLayoutManager Instance { get; private set; }

	public string ActiveMainPanelKey { get; private set; } = "";

	public void Initialize(Control root, XWPanelRegistry panelRegistry)
	{
		Instance = this;
		_root = root;
		_panelRegistry = panelRegistry;
		BuildLayout();
		LoadLayout();
	}

	public void InitializeExisting(Control root, XWPanelRegistry panelRegistry)
	{
		Instance = this;
		_root = root;
		_panelRegistry = panelRegistry;
		LoadLayout();
	}

	public void AdoptContainer(int position, TabContainer container)
	{
		_containers[position] = container;
		ApplyExpandFill(container);
		if (_observedTabContainers.Add(container))
		{
			container.TabChanged += delegate
			{
				CaptureActiveMainPanel(container);
			};
		}
		if (container is XWDockContainer xWDockContainer)
		{
			xWDockContainer.DockPosition = position;
		}
		UpdateDockVisibility();
	}

	private void BuildLayout()
	{
		foreach (Node child in _root.GetChildren())
		{
			if (child != null)
			{
				Node node = child;
				node.QueueFree();
			}
		}
		HSplitContainer hSplitContainer = new HSplitContainer
		{
			Name = "MainHSplit"
		};
		hSplitContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		ApplyExpandFill(hSplitContainer);
		HSplitContainer hSplitContainer2 = new HSplitContainer
		{
			Name = "LeftPanel"
		};
		hSplitContainer2.CustomMinimumSize = new Vector2(240f, 0f);
		ApplyExpandFill(hSplitContainer2);
		VSplitContainer vSplitContainer = new VSplitContainer
		{
			Name = "LeftCol"
		};
		ApplyExpandFill(vSplitContainer);
		XWDockContainer node2 = CreateContainer(4);
		XWDockContainer node3 = CreateContainer(5);
		vSplitContainer.AddChild(node2, forceReadableName: false, Node.InternalMode.Disabled);
		vSplitContainer.AddChild(node3, forceReadableName: false, Node.InternalMode.Disabled);
		VSplitContainer vSplitContainer2 = new VSplitContainer
		{
			Name = "LeftColRight"
		};
		ApplyExpandFill(vSplitContainer2);
		XWDockContainer node4 = CreateContainer(6);
		XWDockContainer node5 = CreateContainer(7);
		vSplitContainer2.AddChild(node4, forceReadableName: false, Node.InternalMode.Disabled);
		vSplitContainer2.AddChild(node5, forceReadableName: false, Node.InternalMode.Disabled);
		hSplitContainer2.AddChild(vSplitContainer, forceReadableName: false, Node.InternalMode.Disabled);
		hSplitContainer2.AddChild(vSplitContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		XWDockContainer node6 = CreateContainer(2);
		HSplitContainer hSplitContainer3 = new HSplitContainer
		{
			Name = "RightPanel"
		};
		hSplitContainer3.CustomMinimumSize = new Vector2(280f, 0f);
		ApplyExpandFill(hSplitContainer3);
		VSplitContainer vSplitContainer3 = new VSplitContainer
		{
			Name = "RightCol"
		};
		ApplyExpandFill(vSplitContainer3);
		XWDockContainer node7 = CreateContainer(8);
		XWDockContainer node8 = CreateContainer(9);
		vSplitContainer3.AddChild(node7, forceReadableName: false, Node.InternalMode.Disabled);
		vSplitContainer3.AddChild(node8, forceReadableName: false, Node.InternalMode.Disabled);
		VSplitContainer vSplitContainer4 = new VSplitContainer
		{
			Name = "RightColRight"
		};
		ApplyExpandFill(vSplitContainer4);
		XWDockContainer node9 = CreateContainer(10);
		XWDockContainer node10 = CreateContainer(11);
		vSplitContainer4.AddChild(node9, forceReadableName: false, Node.InternalMode.Disabled);
		vSplitContainer4.AddChild(node10, forceReadableName: false, Node.InternalMode.Disabled);
		hSplitContainer3.AddChild(vSplitContainer3, forceReadableName: false, Node.InternalMode.Disabled);
		hSplitContainer3.AddChild(vSplitContainer4, forceReadableName: false, Node.InternalMode.Disabled);
		hSplitContainer.AddChild(hSplitContainer2, forceReadableName: false, Node.InternalMode.Disabled);
		hSplitContainer.AddChild(node6, forceReadableName: false, Node.InternalMode.Disabled);
		hSplitContainer.AddChild(hSplitContainer3, forceReadableName: false, Node.InternalMode.Disabled);
		VSplitContainer vSplitContainer5 = new VSplitContainer
		{
			Name = "OuterVSplit"
		};
		vSplitContainer5.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		ApplyExpandFill(vSplitContainer5);
		vSplitContainer5.AddChild(hSplitContainer, forceReadableName: false, Node.InternalMode.Disabled);
		XWDockContainer xWDockContainer = CreateContainer(3);
		xWDockContainer.CustomMinimumSize = new Vector2(0f, 120f);
		vSplitContainer5.AddChild(xWDockContainer, forceReadableName: false, Node.InternalMode.Disabled);
		_root.AddChild(vSplitContainer5, forceReadableName: false, Node.InternalMode.Disabled);
		UpdateDockVisibility();
	}

	private XWDockContainer CreateContainer(int position)
	{
		XWDockContainer xWDockContainer = new XWDockContainer
		{
			Name = $"Dock_{position}",
			DockPosition = position,
			TabAlignment = TabBar.AlignmentMode.Left
		};
		xWDockContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		ApplyExpandFill(xWDockContainer);
		_containers[position] = xWDockContainer;
		return xWDockContainer;
	}

	private static void ApplyExpandFill(Control control)
	{
		if (control != null)
		{
			control.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			control.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		}
	}

	public void RefreshDockVisibility()
	{
		UpdateDockVisibility();
	}

	private void UpdateDockVisibility()
	{
		int[] sideDockPositions = SideDockPositions;
		foreach (int key in sideDockPositions)
		{
			if (_containers.TryGetValue(key, out var value) && GodotObject.IsInstanceValid(value))
			{
				value.Visible = HasTabs(value);
			}
		}
		sideDockPositions = SideDockPositions;
		foreach (int key2 in sideDockPositions)
		{
			if (_containers.TryGetValue(key2, out var value2) && GodotObject.IsInstanceValid(value2))
			{
				UpdateParentVisibility(value2);
			}
		}
	}

	private static bool HasTabs(TabContainer container)
	{
		for (int i = 0; i < container.GetTabCount(); i++)
		{
			Control tabControl = container.GetTabControl(i);
			if (tabControl != null && GodotObject.IsInstanceValid(tabControl) && !tabControl.IsQueuedForDeletion())
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateParentVisibility(Node node)
	{
		Node parent = node.GetParent();
		while (parent is Control control && control != _root)
		{
			string item = control.Name.ToString();
			if (AutoHideParentNames.Contains(item))
			{
				control.Visible = HasVisibleControlChild(control);
			}
			parent = control.GetParent();
		}
	}

	private static bool HasVisibleControlChild(Control control)
	{
		foreach (Node child in control.GetChildren())
		{
			if (child is Control control2 && GodotObject.IsInstanceValid(control2) && !control2.IsQueuedForDeletion() && control2.Visible)
			{
				return true;
			}
		}
		return false;
	}

	public void AddPanel(Control panel, string key, string title, int position)
	{
		XWEditorDock xWEditorDock2;
		if (panel is XWEditorDock xWEditorDock)
		{
			xWEditorDock2 = xWEditorDock;
		}
		else
		{
			xWEditorDock2 = new XWEditorDock
			{
				Title = title,
				LayoutKey = key
			};
			panel.Name = "Content";
			xWEditorDock2.AddChild(panel, forceReadableName: false, Node.InternalMode.Disabled);
		}
		panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		xWEditorDock2.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		xWEditorDock2.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		if (string.IsNullOrEmpty(xWEditorDock2.LayoutKey))
		{
			xWEditorDock2.LayoutKey = key;
		}
		if (string.IsNullOrEmpty(xWEditorDock2.Title))
		{
			xWEditorDock2.Title = title;
		}
		xWEditorDock2.DefaultSlot = _panelRegistry?.GetDefaultDock(key) ?? position;
		xWEditorDock2.IsMainScreen = _panelRegistry?.IsMainPanel(key) ?? xWEditorDock2.IsMainScreen;
		xWEditorDock2.Closable = _panelRegistry?.CanClose(key) ?? xWEditorDock2.Closable;
		XWEditorDock xWEditorDock3 = xWEditorDock2;
		if (xWEditorDock3.DockIcon == null)
		{
			xWEditorDock3.DockIcon = _panelRegistry?.GetIcon(key);
		}
		xWEditorDock2.Name = title;
		xWEditorDock2.SetMeta("_dock_panel_key", key);
		_docksByKey[key] = xWEditorDock2;
		AddDockTo(xWEditorDock2, position);
	}

	public void AddDockTo(XWEditorDock dock, int position)
	{
		int num = ResolveDockPositionFor(dock, position);
		if (_containers.TryGetValue(num, out var value))
		{
			if (dock.GetParent() is TabContainer tabContainer)
			{
				tabContainer.RemoveChild(dock);
			}
			if (!string.IsNullOrEmpty(dock.Title))
			{
				dock.Name = dock.Title;
			}
			if (value is XWDockContainer xWDockContainer)
			{
				xWDockContainer.AddDock(dock);
			}
			else
			{
				value.AddChild(dock, forceReadableName: false, Node.InternalMode.Disabled);
			}
			value.CurrentTab = value.GetTabCount() - 1;
			if (num == 3)
			{
				ShowAncestorControls(value);
			}
			UpdateDockVisibility();
		}
	}

	private static int ResolveDockPositionFor(XWEditorDock dock, int position)
	{
		if (dock != null && dock.IsMainScreen)
		{
			return 2;
		}
		int num = XWLayoutData.ResolveDockPosition(position);
		if (dock != null && num == 2)
		{
			return ResolveFallbackDockFor(dock);
		}
		return num;
	}

	private static int ResolveFallbackDockFor(XWEditorDock dock)
	{
		int num = XWLayoutData.ResolveDockPosition(dock?.DefaultSlot ?? 4);
		if (num == 2)
		{
			return 4;
		}
		return num;
	}

	private static bool IsCenterDock(int position)
	{
		return XWLayoutData.ResolveDockPosition(position) == 2;
	}

	public bool CanMoveDockTo(XWEditorDock dock, int position)
	{
		if (dock == null)
		{
			return false;
		}
		if (!dock.IsMainScreen)
		{
			return !IsCenterDock(position);
		}
		return IsCenterDock(position);
	}

	public bool CanPanelMoveToDock(string panelKey, int targetDock)
	{
		if (_docksByKey.TryGetValue(panelKey, out var value))
		{
			return CanMoveDockTo(value, targetDock);
		}
		return false;
	}

	public void MoveDockTo(XWEditorDock dock, int newPosition)
	{
		if (CanMoveDockTo(dock, newPosition))
		{
			AddDockTo(dock, newPosition);
			AutoSaveLayout();
		}
	}

	public void RestoreDock(XWEditorDock dock)
	{
		AddDockTo(dock, dock.DefaultSlot);
	}

	public void FocusPanel(string key)
	{
		if (!_docksByKey.TryGetValue(key, out var value))
		{
			return;
		}
		value.Visible = true;
		UpdateDockVisibility();
		if (value.GetParent() is TabContainer tabContainer)
		{
			ShowAncestorControls(tabContainer);
			int num = -1;
			for (int i = 0; i < tabContainer.GetTabCount(); i++)
			{
				if (tabContainer.GetTabControl(i) == value)
				{
					num = i;
					break;
				}
			}
			if (num >= 0)
			{
				tabContainer.CurrentTab = num;
			}
		}
		SetActiveMainPanel(key);
	}

	private void CaptureActiveMainPanel(TabContainer container)
	{
		if (GodotObject.IsInstanceValid(container) && container.CurrentTab >= 0 && container.CurrentTab < container.GetTabCount() && container.GetTabControl(container.CurrentTab) is XWEditorDock xWEditorDock)
		{
			SetActiveMainPanel(xWEditorDock.LayoutKey);
		}
	}

	private void SetActiveMainPanel(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			XWPanelRegistry panelRegistry = _panelRegistry;
			if (panelRegistry != null && panelRegistry.IsMainPanel(key))
			{
				ActiveMainPanelKey = key;
			}
		}
	}

	private void ShowAncestorControls(Control control)
	{
		Node node = control;
		while (node is Control control2 && control2 != _root)
		{
			control2.Visible = true;
			if (control2.GetParent() is TabContainer tabContainer)
			{
				for (int i = 0; i < tabContainer.GetTabCount(); i++)
				{
					if (tabContainer.GetTabControl(i) == control2)
					{
						tabContainer.CurrentTab = i;
						break;
					}
				}
			}
			node = control2.GetParent();
		}
	}

	public XWEditorDock GetDock(string key)
	{
		if (!_docksByKey.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void AddControlToDock(int dockSlot, Control control)
	{
		if (control is XWEditorDock dock)
		{
			AddDockTo(dock, dockSlot);
		}
	}

	public void RemoveControlFromDock(Control control)
	{
		if (control is XWEditorDock xWEditorDock && !string.IsNullOrEmpty(xWEditorDock.LayoutKey))
		{
			_docksByKey.Remove(xWEditorDock.LayoutKey);
		}
		if (control.GetParent() is TabContainer tabContainer)
		{
			tabContainer.RemoveChild(control);
			UpdateDockVisibility();
		}
	}

	public void AddMainScreenPlugin(XWEditorPlugin plugin)
	{
	}

	public void RemoveMainScreenPlugin(XWEditorPlugin plugin)
	{
	}

	public void ApplySavedLayout()
	{
		if (_currentLayout == null || _currentLayout.PanelDocks.Count == 0)
		{
			return;
		}
		foreach (KeyValuePair<string, int> item in new List<KeyValuePair<string, int>>(_currentLayout.PanelDocks))
		{
			if (_docksByKey.TryGetValue(item.Key, out var value))
			{
				int num = ResolveDockPositionFor(value, item.Value);
				if (GetDockPosition(value) != num)
				{
					AddDockTo(value, num);
				}
			}
		}
		UpdateDockVisibility();
	}

	public int GetDockPosition(XWEditorDock dock)
	{
		foreach (KeyValuePair<int, TabContainer> container in _containers)
		{
			for (int i = 0; i < container.Value.GetTabCount(); i++)
			{
				if (container.Value.GetTabControl(i) == dock)
				{
					return container.Key;
				}
			}
		}
		return -1;
	}

	public void AutoSaveLayout()
	{
		EnsureLayoutDir();
		_currentLayout.PanelDocks.Clear();
		foreach (KeyValuePair<int, TabContainer> container in _containers)
		{
			for (int i = 0; i < container.Value.GetTabCount(); i++)
			{
				if (container.Value.GetTabControl(i) is XWEditorDock xWEditorDock && !string.IsNullOrEmpty(xWEditorDock.LayoutKey))
				{
					_currentLayout.PanelDocks[xWEditorDock.LayoutKey] = container.Key;
				}
			}
		}
		ConfigFile configFile = new ConfigFile();
		configFile.SetValue("layout", "data", _currentLayout.ToJson());
		configFile.Save("user://ModEditorLayouts/current_layout.cfg");
	}

	private void LoadLayout()
	{
		ConfigFile configFile = new ConfigFile();
		if (configFile.Load("user://ModEditorLayouts/current_layout.cfg") == Error.Ok && configFile.HasSectionKey("layout", "data"))
		{
			Dictionary data = (Dictionary)configFile.GetValue("layout", "data");
			_currentLayout.FromJson(data);
		}
	}

	private void EnsureLayoutDir()
	{
		if (!DirAccess.DirExistsAbsolute("user://ModEditorLayouts"))
		{
			DirAccess.MakeDirAbsolute("user://ModEditorLayouts");
		}
	}

	public XWLayoutData GetCurrentLayout()
	{
		return _currentLayout;
	}

	public TabContainer GetContainer(int position)
	{
		if (!_containers.TryGetValue(position, out var value))
		{
			return null;
		}
		return value;
	}

	public XWDockContainer GetDockContainer(int position)
	{
		if (!_containers.TryGetValue(position, out var value))
		{
			return null;
		}
		return value as XWDockContainer;
	}

	public Control GetRoot()
	{
		return _root;
	}

	public XWPanelRegistry GetRegistry()
	{
		return _panelRegistry;
	}

	public int GetPanelCurrentDock(string panelKey)
	{
		if (!_docksByKey.TryGetValue(panelKey, out var value))
		{
			return 4;
		}
		return GetDockPosition(value);
	}

	public void MovePanelToDock(string panelKey, int targetDock)
	{
		if (_docksByKey.TryGetValue(panelKey, out var value) && CanMoveDockTo(value, targetDock))
		{
			int num = ResolveDockPositionFor(value, targetDock);
			if (GetDockPosition(value) != num)
			{
				MoveDockTo(value, num);
			}
		}
	}

	public string GetPanelName(string panelKey)
	{
		return _panelRegistry?.GetDisplayName(panelKey) ?? panelKey;
	}
}
