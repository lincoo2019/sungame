using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleProcessIZM : TowerDefenseBattleProcess
{
	private ulong _nextFailureCheckFrame;

	private readonly List<TowerDefenseInGamePacketShow> _viewPacketShows = new List<TowerDefenseInGamePacketShow>();

	private bool _viewMapRunning;

	private bool _viewBroadcastActive;

	public TowerDefenseLevelIZMManagerConfig config;

	public TowerDefenseBattleFeatureMap mapFeature;

	public TowerDefenseBattleFeatureProgress progressFeature;

	public TowerDefenseBattleFeatureSeedBank seedBankFeature;

	public TowerDefenseBattleFeaturePacketBank packetBankFeature;

	public TowerDefenseBattleFeatureBrain brainFeature;

	public TowerDefenseBattleFeatureConveyorBelt conveyorBeltFeature;

	public TowerDefenseInGameLevelControl levelControl;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseLevelIZMManagerConfig();
		config.Init(data);
	}

	public override void OnReady()
	{
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

	public override Task GameInit()
	{
		ResolveDependencies();
		return SetupUIAsync();
	}

	public override Task GameInitFromProgress()
	{
		ResolveDependencies();
		return SetupUIAsync();
	}

	private void ResolveDependencies()
	{
		levelControl = control.levelControl;
		mapFeature = GetFeature<TowerDefenseBattleFeatureMap>("Map");
		progressFeature = GetFeature<TowerDefenseBattleFeatureProgress>("Progress");
		seedBankFeature = GetFeature<TowerDefenseBattleFeatureSeedBank>("SeedBank");
		packetBankFeature = GetFeature<TowerDefenseBattleFeaturePacketBank>("PacketBank");
		brainFeature = GetFeature<TowerDefenseBattleFeatureBrain>("Brain");
		conveyorBeltFeature = GetFeature<TowerDefenseBattleFeatureConveyorBelt>("ConveyorBelt");
	}

	public async Task<List<TowerDefenseCharacter>> Execute(IList<TowerDefenseLevelPreSpawnConfig> preSpawnlist)
	{
		if (!base.IsLifetimeActive || preSpawnlist == null)
		{
			return new List<TowerDefenseCharacter>();
		}
		List<TowerDefenseLevelPreSpawnConfig> list = new List<TowerDefenseLevelPreSpawnConfig>(preSpawnlist.Count);
		List<TowerDefenseCharacter> spawnedCharacters = new List<TowerDefenseCharacter>();
		List<TowerDefenseLevelPreSpawnConfig> list2 = new List<TowerDefenseLevelPreSpawnConfig>();
		foreach (TowerDefenseLevelPreSpawnConfig item in preSpawnlist)
		{
			if (!GodotObject.IsInstanceValid(item))
			{
				continue;
			}
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(item.packetName);
			if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(packetConfig.characterConfig))
			{
				continue;
			}
			if (packetConfig.characterConfig is TowerDefensePlantConfig)
			{
				TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = item.Duplicate() as TowerDefenseLevelPreSpawnConfig;
				if (GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig))
				{
					list.Add(towerDefenseLevelPreSpawnConfig);
				}
			}
			else
			{
				list2.Add(item);
			}
		}
		int y = mapFeature.config.gridNum.Y;
		int x = mapFeature.config.gridNum.X;
		List<TowerDefenseLevelPreSpawnConfig>[][] array = new List<TowerDefenseLevelPreSpawnConfig>[y + 1][];
		for (int i = 0; i <= y; i++)
		{
			array[i] = new List<TowerDefenseLevelPreSpawnConfig>[x + 1];
			for (int j = 0; j <= x; j++)
			{
				array[i][j] = new List<TowerDefenseLevelPreSpawnConfig>();
			}
		}
		System.Collections.Generic.Dictionary<string, List<int>> dictionary = new System.Collections.Generic.Dictionary<string, List<int>>(StringComparer.Ordinal);
		for (int k = 1; k <= y; k++)
		{
			string key = BuildLineTopologyKey(k, x);
			if (!dictionary.TryGetValue(key, out var value))
			{
				value = new List<int>();
				dictionary.Add(key, value);
			}
			value.Add(k);
		}
		int[] array2 = new int[y + 1];
		array2[0] = -1;
		foreach (List<int> value4 in dictionary.Values)
		{
			List<int> list3 = new List<int>(value4);
			for (int num = list3.Count - 1; num > 0; num--)
			{
				int num2 = GD.RandRange(0, num);
				List<int> list4 = list3;
				int index = num;
				int index2 = num2;
				int value2 = list3[num2];
				int value3 = list3[num];
				list4[index] = value2;
				list3[index2] = value3;
			}
			for (int l = 0; l < value4.Count; l++)
			{
				array2[value4[l]] = list3[l];
			}
		}
		foreach (TowerDefenseLevelPreSpawnConfig item2 in list)
		{
			int x2 = item2.gridPos.X;
			int y2 = item2.gridPos.Y;
			if (x2 > 0 && x2 <= x && y2 > 0 && y2 <= y)
			{
				int num3 = array2[y2];
				array[num3][x2].Add(item2);
			}
		}
		for (int m = 1; m <= y; m++)
		{
			List<int> list5 = new List<int>();
			for (int n = 1; n <= x; n++)
			{
				foreach (TowerDefenseLevelPreSpawnConfig item3 in array[m][n])
				{
					_ = item3;
					list5.Add(n);
				}
			}
			if (list5.Count <= 0)
			{
				continue;
			}
			for (int num4 = 1; num4 <= x; num4++)
			{
				if (array[m][num4].Count > 0 && (double)GD.Randf() > 0.5)
				{
					int num5 = list5.PickRandom();
					List<TowerDefenseLevelPreSpawnConfig> list6 = array[m][num5];
					array[m][num5] = array[m][num4];
					array[m][num4] = list6;
				}
			}
		}
		for (int num6 = 1; num6 <= y; num6++)
		{
			for (int num7 = 1; num7 <= x; num7++)
			{
				foreach (TowerDefenseLevelPreSpawnConfig item4 in array[num6][num7])
				{
					item4.gridPos = new Vector2I(num7, num6);
				}
			}
		}
		foreach (TowerDefenseLevelPreSpawnConfig item5 in list2)
		{
			list.Add(item5);
		}
		List<TowerDefenseCharacter> list7 = spawnedCharacters;
		list7.AddRange(await SpawnPreSpawnCharactersWithRetry(list));
		return spawnedCharacters;
	}

	private static string BuildLineTopologyKey(int line, int columnCount)
	{
		StringBuilder stringBuilder = new StringBuilder(columnCount * 12);
		for (int i = 1; i <= columnCount; i++)
		{
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(i, line));
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				stringBuilder.Append("!;");
				continue;
			}
			int[] array = new int[mapCell.gridType.Count];
			for (int j = 0; j < mapCell.gridType.Count; j++)
			{
				array[j] = (int)mapCell.gridType[j];
			}
			System.Array.Sort(array);
			int[] array2 = array;
			foreach (int value in array2)
			{
				stringBuilder.Append(value).Append(',');
			}
			stringBuilder.Append(';');
		}
		return stringBuilder.ToString();
	}

	private async Task<List<TowerDefenseCharacter>> SpawnPreSpawnCharactersWithRetry(IList<TowerDefenseLevelPreSpawnConfig> preSpawnConfigs)
	{
		List<TowerDefenseCharacter> spawnedCharacters = new List<TowerDefenseCharacter>();
		List<TowerDefenseLevelPreSpawnConfig> pending = new List<TowerDefenseLevelPreSpawnConfig>(preSpawnConfigs);
		int retryPass = 0;
		while (base.IsLifetimeActive && pending.Count > 0 && retryPass < config.preSpawnMaxRetryPasses)
		{
			List<TowerDefenseLevelPreSpawnConfig> list = new List<TowerDefenseLevelPreSpawnConfig>();
			int num = 0;
			foreach (TowerDefenseLevelPreSpawnConfig item in pending)
			{
				TowerDefenseCharacter towerDefenseCharacter = item.SpawnCharacter();
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					list.Add(item);
					continue;
				}
				num++;
				spawnedCharacters.Add(towerDefenseCharacter);
				if (GodotObject.IsInstanceValid(item.characterOverride))
				{
					item.characterOverride.ExecuteCharacter(towerDefenseCharacter);
				}
			}
			pending = list;
			if (pending.Count == 0 || num == 0)
			{
				break;
			}
			retryPass++;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (!base.IsLifetimeActive)
			{
				break;
			}
		}
		return spawnedCharacters;
	}

	public override async Task GameEntry()
	{
		if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(control))
		{
			return;
		}
		bool mobilePreset = GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool();
		TaskCompletionSource<bool> tcs;
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
		}
		else if (GodotObject.IsInstanceValid(packetBankFeature) && !packetBankFeature.skipPacketChoose && GodotObject.IsInstanceValid(packetBankFeature.packetBank))
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
			if (seedBankFeature != null)
			{
				seedBankFeature.seedBank.packetSlotContainer.Visible = true;
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
			if (base.IsLifetimeActive && GodotObject.IsInstanceValid(packetBankFeature?.packetBank))
			{
				if (seedBankFeature != null)
				{
					seedBankFeature.seedBank.ReadyPackets();
				}
				if (mobilePreset)
				{
					packetBankFeature.packetBank.packetBankAnimationPlayer.Play("MobileExit");
				}
				else
				{
					packetBankFeature.packetBank.packetBankAnimationPlayer.Play("Exit");
				}
				await WaitForTimerOrLifetime(config.packetBankExitDelay);
			}
		}
		else
		{
			if (seedBankFeature != null)
			{
				seedBankFeature.seedBank.ReadyPackets();
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
		void ChooseOverHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public override Task GameReady()
	{
		if (levelControl == null)
		{
			return Task.CompletedTask;
		}
		if (!control.hasProgress && brainFeature != null)
		{
			brainFeature.BrainInit();
		}
		if (progressFeature != null)
		{
			int num = brainFeature?.GetAliveBrainCount() ?? 0;
			progressFeature.SetProgressMeterMaxValue(num);
			progressFeature.SetProgressMeterWaveNum(num);
			progressFeature.SetProgressMeterPreviewWave(0);
			progressFeature.SetProgressMeterValue(0.0);
			progressFeature.SetLevelNameVisible(visible: true);
			if (Global.Instance.enterLevelMode == "DailyLevel" || Global.Instance.enterLevelMode == "OnlineLevel" || Global.Instance.enterLevelMode == "LevelTest" || Global.Instance.enterLevelMode == "DiyLevel")
			{
				progressFeature.SetDifficultVisible(visible: false);
			}
		}
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (progressFeature != null)
		{
			progressFeature.SetProgressMeterHideItem(hide: true);
			progressFeature.SetProgressMeterVisible(visible: true);
		}
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		if (progressFeature != null)
		{
			progressFeature.SetProgressMeterHideItem(hide: true);
			progressFeature.SetProgressMeterVisible(visible: true);
		}
		return Task.CompletedTask;
	}

	public override void GameFail(TowerDefenseCharacter enterCharacter)
	{
		control.ZombieWonLevelFail(playAnime: false);
	}

	public override void ZombieEnterHouse(TowerDefenseCharacter character)
	{
		if (TowerDefenseManager.HasGameplayAuthority && character is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie.instance) && !towerDefenseZombie.instance.hypnoses && brainFeature != null && brainFeature.TryGetAliveBrain(towerDefenseZombie.gridPos.Y, out var brain))
		{
			if (GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay("Chomp");
			}
			brain.Destroy();
		}
		RemoveEnteredZombie(character);
	}

	private void RemoveEnteredZombie(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		character.CreateTween().TweenProperty(character.sprite, "meshColor:a", 0.0, config.enterHouseFadeDuration).Finished += delegate
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

	public override void InputProcess(InputEvent event_)
	{
		if (control != null && control.isView && Input.IsAnythingPressed())
		{
			control.EmitViewBack();
		}
	}

	public override bool CheckFinal()
	{
		if (GodotObject.IsInstanceValid(control) && control.HasPendingBattleOperations)
		{
			return false;
		}
		if (levelControl == null)
		{
			return false;
		}
		if (brainFeature != null && brainFeature.TryGetFirstAliveBrain(out var brain))
		{
			levelControl.awardPos = brain.GetLogicalGlobalPosition();
			return false;
		}
		return true;
	}

	public override bool CheckFail()
	{
		if (GodotObject.IsInstanceValid(control) && control.HasPendingBattleOperations)
		{
			return false;
		}
		SceneTree tree = GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return false;
		}
		if (tree.GetNodeCountInGroup("Sun") > 0 || tree.GetNodeCountInGroup("BrainSun") > 0 || tree.GetNodeCountInGroup("JalapenoSun") > 0 || tree.GetNodeCountInGroup("QXSun") > 0)
		{
			return false;
		}
		EconomyAccountId local = EconomyAccountId.Local;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		bool flag = true;
		if (GodotObject.IsInstanceValid(seedBankFeature?.seedBank))
		{
			foreach (TowerDefenseInGamePacketShow packet in seedBankFeature.seedBank.packetList)
			{
				if (!packet.useCost || instance.CanAffordSun(local, packet.itemCost))
				{
					flag = false;
					break;
				}
			}
		}
		if (flag && GodotObject.IsInstanceValid(conveyorBeltFeature?.conveyorBeltManager))
		{
			if (conveyorBeltFeature.IsSunType())
			{
				foreach (Node packetChild in conveyorBeltFeature.conveyorBeltManager.GetPacketChildren(GameSaveManager.Instance.GetConfigValue("MobilePreset").AsBool()))
				{
					if (packetChild is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && (!towerDefenseInGamePacketShow.useCost || instance.CanAffordSun(local, towerDefenseInGamePacketShow.itemCost)))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					foreach (TowerDefenseConveyorPacketConfig packet2 in conveyorBeltFeature.packetList)
					{
						TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packet2.name);
						if (GodotObject.IsInstanceValid(packetConfig) && instance.CanAffordSun(local, packetConfig.GetCost()))
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					foreach (TowerDefenseLevelPacketConfig packetPrioritySpawn in conveyorBeltFeature.packetPrioritySpawnList)
					{
						TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig(packetPrioritySpawn.packetName);
						if (GodotObject.IsInstanceValid(packetConfig2) && instance.CanAffordSun(local, packetConfig2.GetCost()))
						{
							flag = false;
							break;
						}
					}
				}
			}
			else if (conveyorBeltFeature.running || conveyorBeltFeature.conveyorBeltManager.GetPacketCount() > 0)
			{
				flag = false;
			}
		}
		if (!flag)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			foreach (TowerDefenseCharacter cleanCharacters in instance.characterRegistry.GetCleanCharactersList())
			{
				if (cleanCharacters is TowerDefenseCrater { HasPendingRevival: not false, RevivalCamp: TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE })
				{
					return false;
				}
			}
		}
		foreach (Variant item in instance.GetCampFriendly(TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE))
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !GodotObject.IsInstanceValid(towerDefenseCharacter.config) || towerDefenseCharacter.config.name != config.failureIgnoredZombieName)
			{
				return false;
			}
		}
		return true;
	}

	public override void Finish()
	{
		if (CanFinish() && levelControl != null && !levelControl.awardCreate)
		{
			ViewManager.Instance.FullScreenColorBlink(Colors.White, 0.2, rise: false);
			AudioManager.Instance.AudioPlay("WaveHuge");
			if (brainFeature != null && brainFeature.TryGetFirstAliveBrain(out var brain))
			{
				levelControl.awardPos = brain.GetLogicalGlobalPosition();
			}
			levelControl.AwardCreate(levelControl.awardPos);
		}
	}

	public override void PhysicsProcess(double delta)
	{
		if ((Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || levelControl == null)
		{
			return;
		}
		if (CanFinish() && CheckFinal())
		{
			levelControl.AwardCreate(levelControl.awardPos);
			return;
		}
		if (levelControl.hasSpawn)
		{
			levelControl.hasSpawn = false;
			return;
		}
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (physicsFrames >= _nextFailureCheckFrame)
		{
			_nextFailureCheckFrame = physicsFrames + (ulong)config.failureCheckIntervalFrames;
			if (CheckFail() && control != null)
			{
				control.GameFail(null);
			}
		}
	}

	public override void Destroy()
	{
		base.Destroy();
		if (_viewMapRunning || _viewBroadcastActive || _viewPacketShows.Count > 0)
		{
			RestoreViewMapState();
		}
		_viewMapRunning = false;
		_nextFailureCheckFrame = 0uL;
		config = null;
		mapFeature = null;
		progressFeature = null;
		seedBankFeature = null;
		packetBankFeature = null;
		brainFeature = null;
		conveyorBeltFeature = null;
		levelControl = null;
	}
}
