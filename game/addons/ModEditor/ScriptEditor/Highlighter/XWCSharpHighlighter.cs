using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public partial class XWCSharpHighlighter : CodeHighlighter
{
	private static readonly Color KeywordColor = new Color(0.69f, 0.49f, 0.85f);

	private static readonly Color TypeColor = new Color(0.42f, 0.67f, 0.93f);

	private static readonly Color StringColor = new Color(0.78f, 0.6f, 0.4f);

	private static readonly Color CommentColor = new Color(0.5f, 0.6f, 0.5f);

	private static readonly Color CustomNumberColor = new Color(0.9f, 0.78f, 0.49f);

	private static readonly Color CustomFunctionColor = new Color(0.42f, 0.67f, 0.93f);

	private static readonly Color CustomSymbolColor = new Color(0.7f, 0.7f, 0.7f);

	private static readonly string[] Keywords = new string[98]
	{
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
		"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
		"event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
		"if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
		"new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
		"readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
		"struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
		"unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "var", "get", "set",
		"value", "async", "await", "yield", "partial", "where", "select", "from", "let", "orderby",
		"group", "by", "into", "join", "on", "equals", "ascending", "descending"
	};

	private static readonly string[] GodotTypes = new string[74]
	{
		"Godot", "Node", "Resource", "Variant", "Vector2", "Vector2I", "Vector3", "Vector3I", "Vector4", "Vector4I",
		"Color", "Rect2", "Rect2I", "Transform2D", "Transform3D", "Basis", "Quaternion", "StringName", "NodePath", "PackedScene",
		"PackedByteArray", "PackedInt32Array", "PackedInt64Array", "PackedFloat32Array", "PackedFloat64Array", "PackedStringArray", "PackedVector2Array", "PackedVector3Array", "PackedColorArray", "GD",
		"EmitSignal", "Callable", "Signal", "Array", "Dictionary", "RefCounted", "GodotObject", "InputEvent", "Tween", "Timer",
		"Texture2D", "Sprite2D", "Node2D", "Node3D", "CanvasItem", "Control", "Button", "Label", "LineEdit", "TextEdit",
		"CodeEdit", "Tree", "TreeItem", "Window", "ConfirmationDialog", "FileDialog", "Panel", "PanelContainer", "VBoxContainer", "HBoxContainer",
		"MarginContainer", "HSplitContainer", "VSplitContainer", "TabContainer", "ScrollContainer", "GraphEdit", "GraphNode", "Area2D", "CollisionShape2D", "AnimatedSprite2D",
		"RigidBody2D", "CharacterBody2D", "StaticBody2D", "Camera2D"
	};

	public XWCSharpHighlighter()
	{
		SetFunctionColor(CustomFunctionColor);
		SetNumberColor(CustomNumberColor);
		SetSymbolColor(CustomSymbolColor);
		SetMemberVariableColor(TypeColor);
		string[] keywords = Keywords;
		foreach (string keyword in keywords)
		{
			AddKeywordColor(keyword, KeywordColor);
		}
		keywords = GodotTypes;
		foreach (string keyword2 in keywords)
		{
			AddKeywordColor(keyword2, TypeColor);
		}
		AddColorRegion("/*", "*/", CommentColor);
		AddColorRegion("//", "", CommentColor, lineOnly: true);
		AddColorRegion("\"", "\"", StringColor);
		AddColorRegion("'", "'", StringColor);
	}
}
