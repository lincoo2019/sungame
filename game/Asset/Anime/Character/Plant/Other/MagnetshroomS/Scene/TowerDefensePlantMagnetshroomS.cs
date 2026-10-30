using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantMagnetshroomS : TowerDefensePlant
{
	private readonly struct StoredProjectileHandle(int index, TowerDefenseProjectileConfig config)
	{
		public readonly int Index = index;

		public readonly TowerDefenseProjectileConfig Config = config;
	}

	private const string StoredProjectileName = "StarIron";

	private const int SpawnCount = 5;

	private const float ReleasedProjectileSpeed = 600f;

	public MagnetComponent magnetComponent;

	public FireComponent fireComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> drawEvent = new Array<TowerDefenseCharacterEventBase>();

	private readonly List<StoredProjectileHandle> _storedProjectiles = new List<StoredProjectileHandle>();

	public double timer;

	private double _breakDownTime = 15.0;

	[Export(PropertyHint.None, "")]
	public double breakDownTime
	{
		get
		{
			return _breakDownTime;
		}
		set
		{
			_breakDownTime = value;
			if (IsNodeReady())
			{
				MagnetComponent obj = magnetComponent;
				if (obj != null && !obj.IsReleased)
				{
					magnetComponent.breakDownTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		magnetComponent = componentManager.GetRuntime<MagnetComponent>();
		if (magnetComponent == null)
		{
			GD.PushError("MagnetshroomS is missing its Magnet resource runtime.");
			return;
		}
		fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		if (fireComponent == null)
		{
			GD.PushError("MagnetshroomS is missing its Fire resource runtime.");
			return;
		}
		magnetComponent.OnBreakDown += BreakDown;
		magnetComponent.OnDrawTarget += DrawTarget;
	}

	public override void _ExitTree()
	{
		MagnetComponent obj = magnetComponent;
		if (obj != null && !obj.IsReleased)
		{
			magnetComponent.OnBreakDown -= BreakDown;
			magnetComponent.OnDrawTarget -= DrawTarget;
		}
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (_storedProjectiles.Count == 0)
		{
			return;
		}
		BulletField bulletField = BulletField.Instance;
		if (!GodotObject.IsInstanceValid(bulletField))
		{
			_storedProjectiles.Clear();
			return;
		}
		timer += delta;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		for (int num = _storedProjectiles.Count - 1; num >= 0; num--)
		{
			StoredProjectileHandle stored = _storedProjectiles[num];
			if (!IsStoredProjectileActive(bulletField, stored))
			{
				_storedProjectiles.RemoveAt(num);
			}
			else
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(stored.Index);
				float num2 = (float)num * ((float)Math.PI * 2f / 5f);
				bulletDataRef.pos = logicalGlobalPosition + new Vector2(Mathf.Cos((float)(timer * 2.0) + num2) * 50f, Mathf.Sin((float)timer + num2) * 10f);
				bulletDataRef.rotation += (float)(delta * (double)bulletDataRef.rotateScale * (double)bulletDataRef.fireDirX);
			}
		}
		if (CanReleaseStoredProjectile())
		{
			ReleaseStoredProjectile();
		}
	}

	public override void DestroySet()
	{
		magnetComponent?.Destroy();
		BulletField bulletField = BulletField.Instance;
		for (int num = _storedProjectiles.Count - 1; num >= 0; num--)
		{
			StoredProjectileHandle stored = _storedProjectiles[num];
			if (GodotObject.IsInstanceValid(bulletField) && IsStoredProjectileActive(bulletField, stored))
			{
				bulletField.Despawn(stored.Index);
			}
		}
		_storedProjectiles.Clear();
	}

	public virtual void BreakDown(TowerDefenseArmorInstance _armor)
	{
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			for (int i = 0; i < 5; i++)
			{
				SpawnStoredProjectile();
			}
		}
	}

	private void SpawnStoredProjectile()
	{
		int num = BulletField.Instance?.ActiveCount ?? 0;
		TowerDefenseProjectileCreateData projectileData = new TowerDefenseProjectileCreateData("StarIron")
		{
			damageFlags = 2,
			fireMethodFlags = 32,
			collisionFlags = 11,
			overrideCatapultHeight = true,
			catapultHeight = 400.0
		};
		fireComponent.CreateProjectileByData(0, Vector2.Zero, projectileData, -1, base.camp, Vector2.Zero, new BulletFieldSpawnOverrides
		{
			gridYOverride = base.gridPos.Y
		});
		BulletField bulletField = BulletField.Instance;
		if (!GodotObject.IsInstanceValid(bulletField) || bulletField.ActiveCount <= num)
		{
			return;
		}
		int lastSpawnedIndex = bulletField.LastSpawnedIndex;
		if (lastSpawnedIndex >= 0 && bulletField.IsBulletActive(lastSpawnedIndex))
		{
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(lastSpawnedIndex);
			if (bulletDataRef.fireCharacter == this)
			{
				bulletDataRef.externalControlled = true;
				bulletDataRef.hitOver = true;
				bulletDataRef.vel = Vector2.Zero;
				bulletDataRef.speed = 0f;
				bulletDataRef.target = null;
				bulletDataRef.magneticTarget = null;
				bulletDataRef.pos = new Vector2(bulletDataRef.pos.X, GetLogicalGlobalPosition().Y);
				_storedProjectiles.Add(new StoredProjectileHandle(lastSpawnedIndex, bulletDataRef.config));
			}
		}
	}

	private bool CanReleaseStoredProjectile()
	{
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent == null || fireComponent.IsReleased || _storedProjectiles.Count == 0)
		{
			return false;
		}
		BulletField bulletField = BulletField.Instance;
		List<StoredProjectileHandle> storedProjectiles = _storedProjectiles;
		StoredProjectileHandle stored = storedProjectiles[storedProjectiles.Count - 1];
		if (!GodotObject.IsInstanceValid(bulletField) || !IsStoredProjectileActive(bulletField, stored))
		{
			return false;
		}
		ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(stored.Index);
		return this.fireComponent.CheckTrackTarget(bulletDataRef.collisionFlags);
	}

	private bool ReleaseStoredProjectile(bool refreshFireComponent = true)
	{
		if (_storedProjectiles.Count == 0)
		{
			return false;
		}
		int index = _storedProjectiles.Count - 1;
		StoredProjectileHandle stored = _storedProjectiles[index];
		_storedProjectiles.RemoveAt(index);
		BulletField bulletField = BulletField.Instance;
		if (!GodotObject.IsInstanceValid(bulletField) || !IsStoredProjectileActive(bulletField, stored))
		{
			return false;
		}
		ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(stored.Index);
		bulletDataRef.externalControlled = false;
		bulletDataRef.hitOver = false;
		bulletDataRef.speed = 600f;
		float num = Mathf.Sign(base.Scale.X);
		if (Mathf.IsZeroApprox(num))
		{
			num = 1f;
		}
		bulletDataRef.vel = new Vector2(600f * num, 0f);
		bulletDataRef.fireDirX = num;
		bulletDataRef.trackOpen = true;
		bulletDataRef.checkAll = true;
		bulletDataRef.target = null;
		bulletDataRef.magneticTarget = null;
		bulletDataRef.savePos = bulletDataRef.pos;
		if (refreshFireComponent)
		{
			FireComponent fireComponent = this.fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				this.fireComponent.Refresh();
			}
		}
		return true;
	}

	private bool IsStoredProjectileActive(BulletField field, StoredProjectileHandle stored)
	{
		if (!GodotObject.IsInstanceValid(field) || !field.IsBulletActive(stored.Index))
		{
			return false;
		}
		ref BulletData bulletDataRef = ref field.GetBulletDataRef(stored.Index);
		if (bulletDataRef.config == stored.Config)
		{
			return bulletDataRef.fireCharacter == this;
		}
		return false;
	}

	public virtual void DrawTarget(TowerDefenseCharacter target)
	{
		TowerDefenseExplode.CreateExplode(target.GetLogicalGlobalPosition(), new Vector2(1.5f, 1.5f), drawEvent, new Array<TowerDefenseCharacter>(), base.camp, -1);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "timer", timer },
			{ "breakDownTime", breakDownTime }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		timer = data.GetValueOrDefault("timer", 0.0).AsDouble();
		breakDownTime = data.GetValueOrDefault("breakDownTime", 15.0).AsDouble();
	}
}
