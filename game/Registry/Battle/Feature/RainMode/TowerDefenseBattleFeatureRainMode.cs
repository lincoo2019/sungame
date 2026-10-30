using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureRainMode : TowerDefenseBattleFeature
{
	private readonly struct WeightedRainPacket(TowerDefenseRainModePacketConfig config, int weight)
	{
		public readonly TowerDefenseRainModePacketConfig Config = config;

		public readonly int Weight = weight;
	}

	private const double MinimumSpawnInterval = 0.05;

	private const double MinimumPacketAliveTime = 0.1;

	private static PackedScene _rainManagerScene;

	public RainManager rainManager;

	public TowerDefenseRainModeConfig config;

	private readonly List<TowerDefenseRainModePacketConfig> _packetList = new List<TowerDefenseRainModePacketConfig>();

	private readonly List<WeightedRainPacket> _weightedPackets = new List<WeightedRainPacket>();

	private readonly List<TowerDefenseInGamePacketShow> _activePackets = new List<TowerDefenseInGamePacketShow>();

	private TowerDefenseBattleFeatureSun _sunFeature;

	private readonly EconomyAccountId _sunAccountId = EconomyAccountId.Local;

	public bool running;

	public double timer;

	private static PackedScene RainManagerScene => _rainManagerScene ?? (_rainManagerScene = GD.Load<PackedScene>("res://Registry/Battle/Feature/RainMode/RainManager/RainManager.tscn"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseRainModeConfig();
		config.Init(data);
		rainManager = RainManagerScene.Instantiate<RainManager>(PackedScene.GenEditState.Disabled);
		control.AddUIToTopBankContainer(rainManager);
	}

	public override Task GameInit()
	{
		InitializeRuntime();
		if (data.Count == 0)
		{
			rainManager.Visible = false;
		}
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		InitializeRuntime();
		return Task.CompletedTask;
	}

	private void InitializeRuntime()
	{
		_packetList.Clear();
		foreach (Variant packet in config.packetList)
		{
			TowerDefenseRainModePacketConfig item = (TowerDefenseRainModePacketConfig)(GodotObject)packet;
			_packetList.Add(item);
		}
		UnsubscribeSunDisplay();
		_sunFeature = GetFeature<TowerDefenseBattleFeatureSun>("Sun");
		if (_sunFeature != null)
		{
			_sunFeature.OnAccountSunChange += OnAccountSunChanged;
		}
		long sun = TowerDefenseManager.Instance.GetSun(_sunAccountId);
		rainManager.Init(config.type, sun);
	}

	public override Task GameReady()
	{
		running = false;
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (GodotObject.IsInstanceValid(rainManager) && rainManager.Visible)
		{
			running = true;
		}
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		return GameStart();
	}

	public override void Process(double delta)
	{
		if (running && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && _packetList.Count > 0)
		{
			double num = Math.Max(0.05, config.interval);
			timer += Math.Max(0.0, delta);
			if (!(timer < num))
			{
				timer %= num;
				Spawn();
			}
		}
	}

	public TowerDefenseInGamePacketShow Spawn()
	{
		if (_packetList.Count <= 0)
		{
			return null;
		}
		_weightedPackets.Clear();
		int num = 0;
		foreach (TowerDefenseRainModePacketConfig packet in _packetList)
		{
			double num2 = packet.weight;
			int characterNum = TowerDefenseManager.Instance.GetCharacterNum(packet.name);
			if (packet.maxNum != -1 && characterNum >= packet.maxNum)
			{
				num2 *= packet.maxMagnification;
			}
			if (packet.minNum != -1 && characterNum < packet.minNum)
			{
				num2 *= packet.minMagnification;
			}
			int num3 = Mathf.Max(0, (int)num2);
			if (num3 != 0)
			{
				_weightedPackets.Add(new WeightedRainPacket(packet, num3));
				num += num3;
			}
		}
		if (num <= 0)
		{
			return null;
		}
		int num4 = (int)(GD.Randi() % (uint)num);
		foreach (WeightedRainPacket weightedPacket in _weightedPackets)
		{
			num4 -= weightedPacket.Weight;
			if (num4 < 0)
			{
				return SpawnPacket(weightedPacket.Config.GetPacket());
			}
		}
		return null;
	}

	public TowerDefenseInGamePacketShow SpawnPacket(TowerDefensePacketConfig packetConfig)
	{
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return null;
		}
		double num = GD.RandRange(TowerDefenseManager.Instance.GetMapGridBeginPos().X, TowerDefenseManager.Instance.GetMapGroundRight());
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.Instance.SpawnPacket(_sunAccountId, packetConfig, new Vector2((float)num, TowerDefenseManager.Instance.GetMapGridBeginPos().Y - 100f), Math.Max(0.1, config.aliveTime), isFall: true, config.type == "Sun");
		if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			_activePackets.Add(towerDefenseInGamePacketShow);
		}
		return towerDefenseInGamePacketShow;
	}

	public bool IsSunType()
	{
		if (GodotObject.IsInstanceValid(config))
		{
			return config.type == "Sun";
		}
		return false;
	}

	public Dictionary UpdateRainVisibility(bool mobilePreset)
	{
		bool flag = IsSunType();
		return new Dictionary
		{
			["sun_visible"] = flag,
			["sun_bar_visible"] = flag,
			["mobile_sun_bar_visible"] = flag,
			["use_mobile_sun_label"] = flag && mobilePreset
		};
	}

	public override Dictionary SaveFeature()
	{
		return SerializeRuntimeState(includePackets: true);
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		timer = Math.Max(0.0, _data.GetValueOrDefault("timer", 0.0).AsDouble());
		running = _data.GetValueOrDefault("running", false).AsBool();
		if (GodotObject.IsInstanceValid(rainManager))
		{
			rainManager.Visible = _data.GetValueOrDefault("rain_visible", false).AsBool();
		}
		RestoreActivePackets(_data.GetValueOrDefault("packets", new Godot.Collections.Array()).AsGodotArray());
	}

	public override Dictionary SyncSerialize()
	{
		return SerializeRuntimeState(includePackets: false);
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		timer = Math.Max(0.0, _data.GetValueOrDefault("timer", timer).AsDouble());
		running = _data.GetValueOrDefault("running", running).AsBool();
		if (GodotObject.IsInstanceValid(rainManager))
		{
			rainManager.Visible = _data.GetValueOrDefault("rain_visible", rainManager.Visible).AsBool();
		}
	}

	private Dictionary SerializeRuntimeState(bool includePackets)
	{
		Dictionary dictionary = new Dictionary
		{
			["timer"] = timer,
			["running"] = running,
			["rain_visible"] = GodotObject.IsInstanceValid(rainManager) && rainManager.Visible
		};
		if (includePackets)
		{
			dictionary["packets"] = SerializeActivePackets();
		}
		return dictionary;
	}

	private Godot.Collections.Array SerializeActivePackets()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		for (int num = _activePackets.Count - 1; num >= 0; num--)
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = _activePackets[num];
			if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow) || towerDefenseInGamePacketShow.IsQueuedForDeletion() || !GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.config))
			{
				_activePackets.RemoveAt(num);
			}
			else
			{
				Dictionary dictionary = new Dictionary
				{
					["saveKey"] = towerDefenseInGamePacketShow.config.saveKey,
					["position"] = towerDefenseInGamePacketShow.GlobalPosition,
					["aliveTime"] = towerDefenseInGamePacketShow.aliveTime,
					["aliveTimer"] = towerDefenseInGamePacketShow.aliveTimer,
					["useCost"] = towerDefenseInGamePacketShow.useCost
				};
				TowerDefensePacketRuntimeState.Write(towerDefenseInGamePacketShow.config, dictionary, "packetOverride", "canChangeCost", "changeCostList");
				array.Add(dictionary);
			}
		}
		return array;
	}

	private void RestoreActivePackets(Godot.Collections.Array packetStates)
	{
		_activePackets.Clear();
		foreach (Variant packetState in packetStates)
		{
			Dictionary dictionary = packetState.AsGodotDictionary();
			if (!TowerDefensePacketRuntimeState.TryCreate(dictionary.GetValueOrDefault("saveKey", "").AsString(), dictionary, "packetOverride", "canChangeCost", "changeCostList", out var packetConfig) || !GodotObject.IsInstanceValid(packetConfig))
			{
				continue;
			}
			Vector2 vector = dictionary.GetValueOrDefault("position", Vector2.Zero).AsVector2();
			double num = Math.Max(0.1, dictionary.GetValueOrDefault("aliveTime", config.aliveTime).AsDouble());
			bool useCost = dictionary.GetValueOrDefault("useCost", config.type == "Sun").AsBool();
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.Instance.SpawnPacket(_sunAccountId, packetConfig, vector, num, isFall: false, useCost, useRandf: false, Vector2.Zero);
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.GlobalPosition = vector;
				towerDefenseInGamePacketShow.savePos = vector;
				towerDefenseInGamePacketShow.height = -1.0;
				towerDefenseInGamePacketShow.aliveTimer = Mathf.Clamp(dictionary.GetValueOrDefault("aliveTimer", 0.0).AsDouble(), 0.0, num);
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow.moveComponent))
				{
					towerDefenseInGamePacketShow.moveComponent.QueueFree();
				}
				_activePackets.Add(towerDefenseInGamePacketShow);
			}
		}
	}

	private void OnAccountSunChanged(EconomyAccountId accountId, long balance, SunTransactionReason _reason)
	{
		if (accountId == _sunAccountId && GodotObject.IsInstanceValid(rainManager))
		{
			rainManager.SetSunDisplay(balance);
		}
	}

	public override void Destroy()
	{
		base.Destroy();
		UnsubscribeSunDisplay();
		_packetList.Clear();
		_weightedPackets.Clear();
		_activePackets.Clear();
		running = false;
		if (GodotObject.IsInstanceValid(rainManager))
		{
			rainManager.QueueFree();
		}
		rainManager = null;
	}

	private void UnsubscribeSunDisplay()
	{
		if (_sunFeature != null)
		{
			_sunFeature.OnAccountSunChange -= OnAccountSunChanged;
		}
		_sunFeature = null;
	}
}
