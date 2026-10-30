using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class WallnutReadySetPlantFlickerRuntimeTest : Node
{
	private const string ResultMarker = "WALLNUT_READY_SET_PLANT_FLICKER_RESULT";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Wallnut.tscn";

	private const string NestedPreviewScenePath = "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/SunflowerPea.tscn";

	private const int SampleFrameCount = 90;

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f);

	private static readonly Rect2I[] CaptureRegions = new Rect2I[1]
	{
		new Rect2I(90, 100, 150, 180)
	};

	private AdobeAnimateRenderBackend _originalBackend;

	private int _originalMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<AdobeAnimateSprite> wallnuts = new List<AdobeAnimateSprite>(CaptureRegions.Length);
		AdobeAnimateSprite nestedPreview = null;
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
			ColorRect node = new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(900f, 420f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			AddChild(node, forceReadableName: false, InternalMode.Disabled);
			CanvasLayer battleLayer = new CanvasLayer
			{
				Layer = 1,
				FollowViewportEnabled = true,
				ProcessMode = ProcessModeEnum.Pausable
			};
			AddChild(battleLayer, forceReadableName: false, InternalMode.Disabled);
			Node2D characterHost = new Node2D();
			battleLayer.AddChild(characterHost, forceReadableName: false, InternalMode.Disabled);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Wallnut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				throw new InvalidOperationException("Production Wallnut animation scene is unavailable.");
			}
			for (int i = 0; i < CaptureRegions.Length; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
				adobeAnimateSprite.Position = CaptureRegions[i].GetCenter();
				adobeAnimateSprite.SetAnimation("Idle");
				characterHost.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
				wallnuts.Add(adobeAnimateSprite);
			}
			await WaitFrames(16);
			int[] baselinePixels = await CaptureRegionPixels();
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/SunflowerPea.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			if (!GodotObject.IsInstanceValid(packedScene2))
			{
				throw new InvalidOperationException("Production nested preview animation scene is unavailable.");
			}
			Control control = new Control
			{
				Name = "WallnutReadySetPlantPreviewMount",
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			battleLayer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
			nestedPreview = packedScene2.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
			ConfigurePreviewRenderTree(nestedPreview, control);
			nestedPreview.Position = new Vector2(-1000f, -1000f);
			battleLayer.AddChild(nestedPreview, forceReadableName: false, InternalMode.Disabled);
			characterHost.ProcessMode = ProcessModeEnum.Disabled;
			await WaitFrames(30);
			characterHost.ProcessMode = ProcessModeEnum.Inherit;
			int blankFrames = 0;
			int minimumPixels = 2147483647;
			int nativeCanvasFrames = 0;
			for (int frame = 0; frame < 90; frame++)
			{
				int[] array = await CaptureRegionPixels();
				bool flag = true;
				for (int j = 0; j < array.Length; j++)
				{
					minimumPixels = Math.Min(minimumPixels, array[j]);
					if (baselinePixels[j] <= 0 || array[j] < baselinePixels[j] / 4)
					{
						flag = false;
					}
					wallnuts[j].GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
					if (!nativeCanvasSuppressed)
					{
						nativeCanvasFrames++;
					}
				}
				if (!flag)
				{
					blankFrames++;
				}
			}
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			bool flag2 = Array.TrueForAll(baselinePixels, (int pixels) => pixels > 0) && blankFrames == 0 && minimumPixels > 0 && aggregateRenderStats.CrowdRoots == CaptureRegions.Length && aggregateRenderStats.FallbackRoots == 0;
			GD.Print($"{"WALLNUT_READY_SET_PLANT_FLICKER_RESULT"} passed={flag2} blank={blankFrames}/{90} minimumPixels={minimumPixels} baseline={string.Join(',', baselinePixels)} nativeCanvasFrames={nativeCanvasFrames} crowd={aggregateRenderStats.CrowdRoots} fallback={aggregateRenderStats.FallbackRoots} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag2) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"WALLNUT_READY_SET_PLANT_FLICKER_RESULT"} exception={value}");
		}
		finally
		{
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			Engine.MaxFps = _originalMaxFps;
			if (GodotObject.IsInstanceValid(nestedPreview))
			{
				ReleasePreviewRenderTree(nestedPreview);
				nestedPreview.QueueFree();
			}
			foreach (AdobeAnimateSprite item in wallnuts)
			{
				if (GodotObject.IsInstanceValid(item))
				{
					item.QueueFree();
				}
			}
		}
		GetTree().Quit(exitCode);
	}

	private static void ConfigurePreviewRenderTree(Node node, Control renderMount)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.forceLocalRender = true;
			adobeAnimateSprite.forceCpuPoseRender = true;
			adobeAnimateSprite.SetRenderClipControl(renderMount);
		}
		for (int i = 0; i < node.GetChildCount(); i++)
		{
			ConfigurePreviewRenderTree(node.GetChild(i), renderMount);
		}
	}

	private static void ReleasePreviewRenderTree(Node node)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			adobeAnimateSprite.ClearRenderClipControl();
			adobeAnimateSprite.ReleaseForcedCpuPoseData();
		}
		for (int i = 0; i < node.GetChildCount(); i++)
		{
			ReleasePreviewRenderTree(node.GetChild(i));
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<int[]> CaptureRegionPixels()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		int[] array = new int[CaptureRegions.Length];
		for (int i = 0; i < CaptureRegions.Length; i++)
		{
			array[i] = CountForegroundPixels(image, CaptureRegions[i]);
		}
		return array;
	}

	private static int CountForegroundPixels(Image image, Rect2I region)
	{
		int num = 0;
		int num2 = Math.Min(region.End.X, image.GetWidth());
		int num3 = Math.Min(region.End.Y, image.GetHeight());
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
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
}
