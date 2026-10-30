using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class PropertiesArray : PropertiesBase
{
	public delegate void ValueChangedEventHandler(Array value);

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

	private Button foldButton;

	private VBoxContainer foldContainer;

	private VBoxContainer propertiesContainer;

	public string hintType = "Int";

	public Variant hint;

	public Array<LevelEditorPropertiesContainer> boxList = new Array<LevelEditorPropertiesContainer>();

	public double timer;

	private Array _value = new Array();

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

	public Array value
	{
		get
		{
			return _value;
		}
		set
		{
			if (!IsNodeReady())
			{
				return;
			}
			_value = value;
			foldButton.Text = $"数组( 大小 {_value.Count} )";
			foreach (LevelEditorPropertiesContainer box in boxList)
			{
				if (box != null && GodotObject.IsInstanceValid(box))
				{
					box.QueueFree();
				}
			}
			boxList.Clear();
			for (int i = 0; i < _value.Count; i++)
			{
				Variant variant = _value[i];
				AddEditor(i, variant);
			}
		}
	}

	public event ValueChangedEventHandler OnValueChanged;

	public override void _Ready()
	{
		foldButton = GetNode<Button>("%FoldButton");
		foldContainer = GetNode<VBoxContainer>("%FoldContainer");
		propertiesContainer = GetNode<VBoxContainer>("%PropertiesContainer");
		foldButton.Pressed += FoldButtonPressed;
		GetNode<Button>("%AddButton").Pressed += AddButtonPressed;
	}

	public void ValueChange(Variant _value)
	{
		Array array = new Array();
		foreach (LevelEditorPropertiesContainer box in boxList)
		{
			if (box != null && GodotObject.IsInstanceValid(box) && box.propertyEditor != null)
			{
				array.Add(box.propertyEditor.GetValue());
			}
		}
		_value = array;
		this.OnValueChanged?.Invoke(array);
	}

	public override bool CanSetValue()
	{
		foreach (LevelEditorPropertiesContainer box in boxList)
		{
			if (box != null && GodotObject.IsInstanceValid(box) && box.propertyEditor != null && !box.propertyEditor.CanSetValue())
			{
				return false;
			}
		}
		timer += GetProcessDeltaTime();
		if (timer > 0.2)
		{
			timer = 0.0;
			return true;
		}
		return false;
	}

	public void SetValue(Array newValue)
	{
		value = newValue;
	}

	public void FoldButtonPressed()
	{
		foldContainer.Visible = foldButton.ButtonPressed;
	}

	public void AddButtonPressed()
	{
		AddEditor(value.Count, default(Variant));
		ValueChange(default(Variant));
	}

	public void AddEditor(int id, Variant _value)
	{
		string text = hintType;
		if (text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 4:
			switch (text[0])
			{
			case 'B':
				if (text == "Bool")
				{
					AddBool(id, _value);
				}
				break;
			case 'E':
				if (text == "Enum")
				{
					AddEnum(id, _value);
				}
				break;
			case 'F':
				if (text == "Flag")
				{
					AddFlag(id, _value);
				}
				break;
			case 'C':
			case 'D':
				break;
			}
			break;
		case 5:
			switch (text[0])
			{
			case 'F':
				if (text == "Float")
				{
					AddFloat(id, _value);
				}
				break;
			case 'C':
				if (text == "Color")
				{
					AddColor(id, _value);
				}
				break;
			case 'A':
				if (text == "Array")
				{
					AddArray(id, _value);
				}
				break;
			}
			break;
		case 3:
			if (text == "Int")
			{
				AddInt(id, _value);
			}
			break;
		case 6:
			if (text == "String")
			{
				AddString(id, _value);
			}
			break;
		case 15:
			if (text == "MultilineString")
			{
				AddMultilineString(id, _value);
			}
			break;
		case 7:
			if (text == "Vector2")
			{
				AddVector2(id, _value);
			}
			break;
		}
	}

	public void AddPropertyEditor(int index, Variant _value, Control editor, bool inner = true)
	{
		LevelEditorPropertiesContainer levelEditorPropertiesContainer = PropertiesContainerScene.Instantiate<LevelEditorPropertiesContainer>(PackedScene.GenEditState.Disabled);
		(editor as PropertiesBase).isTargetObj = false;
		propertiesContainer.AddChild(levelEditorPropertiesContainer, forceReadableName: false, InternalMode.Disabled);
		boxList.Add(levelEditorPropertiesContainer);
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
		levelEditorPropertiesContainer.restValue = default(Variant);
		levelEditorPropertiesContainer.keyLabel.Text = index.ToString();
		levelEditorPropertiesContainer.propertyEditor = editor as PropertiesBase;
		if (editor is PropertiesBase propertiesBase)
		{
			propertiesBase.obj = obj;
			propertiesBase.propertyName = propertyName;
		}
		if (editor is PropertiesBool propertiesBool)
		{
			propertiesBool.OnValueChanged += delegate(bool v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesInt propertiesInt)
		{
			propertiesInt.OnValueChanged += delegate(int v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesFloat propertiesFloat)
		{
			propertiesFloat.OnValueChanged += delegate(double v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesString propertiesString)
		{
			propertiesString.OnValueChanged += delegate(string v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesMultilineString propertiesMultilineString)
		{
			propertiesMultilineString.OnValueChanged += delegate(string v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesVector2 propertiesVector)
		{
			propertiesVector.OnValueChanged += delegate(Vector2 v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesColor propertiesColor)
		{
			propertiesColor.OnValueChanged += delegate(Color v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesVector4i propertiesVector4i)
		{
			propertiesVector4i.OnValueChanged += delegate(Vector4I v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesEnum propertiesEnum)
		{
			propertiesEnum.OnValueChanged += delegate(Variant v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesFlag propertiesFlag)
		{
			propertiesFlag.OnValueChanged += delegate(int v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesArray propertiesArray)
		{
			propertiesArray.OnValueChanged += delegate(Array v)
			{
				ValueChange(v);
			};
		}
		else if (editor is PropertiesArrayVector2 propertiesArrayVector)
		{
			propertiesArrayVector.OnValueChanged += delegate(Array<Vector2> v)
			{
				ValueChange(v);
			};
		}
	}

	public PropertiesBool AddBool(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = false;
		}
		PropertiesBool propertiesBool = PropertiesBoolScene.Instantiate<PropertiesBool>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesBool);
		propertiesBool.value = _value.AsBool();
		return propertiesBool;
	}

	public PropertiesInt AddInt(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = 0;
		}
		PropertiesInt propertiesInt = PropertiesIntScene.Instantiate<PropertiesInt>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesInt);
		propertiesInt.value = _value.AsInt32();
		return propertiesInt;
	}

	public PropertiesFloat AddFloat(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = 0.0;
		}
		PropertiesFloat propertiesFloat = PropertiesFloatScene.Instantiate<PropertiesFloat>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesFloat);
		propertiesFloat.value = _value.AsDouble();
		return propertiesFloat;
	}

	public PropertiesString AddString(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = "";
		}
		PropertiesString propertiesString = PropertiesStringScene.Instantiate<PropertiesString>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesString);
		propertiesString.value = _value.AsString();
		return propertiesString;
	}

	public PropertiesMultilineString AddMultilineString(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = "";
		}
		PropertiesMultilineString propertiesMultilineString = PropertiesMultilineStringScene.Instantiate<PropertiesMultilineString>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesMultilineString, inner: false);
		propertiesMultilineString.value = _value.AsString();
		return propertiesMultilineString;
	}

	public PropertiesVector2 AddVector2(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = Vector2.Zero;
		}
		PropertiesVector2 propertiesVector = PropertiesVector2Scene.Instantiate<PropertiesVector2>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesVector);
		propertiesVector.value = _value.AsVector2();
		return propertiesVector;
	}

	public PropertiesColor AddColor(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = Colors.Black;
		}
		PropertiesColor propertiesColor = PropertiesColorScene.Instantiate<PropertiesColor>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesColor);
		propertiesColor.value = _value.AsColor();
		return propertiesColor;
	}

	public PropertiesEnum AddEnum(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = 0;
		}
		PropertiesEnum propertiesEnum = PropertiesEnumScene.Instantiate<PropertiesEnum>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesEnum);
		Dictionary dictionary = new Dictionary();
		if (hint.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary2 = hint.AsGodotDictionary();
			Variant[] array = dictionary2.Keys.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				Variant variant = array[i];
				if (dictionary2[variant].VariantType == Variant.Type.Int)
				{
					dictionary[variant] = dictionary2[variant];
				}
				if (dictionary2[variant].VariantType == Variant.Type.String)
				{
					dictionary[variant] = i;
				}
			}
		}
		if (hint.VariantType == Variant.Type.Array)
		{
			Array array2 = hint.AsGodotArray();
			for (int j = 0; j < array2.Count; j++)
			{
				Variant variant2 = array2[j];
				dictionary[variant2] = j;
			}
		}
		propertiesEnum.SetHintDictionary(dictionary);
		propertiesEnum.value = _value.AsInt32();
		return propertiesEnum;
	}

	public PropertiesFlag AddFlag(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = 0;
		}
		PropertiesFlag propertiesFlag = PropertiesFlagScene.Instantiate<PropertiesFlag>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesFlag);
		propertiesFlag.hintDictionary = hint.AsGodotDictionary();
		propertiesFlag.value = _value.AsInt32();
		return propertiesFlag;
	}

	public PropertiesArray AddArray(int index, Variant _value = default(Variant))
	{
		if (_value.VariantType == Variant.Type.Nil)
		{
			_value = new Array();
		}
		PropertiesArray propertiesArray = PropertiesArrayScene.Instantiate<PropertiesArray>(PackedScene.GenEditState.Disabled);
		AddPropertyEditor(index, _value, propertiesArray, inner: false);
		propertiesArray.hintType = hintType;
		propertiesArray.value = _value.AsGodotArray();
		return propertiesArray;
	}
}
