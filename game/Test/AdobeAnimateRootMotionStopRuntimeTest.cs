using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateRootMotionStopRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT";

	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.015f);

	private const int MovePhysicsFrames = 12;

	private const float MovePerPhysicsFrame = 12f;

	private const int CaptureSampleStride = 2;

	private const double LatePausedCentroidTolerance = 2.0;

	private const int CrowdRootCount = 96;

	private const string ProductionZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Rect2I CrowdCaptureRect = new Rect2I(700, 0, 380, 600);

	private AdobeAnimateRenderBackend _originalBackend;

	private int _originalMaxFps;

	private Node2D _movingHost;

	private int _remainingMoves;

	private readonly List<TowerDefenseZombieNormal> _crowdCharacters = new List<TowerDefenseZombieNormal>(96);

	public override async void _Ready()
	{
		int exitCode = 2;
		try
		{
			GD.Print("ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT stage=setup");
			base.ProcessMode = ProcessModeEnum.Always;
			if (Global.Instance == null)
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			_originalMaxFps = Engine.MaxFps;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			Engine.MaxFps = Math.Max(120, Engine.PhysicsTicksPerSecond * 2);
			CanvasLayer canvasLayer = new CanvasLayer
			{
				Layer = -10,
				ProcessMode = ProcessModeEnum.Always
			};
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			ColorRect node = new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(1080f, 600f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			canvasLayer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			Node2D node2D = new Node2D
			{
				ProcessMode = ProcessModeEnum.Pausable
			};
			AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			Camera2D node2 = new Camera2D
			{
				Position = new Vector2(1250f, 600f),
				Zoom = new Vector2(0.4f, 0.4f),
				Enabled = true,
				ProcessMode = ProcessModeEnum.Pausable
			};
			node2D.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
			_movingHost = new Node2D
			{
				Position = new Vector2(1250f, 350f)
			};
			node2D.AddChild(_movingHost, forceReadableName: false, InternalMode.Disabled);
			PackedScene scene = GD.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/PeaShooter.tscn");
			AdobeAnimateSprite sprite = scene?.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(sprite))
			{
				throw new InvalidOperationException("PeaShooter animation scene could not be instantiated.");
			}
			sprite.SetAnimation("Idle");
			sprite.timeScale = 1.0;
			sprite.pause = false;
			_movingHost.AddChild(sprite, forceReadableName: false, InternalMode.Disabled);
			CreateCrowdCharacters(node2D);
			SetPhysicsProcess(enable: false);
			await WaitProcessFrames(6);
			ActivateCrowdAnimations();
			await WaitProcessFrames(30);
			int crowdPixelsBeforePause = await CaptureSignalPixels(CrowdCaptureRect);
			sprite.GetRuntimeCrowdCullingDebugState(out var cached, out var cachedVisible, out var currentVisible, out var currentVisibleWithPrefetch, out var nativeCanvasSuppressedBeforeDispatchSwitch);
			sprite.SetRuntimeManagerDispatchActive(active: false);
			sprite.GetRuntimeCrowdCullingDebugState(out currentVisibleWithPrefetch, out currentVisible, out cachedVisible, out cached, out var nativeCanvasSuppressedAfterDispatchSwitch);
			sprite.SetRuntimeManagerDispatchActive(active: true);
			await WaitProcessFrames(2);
			GD.Print("ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT stage=warmup-complete");
			_remainingMoves = 12;
			SetPhysicsProcess(enable: true);
			while (_remainingMoves > 0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			GD.Print("ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT stage=movement-complete");
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			bool visibleBeforePause;
			using (Image image = GetViewport().GetTexture().GetImage())
			{
				visibleBeforePause = TryMeasureCentroidY(image, ScaleX(image, 390), ScaleX(image, 690), ScaleY(image, 110), ScaleY(image, 500), out var centroidY);
				GD.Print($"{"ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT"} stage=before-pause-capture visible={visibleBeforePause} centroid={centroidY:F4}");
			}
			GetTree().Paused = true;
			GD.Print("ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT stage=tree-paused");
			double minCentroidY = 1.7976931348623157E+308;
			double maxCentroidY = -1.7976931348623157E+308;
			int blankFrames = 0;
			int pausedMinimumFrame = 2147483647;
			int pausedMaximumFrame = -2147483648;
			int pausedMinimumCrowdPixels = 2147483647;
			for (int frame = 0; frame < 30; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				pausedMinimumFrame = Math.Min(pausedMinimumFrame, sprite.frameIndex);
				pausedMaximumFrame = Math.Max(pausedMaximumFrame, sprite.frameIndex);
				using Image image2 = GetViewport().GetTexture().GetImage();
				pausedMinimumCrowdPixels = Math.Min(pausedMinimumCrowdPixels, CountSignalPixels(image2, CrowdCaptureRect));
				if (!TryMeasureCentroidY(image2, ScaleX(image2, 390), ScaleX(image2, 690), ScaleY(image2, 110), ScaleY(image2, 500), out var centroidY2))
				{
					blankFrames++;
					continue;
				}
				minCentroidY = Math.Min(minCentroidY, centroidY2);
				maxCentroidY = Math.Max(maxCentroidY, centroidY2);
				if (frame == 0)
				{
					GD.Print($"{"ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT"} stage=first-capture centroid={centroidY2:F4}");
				}
			}
			double range = ((maxCentroidY >= minCentroidY) ? (maxCentroidY - minCentroidY) : (1.0 / 0.0));
			GD.Print($"{"ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT"} stage=preexisting-pause-capture-complete range={range:F4}");
			CanvasLayer transientMenuLayer = new CanvasLayer
			{
				Layer = 100,
				ProcessMode = ProcessModeEnum.Always
			};
			AddChild(transientMenuLayer, forceReadableName: false, InternalMode.Disabled);
			Node2D node2D2 = new Node2D
			{
				Position = new Vector2(120f, 120f),
				ProcessMode = ProcessModeEnum.Always
			};
			transientMenuLayer.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
			AdobeAnimateSprite adobeAnimateSprite = scene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				throw new InvalidOperationException("Transient pause-menu animation scene could not be instantiated.");
			}
			adobeAnimateSprite.SetAnimation("Idle");
			node2D2.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
			await WaitProcessFrames(12);
			GetTree().Paused = false;
			transientMenuLayer.QueueFree();
			int resumedMinimumFrame = 2147483647;
			int resumedMaximumFrame = -2147483648;
			int resumedMinimumCrowdPixels = 2147483647;
			for (int frame = 0; frame < 30; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				resumedMinimumFrame = Math.Min(resumedMinimumFrame, sprite.frameIndex);
				resumedMaximumFrame = Math.Max(resumedMaximumFrame, sprite.frameIndex);
				using Image image3 = GetViewport().GetTexture().GetImage();
				resumedMinimumCrowdPixels = Math.Min(resumedMinimumCrowdPixels, CountSignalPixels(image3, CrowdCaptureRect));
			}
			GetTree().Paused = true;
			CanvasLayer canvasLayer2 = new CanvasLayer
			{
				Layer = 101,
				ProcessMode = ProcessModeEnum.Always
			};
			AddChild(canvasLayer2, forceReadableName: false, InternalMode.Disabled);
			Node2D latePausedHost = new Node2D
			{
				Position = new Vector2(220f, 180f),
				ProcessMode = ProcessModeEnum.Always
			};
			canvasLayer2.AddChild(latePausedHost, forceReadableName: false, InternalMode.Disabled);
			AdobeAnimateSprite latePausedSprite = scene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(latePausedSprite))
			{
				throw new InvalidOperationException("Late paused PeaShooter animation could not be instantiated.");
			}
			latePausedSprite.SetAnimation("Idle");
			latePausedSprite.timeScale = 0.0;
			latePausedSprite.pause = false;
			latePausedHost.AddChild(latePausedSprite, forceReadableName: false, InternalMode.Disabled);
			await WaitProcessFrames(12);
			latePausedHost.Position += new Vector2(0f, 72f);
			latePausedSprite.NotifyAncestorTransformChangedForRender(changedInPhysicsFrame: true);
			await WaitProcessFrames(6);
			double lateMinCentroidY = 1.7976931348623157E+308;
			double lateMaxCentroidY = -1.7976931348623157E+308;
			int lateBlankFrames = 0;
			for (int frame = 0; frame < 30; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				using Image image4 = GetViewport().GetTexture().GetImage();
				if (!TryMeasureCentroidY(image4, ScaleX(image4, 50), ScaleX(image4, 360), ScaleY(image4, 100), ScaleY(image4, 560), out var centroidY3))
				{
					lateBlankFrames++;
					continue;
				}
				lateMinCentroidY = Math.Min(lateMinCentroidY, centroidY3);
				lateMaxCentroidY = Math.Max(lateMaxCentroidY, centroidY3);
			}
			double num = ((lateMaxCentroidY >= lateMinCentroidY) ? (lateMaxCentroidY - lateMinCentroidY) : (1.0 / 0.0));
			float num2 = (float)GetViewport().GetTexture().GetHeight() / 600f;
			bool flag = visibleBeforePause && nativeCanvasSuppressedBeforeDispatchSwitch && !nativeCanvasSuppressedAfterDispatchSwitch && blankFrames == 0 && pausedMinimumFrame == pausedMaximumFrame && crowdPixelsBeforePause >= 100 && pausedMinimumCrowdPixels >= crowdPixelsBeforePause / 5 && resumedMinimumFrame < resumedMaximumFrame && resumedMinimumCrowdPixels >= crowdPixelsBeforePause / 5 && range <= 0.75 * (double)num2 && lateBlankFrames == 0 && num <= 2.0 * (double)num2;
			GD.Print($"{"ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT"} passed={flag} blank={blankFrames} lateBlank={lateBlankFrames} nativeBeforeSwitch={nativeCanvasSuppressedBeforeDispatchSwitch} nativeAfterSwitch={nativeCanvasSuppressedAfterDispatchSwitch} crowdBefore={crowdPixelsBeforePause} pausedCrowdMin={pausedMinimumCrowdPixels} resumedCrowdMin={resumedMinimumCrowdPixels} pausedFrames={pausedMinimumFrame}..{pausedMaximumFrame} resumedFrames={resumedMinimumFrame}..{resumedMaximumFrame} range={range:F4} lateRange={num:F4} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT"} exception={value}");
		}
		finally
		{
			GetTree().Paused = false;
			SetPhysicsProcess(enable: false);
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			Engine.MaxFps = _originalMaxFps;
		}
		GetTree().Quit(exitCode);
	}

	private void CreateCrowdCharacters(Node battleLayer)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			throw new InvalidOperationException("Production normal zombie scene could not be loaded.");
		}
		for (int i = 0; i < 96; i++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = packedScene.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(towerDefenseZombieNormal))
			{
				throw new InvalidOperationException($"Production crowd zombie could not be instantiated at index {i}.");
			}
			towerDefenseZombieNormal.Name = $"PauseCrowdZombie{i}";
			towerDefenseZombieNormal.inGame = false;
			towerDefenseZombieNormal.skipDestroySet = true;
			towerDefenseZombieNormal.ProcessMode = ProcessModeEnum.Pausable;
			towerDefenseZombieNormal.Position = new Vector2(1650f + (float)(i % 8) * 90f, 120f + (float)(i / 8 % 12) * 80f);
			towerDefenseZombieNormal.Scale = new Vector2(0.7f, 0.7f);
			battleLayer.AddChild(towerDefenseZombieNormal, forceReadableName: false, InternalMode.Disabled);
			_crowdCharacters.Add(towerDefenseZombieNormal);
		}
	}

	private void ActivateCrowdAnimations()
	{
		for (int i = 0; i < _crowdCharacters.Count; i++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = _crowdCharacters[i];
			if (!GodotObject.IsInstanceValid(towerDefenseZombieNormal?.sprite))
			{
				throw new InvalidOperationException($"Production crowd zombie animation is unavailable at index {i}.");
			}
			towerDefenseZombieNormal.sprite.ProcessMode = ProcessModeEnum.Inherit;
			towerDefenseZombieNormal.sprite.pause = false;
			towerDefenseZombieNormal.sprite.timeScale = 1.0;
			towerDefenseZombieNormal.sprite.RefreshProcessScheduling();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_remainingMoves <= 0 || !GodotObject.IsInstanceValid(_movingHost))
		{
			SetPhysicsProcess(enable: false);
			return;
		}
		_movingHost.Position += new Vector2(0f, 12f);
		if (_remainingMoves == 12)
		{
			GD.Print("ADOBE_ANIMATE_ROOT_MOTION_STOP_RESULT stage=movement-started");
		}
		_remainingMoves--;
		if (_remainingMoves <= 0)
		{
			SetPhysicsProcess(enable: false);
		}
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<int> CaptureSignalPixels(Rect2I area)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		return CountSignalPixels(image, area);
	}

	private static int CountSignalPixels(Image image, Rect2I area)
	{
		int num = 0;
		int num2 = Math.Min(image.GetWidth(), area.End.X);
		int num3 = Math.Min(image.GetHeight(), area.End.Y);
		for (int i = Math.Max(0, area.Position.Y); i < num3; i += 2)
		{
			for (int j = Math.Max(0, area.Position.X); j < num2; j += 2)
			{
				Color pixel = image.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - BackgroundColor.R) + Mathf.Abs(pixel.G - BackgroundColor.G) + Mathf.Abs(pixel.B - BackgroundColor.B) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static int ScaleX(Image image, int logicalX)
	{
		return Mathf.RoundToInt((float)(logicalX * image.GetWidth()) / 1080f);
	}

	private static int ScaleY(Image image, int logicalY)
	{
		return Mathf.RoundToInt((float)(logicalY * image.GetHeight()) / 600f);
	}

	private static bool TryMeasureCentroidY(Image image, int x0, int x1, int y0, int y1, out double centroidY)
	{
		double num = 0.0;
		double num2 = 0.0;
		int num3 = Math.Min(x1, image.GetWidth());
		int num4 = Math.Min(y1, image.GetHeight());
		for (int i = Math.Max(0, y0); i < num4; i += 2)
		{
			for (int j = Math.Max(0, x0); j < num3; j += 2)
			{
				Color pixel = image.GetPixel(j, i);
				double num5 = Math.Abs(pixel.R - BackgroundColor.R) + Math.Abs(pixel.G - BackgroundColor.G) + Math.Abs(pixel.B - BackgroundColor.B);
				if (!(num5 <= 0.08))
				{
					num += (double)i * num5;
					num2 += num5;
				}
			}
		}
		centroidY = ((num2 > 1.0) ? (num / num2) : 0.0);
		return num2 > 1.0;
	}
}
