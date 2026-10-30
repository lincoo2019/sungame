using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseHammerMode : Node2D
{
	private static PackedScene _hammerExplosion;

	private AdobeAnimateSpriteBase hammer;

	public bool isAttack;

	public Godot.Collections.Array checkCharacterList = new Godot.Collections.Array();

	private static PackedScene HAMMER_EXPLOSION => _hammerExplosion ?? (_hammerExplosion = GD.Load<PackedScene>("res://Prefab/Particles/Explosion/Hammer/HammerExplosion.tscn"));

	public override void _Ready()
	{
		hammer = GetNode<AdobeAnimateSpriteBase>("%Hammer");
		hammer.OnAnimeCompleted += AnimeCompleted;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!TowerDefenseManager.Instance.currentControl.isGameFail)
		{
			hammer.GlobalPosition = GetGlobalMousePosition();
			if (Input.IsActionJustPressed("Press"))
			{
				AudioManager.Instance.AudioPlay("Swing");
				hammer.SetAnimation("WhackZombie", loop: false, 0.1);
				isAttack = true;
				checkCharacterList.Clear();
				Attack();
			}
		}
	}

	public async void Attack()
	{
		if (!isAttack)
		{
			return;
		}
		isAttack = false;
		Vector2 pos = hammer.GlobalPosition;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (TryRepairCraterAtMouse(pos))
		{
			return;
		}
		Rect2 checkRect = AabbShapeUtil.RectFromCenter(pos, new Vector2(40f, 80f));
		foreach (TowerDefenseCharacter charactersIntersectingRect in TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(checkRect))
		{
			if (checkCharacterList.Contains(charactersIntersectingRect))
			{
				return;
			}
			if (charactersIntersectingRect.isRise || charactersIntersectingRect is TowerDefenseGravestone || TowerDefenseManager.Instance.IsIZM2Mode() != (charactersIntersectingRect.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT))
			{
				continue;
			}
			PlayHammerHitEffect(charactersIntersectingRect.gridPos, hammer.GlobalPosition);
			checkCharacterList.Add(charactersIntersectingRect);
			double num = 400.0;
			if (charactersIntersectingRect.instance.HasAnyArmor)
			{
				num = Math.Min(num, charactersIntersectingRect.instance.armorList[0].hitPoints);
			}
			if (!(charactersIntersectingRect.Hurt(num) > 0.0) && !(charactersIntersectingRect.instance.hitpoints <= charactersIntersectingRect.config.hitpointsNearDeath))
			{
				return;
			}
			if ((double)GD.Randf() < 0.1)
			{
				for (int i = 0; i < 3; i++)
				{
					charactersIntersectingRect.SunCreate(charactersIntersectingRect.GetLogicalGlobalPosition(), 25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
				}
			}
			charactersIntersectingRect.Destroy();
			return;
		}
		TryRepairCraterAtMouse(pos);
	}

	private bool TryRepairCraterAtMouse(Vector2 pos)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2I mapGridPosFromMouse = instance.GetMapGridPosFromMouse(pos);
		if (!instance.CheckMapGridPosIn(mapGridPosFromMouse))
		{
			return false;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(mapGridPosFromMouse);
		return TryRepairCraterAtCell(mapCell, pos);
	}

	public bool TryRepairCraterAtCell(TowerDefenseCellInstance cell, Vector2 hitPosition)
	{
		TowerDefenseCrater towerDefenseCrater = (GodotObject.IsInstanceValid(cell) ? cell.GetRepairableCrater() : null);
		if (!GodotObject.IsInstanceValid(towerDefenseCrater))
		{
			return false;
		}
		PlayHammerHitEffect(towerDefenseCrater.gridPos, hitPosition);
		towerDefenseCrater.Destroy();
		return true;
	}

	private void PlayHammerHitEffect(Vector2I gridPos, Vector2 hitPosition)
	{
		AudioManager.Instance?.AudioPlay("Bonk");
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(characterNode))
		{
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(HAMMER_EXPLOSION, gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = hitPosition;
			characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (clip == "WhackZombie")
		{
			hammer.SetAnimation("Idle", loop: false, 0.1);
		}
	}
}
