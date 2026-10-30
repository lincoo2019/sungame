using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;
using PVZHE.ModEditor.Tools.GUI;

public partial class ModEditorResourceCreateAutoOpenRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private string _probeRoot = "";

	public override async void _Ready()
	{
		bool f3 = false;
		bool rightClick = false;
		bool toolbar = false;
		bool iconMenus = false;
		bool singleCreate = false;
		bool stateOpened = false;
		bool cardOpened = false;
		bool selected = false;
		bool saveReload = false;
		bool inspectorUntouched = false;
		bool animationCreated = false;
		bool animationOpened = false;
		bool characterChildren = false;
		bool scriptBases = false;
		bool blueprintBases = false;
		bool battleScriptsCompiled = false;
		bool battleTypesAssignable = false;
		bool templatePanel = false;
		bool templateChinese = false;
		bool templateIcons = false;
		bool templateKeysStable = false;
		bool templateCreation = false;
		bool stableTemplateNames = false;
		try
		{
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish(f3: false, rightClick, toolbar, iconMenus, singleCreate, stateOpened, cardOpened, selected, saveReload, inspectorUntouched, animationCreated, animationOpened, characterChildren, scriptBases, blueprintBases, battleScriptsCompiled, battleTypesAssignable, templatePanel, templateChinese, templateIcons, templateKeysStable, templateCreation, stableTemplateNames);
				return;
			}
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
			Require(f3, "F3 did not initialize the Mod editor resource creation surfaces.");
			if (!f3)
			{
				Finish(f3: false, rightClick, toolbar, iconMenus, singleCreate, stateOpened, cardOpened, selected, saveReload, inspectorUntouched, animationCreated, animationOpened, characterChildren, scriptBases, blueprintBases, battleScriptsCompiled, battleTypesAssignable, templatePanel, templateChinese, templateIcons, templateKeysStable, templateCreation, stableTemplateNames);
				return;
			}
			XWFileSystemPanel panel = XWFileSystemPanel.Instance;
			ModEditorPanel editorPanel = XWEditorInterface.Instance.GetEditorPanel() as ModEditorPanel;
			(editorPanel?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
			string text = ProjectSettings.GlobalizePath("user://ModEditorResourceCreateAutoOpenProbe");
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			ModProject modProject = ModProject.Create(text, "资源创建验收", "1.0.0", "后台探针", "Mod 编辑器模板、资源创建和自动打开验收");
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			bool flag = modProject != null && GodotObject.IsInstanceValid(editorPanel) && (bool)(method?.Invoke(editorPanel, new object[1] { modProject }) ?? ((object)false));
			Require(flag, "Could not enter the temporary Mod through the real F3 project route.");
			if (!flag)
			{
				Finish(f3, rightClick, toolbar, iconMenus, singleCreate, stateOpened, cardOpened, selected, saveReload, inspectorUntouched, animationCreated, animationOpened, characterChildren, scriptBases, blueprintBases, battleScriptsCompiled, battleTypesAssignable, templatePanel, templateChinese, templateIcons, templateKeysStable, templateCreation, stableTemplateNames);
				return;
			}
			_probeRoot = Normalize(modProject.ProjectPath);
			panel.NavigateToProject(_probeRoot);
			await WaitFrames(8);
			(templatePanel, templateChinese, templateIcons, templateKeysStable, templateCreation, stableTemplateNames) = await AuditTemplatePresentationAndPanelAsync(editorPanel);
			Require(templatePanel, "The real Mod template panel is unavailable or incomplete.");
			Require(templateChinese, "One or more template UI labels are not Chinese.");
			Require(templateIcons, "One or more visual template choices have no icon.");
			Require(templateKeysStable, "Localized template presentation changed a stable technical key.");
			Require(templateCreation, "The real template panel did not create both C# and resource templates.");
			Require(stableTemplateNames, "Chinese template presentation polluted a generated class, path, or resource id.");
			(battleScriptsCompiled, battleTypesAssignable) = await AuditBattleScriptTemplatesAsync();
			Require(battleScriptsCompiled, "Feature/Process C# templates did not compile as an isolated Mod assembly.");
			Require(battleTypesAssignable, "Compiled Feature/Process templates are not assignable to their game base classes.");
			Node inspectorSentinel = new Node
			{
				Name = "ResourceCreateInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			string text2 = Normalize(Path.Combine(_probeRoot, "Resources", "Animations"));
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-animation", text2, "空白动画");
			string animationPath = Normalize(Path.Combine(text2, "空白动画.tres"));
			AdobeAnimateData animation = ResourceLoader.Load<AdobeAnimateData>(ProjectSettings.LocalizePath(animationPath).Replace('\\', '/'), "", ResourceLoader.CacheMode.Reuse);
			animationCreated = templateCreateResult.Success && SamePath(templateCreateResult.CreatedPath, animationPath) && GodotObject.IsInstanceValid(animation) && Mathf.IsEqualApprox(animation.frameRate, 30.0) && animation.frameMax == 0;
			Require(animationCreated, "Adobe Animate creation route did not produce a strong typed blank animation resource.");
			if (GodotObject.IsInstanceValid(animation))
			{
				XWFileSystem.GetSingleton().ScanChanges();
				panel.NavigateToPath(animationPath);
				await WaitFrames(3);
				XWFileSystemList node = panel.GetNode<XWFileSystemList>("%FileList");
				int animationIndex = FindListItemByPath(node, animationPath);
				if (animationIndex >= 0)
				{
					node.Select(animationIndex);
					node.EmitSignal(ItemList.SignalName.ItemSelected, (long)animationIndex);
				}
				await WaitFrames(40);
				Control nodeOrNull = panel.GetNodeOrNull<Control>("%AnimationFilePreview");
				bool flag2 = animationIndex >= 0 && GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.Visible;
				Require(flag2, "Single-click animation selection did not open the filesystem visual preview.");
				GD.Print($"[MOD_EDITOR_FILESYSTEM_ANIMATION_PREVIEW] selected={animationIndex >= 0} visible={flag2}");
				string path = ProjectSettings.LocalizePath(animationPath).Replace('\\', '/');
				XWEditorInterface.Instance.EditResource(animation, XWResourceEditContext.ForRoot(animation, path, "file_system"));
				animationOpened = await WaitForAnimation(animationPath, 180);
			}
			Require(animationOpened, "The created blank animation did not open in the visual animation editor.");
			(characterChildren, scriptBases, blueprintBases) = AuditCharacterSubResources();
			Require(characterChildren, "One or more character package child configs used the wrong strong type.");
			Require(scriptBases, "One or more character package child C# scripts used the wrong base class.");
			Require(blueprintBases, "One or more character package child Blueprints used the wrong ExtendsClass.");
			string stateDirectory = Normalize(Path.Combine(_probeRoot, "Resources", "StateMachines"));
			string statePath = Normalize(Path.Combine(stateDirectory, "EntryStateMachine.tres"));
			XWFileSystemTree tree = panel.GetNode<XWFileSystemTree>("%FileTree");
			TreeItem stateDirectoryItem = FindTreeItemByPath(tree.GetRoot(), stateDirectory);
			Require(stateDirectoryItem != null, "StateMachines directory is missing from the file-system tree.");
			if (stateDirectoryItem != null)
			{
				ExpandAncestors(stateDirectoryItem);
				tree.DeselectAll();
				stateDirectoryItem.Select(0);
				tree.ScrollToItem(stateDirectoryItem);
				await WaitFrames(2);
				Rect2 itemAreaRect = tree.GetItemAreaRect(stateDirectoryItem, 0);
				Vector2 vector = itemAreaRect.Position + itemAreaRect.Size * 0.5f;
				tree.EmitSignal(Tree.SignalName.ItemMouseSelected, vector, 2L);
				await WaitFrames(1);
				PopupMenu node2 = panel.GetNode<PopupMenu>("%TreePopupMenu");
				int num = FindMenuItem(node2, "新建状态机");
				rightClick = num >= 0;
				bool stateMenuIcon = num >= 0 && GodotObject.IsInstanceValid(node2.GetItemIcon(num));
				Require(rightClick, "The real StateMachines right-click menu has no state-machine creation entry.");
				Require(stateMenuIcon, "The state-machine creation entry is still text-only.");
				if (num >= 0)
				{
					node2.EmitSignal(PopupMenu.SignalName.IdPressed, node2.GetItemId(num));
					ConfirmationDialog confirmationDialog = await WaitForNameDialog(panel, 60);
					Require(GodotObject.IsInstanceValid(confirmationDialog), "State-machine creation did not open its name dialog.");
					if (GodotObject.IsInstanceValid(confirmationDialog))
					{
						LineEdit node3 = confirmationDialog.GetNode<LineEdit>("%NameEdit");
						node3.Text = "EntryStateMachine";
						node3.EmitSignal(LineEdit.SignalName.TextSubmitted, node3.Text);
						confirmationDialog.EmitSignal(AcceptDialog.SignalName.Confirmed);
					}
				}
				stateOpened = await WaitForStateMachine(statePath, 180);
				Require(stateOpened, "The newly created state machine did not automatically open in the graph editor.");
				Label node4 = panel.GetNode<Label>("%PreviewFileNameLabel");
				selected = node4.Text == "EntryStateMachine.tres";
				Require(selected, "The newly created state machine was not selected in the resource browser.");
				iconMenus = stateMenuIcon;
			}
			string cardDirectory = Normalize(Path.Combine(_probeRoot, "Resources", "Cards"));
			string cardPath = Normalize(Path.Combine(cardDirectory, "ToolbarCard.tres"));
			panel.NavigateToPath(cardDirectory + "/");
			await WaitFrames(3);
			MenuButton createButton = panel.GetNode<MenuButton>("%ButtonCreate");
			PopupMenu createMenu = createButton.GetPopup();
			createMenu.EmitSignal(Window.SignalName.AboutToPopup);
			await WaitFrames(1);
			int num2 = FindMenuItem(createMenu, "新建卡片");
			toolbar = GodotObject.IsInstanceValid(createButton) && num2 >= 0;
			bool flag3 = GodotObject.IsInstanceValid(createButton.Icon) && num2 >= 0 && GodotObject.IsInstanceValid(createMenu.GetItemIcon(num2));
			iconMenus = iconMenus && flag3;
			Require(toolbar, "The top create menu did not follow the currently displayed Cards directory.");
			Require(flag3, "The top create menu or card entry is still text-only.");
			if (num2 >= 0)
			{
				createMenu.EmitSignal(PopupMenu.SignalName.IdPressed, createMenu.GetItemId(num2));
				ConfirmationDialog confirmationDialog2 = await WaitForNameDialog(panel, 60);
				Require(GodotObject.IsInstanceValid(confirmationDialog2), "Toolbar card creation did not open its name dialog.");
				if (GodotObject.IsInstanceValid(confirmationDialog2))
				{
					LineEdit node5 = confirmationDialog2.GetNode<LineEdit>("%NameEdit");
					node5.Text = "ToolbarCard";
					node5.EmitSignal(LineEdit.SignalName.TextSubmitted, node5.Text);
					confirmationDialog2.EmitSignal(AcceptDialog.SignalName.Confirmed);
				}
			}
			cardOpened = await WaitForCard(cardPath, 180);
			Require(cardOpened, "The toolbar-created card did not automatically open in the card editor.");
			int num3 = Array.FindAll(Directory.GetFiles(stateDirectory, "*.tres"), (string path4) => string.Equals(Path.GetFileName(path4), "EntryStateMachine.tres", StringComparison.OrdinalIgnoreCase)).Length;
			int num4 = Array.FindAll(Directory.GetFiles(cardDirectory, "*.tres"), (string path4) => string.Equals(Path.GetFileName(path4), "ToolbarCard.tres", StringComparison.OrdinalIgnoreCase)).Length;
			string value = string.Join(",", Array.ConvertAll(Directory.GetFiles(stateDirectory, "EntryStateMachine*.tres"), Path.GetFileName));
			string value2 = string.Join(",", Array.ConvertAll(Directory.GetFiles(cardDirectory, "ToolbarCard*.tres"), Path.GetFileName));
			singleCreate = File.Exists(statePath) && File.Exists(cardPath) && num3 == 1 && num4 == 1;
			Require(singleCreate, $"A name dialog did not create exactly one resource (state={num3}, card={num4}, stateExists={File.Exists(statePath)}, cardExists={File.Exists(cardPath)}, stateFiles={value}, cardFiles={value2}).");
			string path2 = ProjectSettings.LocalizePath(statePath).Replace('\\', '/');
			string path3 = ProjectSettings.LocalizePath(cardPath).Replace('\\', '/');
			StateMachineDefinition stateMachineDefinition = ResourceLoader.Load<StateMachineDefinition>(path2, "", ResourceLoader.CacheMode.Replace);
			TowerDefensePacketConfig instance = ResourceLoader.Load<TowerDefensePacketConfig>(path3, "", ResourceLoader.CacheMode.Replace);
			saveReload = GodotObject.IsInstanceValid(stateMachineDefinition) && stateMachineDefinition.DefinitionId == "EntryStateMachine" && stateMachineDefinition.RootStateId == "EntryStateMachine.root" && GodotObject.IsInstanceValid(instance);
			Require(saveReload, "Created resources did not survive cache-ignoring reload.");
			inspectorUntouched = !(XWEditorInterface.Instance.GetInspector() is XWInspector xWInspector) || xWInspector.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "Automatic resource routing replaced the raw Inspector object.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish(f3, rightClick, toolbar, iconMenus, singleCreate, stateOpened, cardOpened, selected, saveReload, inspectorUntouched, animationCreated, animationOpened, characterChildren, scriptBases, blueprintBases, battleScriptsCompiled, battleTypesAssignable, templatePanel, templateChinese, templateIcons, templateKeysStable, templateCreation, stableTemplateNames);
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && XWFileSystemPanel.Instance != null && instance.GetResourceEditor("state_machine_editor") is XWStateMachineVisualResourceEditor && instance.GetResourceEditor("card_editor") is XWCardVisualResourceEditor && instance.GetResourceEditor("animation_editor") is XWAnimationVisualResourceEditor)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForAnimation(string expectedPath, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWAnimationVisualResourceEditor xWAnimationVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("animation_editor") as XWAnimationVisualResourceEditor;
			if (xWAnimationVisualResourceEditor?.ActiveResource is AdobeAnimateData && SamePath(xWAnimationVisualResourceEditor.ActiveResourcePath, expectedPath))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private (bool Configs, bool Scripts, bool Blueprints) AuditCharacterSubResources()
	{
		(string, string, string, string, string)[] obj = new(string, string, string, string, string)[8]
		{
			("Plants", "new-character-plant", "ProbePlant", "TowerDefensePlantConfig", "TowerDefensePlant"),
			("Zombies", "new-character-zombie", "ProbeZombie", "TowerDefenseZombieConfig", "TowerDefenseZombie"),
			("Props", "new-character-prop", "ProbeProp", "TowerDefenseItemConfig", "TowerDefenseItem"),
			("Vases", "new-character-vase", "ProbeVase", "TowerDefenseVaseConfig", "TowerDefenseVase"),
			("Mowers", "new-character-mower", "ProbeMower", "TowerDefenseMowerConfig", "TowerDefenseMower"),
			("Items", "new-character-item", "ProbeItem", "TowerDefenseItemConfig", "TowerDefenseItem"),
			("Graves", "new-character-grave", "ProbeGrave", "TowerDefenseGravestoneConfig", "TowerDefenseGravestone"),
			("Craters", "new-character-crater", "ProbeCrater", "TowerDefenseCraterConfig", "TowerDefenseCrater")
		};
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		(string, string, string, string, string)[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, string, string, string) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			string item4 = tuple.Item4;
			string item5 = tuple.Item5;
			string directoryPath = Normalize(Path.Combine(_probeRoot, "Resources", "Characters", item));
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction(item2, directoryPath, item3);
			if (!templateCreateResult.Success)
			{
				_failures.Add("角色包创建失败：" + item + "，" + templateCreateResult.Error);
				flag = (flag2 = (flag3 = false));
				continue;
			}
			string resourceName = item3 + "扩展配置";
			string text = item3 + "扩展脚本";
			string resourceName2 = item3 + "扩展蓝图";
			XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = XWResourceCreateRoute.CreateSubResourceFromAction("new-character-config-child", templateCreateResult.CreatedPath, resourceName);
			XWTemplateLibrary.TemplateCreateResult templateCreateResult3 = XWResourceCreateRoute.CreateSubResourceFromAction("new-character-script-child", templateCreateResult.CreatedPath, text);
			XWTemplateLibrary.TemplateCreateResult templateCreateResult4 = XWResourceCreateRoute.CreateSubResourceFromAction("new-character-blueprint-child", templateCreateResult.CreatedPath, resourceName2);
			Resource resource = (templateCreateResult2.Success ? ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(templateCreateResult2.CreatedPath).Replace('\\', '/'), "", ResourceLoader.CacheMode.Reuse) : null);
			bool flag4 = templateCreateResult2.Success && GodotObject.IsInstanceValid(resource) && resource.GetType().Name == item4;
			flag = flag && flag4;
			if (!flag4)
			{
				_failures.Add($"角色配置类型错误：{item}，期望 {item4}，实际 {resource?.GetType().Name ?? templateCreateResult2.Error}");
			}
			string value = "public partial class " + XWTemplateLibrary.SanitizeName(text) + " : " + item5;
			bool flag5 = templateCreateResult3.Success && File.Exists(templateCreateResult3.CreatedPath) && File.ReadAllText(templateCreateResult3.CreatedPath).Contains(value, StringComparison.Ordinal);
			flag2 = flag2 && flag5;
			if (!flag5)
			{
				_failures.Add($"角色脚本基类错误：{item}，期望 {item5}。");
			}
			XWBPScript xWBPScript = (templateCreateResult4.Success ? ResourceLoader.Load<XWBPScript>(ProjectSettings.LocalizePath(templateCreateResult4.CreatedPath).Replace('\\', '/'), "", ResourceLoader.CacheMode.Reuse) : null);
			bool flag6 = templateCreateResult4.Success && GodotObject.IsInstanceValid(xWBPScript) && xWBPScript.ExtendsClass.ToString() == item5;
			flag3 = flag3 && flag6;
			if (!flag6)
			{
				_failures.Add($"角色蓝图基类错误：{item}，期望 {item5}。");
			}
			bool flag7 = !XWResourceCreateRoute.CreateSubResourceFromAction("new-character-config-child", templateCreateResult.CreatedPath, resourceName).Success && File.Exists(templateCreateResult2.CreatedPath);
			flag = flag && flag7;
			if (!flag7)
			{
				_failures.Add("角色子资源重复创建未被拒绝：" + item + "。");
			}
		}
		return (Configs: flag, Scripts: flag2, Blueprints: flag3);
	}

	private async Task<(bool Panel, bool Chinese, bool Icons, bool KeysStable, bool Creation, bool StableNames)> AuditTemplatePresentationAndPanelAsync(ModEditorPanel editorPanel)
	{
		XWTemplateLibrary library = new XWTemplateLibrary();
		bool chinese = library.Templates.Count == 92;
		bool suggestedNamesStable = true;
		foreach (XWTemplateLibrary.TemplateInfo template in library.Templates)
		{
			XWTemplatePresentation xWTemplatePresentation = XWTemplatePresentation.Resolve(template);
			chinese &= XWTemplatePresentation.HasChineseText(xWTemplatePresentation.CategoryLabel) && XWTemplatePresentation.HasChineseText(xWTemplatePresentation.DisplayNameLabel) && XWTemplatePresentation.HasChineseText(xWTemplatePresentation.DescriptionLabel) && XWTemplatePresentation.HasChineseText(xWTemplatePresentation.PreviewTextLabel) && XWTemplatePresentation.HasChineseText(xWTemplatePresentation.DefaultFolderLabel);
			suggestedNamesStable &= !string.IsNullOrWhiteSpace(xWTemplatePresentation.SuggestedName) && !XWTemplatePresentation.HasChineseText(xWTemplatePresentation.SuggestedName);
		}
		XWTemplateLibrary.TemplateInfo templateInfo = library.FindTemplate("feature-csharp");
		XWTemplateLibrary.TemplateInfo templateInfo2 = library.FindTemplate("state-machine-resource");
		bool keysStable = templateInfo?.Category == "Feature" && templateInfo2?.Category == "StateMachine" && templateInfo?.Id == "feature-csharp" && templateInfo2?.Id == "state-machine-resource";
		XWModToolsPanel toolsPanel = null;
		for (int frame = 0; frame < 600; frame++)
		{
			toolsPanel = FindFirstDescendant<XWModToolsPanel>(editorPanel);
			if (GodotObject.IsInstanceValid(toolsPanel) && toolsPanel.GetNodeOrNull<HFlowContainer>("%TemplateCardGrid")?.GetChildCount() == library.Templates.Count)
			{
				break;
			}
			await WaitFrames(1);
		}
		XWEditorInterface.Instance.FocusPanel("mod_tools");
		await WaitFrames(4);
		TabContainer toolTabs = toolsPanel?.GetNodeOrNull<TabContainer>("%ToolTabs");
		if (GodotObject.IsInstanceValid(toolTabs))
		{
			toolTabs.CurrentTab = 1;
		}
		await WaitFrames(3);
		HFlowContainer hFlowContainer = toolsPanel?.GetNodeOrNull<HFlowContainer>("%TemplateCardGrid");
		OptionButton optionButton = toolsPanel?.GetNodeOrNull<OptionButton>("%TemplateOption");
		TextEdit preview = toolsPanel?.GetNodeOrNull<TextEdit>("%TemplatePreview");
		LineEdit nameEdit = toolsPanel?.GetNodeOrNull<LineEdit>("%TemplateName");
		Button createButton = toolsPanel?.GetNodeOrNull<Button>("%CreateTemplate");
		bool panel = GodotObject.IsInstanceValid(toolsPanel) && toolsPanel.IsVisibleInTree() && GodotObject.IsInstanceValid(hFlowContainer) && GodotObject.IsInstanceValid(optionButton) && GodotObject.IsInstanceValid(preview) && GodotObject.IsInstanceValid(nameEdit) && GodotObject.IsInstanceValid(createButton) && hFlowContainer.GetChildCount() == library.Templates.Count && optionButton.ItemCount == library.Templates.Count;
		GD.Print($"[MOD_EDITOR_TEMPLATE_PANEL_DIAGNOSTIC] tools={GodotObject.IsInstanceValid(toolsPanel)} visible={GodotObject.IsInstanceValid(toolsPanel) && toolsPanel.IsVisibleInTree()} cards={(GodotObject.IsInstanceValid(hFlowContainer) ? hFlowContainer.GetChildCount() : (-1))} options={(GodotObject.IsInstanceValid(optionButton) ? optionButton.ItemCount : (-1))} expected={library.Templates.Count}");
		bool icons = panel;
		if (panel)
		{
			foreach (Node child in hFlowContainer.GetChildren())
			{
				if (child is Button button)
				{
					chinese &= XWTemplatePresentation.HasChineseText(button.Text) && XWTemplatePresentation.HasChineseText(button.TooltipText);
					icons &= GodotObject.IsInstanceValid(button.Icon);
				}
			}
			for (int i = 0; i < optionButton.ItemCount; i++)
			{
				chinese &= XWTemplatePresentation.HasChineseText(optionButton.GetItemText(i));
				icons &= GodotObject.IsInstanceValid(optionButton.GetItemIcon(i));
			}
			chinese &= XWTemplatePresentation.HasChineseText(preview.Text);
		}
		int num = FindTemplateIndex(library, "feature-csharp");
		int num2 = FindTemplateIndex(library, "state-machine-resource");
		int num3 = FindTemplateIndex(library, "character-scene-zombie");
		Button button2 = ((num < 0) ? null : hFlowContainer?.GetNodeOrNull<Button>($"TemplateCard_{num}"));
		Button stateCard = ((num2 < 0) ? null : hFlowContainer?.GetNodeOrNull<Button>($"TemplateCard_{num2}"));
		Button zombieCard = ((num3 < 0) ? null : hFlowContainer?.GetNodeOrNull<Button>($"TemplateCard_{num3}"));
		panel &= GodotObject.IsInstanceValid(button2) && GodotObject.IsInstanceValid(stateCard) && GodotObject.IsInstanceValid(zombieCard);
		string featurePath = Normalize(Path.Combine(_probeRoot, "Battle", "Features", "NewFeature.cs"));
		string statePath = Normalize(Path.Combine(_probeRoot, "Resources", "StateMachines", "NewStateMachine.tres"));
		if (panel)
		{
			nameEdit.Text = "";
			button2.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(1);
			chinese &= XWTemplatePresentation.HasChineseText(preview.Text);
			createButton.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(4);
			XWEditorInterface.Instance.FocusPanel("mod_tools");
			await WaitFrames(2);
			if (GodotObject.IsInstanceValid(toolTabs))
			{
				toolTabs.CurrentTab = 1;
			}
			nameEdit.Text = "";
			stateCard.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(1);
			chinese &= XWTemplatePresentation.HasChineseText(preview.Text);
			createButton.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(4);
			XWEditorInterface.Instance.FocusPanel("mod_tools");
			await WaitFrames(2);
			if (GodotObject.IsInstanceValid(toolTabs))
			{
				toolTabs.CurrentTab = 1;
			}
			nameEdit.Text = "";
			zombieCard.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(1);
			chinese &= XWTemplatePresentation.HasChineseText(preview.Text);
			createButton.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(10);
		}
		string text = (File.Exists(featurePath) ? File.ReadAllText(featurePath) : "");
		StateMachineDefinition stateMachineDefinition = (File.Exists(statePath) ? ResourceLoader.Load<StateMachineDefinition>(ProjectSettings.LocalizePath(statePath).Replace('\\', '/'), "", ResourceLoader.CacheMode.Reuse) : null);
		global::_003C_003Ey__InlineArray5<string> buffer = default(global::_003C_003Ey__InlineArray5<string>);
		buffer[0] = _probeRoot;
		buffer[1] = "Resources";
		buffer[2] = "Characters";
		buffer[3] = "Zombies";
		buffer[4] = "NewZombie";
		string path = Normalize(Path.Combine(buffer));
		string text2 = Normalize(Path.Combine(path, "Scene", "NewZombie.tscn"));
		string[] array = new string[8]
		{
			text2,
			Normalize(Path.Combine(path, "Config", "NewZombieConfig.tres")),
			Normalize(Path.Combine(path, "Sprite", "NewZombie.tscn")),
			Normalize(Path.Combine(path, "NewZombie.tres")),
			Normalize(Path.Combine(path, "Script", "NewZombie.cs")),
			Normalize(Path.Combine(path, "DamagePoint", "DamagePointData.tres")),
			Normalize(Path.Combine(path, "Custom", "CustomData.tres")),
			Normalize(Path.Combine(path, "Armor", "ArmorData.tres"))
		};
		XWModManifest manifest = XWModManifest.Load(Path.Combine(_probeRoot, "mod.json"));
		string displayNameLabel = XWTemplatePresentation.Resolve(library.FindTemplate("character-scene-zombie")).DisplayNameLabel;
		bool flag = (File.Exists(array[1]) ? File.ReadAllText(array[1]) : "").Contains("resource_name = \"" + displayNameLabel + "\"", StringComparison.Ordinal) && File.Exists(array[5]) && File.ReadAllText(array[5]).Contains("resource_name = \"角色受伤点集合\"", StringComparison.Ordinal) && File.Exists(array[6]) && File.ReadAllText(array[6]).Contains("resource_name = \"角色外观集合\"", StringComparison.Ordinal) && File.Exists(array[7]) && File.ReadAllText(array[7]).Contains("resource_name = \"角色护甲集合\"", StringComparison.Ordinal);
		PackedScene packedScene = XWEditorInterface.Instance.Get2DSceneEditor()?.CurrentPackedScene;
		bool flag2 = GodotObject.IsInstanceValid(packedScene) && SamePath(packedScene.ResourcePath, text2);
		bool flag3 = array.All(File.Exists) && flag && flag2 && array.All(delegate(string text6)
		{
			string item3 = Path.GetRelativePath(_probeRoot, text6).Replace('\\', '/');
			if (!text6.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				XWModManifest xWModManifest = manifest;
				if (xWModManifest == null)
				{
					return false;
				}
				return xWModManifest.Resources?.Contains(item3) == true;
			}
			XWModManifest xWModManifest2 = manifest;
			return xWModManifest2 != null && xWModManifest2.Scripts?.Contains(item3) == true;
		});
		if (!flag3)
		{
			GD.Print($"[MOD_EDITOR_TEMPLATE_CHARACTER_PACKAGE_DIAGNOSTIC] files={array.All(File.Exists)} chinese={flag} sceneOpened={flag2} actualScene={packedScene?.ResourcePath ?? "null"} expectedScene={text2} scripts={manifest?.Scripts?.Count ?? (-1)} resources={manifest?.Resources?.Count ?? (-1)}");
		}
		bool item = File.Exists(featurePath) && text.Contains("public partial class NewFeature : TowerDefenseBattleFeature", StringComparison.Ordinal) && GodotObject.IsInstanceValid(stateMachineDefinition) && flag3;
		string text3 = Path.GetRelativePath(_probeRoot, featurePath).Replace('\\', '/');
		string text4 = Path.GetRelativePath(_probeRoot, statePath).Replace('\\', '/');
		string text5 = (File.Exists(array[4]) ? File.ReadAllText(array[4]) : "");
		string displayNameLabel2 = XWTemplatePresentation.Resolve(library.FindTemplate("feature-csharp")).DisplayNameLabel;
		bool item2 = suggestedNamesStable && text3 == "Battle/Features/NewFeature.cs" && text4 == "Resources/StateMachines/NewStateMachine.tres" && !XWTemplatePresentation.HasChineseText(text3) && !XWTemplatePresentation.HasChineseText(text4) && text.Contains("// " + displayNameLabel2, StringComparison.Ordinal) && stateMachineDefinition?.DefinitionId == "NewStateMachine" && stateMachineDefinition?.RootStateId == "NewStateMachine.root" && text5.Contains("public partial class NewZombie : TowerDefenseZombie", StringComparison.Ordinal);
		return (Panel: panel, Chinese: chinese, Icons: icons, KeysStable: keysStable, Creation: item, StableNames: item2);
	}

	private static T FindFirstDescendant<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node child in root.GetChildren())
		{
			if (child is T result)
			{
				return result;
			}
			T val = FindFirstDescendant<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private static int FindTemplateIndex(XWTemplateLibrary library, string templateId)
	{
		for (int i = 0; i < library.Templates.Count; i++)
		{
			if (string.Equals(library.Templates[i].Id, templateId, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private async Task<(bool Compiled, bool Assignable)> AuditBattleScriptTemplatesAsync()
	{
		XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
		XWTemplateLibrary.TemplateCreateResult templateCreateResult = xWTemplateLibrary.CreateFromTemplateInDirectory("feature-csharp", Normalize(Path.Combine(_probeRoot, "Battle", "Features")), "ProbeFeature");
		XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = xWTemplateLibrary.CreateFromTemplateInDirectory("process-csharp", Normalize(Path.Combine(_probeRoot, "Battle", "Processes")), "ProbeProcess");
		if (!templateCreateResult.Success || !templateCreateResult2.Success)
		{
			_failures.Add("战斗脚本模板创建失败：Feature=" + templateCreateResult.Error + "，Process=" + templateCreateResult2.Error);
			return (Compiled: false, Assignable: false);
		}
		XWScriptCompiler.CompileResult compileResult = await XWScriptCompiler.CompileModProjectAsync(_probeRoot);
		if (compileResult == null || !compileResult.Success || !File.Exists(compileResult.OutputAssemblyPath))
		{
			string text = ((compileResult != null && compileResult.Diagnostics?.Count > 0) ? compileResult.Diagnostics[0].Message : (compileResult?.Output ?? "无编译结果"));
			_failures.Add("战斗脚本模板编译失败：" + text);
			return (Compiled: false, Assignable: false);
		}
		try
		{
			bool flag = HasCompiledBaseType(compileResult.OutputAssemblyPath, "ProbeFeature", "TowerDefenseBattleFeature");
			bool flag2 = HasCompiledBaseType(compileResult.OutputAssemblyPath, "ProbeProcess", "TowerDefenseBattleProcess");
			return (Compiled: true, Assignable: flag && flag2);
		}
		catch (Exception ex)
		{
			_failures.Add("战斗脚本程序集元数据校验失败：" + ex.GetBaseException().Message);
			return (Compiled: true, Assignable: false);
		}
	}

	private static bool HasCompiledBaseType(string assemblyPath, string typeName, string baseTypeName)
	{
		using FileStream peStream = new FileStream(assemblyPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using PEReader peReader = new PEReader(peStream);
		MetadataReader metadataReader = peReader.GetMetadataReader();
		foreach (TypeDefinitionHandle typeDefinition2 in metadataReader.TypeDefinitions)
		{
			TypeDefinition typeDefinition = metadataReader.GetTypeDefinition(typeDefinition2);
			if (string.Equals(metadataReader.GetString(typeDefinition.Name), typeName, StringComparison.Ordinal))
			{
				return string.Equals(typeDefinition.BaseType.Kind switch
				{
					HandleKind.TypeReference => metadataReader.GetString(metadataReader.GetTypeReference((TypeReferenceHandle)typeDefinition.BaseType).Name), 
					HandleKind.TypeDefinition => metadataReader.GetString(metadataReader.GetTypeDefinition((TypeDefinitionHandle)typeDefinition.BaseType).Name), 
					_ => "", 
				}, baseTypeName, StringComparison.Ordinal);
			}
		}
		return false;
	}

	private async Task<ConfirmationDialog> WaitForNameDialog(Node parent, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			ConfirmationDialog confirmationDialog = parent.FindChild("NameInputDialog", recursive: true, owned: false) as ConfirmationDialog;
			if (GodotObject.IsInstanceValid(confirmationDialog))
			{
				return confirmationDialog;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<bool> WaitForStateMachine(string expectedPath, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWStateMachineVisualResourceEditor xWStateMachineVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("state_machine_editor") as XWStateMachineVisualResourceEditor;
			StateMachineDefinition stateMachineDefinition = xWStateMachineVisualResourceEditor?.GraphSurface?.Definition;
			if (GodotObject.IsInstanceValid(stateMachineDefinition) && stateMachineDefinition.DefinitionId == "EntryStateMachine" && SamePath(xWStateMachineVisualResourceEditor.ActiveResourcePath, expectedPath))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForCard(string expectedPath, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWCardVisualResourceEditor xWCardVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("card_editor") as XWCardVisualResourceEditor;
			if (xWCardVisualResourceEditor?.ActiveResource is TowerDefensePacketConfig && SamePath(xWCardVisualResourceEditor.ActiveResourcePath, expectedPath))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static TreeItem FindTreeItemByPath(TreeItem item, string expectedPath)
	{
		if (item == null)
		{
			return null;
		}
		Variant metadata = item.GetMetadata(0);
		if (metadata.VariantType == Variant.Type.Object)
		{
			XWFileSystemTreeItemData xWFileSystemTreeItemData = metadata.As<XWFileSystemTreeItemData>();
			if (xWFileSystemTreeItemData != null && SamePath(xWFileSystemTreeItemData.Path, expectedPath))
			{
				return item;
			}
		}
		for (TreeItem treeItem = item.GetFirstChild(); treeItem != null; treeItem = treeItem.GetNext())
		{
			TreeItem treeItem2 = FindTreeItemByPath(treeItem, expectedPath);
			if (treeItem2 != null)
			{
				return treeItem2;
			}
		}
		return null;
	}

	private static int FindListItemByPath(XWFileSystemList list, string expectedPath)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return -1;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			XWFileSystemTreeItemData xWFileSystemTreeItemData = list.GetItemMetadata(i).As<XWFileSystemTreeItemData>();
			if (xWFileSystemTreeItemData != null && SamePath(xWFileSystemTreeItemData.Path, expectedPath))
			{
				return i;
			}
		}
		return -1;
	}

	private static void ExpandAncestors(TreeItem item)
	{
		for (TreeItem treeItem = item; treeItem != null; treeItem = treeItem.GetParent())
		{
			treeItem.SetCollapsed(enable: false);
		}
	}

	private static int FindMenuItem(PopupMenu menu, string text)
	{
		if (!GodotObject.IsInstanceValid(menu))
		{
			return -1;
		}
		for (int i = 0; i < menu.ItemCount; i++)
		{
			if (menu.GetItemText(i).Contains(text, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private static string Normalize(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			path = ProjectSettings.GlobalizePath(path);
		}
		return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
	}

	private static bool SamePath(string left, string right)
	{
		return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
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
			GD.PrintErr("[MOD_EDITOR_RESOURCE_CREATE_AUTO_OPEN_PROBE_FAILURE] " + message);
		}
	}

	private void Finish(bool f3, bool rightClick, bool toolbar, bool iconMenus, bool singleCreate, bool stateOpened, bool cardOpened, bool selected, bool saveReload, bool inspectorUntouched, bool animationCreated, bool animationOpened, bool characterChildren, bool scriptBases, bool blueprintBases, bool battleScriptsCompiled, bool battleTypesAssignable, bool templatePanel, bool templateChinese, bool templateIcons, bool templateKeysStable, bool templateCreation, bool stableTemplateNames)
	{
		GD.Print($"[MOD_EDITOR_RESOURCE_CREATE_AUTO_OPEN_PROBE] f3={f3} rightClick={rightClick} toolbar={toolbar} iconMenus={iconMenus} singleCreate={singleCreate} stateOpened={stateOpened} cardOpened={cardOpened} selected={selected} saveReload={saveReload} inspectorUntouched={inspectorUntouched} animationCreated={animationCreated} animationOpened={animationOpened} characterChildren={characterChildren} scriptBases={scriptBases} blueprintBases={blueprintBases} battleScriptsCompiled={battleScriptsCompiled} battleTypesAssignable={battleTypesAssignable} templatePanel={templatePanel} templateChinese={templateChinese} templateIcons={templateIcons} templateKeysStable={templateKeysStable} templateCreation={templateCreation} stableTemplateNames={stableTemplateNames} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_RESOURCE_CREATE_AUTO_OPEN_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
