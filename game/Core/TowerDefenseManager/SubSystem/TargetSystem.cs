using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TargetSystem : Node
{
	private sealed class CharacterDistanceComparer : IComparer<TowerDefenseCharacter>
	{
		public Vector2 RefPos;

		public TowerDefenseEnum.TARGET_NEAR_METHOD Method;

		public bool FarMode;

		public bool FilterGravestone;

		public int GravestoneFirst;

		public bool CheckOffGround;

		public int Compare(TowerDefenseCharacter a, TowerDefenseCharacter b)
		{
			if (CheckOffGround)
			{
				int num = 2;
				bool flag = (a.instance.maskFlags & num) != 0;
				bool flag2 = (b.instance.maskFlags & num) != 0;
				if (!flag && flag2)
				{
					return 1;
				}
				if (flag && !flag2)
				{
					return -1;
				}
			}
			return CompareNormalDistance(a, b);
		}

		private int CompareNormalDistance(TowerDefenseCharacter a, TowerDefenseCharacter b)
		{
			if (FilterGravestone)
			{
				bool flag = a is TowerDefenseGravestone;
				bool flag2 = b is TowerDefenseGravestone;
				if (flag && !flag2)
				{
					return -GravestoneFirst;
				}
				if (!flag && flag2)
				{
					return GravestoneFirst;
				}
			}
			double distanceValue = GetDistanceValue(a.GetLogicalGlobalPosition());
			double distanceValue2 = GetDistanceValue(b.GetLogicalGlobalPosition());
			if (!FarMode)
			{
				return distanceValue.CompareTo(distanceValue2);
			}
			return distanceValue2.CompareTo(distanceValue);
		}

		private double GetDistanceValue(Vector2 position)
		{
			return (Method == TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION) ? position.DistanceSquaredTo(RefPos) : Math.Abs(position.X - RefPos.X);
		}
	}

	private struct RectTargetPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public TowerDefenseCharacter Character;

		public int Line;

		public bool CheckLine;

		public bool FilterGraveStone;

		public bool FilterVase;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (checkCharacter.die || checkCharacter.nearDie)
			{
				return false;
			}
			return CanConsiderLiveCharacter(checkCharacter);
		}

		private bool CanConsiderLiveCharacter(TowerDefenseCharacter checkCharacter)
		{
			if (checkCharacter.instance.invincible || !Character.CanTarget(checkCharacter) || !Character.CanCollision(checkCharacter.instance.maskFlags))
			{
				return false;
			}
			if (CheckLine && !checkCharacter.IsTargetableFromLine(Line))
			{
				return false;
			}
			if (checkCharacter is TowerDefenseCrater)
			{
				return false;
			}
			return CanConsiderItemAndGrave(checkCharacter);
		}

		private bool CanConsiderItemAndGrave(TowerDefenseCharacter checkCharacter)
		{
			if (checkCharacter is TowerDefenseItem { canCheck: false })
			{
				if (!FilterVase)
				{
					return checkCharacter is TowerDefenseVase;
				}
				return false;
			}
			if (FilterGraveStone)
			{
				return !(checkCharacter is TowerDefenseGravestone);
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter checkCharacter)
		{
			return CanConsider(checkCharacter);
		}
	}

	private struct TallRectTargetPredicate : ITowerDefenseCharacterRectPredicate, ITowerDefenseCharacterCandidatePredicate
	{
		public TowerDefenseCharacter Character;

		public int Line;

		public bool CheckLine;

		public double GroundRight;

		public bool CanConsider(TowerDefenseCharacter checkCharacter)
		{
			if (checkCharacter.die || checkCharacter.nearDie || checkCharacter.instance.invincible)
			{
				return false;
			}
			if (!checkCharacter.instance.canBeCollection || (double)checkCharacter.GetLogicalGlobalPosition().X > GroundRight)
			{
				return false;
			}
			if (!Character.CanTarget(checkCharacter) || checkCharacter.instance.height < TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
			{
				return false;
			}
			if (CheckLine && !checkCharacter.IsTargetableFromLine(Line))
			{
				return false;
			}
			if (checkCharacter is TowerDefensePlant && GodotObject.IsInstanceValid(checkCharacter.cell))
			{
				return !GodotObject.IsInstanceValid(checkCharacter.cell.characterLadder);
			}
			return true;
		}

		public bool Matches(TowerDefenseCharacter checkCharacter)
		{
			return CanConsider(checkCharacter);
		}
	}

	private TowerDefenseBattleCharacterRegistry _registry;

	private TowerDefenseManager _manager;

	private static readonly List<TowerDefenseCharacter> _sortBuffer = new List<TowerDefenseCharacter>(128);

	private static readonly List<TowerDefenseCharacter> _resultBuffer = new List<TowerDefenseCharacter>(128);

	private readonly CharacterDistanceComparer _distanceComparer = new CharacterDistanceComparer();

	private const int DisabledTargetPriorityPenalty = 10;

	private bool _IsBasicTargetValid(TowerDefenseCharacter checkCharacter, bool fliterGraveStone = true)
	{
		if (checkCharacter.instance.invincible || !checkCharacter.instance.canBeCollection)
		{
			return false;
		}
		if (checkCharacter is TowerDefenseCrater)
		{
			return false;
		}
		if (checkCharacter is TowerDefenseItem { canCheckTarget: false })
		{
			return false;
		}
		if (fliterGraveStone)
		{
			return !(checkCharacter is TowerDefenseGravestone);
		}
		return true;
	}

	private bool _IsBasicTargetValidWithCheck(TowerDefenseCharacter checkCharacter, bool fliterGraveStone = true)
	{
		if (!GodotObject.IsInstanceValid(checkCharacter))
		{
			return false;
		}
		return _IsBasicTargetValid(checkCharacter, fliterGraveStone);
	}

	private bool _IsAreaTargetValid(TowerDefenseCharacter checkCharacter, bool fliterGraveStone = true)
	{
		if (checkCharacter.die || checkCharacter.nearDie)
		{
			return false;
		}
		return _IsBasicTargetValid(checkCharacter, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromAreaList(TowerDefenseCharacter character, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(checkArea);
		return GetCharacterTargetFromRectList(character, checkRect, checkLine, fliterGraveStone, fliterVase);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		return GetCharacterTargetFromAreaList(character, checkArea, checkLine, fliterGraveStone, fliterVase);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromAreaWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, AabbArea2D checkArea, bool fliterGraveStone = true)
	{
		Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(checkArea);
		return GetCharacterTargetFromRectWithCollisionFlags(character, collisionFlags, checkRect, fliterGraveStone);
	}

	private bool IsCharacterArrayTargetCandidate(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool checkLine, bool fliterGraveStone, bool checkCollision)
	{
		if (!_IsBasicTargetValid(checkCharacter, fliterGraveStone))
		{
			return false;
		}
		if (!character.CanTarget(checkCharacter))
		{
			return false;
		}
		if (checkCollision && !character.CanCollision(checkCharacter.instance.maskFlags))
		{
			return false;
		}
		if (checkLine)
		{
			return checkCharacter.IsTargetableFromLine(character.gridPos.Y);
		}
		return true;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in array)
		{
			if (IsCharacterArrayTargetCandidate(character, item, checkLine, fliterGraveStone, checkCollision: true))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in array)
		{
			if (IsCharacterArrayTargetCandidate(character, item, checkLine, fliterGraveStone, checkCollision: false) && (collisionFlags & item.instance.maskFlags) != 0)
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public bool GetCharacterHasTargetFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true)
	{
		foreach (TowerDefenseCharacter item in _GetAreaIterable(checkArea, checkLine ? character.gridPos.Y : (-2147483648), checkLine))
		{
			if (_IsAreaTargetValid(item, fliterGraveStone) && character.CanTarget(item) && character.CanCollision(item.instance.maskFlags) && (!checkLine || item.IsTargetableFromLine(character.gridPos.Y)))
			{
				return true;
			}
		}
		return false;
	}

	public bool GetCharacterHasTarget(TowerDefenseCharacter character, bool checkLine = false, bool fliterGraveStone = true)
	{
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine, character.gridPos.Y))
		{
			if (_IsBasicTargetValidWithCheck(item, fliterGraveStone) && character.CanTarget(item) && character.CanCollision(item.instance.maskFlags))
			{
				return true;
			}
		}
		return false;
	}

	public bool GetCharacterHasTargetFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		foreach (TowerDefenseCharacter item in array)
		{
			if (IsCharacterArrayTargetCandidate(character, item, checkLine, fliterGraveStone, checkCollision: true))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsCharacterSingleIterableCandidate(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool fliterGravestone)
	{
		if (!_IsBasicTargetValidWithCheck(checkCharacter, fliterGravestone))
		{
			return false;
		}
		if (character.CanTarget(checkCharacter))
		{
			return character.CanCollision(checkCharacter.instance.maskFlags);
		}
		return false;
	}

	private bool IsCharacterSingleCollisionArrayCandidate(TowerDefenseCharacter character, int collisionFlags, TowerDefenseCharacter checkCharacter, bool checkLine, bool fliterGravestone)
	{
		if (!IsCharacterArrayTargetCandidate(character, checkCharacter, checkLine, fliterGravestone, checkCollision: false))
		{
			return false;
		}
		return (collisionFlags & checkCharacter.instance.maskFlags) != 0;
	}

	private static double GetCharacterSingleTargetDistance(Vector2 charPos, TowerDefenseCharacter checkCharacter, TowerDefenseEnum.TARGET_NEAR_METHOD method, bool fliterGravestone, bool farthest)
	{
		double num = _CalcDistance(charPos, checkCharacter.GetLogicalGlobalPosition(), method);
		if (fliterGravestone && checkCharacter is TowerDefenseGravestone)
		{
			if (!farthest)
			{
				return num + 1000000.0;
			}
			return num - 1000000.0;
		}
		return num;
	}

	private static void TrySetNearestCharacter(TowerDefenseCharacter candidate, double dist, ref TowerDefenseCharacter best, ref double bestDist)
	{
		if (dist < bestDist)
		{
			bestDist = dist;
			best = candidate;
		}
	}

	private static void TrySetFarthestCharacter(TowerDefenseCharacter candidate, double dist, ref TowerDefenseCharacter best, ref double bestDist)
	{
		if (dist > bestDist)
		{
			bestDist = dist;
			best = candidate;
		}
	}

	public TowerDefenseCharacter GetCharacterTargetFarthestFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		TowerDefenseCharacter best = null;
		double bestDist = -1.0;
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item in array)
		{
			if (IsCharacterSingleCollisionArrayCandidate(character, collisionFlags, item, checkLine, fliterGravestone))
			{
				double characterSingleTargetDistance = GetCharacterSingleTargetDistance(logicalGlobalPosition, item, method, fliterGravestone, farthest: true);
				TrySetFarthestCharacter(item, characterSingleTargetDistance, ref best, ref bestDist);
			}
		}
		return best;
	}

	public TowerDefenseCharacter GetCharacterTargetNearest(TowerDefenseCharacter character, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		TowerDefenseCharacter best = null;
		double bestDist = 1.0 / 0.0;
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine, character.gridPos.Y))
		{
			if (IsCharacterSingleIterableCandidate(character, item, fliterGravestone))
			{
				double characterSingleTargetDistance = GetCharacterSingleTargetDistance(logicalGlobalPosition, item, method, fliterGravestone, farthest: false);
				TrySetNearestCharacter(item, characterSingleTargetDistance, ref best, ref bestDist);
			}
		}
		return best;
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		TowerDefenseCharacter best = null;
		double bestDist = 1.0 / 0.0;
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item in array)
		{
			if (IsCharacterArrayTargetCandidate(character, item, checkLine, fliterGravestone, checkCollision: true))
			{
				double characterSingleTargetDistance = GetCharacterSingleTargetDistance(logicalGlobalPosition, item, method, fliterGravestone, farthest: false);
				TrySetNearestCharacter(item, characterSingleTargetDistance, ref best, ref bestDist);
			}
		}
		return best;
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		TowerDefenseCharacter best = null;
		double bestDist = 1.0 / 0.0;
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item in array)
		{
			if (IsCharacterSingleCollisionArrayCandidate(character, collisionFlags, item, checkLine, fliterGravestone))
			{
				double characterSingleTargetDistance = GetCharacterSingleTargetDistance(logicalGlobalPosition, item, method, fliterGravestone, farthest: false);
				TrySetNearestCharacter(item, characterSingleTargetDistance, ref best, ref bestDist);
			}
		}
		return best;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetList(TowerDefenseCharacter character, bool checkLine = false, bool checkCollision = false, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine, character.gridPos.Y))
		{
			if (_IsBasicTargetValidWithCheck(item, fliterGraveStone) && character.CanTarget(item) && (!checkCollision || character.CanCollision(item.instance.maskFlags)))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterTarget(TowerDefenseCharacter character, bool checkLine = false, bool checkCollision = false, bool fliterGraveStone = true)
	{
		return GetCharacterTargetList(character, checkLine, checkCollision, fliterGraveStone);
	}

	public bool HasTrackTarget(TowerDefenseCharacter parent, int collectionFlag, bool canTargetGargantuar, float groundRight)
	{
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine: false, parent.gridPos.Y))
		{
			if (_IsProjectileTargetValidWithCheck(item) && item.targetRegistrationComponent.canProjectileCheck && (canTargetGargantuar || !(item is TowerDefenseZombie) || item.instance.zombiePhysique < TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE) && !(item.GetLogicalGlobalPosition().X > groundRight) && parent.CanTarget(item) && (collectionFlag & item.instance.maskFlags) != 0)
			{
				return true;
			}
		}
		return false;
	}

	public List<TowerDefenseCharacter> GetCharacterColumnList(int column, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter columnCharacters in _registry.GetColumnCharactersList(column))
		{
			if (_IsBasicTargetValid(columnCharacters, fliterGraveStone) && (!(columnCharacters is TowerDefenseItem) || ((TowerDefenseItem)columnCharacters).canCheckTarget))
			{
				_resultBuffer.Add(columnCharacters);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterColumn(int column, bool fliterGraveStone = true)
	{
		return GetCharacterColumnList(column, fliterGraveStone);
	}

	public TargetSystem(TowerDefenseBattleCharacterRegistry registry, TowerDefenseManager manager)
	{
		_registry = registry;
		_manager = manager;
	}

	private static double _CalcDistance(Vector2 characterPos, Vector2 checkPosition, TowerDefenseEnum.TARGET_NEAR_METHOD method)
	{
		if (method == TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT)
		{
			return Math.Abs(checkPosition.X - characterPos.X);
		}
		return checkPosition.DistanceSquaredTo(characterPos);
	}

	private List<TowerDefenseCharacter> _SortByDistance(Vector2 refPos, TowerDefenseEnum.TARGET_NEAR_METHOD method, bool farMode, bool filterGravestone, int gravestoneFirst, bool checkOffGround)
	{
		_distanceComparer.RefPos = refPos;
		_distanceComparer.Method = method;
		_distanceComparer.FarMode = farMode;
		_distanceComparer.FilterGravestone = filterGravestone;
		_distanceComparer.GravestoneFirst = gravestoneFirst;
		_distanceComparer.CheckOffGround = checkOffGround;
		_sortBuffer.Sort(_distanceComparer);
		return _sortBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFarFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		_FillSortBufferWithCharacterTargetFromArray(character, array, checkLine, fliterGraveStone: false);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		return _SortByDistance(logicalGlobalPosition, method, farMode: true, fliterGravestone, -1, checkOffGround: false);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFarFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		_FillSortBufferWithCharacterTargetFromArrayWithCollisionFlags(character, collisionFlags, array, checkLine, fliterGraveStone: false);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		return _SortByDistance(logicalGlobalPosition, method, farMode: true, fliterGravestone, -1, checkOffGround: false);
	}

	private List<TowerDefenseCharacter> _GetIterable(bool checkLine, int line)
	{
		if (checkLine)
		{
			return _registry.GetCharactersForLineList(line);
		}
		return _registry.GetCleanCharactersList();
	}

	private List<TowerDefenseCharacter> _GetAreaIterable(AabbArea2D checkArea, int line = -2147483648, bool includeAllLineCheck = false)
	{
		Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(checkArea);
		return _registry.GetCharactersIntersectingRectList(checkRect, line, includeAllLineCheck);
	}

	public List<TowerDefenseCharacter> GetCharacterLineList(int line, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter charactersForLine in _registry.GetCharactersForLineList(line))
		{
			if (_IsBasicTargetValidWithCheck(charactersForLine, fliterGraveStone) && (!(charactersForLine is TowerDefenseItem) || ((TowerDefenseItem)charactersForLine).canCheckTarget))
			{
				_resultBuffer.Add(charactersForLine);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterLine(int line, bool fliterGraveStone = true)
	{
		return GetCharacterLineList(line, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNearFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		_FillSortBufferWithCharacterTargetFromArea(character, checkArea, checkLine);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		return _SortByDistance(logicalGlobalPosition, method, farMode: false, fliterGravestone, 1, checkOffGround: false);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNearFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		_FillSortBufferWithCharacterTargetFromArrayWithCollisionFlags(character, collisionFlags, array, checkLine, fliterGraveStone: false);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		return _SortByDistance(logicalGlobalPosition, method, farMode: false, fliterGravestone, -1, checkOffGround: false);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNearFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		_FillSortBufferWithCharacterTargetFromArray(character, array, checkLine, fliterGraveStone: false);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		return _SortByDistance(logicalGlobalPosition, method, farMode: false, fliterGravestone, -1, checkOffGround: false);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetNear(TowerDefenseCharacter character, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		_FillSortBufferWithCharacterTarget(character, checkLine);
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		return _SortByDistance(logicalGlobalPosition, method, farMode: false, fliterGravestone, 1, checkOffGround: false);
	}

	private static bool CanProjectileCollideWithCharacter(TowerDefenseProjectile projectile, TowerDefenseCharacter checkCharacter, int collisionFlags)
	{
		if (collisionFlags != -1)
		{
			return (collisionFlags & checkCharacter.instance.maskFlags) != 0;
		}
		return projectile.CanCollision(checkCharacter.instance.maskFlags);
	}

	private static bool CanRawProjectileCollideWithCharacter(TowerDefenseCharacter checkCharacter, int collisionFlags)
	{
		return (checkCharacter.instance.maskFlags & collisionFlags) != 0;
	}

	public bool GetProjectileHasTarget(TowerDefenseProjectile projectile, bool checkLine = false, bool fliterGraveStone = true)
	{
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine, projectile.gridPos.Y))
		{
			if (_IsProjectileTargetValidWithCheck(item, fliterGraveStone) && projectile.CanTarget(item) && item.CanCollision(projectile.config.collisionFlags))
			{
				return true;
			}
		}
		return false;
	}

	public bool GetProjectileHasTargetFromArea(TowerDefenseProjectile projectile, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true)
	{
		foreach (TowerDefenseCharacter item in _GetAreaIterable(checkArea, checkLine ? projectile.gridPos.Y : (-2147483648), checkLine))
		{
			if (_IsAreaTargetValid(item, fliterGraveStone) && projectile.CanTarget(item) && item.CanCollision(projectile.config.collisionFlags) && (!checkLine || item.IsTargetableFromLine(projectile.gridPos.Y)))
			{
				return true;
			}
		}
		return false;
	}

	public bool GetProjectileHasTargetFromArray(TowerDefenseProjectile projectile, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		foreach (TowerDefenseCharacter item in array)
		{
			if (_IsProjectileTargetValid(item, fliterGraveStone) && projectile.CanTarget(item) && item.CanCollision(projectile.config.collisionFlags) && (!checkLine || item.IsTargetableFromLine(projectile.gridPos.Y)))
			{
				return true;
			}
		}
		return false;
	}

	private bool TryGetProjectileInitialTrackDistance(TowerDefenseCharacter checkCharacter, Vector2 projectilePos, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, TowerDefenseEnum.TARGET_NEAR_METHOD method, bool fliterGravestone, bool hasOffGroundFlag, ulong physicsFrame, double edgeZ, out double dist)
	{
		dist = 0.0;
		if (!_IsProjectileTargetValidWithCheck(checkCharacter, fliterGravestone) || camp == checkCharacter.camp || !checkCharacter.IsHitBoxEnabled || (collisionFlags & checkCharacter.instance.maskFlags) == 0)
		{
			return false;
		}
		Vector2 globalPositionForPhysicsFrame = checkCharacter.GetGlobalPositionForPhysicsFrame(physicsFrame);
		if ((double)globalPositionForPhysicsFrame.X > edgeZ)
		{
			return false;
		}
		dist = ((method == TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT) ? Math.Abs(globalPositionForPhysicsFrame.X - projectilePos.X) : globalPositionForPhysicsFrame.DistanceSquaredTo(projectilePos));
		ApplyProjectileSingleTargetDistanceBias(checkCharacter, fliterGravestone, hasOffGroundFlag, ref dist);
		return true;
	}

	public TowerDefenseCharacter GetProjectileInitialTrackTarget(Vector2 projectilePos, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION, bool fliterGravestone = true)
	{
		double projectileInitialTrackEdgeZ = GetProjectileInitialTrackEdgeZ();
		TowerDefenseCharacter result = null;
		double bestDistance = 1.0 / 0.0;
		int bestPriority = 2147483647;
		bool hasOffGroundFlag = (collisionFlags & 2) != 0;
		ulong physicsFrames = Engine.GetPhysicsFrames();
		foreach (TowerDefenseCharacter cleanCharacters in _registry.GetCleanCharactersList())
		{
			if (TryGetProjectileInitialTrackDistance(cleanCharacters, projectilePos, collisionFlags, camp, method, fliterGravestone, hasOffGroundFlag, physicsFrames, projectileInitialTrackEdgeZ, out var dist) && _IsBetterProjectileTarget(cleanCharacters, dist, ref bestPriority, ref bestDistance))
			{
				result = cleanCharacters;
			}
		}
		return result;
	}

	private static bool IsProjectileNearestGridCandidate(TowerDefenseCharacter checkCharacter, Vector2I projGridPos, int maxSearchRangeX, int maxSearchRangeY)
	{
		Vector2I gridPos = checkCharacter.gridPos;
		if (Math.Abs(gridPos.X - projGridPos.X) <= maxSearchRangeX)
		{
			return Math.Abs(gridPos.Y - projGridPos.Y) <= maxSearchRangeY;
		}
		return false;
	}

	private static bool IsProjectileNearestCommonCandidate(TowerDefenseCharacter checkCharacter, bool fliterGravestone, double edgeZ, Vector2 checkPosition)
	{
		if ((double)checkPosition.X > edgeZ || checkCharacter.nearDie || checkCharacter.die)
		{
			return false;
		}
		if (checkCharacter.instance.canBeCollection && !checkCharacter.instance.invincible && !checkCharacter.instance.hologram && checkCharacter.targetRegistrationComponent.canProjectileCheck && !(checkCharacter is TowerDefenseCrater) && !(checkCharacter is TowerDefenseItem { canCheckTarget: false }) && (!fliterGravestone || !(checkCharacter is TowerDefenseGravestone)))
		{
			return checkCharacter.IsHitBoxEnabled;
		}
		return false;
	}

	private static bool IsDataProjectileNearestCandidate(TowerDefenseCharacter checkCharacter, TowerDefenseEnum.CHARACTER_CAMP camp, int collisionFlags, bool fliterGravestone, double edgeZ, Vector2I projGridPos, int maxSearchRangeX, int maxSearchRangeY, ulong physicsFrame, out Vector2 checkPosition)
	{
		checkPosition = default(Vector2);
		if (!GodotObject.IsInstanceValid(checkCharacter))
		{
			return false;
		}
		if (!IsProjectileNearestGridCandidate(checkCharacter, projGridPos, maxSearchRangeX, maxSearchRangeY))
		{
			return false;
		}
		checkPosition = ((physicsFrame == 18446744073709551615uL) ? checkCharacter.GetLogicalGlobalPosition() : checkCharacter.GetGlobalPositionForPhysicsFrame(physicsFrame));
		if (IsProjectileNearestCommonCandidate(checkCharacter, fliterGravestone, edgeZ, checkPosition) && camp != checkCharacter.camp)
		{
			return CanRawProjectileCollideWithCharacter(checkCharacter, collisionFlags);
		}
		return false;
	}

	public TowerDefenseCharacter GetProjectileTargetNearest(Vector2 pos, Vector2I gridPos, int collisionFlags, TowerDefenseEnum.CHARACTER_CAMP camp, double speed, bool fliterGravestone = true, ulong physicsFrame = 18446744073709551615uL, bool deprioritizeDisabledTargets = false)
	{
		TowerDefenseCharacter result = null;
		double bestDistance = 1.0 / 0.0;
		int bestPriority = 2147483647;
		bool flag = (collisionFlags & 2) != 0;
		double projectileNearestEdgeZ = GetProjectileNearestEdgeZ();
		int num = Math.Min(20, (int)(speed / 100.0) + 5);
		int num2 = 10;
		foreach (TowerDefenseCharacter charactersForGridWindow in _registry.GetCharactersForGridWindowList(gridPos.X, gridPos.Y, num, num2))
		{
			if (IsDataProjectileNearestCandidate(charactersForGridWindow, camp, collisionFlags, fliterGravestone, projectileNearestEdgeZ, gridPos, num, num2, physicsFrame, out var checkPosition))
			{
				double num3 = checkPosition.DistanceSquaredTo(pos);
				if (flag && (charactersForGridWindow.instance.maskFlags & 2) != 0)
				{
					num3 *= 0.5;
				}
				if (_IsBetterProjectileTarget(charactersForGridWindow, num3, ref bestPriority, ref bestDistance, deprioritizeDisabledTargets))
				{
					result = charactersForGridWindow;
				}
			}
		}
		return result;
	}

	private static double GetProjectileNearestEdgeZ()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (GodotObject.IsInstanceValid(mapFeature) && GodotObject.IsInstanceValid(mapFeature.config))
		{
			return mapFeature.config.edge.Z;
		}
		return 1.0 / 0.0;
	}

	private static bool IsNodeProjectileNearestCandidate(TowerDefenseProjectile projectile, TowerDefenseCharacter checkCharacter, int collisionFlags, bool fliterGravestone, double edgeZ, Vector2I projGridPos, int maxSearchRangeX, int maxSearchRangeY, out Vector2 checkPosition)
	{
		checkPosition = default(Vector2);
		if (!GodotObject.IsInstanceValid(checkCharacter))
		{
			return false;
		}
		if (!IsProjectileNearestGridCandidate(checkCharacter, projGridPos, maxSearchRangeX, maxSearchRangeY))
		{
			return false;
		}
		checkPosition = checkCharacter.GetLogicalGlobalPosition();
		if (IsProjectileNearestCommonCandidate(checkCharacter, fliterGravestone, edgeZ, checkPosition) && projectile.CanTarget(checkCharacter))
		{
			return CanProjectileCollideWithCharacter(projectile, checkCharacter, collisionFlags);
		}
		return false;
	}

	public TowerDefenseCharacter GetProjectileTargetNearest(TowerDefenseProjectile projectile, int collisionFlags = -1, bool fliterGravestone = true)
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		TowerDefenseCharacter result = null;
		double bestDistance = 1.0 / 0.0;
		int bestPriority = 2147483647;
		bool flag = (projectile.config.collisionFlags & 2) != 0;
		Vector2 globalPosition = projectile.GlobalPosition;
		double edgeZ = mapFeature.config.edge.Z;
		Vector2I gridPos = projectile.gridPos;
		int num = Math.Min(20, (int)(projectile.speed / 100.0) + 5);
		int num2 = 10;
		foreach (TowerDefenseCharacter charactersForGridWindow in _registry.GetCharactersForGridWindowList(gridPos.X, gridPos.Y, num, num2))
		{
			if (IsNodeProjectileNearestCandidate(projectile, charactersForGridWindow, collisionFlags, fliterGravestone, edgeZ, gridPos, num, num2, out var checkPosition))
			{
				double num3 = checkPosition.DistanceSquaredTo(globalPosition);
				if (flag && (charactersForGridWindow.instance.maskFlags & 2) != 0)
				{
					num3 *= 0.5;
				}
				if (_IsBetterProjectileTarget(charactersForGridWindow, num3, ref bestPriority, ref bestDistance))
				{
					result = charactersForGridWindow;
				}
			}
		}
		return result;
	}

	private bool TryGetNearestProjectileTargetDistance(TowerDefenseProjectile projectile, TowerDefenseCharacter checkCharacter, int collisionFlags, TowerDefenseEnum.TARGET_NEAR_METHOD method, bool fliterGravestone, bool hasOffGroundFlag, Vector2 projectilePos, double edgeZ, out double dist)
	{
		dist = 0.0;
		if (!_IsProjectileTargetValidWithCheck(checkCharacter, fliterGravestone) || !projectile.CanTarget(checkCharacter) || !checkCharacter.IsHitBoxEnabled)
		{
			return false;
		}
		if (collisionFlags != -1)
		{
			if ((collisionFlags & checkCharacter.instance.maskFlags) == 0)
			{
				return false;
			}
		}
		else if (!projectile.CanCollision(checkCharacter.instance.maskFlags))
		{
			return false;
		}
		Vector2 logicalGlobalPosition = checkCharacter.GetLogicalGlobalPosition();
		if ((double)logicalGlobalPosition.X > edgeZ)
		{
			return false;
		}
		dist = _CalcDistance(projectilePos, logicalGlobalPosition, method);
		ApplyProjectileSingleTargetDistanceBias(checkCharacter, fliterGravestone, hasOffGroundFlag, ref dist);
		return true;
	}

	public TowerDefenseCharacter GetProjectileTargetNearestProjectile(TowerDefenseProjectile projectile, int collisionFlags = -1, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool fliterGravestone = true)
	{
		TowerDefenseCharacter result = null;
		double bestDistance = 1.0 / 0.0;
		int bestPriority = 2147483647;
		bool hasOffGroundFlag = (projectile.config.collisionFlags & 2) != 0;
		Vector2 globalPosition = projectile.GlobalPosition;
		double edgeZ = TowerDefenseManager.GetMapFeature().config.edge.Z;
		foreach (TowerDefenseCharacter cleanCharacters in _registry.GetCleanCharactersList())
		{
			if (TryGetNearestProjectileTargetDistance(projectile, cleanCharacters, collisionFlags, method, fliterGravestone, hasOffGroundFlag, globalPosition, edgeZ, out var dist) && _IsBetterProjectileTarget(cleanCharacters, dist, ref bestPriority, ref bestDistance))
			{
				result = cleanCharacters;
			}
		}
		return result;
	}

	private bool IsProjectileNearListCandidate(TowerDefenseProjectile projectile, TowerDefenseCharacter checkCharacter, int collisionFlags, bool fliterGravestone, bool requireHitBox, double edgeZ)
	{
		if (!_IsProjectileTargetValidWithCheck(checkCharacter, fliterGravestone))
		{
			return false;
		}
		if (!projectile.CanTarget(checkCharacter))
		{
			return false;
		}
		if (requireHitBox && !checkCharacter.IsHitBoxEnabled)
		{
			return false;
		}
		if (!CanProjectileCollideWithCharacter(projectile, checkCharacter, collisionFlags))
		{
			return false;
		}
		return (double)checkCharacter.GetLogicalGlobalPosition().X <= edgeZ;
	}

	private List<TowerDefenseCharacter> GetProjectileTargetNearCore(TowerDefenseProjectile projectile, int collisionFlags, TowerDefenseEnum.TARGET_NEAR_METHOD method, bool checkLine, bool fliterGravestone, bool requireHitBox)
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		_sortBuffer.Clear();
		double edgeZ = mapFeature.config.edge.Z;
		Vector2 globalPosition = projectile.GlobalPosition;
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine, projectile.gridPos.Y))
		{
			if (IsProjectileNearListCandidate(projectile, item, collisionFlags, fliterGravestone, requireHitBox, edgeZ))
			{
				_sortBuffer.Add(item);
			}
		}
		bool checkOffGround = (projectile.config.collisionFlags & 2) != 0;
		return _SortByDistance(globalPosition, method, farMode: false, fliterGravestone, 1, checkOffGround);
	}

	public List<TowerDefenseCharacter> GetProjectileTargetNear(TowerDefenseProjectile projectile, int collisionFlags = -1, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false)
	{
		return GetProjectileTargetNearCore(projectile, collisionFlags, method, checkLine, fliterGravestone, requireHitBox: false);
	}

	public List<TowerDefenseCharacter> GetProjectileTargetNearProjectile(TowerDefenseProjectile projectile, int collisionFlags = -1, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = true)
	{
		return GetProjectileTargetNearCore(projectile, collisionFlags, method, checkLine, fliterGravestone, requireHitBox: true);
	}

	private static int _GetProjectileTargetPriority(TowerDefenseCharacter character, bool deprioritizeDisabledTargets)
	{
		int num = ((!(character is TowerDefenseZombie) && !(character is TowerDefensePlant)) ? 1 : 0);
		if (deprioritizeDisabledTargets && IsDisabledProjectileTarget(character))
		{
			num += 10;
		}
		return num;
	}

	private static bool IsDisabledProjectileTarget(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character?.instance))
		{
			return false;
		}
		return character.IsIncapacitatedTarget();
	}

	private static bool _IsBetterProjectileTarget(TowerDefenseCharacter candidate, double candidateDistance, ref int bestPriority, ref double bestDistance, bool deprioritizeDisabledTargets = false)
	{
		int num = _GetProjectileTargetPriority(candidate, deprioritizeDisabledTargets);
		if (num < bestPriority)
		{
			bestPriority = num;
			bestDistance = candidateDistance;
			return true;
		}
		if (num == bestPriority && candidateDistance < bestDistance)
		{
			bestDistance = candidateDistance;
			return true;
		}
		return false;
	}

	private static void ApplyProjectileSingleTargetDistanceBias(TowerDefenseCharacter checkCharacter, bool fliterGravestone, bool hasOffGroundFlag, ref double dist)
	{
		if (hasOffGroundFlag && (checkCharacter.instance.maskFlags & 2) != 0)
		{
			dist *= 0.5;
		}
		if (fliterGravestone && checkCharacter is TowerDefenseGravestone)
		{
			dist += 1000000.0;
		}
	}

	private static double GetProjectileInitialTrackEdgeZ()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature) || !GodotObject.IsInstanceValid(mapFeature.config))
		{
			return 1.0 / 0.0;
		}
		return mapFeature.config.edge.Z;
	}

	public List<TowerDefenseCharacter> GetProjectileTargetFromArea(TowerDefenseProjectile projectile, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in _GetAreaIterable(checkArea, checkLine ? projectile.gridPos.Y : (-2147483648), checkLine))
		{
			if (_IsAreaTargetValid(item, fliterGraveStone) && projectile.CanTarget(item) && projectile.CanCollision(item.instance.maskFlags) && (!checkLine || item.IsTargetableFromLine(projectile.gridPos.Y)))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetProjectileTargetFromArray(TowerDefenseProjectile projectile, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in array)
		{
			if (_IsProjectileTargetValid(item, fliterGraveStone) && projectile.CanTarget(item) && item.CanCollision(projectile.config.collisionFlags) && (!checkLine || item.IsTargetableFromLine(projectile.gridPos.Y)))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetProjectileTargetList(TowerDefenseProjectile projectile, bool checkLine = false, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine, projectile.gridPos.Y))
		{
			if (_IsProjectileTargetValidWithCheck(item, fliterGraveStone) && projectile.CanTarget(item) && item.CanCollision(projectile.config.collisionFlags))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetProjectileTarget(TowerDefenseProjectile projectile, bool checkLine = false, bool fliterGraveStone = true)
	{
		return GetProjectileTargetList(projectile, checkLine, fliterGraveStone);
	}

	private bool _IsProjectileTargetValid(TowerDefenseCharacter checkCharacter, bool fliterGraveStone = true)
	{
		if (!_IsBasicTargetValid(checkCharacter, fliterGraveStone))
		{
			return false;
		}
		if (!checkCharacter.targetRegistrationComponent.canProjectileCheck)
		{
			return false;
		}
		if (!checkCharacter.nearDie)
		{
			return !checkCharacter.die;
		}
		return false;
	}

	private bool _IsProjectileTargetValidWithCheck(TowerDefenseCharacter checkCharacter, bool fliterGraveStone = true)
	{
		if (!GodotObject.IsInstanceValid(checkCharacter))
		{
			return false;
		}
		return _IsProjectileTargetValid(checkCharacter, fliterGraveStone);
	}

	private static bool CanUseRayLineCharacter(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool checkLine, bool fliterGravestone, bool fliterVase)
	{
		if (checkCharacter.die || checkCharacter.nearDie)
		{
			return false;
		}
		if (!character.CanTarget(checkCharacter))
		{
			return false;
		}
		if (!character.CanCollision(checkCharacter.instance.maskFlags))
		{
			return false;
		}
		if (checkCharacter is TowerDefenseCrater)
		{
			return false;
		}
		if (checkCharacter is TowerDefenseItem { canCheck: false })
		{
			if (fliterVase)
			{
				return false;
			}
			if (!(checkCharacter is TowerDefenseVase))
			{
				return false;
			}
		}
		if (fliterGravestone && checkCharacter is TowerDefenseGravestone)
		{
			return false;
		}
		if (checkLine)
		{
			return checkCharacter.IsTargetableFromLine(character.gridPos.Y);
		}
		return true;
	}

	private TowerDefenseCharacter FindNearestRayLineCharacter(TowerDefenseCharacter character, Rect2 lineRect, bool checkLine, bool fliterGravestone, bool fliterVase)
	{
		TowerDefenseCharacter result = null;
		float num = 1f / 0f;
		foreach (TowerDefenseCharacter item in _registry.GetCharactersIntersectingRectListExcludingCamp(lineRect, character.camp))
		{
			if (CanUseRayLineCharacter(character, item, checkLine, fliterGravestone, fliterVase))
			{
				float x = item.WorldHitRect.Position.X;
				if (x < num)
				{
					num = x;
					result = item;
				}
			}
		}
		return result;
	}

	private static void UpdateRayNearestByMethod(Vector2 characterPosition, TowerDefenseCharacter lineCharacter, TowerDefenseEnum.TARGET_NEAR_METHOD method, ref double bestDistance, ref TowerDefenseCharacter returnCharacter)
	{
		switch (method)
		{
		case TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT:
		{
			double num2 = Math.Abs(lineCharacter.GetLogicalGlobalPosition().X - characterPosition.X);
			if (num2 < bestDistance)
			{
				bestDistance = num2;
				returnCharacter = lineCharacter;
			}
			break;
		}
		case TowerDefenseEnum.TARGET_NEAR_METHOD.POSITION:
		{
			double num = lineCharacter.GetLogicalGlobalPosition().DistanceSquaredTo(characterPosition);
			if (num < bestDistance)
			{
				bestDistance = num;
				returnCharacter = lineCharacter;
			}
			break;
		}
		}
	}

	public TowerDefenseCharacter GetNearCharacter(TowerDefenseCharacter character, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool checkLine = false, bool fliterGravestone = false, bool fliterVase = true)
	{
		TowerDefenseCharacter returnCharacter = null;
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		double bestDistance = 1.0 / 0.0;
		for (int i = 0; i < _manager.gridNum.Y; i++)
		{
			int y = i + 1;
			Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, y));
			float num = (float)_manager.GetMapGroundRight();
			Rect2 lineRect = new Rect2(new Vector2(mapCellPlantPos.X, mapCellPlantPos.Y - 0.5f), new Vector2(num - mapCellPlantPos.X, 1f));
			TowerDefenseCharacter towerDefenseCharacter = FindNearestRayLineCharacter(character, lineRect, checkLine, fliterGravestone, fliterVase);
			if (towerDefenseCharacter != null)
			{
				UpdateRayNearestByMethod(logicalGlobalPosition, towerDefenseCharacter, method, ref bestDistance, ref returnCharacter);
			}
		}
		return returnCharacter;
	}

	private bool IsRectCollisionFlagCandidate(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, int collisionFlags, bool fliterGraveStone)
	{
		if (!_IsAreaTargetValid(checkCharacter, fliterGraveStone))
		{
			return false;
		}
		if (character.CanTarget(checkCharacter))
		{
			return (collisionFlags & checkCharacter.instance.maskFlags) != 0;
		}
		return false;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromRectWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, Rect2 checkRect, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in _registry.GetCharactersIntersectingRectListExcludingCamp(checkRect, character.camp))
		{
			if (IsRectCollisionFlagCandidate(character, item, collisionFlags, fliterGraveStone))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromRectWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, Rect2 checkRect, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in _registry.GetCharactersIntersectingRectListExcludingCamp(checkRect, character.camp, character.gridPos.Y, includeAllLineCheck: true))
		{
			if (IsRectCollisionFlagCandidate(character, item, collisionFlags, fliterGraveStone) && item.IsTargetableFromLine(character.gridPos.Y))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public bool GetCharacterHasTargetFromRect(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		int items = 0;
		try
		{
			int y = character.gridPos.Y;
			RectTargetPredicate predicate = new RectTargetPredicate
			{
				Character = character,
				Line = y,
				CheckLine = checkLine,
				FilterGraveStone = fliterGraveStone,
				FilterVase = fliterVase
			};
			TowerDefenseCharacter match;
			bool num = _registry.TryFindCharacterIntersectingRectExcludingCamp(checkRect, checkLine ? y : (-2147483648), checkLine, character.camp, ref predicate, out match);
			items = (num ? 1 : 0);
			return num;
		}
		finally
		{
			TowerDefensePerfProfiler.End("target.rectHas", startTicks, items);
		}
	}

	private bool IsRectNearestCandidate(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool fliterGravestone)
	{
		if (!_IsAreaTargetValid(checkCharacter, fliterGravestone))
		{
			return false;
		}
		if (!character.CanTarget(checkCharacter) || !character.CanCollision(checkCharacter.instance.maskFlags))
		{
			return false;
		}
		return checkCharacter.IsTargetableFromLine(character.gridPos.Y);
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromRect(TowerDefenseCharacter character, Rect2 checkRect, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool fliterGravestone = false)
	{
		TowerDefenseCharacter result = null;
		double num = 1.0 / 0.0;
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		foreach (TowerDefenseCharacter item in _registry.GetCharactersIntersectingRectListExcludingCamp(checkRect, character.camp, character.gridPos.Y, includeAllLineCheck: true))
		{
			if (IsRectNearestCandidate(character, item, fliterGravestone))
			{
				double num2 = _CalcDistance(logicalGlobalPosition, item.GetLogicalGlobalPosition(), method);
				if (fliterGravestone && item is TowerDefenseGravestone)
				{
					num2 += 1000000.0;
				}
				if (num2 < num)
				{
					num = num2;
					result = item;
				}
			}
		}
		return result;
	}

	public TowerDefenseCharacter GetCharacterTargetNearestFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, TowerDefenseEnum.TARGET_NEAR_METHOD method = TowerDefenseEnum.TARGET_NEAR_METHOD.DEFAULT, bool fliterGravestone = false)
	{
		Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(checkArea);
		return GetCharacterTargetNearestFromRect(character, checkRect, method, fliterGravestone);
	}

	public List<TowerDefenseCharacter> GetCharactersIntersectingRect(Rect2 checkRect, bool checkLine = false, int line = 0)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		try
		{
			_resultBuffer.Clear();
			List<TowerDefenseCharacter> charactersIntersectingRectList = _registry.GetCharactersIntersectingRectList(checkRect, checkLine ? line : (-2147483648), checkLine);
			for (int i = 0; i < charactersIntersectingRectList.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = charactersIntersectingRectList[i];
				if (!towerDefenseCharacter.die && !towerDefenseCharacter.nearDie)
				{
					_resultBuffer.Add(towerDefenseCharacter);
				}
			}
			return _resultBuffer;
		}
		finally
		{
			TowerDefensePerfProfiler.End("target.rawRect", startTicks, _resultBuffer.Count);
		}
	}

	public List<TowerDefenseCharacter> GetOverlappingCharactersFromRect(Rect2 checkRect)
	{
		return GetCharactersIntersectingRect(checkRect);
	}

	public TowerDefenseCharacter GetTallCharacterTargetFromRect(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, int line = 0, double groundRight = 1.0 / 0.0)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		TowerDefenseCharacter match = null;
		try
		{
			TallRectTargetPredicate predicate = new TallRectTargetPredicate
			{
				Character = character,
				Line = line,
				CheckLine = checkLine,
				GroundRight = groundRight
			};
			_registry.TryFindCharacterIntersectingRectExcludingCamp(checkRect, checkLine ? line : (-2147483648), checkLine, character.camp, ref predicate, out match);
			return match;
		}
		finally
		{
			TowerDefensePerfProfiler.End("target.rawRectTall", startTicks, GodotObject.IsInstanceValid(match) ? 1 : 0);
		}
	}

	private static bool IsRectTargetCandidate(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool checkLine, int line, bool fliterGraveStone, bool fliterVase)
	{
		if (checkCharacter.die || checkCharacter.nearDie || checkCharacter.instance.invincible)
		{
			return false;
		}
		if (!character.CanTarget(checkCharacter) || !character.CanCollision(checkCharacter.instance.maskFlags) || checkCharacter is TowerDefenseCrater)
		{
			return false;
		}
		if (checkCharacter is TowerDefenseItem { canCheck: false })
		{
			if (fliterVase)
			{
				return false;
			}
			if (!(checkCharacter is TowerDefenseVase))
			{
				return false;
			}
		}
		if (fliterGraveStone && checkCharacter is TowerDefenseGravestone)
		{
			return false;
		}
		if (checkLine)
		{
			return checkCharacter.IsTargetableFromLine(line);
		}
		return true;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromRectList(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		try
		{
			_resultBuffer.Clear();
			int y = character.gridPos.Y;
			foreach (TowerDefenseCharacter item in _registry.GetCharactersIntersectingRectListExcludingCamp(checkRect, character.camp, checkLine ? y : (-2147483648), checkLine))
			{
				if (IsRectTargetCandidate(character, item, checkLine, y, fliterGraveStone, fliterVase))
				{
					_resultBuffer.Add(item);
				}
			}
			return _resultBuffer;
		}
		finally
		{
			TowerDefensePerfProfiler.End("target.rectList", startTicks, _resultBuffer.Count);
		}
	}

	public List<TowerDefenseCharacter> GetCharacterTargetFromRect(TowerDefenseCharacter character, Rect2 checkRect, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		return GetCharacterTargetFromRectList(character, checkRect, checkLine, fliterGraveStone, fliterVase);
	}

	private void _FillSortBufferWithCharacterTargetFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, bool checkLine = false, bool fliterGraveStone = true, bool fliterVase = true)
	{
		_sortBuffer.Clear();
		foreach (TowerDefenseCharacter item in _GetAreaIterable(checkArea, checkLine ? character.gridPos.Y : (-2147483648), checkLine))
		{
			if (_CanAddAreaTarget(character, item, checkLine, fliterGraveStone, fliterVase))
			{
				_sortBuffer.Add(item);
			}
		}
	}

	private bool _CanAddAreaTarget(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool checkLine, bool fliterGraveStone, bool fliterVase)
	{
		if (checkCharacter.die || checkCharacter.nearDie)
		{
			return false;
		}
		if (!character.CanTarget(checkCharacter) || !character.CanCollision(checkCharacter.instance.maskFlags))
		{
			return false;
		}
		if (checkCharacter is TowerDefenseCrater)
		{
			return false;
		}
		return _CanAddAreaItemAndLineTarget(character, checkCharacter, checkLine, fliterGraveStone, fliterVase);
	}

	private bool _CanAddAreaItemAndLineTarget(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool checkLine, bool fliterGraveStone, bool fliterVase)
	{
		if (checkCharacter is TowerDefenseItem { canCheck: false })
		{
			if (fliterVase)
			{
				return false;
			}
			if (!(checkCharacter is TowerDefenseVase))
			{
				return false;
			}
		}
		if (fliterGraveStone && checkCharacter is TowerDefenseGravestone)
		{
			return false;
		}
		if (checkLine)
		{
			return checkCharacter.IsTargetableFromLine(character.gridPos.Y);
		}
		return true;
	}

	private void _FillSortBufferWithCharacterTargetFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		_sortBuffer.Clear();
		foreach (TowerDefenseCharacter item in array)
		{
			if (_CanAddArrayTarget(character, item, checkLine, fliterGraveStone))
			{
				_sortBuffer.Add(item);
			}
		}
	}

	private bool _CanAddArrayTarget(TowerDefenseCharacter character, TowerDefenseCharacter checkCharacter, bool checkLine, bool fliterGraveStone)
	{
		if (!_IsBasicTargetValid(checkCharacter, fliterGraveStone))
		{
			return false;
		}
		if (!character.CanTarget(checkCharacter) || !character.CanCollision(checkCharacter.instance.maskFlags))
		{
			return false;
		}
		if (checkLine)
		{
			return checkCharacter.IsTargetableFromLine(character.gridPos.Y);
		}
		return true;
	}

	private void _FillSortBufferWithCharacterTarget(TowerDefenseCharacter character, bool checkLine = false, bool fliterGraveStone = true)
	{
		_sortBuffer.Clear();
		foreach (TowerDefenseCharacter item in _GetIterable(checkLine, character.gridPos.Y))
		{
			if (_IsBasicTargetValidWithCheck(item, fliterGraveStone) && character.CanTarget(item))
			{
				_sortBuffer.Add(item);
			}
		}
	}

	private void _FillSortBufferWithCharacterTargetFromArrayWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, List<TowerDefenseCharacter> array, bool checkLine = false, bool fliterGraveStone = true)
	{
		_sortBuffer.Clear();
		foreach (TowerDefenseCharacter item in array)
		{
			if (_CanAddCollisionArrayTarget(character, collisionFlags, item, checkLine, fliterGraveStone))
			{
				_sortBuffer.Add(item);
			}
		}
	}

	private bool _CanAddCollisionArrayTarget(TowerDefenseCharacter character, int collisionFlags, TowerDefenseCharacter checkCharacter, bool checkLine, bool fliterGraveStone)
	{
		if (!_IsBasicTargetValid(checkCharacter, fliterGraveStone))
		{
			return false;
		}
		if (!character.CanTarget(checkCharacter) || (collisionFlags & checkCharacter.instance.maskFlags) == 0)
		{
			return false;
		}
		if (checkLine)
		{
			return checkCharacter.IsTargetableFromLine(character.gridPos.Y);
		}
		return true;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromArea(TowerDefenseCharacter character, AabbArea2D checkArea, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in _GetAreaIterable(checkArea, character.gridPos.Y, includeAllLineCheck: true))
		{
			if (_IsAreaTargetValid(item, fliterGraveStone) && character.CanTarget(item) && character.CanCollision(item.instance.maskFlags) && item.IsTargetableFromLine(character.gridPos.Y))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromAreaWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, AabbArea2D checkArea, bool fliterGraveStone = true)
	{
		Rect2 checkRect = AabbShapeUtil.ComputeAreaWorldRect(checkArea);
		return GetCharacterTargetLineFromRectWithCollisionFlags(character, collisionFlags, checkRect, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineFromArray(TowerDefenseCharacter character, List<TowerDefenseCharacter> array, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter item in array)
		{
			if (_IsBasicTargetValid(item, fliterGraveStone) && character.CanTarget(item) && character.CanCollision(item.instance.maskFlags) && item.IsTargetableFromLine(character.gridPos.Y))
			{
				_resultBuffer.Add(item);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineList(TowerDefenseCharacter character, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter charactersForLine in _registry.GetCharactersForLineList(character.gridPos.Y))
		{
			if (_IsBasicTargetValidWithCheck(charactersForLine, fliterGraveStone) && character.CanTarget(charactersForLine) && character.CanCollision(charactersForLine.instance.maskFlags))
			{
				_resultBuffer.Add(charactersForLine);
			}
		}
		return _resultBuffer;
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLine(TowerDefenseCharacter character, bool fliterGraveStone = true)
	{
		return GetCharacterTargetLineList(character, fliterGraveStone);
	}

	public List<TowerDefenseCharacter> GetCharacterTargetLineWithCollisionFlags(TowerDefenseCharacter character, int collisionFlags, bool fliterGraveStone = true)
	{
		_resultBuffer.Clear();
		foreach (TowerDefenseCharacter charactersForLine in _registry.GetCharactersForLineList(character.gridPos.Y))
		{
			if (_IsBasicTargetValidWithCheck(charactersForLine, fliterGraveStone) && character.CanTarget(charactersForLine) && (collisionFlags & charactersForLine.instance.maskFlags) != 0)
			{
				_resultBuffer.Add(charactersForLine);
			}
		}
		return _resultBuffer;
	}
}
