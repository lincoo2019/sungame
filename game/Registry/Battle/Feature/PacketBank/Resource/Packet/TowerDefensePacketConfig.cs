using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[Tool]
[GlobalClass]
public partial class TowerDefensePacketConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string saveKey = "";

	[Export(PropertyHint.None, "")]
	public Array<UnlockConditionBaseConfig> unlockCheckList = new Array<UnlockConditionBaseConfig>();

	[Export(PropertyHint.None, "")]
	public string name;

	[Export(PropertyHint.None, "")]
	public string describe;

	[Export(PropertyHint.None, "")]
	public string plantfood;

	[Export(PropertyHint.None, "")]
	public string handbookDescribe;

	[Export(PropertyHint.None, "")]
	public string handbookStory;

	[Export(PropertyHint.None, "")]
	public bool packetFlip;

	[Export(PropertyHint.None, "")]
	public string packetAnimeClip = "Idle";

	[Export(PropertyHint.None, "")]
	public Vector2 handbookPacketAnimeOffset = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public Vector2 packetAnimeOffset = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public Vector2 packetAnimeScale = Vector2.Zero;

	private TowerDefenseCharacterConfig _characterConfig;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.PACKET_TYPE type;

	[Export(PropertyHint.None, "")]
	public bool canChangeCost = true;

	[ExportCategory("Spawn")]
	[Export(PropertyHint.Enum, "Noone,Rise,FlyDown")]
	public string spawnMethod = "Rise";

	[ExportCategory("Action Behavior")]
	[Export(PropertyHint.None, "")]
	public Array<CardActionBehaviorDefinition> pressedActions = new Array<CardActionBehaviorDefinition>();

	[Export(PropertyHint.None, "")]
	public Array<CardActionBehaviorDefinition> useSucceededActions = new Array<CardActionBehaviorDefinition>();

	[ExportCategory("Behavior")]
	[Export(PropertyHint.None, "")]
	public Array<StringName> behaviorIds = new Array<StringName>();

	[Export(PropertyHint.None, "")]
	public Array<CardBehaviorDefinition> behaviors = new Array<CardBehaviorDefinition>();

	[ExportCategory("Override")]
	public TowerDefensePacketOverride _override;

	[Export(PropertyHint.None, "")]
	public bool overrideHypnoses;

	[Export(PropertyHint.None, "")]
	public int overrideCostRise = -1;

	[Export(PropertyHint.None, "")]
	public int overrideCost = -1;

	[Export(PropertyHint.None, "")]
	public double overridePacketCooldown = -1.0;

	[Export(PropertyHint.None, "")]
	public double overrideStartingCooldown = -1.0;

	[Export(PropertyHint.None, "")]
	public int overrideWeight = -1;

	[Export(PropertyHint.None, "")]
	public int overrideWavePointCost = -1;

	[ExportCategory("Other")]
	[Export(PropertyHint.None, "")]
	public Array<string> initArmor = new Array<string>();

	[Export(PropertyHint.None, "")]
	public bool plantUseCell = true;

	[Export(PropertyHint.None, "")]
	public bool izmPlantAllCell;

	[Export(PropertyHint.None, "")]
	public bool izmPlantLeft;

	[Export(PropertyHint.None, "")]
	public bool disableWhenSunNegative;

	[Export(PropertyHint.None, "")]
	public int expireSun;

	[ExportCategory("ZombiePlace")]
	[Export(PropertyHint.None, "")]
	public bool canPlaceOnZombie;

	public List<TowerDefensePacketChangeCost> changeCostList = new List<TowerDefensePacketChangeCost>();

	public Dictionary coldDownDecreaseDictionary = new Dictionary();

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterConfig characterConfig
	{
		get
		{
			return _characterConfig;
		}
		set
		{
			_characterConfig = value;
			NotifyPropertyListChanged();
		}
	}

	[Export(PropertyHint.None, "")]
	public TowerDefensePacketOverride @override
	{
		get
		{
			return _override;
		}
		set
		{
			_override = value;
		}
	}

	internal static void SetPreparedCharacterGlobalPosition(TowerDefenseCharacter character, Vector2 globalPosition)
	{
		ulong num = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (num == 18446744073709551615uL)
		{
			num = Engine.GetPhysicsFrames();
		}
		character.SetGlobalPositionForPhysicsFrame(globalPosition, num);
	}

	public TowerDefensePacketConfig CreateSpawnRuntimeCopy()
	{
		TowerDefensePacketConfig towerDefensePacketConfig = Duplicate() as TowerDefensePacketConfig;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			return null;
		}
		towerDefensePacketConfig._override = (GodotObject.IsInstanceValid(_override) ? (_override.Duplicate(deep: true) as TowerDefensePacketOverride) : null);
		towerDefensePacketConfig.changeCostList = new List<TowerDefensePacketChangeCost>();
		towerDefensePacketConfig.coldDownDecreaseDictionary = new Dictionary();
		return towerDefensePacketConfig;
	}

	public TowerDefensePacketConfig CreateRuntimeStateCopy()
	{
		TowerDefensePacketConfig towerDefensePacketConfig = Duplicate() as TowerDefensePacketConfig;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			return null;
		}
		towerDefensePacketConfig._override = (GodotObject.IsInstanceValid(_override) ? (_override.Duplicate() as TowerDefensePacketOverride) : null);
		towerDefensePacketConfig.changeCostList = new List<TowerDefensePacketChangeCost>();
		if (changeCostList != null)
		{
			for (int i = 0; i < changeCostList.Count; i++)
			{
				TowerDefensePacketChangeCost towerDefensePacketChangeCost = changeCostList[i];
				if (GodotObject.IsInstanceValid(towerDefensePacketChangeCost))
				{
					if (towerDefensePacketChangeCost.GetType() == typeof(TowerDefensePacketChangeCost))
					{
						towerDefensePacketConfig.changeCostList.Add(TowerDefensePacketChangeCost.ImportSave(towerDefensePacketChangeCost.ExportSave()));
					}
					else
					{
						towerDefensePacketConfig.changeCostList.Add(towerDefensePacketChangeCost);
					}
				}
			}
		}
		towerDefensePacketConfig.coldDownDecreaseDictionary = new Dictionary();
		return towerDefensePacketConfig;
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		Array<Dictionary> array = new Array<Dictionary>();
		if (GodotObject.IsInstanceValid(characterConfig))
		{
			if (GodotObject.IsInstanceValid(characterConfig.armorData))
			{
				string value = string.Join(",", characterConfig.armorData.armorDictionary.Keys);
				array.Add(new Dictionary
				{
					["name"] = "Armor",
					["type"] = 28,
					["hint"] = 2,
					["hint_string"] = $"{4}/{2}:{value}",
					["usage"] = num
				});
			}
			array.Add(new Dictionary
			{
				["name"] = "cell/plantUseCell",
				["type"] = 1,
				["usage"] = num
			});
			if (characterConfig is TowerDefenseZombieConfig)
			{
				array.Add(new Dictionary
				{
					["name"] = "Override/overrideWeight",
					["type"] = 2,
					["usage"] = num
				});
				array.Add(new Dictionary
				{
					["name"] = "Override/overrideWavePointCost",
					["type"] = 2,
					["usage"] = num
				});
			}
		}
		return array;
	}

	public override bool _Set(StringName property, Variant value)
	{
		switch (property.ToString())
		{
		case "eventPress":
			pressedActions = CardActionBehaviorDefinition.ReadLegacyArray(value, CardActionBehaviorTrigger.Pressed);
			return true;
		case "eventPlant":
			useSucceededActions = CardActionBehaviorDefinition.ReadLegacyArray(value, CardActionBehaviorTrigger.UseSucceeded);
			return true;
		case "_override":
			_override = value.As<TowerDefensePacketOverride>();
			return true;
		case "Armor":
			initArmor.Clear();
			foreach (Variant item in value.AsGodotArray())
			{
				initArmor.Add(item.AsString());
			}
			return true;
		case "Override/overrideWeight":
			overrideWeight = value.AsInt32();
			return true;
		case "Override/overrideWavePointCost":
			overrideWavePointCost = value.AsInt32();
			return true;
		case "cell/plantUseCell":
			plantUseCell = value.AsBool();
			return true;
		default:
			return false;
		}
	}

	public override Variant _Get(StringName property)
	{
		return property.ToString() switch
		{
			"Armor" => initArmor, 
			"Override/overrideWeight" => overrideWeight, 
			"Override/overrideWavePointCost" => overrideWavePointCost, 
			"cell/plantUseCell" => plantUseCell, 
			_ => default(Variant), 
		};
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		switch (property.ToString())
		{
		case "Armor":
		case "Override/overrideWeight":
		case "Override/overrideWavePointCost":
		case "cell/plantUseCell":
			return true;
		default:
			return false;
		}
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		return property.ToString() switch
		{
			"Armor" => new Array<string>(), 
			"Override/overrideWeight" => -1, 
			"Override/overrideWavePointCost" => -1, 
			"cell/plantUseCell" => true, 
			_ => default(Variant), 
		};
	}

	public TowerDefenseEnum.PACKET_TYPE _GetType()
	{
		if (GodotObject.IsInstanceValid(_override) && _override.type != TowerDefenseEnum.PACKET_TYPE.NOONE)
		{
			return _override.type;
		}
		return type;
	}

	public int GetCostRise()
	{
		if (GodotObject.IsInstanceValid(_override) && _override.costRise != -1)
		{
			return _override.costRise;
		}
		if (overrideCostRise != -1)
		{
			return overrideCostRise;
		}
		if (!GodotObject.IsInstanceValid(characterConfig))
		{
			return -1;
		}
		return characterConfig.costRise;
	}

	public double GetCostMultiple()
	{
		if (GodotObject.IsInstanceValid(_override) && _override.costMultiple != -1.0)
		{
			return _override.costMultiple;
		}
		if (!GodotObject.IsInstanceValid(characterConfig))
		{
			return -1.0;
		}
		return characterConfig.costMultiple;
	}

	public int GetCostBeforeModifiers()
	{
		if (!GodotObject.IsInstanceValid(characterConfig))
		{
			return 0;
		}
		int result = characterConfig.cost;
		if (overrideCost != -1)
		{
			result = overrideCost;
		}
		if (GodotObject.IsInstanceValid(_override) && _override.cost != -1)
		{
			result = _override.cost;
		}
		if (characterConfig.costNight != -1 && TowerDefenseManager.GetMapIsNight())
		{
			result = characterConfig.costNight;
		}
		return result;
	}

	public int GetCost(bool skipGlobalChangeCost = false)
	{
		int costBeforeModifiers = GetCostBeforeModifiers();
		return CardBehaviorDispatcher.ApplyCost(this, costBeforeModifiers, skipGlobalChangeCost);
	}

	public double GetPacketCooldown()
	{
		double num = 1.0;
		if (TowerDefenseManager.IsUnlimitedFire())
		{
			num *= 2.0 / 3.0;
		}
		Variant[] array = coldDownDecreaseDictionary.Keys.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			string text = (string)array[i];
			Array array2 = (Array)((Dictionary)coldDownDecreaseDictionary[text])["ControlCharacterList"];
			for (int j = 0; j < array2.Count; j++)
			{
				for (int k = 0; k < array2.Count; k++)
				{
					if (!GodotObject.IsInstanceValid((GodotObject)array2[k]))
					{
						array2.RemoveAt(k);
						break;
					}
				}
			}
			if (array2.Count <= 0)
			{
				coldDownDecreaseDictionary.Remove(text);
			}
		}
		double num2 = 0.0;
		foreach (Variant key in coldDownDecreaseDictionary.Keys)
		{
			string text2 = (string)key;
			Dictionary dictionary = (Dictionary)coldDownDecreaseDictionary[text2];
			num2 += dictionary["Percentage"].AsDouble();
		}
		if (1.0 + num2 > 0.001)
		{
			num /= 1.0 + num2;
		}
		num = TowerDefenseManager.ApplyMapPacketCooldownRules(_GetType(), num);
		if (num < 0.0)
		{
			num = 0.0;
		}
		if (GodotObject.IsInstanceValid(_override) && _override.packetCooldown != -1.0)
		{
			return _override.packetCooldown * num;
		}
		if (overridePacketCooldown != -1.0)
		{
			return overridePacketCooldown * num;
		}
		if (!GodotObject.IsInstanceValid(characterConfig))
		{
			return 0.0;
		}
		return characterConfig.packetCooldown * num;
	}

	public double GetStartingCooldown()
	{
		double num = 1.0;
		if (TowerDefenseManager.IsUnlimitedFire())
		{
			num *= 2.0 / 3.0;
		}
		if (GodotObject.IsInstanceValid(_override) && _override.startingCooldown != -1.0)
		{
			return _override.startingCooldown * num;
		}
		if (overrideStartingCooldown != -1.0)
		{
			return overrideStartingCooldown * num;
		}
		if (!GodotObject.IsInstanceValid(characterConfig))
		{
			return 0.0;
		}
		return characterConfig.startingCooldown * num;
	}

	public int GetWeight()
	{
		if (GodotObject.IsInstanceValid(_override) && _override.weight != -1)
		{
			return _override.weight;
		}
		if (overrideWeight != -1)
		{
			return overrideWeight;
		}
		return (characterConfig as TowerDefenseZombieConfig)?.weight ?? (-1);
	}

	public int GetWavePointCost()
	{
		if (GodotObject.IsInstanceValid(_override) && _override.wavePointCost != -1)
		{
			return _override.wavePointCost;
		}
		if (overrideWavePointCost != -1)
		{
			return overrideWavePointCost;
		}
		return (characterConfig as TowerDefenseZombieConfig)?.wavePointCost ?? (-1);
	}

	public Array<string> GetPlantCover()
	{
		if (GodotObject.IsInstanceValid(_override) && _override.plantCover != null && _override.plantCover.Count > 0)
		{
			return _override.plantCover;
		}
		if (!GodotObject.IsInstanceValid(characterConfig) || characterConfig.plantCover == null)
		{
			return new Array<string>();
		}
		return characterConfig.plantCover;
	}

	public bool GetCoverCanDirectPlant()
	{
		if (GodotObject.IsInstanceValid(_override))
		{
			return _override.coverCanDirectPlant;
		}
		return false;
	}

	public bool GetHypnoses()
	{
		if (GodotObject.IsInstanceValid(_override) && _override.hypnoses)
		{
			return _override.hypnoses;
		}
		return overrideHypnoses;
	}

	public bool IsLimitGridNum()
	{
		if (GodotObject.IsInstanceValid(_override))
		{
			return _override.islimitGridNum;
		}
		return true;
	}

	private bool TryGetPlantGridOverrideReplaceTarget(TowerDefenseCellInstance cell, TowerDefenseCharacter character, bool noLimit, out TowerDefenseCharacter replaceTarget)
	{
		replaceTarget = null;
		if (noLimit)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(cell) || !GodotObject.IsInstanceValid(character))
		{
			return false;
		}
		if (!(characterConfig is TowerDefensePlantConfig))
		{
			return false;
		}
		if (GetPlantCover().Count > 0)
		{
			return false;
		}
		if (characterConfig.plantGridOverrideType == TowerDefenseEnum.PLANTGRIDTYPE.NOONE)
		{
			return false;
		}
		if (!ShouldReplacePlantGridOverrideTarget())
		{
			return false;
		}
		cell.ClearEmpty();
		foreach (TowerDefenseEnum.PLANTGRIDTYPE item in cell.GetGridType())
		{
			if (cell.slot.ContainsKey(item))
			{
				TowerDefenseCharacter towerDefenseCharacter = cell.slot[item];
				if (CanReplacePlantGridOverrideTarget(towerDefenseCharacter, item))
				{
					replaceTarget = towerDefenseCharacter;
					return true;
				}
			}
		}
		return false;
	}

	private bool ShouldReplacePlantGridOverrideTarget()
	{
		bool flag;
		switch (characterConfig.plantGridOverrideType)
		{
		case TowerDefenseEnum.PLANTGRIDTYPE.NOONE:
			return false;
		case TowerDefenseEnum.PLANTGRIDTYPE.LILYPAD:
		case TowerDefenseEnum.PLANTGRIDTYPE.POT:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return false;
		}
		return true;
	}

	private bool CanReplacePlantGridOverrideTarget(TowerDefenseCharacter slotCharacter, TowerDefenseEnum.PLANTGRIDTYPE type)
	{
		if (!GodotObject.IsInstanceValid(slotCharacter))
		{
			return false;
		}
		if (slotCharacter is TowerDefenseItemSheild)
		{
			return false;
		}
		if (!(slotCharacter is TowerDefensePlant))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(slotCharacter.instance))
		{
			return false;
		}
		if (slotCharacter.instance.hypnoses != GetHypnoses() || slotCharacter.instance.hologram)
		{
			return false;
		}
		if (!characterConfig.plantGridType.Contains(type))
		{
			return false;
		}
		return slotCharacter.config.plantGridType.Contains(characterConfig.plantGridOverrideType);
	}

	public TowerDefenseCharacter Plant(EconomyAccountId economyOwnerAccountId, Vector2I gridPos, bool playAudio = true, bool noLimit = false, bool skipPlacementCheck = false)
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return Plant(gridPos, playAudio, noLimit, economyOwnerAccountId, skipPlacementCheck);
	}

	public TowerDefenseCharacter Plant(Vector2I gridPos, bool playAudio = true, bool noLimit = false, EconomyAccountId economyOwnerAccountId = default(EconomyAccountId), bool skipPlacementCheck = false, bool editorPreviewMode = false)
	{
		bool isEditorPreview = IsEditorPreviewContext(editorPreviewMode);
		TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(gridPos);
		if (GodotObject.IsInstanceValid(cell) && GodotObject.IsInstanceValid(cell.FindPlantingBlocker(this)))
		{
			return null;
		}
		if (Global.Instance.isEditor && SceneManager.CurrentScene == "LevelEditorStage" && GodotObject.IsInstanceValid(cell) && !(characterConfig is TowerDefenseVaseConfig) && cell.HasVase())
		{
			((TowerDefenseVase)cell.GetVase()).packetConfig = (TowerDefensePacketConfig)Duplicate(deep: true);
			return null;
		}
		if (!(characterConfig is TowerDefenseZombieConfig) && !GodotObject.IsInstanceValid(cell))
		{
			return null;
		}
		if (skipPlacementCheck && characterConfig is TowerDefensePlantConfig towerDefensePlantConfig)
		{
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				if (!GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(gridPos + item)))
				{
					return null;
				}
			}
		}
		if (!(characterConfig is TowerDefenseZombieConfig) && !skipPlacementCheck && !cell.CanPacketPlant(this, noLimit))
		{
			return null;
		}
		string text = characterConfig.name;
		TowerDefenseCharacter character = (isEditorPreview ? TowerDefenseManager.GetChacraterScene(text).Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled) : TowerDefenseManager.Instance.CreateCharacter(text));
		AssignEconomyOwner(character, economyOwnerAccountId);
		if (isEditorPreview)
		{
			character.inGame = false;
			character.editorPreviewMode = true;
			character.editorMapPreviewMode = true;
			character.ProcessMode = Node.ProcessModeEnum.Disabled;
		}
		ApplyInitialArmor(character);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(gridPos);
		Vector2 vector = ResolvePreparedCharacterMountPosition(characterNode, mapCellPlantPos, isEditorPreview);
		if (isEditorPreview)
		{
			character.SetLogicalGlobalPosition(vector);
		}
		else
		{
			SetPreparedCharacterGlobalPosition(character, vector);
		}
		character.gridPos = gridPos;
		character.cost = characterConfig.cost;
		character.packet = this;
		if (packetFlip)
		{
			character.Scale = new Vector2(0f - character.Scale.X, character.Scale.Y);
		}
		if (GodotObject.IsInstanceValid(cell))
		{
			character.groundHeight = cell.GetGroundHeight();
		}
		character.z = character.groundHeight;
		if (GodotObject.IsInstanceValid(TowerDefenseInGameLevelControl.instance))
		{
			TowerDefenseInGameLevelControl.instance.hasSpawn = true;
		}
		characterNode.CallDeferred("add_child", character);
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(characterNode) && !character.IsQueuedForDeletion() && !characterNode.IsQueuedForDeletion())
			{
				if (GetHypnoses())
				{
					character.Hypnoses();
				}
				if (GodotObject.IsInstanceValid(_override) && GodotObject.IsInstanceValid(_override.characterOverride))
				{
					_override.characterOverride.ExecuteCharacter(character);
				}
				if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(characterNode) && !character.IsQueuedForDeletion() && !characterNode.IsQueuedForDeletion())
				{
					if (isEditorPreview)
					{
						character.inGame = false;
						character.editorPreviewMode = true;
						character.ProcessMode = Node.ProcessModeEnum.Disabled;
						character.sprite.ProcessMode = Node.ProcessModeEnum.Always;
						character.sprite.SetAnimation(packetAnimeClip);
						if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
						{
							TowerDefenseManager.Instance.CharacterRegister(character);
						}
					}
					if (character.GetParent() == characterNode)
					{
						int num = -1;
						int num2 = -1;
						for (int i = 0; i < characterNode.GetChildCount(); i++)
						{
							if (characterNode.GetChild(i) is TowerDefenseCharacter towerDefenseCharacter && towerDefenseCharacter != character && towerDefenseCharacter.itemLayer == character.itemLayer && towerDefenseCharacter.gridPos.Y == character.gridPos.Y && towerDefenseCharacter.gridPos.X > num2 && towerDefenseCharacter.gridPos.X <= character.gridPos.X)
							{
								num2 = towerDefenseCharacter.gridPos.X;
								num = i;
							}
						}
						if (num != -1)
						{
							characterNode.MoveChild(character, num);
						}
					}
					if (overrideCost != -1)
					{
						character.cost = characterConfig.cost;
					}
					TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(GD.Load<PackedScene>("uid://du8ukldfc7fh7"), gridPos);
					characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, Node.InternalMode.Disabled);
					towerDefenseEffectParticlesOnce.GlobalPosition = character.GetLogicalGlobalPosition(character.transformPoint);
					if (plantUseCell && !(characterConfig is TowerDefenseZombieConfig))
					{
						if (TryGetPlantGridOverrideReplaceTarget(cell, character, noLimit, out var replaceTarget))
						{
							cell.CharacterReplace(replaceTarget, character);
						}
						else
						{
							cell.CharacterPlant(this, character, noLimit);
						}
						if (characterConfig is TowerDefensePlantConfig towerDefensePlantConfig2)
						{
							foreach (Vector2I item2 in towerDefensePlantConfig2.extendGrid)
							{
								TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos + item2);
								if (mapCell != cell)
								{
									mapCell.CharacterPlant(this, character, noLimit);
								}
							}
						}
					}
					if (GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && (!Global.Instance.isEditor || SceneManager.CurrentScene != "LevelEditorStage") && characterConfig is TowerDefenseZombieConfig && character is TowerDefenseZombie character2)
					{
						IsZombieWalk(character2);
					}
					if (playAudio)
					{
						if (GodotObject.IsInstanceValid(cell) && cell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER))
						{
							AudioManager.Instance.AudioPlay("PlantWater");
						}
						else
						{
							AudioManager.Instance.AudioPlay("Plant");
						}
					}
				}
			}
		}).CallDeferred();
		return character;
	}

	public TowerDefenseCharacter PlantOnZombie(EconomyAccountId economyOwnerAccountId, TowerDefenseCharacter zombie, bool hypnoses = false, bool playAudio = true)
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return PlantOnZombie(zombie, hypnoses, playAudio, economyOwnerAccountId);
	}

	public TowerDefenseCharacter PlantOnZombie(TowerDefenseCharacter zombie, bool hypnoses = false, bool playAudio = true, EconomyAccountId economyOwnerAccountId = default(EconomyAccountId))
	{
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return null;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(zombie.gridPos);
		if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(mapCell.FindPlantingBlocker(this)))
		{
			return null;
		}
		if (zombie.camp != ((!hypnoses) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.GetChacraterScene(characterConfig.name).Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		AssignEconomyOwner(towerDefenseCharacter, economyOwnerAccountId);
		ApplyInitialArmor(towerDefenseCharacter);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		SetPreparedCharacterGlobalPosition(towerDefenseCharacter, zombie.GetLogicalGlobalPosition() / TowerDefenseManager.GetMapFeature().mapControl.GlobalScale.Y);
		towerDefenseCharacter.gridPos = zombie.gridPos;
		towerDefenseCharacter.cost = characterConfig.cost;
		towerDefenseCharacter.packet = this;
		if (packetFlip)
		{
			towerDefenseCharacter.Scale = new Vector2(0f - towerDefenseCharacter.Scale.X, towerDefenseCharacter.Scale.Y);
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.cell))
		{
			towerDefenseCharacter.groundHeight = towerDefenseCharacter.cell.GetGroundHeight();
		}
		towerDefenseCharacter.z = towerDefenseCharacter.groundHeight;
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
		if (hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (GodotObject.IsInstanceValid(_override) && GodotObject.IsInstanceValid(_override.characterOverride))
		{
			_override.characterOverride.ExecuteCharacter(towerDefenseCharacter);
		}
		TowerDefensePlant plantChar = towerDefenseCharacter as TowerDefensePlant;
		if (plantChar != null)
		{
			plantChar.targetZombie = zombie;
			towerDefenseCharacter.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
			zombie.OnDestroy += delegate
			{
				plantChar.OnTargetZombieDestroyed();
			};
		}
		else
		{
			TowerDefenseItem itemChar = towerDefenseCharacter as TowerDefenseItem;
			if (itemChar != null)
			{
				itemChar.targetZombie = zombie;
				towerDefenseCharacter.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
				zombie.OnDestroy += delegate
				{
					itemChar.OnTargetZombieDestroyed();
				};
			}
		}
		if (playAudio)
		{
			AudioManager.Instance.AudioPlay("Plant");
		}
		if (GodotObject.IsInstanceValid(TowerDefenseInGameLevelControl.instance))
		{
			TowerDefenseInGameLevelControl.instance.hasSpawn = true;
		}
		return towerDefenseCharacter;
	}

	public TowerDefenseCharacter PlantOnPlant(EconomyAccountId economyOwnerAccountId, TowerDefenseCharacter plant, bool playAudio = true)
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return PlantOnPlant(plant, playAudio, economyOwnerAccountId);
	}

	public TowerDefenseCharacter PlantOnPlant(TowerDefenseCharacter plant, bool playAudio = true, EconomyAccountId economyOwnerAccountId = default(EconomyAccountId), bool editorPreviewMode = false)
	{
		if (!GodotObject.IsInstanceValid(plant))
		{
			return null;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(plant.gridPos);
		if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(mapCell.FindPlantingBlocker(this)))
		{
			return null;
		}
		bool hypnoses = GetHypnoses();
		bool flag = IsEditorPreviewContext(editorPreviewMode);
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.GetChacraterScene(characterConfig.name).Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		towerDefenseCharacter.InheritCoverSleepState(plant);
		AssignEconomyOwner(towerDefenseCharacter, economyOwnerAccountId);
		if (flag)
		{
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
			towerDefenseCharacter.editorMapPreviewMode = true;
			towerDefenseCharacter.ProcessMode = Node.ProcessModeEnum.Disabled;
		}
		ApplyInitialArmor(towerDefenseCharacter);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		Vector2 mapWorldPosition = (flag ? characterNode.ToGlobal(plant.Position) : plant.GetLogicalGlobalPosition());
		Vector2 vector = ResolvePreparedCharacterMountPosition(characterNode, mapWorldPosition, flag);
		if (flag)
		{
			towerDefenseCharacter.SetLogicalGlobalPosition(vector);
		}
		else
		{
			SetPreparedCharacterGlobalPosition(towerDefenseCharacter, vector);
		}
		towerDefenseCharacter.gridPos = plant.gridPos;
		towerDefenseCharacter.cost = characterConfig.cost;
		towerDefenseCharacter.packet = this;
		if (packetFlip)
		{
			towerDefenseCharacter.Scale = new Vector2(0f - towerDefenseCharacter.Scale.X, towerDefenseCharacter.Scale.Y);
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.cell))
		{
			towerDefenseCharacter.groundHeight = towerDefenseCharacter.cell.GetGroundHeight();
		}
		towerDefenseCharacter.z = towerDefenseCharacter.groundHeight;
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
		if (flag)
		{
			towerDefenseCharacter.ProcessMode = Node.ProcessModeEnum.Disabled;
			towerDefenseCharacter.sprite.ProcessMode = Node.ProcessModeEnum.Always;
			towerDefenseCharacter.sprite.SetAnimation(packetAnimeClip);
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.CharacterRegister(towerDefenseCharacter);
			}
		}
		if (hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (GodotObject.IsInstanceValid(_override) && GodotObject.IsInstanceValid(_override.characterOverride))
		{
			_override.characterOverride.ExecuteCharacter(towerDefenseCharacter);
		}
		TowerDefensePlant plantChar = towerDefenseCharacter as TowerDefensePlant;
		if (plantChar != null)
		{
			plantChar.targetPlant = plant;
			towerDefenseCharacter.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
			plant.OnDestroy += delegate
			{
				plantChar.OnTargetPlantDestroyed();
			};
		}
		else
		{
			TowerDefenseItem itemChar = towerDefenseCharacter as TowerDefenseItem;
			if (itemChar != null)
			{
				itemChar.targetPlant = plant;
				towerDefenseCharacter.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
				plant.OnDestroy += delegate
				{
					itemChar.OnTargetPlantDestroyed();
				};
			}
		}
		if (playAudio)
		{
			AudioManager.Instance.AudioPlay("Plant");
		}
		if (GodotObject.IsInstanceValid(TowerDefenseInGameLevelControl.instance))
		{
			TowerDefenseInGameLevelControl.instance.hasSpawn = true;
		}
		return towerDefenseCharacter;
	}

	public bool HasSpawnLimit()
	{
		if (characterConfig is TowerDefenseZombieConfig towerDefenseZombieConfig)
		{
			if (towerDefenseZombieConfig.spawnLineNeed.Count > 0)
			{
				return true;
			}
			if (towerDefenseZombieConfig.excludeLineGridType.Count > 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool CanSpawn(int line)
	{
		bool result = true;
		if (characterConfig is TowerDefenseZombieConfig towerDefenseZombieConfig)
		{
			foreach (TowerDefenseEnum.PLANTGRIDTYPE item in towerDefenseZombieConfig.spawnLineNeed)
			{
				if (!TowerDefenseManager.MapLineHasType(line, item))
				{
					result = false;
					break;
				}
			}
			foreach (TowerDefenseEnum.PLANTGRIDTYPE item2 in towerDefenseZombieConfig.excludeLineGridType)
			{
				if (TowerDefenseManager.MapLineHasType(line, item2))
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	public TowerDefenseCharacter Spawn(EconomyAccountId economyOwnerAccountId, int line, double offsetX = 0.0, bool isIdle = false)
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return Spawn(line, offsetX, isIdle, economyOwnerAccountId);
	}

	internal TowerDefenseCharacter SpawnWaveZombieFromReadOnlyConfig(int line, double offsetX = 0.0, bool isIdle = false)
	{
		if (!(characterConfig is TowerDefenseZombieConfig))
		{
			return null;
		}
		return Spawn(line, offsetX, isIdle);
	}

	public TowerDefenseCharacter Spawn(int line, double offsetX = 0.0, bool isIdle = false, EconomyAccountId economyOwnerAccountId = default(EconomyAccountId))
	{
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		string text = characterConfig.name;
		long startBytes2 = TowerDefenseAllocationTelemetry.Begin();
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		TowerDefenseManager.GetChacraterScene(text);
		TowerDefensePerfProfiler.End("character.spawn.sceneLookup", startTicks2);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.CharacterSpawnSceneLookup, startBytes2);
		long startBytes3 = TowerDefenseAllocationTelemetry.Begin();
		long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.Instance.CreateCharacter(text);
		TowerDefensePerfProfiler.End("character.spawn.instantiate", startTicks3);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.CharacterSpawnInstantiate, startBytes3);
		long startBytes4 = TowerDefenseAllocationTelemetry.Begin();
		long startTicks4 = TowerDefensePerfProfiler.BeginHotPath();
		AssignEconomyOwner(towerDefenseCharacter, economyOwnerAccountId);
		ApplyInitialArmor(towerDefenseCharacter);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		Vector2 globalPosition = new Vector2((float)((double)(mapFeature.config.edge.Z + 40f) + offsetX), (float)TowerDefenseManager.GetMapLineY(line));
		SetPreparedCharacterGlobalPosition(towerDefenseCharacter, globalPosition);
		towerDefenseCharacter.gridPos = new Vector2I(-1, line);
		towerDefenseCharacter.packet = this;
		TowerDefensePerfProfiler.End("character.spawn.initialize", startTicks4);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.CharacterSpawnInitialize, startBytes4);
		long startBytes5 = TowerDefenseAllocationTelemetry.Begin();
		long startTicks5 = TowerDefensePerfProfiler.BeginHotPath();
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
		TowerDefensePerfProfiler.End("character.spawn.attachFresh", startTicks5);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.CharacterSpawnAttachFresh, startBytes5);
		long startBytes6 = TowerDefenseAllocationTelemetry.Begin();
		long startTicks6 = TowerDefensePerfProfiler.BeginHotPath();
		if (!isIdle)
		{
			string text2 = spawnMethod;
			if (!(text2 == "Rise"))
			{
				if (text2 == "FlyDown")
				{
					towerDefenseCharacter.isGround = false;
					towerDefenseCharacter.z = 900.0;
				}
			}
			else
			{
				towerDefenseCharacter.Rise();
			}
			towerDefenseCharacter.Spawn();
		}
		if (!isIdle && characterConfig is TowerDefenseZombieConfig && towerDefenseCharacter is TowerDefenseZombie character)
		{
			IsZombieWalk(character);
		}
		RandomAnime(towerDefenseCharacter);
		if (GodotObject.IsInstanceValid(TowerDefenseInGameLevelControl.instance))
		{
			TowerDefenseInGameLevelControl.instance.hasSpawn = true;
		}
		TowerDefensePerfProfiler.End("character.spawn.entry", startTicks6);
		TowerDefensePerfProfiler.End("character.spawn.total", startTicks);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.CharacterSpawnEntry, startBytes6);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.CharacterSpawnTotal, startBytes);
		return towerDefenseCharacter;
	}

	public TowerDefenseCharacter Create(EconomyAccountId economyOwnerAccountId, Vector2 pos, Vector2I gridPos, double height = 0.0)
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return Create(pos, gridPos, height, economyOwnerAccountId);
	}

	public TowerDefenseCharacter Create(Vector2 pos, Vector2I gridPos, double height = 0.0, EconomyAccountId economyOwnerAccountId = default(EconomyAccountId))
	{
		string characterName = characterConfig.name;
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.Instance.CreateCharacter(characterName);
		AssignEconomyOwner(towerDefenseCharacter, economyOwnerAccountId);
		ApplyInitialArmor(towerDefenseCharacter);
		towerDefenseCharacter.z = height;
		SetPreparedCharacterGlobalPosition(towerDefenseCharacter, pos);
		towerDefenseCharacter.gridPos = gridPos;
		towerDefenseCharacter.packet = this;
		if (GodotObject.IsInstanceValid(TowerDefenseInGameLevelControl.instance))
		{
			TowerDefenseInGameLevelControl.instance.hasSpawn = true;
		}
		return towerDefenseCharacter;
	}

	private static void AssignEconomyOwner(TowerDefenseCharacter character, EconomyAccountId economyOwnerAccountId)
	{
		if (economyOwnerAccountId.IsValid)
		{
			character.TryAssignEconomyOwner(economyOwnerAccountId);
		}
	}

	private void ApplyInitialArmor(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(characterConfig?.armorData) || initArmor == null || initArmor.Count == 0)
		{
			return;
		}
		bool flag = GodotObject.IsInstanceValid(character.instance);
		foreach (string item in initArmor)
		{
			if (!string.IsNullOrEmpty(item))
			{
				if (!character.currentArmor.Contains(item))
				{
					character.currentArmor.Add(item);
				}
				if (flag)
				{
					character.instance.ArmorAdd(item);
				}
			}
		}
	}

	private static bool IsEditorPreviewContext(bool explicitlyRequested)
	{
		if (explicitlyRequested || (Global.Instance.isEditor && SceneManager.CurrentScene == "LevelEditorStage"))
		{
			return true;
		}
		LevelEditorMapEditor instance = LevelEditorMapEditor.instance;
		if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(instance.mapFeature))
		{
			return instance.mapFeature.editorPreviewMode;
		}
		return false;
	}

	internal static Vector2 ResolvePreparedCharacterMountPosition(Node2D characterNode, Vector2 mapWorldPosition, bool isEditorPreview)
	{
		if (isEditorPreview)
		{
			return characterNode.ToLocal(mapWorldPosition);
		}
		return mapWorldPosition / TowerDefenseManager.GetMapFeature().mapControl.GlobalScale.Y;
	}

	public void RandomAnime(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.sprite.frameIndex += GD.RandRange(0, 20);
		}
	}

	public async void IsZombieWalk(TowerDefenseCharacter character)
	{
		await ToSignal(character.GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!character.die && !character.nearDie && !character.isRise)
		{
			character.CallDeferred("Walk");
		}
	}

	public bool Unlock()
	{
		if (XWModPlayerProgressService.TryPacketUnlock(this, out var unlocked))
		{
			return unlocked;
		}
		if (CommandManager.Instance.debugPacketOpenAll)
		{
			return true;
		}
		Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue(saveKey);
		if (!towerDefensePacketValue.GetValueOrDefault("Unlock", false).AsBool())
		{
			if (unlockCheckList == null || unlockCheckList.Count <= 0)
			{
				return false;
			}
			foreach (UnlockConditionBaseConfig unlockCheck in unlockCheckList)
			{
				if (!unlockCheck.Check())
				{
					return false;
				}
			}
			towerDefensePacketValue["Unlock"] = true;
			GameSaveManager.Instance.SetTowerDefensePacketValue(saveKey, towerDefensePacketValue);
			GameSaveManager.Instance.Save();
		}
		return true;
	}

	public void ColdDownDecreaseAdd(TowerDefenseCharacter controlCharacter, string key, double percentage)
	{
		if (!coldDownDecreaseDictionary.ContainsKey(key))
		{
			coldDownDecreaseDictionary[key] = new Dictionary
			{
				["ControlCharacterList"] = new Array(),
				["Percentage"] = percentage
			};
		}
		Array array = (Array)((Dictionary)coldDownDecreaseDictionary[key])["ControlCharacterList"];
		if (!array.Contains(controlCharacter))
		{
			controlCharacter.OnDestroy += delegate
			{
				ColdDownDecreaseDelete(controlCharacter, key);
			};
			array.Add(controlCharacter);
		}
	}

	public void ColdDownDecreaseDelete(TowerDefenseCharacter controlCharacter, string key)
	{
		if (coldDownDecreaseDictionary.ContainsKey(key))
		{
			Array array = (Array)((Dictionary)coldDownDecreaseDictionary[key])["ControlCharacterList"];
			array.Remove(controlCharacter);
			if (array.Count <= 0)
			{
				coldDownDecreaseDictionary.Remove(key);
			}
		}
	}

	public bool ChangeCostAdd(TowerDefensePacketChangeCost changeCost)
	{
		if (changeCost.key != "")
		{
			foreach (TowerDefensePacketChangeCost changeCost2 in changeCostList)
			{
				if (changeCost2.key == changeCost.key)
				{
					return false;
				}
			}
		}
		changeCostList.Add(changeCost);
		if (changeCost.lockCost)
		{
			canChangeCost = false;
		}
		return true;
	}

	public bool ChangeCostRemove(TowerDefensePacketChangeCost changeCost)
	{
		int num = changeCostList.IndexOf(changeCost);
		if (num == -1)
		{
			return false;
		}
		changeCostList.RemoveAt(num);
		if (changeCost.lockCost)
		{
			bool flag = false;
			foreach (TowerDefensePacketChangeCost changeCost2 in changeCostList)
			{
				if (changeCost2.lockCost)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				canChangeCost = true;
			}
		}
		return true;
	}
}
