using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSpringFumeShroom : TowerDefensePlant
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
			List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(rect, base.gridPos.Y);
			for (int i = 0; i < charactersIntersectingRectList.Count; i++)
			{
				HitboxEntered(charactersIntersectingRectList[i]);
			}
		}
	}

	public virtual async void HitboxEntered(TowerDefenseCharacter character)
	{
		if (!instance.sleep && !CanSleep() && !springList.Contains(character) && GodotObject.IsInstanceValid(character) && !character.die && !character.nearDie && !character.instance.invincible && CheckDifferentCamp(character.camp) && CanCollision(character.instance.maskFlags) && character.IsTargetableFromLine(base.gridPos.Y, includeAllLineCheck: false))
		{
			Hit(character);
			springList.Add(character);
			await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
			springList.Remove(character);
		}
	}

	public virtual void Hit(TowerDefenseCharacter character)
	{
		if (!TowerDefenseManager.Instance.IsGameRunning() || !(character is TowerDefenseZombie) || !CanCollision(character.instance.maskFlags) || !CanTarget(character) || (character.instance.unUseBuffFlags & 0x200) != 0)
		{
			return;
		}
		base.sprite.SetAnimation("Attack", loop: false, 0.2);
		base.sprite.AddAnimation("Idle", 0.0);
		if (((character.config is TowerDefenseZombieConfig towerDefenseZombieConfig) ? towerDefenseZombieConfig.physique : TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL) < TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE)
		{
			Hurt(50.0);
		}
		else
		{
			Hurt(100.0);
		}
		character.ySpeed = -200.0;
		Tween tween = character.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		float x = logicalGlobalPosition.X + TowerDefenseManager.Instance.GetMapGridSize().X * 2f;
		if (instance.hypnoses)
		{
			x = logicalGlobalPosition.X - TowerDefenseManager.Instance.GetMapGridSize().X * 2f;
		}
		tween.TweenMethod(Callable.From<Vector2>(character.SetLogicalGlobalPosition), logicalGlobalPosition, new Vector2(x, logicalGlobalPosition.Y), 2.0);
		if (character.Get("jumpOver").VariantType == Variant.Type.Nil)
		{
			return;
		}
		character.Set("jumpOver", true);
		if (character.Get("moveTween").VariantType != Variant.Type.Nil)
		{
			Tween tween2 = character.Get("moveTween").As<Tween>();
			if (GodotObject.IsInstanceValid(tween2) && tween2.IsRunning())
			{
				tween2.Kill();
			}
		}
		((TowerDefenseZombie)character).Walk();
	}

	public virtual void Flip()
	{
		if (!instance.sleep)
		{
			base.sprite.SetAnimation("Attack", loop: false, 0.2);
			base.sprite.AddAnimation("Idle", 0.0);
		}
		else
		{
			base.sprite.SetAnimation("Attack", loop: false, 0.2);
			base.sprite.AddAnimation("Sleep", 0.0);
		}
	}

	public virtual void BlockCharacter()
	{
		if (!instance.sleep && !CanSleep())
		{
			base.sprite.SetAnimation("Attack", loop: false, 0.2);
			base.sprite.AddAnimation("Idle", 0.0);
			Hurt(50.0);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
