using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class TowerDefenseProjectileEffectCherryPea : TowerDefenseProjectileEffectBase
{
	private static readonly StringName PeaName = new StringName("Pea");

	private static readonly StringName DefaultSkinName = new StringName("Default");

	private static readonly Vector2[] BurstDirections = CreateBurstDirections();

	private static readonly float[] BurstAngles = CreateBurstAngles();

	public int num = 10;

	public override string NetworkEffectId => "cherry_pea_burst";

	public override void _Ready()
	{
		if (PrepareNetworkEffect())
		{
			AttackCreateAsync();
		}
	}

	protected async Task AttackCreateAsync()
	{
		RandomNumberGenerator random = new RandomNumberGenerator
		{
			Seed = base.NetworkRandomSeed
		};
		try
		{
			TowerDefenseProjectileConfig projectileConfig = BulletField.BuildTemplateConfig(PeaName, DefaultSkinName);
			if (projectileConfig == null)
			{
				GD.PushError("[CherryPea:E_PROJECTILE_CONFIG] Pea/Default template config is unavailable");
				return;
			}
			for (int wave = 0; wave < this.num; wave++)
			{
				if (!FireComponent.TryPrepareProjectileBurstPositionByConfig(null, null, 10.0, base.GlobalPosition, projectileConfig, collisionFlag, camp, out var context))
				{
					break;
				}
				for (int i = 0; i < BurstDirections.Length; i++)
				{
					BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
					{
						checkAllOverride = true,
						initialRotationOverride = BurstAngles[i]
					};
					float num = random.RandfRange(200f, 800f);
					FireComponent.SpawnPreparedProjectileBurstItem(in context, BurstDirections[i] * num, num, overrides);
				}
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
		}
		finally
		{
			random.Dispose();
			if (GodotObject.IsInstanceValid(this))
			{
				QueueFree();
			}
		}
	}

	private static Vector2[] CreateBurstDirections()
	{
		Vector2[] array = new Vector2[36];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Vector2.FromAngle(Mathf.DegToRad((float)i * 10f));
		}
		return array;
	}

	private static float[] CreateBurstAngles()
	{
		float[] array = new float[36];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Mathf.DegToRad((float)i * 10f);
		}
		return array;
	}
}
