using System.Collections.Generic;
using Cysharp.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class LevelEditorStage : Control
{
	private static ButtonGroup _levelEditorPanelButtonGroup;

	private static PackedScene _levelEditorMyLevelItem;

	private const string Path = "user://Csharp/Diy";

	public TextureRect LevelEditorStageTop;

	public TextureRect LevelEditorStageLeft;

	public TextureRect LevelEditorStageRight;

	public CanvasLayer LevelEditorChooseLayer;

	public CanvasLayer LevelEditorMyLevelLayer;

	public CanvasLayer LevelEditorOnlineLevelLayer;

	public CanvasLayer LevelEditorLayer;

	public CanvasLayer LevelEditorBattleLayer;

	public CanvasLayer LevelEditorQuizLayer;

	public LevelEditorInformationEditor LevelEditorInformationEditor;

	public LevelEditorEventEditor LevelEditorEventEditor;

	public LevelEditorMapEditor LevelEditorMapEditor;

	public LevelEditorSeedbankEditor LevelEditorSeedbankEditor;

	public LevelEditorWaveEditor LevelEditorWaveEditor;

	public LevelEditorBattle LevelEditorBattle;

	public Control LevelEditorQuiz;

	public MainButton LevelInformationButton;

	public MainButton LevelEventButton;

	public MainButton LevelMapButton;

	public MainButton LevelSeedbankButton;

	public MainButton LevelWaveButton;

	public HFlowContainer MyLevelContainer;

	public LevelEditorOnlineLevel LevelEditorOnlineLevel;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelConfig LevelConfig;

	public static bool ShowTips;

	public string CurrentUid = "";

	public Array<string> LevelUid = new Array<string>();

	public bool Skip;

	public bool Over;

	private static ButtonGroup LevelEditorPanelButtonGroup => _levelEditorPanelButtonGroup ?? (_levelEditorPanelButtonGroup = GD.Load<ButtonGroup>("uid://clop83geu1mo3"));

	private static PackedScene LevelEditorMyLevelItem => _levelEditorMyLevelItem ?? (_levelEditorMyLevelItem = GD.Load<PackedScene>("uid://jtdmoji4acxv"));

	public void Init(TowerDefenseLevelConfig _levelConfig)
	{
		LevelConfig = _levelConfig;
		LevelEditorInformationEditor.Init(LevelConfig);
		LevelEditorEventEditor.Init(LevelConfig);
		LevelEditorMapEditor.Init(LevelConfig);
		LevelEditorSeedbankEditor.Init(LevelConfig);
		LevelEditorWaveEditor.Init(LevelConfig);
	}

	public override void _EnterTree()
	{
		LevelEditorStageTop = GetNode<TextureRect>("%LevelEditorStageTop");
		LevelEditorStageLeft = GetNode<TextureRect>("%LevelEditorStageLeft");
		LevelEditorStageRight = GetNode<TextureRect>("%LevelEditorStageRight");
		LevelEditorChooseLayer = GetNode<CanvasLayer>("%LevelEditorChooseLayer");
		LevelEditorMyLevelLayer = GetNode<CanvasLayer>("%LevelEditorMyLevelLayer");
		LevelEditorOnlineLevelLayer = GetNode<CanvasLayer>("%LevelEditorOnlineLevelLayer");
		LevelEditorLayer = GetNode<CanvasLayer>("%LevelEditorLayer");
		LevelEditorBattleLayer = GetNode<CanvasLayer>("%LevelEditorBattleLayer");
		LevelEditorQuizLayer = GetNode<CanvasLayer>("%LevelEditorQuizLayer");
		LevelEditorInformationEditor = GetNode<LevelEditorInformationEditor>("%LevelEditorInformationEditor");
		LevelEditorEventEditor = GetNode<LevelEditorEventEditor>("%LevelEditorEventEditor");
		LevelEditorMapEditor = GetNode<LevelEditorMapEditor>("%LevelEditorMapEditor");
		LevelEditorSeedbankEditor = GetNode<LevelEditorSeedbankEditor>("%LevelEditorSeedbankEditor");
		LevelEditorWaveEditor = GetNode<LevelEditorWaveEditor>("%LevelEditorWaveEditor");
		LevelEditorBattle = GetNode<LevelEditorBattle>("%LevelEditorBattle");
		LevelEditorQuiz = GetNode<Control>("%LevelEditorQuiz");
		LevelInformationButton = GetNode<MainButton>("%LevelInformationButton");
		LevelEventButton = GetNode<MainButton>("%LevelEventButton");
		LevelMapButton = GetNode<MainButton>("%LevelMapButton");
		LevelSeedbankButton = GetNode<MainButton>("%LevelSeedbankButton");
		LevelWaveButton = GetNode<MainButton>("%LevelWaveButton");
		MyLevelContainer = GetNode<HFlowContainer>("%MyLevelContainer");
		LevelEditorOnlineLevel = GetNode<LevelEditorOnlineLevel>("%LevelEditorOnlineLevel");
		if (Global.IsEditor && Global.Instance.enterLevelMode == "DiyLevel")
		{
			Skip = true;
			CurrentUid = Global.Instance.currentDiyLevelUid;
			Global.Instance.enterLevelMode = "";
		}
		Global.Instance.isEditor = true;
	}

	public override void _Ready()
	{
		AudioManager.Instance.AudioPlay("ZenGarden", AudioManagerEnum.TYPE.MUSIC);
		if (!DirAccess.DirExistsAbsolute("user://Csharp/Diy"))
		{
			DirAccess.MakeDirRecursiveAbsolute("user://Csharp/Diy");
		}
		string[] filesAt = DirAccess.GetFilesAt("user://Csharp/Diy");
		foreach (string instance in filesAt)
		{
			LevelUid.Add(instance.GetBaseName());
		}
		AnimeEnter();
		LevelInformationButton.ButtonGroup = LevelEditorPanelButtonGroup;
		LevelEventButton.ButtonGroup = LevelInformationButton.ButtonGroup;
		LevelMapButton.ButtonGroup = LevelInformationButton.ButtonGroup;
		LevelSeedbankButton.ButtonGroup = LevelInformationButton.ButtonGroup;
		LevelWaveButton.ButtonGroup = LevelInformationButton.ButtonGroup;
		if (Skip)
		{
			if (TowerDefenseManager.Instance.currentLevelConfig is TowerDefenseLevelConfig levelConfig)
			{
				Init(levelConfig);
				LevelDiyButtonPressed(init: false);
			}
			else
			{
				GD.PushError("[LevelEditorStage] DIY level configuration is missing or incompatible.");
			}
		}
		else if (!ShowTips)
		{
			DialogManager.Instance.DialogCreate("LevelEditorTips");
			ShowTips = true;
		}
		if (Global.Instance.enterLevelIsBattle)
		{
			if (!Global.Instance.enterLevelIsBattleFinish)
			{
				GameSaveManager.Instance.SetKeyValue("LevelEditorBattleFailNum", GameSaveManager.Instance.GetKeyValue("LevelEditorBattleFailNum").AsInt32() + 1);
			}
			else
			{
				GameSaveManager.Instance.SetKeyValue("LevelEditorBattleFinishNum", GameSaveManager.Instance.GetKeyValue("LevelEditorBattleFinishNum").AsInt32() + 1);
			}
			GameSaveManager.Instance.SetKeyValue("LevelEditorBattleCurrentLevel", "-1");
			GameSaveManager.Instance.Save();
			Global.Instance.enterLevelIsBattleFinish = false;
			Global.Instance.enterLevelIsBattle = false;
			LevelBattleButtonPressed();
		}
		else if (Global.Instance.enterLevelId != "-1")
		{
			LevelOnlineButtonPressed();
		}
		if (Global.Instance.enterLevelId != "-1")
		{
			OnlineLevelPreview onlineLevelPreview = (OnlineLevelPreview)DialogManager.Instance.DialogCreate("OnlineLevelPreview");
			if (LevelEditorOnlineLevelLayer.Visible)
			{
				onlineLevelPreview.OnAuthorSelected += LevelEditorOnlineLevel.OpenAuthorLevels;
			}
			onlineLevelPreview.InitDialog(Global.Instance.enterLevelId);
			onlineLevelPreview.OnSelect += delegate(string url)
			{
				LevelEditorOnlineLevel.EnterLevel(url, Global.Instance.enterLevelId);
			};
			Global.Instance.enterLevelId = "-1";
		}
		GetNode<BaseButton>("GUI/LevelEditorChooseLayer/LevelEditorChooseButtonNode/VBoxContainer/LevelMineButton").Pressed += LevelMineButtonPressed;
		GetNode<BaseButton>("GUI/LevelEditorChooseLayer/LevelEditorChooseButtonNode/VBoxContainer/LevelOnlineButton").Pressed += LevelOnlineButtonPressed;
		GetNode<BaseButton>("GUI/LevelEditorChooseLayer/LevelEditorChooseButtonNode/VBoxContainer/LevelDiyButton").Pressed += delegate
		{
			LevelDiyButtonPressed();
		};
		GetNode<BaseButton>("GUI/LevelEditorChooseLayer/LevelEditorChooseButtonNode/VBoxContainer/LevelLoadButton").Pressed += LevelLoadButtonPressed;
		GetNode<BaseButton>("GUI/LevelEditorChooseLayer/LevelBattleButton").Pressed += LevelBattleButtonPressed;
		GetNode<BaseButton>("GUI/LevelEditorChooseLayer/LevelQuizButton").Pressed += LevelQuizButtonPressed;
		GetNode<MainButton>("GUI/LevelEditorChooseLayer/ModLevelsButton").Pressed += ModLevelsButtonPressed;
		GetNode<BaseButton>("%LevelInformationButton").Pressed += PanelChange;
		GetNode<BaseButton>("%LevelEventButton").Pressed += PanelChange;
		GetNode<BaseButton>("%LevelMapButton").Pressed += PanelChange;
		GetNode<BaseButton>("%LevelSeedbankButton").Pressed += PanelChange;
		GetNode<BaseButton>("%LevelWaveButton").Pressed += PanelChange;
		GetNode<BaseButton>("%LevelTestButton").Pressed += LevelTestButtonPressed;
		GetNode<BaseButton>("%LevelSaveButton").Pressed += Save;
		GetNode<BaseButton>("%LevelExportButton").Pressed += Export;
		GetNode<BaseButton>("GUI/ButtonLayer/LevelEditorBackButton").Pressed += LevelEditorBackButtonPressed;
	}

	private void ModLevelsButtonPressed()
	{
		if (!Over && LevelEditorChooseLayer.Visible)
		{
			Over = true;
			XWModLevelSession.Clear();
			Global.Instance.isEditor = false;
			Global.Instance.currentLevelChoose = "__ModLevels";
			Global.Instance.currentChapterId = -1;
			Global.Instance.currentLevelId = -1;
			SceneManager.Instance.ChangeScene("LevelChoose");
		}
	}

	public void PanelChange()
	{
		LevelEditorInformationEditor.Visible = LevelInformationButton.ButtonPressed;
		LevelEditorEventEditor.Visible = LevelEventButton.ButtonPressed;
		LevelEditorMapEditor.Visible = LevelMapButton.ButtonPressed;
		LevelEditorSeedbankEditor.Visible = LevelSeedbankButton.ButtonPressed;
		LevelEditorWaveEditor.Visible = LevelWaveButton.ButtonPressed;
	}

	public void LevelMineButtonPressed()
	{
		LevelEditorChooseLayer.Visible = false;
		LevelEditorMyLevelLayer.Visible = true;
		AnimeExit();
		LevelUid.Clear();
		foreach (Node child in MyLevelContainer.GetChildren())
		{
			child.QueueFree();
		}
		List<System.Collections.Generic.Dictionary<string, Variant>> list = new List<System.Collections.Generic.Dictionary<string, Variant>>();
		string[] filesAt = DirAccess.GetFilesAt("user://Csharp/Diy");
		foreach (string text in filesAt)
		{
			ulong modifiedTime = FileAccess.GetModifiedTime("user://Csharp/Diy".PathJoin(text));
			System.Collections.Generic.Dictionary<string, Variant> item = new System.Collections.Generic.Dictionary<string, Variant>
			{
				["fileName"] = text,
				["fileTime"] = modifiedTime
			};
			list.Add(item);
		}
		list.Sort((System.Collections.Generic.Dictionary<string, Variant> a, System.Collections.Generic.Dictionary<string, Variant> b) => (int)((long)b["fileTime"] - (long)a["fileTime"]));
		foreach (System.Collections.Generic.Dictionary<string, Variant> item2 in list)
		{
			LevelEditorMyLevelItem levelEditorMyLevelItem = LevelEditorMyLevelItem.Instantiate<LevelEditorMyLevelItem>(PackedScene.GenEditState.Disabled);
			levelEditorMyLevelItem.OnSelect += MyLevelSelect;
			levelEditorMyLevelItem.OnDelete += MyLevelDelete;
			MyLevelContainer.AddChild(levelEditorMyLevelItem, forceReadableName: false, InternalMode.Disabled);
			levelEditorMyLevelItem.Init(item2["fileName"].AsString().GetBaseName());
			LevelUid.Add(item2["fileName"].AsString().GetBaseName());
		}
	}

	public static bool MigrateDiyLevelTresIfNeeded(string filePath)
	{
		return ResourceTextScriptMigration.MigrateLevelEditorResourceForLoadIfNeeded(filePath);
	}

	public void MyLevelSelect(string uid)
	{
		LevelEditorMyLevelLayer.Visible = false;
		string text = ZString.Concat("user://Csharp/Diy", "/", uid, ".tres");
		if (FileAccess.FileExists(text) && (MigrateDiyLevelTresIfNeeded(text) ? ResourceLoader.Load(text, "", ResourceLoader.CacheMode.Ignore) : GD.Load(text)) is TowerDefenseLevelConfig levelConfig)
		{
			CurrentUid = uid;
			Init(levelConfig);
			LevelDiyButtonPressed(init: false, isLoad: true);
		}
	}

	public void MyLevelDelete(string uid)
	{
		string path = ZString.Concat("user://Csharp/Diy", "/", uid, ".tres");
		if (FileAccess.FileExists(path))
		{
			DirAccess.RemoveAbsolute(path);
			LevelUid.Remove(uid);
		}
	}

	public void LevelOnlineButtonPressed()
	{
		LevelEditorChooseLayer.Visible = false;
		LevelEditorOnlineLevelLayer.Visible = true;
		AnimeExit();
		LevelEditorOnlineLevel.GetPage(Mathf.Max(1, LevelEditorOnlineLevel.currentPage));
	}

	public void LevelDiyButtonPressed(bool init = true, bool isLoad = false)
	{
		if (init)
		{
			TowerDefenseLevelConfig levelConfig = new TowerDefenseLevelConfig();
			LevelEditorChooseLayer.Visible = false;
			LevelEditorLayer.Visible = true;
			uint num = GD.Randi();
			while (LevelUid.Contains(num.ToString()))
			{
				num = GD.Randi();
			}
			LevelUid.Add(num.ToString());
			CurrentUid = num.ToString();
			Init(levelConfig);
			Save();
			if (!isLoad)
			{
				AnimeExit();
			}
		}
		else
		{
			LevelEditorChooseLayer.Visible = false;
			LevelEditorLayer.Visible = true;
			if (!isLoad)
			{
				AnimeExit();
			}
		}
	}

	public void LevelLoadButtonPressed()
	{
		CommandManager.Instance.LoadLevelButtonPressed();
	}

	public void LevelBattleButtonPressed()
	{
		TowerDefenseManager.Instance.coinBank.ShowCoinBank(new Vector2(450f, 557f), still: true);
		LevelEditorChooseLayer.Visible = false;
		LevelEditorBattle._Show();
		LevelEditorBattleLayer.Visible = true;
	}

	public void LevelQuizButtonPressed()
	{
		LevelEditorQuizLayer.Visible = true;
		LevelEditorChooseLayer.Visible = false;
	}

	public void LevelTestButtonPressed()
	{
		if (!Over)
		{
			Save();
			Over = true;
			Global.Instance.enterLevelMode = "DiyLevel";
			Global.Instance.currentDiyLevelUid = CurrentUid;
			LevelEditorLayer.Visible = false;
			TowerDefenseManager.Instance.currentLevelConfig = LevelConfig;
			SceneManager.Instance.ChangeScene("TowerDefense");
		}
	}

	public async void Save()
	{
		if (Over)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(LevelEditorWaveEditor) && LevelEditorWaveEditor.isLoading)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (GodotObject.IsInstanceValid(LevelEditorWaveEditor) && LevelEditorWaveEditor.isLoading)
			{
				return;
			}
		}
		LevelEditorEventEditor.Save();
		LevelEditorMapEditor.Save(isSave: true);
		LevelEditorSeedbankEditor.Save();
		LevelEditorWaveEditor.Save();
		LevelConfig.PrepareForEditorSave();
		ResourceSaver.Save(LevelConfig, ZString.Concat("user://Csharp/Diy", "/", CurrentUid, ".tres"), ResourceSaver.SaverFlags.None);
	}

	public void Export()
	{
		Save();
		DisplayServer.FileDialogShow("另存为项目", "", "", showHidden: false, DisplayServer.FileDialogMode.SaveFile, new string[1] { "*.json" }, Callable.From<bool, string[], int>(SaveFileTo));
	}

	private void SaveFileTo(bool status, string[] selectedPaths, int selectedFilterIndex)
	{
		if (selectedPaths.Length >= 1)
		{
			string text = selectedPaths[0];
			if (text.GetExtension() != "json")
			{
				text += ".json";
			}
			Save();
			FileAccess fileAccess = FileAccess.Open(text, FileAccess.ModeFlags.WriteRead);
			if (fileAccess != null)
			{
				fileAccess.StoreString(Json.Stringify(LevelConfig.Export()));
				fileAccess.Close();
			}
		}
	}

	public void LevelEditorBackButtonPressed()
	{
		if (LevelEditorChooseLayer.Visible)
		{
			Global.Instance.isEditor = false;
			Global.Instance.enterLevelIsBattle = false;
			Global.Instance.enterLevelIsBattleFinish = false;
			SceneManager.Instance.ChangeScene("MainMenu");
		}
		else if (LevelEditorMyLevelLayer.Visible)
		{
			AnimeEnter();
			LevelEditorChooseLayer.Visible = true;
			LevelEditorMyLevelLayer.Visible = false;
		}
		else if (LevelEditorOnlineLevelLayer.Visible)
		{
			if (!LevelEditorOnlineLevel.TryReturnToLevelList())
			{
				AnimeEnter();
				LevelEditorChooseLayer.Visible = true;
				LevelEditorOnlineLevelLayer.Visible = false;
			}
		}
		else if (LevelEditorLayer.Visible)
		{
			LevelInformationButton.ButtonPressed = true;
			PanelChange();
			Save();
			LevelEditorInformationEditor.Clear();
			LevelEditorEventEditor.Clear();
			LevelEditorMapEditor.Clear();
			LevelEditorSeedbankEditor.Clear();
			LevelEditorWaveEditor.Clear();
			AnimeEnter();
			LevelEditorChooseLayer.Visible = true;
			LevelEditorLayer.Visible = false;
		}
		else if (LevelEditorBattleLayer.Visible)
		{
			if (!LevelEditorBattle.overNode.Visible)
			{
				if (LevelEditorBattle.tipsNode.Visible)
				{
					TowerDefenseManager.Instance.coinBank.ShowCoinBank(new Vector2(450f, 557f), still: true);
					LevelEditorBattle.tipsNode.Visible = false;
					LevelEditorBattle.mainNode.Visible = true;
				}
				else if (LevelEditorBattle.awardNode.Visible)
				{
					TowerDefenseManager.Instance.coinBank.ShowCoinBank(new Vector2(450f, 557f), still: true);
					LevelEditorBattle.awardNode.Visible = false;
					LevelEditorBattle.mainNode.Visible = true;
				}
				else if (LevelEditorBattle.historyNode.Visible)
				{
					TowerDefenseManager.Instance.coinBank.ShowCoinBank(new Vector2(450f, 557f), still: true);
					LevelEditorBattle.historyNode.Visible = false;
					LevelEditorBattle.mainNode.Visible = true;
				}
				else
				{
					TowerDefenseManager.Instance.coinBank.CallDeferred("StartHide");
					LevelEditorChooseLayer.Visible = true;
					LevelEditorBattleLayer.Visible = false;
				}
			}
		}
		else if (LevelEditorQuizLayer.Visible && !(bool)LevelEditorQuiz.Get("mapChooseOver"))
		{
			LevelEditorQuizLayer.Visible = false;
			LevelEditorChooseLayer.Visible = true;
		}
	}

	public void AnimeExit()
	{
		Rect2 viewportRect = GetViewportRect();
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetTrans(Tween.TransitionType.Quart);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(LevelEditorStageTop, "global_position", new Vector2(0f, -120f), 0.5);
		tween.TweenProperty(LevelEditorStageLeft, "global_position", new Vector2(-300f, 0f), 0.5);
		tween.TweenProperty(LevelEditorStageRight, "global_position", new Vector2(viewportRect.Position.X + viewportRect.Size.X + 300f, 0f), 0.5);
	}

	public void AnimeEnter()
	{
		Rect2 viewportRect = GetViewportRect();
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.SetTrans(Tween.TransitionType.Quart);
		tween.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(LevelEditorStageTop, "global_position", new Vector2(0f, 0f), 0.5);
		tween.TweenProperty(LevelEditorStageLeft, "global_position", new Vector2(0f, 0f), 0.5);
		tween.TweenProperty(LevelEditorStageRight, "global_position", new Vector2(viewportRect.Position.X + viewportRect.Size.X - 200f, 0f), 0.5);
	}
}
