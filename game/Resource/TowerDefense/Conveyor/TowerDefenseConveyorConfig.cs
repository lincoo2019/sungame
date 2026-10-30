using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseConveyorConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string type = "Default";

	[Export(PropertyHint.None, "")]
	public double interval = 3.0;

	[Export(PropertyHint.None, "")]
	public int intervalIncreaseEvery = 1000000;

	[Export(PropertyHint.None, "")]
	public double intervalMagnification = 0.5;

	[Export(PropertyHint.None, "")]
	public int maxPacketCount = 14;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelPacketConfig> packetPrioritySpawnList = new Array<TowerDefenseLevelPacketConfig>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseConveyorPacketConfig> packetList = new Array<TowerDefenseConveyorPacketConfig>();

	[Export(PropertyHint.None, "")]
	public Godot.Collections.Array waveEvent = new Godot.Collections.Array
	{
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array(),
		new Godot.Collections.Array()
	};

	public void Init(Dictionary waveData)
	{
		packetList.Clear();
		packetPrioritySpawnList.Clear();
		type = waveData.GetValueOrDefault("Type", "Default").AsString();
		interval = Math.Max(0.05, waveData.GetValueOrDefault("Interval", 3.0).AsDouble());
		intervalIncreaseEvery = Math.Max(1, waveData.GetValueOrDefault("IntervalIncreaseEvery", 2).AsInt32());
		intervalMagnification = waveData.GetValueOrDefault("IntervalMagnification", 0.5).AsDouble();
		maxPacketCount = Math.Max(1, waveData.GetValueOrDefault("MaxPacketCount", 14).AsInt32());
		foreach (Variant item in waveData.GetValueOrDefault("PacketPrioritySpawnList", new Godot.Collections.Array()).AsGodotArray())
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = new TowerDefenseLevelPacketConfig();
			towerDefenseLevelPacketConfig.Init(item);
			packetPrioritySpawnList.Add(towerDefenseLevelPacketConfig);
		}
		foreach (Variant item2 in waveData.GetValueOrDefault("Packet", new Godot.Collections.Array()).AsGodotArray())
		{
			TowerDefenseConveyorPacketConfig towerDefenseConveyorPacketConfig = new TowerDefenseConveyorPacketConfig();
			towerDefenseConveyorPacketConfig.Init(item2.AsGodotDictionary());
			packetList.Add(towerDefenseConveyorPacketConfig);
		}
		waveEvent.Clear();
		foreach (Variant item3 in waveData.GetValueOrDefault("WaveEvent", new Godot.Collections.Array()).AsGodotArray())
		{
			Array<TowerDefenseConveyorEventBase> array = new Array<TowerDefenseConveyorEventBase>();
			foreach (Variant item4 in item3.AsGodotArray())
			{
				Dictionary dictionary = item4.AsGodotDictionary();
				TowerDefenseConveyorEventBase towerDefenseConveyorEventBase = TowerDefenseConveyorEventEnum.EventGet(dictionary.GetValueOrDefault("EventName", "").AsString());
				if (GodotObject.IsInstanceValid(towerDefenseConveyorEventBase))
				{
					towerDefenseConveyorEventBase.Init(dictionary.GetValueOrDefault("Value", new Dictionary()).AsGodotDictionary());
					array.Add(towerDefenseConveyorEventBase);
				}
			}
			waveEvent.Add(array);
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Type"] = type,
			["Interval"] = interval,
			["IntervalIncreaseEvery"] = intervalIncreaseEvery,
			["IntervalMagnification"] = intervalMagnification,
			["MaxPacketCount"] = maxPacketCount,
			["PacketPrioritySpawnList"] = new Godot.Collections.Array(),
			["Packet"] = new Godot.Collections.Array(),
			["WaveEvent"] = new Godot.Collections.Array()
		};
		foreach (TowerDefenseLevelPacketConfig packetPrioritySpawn in packetPrioritySpawnList)
		{
			((Godot.Collections.Array)dictionary["PacketPrioritySpawnList"]).Add(packetPrioritySpawn.Export());
		}
		foreach (TowerDefenseConveyorPacketConfig packet in packetList)
		{
			((Godot.Collections.Array)dictionary["Packet"]).Add(packet.Export());
		}
		foreach (Variant item in waveEvent)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			foreach (Variant item2 in item.AsGodotArray())
			{
				TowerDefenseConveyorEventBase towerDefenseConveyorEventBase = item2.As<TowerDefenseConveyorEventBase>();
				if (towerDefenseConveyorEventBase != null)
				{
					array.Add(towerDefenseConveyorEventBase.Export());
				}
			}
			((Godot.Collections.Array)dictionary["WaveEvent"]).Add(array);
		}
		return dictionary;
	}
}
