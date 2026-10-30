using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorGroupContainer : VBoxContainer
{
	private static readonly string ScenePath = "res://addons/ModEditor/Inspector/GUI/Group/XWInspectorGroupContainer.tscn";

	private const int HeaderMinHeight = 18;

	private const int HeaderHorizontalMargin = 4;

	private static Texture2D _iconArrowClose;

	private static Texture2D _iconArrowOpen;

	private Label _groupNameLabel;

	public VBoxContainer EditorContainer;

	public XWInspectorGroup Group;

	private bool _isFolded;

	private Button _headerButton;

	private Label _changeCountLabel;

	private bool _headerHover;

	private PanelContainer _panelContainer;

	public static XWInspectorGroupContainer Create()
	{
		return ResourceLoader.Load<PackedScene>(ScenePath, null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorGroupContainer>(PackedScene.GenEditState.Disabled);
	}

	private static void LoadIcons()
	{
		_iconArrowClose = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GuiTreeArrowRight.svg", null, ResourceLoader.CacheMode.Reuse));
		_iconArrowOpen = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GuiTreeArrowDown.svg", null, ResourceLoader.CacheMode.Reuse));
	}

	public override void _Ready()
	{
		if (_iconArrowOpen == null)
		{
			LoadIcons();
		}
		_groupNameLabel = GetNode<Label>("%GroupNameLabel");
		EditorContainer = GetNode<VBoxContainer>("%EditorContainer");
		SetupFoldButton();
		_panelContainer = GetNodeOrNull<PanelContainer>("PanelContainer");
		if (GodotObject.IsInstanceValid(_panelContainer))
		{
			_panelContainer.CustomMinimumSize = new Vector2(0f, 18f);
			ApplyHeaderPanelStyle(hover: false);
			_panelContainer.GuiInput += OnHeaderGuiInput;
			_panelContainer.MouseEntered += OnHeaderMouseEntered;
			_panelContainer.MouseExited += OnHeaderMouseExited;
		}
	}

	private void SetupFoldButton()
	{
		PanelContainer nodeOrNull = GetNodeOrNull<PanelContainer>("PanelContainer");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			_headerButton = new Button
			{
				Name = "FoldButton",
				Flat = true,
				Icon = _iconArrowOpen,
				CustomMinimumSize = new Vector2(14f, 14f)
			};
			_headerButton.Pressed += OnFoldButtonPressed;
			_changeCountLabel = new Label
			{
				Name = "ChangeCount"
			};
			_changeCountLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f, 0.8f));
			_changeCountLabel.AddThemeFontSizeOverride("font_size", 11);
			_changeCountLabel.Visible = false;
			HBoxContainer nodeOrNull2 = nodeOrNull.GetNodeOrNull<HBoxContainer>("HBoxContainer");
			if (GodotObject.IsInstanceValid(nodeOrNull2))
			{
				nodeOrNull2.AddChild(_headerButton, forceReadableName: false, InternalMode.Disabled);
				nodeOrNull2.MoveChild(_headerButton, 0);
				nodeOrNull2.AddChild(_changeCountLabel, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	public void Init(XWInspectorGroup group)
	{
		Group = group;
		SetGroupName(group.GroupName.ToString());
		group.GroupContainer = this;
	}

	public void SetGroupName(string groupName)
	{
		if (GodotObject.IsInstanceValid(_groupNameLabel))
		{
			_groupNameLabel.Text = groupName;
		}
	}

	public void AddEditor(XWInspectorPropertyEditorBase editor)
	{
		EditorContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
	}

	private void OnFoldButtonPressed()
	{
		_isFolded = !_isFolded;
		EditorContainer.Visible = !_isFolded;
		if (GodotObject.IsInstanceValid(_headerButton))
		{
			_headerButton.Icon = ((!_isFolded) ? _iconArrowOpen : _iconArrowClose);
		}
		UpdateChangeCount();
	}

	public void SetFolded(bool folded)
	{
		_isFolded = folded;
		if (GodotObject.IsInstanceValid(EditorContainer))
		{
			EditorContainer.Visible = !folded;
		}
		if (GodotObject.IsInstanceValid(_headerButton))
		{
			_headerButton.Icon = ((!folded) ? _iconArrowOpen : _iconArrowClose);
		}
		UpdateChangeCount();
	}

	public bool IsFolded()
	{
		return _isFolded;
	}

	private void UpdateChangeCount()
	{
		if (!GodotObject.IsInstanceValid(_changeCountLabel))
		{
			return;
		}
		if (!_isFolded)
		{
			_changeCountLabel.Visible = false;
			return;
		}
		int num = 0;
		foreach (Node child in EditorContainer.GetChildren())
		{
			if (child is XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase)
			{
				if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase.ResetButton) && xWInspectorPropertyEditorBase.ResetButton.Visible)
				{
					num++;
				}
			}
			else if (child is XWInspectorSubgroupContainer subgroup)
			{
				num += CountRevertableInSubgroup(subgroup);
			}
		}
		if (num > 0)
		{
			_changeCountLabel.Text = $"({num} 项变更)";
			_changeCountLabel.Visible = true;
		}
		else
		{
			_changeCountLabel.Visible = false;
		}
	}

	private static int CountRevertableInSubgroup(XWInspectorSubgroupContainer subgroup)
	{
		int num = 0;
		if (!GodotObject.IsInstanceValid(subgroup))
		{
			return 0;
		}
		VBoxContainer nodeOrNull = subgroup.GetNodeOrNull<VBoxContainer>("%EditorContainer");
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			return 0;
		}
		foreach (Node child in nodeOrNull.GetChildren())
		{
			if (child is XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase.ResetButton) && xWInspectorPropertyEditorBase.ResetButton.Visible)
			{
				num++;
			}
		}
		return num;
	}

	private void OnHeaderGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed)
		{
			OnFoldButtonPressed();
		}
	}

	private void OnHeaderMouseEntered()
	{
		_headerHover = true;
		ApplyHeaderPanelStyle(hover: true);
	}

	private void OnHeaderMouseExited()
	{
		_headerHover = false;
		if (GodotObject.IsInstanceValid(_panelContainer))
		{
			ApplyHeaderPanelStyle(hover: false);
		}
	}

	private void ApplyHeaderPanelStyle(bool hover)
	{
		if (GodotObject.IsInstanceValid(_panelContainer))
		{
			_panelContainer.AddThemeStyleboxOverride("panel", CreateHeaderPanelStyle(hover));
		}
	}

	private static StyleBoxFlat CreateHeaderPanelStyle(bool hover)
	{
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat();
		styleBoxFlat.BgColor = (hover ? new Color(0.392f, 0.392f, 0.392f, 0.6f) : new Color(0.363f, 0.363f, 0.363f, 0.6f));
		styleBoxFlat.CornerRadiusTopLeft = 3;
		styleBoxFlat.CornerRadiusTopRight = 3;
		styleBoxFlat.CornerRadiusBottomRight = 3;
		styleBoxFlat.CornerRadiusBottomLeft = 3;
		styleBoxFlat.CornerDetail = 5;
		styleBoxFlat.SetContentMargin(Side.Left, 4f);
		styleBoxFlat.SetContentMargin(Side.Top, 0f);
		styleBoxFlat.SetContentMargin(Side.Right, 4f);
		styleBoxFlat.SetContentMargin(Side.Bottom, 0f);
		return styleBoxFlat;
	}
}
