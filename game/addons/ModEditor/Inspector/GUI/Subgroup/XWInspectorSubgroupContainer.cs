using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorSubgroupContainer : Container
{
	private static readonly string ScenePath = "res://addons/ModEditor/Inspector/GUI/Subgroup/XWInspectorSubgroupContainer.tscn";

	private const int HeaderMinHeight = 24;

	private const int HeaderVerticalPadding = 6;

	private static Texture2D _iconArrowClose;

	private static Texture2D _iconArrowOpen;

	private VBoxContainer _vbox;

	private VBoxContainer _editorContainer;

	public XWInspectorSubgroup Subgroup;

	private bool _isFolded;

	private bool _headerHover;

	private int _indentDepth = 1;

	private Color _bgColor = new Color(1f, 1f, 1f, 0.06f);

	private int _level = 1;

	public static XWInspectorSubgroupContainer Create()
	{
		return ResourceLoader.Load<PackedScene>(ScenePath, null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorSubgroupContainer>(PackedScene.GenEditState.Disabled);
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
		_vbox = GetNode<VBoxContainer>("%VBox");
		_editorContainer = GetNode<VBoxContainer>("%EditorContainer");
	}

	public void Init(XWInspectorSubgroup subgroup)
	{
		Subgroup = subgroup;
		subgroup.SubgroupContainer = this;
	}

	public void SetIndentDepth(int depth)
	{
		_indentDepth = Mathf.Max(depth, 1);
		QueueRedraw();
	}

	public void SetLevel(int level)
	{
		_level = level;
		_bgColor = new Color(1f, 1f, 1f, 0.06f);
		_bgColor.A /= _level;
		QueueRedraw();
	}

	public void AddEditor(XWInspectorPropertyEditorBase editor)
	{
		_editorContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
	}

	public void SetFolded(bool folded)
	{
		_isFolded = folded;
		if (GodotObject.IsInstanceValid(_editorContainer))
		{
			MarginContainer parentOrNull = _editorContainer.GetParentOrNull<MarginContainer>();
			if (GodotObject.IsInstanceValid(parentOrNull))
			{
				parentOrNull.Visible = !folded;
			}
		}
		QueueRedraw();
	}

	public bool IsFolded()
	{
		return _isFolded;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton inputEventMouseButton && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed)
		{
			int headerHeight = GetHeaderHeight();
			if (inputEventMouseButton.Position.Y < (float)headerHeight)
			{
				_isFolded = !_isFolded;
				SetFolded(_isFolded);
				_headerHover = false;
				QueueRedraw();
			}
		}
	}

	public override void _Notification(int what)
	{
		if ((long)what == 41)
		{
			_headerHover = true;
			QueueRedraw();
		}
		else if ((long)what == 42)
		{
			_headerHover = false;
			QueueRedraw();
		}
		else if ((long)what == 51)
		{
			ResortChildren();
		}
		else if ((long)what == 45)
		{
			QueueRedraw();
		}
	}

	private void ResortChildren()
	{
		if (GodotObject.IsInstanceValid(_vbox))
		{
			int headerHeight = GetHeaderHeight();
			int num = _indentDepth * 12;
			Vector2 size = base.Size;
			FitChildInRect(_vbox, new Rect2(new Vector2(num, headerHeight), new Vector2(size.X - (float)num, size.Y - (float)headerHeight)));
		}
	}

	private int GetHeaderHeight()
	{
		Font themeFont = GetThemeFont("font", "");
		int themeFontSize = GetThemeFontSize("font_size", "");
		int b = 24;
		if (GodotObject.IsInstanceValid(themeFont))
		{
			b = Mathf.CeilToInt(themeFont.GetHeight(themeFontSize)) + 6;
		}
		return Mathf.Max(24, b);
	}

	public override void _Draw()
	{
		int headerHeight = GetHeaderHeight();
		Vector2 size = base.Size;
		int num = _indentDepth * 12;
		Color color = _bgColor;
		color.A *= 0.4f;
		if (_headerHover)
		{
			color = color.Lightened(0.15f);
		}
		Rect2 rect = new Rect2(new Vector2(num, 0f), new Vector2(size.X - (float)num, headerHeight));
		DrawRect(rect, color);
		DrawRect(color: new Color(0.3f, 0.3f, 0.3f, 0.4f), rect: new Rect2(Vector2.Zero, new Vector2(num, size.Y)));
		Texture2D texture2D = ((!_isFolded) ? _iconArrowOpen : _iconArrowClose);
		if (GodotObject.IsInstanceValid(texture2D))
		{
			float x = num + 4;
			float y = (float)(headerHeight - texture2D.GetHeight()) / 2f;
			Color value = new Color(1f, 1f, 1f, _headerHover ? 1f : 0.85f);
			DrawTexture(texture2D, new Vector2(x, y), value);
		}
		Font themeFont = GetThemeFont("font", "");
		int themeFontSize = GetThemeFontSize("font_size", "");
		if (GodotObject.IsInstanceValid(themeFont) && GodotObject.IsInstanceValid(Subgroup))
		{
			float x2 = num + 4 + 20;
			float num2 = ((float)headerHeight - themeFont.GetHeight(themeFontSize)) / 2f;
			DrawString(modulate: new Color(0.85f, 0.85f, 0.85f), font: themeFont, pos: new Vector2(x2, num2 + themeFont.GetAscent(themeFontSize)), text: Subgroup.SubgroupName.ToString(), alignment: HorizontalAlignment.Left, width: -1f, fontSize: themeFontSize, justificationFlags: TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, direction: TextServer.Direction.Auto, orientation: TextServer.Orientation.Horizontal, oversampling: 0f);
		}
		if (!_isFolded || !GodotObject.IsInstanceValid(_editorContainer) || !GodotObject.IsInstanceValid(themeFont))
		{
			return;
		}
		int num3 = 0;
		foreach (Node child in _editorContainer.GetChildren())
		{
			if (child is XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase.ResetButton) && xWInspectorPropertyEditorBase.ResetButton.Visible)
			{
				num3++;
			}
		}
		if (num3 > 0)
		{
			string text = $"({num3})";
			int fontSize = themeFontSize - 2;
			float x3 = themeFont.GetStringSize(text, HorizontalAlignment.Left, -1f, fontSize, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
			float x4 = size.X - x3 - 8f;
			float num4 = ((float)headerHeight - themeFont.GetHeight(fontSize)) / 2f;
			DrawString(themeFont, new Vector2(x4, num4 + themeFont.GetAscent(fontSize)), text, HorizontalAlignment.Left, -1f, fontSize, new Color(0.6f, 0.6f, 0.6f, 0.8f), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}

	public override Vector2 _GetMinimumSize()
	{
		Vector2 zero = Vector2.Zero;
		int headerHeight = GetHeaderHeight();
		zero.Y = headerHeight;
		if (!_isFolded && GodotObject.IsInstanceValid(_vbox))
		{
			Vector2 combinedMinimumSize = _vbox.GetCombinedMinimumSize();
			zero.Y += combinedMinimumSize.Y;
			zero.X = Mathf.Max(zero.X, combinedMinimumSize.X + (float)(_indentDepth * 12));
		}
		return zero;
	}
}
