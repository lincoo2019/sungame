using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.Tools;

public sealed class XWTemplatePresentation
{
	private static readonly Dictionary<string, string> DisplayNameOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["feature-csharp"] = "关卡功能脚本",
		["process-csharp"] = "关卡流程脚本",
		["projectile-csharp"] = "子弹脚本",
		["shared-csharp"] = "通用 C# 脚本",
		["debug-entry-csharp"] = "Mod 调试入口脚本",
		["character-event-csharp"] = "角色事件脚本",
		["shovel-event-csharp"] = "铲子事件脚本",
		["card-event-csharp"] = "卡牌事件脚本",
		["character-zombie"] = "僵尸角色配置",
		["simple-resource"] = "通用资源",
		["adobe-animate-xfl"] = "动画源文件",
		["menu-dialog-scene"] = "菜单对话框",
		["dialog-popup-scene"] = "弹窗对话框",
		["npc-talk-resource"] = "非玩家角色对话",
		["localization-csv"] = "多语言表格"
	};

	private static readonly Dictionary<string, string> SuggestedNameOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["feature-csharp"] = "NewFeature",
		["process-csharp"] = "NewProcess",
		["projectile-csharp"] = "NewProjectileScript",
		["shared-csharp"] = "NewScript",
		["debug-entry-csharp"] = "ModDebugEntry",
		["character-event-csharp"] = "NewCharacterEvent",
		["shovel-event-csharp"] = "NewShovelEvent",
		["card-event-csharp"] = "NewCardEvent",
		["character-zombie"] = "NewZombieConfig",
		["simple-resource"] = "NewResource"
	};

	public string CategoryLabel { get; init; } = "模板";

	public string DisplayNameLabel { get; init; } = "新模板";

	public string DescriptionLabel { get; init; } = "创建新的 Mod 内容。";

	public string PreviewTextLabel { get; init; } = "创建后可继续进行可视化配置。";

	public string DefaultFolderLabel { get; init; } = "工程目录";

	public string SuggestedName { get; init; } = "NewTemplate";

	public static XWTemplatePresentation Resolve(XWTemplateLibrary.TemplateInfo template)
	{
		if (template == null)
		{
			return new XWTemplatePresentation();
		}
		string categoryLabel = ResolveCategoryLabel(template);
		string text = ResolveDisplayNameLabel(template);
		string text2 = ResolveFolderLabel(template.DefaultFolder);
		string descriptionLabel = (HasChineseText(template.Description) ? template.Description.Trim() : ("用于创建“" + text + "”，保存后可直接进入对应编辑界面。"));
		string previewTextLabel = (HasChineseText(template.PreviewText) ? template.PreviewText.Trim() : $"创建后可在“{text2}”中继续配置“{text}”。");
		string value = "";
		if (XWResourceCreateRoute.TryGetTemplateActionPresentation(template.Id, out var _, out var defaultName))
		{
			value = defaultName;
		}
		if (string.IsNullOrWhiteSpace(value) && !SuggestedNameOverrides.TryGetValue(template.Id, out value))
		{
			value = BuildSuggestedName(template.Id);
		}
		return new XWTemplatePresentation
		{
			CategoryLabel = categoryLabel,
			DisplayNameLabel = text,
			DescriptionLabel = descriptionLabel,
			PreviewTextLabel = previewTextLabel,
			DefaultFolderLabel = text2,
			SuggestedName = (string.IsNullOrWhiteSpace(value) ? "NewTemplate" : value)
		};
	}

	public static bool HasChineseText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		foreach (char c in text)
		{
			if (c >= '㐀' && c <= '鿿')
			{
				return true;
			}
		}
		return false;
	}

	private static string ResolveCategoryLabel(XWTemplateLibrary.TemplateInfo template)
	{
		string text = ResolveFolderLabel(template.DefaultFolder);
		if (HasChineseText(text))
		{
			return text;
		}
		return template.Category switch
		{
			"Feature" => "关卡功能", 
			"Process" => "关卡流程", 
			"Character" => "角色", 
			"CharacterEvent" => "角色事件", 
			"Component" => "角色组件", 
			"Projectile" => "子弹", 
			"GUI" => "游戏界面", 
			"Blueprint" => "蓝图", 
			_ => "Mod 模板", 
		};
	}

	private static string ResolveDisplayNameLabel(XWTemplateLibrary.TemplateInfo template)
	{
		if (DisplayNameOverrides.TryGetValue(template.Id, out var value))
		{
			return value;
		}
		if (XWResourceCreateRoute.TryGetTemplateActionPresentation(template.Id, out var displayName, out var _))
		{
			return StripActionVerb(displayName);
		}
		if (HasChineseText(template.DisplayName))
		{
			return template.DisplayName.Trim();
		}
		return "Mod 模板";
	}

	private static string ResolveFolderLabel(string defaultFolder)
	{
		string displayName = XWModProjectLayout.GetDisplayName(defaultFolder ?? "");
		if (!string.IsNullOrWhiteSpace(displayName))
		{
			return displayName.Trim();
		}
		return "工程目录";
	}

	private static string StripActionVerb(string displayName)
	{
		string text = (displayName ?? "").Trim();
		string[] array = new string[3] { "新建", "导入", "创建" };
		foreach (string text2 in array)
		{
			if (text.StartsWith(text2, StringComparison.Ordinal))
			{
				return text.Substring(text2.Length).Trim();
			}
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "Mod 模板";
	}

	private static string BuildSuggestedName(string templateId)
	{
		StringBuilder stringBuilder = new StringBuilder("New");
		string[] array = (templateId ?? "").Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (string text in array)
		{
			bool flag;
			switch (text)
			{
			case "csharp":
			case "resource":
			case "scene":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				stringBuilder.Append(CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text));
			}
		}
		if (stringBuilder.Length <= 3)
		{
			return "NewTemplate";
		}
		return stringBuilder.ToString();
	}
}
