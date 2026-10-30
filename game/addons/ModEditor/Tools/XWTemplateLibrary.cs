using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.FileSystem;

namespace PVZHE.ModEditor.Tools;

public sealed class XWTemplateLibrary
{
	public sealed class TemplateInfo
	{
		public string Id { get; set; } = "";

		public string Category { get; set; } = "";

		public string DisplayName { get; set; } = "";

		public string Description { get; set; } = "";

		public string PreviewText { get; set; } = "";

		public string DefaultFolder { get; set; } = "Scripts";

		public string FileExtension { get; set; } = ".cs";

		public string Content { get; set; } = "";
	}

	public sealed class TemplateCreateResult
	{
		public bool Success { get; set; }

		public string TemplateId { get; set; } = "";

		public string CreatedPath { get; set; } = "";

		public List<string> CreatedPaths { get; set; } = new List<string>();

		public string Error { get; set; } = "";

		public string Warning { get; set; } = "";
	}

	private readonly List<TemplateInfo> _templates = new List<TemplateInfo>();

	public IReadOnlyList<TemplateInfo> Templates => _templates;

	public XWTemplateLibrary()
	{
		RegisterDefaults();
	}

	public void Register(TemplateInfo template)
	{
		if (template != null && !string.IsNullOrWhiteSpace(template.Id))
		{
			_templates.RemoveAll((TemplateInfo t) => t.Id == template.Id);
			_templates.Add(template);
		}
	}

	public TemplateCreateResult CreateFromTemplate(string templateId, string modRoot, string resourceName, bool overwrite = false)
	{
		return CreateFromTemplate(templateId, modRoot, resourceName, resourceName, overwrite);
	}

	public TemplateCreateResult CreateFromTemplate(string templateId, string modRoot, string resourceName, string displayName, bool overwrite = false)
	{
		if (string.IsNullOrWhiteSpace(modRoot))
		{
			return new TemplateCreateResult
			{
				TemplateId = templateId,
				Success = false,
				Error = "Mod 工程目录为空。"
			};
		}
		TemplateInfo templateInfo = FindTemplate(templateId);
		if (templateInfo == null)
		{
			return new TemplateCreateResult
			{
				TemplateId = templateId,
				Success = false,
				Error = "找不到指定模板。"
			};
		}
		return CreateFromTemplateAtFolder(templateInfo, Path.Combine(modRoot, templateInfo.DefaultFolder), resourceName, displayName, overwrite);
	}

	public TemplateCreateResult CreateFromTemplateInDirectory(string templateId, string directoryPath, string resourceName, bool overwrite = false)
	{
		return CreateFromTemplateInDirectory(templateId, directoryPath, resourceName, resourceName, overwrite);
	}

	public TemplateCreateResult CreateFromTemplateInDirectory(string templateId, string directoryPath, string resourceName, string displayName, bool overwrite = false)
	{
		TemplateInfo templateInfo = FindTemplate(templateId);
		if (templateInfo == null)
		{
			return new TemplateCreateResult
			{
				TemplateId = templateId,
				Success = false,
				Error = "找不到指定模板。"
			};
		}
		if (string.IsNullOrWhiteSpace(directoryPath))
		{
			return new TemplateCreateResult
			{
				TemplateId = templateId,
				Success = false,
				Error = "目标目录为空。"
			};
		}
		return CreateFromTemplateAtFolder(templateInfo, directoryPath, resourceName, displayName, overwrite);
	}

	public TemplateInfo FindTemplate(string templateId)
	{
		return _templates.Find((TemplateInfo t) => t.Id == templateId);
	}

	private static TemplateCreateResult CreateFromTemplateAtFolder(TemplateInfo template, string folder, string resourceName, string displayName, bool overwrite)
	{
		string text = (string.IsNullOrWhiteSpace(resourceName) ? XWTemplatePresentation.Resolve(template).SuggestedName : resourceName.Trim());
		string displayName2 = (string.IsNullOrWhiteSpace(displayName) ? text : displayName.Trim());
		string text2 = SanitizeName(text);
		if (template.Id == "blueprint")
		{
			return CreateBlueprintTemplate(template, folder, text2, displayName2, overwrite);
		}
		if (template.Id.StartsWith("character-scene-", StringComparison.OrdinalIgnoreCase))
		{
			return XWResourceCreateRoute.CreateCharacterScenePackageFromTemplate(template.Id, folder, text2, displayName2, overwrite);
		}
		if (template.Id == "character-component")
		{
			return CreateCharacterComponentPackage(template, folder, text2, displayName2, overwrite);
		}
		if (template.Id == "level-resource")
		{
			return CreateLevelResourceTemplate(template, folder, text2, displayName2, overwrite);
		}
		Directory.CreateDirectory(folder);
		string text3 = Path.Combine(folder, text2 + template.FileExtension);
		if (File.Exists(text3) && !overwrite)
		{
			return new TemplateCreateResult
			{
				TemplateId = template.Id,
				CreatedPath = text3,
				Success = false,
				Error = "目标文件已存在。"
			};
		}
		string contents = BuildContent(template, text2, displayName2);
		File.WriteAllText(text3, contents, Encoding.UTF8);
		return new TemplateCreateResult
		{
			TemplateId = template.Id,
			CreatedPath = text3,
			CreatedPaths = new List<string> { text3 },
			Success = true
		};
	}

	private static TemplateCreateResult CreateBlueprintTemplate(TemplateInfo template, string folder, string safeName, string displayName, bool overwrite)
	{
		XWBlueprintCreationService.Result result = XWBlueprintCreationService.Create(NormalizePath(Path.Combine(folder, safeName + ".tres")), "Node", string.IsNullOrWhiteSpace(displayName) ? safeName : displayName, overwrite);
		return new TemplateCreateResult
		{
			TemplateId = template.Id,
			CreatedPath = result.CreatedPath,
			CreatedPaths = (result.Success ? new List<string> { result.CreatedPath } : new List<string>()),
			Success = result.Success,
			Error = result.Error
		};
	}

	private static TemplateCreateResult CreateLevelResourceTemplate(TemplateInfo template, string folder, string safeName, string displayName, bool overwrite)
	{
		Directory.CreateDirectory(folder);
		string text = NormalizePath(Path.Combine(folder, safeName + ".tres"));
		if (File.Exists(text) && !overwrite)
		{
			return new TemplateCreateResult
			{
				TemplateId = template.Id,
				CreatedPath = text,
				Success = false,
				Error = "目标文件已存在。"
			};
		}
		Error error = ResourceSaver.Save(XWNewLevelResourceDefaults.Create(safeName, displayName), text, ResourceSaver.SaverFlags.None);
		return new TemplateCreateResult
		{
			TemplateId = template.Id,
			CreatedPath = text,
			CreatedPaths = ((error == Error.Ok) ? new List<string> { text } : new List<string>()),
			Success = (error == Error.Ok),
			Error = ((error == Error.Ok) ? "" : $"保存关卡资源失败：{error}")
		};
	}

	private static TemplateCreateResult CreateCharacterComponentPackage(TemplateInfo template, string folder, string safeName, string displayName, bool overwrite)
	{
		if (string.IsNullOrWhiteSpace(folder))
		{
			return new TemplateCreateResult
			{
				TemplateId = template.Id,
				Success = false,
				Error = "目标目录为空。"
			};
		}
		string text = NormalizeCharacterComponentClassName(safeName);
		string text2 = text + "Definition";
		string text3 = Path.Combine(folder, text);
		string item = NormalizePath(Path.Combine(text3, text2 + ".cs"));
		string item2 = NormalizePath(Path.Combine(text3, text + ".cs"));
		string text4 = NormalizePath(Path.Combine(text3, text2 + ".tres"));
		if (Directory.Exists(text3) && !overwrite)
		{
			return new TemplateCreateResult
			{
				TemplateId = template.Id,
				CreatedPath = text4,
				Success = false,
				Error = "目标组件目录已存在。"
			};
		}
		string definitionId = Guid.NewGuid().ToString("N");
		string instanceId = Guid.NewGuid().ToString("N");
		string displayName2 = (string.IsNullOrWhiteSpace(displayName) ? text : displayName);
		string text5 = text3 + ".creating-" + Guid.NewGuid().ToString("N");
		string text6 = text3 + ".backup-" + Guid.NewGuid().ToString("N");
		try
		{
			Directory.CreateDirectory(text5);
			File.WriteAllText(Path.Combine(text5, text2 + ".cs"), BuildCharacterComponentDefinitionContent(text2, text), Encoding.UTF8);
			File.WriteAllText(Path.Combine(text5, text + ".cs"), BuildCharacterComponentRuntimeContent(text), Encoding.UTF8);
			File.WriteAllText(Path.Combine(text5, text2 + ".tres"), BuildCharacterComponentResourceContent(text2, text, displayName2, definitionId, instanceId), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			try
			{
				if (Directory.Exists(text5))
				{
					Directory.Delete(text5, recursive: true);
				}
			}
			catch
			{
			}
			return new TemplateCreateResult
			{
				TemplateId = template.Id,
				CreatedPath = text4,
				Success = false,
				Error = "准备组件包失败：" + ex.Message
			};
		}
		bool flag = false;
		try
		{
			if (Directory.Exists(text3))
			{
				Directory.Move(text3, text6);
				flag = true;
			}
			Directory.Move(text5, text3);
		}
		catch (Exception ex2)
		{
			try
			{
				if (Directory.Exists(text5))
				{
					Directory.Delete(text5, recursive: true);
				}
			}
			catch
			{
			}
			string text7 = "";
			if (flag && Directory.Exists(text6) && !Directory.Exists(text3))
			{
				try
				{
					Directory.Move(text6, text3);
				}
				catch (Exception ex3)
				{
					text7 = "；备份仍保留在“" + NormalizePath(text6) + "”：" + ex3.Message;
				}
			}
			else if (flag && Directory.Exists(text6))
			{
				text7 = "；备份仍保留在“" + NormalizePath(text6) + "”。";
			}
			return new TemplateCreateResult
			{
				TemplateId = template.Id,
				CreatedPath = text4,
				Success = false,
				Error = "无法以原子方式安装组件包：" + ex2.Message + text7
			};
		}
		string warning = "";
		if (flag && Directory.Exists(text6))
		{
			try
			{
				Directory.Delete(text6, recursive: true);
			}
			catch (Exception ex4)
			{
				warning = "新组件包已安装；旧备份仍保留在“" + NormalizePath(text6) + "”：" + ex4.Message;
			}
		}
		return new TemplateCreateResult
		{
			TemplateId = template.Id,
			CreatedPath = text4,
			CreatedPaths = new List<string> { text4, item, item2 },
			Success = true,
			Warning = warning
		};
	}

	private static string BuildCharacterComponentDefinitionContent(string definitionClassName, string runtimeClassName)
	{
		return $"using Godot;\n\n[GlobalClass]\n[Tool]\npublic partial class {definitionClassName} : CharacterComponentDefinition\n{{\n    public override CharacterComponentRuntime CreateRuntime() => new {runtimeClassName}();\n}}\n";
	}

	private static string BuildCharacterComponentRuntimeContent(string runtimeClassName)
	{
		return $"public sealed class {runtimeClassName} : CharacterComponentRuntime\n{{\n    // 示例：int damage = GetConfiguration<int>(\"Damage\", 25);\n    protected override void OnBound()\n    {{\n    }}\n\n    protected override void OnActivated()\n    {{\n    }}\n\n    protected override void OnDetaching(ComponentDetachReason reason)\n    {{\n    }}\n\n    protected override void OnReleased()\n    {{\n    }}\n}}\n";
	}

	private static string BuildCharacterComponentResourceContent(string definitionClassName, string runtimeClassName, string displayName, string definitionId, string instanceId)
	{
		global::_003C_003Ey__InlineArray5<object> buffer = default(global::_003C_003Ey__InlineArray5<object>);
		buffer[0] = EscapeGodotString(definitionClassName);
		buffer[1] = EscapeGodotString(displayName);
		buffer[2] = EscapeGodotString(runtimeClassName);
		buffer[3] = EscapeGodotString(definitionId);
		buffer[4] = EscapeGodotString(instanceId);
		return string.Format("[gd_resource type=\"Resource\" script_class=\"ModCharacterComponentDefinition\" load_steps=2 format=3]\n\n[ext_resource type=\"Script\" path=\"res://Script/Component/Runtime/ModCharacterComponentDefinition.cs\" id=\"1_definition\"]\n\n[resource]\nscript = ExtResource(\"1_definition\")\nresource_name = \"{1}\"\nComponentTypeId = \"{2}\"\nDefinitionId = \"{3}\"\nInstanceId = \"{4}\"\nWireIndex = -1\nSchemaVersion = 1\nInitiallyAlive = true\nDefinitionTypeName = \"{0}\"\nRuntimeTypeName = \"{2}\"\nConfiguration = {{}}\n", (ReadOnlySpan<object?>)buffer);
	}

	private static string NormalizeCharacterComponentClassName(string name)
	{
		string text = SanitizeCSharpIdentifier(name);
		if (text.EndsWith("ComponentDefinition", StringComparison.Ordinal))
		{
			text = text.Substring(0, text.Length - "Definition".Length);
		}
		else if (text.EndsWith("Definition", StringComparison.Ordinal))
		{
			text = text.Substring(0, text.Length - "Definition".Length);
		}
		if (!text.EndsWith("Component", StringComparison.Ordinal))
		{
			text += "Component";
		}
		return text;
	}

	private static string BuildContent(TemplateInfo template, string safeName, string displayName)
	{
		string newValue = ((template.FileExtension == ".cs") ? SanitizeCSharpIdentifier(safeName) : safeName);
		return template.Content.Replace("${ClassName}", SanitizeCSharpIdentifier(safeName)).Replace("${Name}", newValue).Replace("${DisplayName}", displayName);
	}

	public static string SanitizeName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return "NewTemplate";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in name)
		{
			stringBuilder.Append((char.IsLetterOrDigit(c) || c == '_') ? c : '_');
		}
		if (stringBuilder.Length != 0)
		{
			return stringBuilder.ToString();
		}
		return "NewTemplate";
	}

	private static string SanitizeCSharpIdentifier(string name)
	{
		string text = SanitizeName(name);
		if (!char.IsLetter(text[0]) && text[0] != '_')
		{
			return "S" + text;
		}
		return text;
	}

	private static string EscapeGodotString(string value)
	{
		return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
	}

	private static string NormalizePath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Replace('\\', '/');
		}
		return "";
	}

	private static TemplateInfo CreateGodotResourceTemplate(string id, string category, string displayName, string defaultFolder, string scriptClass, string scriptUid, string scriptPath, string previewText, string resourceBody, string extraResources = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("[gd_resource type=\"Resource\" script_class=\"").Append(scriptClass).Append("\" format=3]\n\n");
		stringBuilder.Append("[ext_resource type=\"Script\"");
		if (!string.IsNullOrWhiteSpace(scriptUid))
		{
			stringBuilder.Append(" uid=\"").Append(scriptUid).Append('"');
		}
		stringBuilder.Append(" path=\"res://").Append(scriptPath).Append("\" id=\"1_script\"]\n");
		if (!string.IsNullOrWhiteSpace(extraResources))
		{
			stringBuilder.Append(extraResources.TrimEnd()).Append("\n");
		}
		stringBuilder.Append("\n[resource]\n");
		stringBuilder.Append("script = ExtResource(\"1_script\")\n");
		stringBuilder.Append("resource_name = \"${DisplayName}\"\n");
		stringBuilder.Append(resourceBody.TrimStart());
		if (!resourceBody.EndsWith("\n"))
		{
			stringBuilder.Append('\n');
		}
		if (!string.IsNullOrWhiteSpace(scriptUid))
		{
			stringBuilder.Append("metadata/_custom_type_script = \"").Append(scriptUid).Append("\"\n");
		}
		return new TemplateInfo
		{
			Id = id,
			Category = category,
			DisplayName = displayName,
			DefaultFolder = defaultFolder,
			FileExtension = ".tres",
			PreviewText = previewText,
			Content = stringBuilder.ToString()
		};
	}

	private static TemplateInfo CreateSimpleResourceTemplate(string id, string category, string displayName, string defaultFolder, string previewText)
	{
		return new TemplateInfo
		{
			Id = id,
			Category = category,
			DisplayName = displayName,
			DefaultFolder = defaultFolder,
			FileExtension = ".tres",
			PreviewText = previewText,
			Content = "[gd_resource type=\"Resource\" format=3]\n\n[resource]\nresource_name = \"${DisplayName}\"\nmetadata/mod_display_name = \"${DisplayName}\"\n"
		};
	}

	private static TemplateInfo CreateCharacterSceneTemplate(string id, string displayName, string defaultFolder, string category)
	{
		return new TemplateInfo
		{
			Id = id,
			Category = "Character",
			DisplayName = displayName,
			DefaultFolder = defaultFolder,
			FileExtension = ".tscn",
			PreviewText = "创建可运行角色场景，并配套生成配置、游戏视觉、受伤点、外观、护甲与 C# 脚本",
			Content = "[gd_scene format=3]\n\n[node name=\"${Name}\" type=\"Node2D\"]\nmetadata/mod_resource_kind = \"Character\"\nmetadata/mod_character_category = \"" + category + "\"\nmetadata/mod_character_config_path = \"Config/${Name}Config.tres\"\nmetadata/mod_character_sprite_folder = \"Sprite\"\nmetadata/mod_character_damage_point_folder = \"DamagePoint\"\nmetadata/mod_character_custom_folder = \"Custom\"\nmetadata/mod_character_armor_folder = \"Armor\"\nmetadata/mod_character_script_folder = \"Script\"\n"
		};
	}

	private void RegisterDefaults()
	{
		Register(CreateGodotResourceTemplate("level-packet-entry-resource", "PacketSpawnEntry", "Level Packet Entry", "Resources/PacketSpawnEntries/Level", "TowerDefenseLevelPacketConfig", "uid://crdubirc3xofm", "Resource/TowerDefense/Level/Packet/TowerDefenseLevelPacketConfig.cs", "TowerDefenseLevelPacketConfig", "packetName = \"${Name}\"\n"));
		Register(CreateGodotResourceTemplate("conveyor-packet-entry-resource", "PacketSpawnEntry", "Conveyor Packet Entry", "Resources/PacketSpawnEntries/Conveyor", "TowerDefenseConveyorPacketConfig", "uid://brksssc7xyo1f", "Registry/Battle/Feature/ConveyorBelt/Resource/TowerDefenseConveyorPacketConfig.cs", "TowerDefenseConveyorPacketConfig", "name = \"${Name}\"\nweight = 10\nmaxNum = -1\nmaxMagnification = 0.1\nminNum = -1\nminMagnification = 2.0\n"));
		Register(CreateGodotResourceTemplate("rain-packet-entry-resource", "PacketSpawnEntry", "Rain Packet Entry", "Resources/PacketSpawnEntries/Rain", "TowerDefenseRainModePacketConfig", "uid://b1gouveejjoh2", "Registry/Battle/Feature/RainMode/Resource/TowerDefenseRainModePacketConfig.cs", "TowerDefenseRainModePacketConfig", "name = \"${Name}\"\nweight = 10\nmaxNum = -1\nmaxMagnification = 0.1\nminNum = -1\nminMagnification = 2.0\n"));
		Register(CreateGodotResourceTemplate("conveyor-add-packet-event-resource", "ConveyorEvent", "Conveyor Add Packet Event", "Resources/ConveyorEvents", "TowerDefenseConveyorEventAddPacket", "uid://pqpm8g7fa62f", "Registry/Battle/Feature/ConveyorBelt/Resource/Event/Config/TowerDefenseConveyorEventAddPacket.cs", "TowerDefenseConveyorEventAddPacket", "packet = SubResource(\"Packet_Default\")\n", "[ext_resource type=\"Script\" uid=\"uid://brksssc7xyo1f\" path=\"res://Registry/Battle/Feature/ConveyorBelt/Resource/TowerDefenseConveyorPacketConfig.cs\" id=\"2_packet\"]\n\n[sub_resource type=\"Resource\" id=\"Packet_Default\"]\nscript = ExtResource(\"2_packet\")\nname = \"NewPacket\"\nweight = 10\nmaxNum = -1\nmaxMagnification = 0.1\nminNum = -1\nminMagnification = 2.0"));
		Register(CreateGodotResourceTemplate("level-catalog-resource", "LevelCatalog", "Level Select Catalog", "Resources/LevelCatalogs", "LevelCatalogConfig", "", "Resource/Level/LevelCatalogConfig.cs", "LevelCatalogConfig", "catalogKey = \"${Name}\"\nchapterList = Array[ExtResource(\"2_chapter\")]([SubResource(\"Chapter_Default\")])\n", "[ext_resource type=\"Script\" uid=\"uid://bs4qjs6nwaty5\" path=\"res://Resource/Level/LevelChapterConfig.cs\" id=\"2_chapter\"]\n[ext_resource type=\"Script\" uid=\"uid://b0ewa0be6anbr\" path=\"res://Resource/Level/LevelChooseConfig.cs\" id=\"3_level\"]\n\n[sub_resource type=\"Resource\" id=\"Level_Default\"]\nscript = ExtResource(\"3_level\")\nsaveKey = \"Level1_1\"\n\n[sub_resource type=\"Resource\" id=\"Chapter_Default\"]\nscript = ExtResource(\"2_chapter\")\nchapterName = \"新章节\"\nlevelList = Array[ExtResource(\"3_level\")]([SubResource(\"Level_Default\")])"));
		Register(new TemplateInfo
		{
			Id = "feature-csharp",
			Category = "Feature",
			DisplayName = "关卡功能 C#",
			DefaultFolder = "Battle/Features",
			Description = "创建可参与关卡生命周期的功能脚本。",
			PreviewText = "关卡功能脚本（TowerDefenseBattleFeature）",
			Content = "using Godot;\n\n// ${DisplayName}\n[GlobalClass]\npublic partial class ${Name} : TowerDefenseBattleFeature\n{\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "process-csharp",
			Category = "Process",
			DisplayName = "关卡流程 C#",
			DefaultFolder = "Battle/Processes",
			Description = "创建负责胜负判断和关卡推进的流程脚本。",
			PreviewText = "关卡流程脚本（TowerDefenseBattleProcess）",
			Content = "using Godot;\n\n// ${DisplayName}\n[GlobalClass]\npublic partial class ${Name} : TowerDefenseBattleProcess\n{\n    public override bool CheckFinal() => false;\n\n    public override bool CheckFail() => false;\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "character-component",
			Category = "Component",
			DisplayName = "角色组件",
			DefaultFolder = "Resources/CharacterComponents",
			Description = "创建包含定义脚本、运行脚本和可视配置资源的现代角色组件。",
			PreviewText = "角色组件定义（CharacterComponentDefinition）",
			Content = "using Godot;\n\n[GlobalClass]\n[Tool]\npublic partial class ${Name}Definition : CharacterComponentDefinition\n{\n    public override CharacterComponentRuntime CreateRuntime() => new ${Name}();\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "character-csharp",
			Category = "Character",
			DisplayName = "通用角色 C#",
			DefaultFolder = "Scripts",
			Description = "创建通用角色脚本；角色包内创建时会自动使用对应角色类型。",
			PreviewText = "通用角色脚本（TowerDefenseCharacter）",
			Content = "using Godot;\n\n// ${DisplayName}\n[Tool]\n[GlobalClass]\npublic partial class ${Name} : TowerDefenseCharacter\n{\n    public override void _Ready()\n    {\n        base._Ready();\n    }\n}\n"
		});
		Register(CreateGodotResourceTemplate("character-component-resource", "Component", "Character Component Set", "Resources/CharacterComponents", "CharacterComponentSet", "uid://bk4n3xmg4ke0v", "Script/Component/Runtime/CharacterComponentSet.cs", "CharacterComponentSet", "BehaviorIds = Array[StringName]([])\nComponents = Array[CharacterComponentDefinition]([])\nRemovedInstanceIds = Array[String]([])\n"));
		Register(CreateGodotResourceTemplate("character-attack-resource", "CharacterCombat", "Character Attack", "Resources/CharacterCombat/Attacks", "AttackConfig", "uid://cr5nwpwl6677g", "Resource/TowerDefense/Attack/AttackConfig.cs", "AttackConfig", "num = 20.0\nattackScale = 1.0\narmorAttackScale = 1.0\ndamageFlags = 3\ncollisionFlags = 9\n"));
		Register(CreateGodotResourceTemplate("character-buff-frozen-resource", "CharacterCombat", "Frozen Buff", "Resources/CharacterCombat/Buffs", "TowerDefenseCharacterBuffFrozen", "uid://cw2vrb8rkj87m", "Resource/TowerDefense/Character/Buff/TowerDefenseCharacterBuffFrozen.cs", "TowerDefenseCharacterBuffFrozen", "key = \"Frozen\"\nrefresh = true\ncanFliter = true\ntime = 8.0\niceSpeedDownTime = 15.0\ncurrentTime = 0.0\n"));
		Register(CreateGodotResourceTemplate("character-buff-burn-resource", "CharacterCombat", "Burn Buff", "Resources/CharacterCombat/Buffs", "TowerDefenseCharacterBuffBurn", "uid://cspli0aenqr4g", "Resource/TowerDefense/Character/Buff/TowerDefenseCharacterBuffBurn.cs", "TowerDefenseCharacterBuffBurn", "key = \"Burn\"\nrefresh = true\ncanFliter = true\ntime = 3.0\ndpsAttack = 100.0\nsplatSceneType = \"Particles\"\nsplatInterval = 1.0\n"));
		Register(CreateGodotResourceTemplate("character-buff-poison-resource", "CharacterCombat", "Poisoning Buff", "Resources/CharacterCombat/Buffs", "TowerDefenseCharacterBuffPoisoning", "uid://d1fie66tskvhk", "Resource/TowerDefense/Character/Buff/TowerDefenseCharacterBuffPoisoning.cs", "TowerDefenseCharacterBuffPoisoning", "key = \"Poisoning\"\nrefresh = true\ncanFliter = true\ntime = 15.0\ncurrentTime = 0.0\ntimer = 0.0\n"));
		Register(CreateGodotResourceTemplate("character-buff-hypnoses-resource", "CharacterCombat", "Hypnoses Buff", "Resources/CharacterCombat/Buffs", "TowerDefenseCharacterBuffHypnoses", "uid://gn58p3e7knxr", "Resource/TowerDefense/Character/Buff/TowerDefenseCharacterBuffHypnoses.cs", "TowerDefenseCharacterBuffHypnoses", "key = \"Hypnoses\"\nrefresh = true\ncanFliter = true\ntime = -1.0\n"));
		Register(CreateGodotResourceTemplate("character-event-hurt-resource", "CharacterCombat", "直接伤害事件", "Resources/CharacterCombat/Events", "TowerDefenseCharacterEventHurt", "uid://qhanqul6rou", "Resource/TowerDefense/Character/Event/General/TowerDefenseCharacterEventHurt.cs", "直接造成固定数值伤害", "num = 20.0\nplaySplatAudio = true\ndamageFlags = 3\ncollisionFlags = 9\n"));
		Register(CreateGodotResourceTemplate("character-event-config-hurt-resource", "CharacterCombat", "配置伤害事件", "Resources/CharacterCombat/Events", "TowerDefenseCharacterEventHurtWithConfig", "uid://eughsv2mxyyl", "Resource/TowerDefense/Character/Event/General/TowerDefenseCharacterEventHurtWithConfig.cs", "使用攻击配置计算伤害", "playSplatAudio = true\n"));
		Register(CreateGodotResourceTemplate("character-event-add-buff-resource", "CharacterCombat", "添加状态事件", "Resources/CharacterCombat/Events", "TowerDefenseCharacterEventAddBuff", "uid://dhf6ayv71ru1h", "Resource/TowerDefense/Character/Event/General/TowerDefenseCharacterEventAddBuff.cs", "向目标添加一个或多个状态", ""));
		Register(CreateGodotResourceTemplate("character-event-random-resource", "CharacterCombat", "随机条件事件", "Resources/CharacterCombat/Events", "TowerDefenseCharacterEventConditionRandom", "uid://c55feeuvm3ss", "Resource/TowerDefense/Character/Event/Condition/TowerDefenseCharacterEventConditionRandom.cs", "按概率执行嵌套事件", "percentage = 0.3\n"));
		Register(CreateGodotResourceTemplate("character-event-lucky-draw-resource", "CharacterCombat", "权重事件池", "Resources/CharacterCombat/Events", "TowerDefenseCharacterEventLuckyDraw", "uid://c6ovvn8q5kckw", "Resource/TowerDefense/Character/Event/General/TowerDefenseCharacterEventLuckyDraw.cs", "按权重抽取一个嵌套事件", ""));
		Register(CreateGodotResourceTemplate("character-armor-data-resource", "CharacterData", "Character Armor Data", "Resources/CharacterData/Armor", "CharacterArmorData", "uid://d4b0h447ngm28", "Resource/General/Character/Armor/CharacterArmorData.cs", "CharacterArmorData", ""));
		Register(CreateGodotResourceTemplate("character-armor-slot-resource", "CharacterData", "Armor Slot", "Resources/CharacterData/Armor", "ArmorSlotConfig", "uid://cjtgbt8yicxa1", "Resource/General/Character/Armor/ArmorSlotConfig.cs", "ArmorSlotConfig", "armorName = \"${Name}\"\nreplaceMethod = \"Media\"\noffset = Vector2(0, 0)\nrotation = 0.0\nscale = Vector2(1, 1)\ndamagePoint = -1.0\n"));
		Register(CreateGodotResourceTemplate("character-armor-type-resource", "CharacterData", "Armor Type", "Resources/CharacterData/Armor", "TowerDefenseArmorTypeData", "uid://c0oua7o8eid3u", "Registry/Armor/Data/TowerDefenseArmorTypeData.cs", "TowerDefenseArmorTypeData", "armorName = \"${Name}\"\ndamagePoint = 370.0\nheight = 2\nlimitMaxHit = -1.0\nexplodePersontage = 1.0\narmorMethodFlags = 200\n"));
		Register(CreateGodotResourceTemplate("character-custom-data-resource", "CharacterData", "Character Custom Data", "Resources/CharacterData/Custom", "CharacterCustomData", "uid://dthsm21ucumm4", "Resource/General/Character/Costom/CharacterCustomData.cs", "CharacterCustomData", ""));
		Register(CreateGodotResourceTemplate("character-custom-config-resource", "CharacterData", "Character Custom Config", "Resources/CharacterData/Custom", "CharacterCustomConfig", "uid://dntp5rhp7s2ot", "Resource/General/Character/Costom/CharacterCustomConfig.cs", "CharacterCustomConfig", "openKey = \"${Name}\"\ncustomName = \"${DisplayName}\"\ntype = \"White\"\n"));
		Register(CreateGodotResourceTemplate("character-damage-point-data-resource", "CharacterData", "Character Damage Point Data", "Resources/CharacterData/DamagePoints", "CharacterDamagePointData", "uid://glu4fym1t3ns", "Resource/General/Character/DamagePoint/CharacterDamagePointData.cs", "CharacterDamagePointData", ""));
		Register(CreateGodotResourceTemplate("character-damage-point-config-resource", "CharacterData", "Character Damage Point", "Resources/CharacterData/DamagePoints", "CharacterDamagePointConfig", "uid://dw32w1qy2lp10", "Resource/General/Character/DamagePoint/CharacterDamagePointConfig.cs", "CharacterDamagePointConfig", "damagePointName = \"${DisplayName}\"\ndamagePersontage = 0.5\nanimeEffectOffset = Vector2(0, 0)\nisDrop = true\ndamageAudio = \"LimbsPop\"\n"));
		Register(CreateGodotResourceTemplate("buff-visual-resource", "BuffVisual", "BUFF Visual Definition", "Resources/BuffVisuals", "BuffVisualDefinition", "uid://dsm2hj3x7r8kq", "Script/Component/TowerDefense/Character/BuffComponent/BuffVisualDefinition.cs", "BuffVisualDefinition", "buffKey = &\"${Name}\"\nenabled = true\nnodeName = &\"${Name}Visual\"\nposition = Vector2(0, 0)\nscale = Vector2(1, 1)\nrotation = 0.0\nzIndex = 0\nzAsRelative = true\ncentered = true\noffset = Vector2(0, 0)\ndrawBand = 1\n"));
		Register(CreateGodotResourceTemplate("character-hitbox-resource", "CollisionGeometry", "Character Hit Box", "Resources/CollisionGeometry", "CharacterHitBoxDefinition", "uid://bisf4laptfb5u", "Resource/TowerDefense/Collision/CharacterHitBoxDefinition.cs", "CharacterHitBoxDefinition", "Size = Vector2(80, 80)\nLocalTransform = Transform2D(1, 0, 0, 1, 0, 0)\nDefaultEnabled = true\nDefaultMonitorable = true\nDebugDraw = true\nDebugFillColor = Color(0.1, 0.75, 1, 0.18)\nDebugOutlineColor = Color(0.1, 0.75, 1, 0.85)\nDebugLineWidth = 2.0\n"));
		Register(CreateGodotResourceTemplate("aabb-shape-resource", "CollisionGeometry", "AABB Area Shape", "Resources/CollisionGeometry", "AabbShape2DResource", "uid://n4b4cnyw2vms", "Resource/TowerDefense/Collision/AabbShape2DResource.cs", "AabbShape2DResource", "Enabled = true\nGeometry = SubResource(\"RectangleShape_Default\")\nLocalTransform = Transform2D(1, 0, 0, 1, 0, 0)\nDebugDraw = true\nDebugFillColor = Color(0.1, 0.75, 1, 0.18)\nDebugOutlineColor = Color(0.1, 0.75, 1, 0.85)\nDebugLineWidth = 2.0\n", "[sub_resource type=\"RectangleShape2D\" id=\"RectangleShape_Default\"]\nsize = Vector2(80, 80)\n"));
		Register(CreateGodotResourceTemplate("aabb-ray-resource", "CollisionGeometry", "AABB Check Ray", "Resources/CollisionGeometry", "AabbRay2DResource", "uid://bbglc127qw1jb", "Resource/TowerDefense/Collision/AabbRay2DResource.cs", "AabbRay2DResource", "Enabled = true\nLocalTransform = Transform2D(1, 0, 0, 1, 0, 0)\nTargetPosition = Vector2(2000, 0)\nDebugDraw = true\nDebugColor = Color(0.2, 1, 0.25, 0.8)\nDebugLineWidth = 2.0\nDebugEndpointRadius = 5.0\nDebugArrowSize = 14.0\n"));
		Register(CreateGodotResourceTemplate("award-settlement-resource", "Collectable", "Award Settlement Visual", "Resources/AwardSettlements", "AwardSettlementConfig", "uid://bgogdbkv72wbs", "Resource/AwardNote/AwardSettlementConfig.cs", "AwardSettlementConfig", "texture = null\nbackground = null\nimage = null\n"));
		Register(CreateGodotResourceTemplate("state-machine-resource", "StateMachine", "State Machine Definition", "Resources/StateMachines", "StateMachineDefinition", "uid://n4ro0a2kenv5", "addons/godot_state_charts/ResourceRuntime/StateMachineDefinition.cs", "StateMachineDefinition", "DefinitionId = \"${Name}\"\nRootStateId = \"${Name}.root\"\nStates = Array[ExtResource(\"2_state\")]([SubResource(\"State_root\")])\nTransitions = Array[Resource]([])\nAliases = {}\n", "[ext_resource type=\"Script\" uid=\"uid://bbeiiwsi6lxih\" path=\"res://addons/godot_state_charts/ResourceRuntime/StateMachineStateDefinition.cs\" id=\"2_state\"]\n\n[sub_resource type=\"Resource\" id=\"State_root\"]\nscript = ExtResource(\"2_state\")\nStableId = \"${Name}.root\"\nDisplayName = &\"${DisplayName}\"\n"));
		Register(CreateGodotResourceTemplate("state-property-guard-resource", "StateGuard", "资源状态机属性比较条件", "Resources/StateMachines/Conditions", "StateMachineGuardDefinition", "uid://dsmguarddefinition1", "addons/godot_state_charts/ResourceRuntime/StateMachineGuardDefinition.cs", "比较资源状态机运行属性与预期值", "Kind = 0\nComparedProperty = &\"state\"\nOperator = 0\nExpectedValue = \"ready\"\nNegate = false\n"));
		Register(CreateGodotResourceTemplate("expression-guard-resource", "StateGuard", "场景状态图表达式条件", "Resources/StateMachines/Conditions", "ExpressionGuard", "uid://7lkyj1qas8su", "addons/godot_state_charts/ExpressionGuard.cs", "使用场景状态图属性表达式判断是否允许切换", "expression = \"health <= 0\"\n"));
		Register(CreateGodotResourceTemplate("state-active-guard-resource", "StateGuard", "场景状态图状态激活条件", "Resources/StateMachines/Conditions", "StateIsActiveGuard", "uid://dyvigbhnrvcer", "addons/godot_state_charts/StateIsActiveGuard.cs", "判断场景状态图中的目标状态是否处于激活状态", "state = NodePath(\"../Playing\")\n"));
		Register(CreateGodotResourceTemplate("all-of-guard-resource", "StateGuard", "场景状态图全部满足组合", "Resources/StateMachines/Conditions", "AllOfGuard", "uid://ckcac8ucjat0x", "addons/godot_state_charts/AllOfGuard.cs", "所有子条件满足时才允许状态切换", ""));
		Register(CreateGodotResourceTemplate("any-of-guard-resource", "StateGuard", "场景状态图任一满足组合", "Resources/StateMachines/Conditions", "AnyOfGuard", "uid://do37lq4qvwikn", "addons/godot_state_charts/AnyOfGuard.cs", "任一子条件满足时允许状态切换", ""));
		Register(CreateGodotResourceTemplate("not-guard-resource", "StateGuard", "场景状态图结果取反组合", "Resources/StateMachines/Conditions", "NotGuard", "uid://dgihwafvjxrul", "addons/godot_state_charts/NotGuard.cs", "反转一个子条件的判断结果", ""));
		Register(CreateGodotResourceTemplate("unlock-condition-resource", "UnlockCondition", "关卡完成解锁条件", "Resources/UnlockConditions", "UnlockConditionLevelFinishConfig", "uid://b310qglq1koln", "Resource/UnlockCondition/UnlockConditionLevelFinishConfig.cs", "完成指定关卡后解锁", "levelSaveKey = \"Unlock\"\n"));
		Register(CreateGodotResourceTemplate("packet-event-resource", "PacketEvent", "Packet Cost Event", "Resources/PacketEvents", "CardActionBehaviorChangeCost", "uid://c14txwwk4kqio", "Registry/Behavior/Card/Action/CardActionBehaviorChangeCost.cs", "CardActionBehaviorChangeCost", "method = 0\nvalue = 0.0\n_min = -1\n_max = -1\n"));
		Register(CreateGodotResourceTemplate("tool-event-resource", "ToolEvent", "小推车阳光事件", "Resources/ToolEvents", "MowerEventCreateSunConfig", "uid://fvfmhub0cdea", "Registry/Battle/Feature/Mower/Resource/Event/MowerEventCreateSunConfig.cs", "小推车触发时产生阳光", "num = 25\n"));
		Register(new TemplateInfo
		{
			Id = "projectile-csharp",
			Category = "Projectile",
			DisplayName = "子弹 C#",
			DefaultFolder = "Scripts",
			Description = "创建可挂载到子弹场景的 C# 脚本。",
			PreviewText = "子弹脚本（TowerDefenseProjectile）",
			Content = "using Godot;\n\n// ${DisplayName}\n[Tool]\n[GlobalClass]\npublic partial class ${Name} : TowerDefenseProjectile\n{\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "shared-csharp",
			Category = "Shared",
			DisplayName = "通用 C# 脚本",
			DefaultFolder = "Scripts",
			Description = "创建可以挂载到通用节点的 C# 脚本。",
			PreviewText = "通用节点脚本（Node）",
			Content = "using Godot;\n\n// ${DisplayName}\npublic partial class ${Name} : Node\n{\n    public override void _Ready()\n    {\n    }\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "debug-entry-csharp",
			Category = "Debug",
			DisplayName = "Mod 调试入口 C#",
			DefaultFolder = "Scripts",
			Description = "创建可被游戏内调试工作台启动的协作式 C# 调试入口。",
			PreviewText = "调试入口支持代码栏断点、暂停、单步、调用栈和变量监视。",
			Content = "using System.Collections.Generic;\nusing System.Threading.Tasks;\nusing PVZHE.ModEditor.ScriptEditor;\n\n// ${DisplayName}\npublic static class ${Name}\n{\n    public static async Task ModEditorDebugEntry(XWScriptDebugSession.DebugContext context)\n    {\n        int exampleHealth = 100;\n        await context.CheckpointAsync(new Dictionary<string, object>\n        {\n            [\"示例生命值\"] = exampleHealth\n        });\n\n        while (!context.CancellationToken.IsCancellationRequested)\n            await context.NextProcessFrameAsync();\n    }\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "character-event-csharp",
			Category = "CharacterEvent",
			DisplayName = "角色事件 C#",
			DefaultFolder = "Scripts",
			Description = "创建可参与角色受击、持续伤害和子弹命中的 C# 事件。",
			PreviewText = "角色事件脚本（TowerDefenseCharacterEventBase）",
			Content = "using Godot;\nusing Godot.Collections;\n\n// ${DisplayName}\n[GlobalClass]\npublic partial class ${Name} : TowerDefenseCharacterEventBase\n{\n    [Export] public string eventName = \"${DisplayName}\";\n\n    public override void Execute(Vector2 pos, TowerDefenseCharacter target)\n    {\n    }\n\n    public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)\n    {\n    }\n\n    public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)\n    {\n    }\n\n    public override void Init(Dictionary valueDictionary)\n    {\n    }\n\n    public override Dictionary Export()\n    {\n        return new Dictionary\n        {\n            [\"EventName\"] = eventName,\n            [\"Value\"] = new Dictionary()\n        };\n    }\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "shovel-event-csharp",
			Category = "ShovelEvent",
			DisplayName = "铲子事件 C#",
			DefaultFolder = "Scripts",
			Description = "创建使用铲子作用于角色时执行的 C# 事件。",
			PreviewText = "铲子事件脚本（ShovelEventConfig）",
			Content = "using Godot;\n\n// ${DisplayName}\n[GlobalClass]\npublic partial class ${Name} : ShovelEventConfig\n{\n    public override void Execute(TowerDefenseCharacter character)\n    {\n    }\n}\n"
		});
		Register(new TemplateInfo
		{
			Id = "card-event-csharp",
			Category = "CardEvent",
			DisplayName = "卡牌事件 C#",
			DefaultFolder = "Scripts",
			Description = "创建作用于游戏内卡牌的 C# 行为。",
			PreviewText = "卡牌动作行为脚本（CardActionBehaviorDefinition）",
			Content = "using Godot;\nusing Godot.Collections;\n\n// ${DisplayName}\n[GlobalClass]\npublic partial class ${Name} : CardActionBehaviorDefinition\n{\n    // actionId 是存档和脚本引用使用的稳定英文标识；eventName 只负责编辑器中的中文展示。\n    [Export] public string actionId = \"${Name}\";\n    [Export] public string eventName = \"${DisplayName}\";\n\n    public override void ImportConfiguration(Dictionary data)\n    {\n        if (data == null)\n            return;\n        actionId = data.GetValueOrDefault(\"ActionId\", actionId).AsString();\n        eventName = data.GetValueOrDefault(\"EventName\", eventName).AsString();\n    }\n\n    public override void ExecuteAction(TowerDefenseInGamePacketShow packet)\n    {\n    }\n\n    public override Dictionary ExportConfiguration()\n    {\n        return new Dictionary\n        {\n            [\"ActionId\"] = actionId,\n            [\"EventName\"] = eventName,\n            [\"Configuration\"] = new Dictionary()\n        };\n    }\n}\n"
		});
		Register(CreateGodotResourceTemplate("level-resource", "Level", "TowerDefenseLevelNewConfig", "Resources/Levels", "TowerDefenseLevelNewConfig", "uid://c88k3cqu0k3jm", "Resource/TowerDefense/Level/TowerDefenseLevelNewConfig.cs", "TowerDefenseLevelNewConfig", "name = \"${Name}\"\nlevelName = \"${DisplayName}\"\ndescription = \"${DisplayName}\"\nlevelNumber = 1\nversion = &\"1.0\"\nprocessName = &\"Wave\"\n"));
		Register(CreateGodotResourceTemplate("map-resource", "Map", "Map Resource", "Resources/Maps", "TowerDefenseMapConfig", "uid://do66qasykqnkv", "Registry/Battle/Feature/Map/Resource/TowerDefenseMapConfig.cs", "TowerDefenseMapConfig", "translate = \"${DisplayName}\"\nmapScene = ExtResource(\"3_frontlawn_scene\")\ngridNum = Vector2i(9, 5)\ngridBeginPos = Vector2(240, 120)\ngridSize = Vector2(80, 96)\nedge = Vector4(200, 0, 1100, 576)\nplantOffset = 50.0\ncellConfig = Array[ExtResource(\"2_cell\")]([SubResource(\"Resource_grid\")])\nlineUse = Array[int]([1, 2, 3, 4, 5])\nuseSunFall = true\n", "[ext_resource type=\"Script\" uid=\"uid://eoxnow8u525l\" path=\"res://Registry/Battle/Feature/Map/Resource/Cell/Config/TowerDefenseCellConfig.cs\" id=\"2_cell\"]\n[ext_resource type=\"PackedScene\" uid=\"uid://dpco4f0jycuf2\" path=\"res://Asset/Config/Map/Frontlawn/Scene/Day/TowerDefenseMapFrontlawn.tscn\" id=\"3_frontlawn_scene\"]\n\n[sub_resource type=\"Resource\" id=\"Resource_grid\"]\nscript = ExtResource(\"2_cell\")\npos = Vector4i(1, 1, 9, 5)\n"));
		Register(CreateGodotResourceTemplate("map-cell-resource", "Map", "Map Cell Brush", "Resources/MapCells", "TowerDefenseCellConfig", "uid://eoxnow8u525l", "Registry/Battle/Feature/Map/Resource/Cell/Config/TowerDefenseCellConfig.cs", "TowerDefenseCellConfig", "pos = Vector4i(1, 1, 1, 1)\ngridType = Array[int]([2, 4])\nElementFlags = 0\n"));
		Register(CreateGodotResourceTemplate("level-wave-manager-resource", "GameplayLogic", "完整波次管理器", "Resources/GameplayLogic/Waves", "TowerDefenseLevelWaveManagerConfig", "uid://ctvkmai1q8sa6", "Resource/TowerDefense/Level/Wave/TowerDefenseLevelWaveManagerConfig.cs", "包含 7 个动态阶段、1 个波次与 1 个普通僵尸生成项", "zombieInvisible = false\nflagZombieUse = true\nflagZombie = \"ZombieFlag\"\nflagWaveInterval = 10\nmaxNextWaveHealthPercentage = 0.15\nminNextWaveHealthPercentage = 0.2\nbeginCol = 20.0\nspawnColEnd = 20.0\nspawnColStart = 5.0\nspawnFrameBudgetMilliseconds = 6.0\nspawnMaxCharactersPerFrame = 8\ndynamic = Array[ExtResource(\"2_dynamic\")]([SubResource(\"Dynamic_0\"), SubResource(\"Dynamic_1\"), SubResource(\"Dynamic_2\"), SubResource(\"Dynamic_3\"), SubResource(\"Dynamic_4\"), SubResource(\"Dynamic_5\"), SubResource(\"Dynamic_6\")])\nwave = Array[ExtResource(\"3_wave\")]([SubResource(\"Wave_Default\")])\nisCustomSurvival = false\nsurvival = \"\"\n", "[ext_resource type=\"Script\" uid=\"uid://br6kawt7m03sc\" path=\"res://Registry/Battle/Feature/Wave/Resource/TowerDefenseLevelDynamicConfig.cs\" id=\"2_dynamic\"]\n[ext_resource type=\"Script\" uid=\"uid://c67uxwpu4jftb\" path=\"res://Resource/TowerDefense/Level/Wave/TowerDefenseLevelWaveConfig.cs\" id=\"3_wave\"]\n[ext_resource type=\"Script\" uid=\"uid://gc0ev0pebyep\" path=\"res://Resource/TowerDefense/Level/Spawn/TowerDefenseLevelSpawnConfig.cs\" id=\"4_spawn\"]\n[ext_resource type=\"Script\" uid=\"uid://cikc5vwojvfpa\" path=\"res://Resource/TowerDefense/Level/Spawn/TowerDefenseLevelSpawnDynamicConfig.cs\" id=\"5_spawn_dynamic\"]\n\n[sub_resource type=\"Resource\" id=\"Dynamic_0\"]\nscript = ExtResource(\"2_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Dynamic_1\"]\nscript = ExtResource(\"2_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Dynamic_2\"]\nscript = ExtResource(\"2_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Dynamic_3\"]\nscript = ExtResource(\"2_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Dynamic_4\"]\nscript = ExtResource(\"2_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Dynamic_5\"]\nscript = ExtResource(\"2_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Dynamic_6\"]\nscript = ExtResource(\"2_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Spawn_Default\"]\nscript = ExtResource(\"4_spawn\")\nzombie = \"ZombieNormal\"\nline = -1\nnum = 1\n\n[sub_resource type=\"Resource\" id=\"SpawnDynamic_Default\"]\nscript = ExtResource(\"5_spawn_dynamic\")\n\n[sub_resource type=\"Resource\" id=\"Wave_Default\"]\nscript = ExtResource(\"3_wave\")\ndynamicPlantfood = Array[int]([0, 0, 0, 0, 0, 0, 0])\nspawn = Array[ExtResource(\"4_spawn\")]([SubResource(\"Spawn_Default\")])\ndynamic = SubResource(\"SpawnDynamic_Default\")\n"));
		Register(CreateGodotResourceTemplate("level-wave-resource", "GameplayLogic", "波次模板", "Resources/GameplayLogic/Waves", "TowerDefenseLevelWaveConfig", "uid://c67uxwpu4jftb", "Resource/TowerDefense/Level/Wave/TowerDefenseLevelWaveConfig.cs", "带一个普通僵尸生成项的波次", "dynamicPlantfood = Array[int]([0, 0, 0, 0, 0, 0, 0])\nspawn = Array[ExtResource(\"2_spawn\")]([SubResource(\"Spawn_Default\")])\ndynamic = SubResource(\"SpawnDynamic_Default\")\n", "[ext_resource type=\"Script\" uid=\"uid://gc0ev0pebyep\" path=\"res://Resource/TowerDefense/Level/Spawn/TowerDefenseLevelSpawnConfig.cs\" id=\"2_spawn\"]\n[ext_resource type=\"Script\" uid=\"uid://cikc5vwojvfpa\" path=\"res://Resource/TowerDefense/Level/Spawn/TowerDefenseLevelSpawnDynamicConfig.cs\" id=\"3_spawn_dynamic\"]\n\n[sub_resource type=\"Resource\" id=\"Spawn_Default\"]\nscript = ExtResource(\"2_spawn\")\nzombie = \"ZombieNormal\"\nline = -1\nnum = 1\n\n[sub_resource type=\"Resource\" id=\"SpawnDynamic_Default\"]\nscript = ExtResource(\"3_spawn_dynamic\")\n"));
		Register(CreateGodotResourceTemplate("level-wave-spawn-resource", "GameplayLogic", "线路生成项", "Resources/GameplayLogic/Waves", "TowerDefenseLevelSpawnConfig", "uid://gc0ev0pebyep", "Resource/TowerDefense/Level/Spawn/TowerDefenseLevelSpawnConfig.cs", "默认生成 1 个普通僵尸", "zombie = \"ZombieNormal\"\nline = -1\nnum = 1\n"));
		Register(CreateGodotResourceTemplate("level-grid-wave-spawn-resource", "GameplayLogic", "格子生成项", "Resources/GameplayLogic/Waves", "TowerDefenseLevelGridSpawnConfig", "uid://d1wvq2gegjubw", "Registry/Battle/Feature/Wave/Resource/TowerDefenseLevelGridSpawnConfig.cs", "在指定格子生成一张卡牌", "packet = \"\"\ngridPos = Vector2i(0, 0)\n"));
		Register(CreateCharacterSceneTemplate("character-scene-plant", "植物角色场景", "Resources/Characters/Plants", "Plant"));
		Register(CreateCharacterSceneTemplate("character-scene-zombie", "僵尸角色场景", "Resources/Characters/Zombies", "Zombie"));
		Register(CreateCharacterSceneTemplate("character-scene-prop", "场景道具角色", "Resources/Characters/Props", "Prop"));
		Register(CreateCharacterSceneTemplate("character-scene-vase", "花瓶角色场景", "Resources/Characters/Vases", "Vase"));
		Register(CreateCharacterSceneTemplate("character-scene-mower", "小推车角色场景", "Resources/Characters/Mowers", "Mower"));
		Register(CreateCharacterSceneTemplate("character-scene-item", "物品角色场景", "Resources/Characters/Items", "Item"));
		Register(CreateCharacterSceneTemplate("character-scene-grave", "墓碑角色场景", "Resources/Characters/Graves", "Grave"));
		Register(CreateCharacterSceneTemplate("character-scene-crater", "弹坑角色场景", "Resources/Characters/Craters", "Crater"));
		Register(CreateGodotResourceTemplate("character-definition", "Character", "Character Definition", "Resources/Characters/Plants", "TowerDefensePlantConfig", "uid://deeto5x21j3q", "Resource/TowerDefense/Character/Config/TowerDefensePlantConfig.cs", "TowerDefensePlantConfig", "name = \"${Name}\"\nhitpoints = 300.0\nhomeWorld = 1\ncost = 100\npacketCooldown = 5.0\ncanImitate = true\ncanCopy = true\ncanUsePlantfood = true\n"));
		Register(CreateGodotResourceTemplate("character-zombie", "Character", "僵尸角色配置", "Resources/Characters/Zombies", "TowerDefenseZombieConfig", "uid://dvtewieu2qn5h", "Resource/TowerDefense/Character/Config/TowerDefenseZombieConfig.cs", "僵尸属性、出场权重和攻击配置", "name = \"${Name}\"\nhitpoints = 270.0\nhomeWorld = 1\nweight = 1000\nwavePointCost = 100\nattack = 100.0\npreview = true\n"));
		Register(CreateGodotResourceTemplate("card-resource", "Card", "Card Resource", "Resources/Cards", "TowerDefensePacketConfig", "uid://ccc5rdb8j2evm", "Registry/Battle/Feature/PacketBank/Resource/Packet/TowerDefensePacketConfig.cs", "TowerDefensePacketConfig", "saveKey = \"${Name}\"\nname = \"${DisplayName}\"\ndescribe = \"${DisplayName}_DESC\"\nhandbookDescribe = \"${DisplayName}_HANDBOOK\"\nhandbookStory = \"${DisplayName}_STORY\"\npacketAnimeClip = \"Idle\"\ncanChangeCost = true\nspawnMethod = \"Rise\"\n"));
		Register(CreateGodotResourceTemplate("adobe-animate-xfl", "Animation", "Adobe Animate 动画", "Resources/Animations", "AdobeAnimateData", "uid://b7srgu1j1lcoj", "addons/AdobeAnimateEditor/Resource/AdobeAnimateData.cs", "空白动画数据，可在视觉编辑器中选择或重新导入 DAT 数据", "frameRate = 30.0\nframeScale = 1\nframeMax = 0\n"));
		Register(CreateGodotResourceTemplate("animation-atlas-profile-resource", "AnimationAtlas", "动画图集配置", "Resources/AnimationAtlasProfiles", "AdobeAnimateAtlasProfile", "uid://bg68yew5l7br3", "addons/AdobeAnimateEditor/Runtime/AdobeAnimateAtlasProfile.cs", "为动画运行时选择图集清单与启动预载策略", "ProfileId = \"${Name}\"\nManifestPath = \"\"\nStartupOnly = false\n"));
		Register(CreateGodotResourceTemplate("packet-bank-resource", "PacketBank", "卡牌库资源", "Resources/PacketBank", "TowerDefensePacketBankData", "uid://dmtl3aa76j22o", "Registry/Battle/Feature/PacketBank/Resource/TowerDefensePacketBankData.cs", "TowerDefensePacketBankData", "category = {}\n"));
		Register(new TemplateInfo
		{
			Id = "dialog-scene",
			Category = "GUI",
			DisplayName = "Empty GUI Scene",
			DefaultFolder = "Resources/Dialogs",
			FileExtension = ".tscn",
			PreviewText = "Control scene with DialogBoxBase script",
			Content = "[gd_scene load_steps=2 format=3]\n\n[ext_resource type=\"Script\" uid=\"uid://dxpykggmmyrhl\" path=\"res://Core/DialogManager/Base/DialogBoxBase.cs\" id=\"1_dialog\"]\n\n[node name=\"${Name}\" type=\"Control\"]\nprocess_mode = 3\nlayout_mode = 3\nanchors_preset = 15\nanchor_right = 1.0\nanchor_bottom = 1.0\ngrow_horizontal = 2\ngrow_vertical = 2\nscript = ExtResource(\"1_dialog\")\nmetadata/mod_resource_kind = \"GUI\"\nmetadata/mod_display_name = \"${DisplayName}\"\n"
		});
		Register(new TemplateInfo
		{
			Id = "menu-dialog-scene",
			Category = "GUI",
			DisplayName = "MenuDialogBase Scene",
			DefaultFolder = "Resources/Dialogs",
			FileExtension = ".tscn",
			PreviewText = "Inherited scene from MenuDialogBase",
			Content = "[gd_scene load_steps=2 format=3]\n\n[ext_resource type=\"PackedScene\" uid=\"uid://fc46lp4e26sk\" path=\"res://Prefab/GUI/DialogBox/MenuDialog/MenuDialogBase.tscn\" id=\"1_menu_dialog\"]\n\n[node name=\"${Name}\" instance=ExtResource(\"1_menu_dialog\")]\nmetadata/mod_resource_kind = \"GUI\"\nmetadata/mod_display_name = \"${DisplayName}\"\n"
		});
		Register(new TemplateInfo
		{
			Id = "dialog-popup-scene",
			Category = "GUI",
			DisplayName = "DialogPopup Scene",
			DefaultFolder = "Resources/Dialogs",
			FileExtension = ".tscn",
			PreviewText = "Inherited scene from DialogPopup",
			Content = "[gd_scene load_steps=2 format=3]\n\n[ext_resource type=\"PackedScene\" uid=\"uid://nx2prtbxim66\" path=\"res://Prefab/GUI/DialogBox/DialogBoxGeneral/DialogPopup.tscn\" id=\"1_dialog_popup\"]\n\n[node name=\"${Name}\" instance=ExtResource(\"1_dialog_popup\")]\nmetadata/mod_resource_kind = \"GUI\"\nmetadata/mod_display_name = \"${DisplayName}\"\n"
		});
		Register(CreateGodotResourceTemplate("projectile", "Projectile", "Projectile", "Resources/Projectiles", "TowerDefenseProjectileData", "uid://lxm4ws12fc3d", "Registry/Projectile/Data/TowerDefenseProjectileData.cs", "TowerDefenseProjectileData", "name = \"${Name}\"\nbaseDamage = 20.0\nsize = Vector2(28, 28)\nscale = Vector2(1, 1)\nsplatAudio = \"SplatNormal\"\nrangeType = \"Default\"\n"));
		Register(CreateGodotResourceTemplate("projectile-change-resource", "ProjectileChange", "Projectile Change", "Resources/ProjectileChanges", "ChangeProjectileConfig", "uid://c8g82tp4s2un5", "Resource/TowerDefense/Projectile/Change/ChangeProjectileConfig.cs", "ChangeProjectileConfig", "changeList = Array[ChangeProjectileSingleConfig]([])\n"));
		Register(CreateGodotResourceTemplate("falling-object-resource", "FallingObject", "Falling Object", "Resources/FallingObjects", "FallingObjectConfig", "uid://dd6glxsd7sfhb", "Resource/FallingObjects/FallingObjectConfig.cs", "FallingObjectConfig", "weightItem = Array[FallingObjectWeightItemConfig]([])\n"));
		Register(CreateGodotResourceTemplate("mower-resource", "Mower", "Mower Config", "Resources/Mowers", "MowerConfig", "uid://c3e8m34j6welx", "Registry/Battle/Feature/Mower/Resource/MowerConfig.cs", "MowerConfig", "saveKey = \"${Name}\"\nname = \"${DisplayName}\"\ndescribe = \"${DisplayName}_DESC\"\nhandbookDescribe = \"${DisplayName}_HANDBOOK\"\nhandbookStory = \"${DisplayName}_STORY\"\nunlockCheckList = Array[UnlockConditionBaseConfig]([])\neventList = Array[MowerEventConfig]([])\n"));
		Register(CreateGodotResourceTemplate("survival-resource", "Survival", "Survival Config", "Resources/Survivals", "TowerDefenseLevelSurvivalConfig", "uid://huks85pqdgqb", "Registry/Battle/Feature/Wave/Resource/TowerDefenseLevelSurvivalConfig.cs", "TowerDefenseLevelSurvivalConfig", "roundLimit = -1\nroundDayNightChange = false\npointIncrementPerWave = 50\npointIncrementPerBigWave = 100\npointIncrementPerRound = 200\npointBegin = 100\npointMax = 10000000\npointBigWaveScale = 1.5\nzombiePoolBase = [\"ZombieNormal\", \"ZombieNormalCone\"]\nzombiePoolRoundAdd = Array[TowerDefenseLevelSurvivalZombiePoolRoundAddConfig]([])\n"));
		Register(CreateGodotResourceTemplate("tutorial-resource", "Tutorial", "教程流程", "Resources/Tutorials", "TutorialConfig", "uid://ciasl1r6mxl1w", "Core/TutorialManager/Resource/TutorialConfig.cs", "由多个可视化步骤组成的教程", "saveKey = \"${Name}\"\nstep = Array[TutorialStepConfig]([])\n"));
		Register(CreateGodotResourceTemplate("tutorial-condition-resource", "Tutorial", "教程角色数量条件", "Resources/Tutorials/Conditions", "TutorialConditionCheckCharaterNum", "uid://dw7rfoxp8o3wl", "Resource/Tutorial/Condition/TutorialConditionCheckCharaterNum.cs", "等待指定角色数量达到条件", "characterName = \"\"\nmethod = \">=\"\nnum = 1\n"));
		Register(CreateGodotResourceTemplate("tutorial-sun-condition-resource", "Tutorial", "教程阳光收集条件", "Resources/Tutorials/Conditions", "TutorialConditionCheckSunCollect", "uid://do5nc4igp00hn", "Resource/Tutorial/Condition/TutorialConditionCheckSunCollect.cs", "等待玩家收集指定数量的阳光", "num = 25\n"));
		Register(CreateGodotResourceTemplate("tutorial-step-resource", "Tutorial", "教程步骤", "Resources/Tutorials/Steps", "TutorialStepConfig", "uid://do4exfdrtmgkl", "Core/TutorialManager/Resource/TutorialStep/TutorialStepConfig.cs", "配置广播文字和完成条件", ""));
		Register(CreateGodotResourceTemplate("npc-talk-resource", "NpcTalk", "NPC Talk", "Resources/NpcTalks", "NpcTalkConfig", "uid://tvlrtu5vey0k", "Resource/Npc/Talk/Base/NpcTalkConfig.cs", "NpcTalkConfig", "talkList = Array[NpcTalkBaseConfig]([])\n"));
		Register(CreateGodotResourceTemplate("shop-resource", "Shop", "Shop Resource", "Resources/Shops", "ShopConfig", "uid://ch3hwpueo71b6", "Resource/Shop/ShopConfig.cs", "ShopConfig", "pageList = Array[ShopPageConfig]([])\n"));
		Register(CreateGodotResourceTemplate("shovel-resource", "Shovel", "Shovel Resource", "Resources/Shovels", "ShovelConfig", "uid://diyfb5x4qqrn2", "Registry/Battle/Feature/Shovel/Resource/ShovelConfig.cs", "ShovelConfig", "name = \"${Name}\"\neventList = Array[ShovelEventConfig]([])\n"));
		Register(CreateGodotResourceTemplate("collectable-resource", "Collectable", "Collectable Resource", "Resources/Collectables", "CollectableConfig", "uid://c5sm301x71jk6", "Resource/TowerDefense/Collectable/CollectableConfig.cs", "CollectableConfig", "saveKey = \"${Name}\"\nconfig = null\n"));
		Register(CreateGodotResourceTemplate("drop-item-resource", "DropItem", "Battle Drop Item", "Resources/DropItems", "DropItemConfig", "uid://dkew3g1o6laol", "Registry/DropItem/DropItemConfig.cs", "DropItemConfig", "Id = 0\nName = &\"${Name}\"\nPoolMaxNum = 100\nCategory = -1\nValue = 0\nFallAudio = \"CoinFall\"\nPickAudio = \"CoinPick\"\nCoinObjectId = -1\n"));
		Register(CreateGodotResourceTemplate("packet-cost-rule-resource", "PacketCostRule", "Packet Cost Rule", "Resources/CardCostRules", "TowerDefensePacketChangeCost", "uid://d2h8osltb268y", "Registry/Behavior/Card/Cost/TowerDefensePacketChangeCost.cs", "TowerDefensePacketChangeCost", "method = \"Increase\"\nkey = \"${Name}\"\nlockCost = false\nskip = false\n"));
		Register(CreateGodotResourceTemplate("packet-override-resource", "PacketOverride", "Packet Override", "Resources/CardOverrides", "TowerDefensePacketOverride", "uid://bs4450j4g7ip6", "Registry/Battle/Feature/PacketBank/Resource/Packet/Override/TowerDefensePacketOverride.cs", "TowerDefensePacketOverride", "type = -1\ncostRise = -1\ncost = -1\ncostMultiple = -1.0\npacketCooldown = -1.0\nstartingCooldown = -1.0\nweight = -1\nwavePointCost = -1\nislimitGridNum = true\ncoverCanDirectPlant = false\nhypnoses = false\n"));
		Register(CreateGodotResourceTemplate("bgm-resource", "BGM", "BGM Resource", "Resources/BGMConfigs", "TowerDefenseBackgroundMusicConfig", "uid://c7pu3uq6hdh6q", "Registry/Battle/Feature/BGM/Resource/TowerDefenseBackgroundMusicConfig.cs", "TowerDefenseBackgroundMusicConfig", "translate = \"${DisplayName}\"\nentry = \"\"\nflag1 = \"\"\ndrums = \"\"\nwin = \"\"\ndrumsZombieThreshold = 10\ndrumsFadeSpeed = 1.0\ndrumsCheckInterval = 0.25\n"));
		Register(CreateSimpleResourceTemplate("simple-resource", "Resource", "通用资源", "Resources", "可直接编辑名称和元数据的通用资源"));
		Register(new TemplateInfo
		{
			Id = "localization-csv",
			Category = "Localization",
			DisplayName = "Localization CSV",
			DefaultFolder = "Localization",
			FileExtension = ".csv",
			PreviewText = "key,zh_CN,en_US",
			Content = "key,zh_CN,en_US\n${Name}.name,,\n${Name}.description,,\n"
		});
		Register(new TemplateInfo
		{
			Id = "blueprint",
			Category = "Blueprint",
			DisplayName = "Blueprint",
			DefaultFolder = "Scripts",
			FileExtension = ".tres",
			Description = "用拼图节点创建 Mod 逻辑，可生成受保护的 C# 构建产物。",
			PreviewText = "蓝图拼图\n• 图表\n• 方法\n• 变量\n• 信号",
			Content = ""
		});
	}
}
