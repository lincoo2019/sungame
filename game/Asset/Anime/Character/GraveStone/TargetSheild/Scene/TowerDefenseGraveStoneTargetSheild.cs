using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefenseGraveStoneTargetSheild : TowerDefenseGravestone
{
	private readonly HashSet<TowerDefenseZombie> _overlappingZombies = new HashSet<TowerDefenseZombie>();

	private readonly HashSet<TowerDefenseZombie> _overlapScratch = new HashSet<TowerDefenseZombie>();

	public override void _Ready()
	{
		base._Ready();
		Engine.IsEditorHint();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			ProcessHitBoxOverlaps();
		}
	}

	private void ProcessHitBoxOverlaps()
	{
		_overlapScratch.Clear();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || !inGame || nearDie || die || !TryGetActiveWorldHitRect(out var rect))
		{
			return;
		}
		List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(rect, base.gridPos.Y);
		for (int i = 0; i < charactersIntersectingRectList.Count; i++)
		{
			if (charactersIntersectingRectList[i] is TowerDefenseZombie towerDefenseZombie && CanShieldZombie(towerDefenseZombie))
			{
				_overlapScratch.Add(towerDefenseZombie);
				if (!_overlappingZombies.Contains(towerDefenseZombie))
				{
					HandleZombieEntered(towerDefenseZombie);
				}
			}
		}
		foreach (TowerDefenseZombie overlappingZombie in _overlappingZombies)
		{
			if (!GodotObject.IsInstanceValid(overlappingZombie) || !_overlapScratch.Contains(overlappingZombie))
			{
				HandleZombieExited(overlappingZombie);
			}
		}
		_overlappingZombies.RemoveWhere((TowerDefenseZombie zombie) => !GodotObject.IsInstanceValid(zombie) || !_overlapScratch.Contains(zombie));
		foreach (TowerDefenseZombie item in _overlapScratch)
		{
			_overlappingZombies.Add(item);
		}
	}

	private bool CanShieldZombie(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie.isRise)
		{
			return false;
		}
		if ((zombie.instance.maskFlags & 1) == 0)
		{
			return false;
		}
		if (zombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return false;
		}
		if (zombie.camp != base.camp)
		{
			return false;
		}
		if (zombie.gridPos.Y != base.gridPos.Y)
		{
			return false;
		}
		return true;
	}

	private void HandleZombieEntered(TowerDefenseZombie zombie)
	{
		double explosionHurt = zombie.instance.explosionHurt;
		if (explosionHurt != 0.0)
		{
			zombie.instance.explosionHurtSave = explosionHurt;
			zombie.instance.explosionHurt = 0.0;
		}
	}

	private void HandleZombieExited(TowerDefenseZombie zombie)
	{
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(zombie.gridPos);
		if ((mapCell == null || !mapCell.HasCharacter(base.config.name)) && zombie.instance.explosionHurt == 0.0)
		{
			double explosionHurtSave = zombie.instance.explosionHurtSave;
			if (explosionHurtSave >= 0.0)
			{
				zombie.instance.explosionHurt = explosionHurtSave;
			}
			else
			{
				zombie.instance.explosionHurt = -1.0;
			}
		}
	}
}
