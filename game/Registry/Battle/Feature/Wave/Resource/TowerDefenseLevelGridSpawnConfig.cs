using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelGridSpawnConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packet = "";

	[Export(PropertyHint.None, "")]
	public Vector2I gridPos;

	public TowerDefenseCharacterOverride overrideVal;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> spawnEvent = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> dieEvent = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterOverride @override
	{
		get
		{
			return overrideVal;
		}
		set
		{
			overrideVal = value;
		}
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "overrideVal")
		{
			overrideVal = value.As<TowerDefenseCharacterOverride>();
			return true;
		}
		return false;
	}

	public void Init(Dictionary spawnDictionary)
	{
		packet = spawnDictionary.GetValueOrDefault("Packet", "").AsString();
		Array array = (spawnDictionary.ContainsKey("GridPos") ? ((Array)spawnDictionary["GridPos"]) : new Array { 0, 0 });
		gridPos = new Vector2I(array[0].AsInt32(), array[1].AsInt32());
		foreach (Variant item in spawnDictionary.ContainsKey("SpawnEvent") ? ((Array)spawnDictionary["SpawnEvent"]) : new Array())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			string text = dictionary.GetValueOrDefault("EventName", "").AsString();
			if (text != "")
			{
				TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = TowerDefenseCharacterEventMathine.EventGet(text);
				Dictionary valueDictionary = (dictionary.ContainsKey("Value") ? dictionary["Value"].AsGodotDictionary() : new Dictionary());
				towerDefenseCharacterEventBase.Init(valueDictionary);
				spawnEvent.Add(towerDefenseCharacterEventBase);
			}
		}
		foreach (Variant item2 in spawnDictionary.ContainsKey("DieEvent") ? ((Array)spawnDictionary["DieEvent"]) : new Array())
		{
			Dictionary dictionary2 = item2.AsGodotDictionary();
			string text2 = dictionary2.GetValueOrDefault("EventName", "").AsString();
			if (text2 != "")
			{
				TowerDefenseCharacterEventBase towerDefenseCharacterEventBase2 = TowerDefenseCharacterEventMathine.EventGet(text2);
				Dictionary valueDictionary2 = (dictionary2.ContainsKey("Value") ? dictionary2["Value"].AsGodotDictionary() : new Dictionary());
				towerDefenseCharacterEventBase2.Init(valueDictionary2);
				dieEvent.Add(towerDefenseCharacterEventBase2);
			}
		}
		Dictionary dictionary3 = (spawnDictionary.ContainsKey("Override") ? spawnDictionary["Override"].AsGodotDictionary() : new Dictionary());
		if (dictionary3.Count > 0)
		{
			overrideVal = new TowerDefenseCharacterOverride();
			overrideVal.Init(dictionary3);
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Packet"] = packet,
			["GridPos"] = new Array { gridPos.X, gridPos.Y },
			["SpawnEvent"] = new Array(),
			["DieEvent"] = new Array()
		};
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			dictionary["Override"] = overrideVal.Export();
		}
		foreach (TowerDefenseCharacterEventBase item in spawnEvent)
		{
			((Array)dictionary["SpawnEvent"]).Add(item.Export());
		}
		foreach (TowerDefenseCharacterEventBase item2 in dieEvent)
		{
			((Array)dictionary["DieEvent"]).Add(item2.Export());
		}
		return dictionary;
	}
}
