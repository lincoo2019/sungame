using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorWaveEditor : Control
{
	private static PackedScene _levelEditorWaveEditorPacketContainer;

	private Control _spawnNode;

	private Control _eventNode;

	private CheckBox _zombieInvisibleCheckBox;

	private LevelEditorWaveEditorPacketContainer _poolPacketContainer;

	private LevelEditorWaveEditorPacketContainer _randomPacketContainer;

	private VBoxContainer _packetContainerListContainer;

	private LevelEventListContainer _eventListContainer;

	private SpinBox _bigWaveSpinBox;

	private SpinBox _waveIntervalSpinBox;

	private SpinBox _beginColSpinBox;

	private SpinBox _spawnColEndSpinBox;

	private SpinBox _spawnColStartSpinBox;

	private SpinBox _minNextWaveHealthPercentageSpinBox;

	private SpinBox _maxNextWaveHealthPercentageSpinBox;

	private Label _tipsLabel;

	private HSlider _flagSlider;

	private Label _waveLabel;

	private LevelEditorInspector _levelEditorInspector;

	public static LevelEditorWaveEditor Instance;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelConfig levelConfig;

	public TowerDefenseLevelWaveManagerConfig waveManagerConfig;

	public bool isInit;

	public bool isLoad;

	public bool isLoading;

	public int _loadGeneration;

	private int _currentFlag = -1;

	public LevelEditorWaveEditorPacketContainer currentPacketContainer;

	private static PackedScene LEVEL_EDITOR_WAVE_EDITOR_PACKET_CONTAINER => _levelEditorWaveEditorPacketContainer ?? (_levelEditorWaveEditorPacketContainer = GD.Load<PackedScene>("uid://bua2w8mprv7s"));

	public int currentFlag
	{
		get
		{
			return _currentFlag;
		}
		set
		{
			if (_currentFlag != -1 && value != -1 && !isLoading)
			{
				SaveFlag(_currentFlag);
			}
			_currentFlag = value;
		}
	}

	public override void _Ready()
	{
		_spawnNode = GetNode<Control>("%SpawnNode");
		_eventNode = GetNode<Control>("%EventNode");
		_zombieInvisibleCheckBox = GetNode<CheckBox>("%ZombieInvisibleCheckBox");
		_poolPacketContainer = GetNode<LevelEditorWaveEditorPacketContainer>("%PoolPacketContainer");
		_randomPacketContainer = GetNode<LevelEditorWaveEditorPacketContainer>("%RandomPacketContainer");
		_packetContainerListContainer = GetNode<VBoxContainer>("%PacketContainerListContainer");
		_eventListContainer = GetNode<LevelEventListContainer>("%EventListContainer");
		_eventListContainer.OnChange += EventListContainerChange;
		_bigWaveSpinBox = GetNode<SpinBox>("%BigWaveSpinBox");
		_waveIntervalSpinBox = GetNode<SpinBox>("%WaveIntervalSpinBox");
		_beginColSpinBox = GetNode<SpinBox>("%BeginColSpinBox");
		_spawnColEndSpinBox = GetNode<SpinBox>("%SpawnColEndSpinBox");
		_spawnColStartSpinBox = GetNode<SpinBox>("%SpawnColStartSpinBox");
		_minNextWaveHealthPercentageSpinBox = GetNode<SpinBox>("%MinNextWaveHealthPercentageSpinBox");
		_maxNextWaveHealthPercentageSpinBox = GetNode<SpinBox>("%MaxNextWaveHealthPercentageSpinBox");
		_tipsLabel = GetNode<Label>("%TipsLabel");
		_flagSlider = GetNode<HSlider>("%FlagSlider");
		_waveLabel = GetNode<Label>("%WaveLabel");
		_levelEditorInspector = GetNode<LevelEditorInspector>("%LevelEditorInspector");
		_poolPacketContainer.OnSelected += Selected;
		_randomPacketContainer.mainButton.text = "编辑随机出怪";
		_randomPacketContainer.OnSelected += Selected;
		for (int i = 0; i < 25; i++)
		{
			LevelEditorWaveEditorPacketContainer obj = _packetContainerListContainer.GetChild(i) as LevelEditorWaveEditorPacketContainer;
			obj.mainButton.text = $"编辑第{i + 1}行";
			obj.OnSelected += Selected;
		}
		currentPacketContainer = null;
		Instance = this;
		_zombieInvisibleCheckBox.Toggled += ZombieInvisibleCheckBoxToggled;
		_bigWaveSpinBox.ValueChanged += BigWaveSpinBoxValueChanged;
		_waveIntervalSpinBox.ValueChanged += WaveIntervalSpinBoxValueChanged;
		_beginColSpinBox.ValueChanged += BeginColSpinBoxValueChanged;
		_spawnColEndSpinBox.ValueChanged += SpawnColEndSpinBoxValueChanged;
		_spawnColStartSpinBox.ValueChanged += SpawnColStartSpinBoxValueChanged;
		_minNextWaveHealthPercentageSpinBox.ValueChanged += MinNextWaveHealthPercentageSpinBoxValueChanged;
		_maxNextWaveHealthPercentageSpinBox.ValueChanged += MaxNextWaveHealthPercentageSpinBoxValueChanged;
		_flagSlider.DragEnded += FlagChanged;
		GetNode<NinePatchButtonBase>("Transform/SpawnButton").OnPressed += SpawnButtonPressed;
		GetNode<NinePatchButtonBase>("Transform/EventButton").OnPressed += EventButtonPressed;
		base.VisibilityChanged += OnChildPacketBankVisibilityChanged;
	}

	public void OnChildPacketBankVisibilityChanged()
	{
		if (GodotObject.IsInstanceValid(LevelEditorWaveEditorPacketChoose.Instance))
		{
			LevelEditorWaveEditorPacketChoose.Instance.RefreshWhenVisible();
		}
	}

	public void Clear()
	{
		levelConfig = null;
		waveManagerConfig = null;
		ClearPanel();
	}

	public void ClearPanel()
	{
		isLoading = false;
		_poolPacketContainer.Clear();
		_randomPacketContainer.Clear();
		_eventListContainer.Clear();
		for (int i = 0; i < 25; i++)
		{
			(_packetContainerListContainer.GetChild(i) as LevelEditorWaveEditorPacketContainer).Clear();
		}
	}

	public void Init(TowerDefenseLevelConfig _levelConfig)
	{
		isInit = true;
		Clear();
		_currentFlag = -1;
		levelConfig = _levelConfig;
		if (!GodotObject.IsInstanceValid(levelConfig.waveManager))
		{
			waveManagerConfig = new TowerDefenseLevelWaveManagerConfig();
			for (int i = 0; i < waveManagerConfig.flagWaveInterval; i++)
			{
				TowerDefenseLevelWaveConfig item = new TowerDefenseLevelWaveConfig();
				waveManagerConfig.wave.Add(item);
			}
		}
		else
		{
			waveManagerConfig = levelConfig.waveManager.Duplicate(deep: true) as TowerDefenseLevelWaveManagerConfig;
		}
		_zombieInvisibleCheckBox.ButtonPressed = waveManagerConfig.zombieInvisible;
		_bigWaveSpinBox.Value = (double)waveManagerConfig.wave.Count / (double)waveManagerConfig.flagWaveInterval;
		_waveIntervalSpinBox.Value = waveManagerConfig.flagWaveInterval;
		_beginColSpinBox.Value = waveManagerConfig.beginCol;
		_spawnColEndSpinBox.Value = waveManagerConfig.spawnColEnd;
		_spawnColStartSpinBox.Value = waveManagerConfig.spawnColStart;
		_minNextWaveHealthPercentageSpinBox.Value = waveManagerConfig.minNextWaveHealthPercentage;
		_maxNextWaveHealthPercentageSpinBox.Value = waveManagerConfig.maxNextWaveHealthPercentage;
		WaveNumChange();
		isInit = false;
		FlagChanged(isChange: true);
	}

	public void Save()
	{
		if (GodotObject.IsInstanceValid(waveManagerConfig) && !isLoading)
		{
			SaveFlag(_currentFlag);
			waveManagerConfig.zombieInvisible = _zombieInvisibleCheckBox.ButtonPressed;
			waveManagerConfig.flagWaveInterval = (int)_waveIntervalSpinBox.Value;
			waveManagerConfig.beginCol = _beginColSpinBox.Value;
			waveManagerConfig.spawnColEnd = _spawnColEndSpinBox.Value;
			waveManagerConfig.spawnColStart = _spawnColStartSpinBox.Value;
			waveManagerConfig.minNextWaveHealthPercentage = _minNextWaveHealthPercentageSpinBox.Value;
			waveManagerConfig.maxNextWaveHealthPercentage = _maxNextWaveHealthPercentageSpinBox.Value;
			Array<TowerDefenseLevelWaveConfig> array = new Array<TowerDefenseLevelWaveConfig>();
			for (int i = 0; (double)i < _waveIntervalSpinBox.Value * _bigWaveSpinBox.Value; i++)
			{
				array.Add(waveManagerConfig.wave[i]);
			}
			waveManagerConfig.wave = array;
			levelConfig.waveManager = waveManagerConfig;
		}
	}

	public void SaveFlag(int flagId)
	{
		if (_currentFlag == -1 || !GodotObject.IsInstanceValid(waveManagerConfig))
		{
			return;
		}
		TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig = waveManagerConfig.wave[flagId];
		towerDefenseLevelWaveConfig.spawn.Clear();
		towerDefenseLevelWaveConfig.dynamic = new TowerDefenseLevelSpawnDynamicConfig();
		foreach (TowerDefenseInGamePacketShow packet in _poolPacketContainer.packetList)
		{
			string saveKey = packet.config.saveKey;
			int wavePointCost = packet.config.GetWavePointCost();
			towerDefenseLevelWaveConfig.dynamic.points += wavePointCost;
			towerDefenseLevelWaveConfig.dynamic.zombiePool.Add(saveKey);
		}
		foreach (TowerDefenseInGamePacketShow packet2 in _randomPacketContainer.packetList)
		{
			string saveKey2 = packet2.config.saveKey;
			TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig = new TowerDefenseLevelSpawnConfig();
			towerDefenseLevelSpawnConfig.zombie = saveKey2;
			towerDefenseLevelSpawnConfig.line = -1;
			towerDefenseLevelSpawnConfig.num = 1;
			towerDefenseLevelWaveConfig.spawn.Add(towerDefenseLevelSpawnConfig);
		}
		for (int i = 0; i < 25; i++)
		{
			LevelEditorWaveEditorPacketContainer levelEditorWaveEditorPacketContainer = _packetContainerListContainer.GetChild(i) as LevelEditorWaveEditorPacketContainer;
			if (!levelEditorWaveEditorPacketContainer.Visible)
			{
				continue;
			}
			foreach (TowerDefenseInGamePacketShow packet3 in levelEditorWaveEditorPacketContainer.packetList)
			{
				string saveKey3 = packet3.config.saveKey;
				TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig2 = new TowerDefenseLevelSpawnConfig();
				towerDefenseLevelSpawnConfig2.zombie = saveKey3;
				towerDefenseLevelSpawnConfig2.line = i + 1;
				towerDefenseLevelSpawnConfig2.num = 1;
				towerDefenseLevelWaveConfig.spawn.Add(towerDefenseLevelSpawnConfig2);
			}
		}
		towerDefenseLevelWaveConfig.eventList = _eventListContainer.GetEventList();
		_levelEditorInspector.Clear();
	}

	public void MapChange(TowerDefenseMapConfig _config)
	{
		for (int i = 0; i < 25; i++)
		{
			((CanvasItem)_packetContainerListContainer.GetChild(i)).Visible = i < _config.gridNum.Y;
		}
	}

	public async void FlagChanged(bool isChange)
	{
		if (!GodotObject.IsInstanceValid(waveManagerConfig))
		{
			isLoading = false;
			return;
		}
		if (!isChange)
		{
			isLoading = false;
			return;
		}
		if (_currentFlag != -1)
		{
			SaveFlag(_currentFlag);
		}
		_loadGeneration++;
		int currentGeneration = _loadGeneration;
		isLoading = true;
		int num = (int)_flagSlider.Value;
		_currentFlag = num;
		_waveLabel.Text = $"当前波:{_currentFlag + 1}";
		ClearPanel();
		isLoading = true;
		TowerDefenseLevelWaveConfig waveConfig = waveManagerConfig.wave[_currentFlag];
		int batchCount = 0;
		foreach (string item in waveConfig.dynamic.zombiePool)
		{
			if (_loadGeneration != currentGeneration)
			{
				return;
			}
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(item);
			_poolPacketContainer.AddPacket(packetConfig);
			batchCount++;
			if (batchCount >= 8)
			{
				batchCount = 0;
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (_loadGeneration != currentGeneration)
				{
					return;
				}
			}
		}
		foreach (TowerDefenseLevelSpawnConfig item2 in waveConfig.spawn)
		{
			if (_loadGeneration != currentGeneration)
			{
				return;
			}
			string zombie = item2.zombie;
			LevelEditorWaveEditorPacketContainer levelEditorWaveEditorPacketContainer = ((item2.line != -1) ? (_packetContainerListContainer.GetChild(item2.line - 1) as LevelEditorWaveEditorPacketContainer) : _randomPacketContainer);
			TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig(zombie);
			for (int i = 0; i < item2.num; i++)
			{
				levelEditorWaveEditorPacketContainer.AddPacket(packetConfig2);
			}
			batchCount++;
			if (batchCount >= 8)
			{
				batchCount = 0;
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (_loadGeneration != currentGeneration)
				{
					return;
				}
			}
		}
		_eventListContainer.Init(waveConfig.eventList);
		isLoading = false;
	}

	public void BigWaveSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		WaveNumChange();
	}

	public void WaveIntervalSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		WaveNumChange();
	}

	public void WaveNumChange()
	{
		for (int i = 0; (double)i < _waveIntervalSpinBox.Value * _bigWaveSpinBox.Value; i++)
		{
			if (waveManagerConfig.wave.Count <= i)
			{
				TowerDefenseLevelWaveConfig item = new TowerDefenseLevelWaveConfig();
				waveManagerConfig.wave.Add(item);
			}
		}
		_flagSlider.MaxValue = _waveIntervalSpinBox.Value * _bigWaveSpinBox.Value - 1.0;
		_flagSlider.TickCount = (int)_bigWaveSpinBox.Value + 1;
		_flagSlider.Value = 0.0;
		if (!isInit)
		{
			FlagChanged(isChange: true);
		}
	}

	public void Selected(LevelEditorWaveEditorPacketContainer packetContainer)
	{
		currentPacketContainer = packetContainer;
		_tipsLabel.Modulate = Colors.White;
		_tipsLabel.Text = $"当前{currentPacketContainer.mainButton.text}";
	}

	public void SpawnButtonPressed()
	{
		_spawnNode.Visible = true;
		_eventNode.Visible = false;
	}

	public void EventButtonPressed()
	{
		_spawnNode.Visible = false;
		_eventNode.Visible = true;
	}

	public void BeginColSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void SpawnColEndSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void SpawnColStartSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void MinNextWaveHealthPercentageSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void MaxNextWaveHealthPercentageSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void EventListContainerChange()
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void ZombieInvisibleCheckBoxToggled(bool toggledOn)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		waveManagerConfig.zombieInvisible = toggledOn;
	}
}
