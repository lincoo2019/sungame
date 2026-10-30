using Godot;

public readonly struct DropItemCollectionContext
{
	public Node Source { get; }

	public Vector2 Position { get; }

	public long Value { get; }

	public EconomyAccountId AccountId { get; }

	public SunDropOwnershipPolicy OwnershipPolicy { get; }

	public bool UsesLegacyLocalEconomy => OwnershipPolicy == SunDropOwnershipPolicy.LegacySharedReplica;

	public DropItemCollectionContext(Node source, Vector2 position, long value, EconomyAccountId accountId, SunDropOwnershipPolicy ownershipPolicy = SunDropOwnershipPolicy.LegacySharedReplica)
	{
		Source = source;
		Position = position;
		Value = value;
		if (accountId.IsValid)
		{
			AccountId = accountId;
			OwnershipPolicy = ownershipPolicy;
		}
		else
		{
			AccountId = EconomyAccountId.Local;
			OwnershipPolicy = SunDropOwnershipPolicy.LegacySharedReplica;
		}
	}
}
