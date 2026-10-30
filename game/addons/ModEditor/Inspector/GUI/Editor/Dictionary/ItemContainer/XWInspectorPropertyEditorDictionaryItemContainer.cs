using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorDictionaryItemContainer : VBoxContainer
{
	[Signal]
	public delegate void RemoveEventHandler(XWInspectorPropertyEditorDictionaryItemContainer item);

	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Dictionary/ItemContainer/XWInspectorPropertyEditorDictionaryItemContainer.tscn";

	public RefCounted EntryProxy;

	private PanelContainer _keyEditorContainer;

	private PanelContainer _valueEditorContainer;

	private Button _removeButton;

	public static XWInspectorPropertyEditorDictionaryItemContainer Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Dictionary/ItemContainer/XWInspectorPropertyEditorDictionaryItemContainer.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorDictionaryItemContainer>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_keyEditorContainer = GetNode<PanelContainer>("%KeyEditorContainer");
		_valueEditorContainer = GetNode<PanelContainer>("%ValueEditorContainer");
		_removeButton = GetNode<Button>("%RemoveButton");
		_removeButton.Pressed += OnRemoveButtonPressed;
	}

	public PanelContainer GetKeyEditorContainer()
	{
		return _keyEditorContainer;
	}

	public PanelContainer GetValueEditorContainer()
	{
		return _valueEditorContainer;
	}

	private void OnRemoveButtonPressed()
	{
		EmitSignal(SignalName.Remove, this);
	}
}
