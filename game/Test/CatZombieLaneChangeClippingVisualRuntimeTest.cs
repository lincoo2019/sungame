using System;
using System.Threading.Tasks;
using Godot;

public partial class CatZombieLaneChangeClippingVisualRuntimeTest : Node
{
	private int _checks;

	public override async void _Ready()
	{
		int exitCode = 2;
		AdobeAnimateRenderBackend originalBackend = Global.Instance.adobeAnimateRenderBackend;
		int originalMaxFps = Engine.MaxFps;
		TowerDefenseZombieCat zombie = null;
		try
		{
			_ = 9;
			try
			{
				Engine.MaxFps = 120;
				AddChild(new ColorRect
				{
					Color = Colors.Black,
					Size = new Vector2(1080f, 600f),
					ZIndex = -4096,
					MouseFilter = Control.MouseFilterEnum.Ignore
				}, forceReadableName: false, InternalMode.Disabled);
				zombie = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter3/Cat/Scene/TowerDefenseZombieCat.tscn").Instantiate<TowerDefenseZombieCat>(PackedScene.GenEditState.Disabled);
				zombie.inGame = false;
				zombie.editorPreviewMode = true;
				zombie.Position = new Vector2(540f, 350f);
				AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(12);
				zombie.sprite.SetAnimation("Walk");
				zombie.sprite.pause = true;
				zombie.sprite.timeScale = 0.0;
				zombie.shadowComponent.SetShadowVisible(visible: false);
				GarlicComponent garlic = zombie.componentManager.GetRuntime<GarlicComponent>();
				zombie.waterInteractionComponent = zombie.componentManager.GetRuntime<WaterInteractionComponent>() ?? throw new InvalidOperationException("测试角色缺少涉水组件。");
				garlic.changeLineDuration = 0.25f;
				garlic.moveDownChance = 1f;
				AdobeAnimateRenderBackend[] array = new AdobeAnimateRenderBackend[2]
				{
					AdobeAnimateRenderBackend.GpuCrowd,
					AdobeAnimateRenderBackend.CpuPose
				};
				foreach (AdobeAnimateRenderBackend backend in array)
				{
					Global.Instance.adobeAnimateRenderBackend = backend;
					zombie.inWater = false;
					zombie.gridPos = new Vector2I(4, 2);
					zombie.SetLogicalGlobalPosition(new Vector2(540f, 350f));
					zombie.OutWaterDiscardSet();
					await WaitFrames(8);
					using Image reference = await CaptureImage();
					int minimumY;
					int maximumY;
					int referencePixels = CountBodyPixels(reference, out minimumY, out maximumY);
					Require(referencePixels > 2000, $"{backend} 完整猫战士没有绘出: pixels={referencePixels}");
					int cutY = (minimumY + maximumY) / 2;
					zombie.SetSpriteGroupShaderParameter("discardDownPos", cutY);
					await WaitFrames(4);
					using Image clipped = await CaptureImage();
					int topPixels = 0;
					int keptTopPixels = 0;
					int bottomPixels = 0;
					int removedBottomPixels = 0;
					int minimumY2;
					for (int j = minimumY; j <= maximumY; j++)
					{
						for (int k = 0; k < reference.GetWidth(); k++)
						{
							Color pixel = reference.GetPixel(k, j);
							if (IsBodyPixel(pixel) && Math.Abs(j - cutY) > 1)
							{
								Color pixel2 = clipped.GetPixel(k, j);
								if (j < cutY)
								{
									minimumY2 = topPixels++;
									keptTopPixels += ((ColorDistance(pixel2, pixel) < 0.08f) ? 1 : 0);
								}
								else
								{
									minimumY2 = bottomPixels++;
									removedBottomPixels += ((!IsBodyPixel(pixel2)) ? 1 : 0);
								}
							}
						}
					}
					Require(topPixels > 100 && (float)keptTopPixels >= (float)topPixels * 0.98f, $"{backend} 水线上方身体被误裁剪: {keptTopPixels}/{topPixels}");
					Require(bottomPixels > 100 && (float)removedBottomPixels >= (float)bottomPixels * 0.98f, $"{backend} 水线没有随参数更新: {removedBottomPixels}/{bottomPixels}");
					Task laneChange = garlic.ChangeLine();
					await WaitFrames(3);
					int minimumMovingPixels = 2147483647;
					int samples = 0;
					int maximumY2;
					do
					{
						using Image image = await CaptureImage();
						int val = CountBodyPixels(image, out minimumY2, out maximumY2);
						minimumMovingPixels = Math.Min(minimumMovingPixels, val);
						maximumY2 = samples++;
					}
					while (!laneChange.IsCompleted && samples < 60);
					Require(laneChange.IsCompleted && !zombie.isChangeLine && zombie.gridPos.Y == 3, $"{backend} 正式换行没有完成。");
					await laneChange;
					await WaitFrames(4);
					using Image image2 = await CaptureImage();
					int num = CountBodyPixels(image2, out maximumY2, out minimumY2);
					Require(samples > 1 && (float)minimumMovingPixels >= (float)referencePixels * 0.95f, $"{backend} 换行期间身体缺失: minimum={minimumMovingPixels}, full={referencePixels}, samples={samples}");
					Require(!zombie.sprite.GetVerticalClipState().Enabled && (float)num >= (float)referencePixels * 0.98f, $"{backend} 换行后仍残留旧裁剪线: restored={num}, full={referencePixels}");
					GD.Print($"CAT_LANE_CLIPPING_PIXELS backend={backend} full={referencePixels} cutY={cutY} top={keptTopPixels}/{topPixels} bottom={removedBottomPixels}/{bottomPixels} movingMin={minimumMovingPixels} restored={num} samples={samples}");
				}
				Require(_checks == 12, "实际执行的画面回归数量不足。");
				GD.Print($"CAT_LANE_CLIPPING_VISUAL_RESULT passed=True checks={_checks} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = 0;
			}
			catch (Exception value)
			{
				GD.PrintErr($"CAT_LANE_CLIPPING_VISUAL_RESULT passed=False checks={_checks} exception={value}");
			}
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = originalBackend;
			Engine.MaxFps = originalMaxFps;
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			await WaitFrames(3);
		}
		GetTree().Quit(exitCode);
	}

	private static int CountBodyPixels(Image image, out int minimumY, out int maximumY)
	{
		int num = 0;
		minimumY = image.GetHeight();
		maximumY = 0;
		for (int i = 0; i < image.GetHeight(); i++)
		{
			for (int j = 0; j < image.GetWidth(); j++)
			{
				if (IsBodyPixel(image.GetPixel(j, i)))
				{
					num++;
					minimumY = Math.Min(minimumY, i);
					maximumY = Math.Max(maximumY, i);
				}
			}
		}
		return num;
	}

	private static bool IsBodyPixel(Color color)
	{
		return Math.Max(color.R, Math.Max(color.G, color.B)) > 0.08f;
	}

	private static float ColorDistance(Color first, Color second)
	{
		return Math.Max(Math.Abs(first.R - second.R), Math.Max(Math.Abs(first.G - second.G), Math.Abs(first.B - second.B)));
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

	private void Require(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
