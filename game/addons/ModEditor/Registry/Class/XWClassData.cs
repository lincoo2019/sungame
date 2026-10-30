using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Registry.Class;

public sealed partial class XWClassData : RefCounted
{
	public string ClassName { get; set; } = "";

	public Color Color { get; set; } = Colors.White;

	public Script ScriptFile { get; set; }

	public Texture2D Icon { get; set; }

	public bool IsGodotClass { get; set; }

	public bool IsGlobalClass { get; set; }

	public string BaseClass { get; set; } = "";

	public XWClassData ParentClassData { get; set; }

	public string ScriptPath { get; set; } = "";

	public string IconPath { get; set; } = "";

	public Dictionary MethodNamesCache { get; } = new Dictionary();

	public bool MethodNamesCached { get; set; }

	public XWClassData()
	{
	}

	public XWClassData(string className = "", Color color = default(Color), Script scriptFile = null, Texture2D icon = null)
	{
		ClassName = className;
		Color = ((color == default(Color)) ? Colors.White : color);
		ScriptFile = scriptFile;
		Icon = icon;
	}
}
