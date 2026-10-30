using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class LuckyBloverEndlessRoundProgressRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		TowerDefenseControlNew control = null;
		TowerDefenseLevelSurvivalConfig towerDefenseLevelSurvivalConfig = null;
		TowerDefenseLevelSurvivalRunner towerDefenseLevelSurvivalRunner = null;
		TowerDefenseBattleFeatureWave feature = null;
		UnlockConditionLevelSurvivalRoundConfig unlockConditionLevelSurvivalRoundConfig = null;
		TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = null;
		TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp2 = null;
		int transitionOperationId = -1;
		try
		{
			VerifySurvivalPoolCursorAndLegacyRepair();
			VerifyCurrentPoolRefreshesOncePerRound();
			VerifyRoundPointBudgetChangesWithRound();
			towerDefenseLevelSurvivalConfig = new TowerDefenseLevelSurvivalConfig
			{
				roundDayNightChange = true,
				pointBegin = 100,
				pointIncrementPerRound = 180,
				pointMax = 50000,
				zombiePoolBase = new Godot.Collections.Array { "ZombieNormal" }
			};
			towerDefenseLevelSurvivalRunner = new TowerDefenseLevelSurvivalRunner();
			towerDefenseLevelSurvivalRunner.Init(towerDefenseLevelSurvivalConfig);
			towerDefenseLevelSurvivalRunner.roundNum = 9;
			feature = new TowerDefenseBattleFeatureWave
			{
				isSurvival = true,
				survivalRunner = towerDefenseLevelSurvivalRunner
			};
			Dictionary dictionary = feature.SaveFeature();
			Check(dictionary.GetValueOrDefault("survivalRoundNum", -1).AsInt32() == 9, "The probe must begin from a persisted ninth completed round.");
			control = new TowerDefenseControlNew
			{
				isGameRunning = true
			};
			bool transitionStarted = false;
			int persistedRound = -1;
			SurvivalRoundProgressTransaction.AdvanceAndPersist(towerDefenseLevelSurvivalRunner, delegate
			{
				Check(!transitionStarted, "Persistence must run before the day/night map transition begins.");
				Check(control.CanCreateProgressSave(out var reason2), "The real progress-save guard must still accept the checkpoint before map transition; got '" + reason2 + "'.");
				Dictionary dictionary2 = feature.SaveFeature();
				persistedRound = dictionary2.GetValueOrDefault("survivalRoundNum", -1).AsInt32();
			}, delegate
			{
				transitionStarted = true;
				transitionOperationId = control.BeginPendingBattleOperation();
			});
			Check(towerDefenseLevelSurvivalRunner.roundNum == 10, $"The shared transaction must advance the live runner to round ten, got {towerDefenseLevelSurvivalRunner.roundNum}.");
			Check(persistedRound == 10, $"The transaction must persist after advancing, so survivalRoundNum must be 10 at callback time; got {persistedRound}.");
			Check(persistedRound >= 10, "The exported tenth round must immediately satisfy Lucky Blover's ten-round unlock threshold.");
			unlockConditionLevelSurvivalRoundConfig = new UnlockConditionLevelSurvivalRoundConfig
			{
				roundNum = 10
			};
			towerDefenseLevelSaveConfigCSharp = new TowerDefenseLevelSaveConfigCSharp();
			towerDefenseLevelSaveConfigCSharp.featureSave[new StringName("Wave")] = new Dictionary
			{
				["isSurvival"] = true,
				["survivalRoundNum"] = persistedRound
			};
			Check(unlockConditionLevelSurvivalRoundConfig.CheckProgress(towerDefenseLevelSaveConfigCSharp), "The real unlock evaluator must read round ten from the current Wave Feature save section.");
			towerDefenseLevelSaveConfigCSharp.featureSave[new StringName("Wave")].AsGodotDictionary()["survivalRoundNum"] = 9;
			Check(!unlockConditionLevelSurvivalRoundConfig.CheckProgress(towerDefenseLevelSaveConfigCSharp), "The real unlock evaluator must reject a current save with only nine completed rounds.");
			towerDefenseLevelSaveConfigCSharp2 = new TowerDefenseLevelSaveConfigCSharp();
			towerDefenseLevelSaveConfigCSharp2.processSave["main"] = new Dictionary
			{
				["isSurvival"] = true,
				["survivalRoundNum"] = 10
			};
			Check(unlockConditionLevelSurvivalRoundConfig.CheckProgress(towerDefenseLevelSaveConfigCSharp2), "The real unlock evaluator must retain compatibility with legacy process saves.");
			Check(transitionStarted && control.HasPendingBattleOperations, "The deferred day/night transition must begin after persistence and enter the real pending-operation guard.");
			Check(!control.CanCreateProgressSave(out var _), "The real progress-save guard must reject checkpoints after the map transition begins.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[LuckyBloverEndlessRoundProgressRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			if (control != null && transitionOperationId >= 0)
			{
				control.CompletePendingBattleOperation(transitionOperationId);
			}
			control?.Free();
			feature?.Dispose();
			towerDefenseLevelSurvivalRunner?.Dispose();
			towerDefenseLevelSurvivalConfig?.Dispose();
			unlockConditionLevelSurvivalRoundConfig?.Dispose();
			towerDefenseLevelSaveConfigCSharp?.Dispose();
			towerDefenseLevelSaveConfigCSharp2?.Dispose();
		}
		bool flag = _failures == 0;
		GD.Print($"LUCKY_BLOVER_ENDLESS_ROUND_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void VerifySurvivalPoolCursorAndLegacyRepair()
	{
		TowerDefenseLevelSurvivalConfig towerDefenseLevelSurvivalConfig = new TowerDefenseLevelSurvivalConfig
		{
			pointBegin = 100,
			pointIncrementPerRound = 10,
			pointMax = 10000,
			zombiePoolBase = new Godot.Collections.Array { "Base" }
		};
		towerDefenseLevelSurvivalConfig.zombiePoolRoundAdd.Add(CreateRoundAdd(1, "Round1A", "Round1B"));
		towerDefenseLevelSurvivalConfig.zombiePoolRoundAdd.Add(CreateRoundAdd(2, "Round2"));
		towerDefenseLevelSurvivalConfig.zombiePoolRoundAdd.Add(CreateRoundAdd(9, "Round9A", "Round9B"));
		TowerDefenseLevelSurvivalRunner towerDefenseLevelSurvivalRunner = new TowerDefenseLevelSurvivalRunner();
		towerDefenseLevelSurvivalRunner.Init(towerDefenseLevelSurvivalConfig);
		for (int i = 1; i <= 22; i++)
		{
			towerDefenseLevelSurvivalRunner.AdvanceRoundState(i);
		}
		Check(towerDefenseLevelSurvivalRunner.zombiePool.Count == 6, $"Each authored pool group must be added once; expected 6 entries, got {towerDefenseLevelSurvivalRunner.zombiePool.Count}.");
		Check(towerDefenseLevelSurvivalRunner.addZombiePoolReachId == 3, $"The pool cursor must point past all three processed groups, got {towerDefenseLevelSurvivalRunner.addZombiePoolReachId}.");
		Godot.Collections.Array array = new Godot.Collections.Array();
		for (int j = 0; j < 14; j++)
		{
			array.Add("Round9A");
			array.Add("Round9B");
		}
		Godot.Collections.Array savedCurrentZombiePool = new Godot.Collections.Array { "Round9A", "Base" };
		towerDefenseLevelSurvivalRunner.zombiePool = array;
		towerDefenseLevelSurvivalRunner.addZombiePoolReachId = 2;
		towerDefenseLevelSurvivalRunner.RestoreProgressState(4321, 22, savedCurrentZombiePool);
		Check(towerDefenseLevelSurvivalRunner.zombiePool.Count == 6, $"Legacy restore must rebuild the canonical six-entry pool, got {towerDefenseLevelSurvivalRunner.zombiePool.Count}.");
		Check(towerDefenseLevelSurvivalRunner.addZombiePoolReachId == 3, $"Legacy restore must migrate the cursor to the next group, got {towerDefenseLevelSurvivalRunner.addZombiePoolReachId}.");
		Check(towerDefenseLevelSurvivalRunner.currentZombiePool.Count == 2 && towerDefenseLevelSurvivalRunner.currentZombiePool[0].AsString() == "Round9A" && towerDefenseLevelSurvivalRunner.currentZombiePool[1].AsString() == "Base", "Legacy restore must preserve the already-selected current-round pool.");
		towerDefenseLevelSurvivalRunner.Dispose();
		towerDefenseLevelSurvivalConfig.Dispose();
	}

	private void VerifyRoundPointBudgetChangesWithRound()
	{
		TowerDefenseLevelSurvivalConfig towerDefenseLevelSurvivalConfig = new TowerDefenseLevelSurvivalConfig
		{
			pointBegin = 100,
			pointIncrementPerWave = 10,
			pointIncrementPerBigWave = 50,
			pointIncrementPerRound = 20,
			pointBigWaveScale = 2.0,
			pointMax = 250,
			zombiePoolBase = new Godot.Collections.Array { "Base" }
		};
		TowerDefenseLevelSurvivalRunner towerDefenseLevelSurvivalRunner = new TowerDefenseLevelSurvivalRunner();
		towerDefenseLevelSurvivalRunner.Init(towerDefenseLevelSurvivalConfig);
		long num = towerDefenseLevelSurvivalRunner.CalculateCurrentRoundPointBudget(5, 2);
		Check(num == 1270, $"Round zero must project ordinary/big-wave point growth to 1270 total points, got {num}.");
		towerDefenseLevelSurvivalRunner.WaveReach(1, isBigWave: false);
		towerDefenseLevelSurvivalRunner.WaveReach(2, isBigWave: true);
		long num2 = towerDefenseLevelSurvivalRunner.CalculateCurrentRoundPointBudget(5, 2, 2);
		Check(num2 == 840, $"A mid-round restore after two waves must project only the remaining 840 points, got {num2}.");
		for (int i = 3; i <= 5; i++)
		{
			towerDefenseLevelSurvivalRunner.WaveReach(i, i % 2 == 0);
		}
		towerDefenseLevelSurvivalRunner.AdvanceRoundState(1);
		long num3 = towerDefenseLevelSurvivalRunner.CalculateCurrentRoundPointBudget(5, 2);
		Check(towerDefenseLevelSurvivalRunner.point == 250 && num3 == 1750, $"Round one must use its updated capped point value and project 1750 total points; point={towerDefenseLevelSurvivalRunner.point}, budget={num3}.");
		Check(num3 > num, "A later round with more points must request a larger point-derived zombie reservoir.");
		towerDefenseLevelSurvivalRunner.Dispose();
		towerDefenseLevelSurvivalConfig.Dispose();
	}

	private void VerifyCurrentPoolRefreshesOncePerRound()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		for (int i = 0; i < 10; i++)
		{
			array.Add($"Zombie{i}");
		}
		TowerDefenseLevelSurvivalConfig towerDefenseLevelSurvivalConfig = new TowerDefenseLevelSurvivalConfig
		{
			pointBegin = 100,
			pointIncrementPerWave = 5,
			pointIncrementPerRound = 10,
			pointMax = 10000,
			zombiePoolBase = array
		};
		TowerDefenseLevelSurvivalRunner towerDefenseLevelSurvivalRunner = new TowerDefenseLevelSurvivalRunner();
		towerDefenseLevelSurvivalRunner.Init(towerDefenseLevelSurvivalConfig);
		int count = towerDefenseLevelSurvivalRunner.currentZombiePool.Count;
		Check(count >= 7 && count <= 9, $"A round pool must select 7-9 entries when enough zombies exist, got {towerDefenseLevelSurvivalRunner.currentZombiePool.Count}.");
		Check(HasUniqueEntries(towerDefenseLevelSurvivalRunner.currentZombiePool), "A round pool must not select the same authored entry more than once.");
		Godot.Collections.Array right = towerDefenseLevelSurvivalRunner.currentZombiePool.Duplicate();
		towerDefenseLevelSurvivalRunner.RefreshCurrentZombiePool();
		Check(PoolsEqual(towerDefenseLevelSurvivalRunner.currentZombiePool, right), "Refreshing again in the same round must preserve the selected pool.");
		towerDefenseLevelSurvivalRunner.WaveReach(1, isBigWave: false);
		towerDefenseLevelSurvivalRunner.RefreshCurrentZombiePool();
		Check(PoolsEqual(towerDefenseLevelSurvivalRunner.currentZombiePool, right), "Advancing a wave must not reroll the round's zombie pool.");
		towerDefenseLevelSurvivalRunner.zombiePool = new Godot.Collections.Array { "Round1Only" };
		towerDefenseLevelSurvivalRunner.AdvanceRoundState(1);
		Check(PoolMatches(towerDefenseLevelSurvivalRunner.currentZombiePool, "Round1Only"), "Advancing the round must refresh from the newly available zombie pool.");
		towerDefenseLevelSurvivalRunner.zombiePool = new Godot.Collections.Array { "SameRoundMutation" };
		towerDefenseLevelSurvivalRunner.RefreshCurrentZombiePool();
		Check(PoolMatches(towerDefenseLevelSurvivalRunner.currentZombiePool, "Round1Only"), "A second refresh request in round one must remain idempotent.");
		towerDefenseLevelSurvivalRunner.AdvanceRoundState(2);
		Check(PoolMatches(towerDefenseLevelSurvivalRunner.currentZombiePool, "SameRoundMutation"), "The next round must consume the latest unlocked pool and refresh exactly once.");
		towerDefenseLevelSurvivalRunner.Dispose();
		towerDefenseLevelSurvivalConfig.Dispose();
	}

	private static bool HasUniqueEntries(Godot.Collections.Array pool)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (Variant item in pool)
		{
			if (!hashSet.Add(item.AsString()))
			{
				return false;
			}
		}
		return true;
	}

	private static bool PoolsEqual(Godot.Collections.Array left, Godot.Collections.Array right)
	{
		if (left == null || right == null || left.Count != right.Count)
		{
			return false;
		}
		for (int i = 0; i < left.Count; i++)
		{
			if (left[i].AsString() != right[i].AsString())
			{
				return false;
			}
		}
		return true;
	}

	private static bool PoolMatches(Godot.Collections.Array pool, params string[] expected)
	{
		if (pool == null || pool.Count != expected.Length)
		{
			return false;
		}
		for (int i = 0; i < expected.Length; i++)
		{
			if (pool[i].AsString() != expected[i])
			{
				return false;
			}
		}
		return true;
	}

	private static TowerDefenseLevelSurvivalZombiePoolRoundAddConfig CreateRoundAdd(int round, params string[] zombies)
	{
		TowerDefenseLevelSurvivalZombiePoolRoundAddConfig towerDefenseLevelSurvivalZombiePoolRoundAddConfig = new TowerDefenseLevelSurvivalZombiePoolRoundAddConfig
		{
			round = round
		};
		foreach (string text in zombies)
		{
			towerDefenseLevelSurvivalZombiePoolRoundAddConfig.zombieList.Add(text);
		}
		return towerDefenseLevelSurvivalZombiePoolRoundAddConfig;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[LuckyBloverEndlessRoundProgressRuntimeTest] " + message);
		}
	}
}
