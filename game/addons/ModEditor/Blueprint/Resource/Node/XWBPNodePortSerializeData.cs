using Godot;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPNodePortSerializeData : Resource
{
	[Export(PropertyHint.None, "")]
	public string Name { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public int Direction { get; set; }

	[Export(PropertyHint.None, "")]
	public int PortType { get; set; }

	[Export(PropertyHint.None, "")]
	public StringName ClassName { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Variant DefaultValue { get; set; }

	[Export(PropertyHint.None, "")]
	public Variant Value { get; set; }

	public void Serialize(XWBPNodePortData data)
	{
		if (data != null)
		{
			Name = data.Name;
			Direction = (int)data.PortDirection;
			PortType = (int)data.PortTypeValue;
			ClassName = data.ClassName;
			DefaultValue = data.DefaultValue;
			Value = data.Value;
		}
	}

	public XWBPNodePortData Deserialize()
	{
		XWBPNodePortData.Direction direction = ((Direction == 1) ? XWBPNodePortData.Direction.Output : XWBPNodePortData.Direction.Input);
		return new XWBPNodePortData(Name ?? "", direction, (XWBPNodePortData.PortType)PortType, ClassName, DefaultValue)
		{
			Value = Value
		};
	}
}
