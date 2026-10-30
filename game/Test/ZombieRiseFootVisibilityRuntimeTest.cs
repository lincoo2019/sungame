using System;
using System.Threading.Tasks;
using Godot;

public partial class ZombieRiseFootVisibilityRuntimeTest : Node2D
{
	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ResultMarker = "ZOMBIE_RISE_FOOT_VISIBILITY_RESULT";

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.025f);

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	private double _originalTimeScale;

	private TowerDefenseZombieNormal _zombie;

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
			_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
			_originalTimeScale = Engine.TimeScale;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			AdobeAnimateRenderManager.RasterCompositeEnabled = true;
			Engine.TimeScale = 2.0;
			ColorRect node = new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(1080f, 600f),
				MouseFilter = Control.MouseFilterEnum.Ignore,
				ZIndex = -4096
			};
			AddChild(node, forceReadableName: false, InternalMode.Disabled);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				throw new InvalidOperationException("Normal zombie scene could not be loaded.");
			}
			_zombie = packedScene.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(_zombie))
			{
				throw new InvalidOperationException("Normal zombie scene could not be instantiated.");
			}
			_zombie.inGame = false;
			_zombie.editorPreviewMode = false;
			_zombie.Position = new Vector2(540f, 330f);
			_zombie.Visible = true;
			AddChild(_zombie, forceReadableName: false, InternalMode.Disabled);
			await WaitProcessFrames(16);
			_zombie.sprite.SetAnimation("Walk1");
			_zombie.sprite.timeScale = 0.0;
			_zombie.sprite.SetVerticalClip(enabled: false, -10000f, 10000f);
			await WaitProcessFrames(6);
			int groundScreenY = ResolveGroundScreenY(_zombie);
			using Image baseline = await CaptureFrame();
			int baselineFootPixels = CountFootPixels(baseline, groundScreenY);
			if (baselineFootPixels < 24)
			{
				throw new InvalidOperationException($"Baseline zombie feet are not visible enough; pixels={baselineFootPixels} groundY={groundScreenY}.");
			}
			_zombie.Rise(1.0, 0.0, createDirt: false);
			await WaitForNearSurfaceRise(240);
			double nearSurfaceGroundHeight = _zombie.groundHeight;
			using Image nearSurface = await CaptureFrame();
			int nearSurfaceFootPixels = CountFootPixels(nearSurface, groundScreenY);
			VerticalClipState nearSurfaceClip = _zombie.sprite.GetVerticalClipState();
			bool nearSurfaceFeetVisible = _zombie.isRise && nearSurfaceFootPixels >= Mathf.CeilToInt((float)baselineFootPixels * 0.25f);
			bool nearSurfaceClipHasFootMargin = nearSurfaceClip.Enabled && nearSurfaceClip.DownY >= (float)groundScreenY + 16f;
			string nearSurfaceCapturePath = ProjectSettings.GlobalizePath("user://ZombieRiseFootVisibilityNearSurface.png");
			nearSurface.SavePng(nearSurfaceCapturePath);
			await WaitForRiseCompletion(180);
			_zombie.sprite.SetAnimation("Walk1");
			_zombie.sprite.timeScale = 0.0;
			await WaitProcessFrames(6);
			using Image image = await CaptureFrame();
			int num = CountFootPixels(image, groundScreenY);
			VerticalClipState verticalClipState = _zombie.sprite.GetVerticalClipState();
			bool visible = _zombie.shadowSprite.Visible;
			bool flag = num >= Mathf.CeilToInt((float)baselineFootPixels * 0.9f);
			bool flag2 = nearSurfaceFeetVisible && nearSurfaceClipHasFootMargin && !_zombie.isRise && !verticalClipState.Enabled && visible && flag;
			string text = ProjectSettings.GlobalizePath("user://ZombieRiseFootVisibilityRuntime.png");
			image.SavePng(text);
			GD.Print($"{"ZOMBIE_RISE_FOOT_VISIBILITY_RESULT"} passed={flag2} nearSurfaceGroundHeight={nearSurfaceGroundHeight:F2} nearSurfaceClipEnabled={nearSurfaceClip.Enabled} nearSurfaceClipDown={nearSurfaceClip.DownY:F1} nearSurfaceClipHasFootMargin={nearSurfaceClipHasFootMargin} nearSurfaceFootPixels={nearSurfaceFootPixels} nearSurfaceFeetVisible={nearSurfaceFeetVisible} isRise={_zombie.isRise} clipEnabled={verticalClipState.Enabled} clipDown={verticalClipState.DownY:F1} shadowVisible={visible} baselineFootPixels={baselineFootPixels} settledFootPixels={num} groundY={groundScreenY} renderer={RenderingServer.GetCurrentRenderingMethod()} nearCapture={nearSurfaceCapturePath} capture={text}");
			exitCode = ((!flag2) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ZOMBIE_RISE_FOOT_VISIBILITY_RESULT"} passed=False exception={value}");
		}
		finally
		{
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
			Engine.TimeScale = _originalTimeScale;
			if (GodotObject.IsInstanceValid(_zombie))
			{
				_zombie.QueueFree();
			}
		}
		GetTree().Quit(exitCode);
	}

	private static int ResolveGroundScreenY(TowerDefenseCharacter character)
	{
		Transform2D screenTransform = character.GetViewport().GetScreenTransform();
		screenTransform.Origin = Vector2.Zero;
		Vector2 vector = character.GetLogicalGlobalPosition(character.transformPoint) + new Vector2(0f, (float)character.groundHeight);
		return Mathf.RoundToInt((screenTransform * vector).Y);
	}

	private static int CountFootPixels(Image image, int groundScreenY)
	{
		int num = 0;
		int num2 = Math.Max(0, 465);
		int num3 = Math.Min(image.GetWidth(), 615);
		int num4 = Math.Max(0, groundScreenY + 1);
		int num5 = Math.Min(image.GetHeight(), groundScreenY + 34);
		for (int i = num4; i < num5; i++)
		{
			for (int j = num2; j < num3; j++)
			{
				if (ColorDistance(image.GetPixel(j, i), BackgroundColor) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private async Task WaitForRiseCompletion(int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (!_zombie.isRise)
			{
				return;
			}
		}
		throw new TimeoutException($"Rise did not complete within {maximumFrames} process frames.");
	}

	private async Task WaitForNearSurfaceRise(int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (_zombie.isRise && _zombie.groundHeight >= -8.0)
			{
				return;
			}
			if (!_zombie.isRise)
			{
				break;
			}
		}
		throw new TimeoutException($"Rise did not enter the near-surface interval within {maximumFrames} process frames.");
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<Image> CaptureFrame()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		return GetViewport().GetTexture().GetImage();
	}

	private static float ColorDistance(Color left, Color right)
	{
		return Mathf.Max(Mathf.Max(Mathf.Abs(left.R - right.R), Mathf.Abs(left.G - right.G)), Mathf.Max(Mathf.Abs(left.B - right.B), Mathf.Abs(left.A - right.A)));
	}
}
