using System;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWToolEventVisualResourceEditor : XWGenericVisualResourceEditor
{
	private enum PreviewPhase
	{
		Idle,
		ToolDown,
		Effect,
		Recover
	}

	private const string VisualEditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWToolEventVisualEditorLayout.tscn";

	private static PackedScene _visualEditorLayoutScene;

	private Resource _editingEvent;

	private Label _eventType;

	private Label _outcome;

	private Label _formHint;

	private Control _baseFields;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private Control _createSunFields;

	private Control _recycleFields;

	private Control _jalapenoFields;

	private Control _projectileFields;

	private Control _packetFields;

	private SpinBox _sunNum;

	private SpinBox _recyclePercentage;

	private CheckBox _recycleDestroy;

	private CheckBox _jalapenoDestroy;

	private SpinBox _projectileEveryNum;

	private LineEdit _projectileName;

	private SpinBox _projectileDamage;

	private LineEdit _packetName;

	private SpinBox _packetEveryNum;

	private SpinBox _targetCost;

	private HSlider _targetHealth;

	private Label _healthValue;

	private CheckBox _izmMode;

	private Node2D _targetRoot;

	private Node _targetPlant;

	private ProgressBar _targetHealthBar;

	private Node2D _toolRoot;

	private TextureRect _shovelTool;

	private TextureRect _mowerTool;

	private TextureRect _sunEffect;

	private TextureRect _projectileEffect;

	private TextureRect _packetEffect;

	private ColorRect _fireEffect;

	private PanelContainer _pumpkinEffect;

	private Label _effectLabel;

	private XWVisualPropertyBinding _propertyBinding;

	private PreviewPhase _previewPhase;

	private double _previewElapsed;

	private bool _effectApplied;

	public override void _Ready()
	{
		SetProcess(enable: false);
		base.VisibilityChanged += UpdatePreviewProcessingVisibility;
		base._Ready();
	}

	public override void _ExitTree()
	{
		DisposeToolEventBinding();
		base._ExitTree();
	}

	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(_toolRoot) || !IsVisibleInTree())
		{
			SetProcess(enable: false);
			return;
		}
		_previewElapsed += delta;
		switch (_previewPhase)
		{
		case PreviewPhase.ToolDown:
			UpdateToolDownPhase();
			break;
		case PreviewPhase.Effect:
			UpdateEffectPhase();
			break;
		case PreviewPhase.Recover:
			UpdateRecoverPhase();
			break;
		default:
			SetProcess(enable: false);
			break;
		}
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeToolEventBinding();
		if ((base.CurrentResource is MowerEventConfig || base.CurrentResource is ShovelEventConfig) && base.CanvasGrid != null)
		{
			_editingEvent = base.CurrentResource;
			base.CanvasGrid.Columns = 1;
			if (_visualEditorLayoutScene == null)
			{
				_visualEditorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWToolEventVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _visualEditorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindWorkbench(vBoxContainer);
				PopulateEventFields();
				BindDirectEditControls(_editingEvent);
				UpdateSimulationPreview();
				AddSummaryRows();
				UpdatePreviewProcessingVisibility();
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!IsBuiltInToolEventWithCompleteVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool IsBuiltInToolEventWithCompleteVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(MowerEventConfig)) && !(type == typeof(MowerEventCreateSunConfig)) && !(type == typeof(ShovelEventConfig)) && !(type == typeof(ShovelEventCaptainShovelConfig)) && !(type == typeof(ShovelEventCreateProjectileConfig)) && !(type == typeof(ShovelEventDestroyConfig)) && !(type == typeof(ShovelEventJalapenoConfig)) && !(type == typeof(ShovelEventPacketSpawnConfig)) && !(type == typeof(ShovelEventPumpkinShovelConfig)) && !(type == typeof(ShovelEventRecycleConfig)))
		{
			return type == typeof(ShovelEventSkeletonShovelConfig);
		}
		return true;
	}

	private void BindWorkbench(VBoxContainer root)
	{
		_eventType = root.GetNode<Label>("%EventType");
		_outcome = root.GetNode<Label>("%Outcome");
		_formHint = root.GetNode<Label>("%FormHint");
		_baseFields = root.GetNode<Control>("%BaseFields");
		_resourceName = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToScene = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_createSunFields = root.GetNode<Control>("%CreateSunFields");
		_recycleFields = root.GetNode<Control>("%RecycleFields");
		_jalapenoFields = root.GetNode<Control>("%JalapenoFields");
		_projectileFields = root.GetNode<Control>("%ProjectileFields");
		_packetFields = root.GetNode<Control>("%PacketFields");
		_sunNum = root.GetNode<SpinBox>("%SunNum");
		_recyclePercentage = root.GetNode<SpinBox>("%RecyclePercentage");
		_recycleDestroy = root.GetNode<CheckBox>("%RecycleDestroy");
		_jalapenoDestroy = root.GetNode<CheckBox>("%JalapenoDestroy");
		_projectileEveryNum = root.GetNode<SpinBox>("%ProjectileEveryNum");
		_projectileName = root.GetNode<LineEdit>("%ProjectileName");
		_projectileDamage = root.GetNode<SpinBox>("%ProjectileDamage");
		_packetName = root.GetNode<LineEdit>("%PacketName");
		_packetEveryNum = root.GetNode<SpinBox>("%PacketEveryNum");
		_targetCost = root.GetNode<SpinBox>("%TargetCost");
		_targetHealth = root.GetNode<HSlider>("%TargetHealth");
		_healthValue = root.GetNode<Label>("%HealthValue");
		_izmMode = root.GetNode<CheckBox>("%IzmMode");
		_targetRoot = root.GetNode<Node2D>("%TargetRoot");
		_targetPlant = root.GetNode<Node>("%TargetPlant");
		_targetHealthBar = root.GetNode<ProgressBar>("%TargetHealthBar");
		_toolRoot = root.GetNode<Node2D>("%ToolRoot");
		_shovelTool = root.GetNode<TextureRect>("%ShovelTool");
		_mowerTool = root.GetNode<TextureRect>("%MowerTool");
		_sunEffect = root.GetNode<TextureRect>("%SunEffect");
		_projectileEffect = root.GetNode<TextureRect>("%ProjectileEffect");
		_packetEffect = root.GetNode<TextureRect>("%PacketEffect");
		_fireEffect = root.GetNode<ColorRect>("%FireEffect");
		_pumpkinEffect = root.GetNode<PanelContainer>("%PumpkinEffect");
		_effectLabel = root.GetNode<Label>("%EffectLabel");
		root.GetNode<Button>("%ApplyButton").Pressed += StartToolEffectPreview;
		_targetCost.ValueChanged += delegate
		{
			UpdateSimulationPreview();
		};
		_targetHealth.ValueChanged += delegate
		{
			UpdateSimulationPreview();
		};
		_izmMode.Toggled += delegate
		{
			UpdateSimulationPreview();
		};
	}

	private void BindDirectEditControls(Resource toolEvent)
	{
		if (GodotObject.IsInstanceValid(toolEvent))
		{
			_propertyBinding?.Dispose();
			_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnToolEventPropertyEdited);
			_propertyBinding.BindText(_resourceName, toolEvent, "resource_name", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
			_propertyBinding.BindToggle(_localToScene, toolEvent, "resource_local_to_scene", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
			if (toolEvent is MowerEventCreateSunConfig resource)
			{
				_propertyBinding.BindNumber(_sunNum, resource, "num", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
			}
			if (toolEvent is ShovelEventRecycleConfig resource2)
			{
				_propertyBinding.BindNumber(_recyclePercentage, resource2, "percentage", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
				_propertyBinding.BindToggle(_recycleDestroy, resource2, "destroy", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
			}
			if (toolEvent is ShovelEventJalapenoConfig resource3)
			{
				_propertyBinding.BindToggle(_jalapenoDestroy, resource3, "destroy", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
			}
			if (toolEvent is ShovelEventCreateProjectileConfig resource4)
			{
				_propertyBinding.BindNumber(_projectileEveryNum, resource4, "everyNum", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
				_propertyBinding.BindText(_projectileName, resource4, "projecileName", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
				_propertyBinding.BindNumber(_projectileDamage, resource4, "projectilebaseDamage", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
			}
			if (toolEvent is ShovelEventPacketSpawnConfig resource5)
			{
				_propertyBinding.BindText(_packetName, resource5, "packetName", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
				_propertyBinding.BindNumber(_packetEveryNum, resource5, "everyNum", RefreshDirectEditPreview, this, "RefreshToolEventFromHistory");
			}
		}
	}

	private void PopulateEventFields()
	{
		if (GodotObject.IsInstanceValid(_editingEvent) && GodotObject.IsInstanceValid(_baseFields))
		{
			_resourceName.Text = _editingEvent.ResourceName ?? "";
			_localToScene.ButtonPressed = _editingEvent.Get("resource_local_to_scene").AsBool();
			Resource editingEvent = _editingEvent;
			bool flag = ((editingEvent is MowerEventCreateSunConfig || editingEvent is ShovelEventRecycleConfig || editingEvent is ShovelEventJalapenoConfig || editingEvent is ShovelEventCreateProjectileConfig || editingEvent is ShovelEventPacketSpawnConfig) ? true : false);
			bool flag2 = flag;
			_baseFields.Visible = !flag2;
			_createSunFields.Visible = _editingEvent is MowerEventCreateSunConfig;
			_recycleFields.Visible = _editingEvent is ShovelEventRecycleConfig;
			_jalapenoFields.Visible = _editingEvent is ShovelEventJalapenoConfig;
			_projectileFields.Visible = _editingEvent is ShovelEventCreateProjectileConfig;
			_packetFields.Visible = _editingEvent is ShovelEventPacketSpawnConfig;
			if (_editingEvent is MowerEventCreateSunConfig mowerEventCreateSunConfig)
			{
				_sunNum.Value = mowerEventCreateSunConfig.num;
			}
			if (_editingEvent is ShovelEventRecycleConfig shovelEventRecycleConfig)
			{
				_recyclePercentage.Value = shovelEventRecycleConfig.percentage;
				_recycleDestroy.ButtonPressed = shovelEventRecycleConfig.destroy;
			}
			if (_editingEvent is ShovelEventJalapenoConfig shovelEventJalapenoConfig)
			{
				_jalapenoDestroy.ButtonPressed = shovelEventJalapenoConfig.destroy;
			}
			if (_editingEvent is ShovelEventCreateProjectileConfig shovelEventCreateProjectileConfig)
			{
				_projectileEveryNum.Value = Mathf.Max(1, shovelEventCreateProjectileConfig.everyNum);
				_projectileName.Text = shovelEventCreateProjectileConfig.projecileName ?? "";
				_projectileDamage.Value = shovelEventCreateProjectileConfig.projectilebaseDamage;
			}
			if (_editingEvent is ShovelEventPacketSpawnConfig shovelEventPacketSpawnConfig)
			{
				_packetName.Text = shovelEventPacketSpawnConfig.packetName ?? "";
				_packetEveryNum.Value = Mathf.Max(1, shovelEventPacketSpawnConfig.everyNum);
			}
		}
	}

	private void OnToolEventPropertyEdited(bool committed)
	{
		if ((base.CurrentResource is MowerEventConfig || base.CurrentResource is ShovelEventConfig) && base.CurrentResource == _editingEvent)
		{
			Resource currentResource = base.CurrentResource;
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			currentResource.EmitChanged();
		}
	}

	private void RefreshDirectEditPreview()
	{
		UpdateSimulationPreview();
	}

	public void RefreshToolEventFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && (base.CurrentResource is MowerEventConfig || base.CurrentResource is ShovelEventConfig) && base.CurrentResource == _editingEvent)
		{
			_propertyBinding?.Dispose();
			_propertyBinding = null;
			PopulateEventFields();
			BindDirectEditControls(_editingEvent);
			UpdateSimulationPreview();
		}
	}

	private void DisposeToolEventBinding()
	{
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_editingEvent = null;
	}

	private void UpdateSimulationPreview()
	{
		if (GodotObject.IsInstanceValid(_editingEvent) && GodotObject.IsInstanceValid(_outcome))
		{
			ResetEffectVisuals();
			int cost = Mathf.Max(0, Mathf.RoundToInt(_targetCost.Value));
			int num = Mathf.Clamp(Mathf.RoundToInt(_targetHealth.Value), 0, 100);
			bool buttonPressed = _izmMode.ButtonPressed;
			_targetRoot.Visible = true;
			_targetRoot.Modulate = Colors.White;
			_targetRoot.Scale = Vector2.One;
			_targetHealthBar.Value = num;
			_healthValue.Text = $"{num}%";
			bool flag = _editingEvent.Get("resource_local_to_scene").AsBool();
			string text = (string.IsNullOrWhiteSpace(_editingEvent.ResourceName) ? _editingEvent.GetType().Name : (_editingEvent.ResourceName + " · " + _editingEvent.GetType().Name));
			_eventType.Text = text + " · " + (flag ? "场景本地" : "可复用");
			bool flag2 = _editingEvent is MowerEventConfig;
			_shovelTool.Visible = !flag2;
			_mowerTool.Visible = flag2;
			_formHint.Text = (flag2 ? "在真实草坪与角色旁预演小推车触发结果；模拟不调用 Execute。" : "在真实草坪与角色旁预演铲除结果；目标费用、生命和模式仅用于推演。") + (flag ? " 当前资源仅随所属场景保存。" : " 当前资源可被多个场景复用。");
			_outcome.Text = BuildOutcome(cost, num, buttonPressed);
		}
	}

	private string BuildOutcome(int cost, int health, bool izm)
	{
		Resource editingEvent = _editingEvent;
		if (!(editingEvent is MowerEventCreateSunConfig mowerEventCreateSunConfig))
		{
			if (!(editingEvent is MowerEventConfig))
			{
				if (!(editingEvent is ShovelEventDestroyConfig))
				{
					if (!(editingEvent is ShovelEventRecycleConfig shovelEventRecycleConfig))
					{
						if (!(editingEvent is ShovelEventJalapenoConfig shovelEventJalapenoConfig))
						{
							if (!(editingEvent is ShovelEventCreateProjectileConfig shovelEventCreateProjectileConfig))
							{
								if (!(editingEvent is ShovelEventPacketSpawnConfig shovelEventPacketSpawnConfig))
								{
									if (!(editingEvent is ShovelEventPumpkinShovelConfig))
									{
										if (!(editingEvent is ShovelEventSkeletonShovelConfig))
										{
											if (!(editingEvent is ShovelEventCaptainShovelConfig))
											{
												if (editingEvent is ShovelEventConfig)
												{
													return "该铲子事件没有额外参数；点击演示查看固定工具动作。";
												}
												return "当前工具事件没有可推演的结果。";
											}
											return BuildCaptainOutcome(cost);
										}
										return (cost >= 50) ? "费用达到 50：复活最近死亡的植物并转为魅惑单位。" : "费用不足 50：不会触发复活。";
									}
									return $"把目标当前生命（模拟 {health}%）转移给南瓜保护层；若目标本身是南瓜则按剩余生命回收。";
								}
								if (shovelEventPacketSpawnConfig.packetName == "ItemMagnetWave")
								{
									return (cost >= Math.Max(1, shovelEventPacketSpawnConfig.everyNum)) ? $"生成 1 个 ItemMagnetWave，并将 drawNum 设为 {GetSpawnCount(cost, shovelEventPacketSpawnConfig.everyNum)}。" : $"费用不足 {Math.Max(1, shovelEventPacketSpawnConfig.everyNum)}，不会生成 ItemMagnetWave。";
								}
								ShovelEventPacketSpawnConfig shovelEventPacketSpawnConfig2 = shovelEventPacketSpawnConfig;
								return $"在当前格生成 {GetSpawnCount(cost, shovelEventPacketSpawnConfig2.everyNum)} 张 {EmptyName(shovelEventPacketSpawnConfig2.packetName, "未命名卡牌")}。";
							}
							return $"生成 {GetSpawnCount(cost, shovelEventCreateProjectileConfig.everyNum)} 枚 {EmptyName(shovelEventCreateProjectileConfig.projecileName, "未命名子弹")}，每枚基础伤害 {shovelEventCreateProjectileConfig.projectilebaseDamage:0.##}。";
						}
						return $"以目标费用 {cost} 作为伤害生成整行火焰{(shovelEventJalapenoConfig.destroy ? "，随后移除目标。" : "，目标保留。")}";
					}
					return $"返还约 {Mathf.RoundToInt((double)cost * shovelEventRecycleConfig.percentage)} 点费用（{cost} × {shovelEventRecycleConfig.percentage:0.##}）{(shovelEventRecycleConfig.destroy ? "，随后移除目标。" : "，目标继续留在场上。")}";
				}
				return "铲下后立即移除目标，不返还费用。";
			}
			return "该小推车事件没有额外参数；触发时保持基础事件行为。";
		}
		return $"小推车触发后，在目标位置生成 {mowerEventCreateSunConfig.num} 点{(izm ? "脑子" : "普通")}阳光。";
	}

	private static string BuildCaptainOutcome(int cost)
	{
		if (cost < 500)
		{
			int value = Mathf.FloorToInt((double)cost / 100.0);
			return $"生成 {value} 枚银币子弹，并在相邻行空降 {value} 名船员僵尸。";
		}
		return $"生成 {Mathf.FloorToInt((double)cost / 300.0)} 枚金币子弹，并在相邻行空降 {Mathf.FloorToInt((double)cost / 500.0)} 名船长僵尸。";
	}

	private static int GetSpawnCount(int cost, int everyNum)
	{
		return Mathf.FloorToInt((double)cost / (double)Math.Max(1, everyNum));
	}

	private static string EmptyName(string value, string fallback)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return fallback;
	}

	private void StartToolEffectPreview()
	{
		if (GodotObject.IsInstanceValid(_toolRoot))
		{
			UpdateSimulationPreview();
			_previewPhase = PreviewPhase.ToolDown;
			_previewElapsed = 0.0;
			_effectApplied = false;
			_toolRoot.Position = new Vector2(350f, 90f);
			_toolRoot.Rotation = 0f;
			_toolRoot.Scale = Vector2.One;
			_toolRoot.Modulate = Colors.White;
			SetProcess(IsVisibleInTree());
		}
	}

	private void UpdateToolDownPhase()
	{
		float num = Mathf.Clamp((float)(_previewElapsed / 0.36), 0f, 1f);
		float weight = num * num * (3f - 2f * num);
		_toolRoot.Position = new Vector2(350f, 90f).Lerp(new Vector2(480f, 275f), weight);
		_toolRoot.Rotation = ((_editingEvent is ShovelEventConfig) ? Mathf.Lerp(0f, -0.48f, weight) : 0f);
		_toolRoot.Scale = Vector2.One * Mathf.Lerp(1f, 1.12f, Mathf.Sin(num * (float)Math.PI));
		if (!(num < 1f))
		{
			_previewPhase = PreviewPhase.Effect;
			_previewElapsed = 0.0;
		}
	}

	private void UpdateEffectPhase()
	{
		if (!_effectApplied)
		{
			_effectApplied = true;
			ApplyEffectVisuals();
		}
		float num = Mathf.Clamp((float)(_previewElapsed / 0.78), 0f, 1f);
		float num2 = 0.88f + Mathf.Sin(num * (float)Math.PI) * 0.28f;
		if (_sunEffect.Visible)
		{
			_sunEffect.Position = new Vector2(588f, 188f).Lerp(new Vector2(630f, 95f), num);
			_sunEffect.Scale = Vector2.One * num2;
		}
		if (_projectileEffect.Visible)
		{
			_projectileEffect.Position = new Vector2(560f, 292f).Lerp(new Vector2(835f, 292f), num);
		}
		if (_packetEffect.Visible)
		{
			_packetEffect.Scale = Vector2.One * num2;
		}
		if (_fireEffect.Visible)
		{
			_fireEffect.Modulate = new Color(1f, 1f, 1f, 0.45f + Mathf.Sin(num * (float)Math.PI * 6f) * 0.35f);
		}
		if (_pumpkinEffect.Visible)
		{
			_pumpkinEffect.Scale = Vector2.One * num2;
		}
		if (ShouldFadeTarget())
		{
			_targetRoot.Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.22f, num));
		}
		if (!(num < 1f))
		{
			_previewPhase = PreviewPhase.Recover;
			_previewElapsed = 0.0;
		}
	}

	private void UpdateRecoverPhase()
	{
		float num = Mathf.Clamp((float)(_previewElapsed / 0.36), 0f, 1f);
		float weight = num * num * (3f - 2f * num);
		_toolRoot.Position = new Vector2(480f, 275f).Lerp(new Vector2(350f, 90f), weight);
		_toolRoot.Rotation = Mathf.Lerp((_editingEvent is ShovelEventConfig) ? (-0.48f) : 0f, 0f, weight);
		_toolRoot.Scale = Vector2.One;
		if (!(num < 1f))
		{
			_previewPhase = PreviewPhase.Idle;
			SetProcess(enable: false);
		}
	}

	private void ApplyEffectVisuals()
	{
		int num = Mathf.Max(0, Mathf.RoundToInt(_targetCost.Value));
		Resource editingEvent = _editingEvent;
		if (!(editingEvent is MowerEventCreateSunConfig))
		{
			if (!(editingEvent is ShovelEventJalapenoConfig))
			{
				if (!(editingEvent is ShovelEventCreateProjectileConfig) && !(editingEvent is ShovelEventCaptainShovelConfig))
				{
					if (!(editingEvent is ShovelEventPacketSpawnConfig))
					{
						if (!(editingEvent is ShovelEventPumpkinShovelConfig))
						{
							if (!(editingEvent is ShovelEventRecycleConfig))
							{
								if (!(editingEvent is ShovelEventSkeletonShovelConfig))
								{
									if (editingEvent is ShovelEventDestroyConfig)
									{
										_effectLabel.Text = "目标被铲除";
										_effectLabel.Visible = true;
									}
									else
									{
										_effectLabel.Text = "工具事件触发";
										_effectLabel.Visible = true;
									}
								}
								else
								{
									_effectLabel.Text = ((num >= 50) ? "复活最近植物" : "费用不足");
									_effectLabel.Visible = true;
								}
							}
							else
							{
								_effectLabel.Text = "费用返还";
								_effectLabel.Visible = true;
							}
						}
						else
						{
							_pumpkinEffect.Visible = true;
						}
					}
					else
					{
						_packetEffect.Visible = true;
					}
				}
				else
				{
					_projectileEffect.Visible = true;
				}
			}
			else
			{
				_fireEffect.Visible = true;
			}
		}
		else
		{
			_sunEffect.Visible = true;
		}
	}

	private bool ShouldFadeTarget()
	{
		if (!(_editingEvent is ShovelEventDestroyConfig) && !(_editingEvent is ShovelEventPumpkinShovelConfig) && !(_editingEvent is ShovelEventRecycleConfig { destroy: not false }))
		{
			if (_editingEvent is ShovelEventJalapenoConfig shovelEventJalapenoConfig)
			{
				return shovelEventJalapenoConfig.destroy;
			}
			return false;
		}
		return true;
	}

	private void ResetEffectVisuals()
	{
		_previewPhase = PreviewPhase.Idle;
		_previewElapsed = 0.0;
		_effectApplied = false;
		SetProcess(enable: false);
		if (GodotObject.IsInstanceValid(_toolRoot))
		{
			_toolRoot.Position = new Vector2(350f, 90f);
			_toolRoot.Rotation = 0f;
			_toolRoot.Scale = Vector2.One;
			_toolRoot.Modulate = Colors.White;
		}
		if (GodotObject.IsInstanceValid(_sunEffect))
		{
			_sunEffect.Visible = false;
			_sunEffect.Position = new Vector2(588f, 188f);
			_sunEffect.Scale = Vector2.One;
			_sunEffect.Modulate = Colors.White;
		}
		if (GodotObject.IsInstanceValid(_projectileEffect))
		{
			_projectileEffect.Visible = false;
			_projectileEffect.Position = new Vector2(560f, 292f);
			_projectileEffect.Scale = Vector2.One;
			_projectileEffect.Modulate = Colors.White;
		}
		if (GodotObject.IsInstanceValid(_packetEffect))
		{
			_packetEffect.Visible = false;
			_packetEffect.Scale = Vector2.One;
			_packetEffect.Modulate = Colors.White;
		}
		if (GodotObject.IsInstanceValid(_fireEffect))
		{
			_fireEffect.Visible = false;
			_fireEffect.Modulate = Colors.White;
		}
		if (GodotObject.IsInstanceValid(_pumpkinEffect))
		{
			_pumpkinEffect.Visible = false;
			_pumpkinEffect.Scale = Vector2.One;
			_pumpkinEffect.Modulate = Colors.White;
		}
		if (GodotObject.IsInstanceValid(_effectLabel))
		{
			_effectLabel.Visible = false;
			_effectLabel.Modulate = Colors.White;
		}
	}

	private void UpdatePreviewProcessingVisibility()
	{
		bool flag = IsVisibleInTree();
		if (GodotObject.IsInstanceValid(_targetPlant))
		{
			_targetPlant.ProcessMode = (ProcessModeEnum)(flag ? 3 : 4);
		}
		if (!flag)
		{
			SetProcess(enable: false);
		}
		else if (_previewPhase != PreviewPhase.Idle)
		{
			SetProcess(enable: true);
		}
	}

	private void AddSummaryRows()
	{
		AddItemIfMissing(base.PreviewList, "工具事件 -> " + _editingEvent.GetType().Name);
		AddItemIfMissing(base.TimelineList, "点击“演示工具效果” -> 工具落下 -> 游戏效果 -> 回位");
		AddItemIfMissing(base.GraphList, "工具配置 -> 模拟目标 -> 只读结果推演（不调用 Execute）");
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
