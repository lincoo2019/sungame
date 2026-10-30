using System;
using System.Threading.Tasks;
using Godot;

public partial class ShopNpcReentryRuntimeProbe : Node
{
	private const string ShopScenePath = "res://Prefab/GUI/DialogBox/Shop/Shop.tscn";

	private int _checks;

	private int _failures;

	private CanvasLayer _dialogLayer;

	public override async void _Ready()
	{
		try
		{
			await RunProbe();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[ShopNpcReentryRuntimeProbe] Unexpected exception: {value}");
		}
		bool flag = _failures == 0;
		GD.Print($"SHOP_NPC_REENTRY_RUNTIME_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private async Task RunProbe()
	{
		PackedScene scene = GD.Load<PackedScene>("res://Prefab/GUI/DialogBox/Shop/Shop.tscn");
		Check(GodotObject.IsInstanceValid(scene), "The shop scene must load.");
		if (GodotObject.IsInstanceValid(scene))
		{
			_dialogLayer = new CanvasLayer
			{
				Name = "DialogLayer",
				Layer = 100
			};
			AddChild(_dialogLayer, forceReadableName: false, InternalMode.Disabled);
			Shop firstShop = CreatePreviewShop(scene, "FirstShop");
			AdobeAnimateSprite firstSprite = GetNpcSprite(firstShop);
			await WaitProcessFrames(30);
			Check(firstSprite.IsVisibleInTree(), "The NPC must be visible on the first shop entry.");
			Check(firstSprite.IsRuntimeActive, "The NPC animation must be active on the first shop entry.");
			int firstEntryPixels = await MeasureNpcPixels(firstShop);
			Check(firstEntryPixels > 200, $"The first shop entry must render the NPC, got {firstEntryPixels} changed pixels.");
			firstShop.Hide();
			await WaitProcessFrames(3);
			Check(!firstSprite.IsVisibleInTree(), "The NPC must inherit the hidden shop state while away from the shop.");
			firstShop.Show();
			await WaitProcessFrames(8);
			Check(firstSprite.IsVisibleInTree(), "The NPC must become visible when returning to the same shop instance.");
			Check(firstSprite.IsRuntimeActive, "The NPC animation must resume when returning to the same shop instance.");
			int resumedEntryPixels = await MeasureNpcPixels(firstShop);
			Check(resumedEntryPixels > 200, $"Returning to the same shop must render the NPC, got {resumedEntryPixels} changed pixels.");
			firstShop.QueueFree();
			await WaitProcessFrames(3);
			Shop secondShop = CreatePreviewShop(scene, "SecondShop");
			AdobeAnimateSprite secondSprite = GetNpcSprite(secondShop);
			await WaitProcessFrames(30);
			Check(secondSprite.IsVisibleInTree(), "The NPC must be visible on the second shop instance.");
			Check(secondSprite.IsRuntimeActive, "The NPC animation must be active on the second shop instance.");
			int num = await MeasureNpcPixels(secondShop);
			Check(num > 200, $"The second shop entry must render the NPC, got {num} changed pixels.");
			GD.Print($"SHOP_NPC_REENTRY_PIXELS first={firstEntryPixels} resumed={resumedEntryPixels} second={num}");
			secondShop.QueueFree();
			await WaitProcessFrames(2);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			_dialogLayer.QueueFree();
			scene.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}

	private Shop CreatePreviewShop(PackedScene scene, string name)
	{
		Shop shop = scene.Instantiate<Shop>(PackedScene.GenEditState.Disabled);
		shop.Name = name;
		shop.editorPreviewMode = true;
		_dialogLayer.AddChild(shop, forceReadableName: false, InternalMode.Disabled);
		return shop;
	}

	private static AdobeAnimateSprite GetNpcSprite(Shop shop)
	{
		return shop.GetNode<NpcBase>("NpcNode/NpcWeiWeiMi").sprite;
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<int> MeasureNpcPixels(Shop shop)
	{
		NpcBase npc = shop.GetNode<NpcBase>("NpcNode/NpcWeiWeiMi");
		Image visible = GetViewport().GetTexture().GetImage();
		npc.Hide();
		await WaitProcessFrames(3);
		Image hidden = GetViewport().GetTexture().GetImage();
		npc.Show();
		await WaitProcessFrames(3);
		try
		{
			int num = 0;
			int num2 = Math.Min(visible.GetWidth(), hidden.GetWidth());
			int num3 = Math.Min(visible.GetHeight(), hidden.GetHeight());
			for (int i = 0; i < num3; i++)
			{
				for (int j = 0; j < num2; j++)
				{
					Color pixel = visible.GetPixel(j, i);
					Color pixel2 = hidden.GetPixel(j, i);
					if (Math.Abs(pixel.R - pixel2.R) + Math.Abs(pixel.G - pixel2.G) + Math.Abs(pixel.B - pixel2.B) + Math.Abs(pixel.A - pixel2.A) > 0.08f)
					{
						num++;
					}
				}
			}
			return num;
		}
		finally
		{
			visible.Dispose();
			hidden.Dispose();
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[ShopNpcReentryRuntimeProbe] " + message);
		}
	}
}
