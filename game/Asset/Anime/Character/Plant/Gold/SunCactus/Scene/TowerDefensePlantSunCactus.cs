using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunCactus : TowerDefensePlant
{
	public FireComponentExtendCactus fireComponentExtendCactus;

	public FireComponent fireComponent;

	private double _fireInterval = 1.5;

	private string _projectileName = "WhiteFireSpike";

	public bool up;

	public int fireNum = 2;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && this.fireComponent.fireCheckList.Count > 1)
				{
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[0].projectile).projectileName = value;
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[1].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponentExtendCactus = componentManager.GetRuntime<FireComponentExtendCactus>("character.fire.cactus");
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			fireComponent.BindCheckRayToGridRow(0, 1);
			fireComponent.BindCheckRayToGridRow(2, -1);
			fireComponent.onlyEmitSignal = true;
			fireComponent.OnFireVolley += FireVolley;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.OnFireVolley -= FireVolley;
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (!fireComponentExtendCactus.IsUp())
		{
			base.sprite.SetAnimation("Idle", loop: true, 0.1);
		}
		else
		{
			base.sprite.SetAnimation("UpIdle", loop: true, 0.1);
		}
	}

	public void FireVolley(ulong randomSeed)
	{
		FireComponentCheckConfig runningCheck = fireComponent.runningCheck;
		if (!GodotObject.IsInstanceValid(runningCheck) || !GodotObject.IsInstanceValid(runningCheck.projectile))
		{
			return;
		}
		TowerDefenseProjectileCreateData projectile = runningCheck.projectile.GetProjectile();
		if (projectile == null)
		{
			return;
		}
		int collisionFlags = runningCheck.GetCollisionFlags();
		int num = (int)Math.Clamp(GetOwnerSunBalance() / 400, 0L, 4L);
		int num2 = Math.Max(0, fireNum) + num;
		AudioManager.Instance.AudioPlay("ProjectileThrow");
		for (int i = -1; i <= 1; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					flipXOverride = (base.Scale.X < 0f)
				};
				if (j != 0)
				{
					overrides.spawnTweenOffset = new Vector2(j * 25, 0f);
					overrides.spawnTweenDuration = 0.03f;
					overrides.spawnTweenEase = Tween.EaseType.Out;
					overrides.spawnTweenTrans = Tween.TransitionType.Quad;
				}
				fireComponent.CreateProjectile(0, new Vector2(300f, 0f), projectile, collisionFlags, base.camp, Vector2.Zero, i, filterByLine: true, overrides);
			}
		}
	}

	private long GetOwnerSunBalance()
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return 0L;
		}
		if (base.HasEconomyOwner && towerDefenseManager.TryGetSun(base.EconomyOwnerAccountId, out var balance))
		{
			return balance;
		}
		long sun = towerDefenseManager.GetSun();
		return Math.Max(0L, sun);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "up", up },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "WhiteFireSpike").AsString();
		up = data.GetValueOrDefault("up", false).AsBool();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
