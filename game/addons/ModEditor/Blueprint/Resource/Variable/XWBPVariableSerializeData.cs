using Godot;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPVariableSerializeData : Resource
{
	[Export(PropertyHint.None, "")]
	public int Id { get; set; }

	[Export(PropertyHint.None, "")]
	public string Name { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public int Type { get; set; }

	[Export(PropertyHint.None, "")]
	public StringName ClassName { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Variant DefaultValue { get; set; }

	public void Serialize(XWBPVariableData data)
	{
		Id = data.Id;
		Name = data.Name;
		Type = (int)data.Type;
		ClassName = data.ClassName;
		DefaultValue = data.DefaultValue;
	}

	public XWBPVariableData Deserialize()
	{
		return new XWBPVariableData
		{
			Id = Id,
			Name = Name,
			Type = (Variant.Type)Type,
			ClassName = ClassName,
			DefaultValue = DefaultValue
		};
	}
}
