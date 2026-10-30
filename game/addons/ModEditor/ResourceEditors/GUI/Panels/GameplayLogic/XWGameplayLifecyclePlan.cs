using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public static class XWGameplayLifecyclePlan
{
	public enum Phase
	{
		Ready,
		Battle,
		Settle
	}

	public sealed record Entry(double Time, Phase Phase, string Title, string Detail);

	public sealed class Snapshot
	{
		public string ResourceType { get; init; } = "Resource";

		public string Summary { get; init; } = "配置投影";

		public double ReadyDuration { get; init; }

		public double BattleDuration { get; init; }

		public double SettleDuration { get; init; }

		public IReadOnlyList<Entry> Entries { get; init; } = System.Array.Empty<Entry>();

		public double BattleStart => ReadyDuration;

		public double SettleStart => ReadyDuration + BattleDuration;

		public double TotalDuration => ReadyDuration + BattleDuration + SettleDuration;
	}

	public const int MaximumProjectedEntries = 36;

	private static readonly string[] CollectionProperties = new string[13]
	{
		"wave", "waves", "spawn", "gridSpawn", "preSpawn", "eventList", "events", "featureList", "features", "processList",
		"processes", "broadcastList", "zombiePool"
	};

	private static readonly string[] DurationProperties = new string[11]
	{
		"broadCastTime", "entryBroadcastDuration", "cameraTravelDuration", "packetBankExitDelay", "entryLabelDuration", "debugEnterHouseFadeDuration", "enterHouseFadeDuration", "settlementLineDelay", "duration", "waitTime",
		"delay"
	};

	public static Snapshot Build(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return Empty();
		}
		string name = resource.GetType().Name;
		System.Collections.Generic.Dictionary<string, Variant> dictionary = ReadProjectionValues(resource);
		string text = FamilyOf(name);
		List<(string, int)> list = new List<(string, int)>();
		string[] collectionProperties = CollectionProperties;
		foreach (string text2 in collectionProperties)
		{
			if (dictionary.TryGetValue(text2, out var value) && TryCount(value, out var count) && count > 0)
			{
				list.Add((text2, count));
			}
		}
		double val = 0.0;
		collectionProperties = DurationProperties;
		foreach (string key in collectionProperties)
		{
			if (dictionary.TryGetValue(key, out var value2) && TryNumber(value2, out var number) && number > 0.0)
			{
				val = Math.Max(val, number);
			}
		}
		int num = 0;
		foreach (var item4 in list)
		{
			int item = item4.Item2;
			num += item;
		}
		double num2 = 2.0;
		double num3 = Math.Clamp(Math.Max(text switch
		{
			"波次" => 18.0, 
			"流程" => 20.0, 
			"事件" => 8.0, 
			"功能" => 24.0, 
			"广播" => 6.0, 
			_ => 12.0, 
		}, Math.Max(val, (double)num * 1.5)), 4.0, 120.0);
		double num4 = 3.0;
		List<Entry> list2 = new List<Entry>
		{
			new Entry(0.0, Phase.Ready, "读取配置快照", name + " · 不启动真实战斗"),
			new Entry(1.0, Phase.Ready, "检查引用与参数", (list.Count == 0) ? "没有可展开的集合" : $"发现 {num} 个时间线项目"),
			new Entry(num2, Phase.Battle, "进入 Battle", "开始" + text + "的可视化投影")
		};
		if (list.Count == 0)
		{
			list2.Add(new Entry(num2 + num3 * 0.5, Phase.Battle, "预览" + text, "仅更新沙盘画面和时间游标"));
		}
		else
		{
			int num5 = Math.Min(num, 36);
			int num6 = 0;
			foreach (var item5 in list)
			{
				string item2 = item5.Item1;
				int item3 = item5.Item2;
				int num7 = 0;
				while (num7 < item3 && num6 < num5)
				{
					double num8 = ((double)num6 + 1.0) / ((double)num5 + 1.0);
					list2.Add(new Entry(num2 + num3 * num8, Phase.Battle, $"{DisplayProperty(item2)} #{num7 + 1}", $"配置标记 {num7 + 1}/{item3}，不执行生成/奖励/结算"));
					num7++;
					num6++;
				}
				if (num6 >= num5)
				{
					break;
				}
			}
			if (num > num5)
			{
				list2.Add(new Entry(num2 + num3 * 0.94, Phase.Battle, "其余项目已折叠", $"时间线最多显示 {36} 项，配置仍完整保留"));
			}
		}
		list2.Add(new Entry(num2 + num3, Phase.Settle, "进入 Settle", "汇总沙盘历史，不写入存档"));
		list2.Add(new Entry(num2 + num3 + num4, Phase.Settle, "预览完成", "所有运行时副作用均保持关闭"));
		return new Snapshot
		{
			ResourceType = name,
			Summary = $"{text} · {num} 项 · {num3:0.#} 秒投影",
			ReadyDuration = num2,
			BattleDuration = num3,
			SettleDuration = num4,
			Entries = list2
		};
	}

	public static Phase PhaseAt(Snapshot snapshot, double time)
	{
		if (snapshot == null || time < snapshot.BattleStart)
		{
			return Phase.Ready;
		}
		if (!(time < snapshot.SettleStart))
		{
			return Phase.Settle;
		}
		return Phase.Battle;
	}

	private static Snapshot Empty()
	{
		return new Snapshot
		{
			Summary = "尚未绑定配置",
			ReadyDuration = 2.0,
			BattleDuration = 8.0,
			SettleDuration = 3.0,
			Entries = new Entry[1]
			{
				new Entry(0.0, Phase.Ready, "等待配置", "选择 Feature / Process / Event / Wave")
			}
		};
	}

	private static System.Collections.Generic.Dictionary<string, Variant> ReadProjectionValues(Resource resource)
	{
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.Ordinal);
		HashSet<string> hashSet = new HashSet<string>(CollectionProperties, StringComparer.Ordinal);
		hashSet.UnionWith(DurationProperties);
		foreach (Dictionary property in resource.GetPropertyList())
		{
			Variant value;
			string text = (property.TryGetValue("name", out value) ? value.AsString() : "");
			if (hashSet.Contains(text))
			{
				dictionary[text] = resource.Get(text);
			}
		}
		return dictionary;
	}

	private static bool TryCount(Variant value, out int count)
	{
		count = value.VariantType switch
		{
			Variant.Type.Array => value.AsGodotArray().Count, 
			Variant.Type.Dictionary => value.AsGodotDictionary().Count, 
			_ => 0, 
		};
		Variant.Type variantType = value.VariantType;
		if ((ulong)(variantType - 27) <= 1uL)
		{
			return true;
		}
		return false;
	}

	private static bool TryNumber(Variant value, out double number)
	{
		number = value.VariantType switch
		{
			Variant.Type.Int => value.AsInt64(), 
			Variant.Type.Float => value.AsDouble(), 
			_ => 0.0, 
		};
		Variant.Type variantType = value.VariantType;
		if (((ulong)(variantType - 2) <= 1uL) ? true : false)
		{
			return double.IsFinite(number);
		}
		return false;
	}

	private static string FamilyOf(string typeName)
	{
		if (typeName.Contains("Wave", StringComparison.OrdinalIgnoreCase))
		{
			return "波次";
		}
		if (typeName.Contains("Process", StringComparison.OrdinalIgnoreCase))
		{
			return "流程";
		}
		if (typeName.Contains("Event", StringComparison.OrdinalIgnoreCase))
		{
			return "事件";
		}
		if (typeName.Contains("Feature", StringComparison.OrdinalIgnoreCase))
		{
			return "功能";
		}
		if (typeName.Contains("Broadcast", StringComparison.OrdinalIgnoreCase))
		{
			return "广播";
		}
		return "战斗逻辑";
	}

	private static string DisplayProperty(string property)
	{
		switch (property)
		{
		case "waves":
		case "wave":
			return "推进波次";
		case "spawn":
			return "投影行生成";
		case "gridSpawn":
		case "preSpawn":
			return "投影格子生成";
		case "eventList":
		case "events":
			return "触发事件标记";
		case "features":
		case "featureList":
			return "激活功能标记";
		case "processes":
		case "processList":
			return "推进流程标记";
		case "broadcastList":
			return "播放广播标记";
		case "zombiePool":
			return "读取僵尸池";
		default:
			return property;
		}
	}
}
