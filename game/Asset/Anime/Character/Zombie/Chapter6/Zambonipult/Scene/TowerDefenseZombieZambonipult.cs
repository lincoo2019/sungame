using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieZambonipult : TowerDefenseZombie
{
	private const string ZOMBIE_CATAPULT_POLE = "uid://bj2f8bm3g56me";

	private const string ZOMBIE_CATAPULT_POLE_DAMAGE = "uid://dwrc0oqx3bcv";

	private GpuParticles2D _zamboniSmoke;

	private Marker2D _iceCapMarker;

	private FireComponent _fireComponent;

	private CatapultComponent _catapultComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_zamboniSmoke = GetNode<GpuParticles2D>("%ZamboniSmoke");
			_iceCapMarker = GetNode<Marker2D>("%IceCapMarker");
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_catapultComponent = componentManager.GetRuntime<CatapultComponent>();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !die && !nearDie && !instance.hypnoses)
		{
			TowerDefenseManager.Instance.SetIceCapPos(base.gridPos.Y, GetLogicalGlobalPosition(_iceCapMarker));
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		_catapultComponent.IdleProcessing(delta);
	}

	public override void IdleExited()
	{
		base.IdleExited();
	}

	public override void WalkEntered()
	{
		_catapultComponent.WalkEntered();
	}

	public override void WalkProcessing(double delta)
	{
		_catapultComponent.WalkProcessing(delta);
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		if (!isExplode)
		{
			_catapultComponent.CreateDeathEffect();
			Destroy();
		}
		else
		{
			spritePause = true;
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		_catapultComponent.OnDamagePoint(damangePointName);
		if (!(damangePointName == "DamagePoint2"))
		{
			if (damangePointName == "DamagePoint3")
			{
				_catapultComponent.speed = 20.0;
				if (GodotObject.IsInstanceValid(_zamboniSmoke))
				{
					_zamboniSmoke.Visible = true;
				}
			}
		}
		else
		{
			_catapultComponent.speed = 25.0;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire")
		{
			_catapultComponent.OnFireAnimeEvent();
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f)
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(-400f, 0f), _fireComponent.fireCheckList[0].projectile.GetProjetile(), -1, base.camp, Vector2.Zero, overrides);
			UpdateProjectileVisual();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Fire"))
		{
			if (clip == "Wheelie")
			{
				_catapultComponent.CreateDeathEffect();
			}
		}
		else
		{
			_catapultComponent.OnFireAnimeCompleted();
		}
	}

	private void UpdateProjectileVisual()
	{
		int currentProjectileNum = _catapultComponent.currentProjectileNum;
		int projectileNum = _catapultComponent.projectileNum;
		if (currentProjectileNum <= 0)
		{
			base.sprite.SetAtlasReplace("Zombie_zambonipult_pole_withball.png", "uid://bj2f8bm3g56me");
			base.sprite.SetAtlasReplace("Zombie_zambonipult_pole_damage_withball.png", "uid://dwrc0oqx3bcv");
			base.sprite.SetFliters(new Array { "Zombie_catapult_basketball", "Zombie_catapult_basketball2", "Zombie_catapult_basketball3", "Zombie_catapult_basketball4" }, open: false);
		}
		else if ((double)currentProjectileNum <= (double)projectileNum / 4.0 * 1.0)
		{
			base.sprite.SetFliters(new Array { "Zombie_catapult_basketball3" }, open: false);
		}
		else if ((double)currentProjectileNum < (double)projectileNum / 4.0 * 2.0)
		{
			base.sprite.SetFliters(new Array { "Zombie_catapult_basketball2" }, open: false);
		}
		else if ((double)currentProjectileNum < (double)projectileNum / 4.0 * 3.0)
		{
			base.sprite.SetFliters(new Array { "Zombie_catapult_basketball" }, open: false);
		}
	}
}
