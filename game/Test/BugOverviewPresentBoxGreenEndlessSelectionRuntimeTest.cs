using System;
using Godot;
using Godot.Collections;

public partial class BugOverviewPresentBoxGreenEndlessSelectionRuntimeTest : Node
{
	private const string LevelItemScenePath = "res://Prefab/GUI/DragMenu/Select/Level/DragMenuSelectItemlevel.tscn";

	private const string ProbeUser = "PresentBoxGreenEndlessSelectionProbe";

	private const string CurrentLevelKey = "BugOverviewPresentBoxGreenEndlessCurrent";

	private const string LegacyLevelKey = "BugOverviewPresentBoxGreenEndlessLegacy";

	private const string EmptyLevelKey = "BugOverviewPresentBoxGreenEndlessEmpty";

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		DragMenuSelectItemlevel dragMenuSelectItemlevel = null;
		TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp = null;
		TowerDefenseLevelSaveConfigCSharp towerDefenseLevelSaveConfigCSharp2 = null;
		string text = Global.Instance?.currentLevelChoose;
		try
		{
			Check(GodotObject.IsInstanceValid(Global.Instance), "Global autoload must be available.");
			Check(GodotObject.IsInstanceValid(GameSaveManager.Instance), "GameSaveManager autoload must be available.");
			if (!GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(GameSaveManager.Instance))
			{
				return;
			}
			GameSaveManager.Instance.EnsureLoaded();
			GameSaveManager.Instance.SetUserCurrent("PresentBoxGreenEndlessSelectionProbe");
			Global.Instance.currentLevelChoose = "Survival";
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/DragMenu/Select/Level/DragMenuSelectItemlevel.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			Check(GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate(), "The production DragMenuSelectItemlevel scene must load and instantiate.");
			if (!GodotObject.IsInstanceValid(packedScene) || !packedScene.CanInstantiate())
			{
				return;
			}
			dragMenuSelectItemlevel = packedScene.Instantiate<DragMenuSelectItemlevel>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(dragMenuSelectItemlevel), "The production level-selection item must instantiate with its real script.");
			if (!GodotObject.IsInstanceValid(dragMenuSelectItemlevel))
			{
				return;
			}
			AddChild(dragMenuSelectItemlevel, forceReadableName: false, InternalMode.Disabled);
			Label nodeOrNull = dragMenuSelectItemlevel.GetNodeOrNull<Label>("%SurvivalLabel");
			Sprite2D nodeOrNull2 = dragMenuSelectItemlevel.GetNodeOrNull<Sprite2D>("%CupSprite");
			Check(GodotObject.IsInstanceValid(nodeOrNull), "The real level-selection scene must expose SurvivalLabel.");
			Check(GodotObject.IsInstanceValid(nodeOrNull2), "The real level-selection scene must expose CupSprite.");
			if (!GodotObject.IsInstanceValid(nodeOrNull) || !GodotObject.IsInstanceValid(nodeOrNull2))
			{
				return;
			}
			SetLevelResult("BugOverviewPresentBoxGreenEndlessCurrent", mower: false, finish: true);
			towerDefenseLevelSaveConfigCSharp = new TowerDefenseLevelSaveConfigCSharp();
			towerDefenseLevelSaveConfigCSharp.featureSave[new StringName("Wave")] = new Dictionary
			{
				["isSurvival"] = true,
				["survivalRoundNum"] = 12
			};
			towerDefenseLevelSaveConfigCSharp.processSave["main"] = new Dictionary
			{
				["isSurvival"] = true,
				["survivalRoundNum"] = 3
			};
			Check(SaveProgress("BugOverviewPresentBoxGreenEndlessCurrent", towerDefenseLevelSaveConfigCSharp) == Error.Ok, "The isolated current-format progress fixture must save successfully.");
			dragMenuSelectItemlevel.Init("BugOverviewPresentBoxGreenEndlessCurrent");
			Check(nodeOrNull.Visible, "A completed endless level must still show its round count beside the trophy.");
			Check(nodeOrNull.Text == "12轮完成", "Current saves must read Wave Feature round 12, got '" + nodeOrNull.Text + "'.");
			Check(GodotObject.IsInstanceValid(nodeOrNull2.Texture), "Finish state must retain its trophy while the endless round label is visible.");
			SetLevelResult("BugOverviewPresentBoxGreenEndlessLegacy", mower: true, finish: false);
			towerDefenseLevelSaveConfigCSharp2 = new TowerDefenseLevelSaveConfigCSharp();
			towerDefenseLevelSaveConfigCSharp2.processSave["main"] = new Dictionary
			{
				["isSurvival"] = true,
				["survivalRoundNum"] = 7
			};
			Check(SaveProgress("BugOverviewPresentBoxGreenEndlessLegacy", towerDefenseLevelSaveConfigCSharp2) == Error.Ok, "The isolated legacy progress fixture must save successfully.");
			dragMenuSelectItemlevel.Init("BugOverviewPresentBoxGreenEndlessLegacy");
			Check(nodeOrNull.Visible, "A legacy endless save must remain visible after the level item is reused.");
			Check(nodeOrNull.Text == "7轮完成", "Legacy saves must fall back to process main round 7, got '" + nodeOrNull.Text + "'.");
			Check(GodotObject.IsInstanceValid(nodeOrNull2.Texture), "Mower state must retain its trophy while the legacy round label is visible.");
			SetLevelResult("BugOverviewPresentBoxGreenEndlessEmpty", mower: false, finish: false);
			Check(!GameSaveManager.Instance.HasLevelProgress("BugOverviewPresentBoxGreenEndlessEmpty"), "The empty fixture must not have an endless progress resource.");
			dragMenuSelectItemlevel.Init("BugOverviewPresentBoxGreenEndlessEmpty");
			Check(!nodeOrNull.Visible, "Reusing the item for a level without progress must hide the old round label.");
			Check(string.IsNullOrEmpty(nodeOrNull.Text), "Reusing the item without progress must clear stale text, got '" + nodeOrNull.Text + "'.");
			Check(!GodotObject.IsInstanceValid(nodeOrNull2.Texture), "Reusing the item without an award must clear the previous trophy texture.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[PresentBoxGreenEndlessSelection] Unexpected exception: {value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(Global.Instance) && text != null)
			{
				Global.Instance.currentLevelChoose = text;
			}
			dragMenuSelectItemlevel?.QueueFree();
			towerDefenseLevelSaveConfigCSharp?.Dispose();
			towerDefenseLevelSaveConfigCSharp2?.Dispose();
		}
		bool flag = _failures == 0 && _checks == 18;
		GD.Print($"PRESENT_BOX_GREEN_ENDLESS_SELECTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void SetLevelResult(string levelKey, bool mower, bool finish)
	{
		GameSaveManager.Instance.SetLevelValue(levelKey, new Dictionary
		{
			["Normal"] = finish,
			["Difficult"] = false,
			["Ultimate"] = false,
			["Mower"] = mower,
			["Key"] = new Dictionary { ["Finish"] = (finish ? 1 : 0) }
		});
	}

	private static Error SaveProgress(string levelKey, TowerDefenseLevelSaveConfigCSharp progress)
	{
		string text = "user://Csharp/Progress/PresentBoxGreenEndlessSelectionProbe";
		Error error = DirAccess.MakeDirRecursiveAbsolute(text);
		if (error != Error.Ok && error != Error.AlreadyExists)
		{
			return error;
		}
		return ResourceSaver.Save(progress, text + "/" + levelKey + ".tres", ResourceSaver.SaverFlags.None);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PresentBoxGreenEndlessSelection] " + message);
		}
	}
}
