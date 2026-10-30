using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventAddPacket : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	[Export(PropertyHint.None, "")]
	public Array value = new Array();

	public override string _GetName()
	{
		return "LEVLE_EVENT_ADD_PACKET";
	}

	public override void Execute()
	{
		if (!string.IsNullOrWhiteSpace(packetName) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.AddPacket(packetName);
		}
	}

	public override void Init(Dictionary valueDictionary)
	{
		if (valueDictionary == null)
		{
			valueDictionary = new Dictionary();
		}
		packetName = valueDictionary.GetValueOrDefault("PacketName", "").AsString();
		value = valueDictionary.GetValueOrDefault("PacketValue", new Array()).AsGodotArray().Duplicate(deep: true);
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "AddPacket",
			["Value"] = new Dictionary
			{
				["PacketName"] = packetName,
				["PacketValue"] = value?.Duplicate(deep: true) ?? new Array()
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["添加卡片"] = new Dictionary { ["卡片名称"] = new Dictionary
		{
			["Object"] = this,
			["Type"] = "String",
			["Property"] = "packetName",
			["Rest"] = ""
		} };
		return property;
	}
}
