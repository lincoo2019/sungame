using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBossHealthBarManager : CanvasLayer
{
	private sealed class BossEntry
	{
		public TowerDefenseZombie Boss;

		public ulong Sequence;

		public TowerDefenseBossHealthBarView View;

		public Action HitpointsEmptyHandler;

		public bool Dead;

		public bool Retired;

		public double DeathElapsed;
	}

	private const int MaxVisibleBosses = 3;

	private const float DesktopBarScale = 0.5f;

	private const float DesktopLeft = 12f;

	private const float DesktopTop = 88f;

	private const float DesktopGap = 4f;

	private const float MobileMaxBarScale = 0.5f;

	private const float MobileMinBarScale = 0.3f;

	private const float MobilePreferredFallbackX = 480f;

	private const float MobileMinimumX = 204f;

	private const float MobileTop = 12f;

	private const float MobileFallbackTop = 78f;

	private const float MobileBarGap = 3f;

	private const float MobileFallbackPropGap = 6f;

	private const float MobilePropGap = 8f;

	private const float MobileRightReserved = 118f;

	public const double DeathHoldSeconds = 1.5;

	public const double DeathFadeSeconds = 0.25;

	private static readonly Color ZombieBossColor = Color.Color8(152, 205, 69, 255);

	private static readonly Color ZombieBossDaveColor = Color.Color8(145, 94, 224, 255);

	private static readonly Color ZombieBossEdgarColor = Color.Color8(171, 13, 13, 255);

	private static PackedScene _viewScene;

	private static Texture2D _zombieBossIcon;

	private static Texture2D _zombieBossDaveIcon;

	private static Texture2D _zombieBossEdgarIcon;

	private readonly System.Collections.Generic.Dictionary<TowerDefenseZombie, BossEntry> _entries = new System.Collections.Generic.Dictionary<TowerDefenseZombie, BossEntry>();

	private readonly List<BossEntry> _orderedEntries = new List<BossEntry>();

	private readonly System.Collections.Generic.Dictionary<int, Vector2> _touchPositions = new System.Collections.Generic.Dictionary<int, Vector2>();

	private readonly List<BossEntry> _scratchEntries = new List<BossEntry>();

	private ulong _nextSequence;

	private ulong _lastMembershipRevision = 18446744073709551615uL;

	private bool _mobilePreset;

	private bool _showBossHealthBar;

	private bool _battleRunning;

	private bool _viewsDirty;

	private bool _layoutQueued;

	private bool _hasPhysicalMousePosition;

	private Vector2 _physicalMousePosition;

	private BattleEventBus _eventBus;

	private static PackedScene ViewScene => _viewScene ?? (_viewScene = GD.Load<PackedScene>("res://Prefab/GUI/BossHealthBar/TowerDefenseBossHealthBarView.tscn"));

	private static Texture2D ZombieBossIcon => _zombieBossIcon ?? (_zombieBossIcon = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/BossHealthBar/BossHealthBarZombieBoss.png"));

	private static Texture2D ZombieBossDaveIcon => _zombieBossDaveIcon ?? (_zombieBossDaveIcon = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/BossHealthBar/BossHealthBarZombieBossDave.png"));

	private static Texture2D ZombieBossEdgarIcon => _zombieBossEdgarIcon ?? (_zombieBossEdgarIcon = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/BossHealthBar/BossHealthBarZombieBossEdgarII.png"));

	public int VisibleBarCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < _orderedEntries.Count; i++)
			{
				if (GodotObject.IsInstanceValid(_orderedEntries[i].View))
				{
					num++;
				}
			}
			return num;
		}
	}

	public int TrackedBossCount => _entries.Count;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			_showBossHealthBar = GameSaveManager.Instance.GetConfigValue("ShowBossHealthBar").AsBool();
			_mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
			_battleRunning = IsBattleRunning();
			base.Visible = false;
			_hasPhysicalMousePosition = false;
			SubscribeEvents();
			if (_battleRunning)
			{
				ReconcileBosses(force: true);
				base.Visible = _showBossHealthBar;
			}
		}
	}

	public override void _ExitTree()
	{
		UnsubscribeEvents();
		ClearTrackedBosses();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!(inputEvent is InputEventScreenTouch inputEventScreenTouch))
		{
			if (!(inputEvent is InputEventScreenDrag inputEventScreenDrag))
			{
				if (!(inputEvent is InputEventMouseMotion inputEventMouseMotion))
				{
					if (inputEvent is InputEventMouseButton inputEventMouseButton && (long)inputEventMouseButton.Device != -1)
					{
						_physicalMousePosition = inputEventMouseButton.Position;
						_hasPhysicalMousePosition = true;
					}
				}
				else if ((long)inputEventMouseMotion.Device != -1)
				{
					_physicalMousePosition = inputEventMouseMotion.Position;
					_hasPhysicalMousePosition = true;
				}
			}
			else
			{
				if ((long)inputEventScreenDrag.Device != -1)
				{
					_hasPhysicalMousePosition = false;
				}
				_touchPositions[inputEventScreenDrag.Index] = inputEventScreenDrag.Position;
			}
		}
		else
		{
			if ((long)inputEventScreenTouch.Device != -1)
			{
				_hasPhysicalMousePosition = false;
			}
			if (inputEventScreenTouch.Pressed)
			{
				_touchPositions[inputEventScreenTouch.Index] = inputEventScreenTouch.Position;
			}
			else
			{
				_touchPositions.Remove(inputEventScreenTouch.Index);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		bool flag = IsBattleRunning();
		if (flag != _battleRunning)
		{
			_battleRunning = flag;
			if (!_battleRunning)
			{
				base.Visible = false;
				ClearTrackedBosses();
				return;
			}
			ReconcileBosses(force: true);
			base.Visible = _showBossHealthBar;
		}
		if (!_battleRunning)
		{
			return;
		}
		ReconcileBosses(force: false);
		bool flag2 = false;
		for (int num = _orderedEntries.Count - 1; num >= 0; num--)
		{
			BossEntry bossEntry = _orderedEntries[num];
			if (!bossEntry.Retired)
			{
				if (!bossEntry.Dead && IsBossDead(bossEntry.Boss))
				{
					MarkDead(bossEntry);
				}
				double bodyRatio;
				double shieldRatio;
				if (bossEntry.Dead)
				{
					bossEntry.DeathElapsed += delta;
					if (GodotObject.IsInstanceValid(bossEntry.View))
					{
						float lifecycleAlpha = ((bossEntry.DeathElapsed <= 1.5) ? 1f : (1f - (float)((bossEntry.DeathElapsed - 1.5) / 0.25)));
						bossEntry.View.SetLifecycleAlpha(lifecycleAlpha);
					}
					if (bossEntry.DeathElapsed >= 1.75)
					{
						RetireEntry(bossEntry);
						flag2 = true;
					}
				}
				else if (GodotObject.IsInstanceValid(bossEntry.View) && TryGetBossRatios(bossEntry.Boss, out bodyRatio, out shieldRatio))
				{
					bossEntry.View.SetRatios(bodyRatio, shieldRatio);
				}
			}
		}
		if (flag2 || _viewsDirty)
		{
			_viewsDirty = false;
			AssignViewsAndLayout();
		}
	}

	public override void _Process(double delta)
	{
		if (!base.Visible)
		{
			return;
		}
		for (int i = 0; i < _orderedEntries.Count; i++)
		{
			TowerDefenseBossHealthBarView view = _orderedEntries[i].View;
			if (!GodotObject.IsInstanceValid(view))
			{
				continue;
			}
			bool flag = _hasPhysicalMousePosition && view.ContainsCanvasPoint(_physicalMousePosition);
			if (!flag)
			{
				foreach (Vector2 value in _touchPositions.Values)
				{
					if (view.ContainsCanvasPoint(value))
					{
						flag = true;
						break;
					}
				}
			}
			view.SetPointerOverlap(flag);
		}
	}

	private void SubscribeEvents()
	{
		_eventBus = BattleEventBus.Instance;
		if (GodotObject.IsInstanceValid(_eventBus))
		{
			_eventBus.OnShowBossHealthBar += OnShowBossHealthBar;
			_eventBus.OnUiSwitched += OnUiSwitched;
			_eventBus.OnScreenTransformChanged += OnScreenTransformChanged;
		}
	}

	private void UnsubscribeEvents()
	{
		if (GodotObject.IsInstanceValid(_eventBus))
		{
			_eventBus.OnShowBossHealthBar -= OnShowBossHealthBar;
			_eventBus.OnUiSwitched -= OnUiSwitched;
			_eventBus.OnScreenTransformChanged -= OnScreenTransformChanged;
		}
		_eventBus = null;
	}

	private void OnShowBossHealthBar(bool show)
	{
		_showBossHealthBar = show;
		base.Visible = show && _battleRunning;
		if (base.Visible)
		{
			return;
		}
		for (int i = 0; i < _orderedEntries.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_orderedEntries[i].View))
			{
				_orderedEntries[i].View.SetPointerOverlap(overlaps: false);
			}
		}
	}

	private void OnUiSwitched(bool mobilePreset)
	{
		_mobilePreset = mobilePreset;
		QueueLayout();
	}

	private void OnScreenTransformChanged()
	{
		QueueLayout();
	}

	private void QueueLayout()
	{
		if (!_layoutQueued)
		{
			_layoutQueued = true;
			Callable.From(ApplyQueuedLayout).CallDeferred();
		}
	}

	private void ApplyQueuedLayout()
	{
		_layoutQueued = false;
		if (IsInsideTree() && _battleRunning)
		{
			AssignViewsAndLayout();
			_viewsDirty = false;
		}
	}

	private void ReconcileBosses(bool force)
	{
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
		if (!GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry) || (!force && towerDefenseBattleCharacterRegistry.MembershipRevision == _lastMembershipRevision))
		{
			return;
		}
		_lastMembershipRevision = towerDefenseBattleCharacterRegistry.MembershipRevision;
		HashSet<TowerDefenseZombie> hashSet = new HashSet<TowerDefenseZombie>();
		List<TowerDefenseCharacter> activeCharacters = towerDefenseBattleCharacterRegistry.GetActiveCharacters();
		for (int i = 0; i < activeCharacters.Count; i++)
		{
			if (activeCharacters[i] is TowerDefenseZombie towerDefenseZombie && IsBossCharacter(towerDefenseZombie))
			{
				hashSet.Add(towerDefenseZombie);
				if (!_entries.ContainsKey(towerDefenseZombie))
				{
					TrackBoss(towerDefenseZombie);
				}
			}
		}
		_scratchEntries.Clear();
		_scratchEntries.AddRange(_entries.Values);
		for (int j = 0; j < _scratchEntries.Count; j++)
		{
			BossEntry bossEntry = _scratchEntries[j];
			if (!hashSet.Contains(bossEntry.Boss))
			{
				_entries.Remove(bossEntry.Boss);
				DetachHitpointsHandler(bossEntry);
				if (bossEntry.Dead || IsBossDead(bossEntry.Boss))
				{
					MarkDead(bossEntry);
				}
				else
				{
					RemoveEntry(bossEntry);
				}
			}
		}
		AssignViewsAndLayout();
		_viewsDirty = false;
	}

	private void TrackBoss(TowerDefenseZombie boss)
	{
		BossEntry entry = new BossEntry
		{
			Boss = boss,
			Sequence = _nextSequence++,
			Dead = IsBossDead(boss)
		};
		entry.HitpointsEmptyHandler = delegate
		{
			MarkDead(entry);
		};
		boss.instance.hitpointsEmpty += entry.HitpointsEmptyHandler;
		_entries[boss] = entry;
		_orderedEntries.Add(entry);
	}

	private void AssignViewsAndLayout()
	{
		if (!_battleRunning)
		{
			return;
		}
		_orderedEntries.Sort((BossEntry left, BossEntry right) => left.Sequence.CompareTo(right.Sequence));
		int num = 0;
		for (int num2 = 0; num2 < _orderedEntries.Count; num2++)
		{
			BossEntry bossEntry = _orderedEntries[num2];
			if (!bossEntry.Retired)
			{
				if (num >= 3)
				{
					ReleaseView(bossEntry);
					continue;
				}
				EnsureView(bossEntry);
				LayoutView(bossEntry.View, num);
				num++;
			}
		}
	}

	private void EnsureView(BossEntry entry)
	{
		if (!GodotObject.IsInstanceValid(entry.View))
		{
			entry.View = ViewScene.Instantiate<TowerDefenseBossHealthBarView>(PackedScene.GenEditState.Disabled);
			AddChild(entry.View, forceReadableName: false, InternalMode.Disabled);
			ResolveStyle(entry.Boss, out var icon, out var color);
			entry.View.Configure(icon, color);
			entry.View.SetDead(entry.Dead);
			entry.View.SetLifecycleAlpha(1f);
			if (!entry.Dead && TryGetBossRatios(entry.Boss, out var bodyRatio, out var shieldRatio))
			{
				entry.View.SetRatios(bodyRatio, shieldRatio);
			}
		}
	}

	private void LayoutView(TowerDefenseBossHealthBarView view, int displayIndex)
	{
		if (!GodotObject.IsInstanceValid(view))
		{
			return;
		}
		if (!_mobilePreset)
		{
			view.Scale = Vector2.One * 0.5f;
			view.SetVertical(vertical: true);
			float num = 58.5f;
			view.Position = new Vector2(12f + (float)displayIndex * num, 88f);
			return;
		}
		view.SetVertical(vertical: false);
		Vector2 size = GetViewport().GetVisibleRect().Size;
		Rect2 topPropRect = GetTopPropRect();
		float num2 = ((topPropRect.Size.X > 0f) ? (topPropRect.End.X + 8f) : 480f);
		float num3 = size.X - 118f;
		float num4 = (num3 - num2) / 575f;
		float num5;
		float x;
		float num6;
		if (num4 >= 0.3f)
		{
			num5 = Mathf.Min(0.5f, num4);
			x = num2;
			num6 = 12f;
		}
		else
		{
			num5 = 0.3f;
			x = Mathf.Max(204f, num3 - 575f * num5);
			num6 = Mathf.Max(78f, topPropRect.End.Y + 6f);
		}
		view.Scale = Vector2.One * num5;
		float num7 = 109f * num5 + 3f;
		view.Position = new Vector2(x, num6 + (float)displayIndex * num7);
	}

	private static Rect2 GetTopPropRect()
	{
		HBoxContainer hBoxContainer = TowerDefenseManager.CurrentControl?.uITopPropContainer;
		if (!GodotObject.IsInstanceValid(hBoxContainer) || !hBoxContainer.IsInsideTree())
		{
			return default(Rect2);
		}
		return hBoxContainer.GetGlobalRect();
	}

	private static bool IsBattleRunning()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			return instance.IsGameRunning();
		}
		return false;
	}

	private void ClearTrackedBosses()
	{
		_scratchEntries.Clear();
		_scratchEntries.AddRange(_orderedEntries);
		foreach (BossEntry value in _entries.Values)
		{
			if (!_scratchEntries.Contains(value))
			{
				_scratchEntries.Add(value);
			}
		}
		for (int i = 0; i < _scratchEntries.Count; i++)
		{
			BossEntry entry = _scratchEntries[i];
			DetachHitpointsHandler(entry);
			ReleaseView(entry);
		}
		_entries.Clear();
		_orderedEntries.Clear();
		_touchPositions.Clear();
		_scratchEntries.Clear();
		_nextSequence = 0uL;
		_lastMembershipRevision = 18446744073709551615uL;
		_viewsDirty = false;
	}

	private void MarkDead(BossEntry entry)
	{
		if (entry != null && !entry.Dead && !entry.Retired)
		{
			entry.Dead = true;
			entry.DeathElapsed = 0.0;
			if (GodotObject.IsInstanceValid(entry.View))
			{
				entry.View.SetDead(dead: true);
				return;
			}
			RetireEntry(entry);
			_viewsDirty = true;
		}
	}

	private void RetireEntry(BossEntry entry)
	{
		entry.Retired = true;
		_orderedEntries.Remove(entry);
		DetachHitpointsHandler(entry);
		ReleaseView(entry);
	}

	private void RemoveEntry(BossEntry entry)
	{
		if (entry != null)
		{
			_entries.Remove(entry.Boss);
			_orderedEntries.Remove(entry);
			DetachHitpointsHandler(entry);
			ReleaseView(entry);
		}
	}

	private static void DetachHitpointsHandler(BossEntry entry)
	{
		if (entry != null && entry.HitpointsEmptyHandler != null && GodotObject.IsInstanceValid(entry.Boss?.instance))
		{
			entry.Boss.instance.hitpointsEmpty -= entry.HitpointsEmptyHandler;
			entry.HitpointsEmptyHandler = null;
		}
	}

	private static void ReleaseView(BossEntry entry)
	{
		if (GodotObject.IsInstanceValid(entry.View))
		{
			entry.View.QueueFree();
		}
		entry.View = null;
	}

	private static bool IsBossCharacter(TowerDefenseZombie boss)
	{
		if (GodotObject.IsInstanceValid(boss) && GodotObject.IsInstanceValid(boss.instance))
		{
			return boss.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		}
		return false;
	}

	private static bool IsBossDead(TowerDefenseZombie boss)
	{
		if (GodotObject.IsInstanceValid(boss) && GodotObject.IsInstanceValid(boss.instance) && !boss.instance.die)
		{
			return boss.instance.hitpoints <= 0.0;
		}
		return true;
	}

	private static bool TryGetBossRatios(TowerDefenseZombie boss, out double bodyRatio, out double shieldRatio)
	{
		bodyRatio = 0.0;
		shieldRatio = 0.0;
		if (!IsBossCharacter(boss))
		{
			return false;
		}
		double hitpointsSave = boss.instance.hitpointsSave;
		if (double.IsFinite(hitpointsSave) && hitpointsSave > 0.0 && double.IsFinite(boss.instance.hitpoints))
		{
			bodyRatio = Math.Clamp(boss.instance.hitpoints / hitpointsSave, 0.0, 1.0);
		}
		double num = 0.0;
		double num2 = 0.0;
		Array<TowerDefenseArmorInstance> armorList = boss.instance.armorList;
		for (int i = 0; i < armorList.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = armorList[i];
			if (towerDefenseArmorInstance != null && !towerDefenseArmorInstance.isRemove && (towerDefenseArmorInstance.armorMethodFlags & 0x40) != 0)
			{
				if (double.IsFinite(towerDefenseArmorInstance.hitpointsSave) && towerDefenseArmorInstance.hitpointsSave > 0.0)
				{
					num2 += towerDefenseArmorInstance.hitpointsSave;
				}
				if (double.IsFinite(towerDefenseArmorInstance.hitPoints) && towerDefenseArmorInstance.hitPoints > 0.0)
				{
					num += towerDefenseArmorInstance.hitPoints;
				}
			}
		}
		if (num2 > 0.0)
		{
			shieldRatio = Math.Clamp(num / num2, 0.0, 1.0);
		}
		return true;
	}

	private static void ResolveStyle(TowerDefenseZombie boss, out Texture2D icon, out Color color)
	{
		switch (boss.config?.name)
		{
		case "ZombieBoss":
			icon = ZombieBossIcon;
			color = ZombieBossColor;
			break;
		case "ZombieBossDave":
			icon = ZombieBossDaveIcon;
			color = ZombieBossDaveColor;
			break;
		case "ZombieBossEdgarII":
			icon = ZombieBossEdgarIcon;
			color = ZombieBossEdgarColor;
			break;
		default:
			icon = null;
			color = ZombieBossColor;
			break;
		}
	}
}
