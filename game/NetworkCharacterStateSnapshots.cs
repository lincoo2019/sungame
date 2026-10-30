using System.Collections.Generic;
using Godot;
using Godot.Collections;

internal static class NetworkCharacterStateSnapshots
{
	private const ulong StateMachineCheckpointIntervalMs = 1000uL;

	private const string RuntimeAliveField = "_alive";

	private const string RuntimeAliveCacheSuffix = ":alive";

	public static long GetStateMachineCheckpoint()
	{
		return (long)(Time.GetTicksMsec() / 1000);
	}

	public static Dictionary BuildComponentDelta(TowerDefenseCharacter character, Dictionary componentCache, int syncId, bool force, bool updateCache = true)
	{
		Dictionary dictionary = new Dictionary();
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.componentManager))
		{
			return dictionary;
		}
		foreach (ComponentBase component in character.componentManager.componentList)
		{
			if (component == null)
			{
				continue;
			}
			ComponentBase componentBase = component;
			if (!GodotObject.IsInstanceValid(componentBase))
			{
				continue;
			}
			Dictionary dictionary2 = componentBase.SyncSerialize() ?? new Dictionary();
			if (!character.componentManager.TryGetWireKey(componentBase, out var wireKey))
			{
				continue;
			}
			string text = syncId + ":" + wireKey;
			Dictionary b = componentCache.GetValueOrDefault(text, new Dictionary()).AsGodotDictionary();
			bool num = force || !NetworkVariantComparer.DictionaryApproxEquals(dictionary2, b);
			IStateMachineController stateMachine = componentBase.StateMachine;
			int num2;
			long num3;
			if (stateMachine == null)
			{
				num2 = 0;
			}
			else
			{
				num2 = (stateMachine.IsInitialized ? 1 : 0);
				if (num2 != 0)
				{
					num3 = componentBase.StateMachine.SnapshotRevision;
					goto IL_00df;
				}
			}
			num3 = -1L;
			goto IL_00df;
			IL_00df:
			long num4 = num3;
			long num5 = ((num2 != 0 && componentBase.StateMachine.HasPendingTransition) ? GetStateMachineCheckpoint() : (-1));
			long num6 = componentCache.GetValueOrDefault(text + ":smr", -1).AsInt64();
			long num7 = componentCache.GetValueOrDefault(text + ":smc", -1).AsInt64();
			bool flag = num2 != 0 && (force || num4 != num6 || (num5 >= 0 && num5 != num7));
			if (!num && !flag)
			{
				continue;
			}
			Dictionary dictionary3 = (updateCache ? dictionary2.Duplicate(deep: true) : null);
			Dictionary dictionary4 = dictionary2.Duplicate(deep: true);
			if (flag)
			{
				Dictionary dictionary5 = componentBase.CaptureStateMachineSnapshotData();
				if (dictionary5.Count > 0)
				{
					dictionary4["sm"] = dictionary5;
				}
			}
			if (dictionary4.Count > 0)
			{
				dictionary[wireKey] = dictionary4;
			}
			if (updateCache)
			{
				componentCache[text] = dictionary3;
				componentCache[text + ":smr"] = num4;
				componentCache[text + ":smc"] = num5;
			}
		}
		foreach (CharacterComponentRuntime resourceComponent in character.componentManager.ResourceComponents)
		{
			if (resourceComponent == null || resourceComponent.IsReleased || !character.componentManager.TryGetWireKey(resourceComponent, out var wireKey2))
			{
				continue;
			}
			Dictionary dictionary6 = resourceComponent.SyncSerialize() ?? new Dictionary();
			bool alive = resourceComponent.Alive;
			string text2 = syncId + ":" + wireKey2;
			Dictionary dictionary7 = componentCache.GetValueOrDefault(text2, new Dictionary()).AsGodotDictionary();
			string text3 = text2 + ":alive";
			bool flag2 = componentCache.ContainsKey(text3);
			bool flag3 = flag2 && componentCache[text3].AsBool();
			if (!flag2 && dictionary7.ContainsKey("_alive"))
			{
				flag2 = true;
				flag3 = dictionary7["_alive"].AsBool();
			}
			bool num8 = force || !flag2 || alive != flag3 || !NetworkVariantComparer.DictionaryApproxEquals(dictionary6, dictionary7);
			IStateMachineController stateMachine2 = resourceComponent.StateMachine;
			int num9;
			long num10;
			if (stateMachine2 == null)
			{
				num9 = 0;
			}
			else
			{
				num9 = (stateMachine2.IsInitialized ? 1 : 0);
				if (num9 != 0)
				{
					num10 = resourceComponent.StateMachine.SnapshotRevision;
					goto IL_039e;
				}
			}
			num10 = -1L;
			goto IL_039e;
			IL_039e:
			long num11 = num10;
			long num12 = ((num9 != 0 && resourceComponent.StateMachine.HasPendingTransition) ? GetStateMachineCheckpoint() : (-1));
			long num13 = componentCache.GetValueOrDefault(text2 + ":smr", -1).AsInt64();
			long num14 = componentCache.GetValueOrDefault(text2 + ":smc", -1).AsInt64();
			bool flag4 = num9 != 0 && (force || num11 != num13 || (num12 >= 0 && num12 != num14));
			if (!num8 && !flag4)
			{
				continue;
			}
			Dictionary dictionary8 = (updateCache ? dictionary6.Duplicate(deep: true) : null);
			Dictionary dictionary9 = dictionary6.Duplicate(deep: true);
			dictionary9["_alive"] = alive;
			if (flag4)
			{
				Dictionary dictionary10 = resourceComponent.CaptureStateMachineSnapshotData();
				if (dictionary10.Count > 0)
				{
					dictionary9["sm"] = dictionary10;
				}
			}
			if (dictionary9.Count > 0)
			{
				dictionary[wireKey2] = dictionary9;
			}
			if (updateCache)
			{
				componentCache[text2] = dictionary8;
				componentCache[text3] = alive;
				componentCache[text2 + ":smr"] = num11;
				componentCache[text2 + ":smc"] = num12;
			}
		}
		return dictionary;
	}

	public static void ApplyComponents(TowerDefenseCharacter character, Dictionary componentsData)
	{
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.componentManager))
		{
			return;
		}
		foreach (Variant key in componentsData.Keys)
		{
			string wireKey = key.AsString();
			character.componentManager.ApplyOrQueueNetworkState(wireKey, componentsData[key].AsGodotDictionary());
		}
	}

	public static Array BuildArmorSnapshot(TowerDefenseCharacter character)
	{
		Array array = new Array();
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.instance) || character.instance.armorList == null)
		{
			return array;
		}
		for (int i = 0; i < character.instance.armorList.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = character.instance.armorList[i];
			if (GodotObject.IsInstanceValid(towerDefenseArmorInstance) && !towerDefenseArmorInstance.isRemove)
			{
				string text = towerDefenseArmorInstance.slotConfig?.armorName ?? "";
				if (!(text == ""))
				{
					array.Add(new Dictionary
					{
						["i"] = i,
						["n"] = text,
						["hp"] = Mathf.Snapped((float)towerDefenseArmorInstance.hitPoints, 0.1f),
						["si"] = towerDefenseArmorInstance.stageIndex
					});
				}
			}
		}
		return array;
	}

	public static bool AppendArmorSnapshot(Dictionary characterData, TowerDefenseCharacter character, Dictionary last, bool force)
	{
		Array array = BuildArmorSnapshot(character);
		Array array2 = last.GetValueOrDefault("ar", new Array()).AsGodotArray();
		Dictionary a = new Dictionary { ["ar"] = array };
		Dictionary b = new Dictionary { ["ar"] = array2 };
		if (force || !NetworkVariantComparer.DictionaryApproxEquals(a, b))
		{
			characterData["ar"] = array;
			return true;
		}
		return false;
	}

	public static void ApplyArmorSnapshot(TowerDefenseCharacter character, Array armorsData, bool allowAddMissing = true)
	{
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.instance))
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (Variant armorsDatum in armorsData)
		{
			if (armorsDatum.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary = armorsDatum.AsGodotDictionary();
			string text = dictionary.GetValueOrDefault("n", "").AsString();
			TowerDefenseArmorInstance towerDefenseArmorInstance = ((text != "") ? character.GetArmorFromName(text) : null);
			int num = dictionary.GetValueOrDefault("i", -1).AsInt32();
			if (!GodotObject.IsInstanceValid(towerDefenseArmorInstance) && num >= 0 && num < character.instance.armorList.Count)
			{
				towerDefenseArmorInstance = character.instance.armorList[num];
				text = towerDefenseArmorInstance?.slotConfig?.armorName ?? text;
			}
			if (text != "")
			{
				hashSet.Add(text);
			}
			if (dictionary.GetValueOrDefault("rm", false).AsBool())
			{
				if (GodotObject.IsInstanceValid(towerDefenseArmorInstance))
				{
					RemoveArmorInstance(character, towerDefenseArmorInstance, createDamagePart: true);
				}
				continue;
			}
			if (!GodotObject.IsInstanceValid(towerDefenseArmorInstance) && allowAddMissing && text != "")
			{
				character.instance.ArmorAdd(text);
				towerDefenseArmorInstance = character.GetArmorFromName(text);
			}
			if (!GodotObject.IsInstanceValid(towerDefenseArmorInstance) || towerDefenseArmorInstance.isRemove)
			{
				continue;
			}
			if (dictionary.ContainsKey("hp"))
			{
				towerDefenseArmorInstance.hitPoints = dictionary["hp"].AsDouble();
				towerDefenseArmorInstance.RefreshDamageStageFromHitPoints();
			}
			if (dictionary.ContainsKey("si"))
			{
				int num2 = dictionary["si"].AsInt32();
				if (num2 > towerDefenseArmorInstance.stageIndex)
				{
					for (int i = towerDefenseArmorInstance.stageIndex; i < num2; i++)
					{
						towerDefenseArmorInstance.SetDamageStage(i + 1);
					}
					towerDefenseArmorInstance.stageIndex = num2;
				}
			}
			if (towerDefenseArmorInstance.hitPoints <= 0.0 && !towerDefenseArmorInstance.isRemove)
			{
				RemoveArmorInstance(character, towerDefenseArmorInstance, createDamagePart: true);
			}
		}
		foreach (TowerDefenseArmorInstance item in character.instance.armorList.Duplicate())
		{
			if (GodotObject.IsInstanceValid(item))
			{
				string text2 = item.slotConfig?.armorName ?? "";
				if (text2 != "" && !hashSet.Contains(text2))
				{
					RemoveArmorInstance(character, item, createDamagePart: true);
				}
			}
		}
		ShowHealthComponent showHealthComponent = character.showHealthComponent;
		if (showHealthComponent != null && !showHealthComponent.IsReleased)
		{
			character.showHealthComponent.MarkDirty();
		}
	}

	private static void RemoveArmorInstance(TowerDefenseCharacter character, TowerDefenseArmorInstance armor, bool createDamagePart)
	{
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.instance) || !GodotObject.IsInstanceValid(armor))
		{
			return;
		}
		if (!armor.isRemove)
		{
			if (createDamagePart && (armor.armorMethodFlags & 0x80) != 0 && !armor.damagePartDropped)
			{
				character.DamagePartCreate(new StringName(armor.slotConfig.armorName), null, new Vector2(GD.RandRange(-100, 100), -300f), keepSlotScale: true, Vector2.Zero, fromSync: true, null, 0L);
			}
			armor.RemoveArmor();
			armor.isRemove = true;
		}
		character.instance.RemoveArmorRuntimeReferences(armor);
		ShowHealthComponent showHealthComponent = character.showHealthComponent;
		if (showHealthComponent != null && !showHealthComponent.IsReleased)
		{
			character.showHealthComponent.MarkDirty();
		}
	}
}
