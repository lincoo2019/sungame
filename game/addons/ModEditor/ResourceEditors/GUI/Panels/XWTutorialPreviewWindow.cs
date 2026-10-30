using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWTutorialPreviewWindow : Window
{
	private TutorialConfig _config;

	private int _stepIndex;

	private bool _autoPlaying;

	private ItemList _stepList;

	private Label _titleLabel;

	private Label _broadcastLabel;

	private BroadCastManager _broadcastManager;

	private Label _statusLabel;

	private VBoxContainer _conditionRows;

	private Button _previousButton;

	private Button _nextButton;

	private Button _autoButton;

	private Timer _autoTimer;

	private bool _updatingSelection;

	public override void _Ready()
	{
		base.CloseRequested += ClosePreview;
		BindInterface();
		RefreshConfigView();
	}

	public void Preview(TutorialConfig config)
	{
		_config = config;
		_stepIndex = 0;
		if (IsNodeReady())
		{
			RefreshConfigView();
			PopupCenteredClamped(new Vector2I(960, 640), 0.9f);
			GrabFocus();
		}
	}

	public void RefreshConfig(TutorialConfig config)
	{
		_config = config;
		if (IsNodeReady())
		{
			RefreshConfigView();
		}
	}

	private void BindInterface()
	{
		_titleLabel = GetNode<Label>("Layout/Title");
		_stepList = GetNode<ItemList>("Layout/Split/StepList");
		_broadcastManager = GetNode<BroadCastManager>("%RuntimeBroadcast");
		_broadcastLabel = _broadcastManager.GetNode<Label>("%BroadCastLabel");
		_conditionRows = GetNode<VBoxContainer>("Layout/Split/Right/ConditionScroll/ConditionRows");
		_previousButton = GetNode<Button>("Layout/Split/Right/Controls/PreviousButton");
		Button node = GetNode<Button>("Layout/Split/Right/Controls/CompleteButton");
		_autoButton = GetNode<Button>("Layout/Split/Right/Controls/AutoButton");
		_nextButton = GetNode<Button>("Layout/Split/Right/Controls/NextButton");
		_statusLabel = GetNode<Label>("Layout/Split/Right/Status");
		_autoTimer = GetNode<Timer>("AutoTimer");
		_stepList.ItemSelected += delegate(long index)
		{
			if (!_updatingSelection)
			{
				StopAutoPlay();
				_stepIndex = (int)index;
				ShowCurrentStep();
			}
		};
		_previousButton.Pressed += PreviousStep;
		node.Pressed += CompleteCurrentStep;
		_autoButton.Pressed += ToggleAutoPlay;
		_nextButton.Pressed += NextStep;
		_autoTimer.Timeout += NextStep;
	}

	private void RefreshConfigView()
	{
		_stepList.Clear();
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		for (int i = 0; i < valueOrDefault; i++)
		{
			TutorialStepConfig tutorialStepConfig = _config.step[i];
			string value = ((GodotObject.IsInstanceValid(tutorialStepConfig) && tutorialStepConfig.broadCastUse) ? PreviewText(tutorialStepConfig.broadCastConfig?.broadCastString) : "无广播");
			_stepList.AddItem($"步骤 {i + 1}  ·  {value}");
		}
		_titleLabel.Text = "教程流程运行预览  ·  " + EmptyToPlaceholder(_config?.saveKey);
		_stepIndex = ((valueOrDefault != 0) ? Mathf.Clamp(_stepIndex, 0, valueOrDefault - 1) : 0);
		ShowCurrentStep();
	}

	private void ShowCurrentStep()
	{
		_broadcastManager?.BraodCastClear();
		foreach (Node child in _conditionRows.GetChildren())
		{
			child.QueueFree();
		}
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		if (valueOrDefault == 0)
		{
			_broadcastLabel.Text = "尚未添加教程步骤";
			_statusLabel.Text = "0 / 0";
			_previousButton.Disabled = true;
			_nextButton.Disabled = true;
			return;
		}
		_stepIndex = Mathf.Clamp(_stepIndex, 0, valueOrDefault - 1);
		TutorialStepConfig tutorialStepConfig = _config.step[_stepIndex];
		bool flag = GodotObject.IsInstanceValid(tutorialStepConfig);
		if (flag && tutorialStepConfig.broadCastUse && GodotObject.IsInstanceValid(tutorialStepConfig.broadCastConfig))
		{
			_broadcastManager?.BroadCastAdd(tutorialStepConfig.broadCastConfig);
		}
		else
		{
			_broadcastLabel.Text = "（该步骤无广播文本）";
		}
		if (flag && tutorialStepConfig.conditionList != null)
		{
			for (int i = 0; i < tutorialStepConfig.conditionList.Count; i++)
			{
				TutorialConditionConfig condition = tutorialStepConfig.conditionList[i];
				_conditionRows.AddChild(new Label
				{
					Text = $"{i + 1}. {DescribeCondition(condition)}",
					AutowrapMode = TextServer.AutowrapMode.WordSmart
				}, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (_conditionRows.GetChildCount() == 0)
		{
			_conditionRows.AddChild(new Label
			{
				Text = "无条件：该步骤可直接完成"
			}, forceReadableName: false, InternalMode.Disabled);
		}
		_updatingSelection = true;
		_stepList.Select(_stepIndex);
		_stepList.EnsureCurrentIsVisible();
		_updatingSelection = false;
		_previousButton.Disabled = _stepIndex <= 0;
		_nextButton.Disabled = _stepIndex >= valueOrDefault - 1;
		double num = ((flag && tutorialStepConfig.broadCastUse && GodotObject.IsInstanceValid(tutorialStepConfig.broadCastConfig)) ? tutorialStepConfig.broadCastConfig.broadCastTime : (-1.0));
		_statusLabel.Text = $"{_stepIndex + 1} / {valueOrDefault}    广播时长: {((num < 0.0) ? "手动继续" : $"{num:0.##} 秒")}    条件: {(tutorialStepConfig?.conditionList?.Count).GetValueOrDefault()}";
		ScheduleAuto(num);
	}

	private void CompleteCurrentStep()
	{
		NextStep();
	}

	private void PreviousStep()
	{
		StopAutoPlay();
		if (_stepIndex > 0)
		{
			_stepIndex--;
			ShowCurrentStep();
		}
	}

	private void NextStep()
	{
		int valueOrDefault = (_config?.step?.Count).GetValueOrDefault();
		if (_stepIndex + 1 >= valueOrDefault)
		{
			StopAutoPlay();
			_statusLabel.Text = $"教程预览完成，共 {valueOrDefault} 个步骤";
		}
		else
		{
			_stepIndex++;
			ShowCurrentStep();
		}
	}

	private void ToggleAutoPlay()
	{
		if (_autoPlaying)
		{
			StopAutoPlay();
			return;
		}
		_autoPlaying = true;
		_stepIndex = 0;
		_autoButton.Text = "暂停播放";
		ShowCurrentStep();
	}

	private void ScheduleAuto(double configuredDuration)
	{
		if (_autoPlaying)
		{
			_autoTimer.Start((configuredDuration > 0.0) ? configuredDuration : 2.5);
		}
	}

	private void StopAutoPlay()
	{
		_autoPlaying = false;
		_autoTimer.Stop();
		_autoButton.Text = "自动播放";
	}

	private void ClosePreview()
	{
		StopAutoPlay();
		Hide();
	}

	private static string DescribeCondition(TutorialConditionConfig condition)
	{
		if (!(condition is TutorialConditionCheckSunCollect tutorialConditionCheckSunCollect))
		{
			if (!(condition is TutorialConditionCheckCharaterNum tutorialConditionCheckCharaterNum))
			{
				if (condition == null)
				{
					return "空条件";
				}
				return condition.GetType().Name;
			}
			return $"角色 {EmptyToPlaceholder(tutorialConditionCheckCharaterNum.characterName)} 数量 {tutorialConditionCheckCharaterNum.method} {tutorialConditionCheckCharaterNum.num}";
		}
		return $"收集阳光 ≥ {tutorialConditionCheckSunCollect.num}";
	}

	private static string PreviewText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return "空文本";
		}
		string text2 = text.Replace('\n', ' ').Replace('\r', ' ');
		if (text2.Length <= 24)
		{
			return text2;
		}
		return text2.Substring(0, 24) + "…";
	}

	private static string EmptyToPlaceholder(string text)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "未设置";
	}
}
