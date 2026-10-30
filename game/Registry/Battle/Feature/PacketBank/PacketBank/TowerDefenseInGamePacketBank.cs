using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseInGamePacketBank : Control
{
	private sealed class PendingPacketAnimation
	{
		public TowerDefenseInGamePacketShow AnimationPacket;

		public TowerDefenseInGamePacketShow TargetPacket;

		public Tween Tween;
	}

	private static Texture2D _packetBankZombiePanel;

	public Control translate;

	public NinePatchRect packetBankPanelTexture;

	public ScrollContainer packetBankScroll;

	public MarginContainer packetBankMargin;

	public Control packetContainer;

	public Control animeNode;

	public TextureRect cardSort;

	public AnimationPlayer packetBankAnimationPlayer;

	public Control cardZombie;

	public Control cardItem;

	public Control cardGraveStone;

	public TowerDefenseInGameSeedBank seedBank;

	public bool mobilePreset;

	private Array<TowerDefenseInGamePacketShow> _packetPool = new Array<TowerDefenseInGamePacketShow>();

	private readonly List<PendingPacketAnimation> _pendingPacketAnimations = new List<PendingPacketAnimation>();

	private readonly List<TowerDefensePacketConfig> _virtualPacketConfigs = new List<TowerDefensePacketConfig>();

	private readonly System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow> _visiblePackets = new System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow>();

	private bool _virtualPacketRefreshQueued;

	public TowerDefenseBattleFeaturePacketBank packetBankFeature;

	private static Texture2D PACKET_BANK_ZOMBIE_PANEL => _packetBankZombiePanel ?? (_packetBankZombiePanel = GD.Load<Texture2D>("uid://cpd1x4qxqksuh"));

	public int LogicalPacketCount => _virtualPacketConfigs.Count;

	public int VisiblePacketCount => _visiblePackets.Count;

	private int VirtualColumnCount
	{
		get
		{
			if (!mobilePreset)
			{
				return 11;
			}
			return 6;
		}
	}

	private Vector2 VirtualCellStride
	{
		get
		{
			if (!mobilePreset)
			{
				return new Vector2(52f, 70f);
			}
			return new Vector2(98f, 62f);
		}
	}

	private Vector2 VirtualCardFootprint
	{
		get
		{
			if (!mobilePreset)
			{
				return new Vector2(50f, 70f);
			}
			return new Vector2(96f, 60f);
		}
	}

	public override void _Ready()
	{
		translate = GetNode<Control>("%Translate");
		packetBankPanelTexture = GetNode<NinePatchRect>("%PacketBankPanelTexture");
		packetBankScroll = GetNode<ScrollContainer>("%PacketBankScroll");
		packetBankMargin = GetNode<MarginContainer>("%PacketBankMargin");
		packetContainer = GetNode<Control>("%PacketContainer");
		animeNode = GetNode<Control>("%AnimeNode");
		cardSort = GetNode<TextureRect>("%CardSort");
		packetBankAnimationPlayer = GetNode<AnimationPlayer>("%PacketBankAnimationPlayer");
		cardZombie = GetNode<Control>("%CardZombie");
		cardItem = GetNode<Control>("%CardItem");
		cardGraveStone = GetNode<Control>("%CardGraveStone");
		packetBankScroll.GetVScrollBar().ValueChanged += OnPacketBankScrolled;
		base.VisibilityChanged += QueueVirtualizedPacketRefresh;
		BattleEventBus.Instance.OnUiSwitched += SetMobileMode;
		mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		ApplyMode();
		if (mobilePreset)
		{
			translate.Position = new Vector2(210f, 600f);
		}
		else
		{
			translate.Position = new Vector2(0f, 600f);
		}
		bool visible = CommandManager.Instance.debugPacketOpenAll || Global.Instance.enterLevelMode == "OnlineLevel" || Global.Instance.enterLevelMode == "LoadLevel" || Global.Instance.enterLevelMode == "DiyLevel";
		cardZombie.Visible = visible;
		cardItem.Visible = visible;
		cardGraveStone.Visible = visible;
		Callable.From(FinalizeReady).CallDeferred();
	}

	private void FinalizeReady()
	{
		if (IsInsideTree() && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode()))
		{
			cardSort.Visible = false;
			packetBankPanelTexture.Texture = PACKET_BANK_ZOMBIE_PANEL;
		}
	}

	public override void _ExitTree()
	{
		_virtualPacketRefreshQueued = false;
		base._ExitTree();
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnUiSwitched -= SetMobileMode;
		}
	}

	public void SetMobileMode(bool enabled)
	{
		if (mobilePreset == enabled)
		{
			return;
		}
		bool isHidden = !base.Visible || translate.Position.Y >= 500f;
		if (GodotObject.IsInstanceValid(packetBankAnimationPlayer))
		{
			packetBankAnimationPlayer.Stop();
		}
		mobilePreset = enabled;
		ApplyMode();
		ApplyDisplayPosition(isHidden);
		foreach (TowerDefenseInGamePacketShow value in _visiblePackets.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.SetMobileMode(mobilePreset);
			}
		}
		RefreshVirtualizedPacketBindings();
	}

	private void ApplyDisplayPosition(bool isHidden = false)
	{
		if (mobilePreset)
		{
			if (isHidden)
			{
				translate.Position = new Vector2(210f, 600f);
			}
			else
			{
				translate.Position = new Vector2(210f, 72f);
			}
		}
		else if (isHidden)
		{
			translate.Position = new Vector2(0f, 600f);
		}
		else
		{
			translate.Position = new Vector2(0f, 86f);
		}
	}

	private void ApplyMode()
	{
		if (mobilePreset)
		{
			packetBankScroll.Size = new Vector2(590f, 422f);
			packetBankScroll.Position = new Vector2(10f, 34f);
			packetBankMargin.AddThemeConstantOverride("margin_left", 48);
			packetBankMargin.AddThemeConstantOverride("margin_top", 30);
			packetBankMargin.AddThemeConstantOverride("margin_right", 48);
			packetBankMargin.AddThemeConstantOverride("margin_bottom", 30);
		}
		else
		{
			packetBankScroll.Position = new Vector2(17f, 34f);
			packetBankScroll.Size = new Vector2(580f, 422f);
			packetBankMargin.AddThemeConstantOverride("margin_left", 25);
			packetBankMargin.AddThemeConstantOverride("margin_top", 32);
			packetBankMargin.AddThemeConstantOverride("margin_right", 25);
			packetBankMargin.AddThemeConstantOverride("margin_bottom", 38);
		}
		UpdateVirtualizedContentSize();
	}

	public Vector2 GetCameraPos()
	{
		return GetViewport().GetCamera2D().GlobalPosition;
	}

	public void CreateAnime(TowerDefensePacketConfig config, Vector2 pos)
	{
		if (IsInsideTree() && GodotObject.IsInstanceValid(config) && GodotObject.IsInstanceValid(seedBank) && GodotObject.IsInstanceValid(animeNode))
		{
			Vector2 globalPosition = Global.Instance.GetViewport().GetCamera2D().GlobalPosition;
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			animeNode.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
			towerDefenseInGamePacketShow.Init(config);
			towerDefenseInGamePacketShow.onlyDraw = true;
			towerDefenseInGamePacketShow.GlobalPosition = pos - globalPosition;
			Vector2 packetPos = seedBank.GetPacketPos(seedBank.packetNum);
			TowerDefensePacketConfig packetConfig = config.Duplicate(deep: true) as TowerDefensePacketConfig;
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow2 = seedBank.AddPacket(packetConfig, TowerDefenseManager.Instance.IsGameRunning());
			if (towerDefenseInGamePacketShow2 != null)
			{
				towerDefenseInGamePacketShow2.Visible = false;
			}
			PendingPacketAnimation pending = new PendingPacketAnimation
			{
				AnimationPacket = towerDefenseInGamePacketShow,
				TargetPacket = towerDefenseInGamePacketShow2
			};
			Tween tween = towerDefenseInGamePacketShow.CreateTween();
			pending.Tween = tween;
			_pendingPacketAnimations.Add(pending);
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Quart);
			tween.TweenProperty(towerDefenseInGamePacketShow, "global_position", packetPos, 0.5);
			tween.TweenCallback(Callable.From(delegate
			{
				CompletePacketAnimation(pending, cancelled: false);
			}));
		}
	}

	public void ClearAnimeNode()
	{
		PendingPacketAnimation[] array = _pendingPacketAnimations.ToArray();
		foreach (PendingPacketAnimation pending in array)
		{
			CompletePacketAnimation(pending, cancelled: true);
		}
		foreach (Node child in animeNode.GetChildren())
		{
			child.QueueFree();
		}
	}

	public void CancelPacketAnimation(TowerDefenseInGamePacketShow targetPacket)
	{
		PendingPacketAnimation[] array = _pendingPacketAnimations.ToArray();
		foreach (PendingPacketAnimation pendingPacketAnimation in array)
		{
			if (pendingPacketAnimation.TargetPacket == targetPacket)
			{
				CompletePacketAnimation(pendingPacketAnimation, cancelled: true);
				break;
			}
		}
	}

	private void CompletePacketAnimation(PendingPacketAnimation pending, bool cancelled)
	{
		if (pending != null && _pendingPacketAnimations.Remove(pending))
		{
			if (cancelled && GodotObject.IsInstanceValid(pending.Tween))
			{
				pending.Tween.Kill();
			}
			if (GodotObject.IsInstanceValid(pending.TargetPacket))
			{
				pending.TargetPacket.Visible = true;
			}
			if (GodotObject.IsInstanceValid(pending.AnimationPacket))
			{
				pending.AnimationPacket.QueueFree();
			}
		}
	}

	public void ClearPackets()
	{
		_virtualPacketRefreshQueued = false;
		_virtualPacketConfigs.Clear();
		foreach (TowerDefenseInGamePacketShow value in _visiblePackets.Values)
		{
			ReturnPacketToPool(value);
		}
		_visiblePackets.Clear();
		UpdateVirtualizedContentSize();
	}

	private void ReturnPacketToPool(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			if (GodotObject.IsInstanceValid(packetBankFeature))
			{
				packetBankFeature.UnbindVirtualizedPacket(packet);
			}
			packet.ResetForPool();
			if (packet.GetParent() == packetContainer)
			{
				packetContainer.RemoveChild(packet);
			}
			int num = Math.Max(0, packetBankFeature?.config?.maxPoolSize ?? 96);
			if (_packetPool.Count < num)
			{
				_packetPool.Add(packet);
			}
			else
			{
				packet.QueueFree();
			}
		}
	}

	public void DisposeOwnedPackets()
	{
		if (GodotObject.IsInstanceValid(packetContainer))
		{
			ClearPackets();
		}
		if (GodotObject.IsInstanceValid(animeNode))
		{
			ClearAnimeNode();
		}
		foreach (TowerDefenseInGamePacketShow item in _packetPool)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.QueueFree();
			}
		}
		_packetPool.Clear();
		seedBank = null;
		packetBankFeature = null;
	}

	public TowerDefenseInGamePacketShow GetPacketFromPool()
	{
		if (_packetPool.Count > 0)
		{
			TowerDefenseInGamePacketShow result = _packetPool[_packetPool.Count - 1];
			_packetPool.RemoveAt(_packetPool.Count - 1);
			return result;
		}
		return TowerDefenseManager.CreatePacketShow();
	}

	public void SetVirtualizedPackets(IReadOnlyList<TowerDefensePacketConfig> configs)
	{
		ClearPackets();
		if (configs != null)
		{
			for (int i = 0; i < configs.Count; i++)
			{
				TowerDefensePacketConfig towerDefensePacketConfig = configs[i];
				if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
				{
					_virtualPacketConfigs.Add(towerDefensePacketConfig);
				}
			}
		}
		packetBankScroll.ScrollVertical = 0;
		UpdateVirtualizedContentSize();
		RefreshVirtualizedPacketBindings();
	}

	private void UpdateVirtualizedContentSize()
	{
		if (GodotObject.IsInstanceValid(packetContainer))
		{
			int virtualColumnCount = VirtualColumnCount;
			int num = ((_virtualPacketConfigs.Count != 0) ? Mathf.CeilToInt((float)_virtualPacketConfigs.Count / (float)virtualColumnCount) : 0);
			Vector2 virtualCellStride = VirtualCellStride;
			packetContainer.CustomMinimumSize = ((num == 0) ? Vector2.Zero : new Vector2((float)(virtualColumnCount - 1) * virtualCellStride.X, (float)(num - 1) * virtualCellStride.Y));
		}
	}

	private void OnPacketBankScrolled(double value)
	{
		QueueVirtualizedPacketRefresh();
	}

	public void QueueVirtualizedPacketRefresh()
	{
		if (!_virtualPacketRefreshQueued && IsInsideTree())
		{
			_virtualPacketRefreshQueued = true;
			CallDeferred("RefreshVirtualizedPacketBindings");
		}
	}

	public void RefreshVirtualizedPacketBindings()
	{
		_virtualPacketRefreshQueued = false;
		if (!IsInsideTree() || !GodotObject.IsInstanceValid(packetBankScroll) || !GodotObject.IsInstanceValid(packetContainer) || _virtualPacketConfigs.Count == 0)
		{
			return;
		}
		int virtualColumnCount = VirtualColumnCount;
		float y = VirtualCellStride.Y;
		float num = VirtualCardFootprint.Y / 2f;
		float num2 = packetBankMargin.GetThemeConstant("margin_top");
		float num3 = Math.Max(0f, packetBankScroll.ScrollVertical);
		float num4 = num3 + packetBankScroll.Size.Y;
		int num5 = Math.Max(0, Mathf.CeilToInt((num3 - num2 - num) / y));
		int num6 = Math.Max(num5, Mathf.FloorToInt((num4 - num2 + num) / y));
		int num7 = num5 * virtualColumnCount;
		int num8 = Math.Min(_virtualPacketConfigs.Count - 1, (num6 + 1) * virtualColumnCount - 1);
		List<TowerDefenseInGamePacketShow> list = new List<TowerDefenseInGamePacketShow>();
		List<int> list2 = new List<int>();
		foreach (var (num10, towerDefenseInGamePacketShow2) in _visiblePackets)
		{
			if (num10 < num7 || num10 > num8)
			{
				list2.Add(num10);
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow2))
				{
					list.Add(towerDefenseInGamePacketShow2);
				}
			}
		}
		for (int i = 0; i < list2.Count; i++)
		{
			_visiblePackets.Remove(list2[i]);
		}
		int j = 0;
		for (int k = num7; k <= num8; k++)
		{
			if (_visiblePackets.TryGetValue(k, out var value) && GodotObject.IsInstanceValid(value))
			{
				PositionVirtualizedPacket(value, k);
				continue;
			}
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow3;
			if (j < list.Count)
			{
				towerDefenseInGamePacketShow3 = list[j++];
				if (GodotObject.IsInstanceValid(packetBankFeature))
				{
					packetBankFeature.UnbindVirtualizedPacket(towerDefenseInGamePacketShow3);
				}
				towerDefenseInGamePacketShow3.ResetForPool();
			}
			else
			{
				towerDefenseInGamePacketShow3 = GetPacketFromPool();
				if (towerDefenseInGamePacketShow3.GetParent() != packetContainer)
				{
					packetContainer.AddChild(towerDefenseInGamePacketShow3, forceReadableName: false, InternalMode.Disabled);
				}
			}
			towerDefenseInGamePacketShow3.SetPreviewCreationDeferred(deferred: false);
			towerDefenseInGamePacketShow3.SetMobileMode(mobilePreset);
			towerDefenseInGamePacketShow3.Visible = true;
			PositionVirtualizedPacket(towerDefenseInGamePacketShow3, k);
			if (GodotObject.IsInstanceValid(packetBankFeature))
			{
				packetBankFeature.BindVirtualizedPacket(towerDefenseInGamePacketShow3, _virtualPacketConfigs[k]);
			}
			else
			{
				towerDefenseInGamePacketShow3.Init(_virtualPacketConfigs[k]);
			}
			_visiblePackets[k] = towerDefenseInGamePacketShow3;
		}
		for (; j < list.Count; j++)
		{
			ReturnPacketToPool(list[j]);
		}
	}

	private void PositionVirtualizedPacket(TowerDefenseInGamePacketShow packet, int itemIndex)
	{
		int virtualColumnCount = VirtualColumnCount;
		int num = itemIndex % virtualColumnCount;
		int num2 = itemIndex / virtualColumnCount;
		Vector2 virtualCellStride = VirtualCellStride;
		packet.Position = new Vector2((float)num * virtualCellStride.X, (float)num2 * virtualCellStride.Y);
	}
}
