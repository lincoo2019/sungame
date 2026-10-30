using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessVase : TowerDefenseBattleProcess, ITowerDefenseProgressSaveGuard
{
	public TowerDefenseLevelVaseManagerConfig config;

	public TowerDefenseBattleFeatureMap mapFeature;

	public TowerDefenseBattleFeatureProgress progressFeature;

	public TowerDefenseBattleFeatureSeedBank seedBankFeature;

	public TowerDefenseBattleFeaturePacketBank packetBankFeature;

	public TowerDefenseBattleFeatureMower mowerFeature;

	public TowerDefenseInGameLevelControl levelControl;

	private readonly List<TowerDefenseInGamePacketShow> _viewPacketShows = new List<TowerDefenseInGamePacketShow>();

	private bool _gameFailRunning;

	private bool _viewMapRunning;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseLevelVaseManagerConfig();
		config.Init(data);
	}

	private async Task SetupUIAsync()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (GodotObject.IsInstanceValid(control) && progressFeature != null)
		{
			string levelName = Tr(TowerDefenseManager.Instance.currentLevelConfig.levelName).Replace("{LevelNumber}", TowerDefenseManager.Instance.currentLevelConfig.levelNumber.ToString());
			progressFeature.SetLevelName(levelName);
			progressFeature.SetDifficultVisible(visible: false);
		}
	}

	private void BindFeatures()
	{
		levelControl = control.levelControl;
		mapFeature = GetFeature<TowerDefenseBattleFeatureMap>("Map");
		progressFeature = GetFeature<TowerDefenseBattleFeatureProgress>("Progress");
		seedBankFeature = GetFeature<TowerDefenseBattleFeatureSeedBank>("SeedBank");
		packetBankFeature = GetFeature<TowerDefenseBattleFeaturePacketBank>("PacketBank");
		mowerFeature = GetFeature<TowerDefenseBattleFeatureMower>("Mower");
	}

	private void BuildFillPools(out List<TowerDefensePacketConfig> all, out List<TowerDefensePacketConfig> plants, out List<TowerDefensePacketConfig> zombies)
	{
		all = new List<TowerDefensePacketConfig>();
		plants = new List<TowerDefensePacketConfig>();
		zombies = new List<TowerDefensePacketConfig>();
		foreach (TowerDefenseLevelVaseFillConfig vaseFill in config.vaseFillList)
		{
			TowerDefensePacketConfig towerDefensePacketConfig = vaseFill?.GetPacket();
			if (GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(towerDefensePacketConfig.characterConfig))
			{
				all.Add(towerDefensePacketConfig);
				if (towerDefensePacketConfig.characterConfig is TowerDefensePlantConfig)
				{
					plants.Add(towerDefensePacketConfig);
				}
				else if (towerDefensePacketConfig.characterConfig is TowerDefenseZombieConfig)
				{
					zombies.Add(towerDefensePacketConfig);
				}
			}
		}
	}

	private static TowerDefensePacketConfig ClonePacketConfig(TowerDefensePacketConfig source)
	{
		if (!GodotObject.IsInstanceValid(source))
		{
			return null;
		}
		return source.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static Godot.Collections.Array ExportChangeCosts(TowerDefensePacketConfig packetConfig)
	{
		return TowerDefensePacketRuntimeState.ExportChangeCosts(packetConfig);
	}

	private static bool TryCreateRuntimePacketConfig(string saveKey, Dictionary state, string overrideKey, string canChangeCostKey, string changeCostListKey, out TowerDefensePacketConfig packetConfig)
	{
		return TowerDefensePacketRuntimeState.TryCreate(saveKey, state, overrideKey, canChangeCostKey, changeCostListKey, out packetConfig);
	}

	private static string GetVaseType(TowerDefenseVase vase)
	{
		if (!GodotObject.IsInstanceValid(vase) || !GodotObject.IsInstanceValid(vase.config))
		{
			return "Normal";
		}
		string name = vase.config.name;
		if (!(name == "VasePlant"))
		{
			if (name == "VaseZombie")
			{
				return "Zombie";
			}
			return "Normal";
		}
		return "Plant";
	}

	private static TowerDefensePacketConfig GetVaseShellPacketConfig(string vaseType)
	{
		string packetName = ((vaseType == "Plant") ? "VasePlant" : ((!(vaseType == "Zombie")) ? "VaseNormal" : "VaseZombie"));
		return TowerDefenseManager.GetPacketConfig(packetName);
	}

	private static void ApplyVaseContent(TowerDefenseVase vase, Dictionary state, string nameKey, string overrideKey, string canChangeCostKey, string changeCostListKey)
	{
		if (!GodotObject.IsInstanceValid(vase))
		{
			return;
		}
		string text = state.GetValueOrDefault(nameKey, "").AsString();
		if (!IsVaseContentStateCurrent(vase, text, state, overrideKey, canChangeCostKey, changeCostListKey))
		{
			if (!TryCreateRuntimePacketConfig(text, state, overrideKey, canChangeCostKey, changeCostListKey, out var packetConfig))
			{
				text = "";
				packetConfig = null;
			}
			vase.SetContentConfig(packetConfig);
		}
	}

	private static bool IsVaseContentStateCurrent(TowerDefenseVase vase, string contentName, Dictionary state, string overrideKey, string canChangeCostKey, string changeCostListKey)
	{
		TowerDefensePacketConfig packetConfig = vase.packetConfig;
		if (!string.Equals(GodotObject.IsInstanceValid(packetConfig) ? packetConfig.saveKey : "", contentName, StringComparison.Ordinal))
		{
			return false;
		}
		if (!state.ContainsKey(overrideKey) && !state.ContainsKey(canChangeCostKey) && !state.ContainsKey(changeCostListKey))
		{
			return true;
		}
		Dictionary dictionary = new Dictionary();
		Dictionary dictionary2 = new Dictionary();
		if (state.ContainsKey(overrideKey))
		{
			dictionary[overrideKey] = (GodotObject.IsInstanceValid(packetConfig?._override) ? packetConfig._override.Export() : new Dictionary());
			dictionary2[overrideKey] = state[overrideKey];
		}
		if (state.ContainsKey(canChangeCostKey))
		{
			dictionary[canChangeCostKey] = !GodotObject.IsInstanceValid(packetConfig) || packetConfig.canChangeCost;
			dictionary2[canChangeCostKey] = state[canChangeCostKey];
		}
		if (state.ContainsKey(changeCostListKey))
		{
			dictionary[changeCostListKey] = ExportChangeCosts(packetConfig);
			dictionary2[changeCostListKey] = state[changeCostListKey];
		}
		return NetworkVariantComparer.DictionaryApproxEquals(dictionary, dictionary2);
	}

	private TowerDefenseVase CreateSyncedVase(Vector2I gridPos, string vaseType, Dictionary state)
	{
		TowerDefensePacketConfig vaseShellPacketConfig = GetVaseShellPacketConfig(vaseType);
		if (!GodotObject.IsInstanceValid(vaseShellPacketConfig))
		{
			return null;
		}
		TowerDefenseVase towerDefenseVase = vaseShellPacketConfig.Plant(gridPos) as TowerDefenseVase;
		if (GodotObject.IsInstanceValid(towerDefenseVase))
		{
			ApplyVaseContent(towerDefenseVase, state, "content_name", "content_override", "content_can_change_cost", "content_change_cost_list");
		}
		return towerDefenseVase;
	}

	private void RemoveSyncedVase(TowerDefenseVase vase)
	{
		if (GodotObject.IsInstanceValid(vase))
		{
			control?.CleanupCharacterCell(vase);
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.CharacterUnregister(vase);
			}
			vase.RemoveFromGroup("Vase");
			vase.RemoveFromGroup("Character");
			if (!vase.IsQueuedForDeletion())
			{
				vase.QueueFree();
			}
		}
	}

	public override Task GameInit()
	{
		BindFeatures();
		Task result = SetupUIAsync();
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return result;
		}
		BuildFillPools(out var all, out var plants, out var zombies);
		if (!config.shuffle)
		{
			foreach (TowerDefenseLevelVaseConfig vase in config.vaseList)
			{
				if (!GodotObject.IsInstanceValid(vase))
				{
					continue;
				}
				string type = vase.type;
				TowerDefensePacketConfig towerDefensePacketConfig = ((type == "Plant") ? TowerDefenseManager.GetPacketConfig("VasePlant") : ((!(type == "Zombie")) ? TowerDefenseManager.GetPacketConfig("VaseNormal") : TowerDefenseManager.GetPacketConfig("VaseZombie")));
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig))
				{
					continue;
				}
				TowerDefenseVase towerDefenseVase = towerDefensePacketConfig.Plant(vase.gridPos) as TowerDefenseVase;
				if (!GodotObject.IsInstanceValid(towerDefenseVase))
				{
					continue;
				}
				if (vase.packetName != "")
				{
					towerDefenseVase.SetContentConfig(ClonePacketConfig(vase.GetPacket()));
					continue;
				}
				type = vase.type;
				List<TowerDefensePacketConfig> list = ((type == "Plant") ? plants : ((!(type == "Zombie")) ? all : zombies));
				List<TowerDefensePacketConfig> list2 = list;
				if (list2.Count > 0)
				{
					towerDefenseVase.SetContentConfig(ClonePacketConfig(list2.PickRandom()));
				}
			}
		}
		else
		{
			List<Vector2I> list3 = new List<Vector2I>();
			List<string> list4 = new List<string>();
			List<TowerDefensePacketConfig> list5 = new List<TowerDefensePacketConfig>();
			foreach (TowerDefenseLevelVaseConfig vase2 in config.vaseList)
			{
				if (GodotObject.IsInstanceValid(vase2))
				{
					list3.Add(vase2.gridPos);
					list4.Add(vase2.type);
					if (vase2.packetName == "")
					{
						list5.Add((all.Count > 0) ? all.PickRandom() : null);
					}
					else
					{
						list5.Add(vase2.GetPacket());
					}
				}
			}
			list3.Shuffle();
			list4.Shuffle();
			list5.Shuffle();
			foreach (TowerDefensePacketConfig item in list5)
			{
				int index = list3.Count - 1;
				Vector2I gridPos = list3[index];
				list3.RemoveAt(index);
				string text = null;
				if (GodotObject.IsInstanceValid(item))
				{
					if (item.characterConfig is TowerDefensePlantConfig)
					{
						text = "Plant";
					}
					else if (item.characterConfig is TowerDefenseZombieConfig)
					{
						text = "Zombie";
					}
				}
				string text2;
				if (text != null && list4.Remove(text))
				{
					text2 = text;
				}
				else if (text != null && list4.Remove("Normal"))
				{
					text2 = "Normal";
				}
				else
				{
					if (list4.Count == 0)
					{
						break;
					}
					int index2 = list4.Count - 1;
					text2 = list4[index2];
					list4.RemoveAt(index2);
				}
				TowerDefensePacketConfig towerDefensePacketConfig2 = ((text2 == "Plant") ? TowerDefenseManager.GetPacketConfig("VasePlant") : ((!(text2 == "Zombie")) ? TowerDefenseManager.GetPacketConfig("VaseNormal") : TowerDefenseManager.GetPacketConfig("VaseZombie")));
				if (GodotObject.IsInstanceValid(towerDefensePacketConfig2))
				{
					TowerDefenseVase towerDefenseVase2 = towerDefensePacketConfig2.Plant(gridPos) as TowerDefenseVase;
					if (GodotObject.IsInstanceValid(towerDefenseVase2))
					{
						towerDefenseVase2.SetContentConfig(ClonePacketConfig(item));
					}
				}
			}
		}
		return result;
	}

	public override Task GameInitFromProgress()
	{
		BindFeatures();
		return SetupUIAsync();
	}

	public override async Task GameEntry()
	{
		bool mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		if (control.hasProgress)
		{
			if (seedBankFeature != null && GodotObject.IsInstanceValid(seedBankFeature.seedBank))
			{
				seedBankFeature.seedBank.packetSlotContainer.Visible = true;
				if (mobilePreset)
				{
					seedBankFeature.seedBank.animationPlayer?.Play("MobileEnter");
				}
				else
				{
					seedBankFeature.seedBank.animationPlayer?.Play("Enter");
				}
			}
			if (mobilePreset)
			{
				control.uiTopAnimationPlayer.Play("MobileEnter");
			}
			else
			{
				control.uiTopAnimationPlayer.Play("Enter");
			}
			return;
		}
		int packetBankMethod = (int)config.packetBankMethod;
		TaskCompletionSource<bool> tcs;
		if ((uint)(packetBankMethod - 1) <= 2u)
		{
			if (packetBankFeature != null && !packetBankFeature.skipPacketChoose && GodotObject.IsInstanceValid(packetBankFeature.packetBank) && seedBankFeature != null && GodotObject.IsInstanceValid(seedBankFeature.seedBank))
			{
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
				control.buttonPause.Visible = true;
				seedBankFeature.seedBank.packetSlotContainer.Visible = true;
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
				if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(packetBankFeature?.packetBank) || !GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
				{
					return;
				}
				seedBankFeature.seedBank.ReadyPackets();
				if (mobilePreset)
				{
					packetBankFeature.packetBank.packetBankAnimationPlayer.Play("MobileExit");
				}
				else
				{
					packetBankFeature.packetBank.packetBankAnimationPlayer.Play("Exit");
				}
				if (await WaitForTimerOrLifetime(config.packetBankExitDelay) && GodotObject.IsInstanceValid(control))
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
				return;
			}
			if (seedBankFeature != null && GodotObject.IsInstanceValid(seedBankFeature.seedBank))
			{
				seedBankFeature.seedBank.ReadyPackets();
				if (mobilePreset)
				{
					seedBankFeature.seedBank.animationPlayer?.Play("MobileEnter");
				}
				else
				{
					seedBankFeature.seedBank.animationPlayer?.Play("Enter");
				}
			}
			if (mobilePreset)
			{
				control.uiTopAnimationPlayer.Play("MobileEnter");
			}
			else
			{
				control.uiTopAnimationPlayer.Play("Enter");
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
		void ChooseOverHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public override Task GameReady()
	{
		if (!control.hasProgress && config.mowerUse && mowerFeature != null)
		{
			mowerFeature.MowerInit();
		}
		if (progressFeature != null)
		{
			progressFeature.SetLevelNameVisible(visible: true);
			if (Global.Instance.enterLevelMode == "DailyLevel" || Global.Instance.enterLevelMode == "OnlineLevel" || Global.Instance.enterLevelMode == "LevelTest" || Global.Instance.enterLevelMode == "DiyLevel")
			{
				progressFeature.SetDifficultVisible(visible: false);
			}
		}
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
			if (GodotObject.IsInstanceValid(packetBankFeature?.packetBank))
			{
				packetBankFeature.packetBank.Visible = false;
			}
			if (GodotObject.IsInstanceValid(control?.bankUILayer))
			{
				control.bankUILayer.Visible = false;
			}
			bool flag = GodotObject.IsInstanceValid(mapFeature?.currentMap);
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
			character.CreateTween().TweenProperty(character.sprite, "meshColor:a", 0.0, 1.0).Finished += delegate
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
		try
		{
			bool mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
			PlayPacketBankAnimation(mobilePreset ? "MobileExit" : "Exit");
			SetPacketDrawOnly();
			if (!GodotObject.IsInstanceValid(control))
			{
				return;
			}
			control.isView = true;
			BroadCastConfig broadCastConfig = new BroadCastConfig
			{
				broadCastString = "INGAME_VIEW_BACK"
			};
			BroadCastManager.Instance.BroadCastAdd(broadCastConfig);
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
			if (base.IsLifetimeActive)
			{
				RestoreViewMapState();
				PlayPacketBankAnimation(mobilePreset ? "MobileEnter" : "Enter");
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
		if (GodotObject.IsInstanceValid(BroadCastManager.Instance))
		{
			BroadCastManager.Instance.BraodCastClear();
		}
	}

	private void PlayPacketBankAnimation(StringName animationName)
	{
		if (GodotObject.IsInstanceValid(packetBankFeature?.packetBank?.packetBankAnimationPlayer))
		{
			packetBankFeature.packetBank.packetBankAnimationPlayer.Play(animationName);
		}
	}

	public override void InputProcess(InputEvent event_)
	{
		if (control != null && control.isView && Input.IsAnythingPressed())
		{
			control.EmitViewBack();
		}
	}

	public override bool CheckFinal()
	{
		if (!GodotObject.IsInstanceValid(levelControl))
		{
			return false;
		}
		if (!CanFinish())
		{
			return false;
		}
		SceneTree tree = GetTree();
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
		if (!GodotObject.IsInstanceValid(tree) || !GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry) || towerDefenseBattleCharacterRegistry.GetVaseCount() > 0 || tree.GetNodeCountInGroup("Vase") > 0)
		{
			return false;
		}
		Godot.Collections.Array campTarget = TowerDefenseManager.Instance.GetCampTarget(TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		foreach (Variant item in campTarget)
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (!IsIgnoredVaseObjectiveTarget(towerDefenseCharacter))
			{
				levelControl.awardPos = towerDefenseCharacter.GetLogicalGlobalPosition();
				return false;
			}
		}
		foreach (Node item2 in tree.GetNodesInGroup("Zombie"))
		{
			if (item2 is TowerDefenseCharacter { isDestroy: not false, skipDestroySet: false })
			{
				return false;
			}
		}
		foreach (Variant item3 in campTarget)
		{
			TowerDefenseCharacter towerDefenseCharacter3 = (TowerDefenseCharacter)(GodotObject)item3;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter3))
			{
				towerDefenseCharacter3.Destroy();
			}
		}
		return true;
	}

	private static bool IsIgnoredVaseObjectiveTarget(TowerDefenseCharacter target)
	{
		if (!GodotObject.IsInstanceValid(target))
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(target.instance) && target.instance.die)
		{
			return true;
		}
		string text = (GodotObject.IsInstanceValid(target.config) ? target.config.name : "");
		if (!text.StartsWith("ZombieDigger", StringComparison.Ordinal))
		{
			return text.StartsWith("ZombieYetiDigger", StringComparison.Ordinal);
		}
		return true;
	}

	public override void Finish()
	{
		if (!CanFinish() || levelControl == null || levelControl.awardCreate)
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
		levelControl.AwardCreate(levelControl.awardPos);
	}

	public override void PhysicsProcess(double delta)
	{
		if (GodotObject.IsInstanceValid(levelControl) && !levelControl.awardCreate)
		{
			if (levelControl.hasSpawn)
			{
				levelControl.hasSpawn = false;
			}
			else if ((!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && CanFinish() && CheckFinal())
			{
				levelControl.AwardCreate(levelControl.awardPos);
			}
		}
	}

	public bool CanSaveProgress(out string reason)
	{
		if (GodotObject.IsInstanceValid(levelControl) && levelControl.hasSpawn)
		{
			reason = "vase content is still spawning";
			return false;
		}
		SceneTree tree = GetTree();
		if (GodotObject.IsInstanceValid(tree))
		{
			foreach (Node item in tree.GetNodesInGroup("Vase"))
			{
				if (item is TowerDefenseVase towerDefenseVase && (towerDefenseVase.over || towerDefenseVase.isDestroy || towerDefenseVase.IsQueuedForDeletion()))
				{
					reason = "a vase is being opened";
					return false;
				}
			}
			foreach (Node item2 in tree.GetNodesInGroup("VasePacketShow"))
			{
				if (item2 is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && (towerDefenseInGamePacketShow.select || towerDefenseInGamePacketShow.HasMeta("packet_pending_plant")))
				{
					reason = "a vase packet is being placed";
					return false;
				}
			}
		}
		reason = "";
		return true;
	}

	public override Dictionary SyncSerialize()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		Dictionary dictionary = new Dictionary();
		Godot.Collections.Array array2 = new Godot.Collections.Array();
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return new Dictionary { { "vases", array } };
		}
		foreach (Node item in tree.GetNodesInGroup("Vase"))
		{
			if (!GodotObject.IsInstanceValid(item) || !(item is TowerDefenseVase { over: false, isDestroy: false } towerDefenseVase) || towerDefenseVase.IsQueuedForDeletion())
			{
				continue;
			}
			TowerDefensePacketConfig packetConfig = towerDefenseVase.packetConfig;
			Dictionary dictionary2 = (GodotObject.IsInstanceValid(packetConfig?._override) ? packetConfig._override.Export() : dictionary);
			Godot.Collections.Array array3;
			if (GodotObject.IsInstanceValid(packetConfig))
			{
				List<TowerDefensePacketChangeCost> changeCostList = packetConfig.changeCostList;
				if (changeCostList != null && changeCostList.Count > 0)
				{
					array3 = ExportChangeCosts(packetConfig);
					goto IL_00fc;
				}
			}
			array3 = array2;
			goto IL_00fc;
			IL_00fc:
			Godot.Collections.Array array4 = array3;
			array.Add(new Dictionary
			{
				{
					"grid_x",
					towerDefenseVase.gridPos.X
				},
				{
					"grid_y",
					towerDefenseVase.gridPos.Y
				},
				{
					"type",
					GetVaseType(towerDefenseVase)
				},
				{
					"content_name",
					GodotObject.IsInstanceValid(packetConfig) ? packetConfig.saveKey : ""
				},
				{ "content_override", dictionary2 },
				{
					"content_can_change_cost",
					!GodotObject.IsInstanceValid(packetConfig) || packetConfig.canChangeCost
				},
				{ "content_change_cost_list", array4 }
			});
		}
		return new Dictionary { { "vases", array } };
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		if (!_data.ContainsKey("vases"))
		{
			return;
		}
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return;
		}
		System.Collections.Generic.Dictionary<Vector2I, Dictionary> dictionary = new System.Collections.Generic.Dictionary<Vector2I, Dictionary>();
		foreach (Variant item in _data["vases"].AsGodotArray())
		{
			Dictionary dictionary2 = item.AsGodotDictionary();
			int x = dictionary2.GetValueOrDefault("grid_x", 0).AsInt32();
			int y = dictionary2.GetValueOrDefault("grid_y", 0).AsInt32();
			dictionary[new Vector2I(x, y)] = dictionary2;
		}
		System.Collections.Generic.Dictionary<Vector2I, TowerDefenseVase> dictionary3 = new System.Collections.Generic.Dictionary<Vector2I, TowerDefenseVase>();
		foreach (Node item2 in tree.GetNodesInGroup("Vase"))
		{
			if (GodotObject.IsInstanceValid(item2) && item2 is TowerDefenseVase towerDefenseVase)
			{
				if (towerDefenseVase.over || towerDefenseVase.isDestroy || towerDefenseVase.IsQueuedForDeletion() || dictionary3.ContainsKey(towerDefenseVase.gridPos))
				{
					RemoveSyncedVase(towerDefenseVase);
				}
				else
				{
					dictionary3[towerDefenseVase.gridPos] = towerDefenseVase;
				}
			}
		}
		foreach (KeyValuePair<Vector2I, Dictionary> item3 in dictionary)
		{
			Vector2I key = item3.Key;
			Dictionary value = item3.Value;
			string text = value.GetValueOrDefault("type", "Normal").AsString();
			if (dictionary3.TryGetValue(key, out var value2))
			{
				dictionary3.Remove(key);
				if (GetVaseType(value2) == text)
				{
					ApplyVaseContent(value2, value, "content_name", "content_override", "content_can_change_cost", "content_change_cost_list");
					continue;
				}
				RemoveSyncedVase(value2);
			}
			CreateSyncedVase(key, text, value);
		}
		foreach (TowerDefenseVase value3 in dictionary3.Values)
		{
			RemoveSyncedVase(value3);
		}
	}

	public override Dictionary SaveProcess()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Node item in GetTree().GetNodesInGroup("Vase"))
		{
			if (GodotObject.IsInstanceValid(item) && item is TowerDefenseVase { over: false, isDestroy: false } towerDefenseVase && !towerDefenseVase.IsQueuedForDeletion())
			{
				TowerDefensePacketConfig packetConfig = towerDefenseVase.packetConfig;
				array.Add(new Dictionary
				{
					{
						"nodeName",
						towerDefenseVase.Name.ToString().ValidateNodeName()
					},
					{
						"gridPosX",
						towerDefenseVase.gridPos.X
					},
					{
						"gridPosY",
						towerDefenseVase.gridPos.Y
					},
					{
						"contentName",
						GodotObject.IsInstanceValid(packetConfig) ? packetConfig.saveKey : ""
					},
					{
						"contentOverride",
						GodotObject.IsInstanceValid(packetConfig?._override) ? packetConfig._override.Export() : new Dictionary()
					},
					{
						"contentCanChangeCost",
						!GodotObject.IsInstanceValid(packetConfig) || packetConfig.canChangeCost
					},
					{
						"contentChangeCostList",
						ExportChangeCosts(packetConfig)
					}
				});
			}
		}
		Godot.Collections.Array array2 = new Godot.Collections.Array();
		foreach (Node item2 in GetTree().GetNodesInGroup("VasePacketShow"))
		{
			if (GodotObject.IsInstanceValid(item2) && item2 is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && !towerDefenseInGamePacketShow.IsQueuedForDeletion() && towerDefenseInGamePacketShow.alive && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.config))
			{
				Dictionary dictionary = new Dictionary();
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.moveComponent))
				{
					dictionary = towerDefenseInGamePacketShow.moveComponent.ExportComponentSave();
				}
				array2.Add(new Dictionary
				{
					{
						"saveKey",
						towerDefenseInGamePacketShow.config.saveKey
					},
					{
						"overrideSave",
						GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.config._override) ? towerDefenseInGamePacketShow.config._override.Export() : new Dictionary()
					},
					{
						"canChangeCost",
						towerDefenseInGamePacketShow.config.canChangeCost
					},
					{
						"changeCostList",
						ExportChangeCosts(towerDefenseInGamePacketShow.config)
					},
					{
						"posX",
						towerDefenseInGamePacketShow.GlobalPosition.X
					},
					{
						"posY",
						towerDefenseInGamePacketShow.GlobalPosition.Y
					},
					{ "zIndex", towerDefenseInGamePacketShow.ZIndex },
					{ "showLove", towerDefenseInGamePacketShow.showLove },
					{ "showCost", towerDefenseInGamePacketShow.showCost },
					{ "onlyDraw", towerDefenseInGamePacketShow.onlyDraw },
					{ "alive", towerDefenseInGamePacketShow.alive },
					{ "lock", towerDefenseInGamePacketShow.@lock },
					{ "plantOnce", towerDefenseInGamePacketShow.plantOnce },
					{ "useCost", towerDefenseInGamePacketShow.useCost },
					{ "openShadow", towerDefenseInGamePacketShow.openShadow },
					{ "start", towerDefenseInGamePacketShow.start },
					{ "select", towerDefenseInGamePacketShow.select },
					{ "coldDown", towerDefenseInGamePacketShow.coldDown },
					{ "coldDownOpen", towerDefenseInGamePacketShow.coldDownOpen },
					{ "coldDownTimer", towerDefenseInGamePacketShow.coldDownTimer },
					{ "aliveTime", towerDefenseInGamePacketShow.aliveTime },
					{ "aliveTimer", towerDefenseInGamePacketShow.aliveTimer },
					{ "blinkTimer", towerDefenseInGamePacketShow.blinkTimer },
					{ "blink", towerDefenseInGamePacketShow.blink },
					{ "height", towerDefenseInGamePacketShow.height },
					{
						"savePosX",
						towerDefenseInGamePacketShow.savePos.X
					},
					{
						"savePosY",
						towerDefenseInGamePacketShow.savePos.Y
					},
					{ "canPressPutBack", towerDefenseInGamePacketShow.canPressPutBack },
					{ "moveData", dictionary }
				});
			}
		}
		return new Dictionary
		{
			{ "vaseList", array },
			{ "packetShowList", array2 }
		};
	}

	private void ClearVasePacketShowsForLoad()
	{
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return;
		}
		PacketPickControl packetPickControl = TowerDefenseManager.Instance?.GetPacketPickControl();
		foreach (Node item in tree.GetNodesInGroup("VasePacketShow"))
		{
			if (item is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				if (towerDefenseInGamePacketShow.HasMeta("packet_sync_id"))
				{
					int syncId = towerDefenseInGamePacketShow.GetMeta("packet_sync_id").AsInt32();
					control?.UnregisterSyncPacket(syncId);
					towerDefenseInGamePacketShow.RemoveMeta("packet_sync_id");
				}
				towerDefenseInGamePacketShow.RemoveMeta("packet_pending_plant");
				if (GodotObject.IsInstanceValid(packetPickControl) && packetPickControl.packetPick == towerDefenseInGamePacketShow)
				{
					packetPickControl.PacketPickRelease();
				}
				towerDefenseInGamePacketShow.ClearEventHandlers();
				towerDefenseInGamePacketShow.RemoveFromGroup("VasePacketShow");
				if (!towerDefenseInGamePacketShow.IsQueuedForDeletion())
				{
					towerDefenseInGamePacketShow.QueueFree();
				}
			}
		}
	}

	public override void LoadProcess(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		ClearVasePacketShowsForLoad();
		foreach (Variant item in _data.GetValueOrDefault("vaseList", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			StringName key = dictionary.GetValueOrDefault("nodeName", "").AsStringName();
			TowerDefenseVase towerDefenseVase = null;
			if (_owner != null && _owner.charcterDicionary.ContainsKey(key))
			{
				towerDefenseVase = _owner.charcterDicionary[key] as TowerDefenseVase;
			}
			if (GodotObject.IsInstanceValid(towerDefenseVase))
			{
				ApplyVaseContent(towerDefenseVase, dictionary, "contentName", "contentOverride", "contentCanChangeCost", "contentChangeCostList");
			}
		}
		foreach (Variant item2 in _data.GetValueOrDefault("packetShowList", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary dictionary2 = item2.AsGodotDictionary();
			if (!TryCreateRuntimePacketConfig(dictionary2.GetValueOrDefault("saveKey", "").AsString(), dictionary2, "overrideSave", "canChangeCost", "changeCostList", out var packetConfig) || !GodotObject.IsInstanceValid(packetConfig))
			{
				continue;
			}
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) && GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
			{
				towerDefenseInGamePacketShow.ZIndex = dictionary2.GetValueOrDefault("zIndex", 0).AsInt32();
				towerDefenseInGamePacketShow.GlobalPosition = new Vector2((float)dictionary2.GetValueOrDefault("posX", 0.0).AsDouble(), (float)dictionary2.GetValueOrDefault("posY", 0.0).AsDouble());
				TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseInGamePacketShow.Init(packetConfig);
				towerDefenseInGamePacketShow.StartInit();
				towerDefenseInGamePacketShow.showLove = dictionary2.GetValueOrDefault("showLove", false).AsBool();
				towerDefenseInGamePacketShow.showCost = dictionary2.GetValueOrDefault("showCost", false).AsBool();
				towerDefenseInGamePacketShow.onlyDraw = dictionary2.GetValueOrDefault("onlyDraw", false).AsBool();
				towerDefenseInGamePacketShow.alive = dictionary2.GetValueOrDefault("alive", true).AsBool();
				towerDefenseInGamePacketShow.@lock = dictionary2.GetValueOrDefault("lock", false).AsBool();
				towerDefenseInGamePacketShow.plantOnce = dictionary2.GetValueOrDefault("plantOnce", true).AsBool();
				towerDefenseInGamePacketShow.useCost = dictionary2.GetValueOrDefault("useCost", false).AsBool();
				towerDefenseInGamePacketShow.openShadow = dictionary2.GetValueOrDefault("openShadow", false).AsBool();
				towerDefenseInGamePacketShow.start = dictionary2.GetValueOrDefault("start", false).AsBool();
				towerDefenseInGamePacketShow.select = false;
				towerDefenseInGamePacketShow.coldDown = dictionary2.GetValueOrDefault("coldDown", towerDefenseInGamePacketShow.coldDown).AsDouble();
				towerDefenseInGamePacketShow.coldDownOpen = dictionary2.GetValueOrDefault("coldDownOpen", false).AsBool();
				towerDefenseInGamePacketShow.coldDownTimer = dictionary2.GetValueOrDefault("coldDownTimer", 0.0).AsDouble();
				towerDefenseInGamePacketShow.canPressPutBack = dictionary2.GetValueOrDefault("canPressPutBack", false).AsBool();
				towerDefenseInGamePacketShow.aliveTime = dictionary2.GetValueOrDefault("aliveTime", 15.0).AsDouble();
				towerDefenseInGamePacketShow.aliveTimer = dictionary2.GetValueOrDefault("aliveTimer", 0.0).AsDouble();
				towerDefenseInGamePacketShow.blinkTimer = dictionary2.GetValueOrDefault("blinkTimer", 0.0).AsDouble();
				towerDefenseInGamePacketShow.blink = dictionary2.GetValueOrDefault("blink", false).AsBool();
				towerDefenseInGamePacketShow.height = dictionary2.GetValueOrDefault("height", -1.0).AsDouble();
				towerDefenseInGamePacketShow.savePos = new Vector2((float)dictionary2.GetValueOrDefault("savePosX", 0.0).AsDouble(), (float)dictionary2.GetValueOrDefault("savePosY", 0.0).AsDouble());
				Dictionary dictionary3 = dictionary2.GetValueOrDefault("moveData", new Dictionary()).AsGodotDictionary();
				if (dictionary3.Count > 0 && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.moveComponent))
				{
					towerDefenseInGamePacketShow.moveComponent.ImportComponentSave(dictionary3, _owner);
				}
				else if (dictionary3.Count == 0 && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.moveComponent))
				{
					towerDefenseInGamePacketShow.moveComponent.QueueFree();
				}
				PacketPickControl packetPickControl = TowerDefenseManager.Instance.GetPacketPickControl();
				if (GodotObject.IsInstanceValid(packetPickControl))
				{
					towerDefenseInGamePacketShow.OnPressed += packetPickControl.PickPacket;
				}
				towerDefenseInGamePacketShow.AddToGroup("VasePacketShow");
			}
		}
	}

	public override void Destroy()
	{
		base.Destroy();
		if (_viewMapRunning || _viewPacketShows.Count > 0)
		{
			RestoreViewMapState();
		}
		_gameFailRunning = false;
		_viewMapRunning = false;
		config = null;
		mapFeature = null;
		progressFeature = null;
		seedBankFeature = null;
		packetBankFeature = null;
		mowerFeature = null;
		levelControl = null;
	}
}
