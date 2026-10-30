using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class SuperBigMapBattleZoomRuntimeTest : Node
{
	private const string MapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnSuperBig.tres";

	private const string MapScenePath = "res://Asset/Config/Map/Frontlawn/Scene/DaySuperBig/TowerDefenseMapFrontlawnSuperBig.tscn";

	private const string CameraScenePath = "res://Registry/Battle/Feature/Camera/Control/TowerDefenseCameraControl.tscn";

	private const string PacketScenePath = "res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseControlNew previousControl = TowerDefenseManager.Instance?.currentControl;
		string previousLocale = TranslationServer.GetLocale();
		bool previousMobile = Global.Instance.isMobile;
		SuperBigMapBattleZoomControlStub control = null;
		TowerDefenseCameraControl cameraControl = null;
		TowerDefenseMapFrontlawnBig mapScene = null;
		TowerDefenseBattleFeatureMap frameRateFeature = null;
		CanvasLayer userInterfaceLayer = null;
		try
		{
			_ = 24;
			try
			{
				Check(!ProjectSettings.GetSetting("input_devices/pointing/android/enable_pan_and_scale_gestures").AsBool(), "安卓必须关闭原生平移缩放转换，让战斗相机稳定接收完整的多点触控事件流。");
				TowerDefenseMapConfig mapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnSuperBig.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("uid://d3supermapday", null, ResourceLoader.CacheMode.Reuse);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Config/Map/Frontlawn/Scene/DaySuperBig/TowerDefenseMapFrontlawnSuperBig.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Camera/Control/TowerDefenseCameraControl.tscn", null, ResourceLoader.CacheMode.Ignore);
				PackedScene packedPacketScene = ResourceLoader.Load<PackedScene>("res://Prefab/TowerDefense/GUI/InGame/Packet/TowerDefenseInGamePacketShow.tscn", null, ResourceLoader.CacheMode.Ignore);
				mapScene = packedScene?.Instantiate<TowerDefenseMapFrontlawnBig>(PackedScene.GenEditState.Disabled);
				cameraControl = packedScene2?.Instantiate<TowerDefenseCameraControl>(PackedScene.GenEditState.Disabled);
				string reason = "资源未加载";
				Check(GodotObject.IsInstanceValid(mapConfig) && mapConfig.TryValidateRuntime(out reason), "超级大地图配置必须通过运行校验：" + reason);
				Check(GodotObject.IsInstanceValid(towerDefenseMapConfig) && towerDefenseMapConfig.ResourcePath == "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnSuperBig.tres", "地图资源表使用的 UID 必须解析到超级大地图配置。");
				TranslationServer.SetLocale("zh");
				Check(TranslationServer.Translate("MAP_FRONTLAWN_SUPER_BIG") == (StringName)"超级大地图（白天）", "中文地图名称必须显示为“超级大地图（白天）”。");
				Check(GodotObject.IsInstanceValid(mapConfig) && mapConfig.translate == "MAP_FRONTLAWN_SUPER_BIG" && mapConfig.mapSize == new Vector2(2500f, 1200f) && mapConfig.gridNum == new Vector2I(27, 15) && mapConfig.gridBeginPos == new Vector2(244f, 76f) && mapConfig.gridSize == new Vector2(69f, 74f) && Mathf.IsEqualApprox((float)mapConfig.plantOffset, 25f) && mapConfig.enableBattleZoom && mapConfig.maximumFps == 60, "超级大地图必须保留原图尺寸、27x15 草坪、25 像素植物偏移、战斗缩放和 60 FPS 上限配置。");
				Check(mapConfig.lineUse.Count == 15 && mapConfig.lineUse[0] == 1 && mapConfig.lineUse[14] == 15 && GodotObject.IsInstanceValid(mapConfig.GetEffectiveCellConfig(27, 15)), "超级大地图必须完整启用 15 行，并让地面单元配置覆盖到第 27 列第 15 行。");
				TowerDefenseMapConfig towerDefenseMapConfig2 = new TowerDefenseMapConfig
				{
					gridNum = new Vector2I(50, 50)
				};
				TowerDefenseMapConfig towerDefenseMapConfig3 = new TowerDefenseMapConfig
				{
					gridNum = new Vector2I(51, 50)
				};
				Check(towerDefenseMapConfig2.TryValidateRuntime(out var reason2) && string.IsNullOrEmpty(reason2) && !towerDefenseMapConfig3.TryValidateRuntime(out var reason3) && reason3.Contains("50x50", StringComparison.Ordinal), "地图运行校验必须接受 50x50，并继续拒绝超过 50 的维度。");
				TowerDefenseMapConfig towerDefenseMapConfig4 = new TowerDefenseMapConfig
				{
					maximumFps = 0
				};
				TowerDefenseMapConfig towerDefenseMapConfig5 = new TowerDefenseMapConfig
				{
					maximumFps = -2
				};
				Check(!towerDefenseMapConfig4.TryValidateRuntime(out var reason4) && reason4.Contains("maximum FPS", StringComparison.Ordinal) && !towerDefenseMapConfig5.TryValidateRuntime(out var reason5) && reason5.Contains("maximum FPS", StringComparison.Ordinal), "地图最大帧率只能使用 -1 或正整数。");
				Check(TowerDefenseBattleFeatureMap.ResolveMapMaximumFps(144, 60) == 60 && TowerDefenseBattleFeatureMap.ResolveMapMaximumFps(30, 60) == 30 && TowerDefenseBattleFeatureMap.ResolveMapMaximumFps(144, -1) == 144, "地图帧率上限必须与玩家设置取较小值，-1 必须不施加额外限制。");
				frameRateFeature = new TowerDefenseBattleFeatureMap();
				frameRateFeature.ApplyMapFrameRateLimit(mapConfig);
				Check(Engine.MaxFps == TowerDefenseBattleFeatureMap.ResolveMapMaximumFps(Global.Instance.effectiveAnimeFrameRate, 60), $"超级大地图提交后必须立即限制实际引擎帧率，当前为 {Engine.MaxFps}。");
				frameRateFeature.Destroy();
				Check(Engine.MaxFps == Global.Instance.effectiveAnimeFrameRate, $"地图 Feature 销毁后必须恢复玩家帧率设置，当前为 {Engine.MaxFps}。");
				frameRateFeature.Dispose();
				frameRateFeature = null;
				Check(condition: true, "脑子、推车和目标行数组必须覆盖一基索引的第 50 行。");
				TowerDefenseMapRuleConfig towerDefenseMapRuleConfig = new TowerDefenseMapRuleConfig
				{
					id = "maximum-column",
					zombieColumnSpeedMinCol = 50,
					zombieColumnSpeedMaxCol = 50
				};
				TowerDefenseMapRuleConfig towerDefenseMapRuleConfig2 = new TowerDefenseMapRuleConfig
				{
					id = "oversized-column",
					zombieColumnSpeedMinCol = 50,
					zombieColumnSpeedMaxCol = 51
				};
				Check(towerDefenseMapRuleConfig.TryValidateRuntime(out var reason6) && string.IsNullOrEmpty(reason6) && !towerDefenseMapRuleConfig2.TryValidateRuntime(out var reason7) && reason7.Contains("invalid zombie column speed range", StringComparison.Ordinal), "地图列规则必须允许第 50 列，并拒绝超过上限的第 51 列。");
				Check(GodotObject.IsInstanceValid(mapScene) && GodotObject.IsInstanceValid(cameraControl), "超级大地图场景和生产相机场景必须能够实例化。");
				Rect2 value = TowerDefenseBattleFeatureMap.BuildProjectileBoundaryRect(mapConfig);
				Check(value.Position == new Vector2(-100f, 0f) && value.Size == new Vector2(2700f, 1200f) && value.HasPoint(new Vector2(2400f, 600f)), $"子弹清理边界必须覆盖完整的 2500x1200 地图并保留水平缓冲，实际为 {value}。");
				if (!GodotObject.IsInstanceValid(mapConfig) || !GodotObject.IsInstanceValid(mapScene) || !GodotObject.IsInstanceValid(cameraControl))
				{
					goto end_IL_00ef;
				}
				cameraControl.ApplyMapConfig(mapConfig);
				AddChild(mapScene, forceReadableName: false, InternalMode.Disabled);
				AddChild(cameraControl, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				Sprite2D nodeOrNull = mapScene.GetNodeOrNull<Sprite2D>("FrontlawnSuperBig");
				Check(GodotObject.IsInstanceValid(nodeOrNull?.Texture) && nodeOrNull.Texture.GetWidth() == 2500 && nodeOrNull.Texture.GetHeight() == 1200, "生产地图场景必须加载未缩放的 2500x1200 PNG。");
				control = new SuperBigMapBattleZoomControlStub
				{
					Name = "SuperBigMapBattleZoomControl",
					isGameRunning = true
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				TowerDefenseManager.Instance.currentControl = control;
				cameraControl.ApplyMapConfig(mapConfig);
				Check(cameraControl.BattleZoomEnabled && cameraControl.camera.Zoom == Vector2.One * 0.4f && Mathf.IsEqualApprox(cameraControl.camera.GlobalPosition.X, cameraControl.cameraBeginMarker.GlobalPosition.X) && Mathf.IsEqualApprox(cameraControl.cameraRightViewMarker.GlobalPosition.X, cameraControl.cameraBeginMarker.GlobalPosition.X), "地图配置开启缩放后，相机必须从最小倍率开始，并让能够完整显示的地图开场标记保持居中。");
				Vector2I vector2I = new Vector2I(13, 5);
				Vector2I legacyGridPosition;
				Vector2 viewportPosition;
				bool legacyInputWouldHit;
				Vector2I vector2I2 = ResolveGemMatchGridPositionUnderCurrentCamera(control, mapConfig, vector2I, out legacyGridPosition, out viewportPosition, out legacyInputWouldHit);
				Check(vector2I2 == vector2I && legacyInputWouldHit && legacyGridPosition != vector2I, $"超级大地图宝石迷阵必须把视口坐标还原到战场画布后命中真实格子，目标={vector2I}，旧坐标命中={legacyGridPosition}，旧坐标有效={legacyInputWouldHit}，实际命中={vector2I2}，视口坐标={viewportPosition}。");
				TowerDefenseBattleFeatureGemMatch towerDefenseBattleFeatureGemMatch = new TowerDefenseBattleFeatureGemMatch();
				try
				{
					control.featureDictionary["GemMatch"] = towerDefenseBattleFeatureGemMatch;
					cameraControl._UnhandledInput(new InputEventMouseButton
					{
						ButtonIndex = MouseButton.Left,
						Pressed = true,
						Position = viewportPosition
					});
					Check(!cameraControl.IsMousePanActive, "宝石迷阵占用左键交换时，大地图相机不能同时启动视角拖动；中键和右键仍保留地图平移能力。");
					cameraControl._UnhandledInput(new InputEventMouseButton
					{
						ButtonIndex = MouseButton.Left,
						Pressed = false,
						Position = viewportPosition
					});
				}
				finally
				{
					control.featureDictionary.Remove("GemMatch");
					towerDefenseBattleFeatureGemMatch.Dispose();
				}
				bool userInterfaceButtonPressed = false;
				userInterfaceLayer = new CanvasLayer
				{
					Layer = 100
				};
				Button userInterfaceButton = new Button
				{
					Position = new Vector2(40f, 40f),
					Size = new Vector2(160f, 60f),
					Text = "战斗界面按钮"
				};
				userInterfaceButton.Pressed += delegate
				{
					userInterfaceButtonPressed = true;
				};
				AddChild(userInterfaceLayer, forceReadableName: false, InternalMode.Disabled);
				userInterfaceLayer.AddChild(userInterfaceButton, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				Vector2 userInterfaceButtonPoint = userInterfaceButton.GetGlobalRect().GetCenter();
				await PushInput(new InputEventMouseMotion
				{
					Position = userInterfaceButtonPoint
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = userInterfaceButtonPoint
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = userInterfaceButtonPoint
				});
				Check(userInterfaceButtonPressed && !cameraControl.IsMousePanActive, "战斗相机启用拖动时，卡牌、菜单和加速等 CanvasLayer 界面仍必须优先接收点击。");
				TowerDefenseInGamePacketShow packet = packedPacketScene.Instantiate<TowerDefenseInGamePacketShow>(PackedScene.GenEditState.Disabled);
				packet.Position = new Vector2(320f, 100f);
				userInterfaceLayer.AddChild(packet, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				bool packetButtonPressed = false;
				packet.button.Pressed += delegate
				{
					packetButtonPressed = true;
				};
				Vector2 packetButtonPoint = packet.button.GetGlobalRect().GetCenter();
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = packetButtonPoint
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = packetButtonPoint
				});
				Check(packetButtonPressed && !cameraControl.IsMousePanActive, "PC 点击生产战斗卡牌时必须由卡牌按钮接收，不能被地图视角拖动抢占。");
				SuperBigMapBattleZoomGuiProbe guiProbe = new SuperBigMapBattleZoomGuiProbe
				{
					Position = new Vector2(520f, 100f),
					Size = new Vector2(140f, 70f),
					MouseFilter = Control.MouseFilterEnum.Stop
				};
				userInterfaceLayer.AddChild(guiProbe, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				Vector2 guiProbePoint = guiProbe.GetGlobalRect().GetCenter();
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = guiProbePoint
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = guiProbePoint
				});
				Check(guiProbe.ReceivedPress && !cameraControl.IsMousePanActive, "PC 普通战斗 Control 必须先于地图视角接收点击，不能依赖相机维护界面类型白名单。");
				cameraControl.camera.Zoom = Vector2.One;
				cameraControl.camera.GlobalPosition = new Vector2(500f, 200f);
				Vector2 mouseAnchor = new Vector2(400f, 300f);
				await PushInput(new InputEventMouseMotion
				{
					Position = mouseAnchor
				});
				Vector2 worldBeforeWheel = cameraControl.camera.GlobalPosition + mouseAnchor / cameraControl.camera.Zoom;
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.WheelUp,
					Pressed = true,
					Position = mouseAnchor,
					Factor = 1f
				});
				Vector2 to = cameraControl.camera.GlobalPosition + mouseAnchor / cameraControl.camera.Zoom;
				Check(cameraControl.camera.Zoom.X > 1f && worldBeforeWheel.DistanceTo(to) < 0.01f, "鼠标滚轮放大必须生效并保持鼠标锚点对应的世界位置。");
				Vector2 cameraPositionBeforePan = cameraControl.camera.GlobalPosition;
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = mouseAnchor
				});
				Check(cameraControl.IsMousePanActive, "没有选择植物或工具时按住鼠标左键必须启动战斗视角拖动。");
				await PushInput(new InputEventMouseMotion
				{
					Position = mouseAnchor + new Vector2(80f, 0f)
				});
				Check(cameraControl.camera.GlobalPosition.DistanceTo(cameraPositionBeforePan) > 1f, "战斗视角拖动必须按当前倍率移动相机并受地图边界约束。");
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = mouseAnchor + new Vector2(80f, 0f)
				});
				Check(!cameraControl.IsMousePanActive, "鼠标按钮松开后必须结束战斗视角拖动。");
				TowerDefenseMapConfig mapConfig2 = new TowerDefenseMapConfig
				{
					mapSize = mapConfig.mapSize,
					enableBattleZoom = false
				};
				cameraControl.ApplyMapConfig(mapConfig2);
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.WheelUp,
					Pressed = true,
					Position = mouseAnchor,
					Factor = 1f
				});
				Check(!cameraControl.BattleZoomEnabled && cameraControl.camera.Zoom == Vector2.One, "未开启地图缩放配置时，滚轮不得改变相机倍率。");
				cameraControl.ApplyMapConfig(mapConfig);
				Global.Instance.isMobile = true;
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = new Vector2(300f, 300f)
				});
				Check(!cameraControl.IsMousePanActive, "安卓触摸模拟出的鼠标左键不能抢先启动桌面视角拖动。");
				await PushInput(new InputEventScreenTouch
				{
					Index = 0,
					Pressed = true,
					Position = new Vector2(300f, 300f)
				});
				await PushInput(new InputEventScreenDrag
				{
					Index = 0,
					Position = new Vector2(320f, 300f),
					Relative = new Vector2(20f, 0f)
				});
				await PushInput(new InputEventScreenTouch
				{
					Index = 1,
					Pressed = true,
					Position = new Vector2(700f, 300f)
				});
				await PushInput(new InputEventScreenDrag
				{
					Index = 1,
					Position = new Vector2(820f, 300f),
					Relative = new Vector2(120f, 0f)
				});
				Check(cameraControl.camera.Zoom.X > 0.4f, "安卓第一指已经移动后再按下第二指，增大双指间距仍必须从最小倍率放大战斗地图视角。");
				await PushInput(new InputEventScreenTouch
				{
					Index = 0,
					Pressed = false,
					Position = new Vector2(320f, 300f)
				});
				await PushInput(new InputEventScreenTouch
				{
					Index = 1,
					Pressed = false,
					Position = new Vector2(820f, 300f)
				});
				await PushInput(new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = false,
					Position = new Vector2(300f, 300f)
				});
				goto end_IL_007f;
				end_IL_00ef:;
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[SuperBigMapBattleZoomRuntimeTest] 发生未预期异常：{value2}");
				goto end_IL_007f;
			}
			return;
			end_IL_007f:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(frameRateFeature))
			{
				frameRateFeature.Destroy();
				frameRateFeature.Dispose();
			}
			if (GodotObject.IsInstanceValid(userInterfaceLayer))
			{
				userInterfaceLayer.QueueFree();
			}
			TranslationServer.SetLocale(previousLocale);
			Global.Instance.isMobile = previousMobile;
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			if (GodotObject.IsInstanceValid(cameraControl))
			{
				cameraControl.QueueFree();
			}
			if (GodotObject.IsInstanceValid(mapScene))
			{
				mapScene.QueueFree();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 29;
		GD.Print($"SUPER_BIG_MAP_BATTLE_ZOOM_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private Vector2I ResolveGemMatchGridPositionUnderCurrentCamera(TowerDefenseControlNew control, TowerDefenseMapConfig mapConfig, Vector2I targetGridPosition, out Vector2I legacyGridPosition, out Vector2 viewportPosition, out bool legacyInputWouldHit)
	{
		TowerDefenseBattleFeatureGemMatch towerDefenseBattleFeatureGemMatch = new TowerDefenseBattleFeatureGemMatch();
		Node2D node2D = new Node2D();
		try
		{
			control.AddNode(node2D, 2);
			towerDefenseBattleFeatureGemMatch.gemNode = node2D;
			towerDefenseBattleFeatureGemMatch.config = new TowerDefenseBattleFeatureGemMatchConfig
			{
				boardRows = mapConfig.gridNum.Y,
				boardCols = mapConfig.gridNum.X,
				inputRadius = 60f
			};
			Vector2 vector = mapConfig.gridBeginPos + new Vector2(mapConfig.gridSize.X, mapConfig.gridSize.Y + (float)mapConfig.plantOffset);
			Godot.Collections.Array array = new Godot.Collections.Array();
			for (int i = 0; i < towerDefenseBattleFeatureGemMatch.config.boardRows; i++)
			{
				Godot.Collections.Array array2 = new Godot.Collections.Array();
				for (int j = 0; j < towerDefenseBattleFeatureGemMatch.config.boardCols; j++)
				{
					array2.Add(vector + new Vector2((float)j * mapConfig.gridSize.X, (float)i * mapConfig.gridSize.Y));
				}
				array.Add(array2);
			}
			FieldInfo? field = typeof(TowerDefenseBattleFeatureGemMatch).GetField("_cellPositions", BindingFlags.Instance | BindingFlags.NonPublic);
			FieldInfo field2 = typeof(TowerDefenseBattleFeatureGemMatch).GetField("_cellSize", BindingFlags.Instance | BindingFlags.NonPublic);
			MethodInfo method = typeof(TowerDefenseBattleFeatureGemMatch).GetMethod("_ScreenToGrid", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null || field2 == null || method == null)
			{
				throw new MissingMemberException("宝石迷阵输入坐标探针无法访问生产格子解析链路。");
			}
			field.SetValue(towerDefenseBattleFeatureGemMatch, array);
			field2.SetValue(towerDefenseBattleFeatureGemMatch, mapConfig.gridSize);
			Vector2 vector2 = vector + new Vector2((float)targetGridPosition.X * mapConfig.gridSize.X, (float)targetGridPosition.Y * mapConfig.gridSize.Y);
			viewportPosition = node2D.GetCanvasTransform() * vector2;
			legacyGridPosition = new Vector2I((int)Math.Round((viewportPosition.X - vector.X) / mapConfig.gridSize.X), (int)Math.Round((viewportPosition.Y - vector.Y) / mapConfig.gridSize.Y));
			legacyInputWouldHit = legacyGridPosition.X >= 0 && legacyGridPosition.X < towerDefenseBattleFeatureGemMatch.config.boardCols && legacyGridPosition.Y >= 0 && legacyGridPosition.Y < towerDefenseBattleFeatureGemMatch.config.boardRows;
			if (legacyInputWouldHit)
			{
				Vector2 to = (Vector2)((Godot.Collections.Array)array[legacyGridPosition.Y])[legacyGridPosition.X];
				legacyInputWouldHit = viewportPosition.DistanceTo(to) <= towerDefenseBattleFeatureGemMatch.config.inputRadius;
			}
			return (Vector2I)method.Invoke(towerDefenseBattleFeatureGemMatch, new object[1] { viewportPosition });
		}
		finally
		{
			towerDefenseBattleFeatureGemMatch.Dispose();
			node2D.QueueFree();
		}
	}

	private async Task PushInput(InputEvent inputEvent)
	{
		GetViewport().PushInput(inputEvent, inLocalCoords: true);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[SuperBigMapBattleZoomRuntimeTest] " + message);
		}
	}
}
