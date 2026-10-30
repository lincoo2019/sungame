using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWTutorialInlineEditor : VBoxContainer
{
	[Signal]
	public delegate void TutorialEditedEventHandler();

	private enum TutorialEditScope
	{
		Tutorial,
		Step,
		Condition
	}

	private sealed class TutorialRootSnapshot
	{
		public Json Data;

		public string SaveKey = "";

		public Array<TutorialStepConfig> Steps = new Array<TutorialStepConfig>();

		public int SelectedIndex;
	}

	private static readonly string[] ComparisonMethods = new string[5] { ">", ">=", "==", "<", "<=" };

	private const int MaxTutorialSteps = 256;

	private const int MaxVisibleConditionRows = 128;

	private const string ConditionRowScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTutorialConditionRow.tscn";

	private const string CharacterIconPath = "res://addons/ModEditor/Icons/ResourceCharacter.svg";

	private const string SunIconPath = "res://Asset/Texture/GUI/Shop/Item/SunCollect.png";

	private const string AddIconPath = "res://addons/ModEditor/Icons/Add.svg";

	private const string DuplicateIconPath = "res://addons/ModEditor/Icons/Duplicate.svg";

	private const string RemoveIconPath = "res://addons/ModEditor/Icons/Remove.svg";

	private const string MoveUpIconPath = "res://addons/ModEditor/Icons/MoveUp.svg";

	private const string MoveDownIconPath = "res://addons/ModEditor/Icons/MoveDown.svg";

	private static PackedScene _conditionRowScene;

	private static Texture2D _characterIcon;

	private static Texture2D _sunIcon;

	private TutorialConfig _config;

	private Resource _editingRoot;

	private TutorialEditScope _editScope;

	private int _stepIndex;

	private XWUndoRedoManager _undoRedo;

	private XWVisualPropertyBinding _rootBinding;

	private XWVisualPropertyBinding _stepBinding;

	private BroadCastManager _runtimeBroadcast;

	private Control _runtimeBroadcastBar;

	private Label _runtimeBroadcastLabel;

	private TextEdit _broadcastTextEditor;

	private CheckButton _broadcastUse;

	private SpinBox _broadcastDuration;

	private OptionButton _stepOption;

	private XWVisualOptionGallery _stepVisualGallery;

	private LineEdit _saveKeyEdit;

	private VBoxContainer _jsonSourcePickerHost;

	private XWResourcePicker _jsonSourcePicker;

	private Button _previousButton;

	private Button _nextButton;

	private Button _addStepButton;

	private Button _duplicateStepButton;

	private Button _removeStepButton;

	private Button _moveStepUpButton;

	private Button _moveStepDownButton;

	private Label _stepCountBadge;

	private OptionButton _conditionType;

	private HFlowContainer _conditionTypeVisualHost;

	private XWVisualSegmentedOption _conditionTypeVisualChoices;

	private Button _addConditionButton;

	private VBoxContainer _customConditionPickerHost;

	private XWResourcePicker _customConditionPicker;

	private Button _addSelectedConditionButton;

	private VBoxContainer _conditionRows;

	private Label _statusLabel;

	private readonly List<XWVisualSegmentedOption> _comparisonVisualChoices = new List<XWVisualSegmentedOption>();

	private readonly List<XWVisualPropertyBinding> _conditionBindings = new List<XWVisualPropertyBinding>();

	private readonly System.Collections.Generic.Dictionary<long, TutorialRootSnapshot> _rootSnapshots = new System.Collections.Generic.Dictionary<long, TutorialRootSnapshot>();

	private long _nextRootSnapshotId = 1L;

	private bool _updatingControls;

	public int StepCount => (_config?.step?.Count).GetValueOrDefault();

	public int CurrentStepIndex => _stepIndex;

	public int ConditionCount => (GetCurrentStep()?.conditionList?.Count).GetValueOrDefault();

	public bool CanManageStepCollection => _editScope == TutorialEditScope.Tutorial;

	public bool IsStandaloneCondition => _editScope == TutorialEditScope.Condition;

	public XWResourcePicker JsonSourcePicker => _jsonSourcePicker;

	public XWResourcePicker CustomConditionPicker => _customConditionPicker;

	public override void _Ready()
	{
		_undoRedo = XWEditorInterface.Instance?.GetUndoRedoManager();
		_runtimeBroadcast = GetNode<BroadCastManager>("%RuntimeBroadcast");
		_runtimeBroadcastBar = _runtimeBroadcast.GetNode<Control>("%Broad");
		_runtimeBroadcastLabel = _runtimeBroadcast.GetNode<Label>("%BroadCastLabel");
		_runtimeBroadcastLabel.Visible = false;
		_broadcastTextEditor = GetNode<TextEdit>("%BroadcastTextEditor");
		_broadcastUse = GetNode<CheckButton>("%BroadcastUse");
		_broadcastDuration = GetNode<SpinBox>("%BroadcastDuration");
		_stepOption = GetNode<OptionButton>("%StepOption");
		_saveKeyEdit = GetNode<LineEdit>("%SaveKeyEdit");
		_jsonSourcePickerHost = GetNode<VBoxContainer>("%JsonSourcePickerHost");
		_previousButton = GetNode<Button>("%PreviousButton");
		_nextButton = GetNode<Button>("%NextButton");
		_addStepButton = GetNode<Button>("%AddStepButton");
		_duplicateStepButton = GetNode<Button>("%DuplicateStepButton");
		_removeStepButton = GetNode<Button>("%RemoveStepButton");
		_moveStepUpButton = GetNode<Button>("%MoveStepUpButton");
		_moveStepDownButton = GetNode<Button>("%MoveStepDownButton");
		_stepCountBadge = GetNode<Label>("%StepCountBadge");
		_conditionType = GetNode<OptionButton>("%ConditionType");
		_conditionTypeVisualHost = GetNode<HFlowContainer>("%ConditionTypeVisualChoices");
		_addConditionButton = GetNode<Button>("%AddConditionButton");
		_customConditionPickerHost = GetNode<VBoxContainer>("%CustomConditionPickerHost");
		_addSelectedConditionButton = GetNode<Button>("%AddSelectedConditionButton");
		_conditionRows = GetNode<VBoxContainer>("%ConditionRows");
		_statusLabel = GetNode<Label>("%Status");
		_jsonSourcePicker = XWResourcePicker.Create();
		_jsonSourcePicker.Name = "TutorialJsonSourcePicker";
		_jsonSourcePicker.Setup("Json", allowClear: true, allowNew: false);
		_jsonSourcePickerHost.AddChild(_jsonSourcePicker, forceReadableName: false, InternalMode.Disabled);
		_jsonSourcePicker.ResourceChanged += ApplyJsonSource;
		_customConditionPicker = XWResourcePicker.Create();
		_customConditionPicker.Name = "TutorialCustomConditionPicker";
		_customConditionPicker.Setup("TutorialConditionConfig");
		_customConditionPickerHost.AddChild(_customConditionPicker, forceReadableName: false, InternalMode.Disabled);
		_customConditionPicker.ResourceChanged += delegate
		{
			UpdateActionAvailability();
		};
		SetButtonIcon(_addStepButton, "res://addons/ModEditor/Icons/Add.svg", "新增教程步骤");
		SetButtonIcon(_duplicateStepButton, "res://addons/ModEditor/Icons/Duplicate.svg", "复制当前步骤");
		SetButtonIcon(_removeStepButton, "res://addons/ModEditor/Icons/Remove.svg", "移除当前步骤");
		SetButtonIcon(_moveStepUpButton, "res://addons/ModEditor/Icons/MoveUp.svg", "上移当前步骤");
		SetButtonIcon(_moveStepDownButton, "res://addons/ModEditor/Icons/MoveDown.svg", "下移当前步骤");
		SetButtonIcon(_addConditionButton, "res://addons/ModEditor/Icons/Add.svg", "添加所选内置条件");
		SetButtonIcon(_addSelectedConditionButton, "res://addons/ModEditor/Icons/Duplicate.svg", "把所选条件资源作为独立副本加入当前步骤");
		_conditionType.AddItem("角色数量");
		_conditionType.AddItem("收集阳光");
		_stepVisualGallery = new XWVisualOptionGallery(_stepOption, GetNode<HFlowContainer>("%StepVisualChoices"), "TutorialStepVisualCatalog", (int _) => ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/FlowPort.svg", null, ResourceLoader.CacheMode.Reuse));
		_conditionTypeVisualChoices = new XWVisualSegmentedOption(_conditionType, _conditionTypeVisualHost, (int index) => ResourceLoader.Load<Texture2D>((index == 0) ? "res://addons/ModEditor/Icons/ResourceCharacter.svg" : "res://Asset/Texture/GUI/Shop/Item/SunCollect.png", null, ResourceLoader.CacheMode.Reuse));
		_conditionTypeVisualChoices.Rebuild();
		_previousButton.Pressed += ShowPrevious;
		_nextButton.Pressed += ShowNext;
		_addStepButton.Pressed += delegate
		{
			AddStep();
		};
		_duplicateStepButton.Pressed += delegate
		{
			DuplicateStep();
		};
		_removeStepButton.Pressed += delegate
		{
			RemoveStep();
		};
		_moveStepUpButton.Pressed += delegate
		{
			MoveStep(-1);
		};
		_moveStepDownButton.Pressed += delegate
		{
			MoveStep(1);
		};
		_stepOption.ItemSelected += SelectStep;
		_broadcastUse.Toggled += SetBroadcastUse;
		_addConditionButton.Pressed += AddCondition;
		_addSelectedConditionButton.Pressed += delegate
		{
			AddSelectedCondition();
		};
		RefreshFromConfig();
	}

	public override void _ExitTree()
	{
		_stepVisualGallery?.Dispose();
		_stepVisualGallery = null;
		_conditionTypeVisualChoices?.Dispose();
		_conditionTypeVisualChoices = null;
		_rootBinding?.Dispose();
		_stepBinding?.Dispose();
		_rootBinding = null;
		_stepBinding = null;
		DisposeConditionBindings();
		_rootSnapshots.Clear();
		DisposeComparisonVisualChoices();
		base._ExitTree();
	}

	public void EditConfig(TutorialConfig config, Resource editingRoot)
	{
		_config = config;
		_editingRoot = editingRoot;
		TutorialEditScope editScope = ((editingRoot is TutorialStepConfig) ? TutorialEditScope.Step : ((editingRoot is TutorialConditionConfig) ? TutorialEditScope.Condition : TutorialEditScope.Tutorial));
		_editScope = editScope;
		_stepIndex = 0;
		if (IsNodeReady())
		{
			RefreshFromConfig();
		}
	}

	public void SelectStep(long index)
	{
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		if (valueOrDefault != 0)
		{
			_stepIndex = Mathf.Clamp((int)index, 0, valueOrDefault - 1);
			ShowCurrentStep();
		}
	}

	public void ShowPrevious()
	{
		if (_stepIndex > 0)
		{
			SelectStep(_stepIndex - 1);
		}
	}

	public void ShowNext()
	{
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		if (_stepIndex + 1 < valueOrDefault)
		{
			SelectStep(_stepIndex + 1);
		}
	}

	public bool AddStep()
	{
		if (_editScope != TutorialEditScope.Tutorial || !GodotObject.IsInstanceValid(_config))
		{
			return false;
		}
		Array<TutorialStepConfig> array = new Array<TutorialStepConfig>(_config.step ?? new Array<TutorialStepConfig>());
		TutorialStepConfig item = new TutorialStepConfig
		{
			ResourceLocalToScene = true,
			ResourceName = $"TutorialStep_{array.Count + 1}",
			broadCastUse = true,
			broadCastConfig = CreateDefaultBroadcast()
		};
		array.Add(item);
		int doIndex = array.Count - 1;
		ReplaceStepList(array, "新增教程步骤", doIndex, _stepIndex);
		return StepCount == array.Count;
	}

	public bool DuplicateStep()
	{
		TutorialStepConfig currentStep = GetCurrentStep();
		if (_editScope != TutorialEditScope.Tutorial || !GodotObject.IsInstanceValid(currentStep))
		{
			return false;
		}
		TutorialStepConfig tutorialStepConfig = currentStep.Duplicate(deep: true) as TutorialStepConfig;
		if (!GodotObject.IsInstanceValid(tutorialStepConfig))
		{
			return false;
		}
		tutorialStepConfig.ResourceLocalToScene = true;
		tutorialStepConfig.ResourceName = (string.IsNullOrWhiteSpace(currentStep.ResourceName) ? $"TutorialStep_{_stepIndex + 2}" : (currentStep.ResourceName + "_Copy"));
		Array<TutorialStepConfig> array = new Array<TutorialStepConfig>(_config.step);
		int stepIndex = _stepIndex;
		int num = Math.Min(_stepIndex + 1, array.Count);
		array.Insert(num, tutorialStepConfig);
		ReplaceStepList(array, "复制教程步骤", num, stepIndex);
		return StepCount == array.Count;
	}

	public bool RemoveStep()
	{
		if (_editScope != TutorialEditScope.Tutorial || !GodotObject.IsInstanceValid(GetCurrentStep()))
		{
			return false;
		}
		Array<TutorialStepConfig> array = new Array<TutorialStepConfig>(_config.step);
		int stepIndex = _stepIndex;
		array.RemoveAt(_stepIndex);
		int doIndex = ((array.Count != 0) ? Math.Min(_stepIndex, array.Count - 1) : 0);
		ReplaceStepList(array, "移除教程步骤", doIndex, stepIndex);
		return StepCount == array.Count;
	}

	public bool MoveStepUp()
	{
		return MoveStep(-1);
	}

	public bool MoveStepDown()
	{
		return MoveStep(1);
	}

	private bool MoveStep(int direction)
	{
		if (_editScope != TutorialEditScope.Tutorial || !GodotObject.IsInstanceValid(GetCurrentStep()))
		{
			return false;
		}
		int num = _stepIndex + Math.Sign(direction);
		if (num < 0 || num >= StepCount)
		{
			return false;
		}
		Array<TutorialStepConfig> array = new Array<TutorialStepConfig>(_config.step);
		TutorialStepConfig value = array[_stepIndex];
		array[_stepIndex] = array[num];
		array[num] = value;
		int stepIndex = _stepIndex;
		ReplaceStepList(array, (direction < 0) ? "上移教程步骤" : "下移教程步骤", num, stepIndex);
		return _stepIndex == num;
	}

	public void SetBroadcastUse(bool enabled)
	{
		if (_updatingControls || _editScope == TutorialEditScope.Condition)
		{
			return;
		}
		TutorialStepConfig currentStep = GetCurrentStep();
		if (GodotObject.IsInstanceValid(currentStep) && currentStep.broadCastUse != enabled)
		{
			BroadCastConfig broadCastConfig = currentStep.broadCastConfig;
			BroadCastConfig broadCastConfig2 = ((enabled && !GodotObject.IsInstanceValid(broadCastConfig)) ? CreateDefaultBroadcast() : broadCastConfig);
			if (_undoRedo == null)
			{
				currentStep.broadCastUse = enabled;
				currentStep.broadCastConfig = broadCastConfig2;
				ShowCurrentStep();
				EmitTutorialEdited(currentStep, broadCastConfig2);
				return;
			}
			_undoRedo.CreateAction(enabled ? "启用教程广播" : "关闭教程广播");
			_undoRedo.AddDoProperty(currentStep, "broadCastUse", enabled);
			_undoRedo.AddDoProperty(currentStep, "broadCastConfig", broadCastConfig2);
			_undoRedo.AddUndoProperty(currentStep, "broadCastUse", currentStep.broadCastUse);
			_undoRedo.AddUndoProperty(currentStep, "broadCastConfig", broadCastConfig);
			_undoRedo.AddDoMethod(this, "RefreshTutorialPropertiesFromHistory");
			_undoRedo.AddUndoMethod(this, "RefreshTutorialPropertiesFromHistory");
			_undoRedo.CommitAction();
			ShowCurrentStep();
			EmitTutorialEdited(currentStep, broadCastConfig2);
		}
	}

	public void SetBroadcastText(string value)
	{
		if (_updatingControls || _editScope == TutorialEditScope.Condition)
		{
			return;
		}
		TutorialStepConfig currentStep = GetCurrentStep();
		if (GodotObject.IsInstanceValid(currentStep))
		{
			TutorialStepConfig tutorialStepConfig = currentStep;
			if (tutorialStepConfig.broadCastConfig == null)
			{
				tutorialStepConfig.broadCastConfig = CreateDefaultBroadcast();
			}
			SetResourceProperty(currentStep.broadCastConfig, "broadCastString", value ?? "", "修改教程广播文本", currentStep);
		}
	}

	public void SetBroadcastDuration(double value)
	{
		if (_updatingControls || _editScope == TutorialEditScope.Condition)
		{
			return;
		}
		TutorialStepConfig currentStep = GetCurrentStep();
		if (GodotObject.IsInstanceValid(currentStep))
		{
			TutorialStepConfig tutorialStepConfig = currentStep;
			if (tutorialStepConfig.broadCastConfig == null)
			{
				tutorialStepConfig.broadCastConfig = CreateDefaultBroadcast();
			}
			SetResourceProperty(currentStep.broadCastConfig, "broadCastTime", value, "修改教程广播时长", currentStep);
		}
	}

	public void SetCharacterName(TutorialConditionCheckCharaterNum condition, string value)
	{
		if (!_updatingControls && CanEditCondition(condition))
		{
			SetResourceProperty(condition, "characterName", value ?? "", "修改教程目标角色", GetCurrentStep());
		}
	}

	public void SetComparisonMethod(TutorialConditionCheckCharaterNum condition, string value)
	{
		if (!_updatingControls && CanEditCondition(condition) && System.Array.IndexOf(ComparisonMethods, value) >= 0)
		{
			SetResourceProperty(condition, "method", value, "修改教程比较方式", GetCurrentStep());
		}
	}

	public void SetConditionTarget(TutorialConditionConfig condition, double value)
	{
		if (_updatingControls || !CanEditCondition(condition))
		{
			return;
		}
		int num = Math.Max(0, (int)Math.Round(value));
		if (!(condition is TutorialConditionCheckCharaterNum resource))
		{
			if (condition is TutorialConditionCheckSunCollect resource2)
			{
				SetResourceProperty(resource2, "num", num, "修改教程阳光目标", GetCurrentStep());
			}
		}
		else
		{
			SetResourceProperty(resource, "num", num, "修改教程角色数量", GetCurrentStep());
		}
	}

	public void AddCondition()
	{
		if (_editScope != TutorialEditScope.Condition)
		{
			TutorialStepConfig currentStep = GetCurrentStep();
			if (GodotObject.IsInstanceValid(currentStep) && (currentStep.conditionList?.Count ?? 0) < 128)
			{
				TutorialConditionConfig tutorialConditionConfig = ((_conditionType.Selected == 1) ? ((TutorialConditionConfig)new TutorialConditionCheckSunCollect()) : ((TutorialConditionConfig)new TutorialConditionCheckCharaterNum()));
				tutorialConditionConfig.ResourceLocalToScene = true;
				tutorialConditionConfig.ResourceName = $"Condition_{(currentStep.conditionList?.Count ?? 0) + 1}";
				AddConditionResource(tutorialConditionConfig, "添加内置教程条件");
			}
		}
	}

	public bool AddSelectedCondition()
	{
		if (_editScope == TutorialEditScope.Condition || !(_customConditionPicker?.EditedResource is TutorialConditionConfig tutorialConditionConfig))
		{
			return false;
		}
		TutorialConditionConfig tutorialConditionConfig2 = tutorialConditionConfig.Duplicate(deep: true) as TutorialConditionConfig;
		if (!GodotObject.IsInstanceValid(tutorialConditionConfig2))
		{
			return false;
		}
		tutorialConditionConfig2.ResourceLocalToScene = true;
		tutorialConditionConfig2.ResourceName = (string.IsNullOrWhiteSpace(tutorialConditionConfig.ResourceName) ? (tutorialConditionConfig.GetType().Name + "_Copy") : (tutorialConditionConfig.ResourceName + "_Copy"));
		return AddConditionResource(tutorialConditionConfig2, "添加自定义教程条件副本");
	}

	public bool DuplicateCondition(TutorialConditionConfig condition)
	{
		TutorialStepConfig currentStep = GetCurrentStep();
		if (_editScope == TutorialEditScope.Condition || !GodotObject.IsInstanceValid(currentStep) || !GodotObject.IsInstanceValid(condition) || currentStep.conditionList == null)
		{
			return false;
		}
		int num = currentStep.conditionList.IndexOf(condition);
		if (num < 0 || currentStep.conditionList.Count >= 128)
		{
			return false;
		}
		TutorialConditionConfig tutorialConditionConfig = condition.Duplicate(deep: true) as TutorialConditionConfig;
		if (!GodotObject.IsInstanceValid(tutorialConditionConfig))
		{
			return false;
		}
		tutorialConditionConfig.ResourceLocalToScene = true;
		tutorialConditionConfig.ResourceName = (string.IsNullOrWhiteSpace(condition.ResourceName) ? (condition.GetType().Name + "_Copy") : (condition.ResourceName + "_Copy"));
		Array<TutorialConditionConfig> array = new Array<TutorialConditionConfig>(currentStep.conditionList);
		array.Insert(num + 1, tutorialConditionConfig);
		ReplaceConditionList(currentStep, array, "复制教程条件");
		return currentStep.conditionList.Count == array.Count;
	}

	public bool MoveCondition(TutorialConditionConfig condition, int direction)
	{
		TutorialStepConfig currentStep = GetCurrentStep();
		if (_editScope == TutorialEditScope.Condition || !GodotObject.IsInstanceValid(currentStep) || !GodotObject.IsInstanceValid(condition) || currentStep.conditionList == null)
		{
			return false;
		}
		int num = currentStep.conditionList.IndexOf(condition);
		int num2 = num + Math.Sign(direction);
		if (num < 0 || num2 < 0 || num2 >= currentStep.conditionList.Count)
		{
			return false;
		}
		Array<TutorialConditionConfig> array = new Array<TutorialConditionConfig>(currentStep.conditionList);
		array[num] = array[num2];
		array[num2] = condition;
		ReplaceConditionList(currentStep, array, (direction < 0) ? "上移教程条件" : "下移教程条件");
		return true;
	}

	public void RemoveCondition(TutorialConditionConfig condition)
	{
		if (_editScope == TutorialEditScope.Condition || !GodotObject.IsInstanceValid(condition))
		{
			return;
		}
		TutorialStepConfig currentStep = GetCurrentStep();
		if (GodotObject.IsInstanceValid(currentStep) && currentStep.conditionList != null)
		{
			Array<TutorialConditionConfig> array = new Array<TutorialConditionConfig>(currentStep.conditionList);
			if (array.Remove(condition))
			{
				ReplaceConditionList(currentStep, array, "移除教程条件");
			}
		}
	}

	private void RefreshFromConfig()
	{
		if (!IsNodeReady())
		{
			return;
		}
		_rootBinding?.Dispose();
		_rootBinding = null;
		_updatingControls = true;
		_saveKeyEdit.Text = (GodotObject.IsInstanceValid(_config) ? (_config.saveKey ?? "") : "");
		_saveKeyEdit.Editable = _editScope == TutorialEditScope.Tutorial && GodotObject.IsInstanceValid(_config);
		_jsonSourcePickerHost.Visible = _editScope == TutorialEditScope.Tutorial;
		_jsonSourcePicker?.SetEditedResource((_editScope != TutorialEditScope.Tutorial) ? null : _config?.data);
		_stepOption.Clear();
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		for (int i = 0; i < valueOrDefault; i++)
		{
			_stepOption.AddItem(BuildStepLabel(i, _config.step[i]));
		}
		_stepVisualGallery?.Rebuild();
		_updatingControls = false;
		if (_editScope == TutorialEditScope.Tutorial && GodotObject.IsInstanceValid(_config))
		{
			_rootBinding = new XWVisualPropertyBinding(_undoRedo, delegate
			{
				EmitTutorialEdited(_config);
			});
			_rootBinding.BindText(_saveKeyEdit, _config, "saveKey", RefreshStatus, this, "RefreshTutorialPropertiesFromHistory");
		}
		_stepIndex = ((valueOrDefault != 0) ? Mathf.Clamp(_stepIndex, 0, valueOrDefault - 1) : 0);
		ShowCurrentStep();
	}

	private void ShowCurrentStep()
	{
		_stepBinding?.Dispose();
		_stepBinding = null;
		TutorialStepConfig step = GetCurrentStep();
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		bool flag = GodotObject.IsInstanceValid(step);
		_updatingControls = true;
		_stepOption.Disabled = valueOrDefault == 0;
		_previousButton.Disabled = !flag || _stepIndex <= 0;
		_nextButton.Disabled = !flag || _stepIndex >= valueOrDefault - 1;
		_stepCountBadge.Text = $"{valueOrDefault} 步";
		if (flag)
		{
			_stepOption.Select(_stepIndex);
		}
		_stepVisualGallery?.RefreshSelection();
		_stepVisualGallery?.SetDisabled(valueOrDefault == 0, "当前教程没有可选步骤");
		bool flag2 = flag && _editScope != TutorialEditScope.Condition;
		bool flag3 = flag && step.broadCastUse;
		_broadcastUse.ButtonPressed = flag3;
		_broadcastUse.Disabled = !flag2;
		_broadcastTextEditor.Text = ((flag && GodotObject.IsInstanceValid(step.broadCastConfig)) ? (step.broadCastConfig.broadCastString ?? "") : "");
		_broadcastTextEditor.Editable = flag2 && flag3;
		_broadcastDuration.Value = ((flag && GodotObject.IsInstanceValid(step.broadCastConfig)) ? step.broadCastConfig.broadCastTime : (-1.0));
		_broadcastDuration.Editable = flag2 && flag3;
		_runtimeBroadcastBar.Visible = flag;
		_runtimeBroadcastBar.Modulate = (flag3 ? Colors.White : new Color(1f, 1f, 1f, 0.45f));
		_broadcastTextEditor.Visible = flag;
		_conditionType.Disabled = !flag2;
		SetVisualButtonsDisabled(_conditionTypeVisualHost, !flag2);
		_addConditionButton.Disabled = !flag2 || (step?.conditionList?.Count).GetValueOrDefault() >= 128;
		_updatingControls = false;
		if (flag2 && GodotObject.IsInstanceValid(step.broadCastConfig))
		{
			_stepBinding = new XWVisualPropertyBinding(_undoRedo, delegate
			{
				EmitTutorialEdited(step, step.broadCastConfig);
			});
			_stepBinding.BindText(_broadcastTextEditor, step.broadCastConfig, "broadCastString", RefreshStepOptionLabel, this, "RefreshTutorialPropertiesFromHistory");
			_stepBinding.BindNumber(_broadcastDuration, step.broadCastConfig, "broadCastTime", null, this, "RefreshTutorialPropertiesFromHistory");
		}
		RefreshConditionRows();
		UpdateActionAvailability();
		RefreshStatus();
	}

	private void RefreshConditionRows()
	{
		if (!GodotObject.IsInstanceValid(_conditionRows))
		{
			return;
		}
		DisposeConditionBindings();
		DisposeComparisonVisualChoices();
		foreach (Node child in _conditionRows.GetChildren())
		{
			_conditionRows.RemoveChild(child);
			child.QueueFree();
		}
		TutorialStepConfig currentStep = GetCurrentStep();
		int valueOrDefault = (currentStep?.conditionList?.Count).GetValueOrDefault();
		if (valueOrDefault == 0)
		{
			_conditionRows.AddChild(new Label
			{
				Text = "无完成条件：该步骤可直接推进。",
				HorizontalAlignment = HorizontalAlignment.Center
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		if (_conditionRowScene == null)
		{
			_conditionRowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWTutorialConditionRow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		int num = Math.Min(valueOrDefault, 128);
		for (int i = 0; i < num; i++)
		{
			TutorialConditionConfig tutorialConditionConfig = currentStep.conditionList[i];
			if (GodotObject.IsInstanceValid(tutorialConditionConfig))
			{
				PanelContainer panelContainer = _conditionRowScene?.Instantiate<PanelContainer>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(panelContainer))
				{
					_conditionRows.AddChild(panelContainer, forceReadableName: false, InternalMode.Disabled);
					BindConditionRow(panelContainer, tutorialConditionConfig, i);
				}
			}
		}
	}

	private void BindConditionRow(PanelContainer row, TutorialConditionConfig condition, int index)
	{
		TextureRect node = row.GetNode<TextureRect>("%Icon");
		Label node2 = row.GetNode<Label>("%TypeTitle");
		Label node3 = row.GetNode<Label>("%CharacterLabel");
		LineEdit node4 = row.GetNode<LineEdit>("%CharacterName");
		OptionButton node5 = row.GetNode<OptionButton>("%Comparison");
		HFlowContainer node6 = row.GetNode<HFlowContainer>("%ComparisonVisualChoices");
		Label node7 = row.GetNode<Label>("%TargetLabel");
		SpinBox node8 = row.GetNode<SpinBox>("%Target");
		Label node9 = row.GetNode<Label>("%UnitLabel");
		HBoxContainer node10 = row.GetNode<HBoxContainer>("%Values");
		VBoxContainer node11 = row.GetNode<VBoxContainer>("%CustomPropertyHost");
		Button node12 = row.GetNode<Button>("%MoveUpButton");
		Button node13 = row.GetNode<Button>("%MoveDownButton");
		Button node14 = row.GetNode<Button>("%DuplicateButton");
		Button node15 = row.GetNode<Button>("%RemoveButton");
		bool flag = CanEditCondition(condition);
		node5.Clear();
		string[] comparisonMethods = ComparisonMethods;
		foreach (string label in comparisonMethods)
		{
			node5.AddItem(label);
		}
		XWVisualSegmentedOption xWVisualSegmentedOption = new XWVisualSegmentedOption(node5, node6);
		xWVisualSegmentedOption.Rebuild();
		_comparisonVisualChoices.Add(xWVisualSegmentedOption);
		bool flag2 = _editScope != TutorialEditScope.Condition;
		node12.Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/MoveUp.svg", null, ResourceLoader.CacheMode.Reuse);
		node13.Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/MoveDown.svg", null, ResourceLoader.CacheMode.Reuse);
		node14.Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Duplicate.svg", null, ResourceLoader.CacheMode.Reuse);
		node15.Icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse);
		node12.Disabled = !flag2 || index <= 0;
		node13.Disabled = !flag2 || index >= (GetCurrentStep()?.conditionList?.Count).GetValueOrDefault() - 1;
		node14.Disabled = !flag2 || (GetCurrentStep()?.conditionList?.Count).GetValueOrDefault() >= 128;
		node15.Disabled = !flag2;
		node15.TooltipText = (node15.Disabled ? "独立条件资源只能编辑自身，不能从预览包装步骤中移除" : "从当前步骤移除该条件");
		node12.Pressed += delegate
		{
			MoveCondition(condition, -1);
		};
		node13.Pressed += delegate
		{
			MoveCondition(condition, 1);
		};
		node14.Pressed += delegate
		{
			DuplicateCondition(condition);
		};
		node15.Pressed += delegate
		{
			RemoveCondition(condition);
		};
		TutorialConditionCheckCharaterNum tutorialConditionCheckCharaterNum = condition as TutorialConditionCheckCharaterNum;
		if (tutorialConditionCheckCharaterNum == null)
		{
			TutorialConditionCheckSunCollect tutorialConditionCheckSunCollect = condition as TutorialConditionCheckSunCollect;
			if (tutorialConditionCheckSunCollect != null)
			{
				if (_sunIcon == null)
				{
					_sunIcon = ResourceLoader.Load<Texture2D>("res://Asset/Texture/GUI/Shop/Item/SunCollect.png", null, ResourceLoader.CacheMode.Reuse);
				}
				node.Texture = _sunIcon;
				node2.Text = $"条件 {index + 1} · 收集阳光";
				node3.Visible = false;
				node4.Visible = false;
				node5.Visible = false;
				node6.Visible = false;
				node7.Text = "累计收集";
				node8.Value = tutorialConditionCheckSunCollect.num;
				node8.Editable = flag;
				node9.Text = "阳光";
				XWVisualPropertyBinding xWVisualPropertyBinding = new XWVisualPropertyBinding(_undoRedo, delegate
				{
					EmitTutorialEdited(GetCurrentStep(), tutorialConditionCheckSunCollect);
				});
				xWVisualPropertyBinding.BindNumber(node8, tutorialConditionCheckSunCollect, "num", null, this, "RefreshTutorialPropertiesFromHistory");
				_conditionBindings.Add(xWVisualPropertyBinding);
			}
			else
			{
				node2.Text = $"条件 {index + 1} · {condition.GetType().Name}";
				node10.Visible = false;
				node11.Visible = true;
				XWDirectPropertySurface xWDirectPropertySurface = new XWDirectPropertySurface
				{
					Name = $"CustomConditionProperties_{index + 1}",
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				xWDirectPropertySurface.PropertyEdited += delegate
				{
					EmitTutorialEdited(GetCurrentStep(), condition);
				};
				node11.AddChild(xWDirectPropertySurface, forceReadableName: false, InternalMode.Disabled);
				xWDirectPropertySurface.BindResource(condition, _undoRedo);
			}
		}
		else
		{
			if (_characterIcon == null)
			{
				_characterIcon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceCharacter.svg", null, ResourceLoader.CacheMode.Reuse);
			}
			node.Texture = _characterIcon;
			node2.Text = $"条件 {index + 1} · 场上角色数量";
			node4.Text = tutorialConditionCheckCharaterNum.characterName ?? "";
			node4.Editable = flag;
			int idx = Math.Max(0, System.Array.IndexOf(ComparisonMethods, tutorialConditionCheckCharaterNum.method));
			node5.Select(idx);
			xWVisualSegmentedOption.RefreshSelection();
			node5.Disabled = !flag;
			SetVisualButtonsDisabled(node6, !flag);
			node8.Value = tutorialConditionCheckCharaterNum.num;
			node8.Editable = flag;
			XWVisualPropertyBinding xWVisualPropertyBinding2 = new XWVisualPropertyBinding(_undoRedo, delegate
			{
				EmitTutorialEdited(GetCurrentStep(), tutorialConditionCheckCharaterNum);
			});
			xWVisualPropertyBinding2.BindText(node4, tutorialConditionCheckCharaterNum, "characterName", null, this, "RefreshTutorialPropertiesFromHistory");
			xWVisualPropertyBinding2.BindNumber(node8, tutorialConditionCheckCharaterNum, "num", null, this, "RefreshTutorialPropertiesFromHistory");
			_conditionBindings.Add(xWVisualPropertyBinding2);
			node5.ItemSelected += delegate(long selected)
			{
				SetComparisonMethod(tutorialConditionCheckCharaterNum, ComparisonMethods[Mathf.Clamp((int)selected, 0, ComparisonMethods.Length - 1)]);
			};
		}
	}

	private void ApplyJsonSource(Resource resource)
	{
		if (_updatingControls || _editScope != TutorialEditScope.Tutorial || !GodotObject.IsInstanceValid(_config))
		{
			return;
		}
		Json json = resource as Json;
		if (_config.data != json)
		{
			TutorialConfig tutorialConfig = new TutorialConfig
			{
				data = json
			};
			long num = StoreRootSnapshot(_config.data, _config.saveKey, _config.step, _stepIndex);
			long num2 = StoreRootSnapshot(json, tutorialConfig.saveKey, tutorialConfig.step, 0);
			if (_undoRedo == null)
			{
				ApplyTutorialRootSnapshot(num2);
				return;
			}
			_undoRedo.CreateAction((json == null) ? "清除教程 JSON 来源" : "更换教程 JSON 来源");
			_undoRedo.AddDoMethod(this, "ApplyTutorialRootSnapshot", num2);
			_undoRedo.AddUndoMethod(this, "ApplyTutorialRootSnapshot", num);
			_undoRedo.CommitAction();
		}
	}

	private long StoreRootSnapshot(Json data, string saveKey, Array<TutorialStepConfig> steps, int selectedIndex)
	{
		long num = _nextRootSnapshotId++;
		_rootSnapshots[num] = new TutorialRootSnapshot
		{
			Data = data,
			SaveKey = (saveKey ?? ""),
			Steps = new Array<TutorialStepConfig>(steps ?? new Array<TutorialStepConfig>()),
			SelectedIndex = selectedIndex
		};
		return num;
	}

	public void ApplyTutorialRootSnapshot(long snapshotId)
	{
		if (GodotObject.IsInstanceValid(_config) && _rootSnapshots.TryGetValue(snapshotId, out var value))
		{
			_config.Set("data", value.Data);
			_config.Set("saveKey", value.SaveKey);
			_config.Set("step", new Array<TutorialStepConfig>(value.Steps));
			_stepIndex = value.SelectedIndex;
			RefreshFromConfig();
			EmitTutorialEdited(_config);
		}
	}

	private bool AddConditionResource(TutorialConditionConfig condition, string actionName)
	{
		TutorialStepConfig currentStep = GetCurrentStep();
		if (_editScope == TutorialEditScope.Condition || !GodotObject.IsInstanceValid(currentStep) || !GodotObject.IsInstanceValid(condition) || (currentStep.conditionList?.Count ?? 0) >= 128)
		{
			return false;
		}
		Array<TutorialConditionConfig> array = new Array<TutorialConditionConfig>(currentStep.conditionList ?? new Array<TutorialConditionConfig>());
		array.Add(condition);
		ReplaceConditionList(currentStep, array, actionName);
		return currentStep.conditionList.Count == array.Count;
	}

	private void ReplaceConditionList(TutorialStepConfig step, Array<TutorialConditionConfig> next, string actionName)
	{
		if (GodotObject.IsInstanceValid(step) && next != null)
		{
			Array<TutorialConditionConfig> from = new Array<TutorialConditionConfig>(step.conditionList ?? new Array<TutorialConditionConfig>());
			if (_undoRedo == null)
			{
				step.conditionList = next;
				RefreshConditionRows();
				UpdateActionAvailability();
				RefreshStatus();
				EmitTutorialEdited(step);
			}
			else
			{
				_undoRedo.CreateAction(actionName);
				_undoRedo.AddDoProperty(step, "conditionList", Variant.From(in next));
				_undoRedo.AddUndoProperty(step, "conditionList", Variant.From(in from));
				_undoRedo.AddDoMethod(this, "RefreshConditionCollectionFromHistory");
				_undoRedo.AddUndoMethod(this, "RefreshConditionCollectionFromHistory");
				_undoRedo.CommitAction();
			}
		}
	}

	private void ReplaceStepList(Array<TutorialStepConfig> next, string actionName, int doIndex, int undoIndex)
	{
		if (_editScope == TutorialEditScope.Tutorial && GodotObject.IsInstanceValid(_config) && next != null)
		{
			Array<TutorialStepConfig> from = new Array<TutorialStepConfig>(_config.step ?? new Array<TutorialStepConfig>());
			if (_undoRedo == null)
			{
				_config.step = next;
				RefreshTutorialCollectionFromHistory(doIndex);
				return;
			}
			_undoRedo.CreateAction(actionName);
			_undoRedo.AddDoProperty(_config, "step", Variant.From(in next));
			_undoRedo.AddUndoProperty(_config, "step", Variant.From(in from));
			_undoRedo.AddDoMethod(this, "RefreshTutorialCollectionFromHistory", doIndex);
			_undoRedo.AddUndoMethod(this, "RefreshTutorialCollectionFromHistory", undoIndex);
			_undoRedo.CommitAction();
		}
	}

	public void RefreshTutorialCollectionFromHistory(long selectedIndex)
	{
		_stepIndex = (int)selectedIndex;
		RefreshFromConfig();
		EmitTutorialEdited(_config);
	}

	public void RefreshConditionCollectionFromHistory()
	{
		RefreshConditionRows();
		UpdateActionAvailability();
		RefreshStatus();
		EmitTutorialEdited(GetCurrentStep());
	}

	public void RefreshTutorialPropertiesFromHistory()
	{
		if (_undoRedo != null && (_undoRedo.IsUndoing() || _undoRedo.IsRedoing()))
		{
			RefreshFromConfig();
			EmitTutorialEdited(GetCurrentStep());
		}
	}

	private void SetResourceProperty(Resource resource, StringName property, Variant value, string actionName, params Resource[] relatedResources)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		Variant value2 = resource.Get(property);
		if (!value2.Equals(value))
		{
			if (_undoRedo == null)
			{
				resource.Set(property, value);
			}
			else
			{
				_undoRedo.CreateAction(actionName);
				_undoRedo.AddDoProperty(resource, property, value);
				_undoRedo.AddUndoProperty(resource, property, value2);
				_undoRedo.AddDoMethod(this, "RefreshTutorialPropertiesFromHistory");
				_undoRedo.AddUndoMethod(this, "RefreshTutorialPropertiesFromHistory");
				_undoRedo.CommitAction();
			}
			List<Resource> list = new List<Resource> { resource };
			if (relatedResources != null)
			{
				list.AddRange(relatedResources);
			}
			EmitTutorialEdited(list.ToArray());
			RefreshStepOptionLabel();
			RefreshStatus();
		}
	}

	private void UpdateActionAvailability()
	{
		if (IsNodeReady())
		{
			bool flag = _editScope == TutorialEditScope.Tutorial && GodotObject.IsInstanceValid(_config);
			bool flag2 = GodotObject.IsInstanceValid(GetCurrentStep());
			_addStepButton.Disabled = !flag || StepCount >= 256;
			_duplicateStepButton.Disabled = !flag || !flag2 || StepCount >= 256;
			_removeStepButton.Disabled = !flag || !flag2;
			_moveStepUpButton.Disabled = !flag || !flag2 || _stepIndex <= 0;
			_moveStepDownButton.Disabled = !flag || !flag2 || _stepIndex >= StepCount - 1;
			bool flag3 = flag2 && _editScope != TutorialEditScope.Condition;
			bool flag4 = ConditionCount < 128;
			_customConditionPickerHost.Visible = _editScope != TutorialEditScope.Condition;
			_addSelectedConditionButton.Visible = _editScope != TutorialEditScope.Condition;
			_addSelectedConditionButton.Disabled = !flag3 || !flag4 || !(_customConditionPicker?.EditedResource is TutorialConditionConfig);
		}
	}

	private void RefreshStatus()
	{
		if (GodotObject.IsInstanceValid(_statusLabel))
		{
			TutorialStepConfig currentStep = GetCurrentStep();
			if (!GodotObject.IsInstanceValid(currentStep))
			{
				_statusLabel.Text = ((_editScope == TutorialEditScope.Tutorial) ? "当前教程还没有步骤，点击上方“＋”即可直接创建第一步。" : "当前独立资源预览没有可编辑步骤。");
				return;
			}
			_statusLabel.Text = $"步骤 {_stepIndex + 1} / {StepCount} · 广播 {(currentStep.broadCastUse ? "启用" : "关闭")} · 条件 {currentStep.conditionList?.Count ?? 0} 个 · {BuildScopeLabel()}";
		}
	}

	private void DisposeConditionBindings()
	{
		foreach (XWVisualPropertyBinding conditionBinding in _conditionBindings)
		{
			conditionBinding.Dispose();
		}
		_conditionBindings.Clear();
	}

	private static BroadCastConfig CreateDefaultBroadcast()
	{
		return new BroadCastConfig
		{
			ResourceLocalToScene = true,
			broadCastString = "点击这里编辑新的教程提示",
			broadCastTime = -1.0
		};
	}

	private static void SetButtonIcon(Button button, string path, string tooltip)
	{
		if (GodotObject.IsInstanceValid(button))
		{
			button.Icon = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
			button.TooltipText = tooltip;
		}
	}

	private bool CanEditCondition(TutorialConditionConfig condition)
	{
		if (!GodotObject.IsInstanceValid(condition))
		{
			return false;
		}
		if (_editScope != TutorialEditScope.Condition)
		{
			return true;
		}
		return _editingRoot == condition;
	}

	private TutorialStepConfig GetCurrentStep()
	{
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		if (valueOrDefault <= 0 || _stepIndex < 0 || _stepIndex >= valueOrDefault)
		{
			return null;
		}
		return _config.step[_stepIndex];
	}

	private void EmitTutorialEdited(params Resource[] changedResources)
	{
		foreach (Resource resource in changedResources)
		{
			if (GodotObject.IsInstanceValid(resource))
			{
				resource.EmitChanged();
			}
		}
		if (GodotObject.IsInstanceValid(_config))
		{
			_config.EmitChanged();
		}
		if (GodotObject.IsInstanceValid(_editingRoot) && _editingRoot != _config)
		{
			_editingRoot.EmitChanged();
		}
		EmitSignal(SignalName.TutorialEdited);
	}

	private void RefreshStepOptionLabel()
	{
		TutorialStepConfig currentStep = GetCurrentStep();
		if (GodotObject.IsInstanceValid(currentStep) && _stepIndex >= 0 && _stepIndex < _stepOption.ItemCount)
		{
			_stepOption.SetItemText(_stepIndex, BuildStepLabel(_stepIndex, currentStep));
		}
		_stepVisualGallery?.Rebuild();
	}

	private void DisposeComparisonVisualChoices()
	{
		foreach (XWVisualSegmentedOption comparisonVisualChoice in _comparisonVisualChoices)
		{
			comparisonVisualChoice.Dispose();
		}
		_comparisonVisualChoices.Clear();
	}

	private static void SetVisualButtonsDisabled(HFlowContainer host, bool disabled)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		foreach (Node child in host.GetChildren())
		{
			if (child is Button button)
			{
				button.Disabled = disabled;
			}
		}
	}

	private string BuildScopeLabel()
	{
		return _editScope switch
		{
			TutorialEditScope.Step => "独立步骤资源", 
			TutorialEditScope.Condition => "独立条件资源", 
			_ => "完整教程资源", 
		};
	}

	private static string BuildStepLabel(int index, TutorialStepConfig step)
	{
		if (GodotObject.IsInstanceValid(step))
		{
			string text = step.broadCastConfig?.broadCastString ?? "";
			text = text.Replace('\n', ' ').Replace('\r', ' ').StripEdges();
			if (text.Length > 22)
			{
				text = text.Substring(0, 22) + "…";
			}
			return $"步骤 {index + 1} · {(step.broadCastUse ? text : "无广播")}";
		}
		return $"步骤 {index + 1} · 空资源";
	}
}
