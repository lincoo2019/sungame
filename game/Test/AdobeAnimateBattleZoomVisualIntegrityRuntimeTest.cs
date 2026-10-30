using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateBattleZoomVisualIntegrityRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_BATTLE_ZOOM_VISUAL_INTEGRITY_RESULT";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Wallnut.tscn";

	private const string FirenutScenePath = "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Firenut.tscn";

	private const int ZoomRoundTripCount = 12;

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f);

	private static readonly Rect2I[] CaptureRegions = new Rect2I[4]
	{
		new Rect2I(80, 165, 150, 180),
		new Rect2I(280, 165, 150, 180),
		new Rect2I(480, 165, 150, 180),
		new Rect2I(680, 165, 150, 180)
	};

	private AdobeAnimateRenderBackend _originalBackend;

	private int _originalMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<AdobeAnimateSprite> sprites = new List<AdobeAnimateSprite>(CaptureRegions.Length);
		try
		{
			base.ProcessMode = ProcessModeEnum.Always;
			if (Global.Instance == null)
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			_originalMaxFps = Engine.MaxFps;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			Engine.MaxFps = 120;
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
			Node2D node2D = new Node2D();
			AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			Camera2D camera = new Camera2D
			{
				Position = new Vector2(450f, 270f),
				Zoom = Vector2.One,
				Enabled = true
			};
			node2D.AddChild(camera, forceReadableName: false, InternalMode.Disabled);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Wallnut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Firenut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			if (!GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(packedScene2))
			{
				throw new InvalidOperationException("Production Wallnut or Firenut animation scene is unavailable.");
			}
			for (int i = 0; i < CaptureRegions.Length; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = ((i % 2 == 0) ? packedScene : packedScene2).Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
				adobeAnimateSprite.Position = CaptureRegions[i].GetCenter();
				adobeAnimateSprite.SetAnimation("Idle");
				adobeAnimateSprite.timeScale = 0.0;
				node2D.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
				sprites.Add(adobeAnimateSprite);
			}
			await WaitFrames(24);
			long[][] baseline = await CaptureVisualSamples();
			int visibleRoundTrips = 0;
			int identityMatches = 0;
			int minimumPixels = 2147483647;
			for (int roundTrip = 0; roundTrip < 12; roundTrip++)
			{
				camera.Zoom = ((roundTrip % 2 == 0) ? (Vector2.One * 2f) : (Vector2.One * 0.4f));
				camera.Position = ((roundTrip % 2 == 0) ? new Vector2(700f, 270f) : new Vector2(200f, 270f));
				AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
				await WaitPhysicsFrames(2);
				camera.Zoom = Vector2.One;
				camera.Position = new Vector2(450f, 270f);
				AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
				await WaitPhysicsFrames(2);
				long[][] array = await CaptureVisualSamples();
				bool flag = true;
				bool flag2 = true;
				for (int j = 0; j < array.Length; j++)
				{
					minimumPixels = Math.Min(minimumPixels, (int)array[j][0]);
					flag &= baseline[j][0] > 0 && array[j][0] >= baseline[j][0] / 3;
					flag2 &= SamplesHaveSameIdentity(baseline[j], array[j]);
				}
				if (flag)
				{
					visibleRoundTrips++;
				}
				if (flag2)
				{
					identityMatches++;
				}
			}
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			bool flag3 = Array.TrueForAll(baseline, (long[] sample) => sample[0] > 0) && visibleRoundTrips == 12 && identityMatches == 12 && minimumPixels > 0 && aggregateRenderStats.CrowdRoots == CaptureRegions.Length && aggregateRenderStats.FallbackRoots == 0;
			GD.Print($"{"ADOBE_ANIMATE_BATTLE_ZOOM_VISUAL_INTEGRITY_RESULT"} passed={flag3} visible={visibleRoundTrips}/{12} identity={identityMatches}/{12} minimumPixels={minimumPixels} baseline={DescribeSamples(baseline)} crowd={aggregateRenderStats.CrowdRoots} fallback={aggregateRenderStats.FallbackRoots} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag3) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_BATTLE_ZOOM_VISUAL_INTEGRITY_RESULT"} exception={value}");
		}
		finally
		{
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			Engine.MaxFps = _originalMaxFps;
			foreach (AdobeAnimateSprite item in sprites)
			{
				if (GodotObject.IsInstanceValid(item))
				{
					item.QueueFree();
				}
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

	private async Task WaitPhysicsFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<long[][]> CaptureVisualSamples()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		long[][] array = new long[CaptureRegions.Length][];
		for (int i = 0; i < CaptureRegions.Length; i++)
		{
			array[i] = SampleRegion(image, CaptureRegions[i]);
		}
		return array;
	}

	private static long[] SampleRegion(Image image, Rect2I region)
	{
		long[] array = new long[4];
		int num = Math.Min(region.End.X, image.GetWidth());
		int num2 = Math.Min(region.End.Y, image.GetHeight());
		for (int i = Math.Max(0, region.Position.Y); i < num2; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num; j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (!(Math.Abs(pixel.R - BackgroundColor.R) + Math.Abs(pixel.G - BackgroundColor.G) + Math.Abs(pixel.B - BackgroundColor.B) <= 0.08f))
				{
					array[0]++;
					array[1] += Mathf.RoundToInt(pixel.R * 255f);
					array[2] += Mathf.RoundToInt(pixel.G * 255f);
					array[3] += Mathf.RoundToInt(pixel.B * 255f);
				}
			}
		}
		return array;
	}

	private static bool SamplesHaveSameIdentity(long[] baseline, long[] current)
	{
		if (baseline[0] <= 0 || current[0] <= 0)
		{
			return false;
		}
		for (int i = 1; i <= 3; i++)
		{
			double num = (double)baseline[i] / (double)baseline[0];
			double num2 = (double)current[i] / (double)current[0];
			if (Math.Abs(num - num2) > 12.0)
			{
				return false;
			}
		}
		return true;
	}

	private static string DescribeSamples(long[][] samples)
	{
		string[] array = new string[samples.Length];
		for (int i = 0; i < samples.Length; i++)
		{
			long num = Math.Max(1L, samples[i][0]);
			array[i] = $"{samples[i][0]}:{samples[i][1] / num},{samples[i][2] / num},{samples[i][3] / num}";
		}
		return string.Join('|', array);
	}
}
