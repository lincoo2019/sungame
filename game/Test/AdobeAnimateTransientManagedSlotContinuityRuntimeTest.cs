using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateTransientManagedSlotContinuityRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_TRANSIENT_MANAGED_SLOT_CONTINUITY_RESULT";

	private const string NormalZombieAnimationScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn";

	private const string HelmetTexturePath = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/Helmet/ZombieHelmet1.png";

	private const int SpriteCount = 6;

	private const int SampleFrameCount = 8;

	private static readonly Color BackgroundColor = new Color(0.02f, 0.02f, 0.02f);

	private readonly Rect2I[] _regions = new Rect2I[6];

	private AdobeAnimateRenderBackend _originalBackend;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<AdobeAnimateSprite> sprites = new List<AdobeAnimateSprite>(6);
		try
		{
			_ = 3;
			try
			{
				if (!GodotObject.IsInstanceValid(Global.Instance))
				{
					throw new InvalidOperationException("Global autoload is unavailable.");
				}
				_originalBackend = Global.Instance.adobeAnimateRenderBackend;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				AddChild(new ColorRect
				{
					Color = BackgroundColor,
					Position = Vector2.Zero,
					Size = new Vector2(900f, 420f),
					MouseFilter = Control.MouseFilterEnum.Ignore
				}, forceReadableName: false, InternalMode.Disabled);
				Node2D node2D = new Node2D
				{
					Name = "AnimationHost"
				};
				AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
				if (!GodotObject.IsInstanceValid(packedScene))
				{
					throw new InvalidOperationException("The production ordinary Zombie animation scene is unavailable.");
				}
				for (int i = 0; i < 6; i++)
				{
					Vector2 position = new Vector2(85f + (float)i * 140f, 250f);
					AdobeAnimateSprite adobeAnimateSprite = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
					adobeAnimateSprite.Position = position;
					adobeAnimateSprite.ZAsRelative = false;
					adobeAnimateSprite.ZIndex = 200;
					adobeAnimateSprite.forceLocalRender = false;
					node2D.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
					sprites.Add(adobeAnimateSprite);
					_regions[i] = new Rect2I(Mathf.RoundToInt(position.X) - 55, Mathf.RoundToInt(position.Y) - 125, 110, 155);
				}
				await WaitFrames(10);
				AdobeAnimateSprite primarySprite = sprites[0];
				AdobeAnimateSlot nodeOrNull = primarySprite.GetNodeOrNull<AdobeAnimateSlot>("HeadSlot");
				if (!GodotObject.IsInstanceValid(nodeOrNull))
				{
					throw new InvalidOperationException("The production ordinary Zombie HeadSlot is unavailable.");
				}
				AdobeAnimatePart helmet = new AdobeAnimatePart
				{
					Name = "RuntimeHelmet"
				};
				nodeOrNull.AddChild(helmet, forceReadableName: false, InternalMode.Disabled);
				helmet.externalAtlasTexturePath = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/Helmet/ZombieHelmet1.png";
				await WaitFrames(12);
				if (FindManagedAtlasPartIndex(primarySprite, helmet) < 0)
				{
					throw new InvalidOperationException("The production Helmet did not enter the managed Slot queue.");
				}
				using Image baselineImage = await CaptureImage();
				int[] baselinePixels = CountRegions(baselineImage);
				AdobeAnimateCrowdAggregateStats baselineStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				int baselineSuppressed = CountNativeCanvasSuppressed(sprites);
				AdobeAnimateRuntimeManager.RequestRenderRootRepublish();
				int[] minimumPixels = new int[6];
				Array.Fill(minimumPixels, 2147483647);
				int blankFrames = 0;
				int maximumFallbackRoots = 0;
				int minimumCrowdRoots = 2147483647;
				int minimumSuppressed = 2147483647;
				for (int frame = 0; frame < 8; frame++)
				{
					using Image image = await CaptureImage();
					int[] array = CountRegions(image);
					for (int j = 0; j < 6; j++)
					{
						minimumPixels[j] = Math.Min(minimumPixels[j], array[j]);
						if (array[j] < Math.Max(80, Mathf.RoundToInt((float)baselinePixels[j] * 0.7f)))
						{
							blankFrames++;
						}
					}
					AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
					maximumFallbackRoots = Math.Max(maximumFallbackRoots, aggregateRenderStats.FallbackRoots);
					minimumCrowdRoots = Math.Min(minimumCrowdRoots, aggregateRenderStats.CrowdRoots);
					minimumSuppressed = Math.Min(minimumSuppressed, CountNativeCanvasSuppressed(sprites));
				}
				long num = 0L;
				bool flag = Array.TrueForAll(baselinePixels, (int pixels) => pixels >= 80) && baselineStats.CrowdRoots == 6 && baselineStats.FallbackRoots == 0 && baselineSuppressed >= 5 && num == 1 && blankFrames == 0 && maximumFallbackRoots == 0 && minimumCrowdRoots == 6 && minimumSuppressed == baselineSuppressed;
				GD.Print($"{"ADOBE_ANIMATE_TRANSIENT_MANAGED_SLOT_CONTINUITY_RESULT"} passed={flag} baseline={string.Join(',', baselinePixels)} minimum={string.Join(',', minimumPixels)} blankFrames={blankFrames} retained={num} baselineCrowd={baselineStats.CrowdRoots} minimumCrowd={minimumCrowdRoots} maximumFallback={maximumFallbackRoots} baselineSuppressed={baselineSuppressed} minimumSuppressed={minimumSuppressed} samples={8} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag) ? 2 : 0);
			}
			catch (Exception value)
			{
				GD.PrintErr($"{"ADOBE_ANIMATE_TRANSIENT_MANAGED_SLOT_CONTINUITY_RESULT"} exception={value}");
			}
		}
		finally
		{
			for (int num2 = 0; num2 < sprites.Count; num2++)
			{
				if (GodotObject.IsInstanceValid(sprites[num2]))
				{
					sprites[num2].QueueFree();
				}
			}
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			await WaitFrames(4);
		}
		GetTree().Quit(exitCode);
	}

	private static int FindManagedAtlasPartIndex(AdobeAnimateSprite sprite, AdobeAnimatePart atlasPart)
	{
		AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = sprite.GetManagedSlotSpritesForRender();
		for (int i = 0; i < managedSlotSpritesForRender.Length; i++)
		{
			if (managedSlotSpritesForRender[i].AtlasPart == atlasPart)
			{
				return i;
			}
		}
		return -1;
	}

	private static int CountNativeCanvasSuppressed(List<AdobeAnimateSprite> sprites)
	{
		int num = 0;
		for (int i = 0; i < sprites.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = sprites[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
				if (nativeCanvasSuppressed)
				{
					num++;
				}
			}
		}
		return num;
	}

	private async Task WaitFrames(int count)
	{
		for (int index = 0; index < count; index++)
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

	private int[] CountRegions(Image image)
	{
		int[] array = new int[6];
		for (int i = 0; i < 6; i++)
		{
			array[i] = CountForegroundPixels(image, _regions[i]);
		}
		return array;
	}

	private int CountForegroundPixels(Image image, Rect2I logicalRegion)
	{
		Rect2 visibleRect = GetViewport().GetVisibleRect();
		Vector2 vector = new Vector2((float)image.GetWidth() / visibleRect.Size.X, (float)image.GetHeight() / visibleRect.Size.Y);
		int num = Mathf.Clamp(Mathf.FloorToInt((float)logicalRegion.Position.X * vector.X), 0, image.GetWidth());
		int num2 = Mathf.Clamp(Mathf.FloorToInt((float)logicalRegion.Position.Y * vector.Y), 0, image.GetHeight());
		int num3 = Mathf.Clamp(Mathf.CeilToInt((float)logicalRegion.End.X * vector.X), 0, image.GetWidth());
		int num4 = Mathf.Clamp(Mathf.CeilToInt((float)logicalRegion.End.Y * vector.Y), 0, image.GetHeight());
		int num5 = 0;
		for (int i = num2; i < num4; i++)
		{
			for (int j = num; j < num3; j++)
			{
				Color pixel = image.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - BackgroundColor.R) > 0.04f || Mathf.Abs(pixel.G - BackgroundColor.G) > 0.04f || Mathf.Abs(pixel.B - BackgroundColor.B) > 0.04f)
				{
					num5++;
				}
			}
		}
		return num5;
	}
}
