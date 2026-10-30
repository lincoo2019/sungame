using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefensePacketOverride : Resource
{
	[ExportCategory("Total")]
	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.PACKET_TYPE type = TowerDefenseEnum.PACKET_TYPE.NOONE;

	[ExportCategory("Total")]
	[Export(PropertyHint.None, "")]
	public int costRise = -1;

	[Export(PropertyHint.None, "")]
	public int cost = -1;

	[Export(PropertyHint.None, "")]
	public double costMultiple = -1.0;

	[Export(PropertyHint.None, "")]
	public double packetCooldown = -1.0;

	[Export(PropertyHint.None, "")]
	public double startingCooldown = -1.0;

	[ExportCategory("Zombie")]
	[Export(PropertyHint.None, "")]
	public int weight = -1;

	[Export(PropertyHint.None, "")]
	public int wavePointCost = -1;

	[ExportCategory("Action Behavior")]
	[Export(PropertyHint.None, "")]
	public Array<CardActionBehaviorDefinition> pressedActions = new Array<CardActionBehaviorDefinition>();

	[Export(PropertyHint.None, "")]
	public Array<CardActionBehaviorDefinition> useSucceededActions = new Array<CardActionBehaviorDefinition>();

	[ExportCategory("Character")]
	[Export(PropertyHint.None, "")]
	public Array<string> plantCover = new Array<string>();

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterOverride characterOverride = new TowerDefenseCharacterOverride();

	[Export(PropertyHint.None, "")]
	public bool islimitGridNum = true;

	[Export(PropertyHint.None, "")]
	public bool coverCanDirectPlant;

	[Export(PropertyHint.None, "")]
	public bool hypnoses;

	public void Init(Dictionary data)
	{
		if (Enum.TryParse<TowerDefenseEnum.PACKET_TYPE>(data.GetValueOrDefault("Type", "NOONE").AsString().ToUpper(), out var result))
		{
			type = result;
		}
		costRise = data.GetValueOrDefault("CostRise", -1).AsInt32();
		cost = data.GetValueOrDefault("Cost", data.GetValueOrDefault("cost", -1)).AsInt32();
		costMultiple = data.GetValueOrDefault("CostMultiple", -1.0).AsDouble();
		packetCooldown = data.GetValueOrDefault("PacketCooldown", -1.0).AsDouble();
		startingCooldown = data.GetValueOrDefault("StartingCooldown", -1.0).AsDouble();
		weight = data.GetValueOrDefault("Weight", -1).AsInt32();
		wavePointCost = data.GetValueOrDefault("WavePointCost", -1).AsInt32();
		plantCover = new Array<string>();
		foreach (Variant item in data.GetValueOrDefault("PlantCover", new Godot.Collections.Array()).AsGodotArray())
		{
			plantCover.Add(item.AsString());
		}
		pressedActions = ImportActions(data, "PressedActions", "EventPress", CardActionBehaviorTrigger.Pressed);
		useSucceededActions = ImportActions(data, "UseSucceededActions", "EventPlant", CardActionBehaviorTrigger.UseSucceeded);
		characterOverride = new TowerDefenseCharacterOverride();
		characterOverride.Init(data.GetValueOrDefault("CharacterOverride", new Dictionary()).AsGodotDictionary());
		islimitGridNum = data.GetValueOrDefault("IslimitGridNum", true).AsBool();
		coverCanDirectPlant = data.GetValueOrDefault("CoverCanDirectPlant", false).AsBool();
		hypnoses = data.GetValueOrDefault("Hypnoses", false).AsBool();
	}

	public Dictionary Export()
	{
		if (plantCover == null)
		{
			plantCover = new Array<string>();
		}
		if (pressedActions == null)
		{
			pressedActions = new Array<CardActionBehaviorDefinition>();
		}
		if (useSucceededActions == null)
		{
			useSucceededActions = new Array<CardActionBehaviorDefinition>();
		}
		Dictionary dictionary = (GodotObject.IsInstanceValid(characterOverride) ? characterOverride.Export() : new Dictionary());
		Dictionary dictionary2 = new Dictionary
		{
			["Type"] = Enum.GetName(typeof(TowerDefenseEnum.PACKET_TYPE), type),
			["CostRise"] = costRise,
			["Cost"] = cost,
			["CostMultiple"] = costMultiple,
			["PacketCooldown"] = packetCooldown,
			["StartingCooldown"] = startingCooldown,
			["Weight"] = weight,
			["WavePointCost"] = wavePointCost,
			["PressedActions"] = new Godot.Collections.Array(),
			["UseSucceededActions"] = new Godot.Collections.Array(),
			["PlantCover"] = plantCover,
			["CharacterOverride"] = dictionary,
			["IslimitGridNum"] = islimitGridNum,
			["CoverCanDirectPlant"] = coverCanDirectPlant,
			["Hypnoses"] = hypnoses
		};
		foreach (CardActionBehaviorDefinition pressedAction in pressedActions)
		{
			if (GodotObject.IsInstanceValid(pressedAction))
			{
				((Godot.Collections.Array)dictionary2["PressedActions"]).Add(pressedAction.ExportConfiguration());
			}
		}
		foreach (CardActionBehaviorDefinition useSucceededAction in useSucceededActions)
		{
			if (GodotObject.IsInstanceValid(useSucceededAction))
			{
				((Godot.Collections.Array)dictionary2["UseSucceededActions"]).Add(useSucceededAction.ExportConfiguration());
			}
		}
		return dictionary2;
	}

	public override bool _Set(StringName property, Variant value)
	{
		string text = property.ToString();
		if (!(text == "eventPress"))
		{
			if (text == "eventPlant")
			{
				useSucceededActions = CardActionBehaviorDefinition.ReadLegacyArray(value, CardActionBehaviorTrigger.UseSucceeded);
				return true;
			}
			return false;
		}
		pressedActions = CardActionBehaviorDefinition.ReadLegacyArray(value, CardActionBehaviorTrigger.Pressed);
		return true;
	}

	private static Array<CardActionBehaviorDefinition> ImportActions(Dictionary data, string currentKey, string legacyKey, CardActionBehaviorTrigger trigger)
	{
		Array<CardActionBehaviorDefinition> array = new Array<CardActionBehaviorDefinition>();
		foreach (Variant item in data.GetValueOrDefault(currentKey, data.GetValueOrDefault(legacyKey, new Godot.Collections.Array())).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			CardActionBehaviorDefinition cardActionBehaviorDefinition = CardActionBehaviorFactory.Create(dictionary.GetValueOrDefault("ActionId", dictionary.GetValueOrDefault("EventName", "")).AsString());
			if (GodotObject.IsInstanceValid(cardActionBehaviorDefinition))
			{
				Dictionary data2 = dictionary.GetValueOrDefault("Configuration", dictionary.GetValueOrDefault("Value", new Dictionary())).AsGodotDictionary();
				cardActionBehaviorDefinition.ImportConfiguration(data2);
				cardActionBehaviorDefinition.triggerFlags = (int)trigger;
				array.Add(cardActionBehaviorDefinition);
			}
		}
		return array;
	}
}
