using System;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Registry;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorDictionary : XWInspectorPropertyEditorBase
{
	public partial class XWDictionaryEntryProxy : RefCounted
	{
		public Variant Key { get; set; }

		public Variant Value { get; set; }

		public override Variant _Get(StringName property)
		{
			if (property == (StringName)"key")
			{
				return Key;
			}
			if (property == (StringName)"value")
			{
				return Value;
			}
			return default(Variant);
		}

		public override bool _Set(StringName property, Variant value)
		{
			if (property == (StringName)"key")
			{
				Key = value;
				return true;
			}
			if (property == (StringName)"value")
			{
				Value = value;
				return true;
			}
			return false;
		}

		public override Array<Dictionary> _GetPropertyList()
		{
			return new Array<Dictionary>
			{
				new Dictionary
				{
					{ "name", "key" },
					{ "type", 0 },
					{ "usage", 4098 }
				},
				new Dictionary
				{
					{ "name", "value" },
					{ "type", 0 },
					{ "usage", 4098 }
				}
			};
		}
	}

	private const int PageLength = 20;

	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Dictionary/XWInspectorPropertyEditorDictionary.tscn";

	private Button _dictButton;

	private Button _addButton;

	private PanelContainer _editContainer;

	private VBoxContainer _editorContainer;

	private Button _keyTypeButton;

	private Button _valueTypeButton;

	private PanelContainer _newKeyContainer;

	private PanelContainer _newValueContainer;

	private Button _previousPageButton;

	private Button _nextPageButton;

	private Label _pageLabel;

	private Dictionary _dictionary = new Dictionary();

	private Variant.Type _keyType = Variant.Type.String;

	private Variant.Type _valueType;

	private PropertyHint _keyPropertyHint;

	private PropertyHint _valuePropertyHint;

	private string _keyHintString = "";

	private string _valueHintString = "";

	private XWWindowTypeSelector _typeSelectorWindow;

	private XWDictionaryEntryProxy _newEntryProxy;

	private XWInspectorPropertyEditorBase _newKeyEditor;

	private XWInspectorPropertyEditorBase _newValueEditor;

	private bool _preserveEntryEditorsWhileFocused;

	private bool _isExpanded;

	private int _pageIndex;

	private string _lastRenderedFingerprint = "";

	public static XWInspectorPropertyEditorDictionary Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Dictionary/XWInspectorPropertyEditorDictionary.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorDictionary>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_dictButton = GetNode<Button>("%DictButton");
		_addButton = GetNode<Button>("%AddButton");
		_editContainer = GetNode<PanelContainer>("%EditContainer");
		_editorContainer = GetNode<VBoxContainer>("%EditorContainer");
		_keyTypeButton = GetNode<Button>("%KeyTypeButton");
		_valueTypeButton = GetNode<Button>("%ValueTypeButton");
		_newKeyContainer = GetNode<PanelContainer>("%NewKeyContainer");
		_newValueContainer = GetNode<PanelContainer>("%NewValueContainer");
		_dictButton.Toggled += OnDictButtonToggled;
		_addButton.Pressed += OnAddButtonPressed;
		_keyTypeButton.Pressed += OnKeyTypeButtonPressed;
		_valueTypeButton.Pressed += OnValueTypeButtonPressed;
		CreatePaginatorControls();
		_newEntryProxy = new XWDictionaryEntryProxy
		{
			Key = "",
			Value = ""
		};
	}

	public override void SetEditProperty(XWInspectorProperty property, StringName field = null)
	{
		ParseDictionaryHint(property?.HintString ?? "");
		base.SetEditProperty(property, field);
	}

	public override void UpdateValue()
	{
		_dictionary = ReadDictionaryValue();
		SizeUpdate();
		UpdatePaginator();
		string text = BuildDictionaryFingerprint(_dictionary);
		if (!_isExpanded)
		{
			ClearEntryEditors();
			ClearNewEntryEditors();
		}
		else if ((!_isExpanded || !(text == _lastRenderedFingerprint) || !HasRenderedEntryEditors()) && (!_preserveEntryEditorsWhileFocused || (!base.IsContinuousEditActive && !HasFocusedEntryEditor())))
		{
			_preserveEntryEditorsWhileFocused = false;
			RefreshProperty();
		}
	}

	public override Variant GetValue()
	{
		return _dictionary;
	}

	private void RefreshProperty()
	{
		ClearEntryEditors();
		UpdatePaginator();
		UpdateTypeButtons();
		RefreshNewEntryEditors();
		for (int i = GetPageStart(); i < GetPageEnd(); i++)
		{
			Variant keyAtIndex = GetKeyAtIndex(i);
			Variant value = _dictionary[keyAtIndex];
			XWInspectorPropertyEditorDictionaryItemContainer container = XWInspectorPropertyEditorDictionaryItemContainer.Create();
			_editorContainer.AddChild(container, forceReadableName: false, InternalMode.Disabled);
			container.Remove += OnRemoveItem;
			XWDictionaryEntryProxy xWDictionaryEntryProxy = new XWDictionaryEntryProxy
			{
				Key = keyAtIndex,
				Value = value
			};
			container.EntryProxy = xWDictionaryEntryProxy;
			XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = CreateEditorForType(GetKeyEditorType(keyAtIndex), _keyPropertyHint, _keyHintString, keyAtIndex);
			if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase))
			{
				StringName propName = "key";
				PropertyHint keyPropertyHint = _keyPropertyHint;
				string keyHintString = _keyHintString;
				XWInspectorProperty property = new XWInspectorProperty(xWDictionaryEntryProxy, propName, default(Variant), null, null, keyPropertyHint, keyHintString);
				xWInspectorPropertyEditorBase.IsInner = true;
				xWInspectorPropertyEditorBase.ContinuousEditOwner = this;
				xWInspectorPropertyEditorBase.PropertyNameLock = true;
				xWInspectorPropertyEditorBase.ValueChanged += delegate(GodotObject obj, StringName prop, StringName fld, Variant val)
				{
					OnEntryValueChanged(container, prop, val);
				};
				container.GetKeyEditorContainer().AddChild(xWInspectorPropertyEditorBase, forceReadableName: false, InternalMode.Disabled);
				xWInspectorPropertyEditorBase.SetEditProperty(property);
				xWInspectorPropertyEditorBase.PropertyNameLabel.Text = "Key";
			}
			if (ShouldUseReadOnlyVariantSummary(value))
			{
				container.GetValueEditorContainer().AddChild(CreateValueSummaryControl(value), forceReadableName: false, InternalMode.Disabled);
				continue;
			}
			XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase2 = CreateEditorForType(GetValueEditorType(value), _valuePropertyHint, _valueHintString, value);
			if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase2))
			{
				StringName propName2 = "value";
				PropertyHint keyPropertyHint = _valuePropertyHint;
				string keyHintString = _valueHintString;
				XWInspectorProperty property2 = new XWInspectorProperty(xWDictionaryEntryProxy, propName2, default(Variant), null, null, keyPropertyHint, keyHintString);
				xWInspectorPropertyEditorBase2.IsInner = true;
				xWInspectorPropertyEditorBase2.ContinuousEditOwner = this;
				xWInspectorPropertyEditorBase2.PropertyNameLock = true;
				xWInspectorPropertyEditorBase2.ValueChanged += delegate(GodotObject obj, StringName prop, StringName fld, Variant val)
				{
					OnEntryValueChanged(container, prop, val);
				};
				container.GetValueEditorContainer().AddChild(xWInspectorPropertyEditorBase2, forceReadableName: false, InternalMode.Disabled);
				xWInspectorPropertyEditorBase2.SetEditProperty(property2);
				xWInspectorPropertyEditorBase2.PropertyNameLabel.Text = "Value";
			}
		}
		_lastRenderedFingerprint = BuildDictionaryFingerprint(_dictionary);
	}

	private void ClearEntryEditors()
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

	private void ClearNewEntryEditors()
	{
		if (GodotObject.IsInstanceValid(_newKeyEditor))
		{
			_newKeyEditor.QueueFree();
			_newKeyEditor = null;
		}
		if (GodotObject.IsInstanceValid(_newValueEditor))
		{
			_newValueEditor.QueueFree();
			_newValueEditor = null;
		}
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
		int maxPage = GetMaxPage(_dictionary.Count);
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
		_pageIndex = Mathf.Clamp(page, 0, GetMaxPage(_dictionary.Count));
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
		return Mathf.Clamp(_pageIndex * 20, 0, _dictionary.Count);
	}

	private int GetPageEnd()
	{
		return Mathf.Clamp(GetPageStart() + 20, 0, _dictionary.Count);
	}

	private string BuildDictionaryFingerprint(Dictionary dictionary)
	{
		if (dictionary == null)
		{
			return $"{_pageIndex}:null";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(_pageIndex).Append('|').Append(dictionary.Count);
		int num = Mathf.Clamp(_pageIndex * 20, 0, dictionary.Count);
		int num2 = Mathf.Clamp(num + 20, 0, dictionary.Count);
		int num3 = 0;
		foreach (Variant key in dictionary.Keys)
		{
			if (num3 >= num && num3 < num2)
			{
				Variant value = dictionary[key];
				stringBuilder.Append('|').Append(key.VariantType).Append(':')
					.Append(FormatFingerprintValue(key))
					.Append('=')
					.Append(value.VariantType)
					.Append(':')
					.Append(FormatFingerprintValue(value));
			}
			num3++;
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

	private bool HasRenderedEntryEditors()
	{
		if (_lastRenderedFingerprint != "")
		{
			if (_dictionary.Count != 0)
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

	private bool HasFocusedEntryEditor()
	{
		if ((!GodotObject.IsInstanceValid(_editorContainer) || !IsControlTreeFocused(_editorContainer)) && (!GodotObject.IsInstanceValid(_newKeyContainer) || !IsControlTreeFocused(_newKeyContainer)))
		{
			if (GodotObject.IsInstanceValid(_newValueContainer))
			{
				return IsControlTreeFocused(_newValueContainer);
			}
			return false;
		}
		return true;
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

	private Variant GetKeyAtIndex(int index)
	{
		int num = 0;
		foreach (Variant key in _dictionary.Keys)
		{
			if (num == index)
			{
				return key;
			}
			num++;
		}
		return default(Variant);
	}

	private void RefreshNewEntryEditors()
	{
		ClearNewEntryEditors();
		_newEntryProxy.Key = CreateDefaultValue(_keyType);
		_newEntryProxy.Value = CreateDefaultValue(_valueType);
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = CreateEditorForType(_keyType, _keyPropertyHint, _keyHintString, _newEntryProxy.Key);
		if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase))
		{
			XWDictionaryEntryProxy newEntryProxy = _newEntryProxy;
			StringName propName = "key";
			PropertyHint keyPropertyHint = _keyPropertyHint;
			string keyHintString = _keyHintString;
			XWInspectorProperty property = new XWInspectorProperty(newEntryProxy, propName, default(Variant), null, null, keyPropertyHint, keyHintString);
			xWInspectorPropertyEditorBase.IsInner = true;
			xWInspectorPropertyEditorBase.PropertyNameLock = true;
			xWInspectorPropertyEditorBase.ValueChanged += delegate(GodotObject obj, StringName prop, StringName fld, Variant val)
			{
				_newEntryProxy.Key = val;
			};
			_newKeyContainer.AddChild(xWInspectorPropertyEditorBase, forceReadableName: false, InternalMode.Disabled);
			xWInspectorPropertyEditorBase.SetEditProperty(property);
			xWInspectorPropertyEditorBase.PropertyNameLabel.Text = "Key";
			_newKeyEditor = xWInspectorPropertyEditorBase;
		}
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase2 = CreateEditorForType(_valueType, _valuePropertyHint, _valueHintString, _newEntryProxy.Value);
		if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase2))
		{
			XWDictionaryEntryProxy newEntryProxy2 = _newEntryProxy;
			StringName propName2 = "value";
			PropertyHint keyPropertyHint = _valuePropertyHint;
			string keyHintString = _valueHintString;
			XWInspectorProperty property2 = new XWInspectorProperty(newEntryProxy2, propName2, default(Variant), null, null, keyPropertyHint, keyHintString);
			xWInspectorPropertyEditorBase2.IsInner = true;
			xWInspectorPropertyEditorBase2.PropertyNameLock = true;
			xWInspectorPropertyEditorBase2.ValueChanged += delegate(GodotObject obj, StringName prop, StringName fld, Variant val)
			{
				_newEntryProxy.Value = val;
			};
			_newValueContainer.AddChild(xWInspectorPropertyEditorBase2, forceReadableName: false, InternalMode.Disabled);
			xWInspectorPropertyEditorBase2.SetEditProperty(property2);
			xWInspectorPropertyEditorBase2.PropertyNameLabel.Text = "Value";
			_newValueEditor = xWInspectorPropertyEditorBase2;
		}
	}

	private void OnEntryValueChanged(XWInspectorPropertyEditorDictionaryItemContainer container, StringName property, Variant value)
	{
		if (!(container.EntryProxy is XWDictionaryEntryProxy xWDictionaryEntryProxy))
		{
			return;
		}
		Dictionary dictionary = DuplicateDictionary(_dictionary);
		if (property == (StringName)"key")
		{
			Variant key = xWDictionaryEntryProxy.Key;
			Variant variant = value;
			if (key.VariantType != variant.VariantType || !key.Equals(variant))
			{
				if (dictionary.ContainsKey(variant))
				{
					xWDictionaryEntryProxy.Key = key;
					RefreshProperty();
					return;
				}
				Variant value2 = xWDictionaryEntryProxy.Value;
				RemoveDictionaryKey(dictionary, key);
				dictionary[variant] = value2;
				xWDictionaryEntryProxy.Key = variant;
				CommitDictionaryChange(dictionary);
			}
		}
		else if (property == (StringName)"value")
		{
			Variant key2 = xWDictionaryEntryProxy.Key;
			SetDictionaryValue(dictionary, key2, value);
			xWDictionaryEntryProxy.Value = value;
			CommitDictionaryChange(dictionary, preserveEntryEditors: true);
		}
	}

	private void OnAddButtonPressed()
	{
		Variant key = _newEntryProxy.Key;
		Variant value = _newEntryProxy.Value;
		Dictionary dictionary = DuplicateDictionary(_dictionary);
		if (!dictionary.ContainsKey(key))
		{
			dictionary[key] = value;
			_pageIndex = GetMaxPage(dictionary.Count);
			CommitDictionaryChange(dictionary);
		}
	}

	private void OnRemoveItem(XWInspectorPropertyEditorDictionaryItemContainer container)
	{
		if (container.EntryProxy is XWDictionaryEntryProxy { Key: var key })
		{
			Dictionary dictionary = DuplicateDictionary(_dictionary);
			RemoveDictionaryKey(dictionary, key);
			container.QueueFree();
			CommitDictionaryChange(dictionary);
		}
	}

	private Dictionary ReadDictionaryValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType == Variant.Type.Dictionary)
		{
			return DuplicateDictionary(propertyValue.As<Dictionary>());
		}
		return new Dictionary();
	}

	private static Dictionary DuplicateDictionary(Dictionary source)
	{
		if (source != null)
		{
			return source.Duplicate();
		}
		return new Dictionary();
	}

	private void CommitDictionaryChange(Dictionary nextDictionary, bool preserveEntryEditors = false)
	{
		_dictionary = DuplicateDictionary(nextDictionary);
		_preserveEntryEditorsWhileFocused = preserveEntryEditors;
		ValueChange(Variant.From(in nextDictionary));
		SizeUpdate();
		UpdatePaginator();
		if (!_preserveEntryEditorsWhileFocused || (!base.IsContinuousEditActive && !HasFocusedEntryEditor()))
		{
			_preserveEntryEditorsWhileFocused = false;
			RefreshProperty();
		}
	}

	private static void RemoveDictionaryKey(Dictionary dictionary, Variant key)
	{
		if (dictionary.ContainsKey(key))
		{
			dictionary.Remove(key);
			return;
		}
		string text = key.AsString();
		if (dictionary.ContainsKey(text))
		{
			dictionary.Remove(text);
		}
	}

	private static void SetDictionaryValue(Dictionary dictionary, Variant key, Variant value)
	{
		if (dictionary.ContainsKey(key))
		{
			dictionary[key] = value;
			return;
		}
		string text = key.AsString();
		if (dictionary.ContainsKey(text))
		{
			dictionary[text] = value;
		}
		else
		{
			dictionary[key] = value;
		}
	}

	private Variant.Type GetKeyEditorType(Variant key)
	{
		if (_keyType != Variant.Type.Nil)
		{
			return _keyType;
		}
		if (key.VariantType == Variant.Type.Nil)
		{
			return Variant.Type.Nil;
		}
		return key.VariantType;
	}

	private Variant.Type GetValueEditorType(Variant value)
	{
		if (_valueType != Variant.Type.Nil)
		{
			return _valueType;
		}
		if (value.VariantType == Variant.Type.Nil)
		{
			return Variant.Type.Nil;
		}
		return value.VariantType;
	}

	private XWInspectorPropertyEditorBase CreateEditorForType(Variant.Type type, PropertyHint hint, string hintString, Variant value)
	{
		if (hint != PropertyHint.None)
		{
			XWInspectorPropertyEditorBase hintEditor = XWTypeRegistry.Instance.GetHintEditor((int)hint);
			if (GodotObject.IsInstanceValid(hintEditor))
			{
				return hintEditor;
			}
		}
		if (type == Variant.Type.Object || hint == PropertyHint.ResourceType)
		{
			XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = CreateResourceEntryEditor(value, hint);
			if (GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase))
			{
				return xWInspectorPropertyEditorBase;
			}
		}
		if (hint == PropertyHint.ResourceType)
		{
			return XWTypeRegistry.Instance.GetResourcePropertyEditor();
		}
		return XWTypeRegistry.Instance.GetTypeEditor(type);
	}

	private bool ShouldUseReadOnlyVariantSummary(Variant value)
	{
		if (_valueType == Variant.Type.Nil)
		{
			return IsComplexVariantValue(value);
		}
		return false;
	}

	private static bool IsComplexVariantValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if (variantType == Variant.Type.Object || (ulong)(variantType - 27) <= 11uL)
		{
			return true;
		}
		return false;
	}

	private Control CreateValueSummaryControl(Variant value)
	{
		return new Button
		{
			Text = FormatVariantSummary(value),
			TooltipText = FormatVariantSummary(value),
			Disabled = true,
			CustomMinimumSize = new Vector2(0f, 28f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ShrinkCenter,
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
		};
	}

	private static string FormatVariantSummary(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 24;
		if ((ulong)num <= 14uL)
		{
			switch ((int)num)
			{
			case 4:
				return $"Array(大小 {value.AsGodotArray().Count})";
			case 3:
				return $"Dictionary(大小 {value.AsGodotDictionary().Count})";
			case 0:
				return FormatObjectSummary(value);
			case 5:
				return $"PackedByteArray(大小 {value.As<byte[]>().Length})";
			case 6:
				return $"PackedInt32Array(大小 {value.As<int[]>().Length})";
			case 7:
				return $"PackedInt64Array(大小 {value.As<long[]>().Length})";
			case 8:
				return $"PackedFloat32Array(大小 {value.As<float[]>().Length})";
			case 9:
				return $"PackedFloat64Array(大小 {value.As<double[]>().Length})";
			case 10:
				return $"PackedStringArray(大小 {value.As<string[]>().Length})";
			case 11:
				return $"PackedVector2Array(大小 {value.As<Vector2[]>().Length})";
			case 12:
				return $"PackedVector3Array(大小 {value.As<Vector3[]>().Length})";
			case 13:
				return $"PackedColorArray(大小 {value.As<Color[]>().Length})";
			case 14:
				return $"PackedVector4Array(大小 {value.As<Vector4[]>().Length})";
			}
		}
		return value.AsString();
	}

	private static string FormatObjectSummary(Variant value)
	{
		GodotObject godotObject = value.AsGodotObject();
		if (!GodotObject.IsInstanceValid(godotObject))
		{
			return "Object(null)";
		}
		if (!(godotObject is Resource resource))
		{
			return godotObject.GetClass();
		}
		return resource.GetClass() + "(Resource)";
	}

	private XWInspectorPropertyEditorBase CreateResourceEntryEditor(Variant value, PropertyHint hint)
	{
		if (value.VariantType != Variant.Type.Object)
		{
			return null;
		}
		Resource resource = value.As<GodotObject>() as Resource;
		if (!GodotObject.IsInstanceValid(resource))
		{
			if (hint != PropertyHint.ResourceType)
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

	private Variant CreateDefaultValue(Variant.Type type)
	{
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

	private void ParseDictionaryHint(string hintString)
	{
		_keyType = Variant.Type.String;
		_valueType = Variant.Type.Nil;
		_keyPropertyHint = PropertyHint.None;
		_valuePropertyHint = PropertyHint.None;
		_keyHintString = "";
		_valueHintString = "";
		if (!string.IsNullOrWhiteSpace(hintString))
		{
			string[] array = hintString.Split(';', 2);
			ParseTypeDescriptor(array[0], ref _keyType, ref _keyPropertyHint, ref _keyHintString);
			if (array.Length > 1)
			{
				ParseTypeDescriptor(array[1], ref _valueType, ref _valuePropertyHint, ref _valueHintString);
			}
		}
	}

	private static void ParseTypeDescriptor(string descriptor, ref Variant.Type type, ref PropertyHint hint, ref string hintString)
	{
		if (string.IsNullOrWhiteSpace(descriptor))
		{
			return;
		}
		string text = descriptor;
		int num = descriptor.IndexOf(':');
		if (num >= 0)
		{
			text = descriptor.Substring(0, num);
			string text2 = descriptor;
			int num2 = num + 1;
			hintString = text2.Substring(num2, text2.Length - num2);
		}
		int num3 = text.IndexOf('/');
		if (num3 >= 0)
		{
			string text2 = text;
			int num2 = num3 + 1;
			if (int.TryParse(text2.Substring(num2, text2.Length - num2), out var result))
			{
				hint = (PropertyHint)result;
			}
			text = text.Substring(0, num3);
		}
		if (TryParseVariantType(text, out var type2))
		{
			type = type2;
			return;
		}
		type = Variant.Type.Object;
		hint = PropertyHint.ResourceType;
		hintString = descriptor;
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

	private void OnKeyTypeButtonPressed()
	{
		_typeSelectorWindow = XWWindowTypeSelector.Create();
		_typeSelectorWindow.TypeSelect += OnKeyTypeSelected;
		AddChild(_typeSelectorWindow, forceReadableName: false, InternalMode.Disabled);
		_typeSelectorWindow.PopupCentered();
	}

	private void OnValueTypeButtonPressed()
	{
		_typeSelectorWindow = XWWindowTypeSelector.Create();
		_typeSelectorWindow.TypeSelect += OnValueTypeSelected;
		AddChild(_typeSelectorWindow, forceReadableName: false, InternalMode.Disabled);
		_typeSelectorWindow.PopupCentered();
	}

	private void OnKeyTypeSelected(Variant.Type type, StringName className)
	{
		_keyType = type;
		_keyPropertyHint = PropertyHint.None;
		_keyHintString = "";
		if (type == Variant.Type.Object && className != null && className.ToString() != "")
		{
			_keyPropertyHint = PropertyHint.ResourceType;
			_keyHintString = className.ToString();
		}
		UpdateTypeButtons();
		RefreshNewEntryEditors();
	}

	private void OnValueTypeSelected(Variant.Type type, StringName className)
	{
		_valueType = type;
		_valuePropertyHint = PropertyHint.None;
		_valueHintString = "";
		if (type == Variant.Type.Object && className != null && className.ToString() != "")
		{
			_valuePropertyHint = PropertyHint.ResourceType;
			_valueHintString = className.ToString();
		}
		UpdateTypeButtons();
		RefreshNewEntryEditors();
	}

	private void UpdateTypeButtons()
	{
		UpdateTypeButton(_keyTypeButton, "Key", _keyType, _keyHintString);
		UpdateTypeButton(_valueTypeButton, "Value", _valueType, _valueHintString);
	}

	private static void UpdateTypeButton(Button button, string label, Variant.Type type, string hintString)
	{
		if (GodotObject.IsInstanceValid(button))
		{
			if (type == Variant.Type.Nil)
			{
				button.Icon = null;
				button.TooltipText = label + ": Variant";
			}
			else if (XWTypeRegistry.Instance.HasType(type))
			{
				button.Icon = XWTypeRegistry.Instance.GetTypeIcon(type);
				string typeName = XWTypeRegistry.Instance.GetTypeName(type);
				button.TooltipText = (string.IsNullOrEmpty(hintString) ? (label + ": " + typeName) : $"{label}: {typeName} ({hintString})");
			}
			else
			{
				button.Icon = null;
				button.TooltipText = label + ": Variant";
			}
		}
	}

	private void OnDictButtonToggled(bool toggledOn)
	{
		_isExpanded = toggledOn;
		_editContainer.Visible = toggledOn;
		UpdatePaginator();
		if (toggledOn)
		{
			RefreshProperty();
			return;
		}
		ClearEntryEditors();
		ClearNewEntryEditors();
	}

	public override void HideEditor()
	{
		base.HideEditor();
		_isExpanded = false;
		_dictButton.ButtonPressed = false;
		_editContainer.Visible = false;
		ClearEntryEditors();
		ClearNewEntryEditors();
		UpdatePaginator();
	}

	public override void ShowEditor()
	{
		base.ShowEditor();
		_dictButton.ButtonPressed = _isExpanded;
		_editContainer.Visible = _isExpanded;
		if (!_isExpanded)
		{
			ClearEntryEditors();
			ClearNewEntryEditors();
		}
		UpdatePaginator();
	}

	private void SizeUpdate()
	{
		string value = ((_keyType != Variant.Type.Nil) ? ("[" + XWTypeRegistry.Instance.GetTypeName(_keyType)) : "[Variant");
		string value2 = ((_valueType != Variant.Type.Nil) ? XWTypeRegistry.Instance.GetTypeName(_valueType) : "Variant");
		_dictButton.Text = $"Dictionary{value}, {value2}]( 大小 {_dictionary.Count} )";
	}
}
