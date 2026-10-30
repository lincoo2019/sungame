using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class CubeBoxHologramPauseRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		double previousTimeScale = Engine.TimeScale;
		CubeBoxBloverControl control = null;
		try
		{
			_ = 3;
			try
			{
				control = new CubeBoxBloverControl
				{
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig(),
					ProcessMode = ProcessModeEnum.Pausable
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D();
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				TowerDefenseManager.Instance.currentControl = control;
				TowerDefensePlant plant = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn").Instantiate<TowerDefensePlant>(PackedScene.GenEditState.Disabled);
				plant.Position = new Vector2(500f, 250f);
				plant.gridPos = new Vector2I(3, 2);
				control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				plant.instance.hologram = true;
				TowerDefensePlantCubeBox box = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Diamond/CubeBox/Scene/TowerDefensePlantCubeBox.tscn").Instantiate<TowerDefensePlantCubeBox>(PackedScene.GenEditState.Disabled);
				box.editorPreviewMode = true;
				control.characterNode.AddChild(box, forceReadableName: false, InternalMode.Disabled);
				typeof(TowerDefensePlantCubeBox).GetMethod("ScheduleExpire", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(box, new object[1]
				{
					new List<TowerDefenseCharacter> { plant }
				});
				box.QueueFree();
				Engine.TimeScale = 20.0;
				await WaitWallTime(0.25);
				Check(!GodotObject.IsInstanceValid(box), "盒子本体必须已经被释放。");
				Check(GodotObject.IsInstanceValid(plant), "计时约5秒后投影必须仍存在。");
				GetTree().Paused = true;
				await WaitWallTime(2.0);
				Check(GodotObject.IsInstanceValid(plant), "暂停期间经过超过30秒的缩放时间也不能消耗投影寿命。");
				GetTree().Paused = false;
				await WaitWallTime(0.25);
				Check(GodotObject.IsInstanceValid(plant), "恢复后继续剩余时间，不能立即消失。");
				await WaitWallTime(1.2);
				Check(!GodotObject.IsInstanceValid(plant), "恢复后累计运行超过30秒时投影必须到期释放。");
			}
			catch (Exception ex)
			{
				_failures++;
				GD.PushError(ex.ToString());
			}
		}
		finally
		{
			GetTree().Paused = false;
			Engine.TimeScale = previousTimeScale;
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitWallTime(0.1);
		}
		GD.Print($"CUBE_BOX_HOLOGRAM_PAUSE_RESULT passed={_failures == 0 && _checks == 5} checks={_checks} failures={_failures}");
		GetTree().Quit((_failures != 0 || _checks != 5) ? 2 : 0);
	}

	private async Task WaitWallTime(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds, processAlways: true, processInPhysics: false, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.Print("CUBE_BOX_HOLOGRAM_PAUSE_FAILURE " + message);
		}
	}
}
