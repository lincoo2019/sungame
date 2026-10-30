using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewBattlePauseSpaceRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		bool multiplayer = Global.Instance.isMultiplayerMode;
		bool editor = Global.Instance.isEditor;
		bool enterBattle = Global.Instance.enterLevelIsBattle;
		Variant background = GameSaveManager.Instance.GetConfigValue("Backgrounder");
		TowerDefenseControlNew control = null;
		FieldInfo stateField = typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic);
		FieldInfo spritePathsField = typeof(ResourceManager).GetField("_characterSpritePaths", BindingFlags.Instance | BindingFlags.NonPublic);
		object resourceState = stateField.GetValue(ResourceManager.Instance);
		object spritePaths = spritePathsField.GetValue(ResourceManager.Instance);
		bool accumulatedInput = Input.UseAccumulatedInput;
		try
		{
			Input.UseAccumulatedInput = false;
			stateField.SetValue(ResourceManager.Instance, GameplayResourceLoadState.Ready);
			spritePathsField.SetValue(ResourceManager.Instance, new Dictionary<string, string> { ["PlantSunFlower"] = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/SunFlower.tscn" });
			ResourceManager.Instance.CHARCTAER_SPRITE["PlantSunFlower"] = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/SunFlower.tscn");
			Global.Instance.isMultiplayerMode = false;
			Global.Instance.isEditor = false;
			Global.Instance.enterLevelIsBattle = false;
			GameSaveManager.Instance.SetConfigValue("Backgrounder", true);
			control = new BugOverviewBattlePauseControlStub
			{
				isGameRunning = true,
				levelConfig = new TowerDefenseLevelConfig()
			};
			AddChild(control, forceReadableName: false, InternalMode.Disabled);
			Check(InputMap.HasAction("Pause"), "The real Pause action must exist.");
			control.ButtonPauseToggled(toggled: true);
			await Frames(1);
			Check(GetTree().Paused && FindDialog<DialogBattlePause>() != null, "The menu pause button must open BattlePause.");
			Press(echo: true);
			Check(GetTree().Paused, "Held-key repeats must not dismiss BattlePause.");
			Press();
			Check(!GetTree().Paused, "A fresh space press must close BattlePause immediately.");
			await Frames(2);
			Check(FindDialog<DialogBoxGamePause>() == null, "The resume press must not reopen pause on the next physics frame.");
			for (int cycle = 0; cycle < 6; cycle++)
			{
				Press();
				Check(GetTree().Paused && FindDialog<DialogBoxGamePause>() != null, $"Rapid cycle {cycle}: space must pause without a cooldown.");
				await Frames(1);
				Press(echo: true);
				Check(GetTree().Paused, $"Rapid cycle {cycle}: echoes must not toggle pause.");
				Press();
				Check(!GetTree().Paused && !control.waitPause, $"Rapid cycle {cycle}: the next distinct press must resume.");
				await Frames(1);
				Check(FindDialog<DialogBoxGamePause>() == null, $"Rapid cycle {cycle}: a consumed resume press must remain consumed.");
			}
			for (int i = 0; i < 4; i++)
			{
				Press();
				Check(GetTree().Paused == (i % 2 == 0), $"Same-frame press {i} must toggle once even with queued dialogs.");
			}
			await Frames(2);
			Check(!GetTree().Paused && FindDialog<DialogBoxGamePause>() == null, "No stale physics polling may re-pause after rapid presses.");
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[BugOverviewBattlePauseSpaceRuntimeTest] {value}");
		}
		finally
		{
			GetTree().Paused = false;
			Input.UseAccumulatedInput = accumulatedInput;
			stateField.SetValue(ResourceManager.Instance, resourceState);
			spritePathsField.SetValue(ResourceManager.Instance, spritePaths);
			Global.Instance.isMultiplayerMode = multiplayer;
			Global.Instance.isEditor = editor;
			Global.Instance.enterLevelIsBattle = enterBattle;
			GameSaveManager.Instance.SetConfigValue("Backgrounder", background);
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
		}
		bool flag = _failures == 0 && _checks == 34;
		GD.Print($"BATTLE_PAUSE_SPACE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void Press(bool echo = false)
	{
		Input.ParseInputEvent(new InputEventKey
		{
			PhysicalKeycode = Key.Space,
			Unicode = 32L,
			Pressed = true,
			Echo = echo
		});
		if (!echo)
		{
			Input.ParseInputEvent(new InputEventKey
			{
				PhysicalKeycode = Key.Space,
				Unicode = 32L,
				Pressed = false
			});
		}
		Input.FlushBufferedEvents();
	}

	private async Task Frames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private T FindDialog<T>() where T : Node
	{
		foreach (Node child in DialogManager.Instance.GetNode<CanvasLayer>("%DialogLayer").GetChildren())
		{
			if (child is T val && !val.IsQueuedForDeletion())
			{
				return val;
			}
		}
		return null;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewBattlePauseSpaceRuntimeTest] " + message);
		}
	}
}
