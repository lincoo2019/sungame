using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseRainModeConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string type = "Default";

	[Export(PropertyHint.None, "")]
	public double aliveTime = 30.0;

	[Export(PropertyHint.None, "")]
	public double interval = 3.0;

	[Export(PropertyHint.None, "")]
	public Array packetList = new Array();

	public void Init(Dictionary data)
	{
		packetList.Clear();
		type = data.GetValueOrDefault("Type", "Default").AsString();
		aliveTime = data.GetValueOrDefault("AliveTime", 15.0).AsDouble();
		interval = data.GetValueOrDefault("Interval", 3.0).AsDouble();
		foreach (Variant item in data.ContainsKey("Packet") ? ((Array)data["Packet"]) : new Array())
		{
			Dictionary data2 = item.AsGodotDictionary();
			TowerDefenseRainModePacketConfig towerDefenseRainModePacketConfig = new TowerDefenseRainModePacketConfig();
			towerDefenseRainModePacketConfig.Init(data2);
			packetList.Add(towerDefenseRainModePacketConfig);
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Type"] = type,
			["AliveTime"] = aliveTime,
			["Interval"] = interval,
			["Packet"] = new Array()
		};
		foreach (Variant packet in packetList)
		{
			TowerDefenseRainModePacketConfig towerDefenseRainModePacketConfig = (TowerDefenseRainModePacketConfig)(GodotObject)packet;
			((Array)dictionary["Packet"]).Add(towerDefenseRainModePacketConfig.Export());
		}
		return dictionary;
	}
}
