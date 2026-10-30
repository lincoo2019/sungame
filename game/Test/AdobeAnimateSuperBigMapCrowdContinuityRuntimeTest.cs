using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateSuperBigMapCrowdContinuityRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_SUPER_BIG_MAP_CROWD_CONTINUITY_RESULT";

	private static readonly string[] MixedCharacterScenePaths = new string[8] { "uid://b0u8ponnhfcbf", "uid://cc3eup5kd355o", "uid://cpfiv5377lamd", "uid://rn1t8d45celk", "uid://dk8idm88ij8e6", "uid://bu2et1p8gopaq", "uid://b55qmyxtvtj4s", "uid://bxkewpql5rpqo" };

	private const int MapColumns = 27;

	private const int MapRows = 15;

	private const int BaselineRootCount = 192;

	private const int FinalRootCount = 640;

	private const int SpawnBatchSize = 32;

	private const int SampleRootCount = 12;

	private static readonly AdobeAnimateRenderTransactionFaultPhase[] TransactionFaultPhases = new AdobeAnimateRenderTransactionFaultPhase[8]
	{
		AdobeAnimateRenderTransactionFaultPhase.Begin,
		AdobeAnimateRenderTransactionFaultPhase.Prepare,
		AdobeAnimateRenderTransactionFaultPhase.Collect,
		AdobeAnimateRenderTransactionFaultPhase.Encode,
		AdobeAnimateRenderTransactionFaultPhase.Freeze,
		AdobeAnimateRenderTransactionFaultPhase.Publish,
		AdobeAnimateRenderTransactionFaultPhase.PublishState,
		AdobeAnimateRenderTransactionFaultPhase.PublishCrowd
	};

	private const float MapZoom = 0.48f;

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f);

	private AdobeAnimateRenderBackend _originalBackend;

	private int _originalMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<AdobeAnimateSprite> sprites = new List<AdobeAnimateSprite>(640);
		List<TowerDefenseCharacter> characters = new List<TowerDefenseCharacter>(640);
		bool runtimeManagerProcessSuspended = false;
		try
		{
			if (!GodotObject.IsInstanceValid(Global.Instance))
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			_originalMaxFps = Engine.MaxFps;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			Engine.MaxFps = 60;
			CanvasLayer canvasLayer = new CanvasLayer
			{
				Layer = -10
			};
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			canvasLayer.AddChild(new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(1200f, 600f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			}, forceReadableName: false, InternalMode.Disabled);
			Node2D world = new Node2D
			{
				Name = "SuperBigMapAnimationWorld"
			};
			AddChild(world, forceReadableName: false, InternalMode.Disabled);
			Camera2D camera = new Camera2D
			{
				Position = new Vector2(600f, 300f),
				Zoom = Vector2.One,
				Enabled = true
			};
			world.AddChild(camera, forceReadableName: false, InternalMode.Disabled);
			PackedScene[] mixedScenes = LoadMixedScenes();
			while (sprites.Count < 192)
			{
				SpawnCharacters(mixedScenes, world, characters, sprites, Math.Min(192, sprites.Count + 32));
				await WaitFrames(1);
			}
			await WaitFrames(36);
			AdobeAnimateSprite[] samples = SelectExpansionSamples(sprites);
			int cachedOffscreenBeforeExpansion = CountCachedOffscreenSamples(samples);
			AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
			await WaitPhysicsFrames(1);
			await WaitFrames(1);
			camera.Zoom = Vector2.One * 0.48f;
			camera.Position = new Vector2(1250f, 600f);
			await WaitPhysicsFrames(2);
			int[] unnotifiedPixels = await CaptureSamplePixels(samples);
			await WaitFrames(18);
			int[] periodicPixels = await CaptureSamplePixels(samples);
			AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
			await WaitPhysicsFrames(1);
			int[] baselinePixels = await CaptureSamplePixels(samples);
			int unnotifiedBlankSamples = CountBlankSamples(unnotifiedPixels, baselinePixels);
			int periodicBlankSamples = CountBlankSamples(periodicPixels, baselinePixels);
			int[] minimumPixels = (int[])baselinePixels.Clone();
			int blankSamples = 0;
			int sampledFrames = 0;
			int minimumCrowdRoots = 2147483647;
			int minimumSuppressedRoots = 2147483647;
			int minimumExpectedSuppressed = 640 * (MixedCharacterScenePaths.Length - 1) / MixedCharacterScenePaths.Length;
			while (sprites.Count < 640)
			{
				SpawnCharacters(mixedScenes, world, characters, sprites, Math.Min(640, sprites.Count + 32));
				await WaitFrames(2);
				camera.Zoom = Vector2.One * 1.6f;
				camera.Position = new Vector2(650f + (float)(sprites.Count % 900), 360f + (float)(sprites.Count % 420));
				AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
				await WaitPhysicsFrames(1);
				camera.Zoom = Vector2.One * 0.48f;
				camera.Position = new Vector2(1250f, 600f);
				AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
				await WaitPhysicsFrames(1);
				int[] array = await CaptureSamplePixels(samples);
				for (int i = 0; i < array.Length; i++)
				{
					minimumPixels[i] = Math.Min(minimumPixels[i], array[i]);
					if (baselinePixels[i] <= 0 || array[i] < Math.Max(8, baselinePixels[i] / 4))
					{
						blankSamples++;
					}
				}
				minimumCrowdRoots = Math.Min(minimumCrowdRoots, AdobeAnimateRenderManager.GetAggregateRenderStats().CrowdRoots);
				minimumSuppressedRoots = Math.Min(minimumSuppressedRoots, CountSuppressedRoots(sprites));
				sampledFrames++;
			}
			await WaitFrames(12);
			int[] finalPixels = await CaptureSamplePixels(samples);
			for (int j = 0; j < finalPixels.Length; j++)
			{
				minimumPixels[j] = Math.Min(minimumPixels[j], finalPixels[j]);
				if (baselinePixels[j] <= 0 || finalPixels[j] < Math.Max(8, baselinePixels[j] / 4))
				{
					blankSamples++;
				}
			}
			AdobeAnimateRuntimeManager runtimeManager = AdobeAnimateRuntimeManager.Instance;
			if (!GodotObject.IsInstanceValid(runtimeManager))
			{
				throw new InvalidOperationException("Adobe Animate runtime manager is unavailable for transaction fault recovery validation.");
			}
			runtimeManager.SetProcess(enable: false);
			runtimeManagerProcessSuspended = true;
			int recoveredFaultPhases = 0;
			int faultBlankSamples = 0;
			int minimumFaultRetryRoots = 2147483647;
			int minimumFaultRepublishedRoots = 2147483647;
			long failedTransactionCountBeforeFaults = AdobeAnimateRenderManager.FailedRenderTransactionCountForTests;
			for (int phaseIndex = 0; phaseIndex < TransactionFaultPhases.Length; phaseIndex++)
			{
				AdobeAnimateRenderTransactionFaultPhase phase = TransactionFaultPhases[phaseIndex];
				long failedRenderTransactionCountForTests = AdobeAnimateRenderManager.FailedRenderTransactionCountForTests;
				long renderTransactionCountForTests = AdobeAnimateRenderManager.RenderTransactionCountForTests;
				AdobeAnimateRenderManager.InjectRenderTransactionFailureForTests(phase);
				AdobeAnimateRenderManager.RenderActive(sprites, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				bool recoveryTransactionCompleted = AdobeAnimateRenderManager.RenderTransactionCountForTests == renderTransactionCountForTests + 2 && AdobeAnimateRenderManager.LastCompletedRuntimeTransactionVersion != -9223372036854775808L;
				bool failedCountAdvanced = AdobeAnimateRenderManager.FailedRenderTransactionCountForTests == failedRenderTransactionCountForTests + 1;
				bool completeFailureHandoff = AdobeAnimateRenderManager.FailureHandoffCompleteForTests && AdobeAnimateRenderManager.FailureHandoffSubmittedRootCountForTests == 640;
				int retryRoots = CountRetryRoots(sprites);
				minimumFaultRetryRoots = Math.Min(minimumFaultRetryRoots, retryRoots);
				int[] nativeRecoveryPixels = await CaptureSamplePixels(samples);
				int nativeBlankSamples = CountBlankSamples(nativeRecoveryPixels, baselinePixels);
				faultBlankSamples += nativeBlankSamples;
				AdobeAnimateRenderManager.RenderActive(sprites, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
				AdobeAnimateCrowdAggregateStats recoveredStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				int republishedRoots = CountSuppressedRoots(sprites);
				minimumFaultRepublishedRoots = Math.Min(minimumFaultRepublishedRoots, republishedRoots);
				int[] array2 = await CaptureSamplePixels(samples);
				int num = CountBlankSamples(array2, baselinePixels);
				faultBlankSamples += num;
				bool flag = recoveryTransactionCompleted && failedCountAdvanced && completeFailureHandoff && retryRoots == 640 && nativeBlankSamples == 0 && recoveredStats.CrowdRoots == 640 && recoveredStats.FallbackRoots == 0 && republishedRoots >= minimumExpectedSuppressed && num == 0;
				if (flag)
				{
					recoveredFaultPhases++;
				}
				GD.Print($"ADOBE_ANIMATE_TRANSACTION_FAULT_PHASE phase={phase} passed={flag} recoveryCompleted={recoveryTransactionCompleted} failedAdvanced={failedCountAdvanced} handoff={AdobeAnimateRenderManager.FailureHandoffSubmittedRootCountForTests}/{640} handoffComplete={completeFailureHandoff} retryRoots={retryRoots}/{640} firstBlank={nativeBlankSamples}/{12} firstPixels={string.Join(',', nativeRecoveryPixels)} crowdRoots={recoveredStats.CrowdRoots}/{640} fallbackRoots={recoveredStats.FallbackRoots} republished={republishedRoots}/{minimumExpectedSuppressed} secondBlank={num}/{12} secondPixels={string.Join(',', array2)}");
			}
			bool faultCountExact = AdobeAnimateRenderManager.FailedRenderTransactionCountForTests == failedTransactionCountBeforeFaults + TransactionFaultPhases.Length;
			bool faultRecoveryPassed = faultCountExact && recoveredFaultPhases == TransactionFaultPhases.Length && faultBlankSamples == 0;
			runtimeManager.SetProcess(enable: true);
			runtimeManagerProcessSuspended = false;
			AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
			await WaitPhysicsFrames(2);
			await WaitFrames(2);
			AdobeAnimateSprite omittedRoot = FindSuppressedRoot(sprites, null);
			AdobeAnimateSprite publishedRoot = FindSuppressedRoot(sprites, omittedRoot);
			long completeVersionBeforeOmission = AdobeAnimateRenderManager.LastCompletedRuntimeTransactionVersion;
			await WaitFrames(1);
			AdobeAnimateRenderManager.RenderActive(new AdobeAnimateSprite[1] { publishedRoot }, activeListAlreadyValidated: true, renderRootsAlreadyFiltered: true);
			long lastCompletedRuntimeTransactionVersion = AdobeAnimateRenderManager.LastCompletedRuntimeTransactionVersion;
			bool omissionTransactionAdvanced = lastCompletedRuntimeTransactionVersion != completeVersionBeforeOmission;
			bool omittedSuppressedBeforeRecovery = IsNativeCanvasSuppressed(omittedRoot);
			bool omittedRecovered = omittedRoot.RecoverRuntimeNativeCanvasAfterMissedTransaction(lastCompletedRuntimeTransactionVersion);
			bool omittedSuppressedAfterRecovery = IsNativeCanvasSuppressed(omittedRoot);
			bool omittedRetryRequested = omittedRoot.MarkRuntimeRenderSubmissionConsumed();
			AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
			await WaitPhysicsFrames(2);
			await WaitFrames(2);
			bool flag2 = IsNativeCanvasSuppressed(omittedRoot);
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			int num2 = CountSuppressedRoots(sprites);
			bool flag3 = aggregateRenderStats.CrowdStateCapacityTexels > 8192 && aggregateRenderStats.CrowdStateTexels > 8192;
			bool num3 = Array.TrueForAll(baselinePixels, (int pixels) => pixels > 0) && blankSamples == 0;
			bool flag4 = omissionTransactionAdvanced && omittedSuppressedBeforeRecovery && omittedRecovered && !omittedSuppressedAfterRecovery && omittedRetryRequested && flag2;
			bool flag5 = num3 && cachedOffscreenBeforeExpansion == 12 && unnotifiedBlankSamples == 0 && periodicBlankSamples == 0 && flag3 && aggregateRenderStats.CrowdRoots == 640 && aggregateRenderStats.FallbackRoots == 0 && num2 >= minimumExpectedSuppressed && faultRecoveryPassed && flag4;
			GD.Print($"{"ADOBE_ANIMATE_SUPER_BIG_MAP_CROWD_CONTINUITY_RESULT"} passed={flag5} roots={aggregateRenderStats.CrowdRoots}/{640} minimumCrowd={minimumCrowdRoots} suppressed={num2}/{640} minimumSuppressed={minimumSuppressedRoots} cachedOffscreenBeforeExpansion={cachedOffscreenBeforeExpansion}/{12} unnotifiedBlank={unnotifiedBlankSamples}/{12} periodicBlank={periodicBlankSamples}/{12} stateTexels={aggregateRenderStats.CrowdStateTexels} stateCapacity={aggregateRenderStats.CrowdStateCapacityTexels} multiPage={flag3} blankSamples={blankSamples}/{sampledFrames * 12 + 12} faultPhases={recoveredFaultPhases}/{TransactionFaultPhases.Length} faultBlank={faultBlankSamples}/{TransactionFaultPhases.Length * 12 * 2} faultCountExact={faultCountExact} minimumFaultRetry={minimumFaultRetryRoots}/{640} minimumFaultRepublished={minimumFaultRepublishedRoots}/{minimumExpectedSuppressed} omissionAdvanced={omissionTransactionAdvanced} omittedSuppressedBefore={omittedSuppressedBeforeRecovery} omittedRecovered={omittedRecovered} omittedSuppressedAfter={omittedSuppressedAfterRecovery} omittedRetry={omittedRetryRequested} omittedRepublished={flag2} unnotified={string.Join(',', unnotifiedPixels)} periodic={string.Join(',', periodicPixels)} baseline={string.Join(',', baselinePixels)} minimum={string.Join(',', minimumPixels)} final={string.Join(',', finalPixels)} fallback={aggregateRenderStats.FallbackRoots} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag5) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_SUPER_BIG_MAP_CROWD_CONTINUITY_RESULT"} exception={value}");
		}
		finally
		{
			if (runtimeManagerProcessSuspended && GodotObject.IsInstanceValid(AdobeAnimateRuntimeManager.Instance))
			{
				AdobeAnimateRuntimeManager.Instance.SetProcess(enable: true);
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			Engine.MaxFps = _originalMaxFps;
			for (int num4 = 0; num4 < characters.Count; num4++)
			{
				if (GodotObject.IsInstanceValid(characters[num4]))
				{
					characters[num4].QueueFree();
				}
			}
		}
		GetTree().Quit(exitCode);
	}

	private static PackedScene[] LoadMixedScenes()
	{
		PackedScene[] array = new PackedScene[MixedCharacterScenePaths.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ResourceLoader.Load<PackedScene>(MixedCharacterScenePaths[i], null, ResourceLoader.CacheMode.Reuse);
			if (!GodotObject.IsInstanceValid(array[i]))
			{
				throw new InvalidOperationException("Production mixed character scene is unavailable: " + MixedCharacterScenePaths[i]);
			}
		}
		return array;
	}

	private static void SpawnCharacters(PackedScene[] scenes, Node2D world, List<TowerDefenseCharacter> characters, List<AdobeAnimateSprite> sprites, int targetCount)
	{
		while (sprites.Count < targetCount)
		{
			int count = sprites.Count;
			int num = count % 405;
			int num2 = count / 405;
			int num3 = num % 27;
			int num4 = num / 27;
			TowerDefenseCharacter towerDefenseCharacter = scenes[count % scenes.Length].Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
			towerDefenseCharacter.Name = $"SuperBigMapCharacter{count}";
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.skipDestroySet = true;
			towerDefenseCharacter.ProcessMode = ProcessModeEnum.Always;
			towerDefenseCharacter.Position = new Vector2(244f + (float)num3 * 69f + (float)num2 * 6f, 76f + (float)num4 * 74f);
			towerDefenseCharacter.ZIndex = num4 * 15 + 1 + num2;
			world.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter.sprite))
			{
				throw new InvalidOperationException("Production mixed character has no animation root: " + MixedCharacterScenePaths[count % scenes.Length]);
			}
			towerDefenseCharacter.sprite.ProcessMode = ProcessModeEnum.Always;
			towerDefenseCharacter.sprite.pause = false;
			towerDefenseCharacter.sprite.timeScale = 0.0;
			towerDefenseCharacter.sprite.RefreshProcessScheduling();
			characters.Add(towerDefenseCharacter);
			sprites.Add(towerDefenseCharacter.sprite);
		}
	}

	private static AdobeAnimateSprite[] SelectExpansionSamples(List<AdobeAnimateSprite> sprites)
	{
		AdobeAnimateSprite[] array = new AdobeAnimateSprite[12];
		for (int i = 0; i < array.Length; i++)
		{
			int index = i / 2 * 27 + ((i % 2 == 0) ? 21 : 25);
			array[i] = sprites[index];
		}
		return array;
	}

	private static int CountBlankSamples(int[] pixels, int[] baseline)
	{
		int num = 0;
		for (int i = 0; i < pixels.Length; i++)
		{
			if (baseline[i] > 0 && pixels[i] < Math.Max(8, baseline[i] / 4))
			{
				num++;
			}
		}
		return num;
	}

	private static int CountCachedOffscreenSamples(AdobeAnimateSprite[] samples)
	{
		int num = 0;
		for (int i = 0; i < samples.Length; i++)
		{
			samples[i].GetRuntimeCrowdCullingDebugState(out var cached, out var cachedVisible, out var _, out var _, out var _);
			if (cached && !cachedVisible)
			{
				num++;
			}
		}
		return num;
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

	private async Task<int[]> CaptureSamplePixels(AdobeAnimateSprite[] samples)
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		int[] array = new int[samples.Length];
		for (int i = 0; i < samples.Length; i++)
		{
			array[i] = CountForegroundPixels(image, CreateCaptureRegion(samples[i], image.GetSize()));
		}
		return array;
	}

	private static Rect2I CreateCaptureRegion(AdobeAnimateSprite sprite, Vector2I imageSize)
	{
		Rect2 rect = AdobeAnimateDefinitionCache.GetOrBuild(sprite.flashAnimeData)?.LocalBounds ?? new Rect2(new Vector2(-64f, -128f), new Vector2(128f, 160f));
		Transform2D globalTransformWithCanvas = sprite.GetGlobalTransformWithCanvas();
		Vector2 position = globalTransformWithCanvas * rect.Position;
		Vector2 to = globalTransformWithCanvas * (rect.Position + new Vector2(rect.Size.X, 0f));
		Vector2 to2 = globalTransformWithCanvas * (rect.Position + rect.Size);
		Vector2 to3 = globalTransformWithCanvas * (rect.Position + new Vector2(0f, rect.Size.Y));
		Rect2 rect2 = new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3)
			.Abs()
			.Grow(2f);
		int num = Math.Clamp(Mathf.FloorToInt(rect2.Position.X), 0, imageSize.X);
		int num2 = Math.Clamp(Mathf.FloorToInt(rect2.Position.Y), 0, imageSize.Y);
		int num3 = Math.Clamp(Mathf.CeilToInt(rect2.End.X), num, imageSize.X);
		int num4 = Math.Clamp(Mathf.CeilToInt(rect2.End.Y), num2, imageSize.Y);
		return new Rect2I(num, num2, num3 - num, num4 - num2);
	}

	private static int CountForegroundPixels(Image image, Rect2I region)
	{
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

	private static int CountSuppressedRoots(List<AdobeAnimateSprite> sprites)
	{
		int num = 0;
		for (int i = 0; i < sprites.Count; i++)
		{
			sprites[i].GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
			if (nativeCanvasSuppressed)
			{
				num++;
			}
		}
		return num;
	}

	private static int CountRetryRoots(List<AdobeAnimateSprite> sprites)
	{
		int num = 0;
		for (int i = 0; i < sprites.Count; i++)
		{
			if (sprites[i].NeedsRuntimeRenderSubmission)
			{
				num++;
			}
		}
		return num;
	}

	private static AdobeAnimateSprite FindSuppressedRoot(List<AdobeAnimateSprite> sprites, AdobeAnimateSprite excluded)
	{
		for (int i = 0; i < sprites.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = sprites[i];
			if (adobeAnimateSprite != excluded && IsNativeCanvasSuppressed(adobeAnimateSprite))
			{
				return adobeAnimateSprite;
			}
		}
		throw new InvalidOperationException("No Crowd-managed production animation root is available for omission recovery.");
	}

	private static bool IsNativeCanvasSuppressed(AdobeAnimateSprite sprite)
	{
		sprite.GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
		return nativeCanvasSuppressed;
	}
}
