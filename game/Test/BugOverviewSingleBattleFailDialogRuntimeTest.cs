using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewSingleBattleFailDialogRuntimeTest : Node
{
	private const string ZombieWonScenePath = "res://Prefab/TowerDefense/GUI/InGame/ZombieWon/TowerDefenseZombieWon.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		TowerDefenseZombieWon animatedPresenter = null;
		TowerDefenseZombieWon immediatePresenter = null;
		try
		{
			_ = 10;
			try
			{
				Check(GodotObject.IsInstanceValid(DialogManager.Instance), "DialogManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(DialogManager.Instance))
				{
					goto end_IL_00a5;
				}
				animatedPresenter = InstantiatePresenter();
				Check(GodotObject.IsInstanceValid(animatedPresenter), "The real TowerDefenseZombieWon scene must instantiate.");
				if (!GodotObject.IsInstanceValid(animatedPresenter))
				{
					goto end_IL_00a5;
				}
				AddChild(animatedPresenter, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				AdobeAnimateSprite animatedSprite = animatedPresenter.GetNodeOrNull<AdobeAnimateSprite>("%ZombiesWonSprite");
				Check(GodotObject.IsInstanceValid(animatedSprite) && !animatedSprite.Visible && animatedSprite.pause, "The real failure presenter must begin with its authored animation hidden and paused.");
				int baselineDialogs = CountBattleFailDialogs();
				Check(baselineDialogs == 0, $"The isolated runtime must begin without a BattleFail dialog; count={baselineDialogs}.");
				animatedPresenter.LevelFail();
				await WaitFrames(3);
				Check(animatedSprite.Visible && !animatedSprite.pause, "The normal failure path must start the authored zombie-won animation.");
				Check(CountBattleFailDialogs() == baselineDialogs, "The animated failure path must not create its terminal dialog before animation completion.");
				animatedPresenter.LevelFail(playAnime: false);
				await WaitFrames(2);
				Check(CountBattleFailDialogs() == baselineDialogs, "An overlapping immediate failure request must not create an early duplicate dialog.");
				animatedPresenter.AnimeCompleted("Idle");
				await WaitFrames(3);
				Check(CountBattleFailDialogs() == baselineDialogs + 1, "Completing the authored failure animation must create exactly one BattleFail dialog.");
				Check(!animatedSprite.Visible, "The zombie-won animation must hide when its terminal dialog opens.");
				animatedPresenter.AnimeCompleted("Idle");
				animatedPresenter.LevelFail();
				animatedPresenter.LevelFail(playAnime: false);
				await WaitFrames(3);
				Check(CountBattleFailDialogs() == baselineDialogs + 1, "Repeated animation completions and failure requests must remain idempotent.");
				await WaitSeconds(2.3);
				await WaitFrames(3);
				Check(CountBattleFailDialogs() == baselineDialogs + 1, "Delayed failure presentation callbacks must not create a second BattleFail dialog.");
				CloseBattleFailDialogs();
				await WaitFrames(3);
				Check(CountBattleFailDialogs() == baselineDialogs, "The first failure dialog must close cleanly before testing immediate settlement.");
				immediatePresenter = InstantiatePresenter();
				Check(GodotObject.IsInstanceValid(immediatePresenter), "A fresh real failure presenter must instantiate for the no-animation result path.");
				if (!GodotObject.IsInstanceValid(immediatePresenter))
				{
					goto end_IL_00a5;
				}
				AddChild(immediatePresenter, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(3);
				immediatePresenter.LevelFail(playAnime: false);
				immediatePresenter.LevelFail(playAnime: false);
				await WaitFrames(3);
				Check(CountBattleFailDialogs() == baselineDialogs + 1, "The multiplayer/IZM no-animation path must still create exactly one terminal dialog.");
				immediatePresenter.AnimeCompleted("Idle");
				immediatePresenter.LevelFail();
				await WaitFrames(3);
				Check(CountBattleFailDialogs() == baselineDialogs + 1, "A completed immediate result must ignore later animation callbacks and requests.");
				goto end_IL_006d;
				end_IL_00a5:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSingleBattleFailDialogRuntimeTest] Unexpected exception: {value}");
				goto end_IL_006d;
			}
			return;
			end_IL_006d:;
		}
		finally
		{
			CloseBattleFailDialogs();
			if (GodotObject.IsInstanceValid(animatedPresenter))
			{
				animatedPresenter.QueueFree();
			}
			if (GodotObject.IsInstanceValid(immediatePresenter))
			{
				immediatePresenter.QueueFree();
			}
			await WaitFrames(4);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			await WaitFrames(2);
		}
		bool flag = _failures == 0 && _checks == 15;
		GD.Print($"SINGLE_BATTLE_FAIL_DIALOG_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Paused = false;
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseZombieWon InstantiatePresenter()
	{
		return ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/ZombieWon/TowerDefenseZombieWon.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseZombieWon>(PackedScene.GenEditState.Disabled);
	}

	private int CountBattleFailDialogs()
	{
		return CountBattleFailDialogs(GetTree().Root);
	}

	private static int CountBattleFailDialogs(Node parent)
	{
		int num = ((parent is DialogBoxBattleFail) ? 1 : 0);
		foreach (Node child in parent.GetChildren())
		{
			num += CountBattleFailDialogs(child);
		}
		return num;
	}

	private void CloseBattleFailDialogs()
	{
		CloseBattleFailDialogs(GetTree().Root);
	}

	private static void CloseBattleFailDialogs(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is DialogBoxBattleFail dialogBoxBattleFail)
			{
				dialogBoxBattleFail.CloseDialog();
			}
			else
			{
				CloseBattleFailDialogs(child);
			}
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewSingleBattleFailDialogRuntimeTest] " + message);
		}
	}
}
