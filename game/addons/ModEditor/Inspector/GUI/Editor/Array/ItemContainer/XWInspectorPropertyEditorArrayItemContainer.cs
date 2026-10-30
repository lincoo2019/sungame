using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorArrayItemContainer : HBoxContainer
{
	[Signal]
	public delegate void RemoveEventHandler(XWInspectorPropertyEditorArrayItemContainer item);

	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Array/ItemContainer/XWInspectorPropertyEditorArrayItemContainer.tscn";

	private PanelContainer _editorContainer;

	private Button _removeButton;

	public static XWInspectorPropertyEditorArrayItemContainer Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Array/ItemContainer/XWInspectorPropertyEditorArrayItemContainer.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorArrayItemContainer>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		_editorContainer = GetNode<PanelContainer>("%EditorContainer");
		_removeButton = GetNode<Button>("%RemoveButton");
		_removeButton.Pressed += RemoveButtonPressed;
	}

	public void AddEditor(XWInspectorPropertyEditorBase editor)
	{
		_editorContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
	}

	private void RemoveButtonPressed()
	{
		EmitSignal(SignalName.Remove, this);
	}
}
