using System.Collections.Generic;
using Godot;

[Tool]
[GlobalClass]
public partial class FireComponentProjectileSingle : FireComponentProjectileResource
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData projectileData { get; set; }

	public string projectileName
	{
		get
		{
			if (projectileData != null)
			{
				return projectileData.projectileName;
			}
			return "";
		}
		set
		{
			if (projectileData == null)
			{
				projectileData = new TowerDefenseProjectileCreateData(value);
			}
			else if (value != (string?)projectileData.projectileName)
			{
				projectileData.projectileName = value;
				projectileData.baseDamage = -1.0;
				projectileData.InvalidateConfigCache();
			}
		}
	}

	protected override void CollectProjectileDataCore(List<TowerDefenseProjectileCreateData> output, HashSet<FireComponentProjectileResource> visited)
	{
		if (projectileData != null)
		{
			output.Add(projectileData);
		}
	}

	public override bool CanFire(FireComponent fireComponent, int collisionFlag)
	{
		if (projectileData == null)
		{
			return false;
		}
		return fireComponent.CanFire(projectileData, collisionFlag);
	}

	public override TowerDefenseProjectileCreateData GetProjectile()
	{
		return projectileData;
	}

	public override TowerDefenseProjectileCreateData GetProjetile()
	{
		return GetProjectile();
	}
}
