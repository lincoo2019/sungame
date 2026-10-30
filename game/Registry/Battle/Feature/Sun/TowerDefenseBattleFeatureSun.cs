using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureSun : TowerDefenseBattleFeature
{
	public delegate void SunCollectEventHandler(long num);

	public delegate void SunChangeEventHandler(long num);

	public delegate void AccountSunCollectEventHandler(EconomyAccountId accountId, long num);

	public delegate void AccountSunChangeEventHandler(EconomyAccountId accountId, long balance, SunTransactionReason reason);

	private const int AccountSchemaVersion = 1;

	private const long LegacyDefaultSun = 50L;

	public TowerDefenseLevelSunManagerConfig config;

	private TowerDefenseBattleFeatureMap _mapFeature;

	public string type = "Normal";

	public double spawnInterval = 25.0;

	public long spawnNum = 25L;

	public TowerDefenseEnum.SUN_MOVING_METHOD movingMethod;

	public bool isRunning;

	public double timer;

	private readonly System.Collections.Generic.Dictionary<EconomyAccountId, long> _accountBalances = new System.Collections.Generic.Dictionary<EconomyAccountId, long>();

	private readonly System.Collections.Generic.Dictionary<long, SunSpendReceipt> _activeSpendReceipts = new System.Collections.Generic.Dictionary<long, SunSpendReceipt>();

	private long _ledgerGeneration;

	private long _nextSpendTransactionId;

	public EconomyAccountId LocalAccountId => EconomyAccountId.Local;

	public long sunNum
	{
		get
		{
			if (!TryGetSun(LocalAccountId, out var balance))
			{
				return 50L;
			}
			return balance;
		}
		set
		{
			if (!SetSun(LocalAccountId, value, SunTransactionReason.Legacy))
			{
				RegisterAccount(LocalAccountId, value);
			}
		}
	}

	public event SunCollectEventHandler OnSunCollect;

	public event SunChangeEventHandler OnSunChange;

	public event AccountSunCollectEventHandler OnAccountSunCollect;

	public event AccountSunChangeEventHandler OnAccountSunChange;

	public void EmitSunCollect(long num)
	{
		EmitSunCollect(LocalAccountId, num);
	}

	public void EmitSunChange(long num)
	{
		EmitSunChange(LocalAccountId, num, SunTransactionReason.Legacy);
	}

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseLevelSunManagerConfig();
		config.Init(data);
		InvalidateSpendReceipts();
		_accountBalances.Clear();
		RegisterAccount(LocalAccountId, config.begin);
	}

	public override Task GameInit()
	{
		_mapFeature = GetFeature<TowerDefenseBattleFeatureMap>("Map");
		SetSun(LocalAccountId, config.begin, SunTransactionReason.Initialization);
		return Task.CompletedTask;
	}

	public override Task GameInitFromProgress()
	{
		_mapFeature = GetFeature<TowerDefenseBattleFeatureMap>("Map");
		return Task.CompletedTask;
	}

	public override Task GameStart()
	{
		if (GetFeature("Wave") is TowerDefenseBattleFeatureWave { isSurvival: not false } towerDefenseBattleFeatureWave && GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave.survivalRunner) && towerDefenseBattleFeatureWave.survivalRunner.roundNum > 0)
		{
			type = config.type;
			spawnInterval = config.spawnInterval;
			spawnNum = config.spawnNum;
			movingMethod = config.movingMethod;
			timer = spawnInterval - 6.0;
		}
		else
		{
			SunInit(config);
		}
		isRunning = true;
		return Task.CompletedTask;
	}

	public override Task GameStartFromProgress()
	{
		isRunning = true;
		return Task.CompletedTask;
	}

	public override void GameFail()
	{
		isRunning = false;
	}

	public override void Process(double delta)
	{
		if (isRunning && config.open && GodotObject.IsInstanceValid(_mapFeature) && (!_mapFeature.config.isNight || _mapFeature.config.useSunFall))
		{
			if (timer < spawnInterval)
			{
				timer += delta;
				return;
			}
			SunCreate();
			timer = 0.0;
		}
	}

	public override Dictionary SyncSerialize()
	{
		return new Dictionary();
	}

	public override void SyncDeserialize(Dictionary _data)
	{
	}

	public override Dictionary SaveFeature()
	{
		Dictionary dictionary = new Dictionary { ["local"] = sunNum };
		return new Dictionary
		{
			["timer"] = timer,
			["isRunning"] = isRunning,
			["spawnInterval"] = spawnInterval,
			["spawnNum"] = spawnNum,
			["type"] = type,
			["movingMethod"] = (int)movingMethod,
			["sunNum"] = sunNum,
			["accountSchemaVersion"] = 1,
			["accounts"] = dictionary
		};
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		timer = _data.GetValueOrDefault("timer", 0.0).AsDouble();
		isRunning = _data.GetValueOrDefault("isRunning", false).AsBool();
		spawnInterval = _data.GetValueOrDefault("spawnInterval", spawnInterval).AsDouble();
		spawnNum = _data.GetValueOrDefault("spawnNum", spawnNum).AsInt64();
		type = _data.GetValueOrDefault("type", type).AsString();
		movingMethod = (TowerDefenseEnum.SUN_MOVING_METHOD)_data.GetValueOrDefault("movingMethod", (int)movingMethod).AsInt32();
		long num = _data.GetValueOrDefault("sunNum", sunNum).AsInt64();
		if (_data.TryGetValue("accounts", out var value) && value.VariantType == Variant.Type.Dictionary && value.AsGodotDictionary().TryGetValue("local", out var value2))
		{
			num = value2.AsInt64();
		}
		InvalidateSpendReceipts();
		_accountBalances.Clear();
		RegisterAccount(LocalAccountId, num);
		EmitSunChange(LocalAccountId, num, SunTransactionReason.Load);
	}

	public bool RegisterAccount(EconomyAccountId accountId, long initialSun)
	{
		if (!accountId.IsValid)
		{
			return false;
		}
		if (_accountBalances.ContainsKey(accountId))
		{
			return true;
		}
		_accountBalances.Add(accountId, initialSun);
		return true;
	}

	public bool HasAccount(EconomyAccountId accountId)
	{
		if (accountId.IsValid)
		{
			return _accountBalances.ContainsKey(accountId);
		}
		return false;
	}

	public bool TryGetSun(EconomyAccountId accountId, out long balance)
	{
		balance = 0L;
		if (accountId.IsValid)
		{
			return _accountBalances.TryGetValue(accountId, out balance);
		}
		return false;
	}

	public long GetSun(EconomyAccountId accountId)
	{
		if (!TryGetSun(accountId, out var balance))
		{
			return -1L;
		}
		return balance;
	}

	public bool CreditSun(EconomyAccountId accountId, long amount, SunTransactionReason reason = SunTransactionReason.Reward)
	{
		if (amount < 0)
		{
			return false;
		}
		return ApplyBalanceDelta(accountId, amount, reason);
	}

	internal bool ApplyCollectedSunValue(EconomyAccountId accountId, long value)
	{
		return ApplyBalanceDelta(accountId, value, (value >= 0) ? SunTransactionReason.Collection : SunTransactionReason.Penalty);
	}

	private bool ApplyBalanceDelta(EconomyAccountId accountId, long delta, SunTransactionReason reason)
	{
		if (!TryGetSun(accountId, out var balance))
		{
			return false;
		}
		if (delta == 0L)
		{
			return true;
		}
		long num = SaturatingAdd(balance, delta);
		long num2 = num - balance;
		_accountBalances[accountId] = num;
		if (num2 > 0 && reason == SunTransactionReason.Collection)
		{
			EmitSunCollect(accountId, num2);
		}
		EmitSunChange(accountId, num, reason);
		return true;
	}

	public bool TrySpendSun(EconomyAccountId accountId, long amount, SunTransactionReason reason = SunTransactionReason.Cost)
	{
		if (amount < 0 || !TryGetSun(accountId, out var balance) || balance < amount)
		{
			return false;
		}
		if (amount == 0L)
		{
			return true;
		}
		long num = balance - amount;
		_accountBalances[accountId] = num;
		EmitSunChange(accountId, num, reason);
		return true;
	}

	public bool TryBeginSunSpend(EconomyAccountId accountId, long amount, out SunSpendReceipt receipt, SunTransactionReason reason = SunTransactionReason.Cost)
	{
		receipt = null;
		if (amount >= 0)
		{
			if (!TrySpendSun(accountId, amount, reason))
			{
				return false;
			}
		}
		else if (amount == -9223372036854775808L || !CreditSun(accountId, -amount, reason))
		{
			return false;
		}
		long num = NextSpendTransactionId();
		receipt = new SunSpendReceipt(this, _ledgerGeneration, num, accountId, amount);
		_activeSpendReceipts.Add(num, receipt);
		return true;
	}

	internal bool TryCommitSunSpend(SunSpendReceipt receipt)
	{
		if (!TryGetActiveSpendReceipt(receipt, out var transactionId))
		{
			return false;
		}
		_activeSpendReceipts.Remove(transactionId);
		receipt.Complete(SunSpendReceiptState.Committed);
		return true;
	}

	internal bool TryRollbackSunSpend(SunSpendReceipt receipt)
	{
		if (!TryGetActiveSpendReceipt(receipt, out var transactionId))
		{
			return false;
		}
		if (!((receipt.Amount >= 0) ? CreditSun(receipt.AccountId, receipt.Amount, SunTransactionReason.Refund) : ApplyBalanceDelta(receipt.AccountId, receipt.Amount, SunTransactionReason.Refund)))
		{
			return false;
		}
		_activeSpendReceipts.Remove(transactionId);
		receipt.Complete(SunSpendReceiptState.RolledBack);
		return true;
	}

	public bool SetSun(EconomyAccountId accountId, long value, SunTransactionReason reason = SunTransactionReason.Debug)
	{
		if (!HasAccount(accountId))
		{
			return false;
		}
		_accountBalances[accountId] = value;
		EmitSunChange(accountId, value, reason);
		return true;
	}

	public void AddSun(long num)
	{
		long balance;
		if (num >= 0)
		{
			CreditSun(LocalAccountId, num, SunTransactionReason.Collection);
		}
		else if (TryGetSun(LocalAccountId, out balance))
		{
			long num2 = SaturatingAdd(balance, num);
			_accountBalances[LocalAccountId] = num2;
			EmitSunCollect(LocalAccountId, num);
			EmitSunChange(LocalAccountId, num2, SunTransactionReason.Legacy);
		}
	}

	public void UseSun(long num)
	{
		if (TryGetSun(LocalAccountId, out var balance))
		{
			SetSun(LocalAccountId, SaturatingSubtract(balance, num), SunTransactionReason.Legacy);
		}
	}

	public void SetSun(long num)
	{
		SetSun(LocalAccountId, num, SunTransactionReason.Legacy);
	}

	public void SunInit(TowerDefenseLevelSunManagerConfig _config)
	{
		SetSun(LocalAccountId, _config.begin, SunTransactionReason.Initialization);
		type = _config.type;
		spawnInterval = _config.spawnInterval;
		spawnNum = _config.spawnNum;
		movingMethod = _config.movingMethod;
		timer = spawnInterval - 6.0;
	}

	public Node2D SunCreate()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return null;
		}
		Vector2 mapGridBeginPos = instance.GetMapGridBeginPos();
		double num = GD.RandRange(mapGridBeginPos.X, instance.GetMapGroundRight());
		double height = GD.RandRange(mapGridBeginPos.Y + 200f, instance.GetMapGroundDown() - (double)mapGridBeginPos.Y);
		Vector2 pos = new Vector2((float)num, mapGridBeginPos.Y - 100f);
		Node2D result = null;
		switch (type)
		{
		case "Normal":
			result = instance.SunCreate(LocalAccountId, pos, spawnNum, movingMethod, height, new Vector2(0f, 100f));
			break;
		case "Brain":
			result = instance.BrainSunCreate(LocalAccountId, pos, spawnNum, movingMethod, height, new Vector2(0f, 100f));
			break;
		case "Jala":
			result = instance.JalapenoSunCreate(LocalAccountId, pos, spawnNum, movingMethod, height, new Vector2(0f, 100f));
			break;
		case "QX":
			result = instance.QXSunCreate(LocalAccountId, pos, spawnNum, movingMethod, height, new Vector2(0f, 100f));
			break;
		case "MagicSun":
			result = instance.MagicSunCreate(LocalAccountId, pos, spawnNum, movingMethod, height, new Vector2(0f, 100f));
			break;
		}
		return result;
	}

	public override void Destroy()
	{
		isRunning = false;
		this.OnSunCollect = null;
		this.OnSunChange = null;
		this.OnAccountSunCollect = null;
		this.OnAccountSunChange = null;
		InvalidateSpendReceipts();
		_accountBalances.Clear();
		_mapFeature = null;
		config = null;
		base.Destroy();
	}

	private bool TryGetActiveSpendReceipt(SunSpendReceipt receipt, out long transactionId)
	{
		transactionId = 0L;
		if (receipt == null || !receipt.IsActive || receipt.LedgerGeneration != _ledgerGeneration || !_activeSpendReceipts.TryGetValue(receipt.TransactionId, out var value) || value != receipt)
		{
			return false;
		}
		transactionId = receipt.TransactionId;
		return true;
	}

	private long NextSpendTransactionId()
	{
		do
		{
			_nextSpendTransactionId = ((_nextSpendTransactionId == 9223372036854775807L) ? 1 : (_nextSpendTransactionId + 1));
		}
		while (_activeSpendReceipts.ContainsKey(_nextSpendTransactionId));
		return _nextSpendTransactionId;
	}

	private void InvalidateSpendReceipts()
	{
		foreach (SunSpendReceipt value in _activeSpendReceipts.Values)
		{
			value.Complete(SunSpendReceiptState.Invalidated);
		}
		_activeSpendReceipts.Clear();
		_nextSpendTransactionId = 0L;
		_ledgerGeneration = ((_ledgerGeneration == 9223372036854775807L) ? 1 : (_ledgerGeneration + 1));
	}

	private void EmitSunCollect(EconomyAccountId accountId, long num)
	{
		this.OnAccountSunCollect?.Invoke(accountId, num);
		if (accountId == LocalAccountId)
		{
			this.OnSunCollect?.Invoke(num);
		}
	}

	private void EmitSunChange(EconomyAccountId accountId, long balance, SunTransactionReason reason)
	{
		this.OnAccountSunChange?.Invoke(accountId, balance, reason);
		if (accountId == LocalAccountId)
		{
			this.OnSunChange?.Invoke(balance);
		}
	}

	private static long SaturatingAdd(long value, long delta)
	{
		if (delta > 0 && value > 9223372036854775807L - delta)
		{
			return 9223372036854775807L;
		}
		if (delta < 0 && value < -9223372036854775808L - delta)
		{
			return -9223372036854775808L;
		}
		return value + delta;
	}

	private static long SaturatingSubtract(long value, long amount)
	{
		if (amount > 0 && value < -9223372036854775808L + amount)
		{
			return -9223372036854775808L;
		}
		if (amount < 0 && value > 9223372036854775807L + amount)
		{
			return 9223372036854775807L;
		}
		return value - amount;
	}
}
