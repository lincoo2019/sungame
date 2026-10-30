using Godot;

namespace PVZHE.ModEditor.GUI;

public partial class XWBottomPanel : VBoxContainer
{
	private const int CollapsedHeight = 32;

	private const int ExpandedHeight = 220;

	private Button _pinButton;

	private Button _expandButton;

	private HBoxContainer _toasterContainer;

	private MenuButton _layoutButton;

	private TabContainer _contentContainer;

	public TabContainer ContentContainer => _contentContainer;

	public HBoxContainer ToasterContainer => _toasterContainer;

	public override void _Ready()
	{
		_pinButton = GetNode<Button>("%PinButton");
		_expandButton = GetNode<Button>("%ExpandButton");
		_toasterContainer = GetNode<HBoxContainer>("%ToasterContainer");
		_layoutButton = GetNode<MenuButton>("%LayoutButton");
		_contentContainer = GetNode<TabContainer>("%ContentContainer");
		base.SizeFlagsVertical = SizeFlags.ExpandFill;
		_pinButton.Toggled += OnPinToggled;
		_expandButton.Toggled += OnExpandToggled;
		ApplyExpandedState(_expandButton.ButtonPressed);
	}

	private void OnPinToggled(bool pinned)
	{
	}

	private void OnExpandToggled(bool expanded)
	{
		ApplyExpandedState(expanded);
	}

	private void ApplyExpandedState(bool expanded)
	{
		int num = (expanded ? 220 : 32);
		_contentContainer.Visible = expanded;
		base.SizeFlagsVertical = (SizeFlags)(expanded ? 3 : 0);
		base.CustomMinimumSize = new Vector2(0f, num);
		SyncParentSplitOffset(num);
		CallDeferred("SyncCurrentHeight");
	}

	private void SyncCurrentHeight()
	{
		SyncParentSplitOffset((int)base.CustomMinimumSize.Y);
	}

	private void SyncParentSplitOffset(int targetHeight)
	{
		if (GetParent() is VSplitContainer vSplitContainer)
		{
			vSplitContainer.SplitOffsets = new int[1] { -Mathf.Max(targetHeight, 32) };
		}
	}

	public void AddTab(Control content, string title)
	{
		_contentContainer.AddChild(content, forceReadableName: false, InternalMode.Disabled);
		_contentContainer.SetTabTitle(_contentContainer.GetTabCount() - 1, title);
	}
}
