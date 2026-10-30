public class ArmorBehaviorRuntime
{
	public ArmorBehaviorDefinition Definition { get; private set; }

	public TowerDefenseArmorInstance Armor { get; private set; }

	public bool Enabled { get; private set; }

	internal void Bind(ArmorBehaviorDefinition definition, TowerDefenseArmorInstance armor)
	{
		Definition = definition;
		Armor = armor;
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
			Armor = null;
		}
	}

	protected virtual void OnBound()
	{
	}

	protected virtual void OnReleased()
	{
	}

	public virtual void BeforeDamage(ref ArmorDamageContext context)
	{
	}

	public virtual void AfterDamage(ref ArmorDamageContext context)
	{
	}

	public virtual void OnStageChanged(int stage)
	{
	}

	public virtual void OnRemoved(ArmorRemovalReason reason)
	{
	}
}
