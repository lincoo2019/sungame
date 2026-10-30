using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class FirenutPlacementPreviewVisibilityRuntimeTest : Node
{
	private const string ResultMarker = "FIRENUT_PLACEMENT_PREVIEW_VISIBILITY_RESULT";

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f);

	private static readonly Color PreviewOccluderColor = new Color(0.9f, 0.05f, 0.75f);

	private static readonly Rect2I FollowPreviewRegion = new Rect2I(735, 120, 150, 130);

	private static readonly Rect2I PlacementPreviewRegion = new Rect2I(735, 300, 150, 130);

	private static readonly Rect2I[] BattlefieldRegions = new Rect2I[8]
	{
		new Rect2I(0, 80, 110, 130),
		new Rect2I(110, 80, 110, 130),
		new Rect2I(220, 80, 110, 130),
		new Rect2I(330, 80, 110, 130),
		new Rect2I(0, 300, 110, 130),
		new Rect2I(110, 300, 110, 130),
		new Rect2I(220, 300, 110, 130),
		new Rect2I(330, 300, 110, 130)
	};

	private const int ContinuousFrameCount = 12;

	private AdobeAnimateRenderBackend _originalBackend;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<TowerDefensePlantFirenut> battleCharacters = new List<TowerDefensePlantFirenut>();
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		TowerDefenseControlNew battleControl = null;
		TowerDefenseMapControl mapControl = null;
		PacketPickControl packetPickControl = null;
		TowerDefenseInGamePacketShow packetShow = null;
		Resource previousFirenutSprite = null;
		bool hadFirenutSprite = false;
		Resource previousSunflowerPeaSprite = null;
		bool hadSunflowerPeaSprite = false;
		long synchronousTransactions = 0L;
		try
		{
			if (Global.Instance == null || manager == null || ResourceManager.Instance == null)
			{
				throw new InvalidOperationException("Global, TowerDefenseManager, or ResourceManager autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			ColorRect node = new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(900f, 540f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			AddChild(node, forceReadableName: false, InternalMode.Disabled);
			CanvasLayer canvasLayer = new CanvasLayer
			{
				Layer = 1,
				FollowViewportEnabled = true
			};
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			Node2D characterHost = new Node2D();
			canvasLayer.AddChild(characterHost, forceReadableName: false, InternalMode.Disabled);
			CanvasLayer canvasLayer2 = new CanvasLayer
			{
				Layer = 3,
				FollowViewportEnabled = true
			};
			AddChild(canvasLayer2, forceReadableName: false, InternalMode.Disabled);
			Node2D overlayHost = new Node2D();
			canvasLayer2.AddChild(overlayHost, forceReadableName: false, InternalMode.Disabled);
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Packet/PlantFirenut.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Firenut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantFirenut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			TowerDefensePacketConfig sunflowerPeaPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
			PackedScene packedScene3 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/SunflowerPea.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(packedScene2) || !GodotObject.IsInstanceValid(sunflowerPeaPacket) || !GodotObject.IsInstanceValid(packedScene3))
			{
				throw new InvalidOperationException("Production Firenut or SunflowerPea packet and scene resources are unavailable.");
			}
			hadFirenutSprite = ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(towerDefensePacketConfig.saveKey, out previousFirenutSprite);
			ResourceManager.Instance.CHARCTAER_SPRITE[towerDefensePacketConfig.saveKey] = packedScene;
			hadSunflowerPeaSprite = ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(sunflowerPeaPacket.saveKey, out previousSunflowerPeaSprite);
			ResourceManager.Instance.CHARCTAER_SPRITE[sunflowerPeaPacket.saveKey] = packedScene3;
			battleControl = new TowerDefenseControlNew
			{
				characterNode = characterHost
			};
			TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap();
			battleControl.featureDictionary[new StringName("Map")] = towerDefenseBattleFeatureMap;
			manager.currentControl = battleControl;
			mapControl = new TowerDefenseMapControl
			{
				spriteNode = overlayHost
			};
			packetPickControl = new PacketPickControl();
			AddChild(packetPickControl, forceReadableName: false, InternalMode.Disabled);
			packetPickControl.Init(mapControl, towerDefenseBattleFeatureMap);
			for (int i = 0; i < BattlefieldRegions.Length; i++)
			{
				Rect2I rect2I = BattlefieldRegions[i];
				TowerDefensePlantFirenut towerDefensePlantFirenut = packedScene2.Instantiate<TowerDefensePlantFirenut>(PackedScene.GenEditState.Disabled);
				towerDefensePlantFirenut.inGame = false;
				towerDefensePlantFirenut.skipDestroySet = true;
				towerDefensePlantFirenut.ProcessMode = ProcessModeEnum.Disabled;
				towerDefensePlantFirenut.Position = new Vector2((float)rect2I.Position.X + (float)rect2I.Size.X / 2f, (float)rect2I.Position.Y + (float)rect2I.Size.Y / 2f);
				characterHost.AddChild(towerDefensePlantFirenut, forceReadableName: false, InternalMode.Disabled);
				towerDefensePlantFirenut.sprite.ProcessMode = ProcessModeEnum.Always;
				towerDefensePlantFirenut.sprite.pause = false;
				towerDefensePlantFirenut.sprite.timeScale = 1.0;
				towerDefensePlantFirenut.sprite.SetAnimation("Idle");
				towerDefensePlantFirenut.sprite.RefreshProcessScheduling();
				manager.CharacterRegister(towerDefensePlantFirenut);
				battleCharacters.Add(towerDefensePlantFirenut);
			}
			await WaitFrames(16);
			int[] baselinePixels = await CaptureRegionPixels();
			int baselineSuppressedCharacters = CountSuppressedCharacters(battleCharacters);
			packetShow = new TowerDefenseInGamePacketShow
			{
				config = sunflowerPeaPacket,
				select = true
			};
			packetPickControl.PickPacket(packetShow);
			if (!GodotObject.IsInstanceValid(packetPickControl.followSprite) || packetPickControl.plantSpriteList.Count != 1)
			{
				throw new InvalidOperationException("Production packet pick did not create both SunflowerPea preview roles.");
			}
			AdobeAnimateSprite nodeOrNull = packetPickControl.followSprite.GetNodeOrNull<AdobeAnimateSprite>("Head");
			AdobeAnimateSprite nodeOrNull2 = packetPickControl.plantSpriteList[0].GetNodeOrNull<AdobeAnimateSprite>("Head");
			if (!GodotObject.IsInstanceValid(nodeOrNull) || !GodotObject.IsInstanceValid(nodeOrNull2))
			{
				throw new InvalidOperationException("Production SunflowerPea preview is missing its Head child animation.");
			}
			packetPickControl.followSprite.Position = new Vector2(810f, 185f);
			packetPickControl.plantSpriteList[0].Position = new Vector2(810f, 365f);
			PacketPickControl.SetPlacementPreviewRenderRow(packetPickControl.plantSpriteList[0], 1);
			int firstRowPreviewZIndex = packetPickControl.plantSpriteList[0].ZIndex;
			PacketPickControl.SetPlacementPreviewRenderRow(packetPickControl.plantSpriteList[0], 5);
			int fifthRowPreviewZIndex = packetPickControl.plantSpriteList[0].ZIndex;
			int num = 15;
			bool previewRows = firstRowPreviewZIndex == num * 2 - 1 && fifthRowPreviewZIndex == num * 6 - 1 && firstRowPreviewZIndex > num + 10 && firstRowPreviewZIndex < num * 2;
			Control nodeOrNull3 = characterHost.GetNodeOrNull<Control>("PacketPickPlacementPreviewRenderMount");
			Control nodeOrNull4 = overlayHost.GetNodeOrNull<Control>("PacketPickFollowPreviewRenderMount");
			bool previewCanvasMounts = GodotObject.IsInstanceValid(nodeOrNull3) && GodotObject.IsInstanceValid(nodeOrNull4) && nodeOrNull3.GetParent() == characterHost && nodeOrNull4.GetParent() == overlayHost;
			bool followPreviewTop = packetPickControl.followSprite.ZIndex == 4096;
			bool previewsUseLocalRender = packetPickControl.followSprite.forceLocalRender && packetPickControl.plantSpriteList[0].forceLocalRender;
			bool nestedPreviewsUseLocalRender = nodeOrNull.forceLocalRender && nodeOrNull2.forceLocalRender && nodeOrNull.forceCpuPoseRender && nodeOrNull2.forceCpuPoseRender;
			characterHost.AddChild(CreatePreviewOccluder(PlacementPreviewRegion, fifthRowPreviewZIndex - 1), forceReadableName: false, InternalMode.Disabled);
			overlayHost.AddChild(CreatePreviewOccluder(FollowPreviewRegion, 4095), forceReadableName: false, InternalMode.Disabled);
			List<int[]> pickedFramePixels = new List<int[]>();
			for (int frameIndex = 0; frameIndex < 12; frameIndex++)
			{
				List<int[]> list = pickedFramePixels;
				list.Add(await CaptureRegionPixels());
			}
			int[] array = pickedFramePixels[pickedFramePixels.Count - 1];
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 2147483647;
			int num7 = 0;
			int num8 = 0;
			for (int j = 0; j < baselinePixels.Length; j++)
			{
				num4 += baselinePixels[j];
				num5 += array[j];
				if (baselinePixels[j] > 0 && array[j] >= baselinePixels[j] / 3)
				{
					num2++;
				}
				battleCharacters[j].sprite.GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
				if (nativeCanvasSuppressed)
				{
					num8++;
				}
			}
			for (int k = 0; k < pickedFramePixels.Count; k++)
			{
				int[] array2 = pickedFramePixels[k];
				if (k > 0 && PixelsDiffer(array2, pickedFramePixels[k - 1]))
				{
					num7++;
				}
				int num9 = 0;
				bool flag = true;
				for (int l = 0; l < array2.Length; l++)
				{
					num9 += array2[l];
					if (baselinePixels[l] <= 0 || array2[l] < baselinePixels[l] / 3)
					{
						flag = false;
					}
				}
				num6 = Math.Min(num6, num9);
				if (flag)
				{
					num3++;
				}
			}
			using Image image = GetViewport().GetTexture().GetImage();
			Color pixel = image.GetPixel(PlacementPreviewRegion.Position.X + 2, PlacementPreviewRegion.Position.Y + 2);
			Color pixel2 = image.GetPixel(FollowPreviewRegion.Position.X + 2, FollowPreviewRegion.Position.Y + 2);
			int num10 = CountPixelsDifferentFromColor(image, PlacementPreviewRegion, pixel);
			int num11 = CountPixelsDifferentFromColor(image, FollowPreviewRegion, pixel2);
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			int coverFlag = ShaderEffectComponent.ShaderEffectFlags["cover"];
			int count = battleCharacters.FindAll((TowerDefensePlantFirenut character) => (character.shaderEffectComponent.GetEffectFlags() & coverFlag) != 0).Count;
			bool flag2 = num4 > 0 && num2 == BattlefieldRegions.Length && num5 >= num4 / 3 && num6 >= num4 / 3 && num3 == 12 && num7 > 0 && synchronousTransactions == 0 && previewRows && previewCanvasMounts && followPreviewTop && num10 > 100 && num11 > 100 && previewsUseLocalRender && nestedPreviewsUseLocalRender && baselineSuppressedCharacters == BattlefieldRegions.Length && num8 == BattlefieldRegions.Length && count == 0 && aggregateRenderStats.CrowdRoots == BattlefieldRegions.Length && aggregateRenderStats.FallbackRoots == 0;
			GD.Print($"{"FIRENUT_PLACEMENT_PREVIEW_VISIBILITY_RESULT"} passed={flag2} visibleRegions={num2}/{BattlefieldRegions.Length} continuousFrames={num3}/{12} animatedTransitions={num7} synchronousTransactions={synchronousTransactions} baseline={num4} picked={num5} minimumPicked={num6} baselineRegions={string.Join(',', baselinePixels)} pickedRegions={string.Join(',', array)} previewRows={previewRows} firstRowZ={firstRowPreviewZIndex} fifthRowZ={fifthRowPreviewZIndex} previewCanvasMounts={previewCanvasMounts} followPreviewTop={followPreviewTop} placementPreviewPixels={num10} followPreviewPixels={num11} previewsLocal={previewsUseLocalRender} nestedPreviewsLocal={nestedPreviewsUseLocalRender} crowd={aggregateRenderStats.CrowdRoots} fallback={aggregateRenderStats.FallbackRoots} baselineSuppressed={baselineSuppressedCharacters} suppressed={num8} covered={count} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag2) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"FIRENUT_PLACEMENT_PREVIEW_VISIBILITY_RESULT"} exception={value}");
		}
		finally
		{
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			if (GodotObject.IsInstanceValid(packetPickControl))
			{
				packetPickControl.FreePreviewSprites();
				packetPickControl.QueueFree();
			}
			if (GodotObject.IsInstanceValid(packetShow))
			{
				packetShow.Free();
			}
			for (int num12 = 0; num12 < battleCharacters.Count; num12++)
			{
				TowerDefensePlantFirenut towerDefensePlantFirenut2 = battleCharacters[num12];
				if (GodotObject.IsInstanceValid(towerDefensePlantFirenut2))
				{
					manager?.CharacterUnregister(towerDefensePlantFirenut2);
					towerDefensePlantFirenut2.QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (ResourceManager.Instance != null)
			{
				if (hadFirenutSprite)
				{
					ResourceManager.Instance.CHARCTAER_SPRITE["PlantFirenut"] = previousFirenutSprite;
				}
				else
				{
					ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantFirenut");
				}
				if (hadSunflowerPeaSprite)
				{
					ResourceManager.Instance.CHARCTAER_SPRITE["PlantSunflowerPea"] = previousSunflowerPeaSprite;
				}
				else
				{
					ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantSunflowerPea");
				}
			}
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(battleControl))
			{
				battleControl.featureDictionary.Clear();
				battleControl.Free();
			}
		}
		GetTree().Quit(exitCode);
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<int[]> CaptureRegionPixels()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		int[] array = new int[BattlefieldRegions.Length];
		for (int i = 0; i < BattlefieldRegions.Length; i++)
		{
			array[i] = CountForegroundPixels(image, BattlefieldRegions[i]);
		}
		return array;
	}

	private static int CountForegroundPixels(Image image, Rect2I region)
	{
		int num = 0;
		int num2 = Math.Min(region.End.X, image.GetWidth());
		int num3 = Math.Min(region.End.Y, image.GetHeight());
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
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

	private static ColorRect CreatePreviewOccluder(Rect2I region, int zIndex)
	{
		return new ColorRect
		{
			Color = PreviewOccluderColor,
			Position = region.Position,
			Size = region.Size,
			ZIndex = zIndex,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	private static int CountPixelsDifferentFromColor(Image image, Rect2I region, Color referenceColor)
	{
		int num = 0;
		int num2 = Math.Min(region.End.X, image.GetWidth());
		int num3 = Math.Min(region.End.Y, image.GetHeight());
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (Math.Abs(pixel.R - referenceColor.R) + Math.Abs(pixel.G - referenceColor.G) + Math.Abs(pixel.B - referenceColor.B) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static int CountSuppressedCharacters(List<TowerDefensePlantFirenut> characters)
	{
		int num = 0;
		for (int i = 0; i < characters.Count; i++)
		{
			characters[i].sprite.GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
			if (nativeCanvasSuppressed)
			{
				num++;
			}
		}
		return num;
	}

	private static bool PixelsDiffer(int[] current, int[] previous)
	{
		if (current.Length != previous.Length)
		{
			return true;
		}
		for (int i = 0; i < current.Length; i++)
		{
			if (current[i] != previous[i])
			{
				return true;
			}
		}
		return false;
	}
}
