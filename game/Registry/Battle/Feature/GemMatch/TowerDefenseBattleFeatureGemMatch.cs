using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureGemMatch : TowerDefenseBattleFeature
{
	public enum State
	{
		IDLE,
		SWAPPING,
		SWAP_BACK,
		REMOVING,
		GRAVITY
	}

	private const string META_GEM_MATCH_PACKET = "is_gem_match_packet";

	private const string META_UPGRADE_PACKET = "is_upgrade_packet";

	private const string META_FILL_HOLE_PACKET = "is_fill_hole_packet";

	private const string META_REFRESH_BOARD_PACKET = "is_refresh_board_packet";

	private const string META_UPGRADE_KEY = "gem_match_upgrade_key";

	private const string FILL_HOLE_PACKET_KEY = "CraterDayGround";

	private const string LEGACY_REFRESH_BOARD_PACKET_TEXT_NODE = "GemMatchRefreshBoardText";

	public TowerDefenseBattleFeatureGemMatchConfig config;

	public Array<Array<GemPiece>> grid = new Array<Array<GemPiece>>();

	public Node2D gemNode;

	public bool isProcessing;

	public Vector2I dragStartPos = new Vector2I(-1, -1);

	public bool isDragging;

	public Vector2 dragStartScreenPos = Vector2.Zero;

	public int comboCount;

	public int currentMatchCount;

	public int currentMatchValue;

	private bool _isGemMatchCompleting;

	private bool _finishObjectivePending;

	private EconomyAccountId _activeEconomyAccountId = EconomyAccountId.Local;

	private int _boardRevision;

	private int _lastAppliedBoardRevision = -1;

	private double _clientRelinkTimer;

	private int _holeCount;

	private TowerDefenseInGamePacketShow _fillHolePacket;

	private TowerDefenseInGamePacketShow _refreshBoardPacket;

	private Godot.Collections.Array _matchLines = new Godot.Collections.Array();

	private int _activeTweens;

	private readonly List<Tween> _activeFallTweens = new List<Tween>();

	private Godot.Collections.Array _cellPositions = new Godot.Collections.Array();

	private Vector2 _cellSize = Vector2.Zero;

	private readonly List<(TowerDefenseInGamePacketShow packet, StringName upgradeKey, EconomyAccountId accountId)> _pendingUpgrades = new List<(TowerDefenseInGamePacketShow, StringName, EconomyAccountId)>();

	private int _state;

	private double _stateTimer;

	private Vector2I _swapA = new Vector2I(-1, -1);

	private Vector2I _swapB = new Vector2I(-1, -1);

	private Godot.Collections.Array _pendingMatches = new Godot.Collections.Array();

	private bool _needBoardCheck;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleFeatureGemMatchConfig();
		config.Init(data);
		_boardRevision = 0;
		_lastAppliedBoardRevision = -1;
		_clientRelinkTimer = 0.0;
	}

	public override void OnReady()
	{
	}

	public override Task GameInit()
	{
		gemNode = new Node2D();
		control.AddNode(gemNode, 2);
		_isGemMatchCompleting = false;
		_finishObjectivePending = false;
		_activeEconomyAccountId = EconomyAccountId.Local;
		if (IsRemoteMultiplayerClient() || config.plantList.Count == 0)
		{
			if (!IsRemoteMultiplayerClient() && config.plantList.Count == 0)
			{
				GD.PushWarning("[GemMatch] Cannot initialize a board without configured plants.");
			}
			InitializeEmptyBoard();
		}
		else
		{
			InitializeBoard();
		}
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		gemNode = new Node2D();
		control.AddNode(gemNode, 2);
		_isGemMatchCompleting = false;
		_finishObjectivePending = false;
		_activeEconomyAccountId = EconomyAccountId.Local;
		_CacheCellPositions();
		InitializeEmptyBoard(cacheCellPositions: false);
		return Task.CompletedTask;
	}

	public override void Process(double _delta)
	{
		if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !TowerDefenseManager.Instance.IsGameRunning())
		{
			return;
		}
		if (IsRemoteMultiplayerClient())
		{
			_clientRelinkTimer += Math.Max(0.0, _delta);
			if (_clientRelinkTimer >= 0.25)
			{
				_clientRelinkTimer %= 0.25;
				if (HasUnlinkedGems())
				{
					_LinkExistingPlants();
				}
			}
			_HandleInput();
		}
		else
		{
			_UpdateState(_delta);
			if (_finishObjectivePending)
			{
				TryCompleteMatchObjective();
			}
		}
	}

	public override Task GameStart()
	{
		_isGemMatchCompleting = false;
		_finishObjectivePending = GetClearMatchCountTarget() > 0 && HasEnoughMatchCountToClear();
		_pendingUpgrades.Clear();
		_needBoardCheck = !IsRemoteMultiplayerClient();
		_RefreshProgressMeter(visible: true);
		_LinkExistingPlants();
		_RemoveOldUpgradePackets();
		_InitUpgradePackets();
		if (!IsRemoteMultiplayerClient())
		{
			_ConnectPlantDestroySignals();
		}
		_AddFillHolePacket();
		_AddRefreshBoardPacket();
		Callable.From(_RefreshGemMatchPacketHandlers).CallDeferred();
		return Task.CompletedTask;
	}

	public bool OnWaveReachFinal(TowerDefenseBattleFeatureWave waveFeature)
	{
		if (!GodotObject.IsInstanceValid(waveFeature) || waveFeature.config == null)
		{
			return false;
		}
		if (TryProcessWaveEndMatches(waveFeature))
		{
			return true;
		}
		if (HasEnoughMatchCountToClear())
		{
			return false;
		}
		ResetFinalWaveLoop(waveFeature);
		_RefreshProgressMeter(waveFeature, visible: true);
		return true;
	}

	private bool TryProcessWaveEndMatches(TowerDefenseBattleFeatureWave waveFeature)
	{
		if (!TryStartCurrentMatches())
		{
			return false;
		}
		ResetFinalWaveLoop(waveFeature);
		_RefreshProgressMeter(waveFeature, visible: true);
		return true;
	}

	private bool TryStartCurrentMatches()
	{
		if (isProcessing || _state != 0)
		{
			return false;
		}
		Godot.Collections.Array array = FindMatches();
		if (array.Count == 0)
		{
			return false;
		}
		isProcessing = true;
		comboCount = 0;
		_pendingMatches = array;
		_state = 3;
		_stateTimer = config.matchResolveDelay;
		return true;
	}

	private void _TryAutoEliminateOrShuffle()
	{
		if (!isProcessing && _state == 0 && !TryStartCurrentMatches() && !_CheckPossibleMoves())
		{
			isProcessing = true;
			_ShuffleBoardWithFallEffect();
			_state = 4;
			_stateTimer = 0.0;
		}
	}

	private void ResetFinalWaveLoop(TowerDefenseBattleFeatureWave waveFeature)
	{
		waveFeature.waveFinal = false;
		waveFeature.currentWave = GetFinalFlagLoopStartWave(waveFeature);
		waveFeature.spawnOver = false;
		waveFeature.nextWaveTime = waveFeature.config.spawnColStart;
	}

	public bool HasEnoughMatchCountToClear()
	{
		int clearMatchCountTarget = GetClearMatchCountTarget();
		if (clearMatchCountTarget <= 0)
		{
			return true;
		}
		return currentMatchCount >= clearMatchCountTarget;
	}

	private int GetFinalFlagLoopStartWave(TowerDefenseBattleFeatureWave waveFeature)
	{
		if (!GodotObject.IsInstanceValid(waveFeature) || waveFeature.config == null)
		{
			return 0;
		}
		int flagWaveInterval = waveFeature.config.flagWaveInterval;
		int count = waveFeature.config.wave.Count;
		if (flagWaveInterval <= 0 || count <= 0)
		{
			return 0;
		}
		return Mathf.Max(0, (count - 1) / flagWaveInterval * flagWaveInterval);
	}

	private void _RefreshProgressMeter(bool visible)
	{
		_RefreshProgressMeter(GetFeature("Wave") as TowerDefenseBattleFeatureWave, visible);
	}

	private void _RefreshProgressMeter(TowerDefenseBattleFeatureWave waveFeature, bool visible)
	{
		TowerDefenseBattleFeatureProgress towerDefenseBattleFeatureProgress = GetFeature("Progress") as TowerDefenseBattleFeatureProgress;
		if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureProgress) && config != null)
		{
			if (visible)
			{
				towerDefenseBattleFeatureProgress.SetCustomProgress(currentMatchCount, Mathf.Max(GetClearMatchCountTarget(), 1), _GetProgressMeterMatchText(), true, true);
				return;
			}
			towerDefenseBattleFeatureProgress.ReleaseCustomProgress();
			towerDefenseBattleFeatureProgress.SetProgressMeterVisible(visible: false);
		}
	}

	private string _GetProgressMeterMatchText()
	{
		int clearMatchCountTarget = GetClearMatchCountTarget();
		if (clearMatchCountTarget <= 0)
		{
			return currentMatchCount.ToString();
		}
		return $"{Mathf.Min(currentMatchCount, clearMatchCountTarget)}/{clearMatchCountTarget}";
	}

	private int GetClearMatchCountTarget()
	{
		if (config == null)
		{
			return 0;
		}
		return config.clearMatchCount;
	}

	private void _LinkExistingPlants()
	{
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece == null)
				{
					continue;
				}
				if (gemPiece.isHole)
				{
					_EnsureHoleCrater(gemPiece);
				}
				else
				{
					if (GodotObject.IsInstanceValid(gemPiece.character))
					{
						continue;
					}
					TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(_BoardToMap(gemPiece.gridPos));
					if (mapCell == null)
					{
						continue;
					}
					foreach (TowerDefenseCharacter character in mapCell.characterList)
					{
						if (_CanUseCharacterForGem(character, mapCell))
						{
							gemPiece.character = character;
							gemPiece.characterKey = character.packet.saveKey;
							break;
						}
					}
				}
			}
		}
	}

	private bool HasUnlinkedGems()
	{
		for (int i = 0; i < grid.Count; i++)
		{
			for (int j = 0; j < grid[i].Count; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece != null && !gemPiece.isHole && !GodotObject.IsInstanceValid(gemPiece.character))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void _RemoveOldUpgradePackets()
	{
		if (!(GetFeature("SeedBank") is TowerDefenseBattleFeatureSeedBank { seedBank: not null, seedBank: var seedBank }))
		{
			return;
		}
		Dictionary dictionary = new Dictionary();
		foreach (Dictionary plantUpgrade in config.plantUpgradeList)
		{
			string text = (plantUpgrade.ContainsKey("to") ? ((string)plantUpgrade["to"]) : "");
			if (text != "")
			{
				dictionary[text] = true;
			}
		}
		_AddFillHoleCleanupKeys(dictionary);
		_AddRefreshBoardCleanupKeys(dictionary);
		Array<TowerDefenseInGamePacketShow> array = new Array<TowerDefenseInGamePacketShow>();
		foreach (TowerDefenseInGamePacketShow packet in seedBank.packetList)
		{
			if (GodotObject.IsInstanceValid(packet) && GodotObject.IsInstanceValid(packet.config) && (dictionary.ContainsKey(packet.config.saveKey) || packet.HasMeta("is_gem_match_packet") || packet.HasMeta("is_fill_hole_packet") || packet.HasMeta("is_refresh_board_packet") || packet.HasMeta("gem_match_upgrade_key")))
			{
				array.Add(packet);
			}
		}
		foreach (TowerDefenseInGamePacketShow item in array)
		{
			int num = seedBank.packetList.IndexOf(item);
			if (num >= 0)
			{
				seedBank.packetNameSet.Remove(item.config.saveKey);
				seedBank.packetList.RemoveAt(num);
				seedBank.packetNum--;
			}
			item.QueueFree();
		}
	}

	private void _ConnectPlantDestroySignals()
	{
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gem = grid[i][j];
				_ConnectGemCharacterDestroySignal(gem);
			}
		}
	}

	private void _AddFillHolePacket()
	{
		if (!(GetFeature("SeedBank") is TowerDefenseBattleFeatureSeedBank { seedBank: not null } towerDefenseBattleFeatureSeedBank))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(_GetFillHolePacketKey());
		if (packetConfig != null)
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = towerDefenseBattleFeatureSeedBank.seedBank.AddPacket(packetConfig, isStart: true);
			if (towerDefenseInGamePacketShow != null)
			{
				_ConfigureFillHolePacket(towerDefenseInGamePacketShow);
			}
		}
	}

	private void _UpdateFillHolePacket()
	{
		if (GodotObject.IsInstanceValid(_fillHolePacket))
		{
			if (_holeCount > 0)
			{
				_fillHolePacket.alive = true;
			}
			else
			{
				_fillHolePacket.alive = false;
			}
		}
	}

	private void _OnFillHolePacketPressed(TowerDefenseInGamePacketShow _packet)
	{
		_ReleasePacketPick();
		_ResetFunctionPacketPress(_packet);
		if (IsRemoteMultiplayerClient())
		{
			MultiPlayerManager.Instance?.SendGemMatchCommand("fill_hole");
		}
		else
		{
			ExecuteFillHole(EconomyAccountId.Local);
		}
	}

	private bool ExecuteFillHole(EconomyAccountId accountId)
	{
		if (_holeCount <= 0)
		{
			return false;
		}
		if (_state == 1 || _state == 2 || _state == 3)
		{
			return false;
		}
		Array<Vector2I> array = new Array<Vector2I>();
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece != null && gemPiece.isHole)
				{
					array.Add(new Vector2I(j, i));
				}
			}
		}
		if (array.Count == 0 || config.plantList.Count == 0)
		{
			return false;
		}
		if (!TryBeginGemCost(accountId, config.fillHoleCost, out var spendReceipt))
		{
			return false;
		}
		_activeEconomyAccountId = accountId;
		Vector2I pos = array[(int)(GD.Randi() % (uint)array.Count)];
		GemPiece gemPiece2 = grid[pos.Y][pos.X];
		if (gemPiece2 != null)
		{
			_ClearHoleCrater(gemPiece2);
			gemPiece2.QueueFree();
		}
		grid[pos.Y][pos.X] = null;
		StringName characterKey = config.plantList[(int)(GD.Randi() % (uint)config.plantList.Count)];
		_CreateGemAt(pos, characterKey);
		_holeCount--;
		spendReceipt?.TryCommit();
		_UpdateFillHolePacket();
		MarkBoardChanged();
		TryStartCurrentMatches();
		return true;
	}

	private void _AddRefreshBoardPacket()
	{
		if (!(GetFeature("SeedBank") is TowerDefenseBattleFeatureSeedBank { seedBank: not null } towerDefenseBattleFeatureSeedBank))
		{
			return;
		}
		string text = _GetRefreshBoardPacketKey();
		if (text == "")
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (packetConfig != null)
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = towerDefenseBattleFeatureSeedBank.seedBank.AddPacket(packetConfig, isStart: true);
			if (towerDefenseInGamePacketShow != null)
			{
				_ConfigureRefreshBoardPacket(towerDefenseInGamePacketShow);
			}
		}
	}

	private void _OnRefreshBoardPacketPressed(TowerDefenseInGamePacketShow _packet)
	{
		_ReleasePacketPick();
		_ResetFunctionPacketPress(_packet);
		if (IsRemoteMultiplayerClient())
		{
			MultiPlayerManager.Instance?.SendGemMatchCommand("refresh");
		}
		else
		{
			ExecuteRefreshBoard(EconomyAccountId.Local);
		}
	}

	private bool ExecuteRefreshBoard(EconomyAccountId accountId)
	{
		if (isProcessing || _state != 0)
		{
			return false;
		}
		if (!TryBeginGemCost(accountId, config.refreshBoardCost, out var spendReceipt))
		{
			return false;
		}
		_activeEconomyAccountId = accountId;
		_RefreshBoard();
		spendReceipt?.TryCommit();
		return true;
	}

	private void _RefreshBoard()
	{
		isProcessing = true;
		isDragging = false;
		dragStartPos = new Vector2I(-1, -1);
		_swapA = new Vector2I(-1, -1);
		_swapB = new Vector2I(-1, -1);
		_pendingMatches = new Godot.Collections.Array();
		_matchLines.Clear();
		comboCount = 0;
		_needBoardCheck = false;
		_ShuffleBoardWithFallEffect();
		_state = 4;
		_stateTimer = 0.0;
	}

	private void _InitUpgradePackets()
	{
		Dictionary dictionary = new Dictionary();
		foreach (StringName plant in config.plantList)
		{
			StringName upgradeTarget = config.GetUpgradeTarget(plant);
			if ((string?)upgradeTarget != "" && !dictionary.ContainsKey(upgradeTarget))
			{
				dictionary[upgradeTarget] = true;
				_AddUpgradePacket(upgradeTarget);
			}
		}
	}

	private string _GetFillHolePacketKey()
	{
		return "CraterDayGround";
	}

	private string _GetRefreshBoardPacketKey()
	{
		return config.refreshBoardPacketKey;
	}

	private void _AddFillHoleCleanupKeys(Dictionary functionPacketKeys)
	{
		_AddCleanupKeyChain(functionPacketKeys, _GetFillHolePacketKey());
		if (config.plantList.Count > 0)
		{
			_AddCleanupKeyChain(functionPacketKeys, config.plantList[0]);
		}
	}

	private void _AddRefreshBoardCleanupKeys(Dictionary functionPacketKeys)
	{
		string text = _GetRefreshBoardPacketKey();
		if (text != "")
		{
			functionPacketKeys[text] = true;
		}
	}

	private void _AddCleanupKeyChain(Dictionary functionPacketKeys, string fillHoleKey)
	{
		Dictionary dictionary = new Dictionary();
		int num = 0;
		while (fillHoleKey != "" && !dictionary.ContainsKey(fillHoleKey) && num < 20)
		{
			dictionary[fillHoleKey] = true;
			functionPacketKeys[fillHoleKey] = true;
			fillHoleKey = config.GetUpgradeSource(new StringName(fillHoleKey));
			num++;
		}
	}

	private void _ClearGemMatchPacketMeta(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			if (packet.HasMeta("is_gem_match_packet"))
			{
				packet.RemoveMeta("is_gem_match_packet");
			}
			if (packet.HasMeta("is_fill_hole_packet"))
			{
				packet.RemoveMeta("is_fill_hole_packet");
			}
			if (packet.HasMeta("is_refresh_board_packet"))
			{
				packet.RemoveMeta("is_refresh_board_packet");
			}
			if (packet.HasMeta("is_upgrade_packet"))
			{
				packet.RemoveMeta("is_upgrade_packet");
			}
			if (packet.HasMeta("gem_match_upgrade_key"))
			{
				packet.RemoveMeta("gem_match_upgrade_key");
			}
		}
	}

	private void _ConfigureFunctionPacket(TowerDefenseInGamePacketShow packet, int cost, bool alive)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.ClearEventHandlers();
			_ClearGemMatchPacketMeta(packet);
			_RestoreFunctionPacketVisual(packet);
			packet.start = false;
			packet.coldDownOpen = false;
			packet.coldDownTimer = 0.0;
			if (GodotObject.IsInstanceValid(packet.coldDownProgressBar))
			{
				packet.coldDownProgressBar.Visible = false;
			}
			packet.riseCost = -1;
			packet.costMultiple = -1.0;
			packet.baseItemCost = cost;
			packet.itemCost = cost;
			packet.SetMeta("is_gem_match_packet", true);
			packet.SetMeta("is_upgrade_packet", true);
			packet.select = false;
			packet.alive = alive;
			packet.RefreshRuntimeState();
		}
	}

	private void _ConfigureFillHolePacket(TowerDefenseInGamePacketShow packet)
	{
		_ConfigureFunctionPacket(packet, config.fillHoleCost, alive: false);
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.SetMeta("is_fill_hole_packet", true);
			packet.OnPressed += _OnFillHolePacketPressed;
			_fillHolePacket = packet;
			_UpdateFillHolePacket();
		}
	}

	private void _ConfigureRefreshBoardPacket(TowerDefenseInGamePacketShow packet)
	{
		_ConfigureFunctionPacket(packet, config.refreshBoardCost, alive: true);
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.SetMeta("is_refresh_board_packet", true);
			packet.OnPressed += _OnRefreshBoardPacketPressed;
			_refreshBoardPacket = packet;
		}
	}

	private void _RefreshFillHolePacketConfig()
	{
		if (!GodotObject.IsInstanceValid(_fillHolePacket))
		{
			return;
		}
		string text = _GetFillHolePacketKey();
		if (text == "")
		{
			return;
		}
		if (GodotObject.IsInstanceValid(_fillHolePacket.config) && _fillHolePacket.config.saveKey == text)
		{
			_ConfigureFillHolePacket(_fillHolePacket);
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (packetConfig != null)
		{
			TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
			if (towerDefenseBattleFeatureSeedBank != null && towerDefenseBattleFeatureSeedBank.seedBank != null && GodotObject.IsInstanceValid(_fillHolePacket.config))
			{
				towerDefenseBattleFeatureSeedBank.seedBank.packetNameSet.Remove(_fillHolePacket.config.saveKey);
			}
			_fillHolePacket.Init(packetConfig);
			if (towerDefenseBattleFeatureSeedBank != null && towerDefenseBattleFeatureSeedBank.seedBank != null)
			{
				towerDefenseBattleFeatureSeedBank.seedBank.packetNameSet[packetConfig.saveKey] = true;
			}
			_ConfigureFillHolePacket(_fillHolePacket);
		}
	}

	private void _ConfigureUpgradePacket(TowerDefenseInGamePacketShow packet, StringName upgradeKey, int upgradeCost)
	{
		_ConfigureFunctionPacket(packet, upgradeCost, alive: true);
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.SetMeta("gem_match_upgrade_key", (string?)upgradeKey);
			packet.OnPressed += delegate(TowerDefenseInGamePacketShow p)
			{
				_OnUpgradePacketPressed(p, upgradeKey);
			};
		}
	}

	private void _RefreshGemMatchPacketHandlers()
	{
		if (!base.IsLifetimeActive || config == null || !(GetFeature("SeedBank") is TowerDefenseBattleFeatureSeedBank { seedBank: not null } towerDefenseBattleFeatureSeedBank))
		{
			return;
		}
		foreach (TowerDefenseInGamePacketShow packet in towerDefenseBattleFeatureSeedBank.seedBank.packetList)
		{
			if (GodotObject.IsInstanceValid(packet))
			{
				if (packet.HasMeta("is_fill_hole_packet"))
				{
					_ConfigureFillHolePacket(packet);
				}
				else if (packet.HasMeta("is_refresh_board_packet"))
				{
					_ConfigureRefreshBoardPacket(packet);
				}
				else if (packet.HasMeta("gem_match_upgrade_key"))
				{
					StringName stringName = new StringName(packet.GetMeta("gem_match_upgrade_key").AsString());
					StringName upgradeSource = config.GetUpgradeSource(stringName);
					_ConfigureUpgradePacket(packet, stringName, config.GetUpgradeCost(upgradeSource));
				}
			}
		}
	}

	private void _ResetFunctionPacketPress(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.select = false;
			packet.Reset();
		}
	}

	private void _ReleasePacketPick()
	{
		PacketPickControl packetPickControl = TowerDefenseManager.Instance.GetPacketPickControl();
		if (GodotObject.IsInstanceValid(packetPickControl))
		{
			packetPickControl.Release();
		}
	}

	private void _RestoreFunctionPacketVisual(TowerDefenseInGamePacketShow packet)
	{
		if (!GodotObject.IsInstanceValid(packet))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(packet.previewClip))
		{
			packet.previewClip.Visible = true;
		}
		if (GodotObject.IsInstanceValid(packet.backgroundTexture))
		{
			Label nodeOrNull = packet.backgroundTexture.GetNodeOrNull<Label>("GemMatchRefreshBoardText");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.QueueFree();
			}
		}
	}

	private void _ConnectGemCharacterDestroySignal(GemPiece gem)
	{
		if (gem != null && !gem.isHole && GodotObject.IsInstanceValid(gem.character))
		{
			gem.character.OnDestroy -= _OnPlantDestroyed;
			gem.character.OnDestroy += _OnPlantDestroyed;
		}
	}

	private bool _CanUseCharacterForGem(TowerDefenseCharacter character, TowerDefenseCellInstance cell)
	{
		if (!GodotObject.IsInstanceValid(character) || !(character is TowerDefensePlant))
		{
			return false;
		}
		if (character.packet == null)
		{
			return false;
		}
		if (!_IsConfiguredPlantKey(character.packet.saveKey))
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(cell) && cell.GetSurround() == character)
		{
			return false;
		}
		if (character.config != null && character.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.SURROUND))
		{
			return false;
		}
		return true;
	}

	private bool _IsConfiguredPlantKey(StringName plantKey)
	{
		for (int i = 0; i < config.plantList.Count; i++)
		{
			if (config.plantList[i] == plantKey)
			{
				return true;
			}
		}
		return false;
	}

	private void _SetGemAsHole(GemPiece gem, bool destroyExistingCharacter = false)
	{
		if (gem == null || gem.isHole)
		{
			return;
		}
		TowerDefenseCharacter character = gem.character;
		if (GodotObject.IsInstanceValid(character))
		{
			character.OnDestroy -= _OnPlantDestroyed;
			if (destroyExistingCharacter)
			{
				character.Destroy();
			}
		}
		gem.SetAsHole();
		Callable.From(delegate
		{
			if (base.IsLifetimeActive)
			{
				_EnsureHoleCrater(gem);
			}
		}).CallDeferred();
		_holeCount++;
		_UpdateFillHolePacket();
		MarkBoardChanged();
	}

	private void _EnsureHoleCrater(GemPiece gem)
	{
		if (!GodotObject.IsInstanceValid(gem) || !gem.isHole)
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = _FindHoleCrater(gem.gridPos);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("CraterDayGround");
			if (packetConfig == null)
			{
				return;
			}
			towerDefenseCharacter = packetConfig.Plant(_activeEconomyAccountId, _BoardToMap(gem.gridPos), playAudio: false, noLimit: true);
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			if (towerDefenseCharacter is TowerDefenseCrater towerDefenseCrater)
			{
				towerDefenseCrater.weatheringDisabled = true;
			}
			gem.character = towerDefenseCharacter;
			gem.characterKey = "CraterDayGround";
		}
	}

	private TowerDefenseCharacter _FindHoleCrater(Vector2I boardPos)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(_BoardToMap(boardPos));
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return null;
		}
		foreach (TowerDefenseCharacter character in mapCell.characterList)
		{
			if (GodotObject.IsInstanceValid(character) && character is TowerDefenseCrater)
			{
				if (character.packet != null && character.packet.saveKey == "CraterDayGround")
				{
					return character;
				}
				if (character.config != null && character.config.name == "CraterDayGround")
				{
					return character;
				}
			}
		}
		return null;
	}

	private void _ClearHoleCrater(GemPiece gem)
	{
		if (GodotObject.IsInstanceValid(gem))
		{
			TowerDefenseCharacter towerDefenseCharacter = ((GodotObject.IsInstanceValid(gem.character) && gem.character is TowerDefenseCrater) ? gem.character : _FindHoleCrater(gem.gridPos));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.IsInsideTree())
			{
				towerDefenseCharacter.Destroy();
			}
			gem.character = null;
			gem.characterKey = "";
		}
	}

	public override void Destroy()
	{
		_DisconnectExternalCallbacks();
		_StopActiveFallTweens();
		_RefreshProgressMeter(visible: false);
		_pendingUpgrades.Clear();
		for (int i = 0; i < grid.Count; i++)
		{
			for (int j = 0; j < grid[i].Count; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece != null && gemPiece.isHole)
				{
					_ClearHoleCrater(gemPiece);
				}
			}
		}
		if (GodotObject.IsInstanceValid(gemNode))
		{
			gemNode.QueueFree();
			gemNode = null;
		}
		grid.Clear();
		_pendingMatches.Clear();
		_matchLines.Clear();
		_cellPositions.Clear();
		_fillHolePacket = null;
		_refreshBoardPacket = null;
		isProcessing = false;
		isDragging = false;
		_needBoardCheck = false;
		config = null;
		base.Destroy();
	}

	private void _DisconnectExternalCallbacks()
	{
		for (int i = 0; i < grid.Count; i++)
		{
			for (int j = 0; j < grid[i].Count; j++)
			{
				TowerDefenseCharacter towerDefenseCharacter = grid[i][j]?.character;
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					towerDefenseCharacter.OnDestroy -= _OnPlantDestroyed;
				}
			}
		}
		TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		if (towerDefenseBattleFeatureSeedBank?.seedBank == null)
		{
			return;
		}
		foreach (TowerDefenseInGamePacketShow packet in towerDefenseBattleFeatureSeedBank.seedBank.packetList)
		{
			if (GodotObject.IsInstanceValid(packet) && packet.HasMeta("is_gem_match_packet"))
			{
				packet.ClearEventHandlers();
			}
		}
	}

	private void _StopActiveFallTweens()
	{
		foreach (Tween activeFallTween in _activeFallTweens)
		{
			if (GodotObject.IsInstanceValid(activeFallTween))
			{
				activeFallTween.Kill();
			}
		}
		_activeFallTweens.Clear();
		_activeTweens = 0;
	}

	private void _UpdateState(double delta)
	{
		switch (_state)
		{
		case 0:
			if (_pendingUpgrades.Count > 0)
			{
				_ProcessPendingUpgrades();
				_needBoardCheck = true;
			}
			if (_needBoardCheck && !isProcessing)
			{
				_needBoardCheck = false;
				_TryAutoEliminateOrShuffle();
			}
			if (_state == 0)
			{
				_HandleInput();
			}
			break;
		case 1:
			_stateTimer -= delta;
			if (_stateTimer <= 0.0)
			{
				Godot.Collections.Array array = FindMatches();
				if (array.Count == 0)
				{
					_SwapInGrid(_swapA, _swapB);
					_state = 2;
					_stateTimer = config.swapDuration;
				}
				else
				{
					_pendingMatches = array;
					_state = 3;
					_stateTimer = config.matchResolveDelay;
				}
			}
			break;
		case 2:
			_stateTimer -= delta;
			if (_stateTimer <= 0.0)
			{
				_state = 0;
				isProcessing = false;
			}
			break;
		case 3:
			_stateTimer -= delta;
			if (_stateTimer <= 0.0)
			{
				_RemoveMatches(_pendingMatches);
				_pendingMatches = new Godot.Collections.Array();
				_ApplyGravityAndFill();
				_state = 4;
			}
			break;
		case 4:
			if (!_HasActiveFallTweens())
			{
				_pendingMatches = FindMatches();
				if (_pendingMatches.Count == 0 && !_CheckPossibleMoves())
				{
					ShuffleBoard();
					_pendingMatches = FindMatches();
				}
				if (_pendingMatches.Count > 0)
				{
					comboCount++;
					_state = 3;
					_stateTimer = config.matchResolveDelay;
				}
				else
				{
					_state = 0;
					isProcessing = false;
				}
			}
			break;
		}
	}

	private bool TryStartSwap(Vector2I a, Vector2I b, EconomyAccountId accountId)
	{
		if (!accountId.IsValid || isProcessing || _state != 0 || !_IsValidPos(a) || !_IsValidPos(b) || Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) != 1)
		{
			return false;
		}
		GemPiece gemPiece = grid[a.Y][a.X];
		GemPiece gemPiece2 = grid[b.Y][b.X];
		if (gemPiece == null || gemPiece2 == null || gemPiece.isHole || gemPiece2.isHole)
		{
			return false;
		}
		_activeEconomyAccountId = accountId;
		isProcessing = true;
		comboCount = 0;
		_swapA = a;
		_swapB = b;
		_SwapInGrid(a, b);
		_state = 1;
		_stateTimer = config.swapDuration;
		MarkBoardChanged();
		return true;
	}

	private static bool IsRemoteMultiplayerClient()
	{
		if (Global.IsMultiplayerMode)
		{
			return !MultiPlayerManager.IsHost;
		}
		return false;
	}

	private void InitializeEmptyBoard(bool cacheCellPositions = true)
	{
		grid.Clear();
		if (cacheCellPositions)
		{
			_CacheCellPositions();
		}
		for (int i = 0; i < config.boardRows; i++)
		{
			Array<GemPiece> array = new Array<GemPiece>();
			array.Resize(config.boardCols);
			grid.Add(array);
		}
	}

	private void InitializeBoard()
	{
		InitializeEmptyBoard();
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				Vector2I pos = new Vector2I(j, i);
				StringName characterKey = _GetNonMatchingPlant(pos);
				_CreateGemAt(pos, characterKey);
			}
		}
		_ResolveInitialMatches();
		MarkBoardChanged();
	}

	private StringName _GetNonMatchingPlant(Vector2I pos)
	{
		Dictionary dictionary = new Dictionary();
		StringName stringName = _GetPlantKeyAt(new Vector2I(pos.X - 1, pos.Y));
		StringName stringName2 = _GetPlantKeyAt(new Vector2I(pos.X - 2, pos.Y));
		if ((string?)stringName != "" && stringName == stringName2)
		{
			dictionary[stringName] = true;
		}
		StringName stringName3 = _GetPlantKeyAt(new Vector2I(pos.X, pos.Y - 1));
		StringName stringName4 = _GetPlantKeyAt(new Vector2I(pos.X, pos.Y - 2));
		if ((string?)stringName3 != "" && stringName3 == stringName4)
		{
			dictionary[stringName3] = true;
		}
		StringName stringName5 = config.plantList[(int)(GD.Randi() % (uint)config.plantList.Count)];
		int num = 0;
		while (dictionary.ContainsKey(stringName5) && num < 20)
		{
			stringName5 = config.plantList[(int)(GD.Randi() % (uint)config.plantList.Count)];
			num++;
		}
		return stringName5;
	}

	private StringName _GetPlantKeyAt(Vector2I pos)
	{
		if (!_IsValidPos(pos))
		{
			return "";
		}
		GemPiece gemPiece = grid[pos.Y][pos.X];
		if (gemPiece == null)
		{
			return "";
		}
		return gemPiece.characterKey;
	}

	private void _ResolveInitialMatches()
	{
		if (config.plantList.Count < 3)
		{
			return;
		}
		Godot.Collections.Array array = FindMatches();
		int num = 0;
		while (array.Count > 0 && num < 50)
		{
			foreach (Variant item in array)
			{
				GemPiece gemPiece = (GemPiece)(GodotObject)item;
				Dictionary dictionary = new Dictionary();
				StringName stringName = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X - 1, gemPiece.gridPos.Y));
				StringName stringName2 = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X - 2, gemPiece.gridPos.Y));
				if ((string?)stringName != "" && stringName == stringName2)
				{
					dictionary[stringName] = true;
				}
				StringName stringName3 = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X + 1, gemPiece.gridPos.Y));
				StringName stringName4 = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X + 2, gemPiece.gridPos.Y));
				if ((string?)stringName3 != "" && stringName3 == stringName4)
				{
					dictionary[stringName3] = true;
				}
				StringName stringName5 = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X, gemPiece.gridPos.Y - 1));
				StringName stringName6 = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X, gemPiece.gridPos.Y - 2));
				if ((string?)stringName5 != "" && stringName5 == stringName6)
				{
					dictionary[stringName5] = true;
				}
				StringName stringName7 = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X, gemPiece.gridPos.Y + 1));
				StringName stringName8 = _GetPlantKeyAt(new Vector2I(gemPiece.gridPos.X, gemPiece.gridPos.Y + 2));
				if ((string?)stringName7 != "" && stringName7 == stringName8)
				{
					dictionary[stringName7] = true;
				}
				StringName stringName9 = config.plantList[(int)(GD.Randi() % (uint)config.plantList.Count)];
				int num2 = 0;
				while (dictionary.ContainsKey(stringName9) && num2 < 20)
				{
					stringName9 = config.plantList[(int)(GD.Randi() % (uint)config.plantList.Count)];
					num2++;
				}
				gemPiece.characterKey = stringName9;
				if (GodotObject.IsInstanceValid(gemPiece.character))
				{
					gemPiece.character.Destroy();
				}
				_PlantCharacter(gemPiece);
			}
			array = FindMatches();
			num++;
		}
	}

	private GemPiece _CreateGemAt(Vector2I pos, StringName characterKey)
	{
		GemPiece gemPiece = new GemPiece();
		gemPiece.Setup(pos, characterKey);
		gemNode.AddChild(gemPiece, forceReadableName: false, Node.InternalMode.Disabled);
		gemPiece.GlobalPosition = _GridToWorld(pos);
		grid[pos.Y][pos.X] = gemPiece;
		_PlantCharacter(gemPiece);
		_ConnectGemCharacterDestroySignal(gemPiece);
		return gemPiece;
	}

	private void _PlantCharacter(GemPiece gem)
	{
		Vector2I gridPos = _BoardToMap(gem.gridPos);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		if (mapCell != null)
		{
			foreach (TowerDefenseCharacter character in mapCell.characterList)
			{
				if (_CanUseCharacterForGem(character, mapCell))
				{
					gem.character = character;
					gem.characterKey = character.packet.saveKey;
					return;
				}
			}
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(gem.characterKey);
		if (packetConfig != null)
		{
			gem.character = packetConfig.Plant(_activeEconomyAccountId, gridPos, playAudio: true, noLimit: true);
		}
	}

	private void _HandleInput()
	{
		if (Input.IsActionJustPressed("Press"))
		{
			Vector2I pos = _ScreenToGrid(_GetMousePosition());
			if (_IsValidPos(pos))
			{
				GemPiece gemPiece = grid[pos.Y][pos.X];
				if (gemPiece != null && !gemPiece.isHole)
				{
					dragStartPos = pos;
					isDragging = true;
					dragStartScreenPos = _GetMousePosition();
				}
			}
		}
		if (!isDragging || !Input.IsActionJustReleased("Press"))
		{
			return;
		}
		isDragging = false;
		if (dragStartPos == new Vector2I(-1, -1))
		{
			return;
		}
		Vector2 vector = _GetMousePosition() - dragStartScreenPos;
		if (vector.Length() < config.dragThreshold)
		{
			dragStartPos = new Vector2I(-1, -1);
			return;
		}
		Vector2I vector2I = Vector2I.Zero;
		vector2I = ((!(Mathf.Abs(vector.X) > Mathf.Abs(vector.Y))) ? new Vector2I(0, (vector.Y > 0f) ? 1 : (-1)) : new Vector2I((vector.X > 0f) ? 1 : (-1), 0));
		Vector2I vector2I2 = dragStartPos + vector2I;
		if (_IsValidPos(vector2I2))
		{
			GemPiece gemPiece2 = grid[vector2I2.Y][vector2I2.X];
			if (gemPiece2 != null && !gemPiece2.isHole)
			{
				if (IsRemoteMultiplayerClient())
				{
					MultiPlayerManager.Instance?.SendGemMatchCommand("swap", dragStartPos, vector2I2);
				}
				else
				{
					TryStartSwap(dragStartPos, vector2I2, EconomyAccountId.Local);
				}
			}
		}
		dragStartPos = new Vector2I(-1, -1);
	}

	private Vector2 _GetMousePosition()
	{
		return Global.Instance.GetViewport().GetMousePosition();
	}

	private void _SwapInGrid(Vector2I a, Vector2I b)
	{
		GemPiece gemPiece = grid[a.Y][a.X];
		GemPiece gemPiece2 = grid[b.Y][b.X];
		grid[a.Y][a.X] = gemPiece2;
		grid[b.Y][b.X] = gemPiece;
		if (gemPiece != null)
		{
			gemPiece.SetGridPos(b);
			_SwapCharacterAnimated(gemPiece, config.swapDuration);
		}
		if (gemPiece2 != null)
		{
			gemPiece2.SetGridPos(a);
			_SwapCharacterAnimated(gemPiece2, config.swapDuration);
		}
	}

	private void _SwapCharacterAnimated(GemPiece gem, double duration)
	{
		if (!GodotObject.IsInstanceValid(gem.character))
		{
			return;
		}
		Vector2I vector2I = _BoardToMap(gem.gridPos);
		Vector2I gridPos = gem.character.gridPos;
		if (gridPos == vector2I)
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(vector2I);
		if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(mapCell2))
		{
			Vector2 logicalGlobalPosition = gem.character.GetLogicalGlobalPosition();
			Vector2 vector = Vector2.Zero;
			ShadowComponent shadowComponent = gem.character.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				vector = gem.character.shadowComponent.saveShadowPosition;
			}
			mapCell.RemoveCharacter(gem.character);
			mapCell2.CharacterPlant(gem.character.packet, gem.character, noLimit: true);
			gem.character.gridPos = vector2I;
			gem.character.SetLogicalGlobalPosition(logicalGlobalPosition);
			ShadowComponent shadowComponent2 = gem.character.shadowComponent;
			if (shadowComponent2 != null && !shadowComponent2.IsReleased)
			{
				gem.character.shadowComponent.SetSaveShadowPosition(vector);
			}
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(vector2I);
			Tween tween = gem.character.CreateTween();
			tween.SetParallel();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Quad);
			tween.TweenMethod(Callable.From<Vector2>(gem.character.SetLogicalGlobalPosition), logicalGlobalPosition, mapCellPlantPos, duration);
			ShadowComponent shadowComponent3 = gem.character.shadowComponent;
			if (shadowComponent3 != null && !shadowComponent3.IsReleased)
			{
				Vector2 target = vector + mapCellPlantPos - logicalGlobalPosition;
				gem.character.shadowComponent.TweenSaveShadowPosition(tween, target, duration);
			}
		}
	}

	private void _MoveCharacterInstant(GemPiece gem)
	{
		if (!GodotObject.IsInstanceValid(gem.character))
		{
			return;
		}
		Vector2I vector2I = _BoardToMap(gem.gridPos);
		Vector2I gridPos = gem.character.gridPos;
		if (gridPos == vector2I)
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(vector2I);
		if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(mapCell2))
		{
			Vector2 logicalGlobalPosition = gem.character.GetLogicalGlobalPosition();
			Vector2 vector = Vector2.Zero;
			ShadowComponent shadowComponent = gem.character.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				vector = gem.character.shadowComponent.saveShadowPosition;
			}
			mapCell.RemoveCharacter(gem.character);
			mapCell2.CharacterPlant(gem.character.packet, gem.character, noLimit: true);
			gem.character.gridPos = vector2I;
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(vector2I);
			gem.character.SetLogicalGlobalPosition(mapCellPlantPos);
			ShadowComponent shadowComponent2 = gem.character.shadowComponent;
			if (shadowComponent2 != null && !shadowComponent2.IsReleased)
			{
				gem.character.shadowComponent.SetSaveShadowPosition(vector + mapCellPlantPos - logicalGlobalPosition);
			}
		}
	}

	public Godot.Collections.Array FindMatches()
	{
		Dictionary dictionary = new Dictionary();
		_matchLines.Clear();
		for (int i = 0; i < config.boardRows; i++)
		{
			int num = 0;
			while (num < config.boardCols)
			{
				GemPiece gemPiece = grid[i][num];
				if (gemPiece == null || gemPiece.isHole)
				{
					num++;
					continue;
				}
				StringName characterKey = gemPiece.characterKey;
				int j;
				for (j = 1; num + j < config.boardCols; j++)
				{
					GemPiece gemPiece2 = grid[i][num + j];
					if (gemPiece2 == null || gemPiece2.isHole || gemPiece2.characterKey != characterKey)
					{
						break;
					}
				}
				if (j >= 3)
				{
					Godot.Collections.Array array = new Godot.Collections.Array();
					for (int k = 0; k < j; k++)
					{
						dictionary[new Vector2I(num + k, i)] = true;
						array.Add(new Vector2I(num + k, i));
					}
					_matchLines.Add(array);
				}
				num += j;
			}
		}
		for (int l = 0; l < config.boardCols; l++)
		{
			int num2 = 0;
			while (num2 < config.boardRows)
			{
				GemPiece gemPiece3 = grid[num2][l];
				if (gemPiece3 == null || gemPiece3.isHole)
				{
					num2++;
					continue;
				}
				StringName characterKey2 = gemPiece3.characterKey;
				int m;
				for (m = 1; num2 + m < config.boardRows; m++)
				{
					GemPiece gemPiece4 = grid[num2 + m][l];
					if (gemPiece4 == null || gemPiece4.isHole || gemPiece4.characterKey != characterKey2)
					{
						break;
					}
				}
				if (m >= 3)
				{
					Godot.Collections.Array array2 = new Godot.Collections.Array();
					for (int n = 0; n < m; n++)
					{
						dictionary[new Vector2I(l, num2 + n)] = true;
						array2.Add(new Vector2I(l, num2 + n));
					}
					_matchLines.Add(array2);
				}
				num2 += m;
			}
		}
		Godot.Collections.Array array3 = new Godot.Collections.Array();
		foreach (Variant key in dictionary.Keys)
		{
			Vector2I vector2I = (Vector2I)key;
			GemPiece gemPiece5 = grid[vector2I.Y][vector2I.X];
			if (gemPiece5 != null)
			{
				array3.Add(gemPiece5);
			}
		}
		return array3;
	}

	private bool _IsCrossMatch(Vector2I pos)
	{
		bool flag = false;
		bool flag2 = false;
		int i;
		for (i = 0; pos.X - i - 1 >= 0; i++)
		{
			GemPiece gemPiece = grid[pos.Y][pos.X - i - 1];
			if (gemPiece == null || gemPiece.characterKey != _GetPlantKeyAt(pos))
			{
				break;
			}
		}
		int j;
		for (j = 0; pos.X + j + 1 < config.boardCols; j++)
		{
			GemPiece gemPiece2 = grid[pos.Y][pos.X + j + 1];
			if (gemPiece2 == null || gemPiece2.characterKey != _GetPlantKeyAt(pos))
			{
				break;
			}
		}
		if (i + j + 1 >= 3)
		{
			flag = true;
		}
		int k;
		for (k = 0; pos.Y - k - 1 >= 0; k++)
		{
			GemPiece gemPiece3 = grid[pos.Y - k - 1][pos.X];
			if (gemPiece3 == null || gemPiece3.characterKey != _GetPlantKeyAt(pos))
			{
				break;
			}
		}
		int l;
		for (l = 0; pos.Y + l + 1 < config.boardRows; l++)
		{
			GemPiece gemPiece4 = grid[pos.Y + l + 1][pos.X];
			if (gemPiece4 == null || gemPiece4.characterKey != _GetPlantKeyAt(pos))
			{
				break;
			}
		}
		if (k + l + 1 >= 3)
		{
			flag2 = true;
		}
		return flag && flag2;
	}

	private void _RemoveMatches(Godot.Collections.Array matches)
	{
		int num = _CalculateMatchUnitCount();
		currentMatchCount++;
		currentMatchValue += _CalculateMatchValue(num);
		_RefreshProgressMeter(visible: true);
		Vector2 zero = Vector2.Zero;
		foreach (Variant match in matches)
		{
			GemPiece gemPiece = (GemPiece)(GodotObject)match;
			zero += _GridToWorld(gemPiece.gridPos);
		}
		zero /= (float)matches.Count;
		for (int i = 0; i < num; i++)
		{
			Vector2 pos = zero + new Vector2((float)GD.RandRange(-15.0, 15.0), (float)GD.RandRange(-15.0, 0.0));
			TowerDefenseManager.Instance.SunCreate(_activeEconomyAccountId, pos, config.sunPerMatch, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2((float)GD.RandRange(-50.0, 50.0), -300f));
		}
		foreach (Variant match2 in matches)
		{
			GemPiece gemPiece2 = (GemPiece)(GodotObject)match2;
			if (GodotObject.IsInstanceValid(gemPiece2.character))
			{
				gemPiece2.character.OnDestroy -= _OnPlantDestroyed;
			}
			grid[gemPiece2.gridPos.Y][gemPiece2.gridPos.X] = null;
			gemPiece2.PlayRemoveAnimation();
		}
		MarkBoardChanged();
		TryCompleteMatchObjective();
	}

	private void TryCompleteMatchObjective()
	{
		if (_isGemMatchCompleting || control == null || GetClearMatchCountTarget() <= 0 || !HasEnoughMatchCountToClear())
		{
			return;
		}
		_finishObjectivePending = true;
		if (control.process != null && control.process.TryFinish())
		{
			TowerDefenseBattleFeatureWave towerDefenseBattleFeatureWave = GetFeature("Wave") as TowerDefenseBattleFeatureWave;
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave))
			{
				towerDefenseBattleFeatureWave.waveFinal = true;
				towerDefenseBattleFeatureWave.spawnOver = true;
			}
			_isGemMatchCompleting = true;
			_finishObjectivePending = false;
		}
	}

	private int _CalculateMatchUnitCount()
	{
		int num = 0;
		foreach (Variant matchLine in _matchLines)
		{
			int count = ((Godot.Collections.Array)matchLine).Count;
			num += Mathf.Max(count - 2, 1);
		}
		num += comboCount;
		Dictionary dictionary = new Dictionary();
		for (int i = 0; i < _matchLines.Count; i++)
		{
			for (int j = i + 1; j < _matchLines.Count; j++)
			{
				Godot.Collections.Array array = (Godot.Collections.Array)_matchLines[i];
				Godot.Collections.Array array2 = (Godot.Collections.Array)_matchLines[j];
				foreach (Variant item in array)
				{
					foreach (Variant item2 in array2)
					{
						if ((Vector2I)item == (Vector2I)item2)
						{
							dictionary[(Vector2I)item] = true;
						}
					}
				}
			}
		}
		if (dictionary.Count > 0)
		{
			num *= 2;
		}
		return num;
	}

	private int _CalculateMatchValue(int matchUnitCount)
	{
		return matchUnitCount * Mathf.Max(config.matchValuePerMatch, 0);
	}

	private void _AddUpgradePacket(StringName upgradeKey, int insertIndex = -1)
	{
		if (!(GetFeature("SeedBank") is TowerDefenseBattleFeatureSeedBank { seedBank: not null } towerDefenseBattleFeatureSeedBank) || towerDefenseBattleFeatureSeedBank.seedBank.HasPacket(upgradeKey) || !towerDefenseBattleFeatureSeedBank.seedBank.CanAddPacket())
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(upgradeKey);
		if (packetConfig == null)
		{
			return;
		}
		StringName upgradeSource = config.GetUpgradeSource(upgradeKey);
		int upgradeCost = config.GetUpgradeCost(upgradeSource);
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = towerDefenseBattleFeatureSeedBank.seedBank.AddPacket(packetConfig, isStart: true);
		if (towerDefenseInGamePacketShow == null)
		{
			return;
		}
		_ConfigureUpgradePacket(towerDefenseInGamePacketShow, upgradeKey, upgradeCost);
		if (insertIndex >= 0 && insertIndex < towerDefenseBattleFeatureSeedBank.seedBank.packetList.Count)
		{
			int num = towerDefenseBattleFeatureSeedBank.seedBank.packetList.IndexOf(towerDefenseInGamePacketShow);
			if (num >= 0 && num != insertIndex)
			{
				towerDefenseBattleFeatureSeedBank.seedBank.packetList.RemoveAt(num);
				towerDefenseBattleFeatureSeedBank.seedBank.packetList.Insert(insertIndex, towerDefenseInGamePacketShow);
				towerDefenseBattleFeatureSeedBank.seedBank.packetContainer.MoveChild(towerDefenseInGamePacketShow, insertIndex);
			}
		}
	}

	private void _OnUpgradePacketPressed(TowerDefenseInGamePacketShow _packet, StringName upgradeKey)
	{
		_ReleasePacketPick();
		_ResetFunctionPacketPress(_packet);
		if (IsRemoteMultiplayerClient())
		{
			MultiPlayerManager instance = MultiPlayerManager.Instance;
			if (instance != null)
			{
				string upgradeKey2 = upgradeKey;
				instance.SendGemMatchCommand("upgrade", default(Vector2I), default(Vector2I), upgradeKey2);
			}
		}
		else if (isProcessing || _state != 0)
		{
			if (!GodotObject.IsInstanceValid(_packet))
			{
				return;
			}
			for (int i = 0; i < _pendingUpgrades.Count; i++)
			{
				if (_pendingUpgrades[i].packet == _packet)
				{
					return;
				}
			}
			_pendingUpgrades.Add((_packet, upgradeKey, EconomyAccountId.Local));
		}
		else
		{
			ExecuteUpgrade(_packet, upgradeKey, EconomyAccountId.Local);
		}
	}

	private bool ExecuteUpgrade(TowerDefenseInGamePacketShow packet, StringName upgradeKey, EconomyAccountId accountId)
	{
		if (!GodotObject.IsInstanceValid(packet) || !accountId.IsValid || isProcessing || _state != 0)
		{
			return false;
		}
		StringName upgradeSource = config.GetUpgradeSource(upgradeKey);
		if ((string?)upgradeSource == "")
		{
			return false;
		}
		int upgradeCost = config.GetUpgradeCost(upgradeSource);
		if (!TryBeginGemCost(accountId, upgradeCost, out var spendReceipt))
		{
			return false;
		}
		_activeEconomyAccountId = accountId;
		_UpgradeAllPlants(upgradeSource, upgradeKey);
		spendReceipt?.TryCommit();
		for (int i = 0; i < config.plantList.Count; i++)
		{
			if (config.plantList[i] == upgradeSource)
			{
				config.plantList[i] = upgradeKey;
			}
		}
		TowerDefenseBattleFeatureSeedBank towerDefenseBattleFeatureSeedBank = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		int num = -1;
		if (towerDefenseBattleFeatureSeedBank != null && towerDefenseBattleFeatureSeedBank.seedBank != null)
		{
			TowerDefenseInGameSeedBank seedBank = towerDefenseBattleFeatureSeedBank.seedBank;
			num = seedBank.packetList.IndexOf(packet);
			if (num >= 0)
			{
				seedBank.packetNameSet.Remove(packet.config.saveKey);
				seedBank.packetList.RemoveAt(num);
				seedBank.packetNum--;
			}
			packet.QueueFree();
		}
		StringName upgradeTarget = config.GetUpgradeTarget(upgradeKey);
		if ((string?)upgradeTarget != "")
		{
			_AddUpgradePacket(upgradeTarget, num);
		}
		MarkBoardChanged();
		return true;
	}

	private static bool TryBeginGemCost(EconomyAccountId accountId, int cost, out SunSpendReceipt spendReceipt)
	{
		spendReceipt = null;
		if (cost <= 0)
		{
			return true;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			return instance.TryBeginSunSpend(accountId, cost, out spendReceipt);
		}
		return false;
	}

	public bool ExecuteNetworkCommand(Dictionary command, EconomyAccountId accountId)
	{
		if (!base.IsLifetimeActive || command == null || !accountId.IsValid || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost))
		{
			return false;
		}
		return command.GetValueOrDefault("action", "").AsString() switch
		{
			"swap" => TryStartSwap(new Vector2I(command.GetValueOrDefault("from_x", -1).AsInt32(), command.GetValueOrDefault("from_y", -1).AsInt32()), new Vector2I(command.GetValueOrDefault("to_x", -1).AsInt32(), command.GetValueOrDefault("to_y", -1).AsInt32()), accountId), 
			"fill_hole" => ExecuteFillHole(accountId), 
			"refresh" => ExecuteRefreshBoard(accountId), 
			"upgrade" => ExecuteUpgrade(FindUpgradePacket(command.GetValueOrDefault("upgrade_key", "").AsString()), new StringName(command.GetValueOrDefault("upgrade_key", "").AsString()), accountId), 
			_ => false, 
		};
	}

	private TowerDefenseInGamePacketShow FindUpgradePacket(string upgradeKey)
	{
		if (upgradeKey == "")
		{
			return null;
		}
		TowerDefenseInGameSeedBank towerDefenseInGameSeedBank = (GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank)?.seedBank;
		if (towerDefenseInGameSeedBank == null)
		{
			return null;
		}
		foreach (TowerDefenseInGamePacketShow packet in towerDefenseInGameSeedBank.packetList)
		{
			if (GodotObject.IsInstanceValid(packet) && packet.HasMeta("gem_match_upgrade_key") && packet.GetMeta("gem_match_upgrade_key").AsString() == upgradeKey)
			{
				return packet;
			}
		}
		return null;
	}

	private void _ProcessPendingUpgrades()
	{
		if (_pendingUpgrades.Count == 0)
		{
			return;
		}
		List<(TowerDefenseInGamePacketShow, StringName, EconomyAccountId)> list = new List<(TowerDefenseInGamePacketShow, StringName, EconomyAccountId)>(_pendingUpgrades);
		_pendingUpgrades.Clear();
		foreach (var item in list)
		{
			if (GodotObject.IsInstanceValid(item.Item1))
			{
				ExecuteUpgrade(item.Item1, item.Item2, item.Item3);
			}
		}
	}

	private void _UpgradeAllPlants(StringName fromKey, StringName toKey)
	{
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece != null && gemPiece.characterKey == fromKey)
				{
					_ReplaceGemCharacter(gemPiece, toKey);
				}
			}
		}
	}

	private void _ReplaceGemCharacter(GemPiece gem, StringName newKey)
	{
		if (gem == null || gem.isHole)
		{
			return;
		}
		TowerDefenseCharacter character = gem.character;
		Vector2I gridPos = _BoardToMap(gem.gridPos);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(newKey);
		if (packetConfig == null)
		{
			return;
		}
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(gridPos);
		double num = (GodotObject.IsInstanceValid(mapCell) ? mapCell.GetGroundHeight() : 0.0);
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Create(_activeEconomyAccountId, mapCellPlantPos, gridPos, num);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		towerDefenseCharacter.cost = packetConfig.characterConfig.cost;
		towerDefenseCharacter.groundHeight = num;
		towerDefenseCharacter.z = num;
		if (packetConfig.packetFlip)
		{
			towerDefenseCharacter.Scale = new Vector2(0f - towerDefenseCharacter.Scale.X, towerDefenseCharacter.Scale.Y);
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			towerDefenseCharacter.QueueFree();
			return;
		}
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
		gem.characterKey = newKey;
		gem.character = towerDefenseCharacter;
		if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(character))
		{
			character.OnDestroy -= _OnPlantDestroyed;
			mapCell.CharacterReplace(character, towerDefenseCharacter);
		}
		else if (GodotObject.IsInstanceValid(character))
		{
			character.OnDestroy -= _OnPlantDestroyed;
			character.Destroy();
		}
		_ConnectGemCharacterDestroySignal(gem);
	}

	private bool _HasActiveFallTweens()
	{
		_PruneActiveFallTweens();
		return _activeTweens > 0;
	}

	private void _PruneActiveFallTweens()
	{
		_activeFallTweens.RemoveAll((Tween tween) => !GodotObject.IsInstanceValid(tween) || !tween.IsRunning());
		_activeTweens = _activeFallTweens.Count;
	}

	private void _OnTweenCompleted(Tween tween)
	{
		_activeFallTweens.Remove(tween);
		_activeTweens = _activeFallTweens.Count;
	}

	private void _CreateFallTween(TowerDefenseCharacter chara, Vector2 targetPos, ShadowComponent shadowComp, Vector2 shadowTarget, double duration, double delay = 0.0)
	{
		Tween tween = chara.CreateTween();
		_activeFallTweens.Add(tween);
		_activeTweens = _activeFallTweens.Count;
		tween.SetEase(Tween.EaseType.In);
		tween.SetTrans(Tween.TransitionType.Quad);
		if (delay > 0.0)
		{
			tween.TweenInterval(delay);
			tween.Chain();
		}
		tween.SetParallel();
		Vector2 logicalGlobalPosition = chara.GetLogicalGlobalPosition();
		tween.TweenMethod(Callable.From<Vector2>(chara.SetLogicalGlobalPosition), logicalGlobalPosition, targetPos, duration);
		shadowComp?.TweenSaveShadowPosition(tween, shadowTarget, duration);
		tween.Chain().TweenCallback(Callable.From(delegate
		{
			_OnTweenCompleted(tween);
		}));
	}

	private void _DeferredShadowFallTween(TowerDefenseCharacter character, Vector2 startPos, Vector2 targetPos, double duration)
	{
		Callable.From(delegate
		{
			if (base.IsLifetimeActive && GodotObject.IsInstanceValid(character))
			{
				ShadowComponent shadowComponent = character.shadowComponent;
				if (shadowComponent != null && !shadowComponent.IsReleased)
				{
					Vector2 target = shadowComponent.saveShadowPosition + targetPos - startPos;
					Tween tween = character.CreateTween();
					_activeFallTweens.Add(tween);
					_activeTweens = _activeFallTweens.Count;
					tween.SetEase(Tween.EaseType.In);
					tween.SetTrans(Tween.TransitionType.Quad);
					shadowComponent.TweenSaveShadowPosition(tween, target, duration);
					tween.Chain().TweenCallback(Callable.From(delegate
					{
						_OnTweenCompleted(tween);
					}));
				}
			}
		}).CallDeferred();
	}

	private void _ApplyGravityAndFill()
	{
		for (int i = 0; i < config.boardCols; i++)
		{
			int bottomY = config.boardRows - 1;
			for (int num = config.boardRows - 1; num >= -1; num--)
			{
				bool num2 = num < 0;
				bool flag = !num2 && grid[num][i] != null && grid[num][i].isHole;
				if (num2 || flag)
				{
					_ApplyGravityAndFillSegment(i, num + 1, bottomY);
					bottomY = num - 1;
				}
			}
		}
		MarkBoardChanged();
	}

	private void _ApplyGravityAndFillSegment(int x, int topY, int bottomY)
	{
		if (topY > bottomY)
		{
			return;
		}
		int num = bottomY;
		for (int num2 = bottomY; num2 >= topY; num2--)
		{
			GemPiece gemPiece = grid[num2][x];
			if (gemPiece != null)
			{
				if (num2 != num)
				{
					grid[num][x] = gemPiece;
					grid[num2][x] = null;
					gemPiece.SetGridPos(new Vector2I(x, num));
					_StartFallToNewCell(gemPiece);
				}
				num--;
			}
		}
		int num3 = num - topY + 1;
		int num4 = 0;
		for (int num5 = num; num5 >= topY; num5--)
		{
			StringName characterKey = config.plantList[(int)(GD.Randi() % (uint)config.plantList.Count)];
			GemPiece gemPiece2 = _CreateGemAt(new Vector2I(x, num5), characterKey);
			if (GodotObject.IsInstanceValid(gemPiece2.character))
			{
				Vector2 vector = _GridToWorld(new Vector2I(x, num5));
				Vector2 vector2 = vector + new Vector2(0f, (float)(-(num3 - num4 + 1)) * config.fallSpawnHeight);
				gemPiece2.character.SetLogicalGlobalPosition(vector2);
				float num6 = Math.Abs(vector.Y - vector2.Y);
				double fallDuration = GetFallDuration(num6);
				ShadowComponent shadowComponent = gemPiece2.character.shadowComponent;
				if (shadowComponent != null && !shadowComponent.IsReleased)
				{
					ShadowComponent shadowComponent2 = gemPiece2.character.shadowComponent;
					Vector2 saveShadowPosition = shadowComponent2.saveShadowPosition;
					shadowComponent2.SetSaveShadowPosition(saveShadowPosition + vector2 - vector);
					_CreateFallTween(gemPiece2.character, vector, shadowComponent2, saveShadowPosition, fallDuration);
				}
				else
				{
					_CreateFallTween(gemPiece2.character, vector, null, Vector2.Zero, fallDuration);
					_DeferredShadowFallTween(gemPiece2.character, vector2, vector, fallDuration);
				}
			}
			num4++;
		}
	}

	private void _StartFallToNewCell(GemPiece gem)
	{
		if (!GodotObject.IsInstanceValid(gem.character))
		{
			return;
		}
		Vector2I vector2I = _BoardToMap(gem.gridPos);
		Vector2I gridPos = gem.character.gridPos;
		if (gridPos == vector2I)
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(vector2I);
		if (GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(mapCell2))
		{
			Vector2 logicalGlobalPosition = gem.character.GetLogicalGlobalPosition();
			Vector2 vector = Vector2.Zero;
			ShadowComponent shadowComponent = null;
			ShadowComponent shadowComponent2 = gem.character.shadowComponent;
			if (shadowComponent2 != null && !shadowComponent2.IsReleased)
			{
				vector = gem.character.shadowComponent.saveShadowPosition;
				shadowComponent = gem.character.shadowComponent;
			}
			mapCell.RemoveCharacter(gem.character);
			mapCell2.CharacterPlant(gem.character.packet, gem.character, noLimit: true);
			gem.character.gridPos = vector2I;
			gem.character.SetLogicalGlobalPosition(logicalGlobalPosition);
			shadowComponent?.SetSaveShadowPosition(vector);
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(vector2I);
			Vector2 shadowTarget = vector + mapCellPlantPos - logicalGlobalPosition;
			float num = Math.Abs(mapCellPlantPos.Y - logicalGlobalPosition.Y);
			double fallDuration = GetFallDuration(num);
			_CreateFallTween(gem.character, mapCellPlantPos, shadowComponent, shadowTarget, fallDuration);
		}
	}

	private bool _CheckPossibleMoves()
	{
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece == null || gemPiece.isHole)
				{
					continue;
				}
				if (j + 1 < config.boardCols)
				{
					GemPiece gemPiece2 = grid[i][j + 1];
					if (gemPiece2 != null && !gemPiece2.isHole && gemPiece2.characterKey != gemPiece.characterKey)
					{
						_QuickSwapData(new Vector2I(j, i), new Vector2I(j + 1, i));
						if (_HasAnyMatch())
						{
							_QuickSwapData(new Vector2I(j, i), new Vector2I(j + 1, i));
							return true;
						}
						_QuickSwapData(new Vector2I(j, i), new Vector2I(j + 1, i));
					}
				}
				if (i + 1 >= config.boardRows)
				{
					continue;
				}
				GemPiece gemPiece3 = grid[i + 1][j];
				if (gemPiece3 != null && !gemPiece3.isHole && gemPiece3.characterKey != gemPiece.characterKey)
				{
					_QuickSwapData(new Vector2I(j, i), new Vector2I(j, i + 1));
					if (_HasAnyMatch())
					{
						_QuickSwapData(new Vector2I(j, i), new Vector2I(j, i + 1));
						return true;
					}
					_QuickSwapData(new Vector2I(j, i), new Vector2I(j, i + 1));
				}
			}
		}
		return false;
	}

	private void _QuickSwapData(Vector2I a, Vector2I b)
	{
		GemPiece value = grid[a.Y][a.X];
		grid[a.Y][a.X] = grid[b.Y][b.X];
		grid[b.Y][b.X] = value;
	}

	private bool _HasAnyMatch()
	{
		for (int i = 0; i < config.boardRows; i++)
		{
			int num = 0;
			while (num < config.boardCols - 2)
			{
				GemPiece gemPiece = grid[i][num];
				if (gemPiece == null || gemPiece.isHole)
				{
					num++;
					continue;
				}
				StringName characterKey = gemPiece.characterKey;
				int j;
				for (j = 1; num + j < config.boardCols; j++)
				{
					GemPiece gemPiece2 = grid[i][num + j];
					if (gemPiece2 == null || gemPiece2.isHole || gemPiece2.characterKey != characterKey)
					{
						break;
					}
				}
				if (j >= 3)
				{
					return true;
				}
				num += j;
			}
		}
		for (int k = 0; k < config.boardCols; k++)
		{
			int num2 = 0;
			while (num2 < config.boardRows - 2)
			{
				GemPiece gemPiece3 = grid[num2][k];
				if (gemPiece3 == null || gemPiece3.isHole)
				{
					num2++;
					continue;
				}
				StringName characterKey2 = gemPiece3.characterKey;
				int l;
				for (l = 1; num2 + l < config.boardRows; l++)
				{
					GemPiece gemPiece4 = grid[num2 + l][k];
					if (gemPiece4 == null || gemPiece4.isHole || gemPiece4.characterKey != characterKey2)
					{
						break;
					}
				}
				if (l >= 3)
				{
					return true;
				}
				num2 += l;
			}
		}
		return false;
	}

	public void ShuffleBoard(int depth = 0)
	{
		if (depth > 5)
		{
			return;
		}
		Array<GemPiece> array = new Array<GemPiece>();
		Array<Vector2I> array2 = new Array<Vector2I>();
		System.Collections.Generic.Dictionary<Vector2I, GemPiece> dictionary = new System.Collections.Generic.Dictionary<Vector2I, GemPiece>();
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece != null && gemPiece.isHole)
				{
					Vector2I vector2I = new Vector2I(j, i);
					array2.Add(vector2I);
					dictionary[vector2I] = gemPiece;
					continue;
				}
				if (gemPiece != null)
				{
					array.Add(gemPiece);
				}
				grid[i][j] = null;
			}
		}
		array.Shuffle();
		int num = 0;
		for (int k = 0; k < config.boardRows; k++)
		{
			for (int l = 0; l < config.boardCols; l++)
			{
				if (array2.Contains(new Vector2I(l, k)))
				{
					Vector2I key = new Vector2I(l, k);
					if (dictionary.TryGetValue(key, out var value))
					{
						grid[k][l] = value;
					}
				}
				else if (num < array.Count)
				{
					GemPiece gemPiece2 = array[num];
					gemPiece2.SetGridPos(new Vector2I(l, k));
					gemPiece2.GlobalPosition = _GridToWorld(new Vector2I(l, k));
					_MoveCharacterInstant(gemPiece2);
					grid[k][l] = gemPiece2;
					num++;
				}
			}
		}
		if (FindMatches().Count == 0 && !_CheckPossibleMoves())
		{
			ShuffleBoard(depth + 1);
		}
		if (depth == 0)
		{
			MarkBoardChanged();
		}
	}

	private void _ShuffleBoardWithFallEffect()
	{
		Array<GemPiece> array = new Array<GemPiece>();
		Array<Vector2I> array2 = new Array<Vector2I>();
		System.Collections.Generic.Dictionary<Vector2I, GemPiece> dictionary = new System.Collections.Generic.Dictionary<Vector2I, GemPiece>();
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece != null && gemPiece.isHole)
				{
					Vector2I vector2I = new Vector2I(j, i);
					array2.Add(vector2I);
					dictionary[vector2I] = gemPiece;
					continue;
				}
				if (gemPiece != null)
				{
					array.Add(gemPiece);
				}
				grid[i][j] = null;
			}
		}
		array.Shuffle();
		int num = 0;
		for (int k = 0; k < config.boardRows; k++)
		{
			for (int l = 0; l < config.boardCols; l++)
			{
				Vector2I vector2I2 = new Vector2I(l, k);
				if (array2.Contains(vector2I2))
				{
					if (dictionary.TryGetValue(vector2I2, out var value))
					{
						grid[k][l] = value;
					}
				}
				else if (num < array.Count)
				{
					GemPiece gemPiece2 = array[num];
					gemPiece2.SetGridPos(vector2I2);
					gemPiece2.GlobalPosition = _GridToWorld(vector2I2);
					grid[k][l] = gemPiece2;
					_MoveRefreshGemWithFallEffect(gemPiece2);
					num++;
				}
			}
		}
		MarkBoardChanged();
	}

	private void _MoveRefreshGemWithFallEffect(GemPiece gem)
	{
		if (!GodotObject.IsInstanceValid(gem.character))
		{
			return;
		}
		Vector2I vector2I = _BoardToMap(gem.gridPos);
		Vector2I gridPos = gem.character.gridPos;
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(vector2I);
		if (!GodotObject.IsInstanceValid(mapCell2))
		{
			return;
		}
		Vector2 logicalGlobalPosition = gem.character.GetLogicalGlobalPosition();
		Vector2 vector = Vector2.Zero;
		ShadowComponent shadowComponent = null;
		ShadowComponent shadowComponent2 = gem.character.shadowComponent;
		if (shadowComponent2 != null && !shadowComponent2.IsReleased)
		{
			vector = gem.character.shadowComponent.saveShadowPosition;
			shadowComponent = gem.character.shadowComponent;
		}
		if (gridPos != vector2I)
		{
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				return;
			}
			mapCell.RemoveCharacter(gem.character);
			mapCell2.CharacterPlant(gem.character.packet, gem.character, noLimit: true);
			gem.character.gridPos = vector2I;
		}
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(vector2I);
		Vector2 vector2 = vector + mapCellPlantPos - logicalGlobalPosition;
		float num = (float)(config.boardRows - gem.gridPos.Y + 1) * config.fallSpawnHeight;
		Vector2 vector3 = mapCellPlantPos + new Vector2(0f, 0f - num);
		gem.character.SetLogicalGlobalPosition(vector3);
		shadowComponent?.SetSaveShadowPosition(vector2 + vector3 - mapCellPlantPos);
		double delay = (double)gem.gridPos.Y * config.fallRowDelay;
		double fallDuration = GetFallDuration(num);
		_CreateFallTween(gem.character, mapCellPlantPos, shadowComponent, vector2, fallDuration, delay);
	}

	public void OnPlantEaten(Vector2I pos)
	{
		if (_IsValidPos(pos))
		{
			GemPiece gemPiece = grid[pos.Y][pos.X];
			if (gemPiece != null)
			{
				_SetGemAsHole(gemPiece, destroyExistingCharacter: true);
			}
		}
	}

	private void _OnPlantDestroyed(TowerDefenseCharacter character)
	{
		if (!base.IsLifetimeActive || config == null)
		{
			return;
		}
		for (int i = 0; i < config.boardRows; i++)
		{
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (gemPiece != null && gemPiece.character == character)
				{
					_SetGemAsHole(gemPiece);
					_needBoardCheck = true;
					return;
				}
			}
		}
	}

	private void MarkBoardChanged()
	{
		if (!IsRemoteMultiplayerClient())
		{
			_boardRevision = ((_boardRevision == 2147483647) ? 1 : (_boardRevision + 1));
		}
	}

	private double GetFallDuration(double distance)
	{
		return Math.Max(config.minimumFallDuration, Math.Max(0.0, distance) / config.fallSpeed);
	}

	private bool _IsValidPos(Vector2I pos)
	{
		if (pos.X >= 0 && pos.X < config.boardCols && pos.Y >= 0)
		{
			return pos.Y < config.boardRows;
		}
		return false;
	}

	private Vector2 _GridToWorld(Vector2I pos)
	{
		if (pos.Y >= 0 && pos.Y < _cellPositions.Count)
		{
			Godot.Collections.Array array = (Godot.Collections.Array)_cellPositions[pos.Y];
			if (pos.X >= 0 && pos.X < array.Count)
			{
				return (Vector2)array[pos.X];
			}
		}
		return TowerDefenseManager.GetMapCellPlantPos(_BoardToMap(pos));
	}

	private void _CacheCellPositions()
	{
		_cellPositions.Clear();
		for (int i = 0; i < config.boardRows; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			for (int j = 0; j < config.boardCols; j++)
			{
				array.Add(TowerDefenseManager.GetMapCellPlantPos(_BoardToMap(new Vector2I(j, i))));
			}
			_cellPositions.Add(array);
		}
		_cellSize = Vector2.Zero;
		if (config.boardCols >= 2)
		{
			Godot.Collections.Array array2 = (Godot.Collections.Array)_cellPositions[0];
			Vector2 cellSize = _cellSize;
			cellSize.X = Math.Abs(((Vector2)array2[1]).X - ((Vector2)array2[0]).X);
			_cellSize = cellSize;
		}
		if (config.boardRows >= 2)
		{
			Godot.Collections.Array array3 = (Godot.Collections.Array)_cellPositions[0];
			Godot.Collections.Array array4 = (Godot.Collections.Array)_cellPositions[1];
			Vector2 cellSize = _cellSize;
			cellSize.Y = Math.Abs(((Vector2)array4[0]).Y - ((Vector2)array3[0]).Y);
			_cellSize = cellSize;
		}
		Vector2 mapGridSize = TowerDefenseManager.Instance.GetMapGridSize();
		if (_cellSize.X < 1f)
		{
			Vector2 cellSize = _cellSize;
			cellSize.X = Math.Abs(mapGridSize.X);
			_cellSize = cellSize;
		}
		if (_cellSize.Y < 1f)
		{
			Vector2 cellSize = _cellSize;
			cellSize.Y = Math.Abs(mapGridSize.Y);
			_cellSize = cellSize;
		}
	}

	private Vector2I _ScreenToGrid(Vector2 screenPos)
	{
		if (!GodotObject.IsInstanceValid(gemNode) || _cellPositions.Count == 0 || (double)_cellSize.X < 1.0)
		{
			return new Vector2I(-1, -1);
		}
		Vector2 vector = gemNode.GetCanvasTransform().AffineInverse() * screenPos;
		Vector2 vector2 = (Vector2)((Godot.Collections.Array)_cellPositions[0])[0];
		int num = (int)Math.Round((vector.X - vector2.X) / _cellSize.X);
		int num2 = (int)Math.Round((vector.Y - vector2.Y) / _cellSize.Y);
		if (num < 0 || num >= config.boardCols || num2 < 0 || num2 >= config.boardRows)
		{
			return new Vector2I(-1, -1);
		}
		Vector2 to = (Vector2)((Godot.Collections.Array)_cellPositions[num2])[num];
		if (vector.DistanceTo(to) > config.inputRadius)
		{
			return new Vector2I(-1, -1);
		}
		return new Vector2I(num, num2);
	}

	private Vector2I _BoardToMap(Vector2I boardPos)
	{
		return boardPos + Vector2I.One;
	}

	public override Dictionary SaveFeature()
	{
		return SerializeBoardState();
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeBoardState();
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		if (IsRemoteMultiplayerClient())
		{
			int num = _data.GetValueOrDefault("boardRevision", -1).AsInt32();
			currentMatchCount = Math.Max(0, _data.GetValueOrDefault("currentMatchCount", currentMatchCount).AsInt32());
			currentMatchValue = Math.Max(0, _data.GetValueOrDefault("currentMatchValue", currentMatchValue).AsInt32());
			if (num == _lastAppliedBoardRevision)
			{
				_RefreshProgressMeter(visible: true);
				return;
			}
			ApplyBoardState(_data, linkCharacters: true);
			_lastAppliedBoardRevision = num;
		}
	}

	private Dictionary SerializeBoardState()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		for (int i = 0; i < config.boardRows; i++)
		{
			Godot.Collections.Array array2 = new Godot.Collections.Array();
			for (int j = 0; j < config.boardCols; j++)
			{
				GemPiece gemPiece = ((i < grid.Count && j < grid[i].Count) ? grid[i][j] : null);
				if (gemPiece != null && gemPiece.isHole)
				{
					Dictionary dictionary = new Dictionary();
					dictionary["isHole"] = true;
					array2.Add(dictionary);
				}
				else if (gemPiece != null)
				{
					Dictionary dictionary2 = new Dictionary();
					dictionary2["characterKey"] = (string?)gemPiece.characterKey;
					array2.Add(dictionary2);
				}
				else
				{
					array2.Add(default(Variant));
				}
			}
			array.Add(array2);
		}
		Godot.Collections.Array array3 = new Godot.Collections.Array();
		foreach (StringName plant in config.plantList)
		{
			array3.Add((string?)plant);
		}
		return new Dictionary
		{
			["grid"] = array,
			["plantList"] = array3,
			["holeCount"] = _holeCount,
			["currentMatchCount"] = currentMatchCount,
			["currentMatchValue"] = currentMatchValue,
			["boardRevision"] = _boardRevision
		};
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		ApplyBoardState(_data, linkCharacters: false);
	}

	private void ApplyBoardState(Dictionary state, bool linkCharacters)
	{
		if (config == null)
		{
			config = new TowerDefenseBattleFeatureGemMatchConfig();
			config.Init(data);
		}
		Godot.Collections.Array array = (state.ContainsKey("plantList") ? ((Godot.Collections.Array)state["plantList"]) : new Godot.Collections.Array());
		if (array.Count > 0)
		{
			config.plantList.Clear();
			foreach (Variant item in array)
			{
				config.plantList.Add(new StringName((string)item));
			}
		}
		currentMatchCount = Math.Max(0, state.GetValueOrDefault("currentMatchCount", 0).AsInt32());
		currentMatchValue = Math.Max(0, state.GetValueOrDefault("currentMatchValue", 0).AsInt32());
		_boardRevision = Math.Max(0, state.GetValueOrDefault("boardRevision", _boardRevision).AsInt32());
		Godot.Collections.Array array2 = (state.ContainsKey("grid") ? ((Godot.Collections.Array)state["grid"]) : new Godot.Collections.Array());
		if (array2.Count == 0)
		{
			return;
		}
		if (gemNode == null)
		{
			gemNode = new Node2D();
			control.AddNode(gemNode, 2);
		}
		_CacheCellPositions();
		ClearGemPieces();
		grid.Clear();
		for (int i = 0; i < config.boardRows; i++)
		{
			Array<GemPiece> array3 = new Array<GemPiece>();
			for (int j = 0; j < config.boardCols; j++)
			{
				array3.Add(null);
			}
			grid.Add(array3);
		}
		_holeCount = 0;
		for (int k = 0; k < Math.Min(array2.Count, config.boardRows); k++)
		{
			Godot.Collections.Array array4 = (Godot.Collections.Array)array2[k];
			for (int l = 0; l < Math.Min(array4.Count, config.boardCols); l++)
			{
				Variant variant = array4[l];
				if (variant.VariantType == Variant.Type.Nil)
				{
					continue;
				}
				Dictionary dictionary = (Dictionary)variant;
				if (dictionary.Count > 0)
				{
					if (dictionary.ContainsKey("isHole") && (bool)dictionary["isHole"])
					{
						GemPiece gemPiece = new GemPiece();
						gemPiece.Setup(new Vector2I(l, k), "");
						gemNode.AddChild(gemPiece, forceReadableName: false, Node.InternalMode.Disabled);
						gemPiece.GlobalPosition = _GridToWorld(new Vector2I(l, k));
						gemPiece.SetAsHole();
						grid[k][l] = gemPiece;
						_holeCount++;
					}
					else
					{
						StringName characterKey = (dictionary.ContainsKey("characterKey") ? new StringName((string)dictionary["characterKey"]) : new StringName(""));
						GemPiece gemPiece2 = new GemPiece();
						gemPiece2.Setup(new Vector2I(l, k), characterKey);
						gemNode.AddChild(gemPiece2, forceReadableName: false, Node.InternalMode.Disabled);
						gemPiece2.GlobalPosition = _GridToWorld(new Vector2I(l, k));
						grid[k][l] = gemPiece2;
					}
				}
			}
		}
		if (linkCharacters)
		{
			_clientRelinkTimer = 0.0;
			_LinkExistingPlants();
			_UpdateFillHolePacket();
			_RefreshProgressMeter(visible: true);
		}
	}

	private void ClearGemPieces()
	{
		for (int i = 0; i < grid.Count; i++)
		{
			for (int j = 0; j < grid[i].Count; j++)
			{
				GemPiece gemPiece = grid[i][j];
				if (GodotObject.IsInstanceValid(gemPiece))
				{
					gemPiece.QueueFree();
				}
			}
		}
	}

	public override bool CanLoadProgress()
	{
		return true;
	}
}
