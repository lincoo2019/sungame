using Godot;

namespace PVZHE.ModEditor.Layout;

public partial class XWDockContainer : TabContainer
{
	private const float DragThreshold = 12f;

	private const int FloatWindowId = 100;

	private const int CloseId = 101;

	private static readonly int[] MoveDockIds = new int[10] { 4, 5, 6, 7, 2, 8, 9, 10, 11, 3 };

	private bool _isDraggingTab;

	private Vector2 _dragStartPos = Vector2.Zero;

	private int _dragTabIdx = -1;

	private int _pressedTabIdx = -1;

	private bool _pressedTabWasCurrent;

	private PopupMenu _tabContextMenu;

	private int _contextTabIndex = -1;

	public int DockPosition { get; set; }

	public override void _Ready()
	{
		TabBar tabBar = GetTabBar();
		if (tabBar != null)
		{
			tabBar.GuiInput += OnTabBarInput;
		}
		_tabContextMenu = GetNode<PopupMenu>("%TabContextMenu");
		_tabContextMenu.IdPressed += OnContextMenuItem;
	}

	private void OnTabBarInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEventMouseButton)
		{
			if (inputEventMouseButton.ButtonIndex == MouseButton.Left)
			{
				if (inputEventMouseButton.Pressed)
				{
					int num = (_pressedTabIdx = GetTabBar()?.GetTabIdxAtPoint(inputEventMouseButton.Position) ?? (-1));
					_pressedTabWasCurrent = num >= 0 && num == base.CurrentTab;
					if (num >= 0)
					{
						_isDraggingTab = true;
						_dragStartPos = inputEventMouseButton.Position;
						_dragTabIdx = num;
					}
					return;
				}
				int releaseTabIdx = GetTabBar()?.GetTabIdxAtPoint(inputEventMouseButton.Position) ?? (-1);
				if (ShouldHideBottomPanelOnTabClick(releaseTabIdx, inputEventMouseButton.Position))
				{
					HideBottomPanelParent();
					GetViewport()?.SetInputAsHandled();
				}
				_isDraggingTab = false;
				_dragTabIdx = -1;
				_pressedTabIdx = -1;
				_pressedTabWasCurrent = false;
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Right && inputEventMouseButton.Pressed)
			{
				int num2 = GetTabBar()?.GetTabIdxAtPoint(inputEventMouseButton.Position) ?? (-1);
				if (num2 >= 0)
				{
					_contextTabIndex = num2;
					UpdateContextMenuState(GetTabControl(_contextTabIndex) as XWEditorDock);
					_tabContextMenu.Position = DisplayServer.MouseGetPosition();
					_tabContextMenu.ResetSize();
					_tabContextMenu.Popup();
				}
			}
		}
		else
		{
			if (!(@event is InputEventMouseMotion inputEventMouseMotion) || !_isDraggingTab || _dragTabIdx < 0 || !(_dragStartPos.DistanceTo(inputEventMouseMotion.Position) > 12f))
			{
				return;
			}
			Control tabControl = GetTabControl(_dragTabIdx);
			if (tabControl != null)
			{
				string text = IdentifyPanel(tabControl);
				if (text != "")
				{
					XWDragData xWDragData = new XWDragData(text, XWDragData.Type.DockPanel);
					Label preview = CreateDragPreview(text);
					ForceDrag(xWDragData, preview);
				}
			}
			_isDraggingTab = false;
			_dragTabIdx = -1;
		}
	}

	private bool ShouldHideBottomPanelOnTabClick(int releaseTabIdx, Vector2 releasePos)
	{
		if (DockPosition == 3 && _pressedTabWasCurrent && _pressedTabIdx >= 0 && releaseTabIdx == _pressedTabIdx)
		{
			return _dragStartPos.DistanceTo(releasePos) <= 12f;
		}
		return false;
	}

	private void HideBottomPanelParent()
	{
		for (Node parent = GetParent(); parent is Control control; parent = control.GetParent())
		{
			if (control.Name == (StringName)"BottomPanel" || control.GetType().Name == "XWBottomPanel")
			{
				control.Visible = false;
				return;
			}
		}
		base.Visible = false;
	}

	private static Label CreateDragPreview(string panelKey)
	{
		string text = panelKey;
		if (XWLayoutManager.Instance != null)
		{
			text = XWLayoutManager.Instance.GetPanelName(panelKey);
		}
		Label label = new Label();
		label.Text = "  " + text + "  ";
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat();
		styleBoxFlat.BgColor = new Color(0.04f, 0.54f, 1f, 0.7f);
		styleBoxFlat.SetCornerRadiusAll(4);
		styleBoxFlat.SetContentMarginAll(8f);
		styleBoxFlat.BorderColor = new Color(0.06f, 0.58f, 1f, 0.8f);
		styleBoxFlat.BorderWidthLeft = 1;
		styleBoxFlat.BorderWidthTop = 1;
		styleBoxFlat.BorderWidthRight = 1;
		styleBoxFlat.BorderWidthBottom = 1;
		styleBoxFlat.ShadowColor = new Color(0.04f, 0.54f, 1f, 0.35f);
		styleBoxFlat.ShadowSize = 16;
		styleBoxFlat.ShadowOffset = new Vector2(0f, 3f);
		label.AddThemeStyleboxOverride("normal", styleBoxFlat);
		label.Scale = new Vector2(0.8f, 0.8f);
		label.PivotOffset = label.Size / 2f;
		Tween tween = label.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(label, "scale", new Vector2(1f, 1f), 0.2);
		return label;
	}

	public void ReorderTab(string panelKey, Vector2 mouseGlobalPos)
	{
		Control control = null;
		for (int i = 0; i < GetTabCount(); i++)
		{
			Control tabControl = GetTabControl(i);
			if (tabControl != null && tabControl.HasMeta("_dock_panel_key") && (string)tabControl.GetMeta("_dock_panel_key") == panelKey)
			{
				control = tabControl;
				break;
			}
		}
		if (control == null)
		{
			return;
		}
		int tabIdxFromControl = GetTabIdxFromControl(control);
		if (tabIdxFromControl < 0)
		{
			return;
		}
		TabBar tabBar = GetTabBar();
		if (tabBar == null)
		{
			return;
		}
		int dropTabIndex = GetDropTabIndex(tabBar, mouseGlobalPos);
		if (dropTabIndex >= 0 && dropTabIndex != tabIdxFromControl)
		{
			MoveTabControl(control, dropTabIndex);
			string text = "";
			if (XWLayoutManager.Instance != null)
			{
				text = XWLayoutManager.Instance.GetPanelName(panelKey);
			}
			if (string.IsNullOrEmpty(text))
			{
				text = panelKey;
			}
			int tabIdxFromControl2 = GetTabIdxFromControl(control);
			if (tabIdxFromControl2 >= 0)
			{
				SetTabTitle(tabIdxFromControl2, text);
				base.CurrentTab = tabIdxFromControl2;
			}
		}
	}

	private int GetDropTabIndex(TabBar tabBar, Vector2 mouseGlobalPos)
	{
		Vector2 point = mouseGlobalPos - tabBar.GetGlobalRect().Position;
		point.Y = Mathf.Max(1f, tabBar.Size.Y * 0.5f);
		int tabIdxAtPoint = tabBar.GetTabIdxAtPoint(point);
		if (tabIdxAtPoint >= 0)
		{
			return tabIdxAtPoint;
		}
		int tabCount = GetTabCount();
		if (tabCount <= 0)
		{
			return -1;
		}
		if (!(point.X <= 0f))
		{
			return tabCount - 1;
		}
		return 0;
	}

	private void MoveTabControl(Control panel, int tabIndex)
	{
		Control tabControl = GetTabControl(tabIndex);
		if (tabControl != null && tabControl != panel)
		{
			MoveChild(panel, tabControl.GetIndex());
		}
	}

	private static string IdentifyPanel(Control panel)
	{
		if (panel != null && panel.HasMeta("_dock_panel_key"))
		{
			return (string)panel.GetMeta("_dock_panel_key");
		}
		return "";
	}

	private void UpdateContextMenuState(XWEditorDock dock)
	{
		int[] moveDockIds = MoveDockIds;
		foreach (int num in moveDockIds)
		{
			SetMoveItemDisabled(num, dock, num);
		}
	}

	private void SetMoveItemDisabled(int itemId, XWEditorDock dock, int targetDock)
	{
		int itemIndex = _tabContextMenu.GetItemIndex(itemId);
		if (itemIndex >= 0)
		{
			bool flag = XWLayoutManager.Instance?.CanMoveDockTo(dock, targetDock) ?? false;
			_tabContextMenu.SetItemDisabled(itemIndex, !flag);
		}
	}

	private void OnContextMenuItem(long id)
	{
		if (_contextTabIndex >= 0 && GetTabControl(_contextTabIndex) is XWEditorDock xWEditorDock)
		{
			switch (id)
			{
			case 100L:
				xWEditorDock.MakeFloating();
				break;
			case 101L:
				xWEditorDock.Close();
				XWLayoutManager.Instance?.AutoSaveLayout();
				break;
			default:
				XWLayoutManager.Instance?.MoveDockTo(xWEditorDock, (int)id);
				break;
			}
			_contextTabIndex = -1;
		}
	}

	public void AddDock(XWEditorDock dock)
	{
		AddChild(dock, forceReadableName: false, InternalMode.Disabled);
		int tabIdx = (base.CurrentTab = GetTabCount() - 1);
		Texture2D texture2D = dock.DockIcon;
		if (texture2D == null && !string.IsNullOrEmpty(dock.LayoutKey))
		{
			texture2D = XWPanelRegistry.Instance?.GetIcon(dock.LayoutKey);
		}
		if (texture2D != null)
		{
			SetTabIcon(tabIdx, texture2D);
		}
	}

	public void RemoveDock(XWEditorDock dock)
	{
		RemoveChild(dock);
	}
}
