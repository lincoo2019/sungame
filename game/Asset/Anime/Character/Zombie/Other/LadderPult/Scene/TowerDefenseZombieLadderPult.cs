using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieLadderPult : TowerDefenseZombie
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

	private void FireLadder()
	{
		TowerDefenseCharacter target = FindLadderTarget();
		if (GodotObject.IsInstanceValid(target))
		{
			TowerDefenseProjectileCreateData projetile = _fireComponent.fireCheckList[0].projectile.GetProjetile();
			if (projetile != null)
			{
				Marker2D nodeOrNull = GetNodeOrNull<Marker2D>("SpriteGroup/TransformPoint/ZombieLadderPult/FireSlot/FireMarker");
				Vector2 pos = ((nodeOrNull != null) ? GetLogicalGlobalPosition(nodeOrNull) : GetLogicalGlobalPosition());
				double height = GetGroundHeight(pos.Y) - base.groundHeight * 2.0;
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				FireComponent.CreateProjectilePositionByData(this, target, height, pos, new Vector2(-400f, 0f), projetile, -1, base.camp, Vector2.Zero, overrides);
				UpdateProjectileVisual();
			}
		}
	}

	private TowerDefenseCharacter FindLadderTarget()
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return null;
		}
		List<TowerDefenseCharacter> characterTarget = towerDefenseManager.GetCharacterTarget(this, checkLine: true, checkCollision: true);
		if (characterTarget == null || characterTarget.Count == 0)
		{
			return null;
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item in characterTarget)
		{
			if (item is TowerDefensePlant)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = item.cell;
				if (GodotObject.IsInstanceValid(towerDefenseCellInstance) && towerDefenseCellInstance.HasWallnut() && !GodotObject.IsInstanceValid(towerDefenseCellInstance.characterLadder))
				{
					list.Add(item);
				}
			}
		}
		if (list.Count > 0)
		{
			return list[(int)((ulong)GD.Randi() % (ulong)list.Count)];
		}
		TowerDefenseCharacter towerDefenseCharacter = null;
		foreach (TowerDefenseCharacter item2 in characterTarget)
		{
			if (item2 is TowerDefensePlant && (towerDefenseCharacter == null || item2.gridPos.X < towerDefenseCharacter.gridPos.X))
			{
				towerDefenseCharacter = item2;
			}
		}
		return towerDefenseCharacter;
	}

	private void UpdateProjectileVisual()
	{
		int projectileNum = _catapultComponent.projectileNum;
		int currentProjectileNum = _catapultComponent.currentProjectileNum;
		int num = projectileNum - currentProjectileNum;
		Array array = new Array();
		if (num >= 1)
		{
			array.Add("Zombie_catapult_basketball");
		}
		if (num >= 2)
		{
			array.Add("Zombie_catapult_basketball2");
		}
		if (num >= 3)
		{
			array.Add("Zombie_catapult_basketball3");
		}
		if (num >= 4)
		{
			array.Add("ladder");
		}
		if (array.Count > 0)
		{
			base.sprite.SetFliters(array, open: false);
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
			FireLadder();
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
}
