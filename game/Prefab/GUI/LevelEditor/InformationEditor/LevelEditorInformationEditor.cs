using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelEditorInformationEditor : Control
{
	private SpinBox levelNumberSpinBox;

	private LineEdit levelNameLineEdit;

	private TextEdit levelDescriptionTextEdit;

	private OptionButton homeWorldOptionButton;

	private OptionButton mapOptionButton;

	private OptionButton bgmOptionButton;

	private OptionButton talkOptionButton;

	private CheckBox talkCheckBox;

	private OptionButton tutorialOptionButton;

	private CheckBox tutorialCheckBox;

	private OptionButton finishMethodOptionButton;

	private CheckBox mowerUseCheckBox;

	private CheckBox vaseShuffleCheckBox;

	private CheckBox iZMShuffleCheckBox;

	private HBoxContainer fogUseContainer;

	private HBoxContainer fogbeginColumnContainer;

	private CheckBox fogUseCheckBox;

	private SpinBox fogbeginColumnSpinBox;

	private HBoxContainer stormOpenContainer;

	private CheckBox stormOpenCheckBox;

	private TextureRect mapTexture;

	private HBoxContainer vaseShuffleContainer;

	private HBoxContainer iZMShuffleContainer;

	public static LevelEditorInformationEditor Instance;

	public Dictionary homeWorldTranslate = new Dictionary
	{
		[0] = "无",
		[1] = "现代"
	};

	public Dictionary homeWorldDictionary = new Dictionary
	{
		["现代"] = 1,
		["无"] = 0
	};

	public Dictionary finishMethodTranslate = new Dictionary
	{
		[0] = "波模式",
		[1] = "罐子模式",
		[2] = "我是僵尸模式"
	};

	public Dictionary finishMethodDictionary = new Dictionary
	{
		["波模式"] = 0,
		["罐子模式"] = 1,
		["我是僵尸模式"] = 2
	};

	public Dictionary mapDictionary = new Dictionary();

	public Dictionary bgmDictionary = new Dictionary();

	public Dictionary talkDictionary = new Dictionary();

	public Dictionary tutorialDictionary = new Dictionary();

	public Dictionary graveStoneDictionary = new Dictionary();

	public bool isInit;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelConfig levelConfig { get; set; }

	public override void _Ready()
	{
		levelNumberSpinBox = GetNode<SpinBox>("%LevelNumberSpinBox");
		levelNameLineEdit = GetNode<LineEdit>("%LevelNameLineEdit");
		levelDescriptionTextEdit = GetNode<TextEdit>("%LevelDescriptionTextEdit");
		homeWorldOptionButton = GetNode<OptionButton>("%HomeWorldOptionButton");
		mapOptionButton = GetNode<OptionButton>("%MapOptionButton");
		bgmOptionButton = GetNode<OptionButton>("%BgmOptionButton");
		talkOptionButton = GetNode<OptionButton>("%TalkOptionButton");
		talkCheckBox = GetNode<CheckBox>("%TalkCheckBox");
		tutorialOptionButton = GetNode<OptionButton>("%TutorialOptionButton");
		tutorialCheckBox = GetNode<CheckBox>("%TutorialCheckBox");
		finishMethodOptionButton = GetNode<OptionButton>("%FinishMethodOptionButton");
		mowerUseCheckBox = GetNode<CheckBox>("%MowerUseCheckBox");
		vaseShuffleCheckBox = GetNode<CheckBox>("%VaseShuffleCheckBox");
		iZMShuffleCheckBox = GetNode<CheckBox>("%IZMShuffleCheckBox");
		fogUseContainer = GetNode<HBoxContainer>("%FogUseContainer");
		fogbeginColumnContainer = GetNode<HBoxContainer>("%FogBeginColumnContainer");
		fogUseCheckBox = GetNode<CheckBox>("%FogUseCheckBox");
		fogbeginColumnSpinBox = GetNode<SpinBox>("%FogBeginColumnSpinBox");
		stormOpenContainer = GetNode<HBoxContainer>("%StormOpenContainer");
		stormOpenCheckBox = GetNode<CheckBox>("%StormOpenCheckBox");
		mapTexture = GetNode<TextureRect>("%MapTexture");
		vaseShuffleContainer = GetNode<HBoxContainer>("%VaseShuffleContainer");
		iZMShuffleContainer = GetNode<HBoxContainer>("%IZMShuffleContainer");
		LevelEditorDropdown.Configure(homeWorldOptionButton);
		LevelEditorDropdown.Configure(mapOptionButton);
		LevelEditorDropdown.Configure(bgmOptionButton);
		LevelEditorDropdown.Configure(talkOptionButton);
		LevelEditorDropdown.Configure(tutorialOptionButton);
		LevelEditorDropdown.Configure(finishMethodOptionButton);
		levelNumberSpinBox.ValueChanged += LevelNumberSpinBoxValueChanged;
		levelNameLineEdit.TextChanged += LevelNameLineEditTextChanged;
		levelDescriptionTextEdit.TextChanged += LevelDescriptionLineEditTextChanged;
		homeWorldOptionButton.ItemSelected += HomeWorldOptionButtonItemSelected;
		mapOptionButton.ItemSelected += MapOptionButtonItemSelected;
		bgmOptionButton.ItemSelected += BgmOptionButtonItemSelected;
		talkOptionButton.ItemSelected += TalkOptionButtonItemSelected;
		talkCheckBox.Toggled += TalkCheckBoxToggled;
		tutorialOptionButton.ItemSelected += TutorialOptionButtonItemSelected;
		tutorialCheckBox.Toggled += TutorialCheckBoxToggled;
		finishMethodOptionButton.ItemSelected += FinishMethodOptionButtonItemSelected;
		stormOpenCheckBox.Toggled += StormOpenCheckBoxToggled;
		fogUseCheckBox.Toggled += FogUseCheckBoxToggled;
		fogbeginColumnSpinBox.ValueChanged += FogBeginColumnSpinBoxValueChanged;
		mowerUseCheckBox.Toggled += MowerUseCheckBoxToggled;
		vaseShuffleCheckBox.Toggled += VaseShuffleCheckBoxToggled;
		iZMShuffleCheckBox.Toggled += IZMShuffleCheckBoxToggled;
		Instance = this;
		GeneralEnum.HOMEWORLD[] values = Enum.GetValues<GeneralEnum.HOMEWORLD>();
		foreach (GeneralEnum.HOMEWORLD hOMEWORLD in values)
		{
			homeWorldOptionButton.AddItem(homeWorldTranslate[(int)hOMEWORLD].AsString());
		}
		foreach (Variant key in finishMethodTranslate.Keys)
		{
			finishMethodOptionButton.AddItem(finishMethodTranslate[key].AsString());
		}
		foreach (string key2 in ResourceManager.Instance.MAPS.Keys)
		{
			TowerDefenseMapConfig towerDefenseMapConfig = (TowerDefenseMapConfig)ResourceManager.Instance.MAPS[key2];
			mapOptionButton.AddItem(towerDefenseMapConfig.translate);
			mapDictionary[towerDefenseMapConfig.translate] = key2;
		}
		ResourceManager.Instance.EnsureAllBgmsLoaded();
		foreach (string key3 in ResourceManager.Instance.BGMS.Keys)
		{
			TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig = (TowerDefenseBackgroundMusicConfig)ResourceManager.Instance.BGMS[key3];
			bgmOptionButton.AddItem(towerDefenseBackgroundMusicConfig.translate);
			bgmDictionary[towerDefenseBackgroundMusicConfig.translate] = key3;
		}
		foreach (string key4 in ResourceManager.Instance.TALKS.Keys)
		{
			talkOptionButton.AddItem(key4);
		}
		foreach (string key5 in ResourceManager.Instance.TUTORIALS.Keys)
		{
			tutorialOptionButton.AddItem(key5);
		}
	}

	public void Init(TowerDefenseLevelConfig _levelConfig)
	{
		isInit = true;
		levelConfig = _levelConfig;
		levelNumberSpinBox.Value = levelConfig.levelNumber;
		levelNameLineEdit.Text = levelConfig.levelName;
		levelDescriptionTextEdit.Text = levelConfig.description;
		stormOpenCheckBox.ButtonPressed = levelConfig.stormOpen;
		mowerUseCheckBox.ButtonPressed = levelConfig.mowerUse;
		homeWorldOptionButton.Selected = FindOptionButtonId(homeWorldOptionButton, homeWorldTranslate[(int)levelConfig.homeWorld].AsString());
		finishMethodOptionButton.Selected = FindOptionButtonId(finishMethodOptionButton, finishMethodTranslate[(int)levelConfig.finishMethod].AsString());
		FinishMethodOptionButtonItemSelected(finishMethodOptionButton.Selected);
		TowerDefenseMapConfig towerDefenseMapConfig = (TowerDefenseMapConfig)ResourceManager.Instance.MAPS[levelConfig.map];
		mapOptionButton.Selected = FindOptionButtonId(mapOptionButton, towerDefenseMapConfig.translate);
		MapOptionButtonItemSelected(mapOptionButton.Selected);
		TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig = (TowerDefenseBackgroundMusicConfig)ResourceManager.Instance.BGMS[levelConfig.backgroundMusic];
		bgmOptionButton.Selected = FindOptionButtonId(bgmOptionButton, towerDefenseBackgroundMusicConfig.translate);
		talkOptionButton.Selected = FindOptionButtonId(talkOptionButton, levelConfig.talk.AsString());
		tutorialOptionButton.Selected = FindOptionButtonId(tutorialOptionButton, levelConfig.tutorial.AsString());
		talkCheckBox.ButtonPressed = levelConfig.isCustomTalk;
		tutorialCheckBox.ButtonPressed = levelConfig.isCustomTutorial;
		if (!GodotObject.IsInstanceValid(levelConfig.fogManager))
		{
			levelConfig.fogManager = new TowerDefenseLevelFogManagerConfig();
		}
		fogUseCheckBox.ButtonPressed = levelConfig.fogManager.open;
		fogbeginColumnSpinBox.Value = levelConfig.fogManager.beginColumn;
		switch (levelConfig.finishMethod)
		{
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
			if (!GodotObject.IsInstanceValid(levelConfig.vaseManager))
			{
				levelConfig.vaseManager = new TowerDefenseLevelVaseManagerConfig();
			}
			vaseShuffleCheckBox.ButtonPressed = levelConfig.vaseManager.shuffle;
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
			if (!GodotObject.IsInstanceValid(levelConfig.izmManager))
			{
				levelConfig.izmManager = new TowerDefenseLevelIZMManagerConfig();
			}
			iZMShuffleCheckBox.ButtonPressed = levelConfig.izmManager.shuffle;
			break;
		}
		isInit = false;
	}

	public void Clear()
	{
		levelConfig = null;
	}

	public void SetMapTexture(Texture2D texture)
	{
		TowerDefenseMapConfig.ApplyMapPreviewTexture(mapTexture, texture);
	}

	public void FreshFinishMethod()
	{
		vaseShuffleContainer.Visible = false;
		iZMShuffleContainer.Visible = false;
		switch (levelConfig.finishMethod)
		{
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
			LevelEditorSeedbankEditor.Instance.SetIZMMode(open: false);
			levelConfig.packetBank = "GeneralPlant";
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
			iZMShuffleContainer.Visible = true;
			if (!GodotObject.IsInstanceValid(levelConfig.izmManager))
			{
				levelConfig.izmManager = new TowerDefenseLevelIZMManagerConfig();
			}
			iZMShuffleCheckBox.ButtonPressed = levelConfig.izmManager.shuffle;
			LevelEditorSeedbankEditor.Instance.SetIZMMode(open: true);
			levelConfig.packetBank = "GeneralZombie";
			break;
		}
		switch (levelConfig.finishMethod)
		{
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
		{
			vaseShuffleContainer.Visible = true;
			if (!GodotObject.IsInstanceValid(levelConfig.vaseManager))
			{
				levelConfig.vaseManager = new TowerDefenseLevelVaseManagerConfig();
			}
			vaseShuffleCheckBox.ButtonPressed = levelConfig.vaseManager.shuffle;
			Array<TowerDefenseLevelPreSpawnConfig> array = new Array<TowerDefenseLevelPreSpawnConfig>();
			foreach (TowerDefenseLevelPreSpawnConfig preSpawn in levelConfig.preSpawnList)
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(preSpawn.packetName);
				if (packetConfig.characterConfig is TowerDefenseVaseConfig)
				{
					TowerDefenseLevelVaseConfig towerDefenseLevelVaseConfig = new TowerDefenseLevelVaseConfig();
					towerDefenseLevelVaseConfig.gridPos = preSpawn.gridPos;
					if (GodotObject.IsInstanceValid(preSpawn.characterOverride) && preSpawn.characterOverride.propertyChange.Count > 0)
					{
						foreach (TowerDefenseCharacterPropertyChangeConfig item in preSpawn.characterOverride.propertyChange)
						{
							if (item.propertyName == "packetName")
							{
								towerDefenseLevelVaseConfig.packetName = item.value.AsString();
								break;
							}
						}
					}
					string type = packetConfig.saveKey;
					if (!(type == "VasePlant"))
					{
						if (type == "VaseZombie")
						{
							towerDefenseLevelVaseConfig.type = "Zombie";
						}
						else
						{
							towerDefenseLevelVaseConfig.type = "Normal";
						}
					}
					else
					{
						towerDefenseLevelVaseConfig.type = "Plant";
					}
					levelConfig.vaseManager.vaseList.Add(towerDefenseLevelVaseConfig);
				}
				else
				{
					array.Add(preSpawn);
				}
			}
			levelConfig.preSpawnList = array;
			break;
		}
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
			switch (levelConfig.finishMethod)
			{
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
				levelConfig.packetBank = "GeneralPlant";
				break;
			case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
				levelConfig.packetBank = "GeneralZombie";
				break;
			}
			if (!GodotObject.IsInstanceValid(levelConfig.vaseManager))
			{
				break;
			}
			foreach (TowerDefenseLevelVaseConfig vase in levelConfig.vaseManager.vaseList)
			{
				TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig();
				towerDefenseLevelPreSpawnConfig.gridPos = vase.gridPos;
				string type = vase.type;
				if (!(type == "Plant"))
				{
					if (type == "Zombie")
					{
						towerDefenseLevelPreSpawnConfig.packetName = "VaseZombie";
					}
					else
					{
						towerDefenseLevelPreSpawnConfig.packetName = "VaseNormal";
					}
				}
				else
				{
					towerDefenseLevelPreSpawnConfig.packetName = "VasePlant";
				}
				if (vase.packetName != "")
				{
					towerDefenseLevelPreSpawnConfig.characterOverride = new TowerDefenseCharacterOverride();
					TowerDefenseCharacterPropertyChangeConfig towerDefenseCharacterPropertyChangeConfig = new TowerDefenseCharacterPropertyChangeConfig();
					towerDefenseCharacterPropertyChangeConfig.propertyName = "packetName";
					towerDefenseCharacterPropertyChangeConfig.value = vase.packetName;
					towerDefenseLevelPreSpawnConfig.characterOverride.propertyChange.Add(towerDefenseCharacterPropertyChangeConfig);
				}
				levelConfig.preSpawnList.Add(towerDefenseLevelPreSpawnConfig);
			}
			break;
		}
		switch (levelConfig.finishMethod)
		{
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE:
			levelConfig.izmManager = null;
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM:
			levelConfig.vaseManager = null;
			break;
		case TowerDefenseEnum.LEVEL_FINISH_METHOD.WAVE:
			levelConfig.izmManager = null;
			levelConfig.vaseManager = null;
			break;
		}
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

	public void LevelNumberSpinBoxValueChanged(double value)
	{
		levelConfig.levelNumber = (int)value;
	}

	public void LevelNameLineEditTextChanged(string newText)
	{
		levelConfig.levelName = newText;
	}

	public void LevelDescriptionLineEditTextChanged()
	{
		levelConfig.description = levelDescriptionTextEdit.Text;
	}

	public void HomeWorldOptionButtonItemSelected(long index)
	{
		string itemText = homeWorldOptionButton.GetItemText((int)index);
		levelConfig.homeWorld = (GeneralEnum.HOMEWORLD)homeWorldDictionary[itemText].AsInt32();
	}

	public void MapOptionButtonItemSelected(long index)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		string itemText = mapOptionButton.GetItemText((int)index);
		TowerDefenseMapConfig towerDefenseMapConfig = (TowerDefenseMapConfig)ResourceManager.Instance.MAPS[(string)mapDictionary[itemText]];
		Texture2D instance = null;
		if (GodotObject.IsInstanceValid(LevelEditorMapEditor.instance) && GodotObject.IsInstanceValid(LevelEditorMapEditor.instance.mapFeature))
		{
			TowerDefenseBattleFeatureMap mapFeature = LevelEditorMapEditor.instance.mapFeature;
			if (mapFeature.MapInit(towerDefenseMapConfig) && GodotObject.IsInstanceValid(mapFeature.mapControl) && GodotObject.IsInstanceValid(mapFeature.mapControl.editorSprite) && GodotObject.IsInstanceValid(mapFeature.mapControl.editorSprite.Texture))
			{
				LevelEditorMapEditor.instance.ApplyMapPreviewConfig(towerDefenseMapConfig);
				instance = mapFeature.mapControl.editorSprite.Texture;
			}
		}
		LevelEditorWaveEditor.Instance.MapChange(towerDefenseMapConfig);
		if (!GodotObject.IsInstanceValid(instance))
		{
			instance = towerDefenseMapConfig.GetMapTexture(cache: false);
		}
		SetMapTexture(instance);
		levelConfig.SetMapFromEditor(mapDictionary[itemText].AsString());
		fogbeginColumnSpinBox.MaxValue = towerDefenseMapConfig.gridNum.X;
	}

	public void BgmOptionButtonItemSelected(long index)
	{
		string itemText = bgmOptionButton.GetItemText((int)index);
		levelConfig.backgroundMusic = bgmDictionary[itemText].AsString();
	}

	public void FinishMethodOptionButtonItemSelected(long index)
	{
		string itemText = finishMethodOptionButton.GetItemText((int)index);
		levelConfig.SetFinishMethodFromEditor((TowerDefenseEnum.LEVEL_FINISH_METHOD)finishMethodDictionary[itemText].AsInt32());
		FreshFinishMethod();
	}

	public void TalkOptionButtonItemSelected(long index)
	{
		string itemText = talkOptionButton.GetItemText((int)index);
		levelConfig.talk = itemText;
	}

	public void TutorialOptionButtonItemSelected(long index)
	{
		string itemText = tutorialOptionButton.GetItemText((int)index);
		levelConfig.tutorial = itemText;
	}

	public void TalkCheckBoxToggled(bool toggledOn)
	{
		talkOptionButton.Disabled = toggledOn;
		levelConfig.isCustomTalk = toggledOn;
		if (toggledOn)
		{
			talkOptionButton.Selected = -1;
			levelConfig.talk = "";
		}
	}

	public void TutorialCheckBoxToggled(bool toggledOn)
	{
		tutorialOptionButton.Disabled = toggledOn;
		tutorialOptionButton.Selected = -1;
		if (toggledOn)
		{
			levelConfig.isCustomTutorial = toggledOn;
			levelConfig.tutorial = "";
		}
	}

	public void MowerUseCheckBoxToggled(bool toggledOn)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		levelConfig.mowerUse = toggledOn;
	}

	public void StormOpenCheckBoxToggled(bool toggledOn)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		levelConfig.stormOpen = toggledOn;
	}

	public void FogUseCheckBoxToggled(bool toggledOn)
	{
		if (!GodotObject.IsInstanceValid(levelConfig.fogManager))
		{
			levelConfig.fogManager = new TowerDefenseLevelFogManagerConfig();
		}
		levelConfig.fogManager.open = toggledOn;
		fogbeginColumnContainer.Visible = toggledOn;
	}

	public void FogBeginColumnSpinBoxValueChanged(double value)
	{
		if (!GodotObject.IsInstanceValid(levelConfig.fogManager))
		{
			levelConfig.fogManager = new TowerDefenseLevelFogManagerConfig();
		}
		levelConfig.fogManager.beginColumn = (int)value;
	}

	public void VaseShuffleCheckBoxToggled(bool toggledOn)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		if (GodotObject.IsInstanceValid(levelConfig.vaseManager))
		{
			levelConfig.vaseManager.shuffle = toggledOn;
		}
	}

	public void IZMShuffleCheckBoxToggled(bool toggledOn)
	{
		if (!isInit)
		{
			levelConfig.canExport = false;
		}
		if (GodotObject.IsInstanceValid(levelConfig.izmManager))
		{
			levelConfig.izmManager.shuffle = toggledOn;
		}
	}
}
