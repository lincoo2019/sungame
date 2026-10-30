using System;
using System.Threading.Tasks;
using Godot;

public partial class GravestoneRoofClippingVisualRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	private TowerDefenseGravestone _gravestone;

	public override async void _Ready()
	{
		int exitCode = 2;
		AdobeAnimateRenderBackend originalBackend = Global.Instance.adobeAnimateRenderBackend;
		int originalMaxFps = Engine.MaxFps;
		try
		{
			_ = 2;
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
				TowerDefenseMapConfig roof = GD.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Roof/Config/FrontlawnMapRoof.tres");
				AdobeAnimateRenderBackend[] array = new AdobeAnimateRenderBackend[2]
				{
					AdobeAnimateRenderBackend.GpuCrowd,
					AdobeAnimateRenderBackend.CpuPose
				};
				foreach (AdobeAnimateRenderBackend backend in array)
				{
					Global.Instance.adobeAnimateRenderBackend = backend;
					int[] array2 = new int[4] { 1, 3, 5, 6 };
					foreach (int num in array2)
					{
						TowerDefenseCellConfig towerDefenseCellConfig = null;
						foreach (TowerDefenseCellConfig item in roof.cellConfig)
						{
							if (num >= item.pos.X && num <= item.pos.Z)
							{
								towerDefenseCellConfig = item;
								break;
							}
						}
						using TowerDefenseCellInstance cell = new TowerDefenseCellInstance
						{
							groundHeightCurve = towerDefenseCellConfig?.groundHeightCurve
						};
						await VerifyRise(backend, $"roof-column-{num}", cell, (float)cell.GetGroundHeight(), inWater: false, 1f);
					}
					using TowerDefenseCellInstance scaledCell = new TowerDefenseCellInstance
					{
						groundHeightCurve = roof.cellConfig[0].groundHeightCurve
					};
					await VerifyRise(backend, "roof-scaled", scaledCell, (float)scaledCell.GetGroundHeight(), inWater: false, 1.25f);
					await VerifyRise(backend, "water", null, -25f, inWater: true, 1f);
				}
				exitCode = ((_failures != 0) ? 2 : 0);
				GD.Print($"GRAVESTONE_ROOF_CLIPPING_VISUAL_RESULT passed={_failures == 0} checks={_checks} failures={_failures} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			}
			catch (Exception value)
			{
				GD.PrintErr($"GRAVESTONE_ROOF_CLIPPING_VISUAL_RESULT passed=False checks={_checks} exception={value}");
			}
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = originalBackend;
			Engine.MaxFps = originalMaxFps;
			if (GodotObject.IsInstanceValid(_gravestone))
			{
				_gravestone.QueueFree();
			}
			await WaitFrames(3);
		}
		GetTree().Quit(exitCode);
	}

	private async Task VerifyRise(AdobeAnimateRenderBackend backend, string scenario, TowerDefenseCellInstance cell, float groundHeight, bool inWater, float scale)
	{
		_gravestone = GD.Load<PackedScene>("res://Asset/Anime/Character/GraveStone/Default/Scene/TowerDefenseGraveStoneDefault.tscn").Instantiate<TowerDefenseGravestone>(PackedScene.GenEditState.Disabled);
		_gravestone.inGame = false;
		_gravestone.rise = false;
		_gravestone.Position = new Vector2(540f, 280f);
		_gravestone.Scale = Vector2.One * scale;
		_gravestone.cell = cell;
		_gravestone.groundHeight = groundHeight;
		AddChild(_gravestone, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(8);
		_gravestone.groundHeightComponent?.SetAlive(alive: false);
		_gravestone.groundHeight = groundHeight;
		_gravestone.inWater = inWater;
		_gravestone.sprite.SetAnimation("Idle1");
		_gravestone.sprite.pause = true;
		_gravestone.sprite.timeScale = 0.0;
		_gravestone.shadowComponent.SetShadowVisible(visible: false);
		await WaitFrames(4);
		using Image reference = await CaptureImage();
		int referencePixels = CountPixels(reference);
		float waterSurfaceY = _gravestone.ToGlobal(_gravestone.transformPoint.Position).Y;
		Check(referencePixels > 2000, $"{backend}/{scenario} 静止墓碑未完整绘出: pixels={referencePixels}");
		_gravestone.Rise(0.65, 0.0, createDirt: false, changeState: false, 150.0, emitRiseOver: false);
		await WaitForSurface(groundHeight);
		using Image nearSurface = await CaptureImage();
		int nearSurfacePixels = CountPixels(nearSurface);
		VerticalClipState clip = _gravestone.sprite.GetVerticalClipState();
		Check(_gravestone.isRise && clip.Enabled, $"{backend}/{scenario} 没有采集到出土期间的裁剪帧。");
		if (inWater)
		{
			Check(Math.Abs(clip.DownY - waterSurfaceY) < 1f, $"{backend}/{scenario} 水面裁剪线发生变化: actual={clip.DownY}, expected={waterSurfaceY}");
			Check((float)nearSurfacePixels < (float)referencePixels * 0.9f && (float)nearSurfacePixels > (float)referencePixels * 0.25f, $"{backend}/{scenario} 水下部分未按水面裁剪: {nearSurfacePixels}/{referencePixels}");
		}
		else
		{
			bool flag = (float)nearSurfacePixels >= (float)referencePixels * 0.97f;
			Check(flag, $"{backend}/{scenario} 接近地表时墓碑仍然缺块: {nearSurfacePixels}/{referencePixels}, clip={clip.DownY}, height={_gravestone.groundHeight:F2}");
			if (!flag)
			{
				reference.SavePng(ProjectSettings.GlobalizePath($"user://{backend}-{scenario}-reference.png"));
				nearSurface.SavePng(ProjectSettings.GlobalizePath($"user://{backend}-{scenario}-clipped.png"));
			}
		}
		for (int frame = 0; frame < 120; frame++)
		{
			if (!_gravestone.isRise)
			{
				break;
			}
			await WaitFrames(1);
		}
		await WaitFrames(4);
		using Image settled = await CaptureImage();
		int num = CountPixels(settled);
		Check(!_gravestone.isRise && Math.Abs(_gravestone.groundHeight - (double)groundHeight) < 0.009999999776482582, $"{backend}/{scenario} 出土后没有恢复目标地面高度。");
		if (!inWater)
		{
			Check(!_gravestone.sprite.GetVerticalClipState().Enabled && (float)num >= (float)referencePixels * 0.99f, $"{backend}/{scenario} 出土后残留裁剪: {num}/{referencePixels}");
		}
		GD.Print($"GRAVESTONE_ROOF_CLIPPING_PIXELS backend={backend} scenario={scenario} height={groundHeight} scale={scale} full={referencePixels} nearSurface={nearSurfacePixels} settled={num} clip={clip.DownY}");
		_gravestone.QueueFree();
		await WaitFrames(3);
		_gravestone = null;
	}

	private async Task WaitForSurface(float groundHeight)
	{
		for (int frame = 0; frame < 180; frame++)
		{
			await WaitFrames(1);
			if (_gravestone.isRise && _gravestone.groundHeight >= (double)(groundHeight - 2f))
			{
				return;
			}
		}
		throw new TimeoutException("墓碑未进入接近地表的出土阶段。");
	}

	private static int CountPixels(Image image)
	{
		int num = 0;
		for (int i = 0; i < image.GetHeight(); i++)
		{
			for (int j = 0; j < image.GetWidth(); j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PrintErr("GRAVESTONE_ROOF_CLIPPING_FAILURE " + message);
		}
	}
}
