using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class CharacterArmorData : Resource
{
	private static Json _registryJson;

	private Array<ArmorSlotConfig> _armorList = new Array<ArmorSlotConfig>();

	private Action _initAction;

	[Export(PropertyHint.None, "")]
	public Dictionary armorDictionary = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary fliterAllDictionary = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary fliterOpenDictionary = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary fliterCloseDictionary = new Dictionary();

	private static Json REGISTRY_JSON => _registryJson ?? (_registryJson = GD.Load<Json>("res://Registry/Armor/ArmorRegistry.json"));

	[Export(PropertyHint.None, "")]
	public Array<ArmorSlotConfig> armorList
	{
		get
		{
			return _armorList;
		}
		set
		{
			if (_initAction == null)
			{
				_initAction = Init;
			}
			foreach (ArmorSlotConfig armor in _armorList)
			{
				if (armor != null && armor.IsConnected(Resource.SignalName.Changed, Callable.From(_initAction)))
				{
					armor.Changed -= _initAction;
				}
			}
			_armorList = value;
			foreach (ArmorSlotConfig armor2 in _armorList)
			{
				if (armor2 != null && !armor2.IsConnected(Resource.SignalName.Changed, Callable.From(_initAction)))
				{
					armor2.Changed += _initAction;
				}
			}
			Init();
		}
	}

	public static TowerDefenseArmorTypeData _LoadTypeDataFromJSON(string armorName)
	{
		Dictionary dictionary = REGISTRY_JSON.Data.AsGodotDictionary().GetValueOrDefault("Armors", new Dictionary()).AsGodotDictionary();
		if (dictionary.ContainsKey(armorName))
		{
			return GD.Load<TowerDefenseArmorTypeData>(dictionary[armorName].AsString());
		}
		return null;
	}

	public void Init()
	{
		fliterAllDictionary.Clear();
		armorDictionary.Clear();
		fliterOpenDictionary.Clear();
		fliterCloseDictionary.Clear();
		foreach (ArmorSlotConfig armor in armorList)
		{
			if (armor != null)
			{
				TowerDefenseArmorTypeData towerDefenseArmorTypeData = _LoadTypeDataFromJSON(armor.armorName);
				armorDictionary[armor.armorName] = new Dictionary
				{
					["slotConfig"] = armor,
					["typeData"] = towerDefenseArmorTypeData
				};
				Array<string> array = new Array<string>();
				fliterAllDictionary[armor.armorName] = array.Duplicate();
				Array<string> array2 = SplitFliter(armor.destroyFliter);
				if (array2.Count > 0)
				{
					((Godot.Collections.Array)fliterAllDictionary[armor.armorName]).AddRange(array2);
				}
				fliterOpenDictionary[armor.armorName] = array.Duplicate();
				Array<string> array3 = SplitFliter(armor.openFliter);
				if (array3.Count > 0)
				{
					((Godot.Collections.Array)fliterOpenDictionary[armor.armorName]).AddRange(array3);
				}
				fliterCloseDictionary[armor.armorName] = array.Duplicate();
				Array<string> array4 = SplitFliter(armor.closeFliter);
				if (array4.Count > 0)
				{
					((Godot.Collections.Array)fliterCloseDictionary[armor.armorName]).AddRange(array4);
				}
			}
		}
	}

	private Array<string> SplitFliter(string fliter)
	{
		Array<string> array = new Array<string>();
		if (string.IsNullOrEmpty(fliter))
		{
			return array;
		}
		string[] array2 = fliter.Split('&', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array2)
		{
			array.Add(text.Trim());
		}
		return array;
	}

	public void ClearArmorFlitersAll(AdobeAnimateSprite sprite)
	{
		foreach (Variant key in fliterAllDictionary.Keys)
		{
			string armorName = (string)key;
			ClearArmorFliters(sprite, armorName);
		}
	}

	public void ClearArmorFliters(AdobeAnimateSprite sprite, string armorName)
	{
		sprite.SetFliters((Godot.Collections.Array)fliterAllDictionary[armorName], open: false);
		sprite.SetFliters((Godot.Collections.Array)fliterCloseDictionary[armorName], open: true);
		sprite.SetFliters((Godot.Collections.Array)fliterOpenDictionary[armorName], open: false);
	}

	public void OpenArmorFlitersAll(AdobeAnimateSprite sprite)
	{
		foreach (Variant key in fliterAllDictionary.Keys)
		{
			string armorName = (string)key;
			OpenArmorFliters(sprite, armorName);
		}
	}

	public void OpenArmorFliters(AdobeAnimateSprite sprite, string armorName)
	{
		sprite.SetFliters((Godot.Collections.Array)fliterAllDictionary[armorName], open: true);
		sprite.SetFliters((Godot.Collections.Array)fliterCloseDictionary[armorName], open: false);
		sprite.SetFliters((Godot.Collections.Array)fliterOpenDictionary[armorName], open: true);
	}

	public void SetArmorReplace(AdobeAnimateSprite sprite, string armorName, int stage)
	{
		ArmorSlotConfig armorSlotConfig = (ArmorSlotConfig)(GodotObject)((Dictionary)armorDictionary[armorName])["slotConfig"];
		if (armorSlotConfig.replaceMethod != "Media" || armorSlotConfig.replaceMediaName == null || string.IsNullOrEmpty(armorSlotConfig.replaceMediaName.ToString()))
		{
			return;
		}
		TowerDefenseArmorTypeData towerDefenseArmorTypeData = (TowerDefenseArmorTypeData)(GodotObject)((Dictionary)armorDictionary[armorName])["typeData"];
		if (towerDefenseArmorTypeData != null)
		{
			Array<string> stageAnimeTexturePaths = towerDefenseArmorTypeData.stageAnimeTexturePaths;
			if (stageAnimeTexturePaths.Count > 0)
			{
				int index = Mathf.Clamp(stage, 0, stageAnimeTexturePaths.Count - 1);
				sprite.SetAtlasReplace(armorSlotConfig.replaceMediaName, stageAnimeTexturePaths[index]);
			}
		}
	}

	public static AdobeAnimatePart CreateArmorPartNode(AdobeAnimateSprite sprite, ArmorSlotConfig slotConfig, TowerDefenseArmorTypeData typeData, int stage = 0)
	{
		if (!GodotObject.IsInstanceValid(sprite) || slotConfig == null || typeData == null || typeData.stageAnimeTexturePaths == null || typeData.stageAnimeTexturePaths.Count == 0)
		{
			return null;
		}
		AdobeAnimateSlot node = sprite.GetNode<AdobeAnimateSlot>(slotConfig.slotPath);
		AdobeAnimatePart adobeAnimatePart = AdobeAnimatePart.CreateAtlasTexturePart(typeData.stageAnimeTexturePaths[Mathf.Clamp(stage, 0, typeData.stageAnimeTexturePaths.Count - 1)]);
		if (!GodotObject.IsInstanceValid(adobeAnimatePart))
		{
			return null;
		}
		adobeAnimatePart.Position = slotConfig.offset;
		adobeAnimatePart.Rotation = (float)slotConfig.rotation;
		adobeAnimatePart.Scale = slotConfig.scale;
		adobeAnimatePart.Modulate = new Color(1f, 1f, 1f, slotConfig.alphaMultiplier);
		node.AddChild(adobeAnimatePart, forceReadableName: false, Node.InternalMode.Disabled);
		return adobeAnimatePart;
	}

	public ArmorSlotConfig GetSlotConfig(string armorName)
	{
		if (armorDictionary.ContainsKey(armorName))
		{
			return (ArmorSlotConfig)(GodotObject)((Dictionary)armorDictionary[armorName])["slotConfig"];
		}
		return null;
	}

	public ArmorSlotConfig GetOrCreateSlotConfig(string armorName)
	{
		if (armorDictionary.ContainsKey(armorName))
		{
			return (ArmorSlotConfig)(GodotObject)((Dictionary)armorDictionary[armorName])["slotConfig"];
		}
		TowerDefenseArmorTypeData towerDefenseArmorTypeData = _LoadTypeDataFromJSON(armorName);
		if (towerDefenseArmorTypeData == null)
		{
			return null;
		}
		ArmorSlotConfig armorSlotConfig = new ArmorSlotConfig();
		armorSlotConfig.armorName = armorName;
		armorSlotConfig.replaceMethod = "Sprite";
		armorSlotConfig.slotPath = new NodePath("HeadSlot");
		armorSlotConfig.offset = Vector2.Zero;
		armorSlotConfig.rotation = 0.0;
		armorSlotConfig.scale = Vector2.One;
		armorSlotConfig.damagePoint = -1.0;
		armorSlotConfig.openFliter = "";
		armorSlotConfig.closeFliter = "";
		armorSlotConfig.destroyFliter = "";
		armorDictionary[armorName] = new Dictionary
		{
			["slotConfig"] = armorSlotConfig,
			["typeData"] = towerDefenseArmorTypeData
		};
		Array<string> array = new Array<string>();
		fliterAllDictionary[armorName] = array.Duplicate();
		Array<string> array2 = SplitFliter(armorSlotConfig.destroyFliter);
		if (array2.Count > 0)
		{
			((Godot.Collections.Array)fliterAllDictionary[armorName]).AddRange(array2);
		}
		fliterOpenDictionary[armorName] = array.Duplicate();
		Array<string> array3 = SplitFliter(armorSlotConfig.openFliter);
		if (array3.Count > 0)
		{
			((Godot.Collections.Array)fliterOpenDictionary[armorName]).AddRange(array3);
		}
		fliterCloseDictionary[armorName] = array.Duplicate();
		Array<string> array4 = SplitFliter(armorSlotConfig.closeFliter);
		if (array4.Count > 0)
		{
			((Godot.Collections.Array)fliterCloseDictionary[armorName]).AddRange(array4);
		}
		return armorSlotConfig;
	}

	public TowerDefenseArmorTypeData GetTypeData(string armorName)
	{
		if (armorDictionary.ContainsKey(armorName))
		{
			return (TowerDefenseArmorTypeData)(GodotObject)((Dictionary)armorDictionary[armorName])["typeData"];
		}
		return null;
	}
}
