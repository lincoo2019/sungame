using Godot;

public interface IProjectileZone
{
	Rect2 WorldRect { get; }

	int GridY { get; }

	int RowSpan { get; }

	void UpdateRect();

	void OnBulletIntersect(ref BulletData b, int index);

	void OnProjectileIntersect(TowerDefenseProjectile projectile);
}
