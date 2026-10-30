using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorResource : XWInspectorPropertyEditorBase
{
	private static Texture2D _iconArrowOpen;

	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Resource/XWInspectorPropertyEditorResource.tscn";

	private const int ButtonSeparation = 2;

	private static readonly Vector2 ResourceButtonMinimumSize = new Vector2(30f, 28f);

	private Button _foldButton;

	private XWResourcePicker _resourcePicker;

	private string _baseType = "Resource";

	public static XWInspectorPropertyEditorResource Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Resource/XWInspectorPropertyEditorResource.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorResource>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		if (!GodotObject.IsInstanceValid(_iconArrowOpen))
		{
			_iconArrowOpen = XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/GuiTreeArrowDown.svg", null, ResourceLoader.CacheMode.Reuse));
		}
		_foldButton = GetNode<Button>("%FoldButton");
		_resourcePicker = GetNode<XWResourcePicker>("%ResourcePicker");
		ApplyGodotInspectorLayout();
		_foldButton.Icon = _iconArrowOpen;
		_foldButton.TooltipText = "在可视化资源编辑器中编辑";
		_foldButton.Pressed += OnFoldButtonPressed;
		_resourcePicker.ResourceChanged += OnResourceChanged;
		_resourcePicker.ResourceSelected += OnResourceSelected;
	}

	private void ApplyGodotInspectorLayout()
	{
		if (GodotObject.IsInstanceValid(_foldButton))
		{
			_foldButton.CustomMinimumSize = ResourceButtonMinimumSize;
		}
		if (_foldButton?.GetParent() is HBoxContainer hBoxContainer)
		{
			hBoxContainer.AddThemeConstantOverride("separation", 2);
		}
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		if (property.Hint == PropertyHint.ResourceType)
		{
			_baseType = property.HintString;
			if (string.IsNullOrEmpty(_baseType))
			{
				_baseType = "Resource";
			}
		}
		else if (!string.IsNullOrEmpty(property.HintString))
		{
			_baseType = property.HintString;
		}
		if (GodotObject.IsInstanceValid(_resourcePicker))
		{
			_resourcePicker.Setup(_baseType);
		}
		base.SetEditProperty(property, field);
	}

	public override void UpdateValue()
	{
		UpdateResourceDisplay();
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
			}
		}
		else if (GodotObject.IsInstanceValid(_foldButton))
		{
			_foldButton.Visible = false;
		}
	}

	private void OnFoldButtonPressed()
	{
		Variant propertyValue = GetPropertyValue();
		Resource resource = ((propertyValue.VariantType == Variant.Type.Object && propertyValue.As<GodotObject>() is Resource resource2) ? resource2 : null);
		OpenResourceInGlobalInspector(resource);
	}

	private void OnResourceChanged(Resource resource)
	{
		ValueChange(resource);
	}

	private void OnResourceSelected(Resource resource)
	{
		OpenResourceInGlobalInspector(resource);
	}

	private void OpenResourceInGlobalInspector(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			string text = resource.ResourcePath ?? "";
			if (Property?.Object is Resource resource2)
			{
				string ownerPath = resource2.ResourcePath ?? "";
				XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForProperty(resource, resource2, text, ownerPath, Property.PropName.ToString(), -1, "inspector", XWResourceEditContext.IsBuiltInPath(text)));
			}
			else
			{
				XWEditorInterface.Instance?.EditResource(resource, XWResourceEditContext.ForRoot(resource, text, "inspector"));
			}
			XWInspector inspector = GetInspector();
			if (GodotObject.IsInstanceValid(inspector))
			{
				inspector.EmitSignal(XWInspector.SignalName.ResourceSelected, resource);
			}
		}
	}
}
