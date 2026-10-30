using Godot;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class AwardSettlement : Node
{
	public TextureRect awardSettlementBackground;

	public Control awardShowNode;

	public Control imageShowNode;

	public Label typeLabel;

	public Label nameLabel;

	public Label describeLabel;

	public Control marker;

	public Sprite2D imageSprite;

	public NinePatchButtonBase menuButton;

	public NinePatchButtonBase nextLevelButton;

	public TextureButton moreBackButton;

	public override void _Ready()
	{
		awardSettlementBackground = GetNode<TextureRect>("%AwardSettlementBackground");
		awardShowNode = GetNode<Control>("%AwardShowNode");
		imageShowNode = GetNode<Control>("%ImageShowNode");
		typeLabel = GetNode<Label>("%TypeLabel");
		nameLabel = GetNode<Label>("%NameLabel");
		describeLabel = GetNode<Label>("%DescribeLabel");
		marker = GetNode<Control>("%Marker");
		imageSprite = GetNode<Sprite2D>("%ImageSprite");
		menuButton = GetNode<NinePatchButtonBase>("%MenuButton");
		nextLevelButton = GetNode<NinePatchButtonBase>("%NextLevelButton");
		moreBackButton = GetNode<TextureButton>("MoreBackButton");
		AudioManager.Instance.AudioPlay("ZenGarden", AudioManagerEnum.TYPE.MUSIC);
		if (Global.Instance.enterLevelMode == "ModLevel")
		{
			typeLabel.Text = "Mod 关卡完成";
			nameLabel.Text = XWModLevelSession.Current?.LevelSaveKey ?? "";
			describeLabel.Text = "进度独立保存在当前 Mod 中。";
		}
		switch ((TowerDefenseEnum.LEVEL_REWARDTYPE)Global.Instance.currentAwardType)
		{
		case TowerDefenseEnum.LEVEL_REWARDTYPE.PACKET:
			InitPacket(Global.Instance.currentAwardValue);
			break;
		case TowerDefenseEnum.LEVEL_REWARDTYPE.COLLECTABLE:
			InitCollectable(Global.Instance.currentAwardValue);
			break;
		}
		if (TowerDefenseManager.Instance.SetNextLevel(Global.Instance.currentLevelChoose, Global.Instance.currentChapterId, Global.Instance.currentLevelId) != null)
		{
			nextLevelButton.Visible = true;
			menuButton.Visible = false;
		}
		menuButton.OnPressed += MenuButtonPressed;
		nextLevelButton.OnPressed += NextLevelButtonPressed;
		moreBackButton.Pressed += MenuButtonPressed;
	}

	public void InitPacket(string packetName)
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		if (packetConfig.characterConfig is TowerDefensePlantConfig)
		{
			typeLabel.Text = "AWARD_PACKET_PLANT";
		}
		if (packetConfig.characterConfig is TowerDefenseZombieConfig)
		{
			typeLabel.Text = "AWARD_PACKET_ZOMBIE";
		}
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
		towerDefenseInGamePacketShow.setMobileLayout = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		marker.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
		towerDefenseInGamePacketShow.Init(packetConfig, skipGlobalChangeCost: true);
		towerDefenseInGamePacketShow.onlyDraw = true;
		towerDefenseInGamePacketShow.Scale = 1.25f * Vector2.One;
		nameLabel.Text = packetConfig.name;
		describeLabel.Text = packetConfig.describe;
	}

	public void InitCollectable(string collectableName)
	{
		CollectableConfig collectable = TowerDefenseManager.GetCollectable(collectableName);
		if (collectable.config is ShovelConfig shovelConfig)
		{
			typeLabel.Text = "AWARD_COLLECTABLE_SHOVEL";
			Sprite2D sprite2D = new Sprite2D();
			sprite2D.Texture = shovelConfig.texture;
			sprite2D.Scale = Vector2.One * (80f / (float)sprite2D.Texture.GetWidth());
			marker.AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
			nameLabel.Text = shovelConfig.name;
			describeLabel.Text = shovelConfig.describe;
		}
		if (collectable.config is AwardSettlementConfig awardSettlementConfig)
		{
			awardShowNode.Visible = false;
			imageShowNode.Visible = true;
			awardSettlementBackground.Texture = awardSettlementConfig.background;
			imageSprite.Texture = awardSettlementConfig.image;
			menuButton.Position = new Vector2(menuButton.Position.X, 520f);
			nextLevelButton.Position = new Vector2(nextLevelButton.Position.X, 520f);
		}
	}

	public void MenuButtonPressed()
	{
		Global.Instance.currentAwardMode = true;
		switch (Global.Instance.enterLevelMode)
		{
		case "ModLevel":
			Global.Instance.currentAwardMode = false;
			Global.Instance.currentLevelChoose = "__ModLevels";
			SceneManager.Instance.ChangeScene("LevelChoose");
			break;
		case "LevelChoose":
			SceneManager.Instance.ChangeScene("LevelChoose");
			break;
		case "DailyLevel":
			SceneManager.Instance.ChangeScene("MainMenu");
			break;
		case "DiyLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		case "LoadLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		case "OnlineLevel":
			SceneManager.Instance.ChangeScene("LevelEditorStage");
			break;
		}
	}

	public async void NextLevelButtonPressed()
	{
		if (Global.Instance.enterLevelMode == "ModLevel" && Global.IsMultiplayerMode)
		{
			if (MultiPlayerManager.Instance.isHost)
			{
				MultiPlayerManager.Instance.SendSelectLevel(XWModLevelSession.Current.LevelSaveKey);
				await MultiPlayerManager.Instance.StartCompatibleGameAsync();
			}
		}
		else
		{
			SceneManager.Instance.ChangeScene("TowerDefense");
		}
	}
}
