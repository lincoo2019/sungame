using System;
using System.Text;
using Godot;

public partial class LoadingAnimationPresentationRuntimeTest : Node
{
	private const string ResultMarker = "LOADING_ANIMATION_PRESENTATION_RESULT";

	private const string LoadingScenePath = "res://Scene/Loading/Loading.tscn";

	private const double TimeoutSeconds = 90.0;

	private const double GrassPresentationCompleteProgress = 0.95;

	private static readonly string[] AnimationNodeNames = new string[5] { "LoadBarSprout", "LoadBarSprout2", "LoadBarSprout3", "LoadBarSprout4", "LoadBarZombieHead" };

	public override async void _Ready()
	{
		bool passed = false;
		ulong bootstrapFirstFrameUsec = 0uL;
		double minimumProgress = 1.7976931348623157E+308;
		double maximumProgress = -1.7976931348623157E+308;
		string frameRanges = string.Empty;
		string failure = string.Empty;
		try
		{
			PackedScene packedScene = GD.Load<PackedScene>("res://Scene/Loading/Loading.tscn");
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				throw new InvalidOperationException("无法加载正式 Loading 场景：res://Scene/Loading/Loading.tscn");
			}
			Loading loading = packedScene.Instantiate<Loading>(PackedScene.GenEditState.Disabled);
			AddChild(loading, forceReadableName: false, InternalMode.Disabled);
			AdobeAnimateSpriteBase[] animations = ResolveAnimations(loading);
			Button startButton = loading.GetNode<Button>("%StartButton");
			TextureProgressBar progressBar = loading.GetNode<TextureProgressBar>("%ProgressBar");
			Sprite2D sodRollCap = loading.GetNode<Sprite2D>("%SodRollCap");
			bool[] observedIntermediateFrame = new bool[animations.Length];
			int[] minimumObservedFrame = CreateFilledFrameArray(animations.Length, 2147483647);
			int[] maximumObservedFrame = CreateFilledFrameArray(animations.Length, -2147483648);
			bool observedIntermediateGrassProgress = false;
			bool observedSodRollMotion = false;
			Vector2 initialSodRollPosition = sodRollCap.Position;
			ulong startedUsec = Time.GetTicksUsec();
			while (loading.BootstrapFirstFramePresentedUsec == 0L)
			{
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				SamplePresentedFrame(animations, observedIntermediateFrame, minimumObservedFrame, maximumObservedFrame, progressBar, sodRollCap, initialSodRollPosition, ref observedIntermediateGrassProgress, ref observedSodRollMotion, ref minimumProgress, ref maximumProgress);
				RequireWithinTimeout(startedUsec, "Bootstrap 首帧没有完成显示提交");
			}
			bootstrapFirstFrameUsec = loading.BootstrapFirstFramePresentedUsec;
			if (loading.GameplayResourceLoadRequestedUsec < loading.BootstrapFirstFramePresentedUsec)
			{
				throw new InvalidOperationException($"完整资源请求早于 Bootstrap 首帧：firstFrameUsec={loading.BootstrapFirstFramePresentedUsec}, requestUsec={loading.GameplayResourceLoadRequestedUsec}");
			}
			AdobeAnimateSpriteBase[] array = animations;
			foreach (AdobeAnimateSpriteBase adobeAnimateSpriteBase in array)
			{
				if (!adobeAnimateSpriteBase.Visible || !GodotObject.IsInstanceValid(adobeAnimateSpriteBase.atlasProfileOverride))
				{
					throw new InvalidOperationException($"Bootstrap 动画没有在资源请求前使用专用图集显示：{adobeAnimateSpriteBase.Name}");
				}
			}
			while (startButton.Disabled)
			{
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				SamplePresentedFrame(animations, observedIntermediateFrame, minimumObservedFrame, maximumObservedFrame, progressBar, sodRollCap, initialSodRollPosition, ref observedIntermediateGrassProgress, ref observedSodRollMotion, ref minimumProgress, ref maximumProgress);
				RequireWithinTimeout(startedUsec, $"完整资源与 Loading 演出在 {90.0:F0} 秒内没有共同完成");
			}
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			SamplePresentedFrame(animations, observedIntermediateFrame, minimumObservedFrame, maximumObservedFrame, progressBar, sodRollCap, initialSodRollPosition, ref observedIntermediateGrassProgress, ref observedSodRollMotion, ref minimumProgress, ref maximumProgress);
			ValidatePresentation(animations, observedIntermediateFrame, minimumObservedFrame, maximumObservedFrame, observedIntermediateGrassProgress, observedSodRollMotion, minimumProgress, maximumProgress);
			ResourceManager.Instance.RequireFullGameplayResourcesReady("LoadingAnimationPresentationRuntimeTest");
			if (ResourceManager.Instance.LateCharacterResourceLoadCount != 0)
			{
				throw new InvalidOperationException($"Loading 完成后发生迟加载：{ResourceManager.Instance.LateCharacterResourceLoadCount}");
			}
			frameRanges = BuildFrameRanges(minimumObservedFrame, maximumObservedFrame);
			passed = true;
		}
		catch (Exception ex)
		{
			failure = ex.ToString().Replace('\r', ' ').Replace('\n', ' ');
		}
		GD.Print($"{"LOADING_ANIMATION_PRESENTATION_RESULT"} passed={passed} bootstrapFirstFrameUsec={bootstrapFirstFrameUsec} progressRange={minimumProgress:F4}..{maximumProgress:F4} frameRanges={frameRanges} state={ResourceManager.Instance?.CurrentGameplayResourceLoadState} fullWallMs={ResourceManager.Instance?.FullGameplayResourceLoadMetrics.FullWallMilliseconds ?? 0.0:F3} failure={failure}");
		GetTree().Quit((!passed) ? 1 : 0);
	}

	private static AdobeAnimateSpriteBase[] ResolveAnimations(Loading loading)
	{
		AdobeAnimateSpriteBase[] array = new AdobeAnimateSpriteBase[AnimationNodeNames.Length];
		for (int i = 0; i < AnimationNodeNames.Length; i++)
		{
			array[i] = loading.GetNode<AdobeAnimateSpriteBase>("%" + AnimationNodeNames[i]);
			if (!GodotObject.IsInstanceValid(array[i]))
			{
				throw new InvalidOperationException("Loading 动画节点无效：" + AnimationNodeNames[i]);
			}
		}
		return array;
	}

	private static int[] CreateFilledFrameArray(int length, int value)
	{
		int[] array = new int[length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = value;
		}
		return array;
	}

	private static void SamplePresentedFrame(AdobeAnimateSpriteBase[] animations, bool[] observedIntermediateFrame, int[] minimumObservedFrame, int[] maximumObservedFrame, TextureProgressBar progressBar, Sprite2D sodRollCap, Vector2 initialSodRollPosition, ref bool observedIntermediateGrassProgress, ref bool observedSodRollMotion, ref double minimumProgress, ref double maximumProgress)
	{
		double value = progressBar.Value;
		minimumProgress = Math.Min(minimumProgress, value);
		maximumProgress = Math.Max(maximumProgress, value);
		if (value > 0.0 && value < 0.95)
		{
			observedIntermediateGrassProgress = true;
		}
		if (sodRollCap.Visible && value < 0.95 && sodRollCap.Position.DistanceTo(initialSodRollPosition) > 0.5f)
		{
			observedSodRollMotion = true;
		}
		for (int i = 0; i < animations.Length; i++)
		{
			AdobeAnimateSpriteBase adobeAnimateSpriteBase = animations[i];
			int frameIndex = adobeAnimateSpriteBase.frameIndex;
			minimumObservedFrame[i] = Math.Min(minimumObservedFrame[i], frameIndex);
			maximumObservedFrame[i] = Math.Max(maximumObservedFrame[i], frameIndex);
			int num = Math.Max(adobeAnimateSpriteBase.clipRange.X, adobeAnimateSpriteBase.clipRange.Y - 1);
			if (!adobeAnimateSpriteBase.pause && frameIndex > adobeAnimateSpriteBase.clipRange.X && frameIndex < num)
			{
				observedIntermediateFrame[i] = true;
			}
		}
	}

	private static void ValidatePresentation(AdobeAnimateSpriteBase[] animations, bool[] observedIntermediateFrame, int[] minimumObservedFrame, int[] maximumObservedFrame, bool observedIntermediateGrassProgress, bool observedSodRollMotion, double minimumProgress, double maximumProgress)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (!observedIntermediateGrassProgress)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, stringBuilder2);
			handler.AppendLiteral("草皮没有呈现中间进度，范围=");
			handler.AppendFormatted(minimumProgress, "F4");
			handler.AppendLiteral("..");
			handler.AppendFormatted(maximumProgress, "F4");
			handler.AppendLiteral("；");
			stringBuilder3.Append(ref handler);
		}
		if (!observedSodRollMotion)
		{
			stringBuilder.Append("草卷没有在可见阶段发生位移；");
		}
		for (int i = 0; i < animations.Length; i++)
		{
			AdobeAnimateSpriteBase obj = animations[i];
			if (!observedIntermediateFrame[i])
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 3, stringBuilder2);
				handler.AppendFormatted(AnimationNodeNames[i]);
				handler.AppendLiteral(" 未呈现中间帧，范围=");
				handler.AppendFormatted(minimumObservedFrame[i]);
				handler.AppendLiteral("..");
				handler.AppendFormatted(maximumObservedFrame[i]);
				handler.AppendLiteral("；");
				stringBuilder4.Append(ref handler);
			}
			if (!obj.clipOver)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
				handler.AppendFormatted(AnimationNodeNames[i]);
				handler.AppendLiteral(" 尚未完成非循环动画；");
				stringBuilder5.Append(ref handler);
			}
		}
		if (stringBuilder.Length > 0)
		{
			throw new InvalidOperationException(stringBuilder.ToString());
		}
	}

	private static string BuildFrameRanges(int[] minimumObservedFrame, int[] maximumObservedFrame)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < AnimationNodeNames.Length; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 3, stringBuilder2);
			handler.AppendFormatted(AnimationNodeNames[i]);
			handler.AppendLiteral(":");
			handler.AppendFormatted(minimumObservedFrame[i]);
			handler.AppendLiteral("..");
			handler.AppendFormatted(maximumObservedFrame[i]);
			stringBuilder2.Append(ref handler);
		}
		return stringBuilder.ToString();
	}

	private static void RequireWithinTimeout(ulong startedUsec, string message)
	{
		if ((double)(Time.GetTicksUsec() - startedUsec) / 1000000.0 >= 90.0)
		{
			throw new TimeoutException(message);
		}
	}
}
