using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleCharacterRegistry : Node
{
	private readonly struct LineSweepEntry
	{
		public readonly float MinX;

		public readonly float MaxX;

		public readonly TowerDefenseCharacter Character;

		public LineSweepEntry(TowerDefenseCharacter character, Rect2 rect)
		{
			Character = character;
			Vector2 vector = rect.Position + rect.Size;
			MinX = Mathf.Min(rect.Position.X, vector.X);
			MaxX = Mathf.Max(rect.Position.X, vector.X);
		}
	}

	private readonly struct SpatialCoverage(int minLine, int maxLine, int minColumn, int maxColumn)
	{
		public readonly int MinLine = minLine;

		public readonly int MaxLine = maxLine;

		public readonly int MinColumn = minColumn;

		public readonly int MaxColumn = maxColumn;

		public bool Contains(SpatialCoverage other)
		{
			if (other.MinLine >= MinLine && other.MaxLine <= MaxLine && other.MinColumn >= MinColumn)
			{
				return other.MaxColumn <= MaxColumn;
			}
			return false;
		}
	}

	private readonly System.Collections.Generic.Dictionary<(int Line, int MinColumn, int MaxColumn, bool IncludeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP ExcludedCamp), List<TowerDefenseCharacter>> _attackGridLineRangeQueryCache = new System.Collections.Generic.Dictionary<(int, int, int, bool, TowerDefenseEnum.CHARACTER_CAMP), List<TowerDefenseCharacter>>();

	private readonly HashSet<(int Line, int MinColumn, int MaxColumn, bool IncludeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP ExcludedCamp)> _attackGridLineRangeQueryBuiltKeys = new HashSet<(int, int, int, bool, TowerDefenseEnum.CHARACTER_CAMP)>();

	private readonly System.Collections.Generic.Dictionary<(int Line, bool RestrictToLine, TowerDefenseEnum.CHARACTER_CAMP ExcludedCamp, int CollectionMask), bool> _opposingMaskCandidateCache = new System.Collections.Generic.Dictionary<(int, bool, TowerDefenseEnum.CHARACTER_CAMP, int), bool>();

	private long _attackGridLineRangeQueryFrame = -1L;

	private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> _attackGridCells = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _attackGridAllLineCells = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> _attackGridNonPlantCells = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _attackGridNonPlantAllLineCells = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> _attackGridNonZombieCells = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _attackGridNonZombieAllLineCells = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly HashSet<TowerDefenseCharacter> _attackGridSeen = new HashSet<TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<TowerDefenseCharacter, Vector2I> _attackGridPositionByCharacter = new System.Collections.Generic.Dictionary<TowerDefenseCharacter, Vector2I>(ReferenceEqualityComparer.Instance);

	private long _attackGridFrame = -1L;

	private bool _attackGridHomogeneous;

	private TowerDefenseEnum.CHARACTER_CAMP _attackGridHomogeneousCamp;

	private readonly List<TowerDefenseCharacter> _cleanCharacters = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseItem> _cleanItems = new List<TowerDefenseItem>();

	private long _cleanCharactersFrame = -1L;

	private int _cleanZombieCount;

	private int _cleanVaseCount;

	private int _effectCount;

	private long _effectCountFrame = -1L;

	private static readonly List<TowerDefenseCharacter> EmptyCharacterList = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _activeCharacters = new List<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _registeredCharacters = new HashSet<TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<TowerDefenseEnum.CHARACTER_CAMP, int> _characterCountByCamp = new System.Collections.Generic.Dictionary<TowerDefenseEnum.CHARACTER_CAMP, int>();

	private readonly System.Collections.Generic.Dictionary<string, int> _characterCountByConfigName = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<(TowerDefenseEnum.CHARACTER_CAMP Camp, string ConfigName), int> _characterCountByCampAndConfigName = new System.Collections.Generic.Dictionary<(TowerDefenseEnum.CHARACTER_CAMP, string), int>();

	private readonly System.Collections.Generic.Dictionary<(TowerDefenseEnum.CHARACTER_CAMP Camp, string ConfigName), List<TowerDefenseCharacter>> _outsideCampSameConfigQueryCache = new System.Collections.Generic.Dictionary<(TowerDefenseEnum.CHARACTER_CAMP, string), List<TowerDefenseCharacter>>();

	private readonly System.Collections.Generic.Dictionary<(TowerDefenseEnum.CHARACTER_CAMP Camp, string ConfigName), List<TowerDefenseCharacter>> _sameCampDifferentConfigQueryCache = new System.Collections.Generic.Dictionary<(TowerDefenseEnum.CHARACTER_CAMP, string), List<TowerDefenseCharacter>>();

	private bool _characterClassificationCountsDirty = true;

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _lineCharacters = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private long _lineCharactersFrame = -1L;

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _columnCharacters = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private long _columnCharactersFrame = -1L;

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _lineCombinedCache = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _lineCombinedQueryCache = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly HashSet<int> _lineCombinedQueryBuiltLines = new HashSet<int>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _lineCombinedNonPlantCache = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _lineCombinedNonPlantQueryCache = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly HashSet<int> _lineCombinedNonPlantQueryBuiltLines = new HashSet<int>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _lineCombinedNonZombieCache = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> _lineCombinedNonZombieQueryCache = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>();

	private readonly HashSet<int> _lineCombinedNonZombieQueryBuiltLines = new HashSet<int>();

	private long _lineCombinedFrame = -1L;

	private readonly List<TowerDefenseCharacter> _allLineCheckChars = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _allLineCheckNonPlantChars = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _allLineCheckNonZombieChars = new List<TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> _lineSweepIntervals = new System.Collections.Generic.Dictionary<int, List<LineSweepEntry>>();

	private readonly System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> _lineSweepIntervalsByMaxX = new System.Collections.Generic.Dictionary<int, List<LineSweepEntry>>();

	private readonly List<LineSweepEntry> _lineSweepAllLineEntries = new List<LineSweepEntry>();

	private readonly List<LineSweepEntry> _lineSweepAllLineEntriesByMaxX = new List<LineSweepEntry>();

	private readonly System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> _lineSweepTallIntervals = new System.Collections.Generic.Dictionary<int, List<LineSweepEntry>>();

	private readonly System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> _lineSweepTallIntervalsByMaxX = new System.Collections.Generic.Dictionary<int, List<LineSweepEntry>>();

	private readonly List<LineSweepEntry> _lineSweepTallAllLineEntries = new List<LineSweepEntry>();

	private readonly List<LineSweepEntry> _lineSweepTallAllLineEntriesByMaxX = new List<LineSweepEntry>();

	private readonly List<LineSweepEntry> _lineSweepScanBuffer = new List<LineSweepEntry>();

	private readonly HashSet<int> _lineSweepBuiltLines = new HashSet<int>();

	private long _lineSweepFrame = -1L;

	private readonly List<TowerDefenseCharacter> _rowsQueryBuffer = new List<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _rowsQuerySeen = new HashSet<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _rectQueryBuffer = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _gridWindowQueryBuffer = new List<TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> _spatialBuckets = new System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>>();

	private readonly List<TowerDefenseCharacter> _spatialAllLineCheckChars = new List<TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> _spatialNonPlantBuckets = new System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>>();

	private readonly List<TowerDefenseCharacter> _spatialNonPlantAllLineCheckChars = new List<TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> _spatialNonZombieBuckets = new System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>>();

	private readonly List<TowerDefenseCharacter> _spatialNonZombieAllLineCheckChars = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _spatialCandidateBuffer = new List<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _spatialCandidateSeen = new HashSet<TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<TowerDefenseCharacter, SpatialCoverage> _spatialCoverageByCharacter = new System.Collections.Generic.Dictionary<TowerDefenseCharacter, SpatialCoverage>();

	private long _spatialBucketsFrame = -1L;

	public bool HasRegisteredCharacters => _registeredCharacters.Count > 0;

	public int ActiveCharacterCount => _activeCharacters.Count;

	public ulong QueryRevision { get; private set; }

	public ulong MembershipRevision { get; private set; }

	public ulong GeometryRevision { get; private set; }

	private void RemoveActiveCharacter(TowerDefenseCharacter character)
	{
		int num = _activeCharacters.IndexOf(character);
		if (num >= 0)
		{
			int index = _activeCharacters.Count - 1;
			_activeCharacters[num] = _activeCharacters[index];
			_activeCharacters.RemoveAt(index);
		}
	}

	private void CompactInvalidActiveCharacters()
	{
		bool flag = false;
		for (int num = _activeCharacters.Count - 1; num >= 0; num--)
		{
			TowerDefenseCharacter towerDefenseCharacter = _activeCharacters[num];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				_registeredCharacters.Remove(towerDefenseCharacter);
				int index = _activeCharacters.Count - 1;
				_activeCharacters[num] = _activeCharacters[index];
				_activeCharacters.RemoveAt(index);
				flag = true;
			}
		}
		if (flag)
		{
			AdvanceMembershipRevision();
			_characterClassificationCountsDirty = true;
			InvalidateCharacterCaches(invalidateCleanCharacters: true);
		}
	}

	public List<TowerDefenseCharacter> GetActiveCharacters()
	{
		return _activeCharacters;
	}

	private void FillAllLineAttackGridCellRange<TPredicate>(int minColumn, int maxColumn, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, List<TowerDefenseCharacter> destination) where TPredicate : struct, ITowerDefenseCharacterCandidatePredicate
	{
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		for (int i = minColumn; i <= maxColumn; i++)
		{
			if (attackGridAllLineCellsForExcludedCamp.TryGetValue(i, out var value))
			{
				FillAttackGridCellRangePredicateCharacters(value, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, destination);
			}
		}
	}

	private static bool HasAnyValidAttackGridCandidate(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells)
	{
		foreach (System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> value in cells.Values)
		{
			if (HasAnyValidAttackGridCandidate(value))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasAnyValidAttackGridCandidate(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> lists)
	{
		foreach (List<TowerDefenseCharacter> value in lists.Values)
		{
			if (HasValidAttackGridCandidate(value, -2147483648, skipSameLineAllLineEntry: false))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasAnyValidAttackGridCandidateMatchingMask(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, int collectionMask)
	{
		foreach (System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> value in cells.Values)
		{
			if (HasAnyValidAttackGridCandidateMatchingMask(value, collectionMask))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasAnyValidAttackGridCandidateMatchingMask(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> lists, int collectionMask)
	{
		foreach (List<TowerDefenseCharacter> value in lists.Values)
		{
			if (HasValidAttackGridCandidateMatchingMask(value, collectionMask))
			{
				return true;
			}
		}
		return false;
	}

	private void BuildAttackGridBaseIndex(List<TowerDefenseCharacter> clean, out bool foundCharacter, out bool homogeneous)
	{
		foundCharacter = false;
		homogeneous = true;
		for (int i = 0; i < clean.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = clean[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				if (!foundCharacter)
				{
					_attackGridHomogeneousCamp = towerDefenseCharacter.camp;
					foundCharacter = true;
				}
				else if (towerDefenseCharacter.camp != _attackGridHomogeneousCamp)
				{
					homogeneous = false;
				}
				_attackGridPositionByCharacter[towerDefenseCharacter] = towerDefenseCharacter.gridPos;
				AddAttackGridCoverage(towerDefenseCharacter, towerDefenseCharacter.gridPos.Y, _attackGridCells, _attackGridAllLineCells);
			}
		}
	}

	private void BuildAttackGridCampIndexes(List<TowerDefenseCharacter> clean)
	{
		for (int i = 0; i < clean.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = clean[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				int y = towerDefenseCharacter.gridPos.Y;
				if (towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
				{
					AddAttackGridCoverage(towerDefenseCharacter, y, _attackGridNonPlantCells, _attackGridNonPlantAllLineCells);
				}
				if (towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
				{
					AddAttackGridCoverage(towerDefenseCharacter, y, _attackGridNonZombieCells, _attackGridNonZombieAllLineCells);
				}
			}
		}
	}

	private System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> GetAttackGridCellsForExcludedCamp(TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		switch (excludedCamp)
		{
		case TowerDefenseEnum.CHARACTER_CAMP.PLANT:
			if (!_attackGridHomogeneous || _attackGridHomogeneousCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				return _attackGridNonPlantCells;
			}
			return _attackGridCells;
		case TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE:
			if (!_attackGridHomogeneous || _attackGridHomogeneousCamp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
			{
				return _attackGridNonZombieCells;
			}
			return _attackGridCells;
		default:
			return _attackGridCells;
		}
	}

	private System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> GetAttackGridAllLineCellsForExcludedCamp(TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		switch (excludedCamp)
		{
		case TowerDefenseEnum.CHARACTER_CAMP.PLANT:
			if (!_attackGridHomogeneous || _attackGridHomogeneousCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				return _attackGridNonPlantAllLineCells;
			}
			return _attackGridAllLineCells;
		case TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE:
			if (!_attackGridHomogeneous || _attackGridHomogeneousCamp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
			{
				return _attackGridNonZombieAllLineCells;
			}
			return _attackGridAllLineCells;
		default:
			return _attackGridAllLineCells;
		}
	}

	private static bool HasValidAttackGridCandidate(List<TowerDefenseCharacter> candidates, int line, bool skipSameLineAllLineEntry)
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && (!skipSameLineAllLineEntry || !towerDefenseCharacter.IsTargetableFromLine(line, includeAllLineCheck: false)))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasValidAttackGridCandidateMatchingMask(List<TowerDefenseCharacter> candidates, int collectionMask)
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && (collectionMask & towerDefenseCharacter.instance.maskFlags) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void AddAttackGridCell(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, int line, int column, TowerDefenseCharacter character, bool requireDuplicateCheck)
	{
		if (!cells.TryGetValue(line, out var value))
		{
			value = (cells[line] = new System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>());
		}
		if (!value.TryGetValue(column, out var value2))
		{
			value2 = (value[column] = new List<TowerDefenseCharacter>());
		}
		if (requireDuplicateCheck)
		{
			AddUniqueAttackGridCharacter(value2, character);
		}
		else
		{
			value2.Add(character);
		}
	}

	private static void AddAttackGridAllLineCell(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells, int column, TowerDefenseCharacter character, bool requireDuplicateCheck)
	{
		if (!allLineCells.TryGetValue(column, out var value))
		{
			value = (allLineCells[column] = new List<TowerDefenseCharacter>());
		}
		if (requireDuplicateCheck)
		{
			AddUniqueAttackGridCharacter(value, character);
		}
		else
		{
			value.Add(character);
		}
	}

	private void FillAttackGridCellRangePredicateCharacters<TPredicate>(List<TowerDefenseCharacter> candidates, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, List<TowerDefenseCharacter> destination) where TPredicate : struct, ITowerDefenseCharacterCandidatePredicate
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && _attackGridSeen.Add(towerDefenseCharacter))
			{
				candidateCount++;
				if (predicate.CanConsider(towerDefenseCharacter))
				{
					checkedCount++;
					hitCount++;
					destination.Add(towerDefenseCharacter);
				}
			}
		}
	}

	public void FillAttackGridCharactersInCellRangeForPhysicsFrame<TPredicate>(ulong physicsFrame, int minLine, int maxLine, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, List<TowerDefenseCharacter> destination) where TPredicate : struct, ITowerDefenseCharacterCandidatePredicate
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		destination?.Clear();
		NormalizeAttackGridTraversalRange(ref minLine, ref maxLine);
		NormalizeAttackGridColumnRange(ref minColumn, ref maxColumn);
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		if (destination == null)
		{
			EndAttackGridCellRangeQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.cellRangeQuery.fill");
			return;
		}
		if (minLine == maxLine)
		{
			FillSingleLineAttackGridCellRange(physicsFrame, minLine, minColumn, maxColumn, includeAllLineCheck, excludedCamp, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, destination);
			EndAttackGridCellRangeQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.cellRangeQuery.fill");
			return;
		}
		EnsureAttackGridIndex(physicsFrame);
		_attackGridSeen.Clear();
		FillMultiLineAttackGridCellRange(minLine, maxLine, minColumn, maxColumn, excludedCamp, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, destination);
		if (includeAllLineCheck)
		{
			FillAllLineAttackGridCellRange(minColumn, maxColumn, excludedCamp, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, destination);
		}
		EndAttackGridCellRangeQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.cellRangeQuery.fill");
	}

	private static void RemoveAttackGridCell(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, int line, int column, TowerDefenseCharacter character)
	{
		if (cells.TryGetValue(line, out var value) && value.TryGetValue(column, out var value2))
		{
			value2.Remove(character);
		}
	}

	private static void RemoveAttackGridAllLineCell(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells, int column, TowerDefenseCharacter character)
	{
		if (allLineCells.TryGetValue(column, out var value))
		{
			value.Remove(character);
		}
	}

	public bool TrySelectAttackGridCharacterInCellRangeForPhysicsFrame<TSelector>(ulong physicsFrame, int startLine, int endLine, int stepLine, int startColumn, int endColumn, int stepColumn, bool visitRowsFirst, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TSelector selector, out TowerDefenseCharacter match, out int visitedCells) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		EnsureAttackGridIndex(physicsFrame);
		match = null;
		visitedCells = 0;
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		if (IsAttackGridTraversalRangeInvalid(startLine, endLine, stepLine, startColumn, endColumn, stepColumn))
		{
			EndAttackGridCellRangeQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.cellRangeQuery.select");
			return false;
		}
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		if (visitRowsFirst)
		{
			TrySelectAttackGridLineCellsRowsFirst(attackGridCellsForExcludedCamp, attackGridAllLineCellsForExcludedCamp, startLine, endLine, stepLine, startColumn, endColumn, stepColumn, includeAllLineCheck, ref selector, ref visitedCells, ref candidateCount, ref checkedCount, ref hitCount);
		}
		else
		{
			TrySelectAttackGridLineCellsColumnsFirst(attackGridCellsForExcludedCamp, attackGridAllLineCellsForExcludedCamp, startLine, endLine, stepLine, startColumn, endColumn, stepColumn, includeAllLineCheck, ref selector, ref visitedCells, ref candidateCount, ref checkedCount, ref hitCount);
		}
		match = selector.SelectedMatch;
		EndAttackGridCellRangeQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.cellRangeQuery.select");
		return GodotObject.IsInstanceValid(match);
	}

	private static void NormalizeAttackGridColumnRange(ref int minColumn, ref int maxColumn)
	{
		if (minColumn > maxColumn)
		{
			int num = minColumn;
			minColumn = maxColumn;
			maxColumn = num;
		}
	}

	private HashSet<TowerDefenseCharacter> PrepareAttackGridSeen(int minColumn, int maxColumn)
	{
		if (minColumn == maxColumn)
		{
			return null;
		}
		_attackGridSeen.Clear();
		return _attackGridSeen;
	}

	private static bool CharacterCoversAttackGridColumnRange(TowerDefenseCharacter character, int minColumn, int maxColumn)
	{
		int x = character.gridPos.X;
		if (ColumnInRange(x, minColumn, maxColumn))
		{
			return true;
		}
		if (character.config is TowerDefensePlantConfig towerDefensePlantConfig)
		{
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				if (ColumnInRange(x + item.X, minColumn, maxColumn))
				{
					return true;
				}
			}
		}
		int num = character.targetRegistrationComponent?.attackGridColumnAliasOffset ?? 0;
		if (num != 0 && ColumnInRange(x + num, minColumn, maxColumn))
		{
			return true;
		}
		return false;
	}

	private static bool ColumnInRange(int column, int minColumn, int maxColumn)
	{
		if (column >= minColumn)
		{
			return column <= maxColumn;
		}
		return false;
	}

	public bool TrySelectLineCharacterInColumnRangeForPhysicsFrame<TSelector>(ulong physicsFrame, int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TSelector selector, out TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		match = null;
		NormalizeAttackGridColumnRange(ref minColumn, ref maxColumn);
		List<TowerDefenseCharacter> attackGridLineRangeCharactersForPhysicsFrame = GetAttackGridLineRangeCharactersForPhysicsFrame(physicsFrame, line, minColumn, maxColumn, includeAllLineCheck, excludedCamp);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < attackGridLineRangeCharactersForPhysicsFrame.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = attackGridLineRangeCharactersForPhysicsFrame[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				continue;
			}
			num++;
			if (selector.CanConsider(towerDefenseCharacter))
			{
				num2++;
				TowerDefenseCharacter match2 = selector.SelectedMatch;
				if (selector.Visit(towerDefenseCharacter, ref match2))
				{
					num3++;
				}
				selector.CompleteCell();
				if (selector.ShouldStopAfterCell)
				{
					break;
				}
			}
		}
		match = selector.SelectedMatch;
		EndAttackGridCellRangeQuery(perfStart, num, num2, num3, "registry.cellRangeQuery.lineSelect");
		return GodotObject.IsInstanceValid(match);
	}

	private static bool TryResolveAttackGridLineAlias(TowerDefenseCharacter character, int baseLine, out int aliasLine)
	{
		int attackGridLineAliasOffset = character.targetRegistrationComponent.attackGridLineAliasOffset;
		aliasLine = baseLine + attackGridLineAliasOffset;
		if (attackGridLineAliasOffset == 0 || aliasLine == baseLine || aliasLine < 1)
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance) && instance.gridNum.Y > 0)
		{
			return aliasLine <= instance.gridNum.Y;
		}
		return true;
	}

	private void AddAttackGridCoverage(TowerDefenseCharacter character, int line, int column, System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells)
	{
		AddAttackGridCoverageForLine(character, line, column, cells, allLineCells, includeAllLineCoverage: true);
		if (TryResolveAttackGridLineAlias(character, line, out var aliasLine))
		{
			AddAttackGridCoverageForLine(character, aliasLine, column, cells, allLineCells, includeAllLineCoverage: false);
		}
	}

	private void AddAttackGridCoverageForLine(TowerDefenseCharacter character, int line, int column, System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells, bool includeAllLineCoverage)
	{
		TowerDefensePlantConfig towerDefensePlantConfig = character.config as TowerDefensePlantConfig;
		bool requireDuplicateCheck = towerDefensePlantConfig != null;
		AddAttackGridCell(cells, line, column, character, requireDuplicateCheck);
		if (includeAllLineCoverage && character.targetRegistrationComponent.allLineCheck)
		{
			AddAttackGridAllLineCell(allLineCells, column, character, requireDuplicateCheck);
		}
		if (towerDefensePlantConfig != null)
		{
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				int column2 = column + item.X;
				AddAttackGridCell(cells, line, column2, character, requireDuplicateCheck: true);
				if (includeAllLineCoverage && character.targetRegistrationComponent.allLineCheck)
				{
					AddAttackGridAllLineCell(allLineCells, column2, character, requireDuplicateCheck: true);
				}
			}
		}
		int attackGridColumnAliasOffset = character.targetRegistrationComponent.attackGridColumnAliasOffset;
		if (attackGridColumnAliasOffset != 0)
		{
			int column3 = column + attackGridColumnAliasOffset;
			AddAttackGridCell(cells, line, column3, character, requireDuplicateCheck: true);
			if (includeAllLineCoverage && character.targetRegistrationComponent.allLineCheck)
			{
				AddAttackGridAllLineCell(allLineCells, column3, character, requireDuplicateCheck: true);
			}
		}
	}

	private void AddAttackGridCoverage(TowerDefenseCharacter character, int line, System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells)
	{
		AddAttackGridCoverage(character, line, character.gridPos.X, cells, allLineCells);
	}

	private static void ClearAttackGridCells(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells)
	{
		foreach (System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> value in cells.Values)
		{
			foreach (List<TowerDefenseCharacter> value2 in value.Values)
			{
				value2.Clear();
			}
		}
	}

	private static void ClearAttackGridLists(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> lists)
	{
		foreach (List<TowerDefenseCharacter> value in lists.Values)
		{
			value.Clear();
		}
	}

	private void RemoveAttackGridCoverage(TowerDefenseCharacter character, int line, int column, System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells)
	{
		RemoveAttackGridCoverageForLine(character, line, column, cells, allLineCells, includeAllLineCoverage: true);
		if (TryResolveAttackGridLineAlias(character, line, out var aliasLine))
		{
			RemoveAttackGridCoverageForLine(character, aliasLine, column, cells, allLineCells, includeAllLineCoverage: false);
		}
	}

	private void RemoveAttackGridCoverageForLine(TowerDefenseCharacter character, int line, int column, System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> cells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells, bool includeAllLineCoverage)
	{
		TowerDefensePlantConfig towerDefensePlantConfig = character.config as TowerDefensePlantConfig;
		RemoveAttackGridCell(cells, line, column, character);
		if (includeAllLineCoverage && character.targetRegistrationComponent.allLineCheck)
		{
			RemoveAttackGridAllLineCell(allLineCells, column, character);
		}
		if (towerDefensePlantConfig != null)
		{
			foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
			{
				int column2 = column + item.X;
				RemoveAttackGridCell(cells, line, column2, character);
				if (includeAllLineCoverage && character.targetRegistrationComponent.allLineCheck)
				{
					RemoveAttackGridAllLineCell(allLineCells, column2, character);
				}
			}
		}
		int attackGridColumnAliasOffset = character.targetRegistrationComponent.attackGridColumnAliasOffset;
		if (attackGridColumnAliasOffset != 0)
		{
			int column3 = column + attackGridColumnAliasOffset;
			RemoveAttackGridCell(cells, line, column3, character);
			if (includeAllLineCoverage && character.targetRegistrationComponent.allLineCheck)
			{
				RemoveAttackGridAllLineCell(allLineCells, column3, character);
			}
		}
	}

	private void RebuildAttackGridExcludedCampCells()
	{
		ClearAttackGridCells(_attackGridNonPlantCells);
		ClearAttackGridLists(_attackGridNonPlantAllLineCells);
		ClearAttackGridCells(_attackGridNonZombieCells);
		ClearAttackGridLists(_attackGridNonZombieAllLineCells);
		foreach (KeyValuePair<TowerDefenseCharacter, Vector2I> item in _attackGridPositionByCharacter)
		{
			TowerDefenseCharacter key = item.Key;
			if (GodotObject.IsInstanceValid(key))
			{
				AddAttackGridExcludedCampCoverage(key, item.Value);
			}
		}
	}

	private void AddAttackGridExcludedCampCoverage(TowerDefenseCharacter character, Vector2I position)
	{
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			AddAttackGridCoverage(character, position.Y, position.X, _attackGridNonPlantCells, _attackGridNonPlantAllLineCells);
		}
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			AddAttackGridCoverage(character, position.Y, position.X, _attackGridNonZombieCells, _attackGridNonZombieAllLineCells);
		}
	}

	private void RemoveAttackGridExcludedCampCoverage(TowerDefenseCharacter character, Vector2I position)
	{
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			RemoveAttackGridCoverage(character, position.Y, position.X, _attackGridNonPlantCells, _attackGridNonPlantAllLineCells);
		}
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			RemoveAttackGridCoverage(character, position.Y, position.X, _attackGridNonZombieCells, _attackGridNonZombieAllLineCells);
		}
	}

	private bool TryAddAttackGridCharacter(TowerDefenseCharacter character)
	{
		if (_attackGridFrame < 0 || _attackGridPositionByCharacter.ContainsKey(character))
		{
			return _attackGridFrame >= 0;
		}
		bool num = _attackGridPositionByCharacter.Count > 0;
		Vector2I gridPos = character.gridPos;
		_attackGridPositionByCharacter[character] = gridPos;
		AddAttackGridCoverage(character, gridPos.Y, gridPos.X, _attackGridCells, _attackGridAllLineCells);
		if (!num)
		{
			_attackGridHomogeneous = true;
			_attackGridHomogeneousCamp = character.camp;
		}
		else if (_attackGridHomogeneous && character.camp != _attackGridHomogeneousCamp)
		{
			_attackGridHomogeneous = false;
			RebuildAttackGridExcludedCampCells();
		}
		else if (!_attackGridHomogeneous)
		{
			AddAttackGridExcludedCampCoverage(character, gridPos);
		}
		_opposingMaskCandidateCache.Clear();
		return true;
	}

	private bool TryRemoveAttackGridCharacter(TowerDefenseCharacter character)
	{
		if (_attackGridFrame < 0 || !_attackGridPositionByCharacter.Remove(character, out var value))
		{
			return false;
		}
		RemoveAttackGridCoverage(character, value.Y, value.X, _attackGridCells, _attackGridAllLineCells);
		if (!_attackGridHomogeneous)
		{
			RemoveAttackGridExcludedCampCoverage(character, value);
		}
		if (_attackGridPositionByCharacter.Count == 0)
		{
			_attackGridHomogeneous = false;
		}
		_opposingMaskCandidateCache.Clear();
		return true;
	}

	private void EnsureAttackGridIndex(ulong currentFrame)
	{
		if (_attackGridFrame < 0)
		{
			long startTicks = TowerDefensePerfProfiler.Begin();
			TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
			_attackGridFrame = (long)currentFrame;
			ClearAttackGridIndexCaches();
			List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
			BuildAttackGridBaseIndex(cleanCharactersList, out var foundCharacter, out var homogeneous);
			_attackGridHomogeneous = foundCharacter && homogeneous;
			if (!_attackGridHomogeneous)
			{
				BuildAttackGridCampIndexes(cleanCharactersList);
			}
			TowerDefensePerfProfiler.End("registry.attackGridBuild", startTicks, cleanCharactersList.Count);
			TowerDefensePerfProfiler.EndSpikeProbe("registry.attackGridBuild", in probe, cleanCharactersList.Count);
		}
	}

	private void ClearAttackGridIndexCaches()
	{
		ClearAttackGridCells(_attackGridCells);
		ClearAttackGridLists(_attackGridAllLineCells);
		ClearAttackGridCells(_attackGridNonPlantCells);
		ClearAttackGridLists(_attackGridNonPlantAllLineCells);
		ClearAttackGridCells(_attackGridNonZombieCells);
		ClearAttackGridLists(_attackGridNonZombieAllLineCells);
		_attackGridPositionByCharacter.Clear();
		_opposingMaskCandidateCache.Clear();
		_attackGridHomogeneous = false;
	}

	private bool TrySelectAttackGridLineCellsColumnsFirst<TSelector>(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, int startLine, int endLine, int stepLine, int startColumn, int endColumn, int stepColumn, bool includeAllLineCheck, ref TSelector selector, ref int visitedCells, ref int candidateCount, ref int checkedCount, ref int hitCount) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		for (int i = startColumn; IsAttackGridTraversalValueInRange(i, endColumn, stepColumn); i += stepColumn)
		{
			for (int j = startLine; IsAttackGridTraversalValueInRange(j, endLine, stepLine); j += stepLine)
			{
				visitedCells++;
				attackGridCells.TryGetValue(j, out var value);
				if (ScanAttackGridLineTraversalCell(value, attackGridAllLineCells, j, i, includeAllLineCheck && j == startLine, ref selector, ref candidateCount, ref checkedCount, ref hitCount))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool TrySelectAttackGridLineCellsRowsFirst<TSelector>(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, int startLine, int endLine, int stepLine, int startColumn, int endColumn, int stepColumn, bool includeAllLineCheck, ref TSelector selector, ref int visitedCells, ref int candidateCount, ref int checkedCount, ref int hitCount) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		for (int i = startLine; IsAttackGridTraversalValueInRange(i, endLine, stepLine); i += stepLine)
		{
			attackGridCells.TryGetValue(i, out var value);
			for (int j = startColumn; IsAttackGridTraversalValueInRange(j, endColumn, stepColumn); j += stepColumn)
			{
				visitedCells++;
				if (ScanAttackGridLineTraversalCell(value, attackGridAllLineCells, i, j, includeAllLineCheck && i == startLine, ref selector, ref candidateCount, ref checkedCount, ref hitCount))
				{
					return true;
				}
			}
		}
		return false;
	}

	private List<TowerDefenseCharacter> GetAttackGridLineRangeCharactersForPhysicsFrame(ulong physicsFrame, int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		NormalizeAttackGridColumnRange(ref minColumn, ref maxColumn);
		if (_attackGridLineRangeQueryFrame != (long)physicsFrame)
		{
			_attackGridLineRangeQueryFrame = (long)physicsFrame;
			_attackGridLineRangeQueryBuiltKeys.Clear();
		}
		(int, int, int, bool, TowerDefenseEnum.CHARACTER_CAMP) tuple = (line, minColumn, maxColumn, includeAllLineCheck, excludedCamp);
		if (_attackGridLineRangeQueryBuiltKeys.Contains(tuple) && _attackGridLineRangeQueryCache.TryGetValue(tuple, out var value))
		{
			return value;
		}
		if (!_attackGridLineRangeQueryCache.TryGetValue(tuple, out var value2))
		{
			value2 = new List<TowerDefenseCharacter>();
			_attackGridLineRangeQueryCache[tuple] = value2;
		}
		value2.Clear();
		List<TowerDefenseCharacter> list = (includeAllLineCheck ? GetCharactersForLineListExcludingCamp(line, excludedCamp) : GetLineCharactersListExcludingCamp(line, excludedCamp));
		for (int i = 0; i < list.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = list[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && CharacterCoversAttackGridColumnRange(towerDefenseCharacter, minColumn, maxColumn))
			{
				value2.Add(towerDefenseCharacter);
			}
		}
		_attackGridLineRangeQueryBuiltKeys.Add(tuple);
		TowerDefensePerfProfiler.Sample("registry.cellRangeQuery.lineCacheBuild", list.Count);
		return value2;
	}

	private static void FillLineCharactersInAttackGridColumnRange<TPredicate>(List<TowerDefenseCharacter> characters, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, List<TowerDefenseCharacter> destination) where TPredicate : struct, ITowerDefenseCharacterCandidatePredicate
	{
		for (int i = 0; i < characters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = characters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				candidateCount++;
				if (predicate.CanConsider(towerDefenseCharacter))
				{
					checkedCount++;
					hitCount++;
					destination.Add(towerDefenseCharacter);
				}
			}
		}
	}

	public bool TryFindLineCharacterInColumnRange<TPredicate>(int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterCandidatePredicate
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		match = null;
		NormalizeAttackGridColumnRange(ref minColumn, ref maxColumn);
		List<TowerDefenseCharacter> attackGridLineRangeCharactersForPhysicsFrame = GetAttackGridLineRangeCharactersForPhysicsFrame(Engine.GetPhysicsFrames(), line, minColumn, maxColumn, includeAllLineCheck, excludedCamp);
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < attackGridLineRangeCharactersForPhysicsFrame.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = attackGridLineRangeCharactersForPhysicsFrame[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				num++;
				if (predicate.CanConsider(towerDefenseCharacter))
				{
					num2++;
					match = towerDefenseCharacter;
					break;
				}
			}
		}
		EndAttackGridCellRangeQuery(perfStart, num, num2, GodotObject.IsInstanceValid(match) ? 1 : 0, "registry.cellRangeQuery.lineFind");
		return GodotObject.IsInstanceValid(match);
	}

	private bool ScanAttackGridLineSelectorCharacters<TSelector>(List<TowerDefenseCharacter> candidates, int line, bool skipSameLineAllLineEntry, ref TSelector selector, ref int candidateCount, ref int checkedCount, ref int hitCount, ref TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || (skipSameLineAllLineEntry && towerDefenseCharacter.IsTargetableFromLine(line, includeAllLineCheck: false)))
			{
				continue;
			}
			candidateCount++;
			if (selector.CanConsider(towerDefenseCharacter))
			{
				checkedCount++;
				hitCount++;
				if (selector.Visit(towerDefenseCharacter, ref match))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool TrySelectAttackGridCharacterInLineRangeForPhysicsFrame<TSelector>(ulong physicsFrame, int line, int startColumn, int endColumn, int stepColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TSelector selector, out TowerDefenseCharacter match, out int visitedCells) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		EnsureAttackGridIndex(physicsFrame);
		match = null;
		visitedCells = 0;
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		if (stepColumn == 0 || (stepColumn > 0 && startColumn > endColumn) || (stepColumn < 0 && startColumn < endColumn))
		{
			EndAttackGridLineQuery(perfStart, candidateCount, checkedCount, hitCount);
			return false;
		}
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		attackGridCellsForExcludedCamp.TryGetValue(line, out var value);
		for (int i = startColumn; IsAttackGridTraversalValueInRange(i, endColumn, stepColumn); i += stepColumn)
		{
			visitedCells++;
			if (ScanAttackGridLineTraversalCell(value, attackGridAllLineCellsForExcludedCamp, line, i, includeAllLineCheck, ref selector, ref candidateCount, ref checkedCount, ref hitCount))
			{
				break;
			}
		}
		match = selector.SelectedMatch;
		EndAttackGridLineQuery(perfStart, candidateCount, checkedCount, hitCount);
		return GodotObject.IsInstanceValid(match);
	}

	private bool ScanAttackGridLineTraversalCell<TSelector>(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> columns, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells, int line, int column, bool includeAllLineCheck, ref TSelector selector, ref int candidateCount, ref int checkedCount, ref int hitCount) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		TowerDefenseCharacter match = null;
		List<TowerDefenseCharacter> value;
		bool flag = columns != null && columns.TryGetValue(column, out value) && ScanAttackGridLineSelectorCharacters(value, line, skipSameLineAllLineEntry: false, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match);
		if (!flag && includeAllLineCheck && allLineCells.TryGetValue(column, out var value2))
		{
			flag = ScanAttackGridLineSelectorCharacters(value2, line, skipSameLineAllLineEntry: true, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match);
		}
		selector.CompleteCell();
		if (!flag)
		{
			return selector.ShouldStopAfterCell;
		}
		return true;
	}

	private void FillMultiLineAttackGridCellRange<TPredicate>(int minLine, int maxLine, int minColumn, int maxColumn, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, List<TowerDefenseCharacter> destination) where TPredicate : struct, ITowerDefenseCharacterCandidatePredicate
	{
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		for (int i = minLine; i <= maxLine; i++)
		{
			if (!attackGridCellsForExcludedCamp.TryGetValue(i, out var value))
			{
				continue;
			}
			for (int j = minColumn; j <= maxColumn; j++)
			{
				if (value.TryGetValue(j, out var value2))
				{
					FillAttackGridCellRangePredicateCharacters(value2, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, destination);
				}
			}
		}
	}

	public bool HasOpposingAttackCandidatesMatchingMask(int line, bool restrictToLine, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, int collectionMask)
	{
		if (collectionMask == 0)
		{
			return false;
		}
		EnsureAttackGridIndex(TowerDefenseProcessModeDispatch.GetPhysicsFrameForCurrentDispatch());
		(int, bool, TowerDefenseEnum.CHARACTER_CAMP, int) key = (line, restrictToLine, excludedCamp, collectionMask);
		if (_opposingMaskCandidateCache.TryGetValue(key, out var value))
		{
			TowerDefensePerfProfiler.Sample("registry.opposingMaskCache.hit");
			return value;
		}
		bool flag = HasOpposingAttackCandidatesMatchingMaskUncached(line, restrictToLine, excludedCamp, collectionMask);
		_opposingMaskCandidateCache[key] = flag;
		TowerDefensePerfProfiler.Sample("registry.opposingMaskCache.miss");
		return flag;
	}

	private bool HasOpposingAttackCandidatesMatchingMaskUncached(int line, bool restrictToLine, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, int collectionMask)
	{
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		if (!restrictToLine)
		{
			if (!HasAnyValidAttackGridCandidateMatchingMask(attackGridCellsForExcludedCamp, collectionMask))
			{
				return HasAnyValidAttackGridCandidateMatchingMask(attackGridAllLineCellsForExcludedCamp, collectionMask);
			}
			return true;
		}
		if (!attackGridCellsForExcludedCamp.TryGetValue(line, out var value) || !HasAnyValidAttackGridCandidateMatchingMask(value, collectionMask))
		{
			return HasAnyValidAttackGridCandidateMatchingMask(attackGridAllLineCellsForExcludedCamp, collectionMask);
		}
		return true;
	}

	public bool HasOpposingAttackCandidates(int line, bool restrictToLine, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		EnsureAttackGridIndex(Engine.GetPhysicsFrames());
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		if (!restrictToLine)
		{
			if (!HasAnyValidAttackGridCandidate(attackGridCellsForExcludedCamp))
			{
				return HasAnyValidAttackGridCandidate(attackGridAllLineCellsForExcludedCamp);
			}
			return true;
		}
		if (attackGridCellsForExcludedCamp.TryGetValue(line, out var value) && HasAnyValidAttackGridCandidate(value))
		{
			return true;
		}
		return HasAnyValidAttackGridCandidate(attackGridAllLineCellsForExcludedCamp);
	}

	private bool TryUpdateAttackGridPosition(TowerDefenseCharacter character)
	{
		if (_attackGridFrame < 0 || !_attackGridPositionByCharacter.TryGetValue(character, out var value))
		{
			return false;
		}
		Vector2I gridPos = character.gridPos;
		if (gridPos == value)
		{
			return true;
		}
		RemoveAttackGridCoverage(character, value.Y, value.X, _attackGridCells, _attackGridAllLineCells);
		AddAttackGridCoverage(character, gridPos.Y, gridPos.X, _attackGridCells, _attackGridAllLineCells);
		if (!_attackGridHomogeneous)
		{
			if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				RemoveAttackGridCoverage(character, value.Y, value.X, _attackGridNonPlantCells, _attackGridNonPlantAllLineCells);
				AddAttackGridCoverage(character, gridPos.Y, gridPos.X, _attackGridNonPlantCells, _attackGridNonPlantAllLineCells);
			}
			if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
			{
				RemoveAttackGridCoverage(character, value.Y, value.X, _attackGridNonZombieCells, _attackGridNonZombieAllLineCells);
				AddAttackGridCoverage(character, gridPos.Y, gridPos.X, _attackGridNonZombieCells, _attackGridNonZombieAllLineCells);
			}
		}
		_attackGridPositionByCharacter[character] = gridPos;
		_opposingMaskCandidateCache.Clear();
		return true;
	}

	private bool TryFindAttackGridPredicateAllLine<TPredicate>(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, Rect2 checkRect, int line, int minColumn, int maxColumn, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, out TowerDefenseCharacter match, HashSet<TowerDefenseCharacter> seen) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		for (int i = minColumn; i <= maxColumn; i++)
		{
			if (attackGridAllLineCells.TryGetValue(i, out var value) && ScanAttackGridPredicateCharacters(value, line, skipSameLineAllLineEntry: true, checkRect, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, out match, seen))
			{
				return true;
			}
		}
		return false;
	}

	private bool TryFindAttackGridPredicateLine<TPredicate>(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, Rect2 checkRect, int line, int minColumn, int maxColumn, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, out TowerDefenseCharacter match, HashSet<TowerDefenseCharacter> seen) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		if (!attackGridCells.TryGetValue(line, out var value))
		{
			return false;
		}
		for (int i = minColumn; i <= maxColumn; i++)
		{
			if (value.TryGetValue(i, out var value2) && ScanAttackGridPredicateCharacters(value2, line, skipSameLineAllLineEntry: false, checkRect, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, out match, seen))
			{
				return true;
			}
		}
		return false;
	}

	public bool TryFindAttackGridCharacterIntersectingRect<TPredicate>(Rect2 checkRect, int line, int column, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		return TryFindAttackGridCharacterIntersectingRect(checkRect, line, column, column, includeAllLineCheck, excludedCamp, ref predicate, out match);
	}

	public bool TryFindAttackGridCharacterIntersectingRect<TPredicate>(Rect2 checkRect, int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		EnsureAttackGridIndex(Engine.GetPhysicsFrames());
		return TryFindAttackGridPredicateQuery(checkRect, line, minColumn, maxColumn, includeAllLineCheck, excludedCamp, ref predicate, perfStart, out match);
	}

	private bool TryFindAttackGridPredicateQuery<TPredicate>(Rect2 checkRect, int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, long perfStart, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		NormalizeAttackGridColumnRange(ref minColumn, ref maxColumn);
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		HashSet<TowerDefenseCharacter> seen = PrepareAttackGridSeen(minColumn, maxColumn);
		bool flag = TryFindAttackGridPredicateLine(attackGridCellsForExcludedCamp, checkRect, line, minColumn, maxColumn, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, out match, seen);
		if (!flag && includeAllLineCheck)
		{
			flag = TryFindAttackGridPredicateAllLine(attackGridAllLineCellsForExcludedCamp, checkRect, line, minColumn, maxColumn, ref predicate, ref candidateCount, ref checkedCount, ref hitCount, out match, seen);
		}
		EndAttackGridQuery(perfStart, candidateCount, checkedCount, hitCount);
		return flag;
	}

	private bool ScanAttackGridPredicateCharacters<TPredicate>(List<TowerDefenseCharacter> candidates, int line, bool skipSameLineAllLineEntry, Rect2 checkRect, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, out TowerDefenseCharacter match, HashSet<TowerDefenseCharacter> seen) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (!TryAcceptAttackGridScanCandidate(towerDefenseCharacter, line, skipSameLineAllLineEntry, seen))
			{
				continue;
			}
			candidateCount++;
			if (!predicate.CanConsider(towerDefenseCharacter))
			{
				continue;
			}
			checkedCount++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				hitCount++;
				if (predicate.Matches(towerDefenseCharacter))
				{
					match = towerDefenseCharacter;
					return true;
				}
			}
		}
		return false;
	}

	public bool HasAttackGridCharacters(int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		EnsureAttackGridIndex(TowerDefenseProcessModeDispatch.GetPhysicsFrameForCurrentDispatch());
		NormalizeAttackGridColumnRange(ref minColumn, ref maxColumn);
		if (HasAttackGridLineCharacters(GetAttackGridCellsForExcludedCamp(excludedCamp), line, minColumn, maxColumn))
		{
			return true;
		}
		if (includeAllLineCheck)
		{
			return HasAttackGridAllLineCharacters(GetAttackGridAllLineCellsForExcludedCamp(excludedCamp), line, minColumn, maxColumn);
		}
		return false;
	}

	private static bool HasAttackGridLineCharacters(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, int line, int minColumn, int maxColumn)
	{
		if (!attackGridCells.TryGetValue(line, out var value))
		{
			return false;
		}
		return HasAttackGridColumnRangeCharacters(value, line, minColumn, maxColumn, skipSameLineAllLineEntry: false);
	}

	private static bool HasAttackGridAllLineCharacters(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, int line, int minColumn, int maxColumn)
	{
		return HasAttackGridColumnRangeCharacters(attackGridAllLineCells, line, minColumn, maxColumn, skipSameLineAllLineEntry: true);
	}

	private static bool HasAttackGridColumnRangeCharacters(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> columns, int line, int minColumn, int maxColumn, bool skipSameLineAllLineEntry)
	{
		for (int i = minColumn; i <= maxColumn; i++)
		{
			if (columns.TryGetValue(i, out var value) && HasValidAttackGridCandidate(value, line, skipSameLineAllLineEntry))
			{
				return true;
			}
		}
		return false;
	}

	private static void EndAttackGridQuery(long perfStart, int candidateCount, int checkedCount, int hitCount, string operationMetric = "registry.rectQuery.find")
	{
		TowerDefensePerfProfiler.Sample("registry.attackGridCandidates", candidateCount);
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", candidateCount);
		TowerDefensePerfProfiler.Sample("registry.rectHits", hitCount);
		TowerDefensePerfProfiler.End("registry.rectQuery.attackGrid", perfStart, checkedCount);
		TowerDefensePerfProfiler.End(operationMetric, perfStart, checkedCount);
		TowerDefensePerfProfiler.End("registry.rectQuery", perfStart, checkedCount);
	}

	private static void EndAttackGridLineQuery(long perfStart, int candidateCount, int checkedCount, int hitCount)
	{
		TowerDefensePerfProfiler.Sample("registry.attackGridLineCandidates", candidateCount);
		TowerDefensePerfProfiler.Sample("registry.lineHits", hitCount);
		TowerDefensePerfProfiler.End("registry.lineQuery.attackGrid", perfStart, checkedCount);
		TowerDefensePerfProfiler.End("registry.lineQuery.select", perfStart, checkedCount);
	}

	private static void EndAttackGridCellRangeQuery(long perfStart, int candidateCount, int checkedCount, int hitCount, string operationMetric)
	{
		TowerDefensePerfProfiler.Sample("registry.attackGridCellRangeCandidates", candidateCount);
		TowerDefensePerfProfiler.Sample("registry.cellRangeHits", hitCount);
		TowerDefensePerfProfiler.End("registry.cellRangeQuery.attackGrid", perfStart, checkedCount);
		TowerDefensePerfProfiler.End(operationMetric, perfStart, checkedCount);
	}

	private bool TrySelectAttackGridRectCellsColumnsFirst<TSelector>(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, Rect2 checkRect, int startLine, int endLine, int stepLine, int startColumn, int endColumn, int stepColumn, bool includeAllLineCheck, ref TSelector selector, ref int visitedCells, ref int candidateCount, ref int checkedCount, ref int hitCount) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		for (int i = startColumn; IsAttackGridTraversalValueInRange(i, endColumn, stepColumn); i += stepColumn)
		{
			for (int j = startLine; IsAttackGridTraversalValueInRange(j, endLine, stepLine); j += stepLine)
			{
				visitedCells++;
				attackGridCells.TryGetValue(j, out var value);
				if (ScanAttackGridTraversalCell(value, attackGridAllLineCells, j, i, includeAllLineCheck, checkRect, ref selector, ref candidateCount, ref checkedCount, ref hitCount))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool TrySelectAttackGridRectCellsRowsFirst<TSelector>(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, Rect2 checkRect, int startLine, int endLine, int stepLine, int startColumn, int endColumn, int stepColumn, bool includeAllLineCheck, ref TSelector selector, ref int visitedCells, ref int candidateCount, ref int checkedCount, ref int hitCount) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		for (int i = startLine; IsAttackGridTraversalValueInRange(i, endLine, stepLine); i += stepLine)
		{
			attackGridCells.TryGetValue(i, out var value);
			for (int j = startColumn; IsAttackGridTraversalValueInRange(j, endColumn, stepColumn); j += stepColumn)
			{
				visitedCells++;
				if (ScanAttackGridTraversalCell(value, attackGridAllLineCells, i, j, includeAllLineCheck, checkRect, ref selector, ref candidateCount, ref checkedCount, ref hitCount))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool TrySelectCharacterIntersectingRect<TSelector>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TSelector selector, out TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		match = null;
		if (line != -2147483648 && TrySelectLineSweepCharacter(checkRect, line, includeAllLineCheck, ref selector, out match, out var found))
		{
			return found;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		string sourceMetric;
		List<TowerDefenseCharacter> rectQuerySource = GetRectQuerySource(checkRect, line, includeAllLineCheck, out sourceMetric);
		int num = 0;
		int num2 = 0;
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", rectQuerySource.Count);
		for (int i = 0; i < rectQuerySource.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = rectQuerySource[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !selector.CanConsider(towerDefenseCharacter))
			{
				continue;
			}
			num++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				num2++;
				if (selector.Visit(towerDefenseCharacter, ref match))
				{
					TowerDefensePerfProfiler.Sample("registry.rectHits", num2);
					TowerDefensePerfProfiler.End(sourceMetric, startTicks, num);
					TowerDefensePerfProfiler.End("registry.rectQuery.select", startTicks, num);
					TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, num);
					return GodotObject.IsInstanceValid(match);
				}
			}
		}
		TowerDefensePerfProfiler.Sample("registry.rectHits", num2);
		TowerDefensePerfProfiler.End(sourceMetric, startTicks, num);
		TowerDefensePerfProfiler.End("registry.rectQuery.select", startTicks, num);
		TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, num);
		return GodotObject.IsInstanceValid(match);
	}

	public bool TrySelectAttackGridCharacterIntersectingRectForPhysicsFrameAcrossCells<TSelector>(ulong physicsFrame, Rect2 checkRect, int startLine, int endLine, int stepLine, int startColumn, int endColumn, int stepColumn, bool visitRowsFirst, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TSelector selector, out TowerDefenseCharacter match, out int visitedCells) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		EnsureAttackGridIndex(physicsFrame);
		match = null;
		visitedCells = 0;
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		if (IsAttackGridTraversalRangeInvalid(startLine, endLine, stepLine, startColumn, endColumn, stepColumn))
		{
			EndAttackGridQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.rectQuery.select");
			return false;
		}
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		if (visitRowsFirst)
		{
			TrySelectAttackGridRectCellsRowsFirst(attackGridCellsForExcludedCamp, attackGridAllLineCellsForExcludedCamp, checkRect, startLine, endLine, stepLine, startColumn, endColumn, stepColumn, includeAllLineCheck, ref selector, ref visitedCells, ref candidateCount, ref checkedCount, ref hitCount);
		}
		else
		{
			TrySelectAttackGridRectCellsColumnsFirst(attackGridCellsForExcludedCamp, attackGridAllLineCellsForExcludedCamp, checkRect, startLine, endLine, stepLine, startColumn, endColumn, stepColumn, includeAllLineCheck, ref selector, ref visitedCells, ref candidateCount, ref checkedCount, ref hitCount);
		}
		match = selector.SelectedMatch;
		EndAttackGridQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.rectQuery.select");
		return GodotObject.IsInstanceValid(match);
	}

	private bool ScanAttackGridTraversalCell<TSelector>(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> columns, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> allLineCells, int line, int column, bool includeAllLineCheck, Rect2 checkRect, ref TSelector selector, ref int candidateCount, ref int checkedCount, ref int hitCount) where TSelector : struct, ITowerDefenseAttackGridTraversalSelector
	{
		TowerDefenseCharacter match = null;
		List<TowerDefenseCharacter> value;
		bool flag = columns != null && columns.TryGetValue(column, out value) && ScanAttackGridSelectorCharacters(value, line, skipSameLineAllLineEntry: false, checkRect, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match, null);
		if (!flag && includeAllLineCheck && allLineCells.TryGetValue(column, out var value2))
		{
			flag = ScanAttackGridSelectorCharacters(value2, line, skipSameLineAllLineEntry: true, checkRect, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match, null);
		}
		selector.CompleteCell();
		if (!flag)
		{
			return selector.ShouldStopAfterCell;
		}
		return true;
	}

	private static bool TryAcceptAttackGridScanCandidate(TowerDefenseCharacter character, int line, bool skipSameLineAllLineEntry, HashSet<TowerDefenseCharacter> seen)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return false;
		}
		if (skipSameLineAllLineEntry && character.IsTargetableFromLine(line, includeAllLineCheck: false))
		{
			return false;
		}
		return seen?.Add(character) ?? true;
	}

	private bool TrySelectAttackGridSelectorAllLine<TSelector>(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, Rect2 checkRect, int line, int minColumn, int maxColumn, ref TSelector selector, ref int candidateCount, ref int checkedCount, ref int hitCount, ref TowerDefenseCharacter match, HashSet<TowerDefenseCharacter> seen) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		for (int i = minColumn; i <= maxColumn; i++)
		{
			if (attackGridAllLineCells.TryGetValue(i, out var value) && ScanAttackGridSelectorCharacters(value, line, skipSameLineAllLineEntry: true, checkRect, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match, seen))
			{
				return true;
			}
		}
		return false;
	}

	public bool TrySelectAttackGridCharacterIntersectingRectForPhysicsFrame<TSelector>(ulong physicsFrame, Rect2 checkRect, int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TSelector selector, out TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		EnsureAttackGridIndex(physicsFrame);
		match = null;
		NormalizeAttackGridColumnRange(ref minColumn, ref maxColumn);
		System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCellsForExcludedCamp = GetAttackGridCellsForExcludedCamp(excludedCamp);
		System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCellsForExcludedCamp = GetAttackGridAllLineCellsForExcludedCamp(excludedCamp);
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		ScanAttackGridSelectorQuery(seen: PrepareAttackGridSeen(minColumn, maxColumn), attackGridCells: attackGridCellsForExcludedCamp, attackGridAllLineCells: attackGridAllLineCellsForExcludedCamp, checkRect: checkRect, line: line, minColumn: minColumn, maxColumn: maxColumn, includeAllLineCheck: includeAllLineCheck, selector: ref selector, candidateCount: ref candidateCount, checkedCount: ref checkedCount, hitCount: ref hitCount, match: ref match);
		EndAttackGridQuery(perfStart, candidateCount, checkedCount, hitCount, "registry.rectQuery.select");
		return GodotObject.IsInstanceValid(match);
	}

	private bool TrySelectAttackGridSelectorLine<TSelector>(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, Rect2 checkRect, int line, int minColumn, int maxColumn, ref TSelector selector, ref int candidateCount, ref int checkedCount, ref int hitCount, ref TowerDefenseCharacter match, HashSet<TowerDefenseCharacter> seen) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		if (!attackGridCells.TryGetValue(line, out var value))
		{
			return false;
		}
		for (int i = minColumn; i <= maxColumn; i++)
		{
			if (value.TryGetValue(i, out var value2) && ScanAttackGridSelectorCharacters(value2, line, skipSameLineAllLineEntry: false, checkRect, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match, seen))
			{
				return true;
			}
		}
		return false;
	}

	public bool TrySelectAttackGridCharacterIntersectingRect<TSelector>(Rect2 checkRect, int line, int column, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TSelector selector, out TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		return TrySelectAttackGridCharacterIntersectingRect(checkRect, line, column, column, includeAllLineCheck, excludedCamp, ref selector, out match);
	}

	public bool TrySelectAttackGridCharacterIntersectingRect<TSelector>(Rect2 checkRect, int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TSelector selector, out TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		return TrySelectAttackGridCharacterIntersectingRectForPhysicsFrame(Engine.GetPhysicsFrames(), checkRect, line, minColumn, maxColumn, includeAllLineCheck, excludedCamp, ref selector, out match);
	}

	private void ScanAttackGridSelectorQuery<TSelector>(System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>>> attackGridCells, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> attackGridAllLineCells, Rect2 checkRect, int line, int minColumn, int maxColumn, bool includeAllLineCheck, ref TSelector selector, ref int candidateCount, ref int checkedCount, ref int hitCount, ref TowerDefenseCharacter match, HashSet<TowerDefenseCharacter> seen) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		if (!TrySelectAttackGridSelectorLine(attackGridCells, checkRect, line, minColumn, maxColumn, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match, seen) && includeAllLineCheck)
		{
			TrySelectAttackGridSelectorAllLine(attackGridAllLineCells, checkRect, line, minColumn, maxColumn, ref selector, ref candidateCount, ref checkedCount, ref hitCount, ref match, seen);
		}
	}

	private bool ScanAttackGridSelectorCharacters<TSelector>(List<TowerDefenseCharacter> candidates, int line, bool skipSameLineAllLineEntry, Rect2 checkRect, ref TSelector selector, ref int candidateCount, ref int checkedCount, ref int hitCount, ref TowerDefenseCharacter match, HashSet<TowerDefenseCharacter> seen) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (!TryAcceptAttackGridScanCandidate(towerDefenseCharacter, line, skipSameLineAllLineEntry, seen))
			{
				continue;
			}
			candidateCount++;
			if (!selector.CanConsider(towerDefenseCharacter))
			{
				continue;
			}
			checkedCount++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				hitCount++;
				if (selector.Visit(towerDefenseCharacter, ref match))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void FillSingleLineAttackGridCellRange<TPredicate>(ulong physicsFrame, int line, int minColumn, int maxColumn, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, ref int candidateCount, ref int checkedCount, ref int hitCount, List<TowerDefenseCharacter> destination) where TPredicate : struct, ITowerDefenseCharacterCandidatePredicate
	{
		FillLineCharactersInAttackGridColumnRange(GetAttackGridLineRangeCharactersForPhysicsFrame(physicsFrame, line, minColumn, maxColumn, includeAllLineCheck, excludedCamp), ref predicate, ref candidateCount, ref checkedCount, ref hitCount, destination);
	}

	private static bool IsAttackGridTraversalValueInRange(int value, int end, int step)
	{
		if (step <= 0)
		{
			return value >= end;
		}
		return value <= end;
	}

	private static void NormalizeAttackGridTraversalRange(ref int minimum, ref int maximum)
	{
		if (minimum > maximum)
		{
			int num = maximum;
			int num2 = minimum;
			minimum = num;
			maximum = num2;
		}
	}

	private static bool IsAttackGridTraversalRangeInvalid(int startLine, int endLine, int stepLine, int startColumn, int endColumn, int stepColumn)
	{
		if (stepLine != 0 && stepColumn != 0 && (stepLine <= 0 || startLine <= endLine) && (stepLine >= 0 || startLine >= endLine) && (stepColumn <= 0 || startColumn <= endColumn))
		{
			if (stepColumn < 0)
			{
				return startColumn < endColumn;
			}
			return false;
		}
		return true;
	}

	private static void AddUniqueAttackGridCharacter(List<TowerDefenseCharacter> list, TowerDefenseCharacter character)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] == character)
			{
				return;
			}
		}
		list.Add(character);
	}

	public void FillCampTargets(TowerDefenseEnum.CHARACTER_CAMP camp, List<TowerDefenseCharacter> output, bool fliterGraveStone = true)
	{
		if (output == null)
		{
			return;
		}
		output.Clear();
		List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !(towerDefenseCharacter is TowerDefenseCrater) && !(towerDefenseCharacter is TowerDefenseItem) && (!fliterGraveStone || !(towerDefenseCharacter is TowerDefenseGravestone)) && towerDefenseCharacter.camp != camp)
			{
				output.Add(towerDefenseCharacter);
			}
		}
	}

	private void InvalidateCharacterCaches(bool invalidateCleanCharacters, bool invalidateAttackGrid = true, bool invalidateSpatialBuckets = true)
	{
		QueryRevision++;
		GeometryRevision++;
		if (invalidateCleanCharacters)
		{
			_cleanCharactersFrame = -1L;
		}
		_lineCharactersFrame = -1L;
		_columnCharactersFrame = -1L;
		_lineCombinedFrame = -1L;
		_attackGridLineRangeQueryFrame = -1L;
		if (invalidateAttackGrid)
		{
			_attackGridFrame = -1L;
		}
		_lineSweepFrame = -1L;
		if (invalidateSpatialBuckets)
		{
			_spatialBucketsFrame = -1L;
		}
	}

	private static void ClearCharacterLists(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> lists)
	{
		foreach (List<TowerDefenseCharacter> value in lists.Values)
		{
			value.Clear();
		}
	}

	private static List<TowerDefenseCharacter> GetOrCreateCharacterList(System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> lists, int key)
	{
		if (!lists.TryGetValue(key, out var value))
		{
			value = (lists[key] = new List<TowerDefenseCharacter>());
		}
		return value;
	}

	public void NotifyCharacterCampChanged(TowerDefenseCharacter character)
	{
		if (_registeredCharacters.Contains(character))
		{
			_characterClassificationCountsDirty = true;
			_lineCombinedFrame = -1L;
			_attackGridFrame = -1L;
			_attackGridLineRangeQueryFrame = -1L;
			_spatialBucketsFrame = -1L;
		}
	}

	public void NotifyCharacterConfigChanged(TowerDefenseCharacter character)
	{
		if (_registeredCharacters.Contains(character))
		{
			_characterClassificationCountsDirty = true;
			_attackGridLineRangeQueryFrame = -1L;
		}
	}

	public void NotifyCharacterTargetingChanged(TowerDefenseCharacter character)
	{
		if (_registeredCharacters.Contains(character))
		{
			InvalidateCharacterCaches(invalidateCleanCharacters: false);
		}
	}

	public void NotifyCharacterGeometryChanged(TowerDefenseCharacter character)
	{
		if (_registeredCharacters.Contains(character))
		{
			GeometryRevision++;
			_lineSweepFrame = -1L;
			if (!CanReuseSpatialCoverageAfterGeometryChange(character))
			{
				_spatialBucketsFrame = -1L;
			}
		}
	}

	public void NotifyCharacterGridChanged(TowerDefenseCharacter character)
	{
		if (_registeredCharacters.Contains(character))
		{
			bool flag = TryUpdateAttackGridPosition(character);
			bool flag2 = CanReuseSpatialCoverageAfterGeometryChange(character);
			InvalidateCharacterCaches(invalidateCleanCharacters: false, !flag, !flag2);
		}
	}

	public void Register(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && _registeredCharacters.Add(character))
		{
			_activeCharacters.Add(character);
			AdvanceMembershipRevision();
			_characterClassificationCountsDirty = true;
			bool flag = TryAddAttackGridCharacter(character);
			InvalidateCharacterCaches(invalidateCleanCharacters: true, !flag);
		}
	}

	public void Unregister(TowerDefenseCharacter character)
	{
		if (_registeredCharacters.Remove(character))
		{
			bool flag = TryRemoveAttackGridCharacter(character);
			RemoveActiveCharacter(character);
			AdvanceMembershipRevision();
			_characterClassificationCountsDirty = true;
			InvalidateCharacterCaches(invalidateCleanCharacters: true, !flag);
		}
	}

	private void EnsureCharacterClassificationCounts()
	{
		if (!_characterClassificationCountsDirty)
		{
			return;
		}
		_characterCountByCamp.Clear();
		_characterCountByConfigName.Clear();
		_characterCountByCampAndConfigName.Clear();
		_outsideCampSameConfigQueryCache.Clear();
		_sameCampDifferentConfigQueryCache.Clear();
		for (int i = 0; i < _activeCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _activeCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				string text = towerDefenseCharacter.config?.name ?? string.Empty;
				TowerDefenseEnum.CHARACTER_CAMP camp = towerDefenseCharacter.camp;
				_characterCountByCamp[camp] = _characterCountByCamp.GetValueOrDefault(camp) + 1;
				_characterCountByConfigName[text] = _characterCountByConfigName.GetValueOrDefault(text) + 1;
				(TowerDefenseEnum.CHARACTER_CAMP, string) key = (camp, text);
				_characterCountByCampAndConfigName[key] = _characterCountByCampAndConfigName.GetValueOrDefault(key) + 1;
			}
		}
		_characterClassificationCountsDirty = false;
	}

	public bool HasCharacterOutsideCampWithConfigName(TowerDefenseEnum.CHARACTER_CAMP camp, string configName)
	{
		EnsureCharacterClassificationCounts();
		string text = configName ?? string.Empty;
		int valueOrDefault = _characterCountByConfigName.GetValueOrDefault(text);
		int valueOrDefault2 = _characterCountByCampAndConfigName.GetValueOrDefault((camp, text));
		return valueOrDefault > valueOrDefault2;
	}

	public bool HasCharacterInCampWithDifferentConfigName(TowerDefenseEnum.CHARACTER_CAMP camp, string configName)
	{
		EnsureCharacterClassificationCounts();
		string item = configName ?? string.Empty;
		int valueOrDefault = _characterCountByCamp.GetValueOrDefault(camp);
		int valueOrDefault2 = _characterCountByCampAndConfigName.GetValueOrDefault((camp, item));
		return valueOrDefault > valueOrDefault2;
	}

	public int GetZombieCount()
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
		TowerDefensePerfProfiler.End("registry.zombieCount", startTicks, cleanCharactersList.Count);
		return _cleanZombieCount;
	}

	public int GetVaseCount()
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
		TowerDefensePerfProfiler.End("registry.vaseCount", startTicks, cleanCharactersList.Count);
		return _cleanVaseCount;
	}

	public List<TowerDefenseCharacter> GetCleanCharactersList()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (_cleanCharactersFrame < 0)
		{
			long startTicks = TowerDefensePerfProfiler.Begin();
			int num = 0;
			CompactInvalidActiveCharacters();
			_cleanCharactersFrame = (long)physicsFrames;
			_cleanCharacters.Clear();
			_cleanItems.Clear();
			_cleanZombieCount = 0;
			_cleanVaseCount = 0;
			try
			{
				for (int i = 0; i < _activeCharacters.Count; i++)
				{
					num++;
					TowerDefenseCharacter towerDefenseCharacter = _activeCharacters[i];
					if (GodotObject.IsInstanceValid(towerDefenseCharacter))
					{
						_cleanCharacters.Add(towerDefenseCharacter);
						if (towerDefenseCharacter is TowerDefenseItem item)
						{
							_cleanItems.Add(item);
						}
						if (towerDefenseCharacter is TowerDefenseZombie)
						{
							_cleanZombieCount++;
						}
						if (towerDefenseCharacter is TowerDefenseVase)
						{
							_cleanVaseCount++;
						}
					}
				}
			}
			finally
			{
				TowerDefensePerfProfiler.End("registry.cleanBuild", startTicks, num);
			}
		}
		return _cleanCharacters;
	}

	public void Clear()
	{
		AdvanceRegistryRevisions();
		ClearCharacterCollections();
		ClearLineAndColumnCaches();
		ClearLineSweepCaches();
		ClearAttackGridCaches();
		ClearSpatialCaches();
	}

	private void AdvanceRegistryRevisions()
	{
		MembershipRevision++;
		QueryRevision++;
		GeometryRevision++;
	}

	private void ClearAttackGridCaches()
	{
		_attackGridCells.Clear();
		_attackGridAllLineCells.Clear();
		_attackGridNonPlantCells.Clear();
		_attackGridNonPlantAllLineCells.Clear();
		_attackGridNonZombieCells.Clear();
		_attackGridNonZombieAllLineCells.Clear();
		_attackGridLineRangeQueryCache.Clear();
		_attackGridLineRangeQueryBuiltKeys.Clear();
		_attackGridPositionByCharacter.Clear();
		_opposingMaskCandidateCache.Clear();
		_attackGridFrame = -1L;
		_attackGridLineRangeQueryFrame = -1L;
	}

	private void ClearCharacterCollections()
	{
		_activeCharacters.Clear();
		_registeredCharacters.Clear();
		_characterCountByCamp.Clear();
		_characterCountByConfigName.Clear();
		_characterCountByCampAndConfigName.Clear();
		_outsideCampSameConfigQueryCache.Clear();
		_sameCampDifferentConfigQueryCache.Clear();
		_characterClassificationCountsDirty = true;
		_cleanCharacters.Clear();
		_cleanZombieCount = 0;
		_cleanVaseCount = 0;
	}

	private void ClearLineAndColumnCaches()
	{
		_lineCharacters.Clear();
		_columnCharacters.Clear();
		_lineCombinedCache.Clear();
		_lineCombinedQueryCache.Clear();
		_lineCombinedQueryBuiltLines.Clear();
		_lineCombinedNonPlantCache.Clear();
		_lineCombinedNonPlantQueryCache.Clear();
		_lineCombinedNonPlantQueryBuiltLines.Clear();
		_lineCombinedNonZombieCache.Clear();
		_lineCombinedNonZombieQueryCache.Clear();
		_lineCombinedNonZombieQueryBuiltLines.Clear();
		_allLineCheckChars.Clear();
		_allLineCheckNonPlantChars.Clear();
		_allLineCheckNonZombieChars.Clear();
		_rowsQueryBuffer.Clear();
		_rowsQuerySeen.Clear();
		_rectQueryBuffer.Clear();
		_gridWindowQueryBuffer.Clear();
	}

	private void ClearLineSweepCaches()
	{
		_lineSweepIntervals.Clear();
		_lineSweepIntervalsByMaxX.Clear();
		_lineSweepAllLineEntries.Clear();
		_lineSweepAllLineEntriesByMaxX.Clear();
		_lineSweepTallIntervals.Clear();
		_lineSweepTallIntervalsByMaxX.Clear();
		_lineSweepTallAllLineEntries.Clear();
		_lineSweepTallAllLineEntriesByMaxX.Clear();
		_lineSweepScanBuffer.Clear();
		_lineSweepBuiltLines.Clear();
		_lineSweepFrame = -1L;
	}

	private void ClearSpatialCaches()
	{
		_spatialBuckets.Clear();
		_spatialAllLineCheckChars.Clear();
		_spatialNonPlantBuckets.Clear();
		_spatialNonPlantAllLineCheckChars.Clear();
		_spatialNonZombieBuckets.Clear();
		_spatialNonZombieAllLineCheckChars.Clear();
		_spatialCandidateBuffer.Clear();
		_spatialCandidateSeen.Clear();
		_spatialCoverageByCharacter.Clear();
	}

	public List<TowerDefenseCharacter> GetColumnCharactersList(int column)
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (_columnCharactersFrame < 0)
		{
			_columnCharactersFrame = (long)physicsFrames;
			ClearCharacterLists(_columnCharacters);
			List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
			for (int i = 0; i < cleanCharactersList.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
				int x = towerDefenseCharacter.gridPos.X;
				if (!_columnCharacters.TryGetValue(x, out var value))
				{
					value = GetOrCreateCharacterList(_columnCharacters, x);
				}
				value.Add(towerDefenseCharacter);
			}
		}
		if (_columnCharacters.TryGetValue(column, out var value2))
		{
			return value2;
		}
		return EmptyCharacterList;
	}

	private bool TryGetColumnsForRect(Rect2 checkRect, out int minColumn, out int maxColumn, bool clampToMapPadding = true)
	{
		minColumn = 0;
		maxColumn = 0;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2 gridSize = instance.gridSize;
		Vector2 gridBeginPos = instance.gridBeginPos;
		Vector2I gridNum = instance.gridNum;
		if (gridNum.X <= 0 || gridSize.X <= 0f)
		{
			return false;
		}
		Vector2 vector = checkRect.Position + checkRect.Size;
		float num = Mathf.Min(checkRect.Position.X, vector.X);
		float num2 = Mathf.Max(checkRect.Position.X, vector.X);
		minColumn = Mathf.FloorToInt((num - gridBeginPos.X) / gridSize.X) - 1;
		maxColumn = Mathf.FloorToInt((num2 - gridBeginPos.X) / gridSize.X) + 2;
		if (clampToMapPadding)
		{
			minColumn = Mathf.Clamp(minColumn, -1, gridNum.X + 1);
			maxColumn = Mathf.Clamp(maxColumn, -1, gridNum.X + 1);
		}
		return minColumn <= maxColumn;
	}

	private void AdvanceMembershipRevision()
	{
		MembershipRevision++;
	}

	public int GetEffectCount()
	{
		return GetEffectCountForPhysicsFrame(Engine.GetPhysicsFrames());
	}

	internal int GetEffectCountForPhysicsFrame(ulong currentFrame)
	{
		if (currentFrame != (ulong)_effectCountFrame)
		{
			_effectCountFrame = (long)currentFrame;
			_effectCount = GetTree().GetNodeCountInGroup("Effect");
		}
		return _effectCount;
	}

	private static bool IsFinalWaveTargetCandidate(TowerDefenseCharacter character, TowerDefenseEnum.CHARACTER_CAMP camp, bool fliterGraveStone)
	{
		if (character.die || character.nearDie)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(character.instance) && character.instance.die)
		{
			return false;
		}
		if (character is TowerDefenseCrater towerDefenseCrater)
		{
			if (towerDefenseCrater.HasPendingRevival)
			{
				return towerDefenseCrater.RevivalCamp != camp;
			}
			return false;
		}
		if (character is TowerDefenseItem)
		{
			return false;
		}
		if (fliterGraveStone && character is TowerDefenseGravestone)
		{
			return false;
		}
		return character.camp != camp;
	}

	public bool TryGetFinalWaveTarget(TowerDefenseEnum.CHARACTER_CAMP camp, out TowerDefenseCharacter target, out bool hasPendingDestroyZombie, bool fliterGraveStone = true)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		target = null;
		hasPendingDestroyZombie = false;
		List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
		int num = 0;
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				num++;
				if (IsFinalWaveTargetCandidate(towerDefenseCharacter, camp, fliterGraveStone))
				{
					target = towerDefenseCharacter;
					TowerDefensePerfProfiler.End("registry.finalWaveCheck", startTicks, num);
					return true;
				}
				if (towerDefenseCharacter is TowerDefenseZombie { isDestroy: not false, skipDestroySet: false })
				{
					hasPendingDestroyZombie = true;
				}
			}
		}
		TowerDefensePerfProfiler.End("registry.finalWaveCheck", startTicks, num);
		return false;
	}

	private bool TryGetFullLineRange(out int minLine, out int maxLine)
	{
		minLine = 0;
		maxLine = 0;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.gridNum.Y <= 0)
		{
			return false;
		}
		minLine = 1;
		maxLine = instance.gridNum.Y;
		return true;
	}

	public Array<TowerDefenseCharacter> GetCleanCharacters()
	{
		Array<TowerDefenseCharacter> array = new Array<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter cleanCharacters in GetCleanCharactersList())
		{
			array.Add(cleanCharacters);
		}
		return array;
	}

	public Array<TowerDefenseCharacter> GetCharactersIntersectingRect(Rect2 checkRect)
	{
		Array<TowerDefenseCharacter> array = new Array<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter charactersIntersectingRect in GetCharactersIntersectingRectList(checkRect))
		{
			array.Add(charactersIntersectingRect);
		}
		return array;
	}

	public Godot.Collections.Array GetLineCharacters(int line)
	{
		return ToGodotArray(GetLineCharactersList(line));
	}

	public Godot.Collections.Array GetColumnCharacters(int column)
	{
		return ToGodotArray(GetColumnCharactersList(column));
	}

	public Godot.Collections.Array GetCharactersForLine(int line)
	{
		return ToGodotArray(GetCharactersForLineList(line));
	}

	private static Godot.Collections.Array ToGodotArray(List<TowerDefenseCharacter> characters)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseCharacter character in characters)
		{
			array.Add(character);
		}
		return array;
	}

	public List<TowerDefenseCharacter> GetCharactersForGridWindowList(int centerColumn, int centerLine, int rangeColumns, int rangeLines)
	{
		_gridWindowQueryBuffer.Clear();
		rangeColumns = Math.Max(0, rangeColumns);
		rangeLines = Math.Max(0, rangeLines);
		int num = centerColumn - rangeColumns;
		int num2 = centerColumn + rangeColumns;
		int num3 = centerLine - rangeLines;
		int num4 = centerLine + rangeLines;
		for (int i = num; i <= num2; i++)
		{
			List<TowerDefenseCharacter> columnCharactersList = GetColumnCharactersList(i);
			for (int j = 0; j < columnCharactersList.Count; j++)
			{
				TowerDefenseCharacter towerDefenseCharacter = columnCharactersList[j];
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					int y = towerDefenseCharacter.gridPos.Y;
					if (y >= num3 && y <= num4)
					{
						_gridWindowQueryBuffer.Add(towerDefenseCharacter);
					}
				}
			}
		}
		return _gridWindowQueryBuffer;
	}

	public bool TryFindItemIntersectingRect<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TPredicate predicate, out TowerDefenseItem match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		match = null;
		GetCleanCharactersList();
		int num = 0;
		int num2 = 0;
		TowerDefensePerfProfiler.Sample("registry.itemRectCandidates", _cleanItems.Count);
		for (int i = 0; i < _cleanItems.Count; i++)
		{
			TowerDefenseItem towerDefenseItem = _cleanItems[i];
			if (!GodotObject.IsInstanceValid(towerDefenseItem) || (line != -2147483648 && !towerDefenseItem.IsTargetableFromLine(line, includeAllLineCheck)) || !predicate.CanConsider(towerDefenseItem))
			{
				continue;
			}
			num++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseItem.WorldHitRect))
			{
				num2++;
				if (predicate.Matches(towerDefenseItem))
				{
					match = towerDefenseItem;
					TowerDefensePerfProfiler.Sample("registry.itemRectHits", num2);
					TowerDefensePerfProfiler.End("registry.itemRectQuery", startTicks, num);
					return true;
				}
			}
		}
		TowerDefensePerfProfiler.Sample("registry.itemRectHits", num2);
		TowerDefensePerfProfiler.End("registry.itemRectQuery", startTicks, num);
		return false;
	}

	private System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> GetLineCharactersCacheExcludingCamp(TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		return excludedCamp switch
		{
			TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE => _lineCombinedNonZombieCache, 
			TowerDefenseEnum.CHARACTER_CAMP.PLANT => _lineCombinedNonPlantCache, 
			_ => _lineCombinedCache, 
		};
	}

	public List<TowerDefenseCharacter> GetCharactersForLineListExcludingCamp(int line, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		EnsureLineCombinedCache(Engine.GetPhysicsFrames());
		return excludedCamp switch
		{
			TowerDefenseEnum.CHARACTER_CAMP.PLANT => GetFilteredCharactersForLineList(line, _lineCombinedNonPlantCache, _lineCombinedNonPlantQueryCache, _lineCombinedNonPlantQueryBuiltLines, _allLineCheckNonPlantChars), 
			TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE => GetFilteredCharactersForLineList(line, _lineCombinedNonZombieCache, _lineCombinedNonZombieQueryCache, _lineCombinedNonZombieQueryBuiltLines, _allLineCheckNonZombieChars), 
			_ => GetCharactersForLineList(line), 
		};
	}

	public List<TowerDefenseCharacter> GetLineCharactersListExcludingCamp(int line, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		EnsureLineCombinedCache(Engine.GetPhysicsFrames());
		if (!GetLineCharactersCacheExcludingCamp(excludedCamp).TryGetValue(line, out var value))
		{
			return EmptyCharacterList;
		}
		return value;
	}

	public List<TowerDefenseCharacter> GetCharactersForLineList(int line)
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		EnsureLineCombinedCache(physicsFrames);
		if (_lineCombinedQueryBuiltLines.Contains(line) && _lineCombinedQueryCache.TryGetValue(line, out var value))
		{
			return value;
		}
		List<TowerDefenseCharacter> orCreateCharacterList = GetOrCreateCharacterList(_lineCombinedQueryCache, line);
		orCreateCharacterList.Clear();
		if (_lineCombinedCache.TryGetValue(line, out var value2))
		{
			orCreateCharacterList.AddRange(value2);
		}
		AddDifferentLineCharacters(orCreateCharacterList, _allLineCheckChars, line);
		_lineCombinedQueryBuiltLines.Add(line);
		return orCreateCharacterList;
	}

	public List<TowerDefenseCharacter> GetLineCharactersList(int line)
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (_lineCharactersFrame < 0)
		{
			BuildLineCharactersCache(physicsFrames);
		}
		if (_lineCharacters.TryGetValue(line, out var value))
		{
			return value;
		}
		return EmptyCharacterList;
	}

	private void BuildLineCharactersCache(ulong currentFrame)
	{
		_lineCharactersFrame = (long)currentFrame;
		ClearCharacterLists(_lineCharacters);
		List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			int y = towerDefenseCharacter.gridPos.Y;
			GetOrCreateCharacterList(_lineCharacters, y).Add(towerDefenseCharacter);
			if (TryResolveAttackGridLineAlias(towerDefenseCharacter, y, out var aliasLine))
			{
				GetOrCreateCharacterList(_lineCharacters, aliasLine).Add(towerDefenseCharacter);
			}
		}
	}

	private void EnsureLineCombinedCache(ulong currentFrame)
	{
		if (_lineCombinedFrame < 0)
		{
			_lineCombinedFrame = (long)currentFrame;
			ResetLineCombinedCaches();
			List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
			for (int i = 0; i < cleanCharactersList.Count; i++)
			{
				AddLineCombinedCharacter(cleanCharactersList[i]);
			}
		}
	}

	private void AddLineCombinedCharacter(TowerDefenseCharacter character)
	{
		int y = character.gridPos.Y;
		GetOrCreateCharacterList(_lineCombinedCache, y).Add(character);
		AddCampFilteredLineCombinedCharacter(character, y);
		if (TryResolveAttackGridLineAlias(character, y, out var aliasLine))
		{
			GetOrCreateCharacterList(_lineCombinedCache, aliasLine).Add(character);
			AddCampFilteredLineCombinedCharacter(character, aliasLine);
		}
		if (character.targetRegistrationComponent.allLineCheck)
		{
			AddAllLineCheckCharacter(character);
		}
	}

	private void AddCampFilteredLineCombinedCharacter(TowerDefenseCharacter character, int charLine)
	{
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			GetOrCreateCharacterList(_lineCombinedNonPlantCache, charLine).Add(character);
		}
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			GetOrCreateCharacterList(_lineCombinedNonZombieCache, charLine).Add(character);
		}
	}

	private void AddAllLineCheckCharacter(TowerDefenseCharacter character)
	{
		_allLineCheckChars.Add(character);
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			_allLineCheckNonPlantChars.Add(character);
		}
		if (character.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			_allLineCheckNonZombieChars.Add(character);
		}
	}

	private void ResetLineCombinedCaches()
	{
		ClearCharacterLists(_lineCombinedCache);
		ClearCharacterLists(_lineCombinedQueryCache);
		_lineCombinedQueryBuiltLines.Clear();
		ClearCharacterLists(_lineCombinedNonPlantCache);
		ClearCharacterLists(_lineCombinedNonPlantQueryCache);
		_lineCombinedNonPlantQueryBuiltLines.Clear();
		ClearCharacterLists(_lineCombinedNonZombieCache);
		ClearCharacterLists(_lineCombinedNonZombieQueryCache);
		_lineCombinedNonZombieQueryBuiltLines.Clear();
		_allLineCheckChars.Clear();
		_allLineCheckNonPlantChars.Clear();
		_allLineCheckNonZombieChars.Clear();
	}

	private static bool IsLineDirectCandidateValid(TowerDefenseCharacter character, TowerDefenseEnum.CHARACTER_CAMP? excludedCamp)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return false;
		}
		if (excludedCamp.HasValue)
		{
			return character.camp != excludedCamp.Value;
		}
		return true;
	}

	private bool TryFindLineDirectCharacter<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TPredicate predicate, out TowerDefenseCharacter match, out bool found, TowerDefenseEnum.CHARACTER_CAMP? excludedCamp = null) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		match = null;
		found = false;
		List<TowerDefenseCharacter> lineDirectCharacterSource = GetLineDirectCharacterSource(line, includeAllLineCheck, excludedCamp);
		int checkedCount = 0;
		int hitCount = 0;
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", lineDirectCharacterSource.Count);
		found = TryScanLineDirectCharacters(lineDirectCharacterSource, checkRect, ref predicate, excludedCamp, ref checkedCount, ref hitCount, out match);
		EndLineDirectRectQuery(perfStart, checkedCount, hitCount);
		return true;
	}

	private static bool TryScanLineDirectCharacters<TPredicate>(List<TowerDefenseCharacter> source, Rect2 checkRect, ref TPredicate predicate, TowerDefenseEnum.CHARACTER_CAMP? excludedCamp, ref int checkedCount, ref int hitCount, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		for (int i = 0; i < source.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = source[i];
			if (!IsLineDirectCandidateValid(towerDefenseCharacter, excludedCamp) || !predicate.CanConsider(towerDefenseCharacter))
			{
				continue;
			}
			checkedCount++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				hitCount++;
				if (predicate.Matches(towerDefenseCharacter))
				{
					match = towerDefenseCharacter;
					return true;
				}
			}
		}
		return false;
	}

	private List<TowerDefenseCharacter> GetLineDirectCharacterSource(int line, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP? excludedCamp)
	{
		if (excludedCamp.HasValue)
		{
			if (!includeAllLineCheck)
			{
				return GetLineCharactersListExcludingCamp(line, excludedCamp.Value);
			}
			return GetCharactersForLineListExcludingCamp(line, excludedCamp.Value);
		}
		if (!includeAllLineCheck)
		{
			return GetLineCharactersList(line);
		}
		return GetCharactersForLineList(line);
	}

	private static List<TowerDefenseCharacter> GetFilteredCharactersForLineList(int line, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> lineCache, System.Collections.Generic.Dictionary<int, List<TowerDefenseCharacter>> queryCache, HashSet<int> builtLines, List<TowerDefenseCharacter> allLineCharacters)
	{
		if (builtLines.Contains(line) && queryCache.TryGetValue(line, out var value))
		{
			return value;
		}
		List<TowerDefenseCharacter> orCreateCharacterList = GetOrCreateCharacterList(queryCache, line);
		orCreateCharacterList.Clear();
		if (lineCache.TryGetValue(line, out var value2))
		{
			orCreateCharacterList.AddRange(value2);
		}
		AddDifferentLineCharacters(orCreateCharacterList, allLineCharacters, line);
		builtLines.Add(line);
		return orCreateCharacterList;
	}

	private static void AddDifferentLineCharacters(List<TowerDefenseCharacter> destination, List<TowerDefenseCharacter> allLineCharacters, int line)
	{
		for (int i = 0; i < allLineCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = allLineCharacters[i];
			if (!towerDefenseCharacter.IsTargetableFromLine(line, includeAllLineCheck: false))
			{
				destination.Add(towerDefenseCharacter);
			}
		}
	}

	internal bool TryGetSmallCharactersForLineListExcludingCampForFrame(int line, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, bool includeAllLineCheck, int maxCount, ulong currentFrame, out List<TowerDefenseCharacter> characters)
	{
		characters = EmptyCharacterList;
		if (excludedCamp != TowerDefenseEnum.CHARACTER_CAMP.PLANT && excludedCamp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
		{
			return false;
		}
		if (currentFrame > 9223372036854775807L)
		{
			currentFrame = Engine.GetPhysicsFrames();
		}
		EnsureLineCombinedCache(currentFrame);
		characters = (includeAllLineCheck ? GetSmallLineCharactersWithAllLine(line, excludedCamp) : GetSmallLineCharactersWithoutAllLine(line, excludedCamp));
		if (characters.Count <= maxCount)
		{
			return true;
		}
		characters = EmptyCharacterList;
		return false;
	}

	private List<TowerDefenseCharacter> GetSmallLineCharactersWithAllLine(int line, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		if (excludedCamp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
		{
			return GetFilteredCharactersForLineList(line, _lineCombinedNonZombieCache, _lineCombinedNonZombieQueryCache, _lineCombinedNonZombieQueryBuiltLines, _allLineCheckNonZombieChars);
		}
		return GetFilteredCharactersForLineList(line, _lineCombinedNonPlantCache, _lineCombinedNonPlantQueryCache, _lineCombinedNonPlantQueryBuiltLines, _allLineCheckNonPlantChars);
	}

	private List<TowerDefenseCharacter> GetSmallLineCharactersWithoutAllLine(int line, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		if (!((excludedCamp == TowerDefenseEnum.CHARACTER_CAMP.PLANT) ? _lineCombinedNonPlantCache : _lineCombinedNonZombieCache).TryGetValue(line, out var value))
		{
			return EmptyCharacterList;
		}
		return value;
	}

	private void FillLineSweepAllLineEntries(List<TowerDefenseCharacter> clean)
	{
		for (int i = 0; i < clean.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = clean[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.targetRegistrationComponent.allLineCheck)
			{
				Rect2 worldBroadphaseRect = towerDefenseCharacter.WorldBroadphaseRect;
				LineSweepEntry item = new LineSweepEntry(towerDefenseCharacter, worldBroadphaseRect);
				_lineSweepAllLineEntries.Add(item);
				_lineSweepAllLineEntriesByMaxX.Add(item);
				if (towerDefenseCharacter.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
				{
					_lineSweepTallAllLineEntries.Add(item);
					_lineSweepTallAllLineEntriesByMaxX.Add(item);
				}
			}
		}
	}

	private void SortLineSweepAllLineEntries()
	{
		_lineSweepAllLineEntries.Sort(CompareLineSweepEntry);
		_lineSweepAllLineEntriesByMaxX.Sort(CompareLineSweepEntryMaxX);
		_lineSweepTallAllLineEntries.Sort(CompareLineSweepEntry);
		_lineSweepTallAllLineEntriesByMaxX.Sort(CompareLineSweepEntryMaxX);
	}

	private static int UpperBoundLineSweepMinX(List<LineSweepEntry> entries, float right)
	{
		int num = 0;
		int num2 = entries.Count;
		while (num < num2)
		{
			int num3 = num + (num2 - num >> 1);
			if (entries[num3].MinX <= right)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private static int LowerBoundLineSweepMaxX(List<LineSweepEntry> entries, float left)
	{
		int num = 0;
		int num2 = entries.Count;
		while (num < num2)
		{
			int num3 = num + (num2 - num >> 1);
			if (entries[num3].MaxX < left)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3;
			}
		}
		return num;
	}

	private static void GetLineSweepHorizontalBounds(Rect2 checkRect, out float left, out float right)
	{
		Vector2 vector = checkRect.Position + checkRect.Size;
		left = Mathf.Min(checkRect.Position.X, vector.X);
		right = Mathf.Max(checkRect.Position.X, vector.X);
	}

	private void AddLineSweepCandidates(List<LineSweepEntry> entries, List<LineSweepEntry> entriesByMaxX, float left, float right, int line, bool skipSameLineAllLineEntry, List<TowerDefenseCharacter> result, ref int scannedCount)
	{
		int minScanEnd;
		int maxScanStart;
		bool flag = ChooseLineSweepScanByMaxX(entries, entriesByMaxX, left, right, out minScanEnd, out maxScanStart);
		if (flag)
		{
			FillLineSweepScanBufferByMaxX(entriesByMaxX, maxScanStart, left, right, line, skipSameLineAllLineEntry, ref scannedCount);
			entries = _lineSweepScanBuffer;
			minScanEnd = entries.Count;
		}
		AddLineSweepCandidateEntries(entries, minScanEnd, flag, left, right, line, skipSameLineAllLineEntry, result, ref scannedCount);
	}

	private bool TryFillLineSweepCandidateBuffer(Rect2 checkRect, int line, bool includeAllLineCheck, List<TowerDefenseCharacter> result, ulong currentFrame)
	{
		EnsureLineSweepIndex(currentFrame, line);
		result.Clear();
		_spatialCandidateSeen.Clear();
		GetLineSweepHorizontalBounds(checkRect, out var left, out var right);
		int scannedCount = 0;
		AddLineSweepLineCandidates(line, left, right, result, ref scannedCount);
		if (includeAllLineCheck)
		{
			AddLineSweepCandidates(_lineSweepAllLineEntries, _lineSweepAllLineEntriesByMaxX, left, right, line, skipSameLineAllLineEntry: true, result, ref scannedCount);
		}
		RecordLineSweepCandidateMetrics(scannedCount, result.Count);
		return true;
	}

	private void AddLineSweepLineCandidates(int line, float left, float right, List<TowerDefenseCharacter> result, ref int scannedCount)
	{
		if (_lineSweepIntervals.TryGetValue(line, out var value))
		{
			AddLineSweepCandidates(value, GetLineSweepMaxEntries(line), left, right, line, skipSameLineAllLineEntry: false, result, ref scannedCount);
		}
	}

	private void AddLineSweepCandidateEntries(List<LineSweepEntry> entries, int minScanEnd, bool scanByMaxX, float left, float right, int line, bool skipSameLineAllLineEntry, List<TowerDefenseCharacter> result, ref int scannedCount)
	{
		for (int i = 0; i < minScanEnd; i++)
		{
			LineSweepEntry entry = entries[i];
			if (!scanByMaxX)
			{
				scannedCount++;
				if (!CanUseLineSweepCandidateEntry(entry, left, right, line, skipSameLineAllLineEntry, out var _))
				{
					continue;
				}
			}
			TowerDefenseCharacter character2 = entry.Character;
			if (_spatialCandidateSeen.Add(character2))
			{
				result.Add(character2);
			}
		}
	}

	private bool TryFillLineSweepCandidateBuffer(Rect2 checkRect, int line, bool includeAllLineCheck)
	{
		return TryFillLineSweepCandidateBuffer(checkRect, line, includeAllLineCheck, _spatialCandidateBuffer, Engine.GetPhysicsFrames());
	}

	private static bool CanUseLineSweepCandidateEntry(LineSweepEntry entry, float left, float right, int line, bool skipSameLineAllLineEntry, out TowerDefenseCharacter character)
	{
		character = null;
		if (entry.MinX > right)
		{
			return false;
		}
		if (entry.MaxX < left)
		{
			return false;
		}
		character = entry.Character;
		if (GodotObject.IsInstanceValid(character))
		{
			if (skipSameLineAllLineEntry)
			{
				return !character.IsTargetableFromLine(line, includeAllLineCheck: false);
			}
			return true;
		}
		return false;
	}

	private static void RecordLineSweepCandidateMetrics(int scannedCount, int candidateCount)
	{
		TowerDefensePerfProfiler.Sample("registry.lineSweepScanned", scannedCount);
		TowerDefensePerfProfiler.Sample("registry.lineSweepCandidates", candidateCount);
	}

	private static int CompareLineSweepEntry(LineSweepEntry a, LineSweepEntry b)
	{
		return a.MinX.CompareTo(b.MinX);
	}

	private static int CompareLineSweepEntryMaxX(LineSweepEntry a, LineSweepEntry b)
	{
		int num = a.MaxX.CompareTo(b.MaxX);
		if (num == 0)
		{
			return a.MinX.CompareTo(b.MinX);
		}
		return num;
	}

	private void EnsureLineSweepIndex(ulong currentFrame, int line = -2147483648)
	{
		if (currentFrame != (ulong)_lineSweepFrame)
		{
			RebuildLineSweepFrame(currentFrame);
		}
		if (line != -2147483648)
		{
			EnsureLineSweepLineIndex(line);
		}
	}

	private bool TryFindLineSweepCharacter<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TPredicate predicate, out TowerDefenseCharacter match, out bool found) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		return TryFindLineSweepCharacter(checkRect, line, includeAllLineCheck, ref predicate, out match, out found, _lineSweepIntervals, _lineSweepIntervalsByMaxX, _lineSweepAllLineEntries, _lineSweepAllLineEntriesByMaxX);
	}

	private bool TryFindLineSweepCharacter<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TPredicate predicate, out TowerDefenseCharacter match, out bool found, System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> intervals, System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> intervalsByMaxX, List<LineSweepEntry> allLineEntries, List<LineSweepEntry> allLineEntriesByMaxX) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		EnsureLineSweepIndex(Engine.GetPhysicsFrames(), line);
		match = null;
		found = false;
		GetLineSweepHorizontalBounds(checkRect, out var left, out var right);
		int scannedCount = 0;
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		if (intervals.TryGetValue(line, out var value) && ScanLineSweepPredicateEntries(value, GetLineSweepMaxEntries(intervalsByMaxX, line), left, right, line, skipSameLineAllLineEntry: false, checkRect, ref predicate, ref scannedCount, ref candidateCount, ref checkedCount, ref hitCount, out match))
		{
			found = true;
		}
		else if (includeAllLineCheck && ScanLineSweepPredicateEntries(allLineEntries, allLineEntriesByMaxX, left, right, line, skipSameLineAllLineEntry: true, checkRect, ref predicate, ref scannedCount, ref candidateCount, ref checkedCount, ref hitCount, out match))
		{
			found = true;
		}
		EndLineSweepRectQuery(perfStart, scannedCount, candidateCount, checkedCount, hitCount, "registry.rectQuery.find");
		return true;
	}

	private void RebuildLineSweepFrame(ulong currentFrame)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		_lineSweepFrame = (long)currentFrame;
		ClearLineSweepFrameCaches();
		List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
		FillLineSweepAllLineEntries(cleanCharactersList);
		SortLineSweepAllLineEntries();
		TowerDefensePerfProfiler.End("registry.lineSweepBuild", startTicks, cleanCharactersList.Count);
	}

	private void ClearLineSweepFrameCaches()
	{
		ClearLineSweepLists(_lineSweepIntervals);
		ClearLineSweepLists(_lineSweepIntervalsByMaxX);
		_lineSweepAllLineEntries.Clear();
		_lineSweepAllLineEntriesByMaxX.Clear();
		ClearLineSweepLists(_lineSweepTallIntervals);
		ClearLineSweepLists(_lineSweepTallIntervalsByMaxX);
		_lineSweepTallAllLineEntries.Clear();
		_lineSweepTallAllLineEntriesByMaxX.Clear();
		_lineSweepBuiltLines.Clear();
	}

	private void EnsureLineSweepLineIndex(int line)
	{
		if (_lineSweepBuiltLines.Add(line))
		{
			long startTicks = TowerDefensePerfProfiler.Begin();
			List<TowerDefenseCharacter> lineCharactersList = GetLineCharactersList(line);
			BuildLineSweepLineEntries(line, lineCharactersList, out var entries, out var entriesByMaxX, out var tallEntries, out var tallEntriesByMaxX);
			SortLineSweepLineEntries(entries, entriesByMaxX, tallEntries, tallEntriesByMaxX);
			TowerDefensePerfProfiler.End("registry.lineSweepLineBuild", startTicks, lineCharactersList.Count);
			TowerDefensePerfProfiler.End("registry.lineSweepBuild", startTicks, lineCharactersList.Count);
		}
	}

	private void BuildLineSweepLineEntries(int line, List<TowerDefenseCharacter> lineCharacters, out List<LineSweepEntry> entries, out List<LineSweepEntry> entriesByMaxX, out List<LineSweepEntry> tallEntries, out List<LineSweepEntry> tallEntriesByMaxX)
	{
		entries = null;
		entriesByMaxX = null;
		tallEntries = null;
		tallEntriesByMaxX = null;
		for (int i = 0; i < lineCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = lineCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				LineSweepEntry item = new LineSweepEntry(towerDefenseCharacter, towerDefenseCharacter.WorldBroadphaseRect);
				EnsureLineSweepLineEntryLists(line, ref entries, ref entriesByMaxX);
				entries.Add(item);
				entriesByMaxX.Add(item);
				if (towerDefenseCharacter.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
				{
					EnsureLineSweepLineEntryLists(line, ref tallEntries, ref tallEntriesByMaxX, tallOnly: true);
					tallEntries.Add(item);
					tallEntriesByMaxX.Add(item);
				}
			}
		}
	}

	private void EnsureLineSweepLineEntryLists(int line, ref List<LineSweepEntry> entries, ref List<LineSweepEntry> entriesByMaxX, bool tallOnly = false)
	{
		if (entries == null)
		{
			entries = GetOrCreateLineSweepList(tallOnly ? _lineSweepTallIntervals : _lineSweepIntervals, line);
			entriesByMaxX = GetOrCreateLineSweepList(tallOnly ? _lineSweepTallIntervalsByMaxX : _lineSweepIntervalsByMaxX, line);
		}
	}

	private void SortLineSweepLineEntries(List<LineSweepEntry> entries, List<LineSweepEntry> entriesByMaxX, List<LineSweepEntry> tallEntries, List<LineSweepEntry> tallEntriesByMaxX)
	{
		if (entries != null)
		{
			entries.Sort(CompareLineSweepEntry);
			entriesByMaxX.Sort(CompareLineSweepEntryMaxX);
		}
		if (tallEntries != null)
		{
			tallEntries.Sort(CompareLineSweepEntry);
			tallEntriesByMaxX.Sort(CompareLineSweepEntryMaxX);
		}
	}

	private static void ClearLineSweepLists(System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> lists)
	{
		foreach (List<LineSweepEntry> value in lists.Values)
		{
			value.Clear();
		}
	}

	private static List<LineSweepEntry> GetOrCreateLineSweepList(System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> lists, int line)
	{
		if (!lists.TryGetValue(line, out var value))
		{
			value = (lists[line] = new List<LineSweepEntry>());
		}
		return value;
	}

	private List<LineSweepEntry> GetLineSweepMaxEntries(int line)
	{
		return GetLineSweepMaxEntries(_lineSweepIntervalsByMaxX, line);
	}

	private List<LineSweepEntry> GetLineSweepMaxEntries(System.Collections.Generic.Dictionary<int, List<LineSweepEntry>> intervalsByMaxX, int line)
	{
		if (!intervalsByMaxX.TryGetValue(line, out var value))
		{
			return null;
		}
		return value;
	}

	private void FillLineSweepScanBufferByMaxX(List<LineSweepEntry> entriesByMaxX, int start, float left, float right, int line, bool skipSameLineAllLineEntry, ref int scannedCount)
	{
		_lineSweepScanBuffer.Clear();
		for (int i = start; i < entriesByMaxX.Count; i++)
		{
			scannedCount++;
			LineSweepEntry lineSweepEntry = entriesByMaxX[i];
			if (CanUseLineSweepMaxScanEntry(lineSweepEntry, left, right, line, skipSameLineAllLineEntry))
			{
				_lineSweepScanBuffer.Add(lineSweepEntry);
			}
		}
		_lineSweepScanBuffer.Sort(CompareLineSweepEntry);
	}

	private static bool CanUseLineSweepMaxScanEntry(LineSweepEntry entry, float left, float right, int line, bool skipSameLineAllLineEntry)
	{
		if (entry.MaxX < left || entry.MinX > right)
		{
			return false;
		}
		TowerDefenseCharacter character = entry.Character;
		if (GodotObject.IsInstanceValid(character))
		{
			if (skipSameLineAllLineEntry)
			{
				return !character.IsTargetableFromLine(line, includeAllLineCheck: false);
			}
			return true;
		}
		return false;
	}

	private bool ScanLineSweepPredicateEntries<TPredicate>(List<LineSweepEntry> entries, List<LineSweepEntry> entriesByMaxX, float left, float right, int line, bool skipSameLineAllLineEntry, Rect2 checkRect, ref TPredicate predicate, ref int scannedCount, ref int candidateCount, ref int checkedCount, ref int hitCount, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		int minScanEnd;
		int maxScanStart;
		bool flag = ChooseLineSweepScanByMaxX(entries, entriesByMaxX, left, right, out minScanEnd, out maxScanStart);
		if (flag)
		{
			FillLineSweepScanBufferByMaxX(entriesByMaxX, maxScanStart, left, right, line, skipSameLineAllLineEntry, ref scannedCount);
			entries = _lineSweepScanBuffer;
			minScanEnd = entries.Count;
		}
		for (int i = 0; i < minScanEnd; i++)
		{
			LineSweepEntry lineSweepEntry = entries[i];
			if (!flag)
			{
				scannedCount++;
			}
			if (lineSweepEntry.MinX > right)
			{
				break;
			}
			if (lineSweepEntry.MaxX < left)
			{
				continue;
			}
			TowerDefenseCharacter character = lineSweepEntry.Character;
			if (!GodotObject.IsInstanceValid(character) || (skipSameLineAllLineEntry && character.IsTargetableFromLine(line, includeAllLineCheck: false)))
			{
				continue;
			}
			candidateCount++;
			if (!predicate.CanConsider(character))
			{
				continue;
			}
			checkedCount++;
			if (AabbShapeUtil.Intersects(checkRect, character.WorldHitRect))
			{
				hitCount++;
				if (predicate.Matches(character))
				{
					match = character;
					return true;
				}
			}
		}
		return false;
	}

	private static void EndLineSweepRectQuery(long perfStart, int scannedCount, int candidateCount, int checkedCount, int hitCount, string operationMetric)
	{
		TowerDefensePerfProfiler.Sample("registry.lineSweepScanned", scannedCount);
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", candidateCount);
		TowerDefensePerfProfiler.Sample("registry.lineSweepCandidates", candidateCount);
		TowerDefensePerfProfiler.Sample("registry.rectHits", hitCount);
		TowerDefensePerfProfiler.End("registry.rectQuery.lineSweep", perfStart, checkedCount);
		TowerDefensePerfProfiler.End(operationMetric, perfStart, checkedCount);
		TowerDefensePerfProfiler.End("registry.rectQuery", perfStart, checkedCount);
	}

	private static void EndLineDirectRectQuery(long perfStart, int checkedCount, int hitCount)
	{
		TowerDefensePerfProfiler.Sample("registry.rectHits", hitCount);
		TowerDefensePerfProfiler.End("registry.rectQuery.lineDirect", perfStart, checkedCount);
		TowerDefensePerfProfiler.End("registry.rectQuery.find", perfStart, checkedCount);
		TowerDefensePerfProfiler.End("registry.rectQuery", perfStart, checkedCount);
	}

	private bool ChooseLineSweepScanByMaxX(List<LineSweepEntry> entriesByMinX, List<LineSweepEntry> entriesByMaxX, float left, float right, out int minScanEnd, out int maxScanStart)
	{
		minScanEnd = ((entriesByMinX != null) ? UpperBoundLineSweepMinX(entriesByMinX, right) : 0);
		maxScanStart = ((entriesByMaxX != null) ? LowerBoundLineSweepMaxX(entriesByMaxX, left) : 0);
		if (entriesByMinX == null || entriesByMaxX == null || entriesByMinX.Count == 0 || entriesByMaxX.Count != entriesByMinX.Count)
		{
			return false;
		}
		int num = minScanEnd;
		return entriesByMaxX.Count - maxScanStart < num;
	}

	private bool TrySelectLineSweepCharacter<TSelector>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TSelector selector, out TowerDefenseCharacter match, out bool found) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		long perfStart = TowerDefensePerfProfiler.Begin();
		EnsureLineSweepIndex(Engine.GetPhysicsFrames(), line);
		match = null;
		found = false;
		GetLineSweepHorizontalBounds(checkRect, out var left, out var right);
		int scannedCount = 0;
		int candidateCount = 0;
		int checkedCount = 0;
		int hitCount = 0;
		if ((!_lineSweepIntervals.TryGetValue(line, out var value) || !ScanLineSweepSelectorEntries(value, GetLineSweepMaxEntries(line), left, right, line, skipSameLineAllLineEntry: false, checkRect, ref selector, ref scannedCount, ref candidateCount, ref checkedCount, ref hitCount, ref match)) && includeAllLineCheck)
		{
			ScanLineSweepSelectorEntries(_lineSweepAllLineEntries, _lineSweepAllLineEntriesByMaxX, left, right, line, skipSameLineAllLineEntry: true, checkRect, ref selector, ref scannedCount, ref candidateCount, ref checkedCount, ref hitCount, ref match);
		}
		found = GodotObject.IsInstanceValid(match);
		EndLineSweepRectQuery(perfStart, scannedCount, candidateCount, checkedCount, hitCount, "registry.rectQuery.select");
		return true;
	}

	private bool ScanLineSweepSelectorEntries<TSelector>(List<LineSweepEntry> entries, List<LineSweepEntry> entriesByMaxX, float left, float right, int line, bool skipSameLineAllLineEntry, Rect2 checkRect, ref TSelector selector, ref int scannedCount, ref int candidateCount, ref int checkedCount, ref int hitCount, ref TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		int minScanEnd;
		int maxScanStart;
		bool flag = ChooseLineSweepScanByMaxX(entries, entriesByMaxX, left, right, out minScanEnd, out maxScanStart);
		if (flag)
		{
			FillLineSweepScanBufferByMaxX(entriesByMaxX, maxScanStart, left, right, line, skipSameLineAllLineEntry, ref scannedCount);
			entries = _lineSweepScanBuffer;
			minScanEnd = entries.Count;
		}
		for (int i = 0; i < minScanEnd; i++)
		{
			LineSweepEntry lineSweepEntry = entries[i];
			if (!flag)
			{
				scannedCount++;
			}
			if (lineSweepEntry.MinX > right)
			{
				break;
			}
			if (lineSweepEntry.MaxX < left)
			{
				continue;
			}
			TowerDefenseCharacter character = lineSweepEntry.Character;
			if (!GodotObject.IsInstanceValid(character) || (skipSameLineAllLineEntry && character.IsTargetableFromLine(line, includeAllLineCheck: false)))
			{
				continue;
			}
			candidateCount++;
			if (!selector.CanConsider(character))
			{
				continue;
			}
			checkedCount++;
			if (AabbShapeUtil.Intersects(checkRect, character.WorldHitRect))
			{
				hitCount++;
				if (selector.Visit(character, ref match))
				{
					return true;
				}
			}
		}
		return false;
	}

	private List<TowerDefenseCharacter> GetCharactersOutsideCampWithConfigNameList(TowerDefenseEnum.CHARACTER_CAMP camp, string configName)
	{
		EnsureCharacterClassificationCounts();
		string text = configName ?? string.Empty;
		(TowerDefenseEnum.CHARACTER_CAMP, string) key = (camp, text);
		if (_outsideCampSameConfigQueryCache.TryGetValue(key, out var value))
		{
			return value;
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(Math.Max(0, _characterCountByConfigName.GetValueOrDefault(text) - _characterCountByCampAndConfigName.GetValueOrDefault(key)));
		for (int i = 0; i < _activeCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _activeCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.camp != camp && string.Equals(towerDefenseCharacter.config?.name ?? string.Empty, text, StringComparison.Ordinal))
			{
				list.Add(towerDefenseCharacter);
			}
		}
		_outsideCampSameConfigQueryCache[key] = list;
		return list;
	}

	public bool TrySelectCharacterIntersectingRectClassified<TSelector>(Rect2 checkRect, TowerDefenseEnum.CHARACTER_CAMP ownerCamp, string ownerConfigName, bool includeOutsideCampSameConfig, bool includeSameCampDifferentConfig, ref TSelector selector, out TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		match = null;
		List<TowerDefenseCharacter> obj = (includeOutsideCampSameConfig ? GetCharactersOutsideCampWithConfigNameList(ownerCamp, ownerConfigName) : EmptyCharacterList);
		List<TowerDefenseCharacter> list = (includeSameCampDifferentConfig ? GetCharactersInCampWithDifferentConfigNameList(ownerCamp, ownerConfigName) : EmptyCharacterList);
		int items = obj.Count + list.Count;
		int consideredCount = 0;
		int hitCount = 0;
		if (!ScanClassifiedRectSelectorCandidates(obj, checkRect, ref selector, ref consideredCount, ref hitCount, ref match))
		{
			ScanClassifiedRectSelectorCandidates(list, checkRect, ref selector, ref consideredCount, ref hitCount, ref match);
		}
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", items);
		TowerDefensePerfProfiler.Sample("registry.rectConsidered", consideredCount);
		TowerDefensePerfProfiler.Sample("registry.rectHits", hitCount);
		TowerDefensePerfProfiler.End("registry.rectQuery.classified", startTicks, items);
		TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, items);
		return GodotObject.IsInstanceValid(match);
	}

	private static bool ScanClassifiedRectSelectorCandidates<TSelector>(List<TowerDefenseCharacter> candidates, Rect2 checkRect, ref TSelector selector, ref int consideredCount, ref int hitCount, ref TowerDefenseCharacter match) where TSelector : struct, ITowerDefenseCharacterRectSelector
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !selector.CanConsider(towerDefenseCharacter))
			{
				continue;
			}
			consideredCount++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				hitCount++;
				if (selector.Visit(towerDefenseCharacter, ref match))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void FillCharactersIntersectingRectDirectListExcludingCamp(Rect2 checkRect, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, List<TowerDefenseCharacter> result, int line = -2147483648, bool includeAllLineCheck = false)
	{
		if (result == null)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		result.Clear();
		int num = 0;
		for (int i = 0; i < _activeCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _activeCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.camp != excludedCamp && (line == -2147483648 || towerDefenseCharacter.IsTargetableFromLine(line, includeAllLineCheck)))
			{
				num++;
				if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
				{
					result.Add(towerDefenseCharacter);
				}
			}
		}
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", num);
		TowerDefensePerfProfiler.Sample("registry.rectHits", result.Count);
		TowerDefensePerfProfiler.End("registry.rectQuery.directCamp", startTicks, num);
		TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, num);
	}

	public void FillCharactersIntersectingRectDirectList(Rect2 checkRect, List<TowerDefenseCharacter> result, int line = -2147483648, bool includeAllLineCheck = false)
	{
		if (result == null)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		result.Clear();
		int num = 0;
		for (int i = 0; i < _activeCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _activeCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && (line == -2147483648 || towerDefenseCharacter.IsTargetableFromLine(line, includeAllLineCheck)))
			{
				num++;
				if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
				{
					result.Add(towerDefenseCharacter);
				}
			}
		}
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", num);
		TowerDefensePerfProfiler.Sample("registry.rectHits", result.Count);
		TowerDefensePerfProfiler.End("registry.rectQuery.direct", startTicks, num);
		TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, num);
	}

	public bool TryFindCharacterIntersectingRectExcludingCamp<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		if (line != -2147483648 && TryFindLineDirectCharacter(checkRect, line, includeAllLineCheck, ref predicate, out match, out var found, excludedCamp))
		{
			return found;
		}
		long perfStart = TowerDefensePerfProfiler.Begin();
		List<TowerDefenseCharacter> source;
		string sourceMetric;
		if (line == -2147483648 && TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, excludedCamp))
		{
			source = _spatialCandidateBuffer;
			sourceMetric = "registry.rectQuery.spatialCamp";
		}
		else
		{
			source = GetRectQuerySource(checkRect, line, includeAllLineCheck, out sourceMetric);
		}
		return TryFindCharacterInRectSourceExcludingCamp(source, checkRect, excludedCamp, ref predicate, out match, perfStart, sourceMetric);
	}

	private static bool TryFindCharacterInRectSourceExcludingCamp<TPredicate>(List<TowerDefenseCharacter> source, Rect2 checkRect, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, out TowerDefenseCharacter match, long perfStart, string sourceMetric) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		int num = 0;
		int num2 = 0;
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", source.Count);
		for (int i = 0; i < source.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = source[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || towerDefenseCharacter.camp == excludedCamp || !predicate.CanConsider(towerDefenseCharacter))
			{
				continue;
			}
			num++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				num2++;
				if (predicate.Matches(towerDefenseCharacter))
				{
					match = towerDefenseCharacter;
					EndRectFindMetrics(sourceMetric, perfStart, num, num2);
					return true;
				}
			}
		}
		EndRectFindMetrics(sourceMetric, perfStart, num, num2);
		return false;
	}

	private static void EndRectFindMetrics(string sourceMetric, long perfStart, int checkedCount, int hitCount)
	{
		TowerDefensePerfProfiler.Sample("registry.rectHits", hitCount);
		TowerDefensePerfProfiler.End(sourceMetric, perfStart, checkedCount);
		TowerDefensePerfProfiler.End("registry.rectQuery.find", perfStart, checkedCount);
		TowerDefensePerfProfiler.End("registry.rectQuery", perfStart, checkedCount);
	}

	public bool TryFindCharacterIntersectingRect<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TPredicate predicate, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		if (line != -2147483648 && TryFindLineDirectCharacter(checkRect, line, includeAllLineCheck, ref predicate, out match, out var found))
		{
			return found;
		}
		long perfStart = TowerDefensePerfProfiler.Begin();
		string sourceMetric;
		return TryFindCharacterInRectSource(GetRectQuerySource(checkRect, line, includeAllLineCheck, out sourceMetric), checkRect, ref predicate, out match, perfStart, sourceMetric);
	}

	private static bool TryFindCharacterInRectSource<TPredicate>(List<TowerDefenseCharacter> source, Rect2 checkRect, ref TPredicate predicate, out TowerDefenseCharacter match, long perfStart, string sourceMetric) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		int num = 0;
		int num2 = 0;
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", source.Count);
		for (int i = 0; i < source.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = source[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !predicate.CanConsider(towerDefenseCharacter))
			{
				continue;
			}
			num++;
			if (AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				num2++;
				if (predicate.Matches(towerDefenseCharacter))
				{
					match = towerDefenseCharacter;
					EndRectFindMetrics(sourceMetric, perfStart, num, num2);
					return true;
				}
			}
		}
		EndRectFindMetrics(sourceMetric, perfStart, num, num2);
		return false;
	}

	public void FillCharactersForRectGridWindowList(Rect2 checkRect, List<TowerDefenseCharacter> result, int line = -2147483648, bool includeAllLineCheck = false, bool clampToMapPadding = true)
	{
		if (result == null)
		{
			return;
		}
		result.Clear();
		if (!TryGetColumnsForRect(checkRect, out var minColumn, out var maxColumn, clampToMapPadding))
		{
			List<TowerDefenseCharacter> list = ((line == -2147483648) ? GetCleanCharactersList() : (includeAllLineCheck ? GetCharactersForLineList(line) : GetLineCharactersList(line)));
			for (int i = 0; i < list.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = list[i];
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					result.Add(towerDefenseCharacter);
				}
			}
			return;
		}
		bool flag = false;
		int minLine = 0;
		int maxLine = 0;
		if (line != -2147483648)
		{
			flag = true;
			minLine = line;
			maxLine = line;
		}
		else if (TryGetRowsForRect(checkRect, out minLine, out maxLine))
		{
			flag = true;
		}
		for (int j = minColumn; j <= maxColumn; j++)
		{
			List<TowerDefenseCharacter> columnCharactersList = GetColumnCharactersList(j);
			for (int k = 0; k < columnCharactersList.Count; k++)
			{
				TowerDefenseCharacter towerDefenseCharacter2 = columnCharactersList[k];
				if (GodotObject.IsInstanceValid(towerDefenseCharacter2) && (!flag || towerDefenseCharacter2.IsTargetableFromLineRange(minLine, maxLine, includeAllLineCheck)))
				{
					result.Add(towerDefenseCharacter2);
				}
			}
		}
	}

	public void FillCharactersIntersectingRectListExcludingCamp(Rect2 checkRect, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, List<TowerDefenseCharacter> result, int line = -2147483648, bool includeAllLineCheck = false)
	{
		if (result == null)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		result.Clear();
		List<TowerDefenseCharacter> list;
		string sourceMetric;
		if (line != -2147483648)
		{
			list = (includeAllLineCheck ? GetCharactersForLineListExcludingCamp(line, excludedCamp) : GetLineCharactersListExcludingCamp(line, excludedCamp));
			sourceMetric = "registry.rectQuery.lineCamp";
		}
		else if (TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, excludedCamp))
		{
			list = _spatialCandidateBuffer;
			sourceMetric = "registry.rectQuery.spatialCamp";
		}
		else
		{
			list = GetRectQuerySource(checkRect, line, includeAllLineCheck, out sourceMetric);
		}
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = list[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.camp != excludedCamp && AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				result.Add(towerDefenseCharacter);
			}
		}
		TowerDefensePerfProfiler.Sample("registry.rectHits", result.Count);
		TowerDefensePerfProfiler.End(sourceMetric, startTicks, list.Count);
		TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, list.Count);
	}

	public void FillCharactersIntersectingRectList(Rect2 checkRect, List<TowerDefenseCharacter> result, int line = -2147483648, bool includeAllLineCheck = false)
	{
		if (result == null)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		result.Clear();
		string sourceMetric;
		List<TowerDefenseCharacter> rectQuerySource = GetRectQuerySource(checkRect, line, includeAllLineCheck, out sourceMetric);
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", rectQuerySource.Count);
		for (int i = 0; i < rectQuerySource.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = rectQuerySource[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				result.Add(towerDefenseCharacter);
			}
		}
		TowerDefensePerfProfiler.Sample("registry.rectHits", result.Count);
		TowerDefensePerfProfiler.End(sourceMetric, startTicks, rectQuerySource.Count);
		TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, rectQuerySource.Count);
	}

	public void FillCharactersIntersectingRectListForCamp(Rect2 checkRect, TowerDefenseEnum.CHARACTER_CAMP includedCamp, List<TowerDefenseCharacter> result, int line = -2147483648, bool includeAllLineCheck = false)
	{
		if (result == null)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		result.Clear();
		List<TowerDefenseCharacter> list;
		string sourceMetric;
		if (line == -2147483648 && TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, GetSpatialBucketsForIncludedCamp(includedCamp), GetSpatialAllLineCharactersForIncludedCamp(includedCamp)))
		{
			list = _spatialCandidateBuffer;
			sourceMetric = "registry.rectQuery.spatialCamp";
		}
		else
		{
			list = GetRectQuerySource(checkRect, line, includeAllLineCheck, out sourceMetric);
		}
		TowerDefensePerfProfiler.Sample("registry.rectCandidates", list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = list[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.camp == includedCamp && AabbShapeUtil.Intersects(checkRect, towerDefenseCharacter.WorldHitRect))
			{
				result.Add(towerDefenseCharacter);
			}
		}
		TowerDefensePerfProfiler.Sample("registry.rectHits", result.Count);
		TowerDefensePerfProfiler.End(sourceMetric, startTicks, list.Count);
		TowerDefensePerfProfiler.End("registry.rectQuery", startTicks, list.Count);
	}

	public List<TowerDefenseCharacter> GetCharactersIntersectingRectList(Rect2 checkRect, int line = -2147483648, bool includeAllLineCheck = false)
	{
		FillCharactersIntersectingRectList(checkRect, _rectQueryBuffer, line, includeAllLineCheck);
		return _rectQueryBuffer;
	}

	public List<TowerDefenseCharacter> GetCharactersIntersectingRectListExcludingCamp(Rect2 checkRect, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, int line = -2147483648, bool includeAllLineCheck = false)
	{
		FillCharactersIntersectingRectListExcludingCamp(checkRect, excludedCamp, _rectQueryBuffer, line, includeAllLineCheck);
		return _rectQueryBuffer;
	}

	public List<TowerDefenseCharacter> GetCharactersIntersectingRectListForCamp(Rect2 checkRect, TowerDefenseEnum.CHARACTER_CAMP includedCamp, int line = -2147483648, bool includeAllLineCheck = false)
	{
		FillCharactersIntersectingRectListForCamp(checkRect, includedCamp, _rectQueryBuffer, line, includeAllLineCheck);
		return _rectQueryBuffer;
	}

	private List<TowerDefenseCharacter> GetRectQuerySource(Rect2 checkRect, int line, bool includeAllLineCheck, out string sourceMetric)
	{
		if (line != -2147483648)
		{
			sourceMetric = "registry.rectQuery.lineDirect";
			if (!includeAllLineCheck)
			{
				return GetLineCharactersList(line);
			}
			return GetCharactersForLineList(line);
		}
		if (TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck))
		{
			sourceMetric = "registry.rectQuery.spatial";
			return _spatialCandidateBuffer;
		}
		if (line == -2147483648)
		{
			if (TryGetRowsForRect(checkRect, out var minLine, out var maxLine))
			{
				sourceMetric = "registry.rectQuery.rows";
				return GetCharactersForRowsList(minLine, maxLine);
			}
			sourceMetric = "registry.rectQuery.clean";
			return GetCleanCharactersList();
		}
		sourceMetric = "registry.rectQuery.lineFallback";
		if (!includeAllLineCheck)
		{
			return GetLineCharactersList(line);
		}
		return GetCharactersForLineList(line);
	}

	public bool TryFindTallCharacterIntersectingRect<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, ref TPredicate predicate, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		if (line != -2147483648 && TryFindLineDirectCharacter(checkRect, line, includeAllLineCheck, ref predicate, out match, out var found))
		{
			return found;
		}
		return TryFindCharacterIntersectingRect(checkRect, line, includeAllLineCheck, ref predicate, out match);
	}

	public bool TryFindTallCharacterIntersectingRectExcludingCamp<TPredicate>(Rect2 checkRect, int line, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp, ref TPredicate predicate, out TowerDefenseCharacter match) where TPredicate : struct, ITowerDefenseCharacterRectPredicate
	{
		match = null;
		if (line != -2147483648 && TryFindLineDirectCharacter(checkRect, line, includeAllLineCheck, ref predicate, out match, out var found, excludedCamp))
		{
			return found;
		}
		return TryFindCharacterIntersectingRectExcludingCamp(checkRect, line, includeAllLineCheck, excludedCamp, ref predicate, out match);
	}

	private List<TowerDefenseCharacter> GetCharactersForRowsList(int minLine, int maxLine)
	{
		EnsureLineCombinedCache(Engine.GetPhysicsFrames());
		_rowsQueryBuffer.Clear();
		_rowsQuerySeen.Clear();
		for (int i = minLine; i <= maxLine; i++)
		{
			if (!_lineCombinedCache.TryGetValue(i, out var value))
			{
				continue;
			}
			for (int j = 0; j < value.Count; j++)
			{
				TowerDefenseCharacter item = value[j];
				if (_rowsQuerySeen.Add(item))
				{
					_rowsQueryBuffer.Add(item);
				}
			}
		}
		for (int k = 0; k < _allLineCheckChars.Count; k++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _allLineCheckChars[k];
			if (!towerDefenseCharacter.IsTargetableFromLineRange(minLine, maxLine, includeAllLineCheck: false) && _rowsQuerySeen.Add(towerDefenseCharacter))
			{
				_rowsQueryBuffer.Add(towerDefenseCharacter);
			}
		}
		return _rowsQueryBuffer;
	}

	private bool TryGetRowsForRect(Rect2 checkRect, out int minLine, out int maxLine)
	{
		minLine = 0;
		maxLine = 0;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2 gridSize = instance.gridSize;
		Vector2 gridBeginPos = instance.gridBeginPos;
		Vector2I gridNum = instance.gridNum;
		if (gridNum.Y <= 0 || gridSize.Y <= 0f)
		{
			return false;
		}
		Vector2 vector = checkRect.Position + checkRect.Size;
		float num = Mathf.Min(checkRect.Position.Y, vector.Y);
		float num2 = Mathf.Max(checkRect.Position.Y, vector.Y);
		minLine = Mathf.FloorToInt((num - gridBeginPos.Y) / gridSize.Y);
		maxLine = Mathf.FloorToInt((num2 - gridBeginPos.Y) / gridSize.Y) + 2;
		minLine = Mathf.Clamp(minLine, 1, gridNum.Y);
		maxLine = Mathf.Clamp(maxLine, 1, gridNum.Y);
		if (minLine > maxLine)
		{
			return false;
		}
		return maxLine - minLine + 1 < gridNum.Y;
	}

	private List<TowerDefenseCharacter> GetCharactersInCampWithDifferentConfigNameList(TowerDefenseEnum.CHARACTER_CAMP camp, string configName)
	{
		EnsureCharacterClassificationCounts();
		string text = configName ?? string.Empty;
		(TowerDefenseEnum.CHARACTER_CAMP, string) key = (camp, text);
		if (_sameCampDifferentConfigQueryCache.TryGetValue(key, out var value))
		{
			return value;
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(Math.Max(0, _characterCountByCamp.GetValueOrDefault(camp) - _characterCountByCampAndConfigName.GetValueOrDefault(key)));
		for (int i = 0; i < _activeCharacters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _activeCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.camp == camp && !string.Equals(towerDefenseCharacter.config?.name ?? string.Empty, text, StringComparison.Ordinal))
			{
				list.Add(towerDefenseCharacter);
			}
		}
		_sameCampDifferentConfigQueryCache[key] = list;
		return list;
	}

	private System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> GetSpatialBucketsForExcludedCamp(TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		return excludedCamp switch
		{
			TowerDefenseEnum.CHARACTER_CAMP.PLANT => _spatialNonPlantBuckets, 
			TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE => _spatialNonZombieBuckets, 
			_ => _spatialBuckets, 
		};
	}

	private List<TowerDefenseCharacter> GetSpatialAllLineCharactersForExcludedCamp(TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		return excludedCamp switch
		{
			TowerDefenseEnum.CHARACTER_CAMP.PLANT => _spatialNonPlantAllLineCheckChars, 
			TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE => _spatialNonZombieAllLineCheckChars, 
			_ => _spatialAllLineCheckChars, 
		};
	}

	private System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> GetSpatialBucketsForIncludedCamp(TowerDefenseEnum.CHARACTER_CAMP includedCamp)
	{
		return includedCamp switch
		{
			TowerDefenseEnum.CHARACTER_CAMP.PLANT => _spatialNonZombieBuckets, 
			TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE => _spatialNonPlantBuckets, 
			_ => _spatialBuckets, 
		};
	}

	private List<TowerDefenseCharacter> GetSpatialAllLineCharactersForIncludedCamp(TowerDefenseEnum.CHARACTER_CAMP includedCamp)
	{
		return includedCamp switch
		{
			TowerDefenseEnum.CHARACTER_CAMP.PLANT => _spatialNonZombieAllLineCheckChars, 
			TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE => _spatialNonPlantAllLineCheckChars, 
			_ => _spatialAllLineCheckChars, 
		};
	}

	private static void ClearSpatialBuckets(System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> buckets)
	{
		foreach (List<TowerDefenseCharacter> value in buckets.Values)
		{
			value.Clear();
		}
	}

	private static void AddSpatialCoverage(System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> buckets, TowerDefenseCharacter character, SpatialCoverage coverage)
	{
		for (int i = coverage.MinLine; i <= coverage.MaxLine; i++)
		{
			for (int j = coverage.MinColumn; j <= coverage.MaxColumn; j++)
			{
				long key = BucketKey(i, j);
				if (!buckets.TryGetValue(key, out var value))
				{
					value = (buckets[key] = new List<TowerDefenseCharacter>());
				}
				value.Add(character);
			}
		}
	}

	private void EnsureSpatialBuckets(ulong currentFrame)
	{
		if (_spatialBucketsFrame >= 0)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.Begin();
		_spatialBucketsFrame = (long)currentFrame;
		ClearSpatialBuckets(_spatialBuckets);
		ClearSpatialBuckets(_spatialNonPlantBuckets);
		ClearSpatialBuckets(_spatialNonZombieBuckets);
		_spatialAllLineCheckChars.Clear();
		_spatialNonPlantAllLineCheckChars.Clear();
		_spatialNonZombieAllLineCheckChars.Clear();
		_spatialCoverageByCharacter.Clear();
		List<TowerDefenseCharacter> cleanCharactersList = GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !TryResolveSpatialCoverage(towerDefenseCharacter, out var coverage))
			{
				continue;
			}
			SpatialCoverage spatialCoverage = ExpandSpatialCoverageForReuse(coverage);
			_spatialCoverageByCharacter[towerDefenseCharacter] = spatialCoverage;
			AddSpatialCoverage(_spatialBuckets, towerDefenseCharacter, spatialCoverage);
			if (towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				AddSpatialCoverage(_spatialNonPlantBuckets, towerDefenseCharacter, spatialCoverage);
			}
			if (towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
			{
				AddSpatialCoverage(_spatialNonZombieBuckets, towerDefenseCharacter, spatialCoverage);
			}
			if (towerDefenseCharacter.targetRegistrationComponent.allLineCheck)
			{
				_spatialAllLineCheckChars.Add(towerDefenseCharacter);
				if (towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
				{
					_spatialNonPlantAllLineCheckChars.Add(towerDefenseCharacter);
				}
				if (towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
				{
					_spatialNonZombieAllLineCheckChars.Add(towerDefenseCharacter);
				}
			}
		}
		TowerDefensePerfProfiler.End("registry.spatialBuild", startTicks, cleanCharactersList.Count);
	}

	private static long BucketKey(int line, int column)
	{
		return ((long)line << 32) ^ (uint)column;
	}

	private void AddSpatialCandidates(List<TowerDefenseCharacter> candidates)
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && _spatialCandidateSeen.Add(towerDefenseCharacter))
			{
				_spatialCandidateBuffer.Add(towerDefenseCharacter);
			}
		}
	}

	private bool TryFillSpatialCandidateBuffer(Rect2 checkRect, int line, bool includeAllLineCheck, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		return TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, GetSpatialBucketsForExcludedCamp(excludedCamp), GetSpatialAllLineCharactersForExcludedCamp(excludedCamp));
	}

	private bool TryFillSpatialCandidateBuffer(Rect2 checkRect, int line, bool includeAllLineCheck)
	{
		return TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, _spatialBuckets, _spatialAllLineCheckChars, Engine.GetPhysicsFrames());
	}

	private bool TryFillSpatialCandidateBuffer(Rect2 checkRect, int line, bool includeAllLineCheck, ulong currentFrame)
	{
		return TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, _spatialBuckets, _spatialAllLineCheckChars, currentFrame);
	}

	private bool TryFillSpatialCandidateBuffer(Rect2 checkRect, int line, bool includeAllLineCheck, System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> spatialBuckets, List<TowerDefenseCharacter> spatialAllLineCheckChars, ulong currentFrame)
	{
		if (!TryGetSpatialColumnsForRect(checkRect, out var minColumn, out var maxColumn))
		{
			return false;
		}
		int minLine;
		int maxLine;
		if (line == -2147483648)
		{
			if (!TryGetSpatialRowsForRect(checkRect, out minLine, out maxLine) && !TryGetFullLineRange(out minLine, out maxLine))
			{
				return false;
			}
		}
		else
		{
			minLine = line;
			maxLine = line;
		}
		EnsureSpatialBuckets(currentFrame);
		_spatialCandidateBuffer.Clear();
		_spatialCandidateSeen.Clear();
		for (int i = minLine; i <= maxLine; i++)
		{
			for (int j = minColumn; j <= maxColumn; j++)
			{
				if (spatialBuckets.TryGetValue(BucketKey(i, j), out var value))
				{
					AddSpatialCandidates(value);
				}
			}
		}
		if (includeAllLineCheck || line == -2147483648)
		{
			AddSpatialCandidates(spatialAllLineCheckChars);
		}
		return true;
	}

	private bool TryFillSpatialCandidateBuffer(Rect2 checkRect, int line, bool includeAllLineCheck, System.Collections.Generic.Dictionary<long, List<TowerDefenseCharacter>> spatialBuckets, List<TowerDefenseCharacter> spatialAllLineCheckChars)
	{
		return TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, spatialBuckets, spatialAllLineCheckChars, Engine.GetPhysicsFrames());
	}

	private bool CanReuseSpatialCoverageAfterGeometryChange(TowerDefenseCharacter character)
	{
		if (_spatialBucketsFrame < 0 || !_spatialCoverageByCharacter.TryGetValue(character, out var value) || !TryResolveSpatialCoverage(character, out var coverage))
		{
			return false;
		}
		return value.Contains(coverage);
	}

	private SpatialCoverage ExpandSpatialCoverageForReuse(SpatialCoverage coverage)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.gridNum.X <= 0 || instance.gridNum.Y <= 0)
		{
			return coverage;
		}
		return new SpatialCoverage(Math.Max(1, coverage.MinLine - 1), Math.Min(instance.gridNum.Y, coverage.MaxLine + 1), Math.Max(-1, coverage.MinColumn - 1), Math.Min(instance.gridNum.X + 1, coverage.MaxColumn + 1));
	}

	private bool TryResolveSpatialCoverage(TowerDefenseCharacter character, out SpatialCoverage coverage)
	{
		coverage = default(SpatialCoverage);
		if (!GodotObject.IsInstanceValid(character))
		{
			return false;
		}
		Rect2 worldBroadphaseRect = character.WorldBroadphaseRect;
		if (!TryGetSpatialRowsForRect(worldBroadphaseRect, out var minLine, out var maxLine))
		{
			minLine = character.gridPos.Y;
			maxLine = character.gridPos.Y;
		}
		if (!TryGetSpatialColumnsForRect(worldBroadphaseRect, out var minColumn, out var maxColumn))
		{
			minColumn = character.gridPos.X;
			maxColumn = character.gridPos.X;
		}
		coverage = new SpatialCoverage(minLine, maxLine, minColumn, maxColumn);
		return true;
	}

	private bool TryGetSpatialColumnsForRect(Rect2 checkRect, out int minColumn, out int maxColumn)
	{
		minColumn = 0;
		maxColumn = 0;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2 gridSize = instance.gridSize;
		Vector2 gridBeginPos = instance.gridBeginPos;
		Vector2I gridNum = instance.gridNum;
		if (gridNum.X <= 0 || gridSize.X <= 0f)
		{
			return false;
		}
		Vector2 vector = checkRect.Position + checkRect.Size;
		float num = Mathf.Min(checkRect.Position.X, vector.X);
		float num2 = Mathf.Max(checkRect.Position.X, vector.X);
		minColumn = Mathf.FloorToInt((num - gridBeginPos.X) / gridSize.X) + 1;
		maxColumn = Mathf.FloorToInt((num2 - gridBeginPos.X) / gridSize.X) + 1;
		minColumn = Mathf.Clamp(minColumn, -1, gridNum.X + 1);
		maxColumn = Mathf.Clamp(maxColumn, -1, gridNum.X + 1);
		return minColumn <= maxColumn;
	}

	private bool TryGetSpatialRowsForRect(Rect2 checkRect, out int minLine, out int maxLine)
	{
		minLine = 0;
		maxLine = 0;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2 gridSize = instance.gridSize;
		Vector2 gridBeginPos = instance.gridBeginPos;
		Vector2I gridNum = instance.gridNum;
		if (gridNum.Y <= 0 || gridSize.Y <= 0f)
		{
			return false;
		}
		Vector2 vector = checkRect.Position + checkRect.Size;
		float num = Mathf.Min(checkRect.Position.Y, vector.Y);
		float num2 = Mathf.Max(checkRect.Position.Y, vector.Y);
		minLine = Mathf.FloorToInt((num - gridBeginPos.Y) / gridSize.Y) + 1;
		maxLine = Mathf.FloorToInt((num2 - gridBeginPos.Y) / gridSize.Y) + 1;
		minLine = Mathf.Clamp(minLine, 1, gridNum.Y);
		maxLine = Mathf.Clamp(maxLine, 1, gridNum.Y);
		return minLine <= maxLine;
	}

	public void FillCharactersForWorldRectCandidatesList(Rect2 checkRect, List<TowerDefenseCharacter> result, int line = -2147483648, bool includeAllLineCheck = false)
	{
		FillCharactersForWorldRectCandidatesListForFrame(checkRect, result, line, includeAllLineCheck, Engine.GetPhysicsFrames());
	}

	internal void FillCharactersForWorldRectCandidatesListForFrame(Rect2 checkRect, List<TowerDefenseCharacter> result, int line, bool includeAllLineCheck, ulong currentFrame)
	{
		if (result == null)
		{
			return;
		}
		if (currentFrame > 9223372036854775807L)
		{
			currentFrame = Engine.GetPhysicsFrames();
		}
		result.Clear();
		if ((line != -2147483648) ? TryFillLineSweepCandidateBuffer(checkRect, line, includeAllLineCheck, result, currentFrame) : TryFillSpatialCandidateBuffer(checkRect, line, includeAllLineCheck, currentFrame))
		{
			if (line == -2147483648)
			{
				result.AddRange(_spatialCandidateBuffer);
			}
			return;
		}
		List<TowerDefenseCharacter> list = ((line == -2147483648) ? GetCleanCharactersList() : (includeAllLineCheck ? GetCharactersForLineList(line) : GetLineCharactersList(line)));
		for (int i = 0; i < list.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = list[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				result.Add(towerDefenseCharacter);
			}
		}
	}

	internal void VerifyLineSweepCandidatesForTest()
	{
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		List<LineSweepEntry> list2 = new List<LineSweepEntry>();
		List<TowerDefenseCharacter> list3 = new List<TowerDefenseCharacter>();
		try
		{
			for (int i = 0; i < 256; i++)
			{
				TowerDefensePlant towerDefensePlant = new TowerDefensePlant();
				towerDefensePlant.gridPos = new Vector2I(0, i % 2);
				list.Add(towerDefensePlant);
				list2.Add(new LineSweepEntry(towerDefensePlant, new Rect2(i * 10, 0f, 8f, 8f)));
			}
			List<LineSweepEntry> list4 = new List<LineSweepEntry>(list2);
			list4.Sort(CompareLineSweepEntryMaxX);
			VerifyCandidateRangeForTest(list2, list4, list3, 0f, 128f, skipSameLine: false, expectMaxScan: false);
			VerifyCandidateRangeForTest(list2, list4, list3, 2008f, 2400f, skipSameLine: false, expectMaxScan: true);
			VerifyCandidateRangeForTest(list2, list4, list3, 2008f, 2400f, skipSameLine: true, expectMaxScan: true);
			list[210].gridPos = new Vector2I(0, 1);
			VerifyCandidateRangeForTest(list2, list4, list3, 2008f, 2400f, skipSameLine: true, expectMaxScan: true);
			list[215].Free();
			VerifyCandidateRangeForTest(list2, list4, list3, 2008f, 2400f, skipSameLine: false, expectMaxScan: true);
			list2.Add(new LineSweepEntry(list[220], new Rect2(2201f, 0f, 8f, 8f)));
			list2.Sort(CompareLineSweepEntry);
			list4 = new List<LineSweepEntry>(list2);
			list4.Sort(CompareLineSweepEntryMaxX);
			VerifyCandidateRangeForTest(list2, list4, list3, 2008f, 2400f, skipSameLine: false, expectMaxScan: true);
			VerifyCandidateRangeForTest(list2, list4, list3, 3000f, 3010f, skipSameLine: false, expectMaxScan: true);
			for (int j = 0; j < 4; j++)
			{
				int scannedCount = 0;
				long timestamp = Stopwatch.GetTimestamp();
				for (int k = 0; k < 20000; k++)
				{
					list3.Clear();
					_spatialCandidateSeen.Clear();
					AddLineSweepCandidates(list2, list4, 1600f, 2550f, 0, skipSameLineAllLineEntry: false, list3, ref scannedCount);
				}
				GD.Print($"LINE_SWEEP_BENCH pass={j} calls=20000 ms={Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F3} hits={list3.Count} scanned={scannedCount}");
				if (list3.Count != 95)
				{
					throw new InvalidOperationException("压力循环候选数量错误。");
				}
			}
		}
		finally
		{
			foreach (TowerDefenseCharacter item in list)
			{
				if (GodotObject.IsInstanceValid(item))
				{
					item.Free();
				}
			}
		}
	}

	private void VerifyCandidateRangeForTest(List<LineSweepEntry> entries, List<LineSweepEntry> maxEntries, List<TowerDefenseCharacter> result, float left, float right, bool skipSameLine, bool expectMaxScan)
	{
		if (ChooseLineSweepScanByMaxX(entries, maxEntries, left, right, out var _, out var _) != expectMaxScan)
		{
			throw new InvalidOperationException("测试未覆盖预期扫描方向。");
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		foreach (LineSweepEntry entry in entries)
		{
			if (entry.MinX <= right && entry.MaxX >= left && GodotObject.IsInstanceValid(entry.Character) && (!skipSameLine || entry.Character.gridPos.Y != 0) && !list.Contains(entry.Character))
			{
				list.Add(entry.Character);
			}
		}
		result.Clear();
		_spatialCandidateSeen.Clear();
		int scannedCount = 0;
		AddLineSweepCandidates(entries, maxEntries, left, right, 0, skipSameLine, result, ref scannedCount);
		if (result.Count != list.Count)
		{
			throw new InvalidOperationException("候选数量与完整区间查询不一致。");
		}
		for (int i = 0; i < result.Count; i++)
		{
			if (result[i] != list[i])
			{
				throw new InvalidOperationException("候选顺序与完整区间查询不一致。");
			}
		}
		GD.Print($"LINE_SWEEP_CHECK max={expectMaxScan} crossLine={skipSameLine} hits={result.Count} passed=True");
	}
}
