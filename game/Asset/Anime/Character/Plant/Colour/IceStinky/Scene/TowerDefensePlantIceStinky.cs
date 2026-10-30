using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceStinky : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public double forzenTime = 5.0;

	private CharacterMoveComponent _moveComponent;

	public bool isNut;

	public bool isCrawl;

	private readonly HashSet<TowerDefenseCharacter> _hitOverlaps = new HashSet<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _hitScratch = new HashSet<TowerDefenseCharacter>();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_moveComponent = componentManager.GetRuntime<CharacterMoveComponent>();
			instance.ClearHitpointsEmptyListeners();
			instance.keepAlive = true;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && isCrawl)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			shadowComponent.saveShadowPosition = new Vector2(shadowComponent.saveShadowPosition.X, logicalGlobalPosition.Y + 30f);
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition);
			if ((double)logicalGlobalPosition.X > groundRight + 150.0 || logicalGlobalPosition.X < -100f)
			{
				Destroy();
			}
			ProcessHitBoxOverlaps();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
		if (instance.hitpoints <= 0.0)
		{
			instance.invincible = true;
			instance.hitpoints = 300.0;
			instance.die = false;
			destroyComponent?.EndDeathSettlement();
			base.sprite.SetAnimation("Out", loop: false, 0.2);
			base.sprite.AddAnimation("Crawl", 0.0);
			CharacterMoveComponent moveComponent = _moveComponent;
			if (moveComponent != null && !moveComponent.IsReleased)
			{
				_moveComponent.SetVelocity((instance.hypnoses ? Vector2.Left : Vector2.Right) * 20f);
			}
			isCrawl = true;
			instance.collisionFlags = 1;
			cell.CharacterDestroy(this);
		}
	}

	private void ProcessHitBoxOverlaps()
	{
		_hitScratch.Clear();
		if (!TryGetActiveWorldHitRect(out var rect))
		{
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(rect, base.gridPos.Y, includeAllLineCheck: true);
		for (int i = 0; i < charactersIntersectingRectList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = charactersIntersectingRectList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				_hitScratch.Add(towerDefenseCharacter);
				if (!_hitOverlaps.Contains(towerDefenseCharacter))
				{
					HandleHitBoxEntered(towerDefenseCharacter);
				}
			}
		}
		_hitOverlaps.RemoveWhere((TowerDefenseCharacter character) => !GodotObject.IsInstanceValid(character) || !_hitScratch.Contains(character));
		foreach (TowerDefenseCharacter item in _hitScratch)
		{
			_hitOverlaps.Add(item);
		}
	}

	private void HandleHitBoxEntered(TowerDefenseCharacter tdCharacter)
	{
		if (isCrawl && !tdCharacter.instance.die && !tdCharacter.instance.nearDie && !(tdCharacter is TowerDefenseGravestone) && !(tdCharacter is TowerDefenseCrater) && !(tdCharacter is TowerDefensePlantBowlingBase) && (!(tdCharacter is TowerDefenseZombie towerDefenseZombie) || towerDefenseZombie.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS) && CanCollision(tdCharacter.instance.maskFlags) && CheckDifferentCamp(tdCharacter.camp) && tdCharacter.IsTargetableFromLine(base.gridPos.Y))
		{
			AddBuffForzen(tdCharacter);
			if (tdCharacter is TowerDefenseZombie towerDefenseZombie2 && towerDefenseZombie2.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR)
			{
				towerDefenseZombie2.Die();
			}
		}
	}

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (GodotObject.IsInstanceValid(character) && !instance.sleep)
		{
			if (!isNut)
			{
				isNut = true;
				base.sprite.SetAnimation("In", loop: false, 0.2);
				base.sprite.AddAnimation("Nut", 0.0);
			}
			AddBuffForzen(character);
		}
	}

	public void AddBuffForzen(TowerDefenseCharacter character)
	{
		TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen = new TowerDefenseCharacterBuffFrozen();
		towerDefenseCharacterBuffFrozen.time = (float)forzenTime;
		towerDefenseCharacterBuffFrozen.iceSpeedDownTime = 0.0;
		character.BuffAdd(towerDefenseCharacterBuffFrozen);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["forzenTime"] = forzenTime,
			["isNut"] = isNut,
			["isCrawl"] = isCrawl
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		forzenTime = (data.ContainsKey("forzenTime") ? data["forzenTime"].AsDouble() : 5.0);
		isNut = data.ContainsKey("isNut") && data["isNut"].AsBool();
		isCrawl = data.ContainsKey("isCrawl") && data["isCrawl"].AsBool();
	}
}
