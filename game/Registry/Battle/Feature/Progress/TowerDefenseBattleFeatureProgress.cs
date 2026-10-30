using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureProgress : TowerDefenseBattleFeature
{
	private static PackedScene _towerDefenseProgressManager;

	public TowerDefenseProgressManager progressManager;

	public TowerDefenseBattleFeatureProgressConfig config;

	private string _configuredLevelName = "";

	private string _configuredDifficultText = "";

	private string _configuredSurvivalText = "";

	private string _configuredProgressText = "{value}/{max}";

	private bool _hasConfiguredProgressText;

	private bool _manualProgress;

	private bool _hasHideProgressItems;

	private bool _hideProgressItems;

	private double _initialProgressValue;

	private double _initialProgressMax = 1.0;

	private bool _hasDifficultVisibility;

	private bool _difficultVisible;

	private bool _singleDifficultyLevel;

	private bool _hasLevelNameVisibility;

	private bool _levelNameVisible;

	private bool _hasSurvivalVisibility;

	private bool _survivalVisible;

	private bool _hasProgressVisibility;

	private bool _progressVisible;

	private bool _hasProgressTextVisibility;

	private bool _progressTextVisible;

	private string _progressTextSource = "{value}/{max}";

	private bool _translateProgressTextSource;

	private bool _runtimeCustomProgress;

	private static PackedScene TOWER_DEFENSE_PROGRESS_MANAGER => _towerDefenseProgressManager ?? (_towerDefenseProgressManager = GD.Load<PackedScene>("uid://dlgafsqc16cbw"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		progressManager = TOWER_DEFENSE_PROGRESS_MANAGER.Instantiate<TowerDefenseProgressManager>(PackedScene.GenEditState.Disabled);
		progressManager.progressFeature = this;
		control.AddUI(progressManager, 3);
		ReadConfiguration();
		_singleDifficultyLevel = HasSingleConfiguredDifficulty();
		ApplyConfiguredPresentation();
	}

	private void ReadConfiguration()
	{
		config = new TowerDefenseBattleFeatureProgressConfig();
		config.Init(data);
		_configuredLevelName = ResolveConfiguredText(config.levelName);
		_configuredDifficultText = ResolveConfiguredText(config.difficultyText);
		_configuredSurvivalText = ResolveConfiguredText(config.survivalText);
		_hasConfiguredProgressText = config.HasProgressText;
		_progressTextSource = config.progressText;
		_translateProgressTextSource = config.HasProgressText;
		_configuredProgressText = ResolveConfiguredText(config.progressText);
		_manualProgress = config.mode == TowerDefenseProgressMode.Manual;
		_hasHideProgressItems = config.HasHideProgressItems;
		_hideProgressItems = config.HideProgressItems;
		_initialProgressValue = config.progressValue;
		_initialProgressMax = config.progressMax;
		_hasDifficultVisibility = TowerDefenseBattleFeatureProgressConfig.TryGetVisibility(config.difficultyVisibility, out _difficultVisible);
		_hasLevelNameVisibility = TowerDefenseBattleFeatureProgressConfig.TryGetVisibility(config.levelNameVisibility, out _levelNameVisible);
		_hasSurvivalVisibility = TowerDefenseBattleFeatureProgressConfig.TryGetVisibility(config.survivalVisibility, out _survivalVisible);
		_hasProgressVisibility = TowerDefenseBattleFeatureProgressConfig.TryGetVisibility(config.progressVisibility, out _progressVisible);
		_hasProgressTextVisibility = TowerDefenseBattleFeatureProgressConfig.TryGetVisibility(config.progressTextVisibility, out _progressTextVisible);
		if (!_hasProgressTextVisibility && (_hasConfiguredProgressText || _manualProgress))
		{
			_hasProgressTextVisibility = true;
			_progressTextVisible = true;
		}
	}

	private string ResolveConfiguredText(string value)
	{
		if (!(value == ""))
		{
			return Tr(value);
		}
		return "";
	}

	private void ApplyConfiguredPresentation()
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			if (_configuredLevelName != "")
			{
				progressManager.levelNameLabel.Text = _configuredLevelName;
			}
			if (_configuredDifficultText != "")
			{
				progressManager.difficultLabel.Text = _configuredDifficultText;
			}
			if (_configuredSurvivalText != "")
			{
				progressManager.survivalLabel.Text = FormatRoundText(_configuredSurvivalText, 0);
			}
			if (_manualProgress && !progressManager.progressMeter.manualProgressValue)
			{
				progressManager.progressMeter.SetManualProgress(_initialProgressValue, _initialProgressMax, _configuredProgressText);
			}
			else if (_hasConfiguredProgressText)
			{
				progressManager.progressMeter.SetProgressText(_configuredProgressText);
			}
			if (_hasHideProgressItems)
			{
				progressManager.progressMeter.SetHideItem(_hideProgressItems);
			}
			if (_hasDifficultVisibility || _singleDifficultyLevel)
			{
				SetDifficultVisible(progressManager.difficultLabel.Visible);
			}
			if (_hasLevelNameVisibility)
			{
				progressManager.levelNameLabel.Visible = _levelNameVisible;
			}
			if (_hasSurvivalVisibility)
			{
				progressManager.survivalLabel.Visible = _survivalVisible;
			}
			if (_hasProgressVisibility)
			{
				progressManager.progressMeter.Visible = _progressVisible;
			}
			if (_hasProgressTextVisibility)
			{
				progressManager.progressMeter.SetProgressTextVisible(_progressTextVisible);
			}
		}
	}

	private static string FormatRoundText(string template, int round)
	{
		return (template ?? "").Replace("{round}", round.ToString()).Replace("%d", round.ToString());
	}

	public override Task GameReady()
	{
		ConfigureAutoProgressResponse();
		ApplyConfiguredPresentation();
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (!GodotObject.IsInstanceValid(progressManager))
		{
			return Task.CompletedTask;
		}
		ConfigureAutoProgressResponse();
		if (GetProcess() is TowerDefenseBattleProcessIZM)
		{
			progressManager.progressMeter.SetHideItem(hide: true);
			progressManager.progressMeter.Visible = true;
		}
		ApplyConfiguredPresentation();
		return Task.CompletedTask;
	}

	private void ConfigureAutoProgressResponse()
	{
		if (GodotObject.IsInstanceValid(progressManager) && GodotObject.IsInstanceValid(progressManager.progressMeter))
		{
			double autoProgressResponse = ((config != null && config.autoProgressResponse >= 0.0) ? config.autoProgressResponse : ((GetProcess() is TowerDefenseBattleProcessIZM) ? 2.0 : 0.1));
			progressManager.progressMeter.SetAutoProgressResponse(autoProgressResponse);
		}
	}

	public void ProgressInit(int waveNum, int flagWaveInterval)
	{
		ConfigureWaveProgress(waveNum, flagWaveInterval, 0);
	}

	public void ConfigureWaveProgress(int waveNum, int flagWaveInterval, int currentWave)
	{
		if (GodotObject.IsInstanceValid(progressManager) && GodotObject.IsInstanceValid(progressManager.progressMeter) && !progressManager.progressMeter.manualProgressValue)
		{
			if (_manualProgress)
			{
				progressManager.progressMeter.SetManualProgress(_initialProgressValue, _initialProgressMax, _configuredProgressText);
				return;
			}
			progressManager.progressMeter.SetManualProgressEnabled(enabled: false);
			progressManager.progressMeter.Init(waveNum, flagWaveInterval);
			progressManager.progressMeter.SetWaveCurrent(currentWave);
		}
	}

	public void ProgressRefresh(bool isSurvival = false, int survivalRoundNum = 0)
	{
		if (!GodotObject.IsInstanceValid(progressManager))
		{
			return;
		}
		if (progressManager.progressMeter.manualProgressValue || _manualProgress)
		{
			if (!progressManager.progressMeter.manualProgressValue)
			{
				progressManager.progressMeter.SetManualProgress(_initialProgressValue, _initialProgressMax, _configuredProgressText);
			}
			SetProgressMeterVisible(visible: true);
			return;
		}
		progressManager.progressMeter.SetManualProgressEnabled(enabled: false);
		progressManager.progressMeter.Visible = false;
		progressManager.progressMeter.SetWaveCurrent(0);
		progressManager.progressBar.Value = 0.0;
		if (isSurvival)
		{
			SetSurvivalText(survivalRoundNum);
		}
	}

	public void SetupUI(string difficult, string levelName, bool isSurvival = false, int survivalRoundNum = 0)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.SetupUI(difficult, levelName, isSurvival, survivalRoundNum);
			if (_configuredLevelName != "")
			{
				progressManager.levelNameLabel.Text = _configuredLevelName;
			}
			if (_configuredDifficultText != "")
			{
				progressManager.difficultLabel.Text = _configuredDifficultText;
			}
			if (_configuredSurvivalText != "")
			{
				progressManager.survivalLabel.Text = FormatRoundText(_configuredSurvivalText, survivalRoundNum);
			}
		}
	}

	public void SetDifficultModulate(string difficult)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			switch (difficult)
			{
			case "Normal":
				progressManager.difficultLabel.Modulate = new Color(progressManager.difficultLabel.Modulate.R, 1f, progressManager.difficultLabel.Modulate.B, progressManager.difficultLabel.Modulate.A);
				break;
			case "Difficult":
				progressManager.difficultLabel.Modulate = new Color(progressManager.difficultLabel.Modulate.R, 0f, progressManager.difficultLabel.Modulate.B, progressManager.difficultLabel.Modulate.A);
				break;
			case "Ultimate":
				progressManager.difficultLabel.Modulate = new Color(progressManager.difficultLabel.Modulate.R, 0f, progressManager.difficultLabel.Modulate.B, progressManager.difficultLabel.Modulate.A);
				break;
			}
		}
	}

	public void SetDifficultText(string difficultString)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.difficultLabel.Text = ((_configuredDifficultText != "") ? _configuredDifficultText.Replace("{difficulty}", difficultString).Replace("%s", difficultString) : Tr("INGAME_DIFFICULT").Replace("%s", difficultString));
		}
	}

	public void SetLevelName(string levelName)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.levelNameLabel.Text = ((_configuredLevelName != "") ? _configuredLevelName : levelName);
		}
	}

	public void SetSurvivalText(int survivalRoundNum)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.survivalLabel.Text = ((_configuredSurvivalText != "") ? FormatRoundText(_configuredSurvivalText, survivalRoundNum) : Tr("PLAYERS_SURVIVAL_LEVEL_DESCRIBE").Replace("%d", survivalRoundNum.ToString()));
		}
	}

	public void SetDifficultVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.difficultLabel.Visible = (_hasDifficultVisibility ? _difficultVisible : (visible && !_singleDifficultyLevel));
		}
	}

	private static bool HasSingleConfiguredDifficulty()
	{
		TowerDefenseLevelBaseConfig towerDefenseLevelBaseConfig = TowerDefenseManager.Instance?.currentLevelConfig;
		if (!GodotObject.IsInstanceValid(towerDefenseLevelBaseConfig) || string.IsNullOrEmpty(towerDefenseLevelBaseConfig.name) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			return false;
		}
		foreach (Dictionary value3 in ResourceManager.Instance.LEVELS.Values)
		{
			if (!value3.TryGetValue("Chapter", out var value))
			{
				continue;
			}
			foreach (Variant item in value.AsGodotArray())
			{
				if (item.AsGodotDictionary().TryGetValue("Level", out var value2))
				{
					bool? flag = FindSingleDifficultyLevel(value2.AsGodotArray(), towerDefenseLevelBaseConfig.name);
					if (flag.HasValue)
					{
						return flag.Value;
					}
				}
			}
		}
		if (Global.Instance?.currentLevelChoose == "TryLevel")
		{
			foreach (Variant value4 in GD.Load<Json>("res://Asset/Config/Level/TryLevelResource.json").Data.AsGodotDictionary().Values)
			{
				bool? flag2 = FindSingleDifficultyLevel(value4.AsGodotArray(), towerDefenseLevelBaseConfig.name);
				if (flag2.HasValue)
				{
					return flag2.Value;
				}
			}
		}
		return false;
	}

	private static bool? FindSingleDifficultyLevel(Array levels, string levelName)
	{
		foreach (Variant level in levels)
		{
			Dictionary dictionary = level.AsGodotDictionary();
			if (!(dictionary.GetValueOrDefault("SaveKey", "").AsString() != levelName) && dictionary.TryGetValue("Level", out var value))
			{
				Dictionary dictionary2 = value.AsGodotDictionary();
				int num = 0;
				if (!string.IsNullOrEmpty(dictionary2.GetValueOrDefault("Normal", "").AsString()))
				{
					num++;
				}
				if (!string.IsNullOrEmpty(dictionary2.GetValueOrDefault("Difficult", "").AsString()))
				{
					num++;
				}
				if (!string.IsNullOrEmpty(dictionary2.GetValueOrDefault("Ultimate", "").AsString()))
				{
					num++;
				}
				return num == 1;
			}
		}
		return null;
	}

	public void SetSurvivalVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.survivalLabel.Visible = (_hasSurvivalVisibility ? _survivalVisible : visible);
		}
	}

	public void SetLevelNameVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.levelNameLabel.Visible = (_hasLevelNameVisibility ? _levelNameVisible : visible);
		}
	}

	public void SetProgressMeterVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.Visible = (_hasProgressVisibility ? _progressVisible : visible);
		}
	}

	public void SetProgressMeterHideItem(bool hide)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.SetHideItem(_hasHideProgressItems ? _hideProgressItems : hide);
		}
	}

	public void SetProgressMeterWaveCurrent(int waveCurrent)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.SetWaveCurrent(waveCurrent);
		}
	}

	public void SetProgressMeterMaxValue(double maxValue)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.SetProgressMaxValue(maxValue);
		}
	}

	public void SetProgressMeterWaveNum(int waveNum)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.waveNum = waveNum;
		}
	}

	public void SetProgressMeterPreviewWave(int previewWave)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.previewWave = previewWave;
		}
	}

	public void SetProgressMeterValue(double value)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.SetProgressValue(value);
		}
	}

	public void SetProgressMeterManualProgress(double value, double maxValue, string text)
	{
		SetCustomProgress(value, maxValue, text, null, null);
	}

	public void SetCustomProgress(double value, double maxValue, string text, bool? visible = true, bool? hideItems = true)
	{
		if (GodotObject.IsInstanceValid(progressManager) && GodotObject.IsInstanceValid(progressManager.progressMeter))
		{
			_runtimeCustomProgress = true;
			_progressTextSource = text ?? "";
			_translateProgressTextSource = false;
			progressManager.progressMeter.SetManualProgress(value, maxValue, text);
			if (visible.HasValue)
			{
				progressManager.progressMeter.Visible = visible.Value;
			}
			if (hideItems.HasValue)
			{
				progressManager.progressMeter.SetHideItem(hideItems.Value);
			}
		}
	}

	public void ReleaseCustomProgress()
	{
		if (!_runtimeCustomProgress || !GodotObject.IsInstanceValid(progressManager) || !GodotObject.IsInstanceValid(progressManager.progressMeter))
		{
			return;
		}
		_runtimeCustomProgress = false;
		if (_manualProgress)
		{
			_progressTextSource = config?.progressText ?? "{value}/{max}";
			_translateProgressTextSource = config?.HasProgressText ?? false;
			progressManager.progressMeter.SetManualProgress(_initialProgressValue, _initialProgressMax, _configuredProgressText);
			return;
		}
		progressManager.progressMeter.SetManualProgressEnabled(enabled: false);
		if (_hasHideProgressItems)
		{
			progressManager.progressMeter.SetHideItem(_hideProgressItems);
		}
		else
		{
			progressManager.progressMeter.SetHideItem(hide: false);
		}
		ConfigureAutoProgressResponse();
	}

	public void SetProgressMeterText(string text)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			_progressTextSource = text ?? "";
			_translateProgressTextSource = false;
			progressManager.progressMeter.SetProgressText(text);
			progressManager.progressMeter.SetProgressTextVisible(visible: true);
		}
	}

	public void SetProgressMeterTextVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.SetProgressTextVisible(visible);
		}
	}

	public void IncrementPreviewWave()
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressMeter.previewWave++;
		}
	}

	public override Dictionary SaveFeature()
	{
		return SerializeState(includeLocalizedText: true);
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		DeserializeState(_data);
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeState(includeLocalizedText: false);
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		DeserializeState(_data);
	}

	private Dictionary SerializeState(bool includeLocalizedText)
	{
		Dictionary dictionary = new Dictionary();
		if (!IsProgressReady())
		{
			return dictionary;
		}
		if (includeLocalizedText)
		{
			dictionary["level_name"] = progressManager.levelNameLabel.Text;
			dictionary["difficult_text"] = progressManager.difficultLabel.Text;
			dictionary["survival_text"] = progressManager.survivalLabel.Text;
		}
		bool manualProgressValue = progressManager.progressMeter.manualProgressValue;
		if (includeLocalizedText || manualProgressValue)
		{
			dictionary["progress_value"] = progressManager.progressBar.Value;
		}
		dictionary["progress_max"] = progressManager.progressBar.MaxValue;
		dictionary["manual_progress"] = manualProgressValue;
		dictionary["runtime_custom_progress"] = _runtimeCustomProgress;
		dictionary["progress_text_source"] = _progressTextSource;
		dictionary["translate_progress_text"] = _translateProgressTextSource;
		if (includeLocalizedText)
		{
			dictionary["progress_text"] = progressManager.progressMeter.ManualProgressTextTemplate;
		}
		dictionary["show_progress_text"] = progressManager.progressMeter.showProgressText;
		dictionary["wave_num"] = progressManager.progressMeter.waveNum;
		dictionary["wave_interval"] = progressManager.progressMeter.waveInterval;
		dictionary["preview_wave"] = progressManager.progressMeter.previewWave;
		dictionary["hide_progress_items"] = progressManager.progressMeter.hideItem;
		dictionary["show_difficult"] = progressManager.difficultLabel.Visible;
		dictionary["show_level_name"] = progressManager.levelNameLabel.Visible;
		dictionary["show_survival"] = progressManager.survivalLabel.Visible;
		dictionary["show_progress"] = progressManager.progressMeter.Visible;
		return dictionary;
	}

	private void DeserializeState(Dictionary state)
	{
		if (!IsProgressReady())
		{
			return;
		}
		if (state.ContainsKey("level_name"))
		{
			progressManager.levelNameLabel.Text = state["level_name"].AsString();
		}
		if (state.ContainsKey("difficult_text"))
		{
			progressManager.difficultLabel.Text = state["difficult_text"].AsString();
		}
		if (state.ContainsKey("survival_text"))
		{
			progressManager.survivalLabel.Text = state["survival_text"].AsString();
		}
		progressManager.progressMeter.waveNum = state.GetValueOrDefault("wave_num", progressManager.progressMeter.waveNum).AsInt32();
		progressManager.progressMeter.waveInterval = state.GetValueOrDefault("wave_interval", progressManager.progressMeter.waveInterval).AsInt32();
		progressManager.progressMeter.previewWave = state.GetValueOrDefault("preview_wave", progressManager.progressMeter.previewWave).AsInt32();
		progressManager.progressMeter.SetHideItem(state.GetValueOrDefault("hide_progress_items", progressManager.progressMeter.hideItem).AsBool());
		double num = state.GetValueOrDefault("progress_value", progressManager.progressBar.Value).AsDouble();
		double num2 = state.GetValueOrDefault("progress_max", progressManager.progressBar.MaxValue).AsDouble();
		_progressTextSource = state.GetValueOrDefault("progress_text_source", state.GetValueOrDefault("progress_text", _progressTextSource)).AsString();
		_translateProgressTextSource = state.GetValueOrDefault("translate_progress_text", false).AsBool();
		_runtimeCustomProgress = state.GetValueOrDefault("runtime_custom_progress", false).AsBool();
		string text = (_translateProgressTextSource ? Tr(_progressTextSource) : _progressTextSource);
		if (state.GetValueOrDefault("manual_progress", progressManager.progressMeter.manualProgressValue).AsBool())
		{
			progressManager.progressMeter.SetManualProgress(num, num2, text);
		}
		else
		{
			progressManager.progressMeter.SetManualProgressEnabled(enabled: false);
			if (state.ContainsKey("progress_text_source") || state.ContainsKey("progress_text"))
			{
				progressManager.progressMeter.SetProgressText(text);
			}
			progressManager.progressMeter.SetProgressMaxValue(num2);
			if (state.ContainsKey("progress_value"))
			{
				progressManager.progressMeter.SetProgressValue(num);
			}
		}
		progressManager.progressMeter.SetProgressTextVisible(state.GetValueOrDefault("show_progress_text", progressManager.progressMeter.showProgressText).AsBool());
		SetDifficultVisible(state.GetValueOrDefault("show_difficult", progressManager.difficultLabel.Visible).AsBool());
		SetLevelNameVisible(state.GetValueOrDefault("show_level_name", progressManager.levelNameLabel.Visible).AsBool());
		SetSurvivalVisible(state.GetValueOrDefault("show_survival", progressManager.survivalLabel.Visible).AsBool());
		SetProgressMeterVisible(state.GetValueOrDefault("show_progress", progressManager.progressMeter.Visible).AsBool());
	}

	private bool IsProgressReady()
	{
		if (GodotObject.IsInstanceValid(progressManager) && GodotObject.IsInstanceValid(progressManager.progressMeter))
		{
			return GodotObject.IsInstanceValid(progressManager.progressBar);
		}
		return false;
	}

	public override void Destroy()
	{
		if (GodotObject.IsInstanceValid(progressManager))
		{
			progressManager.progressFeature = null;
			progressManager.QueueFree();
		}
		progressManager = null;
		base.Destroy();
	}
}
