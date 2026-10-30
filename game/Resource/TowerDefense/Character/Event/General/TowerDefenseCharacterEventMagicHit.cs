using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventMagicHit : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double num = 60.0;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterBuffConfig> buffList = new Array<TowerDefenseCharacterBuffConfig>();

	[Export(PropertyHint.None, "")]
	public bool plantsOnly;

	[Export(PropertyHint.None, "")]
	public bool allowOtherUnitTargets;

	[Export(PropertyHint.None, "")]
	public int slowRangeColumns;

	[Export(PropertyHint.None, "")]
	public int slowRangeLines;

	[Export(PropertyHint.None, "")]
	public double slowTime;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, num, buffList, plantsOnly, slowRangeColumns, slowRangeLines, slowTime, TowerDefenseEnum.CHARACTER_CAMP.ALL, allowOtherUnitTargets);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, num, buffList, plantsOnly, slowRangeColumns, slowRangeLines, slowTime, TowerDefenseEnum.CHARACTER_CAMP.ALL, allowOtherUnitTargets);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		bool blockEffects = projectile != null && GodotObject.IsInstanceValid(target) && target.ProjectileEffectsBlocked(projectile.damageFlags, projectile.fireMethodFlags);
		Run(projectile.GlobalPosition, target, num, buffList, plantsOnly, slowRangeColumns, slowRangeLines, slowTime, projectile.camp, allowOtherUnitTargets, blockEffects);
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, double num, Array<TowerDefenseCharacterBuffConfig> buffList, bool plantsOnly = false, int slowRangeColumns = 0, int slowRangeLines = 0, double slowTime = 0.0, TowerDefenseEnum.CHARACTER_CAMP casterCamp = TowerDefenseEnum.CHARACTER_CAMP.ALL, bool allowOtherUnitTargets = false, bool blockEffects = false)
	{
		if (!TowerDefenseManager.HasGameplayAuthority || !GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(target.instance) || target.die || target.isDestroy || (plantsOnly && !(target is TowerDefensePlant)))
		{
			return;
		}
		if (allowOtherUnitTargets)
		{
			if (target is TowerDefenseGravestone || target is TowerDefenseVase)
			{
				return;
			}
		}
		else if (!(target is TowerDefensePlant) && !(target is TowerDefenseZombie))
		{
			return;
		}
		if (target is TowerDefenseZombie && target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
		{
			return;
		}
		Vector2I gridPos = target.gridPos;
		int damageFlags = 67;
		TowerDefenseCharacterEventHurt.Run(pos, target, num, damageFlags);
		if (!blockEffects)
		{
			if (slowTime > 0.0)
			{
				ApplyAreaTimeSlow(gridPos, slowRangeColumns, slowRangeLines, slowTime, ResolveCasterCamp(casterCamp, target), target);
			}
			if (!target.die && !target.nearDie && !target.isDestroy)
			{
				TowerDefenseCharacterEventAddBuff.Run(target, buffList);
			}
		}
	}

	private static TowerDefenseEnum.CHARACTER_CAMP ResolveCasterCamp(TowerDefenseEnum.CHARACTER_CAMP casterCamp, TowerDefenseCharacter target)
	{
		if (casterCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT || casterCamp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			return casterCamp;
		}
		if (target.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			return TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		}
		return TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
	}

	public static void ApplyAreaTimeSlow(Vector2I center, int rangeColumns, int rangeLines, double time, TowerDefenseEnum.CHARACTER_CAMP casterCamp, TowerDefenseCharacter centerTarget = null)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(centerTarget))
		{
			TryApplyTimeSlow(centerTarget, casterCamp, time);
		}
		Rect2 checkRect = BuildGridAreaWorldRect(instance, center, Mathf.Max(0, rangeColumns), Mathf.Max(0, rangeLines));
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(checkRect, casterCamp));
		for (int i = 0; i < list.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = list[i];
			if (towerDefenseCharacter != centerTarget)
			{
				TryApplyTimeSlow(towerDefenseCharacter, casterCamp, time);
			}
		}
	}

	private static void TryApplyTimeSlow(TowerDefenseCharacter candidate, TowerDefenseEnum.CHARACTER_CAMP casterCamp, double time)
	{
		if (GodotObject.IsInstanceValid(candidate) && candidate.camp != casterCamp && (candidate is TowerDefensePlant || candidate is TowerDefenseZombie) && !(candidate is TowerDefensePlantBowlingBase) && GodotObject.IsInstanceValid(candidate.instance) && !candidate.die && !candidate.nearDie && !candidate.isDestroy)
		{
			BuffComponent buff = candidate.buff;
			if (buff != null && !buff.IsReleased && (!(candidate is TowerDefenseZombie) || candidate.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS))
			{
				TowerDefenseCharacterBuffTimeMagic towerDefenseCharacterBuffTimeMagic = new TowerDefenseCharacterBuffTimeMagic();
				towerDefenseCharacterBuffTimeMagic.time = time;
				candidate.buff.AddBuff(towerDefenseCharacterBuffTimeMagic);
			}
		}
	}

	private static Rect2 BuildGridAreaWorldRect(TowerDefenseManager manager, Vector2I center, int rangeColumns, int rangeLines)
	{
		Vector2 mapGridSize = manager.GetMapGridSize();
		Vector2 mapCellPos = manager.GetMapCellPos(new Vector2I(center.X - rangeColumns, center.Y - rangeLines));
		Vector2 size = new Vector2(mapGridSize.X * (float)(rangeColumns * 2 + 1), mapGridSize.Y * (float)(rangeLines * 2 + 1));
		return new Rect2(mapCellPos, size);
	}
}
