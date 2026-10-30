using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Registry;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorXWBPVariableData : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/ResourceInspectorExtend/XWBPVariableData/XWInspectorPropertyEditorXWBPVariableData.tscn";

	private Button _variableTypeButton;

	private HBoxContainer _variableDefaultContainer;

	private PanelContainer _editorContainer;

	private XWWindowTypeSelector _typeSelectorWindow;

	private XWInspectorPropertyEditorBase _editor;

	private Variant.Type _currentType;

	private XWBPVariableData _variableData;

	public static XWInspectorPropertyEditorXWBPVariableData Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/ResourceInspectorExtend/XWBPVariableData/XWInspectorPropertyEditorXWBPVariableData.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorXWBPVariableData>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_variableTypeButton = GetNode<Button>("%VariableTypeButton");
		_variableDefaultContainer = GetNode<HBoxContainer>("%VariableDefaultContainer");
		_editorContainer = GetNode<PanelContainer>("%EditorContainer");
		_variableTypeButton.Pressed += OnTypeButtonPressed;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		_variableData = GetPropertyValue().As<XWBPVariableData>();
		Refresh();
	}

	public override void UpdateValue()
	{
		_variableData = GetPropertyValue().As<XWBPVariableData>();
		Refresh();
	}

	public override Variant GetValue()
	{
		return _variableData;
	}

	private void Refresh()
	{
		if (!GodotObject.IsInstanceValid(_variableData))
		{
			return;
		}
		if (_variableData.Type == Variant.Type.Object && XWClassRegistry.Instance.HasClass(_variableData.ClassName))
		{
			_variableTypeButton.Icon = XWClassRegistry.Instance.GetClassIcon(_variableData.ClassName);
			_variableTypeButton.Text = _variableData.ClassName;
		}
		else if (XWTypeRegistry.Instance.HasType(_variableData.Type))
		{
			_variableTypeButton.Icon = XWTypeRegistry.Instance.GetTypeIcon(_variableData.Type);
			_variableTypeButton.Text = XWTypeRegistry.Instance.GetTypeName(_variableData.Type);
		}
		if (_currentType == _variableData.Type)
		{
			return;
		}
		_currentType = _variableData.Type;
		_variableDefaultContainer.Visible = false;
		if (GodotObject.IsInstanceValid(_editor))
		{
			_editor.QueueFree();
			_editor = null;
		}
		if (XWTypeRegistry.Instance.HasType(_currentType) && _variableData.Type != Variant.Type.Nil)
		{
			_variableDefaultContainer.Visible = true;
			XWInspectorProperty property = new XWInspectorProperty(_variableData, "defaultValue", XWTypeRegistry.Instance.GetTypeDefaultValue(_currentType), null, null, PropertyHint.None);
			_editor = XWTypeRegistry.Instance.GetTypeEditor(_currentType);
			if (GodotObject.IsInstanceValid(_editor))
			{
				_editor.IsInner = true;
				_editor.PropertyNameLock = true;
				_editor.ValueChanged += OnDefaultValueChange;
				_editorContainer.AddChild(_editor, forceReadableName: false, InternalMode.Disabled);
				_editor.SetEditProperty(property);
				_editor.PropertyNameLabel.Text = "默认值";
			}
		}
	}

	private void OnDefaultValueChange(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (GodotObject.IsInstanceValid(_variableData))
		{
			_variableData.DefaultValue = value;
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
		if (GodotObject.IsInstanceValid(_variableData))
		{
			if (_currentType != type)
			{
				_variableData.DefaultValue = (XWTypeRegistry.Instance.HasType(type) ? XWTypeRegistry.Instance.GetTypeDefaultValue(type) : default(Variant));
			}
			_variableData.Type = type;
			_variableData.ClassName = className.ToString();
			Refresh();
			Property.SetCall();
		}
	}
}
