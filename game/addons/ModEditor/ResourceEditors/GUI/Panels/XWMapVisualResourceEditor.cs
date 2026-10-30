using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWMapVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMapVisualEditorLayout.tscn";

	private const string MapRuleRuntimePreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMapRuleRuntimePreview.tscn";

	private static PackedScene _editorLayoutScene;

	private static PackedScene _mapRuleRuntimePreviewScene;

	private const string DefaultFrontlawnScenePath = "res://Asset/Config/Map/Frontlawn/Scene/Day/TowerDefenseMapFrontlawn.tscn";

	private static PackedScene _defaultFrontlawnScene;

	private TowerDefenseMapConfig _editingMap;

	private XWVisualPropertyBinding _mapPropertyBinding;

	private FlowContainer _lineUseHost;

	private VBoxContainer _specialRulesHost;

	private Label _specialRulesSummary;

	private SubViewport _mapSceneViewport;

	private Node2D _mapScenePreviewRoot;

	private Control _mapPreviewStack;

	private Control _mapGridOverlay;

	private Label _scenePathLabel;

	private Label _gridSummaryLabel;

	private Label _edgeSummaryLabel;

	private Label _flagsSummaryLabel;

	private Label _cellSelectionLabel;

	private CheckBox _useCellBrushTool;

	private OptionButton _brushGridTypeOption;

	private CheckBox _brushGroundCheckBox;

	private CheckBox _brushAirCheckBox;

	private CheckBox _brushWaterCheckBox;

	private SpinBox _brushElementFlagsSpinBox;

	private bool _isGridBoxSelecting;

	private bool MapCanvasPan;

	private Vector2 MapCanvasLastPointer = Vector2.Zero;

	private float MapCanvasZoom = 1f;

	private Vector2 MapCanvasOffset = Vector2.Zero;

	private Vector2I _gridSelectionStart = new Vector2I(-1, -1);

	private Vector2I _gridSelectionEnd = new Vector2I(-1, -1);

	private Rect2I MapGridSelectionRect = new Rect2I(0, 0, 0, 0);

	private bool _updatingControls;

	private Timer _mapSaveTimer;

	private Resource _editingMapRuleResource;

	private Node2D _mapRuleBaselineRoot;

	private Node2D _mapRuleResultRoot;

	private Label _mapRulePreviewStatus;

	private Label _mapRulePreviewResult;

	private Label _mapRulePreviewValidation;

	private OptionButton _mapRulePacketType;

	private Button _mapRulePacketTypeCatalogButton;

	private XWGameplayResourcePickerWindow _mapVisualPicker;

	private readonly List<XWVisualSegmentedOption> _mapSegmentedOptions = new List<XWVisualSegmentedOption>();

	private LineEdit _mapRuleCharacterName;

	private SpinBox _mapRuleBaseCooldown;

	private SpinBox _mapRuleBaseSpeed;

	private SpinBox _mapRuleBaseHitpoints;

	private SpinBox _mapRuleBaseScale;

	private SpinBox _mapRuleBaseDps;

	private double _mapRuleResultCooldown;

	private double _mapRuleResultSpeed;

	private double _mapRuleResultHitpoints;

	private double _mapRuleResultScale;

	private const float MapCanvasMinZoom = 0.05f;

	private const float MapCanvasMaxZoom = 12f;

	private bool UseCellBrushTool
	{
		get
		{
			if (GodotObject.IsInstanceValid(_useCellBrushTool))
			{
				return _useCellBrushTool.ButtonPressed;
			}
			return true;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		_mapSaveTimer = new Timer
		{
			WaitTime = 0.35,
			OneShot = true
		};
		_mapSaveTimer.Timeout += SavePendingMapResource;
		AddChild(_mapSaveTimer, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _ExitTree()
	{
		DisposeMapVisualChoices();
		DisposeMapPropertyBinding();
		_editingMap = null;
		_editingMapRuleResource = null;
		if (GodotObject.IsInstanceValid(_mapSaveTimer))
		{
			_mapSaveTimer.Stop();
		}
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeMapVisualChoices();
		DisposeMapPropertyBinding();
		_editingMap = null;
		_editingMapRuleResource = null;
		Resource currentResource = base.CurrentResource;
		if (!(currentResource is TowerDefenseMapConfig towerDefenseMapConfig))
		{
			if (currentResource is TowerDefenseMapRuleConfig || currentResource is TowerDefenseMapPacketRuleConfig || currentResource is TowerDefenseMapCharacterRuleConfig)
			{
				_editingMap = null;
				_editingMapRuleResource = base.CurrentResource;
				RenderMapRuleRuntimePreview(base.CurrentResource);
			}
		}
		else
		{
			_editingMap = towerDefenseMapConfig;
			_editingMapRuleResource = null;
			RenderMapEditor(towerDefenseMapConfig);
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteMapVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompleteMapVisualCoverage(Resource resource)
	{
		return resource?.GetType() == typeof(TowerDefenseMapConfig);
	}

	protected override void OnEmbeddedInspectorPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		Resource resource = obj as Resource;
		bool flag = resource != null;
		if (flag)
		{
			bool flag2 = ((resource is TowerDefenseMapRuleConfig || resource is TowerDefenseMapPacketRuleConfig || resource is TowerDefenseMapCharacterRuleConfig) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			_editingMapRuleResource = resource;
			UpdateMapRuleRuntimePreview();
		}
	}

	private void RenderMapRuleRuntimePreview(Resource rule)
	{
		if (base.CanvasGrid == null || !GodotObject.IsInstanceValid(rule))
		{
			return;
		}
		if (_mapRuleRuntimePreviewScene == null)
		{
			_mapRuleRuntimePreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMapRuleRuntimePreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Control control = _mapRuleRuntimePreviewScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(control))
		{
			base.CanvasGrid.Columns = 1;
			_mapRuleBaselineRoot = control.GetNode<Node2D>("%BaselineRoot");
			_mapRuleResultRoot = control.GetNode<Node2D>("%ResultRoot");
			_mapRulePreviewStatus = control.GetNode<Label>("Layout/Header/Status");
			_mapRulePreviewResult = control.GetNode<Label>("Layout/Split/Inputs/Result");
			_mapRulePreviewValidation = control.GetNode<Label>("Layout/Split/Inputs/Validation");
			_mapRulePacketType = control.GetNode<OptionButton>("Layout/Split/Inputs/PacketRow/PacketType");
			_mapRulePacketTypeCatalogButton = control.GetNode<Button>("%PacketTypeCatalogButton");
			_mapRuleCharacterName = control.GetNode<LineEdit>("Layout/Split/Inputs/CharacterRow/CharacterName");
			_mapRuleBaseCooldown = control.GetNode<SpinBox>("Layout/Split/Inputs/CooldownRow/Value");
			_mapRuleBaseSpeed = control.GetNode<SpinBox>("Layout/Split/Inputs/SpeedRow/Value");
			_mapRuleBaseHitpoints = control.GetNode<SpinBox>("Layout/Split/Inputs/HitpointsRow/Value");
			_mapRuleBaseScale = control.GetNode<SpinBox>("Layout/Split/Inputs/ScaleRow/Value");
			_mapRuleBaseDps = control.GetNode<SpinBox>("Layout/Split/Inputs/DpsRow/Value");
			TowerDefenseEnum.PACKET_TYPE[] values = Enum.GetValues<TowerDefenseEnum.PACKET_TYPE>();
			for (int i = 0; i < values.Length; i++)
			{
				TowerDefenseEnum.PACKET_TYPE id = values[i];
				_mapRulePacketType.AddItem(id.ToString(), (int)id);
			}
			_mapRulePacketTypeCatalogButton.Text = $"卡牌类型图鉴 · {(TowerDefenseEnum.PACKET_TYPE)_mapRulePacketType.GetSelectedId()}";
			_mapRulePacketTypeCatalogButton.Pressed += OpenMapRulePacketTypeCatalog;
			if (rule is TowerDefenseMapCharacterRuleConfig towerDefenseMapCharacterRuleConfig)
			{
				_mapRuleCharacterName.Text = towerDefenseMapCharacterRuleConfig.characterName;
			}
			_mapRulePacketType.ItemSelected += delegate
			{
				UpdateMapRuleRuntimePreview();
			};
			_mapRuleCharacterName.TextChanged += delegate
			{
				UpdateMapRuleRuntimePreview();
			};
			_mapRuleBaseCooldown.ValueChanged += delegate
			{
				UpdateMapRuleRuntimePreview();
			};
			_mapRuleBaseSpeed.ValueChanged += delegate
			{
				UpdateMapRuleRuntimePreview();
			};
			_mapRuleBaseHitpoints.ValueChanged += delegate
			{
				UpdateMapRuleRuntimePreview();
			};
			_mapRuleBaseScale.ValueChanged += delegate
			{
				UpdateMapRuleRuntimePreview();
			};
			_mapRuleBaseDps.ValueChanged += delegate
			{
				UpdateMapRuleRuntimePreview();
			};
			control.GetNode<Button>("Layout/Split/Inputs/RunButton").Pressed += UpdateMapRuleRuntimePreview;
			base.CanvasGrid.AddChild(control, forceReadableName: false, InternalMode.Disabled);
			base.PreviewList?.AddItem("输入测试卡牌、角色和基础数值，预览地图规则的实际乘区结果。");
			UpdateMapRuleRuntimePreview();
		}
	}

	private void UpdateMapRuleRuntimePreview()
	{
		if (!GodotObject.IsInstanceValid(_editingMapRuleResource))
		{
			return;
		}
		TowerDefenseEnum.PACKET_TYPE candidateType = (TowerDefenseEnum.PACKET_TYPE)(_mapRulePacketType?.GetSelectedId() ?? 0);
		string text = _mapRuleCharacterName?.Text ?? "";
		double num = _mapRuleBaseCooldown?.Value ?? 10.0;
		double num2 = _mapRuleBaseSpeed?.Value ?? 1.0;
		double num3 = _mapRuleBaseHitpoints?.Value ?? 100.0;
		double num4 = _mapRuleBaseScale?.Value ?? 1.0;
		double num5 = _mapRuleBaseDps?.Value ?? 20.0;
		bool flag = false;
		double value = 0.0;
		int num6 = 0;
		int num7 = 0;
		Resource editingMapRuleResource = _editingMapRuleResource;
		bool flag2;
		string reason;
		if (!(editingMapRuleResource is TowerDefenseMapRuleConfig towerDefenseMapRuleConfig))
		{
			if (!(editingMapRuleResource is TowerDefenseMapPacketRuleConfig towerDefenseMapPacketRuleConfig))
			{
				if (editingMapRuleResource is TowerDefenseMapCharacterRuleConfig towerDefenseMapCharacterRuleConfig)
				{
					flag2 = towerDefenseMapCharacterRuleConfig.TryValidateRuntime(out reason);
					if (string.Equals(towerDefenseMapCharacterRuleConfig.characterName, text, StringComparison.Ordinal))
					{
						num7 = 1;
						num2 *= towerDefenseMapCharacterRuleConfig.timeScaleMultiplier;
						num3 *= towerDefenseMapCharacterRuleConfig.hitpointScaleMultiplier;
						num4 *= towerDefenseMapCharacterRuleConfig.visualScaleMultiplier;
					}
					_mapRulePreviewStatus.Text = ((num7 > 0) ? "角色规则已命中" : "角色规则未命中");
				}
				else
				{
					flag2 = false;
					reason = "不支持的地图规则类型";
				}
			}
			else
			{
				flag2 = towerDefenseMapPacketRuleConfig.TryValidateRuntime(out reason);
				if (towerDefenseMapPacketRuleConfig.Matches(candidateType))
				{
					num6 = 1;
					num *= towerDefenseMapPacketRuleConfig.cooldownMultiplier;
					flag = towerDefenseMapPacketRuleConfig.ignoreDynamicCostGrowth;
				}
				_mapRulePreviewStatus.Text = ((num6 > 0) ? "卡牌规则已命中" : "卡牌规则未命中");
			}
		}
		else
		{
			flag2 = towerDefenseMapRuleConfig.TryValidateRuntime(out reason);
			value = num5 * towerDefenseMapRuleConfig.attackDpsLifestealRatio;
			if (towerDefenseMapRuleConfig.packetRules != null)
			{
				foreach (TowerDefenseMapPacketRuleConfig packetRule in towerDefenseMapRuleConfig.packetRules)
				{
					if (GodotObject.IsInstanceValid(packetRule) && packetRule.Matches(candidateType))
					{
						num6++;
						num *= packetRule.cooldownMultiplier;
						flag |= packetRule.ignoreDynamicCostGrowth;
					}
				}
			}
			if (towerDefenseMapRuleConfig.characterRules != null)
			{
				foreach (TowerDefenseMapCharacterRuleConfig characterRule in towerDefenseMapRuleConfig.characterRules)
				{
					if (GodotObject.IsInstanceValid(characterRule) && string.Equals(characterRule.characterName, text, StringComparison.Ordinal))
					{
						num7++;
						num2 *= characterRule.timeScaleMultiplier;
						num3 *= characterRule.hitpointScaleMultiplier;
						num4 *= characterRule.visualScaleMultiplier;
					}
				}
			}
			_mapRulePreviewStatus.Text = $"规则 {towerDefenseMapRuleConfig.id} · 卡牌 {num6} · 角色 {num7}";
		}
		_mapRuleResultCooldown = num;
		_mapRuleResultSpeed = num2;
		_mapRuleResultHitpoints = num3;
		_mapRuleResultScale = num4;
		_mapRulePreviewResult.Text = $"冷却: {num:0.###}\n速度: {num2:0.###}\n生命: {num3:0.###}\n视觉缩放: {num4:0.###}\nDPS 吸血: {value:0.###}\n忽略动态费用增长: {(flag ? "是" : "否")}";
		_mapRulePreviewValidation.Text = (flag2 ? "✓ 运行校验通过" : ("⚠ 运行校验失败: " + reason));
		_mapRulePreviewValidation.Modulate = (flag2 ? new Color(0.55f, 0.92f, 0.65f) : new Color(1f, 0.58f, 0.45f));
		RebuildMapRuleGamePreview(text);
	}

	private void RebuildMapRuleGamePreview(string characterName)
	{
		if (!GodotObject.IsInstanceValid(_mapRuleBaselineRoot) || !GodotObject.IsInstanceValid(_mapRuleResultRoot))
		{
			return;
		}
		foreach (Node child in _mapRuleBaselineRoot.GetChildren())
		{
			child.QueueFree();
		}
		foreach (Node child2 in _mapRuleResultRoot.GetChildren())
		{
			child2.QueueFree();
		}
		PackedScene packedScene = null;
		if (!string.IsNullOrWhiteSpace(characterName) && ResourceManager.Instance != null)
		{
			packedScene = ResourceManager.Instance.GetCharacterScene(characterName);
		}
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			packedScene = ResourceLoader.Load<PackedScene>(characterName.Contains("Zombie", StringComparison.OrdinalIgnoreCase) ? "res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn" : "res://Asset/Anime/Character/Plant/Chapter0/Blover/Scene/TowerDefensePlantBlover.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = InstantiateMapRuleCharacter(packedScene, _mapRuleBaselineRoot);
		TowerDefenseCharacter towerDefenseCharacter2 = InstantiateMapRuleCharacter(packedScene, _mapRuleResultRoot);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			towerDefenseCharacter.timeScale = _mapRuleBaseSpeed?.Value ?? 1.0;
			towerDefenseCharacter.Scale = Vector2.One * (float)(_mapRuleBaseScale?.Value ?? 1.0);
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
		{
			towerDefenseCharacter2.timeScale = _mapRuleResultSpeed;
			towerDefenseCharacter2.Scale = Vector2.One * (float)_mapRuleResultScale;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter2.instance))
			{
				towerDefenseCharacter2.instance.hitpoints = _mapRuleResultHitpoints;
			}
			double num = Math.Max(1.0, _mapRuleBaseHitpoints?.Value ?? 100.0);
			towerDefenseCharacter2.previewDamagePointPersontage = Mathf.Clamp(_mapRuleResultHitpoints / num, 0.0, 1.0);
		}
	}

	private static TowerDefenseCharacter InstantiateMapRuleCharacter(PackedScene scene, Node parent)
	{
		try
		{
			Node node = scene.Instantiate(PackedScene.GenEditState.Disabled);
			if (!(node is TowerDefenseCharacter towerDefenseCharacter))
			{
				node.Free();
				return null;
			}
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
			parent.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			return towerDefenseCharacter;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Map rule game preview failed: " + ex.Message);
			return null;
		}
	}

	private void RenderMapEditor(TowerDefenseMapConfig map)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			if (_editorLayoutScene == null)
			{
				_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMapVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindMapLayout(vBoxContainer, map);
				AddMapSummaryRows(map);
			}
		}
	}

	private void BindMapLayout(VBoxContainer root, TowerDefenseMapConfig map)
	{
		_scenePathLabel = root.GetNode<Label>("%ScenePathLabel");
		_scenePathLabel.Text = BuildMapSceneLabel(map);
		root.GetNode<Button>("%ResetViewButton").Pressed += ResetMapPreviewView;
		_mapPreviewStack = root.GetNode<Control>("%PreviewStack");
		_mapPreviewStack.Resized += delegate
		{
			UpdateMapSceneViewportSize();
			UpdateMapCanvasTransform();
			QueueGridOverlayRedraw();
		};
		_mapSceneViewport = root.GetNode<SubViewport>("%MapSceneViewport");
		_mapScenePreviewRoot = root.GetNode<Node2D>("%MapScenePreviewRoot");
		_mapGridOverlay = root.GetNode<Control>("%MapGridOverlay");
		_mapGridOverlay.Draw += delegate
		{
			DrawMapGridOverlay(_mapGridOverlay);
		};
		_mapGridOverlay.GuiInput += OnMapGridOverlayGuiInput;
		_useCellBrushTool = root.GetNode<CheckBox>("%UseCellBrushTool");
		_useCellBrushTool.Toggled += delegate
		{
			OnBrushSettingsChanged();
		};
		_brushGridTypeOption = root.GetNode<OptionButton>("%BrushGridTypeOption");
		_brushGridTypeOption.AddItem("地面 + 空中");
		_brushGridTypeOption.AddItem("仅地面");
		_brushGridTypeOption.AddItem("仅空中");
		_brushGridTypeOption.AddItem("水面");
		_brushGridTypeOption.ItemSelected += delegate(long index)
		{
			ApplyBrushGridPreset((int)index);
		};
		MountMapSegmentedOption(_brushGridTypeOption, root.GetNode<HFlowContainer>("%BrushGridTypeSegments"));
		_brushGroundCheckBox = root.GetNode<CheckBox>("%BrushGroundCheckBox");
		_brushAirCheckBox = root.GetNode<CheckBox>("%BrushAirCheckBox");
		_brushWaterCheckBox = root.GetNode<CheckBox>("%BrushWaterCheckBox");
		_brushGroundCheckBox.Toggled += delegate
		{
			OnBrushSettingsChanged();
		};
		_brushAirCheckBox.Toggled += delegate
		{
			OnBrushSettingsChanged();
		};
		_brushWaterCheckBox.Toggled += delegate
		{
			OnBrushSettingsChanged();
		};
		_brushElementFlagsSpinBox = root.GetNode<SpinBox>("%BrushElementFlagsSpinBox");
		_brushElementFlagsSpinBox.ValueChanged += delegate
		{
			UpdateCellSelectionLabel();
		};
		_cellSelectionLabel = root.GetNode<Label>("%CellSelectionLabel");
		_cellSelectionLabel.Text = BuildCellSelectionText();
		BindMapFields(root, map);
		RebuildMapScenePreview();
		ResetMapPreviewView();
	}

	private void OnBrushSettingsChanged()
	{
		UpdateCellSelectionLabel();
		QueueGridOverlayRedraw();
	}

	private void BindMapFields(VBoxContainer root, TowerDefenseMapConfig map)
	{
		_mapPropertyBinding?.Dispose();
		_mapPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnMapVisualPropertyEdited);
		_mapPropertyBinding.BindText(root.GetNode<LineEdit>("%ResourceNameEdit"), map, "resource_name", delegate
		{
			RefreshMapField("resource_name", refreshOverlay: false);
		}, this, "RefreshMapEditorFromHistory");
		_mapPropertyBinding.BindToggle(root.GetNode<CheckButton>("%LocalToSceneCheck"), map, "resource_local_to_scene", delegate
		{
			RefreshMapField("resource_local_to_scene", refreshOverlay: false);
		}, this, "RefreshMapEditorFromHistory");
		_mapPropertyBinding.BindText(root.GetNode<LineEdit>("%TranslateLineEdit"), map, "translate", delegate
		{
			RefreshMapField("translate", refreshOverlay: false);
		}, this, "RefreshMapEditorFromHistory");
		_mapPropertyBinding.BindText(root.GetNode<LineEdit>("%DayNightSwitchingLineEdit"), map, "dayNightSwitching", delegate
		{
			RefreshMapField("dayNightSwitching", refreshOverlay: false);
		}, this, "RefreshMapEditorFromHistory");
		MountMapResourcePicker(root.GetNode<HBoxContainer>("%MapScenePickerHost"), "MapScenePicker", "PackedScene", map.mapScene, delegate(Resource resource)
		{
			SetMapResourcePath("mapScenePath", resource, "更换地图场景", rebuildPreview: true);
		});
		MountMapResourcePicker(root.GetNode<HBoxContainer>("%MapTexturePickerHost"), "MapTexturePicker", "Texture2D", map.mapTexture, delegate(Resource resource)
		{
			SetMapResourcePath("mapTexturePath", resource, "更换地图背景", rebuildPreview: true);
		});
		_mapPropertyBinding.BindToggle(root.GetNode<CheckBox>("%IsNightCheck"), map, "isNight", delegate
		{
			RefreshMapField("isNight");
		}, this, "RefreshMapEditorFromHistory");
		_mapPropertyBinding.BindToggle(root.GetNode<CheckBox>("%UseSunFallCheck"), map, "useSunFall", delegate
		{
			RefreshMapField("useSunFall");
		}, this, "RefreshMapEditorFromHistory");
		_flagsSummaryLabel = root.GetNode<Label>("%FlagsSummaryLabel");
		BindVector2Field(root, "MapSize", map, "mapSize", delegate
		{
			RefreshMapField("mapSize", refreshOverlay: true, rebuildPreview: true);
		});
		BindVector2Field(root, "MapOffset", map, "mapOffset", delegate
		{
			RefreshMapField("mapOffset");
		});
		_mapPropertyBinding.BindNumber(root.GetNode<SpinBox>("%PlantOffsetSpinBox"), map, "plantOffset", delegate
		{
			RefreshMapField("plantOffset");
		}, this, "RefreshMapEditorFromHistory");
		BindVector2IField(root, "GridNum", map, "gridNum", delegate
		{
			RefreshMapField("gridNum");
		});
		BindVector2Field(root, "GridBegin", map, "gridBeginPos", delegate
		{
			RefreshMapField("gridBeginPos");
		});
		BindVector2Field(root, "GridSize", map, "gridSize", delegate
		{
			RefreshMapField("gridSize");
		});
		BindVector4Field(root, map, "edge", delegate
		{
			RefreshMapField("edge");
		});
		_gridSummaryLabel = root.GetNode<Label>("%GridSummaryLabel");
		_edgeSummaryLabel = root.GetNode<Label>("%EdgeSummaryLabel");
		_lineUseHost = root.GetNode<FlowContainer>("%LineUseHost");
		_specialRulesHost = root.GetNode<VBoxContainer>("%SpecialRulesHost");
		_specialRulesSummary = root.GetNode<Label>("%SpecialRulesSummary");
		root.GetNode<Button>("%AddRuleButton").Pressed += AddSpecialRule;
		RebuildLineUseControls();
		RebuildSpecialRuleControls();
		UpdateMapSummary();
	}

	private XWResourcePicker MountMapResourcePicker(Control host, string name, string baseType, Resource value, Action<Resource> changed)
	{
		XWResourcePicker picker = XWResourcePicker.Create();
		picker.Name = name;
		picker.Setup(baseType);
		picker.SetEditedResource(value);
		picker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		picker.ResourceChanged += delegate(Resource resource)
		{
			changed?.Invoke(resource);
		};
		host.AddChild(picker, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Name = name + "CatalogButton",
			Text = "图鉴",
			TooltipText = "搜索游戏与当前 Mod 资源",
			CustomMinimumSize = new Vector2(72f, 34f)
		};
		button.Pressed += delegate
		{
			EnsureMapVisualPicker();
			_mapVisualPicker?.OpenResourceLibrary(baseType, (baseType == "Texture2D") ? "地图背景" : "地图场景", value?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { baseType }, new string[3] { "Resources", "Scenes", "Asset" }, (baseType == "Texture2D") ? "res://addons/ModEditor/Icons/ClassIcon/Image.svg" : "res://addons/ModEditor/Icons/PackedScene.svg", delegate(XWGameplayResourceChoice choice)
			{
				Resource resource = (value = ResourceLoader.Load<Resource>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse));
				picker.SetEditedResource(resource);
				changed?.Invoke(resource);
			});
		};
		host.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		return picker;
	}

	private void OpenMapRulePacketTypeCatalog()
	{
		if (!GodotObject.IsInstanceValid(_mapRulePacketType))
		{
			return;
		}
		EnsureMapVisualPicker();
		List<XWGameplayResourceChoice> list = new List<XWGameplayResourceChoice>();
		TowerDefenseEnum.PACKET_TYPE[] values = Enum.GetValues<TowerDefenseEnum.PACKET_TYPE>();
		for (int i = 0; i < values.Length; i++)
		{
			TowerDefenseEnum.PACKET_TYPE pACKET_TYPE = values[i];
			int num = (int)pACKET_TYPE;
			list.Add(new XWGameplayResourceChoice(num.ToString(), pACKET_TYPE.ToString(), $"PACKET_TYPE/{pACKET_TYPE}", ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCard.svg", null, ResourceLoader.CacheMode.Reuse), XWGameplayResourceKind.Resource, IsModResource: false));
		}
		_mapVisualPicker?.OpenChoices("卡牌类型图鉴", "搜索运行时规则要测试的卡牌类型", _mapRulePacketType.GetSelectedId().ToString(), list, delegate(XWGameplayResourceChoice choice)
		{
			if (int.TryParse(choice?.Key, out var result))
			{
				int itemIndex = _mapRulePacketType.GetItemIndex(result);
				if (itemIndex >= 0)
				{
					_mapRulePacketType.Select(itemIndex);
					_mapRulePacketTypeCatalogButton.Text = $"卡牌类型图鉴 · {(TowerDefenseEnum.PACKET_TYPE)result}";
					_mapRulePacketType.EmitSignal(OptionButton.SignalName.ItemSelected, itemIndex);
				}
			}
		});
	}

	private void MountMapSegmentedOption(OptionButton option, HFlowContainer host)
	{
		if (!GodotObject.IsInstanceValid(option) || !GodotObject.IsInstanceValid(host) || option.ItemCount < 2 || option.ItemCount > 8)
		{
			if (GodotObject.IsInstanceValid(option))
			{
				option.Visible = true;
			}
		}
		else
		{
			XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(option, host);
			xWVisualSegmentedOption.Rebuild();
			_mapSegmentedOptions.Add(xWVisualSegmentedOption);
		}
	}

	private void EnsureMapVisualPicker()
	{
		if (!GodotObject.IsInstanceValid(_mapVisualPicker))
		{
			_mapVisualPicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_mapVisualPicker))
			{
				AddChild(_mapVisualPicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void DisposeMapVisualChoices()
	{
		foreach (XWVisualSegmentedOption mapSegmentedOption in _mapSegmentedOptions)
		{
			mapSegmentedOption?.Dispose();
		}
		_mapSegmentedOptions.Clear();
	}

	private void BindVector2Field(VBoxContainer root, string prefix, Resource resource, StringName property, Action refresh)
	{
		SpinBox x = root.GetNode<SpinBox>("%" + prefix + "X");
		SpinBox y = root.GetNode<SpinBox>("%" + prefix + "Y");
		Vector2 vector = resource.Get(property).AsVector2();
		x.Value = vector.X;
		y.Value = vector.Y;
		BindCompositeField(resource, property, new Godot.Range[2] { x, y }, () => Variant.From<Vector2>(new Vector2((float)x.Value, (float)y.Value)), refresh);
	}

	private void BindVector2IField(VBoxContainer root, string prefix, Resource resource, StringName property, Action refresh)
	{
		SpinBox x = root.GetNode<SpinBox>("%" + prefix + "X");
		SpinBox y = root.GetNode<SpinBox>("%" + prefix + "Y");
		x.MinValue = 1.0;
		y.MinValue = 1.0;
		x.MaxValue = 50.0;
		y.MaxValue = 50.0;
		Vector2I vector2I = resource.Get(property).AsVector2I();
		x.Value = vector2I.X;
		y.Value = vector2I.Y;
		BindCompositeField(resource, property, new Godot.Range[2] { x, y }, () => Variant.From<Vector2I>(new Vector2I(Mathf.RoundToInt((float)x.Value), Mathf.RoundToInt((float)y.Value))), refresh);
	}

	private void BindVector4Field(VBoxContainer root, Resource resource, StringName property, Action refresh)
	{
		SpinBox left = root.GetNode<SpinBox>("%EdgeLeft");
		SpinBox top = root.GetNode<SpinBox>("%EdgeTop");
		SpinBox right = root.GetNode<SpinBox>("%EdgeRight");
		SpinBox bottom = root.GetNode<SpinBox>("%EdgeBottom");
		Vector4 vector = resource.Get(property).AsVector4();
		left.Value = vector.X;
		top.Value = vector.Y;
		right.Value = vector.Z;
		bottom.Value = vector.W;
		BindCompositeField(resource, property, new Godot.Range[4] { left, top, right, bottom }, () => Variant.From<Vector4>(ReadVector4(left, top, right, bottom)), refresh);
	}

	private void BindCompositeField(Resource resource, StringName property, Godot.Range[] controls, Func<Variant> readValue, Action refresh)
	{
		foreach (Godot.Range obj in controls)
		{
			obj.FocusEntered += delegate
			{
				_mapPropertyBinding?.BeginEdit(resource, property);
			};
			obj.ValueChanged += delegate
			{
				if (!_updatingControls && _mapPropertyBinding != null)
				{
					_mapPropertyBinding.PreviewValue(resource, property, readValue());
					refresh?.Invoke();
				}
			};
			obj.FocusExited += delegate
			{
				if (!_updatingControls && _mapPropertyBinding != null)
				{
					_mapPropertyBinding.CommitEdit(resource, property, readValue(), $"修改 {property}", this, "RefreshMapEditorFromHistory");
					refresh?.Invoke();
				}
			};
		}
	}

	private void DisposeMapPropertyBinding()
	{
		_mapPropertyBinding?.Dispose();
		_mapPropertyBinding = null;
		_lineUseHost = null;
		_specialRulesHost = null;
		_specialRulesSummary = null;
	}

	private void OnMapVisualPropertyEdited(bool committed)
	{
		if (base.CurrentResource is TowerDefenseMapConfig towerDefenseMapConfig && towerDefenseMapConfig == _editingMap)
		{
			MarkCurrentResourceDirty();
			towerDefenseMapConfig.EmitChanged();
			if (committed)
			{
				towerDefenseMapConfig.NotifyPropertyListChanged();
			}
			if (GodotObject.IsInstanceValid(_mapSaveTimer))
			{
				_mapSaveTimer.Start();
			}
			UpdateMapSummary();
		}
	}

	private void RefreshMapField(string propertyName, bool refreshOverlay = true, bool rebuildPreview = false)
	{
		if (!_updatingControls && GodotObject.IsInstanceValid(_editingMap))
		{
			if (propertyName == "gridNum")
			{
				RebuildLineUseControls();
			}
			UpdateMapSummary();
			if (rebuildPreview)
			{
				RebuildMapScenePreview();
			}
			if (propertyName == "mapSize")
			{
				ResetMapPreviewView();
			}
			else if (propertyName == "mapOffset")
			{
				UpdateMapCanvasTransform();
			}
			if (refreshOverlay)
			{
				QueueGridOverlayRedraw();
			}
		}
	}

	private void SetMapResourcePath(string propertyName, Resource resource, string actionName, bool rebuildPreview)
	{
		if (_mapPropertyBinding != null && GodotObject.IsInstanceValid(_editingMap))
		{
			string text = (GodotObject.IsInstanceValid(resource) ? (resource.ResourcePath ?? "") : "");
			_mapPropertyBinding.SetValue(_editingMap, propertyName, text, actionName, this, "RefreshMapEditorFromHistory");
			_editingMap.ClearLoadedMapResources();
			RefreshMapField(propertyName, refreshOverlay: true, rebuildPreview);
		}
	}

	private void RebuildLineUseControls()
	{
		if (!GodotObject.IsInstanceValid(_lineUseHost) || !GodotObject.IsInstanceValid(_editingMap))
		{
			return;
		}
		ClearDynamicChildren(_lineUseHost);
		int num = Math.Clamp(_editingMap.gridNum.Y, 1, 50);
		for (int i = 1; i <= num; i++)
		{
			int capturedLine = i;
			CheckButton checkButton = new CheckButton
			{
				Text = $"第 {i} 行",
				ButtonPressed = (_editingMap.lineUse?.Contains(i) ?? false),
				TooltipText = $"控制游戏地图第 {i} 行能否出怪和放置角色"
			};
			checkButton.Toggled += delegate(bool enabled)
			{
				SetLineEnabled(capturedLine, enabled);
			};
			_lineUseHost.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void SetLineEnabled(int line, bool enabled)
	{
		if (_updatingControls || _mapPropertyBinding == null || !GodotObject.IsInstanceValid(_editingMap))
		{
			return;
		}
		Array<int> array = ((_editingMap.lineUse == null) ? new Array<int>() : new Array<int>(_editingMap.lineUse));
		bool flag = array.Contains(line);
		if (enabled != flag)
		{
			if (enabled)
			{
				array.Add(line);
			}
			else
			{
				array.Remove(line);
			}
			_mapPropertyBinding.SetValue(_editingMap, "lineUse", array, enabled ? $"启用地图第 {line} 行" : $"停用地图第 {line} 行", this, "RefreshMapEditorFromHistory");
			RebuildLineUseControls();
			RefreshMapField("lineUse");
		}
	}

	private void RebuildSpecialRuleControls()
	{
		if (!GodotObject.IsInstanceValid(_specialRulesHost) || !GodotObject.IsInstanceValid(_editingMap))
		{
			return;
		}
		ClearDynamicChildren(_specialRulesHost);
		int num = _editingMap.specialRules?.Count ?? 0;
		if (GodotObject.IsInstanceValid(_specialRulesSummary))
		{
			_specialRulesSummary.Text = ((num == 0) ? "未配置特殊规则；地图只使用基础运行逻辑。" : $"已配置 {num} 条规则：{BuildSpecialRuleSummary(_editingMap)}");
		}
		for (int i = 0; i < num; i++)
		{
			int capturedIndex = i;
			TowerDefenseMapRuleConfig rule = _editingMap.specialRules[i];
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = $"SpecialRule_{i}",
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			hBoxContainer.AddChild(new Label
			{
				Text = $"{i + 1}.",
				CustomMinimumSize = new Vector2(28f, 0f),
				VerticalAlignment = VerticalAlignment.Center
			}, forceReadableName: false, InternalMode.Disabled);
			XWResourcePicker xWResourcePicker = XWResourcePicker.Create();
			xWResourcePicker.Name = $"SpecialRulePicker_{i}";
			xWResourcePicker.Setup("TowerDefenseMapRuleConfig");
			xWResourcePicker.SetEditedResource(rule);
			xWResourcePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			xWResourcePicker.ResourceChanged += delegate(Resource resource)
			{
				ReplaceSpecialRule(capturedIndex, resource as TowerDefenseMapRuleConfig);
			};
			xWResourcePicker.ResourceSelected += delegate(Resource resource)
			{
				OpenSpecialRule(capturedIndex, resource as TowerDefenseMapRuleConfig);
			};
			hBoxContainer.AddChild(xWResourcePicker, forceReadableName: false, InternalMode.Disabled);
			hBoxContainer.AddChild(CreateCompactRuleButton("↑", "规则上移", delegate
			{
				MoveSpecialRule(capturedIndex, -1);
			}, i == 0), forceReadableName: false, InternalMode.Disabled);
			hBoxContainer.AddChild(CreateCompactRuleButton("↓", "规则下移", delegate
			{
				MoveSpecialRule(capturedIndex, 1);
			}, i == num - 1), forceReadableName: false, InternalMode.Disabled);
			hBoxContainer.AddChild(CreateCompactRuleButton("编辑", "在规则运行画面中编辑", delegate
			{
				OpenSpecialRule(capturedIndex, rule);
			}), forceReadableName: false, InternalMode.Disabled);
			hBoxContainer.AddChild(CreateCompactRuleButton("×", "移除规则", delegate
			{
				RemoveSpecialRule(capturedIndex);
			}), forceReadableName: false, InternalMode.Disabled);
			_specialRulesHost.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static Button CreateCompactRuleButton(string text, string tooltip, Action pressed, bool disabled = false)
	{
		Button button = new Button();
		button.Text = text;
		button.TooltipText = tooltip;
		button.FocusMode = FocusModeEnum.None;
		button.Disabled = disabled;
		button.CustomMinimumSize = new Vector2((text == "编辑") ? 52 : 32, 0f);
		button.Pressed += delegate
		{
			pressed?.Invoke();
		};
		return button;
	}

	private void AddSpecialRule()
	{
		if (GodotObject.IsInstanceValid(_editingMap))
		{
			int num = _editingMap.specialRules?.Count ?? 0;
			TowerDefenseMapRuleConfig item = new TowerDefenseMapRuleConfig
			{
				ResourceName = $"MapRule_{num + 1}",
				id = new StringName($"map_rule_{num + 1}")
			};
			Array<TowerDefenseMapRuleConfig> array = ((_editingMap.specialRules == null) ? new Array<TowerDefenseMapRuleConfig>() : new Array<TowerDefenseMapRuleConfig>(_editingMap.specialRules));
			array.Add(item);
			SetSpecialRules(array, "添加地图特殊规则");
		}
	}

	private void ReplaceSpecialRule(int index, TowerDefenseMapRuleConfig rule)
	{
		if (GodotObject.IsInstanceValid(_editingMap) && _editingMap.specialRules != null && index >= 0 && index < _editingMap.specialRules.Count)
		{
			Array<TowerDefenseMapRuleConfig> array = new Array<TowerDefenseMapRuleConfig>(_editingMap.specialRules);
			array[index] = rule;
			SetSpecialRules(array, "替换地图特殊规则");
		}
	}

	private void RemoveSpecialRule(int index)
	{
		if (GodotObject.IsInstanceValid(_editingMap) && _editingMap.specialRules != null && index >= 0 && index < _editingMap.specialRules.Count)
		{
			Array<TowerDefenseMapRuleConfig> array = new Array<TowerDefenseMapRuleConfig>(_editingMap.specialRules);
			array.RemoveAt(index);
			SetSpecialRules(array, "移除地图特殊规则");
		}
	}

	private void MoveSpecialRule(int index, int direction)
	{
		if (GodotObject.IsInstanceValid(_editingMap) && _editingMap.specialRules != null)
		{
			int num = index + direction;
			if (index >= 0 && index < _editingMap.specialRules.Count && num >= 0 && num < _editingMap.specialRules.Count)
			{
				Array<TowerDefenseMapRuleConfig> array = new Array<TowerDefenseMapRuleConfig>(_editingMap.specialRules);
				TowerDefenseMapRuleConfig item = array[index];
				array.RemoveAt(index);
				array.Insert(num, item);
				SetSpecialRules(array, "调整地图特殊规则顺序");
			}
		}
	}

	private void SetSpecialRules(Array<TowerDefenseMapRuleConfig> next, string actionName)
	{
		if (_mapPropertyBinding != null && GodotObject.IsInstanceValid(_editingMap))
		{
			_mapPropertyBinding.SetValue(_editingMap, "specialRules", next, actionName, this, "RefreshMapEditorFromHistory");
			RebuildSpecialRuleControls();
			RefreshMapField("specialRules", refreshOverlay: false);
		}
	}

	private void OpenSpecialRule(int index, TowerDefenseMapRuleConfig rule)
	{
		if (GodotObject.IsInstanceValid(rule) && GodotObject.IsInstanceValid(_editingMap))
		{
			XWResourceEditContext context = XWResourceEditContext.ForProperty(rule, _editingMap, rule.ResourcePath, base.CurrentResourcePath, "specialRules", index, "map_editor", base.CurrentEditContext?.IsBuiltInSource ?? false, $"map_rule_{index}");
			XWEditorInterface.Instance?.EditResource(rule, context);
		}
	}

	public void RefreshMapEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || (!xWUndoRedoManager.IsUndoing() && !xWUndoRedoManager.IsRedoing()) || !(base.CurrentResource is TowerDefenseMapConfig towerDefenseMapConfig) || towerDefenseMapConfig != _editingMap)
		{
			return;
		}
		MarkCurrentResourceDirty();
		towerDefenseMapConfig.EmitChanged();
		towerDefenseMapConfig.ClearLoadedMapResources();
		if (GodotObject.IsInstanceValid(_mapSaveTimer))
		{
			_mapSaveTimer.Start();
		}
		DisposeMapPropertyBinding();
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderMapEditor(towerDefenseMapConfig);
	}

	private static void ClearDynamicChildren(Node host)
	{
		foreach (Node child in host.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void RebuildMapScenePreview()
	{
		if (!GodotObject.IsInstanceValid(_mapScenePreviewRoot) || _editingMap == null)
		{
			return;
		}
		foreach (Node child in _mapScenePreviewRoot.GetChildren())
		{
			child.QueueFree();
		}
		UpdateMapSceneViewportSize();
		UpdateMapCanvasTransform();
		Node node = null;
		PackedScene packedScene = ResolveMapPreviewScene(_editingMap);
		if (GodotObject.IsInstanceValid(packedScene))
		{
			try
			{
				node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Map editor scene preview failed: " + ex.Message);
			}
		}
		if (node != null)
		{
			node.Name = ((!string.IsNullOrWhiteSpace(_editingMap.mapScenePath)) ? "ConfiguredMapScenePreview" : "DefaultFrontlawnScenePreview");
			_mapScenePreviewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		Texture2D mapTexture = _editingMap.GetMapTexture(cache: false);
		if (GodotObject.IsInstanceValid(mapTexture))
		{
			_mapScenePreviewRoot.AddChild(new Sprite2D
			{
				Name = "MapTextureFallbackPreview",
				Texture = mapTexture,
				Centered = false
			}, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			Label node2 = new Label
			{
				Name = "MapEmptyPreviewLabel",
				Text = "未配置 mapScene 或 mapTexture"
			};
			_mapScenePreviewRoot.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
		}
		UpdateMapCanvasTransform();
		QueueGridOverlayRedraw();
	}

	private void ResetMapPreviewView()
	{
		if (_editingMap != null)
		{
			Vector2 mapPreviewViewSize = GetMapPreviewViewSize();
			Vector2 vector = new Vector2(Mathf.Max(1f, _editingMap.mapSize.X), Mathf.Max(1f, _editingMap.mapSize.Y));
			float value = Mathf.Min(mapPreviewViewSize.X / vector.X, mapPreviewViewSize.Y / vector.Y) * 0.96f;
			MapCanvasZoom = Mathf.Clamp(value, 0.05f, 12f);
			MapCanvasOffset = (mapPreviewViewSize - vector * MapCanvasZoom) * 0.5f;
			UpdateMapCanvasTransform();
			QueueGridOverlayRedraw();
		}
	}

	private void SetMapCanvasZoom(float zoom, Vector2 pivot)
	{
		float num = Mathf.Clamp(zoom, 0.05f, 12f);
		if (!Mathf.IsEqualApprox(num, MapCanvasZoom))
		{
			Vector2 vector = MapViewToWorld(pivot);
			MapCanvasZoom = num;
			MapCanvasOffset = pivot - vector * MapCanvasZoom;
			UpdateMapCanvasTransform();
			QueueGridOverlayRedraw();
		}
	}

	private void UpdateMapCanvasTransform()
	{
		UpdateMapSceneViewportSize();
		if (_editingMap != null && GodotObject.IsInstanceValid(_mapScenePreviewRoot))
		{
			_mapScenePreviewRoot.Position = MapWorldToView(_editingMap.mapOffset);
			_mapScenePreviewRoot.Scale = Vector2.One * MapCanvasZoom;
		}
	}

	private Vector2 MapWorldToView(Vector2 world)
	{
		return world * MapCanvasZoom + MapCanvasOffset;
	}

	private Vector2 MapViewToWorld(Vector2 view)
	{
		float num = Mathf.Max(MapCanvasZoom, 0.001f);
		return (view - MapCanvasOffset) / num;
	}

	private void UpdateMapSceneViewportSize()
	{
		if (GodotObject.IsInstanceValid(_mapSceneViewport))
		{
			Vector2 mapPreviewViewSize = GetMapPreviewViewSize();
			_mapSceneViewport.Size = new Vector2I(Mathf.Max(1, Mathf.RoundToInt(mapPreviewViewSize.X)), Mathf.Max(1, Mathf.RoundToInt(mapPreviewViewSize.Y)));
		}
	}

	private Vector2 GetMapPreviewViewSize()
	{
		if (GodotObject.IsInstanceValid(_mapGridOverlay) && _mapGridOverlay.Size.X > 1f && _mapGridOverlay.Size.Y > 1f)
		{
			return _mapGridOverlay.Size;
		}
		if (GodotObject.IsInstanceValid(_mapPreviewStack) && _mapPreviewStack.Size.X > 1f && _mapPreviewStack.Size.Y > 1f)
		{
			return _mapPreviewStack.Size;
		}
		return new Vector2(700f, 330f);
	}

	private void DrawMapGridOverlay(Control overlay)
	{
		if (!GodotObject.IsInstanceValid(overlay) || _editingMap == null)
		{
			return;
		}
		Vector2 begin = MapWorldToView(_editingMap.mapOffset + _editingMap.gridBeginPos);
		Vector2 cellSize = _editingMap.gridSize * MapCanvasZoom;
		int num = Math.Clamp(_editingMap.gridNum.X, 1, 50);
		int num2 = Math.Clamp(_editingMap.gridNum.Y, 1, 50);
		Color color = new Color(0.12f, 0.5f, 0.22f, 0.16f);
		Color color2 = new Color(0.5f, 0.95f, 0.58f, 0.72f);
		Color color3 = new Color(0.35f, 0.72f, 1f, 0.25f);
		Color color4 = new Color(1f, 0.72f, 0.18f, 0.88f);
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Rect2 cellRect = GetCellRect(j, i, begin, cellSize);
				overlay.DrawRect(cellRect, color);
				overlay.DrawRect(cellRect, color2, filled: false, 1f);
			}
		}
		DrawConfiguredCellRects(overlay, begin, cellSize, color3);
		Rect2 rect = new Rect2(MapWorldToView(_editingMap.mapOffset + new Vector2(_editingMap.edge.X, _editingMap.edge.Y)), new Vector2(Math.Max(0f, _editingMap.edge.Z - _editingMap.edge.X), Math.Max(0f, _editingMap.edge.W - _editingMap.edge.Y)) * MapCanvasZoom);
		overlay.DrawRect(rect, color4, filled: false, 2f);
		float y = MapWorldToView(_editingMap.mapOffset + new Vector2(0f, (float)((double)_editingMap.gridBeginPos.Y + _editingMap.plantOffset))).Y;
		overlay.DrawLine(new Vector2(0f, y), new Vector2(overlay.Size.X, y), new Color(0.35f, 0.72f, 1f, 0.9f), 1.5f);
		DrawCellSelection(overlay, begin, cellSize);
	}

	private void OnMapGridOverlayGuiInput(InputEvent inputEvent)
	{
		if (_editingMap == null || !GodotObject.IsInstanceValid(_mapGridOverlay))
		{
			return;
		}
		if (inputEvent is InputEventMouseButton inputEventMouseButton)
		{
			if (inputEventMouseButton.ButtonIndex == MouseButton.WheelUp && inputEventMouseButton.Pressed)
			{
				SetMapCanvasZoom(MapCanvasZoom * 1.1f, inputEventMouseButton.Position);
				_mapGridOverlay.AcceptEvent();
				return;
			}
			if (inputEventMouseButton.ButtonIndex == MouseButton.WheelDown && inputEventMouseButton.Pressed)
			{
				SetMapCanvasZoom(MapCanvasZoom / 1.1f, inputEventMouseButton.Position);
				_mapGridOverlay.AcceptEvent();
				return;
			}
			MouseButton buttonIndex = inputEventMouseButton.ButtonIndex;
			if (((ulong)(buttonIndex - 2) <= 1uL) ? true : false)
			{
				MapCanvasPan = inputEventMouseButton.Pressed;
				MapCanvasLastPointer = inputEventMouseButton.Position;
				_mapGridOverlay.AcceptEvent();
			}
			else
			{
				if (inputEventMouseButton.ButtonIndex != MouseButton.Left)
				{
					return;
				}
				Vector2I cellAtPosition = GetCellAtPosition(inputEventMouseButton.Position);
				if (inputEventMouseButton.Pressed)
				{
					if (cellAtPosition.X >= 0)
					{
						StartGridBoxSelection(cellAtPosition);
						_mapGridOverlay.AcceptEvent();
					}
				}
				else if (_isGridBoxSelecting)
				{
					if (cellAtPosition.X >= 0)
					{
						UpdateGridBoxSelection(cellAtPosition);
					}
					FinishGridBoxSelection();
					_mapGridOverlay.AcceptEvent();
				}
			}
		}
		else
		{
			if (!(inputEvent is InputEventMouseMotion inputEventMouseMotion))
			{
				return;
			}
			if (MapCanvasPan)
			{
				PanMapCanvasTo(inputEventMouseMotion.Position);
			}
			else if (_isGridBoxSelecting)
			{
				Vector2I cellAtPosition2 = GetCellAtPosition(inputEventMouseMotion.Position);
				if (cellAtPosition2.X >= 0)
				{
					UpdateGridBoxSelection(cellAtPosition2);
					_mapGridOverlay.AcceptEvent();
				}
			}
		}
	}

	private void PanMapCanvasTo(Vector2 position)
	{
		MapCanvasOffset += position - MapCanvasLastPointer;
		MapCanvasLastPointer = position;
		UpdateMapCanvasTransform();
		QueueGridOverlayRedraw();
		_mapGridOverlay.AcceptEvent();
	}

	private void StartGridBoxSelection(Vector2I cell)
	{
		_isGridBoxSelecting = true;
		_gridSelectionStart = cell;
		_gridSelectionEnd = cell;
		MapGridSelectionRect = NormalizeCellSelection(_gridSelectionStart, _gridSelectionEnd);
		UpdateCellSelectionLabel();
		QueueGridOverlayRedraw();
	}

	private void UpdateGridBoxSelection(Vector2I cell)
	{
		_gridSelectionEnd = cell;
		MapGridSelectionRect = NormalizeCellSelection(_gridSelectionStart, _gridSelectionEnd);
		UpdateCellSelectionLabel();
		QueueGridOverlayRedraw();
	}

	private void FinishGridBoxSelection()
	{
		_isGridBoxSelecting = false;
		if (MapGridSelectionRect.Size.X > 0 && MapGridSelectionRect.Size.Y > 0)
		{
			if (UseCellBrushTool)
			{
				ApplyBrushToSelection();
			}
			else
			{
				ApplySelectedCellConfig();
			}
		}
		UpdateCellSelectionLabel();
		QueueGridOverlayRedraw();
	}

	private void ApplyBrushToSelection()
	{
		if (_editingMap == null || _mapPropertyBinding == null || MapGridSelectionRect.Size.X <= 0 || MapGridSelectionRect.Size.Y <= 0)
		{
			return;
		}
		Array<TowerDefenseCellConfig> array = CloneCellConfigs(_editingMap.cellConfig);
		for (int i = MapGridSelectionRect.Position.Y; i < MapGridSelectionRect.Position.Y + MapGridSelectionRect.Size.Y; i++)
		{
			for (int j = MapGridSelectionRect.Position.X; j < MapGridSelectionRect.Position.X + MapGridSelectionRect.Size.X; j++)
			{
				PaintBrushAtCell(array, new Vector2I(j, i));
			}
		}
		SetCellConfigs(array, "绘制地图格子");
	}

	private void PaintBrushAtCell(Array<TowerDefenseCellConfig> cellConfigs, Vector2I cell)
	{
		if (cellConfigs == null || cell.X < 0 || cell.Y < 0)
		{
			return;
		}
		Vector4I vector4I = new Vector4I(cell.X + 1, cell.Y + 1, cell.X + 1, cell.Y + 1);
		TowerDefenseCellConfig towerDefenseCellConfig = null;
		foreach (TowerDefenseCellConfig cellConfig in cellConfigs)
		{
			if (GodotObject.IsInstanceValid(cellConfig) && cellConfig.pos == vector4I)
			{
				towerDefenseCellConfig = cellConfig;
				break;
			}
		}
		if (!GodotObject.IsInstanceValid(towerDefenseCellConfig))
		{
			towerDefenseCellConfig = new TowerDefenseCellConfig
			{
				ResourceName = $"Cell_{vector4I.X}_{vector4I.Y}",
				pos = vector4I
			};
			cellConfigs.Add(towerDefenseCellConfig);
		}
		SetCellConfigBrushData(towerDefenseCellConfig);
	}

	private void SetCellConfigBrushData(TowerDefenseCellConfig config)
	{
		if (GodotObject.IsInstanceValid(config))
		{
			config.gridType = BuildBrushGridTypeArray();
			config.ElementFlags = (GodotObject.IsInstanceValid(_brushElementFlagsSpinBox) ? Mathf.RoundToInt((float)_brushElementFlagsSpinBox.Value) : 0);
		}
	}

	private Array<TowerDefenseEnum.PLANTGRIDTYPE> BuildBrushGridTypeArray()
	{
		Array<TowerDefenseEnum.PLANTGRIDTYPE> array = new Array<TowerDefenseEnum.PLANTGRIDTYPE>();
		if (GodotObject.IsInstanceValid(_brushGroundCheckBox) && _brushGroundCheckBox.ButtonPressed)
		{
			array.Add(TowerDefenseEnum.PLANTGRIDTYPE.GROUND);
		}
		if (GodotObject.IsInstanceValid(_brushAirCheckBox) && _brushAirCheckBox.ButtonPressed)
		{
			array.Add(TowerDefenseEnum.PLANTGRIDTYPE.AIR);
		}
		if (GodotObject.IsInstanceValid(_brushWaterCheckBox) && _brushWaterCheckBox.ButtonPressed)
		{
			array.Add(TowerDefenseEnum.PLANTGRIDTYPE.WATER);
		}
		if (array.Count == 0)
		{
			array.Add(TowerDefenseEnum.PLANTGRIDTYPE.NOONE);
		}
		return array;
	}

	private void ApplySelectedCellConfig()
	{
		if (_editingMap != null && _mapPropertyBinding != null && MapGridSelectionRect.Size.X > 0 && MapGridSelectionRect.Size.Y > 0)
		{
			Array<TowerDefenseCellConfig> array = CloneCellConfigs(_editingMap.cellConfig);
			if (EnsureCellConfigForSelection(array, MapGridSelectionRect))
			{
				SetCellConfigs(array, "添加地图格子区域");
			}
		}
	}

	private static bool EnsureCellConfigForSelection(Array<TowerDefenseCellConfig> cellConfigs, Rect2I selection)
	{
		Vector4I vector4I = ToCellConfigPos(selection);
		foreach (TowerDefenseCellConfig cellConfig in cellConfigs)
		{
			if (GodotObject.IsInstanceValid(cellConfig) && cellConfig.pos == vector4I)
			{
				return false;
			}
		}
		cellConfigs.Add(new TowerDefenseCellConfig
		{
			ResourceName = $"Cell_{vector4I.X}_{vector4I.Y}_{vector4I.Z}_{vector4I.W}",
			pos = vector4I
		});
		return true;
	}

	private static Array<TowerDefenseCellConfig> CloneCellConfigs(Array<TowerDefenseCellConfig> source)
	{
		Array<TowerDefenseCellConfig> array = new Array<TowerDefenseCellConfig>();
		if (source == null)
		{
			return array;
		}
		foreach (TowerDefenseCellConfig item in source)
		{
			if (!GodotObject.IsInstanceValid(item))
			{
				array.Add(item);
			}
			else
			{
				array.Add((item.Duplicate(deep: true) as TowerDefenseCellConfig) ?? item);
			}
		}
		return array;
	}

	private void SetCellConfigs(Array<TowerDefenseCellConfig> next, string actionName)
	{
		if (_mapPropertyBinding != null && GodotObject.IsInstanceValid(_editingMap))
		{
			_mapPropertyBinding.SetValue(_editingMap, "cellConfig", next, actionName, this, "RefreshMapEditorFromHistory");
			RefreshMapField("cellConfig");
		}
	}

	private Vector2I GetCellAtPosition(Vector2 position)
	{
		if (_editingMap == null || !GodotObject.IsInstanceValid(_mapGridOverlay))
		{
			return new Vector2I(-1, -1);
		}
		if (Mathf.IsZeroApprox(MapCanvasZoom) || Mathf.IsZeroApprox(_editingMap.gridSize.X) || Mathf.IsZeroApprox(_editingMap.gridSize.Y))
		{
			return new Vector2I(-1, -1);
		}
		Vector2 vector = MapViewToWorld(position) - _editingMap.mapOffset - _editingMap.gridBeginPos;
		int num = Mathf.FloorToInt(vector.X / _editingMap.gridSize.X);
		int num2 = Mathf.FloorToInt(vector.Y / _editingMap.gridSize.Y);
		int num3 = Math.Clamp(_editingMap.gridNum.X, 1, 50);
		int num4 = Math.Clamp(_editingMap.gridNum.Y, 1, 50);
		if (num < 0 || num2 < 0 || num >= num3 || num2 >= num4)
		{
			return new Vector2I(-1, -1);
		}
		return new Vector2I(num, num2);
	}

	private static Rect2 GetCellRect(int x, int y, Vector2 begin, Vector2 cellSize)
	{
		return new Rect2(begin + new Vector2((float)x * cellSize.X, (float)y * cellSize.Y), cellSize);
	}

	private static Rect2I NormalizeCellSelection(Vector2I a, Vector2I b)
	{
		int num = Math.Min(a.X, b.X);
		int num2 = Math.Min(a.Y, b.Y);
		int num3 = Math.Max(a.X, b.X);
		int num4 = Math.Max(a.Y, b.Y);
		return new Rect2I(num, num2, num3 - num + 1, num4 - num2 + 1);
	}

	private static Vector4I ToCellConfigPos(Rect2I selection)
	{
		return new Vector4I(selection.Position.X + 1, selection.Position.Y + 1, selection.Position.X + selection.Size.X, selection.Position.Y + selection.Size.Y);
	}

	private void DrawConfiguredCellRects(Control overlay, Vector2 begin, Vector2 cellSize, Color color)
	{
		if (_editingMap?.cellConfig == null)
		{
			return;
		}
		foreach (TowerDefenseCellConfig item in _editingMap.cellConfig)
		{
			if (!GodotObject.IsInstanceValid(item))
			{
				continue;
			}
			Rect2I rect2I = RectFromCellConfigPos(item.pos);
			if (rect2I.Size.X <= 0 || rect2I.Size.Y <= 0)
			{
				continue;
			}
			Color cellConfigColor = GetCellConfigColor(item);
			for (int i = rect2I.Position.Y; i < rect2I.Position.Y + rect2I.Size.Y; i++)
			{
				for (int j = rect2I.Position.X; j < rect2I.Position.X + rect2I.Size.X; j++)
				{
					overlay.DrawRect(GetCellRect(j, i, begin, cellSize), cellConfigColor);
				}
			}
		}
	}

	private void DrawCellSelection(Control overlay, Vector2 begin, Vector2 cellSize)
	{
		if (MapGridSelectionRect.Size.X > 0 && MapGridSelectionRect.Size.Y > 0)
		{
			Rect2 rect = new Rect2(begin + new Vector2((float)MapGridSelectionRect.Position.X * cellSize.X, (float)MapGridSelectionRect.Position.Y * cellSize.Y), new Vector2((float)MapGridSelectionRect.Size.X * cellSize.X, (float)MapGridSelectionRect.Size.Y * cellSize.Y));
			DrawBrushPreviewCell(overlay, rect);
			overlay.DrawRect(rect, new Color(0.38f, 0.78f, 1f, 0.95f), filled: false, 2f);
		}
	}

	private void DrawBrushPreviewCell(Control overlay, Rect2 rect)
	{
		Color color = new Color(0.18f, 0.54f, 1f, 0.24f);
		Array<TowerDefenseEnum.PLANTGRIDTYPE> array = BuildBrushGridTypeArray();
		if (array.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER))
		{
			color = new Color(0.12f, 0.46f, 0.95f, 0.26f);
		}
		else if (array.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND) && array.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
		{
			color = new Color(0.18f, 0.68f, 0.32f, 0.24f);
		}
		else if (array.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
		{
			color = new Color(0.68f, 0.46f, 1f, 0.24f);
		}
		overlay.DrawRect(rect, color);
	}

	private static Color GetCellConfigColor(TowerDefenseCellConfig config)
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			return new Color(0.35f, 0.72f, 1f, 0.25f);
		}
		if (config.gridType != null && config.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER))
		{
			return new Color(0.08f, 0.42f, 0.95f, 0.3f);
		}
		if (config.gridType != null && config.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND) && config.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
		{
			return new Color(0.2f, 0.72f, 0.34f, 0.28f);
		}
		if (config.gridType != null && config.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND))
		{
			return new Color(0.38f, 0.76f, 0.28f, 0.3f);
		}
		if (config.gridType != null && config.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
		{
			return new Color(0.66f, 0.45f, 1f, 0.28f);
		}
		return new Color(0.35f, 0.72f, 1f, 0.25f);
	}

	private static Rect2I RectFromCellConfigPos(Vector4I pos)
	{
		int num = Math.Max(0, pos.X - 1);
		int num2 = Math.Max(0, pos.Y - 1);
		int num3 = Math.Max(num, pos.Z - 1);
		int num4 = Math.Max(num2, pos.W - 1);
		return new Rect2I(num, num2, num3 - num + 1, num4 - num2 + 1);
	}

	private void UpdateCellSelectionLabel()
	{
		if (GodotObject.IsInstanceValid(_cellSelectionLabel))
		{
			if (MapGridSelectionRect.Size.X <= 0 || MapGridSelectionRect.Size.Y <= 0)
			{
				_cellSelectionLabel.Text = BuildCellSelectionText();
				return;
			}
			Vector4I vector4I = ToCellConfigPos(MapGridSelectionRect);
			_cellSelectionLabel.Text = $"已选格子: ({vector4I.X},{vector4I.Y}) - ({vector4I.Z},{vector4I.W})，共 {MapGridSelectionRect.Size.X * MapGridSelectionRect.Size.Y} 格；{BuildBrushSummary()}";
		}
	}

	private void ApplyBrushGridPreset(int preset)
	{
		if (GodotObject.IsInstanceValid(_brushGroundCheckBox) && GodotObject.IsInstanceValid(_brushAirCheckBox) && GodotObject.IsInstanceValid(_brushWaterCheckBox))
		{
			CheckBox brushGroundCheckBox = _brushGroundCheckBox;
			bool buttonPressed = (uint)preset <= 1u;
			brushGroundCheckBox.ButtonPressed = buttonPressed;
			brushGroundCheckBox = _brushAirCheckBox;
			buttonPressed = ((preset == 0 || preset == 2) ? true : false);
			brushGroundCheckBox.ButtonPressed = buttonPressed;
			_brushWaterCheckBox.ButtonPressed = preset == 3;
			UpdateCellSelectionLabel();
			QueueGridOverlayRedraw();
		}
	}

	private string BuildCellSelectionText()
	{
		return "在预览中设置好格子位置后拖拽刷格子；" + BuildBrushSummary();
	}

	private string BuildBrushSummary()
	{
		Array<TowerDefenseEnum.PLANTGRIDTYPE> array = BuildBrushGridTypeArray();
		List<string> list = new List<string>();
		foreach (TowerDefenseEnum.PLANTGRIDTYPE item in array)
		{
			list.Add(item.ToString());
		}
		int value = (GodotObject.IsInstanceValid(_brushElementFlagsSpinBox) ? Mathf.RoundToInt((float)_brushElementFlagsSpinBox.Value) : 0);
		return $"刷子: {string.Join("+", list)}, ElementFlags={value}";
	}

	private void SavePendingMapResource()
	{
		if (GodotObject.IsInstanceValid(_editingMap))
		{
			SaveMapResource(_editingMap);
		}
	}

	private void SaveMapResource(TowerDefenseMapConfig map)
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !GodotObject.IsInstanceValid(map))
		{
			return;
		}
		Error error = ResourceSaver.Save(map, currentResourcePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushWarning($"Map editor save failed: {error} {currentResourcePath}");
		}
		else
		{
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWModManifestSyncService.RegisterPath(text, currentResourcePath);
			}
		}
	}

	private void UpdateMapSummary()
	{
		if (_editingMap == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_scenePathLabel))
			{
				_scenePathLabel.Text = BuildMapSceneLabel(_editingMap);
			}
			if (GodotObject.IsInstanceValid(_gridSummaryLabel))
			{
				_gridSummaryLabel.Text = $"格子: {_editingMap.gridNum.X} x {_editingMap.gridNum.Y}, 起点 {FormatVector(_editingMap.gridBeginPos)}, 大小 {FormatVector(_editingMap.gridSize)}";
			}
			if (GodotObject.IsInstanceValid(_gridSummaryLabel) && !_editingMap.TryValidateRuntime(out var reason))
			{
				Label gridSummaryLabel = _gridSummaryLabel;
				gridSummaryLabel.Text = gridSummaryLabel.Text + "\nRuntime validation: " + reason;
			}
			if (GodotObject.IsInstanceValid(_edgeSummaryLabel))
			{
				_edgeSummaryLabel.Text = $"边界: left={_editingMap.edge.X:0.##}, top={_editingMap.edge.Y:0.##}, right={_editingMap.edge.Z:0.##}, bottom={_editingMap.edge.W:0.##}";
			}
			if (GodotObject.IsInstanceValid(_flagsSummaryLabel))
			{
				_flagsSummaryLabel.Text = $"标记: 夜晚={FormatBool(_editingMap.isNight)}, 掉阳光={FormatBool(_editingMap.useSunFall)}, 特殊规则={BuildSpecialRuleSummary(_editingMap)}";
			}
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void QueueGridOverlayRedraw()
	{
		if (GodotObject.IsInstanceValid(_mapGridOverlay))
		{
			_mapGridOverlay.QueueRedraw();
		}
	}

	private void AddMapSummaryRows(TowerDefenseMapConfig map)
	{
		AddItemIfMissing(base.PreviewList, "地图场景 mapScene -> " + FormatResourcePath(map.mapScenePath));
		AddItemIfMissing(base.PreviewList, "背景贴图 mapTexture -> " + FormatResourcePath(map.mapTexturePath));
		AddItemIfMissing(base.PreviewList, "地图尺寸 mapSize -> " + FormatVector(map.mapSize));
		AddItemIfMissing(base.TimelineList, $"格子: {map.gridNum.X} x {map.gridNum.Y}, begin={FormatVector(map.gridBeginPos)}, size={FormatVector(map.gridSize)}");
		AddItemIfMissing(base.GraphList, "地图 -> mapScene -> " + FormatResourcePath(map.mapScenePath));
		AddItemIfMissing(base.GraphList, "地图 -> mapTexture -> " + FormatResourcePath(map.mapTexturePath));
		AddItemIfMissing(base.ReferenceList, "配置场景 mapScene -> " + FormatResourcePath(map.mapScenePath));
		AddItemIfMissing(base.ReferenceList, "背景贴图 mapTexture -> " + FormatResourcePath(map.mapTexturePath));
	}

	private static Vector4 ReadVector4(SpinBox left, SpinBox top, SpinBox right, SpinBox bottom)
	{
		return new Vector4((float)left.Value, (float)top.Value, (float)right.Value, (float)bottom.Value);
	}

	private static Vector2I GetViewportSize(TowerDefenseMapConfig map)
	{
		if (map == null)
		{
			return new Vector2I(1400, 600);
		}
		return new Vector2I(Mathf.Max(1, Mathf.RoundToInt(map.mapSize.X)), Mathf.Max(1, Mathf.RoundToInt(map.mapSize.Y)));
	}

	private static PackedScene ResolveMapPreviewScene(TowerDefenseMapConfig map)
	{
		if (map != null)
		{
			PackedScene mapScene = map.GetMapScene(cache: false);
			if (GodotObject.IsInstanceValid(mapScene))
			{
				return mapScene;
			}
		}
		return LoadDefaultFrontlawnScene();
	}

	private static PackedScene LoadDefaultFrontlawnScene()
	{
		if (GodotObject.IsInstanceValid(_defaultFrontlawnScene))
		{
			return _defaultFrontlawnScene;
		}
		if (!ResourceLoader.Exists("res://Asset/Config/Map/Frontlawn/Scene/Day/TowerDefenseMapFrontlawn.tscn"))
		{
			return null;
		}
		_defaultFrontlawnScene = ResourceLoader.Load<PackedScene>("res://Asset/Config/Map/Frontlawn/Scene/Day/TowerDefenseMapFrontlawn.tscn", null, ResourceLoader.CacheMode.Reuse);
		return _defaultFrontlawnScene;
	}

	private static string BuildMapSceneLabel(TowerDefenseMapConfig map)
	{
		if (map != null && !string.IsNullOrWhiteSpace(map.mapScenePath))
		{
			return "mapScene: " + FormatResourcePath(map.mapScenePath);
		}
		PackedScene packedScene = ResolveMapPreviewScene(map);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return "mapScene: 未配置";
		}
		return "mapScene: 默认 Frontlawn (" + FormatResource(packedScene) + ")";
	}

	private static string FormatResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未配置";
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return resource.ResourcePath.GetFile();
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourceName))
		{
			return resource.ResourceName;
		}
		return resource.GetType().Name;
	}

	private static string FormatResourcePath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.GetFile();
		}
		return "未配置";
	}

	private static string FormatVector(Vector2 value)
	{
		return $"({value.X:0.##}, {value.Y:0.##})";
	}

	private static string FormatBool(bool value)
	{
		if (!value)
		{
			return "关";
		}
		return "开";
	}

	private static string BuildSpecialRuleSummary(TowerDefenseMapConfig map)
	{
		if (map?.specialRules == null || map.specialRules.Count == 0)
		{
			return "无";
		}
		List<string> list = new List<string>(map.specialRules.Count);
		foreach (TowerDefenseMapRuleConfig specialRule in map.specialRules)
		{
			if (GodotObject.IsInstanceValid(specialRule) && !specialRule.id.IsEmpty)
			{
				list.Add(specialRule.id.ToString());
			}
		}
		if (list.Count <= 0)
		{
			return "无";
		}
		return string.Join(", ", list);
	}

	private static void AddItemIfMissing(ItemList list, string text)
	{
		if (!GodotObject.IsInstanceValid(list) || string.IsNullOrWhiteSpace(text))
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
}
