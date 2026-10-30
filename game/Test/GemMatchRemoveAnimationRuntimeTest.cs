using System;
using System.Threading.Tasks;
using Godot;

public sealed partial class GemMatchRemoveAnimationRuntimeTest : Node
{
	private const string ResultMarker = "GEM_MATCH_REMOVE_ANIMATION_RESULT";

	private const string CharacterScenePath = "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantFirenut.tscn";

	private const string CharacterSpriteScenePath = "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Firenut.tscn";

	private const string CharacterPacketPath = "res://Asset/Anime/Character/Plant/Chapter1/Firenut/Packet/PlantFirenut.tres";

	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.02f);

	private AdobeAnimateRenderBackend _previousBackend;

	private int _previousMaxFps;

	public override async void _Ready()
	{
		int exitCode = 2;
		TowerDefensePlantFirenut character = null;
		GemPiece gem = null;
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		TowerDefenseControlNew battleControl = null;
		Resource previousCharacterSprite = null;
		bool hadCharacterSprite = false;
		try
		{
			if (!GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				throw new InvalidOperationException("Global, TowerDefenseManager, or ResourceManager autoload is unavailable.");
			}
			_previousBackend = Global.Instance.adobeAnimateRenderBackend;
			_previousMaxFps = Engine.MaxFps;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			Engine.MaxFps = Math.Max(120, Engine.PhysicsTicksPerSecond * 2);
			ColorRect node = new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(2000f, 1200f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			AddChild(node, forceReadableName: false, InternalMode.Disabled);
			CanvasLayer canvasLayer = new CanvasLayer
			{
				Layer = 1,
				ProcessMode = ProcessModeEnum.Always,
				FollowViewportEnabled = true
			};
			AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
			Node2D node2D = new Node2D();
			canvasLayer.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			battleControl = new TowerDefenseControlNew
			{
				characterNode = node2D
			};
			battleControl.featureDictionary[new StringName("Map")] = new TowerDefenseBattleFeatureMap();
			manager.currentControl = battleControl;
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Packet/PlantFirenut.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Firenut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			PackedScene packedScene2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Chapter1/Firenut/Scene/TowerDefensePlantFirenut.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(packedScene) || !GodotObject.IsInstanceValid(packedScene2))
			{
				throw new InvalidOperationException("Production Firenut packet, animation, or character scene could not be loaded.");
			}
			hadCharacterSprite = ResourceManager.Instance.CHARCTAER_SPRITE.TryGetValue(towerDefensePacketConfig.saveKey, out previousCharacterSprite);
			ResourceManager.Instance.CHARCTAER_SPRITE[towerDefensePacketConfig.saveKey] = packedScene;
			character = packedScene2.Instantiate<TowerDefensePlantFirenut>(PackedScene.GenEditState.Disabled);
			character.Name = "GemMatchRemovingCharacter";
			character.inGame = false;
			character.skipDestroySet = true;
			character.ProcessMode = ProcessModeEnum.Disabled;
			character.Position = new Vector2(320f, 270f);
			node2D.AddChild(character, forceReadableName: false, InternalMode.Disabled);
			if (character.destroyComponent == null || character.destroyComponent.IsReleased || !GodotObject.IsInstanceValid(character.sprite))
			{
				throw new InvalidOperationException("Production Firenut did not initialize its destroy runtime or Adobe animation root.");
			}
			character.sprite.ProcessMode = ProcessModeEnum.Always;
			character.sprite.pause = false;
			character.sprite.timeScale = 0.0;
			character.sprite.SetAnimation("Idle");
			character.sprite.RefreshProcessScheduling();
			manager.CharacterRegister(character);
			character.AddToGroup("Character", persistent: true);
			gem = new GemPiece
			{
				Name = "RemovingGem",
				character = character,
				characterKey = new StringName("PlantFirenut")
			};
			AddChild(gem, forceReadableName: false, InternalMode.Disabled);
			await WaitProcessFrames(20);
			int baselinePixels = await CaptureSignalPixels();
			Vector2 initialScale = character.Scale;
			gem.PlayRemoveAnimation();
			bool gameplayEndedImmediately = character.isDestroy;
			bool groupRemovedImmediately = !character.IsInGroup("Character");
			bool characterRetainedImmediately = GodotObject.IsInstanceValid(character) && !character.IsQueuedForDeletion();
			await WaitProcessFrames(5);
			if (!GodotObject.IsInstanceValid(character))
			{
				throw new InvalidOperationException("GemMatch removed the real character before the shrink animation could render.");
			}
			Vector2 middleScale = character.Scale;
			int middlePixels = await CaptureSignalPixels();
			bool scaleAdvanced = middleScale.Length() < initialScale.Length() - 0.01f && middleScale.Length() > 0.05f;
			bool middleFrameVisible = baselinePixels >= 100 && middlePixels >= Math.Max(40, baselinePixels / 10);
			await WaitProcessFrames(8);
			if (!GodotObject.IsInstanceValid(character))
			{
				throw new InvalidOperationException("GemMatch removed the real character before the late shrink frame could render.");
			}
			Vector2 lateScale = character.Scale;
			int latePixels = await CaptureSignalPixels();
			bool scaleContinued = lateScale.Length() < middleScale.Length() - 0.1f && lateScale.Length() > 0.02f;
			bool lateFrameVisible = latePixels >= 20 && latePixels < middlePixels;
			await WaitProcessFrames(32);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			bool flag = !GodotObject.IsInstanceValid(character);
			bool flag2 = !GodotObject.IsInstanceValid(gem);
			bool flag3 = gameplayEndedImmediately && groupRemovedImmediately && characterRetainedImmediately && scaleAdvanced && middleFrameVisible && scaleContinued && lateFrameVisible && flag && flag2;
			GD.Print($"{"GEM_MATCH_REMOVE_ANIMATION_RESULT"} passed={flag3} gameplayEndedImmediately={gameplayEndedImmediately} groupRemovedImmediately={groupRemovedImmediately} characterRetainedImmediately={characterRetainedImmediately} scaleAdvanced={scaleAdvanced} middleFrameVisible={middleFrameVisible} scaleContinued={scaleContinued} lateFrameVisible={lateFrameVisible} characterReleased={flag} gemReleased={flag2} baselinePixels={baselinePixels} middlePixels={middlePixels} latePixels={latePixels} initialScale={initialScale} middleScale={middleScale} lateScale={lateScale} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag3) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"GEM_MATCH_REMOVE_ANIMATION_RESULT"} passed=False exception={value}");
		}
		finally
		{
			if (GodotObject.IsInstanceValid(character))
			{
				manager?.CharacterUnregister(character);
				character.QueueFree();
			}
			if (GodotObject.IsInstanceValid(gem))
			{
				gem.QueueFree();
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.adobeAnimateRenderBackend = _previousBackend;
			}
			if (GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				if (hadCharacterSprite)
				{
					ResourceManager.Instance.CHARCTAER_SPRITE["PlantFirenut"] = previousCharacterSprite;
				}
				else
				{
					ResourceManager.Instance.CHARCTAER_SPRITE.Remove("PlantFirenut");
				}
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(battleControl))
			{
				battleControl.featureDictionary.Clear();
				battleControl.Free();
			}
			Engine.MaxFps = _previousMaxFps;
		}
		GetTree().Quit(exitCode);
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<int> CaptureSignalPixels()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		int num = 0;
		for (int i = 0; i < image.GetHeight(); i += 2)
		{
			for (int j = 0; j < image.GetWidth(); j += 2)
			{
				Color pixel = image.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - BackgroundColor.R) + Mathf.Abs(pixel.G - BackgroundColor.G) + Mathf.Abs(pixel.B - BackgroundColor.B) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}
}
