using Godot;

namespace PVZHE.ModEditor.RunBar;

public partial class XWEditorRunBar : HBoxContainer
{
	public enum RunMode
	{
		Stopped,
		RunMain,
		RunCurrent,
		RunCustom
	}

	[Signal]
	public delegate void PlayPressedEventHandler();

	[Signal]
	public delegate void PlayScenePressedEventHandler();

	[Signal]
	public delegate void StopPressedEventHandler();

	[Signal]
	public delegate void PauseToggledEventHandler(bool isPaused);

	[Signal]
	public delegate void PlayCustomPressedEventHandler();

	[Signal]
	public delegate void ProfilerIndicatorPressedEventHandler();

	[Signal]
	public delegate void RunStatusChangedEventHandler(bool running, bool paused, int mode);

	private RunMode _runMode;

	private bool _isPaused;

	private bool _updatingUi;

	private Button _profilerIndicator;

	private Button _playBtn;

	private Button _pauseBtn;

	private Button _stopBtn;

	private MenuButton _remoteBtn;

	private Button _playSceneBtn;

	private Button _playCustomBtn;

	public override void _Ready()
	{
		_profilerIndicator = GetNode<Button>("%ProfilerIndicator");
		_playBtn = GetNode<Button>("%PlayBtn");
		_pauseBtn = GetNode<Button>("%PauseBtn");
		_stopBtn = GetNode<Button>("%StopBtn");
		_remoteBtn = GetNode<MenuButton>("%RemoteBtn");
		_playSceneBtn = GetNode<Button>("%PlaySceneBtn");
		_playCustomBtn = GetNode<Button>("%PlayCustomBtn");
		InitRemoteMenu();
		_profilerIndicator.Pressed += delegate
		{
			EmitSignal(SignalName.ProfilerIndicatorPressed);
		};
		_playBtn.Toggled += OnPlayPressed;
		_playSceneBtn.Toggled += OnPlayScenePressed;
		_playCustomBtn.Toggled += OnPlayCustomPressed;
		_stopBtn.Pressed += delegate
		{
			EmitSignal(SignalName.StopPressed);
		};
		_pauseBtn.Toggled += OnPauseToggled;
		UpdateUI();
	}

	private void InitRemoteMenu()
	{
		PopupMenu popup = _remoteBtn.GetPopup();
		popup.Clear();
		popup.AddCheckItem("部署并远程调试", 0, Key.None);
		popup.AddSeparator();
		popup.AddItem("编辑远程调试设置...", 1, Key.None);
	}

	private void OnPlayPressed(bool pressed)
	{
		if (!_updatingUi && pressed)
		{
			EmitSignal(SignalName.PlayPressed);
		}
	}

	private void OnPlayScenePressed(bool pressed)
	{
		if (!_updatingUi && pressed)
		{
			EmitSignal(SignalName.PlayScenePressed);
		}
	}

	private void OnPlayCustomPressed(bool pressed)
	{
		if (!_updatingUi && pressed)
		{
			EmitSignal(SignalName.PlayCustomPressed);
		}
	}

	private void OnPauseToggled(bool pressed)
	{
		if (!_updatingUi)
		{
			EmitSignal(SignalName.PauseToggled, pressed);
		}
	}

	public void SetRunning(bool running)
	{
		if (running)
		{
			if (_runMode == RunMode.Stopped)
			{
				_runMode = RunMode.RunMain;
			}
		}
		else
		{
			_runMode = RunMode.Stopped;
			_isPaused = false;
		}
		UpdateUI();
	}

	public void SetRunMode(RunMode mode)
	{
		_runMode = mode;
		_isPaused = false;
		UpdateUI();
	}

	public void SetPaused(bool paused)
	{
		_isPaused = paused;
		UpdateUI();
	}

	public bool IsRunning()
	{
		return _runMode != RunMode.Stopped;
	}

	public bool IsPaused()
	{
		return _isPaused;
	}

	public RunMode GetRunMode()
	{
		return _runMode;
	}

	public void ShowProfilerIndicator(bool show)
	{
		if (GodotObject.IsInstanceValid(_profilerIndicator))
		{
			_profilerIndicator.Visible = show;
		}
	}

	public void SetProfilerText(string text)
	{
		if (GodotObject.IsInstanceValid(_profilerIndicator))
		{
			_profilerIndicator.Text = text;
		}
	}

	public void UpdateUI()
	{
		_updatingUi = true;
		bool flag = _runMode != RunMode.Stopped;
		_playBtn.SetPressedNoSignal(_runMode == RunMode.RunMain);
		_playSceneBtn.SetPressedNoSignal(_runMode == RunMode.RunCurrent);
		_playCustomBtn.SetPressedNoSignal(_runMode == RunMode.RunCustom);
		_stopBtn.Disabled = !flag;
		_pauseBtn.Disabled = !flag;
		_pauseBtn.SetPressedNoSignal(_isPaused);
		_updatingUi = false;
		EmitSignal(SignalName.RunStatusChanged, flag, _isPaused, (int)_runMode);
	}
}
