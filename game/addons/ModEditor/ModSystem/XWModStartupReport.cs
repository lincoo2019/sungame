using System;
using System.Collections.Generic;
using System.Linq;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModStartupReport
{
	public int LegacyCount { get; internal set; }

	public IReadOnlyList<string> ConfiguredIds { get; internal set; } = Array.Empty<string>();

	public IReadOnlyList<string> LoadedIds { get; internal set; } = Array.Empty<string>();

	public IReadOnlyDictionary<string, string> FailedById { get; internal set; } = new Dictionary<string, string>();

	public XWModEnvironmentStatus Environment { get; internal set; }

	public string BlockingReason { get; internal set; } = "";

	public bool HasFailures
	{
		get
		{
			if (LegacyCount >= 0)
			{
				return FailedById.Count > 0;
			}
			return true;
		}
	}

	public string PlayerSummary()
	{
		string text = $"本次启动已加载 {LoadedIds.Count} 个 Mod。";
		if (FailedById.Count > 0)
		{
			text = text + "\n以下 Mod 未正常加载：\n" + string.Join("\n", from item in FailedById.Take(6)
				select item.Key + "：" + item.Value);
		}
		if (FailedById.Count > 6)
		{
			text += "\n其余问题请查看 Mod 管理。";
		}
		if (LegacyCount < 0)
		{
			text = text + "\nMod 环境未能完成加载，请查看诊断：" + BlockingReason;
		}
		if (HasFailures)
		{
			text += "\n可在 Mod 管理中查看诊断或停用，再重启游戏重试。";
		}
		return text;
	}
}
