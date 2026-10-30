using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureNpcTalk : TowerDefenseBattleFeature
{
	public delegate void TalkFinishEventHandler();

	public delegate void FinishEventHandler();

	private static PackedScene _npcTalkControl;

	private static PackedScene _npcCrazyDave;

	private static PackedScene _npcWeiWeiMi;

	public NpcTalkControl npcTalkControl;

	public NpcTalkConfig config;

	public int currentIndex;

	public Dictionary npcDictionary = new Dictionary();

	private bool _talkRunning;

	private bool _tutorialActive;

	private double _startDelay = 1.5;

	private string _temporaryBgm = "MainMenu";

	private static PackedScene NPC_TALK_CONTROL => _npcTalkControl ?? (_npcTalkControl = GD.Load<PackedScene>("uid://cjc8tc43kdv47"));

	private static PackedScene NPC_CRAZY_DAVE => _npcCrazyDave ?? (_npcCrazyDave = GD.Load<PackedScene>("uid://cvadcalsore2n"));

	private static PackedScene NPC_WEI_WEI_MI => _npcWeiWeiMi ?? (_npcWeiWeiMi = GD.Load<PackedScene>("uid://dolnrfw30x8ec"));

	public event TalkFinishEventHandler OnTalkFinish;

	public event FinishEventHandler OnFinish;

	public void EmitTalkFinish()
	{
		this.OnTalkFinish?.Invoke();
	}

	public void EmitFinish()
	{
		this.OnFinish?.Invoke();
	}

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		npcTalkControl = NPC_TALK_CONTROL.Instantiate<NpcTalkControl>(PackedScene.GenEditState.Disabled);
		npcTalkControl.npcTalkFeature = this;
		control.AddUI(npcTalkControl, 4);
		_startDelay = Math.Max(0.0, data.GetValueOrDefault("StartDelay", 1.5).AsDouble());
		_temporaryBgm = data.GetValueOrDefault("TemporaryBGM", "MainMenu").AsString();
		if (!data.GetValueOrDefault("isCustom", false).AsBool())
		{
			string text = data.GetValueOrDefault("TalkName", "").AsString();
			if (!(text != ""))
			{
				return;
			}
			NpcTalkConfig npcTalk = TowerDefenseManager.GetNpcTalk(text);
			if (!GodotObject.IsInstanceValid(npcTalk))
			{
				GD.PushWarning("[NpcTalk] Talk '" + text + "' is unavailable.");
				return;
			}
			config = npcTalk.Duplicate(deep: true) as NpcTalkConfig;
			if (GodotObject.IsInstanceValid(config))
			{
				config.Init();
			}
		}
		else
		{
			config = new NpcTalkConfig();
			config.Load(data);
		}
	}

	public override Task GameInit()
	{
		return Task.CompletedTask;
	}

	public async Task StartTalk()
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			EmitTalkFinish();
			return;
		}
		bool flag = false;
		if (config.saveKey != "")
		{
			flag = GameSaveManager.Instance.GetTutorialValue(config.saveKey);
		}
		TaskCompletionSource<bool> tcs;
		if (!flag)
		{
			if (_startDelay > 0.0)
			{
				await ToSignal(control.GetTree().CreateTimer(_startDelay, processAlways: false), SceneTreeTimer.SignalName.Timeout);
			}
			if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(npcTalkControl))
			{
				return;
			}
			TowerDefenseBattleFeatureBGM bgmFeature = GetFeature("BGM") as TowerDefenseBattleFeatureBGM;
			if (bgmFeature != null)
			{
				bgmFeature.StopBGM();
				if (_temporaryBgm != "")
				{
					bgmFeature.backgroundAudio = AudioManager.Instance.AudioPlay(_temporaryBgm, AudioManagerEnum.TYPE.MUSIC);
				}
			}
			currentIndex = 0;
			tcs = new TaskCompletionSource<bool>();
			OnFinish += FinishHandler;
			try
			{
				TalkNext();
				if (!(await WaitForTaskOrLifetime(tcs.Task)))
				{
					return;
				}
			}
			finally
			{
				OnFinish -= FinishHandler;
			}
			if (base.IsLifetimeActive)
			{
				bgmFeature?.PlayEntryBGM();
			}
		}
		if (base.IsLifetimeActive)
		{
			EmitTalkFinish();
		}
		void FinishHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public void TalkNext()
	{
		if (!_talkRunning && base.IsLifetimeActive)
		{
			_talkRunning = true;
			RunLifetimeTask(TalkNextAsync, "TalkNext");
		}
	}

	private async Task TalkNextAsync()
	{
		_ = 3;
		try
		{
			while (base.IsLifetimeActive && GodotObject.IsInstanceValid(config) && currentIndex < config.talkList.Count)
			{
				NpcTalkBaseConfig talk = config.talkList[currentIndex];
				NpcBase npc = await GetNpc(talk.npc);
				if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(npc) || !GodotObject.IsInstanceValid(npcTalkControl))
				{
					return;
				}
				npcTalkControl.ShowTalk(npc, talk);
				if (talk is NpcTalkHandConfig talk2)
				{
					npcTalkControl.ShowHand(npc, talk2);
				}
				if (npc.IsLeaveAnimation(talk.anime))
				{
					if (npcDictionary.ContainsKey(talk.npc))
					{
						NpcBase npcBase = npcDictionary[talk.npc].As<NpcBase>();
						if (GodotObject.IsInstanceValid(npcBase))
						{
							OnFinish -= npcBase.Finish;
						}
						npcDictionary.Remove(talk.npc);
					}
					NpcTalkTutorialConfig npcTalkTutorialConfig = talk as NpcTalkTutorialConfig;
					bool flag = npcTalkTutorialConfig != null;
					if (flag)
					{
						flag = !(await RunEmbeddedTutorial(npcTalkTutorialConfig));
					}
					if (flag)
					{
						return;
					}
				}
				else
				{
					NpcTalkTutorialConfig npcTalkTutorialConfig2 = talk as NpcTalkTutorialConfig;
					bool flag = npcTalkTutorialConfig2 != null;
					if (flag)
					{
						flag = !(await RunEmbeddedTutorial(npcTalkTutorialConfig2));
					}
					if (flag || !(await WaitForNpcTalkNext(npc)))
					{
						return;
					}
				}
				currentIndex++;
			}
			if (base.IsLifetimeActive)
			{
				EmitFinish();
			}
		}
		finally
		{
			_talkRunning = false;
		}
	}

	private async Task<bool> RunEmbeddedTutorial(NpcTalkTutorialConfig tutorialTalk)
	{
		if (!GodotObject.IsInstanceValid(TutorialManager.Instance))
		{
			return false;
		}
		_tutorialActive = true;
		TutorialManager.Instance.StartTutorial(tutorialTalk.tutorial);
		try
		{
			if (!(await WaitForTaskOrLifetime(TutorialManager.Instance.WaitForFinish())))
			{
				return false;
			}
			RestoreTutorialToolState();
			return true;
		}
		finally
		{
			_tutorialActive = false;
		}
	}

	private async Task<bool> WaitForNpcTalkNext(NpcBase npc)
	{
		if (!GodotObject.IsInstanceValid(npc))
		{
			return false;
		}
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		npc.OnTalkNext += TalkNextHandler;
		try
		{
			return await WaitForTaskOrLifetime(tcs.Task);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(npc))
			{
				npc.OnTalkNext -= TalkNextHandler;
			}
		}
		void TalkNextHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	private void RestoreTutorialToolState()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			TowerDefenseMap currentMap = instance.GetCurrentMap();
			if (GodotObject.IsInstanceValid(currentMap))
			{
				currentMap.BackShovel();
			}
			TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = GetFeature("Map") as TowerDefenseBattleFeatureMap;
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureMap) && GodotObject.IsInstanceValid(towerDefenseBattleFeatureMap.shovelManager))
			{
				towerDefenseBattleFeatureMap.shovelManager.ShovelReset();
			}
		}
	}

	public bool ContainsEmbeddedTutorial()
	{
		if (!GodotObject.IsInstanceValid(config))
		{
			return false;
		}
		foreach (NpcTalkBaseConfig talk in config.talkList)
		{
			if (talk is NpcTalkTutorialConfig)
			{
				return true;
			}
		}
		return false;
	}

	public async Task<NpcBase> GetNpc(string npcName = "CrazyDave")
	{
		if (npcDictionary.ContainsKey(npcName) && !GodotObject.IsInstanceValid(npcDictionary[npcName].AsGodotObject()))
		{
			npcDictionary.Remove(npcName);
		}
		TaskCompletionSource<bool> tcs;
		if (!npcDictionary.ContainsKey(npcName))
		{
			PackedScene packedScene = ((npcName == "CrazyDave") ? NPC_CRAZY_DAVE : ((!(npcName == "WeiWeiMi")) ? null : NPC_WEI_WEI_MI));
			PackedScene packedScene2 = packedScene;
			if (!GodotObject.IsInstanceValid(packedScene2))
			{
				GD.PushWarning("[NpcTalk] Unsupported NPC '" + npcName + "'.");
				return null;
			}
			if (!base.IsLifetimeActive || !GodotObject.IsInstanceValid(npcTalkControl))
			{
				return null;
			}
			NpcBase instance = packedScene2.Instantiate<NpcBase>(PackedScene.GenEditState.Disabled);
			tcs = new TaskCompletionSource<bool>();
			instance.OnNpcReady += NpcReadyHandler;
			npcDictionary[npcName] = instance;
			OnFinish += instance.Finish;
			try
			{
				npcTalkControl.AddNpc(instance);
				if (!(await WaitForTaskOrLifetime(tcs.Task)))
				{
					return null;
				}
			}
			finally
			{
				if (GodotObject.IsInstanceValid(instance))
				{
					instance.OnNpcReady -= NpcReadyHandler;
				}
			}
		}
		return npcDictionary[npcName].As<NpcBase>();
		void NpcReadyHandler()
		{
			tcs.TrySetResult(result: true);
		}
	}

	public override void Destroy()
	{
		base.Destroy();
		if (_tutorialActive && GodotObject.IsInstanceValid(TutorialManager.Instance))
		{
			TutorialManager.Instance.TutorialClear();
		}
		RestoreTutorialToolState();
		foreach (Variant value in npcDictionary.Values)
		{
			NpcBase npcBase = value.As<NpcBase>();
			if (GodotObject.IsInstanceValid(npcBase))
			{
				OnFinish -= npcBase.Finish;
			}
		}
		npcDictionary.Clear();
		this.OnFinish = null;
		this.OnTalkFinish = null;
		if (GodotObject.IsInstanceValid(npcTalkControl))
		{
			npcTalkControl.npcTalkFeature = null;
			npcTalkControl.QueueFree();
		}
		npcTalkControl = null;
		config = null;
		_talkRunning = false;
		_tutorialActive = false;
	}

	public override async Task GameEntry()
	{
		if (!control.hasProgress)
		{
			await StartTalk();
		}
	}
}
