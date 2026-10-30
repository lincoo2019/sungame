using System;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Registry;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorArray : XWInspectorPropertyEditorBase
{
	private const int PageLength = 20;

	private Button _arrayButton;

	private Button _addButton;

	private PanelContainer _editContainer;

	private VBoxContainer _editorContainer;

	private Button _previousPageButton;

	private Button _nextPageButton;

	private Label _pageLabel;

	private Variant.Type _elementType;

	private Variant.Type _hintElementType;

	private PropertyHint _hintElementPropertyHint;

	private string _hintElementString = "";

	private Godot.Collections.Array _array = new Godot.Collections.Array();

	private bool _preserveElementEditorsWhileFocused;

	private bool _isExpanded;

	private int _pageIndex;

	private string _lastRenderedFingerprint = "";

	public override void _Ready()
	{
		base._Ready();
		_arrayButton = GetNode<Button>("%ArrayButton");
		_addButton = GetNode<Button>("%AddButton");
		_editContainer = GetNode<PanelContainer>("%EditContainer");
		_editorContainer = GetNode<VBoxContainer>("%EditorContainer");
		_arrayButton.Toggled += ArrayButtonPressed;
		_addButton.Pressed += AddButtonPressed;
		CreatePaginatorControls();
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		ParseArrayHint(property?.HintString ?? "");
		base.SetEditProperty(property, field);
	}

	public override void UpdateValue()
	{
		_array = ReadArrayValue();
		DetectElementType();
		SizeUpdate();
		UpdatePaginator();
		string text = BuildArrayFingerprint(_array);
		if (!_isExpanded)
		{
			ClearElementEditors();
		}
		else if ((!(text == _lastRenderedFingerprint) || !HasRenderedElementEditors()) && (!_preserveElementEditorsWhileFocused || (!base.IsContinuousEditActive && !HasFocusedElementEditor())))
		{
			_preserveElementEditorsWhileFocused = false;
			RefreshProperty();
		}
	}

	public override Variant GetValue()
	{
		return _array;
	}

	private void DetectElementType()
	{
		_elementType = _hintElementType;
		if (_elementType != Variant.Type.Nil || _array.Count == 0)
		{
			return;
		}
		Variant.Type variantType = _array[0].VariantType;
		for (int i = 1; i < _array.Count; i++)
		{
			if (_array[i].VariantType != variantType)
			{
				return;
			}
		}
		_elementType = variantType;
	}

	private void RefreshProperty()
	{
		ClearElementEditors();
		UpdatePaginator();
		for (int i = GetPageStart(); i < GetPageEnd(); i++)
		{
			XWInspectorPropertyEditorArrayItemContainer xWInspectorPropertyEditorArrayItemContainer = XWInspectorPropertyEditorArrayItemContainer.Create();
			_editorContainer.AddChild(xWInspectorPropertyEditorArrayItemContainer, forceReadableName: false, InternalMode.Disabled);
			xWInspectorPropertyEditorArrayItemContainer.Remove += RemoveItem;
			Variant value = _array[i];
			Variant.Type elementEditorType = GetElementEditorType(value);
			XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = CreateElementEditor(elementEditorType, value);
			if (xWInspectorPropertyEditorBase != null)
			{
				GodotObject obj = Property.Object;
				StringName propName = Property.PropName;
				PropertyHint hintElementPropertyHint = _hintElementPropertyHint;
				string hintElementString = _hintElementString;
				XWInspectorProperty property = new XWInspectorProperty(obj, propName, default(Variant), null, null, hintElementPropertyHint, hintElementString)
				{
					ReadOnly = Property.ReadOnly,
					Description = Property.Description,
					PropertyUsage = Property.PropertyUsage
				};
				xWInspectorPropertyEditorBase.IsInner = true;
				xWInspectorPropertyEditorBase.ContinuousEditOwner = this;
				xWInspectorPropertyEditorArrayItemContainer.AddEditor(xWInspectorPropertyEditorBase);
				xWInspectorPropertyEditorBase.SetEditProperty(property, new StringName(i.ToString()));
				int itemIndex = i;
				XWInspectorPropertyEditorBase itemEditor = xWInspectorPropertyEditorBase;
				xWInspectorPropertyEditorBase.ValueChanged += delegate(GodotObject godotObject, StringName prop, StringName fld, Variant val)
				{
					InnerValueChange(itemIndex, itemEditor, val);
				};
			}
		}
		_lastRenderedFingerprint = BuildArrayFingerprint(_array);
	}

	private void ClearElementEditors()
	{
		if (!GodotObject.IsInstanceValid(_editorContainer))
		{
			return;
		}
		foreach (Node child in _editorContainer.GetChildren())
		{
			child.QueueFree();
		}
		_lastRenderedFingerprint = "";
	}

	private string BuildArrayFingerprint(Godot.Collections.Array array)
	{
		if (array == null)
		{
			return $"{_pageIndex}:null";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_pageIndex).Append('|').Append(array.Count);
		int num = Mathf.Clamp(_pageIndex * 20, 0, array.Count);
		int num2 = Mathf.Clamp(num + 20, 0, array.Count);
		for (int i = num; i < num2; i++)
		{
			Variant value = array[i];
			stringBuilder.Append('|').Append(value.VariantType).Append(':')
				.Append(FormatFingerprintValue(value));
		}
		return stringBuilder.ToString();
	}

	private static string FormatFingerprintValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 24;
		if ((ulong)num <= 14uL)
		{
			switch ((int)num)
			{
			case 4:
				return value.AsGodotArray().Count.ToString();
			case 3:
				return value.AsGodotDictionary().Count.ToString();
			case 0:
				return FormatObjectFingerprint(value);
			case 5:
				return value.As<byte[]>().Length.ToString();
			case 6:
				return value.As<int[]>().Length.ToString();
			case 7:
				return value.As<long[]>().Length.ToString();
			case 8:
				return value.As<float[]>().Length.ToString();
			case 9:
				return value.As<double[]>().Length.ToString();
			case 10:
				return value.As<string[]>().Length.ToString();
			case 11:
				return value.As<Vector2[]>().Length.ToString();
			case 12:
				return value.As<Vector3[]>().Length.ToString();
			case 13:
				return value.As<Color[]>().Length.ToString();
			case 14:
				return value.As<Vector4[]>().Length.ToString();
			}
		}
		return value.AsString();
	}

	private static string FormatObjectFingerprint(Variant value)
	{
		GodotObject godotObject = value.AsGodotObject();
		if (!GodotObject.IsInstanceValid(godotObject))
		{
			return "null";
		}
		return godotObject.GetInstanceId().ToString();
	}

	private bool HasRenderedElementEditors()
	{
		if (_lastRenderedFingerprint != "")
		{
			if (_array.Count != 0)
			{
				if (GodotObject.IsInstanceValid(_editorContainer))
				{
					return _editorContainer.GetChildCount() > 0;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private void CreatePaginatorControls()
	{
		if (!GodotObject.IsInstanceValid(_addButton) || GodotObject.IsInstanceValid(_pageLabel))
		{
			return;
		}
		HBoxContainer hBoxContainer = _addButton.GetParent() as HBoxContainer;
		if (GodotObject.IsInstanceValid(hBoxContainer))
		{
			Control node = new Control
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_previousPageButton = new Button
			{
				Text = "<",
				CustomMinimumSize = new Vector2(28f, 26f),
				TooltipText = "上一页"
			};
			_previousPageButton.Pressed += delegate
			{
				ChangePage(_pageIndex - 1);
			};
			hBoxContainer.AddChild(_previousPageButton, forceReadableName: false, InternalMode.Disabled);
			_pageLabel = new Label
			{
				Text = "1/1",
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				CustomMinimumSize = new Vector2(52f, 26f)
			};
			hBoxContainer.AddChild(_pageLabel, forceReadableName: false, InternalMode.Disabled);
			_nextPageButton = new Button
			{
				Text = ">",
				CustomMinimumSize = new Vector2(28f, 26f),
				TooltipText = "下一页"
			};
			_nextPageButton.Pressed += delegate
			{
				ChangePage(_pageIndex + 1);
			};
			hBoxContainer.AddChild(_nextPageButton, forceReadableName: false, InternalMode.Disabled);
			UpdatePaginator();
		}
	}

	private void UpdatePaginator()
	{
		int maxPage = GetMaxPage(_array.Count);
		_pageIndex = Mathf.Clamp(_pageIndex, 0, maxPage);
		bool visible = _isExpanded && maxPage > 0;
		if (GodotObject.IsInstanceValid(_previousPageButton))
		{
			_previousPageButton.Visible = visible;
			_previousPageButton.Disabled = _pageIndex <= 0;
		}
		if (GodotObject.IsInstanceValid(_nextPageButton))
		{
			_nextPageButton.Visible = visible;
			_nextPageButton.Disabled = _pageIndex >= maxPage;
		}
		if (GodotObject.IsInstanceValid(_pageLabel))
		{
			_pageLabel.Visible = visible;
			_pageLabel.Text = $"{_pageIndex + 1}/{maxPage + 1}";
		}
		if (GodotObject.IsInstanceValid(_addButton))
		{
			_addButton.Visible = !_isExpanded || _pageIndex == maxPage;
		}
	}

	private void ChangePage(int page)
	{
		_pageIndex = Mathf.Clamp(page, 0, GetMaxPage(_array.Count));
		if (_isExpanded)
		{
			RefreshProperty();
		}
		else
		{
			UpdatePaginator();
		}
	}

	private int GetMaxPage(int count)
	{
		if (count > 0)
		{
			return (count - 1) / 20;
		}
		return 0;
	}

	private int GetPageStart()
	{
		return Mathf.Clamp(_pageIndex * 20, 0, _array.Count);
	}

	private int GetPageEnd()
	{
		return Mathf.Clamp(GetPageStart() + 20, 0, _array.Count);
	}

	private void InnerValueChange(int index, XWInspectorPropertyEditorBase editor, Variant fallbackValue)
	{
		if (index >= 0 && index < _array.Count)
		{
			Godot.Collections.Array array = DuplicateArray(_array);
			array[index] = GetElementEditorValue(editor, fallbackValue, _array[index]);
			CommitArrayChange(array, preserveElementEditors: true);
		}
	}

	private void AddElement()
	{
		Godot.Collections.Array array = DuplicateArray(_array);
		array.Add(CreateDefaultElement());
		_pageIndex = GetMaxPage(array.Count);
		CommitArrayChange(array);
	}

	private void RemoveItem(XWInspectorPropertyEditorArrayItemContainer itemContainer)
	{
		int num = _editorContainer.GetChildren().IndexOf(itemContainer);
		if (num != -1)
		{
			int num2 = GetPageStart() + num;
			if (num2 >= 0 && num2 < _array.Count)
			{
				Godot.Collections.Array array = DuplicateArray(_array);
				array.RemoveAt(num2);
				itemContainer.QueueFree();
				CommitArrayChange(array);
			}
		}
	}

	private Godot.Collections.Array ReadArrayValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Array)
		{
			return DuplicateArray(propertyValue.As<Godot.Collections.Array>());
		}
		return new Godot.Collections.Array();
	}

	private static Godot.Collections.Array DuplicateArray(Godot.Collections.Array source)
	{
		if (source != null)
		{
			return source.Duplicate();
		}
		return new Godot.Collections.Array();
	}

	private void CommitArrayChange(Godot.Collections.Array nextArray, bool preserveElementEditors = false)
	{
		_array = DuplicateArray(nextArray);
		DetectElementType();
		_preserveElementEditorsWhileFocused = preserveElementEditors;
		ValueChange(Variant.From(in nextArray));
		SizeUpdate();
		UpdatePaginator();
		if (!_preserveElementEditorsWhileFocused || (!base.IsContinuousEditActive && !HasFocusedElementEditor()))
		{
			_preserveElementEditorsWhileFocused = false;
			RefreshProperty();
		}
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

	private Variant.Type GetElementEditorType(Variant value)
	{
		if (_hintElementType != Variant.Type.Nil)
		{
			return _hintElementType;
		}
		if (_elementType != Variant.Type.Nil)
		{
			return _elementType;
		}
		if (value.VariantType != Variant.Type.Nil)
		{
			return value.VariantType;
		}
		return Variant.Type.Nil;
	}

	private XWInspectorPropertyEditorBase CreateElementEditor(Variant.Type type, Variant value)
	{
		if (_hintElementPropertyHint != PropertyHint.None)
		{
			XWInspectorPropertyEditorBase hintEditor = XWTypeRegistry.Instance.GetHintEditor((int)_hintElementPropertyHint);
			if (GodotObject.IsInstanceValid(hintEditor))
			{
				return hintEditor;
			}
		}
		if (type == Variant.Type.Object || _hintElementPropertyHint == PropertyHint.ResourceType)
		{
			XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = CreateResourceElementEditor(value);
			if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase))
			{
				return xWInspectorPropertyEditorBase;
			}
		}
		if (_hintElementPropertyHint == PropertyHint.ResourceType)
		{
			return XWTypeRegistry.Instance.GetResourcePropertyEditor();
		}
		return XWTypeRegistry.Instance.GetTypeEditor(type);
	}

	private XWInspectorPropertyEditorBase CreateResourceElementEditor(Variant value)
	{
		if (value.VariantType != Variant.Type.Object)
		{
			return null;
		}
		Resource resource = value.As<GodotObject>() as Resource;
		if (!GodotObject.IsInstanceValid(resource))
		{
			if (_hintElementPropertyHint != PropertyHint.ResourceType)
			{
				return null;
			}
			return XWTypeRegistry.Instance.GetResourcePropertyEditor();
		}
		PackedScene editor = XWInspectorPropertyRegistry.GetEditor(resource);
		if (editor != null)
		{
			return editor.Instantiate<XWInspectorPropertyEditorBase>(PackedScene.GenEditState.Disabled);
		}
		return XWTypeRegistry.Instance.GetResourcePropertyEditor();
	}

	private Variant CreateDefaultElement()
	{
		Variant.Type type = ((_hintElementType != Variant.Type.Nil) ? _hintElementType : _elementType);
		switch (type)
		{
		case Variant.Type.Nil:
			return "";
		case Variant.Type.Array:
			return new Godot.Collections.Array();
		case Variant.Type.Dictionary:
			return new Dictionary();
		case Variant.Type.Object:
			return Variant.From<GodotObject>((GodotObject)null);
		default:
			if (!XWTypeRegistry.Instance.HasType(type))
			{
				return default(Variant);
			}
			return XWTypeRegistry.Instance.GetTypeDefaultValue(type);
		}
	}

	private void ParseArrayHint(string hintString)
	{
		_hintElementType = Variant.Type.Nil;
		_hintElementPropertyHint = PropertyHint.None;
		_hintElementString = "";
		if (string.IsNullOrWhiteSpace(hintString))
		{
			return;
		}
		string text = hintString;
		int num = hintString.IndexOf(':');
		if (num >= 0)
		{
			text = hintString.Substring(0, num);
			string text2 = hintString;
			int num2 = num + 1;
			_hintElementString = text2.Substring(num2, text2.Length - num2);
		}
		int num3 = text.IndexOf('/');
		if (num3 >= 0)
		{
			string text2 = text;
			int num2 = num3 + 1;
			if (int.TryParse(text2.Substring(num2, text2.Length - num2), out var result))
			{
				_hintElementPropertyHint = (PropertyHint)result;
			}
			text = text.Substring(0, num3);
		}
		if (TryParseVariantType(text, out var type))
		{
			_hintElementType = type;
			return;
		}
		_hintElementType = Variant.Type.Object;
		_hintElementPropertyHint = PropertyHint.ResourceType;
		_hintElementString = hintString;
	}

	private static bool TryParseVariantType(string text, out Variant.Type type)
	{
		type = Variant.Type.Nil;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		Variant.Type[] values;
		if (long.TryParse(text, out var result))
		{
			values = Enum.GetValues<Variant.Type>();
			foreach (Variant.Type type2 in values)
			{
				if (Convert.ToInt64(type2) == result)
				{
					type = type2;
					return true;
				}
			}
		}
		values = Enum.GetValues<Variant.Type>();
		for (int i = 0; i < values.Length; i++)
		{
			Variant.Type type3 = values[i];
			if (string.Equals(type3.ToString(), text, StringComparison.OrdinalIgnoreCase))
			{
				type = type3;
				return true;
			}
		}
		foreach (XWTypeData item in XWTypeRegistry.Instance.GetAllType())
		{
			if (string.Equals(item.Name, text, StringComparison.OrdinalIgnoreCase))
			{
				type = item.Type;
				return true;
			}
		}
		return false;
	}

	private void ArrayButtonPressed(bool toggledOn)
	{
		_isExpanded = toggledOn;
		_editContainer.Visible = toggledOn;
		UpdatePaginator();
		if (toggledOn)
		{
			RefreshProperty();
		}
		else
		{
			ClearElementEditors();
		}
	}

	private void AddButtonPressed()
	{
		AddElement();
	}

	public override void HideEditor()
	{
		base.HideEditor();
		_isExpanded = false;
		_arrayButton.ButtonPressed = false;
		_editContainer.Visible = false;
		ClearElementEditors();
		UpdatePaginator();
	}

	public override void ShowEditor()
	{
		base.ShowEditor();
		_arrayButton.ButtonPressed = _isExpanded;
		_editContainer.Visible = _isExpanded;
		if (!_isExpanded)
		{
			ClearElementEditors();
		}
		UpdatePaginator();
	}

	private void SizeUpdate()
	{
		string value = ((_hintElementType != Variant.Type.Nil) ? ("[" + XWTypeRegistry.Instance.GetTypeName(_hintElementType) + "]") : "");
		_arrayButton.Text = $"Array{value}( 大小 {_array.Count} )";
	}
}
