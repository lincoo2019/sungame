using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

public sealed partial class AdobeAnimateGpuGraphCompactionContinuityRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_GPU_GRAPH_COMPACTION_CONTINUITY_RESULT";

	private const string PrefixAnimationScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/ZombieDancer.tscn";

	private const string RetainedAnimationScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn";

	private const int OldRootCount = 4;

	private const int NewRootCount = 2;

	private const int SampleFrameCount = 12;

	private const int HotPathWarmupBatchCount = 240;

	private const int HotPathMeasuredBatchCount = 1200;

	private const int HotPathRootCount = 1000;

	private const float MinimumPixelRatio = 0.65f;

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f);

	private AdobeAnimateRenderBackend _originalBackend;

	public override async void _Ready()
	{
		int exitCode = 2;
		AdobeAnimateSprite prefixSprite = null;
		List<AdobeAnimateSprite> oldSprites = new List<AdobeAnimateSprite>(4);
		List<AdobeAnimateSprite> newSprites = new List<AdobeAnimateSprite>(2);
		List<Rect2I> oldRegions = new List<Rect2I>(4);
		List<Rect2I> newRegions = new List<Rect2I>(2);
		try
		{
			_ = 8;
			try
			{
				if (!GodotObject.IsInstanceValid(Global.Instance))
				{
					throw new InvalidOperationException("Global autoload is unavailable.");
				}
				_originalBackend = Global.Instance.adobeAnimateRenderBackend;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				AddChild(new ColorRect
				{
					Color = BackgroundColor,
					Position = Vector2.Zero,
					Size = new Vector2(960f, 560f),
					MouseFilter = Control.MouseFilterEnum.Ignore
				}, forceReadableName: false, InternalMode.Disabled);
				Node2D animationHost = new Node2D
				{
					Name = "AnimationHost"
				};
				AddChild(animationHost, forceReadableName: false, InternalMode.Disabled);
				PackedScene scene = LoadAnimationScene("res://Asset/Anime/Character/Zombie/Chapter2/Dancer/ZombieDancer.tscn");
				PackedScene retainedScene = LoadAnimationScene("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn");
				prefixSprite = CreateAnimationRoot(scene, animationHost, "PrefixGraph", new Vector2(880f, 470f));
				await WaitFrames(12);
				for (int i = 0; i < 4; i++)
				{
					Vector2 position = new Vector2(125f + (float)i * 225f, 235f);
					oldSprites.Add(CreateAnimationRoot(retainedScene, animationHost, $"OldRoot{i}", position));
					oldRegions.Add(CreateCaptureRegion(position));
				}
				await WaitFrames(18);
				using Image baselineImage = await CaptureImage();
				int[] baselinePixels = CountRegions(baselineImage, oldRegions);
				bool baselineVisible = Array.TrueForAll(baselinePixels, (int num3) => num3 >= 80);
				bool beforeBindingReady = false;
				Vector2I beforeCachedAddress = new Vector2I(-1, -1);
				Vector2I beforeCurrentAddress = new Vector2I(-1, -1);
				prefixSprite.QueueFree();
				prefixSprite = null;
				await WaitFrames(4);
				await WaitFrames(1);
				int[] oldMinimumPixels = CreateMinimumArray(4);
				int oldBlankFrames = 0;
				int maximumFallbackRoots = 0;
				int minimumOldCrowdRoots = 2147483647;
				for (int frame = 0; frame < 12; frame++)
				{
					using Image image = await CaptureImage();
					int[] pixels = CountRegions(image, oldRegions);
					oldBlankFrames += UpdateMinimumPixels(pixels, baselinePixels, oldMinimumPixels);
					AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
					maximumFallbackRoots = Math.Max(maximumFallbackRoots, aggregateRenderStats.FallbackRoots);
					minimumOldCrowdRoots = Math.Min(minimumOldCrowdRoots, aggregateRenderStats.CrowdRoots);
				}
				bool afterBindingReady = false;
				Vector2I afterCachedAddress = new Vector2I(-1, -1);
				Vector2I afterCurrentAddress = new Vector2I(-1, -1);
				long compactionDelta = 0L;
				long patchDelta = 0L;
				for (int num = 0; num < 2; num++)
				{
					Vector2 position2 = new Vector2(300f + (float)num * 360f, 475f);
					newSprites.Add(CreateAnimationRoot(retainedScene, animationHost, $"NewRoot{num}", position2));
					newRegions.Add(CreateCaptureRegion(position2));
				}
				await WaitFrames(6);
				using Image newBaselineImage = await CaptureImage();
				int[] newBaselinePixels = CountRegions(newBaselineImage, newRegions);
				int[] newMinimumPixels = CreateMinimumArray(2);
				int combinedBlankFrames = 0;
				int minimumCombinedCrowdRoots = 2147483647;
				for (int frame = 0; frame < 12; frame++)
				{
					using Image image2 = await CaptureImage();
					combinedBlankFrames += UpdateMinimumPixels(CountRegions(image2, oldRegions), baselinePixels, oldMinimumPixels);
					combinedBlankFrames += UpdateMinimumPixels(CountRegions(image2, newRegions), newBaselinePixels, newMinimumPixels);
					AdobeAnimateCrowdAggregateStats aggregateRenderStats2 = AdobeAnimateRenderManager.GetAggregateRenderStats();
					maximumFallbackRoots = Math.Max(maximumFallbackRoots, aggregateRenderStats2.FallbackRoots);
					minimumCombinedCrowdRoots = Math.Min(minimumCombinedCrowdRoots, aggregateRenderStats2.CrowdRoots);
				}
				Vector2I vector2I = new Vector2I(-1, -1);
				Vector2I vector2I2 = new Vector2I(-1, -1);
				int num2 = CountNativeCanvasSuppressed(oldSprites) + CountNativeCanvasSuppressed(newSprites);
				double p99Milliseconds;
				long allocatedBytes;
				bool flag = MeasureGraphAllocationValidationPerformance(out p99Milliseconds, out allocatedBytes);
				bool flag2 = beforeBindingReady && afterBindingReady && beforeCachedAddress == beforeCurrentAddress && afterCachedAddress == afterCurrentAddress && beforeCurrentAddress != afterCurrentAddress;
				bool flag3 = false && vector2I == vector2I2 && vector2I2 == afterCurrentAddress;
				bool flag4 = RenderingServer.GetCurrentRenderingMethod() == "mobile" && baselineVisible && Array.TrueForAll(newBaselinePixels, (int num3) => num3 >= 80) && flag2 && flag3 && compactionDelta == 1 && patchDelta >= 4 && oldBlankFrames == 0 && combinedBlankFrames == 0 && maximumFallbackRoots == 0 && minimumOldCrowdRoots >= 4 && minimumCombinedCrowdRoots >= 6 && num2 == 6 && flag;
				GD.Print($"{"ADOBE_ANIMATE_GPU_GRAPH_COMPACTION_CONTINUITY_RESULT"} passed={flag4} baseline={string.Join(',', baselinePixels)} oldMinimum={string.Join(',', oldMinimumPixels)} newBaseline={string.Join(',', newBaselinePixels)} newMinimum={string.Join(',', newMinimumPixels)} oldBlankFrames={oldBlankFrames} combinedBlankFrames={combinedBlankFrames} before={beforeCurrentAddress} after={afterCurrentAddress} oldCached={afterCachedAddress} newCached={vector2I} compactions={compactionDelta} patches={patchDelta} minimumOldCrowd={minimumOldCrowdRoots} minimumCombinedCrowd={minimumCombinedCrowdRoots} maximumFallback={maximumFallbackRoots} suppressed={num2} hotPathPassed={flag} hotPathP99Ms={p99Milliseconds:F6} hotPathAllocatedBytes={allocatedBytes} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag4) ? 2 : 0);
			}
			catch (Exception value)
			{
				GD.PrintErr($"{"ADOBE_ANIMATE_GPU_GRAPH_COMPACTION_CONTINUITY_RESULT"} exception={value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(prefixSprite))
			{
				prefixSprite.QueueFree();
			}
			QueueAnimationRoots(oldSprites);
			QueueAnimationRoots(newSprites);
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			await WaitFrames(4);
		}
		GetTree().Quit(exitCode);
	}

	private static PackedScene LoadAnimationScene(string path)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.IgnoreDeep);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			throw new InvalidOperationException("Animation scene is unavailable: " + path);
		}
		return packedScene;
	}

	private static AdobeAnimateSprite CreateAnimationRoot(PackedScene scene, Node parent, string name, Vector2 position)
	{
		AdobeAnimateSprite adobeAnimateSprite = scene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		adobeAnimateSprite.Name = name;
		adobeAnimateSprite.Position = position;
		adobeAnimateSprite.ZAsRelative = false;
		adobeAnimateSprite.ZIndex = 200;
		adobeAnimateSprite.ProcessMode = ProcessModeEnum.Always;
		adobeAnimateSprite.forceLocalRender = false;
		adobeAnimateSprite.pause = false;
		parent.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
		return adobeAnimateSprite;
	}

	private static Rect2I CreateCaptureRegion(Vector2 position)
	{
		return new Rect2I(Mathf.RoundToInt(position.X) - 55, Mathf.RoundToInt(position.Y) - 125, 110, 155);
	}

	private static int[] CreateMinimumArray(int count)
	{
		int[] array = new int[count];
		Array.Fill(array, 2147483647);
		return array;
	}

	private static int UpdateMinimumPixels(int[] pixels, int[] baseline, int[] minimum)
	{
		int num = 0;
		for (int i = 0; i < pixels.Length; i++)
		{
			minimum[i] = Math.Min(minimum[i], pixels[i]);
			if (pixels[i] < Math.Max(80, Mathf.RoundToInt((float)baseline[i] * 0.65f)))
			{
				num++;
			}
		}
		return num;
	}

	private static bool MeasureGraphAllocationValidationPerformance(out double p99Milliseconds, out long allocatedBytes)
	{
		AdobeAnimateGpuRenderGraphAllocation allocation = new AdobeAnimateGpuRenderGraphAllocation(1uL, 0, 128, 256, 31, 1);
		float[] buffer = new float[8] { allocation.BaseTexel, allocation.Page, allocation.RenderSlotCount, 7f, allocation.OwnerCount, 0f, 0f, 0f };
		bool flag = true;
		for (int i = 0; i < 240; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				flag &= AdobeAnimateGpuGraphStateWriter.TryRefreshGraphAllocation(buffer, 0, in allocation, out var changed) && !changed;
			}
		}
		long[] array = new long[1200];
		GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
		GC.WaitForPendingFinalizers();
		GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		for (int k = 0; k < 1200; k++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			for (int l = 0; l < 1000; l++)
			{
				flag &= AdobeAnimateGpuGraphStateWriter.TryRefreshGraphAllocation(buffer, 0, in allocation, out var changed2) && !changed2;
			}
			array[k] = Stopwatch.GetTimestamp() - timestamp;
		}
		allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		Array.Sort(array);
		int num = Math.Clamp((int)Math.Ceiling((double)array.Length * 0.99) - 1, 0, array.Length - 1);
		p99Milliseconds = (double)array[num] * 1000.0 / (double)Stopwatch.Frequency;
		if (flag && allocatedBytes == 0L)
		{
			return p99Milliseconds < 0.2;
		}
		return false;
	}

	private int[] CountRegions(Image image, List<Rect2I> regions)
	{
		int[] array = new int[regions.Count];
		for (int i = 0; i < regions.Count; i++)
		{
			array[i] = CountForegroundPixels(image, regions[i]);
		}
		return array;
	}

	private int CountForegroundPixels(Image image, Rect2I region)
	{
		Rect2 visibleRect = GetViewport().GetVisibleRect();
		Vector2 vector = new Vector2((float)image.GetWidth() / visibleRect.Size.X, (float)image.GetHeight() / visibleRect.Size.Y);
		int num = Mathf.Clamp(Mathf.FloorToInt((float)region.Position.X * vector.X), 0, image.GetWidth());
		int num2 = Mathf.Clamp(Mathf.FloorToInt((float)region.Position.Y * vector.Y), 0, image.GetHeight());
		int num3 = Mathf.Clamp(Mathf.CeilToInt((float)region.End.X * vector.X), 0, image.GetWidth());
		int num4 = Mathf.Clamp(Mathf.CeilToInt((float)region.End.Y * vector.Y), 0, image.GetHeight());
		int num5 = 0;
		for (int i = num2; i < num4; i++)
		{
			for (int j = num; j < num3; j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - BackgroundColor.R) > 0.04f || Mathf.Abs(pixel.G - BackgroundColor.G) > 0.04f || Mathf.Abs(pixel.B - BackgroundColor.B) > 0.04f)
				{
					num5++;
				}
			}
		}
		return num5;
	}

	private static int CountNativeCanvasSuppressed(List<AdobeAnimateSprite> sprites)
	{
		int num = 0;
		for (int i = 0; i < sprites.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = sprites[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
				if (nativeCanvasSuppressed)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static void QueueAnimationRoots(List<AdobeAnimateSprite> sprites)
	{
		for (int i = 0; i < sprites.Count; i++)
		{
			if (GodotObject.IsInstanceValid(sprites[i]))
			{
				sprites[i].QueueFree();
			}
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<Image> CaptureImage()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		return GetViewport().GetTexture().GetImage();
	}
}
