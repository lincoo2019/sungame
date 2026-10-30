using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorGradientTexture : XWInspectorPropertyEditorBase
{
	private TextureRect _gradientTexturePreview;

	private XWResourcePicker _gradientPicker;

	private SpinBox _widthSpinBox;

	private SpinBox _heightSpinBox;

	private CheckBox _useHdrCheckBox;

	private OptionButton _fillOption;

	private OptionButton _repeatOption;

	private SpinBox _fillFromXSpinBox;

	private SpinBox _fillFromYSpinBox;

	private SpinBox _fillToXSpinBox;

	private SpinBox _fillToYSpinBox;

	private Control _texture2DControls;

	private Texture2D _texture;

	private bool _updating;

	public override void _Ready()
	{
		base._Ready();
		_gradientTexturePreview = GetNode<TextureRect>("%GradientTexturePreview");
		_gradientPicker = GetNode<XWResourcePicker>("%GradientPicker");
		_widthSpinBox = GetNode<SpinBox>("%WidthSpinBox");
		_heightSpinBox = GetNode<SpinBox>("%HeightSpinBox");
		_useHdrCheckBox = GetNode<CheckBox>("%UseHdrCheckBox");
		_fillOption = GetNode<OptionButton>("%FillOption");
		_repeatOption = GetNode<OptionButton>("%RepeatOption");
		_fillFromXSpinBox = GetNode<SpinBox>("%FillFromXSpinBox");
		_fillFromYSpinBox = GetNode<SpinBox>("%FillFromYSpinBox");
		_fillToXSpinBox = GetNode<SpinBox>("%FillToXSpinBox");
		_fillToYSpinBox = GetNode<SpinBox>("%FillToYSpinBox");
		_texture2DControls = GetNode<Control>("%Texture2DControls");
		SetupOptions();
		_gradientPicker.Setup("Gradient");
		_gradientPicker.ResourceChanged += OnGradientChanged;
		_widthSpinBox.ValueChanged += OnWidthChanged;
		_heightSpinBox.ValueChanged += OnHeightChanged;
		_useHdrCheckBox.Toggled += OnUseHdrToggled;
		_fillOption.ItemSelected += OnFillSelected;
		_repeatOption.ItemSelected += OnRepeatSelected;
		_fillFromXSpinBox.ValueChanged += delegate
		{
			OnFillVectorChanged();
		};
		_fillFromYSpinBox.ValueChanged += delegate
		{
			OnFillVectorChanged();
		};
		_fillToXSpinBox.ValueChanged += delegate
		{
			OnFillVectorChanged();
		};
		_fillToYSpinBox.ValueChanged += delegate
		{
			OnFillVectorChanged();
		};
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		RefreshTexture();
	}

	public override void UpdateValue()
	{
		RefreshTexture();
	}

	public override Variant GetValue()
	{
		return _texture;
	}

	private void SetupOptions()
	{
		_fillOption.Clear();
		_fillOption.AddItem("线性", 0);
		_fillOption.AddItem("径向", 1);
		_fillOption.AddItem("方形", 2);
		_fillOption.AddItem("锥形", 3);
		_repeatOption.Clear();
		_repeatOption.AddItem("不重复", 0);
		_repeatOption.AddItem("重复", 1);
		_repeatOption.AddItem("镜像", 2);
	}

	private void RefreshTexture()
	{
		_texture = ReadTexture();
		_updating = true;
		bool flag = GodotObject.IsInstanceValid(_texture);
		bool flag2 = _texture is GradientTexture1D;
		bool flag3 = _texture is GradientTexture2D;
		_gradientTexturePreview.Texture = (flag ? _texture : null);
		_gradientPicker.SetEditedResource(ReadGradient());
		_widthSpinBox.Editable = flag2 || flag3;
		_heightSpinBox.Editable = flag3;
		_useHdrCheckBox.Disabled = !(flag2 || flag3);
		_fillOption.Disabled = !flag3;
		_repeatOption.Disabled = !flag3;
		_fillFromXSpinBox.Editable = flag3;
		_fillFromYSpinBox.Editable = flag3;
		_fillToXSpinBox.Editable = flag3;
		_fillToYSpinBox.Editable = flag3;
		_texture2DControls.Visible = flag3;
		if (_texture is GradientTexture1D gradientTexture1D)
		{
			_widthSpinBox.Value = gradientTexture1D.Width;
			_heightSpinBox.Value = 1.0;
			_useHdrCheckBox.ButtonPressed = gradientTexture1D.UseHdr;
		}
		else if (_texture is GradientTexture2D gradientTexture2D)
		{
			_widthSpinBox.Value = gradientTexture2D.Width;
			_heightSpinBox.Value = gradientTexture2D.Height;
			_useHdrCheckBox.ButtonPressed = gradientTexture2D.UseHdr;
			_fillOption.Select(GetItemIndexById(_fillOption, (int)gradientTexture2D.Fill));
			_repeatOption.Select(GetItemIndexById(_repeatOption, (int)gradientTexture2D.Repeat));
			_fillFromXSpinBox.Value = gradientTexture2D.FillFrom.X;
			_fillFromYSpinBox.Value = gradientTexture2D.FillFrom.Y;
			_fillToXSpinBox.Value = gradientTexture2D.FillTo.X;
			_fillToYSpinBox.Value = gradientTexture2D.FillTo.Y;
		}
		_updating = false;
	}

	private Texture2D ReadTexture()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Object || !(propertyValue.As<GodotObject>() is Texture2D result))
		{
			return null;
		}
		return result;
	}

	private Gradient ReadGradient()
	{
		Texture2D texture = _texture;
		if (!(texture is GradientTexture1D { Gradient: var gradient }))
		{
			if (!(texture is GradientTexture2D { Gradient: var gradient2 }))
			{
				return null;
			}
			return gradient2;
		}
		return gradient;
	}

	private void OnGradientChanged(Resource resource)
	{
		if (!_updating && GodotObject.IsInstanceValid(_texture))
		{
			if (_texture is GradientTexture1D gradientTexture1D)
			{
				gradientTexture1D.Gradient = resource as Gradient;
			}
			else if (_texture is GradientTexture2D gradientTexture2D)
			{
				gradientTexture2D.Gradient = resource as Gradient;
			}
			CommitTextureChange();
		}
	}

	private void OnWidthChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_texture))
		{
			int width = Mathf.Max(1, (int)value);
			if (_texture is GradientTexture1D gradientTexture1D)
			{
				gradientTexture1D.Width = width;
			}
			else if (_texture is GradientTexture2D gradientTexture2D)
			{
				gradientTexture2D.Width = width;
			}
			CommitTextureChange();
		}
	}

	private void OnHeightChanged(double value)
	{
		if (!_updating && _texture is GradientTexture2D gradientTexture2D)
		{
			gradientTexture2D.Height = Mathf.Max(1, (int)value);
			CommitTextureChange();
		}
	}

	private void OnUseHdrToggled(bool toggled)
	{
		if (!_updating && GodotObject.IsInstanceValid(_texture))
		{
			if (_texture is GradientTexture1D gradientTexture1D)
			{
				gradientTexture1D.UseHdr = toggled;
			}
			else if (_texture is GradientTexture2D gradientTexture2D)
			{
				gradientTexture2D.UseHdr = toggled;
			}
			CommitTextureChange();
		}
	}

	private void OnFillSelected(long index)
	{
		if (!_updating && _texture is GradientTexture2D gradientTexture2D)
		{
			gradientTexture2D.Fill = (GradientTexture2D.FillEnum)_fillOption.GetItemId((int)index);
			CommitTextureChange();
		}
	}

	private void OnRepeatSelected(long index)
	{
		if (!_updating && _texture is GradientTexture2D gradientTexture2D)
		{
			gradientTexture2D.Repeat = (GradientTexture2D.RepeatEnum)_repeatOption.GetItemId((int)index);
			CommitTextureChange();
		}
	}

	private void OnFillVectorChanged()
	{
		if (!_updating && _texture is GradientTexture2D gradientTexture2D)
		{
			gradientTexture2D.FillFrom = new Vector2((float)_fillFromXSpinBox.Value, (float)_fillFromYSpinBox.Value);
			gradientTexture2D.FillTo = new Vector2((float)_fillToXSpinBox.Value, (float)_fillToYSpinBox.Value);
			CommitTextureChange();
		}
	}

	private void CommitTextureChange()
	{
		if (GodotObject.IsInstanceValid(_texture))
		{
			_texture.EmitChanged();
			if (GodotObject.IsInstanceValid(Property) && Property.PropName != (StringName)"")
			{
				ValueChange(_texture);
			}
			else
			{
				Property?.SetCall();
			}
			_gradientTexturePreview.Texture = _texture;
		}
	}

	private static int GetItemIndexById(OptionButton option, int id)
	{
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemId(i) == id)
			{
				return i;
			}
		}
		return 0;
	}
}
