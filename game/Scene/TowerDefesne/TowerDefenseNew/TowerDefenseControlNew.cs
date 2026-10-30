using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;

[GlobalClass]
public partial class TowerDefenseControlNew : TowerDefenseControl
{
	public delegate void ViewBackEventHandler();

	private sealed class FeatureAddTransaction
	{
		public readonly List<StringName> AddedFeatureNames = new List<StringName>();
	}

	public StateChart state;

	public TowerDefenseInGameLevelControl levelControl;

	public CanvasModulate characterCanvasModulate;

	public Node2D characterNode;

	public CanvasLayer bankUILayer;

	public HBoxContainer uITopBankContainer;

	public HBoxContainer uITopPropContainer;

	public Control mobileInterval;

	public AnimationPlayer uiTopAnimationPlayer;

	private TowerDefenseCoexistHud _coexistHud;

	public System.Collections.Generic.Dictionary<int, CanvasLayer> uilayerDictionary = new System.Collections.Generic.Dictionary<int, CanvasLayer>();

	public System.Collections.Generic.Dictionary<int, CanvasLayer> layerDictionary = new System.Collections.Generic.Dictionary<int, CanvasLayer>();

	public System.Collections.Generic.Dictionary<StringName, TowerDefenseBattleFeature> featureDictionary = new System.Collections.Generic.Dictionary<StringName, TowerDefenseBattleFeature>();

	private readonly List<StringName> _featureInitializationOrder = new List<StringName>();

	private readonly HashSet<StringName> _featuresBeingAdded = new HashSet<StringName>();

	public TowerDefenseBattleProcess process;

	public List<TowerDefensePacketChangeCost> changeCostList = new List<TowerDefensePacketChangeCost>();

	public TowerDefenseZombieWon zombieWon;

	public AabbArea2D zombieCheckArea;

	private readonly HashSet<TowerDefenseZombie> _zombieCheckOverlaps = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _zombieCheckScratch = new HashSet<TowerDefenseZombie>();

	private readonly List<TowerDefenseCharacter> _zombieCheckCandidates = new List<TowerDefenseCharacter>();

	public bool isGameRunning;

	public bool isGameFail;

	public bool isInit = true;

	public TowerDefenseCharacter failCharacter;

	public bool waitPause;

	public bool isView;

	private bool _battleGraphReady;

	private bool _battleShuttingDown;

	private bool _preserveLevelConfigOnExit;

	private bool _gameRunningEntryReady;

	private Dictionary _pendingDestroySyncIds = new Dictionary();

	private TowerDefenseBattleNetworkHost _battleNetworkHost;

	private System.Collections.Generic.Dictionary<int, int> _zombieSyncMissCount = new System.Collections.Generic.Dictionary<int, int>();

	private int _syncIdCounter;

	internal System.Collections.Generic.Dictionary<int, TowerDefenseCharacter> _syncCharacters = new System.Collections.Generic.Dictionary<int, TowerDefenseCharacter>();

	private int _packetSyncIdCounter;

	private System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow> _syncPackets = new System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow>();

	private bool _isNetworkPaused;

	private System.Collections.Generic.Dictionary<int, Vector2> _zombieTargetPositions = new System.Collections.Generic.Dictionary<int, Vector2>();

	private Dictionary _zombieLastSyncState = new Dictionary();

	private System.Collections.Generic.Dictionary<int, Vector2> _zombieSyncVelocities = new System.Collections.Generic.Dictionary<int, Vector2>();

	private System.Collections.Generic.Dictionary<int, double> _zombieLastSyncTime = new System.Collections.Generic.Dictionary<int, double>();

	private System.Collections.Generic.Dictionary<string, RemoteCursor> _remoteCursors = new System.Collections.Generic.Dictionary<string, RemoteCursor>();

	private HashSet<string> _chooseReadyPeers = new HashSet<string>();

	private bool _chooseOverReceived;

	private RichTextLabel _chooseWaitLabel;

	private PlayerStatusPanel _playerStatusPanel;

	private readonly TowerDefenseBattleOperationTracker _pendingBattleOperations = new TowerDefenseBattleOperationTracker();

	private readonly Color[] PlayerColors = new Color[4]
	{
		new Color(0.3f, 0.7f, 1f),
		new Color(1f, 0.5f, 0.3f),
		new Color(0.5f, 1f, 0.5f),
		new Color(1f, 1f, 0.3f)
	};

	private Dictionary _gameStateLastSync = new Dictionary();

	private Dictionary _characterLastSyncState = new Dictionary();

	private Dictionary _componentLastSyncState = new Dictionary();

	private bool _levelConfigurationRejected;

	private TowerDefenseLevelSaveConfigCSharp _restoringProgress;

	private readonly HashSet<TowerDefenseBattleComponentBase> _progressInitialized = new HashSet<TowerDefenseBattleComponentBase>();

	private DialogBoxBase _progressPause;

	private bool _progressNoticeShown;

	internal IDictionary<int, TowerDefenseInGamePacketShow> SyncPackets => _syncPackets;

	internal bool IsNetworkPaused => _isNetworkPaused;

	public bool HasPendingBattleOperations => _pendingBattleOperations.HasPending;

	internal Dictionary GameStateLastSync => _gameStateLastSync;

	internal Dictionary CharacterLastSyncState => _characterLastSyncState;

	internal Dictionary ComponentLastSyncState => _componentLastSyncState;

	internal Dictionary PendingDestroySyncIds => _pendingDestroySyncIds;

	internal Dictionary ZombieLastSyncState => _zombieLastSyncState;

	internal System.Collections.Generic.Dictionary<int, int> ZombieSyncMissCount => _zombieSyncMissCount;

	internal System.Collections.Generic.Dictionary<int, Vector2> ZombieTargetPositions => _zombieTargetPositions;

	internal System.Collections.Generic.Dictionary<int, Vector2> ZombieSyncVelocities => _zombieSyncVelocities;

	internal System.Collections.Generic.Dictionary<int, double> ZombieLastSyncTime => _zombieLastSyncTime;

	public XWModLevelIdentity ModLevelIdentity { get; private set; }

	public event ViewBackEventHandler OnViewBack;

	public void EmitViewBack()
	{
		this.OnViewBack?.Invoke();
	}

	public int BeginPendingBattleOperation()
	{
		return _pendingBattleOperations.Begin();
	}

	public bool IsPendingBattleOperationCurrent(int operationId)
	{
		return _pendingBattleOperations.IsCurrent(operationId);
	}

	public void CompletePendingBattleOperation(int operationId)
	{
		_pendingBattleOperations.Complete(operationId);
	}

	public override void _Ready()
	{
		state = GetNode<StateChart>("%State");
		levelControl = GetNode<TowerDefenseInGameLevelControl>("%TowerDefenseInGameLevelControl");
		characterCanvasModulate = GetNode<CanvasModulate>("%CharacterCanvasModulate");
		characterNode = GetNode<Node2D>("%CharacterNode");
		bankUILayer = GetNode<CanvasLayer>("%BankUILayer");
		uITopBankContainer = GetNode<HBoxContainer>("%UITopBankContainer");
		uITopPropContainer = GetNode<HBoxContainer>("%UITopPropContainer");
		mobileInterval = GetNode<Control>("%MobileInterval");
		uiTopAnimationPlayer = GetNode<AnimationPlayer>("%UITopAnimationPlayer");
		zombieWon = GetNode<TowerDefenseZombieWon>("%TowerDefenseZombieWon");
		zombieCheckArea = GetNode<AabbArea2D>("%ZombieCheckArea");
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ResourceManager.Instance.RequireFullGameplayResourcesReady("TowerDefenseControlNew");
			TowerDefenseBattleRegistry.Init();
			TowerDefenseManager.Instance.pausePacket = false;
			TowerDefenseManager.Instance.pauseZombie = false;
			TowerDefenseManager.Instance.backPacket = false;
			TowerDefenseManager.Instance.backZombie = false;
			TowerDefenseManager.ClearDeathRecords();
			DropItemRegistry.Reset();
			_coexistHud = new TowerDefenseCoexistHud
			{
				Battle = this
			};
			uITopBankContainer.AddChild(_coexistHud, forceReadableName: false, InternalMode.Disabled);
			BattleEventBus.Instance.OnUiSwitched += UISwitched;
			BattleEventBus.Instance.OnPacketUIFront += PacketUIFront;
			SceneManager.Instance.OnSceneChange += OnSceneChange;
			PhonkComponent.phonkEnabled = GameSaveManager.Instance.GetConfigValue("PhonkEnabled").AsBool();
			PhonkComponent.phonkIntensity = (float)GameSaveManager.Instance.GetConfigValue("PhonkIntensity").AsDouble();
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentLevelConfig))
			{
				Init(TowerDefenseManager.Instance.currentLevelConfig);
			}
			else
			{
				GD.PushError("[TowerDefenseControlNew] currentLevelConfig 无效!");
			}
			if (!_battleGraphReady)
			{
				GD.PushError("[TowerDefenseControlNew] Battle graph initialization failed; state startup was cancelled.");
				SceneManager.Instance?.CompleteSceneLoading();
				return;
			}
			UISwitched(GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool());
			GetNode<StateChartState>("State/State/CompoundState/GameInit").OnStateEntered += GameInitEntered;
			StateChartState node = GetNode<StateChartState>("State/State/CompoundState/GameEntry");
			node.OnStateEntered += GameEntryEntered;
			node.OnStateExited += GameEntryExited;
			StateChartState node2 = GetNode<StateChartState>("State/State/CompoundState/GameReady");
			node2.OnStateEntered += GameReadyEntered;
			node2.OnStateExited += GameReadyExited;
			StateChartState node3 = GetNode<StateChartState>("State/State/CompoundState/GameRunning");
			node3.OnStateEntered += GameRunningEntered;
			node3.OnStateExited += GameRunningExited;
			node3.OnStatePhysicsProcessing += GameRunningProcessing;
			GetNode<StateChartState>("State/State/CompoundState/GameFail").OnStateEntered += GameFailEntered;
			CallDeferred("SendStateEvent");
		}
	}

	public override void _ExitTree()
	{
		GameSaveManager.Instance?.EndProgressRecovery(this);
		_restoringProgress = null;
		_battleShuttingDown = true;
		CancelBattleLifetimes();
		_pendingBattleOperations.Clear();
		if (GodotObject.IsInstanceValid(SceneManager.Instance))
		{
			SceneManager.Instance.OnSceneChange -= OnSceneChange;
		}
		if (GodotObject.IsInstanceValid(BattleEventBus.Instance))
		{
			BattleEventBus.Instance.OnUiSwitched -= UISwitched;
			BattleEventBus.Instance.OnPacketUIFront -= PacketUIFront;
			if (_battleNetworkHost != null)
			{
				BattleEventBus.Instance.OnGameVictory -= _battleNetworkHost.HandleLocalVictory;
				BattleEventBus.Instance.OnGameFailed -= _battleNetworkHost.HandleLocalFailed;
			}
		}
		if (GodotObject.IsInstanceValid(MultiPlayerManager.Instance) && _battleNetworkHost != null)
		{
			MultiPlayerManager.Instance.OnMatchStateReceived -= _battleNetworkHost.HandleLegacyMessage;
			MultiPlayerManager.Instance.OnPeerLeft -= _battleNetworkHost.HandlePeerLeft;
		}
		_battleNetworkHost?.Dispose();
		_battleNetworkHost = null;
		DestroyProcessSafely(process, "scene exit");
		process = null;
		DestroyAllFeaturesSafely("scene exit");
		if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
		{
			ProjectileUpdateManager.Instance.Clear();
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.characterRegistry))
		{
			TowerDefenseManager.Instance.characterRegistry.Clear();
		}
		TowerDefenseManager.ClearDeathRecords();
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		TowerDefenseCharacterBuffHypnoses.CleanupBattleState();
		TowerDefenseGroundItemBase.ClearStaticBattleReferences();
		DragMoveComponent.ClearCurrentMoveCharacter();
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.ReleaseBattleReferences(this, _preserveLevelConfigOnExit);
		}
		levelConfig = null;
		levelControl = null;
		characterNode = null;
		MultiPlayerManager.Instance?.ReleaseBattleCompatibility();
	}

	private void CancelBattleLifetimes()
	{
		process?.CancelLifetime();
		foreach (TowerDefenseBattleFeature value in featureDictionary.Values)
		{
			value?.CancelLifetime();
		}
	}

	private void TrySendBattleStateEvent(StringName eventName)
	{
		if (!_battleShuttingDown && GodotObject.IsInstanceValid(this) && GodotObject.IsInstanceValid(state))
		{
			state.SendEvent(eventName);
		}
	}

	private async void SendStateEvent()
	{
		if (state == null)
		{
			GD.PushError("[SendStateEvent] state null!");
			return;
		}
		for (int i = 0; i < state.GetChildCount(); i++)
		{
			state.GetChild(i);
		}
		await ToSignal(Engine.GetMainLoop(), "process_frame");
		await ToSignal(Engine.GetMainLoop(), "process_frame");
		CompoundState compoundState = ((state.GetChildCount() > 0) ? (state.GetChild(0) as CompoundState) : null);
		if (compoundState != null)
		{
			for (int j = 0; j < compoundState.GetChildCount(); j++)
			{
				_ = compoundState.GetChild(j) is StateChartState;
			}
		}
		else
		{
			GD.PushError("[SendStateEvent] CompoundState null 或类型不匹配!");
		}
		try
		{
			TrySendBattleStateEvent("ToGameInit");
		}
		catch (Exception value)
		{
			GD.PushError($"[SendStateEvent] SendEvent 异常: {value}");
		}
	}

	public override void Init(TowerDefenseLevelBaseConfig _levelConfig)
	{
		ModLevelIdentity = ((Global.Instance.enterLevelMode == "ModLevel") ? XWModLevelSession.Current : null);
		if (Global.Instance.enterLevelMode == "ModLevel" && (ModLevelIdentity == null || ModLevelIdentity.LevelSaveKey != _levelConfig.name))
		{
			throw new InvalidOperationException("Mod 关卡身份与启动配置不匹配");
		}
		if (Global.IsMultiplayerMode && _battleNetworkHost == null)
		{
			_battleNetworkHost = TowerDefenseBattleNetworkHost.TryCreate(new TowerDefenseBattleNetworkContext(this));
		}
		base.Init(_levelConfig);
		if (levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig)
		{
			Global.TimeScale = towerDefenseLevelConfig.baseTimeScale;
		}
		if (levelConfig.name != "")
		{
			if (!Global.IsMultiplayerMode && GameSaveManager.Instance.CanUseLevelProgressInCurrentMode() && GameSaveManager.Instance.HasLevelProgress(levelConfig.name, ModLevelIdentity) && TowerDefenseManager.GetGameMethod() != TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ)
			{
				hasProgress = true;
			}
			if (!Global.IsMultiplayerMode)
			{
				Dictionary dictionary = ((ModLevelIdentity == null) ? GameSaveManager.Instance.GetLevelValue(levelConfig.name) : XWModPlayerProgressService.GetLevel(ModLevelIdentity));
				Dictionary dictionary2 = dictionary.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary();
				int num = dictionary2.GetValueOrDefault("Play", 0).AsInt32();
				dictionary2["Play"] = num + 1;
				dictionary["Key"] = dictionary2;
				if (ModLevelIdentity != null)
				{
					XWModPlayerProgressService.SetLevel(ModLevelIdentity, dictionary);
				}
				else
				{
					GameSaveManager.Instance.SetLevelValue(levelConfig.name, dictionary);
					GameSaveManager.Instance.Save();
				}
			}
		}
		bool flag;
		try
		{
			flag = ((!(levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig2)) ? (levelConfig is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig && NewLevelInit(towerDefenseLevelNewConfig)) : OldLevelInit(towerDefenseLevelConfig2));
		}
		catch (Exception value)
		{
			GD.PushError($"[BattleGraph] Initialization threw: {value}");
			flag = false;
		}
		if (!flag)
		{
			AbortBattleGraphInitialization();
			return;
		}
		_battleGraphReady = true;
		if (CommandManager.Instance.debugOpenGlove)
		{
			AddFeature("Glove", new Dictionary());
		}
		if (Global.IsMultiplayerMode)
		{
			if (_battleNetworkHost != null)
			{
				MultiPlayerManager.Instance.OnMatchStateReceived += _battleNetworkHost.HandleLegacyMessage;
				MultiPlayerManager.Instance.OnPeerLeft += _battleNetworkHost.HandlePeerLeft;
				BattleEventBus.Instance.OnGameVictory += _battleNetworkHost.HandleLocalVictory;
				BattleEventBus.Instance.OnGameFailed += _battleNetworkHost.HandleLocalFailed;
				_battleNetworkHost.RequestFullSnapshot();
			}
			InitPlayerStatusPanel();
		}
	}

	public bool OldLevelInit(TowerDefenseLevelConfig _levelConfig)
	{
		if (GodotObject.IsInstanceValid(_levelConfig.data))
		{
			_levelConfig.ExportToFeatureProcess();
		}
		foreach (StringName key in _levelConfig.featureData.Keys)
		{
			if (!AddFeature(key, _levelConfig.featureData[key]))
			{
				return false;
			}
		}
		if (HasProcessName(_levelConfig.processName))
		{
			if (_levelConfig.processName == (StringName)"Wave" && _levelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM2)
			{
				if (!SetProcess("IZM2", _levelConfig.processData))
				{
					return false;
				}
			}
			else if (!SetProcess(_levelConfig.processName, _levelConfig.processData))
			{
				return false;
			}
		}
		foreach (TowerDefenseBattleFeature value in featureDictionary.Values)
		{
			value.OnReady();
		}
		process?.OnReady();
		levelControl?.Init(_levelConfig);
		return true;
	}

	public bool NewLevelInit(TowerDefenseLevelNewConfig _levelConfig)
	{
		foreach (StringName key in _levelConfig.featureData.Keys)
		{
			if (!AddFeature(key, _levelConfig.featureData[key]))
			{
				return false;
			}
		}
		if (HasProcessName(_levelConfig.processName) && !SetProcess(_levelConfig.processName, _levelConfig.processData))
		{
			return false;
		}
		foreach (TowerDefenseBattleFeature value in featureDictionary.Values)
		{
			value.OnReady();
		}
		process?.OnReady();
		return true;
	}

	public bool AddFeature(StringName featureName, Dictionary data)
	{
		if (_battleShuttingDown)
		{
			return false;
		}
		if (featureDictionary.ContainsKey(featureName))
		{
			return true;
		}
		FeatureAddTransaction transaction = new FeatureAddTransaction();
		if (TryAddFeature(featureName, data, transaction, out var failureReason))
		{
			return true;
		}
		RollbackFeatureAddTransaction(transaction);
		GD.PushError($"[AddFeature] Failed to add {featureName}: {failureReason}");
		return false;
	}

	private bool TryAddFeature(StringName featureName, Dictionary data, FeatureAddTransaction transaction, out string failureReason)
	{
		failureReason = "";
		if (featureDictionary.ContainsKey(featureName))
		{
			return true;
		}
		if (!_featuresBeingAdded.Add(featureName))
		{
			failureReason = $"circular feature dependency at {featureName}";
			return false;
		}
		TowerDefenseBattleFeature towerDefenseBattleFeature = null;
		try
		{
			towerDefenseBattleFeature = TowerDefenseBattleRegistry.GetFeature(featureName);
			if (towerDefenseBattleFeature == null)
			{
				failureReason = $"feature {featureName} is not registered";
				return false;
			}
			if (towerDefenseBattleFeature.dependenceData != null)
			{
				foreach (StringName requiredFeatureName in towerDefenseBattleFeature.dependenceData.RequiredFeatureNames)
				{
					if (!TryAddDependenceFeature(requiredFeatureName, transaction, out var failureReason2))
					{
						failureReason = $"dependency {requiredFeatureName} for {featureName} failed: {failureReason2}";
						return false;
					}
				}
				foreach (StringName item in towerDefenseBattleFeature.dependenceData.ConfiguredOptionalFeatureNames ?? new Array<StringName>())
				{
					if (!TryAddConfiguredOptionalFeature(item, transaction, out var failureReason3))
					{
						failureReason = $"configured optional dependency {item} for {featureName} failed: {failureReason3}";
						return false;
					}
				}
			}
			towerDefenseBattleFeature.control = this;
			towerDefenseBattleFeature.Init(data ?? new Dictionary());
			featureDictionary[featureName] = towerDefenseBattleFeature;
			_featureInitializationOrder.Add(featureName);
			transaction.AddedFeatureNames.Add(featureName);
			return true;
		}
		catch (Exception value)
		{
			if (featureDictionary.TryGetValue(featureName, out var value2) && value2 == towerDefenseBattleFeature)
			{
				featureDictionary.Remove(featureName);
				_featureInitializationOrder.Remove(featureName);
			}
			DestroyFeatureSafely(featureName, towerDefenseBattleFeature, "initialization failure");
			failureReason = $"{towerDefenseBattleFeature?.GetType().Name ?? "unknown feature"} initialization threw: {value}";
			return false;
		}
		finally
		{
			_featuresBeingAdded.Remove(featureName);
		}
	}

	public void AddProcessDependenceFeature()
	{
		if (process != null)
		{
			FeatureAddTransaction transaction = new FeatureAddTransaction();
			if (!TryAddProcessDependenceFeatures(process, transaction, out var failureReason))
			{
				RollbackFeatureAddTransaction(transaction);
				GD.PushError("[ProcessDependencies] Failed for " + process.GetType().Name + ": " + failureReason);
			}
		}
	}

	private bool TryAddProcessDependenceFeatures(TowerDefenseBattleProcess targetProcess, FeatureAddTransaction transaction, out string failureReason)
	{
		failureReason = "";
		if (targetProcess == null || targetProcess.dependenceData == null)
		{
			return true;
		}
		foreach (StringName requiredFeatureName in targetProcess.dependenceData.RequiredFeatureNames)
		{
			if (!TryAddDependenceFeature(requiredFeatureName, transaction, out failureReason))
			{
				return false;
			}
		}
		foreach (StringName item in targetProcess.dependenceData.ConfiguredOptionalFeatureNames ?? new Array<StringName>())
		{
			if (!TryAddConfiguredOptionalFeature(item, transaction, out failureReason))
			{
				return false;
			}
		}
		return true;
	}

	private bool TryAddDependenceFeature(StringName featureName, FeatureAddTransaction transaction, out string failureReason)
	{
		failureReason = "";
		if (featureDictionary.ContainsKey(featureName))
		{
			return true;
		}
		try
		{
			return TryAddFeature(featureName, GetConfiguredFeatureData(featureName), transaction, out failureReason);
		}
		catch (Exception value)
		{
			failureReason = $"failed to build configuration for {featureName}: {value}";
			return false;
		}
	}

	private bool TryAddConfiguredOptionalFeature(StringName featureName, FeatureAddTransaction transaction, out string failureReason)
	{
		failureReason = "";
		if (featureDictionary.ContainsKey(featureName) || !IsFeatureConfigured(featureName))
		{
			return true;
		}
		return TryAddDependenceFeature(featureName, transaction, out failureReason);
	}

	private bool IsFeatureConfigured(StringName featureName)
	{
		TowerDefenseLevelBaseConfig towerDefenseLevelBaseConfig = levelConfig;
		if (!(towerDefenseLevelBaseConfig is TowerDefenseLevelConfig towerDefenseLevelConfig))
		{
			if (towerDefenseLevelBaseConfig is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig)
			{
				return towerDefenseLevelNewConfig.featureData.ContainsKey(featureName);
			}
			return false;
		}
		return towerDefenseLevelConfig.featureData.ContainsKey(featureName);
	}

	private void RollbackFeatureAddTransaction(FeatureAddTransaction transaction)
	{
		for (int num = transaction.AddedFeatureNames.Count - 1; num >= 0; num--)
		{
			StringName stringName = transaction.AddedFeatureNames[num];
			if (featureDictionary.Remove(stringName, out var value))
			{
				_featureInitializationOrder.Remove(stringName);
				DestroyFeatureSafely(stringName, value, "dependency rollback");
			}
		}
		transaction.AddedFeatureNames.Clear();
	}

	private static void DestroyFeatureSafely(StringName featureName, TowerDefenseBattleFeature feature, string context)
	{
		if (feature == null)
		{
			return;
		}
		try
		{
			feature.CancelLifetime();
			feature.Destroy();
		}
		catch (Exception value)
		{
			GD.PushError($"[FeatureCleanup] Failed to destroy {featureName} during {context}: {value}");
		}
		finally
		{
			feature.control = null;
		}
	}

	private static void DestroyProcessSafely(TowerDefenseBattleProcess targetProcess, string context)
	{
		if (targetProcess == null)
		{
			return;
		}
		try
		{
			targetProcess.CancelLifetime();
			targetProcess.Destroy();
		}
		catch (Exception value)
		{
			GD.PushError($"[ProcessCleanup] Failed to destroy {targetProcess.GetType().Name} during {context}: {value}");
		}
		finally
		{
			targetProcess.control = null;
		}
	}

	private void DestroyAllFeaturesSafely(string context)
	{
		for (int num = _featureInitializationOrder.Count - 1; num >= 0; num--)
		{
			StringName stringName = _featureInitializationOrder[num];
			if (featureDictionary.Remove(stringName, out var value))
			{
				DestroyFeatureSafely(stringName, value, context);
			}
		}
		_featureInitializationOrder.Clear();
		foreach (KeyValuePair<StringName, TowerDefenseBattleFeature> item in featureDictionary.ToList())
		{
			featureDictionary.Remove(item.Key);
			DestroyFeatureSafely(item.Key, item.Value, context);
		}
	}

	private void AbortBattleGraphInitialization()
	{
		_battleGraphReady = false;
		DestroyProcessSafely(process, "battle graph abort");
		process = null;
		DestroyAllFeaturesSafely("battle graph abort");
	}

	internal Dictionary GetConfiguredFeatureData(StringName featureName)
	{
		if (levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig && towerDefenseLevelConfig.featureData.TryGetValue(featureName, out var value))
		{
			return value;
		}
		if (levelConfig is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig && towerDefenseLevelNewConfig.featureData.TryGetValue(featureName, out var value2))
		{
			return value2;
		}
		return BuildMissingDependenceFeatureData(featureName);
	}

	private Dictionary BuildMissingDependenceFeatureData(StringName featureName)
	{
		Dictionary dictionary = new Dictionary();
		if (!(levelConfig is TowerDefenseLevelConfig towerDefenseLevelConfig))
		{
			return dictionary;
		}
		string text = featureName.ToString();
		if (!(text == "PacketBank"))
		{
			if (text == "SeedBank")
			{
				dictionary["Method"] = Enum.GetName(typeof(TowerDefenseEnum.LEVEL_SEEDBANK_METHOD), towerDefenseLevelConfig.packetBankMethod);
				dictionary["PlantColumn"] = towerDefenseLevelConfig.plantColumn;
				dictionary["ColdDownStart"] = towerDefenseLevelConfig.packetColdDownStart;
				dictionary["ColdDownUse"] = towerDefenseLevelConfig.packetColdDownUse;
				dictionary["Packet"] = new Godot.Collections.Array();
				foreach (Variant packetBank in towerDefenseLevelConfig.packetBankList)
				{
					if (packetBank.VariantType == Variant.Type.Object)
					{
						TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = packetBank.As<TowerDefenseLevelPacketConfig>();
						if (towerDefenseLevelPacketConfig != null)
						{
							((Godot.Collections.Array)dictionary["Packet"]).Add(towerDefenseLevelPacketConfig.Export());
						}
					}
					else if (packetBank.VariantType == Variant.Type.String)
					{
						((Godot.Collections.Array)dictionary["Packet"]).Add(packetBank);
					}
				}
			}
		}
		else
		{
			dictionary["PacketBankName"] = (string.IsNullOrEmpty(towerDefenseLevelConfig.packetBank) ? "GeneralPlant" : towerDefenseLevelConfig.packetBank);
		}
		return dictionary;
	}

	public void ZombieWonLevelFail(bool playAnime = true)
	{
		if (GodotObject.IsInstanceValid(zombieWon))
		{
			zombieWon.LevelFail(playAnime);
		}
	}

	public TowerDefenseBattleFeature GetFeature(StringName featureName)
	{
		if (!featureDictionary.TryGetValue(featureName, out var value))
		{
			return null;
		}
		return value;
	}

	public T GetFeature<T>(StringName featureName) where T : TowerDefenseBattleFeature
	{
		if (!featureDictionary.TryGetValue(featureName, out var value))
		{
			return null;
		}
		return value as T;
	}

	public bool TryGetFeature<T>(StringName featureName, out T feature) where T : TowerDefenseBattleFeature
	{
		feature = GetFeature<T>(featureName);
		return feature != null;
	}

	public bool RemoveFeature(StringName featureName)
	{
		if (!CanRemoveFeature(featureName, out var failureReason))
		{
			GD.PushWarning($"[RemoveFeature] Rejected {featureName}: {failureReason}");
			return false;
		}
		if (!featureDictionary.Remove(featureName, out var value))
		{
			return false;
		}
		_featureInitializationOrder.Remove(featureName);
		DestroyFeatureSafely(featureName, value, "feature removal");
		return true;
	}

	private bool CanRemoveFeature(StringName featureName, out string failureReason)
	{
		failureReason = "";
		if (!featureDictionary.ContainsKey(featureName))
		{
			return true;
		}
		if (ComponentDependsOnFeature(process, featureName))
		{
			failureReason = "active Process " + process.GetType().Name + " depends on it";
			return false;
		}
		foreach (KeyValuePair<StringName, TowerDefenseBattleFeature> item in featureDictionary)
		{
			if (!(item.Key == featureName) && ComponentDependsOnFeature(item.Value, featureName))
			{
				failureReason = $"active Feature {item.Key} depends on it";
				return false;
			}
		}
		return true;
	}

	private static bool ComponentDependsOnFeature(TowerDefenseBattleComponentBase component, StringName featureName)
	{
		if (component?.dependenceData == null)
		{
			return false;
		}
		if (!ContainsFeatureName(component.dependenceData.RequiredFeatureNames, featureName))
		{
			return ContainsFeatureName(component.dependenceData.ConfiguredOptionalFeatureNames, featureName);
		}
		return true;
	}

	private static bool ContainsFeatureName(Array<StringName> featureNames, StringName featureName)
	{
		if (featureNames == null)
		{
			return false;
		}
		foreach (StringName featureName2 in featureNames)
		{
			if (featureName2 == featureName)
			{
				return true;
			}
		}
		return false;
	}

	public bool SetProcess(StringName processName, Dictionary data = null)
	{
		if (_battleShuttingDown)
		{
			return false;
		}
		if (!HasProcessName(processName))
		{
			GD.PushError("[SetProcess] Process name is empty.");
			return false;
		}
		if (data == null)
		{
			data = new Dictionary();
		}
		TowerDefenseBattleProcess towerDefenseBattleProcess = TowerDefenseBattleRegistry.GetProcess(processName);
		if (towerDefenseBattleProcess == null)
		{
			return false;
		}
		towerDefenseBattleProcess.control = this;
		FeatureAddTransaction transaction = new FeatureAddTransaction();
		if (!TryAddProcessDependenceFeatures(towerDefenseBattleProcess, transaction, out var failureReason))
		{
			RollbackFeatureAddTransaction(transaction);
			DestroyProcessSafely(towerDefenseBattleProcess, "dependency failure");
			GD.PushError($"[SetProcess] Failed to resolve dependencies for {processName}: {failureReason}");
			return false;
		}
		try
		{
			towerDefenseBattleProcess.Init(data);
			TowerDefenseBattleProcess towerDefenseBattleProcess2 = process;
			process = towerDefenseBattleProcess;
			if (towerDefenseBattleProcess2 != null && towerDefenseBattleProcess2 != towerDefenseBattleProcess)
			{
				DestroyProcessSafely(towerDefenseBattleProcess2, "process replacement");
			}
			return true;
		}
		catch (Exception value)
		{
			RollbackFeatureAddTransaction(transaction);
			DestroyProcessSafely(towerDefenseBattleProcess, "initialization failure");
			GD.PushError($"[SetProcess] Failed to initialize {processName} ({towerDefenseBattleProcess.GetType().Name}): {value}");
			return false;
		}
	}

	private static bool HasProcessName(StringName processName)
	{
		if (processName != null)
		{
			return !processName.IsEmpty;
		}
		return false;
	}

	public bool CanCreateProgressSave(out string reason)
	{
		if (HasPendingBattleOperations)
		{
			reason = "a transient battle operation is still running";
			return false;
		}
		foreach (KeyValuePair<StringName, TowerDefenseBattleFeature> item in featureDictionary)
		{
			if (item.Value is ITowerDefenseProgressSaveGuard towerDefenseProgressSaveGuard && !towerDefenseProgressSaveGuard.CanSaveProgress(out var reason2))
			{
				reason = $"Feature[{item.Key}]: {reason2}";
				return false;
			}
		}
		if (process is ITowerDefenseProgressSaveGuard towerDefenseProgressSaveGuard2 && !towerDefenseProgressSaveGuard2.CanSaveProgress(out var reason3))
		{
			reason = "Process[" + process.GetType().Name + "]: " + reason3;
			return false;
		}
		reason = "";
		return true;
	}

	public void AddNode(Node node, int layerId = -1)
	{
		if (!layerDictionary.ContainsKey(layerId))
		{
			CanvasLayer canvasLayer = new CanvasLayer();
			canvasLayer.Layer = layerId;
			canvasLayer.FollowViewportEnabled = true;
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			layerDictionary[layerId] = canvasLayer;
		}
		layerDictionary[layerId].AddChild(node, forceReadableName: false, InternalMode.Disabled);
	}

	public void AddUI(Node node, int layerId)
	{
		if (!uilayerDictionary.ContainsKey(layerId))
		{
			CanvasLayer canvasLayer = new CanvasLayer();
			canvasLayer.Layer = layerId;
			canvasLayer.FollowViewportEnabled = false;
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			uilayerDictionary[layerId] = canvasLayer;
		}
		uilayerDictionary[layerId].AddChild(node, forceReadableName: false, InternalMode.Disabled);
	}

	public void MoveUI(Node node, int layerId)
	{
		if (!uilayerDictionary.ContainsKey(layerId))
		{
			CanvasLayer canvasLayer = new CanvasLayer();
			canvasLayer.Layer = layerId;
			canvasLayer.FollowViewportEnabled = false;
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			uilayerDictionary[layerId] = canvasLayer;
		}
		CanvasLayer newParent = uilayerDictionary[layerId];
		node.Reparent(newParent);
	}

	public void AddUIToTopBankContainer(Node node)
	{
		if (_coexistHud != null)
		{
			_coexistHud.RegisterBank(node);
		}
		else
		{
			uITopBankContainer?.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void AddUIToTopPropContainer(Node node)
	{
		uITopPropContainer?.AddChild(node, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isGameRunning)
		{
			TowerDefenseManager.Instance.runGameTime += delta;
			if (!waitPause && !Global.IsMultiplayerMode && !GetTree().Paused && !GameSaveManager.Instance.GetConfigValue("Backgrounder").AsBool() && !DisplayServer.WindowIsFocused())
			{
				OpenGamePauseDialog();
			}
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (isGameRunning && !waitPause && !GetTree().Paused && inputEvent.IsActionPressed("Pause"))
		{
			GetViewport().SetInputAsHandled();
			if (!Global.IsMultiplayerMode)
			{
				OpenGamePauseDialog();
			}
			else if (!_isNetworkPaused)
			{
				_battleNetworkHost?.ApplyPause(paused: true);
				MultiPlayerManager.Instance.SendPause();
			}
		}
	}

	private void OpenGamePauseDialog()
	{
		AudioManager.Instance.AudioPlay("Pause", AudioManagerEnum.TYPE.SFX, 0.0, once: true, pauseAlive: true);
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("Pause");
		waitPause = true;
		dialogBoxBase.OnClose += delegate
		{
			waitPause = false;
		};
	}

	public override void _Input(InputEvent event_)
	{
		if (process != null)
		{
			process.InputProcess(event_);
		}
		if (event_.IsActionPressed("SpeedUp") && !(event_ is InputEventKey { Echo: not false }) && !Global.IsMultiplayerMode)
		{
			checkBox2X.ButtonPressed = !checkBox2X.ButtonPressed;
		}
		if (Input.IsActionJustPressed("PacketUIFront"))
		{
			GameSaveManager.Instance.SetConfigValue("PacketUIFront", !GameSaveManager.Instance.GetConfigValue("PacketUIFront").AsBool());
			BattleEventBus.Instance.EmitPacketUIFront(GameSaveManager.Instance.GetConfigValue("PacketUIFront").AsBool());
			GameSaveManager.Instance.SaveGameConfig();
		}
		if (Input.IsActionJustPressed("ShowPlantHealth"))
		{
			GameSaveManager.Instance.SetConfigValue("ShowPlantHealth", !GameSaveManager.Instance.GetConfigValue("ShowPlantHealth").AsBool());
			BattleEventBus.Instance.EmitShowPlantHealth(GameSaveManager.Instance.GetConfigValue("ShowPlantHealth").AsBool());
			GameSaveManager.Instance.SaveGameConfig();
		}
		if (Input.IsActionJustPressed("ShowZombieHealth"))
		{
			GameSaveManager.Instance.SetConfigValue("ShowZombieHealth", !GameSaveManager.Instance.GetConfigValue("ShowZombieHealth").AsBool());
			BattleEventBus.Instance.EmitShowZombieHealth(GameSaveManager.Instance.GetConfigValue("ShowZombieHealth").AsBool());
			GameSaveManager.Instance.SaveGameConfig();
		}
		if (Global.IsMultiplayerMode && GodotObject.IsInstanceValid(_playerStatusPanel))
		{
			if (Input.IsActionPressed("ShowPlayerStatus"))
			{
				_playerStatusPanel.Call("ShowPanel");
			}
			else
			{
				_playerStatusPanel.Call("HidePanel");
			}
		}
	}

	private async Task InvokeLifecycleWithProgress(Func<TowerDefenseBattleComponentBase, Task> fresh, Func<TowerDefenseBattleComponentBase, Task> fromProgress)
	{
		_progressInitialized.Clear();
		foreach (KeyValuePair<StringName, TowerDefenseBattleFeature> item in featureDictionary.ToList())
		{
			await InitializeProgressComponent(item.Value, _restoringProgress.TryGetFeatureSaveData(item.Key.ToString(), out var _), fresh, fromProgress);
		}
		if (process != null)
		{
			await InitializeProgressComponent(process, _restoringProgress.HasProcessSaveData, fresh, fromProgress);
		}
	}

	private async Task InitializeProgressComponent(TowerDefenseBattleComponentBase component, bool hasSection, Func<TowerDefenseBattleComponentBase, Task> fresh, Func<TowerDefenseBattleComponentBase, Task> fromProgress)
	{
		_ = 1;
		try
		{
			if (!hasSection || !component.CanLoadProgress())
			{
				await fresh(component);
				return;
			}
			_progressInitialized.Add(component);
			await fromProgress(component);
		}
		catch (Exception ex)
		{
			_restoringProgress.RestoreReport.Record("Initialization", component.GetType().Name + ": " + ex.Message);
		}
	}

	private async Task InvokeLifecycle(Func<TowerDefenseBattleComponentBase, Task> action)
	{
		foreach (TowerDefenseBattleFeature feature in featureDictionary.Values.ToList())
		{
			try
			{
				await action(feature);
			}
			catch (Exception value)
			{
				GD.PushError($"[InvokeLifecycle] 组件异常: {feature.GetType().Name}, {value}");
			}
		}
		if (process != null)
		{
			try
			{
				await action(process);
			}
			catch (Exception value2)
			{
				GD.PushError($"[InvokeLifecycle] Process 异常: {process.GetType().Name}, {value2}");
			}
		}
	}

	private async Task InvokeGameStartLifecycle(bool fromProgress)
	{
		IEnumerable<TowerDefenseBattleComponentBase> source = featureDictionary.Values.Cast<TowerDefenseBattleComponentBase>();
		if (process != null)
		{
			source = source.Append(process);
		}
		IEnumerable<TowerDefenseBattleComponentBase> enumerable = from item in source.Select((TowerDefenseBattleComponentBase component, int index) => new { component, index })
			orderby item.component.gameStartPriority, item.index
			select item.component;
		foreach (TowerDefenseBattleComponentBase item in enumerable)
		{
			await InvokeGameStartComponent(item, fromProgress && _progressInitialized.Contains(item));
		}
	}

	private async Task InvokeGameStartComponent(TowerDefenseBattleComponentBase component, bool fromProgress)
	{
		_ = 1;
		try
		{
			if (!fromProgress || !component.CanLoadProgress())
			{
				await component.GameStart();
			}
			else
			{
				await component.GameStartFromProgress();
			}
		}
		catch (Exception ex)
		{
			if (_restoringProgress != null)
			{
				_restoringProgress.RestoreReport.Record("Initialization", ex.Message);
				return;
			}
			GD.PushError($"[InvokeGameStartLifecycle] 组件异常: {component.GetType().Name}, {ex}");
		}
	}

	private async Task InvokeGameEntryLifecycle()
	{
		bool npcTalkRequiresPreSpawnBeforeTalk = NpcTalkRequiresPreSpawnBeforeTalk();
		IEnumerable<TowerDefenseBattleFeature> enumerable = from item in featureDictionary.Values.Select((TowerDefenseBattleFeature feature2, int index) => new
			{
				feature = feature2,
				index = index
			})
			orderby GetGameEntryFeaturePriority(item.feature, npcTalkRequiresPreSpawnBeforeTalk), item.index
			select item.feature;
		foreach (TowerDefenseBattleFeature feature in enumerable)
		{
			try
			{
				await feature.GameEntry();
			}
			catch (Exception value)
			{
				GD.PushError($"[InvokeGameEntryLifecycle] 组件异常: {feature.GetType().Name}, {value}");
			}
		}
		if (process != null)
		{
			try
			{
				await process.GameEntry();
			}
			catch (Exception value2)
			{
				GD.PushError($"[InvokeGameEntryLifecycle] Process 异常: {process.GetType().Name}, {value2}");
			}
		}
	}

	private bool NpcTalkRequiresPreSpawnBeforeTalk()
	{
		if (!featureDictionary.TryGetValue("NpcTalk", out var value))
		{
			return false;
		}
		if (value is TowerDefenseBattleFeatureNpcTalk towerDefenseBattleFeatureNpcTalk)
		{
			return towerDefenseBattleFeatureNpcTalk.ContainsEmbeddedTutorial();
		}
		return false;
	}

	private static int GetGameEntryFeaturePriority(TowerDefenseBattleFeature feature, bool npcTalkRequiresPreSpawnBeforeTalk)
	{
		if (!(feature is TowerDefenseBattleFeaturePreSpawn))
		{
			if (feature is TowerDefenseBattleFeatureNpcTalk)
			{
				return 20;
			}
			return 10;
		}
		if (npcTalkRequiresPreSpawnBeforeTalk)
		{
			return 0;
		}
		return 30;
	}

	public async void GameInitEntered()
	{
		SceneManager.Instance?.UpdateLoadingStatus("正在初始化关卡");
		try
		{
			if (GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				await ResourceManager.Instance.EnsureGameplayAtlasesReadyAsync();
			}
		}
		catch (Exception value)
		{
			GD.PushError($"[State] Gameplay atlas readiness failed: {value}");
			SceneManager.Instance?.UpdateLoadingStatus("战斗动画资源加载失败");
			return;
		}
		try
		{
			TowerDefenseLevelSaveConfigCSharp progressSave = null;
			if (Global.IsMultiplayerMode)
			{
				await XWModEnvironmentService.EnsureReadyAsync();
			}
			if (hasProgress && !GameSaveManager.Instance.TryGetLoadableLevelProgress(levelConfig.name, out progressSave, out var reason, ModLevelIdentity))
			{
				RejectProgressLoad(reason);
				return;
			}
			if (hasProgress)
			{
				SceneManager.Instance?.UpdateLoadingStatus("正在准备存档关卡");
				_restoringProgress = progressSave;
				PrepareSavedFeatures();
				await InvokeLifecycleWithProgress((TowerDefenseBattleComponentBase c) => c.GameInit(), (TowerDefenseBattleComponentBase c) => c.GameInitFromProgress());
				await ShowLoadingStatusForNextFrameAsync("正在读取存档");
				if (!progressSave.Load(resetReport: false))
				{
					RejectProgressLoad("当前关卡的基础结构无法初始化。");
					return;
				}
			}
			else
			{
				SceneManager.Instance?.UpdateLoadingStatus("正在加载关卡角色资源");
				await InvokeLifecycle((TowerDefenseBattleComponentBase c) => c.GameInit());
			}
		}
		catch (Exception ex)
		{
			GD.PushError($"[State] GameInitEntered 异常: {ex}");
			if (hasProgress)
			{
				RejectProgressLoad("存档恢复失败：" + ex.Message);
				return;
			}
		}
		if (!_levelConfigurationRejected)
		{
			SceneManager.Instance?.UpdateLoadingStatus("正在进入关卡");
			SceneManager.Instance?.CompleteSceneLoading();
			TrySendBattleStateEvent("ToGameEntry");
		}
	}

	private void RejectProgressLoad(string reason)
	{
		isGameRunning = false;
		SceneManager.Instance?.CompleteSceneLoading();
		GD.PushWarning("[ProgressLoad] " + reason);
		DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("DialogBoxTips");
		dialogBoxBase.Set("text", "无法恢复关卡进度，原存档已保留。\n" + reason.Replace("[", "[lb]"));
		dialogBoxBase.OnClose += delegate
		{
			string text;
			switch (Global.Instance.enterLevelMode)
			{
			case "ModLevel":
				text = "LevelChoose";
				break;
			case "LevelChoose":
				if (Global.Instance.currentLevelChoose != "TryLevel")
				{
					text = "LevelChoose";
					break;
				}
				goto default;
			case "DiyLevel":
			case "LoadLevel":
			case "OnlineLevel":
				text = "LevelEditorStage";
				break;
			default:
				text = "MainMenu";
				break;
			}
			string scene = text;
			SceneManager.Instance.ChangeScene(scene);
		};
	}

	private async Task ShowLoadingStatusForNextFrameAsync(string status)
	{
		SceneManager.Instance?.UpdateLoadingStatus(status);
		SceneTree tree = GetTree();
		if (GodotObject.IsInstanceValid(tree))
		{
			await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
			await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		}
	}

	public async void GameEntryEntered()
	{
		isGameRunning = false;
		try
		{
			TaskCompletionSource<bool> gameEntryAckedTcs;
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && MultiPlayerManager.Instance.GameEntrySent)
			{
				MultiPlayerManager.Instance.ResetGameEntryAck();
				if (!MultiPlayerManager.Instance.CheckAllGameEntryAcked())
				{
					gameEntryAckedTcs = new TaskCompletionSource<bool>();
					MultiPlayerManager.Instance.OnAllGameEntryAcked += OnAllGameEntryAckedHandler;
					try
					{
						await gameEntryAckedTcs.Task;
					}
					finally
					{
						MultiPlayerManager.Instance.OnAllGameEntryAcked -= OnAllGameEntryAckedHandler;
					}
				}
			}
			await InvokeGameEntryLifecycle();
			if (!isInit)
			{
				levelControl.awardCreate = false;
			}
			void OnAllGameEntryAckedHandler()
			{
				gameEntryAckedTcs.TrySetResult(result: true);
			}
		}
		catch (Exception value)
		{
			GD.PushError($"[State] GameEntryEntered 异常: {value}");
		}
		TrySendBattleStateEvent("ToGameReady");
	}

	public void GameEntryExited()
	{
	}

	public async void GameReadyEntered()
	{
		_ = 1;
		try
		{
			TaskCompletionSource<bool> tcs;
			if (Global.IsMultiplayerMode)
			{
				if (!_chooseReadyPeers.Contains(MultiPlayerManager.Instance.peerId))
				{
					_chooseReadyPeers.Add(MultiPlayerManager.Instance.peerId);
				}
				ShowChooseWaitLabel();
				MultiPlayerManager.Instance.SendChooseReady();
				MultiPlayerManager.Instance.ResetClientsReady();
				if (MultiPlayerManager.IsHost && !_chooseOverReceived)
				{
					bool flag = true;
					foreach (Variant battleAdmittedPeer in MultiPlayerManager.Instance.BattleAdmittedPeers)
					{
						if (!_chooseReadyPeers.Contains(battleAdmittedPeer.AsString()))
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						MultiPlayerManager.Instance.SendChooseOver();
						DoChooseOver();
					}
				}
				if (!_chooseOverReceived)
				{
					tcs = new TaskCompletionSource<bool>();
					OnViewBack += ViewBackHandler;
					try
					{
						await tcs.Task;
					}
					finally
					{
						OnViewBack -= ViewBackHandler;
					}
				}
				HideChooseWaitLabel();
			}
			await InvokeLifecycle((TowerDefenseBattleComponentBase c) => c.GameReady());
			void ViewBackHandler()
			{
				tcs.TrySetResult(result: true);
			}
		}
		catch (Exception value)
		{
			GD.PushError($"[State] GameReadyEntered 异常: {value}");
		}
		TrySendBattleStateEvent("ToGameRunning");
	}

	public void GameReadyExited()
	{
	}

	public async void GameRunningEntered()
	{
		isGameRunning = true;
		isInit = false;
		_gameRunningEntryReady = false;
		try
		{
			bool num = hasProgress;
			if (Global.IsMultiplayerMode)
			{
				_chooseReadyPeers.Clear();
				_chooseOverReceived = false;
			}
			if (levelConfig is TowerDefenseLevelConfig { finishMethod: TowerDefenseEnum.LEVEL_FINISH_METHOD.QUIZ })
			{
				isGameRunning = false;
			}
			if (num)
			{
				_progressPause = DialogManager.Instance.DialogCreate("Pause");
				if (GodotObject.IsInstanceValid(_progressPause))
				{
					_progressPause.ProcessMode = ProcessModeEnum.Disabled;
				}
			}
			buttonPause.Visible = true;
			optionButton.Visible = true;
			if (Global.IsMultiplayerMode)
			{
				checkBox2X.Visible = false;
				if (checkBox2X.ButtonPressed)
				{
					checkBox2X.ButtonPressed = false;
				}
			}
			else
			{
				checkBox2X.Visible = true;
			}
			if (!num)
			{
				ResumeEntryCharacters();
			}
			if (!num)
			{
				await InvokeGameStartLifecycle(fromProgress: false);
			}
			else
			{
				await InvokeGameStartLifecycle(fromProgress: true);
				ResumeEntryCharacters();
				hasProgress = false;
			}
		}
		catch (Exception value)
		{
			GD.PushError($"[GameRunningEntered] 异常: {value}");
		}
		_gameRunningEntryReady = true;
		CompleteProgressRecovery();
	}

	public void GameRunningExited()
	{
		isGameRunning = false;
		_gameRunningEntryReady = false;
	}

	public void GameRunningProcessing(double delta)
	{
		if (!_gameRunningEntryReady)
		{
			return;
		}
		if (!TowerDefensePerfProfiler.Enabled)
		{
			ProcessZombieCheckArea();
			foreach (TowerDefenseBattleFeature value in featureDictionary.Values)
			{
				value.Process(delta);
			}
			process?.PhysicsProcess(delta);
			if (!Global.IsMultiplayerMode || !MultiPlayerManager.Instance.IsConnect())
			{
				return;
			}
			foreach (TowerDefenseBattleFeature value2 in featureDictionary.Values)
			{
				value2.SyncProcess(delta);
			}
			process?.SyncProcess(delta);
			_battleNetworkHost?.Process(delta);
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		ProcessZombieCheckArea();
		TowerDefensePerfProfiler.End("control.zombieCheckArea", startTicks, _zombieCheckCandidates.Count);
		long startTicks2 = TowerDefensePerfProfiler.Begin();
		foreach (TowerDefenseBattleFeature value3 in featureDictionary.Values)
		{
			long startTicks3 = TowerDefensePerfProfiler.Begin();
			value3.Process(delta);
			TowerDefensePerfProfiler.End("control.featureProcess." + value3.GetType().Name, startTicks3);
		}
		TowerDefensePerfProfiler.End("control.featureProcess", startTicks2, featureDictionary.Count);
		if (process != null)
		{
			long startTicks4 = TowerDefensePerfProfiler.Begin();
			process.PhysicsProcess(delta);
			TowerDefensePerfProfiler.End("control.processPhysics." + process.GetType().Name, startTicks4);
			TowerDefensePerfProfiler.End("control.processPhysics", startTicks4, 1);
		}
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.Instance.IsConnect())
		{
			return;
		}
		long startTicks5 = TowerDefensePerfProfiler.Begin();
		foreach (TowerDefenseBattleFeature value4 in featureDictionary.Values)
		{
			long startTicks6 = TowerDefensePerfProfiler.Begin();
			value4.SyncProcess(delta);
			TowerDefensePerfProfiler.End("control.featureSyncProcess." + value4.GetType().Name, startTicks6);
		}
		TowerDefensePerfProfiler.End("control.featureSyncProcess", startTicks5, featureDictionary.Count);
		if (process != null)
		{
			long startTicks7 = TowerDefensePerfProfiler.Begin();
			process.SyncProcess(delta);
			TowerDefensePerfProfiler.End("control.processSync." + process.GetType().Name, startTicks7);
			TowerDefensePerfProfiler.End("control.processSync", startTicks7, 1);
		}
		if (_battleNetworkHost != null)
		{
			long startTicks8 = TowerDefensePerfProfiler.Begin();
			_battleNetworkHost.Process(delta);
			TowerDefensePerfProfiler.End("control.networkHostProcess", startTicks8);
		}
	}

	private void ResumeEntryCharacters()
	{
		foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
		{
			if (item.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter)
			{
				towerDefenseCharacter.ProcessMode = ProcessModeEnum.Inherit;
				IStateMachineController stateMachine = towerDefenseCharacter.StateMachine;
				if (stateMachine != null && stateMachine.IsInitialized)
				{
					towerDefenseCharacter.SetMainStateMachineDispatchEnabled(enabled: true);
				}
			}
		}
		foreach (Node item2 in GetTree().GetNodesInGroup("Gravestone"))
		{
			if (item2 is TowerDefenseGravestone { inGame: not false, isDestroy: false } towerDefenseGravestone)
			{
				towerDefenseGravestone.ProcessMode = ProcessModeEnum.Inherit;
				IStateMachineController stateMachine2 = towerDefenseGravestone.StateMachine;
				if (stateMachine2 != null && stateMachine2.IsInitialized)
				{
					towerDefenseGravestone.SetMainStateMachineDispatchEnabled(enabled: true);
				}
			}
		}
		foreach (Variant item3 in TowerDefenseManager.Instance.GetZombie())
		{
			if (item3.AsGodotObject() is TowerDefenseZombie towerDefenseZombie)
			{
				towerDefenseZombie.Walk();
			}
		}
	}

	public void GameFailEntered()
	{
		isGameFail = true;
		isGameRunning = false;
		foreach (TowerDefenseBattleFeature value in featureDictionary.Values)
		{
			value.GameFail();
		}
		process?.GameFail(failCharacter);
	}

	private void OnSceneChange(string sceneName)
	{
		_preserveLevelConfigOnExit = sceneName == "TowerDefense" || (sceneName == "LevelEditorStage" && Global.IsEditor && Global.Instance?.enterLevelMode == "DiyLevel");
		if (GodotObject.IsInstanceValid(this))
		{
			_battleShuttingDown = true;
			CancelBattleLifetimes();
			isGameRunning = false;
			base.ProcessMode = ProcessModeEnum.Disabled;
		}
	}

	public void TriggerGameFail(TowerDefenseCharacter enterCharacter)
	{
		failCharacter = enterCharacter;
		TrySendBattleStateEvent("ToGameFail");
	}

	public void TriggerGameEntry()
	{
		TrySendBattleStateEvent("ToGameEntry");
	}

	public async Task ReadySetPlantPlay()
	{
		if (levelControl != null)
		{
			await levelControl.ReadySetPlantPlay();
		}
	}

	public void TipsPlay(string text, double duration = 2.0)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			MultiPlayerManager.Instance.SendTipsPlay(text, duration);
		}
		if (levelControl != null)
		{
			levelControl.TipsPlay(text, duration);
		}
	}

	public void ViewMap()
	{
		process?.ViewMap();
	}

	public void GameFail(TowerDefenseCharacter enterCharacter)
	{
		failCharacter = enterCharacter;
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			MultiPlayerManager.Instance.SendGameResult(victory: false);
		}
		foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
		{
			if (item.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && towerDefenseCharacter != enterCharacter)
			{
				towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
			}
		}
		TrySendBattleStateEvent("ToGameFail");
	}

	public void GameEntry()
	{
		TriggerGameEntry();
	}

	private void ProcessZombieCheckArea()
	{
		if (!GodotObject.IsInstanceValid(zombieCheckArea) || zombieCheckArea.ProcessMode == ProcessModeEnum.Disabled)
		{
			_zombieCheckOverlaps.Clear();
		}
		else
		{
			if (TowerDefenseManager.Instance == null || TowerDefenseManager.Instance.characterRegistry == null)
			{
				return;
			}
			Rect2 rect = AabbShapeUtil.ComputeAreaWorldRect(zombieCheckArea);
			TowerDefenseManager.Instance.characterRegistry.FillCharactersForRectGridWindowList(rect, _zombieCheckCandidates);
			_zombieCheckScratch.Clear();
			for (int i = 0; i < _zombieCheckCandidates.Count; i++)
			{
				if (_zombieCheckCandidates[i] is TowerDefenseZombie towerDefenseZombie && AabbShapeUtil.Intersects(rect, towerDefenseZombie.WorldHitRect))
				{
					_zombieCheckScratch.Add(towerDefenseZombie);
					if (!_zombieCheckOverlaps.Contains(towerDefenseZombie))
					{
						HandleZombieEnterHouse(towerDefenseZombie);
					}
				}
			}
			_zombieCheckOverlaps.RemoveWhere((TowerDefenseZombie zombie) => !GodotObject.IsInstanceValid(zombie) || !_zombieCheckScratch.Contains(zombie));
			foreach (TowerDefenseZombie item in _zombieCheckScratch)
			{
				_zombieCheckOverlaps.Add(item);
			}
		}
	}

	private void HandleZombieEnterHouse(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie) || zombie.instance.die || zombie.instance.nearDie || zombie.instance.hypnoses || zombie.Scale.X < 0f)
		{
			return;
		}
		foreach (TowerDefenseBattleFeature value in featureDictionary.Values)
		{
			value.ZombieEnterHouse(zombie);
		}
		process?.ZombieEnterHouse(zombie);
	}

	public void UISwitched(bool shown)
	{
		if (shown)
		{
			bankUILayer.Layer = 3;
			mobileInterval.CustomMinimumSize = new Vector2(400f, mobileInterval.CustomMinimumSize.Y);
		}
		else
		{
			bankUILayer.Layer = ((!GameSaveManager.Instance.GetConfigValue("PacketUIFront").AsBool()) ? 1 : 3);
			mobileInterval.CustomMinimumSize = new Vector2(0f, mobileInterval.CustomMinimumSize.Y);
		}
	}

	public void PacketUIFront(bool open)
	{
		if (open)
		{
			bankUILayer.Layer = 3;
		}
		else
		{
			bankUILayer.Layer = ((!GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool()) ? 1 : 3);
		}
	}

	public bool ChangeCostAdd(TowerDefensePacketChangeCost changeCost)
	{
		if (changeCost.key != "")
		{
			foreach (TowerDefensePacketChangeCost changeCost2 in changeCostList)
			{
				if (changeCost2.key == changeCost.key)
				{
					return false;
				}
			}
		}
		changeCostList.Add(changeCost);
		return true;
	}

	public bool ChangeCostRemove(TowerDefensePacketChangeCost changeCost)
	{
		int num = changeCostList.IndexOf(changeCost);
		if (num == -1)
		{
			return false;
		}
		changeCostList.RemoveAt(num);
		return true;
	}

	private void InitPlayerStatusPanel()
	{
		CanvasLayer nodeOrNull = GetNodeOrNull<CanvasLayer>("GUITop");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			PackedScene packedScene = GD.Load<PackedScene>("uid://dg77jwfvccr4t");
			_playerStatusPanel = packedScene.Instantiate<PlayerStatusPanel>(PackedScene.GenEditState.Disabled);
			nodeOrNull.AddChild(_playerStatusPanel, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public int GetNextSyncId()
	{
		if (_battleNetworkHost != null)
		{
			return _battleNetworkHost.Entities.NextCharacterSyncId();
		}
		_syncIdCounter++;
		return _syncIdCounter;
	}

	public int GetNextPacketSyncId()
	{
		if (_battleNetworkHost != null)
		{
			return _battleNetworkHost.Entities.NextPacketSyncId();
		}
		_packetSyncIdCounter++;
		return _packetSyncIdCounter;
	}

	public void RegisterSyncPacket(int syncId, TowerDefenseInGamePacketShow packet)
	{
		_syncPackets[syncId] = packet;
		_battleNetworkHost?.Entities.RegisterPacket(syncId, packet);
	}

	public void UnregisterSyncPacket(int syncId)
	{
		_syncPackets.Remove(syncId);
		_battleNetworkHost?.Entities.UnregisterPacket(syncId);
	}

	internal bool TryGetSyncPacket(int syncId, out TowerDefenseInGamePacketShow packet)
	{
		if (_syncPackets.TryGetValue(syncId, out packet) && GodotObject.IsInstanceValid(packet))
		{
			return true;
		}
		packet = null;
		UnregisterSyncPacket(syncId);
		return false;
	}

	internal void RemoveCharacterSyncState(int syncId)
	{
		_syncCharacters.Remove(syncId);
		_characterLastSyncState.Remove(syncId);
		string value = syncId + ":";
		List<Variant> list = new List<Variant>();
		foreach (Variant key in _componentLastSyncState.Keys)
		{
			if (key.AsString().StartsWith(value, StringComparison.Ordinal))
			{
				list.Add(key);
			}
		}
		foreach (Variant item in list)
		{
			_componentLastSyncState.Remove(item);
		}
		_battleNetworkHost?.Entities.UnregisterCharacter(syncId);
	}

	internal void RemoveZombieSyncState(int syncId)
	{
		_zombieLastSyncState.Remove(syncId);
		_zombieTargetPositions.Remove(syncId);
		_zombieSyncVelocities.Remove(syncId);
		_zombieLastSyncTime.Remove(syncId);
		_zombieSyncMissCount.Remove(syncId);
	}

	public void RegisterSyncCharacter(int syncIdVal, TowerDefenseCharacter character)
	{
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, TowerDefenseCharacter> syncCharacter in _syncCharacters)
		{
			if (syncCharacter.Key != syncIdVal && syncCharacter.Value == character)
			{
				list.Add(syncCharacter.Key);
			}
		}
		foreach (int item in list)
		{
			RemoveCharacterSyncState(item);
			RemoveZombieSyncState(item);
		}
		if (_syncCharacters.TryGetValue(syncIdVal, out var value) && value != character)
		{
			_battleNetworkHost?.CharacterState.DiscardPendingOwnerState(syncIdVal);
			if (GodotObject.IsInstanceValid(value))
			{
				value.OnDestroy -= OnSyncCharacterDestroy;
				CleanupCharacterCell(value);
			}
			RemoveCharacterSyncState(syncIdVal);
			RemoveZombieSyncState(syncIdVal);
			if (GodotObject.IsInstanceValid(value) && !value.isDestroy)
			{
				if (value.IsNodeReady())
				{
					DestroyComponent destroyComponent = value.destroyComponent;
					if (destroyComponent != null && !destroyComponent.IsReleased)
					{
						value.destroyComponent.isRemoteDestroy = true;
						value.Destroy();
						goto IL_018a;
					}
				}
				value.skipDestroySet = true;
				if (!value.IsQueuedForDeletion())
				{
					value.QueueFree();
				}
			}
		}
		goto IL_018a;
		IL_018a:
		character.syncId = syncIdVal;
		_syncCharacters[syncIdVal] = character;
		_battleNetworkHost?.Entities.RegisterCharacter(syncIdVal, character);
		_battleNetworkHost?.CharacterState.BindPendingDestroyTarget(syncIdVal, character);
		_battleNetworkHost?.Events.BindPendingCharacterGeneration(syncIdVal, character);
		character.OnDestroy -= OnSyncCharacterDestroy;
		character.OnDestroy += OnSyncCharacterDestroy;
		Dictionary pendingData;
		if (_pendingDestroySyncIds.ContainsKey(syncIdVal))
		{
			pendingData = _pendingDestroySyncIds[syncIdVal].AsGodotDictionary();
			_pendingDestroySyncIds.Remove(syncIdVal);
			if (character.IsNodeReady())
			{
				ApplyPendingDestroyToReadyCharacter(syncIdVal, character, pendingData);
			}
			else
			{
				character.Ready += ApplyWhenReady;
			}
		}
		else if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			CallDeferred("SyncCharacterInitTimer", syncIdVal);
			CallDeferred("SyncCharacterPositionTimer", syncIdVal);
		}
		void ApplyWhenReady()
		{
			character.Ready -= ApplyWhenReady;
			ApplyPendingDestroyToReadyCharacter(syncIdVal, character, pendingData);
		}
	}

	private void ApplyPendingDestroyToReadyCharacter(int syncIdVal, TowerDefenseCharacter character, Dictionary pendingData)
	{
		if (pendingData == null || !GodotObject.IsInstanceValid(character) || character.isDestroy || !character.IsNodeReady() || !_syncCharacters.TryGetValue(syncIdVal, out var value) || value != character)
		{
			return;
		}
		character.OnDestroy -= OnSyncCharacterDestroy;
		RemoveCharacterSyncState(syncIdVal);
		RemoveZombieSyncState(syncIdVal);
		CleanupCharacterCell(character);
		bool flag = pendingData.GetValueOrDefault("is_explode", false).AsBool();
		bool isSmash = pendingData.GetValueOrDefault("is_smash", false).AsBool();
		if (flag && GodotObject.IsInstanceValid(character.instance) && character.instance.ashScene != null && !character.inWater)
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(character.instance.ashScene, character.gridPos, "Idle");
			Node2D node2D = TowerDefenseManager.GetCharacterNode();
			towerDefenseEffectSpriteOnce.GlobalPosition = character.GetLogicalGlobalPosition(character.sprite);
			towerDefenseEffectSpriteOnce.Scale = character.Scale * character.transformPoint.Scale;
			node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.ZIndex -= 6;
		}
		character.isExplode = flag;
		character.isSmash = isSmash;
		DestroyComponent destroyComponent = character.destroyComponent;
		if (destroyComponent != null && !destroyComponent.IsReleased)
		{
			character.destroyComponent.isRemoteDestroy = true;
			character.Destroy();
			return;
		}
		character.skipDestroySet = true;
		if (!character.IsQueuedForDeletion())
		{
			character.QueueFree();
		}
	}

	internal void DetachSyncCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && character.syncId >= 0)
		{
			_syncCharacters.Remove(character.syncId);
			_battleNetworkHost?.Entities.UnregisterCharacter(character.syncId);
			RemoveZombieSyncState(character.syncId);
			character.OnDestroy -= OnSyncCharacterDestroy;
		}
	}

	private async void SyncCharacterInitTimer(int syncIdVal)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!_syncCharacters.ContainsKey(syncIdVal))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = _syncCharacters[syncIdVal];
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
		if (towerDefenseCharacter2 == null || towerDefenseCharacter2.isDestroy)
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter2.sprite))
		{
			await ToSignal(towerDefenseCharacter2, Node.SignalName.Ready);
		}
		if (!_syncCharacters.ContainsKey(syncIdVal))
		{
			return;
		}
		towerDefenseCharacter = _syncCharacters[syncIdVal];
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2 != null && !towerDefenseCharacter2.isDestroy)
			{
				_battleNetworkHost?.CharacterState?.SendInitialState(syncIdVal);
			}
		}
	}

	private async void SyncCharacterPositionTimer(int syncIdVal)
	{
		await ToSignal(GetTree().CreateTimer(2.0), SceneTreeTimer.SignalName.Timeout);
		if (!_syncCharacters.ContainsKey(syncIdVal))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = _syncCharacters[syncIdVal];
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			if (towerDefenseCharacter2 != null && !towerDefenseCharacter2.isDestroy && !(towerDefenseCharacter2 is TowerDefenseZombie))
			{
				_battleNetworkHost?.CharacterState?.SendPosition(syncIdVal);
			}
		}
	}

	private void OnSyncCharacterDestroy(TowerDefenseCharacter character)
	{
		if (character.syncId >= 0 && _syncCharacters.TryGetValue(character.syncId, out var value) && value == character)
		{
			RemoveCharacterSyncState(character.syncId);
			RemoveZombieSyncState(character.syncId);
			if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				_battleNetworkHost?.CharacterState?.SendDestroy(character);
			}
		}
	}

	public void CleanupCharacterCell(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(character.gridPos);
		if (GodotObject.IsInstanceValid(mapCell) && mapCell.characterList.Contains(character))
		{
			character.OnDestroy -= mapCell.CharacterDestroy;
			mapCell.CharacterDestroy(character);
		}
		if (!GodotObject.IsInstanceValid(character.config) || !(character.config is TowerDefensePlantConfig towerDefensePlantConfig))
		{
			return;
		}
		foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
		{
			TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(character.gridPos + item);
			if (GodotObject.IsInstanceValid(mapCell2) && mapCell2 != mapCell && mapCell2.characterList.Contains(character))
			{
				character.OnDestroy -= mapCell2.CharacterDestroy;
				mapCell2.CharacterDestroy(character);
			}
		}
	}

	private void ApplyNetworkPauseUi()
	{
		if (!_isNetworkPaused)
		{
			_isNetworkPaused = true;
			AudioManager.Instance.AudioPlay("Pause", AudioManagerEnum.TYPE.SFX, 0.0, once: true, pauseAlive: true);
			DialogManager.Instance.DialogCreate("BattlePause");
			if (GodotObject.IsInstanceValid(buttonPause))
			{
				buttonPause.ButtonPressed = true;
			}
		}
	}

	private void ApplyNetworkResumeUi()
	{
		if (!_isNetworkPaused)
		{
			return;
		}
		_isNetworkPaused = false;
		CanvasLayer canvasLayer = (CanvasLayer)(GodotObject)DialogManager.Instance.Get("_dialogLayer");
		if (GodotObject.IsInstanceValid(canvasLayer))
		{
			foreach (Node child in canvasLayer.GetChildren())
			{
				if (child is CommandConsole commandConsole)
				{
					commandConsole.Close();
					return;
				}
			}
		}
		if (GodotObject.IsInstanceValid(buttonPause))
		{
			buttonPause.ButtonPressed = false;
		}
	}

	public void ApplyNetworkPauseFromSession(bool paused)
	{
		if (paused)
		{
			ApplyNetworkPauseUi();
		}
		else
		{
			ApplyNetworkResumeUi();
		}
	}

	private RemoteCursor GetOrCreateRemoteCursor(string userId)
	{
		if (_remoteCursors.ContainsKey(userId))
		{
			RemoteCursor remoteCursor = _remoteCursors[userId];
			if (GodotObject.IsInstanceValid(remoteCursor))
			{
				return remoteCursor;
			}
			_remoteCursors.Remove(userId);
		}
		CanvasLayer nodeOrNull = GetNodeOrNull<CanvasLayer>("GUITop");
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			return null;
		}
		RemoteCursor remoteCursor2 = GD.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/RemoteCursor.tscn").Instantiate<RemoteCursor>(PackedScene.GenEditState.Disabled);
		nodeOrNull.AddChild(remoteCursor2, forceReadableName: false, InternalMode.Disabled);
		int num = MultiPlayerManager.Instance.BattleAdmittedPeers.IndexOf(userId);
		if (num < 0)
		{
			num = _remoteCursors.Count;
		}
		string peerName = MultiPlayerManager.Instance.GetPeerName(userId);
		remoteCursor2.SetPlayerInfo(num, peerName);
		_remoteCursors[userId] = remoteCursor2;
		return remoteCursor2;
	}

	internal void RemoveRemoteCursor(string userId)
	{
		if (_remoteCursors.ContainsKey(userId))
		{
			RemoteCursor remoteCursor = _remoteCursors[userId];
			_remoteCursors.Remove(userId);
			if (GodotObject.IsInstanceValid(remoteCursor))
			{
				remoteCursor.QueueFree();
			}
		}
	}

	internal void ApplyRemoteCursorPosition(string userId, Vector2 position)
	{
		RemoteCursor orCreateRemoteCursor = GetOrCreateRemoteCursor(userId);
		if (GodotObject.IsInstanceValid(orCreateRemoteCursor))
		{
			orCreateRemoteCursor.UpdatePosition(position.X, position.Y);
		}
	}

	internal void ApplyRemoteCursorPick(string userId, string pickType, string pickName)
	{
		RemoteCursor orCreateRemoteCursor = GetOrCreateRemoteCursor(userId);
		if (GodotObject.IsInstanceValid(orCreateRemoteCursor))
		{
			orCreateRemoteCursor.UpdatePick(pickType, pickName);
		}
	}

	internal void OnChooseReady(Dictionary data)
	{
		string text = data.GetValueOrDefault("user_id", "").AsString();
		if (!MultiPlayerManager.Instance.IsBattleParticipant(text) || _chooseReadyPeers.Contains(text))
		{
			return;
		}
		_chooseReadyPeers.Add(text);
		UpdateChooseWaitLabel();
		if (!MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		if (_chooseReadyPeers.Contains(MultiPlayerManager.Instance.peerId))
		{
			bool flag = true;
			foreach (Variant battleAdmittedPeer in MultiPlayerManager.Instance.BattleAdmittedPeers)
			{
				if (!_chooseReadyPeers.Contains(battleAdmittedPeer.AsString()))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				MultiPlayerManager.Instance.SendChooseOver();
				DoChooseOver();
			}
		}
		else if (_chooseReadyPeers.Count >= MultiPlayerManager.Instance.BattleAdmittedPeers.Count)
		{
			MultiPlayerManager.Instance.SendChooseOver();
			DoChooseOver();
		}
	}

	internal void OnChooseOver()
	{
		if (!_chooseReadyPeers.Contains(MultiPlayerManager.Instance.peerId))
		{
			_chooseReadyPeers.Add(MultiPlayerManager.Instance.peerId);
		}
		DoChooseOver();
	}

	private void DoChooseOver()
	{
		_chooseOverReceived = true;
		EmitViewBack();
	}

	private void ShowChooseWaitLabel()
	{
		CanvasLayer nodeOrNull = GetNodeOrNull<CanvasLayer>("GUITop");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			_chooseWaitLabel = new RichTextLabel();
			_chooseWaitLabel.BbcodeEnabled = true;
			_chooseWaitLabel.FitContent = true;
			_chooseWaitLabel.ZIndex = 3000;
			_chooseWaitLabel.ZAsRelative = false;
			_chooseWaitLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
			_chooseWaitLabel.AddThemeFontSizeOverride("normal_font_size", 22);
			_chooseWaitLabel.Position = new Vector2(GetViewport().GetVisibleRect().Size.X / 2f - 150f, 80f);
			_chooseWaitLabel.Size = new Vector2(300f, 200f);
			nodeOrNull.AddChild(_chooseWaitLabel, forceReadableName: false, InternalMode.Disabled);
			UpdateChooseWaitLabel();
		}
	}

	private void HideChooseWaitLabel()
	{
		if (GodotObject.IsInstanceValid(_chooseWaitLabel))
		{
			_chooseWaitLabel.QueueFree();
		}
		_chooseWaitLabel = null;
	}

	private void UpdateChooseWaitLabel()
	{
		if (!GodotObject.IsInstanceValid(_chooseWaitLabel))
		{
			return;
		}
		string text = "[center]";
		bool flag = false;
		for (int i = 0; i < MultiPlayerManager.Instance.BattleAdmittedPeers.Count; i++)
		{
			string text2 = MultiPlayerManager.Instance.BattleAdmittedPeers[i].AsString();
			if (!_chooseReadyPeers.Contains(text2))
			{
				Color color = PlayerColors[i % PlayerColors.Length];
				string value = color.ToHtml(includeAlpha: false);
				string peerName = MultiPlayerManager.Instance.GetPeerName(text2);
				text += $"[color=#{value}]等待{peerName}选卡[/color]\n";
				flag = true;
			}
		}
		if (!flag)
		{
			text += "所有玩家已就绪";
		}
		text += "[/center]";
		_chooseWaitLabel.Text = text;
	}

	public void RejectLevelConfiguration(string reason)
	{
		if (_levelConfigurationRejected)
		{
			return;
		}
		_levelConfigurationRejected = true;
		isGameRunning = false;
		GD.PushWarning("[LevelConfiguration] " + reason);
		Callable.From(delegate
		{
			if (IsInsideTree())
			{
				AbortBattleGraphInitialization();
				SceneManager.Instance?.CompleteSceneLoading();
				DialogBoxBase dialogBoxBase = DialogManager.Instance.DialogCreate("DialogBoxTips");
				dialogBoxBase.Set("text", "关卡配置不可用，未记录通关或失败。\n" + reason.Replace("[", "[lb]"));
				dialogBoxBase.OnClose += ReturnFromRejectedLevel;
			}
		}).CallDeferred();
	}

	private void ReturnFromRejectedLevel()
	{
		string text;
		switch (Global.Instance.enterLevelMode)
		{
		case "ModLevel":
			text = "LevelChoose";
			break;
		case "LevelChoose":
			if (Global.Instance.currentLevelChoose != "TryLevel")
			{
				text = "LevelChoose";
				break;
			}
			goto default;
		case "DiyLevel":
		case "LoadLevel":
		case "OnlineLevel":
			text = "LevelEditorStage";
			break;
		default:
			text = "MainMenu";
			break;
		}
		string scene = text;
		SceneManager.Instance.ChangeScene(scene);
	}

	private void PrepareSavedFeatures()
	{
		foreach (KeyValuePair<Variant, Variant> item in _restoringProgress.featureSave ?? new Dictionary())
		{
			Variant.Type variantType = item.Key.VariantType;
			if ((variantType != Variant.Type.String && variantType != Variant.Type.StringName) || 1 == 0)
			{
				continue;
			}
			StringName name = item.Key.AsStringName();
			if (featureDictionary.ContainsKey(name))
			{
				continue;
			}
			_restoringProgress.RestoreReport.Try("Feature", delegate
			{
				FeatureAddTransaction transaction = new FeatureAddTransaction();
				if (!TryAddFeature(name, GetConfiguredFeatureData(name), transaction, out var failureReason))
				{
					RollbackFeatureAddTransaction(transaction);
					_restoringProgress.RestoreReport.Record("Feature", failureReason);
				}
			});
		}
	}

	private void CompleteProgressRecovery()
	{
		if (_restoringProgress == null || _progressNoticeShown)
		{
			return;
		}
		_progressNoticeShown = true;
		GameSaveManager.ProgressRecoverySession progressRecovery = GameSaveManager.Instance.GetProgressRecovery(this);
		if (progressRecovery == null)
		{
			return;
		}
		if (!_restoringProgress.RestoreReport.HasIssues && !progressRecovery.UsedBackup && progressRecovery.BackupAvailable)
		{
			GameSaveManager.Instance.AcceptProgressRecovery(this);
			if (GodotObject.IsInstanceValid(_progressPause))
			{
				_progressPause.ProcessMode = ProcessModeEnum.Always;
			}
		}
		else
		{
			ShowProgressRecoveryNotice("");
		}
	}

	private void ShowProgressRecoveryNotice(string failure)
	{
		if (_battleShuttingDown || !IsInsideTree())
		{
			return;
		}
		GameSaveManager.ProgressRecoverySession session = GameSaveManager.Instance.GetProgressRecovery(this);
		if (session == null)
		{
			return;
		}
		DialogBoxChoose dialogBoxChoose = DialogManager.Instance.DialogCreate("DialogBoxChoose") as DialogBoxChoose;
		if (!GodotObject.IsInstanceValid(dialogBoxChoose))
		{
			GD.PushWarning("[ProgressLoad] 恢复提示无法创建，原始备份继续保留。");
			GameSaveManager.Instance.AcceptProgressRecovery(this);
			if (GodotObject.IsInstanceValid(_progressPause))
			{
				_progressPause.ProcessMode = ProcessModeEnum.Always;
			}
			return;
		}
		if (GodotObject.IsInstanceValid(_progressPause))
		{
			_progressPause.SetProcessInput(enable: false);
		}
		dialogBoxChoose.text = "[center]关卡进度已恢复[/center]\n" + (session.UsedBackup ? "主存档无法读取，已从备份续档。\n" : "") + (_restoringProgress.RestoreReport.HasIssues ? "部分内容无法恢复，已跳过或使用当前默认状态。\n" : "") + (session.BackupAvailable ? "可以继续游戏，或退出关卡并恢复备份。" : "备份未能建立，原存档暂不会被覆盖。可以继续游戏，或退出并保留原档。") + (string.IsNullOrEmpty(failure) ? "" : ("\n恢复备份失败，可重试：" + failure.Replace("[", "[lb]")));
		dialogBoxChoose.GetNode<BaseButton>("%TrueButton").Set("text", "继续游戏");
		dialogBoxChoose.GetNode<BaseButton>("%FalseButton").Set("text", session.BackupAvailable ? "退出并恢复" : "退出关卡");
		bool exit = false;
		string retryReason = null;
		dialogBoxChoose.OnChooseFalse += delegate
		{
			if (session.BackupAvailable && !GameSaveManager.Instance.TryRestoreProgressBackup(this, out var reason))
			{
				retryReason = reason;
			}
			else
			{
				GameSaveManager.Instance.DiscardProgressRecovery(this);
				exit = true;
			}
		};
		dialogBoxChoose.OnClose += delegate
		{
			if (!_battleShuttingDown)
			{
				if (retryReason != null)
				{
					Callable.From(delegate
					{
						ShowProgressRecoveryNotice(retryReason);
					}).CallDeferred();
				}
				else if (exit)
				{
					isGameRunning = false;
					SceneManager.Instance.ChangeScene(ProgressExitDestination());
				}
				else
				{
					GameSaveManager.Instance.AcceptProgressRecovery(this);
					if (GodotObject.IsInstanceValid(_progressPause) && !_progressPause.IsQueuedForDeletion())
					{
						_progressPause.CloseDialog();
					}
				}
			}
		};
	}

	private static string ProgressExitDestination()
	{
		switch (Global.Instance.enterLevelMode)
		{
		case "ModLevel":
			return "LevelChoose";
		case "LevelChoose":
			if (Global.Instance.currentLevelChoose != "TryLevel")
			{
				return "LevelChoose";
			}
			break;
		case "DiyLevel":
		case "LoadLevel":
		case "OnlineLevel":
			return "LevelEditorStage";
		}
		return "MainMenu";
	}
}
