using System;
using Godot;

public partial class BugDepartmentNormalDifficultyColorRuntimeTest : Node
{
	private const string ProgressManagerPath = "res://Registry/Battle/Feature/Progress/Manager/TowerDefenseProgressManager.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseProgressManager progress = null;
		try
		{
			try
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Progress/Manager/TowerDefenseProgressManager.tscn", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(packedScene), "The production progress-manager scene must load.");
				if (!GodotObject.IsInstanceValid(packedScene))
				{
					goto end_IL_0037;
				}
				progress = packedScene.Instantiate<TowerDefenseProgressManager>(PackedScene.GenEditState.Disabled);
				AddChild(progress, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				Check(GodotObject.IsInstanceValid(progress.difficultLabel), "The real progress manager must expose DifficultLabel.");
				progress.SetupUI("Normal", "Runtime difficulty color test");
				Check(IsColor(progress.difficultLabel.Modulate, 1f, 1f, 0f, 1f), $"Normal difficulty must render as yellow; got {progress.difficultLabel.Modulate}.");
				Check(!string.IsNullOrWhiteSpace(progress.difficultLabel.Text), "The real Normal setup path must also populate the difficulty label text.");
				progress.difficultLabel.Modulate = new Color(1f, 0f, 0f, 0.35f);
				TowerDefenseBattleFeatureProgress towerDefenseBattleFeatureProgress = new TowerDefenseBattleFeatureProgress();
				towerDefenseBattleFeatureProgress.progressManager = progress;
				towerDefenseBattleFeatureProgress.SetDifficultModulate("Normal");
				Check(IsColor(progress.difficultLabel.Modulate, 1f, 1f, 0f, 0.35f), $"Feature-driven Normal difficulty must be yellow and preserve alpha; got {progress.difficultLabel.Modulate}.");
				towerDefenseBattleFeatureProgress.SetDifficultModulate("Difficult");
				Check(IsColor(progress.difficultLabel.Modulate, 1f, 0f, 0f, 0.35f), "Difficult must remain red while preserving label alpha.");
				towerDefenseBattleFeatureProgress.SetDifficultModulate("Ultimate");
				Check(IsColor(progress.difficultLabel.Modulate, 1f, 0f, 0f, 0.35f), "Ultimate must remain red while preserving label alpha.");
				goto end_IL_002e;
				end_IL_0037:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentNormalDifficultyColorRuntimeTest] Unexpected exception: {value}");
				goto end_IL_002e;
			}
			return;
			end_IL_002e:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(progress))
			{
				progress.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 7;
		GD.Print($"NORMAL_DIFFICULTY_COLOR_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool IsColor(Color color, float red, float green, float blue, float alpha)
	{
		if (Mathf.IsEqualApprox(color.R, red) && Mathf.IsEqualApprox(color.G, green) && Mathf.IsEqualApprox(color.B, blue))
		{
			return Mathf.IsEqualApprox(color.A, alpha);
		}
		return false;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugDepartmentNormalDifficultyColorRuntimeTest] " + message);
		}
	}
}
