using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorColor : XWInspectorPropertyEditorBase
{
	private static readonly Color[] PaletteColors = new Color[7]
	{
		new Color("f6f1d5"),
		new Color("252934"),
		new Color("ffd34d"),
		new Color("65c466"),
		new Color("58b7e8"),
		new Color("ef5b5b"),
		new Color("a97be8")
	};

	private static readonly string[] PaletteNames = new string[7] { "月光白", "深夜黑", "阳光金", "植物绿", "寒冰蓝", "警告红", "能量紫" };

	private ColorPickerButton _colorPickerButton;

	private Control _colorPreview;

	private Label _colorValueLabel;

	private Button[] _paletteButtons;

	private Color _comparisonColor = Colors.Transparent;

	private Color _currentColor = Colors.Transparent;

	private bool _updating;

	public override void _Ready()
	{
		base._Ready();
		_colorPickerButton = GetNode<ColorPickerButton>("%ColorPickerButton");
		_colorPreview = GetNode<Control>("%ColorPreview");
		_colorValueLabel = GetNode<Label>("%ColorValueLabel");
		_paletteButtons = new Button[PaletteColors.Length];
		_colorPickerButton.ColorChanged += ApplyColor;
		_colorPreview.Draw += DrawColorPreview;
		for (int i = 0; i < PaletteColors.Length; i++)
		{
			int paletteIndex = i;
			Button node = GetNode<Button>($"%PaletteButton{i}");
			node.TooltipText = PaletteNames[i] + "  #" + PaletteColors[i].ToHtml(includeAlpha: false).ToUpperInvariant();
			node.Pressed += delegate
			{
				ApplyPaletteColor(paletteIndex);
			};
			_paletteButtons[i] = node;
		}
		RefreshVisuals();
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		_comparisonColor = ReadColor();
		_currentColor = _comparisonColor;
		RefreshVisuals();
	}

	public override void UpdateValue()
	{
		_currentColor = ReadColor();
		RefreshVisuals();
	}

	public override Variant GetValue()
	{
		return _currentColor;
	}

	private Color ReadColor()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Color)
		{
			return Colors.Transparent;
		}
		return propertyValue.AsColor();
	}

	private void ApplyPaletteColor(int index)
	{
		if (index >= 0 && index < PaletteColors.Length)
		{
			Color color = PaletteColors[index];
			color.A = _currentColor.A;
			ApplyColor(color);
		}
	}

	private void ApplyColor(Color color)
	{
		if (!_updating && !_currentColor.IsEqualApprox(color))
		{
			_currentColor = color;
			ValueChange(color);
			RefreshVisuals();
		}
	}

	private void RefreshVisuals()
	{
		if (IsNodeReady())
		{
			_updating = true;
			_colorPickerButton.Color = _currentColor;
			_colorValueLabel.Text = $"#{_currentColor.ToHtml().ToUpperInvariant()}  ·  A {Mathf.RoundToInt(_currentColor.A * 100f)}%";
			_colorValueLabel.TooltipText = $"R {Mathf.RoundToInt(_currentColor.R * 255f)}   G {Mathf.RoundToInt(_currentColor.G * 255f)}   B {Mathf.RoundToInt(_currentColor.B * 255f)}   A {Mathf.RoundToInt(_currentColor.A * 255f)}";
			for (int i = 0; i < _paletteButtons.Length; i++)
			{
				ApplyPaletteStyle(_paletteButtons[i], PaletteColors[i], IsPaletteSelected(i));
			}
			_updating = false;
			_colorPreview.QueueRedraw();
		}
	}

	private bool IsPaletteSelected(int index)
	{
		Color color = PaletteColors[index];
		if (Mathf.IsEqualApprox(_currentColor.R, color.R) && Mathf.IsEqualApprox(_currentColor.G, color.G))
		{
			return Mathf.IsEqualApprox(_currentColor.B, color.B);
		}
		return false;
	}

	private static void ApplyPaletteStyle(Button button, Color color, bool selected)
	{
		if (GodotObject.IsInstanceValid(button))
		{
			button.AddThemeStyleboxOverride("normal", CreatePaletteStyle(color, selected ? new Color("ffd45a") : new Color("38445a"), (!selected) ? 1 : 3));
			button.AddThemeStyleboxOverride("hover", CreatePaletteStyle(color.Lightened(0.08f), new Color("e7eefc"), 2));
			button.AddThemeStyleboxOverride("pressed", CreatePaletteStyle(color.Darkened(0.1f), new Color("ffd45a"), 3));
		}
	}

	private static StyleBoxFlat CreatePaletteStyle(Color background, Color border, int width)
	{
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat();
		styleBoxFlat.BgColor = background;
		styleBoxFlat.BorderColor = border;
		styleBoxFlat.SetBorderWidthAll(width);
		styleBoxFlat.SetCornerRadiusAll(5);
		return styleBoxFlat;
	}

	private void DrawColorPreview()
	{
		if (GodotObject.IsInstanceValid(_colorPreview))
		{
			Rect2 rect = new Rect2(Vector2.Zero, _colorPreview.Size);
			DrawCheckerboard(rect);
			float num = Mathf.Floor(rect.Size.X * 0.34f);
			_colorPreview.DrawRect(new Rect2(0f, 0f, num, rect.Size.Y), _comparisonColor);
			_colorPreview.DrawRect(new Rect2(num, 0f, rect.Size.X - num, rect.Size.Y), _currentColor);
			_colorPreview.DrawLine(new Vector2(num, 0f), new Vector2(num, rect.Size.Y), new Color(1f, 1f, 1f, 0.34f), 1f);
			float width = Mathf.Max(0f, (rect.Size.X - 8f) * _currentColor.A);
			_colorPreview.DrawRect(new Rect2(4f, rect.Size.Y - 7f, rect.Size.X - 8f, 3f), new Color(0.04f, 0.05f, 0.08f, 0.82f));
			_colorPreview.DrawRect(new Rect2(4f, rect.Size.Y - 7f, width, 3f), new Color("ffd45a"));
			_colorPreview.DrawRect(rect.Grow(-0.5f), new Color("60708c"), filled: false, 1f);
		}
	}

	private void DrawCheckerboard(Rect2 rect)
	{
		Color color = new Color("252b38");
		Color color2 = new Color("465064");
		int num = Mathf.CeilToInt(rect.Size.X / 8f);
		int num2 = Mathf.CeilToInt(rect.Size.Y / 8f);
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Color color3 = ((((j + i) & 1) == 0) ? color : color2);
				_colorPreview.DrawRect(new Rect2((float)j * 8f, (float)i * 8f, 8f, 8f), color3);
			}
		}
	}
}
