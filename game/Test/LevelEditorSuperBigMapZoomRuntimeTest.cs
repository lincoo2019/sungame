using System;
using System.Threading.Tasks;
using Godot;

public partial class LevelEditorSuperBigMapZoomRuntimeTest : Node
{
	private const string EditorScenePath = "res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn";

	private const string MapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnSuperBig.tres";

	private const int ExpectedChecks = 33;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		bool previousEditor = Global.IsEditor;
		bool previousMobile = Global.Instance.isMobile;
		string previousScene = SceneManager.CurrentScene;
		Control host = null;
		LevelEditorMapEditor editor = null;
		TowerDefenseMapConfig mapConfig = null;
		try
		{
			_ = 5;
			try
			{
				Check(!ProjectSettings.GetSetting("input_devices/pointing/android/enable_pan_and_scale_gestures").AsBool(), "安卓必须关闭原生平移缩放转换，让地图编辑器稳定接收完整的多点触控事件流。");
				Check(GodotObject.IsInstanceValid(Global.Instance) && GodotObject.IsInstanceValid(SceneManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance), "运行时测试所需的自动加载节点必须有效。");
				Global.Instance.isEditor = true;
				Global.Instance.isMobile = false;
				SceneManager.Instance.currentScene = "LevelEditorStage";
				host = new Control();
				host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize);
				AddChild(host, forceReadableName: false, InternalMode.Disabled);
				editor = ResourceLoader.Load<PackedScene>("res://Prefab/GUI/LevelEditor/MapEditor/LevelEditorMapEditor.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<LevelEditorMapEditor>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(editor), "真实自制关卡地图编辑器场景必须能够实例化。");
				if (!GodotObject.IsInstanceValid(editor))
				{
					goto end_IL_0086;
				}
				editor.levelConfig = new TowerDefenseLevelConfig();
				host.AddChild(editor, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				mapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnSuperBig.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(mapConfig) && editor.mapFeature.MapInit(mapConfig), "超级大地图必须能够加载到真实自制关卡地图编辑器。");
				if (!GodotObject.IsInstanceValid(mapConfig))
				{
					goto end_IL_0086;
				}
				editor.ApplyMapPreviewConfig(mapConfig);
				await WaitFrames(1);
				Check(Mathf.IsEqualApprox(editor.MapPreviewZoom, 0.48f), $"2500x1200 超级大地图必须以 0.48 倍完整适配 1200x600 编辑区域，实际为 {editor.MapPreviewZoom}。");
				Check(GodotObject.IsInstanceValid(editor.mapFeature.mapControl.editorSprite) && editor.mapFeature.mapControl.editorSprite.Scale.IsEqualApprox(Vector2.One), "超级大地图背景不能单独缩放，必须交给统一地图世界变换。");
				float num = 0.408f;
				Check(editor.mapFeature.mapControl.GlobalScale.IsEqualApprox(editor.characterNode.GlobalScale) && Mathf.IsEqualApprox(editor.characterNode.GlobalScale.X, num), $"背景、格子和预放角色必须共享倍率 {num}，背景={editor.mapFeature.mapControl.GlobalScale}，角色={editor.characterNode.GlobalScale}。");
				Check(TowerDefenseManager.Instance.gridNum == new Vector2I(27, 15) && TowerDefenseManager.Instance.gridSize.IsEqualApprox(mapConfig.gridSize * editor.mapFeature.mapControl.GlobalScale), "统一适配后管理器必须刷新 27x15 格子尺寸缓存。");
				Check(editor.mapFeature.rect == TowerDefenseBattleFeatureMap.BuildProjectileBoundaryRect(mapConfig), $"地图 Feature 必须提交基于完整地图尺寸的子弹边界，实际为 {editor.mapFeature.rect}。");
				Check(FireComponent.ComputeProjectileMapRect() == editor.mapFeature.rect, "BulletField 和节点子弹必须从当前地图 Feature 读取同一份清理边界。");
				Control node = editor.GetNode<Control>("%LevelEditorPacketBank");
				Vector2 vector = editor.mapFeature.mapControl.GetGlobalTransformWithCanvas() * new Vector2(mapConfig.mapOffset.X + mapConfig.mapSize.X, 0f);
				Check(Mathf.Abs(vector.X - node.GetGlobalRect().Position.X) <= 1f, $"超级大地图初始位置必须向左偏移，并把地图右边界对齐卡牌面板左侧，地图={vector.X}，面板={node.GetGlobalRect().Position.X}。");
				Vector2I previewGridPosition = new Vector2I(27, 15);
				Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(previewGridPosition);
				Vector2 position = TowerDefensePacketConfig.ResolvePreparedCharacterMountPosition(editor.characterNode, mapCellPlantPos, isEditorPreview: true);
				Node2D previewCharacterMarker = new Node2D
				{
					Position = position
				};
				editor.characterNode.AddChild(previewCharacterMarker, forceReadableName: false, InternalMode.Disabled);
				Check(previewCharacterMarker.GlobalPosition.DistanceTo(mapCellPlantPos) <= 0.1f, $"自制关卡预放角色必须与缩放、平移后的格子位置完全一致，格子={mapCellPlantPos}，角色={previewCharacterMarker.GlobalPosition}。");
				Vector2 position2 = TowerDefensePacketConfig.ResolvePreparedCharacterMountPosition(editor.characterNode, editor.characterNode.ToGlobal(previewCharacterMarker.Position), isEditorPreview: true);
				Node2D node2D = new Node2D
				{
					Position = position2
				};
				editor.characterNode.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				Check(node2D.GlobalPosition.DistanceTo(previewCharacterMarker.GlobalPosition) <= 0.1f, "自制关卡叠放植物必须与底层植物使用同一挂载坐标。");
				Node2D node2D2 = new Node2D();
				editor.mapFeature.mapControl.spriteNode.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
				editor.mapFeature.mapControl.PositionMapOverlayAtWorldPoint(node2D2, mapCellPlantPos, Vector2.Zero);
				Vector2 to = editor.mapFeature.mapControl.GetCanvasTransform() * mapCellPlantPos;
				Check(node2D2.GetGlobalTransformWithCanvas().Origin.DistanceTo(to) <= 0.1f, "自制关卡种植跟随图必须把地图世界坐标转换到跨 Canvas 覆盖层。");
				bool outsideButtonPressed = false;
				Button outsideButton = new Button
				{
					Position = new Vector2(20f, Mathf.Max(0f, host.Size.Y - 50f)),
					Size = new Vector2(160f, 40f),
					Text = "地图外按钮"
				};
				outsideButton.Pressed += delegate
				{
					outsideButtonPressed = true;
				};
				host.AddChild(outsideButton, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				Vector2 center = outsideButton.GetGlobalRect().GetCenter();
				GetViewport().PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = center
				}, inLocalCoords: true);
				GetViewport().PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = center
				}, inLocalCoords: true);
				await WaitFrames(1);
				Check(outsideButtonPressed && !editor.IsMapPanActive, "地图预览区域外的界面按钮必须正常接收点击，不能被地图拖动输入消费。");
				outsideButton.QueueFree();
				await WaitFrames(1);
				Vector2 center2 = editor.GetGlobalRect().GetCenter();
				Vector2 vector2 = editor.mapFeature.ResolveViewportInputPosition(center2);
				Check(vector2.DistanceTo(center2) <= 0.1f, $"自制关卡实时鼠标必须与格子、铲子和种植链路使用同一世界坐标，屏幕={center2}，地图鼠标={vector2}。");
				Vector2 vector3 = editor.mapFeature.mapControl.ToLocal(center2);
				float mapPreviewZoom = editor.MapPreviewZoom;
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.WheelUp,
					Pressed = true,
					Factor = 1f,
					Position = center2
				});
				Vector2 to2 = editor.mapFeature.mapControl.ToLocal(center2);
				Check(editor.MapPreviewZoom > mapPreviewZoom, "鼠标滚轮向上必须放大自制关卡地图。");
				Check(vector3.DistanceTo(to2) <= 0.1f, $"滚轮缩放必须保持光标锚点稳定，偏差为 {vector3.DistanceTo(to2)}。");
				Vector2I vector2I = editor.mapFeature.ResolveInputGridPosition(vector2);
				Vector2 globalPosition = editor.mapFeature.mapControl.GlobalPosition;
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = center2
				});
				Check(editor.IsMapPanActive, "没有选择放置工具时按住鼠标左键必须启动地图视角拖动。");
				editor._Input(new InputEventMouseMotion
				{
					Position = center2 + new Vector2(40f, 0f)
				});
				Check(editor.mapFeature.mapControl.GlobalPosition.DistanceTo(globalPosition) > 1f, "鼠标按下拖动必须移动放大后的地图视角。");
				Check(previewCharacterMarker.GlobalPosition.DistanceTo(TowerDefenseManager.GetMapCellPlantPos(previewGridPosition)) <= 0.1f, "自制关卡视角平移后，已放置角色必须继续跟随同一格子。");
				Vector2I vector2I2 = editor.mapFeature.ResolveInputGridPosition(vector2);
				Check(vector2I2 == TowerDefenseManager.Instance.GetMapGridPosFromMouse(vector2) && vector2I2 != vector2I, $"地图平移后必须清除植物和铲子的旧格子缓存，平移前={vector2I}，平移后={vector2I2}。");
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = center2 + new Vector2(40f, 0f)
				});
				Check(!editor.IsMapPanActive, "鼠标按钮松开后必须结束地图视角拖动。");
				editor.mapFeature.shovelManager.shovelPick = true;
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(vector2I2);
				editor.mapFeature.shovelManager.ProcessShovelPick(mapCell, vector2I2, vector2);
				Vector2 origin = editor.mapFeature.shovelManager.mapShovelSprite.GetGlobalTransformWithCanvas().Origin;
				Check(origin.IsEqualApprox(center2 - new Vector2(-35f, 35f)), $"铲子跟随图必须跨 Canvas 对齐地图输入位置，实际屏幕位置为 {origin}。");
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = center2
				});
				Check(!editor.IsMapPanActive, "选择铲子等放置工具时鼠标左键不能启动地图视角拖动。");
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Middle,
					Pressed = true,
					Position = center2
				});
				Check(editor.IsMapPanActive, "选择放置工具时鼠标中键仍必须能够启动地图视角拖动。");
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Middle,
					Pressed = false,
					Position = center2
				});
				Check(!editor.IsMapPanActive, "鼠标中键松开后必须结束地图视角拖动。");
				editor.mapFeature.shovelManager.shovelPick = false;
				Global.Instance.isMobile = true;
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = center2
				});
				Check(!editor.IsMapPanActive, "安卓触摸模拟出的鼠标左键不能抢先启动编辑器桌面拖动。");
				editor._Input(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = center2
				});
				Vector2 position3 = center2 + new Vector2(-50f, 0f);
				Vector2 position4 = center2 + new Vector2(50f, 0f);
				editor._Input(new InputEventScreenTouch
				{
					Index = 0,
					Pressed = true,
					Position = position3
				});
				position3 += new Vector2(20f, 0f);
				editor._Input(new InputEventScreenDrag
				{
					Index = 0,
					Position = position3,
					Relative = new Vector2(20f, 0f)
				});
				editor._Input(new InputEventScreenTouch
				{
					Index = 1,
					Pressed = true,
					Position = position4
				});
				Check(editor.ShouldSuppressPlacementConfirm, "建立双指手势后必须阻止地图放置确认。");
				float mapPreviewZoom2 = editor.MapPreviewZoom;
				editor._Input(new InputEventScreenDrag
				{
					Index = 1,
					Position = center2 + new Vector2(90f, 0f)
				});
				Check(editor.MapPreviewZoom > mapPreviewZoom2, "安卓第一指已经移动后再按下第二指，增大双指间距仍必须放大自制关卡地图。");
				editor._Input(new InputEventScreenTouch
				{
					Index = 0,
					Pressed = false,
					Position = position3
				});
				editor._Input(new InputEventScreenTouch
				{
					Index = 1,
					Pressed = false,
					Position = center2 + new Vector2(90f, 0f)
				});
				Check(editor.ShouldSuppressPlacementConfirm, "双指松开的当前物理帧仍必须阻止误放单位。");
				await WaitFrames(2);
				Check(!editor.ShouldSuppressPlacementConfirm, "双指手势结束后必须恢复正常地图放置输入。");
				Check(editor.MapPreviewZoom <= 2f, "自制关卡地图编辑器缩放不得超过 2 倍上限。");
				goto end_IL_0063;
				end_IL_0086:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[LevelEditorSuperBigMapZoomRuntimeTest] 未处理异常：{value}");
				goto end_IL_0063;
			}
			return;
			end_IL_0063:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(editor))
			{
				editor.QueueFree();
			}
			if (GodotObject.IsInstanceValid(host))
			{
				host.QueueFree();
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.isEditor = previousEditor;
				Global.Instance.isMobile = previousMobile;
			}
			if (GodotObject.IsInstanceValid(SceneManager.Instance))
			{
				SceneManager.Instance.currentScene = previousScene;
			}
			mapConfig?.Dispose();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 33;
		GD.Print($"LEVEL_EDITOR_SUPER_BIG_ZOOM_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[LevelEditorSuperBigMapZoomRuntimeTest] " + message);
		}
	}
}
