using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace PVZHE.ModEditor.Debugging;

public partial class XWModDebugWorkbench : PanelContainer
{
	private readonly object _pendingSnapshotLock = new object();

	private XWModDebugController _controller;

	private XWModDebugSnapshot _snapshot;

	private XWModDebugSnapshot _pendingSnapshot;

	private int _pendingSnapshotGeneration;

	private int _bindingGeneration;

	private int _deferredRefreshQueued;

	private bool _snapshotDirty;

	private bool _leavingTree;

	private double _reconcileElapsed;

	private Label _stateBadgeLabel;

	private Label _statusLabel;

	private Label _locationLabel;

	private Label _pauseReasonLabel;

	private Label _callStackCountLabel;

	private Label _variableCountLabel;

	private Label _footerLabel;

	private Button _pauseContinueButton;

	private Button _stepIntoButton;

	private Button _stepOverButton;

	private Button _stepOutButton;

	private Button _stopButton;

	private ItemList _callStackList;

	private ItemList _variableList;

	private GridContainer _dataSplit;

	public XWModDebugController Controller => _controller;

	public XWModDebugSnapshot Snapshot => _snapshot;

	public bool IsControllerBound => _controller != null;

	public bool IsHiddenProcessSuspended => !IsProcessing();

	public int CallStackItemCount => _callStackList?.ItemCount ?? 0;

	public int VariableItemCount => _variableList?.ItemCount ?? 0;

	public string StatusText => _statusLabel?.Text ?? "";

	public string CurrentLocationText => _locationLabel?.Text ?? "";

	public string PauseReasonText => _pauseReasonLabel?.Text ?? "";

	public long LastAppliedSequence => _snapshot?.Sequence ?? (-1);

	public int VisibleRefreshCount { get; private set; }

	public event Action StopRequested;

	public override void _Ready()
	{
		_stateBadgeLabel = GetNode<Label>("%StateBadgeLabel");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_locationLabel = GetNode<Label>("%LocationLabel");
		_pauseReasonLabel = GetNode<Label>("%PauseReasonLabel");
		_callStackCountLabel = GetNode<Label>("%CallStackCountLabel");
		_variableCountLabel = GetNode<Label>("%VariableCountLabel");
		_footerLabel = GetNode<Label>("%FooterLabel");
		_pauseContinueButton = GetNode<Button>("%PauseContinueButton");
		_stepIntoButton = GetNode<Button>("%StepIntoButton");
		_stepOverButton = GetNode<Button>("%StepOverButton");
		_stepOutButton = GetNode<Button>("%StepOutButton");
		_stopButton = GetNode<Button>("%StopButton");
		_callStackList = GetNode<ItemList>("%CallStackList");
		_variableList = GetNode<ItemList>("%VariableList");
		_dataSplit = GetNode<GridContainer>("%DataSplit");
		_pauseContinueButton.Pressed += OnPauseContinuePressed;
		_stepIntoButton.Pressed += OnStepIntoPressed;
		_stepOverButton.Pressed += OnStepOverPressed;
		_stepOutButton.Pressed += OnStepOutPressed;
		_stopButton.Pressed += OnStopPressed;
		base.VisibilityChanged += UpdateProcessingState;
		base.Resized += UpdateResponsiveLayout;
		if (_snapshot != null)
		{
			RefreshSnapshot(_snapshot);
		}
		else
		{
			ShowDisconnectedState();
		}
		UpdateProcessingState();
		UpdateResponsiveLayout();
	}

	public override void _ExitTree()
	{
		_leavingTree = true;
		base.Resized -= UpdateResponsiveLayout;
		UnbindController();
		SetProcess(enable: false);
	}

	public override void _Process(double delta)
	{
		if (!IsVisibleInTree() || _controller == null)
		{
			SetProcess(enable: false);
			return;
		}
		if (_snapshotDirty)
		{
			_snapshotDirty = false;
			RefreshSnapshot(_snapshot);
		}
		_reconcileElapsed += delta;
		if (!(_reconcileElapsed < 0.5))
		{
			_reconcileElapsed = 0.0;
			XWModDebugSnapshot snapshot = _controller.Snapshot;
			if (snapshot != null && snapshot.Sequence != (_snapshot?.Sequence ?? (-1)))
			{
				QueueSnapshotFromAnyThread(snapshot, _bindingGeneration);
			}
		}
	}

	public void BindController(XWModDebugController controller)
	{
		if (_controller == controller)
		{
			if (controller != null)
			{
				QueueSnapshotFromAnyThread(controller.Snapshot, _bindingGeneration);
			}
			return;
		}
		UnbindController();
		_leavingTree = false;
		_controller = controller;
		_bindingGeneration++;
		if (_controller != null)
		{
			_controller.SnapshotChanged += OnControllerSnapshotChanged;
			QueueSnapshotFromAnyThread(_controller.Snapshot, _bindingGeneration);
		}
		else if (IsNodeReady())
		{
			ShowDisconnectedState();
		}
		UpdateProcessingState();
	}

	public void UnbindController()
	{
		XWModDebugController controller = _controller;
		_controller = null;
		_bindingGeneration++;
		if (controller != null)
		{
			controller.SnapshotChanged -= OnControllerSnapshotChanged;
		}
		lock (_pendingSnapshotLock)
		{
			_pendingSnapshot = null;
			_pendingSnapshotGeneration = _bindingGeneration;
		}
		Interlocked.Exchange(ref _deferredRefreshQueued, 0);
		_snapshot = null;
		_snapshotDirty = false;
		if (IsNodeReady())
		{
			ShowDisconnectedState();
		}
		SetProcess(enable: false);
	}

	private void OnControllerSnapshotChanged(object sender, XWModDebugSnapshot snapshot)
	{
		XWModDebugController controller = _controller;
		if (controller != null && sender == controller && snapshot != null)
		{
			QueueSnapshotFromAnyThread(snapshot, _bindingGeneration);
		}
	}

	private void QueueSnapshotFromAnyThread(XWModDebugSnapshot snapshot, int generation)
	{
		if (snapshot == null || _leavingTree || generation != _bindingGeneration)
		{
			return;
		}
		lock (_pendingSnapshotLock)
		{
			if (generation != _bindingGeneration)
			{
				return;
			}
			_pendingSnapshot = snapshot;
			_pendingSnapshotGeneration = generation;
		}
		if (Interlocked.Exchange(ref _deferredRefreshQueued, 1) == 0)
		{
			CallDeferred("ApplyPendingSnapshotOnMainThread");
		}
	}

	private void ApplyPendingSnapshotOnMainThread()
	{
		Interlocked.Exchange(ref _deferredRefreshQueued, 0);
		XWModDebugSnapshot pendingSnapshot;
		int pendingSnapshotGeneration;
		lock (_pendingSnapshotLock)
		{
			pendingSnapshot = _pendingSnapshot;
			pendingSnapshotGeneration = _pendingSnapshotGeneration;
			_pendingSnapshot = null;
		}
		if (!_leavingTree && pendingSnapshot != null && pendingSnapshotGeneration == _bindingGeneration && _controller != null)
		{
			_snapshot = pendingSnapshot;
			if (!IsNodeReady() || !IsVisibleInTree())
			{
				_snapshotDirty = true;
			}
			else
			{
				RefreshSnapshot(pendingSnapshot);
			}
		}
	}

	private void RefreshSnapshot(XWModDebugSnapshot snapshot)
	{
		if (IsNodeReady() && snapshot != null)
		{
			VisibleRefreshCount++;
			_stateBadgeLabel.Text = GetStateBadge(snapshot.State);
			_stateBadgeLabel.Modulate = GetStateColor(snapshot.State);
			_statusLabel.Text = (string.IsNullOrWhiteSpace(snapshot.StatusText) ? "等待调试状态" : snapshot.StatusText);
			_locationLabel.Text = FormatCheckpoint(snapshot.CurrentCheckpoint);
			_pauseReasonLabel.Text = "暂停原因：" + GetPauseReasonText(snapshot.PauseReason);
			RefreshCallStack(snapshot);
			RefreshVariables(snapshot);
			UpdateCommandButtons(snapshot);
			_footerLabel.Text = ((snapshot.State == XWModDebugState.Paused) ? "已停在安全检查点，可单步检查调用路径与变量。" : "调试工作在后台异步运行，不会阻塞编辑器主线程。");
			_footerLabel.Modulate = new Color("81b0c4");
		}
	}

	private void RefreshCallStack(XWModDebugSnapshot snapshot)
	{
		_callStackList.Clear();
		for (int i = 0; i < snapshot.CallStack.Count; i++)
		{
			XWModDebugStackFrame xWModDebugStackFrame = snapshot.CallStack[i];
			string text = FormatFrameLocation(xWModDebugStackFrame);
			_callStackList.AddItem("◆ " + xWModDebugStackFrame.DisplayName + "\n   " + text);
			_callStackList.SetItemTooltip(i, $"{xWModDebugStackFrame.DisplayName}\n{text}\n帧编号：{xWModDebugStackFrame.Id}");
		}
		if (_callStackList.ItemCount == 0)
		{
			_callStackList.AddItem("◇ 暂无调用栈；暂停后将在这里显示执行路径");
			_callStackList.SetItemDisabled(0, disabled: true);
		}
		_callStackCountLabel.Text = ((snapshot.CallStack.Count > 0) ? $"{snapshot.CallStack.Count} 帧" : "未暂停");
	}

	private void RefreshVariables(XWModDebugSnapshot snapshot)
	{
		_variableList.Clear();
		int rendered = 0;
		foreach (var (fallbackName, variable) in snapshot.Variables)
		{
			AppendVariable(fallbackName, variable, 0, ref rendered);
		}
		if (rendered == 0)
		{
			_variableList.AddItem("◇ 暂无变量；命中断点后可检查当前上下文");
			_variableList.SetItemDisabled(0, disabled: true);
		}
		_variableCountLabel.Text = ((rendered > 0) ? $"{rendered} 项" : "无数据");
	}

	private void AppendVariable(string fallbackName, XWModDebugVariable variable, int depth, ref int rendered)
	{
		if (variable == null || rendered >= 512)
		{
			return;
		}
		string value = (string.IsNullOrWhiteSpace(variable.Name) ? fallbackName : variable.Name);
		string value2 = (string.IsNullOrWhiteSpace(variable.TypeName) ? "自动" : variable.TypeName);
		string value3 = new string(' ', Math.Min(depth, 12) * 3);
		_variableList.AddItem($"{value3}◆ {value}  :  {value2}\n{value3}   {variable.DisplayValue}");
		int idx = _variableList.ItemCount - 1;
		_variableList.SetItemTooltip(idx, $"{value} ({value2}) = {variable.DisplayValue}");
		rendered++;
		foreach (XWModDebugVariable child in variable.Children)
		{
			AppendVariable(child?.Name ?? "", child, depth + 1, ref rendered);
		}
	}

	private void UpdateCommandButtons(XWModDebugSnapshot snapshot)
	{
		bool flag = snapshot.State == XWModDebugState.Paused;
		_pauseContinueButton.Text = (flag ? "▶ 继续" : "Ⅱ 暂停");
		_pauseContinueButton.TooltipText = (flag ? "继续运行到下一个断点" : "请求在下一个安全检查点暂停");
		_pauseContinueButton.Disabled = (flag ? (!snapshot.CanContinue) : (snapshot.State != XWModDebugState.Running));
		_stepIntoButton.Disabled = !snapshot.CanStep;
		_stepOverButton.Disabled = !snapshot.CanStep;
		_stepOutButton.Disabled = !snapshot.CanStep;
		_stopButton.Disabled = !snapshot.CanStop;
	}

	private void OnPauseContinuePressed()
	{
		if (_controller != null && _snapshot != null)
		{
			bool accepted = ((_snapshot.State == XWModDebugState.Paused) ? _controller.Continue() : _controller.RequestPause());
			ShowCommandResult(accepted, (_snapshot.State == XWModDebugState.Paused) ? "继续运行" : "请求暂停");
		}
	}

	private void OnStepIntoPressed()
	{
		ShowCommandResult(_controller?.StepInto() ?? false, "单步进入");
	}

	private void OnStepOverPressed()
	{
		ShowCommandResult(_controller?.StepOver() ?? false, "单步跳过");
	}

	private void OnStepOutPressed()
	{
		ShowCommandResult(_controller?.StepOut() ?? false, "单步跳出");
	}

	private void OnStopPressed()
	{
		if (_controller != null)
		{
			Action action = this.StopRequested;
			if (action != null)
			{
				action();
				_footerLabel.Text = "已向宿主请求停止调试。";
			}
			else
			{
				ShowCommandResult(_controller.Stop(), "停止调试");
			}
		}
	}

	private void ShowCommandResult(bool accepted, string action)
	{
		_footerLabel.Text = (accepted ? ("已发送“" + action + "”命令。") : ("当前调试状态不允许“" + action + "”。"));
		_footerLabel.Modulate = (accepted ? new Color("9ddfb0") : new Color("ff9a80"));
	}

	private void UpdateProcessingState()
	{
		bool flag = IsInsideTree() && IsVisibleInTree() && _controller != null;
		SetProcess(flag);
		if (flag)
		{
			UpdateResponsiveLayout();
		}
		if (flag && _snapshotDirty)
		{
			_snapshotDirty = false;
			RefreshSnapshot(_snapshot);
		}
	}

	private void UpdateResponsiveLayout()
	{
		if (GodotObject.IsInstanceValid(_dataSplit))
		{
			_dataSplit.Columns = ((base.Size.X < 700f) ? 1 : 2);
		}
	}

	private void ShowDisconnectedState()
	{
		if (IsNodeReady())
		{
			_stateBadgeLabel.Text = "◇ 未连接";
			_stateBadgeLabel.Modulate = new Color("8ba0aa");
			_statusLabel.Text = "等待 C# 或蓝图调试会话";
			_locationLabel.Text = "当前位置：尚未进入调试检查点";
			_pauseReasonLabel.Text = "暂停原因：无";
			_callStackList.Clear();
			_callStackList.AddItem("◇ 绑定调试控制器后显示调用栈");
			_callStackList.SetItemDisabled(0, disabled: true);
			_variableList.Clear();
			_variableList.AddItem("◇ 暂无变量监视");
			_variableList.SetItemDisabled(0, disabled: true);
			_callStackCountLabel.Text = "未连接";
			_variableCountLabel.Text = "未连接";
			_footerLabel.Text = "共享工作台可由 C# 脚本编辑器与蓝图编辑器复用。";
			Button[] array = new Button[5] { _pauseContinueButton, _stepIntoButton, _stepOverButton, _stepOutButton, _stopButton };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Disabled = true;
			}
		}
	}

	private static string FormatCheckpoint(XWModDebugCheckpoint checkpoint)
	{
		if (checkpoint == null)
		{
			return "当前位置：等待下一个安全检查点";
		}
		if (checkpoint.Kind != XWModDebugCheckpointKind.CSharpLine)
		{
			return $"当前位置：{checkpoint.DisplayName} · 蓝图节点 {checkpoint.BlueprintNodeId}\n{checkpoint.SourcePath}";
		}
		return $"当前位置：{checkpoint.DisplayName} · {checkpoint.SourcePath} 第 {checkpoint.Line} 行";
	}

	private static string FormatFrameLocation(XWModDebugStackFrame frame)
	{
		if (frame == null)
		{
			return "未知调用位置";
		}
		if (frame.Line > 0)
		{
			return $"{frame.SourcePath} 第 {frame.Line} 行";
		}
		if (!string.IsNullOrWhiteSpace(frame.BlueprintNodeId))
		{
			return frame.SourcePath + " · 节点 " + frame.BlueprintNodeId;
		}
		if (!string.IsNullOrWhiteSpace(frame.SourcePath))
		{
			return frame.SourcePath;
		}
		return "内部调用帧";
	}

	private static string GetStateBadge(XWModDebugState state)
	{
		return state switch
		{
			XWModDebugState.Running => "▶ 运行中", 
			XWModDebugState.PauseRequested => "◌ 等待暂停", 
			XWModDebugState.Paused => "Ⅱ 已暂停", 
			XWModDebugState.Stopped => "■ 已停止", 
			_ => "◇ 未知状态", 
		};
	}

	private static Color GetStateColor(XWModDebugState state)
	{
		return state switch
		{
			XWModDebugState.Running => new Color("78e196"), 
			XWModDebugState.PauseRequested => new Color("ffd36d"), 
			XWModDebugState.Paused => new Color("79c8ff"), 
			XWModDebugState.Stopped => new Color("ff8b77"), 
			_ => new Color("9babb3"), 
		};
	}

	private static string GetPauseReasonText(XWModDebugPauseReason reason)
	{
		return reason switch
		{
			XWModDebugPauseReason.None => "无", 
			XWModDebugPauseReason.PauseRequest => "手动暂停请求", 
			XWModDebugPauseReason.Breakpoint => "命中断点", 
			XWModDebugPauseReason.ForcedBreakpoint => "强制断点", 
			XWModDebugPauseReason.Step => "单步执行完成", 
			_ => "未知原因", 
		};
	}
}
