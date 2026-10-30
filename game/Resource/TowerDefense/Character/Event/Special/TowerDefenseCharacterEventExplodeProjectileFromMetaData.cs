using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventExplodeProjectileFromMetaData : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double speed = 300.0;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.eventProjectiles, speed, projectile.GlobalPosition, projectile.height, projectile.collisionFlags, projectile.camp);
	}

	public override void Init(Dictionary valueDictionary)
	{
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "SnowBallSpawn",
			["Value"] = new Dictionary()
		};
	}

	public static void Run(BulletFieldStoredProjectile[] storedProjectiles, double speed, Vector2 position, double height, int collisionFlags = 1, TowerDefenseEnum.CHARACTER_CAMP camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT)
	{
		if (storedProjectiles == null)
		{
			return;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = null;
		for (int i = 0; i < storedProjectiles.Length; i++)
		{
			if (storedProjectiles[i].Config != null)
			{
				towerDefenseProjectileConfig = storedProjectiles[i].Config;
				break;
			}
		}
		if (towerDefenseProjectileConfig == null || !FireComponent.TryPrepareProjectileBurstPositionByConfig(null, null, height, position, towerDefenseProjectileConfig, collisionFlags, camp, out var context))
		{
			return;
		}
		float num = (float)speed;
		for (int j = 0; j < storedProjectiles.Length; j++)
		{
			BulletFieldStoredProjectile bulletFieldStoredProjectile = storedProjectiles[j];
			TowerDefenseProjectileConfig config = bulletFieldStoredProjectile.Config;
			if (config != null)
			{
				float num2 = Random.Shared.NextSingle() * ((float)Math.PI * 2f);
				Vector2 velocity = Vector2.FromAngle(num2) * num;
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					checkAllOverride = true,
					initialRotationOverride = num2,
					fireMethodFlagsOverride = bulletFieldStoredProjectile.FireMethodFlags
				};
				FireComponent.SpawnPreparedProjectileBurstItem(in context, config, velocity, num, overrides);
			}
		}
	}
}
