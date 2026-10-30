using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorTestModExampleBatchProbe : Node
{
	private sealed class ExampleSpec
	{
		public string TemplateId { get; init; } = "";

		public string FileName { get; init; } = "";

		public string DisplayName { get; init; } = "";

		public string ExpectedClass { get; init; } = "";

		public string ExpectedEditorCategory { get; init; } = "";
	}

	private static readonly ExampleSpec[] Examples = new ExampleSpec[16]
	{
		new ExampleSpec
		{
			TemplateId = "state-machine-resource",
			FileName = "ExampleStateMachine",
			DisplayName = "示例状态机",
			ExpectedClass = "StateMachineDefinition",
			ExpectedEditorCategory = "StateMachine"
		},
		new ExampleSpec
		{
			TemplateId = "aabb-ray-resource",
			FileName = "ExampleCollisionRay",
			DisplayName = "示例碰撞射线",
			ExpectedClass = "AabbRay2DResource",
			ExpectedEditorCategory = "CollisionGeometry"
		},
		new ExampleSpec
		{
			TemplateId = "aabb-shape-resource",
			FileName = "ExampleCollisionArea",
			DisplayName = "示例碰撞区域",
			ExpectedClass = "AabbShape2DResource",
			ExpectedEditorCategory = "CollisionGeometry"
		},
		new ExampleSpec
		{
			TemplateId = "buff-visual-resource",
			FileName = "ExampleBuffVisual",
			DisplayName = "示例状态外观",
			ExpectedClass = "BuffVisualDefinition",
			ExpectedEditorCategory = "BuffVisual"
		},
		new ExampleSpec
		{
			TemplateId = "character-attack-resource",
			FileName = "ExampleAttack",
			DisplayName = "示例攻击参数",
			ExpectedClass = "AttackConfig",
			ExpectedEditorCategory = "CharacterCombat"
		},
		new ExampleSpec
		{
			TemplateId = "character-buff-burn-resource",
			FileName = "ExampleBurnBuff",
			DisplayName = "示例灼烧状态",
			ExpectedClass = "TowerDefenseCharacterBuffBurn",
			ExpectedEditorCategory = "CharacterCombat"
		},
		new ExampleSpec
		{
			TemplateId = "character-buff-frozen-resource",
			FileName = "ExampleFrozenBuff",
			DisplayName = "示例冰冻状态",
			ExpectedClass = "TowerDefenseCharacterBuffFrozen",
			ExpectedEditorCategory = "CharacterCombat"
		},
		new ExampleSpec
		{
			TemplateId = "character-buff-hypnoses-resource",
			FileName = "ExampleHypnosesBuff",
			DisplayName = "示例魅惑状态",
			ExpectedClass = "TowerDefenseCharacterBuffHypnoses",
			ExpectedEditorCategory = "CharacterCombat"
		},
		new ExampleSpec
		{
			TemplateId = "character-buff-poison-resource",
			FileName = "ExamplePoisonBuff",
			DisplayName = "示例中毒状态",
			ExpectedClass = "TowerDefenseCharacterBuffPoisoning",
			ExpectedEditorCategory = "CharacterCombat"
		},
		new ExampleSpec
		{
			TemplateId = "character-armor-slot-resource",
			FileName = "ExampleArmorSlot",
			DisplayName = "示例护甲槽位",
			ExpectedClass = "ArmorSlotConfig",
			ExpectedEditorCategory = "CharacterData"
		},
		new ExampleSpec
		{
			TemplateId = "character-armor-type-resource",
			FileName = "ExampleArmorType",
			DisplayName = "示例护甲类型",
			ExpectedClass = "TowerDefenseArmorTypeData",
			ExpectedEditorCategory = "CharacterData"
		},
		new ExampleSpec
		{
			TemplateId = "character-custom-config-resource",
			FileName = "ExampleCustomConfig",
			DisplayName = "示例角色自定义项",
			ExpectedClass = "CharacterCustomConfig",
			ExpectedEditorCategory = "CharacterData"
		},
		new ExampleSpec
		{
			TemplateId = "character-damage-point-config-resource",
			FileName = "ExampleDamagePoint",
			DisplayName = "示例角色受伤点",
			ExpectedClass = "CharacterDamagePointConfig",
			ExpectedEditorCategory = "CharacterData"
		},
		new ExampleSpec
		{
			TemplateId = "collectable-resource",
			FileName = "NewCollectable",
			DisplayName = "示例收集物",
			ExpectedClass = "CollectableConfig",
			ExpectedEditorCategory = "Collectable"
		},
		new ExampleSpec
		{
			TemplateId = "falling-object-resource",
			FileName = "NewFallingObject",
			DisplayName = "示例掉落物",
			ExpectedClass = "FallingObjectConfig",
			ExpectedEditorCategory = "FallingObject"
		},
		new ExampleSpec
		{
			TemplateId = "projectile-change-resource",
			FileName = "NewProjectileChange",
			DisplayName = "示例子弹变化",
			ExpectedClass = "ChangeProjectileConfig",
			ExpectedEditorCategory = "ProjectileChange"
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
				Name = "TestModExampleInspectorSentinel"
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
					Resource resource = ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(text).Replace('\\', '/'), "", ResourceLoader.CacheMode.Reuse);
					bool flag3 = GodotObject.IsInstanceValid(resource) && (resource.GetType().Name == exampleSpec.ExpectedClass || resource.IsClass(exampleSpec.ExpectedClass));
					strongTypes = strongTypes && flag3;
					Require(flag3, $"示例类型错误：{exampleSpec.TemplateId}，期望 {exampleSpec.ExpectedClass}，实际 {resource?.GetType().Name ?? "null"}");
					if (flag3)
					{
						createdResources.Add((exampleSpec, text, resource));
					}
				}
			}
			manifestSynced = XWModManifestSyncService.SyncProject(projectRoot) || File.Exists(Path.Combine(projectRoot, "mod.json"));
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			foreach (var item3 in createdResources)
			{
				string item = item3.Path;
				string relative = XWModManifestSyncService.ToProjectRelativeManifestPath(item, projectRoot);
				manifestSynced &= xWModManifest != null && xWModManifest.Resources?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) == true;
			}
			Require(manifestSynced, "新示例没有完整同步到 mod.json。");
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
					if (!flag6)
					{
						Resource resource3 = (XWEditorInterface.Instance.GetInspector() as XWInspector)?.CurrentObject as Resource;
						XWVisualEditorDescriptor descriptor2 = null;
						bool flag7 = GodotObject.IsInstanceValid(resource3) && XWResourceEditorRegistry.TryGetEditor(resource3, path, out descriptor2);
						GD.Print($"[MOD_EDITOR_TEST_MOD_EXAMPLE_OPEN_DIAGNOSTIC] template={spec.TemplateId} dock={descriptor.DockKey} editor={xWGenericVisualResourceEditor?.GetType().Name ?? "null"} activePath={xWGenericVisualResourceEditor?.ActiveResourcePath ?? "null"} expectedPath={path} inspectedType={resource3?.GetType().Name ?? "null"} inspectedRoute={flag7} inspectedCategory={(flag7 ? descriptor2.Category : "无")}");
					}
					opened = opened && flag6;
					if (flag6)
					{
						openedCount++;
					}
					Require(flag6, "示例未在主面板打开：" + spec.TemplateId);
					XWInspector xWInspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
					bool flag8 = xWInspector == null || xWInspector.CurrentObject == inspectorSentinel;
					if (!flag8)
					{
						GD.Print($"[MOD_EDITOR_TEST_MOD_EXAMPLE_INSPECTOR_DIAGNOSTIC] template={spec.TemplateId} current={xWInspector?.CurrentObject?.GetType().Name ?? "null"}");
					}
					inspectorUntouched = inspectorUntouched && flag8;
					Require(flag8, "示例错误地占用了 Inspector：" + spec.TemplateId);
					descriptor = null;
				}
			}
			foreach (var item4 in createdResources)
			{
				ExampleSpec item2 = item4.Spec;
				Resource resource4 = ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(item4.Path).Replace('\\', '/'), "", ResourceLoader.CacheMode.Replace);
				bool flag9 = GodotObject.IsInstanceValid(resource4) && (resource4.GetType().Name == item2.ExpectedClass || resource4.IsClass(item2.ExpectedClass));
				reload = reload && flag9;
				Require(flag9, "示例保存后无法重新加载：" + item2.TemplateId);
			}
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_TEST_MOD_EXAMPLE_BATCH_PROBE] f3={f3} projectLoaded={projectLoaded} created={created} strongTypes={strongTypes} chineseNames={chineseNames} visualRoutes={visualRoutes} opened={opened} inspectorUntouched={inspectorUntouched} manifestSynced={manifestSynced} reload={reload} createdCount={createdCount}/{Examples.Length} openedCount={openedCount}/{Examples.Length} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_TEST_MOD_EXAMPLE_BATCH_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
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
