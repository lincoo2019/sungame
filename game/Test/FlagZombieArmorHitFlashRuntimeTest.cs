using System;
using System.Threading.Tasks;
using Godot;

public partial class FlagZombieArmorHitFlashRuntimeTest : Node2D
{
	private const string ResultMarker = "FLAG_ZOMBIE_ARMOR_HIT_FLASH_RESULT";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Flag/TowerDefenseZombieFlag.tscn";

	private const string ConePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Flag/ZombieFlagCone.tres";

	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.02f);

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	private int _originalMaxFps;

	private TowerDefenseZombieFlag _target;

	public override async void _Ready()
	{
		int exitCode = 2;
		Resource previousScene = null;
		bool hadPreviousScene = false;
		bool registeredScene = false;
		try
		{
			Require(GodotObject.IsInstanceValid(Global.Instance), "Global autoload is unavailable.");
			Require(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload is unavailable.");
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
			_originalMaxFps = Engine.MaxFps;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			AdobeAnimateRenderManager.RasterCompositeEnabled = false;
			Engine.MaxFps = Math.Max(120, Engine.PhysicsTicksPerSecond * 2);
			ColorRect node = new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(1080f, 600f),
				MouseFilter = Control.MouseFilterEnum.Ignore,
				ZIndex = -4096
			};
			AddChild(node, forceReadableName: false, InternalMode.Disabled);
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Flag/TowerDefenseZombieFlag.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Flag/ZombieFlagCone.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
			Require(GodotObject.IsInstanceValid(packedScene), "Flag-zombie scene could not be loaded.");
			Require(GodotObject.IsInstanceValid(towerDefensePacketConfig), "Flag-zombie cone packet could not be loaded.");
			Require(towerDefensePacketConfig.saveKey == "ZombieFlagCone" && towerDefensePacketConfig.characterConfig?.name == "ZombieFlag" && towerDefensePacketConfig.initArmor.Count == 1 && towerDefensePacketConfig.initArmor[0] == "Cone", "The fixture must use the authored flag-zombie cone packet.");
			hadPreviousScene = ResourceManager.Instance.TOWERDEFENSE_CHARCATERS.TryGetValue("ZombieFlag", out previousScene);
			ResourceManager.Instance.TOWERDEFENSE_CHARCATERS["ZombieFlag"] = packedScene;
			registeredScene = true;
			_target = towerDefensePacketConfig.Create(new Vector2(540f, 330f), new Vector2I(-1, 1)) as TowerDefenseZombieFlag;
			Require(GodotObject.IsInstanceValid(_target), "Authored cone packet did not create a flag zombie.");
			_target.inGame = false;
			_target.editorPreviewMode = true;
			_target.Visible = true;
			_target.Scale = Vector2.One * 1.5f;
			AddChild(_target, forceReadableName: false, InternalMode.Disabled);
			await WaitProcessFrames(24);
			AdobeAnimateSprite flag = _target.GetNodeOrNull<AdobeAnimateSprite>("%ZombieFlagpole");
			TowerDefenseArmorInstance cone = FindArmor(_target, "Cone");
			Require(GodotObject.IsInstanceValid(_target.sprite) && GodotObject.IsInstanceValid(flag), "Flag-zombie body or flag animation is unavailable.");
			Require(GodotObject.IsInstanceValid(cone) && cone.hitPoints > 0.0, "Authored cone armor was not initialized.");
			_target.sprite.SetAnimation("Walk1");
			_target.sprite.SetVerticalClip(enabled: false, -10000f, 10000f);
			_target.sprite.timeScale = 0.0;
			flag.timeScale = 0.0;
			await WaitProcessFrames(4);
			flag.Visible = false;
			await WaitProcessFrames(2);
			using Image withoutFlag = await CaptureFrame();
			flag.Visible = true;
			await WaitProcessFrames(2);
			using Image baseline = await CaptureFrame();
			double bodyHitpointsBefore = _target.instance.hitpoints;
			double armorHitpointsBefore = cone.hitPoints;
			bool rootGpuEligible = _target.sprite.CanUseGpuHitFlashEnvelope();
			bool flagGpuEligible = flag.CanUseGpuHitFlashEnvelope();
			Require(rootGpuEligible && flagGpuEligible, $"Body and flag must both accept one atomic GPU hit-flash envelope; root={rootGpuEligible} flag={flagGpuEligible}.");
			_target.Hurt(20.0, playSplatAudio: false, Vector2.Zero, createDamagePart: false);
			Require(cone.hitPoints < armorHitpointsBefore, $"Cone armor did not absorb the production hit; before={armorHitpointsBefore:F2} after={cone.hitPoints:F2}.");
			Require(Mathf.IsEqualApprox(_target.instance.hitpoints, bodyHitpointsBefore), $"Armor-only hit changed body health; before={bodyHitpointsBefore:F2} after={_target.instance.hitpoints:F2}.");
			await WaitProcessFrames(1);
			Require(_target.hitFlashComponent?.IsVisualFlashActive ?? false, "Armor hit did not start the production white-flash feedback.");
			AdobeAnimateCrowdRenderState state;
			AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = _target.sprite.TryBuildCrowdRenderState(out state);
			Require(adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted && state != null && state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph, $"Flag-zombie root did not submit through the GPU graph; result={adobeAnimateCrowdRenderStateResult} mode={state?.Mode}.");
			AdobeAnimateGpuHitFlashState rootWhite = state.GpuGraphRootOwnerState?.GpuWhiteFlash ?? default(AdobeAnimateGpuHitFlashState);
			Require(flag.TryBuildGpuGraphNestedOwnerStateForRender(state.RenderMountParent, enableGpuClock: true, (float)AdobeAnimateRuntimeManager.AnimationClockSeconds, out var flagState), "Flag nested GPU owner state could not be built.");
			Require(rootWhite.Enabled && flagState.GpuWhiteFlash.Enabled, $"Body and flag GPU owners did not both receive white flash; root={rootWhite.Enabled} flag={flagState.GpuWhiteFlash.Enabled}.");
			using Image hitFrame = await CaptureFrame();
			Vector4 vector = MeasureHitFlashPixels(baseline, withoutFlag, hitFrame);
			GD.Print($"{"FLAG_ZOMBIE_ARMOR_HIT_FLASH_RESULT"} stage=diagnostic rootGpuEligible={rootGpuEligible} flagGpuEligible={flagGpuEligible} gpuEnvelope={_target.hitFlashComponent.IsGpuFlashEnvelopeActive} currentWhite={_target.hitFlashComponent.CurrentWhiteStrength:F4} rootModulate={_target.sprite.Modulate} flagModulate={flag.Modulate} rootWhite={rootWhite.Enabled}/{rootWhite.Strength:F4} flagWhite={flagState.GpuWhiteFlash.Enabled}/{flagState.GpuWhiteFlash.Strength:F4} flagPixels={vector.X:F0} flagLuminanceRise={vector.Y:F4} bodyPixels={vector.Z:F0} bodyLuminanceRise={vector.W:F4}");
			Require(vector.X >= 120f, $"Flag-only pixel mask was too small; pixels={vector.X:F0}.");
			Require(vector.Y >= 0.07f, $"Flag pixels did not visibly flash; luminanceRise={vector.Y:F4}.");
			Require(vector.Z >= 300f, $"Body pixel mask was too small; pixels={vector.Z:F0}.");
			Require(vector.W >= 0.07f, $"Body pixels did not visibly flash; luminanceRise={vector.W:F4}.");
			GD.Print($"{"FLAG_ZOMBIE_ARMOR_HIT_FLASH_RESULT"} passed=True armor=Cone armorBefore={armorHitpointsBefore:F2} armorAfter={cone.hitPoints:F2} bodyBefore={bodyHitpointsBefore:F2} bodyAfter={_target.instance.hitpoints:F2} rootGpuEligible={rootGpuEligible} flagGpuEligible={flagGpuEligible} rootWhite={rootWhite.Enabled} flagWhite={flagState.GpuWhiteFlash.Enabled} flagPixels={vector.X:F0} flagLuminanceRise={vector.Y:F4} bodyPixels={vector.Z:F0} bodyLuminanceRise={vector.W:F4} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = 0;
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"FLAG_ZOMBIE_ARMOR_HIT_FLASH_RESULT"} passed=False exception={value}");
		}
		finally
		{
			if (registeredScene && GodotObject.IsInstanceValid(ResourceManager.Instance))
			{
				if (hadPreviousScene)
				{
					ResourceManager.Instance.TOWERDEFENSE_CHARCATERS["ZombieFlag"] = previousScene;
				}
				else
				{
					ResourceManager.Instance.TOWERDEFENSE_CHARCATERS.Remove("ZombieFlag");
				}
			}
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
			Engine.MaxFps = _originalMaxFps;
		}
		GetTree().Quit(exitCode);
	}

	private static TowerDefenseArmorInstance FindArmor(TowerDefenseCharacter character, string armorName)
	{
		if (character?.instance?.armorList == null)
		{
			return null;
		}
		foreach (TowerDefenseArmorInstance armor in character.instance.armorList)
		{
			if (armor?.slotConfig?.armorName == armorName && !armor.isRemove)
			{
				return armor;
			}
		}
		return null;
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

	private static Vector4 MeasureHitFlashPixels(Image baseline, Image withoutFlag, Image hitFrame)
	{
		int num = 0;
		double num2 = 0.0;
		int num3 = 0;
		double num4 = 0.0;
		int num5 = Math.Min(baseline.GetWidth(), Math.Min(withoutFlag.GetWidth(), hitFrame.GetWidth()));
		int num6 = Math.Min(baseline.GetHeight(), Math.Min(withoutFlag.GetHeight(), hitFrame.GetHeight()));
		for (int i = 0; i < num6; i++)
		{
			for (int j = 0; j < num5; j++)
			{
				Color pixel = baseline.GetPixel(j, i);
				Color pixel2 = withoutFlag.GetPixel(j, i);
				Color pixel3 = hitFrame.GetPixel(j, i);
				if (ColorDistance(pixel2, BackgroundColor) <= 0.05f && ColorDistance(pixel, BackgroundColor) >= 0.12f)
				{
					num++;
					num2 += (double)(Luminance(pixel3) - Luminance(pixel));
				}
				if (ColorDistance(pixel2, BackgroundColor) >= 0.12f)
				{
					num3++;
					num4 += (double)(Luminance(pixel3) - Luminance(pixel));
				}
			}
		}
		float y = ((num > 0) ? ((float)(num2 / (double)num)) : (-3.4028235E+38f));
		float w = ((num3 > 0) ? ((float)(num4 / (double)num3)) : (-3.4028235E+38f));
		return new Vector4(num, y, num3, w);
	}

	private static float Luminance(Color color)
	{
		return (color.R + color.G + color.B) / 3f;
	}

	private static float ColorDistance(Color left, Color right)
	{
		return Math.Max(Math.Abs(left.R - right.R), Math.Max(Math.Abs(left.G - right.G), Math.Abs(left.B - right.B)));
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
