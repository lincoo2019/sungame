using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorObject : XWInspectorPropertyEditorBase
{
	private static readonly Texture2D IconArrowClose = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GuiTreeArrowRight.svg", null, ResourceLoader.CacheMode.Reuse));

	private static readonly Texture2D IconArrowOpen = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GuiTreeArrowDown.svg", null, ResourceLoader.CacheMode.Reuse));

	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Object/XWInspectorPropertyEditorObject.tscn";

	private Label _objectLabel;

	private XWResourcePicker _resourcePicker;

	private XWInspector _subInspector;

	private Button _foldButton;

	private bool _isFolded = true;

	private string _baseType = "";

	private VBoxContainer _subInspectorContainer;

	private bool _isResource;

	private HBoxContainer _pickerContainer;

	public static XWInspectorPropertyEditorObject Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Object/XWInspectorPropertyEditorObject.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorObject>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_objectLabel = GetNode<Label>("%ObjectLabel");
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		Variant variant = default(Variant);
		if (property.PropName != null && property.PropName != (StringName)"" && GodotObject.IsInstanceValid(property.Object))
		{
			variant = XWInspectorPropertyEditorBase.ReadObjectProperty(property.Object, property.PropName);
		}
		_isResource = variant.VariantType == Variant.Type.Object && variant.As<GodotObject>() is Resource;
		if (property.Hint == PropertyHint.ResourceType)
		{
			_isResource = true;
			_baseType = property.HintString;
			if (string.IsNullOrEmpty(_baseType))
			{
				_baseType = "Resource";
			}
		}
		if (_isResource)
		{
			base.IsResourceEditor = true;
		}
		base.SetEditProperty(property, field);
		if (_isResource)
		{
			SetupResourceEditor();
		}
		else
		{
			SetupObjectEditor();
		}
	}

	private void SetupResourceEditor()
	{
		_objectLabel.Visible = false;
		if (!GodotObject.IsInstanceValid(_pickerContainer))
		{
			_pickerContainer = new HBoxContainer
			{
				Name = "PickerContainer",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			_foldButton = new Button
			{
				Icon = IconArrowClose,
				CustomMinimumSize = new Vector2(20f, 24f),
				Flat = true,
				TooltipText = "展开/折叠资源属性"
			};
			_foldButton.Pressed += OnFoldButtonPressed;
			_pickerContainer.AddChild(_foldButton, forceReadableName: false, InternalMode.Disabled);
			_resourcePicker = XWResourcePicker.Create();
			_resourcePicker.Name = "ResourcePicker";
			_resourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			_resourcePicker.ResourceChanged += OnResourceChanged;
			_resourcePicker.ResourceSelected += OnResourceSelected;
			_pickerContainer.AddChild(_resourcePicker, forceReadableName: false, InternalMode.Disabled);
			PropertyEditor.AddChild(_pickerContainer, forceReadableName: false, InternalMode.Disabled);
			NormalizePropertyEditorLayout();
		}
		if (GodotObject.IsInstanceValid(PropertyEditor))
		{
			PropertyEditor.Visible = true;
		}
		_pickerContainer.Visible = true;
		_resourcePicker.Setup(string.IsNullOrEmpty(_baseType) ? "Resource" : _baseType);
		UpdateResourceDisplay();
	}

	private void SetupObjectEditor()
	{
		if (GodotObject.IsInstanceValid(_pickerContainer))
		{
			_pickerContainer.Visible = false;
		}
		if (GodotObject.IsInstanceValid(PropertyEditor))
		{
			PropertyEditor.Visible = true;
		}
		_objectLabel.Visible = true;
		UpdateValue();
	}

	public override void UpdateValue()
	{
		if (_isResource)
		{
			UpdateResourceDisplay();
			return;
		}
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Object && GodotObject.IsInstanceValid(propertyValue.As<GodotObject>()))
		{
			GodotObject godotObject = propertyValue.As<GodotObject>();
			Variant script = godotObject.GetScript();
			string text = godotObject.GetClass();
			GodotObject godotObject2 = script.As<GodotObject>();
			if (GodotObject.IsInstanceValid(godotObject2) && godotObject2 is Script script2)
			{
				string text2 = script2.GetGlobalName();
				if (text2 != "")
				{
					text = text2 + " (" + godotObject.GetClass() + ")";
				}
			}
			_objectLabel.Text = text;
			_objectLabel.TooltipText = $"类: {godotObject.GetClass()}\n实例ID: {godotObject.GetInstanceId()}";
		}
		else
		{
			_objectLabel.Text = "<null>";
			_objectLabel.TooltipText = "";
		}
	}

	public override Variant GetValue()
	{
		return GetPropertyValue();
	}

	private void UpdateResourceDisplay()
	{
		if (!GodotObject.IsInstanceValid(_resourcePicker))
		{
			return;
		}
		Variant propertyValue = GetPropertyValue();
		Resource resource = ((propertyValue.VariantType == Variant.Type.Object && propertyValue.As<GodotObject>() is Resource resource2) ? resource2 : null);
		_resourcePicker.SetEditedResource(resource);
		if (GodotObject.IsInstanceValid(resource))
		{
			if (GodotObject.IsInstanceValid(_foldButton))
			{
				_foldButton.Visible = true;
				_foldButton.Icon = (_isFolded ? IconArrowClose : IconArrowOpen);
			}
			if (!_isFolded)
			{
				ShowSubInspector(resource);
			}
		}
		else
		{
			if (GodotObject.IsInstanceValid(_foldButton))
			{
				_foldButton.Visible = false;
			}
			HideSubInspector();
		}
	}

	private void OnFoldButtonPressed()
	{
		_isFolded = !_isFolded;
		if (GodotObject.IsInstanceValid(_foldButton))
		{
			_foldButton.Icon = (_isFolded ? IconArrowClose : IconArrowOpen);
		}
		if (!_isFolded)
		{
			Variant propertyValue = GetPropertyValue();
			Resource resource = ((propertyValue.VariantType == Variant.Type.Object && propertyValue.As<GodotObject>() is Resource resource2) ? resource2 : null);
			if (GodotObject.IsInstanceValid(resource))
			{
				ShowSubInspector(resource);
			}
		}
		else
		{
			HideSubInspector();
		}
	}

	private void ShowSubInspector(Resource res)
	{
		if (!GodotObject.IsInstanceValid(_subInspector))
		{
			_subInspectorContainer = new VBoxContainer
			{
				Name = "SubInspectorContainer",
				Visible = false
			};
			_subInspectorContainer.AddThemeConstantOverride("separation", 0);
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = "IndentContainer"
			};
			ColorRect node = new ColorRect
			{
				CustomMinimumSize = new Vector2(12f, 0f),
				Color = new Color(0.04f, 0.54f, 1f, 0.12f)
			};
			hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer();
			marginContainer.AddThemeConstantOverride("margin_left", 16);
			marginContainer.AddThemeConstantOverride("margin_right", 4);
			marginContainer.AddThemeConstantOverride("margin_top", 2);
			marginContainer.AddThemeConstantOverride("margin_bottom", 2);
			marginContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			_subInspector = XWInspector.CreateSubInspector();
			_subInspector.Name = "SubInspector";
			_subInspector.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			marginContainer.AddChild(_subInspector, forceReadableName: false, InternalMode.Disabled);
			hBoxContainer.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
			_subInspectorContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		}
		VBoxContainer vBoxContainer = GetParent() as VBoxContainer;
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			if (!_subInspectorContainer.IsInsideTree())
			{
				int num = vBoxContainer.GetChildren().IndexOf(this);
				vBoxContainer.AddChild(_subInspectorContainer, forceReadableName: false, InternalMode.Disabled);
				vBoxContainer.MoveChild(_subInspectorContainer, num + 1);
			}
			_subInspectorContainer.Visible = true;
			_subInspector.SetObject(res);
		}
	}

	private void HideSubInspector()
	{
		if (GodotObject.IsInstanceValid(_subInspectorContainer))
		{
			_subInspectorContainer.Visible = false;
		}
		if (GodotObject.IsInstanceValid(_subInspector))
		{
			_subInspector.SetObject(null);
		}
	}

	private void OnResourceChanged(Resource resource)
	{
		ValueChange(resource);
		if (GodotObject.IsInstanceValid(resource) && !_isFolded)
		{
			ShowSubInspector(resource);
		}
		else if (!GodotObject.IsInstanceValid(resource))
		{
			HideSubInspector();
		}
	}

	private void OnResourceSelected(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			_isFolded = false;
			if (GodotObject.IsInstanceValid(_foldButton))
			{
				_foldButton.Icon = IconArrowOpen;
			}
			ShowSubInspector(resource);
			XWInspector inspector = GetInspector();
			if (GodotObject.IsInstanceValid(inspector))
			{
				inspector.EmitSignal(XWInspector.SignalName.ResourceSelected, resource);
			}
		}
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1 && GodotObject.IsInstanceValid(_subInspectorContainer) && _subInspectorContainer.IsInsideTree())
		{
			_subInspectorContainer.QueueFree();
		}
	}
}
