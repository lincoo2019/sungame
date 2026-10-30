using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPSignalSerializeData : Resource
{
	[Export(PropertyHint.None, "")]
	public int Id { get; set; }

	[Export(PropertyHint.None, "")]
	public string Name { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Array<XWBPNodePortSerializeData> Inputs { get; set; } = new Array<XWBPNodePortSerializeData>();

	public void Serialize(XWBPSignalData data)
	{
		Id = data.Id;
		Name = data.Name;
		Inputs.Clear();
		foreach (XWBPNodePortData input in data.Inputs)
		{
			XWBPNodePortSerializeData xWBPNodePortSerializeData = new XWBPNodePortSerializeData();
			xWBPNodePortSerializeData.Serialize(input);
			Inputs.Add(xWBPNodePortSerializeData);
		}
	}

	public XWBPSignalData Deserialize()
	{
		XWBPSignalData xWBPSignalData = new XWBPSignalData
		{
			Id = Id,
			Name = Name
		};
		foreach (XWBPNodePortSerializeData input in Inputs)
		{
			xWBPSignalData.Inputs.Add(input.Deserialize());
		}
		return xWBPSignalData;
	}
}
