using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class TowerDefenseLevelSaveConfigCSharp : Resource
{
	[Export(PropertyHint.None, "")]
	public Dictionary modSnapshot;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterSaveConfigCSharp> characterList = new Array<TowerDefenseCharacterSaveConfigCSharp>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseProjectileSaveConfigCSharp> projectileList = new Array<TowerDefenseProjectileSaveConfigCSharp>();

	[Export(PropertyHint.None, "")]
	public Array<Dictionary> bulletFieldList = new Array<Dictionary>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseDropItemSaveConfigCSharp> dropItemList = new Array<TowerDefenseDropItemSaveConfigCSharp>();

	[Export(PropertyHint.None, "")]
	public Dictionary featureSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary processSave = new Dictionary();

	[Export(PropertyHint.None, "")]
	public Dictionary gameStateSave = new Dictionary();

	public System.Collections.Generic.Dictionary<StringName, TowerDefenseCharacter> charcterDicionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseCharacter>();

	public ProgressRestoreReport RestoreReport { get; private set; } = new ProgressRestoreReport();

	internal bool HasProcessSaveData
	{
		get
		{
			Dictionary data;
			return TryGetSection(processSave, "main", out data);
		}
	}

	public bool CanLoad(out string reason)
	{
		reason = "";
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.currentControl))
		{
			reason = "当前战斗控制器不可用。";
			return false;
		}
		return true;
	}

	public void Save()
	{
		XWModEnvironmentStatus status = XWModEnvironmentService.GetStatus();
		modSnapshot = ((status.Ready && status.Snapshot != null) ? XWModEnvironmentService.WriteSnapshot(status.Snapshot) : null);
		characterList.Clear();
		foreach (Node item in Global.Instance.GetTree().GetNodesInGroup("Character"))
		{
			if (item is TowerDefenseCharacter { isShow: false, inGame: not false, die: false } towerDefenseCharacter && !towerDefenseCharacter.instance.die)
			{
				TowerDefenseCharacterSaveConfigCSharp towerDefenseCharacterSaveConfigCSharp = new TowerDefenseCharacterSaveConfigCSharp();
				towerDefenseCharacterSaveConfigCSharp.SaveCharacter(towerDefenseCharacter);
				characterList.Add(towerDefenseCharacterSaveConfigCSharp);
			}
		}
		if (projectileList == null)
		{
			projectileList = new Array<TowerDefenseProjectileSaveConfigCSharp>();
		}
		projectileList.Clear();
		if (bulletFieldList == null)
		{
			bulletFieldList = new Array<Dictionary>();
		}
		bulletFieldList.Clear();
		if (GodotObject.IsInstanceValid(BulletField.Instance))
		{
			bulletFieldList = BulletField.Instance.ExportBulletFieldSave();
		}
		dropItemList.Clear();
		foreach (Node item2 in Global.Instance.GetTree().GetNodesInGroup("SunDropItem"))
		{
			if (item2 is TowerDefenseSunBase { die: false, isCollect: false, AccountId: { IsLocal: not false } } towerDefenseSunBase)
			{
				TowerDefenseDropItemSaveConfigCSharp towerDefenseDropItemSaveConfigCSharp = new TowerDefenseDropItemSaveConfigCSharp();
				towerDefenseDropItemSaveConfigCSharp.SaveDropItem(towerDefenseSunBase);
				dropItemList.Add(towerDefenseDropItemSaveConfigCSharp);
			}
		}
		featureSave.Clear();
		processSave.Clear();
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (GodotObject.IsInstanceValid(currentControl))
		{
			foreach (StringName key5 in currentControl.featureDictionary.Keys)
			{
				Dictionary dictionary = currentControl.featureDictionary[key5].SaveFeature();
				if (dictionary.Count > 0)
				{
					featureSave[key5] = dictionary;
				}
			}
			if (GodotObject.IsInstanceValid(currentControl.process))
			{
				Dictionary dictionary2 = currentControl.process.SaveProcess();
				if (dictionary2.Count > 0)
				{
					processSave["main"] = dictionary2;
				}
			}
		}
		gameStateSave = new Dictionary
		{
			["runGameTime"] = TowerDefenseManager.Instance.runGameTime,
			["timeScale"] = Global.Instance.timeScale,
			["pausePacket"] = TowerDefenseManager.Instance.pausePacket,
			["pauseZombie"] = TowerDefenseManager.Instance.pauseZombie
		};
	}

	public bool Load(bool resetReport = true)
	{
		if (resetReport)
		{
			RestoreReport = new ProgressRestoreReport();
		}
		if (!CanLoad(out var reason))
		{
			GD.PushWarning("[ProgressLoad] " + reason);
			return false;
		}
		TowerDefenseControlNew control = TowerDefenseManager.Instance.currentControl;
		control.featureDictionary.TryGetValue("Map", out var value);
		TowerDefenseBattleFeatureMap mapFeature = value as TowerDefenseBattleFeatureMap;
		if (GodotObject.IsInstanceValid(mapFeature) && !GodotObject.IsInstanceValid(mapFeature.currentMap))
		{
			GD.PushWarning("[ProgressLoad] 当前关卡地图未能建立。");
			return false;
		}
		Dictionary mapSaveData = null;
		if (TryGetFeatureSaveData("Map", out mapSaveData) && GodotObject.IsInstanceValid(mapFeature))
		{
			RestoreReport.Try("Map", delegate
			{
				if (!mapFeature.TryPrepareProgressLoad(mapSaveData, out var reason2))
				{
					RestoreReport.Record("Map", reason2);
				}
			});
		}
		if (characterList == null)
		{
			characterList = new Array<TowerDefenseCharacterSaveConfigCSharp>();
		}
		charcterDicionary.Clear();
		List<(TowerDefenseCharacterSaveConfigCSharp, TowerDefenseCharacter)> list = new List<(TowerDefenseCharacterSaveConfigCSharp, TowerDefenseCharacter)>();
		foreach (TowerDefenseCharacterSaveConfigCSharp saved in characterList)
		{
			TowerDefenseCharacter character = null;
			if (!RestoreReport.Try("Character", delegate
			{
				if (!GodotObject.IsInstanceValid(saved))
				{
					throw new InvalidOperationException("Empty character record.");
				}
				if (string.IsNullOrWhiteSpace(saved.nodeName) || charcterDicionary.ContainsKey(saved.nodeName))
				{
					throw new InvalidOperationException("Empty or duplicate character identity.");
				}
				saved.owner = this;
				character = saved.InstantiateCharacterForRestore();
				if (!GodotObject.IsInstanceValid(character))
				{
					throw new InvalidOperationException("Cannot create character: " + saved.packetName + ".");
				}
			}))
			{
				DiscardRestoredCharacter(character);
				continue;
			}
			charcterDicionary[saved.nodeName] = character;
			list.Add((saved, character));
		}
		foreach (var item in list)
		{
			if (!RestoreReport.Try("Character", delegate
			{
				item.Item1.RestoreCharacter(item.Item2);
			}))
			{
				charcterDicionary.Remove(item.Item1.nodeName);
				DiscardRestoredCharacter(item.Item2);
			}
		}
		if (bulletFieldList == null)
		{
			bulletFieldList = new Array<Dictionary>();
		}
		if (bulletFieldList.Count > 0 || GodotObject.IsInstanceValid(BulletField.Instance))
		{
			RestoreReport.Try("Projectile", delegate
			{
				BulletField bulletField = BulletField.EnsureMountedOnCharacterNode();
				if (GodotObject.IsInstanceValid(bulletField))
				{
					bulletField.ImportBulletFieldSave(bulletFieldList, this);
				}
				else
				{
					RestoreReport.Record("Projectile", "Bullet field unavailable.", bulletFieldList.Count);
				}
			});
		}
		RestoreReport.Record("Projectile", "Skipped unsupported Node projectile records.", projectileList?.Count ?? 0);
		if (dropItemList == null)
		{
			dropItemList = new Array<TowerDefenseDropItemSaveConfigCSharp>();
		}
		foreach (TowerDefenseDropItemSaveConfigCSharp drop in dropItemList)
		{
			RestoreReport.Try("DropItem", delegate
			{
				if (!GodotObject.IsInstanceValid(drop) || !drop.LoadDropItem(this))
				{
					RestoreReport.Record("DropItem", "Skipped unavailable drop item.");
				}
			});
		}
		if (mapSaveData != null && GodotObject.IsInstanceValid(mapFeature))
		{
			RestoreReport.Try("Map", delegate
			{
				mapFeature.LoadFeature(mapSaveData, this);
			});
		}
		if (featureSave == null)
		{
			featureSave = new Dictionary();
		}
		foreach (KeyValuePair<Variant, Variant> item2 in featureSave)
		{
			Variant.Type variantType = item2.Key.VariantType;
			if ((variantType != Variant.Type.String && variantType != Variant.Type.StringName) || 1 == 0)
			{
				RestoreReport.Record("Feature", "Invalid Feature name.");
				continue;
			}
			string name = item2.Key.AsString();
			if (name == "Map")
			{
				continue;
			}
			RestoreReport.Try("Feature", delegate
			{
				if (!TryGetFeatureSaveData(name, out var data2))
				{
					throw new InvalidOperationException("Invalid Feature record: " + name + ".");
				}
				if (!control.featureDictionary.TryGetValue(name, out var value2) || !GodotObject.IsInstanceValid(value2))
				{
					RestoreReport.Record("Feature", "Unavailable Feature: " + name + ".");
				}
				else
				{
					value2.LoadFeature(data2, this);
				}
			});
		}
		foreach (KeyValuePair<StringName, TowerDefenseBattleFeature> pair in control.featureDictionary)
		{
			if (!GodotObject.IsInstanceValid(pair.Value) || TryGetFeatureSaveData(pair.Key.ToString(), out var _))
			{
				continue;
			}
			RestoreReport.Try("Feature", delegate
			{
				if (pair.Value.CanLoadProgress())
				{
					Dictionary dictionary2 = pair.Value.SaveFeature();
					if (dictionary2 != null && dictionary2.Count > 0)
					{
						RestoreReport.Record("Feature", $"Using initial state for {pair.Key}.");
					}
				}
			});
		}
		if (GodotObject.IsInstanceValid(control.process))
		{
			if (TryGetSection(processSave, "main", out var processData))
			{
				RestoreReport.Try("Process", delegate
				{
					control.process.LoadProcess(processData, this);
				});
			}
			else
			{
				RestoreReport.Try("Process", delegate
				{
					if (control.process.CanLoadProgress())
					{
						Dictionary dictionary2 = control.process.SaveProcess();
						if (dictionary2 != null && dictionary2.Count > 0)
						{
							RestoreReport.Record("Process", "Using initial Process state.");
						}
					}
				});
			}
		}
		else
		{
			Dictionary dictionary = processSave;
			if (dictionary != null && dictionary.Count > 0)
			{
				RestoreReport.Record("Process", "Saved Process is unavailable.");
			}
		}
		TowerDefenseManager.Instance.runGameTime = ReadFinite(gameStateSave, "runGameTime", 0.0);
		Global.Instance.timeScale = ReadFinite(gameStateSave, "timeScale", 1.0);
		TowerDefenseManager.Instance.pausePacket = ReadBool(gameStateSave, "pausePacket", fallback: false);
		TowerDefenseManager.Instance.pauseZombie = ReadBool(gameStateSave, "pauseZombie", fallback: false);
		return true;
	}

	internal bool TryGetFeatureSaveData(string name, out Dictionary data)
	{
		return TryGetSection(featureSave, name, out data);
	}

	internal static bool TryGetSection(Dictionary container, string name, out Dictionary data)
	{
		data = null;
		if (container == null)
		{
			return false;
		}
		bool flag;
		foreach (KeyValuePair<Variant, Variant> item in container)
		{
			Variant.Type variantType = item.Key.VariantType;
			flag = ((variantType == Variant.Type.String || variantType == Variant.Type.StringName) ? true : false);
			if (!flag || item.Key.AsString() != name)
			{
				continue;
			}
			if (item.Value.VariantType != Variant.Type.Dictionary)
			{
				flag = false;
			}
			else
			{
				data = item.Value.AsGodotDictionary();
				flag = data != null;
			}
			goto IL_009e;
		}
		return false;
		IL_009e:
		return flag;
	}

	internal double ReadFinite(Dictionary data, string key, double fallback, string category = "State")
	{
		if (data == null || !data.TryGetValue(key, out var value))
		{
			return fallback;
		}
		Variant.Type variantType = value.VariantType;
		if (((ulong)(variantType - 2) <= 1uL) ? true : false)
		{
			double num = value.AsDouble();
			if (double.IsFinite(num))
			{
				return num;
			}
		}
		RestoreReport.Record(category, "Invalid numeric field: " + key + "; using default.");
		return fallback;
	}

	internal bool ReadBool(Dictionary data, string key, bool fallback, string category = "State")
	{
		if (data == null || !data.TryGetValue(key, out var value))
		{
			return fallback;
		}
		if (value.VariantType == Variant.Type.Bool)
		{
			return value.AsBool();
		}
		RestoreReport.Record(category, "Invalid boolean field: " + key + "; using default.");
		return fallback;
	}

	internal static void DiscardRestoredCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.ProcessMode = Node.ProcessModeEnum.Disabled;
			character.GetParent()?.RemoveChild(character);
			character.QueueFree();
		}
	}
}
