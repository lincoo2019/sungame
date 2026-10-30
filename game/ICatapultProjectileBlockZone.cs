using Godot;

public interface ICatapultProjectileBlockZone : IProjectileZone
{
	bool TryBlockCatapultBullet(ref BulletData bullet, int index, Vector2 collisionStartPosition, Vector2 collisionEndPosition, Vector2 collisionHalfSize);
}
