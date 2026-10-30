using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorXWBPNodePortData : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/ResourceInspectorExtend/XWBPNodePortData/XWInspectorPropertyEditorXWBPNodePortData.tscn";

	private LineEdit _nameLineEdit;

	private Button _typeButton;

	private XWWindowTypeSelector _typeSelectorWindow;

	private XWBPNodePortData _portData;

	public static XWInspectorPropertyEditorXWBPNodePortData Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/ResourceInspectorExtend/XWBPNodePortData/XWInspectorPropertyEditorXWBPNodePortData.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorXWBPNodePortData>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_nameLineEdit = GetNode<LineEdit>("%LineEdit");
		_typeButton = GetNode<Button>("%TypeButton");
		_nameLineEdit.TextChanged += OnNameChange;
		_typeButton.Pressed += OnTypeButtonPressed;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		_portData = GetPropertyValue().As<XWBPNodePortData>();
		if (GodotObject.IsInstanceValid(_portData))
		{
			_nameLineEdit.Text = _portData.Name;
		}
		Refresh();
	}

	public override void UpdateValue()
	{
		_portData = GetPropertyValue().As<XWBPNodePortData>();
		if (GodotObject.IsInstanceValid(_portData))
		{
			_nameLineEdit.Text = _portData.Name;
		}
		Refresh();
	}

	public override Variant GetValue()
	{
		return _portData;
	}

	private void Refresh()
	{
		if (!GodotObject.IsInstanceValid(_portData))
		{
			return;
		}
		if (_portData.PortTypeValue == XWBPNodePortData.PortType.Object && XWClassRegistry.Instance.HasClass(_portData.ClassName.ToString()))
		{
			_typeButton.Icon = XWClassRegistry.Instance.GetClassIcon(_portData.ClassName.ToString());
			_typeButton.Text = _portData.ClassName.ToString();
			return;
		}
		Variant.Type type = (Variant.Type)_portData.PortTypeValue;
		if (XWTypeRegistry.Instance.HasType(type))
		{
			_typeButton.Icon = XWTypeRegistry.Instance.GetTypeIcon(type);
			_typeButton.Text = XWTypeRegistry.Instance.GetTypeName(type);
		}
	}

	private void OnNameChange(string newText)
	{
		if (GodotObject.IsInstanceValid(_portData))
		{
			_portData.Name = newText;
			Property.SetCall();
		}
	}

	private void OnTypeButtonPressed()
	{
		_typeSelectorWindow = XWWindowTypeSelector.Create();
		_typeSelectorWindow.TypeSelect += OnTypeSelected;
		AddChild(_typeSelectorWindow, forceReadableName: false, InternalMode.Disabled);
		_typeSelectorWindow.PopupCentered();
	}

	private void OnTypeSelected(Variant.Type type, StringName className)
	{
		if (GodotObject.IsInstanceValid(_portData))
		{
			_portData.PortTypeValue = (XWBPNodePortData.PortType)type;
			_portData.ClassName = className;
			Property.SetCall();
			Refresh();
		}
	}
}
