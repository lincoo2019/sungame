using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class FireComponentProjectileWeight : FireComponentProjectileResource
{
	[Export(PropertyHint.None, "")]
	public bool averageWeight;

	public TowerDefenseProjectileCreateData readyProjectile;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData projectileData { get; set; }

	[Export(PropertyHint.None, "")]
	public Array<FireComponentProjectileWeightItem> projectileWeight { get; set; } = new Array<FireComponentProjectileWeightItem>();

	public string projectileSimilarName
	{
		get
		{
			if (projectileData == null)
			{
				return "";
			}
			return projectileData.projectileName;
		}
		set
		{
			if (projectileData == null)
			{
				projectileData = new TowerDefenseProjectileCreateData(value);
			}
			else if (!(value == (string?)projectileData.projectileName))
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
		if (projectileWeight == null)
		{
			return;
		}
		for (int i = 0; i < projectileWeight.Count; i++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem = projectileWeight[i];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem.projectileResource))
			{
				fireComponentProjectileWeightItem.projectileResource.CollectProjectileData(output, visited);
			}
		}
	}

	public override bool CanFire(FireComponent fireComponent, int collisionFlag)
	{
		if (projectileData != null && fireComponent != null && !fireComponent.IsReleased)
		{
			return fireComponent.CanFire(projectileData, collisionFlag);
		}
		return false;
	}

	public override TowerDefenseProjectileCreateData GetProjectile()
	{
		return GetProjectile(null);
	}

	public override TowerDefenseProjectileCreateData GetProjectile(RandomNumberGenerator random)
	{
		if (projectileWeight == null || projectileWeight.Count == 0)
		{
			return projectileData;
		}
		FireComponentProjectileResource fireComponentProjectileResource = (averageWeight ? PickUniformProjectile(random) : PickWeightedProjectile(random));
		if (!GodotObject.IsInstanceValid(fireComponentProjectileResource))
		{
			return projectileData;
		}
		return fireComponentProjectileResource.GetProjectile();
	}

	public override TowerDefenseProjectileCreateData GetProjetile()
	{
		return GetProjectile();
	}

	private FireComponentProjectileResource PickUniformProjectile(RandomNumberGenerator random)
	{
		int num = 0;
		for (int i = 0; i < projectileWeight.Count; i++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem = projectileWeight[i];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem.projectileResource))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return null;
		}
		int num2 = (int)((random?.Randi() ?? GD.Randi()) % (uint)num);
		for (int j = 0; j < projectileWeight.Count; j++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem2 = projectileWeight[j];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2.projectileResource) && num2-- == 0)
			{
				return fireComponentProjectileWeightItem2.projectileResource;
			}
		}
		return null;
	}

	private FireComponentProjectileResource PickWeightedProjectile(RandomNumberGenerator random)
	{
		float num = 0f;
		for (int i = 0; i < projectileWeight.Count; i++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem = projectileWeight[i];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem.projectileResource))
			{
				num += Mathf.Max(0f, fireComponentProjectileWeightItem.weight);
			}
		}
		if (num <= 0f)
		{
			return PickUniformProjectile(random);
		}
		float num2 = (random?.Randf() ?? GD.Randf()) * num;
		FireComponentProjectileResource result = null;
		for (int j = 0; j < projectileWeight.Count; j++)
		{
			FireComponentProjectileWeightItem fireComponentProjectileWeightItem2 = projectileWeight[j];
			if (GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2) && GodotObject.IsInstanceValid(fireComponentProjectileWeightItem2.projectileResource))
			{
				result = fireComponentProjectileWeightItem2.projectileResource;
				num2 -= Mathf.Max(0f, fireComponentProjectileWeightItem2.weight);
				if (num2 <= 0f)
				{
					return fireComponentProjectileWeightItem2.projectileResource;
				}
			}
		}
		return result;
	}

	public void RefreshProjectile()
	{
		readyProjectile = GetProjectile();
	}
}
