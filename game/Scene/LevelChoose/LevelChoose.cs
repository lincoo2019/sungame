using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class LevelChoose : Control
{
	public enum LevelChooseMode
	{
		Chapter,
		Level,
		Option
	}

	private static PackedScene _LevelSelectItem;

	private static PackedScene _DragMenuSelectItemLevel;

	private static PackedScene _DragMenuSelectItemChapter;

	public Camera2D camera;

	public Marker2D levelChooseMarker;

	public Marker2D defaultMarker;

	public CanvasLayer gui;

	public CanvasLayer guiFollow;

	public CanvasLayer optionGUI;

	public Label informationLabel;

	public Node chapterMenu;

	public DragMenu levelMenu;

	public TextureButton normalButton;

	public TextureButton difficultButton;

	public Label normalLabel;

	public Label difficultLabel;

	public CheckBox levelMenuFlattenCheckBox;

	public TextureRect chapterTexture;

	public TextureRect chapterSelectBackground;

	public Control levelMenuFlatten;

	public HFlowContainer levelContainer;

	public Array currentChapterList;

	public Dictionary currentChapter;

	public int currentChapterIndex = -1;

	public LevelChooseMode currentMode;

	public LevelChooseMode saveBackMode;

	public Tween tween;

	private bool _showingModCatalogs;

	private string _modDifficulty = "Normal";

	private static PackedScene levelSelectItem => _LevelSelectItem ?? (_LevelSelectItem = GD.Load<PackedScene>("uid://cp8mgtcrcrxwf"));

	private static PackedScene dragMenuSelectItemLevel => _DragMenuSelectItemLevel ?? (_DragMenuSelectItemLevel = GD.Load<PackedScene>("uid://bftr1dw41t3pu"));

	private static PackedScene dragMenuSelectItemChapter => _DragMenuSelectItemChapter ?? (_DragMenuSelectItemChapter = GD.Load<PackedScene>("uid://rsb7ubyly6ta"));

	private bool IsModBrowser => Global.Instance.currentLevelChoose == "__ModLevels";

	private Dictionary ReadProgress(string key)
	{
		if (!IsModBrowser)
		{
			return GameSaveManager.Instance.GetLevelValue(key);
		}
		return XWModPlayerProgressService.GetLevel(XWModLevelSession.ForLevel(key, _modDifficulty));
	}

	private XWModLevelIdentity IdentityFor(string key)
	{
		if (!IsModBrowser)
		{
			return null;
		}
		return XWModLevelSession.ForLevel(key, _modDifficulty);
	}

	public override void _Ready()
	{
		camera = GetNode<Camera2D>("%Camera");
		levelChooseMarker = GetNode<Marker2D>("%LevelChooseMarker");
		defaultMarker = GetNode<Marker2D>("%DefaultMarker");
		gui = GetNode<CanvasLayer>("%GUI");
		guiFollow = GetNode<CanvasLayer>("%GUIFollow");
		optionGUI = GetNode<CanvasLayer>("%OptionGUI");
		informationLabel = GetNode<Label>("%InformationLabel");
		chapterMenu = GetNode("%ChapterMenu");
		levelMenu = GetNode<DragMenu>("%LevelMenu");
		normalButton = GetNode<TextureButton>("%NormalButton");
		difficultButton = GetNode<TextureButton>("%DifficultButton");
		normalLabel = GetNode<Label>("%NormalLabel");
		difficultLabel = GetNode<Label>("%DifficultLabel");
		levelMenuFlattenCheckBox = GetNode<CheckBox>("%LevelMenuFlattenCheckBox");
		chapterTexture = GetNode<TextureRect>("%ChapterTexture");
		chapterSelectBackground = GetNode<TextureRect>("%ChapterSelectBackground");
		levelMenuFlatten = GetNode<Control>("%LevelMenuFlatten");
		levelContainer = GetNode<HFlowContainer>("%LevelContainer");
		AudioManager.Instance.AudioPlay("ZenGarden", AudioManagerEnum.TYPE.MUSIC);
		if (IsModBrowser)
		{
			_modDifficulty = XWModLevelSession.Current?.Difficulty ?? "Normal";
			currentChapterList = new Array();
			ShowModCatalogs();
		}
		else
		{
			XWModLevelSession.Clear();
			currentChapterList = (Array)ResourceManager.Instance.LEVELS[Global.Instance.currentLevelChoose]["Chapter"];
		}
		InitChapter();
		if (!IsModBrowser && Global.Instance.currentAwardMode)
		{
			Global.Instance.currentAwardMode = false;
			int num = (int)GameSaveManager.Instance.GetKeyValue("AdventureChapterIndex");
			if (num != -1)
			{
				Select(num);
			}
		}
		else if (!IsModBrowser && Global.Instance.currentChapterId != -1 && Global.Instance.enterLevelMode == "LevelChoose")
		{
			Select(Global.Instance.currentChapterId);
		}
		normalLabel.Modulate = Colors.White;
		difficultLabel.Modulate = Colors.White;
		string text = (IsModBrowser ? _modDifficulty : GameSaveManager.Instance.GetKeyValue("CurrentDifficult").AsString());
		if (!(text == "Normal"))
		{
			if (text == "Difficult")
			{
				difficultButton.ButtonPressed = true;
				difficultLabel.Modulate = Colors.OrangeRed;
			}
			else
			{
				GameSaveManager.Instance.SetKeyValue("CurrentDifficult", "Normal");
				normalButton.ButtonPressed = true;
				normalLabel.Modulate = Colors.Green;
			}
		}
		else
		{
			normalButton.ButtonPressed = true;
			normalLabel.Modulate = Colors.Green;
		}
		levelMenuFlattenCheckBox.ButtonPressed = GameSaveManager.Instance.GetKeyValue("LevelMenuFlatten").AsBool();
		if (levelMenuFlattenCheckBox.ButtonPressed)
		{
			levelMenu.Visible = false;
			levelMenuFlatten.Visible = true;
			gui.FollowViewportEnabled = true;
		}
		else
		{
			levelMenu.Visible = true;
			levelMenuFlatten.Visible = false;
			gui.FollowViewportEnabled = false;
		}
		GetNode<SpriteBrightButton>("GUI/OptionButton").OnPressed += OptionButtonPressed;
		GetNode<BaseButton>("%NormalButton").Pressed += DifficultChange;
		GetNode<BaseButton>("%DifficultButton").Pressed += DifficultChange;
		GetNode<CheckBox>("%LevelMenuFlattenCheckBox").Toggled += LevelMenuFlattenCheckBoxToggled;
		GetNode<BaseButton>("%BackButton").Pressed += Back;
	}

	public void InitChapter()
	{
		for (int i = 0; i < currentChapterList.Count; i++)
		{
			Dictionary dictionary = (Dictionary)currentChapterList[i];
			DragMenuSelectItemChapter dragMenuSelectItemChapter = LevelChoose.dragMenuSelectItemChapter.Instantiate<DragMenuSelectItemChapter>(PackedScene.GenEditState.Disabled);
			dragMenuSelectItemChapter.index = i;
			dragMenuSelectItemChapter.OnSelect += Select;
			chapterMenu.AddChild(dragMenuSelectItemChapter, forceReadableName: false, InternalMode.Disabled);
			dragMenuSelectItemChapter.Init(dictionary, IdentityFor(""));
			if (dictionary["OpenKey"].AsString() == "")
			{
				if (dictionary["UnlockImage"].AsString().Length > 0)
				{
					dragMenuSelectItemChapter.sprite.Texture = GD.Load<Texture2D>(dictionary["UnlockImage"].AsString());
				}
				continue;
			}
			if (ReadProgress(dictionary["OpenKey"].AsString()).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Finish", 0)
				.AsInt32() > 0 || CommandManager.Instance.debugOpenAllLevel)
			{
				if (dictionary["UnlockImage"].AsString().Length > 0)
				{
					dragMenuSelectItemChapter.sprite.Texture = GD.Load<Texture2D>(dictionary["UnlockImage"].AsString());
				}
				continue;
			}
			if (dictionary["LockImage"].AsString().Length > 0)
			{
				dragMenuSelectItemChapter.sprite.Texture = GD.Load<Texture2D>(dictionary["LockImage"].AsString());
			}
			dragMenuSelectItemChapter.@lock = true;
		}
	}

	public void InitLevel(int chapterId)
	{
		currentChapter = (Dictionary)currentChapterList[chapterId];
		if (currentChapter.GetValueOrDefault("Background", "").AsString() != "")
		{
			chapterTexture.Texture = GD.Load<Texture2D>(currentChapter["Background"].AsString());
		}
		if (currentChapter.GetValueOrDefault("Building", "").AsString() != "")
		{
			chapterSelectBackground.Texture = GD.Load<Texture2D>(currentChapter["Building"].AsString());
		}
		int num = currentChapter.GetValueOrDefault("PreOpen", 0).AsInt32() - 1;
		Array array = (Array)currentChapter["Level"];
		for (int i = 0; i < array.Count; i++)
		{
			Dictionary dictionary = (Dictionary)array[i];
			if (dictionary["OpenKey"].AsString() == "Lock")
			{
				continue;
			}
			Dictionary dictionary2 = ReadProgress(dictionary["OpenKey"].AsString()).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
			if (!CommandManager.Instance.debugOpenAllLevel && !(dictionary["OpenKey"].AsString() == "") && dictionary2.GetValueOrDefault("Finish", 0).AsInt32() <= 0)
			{
				if (num <= 0)
				{
					continue;
				}
				num--;
			}
			DragMenuSelectItemlevel dragMenuSelectItemlevel = dragMenuSelectItemLevel.Instantiate<DragMenuSelectItemlevel>(PackedScene.GenEditState.Disabled);
			dragMenuSelectItemlevel.index = i;
			dragMenuSelectItemlevel.OnSelect += Select;
			levelMenu.AddChild(dragMenuSelectItemlevel, forceReadableName: false, InternalMode.Disabled);
			if (((Dictionary)((Array)currentChapter["Level"])[i])["SaveKey"].AsString() != "")
			{
				dragMenuSelectItemlevel.Init(dictionary["SaveKey"].AsString(), IdentityFor(dictionary["SaveKey"].AsString()));
			}
			if (dictionary["UnlockImage"].AsString().Length > 0)
			{
				dragMenuSelectItemlevel.sprite.Texture = GD.Load<Texture2D>(dictionary["UnlockImage"].AsString());
			}
			DragMenuSelectItemlevel dragMenuSelectItemlevel2 = dragMenuSelectItemLevel.Instantiate<DragMenuSelectItemlevel>(PackedScene.GenEditState.Disabled);
			dragMenuSelectItemlevel2.index = i;
			dragMenuSelectItemlevel2.OnSelect += Select;
			levelContainer.AddChild(dragMenuSelectItemlevel2, forceReadableName: false, InternalMode.Disabled);
			dragMenuSelectItemlevel2.graphics.Scale = Vector2.One * 0.5f;
			if (((Dictionary)((Array)currentChapter["Level"])[i])["SaveKey"].AsString() != "")
			{
				dragMenuSelectItemlevel2.Init(dictionary["SaveKey"].AsString(), IdentityFor(dictionary["SaveKey"].AsString()));
			}
			if (dictionary["UnlockImage"].AsString().Length > 0)
			{
				dragMenuSelectItemlevel2.sprite.Texture = GD.Load<Texture2D>(dictionary["UnlockImage"].AsString());
			}
		}
		int num2 = (IsModBrowser ? (-1) : GameSaveManager.Instance.GetKeyValue($"AdventureChapter{chapterId + 1}Index").AsInt32());
		if (num2 != -1)
		{
			levelMenu.CallDeferred("SetPos", num2);
		}
	}

	public async void Select(int id)
	{
		if (GodotObject.IsInstanceValid(tween) && tween.IsRunning())
		{
			return;
		}
		switch (currentMode)
		{
		case LevelChooseMode.Chapter:
			if (((Dictionary)currentChapterList[id]).GetValueOrDefault("Lock", false).AsBool())
			{
				DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", "[center][font_size=24]敬请期待[/font_size][/center]");
				break;
			}
			currentChapterIndex = id;
			Global.Instance.currentChapterId = id;
			if (!IsModBrowser)
			{
				GameSaveManager.Instance.SetKeyValue("AdventureChapterIndex", id);
			}
			chapterMenu.Set("alive", false);
			tween = camera.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Quad);
			tween.TweenProperty(camera, "global_position:y", levelChooseMarker.GlobalPosition.Y, 0.5);
			currentMode = LevelChooseMode.Level;
			InitLevel(id);
			informationLabel.Text = currentChapter["Name"].AsString();
			break;
		case LevelChooseMode.Level:
		{
			ResourceManager.Instance.RequireFullGameplayResourcesReady("Select");
			if (IsModBrowser)
			{
				await LaunchModLevel(id);
				break;
			}
			string text = GameSaveManager.Instance.GetKeyValue("CurrentDifficult").AsString();
			chapterMenu.Set("alive", true);
			GameSaveManager.Instance.SetKeyValue($"AdventureChapter{currentChapterIndex + 1}Index", id);
			GameSaveManager.Instance.Save();
			Dictionary dictionary = (Dictionary)((Array)currentChapter["Level"])[id];
			Dictionary dictionary2 = (Dictionary)dictionary["Level"];
			if (dictionary2[text].AsString() != "")
			{
				TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>(dictionary2[text].AsString());
			}
			else
			{
				TowerDefenseManager.Instance.currentLevelConfig = GD.Load<TowerDefenseLevelConfig>(dictionary2["Normal"].AsString());
			}
			Global.Instance.currentLevelId = id;
			Global.Instance.enterLevelMode = "LevelChoose";
			if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				MultiPlayerManager.Instance.SendSelectLevel(dictionary["SaveKey"].AsString());
				await MultiPlayerManager.Instance.StartCompatibleGameAsync();
			}
			else
			{
				SceneManager.Instance.ChangeScene("TowerDefense");
			}
			break;
		}
		}
	}

	public async void Back()
	{
		if (IsModBrowser && currentMode == LevelChooseMode.Chapter)
		{
			if (_showingModCatalogs)
			{
				XWModLevelSession.Clear();
				SceneManager.Instance.ChangeScene("LevelEditorStage");
			}
			else
			{
				ShowModCatalogs();
			}
			return;
		}
		switch (currentMode)
		{
		case LevelChooseMode.Chapter:
			GameSaveManager.Instance.Save();
			if (Global.Instance.currentLevelChoose == "MiniGames" || Global.Instance.currentLevelChoose == "Puzzle" || Global.Instance.currentLevelChoose == "IZM2" || Global.Instance.currentLevelChoose == "HybridPark")
			{
				Global.Instance.mainMenuShowMoreModes = true;
			}
			SceneManager.Instance.ChangeScene("MainMenu");
			break;
		case LevelChooseMode.Level:
			informationLabel.Text = "章节选择";
			tween = camera.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Quad);
			tween.TweenProperty(camera, "global_position:y", defaultMarker.GlobalPosition.Y, 0.5);
			currentMode = LevelChooseMode.Chapter;
			chapterMenu.Set("alive", true);
			await ToSignal(tween, Tween.SignalName.Finished);
			foreach (Node child in levelMenu.GetChildren())
			{
				child.QueueFree();
			}
			{
				foreach (Node child2 in levelContainer.GetChildren())
				{
					child2.QueueFree();
				}
				break;
			}
		case LevelChooseMode.Option:
			gui.Visible = true;
			guiFollow.Visible = true;
			optionGUI.Visible = false;
			currentMode = saveBackMode;
			break;
		}
	}

	public void OptionButtonPressed()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		saveBackMode = currentMode;
		currentMode = LevelChooseMode.Option;
		gui.Visible = false;
		guiFollow.Visible = false;
		optionGUI.Visible = true;
	}

	public void DifficultChange()
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		string text = "Normal";
		normalLabel.Modulate = Colors.White;
		difficultLabel.Modulate = Colors.White;
		bool flag = false;
		if (normalButton.ButtonPressed)
		{
			text = "Normal";
			normalLabel.Modulate = Colors.Green;
			flag = true;
		}
		if (difficultButton.ButtonPressed)
		{
			DialogManager.Instance.DialogCreate("DifficultWarning");
			text = "Difficult";
			difficultLabel.Modulate = Colors.OrangeRed;
			flag = true;
		}
		if (!flag)
		{
			text = "Normal";
			normalLabel.Modulate = Colors.Green;
			flag = true;
		}
		if (IsModBrowser)
		{
			_modDifficulty = text;
			return;
		}
		GameSaveManager.Instance.SetKeyValue("CurrentDifficult", text);
		GameSaveManager.Instance.Save();
	}

	public void LevelMenuFlattenCheckBoxToggled(bool toggle)
	{
		AudioManager.Instance.AudioPlay("ButtonPress");
		if (toggle)
		{
			levelMenu.Visible = false;
			levelMenuFlatten.Visible = true;
			gui.FollowViewportEnabled = true;
		}
		else
		{
			levelMenu.Visible = true;
			levelMenuFlatten.Visible = false;
			gui.FollowViewportEnabled = false;
		}
		GameSaveManager.Instance.SetKeyValue("LevelMenuFlatten", toggle);
		GameSaveManager.Instance.Save();
	}

	private void ShowModCatalogs()
	{
		_showingModCatalogs = true;
		Global.Instance.currentAwardMode = false;
		gui.Visible = false;
		guiFollow.Visible = false;
		chapterMenu.Set("alive", false);
		CanvasLayer browser = GetNode<CanvasLayer>("ModBrowser");
		browser.Visible = true;
		VBoxContainer node = browser.GetNode<VBoxContainer>("Panel/Scroll/Catalogs");
		foreach (Node child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
		node.AddChild(new Label
		{
			Text = "Mod 关卡",
			HorizontalAlignment = HorizontalAlignment.Center
		}, forceReadableName: false, InternalMode.Disabled);
		IReadOnlyList<XWModContentCatalog.Catalog> catalogs = XWModContentCatalog.GetCatalogs();
		if (catalogs.Count == 0)
		{
			node.AddChild(new Label
			{
				Text = "没有可用的 Mod 关卡。请在 Mod 管理中安装并启用包含关卡目录的 Mod。",
				AutowrapMode = TextServer.AutowrapMode.WordSmart
			}, forceReadableName: false, InternalMode.Disabled);
		}
		foreach (XWModContentCatalog.Catalog catalog in catalogs)
		{
			Button button = new Button
			{
				Text = catalog.Title + "  ·  " + catalog.OwnerModId,
				CustomMinimumSize = new Vector2(0f, 56f)
			};
			button.Pressed += delegate
			{
				if (!XWModPlayerProgressService.CanPlay(catalog.OwnerModId, out var reason))
				{
					DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", reason);
				}
				else
				{
					XWModLevelSession.SelectCatalog(catalog.OwnerModId, catalog.Key);
					currentChapterList = catalog.Data["Chapter"].AsGodotArray();
					foreach (Node child2 in chapterMenu.GetChildren())
					{
						chapterMenu.RemoveChild(child2);
						child2.QueueFree();
					}
					browser.Visible = false;
					_showingModCatalogs = false;
					gui.Visible = true;
					guiFollow.Visible = true;
					chapterMenu.Set("alive", true);
					informationLabel.Text = catalog.Title;
					InitChapter();
				}
			};
			node.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private async Task LaunchModLevel(int id)
	{
		Dictionary dictionary = currentChapter["Level"].AsGodotArray()[id].AsGodotDictionary();
		XWModLevelIdentity xWModLevelIdentity = XWModLevelSession.ForLevel(difficulty: (dictionary["Level"].AsGodotDictionary().GetValueOrDefault(_modDifficulty, "").AsString()
			.Length > 0) ? _modDifficulty : "Normal", key: dictionary["SaveKey"].AsString());
		if (!XWModPlayerProgressService.CanPlay(xWModLevelIdentity.OwnerModId, out var reason))
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", reason);
			return;
		}
		if (!XWModContentCatalog.TryResolve(xWModLevelIdentity, out var config, out var error))
		{
			DialogManager.Instance.DialogCreate("DialogBoxTips").Set("text", error);
			return;
		}
		XWModLevelSession.Select(xWModLevelIdentity);
		TowerDefenseManager.Instance.currentLevelConfig = config;
		Global.Instance.currentLevelId = id;
		Global.Instance.enterLevelMode = "ModLevel";
		if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.SendSelectLevel(xWModLevelIdentity.LevelSaveKey);
			await MultiPlayerManager.Instance.StartCompatibleGameAsync();
		}
		else
		{
			SceneManager.Instance.ChangeScene("TowerDefense");
		}
	}
}
