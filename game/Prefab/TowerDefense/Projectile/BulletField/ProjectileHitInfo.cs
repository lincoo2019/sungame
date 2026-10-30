using System;
using Godot;

public struct ProjectileHitInfo
{
	public double damage;

	public int damageFlags;

	public bool useRuntimeOverrides;

	public int fireMethodFlags;

	public Vector2 position;

	public TowerDefenseEnum.CHARACTER_HEIGHT projectileHeight;

	public TowerDefenseProjectileConfig config;

	public TowerDefenseCharacter fireCharacter;

	public TowerDefenseEnum.CHARACTER_CAMP camp;

	public int collisionFlags;

	public Vector2I gridPos;

	public double height;

	public bool hasTargetTransform;

	public float targetOriginX;

	public float targetScaleX;

	public Action onSourceDespawn;
}
