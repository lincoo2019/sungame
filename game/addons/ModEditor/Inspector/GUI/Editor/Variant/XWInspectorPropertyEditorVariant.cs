using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Inspector;

public partial class XWInspectorPropertyEditorVariant : XWInspectorPropertyEditorBase
{
	private const string ScenePath = "res://addons/ModEditor/Inspector/GUI/Editor/Variant/XWInspectorPropertyEditorVariant.tscn";

	private OptionButton _typeOption;

	private VBoxContainer _editorContainer;

	private CheckBox _boolCheck;

	private SpinBox _intSpin;

	private SpinBox _floatSpin;

	private LineEdit _stringEdit;

	private ColorPickerButton _colorPicker;

	private Variant.Type _currentType;

	private readonly Dictionary<Variant.Type, int> _typeToIndex = new Dictionary<Variant.Type, int>();

	private readonly Dictionary<int, Variant.Type> _indexToType = new Dictionary<int, Variant.Type>();

	private SpinBox[] _vector2Edits;

	private SpinBox[] _vector2iEdits;

	private SpinBox[] _vector3Edits;

	private SpinBox[] _vector3iEdits;

	private SpinBox[] _vector4Edits;

	private SpinBox[] _vector4iEdits;

	private SpinBox[] _rect2Edits;

	private SpinBox[] _rect2iEdits;

	private static readonly Variant.Type[] SupportedTypes = new Variant.Type[13]
	{
		Variant.Type.Bool,
		Variant.Type.Int,
		Variant.Type.Float,
		Variant.Type.String,
		Variant.Type.Vector2,
		Variant.Type.Vector2I,
		Variant.Type.Vector3,
		Variant.Type.Vector3I,
		Variant.Type.Vector4,
		Variant.Type.Vector4I,
		Variant.Type.Color,
		Variant.Type.Rect2,
		Variant.Type.Rect2I
	};

	private static readonly string[] TypeNames = new string[13]
	{
		"bool", "int", "float", "String", "Vector2", "Vector2i", "Vector3", "Vector3i", "Vector4", "Vector4i",
		"Color", "Rect2", "Rect2i"
	};

	public static XWInspectorPropertyEditorVariant Create()
	{
		return ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Inspector/GUI/Editor/Variant/XWInspectorPropertyEditorVariant.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<XWInspectorPropertyEditorVariant>(PackedScene.GenEditState.Disabled);
	}

	public override void _Ready()
	{
		base._Ready();
		_typeOption = GetNode<OptionButton>("%TypeOption");
		_editorContainer = GetNode<VBoxContainer>("%EditorContainer");
		_boolCheck = GetNode<CheckBox>("%BoolCheck");
		_intSpin = GetNode<SpinBox>("%IntSpin");
		_floatSpin = GetNode<SpinBox>("%FloatSpin");
		_stringEdit = GetNode<LineEdit>("%StringEdit");
		_colorPicker = GetNode<ColorPickerButton>("%ColorPicker");
		XWInspectorPropertyEditorBase.NormalizeSpinBox(_intSpin);
		XWInspectorPropertyEditorBase.NormalizeSpinBox(_floatSpin);
		for (int i = 0; i < SupportedTypes.Length; i++)
		{
			_typeToIndex[SupportedTypes[i]] = i;
			_indexToType[i] = SupportedTypes[i];
			_typeOption.AddItem(TypeNames[i]);
		}
		_typeOption.ItemSelected += OnTypeSelected;
		_boolCheck.Toggled += delegate(bool v)
		{
			ValueChange(v);
		};
		_intSpin.ValueChanged += delegate(double v)
		{
			ValueChange((long)v);
		};
		_floatSpin.ValueChanged += delegate(double v)
		{
			ValueChange(v);
		};
		_stringEdit.TextChanged += delegate(string v)
		{
			ValueChange(v);
		};
		_colorPicker.ColorChanged += delegate(Color v)
		{
			ValueChange(v);
		};
		_vector2Edits = CreateSpinBoxRow(2, -3.4028235E+38, 3.4028235E+38, 0.01);
		_vector2iEdits = CreateSpinBoxRow(2, -2147483648.0, 2147483647.0, 1.0);
		_vector3Edits = CreateSpinBoxRow(3, -3.4028235E+38, 3.4028235E+38, 0.01);
		_vector3iEdits = CreateSpinBoxRow(3, -2147483648.0, 2147483647.0, 1.0);
		_vector4Edits = CreateSpinBoxRow(4, -3.4028235E+38, 3.4028235E+38, 0.01);
		_vector4iEdits = CreateSpinBoxRow(4, -2147483648.0, 2147483647.0, 1.0);
		_rect2Edits = CreateSpinBoxRow(4, -3.4028235E+38, 3.4028235E+38, 0.01);
		_rect2iEdits = CreateSpinBoxRow(4, -2147483648.0, 2147483647.0, 1.0);
	}

	private SpinBox[] CreateSpinBoxRow(int count, double minVal, double maxVal, double stepVal)
	{
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Visible = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer.AddThemeConstantOverride("separation", 2);
		SpinBox[] array = new SpinBox[count];
		for (int i = 0; i < count; i++)
		{
			SpinBox spinBox = new SpinBox
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				MinValue = minVal,
				MaxValue = maxVal,
				Step = stepVal
			};
			spinBox.ValueChanged += delegate
			{
				ValueChange(GetValue());
			};
			hBoxContainer.AddChild(spinBox, forceReadableName: false, InternalMode.Disabled);
			XWInspectorPropertyEditorBase.NormalizeSpinBox(spinBox);
			array[i] = spinBox;
		}
		_editorContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		return array;
	}

	public override void UpdateValue()
	{
		Variant propertyValue = GetPropertyValue();
		if (propertyValue.VariantType != Variant.Type.Nil)
		{
			Variant.Type variantType = propertyValue.VariantType;
			if (_typeToIndex.TryGetValue(variantType, out var value))
			{
				_typeOption.Select(value);
				_currentType = variantType;
				ShowEditorForType(variantType);
				SetEditorValue(propertyValue);
			}
		}
	}

	public override Variant GetValue()
	{
		Variant.Type currentType = _currentType;
		Variant.Type num = currentType - 1;
		if ((ulong)num <= 19uL)
		{
			switch ((int)num)
			{
			case 0:
				return _boolCheck.ButtonPressed;
			case 1:
				return (long)_intSpin.Value;
			case 2:
				return _floatSpin.Value;
			case 3:
				return _stringEdit.Text;
			case 4:
				return new Vector2((float)_vector2Edits[0].Value, (float)_vector2Edits[1].Value);
			case 5:
				return new Vector2I((int)_vector2iEdits[0].Value, (int)_vector2iEdits[1].Value);
			case 8:
				return new Vector3((float)_vector3Edits[0].Value, (float)_vector3Edits[1].Value, (float)_vector3Edits[2].Value);
			case 9:
				return new Vector3I((int)_vector3iEdits[0].Value, (int)_vector3iEdits[1].Value, (int)_vector3iEdits[2].Value);
			case 11:
				return new Vector4((float)_vector4Edits[0].Value, (float)_vector4Edits[1].Value, (float)_vector4Edits[2].Value, (float)_vector4Edits[3].Value);
			case 12:
				return new Vector4I((int)_vector4iEdits[0].Value, (int)_vector4iEdits[1].Value, (int)_vector4iEdits[2].Value, (int)_vector4iEdits[3].Value);
			case 19:
				return _colorPicker.Color;
			case 6:
				return new Rect2((float)_rect2Edits[0].Value, (float)_rect2Edits[1].Value, (float)_rect2Edits[2].Value, (float)_rect2Edits[3].Value);
			case 7:
				return new Rect2I((int)_rect2iEdits[0].Value, (int)_rect2iEdits[1].Value, (int)_rect2iEdits[2].Value, (int)_rect2iEdits[3].Value);
			}
		}
		return default(Variant);
	}

	private void ShowEditorForType(Variant.Type t)
	{
		_boolCheck.Visible = t == Variant.Type.Bool;
		_intSpin.Visible = t == Variant.Type.Int;
		_floatSpin.Visible = t == Variant.Type.Float;
		_stringEdit.Visible = t == Variant.Type.String;
		_colorPicker.Visible = t == Variant.Type.Color;
		_vector2Edits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Vector2);
		_vector2iEdits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Vector2I);
		_vector3Edits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Vector3);
		_vector3iEdits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Vector3I);
		_vector4Edits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Vector4);
		_vector4iEdits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Vector4I);
		_rect2Edits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Rect2);
		_rect2iEdits[0].GetParent().CallDeferred("set", "visible", t == Variant.Type.Rect2I);
	}

	private void SetEditorValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 19uL)
		{
			switch ((int)num)
			{
			case 0:
				_boolCheck.ButtonPressed = value.AsBool();
				break;
			case 1:
				_intSpin.Value = value.AsInt64();
				break;
			case 2:
				_floatSpin.Value = value.AsDouble();
				break;
			case 3:
				XWInspectorPropertyEditorBase.SetLineEditTextPreservingCaret(_stringEdit, value.AsString());
				break;
			case 4:
			{
				Vector2 vector3 = value.AsVector2();
				_vector2Edits[0].Value = vector3.X;
				_vector2Edits[1].Value = vector3.Y;
				break;
			}
			case 5:
			{
				Vector2I vector2I = value.AsVector2I();
				_vector2iEdits[0].Value = vector2I.X;
				_vector2iEdits[1].Value = vector2I.Y;
				break;
			}
			case 8:
			{
				Vector3 vector2 = value.AsVector3();
				_vector3Edits[0].Value = vector2.X;
				_vector3Edits[1].Value = vector2.Y;
				_vector3Edits[2].Value = vector2.Z;
				break;
			}
			case 9:
			{
				Vector3I vector3I = value.AsVector3I();
				_vector3iEdits[0].Value = vector3I.X;
				_vector3iEdits[1].Value = vector3I.Y;
				_vector3iEdits[2].Value = vector3I.Z;
				break;
			}
			case 11:
			{
				Vector4 vector = value.AsVector4();
				_vector4Edits[0].Value = vector.X;
				_vector4Edits[1].Value = vector.Y;
				_vector4Edits[2].Value = vector.Z;
				_vector4Edits[3].Value = vector.W;
				break;
			}
			case 12:
			{
				Vector4I vector4I = value.AsVector4I();
				_vector4iEdits[0].Value = vector4I.X;
				_vector4iEdits[1].Value = vector4I.Y;
				_vector4iEdits[2].Value = vector4I.Z;
				_vector4iEdits[3].Value = vector4I.W;
				break;
			}
			case 19:
				_colorPicker.Color = value.AsColor();
				break;
			case 6:
			{
				Rect2 rect = value.AsRect2();
				_rect2Edits[0].Value = rect.Position.X;
				_rect2Edits[1].Value = rect.Position.Y;
				_rect2Edits[2].Value = rect.Size.X;
				_rect2Edits[3].Value = rect.Size.Y;
				break;
			}
			case 7:
			{
				Rect2I rect2I = value.AsRect2I();
				_rect2iEdits[0].Value = rect2I.Position.X;
				_rect2iEdits[1].Value = rect2I.Position.Y;
				_rect2iEdits[2].Value = rect2I.Size.X;
				_rect2iEdits[3].Value = rect2I.Size.Y;
				break;
			}
			case 10:
			case 13:
			case 14:
			case 15:
			case 16:
			case 17:
			case 18:
				break;
			}
		}
	}

	private void OnTypeSelected(long index)
	{
		if (_indexToType.TryGetValue((int)index, out var value))
		{
			_currentType = value;
			ShowEditorForType(value);
			Variant defaultForType = GetDefaultForType(value);
			SetEditorValue(defaultForType);
			ValueChange(defaultForType);
		}
	}

	private static Variant GetDefaultForType(Variant.Type t)
	{
		Variant.Type num = t - 1;
		if ((ulong)num <= 19uL)
		{
			switch ((int)num)
			{
			case 0:
				return false;
			case 1:
				return 0L;
			case 2:
				return 0.0;
			case 3:
				return "";
			case 4:
				return Vector2.Zero;
			case 5:
				return Vector2I.Zero;
			case 8:
				return Vector3.Zero;
			case 9:
				return Vector3I.Zero;
			case 11:
				return Vector4.Zero;
			case 12:
				return Vector4I.Zero;
			case 19:
				return default(Color);
			case 6:
				return default(Rect2);
			case 7:
				return default(Rect2I);
			}
		}
		return default(Variant);
	}
}
