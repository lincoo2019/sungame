using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWTextFlowVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string GuiSceneEmbeddedPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGuiSceneEmbeddedPreview.tscn";

	private const string NpcTalkInlineEditorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkInlineEditor.tscn";

	private const string NpcTalkPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkPreviewWindow.tscn";

	private const string RuntimePreviewCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTextFlowRuntimePreviewCard.tscn";

	private const string TutorialInlineEditorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTutorialInlineEditor.tscn";

	private const string TutorialPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTutorialPreviewWindow.tscn";

	private static PackedScene _guiSceneEmbeddedPreviewScene;

	private static PackedScene _npcTalkInlineEditorScene;

	private static PackedScene _npcTalkPreviewScene;

	private static PackedScene _runtimePreviewCardScene;

	private static PackedScene _tutorialInlineEditorScene;

	private static PackedScene _tutorialPreviewScene;

	private XWNpcTalkPreviewWindow _npcTalkPreviewWindow;

	private XWNpcTalkInlineEditor _npcTalkInlineEditor;

	private NpcTalkConfig _npcTalkRootAdapter;

	private Button _npcTalkRuntimePreviewButton;

	private XWTutorialInlineEditor _tutorialInlineEditor;

	private TutorialConfig _tutorialRootAdapter;

	private XWTutorialPreviewWindow _tutorialPreviewWindow;

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_npcTalkPreviewWindow))
		{
			_npcTalkPreviewWindow.QueueFree();
		}
		if (GodotObject.IsInstanceValid(_tutorialPreviewWindow))
		{
			_tutorialPreviewWindow.QueueFree();
		}
		_npcTalkPreviewWindow = null;
		_npcTalkRuntimePreviewButton = null;
		_npcTalkRootAdapter = null;
		_tutorialInlineEditor = null;
		_tutorialRootAdapter = null;
		_tutorialPreviewWindow = null;
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		Resource currentResource = base.CurrentResource;
		if (!GodotObject.IsInstanceValid(currentResource))
		{
			if (GodotObject.IsInstanceValid(_npcTalkPreviewWindow))
			{
				_npcTalkPreviewWindow.QueueFree();
			}
			if (GodotObject.IsInstanceValid(_tutorialPreviewWindow))
			{
				_tutorialPreviewWindow.QueueFree();
			}
			_npcTalkPreviewWindow = null;
			_npcTalkInlineEditor = null;
			_npcTalkRootAdapter = null;
			_tutorialInlineEditor = null;
			_tutorialRootAdapter = null;
			_tutorialPreviewWindow = null;
			return;
		}
		RenderLocalizationHints(currentResource);
		if (!(currentResource is TutorialConfig tutorialConfig))
		{
			if (!(currentResource is TutorialStepConfig step))
			{
				if (!(currentResource is TutorialConditionConfig condition))
				{
					if (!(currentResource is NpcTalkConfig npcTalkConfig))
					{
						if (!(currentResource is NpcTalkBaseConfig talk))
						{
							if (!(currentResource is ShopConfig shop))
							{
								if (currentResource is PackedScene dialogScene)
								{
									RenderDialogPreview(dialogScene);
								}
								else
								{
									RenderGenericTextFlow(currentResource);
								}
							}
							else
							{
								RenderShopGrid(shop);
							}
						}
						else
						{
							RenderNpcTalkEntry(talk);
						}
					}
					else
					{
						_npcTalkRootAdapter = null;
						RenderNpcTalkFlow(npcTalkConfig, npcTalkConfig);
					}
				}
				else
				{
					RenderTutorialCondition(condition);
				}
			}
			else
			{
				RenderTutorialStep(step);
			}
		}
		else
		{
			_tutorialRootAdapter = null;
			RenderTutorialFlow(tutorialConfig, tutorialConfig);
		}
	}

	private void RenderNpcTalkEntry(NpcTalkBaseConfig talk)
	{
		_npcTalkRootAdapter = new NpcTalkConfig
		{
			saveKey = (string.IsNullOrWhiteSpace(talk.ResourceName) ? ("单条对话 · " + talk.GetType().Name) : talk.ResourceName)
		};
		_npcTalkRootAdapter.talkList.Add(talk);
		RenderNpcTalkFlow(_npcTalkRootAdapter, talk);
	}

	private void RenderTutorialStep(TutorialStepConfig step)
	{
		_tutorialRootAdapter = CreateTutorialPreviewAdapter(base.CurrentResource);
		RenderTutorialFlow(_tutorialRootAdapter, step);
	}

	private void RenderTutorialCondition(TutorialConditionConfig condition)
	{
		_tutorialRootAdapter = CreateTutorialPreviewAdapter(base.CurrentResource);
		RenderTutorialFlow(_tutorialRootAdapter, condition);
	}

	private TutorialConfig CreateTutorialPreviewAdapter(Resource resource)
	{
		if (!(resource is TutorialConfig result))
		{
			if (!(resource is TutorialStepConfig step))
			{
				if (resource is TutorialConditionConfig condition)
				{
					return CreateTutorialConditionAdapter(condition);
				}
				return null;
			}
			return CreateTutorialStepAdapter(step);
		}
		return result;
	}

	private static TutorialConfig CreateTutorialStepAdapter(TutorialStepConfig step)
	{
		return new TutorialConfig
		{
			saveKey = (string.IsNullOrWhiteSpace(step.ResourceName) ? "独立教程步骤" : step.ResourceName),
			step = { step }
		};
	}

	private static TutorialConfig CreateTutorialConditionAdapter(TutorialConditionConfig condition)
	{
		TutorialStepConfig tutorialStepConfig = new TutorialStepConfig();
		tutorialStepConfig.conditionList.Add(condition);
		return new TutorialConfig
		{
			saveKey = (string.IsNullOrWhiteSpace(condition.ResourceName) ? ("独立教程条件 · " + condition.GetType().Name) : condition.ResourceName),
			step = { tutorialStepConfig }
		};
	}

	private void RenderTutorialFlow(TutorialConfig tutorial, Resource editingRoot)
	{
		AddPreviewRow("教程 key: " + FormatTextValue(tutorial.saveKey));
		AddTimelineRow($"步骤数量: {tutorial.step?.Count ?? 0}");
		AddGraphRow("saveKey -> " + FormatTextValue(tutorial.saveKey));
		PrepareCanvas(1);
		RenderTutorialInlineEditor(tutorial, editingRoot);
		AddTutorialRuntimePreviewCard(tutorial);
		if (GodotObject.IsInstanceValid(_tutorialPreviewWindow))
		{
			_tutorialPreviewWindow.RefreshConfig(tutorial);
		}
		if (tutorial.step == null || tutorial.step.Count == 0)
		{
			AddFlowCard("教程步骤", "尚未添加步骤。");
			return;
		}
		for (int i = 0; i < tutorial.step.Count; i++)
		{
			TutorialStepConfig tutorialStepConfig = tutorial.step[i];
			if (!GodotObject.IsInstanceValid(tutorialStepConfig))
			{
				AddFlowCard($"步骤 {i + 1}", "空步骤");
				continue;
			}
			string value = "无广播";
			if (tutorialStepConfig.broadCastUse && GodotObject.IsInstanceValid(tutorialStepConfig.broadCastConfig))
			{
				value = $"{tutorialStepConfig.broadCastConfig.broadCastString}\n持续: {tutorialStepConfig.broadCastConfig.broadCastTime:0.##}s";
				AddReferenceRow("多语言文本 -> " + tutorialStepConfig.broadCastConfig.broadCastString);
			}
			int value2 = tutorialStepConfig.conditionList?.Count ?? 0;
			AddFlowCard($"步骤 {i + 1}", $"{value}\n条件数: {value2}");
			AddTimelineRow($"步骤 {i + 1}: broadCastConfig={tutorialStepConfig.broadCastUse}, conditionList={value2}");
			AddGraphRow($"步骤 {i + 1} -> conditionList({value2})");
			if (tutorialStepConfig.conditionList == null)
			{
				continue;
			}
			for (int j = 0; j < tutorialStepConfig.conditionList.Count; j++)
			{
				TutorialConditionConfig tutorialConditionConfig = tutorialStepConfig.conditionList[j];
				if (GodotObject.IsInstanceValid(tutorialConditionConfig))
				{
					AddGraphRow($"步骤 {i + 1}.条件 {j + 1} -> {tutorialConditionConfig.GetType().Name}");
					AddReferenceRow("教程条件 -> " + tutorialConditionConfig.GetType().Name);
				}
			}
		}
	}

	private void RenderTutorialInlineEditor(TutorialConfig tutorial, Resource editingRoot)
	{
		if (base.CanvasGrid != null)
		{
			if (_tutorialInlineEditorScene == null)
			{
				_tutorialInlineEditorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTutorialInlineEditor.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_tutorialInlineEditor = _tutorialInlineEditorScene?.Instantiate<XWTutorialInlineEditor>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_tutorialInlineEditor))
			{
				base.CanvasGrid.AddChild(_tutorialInlineEditor, forceReadableName: false, InternalMode.Disabled);
				_tutorialInlineEditor.Connect(XWTutorialInlineEditor.SignalName.TutorialEdited, new Callable(this, "OnTutorialInlineEdited"));
				_tutorialInlineEditor.EditConfig(tutorial, editingRoot);
			}
		}
	}

	private void OnTutorialInlineEdited()
	{
		TutorialConfig tutorialConfig = _tutorialRootAdapter ?? (base.CurrentResource as TutorialConfig);
		if (GodotObject.IsInstanceValid(_tutorialPreviewWindow) && GodotObject.IsInstanceValid(tutorialConfig))
		{
			_tutorialPreviewWindow.RefreshConfig(tutorialConfig);
		}
		NotifyCurrentResourceEdited();
	}

	private void AddTutorialRuntimePreviewCard(TutorialConfig tutorial)
	{
		if (base.CanvasGrid != null)
		{
			PanelContainer panelContainer = CreateRuntimePreviewCard("教程流程运行预览", "模拟游戏广播栏、步骤条件、手动推进和定时自动播放。", $"▶ 运行教程预览（{tutorial.step?.Count ?? 0} 步）", tutorial.step == null || tutorial.step.Count == 0, delegate
			{
				OpenTutorialRuntimePreview(tutorial);
			});
			if (GodotObject.IsInstanceValid(panelContainer))
			{
				base.CanvasGrid.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void OpenTutorialRuntimePreview(TutorialConfig tutorial)
	{
		if (!GodotObject.IsInstanceValid(_tutorialPreviewWindow))
		{
			if (_tutorialPreviewScene == null)
			{
				_tutorialPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTutorialPreviewWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_tutorialPreviewWindow = _tutorialPreviewScene?.Instantiate<XWTutorialPreviewWindow>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_tutorialPreviewWindow))
			{
				return;
			}
			AddChild(_tutorialPreviewWindow, forceReadableName: false, InternalMode.Disabled);
		}
		_tutorialPreviewWindow.Preview(tutorial);
	}

	private void RenderNpcTalkFlow(NpcTalkConfig npcTalk, Resource editingRoot)
	{
		AddPreviewRow("对话 key: " + FormatTextValue(npcTalk.saveKey));
		AddTimelineRow($"对话条目: {npcTalk.talkList?.Count ?? 0}");
		AddGraphRow("saveKey -> " + FormatTextValue(npcTalk.saveKey));
		PrepareCanvas(1);
		RenderNpcTalkInlineEditor(npcTalk, editingRoot);
		AddNpcTalkRuntimePreviewCard(npcTalk);
		if (GodotObject.IsInstanceValid(_npcTalkPreviewWindow))
		{
			_npcTalkPreviewWindow.RefreshConfig(npcTalk);
		}
		if (npcTalk.talkList == null)
		{
			return;
		}
		for (int i = 0; i < npcTalk.talkList.Count; i++)
		{
			NpcTalkBaseConfig npcTalkBaseConfig = npcTalk.talkList[i];
			if (!GodotObject.IsInstanceValid(npcTalkBaseConfig))
			{
				continue;
			}
			AddTimelineRow($"对话 {i + 1}: npc={FormatTextValue(npcTalkBaseConfig.npc)}, text={TrimLine(npcTalkBaseConfig.text)}");
			AddGraphRow(FormatTextValue(npcTalkBaseConfig.npc) + " -> text / anime / audio");
			AddReferenceRow("角色引用 npc -> " + FormatTextValue(npcTalkBaseConfig.npc));
			AddReferenceRow("动画引用 anime -> " + FormatTextValue(npcTalkBaseConfig.anime));
			AddReferenceRow("音频引用 audio -> " + FormatTextValue(npcTalkBaseConfig.audio));
			AddReferenceRow("多语言文本 -> " + TrimLine(npcTalkBaseConfig.text));
			if (!(npcTalkBaseConfig is NpcTalkTutorialConfig npcTalkTutorialConfig))
			{
				if (npcTalkBaseConfig is NpcTalkHandConfig npcTalkHandConfig)
				{
					AddGraphRow($"对话 {i + 1} -> handScene / shoulderScene / shoulder2Scene / headScene");
					AddReferenceRow("手部场景 -> " + FormatResource(npcTalkHandConfig.handScene));
					AddReferenceRow("肩部场景 -> " + FormatResource(npcTalkHandConfig.shoulderScene));
					AddReferenceRow("肩部场景2 -> " + FormatResource(npcTalkHandConfig.shoulder2Scene));
					AddReferenceRow("头部场景 -> " + FormatResource(npcTalkHandConfig.headScene));
				}
			}
			else
			{
				AddGraphRow($"对话 {i + 1} -> tutorial {FormatResource(npcTalkTutorialConfig.tutorial)}");
				AddReferenceRow("教程引用 -> " + FormatResource(npcTalkTutorialConfig.tutorial));
			}
		}
	}

	private void RenderNpcTalkInlineEditor(NpcTalkConfig npcTalk, Resource editingRoot)
	{
		if (base.CanvasGrid != null)
		{
			if (_npcTalkInlineEditorScene == null)
			{
				_npcTalkInlineEditorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkInlineEditor.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_npcTalkInlineEditor = _npcTalkInlineEditorScene?.Instantiate<XWNpcTalkInlineEditor>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(_npcTalkInlineEditor))
			{
				base.CanvasGrid.AddChild(_npcTalkInlineEditor, forceReadableName: false, InternalMode.Disabled);
				_npcTalkInlineEditor.Connect(XWNpcTalkInlineEditor.SignalName.TalkEdited, new Callable(this, "OnNpcTalkInlineEdited"));
				_npcTalkInlineEditor.EditConfig(npcTalk, editingRoot);
			}
		}
	}

	private void OnNpcTalkInlineEdited()
	{
		NpcTalkConfig npcTalkConfig = _npcTalkRootAdapter ?? (base.CurrentResource as NpcTalkConfig);
		if (GodotObject.IsInstanceValid(_npcTalkPreviewWindow) && GodotObject.IsInstanceValid(npcTalkConfig))
		{
			_npcTalkPreviewWindow.RefreshConfig(npcTalkConfig);
		}
		RefreshNpcTalkRuntimePreviewButton(npcTalkConfig);
		NotifyCurrentResourceEdited();
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!(resource is NpcTalkConfig) && !(resource is NpcTalkBaseConfig) && !IsTutorialAuthoringResource(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	protected override bool ShouldBindInlineTextSurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!IsTutorialAuthoringResource(resource))
		{
			return base.ShouldBindInlineTextSurface(resource, path, descriptor);
		}
		return false;
	}

	protected override bool ShouldBindDirectPropertySurface(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!IsTutorialAuthoringResource(resource))
		{
			return base.ShouldBindDirectPropertySurface(resource, path, descriptor);
		}
		return false;
	}

	private static bool IsTutorialAuthoringResource(Resource resource)
	{
		if (resource is TutorialConfig || resource is TutorialStepConfig || resource is TutorialConditionConfig)
		{
			return true;
		}
		return false;
	}

	private void AddNpcTalkRuntimePreviewCard(NpcTalkConfig npcTalk)
	{
		if (base.CanvasGrid != null)
		{
			PanelContainer panelContainer = CreateRuntimePreviewCard("真实 NPC 对话预览", "打开独立预览窗口，按游戏实际效果播放 NPC、气泡文本、动画、手持物和音频。", $"▶ 运行对话预览（{npcTalk.talkList?.Count ?? 0} 条）", npcTalk.talkList == null || npcTalk.talkList.Count == 0, delegate
			{
				OpenNpcTalkRuntimePreview(npcTalk);
			});
			if (GodotObject.IsInstanceValid(panelContainer))
			{
				base.CanvasGrid.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
				_npcTalkRuntimePreviewButton = panelContainer.GetNode<Button>("%RunButton");
				RefreshNpcTalkRuntimePreviewButton(npcTalk);
			}
		}
	}

	private void RefreshNpcTalkRuntimePreviewButton(NpcTalkConfig npcTalk)
	{
		if (GodotObject.IsInstanceValid(_npcTalkRuntimePreviewButton))
		{
			int valueOrDefault = (npcTalk?.talkList?.Count).GetValueOrDefault();
			_npcTalkRuntimePreviewButton.Disabled = valueOrDefault == 0;
			_npcTalkRuntimePreviewButton.Text = $"▶ 运行对话预览（{valueOrDefault} 条）";
		}
	}

	private PanelContainer CreateRuntimePreviewCard(string title, string description, string buttonText, bool disabled, Action pressed)
	{
		if (_runtimePreviewCardScene == null)
		{
			_runtimePreviewCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTextFlowRuntimePreviewCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		PanelContainer panelContainer = _runtimePreviewCardScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(panelContainer))
		{
			return null;
		}
		panelContainer.GetNode<Label>("%Title").Text = title;
		panelContainer.GetNode<Label>("%Description").Text = description;
		Button node = panelContainer.GetNode<Button>("%RunButton");
		node.Text = buttonText;
		node.Disabled = disabled;
		node.Pressed += delegate
		{
			pressed?.Invoke();
		};
		return panelContainer;
	}

	private void OpenNpcTalkRuntimePreview(NpcTalkConfig npcTalk)
	{
		if (!GodotObject.IsInstanceValid(_npcTalkPreviewWindow))
		{
			if (_npcTalkPreviewScene == null)
			{
				_npcTalkPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWNpcTalkPreviewWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_npcTalkPreviewWindow = _npcTalkPreviewScene?.Instantiate<XWNpcTalkPreviewWindow>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_npcTalkPreviewWindow))
			{
				return;
			}
			AddChild(_npcTalkPreviewWindow, forceReadableName: false, InternalMode.Disabled);
		}
		_npcTalkPreviewWindow.Preview(npcTalk);
	}

	private void RenderShopGrid(ShopConfig shop)
	{
		AddPreviewRow($"商店页数: {shop.pageList?.Count ?? 0}");
		AddTimelineRow("商店流程: 页面 -> 商品 -> 阶段");
		AddGraphRow("pageList -> itemList -> stageList");
		PrepareCanvas(3);
		if (shop.pageList == null || shop.pageList.Count == 0)
		{
			AddFlowCard("商店", "尚未添加商品页。");
			return;
		}
		for (int i = 0; i < shop.pageList.Count; i++)
		{
			ShopPageConfig shopPageConfig = shop.pageList[i];
			int valueOrDefault = (shopPageConfig?.itemList?.Count).GetValueOrDefault();
			AddTimelineRow($"页面 {i + 1}: itemList={valueOrDefault}");
			AddGraphRow($"页面 {i + 1} -> itemList({valueOrDefault})");
			if (!GodotObject.IsInstanceValid(shopPageConfig) || shopPageConfig.itemList == null)
			{
				continue;
			}
			for (int j = 0; j < shopPageConfig.itemList.Count; j++)
			{
				ShopItemConfig shopItemConfig = shopPageConfig.itemList[j];
				int valueOrDefault2 = (shopItemConfig?.stageList?.Count).GetValueOrDefault();
				AddGraphRow($"页面 {i + 1}.商品 {j + 1} -> type={FormatTextValue(shopItemConfig?.type)} stageList({valueOrDefault2})");
				if (!GodotObject.IsInstanceValid(shopItemConfig) || shopItemConfig.stageList == null)
				{
					continue;
				}
				for (int k = 0; k < shopItemConfig.stageList.Count; k++)
				{
					ShopItemStageConfig shopItemStageConfig = shopItemConfig.stageList[k];
					if (GodotObject.IsInstanceValid(shopItemStageConfig))
					{
						string text = $"P{i + 1}-{j + 1}-{k + 1}: {FormatTextValue(shopItemStageConfig.saveKey)}";
						string body = $"类型: {FormatTextValue(shopItemStageConfig.saveType)}\n价格: {shopItemStageConfig.cost} {FormatTextValue(shopItemStageConfig.type)}\n库存: {shopItemStageConfig.openMinNum}-{shopItemStageConfig.openMaxNum} +{shopItemStageConfig.addNum}\n说明: {TrimLine(shopItemStageConfig.describe)}";
						AddFlowCard(text, body, shopItemStageConfig.texture);
						AddTimelineRow($"{text}: cost={shopItemStageConfig.cost}, npcTalk={FormatTextValue(shopItemStageConfig.npcTalk)}");
						AddReferenceRow("商品 key -> " + FormatTextValue(shopItemStageConfig.saveKey));
						AddReferenceRow("商品贴图 -> " + FormatResource(shopItemStageConfig.texture));
						AddReferenceRow("NPC 对话 npcTalk -> " + FormatTextValue(shopItemStageConfig.npcTalk));
						AddReferenceRow("多语言说明 -> " + TrimLine(shopItemStageConfig.describe));
					}
				}
			}
		}
	}

	private void RenderDialogPreview(PackedScene dialogScene)
	{
		PrepareCanvas(1);
		AddPreviewRow("GUI 场景: " + FormatResource(dialogScene));
		AddGraphRow("PackedScene -> " + FormatResource(dialogScene));
		AddReferenceRow("GUI 场景 -> " + FormatResource(dialogScene));
		if (_guiSceneEmbeddedPreviewScene == null)
		{
			_guiSceneEmbeddedPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGuiSceneEmbeddedPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		PanelContainer panelContainer = _guiSceneEmbeddedPreviewScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(panelContainer))
		{
			return;
		}
		base.CanvasGrid?.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		Control node = panelContainer.GetNode<Control>("%PreviewRoot");
		Label node2 = panelContainer.GetNode<Label>("%ErrorLabel");
		try
		{
			Node node3 = dialogScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node3 is Control control)
			{
				control.ProcessMode = ProcessModeEnum.Inherit;
				control.MouseFilter = MouseFilterEnum.Ignore;
				node.AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
				AddGraphRow("根节点 -> " + control.GetType().Name);
			}
			else
			{
				node3?.QueueFree();
				node2.Text = "该场景根节点不是 Control，无法作为 UI 预览嵌入。";
				node2.Show();
			}
		}
		catch (Exception ex)
		{
			node2.Text = "场景预览失败: " + ex.Message;
			node2.Show();
		}
	}

	private void RenderGenericTextFlow(Resource resource)
	{
		PrepareCanvas(2);
		AddFlowCard(resource.GetType().Name, "当前资源暂未识别为教程、NPC、商店或 GUI，已按文本字段生成预览。");
		foreach (var item in ExtractTextProperties(resource))
		{
			AddPreviewRow(item.Name + ": " + item.Value);
			AddReferenceRow("文本字段 " + item.Name + " -> " + item.Value);
		}
	}

	private void RenderLocalizationHints(Resource resource)
	{
		foreach (var item in ExtractTextProperties(resource))
		{
			if (IsLocalizationLike(item.Name))
			{
				AddPreviewRow("多语言字段: " + item.Name);
				AddReferenceRow("Localization / 多语言 -> " + item.Value);
			}
		}
	}

	private List<(string Name, string Value)> ExtractTextProperties(Resource resource)
	{
		List<(string, string)> list = new List<(string, string)>();
		if (!GodotObject.IsInstanceValid(resource))
		{
			return list;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string text = ReadPropertyName(property);
			if (!string.IsNullOrWhiteSpace(text) && IsTextLike(text))
			{
				Variant value;
				try
				{
					value = resource.Get(text);
				}
				catch
				{
					continue;
				}
				list.Add((text, FormatVariant(value)));
			}
		}
		return list;
	}

	private void PrepareCanvas(int columns)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = Math.Max(1, columns);
		}
	}

	private void AddFlowCard(string title, string body, Texture2D texture = null)
	{
		if (base.CanvasGrid != null)
		{
			PanelContainer panelContainer = new PanelContainer
			{
				CustomMinimumSize = new Vector2(250f, 140f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			base.CanvasGrid.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			vBoxContainer.AddChild(new Label
			{
				Text = title,
				ClipText = true
			}, forceReadableName: false, InternalMode.Disabled);
			if (GodotObject.IsInstanceValid(texture))
			{
				vBoxContainer.AddChild(new TextureRect
				{
					Texture = texture,
					CustomMinimumSize = new Vector2(96f, 64f),
					ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
					StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				}, forceReadableName: false, InternalMode.Disabled);
			}
			vBoxContainer.AddChild(new Label
			{
				Text = body,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void AddTimelineRow(string text)
	{
		AddItemIfMissing(base.TimelineList, text);
	}

	private void AddPreviewRow(string text)
	{
		AddItemIfMissing(base.PreviewList, text);
	}

	private void AddGraphRow(string text)
	{
		AddItemIfMissing(base.GraphList, text);
	}

	private void AddReferenceRow(string text)
	{
		AddItemIfMissing(base.ReferenceList, text);
	}

	private static void AddItemIfMissing(ItemList list, string text)
	{
		if (list == null || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			if (list.GetItemText(i) == text)
			{
				return;
			}
		}
		list.AddItem(text);
	}

	private static string ReadPropertyName(Dictionary property)
	{
		if (property == null || !property.ContainsKey("name"))
		{
			return "";
		}
		return property["name"].AsString();
	}

	private static bool IsTextLike(string name)
	{
		string text = name.ToLowerInvariant();
		if (!text.Contains("text") && !text.Contains("describe") && !text.Contains("description") && !text.Contains("translate") && !text.Contains("localization") && !text.Contains("npc") && !text.Contains("talk") && !text.Contains("savekey") && !text.Contains("key") && !text.Contains("title") && !text.Contains("broadcast") && !text.Contains("audio"))
		{
			return text.Contains("anime");
		}
		return true;
	}

	private static bool IsLocalizationLike(string name)
	{
		string text = name.ToLowerInvariant();
		if (!text.Contains("text") && !text.Contains("describe") && !text.Contains("translate") && !text.Contains("localization") && !text.Contains("title"))
		{
			return text.Contains("broadcast");
		}
		return true;
	}

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return "空";
			case 4:
				goto IL_006b;
			case 1:
				return value.AsBool() ? "启用" : "关闭";
			case 2:
				return value.AsInt64().ToString();
			case 3:
				return value.AsDouble().ToString("0.###");
			}
		}
		if (variantType != Variant.Type.StringName)
		{
			Variant.Type num = variantType - 24;
			if ((ulong)num <= 4uL)
			{
				switch ((int)num)
				{
				case 0:
					return FormatResource(value.AsGodotObject() as Resource);
				case 4:
					return $"数组({value.AsGodotArray().Count})";
				case 3:
					return $"字典({value.AsGodotDictionary().Count})";
				}
			}
			return value.ToString();
		}
		goto IL_006b;
		IL_006b:
		return FormatTextValue(value.AsString());
	}

	private static string FormatResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未设置";
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return resource.ResourcePath;
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourceName))
		{
			return resource.ResourceName;
		}
		return resource.GetType().Name;
	}

	private static string FormatTextValue(string text)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "未设置";
	}

	private static string TrimLine(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "未设置";
		}
		string text2 = text.Replace("\r", " ").Replace("\n", " ").Trim();
		if (text2.Length > 96)
		{
			return text2.Substring(0, 96) + "...";
		}
		return text2;
	}
}
