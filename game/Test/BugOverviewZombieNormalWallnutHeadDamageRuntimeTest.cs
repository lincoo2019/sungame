using System;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewZombieNormalWallnutHeadDamageRuntimeTest : Node
{
	private const string ResultMarker = "ZOMBIE_NORMAL_WALLNUT_HEAD_DAMAGE_RESULT";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Wallnut/TowerDefenseZombieNormalWallnut.tscn";

	private const string WallnutBodyMedia = "Wallnut_body.png";

	private const string WallnutCracked1 = "uid://dpnwmtm6ypomi";

	private const string WallnutCracked2 = "uid://dq2ayfqxj5mfx";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		bool previousUseBatch = TowerDefenseZombie.UseBatch;
		AdobeAnimateRenderBackend previousBackend = Global.Instance?.adobeAnimateRenderBackend ?? AdobeAnimateRenderBackend.GpuCrowd;
		ZombieNormalWallnutHeadDamageRuntimeControlStub control = null;
		TowerDefenseZombieNormalWallnut wallnut = null;
		string capturePath = string.Empty;
		string damage1Path = string.Empty;
		string damage2Path = string.Empty;
		int damage1PixelDiff = 0;
		int damage2PixelDiff = 0;
		try
		{
			_ = 6;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available.");
				Check(Global.Instance != null, "Global autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ObjectManager.Instance) || Global.Instance == null)
				{
					throw new InvalidOperationException("Required autoloads are not available.");
				}
				TowerDefenseZombie.UseBatch = false;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				control = new ZombieNormalWallnutHeadDamageRuntimeControlStub
				{
					Name = "ZombieNormalWallnutHeadDamageRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				wallnut = Instantiate<TowerDefenseZombieNormalWallnut>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Wallnut/TowerDefenseZombieNormalWallnut.tscn");
				Check(GodotObject.IsInstanceValid(wallnut), "The real ZombieNormalWallnut scene must instantiate.");
				if (!GodotObject.IsInstanceValid(wallnut))
				{
					throw new InvalidOperationException("The real ZombieNormalWallnut scene did not instantiate.");
				}
				wallnut.editorPreviewMode = false;
				wallnut.inGame = true;
				wallnut.gridPos = new Vector2I(5, 3);
				wallnut.GlobalPosition = new Vector2(520f, 300f);
				control.characterNode.AddChild(wallnut, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(12);
				ZombieNormalWallnutSprite zombieNormalWallnutSprite = wallnut.sprite as ZombieNormalWallnutSprite;
				AdobeAnimateSpriteBase head = zombieNormalWallnutSprite?.head;
				Check(wallnut.HasValidRuntimeConfiguration && GodotObject.IsInstanceValid(zombieNormalWallnutSprite) && GodotObject.IsInstanceValid(head), "The live wallnut zombie must expose its nested Head sprite.");
				Check(GodotObject.IsInstanceValid(head) && head.IsRuntimeReadyCached, "The nested Head sprite must be runtime-ready before damage is applied.");
				if (!GodotObject.IsInstanceValid(zombieNormalWallnutSprite) || !GodotObject.IsInstanceValid(head))
				{
					throw new InvalidOperationException("The live wallnut zombie did not expose a runtime-ready nested Head sprite.");
				}
				zombieNormalWallnutSprite.SetAnimation("Idle1");
				zombieNormalWallnutSprite.pause = true;
				head.pause = true;
				await WaitFrames(6);
				Image pristine = await CaptureViewportImage();
				string atlasReplacePath = head.GetAtlasReplacePath("Wallnut_body.png");
				Check(string.IsNullOrEmpty(atlasReplacePath), "The pristine Head should not start with a replacement path, actual=" + atlasReplacePath + ".");
				wallnut.instance.SkipInvincibleDealHurt(500.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
				await WaitFrames(8);
				Image cracked1 = await CaptureViewportImage();
				damage1Path = head.GetAtlasReplacePath("Wallnut_body.png");
				damage1PixelDiff = CountDifferentPixels(pristine, cracked1);
				Check(wallnut.instance.damagePointIndex >= 2, $"Damage should advance through Damage1, index={wallnut.instance.damagePointIndex}.");
				Check(damage1Path == "uid://dpnwmtm6ypomi", "Damage1 must replace the Head with the first cracked wallnut texture, actual=" + damage1Path + ".");
				Check(damage1PixelDiff > 24, $"Damage1 must visibly alter rendered pixels, diff={damage1PixelDiff}.");
				wallnut.instance.SkipInvincibleDealHurt(450.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
				await WaitFrames(8);
				Image image = await CaptureViewportImage();
				damage2Path = head.GetAtlasReplacePath("Wallnut_body.png");
				damage2PixelDiff = CountDifferentPixels(cracked1, image);
				capturePath = ProjectSettings.GlobalizePath("user://ZombieNormalWallnutHeadDamageRuntime.png");
				Error error = image.SavePng(capturePath);
				Check(wallnut.instance.damagePointIndex >= 3, $"Damage should advance through Damage2, index={wallnut.instance.damagePointIndex}.");
				Check(damage2Path == "uid://dq2ayfqxj5mfx", "Damage2 must replace the Head with the second cracked wallnut texture, actual=" + damage2Path + ".");
				Check(damage2PixelDiff > 24, $"Damage2 must visibly alter rendered pixels, diff={damage2PixelDiff}.");
				Check(error == Error.Ok, $"The runtime capture should save successfully, error={error}.");
				pristine.Dispose();
				cracked1.Dispose();
				image.Dispose();
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[{"BugOverviewZombieNormalWallnutHeadDamageRuntimeTest"}] Unexpected exception: {value}");
			}
		}
		finally
		{
			TowerDefenseZombie.UseBatch = previousUseBatch;
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = previousBackend;
			}
			if (GodotObject.IsInstanceValid(wallnut))
			{
				wallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
		}
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"{"ZOMBIE_NORMAL_WALLNUT_HEAD_DAMAGE_RESULT"} passed={flag} checks={_checks} failures={_failures} renderer={RenderingServer.GetCurrentRenderingMethod()} damage1Path={damage1Path} damage1Pixels={damage1PixelDiff} damage2Path={damage2Path} damage2Pixels={damage2PixelDiff} capture={capturePath}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static T Instantiate<T>(string scenePath) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private async Task WaitFrames(int frameCount)
	{
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<Image> CaptureViewportImage()
	{
		await WaitFrames(2);
		return GetViewport().GetTexture().GetImage();
	}

	private static int CountDifferentPixels(Image before, Image after)
	{
		if (before == null || after == null || before.GetWidth() != after.GetWidth() || before.GetHeight() != after.GetHeight())
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < before.GetHeight(); i++)
		{
			for (int j = 0; j < before.GetWidth(); j++)
			{
				Color pixel = before.GetPixel(j, i);
				Color pixel2 = after.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - pixel2.R) + Mathf.Abs(pixel.G - pixel2.G) + Mathf.Abs(pixel.B - pixel2.B) + Mathf.Abs(pixel.A - pixel2.A) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewZombieNormalWallnutHeadDamageRuntimeTest] " + message);
		}
	}
}
