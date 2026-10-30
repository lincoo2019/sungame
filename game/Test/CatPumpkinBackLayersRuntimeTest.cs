using System;
using System.IO;
using Godot;

public partial class CatPumpkinBackLayersRuntimeTest : Node
{
	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		TowerDefensePlantCatPumpkin plant = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter4/CatPumpkin/Scene/TowerDefensePlantCatPumpkin.tscn", null, ResourceLoader.CacheMode.Reuse).Instantiate<TowerDefensePlantCatPumpkin>(PackedScene.GenEditState.Disabled);
		try
		{
			int num;
			_ = num - 1;
			_ = 1;
			try
			{
				plant.Position = new Vector2(400f, 300f);
				if (!string.IsNullOrEmpty(OS.GetEnvironment("CAT_PUMPKIN_CAPTURE_DIR")))
				{
					Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.CpuPose;
					plant.sprite.forceLocalRender = true;
					plant.sprite.forceCpuPoseRender = true;
				}
				AddChild(plant, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				CatPumpkinSprite sprite = (CatPumpkinSprite)plant.sprite;
				string[] array = new string[3] { "", "Custom0", "" };
				foreach (string skin in array)
				{
					plant.SwitchCustom(skin);
					string[] array2 = new string[2] { "Idle", "Fire" };
					foreach (string clip in array2)
					{
						sprite.SetAnimation(clip);
						bool condition = true;
						foreach (Variant key in sprite.back.flashAnimeData.layerDictionary.Keys)
						{
							StringName stringName = (StringName)key;
							if (stringName != (StringName)"Pumpkin_back" && stringName != (StringName)"skin2" && sprite.back.GetFliter(stringName))
							{
								condition = false;
								GD.Print($"CAT_PUMPKIN_UNEXPECTED_BACK_LAYER skin={skin} clip={clip} layer={stringName}");
							}
						}
						Check(condition, skin + "/" + clip + "：Back只能绘制后半壳，不能重复绘制尾巴、前壳或爪子。");
						Check(sprite.back.GetFliter("Pumpkin_back") == (skin == "") && sprite.back.GetFliter("skin2") == (skin == "Custom0"), skin + "/" + clip + "：后半壳必须保留正确的外观图层。");
						Check(sprite.GetFliter("Cattail_tail2") == (skin == "") && sprite.GetFliter("skin4_2") == (skin == "Custom0"), skin + "/" + clip + "：主体必须保留本外观的尾巴。");
						string captureDirectory = OS.GetEnvironment("CAT_PUMPKIN_CAPTURE_DIR");
						if (!string.IsNullOrEmpty(captureDirectory))
						{
							plant.Scale = Vector2.One * 3f;
							plant.Visible = true;
							sprite.Visible = true;
							sprite.pause = true;
							sprite.back.pause = true;
							sprite.frameIndex = sprite.clipRange.X + 8;
							sprite.elapsedTimer = 8.0 / sprite.frameRate;
							sprite.BatchPhysicsUpdate(0.0);
							sprite.UpdateChild();
							sprite.back.UpdateChild();
							sprite.QueueRedraw();
							sprite.back.QueueRedraw();
							for (int frame = 0; frame < 10; frame++)
							{
								await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
							}
							using Image image = GetViewport().GetTexture().GetImage();
							image.SavePng(Path.Combine(captureDirectory, ((skin == "") ? "Default" : skin) + "-" + clip + ".png"));
						}
					}
				}
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[CatPumpkinBackLayers] {value}");
			}
		}
		finally
		{
			plant.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		GD.Print($"CAT_PUMPKIN_BACK_LAYERS_RESULT passed={_checks == 18 && _failures == 0} checks={_checks} failures={_failures}");
		GetTree().Quit((_checks != 18 || _failures != 0) ? 2 : 0);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[CatPumpkinBackLayers] " + message);
		}
	}
}
