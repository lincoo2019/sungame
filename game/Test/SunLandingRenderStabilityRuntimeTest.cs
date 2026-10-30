using System;
using System.Threading.Tasks;
using Godot;

public partial class SunLandingRenderStabilityRuntimeTest : Node
{
	private const string ResultMarker = "SUN_LANDING_RENDER_STABILITY_RESULT";

	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.015f);

	private const double LandingCentroidTolerance = 1.25;

	private AdobeAnimateRenderBackend _originalBackend;

	private int _originalMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		try
		{
			_ = 6;
			try
			{
				if (Global.Instance == null)
				{
					throw new InvalidOperationException("Global autoload is unavailable.");
				}
				_originalBackend = Global.Instance.adobeAnimateRenderBackend;
				_originalMaxFps = Engine.MaxFps;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				Engine.MaxFps = Math.Max(120, Engine.PhysicsTicksPerSecond * 2);
				ColorRect node = new ColorRect
				{
					Color = BackgroundColor,
					Position = Vector2.Zero,
					Size = new Vector2(1080f, 600f),
					MouseFilter = Control.MouseFilterEnum.Ignore,
					ZIndex = -4096
				};
				AddChild(node, forceReadableName: false, InternalMode.Disabled);
				TowerDefenseSunBase anchorSun = PopSun();
				if (!GodotObject.IsInstanceValid(anchorSun))
				{
					throw new InvalidOperationException("Anchor sun could not be popped from the runtime pool.");
				}
				anchorSun.Position = new Vector2(250f, 250f);
				anchorSun.sprite.timeScale = 1.0;
				anchorSun.Init(25L, TowerDefenseEnum.SUN_MOVING_METHOD.LAND, 0.0, Vector2.Zero);
				TowerDefenseSunBase sun = PopSun();
				if (!GodotObject.IsInstanceValid(sun))
				{
					throw new InvalidOperationException("Default sun could not be popped from the runtime pool.");
				}
				sun.Position = new Vector2(540f, 150f);
				await WaitProcessFrames(20);
				if (!GodotObject.IsInstanceValid(sun.sprite))
				{
					throw new InvalidOperationException("Sun animation sprite is unavailable.");
				}
				if (GodotObject.IsInstanceValid(sun.light) || GodotObject.IsInstanceValid(sun.GetNodeOrNull<PointLight2D>("SunSprite/Light")))
				{
					throw new InvalidOperationException("Sun drop must not retain a PointLight2D render cost.");
				}
				(double, int, int) tuple = await DropAndMeasure(sun, 180f);
				double firstRange = tuple.Item1;
				int firstFlightBlankFrames = tuple.Item2;
				int firstLandingBlankFrames = tuple.Item3;
				ulong firstInstanceId = sun.GetInstanceId();
				ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SUN, sun);
				await WaitProcessFrames(3);
				sun = PopSun();
				if (!GodotObject.IsInstanceValid(sun))
				{
					throw new InvalidOperationException("Default sun could not be reused from the runtime pool.");
				}
				bool reusedSameInstance = sun.GetInstanceId() == firstInstanceId;
				sun.Position = new Vector2(540f, 150f);
				var (reusedRange, reusedFlightBlankFrames, reusedLandingBlankFrames) = await DropAndMeasure(sun, 180f);
				ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SUN, sun);
				await WaitProcessFrames(3);
				ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SUN, anchorSun);
				await WaitProcessFrames(3);
				int num = await MeasureTopologyChurnContinuity();
				bool flag = reusedSameInstance && firstFlightBlankFrames == 0 && firstLandingBlankFrames == 0 && reusedFlightBlankFrames == 0 && reusedLandingBlankFrames == 0 && num == 0 && firstRange <= 1.25 && reusedRange <= 1.25;
				GD.Print($"{"SUN_LANDING_RENDER_STABILITY_RESULT"} passed={flag} reusedSame={reusedSameInstance} firstFlightBlank={firstFlightBlankFrames} firstLandingBlank={firstLandingBlankFrames} reusedFlightBlank={reusedFlightBlankFrames} reusedLandingBlank={reusedLandingBlankFrames} churnBlank={num} firstRange={firstRange:F4} reusedRange={reusedRange:F4} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag) ? 2 : 0);
			}
			catch (Exception value)
			{
				GD.PrintErr($"{"SUN_LANDING_RENDER_STABILITY_RESULT"} exception={value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
				await WaitProcessFrames(3);
			}
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			Engine.MaxFps = _originalMaxFps;
		}
		GetTree().Quit(exitCode);
	}

	private TowerDefenseSunBase PopSun()
	{
		return ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.SUN, this) as TowerDefenseSunBase;
	}

	private async Task<(double Range, int FlightBlankFrames, int LandingBlankFrames)> DropAndMeasure(TowerDefenseSunBase sun, float landingHeight)
	{
		sun.autoCollect = false;
		sun.sprite.SetAnimation("Idle");
		sun.sprite.timeScale = 1.0;
		sun.Init(25L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, landingHeight, new Vector2(0f, -240f), 980.0);
		int flightBlankFrames = 0;
		bool hasVisibleFlightFrame = false;
		int remainingProcessFrames = 360;
		while (!sun.over && remainingProcessFrames-- > 0)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			if (sun.sprite.Scale.X < 0.25f)
			{
				continue;
			}
			using Image image = GetViewport().GetTexture().GetImage();
			if (TryMeasureCentroidY(image, 450, 630, 40, 500, out var _))
			{
				hasVisibleFlightFrame = true;
			}
			else if (hasVisibleFlightFrame)
			{
				flightBlankFrames++;
				sun.sprite.GetRuntimeCrowdCullingDebugState(out var cached, out var cachedVisible, out var currentVisible, out var currentVisibleWithPrefetch, out var nativeCanvasSuppressed);
				AdobeAnimateCrowdRenderState state;
				AdobeAnimateCrowdRenderStateResult value = sun.sprite.TryBuildCrowdRenderState(out state);
				AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				GD.Print($"{"SUN_LANDING_RENDER_STABILITY_RESULT"} blankFrame process={Engine.GetProcessFrames()} physics={Engine.GetPhysicsFrames()} scale={sun.sprite.Scale} localPosition={sun.sprite.Position} frame={sun.sprite.frameIndex} elapsed={sun.sprite.elapsedTimer:F4} stateResult={value} stateMode={state?.Mode} cullingCached={cached} cachedVisible={cachedVisible} currentVisible={currentVisible} prefetchVisible={currentVisibleWithPrefetch} nativeSuppressed={nativeCanvasSuppressed} crowdRoots={aggregateRenderStats.CrowdRoots} fallbackRoots={aggregateRenderStats.FallbackRoots}");
			}
		}
		if (!sun.over)
		{
			throw new InvalidOperationException("Sun did not reach its landing state.");
		}
		if (!hasVisibleFlightFrame)
		{
			throw new InvalidOperationException("Sun never became visible during flight.");
		}
		await WaitPhysicsFrames(3);
		if (sun.moveComponent.HasActiveMovement)
		{
			throw new InvalidOperationException("Sun movement remained active after landing.");
		}
		if (!Mathf.IsEqualApprox(sun.sprite.Position.Y, landingHeight))
		{
			throw new InvalidOperationException($"Sun landed at {sun.sprite.Position.Y:F4}, expected {landingHeight:F4}.");
		}
		double minCentroidY = 1.7976931348623157E+308;
		double maxCentroidY = -1.7976931348623157E+308;
		int landingBlankFrames = 0;
		for (int frame = 0; frame < 60; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			using Image image2 = GetViewport().GetTexture().GetImage();
			if (!TryMeasureCentroidY(image2, 470, 610, 250, 430, out var centroidY2))
			{
				landingBlankFrames++;
				continue;
			}
			minCentroidY = Math.Min(minCentroidY, centroidY2);
			maxCentroidY = Math.Max(maxCentroidY, centroidY2);
		}
		return (Range: (maxCentroidY >= minCentroidY) ? (maxCentroidY - minCentroidY) : (1.0 / 0.0), FlightBlankFrames: flightBlankFrames, LandingBlankFrames: landingBlankFrames);
	}

	private async Task<int> MeasureTopologyChurnContinuity()
	{
		TowerDefenseSunBase stableSun = PopSun();
		if (!GodotObject.IsInstanceValid(stableSun))
		{
			throw new InvalidOperationException("Stable sun could not be popped for topology churn.");
		}
		stableSun.Position = new Vector2(300f, 250f);
		stableSun.sprite.timeScale = 1.0;
		stableSun.Init(25L, TowerDefenseEnum.SUN_MOVING_METHOD.LAND, 0.0, Vector2.Zero);
		await WaitProcessFrames(20);
		int blankFrames = 0;
		for (int cycle = 0; cycle < 12; cycle++)
		{
			TowerDefenseSunBase churnSun = PopSun();
			if (!GodotObject.IsInstanceValid(churnSun))
			{
				throw new InvalidOperationException($"Churn sun {cycle} could not be popped.");
			}
			churnSun.Position = new Vector2(760f, 250f);
			churnSun.sprite.timeScale = 1.0;
			churnSun.Init(25L, TowerDefenseEnum.SUN_MOVING_METHOD.LAND, 0.0, Vector2.Zero);
			int num = blankFrames;
			blankFrames = num + await MeasureRegionBlankFrames(6, 210, 390, 150, 360);
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SUN, churnSun);
			num = blankFrames;
			blankFrames = num + await MeasureRegionBlankFrames(4, 210, 390, 150, 360);
		}
		ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SUN, stableSun);
		await WaitProcessFrames(3);
		return blankFrames;
	}

	private async Task<int> MeasureRegionBlankFrames(int frameCount, int x0, int x1, int y0, int y1)
	{
		int blankFrames = 0;
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			using Image image = GetViewport().GetTexture().GetImage();
			if (!TryMeasureCentroidY(image, x0, x1, y0, y1, out var _))
			{
				blankFrames++;
			}
		}
		return blankFrames;
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
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
