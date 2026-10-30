using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorMaterial : XWInspectorPropertyEditorBase
{
	private Control _materialPreview;

	private VBoxContainer _shaderControls;

	private VBoxContainer _canvasControls;

	private VBoxContainer _base3DControls;

	private XWResourcePicker _shaderPicker;

	private Tree _shaderParamTree;

	private OptionButton _blendModeOption;

	private OptionButton _lightModeOption;

	private CheckBox _particlesAnimationCheckBox;

	private CheckBox _particlesAnimLoopCheckBox;

	private SpinBox _particlesHFramesSpinBox;

	private SpinBox _particlesVFramesSpinBox;

	private ColorPickerButton _albedoColorPicker;

	private SpinBox _metallicSpinBox;

	private SpinBox _roughnessSpinBox;

	private Material _material;

	private bool _updating;

	public override void _Ready()
	{
		base._Ready();
		_materialPreview = GetNode<Control>("%MaterialPreview");
		_shaderControls = GetNode<VBoxContainer>("%ShaderControls");
		_canvasControls = GetNode<VBoxContainer>("%CanvasControls");
		_base3DControls = GetNode<VBoxContainer>("%Base3DControls");
		_shaderPicker = GetNode<XWResourcePicker>("%ShaderPicker");
		_shaderParamTree = GetNode<Tree>("%ShaderParamTree");
		_blendModeOption = GetNode<OptionButton>("%BlendModeOption");
		_lightModeOption = GetNode<OptionButton>("%LightModeOption");
		_particlesAnimationCheckBox = GetNode<CheckBox>("%ParticlesAnimationCheckBox");
		_particlesAnimLoopCheckBox = GetNode<CheckBox>("%ParticlesAnimLoopCheckBox");
		_particlesHFramesSpinBox = GetNode<SpinBox>("%ParticlesHFramesSpinBox");
		_particlesVFramesSpinBox = GetNode<SpinBox>("%ParticlesVFramesSpinBox");
		_albedoColorPicker = GetNode<ColorPickerButton>("%AlbedoColorPicker");
		_metallicSpinBox = GetNode<SpinBox>("%MetallicSpinBox");
		_roughnessSpinBox = GetNode<SpinBox>("%RoughnessSpinBox");
		SetupOptions();
		_shaderPicker.Setup("Shader");
		_shaderParamTree.SetColumnTitle(0, "参数");
		_shaderParamTree.SetColumnTitle(1, "类型");
		_shaderParamTree.SetColumnTitle(2, "值");
		_materialPreview.Draw += OnDrawMaterialPreview;
		_shaderPicker.ResourceChanged += OnShaderChanged;
		_shaderParamTree.ItemActivated += ReapplySelectedShaderParameter;
		_blendModeOption.ItemSelected += OnBlendModeSelected;
		_lightModeOption.ItemSelected += OnLightModeSelected;
		_particlesAnimationCheckBox.Toggled += OnParticlesAnimationToggled;
		_particlesAnimLoopCheckBox.Toggled += OnParticlesAnimLoopToggled;
		_particlesHFramesSpinBox.ValueChanged += OnParticlesHFramesChanged;
		_particlesVFramesSpinBox.ValueChanged += OnParticlesVFramesChanged;
		_albedoColorPicker.ColorChanged += OnAlbedoColorChanged;
		_metallicSpinBox.ValueChanged += OnMetallicChanged;
		_roughnessSpinBox.ValueChanged += OnRoughnessChanged;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		RefreshMaterial();
	}

	public override void UpdateValue()
	{
		RefreshMaterial();
	}

	public override Variant GetValue()
	{
		return _material;
	}

	private void SetupOptions()
	{
		_blendModeOption.Clear();
		_blendModeOption.AddItem("混合", 0);
		_blendModeOption.AddItem("叠加", 1);
		_blendModeOption.AddItem("相减", 2);
		_blendModeOption.AddItem("相乘", 3);
		_blendModeOption.AddItem("预乘 Alpha", 4);
		_lightModeOption.Clear();
		_lightModeOption.AddItem("正常", 0);
		_lightModeOption.AddItem("无光照", 1);
		_lightModeOption.AddItem("仅光照", 2);
	}

	private void RefreshMaterial()
	{
		_material = ReadMaterial();
		_updating = true;
		bool visible = _material is ShaderMaterial;
		bool visible2 = _material is CanvasItemMaterial;
		bool visible3 = _material is BaseMaterial3D;
		_shaderControls.Visible = visible;
		_canvasControls.Visible = visible2;
		_base3DControls.Visible = visible3;
		if (_material is ShaderMaterial shaderMaterial)
		{
			_shaderPicker.SetEditedResource(shaderMaterial.Shader);
			PopulateShaderParamTree(shaderMaterial);
		}
		else
		{
			_shaderPicker.SetEditedResource(null);
			_shaderParamTree.Clear();
		}
		if (_material is CanvasItemMaterial canvasItemMaterial)
		{
			_blendModeOption.Select(GetItemIndexById(_blendModeOption, (int)canvasItemMaterial.BlendMode));
			_lightModeOption.Select(GetItemIndexById(_lightModeOption, (int)canvasItemMaterial.LightMode));
			_particlesAnimationCheckBox.ButtonPressed = canvasItemMaterial.ParticlesAnimation;
			_particlesAnimLoopCheckBox.ButtonPressed = canvasItemMaterial.ParticlesAnimLoop;
			_particlesHFramesSpinBox.Value = canvasItemMaterial.ParticlesAnimHFrames;
			_particlesVFramesSpinBox.Value = canvasItemMaterial.ParticlesAnimVFrames;
		}
		if (_material is BaseMaterial3D baseMaterial3D)
		{
			_albedoColorPicker.Color = baseMaterial3D.AlbedoColor;
			_metallicSpinBox.Value = baseMaterial3D.Metallic;
			_roughnessSpinBox.Value = baseMaterial3D.Roughness;
		}
		_updating = false;
		_materialPreview.QueueRedraw();
	}

	private Material ReadMaterial()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Object || !(propertyValue.As<GodotObject>() is Material result))
		{
			return null;
		}
		return result;
	}

	private void OnDrawMaterialPreview()
	{
		Rect2 rect = new Rect2(Vector2.Zero, _materialPreview.Size);
		_materialPreview.DrawRect(rect, new Color(0.075f, 0.075f, 0.075f));
		Color color = new Color(0.35f, 0.5f, 0.95f);
		if (_material is BaseMaterial3D baseMaterial3D)
		{
			color = baseMaterial3D.AlbedoColor;
		}
		else if (_material is ShaderMaterial)
		{
			color = new Color(0.58f, 0.42f, 0.94f);
		}
		else if (_material is CanvasItemMaterial canvasItemMaterial)
		{
			color = ((canvasItemMaterial.BlendMode == CanvasItemMaterial.BlendModeEnum.Add) ? new Color(0.95f, 0.7f, 0.22f) : new Color(0.32f, 0.62f, 0.92f));
		}
		Rect2 rect2 = rect.Grow(-10f);
		_materialPreview.DrawRect(rect2, color);
		_materialPreview.DrawRect(rect2, new Color(1f, 1f, 1f, 0.18f), filled: false, 1f);
	}

	private void OnShaderChanged(Resource resource)
	{
		if (!_updating && _material is ShaderMaterial shaderMaterial)
		{
			shaderMaterial.Shader = resource as Shader;
			PopulateShaderParamTree(shaderMaterial);
			CommitMaterialChange();
		}
	}

	private void PopulateShaderParamTree(ShaderMaterial shaderMaterial)
	{
		_shaderParamTree.Clear();
		TreeItem parent = _shaderParamTree.CreateItem();
		if (!GodotObject.IsInstanceValid(shaderMaterial?.Shader))
		{
			return;
		}
		foreach (Variant shaderUniform in shaderMaterial.Shader.GetShaderUniformList())
		{
			Dictionary dictionary = (Dictionary)shaderUniform;
			if (dictionary.TryGetValue("name", out var value))
			{
				string text = value.AsString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					TreeItem treeItem = _shaderParamTree.CreateItem(parent);
					treeItem.SetText(0, text);
					treeItem.SetText(1, dictionary.TryGetValue("type", out var value2) ? ((Variant.Type)value2.AsInt32()/*cast due to .constrained prefix*/).ToString() : "");
					Variant shaderParameter = shaderMaterial.GetShaderParameter(new StringName(text));
					treeItem.SetText(2, FormatVariant(shaderParameter));
					treeItem.SetMetadata(0, text);
				}
			}
		}
	}

	private void ReapplySelectedShaderParameter()
	{
		if (!(_material is ShaderMaterial shaderMaterial))
		{
			return;
		}
		TreeItem selected = _shaderParamTree.GetSelected();
		if (selected != null)
		{
			string text = selected.GetMetadata(0).AsString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				Variant shaderParameter = shaderMaterial.GetShaderParameter(new StringName(text));
				shaderMaterial.SetShaderParameter(new StringName(text), shaderParameter);
				CommitMaterialChange();
			}
		}
	}

	private void OnBlendModeSelected(long index)
	{
		if (!_updating && _material is CanvasItemMaterial canvasItemMaterial)
		{
			canvasItemMaterial.BlendMode = (CanvasItemMaterial.BlendModeEnum)_blendModeOption.GetItemId((int)index);
			CommitMaterialChange();
		}
	}

	private void OnLightModeSelected(long index)
	{
		if (!_updating && _material is CanvasItemMaterial canvasItemMaterial)
		{
			canvasItemMaterial.LightMode = (CanvasItemMaterial.LightModeEnum)_lightModeOption.GetItemId((int)index);
			CommitMaterialChange();
		}
	}

	private void OnParticlesAnimationToggled(bool toggled)
	{
		if (!_updating && _material is CanvasItemMaterial canvasItemMaterial)
		{
			canvasItemMaterial.ParticlesAnimation = toggled;
			CommitMaterialChange();
		}
	}

	private void OnParticlesAnimLoopToggled(bool toggled)
	{
		if (!_updating && _material is CanvasItemMaterial canvasItemMaterial)
		{
			canvasItemMaterial.ParticlesAnimLoop = toggled;
			CommitMaterialChange();
		}
	}

	private void OnParticlesHFramesChanged(double value)
	{
		if (!_updating && _material is CanvasItemMaterial canvasItemMaterial)
		{
			canvasItemMaterial.ParticlesAnimHFrames = Mathf.Max(1, (int)value);
			CommitMaterialChange();
		}
	}

	private void OnParticlesVFramesChanged(double value)
	{
		if (!_updating && _material is CanvasItemMaterial canvasItemMaterial)
		{
			canvasItemMaterial.ParticlesAnimVFrames = Mathf.Max(1, (int)value);
			CommitMaterialChange();
		}
	}

	private void OnAlbedoColorChanged(Color color)
	{
		if (!_updating && _material is BaseMaterial3D baseMaterial3D)
		{
			baseMaterial3D.AlbedoColor = color;
			CommitMaterialChange();
		}
	}

	private void OnMetallicChanged(double value)
	{
		if (!_updating && _material is BaseMaterial3D baseMaterial3D)
		{
			baseMaterial3D.Metallic = Mathf.Clamp((float)value, 0f, 1f);
			CommitMaterialChange();
		}
	}

	private void OnRoughnessChanged(double value)
	{
		if (!_updating && _material is BaseMaterial3D baseMaterial3D)
		{
			baseMaterial3D.Roughness = Mathf.Clamp((float)value, 0f, 1f);
			CommitMaterialChange();
		}
	}

	private void CommitMaterialChange()
	{
		if (GodotObject.IsInstanceValid(_material))
		{
			_material.EmitChanged();
			if (GodotObject.IsInstanceValid(Property) && Property.PropName != (StringName)"")
			{
				ValueChange(_material);
			}
			else
			{
				Property?.SetCall();
			}
			_materialPreview.QueueRedraw();
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

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 3uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "true" : "false";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			}
		}
		return variantType switch
		{
			Variant.Type.Color => value.AsColor().ToHtml(), 
			Variant.Type.Object => value.AsGodotObject()?.GetClass() ?? "空", 
			_ => value.ToString(), 
		};
	}
}
