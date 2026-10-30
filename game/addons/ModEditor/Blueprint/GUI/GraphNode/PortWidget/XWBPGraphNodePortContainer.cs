using Godot;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.Registry;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPGraphNodePortContainer : MarginContainer
{
	[Signal]
	public delegate void InputValueChangeEventHandler(XWBPNodePortData portData, Variant oldValue, Variant newValue);

	private const float PortEditorHeight = 24f;

	private const string ScenePath = "res://addons/ModEditor/Blueprint/GUI/GraphNode/PortWidget/XWBPGraphNodePortContainer.tscn";

	private static PackedScene _scene;

	private PanelContainer _inputContainer;

	private PanelContainer _outputContainer;

	private Label _inputLabel;

	private Label _outputLabel;

	private TextureRect _inputTypeIcon;

	private TextureRect _outputTypeIcon;

	private PanelContainer _inputEditorContainer;

	private XWInspectorPropertyEditorBase _inputEditor;

	private Variant _lastInputValue;

	public XWBPNodePortData InputPortData { get; private set; }

	public XWBPNodePortData OutputPortData { get; private set; }

	public XWInspectorPropertyEditorBase InputEditor => _inputEditor;

	public static XWBPGraphNodePortContainer Create()
	{
		if (_scene == null)
		{
			_scene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Blueprint/GUI/GraphNode/PortWidget/XWBPGraphNodePortContainer.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		return _scene.Instantiate<XWBPGraphNodePortContainer>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_inputContainer = GetNode<PanelContainer>("%InputContainer");
		_outputContainer = GetNode<PanelContainer>("%OutputContainer");
		_inputTypeIcon = GetNode<TextureRect>("%InputTypeIcon");
		_inputLabel = GetNode<Label>("%InputLabel");
		_inputEditorContainer = GetNode<PanelContainer>("%InputEditorContainer");
		_outputLabel = GetNode<Label>("%OutputLabel");
		_outputTypeIcon = GetNode<TextureRect>("%OutputTypeIcon");
		SetVisibility();
	}

	private void SetVisibility()
	{
		if (_inputContainer != null)
		{
			_inputContainer.Visible = InputPortData != null;
			_inputLabel.Visible = InputPortData != null;
			if (GodotObject.IsInstanceValid(_inputEditorContainer))
			{
				_inputEditorContainer.Visible = false;
			}
			_outputContainer.Visible = OutputPortData != null;
			_outputLabel.Visible = OutputPortData != null;
		}
	}

	public void InputSet(XWBPNodePortData portData)
	{
		InputPortData = portData;
		_inputContainer.Visible = true;
		_inputLabel.Visible = true;
		_inputLabel.Text = portData.Name;
		_inputEditor = null;
		if (portData.PortTypeValue >= XWBPNodePortData.PortType.Flow)
		{
			return;
		}
		bool flag = ShouldCreateInputEditor(portData);
		_inputEditorContainer.Visible = flag;
		if (flag)
		{
			if (portData.Value.VariantType == Variant.Type.Nil && portData.DefaultValue.VariantType != Variant.Type.Nil)
			{
				portData.Value = portData.DefaultValue;
			}
			_lastInputValue = portData.Value;
			Variant.Type type = (Variant.Type)portData.PortTypeValue;
			_inputEditor = XWTypeRegistry.Instance.GetTypeEditor(type);
			if (GodotObject.IsInstanceValid(_inputEditor))
			{
				XWInspectorProperty property = new XWInspectorProperty(portData, "Value", default(Variant), null, null, PropertyHint.None);
				_inputEditor.IsInner = true;
				_inputEditor.ValueChanged += OnInputEditorValueChanged;
				_inputEditorContainer.AddChild(_inputEditor, forceReadableName: false, InternalMode.Disabled);
				_inputEditor.SetEditProperty(property, "Value");
				_inputEditor.PropertyNameLabel.Text = portData.Name;
				NormalizeEmbeddedEditor(_inputEditor);
				Control nodeOrNull = _inputEditor.GetNodeOrNull<Control>("%InformationContainner");
				if (GodotObject.IsInstanceValid(nodeOrNull))
				{
					nodeOrNull.Visible = false;
				}
			}
			else
			{
				_inputEditorContainer.Visible = false;
			}
		}
		_inputTypeIcon.Texture = XWBPNodePortData.GetTypeIcon(portData.PortTypeValue, portData.ClassName);
		_inputTypeIcon.Visible = true;
	}

	public void OutputSet(XWBPNodePortData portData)
	{
		OutputPortData = portData;
		_outputContainer.Visible = true;
		_outputLabel.Visible = true;
		_outputLabel.Text = portData.Name;
		if (portData.PortTypeValue < XWBPNodePortData.PortType.Flow)
		{
			_outputTypeIcon.Texture = XWBPNodePortData.GetTypeIcon(portData.PortTypeValue, portData.ClassName);
			_outputTypeIcon.Visible = true;
		}
	}

	private static bool ShouldCreateInputEditor(XWBPNodePortData portData)
	{
		if (GodotObject.IsInstanceValid(portData) && portData.PortTypeValue < XWBPNodePortData.PortType.Flow && !XWBPNodePortData.IsObjectPortType(portData.PortTypeValue))
		{
			return XWTypeRegistry.Instance.HasType((Variant.Type)portData.PortTypeValue);
		}
		return false;
	}

	private static void NormalizeEmbeddedEditor(XWInspectorPropertyEditorBase editor)
	{
		if (GodotObject.IsInstanceValid(editor))
		{
			editor.CustomMinimumSize = new Vector2(0f, 24f);
			editor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			editor.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			MarginContainer nodeOrNull = editor.GetNodeOrNull<MarginContainer>("MarginContainer");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.AddThemeConstantOverride("margin_left", 0);
				nodeOrNull.AddThemeConstantOverride("margin_right", 0);
				nodeOrNull.AddThemeConstantOverride("margin_top", 0);
				nodeOrNull.AddThemeConstantOverride("margin_bottom", 0);
			}
			HBoxContainer nodeOrNull2 = editor.GetNodeOrNull<HBoxContainer>("MarginContainer/VBoxContainer/HBoxContainer");
			if (GodotObject.IsInstanceValid(nodeOrNull2))
			{
				nodeOrNull2.CustomMinimumSize = new Vector2(0f, 24f);
				nodeOrNull2.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			}
			NormalizeEmbeddedControl(editor.GetNodeOrNull<Control>("%PropertyEditor") ?? editor.GetNodeOrNull<Control>("MarginContainer/VBoxContainer/HBoxContainer/CheckBox"));
		}
	}

	private static void NormalizeEmbeddedControl(Control control)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return;
		}
		control.CustomMinimumSize = new Vector2(control.CustomMinimumSize.X, 24f);
		control.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		if (control is Button button)
		{
			button.VerticalIconAlignment = VerticalAlignment.Center;
			button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		}
		foreach (Node child in control.GetChildren())
		{
			if (child is Control control2)
			{
				NormalizeEmbeddedControl(control2);
			}
		}
	}

	private void OnInputEditorValueChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if (GodotObject.IsInstanceValid(InputPortData))
		{
			Variant lastInputValue = _lastInputValue;
			if (!lastInputValue.Equals(value))
			{
				InputPortData.Value = value;
				_lastInputValue = value;
				EmitSignal(SignalName.InputValueChange, InputPortData, lastInputValue, value);
			}
		}
	}
}
