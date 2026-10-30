using System;
using System.Threading.Tasks;
using Godot;

public partial class BulletFieldCameraPanContinuityRuntimeTest : Node
{
	private const string ResultMarker = "BULLET_FIELD_CAMERA_PAN_CONTINUITY_RESULT";

	private const string MapConfigPath = "res://Asset/Config/Map/TreasureIslandFrontLawn/Config/TreasureIslandFrontlawnMapTreasureIslandFrontLawn.tres";

	private const string CameraScenePath = "res://Registry/Battle/Feature/Camera/Control/TowerDefenseCameraControl.tscn";

	private const int PanRoundTripCount = 8;

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f);

	private static readonly Rect2I BulletCaptureRegion = new Rect2I(380, 230, 140, 90);

	private AdobeAnimateRenderBackend _originalBackend;

	public override async void _Ready()
	{
		int exitCode = 2;
		BulletField bulletField = null;
		TowerDefenseCameraControl cameraControl = null;
		CanvasLayer cameraLayer = null;
		AdobeAnimateSprite cullingProbe = null;
		try
		{
			base.ProcessMode = ProcessModeEnum.Always;
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			CanvasLayer canvasLayer = new CanvasLayer
			{
				Layer = -10
			};
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			ColorRect node = new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(900f, 540f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			canvasLayer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			cameraControl = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Camera/Control/TowerDefenseCameraControl.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseCameraControl>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(cameraControl))
			{
				throw new InvalidOperationException("The production battle camera scene is unavailable.");
			}
			cameraLayer = new CanvasLayer
			{
				Layer = -1,
				FollowViewportEnabled = true
			};
			AddChild(cameraLayer, forceReadableName: false, InternalMode.Disabled);
			cameraLayer.AddChild(cameraControl, forceReadableName: false, InternalMode.Disabled);
			Camera2D camera = cameraControl.camera;
			camera.Zoom = Vector2.One;
			camera.GlobalPosition = Vector2.Zero;
			CanvasLayer canvasLayer2 = new CanvasLayer
			{
				FollowViewportEnabled = true
			};
			AddChild(canvasLayer2, forceReadableName: false, InternalMode.Disabled);
			Node2D node2D = new Node2D
			{
				YSortEnabled = true
			};
			canvasLayer2.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			bulletField = new BulletField
			{
				Name = "BulletFieldCameraPanProbe"
			};
			node2D.AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseProjectileRegistry.Init();
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData(new StringName("FirePea")).BuildConfig();
			if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig))
			{
				throw new InvalidOperationException("The production FirePea projectile config is unavailable.");
			}
			TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/TreasureIslandFrontLawn/Config/TreasureIslandFrontlawnMapTreasureIslandFrontLawn.tres", null, ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(towerDefenseMapConfig))
			{
				throw new InvalidOperationException("The production Treasure Island map config is unavailable.");
			}
			cullingProbe = towerDefenseProjectileConfig.projectileScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			cullingProbe.Position = new Vector2(450f, 390f);
			cullingProbe.SetAnimation("Idle");
			cullingProbe.ProcessMode = ProcessModeEnum.Always;
			cullingProbe.timeScale = 0.0;
			node2D.AddChild(cullingProbe, forceReadableName: false, InternalMode.Disabled);
			Rect2 mapBoundary = TowerDefenseBattleFeatureMap.BuildProjectileBoundaryRect(towerDefenseMapConfig);
			int num = bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, new Vector2(450f, 270f), Vector2.Zero, 0.0, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, new Vector2I(3, 3), 3, mapBoundary);
			int num2 = bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, new Vector2(1350f, 270f), Vector2.Zero, 0.0, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, new Vector2I(9, 3), 3, mapBoundary);
			if (num < 0 || num2 < 0)
			{
				throw new InvalidOperationException($"Production FirePea projectiles did not enter BulletField: left={num}, right={num2}.");
			}
			bulletField.UpdateSimulation(0.0, Engine.GetPhysicsFrames());
			bulletField.PublishRenderState(0.0, Engine.GetPhysicsFrames());
			await WaitFrames(8);
			int baselineBulletPixels = await CountForegroundPixels(BulletCaptureRegion);
			int minimumLeftBulletPixels = baselineBulletPixels;
			int minimumRightBulletPixels = 2147483647;
			for (int roundTrip = 0; roundTrip < 8; roundTrip++)
			{
				camera.GlobalPosition = new Vector2(900f, 0f);
				bulletField.UpdateSimulation(0.0, Engine.GetPhysicsFrames());
				bulletField.PublishRenderState(0.0, Engine.GetPhysicsFrames());
				await WaitFrames(2);
				int val = minimumRightBulletPixels;
				minimumRightBulletPixels = Math.Min(val, await CountForegroundPixels(BulletCaptureRegion));
				camera.GlobalPosition = Vector2.Zero;
				bulletField.UpdateSimulation(0.0, Engine.GetPhysicsFrames());
				bulletField.PublishRenderState(0.0, Engine.GetPhysicsFrames());
				await WaitFrames(2);
				val = minimumLeftBulletPixels;
				minimumLeftBulletPixels = Math.Min(val, await CountForegroundPixels(BulletCaptureRegion));
			}
			camera.GlobalPosition = Vector2.Zero;
			AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
			await WaitPhysicsFrames(2);
			await WaitFrames(2);
			cullingProbe.GetRuntimeCrowdCullingDebugState(out var visibleCached, out var cachedVisibleBeforeExit, out var _, out var currentVisibleWithPrefetch, out var nativeCanvasSuppressed);
			camera.GlobalPosition = new Vector2(900f, 0f);
			AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
			await WaitPhysicsFrames(2);
			await WaitFrames(2);
			cullingProbe.GetRuntimeCrowdCullingDebugState(out var offscreenCached, out var cachedVisibleOffscreen, out var currentVisibleOffscreen, out nativeCanvasSuppressed, out currentVisibleWithPrefetch);
			AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
			await WaitPhysicsFrames(1);
			await WaitFrames(1);
			camera.GlobalPosition = Vector2.Zero;
			await WaitPhysicsFrames(1);
			await WaitFrames(1);
			cullingProbe.GetRuntimeCrowdCullingDebugState(out var returnedCached, out var cachedVisibleAfterReturn, out var currentVisibleAfterReturn, out currentVisibleWithPrefetch, out var nativeCanvasSuppressedAfterReturn);
			int num3 = await CountForegroundPixels(BulletCaptureRegion);
			bool flag = visibleCached && cachedVisibleBeforeExit && offscreenCached && !cachedVisibleOffscreen && !currentVisibleOffscreen;
			bool flag2 = returnedCached && cachedVisibleAfterReturn && currentVisibleAfterReturn;
			bool flag3 = bulletField.ActiveCount == 2 && bulletField.GetStaticVisibleInstanceCountForTest() == 0 && bulletField.GetAnimatedMeshActiveInstanceCountForTest() == 2 && bulletField.GetAnimatedMeshVisibleInstanceCountForTest() == 2;
			bool flag4 = baselineBulletPixels > 0 && minimumLeftBulletPixels > 0 && minimumRightBulletPixels > 0 && num3 > 0;
			bool flag5 = flag3 && flag4 && flag && flag2;
			GD.Print($"{"BULLET_FIELD_CAMERA_PAN_CONTINUITY_RESULT"} passed={flag5} animatedRoute={flag3} visualContinuity={flag4} cullingPrecondition={flag} cullingRecovered={flag2} active={bulletField.ActiveCount} animatedVisible={bulletField.GetAnimatedMeshVisibleInstanceCountForTest()} boundary={mapBoundary} baselineBulletPixels={baselineBulletPixels} minimumLeftBulletPixels={minimumLeftBulletPixels} minimumRightBulletPixels={minimumRightBulletPixels} returnedBulletPixels={num3} returnedCached={returnedCached} cachedVisibleAfterReturn={cachedVisibleAfterReturn} currentVisibleAfterReturn={currentVisibleAfterReturn} nativeSuppressedAfterReturn={nativeCanvasSuppressedAfterReturn} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag5) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"BULLET_FIELD_CAMERA_PAN_CONTINUITY_RESULT"} exception={value}");
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			if (GodotObject.IsInstanceValid(cullingProbe))
			{
				cullingProbe.QueueFree();
			}
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			if (GodotObject.IsInstanceValid(cameraControl))
			{
				cameraControl.QueueFree();
			}
			if (GodotObject.IsInstanceValid(cameraLayer))
			{
				cameraLayer.QueueFree();
			}
		}
		GetTree().Quit(exitCode);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<int> CountForegroundPixels(Rect2I region)
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		int num = 0;
		for (int i = region.Position.Y; i < region.End.Y; i++)
		{
			for (int j = region.Position.X; j < region.End.X; j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (Math.Abs(pixel.R - BackgroundColor.R) + Math.Abs(pixel.G - BackgroundColor.G) + Math.Abs(pixel.B - BackgroundColor.B) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}
}
