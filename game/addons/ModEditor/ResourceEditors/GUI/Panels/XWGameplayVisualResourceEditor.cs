using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWGameplayVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string SurvivalPreviewCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalRuntimePreviewCard.tscn";

	private const string SurvivalPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalPreviewWindow.tscn";

	private const string SurvivalConfigEditorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalConfigEditorLayout.tscn";

	private const string SurvivalRoundAddEditorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalRoundAddEditorLayout.tscn";

	private static PackedScene _survivalPreviewCardScene;

	private static PackedScene _survivalPreviewScene;

	private static PackedScene _survivalConfigEditorScene;

	private static PackedScene _survivalRoundAddEditorScene;

	private readonly XWGameplayLogicPreviewSafety _survivalPreviewSafety = new XWGameplayLogicPreviewSafety();

	private XWSurvivalPreviewWindow _survivalPreviewWindow;

	private XWGameplayResourcePickerWindow _survivalZombiePicker;

	private XWVisualPropertyBinding _survivalConfigBinding;

	private TowerDefenseLevelSurvivalConfig _editingSurvivalConfig;

	private readonly System.Collections.Generic.Dictionary<string, SpinBox> _survivalConfigNumberControls = new System.Collections.Generic.Dictionary<string, SpinBox>();

	private CheckBox _survivalConfigEndlessCheck;

	private CheckBox _survivalConfigDayNightCheck;

	private Button _survivalConfigJsonButton;

	private FileDialog _survivalConfigJsonDialog;

	private ItemList _survivalBaseZombieList;

	private ItemList _survivalRoundAddList;

	private Button _survivalBaseRemoveButton;

	private Button _survivalBaseMoveUpButton;

	private Button _survivalBaseMoveDownButton;

	private Button _survivalRoundAddOpenButton;

	private Button _survivalRoundAddRemoveButton;

	private Button _survivalRoundAddMoveUpButton;

	private Button _survivalRoundAddMoveDownButton;

	private Node2D _survivalConfigZombieRoot;

	private TextureRect _survivalConfigBackground;

	private Label _survivalConfigRoundLabel;

	private Label _survivalConfigTimeLabel;

	private Label _survivalConfigPoolStatus;

	private Label _survivalConfigPointLabel;

	private Label _survivalConfigStatus;

	private ProgressBar _survivalConfigPointProgress;

	private SpinBox _survivalPreviewRoundSpin;

	private SpinBox _survivalPreviewWaveSpin;

	private CheckBox _survivalPreviewBigWaveCheck;

	private int _selectedSurvivalBaseZombieIndex = -1;

	private int _selectedSurvivalRoundAddIndex = -1;

	private int _lastFiniteSurvivalRoundLimit = 10;

	private XWVisualPropertyBinding _survivalRoundBinding;

	private TowerDefenseLevelSurvivalZombiePoolRoundAddConfig _editingSurvivalRoundAdd;

	private SpinBox _survivalRoundSpinBox;

	private ItemList _survivalRoundZombieList;

	private Node2D _survivalRoundZombieRoot;

	private Label _survivalRoundTitle;

	private Label _survivalRoundStatus;

	private Button _survivalRoundRemoveButton;

	private Button _survivalRoundMoveUpButton;

	private Button _survivalRoundMoveDownButton;

	private int _selectedSurvivalZombieIndex = -1;

	public override void _ExitTree()
	{
		_survivalPreviewSafety.Dispose();
		_survivalConfigBinding?.Dispose();
		_survivalConfigBinding = null;
		_survivalRoundBinding?.Dispose();
		_survivalRoundBinding = null;
		if (GodotObject.IsInstanceValid(_survivalPreviewWindow))
		{
			_survivalPreviewWindow.QueueFree();
		}
		_survivalPreviewWindow = null;
		if (GodotObject.IsInstanceValid(_survivalZombiePicker))
		{
			_survivalZombiePicker.QueueFree();
		}
		_survivalZombiePicker = null;
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		if (GodotObject.IsInstanceValid(_survivalZombiePicker))
		{
			_survivalZombiePicker.Dismiss();
		}
		_survivalConfigBinding?.Dispose();
		_survivalConfigBinding = null;
		_editingSurvivalConfig = null;
		_survivalConfigNumberControls.Clear();
		_survivalConfigEndlessCheck = null;
		_survivalConfigDayNightCheck = null;
		_survivalConfigJsonButton = null;
		_survivalConfigJsonDialog = null;
		_survivalBaseZombieList = null;
		_survivalRoundAddList = null;
		_survivalBaseRemoveButton = null;
		_survivalBaseMoveUpButton = null;
		_survivalBaseMoveDownButton = null;
		_survivalRoundAddOpenButton = null;
		_survivalRoundAddRemoveButton = null;
		_survivalRoundAddMoveUpButton = null;
		_survivalRoundAddMoveDownButton = null;
		_survivalConfigZombieRoot = null;
		_survivalConfigBackground = null;
		_survivalConfigRoundLabel = null;
		_survivalConfigTimeLabel = null;
		_survivalConfigPoolStatus = null;
		_survivalConfigPointLabel = null;
		_survivalConfigStatus = null;
		_survivalConfigPointProgress = null;
		_survivalPreviewRoundSpin = null;
		_survivalPreviewWaveSpin = null;
		_survivalPreviewBigWaveCheck = null;
		_selectedSurvivalBaseZombieIndex = -1;
		_selectedSurvivalRoundAddIndex = -1;
		_survivalRoundBinding?.Dispose();
		_survivalRoundBinding = null;
		_editingSurvivalRoundAdd = null;
		_survivalRoundSpinBox = null;
		_survivalRoundZombieList = null;
		_survivalRoundZombieRoot = null;
		_survivalRoundTitle = null;
		_survivalRoundStatus = null;
		_survivalRoundRemoveButton = null;
		_survivalRoundMoveUpButton = null;
		_survivalRoundMoveDownButton = null;
		_selectedSurvivalZombieIndex = -1;
		Resource currentResource = base.CurrentResource;
		if (!GodotObject.IsInstanceValid(currentResource))
		{
			return;
		}
		string text = base.CurrentDescriptor?.Category ?? "";
		RenderResourceReferences(currentResource);
		if (currentResource is TowerDefenseLevelSurvivalConfig survival)
		{
			RenderSurvivalConfigEditor(survival);
			return;
		}
		if (currentResource is TowerDefenseLevelSurvivalZombiePoolRoundAddConfig roundAdd)
		{
			RenderSurvivalRoundAddEditor(roundAdd);
			return;
		}
		switch (text)
		{
		case "Level":
			RenderLevelGrid(currentResource);
			break;
		case "Map":
			RenderMapGrid(currentResource);
			break;
		case "Character":
			RenderCharacterPreview(currentResource);
			break;
		case "Card":
			RenderCardPreview(currentResource);
			break;
		case "PacketBank":
			RenderPacketBankGrid(currentResource);
			break;
		case "Projectile":
		case "ProjectileChange":
			RenderProjectileChain(currentResource);
			break;
		case "Mower":
		case "Collectable":
		case "Shovel":
		case "Survival":
		case "FallingObject":
			RenderUtilityConfig(currentResource, text);
			break;
		default:
			RenderUtilityConfig(currentResource, text);
			break;
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!(resource is TowerDefenseLevelSurvivalConfig) && !(resource is TowerDefenseLevelSurvivalZombiePoolRoundAddConfig))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private void RenderLevelGrid(Resource resource)
	{
		PrepareCanvas(3);
		AddFlowCard("关卡资源", BuildKnownPropertyBlock(resource, "name", "levelName", "description", "backgroundMusic"));
		AddFlowCard("地图绑定", BuildKnownPropertyBlock(resource, "map", "mapConfig", "mapName", "gridNum"));
		AddFlowCard("卡牌栏", BuildKnownPropertyBlock(resource, "packetList", "packetBank", "packetBankName", "cardList"));
		RenderFeatureProcessSlots(resource);
		RenderLevelIntegratedScenarioResources(resource);
		RenderLevelEventResources(resource);
		AddTimelineRow("关卡 key: " + ReadKnown(resource, "name", "saveKey", "levelName"));
		AddTimelineRow("下一关: " + ReadKnown(resource, "nextLevel", "nextLevelName"));
		AddGraphRow("关卡 -> 地图 -> 卡牌栏 -> feature/process -> 波次");
	}

	private void RenderMapGrid(Resource resource)
	{
		Vector2I vector2I = ReadVector2I(resource, "gridNum", new Vector2I(9, 5));
		Vector2 value = ReadVector2(resource, "gridBeginPos", Vector2.Zero);
		Vector2 value2 = ReadVector2(resource, "gridSize", new Vector2(80f, 100f));
		Texture2D texture2D = TryReadTexture(resource, "mapTexture", "texture", "background", "backgroundTexture");
		PrepareCanvas(Math.Clamp(vector2I.X, 1, 14));
		AddPreviewRow($"地图格子: {vector2I.X} x {vector2I.Y}");
		AddPreviewRow("gridBeginPos: " + FormatVector(value));
		AddPreviewRow("gridSize: " + FormatVector(value2));
		AddReferenceRow("mapTexture -> " + FormatResource(texture2D));
		if (GodotObject.IsInstanceValid(texture2D))
		{
			AddFlowCard("背景图", "mapTexture", texture2D);
		}
		int num = Math.Clamp(vector2I.X * vector2I.Y, 1, 140);
		for (int i = 0; i < num; i++)
		{
			int value3 = i % Math.Max(1, vector2I.X) + 1;
			int value4 = i / Math.Max(1, vector2I.X) + 1;
			AddGridCell($"{value3},{value4}", new Color(0.16f, 0.32f, 0.22f, 0.9f), $"格子 {value3},{value4}");
		}
		AddGraphRow("地图 -> 背景图 / 格子 / 边界 / 可种植类型");
		AddTimelineRow("画布偏移: gridBeginPos=" + FormatVector(value) + ", gridSize=" + FormatVector(value2));
	}

	private void RenderCharacterPreview(Resource resource)
	{
		PrepareCanvas(2);
		Texture2D texture = TryReadTexture(resource, "spriteOverride", "texture", "previewTexture", "image", "icon");
		AddFlowCard("角色预览", BuildKnownPropertyBlock(resource, "name", "saveKey", "type", "hitpoints", "speed"), texture);
		AddFlowCard("Component Patch", BuildKnownPropertyBlock(resource, "componentPatch", "componentList", "spawnEvent", "dieEvent", "propertyChange"));
		RenderOverrideDiff(resource);
		AddTimelineRow("角色 key: " + ReadKnown(resource, "name", "saveKey"));
		AddTimelineRow("分类 category: " + ReadKnown(resource, "category", "characterType", "type"));
		AddGraphRow("角色 -> 场景 -> Component -> 事件 -> 卡片");
		AddReferenceRow("角色场景 sceneOverride -> " + ReadKnown(resource, "sceneOverride", "scene", "characterScene"));
		AddReferenceRow("角色贴图 spriteOverride -> " + ReadKnown(resource, "spriteOverride", "texture", "image"));
	}

	private void RenderCardPreview(Resource resource)
	{
		PrepareCanvas(2);
		Texture2D texture = TryReadTexture(resource, "texture", "icon", "packetTexture", "packetIcon");
		AddFlowCard("卡片预览", BuildKnownPropertyBlock(resource, "saveKey", "name", "cost", "packetCooldown", "type"), texture);
		AddFlowCard("图鉴预览", BuildKnownPropertyBlock(resource, "describe", "handbookDescribe", "handbookStory", "packetAnimeClip"));
		AddTimelineRow("卡片 key: " + ReadKnown(resource, "saveKey", "name"));
		AddTimelineRow("费用/冷却: cost=" + ReadKnown(resource, "cost") + ", packetCooldown=" + ReadKnown(resource, "packetCooldown", "cooldown"));
		AddGraphRow("卡片 -> characterConfig -> " + ReadKnown(resource, "characterConfig", "characterName"));
		AddReferenceRow("绑定角色 characterConfig -> " + ReadKnown(resource, "characterConfig", "characterName"));
		AddReferenceRow("卡牌动画 packetAnimeClip -> " + ReadKnown(resource, "packetAnimeClip"));
	}

	private void RenderPacketBankGrid(Resource resource)
	{
		PrepareCanvas(3);
		AddFlowCard("卡牌库", BuildKnownPropertyBlock(resource, "name", "saveKey", "category", "packetList"));
		int num = 0;
		foreach (var item in ExtractInterestingProperties(resource, "packet", "category", "unlock", "bank"))
		{
			AddFlowCard(item.Name, item.Value);
			AddGraphRow("卡牌库 -> " + item.Name);
			num++;
			if (num >= 12)
			{
				break;
			}
		}
		AddTimelineRow("卡牌库分类: " + ReadKnown(resource, "category", "categoryList"));
		AddReferenceRow("卡牌库资源 -> " + base.CurrentResourcePath);
	}

	private void RenderProjectileChain(Resource resource)
	{
		PrepareCanvas(2);
		Texture2D texture = TryReadTexture(resource, "texture", "icon", "projectileTexture");
		AddFlowCard("子弹", BuildKnownPropertyBlock(resource, "name", "projectileName", "baseDamage", "size", "scale", "splatAudio"), texture);
		AddFlowCard("ProjectileChange 链路", BuildKnownPropertyBlock(resource, "ProjectileChange", "changeList", "changeConfig", "methods"));
		AddFlowCard("命中事件", BuildKnownPropertyBlock(resource, "hitTargetEventList", "hitCharacterEventList", "hitGroundEventList", "splatScene"));
		AddGraphRow("ProjectileChange -> 变形 / 分裂 / 替换 / 终止");
		AddGraphRow("methods -> " + ReadKnown(resource, "methods"));
		AddTimelineRow("伤害: " + ReadKnown(resource, "baseDamage", "damage"));
		AddTimelineRow("命中音效: splatAudio=" + ReadKnown(resource, "splatAudio"));
		AddReferenceRow("子弹场景 projectileScene -> " + ReadKnown(resource, "projectileScene"));
		AddReferenceRow("命中特效 splatScene -> " + ReadKnown(resource, "splatScene", "hitEffect"));
		AddReferenceRow("命中音效 splatAudio -> " + ReadKnown(resource, "splatAudio"));
	}

	private void RenderUtilityConfig(Resource resource, string category)
	{
		PrepareCanvas(2);
		if (category == "Survival" && resource is TowerDefenseLevelSurvivalConfig towerDefenseLevelSurvivalConfig)
		{
			AddSurvivalRuntimePreviewCard(towerDefenseLevelSurvivalConfig);
			if (GodotObject.IsInstanceValid(_survivalPreviewWindow))
			{
				_survivalPreviewWindow.RefreshConfig(towerDefenseLevelSurvivalConfig);
			}
		}
		string text = (string.IsNullOrWhiteSpace(category) ? resource.GetType().Name : category);
		AddFlowCard(text, BuildKnownPropertyBlock(resource, "name", "saveKey", "type", "description", "describe"));
		AddFlowCard("事件 / 解锁", BuildKnownPropertyBlock(resource, "eventList", "unlockCheckList", "shovelableNames", "conditionList"));
		AddFlowCard("场景 / 贴图", BuildKnownPropertyBlock(resource, "scene", "texture", "icon", "audio", "anime"), TryReadTexture(resource, "texture", "icon"));
		AddGraphRow(text + " -> eventList / unlockCheckList / 引用资源");
		AddTimelineRow(text + ": " + ReadKnown(resource, "name", "saveKey", "type"));
		AddReferenceRow("掉落物/收集物/小推车/铲子/生存模式资源 -> " + base.CurrentResourcePath);
	}

	private void AddSurvivalRuntimePreviewCard(TowerDefenseLevelSurvivalConfig survival)
	{
		if (base.CanvasGrid == null)
		{
			return;
		}
		if (_survivalPreviewCardScene == null)
		{
			_survivalPreviewCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalRuntimePreviewCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		PanelContainer panelContainer = _survivalPreviewCardScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(panelContainer))
		{
			panelContainer.GetNode<Button>("Layout/RunButton").Pressed += delegate
			{
				OpenSurvivalRuntimePreview(survival);
			};
			base.CanvasGrid.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void OpenSurvivalRuntimePreview(TowerDefenseLevelSurvivalConfig survival)
	{
		if (!GodotObject.IsInstanceValid(_survivalPreviewWindow))
		{
			if (_survivalPreviewScene == null)
			{
				_survivalPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalPreviewWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			_survivalPreviewWindow = _survivalPreviewScene?.Instantiate<XWSurvivalPreviewWindow>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_survivalPreviewWindow))
			{
				return;
			}
			AddChild(_survivalPreviewWindow, forceReadableName: false, InternalMode.Disabled);
		}
		_survivalPreviewWindow.Preview(survival);
	}

	private void RenderSurvivalConfigEditor(TowerDefenseLevelSurvivalConfig survival)
	{
		if (base.CanvasGrid != null && GodotObject.IsInstanceValid(survival))
		{
			PrepareCanvas(1);
			if (_survivalConfigEditorScene == null)
			{
				_survivalConfigEditorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalConfigEditorLayout.tscn", "", ResourceLoader.CacheMode.Ignore);
			}
			Control control = _survivalConfigEditorScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(control))
			{
				AddPreviewRow("生存模式规则设计台加载失败");
				return;
			}
			base.CanvasGrid.AddChild(control, forceReadableName: false, InternalMode.Disabled);
			BindSurvivalConfigEditor(control, survival);
		}
	}

	private void BindSurvivalConfigEditor(Control root, TowerDefenseLevelSurvivalConfig survival)
	{
		_editingSurvivalConfig = survival;
		_survivalConfigBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), delegate
		{
			NotifyCurrentResourceEdited();
		});
		_survivalConfigEndlessCheck = root.GetNode<CheckBox>("%EndlessCheck");
		_survivalConfigDayNightCheck = root.GetNode<CheckBox>("%DayNightCheck");
		_survivalConfigJsonButton = root.GetNode<Button>("%JsonSourceButton");
		_survivalConfigJsonDialog = root.GetNode<FileDialog>("%JsonSourceDialog");
		_survivalBaseZombieList = root.GetNode<ItemList>("%BaseZombieList");
		_survivalRoundAddList = root.GetNode<ItemList>("%RoundAddList");
		_survivalBaseRemoveButton = root.GetNode<Button>("%RemoveBaseZombieButton");
		_survivalBaseMoveUpButton = root.GetNode<Button>("%MoveBaseZombieUpButton");
		_survivalBaseMoveDownButton = root.GetNode<Button>("%MoveBaseZombieDownButton");
		_survivalRoundAddOpenButton = root.GetNode<Button>("%OpenRoundAddButton");
		_survivalRoundAddRemoveButton = root.GetNode<Button>("%RemoveRoundAddButton");
		_survivalRoundAddMoveUpButton = root.GetNode<Button>("%MoveRoundAddUpButton");
		_survivalRoundAddMoveDownButton = root.GetNode<Button>("%MoveRoundAddDownButton");
		_survivalConfigZombieRoot = root.GetNode<Node2D>("%ZombieRoot");
		_survivalConfigBackground = root.GetNode<TextureRect>("%Background");
		_survivalConfigRoundLabel = root.GetNode<Label>("%PreviewRoundLabel");
		_survivalConfigTimeLabel = root.GetNode<Label>("%PreviewTimeLabel");
		_survivalConfigPoolStatus = root.GetNode<Label>("%PoolStatusLabel");
		_survivalConfigPointLabel = root.GetNode<Label>("%PointLabel");
		_survivalConfigStatus = root.GetNode<Label>("%StatusLabel");
		_survivalConfigPointProgress = root.GetNode<ProgressBar>("%PointProgress");
		_survivalPreviewRoundSpin = root.GetNode<SpinBox>("%PreviewRoundSpin");
		_survivalPreviewWaveSpin = root.GetNode<SpinBox>("%PreviewWaveSpin");
		_survivalPreviewBigWaveCheck = root.GetNode<CheckBox>("%PreviewBigWaveCheck");
		BindSurvivalConfigNumber(root, "%RoundLimitSpin", "roundLimit");
		BindSurvivalConfigNumber(root, "%PointBeginSpin", "pointBegin");
		BindSurvivalConfigNumber(root, "%PointMaxSpin", "pointMax");
		BindSurvivalConfigNumber(root, "%PointWaveSpin", "pointIncrementPerWave");
		BindSurvivalConfigNumber(root, "%PointBigWaveSpin", "pointIncrementPerBigWave");
		BindSurvivalConfigNumber(root, "%PointRoundSpin", "pointIncrementPerRound");
		BindSurvivalConfigNumber(root, "%BigWaveScaleSpin", "pointBigWaveScale");
		_survivalConfigBinding.BindToggle(_survivalConfigDayNightCheck, survival, "roundDayNightChange", RebuildSurvivalConfigPreview, this, "RefreshSurvivalConfigEditorState");
		if (survival.roundLimit >= 0)
		{
			_lastFiniteSurvivalRoundLimit = Math.Max(1, survival.roundLimit);
		}
		_survivalConfigEndlessCheck.SetPressedNoSignal(survival.roundLimit < 0);
		_survivalConfigEndlessCheck.Toggled += SetSurvivalConfigEndless;
		_survivalConfigJsonButton.Pressed += OpenSurvivalConfigJsonSource;
		_survivalConfigJsonDialog.FileSelected += SetSurvivalConfigJsonSource;
		root.GetNode<Button>("%RunPreviewButton").Pressed += delegate
		{
			OpenSurvivalRuntimePreview(survival);
		};
		_survivalPreviewRoundSpin.ValueChanged += delegate
		{
			RebuildSurvivalConfigPreview();
		};
		_survivalPreviewWaveSpin.ValueChanged += delegate
		{
			RebuildSurvivalConfigPreview();
		};
		_survivalPreviewBigWaveCheck.Toggled += delegate
		{
			RebuildSurvivalConfigPreview();
		};
		root.GetNode<Button>("%ResetPreviewButton").Pressed += ResetSurvivalConfigPreview;
		root.GetNode<Button>("%NextWavePreviewButton").Pressed += AdvanceSurvivalConfigPreviewWave;
		root.GetNode<Button>("%NextRoundPreviewButton").Pressed += AdvanceSurvivalConfigPreviewRound;
		_survivalBaseZombieList.ItemSelected += delegate(long index)
		{
			_selectedSurvivalBaseZombieIndex = (int)index;
			UpdateSurvivalConfigButtons();
		};
		root.GetNode<Button>("%AddBaseZombieButton").Pressed += OpenSurvivalBaseZombiePicker;
		_survivalBaseRemoveButton.Pressed += RemoveSelectedSurvivalBaseZombie;
		_survivalBaseMoveUpButton.Pressed += delegate
		{
			MoveSelectedSurvivalBaseZombie(-1);
		};
		_survivalBaseMoveDownButton.Pressed += delegate
		{
			MoveSelectedSurvivalBaseZombie(1);
		};
		_survivalRoundAddList.ItemSelected += delegate(long index)
		{
			_selectedSurvivalRoundAddIndex = (int)index;
			UpdateSurvivalConfigButtons();
		};
		_survivalRoundAddList.ItemActivated += delegate
		{
			OpenSelectedSurvivalRoundAdd();
		};
		root.GetNode<Button>("%AddRoundAddButton").Pressed += AddSurvivalRoundAdd;
		_survivalRoundAddOpenButton.Pressed += OpenSelectedSurvivalRoundAdd;
		_survivalRoundAddRemoveButton.Pressed += RemoveSelectedSurvivalRoundAdd;
		_survivalRoundAddMoveUpButton.Pressed += delegate
		{
			MoveSelectedSurvivalRoundAdd(-1);
		};
		_survivalRoundAddMoveDownButton.Pressed += delegate
		{
			MoveSelectedSurvivalRoundAdd(1);
		};
		ResetSurvivalConfigPreview();
		RefreshSurvivalConfigEditorState();
	}

	private void BindSurvivalConfigNumber(Control root, NodePath controlPath, string property)
	{
		SpinBox node = root.GetNode<SpinBox>(controlPath);
		_survivalConfigNumberControls[property] = node;
		_survivalConfigBinding.BindNumber(node, _editingSurvivalConfig, property, RebuildSurvivalConfigPreview, this, "RefreshSurvivalConfigEditorState");
	}

	public void RefreshSurvivalConfigEditorState()
	{
		TowerDefenseLevelSurvivalConfig editingSurvivalConfig = _editingSurvivalConfig;
		if (!GodotObject.IsInstanceValid(editingSurvivalConfig))
		{
			return;
		}
		foreach (var (text2, spinBox2) in _survivalConfigNumberControls)
		{
			if (GodotObject.IsInstanceValid(spinBox2))
			{
				spinBox2.SetValueNoSignal(editingSurvivalConfig.Get(text2).AsDouble());
			}
		}
		if (editingSurvivalConfig.roundLimit >= 0)
		{
			_lastFiniteSurvivalRoundLimit = Math.Max(1, editingSurvivalConfig.roundLimit);
		}
		if (GodotObject.IsInstanceValid(_survivalConfigEndlessCheck))
		{
			_survivalConfigEndlessCheck.SetPressedNoSignal(editingSurvivalConfig.roundLimit < 0);
		}
		if (_survivalConfigNumberControls.TryGetValue("roundLimit", out var value))
		{
			value.Editable = editingSurvivalConfig.roundLimit >= 0;
		}
		if (GodotObject.IsInstanceValid(_survivalConfigDayNightCheck))
		{
			_survivalConfigDayNightCheck.SetPressedNoSignal(editingSurvivalConfig.roundDayNightChange);
		}
		if (GodotObject.IsInstanceValid(_survivalConfigJsonButton))
		{
			string text3 = (GodotObject.IsInstanceValid(editingSurvivalConfig.json) ? editingSurvivalConfig.json.ResourcePath.GetFile() : "未设置");
			_survivalConfigJsonButton.Text = "JSON 来源：" + text3;
			_survivalConfigJsonButton.TooltipText = (GodotObject.IsInstanceValid(editingSurvivalConfig.json) ? editingSurvivalConfig.json.ResourcePath : "选择一个 JSON 资源作为生存规则来源");
		}
		RebuildSurvivalBaseZombieList();
		RebuildSurvivalRoundAddList();
		RebuildSurvivalConfigPreview();
		NotifySurvivalUndoRedoRefresh();
	}

	private void SetSurvivalConfigEndless(bool endless)
	{
		TowerDefenseLevelSurvivalConfig editingSurvivalConfig = _editingSurvivalConfig;
		if (GodotObject.IsInstanceValid(editingSurvivalConfig) && _survivalConfigBinding != null && (endless || editingSurvivalConfig.roundLimit < 0))
		{
			if (endless && editingSurvivalConfig.roundLimit >= 0)
			{
				_lastFiniteSurvivalRoundLimit = Math.Max(1, editingSurvivalConfig.roundLimit);
			}
			int num = (endless ? (-1) : Math.Max(1, _lastFiniteSurvivalRoundLimit));
			_survivalConfigBinding.SetValue(editingSurvivalConfig, "roundLimit", num, endless ? "切换为无尽生存" : "设置生存轮次上限", this, "RefreshSurvivalConfigEditorState");
		}
	}

	private void OpenSurvivalConfigJsonSource()
	{
		if (GodotObject.IsInstanceValid(_survivalConfigJsonDialog))
		{
			_survivalConfigJsonDialog.PopupCenteredClamped(new Vector2I(900, 620), 0.9f);
		}
	}

	private void SetSurvivalConfigJsonSource(string path)
	{
		TowerDefenseLevelSurvivalConfig editingSurvivalConfig = _editingSurvivalConfig;
		if (GodotObject.IsInstanceValid(editingSurvivalConfig) && _survivalConfigBinding != null && !string.IsNullOrWhiteSpace(path))
		{
			Json json = ResourceLoader.Load<Json>(path, "", ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(json))
			{
				XWEditorInterface.Instance?.ShowToast("无法加载生存模式 JSON：" + path);
			}
			else
			{
				_survivalConfigBinding.SetValue(editingSurvivalConfig, "json", json, "更换生存模式 JSON 来源", this, "RefreshSurvivalConfigEditorState");
			}
		}
	}

	private void ResetSurvivalConfigPreview()
	{
		_survivalPreviewRoundSpin?.SetValueNoSignal(0.0);
		_survivalPreviewWaveSpin?.SetValueNoSignal(0.0);
		_survivalPreviewBigWaveCheck?.SetPressedNoSignal(pressed: false);
		RebuildSurvivalConfigPreview();
	}

	private void AdvanceSurvivalConfigPreviewWave()
	{
		if (GodotObject.IsInstanceValid(_survivalPreviewWaveSpin))
		{
			_survivalPreviewWaveSpin.Value += 1.0;
			_survivalPreviewBigWaveCheck?.SetPressedNoSignal(pressed: false);
			RebuildSurvivalConfigPreview();
		}
	}

	private void AdvanceSurvivalConfigPreviewRound()
	{
		if (GodotObject.IsInstanceValid(_survivalPreviewRoundSpin) && GodotObject.IsInstanceValid(_editingSurvivalConfig) && (_editingSurvivalConfig.roundLimit < 0 || !(_survivalPreviewRoundSpin.Value >= (double)_editingSurvivalConfig.roundLimit)))
		{
			_survivalPreviewRoundSpin.Value += 1.0;
			_survivalPreviewWaveSpin?.SetValueNoSignal(0.0);
			RebuildSurvivalConfigPreview();
		}
	}

	private void RebuildSurvivalConfigPreview()
	{
		TowerDefenseLevelSurvivalConfig editingSurvivalConfig = _editingSurvivalConfig;
		if (!GodotObject.IsInstanceValid(editingSurvivalConfig))
		{
			return;
		}
		int num = (GodotObject.IsInstanceValid(_survivalPreviewRoundSpin) ? ((int)_survivalPreviewRoundSpin.Value) : 0);
		int num2 = (GodotObject.IsInstanceValid(_survivalPreviewWaveSpin) ? ((int)_survivalPreviewWaveSpin.Value) : 0);
		bool flag = GodotObject.IsInstanceValid(_survivalPreviewBigWaveCheck) && _survivalPreviewBigWaveCheck.ButtonPressed;
		bool flag2 = editingSurvivalConfig.roundDayNightChange && num % 2 != 0;
		long num3 = editingSurvivalConfig.pointBegin + (long)num * (long)editingSurvivalConfig.pointIncrementPerRound + (long)num2 * (long)editingSurvivalConfig.pointIncrementPerWave;
		if (flag && num2 > 0)
		{
			num3 += editingSurvivalConfig.pointIncrementPerBigWave - editingSurvivalConfig.pointIncrementPerWave;
		}
		num3 = Math.Clamp(num3, 0L, Math.Max(0L, editingSurvivalConfig.pointMax));
		if (GodotObject.IsInstanceValid(_survivalConfigRoundLabel))
		{
			_survivalConfigRoundLabel.Text = ((editingSurvivalConfig.roundLimit < 0) ? $"轮次 {num} / 无尽" : $"轮次 {num} / {editingSurvivalConfig.roundLimit}");
		}
		if (GodotObject.IsInstanceValid(_survivalConfigTimeLabel))
		{
			_survivalConfigTimeLabel.Text = (flag2 ? "黑夜" : "白天");
			_survivalConfigTimeLabel.AddThemeColorOverride("font_color", flag2 ? new Color(0.55f, 0.68f, 1f) : new Color(1f, 0.88f, 0.38f));
		}
		if (GodotObject.IsInstanceValid(_survivalConfigBackground))
		{
			_survivalConfigBackground.Modulate = (flag2 ? new Color(0.36f, 0.48f, 0.72f) : Colors.White);
		}
		if (GodotObject.IsInstanceValid(_survivalConfigPointLabel))
		{
			_survivalConfigPointLabel.Text = $"生成点数 {num3:N0} / {editingSurvivalConfig.pointMax:N0} · 大波倍率 ×{editingSurvivalConfig.pointBigWaveScale:0.##}";
		}
		if (GodotObject.IsInstanceValid(_survivalConfigPointProgress))
		{
			_survivalConfigPointProgress.MinValue = 0.0;
			_survivalConfigPointProgress.MaxValue = Math.Max(1.0, editingSurvivalConfig.pointMax);
			_survivalConfigPointProgress.Value = num3;
		}
		List<string> list = new List<string>();
		foreach (Variant item in editingSurvivalConfig.zombiePoolBase)
		{
			list.Add(item.AsString());
		}
		foreach (TowerDefenseLevelSurvivalZombiePoolRoundAddConfig item2 in editingSurvivalConfig.zombiePoolRoundAdd)
		{
			if (!GodotObject.IsInstanceValid(item2) || item2.round > num)
			{
				continue;
			}
			foreach (Variant zombie in item2.zombieList)
			{
				list.Add(zombie.AsString());
			}
		}
		if (GodotObject.IsInstanceValid(_survivalConfigPoolStatus))
		{
			_survivalConfigPoolStatus.Text = $"当前僵尸池 {list.Count} 种";
		}
		RebuildSurvivalConfigZombiePreview(list);
		if (GodotObject.IsInstanceValid(_survivalConfigStatus))
		{
			_survivalConfigStatus.Text = $"预览：第 {num} 轮、第 {num2} 波 · {(flag2 ? "黑夜" : "白天")} · {(flag ? "大波点数" : "普通波点数")} · 不执行游戏逻辑";
		}
	}

	private void RebuildSurvivalConfigZombiePreview(List<string> pool)
	{
		if (!GodotObject.IsInstanceValid(_survivalConfigZombieRoot))
		{
			return;
		}
		foreach (Node child in _survivalConfigZombieRoot.GetChildren())
		{
			_survivalConfigZombieRoot.RemoveChild(child);
			child.QueueFree();
		}
		int num = Math.Min(pool?.Count ?? 0, 8);
		for (int i = 0; i < num; i++)
		{
			string text = pool[i]?.StripEdges() ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			PackedScene packedScene = ResourceManager.Instance?.GetCharacterScene(text);
			if (packedScene == null)
			{
				packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				continue;
			}
			try
			{
				Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				_survivalPreviewSafety.PrepareCharacter(node);
				XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(node);
				TowerDefenseCharacter towerDefenseCharacter = FindSurvivalPreviewCharacter(node);
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					node.Free();
					continue;
				}
				towerDefenseCharacter.inGame = false;
				towerDefenseCharacter.editorPreviewMode = true;
				towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
				node.ProcessMode = ProcessModeEnum.Disabled;
				Node2D obj = (node as Node2D) ?? towerDefenseCharacter;
				obj.Position = new Vector2(600f + (float)(i % 4) * 96f, 125f + (float)(i / 4) * 110f);
				obj.Scale = Vector2.One * 0.58f;
				_survivalConfigZombieRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Survival config zombie preview failed: " + text + " " + ex.Message);
			}
		}
	}

	private void RebuildSurvivalBaseZombieList()
	{
		if (GodotObject.IsInstanceValid(_survivalBaseZombieList) && GodotObject.IsInstanceValid(_editingSurvivalConfig))
		{
			int num = _editingSurvivalConfig.zombiePoolBase?.Count ?? 0;
			_selectedSurvivalBaseZombieIndex = ((num == 0) ? (-1) : Math.Clamp(_selectedSurvivalBaseZombieIndex, 0, num - 1));
			_survivalBaseZombieList.Clear();
			for (int i = 0; i < num; i++)
			{
				string text = _editingSurvivalConfig.zombiePoolBase[i].AsString();
				int idx = _survivalBaseZombieList.AddItem($"{i + 1}. {(string.IsNullOrWhiteSpace(text) ? "未设置僵尸" : text)}");
				_survivalBaseZombieList.SetItemMetadata(idx, text);
			}
			if (_selectedSurvivalBaseZombieIndex >= 0)
			{
				_survivalBaseZombieList.Select(_selectedSurvivalBaseZombieIndex);
			}
			UpdateSurvivalConfigButtons();
		}
	}

	private void RebuildSurvivalRoundAddList()
	{
		if (GodotObject.IsInstanceValid(_survivalRoundAddList) && GodotObject.IsInstanceValid(_editingSurvivalConfig))
		{
			int num = _editingSurvivalConfig.zombiePoolRoundAdd?.Count ?? 0;
			_selectedSurvivalRoundAddIndex = ((num == 0) ? (-1) : Math.Clamp(_selectedSurvivalRoundAddIndex, 0, num - 1));
			_survivalRoundAddList.Clear();
			for (int i = 0; i < num; i++)
			{
				TowerDefenseLevelSurvivalZombiePoolRoundAddConfig towerDefenseLevelSurvivalZombiePoolRoundAddConfig = _editingSurvivalConfig.zombiePoolRoundAdd[i];
				string value = (GodotObject.IsInstanceValid(towerDefenseLevelSurvivalZombiePoolRoundAddConfig) ? $"第 {towerDefenseLevelSurvivalZombiePoolRoundAddConfig.round} 轮 · 新增 {towerDefenseLevelSurvivalZombiePoolRoundAddConfig.zombieList.Count} 种僵尸" : "未设置轮次组");
				_survivalRoundAddList.AddItem($"{i + 1}. {value}");
			}
			if (_selectedSurvivalRoundAddIndex >= 0)
			{
				_survivalRoundAddList.Select(_selectedSurvivalRoundAddIndex);
			}
			UpdateSurvivalConfigButtons();
		}
	}

	private void UpdateSurvivalConfigButtons()
	{
		int num = (GodotObject.IsInstanceValid(_editingSurvivalConfig) ? (_editingSurvivalConfig.zombiePoolBase?.Count ?? 0) : 0);
		bool flag = _selectedSurvivalBaseZombieIndex >= 0 && _selectedSurvivalBaseZombieIndex < num;
		if (GodotObject.IsInstanceValid(_survivalBaseRemoveButton))
		{
			_survivalBaseRemoveButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_survivalBaseMoveUpButton))
		{
			_survivalBaseMoveUpButton.Disabled = !flag || _selectedSurvivalBaseZombieIndex == 0;
		}
		if (GodotObject.IsInstanceValid(_survivalBaseMoveDownButton))
		{
			_survivalBaseMoveDownButton.Disabled = !flag || _selectedSurvivalBaseZombieIndex >= num - 1;
		}
		int num2 = (GodotObject.IsInstanceValid(_editingSurvivalConfig) ? (_editingSurvivalConfig.zombiePoolRoundAdd?.Count ?? 0) : 0);
		bool flag2 = _selectedSurvivalRoundAddIndex >= 0 && _selectedSurvivalRoundAddIndex < num2;
		if (GodotObject.IsInstanceValid(_survivalRoundAddOpenButton))
		{
			_survivalRoundAddOpenButton.Disabled = !flag2;
		}
		if (GodotObject.IsInstanceValid(_survivalRoundAddRemoveButton))
		{
			_survivalRoundAddRemoveButton.Disabled = !flag2;
		}
		if (GodotObject.IsInstanceValid(_survivalRoundAddMoveUpButton))
		{
			_survivalRoundAddMoveUpButton.Disabled = !flag2 || _selectedSurvivalRoundAddIndex == 0;
		}
		if (GodotObject.IsInstanceValid(_survivalRoundAddMoveDownButton))
		{
			_survivalRoundAddMoveDownButton.Disabled = !flag2 || _selectedSurvivalRoundAddIndex >= num2 - 1;
		}
	}

	private void OpenSurvivalBaseZombiePicker()
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalConfig))
		{
			EnsureSurvivalZombiePicker();
			if (GodotObject.IsInstanceValid(_survivalZombiePicker))
			{
				string currentKey = ((_selectedSurvivalBaseZombieIndex >= 0 && _selectedSurvivalBaseZombieIndex < _editingSurvivalConfig.zombiePoolBase.Count) ? _editingSurvivalConfig.zombiePoolBase[_selectedSurvivalBaseZombieIndex].AsString() : "");
				_survivalZombiePicker.Open(XWGameplayResourceKind.Character, currentKey, AddSurvivalBaseZombie, IsZombieChoice);
			}
		}
	}

	private void EnsureSurvivalZombiePicker()
	{
		if (!GodotObject.IsInstanceValid(_survivalZombiePicker))
		{
			_survivalZombiePicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_survivalZombiePicker))
			{
				AddChild(_survivalZombiePicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void AddSurvivalBaseZombie(XWGameplayResourceChoice choice)
	{
		if (!GodotObject.IsInstanceValid(_editingSurvivalConfig) || !IsZombieChoice(choice))
		{
			return;
		}
		for (int i = 0; i < _editingSurvivalConfig.zombiePoolBase.Count; i++)
		{
			if (_editingSurvivalConfig.zombiePoolBase[i].AsString().Equals(choice.Key, StringComparison.OrdinalIgnoreCase))
			{
				_selectedSurvivalBaseZombieIndex = i;
				RebuildSurvivalBaseZombieList();
				return;
			}
		}
		Godot.Collections.Array array = new Godot.Collections.Array(_editingSurvivalConfig.zombiePoolBase);
		array.Add(choice.Key);
		_selectedSurvivalBaseZombieIndex = array.Count - 1;
		ReplaceSurvivalBaseZombieList(array, "添加基础僵尸");
	}

	private void RemoveSelectedSurvivalBaseZombie()
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalConfig) && _selectedSurvivalBaseZombieIndex >= 0 && _selectedSurvivalBaseZombieIndex < _editingSurvivalConfig.zombiePoolBase.Count)
		{
			Godot.Collections.Array array = new Godot.Collections.Array(_editingSurvivalConfig.zombiePoolBase);
			array.RemoveAt(_selectedSurvivalBaseZombieIndex);
			_selectedSurvivalBaseZombieIndex = ((array.Count == 0) ? (-1) : Math.Min(_selectedSurvivalBaseZombieIndex, array.Count - 1));
			ReplaceSurvivalBaseZombieList(array, "移除基础僵尸");
		}
	}

	private void MoveSelectedSurvivalBaseZombie(int direction)
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalConfig))
		{
			int num = _selectedSurvivalBaseZombieIndex + Math.Sign(direction);
			if (_selectedSurvivalBaseZombieIndex >= 0 && _selectedSurvivalBaseZombieIndex < _editingSurvivalConfig.zombiePoolBase.Count && num >= 0 && num < _editingSurvivalConfig.zombiePoolBase.Count)
			{
				Godot.Collections.Array array = new Godot.Collections.Array(_editingSurvivalConfig.zombiePoolBase);
				Variant value = array[_selectedSurvivalBaseZombieIndex];
				array[_selectedSurvivalBaseZombieIndex] = array[num];
				array[num] = value;
				_selectedSurvivalBaseZombieIndex = num;
				ReplaceSurvivalBaseZombieList(array, (direction < 0) ? "上移基础僵尸" : "下移基础僵尸");
			}
		}
	}

	private void ReplaceSurvivalBaseZombieList(Godot.Collections.Array next, string actionName)
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalConfig) && _survivalConfigBinding != null)
		{
			_survivalConfigBinding.SetValue(_editingSurvivalConfig, "zombiePoolBase", next, actionName, this, "RefreshSurvivalConfigEditorState");
		}
	}

	private void AddSurvivalRoundAdd()
	{
		if (!GodotObject.IsInstanceValid(_editingSurvivalConfig) || _survivalConfigBinding == null)
		{
			return;
		}
		int num = 1;
		foreach (TowerDefenseLevelSurvivalZombiePoolRoundAddConfig item2 in _editingSurvivalConfig.zombiePoolRoundAdd)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				num = Math.Max(num, item2.round + 1);
			}
		}
		TowerDefenseLevelSurvivalZombiePoolRoundAddConfig item = new TowerDefenseLevelSurvivalZombiePoolRoundAddConfig
		{
			round = num
		};
		Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig> array = new Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig>(_editingSurvivalConfig.zombiePoolRoundAdd);
		array.Add(item);
		_selectedSurvivalRoundAddIndex = array.Count - 1;
		ReplaceSurvivalRoundAddList(array, "新建生存轮次组");
	}

	private void RemoveSelectedSurvivalRoundAdd()
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalConfig) && _selectedSurvivalRoundAddIndex >= 0 && _selectedSurvivalRoundAddIndex < _editingSurvivalConfig.zombiePoolRoundAdd.Count)
		{
			Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig> array = new Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig>(_editingSurvivalConfig.zombiePoolRoundAdd);
			array.RemoveAt(_selectedSurvivalRoundAddIndex);
			_selectedSurvivalRoundAddIndex = ((array.Count == 0) ? (-1) : Math.Min(_selectedSurvivalRoundAddIndex, array.Count - 1));
			ReplaceSurvivalRoundAddList(array, "移除生存轮次组");
		}
	}

	private void MoveSelectedSurvivalRoundAdd(int direction)
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalConfig))
		{
			int num = _selectedSurvivalRoundAddIndex + Math.Sign(direction);
			if (_selectedSurvivalRoundAddIndex >= 0 && _selectedSurvivalRoundAddIndex < _editingSurvivalConfig.zombiePoolRoundAdd.Count && num >= 0 && num < _editingSurvivalConfig.zombiePoolRoundAdd.Count)
			{
				Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig> array = new Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig>(_editingSurvivalConfig.zombiePoolRoundAdd);
				TowerDefenseLevelSurvivalZombiePoolRoundAddConfig value = array[_selectedSurvivalRoundAddIndex];
				array[_selectedSurvivalRoundAddIndex] = array[num];
				array[num] = value;
				_selectedSurvivalRoundAddIndex = num;
				ReplaceSurvivalRoundAddList(array, (direction < 0) ? "上移生存轮次组" : "下移生存轮次组");
			}
		}
	}

	private void ReplaceSurvivalRoundAddList(Array<TowerDefenseLevelSurvivalZombiePoolRoundAddConfig> next, string actionName)
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalConfig) && _survivalConfigBinding != null)
		{
			_survivalConfigBinding.SetValue(_editingSurvivalConfig, "zombiePoolRoundAdd", next, actionName, this, "RefreshSurvivalConfigEditorState");
		}
	}

	private void OpenSelectedSurvivalRoundAdd()
	{
		TowerDefenseLevelSurvivalConfig editingSurvivalConfig = _editingSurvivalConfig;
		if (GodotObject.IsInstanceValid(editingSurvivalConfig) && _selectedSurvivalRoundAddIndex >= 0 && _selectedSurvivalRoundAddIndex < editingSurvivalConfig.zombiePoolRoundAdd.Count)
		{
			TowerDefenseLevelSurvivalZombiePoolRoundAddConfig towerDefenseLevelSurvivalZombiePoolRoundAddConfig = editingSurvivalConfig.zombiePoolRoundAdd[_selectedSurvivalRoundAddIndex];
			if (GodotObject.IsInstanceValid(towerDefenseLevelSurvivalZombiePoolRoundAddConfig))
			{
				string text = base.CurrentEditContext?.OwnerPath ?? base.CurrentResourcePath;
				XWResourceEditContext context = XWResourceEditContext.ForProperty(towerDefenseLevelSurvivalZombiePoolRoundAddConfig, editingSurvivalConfig, towerDefenseLevelSurvivalZombiePoolRoundAddConfig.ResourcePath, text, "zombiePoolRoundAdd", _selectedSurvivalRoundAddIndex, "survival_editor", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(text), $"第 {towerDefenseLevelSurvivalZombiePoolRoundAddConfig.round} 轮新增僵尸");
				XWResourceEditorRegistry.TryOpen(towerDefenseLevelSurvivalZombiePoolRoundAddConfig, towerDefenseLevelSurvivalZombiePoolRoundAddConfig.ResourcePath, context);
			}
		}
	}

	private void RenderSurvivalRoundAddEditor(TowerDefenseLevelSurvivalZombiePoolRoundAddConfig roundAdd)
	{
		if (base.CanvasGrid != null && GodotObject.IsInstanceValid(roundAdd))
		{
			PrepareCanvas(1);
			if (_survivalRoundAddEditorScene == null)
			{
				_survivalRoundAddEditorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWSurvivalRoundAddEditorLayout.tscn", "", ResourceLoader.CacheMode.Ignore);
			}
			Control control = _survivalRoundAddEditorScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(control))
			{
				AddPreviewRow("生存轮次编辑场景加载失败");
				return;
			}
			base.CanvasGrid.AddChild(control, forceReadableName: false, InternalMode.Disabled);
			BindSurvivalRoundAddEditor(control, roundAdd);
		}
	}

	private void BindSurvivalRoundAddEditor(Control root, TowerDefenseLevelSurvivalZombiePoolRoundAddConfig roundAdd)
	{
		_editingSurvivalRoundAdd = roundAdd;
		_survivalRoundSpinBox = root.GetNode<SpinBox>("%RoundSpinBox");
		_survivalRoundZombieList = root.GetNode<ItemList>("%ZombieList");
		_survivalRoundZombieRoot = root.GetNode<Node2D>("%ZombieRoot");
		_survivalRoundTitle = root.GetNode<Label>("%StageTitle");
		_survivalRoundStatus = root.GetNode<Label>("%PreviewStatus");
		_survivalRoundRemoveButton = root.GetNode<Button>("%RemoveZombieButton");
		_survivalRoundMoveUpButton = root.GetNode<Button>("%MoveZombieUpButton");
		_survivalRoundMoveDownButton = root.GetNode<Button>("%MoveZombieDownButton");
		_survivalRoundBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), delegate
		{
			NotifyCurrentResourceEdited();
		});
		_survivalRoundBinding.BindNumber(_survivalRoundSpinBox, roundAdd, "round", RefreshSurvivalRoundHeader, this, "RefreshSurvivalRoundHeaderState");
		_survivalRoundZombieList.ItemSelected += delegate(long index)
		{
			_selectedSurvivalZombieIndex = (int)index;
			UpdateSurvivalRoundButtons();
		};
		root.GetNode<Button>("%AddZombieButton").Pressed += OpenSurvivalZombiePicker;
		_survivalRoundRemoveButton.Pressed += RemoveSelectedSurvivalZombie;
		_survivalRoundMoveUpButton.Pressed += delegate
		{
			MoveSelectedSurvivalZombie(-1);
		};
		_survivalRoundMoveDownButton.Pressed += delegate
		{
			MoveSelectedSurvivalZombie(1);
		};
		RefreshSurvivalRoundHeader();
		RebuildSurvivalRoundZombieList();
		RebuildSurvivalRoundZombiePreview();
	}

	private void RefreshSurvivalRoundHeader()
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalRoundAdd))
		{
			if (GodotObject.IsInstanceValid(_survivalRoundTitle))
			{
				_survivalRoundTitle.Text = $"第 {_editingSurvivalRoundAdd.round} 轮加入阵容";
			}
			UpdateSurvivalRoundStatus();
		}
	}

	public void RefreshSurvivalRoundHeaderState()
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalRoundAdd))
		{
			if (GodotObject.IsInstanceValid(_survivalRoundSpinBox))
			{
				_survivalRoundSpinBox.SetValueNoSignal(_editingSurvivalRoundAdd.round);
			}
			RefreshSurvivalRoundHeader();
			NotifySurvivalUndoRedoRefresh();
		}
	}

	private void RebuildSurvivalRoundZombieList()
	{
		if (GodotObject.IsInstanceValid(_survivalRoundZombieList) && GodotObject.IsInstanceValid(_editingSurvivalRoundAdd))
		{
			int num = _editingSurvivalRoundAdd.zombieList?.Count ?? 0;
			_selectedSurvivalZombieIndex = ((num == 0) ? (-1) : Math.Clamp(_selectedSurvivalZombieIndex, 0, num - 1));
			_survivalRoundZombieList.Clear();
			for (int i = 0; i < num; i++)
			{
				string text = _editingSurvivalRoundAdd.zombieList[i].AsString().StripEdges();
				int idx = _survivalRoundZombieList.AddItem(string.IsNullOrWhiteSpace(text) ? $"{i + 1}. 未设置僵尸" : $"{i + 1}. {text}");
				_survivalRoundZombieList.SetItemMetadata(idx, text);
				_survivalRoundZombieList.SetItemTooltip(idx, string.IsNullOrWhiteSpace(text) ? "该位置尚未配置僵尸" : ("草坪预览角色：" + text));
			}
			if (_selectedSurvivalZombieIndex >= 0)
			{
				_survivalRoundZombieList.Select(_selectedSurvivalZombieIndex);
			}
			UpdateSurvivalRoundButtons();
		}
	}

	private void RebuildSurvivalRoundZombiePreview()
	{
		if (!GodotObject.IsInstanceValid(_survivalRoundZombieRoot) || !GodotObject.IsInstanceValid(_editingSurvivalRoundAdd))
		{
			return;
		}
		foreach (Node child in _survivalRoundZombieRoot.GetChildren())
		{
			_survivalRoundZombieRoot.RemoveChild(child);
			child.QueueFree();
		}
		int num = Math.Min(_editingSurvivalRoundAdd.zombieList?.Count ?? 0, 8);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			string text = _editingSurvivalRoundAdd.zombieList[i].AsString().StripEdges();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			PackedScene packedScene = ResourceManager.Instance?.GetCharacterScene(text);
			if (packedScene == null)
			{
				packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				continue;
			}
			try
			{
				Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				_survivalPreviewSafety.PrepareCharacter(node);
				XWRuntimePreviewSceneAdapter.PrepareAnimationSprites(node);
				TowerDefenseCharacter towerDefenseCharacter = FindSurvivalPreviewCharacter(node);
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					node.Free();
					continue;
				}
				towerDefenseCharacter.inGame = false;
				towerDefenseCharacter.editorPreviewMode = true;
				towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
				node.ProcessMode = ProcessModeEnum.Disabled;
				Node2D obj = (node as Node2D) ?? towerDefenseCharacter;
				obj.Position = new Vector2(610f + (float)(i % 4) * 92f, 125f + (float)(i / 4) * 112f);
				obj.Scale = Vector2.One * 0.58f;
				_survivalRoundZombieRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
				num2++;
			}
			catch (Exception ex)
			{
				GD.PushWarning("Survival round zombie preview failed: " + text + " " + ex.Message);
			}
		}
		UpdateSurvivalRoundStatus(num2);
	}

	private void UpdateSurvivalRoundStatus(int renderedCount = -1)
	{
		if (GodotObject.IsInstanceValid(_survivalRoundStatus) && GodotObject.IsInstanceValid(_editingSurvivalRoundAdd))
		{
			int num = _editingSurvivalRoundAdd.zombieList?.Count ?? 0;
			if (num == 0)
			{
				_survivalRoundStatus.Text = $"第 {_editingSurvivalRoundAdd.round} 轮尚未配置新增僵尸";
				return;
			}
			string value = ((renderedCount >= 0) ? $" · 草坪已显示 {renderedCount} 个安全预览" : "");
			_survivalRoundStatus.Text = $"第 {_editingSurvivalRoundAdd.round} 轮新增 {num} 种僵尸{value}";
		}
	}

	private void UpdateSurvivalRoundButtons()
	{
		int num = (GodotObject.IsInstanceValid(_editingSurvivalRoundAdd) ? (_editingSurvivalRoundAdd.zombieList?.Count ?? 0) : 0);
		bool flag = _selectedSurvivalZombieIndex >= 0 && _selectedSurvivalZombieIndex < num;
		if (GodotObject.IsInstanceValid(_survivalRoundRemoveButton))
		{
			_survivalRoundRemoveButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_survivalRoundMoveUpButton))
		{
			_survivalRoundMoveUpButton.Disabled = !flag || _selectedSurvivalZombieIndex == 0;
		}
		if (GodotObject.IsInstanceValid(_survivalRoundMoveDownButton))
		{
			_survivalRoundMoveDownButton.Disabled = !flag || _selectedSurvivalZombieIndex >= num - 1;
		}
	}

	private void OpenSurvivalZombiePicker()
	{
		if (!GodotObject.IsInstanceValid(_editingSurvivalRoundAdd))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(_survivalZombiePicker))
		{
			_survivalZombiePicker = XWGameplayResourcePickerWindow.Create();
			if (!GodotObject.IsInstanceValid(_survivalZombiePicker))
			{
				return;
			}
			AddChild(_survivalZombiePicker, forceReadableName: false, InternalMode.Disabled);
		}
		string currentKey = ((_selectedSurvivalZombieIndex >= 0 && _selectedSurvivalZombieIndex < _editingSurvivalRoundAdd.zombieList.Count) ? _editingSurvivalRoundAdd.zombieList[_selectedSurvivalZombieIndex].AsString() : "");
		_survivalZombiePicker.Open(XWGameplayResourceKind.Character, currentKey, AddSurvivalZombie, IsZombieChoice);
	}

	private static bool IsZombieChoice(XWGameplayResourceChoice choice)
	{
		if (choice == null || choice.Kind != XWGameplayResourceKind.Character)
		{
			return false;
		}
		string text = (choice.ResourcePath ?? "").Replace('\\', '/');
		if (!choice.Key.Contains("Zombie", StringComparison.OrdinalIgnoreCase))
		{
			return text.Contains("/Zombie/", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private void AddSurvivalZombie(XWGameplayResourceChoice choice)
	{
		if (!GodotObject.IsInstanceValid(_editingSurvivalRoundAdd) || !IsZombieChoice(choice) || string.IsNullOrWhiteSpace(choice.Key))
		{
			return;
		}
		for (int i = 0; i < _editingSurvivalRoundAdd.zombieList.Count; i++)
		{
			if (_editingSurvivalRoundAdd.zombieList[i].AsString().Equals(choice.Key, StringComparison.OrdinalIgnoreCase))
			{
				_selectedSurvivalZombieIndex = i;
				RebuildSurvivalRoundZombieList();
				return;
			}
		}
		TowerDefenseLevelSurvivalZombiePoolRoundAddConfig editingSurvivalRoundAdd = _editingSurvivalRoundAdd;
		Godot.Collections.Array array = new Godot.Collections.Array(editingSurvivalRoundAdd.zombieList);
		array.Add(choice.Key);
		_selectedSurvivalZombieIndex = array.Count - 1;
		ReplaceSurvivalZombieList(editingSurvivalRoundAdd, array, "添加生存轮次僵尸");
	}

	private void RemoveSelectedSurvivalZombie()
	{
		TowerDefenseLevelSurvivalZombiePoolRoundAddConfig editingSurvivalRoundAdd = _editingSurvivalRoundAdd;
		if (GodotObject.IsInstanceValid(editingSurvivalRoundAdd) && _selectedSurvivalZombieIndex >= 0 && _selectedSurvivalZombieIndex < editingSurvivalRoundAdd.zombieList.Count)
		{
			Godot.Collections.Array array = new Godot.Collections.Array(editingSurvivalRoundAdd.zombieList);
			array.RemoveAt(_selectedSurvivalZombieIndex);
			_selectedSurvivalZombieIndex = ((array.Count == 0) ? (-1) : Math.Min(_selectedSurvivalZombieIndex, array.Count - 1));
			ReplaceSurvivalZombieList(editingSurvivalRoundAdd, array, "移除生存轮次僵尸");
		}
	}

	private void MoveSelectedSurvivalZombie(int direction)
	{
		TowerDefenseLevelSurvivalZombiePoolRoundAddConfig editingSurvivalRoundAdd = _editingSurvivalRoundAdd;
		if (GodotObject.IsInstanceValid(editingSurvivalRoundAdd))
		{
			int num = _selectedSurvivalZombieIndex + Math.Sign(direction);
			if (_selectedSurvivalZombieIndex >= 0 && _selectedSurvivalZombieIndex < editingSurvivalRoundAdd.zombieList.Count && num >= 0 && num < editingSurvivalRoundAdd.zombieList.Count)
			{
				Godot.Collections.Array array = new Godot.Collections.Array(editingSurvivalRoundAdd.zombieList);
				Variant value = array[_selectedSurvivalZombieIndex];
				array[_selectedSurvivalZombieIndex] = array[num];
				array[num] = value;
				_selectedSurvivalZombieIndex = num;
				ReplaceSurvivalZombieList(editingSurvivalRoundAdd, array, (direction < 0) ? "上移生存轮次僵尸" : "下移生存轮次僵尸");
			}
		}
	}

	private void ReplaceSurvivalZombieList(TowerDefenseLevelSurvivalZombiePoolRoundAddConfig roundAdd, Godot.Collections.Array next, string actionName)
	{
		if (GodotObject.IsInstanceValid(roundAdd) && _survivalRoundBinding != null)
		{
			_survivalRoundBinding.SetValue(roundAdd, "zombieList", next, actionName, this, "RefreshSurvivalRoundZombieEditorState");
		}
	}

	public void RefreshSurvivalRoundZombieEditorState()
	{
		if (GodotObject.IsInstanceValid(_editingSurvivalRoundAdd))
		{
			RebuildSurvivalRoundZombieList();
			RebuildSurvivalRoundZombiePreview();
			NotifySurvivalUndoRedoRefresh();
		}
	}

	private void NotifySurvivalUndoRedoRefresh()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
		{
			NotifyCurrentResourceEdited();
		}
	}

	private static TowerDefenseCharacter FindSurvivalPreviewCharacter(Node node)
	{
		if (node is TowerDefenseCharacter result)
		{
			return result;
		}
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		foreach (Node child in node.GetChildren())
		{
			TowerDefenseCharacter towerDefenseCharacter = FindSurvivalPreviewCharacter(child);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private void RenderFeatureProcessSlots(Resource resource)
	{
		AddFlowCard("Feature 启用", BuildKnownPropertyBlock(resource, "feature", "featureList", "features", "featureConfig"));
		AddFlowCard("Process 配置", BuildKnownPropertyBlock(resource, "process", "processList", "processes", "processConfig"));
		AddGraphRow("feature -> 启用/关闭 -> 属性配置");
		AddGraphRow("process -> 模板/脚本/蓝图绑定");
		AddReferenceRow("feature -> " + ReadKnown(resource, "feature", "featureList", "features"));
		AddReferenceRow("process -> " + ReadKnown(resource, "process", "processList", "processes"));
	}

	private void RenderLevelIntegratedScenarioResources(Resource resource)
	{
		AddFlowCard("教程 / NPC / 生存", BuildKnownPropertyBlock(resource, "tutorial", "tutorialList", "tutorialConfig", "TutorialConfig", "npcTalk", "npcTalkList", "npcTalkConfig", "NpcTalkConfig", "survival", "survivalConfig", "Survival"));
		AddTimelineRow("关卡附属资源: tutorial / tutorialList / npcTalk / npcTalkList / survivalConfig");
		AddGraphRow("关卡 -> 关卡附属资源 -> TutorialConfig / NpcTalkConfig / Survival");
		AddReferenceRow("教程 tutorial -> " + ReadKnown(resource, "tutorial", "tutorialList", "tutorialConfig"));
		AddReferenceRow("NPC 对话 npcTalk -> " + ReadKnown(resource, "npcTalk", "npcTalkList", "npcTalkConfig"));
		AddReferenceRow("生存模式 survival -> " + ReadKnown(resource, "survival", "survivalConfig"));
	}

	private void RenderLevelEventResources(Resource resource)
	{
		AddFlowCard("关卡事件", BuildKnownPropertyBlock(resource, "eventList", "wave", "waveList", "spawnEvent", "dieEvent", "hitTargetEventList", "hitCharacterEventList", "hitGroundEventList"));
		AddFlowCard("触发/条件", BuildKnownPropertyBlock(resource, "conditionList", "trigger", "triggerList", "timeline", "unlockCheckList"));
		AddGraphRow("关卡 -> 事件资源 -> Feature/Process -> wave/timeline");
		AddTimelineRow("事件: eventList / wave / spawnEvent / dieEvent / trigger");
		HashSet<Resource> visited = new HashSet<Resource>();
		int count = 0;
		CollectLevelEventResources(resource, "level", visited, ref count, 64);
		if (count == 0)
		{
			AddPreviewRow("未发现关卡事件资源");
		}
	}

	private void CollectLevelEventResources(Resource resource, string ownerPath, HashSet<Resource> visited, ref int count, int maxCount)
	{
		if (!GodotObject.IsInstanceValid(resource) || count >= maxCount || !visited.Add(resource))
		{
			return;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string text = ReadPropertyName(property);
			if (!string.IsNullOrWhiteSpace(text) && !ShouldSkipProperty(text) && TryGetProperty(resource, text, out var value))
			{
				bool flag = IsLevelEventPropertyName(text);
				if (flag)
				{
					AddTimelineRow($"{ownerPath}.{text}: {FormatVariant(value)}");
					AddGraphRow("事件 -> " + ownerPath + "." + text);
					AddReferenceRow($"{ownerPath}.{text} -> {FormatVariant(value)}");
				}
				if (flag || value.VariantType == Variant.Type.Array || value.VariantType == Variant.Type.Dictionary || value.VariantType == Variant.Type.Object)
				{
					CollectLevelEventResources(value, ownerPath + "." + text, flag, visited, ref count, maxCount);
				}
			}
		}
	}

	private void CollectLevelEventResources(Variant value, string propertyPath, bool parentIsEventProperty, HashSet<Resource> visited, ref int count, int maxCount)
	{
		if (count >= maxCount)
		{
			return;
		}
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 24;
		if ((ulong)num > 4uL)
		{
			return;
		}
		int num2;
		switch ((int)num)
		{
		case 0:
			if (!(value.AsGodotObject() is Resource resource) || !GodotObject.IsInstanceValid(resource))
			{
				break;
			}
			if (!parentIsEventProperty)
			{
				num2 = (IsLevelEventResource(resource) ? 1 : 0);
				if (num2 == 0)
				{
					goto IL_0070;
				}
			}
			else
			{
				num2 = 1;
			}
			RegisterLevelEventResource(propertyPath, resource, ref count, maxCount);
			goto IL_0070;
		case 4:
		{
			Godot.Collections.Array array = value.AsGodotArray();
			for (int i = 0; i < array.Count; i++)
			{
				if (count >= maxCount)
				{
					break;
				}
				CollectLevelEventResources(array[i], $"{propertyPath}[{i}]", parentIsEventProperty, visited, ref count, maxCount);
			}
			break;
		}
		case 3:
		{
			Dictionary dictionary = value.AsGodotDictionary();
			{
				foreach (Variant key in dictionary.Keys)
				{
					string text = key.AsString();
					bool parentIsEventProperty2 = parentIsEventProperty || IsLevelEventPropertyName(text);
					CollectLevelEventResources(dictionary[key], propertyPath + "." + text, parentIsEventProperty2, visited, ref count, maxCount);
					if (count >= maxCount)
					{
						break;
					}
				}
				break;
			}
		}
		case 1:
		case 2:
			break;
			IL_0070:
			if (num2 != 0)
			{
				CollectLevelEventResources(resource, propertyPath, visited, ref count, maxCount);
			}
			break;
		}
	}

	private void RegisterLevelEventResource(string propertyPath, Resource eventResource, ref int count, int maxCount)
	{
		if (count < maxCount && GodotObject.IsInstanceValid(eventResource))
		{
			AddLevelEventCard(propertyPath, eventResource);
			AddReferenceRow("关卡事件 " + propertyPath + " -> " + FormatResource(eventResource));
			AddGraphRow("事件资源 -> " + propertyPath);
			count++;
		}
	}

	private void AddLevelEventCard(string title, Resource eventResource)
	{
		string text = BuildKnownPropertyBlock(eventResource, "name", "saveKey", "type", "enabled", "className", "method", "script", "blueprint", "conditionList", "eventList");
		text = text + "\nresource: " + FormatResource(eventResource);
		AddFlowCard(string.IsNullOrWhiteSpace(title) ? "关卡事件" : title, text, TryReadTexture(eventResource, "icon", "texture", "previewTexture"));
	}

	private static bool IsLevelEventResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return false;
		}
		if (!IsLevelEventPropertyName(resource.GetType().Name) && !IsLevelEventPropertyName(resource.ResourceName))
		{
			return IsLevelEventPropertyName(resource.ResourcePath);
		}
		return true;
	}

	private static bool IsLevelEventPropertyName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return false;
		}
		return ContainsAny(name, "event", "wave", "spawn", "die", "hitTarget", "hitCharacter", "hitGround", "feature", "process", "condition", "trigger", "timeline", "unlockCheck", "tutorial", "npc", "talk", "survival");
	}

	private void RenderOverrideDiff(Resource resource)
	{
		AddFlowCard("继承/覆盖", BuildKnownPropertyBlock(resource, "category", "creationMode", "baseCharacterKey", "targetCharacterKey"));
		AddGraphRow("category -> " + ReadKnown(resource, "category", "characterType", "type"));
		AddGraphRow("creationMode -> " + ReadKnown(resource, "creationMode", "mode"));
		AddGraphRow("baseCharacterKey -> " + ReadKnown(resource, "baseCharacterKey"));
		AddGraphRow("targetCharacterKey -> " + ReadKnown(resource, "targetCharacterKey"));
		AddGraphRow("componentPatch -> " + ReadKnown(resource, "componentPatch", "componentList"));
	}

	private void RenderResourceReferences(Resource resource)
	{
		foreach (var item in ExtractReferenceProperties(resource))
		{
			AddReferenceRow(item.Name + " -> " + item.Value);
		}
	}

	private void PrepareCanvas(int columns)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = Math.Max(1, columns);
		}
	}

	private void AddGridCell(string label, Color color, string tooltip = "")
	{
		if (base.CanvasGrid != null)
		{
			PanelContainer panelContainer = new PanelContainer
			{
				CustomMinimumSize = new Vector2(54f, 44f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				TooltipText = tooltip
			};
			base.CanvasGrid.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
			ColorRect colorRect = new ColorRect
			{
				Color = color,
				CustomMinimumSize = new Vector2(48f, 38f),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			panelContainer.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
			Label node = new Label
			{
				Text = label,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			colorRect.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void AddFlowCard(string title, string body, Texture2D texture = null)
	{
		if (base.CanvasGrid != null)
		{
			PanelContainer panelContainer = new PanelContainer
			{
				CustomMinimumSize = new Vector2(240f, 130f),
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
					CustomMinimumSize = new Vector2(112f, 68f),
					ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
					StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				}, forceReadableName: false, InternalMode.Disabled);
			}
			vBoxContainer.AddChild(new Label
			{
				Text = (string.IsNullOrWhiteSpace(body) ? "未设置" : body),
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private string BuildKnownPropertyBlock(Resource resource, params string[] names)
	{
		List<string> list = new List<string>();
		foreach (string text in names)
		{
			if (TryGetProperty(resource, text, out var value))
			{
				list.Add(text + ": " + FormatVariant(value));
			}
		}
		if (list.Count != 0)
		{
			return string.Join("\n", list);
		}
		return "未设置";
	}

	private string ReadKnown(Resource resource, params string[] names)
	{
		foreach (string propertyName in names)
		{
			if (TryGetProperty(resource, propertyName, out var value))
			{
				return FormatVariant(value);
			}
		}
		return "未设置";
	}

	private bool TryGetProperty(Resource resource, string propertyName, out Variant value)
	{
		value = default(Variant);
		if (!GodotObject.IsInstanceValid(resource) || string.IsNullOrWhiteSpace(propertyName))
		{
			return false;
		}
		try
		{
			value = resource.Get(propertyName);
			return value.VariantType != Variant.Type.Nil;
		}
		catch
		{
			return false;
		}
	}

	private Texture2D TryReadTexture(Resource resource, params string[] names)
	{
		foreach (string propertyName in names)
		{
			if (TryGetProperty(resource, propertyName, out var value) && value.VariantType == Variant.Type.Object && value.AsGodotObject() is Texture2D result)
			{
				return result;
			}
		}
		return null;
	}

	private Vector2I ReadVector2I(Resource resource, string name, Vector2I fallback)
	{
		if (!TryGetProperty(resource, name, out var value))
		{
			return fallback;
		}
		return value.VariantType switch
		{
			Variant.Type.Vector2I => value.AsVector2I(), 
			Variant.Type.Vector2 => new Vector2I(Mathf.RoundToInt(value.AsVector2().X), Mathf.RoundToInt(value.AsVector2().Y)), 
			_ => fallback, 
		};
	}

	private Vector2 ReadVector2(Resource resource, string name, Vector2 fallback)
	{
		if (!TryGetProperty(resource, name, out var value))
		{
			return fallback;
		}
		return value.VariantType switch
		{
			Variant.Type.Vector2 => value.AsVector2(), 
			Variant.Type.Vector2I => value.AsVector2I(), 
			_ => fallback, 
		};
	}

	private List<(string Name, string Value)> ExtractInterestingProperties(Resource resource, params string[] fragments)
	{
		List<(string, string)> list = new List<(string, string)>();
		if (!GodotObject.IsInstanceValid(resource))
		{
			return list;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string text = ReadPropertyName(property);
			if (!string.IsNullOrWhiteSpace(text) && !ShouldSkipProperty(text) && ContainsAny(text, fragments) && TryGetProperty(resource, text, out var value))
			{
				list.Add((text, FormatVariant(value)));
			}
		}
		return list;
	}

	private List<(string Name, string Value)> ExtractReferenceProperties(Resource resource)
	{
		List<(string, string)> list = new List<(string, string)>();
		if (!GodotObject.IsInstanceValid(resource))
		{
			return list;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string text = ReadPropertyName(property);
			if (!string.IsNullOrWhiteSpace(text) && !ShouldSkipProperty(text) && TryGetProperty(resource, text, out var value) && TryFormatReference(value, out var formatted))
			{
				list.Add((text, formatted));
			}
		}
		return list;
	}

	private static bool TryFormatReference(Variant value, out string formatted)
	{
		formatted = "";
		Variant.Type variantType = value.VariantType;
		if (variantType != Variant.Type.String)
		{
			Variant.Type num = variantType - 21;
			if ((ulong)num > 7uL)
			{
				goto IL_0129;
			}
			switch ((int)num)
			{
			case 3:
				if (value.AsGodotObject() is Resource resource)
				{
					formatted = FormatResource(resource);
					return formatted != "未设置";
				}
				goto IL_0129;
			case 0:
			case 1:
				break;
			case 7:
				formatted = $"数组({value.AsGodotArray().Count})";
				return value.AsGodotArray().Count > 0;
			case 6:
				formatted = $"字典({value.AsGodotDictionary().Count})";
				return value.AsGodotDictionary().Count > 0;
			default:
				goto IL_0129;
			}
		}
		string text = value.AsString();
		if (LooksLikeReference(text))
		{
			formatted = text;
			return true;
		}
		goto IL_0129;
		IL_0129:
		return false;
	}

	private static bool LooksLikeReference(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!text.Contains("res://", StringComparison.OrdinalIgnoreCase) && !text.Contains("uid://", StringComparison.OrdinalIgnoreCase) && !text.Contains("/", StringComparison.Ordinal) && !text.Contains("\\", StringComparison.Ordinal) && !text.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) && !text.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
		{
			return text.EndsWith(".dat", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string ReadPropertyName(Dictionary property)
	{
		if (property == null || !property.ContainsKey("name"))
		{
			return "";
		}
		return property["name"].AsString();
	}

	private static bool ShouldSkipProperty(string name)
	{
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

	private static bool ContainsAny(string text, params string[] fragments)
	{
		foreach (string value in fragments)
		{
			if (text.Contains(value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
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

	private static string FormatVariant(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 6uL)
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
				goto IL_00c7;
			case 5:
				return FormatVector(value.AsVector2());
			case 6:
				return FormatVector(value.AsVector2I());
			}
		}
		Variant.Type num = variantType - 20;
		if ((ulong)num > 8uL)
		{
			goto IL_01b4;
		}
		switch ((int)num)
		{
		case 1:
		case 2:
			break;
		case 0:
			return value.AsColor().ToHtml();
		case 8:
			return $"数组({value.AsGodotArray().Count})";
		case 7:
			return $"字典({value.AsGodotDictionary().Count})";
		case 4:
			return FormatResource(value.AsGodotObject() as Resource);
		default:
			goto IL_01b4;
		}
		goto IL_00c7;
		IL_01b4:
		return value.ToString();
		IL_00c7:
		return string.IsNullOrWhiteSpace(value.AsString()) ? "空" : value.AsString();
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

	private static string FormatVector(Vector2 value)
	{
		return $"({value.X:0.###}, {value.Y:0.###})";
	}

	private static string FormatVector(Vector2I value)
	{
		return $"({value.X}, {value.Y})";
	}
}
