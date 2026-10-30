using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWProjectileVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileVisualEditorLayout.tscn";

	private const string CoreFieldsScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileCoreFields.tscn";

	private const string HitFieldsScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileHitFields.tscn";

	private const string FirePipelineScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileFirePipelineLayout.tscn";

	private const string PipelineBranchScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectilePipelineBranchCard.tscn";

	private const string BehaviorFieldsScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileBehaviorFields.tscn";

	private const string FireComponentFieldsScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileFireComponentFields.tscn";

	private const string ProjectileRegistryPath = "res://Registry/Projectile/ProjectileRegistry.json";

	private static PackedScene _editorLayoutScene;

	private static PackedScene _coreFieldsScene;

	private static PackedScene _hitFieldsScene;

	private static PackedScene _firePipelineScene;

	private static PackedScene _pipelineBranchScene;

	private static PackedScene _behaviorFieldsScene;

	private static PackedScene _fireComponentFieldsScene;

	private static readonly HashSet<string> ProjectileInspectorEditableProperties = new HashSet<string>
	{
		"resource_name", "resource_local_to_scene", "name", "skinName", "baseDamage", "size", "scale", "projectileObject", "projectileScene", "splatSceneType",
		"splatAudio", "splatScene", "hitEffect", "damageFlags", "fireMethodFlags", "collisionFlags", "catapultHeight", "trackSearchInterval", "rotateFollowVelocity", "hitBody",
		"isFire", "rangeType", "useRange", "rangeSize", "hitPesontage", "penetrateNum", "penetrateOverBack", "backOutGround", "backDuration", "hitTargetEventList",
		"hitCharacterEventList", "hitGroundEventList", "behaviors", "Flag/Damage", "Flag/FireMethod", "Flag/Collision", "Catapult/Height", "Penetrate/Num", "Penetrate/OverBack", "Back/OutOfGround",
		"Back/Duration"
	};

	private Resource _editingProjectile;

	private SubViewport _projectileViewport;

	private Node2D _projectilePreviewRoot;

	private Node2D _projectileTargetRoot;

	private Node2D _projectileImpactRoot;

	private HSlider _projectileTimelineSlider;

	private Button _projectilePlayButton;

	private Label _projectilePlaybackStatusLabel;

	private bool _projectilePreviewPlaying;

	private double _projectilePreviewProgress;

	private Node _projectileSceneInstance;

	private Label _projectileNameLabel;

	private Label _sceneLabel;

	private Label _damageLabel;

	private Label _flagLabel;

	private Label _rangeLabel;

	private ItemList _hitTargetEventList;

	private ItemList _hitCharacterEventList;

	private ItemList _hitGroundEventList;

	private ItemList _behaviorsList;

	private ItemList _projectileChangeList;

	private Label _projectileChangeHintLabel;

	private bool _updatingControls;

	private XWVisualPropertyBinding _projectilePropertyBinding;

	private Node2D _pipelineProjectileRoot;

	private Node2D _pipelineTargetRoot;

	private Line2D _pipelineTrajectoryLine;

	private HSlider _pipelineTimelineSlider;

	private Button _pipelinePlayButton;

	private Label _pipelinePreviewStatus;

	private Label _pipelineStatusLabel;

	private Node _pipelineProjectileInstance;

	private PackedScene _pipelineResolvedScene;

	private TowerDefenseProjectileConfig _pipelinePreviewConfig;

	private TowerDefenseProjectileCreateData _pipelineCreateData;

	private ProjectileBehaviorDefinition _pipelineBehavior;

	private double _pipelinePreviewProgress;

	private bool _pipelinePreviewPlaying;

	private string _pipelinePreviewProjectileKey = "";

	private XWGameplayResourcePickerWindow _projectilePicker;

	private readonly List<XWVisualSegmentedOption> _projectileSegmentedOptions = new List<XWVisualSegmentedOption>();

	private HBoxContainer _pipelineBranchHost;

	private ItemList _pipelineWeightList;

	private FireComponentProjectileWeight _pipelineWeightResource;

	private int _selectedPipelineWeightIndex = -1;

	public override void _Ready()
	{
		base._Ready();
		EnableVisibilityGatedProcessing();
	}

	public override void _ExitTree()
	{
		DisposeProjectileVisualChoices();
		_pipelinePreviewPlaying = false;
		_projectilePropertyBinding?.Dispose();
		_projectilePropertyBinding = null;
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeProjectileVisualChoices();
		_projectilePropertyBinding?.Dispose();
		_projectilePropertyBinding = null;
		_projectilePreviewPlaying = false;
		_pipelinePreviewPlaying = false;
		_pipelineProjectileRoot = null;
		_pipelineTargetRoot = null;
		_pipelineTrajectoryLine = null;
		_pipelineTimelineSlider = null;
		_pipelinePlayButton = null;
		_pipelinePreviewStatus = null;
		_pipelineStatusLabel = null;
		_pipelineProjectileInstance = null;
		_pipelineResolvedScene = null;
		_pipelinePreviewConfig = null;
		_pipelineCreateData = null;
		_pipelineBehavior = null;
		_pipelineBranchHost = null;
		_pipelineWeightList = null;
		_pipelineWeightResource = null;
		_selectedPipelineWeightIndex = -1;
		RequestVisibilityGatedProcessing(requested: false);
		Resource currentResource = base.CurrentResource;
		if ((currentResource is TowerDefenseProjectileConfig || currentResource is TowerDefenseProjectileData) ? true : false)
		{
			RenderProjectileEditor(_editingProjectile = base.CurrentResource);
		}
		else if (IsProjectilePipelineResource(base.CurrentResource))
		{
			_editingProjectile = base.CurrentResource;
			RenderProjectileFirePipeline(base.CurrentResource);
		}
		else
		{
			_editingProjectile = null;
		}
	}

	protected override HashSet<string> GetEmbeddedInspectorAllowedProperties(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if ((resource is TowerDefenseProjectileConfig || resource is TowerDefenseProjectileData) ? true : false)
		{
			return ProjectileInspectorEditableProperties;
		}
		return base.GetEmbeddedInspectorAllowedProperties(resource, path, descriptor);
	}

	protected override void OnEmbeddedInspectorPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		if ((obj is TowerDefenseProjectileConfig || obj is TowerDefenseProjectileData) ? true : false)
		{
			Resource projectile = (_editingProjectile = obj as Resource);
			UpdateProjectileSummary();
			RefreshProjectileLists(projectile);
			UpdateProjectilePlaybackVisual();
		}
	}

	private void RenderProjectileEditor(Resource projectile)
	{
		if (base.CanvasGrid == null)
		{
			return;
		}
		base.CanvasGrid.Columns = 1;
		if (_editorLayoutScene == null)
		{
			_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			BindProjectileScenePreview(vBoxContainer);
			if (_coreFieldsScene == null)
			{
				_coreFieldsScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileCoreFields.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			if (_hitFieldsScene == null)
			{
				_hitFieldsScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileHitFields.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			Control control = _coreFieldsScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			Control control2 = _hitFieldsScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(control))
			{
				vBoxContainer.GetNode<VBoxContainer>("%CoreHost").AddChild(control, forceReadableName: false, InternalMode.Disabled);
				BindProjectileCoreFields(control, projectile);
			}
			if (GodotObject.IsInstanceValid(control2))
			{
				vBoxContainer.GetNode<VBoxContainer>("%HitHost").AddChild(control2, forceReadableName: false, InternalMode.Disabled);
				BindProjectileHitFields(control2, projectile);
			}
			BindProjectileChangePanel(vBoxContainer, projectile);
		}
	}

	private static bool IsProjectilePipelineResource(Resource resource)
	{
		if (resource is TowerDefenseProjectileCreateData || resource is ProjectileBehaviorDefinition || resource is FireComponentCheckConfig || resource is FireComponentFireProjectileConfig || resource is FireComponentProjectileResource || resource is FireComponentProjectileWeightItem)
		{
			return true;
		}
		return false;
	}

	private void RenderProjectileFirePipeline(Resource resource)
	{
		if (base.CanvasGrid == null || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		base.CanvasGrid.Columns = 1;
		if (_firePipelineScene == null)
		{
			_firePipelineScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileFirePipelineLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (_pipelineBranchScene == null)
		{
			_pipelineBranchScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectilePipelineBranchCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		VBoxContainer vBoxContainer = _firePipelineScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(vBoxContainer))
		{
			base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			EnsureProjectilePropertyBinding();
			_pipelinePreviewProjectileKey = ((!string.IsNullOrWhiteSpace(base.CurrentEditContext?.PreviewKey)) ? base.CurrentEditContext.PreviewKey : GetProjectilePreviewKey(resource));
			if (string.IsNullOrWhiteSpace(_pipelinePreviewProjectileKey))
			{
				_pipelinePreviewProjectileKey = "PeaDefault";
			}
			vBoxContainer.GetNode<Label>("%ResourceTypeLabel").Text = resource.GetType().Name;
			vBoxContainer.GetNode<Label>("%Title").Text = GetPipelineTitle(resource);
			vBoxContainer.GetNode<Label>("%ParameterHint").Text = GetPipelineParameterHint(resource);
			vBoxContainer.GetNode<Label>("%PipelineStatusLabel").Text = GetPipelineStatus(resource);
			ConfigurePipelineNode(vBoxContainer.GetNode<PanelContainer>("%ShooterNode"), resource is FireComponentFireProjectileConfig, GetShooterNodeDetail(resource));
			ConfigurePipelineNode(vBoxContainer.GetNode<PanelContainer>("%CheckNode"), resource is FireComponentCheckConfig, GetCheckNodeDetail(resource));
			PanelContainer node = vBoxContainer.GetNode<PanelContainer>("%PoolNode");
			bool active = ((resource is FireComponentProjectileResource || resource is FireComponentProjectileWeightItem || resource is FireComponentCheckConfig) ? true : false);
			ConfigurePipelineNode(node, active, GetPoolNodeDetail(resource));
			PanelContainer node2 = vBoxContainer.GetNode<PanelContainer>("%CreateNode");
			active = ((resource is TowerDefenseProjectileCreateData || resource is FireComponentProjectileResource || resource is FireComponentProjectileWeightItem) ? true : false);
			ConfigurePipelineNode(node2, active, GetCreateNodeDetail(resource));
			PanelContainer node3 = vBoxContainer.GetNode<PanelContainer>("%BehaviorNode");
			active = ((resource is ProjectileBehaviorDefinition || resource is TowerDefenseProjectileCreateData) ? true : false);
			ConfigurePipelineNode(node3, active, GetBehaviorNodeDetail(resource));
			ConfigurePipelineNode(vBoxContainer.GetNode<PanelContainer>("%TargetNode"), resource is TowerDefenseProjectileCreateData, "命中事件 · 安全只读预览");
			_pipelineBranchHost = vBoxContainer.GetNode<HBoxContainer>("%BranchHost");
			PopulatePipelineBranches(_pipelineBranchHost, resource);
			BindPipelineGamePreview(vBoxContainer, resource);
			BindPipelineEditorFields(vBoxContainer.GetNode<VBoxContainer>("%ParameterHost"), resource);
		}
	}

	private void BindPipelineEditorFields(VBoxContainer parameterHost, Resource resource)
	{
		if (!GodotObject.IsInstanceValid(parameterHost))
		{
			return;
		}
		if (resource is TowerDefenseProjectileCreateData towerDefenseProjectileCreateData)
		{
			if (_coreFieldsScene == null)
			{
				_coreFieldsScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileCoreFields.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			if (_hitFieldsScene == null)
			{
				_hitFieldsScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileHitFields.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			Control control = _coreFieldsScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			Control control2 = _hitFieldsScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(control))
			{
				parameterHost.AddChild(control, forceReadableName: false, InternalMode.Disabled);
				BindCreateDataCoreFields(control, towerDefenseProjectileCreateData);
			}
			if (GodotObject.IsInstanceValid(control2))
			{
				parameterHost.AddChild(control2, forceReadableName: false, InternalMode.Disabled);
				BindProjectileHitFields(control2, towerDefenseProjectileCreateData);
			}
		}
		else if (resource is ProjectileBehaviorDefinition behavior)
		{
			if (_behaviorFieldsScene == null)
			{
				_behaviorFieldsScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileBehaviorFields.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			PanelContainer panelContainer = _behaviorFieldsScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(panelContainer))
			{
				parameterHost.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
				BindProjectileBehaviorFields(panelContainer, behavior);
			}
		}
		else
		{
			if (_fireComponentFieldsScene == null)
			{
				_fireComponentFieldsScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWProjectileFireComponentFields.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			PanelContainer panelContainer2 = _fireComponentFieldsScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(panelContainer2))
			{
				parameterHost.AddChild(panelContainer2, forceReadableName: false, InternalMode.Disabled);
				BindFireComponentFields(panelContainer2, resource);
			}
		}
	}

	private void BindFireComponentFields(PanelContainer root, Resource resource)
	{
		EnsureProjectilePropertyBinding();
		root.GetNode<Control>("%CheckSection").Visible = resource is FireComponentCheckConfig;
		root.GetNode<Control>("%FireSection").Visible = resource is FireComponentFireProjectileConfig;
		root.GetNode<Control>("%SingleSection").Visible = resource is FireComponentProjectileSingle;
		root.GetNode<Control>("%WeightSection").Visible = resource is FireComponentProjectileWeight;
		root.GetNode<Control>("%WeightItemSection").Visible = resource is FireComponentProjectileWeightItem;
		if (!(resource is FireComponentCheckConfig check))
		{
			if (!(resource is FireComponentFireProjectileConfig fire))
			{
				if (!(resource is FireComponentProjectileSingle single))
				{
					if (!(resource is FireComponentProjectileWeight weight))
					{
						if (resource is FireComponentProjectileWeightItem item)
						{
							BindFireProjectileWeightItemFields(root, item);
						}
					}
					else
					{
						BindFireProjectileWeightFields(root, weight);
					}
				}
				else
				{
					BindFireProjectileSingleFields(root, single);
				}
			}
			else
			{
				BindFireProjectileFields(root, fire);
			}
		}
		else
		{
			BindFireComponentCheckFields(root, check);
		}
	}

	private void BindFireComponentCheckFields(PanelContainer root, FireComponentCheckConfig check)
	{
		CheckButton node = root.GetNode<CheckButton>("%UseParentCollisionCheck");
		GridContainer flagsGrid = root.GetNode<GridContainer>("%CollisionFlagsGrid");
		Label flagsLabel = root.GetNode<Label>("%CollisionFlagsLabel");
		node.ButtonPressed = check.useParentCollision;
		RefreshCollisionVisibility();
		node.Toggled += delegate(bool value)
		{
			_projectilePropertyBinding.SetValue(check, "useParentCollision", value, "修改碰撞阵营继承");
			check.NotifyPropertyListChanged();
			RefreshCollisionVisibility();
		};
		BuildFireCollisionFlagGrid(flagsGrid, check);
		Button open = root.GetNode<Button>("%OpenCheckProjectileButton");
		XWResourcePicker node2 = root.GetNode<XWResourcePicker>("%CheckProjectilePicker");
		BindFireProjectilePicker(node2, check.projectile, delegate(FireComponentProjectileResource resource)
		{
			_projectilePropertyBinding.SetValue(check, "projectile", resource, "修改检测子弹池");
			open.Disabled = !GodotObject.IsInstanceValid(resource);
			RefreshPipelineBranchCards();
			RefreshPipelineCreateDataPreview();
		});
		open.Disabled = !GodotObject.IsInstanceValid(check.projectile);
		open.Pressed += delegate
		{
			OpenFirePipelineNestedResource(check.projectile, check, "projectile", -1);
		};
		node2.ResourceSelected += delegate(Resource resource)
		{
			OpenFirePipelineNestedResource(resource, check, "projectile", -1);
		};
		void RefreshCollisionVisibility()
		{
			flagsGrid.Visible = !check.useParentCollision;
			flagsLabel.Visible = !check.useParentCollision;
		}
	}

	private void BindFireProjectileFields(PanelContainer root, FireComponentFireProjectileConfig fire)
	{
		_projectilePropertyBinding.BindNumber(root.GetNode<SpinBox>("%CheckProjectileIdSpinBox"), fire, "checkProjectileId", RefreshFireProjectilePreview);
		_projectilePropertyBinding.BindNumber(root.GetNode<SpinBox>("%FirePosIdSpinBox"), fire, "firePosId", RefreshFireProjectilePreview);
		_projectilePropertyBinding.BindNumber(root.GetNode<SpinBox>("%SpeedSpinBox"), fire, "speed", RefreshFireProjectilePreview);
		_projectilePropertyBinding.BindNumber(root.GetNode<SpinBox>("%DirectionSpinBox"), fire, "dir", RefreshFireProjectilePreview);
		_projectilePropertyBinding.BindNumber(root.GetNode<SpinBox>("%OffsetLineSpinBox"), fire, "offsetLine", RefreshFireProjectilePreview);
		_projectilePropertyBinding.BindNumber(root.GetNode<SpinBox>("%FireNumSkipSpinBox"), fire, "fireNumSkip", RefreshFireProjectilePreview);
		_projectilePropertyBinding.BindText(root.GetNode<LineEdit>("%FireEventNeedLineEdit"), fire, "fireEventNeed", RefreshFireProjectilePreview);
		CheckButton node = root.GetNode<CheckButton>("%ProjectileFlipCheck");
		node.ButtonPressed = fire.projectileFlip;
		node.Toggled += delegate(bool value)
		{
			_projectilePropertyBinding.SetValue(fire, "projectileFlip", value, "修改子弹翻转");
			RefreshFireProjectilePreview();
		};
	}

	private void BindFireProjectileSingleFields(PanelContainer root, FireComponentProjectileSingle single)
	{
		Button open = root.GetNode<Button>("%OpenSingleProjectileButton");
		XWResourcePicker node = root.GetNode<XWResourcePicker>("%SingleProjectileDataPicker");
		BindCreateDataPicker(node, single.projectileData, delegate(TowerDefenseProjectileCreateData resource)
		{
			_projectilePropertyBinding.SetValue(single, "projectileData", resource, "修改单发子弹数据");
			open.Disabled = !GodotObject.IsInstanceValid(resource);
			RefreshPipelineBranchCards();
			RefreshPipelineCreateDataPreview();
		});
		open.Disabled = !GodotObject.IsInstanceValid(single.projectileData);
		open.Pressed += delegate
		{
			OpenFirePipelineNestedResource(single.projectileData, single, "projectileData", -1);
		};
		node.ResourceSelected += delegate(Resource resource)
		{
			OpenFirePipelineNestedResource(resource, single, "projectileData", -1);
		};
	}

	private void BindFireProjectileWeightFields(PanelContainer root, FireComponentProjectileWeight weight)
	{
		_pipelineWeightResource = weight;
		CheckButton node = root.GetNode<CheckButton>("%AverageWeightCheck");
		node.ButtonPressed = weight.averageWeight;
		node.Toggled += delegate(bool value)
		{
			_projectilePropertyBinding.SetValue(weight, "averageWeight", value, "修改权重模式");
			RefreshPipelineWeightProbabilities();
		};
		Button openDefault = root.GetNode<Button>("%OpenDefaultProjectileButton");
		XWResourcePicker node2 = root.GetNode<XWResourcePicker>("%WeightDefaultProjectileDataPicker");
		BindCreateDataPicker(node2, weight.projectileData, delegate(TowerDefenseProjectileCreateData resource)
		{
			_projectilePropertyBinding.SetValue(weight, "projectileData", resource, "修改默认子弹数据");
			openDefault.Disabled = !GodotObject.IsInstanceValid(resource);
			if (weight.projectileWeight == null || weight.projectileWeight.Count == 0)
			{
				RefreshPipelineBranchCards();
			}
			RefreshPipelineCreateDataPreview();
		});
		openDefault.Disabled = !GodotObject.IsInstanceValid(weight.projectileData);
		openDefault.Pressed += delegate
		{
			OpenFirePipelineNestedResource(weight.projectileData, weight, "projectileData", -1);
		};
		node2.ResourceSelected += delegate(Resource resource)
		{
			OpenFirePipelineNestedResource(resource, weight, "projectileData", -1);
		};
		_pipelineWeightList = root.GetNode<ItemList>("%WeightList");
		_pipelineWeightList.ItemSelected += delegate(long index)
		{
			SelectPipelineWeightBranch((int)index);
		};
		_pipelineWeightList.ItemActivated += delegate(long index)
		{
			OpenPipelineWeightItem((int)index);
		};
		root.GetNode<Button>("%AddWeightButton").Pressed += AddPipelineWeightItem;
		root.GetNode<Button>("%DuplicateWeightButton").Pressed += DuplicatePipelineWeightItem;
		root.GetNode<Button>("%RemoveWeightButton").Pressed += RemovePipelineWeightItem;
		root.GetNode<Button>("%MoveWeightUpButton").Pressed += delegate
		{
			MovePipelineWeightItem(-1);
		};
		root.GetNode<Button>("%MoveWeightDownButton").Pressed += delegate
		{
			MovePipelineWeightItem(1);
		};
		RefreshPipelineWeightList();
	}

	private void BindFireProjectileWeightItemFields(PanelContainer root, FireComponentProjectileWeightItem item)
	{
		Button open = root.GetNode<Button>("%OpenWeightItemProjectileButton");
		XWResourcePicker node = root.GetNode<XWResourcePicker>("%WeightItemProjectilePicker");
		BindFireProjectilePicker(node, item.projectileResource, delegate(FireComponentProjectileResource resource)
		{
			_projectilePropertyBinding.SetValue(item, "projectileResource", resource, "修改权重分支子弹池");
			open.Disabled = !GodotObject.IsInstanceValid(resource);
			RefreshPipelineBranchCards();
			RefreshPipelineCreateDataPreview();
		});
		open.Disabled = !GodotObject.IsInstanceValid(item.projectileResource);
		open.Pressed += delegate
		{
			OpenFirePipelineNestedResource(item.projectileResource, item, "projectileResource", -1);
		};
		node.ResourceSelected += delegate(Resource resource)
		{
			OpenFirePipelineNestedResource(resource, item, "projectileResource", -1);
		};
		_projectilePropertyBinding.BindNumber(root.GetNode<SpinBox>("%WeightSpinBox"), item, "weight", RefreshPipelineWeightProbabilities);
	}

	private static void BindFireProjectilePicker(XWResourcePicker picker, FireComponentProjectileResource value, Action<FireComponentProjectileResource> changed)
	{
		picker.Setup("FireComponentProjectileResource");
		picker.SetEditedResource(value);
		picker.ResourceChanged += delegate(Resource resource)
		{
			changed?.Invoke(resource as FireComponentProjectileResource);
		};
	}

	private static void BindCreateDataPicker(XWResourcePicker picker, TowerDefenseProjectileCreateData value, Action<TowerDefenseProjectileCreateData> changed)
	{
		picker.Setup("TowerDefenseProjectileCreateData");
		picker.SetEditedResource(value);
		picker.ResourceChanged += delegate(Resource resource)
		{
			changed?.Invoke(resource as TowerDefenseProjectileCreateData);
		};
	}

	private void BuildFireCollisionFlagGrid(GridContainer grid, FireComponentCheckConfig check)
	{
		TowerDefenseEnum.CHARACTER_COLLISION_FLAGS[] values = Enum.GetValues<TowerDefenseEnum.CHARACTER_COLLISION_FLAGS>();
		for (int i = 0; i < values.Length; i++)
		{
			TowerDefenseEnum.CHARACTER_COLLISION_FLAGS cHARACTER_COLLISION_FLAGS = values[i];
			int flag = Convert.ToInt32(cHARACTER_COLLISION_FLAGS);
			CheckButton checkButton = new CheckButton
			{
				Text = cHARACTER_COLLISION_FLAGS.ToString(),
				ButtonPressed = ((check.collisionFlags & flag) == flag),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			checkButton.Toggled += delegate(bool pressed)
			{
				int collisionFlags = check.collisionFlags;
				collisionFlags = (pressed ? (collisionFlags | flag) : (collisionFlags & ~flag));
				_projectilePropertyBinding.SetValue(check, "collisionFlags", collisionFlags, "修改检测碰撞阵营");
			};
			grid.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void RefreshFireProjectilePreview()
	{
		RefreshPipelineCreateDataPreview();
		if (base.CurrentResource is FireComponentFireProjectileConfig fireComponentFireProjectileConfig && GodotObject.IsInstanceValid(_pipelineStatusLabel))
		{
			_pipelineStatusLabel.Text = $"确定性预览：发射点 {fireComponentFireProjectileConfig.firePosId}，速度 {fireComponentFireProjectileConfig.speed:0.##} px/s，方向 {fireComponentFireProjectileConfig.dir:0.##}°，行偏移 {fireComponentFireProjectileConfig.offsetLine}";
		}
	}

	private void BindCreateDataCoreFields(Control root, TowerDefenseProjectileCreateData createData)
	{
		EnsureProjectilePropertyBinding();
		Button node = root.GetNode<Button>("%SelectProjectileButton");
		node.Visible = true;
		node.Pressed += delegate
		{
			OpenCreateDataProjectilePicker(createData, root);
		};
		LineEdit node2 = root.GetNode<LineEdit>("%ProjectileNameLineEdit");
		node2.Text = createData.projectileName.ToString();
		node2.Editable = false;
		node2.TooltipText = "使用右上角“从图鉴选择”以图像方式选择子弹";
		root.GetNode<Control>("%NameRow").Visible = true;
		BindTextIfPresent(root, "%SkinRow", root.GetNode<LineEdit>("%SkinNameLineEdit"), createData, "skinName", RefreshPipelineCreateDataPreview);
		BindNumberIfPresent(root, "%DamageRow", root.GetNode<SpinBox>("%BaseDamageSpinBox"), createData, "baseDamage", RefreshPipelineCreateDataPreview);
		BindVector2IfPresent(root, "%SizeRow", root.GetNode<SpinBox>("%SizeXSpinBox"), root.GetNode<SpinBox>("%SizeYSpinBox"), createData, "size", RefreshPipelineCreateDataPreview);
		BindVector2IfPresent(root, "%ScaleRow", root.GetNode<SpinBox>("%ScaleXSpinBox"), root.GetNode<SpinBox>("%ScaleYSpinBox"), createData, "scale", RefreshPipelineCreateDataPreview);
		root.GetNode<Control>("%ObjectRow").Visible = false;
		root.GetNode<Control>("%ProjectileSceneRow").Visible = false;
		root.GetNode<Control>("%SplatTypeRow").Visible = false;
		root.GetNode<Control>("%SplatAudioRow").Visible = false;
		root.GetNode<Control>("%SplatSceneRow").Visible = false;
		root.GetNode<Control>("%HitEffectRow").Visible = false;
		_damageLabel = root.GetNode<Label>("%ProjectileDamageSummary");
		_damageLabel.Text = $"覆盖伤害：{createData.baseDamage:0.##} · 尺寸 {FormatVector(createData.size)} · 缩放 {FormatVector(createData.scale)}";
	}

	private void BindProjectileBehaviorFields(PanelContainer root, ProjectileBehaviorDefinition behavior)
	{
		EnsureProjectilePropertyBinding();
		root.GetNode<Label>("%PreviewProjectileLabel").Text = "预览子弹：" + _pipelinePreviewProjectileKey;
		root.GetNode<Button>("%SelectPreviewProjectileButton").Pressed += delegate
		{
			OpenBehaviorPreviewProjectilePicker(root);
		};
		SpinBox node = root.GetNode<SpinBox>("%StrengthSpinBox");
		SpinBox node2 = root.GetNode<SpinBox>("%SpeedSpinBox");
		Control node3 = root.GetNode<Control>("%StrengthRow");
		Control node4 = root.GetNode<Control>("%SpeedRow");
		bool flag = (node3.Visible = behavior is ProjectileBehaviorYMoveSin);
		node4.Visible = flag;
		if (flag)
		{
			_projectilePropertyBinding.BindNumber(node, behavior, "Strength", RefreshPipelineCreateDataPreview);
			_projectilePropertyBinding.BindNumber(node2, behavior, "Speed", RefreshPipelineCreateDataPreview);
			root.GetNode<Label>("%BehaviorWarning").Text = "已识别正弦轨迹：只读取 Strength / Speed 绘制曲线，不调用旧式逐子弹回调。";
		}
		else
		{
			root.GetNode<Label>("%BehaviorWarning").Text = behavior.GetType().Name + " 尚无专用轨迹适配器；不会执行行为脚本。";
		}
	}

	private void OpenCreateDataProjectilePicker(TowerDefenseProjectileCreateData createData, Control coreFields)
	{
		EnsureProjectilePicker();
		_projectilePicker?.Open(XWGameplayResourceKind.Projectile, createData.projectileName.ToString(), delegate(XWGameplayResourceChoice choice)
		{
			if (!(choice == null) && !string.IsNullOrWhiteSpace(choice.Key))
			{
				EnsureProjectilePropertyBinding();
				_projectilePropertyBinding.SetValue(createData, "projectileName", Variant.From<StringName>(new StringName(choice.Key)), "选择预览子弹");
				createData.InvalidateConfigCache();
				_pipelinePreviewProjectileKey = choice.Key;
				if (GodotObject.IsInstanceValid(coreFields))
				{
					coreFields.GetNode<LineEdit>("%ProjectileNameLineEdit").Text = choice.Key;
				}
				RefreshPipelineCreateDataPreview();
			}
		});
	}

	private void OpenBehaviorPreviewProjectilePicker(PanelContainer behaviorFields)
	{
		EnsureProjectilePicker();
		_projectilePicker?.Open(XWGameplayResourceKind.Projectile, _pipelinePreviewProjectileKey, delegate(XWGameplayResourceChoice choice)
		{
			if (!(choice == null) && !string.IsNullOrWhiteSpace(choice.Key))
			{
				_pipelinePreviewProjectileKey = choice.Key;
				if (GodotObject.IsInstanceValid(behaviorFields))
				{
					behaviorFields.GetNode<Label>("%PreviewProjectileLabel").Text = "预览子弹：" + choice.Key;
				}
				RefreshPipelineCreateDataPreview();
			}
		});
	}

	private void EnsureProjectilePicker()
	{
		if (!GodotObject.IsInstanceValid(_projectilePicker))
		{
			_projectilePicker = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_projectilePicker))
			{
				AddChild(_projectilePicker, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private void OpenProjectileObjectCatalog(OptionButton option, Button catalogButton)
	{
		EnsureProjectilePicker();
		List<XWGameplayResourceChoice> list = new List<XWGameplayResourceChoice>();
		ObjectManagerConfig.OBJECT[] values = Enum.GetValues<ObjectManagerConfig.OBJECT>();
		for (int i = 0; i < values.Length; i++)
		{
			ObjectManagerConfig.OBJECT oBJECT = values[i];
			int num = (int)oBJECT;
			list.Add(new XWGameplayResourceChoice(num.ToString(), oBJECT.ToString(), $"OBJECT ID {(int)oBJECT}", ResourceLoader.Load<Texture2D>(ResolveProjectileObjectIcon(oBJECT), null, ResourceLoader.CacheMode.Reuse), XWGameplayResourceKind.Resource, IsModResource: false));
		}
		_projectilePicker?.OpenChoices("对象池图鉴", "搜索对象池名称或 ID；隐藏选项保留原始枚举语义", option.GetSelectedId().ToString(), list, delegate(XWGameplayResourceChoice choice)
		{
			if (int.TryParse(choice?.Key, out var result))
			{
				int itemIndex = option.GetItemIndex(result);
				if (itemIndex >= 0)
				{
					option.Select(itemIndex);
					catalogButton.Text = $"对象池图鉴 · {(ObjectManagerConfig.OBJECT)result}";
					option.EmitSignal(OptionButton.SignalName.ItemSelected, itemIndex);
				}
			}
		});
	}

	private static string ResolveProjectileObjectIcon(ObjectManagerConfig.OBJECT value)
	{
		string text = value.ToString();
		if (text.Contains("PROJECTILE", StringComparison.OrdinalIgnoreCase))
		{
			return "res://addons/ModEditor/Icons/ResourceProjectile.svg";
		}
		if (text.Contains("COIN", StringComparison.OrdinalIgnoreCase) || text.Contains("SUN", StringComparison.OrdinalIgnoreCase))
		{
			return "res://addons/ModEditor/Icons/ResourceCollectable.svg";
		}
		return "res://addons/ModEditor/Icons/ResourceCard.svg";
	}

	private void MountProjectileSegmentedOption(OptionButton option, HFlowContainer host)
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
			_projectileSegmentedOptions.Add(xWVisualSegmentedOption);
		}
	}

	private void DisposeProjectileVisualChoices()
	{
		foreach (XWVisualSegmentedOption projectileSegmentedOption in _projectileSegmentedOptions)
		{
			projectileSegmentedOption?.Dispose();
		}
		_projectileSegmentedOptions.Clear();
	}

	private void BindPipelineGamePreview(VBoxContainer root, Resource resource)
	{
		_pipelineProjectileRoot = root.GetNode<Node2D>("%ProjectileRoot");
		_pipelineTargetRoot = root.GetNode<Node2D>("%TargetRoot");
		_pipelineTrajectoryLine = root.GetNode<Line2D>("%TrajectoryLine");
		_pipelineTimelineSlider = root.GetNode<HSlider>("%PipelineTimelineSlider");
		_pipelinePlayButton = root.GetNode<Button>("%PipelinePlayButton");
		_pipelinePreviewStatus = root.GetNode<Label>("%PipelinePreviewStatus");
		_pipelineStatusLabel = root.GetNode<Label>("%PipelineStatusLabel");
		_pipelineCreateData = ResolvePipelineCreateData(resource);
		_pipelineBehavior = (resource as ProjectileBehaviorDefinition) ?? FindSinBehavior(_pipelineCreateData);
		_pipelinePreviewProgress = 0.0;
		_pipelinePlayButton.Pressed += PlayPipelinePreview;
		_pipelineTimelineSlider.ValueChanged += delegate(double value)
		{
			_pipelinePreviewProgress = value;
			_pipelinePreviewPlaying = false;
			UpdatePipelinePlaybackVisual();
			RequestVisibilityGatedProcessing(requested: false);
		};
		RebuildPipelineTargetPreview();
		RefreshPipelineCreateDataPreview();
		RefreshVisibilityGatedProcessing();
	}

	private TowerDefenseProjectileCreateData ResolvePipelineCreateData(Resource resource)
	{
		if (!(resource is TowerDefenseProjectileCreateData result))
		{
			if (!(resource is FireComponentProjectileSingle { projectileData: var projectileData }))
			{
				if (!(resource is FireComponentProjectileWeight weight))
				{
					if (!(resource is FireComponentProjectileWeightItem fireComponentProjectileWeightItem))
					{
						if (!(resource is FireComponentCheckConfig fireComponentCheckConfig))
						{
							if (!(resource is ProjectileBehaviorDefinition))
							{
								if (resource is FireComponentFireProjectileConfig)
								{
									return new TowerDefenseProjectileCreateData(new StringName(_pipelinePreviewProjectileKey));
								}
								return null;
							}
							return new TowerDefenseProjectileCreateData(new StringName(_pipelinePreviewProjectileKey));
						}
						return ResolvePipelineCreateData(fireComponentCheckConfig.projectile);
					}
					return ResolvePipelineCreateData(fireComponentProjectileWeightItem.projectileResource);
				}
				return ResolveWeightPreviewData(weight);
			}
			return projectileData;
		}
		return result;
	}

	private TowerDefenseProjectileCreateData ResolveWeightPreviewData(FireComponentProjectileWeight weight)
	{
		if (!GodotObject.IsInstanceValid(weight))
		{
			return null;
		}
		if (weight == _pipelineWeightResource && base.CurrentResource == weight && _selectedPipelineWeightIndex >= 0 && weight.projectileWeight != null && _selectedPipelineWeightIndex < weight.projectileWeight.Count)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem = weight.projectileWeight[_selectedPipelineWeightIndex];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem.projectileResource))
			{
				return ResolvePipelineCreateData(fireComponentProjectileWeightItem.projectileResource);
			}
		}
		return weight.projectileData;
	}

	private void RefreshPipelineCreateDataPreview()
	{
		if (GodotObject.IsInstanceValid(_pipelineProjectileRoot))
		{
			Resource currentResource = base.CurrentResource;
			if ((currentResource is ProjectileBehaviorDefinition || currentResource is FireComponentFireProjectileConfig) ? true : false)
			{
				_pipelineCreateData = new TowerDefenseProjectileCreateData(new StringName(_pipelinePreviewProjectileKey));
			}
			else
			{
				_pipelineCreateData = ResolvePipelineCreateData(base.CurrentResource);
			}
			_pipelineBehavior = (base.CurrentResource as ProjectileBehaviorDefinition) ?? FindSinBehavior(_pipelineCreateData);
			_pipelinePreviewConfig = ResolveCreateDataPreviewConfig(_pipelineCreateData, out var status);
			RebuildPipelineProjectilePreview();
			RebuildPipelineTrajectory();
			UpdatePipelinePlaybackVisual();
			if (GodotObject.IsInstanceValid(_pipelineStatusLabel))
			{
				_pipelineStatusLabel.Text = status;
			}
		}
	}

	private static TowerDefenseProjectileConfig ResolveCreateDataPreviewConfig(TowerDefenseProjectileCreateData createData, out string status)
	{
		status = "安全预览：未配置子弹，不执行游戏逻辑";
		if (!GodotObject.IsInstanceValid(createData) || createData.projectileName.IsEmpty)
		{
			return null;
		}
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = createData.Duplicate(deep: true) as TowerDefenseProjectileCreateData;
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileCreateData))
		{
			return null;
		}
		towerDefenseProjectileCreateData.InvalidateConfigCache();
		string text = towerDefenseProjectileCreateData.projectileName.ToString();
		Resource resource = LoadProjectileRegistryResource(text);
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = ((resource is TowerDefenseProjectileConfig towerDefenseProjectileConfig2) ? (towerDefenseProjectileConfig2.Duplicate(deep: true) as TowerDefenseProjectileConfig) : ((!(resource is TowerDefenseProjectileData data)) ? null : BuildPreviewConfigFromData(data)));
		TowerDefenseProjectileConfig towerDefenseProjectileConfig3 = towerDefenseProjectileConfig;
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig3))
		{
			status = "安全预览：找不到子弹 " + text + "，仅显示缺失提示";
			return null;
		}
		towerDefenseProjectileConfig3.name = (string.IsNullOrWhiteSpace(towerDefenseProjectileConfig3.name) ? text : towerDefenseProjectileConfig3.name);
		towerDefenseProjectileConfig3.skinName = towerDefenseProjectileCreateData.skinName;
		towerDefenseProjectileCreateData.ApplyOverride(towerDefenseProjectileConfig3);
		status = "安全预览：" + text + " 已从复制体解析；不会回写继承字段或执行方法/事件";
		return towerDefenseProjectileConfig3;
	}

	private static Resource LoadProjectileRegistryResource(string projectileKey)
	{
		if (string.IsNullOrWhiteSpace(projectileKey) || !ResourceLoader.Exists("res://Registry/Projectile/ProjectileRegistry.json"))
		{
			return null;
		}
		Json json = ResourceLoader.Load<Json>("res://Registry/Projectile/ProjectileRegistry.json", "", ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
		{
			return null;
		}
		Dictionary dictionary = (Dictionary)json.Data.AsGodotDictionary().GetValueOrDefault("Projectiles", new Dictionary());
		string text = "";
		foreach (Variant key in dictionary.Keys)
		{
			if (string.Equals(key.AsString(), projectileKey, StringComparison.OrdinalIgnoreCase))
			{
				text = dictionary[key].AsString();
				break;
			}
		}
		if (string.IsNullOrWhiteSpace(text) || !ResourceLoader.Exists(text))
		{
			return null;
		}
		return ResourceLoader.Load<Resource>(text, "", ResourceLoader.CacheMode.Reuse);
	}

	private static TowerDefenseProjectileConfig BuildPreviewConfigFromData(TowerDefenseProjectileData data)
	{
		if (!GodotObject.IsInstanceValid(data))
		{
			return null;
		}
		return new TowerDefenseProjectileConfig
		{
			name = data.name,
			baseDamage = data.baseDamage,
			size = data.size,
			scale = data.scale,
			projectileScene = data.projectileScene,
			splatAudio = data.splatAudio,
			splatScene = data.splatScene,
			hitEffect = data.hitEffect,
			hitTargetEventList = data.hitTargetEventList,
			hitCharacterEventList = data.hitCharacterEventList,
			hitGroundEventList = data.hitGroundEventList,
			blockHurt = data.blockHurt,
			rotateFollowVelocity = data.rotateFollowVelocity,
			rotateScale = data.rotateScale,
			hitBody = data.hitBody,
			rangeType = data.rangeType,
			useRange = data.useRange,
			rangeSize = data.rangeSize,
			hitPesontage = data.hitPesontage,
			damageFlags = (data.isFire ? 4 : 2)
		};
	}

	private void RebuildPipelineProjectilePreview()
	{
		if (!GodotObject.IsInstanceValid(_pipelineProjectileRoot))
		{
			return;
		}
		PackedScene packedScene = _pipelinePreviewConfig?.projectileScene;
		if (GodotObject.IsInstanceValid(_pipelineProjectileInstance) && packedScene == _pipelineResolvedScene)
		{
			ApplyPipelineProjectileScale(_pipelineProjectileInstance, GetPipelineProjectilePreviewScale());
			return;
		}
		foreach (Node child in _pipelineProjectileRoot.GetChildren())
		{
			child.QueueFree();
		}
		_pipelineProjectileInstance = null;
		_pipelineResolvedScene = packedScene;
		if (GodotObject.IsInstanceValid(packedScene))
		{
			try
			{
				Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				node.Name = "PipelineProjectilePreview";
				TowerDefenseProjectile towerDefenseProjectile = FindRuntimeProjectile(node);
				if (towerDefenseProjectile != null)
				{
					towerDefenseProjectile.suppressGameplay = true;
				}
				ApplyPipelineProjectileScale(node, GetPipelineProjectilePreviewScale());
				_pipelineProjectileRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
				_pipelineProjectileInstance = node;
			}
			catch (Exception ex)
			{
				GD.PushWarning("Pipeline projectile preview failed: " + ex.Message);
			}
		}
		if (!GodotObject.IsInstanceValid(_pipelineProjectileInstance))
		{
			Polygon2D polygon2D = new Polygon2D();
			polygon2D.Name = "MissingProjectilePreview";
			polygon2D.Polygon = new Vector2[4]
			{
				new Vector2(-12f, -12f),
				new Vector2(12f, -12f),
				new Vector2(12f, 12f),
				new Vector2(-12f, 12f)
			};
			polygon2D.Color = new Color(0.95f, 0.18f, 0.18f, 0.9f);
			Polygon2D polygon2D2 = polygon2D;
			_pipelineProjectileRoot.AddChild(polygon2D2, forceReadableName: false, InternalMode.Disabled);
			_pipelineProjectileInstance = polygon2D2;
		}
	}

	private static void ApplyPipelineProjectileScale(Node instance, Vector2 scale)
	{
		if (instance is Node2D node2D)
		{
			node2D.Scale = scale;
		}
		else if (instance is Control control)
		{
			control.Scale = scale;
		}
	}

	private Vector2 GetPipelineProjectilePreviewScale()
	{
		Vector2 result = _pipelinePreviewConfig?.scale ?? Vector2.One;
		if (base.CurrentResource is FireComponentFireProjectileConfig { projectileFlip: not false })
		{
			result.X = 0f - Mathf.Abs(result.X);
		}
		return result;
	}

	private void RebuildPipelineTargetPreview()
	{
		if (!GodotObject.IsInstanceValid(_pipelineTargetRoot))
		{
			return;
		}
		foreach (Node child in _pipelineTargetRoot.GetChildren())
		{
			child.QueueFree();
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
		if (GodotObject.IsInstanceValid(packedScene))
		{
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(node);
			if (towerDefenseCharacter != null)
			{
				towerDefenseCharacter.inGame = false;
				towerDefenseCharacter.editorPreviewMode = true;
			}
			_pipelineTargetRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void RebuildPipelineTrajectory()
	{
		if (GodotObject.IsInstanceValid(_pipelineTrajectoryLine))
		{
			Vector2[] array = new Vector2[41];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = EvaluatePipelinePosition((float)i / (float)(array.Length - 1));
			}
			_pipelineTrajectoryLine.Points = array;
		}
	}

	private Vector2 EvaluatePipelinePosition(float progress)
	{
		Vector2 vector = new Vector2(108f, 164f);
		Vector2 to = new Vector2(602f, 148f);
		if (base.CurrentResource is FireComponentFireProjectileConfig fireComponentFireProjectileConfig)
		{
			vector.Y += (float)fireComponentFireProjectileConfig.offsetLine * 42f;
			Vector2 vector2 = new Vector2(494f, 0f).Rotated(Mathf.DegToRad(fireComponentFireProjectileConfig.dir));
			to = vector + vector2;
		}
		int num = _pipelinePreviewConfig?.fireMethodFlags ?? _pipelineCreateData?.fireMethodFlags ?? 1;
		bool num2 = (num & 8) != 0;
		float num3 = progress;
		if (num2)
		{
			num3 = ((progress <= 0.55f) ? (progress / 0.55f) : (1f - (progress - 0.55f) / 0.45f));
		}
		Vector2 result = vector.Lerp(to, Mathf.Clamp(num3, 0f, 1f));
		if ((num & 2) != 0)
		{
			float num4 = Mathf.Clamp((float)(_pipelinePreviewConfig?.catapultHeight ?? _pipelineCreateData?.catapultHeight ?? 300.0), 30f, 420f);
			result.Y -= Mathf.Sin(num3 * (float)Math.PI) * num4 * 0.34f;
		}
		if (_pipelineBehavior is ProjectileBehaviorYMoveSin projectileBehaviorYMoveSin)
		{
			result.Y += Mathf.Sin(progress * (float)projectileBehaviorYMoveSin.Speed * 1.2f) * (float)projectileBehaviorYMoveSin.Strength;
		}
		return result;
	}

	private static ProjectileBehaviorDefinition FindSinBehavior(TowerDefenseProjectileCreateData createData)
	{
		if (!GodotObject.IsInstanceValid(createData) || createData.behaviors == null)
		{
			return null;
		}
		foreach (ProjectileBehaviorDefinition behavior in createData.behaviors)
		{
			if (behavior is ProjectileBehaviorYMoveSin)
			{
				return behavior;
			}
		}
		return null;
	}

	private static string GetProjectilePreviewKey(Resource resource)
	{
		if (!(resource is TowerDefenseProjectileCreateData towerDefenseProjectileCreateData))
		{
			if (!(resource is TowerDefenseProjectileConfig { name: var name }))
			{
				if (!(resource is TowerDefenseProjectileData { name: var name2 }))
				{
					if (!(resource is FireComponentProjectileSingle fireComponentProjectileSingle))
					{
						if (!(resource is FireComponentProjectileWeight fireComponentProjectileWeight))
						{
							if (!(resource is FireComponentProjectileWeightItem fireComponentProjectileWeightItem))
							{
								if (resource is FireComponentCheckConfig fireComponentCheckConfig)
								{
									return GetProjectilePreviewKey(fireComponentCheckConfig.projectile);
								}
								return "";
							}
							return GetProjectilePreviewKey(fireComponentProjectileWeightItem.projectileResource);
						}
						return fireComponentProjectileWeight.projectileData?.projectileName.ToString() ?? "";
					}
					return fireComponentProjectileSingle.projectileData?.projectileName.ToString() ?? "";
				}
				return name2;
			}
			return name;
		}
		return towerDefenseProjectileCreateData.projectileName.ToString();
	}

	private static void ConfigurePipelineNode(PanelContainer node, bool active, string detail)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			node.Modulate = (active ? Colors.White : new Color(0.46f, 0.5f, 0.52f, 0.72f));
			Label nodeOrNull = node.GetNodeOrNull<Label>("Layout/Detail");
			if (GodotObject.IsInstanceValid(nodeOrNull) && !string.IsNullOrWhiteSpace(detail))
			{
				nodeOrNull.Text = detail;
			}
		}
	}

	private void PopulatePipelineBranches(HBoxContainer branchHost, Resource resource)
	{
		if (GodotObject.IsInstanceValid(branchHost))
		{
			if (resource is FireComponentProjectileWeight weight)
			{
				PopulateEditablePipelineWeightBranches(branchHost, weight);
				return;
			}
			string text = ((resource is TowerDefenseProjectileCreateData data) ? DescribeCreateData(data) : ((resource is FireComponentProjectileSingle fireComponentProjectileSingle) ? DescribeCreateData(fireComponentProjectileSingle.projectileData) : ((resource is FireComponentProjectileWeightItem fireComponentProjectileWeightItem) ? DescribeProjectileResource(fireComponentProjectileWeightItem.projectileResource) : ((!(resource is FireComponentCheckConfig fireComponentCheckConfig)) ? resource.GetType().Name : DescribeProjectileResource(fireComponentCheckConfig.projectile)))));
			string text2 = text;
			string warning = ((text2 == "未配置子弹") ? "需要选择子弹资源" : "确定性预览，不执行随机或伤害");
			AddPipelineBranchCard(branchHost, text2, "概率：100%", warning);
		}
	}

	private void AddPipelineBranchCard(HBoxContainer branchHost, string projectileName, string probability, string warning)
	{
		PanelContainer panelContainer = _pipelineBranchScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(panelContainer))
		{
			panelContainer.GetNode<Label>("%ProjectileName").Text = (string.IsNullOrWhiteSpace(projectileName) ? "未配置子弹" : projectileName);
			panelContainer.GetNode<Label>("%Probability").Text = probability ?? "概率：--";
			Label node = panelContainer.GetNode<Label>("%Warning");
			node.Text = warning ?? "";
			node.Visible = !string.IsNullOrWhiteSpace(warning);
			branchHost.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void PopulateEditablePipelineWeightBranches(HBoxContainer branchHost, FireComponentProjectileWeight weight)
	{
		_pipelineWeightResource = weight;
		Array<FireComponentProjectileWeightItem> projectileWeight = weight.projectileWeight;
		if (projectileWeight == null || projectileWeight.Count == 0)
		{
			AddPipelineBranchCard(branchHost, DescribeCreateData(weight.projectileData), "概率：100%", "权重池为空，使用默认子弹");
			return;
		}
		int num = 0;
		double num2 = 0.0;
		for (int i = 0; i < projectileWeight.Count; i++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem = projectileWeight[i];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem.projectileResource))
			{
				num++;
				num2 += Math.Max(0.0, fireComponentProjectileWeightItem.weight);
			}
		}
		for (int j = 0; j < projectileWeight.Count; j++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem2 = projectileWeight[j];
			bool flag = GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2.projectileResource);
			double value = ((!flag) ? 0.0 : ((!weight.averageWeight && !(num2 <= 0.0)) ? (Math.Max(0.0, fireComponentProjectileWeightItem2.weight) / num2) : ((num > 0) ? (1.0 / (double)num) : 0.0)));
			AddPipelineBranchCard(branchHost, flag ? DescribeProjectileResource(fireComponentProjectileWeightItem2.projectileResource) : "未配置子弹", $"概率：{value:P1}", flag ? $"权重 {fireComponentProjectileWeightItem2.weight:0.##}" : "缺少子弹引用");
			ConfigurePipelineWeightBranchCard(branchHost.GetChild<PanelContainer>(branchHost.GetChildCount() - 1), weight, fireComponentProjectileWeightItem2, j);
		}
	}

	private void ConfigurePipelineWeightBranchCard(PanelContainer card, FireComponentProjectileWeight weight, FireComponentProjectileWeightItem item, int index)
	{
		if (GodotObject.IsInstanceValid(card))
		{
			card.SetMeta("weight_index", index);
			SpinBox node = card.GetNode<SpinBox>("%WeightSpinBox");
			Button node2 = card.GetNode<Button>("%SelectButton");
			Button node3 = card.GetNode<Button>("%OpenButton");
			node.Visible = GodotObject.IsInstanceValid(item);
			node2.Visible = true;
			node3.Visible = true;
			node3.Disabled = !GodotObject.IsInstanceValid(item);
			node2.Pressed += delegate
			{
				SelectPipelineWeightBranch(index);
			};
			node3.Pressed += delegate
			{
				OpenFirePipelineNestedResource(item, weight, "projectileWeight", index);
			};
			if (GodotObject.IsInstanceValid(item))
			{
				_projectilePropertyBinding.BindNumber(node, item, "weight", RefreshPipelineWeightProbabilities);
			}
		}
	}

	private void RefreshPipelineWeightProbabilities()
	{
		FireComponentProjectileWeight pipelineWeightResource = _pipelineWeightResource;
		if (!GodotObject.IsInstanceValid(pipelineWeightResource) || pipelineWeightResource.projectileWeight == null)
		{
			return;
		}
		int num = 0;
		double num2 = 0.0;
		for (int i = 0; i < pipelineWeightResource.projectileWeight.Count; i++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem = pipelineWeightResource.projectileWeight[i];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem.projectileResource))
			{
				num++;
				num2 += Math.Max(0.0, fireComponentProjectileWeightItem.weight);
			}
		}
		if (GodotObject.IsInstanceValid(_pipelineBranchHost))
		{
			foreach (Node child in _pipelineBranchHost.GetChildren())
			{
				if (!(child is PanelContainer panelContainer) || !panelContainer.HasMeta("weight_index"))
				{
					continue;
				}
				int num3 = panelContainer.GetMeta("weight_index").AsInt32();
				if (num3 >= 0 && num3 < pipelineWeightResource.projectileWeight.Count)
				{
					FireComponentProjectileWeightItem fireComponentProjectileWeightItem2 = pipelineWeightResource.projectileWeight[num3];
					bool flag = GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2.projectileResource);
					double value = ((!flag) ? 0.0 : ((!pipelineWeightResource.averageWeight && !(num2 <= 0.0)) ? (Math.Max(0.0, fireComponentProjectileWeightItem2.weight) / num2) : ((num > 0) ? (1.0 / (double)num) : 0.0)));
					panelContainer.GetNode<Label>("%ProjectileName").Text = (flag ? DescribeProjectileResource(fireComponentProjectileWeightItem2.projectileResource) : "未配置子弹");
					panelContainer.GetNode<Label>("%Probability").Text = $"概率：{value:P1}";
					Label node = panelContainer.GetNode<Label>("%Warning");
					node.Text = (flag ? $"权重 {fireComponentProjectileWeightItem2.weight:0.##}" : "缺少子弹引用");
					node.Visible = true;
					if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2))
					{
						panelContainer.GetNode<SpinBox>("%WeightSpinBox").SetValueNoSignal(fireComponentProjectileWeightItem2.weight);
					}
				}
			}
		}
		RefreshPipelineWeightList();
	}

	private void RefreshPipelineWeightList()
	{
		if (!GodotObject.IsInstanceValid(_pipelineWeightList) || !GodotObject.IsInstanceValid(_pipelineWeightResource))
		{
			return;
		}
		_pipelineWeightList.Clear();
		Array<FireComponentProjectileWeightItem> projectileWeight = _pipelineWeightResource.projectileWeight;
		if (projectileWeight == null || projectileWeight.Count == 0)
		{
			_pipelineWeightList.AddItem("暂无权重分支；预览使用默认子弹");
			_selectedPipelineWeightIndex = -1;
			return;
		}
		for (int i = 0; i < projectileWeight.Count; i++)
		{
			FireComponentProjectileWeightItem from = projectileWeight[i];
			string value = (GodotObject.IsInstanceValid(from) ? DescribeProjectileResource(from.projectileResource) : "缺失条目");
			double value2 = (GodotObject.IsInstanceValid(from) ? ((double)from.weight) : 0.0);
			int idx = _pipelineWeightList.AddItem($"{i + 1}. {value}    权重 {value2:0.##}");
			_pipelineWeightList.SetItemMetadata(idx, Variant.From(in from));
		}
		if (_selectedPipelineWeightIndex >= projectileWeight.Count)
		{
			_selectedPipelineWeightIndex = projectileWeight.Count - 1;
		}
		if (_selectedPipelineWeightIndex >= 0)
		{
			_pipelineWeightList.Select(_selectedPipelineWeightIndex);
		}
	}

	private void SelectPipelineWeightBranch(int index)
	{
		if (GodotObject.IsInstanceValid(_pipelineWeightResource) && _pipelineWeightResource.projectileWeight != null && index >= 0 && index < _pipelineWeightResource.projectileWeight.Count)
		{
			_selectedPipelineWeightIndex = index;
			if (GodotObject.IsInstanceValid(_pipelineWeightList))
			{
				_pipelineWeightList.Select(index);
			}
			RefreshPipelineCreateDataPreview();
		}
	}

	private void OpenPipelineWeightItem(int index)
	{
		if (GodotObject.IsInstanceValid(_pipelineWeightResource) && _pipelineWeightResource.projectileWeight != null && index >= 0 && index < _pipelineWeightResource.projectileWeight.Count)
		{
			OpenFirePipelineNestedResource(_pipelineWeightResource.projectileWeight[index], _pipelineWeightResource, "projectileWeight", index);
		}
	}

	private void AddPipelineWeightItem()
	{
		FireComponentProjectileWeight pipelineWeightResource = _pipelineWeightResource;
		if (GodotObject.IsInstanceValid(pipelineWeightResource))
		{
			Array<FireComponentProjectileWeightItem> array = ((pipelineWeightResource.projectileWeight == null) ? new Array<FireComponentProjectileWeightItem>() : new Array<FireComponentProjectileWeightItem>(pipelineWeightResource.projectileWeight));
			array.Add(new FireComponentProjectileWeightItem
			{
				ResourceLocalToScene = true,
				weight = 1f
			});
			_selectedPipelineWeightIndex = array.Count - 1;
			ReplacePipelineWeightItems(pipelineWeightResource, array, "新增权重分支");
		}
	}

	private void DuplicatePipelineWeightItem()
	{
		FireComponentProjectileWeight pipelineWeightResource = _pipelineWeightResource;
		if (TryGetSelectedPipelineWeightItem(out var item))
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem = (item.Duplicate(deep: true) as FireComponentProjectileWeightItem) ?? new FireComponentProjectileWeightItem
			{
				projectileResource = item.projectileResource,
				weight = item.weight
			};
			fireComponentProjectileWeightItem.ResourceLocalToScene = true;
			Array<FireComponentProjectileWeightItem> array = new Array<FireComponentProjectileWeightItem>(pipelineWeightResource.projectileWeight);
			_selectedPipelineWeightIndex = Mathf.Clamp(_selectedPipelineWeightIndex + 1, 0, array.Count);
			array.Insert(_selectedPipelineWeightIndex, fireComponentProjectileWeightItem);
			ReplacePipelineWeightItems(pipelineWeightResource, array, "复制权重分支");
		}
	}

	private void RemovePipelineWeightItem()
	{
		FireComponentProjectileWeight pipelineWeightResource = _pipelineWeightResource;
		if (TryGetSelectedPipelineWeightItem(out var _))
		{
			Array<FireComponentProjectileWeightItem> array = new Array<FireComponentProjectileWeightItem>(pipelineWeightResource.projectileWeight);
			array.RemoveAt(_selectedPipelineWeightIndex);
			_selectedPipelineWeightIndex = ((array.Count == 0) ? (-1) : Mathf.Min(_selectedPipelineWeightIndex, array.Count - 1));
			ReplacePipelineWeightItems(pipelineWeightResource, array, "删除权重分支");
		}
	}

	private void MovePipelineWeightItem(int direction)
	{
		FireComponentProjectileWeight pipelineWeightResource = _pipelineWeightResource;
		if (TryGetSelectedPipelineWeightItem(out var item))
		{
			int num = _selectedPipelineWeightIndex + Math.Sign(direction);
			if (num >= 0 && num < pipelineWeightResource.projectileWeight.Count)
			{
				Array<FireComponentProjectileWeightItem> array = new Array<FireComponentProjectileWeightItem>(pipelineWeightResource.projectileWeight);
				array.RemoveAt(_selectedPipelineWeightIndex);
				array.Insert(num, item);
				_selectedPipelineWeightIndex = num;
				ReplacePipelineWeightItems(pipelineWeightResource, array, (direction < 0) ? "上移权重分支" : "下移权重分支");
			}
		}
	}

	private bool TryGetSelectedPipelineWeightItem(out FireComponentProjectileWeightItem item)
	{
		item = null;
		FireComponentProjectileWeight pipelineWeightResource = _pipelineWeightResource;
		if (!GodotObject.IsInstanceValid(pipelineWeightResource) || pipelineWeightResource.projectileWeight == null || _selectedPipelineWeightIndex < 0 || _selectedPipelineWeightIndex >= pipelineWeightResource.projectileWeight.Count)
		{
			return false;
		}
		item = pipelineWeightResource.projectileWeight[_selectedPipelineWeightIndex];
		return GodotObject.IsInstanceValid(item);
	}

	private void ReplacePipelineWeightItems(FireComponentProjectileWeight weight, Array<FireComponentProjectileWeightItem> next, string actionName)
	{
		_projectilePropertyBinding.SetValue(weight, "projectileWeight", next, actionName);
		weight.NotifyPropertyListChanged();
		RebuildPipelineWeightSurfaces(weight);
	}

	private void RebuildPipelineWeightSurfaces(FireComponentProjectileWeight weight)
	{
		RefreshPipelineBranchCards(weight);
		RefreshPipelineWeightList();
		RefreshPipelineCreateDataPreview();
	}

	private void RefreshPipelineBranchCards(Resource resource = null)
	{
		if (!GodotObject.IsInstanceValid(_pipelineBranchHost))
		{
			return;
		}
		foreach (Node child in _pipelineBranchHost.GetChildren())
		{
			_pipelineBranchHost.RemoveChild(child);
			child.QueueFree();
		}
		PopulatePipelineBranches(_pipelineBranchHost, resource ?? base.CurrentResource);
	}

	private void OpenFirePipelineNestedResource(Resource child, Resource owner, string propertyName, int index)
	{
		if (GodotObject.IsInstanceValid(child) && GodotObject.IsInstanceValid(owner))
		{
			string text = ((owner == base.CurrentResource) ? base.CurrentResourcePath : owner.ResourcePath);
			if (string.IsNullOrWhiteSpace(text))
			{
				text = base.CurrentEditContext?.OwnerPath ?? base.CurrentResourcePath;
			}
			XWResourceEditContext context = XWResourceEditContext.ForProperty(child, owner, child.ResourcePath, text, propertyName, index, "projectile_editor", base.CurrentEditContext?.IsBuiltInSource ?? XWResourceEditContext.IsBuiltInPath(text), GetProjectilePreviewKey(owner));
			XWEditorInterface.Instance?.EditResource(child, context);
		}
	}

	private static string GetPipelineTitle(Resource resource)
	{
		if (!(resource is TowerDefenseProjectileCreateData))
		{
			if (!(resource is ProjectileBehaviorDefinition))
			{
				if (!(resource is FireComponentCheckConfig))
				{
					if (!(resource is FireComponentFireProjectileConfig))
					{
						if (!(resource is FireComponentProjectileWeight))
						{
							if (!(resource is FireComponentProjectileSingle))
							{
								if (!(resource is FireComponentProjectileWeightItem))
								{
									if (resource is FireComponentProjectileResource)
									{
										return "FireComponent 子弹池";
									}
									return "子弹发射蓝图";
								}
								return "子弹权重分支";
							}
							return "单发子弹池";
						}
						return "权重子弹池 · 概率分支";
					}
					return "发射参数 · 发射点与速度";
				}
				return "发射检测 · 目标与子弹池";
			}
			return "子弹轨迹方法 · 曲线预览";
		}
		return "子弹创建数据 · 发射预览";
	}

	private static string GetPipelineParameterHint(Resource resource)
	{
		if (!(resource is FireComponentFireProjectileConfig fireComponentFireProjectileConfig))
		{
			if (!(resource is FireComponentCheckConfig fireComponentCheckConfig))
			{
				if (!(resource is FireComponentProjectileWeight fireComponentProjectileWeight))
				{
					if (!(resource is FireComponentProjectileWeightItem fireComponentProjectileWeightItem))
					{
						if (!(resource is ProjectileBehaviorDefinition projectileBehaviorDefinition))
						{
							if (resource is TowerDefenseProjectileCreateData data)
							{
								return "子弹 Key：" + DescribeCreateData(data) + "。后续参数直接在游戏画面节点上编辑。";
							}
							return "在画面节点上编辑发射参数；完整字段仍可在下方检查器中访问。";
						}
						return "行为类型：" + projectileBehaviorDefinition.GetType().Name + "。预览只读取配置，不执行 BulletField Kernel。";
					}
					return $"当前分支权重：{fireComponentProjectileWeightItem.weight:0.##}";
				}
				return fireComponentProjectileWeight.averageWeight ? "当前为等概率分支。" : "分支宽度和概率由非负权重决定。";
			}
			return fireComponentCheckConfig.useParentCollision ? "继承角色碰撞阵营；子弹池决定可发射资源。" : $"自定义碰撞 flags：{fireComponentCheckConfig.collisionFlags}";
		}
		return $"发射点 {fireComponentFireProjectileConfig.firePosId} · 速度 {fireComponentFireProjectileConfig.speed:0.##} · 方向 {fireComponentFireProjectileConfig.dir:0.##}° · 行偏移 {fireComponentFireProjectileConfig.offsetLine}";
	}

	private static string GetPipelineStatus(Resource resource)
	{
		if (resource is ProjectileBehaviorDefinition projectileBehaviorDefinition)
		{
			if (!(projectileBehaviorDefinition is ProjectileBehaviorYMoveSin))
			{
				return "安全预览：未知方法只显示类型，不执行方法代码";
			}
			return "安全预览：已识别正弦轨迹适配器，不执行方法代码";
		}
		return "安全预览：不会执行真实 FireComponent、命中事件、伤害或随机选择";
	}

	private static string GetShooterNodeDetail(Resource resource)
	{
		if (!(resource is FireComponentFireProjectileConfig fireComponentFireProjectileConfig))
		{
			return "角色 / 发射点";
		}
		return $"点 {fireComponentFireProjectileConfig.firePosId} · {fireComponentFireProjectileConfig.speed:0.#} px/s";
	}

	private static string GetCheckNodeDetail(Resource resource)
	{
		if (!(resource is FireComponentCheckConfig fireComponentCheckConfig))
		{
			return "碰撞阵营";
		}
		if (!fireComponentCheckConfig.useParentCollision)
		{
			return $"flags {fireComponentCheckConfig.collisionFlags}";
		}
		return "继承碰撞阵营";
	}

	private static string GetPoolNodeDetail(Resource resource)
	{
		if (!(resource is FireComponentProjectileWeight fireComponentProjectileWeight))
		{
			if (!(resource is FireComponentProjectileSingle))
			{
				if (!(resource is FireComponentProjectileWeightItem fireComponentProjectileWeightItem))
				{
					if (resource is FireComponentCheckConfig)
					{
						return "检测使用的子弹池";
					}
					return "单发 / 权重";
				}
				return $"分支权重 {fireComponentProjectileWeightItem.weight:0.##}";
			}
			return "单一子弹";
		}
		return fireComponentProjectileWeight.averageWeight ? "等概率权重池" : $"权重分支 {fireComponentProjectileWeight.projectileWeight?.Count ?? 0}";
	}

	private static string GetCreateNodeDetail(Resource resource)
	{
		if (!(resource is TowerDefenseProjectileCreateData data))
		{
			if (!(resource is FireComponentProjectileSingle fireComponentProjectileSingle))
			{
				if (!(resource is FireComponentProjectileWeight fireComponentProjectileWeight))
				{
					if (resource is FireComponentProjectileWeightItem fireComponentProjectileWeightItem)
					{
						return DescribeProjectileResource(fireComponentProjectileWeightItem.projectileResource);
					}
					return "Key / 皮肤 / 伤害";
				}
				return DescribeCreateData(fireComponentProjectileWeight.projectileData);
			}
			return DescribeCreateData(fireComponentProjectileSingle.projectileData);
		}
		return DescribeCreateData(data);
	}

	private static string GetBehaviorNodeDetail(Resource resource)
	{
		if (!(resource is ProjectileBehaviorDefinition projectileBehaviorDefinition))
		{
			if (resource is TowerDefenseProjectileCreateData towerDefenseProjectileCreateData)
			{
				return $"行为 {towerDefenseProjectileCreateData.behaviors?.Count ?? 0}";
			}
			return "直线 / 抛物线 / 正弦";
		}
		return projectileBehaviorDefinition.GetType().Name;
	}

	private static string DescribeCreateData(TowerDefenseProjectileCreateData data)
	{
		if (!GodotObject.IsInstanceValid(data) || data.projectileName.IsEmpty)
		{
			return "未配置子弹";
		}
		return data.projectileName.ToString();
	}

	private static string DescribeProjectileResource(FireComponentProjectileResource resource)
	{
		if (!(resource is FireComponentProjectileSingle fireComponentProjectileSingle))
		{
			if (!(resource is FireComponentProjectileWeight fireComponentProjectileWeight))
			{
				if (resource == null)
				{
					return "未配置子弹";
				}
				return resource.GetType().Name;
			}
			return DescribeCreateData(fireComponentProjectileWeight.projectileData);
		}
		return DescribeCreateData(fireComponentProjectileSingle.projectileData);
	}

	private void BindProjectileScenePreview(VBoxContainer root)
	{
		_projectileNameLabel = root.GetNode<Label>("%ProjectileNameLabel");
		_sceneLabel = root.GetNode<Label>("%SceneLabel");
		_projectileViewport = root.GetNode<SubViewport>("%ProjectileViewport");
		_projectilePreviewRoot = root.GetNode<Node2D>("%PreviewRoot");
		_projectileTargetRoot = root.GetNode<Node2D>("%TargetRoot");
		_projectileImpactRoot = root.GetNode<Node2D>("%ImpactRoot");
		_projectileTimelineSlider = root.GetNode<HSlider>("%TimelineSlider");
		_projectilePlayButton = root.GetNode<Button>("%PlayButton");
		_projectilePlaybackStatusLabel = root.GetNode<Label>("%PlaybackStatusLabel");
		_projectilePlayButton.Pressed += PlayProjectilePreview;
		_projectileTimelineSlider.ValueChanged += delegate(double value)
		{
			_projectilePreviewProgress = value;
			_projectilePreviewPlaying = false;
			RequestVisibilityGatedProcessing(requested: false);
			UpdateProjectilePlaybackVisual();
		};
		RebuildProjectileTargetPreview();
		RebuildProjectileScenePreview();
		UpdateProjectileSummary();
		RefreshVisibilityGatedProcessing();
	}

	private void BindProjectileCoreFields(Control root, Resource projectile)
	{
		EnsureProjectilePropertyBinding();
		LineEdit node = root.GetNode<LineEdit>("%ProjectileNameLineEdit");
		LineEdit node2 = root.GetNode<LineEdit>("%SkinNameLineEdit");
		SpinBox node3 = root.GetNode<SpinBox>("%BaseDamageSpinBox");
		SpinBox node4 = root.GetNode<SpinBox>("%SizeXSpinBox");
		SpinBox node5 = root.GetNode<SpinBox>("%SizeYSpinBox");
		SpinBox node6 = root.GetNode<SpinBox>("%ScaleXSpinBox");
		SpinBox node7 = root.GetNode<SpinBox>("%ScaleYSpinBox");
		BindTextIfPresent(root, "%NameRow", node, projectile, "name", UpdateProjectileSummary);
		BindTextIfPresent(root, "%SkinRow", node2, projectile, "skinName", UpdateProjectileSummary);
		BindNumberIfPresent(root, "%DamageRow", node3, projectile, "baseDamage", UpdateProjectileSummary);
		BindVector2IfPresent(root, "%SizeRow", node4, node5, projectile, "size", RebuildProjectileScenePreview);
		BindVector2IfPresent(root, "%ScaleRow", node6, node7, projectile, "scale", RebuildProjectileScenePreview);
		OptionButton projectileObject = root.GetNode<OptionButton>("%ProjectileObjectOption");
		if (root.GetNode<Control>("%ObjectRow").Visible = HasProjectileProperty(projectile, "projectileObject"))
		{
			projectileObject.Clear();
			int num = ReadProjectileInt(projectile, "projectileObject");
			ObjectManagerConfig.OBJECT[] values = Enum.GetValues<ObjectManagerConfig.OBJECT>();
			for (int i = 0; i < values.Length; i++)
			{
				ObjectManagerConfig.OBJECT oBJECT = values[i];
				projectileObject.AddItem(oBJECT.ToString(), (int)oBJECT);
				if (oBJECT == (ObjectManagerConfig.OBJECT)num)
				{
					projectileObject.Select(projectileObject.ItemCount - 1);
				}
			}
			projectileObject.ItemSelected += delegate(long index)
			{
				if (!_updatingControls)
				{
					CommitProjectileProperty("projectileObject", Variant.From<int>(projectileObject.GetItemId((int)index)), refreshPropertyList: true);
				}
			};
			Button objectCatalogButton = root.GetNode<Button>("%ProjectileObjectCatalogButton");
			objectCatalogButton.Text = $"对象池图鉴 · {(ObjectManagerConfig.OBJECT)num}";
			objectCatalogButton.Pressed += delegate
			{
				OpenProjectileObjectCatalog(projectileObject, objectCatalogButton);
			};
		}
		BindResourcePickerIfPresent(root, "%ProjectileSceneRow", "%ProjectileScenePicker", projectile, "projectileScene", RebuildProjectileScenePreview);
		BindStringOptionIfPresent(root, "%SplatTypeRow", "%SplatSceneTypeOption", projectile, "splatSceneType", new string[2] { "Particles", "Sprite" }, UpdateProjectileSummary);
		MountProjectileSegmentedOption(root.GetNode<OptionButton>("%SplatSceneTypeOption"), root.GetNode<HFlowContainer>("%SplatSceneTypeSegments"));
		BindTextIfPresent(root, "%SplatAudioRow", root.GetNode<LineEdit>("%SplatAudioLineEdit"), projectile, "splatAudio", UpdateProjectileSummary);
		BindResourcePickerIfPresent(root, "%SplatSceneRow", "%SplatScenePicker", projectile, "splatScene", UpdateProjectilePlaybackVisual);
		BindResourcePickerIfPresent(root, "%HitEffectRow", "%HitEffectPicker", projectile, "hitEffect", UpdateProjectilePlaybackVisual);
		BindProjectileResourceCatalogButton(root, "%ProjectileSceneCatalogButton", "%ProjectileScenePicker", projectile, "projectileScene", RebuildProjectileScenePreview);
		BindProjectileResourceCatalogButton(root, "%SplatSceneCatalogButton", "%SplatScenePicker", projectile, "splatScene", UpdateProjectilePlaybackVisual);
		BindProjectileResourceCatalogButton(root, "%HitEffectCatalogButton", "%HitEffectPicker", projectile, "hitEffect", UpdateProjectilePlaybackVisual);
		_damageLabel = root.GetNode<Label>("%ProjectileDamageSummary");
		UpdateProjectileSummary();
	}

	private void BindProjectileHitFields(Control root, Resource projectile)
	{
		EnsureProjectilePropertyBinding();
		bool flag = BuildProjectileFlagGrid<TowerDefenseEnum.PROJECTILE_DAMAGE_FLAG>(root.GetNode<GridContainer>("%DamageFlagsGrid"), projectile, "damageFlags");
		bool flag2 = BuildProjectileFlagGrid<TowerDefenseEnum.PROJECTILE_FIRE_METHOD_FLAG>(root.GetNode<GridContainer>("%FireMethodFlagsGrid"), projectile, "fireMethodFlags");
		bool flag3 = BuildProjectileFlagGrid<TowerDefenseEnum.CHARACTER_COLLISION_FLAGS>(root.GetNode<GridContainer>("%CollisionFlagsGrid"), projectile, "collisionFlags");
		root.GetNode<Control>("%FlagsSection").Visible = flag || flag2 || flag3;
		BindNumberIfPresent(root, "%CatapultHeightRow", root.GetNode<SpinBox>("%CatapultHeightSpinBox"), projectile, "catapultHeight", UpdateProjectilePlaybackVisual);
		BindNumberIfPresent(root, "%TrackIntervalRow", root.GetNode<SpinBox>("%TrackIntervalSpinBox"), projectile, "trackSearchInterval", UpdateProjectileSummary);
		BindToggleIfPresent(root.GetNode<BaseButton>("%RotateFollowVelocityCheck"), projectile, "rotateFollowVelocity", UpdateProjectilePlaybackVisual);
		BindToggleIfPresent(root.GetNode<BaseButton>("%HitBodyCheck"), projectile, "hitBody", UpdateProjectileSummary);
		BindToggleIfPresent(root.GetNode<BaseButton>("%IsFireCheck"), projectile, "isFire", UpdateProjectileSummary);
		BindStringOptionIfPresent(root, "%RangeTypeRow", "%RangeTypeOption", projectile, "rangeType", new string[2] { "Default", "Bomb" }, UpdateProjectileSummary);
		MountProjectileSegmentedOption(root.GetNode<OptionButton>("%RangeTypeOption"), root.GetNode<HFlowContainer>("%RangeTypeSegments"));
		BindToggleIfPresent(root.GetNode<BaseButton>("%UseRangeCheck"), projectile, "useRange", UpdateProjectileSummary);
		BindVector2IfPresent(root, "%RangeSizeRow", root.GetNode<SpinBox>("%RangeSizeXSpinBox"), root.GetNode<SpinBox>("%RangeSizeYSpinBox"), projectile, "rangeSize", UpdateProjectileSummary);
		BindNumberIfPresent(root, "%HitPercentageRow", root.GetNode<SpinBox>("%HitPercentageSpinBox"), projectile, "hitPesontage", UpdateProjectileSummary);
		bool visible = HasProjectileProperty(projectile, "penetrateNum") || HasProjectileProperty(projectile, "penetrateOverBack") || HasProjectileProperty(projectile, "backOutGround") || HasProjectileProperty(projectile, "backDuration");
		root.GetNode<Control>("%PenetrateSection").Visible = visible;
		BindNumberIfPresent(root, "%PenetrateNumRow", root.GetNode<SpinBox>("%PenetrateNumSpinBox"), projectile, "penetrateNum", UpdateProjectileSummary);
		BindToggleIfPresent(root.GetNode<BaseButton>("%PenetrateOverBackCheck"), projectile, "penetrateOverBack", UpdateProjectileSummary);
		BindToggleIfPresent(root.GetNode<BaseButton>("%BackOutGroundCheck"), projectile, "backOutGround", UpdateProjectileSummary);
		BindNumberIfPresent(root, "%BackDurationRow", root.GetNode<SpinBox>("%BackDurationSpinBox"), projectile, "backDuration", UpdateProjectilePlaybackVisual);
		_flagLabel = root.GetNode<Label>("%ProjectileFlagSummary");
		_rangeLabel = root.GetNode<Label>("%ProjectileRangeSummary");
		_hitTargetEventList = root.GetNode<ItemList>("%HitTargetEventList");
		_hitCharacterEventList = root.GetNode<ItemList>("%HitCharacterEventList");
		_hitGroundEventList = root.GetNode<ItemList>("%HitGroundEventList");
		_behaviorsList = root.GetNode<ItemList>("%BehaviorsList");
		root.GetNode<Control>("%BehaviorsBox").Visible = HasProjectileProperty(projectile, "behaviors");
		_hitTargetEventList.ItemActivated += delegate(long index)
		{
			OpenProjectileNestedResource(_hitTargetEventList, "hitTargetEventList", (int)index);
		};
		_hitCharacterEventList.ItemActivated += delegate(long index)
		{
			OpenProjectileNestedResource(_hitCharacterEventList, "hitCharacterEventList", (int)index);
		};
		_hitGroundEventList.ItemActivated += delegate(long index)
		{
			OpenProjectileNestedResource(_hitGroundEventList, "hitGroundEventList", (int)index);
		};
		_behaviorsList.ItemActivated += delegate(long index)
		{
			OpenProjectileNestedResource(_behaviorsList, "behaviors", (int)index);
		};
		RefreshProjectileLists(projectile);
		UpdateProjectileSummary();
	}

	private void EnsureProjectilePropertyBinding()
	{
		if (_projectilePropertyBinding == null)
		{
			_projectilePropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), delegate
			{
				NotifyCurrentResourceEdited();
			});
		}
	}

	private void BindTextIfPresent(Control root, NodePath rowPath, LineEdit control, Resource projectile, StringName property, Action refresh)
	{
		bool flag = HasProjectileProperty(projectile, property.ToString());
		root.GetNode<Control>(rowPath).Visible = flag;
		if (flag)
		{
			_projectilePropertyBinding.BindVariantText(control, null, projectile, property, refresh);
		}
	}

	private void BindNumberIfPresent(Control root, NodePath rowPath, Godot.Range control, Resource projectile, StringName property, Action refresh)
	{
		bool flag = HasProjectileProperty(projectile, property.ToString());
		root.GetNode<Control>(rowPath).Visible = flag;
		if (flag)
		{
			_projectilePropertyBinding.BindNumber(control, projectile, property, refresh);
		}
	}

	private void BindVector2IfPresent(Control root, NodePath rowPath, SpinBox xControl, SpinBox yControl, Resource projectile, StringName property, Action refresh)
	{
		bool flag = HasProjectileProperty(projectile, property.ToString());
		root.GetNode<Control>(rowPath).Visible = flag;
		if (flag)
		{
			_projectilePropertyBinding.BindVector2Range(xControl, yControl, projectile, property, refresh);
		}
	}

	private void BindToggleIfPresent(BaseButton control, Resource projectile, StringName property, Action refresh)
	{
		if (control.Visible = HasProjectileProperty(projectile, property.ToString()))
		{
			_projectilePropertyBinding.BindToggle(control, projectile, property, refresh);
		}
	}

	private void BindResourcePickerIfPresent(Control root, NodePath rowPath, NodePath pickerPath, Resource projectile, StringName property, Action refresh)
	{
		bool flag = HasProjectileProperty(projectile, property.ToString());
		root.GetNode<Control>(rowPath).Visible = flag;
		if (flag)
		{
			XWResourcePicker node = root.GetNode<XWResourcePicker>(pickerPath);
			node.Setup("PackedScene");
			node.SetEditedResource(ReadProjectilePackedScene(projectile, property.ToString()));
			node.ResourceChanged += delegate(Resource resource)
			{
				CommitProjectileProperty(property, Variant.From<PackedScene>(resource as PackedScene), refreshPropertyList: false);
				refresh?.Invoke();
			};
		}
	}

	private void BindProjectileResourceCatalogButton(Control root, NodePath buttonPath, NodePath pickerPath, Resource projectile, StringName property, Action refresh)
	{
		Button node = root.GetNode<Button>(buttonPath);
		XWResourcePicker surfacePicker = root.GetNode<XWResourcePicker>(pickerPath);
		if (!(node.Visible = HasProjectileProperty(projectile, property.ToString())))
		{
			return;
		}
		node.Pressed += delegate
		{
			EnsureProjectilePicker();
			PackedScene packedScene = ReadProjectilePackedScene(projectile, property.ToString());
			_projectilePicker?.OpenResourceLibrary("PackedScene", property.ToString(), packedScene?.ResourcePath ?? string.Empty, XWFileSystem.GetSingleton()?.ProjectFolderPath ?? string.Empty, new string[1] { "PackedScene" }, new string[3] { "Scenes", "Resources", "Asset" }, "res://addons/ModEditor/Icons/PackedScene.svg", delegate(XWGameplayResourceChoice choice)
			{
				PackedScene from = ResourceLoader.Load<PackedScene>(choice?.ResourcePath ?? string.Empty, null, ResourceLoader.CacheMode.Reuse);
				surfacePicker.SetEditedResource(from);
				CommitProjectileProperty(property, Variant.From(in from), refreshPropertyList: false);
				refresh?.Invoke();
			});
		};
	}

	private void BindStringOptionIfPresent(Control root, NodePath rowPath, NodePath optionPath, Resource projectile, StringName property, IReadOnlyList<string> options, Action refresh)
	{
		bool flag = HasProjectileProperty(projectile, property.ToString());
		root.GetNode<Control>(rowPath).Visible = flag;
		if (!flag)
		{
			return;
		}
		OptionButton option = root.GetNode<OptionButton>(optionPath);
		string b = ReadProjectileString(projectile, property.ToString());
		option.Clear();
		for (int i = 0; i < options.Count; i++)
		{
			option.AddItem(options[i], i);
			if (string.Equals(options[i], b, StringComparison.OrdinalIgnoreCase))
			{
				option.Select(i);
			}
		}
		option.ItemSelected += delegate(long index)
		{
			if (!_updatingControls)
			{
				CommitProjectileProperty(property, Variant.From<string>(option.GetItemText((int)index)), refreshPropertyList: false);
				refresh?.Invoke();
			}
		};
	}

	private bool BuildProjectileFlagGrid<TEnum>(GridContainer grid, Resource projectile, StringName property) where TEnum : struct, Enum
	{
		if (!HasProjectileProperty(projectile, property.ToString()))
		{
			grid.Visible = false;
			return false;
		}
		grid.Visible = true;
		int num = ReadProjectileInt(projectile, property.ToString());
		TEnum[] values = Enum.GetValues<TEnum>();
		for (int i = 0; i < values.Length; i++)
		{
			TEnum val = values[i];
			int flag = Convert.ToInt32(val);
			if (flag != 0)
			{
				CheckButton checkButton = new CheckButton
				{
					Text = val.ToString(),
					ButtonPressed = ((num & flag) == flag),
					TooltipText = $"{property}: {flag}",
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				checkButton.Toggled += delegate(bool pressed)
				{
					int num2 = ReadProjectileInt(projectile, property.ToString());
					num2 = (pressed ? (num2 | flag) : (num2 & ~flag));
					CommitProjectileProperty(property, Variant.From(in num2), refreshPropertyList: true);
					UpdateProjectileSummary();
					UpdateProjectilePlaybackVisual();
				};
				grid.AddChild(checkButton, forceReadableName: false, InternalMode.Disabled);
			}
		}
		return true;
	}

	private void CommitProjectileProperty(StringName property, Variant value, bool refreshPropertyList)
	{
		if (GodotObject.IsInstanceValid(_editingProjectile) && HasProjectileProperty(_editingProjectile, property.ToString()))
		{
			EnsureProjectilePropertyBinding();
			_projectilePropertyBinding.SetValue(_editingProjectile, property, value, $"修改子弹 {property}");
			if (refreshPropertyList)
			{
				_editingProjectile.NotifyPropertyListChanged();
			}
		}
	}

	private void OpenProjectileNestedResource(ItemList list, string propertyName, int index)
	{
		if (GodotObject.IsInstanceValid(list) && index >= 0 && index < list.ItemCount)
		{
			Variant itemMetadata = list.GetItemMetadata(index);
			if (itemMetadata.VariantType == Variant.Type.Object && itemMetadata.AsGodotObject() is Resource resource && GodotObject.IsInstanceValid(resource))
			{
				XWResourceEditContext context = XWResourceEditContext.ForProperty(resource, _editingProjectile, resource.ResourcePath, base.CurrentResourcePath, propertyName, index, "projectile_editor", base.CurrentEditContext?.IsBuiltInSource ?? false, GetProjectilePreviewKey(_editingProjectile));
				XWEditorInterface.Instance?.EditResource(resource, context);
			}
		}
	}

	private void RebuildProjectileScenePreview()
	{
		if (!GodotObject.IsInstanceValid(_projectilePreviewRoot) || _editingProjectile == null)
		{
			return;
		}
		foreach (Node child in _projectilePreviewRoot.GetChildren())
		{
			child.QueueFree();
		}
		_projectileSceneInstance = null;
		Node node = null;
		PackedScene packedScene = ReadProjectilePackedScene(_editingProjectile, "projectileScene");
		if (GodotObject.IsInstanceValid(packedScene))
		{
			try
			{
				node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			}
			catch (Exception ex)
			{
				GD.PushWarning("Projectile editor scene preview failed: " + ex.Message);
			}
		}
		if (node != null)
		{
			node.Name = "ConfiguredProjectileScenePreview";
			TowerDefenseProjectile towerDefenseProjectile = FindRuntimeProjectile(node);
			if (towerDefenseProjectile != null)
			{
				towerDefenseProjectile.suppressGameplay = true;
			}
			Vector2 scale = ReadProjectileVector2(_editingProjectile, "scale", Vector2.One);
			if (node is Node2D node2D)
			{
				node2D.Scale = scale;
			}
			else if (node is Control control)
			{
				control.Scale = scale;
			}
			_projectilePreviewRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_projectileSceneInstance = node;
			UpdateProjectilePlaybackVisual();
		}
		else
		{
			Vector2 vector = ReadProjectileVector2(_editingProjectile, "size", new Vector2(28f, 28f));
			ColorRect colorRect = new ColorRect
			{
				Name = "ProjectileFallbackShape",
				Color = new Color(0.45f, 0.7f, 1f, 0.85f),
				CustomMinimumSize = vector
			};
			colorRect.Position = -vector * 0.5f;
			_projectilePreviewRoot.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
			_projectileSceneInstance = colorRect;
			UpdateProjectilePlaybackVisual();
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (_projectilePreviewPlaying && !GodotObject.IsInstanceValid(_projectileSceneInstance))
		{
			_projectilePreviewPlaying = false;
		}
		if (_projectilePreviewPlaying)
		{
			_projectilePreviewProgress = Mathf.Clamp(_projectilePreviewProgress + delta / 1.35, 0.0, 1.0);
			if (GodotObject.IsInstanceValid(_projectileTimelineSlider))
			{
				_projectileTimelineSlider.SetValueNoSignal(_projectilePreviewProgress);
			}
			UpdateProjectilePlaybackVisual();
			if (_projectilePreviewProgress >= 1.0)
			{
				_projectilePreviewPlaying = false;
			}
		}
		if (_pipelinePreviewPlaying && !GodotObject.IsInstanceValid(_pipelineProjectileInstance))
		{
			_pipelinePreviewPlaying = false;
		}
		if (_pipelinePreviewPlaying)
		{
			_pipelinePreviewProgress = Mathf.Clamp(_pipelinePreviewProgress + delta / GetPipelinePreviewDuration(), 0.0, 1.0);
			if (GodotObject.IsInstanceValid(_pipelineTimelineSlider))
			{
				_pipelineTimelineSlider.SetValueNoSignal(_pipelinePreviewProgress);
			}
			UpdatePipelinePlaybackVisual();
			if (_pipelinePreviewProgress >= 1.0)
			{
				_pipelinePreviewPlaying = false;
			}
		}
		if (!_projectilePreviewPlaying && !_pipelinePreviewPlaying)
		{
			RequestVisibilityGatedProcessing(requested: false);
		}
	}

	private double GetPipelinePreviewDuration()
	{
		if (!(base.CurrentResource is FireComponentFireProjectileConfig fireComponentFireProjectileConfig))
		{
			return 1.35;
		}
		double num = Math.Max(1.0, Math.Abs(fireComponentFireProjectileConfig.speed));
		return Math.Clamp(494.0 / num, 0.25, 4.0);
	}

	private void PlayProjectilePreview()
	{
		ClearProjectileImpactPreview();
		_projectilePreviewProgress = 0.0;
		_projectilePreviewPlaying = GodotObject.IsInstanceValid(_projectileSceneInstance);
		if (GodotObject.IsInstanceValid(_projectileTimelineSlider))
		{
			_projectileTimelineSlider.SetValueNoSignal(0.0);
		}
		UpdateProjectilePlaybackVisual();
		if (_projectilePreviewPlaying)
		{
			RequestVisibilityGatedProcessing(requested: true);
		}
		else
		{
			RequestVisibilityGatedProcessing(requested: false);
		}
	}

	private void PlayPipelinePreview()
	{
		_pipelinePreviewProgress = 0.0;
		_pipelinePreviewPlaying = GodotObject.IsInstanceValid(_pipelineProjectileInstance);
		if (GodotObject.IsInstanceValid(_pipelineTimelineSlider))
		{
			_pipelineTimelineSlider.SetValueNoSignal(0.0);
		}
		UpdatePipelinePlaybackVisual();
		RequestVisibilityGatedProcessing(_pipelinePreviewPlaying || _projectilePreviewPlaying);
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		ApplyProjectileRuntimeProcessMode(visible);
	}

	private void ApplyProjectileRuntimeProcessMode(bool visible)
	{
		ProcessModeEnum processMode = (ProcessModeEnum)(visible ? 0 : 4);
		if (GodotObject.IsInstanceValid(_projectilePreviewRoot))
		{
			_projectilePreviewRoot.ProcessMode = processMode;
		}
		if (GodotObject.IsInstanceValid(_projectileTargetRoot))
		{
			_projectileTargetRoot.ProcessMode = processMode;
		}
		if (GodotObject.IsInstanceValid(_projectileImpactRoot))
		{
			_projectileImpactRoot.ProcessMode = processMode;
		}
		if (GodotObject.IsInstanceValid(_pipelineProjectileRoot))
		{
			_pipelineProjectileRoot.ProcessMode = processMode;
		}
		if (GodotObject.IsInstanceValid(_pipelineTargetRoot))
		{
			_pipelineTargetRoot.ProcessMode = processMode;
		}
	}

	private void UpdatePipelinePlaybackVisual()
	{
		if (!GodotObject.IsInstanceValid(_pipelineProjectileRoot))
		{
			return;
		}
		float num = (float)Mathf.Clamp(_pipelinePreviewProgress, 0.0, 1.0);
		Vector2 position = EvaluatePipelinePosition(num);
		_pipelineProjectileRoot.Position = position;
		TowerDefenseProjectileConfig pipelinePreviewConfig = _pipelinePreviewConfig;
		if (pipelinePreviewConfig != null && pipelinePreviewConfig.rotateFollowVelocity && _pipelineProjectileInstance is Node2D node2D)
		{
			Vector2 vector = EvaluatePipelinePosition(Mathf.Min(num + 0.01f, 1f));
			if (!position.IsEqualApprox(vector))
			{
				node2D.Rotation = position.DirectionTo(vector).Angle();
			}
		}
		if (GodotObject.IsInstanceValid(_pipelinePreviewStatus))
		{
			_pipelinePreviewStatus.Text = ((!GodotObject.IsInstanceValid(_pipelinePreviewConfig)) ? "缺失子弹配置" : ((num >= 1f) ? "轨迹预览完成" : (_pipelinePreviewPlaying ? "安全飞行预览中" : $"时间 {num:P0}")));
		}
	}

	private void UpdateProjectilePlaybackVisual()
	{
		if (!GodotObject.IsInstanceValid(_projectilePreviewRoot))
		{
			return;
		}
		float num = (float)Mathf.Clamp(_projectilePreviewProgress, 0.0, 1.0);
		Vector2 vector = new Vector2(120f, 218f);
		Vector2 to = new Vector2(552f, 190f);
		Vector2 position = vector.Lerp(to, num);
		bool flag = (ReadProjectileInt(_editingProjectile, "fireMethodFlags", 1) & 2) != 0;
		if (flag)
		{
			float num2 = Mathf.Clamp((float)ReadProjectileDouble(_editingProjectile, "catapultHeight", 300.0), 30f, 420f);
			position.Y -= Mathf.Sin(num * (float)Math.PI) * num2 * 0.42f;
		}
		_projectilePreviewRoot.Position = position;
		if (ReadProjectileBool(_editingProjectile, "rotateFollowVelocity") && GodotObject.IsInstanceValid(_projectileSceneInstance))
		{
			Vector2 to2 = vector.Lerp(to, Mathf.Min(num + 0.01f, 1f));
			if (flag)
			{
				to2.Y -= Mathf.Sin(Mathf.Min(num + 0.01f, 1f) * (float)Math.PI) * Mathf.Clamp((float)ReadProjectileDouble(_editingProjectile, "catapultHeight", 300.0), 30f, 420f) * 0.42f;
			}
			if (_projectileSceneInstance is Node2D node2D)
			{
				node2D.Rotation = position.DirectionTo(to2).Angle();
			}
		}
		if (GodotObject.IsInstanceValid(_projectilePlaybackStatusLabel))
		{
			_projectilePlaybackStatusLabel.Text = ((num >= 1f) ? "已命中目标" : (_projectilePreviewPlaying ? "飞行中" : $"时间 {num:P0}"));
		}
		if (num >= 1f)
		{
			ShowProjectileImpactPreview();
		}
		else
		{
			ClearProjectileImpactPreview();
		}
	}

	private void RebuildProjectileTargetPreview()
	{
		if (!GodotObject.IsInstanceValid(_projectileTargetRoot))
		{
			return;
		}
		foreach (Node child in _projectileTargetRoot.GetChildren())
		{
			child.QueueFree();
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Puzzle/Target/Scene/TowerDefenseZombieTarget.tscn", null, ResourceLoader.CacheMode.Reuse);
		if (GodotObject.IsInstanceValid(packedScene))
		{
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(node);
			if (towerDefenseCharacter != null)
			{
				towerDefenseCharacter.inGame = false;
				towerDefenseCharacter.editorPreviewMode = true;
			}
			_projectileTargetRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void ShowProjectileImpactPreview()
	{
		if (!GodotObject.IsInstanceValid(_projectileImpactRoot) || _projectileImpactRoot.GetChildCount() > 0)
		{
			return;
		}
		PackedScene packedScene = ReadProjectilePackedScene(_editingProjectile, "splatScene") ?? ReadProjectilePackedScene(_editingProjectile, "hitEffect");
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return;
		}
		try
		{
			_projectileImpactRoot.AddChild(packedScene.Instantiate(PackedScene.GenEditState.Disabled), forceReadableName: false, InternalMode.Disabled);
		}
		catch (Exception ex)
		{
			GD.PushWarning("Projectile impact preview failed: " + ex.Message);
		}
	}

	private void ClearProjectileImpactPreview()
	{
		if (!GodotObject.IsInstanceValid(_projectileImpactRoot))
		{
			return;
		}
		foreach (Node child in _projectileImpactRoot.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static TowerDefenseProjectile FindRuntimeProjectile(Node node)
	{
		if (node is TowerDefenseProjectile result)
		{
			return result;
		}
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		foreach (Node child in node.GetChildren())
		{
			TowerDefenseProjectile towerDefenseProjectile = FindRuntimeProjectile(child);
			if (GodotObject.IsInstanceValid(towerDefenseProjectile))
			{
				return towerDefenseProjectile;
			}
		}
		return null;
	}

	private static TowerDefenseCharacter FindRuntimeCharacter(Node node)
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
			TowerDefenseCharacter towerDefenseCharacter = FindRuntimeCharacter(child);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private void UpdateProjectileSummary()
	{
		if (_editingProjectile == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_projectileNameLabel))
			{
				_projectileNameLabel.Text = "子弹: " + EmptyToPlaceholder(ReadProjectileString(_editingProjectile, "name"));
			}
			if (GodotObject.IsInstanceValid(_sceneLabel))
			{
				_sceneLabel.Text = "projectileScene: " + FormatResource(ReadProjectilePackedScene(_editingProjectile, "projectileScene"));
			}
			if (GodotObject.IsInstanceValid(_projectileChangeHintLabel))
			{
				_projectileChangeHintLabel.Text = "当前子弹 key: " + EmptyToPlaceholder(ReadProjectileString(_editingProjectile, "name")) + "。双击下面的 ProjectileChange 资源可打开链路编辑器。";
			}
			if (GodotObject.IsInstanceValid(_damageLabel))
			{
				_damageLabel.Text = $"伤害: {ReadProjectileDouble(_editingProjectile, "baseDamage", 20.0):0.##}, size={FormatVector(ReadProjectileVector2(_editingProjectile, "size", new Vector2(28f, 28f)))}, scale={FormatVector(ReadProjectileVector2(_editingProjectile, "scale", Vector2.One))}";
			}
			if (GodotObject.IsInstanceValid(_flagLabel))
			{
				_flagLabel.Text = (HasProjectileProperty(_editingProjectile, "damageFlags") ? $"damageFlags={ReadProjectileInt(_editingProjectile, "damageFlags")}, fireMethodFlags={ReadProjectileInt(_editingProjectile, "fireMethodFlags")}, collisionFlags={ReadProjectileInt(_editingProjectile, "collisionFlags")}" : ("isFire=" + FormatBool(ReadProjectileBool(_editingProjectile, "isFire"))));
			}
			if (GodotObject.IsInstanceValid(_rangeLabel))
			{
				_rangeLabel.Text = $"rangeType={ReadProjectileString(_editingProjectile, "rangeType", "Default")}, useRange={FormatBool(ReadProjectileBool(_editingProjectile, "useRange"))}, rangeSize={FormatVector(ReadProjectileVector2(_editingProjectile, "rangeSize", new Vector2(0.5f, 0.5f)))}";
			}
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshProjectileLists(Resource projectile)
	{
		PopulateList(_hitTargetEventList, ReadProjectileEnumerable(projectile, "hitTargetEventList"));
		PopulateList(_hitCharacterEventList, ReadProjectileEnumerable(projectile, "hitCharacterEventList"));
		PopulateList(_hitGroundEventList, ReadProjectileEnumerable(projectile, "hitGroundEventList"));
		PopulateList(_behaviorsList, ReadProjectileEnumerable(projectile, "behaviors"));
	}

	private void BindProjectileChangePanel(VBoxContainer root, Resource projectile)
	{
		root.GetNode<Button>("%RefreshButton").Pressed += RefreshProjectileChangeList;
		_projectileChangeHintLabel = root.GetNode<Label>("%ChangeHint");
		_projectileChangeHintLabel.Text = "当前子弹 key: " + EmptyToPlaceholder(ReadProjectileString(projectile, "name")) + "。双击下面的 ProjectileChange 资源可打开链路编辑器。";
		_projectileChangeList = root.GetNode<ItemList>("%ProjectileChangeList");
		_projectileChangeList.ItemActivated += OpenProjectileChangeItem;
		RefreshProjectileChangeList();
	}

	private void RefreshProjectileChangeList()
	{
		if (!GodotObject.IsInstanceValid(_projectileChangeList))
		{
			return;
		}
		_projectileChangeList.Clear();
		foreach (string item in CollectProjectileChangeResources())
		{
			string from = item;
			int idx = _projectileChangeList.AddItem(from.GetFile().GetBaseName());
			_projectileChangeList.SetItemTooltip(idx, from);
			_projectileChangeList.SetItemMetadata(idx, Variant.From(in from));
		}
		if (_projectileChangeList.ItemCount == 0)
		{
			_projectileChangeList.AddItem("未找到 Resources/ProjectileChanges 中的子弹变化资源");
		}
	}

	private void OpenProjectileChangeItem(long index)
	{
		if (!GodotObject.IsInstanceValid(_projectileChangeList) || index < 0 || index >= _projectileChangeList.ItemCount)
		{
			return;
		}
		Variant itemMetadata = _projectileChangeList.GetItemMetadata((int)index);
		if (itemMetadata.VariantType == Variant.Type.String)
		{
			string text = itemMetadata.AsString();
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWResourceEditorRegistry.TryOpenPath(text);
			}
		}
	}

	private static List<string> CollectProjectileChangeResources()
	{
		List<string> list = new List<string>();
		string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return list;
		}
		string path = Path.Combine(text, "Resources", "ProjectileChanges");
		if (!Directory.Exists(path))
		{
			return list;
		}
		foreach (string item in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
		{
			string text2 = Path.GetExtension(item).ToLowerInvariant();
			if ((text2 == ".tres" || text2 == ".res") ? true : false)
			{
				list.Add(item.Replace('\\', '/'));
			}
		}
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static void PopulateList(ItemList list, IEnumerable items)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return;
		}
		list.Clear();
		if (items == null)
		{
			list.AddItem("未配置");
			return;
		}
		int num = 0;
		foreach (object item in items)
		{
			int idx = list.AddItem($"{num}: {FormatObject(item)}");
			Resource from = item as Resource;
			if (item is Variant variant && variant.VariantType == Variant.Type.Object && variant.AsGodotObject() is Resource resource)
			{
				from = resource;
			}
			if (GodotObject.IsInstanceValid(from))
			{
				list.SetItemMetadata(idx, Variant.From(in from));
			}
			num++;
		}
		if (num == 0)
		{
			list.AddItem("未配置");
		}
	}

	private static bool HasProjectileProperty(Resource projectile, string propertyName)
	{
		if (!GodotObject.IsInstanceValid(projectile) || string.IsNullOrWhiteSpace(propertyName))
		{
			return false;
		}
		foreach (Dictionary property in projectile.GetPropertyList())
		{
			if (property.ContainsKey("name") && property["name"].AsString() == propertyName)
			{
				return true;
			}
		}
		return false;
	}

	private static string ReadProjectileString(Resource projectile, string propertyName, string defaultValue = "")
	{
		Variant variant = ReadProjectileVariant(projectile, propertyName);
		if (variant.VariantType != Variant.Type.Nil)
		{
			return variant.AsString();
		}
		return defaultValue;
	}

	private static double ReadProjectileDouble(Resource projectile, string propertyName, double defaultValue = 0.0)
	{
		Variant variant = ReadProjectileVariant(projectile, propertyName);
		Variant.Type variantType = variant.VariantType;
		if ((ulong)(variantType - 2) > 1uL || 1 == 0)
		{
			return defaultValue;
		}
		return variant.AsDouble();
	}

	private static int ReadProjectileInt(Resource projectile, string propertyName, int defaultValue = 0)
	{
		Variant variant = ReadProjectileVariant(projectile, propertyName);
		Variant.Type variantType = variant.VariantType;
		if ((ulong)(variantType - 2) > 1uL || 1 == 0)
		{
			return defaultValue;
		}
		return variant.AsInt32();
	}

	private static bool ReadProjectileBool(Resource projectile, string propertyName, bool defaultValue = false)
	{
		Variant variant = ReadProjectileVariant(projectile, propertyName);
		if (variant.VariantType != Variant.Type.Bool)
		{
			return defaultValue;
		}
		return variant.AsBool();
	}

	private static Vector2 ReadProjectileVector2(Resource projectile, string propertyName, Vector2 defaultValue)
	{
		Variant variant = ReadProjectileVariant(projectile, propertyName);
		if (variant.VariantType != Variant.Type.Vector2)
		{
			return defaultValue;
		}
		return variant.AsVector2();
	}

	private static PackedScene ReadProjectilePackedScene(Resource projectile, string propertyName)
	{
		Variant variant = ReadProjectileVariant(projectile, propertyName);
		if (variant.VariantType != Variant.Type.Object)
		{
			return null;
		}
		return variant.AsGodotObject() as PackedScene;
	}

	private static IEnumerable ReadProjectileEnumerable(Resource projectile, string propertyName)
	{
		Variant variant = ReadProjectileVariant(projectile, propertyName);
		if (variant.VariantType == Variant.Type.Array)
		{
			return variant.AsGodotArray();
		}
		if (variant.VariantType == Variant.Type.Object)
		{
			return variant.AsGodotObject() as IEnumerable;
		}
		return null;
	}

	private static Variant ReadProjectileVariant(Resource projectile, string propertyName)
	{
		if (!GodotObject.IsInstanceValid(projectile) || string.IsNullOrWhiteSpace(propertyName) || !HasProjectileProperty(projectile, propertyName))
		{
			return default(Variant);
		}
		try
		{
			return projectile.Get(propertyName);
		}
		catch
		{
			return default(Variant);
		}
	}

	private static string FormatObject(object value)
	{
		if (value is Variant variant)
		{
			if (variant.VariantType != Variant.Type.Object)
			{
				return variant.ToString();
			}
			value = variant.AsGodotObject();
		}
		if (value is Resource resource)
		{
			return FormatResource(resource);
		}
		if (!(value is GodotObject godotObject))
		{
			return value?.ToString() ?? "空";
		}
		return godotObject.GetType().Name;
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

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
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
}
