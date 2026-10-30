using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

[GlobalClass]
public partial class XWBPNodeSerializeData : Resource
{
	[Export(PropertyHint.None, "")]
	public int Id { get; set; }

	[Export(PropertyHint.None, "")]
	public bool Lock { get; set; }

	[Export(PropertyHint.None, "")]
	public StringName TypeId { get; set; } = "";

	[Export(PropertyHint.None, "")]
	public Vector2 Position { get; set; } = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public Vector2 Size { get; set; } = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public Array<XWBPNodePortSerializeData> InputPorts { get; set; } = new Array<XWBPNodePortSerializeData>();

	[Export(PropertyHint.None, "")]
	public Array<XWBPNodePortSerializeData> OutputPorts { get; set; } = new Array<XWBPNodePortSerializeData>();

	[Export(PropertyHint.None, "")]
	public Dictionary MetaData { get; set; } = new Dictionary();

	public void Serialize(XWBPNodeData data)
	{
		InputPorts.Clear();
		OutputPorts.Clear();
		if (data == null)
		{
			MetaData = new Dictionary();
			return;
		}
		Id = data.Id;
		Lock = data.Lock;
		TypeId = data.TypeId;
		Position = data.Position;
		Size = data.Size;
		foreach (XWBPNodePortData inputPort in data.InputPorts)
		{
			if (inputPort != null)
			{
				XWBPNodePortSerializeData xWBPNodePortSerializeData = new XWBPNodePortSerializeData();
				xWBPNodePortSerializeData.Serialize(inputPort);
				InputPorts.Add(xWBPNodePortSerializeData);
			}
		}
		foreach (XWBPNodePortData outputPort in data.OutputPorts)
		{
			if (outputPort != null)
			{
				XWBPNodePortSerializeData xWBPNodePortSerializeData2 = new XWBPNodePortSerializeData();
				xWBPNodePortSerializeData2.Serialize(outputPort);
				OutputPorts.Add(xWBPNodePortSerializeData2);
			}
		}
		MetaData = ((data.MetaData != null) ? data.MetaData.Duplicate(deep: true) : new Dictionary());
	}

	public XWBPNodeData Deserialize()
	{
		XWBPNodeData xWBPNodeData = new XWBPNodeData(Id, TypeId)
		{
			Lock = Lock,
			Position = Position,
			Size = Size
		};
		foreach (XWBPNodePortSerializeData inputPort in InputPorts)
		{
			if (inputPort != null)
			{
				XWBPNodePortData xWBPNodePortData = inputPort.Deserialize();
				if (xWBPNodePortData != null)
				{
					xWBPNodeData.InputPorts.Add(xWBPNodePortData);
				}
			}
		}
		foreach (XWBPNodePortSerializeData outputPort in OutputPorts)
		{
			if (outputPort != null)
			{
				XWBPNodePortData xWBPNodePortData2 = outputPort.Deserialize();
				if (xWBPNodePortData2 != null)
				{
					xWBPNodeData.OutputPorts.Add(xWBPNodePortData2);
				}
			}
		}
		xWBPNodeData.MetaData = ((MetaData != null) ? MetaData.Duplicate(deep: true) : new Dictionary());
		xWBPNodeData.RebuildPortMap();
		return xWBPNodeData;
	}
}
