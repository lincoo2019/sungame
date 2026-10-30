using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureSlotMachine : TowerDefenseBattleFeature
{
	private sealed class SpinResult
	{
		public string[] Items = new string[3] { "PlantPeaShooterSingle", "PlantWallnut", "PlantSnowPea" };

		public string MatchedItem = "";

		public int MatchedCount;

		public int RewardCount;
	}

	private enum SlotRewardType
	{
		Packet,
		Sun,
		BrainSun,
		Diamond,
		Coin
	}

	private sealed class SlotItem
	{
		public string Key = "";

		public string DisplayKey = "";

		public SlotRewardType Type;

		public int Amount = 1;

		public int Weight = 1;
	}

	private static PackedScene _slotMachineControlScene;

	public SlotMachineControl slotMachineControl;

	public bool open = true;

	public bool running;

	public bool autoStart = true;

	public int cost = 25;

	public double cooldown = 0.5;

	public double cooldownTimer;

	public double spinDuration = 1.25;

	public double packetAliveTime = 15.0;

	public int spinNum;

	public int clearSunReward;

	public string closedText = "CLOSED";

	public string waitText = "WAIT";

	public string hostOnlyText = "HOST ONLY";

	public string spinningText = "SPINNING";

	public string missText = "MISS";

	public string spinText = "SPIN";

	public string spinCostTextTemplate = "SPIN {0}";

	public string insufficientSunTextTemplate = "{0} SUN";

	public string cooldownTextTemplate = "{0}s";

	public string rewardCountTextTemplate = "{0}x {1}";

	public string sunRewardTextTemplate = "SUN {0}";

	public string brainRewardTextTemplate = "BRAIN {0}";

	public string diamondRewardTextTemplate = "DIAMOND {0}";

	public string coinRewardTextTemplate = "COIN {0}";

	private readonly List<SlotItem> _slotItems = new List<SlotItem>();

	private readonly System.Collections.Generic.Dictionary<string, SlotItem> _slotItemLookup = new System.Collections.Generic.Dictionary<string, SlotItem>(StringComparer.OrdinalIgnoreCase);

	private bool _spinInProgress;

	private bool _clearRewardCreated;

	private string _message = "";

	private double _messageTimer;

	private readonly EconomyAccountId _sunAccountId = EconomyAccountId.Local;

	private TowerDefenseBattleFeatureSun _sunFeature;

	private static PackedScene SlotMachineControlScene => _slotMachineControlScene ?? (_slotMachineControlScene = GD.Load<PackedScene>("res://Registry/Battle/Feature/SlotMachine/SlotMachineControl.tscn"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		ReadConfig();
		slotMachineControl = SlotMachineControlScene.Instantiate<SlotMachineControl>(PackedScene.GenEditState.Disabled);
		slotMachineControl.Init(this);
		slotMachineControl.Visible = open;
		control.AddUIToTopBankContainer(slotMachineControl);
	}

	public override Task GameInit()
	{
		SubscribeSunAccount();
		running = false;
		_clearRewardCreated = false;
		UpdateControl();
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		SubscribeSunAccount();
		UpdateControl();
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		running = open && autoStart;
		UpdateControl();
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		running = open && autoStart;
		UpdateControl();
		return Task.CompletedTask;
	}

	public override void Process(double delta)
	{
		bool flag = false;
		if (cooldownTimer > 0.0)
		{
			int num = (int)Math.Ceiling(cooldownTimer);
			cooldownTimer = Math.Max(0.0, cooldownTimer - delta);
			flag = num != (int)Math.Ceiling(cooldownTimer);
		}
		if (_messageTimer > 0.0)
		{
			_messageTimer = Math.Max(0.0, _messageTimer - delta);
			flag |= _messageTimer <= 0.0;
		}
		if (flag)
		{
			UpdateControl();
		}
	}

	public override void GameFail()
	{
		running = false;
		UpdateControl();
	}

	public override void Destroy()
	{
		UnsubscribeSunAccount();
		running = false;
		_spinInProgress = false;
		if (GodotObject.IsInstanceValid(slotMachineControl))
		{
			slotMachineControl.CancelPendingWork();
			slotMachineControl.QueueFree();
		}
		slotMachineControl = null;
		_slotItems.Clear();
		_slotItemLookup.Clear();
		base.Destroy();
	}

	public string[] GetSpinItemKeys()
	{
		if (_slotItems.Count == 0)
		{
			return new string[4] { "PlantPeaShooterSingle", "PlantWallnut", "PlantSnowPea", "PlantSunFlower" };
		}
		string[] array = new string[_slotItems.Count];
		for (int i = 0; i < _slotItems.Count; i++)
		{
			array[i] = _slotItems[i].Key;
		}
		return array;
	}

	public bool CanSpin()
	{
		if (!open || !running || _spinInProgress || cooldownTimer > 0.0)
		{
			return false;
		}
		if (cost <= 0)
		{
			return true;
		}
		return TowerDefenseManager.Instance.CanAffordSun(_sunAccountId, cost);
	}

	public void RequestSpin()
	{
		if (_spinInProgress)
		{
			return;
		}
		if (!open || !running)
		{
			SetMessage(waitText, 0.8);
		}
		else if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			SetMessage(hostOnlyText, 1.0);
		}
		else if (cooldownTimer > 0.0)
		{
			SetMessage(FormatText(cooldownTextTemplate, FormatNumber(Math.Ceiling(cooldownTimer))), 0.4);
		}
		else if (GodotObject.IsInstanceValid(slotMachineControl))
		{
			SunSpendReceipt receipt = null;
			if (cost > 0 && !TowerDefenseManager.Instance.TryBeginSunSpend(_sunAccountId, cost, out receipt))
			{
				SetMessage(FormatText(insufficientSunTextTemplate, FormatNumber(cost)), 1.0);
				AudioManager.Instance.AudioPlay("ShovelDeny");
				return;
			}
			SpinResult spinResult = RollSpinResult();
			receipt?.TryCommit();
			spinNum++;
			cooldownTimer = cooldown;
			_spinInProgress = true;
			SetMessage(spinningText, spinDuration);
			UpdateControl();
			slotMachineControl.PlaySpin(spinResult.Items, spinDuration, FinishSpin);
		}
	}

	public string GetStatusText()
	{
		if (!open)
		{
			return closedText;
		}
		if (_spinInProgress)
		{
			return spinningText;
		}
		if (_messageTimer > 0.0 && _message != "")
		{
			return _message;
		}
		if (!running)
		{
			return waitText;
		}
		if (cooldownTimer > 0.0)
		{
			return FormatText(cooldownTextTemplate, FormatNumber(Math.Ceiling(cooldownTimer)));
		}
		if (cost > 0)
		{
			if (!TowerDefenseManager.Instance.CanAffordSun(_sunAccountId, cost))
			{
				return FormatText(insufficientSunTextTemplate, FormatNumber(cost));
			}
			return FormatText(spinCostTextTemplate, FormatNumber(cost));
		}
		return spinText;
	}

	public override Dictionary SaveFeature()
	{
		return new Dictionary
		{
			["open"] = open,
			["running"] = running,
			["cooldownTimer"] = cooldownTimer,
			["spinNum"] = spinNum,
			["clearRewardCreated"] = _clearRewardCreated
		};
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		open = _data.GetValueOrDefault("open", open).AsBool();
		running = _data.GetValueOrDefault("running", running).AsBool();
		cooldownTimer = _data.GetValueOrDefault("cooldownTimer", 0.0).AsDouble();
		spinNum = _data.GetValueOrDefault("spinNum", 0).AsInt32();
		_clearRewardCreated = _data.GetValueOrDefault("clearRewardCreated", false).AsBool();
		if (GodotObject.IsInstanceValid(slotMachineControl))
		{
			slotMachineControl.Visible = open;
		}
		UpdateControl();
	}

	public override Dictionary SyncSerialize()
	{
		return new Dictionary
		{
			["open"] = open,
			["running"] = running,
			["cooldownTimer"] = cooldownTimer,
			["spinNum"] = spinNum,
			["clearRewardCreated"] = _clearRewardCreated
		};
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		open = _data.GetValueOrDefault("open", open).AsBool();
		running = _data.GetValueOrDefault("running", running).AsBool();
		cooldownTimer = _data.GetValueOrDefault("cooldownTimer", cooldownTimer).AsDouble();
		spinNum = _data.GetValueOrDefault("spinNum", spinNum).AsInt32();
		_clearRewardCreated = _data.GetValueOrDefault("clearRewardCreated", _clearRewardCreated).AsBool();
		if (GodotObject.IsInstanceValid(slotMachineControl))
		{
			slotMachineControl.Visible = open;
		}
		UpdateControl();
	}

	private void ReadConfig()
	{
		open = data.GetValueOrDefault("Open", true).AsBool();
		autoStart = data.GetValueOrDefault("AutoStart", true).AsBool();
		cost = Math.Max(0, data.GetValueOrDefault("Cost", data.GetValueOrDefault("SunCost", 25)).AsInt32());
		cooldown = Math.Max(0.0, data.GetValueOrDefault("Cooldown", 0.5).AsDouble());
		spinDuration = Math.Max(0.35, data.GetValueOrDefault("SpinDuration", 1.25).AsDouble());
		packetAliveTime = Math.Max(0.1, data.GetValueOrDefault("PacketAliveTime", 15.0).AsDouble());
		clearSunReward = Math.Max(0, data.GetValueOrDefault("ClearSunReward", data.GetValueOrDefault("PassSunReward", 0)).AsInt32());
		closedText = data.GetValueOrDefault("ClosedText", "CLOSED").AsString();
		waitText = data.GetValueOrDefault("WaitText", "WAIT").AsString();
		hostOnlyText = data.GetValueOrDefault("HostOnlyText", "HOST ONLY").AsString();
		spinningText = data.GetValueOrDefault("SpinningText", "SPINNING").AsString();
		missText = data.GetValueOrDefault("MissText", "MISS").AsString();
		spinText = data.GetValueOrDefault("SpinText", "SPIN").AsString();
		spinCostTextTemplate = data.GetValueOrDefault("SpinCostTextTemplate", "SPIN {0}").AsString();
		insufficientSunTextTemplate = data.GetValueOrDefault("InsufficientSunTextTemplate", "{0} SUN").AsString();
		cooldownTextTemplate = data.GetValueOrDefault("CooldownTextTemplate", "{0}s").AsString();
		rewardCountTextTemplate = data.GetValueOrDefault("RewardCountTextTemplate", "{0}x {1}").AsString();
		sunRewardTextTemplate = data.GetValueOrDefault("SunRewardTextTemplate", "SUN {0}").AsString();
		brainRewardTextTemplate = data.GetValueOrDefault("BrainRewardTextTemplate", "BRAIN {0}").AsString();
		diamondRewardTextTemplate = data.GetValueOrDefault("DiamondRewardTextTemplate", "DIAMOND {0}").AsString();
		coinRewardTextTemplate = data.GetValueOrDefault("CoinRewardTextTemplate", "COIN {0}").AsString();
		ReadPacketList(data.GetValueOrDefault("PacketList", new Godot.Collections.Array()).AsGodotArray());
	}

	private void ReadPacketList(Godot.Collections.Array packetListData)
	{
		_slotItems.Clear();
		_slotItemLookup.Clear();
		foreach (Variant packetListDatum in packetListData)
		{
			AddSlotItem(ParseSlotItem(packetListDatum.AsString()));
		}
		if (_slotItems.Count == 0)
		{
			AddSlotItem(ParseSlotItem("PlantPeaShooterSingle"));
			AddSlotItem(ParseSlotItem("PlantWallnut"));
			AddSlotItem(ParseSlotItem("PlantSnowPea"));
			AddSlotItem(ParseSlotItem("PlantSunFlower"));
		}
	}

	private void AddSlotItem(SlotItem item)
	{
		if (item != null && !(item.Key == "") && !_slotItemLookup.ContainsKey(item.Key))
		{
			_slotItems.Add(item);
			_slotItemLookup[item.Key] = item;
		}
	}

	private SpinResult RollSpinResult()
	{
		string[] array = new string[3];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = PickSlotItem().Key;
		}
		return BuildSpinResult(array);
	}

	private SpinResult BuildSpinResult(string[] items)
	{
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>();
		foreach (string text in items)
		{
			if (!(text == ""))
			{
				dictionary.TryGetValue(text, out var value);
				dictionary[text] = value + 1;
			}
		}
		string matchedItem = "";
		int num = 0;
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			if (item.Value > num)
			{
				matchedItem = item.Key;
				num = item.Value;
			}
		}
		int rewardCount = 0;
		if (num >= 3)
		{
			rewardCount = 3;
		}
		else if (num >= 2)
		{
			rewardCount = 1;
		}
		return new SpinResult
		{
			Items = items,
			MatchedItem = matchedItem,
			MatchedCount = num,
			RewardCount = rewardCount
		};
	}

	private SlotItem PickSlotItem()
	{
		if (_slotItems.Count == 0)
		{
			return ParseSlotItem("PlantPeaShooterSingle");
		}
		double num = 0.0;
		foreach (SlotItem slotItem in _slotItems)
		{
			num += (double)Math.Max(1, slotItem.Weight);
		}
		double num2 = ((num > 0.0) ? ((double)GD.Randf() * num) : 0.0);
		foreach (SlotItem slotItem2 in _slotItems)
		{
			num2 -= (double)Math.Max(1, slotItem2.Weight);
			if (num2 < 0.0)
			{
				return slotItem2;
			}
		}
		List<SlotItem> slotItems = _slotItems;
		return slotItems[slotItems.Count - 1];
	}

	private void FinishSpin(string[] finalItems)
	{
		_spinInProgress = false;
		SpinResult result = BuildSpinResult(finalItems);
		string message = ApplyReward(result);
		SetMessage(message, 1.6);
		UpdateControl();
	}

	private string ApplyReward(SpinResult result)
	{
		Vector2 rewardSpawnPosition = GetRewardSpawnPosition();
		if (result.MatchedCount < 2 || result.RewardCount <= 0)
		{
			return missText;
		}
		if (result.RewardCount > 0)
		{
			SlotItem slotItem = GetSlotItem(result.MatchedItem);
			for (int i = 0; i < result.RewardCount; i++)
			{
				float x = (float)(((double)i - (double)(result.RewardCount - 1) * 0.5) * 46.0);
				Vector2 pos = rewardSpawnPosition + new Vector2(x, 0f);
				SpawnSlotReward(slotItem, pos);
			}
			return FormatText(rewardCountTextTemplate, FormatNumber(result.RewardCount), GetRewardLabel(slotItem));
		}
		return missText;
	}

	public void CreateLevelClearReward(Vector2 pos)
	{
		if (!_clearRewardCreated && clearSunReward > 0)
		{
			_clearRewardCreated = true;
			TowerDefenseManager.Instance.SunCreate(_sunAccountId, pos + new Vector2(0f, -36f), clearSunReward, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, new Vector2(0f, -320f));
		}
	}

	private SlotItem GetSlotItem(string itemKey)
	{
		if (itemKey != "" && _slotItemLookup.TryGetValue(itemKey, out var value))
		{
			return value;
		}
		return ParseSlotItem(itemKey);
	}

	private void SpawnSlotReward(SlotItem reward, Vector2 pos)
	{
		switch (reward.Type)
		{
		case SlotRewardType.Sun:
			TowerDefenseManager.Instance.SunCreate(_sunAccountId, pos, reward.Amount, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, CreateRewardVelocity());
			break;
		case SlotRewardType.BrainSun:
			TowerDefenseManager.Instance.BrainSunCreate(_sunAccountId, pos, reward.Amount, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, 0.0, CreateRewardVelocity());
			break;
		case SlotRewardType.Diamond:
			TowerDefenseManager.Instance.CoinCreate(pos, Math.Max(1, reward.Amount) * 1000, 0.0, CreateRewardVelocity(), 980.0);
			break;
		case SlotRewardType.Coin:
			TowerDefenseManager.Instance.CoinCreate(pos, Math.Max(10, reward.Amount), 0.0, CreateRewardVelocity(), 980.0);
			break;
		default:
			SpawnPacketReward(reward.Key, pos);
			break;
		}
	}

	private void SpawnPacketReward(string packetName, Vector2 pos)
	{
		if (!(packetName == ""))
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
			if (!GodotObject.IsInstanceValid(packetConfig))
			{
				GD.PushWarning("[SlotMachine] Invalid packet reward: " + packetName);
			}
			else
			{
				TowerDefenseManager.Instance.SpawnPacket(_sunAccountId, packetConfig, pos, packetAliveTime, isFall: false);
			}
		}
	}

	private static Vector2 CreateRewardVelocity()
	{
		return new Vector2((float)GD.RandRange(-50.0, 50.0), -400f);
	}

	private Vector2 GetRewardSpawnPosition()
	{
		if (TowerDefenseManager.GetMapFeature() == null)
		{
			return new Vector2(400f, 120f);
		}
		Vector2 mapGridBeginPos = TowerDefenseManager.Instance.GetMapGridBeginPos();
		return new Vector2((float)(((double)mapGridBeginPos.X + TowerDefenseManager.Instance.GetMapGroundRight()) * 0.5), mapGridBeginPos.Y - 60f);
	}

	private static string GetShortPacketName(string packetName)
	{
		switch (packetName)
		{
		case "PlantPeaShooterSingle":
			return "PEA";
		case "PlantWallnut":
			return "NUT";
		case "PlantSnowPea":
			return "ICE";
		case "PlantSunFlower":
			return "SUN";
		case "PlantPresentBox":
			return "BOX";
		default:
		{
			string result;
			if (!packetName.StartsWith("Plant", StringComparison.OrdinalIgnoreCase))
			{
				result = packetName;
			}
			else
			{
				result = packetName.Substring(5, packetName.Length - 5);
			}
			return result;
		}
		}
	}

	private string GetRewardLabel(SlotItem reward)
	{
		return reward.Type switch
		{
			SlotRewardType.Sun => FormatText(sunRewardTextTemplate, FormatNumber(reward.Amount)), 
			SlotRewardType.BrainSun => FormatText(brainRewardTextTemplate, FormatNumber(reward.Amount)), 
			SlotRewardType.Diamond => FormatText(diamondRewardTextTemplate, FormatNumber(reward.Amount)), 
			SlotRewardType.Coin => FormatText(coinRewardTextTemplate, FormatNumber(reward.Amount)), 
			_ => GetShortPacketName(reward.Key), 
		};
	}

	private static string FormatNumber(double value)
	{
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	private static string FormatText(string template, params string[] values)
	{
		string text = template ?? "";
		for (int i = 0; i < values.Length; i++)
		{
			text = text.Replace($"{{{i}}}", values[i] ?? "", StringComparison.Ordinal);
		}
		return text;
	}

	private static SlotItem ParseSlotItem(string rawItem)
	{
		string text = (rawItem ?? "").Trim();
		if (text == "")
		{
			return new SlotItem();
		}
		int weight = 1;
		bool flag = false;
		int num = text.LastIndexOf('@');
		if (num >= 0)
		{
			string text2 = text;
			int num2 = num + 1;
			if (int.TryParse(text2.Substring(num2, text2.Length - num2).Trim(), out var result) && result > 0)
			{
				weight = result;
				flag = true;
			}
			text = text.Substring(0, num).Trim();
		}
		string[] array = text.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		string text3 = ((array.Length != 0) ? array[0] : text).ToUpperInvariant();
		if (!flag && array.Length >= 3 && int.TryParse(array[2], out var result2) && result2 > 0)
		{
			weight = result2;
		}
		switch (text3)
		{
		case "SUN":
			return CreateValueReward(SlotRewardType.Sun, "SUN", array, 25, weight, "PlantSunFlower");
		case "BRAIN":
		case "BRAINSUN":
		case "SUNBRAIN":
			return CreateValueReward(SlotRewardType.BrainSun, "BRAIN", array, 25, weight, "ItemBrain");
		case "DIAMOND":
			return CreateValueReward(SlotRewardType.Diamond, "DIAMOND", array, 1, weight, "PlantDiamondseed");
		case "COIN":
			return CreateValueReward(SlotRewardType.Coin, "COIN", array, 1000, weight, "PlantDiamondseed");
		default:
			return CreatePacketReward(array, text, weight);
		}
	}

	private static SlotItem CreateValueReward(SlotRewardType type, string prefix, string[] parts, int defaultAmount, int weight, string displayKey)
	{
		int num = defaultAmount;
		if (parts.Length >= 2 && int.TryParse(parts[1], out var result) && result > 0)
		{
			num = result;
		}
		return new SlotItem
		{
			Key = $"{prefix}:{num}",
			DisplayKey = displayKey,
			Type = type,
			Amount = num,
			Weight = Math.Max(1, weight)
		};
	}

	private static SlotItem CreatePacketReward(string[] parts, string token, int weight)
	{
		string text = token;
		if (parts.Length == 2 && int.TryParse(parts[1], out var result) && result > 0)
		{
			text = parts[0];
			weight = result;
		}
		return new SlotItem
		{
			Key = text,
			DisplayKey = text,
			Type = SlotRewardType.Packet,
			Amount = 1,
			Weight = Math.Max(1, weight)
		};
	}

	private void SetMessage(string message, double duration)
	{
		_message = message;
		_messageTimer = duration;
		UpdateControl();
	}

	private void SubscribeSunAccount()
	{
		UnsubscribeSunAccount();
		_sunFeature = GetFeature<TowerDefenseBattleFeatureSun>("Sun");
		if (_sunFeature != null)
		{
			_sunFeature.OnAccountSunChange += OnAccountSunChanged;
		}
	}

	private void UnsubscribeSunAccount()
	{
		if (_sunFeature != null)
		{
			_sunFeature.OnAccountSunChange -= OnAccountSunChanged;
		}
		_sunFeature = null;
	}

	private void OnAccountSunChanged(EconomyAccountId accountId, long _balance, SunTransactionReason _reason)
	{
		if (accountId == _sunAccountId)
		{
			UpdateControl();
		}
	}

	private void UpdateControl()
	{
		if (GodotObject.IsInstanceValid(slotMachineControl))
		{
			slotMachineControl.SetStatusText(GetStatusText());
			slotMachineControl.SetInteractable(CanSpin());
		}
	}
}
