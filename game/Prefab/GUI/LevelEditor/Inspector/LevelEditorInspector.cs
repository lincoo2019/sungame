using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorInspector : PanelContainer
{
	private Label titleLabel;

	private PropertiesBox propertiesBox;

	public Dictionary nowCheckDictionary = new Dictionary();

	public GodotObject nowObj;

	[Export(PropertyHint.None, "")]
	public string title { get; set; } = "检查器";

	public override void _Ready()
	{
		titleLabel = GetNode<Label>("%TitleLabel");
		propertiesBox = GetNode<PropertiesBox>("%PropertiesBox");
		propertiesBox.OnValueChanged += ChangeValue;
		titleLabel.Text = title;
	}

	public void Init(GodotObject obj, Dictionary propertiesData)
	{
		ReFresh(propertiesData);
		nowObj = obj;
	}

	public void ReFresh(Dictionary propertiesData)
	{
		Clear();
		foreach (Variant key in propertiesData.Keys)
		{
			string text = key.AsString();
			Dictionary dictionary = propertiesData[text].AsGodotDictionary();
			propertiesBox.add_group(text);
			nowCheckDictionary.Merge(dictionary, overwrite: true);
			foreach (Variant key2 in dictionary.Keys)
			{
				string text2 = key2.AsString();
				AddProperty(dictionary[text2].AsGodotDictionary(), text2, text + "/" + text2);
			}
			propertiesBox.end_group();
		}
	}

	public PropertiesBase AddProperty(Dictionary properties, string propertiesShowName, string path = "")
	{
		if (!nowCheckDictionary.ContainsKey(propertiesShowName))
		{
			nowCheckDictionary[propertiesShowName] = properties;
		}
		GodotObject godotObject = properties["Object"].AsGodotObject();
		string text = properties["Type"].AsString();
		string text2 = properties["Property"].AsString();
		Variant restValue = default(Variant);
		if (properties.ContainsKey("Rest"))
		{
			restValue = properties["Rest"];
		}
		Variant value = godotObject.GetIndexed(text2);
		bool isTargetObj = true;
		Variant variant = false;
		if (value.VariantType == Variant.Type.Dictionary)
		{
			value = value.AsGodotDictionary()[propertiesShowName];
			isTargetObj = false;
			variant = true;
		}
		if (properties.ContainsKey("Close"))
		{
			variant = properties["Close"];
		}
		PropertiesBase propertiesBase = null;
		switch (text)
		{
		case "Bool":
			propertiesBase = propertiesBox.add_bool(godotObject, text2, propertiesShowName, value.AsBool(), restValue);
			break;
		case "Int":
			propertiesBase = propertiesBox.add_int(godotObject, text2, propertiesShowName, value.AsInt32(), restValue);
			break;
		case "Float":
			propertiesBase = propertiesBox.add_float(godotObject, text2, propertiesShowName, value.AsDouble(), restValue);
			break;
		case "String":
			propertiesBase = propertiesBox.add_string(godotObject, text2, propertiesShowName, value.AsString(), restValue);
			break;
		case "MultilineString":
			propertiesBase = propertiesBox.add_string(godotObject, text2, propertiesShowName, value.AsString(), restValue, isMultiline: true);
			break;
		case "Color":
			if (restValue.VariantType == Variant.Type.String)
			{
				restValue = GetParseValue("Color", restValue.AsString());
			}
			propertiesBase = propertiesBox.add_color(godotObject, text2, propertiesShowName, value.AsColor(), restValue);
			break;
		case "Vector2":
			if (restValue.VariantType == Variant.Type.String)
			{
				restValue = GetParseValue("Vector2", restValue.AsString());
			}
			propertiesBase = propertiesBox.add_vector2(godotObject, text2, propertiesShowName, value.AsVector2(), restValue);
			break;
		case "Vector4i":
			propertiesBase = propertiesBox.add_vector4i(godotObject, text2, propertiesShowName, value.AsVector4I(), restValue);
			break;
		case "Enum":
		{
			Variant hint4 = properties["Hint"];
			propertiesBase = propertiesBox.add_enum(godotObject, text2, propertiesShowName, hint4, value, restValue);
			break;
		}
		case "Flag":
		{
			Variant hint3 = properties["Hint"];
			propertiesBase = propertiesBox.add_flag(godotObject, text2, propertiesShowName, hint3, value.AsInt32(), restValue);
			break;
		}
		case "File":
		{
			Variant hint2 = properties["Hint"];
			propertiesBase = propertiesBox.add_file_select(godotObject, text2, propertiesShowName, hint2, value, restValue);
			break;
		}
		case "Map":
			propertiesBase = propertiesBox.add_map_select(godotObject, text2, propertiesShowName, value, restValue);
			break;
		case "Room":
		{
			Variant hint = properties["Hint"];
			propertiesBase = propertiesBox.add_room_select(godotObject, text2, propertiesShowName, hint, value, restValue);
			break;
		}
		case "Array":
		{
			string text3 = "";
			if (properties.ContainsKey("Hint"))
			{
				text3 = properties["Hint"].AsString();
			}
			string hintType = properties["HintType"].AsString();
			propertiesBase = propertiesBox.add_array(godotObject, text2, propertiesShowName, text3, hintType, godotObject.GetIndexed(text2).AsGodotArray(), restValue);
			break;
		}
		case "ArrayVector2":
			propertiesBase = propertiesBox.add_array_vector2(godotObject, text2, propertiesShowName, value.AsGodotArray<Vector2>(), restValue);
			break;
		}
		if (propertiesBase != null)
		{
			propertiesBase.isTargetObj = isTargetObj;
			propertiesBase.inspector = this;
			propertiesBase.path = path;
			propertiesBase.showCloseButton = variant.AsBool();
		}
		return propertiesBase;
	}

	public static Variant GetRestValue(string type)
	{
		return type switch
		{
			"Bool" => false, 
			"Int" => 0, 
			"Float" => 0.0, 
			"String" => "", 
			"Color" => Colors.White, 
			"Vector2" => Vector2.Zero, 
			"Array" => new Godot.Collections.Array(), 
			"ArrayVector2" => new Godot.Collections.Array(), 
			_ => default(Variant), 
		};
	}

	public static bool GetCanParseValue(string type)
	{
		switch (type)
		{
		case "Color":
		case "Float":
		case "Bool":
		case "Int":
		case "String":
		case "Vector2":
			return true;
		case "Array":
		case "ArrayVector2":
			return false;
		default:
			return false;
		}
	}

	public static Variant GetParseValue(string type, string str)
	{
		switch (type)
		{
		case "Bool":
			return str == "true";
		case "Int":
			return str.ToInt();
		case "Float":
			return str.ToFloat();
		case "String":
			return str;
		case "Color":
		{
			string[] array3 = str.Split(",", StringSplitOptions.RemoveEmptyEntries);
			float[] array4 = new float[array3.Length];
			for (int j = 0; j < array3.Length; j++)
			{
				array4[j] = array3[j].ToFloat();
			}
			return new Color(array4[0], array4[1], array4[2], array4[3]);
		}
		case "Vector2":
		{
			string[] array = str.Split(",", StringSplitOptions.RemoveEmptyEntries);
			float[] array2 = new float[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = array[i].ToFloat();
			}
			return new Vector2(array2[0], array2[1]);
		}
		default:
			return default(Variant);
		}
	}

	public static string GetTypeString(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 4uL)
		{
			switch ((int)num)
			{
			case 0:
				return "Bool";
			case 1:
				return "Int";
			case 2:
				return "Float";
			case 3:
				return "String";
			case 4:
				return "Vector2";
			}
		}
		switch (variantType)
		{
		case Variant.Type.Color:
			return "Color";
		case Variant.Type.Array:
		{
			Godot.Collections.Array array = value.AsGodotArray();
			if (array.Count > 0 && array[0].VariantType == Variant.Type.Vector2)
			{
				return "ArrayVector2";
			}
			return "Array";
		}
		default:
			return "";
		}
	}

	public void Clear()
	{
		nowObj = null;
		propertiesBox.Clear();
		nowCheckDictionary.Clear();
	}

	public void ChangeValue(StringName key, Variant newValue)
	{
		Dictionary dictionary = nowCheckDictionary[key].AsGodotDictionary();
		GodotObject godotObject = dictionary["Object"].AsGodotObject();
		string text = dictionary["Property"].AsString();
		PropertiesBase propertiesBase = propertiesBox._keys[key].As<PropertiesBase>();
		Variant indexed = godotObject.GetIndexed(text);
		if (!propertiesBase.isTargetObj)
		{
			indexed.AsGodotDictionary()[key] = newValue;
		}
		else
		{
			godotObject.SetIndexed(text, newValue);
		}
		if (dictionary.ContainsKey("Set"))
		{
			dictionary["Set"].AsCallable().Call();
		}
	}
}
