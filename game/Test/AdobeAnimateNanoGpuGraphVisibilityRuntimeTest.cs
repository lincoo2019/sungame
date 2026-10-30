using System;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateNanoGpuGraphVisibilityRuntimeTest : Node2D
{
	private const string ResultMarker = "ADOBE_ANIMATE_NANO_GPU_GRAPH_VISIBILITY_RESULT";

	private const string NanoScenePath = "res://Asset/Anime/Character/Zombie/Challenge/Nano/Scene/TowerDefenseZombieNano.tscn";

	private const string HelmetMediaName = "Zombie_Nano_helmet.png";

	private const string HelmetStageZeroPath = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/HelmetNano/Zombie_Nano_helmet.png";

	private const string HelmetStageTwoPath = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/HelmetNano/Zombie_Nano_helmet3.png";

	private const int SampleFrameCount = 12;

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.025f);

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	private int _originalMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		TowerDefenseZombieNano nano = null;
		try
		{
			_ = 14;
			try
			{
				Require(GodotObject.IsInstanceValid(Global.Instance), "Global autoload is unavailable.");
				_originalBackend = Global.Instance.adobeAnimateRenderBackend;
				_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
				_originalMaxFps = Engine.MaxFps;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				AdobeAnimateRenderManager.RasterCompositeEnabled = false;
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
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/Nano/Scene/TowerDefenseZombieNano.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Require(GodotObject.IsInstanceValid(packedScene), "Nano character scene could not be loaded.");
				nano = packedScene.Instantiate<TowerDefenseZombieNano>(PackedScene.GenEditState.Disabled);
				Require(GodotObject.IsInstanceValid(nano), "Nano character scene could not be instantiated.");
				nano.inGame = false;
				nano.editorPreviewMode = true;
				nano.Visible = true;
				nano.Position = new Vector2(540f, 360f);
				nano.Scale = Vector2.One * 2f;
				AddChild(nano, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(24);
				AdobeAnimateSprite sprite = nano.sprite;
				AdobeAnimateSlot headSlot = nano.headSlot;
				TowerDefenseArmorInstance towerDefenseArmorInstance = FindArmor(nano, "HelmetNano");
				Require(GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(headSlot), "Nano body or HeadSlot did not initialize.");
				Require(GodotObject.IsInstanceValid(towerDefenseArmorInstance) && towerDefenseArmorInstance.hitPoints > 0.0, "Nano packet-authored helmet armor did not initialize.");
				Require(headSlot.drawLayerId == -2, $"Nano HeadSlot is not authored at Top; actual={headSlot.drawLayerId}.");
				Require(headSlot.ResolveDrawLayerId() == sprite.GetLayerVisibleCountForRender() + 1, $"Nano HeadSlot did not resolve above every body layer; resolved={headSlot.ResolveDrawLayerId()} layers={sprite.GetLayerVisibleCountForRender()}.");
				AdobeAnimateData flashAnimeData = sprite.flashAnimeData;
				Require(flashAnimeData != null && flashAnimeData.mediaDictionary?.ContainsKey("Zombie_Nano_helmet.png") == true, "Nano animation does not expose the helmet media slot.");
				int helmetMediaId = (int)sprite.flashAnimeData.mediaDictionary["Zombie_Nano_helmet.png"];
				string atlasReplacePath = sprite.GetAtlasReplacePath("Zombie_Nano_helmet.png");
				Require(atlasReplacePath == "res://Asset/AtlasSource/Armor/Texture/Character/Armor/HelmetNano/Zombie_Nano_helmet.png", "Nano initial helmet replacement is wrong; actual=" + atlasReplacePath + ".");
				Require(helmetMediaId >= 0 && helmetMediaId < sprite.mediaReplaceUse.Count && sprite.mediaReplaceUse[helmetMediaId], "Nano initial helmet replacement is not enabled on its media slot.");
				Require(AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasReplacePath, out var stageZeroAllocation), "Nano initial helmet is absent from the shared replacement atlas.");
				Require(stageZeroAllocation.UsesTextureArray && stageZeroAllocation.TextureArrayRid.IsValid, "Nano initial helmet does not use the shared texture array.");
				sprite.SetAnimation("Idle");
				sprite.SetVerticalClip(enabled: false, -10000f, 10000f);
				sprite.timeScale = 0.0;
				await WaitFrames(4);
				AdobeAnimateCrowdRenderState state;
				AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = sprite.TryBuildCrowdRenderState(out state);
				AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState = state?.GpuGraphRootOwnerState;
				Require(adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted && state != null && state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph && adobeAnimateGpuGraphOwnerState != null, $"Nano did not submit through GPU Graph; result={adobeAnimateCrowdRenderStateResult} mode={state?.Mode}.");
				Require(adobeAnimateGpuGraphOwnerState.Definition?.AtlasTextureArrayRid == stageZeroAllocation.TextureArrayRid, "Nano helmet replacement and body definition do not share one texture-array RID.");
				Require(adobeAnimateGpuGraphOwnerState.HasMediaReplace && helmetMediaId < adobeAnimateGpuGraphOwnerState.MediaReplaceUse.Count && adobeAnimateGpuGraphOwnerState.MediaReplaceUse[helmetMediaId] && adobeAnimateGpuGraphOwnerState.MediaReplaceRect[helmetMediaId] == stageZeroAllocation.Rect && adobeAnimateGpuGraphOwnerState.MediaReplaceAtlasPages[helmetMediaId] == stageZeroAllocation.AtlasPage, "Nano helmet replacement did not enter the GPU Graph owner state.");
				Rect2I headRegion = CreateCenteredRegion(headSlot.GlobalPosition, 190, 190);
				using Image stageZeroImage = await CaptureImage();
				int stageZeroHeadPixels = CountForegroundPixels(stageZeroImage, headRegion);
				nano.SetArmor("HelmetNano", 2);
				await WaitFrames(4);
				string atlasReplacePath2 = sprite.GetAtlasReplacePath("Zombie_Nano_helmet.png");
				Require(atlasReplacePath2 == "res://Asset/AtlasSource/Armor/Texture/Character/Armor/HelmetNano/Zombie_Nano_helmet3.png", "Nano damaged helmet replacement is wrong; actual=" + atlasReplacePath2 + ".");
				Require(AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasReplacePath2, out var allocation) && allocation.UsesTextureArray && allocation.TextureArrayRid == stageZeroAllocation.TextureArrayRid, "Nano damaged helmet left the shared body texture array.");
				using Image stageTwoImage = await CaptureImage();
				int stageTwoHeadPixels = CountForegroundPixels(stageTwoImage, headRegion);
				int changedHeadPixels = CountChangedPixels(stageZeroImage, stageTwoImage, headRegion);
				Require(stageZeroHeadPixels >= 120 && stageTwoHeadPixels >= 120 && changedHeadPixels >= 24, $"Nano helmet stages were not visibly rendered in the HeadSlot region; stage0={stageZeroHeadPixels} stage2={stageTwoHeadPixels} changed={changedHeadPixels}.");
				nano.SetArmor("HelmetNano", 0);
				sprite.timeScale = 1.0;
				AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
				await WaitFrames(4);
				Rect2I bodyRegion = CreateCenteredRegion(nano.Position + new Vector2(0f, -80f), 380, 400);
				int minimumBodyPixels = 2147483647;
				int minimumHeadPixels = 2147483647;
				int minimumCrowdRoots = 2147483647;
				int maximumFallbackRoots = 0;
				int blankFrames = 0;
				for (int frame = 0; frame < 12; frame++)
				{
					using Image image = await CaptureImage();
					int num = CountForegroundPixels(image, bodyRegion);
					int num2 = CountForegroundPixels(image, headRegion);
					minimumBodyPixels = Math.Min(minimumBodyPixels, num);
					minimumHeadPixels = Math.Min(minimumHeadPixels, num2);
					if (num < 400 || num2 < 80)
					{
						blankFrames++;
					}
					AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
					minimumCrowdRoots = Math.Min(minimumCrowdRoots, aggregateRenderStats.CrowdRoots);
					maximumFallbackRoots = Math.Max(maximumFallbackRoots, aggregateRenderStats.FallbackRoots);
				}
				sprite.GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
				Require(blankFrames == 0 && minimumBodyPixels >= 400 && minimumHeadPixels >= 80, $"Nano produced a visually missing frame; blank={blankFrames} bodyMin={minimumBodyPixels} headMin={minimumHeadPixels}.");
				Require(minimumCrowdRoots >= 1 && maximumFallbackRoots == 0 && !sprite.forceLocalRender, $"Nano left Crowd ownership; crowdMin={minimumCrowdRoots} fallbackMax={maximumFallbackRoots} forceLocal={sprite.forceLocalRender}.");
				AdobeAnimateRenderBackend[] array = new AdobeAnimateRenderBackend[2]
				{
					AdobeAnimateRenderBackend.GpuCrowd,
					AdobeAnimateRenderBackend.CpuPose
				};
				foreach (AdobeAnimateRenderBackend backend in array)
				{
					Global.Instance.adobeAnimateRenderBackend = backend;
					await WaitFrames(4);
					using Image beforePlayback = await CaptureImage();
					await WaitFrames(12);
					using Image afterPlayback = await CaptureImage();
					int playbackPixels = CountChangedPixels(beforePlayback, afterPlayback, bodyRegion);
					Require(CountForegroundPixels(afterPlayback, bodyRegion) >= 400 && playbackPixels >= 24, $"{backend} 动画未持续绘制或播放: changed={playbackPixels}.");
					nano.Visible = false;
					await WaitFrames(4);
					using Image hiddenImage = await CaptureImage();
					Require(CountForegroundPixels(hiddenImage, bodyRegion) == 0, $"{backend} 隐藏角色后仍残留动画像素。");
					nano.Visible = true;
					await WaitFrames(4);
					using Image image2 = await CaptureImage();
					Require(CountForegroundPixels(image2, bodyRegion) >= 400, $"{backend} 重新显示角色后动画未恢复。");
					GD.Print($"NANO_BACKEND_VISIBILITY backend={backend} playbackPixels={playbackPixels} hidden=True restored=True");
				}
				AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot statistics = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.Statistics;
				Require(statistics.AppliedUploadCount > 0 && statistics.FailedBatchCount == 0L && statistics.BufferUpdateFailureCount == 0L && statistics.InvalidTargetCount == 0, "动画实例上传失败或没有实际提交。");
				Require((!(RenderingServer.GetCurrentRenderingMethod() == "gl_compatibility")) ? (statistics.RenderingDeviceUnavailableCount == 0L && statistics.BufferRidRefreshCount > 0) : (statistics.RenderingDeviceUnavailableCount > 0 && statistics.BufferRidRefreshCount == 0), "动画上传没有按图形设备能力选择正确路径。");
				GD.Print($"{"ADOBE_ANIMATE_NANO_GPU_GRAPH_VISIBILITY_RESULT"} passed=True headSlot={headSlot.drawLayerId}/{headSlot.ResolveDrawLayerId()} helmetMedia={helmetMediaId} atlasPage={stageZeroAllocation.AtlasPage} sharedRid={stageZeroAllocation.TextureArrayRid} stage0Head={stageZeroHeadPixels} stage2Head={stageTwoHeadPixels} changedHead={changedHeadPixels} bodyMin={minimumBodyPixels} headMin={minimumHeadPixels} blankFrames={blankFrames} crowdMin={minimumCrowdRoots} fallbackMax={maximumFallbackRoots} suppressed={nativeCanvasSuppressed} samples={12} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = 0;
			}
			catch (Exception value)
			{
				GD.PrintErr($"{"ADOBE_ANIMATE_NANO_GPU_GRAPH_VISIBILITY_RESULT"} passed=False exception={value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(nano) && !nano.IsQueuedForDeletion())
			{
				nano.QueueFree();
			}
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
			Engine.MaxFps = _originalMaxFps;
			await WaitFrames(4);
		}
		GetTree().Quit(exitCode);
	}

	private static TowerDefenseArmorInstance FindArmor(TowerDefenseCharacter character, string armorName)
	{
		if (character?.instance?.armorList == null)
		{
			return null;
		}
		foreach (TowerDefenseArmorInstance armor in character.instance.armorList)
		{
			if (armor?.slotConfig?.armorName == armorName && !armor.isRemove)
			{
				return armor;
			}
		}
		return null;
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

	private static Rect2I CreateCenteredRegion(Vector2 center, int width, int height)
	{
		return new Rect2I(Mathf.RoundToInt(center.X) - width / 2, Mathf.RoundToInt(center.Y) - height / 2, width, height);
	}

	private int CountForegroundPixels(Image image, Rect2I logicalRegion)
	{
		Rect2I rect2I = ConvertToImageRegion(image, logicalRegion);
		int num = 0;
		for (int i = rect2I.Position.Y; i < rect2I.End.Y; i++)
		{
			for (int j = rect2I.Position.X; j < rect2I.End.X; j++)
			{
				if (ColorDistance(image.GetPixel(j, i), BackgroundColor) > 0.05f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private int CountChangedPixels(Image first, Image second, Rect2I logicalRegion)
	{
		Rect2I rect2I = ConvertToImageRegion(first, logicalRegion);
		Rect2I rect2I2 = ConvertToImageRegion(second, logicalRegion);
		int num = Math.Min(rect2I.Size.X, rect2I2.Size.X);
		int num2 = Math.Min(rect2I.Size.Y, rect2I2.Size.Y);
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Color pixel = first.GetPixel(rect2I.Position.X + j, rect2I.Position.Y + i);
				Color pixel2 = second.GetPixel(rect2I2.Position.X + j, rect2I2.Position.Y + i);
				if (ColorDistance(pixel, pixel2) > 0.05f)
				{
					num3++;
				}
			}
		}
		return num3;
	}

	private Rect2I ConvertToImageRegion(Image image, Rect2I logicalRegion)
	{
		Rect2 visibleRect = GetViewport().GetVisibleRect();
		Vector2 vector = new Vector2((float)image.GetWidth() / visibleRect.Size.X, (float)image.GetHeight() / visibleRect.Size.Y);
		int num = Mathf.Clamp(Mathf.FloorToInt((float)logicalRegion.Position.X * vector.X), 0, image.GetWidth());
		int num2 = Mathf.Clamp(Mathf.FloorToInt((float)logicalRegion.Position.Y * vector.Y), 0, image.GetHeight());
		int num3 = Mathf.Clamp(Mathf.CeilToInt((float)logicalRegion.End.X * vector.X), 0, image.GetWidth());
		int num4 = Mathf.Clamp(Mathf.CeilToInt((float)logicalRegion.End.Y * vector.Y), 0, image.GetHeight());
		return new Rect2I(num, num2, num3 - num, num4 - num2);
	}

	private static float ColorDistance(Color left, Color right)
	{
		return Math.Max(Math.Abs(left.R - right.R), Math.Max(Math.Abs(left.G - right.G), Math.Abs(left.B - right.B)));
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
