using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureConveyorBelt : TowerDefenseBattleFeature
{
	private const double MinimumSpawnInterval = 0.05;

	private static PackedScene _conveyorBeltManagerScene;

	public ConveyorBeltManager conveyorBeltManager;

	public TowerDefenseConveyorConfig config;

	public Array<TowerDefenseLevelPacketConfig> packetPrioritySpawnList = new Array<TowerDefenseLevelPacketConfig>();

	public Array<TowerDefenseConveyorPacketConfig> packetList = new Array<TowerDefenseConveyorPacketConfig>();

	public double timer;

	public bool running;

	private TowerDefenseBattleFeatureSun _sunFeature;

	private TowerDefenseBattleFeatureWave _waveFeature;

	private readonly EconomyAccountId _sunAccountId = EconomyAccountId.Local;

	private readonly Array<WeightPickItemBase> _weightPickItems = new Array<WeightPickItemBase>();

	private static PackedScene ConveyorBeltManagerScene => _conveyorBeltManagerScene ?? (_conveyorBeltManagerScene = GD.Load<PackedScene>("uid://u3bg1snrdknl"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseConveyorConfig();
		config.Init(data);
		conveyorBeltManager = ConveyorBeltManagerScene.Instantiate<ConveyorBeltManager>(PackedScene.GenEditState.Disabled);
		if (!conveyorBeltManager.TryBindSunAccount(_sunAccountId))
		{
			GD.PushError("[ConveyorBelt] Failed to bind the local Sun account.");
		}
		control.AddUIToTopBankContainer(conveyorBeltManager);
		packetPrioritySpawnList = config.packetPrioritySpawnList.Duplicate(deep: true);
		packetList = config.packetList.Duplicate(deep: true);
		conveyorBeltManager.Init(config.type);
	}

	public override void Destroy()
	{
		UnsubscribeWaveEvents();
		UnsubscribeSunDisplay();
		_weightPickItems.Clear();
		base.Destroy();
	}

	public override Task GameInit()
	{
		SubscribeWaveEvents();
		SubscribeSunDisplay();
		if (data.Count == 0)
		{
			conveyorBeltManager.Visible = false;
		}
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		SubscribeWaveEvents();
		SubscribeSunDisplay();
		if (data.Count > 0 && GodotObject.IsInstanceValid(conveyorBeltManager))
		{
			conveyorBeltManager.Visible = true;
		}
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (GodotObject.IsInstanceValid(conveyorBeltManager) && conveyorBeltManager.Visible)
		{
			running = true;
		}
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		if (GodotObject.IsInstanceValid(conveyorBeltManager) && conveyorBeltManager.Visible)
		{
			running = true;
		}
		return Task.CompletedTask;
	}

	public override void Process(double _delta)
	{
		if (!running)
		{
			return;
		}
		conveyorBeltManager.UpdateBeltAnimation(_delta);
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		int packetCount = conveyorBeltManager.GetPacketCount();
		if (packetCount < config.maxPacketCount)
		{
			timer += Math.Max(0.0, _delta);
			double spawnInterval = GetSpawnInterval(packetCount);
			if (!(timer < spawnInterval))
			{
				timer %= spawnInterval;
				Spawn();
			}
		}
	}

	public override Dictionary SyncSerialize()
	{
		return new Dictionary
		{
			["timer"] = timer,
			["running"] = running,
			["packetList"] = SerializePacketList()
		};
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		if (_data.ContainsKey("timer"))
		{
			timer = _data["timer"].AsDouble();
		}
		if (_data.ContainsKey("running"))
		{
			running = _data["running"].AsBool();
		}
		if (_data.ContainsKey("packetList"))
		{
			RestorePacketList(_data["packetList"].AsGodotArray(), force: false);
		}
	}

	public override Dictionary SaveFeature()
	{
		return new Dictionary
		{
			["timer"] = timer,
			["running"] = running,
			["packetList"] = SerializePacketList(),
			["priorityPacketList"] = SerializePriorityPacketList()
		};
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		timer = _data.GetValueOrDefault("timer", 0.0).AsDouble();
		running = _data.GetValueOrDefault("running", false).AsBool();
		if (_data.ContainsKey("priorityPacketList"))
		{
			RestorePriorityPacketList(_data["priorityPacketList"].AsGodotArray());
		}
		RestorePacketList(_data.GetValueOrDefault("packetList", new Godot.Collections.Array()).AsGodotArray(), force: true);
	}

	private Godot.Collections.Array SerializePacketList()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		if (!GodotObject.IsInstanceValid(conveyorBeltManager))
		{
			return array;
		}
		foreach (Node packetChild in conveyorBeltManager.GetPacketChildren(conveyorBeltManager.isMobileUI))
		{
			if (packetChild is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) && GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.config))
			{
				Dictionary dictionary = new Dictionary { ["saveKey"] = towerDefenseInGamePacketShow.config.saveKey };
				TowerDefensePacketRuntimeState.Write(towerDefenseInGamePacketShow.config, dictionary, "packetOverride", "canChangeCost", "changeCostList");
				array.Add(dictionary);
			}
		}
		return array;
	}

	private void RestorePacketList(Godot.Collections.Array packetListData, bool force)
	{
		if (!GodotObject.IsInstanceValid(conveyorBeltManager) || (!force && PacketListMatches(packetListData)))
		{
			return;
		}
		ClearPacketList();
		foreach (Variant packetListDatum in packetListData)
		{
			Dictionary dictionary = packetListDatum.AsGodotDictionary();
			if (TowerDefensePacketRuntimeState.TryCreate(dictionary.GetValueOrDefault("saveKey", "").AsString(), dictionary, "packetOverride", "canChangeCost", "changeCostList", out var packetConfig) && GodotObject.IsInstanceValid(packetConfig))
			{
				CreatePacketShow(packetConfig, config.type);
			}
		}
		conveyorBeltManager.ResetPacketPositions();
	}

	private bool PacketListMatches(Godot.Collections.Array packetListData)
	{
		Array<Node> packetChildren = conveyorBeltManager.GetPacketChildren(conveyorBeltManager.isMobileUI);
		if (packetChildren.Count != packetListData.Count)
		{
			return false;
		}
		for (int i = 0; i < packetChildren.Count; i++)
		{
			if (!(packetChildren[i] is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow) || !GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.config))
			{
				return false;
			}
			Dictionary dictionary = packetListData[i].AsGodotDictionary();
			if (towerDefenseInGamePacketShow.config.saveKey != dictionary.GetValueOrDefault("saveKey", "").AsString())
			{
				return false;
			}
		}
		return true;
	}

	private void ClearPacketList()
	{
		foreach (Node packetChild in conveyorBeltManager.GetPacketChildren(conveyorBeltManager.isMobileUI))
		{
			if (GodotObject.IsInstanceValid(packetChild))
			{
				Node parent = packetChild.GetParent();
				if (GodotObject.IsInstanceValid(parent))
				{
					parent.RemoveChild(packetChild);
				}
				packetChild.QueueFree();
			}
		}
	}

	private Godot.Collections.Array SerializePriorityPacketList()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseLevelPacketConfig packetPrioritySpawn in packetPrioritySpawnList)
		{
			if (GodotObject.IsInstanceValid(packetPrioritySpawn))
			{
				array.Add(packetPrioritySpawn.Export());
			}
		}
		return array;
	}

	private void RestorePriorityPacketList(Godot.Collections.Array priorityPacketData)
	{
		packetPrioritySpawnList.Clear();
		foreach (Variant priorityPacketDatum in priorityPacketData)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = new TowerDefenseLevelPacketConfig();
			towerDefenseLevelPacketConfig.Init(priorityPacketDatum);
			if (towerDefenseLevelPacketConfig.packetName != "")
			{
				packetPrioritySpawnList.Add(towerDefenseLevelPacketConfig);
			}
		}
	}

	public TowerDefenseInGamePacketShow Spawn()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return null;
		}
		TowerDefensePacketConfig towerDefensePacketConfig = null;
		if (packetPrioritySpawnList.Count <= 0)
		{
			_weightPickItems.Clear();
			foreach (TowerDefenseConveyorPacketConfig packet in packetList)
			{
				double num = packet.weight;
				int characterNum = TowerDefenseManager.Instance.GetCharacterNum(packet.name, containConveyor: true);
				if (packet.maxNum != -1 && characterNum >= packet.maxNum)
				{
					num *= packet.maxMagnification;
				}
				if (packet.minNum != -1 && characterNum < packet.minNum)
				{
					num *= packet.minMagnification;
				}
				_weightPickItems.Add(new WeightPickItemBase(packet, Math.Max(0, (int)num)));
			}
			WeightPickItemBase weightPickItemBase = WeightPickMathine.Pick(_weightPickItems);
			if (GodotObject.IsInstanceValid(weightPickItemBase))
			{
				towerDefensePacketConfig = (weightPickItemBase.item.AsGodotObject() as TowerDefenseConveyorPacketConfig).GetPacket();
			}
		}
		else
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = packetPrioritySpawnList[0];
			packetPrioritySpawnList.RemoveAt(0);
			towerDefensePacketConfig = towerDefenseLevelPacketConfig.GetPacket();
		}
		TowerDefenseInGamePacketShow result = SpawnPacket(towerDefensePacketConfig);
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			MultiPlayerManager.Instance.SendConveyorSpawn(towerDefensePacketConfig.saveKey, config.type);
		}
		return result;
	}

	public TowerDefenseInGamePacketShow SpawnPacket(TowerDefensePacketConfig packetConfig)
	{
		return CreatePacketShow(packetConfig, config.type);
	}

	public TowerDefenseInGamePacketShow SpawnPacketFromSync(TowerDefensePacketConfig packetConfig, string packetType)
	{
		return CreatePacketShow(packetConfig, packetType);
	}

	private TowerDefenseInGamePacketShow CreatePacketShow(TowerDefensePacketConfig packetConfig, string packetType)
	{
		if (!GodotObject.IsInstanceValid(conveyorBeltManager))
		{
			return null;
		}
		if (conveyorBeltManager.GetPacketCount() >= config.maxPacketCount)
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
			conveyorBeltManager.AddPacketToUI(towerDefenseInGamePacketShow);
			towerDefenseInGamePacketShow.Init(packetConfig);
			if (!towerDefenseInGamePacketShow.TryBindSunAccount(_sunAccountId))
			{
				GD.PushError("[ConveyorBelt] Packet already belongs to a different Sun account.");
				towerDefenseInGamePacketShow.QueueFree();
				return null;
			}
			towerDefenseInGamePacketShow.coldDownOpen = false;
			towerDefenseInGamePacketShow.onlyDraw = false;
			towerDefenseInGamePacketShow.plantOnce = true;
			towerDefenseInGamePacketShow.StartInit();
			towerDefenseInGamePacketShow.alive = true;
			bool start = (towerDefenseInGamePacketShow.showCost = (towerDefenseInGamePacketShow.useCost = packetType == "Sun"));
			towerDefenseInGamePacketShow.start = start;
			towerDefenseInGamePacketShow.Reset();
			towerDefenseInGamePacketShow.OnPressed += TowerDefenseManager.Instance.GetPacketPickControl().PickPacket;
			return towerDefenseInGamePacketShow;
		}
		return null;
	}

	private double GetSpawnInterval(int packetCount)
	{
		int num = Math.Max(1, config.intervalIncreaseEvery);
		double num2 = Math.Floor((double)Math.Max(0, packetCount) / (double)num);
		double num3 = Math.Max(0.0, 1.0 + num2 * config.intervalMagnification);
		return Math.Max(0.05, config.interval * num3);
	}

	private void SubscribeSunDisplay()
	{
		UnsubscribeSunDisplay();
		_sunFeature = GetFeature<TowerDefenseBattleFeatureSun>("Sun");
		if (_sunFeature != null)
		{
			_sunFeature.OnAccountSunChange += OnAccountSunChanged;
		}
		if (GodotObject.IsInstanceValid(conveyorBeltManager))
		{
			conveyorBeltManager.UpdateSunDisplay();
		}
	}

	private void SubscribeWaveEvents()
	{
		UnsubscribeWaveEvents();
		_waveFeature = GetFeature<TowerDefenseBattleFeatureWave>("Wave");
		if (_waveFeature != null)
		{
			_waveFeature.OnBigWaveBegin += WaveEventExecute;
		}
	}

	private void UnsubscribeWaveEvents()
	{
		if (_waveFeature != null)
		{
			_waveFeature.OnBigWaveBegin -= WaveEventExecute;
		}
		_waveFeature = null;
	}

	private void UnsubscribeSunDisplay()
	{
		if (_sunFeature != null)
		{
			_sunFeature.OnAccountSunChange -= OnAccountSunChanged;
		}
		_sunFeature = null;
	}

	private void OnAccountSunChanged(EconomyAccountId accountId, long _balance, SunTransactionReason _reason)
	{
		if (accountId == _sunAccountId && GodotObject.IsInstanceValid(conveyorBeltManager))
		{
			conveyorBeltManager.UpdateSunDisplay();
		}
	}

	public void WaveEventExecute(int bigWaveId)
	{
		if (!conveyorBeltManager.Visible || !running || config.waveEvent.Count <= bigWaveId)
		{
			return;
		}
		foreach (Variant item in config.waveEvent[bigWaveId].AsGodotArray())
		{
			item.As<TowerDefenseConveyorEventBase>()?.Execute();
		}
	}

	public bool IsSunType()
	{
		if (GodotObject.IsInstanceValid(config))
		{
			return config.type == "Sun";
		}
		return false;
	}

	public Dictionary UpdateConveyorVisibility(bool mobilePreset)
	{
		Dictionary obj = new Dictionary
		{
			["sun_visible"] = false,
			["sun_bar_visible"] = false,
			["mobile_sun_bar_visible"] = false,
			["use_mobile_sun_label"] = false
		};
		bool flag = IsSunType();
		obj["sun_visible"] = flag;
		obj["sun_bar_visible"] = flag;
		obj["mobile_sun_bar_visible"] = flag;
		obj["use_mobile_sun_label"] = flag && mobilePreset;
		return obj;
	}

	public Array<Node> GetPacketChildren()
	{
		return conveyorBeltManager.GetPacketChildren(conveyorBeltManager.isMobileUI);
	}
}
