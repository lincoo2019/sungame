using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseInGameSeedBank : Control
{
	private static PackedScene _towerDefenseInGamePacketSlot;

	private static Texture2D _seedBankZombie;

	private static Texture2D _sunBankZombieMobile;

	public Control pcControl;

	public Label pcSunLabel;

	public MarginContainer pcSeedContainer;

	public HBoxContainer pcItemContainer;

	public CanvasItem pcPacketSlotContainer;

	public Node pcPacketContainer;

	public NinePatchRect pcSeedBankTexture;

	public TextureRect pcSunBankTexture;

	public Control mobileControl;

	public Label mobileSunLabel;

	public MarginContainer mobileSeedContainer;

	public HBoxContainer mobileItemContainer;

	public CanvasItem mobilePacketSlotContainer;

	public Node mobilePacketContainer;

	public TextureRect mobileSunBarTexture;

	public MarginContainer mobileISeedContain;

	private double _sunNumShow = 50.0;

	private Label _sunDisplayLabel;

	private long _sunDisplayValue;

	private bool _sunDisplayInitialized;

	private EconomyAccountId _sunAccountId;

	public int packetNum;

	public Array<TowerDefenseInGamePacketShow> packetList = new Array<TowerDefenseInGamePacketShow>();

	private Array<TowerDefenseInGamePacketShow> _packetPool = new Array<TowerDefenseInGamePacketShow>();

	private const int MAX_POOL_SIZE = 24;

	public Dictionary packetNameSet = new Dictionary();

	public Dictionary packetColdDownState = new Dictionary();

	public bool hasGameStarted;

	public bool mobilePreset;

	private bool _referencesInitialized;

	private SceneTree _layoutRefreshTree;

	private Action _layoutRefreshHandler;

	private bool _layoutRefreshPending;

	public Label sunLabel;

	public HBoxContainer itemContainer;

	public CanvasItem packetSlotContainer;

	public Node packetContainer;

	public MarginContainer seedContainer;

	public TextureRect sunBankTexture;

	public AnimationPlayer animationPlayer;

	private static PackedScene TOWER_DEFENSE_IN_GAME_PACKET_SLOT => _towerDefenseInGamePacketSlot ?? (_towerDefenseInGamePacketSlot = GD.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketSlot.tscn"));

	private static Texture2D SEED_BANK_ZOMBIE => _seedBankZombie ?? (_seedBankZombie = GD.Load<Texture2D>("uid://1styv80tn18a"));

	private static Texture2D SUN_BANK_ZOMBIE_MOBILE => _sunBankZombieMobile ?? (_sunBankZombieMobile = GD.Load<Texture2D>("uid://bpdw8ty3ivc67"));

	public EconomyAccountId SunAccountId => _sunAccountId;

	public bool HasSunAccount => _sunAccountId.IsValid;

	public double sunNumShow
	{
		get
		{
			return _sunNumShow;
		}
		set
		{
			long num = (long)Math.Round(value);
			_sunNumShow = value;
			if (sunLabel != null && (!_sunDisplayInitialized || _sunDisplayLabel != sunLabel || _sunDisplayValue != num))
			{
				sunLabel.Text = num.ToString();
				_sunDisplayLabel = sunLabel;
				_sunDisplayValue = num;
				_sunDisplayInitialized = true;
			}
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

	private long GetBoundSun()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return (long)Math.Round(_sunNumShow);
		}
		if (!HasSunAccount)
		{
			return TowerDefenseManager.Instance.GetSun();
		}
		return TowerDefenseManager.Instance.GetSun(_sunAccountId);
	}

	private void SetBoundSun(long value)
	{
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			if (HasSunAccount)
			{
				TowerDefenseManager.Instance.SetSun(_sunAccountId, value);
			}
			else
			{
				TowerDefenseManager.Instance.SetSun(value);
			}
		}
	}

	public void SetMobileMode(bool enabled)
	{
		if (mobilePreset != enabled)
		{
			Node targetContainer = (enabled ? mobilePacketContainer : pcPacketContainer);
			TransferPackets(targetContainer);
			mobilePreset = enabled;
			ApplyMode();
			GameSaveManager.Instance.SetConfigValue("MobilePreset", enabled);
		}
	}

	private void SetModeReferences()
	{
		if (mobilePreset)
		{
			sunLabel = mobileSunLabel;
			itemContainer = mobileItemContainer;
			packetSlotContainer = mobilePacketSlotContainer;
			packetContainer = mobilePacketContainer;
			seedContainer = mobileSeedContainer;
			sunBankTexture = mobileSunBarTexture;
		}
		else
		{
			sunLabel = pcSunLabel;
			itemContainer = pcItemContainer;
			packetSlotContainer = pcPacketSlotContainer;
			packetContainer = pcPacketContainer;
			seedContainer = pcSeedContainer;
			sunBankTexture = pcSunBankTexture;
		}
	}

	private void ApplyMode()
	{
		SetModeReferences();
		if (mobilePreset)
		{
			pcControl.Visible = false;
			mobileControl.Visible = true;
			pcSeedContainer.Visible = false;
			mobilePacketSlotContainer.Visible = true;
			pcPacketSlotContainer.Visible = false;
			base.CustomMinimumSize = new Vector2(0f, base.CustomMinimumSize.Y);
		}
		else
		{
			mobileControl.Visible = false;
			pcControl.Visible = true;
			mobileSeedContainer.Visible = false;
			mobilePacketSlotContainer.Visible = false;
			pcPacketSlotContainer.Visible = true;
			base.CustomMinimumSize = new Vector2(itemContainer.Size.X, base.CustomMinimumSize.Y);
		}
		SyncPacketSlots();
		seedContainer.Visible = true;
		sunNumShow = GetBoundSun();
		itemContainer.Visible = true;
	}

	private void TransferPackets(Node targetContainer)
	{
		if (packetContainer == targetContainer)
		{
			return;
		}
		foreach (Node child in packetContainer.GetChildren())
		{
			if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow)
			{
				towerDefenseInGamePacketShow.Reparent(targetContainer);
			}
		}
	}

	private int GetRequiredSlotNum()
	{
		return Math.Max(GodotObject.IsInstanceValid(TowerDefenseManager.Instance) ? TowerDefenseManager.Instance.seedbankPacketMax : packetList.Count, packetList.Count);
	}

	private void SyncPacketSlots()
	{
		int requiredSlotNum = GetRequiredSlotNum();
		EnsurePacketSlotCount(pcPacketSlotContainer, requiredSlotNum, isMobile: false);
		EnsurePacketSlotCount(mobilePacketSlotContainer, requiredSlotNum, isMobile: true);
		ScheduleLayoutRefresh();
	}

	private void UpdateCustomMinimumSize()
	{
		if (mobilePreset)
		{
			base.CustomMinimumSize = new Vector2(0f, base.CustomMinimumSize.Y);
		}
		else
		{
			base.CustomMinimumSize = new Vector2(seedContainer.Size.X, base.CustomMinimumSize.Y);
		}
	}

	private void EnsurePacketSlots()
	{
		int requiredSlotNum = GetRequiredSlotNum();
		int childCount = pcPacketSlotContainer.GetChildCount();
		if (requiredSlotNum > childCount)
		{
			EnsurePacketSlotCount(pcPacketSlotContainer, requiredSlotNum, isMobile: false);
			EnsurePacketSlotCount(mobilePacketSlotContainer, requiredSlotNum, isMobile: true);
			ScheduleLayoutRefresh();
		}
	}

	private void EnsurePacketSlotCount(Node container, int slotNum, bool isMobile)
	{
		while (container.GetChildCount() > slotNum)
		{
			Node child = container.GetChild(container.GetChildCount() - 1);
			container.RemoveChild(child);
			child.QueueFree();
		}
		while (container.GetChildCount() < slotNum)
		{
			TowerDefenseInGamePacketSlot towerDefenseInGamePacketSlot = TOWER_DEFENSE_IN_GAME_PACKET_SLOT.Instantiate<TowerDefenseInGamePacketSlot>(PackedScene.GenEditState.Disabled);
			container.AddChild(towerDefenseInGamePacketSlot, forceReadableName: false, InternalMode.Disabled);
			towerDefenseInGamePacketSlot.SetContainerFootprint(enabled: true);
			towerDefenseInGamePacketSlot.SetMobileMode(isMobile);
		}
	}

	private void InitPacketSlots(CanvasItem container, int slotNum, bool isMobile)
	{
		EnsurePacketSlotCount(container, slotNum, isMobile);
		container.Visible = false;
	}

	private void ScheduleLayoutRefresh()
	{
		if (!_layoutRefreshPending && IsInsideTree())
		{
			_layoutRefreshTree = GetTree();
			if (GodotObject.IsInstanceValid(_layoutRefreshTree))
			{
				_layoutRefreshPending = true;
				_layoutRefreshHandler = OnLayoutRefreshFrame;
				_layoutRefreshTree.ProcessFrame += _layoutRefreshHandler;
			}
		}
	}

	private void OnLayoutRefreshFrame()
	{
		CancelLayoutRefresh();
		if (IsInsideTree())
		{
			UpdateCustomMinimumSize();
		}
	}

	private void CancelLayoutRefresh()
	{
		if (GodotObject.IsInstanceValid(_layoutRefreshTree) && _layoutRefreshHandler != null)
		{
			_layoutRefreshTree.ProcessFrame -= _layoutRefreshHandler;
		}
		_layoutRefreshTree = null;
		_layoutRefreshHandler = null;
		_layoutRefreshPending = false;
	}

	private void InitializeReferences()
	{
		if (!_referencesInitialized)
		{
			pcControl = GetNode<Control>("%PCControl");
			pcSunLabel = GetNode<Label>("%SunLabel");
			pcSeedContainer = GetNode<MarginContainer>("%SeedContainer");
			pcItemContainer = GetNode<HBoxContainer>("%ItemContainer");
			pcPacketSlotContainer = GetNode<CanvasItem>("%PacketSlotContainer");
			pcPacketContainer = GetNode("%PacketContainer");
			pcSeedBankTexture = GetNode<NinePatchRect>("%SeedBankTexture");
			pcSunBankTexture = GetNode<TextureRect>("%SunBankTexture");
			mobileControl = GetNode<Control>("%MobileControl");
			mobileSunLabel = GetNode<Label>("%MobileSunLabel");
			mobileSeedContainer = GetNode<MarginContainer>("%MobileSeedContainer");
			mobileItemContainer = GetNode<HBoxContainer>("%MobileItemContainer");
			mobilePacketSlotContainer = GetNode<CanvasItem>("%MobilePacketSlotContainer");
			mobilePacketContainer = GetNode("%MobilePacketContainer");
			mobileSunBarTexture = GetNode<TextureRect>("%MobileSunBarTexture");
			mobileISeedContain = GetNode<MarginContainer>("%MobileISeedContanin");
			if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
			{
				BattleEventBus.Instance.OnUiSwitched -= SetMobileMode;
				BattleEventBus.Instance.OnUiSwitched += SetMobileMode;
			}
			int packetSlotNum = TowerDefenseManager.Instance.GetPacketSlotNum();
			InitPacketSlots(pcPacketSlotContainer, packetSlotNum, isMobile: false);
			InitPacketSlots(mobilePacketSlotContainer, packetSlotNum, isMobile: true);
			mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
			SetModeReferences();
			packetList.Clear();
			packetNameSet.Clear();
			_referencesInitialized = true;
		}
	}

	public override void _Ready()
	{
		InitializeReferences();
		Callable.From(FinalizeReady).CallDeferred();
	}

	private void FinalizeReady()
	{
		if (IsInsideTree() && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode())
			{
				pcSeedBankTexture.Texture = SEED_BANK_ZOMBIE;
				mobileSunBarTexture.Texture = SUN_BANK_ZOMBIE_MOBILE;
			}
			ApplyMode();
		}
	}

	public override void _ExitTree()
	{
		CancelLayoutRefresh();
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnUiSwitched -= SetMobileMode;
		}
		DisposeOwnedPackets();
		base._ExitTree();
	}

	public void UpdateDisplay()
	{
		RefreshSunDisplay();
	}

	public void RefreshSunDisplay()
	{
		if (GodotObject.IsInstanceValid(seedContainer) && GodotObject.IsInstanceValid(sunLabel) && !sunLabel.Visible)
		{
			sunLabel.Visible = seedContainer.Visible;
		}
		if (GodotObject.IsInstanceValid(sunBankTexture) && GodotObject.IsInstanceValid(sunLabel) && !sunLabel.Visible)
		{
			sunLabel.Visible = sunBankTexture.Visible;
		}
		sunNumShow = GetBoundSun();
	}

	public void SetSunDisplay(long balance)
	{
		sunNumShow = balance;
	}

	public void ApplyDebugSunMax()
	{
		SetBoundSun(100000L);
	}

	public void RefreshPacketRuntimeStates(bool includeCost)
	{
		for (int i = 0; i < packetList.Count; i++)
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = packetList[i];
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.RefreshRuntimeState(includeCost);
			}
		}
	}

	public bool HasPacket(string packetName)
	{
		return packetNameSet.ContainsKey(packetName);
	}

	public bool CanAddPacket()
	{
		return packetNum < TowerDefenseManager.Instance.seedbankPacketMax;
	}

	public Vector2 GetPacketPos(int id)
	{
		return packetSlotContainer.GetChild<TowerDefenseInGamePacketSlot>(id).GetVisualCenterGlobalPosition();
	}

	private TowerDefenseInGamePacketShow GetPacketFromPool()
	{
		if (_packetPool.Count > 0)
		{
			TowerDefenseInGamePacketShow result = _packetPool[_packetPool.Count - 1];
			_packetPool.RemoveAt(_packetPool.Count - 1);
			return result;
		}
		return TowerDefenseManager.CreatePacketShow();
	}

	private void AttachBattlePacket(TowerDefenseInGamePacketShow packet)
	{
		packet.SetCentralRuntimeStateRefresh(enabled: true);
		packetContainer.AddChild(packet, forceReadableName: false, InternalMode.Disabled);
		packet.SetMobileMode(mobilePreset);
		packet.SetContainerFootprint(enabled: true);
	}

	private static TowerDefensePacketConfig CreateRoundPacketConfig(TowerDefensePacketConfig source)
	{
		if (!GodotObject.IsInstanceValid(source))
		{
			return null;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = source.Duplicate(deep: true) as TowerDefensePacketConfig;
		if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(source._override))
		{
			towerDefensePacketConfig._override = source._override.Duplicate(deep: true) as TowerDefensePacketOverride;
		}
		towerDefensePacketConfig.changeCostList = new List<TowerDefensePacketChangeCost>();
		towerDefensePacketConfig.coldDownDecreaseDictionary = new Dictionary();
		return towerDefensePacketConfig;
	}

	private void ReturnPacketToPool(TowerDefenseInGamePacketShow packet)
	{
		packet.ResetForPool();
		packetContainer.RemoveChild(packet);
		if (_packetPool.Count < 24)
		{
			_packetPool.Add(packet);
		}
		else
		{
			packet.QueueFree();
		}
	}

	public TowerDefenseInGamePacketShow AddPacket(TowerDefensePacketConfig _packetConfig, bool isStart = false)
	{
		if (_packetConfig == null)
		{
			GD.PushError("[SeedBank] AddPacket: _packetConfig is null, skipping");
			return null;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = CreateRoundPacketConfig(_packetConfig);
		if (towerDefensePacketConfig == null)
		{
			GD.PushError("[SeedBank] AddPacket: failed to create round-local packet config, skipping");
			return null;
		}
		InitializeReferences();
		if (packetContainer == null)
		{
			GD.PushError("[SeedBank] AddPacket: packetContainer is null (_Ready not complete), skipping");
			return null;
		}
		packetNum++;
		TowerDefenseInGamePacketShow packetFromPool = GetPacketFromPool();
		AttachBattlePacket(packetFromPool);
		packetFromPool.Init(towerDefensePacketConfig);
		BindPacketSunAccount(packetFromPool);
		packetFromPool.onlyDraw = false;
		packetFromPool.enforceRuntimeAvailabilityOnPress = isStart;
		packetNameSet[towerDefensePacketConfig.saveKey] = true;
		if (!isStart)
		{
			packetFromPool.OnPressed += DeletePacket;
		}
		else
		{
			packetFromPool.alive = true;
			packetFromPool.start = true;
			packetFromPool.OnPressed += TowerDefenseManager.Instance.GetPacketPickControl().PickPacket;
			packetFromPool.StartInit();
		}
		packetList.Add(packetFromPool);
		if (packetFromPool.start)
		{
			packetFromPool.RefreshRuntimeState(includeCost: true);
		}
		EnsurePacketSlots();
		return packetFromPool;
	}

	public void ClearPacketsForProgressRestore()
	{
		InitializeReferences();
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			if (GodotObject.IsInstanceValid(packet))
			{
				packetNameSet.Remove(packet.config?.saveKey ?? "");
				if (packet.GetParent() == packetContainer)
				{
					ReturnPacketToPool(packet);
				}
				else
				{
					packet.QueueFree();
				}
			}
		}
		packetList.Clear();
		packetNameSet.Clear();
		packetColdDownState.Clear();
		packetNum = 0;
		EnsurePacketSlots();
	}

	public TowerDefenseInGamePacketShow RestorePacketFromProgress(TowerDefensePacketConfig packetConfig)
	{
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			GD.PushError("[SeedBank] RestorePacketFromProgress: packetConfig is invalid, skipping");
			return null;
		}
		InitializeReferences();
		if (packetContainer == null)
		{
			GD.PushError("[SeedBank] RestorePacketFromProgress: packetContainer is null, skipping");
			return null;
		}
		TowerDefenseInGamePacketShow packetFromPool = GetPacketFromPool();
		AttachBattlePacket(packetFromPool);
		packetFromPool.Init(packetConfig);
		BindPacketSunAccount(packetFromPool);
		packetFromPool.onlyDraw = false;
		packetNameSet[packetConfig.saveKey] = true;
		packetList.Add(packetFromPool);
		packetNum = packetList.Count;
		EnsurePacketSlots();
		return packetFromPool;
	}

	private void BindPacketSunAccount(TowerDefenseInGamePacketShow packet)
	{
		if (HasSunAccount && !packet.TryBindSunAccount(_sunAccountId))
		{
			GD.PushError("[SeedBank] Packet already belongs to a different Sun account.");
		}
	}

	public void DeletePacket(TowerDefenseInGamePacketShow _packet)
	{
		int num = packetList.IndexOf(_packet);
		if (num < 0)
		{
			if (GodotObject.IsInstanceValid(_packet))
			{
				if (GodotObject.IsInstanceValid(_packet.config))
				{
					packetNameSet.Remove(_packet.config.saveKey);
					string packetName = ((_packet.originalSaveKey != "") ? _packet.originalSaveKey : _packet.config.saveKey);
					TowerDefenseManager.Instance.GetPacketBankFeature()?.PacketAlive(packetName);
				}
				if (_packet.GetParent() == packetContainer)
				{
					ReturnPacketToPool(_packet);
				}
				else
				{
					_packet.QueueFree();
				}
			}
			packetNum = packetList.Count;
		}
		else
		{
			packetNameSet.Remove(_packet.config.saveKey);
			string packetName2 = ((_packet.originalSaveKey != "") ? _packet.originalSaveKey : _packet.config.saveKey);
			TowerDefenseManager.Instance.GetPacketBankFeature()?.PacketAlive(packetName2);
			packetList.RemoveAt(num);
			ReturnPacketToPool(_packet);
			packetNum = packetList.Count;
		}
	}

	public void DeleteAllPacket()
	{
		Array<TowerDefenseInGamePacketShow> array = new Array<TowerDefenseInGamePacketShow>();
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			if (packet.@lock)
			{
				array.Add(packet);
				continue;
			}
			packetNameSet.Remove(packet.config.saveKey);
			string packetName = ((packet.originalSaveKey != "") ? packet.originalSaveKey : packet.config.saveKey);
			TowerDefenseManager.Instance.GetPacketBankFeature()?.PacketAlive(packetName);
			ReturnPacketToPool(packet);
		}
		packetList = array;
		packetNum = packetList.Count;
	}

	public void Prepare()
	{
		packetNameSet.Clear();
		if (hasGameStarted)
		{
			packetColdDownState.Clear();
			foreach (Node child in packetContainer.GetChildren())
			{
				if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.config))
				{
					packetColdDownState[towerDefenseInGamePacketShow.config.saveKey] = new Dictionary
					{
						["coldDownOpen"] = towerDefenseInGamePacketShow.coldDownOpen,
						["coldDownTimer"] = towerDefenseInGamePacketShow.coldDownTimer
					};
				}
			}
		}
		foreach (Node child2 in packetContainer.GetChildren())
		{
			if (child2 is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow2)
			{
				BindPacketSunAccount(towerDefenseInGamePacketShow2);
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow2.config))
				{
					packetNameSet[towerDefenseInGamePacketShow2.config.saveKey] = true;
				}
				towerDefenseInGamePacketShow2.alive = true;
				towerDefenseInGamePacketShow2.@lock = false;
				towerDefenseInGamePacketShow2.onlyDraw = false;
				towerDefenseInGamePacketShow2.enforceRuntimeAvailabilityOnPress = false;
				towerDefenseInGamePacketShow2.start = false;
				towerDefenseInGamePacketShow2.coldDownProgressBar.Visible = false;
				towerDefenseInGamePacketShow2.OnPressed += DeletePacket;
				towerDefenseInGamePacketShow2.OnPressed -= TowerDefenseManager.Instance.GetPacketPickControl().PickPacket;
			}
		}
	}

	public void ReadyPackets()
	{
		PacketPickControl packetPickControl = TowerDefenseManager.Instance?.GetPacketPickControl();
		foreach (Node child in packetContainer.GetChildren())
		{
			if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow)
			{
				towerDefenseInGamePacketShow.alive = false;
				towerDefenseInGamePacketShow.@lock = true;
				towerDefenseInGamePacketShow.onlyDraw = true;
				towerDefenseInGamePacketShow.OnPressed -= DeletePacket;
				if (GodotObject.IsInstanceValid(packetPickControl))
				{
					towerDefenseInGamePacketShow.OnPressed -= packetPickControl.PickPacket;
				}
			}
		}
	}

	public void Start()
	{
		if (!GodotObject.IsInstanceValid(packetContainer))
		{
			return;
		}
		foreach (Node child in packetContainer.GetChildren())
		{
			if (!(child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow))
			{
				continue;
			}
			towerDefenseInGamePacketShow.alive = true;
			towerDefenseInGamePacketShow.@lock = false;
			towerDefenseInGamePacketShow.onlyDraw = false;
			towerDefenseInGamePacketShow.enforceRuntimeAvailabilityOnPress = true;
			towerDefenseInGamePacketShow.start = true;
			towerDefenseInGamePacketShow.StartInit();
			if (packetColdDownState.ContainsKey(towerDefenseInGamePacketShow.config.saveKey))
			{
				Dictionary dictionary = packetColdDownState[towerDefenseInGamePacketShow.config.saveKey].AsGodotDictionary();
				towerDefenseInGamePacketShow.coldDownOpen = dictionary.GetValueOrDefault("coldDownOpen", false).AsBool();
				towerDefenseInGamePacketShow.coldDownTimer = dictionary.GetValueOrDefault("coldDownTimer", 0.0).AsDouble();
				if (towerDefenseInGamePacketShow.coldDownOpen)
				{
					TextureProgressBar coldDownProgressBar = towerDefenseInGamePacketShow.coldDownProgressBar;
					coldDownProgressBar.Visible = true;
					coldDownProgressBar.MaxValue = towerDefenseInGamePacketShow.coldDown;
					coldDownProgressBar.Value = towerDefenseInGamePacketShow.coldDownTimer;
				}
			}
			towerDefenseInGamePacketShow.OnPressed += TowerDefenseManager.Instance.GetPacketPickControl().PickPacket;
			towerDefenseInGamePacketShow.OnPressed -= DeletePacket;
		}
		hasGameStarted = true;
		packetColdDownState.Clear();
		RefreshPacketRuntimeStates(includeCost: true);
	}

	public void StartFromProgress()
	{
		hasGameStarted = true;
		if (!GodotObject.IsInstanceValid(packetContainer))
		{
			return;
		}
		foreach (Node child in packetContainer.GetChildren())
		{
			if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow)
			{
				towerDefenseInGamePacketShow.enforceRuntimeAvailabilityOnPress = true;
				towerDefenseInGamePacketShow.start = true;
				towerDefenseInGamePacketShow.onlyDraw = false;
				towerDefenseInGamePacketShow.OnPressed -= DeletePacket;
				towerDefenseInGamePacketShow.OnPressed -= TowerDefenseManager.Instance.GetPacketPickControl().PickPacket;
				towerDefenseInGamePacketShow.OnPressed += TowerDefenseManager.Instance.GetPacketPickControl().PickPacket;
				towerDefenseInGamePacketShow.OnVisibilityChanged();
			}
		}
		RefreshPacketRuntimeStates(includeCost: true);
	}

	public void DisposeOwnedPackets()
	{
		HashSet<TowerDefenseInGamePacketShow> released = new HashSet<TowerDefenseInGamePacketShow>();
		foreach (TowerDefenseInGamePacketShow packet in packetList)
		{
			ReleasePacket(packet);
		}
		if (GodotObject.IsInstanceValid(pcPacketContainer))
		{
			foreach (Node child in pcPacketContainer.GetChildren())
			{
				ReleasePacket(child as TowerDefenseInGamePacketShow);
			}
		}
		if (GodotObject.IsInstanceValid(mobilePacketContainer))
		{
			foreach (Node child2 in mobilePacketContainer.GetChildren())
			{
				ReleasePacket(child2 as TowerDefenseInGamePacketShow);
			}
		}
		foreach (TowerDefenseInGamePacketShow item in _packetPool)
		{
			ReleasePacket(item);
		}
		packetList.Clear();
		_packetPool.Clear();
		packetNameSet.Clear();
		packetColdDownState.Clear();
		packetNum = 0;
		hasGameStarted = false;
		void ReleasePacket(TowerDefenseInGamePacketShow packet)
		{
			if (GodotObject.IsInstanceValid(packet) && released.Add(packet))
			{
				packet.ClearEventHandlers();
				packet.QueueFree();
			}
		}
	}
}
