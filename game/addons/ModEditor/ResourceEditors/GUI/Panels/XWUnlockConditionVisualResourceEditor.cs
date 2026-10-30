using System;
using Godot;
using PVZHE.ModEditor.Core;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWUnlockConditionVisualResourceEditor : XWGenericVisualResourceEditor
{
	private const string WorkbenchScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWUnlockConditionWorkbench.tscn";

	private static PackedScene _workbenchScene;

	private UnlockConditionBaseConfig _editingCondition;

	private XWVisualPropertyBinding _propertyBinding;

	private VBoxContainer _baseFields;

	private VBoxContainer _levelFinishFields;

	private VBoxContainer _survivalFields;

	private VBoxContainer _packetFields;

	private VBoxContainer _packetBankFields;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private LineEdit _levelFinishKey;

	private LineEdit _survivalLevelKey;

	private SpinBox _round;

	private LineEdit _packetName;

	private LineEdit _bankName;

	private LineEdit _category;

	private SpinBox _count;

	private CheckBox _simulateComplete;

	private Label _simulateProgressLabel;

	private SpinBox _simulateProgress;

	private ProgressBar _progress;

	private Label _simulationHint;

	private Label _title;

	private Label _type;

	private PanelContainer _cardFrame;

	private Label _targetLabel;

	private TextureRect _lockIcon;

	private TextureRect _unlockIcon;

	private Label _stateLabel;

	private Label _requirement;

	private bool _updatingControls;

	public override void _ExitTree()
	{
		DisposePropertyBinding();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposePropertyBinding();
		if (base.CurrentResource is UnlockConditionBaseConfig editingCondition && base.CanvasGrid != null)
		{
			_editingCondition = editingCondition;
			base.CanvasGrid.Columns = 1;
			if (_workbenchScene == null)
			{
				_workbenchScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWUnlockConditionWorkbench.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _workbenchScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindWorkbench(vBoxContainer);
				PopulateControls();
				BindPropertyControls();
				UpdatePreviewState();
				AddSummaryRows();
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!IsBuiltInUnlockConditionWithCompleteVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool IsBuiltInUnlockConditionWithCompleteVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(UnlockConditionBaseConfig)) && !(type == typeof(UnlockConditionLevelFinishConfig)) && !(type == typeof(UnlockConditionLevelSurvivalRoundConfig)) && !(type == typeof(UnlockConditionPacketUnlockConfig)))
		{
			return type == typeof(UnlockConditionPacketBankCategoryPacketUnlockNumConfig);
		}
		return true;
	}

	private void DisposePropertyBinding(bool clearEditingCondition = true)
	{
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		if (clearEditingCondition)
		{
			_editingCondition = null;
		}
	}

	private void BindWorkbench(VBoxContainer root)
	{
		_baseFields = root.GetNode<VBoxContainer>("%BaseFields");
		_levelFinishFields = root.GetNode<VBoxContainer>("%LevelFinishFields");
		_survivalFields = root.GetNode<VBoxContainer>("%SurvivalFields");
		_packetFields = root.GetNode<VBoxContainer>("%PacketFields");
		_packetBankFields = root.GetNode<VBoxContainer>("%PacketBankFields");
		_resourceName = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToScene = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_levelFinishKey = root.GetNode<LineEdit>("%LevelFinishKey");
		_survivalLevelKey = root.GetNode<LineEdit>("%SurvivalLevelKey");
		_round = root.GetNode<SpinBox>("%Round");
		_packetName = root.GetNode<LineEdit>("%PacketName");
		_bankName = root.GetNode<LineEdit>("%BankName");
		_category = root.GetNode<LineEdit>("%Category");
		_count = root.GetNode<SpinBox>("%Count");
		_simulateComplete = root.GetNode<CheckBox>("%SimulateComplete");
		_simulateProgressLabel = root.GetNode<Label>("%SimulateProgressLabel");
		_simulateProgress = root.GetNode<SpinBox>("%SimulateProgress");
		_progress = root.GetNode<ProgressBar>("%Progress");
		_simulationHint = root.GetNode<Label>("%SimulationHint");
		_title = root.GetNode<Label>("%Title");
		_type = root.GetNode<Label>("%Type");
		_cardFrame = root.GetNode<PanelContainer>("%CardFrame");
		_targetLabel = root.GetNode<Label>("%TargetLabel");
		_lockIcon = root.GetNode<TextureRect>("%LockIcon");
		_unlockIcon = root.GetNode<TextureRect>("%UnlockIcon");
		_stateLabel = root.GetNode<Label>("%StateLabel");
		_requirement = root.GetNode<Label>("%Requirement");
		_simulateComplete.Toggled += delegate
		{
			if (!_updatingControls)
			{
				UpdatePreviewState();
			}
		};
		_simulateProgress.ValueChanged += delegate
		{
			if (!_updatingControls)
			{
				UpdatePreviewState();
			}
		};
	}

	private void BindPropertyControls()
	{
		DisposePropertyBinding(clearEditingCondition: false);
		if (GodotObject.IsInstanceValid(_editingCondition))
		{
			_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnUnlockConditionVisualPropertyEdited);
			_propertyBinding.BindText(_resourceName, _editingCondition, "resource_name", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
			_propertyBinding.BindToggle(_localToScene, _editingCondition, "resource_local_to_scene", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
			if (_editingCondition is UnlockConditionLevelFinishConfig resource)
			{
				_propertyBinding.BindText(_levelFinishKey, resource, "levelSaveKey", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
			}
			if (_editingCondition is UnlockConditionLevelSurvivalRoundConfig resource2)
			{
				_propertyBinding.BindText(_survivalLevelKey, resource2, "levelSaveKey", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
				_propertyBinding.BindNumber(_round, resource2, "roundNum", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
			}
			if (_editingCondition is UnlockConditionPacketUnlockConfig resource3)
			{
				_propertyBinding.BindText(_packetName, resource3, "packetName", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
			}
			if (_editingCondition is UnlockConditionPacketBankCategoryPacketUnlockNumConfig resource4)
			{
				_propertyBinding.BindText(_bankName, resource4, "packetBankName", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
				_propertyBinding.BindText(_category, resource4, "category", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
				_propertyBinding.BindNumber(_count, resource4, "num", UpdatePreviewState, this, "RefreshUnlockConditionEditorFromHistory");
			}
		}
	}

	private void PopulateControls()
	{
		if (!GodotObject.IsInstanceValid(_editingCondition) || !GodotObject.IsInstanceValid(_baseFields))
		{
			return;
		}
		_updatingControls = true;
		try
		{
			_baseFields.Visible = _editingCondition.GetType() == typeof(UnlockConditionBaseConfig);
			_levelFinishFields.Visible = _editingCondition is UnlockConditionLevelFinishConfig;
			_survivalFields.Visible = _editingCondition is UnlockConditionLevelSurvivalRoundConfig;
			_packetFields.Visible = _editingCondition is UnlockConditionPacketUnlockConfig;
			_packetBankFields.Visible = _editingCondition is UnlockConditionPacketBankCategoryPacketUnlockNumConfig;
			_simulateComplete.ButtonPressed = false;
			_simulateProgress.Value = 0.0;
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void OnUnlockConditionVisualPropertyEdited(bool committed)
	{
		if (base.CurrentResource is UnlockConditionBaseConfig unlockConditionBaseConfig && unlockConditionBaseConfig == _editingCondition)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
				return;
			}
			MarkCurrentResourceDirty();
			base.CurrentResource.EmitChanged();
		}
	}

	public void RefreshUnlockConditionEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is UnlockConditionBaseConfig unlockConditionBaseConfig && unlockConditionBaseConfig == _editingCondition)
		{
			BindPropertyControls();
			PopulateControls();
			UpdatePreviewState();
		}
	}

	private void UpdatePreviewState()
	{
		if (!GodotObject.IsInstanceValid(_editingCondition) || !GodotObject.IsInstanceValid(_requirement))
		{
			return;
		}
		int num = 0;
		int num2 = 1;
		UnlockConditionBaseConfig editingCondition = _editingCondition;
		string text;
		string text4;
		string text5;
		bool flag;
		bool flag2;
		if (!(editingCondition is UnlockConditionLevelFinishConfig unlockConditionLevelFinishConfig))
		{
			if (!(editingCondition is UnlockConditionLevelSurvivalRoundConfig unlockConditionLevelSurvivalRoundConfig))
			{
				if (!(editingCondition is UnlockConditionPacketUnlockConfig unlockConditionPacketUnlockConfig))
				{
					if (editingCondition is UnlockConditionPacketBankCategoryPacketUnlockNumConfig unlockConditionPacketBankCategoryPacketUnlockNumConfig)
					{
						text = "卡牌库数量解锁";
						string text2 = EmptyToPlaceholder(unlockConditionPacketBankCategoryPacketUnlockNumConfig.packetBankName, "卡牌库");
						string text3 = EmptyToPlaceholder(unlockConditionPacketBankCategoryPacketUnlockNumConfig.category, "分类");
						text4 = text2 + " / " + text3;
						num2 = Math.Max(1, unlockConditionPacketBankCategoryPacketUnlockNumConfig.num);
						num = Mathf.RoundToInt(_simulateProgress.Value);
						text5 = $"在 {text4} 中至少解锁 {num2} 张卡牌";
						flag = num >= num2;
						flag2 = true;
					}
					else
					{
						text = "基础解锁条件";
						text4 = "未指定内容";
						text5 = "基础条件不会自动解锁，请换用一个具体条件类型";
						flag = false;
						flag2 = false;
					}
				}
				else
				{
					text = "卡牌前置解锁";
					text4 = EmptyToPlaceholder(unlockConditionPacketUnlockConfig.packetName, "卡牌 saveKey");
					text5 = "先解锁卡牌：" + text4;
					flag = _simulateComplete.ButtonPressed;
					num = (flag ? 1 : 0);
					flag2 = false;
				}
			}
			else
			{
				text = "生存轮数解锁";
				text4 = EmptyToPlaceholder(unlockConditionLevelSurvivalRoundConfig.levelSaveKey, "生存关卡存档键");
				num2 = Math.Max(1, unlockConditionLevelSurvivalRoundConfig.roundNum);
				num = Mathf.RoundToInt(_simulateProgress.Value);
				text5 = $"在 {text4} 完成 {num2} 轮";
				flag = num >= num2;
				flag2 = true;
			}
		}
		else
		{
			text = "通关解锁";
			text4 = EmptyToPlaceholder(unlockConditionLevelFinishConfig.levelSaveKey, "关卡存档键");
			text5 = ((unlockConditionLevelFinishConfig.levelSaveKey == "Unlock") ? "该内容在游戏中始终解锁" : ("通关关卡：" + text4));
			flag = unlockConditionLevelFinishConfig.levelSaveKey == "Unlock" || _simulateComplete.ButtonPressed;
			num = (flag ? 1 : 0);
			flag2 = false;
		}
		_type.Text = text;
		_title.Text = "游戏解锁条件 · " + GetResourceDisplayName();
		_targetLabel.Text = text4;
		_requirement.Text = text5;
		_stateLabel.Text = (flag ? "游戏中已解锁" : "游戏中仍锁定");
		_stateLabel.Modulate = (flag ? new Color(0.62f, 1f, 0.42f) : new Color(1f, 0.72f, 0.42f));
		_lockIcon.Visible = !flag;
		_unlockIcon.Visible = flag;
		_cardFrame.Modulate = (flag ? Colors.White : new Color(0.62f, 0.62f, 0.62f));
		_simulateComplete.Visible = !flag2 && _editingCondition.GetType() != typeof(UnlockConditionBaseConfig);
		_simulateProgressLabel.Visible = flag2;
		_simulateProgress.Visible = flag2;
		_progress.Visible = _editingCondition.GetType() != typeof(UnlockConditionBaseConfig);
		_progress.MaxValue = Math.Max(1, num2);
		_progress.Value = Mathf.Clamp(num, 0, num2);
		_simulationHint.Text = (flag2 ? $"模拟进度：{num} / {num2}。这只改变预览，不会修改游戏存档。" : "切换前置条件状态可预览锁定/解锁结果；不会修改游戏存档。");
	}

	private void AddSummaryRows()
	{
		if (base.PreviewList != null)
		{
			base.PreviewList.AddItem("资源类型 -> " + _editingCondition.GetType().Name);
			base.PreviewList.AddItem("编辑区 -> 游戏锁定卡片上的解锁要求");
			base.PreviewList.AddItem("模拟状态 -> 仅用于预览，不读取或写入主游戏存档");
		}
	}

	private string GetResourceDisplayName()
	{
		if (!string.IsNullOrWhiteSpace(_editingCondition.ResourceName))
		{
			return _editingCondition.ResourceName;
		}
		if (!string.IsNullOrWhiteSpace(base.CurrentResourcePath))
		{
			string text = base.CurrentResourcePath.Replace('\\', '/');
			int num = text.LastIndexOf('/');
			if (num < 0)
			{
				return text;
			}
			string text2 = text;
			int num2 = num + 1;
			return text2.Substring(num2, text2.Length - num2);
		}
		return _editingCondition.GetType().Name;
	}

	private static string EmptyToPlaceholder(string value, string placeholder)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "<" + placeholder + ">";
	}
}
