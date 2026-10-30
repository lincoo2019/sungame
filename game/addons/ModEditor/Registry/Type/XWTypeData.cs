using Godot;

namespace PVZHE.ModEditor.Registry;

public partial class XWTypeData : RefCounted
{
	public Variant.Type Type { get; set; }

	public string Name { get; set; }

	public Variant DefaultValue { get; set; }

	public string DefaultValueString { get; set; }

	public Color Color { get; set; }

	public PackedScene Editor { get; set; }

	public Texture2D Icon { get; set; }

	public XWTypeData()
	{
	}

	public XWTypeData(Variant.Type type, string name, Variant defaultValue, string defaultValueString, Color color, PackedScene editor, Texture2D icon = null)
	{
		Type = type;
		Name = name;
		DefaultValue = defaultValue;
		DefaultValueString = defaultValueString;
		Color = color;
		Editor = editor;
		Icon = icon;
	}
}
