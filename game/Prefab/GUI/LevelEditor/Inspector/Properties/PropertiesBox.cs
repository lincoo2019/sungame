using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PropertiesBox : VBoxContainer
{
	public delegate void ValueChangedEventHandler(StringName key, Variant newValue);

	public delegate void NumberChangedEventHandler(StringName key, double newValue);

	public delegate void StringChangedEventHandler(StringName key, string newValue);

	public delegate void BoolChangedEventHandler(StringName key, bool newValue);

	public delegate void ColorChangedEventHandler(StringName key, Color newValue);

	public delegate void Vector2ChangedEventHandler(StringName key, Vector2 newValue);

	public delegate void Vector4iChangedEventHandler(StringName key, Vector4I newValue);

	public delegate void ArrayChangedEventHandler(StringName key, Array newValue);

	public delegate void ArrayVector2ChangedEventHandler(StringName key, Array<Vector2> newValue);

	private static Texture2D _iconReload;

	private static PackedScene _propertiesContainerScene;

	private static PackedScene _propertiesBoolScene;

	private static PackedScene _propertiesIntScene;

	private static PackedScene _propertiesFloatScene;

	private static PackedScene _propertiesStringScene;

	private static PackedScene _propertiesMultilineStringScene;

	private static PackedScene _propertiesVector2Scene;

	private static PackedScene _propertiesColorScene;

	private static PackedScene _propertiesEnumScene;

	private static PackedScene _propertiesFlagScene;

	private static PackedScene _propertiesArrayScene;

	private static PackedScene _propertiesArrayVector2Scene;

	private static PackedScene _propertiesVector4iScene;

	public Dictionary _keys = new Dictionary();

	private Array _groupStack = new Array();

	private static Texture2D IconReload => _iconReload ?? (_iconReload = GD.Load<Texture2D>("uid://b261mqo4crf07"));

	private static PackedScene PropertiesContainerScene => _propertiesContainerScene ?? (_propertiesContainerScene = GD.Load<PackedScene>("uid://dvx0agv2rta6e"));

	private static PackedScene PropertiesBoolScene => _propertiesBoolScene ?? (_propertiesBoolScene = GD.Load<PackedScene>("uid://drg0fi23ifge4"));

	private static PackedScene PropertiesIntScene => _propertiesIntScene ?? (_propertiesIntScene = GD.Load<PackedScene>("uid://c43mrf03jpt2g"));

	private static PackedScene PropertiesFloatScene => _propertiesFloatScene ?? (_propertiesFloatScene = GD.Load<PackedScene>("uid://ce6nbs0h3lltc"));

	private static PackedScene PropertiesStringScene => _propertiesStringScene ?? (_propertiesStringScene = GD.Load<PackedScene>("uid://m0ep8vt6fdbs"));

	private static PackedScene PropertiesMultilineStringScene => _propertiesMultilineStringScene ?? (_propertiesMultilineStringScene = GD.Load<PackedScene>("uid://bff45o7x3xb2u"));

	private static PackedScene PropertiesVector2Scene => _propertiesVector2Scene ?? (_propertiesVector2Scene = GD.Load<PackedScene>("uid://cbjrnlebs22v0"));

	private static PackedScene PropertiesColorScene => _propertiesColorScene ?? (_propertiesColorScene = GD.Load<PackedScene>("uid://ca1dcepidnu8m"));

	private static PackedScene PropertiesEnumScene => _propertiesEnumScene ?? (_propertiesEnumScene = GD.Load<PackedScene>("uid://c4cxl5ec7iret"));

	private static PackedScene PropertiesFlagScene => _propertiesFlagScene ?? (_propertiesFlagScene = GD.Load<PackedScene>("uid://dwohqr20egmxg"));

	private static PackedScene PropertiesArrayScene => _propertiesArrayScene ?? (_propertiesArrayScene = GD.Load<PackedScene>("uid://b6pvi03p7a6nj"));

	private static PackedScene PropertiesArrayVector2Scene => _propertiesArrayVector2Scene ?? (_propertiesArrayVector2Scene = GD.Load<PackedScene>("uid://s2ng0og2b627"));

	private static PackedScene PropertiesVector4iScene => _propertiesVector4iScene ?? (_propertiesVector4iScene = GD.Load<PackedScene>("uid://dkwhkpg0ys0sj"));

	[Export(PropertyHint.None, "")]
	public double groupIndent { get; set; } = 8.0;

	public event ValueChangedEventHandler OnValueChanged;

	public event NumberChangedEventHandler OnNumberChanged;

	public event StringChangedEventHandler OnStringChanged;

	public event BoolChangedEventHandler OnBoolChanged;

	public event ColorChangedEventHandler OnColorChanged;

	public event Vector2ChangedEventHandler OnVector2Changed;

	public event Vector4iChangedEventHandler OnVector4iChanged;

	public event ArrayChangedEventHandler OnArrayChanged;

	public event ArrayVector2ChangedEventHandler OnArrayVector2Changed;

	public void Clear()
	{
		foreach (Node child in GetChildren())
		{
			child.QueueFree();
		}
		_keys.Clear();
		_groupStack.Clear();
	}

	public PropertiesBool add_bool(GodotObject obj, string propertyName, StringName key, bool value = false, Variant restValue = default(Variant))
	{
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = false;
		}
		PropertiesBool propertiesBool = PropertiesBoolScene.Instantiate<PropertiesBool>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesBool, Callable.From(delegate(bool v)
		{
			_on_bool_changed(v, key);
		}), restValue);
		propertiesBool.value = value;
		propertiesBool.obj = obj;
		propertiesBool.propertyName = propertyName;
		return propertiesBool;
	}

	public PropertiesInt add_int(GodotObject obj, string propertyName, StringName key, int value = 0, Variant restValue = default(Variant))
	{
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = 0;
		}
		PropertiesInt propertiesInt = PropertiesIntScene.Instantiate<PropertiesInt>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesInt, Callable.From(delegate(int v)
		{
			_on_number_changed(v, key);
		}), restValue);
		propertiesInt.value = value;
		propertiesInt.obj = obj;
		propertiesInt.propertyName = propertyName;
		return propertiesInt;
	}

	public PropertiesFloat add_float(GodotObject obj, string propertyName, StringName key, double value = 0.0, Variant restValue = default(Variant))
	{
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = 0.0;
		}
		PropertiesFloat propertiesFloat = PropertiesFloatScene.Instantiate<PropertiesFloat>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesFloat, Callable.From(delegate(double v)
		{
			_on_number_changed(v, key);
		}), restValue);
		propertiesFloat.value = value;
		propertiesFloat.obj = obj;
		propertiesFloat.propertyName = propertyName;
		return propertiesFloat;
	}

	public PropertiesBase add_string(GodotObject obj, string propertyName, StringName key, string value = "", Variant restValue = default(Variant), bool isMultiline = false)
	{
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = "";
		}
		if (!isMultiline)
		{
			PropertiesString propertiesString = PropertiesStringScene.Instantiate<PropertiesString>(PackedScene.GenEditState.Disabled);
			_add_property_editor(key, propertiesString, Callable.From(delegate(string v)
			{
				_on_string_changed(v, key);
			}), restValue);
			propertiesString.value = value;
			propertiesString.obj = obj;
			propertiesString.propertyName = propertyName;
			return propertiesString;
		}
		PropertiesMultilineString propertiesMultilineString = PropertiesMultilineStringScene.Instantiate<PropertiesMultilineString>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesMultilineString, Callable.From(delegate(string v)
		{
			_on_string_changed(v, key);
		}), restValue, inner: false);
		propertiesMultilineString.value = value;
		propertiesMultilineString.obj = obj;
		propertiesMultilineString.propertyName = propertyName;
		return propertiesMultilineString;
	}

	public PropertiesColor add_color(GodotObject obj, string propertyName, StringName key, Color value = default(Color), Variant restValue = default(Variant))
	{
		if (value == default(Color))
		{
			value = Colors.Black;
		}
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = Colors.White;
		}
		PropertiesColor propertiesColor = PropertiesColorScene.Instantiate<PropertiesColor>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesColor, Callable.From(delegate(Color v)
		{
			_on_color_changed(v, key);
		}), restValue);
		propertiesColor.value = value;
		propertiesColor.obj = obj;
		propertiesColor.propertyName = propertyName;
		return propertiesColor;
	}

	public PropertiesVector2 add_vector2(GodotObject obj, string propertyName, StringName key, Vector2 value = default(Vector2), Variant restValue = default(Variant))
	{
		if (value == default(Vector2))
		{
			value = Vector2.Zero;
		}
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = Vector2.Zero;
		}
		PropertiesVector2 propertiesVector = PropertiesVector2Scene.Instantiate<PropertiesVector2>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesVector, Callable.From(delegate(Vector2 v)
		{
			_on_vector2_changed(v, key);
		}), restValue);
		propertiesVector.value = value;
		propertiesVector.obj = obj;
		propertiesVector.propertyName = propertyName;
		return propertiesVector;
	}

	public PropertiesVector4i add_vector4i(GodotObject obj, string propertyName, StringName key, Vector4I value = default(Vector4I), Variant restValue = default(Variant))
	{
		if (value == default(Vector4I))
		{
			value = Vector4I.Zero;
		}
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = Vector4I.Zero;
		}
		PropertiesVector4i propertiesVector4i = PropertiesVector4iScene.Instantiate<PropertiesVector4i>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesVector4i, Callable.From(delegate(Vector4I v)
		{
			_on_vector4i_changed(v, key);
		}), restValue);
		propertiesVector4i.value = value;
		propertiesVector4i.obj = obj;
		propertiesVector4i.propertyName = propertyName;
		return propertiesVector4i;
	}

	public PropertiesEnum add_enum(GodotObject obj, string propertyName, StringName key, Variant hint, Variant value, Variant restValue)
	{
		PropertiesEnum propertiesEnum = PropertiesEnumScene.Instantiate<PropertiesEnum>(PackedScene.GenEditState.Disabled);
		Dictionary dictionary = new Dictionary();
		if (hint.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary2 = hint.AsGodotDictionary();
			Variant[] array = dictionary2.Keys.ToArray();
			foreach (Variant key2 in array)
			{
				dictionary[key2] = dictionary2[key2];
			}
		}
		if (hint.VariantType == Variant.Type.Array)
		{
			Array array2 = hint.AsGodotArray();
			for (int j = 0; j < array2.Count; j++)
			{
				Variant key3 = array2[j];
				dictionary[key3] = j;
			}
		}
		_add_property_editor(key, propertiesEnum, Callable.From(delegate(Variant v)
		{
			_on_value_changed(v, key);
		}), restValue);
		propertiesEnum.SetHintDictionary(dictionary);
		propertiesEnum.value = value;
		propertiesEnum.obj = obj;
		propertiesEnum.propertyName = propertyName;
		return propertiesEnum;
	}

	public PropertiesFlag add_flag(GodotObject obj, string propertyName, StringName key, Variant hint, int value = 0, Variant restValue = default(Variant))
	{
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = 0;
		}
		PropertiesFlag propertiesFlag = PropertiesFlagScene.Instantiate<PropertiesFlag>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesFlag, Callable.From(delegate(int v)
		{
			_on_number_changed(v, key);
		}), restValue);
		propertiesFlag.hintDictionary = (Dictionary)hint;
		propertiesFlag.value = value;
		propertiesFlag.obj = obj;
		propertiesFlag.propertyName = propertyName;
		return propertiesFlag;
	}

	public PropertiesArray add_array(GodotObject obj, string propertyName, StringName key, Variant hint, string hintType, Array value = null, Variant restValue = default(Variant))
	{
		if (value == null)
		{
			value = new Array();
		}
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = Colors.White;
		}
		PropertiesArray propertiesArray = PropertiesArrayScene.Instantiate<PropertiesArray>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesArray, Callable.From(delegate(Array v)
		{
			_on_array_changed(v, key);
		}), restValue, inner: false);
		propertiesArray.hint = hint;
		propertiesArray.hintType = hintType;
		propertiesArray.value = value;
		propertiesArray.obj = obj;
		propertiesArray.propertyName = propertyName;
		return propertiesArray;
	}

	public PropertiesArrayVector2 add_array_vector2(GodotObject obj, string propertyName, StringName key, Array<Vector2> value = null, Variant restValue = default(Variant))
	{
		if (value == null)
		{
			value = new Array<Vector2>();
		}
		if (restValue.VariantType == Variant.Type.Nil)
		{
			restValue = new Array();
		}
		PropertiesArrayVector2 propertiesArrayVector = PropertiesArrayVector2Scene.Instantiate<PropertiesArrayVector2>(PackedScene.GenEditState.Disabled);
		_add_property_editor(key, propertiesArrayVector, Callable.From(delegate(Array<Vector2> v)
		{
			_on_array_vector2_changed(v, key);
		}), restValue, inner: false);
		propertiesArrayVector.value = value;
		propertiesArrayVector.obj = obj;
		propertiesArrayVector.propertyName = propertyName;
		return propertiesArrayVector;
	}

	public PropertiesBase add_file_select(GodotObject obj, string propertyName, StringName key, Variant hint, Variant value, Variant restValue)
	{
		return add_string(obj, propertyName, key, value.AsString(), restValue);
	}

	public PropertiesBase add_map_select(GodotObject obj, string propertyName, StringName key, Variant value, Variant restValue)
	{
		return add_string(obj, propertyName, key, value.AsString(), restValue);
	}

	public PropertiesBase add_room_select(GodotObject obj, string propertyName, StringName key, Variant hint, Variant value, Variant restValue)
	{
		return add_string(obj, propertyName, key, value.AsString(), restValue);
	}

	public void add_group(string label)
	{
		Button button = new Button();
		VBoxContainer vBoxContainer = new VBoxContainer();
		HBoxContainer hBoxContainer = new HBoxContainer();
		Control control = new Control();
		VBoxContainer vBoxContainer2 = new VBoxContainer();
		button.FocusMode = FocusModeEnum.None;
		button.Text = label;
		button.ToggleMode = true;
		button.ButtonPressed = true;
		button.Alignment = HorizontalAlignment.Left;
		button.IconAlignment = HorizontalAlignment.Left;
		button.Icon = GetThemeIcon("arrow", "Tree");
		button.AddThemeFontSizeOverride("font_size", 16);
		button.Toggled += hBoxContainer.SetVisible;
		button.LightMask = 0;
		hBoxContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hBoxContainer.AddThemeConstantOverride("separation", 0);
		hBoxContainer.LightMask = 0;
		control.CustomMinimumSize = new Vector2((float)groupIndent, 0f);
		control.LightMask = 0;
		vBoxContainer2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		vBoxContainer2.LightMask = 0;
		vBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.LightMask = 0;
		hBoxContainer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(vBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		_get_box().AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_groupStack.Add(vBoxContainer2);
	}

	public void end_group()
	{
		if (_groupStack.Count >= 1)
		{
			_groupStack.RemoveAt(_groupStack.Count - 1);
		}
	}

	public void delete_value(StringName key)
	{
		PropertiesBase propertiesBase = _keys[key].As<PropertiesBase>();
		_keys.Remove(key);
		propertiesBase.container.QueueFree();
		propertiesBase.QueueFree();
	}

	public Variant get_value(StringName key)
	{
		Variant result = _keys[key];
		if (result.VariantType == Variant.Type.Object && result.As<PropertiesBase>() != null)
		{
			return result.As<PropertiesBase>().GetValue();
		}
		return result;
	}

	public bool get_bool(StringName key)
	{
		return _keys[key].As<PropertiesBool>().value;
	}

	public int get_int(StringName key)
	{
		return _keys[key].As<PropertiesInt>().value;
	}

	public double get_float(StringName key)
	{
		return _keys[key].As<PropertiesFloat>().value;
	}

	public string get_string(StringName key)
	{
		return _keys[key].As<PropertiesString>().value;
	}

	public Color get_color(StringName key)
	{
		return _keys[key].As<PropertiesColor>().value;
	}

	public Vector2 get_vector2(StringName key)
	{
		return _keys[key].As<PropertiesVector2>().value;
	}

	public int get_option(StringName key)
	{
		return get_int(key);
	}

	private Control _get_box()
	{
		if (_groupStack.Count == 0)
		{
			return this;
		}
		return _groupStack[_groupStack.Count - 1].As<Control>();
	}

	public void set_value(StringName key, Variant value)
	{
		if (!_keys.ContainsKey(key))
		{
			return;
		}
		PropertiesBase propertiesBase = _keys[key].As<PropertiesBase>();
		if (propertiesBase == null || !propertiesBase.HasFocus())
		{
			if (propertiesBase is PropertiesBool propertiesBool)
			{
				propertiesBool.SetValue(value.AsBool());
			}
			else if (propertiesBase is PropertiesInt propertiesInt)
			{
				propertiesInt.SetValue(value.AsInt32());
			}
			else if (propertiesBase is PropertiesFloat propertiesFloat)
			{
				propertiesFloat.SetValue(value.AsDouble());
			}
			else if (propertiesBase is PropertiesString propertiesString)
			{
				propertiesString.SetValue(value.AsString());
			}
			else if (propertiesBase is PropertiesMultilineString propertiesMultilineString)
			{
				propertiesMultilineString.SetValue(value.AsString());
			}
			else if (propertiesBase is PropertiesVector2 propertiesVector)
			{
				propertiesVector.SetValue(value.AsVector2());
			}
			else if (propertiesBase is PropertiesVector4i propertiesVector4i)
			{
				propertiesVector4i.SetValue(value.AsVector4I());
			}
			else if (propertiesBase is PropertiesColor propertiesColor)
			{
				propertiesColor.SetValue(value.AsColor());
			}
			else if (propertiesBase is PropertiesEnum propertiesEnum)
			{
				propertiesEnum.SetValue(value);
			}
			else if (propertiesBase is PropertiesFlag propertiesFlag)
			{
				propertiesFlag.SetValue(value.AsInt32());
			}
			else if (propertiesBase is PropertiesArray propertiesArray)
			{
				propertiesArray.SetValue(value.AsGodotArray());
			}
			else if (propertiesBase is PropertiesArrayVector2 propertiesArrayVector)
			{
				propertiesArrayVector.SetValue(value.AsGodotArray<Vector2>());
			}
		}
	}

	private void _add_property_editor(StringName key, Control editor, Callable signalHandler, Variant restValue = default(Variant), bool inner = true)
	{
		_keys[key] = editor;
		PropertiesBase propertiesBase = editor as PropertiesBase;
		if (propertiesBase != null)
		{
			propertiesBase.key = key;
			propertiesBase.propertiesBox = this;
		}
		LevelEditorPropertiesContainer levelEditorPropertiesContainer = (propertiesBase.container = PropertiesContainerScene.Instantiate<LevelEditorPropertiesContainer>(PackedScene.GenEditState.Disabled));
		_get_box().AddChild(levelEditorPropertiesContainer, forceReadableName: false, InternalMode.Disabled);
		if (inner)
		{
			editor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			levelEditorPropertiesContainer.innerContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			editor.SizeFlagsVertical = SizeFlags.ExpandFill;
			levelEditorPropertiesContainer.outerContainer.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
		}
		levelEditorPropertiesContainer.keyLabel.Text = key;
		levelEditorPropertiesContainer.restValue = restValue;
		levelEditorPropertiesContainer.propertyEditor = propertiesBase;
		if (restValue.VariantType != Variant.Type.Nil)
		{
			levelEditorPropertiesContainer.refreshButton.Pressed += delegate
			{
				signalHandler.Call(restValue, key);
			};
		}
		_subscribe_value_changed(editor, key);
	}

	private void _subscribe_value_changed(Control editor, StringName key)
	{
		if (editor is PropertiesBool propertiesBool)
		{
			propertiesBool.OnValueChanged += delegate(bool v)
			{
				_on_bool_changed(v, key);
			};
		}
		else if (editor is PropertiesInt propertiesInt)
		{
			propertiesInt.OnValueChanged += delegate(int v)
			{
				_on_number_changed(v, key);
			};
		}
		else if (editor is PropertiesFloat propertiesFloat)
		{
			propertiesFloat.OnValueChanged += delegate(double v)
			{
				_on_number_changed(v, key);
			};
		}
		else if (editor is PropertiesString propertiesString)
		{
			propertiesString.OnValueChanged += delegate(string v)
			{
				_on_string_changed(v, key);
			};
		}
		else if (editor is PropertiesMultilineString propertiesMultilineString)
		{
			propertiesMultilineString.OnValueChanged += delegate(string v)
			{
				_on_string_changed(v, key);
			};
		}
		else if (editor is PropertiesVector2 propertiesVector)
		{
			propertiesVector.OnValueChanged += delegate(Vector2 v)
			{
				_on_vector2_changed(v, key);
			};
		}
		else if (editor is PropertiesVector4i propertiesVector4i)
		{
			propertiesVector4i.OnValueChanged += delegate(Vector4I v)
			{
				_on_vector4i_changed(v, key);
			};
		}
		else if (editor is PropertiesColor propertiesColor)
		{
			propertiesColor.OnValueChanged += delegate(Color v)
			{
				_on_color_changed(v, key);
			};
		}
		else if (editor is PropertiesEnum propertiesEnum)
		{
			propertiesEnum.OnValueChanged += delegate(Variant v)
			{
				_on_value_changed(v, key);
			};
		}
		else if (editor is PropertiesFlag propertiesFlag)
		{
			propertiesFlag.OnValueChanged += delegate(int v)
			{
				_on_number_changed(v, key);
			};
		}
		else if (editor is PropertiesArray propertiesArray)
		{
			propertiesArray.OnValueChanged += delegate(Array v)
			{
				_on_array_changed(v, key);
			};
		}
		else if (editor is PropertiesArrayVector2 propertiesArrayVector)
		{
			propertiesArrayVector.OnValueChanged += delegate(Array<Vector2> v)
			{
				_on_array_vector2_changed(v, key);
			};
		}
	}

	private void _on_value_changed(Variant value, StringName key)
	{
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_number_changed(double value, StringName key)
	{
		this.OnNumberChanged?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_number_changed(int value, StringName key)
	{
		this.OnNumberChanged?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_string_changed(string value, StringName key)
	{
		this.OnStringChanged?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_bool_changed(bool value, StringName key)
	{
		this.OnBoolChanged?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_color_changed(Color value, StringName key)
	{
		this.OnColorChanged?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_vector2_changed(Vector2 value, StringName key)
	{
		this.OnVector2Changed?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_vector4i_changed(Vector4I value, StringName key)
	{
		this.OnVector4iChanged?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_array_changed(Array value, StringName key)
	{
		this.OnArrayChanged?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}

	private void _on_array_vector2_changed(Array<Vector2> value, StringName key)
	{
		this.OnArrayVector2Changed?.Invoke(key, value);
		this.OnValueChanged?.Invoke(key, value);
	}
}
