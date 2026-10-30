using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventConditionNpcTalkFinish : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public string npcTalkKey = "";

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelEventBase> finishEventList = new Array<TowerDefenseLevelEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelEventBase> unfinishEventList = new Array<TowerDefenseLevelEventBase>();

	public override string _GetName()
	{
		return "LEVLE_EVENT_CONDITION_NPC_TALK_FINISH";
	}

	public override void Execute()
	{
		if (GodotObject.IsInstanceValid(GameSaveManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.ExecuteLevelEvent(GameSaveManager.Instance.GetTutorialValue(npcTalkKey) ? finishEventList : unfinishEventList);
		}
	}

	public override void Init(Dictionary valueDictionary)
	{
		if (valueDictionary == null)
		{
			valueDictionary = new Dictionary();
		}
		npcTalkKey = valueDictionary.GetValueOrDefault("NpcTalkKey", "").AsString();
		if (finishEventList == null)
		{
			finishEventList = new Array<TowerDefenseLevelEventBase>();
		}
		if (unfinishEventList == null)
		{
			unfinishEventList = new Array<TowerDefenseLevelEventBase>();
		}
		finishEventList.Clear();
		unfinishEventList.Clear();
		LoadNestedEvents(valueDictionary, "FinishEvent", finishEventList);
		string key = (valueDictionary.ContainsKey("UnfinishEvent") ? "UnfinishEvent" : "UnfinishedEvent");
		LoadNestedEvents(valueDictionary, key, unfinishEventList);
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "ConditionNpcTalkFinish",
			["Value"] = new Dictionary
			{
				["NpcTalkKey"] = npcTalkKey,
				["FinishEvent"] = ExportNestedEvents(finishEventList, "FinishEvent"),
				["UnfinishEvent"] = ExportNestedEvents(unfinishEventList, "UnfinishEvent")
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["NPC对话完成条件"] = new Dictionary { ["对话存档键"] = new Dictionary
		{
			["Object"] = this,
			["Type"] = "String",
			["Property"] = "npcTalkKey",
			["Rest"] = ""
		} };
		return property;
	}

	private static void LoadNestedEvents(Dictionary source, string key, Array<TowerDefenseLevelEventBase> target)
	{
		Godot.Collections.Array array = source.GetValueOrDefault(key, new Godot.Collections.Array()).AsGodotArray();
		for (int i = 0; i < array.Count; i++)
		{
			try
			{
				if (array[i].VariantType == Variant.Type.Dictionary)
				{
					Dictionary dictionary = array[i].AsGodotDictionary();
					string text = dictionary.GetValueOrDefault("EventName", "").AsString();
					TowerDefenseLevelEventBase towerDefenseLevelEventBase = TowerDefenseLevelEventRegistry.Create(text);
					if (!GodotObject.IsInstanceValid(towerDefenseLevelEventBase))
					{
						GD.PushError($"[ConditionNpcTalkFinish] Unknown {key}[{i}] event '{text}'.");
					}
					else
					{
						towerDefenseLevelEventBase.Init(dictionary.GetValueOrDefault("Value", new Dictionary()).AsGodotDictionary());
						target.Add(towerDefenseLevelEventBase);
					}
				}
			}
			catch (Exception value)
			{
				GD.PushError($"[ConditionNpcTalkFinish] Failed to load {key}[{i}]: {value}");
			}
		}
	}

	private static Godot.Collections.Array ExportNestedEvents(Array<TowerDefenseLevelEventBase> events, string key)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		if (events == null)
		{
			return array;
		}
		for (int i = 0; i < events.Count; i++)
		{
			try
			{
				if (GodotObject.IsInstanceValid(events[i]))
				{
					array.Add(events[i].Export());
				}
			}
			catch (Exception value)
			{
				GD.PushError($"[ConditionNpcTalkFinish] Failed to export {key}[{i}]: {value}");
			}
		}
		return array;
	}
}
