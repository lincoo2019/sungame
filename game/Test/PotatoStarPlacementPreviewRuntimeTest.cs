using System;
using System.Collections.Generic;
using Godot;

public partial class PotatoStarPlacementPreviewRuntimeTest : Node
{
	private const string PotatoPacketPath = "res://Asset/Anime/Character/Plant/Other/PotatoStar/Packet/PlantPotatoStar.tres";

	private const string PotatoSpritePath = "res://Asset/Anime/Character/Plant/Other/PotatoStar/PotatoStar.tscn";

	private const string PumpkinPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Packet/PlantPumpkin.tres";

	private const string PumpkinSpritePath = "res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Pumpkin.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		AdobeAnimateSprite potatoPreview = null;
		AdobeAnimateSprite pumpkinPreview = null;
		AdobeAnimateSprite plantedControl = null;
		PacketPickControl control = null;
		TowerDefensePacketConfig potatoPacket = null;
		TowerDefensePacketConfig pumpkinPacket = null;
		PackedScene potatoSprite = null;
		PackedScene pumpkinSprite = null;
		Resource previousPotatoSprite = null;
		Resource previousPumpkinSprite = null;
		bool hadPotatoSprite = false;
		bool hadPumpkinSprite = false;
		try
		{
			try
			{
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					return;
				}
				potatoPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Other/PotatoStar/Packet/PlantPotatoStar.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				pumpkinPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Packet/PlantPumpkin.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				potatoSprite = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Other/PotatoStar/PotatoStar.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				pumpkinSprite = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter0/Pumpkin/Pumpkin.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(potatoPacket) && GodotObject.IsInstanceValid(potatoSprite), "Real PotatoStar packet and sprite resources must load.");
				Check(GodotObject.IsInstanceValid(pumpkinPacket) && GodotObject.IsInstanceValid(pumpkinSprite), "Real Pumpkin packet and sprite resources must load.");
				if (!GodotObject.IsInstanceValid(potatoPacket) || !GodotObject.IsInstanceValid(potatoSprite) || !GodotObject.IsInstanceValid(pumpkinPacket) || !GodotObject.IsInstanceValid(pumpkinSprite))
				{
					return;
				}
				hadPotatoSprite = ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(potatoPacket.saveKey, out previousPotatoSprite);
				hadPumpkinSprite = ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(pumpkinPacket.saveKey, out previousPumpkinSprite);
				ResourceManager.Instance.CHARCTAER_SPRITE[potatoPacket.saveKey] = potatoSprite;
				ResourceManager.Instance.CHARCTAER_SPRITE[pumpkinPacket.saveKey] = pumpkinSprite;
				control = new PacketPickControl();
				potatoPreview = control.CreatePreviewSprite(potatoPacket);
				pumpkinPreview = control.CreatePreviewSprite(pumpkinPacket);
				Check(GodotObject.IsInstanceValid(potatoPreview), "Production CreatePreviewSprite must instantiate PotatoStar.");
				Check(GodotObject.IsInstanceValid(pumpkinPreview), "Production CreatePreviewSprite must instantiate Pumpkin.");
				if (!GodotObject.IsInstanceValid(potatoPreview))
				{
					goto IL_072f;
				}
				GD.Print("POTATO_STAR_PREVIEW_TREE " + DescribeAnimationTree(potatoPreview));
				Check(potatoPreview.ZIndex == PacketPickControl.ResolvePlacementPreviewZIndex(0), $"PotatoStar placement preview must begin in the row-top layer; got z={potatoPreview.ZIndex}.");
				Check(Mathf.IsEqualApprox(potatoPreview.Modulate.A, 0.5f) && Mathf.IsEqualApprox(potatoPreview.meshColor.A, 0.5f), "PotatoStar placement preview must retain the production half-alpha treatment.");
				plantedControl = potatoSprite.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(plantedControl), "Real PotatoStar sprite must instantiate as the planted rendering control.");
				if (!GodotObject.IsInstanceValid(plantedControl))
				{
					return;
				}
				AddChild(potatoPreview, forceReadableName: false, InternalMode.Disabled);
				AddChild(plantedControl, forceReadableName: false, InternalMode.Disabled);
				potatoPreview.Position = new Vector2(128f, 128f);
				plantedControl.Position = new Vector2(768f, 128f);
				plantedControl.ZIndex = 900;
				potatoPreview.ResetAnimation();
				plantedControl.ResetAnimation();
				potatoPreview.SetFrozenPreview(frozen: true);
				plantedControl.SetFrozenPreview(frozen: true);
				for (int frame = 0; frame < 5; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				Image image = GetViewport().GetTexture().GetImage();
				Color pixel = image.GetPixel(0, 0);
				Rect2I bounds;
				int num = MeasureVisiblePixels(image, pixel, 0, 256, out bounds);
				Rect2I bounds2;
				int num2 = MeasureVisiblePixels(image, pixel, 256, 512, out bounds2);
				double value = ((num2 == 0) ? 0.0 : ((double)num / (double)num2));
				double num3 = ComputeVisibleMaskSimilarity(image, pixel, bounds, bounds2);
				GD.Print($"POTATO_STAR_PREVIEW_PIXELS size={image.GetWidth()}x{image.GetHeight()} preview={num} previewBounds={bounds} control={num2} controlBounds={bounds2} ratio={value:F3} maskSimilarity={num3:F3}");
				Check(num > 250, $"Selected PotatoStar preview must render a visible body, not only a few highlights; got {num} pixels.");
				Check(num2 > 250, $"Planted PotatoStar rendering control must be visible; got {num2} pixels.");
				Check(Math.Abs(bounds.Size.X - bounds2.Size.X) <= 1 && Math.Abs(bounds.Size.Y - bounds2.Size.Y) <= 1, $"Selected PotatoStar bounds must match the planted sprite; preview={bounds}, control={bounds2}.");
				Check(num3 >= 0.94, $"Selected PotatoStar normalized silhouette must match the planted sprite; similarity={num3:F3}.");
				goto IL_072f;
				IL_072f:
				if (GodotObject.IsInstanceValid(pumpkinPreview))
				{
					GD.Print("PUMPKIN_PREVIEW_TREE " + DescribeAnimationTree(pumpkinPreview));
					Check(pumpkinPreview.ZIndex == PacketPickControl.ResolvePlacementPreviewZIndex(0), $"Pumpkin split Back preview must begin in the row-top layer without flattening its child layers; got z={pumpkinPreview.ZIndex}.");
				}
				goto end_IL_0083;
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[PotatoStarPlacementPreviewRuntimeTest] Unexpected exception: {value2}");
				goto end_IL_0083;
			}
			end_IL_0083:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(potatoPreview))
			{
				potatoPreview.QueueFree();
			}
			if (GodotObject.IsInstanceValid(plantedControl))
			{
				plantedControl.QueueFree();
			}
			if (GodotObject.IsInstanceValid(pumpkinPreview))
			{
				pumpkinPreview.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.Free();
			}
			if (GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				if (hadPotatoSprite)
				{
					ResourceManager.Instance.CHARCTAER_SPRITE["PlantPotatoStar"] = previousPotatoSprite;
				}
				else
				{
					ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantPotatoStar");
				}
				if (hadPumpkinSprite)
				{
					ResourceManager.Instance.CHARCTAER_SPRITE["PlantPumpkin"] = previousPumpkinSprite;
				}
				else
				{
					ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantPumpkin");
				}
			}
			for (int frame = 0; frame < 3; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			potatoPacket?.Dispose();
			pumpkinPacket?.Dispose();
			potatoSprite?.Dispose();
			pumpkinSprite?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0;
		GD.Print($"POTATO_STAR_PLACEMENT_PREVIEW_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static int MeasureVisiblePixels(Image image, Color background, int beginX, int endX, out Rect2I bounds)
	{
		bounds = default(Rect2I);
		if (image == null || image.IsEmpty())
		{
			return 0;
		}
		int num = 0;
		int num2 = endX;
		int num3 = image.GetHeight();
		int num4 = beginX - 1;
		int num5 = -1;
		for (int i = 0; i < image.GetHeight(); i++)
		{
			for (int j = beginX; j < endX; j++)
			{
				if (IsForeground(image.GetPixel(j, i), background))
				{
					num++;
					num2 = Math.Min(num2, j);
					num3 = Math.Min(num3, i);
					num4 = Math.Max(num4, j);
					num5 = Math.Max(num5, i);
				}
			}
		}
		if (num > 0)
		{
			bounds = new Rect2I(num2, num3, num4 - num2 + 1, num5 - num3 + 1);
		}
		return num;
	}

	private static double ComputeVisibleMaskSimilarity(Image image, Color background, Rect2I first, Rect2I second)
	{
		int num = Math.Max(first.Size.X, second.Size.X);
		int num2 = Math.Max(first.Size.Y, second.Size.Y);
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				bool num5 = j < first.Size.X && i < first.Size.Y && IsForeground(image.GetPixel(first.Position.X + j, first.Position.Y + i), background);
				bool flag = j < second.Size.X && i < second.Size.Y && IsForeground(image.GetPixel(second.Position.X + j, second.Position.Y + i), background);
				if (num5 || flag)
				{
					num4++;
				}
				if (num5 && flag)
				{
					num3++;
				}
			}
		}
		if (num4 != 0)
		{
			return (double)num3 / (double)num4;
		}
		return 0.0;
	}

	private static bool IsForeground(Color pixel, Color background)
	{
		return Math.Abs(pixel.R - background.R) + Math.Abs(pixel.G - background.G) + Math.Abs(pixel.B - background.B) > 0.05f;
	}

	private static string DescribeAnimationTree(Node root)
	{
		List<string> list = new List<string>();
		AppendAnimationTree(root, list);
		return string.Join(";", list);
	}

	private static void AppendAnimationTree(Node node, List<string> parts)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite)
		{
			parts.Add($"{adobeAnimateSprite.Name}:behind={adobeAnimateSprite.ShowBehindParent}:z={adobeAnimateSprite.ZIndex}");
		}
		foreach (Node child in node.GetChildren())
		{
			AppendAnimationTree(child, parts);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PotatoStarPlacementPreviewRuntimeTest] " + message);
		}
	}
}
