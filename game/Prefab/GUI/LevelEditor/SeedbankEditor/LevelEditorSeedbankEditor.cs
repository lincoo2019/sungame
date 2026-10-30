using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorSeedbankEditor : Control
{
	private CheckBox _plantColumnCheckBox;

	private VBoxContainer _sunContainer;

	private VBoxContainer _sunManagerContainer;

	private VBoxContainer _conveyorContainer;

	private HBoxContainer _packetColdDownStartContainer;

	private HBoxContainer _packetColdDownUseContainer;

	private VBoxContainer _rainModeContainer;

	private OptionButton _methodOptionButton;

	private SpinBox _sunSpinBox;

	private CheckBox _sunUseCheckBox;

	private SpinBox _sunSpawnIntervalSpinBox;

	private SpinBox _sunSpawnNumSpinBox;

	private SpinBox _conveyorIntervalSpinBox;

	private OptionButton _conveyorTypeOptionButton;

	private CheckBox _packetColdDownStartUseCheckBox;

	private CheckBox _packetColdDownUseCheckBox;

	private SpinBox _rainModeIntervalSpinBox;

	private SpinBox _rainModeAliveTimeSpinBox;

	private OptionButton _rainModeTypeOptionButton;

	public static LevelEditorSeedbankEditor Instance;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelConfig levelConfig;

	public bool isInit;

	public static readonly Dictionary methodTranslate = new Dictionary
	{
		{ 0, "无" },
		{ 1, "选卡模式" },
		{ 2, "预选卡模式" },
		{ 3, "传送带模式" },
		{ 4, "种子雨模式" }
	};

	public static readonly Dictionary methodDictionary = new Dictionary
	{
		{ "无", 0 },
		{ "选卡模式", 1 },
		{ "预选卡模式", 2 },
		{ "传送带模式", 3 },
		{ "种子雨模式", 4 }
	};

	public Dictionary conveyorTypeTranslate = new Dictionary
	{
		{ "Default", "默认" },
		{ "Sun", "消耗阳光" }
	};

	public Dictionary conveyorTypeDictionary = new Dictionary
	{
		{ "默认", "Default" },
		{ "消耗阳光", "Sun" }
	};

	public Dictionary rainModeTypeTranslate = new Dictionary
	{
		{ "Default", "默认" },
		{ "Sun", "消耗阳光" }
	};

	public Dictionary rainModeTypeDictionary = new Dictionary
	{
		{ "默认", "Default" },
		{ "消耗阳光", "Sun" }
	};

	public override void _Ready()
	{
		_plantColumnCheckBox = GetNode<CheckBox>("%PlantColumnCheckBox");
		_sunContainer = GetNode<VBoxContainer>("%SunContainer");
		_sunManagerContainer = GetNode<VBoxContainer>("%SunManagerContainer");
		_conveyorContainer = GetNode<VBoxContainer>("%ConveyorContainer");
		_packetColdDownStartContainer = GetNode<HBoxContainer>("%PacketColdDownStartContainer");
		_packetColdDownUseContainer = GetNode<HBoxContainer>("%PacketColdDownUseContainer");
		_rainModeContainer = GetNode<VBoxContainer>("%RainModeContainer");
		_methodOptionButton = GetNode<OptionButton>("%MethodOptionButton");
		_sunSpinBox = GetNode<SpinBox>("%SunSpinBox");
		_sunUseCheckBox = GetNode<CheckBox>("%SunUseCheckBox");
		_sunSpawnIntervalSpinBox = GetNode<SpinBox>("%SunSpawnIntervalSpinBox");
		_sunSpawnNumSpinBox = GetNode<SpinBox>("%SunSpawnNumSpinBox");
		_conveyorIntervalSpinBox = GetNode<SpinBox>("%ConveyorIntervalSpinBox");
		_conveyorTypeOptionButton = GetNode<OptionButton>("%ConveyorTypeOptionButton");
		_packetColdDownStartUseCheckBox = GetNode<CheckBox>("%PacketColdDownStartUseCheckBox");
		_packetColdDownUseCheckBox = GetNode<CheckBox>("%PacketColdDownUseCheckBox");
		_rainModeIntervalSpinBox = GetNode<SpinBox>("%RainModeIntervalSpinBox");
		_rainModeAliveTimeSpinBox = GetNode<SpinBox>("%RainModeAliveTimeSpinBox");
		_rainModeTypeOptionButton = GetNode<OptionButton>("%RainModeTypeOptionButton");
		LevelEditorDropdown.Configure(_methodOptionButton);
		LevelEditorDropdown.Configure(_conveyorTypeOptionButton);
		LevelEditorDropdown.Configure(_rainModeTypeOptionButton);
		base.VisibilityChanged += Save;
		base.VisibilityChanged += OnChildPacketBankVisibilityChanged;
		_plantColumnCheckBox.Toggled += PlantColumnCheckBoxToggled;
		_methodOptionButton.ItemSelected += delegate(long index)
		{
			MethodOptionButtonItemSelected((int)index);
		};
		_packetColdDownUseCheckBox.Pressed += PacketColdDownUseCheckBoxPressed;
		_packetColdDownStartUseCheckBox.Pressed += PacketColdDownStartUseCheckBoxPressed;
		_conveyorIntervalSpinBox.ValueChanged += SunSpawnNumSpinBoxValueChanged;
		_conveyorTypeOptionButton.ItemSelected += delegate(long index)
		{
			ConveyorTypeOptionButtonItemSelected((int)index);
		};
		_rainModeIntervalSpinBox.ValueChanged += RainModeIntervalSpinBoxValueChanged;
		_rainModeAliveTimeSpinBox.ValueChanged += RainModeAliveTimeSpinBoxValueChanged;
		_rainModeTypeOptionButton.ItemSelected += delegate(long index)
		{
			RainModeTypeOptionButtonItemSelected((int)index);
		};
		_sunSpinBox.ValueChanged += SunSpinBoxValueChanged;
		_sunUseCheckBox.Toggled += SunUseCheckBoxToggled;
		_sunSpawnIntervalSpinBox.ValueChanged += SunSpawnIntervalSpinBoxValueChanged;
		_sunSpawnNumSpinBox.ValueChanged += SunSpawnNumSpinBoxValueChanged;
		Instance = this;
		_methodOptionButton.Clear();
		TowerDefenseEnum.LEVEL_SEEDBANK_METHOD[] values = Enum.GetValues<TowerDefenseEnum.LEVEL_SEEDBANK_METHOD>();
		foreach (TowerDefenseEnum.LEVEL_SEEDBANK_METHOD lEVEL_SEEDBANK_METHOD in values)
		{
			_methodOptionButton.AddItem((string)methodTranslate[(int)lEVEL_SEEDBANK_METHOD]);
		}
		_conveyorTypeOptionButton.Clear();
		foreach (Variant key in conveyorTypeTranslate.Keys)
		{
			string text = (string)key;
			_conveyorTypeOptionButton.AddItem((string)conveyorTypeTranslate[text]);
		}
		_rainModeTypeOptionButton.Clear();
		foreach (Variant key2 in rainModeTypeTranslate.Keys)
		{
			string text2 = (string)key2;
			_rainModeTypeOptionButton.AddItem((string)rainModeTypeTranslate[text2]);
		}
	}

	public void Init(TowerDefenseLevelConfig _levelConfig)
	{
		isInit = true;
		Clear();
		levelConfig = _levelConfig;
		_plantColumnCheckBox.ButtonPressed = levelConfig.plantColumn;
		_methodOptionButton.Selected = FindOptionButtonId(_methodOptionButton, (string)methodTranslate[(int)levelConfig.packetBankMethod]);
		MethodOptionButtonItemSelected(_methodOptionButton.Selected);
		if (!GodotObject.IsInstanceValid(levelConfig.conveyorData))
		{
			levelConfig.conveyorData = new TowerDefenseConveyorConfig();
		}
		else
		{
			levelConfig.conveyorData = levelConfig.conveyorData.Duplicate(deep: true) as TowerDefenseConveyorConfig;
		}
		_conveyorIntervalSpinBox.Value = levelConfig.conveyorData.interval;
		_conveyorTypeOptionButton.Selected = FindOptionButtonId(_conveyorTypeOptionButton, (string)conveyorTypeTranslate[levelConfig.conveyorData.type]);
		if (levelConfig.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR)
		{
			ConveyorTypeOptionButtonItemSelected(_conveyorTypeOptionButton.Selected);
		}
		if (!GodotObject.IsInstanceValid(levelConfig.rainData))
		{
			levelConfig.rainData = new TowerDefenseRainModeConfig();
		}
		else
		{
			levelConfig.rainData = levelConfig.rainData.Duplicate(deep: true) as TowerDefenseRainModeConfig;
		}
		_rainModeIntervalSpinBox.Value = levelConfig.rainData.interval;
		_rainModeAliveTimeSpinBox.Value = levelConfig.rainData.aliveTime;
		_rainModeTypeOptionButton.Selected = FindOptionButtonId(_rainModeTypeOptionButton, (string)rainModeTypeTranslate[levelConfig.rainData.type]);
		if (levelConfig.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN)
		{
			RainModeTypeOptionButtonItemSelected(_rainModeTypeOptionButton.Selected);
		}
		if (!GodotObject.IsInstanceValid(levelConfig.sunManager))
		{
			levelConfig.sunManager = new TowerDefenseLevelSunManagerConfig();
		}
		else
		{
			levelConfig.sunManager = levelConfig.sunManager.Duplicate(deep: true) as TowerDefenseLevelSunManagerConfig;
		}
		_sunSpinBox.Value = levelConfig.sunManager.begin;
		SunSpinBoxValueChanged(_sunSpinBox.Value);
		_sunUseCheckBox.ButtonPressed = levelConfig.sunManager.open;
		_sunSpawnIntervalSpinBox.Value = levelConfig.sunManager.spawnInterval;
		_sunSpawnNumSpinBox.Value = levelConfig.sunManager.spawnNum;
		_packetColdDownStartUseCheckBox.ButtonPressed = levelConfig.packetColdDownStart;
		_packetColdDownUseCheckBox.ButtonPressed = levelConfig.packetColdDownUse;
		switch (levelConfig.packetBankMethod)
		{
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE:
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET:
			LevelEditorSeedBankChoose.Instance.PacketListChoose(levelConfig.packetBankList);
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR:
			foreach (TowerDefenseConveyorPacketConfig packet in levelConfig.conveyorData.packetList)
			{
				LevelEditorSeedBankChoose.Instance.PacketNameChoose(packet.name);
			}
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN:
			foreach (Variant packet2 in levelConfig.rainData.packetList)
			{
				TowerDefenseRainModePacketConfig towerDefenseRainModePacketConfig = (TowerDefenseRainModePacketConfig)(GodotObject)packet2;
				LevelEditorSeedBankChoose.Instance.PacketNameChoose(towerDefenseRainModePacketConfig.name);
			}
			break;
		}
		isInit = false;
	}

	public void SetIZMMode(bool open)
	{
		if (open)
		{
			LevelEditorSeedbank.Instance.seedBankTexture.Texture = LevelEditorSeedbank.SEED_BANK_ZOMBIE;
		}
		else
		{
			LevelEditorSeedbank.Instance.seedBankTexture.Texture = LevelEditorSeedbank.SEED_BANK;
		}
	}

	public void OnChildPacketBankVisibilityChanged()
	{
		if (GodotObject.IsInstanceValid(LevelEditorSeedBankChoose.Instance))
		{
			LevelEditorSeedBankChoose.Instance.RefreshWhenVisible();
		}
	}

	public void Save()
	{
		if (!GodotObject.IsInstanceValid(levelConfig))
		{
			return;
		}
		levelConfig.plantColumn = _plantColumnCheckBox.ButtonPressed;
		levelConfig.conveyorData.interval = _conveyorIntervalSpinBox.Value;
		levelConfig.rainData.interval = _rainModeIntervalSpinBox.Value;
		levelConfig.rainData.aliveTime = _rainModeAliveTimeSpinBox.Value;
		levelConfig.sunManager.begin = (long)_sunSpinBox.Value;
		levelConfig.sunManager.open = _sunUseCheckBox.ButtonPressed;
		levelConfig.sunManager.spawnInterval = _sunSpawnIntervalSpinBox.Value;
		levelConfig.sunManager.spawnNum = (long)_sunSpawnNumSpinBox.Value;
		levelConfig.packetColdDownStart = _packetColdDownStartUseCheckBox.ButtonPressed;
		levelConfig.packetColdDownUse = _packetColdDownUseCheckBox.ButtonPressed;
		switch (levelConfig.packetBankMethod)
		{
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE:
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET:
			levelConfig.packetBankList.Clear();
			{
				foreach (TowerDefenseInGamePacketShow packet in LevelEditorSeedbank.Instance.packetList)
				{
					TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = new TowerDefenseLevelPacketConfig();
					towerDefenseLevelPacketConfig.packetName = packet.config.saveKey;
					levelConfig.packetBankList.Add(towerDefenseLevelPacketConfig);
				}
				break;
			}
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR:
			levelConfig.conveyorData.packetList.Clear();
			{
				foreach (TowerDefenseInGamePacketShow packet2 in LevelEditorSeedbank.Instance.packetList)
				{
					TowerDefenseConveyorPacketConfig towerDefenseConveyorPacketConfig = new TowerDefenseConveyorPacketConfig();
					towerDefenseConveyorPacketConfig.name = packet2.config.saveKey;
					levelConfig.conveyorData.packetList.Add(towerDefenseConveyorPacketConfig);
				}
				break;
			}
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN:
			levelConfig.rainData.packetList.Clear();
			{
				foreach (TowerDefenseInGamePacketShow packet3 in LevelEditorSeedbank.Instance.packetList)
				{
					TowerDefenseRainModePacketConfig towerDefenseRainModePacketConfig = new TowerDefenseRainModePacketConfig();
					towerDefenseRainModePacketConfig.name = packet3.config.saveKey;
					levelConfig.rainData.packetList.Add(towerDefenseRainModePacketConfig);
				}
				break;
			}
		}
	}

	public void Clear()
	{
		if (GodotObject.IsInstanceValid(LevelEditorSeedBankChoose.Instance))
		{
			LevelEditorSeedBankChoose.Instance.ClearSelectionAnimations();
		}
		LevelEditorSeedbank.Instance.Clear();
		levelConfig = null;
	}

	public int FindOptionButtonId(OptionButton optionButton, string key)
	{
		for (int i = 0; i < optionButton.ItemCount; i++)
		{
			if (optionButton.GetItemText(i) == key)
			{
				return optionButton.GetItemId(i);
			}
		}
		return -1;
	}

	public void MethodOptionButtonItemSelected(int index)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		string itemText = _methodOptionButton.GetItemText(index);
		levelConfig.SetPacketBankMethodFromEditor((TowerDefenseEnum.LEVEL_SEEDBANK_METHOD)(int)methodDictionary[itemText]);
		LevelEditorSeedbank.Instance.seedContanin.Visible = false;
		LevelEditorSeedbank.Instance.conveyor.Visible = false;
		LevelEditorSeedbank.Instance.packetContainer.Visible = false;
		_packetColdDownStartContainer.Visible = false;
		_packetColdDownUseContainer.Visible = false;
		_sunContainer.Visible = false;
		_conveyorContainer.Visible = false;
		_rainModeContainer.Visible = false;
		switch (levelConfig.packetBankMethod)
		{
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.NOONE:
			_packetColdDownStartContainer.Visible = true;
			_packetColdDownUseContainer.Visible = true;
			_sunContainer.Visible = true;
			LevelEditorSeedBankChoose.Instance.Visible = false;
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE:
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.PRESET:
			LevelEditorSeedbank.Instance.seedContanin.Visible = true;
			LevelEditorSeedbank.Instance.packetContainer.Visible = true;
			_packetColdDownStartContainer.Visible = true;
			_packetColdDownUseContainer.Visible = true;
			_sunContainer.Visible = true;
			LevelEditorSeedBankChoose.Instance.Visible = true;
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CONVEYOR:
			_sunUseCheckBox.ButtonPressed = false;
			LevelEditorSeedbank.Instance.conveyor.Visible = true;
			LevelEditorSeedbank.Instance.packetContainer.Visible = true;
			_conveyorContainer.Visible = true;
			LevelEditorSeedBankChoose.Instance.Visible = true;
			if (!GodotObject.IsInstanceValid(levelConfig.conveyorData))
			{
				levelConfig.conveyorData = new TowerDefenseConveyorConfig();
			}
			_conveyorTypeOptionButton.Selected = FindOptionButtonId(_conveyorTypeOptionButton, (string)conveyorTypeTranslate[levelConfig.conveyorData.type]);
			ConveyorTypeOptionButtonItemSelected(_conveyorTypeOptionButton.Selected);
			break;
		case TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.RAIN:
			_sunUseCheckBox.ButtonPressed = false;
			LevelEditorSeedbank.Instance.seedContanin.Visible = true;
			LevelEditorSeedbank.Instance.packetContainer.Visible = true;
			_rainModeContainer.Visible = true;
			LevelEditorSeedBankChoose.Instance.Visible = true;
			if (!GodotObject.IsInstanceValid(levelConfig.rainData))
			{
				levelConfig.rainData = new TowerDefenseRainModeConfig();
			}
			_rainModeTypeOptionButton.Selected = FindOptionButtonId(_rainModeTypeOptionButton, (string)rainModeTypeTranslate[levelConfig.rainData.type]);
			RainModeTypeOptionButtonItemSelected(_rainModeTypeOptionButton.Selected);
			break;
		}
	}

	public void SunUseCheckBoxToggled(bool toggledOn)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		_sunManagerContainer.Visible = toggledOn;
	}

	public void SunSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		LevelEditorSeedbank.Instance.sunLabel.Text = ((int)value).ToString();
		LevelEditorSeedbank.Instance.conveyorSunLabel.Text = ((int)value).ToString();
	}

	public void PacketColdDownStartUseCheckBoxPressed()
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void PacketColdDownUseCheckBoxPressed()
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void SunSpawnIntervalSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void SunSpawnNumSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void RainModeIntervalSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void RainModeAliveTimeSpinBoxValueChanged(double value)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void PlantColumnCheckBoxToggled(bool toggledOn)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
	}

	public void ConveyorTypeOptionButtonItemSelected(int index)
	{
		string itemText = _conveyorTypeOptionButton.GetItemText(index);
		levelConfig.conveyorData.type = (string)conveyorTypeDictionary[itemText];
		if (levelConfig.conveyorData.type == "Sun")
		{
			LevelEditorSeedbank.Instance.conveyorBeltRectPC.Texture = LevelEditorSeedbank.CONVEYOR_BELT_SUN_BACKDROP;
			LevelEditorSeedbank.Instance.belt.Texture = LevelEditorSeedbank.CONVEYOR_BELT_SUN;
			LevelEditorSeedbank.Instance.conveyorSunBankTexture.Visible = true;
			_sunContainer.Visible = true;
		}
		else
		{
			LevelEditorSeedbank.Instance.conveyorBeltRectPC.Texture = LevelEditorSeedbank.CONVEYOR_BELT_BACKDROP;
			LevelEditorSeedbank.Instance.belt.Texture = LevelEditorSeedbank.CONVEYOR_BELT;
			LevelEditorSeedbank.Instance.conveyorSunBankTexture.Visible = false;
			_sunContainer.Visible = false;
		}
	}

	public void RainModeTypeOptionButtonItemSelected(int index)
	{
		string itemText = _rainModeTypeOptionButton.GetItemText(index);
		levelConfig.rainData.type = (string)rainModeTypeDictionary[itemText];
		_sunContainer.Visible = levelConfig.rainData.type == "Sun";
	}
}
