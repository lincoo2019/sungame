using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieCatapult : TowerDefenseZombie
{
	private FireComponent _fireComponent;

	private CatapultComponent _catapultComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_catapultComponent = componentManager.GetRuntime<CatapultComponent>();
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
		_catapultComponent.CreateDeathEffect();
		Destroy();
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		_catapultComponent.OnDamagePoint(damangePointName);
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
		if (!(clip == "Bounce"))
		{
			if (clip == "Fire")
			{
				_catapultComponent.OnFireAnimeCompleted();
			}
		}
		else
		{
			_catapultComponent.CreateDeathEffect();
		}
	}

	private void UpdateProjectileVisual()
	{
		int projectileNum = _catapultComponent.projectileNum;
		int currentProjectileNum = _catapultComponent.currentProjectileNum;
		if (currentProjectileNum <= 0)
		{
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
