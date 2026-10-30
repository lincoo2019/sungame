using System.Collections.Generic;
using Godot;

[Tool]
[GlobalClass]
public partial class FireComponentProjectileResource : Resource
{
	public void CollectProjectileData(List<TowerDefenseProjectileCreateData> output, HashSet<FireComponentProjectileResource> visited)
	{
		if (output != null && visited != null && visited.Add(this))
		{
			CollectProjectileDataCore(output, visited);
		}
	}

	protected virtual void CollectProjectileDataCore(List<TowerDefenseProjectileCreateData> output, HashSet<FireComponentProjectileResource> visited)
	{
	}

	public virtual bool CanFire(FireComponent fireComponent, int collisionFlag)
	{
		return false;
	}

	public virtual TowerDefenseProjectileCreateData GetProjectile()
	{
		return null;
	}

	public virtual TowerDefenseProjectileCreateData GetProjectile(RandomNumberGenerator random)
	{
		return GetProjectile();
	}

	public virtual TowerDefenseProjectileCreateData GetProjetile()
	{
		return GetProjectile();
	}
}
