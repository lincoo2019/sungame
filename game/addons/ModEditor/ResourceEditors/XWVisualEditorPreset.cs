using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ResourceEditors;

public sealed class XWVisualEditorPreset
{
	private const int MaxDetailRows = 64;

	private const int MaxReferenceRows = 48;

	private const int MaxVisualGridCells = 90;

	public string Category { get; set; } = "";

	public string Title { get; set; } = "";

	public string Summary { get; set; } = "";

	public XWVisualEditorDescriptor.SurfaceKind PrimarySurface { get; set; }

	public List<string> ToolbarActions { get; } = new List<string>();

	public List<string> CanvasItems { get; } = new List<string>();

	public List<string> TimelineItems { get; } = new List<string>();

	public List<string> GraphItems { get; } = new List<string>();

	public List<string> PreviewItems { get; } = new List<string>();

	public List<string> DetailItems { get; } = new List<string>();

	public List<string> ReferenceItems { get; } = new List<string>();

	public static XWVisualEditorPreset CreateFor(XWVisualEditorDescriptor descriptor, Resource resource, string path)
	{
		XWVisualEditorPreset xWVisualEditorPreset = new XWVisualEditorPreset
		{
			Category = (descriptor?.Category ?? ""),
			Title = (descriptor?.DisplayName ?? "资源编辑器"),
			Summary = BuildSummary(resource, path),
			PrimarySurface = (descriptor?.Kind ?? XWVisualEditorDescriptor.SurfaceKind.Preview)
		};
		xWVisualEditorPreset.ToolbarActions.AddRange(new string[7] { "打开", "保存", "撤销", "重做", "自动保存", "恢复草稿", "发布检查" });
		AppendCategoryPreset(xWVisualEditorPreset);
		AppendResourceData(xWVisualEditorPreset, resource, path);
		return xWVisualEditorPreset;
	}

	private static void AppendCategoryPreset(XWVisualEditorPreset preset)
	{
		switch (preset.Category)
		{
		case "Level":
			preset.Title = "关卡编辑器";
			preset.CanvasItems.AddRange(new string[5] { "地图格子", "可种植点", "路线边界", "选中格配置", "卡牌栏" });
			preset.TimelineItems.AddRange(new string[4] { "波次时间线", "僵尸刷新", "事件节点", "Feature/Process 启用表" });
			preset.GraphItems.AddRange(new string[3] { "Feature/Process 依赖", "关卡条件", "胜负流程" });
			preset.DetailItems.AddRange(new string[5] { "关卡 key", "地图绑定", "卡牌选择", "Feature/Process 属性", "测试入口" });
			preset.ReferenceItems.AddRange(new string[5] { "引用地图", "引用卡牌", "引用角色", "引用脚本", "引用蓝图" });
			break;
		case "Map":
			preset.Title = "地图编辑器";
			preset.CanvasItems.AddRange(new string[6] { "背景图", "格子刷选", "行列", "边界", "偏移", "可种植类型" });
			preset.DetailItems.AddRange(new string[5] { "地图 key", "背景资源", "格子尺寸", "行列数量", "碰撞边界" });
			preset.ReferenceItems.AddRange(new string[3] { "引用贴图", "引用音频", "被关卡引用" });
			break;
		case "Character":
			preset.Title = "角色编辑器";
			preset.PreviewItems.AddRange(new string[5] { "角色预览", "分类创建", "继承内置角色", "覆盖内置角色", "Component 列表" });
			preset.GraphItems.AddRange(new string[3] { "Component 继承", "禁用/替换/新增", "排序链路" });
			preset.DetailItems.AddRange(new string[7] { "category", "creationMode", "baseCharacterKey", "targetCharacterKey", "sceneOverride", "spriteOverride", "Component Patch" });
			preset.ReferenceItems.AddRange(new string[5] { "引用场景", "引用贴图", "引用动画", "引用脚本", "引用蓝图" });
			break;
		case "Card":
			preset.Title = "卡片编辑器";
			preset.PreviewItems.AddRange(new string[6] { "真实卡牌预览", "图鉴预览", "角色绑定", "费用", "冷却", "标签" });
			preset.DetailItems.AddRange(new string[6] { "Localization / 多语言", "多语言名称", "多语言描述", "图鉴文本", "卡牌数值", "卡牌库归属" });
			preset.ReferenceItems.AddRange(new string[4] { "引用角色", "引用图标", "引用图鉴贴图", "引用翻译 key" });
			break;
		case "Projectile":
		case "ProjectileChange":
			preset.Title = ((preset.Category == "ProjectileChange") ? "子弹变化编辑器" : "子弹编辑器");
			preset.CanvasItems.AddRange(new string[4] { "轨迹预览", "命中测试", "特效预览", "ProjectileChange 链路" });
			preset.GraphItems.AddRange(new string[5] { "命中后变形", "分裂", "替换", "链式触发", "终止条件" });
			preset.DetailItems.AddRange(new string[5] { "子弹 key", "速度/加速度", "碰撞", "命中特效", "ProjectileChange 参数" });
			preset.ReferenceItems.AddRange(new string[4] { "引用贴图", "引用音频", "引用脚本", "引用蓝图" });
			break;
		case "Collectable":
			preset.Title = "收集物编辑器";
			preset.PreviewItems.AddRange(new string[4] { "掉落预览", "拾取范围", "数值收益", "消失动画" });
			preset.DetailItems.AddRange(new string[5] { "收集物 key", "类型", "数值", "拾取音效", "掉落权重" });
			break;
		case "Mower":
			preset.Title = "小推车编辑器";
			preset.PreviewItems.AddRange(new string[4] { "小推车预览", "触发线", "移动速度", "清场范围" });
			preset.DetailItems.AddRange(new string[5] { "小推车 key", "场景", "速度", "伤害", "音效" });
			break;
		case "Shovel":
			preset.Title = "铲子编辑器";
			preset.PreviewItems.AddRange(new string[4] { "铲子预览", "可铲目标", "冷却", "反馈动画" });
			preset.DetailItems.AddRange(new string[5] { "铲子 key", "图标", "音效", "目标过滤", "交互提示" });
			break;
		case "Survival":
			preset.Title = "生存模式编辑器";
			preset.CanvasItems.AddRange(new string[4] { "阶段配置", "波次池", "奖励", "失败条件" });
			preset.TimelineItems.AddRange(new string[3] { "阶段时间线", "难度曲线", "事件触发" });
			break;
		case "FallingObject":
			preset.Title = "掉落物编辑器";
			preset.CanvasItems.AddRange(new string[4] { "落点预览", "轨迹", "生成区域", "命中效果" });
			preset.DetailItems.AddRange(new string[5] { "掉落物 key", "下落速度", "目标行列", "生成权重", "命中脚本" });
			break;
		case "Animation":
			preset.Title = "动画编辑器";
			preset.DetailItems.AddRange(new string[5] { "动画 key", "帧率", "导出路径", "贴图集", "事件绑定" });
			break;
		case "Audio":
		case "BGM":
			preset.Title = ((preset.Category == "BGM") ? "BGM 编辑器" : "音频编辑器");
			preset.PreviewItems.AddRange(new string[4] { "波形预览", "试听", "循环点", "音量预览" });
			preset.TimelineItems.AddRange(new string[4] { "播放时间线", "淡入淡出", "循环段", "BGM 切换点" });
			preset.DetailItems.AddRange(new string[5] { "音频 key", "文件", "音量", "循环", "BGM 场景绑定" });
			break;
		case "Shop":
		case "Dialog":
		case "Tutorial":
		case "NpcTalk":
			preset.Title = preset.Category switch
			{
				"Tutorial" => "教程编辑器", 
				"NpcTalk" => "NPC 对话编辑器", 
				"Shop" => "商店编辑器", 
				_ => "GUI 编辑器", 
			};
			preset.PreviewItems.AddRange(new string[4] { "所见即所得预览", "文本预览", "按钮/选项预览", "Localization / 多语言状态" });
			preset.GraphItems.AddRange(new string[4] { "流程图", "条件", "分支", "跳转" });
			preset.DetailItems.AddRange(new string[5] { "多语言文本", "条件", "奖励", "商店条目", "UI 绑定" });
			preset.ReferenceItems.AddRange(new string[4] { "引用角色", "引用贴图", "引用音频", "引用翻译 key" });
			break;
		default:
			preset.PreviewItems.AddRange(new string[4] { "资源缩略图", "可视化预览", "主要属性", "测试预览" });
			preset.DetailItems.AddRange(new string[4] { "key", "分类", "资源文件", "检查器详情" });
			preset.ReferenceItems.AddRange(new string[3] { "正向引用", "反向引用", "缺失资源" });
			break;
		}
	}

	private static void AppendResourceData(XWVisualEditorPreset preset, Resource resource, string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			AddUnique(preset.DetailItems, "文件: " + path);
		}
		if (resource == null)
		{
			AddUnique(preset.PreviewItems, "尚未打开资源");
			AddUnique(preset.DetailItems, "从文件系统打开对应资源后，这里会显示实际资源数据。");
			return;
		}
		AddUnique(preset.DetailItems, "类型: " + resource.GetType().Name);
		if (!string.IsNullOrWhiteSpace(resource.ResourceName))
		{
			AddUnique(preset.DetailItems, "资源名: " + resource.ResourceName);
		}
		switch (preset.Category)
		{
		case "Map":
			AppendMapGridPreview(preset, resource);
			break;
		case "Character":
			AppendCharacterPreview(preset, resource);
			break;
		case "Card":
			AppendCardPreview(preset, resource);
			break;
		case "Projectile":
		case "ProjectileChange":
			AppendProjectileChangePreview(preset, resource);
			break;
		case "Level":
			AppendLevelPreview(preset, resource);
			break;
		}
		AppendResourceProperties(preset, resource);
		AppendResourceReferences(preset, resource);
	}

	private static void AppendMapGridPreview(XWVisualEditorPreset preset, Resource resource)
	{
		if (TryGetProperty(resource, "gridNum", out var value) && value.VariantType == Variant.Type.Vector2I)
		{
			Vector2I vector2I = value.AsVector2I();
			int num = Math.Clamp(vector2I.X, 1, 30);
			int num2 = Math.Clamp(vector2I.Y, 1, 20);
			AddUnique(preset.DetailItems, $"地图行列: {num} x {num2}");
			int num3 = num * num2;
			int num4 = Math.Min(num3, 90);
			for (int i = 0; i < num4; i++)
			{
				int value2 = i % num + 1;
				int value3 = i / num + 1;
				AddUnique(preset.CanvasItems, $"格子 {value2},{value3}");
			}
			if (num3 > num4)
			{
				AddUnique(preset.CanvasItems, $"还有 {num3 - num4} 个格子...");
			}
		}
		AppendKnownDetail(preset, resource, "mapTexture", "背景贴图");
		AppendKnownDetail(preset, resource, "mapScene", "地图场景");
		AppendKnownDetail(preset, resource, "gridBeginPos", "格子起点");
		AppendKnownDetail(preset, resource, "gridSize", "格子尺寸");
		AppendKnownDetail(preset, resource, "edge", "地图边界");
	}

	private static void AppendCharacterPreview(XWVisualEditorPreset preset, Resource resource)
	{
		AddUnique(preset.PreviewItems, "分类: 植物 / 僵尸 / 花瓶 / 小推车 / 物品 / 墓碑 / 弹坑");
		AddUnique(preset.PreviewItems, "创建模式: 空白 / 继承内置 / 覆盖内置");
		AddUnique(preset.GraphItems, "Component Patch: 继承 / 禁用 / 替换 / 新增 / 排序");
		AppendKnownDetail(preset, resource, "name", "角色 key");
		AppendKnownDetail(preset, resource, "hitpoints", "生命值");
		AppendKnownDetail(preset, resource, "cost", "阳光费用");
		AppendKnownDetail(preset, resource, "packetCooldown", "冷却时间");
		AppendKnownDetail(preset, resource, "plantGridType", "可种植格子");
		AppendKnownDetail(preset, resource, "ashScene", "灰烬场景");
	}

	private static void AppendCardPreview(XWVisualEditorPreset preset, Resource resource)
	{
		AppendKnownDetail(preset, resource, "saveKey", "卡片 key");
		AppendKnownDetail(preset, resource, "name", "多语言名称");
		AppendKnownDetail(preset, resource, "describe", "多语言描述");
		AppendKnownDetail(preset, resource, "handbookDescribe", "图鉴描述");
		AppendKnownDetail(preset, resource, "characterConfig", "绑定角色");
		AppendKnownCardBaseCost(preset, resource);
		AppendKnownDetail(preset, resource, "type", "卡片类型");
		AppendKnownDetail(preset, resource, "packetAnimeClip", "卡片动画");
	}

	private static void AppendKnownCardBaseCost(XWVisualEditorPreset preset, Resource resource)
	{
		if (resource is TowerDefensePacketConfig towerDefensePacketConfig && GodotObject.IsInstanceValid(towerDefensePacketConfig.characterConfig))
		{
			AddUnique(preset.DetailItems, $"基础价格: {towerDefensePacketConfig.GetCost(skipGlobalChangeCost: true)}");
		}
	}

	private static void AppendProjectileChangePreview(XWVisualEditorPreset preset, Resource resource)
	{
		AddUnique(preset.GraphItems, "ProjectileChange: 原子弹 -> 变化目标");
		AddUnique(preset.GraphItems, "命中后动作: 变形 / 分裂 / 替换 / 终止");
		AppendKnownDetail(preset, resource, "name", "子弹 key");
		AppendKnownDetail(preset, resource, "baseDamage", "基础伤害");
		AppendKnownDetail(preset, resource, "projectileScene", "子弹场景");
		AppendKnownDetail(preset, resource, "splatScene", "命中特效");
		AppendKnownDetail(preset, resource, "splatAudio", "命中音效");
		AppendKnownDetail(preset, resource, "methods", "子弹方法链");
	}

	private static void AppendLevelPreview(XWVisualEditorPreset preset, Resource resource)
	{
		AppendKnownDetail(preset, resource, "name", "关卡 key");
		AppendKnownDetail(preset, resource, "levelName", "关卡名称");
		AppendKnownDetail(preset, resource, "description", "关卡描述");
		AppendKnownDetail(preset, resource, "nextLevel", "下一关");
		AddUnique(preset.GraphItems, "Feature 启用/禁用");
		AddUnique(preset.GraphItems, "Process 模板/脚本/蓝图绑定");
	}

	private static void AppendKnownDetail(XWVisualEditorPreset preset, Resource resource, string propertyName, string label)
	{
		if (TryGetProperty(resource, propertyName, out var value))
		{
			AddUnique(preset.DetailItems, label + ": " + FormatVariantValue(value));
		}
	}

	private static void AppendResourceProperties(XWVisualEditorPreset preset, Resource resource)
	{
		int num = 0;
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (num >= 64)
			{
				AddUnique(preset.DetailItems, "还有更多属性，打开检查器查看。");
				break;
			}
			if (TryReadPropertyName(property, out var name) && !ShouldSkipProperty(name) && IsEditorProperty(property) && TryGetProperty(resource, name, out var value))
			{
				AddUnique(preset.DetailItems, name + ": " + FormatVariantValue(value));
				num++;
			}
		}
	}

	private static void AppendResourceReferences(XWVisualEditorPreset preset, Resource resource)
	{
		int num = 0;
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (num >= 48)
			{
				AddUnique(preset.ReferenceItems, "还有更多引用，打开资源引用图查看。");
				return;
			}
			if (TryReadPropertyName(property, out var name) && !ShouldSkipProperty(name) && TryGetProperty(resource, name, out var value))
			{
				num += AppendVariantReferences(preset.ReferenceItems, name, value, 48 - num);
			}
		}
		if (num == 0)
		{
			AddUnique(preset.ReferenceItems, "未检测到显式资源引用");
		}
	}

	private static int AppendVariantReferences(List<string> target, string label, Variant value, int remaining)
	{
		if (remaining <= 0)
		{
			return 0;
		}
		Variant.Type variantType = value.VariantType;
		if (variantType != Variant.Type.String)
		{
			Variant.Type num = variantType - 21;
			if ((ulong)num > 7uL)
			{
				goto IL_01a3;
			}
			switch ((int)num)
			{
			case 3:
				if (value.AsGodotObject() is Resource resource)
				{
					string resourcePath = GetResourcePath(resource);
					if (!string.IsNullOrWhiteSpace(resourcePath))
					{
						AddUnique(target, label + " -> " + resourcePath);
						return 1;
					}
				}
				goto IL_01a3;
			case 0:
			case 1:
				break;
			case 7:
			{
				int num3 = 0;
				foreach (Variant item in value.AsGodotArray())
				{
					num3 += AppendVariantReferences(target, $"{label}[{num3}]", item, remaining - num3);
					if (num3 >= remaining)
					{
						break;
					}
				}
				return num3;
			}
			case 6:
			{
				int num2 = 0;
				foreach (Variant key in value.AsGodotDictionary().Keys)
				{
					Variant value2 = value.AsGodotDictionary()[key];
					num2 += AppendVariantReferences(target, label + "." + key.AsString(), value2, remaining - num2);
					if (num2 >= remaining)
					{
						break;
					}
				}
				return num2;
			}
			default:
				goto IL_01a3;
			}
		}
		string text = value.AsString();
		if (LooksLikeResourceReference(text))
		{
			AddUnique(target, label + " -> " + text);
			return 1;
		}
		goto IL_01a3;
		IL_01a3:
		return 0;
	}

	private static string BuildSummary(Resource resource, string path)
	{
		string obj = ((resource == null) ? "路径资源" : resource.GetType().Name);
		string text = (string.IsNullOrWhiteSpace(path) ? "未选择文件" : path);
		return obj + "  " + text;
	}

	private static bool TryReadPropertyName(Dictionary property, out string name)
	{
		name = "";
		if (property == null || !property.ContainsKey("name"))
		{
			return false;
		}
		name = property["name"].AsString();
		return !string.IsNullOrWhiteSpace(name);
	}

	private static bool IsEditorProperty(Dictionary property)
	{
		if (property == null || !property.ContainsKey("usage"))
		{
			return true;
		}
		return ((ulong)property["usage"].AsInt32() & 4uL) != 0;
	}

	private static bool ShouldSkipProperty(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return true;
		}
		switch (name)
		{
		default:
			return name.StartsWith("_", StringComparison.Ordinal);
		case "script":
		case "resource_name":
		case "resource_path":
		case "resource_local_to_scene":
			return true;
		}
	}

	private static bool TryGetProperty(Resource resource, string propertyName, out Variant value)
	{
		value = default(Variant);
		if (resource == null || string.IsNullOrWhiteSpace(propertyName))
		{
			return false;
		}
		try
		{
			value = resource.Get(propertyName);
			return true;
		}
		catch (Exception ex)
		{
			GD.PushWarning("ModEditor resource preset could not read '" + propertyName + "': " + ex.Message);
			return false;
		}
	}

	private static string FormatVariantValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 28uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 1:
				return value.AsBool() ? "启用" : "关闭";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			case 4:
			case 21:
			case 22:
				return string.IsNullOrEmpty(value.AsString()) ? "空" : value.AsString();
			case 5:
				return FormatVector(value.AsVector2());
			case 6:
				return FormatVector(value.AsVector2I());
			case 9:
				return FormatVector(value.AsVector3());
			case 10:
				return FormatVector(value.AsVector3I());
			case 12:
				return FormatVector(value.AsVector4());
			case 13:
				return FormatVector(value.AsVector4I());
			case 20:
				return value.AsColor().ToHtml();
			case 28:
				return $"数组({value.AsGodotArray().Count})";
			case 27:
				return $"字典({value.AsGodotDictionary().Count})";
			case 24:
				return FormatObject(value.AsGodotObject());
			}
		}
		return value.ToString();
	}

	private static string FormatObject(GodotObject obj)
	{
		if (obj == null || !GodotObject.IsInstanceValid(obj))
		{
			return "空";
		}
		if (obj is Resource resource)
		{
			string resourcePath = GetResourcePath(resource);
			if (!string.IsNullOrWhiteSpace(resourcePath))
			{
				return resourcePath;
			}
			if (!string.IsNullOrWhiteSpace(resource.ResourceName))
			{
				return resource.GetType().Name + "(" + resource.ResourceName + ")";
			}
		}
		return obj.GetType().Name;
	}

	private static string FormatVector(Vector2 value)
	{
		return $"({value.X:0.###}, {value.Y:0.###})";
	}

	private static string FormatVector(Vector2I value)
	{
		return $"({value.X}, {value.Y})";
	}

	private static string FormatVector(Vector3 value)
	{
		return $"({value.X:0.###}, {value.Y:0.###}, {value.Z:0.###})";
	}

	private static string FormatVector(Vector3I value)
	{
		return $"({value.X}, {value.Y}, {value.Z})";
	}

	private static string FormatVector(Vector4 value)
	{
		return $"({value.X:0.###}, {value.Y:0.###}, {value.Z:0.###}, {value.W:0.###})";
	}

	private static string FormatVector(Vector4I value)
	{
		return $"({value.X}, {value.Y}, {value.Z}, {value.W})";
	}

	private static string GetResourcePath(Resource resource)
	{
		if (resource == null || !GodotObject.IsInstanceValid(resource))
		{
			return "";
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return resource.ResourcePath;
		}
		return "";
	}

	private static bool LooksLikeResourceReference(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("uid://", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".res", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
		{
			return text.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static void AddUnique(List<string> list, string item)
	{
		if (!string.IsNullOrWhiteSpace(item) && !list.Contains(item))
		{
			list.Add(item);
		}
	}
}
