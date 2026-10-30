using System;

public static class SurvivalRoundProgressTransaction
{
	public static int AdvanceAndPersist(TowerDefenseLevelSurvivalRunner runner, Action persistProgress, Action beginRoundTransition)
	{
		if (runner == null)
		{
			throw new ArgumentNullException("runner");
		}
		if (beginRoundTransition == null)
		{
			throw new ArgumentNullException("beginRoundTransition");
		}
		runner.AdvanceRoundState(runner.roundNum + 1);
		persistProgress?.Invoke();
		beginRoundTransition();
		return runner.roundNum;
	}
}
