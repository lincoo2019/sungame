using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessWave : TowerDefenseBattleProcess
{
	public TowerDefenseBattleFeatureWave waveFeature;

	public TowerDefenseBattleProcessWaveConfig config;

	public TowerDefenseBattleFeatureSun sunFeature;

	public TowerDefenseBattleFeatureMap mapFeature;

	public TowerDefenseBattleFeatureProgress progressFeature;

	public TowerDefenseBattleFeatureCamera cameraFeature;

	public TowerDefenseBattleFeatureSeedBank seedBankFeature;

	public TowerDefenseBattleFeaturePacketBank packetBankFeature;

	public TowerDefenseBattleFeatureMower mowerFeature;

	public TowerDefenseInGameLevelControl levelControl;

	private readonly List<TowerDefenseInGamePacketShow> _viewPacketShows = new List<TowerDefenseInGamePacketShow>();

	private readonly System.Collections.Generic.Dictionary<Node, Node.ProcessModeEnum> _pausedEntryCharacters = new System.Collections.Generic.Dictionary<Node, Node.ProcessModeEnum>();

	private bool _gameFailRunning;

	private bool _viewMapRunning;

	private bool _entryBroadcastActive;

	private bool _viewBroadcastActive;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleProcessWaveConfig();
		config.Init(data);
	}

	public override void OnReady()
	{
	}

	public override void PhysicsProcess(double delta)
	{
		if ((Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || !GodotObject.IsInstanceValid(levelControl) || levelControl.awardCreate)
		{
			return;
		}
		waveFeature.WavePhysicsProcess(delta);
		if (levelControl.hasSpawn)
		{
			levelControl.hasSpawn = false;
		}
		else
		{
			if (!waveFeature.waveFinal || !waveFeature.spawnOver || !CanFinish() || !CheckFinal())
			{
				return;
			}
			if (!waveFeature.isSurvival)
			{
				CreateLevelClearReward(levelControl.awardPos);
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
				DropItemRegistry.Reset();
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
		levelControl = control.levelControl;
		waveFeature = GetFeature("Wave") as TowerDefenseBattleFeatureWave;
		sunFeature = GetFeature("Sun") as TowerDefenseBattleFeatureSun;
		mapFeature = GetFeature("Map") as TowerDefenseBattleFeatureMap;
		progressFeature = GetFeature("Progress") as TowerDefenseBattleFeatureProgress;
		cameraFeature = GetFeature("Camera") as TowerDefenseBattleFeatureCamera;
		seedBankFeature = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		packetBankFeature = GetFeature("PacketBank") as TowerDefenseBattleFeaturePacketBank;
		mowerFeature = GetFeature("Mower") as TowerDefenseBattleFeatureMower;
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		levelControl = control.levelControl;
		waveFeature = GetFeature("Wave") as TowerDefenseBattleFeatureWave;
		sunFeature = GetFeature("Sun") as TowerDefenseBattleFeatureSun;
		mapFeature = GetFeature("Map") as TowerDefenseBattleFeatureMap;
		progressFeature = GetFeature("Progress") as TowerDefenseBattleFeatureProgress;
		cameraFeature = GetFeature("Camera") as TowerDefenseBattleFeatureCamera;
		seedBankFeature = GetFeature("SeedBank") as TowerDefenseBattleFeatureSeedBank;
		packetBankFeature = GetFeature("PacketBank") as TowerDefenseBattleFeaturePacketBank;
		mowerFeature = GetFeature("Mower") as TowerDefenseBattleFeatureMower;
		return Task.CompletedTask;
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
			if (seedBankFeature != null)
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
			if (packetBankFeature != null)
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
			if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control))
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
			if (seedBankFeature != null && packetBankFeature != null && !packetBankFeature.skipPacketChoose)
			{
				seedBankFeature.seedBank.Prepare();
			}
		}
		if (packetBankFeature != null)
		{
			packetBankFeature.packetBank.Visible = true;
			levelControl.worldEntryLabel.Visible = false;
			if (sunFeature != null)
			{
				sunFeature.isRunning = false;
			}
		}
		if (waveFeature == null)
		{
			GD.PushError("[GameEntry] waveFeature 为 null，可能 GameInit/GameInitFromProgress 未被正确调用");
			return;
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
		if (packetBankFeature != null && !packetBankFeature.skipPacketChoose)
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
			if (seedBankFeature != null)
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
			if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control))
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
			if (seedBankFeature != null)
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
			if (seedBankFeature != null)
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
		if (seedBankFeature != null)
		{
			seedBankFeature.seedBank.packetSlotContainer.Visible = true;
		}
		if (packetBankFeature != null)
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
		if (waveFeature == null)
		{
			GD.PushError("[GameReady] waveFeature 为 null，可能 GameInit/GameInitFromProgress 未被正确调用");
			return;
		}
		if (!control.hasProgress && config.mowerUse && (!waveFeature.isSurvival || control.isInit))
		{
			mowerFeature?.MowerInit();
		}
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
		if (!TowerDefenseManager.Instance.IsIZMMode())
		{
			List<int> list = new List<int>();
			string text = "Pitchfork";
			int num = GameSaveManager.Instance.GetFeatureValue("Pitchfork");
			int featureValue = GameSaveManager.Instance.GetFeatureValue("PitchforkSun");
			if (featureValue > 0)
			{
				text = "PitchforkSun";
				num = featureValue;
			}
			if (num > 0)
			{
				for (int i = 0; i < mapFeature.lineUse.Count; i++)
				{
					if (mapFeature.lineUse[i] && !TowerDefenseManager.MapLineHasType(i, TowerDefenseEnum.PLANTGRIDTYPE.WATER))
					{
						list.Add(i);
					}
				}
				if (list.Count > 0)
				{
					GameSaveManager.Instance.SetFeatureValue(text, num - 1);
					TowerDefensePacketConfig towerDefensePacketConfig = ((text == "Pitchfork") ? TowerDefenseManager.GetPacketConfig("ItemRake") : ((!(text == "PitchforkSun")) ? TowerDefenseManager.GetPacketConfig("ItemRake") : TowerDefenseManager.GetPacketConfig("ItemRakeSun")));
					waveFeature.savePitchforkLine = list.PickRandom();
					towerDefensePacketConfig.Plant(new Vector2I(5, waveFeature.savePitchforkLine), playAudio: false);
				}
			}
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
		if (!_gameFailRunning && base.IsLifetimeActive)
		{
			_gameFailRunning = true;
			RunLifetimeTask(() => GameFailAsync(enterCharacter), "GameFail");
		}
	}

	private async Task GameFailAsync(TowerDefenseCharacter enterCharacter)
	{
		try
		{
			foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
			{
				TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
				if (towerDefenseCharacter != enterCharacter)
				{
					towerDefenseCharacter.ProcessMode = Node.ProcessModeEnum.Disabled;
				}
			}
			if (packetBankFeature != null)
			{
				packetBankFeature.packetBank.Visible = false;
			}
			control.bankUILayer.Visible = false;
			bool flag = mapFeature != null && GodotObject.IsInstanceValid(mapFeature.currentMap);
			if (flag)
			{
				flag = !(await WaitForTaskOrLifetime(mapFeature.currentMap.EnterRoom(enterCharacter)));
			}
			if (!flag && base.IsLifetimeActive && GodotObject.IsInstanceValid(control))
			{
				if (GodotObject.IsInstanceValid(enterCharacter))
				{
					enterCharacter.ProcessMode = Node.ProcessModeEnum.Disabled;
				}
				control.ZombieWonLevelFail();
			}
		}
		finally
		{
			_gameFailRunning = false;
		}
	}

	public override void ZombieEnterHouse(TowerDefenseCharacter character)
	{
		if (CommandManager.Instance.debug && CommandManager.Instance.debugNoLose)
		{
			character.CreateTween().TweenProperty(character.sprite, "meshColor:a", 0.0, config.debugEnterHouseFadeDuration).Finished += delegate
			{
				if (GodotObject.IsInstanceValid(character))
				{
					character.Destroy();
				}
			};
		}
		else if (control != null)
		{
			control.GameFail(character);
		}
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
			Tween tween = cameraFeature.cameraControl.camera.CreateTween();
			tween.SetEase(Tween.EaseType.InOut);
			tween.SetTrans(Tween.TransitionType.Sine);
			tween.TweenProperty(cameraFeature.cameraControl.camera, "global_position:x", cameraFeature.cameraControl.cameraBeginMarker.GlobalPosition.X, config.cameraTravelDuration);
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
			if (base.IsLifetimeActive && GodotObject.IsInstanceValid(control))
			{
				RestoreViewMapState();
				tween = cameraFeature.cameraControl.camera.CreateTween();
				tween.SetEase(Tween.EaseType.InOut);
				tween.SetTrans(Tween.TransitionType.Sine);
				tween.TweenProperty(cameraFeature.cameraControl.camera, "global_position:x", cameraFeature.cameraControl.cameraPreViewMarker.GlobalPosition.X, config.cameraTravelDuration);
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

	public override bool CheckFinal()
	{
		if (GodotObject.IsInstanceValid(control) && control.HasPendingBattleOperations)
		{
			return false;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		try
		{
			TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
			if (!GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry))
			{
				return false;
			}
			if (towerDefenseBattleCharacterRegistry.GetVaseCount() > 0)
			{
				return false;
			}
			if (TryGetLivingHostileInvisibleZombie(out var target))
			{
				levelControl.awardPos = target.GetLogicalGlobalPosition();
				return false;
			}
			if (towerDefenseBattleCharacterRegistry.TryGetFinalWaveTarget(TowerDefenseEnum.CHARACTER_CAMP.PLANT, out var target2, out var hasPendingDestroyZombie))
			{
				levelControl.awardPos = target2.GetLogicalGlobalPosition();
				return false;
			}
			return !hasPendingDestroyZombie;
		}
		finally
		{
			TowerDefensePerfProfiler.End("processWave.checkFinal", startTicks);
		}
	}

	private static bool TryGetLivingHostileInvisibleZombie(out TowerDefenseZombie target)
	{
		target = null;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		foreach (Variant item in instance.GetZombie())
		{
			if (item.AsGodotObject() is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie) && towerDefenseZombie.inGame && towerDefenseZombie.invisible && towerDefenseZombie.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT && !towerDefenseZombie.die && !towerDefenseZombie.nearDie && !towerDefenseZombie.IsQueuedForDeletion() && (!GodotObject.IsInstanceValid(towerDefenseZombie.instance) || !towerDefenseZombie.instance.die))
			{
				target = towerDefenseZombie;
				return true;
			}
		}
		return false;
	}

	public override void Finish()
	{
		if (!CanFinish() || levelControl.awardCreate)
		{
			return;
		}
		ViewManager.Instance.FullScreenColorBlink(Colors.White, 0.2, rise: false);
		AudioManager.Instance.AudioPlay("WaveHuge");
		Godot.Collections.Array campTarget = TowerDefenseManager.Instance.GetCampTarget(TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		if (campTarget.Count > 0)
		{
			levelControl.awardPos = ((TowerDefenseCharacter)(GodotObject)campTarget[0]).GetLogicalGlobalPosition();
		}
		foreach (Variant item in campTarget)
		{
			((TowerDefenseCharacter)(GodotObject)item).Destroy();
		}
		CreateLevelClearReward(levelControl.awardPos);
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

	private async Task HideEntryLabelsAfterDelayAsync()
	{
		if (await WaitForTimerOrLifetime(config.entryLabelDuration) && base.IsLifetimeActive && GodotObject.IsInstanceValid(levelControl))
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

	public override void Destroy()
	{
		base.Destroy();
		ClearEntryBroadcast();
		RestoreEntryCharacters();
		if (_viewMapRunning || _viewBroadcastActive || _viewPacketShows.Count > 0)
		{
			RestoreViewMapState();
		}
		_gameFailRunning = false;
		_viewMapRunning = false;
		waveFeature = null;
		config = null;
		sunFeature = null;
		mapFeature = null;
		progressFeature = null;
		cameraFeature = null;
		seedBankFeature = null;
		packetBankFeature = null;
		mowerFeature = null;
		levelControl = null;
	}

	private void CreateLevelClearReward(Vector2 pos)
	{
		TowerDefenseBattleFeatureSlotMachine towerDefenseBattleFeatureSlotMachine = GetFeature("SlotMachine") as TowerDefenseBattleFeatureSlotMachine;
		if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureSlotMachine))
		{
			towerDefenseBattleFeatureSlotMachine.CreateLevelClearReward(pos);
		}
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
}
