public readonly struct BulletFieldStoredProjectile(TowerDefenseProjectileConfig config, int fireMethodFlags)
{
	public readonly TowerDefenseProjectileConfig Config = config;

	public readonly int FireMethodFlags = fireMethodFlags;
}
