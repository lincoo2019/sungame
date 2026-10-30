using Godot;
using PVZHE.ModEditor.Registry;

namespace PVZHE.ModEditor.Inspector;

public abstract partial class XWInspectorPropertyEditorPackedArrayBase : XWInspectorPropertyEditorBase
{
	protected Button _arrayButton;

	protected Button _addButton;

	protected PanelContainer _editContainer;

	protected VBoxContainer _editorContainer;

	private bool _preserveElementEditorsWhileFocused;

	protected abstract string ArrayTypeName { get; }

	protected abstract Variant.Type ElementType { get; }

	protected abstract Variant GetDefaultElement();

	protected abstract int GetArraySize();

	protected abstract Variant GetElement(int index);

	protected abstract void SetElement(int index, Variant value);

	protected abstract void AppendElement(Variant value);

	protected abstract void RemoveAt(int index);

	protected abstract Variant GetArrayAsVariant();

	protected abstract void LoadArrayFromVariant(Variant value);

	public override void _Ready()
	{
		base._Ready();
		_arrayButton = GetNode<Button>("%ArrayButton");
		_addButton = GetNode<Button>("%AddButton");
		_editContainer = GetNode<PanelContainer>("%EditContainer");
		_editorContainer = GetNode<VBoxContainer>("%EditorContainer");
		_arrayButton.Toggled += ArrayButtonPressed;
		_addButton.Pressed += AddButtonPressed;
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		base.SetEditProperty(property, field);
		LoadArrayFromVariant(GetPropertyValue());
		SizeUpdate();
		if (!_preserveElementEditorsWhileFocused || !HasFocusedElementEditor())
		{
			_preserveElementEditorsWhileFocused = false;
			RefreshProperty();
		}
	}

	public override void UpdateValue()
	{
		LoadArrayFromVariant(GetPropertyValue());
		SizeUpdate();
		if (!_preserveElementEditorsWhileFocused || !HasFocusedElementEditor())
		{
			_preserveElementEditorsWhileFocused = false;
			RefreshProperty();
		}
	}

	public override Variant GetValue()
	{
		return GetArrayAsVariant();
	}

	protected void RefreshProperty()
	{
		foreach (Node child in _editorContainer.GetChildren())
		{
			child.QueueFree();
		}
		int arraySize = GetArraySize();
		for (int i = 0; i < arraySize; i++)
		{
			XWInspectorPropertyEditorArrayItemContainer xWInspectorPropertyEditorArrayItemContainer = XWInspectorPropertyEditorArrayItemContainer.Create();
			_editorContainer.AddChild(xWInspectorPropertyEditorArrayItemContainer, forceReadableName: false, InternalMode.Disabled);
			xWInspectorPropertyEditorArrayItemContainer.Remove += RemoveItem;
			XWInspectorPropertyEditorBase typeEditor = XWTypeRegistry.Instance.GetTypeEditor(ElementType);
			if (typeEditor != null)
			{
				typeEditor.IsInner = true;
				xWInspectorPropertyEditorArrayItemContainer.AddEditor(typeEditor);
				typeEditor.SetEditProperty(Property, new StringName(i.ToString()));
				int itemIndex = i;
				XWInspectorPropertyEditorBase itemEditor = typeEditor;
				typeEditor.ValueChanged += delegate(GodotObject obj, StringName prop, StringName fld, Variant val)
				{
					InnerValueChange(itemIndex, itemEditor, val);
				};
			}
		}
	}

	protected void InnerValueChange(int index, XWInspectorPropertyEditorBase editor, Variant fallbackValue)
	{
		if (index >= 0 && index < GetArraySize())
		{
			SetElement(index, GetElementEditorValue(editor, fallbackValue, GetElement(index)));
			_preserveElementEditorsWhileFocused = true;
			ValueChange(GetArrayAsVariant());
			SizeUpdate();
			if (!_preserveElementEditorsWhileFocused || !HasFocusedElementEditor())
			{
				_preserveElementEditorsWhileFocused = false;
				RefreshProperty();
			}
		}
	}

	protected void WriteBack()
	{
		if (GodotObject.IsInstanceValid(Property) && GodotObject.IsInstanceValid(Property.Object))
		{
			XWInspectorPropertyEditorBase.WriteObjectProperty(Property.Object, Property.PropName, GetArrayAsVariant());
		}
	}

	protected void AddElement()
	{
		_preserveElementEditorsWhileFocused = false;
		AppendElement(GetDefaultElement());
		WriteBack();
		Property?.SetCall();
		SizeUpdate();
		RefreshProperty();
	}

	protected void RemoveItem(XWInspectorPropertyEditorArrayItemContainer itemContainer)
	{
		int num = _editorContainer.GetChildren().IndexOf(itemContainer);
		if (num != -1)
		{
			_preserveElementEditorsWhileFocused = false;
			RemoveAt(num);
			WriteBack();
			itemContainer.QueueFree();
			Property?.SetCall();
			SizeUpdate();
			RefreshProperty();
		}
	}

	protected void ArrayButtonPressed(bool toggledOn)
	{
		_editContainer.Visible = toggledOn;
	}

	protected void AddButtonPressed()
	{
		AddElement();
	}

	public override void HideEditor()
	{
		base.HideEditor();
		_arrayButton.ButtonPressed = false;
		_editContainer.Visible = false;
	}

	public override void ShowEditor()
	{
		base.ShowEditor();
		_arrayButton.ButtonPressed = false;
		_editContainer.Visible = true;
	}

	protected void SizeUpdate()
	{
		_arrayButton.Text = $"{ArrayTypeName}( 大小 {GetArraySize()} )";
	}

	private static Variant GetElementEditorValue(XWInspectorPropertyEditorBase editor, Variant fallbackValue, Variant currentValue)
	{
		if (!GodotObject.IsInstanceValid(editor))
		{
			return fallbackValue;
		}
		Variant value = editor.GetValue();
		if (value.VariantType == Variant.Type.Nil && fallbackValue.VariantType != Variant.Type.Nil)
		{
			return fallbackValue;
		}
		if (fallbackValue.VariantType != Variant.Type.Nil && value.Equals(currentValue) && !fallbackValue.Equals(currentValue))
		{
			return fallbackValue;
		}
		return value;
	}

	private bool HasFocusedElementEditor()
	{
		if (GodotObject.IsInstanceValid(_editorContainer))
		{
			return IsControlTreeFocused(_editorContainer);
		}
		return false;
	}

	private static bool IsControlTreeFocused(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		if (node is Control control && control.HasFocus())
		{
			return true;
		}
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				if (IsControlTreeFocused(node2))
				{
					return true;
				}
			}
		}
		return false;
	}
}
