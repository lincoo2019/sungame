using System;
using Godot;

public partial class Chapter7MapCameraBoundsRuntimeTest : Node
{
	private const string CameraScenePath = "res://Registry/Battle/Feature/Camera/Control/TowerDefenseCameraControl.tscn";

	private static readonly string[] MapConfigPaths = new string[4] { "res://Asset/Config/Map/TreasureIslandFrontLawn/Config/TreasureIslandFrontlawnMapTreasureIslandFrontLawn.tres", "res://Asset/Config/Map/TreasureIslandFrontLawn/Config/TreasureIslandFrontlawnMapTreasureIslandFrontlawnNight.tres", "res://Asset/Config/Map/TreasureIslandBackyard/Config/TreasureIslandBackyardMapTreasureIslandBackyard.tres", "res://Asset/Config/Map/TreasureIslandBackyard/Config/TreasureIslandBackyardMapTreasureIslandBackyardNight.tres" };

	private int _checks;

	private int _failures;

	public override void _Ready()
	{
		TowerDefenseCameraControl towerDefenseCameraControl = null;
		try
		{
			towerDefenseCameraControl = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Camera/Control/TowerDefenseCameraControl.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseCameraControl>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(towerDefenseCameraControl), "生产相机场景必须能够实例化。");
			if (!GodotObject.IsInstanceValid(towerDefenseCameraControl))
			{
				Finish();
				return;
			}
			AddChild(towerDefenseCameraControl, forceReadableName: false, InternalMode.Disabled);
			string[] mapConfigPaths = MapConfigPaths;
			foreach (string mapConfigPath in mapConfigPaths)
			{
				VerifyMapCameraBounds(towerDefenseCameraControl, mapConfigPath);
			}
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[Chapter7MapCameraBoundsRuntimeTest] 未处理异常：{value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(towerDefenseCameraControl))
			{
				towerDefenseCameraControl.QueueFree();
			}
		}
		Finish();
	}

	private void VerifyMapCameraBounds(TowerDefenseCameraControl cameraControl, string mapConfigPath)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>(mapConfigPath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(towerDefenseMapConfig) && towerDefenseMapConfig.mapSize == new Vector2(1400f, 600f) && towerDefenseMapConfig.mapOffset == new Vector2(-205f, 0f) && !string.IsNullOrWhiteSpace(towerDefenseMapConfig.mapTexturePath), "第七章地图必须保留 1400×600 逻辑范围、-205 背景偏移和背景资源路径：" + mapConfigPath);
		if (GodotObject.IsInstanceValid(towerDefenseMapConfig))
		{
			cameraControl.ApplyMapConfig(towerDefenseMapConfig);
			float x = GetViewport().GetVisibleRect().Size.X;
			float num = Mathf.Max(0f, towerDefenseMapConfig.mapSize.X - x);
			float b = Mathf.Max(0f, num - 56f);
			Check(Mathf.IsEqualApprox(cameraControl.camera.GlobalPosition.X, 0f) && Mathf.IsEqualApprox(cameraControl.cameraBeginMarker.GlobalPosition.X, 0f), $"第七章地图入场相机必须从逻辑 X=0 开始，不能从背景偏移 {towerDefenseMapConfig.mapOffset.X} 开始：{mapConfigPath}");
			Check(Mathf.IsEqualApprox(cameraControl.cameraRightViewMarker.GlobalPosition.X, num) && Mathf.IsEqualApprox(cameraControl.cameraPreViewMarker.GlobalPosition.X, b), $"第七章地图开场预览标记必须覆盖 1400 像素逻辑宽度，实际右侧={cameraControl.cameraRightViewMarker.GlobalPosition.X}，返回前={cameraControl.cameraPreViewMarker.GlobalPosition.X}：{mapConfigPath}");
			Check(cameraControl.downRightMarker.GlobalPosition.IsEqualApprox(towerDefenseMapConfig.mapSize), $"第七章地图右下相机边界必须使用逻辑地图尺寸，实际为 {cameraControl.downRightMarker.GlobalPosition}：{mapConfigPath}");
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[Chapter7MapCameraBoundsRuntimeTest] " + message);
		}
	}

	private void Finish()
	{
		bool flag = _failures == 0 && _checks == 17;
		GD.Print($"CHAPTER7_MAP_CAMERA_BOUNDS_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}
}
