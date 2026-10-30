using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Registry;

public static class XWInspectorPropertyRegistry
{
	private static bool _isInit = false;

	private static readonly Dictionary<string, PackedScene> _dict = new Dictionary<string, PackedScene>();

	public static void Init()
	{
		if (!_isInit)
		{
			_isInit = true;
			PackedScene editor = LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/Resource/XWInspectorPropertyEditorResourceSummary.tscn");
			RegisterEditor("Texture2D", editor);
			RegisterEditor("CompressedTexture2D", editor);
			RegisterEditor("ImageTexture", editor);
			RegisterEditor("PortableCompressedTexture2D", editor);
			RegisterEditor("AudioStream", editor);
			RegisterEditor("AudioStreamWav", editor);
			RegisterEditor("AudioStreamOggVorbis", editor);
			RegisterEditor("AudioStreamMP3", editor);
			RegisterEditor("Font", editor);
			RegisterEditor("FontFile", editor);
			RegisterEditor("PackedScene", editor);
			RegisterEditor("Script", editor);
			RegisterEditor("CSharpScript", editor);
			PackedScene editor2 = LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/GradientTexture/XWInspectorPropertyEditorGradientTexture.tscn");
			PackedScene editor3 = LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/CurveTexture/XWInspectorPropertyEditorCurveTexture.tscn");
			PackedScene editor4 = LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/StyleBox/XWInspectorPropertyEditorStyleBox.tscn");
			PackedScene editor5 = LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/Material/XWInspectorPropertyEditorMaterial.tscn");
			PackedScene editor6 = LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/Theme/XWInspectorPropertyEditorTheme.tscn");
			RegisterEditor("Material", editor5);
			RegisterEditor("ShaderMaterial", editor5);
			RegisterEditor("CanvasItemMaterial", editor5);
			RegisterEditor("BaseMaterial3D", editor5);
			RegisterEditor("StandardMaterial3D", editor5);
			RegisterEditor("GradientTexture1D", editor2);
			RegisterEditor("GradientTexture2D", editor2);
			RegisterEditor("CurveTexture", editor3);
			RegisterEditor("CurveXYZTexture", editor3);
			RegisterEditor("StyleBox", editor4);
			RegisterEditor("StyleBoxFlat", editor4);
			RegisterEditor("StyleBoxTexture", editor4);
			RegisterEditor("StyleBoxLine", editor4);
			RegisterEditor("Theme", editor6);
			RegisterEditor("Curve", LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/Curve/XWInspectorPropertyEditorCurve.tscn"));
			RegisterEditor("Gradient", LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/Gradient/XWInspectorPropertyEditorGradient.tscn"));
			RegisterEditor("XWBPNodePortData", LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/XWBPNodePortData/XWInspectorPropertyEditorXWBPNodePortData.tscn"));
			RegisterEditor("XWBPVariableData", LoadScene("res://addons/ModEditor/Inspector/ResourceInspectorExtend/XWBPVariableData/XWInspectorPropertyEditorXWBPVariableData.tscn"));
		}
	}

	private static PackedScene LoadScene(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
	}

	public static void RegisterEditor(string objectClassName, PackedScene editor)
	{
		_dict[objectClassName] = editor;
	}

	public static PackedScene GetEditor(GodotObject obj)
	{
		if (!GodotObject.IsInstanceValid(obj))
		{
			return null;
		}
		Script script = obj.GetScript().As<Script>();
		if (GodotObject.IsInstanceValid(script))
		{
			StringName globalName = script.GetGlobalName();
			if (!string.IsNullOrEmpty(globalName) && _dict.TryGetValue(globalName, out var value))
			{
				return value;
			}
		}
		string text = obj.GetClass();
		if (_dict.TryGetValue(text, out var value2))
		{
			return value2;
		}
		foreach (KeyValuePair<string, PackedScene> item in _dict)
		{
			if (ClassDB.ClassExists(text) && ClassDB.ClassExists(item.Key) && ClassDB.IsParentClass(text, item.Key))
			{
				return item.Value;
			}
		}
		return null;
	}

	public static PackedScene GetEditorFromObjectName(string objectName)
	{
		if (!_dict.TryGetValue(objectName, out var value))
		{
			return null;
		}
		return value;
	}
}
