using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorTestModExampleBatchTwoProbe : Node
{
	private sealed class ExampleSpec
	{
		public string TemplateId { get; init; } = "";

		public string FileName { get; init; } = "";

		public string DisplayName { get; init; } = "";

		public string ExpectedClass { get; init; } = "";

		public string ExpectedEditorCategory { get; init; } = "";
	}

	private static readonly ExampleSpec[] Examples = new ExampleSpec[17]
	{
		new ExampleSpec
		{
			TemplateId = "character-armor-data-resource",
			FileName = "ExampleArmorData",
			DisplayName = "示例角色护甲集合",
			ExpectedClass = "CharacterArmorData",
			ExpectedEditorCategory = "CharacterData"
		},
		new ExampleSpec
		{
			TemplateId = "character-custom-data-resource",
			FileName = "ExampleCustomData",
			DisplayName = "示例角色外观集合",
			ExpectedClass = "CharacterCustomData",
			ExpectedEditorCategory = "CharacterData"
		},
		new ExampleSpec
		{
			TemplateId = "character-damage-point-data-resource",
			FileName = "ExampleDamagePointData",
			DisplayName = "示例角色受伤点集合",
			ExpectedClass = "CharacterDamagePointData",
			ExpectedEditorCategory = "CharacterData"
		},
		new ExampleSpec
		{
			TemplateId = "character-definition",
			FileName = "ExamplePlantConfig",
			DisplayName = "示例植物配置",
			ExpectedClass = "TowerDefensePlantConfig",
			ExpectedEditorCategory = "Character"
		},
		new ExampleSpec
		{
			TemplateId = "character-component-resource",
			FileName = "ExampleComponentSet",
			DisplayName = "示例角色组件拼图",
			ExpectedClass = "CharacterComponentSet",
			ExpectedEditorCategory = "CharacterComponent"
		},
		new ExampleSpec
		{
			TemplateId = "level-catalog-resource",
			FileName = "ExampleLevelCatalog",
			DisplayName = "示例关卡选择目录",
			ExpectedClass = "LevelCatalogConfig",
			ExpectedEditorCategory = "LevelCatalog"
		},
		new ExampleSpec
		{
			TemplateId = "level-packet-entry-resource",
			FileName = "ExampleLevelPacket",
			DisplayName = "示例选卡条目",
			ExpectedClass = "TowerDefenseLevelPacketConfig",
			ExpectedEditorCategory = "PacketSpawnEntry"
		},
		new ExampleSpec
		{
			TemplateId = "conveyor-packet-entry-resource",
			FileName = "ExampleConveyorPacket",
			DisplayName = "示例传送带卡牌条目",
			ExpectedClass = "TowerDefenseConveyorPacketConfig",
			ExpectedEditorCategory = "PacketSpawnEntry"
		},
		new ExampleSpec
		{
			TemplateId = "rain-packet-entry-resource",
			FileName = "ExampleRainPacket",
			DisplayName = "示例天降卡牌条目",
			ExpectedClass = "TowerDefenseRainModePacketConfig",
			ExpectedEditorCategory = "PacketSpawnEntry"
		},
		new ExampleSpec
		{
			TemplateId = "conveyor-add-packet-event-resource",
			FileName = "ExampleAddPacketEvent",
			DisplayName = "示例传送带加卡事件",
			ExpectedClass = "TowerDefenseConveyorEventAddPacket",
			ExpectedEditorCategory = "ConveyorEvent"
		},
		new ExampleSpec
		{
			TemplateId = "level-wave-manager-resource",
			FileName = "ExampleWaveManager",
			DisplayName = "示例完整波次管理器",
			ExpectedClass = "TowerDefenseLevelWaveManagerConfig",
			ExpectedEditorCategory = "GameplayLogic"
		},
		new ExampleSpec
		{
			TemplateId = "level-wave-resource",
			FileName = "ExampleWave",
			DisplayName = "示例波次",
			ExpectedClass = "TowerDefenseLevelWaveConfig",
			ExpectedEditorCategory = "GameplayLogic"
		},
		new ExampleSpec
		{
			TemplateId = "level-wave-spawn-resource",
			FileName = "ExampleLaneSpawn",
			DisplayName = "示例线路生成项",
			ExpectedClass = "TowerDefenseLevelSpawnConfig",
			ExpectedEditorCategory = "GameplayLogic"
		},
		new ExampleSpec
		{
			TemplateId = "level-grid-wave-spawn-resource",
			FileName = "ExampleGridSpawn",
			DisplayName = "示例格子生成项",
			ExpectedClass = "TowerDefenseLevelGridSpawnConfig",
			ExpectedEditorCategory = "GameplayLogic"
		},
		new ExampleSpec
		{
			TemplateId = "map-cell-resource",
			FileName = "ExampleMapCell",
			DisplayName = "示例地图格子画笔",
			ExpectedClass = "TowerDefenseCellConfig",
			ExpectedEditorCategory = "MapCell"
		},
		new ExampleSpec
		{
			TemplateId = "packet-event-resource",
			FileName = "ExamplePacketCostEvent",
			DisplayName = "示例卡牌费用事件",
			ExpectedClass = "CardActionBehaviorChangeCost",
			ExpectedEditorCategory = "PacketEvent"
		},
		new ExampleSpec
		{
			TemplateId = "packet-override-resource",
			FileName = "ExamplePacketOverride",
			DisplayName = "示例卡牌覆盖规则",
			ExpectedClass = "TowerDefensePacketOverride",
			ExpectedEditorCategory = "PacketOverride"
		}
	};

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool f3 = false;
		bool projectLoaded = false;
		bool created = true;
		bool strongTypes = true;
		bool chineseNames = true;
		bool linkedExamples = false;
		bool visualRoutes = true;
		bool opened = true;
		bool inspectorUntouched = true;
		bool manifestSynced = false;
		bool reload = true;
		int createdCount = 0;
		int openedCount = 0;
		try
		{
			string projectRoot = Normalize(System.Environment.GetEnvironmentVariable("PVZHE_TEST_MOD_ROOT"));
			Require(!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot), "没有通过 PVZHE_TEST_MOD_ROOT 指定可用的测试 Mod 工程。");
			string projectFile = Directory.GetFiles(projectRoot, "*.pvzmodeproject", SearchOption.TopDirectoryOnly).SingleOrDefault();
			Require(!string.IsNullOrWhiteSpace(projectFile), "测试 Mod 工程文件不存在或数量不唯一。");
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "无法实例化 ModEditorManager。");
			await WaitFrames(2);
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = true
			});
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = false
			});
			f3 = await WaitForEditor(900);
			Require(f3, "F3 未能初始化 Mod 编辑器。");
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			(modEditorPanel?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
			ModProject modProject = ModProject.Load(projectFile);
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			projectLoaded = modProject != null && GodotObject.IsInstanceValid(modEditorPanel) && (bool)(method?.Invoke(modEditorPanel, new object[1] { modProject }) ?? ((object)false));
			Require(projectLoaded, "无法通过真实 Mod 编辑器入口载入测试工程。");
			await WaitFrames(8);
			Node inspectorSentinel = new Node
			{
				Name = "TestModExampleBatchTwoInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
			List<(ExampleSpec Spec, string Path, Resource Resource)> createdResources = new List<(ExampleSpec, string, Resource)>();
			ExampleSpec[] examples = Examples;
			foreach (ExampleSpec exampleSpec in examples)
			{
				XWTemplateLibrary.TemplateInfo templateInfo = xWTemplateLibrary.FindTemplate(exampleSpec.TemplateId);
				if (templateInfo == null)
				{
					created = false;
					Require(condition: false, "找不到示例模板：" + exampleSpec.TemplateId);
					continue;
				}
				string text = Normalize(Path.Combine(projectRoot, templateInfo.DefaultFolder, exampleSpec.FileName + templateInfo.FileExtension));
				XWTemplateLibrary.TemplateCreateResult templateCreateResult = xWTemplateLibrary.CreateFromTemplate(exampleSpec.TemplateId, projectRoot, exampleSpec.FileName, exampleSpec.DisplayName, overwrite: true);
				bool flag = templateCreateResult.Success && SamePath(templateCreateResult.CreatedPath, text) && File.Exists(text);
				created = created && flag;
				if (flag)
				{
					createdCount++;
				}
				Require(flag, "创建示例失败：" + exampleSpec.TemplateId + " -> " + templateCreateResult.Error);
				if (flag)
				{
					string text2 = File.ReadAllText(text);
					bool flag2 = text2.Contains(exampleSpec.DisplayName, StringComparison.Ordinal) && !text2.Contains("${", StringComparison.Ordinal);
					chineseNames = chineseNames && flag2;
					Require(flag2, "示例未写入中文显示名或仍有模板占位符：" + text);
					Resource resource = LoadResource(text, ResourceLoader.CacheMode.Reuse);
					bool flag3 = GodotObject.IsInstanceValid(resource) && (resource.GetType().Name == exampleSpec.ExpectedClass || resource.IsClass(exampleSpec.ExpectedClass));
					strongTypes = strongTypes && flag3;
					Require(flag3, $"示例类型错误：{exampleSpec.TemplateId}，期望 {exampleSpec.ExpectedClass}，实际 {resource?.GetType().Name ?? "null"}");
					if (flag3)
					{
						createdResources.Add((exampleSpec, text, resource));
					}
				}
			}
			linkedExamples = LinkCharacterDataExamples(projectRoot, createdResources);
			Require(linkedExamples, "角色数据集合或植物配置没有连接到现有中文子资源示例。");
			manifestSynced = XWModManifestSyncService.SyncProject(projectRoot) || File.Exists(Path.Combine(projectRoot, "mod.json"));
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			foreach (var item3 in createdResources)
			{
				string item = item3.Path;
				string relative = XWModManifestSyncService.ToProjectRelativeManifestPath(item, projectRoot);
				manifestSynced &= xWModManifest != null && xWModManifest.Resources?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) == true;
			}
			Require(manifestSynced, "第二批示例没有完整同步到 mod.json。");
			XWFileSystemPanel fileSystemPanel = XWFileSystemPanel.Instance;
			XWFileSystemList fileList = fileSystemPanel?.GetNodeOrNull<XWFileSystemList>("%FileList");
			Require(GodotObject.IsInstanceValid(fileSystemPanel) && GodotObject.IsInstanceValid(fileList), "测试 Mod 文件列表不可用。");
			XWFileSystem.GetSingleton().SetProjectFolderPath(projectRoot);
			fileSystemPanel.NavigateToProject(projectRoot);
			await WaitFrames(6);
			foreach (var (spec, path, resource2) in createdResources)
			{
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				await WaitFrames(1);
				XWVisualEditorDescriptor descriptor;
				bool flag4 = XWResourceEditorRegistry.TryGetEditor(resource2, path, out descriptor) && descriptor.Category == spec.ExpectedEditorCategory && descriptor.Category != "General";
				visualRoutes = visualRoutes && flag4;
				Require(flag4, "示例没有进入专用可视化编辑器：" + spec.TemplateId + " -> " + (descriptor?.Category ?? "无"));
				if (!flag4)
				{
					continue;
				}
				fileSystemPanel.NavigateToPath(path);
				await WaitFrames(2);
				int[] selectedItems = fileList.GetSelectedItems();
				bool flag5 = selectedItems.Length == 1 && selectedItems[0] >= 0 && selectedItems[0] < fileList.ItemCount;
				opened = opened && flag5;
				Require(flag5, "示例没有出现在真实文件列表中：" + spec.TemplateId);
				if (flag5)
				{
					fileList.EmitSignal(ItemList.SignalName.ItemActivated, (long)selectedItems[0]);
					await WaitFrames(3);
					XWGenericVisualResourceEditor xWGenericVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor(descriptor.DockKey) as XWGenericVisualResourceEditor;
					bool flag6 = GodotObject.IsInstanceValid(xWGenericVisualResourceEditor) && SamePath(xWGenericVisualResourceEditor.ActiveResourcePath, path);
					opened = opened && flag6;
					if (flag6)
					{
						openedCount++;
					}
					Require(flag6, "示例未在主面板打开：" + spec.TemplateId);
					bool flag7 = !(XWEditorInterface.Instance.GetInspector() is XWInspector xWInspector) || xWInspector.CurrentObject == inspectorSentinel;
					inspectorUntouched = inspectorUntouched && flag7;
					Require(flag7, "示例错误地占用了 Inspector：" + spec.TemplateId);
					descriptor = null;
				}
			}
			foreach (var item4 in createdResources)
			{
				ExampleSpec item2 = item4.Spec;
				Resource resource3 = LoadResource(item4.Path, ResourceLoader.CacheMode.Replace);
				bool flag8 = GodotObject.IsInstanceValid(resource3) && (resource3.GetType().Name == item2.ExpectedClass || resource3.IsClass(item2.ExpectedClass));
				reload = reload && flag8;
				Require(flag8, "示例保存后无法重新加载：" + item2.TemplateId);
			}
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_TEST_MOD_EXAMPLE_BATCH_TWO_PROBE] f3={f3} projectLoaded={projectLoaded} created={created} strongTypes={strongTypes} chineseNames={chineseNames} linkedExamples={linkedExamples} visualRoutes={visualRoutes} opened={opened} inspectorUntouched={inspectorUntouched} manifestSynced={manifestSynced} reload={reload} createdCount={createdCount}/{Examples.Length} openedCount={openedCount}/{Examples.Length} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_TEST_MOD_EXAMPLE_BATCH_TWO_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private bool LinkCharacterDataExamples(string projectRoot, IReadOnlyList<(ExampleSpec Spec, string Path, Resource Resource)> createdResources)
	{
		CharacterArmorData characterArmorData = Find<CharacterArmorData>(createdResources, "character-armor-data-resource");
		CharacterCustomData characterCustomData = Find<CharacterCustomData>(createdResources, "character-custom-data-resource");
		CharacterDamagePointData characterDamagePointData = Find<CharacterDamagePointData>(createdResources, "character-damage-point-data-resource");
		TowerDefensePlantConfig towerDefensePlantConfig = Find<TowerDefensePlantConfig>(createdResources, "character-definition");
		ArmorSlotConfig armorSlotConfig = LoadResource(Path.Combine(projectRoot, "Resources/CharacterData/Armor/ExampleArmorSlot.tres"), ResourceLoader.CacheMode.Reuse) as ArmorSlotConfig;
		CharacterCustomConfig characterCustomConfig = LoadResource(Path.Combine(projectRoot, "Resources/CharacterData/Custom/ExampleCustomConfig.tres"), ResourceLoader.CacheMode.Reuse) as CharacterCustomConfig;
		CharacterDamagePointConfig characterDamagePointConfig = LoadResource(Path.Combine(projectRoot, "Resources/CharacterData/DamagePoints/ExampleDamagePoint.tres"), ResourceLoader.CacheMode.Reuse) as CharacterDamagePointConfig;
		if (!GodotObject.IsInstanceValid(characterArmorData) || !GodotObject.IsInstanceValid(characterCustomData) || !GodotObject.IsInstanceValid(characterDamagePointData) || !GodotObject.IsInstanceValid(towerDefensePlantConfig) || !GodotObject.IsInstanceValid(armorSlotConfig) || !GodotObject.IsInstanceValid(characterCustomConfig) || !GodotObject.IsInstanceValid(characterDamagePointConfig))
		{
			return false;
		}
		characterArmorData.armorList = new Array<ArmorSlotConfig> { armorSlotConfig };
		characterCustomData.customList = new Array<CharacterCustomConfig> { characterCustomConfig };
		characterDamagePointData.damagePointList = new Array<CharacterDamagePointConfig> { characterDamagePointConfig };
		towerDefensePlantConfig.armorData = characterArmorData;
		towerDefensePlantConfig.customData = characterCustomData;
		towerDefensePlantConfig.damagePointData = characterDamagePointData;
		if (Save(characterArmorData) && Save(characterCustomData) && Save(characterDamagePointData))
		{
			return Save(towerDefensePlantConfig);
		}
		return false;
	}

	private static T Find<T>(IReadOnlyList<(ExampleSpec Spec, string Path, Resource Resource)> resources, string templateId) where T : Resource
	{
		return resources.FirstOrDefault(((ExampleSpec Spec, string Path, Resource Resource) item) => string.Equals(item.Spec.TemplateId, templateId, StringComparison.Ordinal)).Resource as T;
	}

	private static bool Save(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			return ResourceSaver.Save(resource, resource.ResourcePath, ResourceSaver.SaverFlags.None) == Error.Ok;
		}
		return false;
	}

	private static Resource LoadResource(string path, ResourceLoader.CacheMode cacheMode)
	{
		return ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(Normalize(path)).Replace('\\', '/'), "", cacheMode);
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance != null && XWEditorInterface.Instance.GetEditorPanel() is ModEditorPanel)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private static bool SamePath(string left, string right)
	{
		return string.Equals(Normalize(left).TrimEnd('/'), Normalize(right).TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
	}

	private static string Normalize(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Replace('\\', '/');
		}
		return "";
	}
}
