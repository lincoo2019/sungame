public class CardBehaviorRuntime
{
	public CardBehaviorDefinition Definition { get; private set; }

	public TowerDefenseInGamePacketShow Owner { get; private set; }

	public TowerDefensePacketConfig Config { get; private set; }

	public bool Enabled { get; private set; }

	internal void Bind(CardBehaviorDefinition definition, TowerDefenseInGamePacketShow owner, TowerDefensePacketConfig config)
	{
		Definition = definition;
		Owner = owner;
		Config = config;
		Enabled = definition?.InitiallyEnabled ?? false;
		OnBound();
	}

	internal void Disable()
	{
		Enabled = false;
	}

	internal void Release()
	{
		try
		{
			OnReleased();
		}
		finally
		{
			Enabled = false;
			Definition = null;
			Owner = null;
			Config = null;
		}
	}

	protected virtual void OnBound()
	{
	}

	protected virtual void OnReleased()
	{
	}

	public virtual void OnPressed()
	{
	}

	public virtual void OnUseSucceeded(TowerDefenseCharacter createdCharacter)
	{
	}
}
