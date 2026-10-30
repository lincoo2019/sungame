using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public partial class XWGameplayLifecycleWorkbench : PanelContainer
{
	private const double TickSeconds = 0.1;

	private XWGameplayLifecyclePlan.Snapshot _snapshot;

	private Resource _resource;

	private Button _readyButton;

	private Button _battleButton;

	private Button _settleButton;

	private Button _playPauseButton;

	private Button _stepButton;

	private Button _resetButton;

	private Label _phaseBadge;

	private Label _timeLabel;

	private Label _summaryLabel;

	private HSlider _scrubber;

	private VBoxContainer _historyRoot;

	private XWGameplayLifecycleTimelineCanvas _canvas;

	private Timer _timer;

	private double _previewTime;

	private bool _syncingScrubber;

	private bool _signalsConnected;

	public double PreviewTime => _previewTime;

	public bool IsPlaying
	{
		get
		{
			if (GodotObject.IsInstanceValid(_timer))
			{
				return !_timer.IsStopped();
			}
			return false;
		}
	}

	public string PhaseName => XWGameplayLifecyclePlan.PhaseAt(_snapshot, _previewTime).ToString();

	public override void _Ready()
	{
		_readyButton = GetNode<Button>("%LifecycleReadyButton");
		_battleButton = GetNode<Button>("%LifecycleBattleButton");
		_settleButton = GetNode<Button>("%LifecycleSettleButton");
		_playPauseButton = GetNode<Button>("%PlayPauseButton");
		_stepButton = GetNode<Button>("%StepButton");
		_resetButton = GetNode<Button>("%ResetButton");
		_phaseBadge = GetNode<Label>("%LifecyclePhaseBadge");
		_timeLabel = GetNode<Label>("%LifecycleTimeLabel");
		_summaryLabel = GetNode<Label>("%LifecycleSummaryLabel");
		_scrubber = GetNode<HSlider>("%LifecycleScrubber");
		_historyRoot = GetNode<VBoxContainer>("%LifecycleHistoryRoot");
		_canvas = GetNode<XWGameplayLifecycleTimelineCanvas>("%LifecycleTimelineCanvas");
		_timer = GetNode<Timer>("%LifecyclePreviewTimer");
		ConnectSignals();
		SetProcess(enable: false);
		Bind(null);
	}

	public override void _ExitTree()
	{
		StopPlayback();
		DisconnectSignals();
		_resource = null;
		_snapshot = null;
	}

	public void Bind(Resource resource)
	{
		StopPlayback();
		_resource = (GodotObject.IsInstanceValid(resource) ? resource : null);
		_snapshot = XWGameplayLifecyclePlan.Build(_resource);
		if (GodotObject.IsInstanceValid(_summaryLabel))
		{
			_summaryLabel.Text = _snapshot.Summary;
		}
		if (GodotObject.IsInstanceValid(_scrubber))
		{
			_scrubber.MaxValue = Math.Max(0.01, _snapshot.TotalDuration);
		}
		SetPreviewTime(0.0);
	}

	public void Unbind()
	{
		StopPlayback();
		_resource = null;
		_snapshot = XWGameplayLifecyclePlan.Build(null);
		SetPreviewTime(0.0);
	}

	public void TogglePlayback()
	{
		if (!GodotObject.IsInstanceValid(_timer) || _snapshot == null || !IsVisibleInTree())
		{
			return;
		}
		if (IsPlaying)
		{
			StopPlayback();
			return;
		}
		if (_previewTime >= _snapshot.TotalDuration)
		{
			SetPreviewTime(0.0);
		}
		_timer.Start();
		UpdatePlayButton();
	}

	public void StepForward()
	{
		StopPlayback();
		if (_snapshot == null)
		{
			return;
		}
		double previewTime = Math.Min(_snapshot.TotalDuration, _previewTime + 1.0);
		foreach (XWGameplayLifecyclePlan.Entry entry in _snapshot.Entries)
		{
			if (entry.Time > _previewTime + 0.001)
			{
				previewTime = entry.Time;
				break;
			}
		}
		SetPreviewTime(previewTime);
	}

	public void ResetPreview()
	{
		StopPlayback();
		SetPreviewTime(0.0);
	}

	private void ConnectSignals()
	{
		if (!_signalsConnected)
		{
			_readyButton.Pressed += GoReady;
			_battleButton.Pressed += GoBattle;
			_settleButton.Pressed += GoSettle;
			_playPauseButton.Pressed += TogglePlayback;
			_stepButton.Pressed += StepForward;
			_resetButton.Pressed += ResetPreview;
			_scrubber.ValueChanged += OnScrubberChanged;
			_timer.Timeout += OnPreviewTick;
			base.VisibilityChanged += OnVisibilityChanged;
			_signalsConnected = true;
		}
	}

	private void DisconnectSignals()
	{
		if (_signalsConnected)
		{
			if (GodotObject.IsInstanceValid(_readyButton))
			{
				_readyButton.Pressed -= GoReady;
			}
			if (GodotObject.IsInstanceValid(_battleButton))
			{
				_battleButton.Pressed -= GoBattle;
			}
			if (GodotObject.IsInstanceValid(_settleButton))
			{
				_settleButton.Pressed -= GoSettle;
			}
			if (GodotObject.IsInstanceValid(_playPauseButton))
			{
				_playPauseButton.Pressed -= TogglePlayback;
			}
			if (GodotObject.IsInstanceValid(_stepButton))
			{
				_stepButton.Pressed -= StepForward;
			}
			if (GodotObject.IsInstanceValid(_resetButton))
			{
				_resetButton.Pressed -= ResetPreview;
			}
			if (GodotObject.IsInstanceValid(_scrubber))
			{
				_scrubber.ValueChanged -= OnScrubberChanged;
			}
			if (GodotObject.IsInstanceValid(_timer))
			{
				_timer.Timeout -= OnPreviewTick;
			}
			base.VisibilityChanged -= OnVisibilityChanged;
			_signalsConnected = false;
		}
	}

	private void GoReady()
	{
		StopPlayback();
		SetPreviewTime(0.0);
	}

	private void GoBattle()
	{
		StopPlayback();
		SetPreviewTime(_snapshot?.BattleStart ?? 0.0);
	}

	private void GoSettle()
	{
		StopPlayback();
		SetPreviewTime(_snapshot?.SettleStart ?? 0.0);
	}

	private void OnScrubberChanged(double value)
	{
		if (!_syncingScrubber)
		{
			StopPlayback();
			SetPreviewTime(value);
		}
	}

	private void OnPreviewTick()
	{
		if (!IsVisibleInTree() || _snapshot == null)
		{
			StopPlayback();
			return;
		}
		double num = Math.Min(_snapshot.TotalDuration, _previewTime + 0.1);
		SetPreviewTime(num);
		if (num >= _snapshot.TotalDuration)
		{
			StopPlayback();
		}
	}

	private void OnVisibilityChanged()
	{
		if (!IsVisibleInTree())
		{
			StopPlayback();
		}
	}

	private void StopPlayback()
	{
		if (GodotObject.IsInstanceValid(_timer))
		{
			_timer.Stop();
		}
		UpdatePlayButton();
	}

	private void SetPreviewTime(double value)
	{
		if (_snapshot != null)
		{
			_previewTime = Math.Clamp(double.IsFinite(value) ? value : 0.0, 0.0, _snapshot.TotalDuration);
			_syncingScrubber = true;
			if (GodotObject.IsInstanceValid(_scrubber))
			{
				_scrubber.Value = _previewTime;
			}
			_syncingScrubber = false;
			UpdateLabels();
			_canvas?.SetPlan(_snapshot, _previewTime);
			RebuildHistory();
		}
	}

	private void UpdateLabels()
	{
		XWGameplayLifecyclePlan.Phase phase = XWGameplayLifecyclePlan.PhaseAt(_snapshot, _previewTime);
		if (GodotObject.IsInstanceValid(_readyButton))
		{
			_readyButton.ButtonPressed = phase == XWGameplayLifecyclePlan.Phase.Ready;
		}
		if (GodotObject.IsInstanceValid(_battleButton))
		{
			_battleButton.ButtonPressed = phase == XWGameplayLifecyclePlan.Phase.Battle;
		}
		if (GodotObject.IsInstanceValid(_settleButton))
		{
			_settleButton.ButtonPressed = phase == XWGameplayLifecyclePlan.Phase.Settle;
		}
		if (GodotObject.IsInstanceValid(_phaseBadge))
		{
			_phaseBadge.Text = phase.ToString().ToUpperInvariant();
			Label phaseBadge = _phaseBadge;
			StringName name = "font_color";
			phaseBadge.AddThemeColorOverride(name, phase switch
			{
				XWGameplayLifecyclePlan.Phase.Ready => new Color("8ed4ff"), 
				XWGameplayLifecyclePlan.Phase.Battle => new Color("91eda2"), 
				_ => new Color("ffc17d"), 
			});
		}
		if (GodotObject.IsInstanceValid(_timeLabel))
		{
			_timeLabel.Text = FormatTime(_previewTime) + " / " + FormatTime(_snapshot.TotalDuration);
		}
	}

	private void UpdatePlayButton()
	{
		if (GodotObject.IsInstanceValid(_playPauseButton))
		{
			_playPauseButton.Text = (IsPlaying ? "Ⅱ  暂停" : "▶  播放");
		}
	}

	private void RebuildHistory()
	{
		if (!GodotObject.IsInstanceValid(_historyRoot) || _snapshot == null)
		{
			return;
		}
		foreach (Node child in _historyRoot.GetChildren())
		{
			_historyRoot.RemoveChild(child);
			child.QueueFree();
		}
		int num = 0;
		foreach (XWGameplayLifecyclePlan.Entry entry in _snapshot.Entries)
		{
			if (!(entry.Time > _previewTime + 0.001) && num++ < 32)
			{
				Label label = new Label
				{
					Text = $"{FormatTime(entry.Time)}  {entry.Title}\n{entry.Detail}",
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
					TooltipText = $"{entry.Phase} · 沙盘记录，不是运行时事件"
				};
				Label label2 = label;
				StringName name = "font_color";
				label2.AddThemeColorOverride(name, entry.Phase switch
				{
					XWGameplayLifecyclePlan.Phase.Ready => new Color("9ed8f5"), 
					XWGameplayLifecyclePlan.Phase.Battle => new Color("a6e8ae"), 
					_ => new Color("f4c18c"), 
				});
				_historyRoot.AddChild(label, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private static string FormatTime(double seconds)
	{
		int num = Math.Max(0, (int)Math.Round(seconds));
		return $"{num / 60:00}:{num % 60:00}";
	}
}
