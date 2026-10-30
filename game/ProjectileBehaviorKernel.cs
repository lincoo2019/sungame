public abstract class ProjectileBehaviorKernel
{
	public BulletField Field { get; private set; }

	internal void Bind(BulletField field)
	{
		Field = field;
	}

	public virtual void OnSpawn(int index, ref BulletData bullet)
	{
	}

	public virtual void Process(int index, ref BulletData bullet, double delta)
	{
	}

	public virtual void OnHitTarget(int index, ref BulletData bullet, TowerDefenseCharacter target)
	{
	}

	public virtual void OnLand(int index, ref BulletData bullet)
	{
	}

	public virtual void OnDespawn(int index, ref BulletData bullet)
	{
	}
}
