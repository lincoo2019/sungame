using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class TowerDefenseInGamePacketShow : Control
{
	public delegate void PressedEventHandler(TowerDefenseInGamePacketShow packet);

	public delegate void LoveChangeEventHandler(TowerDefenseInGamePacketShow packet);

	internal readonly record struct ColumnPlacement(Vector2I GridPos, TowerDefenseCharacter Target, string Kind);

	private static readonly Vector2 PcContainerFootprint = new Vector2(50f, 70f);

	private static readonly Vector2 MobileContainerFootprint = new Vector2(96f, 60f);

	private int _asyncLifetimeVersion;

	private bool _exitedTree = true;

	private bool _visibilityRefreshQueued;

	private int _queuedVisibilityRefreshVersion;

	private bool _centralRuntimeStateRefresh;

	private bool _runtimeAvailabilityDirty = true;

	private bool _runtimeCostDirty = true;

	private bool _cachedRuntimeAvailability = true;

	private bool _cachedUpgradePacket;

	private static Font _fzkt;

	private static Font _pcFont;

	private static Texture2D _mobileProgressTexture;

	private static Texture2D _packetColour;

	private static Texture2D _packetDiamond;

	private static Texture2D _packetGold;

	private static Texture2D _packetNormal;

	private static Texture2D _packetStar;

	private static Texture2D _packetZombie;

	private static Texture2D _packetCover;

	private static Texture2D _packetGray;

	private const float CostChangeFlashScale = 1.14f;

	private static readonly Color CostChangeFlashColor = new Color(1f, 0.9f, 0.2f);

	private static Texture2D _packetColourMobile;

	private static Texture2D _packetCoverMobile;

	private static Texture2D _packetDiamondMobile;

	private static Texture2D _packetGoldMobile;

	private static Texture2D _packetNormalMobile;

	private static Texture2D _packetStarMobile;

	private static Texture2D _packetZombieMobile;

	public Control previewClip;

	public Control previewSpriteNode;

	public TextureRect backgroundTexture;

	public NinePatchRect selectTexture;

	public Control layout;

	public Control body;

	public Label itemCostLabel;

	public Button button;

	public TextureProgressBar coldDownProgressBar;

	public TextureButton loveButton;

	public MoveComponent moveComponent;

	private bool _reserveContainerFootprint;

	private TowerDefensePacketConfig _config;

	private readonly CardBehaviorHost _cardBehaviorHost = new CardBehaviorHost();

	private EconomyAccountId _sunAccountId;

	private bool _showLove;

	private bool _syncingLoveButtonFromSave;

	private bool _showCost = true;

	private bool _onlyDraw;

	private bool _alive = true;

	private bool _lock;

	[Export(PropertyHint.None, "")]
	public bool plantOnce;

	[Export(PropertyHint.None, "")]
	public bool useCost = true;

	private bool _openShadow;

	private bool _start;

	private bool _select;

	public double coldDown;

	private bool _coldDownOpen;

	public double coldDownTimer;

	private double _pressDelayTimer;

	[Export(PropertyHint.None, "")]
	public bool setMobileLayout;

	[Export(PropertyHint.None, "")]
	public bool setPcLayout;

	[Export(PropertyHint.None, "")]
	public bool canPressPutBack = true;

	public bool enforceRuntimeAvailabilityOnPress = true;

	public bool allowPressWhenUnavailable;

	public bool isMobile;

	public Texture2D pcProgressTexture;

	private double _aliveTime = -1.0;

	public double aliveTimer;

	public double blinkTimer;

	public bool blink;

	private double _height = -1.0;

	public Vector2 savePos = Vector2.Zero;

	public string originalSaveKey = "";

	public AdobeAnimateSprite sprite;

	private int _livePreviewGeneration;

	private bool _previewHovered;

	private bool _previewCreationDeferred;

	private HashSet<string> _extendCoverNames;

	public long baseItemCost;

	private bool _itemCostLabelInitialized;

	private bool _itemCostLabelUsesRiseSuffix;

	private long _itemCostLabelValue;

	private Tween _costChangeFlashTween;

	private Vector2 _costChangeFlashBaseScale = Vector2.One;

	private bool _costChangeFlashActive;

	private long _itemCost = 100L;

	public int riseCost = -1;

	public double costMultiple = -1.0;

	private static Font FZKT => _fzkt ?? (_fzkt = GD.Load<Font>("uid://coqskwlqtnypf"));

	private static Font PC_FONT => _pcFont ?? (_pcFont = GD.Load<Font>("uid://dww00k8yk5k72"));

	private static Texture2D MOBILE_PROGRESS_TEXTURE => _mobileProgressTexture ?? (_mobileProgressTexture = GD.Load<Texture2D>("uid://cy8jc6hsspvfy"));

	private static Texture2D PACKET_COLOUR => _packetColour ?? (_packetColour = GD.Load<Texture2D>("uid://dricqt0scm3sm"));

	private static Texture2D PACKET_DIAMOND => _packetDiamond ?? (_packetDiamond = GD.Load<Texture2D>("uid://eutur83nlbar"));

	private static Texture2D PACKET_GOLD => _packetGold ?? (_packetGold = GD.Load<Texture2D>("uid://dfihegby6yat6"));

	private static Texture2D PACKET_NORMAL => _packetNormal ?? (_packetNormal = GD.Load<Texture2D>("uid://bwksngvkn16cd"));

	private static Texture2D PACKET_STAR => _packetStar ?? (_packetStar = GD.Load<Texture2D>("uid://bwnw5thitfc8e"));

	private static Texture2D PACKET_ZOMBIE => _packetZombie ?? (_packetZombie = GD.Load<Texture2D>("uid://btgdkkg66xc8d"));

	private static Texture2D PACKET_COVER => _packetCover ?? (_packetCover = GD.Load<Texture2D>("uid://dbkcwpq2ie7t0"));

	private static Texture2D PACKET_GRAY => _packetGray ?? (_packetGray = GD.Load<Texture2D>("uid://dfy7jg4c8v30x"));

	private static Texture2D PACKET_COLOUR_MOBILE => _packetColourMobile ?? (_packetColourMobile = GD.Load<Texture2D>("uid://cdrj5bkubm7la"));

	private static Texture2D PACKET_COVER_MOBILE => _packetCoverMobile ?? (_packetCoverMobile = GD.Load<Texture2D>("uid://3k84w2g5d10r"));

	private static Texture2D PACKET_DIAMOND_MOBILE => _packetDiamondMobile ?? (_packetDiamondMobile = GD.Load<Texture2D>("uid://1242bw5k8uwq"));

	private static Texture2D PACKET_GOLD_MOBILE => _packetGoldMobile ?? (_packetGoldMobile = GD.Load<Texture2D>("uid://2smnyulcahs3"));

	private static Texture2D PACKET_NORMAL_MOBILE => _packetNormalMobile ?? (_packetNormalMobile = GD.Load<Texture2D>("uid://bwfy3kman4ich"));

	private static Texture2D PACKET_STAR_MOBILE => _packetStarMobile ?? (_packetStarMobile = GD.Load<Texture2D>("uid://dxa7vky1ueb2o"));

	private static Texture2D PACKET_ZOMBIE_MOBILE => _packetZombieMobile ?? (_packetZombieMobile = GD.Load<Texture2D>("uid://csgt5dkel0xeb"));

	public EconomyAccountId SunAccountId => _sunAccountId;

	public bool HasSunAccount => _sunAccountId.IsValid;

	public TowerDefensePacketConfig config
	{
		get
		{
			return _config;
		}
		set
		{
			_config = value;
			if (backgroundTexture != null)
			{
				backgroundTexture.Visible = _config != null;
			}
			if (GodotObject.IsInstanceValid(button))
			{
				button.Visible = _config != null;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool showLove
	{
		get
		{
			return _showLove;
		}
		set
		{
			_showLove = value;
			if (IsNodeReady())
			{
				loveButton.Visible = _showLove;
				if (config != null && TryGetPacketSaveValue(config.saveKey, out var packetData))
				{
					SetLoveButtonPressedFromSave(packetData.GetValueOrDefault("Love", Variant.From<bool>(false)).AsBool());
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool showCost
	{
		get
		{
			return _showCost;
		}
		set
		{
			_showCost = value;
			if (IsNodeReady() && GodotObject.IsInstanceValid(itemCostLabel))
			{
				itemCostLabel.Visible = _showCost;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool onlyDraw
	{
		get
		{
			return _onlyDraw;
		}
		set
		{
			if (_onlyDraw != value)
			{
				_onlyDraw = value;
				if (IsInsideTree())
				{
					RefreshPhysicsProcessState();
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool alive
	{
		get
		{
			return _alive;
		}
		set
		{
			if (_alive != value)
			{
				_alive = value;
				ColorSet();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool @lock
	{
		get
		{
			return _lock;
		}
		set
		{
			_lock = value;
			ColorSet();
		}
	}

	[Export(PropertyHint.None, "")]
	public bool openShadow
	{
		get
		{
			return _openShadow;
		}
		set
		{
			_openShadow = value;
			ColorSet();
		}
	}

	public bool start
	{
		get
		{
			return _start;
		}
		set
		{
			if (_start != value)
			{
				_start = value;
				InvalidateRuntimeState(includeCost: true);
			}
		}
	}

	public bool select
	{
		get
		{
			return _select;
		}
		set
		{
			_select = value;
			if (selectTexture != null && GodotObject.IsInstanceValid(selectTexture))
			{
				selectTexture.Visible = _select;
			}
		}
	}

	public bool coldDownOpen
	{
		get
		{
			return _coldDownOpen;
		}
		set
		{
			if (_coldDownOpen != value)
			{
				_coldDownOpen = value;
				_runtimeAvailabilityDirty = true;
				if (!_coldDownOpen && GodotObject.IsInstanceValid(coldDownProgressBar))
				{
					coldDownProgressBar.Visible = false;
				}
				RefreshPhysicsProcessState();
			}
		}
	}

	public double pressDelayTimer
	{
		get
		{
			return _pressDelayTimer;
		}
		set
		{
			if (!Mathf.IsEqualApprox(_pressDelayTimer, value))
			{
				_pressDelayTimer = value;
				RefreshPhysicsProcessState();
			}
		}
	}

	public double aliveTime
	{
		get
		{
			return _aliveTime;
		}
		set
		{
			if (!Mathf.IsEqualApprox(_aliveTime, value))
			{
				_aliveTime = value;
				RefreshPhysicsProcessState();
			}
		}
	}

	public double height
	{
		get
		{
			return _height;
		}
		set
		{
			if (!Mathf.IsEqualApprox(_height, value))
			{
				_height = value;
				RefreshPhysicsProcessState();
			}
		}
	}

	public bool IsPreviewCreationDeferred => _previewCreationDeferred;

	public bool HasPreparedPreview
	{
		get
		{
			if (GodotObject.IsInstanceValid(sprite) && sprite.IsFrozenPreview)
			{
				return sprite.frameIndex == sprite.clipRange.X;
			}
			return false;
		}
	}

	public string PreparedPreviewClip
	{
		get
		{
			if (!GodotObject.IsInstanceValid(sprite))
			{
				return "";
			}
			return sprite.clip ?? "";
		}
	}

	public long itemCost
	{
		get
		{
			return _itemCost;
		}
		set
		{
			bool flag = riseCost != -1;
			if (_itemCost != value || !_itemCostLabelInitialized || _itemCostLabelUsesRiseSuffix != flag)
			{
				_itemCost = value;
				UpdateItemCostLabel();
			}
		}
	}

	public event PressedEventHandler OnPressed;

	public event LoveChangeEventHandler OnLoveChange;

	public void SetContainerFootprint(bool enabled)
	{
		_reserveContainerFootprint = enabled;
		ApplyContainerFootprint();
	}

	private void ApplyContainerFootprint()
	{
		Vector2 vector = (isMobile ? MobileContainerFootprint : PcContainerFootprint);
		base.CustomMinimumSize = (_reserveContainerFootprint ? vector : Vector2.Zero);
		if (GodotObject.IsInstanceValid(body))
		{
			body.Position = (_reserveContainerFootprint ? (vector / 2f) : Vector2.Zero);
		}
	}

	public bool TryBindSunAccount(EconomyAccountId accountId)
	{
		if (!accountId.IsValid)
		{
			return false;
		}
		if (_sunAccountId.IsValid)
		{
			return _sunAccountId == accountId;
		}
		_sunAccountId = accountId;
		return true;
	}

	private bool CanAffordItemCost()
	{
		if (!MeetsSunBalanceRequirement())
		{
			return false;
		}
		if (!useCost)
		{
			return true;
		}
		if (!HasSunAccount)
		{
			return TowerDefenseManager.Instance.GetSun() >= itemCost;
		}
		return TowerDefenseManager.Instance.CanAffordSun(_sunAccountId, itemCost);
	}

	private bool MeetsSunBalanceRequirement()
	{
		TowerDefensePacketConfig towerDefensePacketConfig = config;
		if (towerDefensePacketConfig == null || !towerDefensePacketConfig.disableWhenSunNegative)
		{
			return true;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		EconomyAccountId accountId = (HasSunAccount ? _sunAccountId : instance.GetLocalSunAccountId());
		if (instance.TryGetSun(accountId, out var balance))
		{
			return balance >= 0;
		}
		return false;
	}

	private bool TryBeginItemCost(bool useSun, out SunSpendReceipt receipt)
	{
		receipt = null;
		if (!useCost || !useSun)
		{
			return true;
		}
		if (!MeetsSunBalanceRequirement())
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		EconomyAccountId accountId = (HasSunAccount ? _sunAccountId : instance.GetLocalSunAccountId());
		return instance.TryBeginSunSpend(accountId, itemCost, out receipt);
	}

	private void UpdateItemCostLabel(bool force = false)
	{
		if (!GodotObject.IsInstanceValid(itemCostLabel))
		{
			_itemCostLabelInitialized = false;
			return;
		}
		bool flag = riseCost != -1;
		if (force || !_itemCostLabelInitialized || _itemCostLabelValue != _itemCost || _itemCostLabelUsesRiseSuffix != flag)
		{
			itemCostLabel.Text = (flag ? (_itemCost + "+") : _itemCost.ToString());
			_itemCostLabelInitialized = true;
			_itemCostLabelValue = _itemCost;
			_itemCostLabelUsesRiseSuffix = flag;
		}
	}

	public void SetCentralRuntimeStateRefresh(bool enabled)
	{
		if (_centralRuntimeStateRefresh != enabled)
		{
			_centralRuntimeStateRefresh = enabled;
			InvalidateRuntimeState(includeCost: true);
		}
	}

	public void InvalidateRuntimeState(bool includeCost = false)
	{
		_runtimeAvailabilityDirty = true;
		if (includeCost)
		{
			_runtimeCostDirty = true;
		}
		RefreshPhysicsProcessState();
	}

	public void RefreshRuntimeState(bool includeCost = false)
	{
		if (includeCost)
		{
			_runtimeCostDirty = true;
		}
		bool flag = (_cachedUpgradePacket = HasMeta("is_upgrade_packet"));
		if (!GodotObject.IsInstanceValid(config) || (!start && !flag))
		{
			_runtimeAvailabilityDirty = false;
			_runtimeCostDirty = false;
			RefreshPhysicsProcessState();
			return;
		}
		if (_runtimeCostDirty && !flag)
		{
			RefreshDynamicItemCost();
		}
		_runtimeCostDirty = false;
		_cachedRuntimeAvailability = CanAffordItemCost() && (flag || HasRequiredPlantCover());
		_runtimeAvailabilityDirty = false;
		ApplyCachedRuntimeAvailability();
		RefreshPhysicsProcessState();
	}

	private void RefreshDynamicItemCost()
	{
		baseItemCost = config.GetCost();
		long num = baseItemCost;
		if (!TowerDefenseManager.MapIgnoresDynamicPacketCostGrowth(config._GetType()))
		{
			int characterNum = TowerDefenseManager.Instance.GetCharacterNum(config.saveKey);
			if (costMultiple != -1.0)
			{
				double num2 = (double)num * (double)Mathf.Pow((float)costMultiple, characterNum);
				num = ((!(num2 > 9.223372036854776E+18) && !double.IsNaN(num2)) ? ((long)Mathf.Floor(num2)) : 9223372036854775807L);
			}
			if (riseCost != -1)
			{
				num += (long)characterNum * (long)riseCost;
			}
		}
		itemCost = num;
	}

	private bool HasRequiredPlantCover()
	{
		Array<string> plantCover = config.GetPlantCover();
		if (plantCover != null && plantCover.Count > 0)
		{
			bool flag = false;
			foreach (string item in plantCover)
			{
				if (TowerDefenseManager.Instance.GetCharacterNum(item) > 0)
				{
					flag = true;
					break;
				}
			}
			return config.GetCoverCanDirectPlant() || flag;
		}
		if (!(config.characterConfig is TowerDefensePlantConfig { extendCoverDictionary: not null } towerDefensePlantConfig) || towerDefensePlantConfig.extendCoverDictionary.Count == 0)
		{
			return true;
		}
		if (_extendCoverNames == null)
		{
			_extendCoverNames = new HashSet<string>();
			foreach (string value in towerDefensePlantConfig.extendCoverDictionary.Values)
			{
				_extendCoverNames.Add(value);
			}
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return false;
		}
		List<TowerDefenseCharacter> cleanCharactersList = instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.characterFilter && towerDefenseCharacter.config != null && GodotObject.IsInstanceValid(towerDefenseCharacter.cell) && _extendCoverNames.Contains(towerDefenseCharacter.config.name) && towerDefenseCharacter.cell.CanPacketPlant(config))
			{
				return true;
			}
		}
		return false;
	}

	private void ApplyCachedRuntimeAvailability()
	{
		if (start || _cachedUpgradePacket)
		{
			alive = _cachedRuntimeAvailability && (_cachedUpgradePacket || !coldDownOpen);
		}
	}

	private bool NeedsContinuousPhysicsProcess()
	{
		if (onlyDraw)
		{
			return false;
		}
		if (!_centralRuntimeStateRefresh)
		{
			return true;
		}
		if (_runtimeAvailabilityDirty || _runtimeCostDirty)
		{
			return true;
		}
		if (pressDelayTimer > 0.0 || coldDownOpen || aliveTime >= 0.0)
		{
			return true;
		}
		if (height >= 0.0 && GodotObject.IsInstanceValid(moveComponent))
		{
			return !moveComponent.IsQueuedForDeletion();
		}
		return false;
	}

	private void RefreshPhysicsProcessState()
	{
		if (IsInsideTree())
		{
			SetPhysicsProcess(NeedsContinuousPhysicsProcess());
		}
	}

	public void ColorSet()
	{
		Color modulate = ((alive && !openShadow && !@lock && !blink) ? Colors.White : Colors.DimGray);
		if (layout == null)
		{
			base.Modulate = modulate;
		}
		else
		{
			layout.Modulate = modulate;
		}
	}

	public void MobilePreset()
	{
		backgroundTexture.Size = new Vector2(96f, 60f);
		backgroundTexture.Position = -backgroundTexture.Size / 2f;
		previewClip.Position = new Vector2(6f, 6f);
		previewClip.Size = new Vector2(82f, 46f);
		itemCostLabel.Size = new Vector2(48f, 25f);
		itemCostLabel.Position = new Vector2(45f, 32f);
		itemCostLabel.TextureFilter = TextureFilterEnum.ParentNode;
		itemCostLabel.AddThemeColorOverride("font_color", Colors.White);
		itemCostLabel.AddThemeConstantOverride("outline_size", 5);
		itemCostLabel.AddThemeFontOverride("font", FZKT);
		itemCostLabel.AddThemeFontSizeOverride("font_size", 24);
		selectTexture.Size = new Vector2(312f, 198f);
		selectTexture.Position = new Vector2(-48f, -30f);
		button.Size = new Vector2(94f, 60f);
		button.Position = new Vector2(-48f, -30f);
		coldDownProgressBar.Size = new Vector2(95f, 59f);
		coldDownProgressBar.Position = new Vector2(-48f, -30f);
		coldDownProgressBar.TextureProgress = MOBILE_PROGRESS_TEXTURE;
		loveButton.Position = new Vector2(26f, -34f);
		ApplyContainerFootprint();
	}

	public void SetPcPreset()
	{
		backgroundTexture.Size = new Vector2(50f, 70f);
		backgroundTexture.Position = new Vector2(-24f, -32f);
		previewClip.Position = new Vector2(3f, 6f);
		previewClip.Size = new Vector2(44f, 47f);
		itemCostLabel.TextureFilter = TextureFilterEnum.Linear;
		itemCostLabel.AddThemeColorOverride("font_color", new Color(0f, 0f, 0f));
		itemCostLabel.RemoveThemeConstantOverride("outline_size");
		itemCostLabel.AddThemeFontOverride("font", PC_FONT);
		itemCostLabel.AddThemeFontSizeOverride("font_size", 12);
		itemCostLabel.HorizontalAlignment = HorizontalAlignment.Center;
		itemCostLabel.VerticalAlignment = VerticalAlignment.Center;
		itemCostLabel.Position = new Vector2(2f, 52f);
		itemCostLabel.Size = new Vector2(35f, 17f);
		selectTexture.Size = new Vector2(170f, 237f);
		selectTexture.Position = new Vector2(-25f, -33f);
		button.Position = new Vector2(-24f, -32f);
		button.Size = new Vector2(50f, 70f);
		coldDownProgressBar.Position = new Vector2(-24f, -32f);
		coldDownProgressBar.Size = new Vector2(50f, 70f);
		if (pcProgressTexture != null)
		{
			coldDownProgressBar.TextureProgress = pcProgressTexture;
		}
		loveButton.Position = new Vector2(4f, -35f);
		ApplyContainerFootprint();
	}

	public void SetMobileMode(bool enabled)
	{
		if (!setMobileLayout && !setPcLayout && isMobile != enabled)
		{
			isMobile = enabled;
			if (isMobile)
			{
				MobilePreset();
			}
			else
			{
				SetPcPreset();
			}
			UpdateBackgroundTexture();
			UpdateSpriteLayout();
			RefreshPreview();
		}
	}

	public void RefreshPreview()
	{
		if (!CanContinueAsync(_asyncLifetimeVersion) || config == null)
		{
			return;
		}
		if (_previewCreationDeferred)
		{
			SetPreviewVisible(visible: false);
			return;
		}
		bool flag = false;
		if (!GodotObject.IsInstanceValid(sprite))
		{
			CreateSprite();
			flag = GodotObject.IsInstanceValid(sprite);
		}
		if (GodotObject.IsInstanceValid(sprite))
		{
			UpdateSpriteLayout();
			sprite.Visible = true;
			if (!flag)
			{
				FreezePreviewTree(sprite);
			}
		}
		SetPreviewVisible(HasPreparedPreview);
	}

	public void UpdateBackgroundTexture()
	{
		if (config == null || !GodotObject.IsInstanceValid(backgroundTexture))
		{
			return;
		}
		if (isMobile)
		{
			switch (config._GetType())
			{
			case TowerDefenseEnum.PACKET_TYPE.WHITE:
				backgroundTexture.Texture = PACKET_NORMAL_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.GOLD:
				backgroundTexture.Texture = PACKET_GOLD_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.DIAMOND:
				backgroundTexture.Texture = PACKET_DIAMOND_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.COLOUR:
				backgroundTexture.Texture = PACKET_COLOUR_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.STAR:
				backgroundTexture.Texture = PACKET_STAR_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.ORIGINAL:
				backgroundTexture.Texture = PACKET_NORMAL_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.ZOMBIE:
				backgroundTexture.Texture = PACKET_ZOMBIE_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.COVER:
				backgroundTexture.Texture = PACKET_COVER_MOBILE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.GRAY:
				backgroundTexture.Texture = PACKET_GRAY;
				break;
			}
		}
		else
		{
			switch (config._GetType())
			{
			case TowerDefenseEnum.PACKET_TYPE.WHITE:
				backgroundTexture.Texture = PACKET_NORMAL;
				break;
			case TowerDefenseEnum.PACKET_TYPE.GOLD:
				backgroundTexture.Texture = PACKET_GOLD;
				break;
			case TowerDefenseEnum.PACKET_TYPE.DIAMOND:
				backgroundTexture.Texture = PACKET_DIAMOND;
				break;
			case TowerDefenseEnum.PACKET_TYPE.COLOUR:
				backgroundTexture.Texture = PACKET_COLOUR;
				break;
			case TowerDefenseEnum.PACKET_TYPE.STAR:
				backgroundTexture.Texture = PACKET_STAR;
				break;
			case TowerDefenseEnum.PACKET_TYPE.ORIGINAL:
				backgroundTexture.Texture = PACKET_NORMAL;
				break;
			case TowerDefenseEnum.PACKET_TYPE.ZOMBIE:
				backgroundTexture.Texture = PACKET_ZOMBIE;
				break;
			case TowerDefenseEnum.PACKET_TYPE.COVER:
				backgroundTexture.Texture = PACKET_COVER;
				break;
			case TowerDefenseEnum.PACKET_TYPE.GRAY:
				backgroundTexture.Texture = PACKET_GRAY;
				break;
			}
		}
	}

	public void UpdateSpriteLayout()
	{
		if (config == null)
		{
			return;
		}
		TowerDefenseCharacterConfig characterConfig = config.characterConfig;
		if (GodotObject.IsInstanceValid(sprite) && isMobile)
		{
			if (characterConfig is TowerDefenseZombieConfig)
			{
				sprite.Position = config.packetAnimeOffset * 1.2f;
				sprite.Scale = config.packetAnimeScale * 1.25f;
			}
			else
			{
				sprite.Position = config.packetAnimeOffset;
				sprite.Scale = config.packetAnimeScale * 1.25f;
			}
		}
		else if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.Position = config.packetAnimeOffset;
			sprite.Scale = config.packetAnimeScale;
		}
		if (GodotObject.IsInstanceValid(sprite) && config.packetFlip)
		{
			sprite.Scale = new Vector2(0f - sprite.Scale.X, sprite.Scale.Y);
		}
	}

	public void Clear()
	{
		_asyncLifetimeVersion++;
		_cardBehaviorHost.Release();
		_livePreviewGeneration++;
		_previewHovered = false;
		_runtimeAvailabilityDirty = true;
		_runtimeCostDirty = true;
		_cachedRuntimeAvailability = true;
		_cachedUpgradePacket = false;
		if (GodotObject.IsInstanceValid(config))
		{
			TryUnsubscribeCharacterSkinSwitch();
		}
		config = null;
		_extendCoverNames = null;
		if (GodotObject.IsInstanceValid(backgroundTexture))
		{
			backgroundTexture.Texture = PACKET_NORMAL;
		}
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.ReleaseForcedCpuPoseData();
			sprite.ClearRenderClipControl();
			sprite.QueueFree();
			sprite = null;
		}
		baseItemCost = 0L;
		_itemCostLabelInitialized = false;
		itemCost = 0L;
		riseCost = -1;
		coldDown = 0.0;
		_previewCreationDeferred = false;
		SetPreviewVisible(visible: false);
	}

	public void ClearEventHandlers()
	{
		this.OnPressed = null;
		this.OnLoveChange = null;
	}

	public void ResetForPool()
	{
		SetContainerFootprint(enabled: false);
		ClearEventHandlers();
		Clear();
		sprite = null;
		showLove = false;
		alive = true;
		@lock = false;
		select = false;
		onlyDraw = false;
		plantOnce = false;
		useCost = true;
		enforceRuntimeAvailabilityOnPress = true;
		allowPressWhenUnavailable = false;
		start = false;
		coldDownOpen = false;
		coldDownTimer = 0.0;
		pressDelayTimer = 0.0;
		aliveTime = -1.0;
		aliveTimer = 0.0;
		blinkTimer = 0.0;
		blink = false;
		height = -1.0;
		costMultiple = -1.0;
		originalSaveKey = "";
		_sunAccountId = default(EconomyAccountId);
		_centralRuntimeStateRefresh = false;
		_runtimeAvailabilityDirty = true;
		_runtimeCostDirty = true;
		if (IsInsideTree())
		{
			SetPhysicsProcess(enable: false);
		}
	}

	public void Init(TowerDefensePacketConfig _config, bool skipGlobalChangeCost = false)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		_asyncLifetimeVersion++;
		config = _config;
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		_cardBehaviorHost.Bind(this, config);
		TowerDefensePerfProfiler.End("card.create.behavior", startTicks2);
		_extendCoverNames = null;
		if (originalSaveKey == "")
		{
			originalSaveKey = config.saveKey;
		}
		long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
		baseItemCost = config.GetCost(skipGlobalChangeCost);
		itemCost = baseItemCost;
		riseCost = config.GetCostRise();
		costMultiple = config.GetCostMultiple();
		InvalidateRuntimeState(includeCost: true);
		TowerDefensePerfProfiler.End("card.create.economy", startTicks3);
		long startTicks4 = TowerDefensePerfProfiler.BeginHotPath();
		if (!_previewCreationDeferred)
		{
			CreateSprite();
		}
		TowerDefensePerfProfiler.End("card.create.preview", startTicks4);
		if (!_previewCreationDeferred)
		{
			TrySubscribeCharacterSkinSwitch();
		}
		long startTicks5 = TowerDefensePerfProfiler.BeginHotPath();
		coldDown = config.GetPacketCooldown();
		ApplyInitializedUiState();
		if (!_previewCreationDeferred)
		{
			OnVisibilityChanged();
		}
		TowerDefensePerfProfiler.End("card.create.ui", startTicks5);
		TowerDefensePerfProfiler.End("card.create.total", startTicks);
	}

	public void InitVisualPreview(TowerDefensePacketConfig previewConfig, int previewCost, int previewCostRise, double previewCostMultiple, double previewCooldown)
	{
		_asyncLifetimeVersion++;
		config = previewConfig;
		if (GodotObject.IsInstanceValid(config))
		{
			if (originalSaveKey == "")
			{
				originalSaveKey = config.saveKey;
			}
			baseItemCost = previewCost;
			itemCost = previewCost;
			riseCost = previewCostRise;
			costMultiple = previewCostMultiple;
			InvalidateRuntimeState();
			CreateSprite();
			TrySubscribeCharacterSkinSwitch();
			coldDown = Math.Max(0.0, previewCooldown);
			ApplyInitializedUiState();
			OnVisibilityChanged();
		}
	}

	private void ApplyInitializedUiState()
	{
		UpdateBackgroundTexture();
		if (GodotObject.IsInstanceValid(coldDownProgressBar))
		{
			UpdateItemCostLabel(force: true);
			coldDownProgressBar.MaxValue = coldDown;
			coldDownProgressBar.Value = coldDownProgressBar.MaxValue;
		}
	}

	public void CreateSprite()
	{
		if (config == null)
		{
			return;
		}
		TowerDefenseCharacterConfig characterConfig = config.characterConfig;
		if (!GodotObject.IsInstanceValid(characterConfig))
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.ReleaseForcedCpuPoseData();
			sprite.ClearRenderClipControl();
			sprite.QueueFree();
			sprite = null;
		}
		if (!HasPreviewControls())
		{
			TowerDefensePerfProfiler.End("card.preview.total", startTicks);
			return;
		}
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		sprite = TowerDefenseManager.GetPacketSprite(config);
		TowerDefensePerfProfiler.End("card.preview.instantiate", startTicks2);
		if (!GodotObject.IsInstanceValid(sprite))
		{
			TowerDefensePerfProfiler.End("card.preview.total", startTicks);
			return;
		}
		sprite.forceCpuPoseRender = true;
		ConfigurePreviewTreeBeforeAttach(sprite);
		long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
		previewSpriteNode.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
		TowerDefensePerfProfiler.End("card.preview.attach", startTicks3);
		bool flag = false;
		if (config.GetHypnoses())
		{
			sprite.Modulate = new Color(0.72f, 0.62f, 1f, sprite.Modulate.A);
		}
		UpdateSpriteLayout();
		long startTicks4 = TowerDefensePerfProfiler.BeginHotPath();
		if (characterConfig.armorData != null && config.initArmor != null && config.initArmor.Count > 0)
		{
			foreach (string item in config.initArmor)
			{
				ArmorSlotConfig slotConfig = characterConfig.armorData.GetSlotConfig(item);
				TowerDefenseArmorTypeData typeData = characterConfig.armorData.GetTypeData(item);
				if (typeData == null)
				{
					continue;
				}
				string replaceMethod = slotConfig.replaceMethod;
				if (!(replaceMethod == "Media"))
				{
					if (replaceMethod == "Sprite")
					{
						CharacterArmorData.CreateArmorPartNode(sprite, slotConfig, typeData);
						flag = true;
					}
				}
				else
				{
					characterConfig.armorData.OpenArmorFliters(sprite, item);
					characterConfig.armorData.SetArmorReplace(sprite, item, 0);
					flag = true;
				}
			}
		}
		if (characterConfig.customData != null && TryGetPacketSaveValue(config.saveKey, out var packetData))
		{
			string text = packetData.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary().GetValueOrDefault("Custom", "")
				.AsString();
			if (text != "")
			{
				characterConfig.customData.SetCustomFliters(sprite, text);
				flag = true;
			}
		}
		TowerDefensePerfProfiler.End("card.preview.customize", startTicks4);
		long startTicks5 = TowerDefensePerfProfiler.BeginHotPath();
		if (!string.IsNullOrEmpty(config.packetAnimeClip) && config.packetAnimeClip.IndexOf('&') < 0 && sprite.clip == config.packetAnimeClip && sprite.HasClip(config.packetAnimeClip))
		{
			sprite.loop = true;
			sprite.clipOver = false;
			sprite.frameIndex = sprite.clipRange.X;
			sprite.elapsedTimer = 0.0;
			TowerDefensePerfProfiler.SampleHotPath("card.preview.reuseAuthoredClip", 1);
		}
		else
		{
			sprite.SetAnimation(config.packetAnimeClip);
			TowerDefensePerfProfiler.SampleHotPath("card.preview.selectPacketClip", 1);
		}
		TowerDefensePerfProfiler.End("card.preview.animation", startTicks5);
		long startTicks6 = TowerDefensePerfProfiler.BeginHotPath();
		FreezePreparedPreviewTree(sprite, flag, !flag);
		sprite.QueueRedraw();
		TowerDefensePerfProfiler.End("card.preview.freeze", startTicks6);
		long startTicks7 = TowerDefensePerfProfiler.BeginHotPath();
		AdobeAnimateRenderManager.WarmupRenderMount(sprite);
		TowerDefensePerfProfiler.End("card.preview.renderMount", startTicks7);
		ColorSet();
		TowerDefensePerfProfiler.End("card.preview.total", startTicks);
	}

	private void OnCharacterSkinSwitched(string packetSaveKey, string customKey)
	{
		if (!CanContinueAsync(_asyncLifetimeVersion) || !GodotObject.IsInstanceValid(config) || config.saveKey != packetSaveKey)
		{
			return;
		}
		TowerDefenseCharacterConfig characterConfig = config.characterConfig;
		if (GodotObject.IsInstanceValid(characterConfig) && characterConfig.customData != null && GodotObject.IsInstanceValid(sprite) && (!(customKey != "") || characterConfig.customData.customDictionary.ContainsKey(customKey)))
		{
			characterConfig.customData.ClearCustomFliters(sprite);
			if (customKey != "")
			{
				characterConfig.customData.SetCustomFliters(sprite, customKey);
			}
			sprite.UpdateMediaReplaceData();
			sprite.UpdateChild();
			sprite.RefreshManagedSlotSpriteCacheForRender();
			FreezePreviewTree(sprite, forcePoseRefresh: true);
		}
	}

	public void OnVisibilityChanged()
	{
		_queuedVisibilityRefreshVersion = _asyncLifetimeVersion;
		if (CanContinueAsync(_queuedVisibilityRefreshVersion) && !_visibilityRefreshQueued)
		{
			_visibilityRefreshQueued = true;
			CallDeferred("ApplyDeferredVisibilityState");
		}
	}

	public void ApplyDeferredVisibilityState()
	{
		_visibilityRefreshQueued = false;
		int queuedVisibilityRefreshVersion = _queuedVisibilityRefreshVersion;
		if (!CanContinueAsync(queuedVisibilityRefreshVersion))
		{
			return;
		}
		if (_previewCreationDeferred)
		{
			SetPreviewVisible(visible: false);
		}
		else if (!IsVisibleInTree())
		{
			if (GodotObject.IsInstanceValid(sprite))
			{
				FreezePreviewTree(sprite);
			}
			SetPreviewVisible(visible: false);
		}
		else if (!_previewHovered)
		{
			if (!GodotObject.IsInstanceValid(sprite))
			{
				CreateSprite();
			}
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.Visible = true;
				FreezePreviewTree(sprite, forcePoseRefresh: false, forceRenderResubmit: true);
			}
			SetPreviewVisible(HasPreparedPreview);
		}
	}

	public override void _EnterTree()
	{
		_exitedTree = false;
		_asyncLifetimeVersion++;
		base._EnterTree();
		if (!setMobileLayout && !setPcLayout && BattleEventBus.Instance != null)
		{
			BattleEventBus.Instance.OnUiSwitched += SetMobileMode;
		}
	}

	public override void _ExitTree()
	{
		_exitedTree = true;
		_asyncLifetimeVersion++;
		_livePreviewGeneration++;
		_previewHovered = false;
		if (BattleEventBus.Instance != null)
		{
			BattleEventBus.Instance.OnUiSwitched -= SetMobileMode;
		}
		TryUnsubscribeCharacterSkinSwitch();
		_visibilityRefreshQueued = false;
		base._ExitTree();
	}

	public override void _Ready()
	{
		previewClip = GetNodeOrNull<Control>("%PreviewClip");
		previewSpriteNode = GetNodeOrNull<Control>("%PreviewSpriteNode");
		backgroundTexture = GetNodeOrNull<TextureRect>("%BackgroundTexture");
		selectTexture = GetNodeOrNull<NinePatchRect>("%SelectTexture");
		layout = GetNodeOrNull<Control>("%Layout");
		body = GetNodeOrNull<Control>("%Body");
		itemCostLabel = GetNodeOrNull<Label>("%ItemCostLabel");
		button = GetNodeOrNull<Button>("%Button");
		coldDownProgressBar = GetNodeOrNull<TextureProgressBar>("%ColdDownProgressBar");
		loveButton = GetNodeOrNull<TextureButton>("%LoveButton");
		moveComponent = GetNodeOrNull<MoveComponent>("%MoveComponent");
		savePos = base.GlobalPosition;
		button.ActionMode = BaseButton.ActionModeEnum.Press;
		base.VisibilityChanged += OnVisibilityChanged;
		button.MouseEntered += OnMouseEntered;
		button.MouseExited += OnMouseExited;
		button.Pressed += Pressed;
		loveButton.Toggled += LoveButtonToggled;
		pcProgressTexture = coldDownProgressBar.TextureProgress;
		isMobile = setMobileLayout || (!setPcLayout && TryGetMobilePresetConfig() && SceneManager.CurrentScene != "LevelEditorStage");
		if (isMobile)
		{
			MobilePreset();
		}
		else
		{
			ApplyContainerFootprint();
		}
		coldDownProgressBar.Visible = false;
		ApplyInitializedUiState();
		if (config != null)
		{
			RefreshPreview();
		}
		RefreshPhysicsProcessState();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (onlyDraw)
		{
			return;
		}
		if (pressDelayTimer > 0.0)
		{
			pressDelayTimer -= delta;
		}
		if (GodotObject.IsInstanceValid(moveComponent) && height != -1.0 && (double)base.GlobalPosition.Y > (double)savePos.Y + height && moveComponent.velocity.Y >= 0f)
		{
			moveComponent.QueueFree();
		}
		if (aliveTime != -1.0)
		{
			if (!(aliveTimer < aliveTime))
			{
				if (!select)
				{
					GrantExpireSun();
					if (Global.IsMultiplayerMode && HasMeta("packet_sync_id"))
					{
						int syncId = (int)GetMeta("packet_sync_id");
						MultiPlayerManager.Instance.SendPacketPick(syncId);
					}
					QueueFree();
				}
				return;
			}
			aliveTimer += delta;
			if (aliveTimer > aliveTime - 5.0)
			{
				if (blinkTimer < 0.25)
				{
					blinkTimer += delta;
				}
				else
				{
					if (!select)
					{
						blink = !blink;
						ColorSet();
					}
					blinkTimer = 0.0;
				}
			}
		}
		bool flag = HasMeta("is_upgrade_packet");
		if (flag != _cachedUpgradePacket)
		{
			_runtimeAvailabilityDirty = true;
		}
		if (start || flag)
		{
			if (!flag && coldDownOpen)
			{
				if (!TowerDefenseManager.Instance.pausePacket)
				{
					if (!TowerDefenseManager.Instance.backPacket)
					{
						coldDownTimer = Math.Max(0.0, coldDownTimer - delta);
						if (coldDownTimer <= 0.0)
						{
							coldDownOpen = false;
						}
					}
					else if (coldDownTimer < coldDown)
					{
						coldDownTimer += delta;
					}
				}
				if (coldDownOpen && GodotObject.IsInstanceValid(coldDownProgressBar))
				{
					if (!coldDownProgressBar.Visible)
					{
						coldDownProgressBar.Visible = true;
					}
					coldDownProgressBar.Value = coldDownTimer;
				}
			}
			if (!_centralRuntimeStateRefresh)
			{
				_runtimeAvailabilityDirty = true;
				if (Engine.GetPhysicsFrames() % 10 == 0L && !flag)
				{
					_runtimeCostDirty = true;
				}
			}
			if (_runtimeAvailabilityDirty || _runtimeCostDirty)
			{
				RefreshRuntimeState();
			}
		}
		RefreshPhysicsProcessState();
	}

	private void GrantExpireSun()
	{
		TowerDefensePacketConfig towerDefensePacketConfig = config;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			return;
		}
		int expireSun = towerDefensePacketConfig.expireSun;
		if (expireSun <= 0 || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			bool hypnoses = towerDefensePacketConfig.GetHypnoses();
			Vector2 velocity = new Vector2((float)GD.RandRange(-50.0, 50.0), -400f);
			TowerDefenseSunBase towerDefenseSunBase = ((!HasSunAccount) ? (hypnoses ? instance.BrainSunCreate(base.GlobalPosition, expireSun, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, velocity) : instance.SunCreate(base.GlobalPosition, expireSun, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, velocity)) : (hypnoses ? instance.BrainSunCreate(_sunAccountId, base.GlobalPosition, expireSun, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, velocity) : instance.SunCreate(_sunAccountId, base.GlobalPosition, expireSun, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, velocity)));
			if (GodotObject.IsInstanceValid(towerDefenseSunBase))
			{
				towerDefenseSunBase.RestoreAutoCollect(savedAutoCollect: true);
			}
		}
	}

	public void Pressed()
	{
		if (onlyDraw || config == null || @lock)
		{
			return;
		}
		if (enforceRuntimeAvailabilityOnPress && !CanAffordItemCost())
		{
			alive = false;
		}
		else if ((alive || allowPressWhenUnavailable) && !(pressDelayTimer > 0.0))
		{
			pressDelayTimer = 0.2;
			AudioManager.Instance.AudioPlay("PacketPick");
			select = !select;
			if (!canPressPutBack)
			{
				button.MouseFilter = MouseFilterEnum.Ignore;
			}
			if (select)
			{
				_cardBehaviorHost.NotifyPressed();
			}
			this.OnPressed?.Invoke(this);
		}
	}

	public void OnMouseEntered()
	{
		if (config != null && (!allowPressWhenUnavailable || alive))
		{
			if (_previewCreationDeferred)
			{
				SetPreviewCreationDeferred(deferred: false);
			}
			_previewHovered = true;
			_livePreviewGeneration++;
			if (!GodotObject.IsInstanceValid(sprite))
			{
				CreateSprite();
			}
			if (!GodotObject.IsInstanceValid(sprite))
			{
				SetPreviewVisible(visible: false);
				return;
			}
			sprite.SetAnimation(config.packetAnimeClip);
			sprite.Visible = true;
			PlayPreviewTreeFromStart(sprite);
			sprite.QueueRedraw();
			AdobeAnimateRenderManager.WarmupRenderMount(sprite);
			SetPreviewVisible(visible: true);
		}
	}

	public void OnMouseExited()
	{
		_previewHovered = false;
		_livePreviewGeneration++;
		if (config != null)
		{
			if (GodotObject.IsInstanceValid(sprite))
			{
				FreezePreviewTree(sprite);
			}
			SetPreviewVisible(GodotObject.IsInstanceValid(sprite));
		}
	}

	public void Reset()
	{
		if (GodotObject.IsInstanceValid(this) && !onlyDraw && config != null)
		{
			_previewHovered = false;
			_livePreviewGeneration++;
			select = false;
			pressDelayTimer = 0.0;
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.Visible = true;
				FreezePreviewTree(sprite);
			}
			if (!canPressPutBack)
			{
				button.MouseFilter = MouseFilterEnum.Pass;
			}
			SetPreviewVisible(HasPreparedPreview);
		}
	}

	public void StartInit()
	{
		alive = false;
		if (!plantOnce && !CommandManager.Instance.debugPacketColdDown)
		{
			TowerDefenseLevelBaseConfig obj = TowerDefenseManager.Instance.currentControl?.levelConfig;
			bool flag = false;
			bool flag2 = false;
			if (obj is TowerDefenseLevelConfig towerDefenseLevelConfig)
			{
				flag = towerDefenseLevelConfig.packetColdDownStart;
				flag2 = towerDefenseLevelConfig.packetColdDownUse;
			}
			else
			{
				TowerDefenseLevelSeedBankConfig towerDefenseLevelSeedBankConfig = TowerDefenseManager.Instance.GetSeedBankFeature()?.config;
				if (towerDefenseLevelSeedBankConfig != null)
				{
					flag = towerDefenseLevelSeedBankConfig.packetColdDownStart;
					flag2 = towerDefenseLevelSeedBankConfig.packetColdDownUse;
				}
			}
			if (flag && flag2)
			{
				double startingCooldown = config.GetStartingCooldown();
				if (startingCooldown > 0.0)
				{
					coldDownOpen = true;
					coldDownTimer = startingCooldown;
				}
			}
		}
		OnVisibilityChanged();
	}

	private void RestoreMagicBeanRestoreState(TowerDefenseCharacter character)
	{
		TowerDefenseCharacterSaveConfigCSharp restoreConfig;
		if (GodotObject.IsInstanceValid(character) && HasMeta("MagicBeanRestore"))
		{
			GodotObject godotObject = GetMeta("MagicBeanRestore").AsGodotObject();
			restoreConfig = godotObject as TowerDefenseCharacterSaveConfigCSharp;
			if (restoreConfig != null)
			{
				character.PrepareForProgressRestore();
				character.Ready += RestoreOnce;
			}
		}
		void RestoreOnce()
		{
			character.Ready -= RestoreOnce;
			TowerDefensePlantMagicBean.ApplySavedState(character, restoreConfig);
		}
	}

	public TowerDefenseCharacter Plant(Vector2I gridPos, bool useSun = true, bool executeEvent = true)
	{
		if (!alive)
		{
			return null;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		if (GodotObject.IsInstanceValid(mapCell) && mapCell.NotifyBlockedPlanting(config))
		{
			return null;
		}
		bool flag = Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage";
		SunSpendReceipt receipt = null;
		if (!flag && !TryBeginItemCost(useSun, out receipt))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = (HasSunAccount ? config.Plant(_sunAccountId, gridPos) : config.Plant(gridPos));
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			receipt?.TryRollback();
			return null;
		}
		RestoreMagicBeanRestoreState(towerDefenseCharacter);
		if (Global.IsMultiplayerMode && HasMeta("packet_sync_id"))
		{
			SetMeta("packet_planted", true);
		}
		if (!flag)
		{
			CompleteUse(receipt);
		}
		if (executeEvent)
		{
			_cardBehaviorHost.NotifyUseSucceeded(towerDefenseCharacter);
		}
		return towerDefenseCharacter;
	}

	public List<TowerDefenseCharacter> PlantColumnBatch(IReadOnlyList<Vector2I> gridPositions)
	{
		List<ColumnPlacement> list = new List<ColumnPlacement>();
		if (gridPositions != null)
		{
			foreach (Vector2I gridPosition in gridPositions)
			{
				list.Add(new ColumnPlacement(gridPosition, null, "grid"));
			}
		}
		return PlantColumnTargets(list);
	}

	internal List<TowerDefenseCharacter> PlantColumnTargets(IReadOnlyList<ColumnPlacement> placements, Action<ColumnPlacement, TowerDefenseCharacter> onCreated = null)
	{
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		if (!alive || placements == null || placements.Count == 0)
		{
			return list;
		}
		bool flag = Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage";
		SunSpendReceipt receipt = null;
		if (!flag && !TryBeginItemCost(useSun: true, out receipt))
		{
			return list;
		}
		bool hypnoses = config.GetHypnoses();
		ColumnPlantGridBudget columnPlantGridBudget = new ColumnPlantGridBudget(config);
		HashSet<Vector2I> hashSet = new HashSet<Vector2I>();
		HashSet<TowerDefenseCharacter> hashSet2 = new HashSet<TowerDefenseCharacter>();
		foreach (ColumnPlacement placement in placements)
		{
			TowerDefenseCharacter target = placement.Target;
			if (placement.Kind != "grid" && (!GodotObject.IsInstanceValid(target) || target.isDestroy || target.die || target.nearDie || !GodotObject.IsInstanceValid(target.instance) || target.instance.invincible || !target.instance.canBeCollection || (placement.Kind == "plant" && (!(target is TowerDefensePlant) || target.instance.hologram))))
			{
				continue;
			}
			Vector2I vector2I = ((placement.Kind == "grid") ? placement.GridPos : target.gridPos);
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(vector2I);
			if (!GodotObject.IsInstanceValid(mapCell) || mapCell.NotifyBlockedPlanting(config) || (placement.Kind == "grid" && (!columnPlantGridBudget.CanPlace(vector2I) || !hashSet.Add(vector2I))) || (placement.Kind != "grid" && !hashSet2.Add(target)))
			{
				continue;
			}
			if (placement.Kind == "jala_vase")
			{
				if (target is TowerDefensePlantJalaVase towerDefensePlantJalaVase && towerDefensePlantJalaVase.TryAddJala(config))
				{
					list.Add(towerDefensePlantJalaVase);
					onCreated?.Invoke(placement, towerDefensePlantJalaVase);
				}
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter;
			switch (placement.Kind)
			{
			case "zombie":
				if (config.canPlaceOnZombie)
				{
					towerDefenseCharacter = (HasSunAccount ? config.PlantOnZombie(_sunAccountId, target, hypnoses) : config.PlantOnZombie(target, hypnoses));
					break;
				}
				goto default;
			case "plant":
				if (config.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.PLANT))
				{
					towerDefenseCharacter = (HasSunAccount ? config.PlantOnPlant(_sunAccountId, target) : config.PlantOnPlant(target));
					break;
				}
				goto default;
			case "grid":
				if (mapCell.CanPacketPlant(config))
				{
					towerDefenseCharacter = (HasSunAccount ? config.Plant(_sunAccountId, vector2I) : config.Plant(vector2I));
					break;
				}
				goto default;
			default:
				towerDefenseCharacter = null;
				break;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
			{
				if (placement.Kind == "grid")
				{
					columnPlantGridBudget.Reserve(vector2I);
				}
				RestoreMagicBeanRestoreState(towerDefenseCharacter2);
				list.Add(towerDefenseCharacter2);
				onCreated?.Invoke(placement, towerDefenseCharacter2);
			}
		}
		if (list.Count == 0)
		{
			receipt?.TryRollback();
			return list;
		}
		if (Global.IsMultiplayerMode && HasMeta("packet_sync_id"))
		{
			SetMeta("packet_planted", true);
		}
		if (!flag)
		{
			CompleteUse(receipt);
		}
		_cardBehaviorHost.NotifyUseSucceeded(list[list.Count - 1]);
		return list;
	}

	public TowerDefenseCharacter PlantOnZombie(TowerDefenseCharacter zombie, bool hypnoses = false, bool useSun = true, bool executeEvent = true)
	{
		if (!alive)
		{
			return null;
		}
		TowerDefenseCellInstance towerDefenseCellInstance = (GodotObject.IsInstanceValid(zombie) ? TowerDefenseManager.GetMapCell(zombie.gridPos) : null);
		if (GodotObject.IsInstanceValid(towerDefenseCellInstance) && towerDefenseCellInstance.NotifyBlockedPlanting(config))
		{
			return null;
		}
		bool flag = Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage";
		SunSpendReceipt receipt = null;
		if (!flag && !TryBeginItemCost(useSun, out receipt))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = (HasSunAccount ? config.PlantOnZombie(_sunAccountId, zombie, hypnoses) : config.PlantOnZombie(zombie, hypnoses));
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			receipt?.TryRollback();
			return null;
		}
		RestoreMagicBeanRestoreState(towerDefenseCharacter);
		if (Global.IsMultiplayerMode && HasMeta("packet_sync_id"))
		{
			SetMeta("packet_planted", true);
		}
		if (!flag)
		{
			CompleteUse(receipt);
		}
		if (executeEvent)
		{
			_cardBehaviorHost.NotifyUseSucceeded(towerDefenseCharacter);
		}
		return towerDefenseCharacter;
	}

	public TowerDefenseCharacter PlantOnPlant(TowerDefenseCharacter plant, bool useSun = true, bool executeEvent = true)
	{
		if (!alive)
		{
			return null;
		}
		TowerDefenseCellInstance towerDefenseCellInstance = (GodotObject.IsInstanceValid(plant) ? TowerDefenseManager.GetMapCell(plant.gridPos) : null);
		if (GodotObject.IsInstanceValid(towerDefenseCellInstance) && towerDefenseCellInstance.NotifyBlockedPlanting(config))
		{
			return null;
		}
		bool flag = Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage";
		SunSpendReceipt receipt = null;
		if (!flag && !TryBeginItemCost(useSun, out receipt))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = (HasSunAccount ? config.PlantOnPlant(_sunAccountId, plant) : config.PlantOnPlant(plant));
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			receipt?.TryRollback();
			return null;
		}
		RestoreMagicBeanRestoreState(towerDefenseCharacter);
		if (Global.IsMultiplayerMode && HasMeta("packet_sync_id"))
		{
			SetMeta("packet_planted", true);
		}
		if (!flag)
		{
			CompleteUse(receipt);
		}
		if (executeEvent)
		{
			_cardBehaviorHost.NotifyUseSucceeded(towerDefenseCharacter);
		}
		return towerDefenseCharacter;
	}

	public bool Use(bool useSun = true)
	{
		if (!TryBeginItemCost(useSun, out var receipt))
		{
			return false;
		}
		CompleteUse(receipt);
		_cardBehaviorHost.NotifyUseSucceeded(null, includeActions: false);
		return true;
	}

	public void NotifyUseBehaviorSucceeded(TowerDefenseCharacter createdCharacter = null)
	{
		_cardBehaviorHost.NotifyUseSucceeded(createdCharacter);
	}

	public bool TryBeginPendingUse(out SunSpendReceipt spendReceipt, bool useSun = true)
	{
		if (!TryBeginItemCost(useSun, out spendReceipt))
		{
			return false;
		}
		ApplyUseState(removePlantOnce: false);
		return true;
	}

	public bool TryCommitPendingUse(SunSpendReceipt spendReceipt)
	{
		if (spendReceipt != null && !spendReceipt.TryCommit())
		{
			return false;
		}
		ConsumePurchaseCostChanges();
		if (plantOnce)
		{
			QueueFree();
		}
		return true;
	}

	private void CompleteUse(SunSpendReceipt spendReceipt)
	{
		if (spendReceipt == null || spendReceipt.TryCommit())
		{
			ConsumePurchaseCostChanges();
		}
		ApplyUseState(removePlantOnce: true);
	}

	private void ConsumePurchaseCostChanges()
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			return;
		}
		bool flag = false;
		for (int num = config.changeCostList.Count - 1; num >= 0; num--)
		{
			TowerDefensePacketChangeCost towerDefensePacketChangeCost = config.changeCostList[num];
			if (GodotObject.IsInstanceValid(towerDefensePacketChangeCost) && towerDefensePacketChangeCost.consumeOnPurchase)
			{
				flag |= config.ChangeCostRemove(towerDefensePacketChangeCost);
			}
		}
		if (flag)
		{
			RefreshRuntimeState(includeCost: true);
		}
	}

	private void ApplyUseState(bool removePlantOnce)
	{
		if (removePlantOnce && plantOnce)
		{
			QueueFree();
		}
		if (!CommandManager.Instance.debugPacketColdDown)
		{
			TowerDefenseLevelBaseConfig obj = TowerDefenseManager.Instance.currentControl?.levelConfig;
			bool flag = false;
			if (obj is TowerDefenseLevelConfig towerDefenseLevelConfig)
			{
				flag = towerDefenseLevelConfig.packetColdDownUse;
			}
			else
			{
				TowerDefenseLevelSeedBankConfig towerDefenseLevelSeedBankConfig = TowerDefenseManager.Instance.GetSeedBankFeature()?.config;
				if (towerDefenseLevelSeedBankConfig != null)
				{
					flag = towerDefenseLevelSeedBankConfig.packetColdDownUse;
				}
			}
			if (flag)
			{
				coldDownProgressBar.Visible = true;
				coldDown = config.GetPacketCooldown();
				coldDownProgressBar.MaxValue = coldDown;
				coldDownProgressBar.Value = coldDownProgressBar.MaxValue;
				coldDownOpen = true;
				coldDownTimer = coldDown;
			}
		}
		Reset();
	}

	public void LoveButtonToggled(bool toggled)
	{
		if (_syncingLoveButtonFromSave)
		{
			return;
		}
		if (config == null || !TryGetPacketSaveValue(config.saveKey, out var packetData))
		{
			loveButton.ButtonPressed = toggled;
			this.OnLoveChange?.Invoke(this);
			return;
		}
		packetData["Love"] = toggled;
		loveButton.ButtonPressed = toggled;
		if (GameSaveManager.Instance != null)
		{
			XWModPlayerProgressService.SetPacketState(config.saveKey, packetData);
		}
		this.OnLoveChange?.Invoke(this);
	}

	private void SetLoveButtonPressedFromSave(bool pressed)
	{
		if (!GodotObject.IsInstanceValid(loveButton))
		{
			return;
		}
		_syncingLoveButtonFromSave = true;
		try
		{
			loveButton.ButtonPressed = pressed;
		}
		finally
		{
			_syncingLoveButtonFromSave = false;
		}
	}

	private void TrySubscribeCharacterSkinSwitch()
	{
		if (BattleEventBus.Instance != null)
		{
			BattleEventBus.Instance.OnCharacterSkinSwitched -= OnCharacterSkinSwitched;
			BattleEventBus.Instance.OnCharacterSkinSwitched += OnCharacterSkinSwitched;
		}
	}

	private void TryUnsubscribeCharacterSkinSwitch()
	{
		if (BattleEventBus.Instance != null)
		{
			BattleEventBus.Instance.OnCharacterSkinSwitched -= OnCharacterSkinSwitched;
		}
	}

	private static bool TryGetPacketSaveValue(string saveKey, out Dictionary packetData)
	{
		packetData = new Dictionary();
		if (string.IsNullOrWhiteSpace(saveKey) || GameSaveManager.Instance == null)
		{
			return false;
		}
		packetData = XWModPlayerProgressService.GetPacketState(saveKey);
		return packetData != null;
	}

	private static bool TryGetMobilePresetConfig()
	{
		if (GameSaveManager.Instance != null)
		{
			return GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		}
		return false;
	}

	private bool CanContinueAsync(int asyncVersion)
	{
		if (_exitedTree || asyncVersion != _asyncLifetimeVersion)
		{
			return false;
		}
		try
		{
			return GodotObject.IsInstanceValid(this) && !IsQueuedForDeletion() && IsInsideTree();
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
	}

	private bool HasPreviewControls()
	{
		if (GodotObject.IsInstanceValid(previewClip))
		{
			return GodotObject.IsInstanceValid(previewSpriteNode);
		}
		return false;
	}

	public void SetPreviewCreationDeferred(bool deferred, bool releaseExisting = false)
	{
		if (_previewCreationDeferred == deferred && (!deferred || !releaseExisting || !GodotObject.IsInstanceValid(sprite)))
		{
			return;
		}
		_previewCreationDeferred = deferred;
		if (deferred)
		{
			if (releaseExisting && GodotObject.IsInstanceValid(sprite))
			{
				TryUnsubscribeCharacterSkinSwitch();
				sprite.ReleaseForcedCpuPoseData();
				sprite.ClearRenderClipControl();
				sprite.QueueFree();
				sprite = null;
			}
			SetPreviewVisible(visible: false);
		}
		else if (GodotObject.IsInstanceValid(config) && IsInsideTree())
		{
			TrySubscribeCharacterSkinSwitch();
			RefreshPreview();
		}
	}

	public void Cover(TowerDefensePacketConfig _config, TowerDefensePacketOverride overrideVal = null, bool keepColddown = true, bool changePacket = true)
	{
		if (!GodotObject.IsInstanceValid(overrideVal))
		{
			overrideVal = new TowerDefensePacketOverride();
		}
		if (changePacket)
		{
			CardActionBehaviorChangePacket cardActionBehaviorChangePacket = new CardActionBehaviorChangePacket();
			cardActionBehaviorChangePacket.packetConfig = config;
			overrideVal.useSucceededActions.Add(cardActionBehaviorChangePacket);
		}
		if (keepColddown)
		{
			CardActionBehaviorSetCooldown cardActionBehaviorSetCooldown = new CardActionBehaviorSetCooldown();
			cardActionBehaviorSetCooldown.value = coldDownTimer;
			overrideVal.useSucceededActions.Add(cardActionBehaviorSetCooldown);
		}
		_config._override = overrideVal;
		_config.changeCostList = config.changeCostList;
		Init(_config);
	}

	private void SetPreviewVisible(bool visible)
	{
		if (GodotObject.IsInstanceValid(previewSpriteNode))
		{
			previewSpriteNode.Visible = visible && GodotObject.IsInstanceValid(sprite);
		}
		if (GodotObject.IsInstanceValid(previewClip))
		{
			previewClip.Visible = visible && GodotObject.IsInstanceValid(sprite);
		}
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.Visible = visible;
		}
	}

	private void ConfigurePreviewSprite(AdobeAnimateSprite animateSprite)
	{
		animateSprite.LightMask = 0;
		animateSprite.ZIndex = 0;
		animateSprite.keepRenderSubmittedWhenPaused = true;
		animateSprite.forceLocalRender = true;
		animateSprite.SetRenderClipControl(previewClip);
		animateSprite.ProcessMode = ProcessModeEnum.Always;
	}

	private void ConfigurePreviewNode(Node node)
	{
		if (node is CanvasItem canvasItem)
		{
			canvasItem.ZIndex = 0;
		}
		if (node is AdobeAnimateSprite animateSprite)
		{
			ConfigurePreviewSprite(animateSprite);
		}
	}

	private void ConfigurePreviewTreeBeforeAttach(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		ConfigurePreviewNode(node);
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			ConfigurePreviewTreeBeforeAttach(child);
		}
	}

	private void FreezePreviewTree(Node node, bool forcePoseRefresh = false, bool forceRenderResubmit = false)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		ConfigurePreviewNode(node);
		AdobeAnimateSprite adobeAnimateSprite = null;
		if (node is AdobeAnimateSprite adobeAnimateSprite2)
		{
			adobeAnimateSprite = adobeAnimateSprite2;
			bool flag = forcePoseRefresh || !adobeAnimateSprite2.IsFrozenPreview;
			if (flag)
			{
				adobeAnimateSprite2.RefreshManagedSlotSpriteCacheForRender();
				adobeAnimateSprite2.ResetAnimation();
				adobeAnimateSprite2.UpdateMediaReplaceData();
				adobeAnimateSprite2.UpdateChild();
				adobeAnimateSprite2.QueueRedraw();
			}
			adobeAnimateSprite2.SetFrozenPreview(frozen: true);
			if (forceRenderResubmit && !flag)
			{
				adobeAnimateSprite2.EnsureFrozenPreviewRenderSubmission();
			}
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			FreezePreviewTree(child, forcePoseRefresh, forceRenderResubmit);
		}
		if (GodotObject.IsInstanceValid(adobeAnimateSprite))
		{
			SynchronizeSameDataPreviewChildren(adobeAnimateSprite);
		}
	}

	private void FreezePreparedPreviewTree(Node node, bool forcePoseRefresh, bool poseAlreadyPrepared)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		ConfigurePreviewNode(node);
		AdobeAnimateSprite adobeAnimateSprite = null;
		if (node is AdobeAnimateSprite adobeAnimateSprite2)
		{
			adobeAnimateSprite = adobeAnimateSprite2;
			if (!poseAlreadyPrepared && (forcePoseRefresh || !adobeAnimateSprite2.IsFrozenPreview))
			{
				adobeAnimateSprite2.RefreshManagedSlotSpriteCacheForRender();
				adobeAnimateSprite2.UpdateMediaReplaceData();
				adobeAnimateSprite2.UpdateChild();
				adobeAnimateSprite2.QueueRedraw();
			}
			adobeAnimateSprite2.SetFrozenPreview(frozen: true);
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			FreezePreparedPreviewTree(child, forcePoseRefresh, poseAlreadyPrepared);
		}
		if (GodotObject.IsInstanceValid(adobeAnimateSprite) && !poseAlreadyPrepared)
		{
			SynchronizeSameDataPreviewChildren(adobeAnimateSprite);
		}
	}

	private void SynchronizeSameDataPreviewChildren(AdobeAnimateSprite parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		AdobeAnimateData flashAnimeData = parent.flashAnimeData;
		if (!GodotObject.IsInstanceValid(flashAnimeData))
		{
			return;
		}
		foreach (Node child in parent.GetChildren(includeInternal: true))
		{
			if (child is AdobeAnimateSprite adobeAnimateSprite && adobeAnimateSprite != parent && adobeAnimateSprite.flashAnimeData == flashAnimeData && !(adobeAnimateSprite.clip != parent.clip))
			{
				adobeAnimateSprite.frameIndex = parent.frameIndex;
				adobeAnimateSprite.elapsedTimer = parent.elapsedTimer;
				adobeAnimateSprite.loop = parent.loop;
				adobeAnimateSprite.clipOver = parent.clipOver;
				adobeAnimateSprite.RefreshManagedSlotSpriteCacheForRender();
				adobeAnimateSprite.UpdateMediaReplaceData();
				adobeAnimateSprite.UpdateChild();
				adobeAnimateSprite.QueueRedraw();
			}
		}
	}

	public void FlashCostChange()
	{
		Control control = (GodotObject.IsInstanceValid(layout) ? layout : this);
		if (GodotObject.IsInstanceValid(control))
		{
			if (!_costChangeFlashActive)
			{
				_costChangeFlashBaseScale = control.Scale;
			}
			else
			{
				control.Scale = _costChangeFlashBaseScale;
			}
			_costChangeFlashActive = true;
			_costChangeFlashTween?.Kill();
			ColorSet();
			Color modulate = control.Modulate;
			Vector2 costChangeFlashBaseScale = _costChangeFlashBaseScale;
			Vector2 vector = costChangeFlashBaseScale * 1.14f;
			Color costChangeFlashColor = CostChangeFlashColor;
			Tween tween = (_costChangeFlashTween = CreateTween());
			tween.SetTrans(Tween.TransitionType.Sine);
			tween.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(control, "modulate", costChangeFlashColor, 0.08);
			tween.Parallel().TweenProperty(control, "scale", vector, 0.08);
			tween.TweenInterval(0.1);
			tween.TweenProperty(control, "modulate", modulate, 0.24);
			tween.Parallel().TweenProperty(control, "scale", costChangeFlashBaseScale, 0.24);
			tween.TweenCallback(Callable.From(ResetCostChangeFlash));
		}
	}

	private void ResetCostChangeFlash()
	{
		_costChangeFlashTween = null;
		_costChangeFlashActive = false;
		Control control = (GodotObject.IsInstanceValid(layout) ? layout : this);
		if (GodotObject.IsInstanceValid(control))
		{
			control.Scale = _costChangeFlashBaseScale;
		}
		ColorSet();
	}

	private void PlayPreviewTreeFromStart(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		ConfigurePreviewNode(node);
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.RefreshManagedSlotSpriteCacheForRender();
			adobeAnimateSprite.ResetAnimation();
			adobeAnimateSprite.UpdateMediaReplaceData();
			adobeAnimateSprite.UpdateChild();
			adobeAnimateSprite.SetFrozenPreview(frozen: false);
			adobeAnimateSprite.RefreshProcessScheduling();
			adobeAnimateSprite.QueueRedraw();
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			PlayPreviewTreeFromStart(child);
		}
	}
}
