using System.Collections.Generic;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.Registry;

public sealed partial class XWTypeRegistry : RefCounted
{
	private static XWTypeRegistry _instance;

	private bool _isInit;

	private readonly Dictionary<Variant.Type, XWTypeData> _typeDictionary = new Dictionary<Variant.Type, XWTypeData>();

	private readonly Dictionary<int, PackedScene> _hintEditorDictionary = new Dictionary<int, PackedScene>();

	private PackedScene _resourcePropertyEditorScene;

	public static XWTypeRegistry Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new XWTypeRegistry();
				_instance.Initialize();
			}
			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}

	public void Initialize()
	{
		if (!_isInit)
		{
			_isInit = true;
			RegisterInit();
		}
	}

	public void Init()
	{
		Initialize();
	}

	private void RegisterInit()
	{
		RegisterEditor(LoadTypeData(Variant.Type.Bool, "bool", false, "false", new Color(0.435f, 0.569f, 0.941f), "Bool/XWInspectorPropertyEditorBool.tscn", "bool.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Int, "int", 0, "0", new Color(0.353f, 0.733f, 0.937f), "Integer/XWInspectorPropertyEditorInteger.tscn", "int.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Float, "float", 0.0, "0.0", new Color(0.208f, 0.831f, 0.957f), "Float/XWInspectorPropertyEditorFloat.tscn", "float.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.String, "String", "", "\"\"", new Color(0.271f, 0.576f, 0.925f), "String/XWInspectorPropertyEditorString.tscn", "String.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Color, "Color", default(Color), "Color.WHITE", new Color(0.373f, 1f, 0.592f), "Color/XWInspectorPropertyEditorColor.tscn", "Color.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.StringName, "StringName", default(Variant), "&\"\"", new Color(0.561f, 0.745f, 0.953f), "StringName/XWInspectorPropertyEditorStringName.tscn", "StringName.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.NodePath, "NodePath", default(Variant), "^\"\"", new Color(0.255f, 0.478f, 0.925f), "NodePath/XWInspectorPropertyEditorNodePath.tscn", "NodePath.svg"));
		RegisterHintEditor(2, LoadScene("Enum/XWInspectorPropertyEditorEnum.tscn"));
		RegisterHintEditor(18, LoadScene("Multiline/XWInspectorPropertyEditorMultiline.tscn"));
		RegisterHintEditor(6, LoadScene("Flags/XWInspectorPropertyEditorFlags.tscn"));
		RegisterHintEditor(7, LoadScene("Layers/XWInspectorPropertyEditorLayers.tscn"));
		RegisterHintEditor(8, LoadScene("Layers/XWInspectorPropertyEditorLayers.tscn"));
		RegisterHintEditor(9, LoadScene("Layers/XWInspectorPropertyEditorLayers.tscn"));
		RegisterHintEditor(10, LoadScene("Layers/XWInspectorPropertyEditorLayers.tscn"));
		RegisterHintEditor(11, LoadScene("Layers/XWInspectorPropertyEditorLayers.tscn"));
		RegisterHintEditor(12, LoadScene("Layers/XWInspectorPropertyEditorLayers.tscn"));
		RegisterHintEditor(4, LoadScene("Easing/XWInspectorPropertyEditorEasing.tscn"));
		RegisterHintEditor(23, LoadScene("ClassName/XWInspectorPropertyEditorClassName.tscn"));
		RegisterHintEditor(3, LoadScene("TextEnum/XWInspectorPropertyEditorTextEnum.tscn"));
		RegisterHintEditor(32, LoadScene("Locale/XWInspectorPropertyEditorLocale.tscn"));
		RegisterHintEditor(22, LoadScene("ObjectID/XWInspectorPropertyEditorObjectID.tscn"));
		RegisterHintEditor(13, LoadScene("Path/XWInspectorPropertyEditorPath.tscn"));
		RegisterHintEditor(44, LoadScene("Path/XWInspectorPropertyEditorPath.tscn"));
		RegisterHintEditor(14, LoadScene("Path/XWInspectorPropertyEditorPath.tscn"));
		RegisterHintEditor(15, LoadScene("Path/XWInspectorPropertyEditorPath.tscn"));
		RegisterHintEditor(16, LoadScene("Path/XWInspectorPropertyEditorPath.tscn"));
		RegisterHintEditor(27, LoadScene("Path/XWInspectorPropertyEditorPath.tscn"));
		RegisterHintEditor(28, LoadScene("Path/XWInspectorPropertyEditorPath.tscn"));
		RegisterEditor(LoadTypeData(Variant.Type.Vector2, "Vector2", Vector2.Zero, "Vector2.ZERO", new Color(0.675f, 0.451f, 0.945f), "Vector2/XWInspectorPropertyEditorVector2.tscn", "Vector2.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Vector2I, "Vector2i", Vector2I.Zero, "Vector2i.ZERO", new Color(0.675f, 0.451f, 0.945f), "Vector2i/XWInspectorPropertyEditorVector2i.tscn", "Vector2i.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Vector3, "Vector3", Vector3.Zero, "Vector3.ZERO", new Color(0.922f, 0.639f, 0.965f), "Vector3/XWInspectorPropertyEditorVector3.tscn", "Vector3.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Vector3I, "Vector3i", Vector3I.Zero, "Vector3i.ZERO", new Color(0.922f, 0.639f, 0.965f), "Vector3i/XWInspectorPropertyEditorVector3i.tscn", "Vector3i.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Vector4, "Vector4", default(Variant), "Vector4.ZERO", new Color(0.847f, 0.369f, 0.671f), "Vector4/XWInspectorPropertyEditorVector4.tscn", "Vector4.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Vector4I, "Vector4i", default(Variant), "Vector4i.ZERO", new Color(0.847f, 0.369f, 0.671f), "Vector4i/XWInspectorPropertyEditorVector4i.tscn", "Vector4i.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Rect2, "Rect2", default(Variant), "Rect2()", new Color(0.945f, 0.451f, 0.561f), "Rect2/XWInspectorPropertyEditorRect2.tscn", "Rect2.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Rect2I, "Rect2i", default(Variant), "Rect2i()", new Color(0.945f, 0.451f, 0.561f), "Rect2i/XWInspectorPropertyEditorRect2i.tscn", "Rect2i.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Plane, "Plane", default(Variant), "Plane()", new Color(0.969f, 0.286f, 0.286f), "Plane/XWInspectorPropertyEditorPlane.tscn", "Plane.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Quaternion, "Quaternion", default(Variant), "Quaternion.IDENTITY", new Color(0.953f, 0.553f, 0.733f), "Quaternion/XWInspectorPropertyEditorQuaternion.tscn", "Quaternion.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Aabb, "AABB", default(Variant), "AABB()", new Color(0.949f, 0.49f, 0.592f), "AABB/XWInspectorPropertyEditorAABB.tscn", "AABB.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Basis, "Basis", default(Variant), "Basis.IDENTITY", new Color(0.882f, 0.925f, 0.255f), "Basis/XWInspectorPropertyEditorBasis.tscn", "Basis.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Transform2D, "Transform2D", default(Variant), "Transform2D.IDENTITY", new Color(0.835f, 0.953f, 0.553f), "Transform2D/XWInspectorPropertyEditorTransform2D.tscn", "Transform2D.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Transform3D, "Transform3D", default(Variant), "Transform3D.IDENTITY", new Color(0.976f, 0.737f, 0.561f), "Transform3D/XWInspectorPropertyEditorTransform3D.tscn", "Transform3D.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Projection, "Projection", default(Variant), "Projection()", new Color(0.8f, 0.667f, 0.965f), "Projection/XWInspectorPropertyEditorProjection.tscn", "Projection.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Nil, "Variant", default(Variant), "null", new Color(0.282f, 0.929f, 0.69f), "Variant/XWInspectorPropertyEditorVariant.tscn", "Nil.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Object, "Object", default(Variant), "null", new Color(0.878f, 0.878f, 0.878f), "Object/XWInspectorPropertyEditorObject.tscn", "Object.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Array, "Array", default(Variant), "[]", new Color(0.878f, 0.878f, 0.878f), "Array/XWInspectorPropertyEditorArray.tscn", "Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Dictionary, "Dictionary", default(Variant), "{}", new Color(0.302f, 0.824f, 0.557f), "Dictionary/XWInspectorPropertyEditorDictionary.tscn", "Dictionary.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedByteArray, "PackedByteArray", default(Variant), "PackedByteArray()", new Color(0.624f, 1f, 0.753f), "PackedByteArray/XWInspectorPropertyEditorPackedByteArray.tscn", "PackedByteArray.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedInt32Array, "PackedInt32Array", default(Variant), "PackedInt32Array()", new Color(0.612f, 0.839f, 0.961f), "PackedInt32Array/XWInspectorPropertyEditorPackedInt32Array.tscn", "PackedInt32Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedInt64Array, "PackedInt64Array", default(Variant), "PackedInt64Array()", new Color(0.612f, 0.839f, 0.961f), "PackedInt64Array/XWInspectorPropertyEditorPackedInt64Array.tscn", "PackedInt64Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedFloat32Array, "PackedFloat32Array", default(Variant), "PackedFloat32Array()", new Color(0.522f, 0.898f, 0.973f), "PackedFloat32Array/XWInspectorPropertyEditorPackedFloat32Array.tscn", "PackedFloat32Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedFloat64Array, "PackedFloat64Array", default(Variant), "PackedFloat64Array()", new Color(0.522f, 0.898f, 0.973f), "PackedFloat64Array/XWInspectorPropertyEditorPackedFloat64Array.tscn", "PackedFloat64Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedStringArray, "PackedStringArray", default(Variant), "PackedStringArray()", new Color(0.561f, 0.745f, 0.953f), "PackedStringArray/XWInspectorPropertyEditorPackedStringArray.tscn", "PackedStringArray.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedVector2Array, "PackedVector2Array", default(Variant), "PackedVector2Array()", new Color(0.804f, 0.671f, 0.965f), "PackedVector2Array/XWInspectorPropertyEditorPackedVector2Array.tscn", "PackedVector2Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedVector3Array, "PackedVector3Array", default(Variant), "PackedVector3Array()", new Color(0.855f, 0.584f, 0.894f), "PackedVector3Array/XWInspectorPropertyEditorPackedVector3Array.tscn", "PackedVector3Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedColorArray, "PackedColorArray", default(Variant), "PackedColorArray()", new Color(0.502f, 1f, 0.271f), "PackedColorArray/XWInspectorPropertyEditorPackedColorArray.tscn", "PackedColorArray.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.PackedVector4Array, "PackedVector4Array", default(Variant), "PackedVector4Array()", new Color(0.8f, 0.667f, 0.965f), "PackedVector4Array/XWInspectorPropertyEditorPackedVector4Array.tscn", "PackedVector4Array.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Callable, "Callable", default(Variant), "Callable()", new Color(0.7f, 0.8f, 0.6f), "Callable/XWInspectorPropertyEditorCallable.tscn", "Callable.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Rid, "RID", default(Variant), "RID()", new Color(0.255f, 0.925f, 0.502f), "RID/XWInspectorPropertyEditorRID.tscn", "RID.svg"));
		RegisterEditor(LoadTypeData(Variant.Type.Signal, "Signal", default(Variant), "Signal()", new Color(1f, 0.373f, 0.373f), "Signal/XWInspectorPropertyEditorSignal.tscn", "Signal.svg"));
		_resourcePropertyEditorScene = LoadScene("Resource/XWInspectorPropertyEditorResource.tscn");
	}

	private XWTypeData LoadTypeData(Variant.Type type, string name, Variant defaultValue, string defaultValueString, Color color, string sceneRelativePath, string iconFileName = null)
	{
		PackedScene editor = LoadScene(sceneRelativePath);
		Texture2D icon = LoadTypeIcon(iconFileName);
		return new XWTypeData(type, name, defaultValue, defaultValueString, color, editor, icon);
	}

	private static PackedScene LoadScene(string sceneRelativePath)
	{
		string path = "res://addons/ModEditor/Inspector/GUI/Editor/" + sceneRelativePath;
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
	}

	private static Texture2D LoadTypeIcon(string iconFileName)
	{
		if (string.IsNullOrEmpty(iconFileName))
		{
			return null;
		}
		string path = "res://addons/ModEditor/Icons/TypeIcon/" + iconFileName;
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}

	public void RegisterEditor(XWTypeData data)
	{
		_typeDictionary[data.Type] = data;
	}

	public void RegisterHintEditor(int hint, PackedScene editor)
	{
		_hintEditorDictionary[hint] = editor;
	}

	public XWInspectorPropertyEditorBase GetHintEditor(int hint)
	{
		if (_hintEditorDictionary.TryGetValue(hint, out var value) && value != null)
		{
			return value.Instantiate<XWInspectorPropertyEditorBase>(PackedScene.GenEditState.Disabled);
		}
		return null;
	}

	public XWInspectorPropertyEditorBase GetEditorFromObjectProperty(GodotObject obj, StringName propertyName = null, int hint = 0, string hintString = "")
	{
		Variant variant = ReadPropertyValue(obj, propertyName);
		if (IsContainerValueType(variant.VariantType))
		{
			XWInspectorPropertyEditorBase typeEditor = GetTypeEditor(variant.VariantType);
			if (GodotObject.IsInstanceValid(typeEditor))
			{
				return typeEditor;
			}
		}
		XWInspectorPropertyEditorBase hintEditor = GetHintEditor(hint);
		if (GodotObject.IsInstanceValid(hintEditor))
		{
			return hintEditor;
		}
		if (hint == 17)
		{
			if (variant.VariantType == Variant.Type.Object && variant.As<GodotObject>() is Resource resObj)
			{
				return GetResourceValueEditor(resObj);
			}
			return GetResourcePropertyEditor();
		}
		if (propertyName != (StringName)"" && variant.VariantType != Variant.Type.Object)
		{
			XWInspectorPropertyEditorBase typeEditor2 = GetTypeEditor(variant.VariantType);
			if (GodotObject.IsInstanceValid(typeEditor2))
			{
				if (hint == 1)
				{
					typeEditor2.SetupRangeHint(hintString);
				}
				return typeEditor2;
			}
		}
		if (variant.VariantType == Variant.Type.Object)
		{
			GodotObject godotObject = variant.As<GodotObject>();
			if (GodotObject.IsInstanceValid(godotObject) && godotObject is Resource)
			{
				return GetResourceValueEditor((Resource)godotObject);
			}
		}
		PackedScene editor = XWInspectorPropertyRegistry.GetEditor(obj);
		if (editor != null)
		{
			return editor.Instantiate<XWInspectorPropertyEditorBase>(PackedScene.GenEditState.Disabled);
		}
		if (propertyName != (StringName)"" && variant.VariantType == Variant.Type.Object)
		{
			XWInspectorPropertyEditorBase typeEditor3 = GetTypeEditor(Variant.Type.Object);
			if (GodotObject.IsInstanceValid(typeEditor3))
			{
				return typeEditor3;
			}
		}
		return null;
	}

	private static Variant ReadPropertyValue(GodotObject obj, StringName propertyName)
	{
		if (propertyName != null && propertyName != (StringName)"" && GodotObject.IsInstanceValid(obj))
		{
			return obj.Get(propertyName);
		}
		return default(Variant);
	}

	private static bool IsContainerValueType(Variant.Type valueType)
	{
		if (valueType >= Variant.Type.Array)
		{
			return valueType <= Variant.Type.PackedVector4Array;
		}
		return false;
	}

	public XWInspectorPropertyEditorBase GetResourcePropertyEditor()
	{
		if (_resourcePropertyEditorScene == null)
		{
			return GetTypeEditor(Variant.Type.Object);
		}
		return _resourcePropertyEditorScene.Instantiate<XWInspectorPropertyEditorBase>(PackedScene.GenEditState.Disabled);
	}

	public XWInspectorPropertyEditorBase GetResourceValueEditor(Resource resObj)
	{
		if (GodotObject.IsInstanceValid(resObj))
		{
			PackedScene editor = XWInspectorPropertyRegistry.GetEditor(resObj);
			if (editor != null)
			{
				return editor.Instantiate<XWInspectorPropertyEditorBase>(PackedScene.GenEditState.Disabled);
			}
		}
		return GetResourcePropertyEditor();
	}

	public XWInspectorPropertyEditorBase GetTypeEditor(Variant.Type type)
	{
		if (_typeDictionary.TryGetValue(type, out var value) && value?.Editor != null)
		{
			return value.Editor.Instantiate<XWInspectorPropertyEditorBase>(PackedScene.GenEditState.Disabled);
		}
		return null;
	}

	public bool HasType(Variant.Type type)
	{
		return _typeDictionary.ContainsKey(type);
	}

	public string GetTypeName(Variant.Type type)
	{
		if (!_typeDictionary.TryGetValue(type, out var value))
		{
			return "";
		}
		return value.Name;
	}

	public Variant GetTypeDefaultValue(Variant.Type type)
	{
		if (!_typeDictionary.TryGetValue(type, out var value))
		{
			return default(Variant);
		}
		return value.DefaultValue;
	}

	public string GetTypeDefaultValueString(Variant.Type type)
	{
		if (!_typeDictionary.TryGetValue(type, out var value))
		{
			return "";
		}
		return value.DefaultValueString;
	}

	public Color GetTypeColor(Variant.Type type)
	{
		if (!_typeDictionary.TryGetValue(type, out var value))
		{
			return Colors.Gray;
		}
		return value.Color;
	}

	public Texture2D GetTypeIcon(Variant.Type type)
	{
		if (!_typeDictionary.TryGetValue(type, out var value))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(value.Icon);
	}

	public IReadOnlyCollection<XWTypeData> GetAllType()
	{
		return _typeDictionary.Values;
	}
}
