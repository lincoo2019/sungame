using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

[GlobalClass]
public partial class TowerDefenseProjectileEffectPlanternSix : TowerDefenseProjectileEffectBase
{
	private struct StructBullet
	{
		public int index;

		public double timer;

		public bool recallStarted;

		public float recallDirRad;

		public Vector2 recallPos;

		public bool origRotateFollowVelocity;
	}

	private readonly List<StructBullet> structBullets = new List<StructBullet>();

	private TowerDefenseProjectileCreateData[] _projectileDataByDirection;

	public double timer;

	public bool over;

	public override string NetworkEffectId => "plantern_six_burst";

	public override void _Ready()
	{
		if (PrepareNetworkEffect())
		{
			_projectileDataByDirection = new TowerDefenseProjectileCreateData[6]
			{
				new TowerDefenseProjectileCreateData(new StringName("Pea")),
				new TowerDefenseProjectileCreateData(new StringName("FirePea")),
				new TowerDefenseProjectileCreateData(new StringName("SnowPea")),
				new TowerDefenseProjectileCreateData(new StringName("Pea")),
				new TowerDefenseProjectileCreateData(new StringName("FirePea")),
				new TowerDefenseProjectileCreateData(new StringName("SnowPea"))
			};
			AttackCreateAsync();
		}
	}

	public override void _ExitTree()
	{
		BulletField instance = BulletField.Instance;
		for (int i = 0; i < structBullets.Count; i++)
		{
			StructBullet structBullet = structBullets[i];
			if (instance != null && instance.IsBulletActive(structBullet.index))
			{
				ref BulletData bulletDataRef = ref instance.GetBulletDataRef(structBullet.index);
				bulletDataRef.externalControlled = false;
				bulletDataRef.rotateFollowVelocity = structBullet.origRotateFollowVelocity;
			}
		}
		structBullets.Clear();
		if (_projectileDataByDirection != null)
		{
			TowerDefenseProjectileCreateData[] projectileDataByDirection = _projectileDataByDirection;
			for (int j = 0; j < projectileDataByDirection.Length; j++)
			{
				projectileDataByDirection[j]?.Dispose();
			}
			_projectileDataByDirection = null;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		timer += delta;
		if (structBullets.Count > 0)
		{
			BulletField instance = BulletField.Instance;
			for (int num = structBullets.Count - 1; num >= 0; num--)
			{
				StructBullet value = structBullets[num];
				if (instance == null || !instance.IsBulletActive(value.index))
				{
					structBullets.RemoveAt(num);
				}
				else
				{
					ref BulletData bulletDataRef = ref instance.GetBulletDataRef(value.index);
					if (value.recallStarted)
					{
						if (!instance.IsBulletTweening(value.index))
						{
							bulletDataRef.vel = Vector2.FromAngle(value.recallDirRad) * 300f;
							bulletDataRef.speed = 300f;
							bulletDataRef.rotation = value.recallDirRad;
							bulletDataRef.rotateFollowVelocity = value.origRotateFollowVelocity;
							bulletDataRef.externalControlled = false;
							structBullets.RemoveAt(num);
						}
					}
					else
					{
						value.timer += delta;
						if (value.timer >= 2.0)
						{
							value.recallStarted = true;
							instance.StartRuntimeTween(value.index, value.recallPos, 0.1f, Tween.EaseType.Out, Tween.TransitionType.Quart);
						}
						structBullets[num] = value;
					}
				}
			}
			UpdateOrbitingBullets(instance, delta);
		}
		if (over && structBullets.Count == 0)
		{
			QueueFree();
		}
	}

	private void UpdateOrbitingBullets(BulletField bulletField, double delta)
	{
		int count = structBullets.Count;
		for (int i = 0; i < count; i++)
		{
			StructBullet structBullet = structBullets[i];
			if (bulletField != null && bulletField.IsBulletActive(structBullet.index))
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(structBullet.index);
				if (structBullet.recallStarted)
				{
					bulletDataRef.rotation = (float)Mathf.LerpAngle(bulletDataRef.rotation, structBullet.recallDirRad, 10.0 * delta);
					continue;
				}
				Vector2 orbitPosition = GetOrbitPosition(i, count);
				bulletDataRef.rotation = (float)Mathf.LerpAngle(bulletDataRef.rotation, (orbitPosition - bulletDataRef.pos).Angle(), 5.0 * delta);
				bulletDataRef.pos = bulletDataRef.pos.Lerp(orbitPosition, (float)(5.0 * delta));
			}
		}
	}

	private Vector2 GetOrbitPosition(int index, int total)
	{
		int num = total / 2;
		bool flag = index < num;
		int num2 = (flag ? num : (total - num));
		int num3 = (flag ? index : (index - num));
		double s = (double)((float)Math.PI * 2f / (float)num2 * (float)num3) + (flag ? (timer * 5.0) : timer);
		double num4 = (flag ? 30.0 : 60.0);
		return base.GlobalPosition + new Vector2((float)(Mathf.Cos(s) * num4), (float)(Mathf.Sin(s) * num4));
	}

	private void PlaceOrbitingBulletImmediately(BulletField bulletField, int trackedIndex)
	{
		int count = structBullets.Count;
		if (trackedIndex >= 0 && trackedIndex < count && bulletField != null)
		{
			StructBullet structBullet = structBullets[trackedIndex];
			if (!structBullet.recallStarted && bulletField.IsBulletActive(structBullet.index))
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(structBullet.index);
				bulletDataRef.rotation = ((bulletDataRef.pos = GetOrbitPosition(trackedIndex, count)) - base.GlobalPosition).Angle();
			}
		}
	}

	private async Task AttackCreateAsync()
	{
		Vector2 startPos = base.GlobalPosition;
		for (int wave = 0; wave < 10; wave++)
		{
			for (int i = 0; i < 6; i++)
			{
				double num = i switch
				{
					0 => 60.0, 
					1 => 0.0, 
					2 => -60.0, 
					3 => 120.0, 
					4 => 180.0, 
					_ => 240.0, 
				};
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					checkAllOverride = true,
					gridYOverride = gridPos.Y
				};
				int num2 = BulletField.Instance?.ActiveCount ?? 0;
				FireComponent.CreateProjectilePositionByData(null, null, 30.0, base.GlobalPosition, Vector2.Zero, _projectileDataByDirection[i], collisionFlag, camp, default(Vector2), overrides);
				BulletField instance = BulletField.Instance;
				if (instance == null || instance.ActiveCount <= num2)
				{
					continue;
				}
				int lastSpawnedIndex = instance.LastSpawnedIndex;
				if (lastSpawnedIndex >= 0 && instance.IsBulletActive(lastSpawnedIndex))
				{
					ref BulletData bulletDataRef = ref instance.GetBulletDataRef(lastSpawnedIndex);
					if (bulletDataRef.trackOpen)
					{
						bulletDataRef.speed = 300f;
						continue;
					}
					bool rotateFollowVelocity = bulletDataRef.rotateFollowVelocity;
					bulletDataRef.externalControlled = true;
					bulletDataRef.rotateFollowVelocity = false;
					structBullets.Add(new StructBullet
					{
						index = lastSpawnedIndex,
						timer = 0.0,
						recallStarted = false,
						recallDirRad = Mathf.DegToRad((float)num),
						recallPos = startPos,
						origRotateFollowVelocity = rotateFollowVelocity
					});
				}
			}
			BulletField instance2 = BulletField.Instance;
			for (int j = 0; j < structBullets.Count; j++)
			{
				PlaceOrbitingBulletImmediately(instance2, j);
			}
			await ToSignal(GetTree().CreateTimer(0.1, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
		over = true;
	}
}
