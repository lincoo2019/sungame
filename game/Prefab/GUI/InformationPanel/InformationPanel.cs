using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class InformationPanel : Control
{
	private static Texture2D _almanacGroundDay;

	private static Texture2D _almanacGroundNight;

	private static Texture2D _almanacGroundNightPool;

	private static Texture2D _almanacGroundPool;

	private TextureRect tabTexture;

	private TextureRect groundTexture;

	private Control informationNode;

	private Control customNode;

	private Control spriteNode;

	private Control previewClip;

	private Label nameLabel;

	private RichTextLabel expressionLabel;

	private RichTextLabel handbookExpressionLabel;

	private RichTextLabel handbookStoryLabel;

	private RichTextLabel informationCostLabel;

	private RichTextLabel informationColdDownLabel;

	private ScrollContainer scrollContainer;

	private TextureButton informationTabButton;

	private Label informationTabLabel;

	private TextureButton customTabButton;

	private Label customTabLabel;

	private SpriteBrightButton preCustomButton;

	private NinePatchButtonBase equipmentButton;

	private SpriteBrightButton nextCustomButton;

	private Label nooneCustomLabel;

	private RichTextLabel customNameLabel;

	private RichTextLabel customAccessLabel;

	private RichTextLabel customStoryLabel;

	public TowerDefenseCharacter currentCharcter;

	public Sprite2D currentSpriteImage;

	public TowerDefensePacketConfig currentPacketConfig;

	public string currentCustom = "";

	private int _currentCustomId;

	private static Texture2D ALMANAC_GROUND_DAY => _almanacGroundDay ?? (_almanacGroundDay = GD.Load<Texture2D>("uid://ceqaimccnt56q"));

	private static Texture2D ALMANAC_GROUND_NIGHT => _almanacGroundNight ?? (_almanacGroundNight = GD.Load<Texture2D>("uid://wjso8qh4jnie"));

	private static Texture2D ALMANAC_GROUND_NIGHT_POOL => _almanacGroundNightPool ?? (_almanacGroundNightPool = GD.Load<Texture2D>("uid://y166oxgfiohu"));

	private static Texture2D ALMANAC_GROUND_POOL => _almanacGroundPool ?? (_almanacGroundPool = GD.Load<Texture2D>("uid://cpfhqdus53fr0"));

	public int currentCustomId
	{
		get
		{
			return _currentCustomId;
		}
		set
		{
			_currentCustomId = value;
			FreshCustom();
		}
	}

	public override void _Ready()
	{
		tabTexture = GetNode<TextureRect>("%TabTexture");
		groundTexture = GetNode<TextureRect>("%GroundTexture");
		informationNode = GetNode<Control>("%InformationNode");
		customNode = GetNode<Control>("%CustomNode");
		spriteNode = GetNode<Control>("%SpriteNode");
		previewClip = spriteNode.GetParent() as Control;
		nameLabel = GetNode<Label>("%NameLabel");
		expressionLabel = GetNode<RichTextLabel>("%ExpressionLabel");
		handbookExpressionLabel = GetNode<RichTextLabel>("%HandbookExpressionLabel");
		handbookStoryLabel = GetNode<RichTextLabel>("%HandbookStoryLabel");
		informationCostLabel = GetNode<RichTextLabel>("%InformationCostLabel");
		informationColdDownLabel = GetNode<RichTextLabel>("%InformationColdDownLabel");
		scrollContainer = GetNode<ScrollContainer>("%ScrollContainer");
		informationTabButton = GetNode<TextureButton>("%InformationTabButton");
		informationTabLabel = GetNode<Label>("%InformationTabLabel");
		customTabButton = GetNode<TextureButton>("%CustomTabButton");
		customTabLabel = GetNode<Label>("%CustomTabLabel");
		preCustomButton = GetNode<SpriteBrightButton>("%PreCustomButton");
		equipmentButton = GetNode<NinePatchButtonBase>("%EquipmentButton");
		nextCustomButton = GetNode<SpriteBrightButton>("%NextCustomButton");
		preCustomButton.OnPressed += PreCustomButtonPressed;
		equipmentButton.OnPressed += EquipmentButtonPressed;
		nextCustomButton.OnPressed += NextCustomButtonPressed;
		nooneCustomLabel = GetNode<Label>("%NooneCustomLabel");
		customNameLabel = GetNode<RichTextLabel>("%CustomNameLabel");
		customAccessLabel = GetNode<RichTextLabel>("%CustomAccessLabel");
		customStoryLabel = GetNode<RichTextLabel>("%CustomStoryLabel");
		GetNode<BaseButton>("%InformationTabButton").Pressed += TabButtonPressed;
		GetNode<BaseButton>("%CustomTabButton").Pressed += TabButtonPressed;
	}

	public void InitPacket(TowerDefensePacketConfig packetConfig)
	{
		Clear();
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(packetConfig.characterConfig))
		{
			GD.PushError("[InformationPanel] Cannot preview an invalid packet or character config.");
			return;
		}
		TowerDefenseCharacterConfig characterConfig = packetConfig.characterConfig;
		if (string.IsNullOrWhiteSpace(characterConfig.name))
		{
			GD.PushError("[InformationPanel] Packet '" + packetConfig.saveKey + "' has an empty character name.");
			return;
		}
		Freshbackground(characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER), characterConfig.sleepTime == "Day");
		PackedScene chacraterScene = TowerDefenseManager.GetChacraterScene(characterConfig.name);
		if (!GodotObject.IsInstanceValid(chacraterScene))
		{
			GD.PushError("[InformationPanel] Character scene not found for '" + characterConfig.name + "'.");
			return;
		}
		Node node = chacraterScene.Instantiate(PackedScene.GenEditState.Disabled);
		currentCharcter = node as TowerDefenseCharacter;
		if (!GodotObject.IsInstanceValid(currentCharcter) || !GodotObject.IsInstanceValid(currentCharcter.config) || !GodotObject.IsInstanceValid(currentCharcter.sprite))
		{
			GD.PushError("[InformationPanel] Character preview '" + characterConfig.name + "' has incompatible serialized properties.");
			node?.QueueFree();
			currentCharcter = null;
			return;
		}
		currentPacketConfig = packetConfig;
		currentCharcter.inGame = false;
		currentCharcter.packet = packetConfig;
		SetLocalPreviewRender(currentCharcter);
		spriteNode.AddChild(currentCharcter, forceReadableName: false, InternalMode.Disabled);
		SetLocalPreviewRender(currentCharcter);
		currentCharcter.sprite.ProcessMode = ProcessModeEnum.Always;
		currentCharcter.SetMainStateMachineDispatchEnabled(enabled: false);
		currentCharcter.sprite.SetAnimation(packetConfig.packetAnimeClip);
		currentCharcter.gridPos = new Vector2I(currentCharcter.gridPos.X, 10);
		currentCharcter.ZIndex = 0;
		currentCharcter.SetLogicalGlobalPosition(spriteNode.GetGlobalTransform() * packetConfig.handbookPacketAnimeOffset);
		ShadowComponent shadowComponent = currentCharcter.shadowComponent;
		if (shadowComponent != null && !shadowComponent.IsReleased)
		{
			currentCharcter.shadowComponent.preferMultiMesh = false;
			currentCharcter.shadowComponent.Init();
			currentCharcter.shadowComponent.BatchUpdate();
		}
		currentCharcter.shadowSprite.ZIndex = -1;
		FlattenLocalPreviewCanvasZ(currentCharcter.sprite);
		if (characterConfig is TowerDefensePlantConfig towerDefensePlantConfig)
		{
			Vector2 logicalGlobalPosition = currentCharcter.GetLogicalGlobalPosition();
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				logicalGlobalPosition.X -= 40 * item.X;
			}
			currentCharcter.SetLogicalGlobalPosition(logicalGlobalPosition);
		}
		if (characterConfig.armorData != null && packetConfig.initArmor != null && packetConfig.initArmor.Count > 0)
		{
			foreach (string item2 in packetConfig.initArmor)
			{
				ArmorSlotConfig slotConfig = characterConfig.armorData.GetSlotConfig(item2);
				TowerDefenseArmorTypeData typeData = characterConfig.armorData.GetTypeData(item2);
				if (typeData == null)
				{
					continue;
				}
				string replaceMethod = slotConfig.replaceMethod;
				if (!(replaceMethod == "Media"))
				{
					if (replaceMethod == "Sprite")
					{
						AdobeAnimatePart adobeAnimatePart = CharacterArmorData.CreateArmorPartNode(currentCharcter.sprite, slotConfig, typeData);
						if (GodotObject.IsInstanceValid(adobeAnimatePart))
						{
							adobeAnimatePart.LightMask = 0;
						}
					}
				}
				else
				{
					characterConfig.armorData.OpenArmorFliters(currentCharcter.sprite, item2);
					characterConfig.armorData.SetArmorReplace(currentCharcter.sprite, item2, 0);
				}
			}
		}
		SetLocalPreviewRender(currentCharcter);
		FlattenLocalPreviewCanvasZ(currentCharcter.sprite);
		scrollContainer.ScrollVertical = 0;
		if (characterConfig is TowerDefensePlantConfig)
		{
			scrollContainer.Size = new Vector2(scrollContainer.Size.X, 210f);
		}
		if (characterConfig is TowerDefenseZombieConfig)
		{
			scrollContainer.Size = new Vector2(scrollContainer.Size.X, 140f);
		}
		nameLabel.Text = packetConfig.name;
		expressionLabel.Visible = packetConfig.describe != "";
		expressionLabel.Text = $"[color=2f375e]{Tr(packetConfig.describe)}[/color]";
		handbookExpressionLabel.Visible = packetConfig.handbookDescribe != "";
		handbookExpressionLabel.Text = packetConfig.handbookDescribe;
		handbookStoryLabel.Visible = packetConfig.handbookStory != "";
		handbookStoryLabel.Text = packetConfig.handbookStory;
		informationCostLabel.Text = $"[color=ab5e57]花费:{characterConfig.cost}[/color]";
		informationColdDownLabel.Text = $"[color=ab5e57]冷却时间:{characterConfig.packetCooldown:F1}[/color]";
		SetLightMask(currentCharcter);
		if (customTabButton.ButtonPressed)
		{
			EnterCustom();
		}
	}

	public void InitShovel(ShovelConfig shovelConfig)
	{
		Clear();
		Freshbackground();
		scrollContainer.Size = new Vector2(scrollContainer.Size.X, 180f);
		tabTexture.Visible = false;
		informationTabButton.Visible = false;
		customTabButton.Visible = false;
		currentSpriteImage = new Sprite2D();
		currentSpriteImage.Texture = shovelConfig.texture;
		currentSpriteImage.LightMask = 0;
		spriteNode.AddChild(currentSpriteImage, forceReadableName: false, InternalMode.Disabled);
		nameLabel.Text = shovelConfig.name;
		expressionLabel.Visible = shovelConfig.describe != "";
		expressionLabel.Text = $"[color=2f375e]{Tr(shovelConfig.describe)}[/color]";
		handbookExpressionLabel.Visible = shovelConfig.handbookDescribe != "";
		handbookExpressionLabel.Text = shovelConfig.handbookDescribe;
		handbookStoryLabel.Visible = shovelConfig.handbookStory != "";
		handbookStoryLabel.Text = shovelConfig.handbookStory;
		informationCostLabel.Text = "";
		informationColdDownLabel.Text = "";
	}

	public void InitMower(MowerConfig mowerConfig)
	{
		Clear();
		Freshbackground();
		scrollContainer.Size = new Vector2(scrollContainer.Size.X, 180f);
		tabTexture.Visible = false;
		informationTabButton.Visible = false;
		customTabButton.Visible = false;
		currentSpriteImage = new Sprite2D();
		currentSpriteImage.Texture = mowerConfig.texture;
		currentSpriteImage.Scale = Vector2.One * 0.6f;
		currentSpriteImage.LightMask = 0;
		spriteNode.AddChild(currentSpriteImage, forceReadableName: false, InternalMode.Disabled);
		nameLabel.Text = mowerConfig.name;
		expressionLabel.Visible = mowerConfig.describe != "";
		expressionLabel.Text = $"[color=2f375e]{Tr(mowerConfig.describe)}[/color]";
		handbookExpressionLabel.Visible = mowerConfig.handbookDescribe != "";
		handbookExpressionLabel.Text = mowerConfig.handbookDescribe;
		handbookStoryLabel.Visible = mowerConfig.handbookStory != "";
		handbookStoryLabel.Text = mowerConfig.handbookStory;
		informationCostLabel.Text = "";
		informationColdDownLabel.Text = "";
	}

	public void EnterInformation()
	{
		if (currentPacketConfig.characterConfig.customData != null)
		{
			Dictionary packetState = XWModPlayerProgressService.GetPacketState(currentPacketConfig.saveKey);
			if (packetState.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Custom", "")
				.AsString() != "")
			{
				currentCharcter.currentCustom = new Array<string> { (string)packetState["Key"].AsGodotDictionary()["Custom"] };
			}
			else
			{
				currentCharcter.currentCustom = new Array<string>();
			}
		}
		SetLightMask(currentCharcter);
	}

	public async void EnterCustom()
	{
		TowerDefenseCharacterConfig characterConfig = currentPacketConfig.characterConfig;
		if (characterConfig.customData != null)
		{
			nooneCustomLabel.Visible = false;
			customNode.Visible = true;
			Dictionary dictionary = XWModPlayerProgressService.GetPacketState(currentPacketConfig.saveKey).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
			if (dictionary.GetValueOrDefault("Custom", "").AsString() != "")
			{
				for (int i = 0; i < characterConfig.customData.customList.Count; i++)
				{
					if (characterConfig.customData.customList[i].customName == dictionary["Custom"].AsString())
					{
						currentCustomId = i;
						break;
					}
				}
			}
			else
			{
				currentCustomId = 0;
			}
		}
		else
		{
			nooneCustomLabel.Visible = true;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			customNode.Visible = false;
		}
	}

	public void Clear()
	{
		DisposePreviewNode(currentSpriteImage);
		DisposePreviewNode(currentCharcter);
		currentSpriteImage = null;
		currentCharcter = null;
		currentPacketConfig = null;
	}

	private static void DisposePreviewNode(Node previewNode)
	{
		if (GodotObject.IsInstanceValid(previewNode))
		{
			ReleaseLocalPreviewSubmissions(previewNode);
			Node parent = previewNode.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(previewNode);
			}
			previewNode.QueueFree();
		}
	}

	private static void ReleaseLocalPreviewSubmissions(Node node)
	{
		if (node is AdobeAnimateSprite sprite)
		{
			AdobeAnimateRenderManager.ReleaseImmediateSubmission(sprite);
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			ReleaseLocalPreviewSubmissions(child);
		}
	}

	public void TabButtonPressed()
	{
		nooneCustomLabel.Visible = false;
		informationNode.Visible = false;
		customNode.Visible = false;
		if (informationTabButton.ButtonPressed)
		{
			EnterInformation();
			informationNode.Visible = true;
			informationTabLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		}
		else
		{
			informationTabLabel.AddThemeColorOverride("font_color", new Color("c35b25"));
		}
		if (customTabButton.ButtonPressed)
		{
			EnterCustom();
			customNode.Visible = true;
			customTabLabel.AddThemeColorOverride("font_color", Colors.Yellow);
		}
		else
		{
			customTabLabel.AddThemeColorOverride("font_color", new Color("c35b25"));
		}
	}

	public void EquipmentButtonPressed()
	{
		string text = equipmentButton.text;
		if (!(text == "装备装扮"))
		{
			if (text == "卸下装扮")
			{
				Dictionary packetState = XWModPlayerProgressService.GetPacketState(currentPacketConfig.saveKey);
				if (!packetState.ContainsKey("Key"))
				{
					packetState["Key"] = new Dictionary();
				}
				packetState["Key"].AsGodotDictionary()["Custom"] = "";
				SaveCurrentPacketState(packetState);
				BattleEventBus.Instance.EmitCharacterSkinSwitched(currentPacketConfig.saveKey, "");
				equipmentButton.text = "装备装扮";
			}
		}
		else
		{
			Dictionary packetState2 = XWModPlayerProgressService.GetPacketState(currentPacketConfig.saveKey);
			if (!packetState2.ContainsKey("Key"))
			{
				packetState2["Key"] = new Dictionary();
			}
			packetState2["Key"].AsGodotDictionary()["Custom"] = currentCustom;
			SaveCurrentPacketState(packetState2);
			BattleEventBus.Instance.EmitCharacterSkinSwitched(currentPacketConfig.saveKey, currentCustom);
			equipmentButton.text = "卸下装扮";
		}
	}

	private void SaveCurrentPacketState(Dictionary value)
	{
		if (XWModContentCatalog.GetContentOwner("Packet", currentPacketConfig.saveKey).Length > 0)
		{
			XWModPlayerProgressService.SetPacketState(currentPacketConfig.saveKey, value);
		}
		else
		{
			GameSaveManager.Instance.SetTowerDefensePacketValue(currentPacketConfig.saveKey, value);
		}
	}

	public void Freshbackground(bool isWater = false, bool isNight = false)
	{
		if (!isWater)
		{
			if (!isNight)
			{
				groundTexture.Texture = ALMANAC_GROUND_DAY;
			}
			else
			{
				groundTexture.Texture = ALMANAC_GROUND_NIGHT;
			}
		}
		else if (!isNight)
		{
			groundTexture.Texture = ALMANAC_GROUND_POOL;
		}
		else
		{
			groundTexture.Texture = ALMANAC_GROUND_NIGHT_POOL;
		}
	}

	public void FreshCustom()
	{
		TowerDefenseCharacterConfig characterConfig = currentPacketConfig.characterConfig;
		Dictionary packetState = XWModPlayerProgressService.GetPacketState(currentPacketConfig.saveKey);
		currentCustom = characterConfig.customData.customList[currentCustomId].customName;
		currentCharcter.currentCustom = new Array<string> { currentCustom };
		SetLightMask(currentCharcter);
		string openKey = characterConfig.customData.customList[currentCustomId].openKey;
		string contentOwner = XWModContentCatalog.GetContentOwner("Packet", currentPacketConfig.saveKey);
		if (CommandManager.Instance.debugOpenAllCustom || openKey == "" || ((contentOwner.Length > 0) ? XWModPlayerProgressService.IsUnlocked(contentOwner, "Custom", openKey) : (GameSaveManager.Instance.GetFeatureValue(openKey) > 0)))
		{
			equipmentButton.disable = false;
			if (packetState.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Custom", "")
				.AsString() == currentCustom)
			{
				equipmentButton.text = "卸下装扮";
			}
			else
			{
				equipmentButton.text = "装备装扮";
			}
		}
		else
		{
			equipmentButton.disable = true;
			equipmentButton.text = "未获得";
		}
		customNameLabel.Text = $"[color=ff3eff]{Tr(characterConfig.customData.customList[currentCustomId].customHandbookName)}[/color]";
		customAccessLabel.Text = $"[color=red]获取方式[/color]:{Tr(characterConfig.customData.customList[currentCustomId].customHandbookAccess)}";
		customStoryLabel.Text = characterConfig.customData.customList[currentCustomId].customHandbookStory;
	}

	public void PreCustomButtonPressed()
	{
		TowerDefenseCharacterConfig characterConfig = currentPacketConfig.characterConfig;
		currentCustomId = (currentCustomId - 1 + characterConfig.customData.customList.Count) % characterConfig.customData.customList.Count;
	}

	public void NextCustomButtonPressed()
	{
		TowerDefenseCharacterConfig characterConfig = currentPacketConfig.characterConfig;
		currentCustomId = (currentCustomId + 1 + characterConfig.customData.customList.Count) % characterConfig.customData.customList.Count;
	}

	private void SetLocalPreviewRender(Node node)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.forceLocalRender = true;
			adobeAnimateSprite.SetRenderClipControl(previewClip);
			adobeAnimateSprite.ProcessMode = ProcessModeEnum.Always;
			adobeAnimateSprite.pause = false;
			adobeAnimateSprite.UpdateChild();
			adobeAnimateSprite.QueueRedraw();
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			SetLocalPreviewRender(child);
		}
	}

	private void FlattenLocalPreviewCanvasZ(Node node)
	{
		if (node is CanvasItem canvasItem)
		{
			canvasItem.ZIndex = 0;
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			FlattenLocalPreviewCanvasZ(child);
		}
	}

	private void SetLightMask(Node node)
	{
		if (node is CanvasItem canvasItem)
		{
			canvasItem.LightMask = 0;
		}
		if (node is Light2D light2D)
		{
			light2D.Visible = false;
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			SetLightMask(child);
		}
	}
}
