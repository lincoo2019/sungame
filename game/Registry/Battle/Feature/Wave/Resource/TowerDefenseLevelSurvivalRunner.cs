using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelSurvivalRunner : Resource
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelSurvivalConfig config;

	[Export(PropertyHint.None, "")]
	public int point;

	[Export(PropertyHint.None, "")]
	public Godot.Collections.Array zombiePool = new Godot.Collections.Array();

	[Export(PropertyHint.None, "")]
	public int addZombiePoolReachId;

	[Export(PropertyHint.None, "")]
	public int roundNum;

	[Export(PropertyHint.None, "")]
	public Godot.Collections.Array currentZombiePool = new Godot.Collections.Array();

	private int _currentZombiePoolRound = -2147483648;

	public void Init(TowerDefenseLevelSurvivalConfig _config)
	{
		config = _config.Duplicate() as TowerDefenseLevelSurvivalConfig;
		point = config.pointBegin;
		zombiePool = config.zombiePoolBase.Duplicate();
		addZombiePoolReachId = 0;
		roundNum = 0;
		if (currentZombiePool == null)
		{
			currentZombiePool = new Godot.Collections.Array();
		}
		_currentZombiePoolRound = -2147483648;
		if (point > config.pointMax)
		{
			point = config.pointMax;
		}
		RefreshCurrentZombiePool();
	}

	public void WaveReach(int waveId, bool isBigWave)
	{
		if (!isBigWave)
		{
			point += config.pointIncrementPerWave;
		}
		else
		{
			point += config.pointIncrementPerBigWave;
		}
		if (point > config.pointMax)
		{
			point = config.pointMax;
		}
	}

	public void RoundReach(int _roundNum)
	{
		AdvanceRoundState(_roundNum);
		BeginRoundTransition();
	}

	public void AdvanceRoundState(int _roundNum)
	{
		roundNum = _roundNum;
		point += config.pointIncrementPerRound;
		if (point > config.pointMax)
		{
			point = config.pointMax;
		}
		for (int i = Mathf.Clamp(addZombiePoolReachId, 0, config.zombiePoolRoundAdd.Count); i < config.zombiePoolRoundAdd.Count; i++)
		{
			TowerDefenseLevelSurvivalZombiePoolRoundAddConfig towerDefenseLevelSurvivalZombiePoolRoundAddConfig = config.zombiePoolRoundAdd[i];
			if (towerDefenseLevelSurvivalZombiePoolRoundAddConfig.round > roundNum)
			{
				break;
			}
			foreach (Variant zombie in towerDefenseLevelSurvivalZombiePoolRoundAddConfig.zombieList)
			{
				zombiePool.Add(zombie);
			}
			addZombiePoolReachId = i + 1;
		}
		RefreshCurrentZombiePool();
	}

	public void RestoreProgressState(int savedPoint, int savedRoundNum, Godot.Collections.Array savedCurrentZombiePool)
	{
		point = Mathf.Clamp(savedPoint, 0, config.pointMax);
		roundNum = Mathf.Max(0, savedRoundNum);
		zombiePool = config.zombiePoolBase?.Duplicate() ?? new Godot.Collections.Array();
		addZombiePoolReachId = 0;
		for (int i = 0; i < config.zombiePoolRoundAdd.Count; i++)
		{
			TowerDefenseLevelSurvivalZombiePoolRoundAddConfig towerDefenseLevelSurvivalZombiePoolRoundAddConfig = config.zombiePoolRoundAdd[i];
			if (towerDefenseLevelSurvivalZombiePoolRoundAddConfig.round > roundNum)
			{
				break;
			}
			foreach (Variant zombie in towerDefenseLevelSurvivalZombiePoolRoundAddConfig.zombieList)
			{
				zombiePool.Add(zombie);
			}
			addZombiePoolReachId = i + 1;
		}
		currentZombiePool = savedCurrentZombiePool?.Duplicate() ?? new Godot.Collections.Array();
		_currentZombiePoolRound = ((currentZombiePool.Count > 0) ? roundNum : (-2147483648));
		RefreshCurrentZombiePool();
	}

	public void BeginRoundTransition()
	{
		if (config.roundDayNightChange)
		{
			TowerDefenseManager.Instance.MapDayNightSwitch(5.0, -1.0);
		}
	}

	public long CalculateCurrentRoundPointBudget(int waveCount, int flagWaveInterval, int completedWaveCount = 0)
	{
		if (waveCount <= 0)
		{
			return 0L;
		}
		int num = point;
		long num2 = 0L;
		int num3;
		int num4;
		long num5;
		int b;
		for (int i = Mathf.Clamp(completedWaveCount, 0, waveCount) + 1; i <= waveCount; num5 = num4, num = (int)Math.Min(num + num5, config.pointMax), b = ((num3 != 0) ? ((int)Mathf.Floor((double)num * config.pointBigWaveScale)) : num), num2 += Mathf.Max(0, b), i++)
		{
			if (flagWaveInterval > 0)
			{
				num3 = ((i % flagWaveInterval == 0) ? 1 : 0);
				if (num3 != 0)
				{
					num4 = config.pointIncrementPerBigWave;
					continue;
				}
			}
			else
			{
				num3 = 0;
			}
			num4 = config.pointIncrementPerWave;
		}
		return num2;
	}

	public Godot.Collections.Array RefreshCurrentZombiePool()
	{
		if (_currentZombiePoolRound == roundNum)
		{
			return currentZombiePool;
		}
		currentZombiePool.Clear();
		Godot.Collections.Array array = zombiePool.Duplicate();
		int num = Mathf.Min(GD.RandRange(7, 9), array.Count);
		for (int i = 0; i < num; i++)
		{
			int index = GD.RandRange(0, array.Count - 1);
			currentZombiePool.Add(array[index]);
			array.RemoveAt(index);
		}
		_currentZombiePoolRound = roundNum;
		return currentZombiePool;
	}
}
