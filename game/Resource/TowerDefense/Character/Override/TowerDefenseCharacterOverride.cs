using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterOverride : Resource
{
	[Export(PropertyHint.None, "")]
	public bool invisible;

	[Export(PropertyHint.None, "")]
	public double scale = -1.0;

	[Export(PropertyHint.None, "")]
	public double hitpointScale = -1.0;

	[Export(PropertyHint.None, "")]
	public Vector2 walkSpeedScale = new Vector2(-1f, -1f);

	[Export(PropertyHint.None, "")]
	public Vector2 animeSpeedScale = new Vector2(-1f, -1f);

	[Export(PropertyHint.None, "")]
	public Array armor = new Array();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterPropertyChangeConfig> propertyChange = new Array<TowerDefenseCharacterPropertyChangeConfig>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> spawnEvent = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> dieEvent = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public bool canMowerMove;

	[Export(PropertyHint.None, "")]
	public bool hypnoses;

	[Export(PropertyHint.None, "")]
	public bool spawnFromLeft;

	private bool _hasCanMowerMoveOverride;

	public void Init(Dictionary data)
	{
		propertyChange.Clear();
		spawnEvent.Clear();
		dieEvent.Clear();
		invisible = data.GetValueOrDefault("Invisible", false).AsBool();
		scale = data.GetValueOrDefault("Scale", -1).AsDouble();
		hitpointScale = data.GetValueOrDefault("HitpointScale", -1).AsDouble();
		Variant valueOrDefault = data.GetValueOrDefault("WalkSpeedScale", new Array { -1, -1 });
		if (valueOrDefault.VariantType == Variant.Type.Array)
		{
			Array array = valueOrDefault.AsGodotArray();
			if (array.Count == 2)
			{
				walkSpeedScale = new Vector2((float)array[0].AsDouble(), (float)array[1].AsDouble());
			}
			else if (array.Count == 1)
			{
				walkSpeedScale = new Vector2((float)array[0].AsDouble(), (float)array[0].AsDouble());
			}
		}
		if (valueOrDefault.VariantType == Variant.Type.Float)
		{
			walkSpeedScale = Vector2.One * (float)valueOrDefault.AsDouble();
		}
		Variant valueOrDefault2 = data.GetValueOrDefault("AnimeSpeedScale", new Array { -1, -1 });
		if (valueOrDefault2.VariantType == Variant.Type.Array)
		{
			Array array2 = valueOrDefault2.AsGodotArray();
			if (array2.Count == 2)
			{
				animeSpeedScale = new Vector2((float)array2[0].AsDouble(), (float)array2[1].AsDouble());
			}
			else if (array2.Count == 1)
			{
				animeSpeedScale = new Vector2((float)array2[0].AsDouble(), (float)array2[0].AsDouble());
			}
		}
		if (valueOrDefault2.VariantType == Variant.Type.Float)
		{
			animeSpeedScale = Vector2.One * (float)valueOrDefault2.AsDouble();
		}
		armor = data.GetValueOrDefault("Armor", new Array()).AsGodotArray();
		Array array3 = data.GetValueOrDefault("PropertyChange", new Array()).AsGodotArray();
		if (array3 != null && array3.Count != 0)
		{
			foreach (Variant item in array3)
			{
				Dictionary data2 = item.AsGodotDictionary();
				TowerDefenseCharacterPropertyChangeConfig towerDefenseCharacterPropertyChangeConfig = new TowerDefenseCharacterPropertyChangeConfig();
				towerDefenseCharacterPropertyChangeConfig.Init(data2);
				propertyChange.Add(towerDefenseCharacterPropertyChangeConfig);
			}
		}
		Array array4 = data.GetValueOrDefault("SpawnEvent", new Array()).AsGodotArray();
		if (array4 != null && array4.Count != 0)
		{
			foreach (Variant item2 in array4)
			{
				Dictionary dictionary = item2.AsGodotDictionary();
				string text = dictionary.GetValueOrDefault("EventName", "").AsString();
				if (!string.IsNullOrEmpty(text))
				{
					TowerDefenseCharacterEventBase towerDefenseCharacterEventBase = TowerDefenseCharacterEventMathine.EventGet(text);
					Dictionary valueDictionary = dictionary.GetValueOrDefault("Value", new Dictionary()).AsGodotDictionary();
					towerDefenseCharacterEventBase.Init(valueDictionary);
					spawnEvent.Add(towerDefenseCharacterEventBase);
				}
			}
		}
		Array array5 = data.GetValueOrDefault("DieEvent", new Array()).AsGodotArray();
		if (array5 != null && array5.Count != 0)
		{
			foreach (Variant item3 in array5)
			{
				Dictionary dictionary2 = item3.AsGodotDictionary();
				string text2 = dictionary2.GetValueOrDefault("EventName", "").AsString();
				if (!string.IsNullOrEmpty(text2))
				{
					TowerDefenseCharacterEventBase towerDefenseCharacterEventBase2 = TowerDefenseCharacterEventMathine.EventGet(text2);
					Dictionary valueDictionary2 = dictionary2.GetValueOrDefault("Value", new Dictionary()).AsGodotDictionary();
					towerDefenseCharacterEventBase2.Init(valueDictionary2);
					dieEvent.Add(towerDefenseCharacterEventBase2);
				}
			}
		}
		_hasCanMowerMoveOverride = data.ContainsKey("CanMowerMove");
		canMowerMove = _hasCanMowerMoveOverride && data["CanMowerMove"].AsBool();
		hypnoses = data.GetValueOrDefault("Hypnoses", false).AsBool();
		spawnFromLeft = data.GetValueOrDefault("SpawnFromLeft", false).AsBool();
	}

	public Dictionary Export()
	{
		if (armor == null)
		{
			armor = new Array();
		}
		if (propertyChange == null)
		{
			propertyChange = new Array<TowerDefenseCharacterPropertyChangeConfig>();
		}
		if (spawnEvent == null)
		{
			spawnEvent = new Array<TowerDefenseCharacterEventBase>();
		}
		if (dieEvent == null)
		{
			dieEvent = new Array<TowerDefenseCharacterEventBase>();
		}
		Dictionary dictionary = new Dictionary
		{
			["Invisible"] = invisible,
			["Scale"] = scale,
			["HitpointScale"] = hitpointScale,
			["WalkSpeedScale"] = new Array { walkSpeedScale.X, walkSpeedScale.Y },
			["AnimeSpeedScale"] = new Array { animeSpeedScale.X, animeSpeedScale.Y },
			["Armor"] = armor,
			["PropertyChange"] = new Array(),
			["SpawnEvent"] = new Array(),
			["DieEvent"] = new Array()
		};
		if (_hasCanMowerMoveOverride || canMowerMove)
		{
			dictionary["CanMowerMove"] = canMowerMove;
		}
		if (hypnoses)
		{
			dictionary["Hypnoses"] = true;
		}
		if (spawnFromLeft)
		{
			dictionary["SpawnFromLeft"] = true;
		}
		foreach (TowerDefenseCharacterPropertyChangeConfig item in propertyChange)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				((Array)dictionary["PropertyChange"]).Add(item.Export());
			}
		}
		foreach (TowerDefenseCharacterEventBase item2 in spawnEvent)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				((Array)dictionary["SpawnEvent"]).Add(item2.Export());
			}
		}
		foreach (TowerDefenseCharacterEventBase item3 in dieEvent)
		{
			if (GodotObject.IsInstanceValid(item3))
			{
				((Array)dictionary["DieEvent"]).Add(item3.Export());
			}
		}
		return dictionary;
	}

	public void ExecuteCharacter(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		if (!character.IsNodeReady())
		{
			ExecuteCharacterWhenReady(character);
			return;
		}
		if (!character.invisible)
		{
			character.invisible = invisible;
		}
		if (scale != -1.0)
		{
			character.transformPoint.Scale = (float)scale * Vector2.One;
		}
		if (hitpointScale != -1.0)
		{
			character.instance.hitpointScale = hitpointScale;
		}
		if (character is TowerDefenseZombie towerDefenseZombie && walkSpeedScale != new Vector2(-1f, -1f))
		{
			towerDefenseZombie.walkSpeedScale *= (float)GD.RandRange(walkSpeedScale.X, walkSpeedScale.Y);
		}
		if (hypnoses)
		{
			character.Hypnoses();
		}
		if (spawnFromLeft && character is TowerDefenseZombie && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
			logicalGlobalPosition.X = (float)TowerDefenseManager.Instance.GetMapGroundLeft() - 40f;
			character.SetLogicalGlobalPosition(logicalGlobalPosition);
		}
		if (animeSpeedScale != new Vector2(-1f, -1f))
		{
			character.timeScale *= (float)GD.RandRange(animeSpeedScale.X, animeSpeedScale.Y);
		}
		if (armor != null && armor.Count != 0)
		{
			foreach (Variant item in armor)
			{
				character.instance.ArmorAdd(item.AsString());
			}
		}
		if (propertyChange != null && propertyChange.Count != 0)
		{
			foreach (TowerDefenseCharacterPropertyChangeConfig item2 in propertyChange)
			{
				item2.Execute(character);
			}
		}
		character.dieEvent.AddRange(dieEvent);
		foreach (TowerDefenseCharacterEventBase item3 in spawnEvent)
		{
			item3.Execute(character.GetLogicalGlobalPosition(), character);
		}
		if (_hasCanMowerMoveOverride || canMowerMove)
		{
			character.canMowerMove = canMowerMove;
		}
	}

	private async void ExecuteCharacterWhenReady(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			await ToSignal(character, Node.SignalName.Ready);
			if (GodotObject.IsInstanceValid(character))
			{
				ExecuteCharacter(character);
			}
		}
	}
}
