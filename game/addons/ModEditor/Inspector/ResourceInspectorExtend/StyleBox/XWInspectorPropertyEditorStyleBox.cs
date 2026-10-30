using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorStyleBox : XWInspectorPropertyEditorBase
{
	private Control _styleBoxPreview;

	private VBoxContainer _flatControls;

	private VBoxContainer _textureControls;

	private VBoxContainer _lineControls;

	private ColorPickerButton _bgColorPicker;

	private ColorPickerButton _borderColorPicker;

	private ColorPickerButton _lineColorPicker;

	private ColorPickerButton _modulateColorPicker;

	private XWResourcePicker _texturePicker;

	private SpinBox _borderWidthSpinBox;

	private SpinBox _cornerRadiusSpinBox;

	private SpinBox _textureMarginSpinBox;

	private SpinBox _thicknessSpinBox;

	private CheckBox _drawCenterCheckBox;

	private CheckBox _verticalCheckBox;

	private StyleBox _styleBox;

	private bool _updating;

	public override void _Ready()
	{
		base._Ready();
		_styleBoxPreview = GetNode<Control>("%StyleBoxPreview");
		_flatControls = GetNode<VBoxContainer>("%FlatControls");
		_textureControls = GetNode<VBoxContainer>("%TextureControls");
		_lineControls = GetNode<VBoxContainer>("%LineControls");
		_bgColorPicker = GetNode<ColorPickerButton>("%BgColorPicker");
		_borderColorPicker = GetNode<ColorPickerButton>("%BorderColorPicker");
		_lineColorPicker = GetNode<ColorPickerButton>("%LineColorPicker");
		_modulateColorPicker = GetNode<ColorPickerButton>("%ModulateColorPicker");
		_texturePicker = GetNode<XWResourcePicker>("%TexturePicker");
		_borderWidthSpinBox = GetNode<SpinBox>("%BorderWidthSpinBox");
		_cornerRadiusSpinBox = GetNode<SpinBox>("%CornerRadiusSpinBox");
		_textureMarginSpinBox = GetNode<SpinBox>("%TextureMarginSpinBox");
		_thicknessSpinBox = GetNode<SpinBox>("%ThicknessSpinBox");
		_drawCenterCheckBox = GetNode<CheckBox>("%DrawCenterCheckBox");
		_verticalCheckBox = GetNode<CheckBox>("%VerticalCheckBox");
		_texturePicker.Setup("Texture2D");
		_styleBoxPreview.Draw += OnDrawStyleBoxPreview;
		_bgColorPicker.ColorChanged += OnBgColorChanged;
		_borderColorPicker.ColorChanged += OnBorderColorChanged;
		_lineColorPicker.ColorChanged += OnLineColorChanged;
		_modulateColorPicker.ColorChanged += OnModulateColorChanged;
		_texturePicker.ResourceChanged += OnTextureChanged;
		_borderWidthSpinBox.ValueChanged += OnBorderWidthChanged;
		_cornerRadiusSpinBox.ValueChanged += OnCornerRadiusChanged;
		_textureMarginSpinBox.ValueChanged += OnTextureMarginChanged;
		_thicknessSpinBox.ValueChanged += OnThicknessChanged;
		_drawCenterCheckBox.Toggled += OnDrawCenterToggled;
		_verticalCheckBox.Toggled += OnVerticalToggled;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		RefreshStyleBox();
	}

	public override void UpdateValue()
	{
		RefreshStyleBox();
	}

	public override Variant GetValue()
	{
		return _styleBox;
	}

	private void RefreshStyleBox()
	{
		_styleBox = ReadStyleBox();
		_updating = true;
		bool visible = _styleBox is StyleBoxFlat;
		bool visible2 = _styleBox is StyleBoxTexture;
		bool visible3 = _styleBox is StyleBoxLine;
		_flatControls.Visible = visible;
		_textureControls.Visible = visible2;
		_lineControls.Visible = visible3;
		if (_styleBox is StyleBoxFlat styleBoxFlat)
		{
			_bgColorPicker.Color = styleBoxFlat.BgColor;
			_borderColorPicker.Color = styleBoxFlat.BorderColor;
			_borderWidthSpinBox.Value = styleBoxFlat.BorderWidthLeft;
			_cornerRadiusSpinBox.Value = styleBoxFlat.CornerRadiusTopLeft;
			_drawCenterCheckBox.ButtonPressed = styleBoxFlat.DrawCenter;
		}
		else if (_styleBox is StyleBoxTexture styleBoxTexture)
		{
			_texturePicker.SetEditedResource(styleBoxTexture.Texture);
			_textureMarginSpinBox.Value = styleBoxTexture.TextureMarginLeft;
			_modulateColorPicker.Color = styleBoxTexture.ModulateColor;
			_drawCenterCheckBox.ButtonPressed = styleBoxTexture.DrawCenter;
		}
		else if (_styleBox is StyleBoxLine styleBoxLine)
		{
			_lineColorPicker.Color = styleBoxLine.Color;
			_thicknessSpinBox.Value = styleBoxLine.Thickness;
			_verticalCheckBox.ButtonPressed = styleBoxLine.Vertical;
		}
		_updating = false;
		_styleBoxPreview.QueueRedraw();
	}

	private StyleBox ReadStyleBox()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Object || !(propertyValue.As<GodotObject>() is StyleBox result))
		{
			return null;
		}
		return result;
	}

	private void OnDrawStyleBoxPreview()
	{
		Rect2 rect = new Rect2(Vector2.Zero, _styleBoxPreview.Size);
		StyleBoxFlat styleBoxFlat = new StyleBoxFlat
		{
			BgColor = new Color(0.08f, 0.08f, 0.08f)
		};
		styleBoxFlat.SetCornerRadiusAll(3);
		_styleBoxPreview.DrawStyleBox(styleBoxFlat, rect);
		if (GodotObject.IsInstanceValid(_styleBox))
		{
			_styleBoxPreview.DrawStyleBox(_styleBox, rect.Grow(-10f));
		}
	}

	private void OnBgColorChanged(Color color)
	{
		if (!_updating && _styleBox is StyleBoxFlat styleBoxFlat)
		{
			styleBoxFlat.BgColor = color;
			CommitStyleBoxChange();
		}
	}

	private void OnBorderColorChanged(Color color)
	{
		if (!_updating && _styleBox is StyleBoxFlat styleBoxFlat)
		{
			styleBoxFlat.BorderColor = color;
			CommitStyleBoxChange();
		}
	}

	private void OnLineColorChanged(Color color)
	{
		if (!_updating && _styleBox is StyleBoxLine styleBoxLine)
		{
			styleBoxLine.Color = color;
			CommitStyleBoxChange();
		}
	}

	private void OnModulateColorChanged(Color color)
	{
		if (!_updating && _styleBox is StyleBoxTexture styleBoxTexture)
		{
			styleBoxTexture.ModulateColor = color;
			CommitStyleBoxChange();
		}
	}

	private void OnTextureChanged(Resource resource)
	{
		if (!_updating && _styleBox is StyleBoxTexture styleBoxTexture)
		{
			styleBoxTexture.Texture = resource as Texture2D;
			CommitStyleBoxChange();
		}
	}

	private void OnBorderWidthChanged(double value)
	{
		if (!_updating && _styleBox is StyleBoxFlat styleBoxFlat)
		{
			styleBoxFlat.SetBorderWidthAll(Mathf.Max(0, (int)value));
			CommitStyleBoxChange();
		}
	}

	private void OnCornerRadiusChanged(double value)
	{
		if (!_updating && _styleBox is StyleBoxFlat styleBoxFlat)
		{
			styleBoxFlat.SetCornerRadiusAll(Mathf.Max(0, (int)value));
			CommitStyleBoxChange();
		}
	}

	private void OnTextureMarginChanged(double value)
	{
		if (!_updating && _styleBox is StyleBoxTexture styleBoxTexture)
		{
			float textureMarginBottom = (styleBoxTexture.TextureMarginRight = (styleBoxTexture.TextureMarginTop = (styleBoxTexture.TextureMarginLeft = Mathf.Max(0f, (float)value))));
			styleBoxTexture.TextureMarginBottom = textureMarginBottom;
			CommitStyleBoxChange();
		}
	}

	private void OnThicknessChanged(double value)
	{
		if (!_updating && _styleBox is StyleBoxLine styleBoxLine)
		{
			styleBoxLine.Thickness = Mathf.Max(0, (int)value);
			CommitStyleBoxChange();
		}
	}

	private void OnDrawCenterToggled(bool toggled)
	{
		if (_updating)
		{
			return;
		}
		if (_styleBox is StyleBoxFlat styleBoxFlat)
		{
			styleBoxFlat.DrawCenter = toggled;
		}
		else
		{
			if (!(_styleBox is StyleBoxTexture styleBoxTexture))
			{
				return;
			}
			styleBoxTexture.DrawCenter = toggled;
		}
		CommitStyleBoxChange();
	}

	private void OnVerticalToggled(bool toggled)
	{
		if (!_updating && _styleBox is StyleBoxLine styleBoxLine)
		{
			styleBoxLine.Vertical = toggled;
			CommitStyleBoxChange();
		}
	}

	private void CommitStyleBoxChange()
	{
		if (GodotObject.IsInstanceValid(_styleBox))
		{
			_styleBox.EmitChanged();
			if (GodotObject.IsInstanceValid(Property) && Property.PropName != (StringName)"")
			{
				ValueChange(_styleBox);
			}
			else
			{
				Property?.SetCall();
			}
			_styleBoxPreview.QueueRedraw();
		}
	}
}
