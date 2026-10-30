using System.Collections.Generic;
using Godot;

public partial class CommandManager : Control
{
	private const string ImportLevelJsonTempPath = "user://Csharp/import_level.json";

	private const string ImportLevelTresTempPath = "user://Csharp/import_level.tres";

	private const double DebugStatsRefreshInterval = 0.2;

	private double _debugStatsRefreshRemaining;

	private MainButton _openButton;

	private CanvasLayer _guiLayer;

	private CheckBox _openAllLevelCheckBox;

	private CheckBox _coinMaxCheckBox;

	private CheckBox _sunMaxCheckBox;

	private CheckBox _packetSelectCheckBox;

	private CheckBox _packetOpenAllCheckBox;

	private CheckBox _packetColdDownCheckBox;

	private CheckBox _openAllCustomCheckBox;

	private CheckBox _openGloveCheckBox;

	private CheckBox _unlimitedFireCheckBox;

	private CheckBox _plantInvincibleCheckBox;

	private CheckBox _noLoseCheckBox;

	private SpinBox _sunSpinBox;

	private SpinBox _coinSpinBox;

	private SpinBox _crystalSpinBox;

	private CheckBox _wavePausedCheckBox;

	private CheckBox _noZombieSpawnCheckBox;

	private SpinBox _waveSpinBox;

	private CheckBox _brainInvincibleCheckBox;

	private Label _gameSpeedLabel;

	private HSlider _gameSpeedSlider;

	private Label _fpsLabel;

	private Label _waveInfoLabel;

	private Label _characterCountLabel;

	private Label _bulletCountLabel;

	[Export(PropertyHint.None, "")]
	public bool debug;

	[Export(PropertyHint.None, "")]
	public bool debugUnlimitedFire;

	private bool _debugCoinMax;

	[Export(PropertyHint.None, "")]
	public bool debugOpenAllLevel;

	[Export(PropertyHint.None, "")]
	public bool debugSunMax;

	[Export(PropertyHint.None, "")]
	public bool debugPacketSelect;

	[Export(PropertyHint.None, "")]
	public bool debugPacketOpenAll;

	[Export(PropertyHint.None, "")]
	public bool debugPacketColdDown;

	[Export(PropertyHint.None, "")]
	public bool debugOpenAllCustom;

	[Export(PropertyHint.None, "")]
	public bool debugOpenGlove;

	[Export(PropertyHint.None, "")]
	public bool debugPlantInvincible;

	[Export(PropertyHint.None, "")]
	public bool debugNoLose;

	[Export(PropertyHint.None, "")]
	public bool debugWavePaused;

	[Export(PropertyHint.None, "")]
	public bool debugNoZombieSpawn;

	[Export(PropertyHint.None, "")]
	public bool debugBrainInvincible;

	public static CommandManager Instance;

	[Export(PropertyHint.None, "")]
	public bool debugCoinMax
	{
		get
		{
			return _debugCoinMax;
		}
		set
		{
			_debugCoinMax = value;
			if (value && GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.coinBank))
			{
				TowerDefenseManager.Instance.coinBank.SetNum(999999999L);
			}
		}
	}

	public override void _Ready()
	{
		Instance = this;
		if (debugCoinMax && GodotObject.IsInstanceValid(TowerDefenseManager.Instance?.coinBank))
		{
			TowerDefenseManager.Instance.coinBank.SetNum(999999999L);
		}
		_openButton = GetNode<MainButton>("%OpenButton");
		_guiLayer = GetNode<CanvasLayer>("GUILayer");
		_openAllLevelCheckBox = GetNode<CheckBox>("%OpenAllLevelCheckBox");
		_coinMaxCheckBox = GetNode<CheckBox>("%CoinMaxCheckBox");
		_sunMaxCheckBox = GetNode<CheckBox>("%SunMaxCheckBox");
		_packetSelectCheckBox = GetNode<CheckBox>("%PacketSelectCheckBox");
		_packetOpenAllCheckBox = GetNode<CheckBox>("%PacketOpenAllCheckBox");
		_packetColdDownCheckBox = GetNode<CheckBox>("%PacketColdDownCheckBox");
		_openAllCustomCheckBox = GetNode<CheckBox>("%OpenAllCustomCheckBox");
		_openGloveCheckBox = GetNode<CheckBox>("%OpenGloveCheckBox");
		_unlimitedFireCheckBox = GetNode<CheckBox>("%UnlimitedFireCheckBox");
		_plantInvincibleCheckBox = GetNode<CheckBox>("%PlantInvincibleCheckBox");
		_noLoseCheckBox = GetNode<CheckBox>("%NoLoseCheckBox");
		_sunSpinBox = GetNode<SpinBox>("%SunSpinBox");
		_coinSpinBox = GetNode<SpinBox>("%CoinSpinBox");
		_crystalSpinBox = GetNode<SpinBox>("%CrystalSpinBox");
		_wavePausedCheckBox = GetNode<CheckBox>("%WavePausedCheckBox");
		_noZombieSpawnCheckBox = GetNode<CheckBox>("%NoZombieSpawnCheckBox");
		_waveSpinBox = GetNode<SpinBox>("%WaveSpinBox");
		_brainInvincibleCheckBox = GetNode<CheckBox>("%BrainInvincibleCheckBox");
		_gameSpeedLabel = GetNode<Label>("%GameSpeedLabel");
		_gameSpeedSlider = GetNode<HSlider>("%GameSpeedSlider");
		_fpsLabel = GetNode<Label>("%FpsLabel");
		_waveInfoLabel = GetNode<Label>("%WaveInfoLabel");
		_characterCountLabel = GetNode<Label>("%CharacterCountLabel");
		_bulletCountLabel = GetNode<Label>("%BulletCountLabel");
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/游戏内/VBox/SunHBox/SunSetButton").Pressed += SetSunValue;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/游戏内/VBox/CoinHBox/CoinSetButton").Pressed += SetCoinValue;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/游戏内/VBox/CrystalHBox/CrystalSetButton").Pressed += SetCrystalValue;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/战斗/VBox/WaveHBox/WaveButton").Pressed += SkipToWave;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/战斗/VBox/SkipWaitButton").Pressed += SkipWaveWait;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/战斗/VBox/FinalWaveButton").Pressed += SkipToFinalWave;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/战斗/VBox/KillAllButton").Pressed += KillAllZombies;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/战斗/VBox/InstantWinButton").Pressed += InstantWin;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/场景/VBox/RestoreMowerButton").Pressed += RestoreAllMowers;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/场景/VBox/RemoveMowerButton").Pressed += RemoveAllMowers;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/场景/VBox/ResetBrainButton").Pressed += ResetAllBrains;
		GetNode<BaseButton>("GUILayer/CommandContainer/TabContainer/速度/VBox/SpeedResetButton").Pressed += _on_speed_reset_button_pressed;
		GetNode<BaseButton>("GUILayer/TestLevelButton").Pressed += TestLevelButtonPressed;
		GetNode<BaseButton>("GUILayer/LoadLevelButton").Pressed += LoadLevelButtonPressed;
		GetNode<BaseButton>("%OpenButton").Toggled += OpenButtonToggled;
		if (!debug)
		{
			_openButton.Visible = false;
			base.ProcessMode = ProcessModeEnum.Disabled;
		}
		else
		{
			Init();
			SetProcess(_guiLayer.Visible);
		}
	}

	public override void _Input(InputEvent _event)
	{
		if (debug && Input.IsActionJustPressed("Command"))
		{
			GrabFocus();
			SetCommandLayerVisible(!_guiLayer.Visible);
		}
	}

	public override void _Process(double delta)
	{
		if (!debug || !_guiLayer.Visible)
		{
			return;
		}
		_debugStatsRefreshRemaining -= delta;
		if (_debugStatsRefreshRemaining > 0.0)
		{
			return;
		}
		_debugStatsRefreshRemaining = 0.2;
		_fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
		if (GodotObject.IsInstanceValid(TowerDefenseBattleFeatureWave.Instance))
		{
			TowerDefenseBattleFeatureWave instance = TowerDefenseBattleFeatureWave.Instance;
			int num = ((instance.config != null) ? instance.config.wave.Count : 0);
			_waveInfoLabel.Text = $"波次: {instance.currentWave}/{num}\n血量: {instance.currentHpPoint:F0}/{instance.currentHpPointTotal:F0}";
			if (_waveSpinBox.MaxValue != (double)num)
			{
				_waveSpinBox.MaxValue = Mathf.Max(1, num);
			}
		}
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = ((TowerDefenseManager.Instance != null) ? TowerDefenseManager.Instance.characterRegistry : null);
		if (GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry))
		{
			int num2 = 0;
			int num3 = 0;
			List<TowerDefenseCharacter> cleanCharactersList = towerDefenseBattleCharacterRegistry.GetCleanCharactersList();
			for (int i = 0; i < cleanCharactersList.Count; i++)
			{
				if (cleanCharactersList[i] is TowerDefenseZombie)
				{
					num3++;
				}
				else if (cleanCharactersList[i] is TowerDefensePlant)
				{
					num2++;
				}
			}
			_characterCountLabel.Text = $"植物: {num2}  僵尸: {num3}  总数: {num2 + num3}";
		}
		int num4 = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance.ActiveCount : 0);
		int nodeCountInGroup = GetTree().GetNodeCountInGroup("Projectile");
		_bulletCountLabel.Text = $"子弹: {num4 + nodeCountInGroup}  (场内:{nodeCountInGroup} 缓存场:{num4})";
	}

	public void Init()
	{
		_openAllLevelCheckBox.ButtonPressed = debugOpenAllLevel;
		_openAllLevelCheckBox.Toggled += delegate(bool toggle)
		{
			debugOpenAllLevel = toggle;
		};
		_coinMaxCheckBox.ButtonPressed = debugCoinMax;
		_coinMaxCheckBox.Toggled += delegate(bool toggle)
		{
			debugCoinMax = toggle;
		};
		_sunMaxCheckBox.ButtonPressed = debugSunMax;
		_sunMaxCheckBox.Toggled += delegate(bool toggle)
		{
			debugSunMax = toggle;
		};
		_packetSelectCheckBox.ButtonPressed = debugPacketSelect;
		_packetSelectCheckBox.Toggled += delegate(bool toggle)
		{
			debugPacketSelect = toggle;
		};
		_packetOpenAllCheckBox.ButtonPressed = debugPacketOpenAll;
		_packetOpenAllCheckBox.Toggled += delegate(bool toggle)
		{
			debugPacketOpenAll = toggle;
		};
		_packetColdDownCheckBox.ButtonPressed = debugPacketColdDown;
		_packetColdDownCheckBox.Toggled += delegate(bool toggle)
		{
			debugPacketColdDown = toggle;
		};
		_openAllCustomCheckBox.ButtonPressed = debugOpenAllCustom;
		_openAllCustomCheckBox.Toggled += delegate(bool toggle)
		{
			debugOpenAllCustom = toggle;
		};
		_openGloveCheckBox.ButtonPressed = GameSaveManager.Instance.GetFeatureValue("Glove") > 0;
		_openGloveCheckBox.Toggled += delegate(bool toggle)
		{
			Instance.debugOpenGlove = toggle;
		};
		_unlimitedFireCheckBox.ButtonPressed = debugUnlimitedFire;
		_unlimitedFireCheckBox.Toggled += delegate(bool toggle)
		{
			debugUnlimitedFire = toggle;
		};
		_plantInvincibleCheckBox.ButtonPressed = debugPlantInvincible;
		_plantInvincibleCheckBox.Toggled += delegate(bool toggle)
		{
			debugPlantInvincible = toggle;
		};
		_noLoseCheckBox.ButtonPressed = debugNoLose;
		_noLoseCheckBox.Toggled += delegate(bool toggle)
		{
			debugNoLose = toggle;
		};
		_wavePausedCheckBox.ButtonPressed = debugWavePaused;
		_wavePausedCheckBox.Toggled += delegate(bool toggle)
		{
			debugWavePaused = toggle;
		};
		_noZombieSpawnCheckBox.ButtonPressed = debugNoZombieSpawn;
		_noZombieSpawnCheckBox.Toggled += delegate(bool toggle)
		{
			debugNoZombieSpawn = toggle;
		};
		_brainInvincibleCheckBox.ButtonPressed = debugBrainInvincible;
		_brainInvincibleCheckBox.Toggled += delegate(bool toggle)
		{
			debugBrainInvincible = toggle;
		};
		_gameSpeedSlider.ValueChanged += delegate(double value)
		{
			Global.TimeScale = value;
			_gameSpeedLabel.Text = $"游戏速度:{value:F1}x";
		};
	}

	public void OpenButtonToggled(bool toggledOn)
	{
		SetCommandLayerVisible(toggledOn);
	}

	private void SetCommandLayerVisible(bool visible)
	{
		_guiLayer.Visible = visible;
		GetTree().Paused = visible;
		_debugStatsRefreshRemaining = 0.0;
		SetProcess(debug && visible);
	}

	public void _on_speed_reset_button_pressed()
	{
		_gameSpeedSlider.Value = 1.0;
	}

	public void SetSunValue()
	{
		TowerDefenseManager.Instance.SetSun((int)_sunSpinBox.Value);
	}

	public void SetCoinValue()
	{
		TowerDefenseManager.Instance.coinBank.num = (long)_coinSpinBox.Value;
	}

	public void SetCrystalValue()
	{
		GameSaveManager.Instance.SetKeyValue("CrystalNum", (int)_crystalSpinBox.Value);
		GameSaveManager.Instance.Save();
	}

	public void TestLevelButtonPressed()
	{
		TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>("uid://bfl6f5wb3lu7m");
		_guiLayer.Visible = !_guiLayer.Visible;
		GetTree().Paused = _guiLayer.Visible;
		Global.Instance.enterLevelMode = "LevelChoose";
		SceneManager.Instance.ChangeScene("TowerDefense");
	}

	public void LoadLevelButtonPressed()
	{
		DisplayServer.FileDialogShow("打开关卡文件", "", "", showHidden: false, DisplayServer.FileDialogMode.OpenFile, new string[1] { "*.json,*.tres" }, Callable.From(delegate(long status, string[] selectedPaths, int selectedFilterIndex)
		{
			if (selectedPaths.Length != 0)
			{
				LoadSelectedLevelFile(selectedPaths[0]);
			}
		}));
	}

	private void LoadSelectedLevelFile(string selectedPath)
	{
		if (string.IsNullOrEmpty(selectedPath))
		{
			return;
		}
		byte[] fileAsBytes = FileAccess.GetFileAsBytes(selectedPath);
		if (fileAsBytes == null || fileAsBytes.Length == 0)
		{
			BroadcastInvalidLevelFile();
			return;
		}
		string text = selectedPath.GetExtension().ToLowerInvariant();
		TowerDefenseLevelConfig config3;
		if (!(text == "json"))
		{
			TowerDefenseLevelConfig config2;
			if (text == "tres")
			{
				if (TryLoadTresLevel(fileAsBytes, out var config))
				{
					EnterLoadedLevel(config);
					return;
				}
			}
			else if (TryLoadJsonLevel(fileAsBytes, out config2) || TryLoadTresLevel(fileAsBytes, out config2))
			{
				EnterLoadedLevel(config2);
				return;
			}
		}
		else if (TryLoadJsonLevel(fileAsBytes, out config3))
		{
			EnterLoadedLevel(config3);
			return;
		}
		BroadcastInvalidLevelFile();
	}

	private static bool TryLoadJsonLevel(byte[] bytes, out TowerDefenseLevelConfig config)
	{
		config = null;
		if (!StoreSelectedLevelBytes(bytes, "user://Csharp/import_level.json"))
		{
			return false;
		}
		try
		{
			Json json = new Json();
			if (json.Parse(bytes.GetStringFromUtf8()) != Error.Ok)
			{
				return false;
			}
			TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig
			{
				data = json
			};
			towerDefenseLevelConfig.Init();
			config = (TowerDefenseLevelConfig)towerDefenseLevelConfig.DuplicateDeep(Resource.DeepDuplicateMode.Internal);
			return config != null;
		}
		catch
		{
			config = null;
			return false;
		}
	}

	private static bool TryLoadTresLevel(byte[] bytes, out TowerDefenseLevelConfig config)
	{
		config = null;
		if (!StoreSelectedLevelBytes(bytes, "user://Csharp/import_level.tres"))
		{
			return false;
		}
		ResourceTextScriptMigration.MigrateLevelEditorResourceForLoadIfNeeded("user://Csharp/import_level.tres");
		TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("user://Csharp/import_level.tres", "", ResourceLoader.CacheMode.Ignore);
		if (towerDefenseLevelConfig == null)
		{
			return false;
		}
		config = (TowerDefenseLevelConfig)towerDefenseLevelConfig.DuplicateDeep(Resource.DeepDuplicateMode.Internal);
		return config != null;
	}

	private static bool StoreSelectedLevelBytes(byte[] bytes, string tempPath)
	{
		if (bytes == null || bytes.Length == 0 || string.IsNullOrEmpty(tempPath))
		{
			return false;
		}
		EnsureImportLevelDirectory();
		FileAccess fileAccess = FileAccess.Open(tempPath, FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			return false;
		}
		fileAccess.StoreBuffer(bytes);
		fileAccess.Close();
		return true;
	}

	private static void EnsureImportLevelDirectory()
	{
		if (!DirAccess.DirExistsAbsolute("user://Csharp"))
		{
			DirAccess.MakeDirRecursiveAbsolute("user://Csharp");
		}
	}

	private void EnterLoadedLevel(TowerDefenseLevelConfig config)
	{
		TowerDefenseManager.Instance.currentLevelConfig = config;
		_guiLayer.Visible = false;
		GetTree().Paused = _guiLayer.Visible;
		Global.Instance.enterLevelMode = "LoadLevel";
		SceneManager.Instance.ChangeScene("TowerDefense");
		Global.Instance.isEditor = false;
	}

	private static void BroadcastInvalidLevelFile()
	{
		BroadCastManager.Instance.BroadCastFloatCreate("不是关卡文件", Colors.Red);
	}

	public async void SkipToWave()
	{
		TowerDefenseBattleFeatureWave wave = TowerDefenseBattleFeatureWave.Instance;
		if (GodotObject.IsInstanceValid(wave))
		{
			int targetWave = (int)_waveSpinBox.Value;
			while (wave.currentWave < targetWave && !wave.waveFinal)
			{
				wave.timer = wave.nextWaveTime;
				wave.awaitSpawn = false;
				await ToSignal(GetTree().CreateTimer(0.2, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			}
		}
	}

	public async void SkipToFinalWave()
	{
		TowerDefenseBattleFeatureWave wave = TowerDefenseBattleFeatureWave.Instance;
		if (GodotObject.IsInstanceValid(wave))
		{
			if (!wave.waveStart)
			{
				wave.waveStart = true;
			}
			while (!wave.waveFinal)
			{
				wave.timer = wave.nextWaveTime;
				wave.awaitSpawn = false;
				await ToSignal(GetTree().CreateTimer(0.2, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			}
		}
	}

	public void SkipWaveWait()
	{
		TowerDefenseBattleFeatureWave instance = TowerDefenseBattleFeatureWave.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.timer = instance.nextWaveTime;
			instance.awaitSpawn = false;
		}
	}

	public void KillAllZombies()
	{
		foreach (Node item in GetTree().GetNodesInGroup("Zombie"))
		{
			if (GodotObject.IsInstanceValid(item) && item is TowerDefenseZombie towerDefenseZombie && !towerDefenseZombie.instance.die)
			{
				towerDefenseZombie.Hurt(1000000000000.0);
			}
		}
	}

	public void InstantWin()
	{
		TowerDefenseBattleFeatureWave instance = TowerDefenseBattleFeatureWave.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			KillAllZombies();
			instance.waveStart = true;
			instance.waveFinal = true;
			instance.EmitFinal();
			instance.awaitSpawn = false;
		}
	}

	public void RestoreAllMowers()
	{
		if (TowerDefenseManager.Instance == null)
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (currentControl == null || !(currentControl.GetFeature("Mower") is TowerDefenseBattleFeatureMower towerDefenseBattleFeatureMower))
		{
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (mapFeature == null)
		{
			return;
		}
		for (int i = 1; i <= mapFeature.config.gridNum.Y; i++)
		{
			if (TowerDefenseManager.Instance.GetMapLineUse(i) && !GodotObject.IsInstanceValid(towerDefenseBattleFeatureMower.mowerLine[i]))
			{
				towerDefenseBattleFeatureMower.CreateMower(i);
			}
		}
	}

	public void RemoveAllMowers()
	{
		if (TowerDefenseManager.Instance == null)
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (currentControl == null || !(currentControl.GetFeature("Mower") is TowerDefenseBattleFeatureMower towerDefenseBattleFeatureMower))
		{
			return;
		}
		for (int i = 0; i < towerDefenseBattleFeatureMower.mowerLine.Count; i++)
		{
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureMower.mowerLine[i]))
			{
				towerDefenseBattleFeatureMower.mowerLine[i].QueueFree();
				towerDefenseBattleFeatureMower.mowerLine[i] = null;
			}
		}
	}

	public void ResetAllBrains()
	{
		if (TowerDefenseManager.Instance == null)
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
		if (currentControl == null || !(currentControl.GetFeature("Brain") is TowerDefenseBattleFeatureBrain towerDefenseBattleFeatureBrain))
		{
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (mapFeature == null)
		{
			return;
		}
		for (int i = 0; i < towerDefenseBattleFeatureBrain.brainLine.Count; i++)
		{
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureBrain.brainLine[i]))
			{
				towerDefenseBattleFeatureBrain.brainLine[i].QueueFree();
				towerDefenseBattleFeatureBrain.brainLine[i] = null;
			}
		}
		for (int j = 1; j <= mapFeature.config.gridNum.Y; j++)
		{
			if (TowerDefenseManager.Instance.GetMapLineUse(j))
			{
				towerDefenseBattleFeatureBrain.CreateBrain(j);
			}
		}
	}

	public CommandManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/CommandManager");
	}
}
