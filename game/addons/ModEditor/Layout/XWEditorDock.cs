using Godot;

namespace PVZHE.ModEditor.Layout;

public partial class XWEditorDock : MarginContainer
{
	[Export(PropertyHint.None, "")]
	public string LayoutKey = "";

	[Export(PropertyHint.None, "")]
	public string Title = "";

	[Export(PropertyHint.None, "")]
	public int DefaultSlot;

	[Export(PropertyHint.None, "")]
	public bool Closable = true;

	[Export(PropertyHint.None, "")]
	public bool IsGlobal;

	[Export(PropertyHint.None, "")]
	public bool IsTransient;

	[Export(PropertyHint.None, "")]
	public bool IsMainScreen;

	public Texture2D DockIcon;

	public Shortcut Shortcut;

	protected XWLayoutManager _layoutManager;

	protected XWWindowWrapper _floatingWindow;

	protected bool _isFloating;

	public override void _Ready()
	{
		_layoutManager = XWLayoutManager.Instance;
		AddThemeConstantOverride("margin_top", 0);
		AddThemeConstantOverride("margin_bottom", 0);
		AddThemeConstantOverride("margin_left", 0);
		AddThemeConstantOverride("margin_right", 0);
		if (!string.IsNullOrEmpty(Title))
		{
			base.Name = Title;
		}
	}

	public void SetTitle(string title)
	{
		Title = title;
		base.Name = title;
		if (GetParent() is TabContainer tabContainer)
		{
			int num = tabContainer.GetChildren().IndexOf(this);
			if (num >= 0)
			{
				tabContainer.SetTabTitle(num, title);
			}
		}
	}

	public void MakeFloating(Rect2I rect = default(Rect2I))
	{
		if (!_isFloating)
		{
			_floatingWindow = new XWWindowWrapper();
			_floatingWindow.Title = Title;
			if (!rect.Equals(default(Rect2I)))
			{
				_floatingWindow.Position = rect.Position;
				_floatingWindow.Size = rect.Size;
			}
			GetParent()?.RemoveChild(this);
			_layoutManager?.RefreshDockVisibility();
			_floatingWindow.AddChild(this, forceReadableName: false, InternalMode.Disabled);
			_layoutManager?.GetRoot()?.AddChild(_floatingWindow, forceReadableName: false, InternalMode.Disabled);
			_floatingWindow.CloseRequested += MakeDocked;
			_isFloating = true;
		}
	}

	public void MakeDocked()
	{
		if (_isFloating && _floatingWindow != null)
		{
			_floatingWindow.RemoveChild(this);
			_floatingWindow.QueueFree();
			_floatingWindow = null;
			_isFloating = false;
			_layoutManager?.RestoreDock(this);
		}
	}

	public void SetDockVisible(bool visible)
	{
		base.Visible = visible;
	}

	public bool IsFloating()
	{
		return _isFloating;
	}

	public void Close()
	{
		if (Closable)
		{
			if (_isFloating)
			{
				_floatingWindow?.QueueFree();
				return;
			}
			_layoutManager?.RemoveControlFromDock(this);
			QueueFree();
		}
	}
}
