using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class ZombieNormalRasterCompositeRuntimeTest : Node
{
	private const string ResultMarker = "ZOMBIE_NORMAL_RASTER_COMPOSITE_RESULT";

	private const string SpriteScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn";

	private const int VisibleSpriteCount = 24;

	private readonly List<AdobeAnimateSprite> _sprites = new List<AdobeAnimateSprite>(24);

	private readonly List<string> _failures = new List<string>();

	private AdobeAnimateRenderBackend _originalBackend;

	private int _originalMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		try
		{
			if (Global.Instance == null)
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			_originalMaxFps = Engine.MaxFps;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			AdobeAnimateRenderManager.RasterCompositeEnabled = true;
			Engine.MaxFps = Math.Max(120, Engine.PhysicsTicksPerSecond * 2);
			PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn");
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				throw new InvalidOperationException("ZombieNormal sprite scene could not be loaded.");
			}
			for (int i = 0; i < 24; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
				if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					throw new InvalidOperationException($"ZombieNormal sprite {i} could not be instantiated.");
				}
				adobeAnimateSprite.Name = $"RasterZombie{i}";
				adobeAnimateSprite.Position = new Vector2(80f + (float)(i % 12) * 82f, 160f + (float)(i / 12) * 230f);
				adobeAnimateSprite.SetFliter("anim_tongue", (i & 1) != 0);
				adobeAnimateSprite.SetAnimation((i % 3 == 2) ? "Eat" : ((i % 3 == 1) ? "Walk2" : "Walk1"));
				if (i >= 12)
				{
					ApplyProductionDormantArmorReplacements(adobeAnimateSprite);
				}
				if (i % 3 == 0)
				{
					adobeAnimateSprite.SetVerticalClip(enabled: true, -10000f, 10000f);
				}
				AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
				_sprites.Add(adobeAnimateSprite);
			}
			await WaitProcessFrames(24);
			int rasterStates = CountRasterStates();
			AdobeAnimateCrowdAggregateStats stats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			Check(rasterStates == 24, $"Every pristine Normal zombie must use RasterComposite; actual={rasterStates}/{24}.");
			Check(stats.CrowdRoots >= 24, $"GPU Crowd did not publish every visible root; crowd={stats.CrowdRoots} fallback={stats.FallbackRoots}.");
			Check(stats.FallbackRoots == 0, $"Pristine Normal zombies unexpectedly used fallback roots={stats.FallbackRoots}.");
			Check(stats.CrowdMeshQuadCapacity > 0 && stats.CrowdMeshQuadCapacity <= 8, $"Raster batch must stay within the one-quad mesh growth floor; capacity={stats.CrowdMeshQuadCapacity}.");
			Vector2[] originalPositions = new Vector2[_sprites.Count];
			for (int j = 0; j < _sprites.Count; j++)
			{
				originalPositions[j] = _sprites[j].Position;
				_sprites[j].Position += new Vector2(18f, -6f);
			}
			await WaitProcessFrames(3);
			int translatedRasterStates = CountRasterStates();
			Check(translatedRasterStates == 24, $"Translation-only movement must retain every raster state; actual={translatedRasterStates}/{24}.");
			for (int k = 0; k < _sprites.Count; k++)
			{
				_sprites[k].Position += new Vector2(5000f, 0f);
			}
			await WaitProcessFrames(3);
			int offscreenRasterStates = CountRasterStates();
			Check(offscreenRasterStates == 0, $"Translation-only movement outside the padded viewport must cull every raster state; actual={offscreenRasterStates}.");
			for (int l = 0; l < _sprites.Count; l++)
			{
				_sprites[l].Position = originalPositions[l];
			}
			await WaitProcessFrames(3);
			int restoredRasterStates = CountRasterStates();
			Check(restoredRasterStates == 24, $"Returning translated sprites to the viewport must restore every raster state; actual={restoredRasterStates}/{24}.");
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			using Image capture = GetViewport().GetTexture().GetImage();
			int visiblePixelCount = CountVisiblePixels(capture);
			int repeatedQuadLeakPixels = CountVisiblePixels(capture, 0, 520, 1080, 600);
			Check(visiblePixelCount >= 12000, $"Raster composite output is blank or incomplete; visiblePixels={visiblePixelCount}.");
			Check(repeatedQuadLeakPixels == 0, $"Unused shared-mesh slots repeated the raster quad; leakPixels={repeatedQuadLeakPixels}.");
			string capturePath = ProjectSettings.GlobalizePath("user://ZombieNormalRasterCompositeRuntime.png");
			Check(capture.SavePng(capturePath) == Error.Ok, "Runtime visual capture could not be saved to " + capturePath + ".");
			AdobeAnimateSprite dynamicSprite = _sprites[0];
			dynamicSprite.SetFliter("Zombie_outerarm_upper", open: false);
			await WaitProcessFrames(3);
			AdobeAnimateCrowdRenderState fallbackState;
			AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = dynamicSprite.TryBuildCrowdRenderState(out fallbackState);
			Check(adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted && fallbackState.Mode != AdobeAnimateCrowdRenderMode.RasterComposite, "A dynamic layer mutation must fall back to the original GPU rendering path.");
			AdobeAnimateSprite replacementSprite = _sprites[1];
			Array<string> array = CreateEmptyMediaPaths();
			array[1] = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/Bucket/ZombieBucket1.png";
			replacementSprite.mediaReplaceAtlasPaths = array;
			await WaitProcessFrames(3);
			AdobeAnimateCrowdRenderState state;
			AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult2 = replacementSprite.TryBuildCrowdRenderState(out state);
			Check(adobeAnimateCrowdRenderStateResult2 == AdobeAnimateCrowdRenderStateResult.Submitted && state.Mode != AdobeAnimateCrowdRenderMode.RasterComposite, "A replacement on a visible body media ID must fall back to the original GPU rendering path.");
			bool flag = _failures.Count == 0;
			GD.Print($"{"ZOMBIE_NORMAL_RASTER_COMPOSITE_RESULT"} passed={flag} raster={rasterStates}/{24} translated={translatedRasterStates}/{24} offscreen={offscreenRasterStates} restored={restoredRasterStates}/{24} visiblePixels={visiblePixelCount} repeatedQuadLeakPixels={repeatedQuadLeakPixels} crowdRoots={stats.CrowdRoots} fallbackRoots={stats.FallbackRoots} quadCapacity={stats.CrowdMeshQuadCapacity} dynamicMode={fallbackState.Mode} renderer={RenderingServer.GetCurrentRenderingMethod()} capture={capturePath}");
			foreach (string failure in _failures)
			{
				GD.PrintErr("ZOMBIE_NORMAL_RASTER_COMPOSITE_RESULT failure=" + failure);
			}
			exitCode = ((!flag) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ZOMBIE_NORMAL_RASTER_COMPOSITE_RESULT"} exception={value}");
		}
		finally
		{
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			Engine.MaxFps = _originalMaxFps;
		}
		GetTree().Quit(exitCode);
	}

	private static void ApplyProductionDormantArmorReplacements(AdobeAnimateSprite sprite)
	{
		Array<string> array = CreateEmptyMediaPaths();
		array[2] = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/Bucket/ZombieBucket1.png";
		array[3] = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/Cone/ZombieCone1.png";
		sprite.mediaReplaceAtlasPaths = array;
	}

	private static Array<string> CreateEmptyMediaPaths()
	{
		Array<string> array = new Array<string>();
		array.Resize(38);
		return array;
	}

	private int CountRasterStates()
	{
		int num = 0;
		foreach (AdobeAnimateSprite sprite in _sprites)
		{
			if (sprite.TryBuildCrowdRenderState(out var state) == AdobeAnimateCrowdRenderStateResult.Submitted && state.Mode == AdobeAnimateCrowdRenderMode.RasterComposite)
			{
				num++;
			}
		}
		return num;
	}

	private static int CountVisiblePixels(Image image)
	{
		return CountVisiblePixels(image, 20, 40, 1060, 560);
	}

	private static int CountVisiblePixels(Image image, int left, int top, int right, int bottom)
	{
		int num = 0;
		for (int i = Math.Max(0, top); i < Math.Min(image.GetHeight(), bottom); i += 2)
		{
			for (int j = Math.Max(0, left); j < Math.Min(image.GetWidth(), right); j += 2)
			{
				Color pixel = image.GetPixel(j, i);
				if (pixel.R + pixel.G + pixel.B >= 0.12f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Check(bool condition, string failure)
	{
		if (!condition)
		{
			_failures.Add(failure);
		}
	}
}
