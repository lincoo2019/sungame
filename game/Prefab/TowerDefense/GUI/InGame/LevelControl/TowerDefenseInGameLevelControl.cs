using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class TowerDefenseInGameLevelControl : Control
{
	public static TowerDefenseInGameLevelControl instance;

	public RichTextLabel worldEntryLabel;

	public Label survivleLabel;

	private Label _tipsLabel;

	private AnimationPlayer _animationPlayer;

	public TowerDefenseLevelConfig config;

	public Vector2 awardPos = Vector2.Zero;

	public bool awardCreate;

	public bool hasSpawn;

	public void Init(TowerDefenseLevelConfig _config)
	{
		config = _config;
		config.Init();
		string text = Tr(config.description).Replace("{UserName}", GameSaveManager.Instance.GetUserCurrent());
		worldEntryLabel.Text = text;
	}

	public override void _Ready()
	{
		instance = this;
		worldEntryLabel = GetNode<RichTextLabel>("%WorldEntryLabel");
		survivleLabel = GetNode<Label>("%SurvivleLabel");
		_tipsLabel = GetNode<Label>("%TipsLabel");
		_tipsLabel.MouseFilter = MouseFilterEnum.Ignore;
		_animationPlayer = GetNode<AnimationPlayer>("%AnimationPlayer");
		worldEntryLabel.Visible = false;
	}

	public override void _ExitTree()
	{
		if (instance == this)
		{
			instance = null;
		}
		config = null;
		worldEntryLabel = null;
		survivleLabel = null;
		_tipsLabel = null;
		_animationPlayer = null;
	}

	public async Task ReadySetPlantPlay()
	{
		AudioManager.Instance.AudioPlay("ReadySetPlants");
		if (_animationPlayer != null)
		{
			string text = (TowerDefenseManager.Instance.IsIZM2Mode() ? "ReadyPlaceZombies" : "ReadySetPlants");
			_animationPlayer.Play(text);
		}
		await ToSignal(GetTree().CreateTimer(2.0, processAlways: false), SceneTreeTimer.SignalName.Timeout);
	}

	public async void TipsPlay(string text, double duration = 2.0)
	{
		if (_tipsLabel != null)
		{
			_tipsLabel.Visible = true;
			_tipsLabel.Text = text;
		}
		if (_animationPlayer != null)
		{
			_animationPlayer.Play("Tips");
		}
		await ToSignal(GetTree().CreateTimer(duration, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (_tipsLabel != null)
		{
			_tipsLabel.Visible = false;
		}
	}

	public async void AwardCreate(Vector2 pos)
	{
		if (awardCreate)
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (GodotObject.IsInstanceValid(currentControl) && currentControl.HasPendingBattleOperations)
		{
			return;
		}
		awardCreate = true;
		if (!Global.IsMultiplayerMode && GodotObject.IsInstanceValid(GameSaveManager.Instance) && config != null && !string.IsNullOrEmpty(config.name))
		{
			GameSaveManager.Instance.DeleteLevelProgress(config.name, currentControl?.ModLevelIdentity);
		}
		BattleEventBus.Instance.EmitGameVictory();
		Global global = Global.Instance;
		XWModLevelIdentity xWModLevelIdentity = currentControl?.ModLevelIdentity;
		if ((object)xWModLevelIdentity != null)
		{
			if (!Global.IsMultiplayerMode)
			{
				GameSaveManager.Instance.DeleteLevelProgress(xWModLevelIdentity.LevelSaveKey, xWModLevelIdentity);
				Dictionary level = XWModPlayerProgressService.GetLevel(xWModLevelIdentity);
				Dictionary dictionary = level.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
				dictionary["Finish"] = dictionary.GetValueOrDefault("Finish", 0).AsInt32() + 1;
				level["Key"] = dictionary;
				level[xWModLevelIdentity.Difficulty] = true;
				TowerDefenseBattleFeatureMower mowerFeature = TowerDefenseManager.Instance.GetMowerFeature();
				if (mowerFeature != null && !mowerFeature.mowerHasRun)
				{
					level["Mower"] = true;
				}
				XWModPlayerProgressService.SetLevel(xWModLevelIdentity, level);
			}
			global.currentAwardType = 0;
			TowerDefenseManager.Instance.CreateAward(TowerDefenseEnum.LEVEL_REWARDTYPE.TROPHY, "0", pos);
			return;
		}
		if ((Global.IsEditor && global.enterLevelMode == "DiyLevel") || global.enterLevelMode == "LoadLevel" || global.enterLevelMode == "OnlineLevel")
		{
			if (global.enterLevelMode == "OnlineLevel")
			{
				InternetServerManager.Instance.OnlineLevelPost(global.enterLevelId, "completion");
				Dictionary levelValue = GameSaveManager.Instance.GetLevelValue(config.name);
				if (!levelValue.ContainsKey("Key"))
				{
					levelValue["Key"] = new Dictionary();
				}
				Dictionary dictionary2 = levelValue["Key"].AsGodotDictionary();
				int num = dictionary2.GetValueOrDefault("Finish", 0).AsInt32();
				dictionary2["Finish"] = num + 1;
				GameSaveManager.Instance.SetKeyValue("CrystalNum", GameSaveManager.Instance.GetKeyValue("CrystalNum").AsInt32() + 1);
				GameSaveManager.Instance.SetLevelValue(config.name, levelValue);
				GameSaveManager.Instance.Save();
				if (global.enterLevelIsBattle)
				{
					global.enterLevelIsBattleFinish = true;
				}
			}
			if (global.enterLevelMode == "DiyLevel" && TowerDefenseManager.Instance.currentLevelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig)
			{
				towerDefenseLevelConfig.canExport = true;
			}
			if (Global.IsEditor && global.enterLevelMode == "DiyLevel")
			{
				DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("DialogBoxTips");
				dialogBoxBase.Set("text", "[center][font_size=24]您已完成关卡[/font_size][/center]");
				await ToSignal(dialogBoxBase, Node.SignalName.TreeExited);
				GetTree().Paused = false;
				Global.TimeScale = 1.0;
				SceneManager.Instance.ChangeScene("LevelEditorStage");
			}
			else
			{
				TowerDefenseManager.Instance.CreateAward(TowerDefenseEnum.LEVEL_REWARDTYPE.TROPHY, "250", pos);
			}
			return;
		}
		if (config.talk.AsString() != "")
		{
			NpcTalkConfig npcTalk = TowerDefenseManager.GetNpcTalk(config.talk.AsString());
			if (GodotObject.IsInstanceValid(npcTalk))
			{
				GameSaveManager.Instance.SetTutorialValue(npcTalk.saveKey, value: true);
			}
		}
		if (config.tutorial.AsString() != "")
		{
			TutorialConfig tutorial = TowerDefenseManager.GetTutorial(config.tutorial.AsString());
			if (GodotObject.IsInstanceValid(tutorial))
			{
				GameSaveManager.Instance.SetTutorialValue(tutorial.saveKey, value: true);
			}
		}
		bool flag = false;
		string text = GameSaveManager.Instance.GetKeyValue("CurrentDifficult").AsString();
		Dictionary levelValue2 = GameSaveManager.Instance.GetLevelValue(config.name);
		if (!levelValue2.ContainsKey("Key"))
		{
			levelValue2["Key"] = new Dictionary();
		}
		Dictionary dictionary3 = levelValue2["Key"].AsGodotDictionary();
		int num2 = dictionary3.GetValueOrDefault("Finish", 0).AsInt32();
		dictionary3["Finish"] = num2 + 1;
		TowerDefenseBattleFeatureMower mowerFeature2 = TowerDefenseManager.Instance.GetMowerFeature();
		if ((mowerFeature2 != null && !mowerFeature2.mowerHasRun) || TowerDefenseManager.Instance.IsIZM2Mode())
		{
			levelValue2["Mower"] = true;
		}
		if (!levelValue2.GetValueOrDefault(text, false).AsBool())
		{
			levelValue2[text] = true;
		}
		GameSaveManager.Instance.SetLevelValue(config.name, levelValue2);
		global.currentAwardType = (int)config.firstRewardType;
		switch (config.firstRewardType)
		{
		case TowerDefenseEnum.LEVEL_REWARDTYPE.PACKET:
			if (config.firstRewardValue.VariantType == Variant.Type.String)
			{
				string text2 = config.firstRewardValue.AsString();
				Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue(text2);
				if (!towerDefensePacketValue.GetValueOrDefault("Unlock", false).AsBool())
				{
					towerDefensePacketValue["Unlock"] = true;
					GameSaveManager.Instance.SetTowerDefensePacketValue(text2, towerDefensePacketValue);
					global.currentAwardValue = text2;
					flag = true;
				}
			}
			else
			{
				if (config.firstRewardValue.VariantType != Variant.Type.Array)
				{
					break;
				}
				foreach (Variant item in config.firstRewardValue.AsGodotArray())
				{
					string text3 = item.AsString();
					Dictionary towerDefensePacketValue2 = GameSaveManager.Instance.GetTowerDefensePacketValue(text3);
					if (!towerDefensePacketValue2.GetValueOrDefault("Unlock", false).AsBool())
					{
						towerDefensePacketValue2["Unlock"] = true;
						GameSaveManager.Instance.SetTowerDefensePacketValue(text3, towerDefensePacketValue2);
						if (!flag)
						{
							global.currentAwardValue = text3;
							flag = true;
						}
					}
				}
			}
			break;
		case TowerDefenseEnum.LEVEL_REWARDTYPE.COLLECTABLE:
			if (config.firstRewardValue.VariantType == Variant.Type.String)
			{
				string text4 = config.firstRewardValue.AsString();
				if (GameSaveManager.Instance.GetFeatureValue(text4) == 0)
				{
					GameSaveManager.Instance.SetFeatureValue(text4, true);
					global.currentAwardValue = text4;
					flag = true;
				}
			}
			else
			{
				if (config.firstRewardValue.VariantType != Variant.Type.Array)
				{
					break;
				}
				foreach (Variant item2 in config.firstRewardValue.AsGodotArray())
				{
					string text5 = item2.AsString();
					if (GameSaveManager.Instance.GetFeatureValue(text5) == 0)
					{
						GameSaveManager.Instance.SetFeatureValue(text5, true);
						if (!flag)
						{
							global.currentAwardValue = text5;
							flag = true;
						}
					}
				}
			}
			break;
		case TowerDefenseEnum.LEVEL_REWARDTYPE.COIN:
			if (config.firstRewardValue.VariantType == Variant.Type.String && dictionary3["Finish"].AsInt32() == 1 && !flag)
			{
				global.currentAwardValue = config.firstRewardValue.AsString();
				flag = true;
			}
			break;
		}
		if (!flag)
		{
			global.currentAwardType = 0;
			TowerDefenseManager.Instance.CreateAward(TowerDefenseEnum.LEVEL_REWARDTYPE.NOONE, "250", pos);
		}
		else
		{
			TowerDefenseManager.Instance.CreateAward(config.firstRewardType, global.currentAwardValue, pos);
		}
		GameSaveManager.Instance.Save();
	}
}
