using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorCurveTexture : XWInspectorPropertyEditorBase
{
	private TextureRect _curveTexturePreview;

	private XWResourcePicker _curvePicker;

	private XWResourcePicker _curveXPicker;

	private XWResourcePicker _curveYPicker;

	private XWResourcePicker _curveZPicker;

	private SpinBox _widthSpinBox;

	private OptionButton _textureModeOption;

	private Control _singleCurveControls;

	private Control _xyzCurveControls;

	private Texture2D _texture;

	private bool _updating;

	public override void _Ready()
	{
		base._Ready();
		_curveTexturePreview = GetNode<TextureRect>("%CurveTexturePreview");
		_curvePicker = GetNode<XWResourcePicker>("%CurvePicker");
		_curveXPicker = GetNode<XWResourcePicker>("%CurveXPicker");
		_curveYPicker = GetNode<XWResourcePicker>("%CurveYPicker");
		_curveZPicker = GetNode<XWResourcePicker>("%CurveZPicker");
		_widthSpinBox = GetNode<SpinBox>("%WidthSpinBox");
		_textureModeOption = GetNode<OptionButton>("%TextureModeOption");
		_singleCurveControls = GetNode<Control>("%SingleCurveControls");
		_xyzCurveControls = GetNode<Control>("%XyzCurveControls");
		SetupOptions();
		SetupPicker(_curvePicker);
		SetupPicker(_curveXPicker);
		SetupPicker(_curveYPicker);
		SetupPicker(_curveZPicker);
		_curvePicker.ResourceChanged += OnCurveChanged;
		_curveXPicker.ResourceChanged += delegate(Resource resource)
		{
			OnCurveChanged("x", resource);
		};
		_curveYPicker.ResourceChanged += delegate(Resource resource)
		{
			OnCurveChanged("y", resource);
		};
		_curveZPicker.ResourceChanged += delegate(Resource resource)
		{
			OnCurveChanged("z", resource);
		};
		_widthSpinBox.ValueChanged += OnWidthChanged;
		_textureModeOption.ItemSelected += OnTextureModeSelected;
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
		_textureModeOption.Clear();
		_textureModeOption.AddItem("RGB", 0);
		_textureModeOption.AddItem("Red", 1);
	}

	private static void SetupPicker(XWResourcePicker picker)
	{
		picker.Setup("Curve");
	}

	private void RefreshTexture()
	{
		_texture = ReadTexture();
		_updating = true;
		bool flag = _texture is CurveTexture;
		bool flag2 = _texture is CurveXyzTexture;
		_curveTexturePreview.Texture = (GodotObject.IsInstanceValid(_texture) ? _texture : null);
		_singleCurveControls.Visible = flag;
		_xyzCurveControls.Visible = flag2;
		_widthSpinBox.Editable = flag || flag2;
		_textureModeOption.Disabled = !flag;
		if (_texture is CurveTexture curveTexture)
		{
			_widthSpinBox.Value = curveTexture.Width;
			_textureModeOption.Select(GetItemIndexById(_textureModeOption, (int)curveTexture.TextureMode));
			_curvePicker.SetEditedResource(curveTexture.Curve);
		}
		else if (_texture is CurveXyzTexture curveXyzTexture)
		{
			_widthSpinBox.Value = curveXyzTexture.Width;
			_curveXPicker.SetEditedResource(curveXyzTexture.CurveX);
			_curveYPicker.SetEditedResource(curveXyzTexture.CurveY);
			_curveZPicker.SetEditedResource(curveXyzTexture.CurveZ);
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

	private void OnCurveChanged(Resource resource)
	{
		if (!_updating && _texture is CurveTexture curveTexture)
		{
			curveTexture.Curve = resource as Curve;
			CommitTextureChange();
		}
	}

	private void OnCurveChanged(string channel, Resource resource)
	{
		if (!_updating && _texture is CurveXyzTexture curveXyzTexture)
		{
			Curve curve = resource as Curve;
			switch (channel)
			{
			case "x":
				curveXyzTexture.CurveX = curve;
				break;
			case "y":
				curveXyzTexture.CurveY = curve;
				break;
			case "z":
				curveXyzTexture.CurveZ = curve;
				break;
			}
			CommitTextureChange();
		}
	}

	private void OnWidthChanged(double value)
	{
		if (!_updating && GodotObject.IsInstanceValid(_texture))
		{
			int width = Mathf.Max(1, (int)value);
			if (_texture is CurveTexture curveTexture)
			{
				curveTexture.Width = width;
			}
			else if (_texture is CurveXyzTexture curveXyzTexture)
			{
				curveXyzTexture.Width = width;
			}
			CommitTextureChange();
		}
	}

	private void OnTextureModeSelected(long index)
	{
		if (!_updating && _texture is CurveTexture curveTexture)
		{
			curveTexture.TextureMode = (CurveTexture.TextureModeEnum)_textureModeOption.GetItemId((int)index);
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
			_curveTexturePreview.Texture = _texture;
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
