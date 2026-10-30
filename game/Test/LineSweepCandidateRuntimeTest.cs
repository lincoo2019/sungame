using System;
using Godot;

public partial class LineSweepCandidateRuntimeTest : Node
{
	public override void _Ready()
	{
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = new TowerDefenseBattleCharacterRegistry();
		try
		{
			towerDefenseBattleCharacterRegistry.VerifyLineSweepCandidatesForTest();
			GD.Print("LINE_SWEEP_CANDIDATE_RESULT passed=True");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex);
			GetTree().Quit(1);
		}
		finally
		{
			towerDefenseBattleCharacterRegistry.Free();
		}
	}
}
