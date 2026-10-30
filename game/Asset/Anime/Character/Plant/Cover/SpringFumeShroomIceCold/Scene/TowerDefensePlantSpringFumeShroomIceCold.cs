using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSpringFumeShroomIceCold : TowerDefensePlant
{
	private BlockComponent _blockComponent;

	public bool over;

	public Array springList = new Array();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_blockComponent = componentManager.GetRuntime<BlockComponent>();
			if (_blockComponent != null)
			{
				_blockComponent.OnBlock += BlockCharacter;
				GetNode<Button>("SpriteGroup/Button").Pressed += Flip;
				_blockComponent.SetCheckRectangleSize(TowerDefenseManager.Instance.GetMapGridSize() * 0.5f);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (_blockComponent != null)
		{
			_blockComponent.OnBlock -= BlockCharacter;
		}
		_blockComponent = null;
	}

	public override bool CanSleep()
	{
		if (TowerDefenseManager.Instance.IsIZMMode())
		{
			return false;
		}
		return base.CanSleep();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			instance.canBeCollection = true;
			ProcessHitboxOverlaps();
		}
	}

	private void ProcessHitboxOverlaps()
	{
		if (TryGetActiveWorldHitRect(out var rect))
		{
			List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(rect, base.gridPos.Y, includeAllLineCheck: true);
			for (int i = 0; i < charactersIntersectingRectList.Count; i++)
			{
				HitboxEntered(charactersIntersectingRectList[i]);
			}
		}
	}

	public async void HitboxEntered(TowerDefenseCharacter character)
	{
		if (!instance.sleep && GodotObject.IsInstanceValid(character) && !springList.Contains(character) && !character.die && !character.nearDie && !character.instance.invincible && CheckDifferentCamp(character.camp) && CanCollision(character.instance.maskFlags) && character.IsTargetableFromLine(base.gridPos.Y))
		{
			Hit(character);
			springList.Add(character);
			await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
			springList.Remove(character);
		}
	}

	public void Hit(TowerDefenseCharacter character)
	{
		if (!TowerDefenseManager.Instance.IsGameRunning() || !(character is TowerDefenseZombie towerDefenseZombie) || !CanCollision(towerDefenseZombie.instance.maskFlags) || !CanTarget(towerDefenseZombie) || (towerDefenseZombie.instance.unUseBuffFlags & 0x200) != 0)
		{
			return;
		}
		base.sprite.SetAnimation("Shooting", loop: false, 0.2);
		base.sprite.AddAnimation("Idle", 0.0);
		if (((towerDefenseZombie.config is TowerDefenseZombieConfig towerDefenseZombieConfig) ? towerDefenseZombieConfig.physique : TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL) < TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE)
		{
			Hurt(50.0);
		}
		else
		{
			Hurt(100.0);
		}
		towerDefenseZombie.ySpeed = -200.0;
		Tween tween = towerDefenseZombie.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		Vector2 logicalGlobalPosition = towerDefenseZombie.GetLogicalGlobalPosition();
		float x = logicalGlobalPosition.X + TowerDefenseManager.Instance.GetMapGridSize().X * 2f;
		if (instance.hypnoses)
		{
			x = logicalGlobalPosition.X - TowerDefenseManager.Instance.GetMapGridSize().X * 2f;
		}
		tween.TweenMethod(Callable.From<Vector2>(towerDefenseZombie.SetLogicalGlobalPosition), logicalGlobalPosition, new Vector2(x, logicalGlobalPosition.Y), 2.0);
		if (towerDefenseZombie.Get("jumpOver").VariantType != Variant.Type.Nil)
		{
			towerDefenseZombie.Set("jumpOver", true);
			if (towerDefenseZombie.Get("moveTween").VariantType != Variant.Type.Nil)
			{
				Tween tween2 = towerDefenseZombie.Get("moveTween").As<Tween>();
				if (GodotObject.IsInstanceValid(tween2) && tween2.IsRunning())
				{
					tween2.Kill();
				}
			}
			towerDefenseZombie.Walk();
		}
		TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen = new TowerDefenseCharacterBuffFrozen();
		towerDefenseCharacterBuffFrozen.time = 3.0;
		towerDefenseCharacterBuffFrozen.iceSpeedDownTime = 15.0;
		towerDefenseZombie.BuffAdd(towerDefenseCharacterBuffFrozen);
	}

	public void Flip()
	{
		if (!instance.sleep)
		{
			base.sprite.SetAnimation("Shooting", loop: false, 0.2);
			base.sprite.AddAnimation("Idle", 0.0);
		}
		else
		{
			base.sprite.SetAnimation("Shooting", loop: false, 0.2);
			base.sprite.AddAnimation("Idle", 0.0);
		}
	}

	public void BlockCharacter()
	{
		base.sprite.SetAnimation("Shooting", loop: false, 0.2);
		base.sprite.AddAnimation("Idle", 0.0);
		Hurt(50.0);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["over"] = over };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
	}
}
