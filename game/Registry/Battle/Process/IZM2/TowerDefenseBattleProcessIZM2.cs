using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessIZM2 : TowerDefenseBattleProcess
{
	public TowerDefenseBattleFeatureWave waveFeature;

	public TowerDefenseBattleProcessIZM2Config config;

	public TowerDefenseBattleFeatureProgress progressFeature;

	public TowerDefenseBattleFeatureCamera cameraFeature;

	public TowerDefenseBattleFeatureSeedBank seedBankFeature;

	public TowerDefenseBattleFeaturePacketBank packetBankFeature;

	public TowerDefenseBattleFeatureMower mowerFeature;

	public TowerDefenseInGameLevelControl levelControl;

	private readonly List<TowerDefenseInGamePacketShow> _viewPacketShows = new List<TowerDefenseInGamePacketShow>();

	private readonly System.Collections.Generic.Dictionary<Node, Node.ProcessModeEnum> _pausedEntryCharacters = new System.Collections.Generic.Dictionary<Node, Node.ProcessModeEnum>();

	private bool _viewMapRunning;

	private bool _entryBroadcastActive;

	private bool _viewBroadcastActive;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleProcessIZM2Config();
		config.Init(data);
	}

	public override void OnReady()
	{
	}

	public override void PhysicsProcess(double delta)
	{
		if ((Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || !GodotObject.IsInstanceValid(levelControl) || levelControl.awardCreate || waveFeature == null)
		{
			return;
		}
		waveFeature.WavePhysicsProcess(delta);
		if (levelControl.awardCreate)
		{
			return;
		}
		if (levelControl.hasSpawn)
		{
			levelControl.hasSpawn = false;
		}
		else
		{
			if (!waveFeature.IsFinalWaveSpawnSettled || !CanFinish() || !CheckFinal())
			{
				return;
			}
			if (!waveFeature.isSurvival)
			{
				levelControl.AwardCreate(levelControl.awardPos);
			}
			else if (waveFeature.survivalRunner.config.roundLimit == -1 || waveFeature.survivalRunner.config.roundLimit > waveFeature.survivalRunner.roundNum + 1)
			{
				Action persistProgress = null;
				if (Global.Instance.enterLevelMode != "OnlineLevel" && !Global.IsMultiplayerMode)
				{
					persistProgress = delegate
					{
						GameSaveManager.Instance.SaveLevelProgress(control.levelConfig.name, control.ModLevelIdentity);
					};
				}
				SurvivalRoundProgressTransaction.AdvanceAndPersist(waveFeature.survivalRunner, persistProgress, waveFeature.survivalRunner.BeginRoundTransition);
				if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
				{
					MultiPlayerManager.Instance.SendGameEntry(waveFeature.survivalRunner.roundNum);
				}
				control.GameEntry();
				levelControl.awardCreate = true;
			}
			else
			{
				GameSaveManager.Instance.DeleteLevelProgress(control.levelConfig.name, control.ModLevelIdentity);
				levelControl.AwardCreate(levelControl.awardPos);
			}
		}
	}

	public override void InputProcess(InputEvent event_)
	{
		if (CommandManager.Instance.debug && Input.IsActionJustPressed("DebugWaveNext"))
		{
			waveFeature.NextWave();
		}
		if (control != null && control.isView && Input.IsAnythingPressed())
		{
			control.EmitViewBack();
		}
	}

	public override Task GameInit()
	{
		ResolveDependencies();
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		ResolveDependencies();
		return Task.CompletedTask;
	}

	private void ResolveDependencies()
	{
		levelControl = control.levelControl;
		waveFeature = GetFeature<TowerDefenseBattleFeatureWave>("Wave");
		progressFeature = GetFeature<TowerDefenseBattleFeatureProgress>("Progress");
		cameraFeature = GetFeature<TowerDefenseBattleFeatureCamera>("Camera");
		seedBankFeature = GetFeature<TowerDefenseBattleFeatureSeedBank>("SeedBank");
		packetBankFeature = GetFeature<TowerDefenseBattleFeaturePacketBank>("PacketBank");
		mowerFeature = GetFeature<TowerDefenseBattleFeatureMower>("Mower");
	}

	public override async Task GameEntry()
	{
		if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control))
		{
			return;
		}
		bool mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		bool firstEntry = !control.isInit;
		if (control.hasProgress)
		{
			PauseEntryCharacters();
		}
		if (firstEntry && GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
		{
			seedBankFeature.seedBank.ReadyPackets();
		}
		if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control))
		{
			RestoreEntryCharacters();
			return;
		}
		if (control.hasProgress)
		{
			if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
			{
				seedBankFeature.seedBank.packetSlotContainer.Visible = true;
			}
			if (mobilePreset)
			{
				control.uiTopAnimationPlayer.Play("MobileEnter");
			}
			else
			{
				control.uiTopAnimationPlayer.Play("Enter");
			}
			if (GodotObject.IsInstanceValid(packetBankFeature?.packetBank))
			{
				packetBankFeature.packetBank.Visible = false;
			}
			return;
		}
		if (firstEntry)
		{
			BroadCastConfig broadCastConfig = new BroadCastConfig();
			broadCastConfig.broadCastString = "更多的僵尸要来了！";
			if (GodotObject.IsInstanceValid(BroadCastManager.Instance))
			{
				BroadCastManager.Instance.BroadCastAdd(broadCastConfig);
				_entryBroadcastActive = true;
			}
			try
			{
				if (!(await WaitForTimerOrLifetime(config.entryBroadcastDuration)))
				{
					return;
				}
			}
			finally
			{
				ClearEntryBroadcast();
			}
			if (!GodotObject.IsInstanceValid(control))
			{
				return;
			}
			if (mobilePreset)
			{
				control.uiTopAnimationPlayer.Play("MobileExit");
			}
			else
			{
				control.uiTopAnimationPlayer.Play("Exit");
			}
			if (_pausedEntryCharacters.Count == 0)
			{
				PauseEntryCharacters();
			}
			if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank) && GodotObject.IsInstanceValid(packetBankFeature?.packetBank) && !packetBankFeature.skipPacketChoose)
			{
				seedBankFeature.seedBank.Prepare();
			}
		}
		if (!GodotObject.IsInstanceValid(waveFeature) || !GodotObject.IsInstanceValid(levelControl) || !GodotObject.IsInstanceValid(cameraFeature?.cameraControl?.camera) || !GodotObject.IsInstanceValid(cameraFeature.cameraControl.cameraRightViewMarker) || !GodotObject.IsInstanceValid(cameraFeature.cameraControl.cameraPreViewMarker) || !GodotObject.IsInstanceValid(cameraFeature.cameraControl.cameraBeginMarker))
		{
			RestoreEntryCharacters();
			return;
		}
		if (GodotObject.IsInstanceValid(packetBankFeature?.packetBank))
		{
			packetBankFeature.packetBank.Visible = true;
			levelControl.worldEntryLabel.Visible = false;
		}
		waveFeature.Refresh();
		waveFeature.ShowCharacter();
		levelControl.worldEntryLabel.Visible = true;
		if (waveFeature.isSurvival)
		{
			levelControl.survivleLabel.Text = $"{waveFeature.survivalRunner.roundNum}轮完成";
			levelControl.survivleLabel.Visible = true;
		}
		RunLifetimeTask(HideEntryLabelsAfterDelayAsync, "HideEntryLabelsAfterDelayAsync");
		Tween tween = cameraFeature.cameraControl.camera.CreateTween();
		tween.SetEase(Tween.EaseType.InOut);
		tween.SetTrans(Tween.TransitionType.Quad);
		tween.TweenProperty(cameraFeature.cameraControl.camera, "global_position:x", cameraFeature.cameraControl.cameraRightViewMarker.GlobalPosition.X, config.cameraTravelDuration);
		if (!(await WaitForTweenOrLifetime(tween)))
		{
			return;
		}
		TaskCompletionSource<bool> tcs;
		if (GodotObject.IsInstanceValid(packetBankFeature?.packetBank) && !packetBankFeature.skipPacketChoose)
		{
			tween = cameraFeature.cameraControl.camera.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Quart);
			tween.TweenProperty(cameraFeature.cameraControl.camera, "global_position:x", cameraFeature.cameraControl.cameraPreViewMarker.GlobalPosition.X, config.cameraTravelDuration);
			if (!(await WaitForTweenOrLifetime(tween)))
			{
				return;
			}
			control.buttonPause.Visible = true;
			if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
			{
				seedBankFeature.seedBank.packetSlotContainer.Visible = true;
			}
			if (mobilePreset)
			{
				packetBankFeature.packetBank.packetBankAnimationPlayer.Play("MobileEnter");
				control.uiTopAnimationPlayer.Play("MobileEnter");
			}
			else
			{
				packetBankFeature.packetBank.packetBankAnimationPlayer.Play("Enter");
				control.uiTopAnimationPlayer.Play("Enter");
			}
			TowerDefenseBattleFeaturePacketBank chooseFeature = packetBankFeature;
			tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			chooseFeature.OnChooseOver += ChooseOverHandler;
			try
			{
				if (!(await WaitForTaskOrLifetime(tcs.Task)))
				{
					return;
				}
			}
			finally
			{
				if (GodotObject.IsInstanceValid(chooseFeature))
				{
					chooseFeature.OnChooseOver -= ChooseOverHandler;
				}
			}
			if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(packetBankFeature?.packetBank))
			{
				return;
			}
			if (mobilePreset)
			{
				packetBankFeature.packetBank.packetBankAnimationPlayer.Play("MobileExit");
			}
			else
			{
				packetBankFeature.packetBank.packetBankAnimationPlayer.Play("Exit");
			}
			if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
			{
				seedBankFeature.seedBank.ReadyPackets();
			}
			if (!(await WaitForTimerOrLifetime(config.packetBankExitDelay)))
			{
				return;
			}
		}
		else
		{
			if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
			{
				seedBankFeature.seedBank.ReadyPackets();
			}
			if (!(await WaitForTimerOrLifetime(config.packetBankExitDelay)))
			{
				return;
			}
		}
		tween = cameraFeature.cameraControl.camera.CreateTween();
		tween.SetEase(Tween.EaseType.InOut);
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(cameraFeature.cameraControl.camera, "global_position:x", cameraFeature.cameraControl.cameraBeginMarker.GlobalPosition.X, config.cameraTravelDuration);
		if (!(await WaitForTweenOrLifetime(tween)))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
		{
			seedBankFeature.seedBank.packetSlotContainer.Visible = true;
		}
		if (GodotObject.IsInstanceValid(packetBankFeature))
		{
			if (packetBankFeature.skipPacketChoose)
			{
				if (mobilePreset)
				{
					control.uiTopAnimationPlayer.Play("MobileEnter");
				}
				else
				{
					control.uiTopAnimationPlayer.Play("Enter");
				}
			}
		}
		else if (mobilePreset)
		{
			control.uiTopAnimationPlayer.Play("MobileEnter");
		}
		else
		{
			control.uiTopAnimationPlayer.Play("Enter");
		}
		if (firstEntry)
		{
			waveFeature.SurvivalReady();
			RestoreEntryCharacters();
		}
		void ChooseOverHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public override async Task GameReady()
	{
		if (!control.hasProgress && config.mowerUse && (!waveFeature.isSurvival || control.isInit))
		{
			mowerFeature.MowerInit();
		}
		mowerFeature.IZM2Init();
		waveFeature.ClearShowCharacter();
		progressFeature.SetDifficultVisible(visible: true);
		if (waveFeature.isSurvival)
		{
			progressFeature.SetSurvivalVisible(visible: true);
		}
		progressFeature.SetLevelNameVisible(visible: true);
		if (Global.Instance.enterLevelMode == "DailyLevel" || Global.Instance.enterLevelMode == "OnlineLevel" || Global.Instance.enterLevelMode == "LevelTest" || Global.Instance.enterLevelMode == "DiyLevel")
		{
			progressFeature.SetDifficultVisible(visible: false);
		}
		if (control.hasProgress)
		{
			waveFeature.readySetPlantOver = true;
			return;
		}
		if (seedBankFeature != null)
		{
			foreach (Node child in seedBankFeature.seedBank.packetContainer.GetChildren())
			{
				if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow)
				{
					towerDefenseInGamePacketShow.onlyDraw = true;
				}
			}
		}
		if (await WaitForTaskOrLifetime(control.ReadySetPlantPlay()))
		{
			waveFeature.readySetPlantOver = true;
		}
	}

	public override Task GameStart()
	{
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		RestoreEntryCharacters();
		return Task.CompletedTask;
	}

	public override void GameFail(TowerDefenseCharacter enterCharacter)
	{
		control.ZombieWonLevelFail(playAnime: false);
	}

	public override void ZombieEnterHouse(TowerDefenseCharacter character)
	{
		if (!CommandManager.Instance.debug || !CommandManager.Instance.debugNoLose)
		{
			return;
		}
		character.CreateTween().TweenProperty(character.sprite, "meshColor:a", 0.0, config.debugEnterHouseFadeDuration).Finished += delegate
		{
			if (GodotObject.IsInstanceValid(character))
			{
				character.Destroy();
			}
		};
	}

	public override void ViewMap()
	{
		if (!_viewMapRunning && base.IsLifetimeActive)
		{
			_viewMapRunning = true;
			RunLifetimeTask(ViewMapAsync, "ViewMap");
		}
	}

	private async Task ViewMapAsync()
	{
		_ = 2;
		try
		{
			bool mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
			PlayPacketBankAnimation(mobilePreset ? "MobileExit" : "Exit");
			SetPacketDrawOnly();
			TowerDefenseCameraControl cameraControl = cameraFeature?.cameraControl;
			if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(cameraControl?.camera) || !GodotObject.IsInstanceValid(cameraControl.cameraBeginMarker))
			{
				return;
			}
			Tween tween = cameraControl.camera.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Sine);
			tween.TweenProperty(cameraControl.camera, "global_position:x", cameraControl.cameraBeginMarker.GlobalPosition.X, config.cameraTravelDuration);
			control.isView = true;
			if (!(await WaitForTweenOrLifetime(tween)))
			{
				return;
			}
			if (GodotObject.IsInstanceValid(BroadCastManager.Instance))
			{
				BroadCastConfig broadCastConfig = new BroadCastConfig
				{
					broadCastString = "INGAME_VIEW_BACK"
				};
				BroadCastManager.Instance.BroadCastAdd(broadCastConfig);
				_viewBroadcastActive = true;
			}
			TowerDefenseControlNew viewControl = control;
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			viewControl.OnViewBack += ViewBackHandler;
			try
			{
				if (!(await WaitForTaskOrLifetime(tcs.Task)))
				{
					return;
				}
			}
			finally
			{
				if (GodotObject.IsInstanceValid(viewControl))
				{
					viewControl.OnViewBack -= ViewBackHandler;
				}
			}
			if (base.IsLifetimeActive && GodotObject.IsInstanceValid(cameraControl?.camera) && GodotObject.IsInstanceValid(cameraControl.cameraPreViewMarker))
			{
				RestoreViewMapState();
				tween = cameraControl.camera.CreateTween();
				tween.SetEase(Tween.EaseType.InOut);
				tween.SetTrans(Tween.TransitionType.Sine);
				tween.TweenProperty(cameraControl.camera, "global_position:x", cameraControl.cameraPreViewMarker.GlobalPosition.X, config.cameraTravelDuration);
				if (await WaitForTweenOrLifetime(tween))
				{
					PlayPacketBankAnimation(mobilePreset ? "MobileEnter" : "Enter");
				}
			}
			void ViewBackHandler()
			{
				tcs.TrySetResult(result: true);
			}
		}
		finally
		{
			RestoreViewMapState();
			_viewMapRunning = false;
		}
	}

	private async Task HideEntryLabelsAfterDelayAsync()
	{
		if (await WaitForTimerOrLifetime(config.entryLabelDuration) && GodotObject.IsInstanceValid(levelControl))
		{
			levelControl.worldEntryLabel.Visible = false;
			if (GodotObject.IsInstanceValid(GetLevelControl()) && GodotObject.IsInstanceValid(waveFeature) && waveFeature.isSurvival)
			{
				levelControl.survivleLabel.Visible = false;
			}
		}
	}

	private void ClearEntryBroadcast()
	{
		if (_entryBroadcastActive && GodotObject.IsInstanceValid(BroadCastManager.Instance))
		{
			BroadCastManager.Instance.BraodCastClear();
		}
		_entryBroadcastActive = false;
	}

	private void SetPacketDrawOnly()
	{
		_viewPacketShows.Clear();
		if (!GodotObject.IsInstanceValid(seedBankFeature?.seedBank?.packetContainer))
		{
			return;
		}
		foreach (Node child in seedBankFeature.seedBank.packetContainer.GetChildren())
		{
			if (child is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) && !towerDefenseInGamePacketShow.onlyDraw)
			{
				towerDefenseInGamePacketShow.onlyDraw = true;
				_viewPacketShows.Add(towerDefenseInGamePacketShow);
			}
		}
	}

	private void RestoreViewMapState()
	{
		foreach (TowerDefenseInGamePacketShow viewPacketShow in _viewPacketShows)
		{
			if (GodotObject.IsInstanceValid(viewPacketShow))
			{
				viewPacketShow.onlyDraw = false;
			}
		}
		_viewPacketShows.Clear();
		if (GodotObject.IsInstanceValid(control))
		{
			control.isView = false;
		}
		if (_viewBroadcastActive && GodotObject.IsInstanceValid(BroadCastManager.Instance))
		{
			BroadCastManager.Instance.BraodCastClear();
		}
		_viewBroadcastActive = false;
	}

	private void PlayPacketBankAnimation(StringName animationName)
	{
		if (GodotObject.IsInstanceValid(packetBankFeature?.packetBank?.packetBankAnimationPlayer))
		{
			packetBankFeature.packetBank.packetBankAnimationPlayer.Play(animationName);
		}
	}

	public override bool CheckFinal()
	{
		if (GodotObject.IsInstanceValid(control) && control.HasPendingBattleOperations)
		{
			return false;
		}
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
		if (!GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry) || towerDefenseBattleCharacterRegistry.GetVaseCount() > 0)
		{
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = null;
		List<TowerDefenseCharacter> cleanCharactersList = towerDefenseBattleCharacterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = cleanCharactersList[i];
			if (IsIZM2ObjectiveTarget(towerDefenseCharacter2))
			{
				towerDefenseCharacter = towerDefenseCharacter2;
				break;
			}
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			levelControl.awardPos = towerDefenseCharacter.GetLogicalGlobalPosition();
			return false;
		}
		foreach (Variant item in TowerDefenseManager.Instance.GetCampTarget(TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE))
		{
			((TowerDefenseCharacter)(GodotObject)item).Hurt(1000000000000.0);
		}
		return true;
	}

	private static bool IsIZM2ObjectiveTarget(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && character is TowerDefenseCrater towerDefenseCrater)
		{
			if (towerDefenseCrater.HasPendingRevival)
			{
				return towerDefenseCrater.RevivalCamp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
			}
			return false;
		}
		if (!GodotObject.IsInstanceValid(character) || character.die || character.nearDie || character.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE || character is TowerDefenseCrater || character is TowerDefenseItem || character is TowerDefenseGravestone)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(character.instance) && character.instance.die)
		{
			return false;
		}
		if (character is TowerDefensePlant { config: TowerDefensePlantConfig towerDefensePlantConfig })
		{
			return !towerDefensePlantConfig.izm2Fliter;
		}
		return true;
	}

	public override void Finish()
	{
		if (!CanFinish() || levelControl.awardCreate)
		{
			return;
		}
		ViewManager.Instance.FullScreenColorBlink(Colors.White, 0.2, rise: false);
		AudioManager.Instance.AudioPlay("WaveHuge");
		Godot.Collections.Array campTarget = TowerDefenseManager.Instance.GetCampTarget(TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE);
		if (campTarget.Count > 0)
		{
			levelControl.awardPos = ((TowerDefenseCharacter)(GodotObject)campTarget[0]).GetLogicalGlobalPosition();
		}
		foreach (Variant item in campTarget)
		{
			((TowerDefenseCharacter)(GodotObject)item).Destroy();
		}
		levelControl.AwardCreate(levelControl.awardPos);
	}

	public override bool CanFinish()
	{
		if (base.CanFinish() && GodotObject.IsInstanceValid(waveFeature))
		{
			return !waveFeature.HasPendingSpawnOperations;
		}
		return false;
	}

	private void PauseEntryCharacters()
	{
		_pausedEntryCharacters.Clear();
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return;
		}
		foreach (Node item in tree.GetNodesInGroup("Character"))
		{
			if (GodotObject.IsInstanceValid(item) && !_pausedEntryCharacters.ContainsKey(item))
			{
				_pausedEntryCharacters[item] = item.ProcessMode;
				item.ProcessMode = Node.ProcessModeEnum.Disabled;
			}
		}
	}

	private void RestoreEntryCharacters()
	{
		foreach (var (node2, processMode) in _pausedEntryCharacters)
		{
			if (GodotObject.IsInstanceValid(node2))
			{
				node2.ProcessMode = processMode;
			}
		}
		_pausedEntryCharacters.Clear();
	}

	public override void Destroy()
	{
		base.Destroy();
		ClearEntryBroadcast();
		RestoreEntryCharacters();
		if (_viewMapRunning || _viewBroadcastActive || _viewPacketShows.Count > 0)
		{
			RestoreViewMapState();
		}
		_viewMapRunning = false;
		if (GodotObject.IsInstanceValid(levelControl?.worldEntryLabel))
		{
			levelControl.worldEntryLabel.Visible = false;
		}
		if (GodotObject.IsInstanceValid(levelControl?.survivleLabel))
		{
			levelControl.survivleLabel.Visible = false;
		}
		waveFeature = null;
		config = null;
		progressFeature = null;
		cameraFeature = null;
		seedBankFeature = null;
		packetBankFeature = null;
		mowerFeature = null;
		levelControl = null;
	}
}
