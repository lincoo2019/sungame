public sealed class SunSpendReceipt
{
	private TowerDefenseBattleFeatureSun _owner;

	public EconomyAccountId AccountId { get; }

	public long Amount { get; }

	public SunSpendReceiptState State { get; private set; }

	public bool IsActive => State == SunSpendReceiptState.Active;

	internal long LedgerGeneration { get; }

	internal long TransactionId { get; }

	internal SunSpendReceipt(TowerDefenseBattleFeatureSun owner, long ledgerGeneration, long transactionId, EconomyAccountId accountId, long amount)
	{
		_owner = owner;
		LedgerGeneration = ledgerGeneration;
		TransactionId = transactionId;
		AccountId = accountId;
		Amount = amount;
		State = SunSpendReceiptState.Active;
	}

	public bool TryCommit()
	{
		return _owner?.TryCommitSunSpend(this) ?? false;
	}

	public bool TryRollback()
	{
		return _owner?.TryRollbackSunSpend(this) ?? false;
	}

	internal void Complete(SunSpendReceiptState state)
	{
		if (IsActive)
		{
			State = state;
			_owner = null;
		}
	}
}
