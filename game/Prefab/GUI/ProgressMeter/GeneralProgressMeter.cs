using System;
using System.Globalization;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GeneralProgressMeter : Control
{
	private const ulong AutoTextRefreshIntervalMsec = 100uL;

	private static PackedScene _generalProgressFlag;

	private TextureProgressBar _progressBar;

	private TextureRect _headTexture;

	private HBoxContainer _flagContainer;

	private Label _progressTextLabel;

	private string _progressTextTemplate = "";

	private bool _hasInitConfig;

	private ulong _nextAutoTextRefreshMsec;

	private long _lastTextValueUnits = -9223372036854775808L;

	private long _lastTextMaxUnits = -9223372036854775808L;

	private long _lastTextPercentUnits = -9223372036854775808L;

	private string _lastRenderedTemplate;

	private double _autoProgressResponse = 0.1;

	private int _renderedFlagWaveNum = -1;

	private int _renderedFlagWaveInterval = -1;

	[Export(PropertyHint.None, "")]
	public bool hideItem;

	[Export(PropertyHint.None, "")]
	public bool manualProgressValue;

	[Export(PropertyHint.None, "")]
	public bool showProgressText;

	private int _waveNum = 1;

	private int _waveInterval = 1;

	private int _previewWave;

	public Array<GeneralProgressFlag> flagList = new Array<GeneralProgressFlag>();

	private static PackedScene GENERAL_PROGRESS_FLAG => _generalProgressFlag ?? (_generalProgressFlag = GD.Load<PackedScene>("res://Prefab/GUI/ProgressMeter/ProgressFlag/GeneralProgressFlag.tscn"));

	public string ManualProgressText => _progressTextLabel?.Text ?? "";

	public string ManualProgressTextTemplate => _progressTextTemplate;

	[Export(PropertyHint.None, "")]
	public int waveNum
	{
		get
		{
			return _waveNum;
		}
		set
		{
			_waveNum = Mathf.Max(1, value);
			_previewWave = Mathf.Clamp(_previewWave, 0, _waveNum);
			if (Engine.IsEditorHint())
			{
				FlagRefresh();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int waveInterval
	{
		get
		{
			return _waveInterval;
		}
		set
		{
			_waveInterval = Mathf.Max(1, value);
			if (Engine.IsEditorHint())
			{
				FlagRefresh();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int previewWave
	{
		get
		{
			return _previewWave;
		}
		set
		{
			_previewWave = value;
			_previewWave = Mathf.Clamp(_previewWave, 0, _waveNum);
			if (!hideItem && flagList.Count != 0)
			{
				RefreshFlagReachState();
			}
		}
	}

	public void Init(int newWaveNum, int newWaveInterval)
	{
		_hasInitConfig = true;
		waveNum = newWaveNum;
		waveInterval = newWaveInterval;
		FlagRefresh();
		if (hideItem)
		{
			SetHideItem(hide: true);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Engine.IsEditorHint() && !manualProgressValue && IsVisibleInTree() && waveNum > 0 && !(_autoProgressResponse <= 0.0))
		{
			double num = _progressBar.MaxValue * (double)previewWave / (double)waveNum;
			float weight = 1f - Mathf.Exp((0f - (float)_autoProgressResponse) * (float)delta);
			_progressBar.Value = Mathf.Lerp((float)_progressBar.Value, (float)num, weight);
		}
	}

	public void SetAutoProgressResponse(double response)
	{
		_autoProgressResponse = Mathf.Max(0.0, response);
		RefreshPhysicsProcessing();
	}

	public void SetWaveCurrent(int waveCurrent)
	{
		previewWave = waveCurrent;
	}

	public void SetHideItem(bool hide)
	{
		if (hideItem == hide)
		{
			RefreshProgressPresentation();
			if (!hide)
			{
				FlagRefresh();
			}
			return;
		}
		hideItem = hide;
		RefreshProgressPresentation();
		if (hide)
		{
			_ClearFlags();
		}
		else
		{
			FlagRefresh();
		}
	}

	public void SetManualProgressEnabled(bool enabled)
	{
		bool flag = manualProgressValue;
		manualProgressValue = enabled;
		if (enabled)
		{
			showProgressText = true;
		}
		else if (flag)
		{
			showProgressText = false;
		}
		RefreshProgressPresentation(forceTextRefresh: true);
		RefreshPhysicsProcessing();
	}

	public void SetManualProgress(double value, double maxValue, string text)
	{
		_progressTextTemplate = text ?? "";
		SetManualProgressEnabled(enabled: true);
		double num = ((maxValue > 0.0) ? maxValue : 1.0);
		_progressBar.MaxValue = num;
		_progressBar.Value = Mathf.Clamp(value, 0.0, num);
		RefreshProgressPresentation(forceTextRefresh: true);
	}

	public void SetProgressValue(double value)
	{
		_progressBar.Value = Mathf.Clamp(value, _progressBar.MinValue, _progressBar.MaxValue);
	}

	public void SetProgressMaxValue(double maxValue)
	{
		_progressBar.MaxValue = ((maxValue > 0.0) ? maxValue : 1.0);
		RefreshProgressPresentation(forceTextRefresh: true);
	}

	public void SetProgressText(string text)
	{
		_progressTextTemplate = text ?? "";
		RefreshProgressPresentation(forceTextRefresh: true);
	}

	public void SetProgressTextVisible(bool visible)
	{
		showProgressText = visible;
		RefreshProgressPresentation(forceTextRefresh: true);
	}

	public void FlagRefresh()
	{
		if (hideItem || _flagContainer == null)
		{
			return;
		}
		int num = Mathf.FloorToInt(waveNum / waveInterval);
		if (num <= 0)
		{
			_ClearFlags();
			return;
		}
		if (_renderedFlagWaveNum == waveNum && _renderedFlagWaveInterval == waveInterval && flagList.Count == num)
		{
			RefreshFlagReachState();
			return;
		}
		_ClearFlags();
		_renderedFlagWaveNum = waveNum;
		_renderedFlagWaveInterval = waveInterval;
		_flagContainer.AddThemeConstantOverride("separation", Mathf.Max(0, Mathf.FloorToInt(150f / (float)num)));
		for (int i = 0; i < num; i++)
		{
			GeneralProgressFlag generalProgressFlag = GENERAL_PROGRESS_FLAG.Instantiate<GeneralProgressFlag>(PackedScene.GenEditState.Disabled);
			_flagContainer.AddChild(generalProgressFlag, forceReadableName: false, InternalMode.Disabled);
			flagList.Insert(0, generalProgressFlag);
		}
		RefreshFlagReachState();
	}

	private void RefreshFlagReachState()
	{
		int num = Mathf.Min(previewWave / waveInterval, flagList.Count);
		for (int i = 0; i < flagList.Count; i++)
		{
			flagList[i].SetReach(i < num);
		}
	}

	private void _ClearFlags()
	{
		if (_flagContainer == null)
		{
			return;
		}
		foreach (GeneralProgressFlag flag in flagList)
		{
			if (GodotObject.IsInstanceValid(flag) && !flag.IsQueuedForDeletion())
			{
				if (flag.GetParent() == _flagContainer)
				{
					_flagContainer.RemoveChild(flag);
				}
				flag.QueueFree();
			}
		}
		flagList.Clear();
		_renderedFlagWaveNum = -1;
		_renderedFlagWaveInterval = -1;
	}

	private void RefreshPhysicsProcessing()
	{
		SetPhysicsProcess(!Engine.IsEditorHint() && IsVisibleInTree() && !manualProgressValue && _autoProgressResponse > 0.0);
	}

	private void RefreshProgressPresentation(bool forceTextRefresh = false)
	{
		if (_headTexture != null)
		{
			_headTexture.Visible = !hideItem && !manualProgressValue;
			if (_progressBar != null && _progressBar.MaxValue > 0.0)
			{
				_headTexture.Position = new Vector2((float)(140.0 - 150.0 * _progressBar.Value / _progressBar.MaxValue), _headTexture.Position.Y);
			}
		}
		RefreshProgressText(forceTextRefresh);
	}

	private void RefreshProgressText(bool forceRefresh)
	{
		if (_progressTextLabel == null)
		{
			return;
		}
		_progressTextLabel.Visible = showProgressText;
		if (!showProgressText || _progressBar == null)
		{
			return;
		}
		ulong ticksMsec = Time.GetTicksMsec();
		if (forceRefresh || manualProgressValue || ticksMsec >= _nextAutoTextRefreshMsec)
		{
			if (!manualProgressValue)
			{
				_nextAutoTextRefreshMsec = ticksMsec + 100;
			}
			double num = ((_progressBar.MaxValue > 0.0) ? _progressBar.MaxValue : 1.0);
			bool flag = _progressTextTemplate.Contains("{value}", StringComparison.Ordinal);
			bool flag2 = _progressTextTemplate.Contains("{max}", StringComparison.Ordinal);
			bool flag3 = _progressTextTemplate.Contains("{percent}", StringComparison.Ordinal);
			long num2 = (flag ? ((long)Math.Round(_progressBar.Value * 100.0, MidpointRounding.AwayFromZero)) : 0);
			long num3 = (flag2 ? ((long)Math.Round(num * 100.0, MidpointRounding.AwayFromZero)) : 0);
			long num4 = (flag3 ? ((long)Math.Round(_progressBar.Value / num * 10000.0, MidpointRounding.AwayFromZero)) : 0);
			if (num2 != _lastTextValueUnits || num3 != _lastTextMaxUnits || num4 != _lastTextPercentUnits || !string.Equals(_progressTextTemplate, _lastRenderedTemplate, StringComparison.Ordinal))
			{
				_lastTextValueUnits = num2;
				_lastTextMaxUnits = num3;
				_lastTextPercentUnits = num4;
				_lastRenderedTemplate = _progressTextTemplate;
				string newValue = (flag ? ((double)num2 / 100.0).ToString("0.##", CultureInfo.InvariantCulture) : "");
				string newValue2 = (flag2 ? ((double)num3 / 100.0).ToString("0.##", CultureInfo.InvariantCulture) : "");
				string newValue3 = (flag3 ? ((double)num4 / 100.0).ToString("0.##", CultureInfo.InvariantCulture) : "");
				_progressTextLabel.Text = _progressTextTemplate.Replace("{value}", newValue, StringComparison.Ordinal).Replace("{max}", newValue2, StringComparison.Ordinal).Replace("{percent}", newValue3, StringComparison.Ordinal);
			}
		}
	}

	public override void _Ready()
	{
		_progressBar = GetNode<TextureProgressBar>("%ProgressBar");
		_headTexture = GetNode<TextureRect>("%HeadTexture");
		_flagContainer = GetNode<HBoxContainer>("%FlagContainer");
		_progressTextLabel = GetNodeOrNull<Label>("%ProgressTextLabel");
		_progressBar.ValueChanged += OnProgressValueChanged;
		base.VisibilityChanged += RefreshPhysicsProcessing;
		if (_hasInitConfig)
		{
			FlagRefresh();
		}
		RefreshProgressPresentation(forceTextRefresh: true);
		RefreshPhysicsProcessing();
	}

	private void OnProgressValueChanged(double value)
	{
		RefreshProgressPresentation();
	}
}
