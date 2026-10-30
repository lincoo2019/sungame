using System;
using Godot;

public partial class AdobeAnimateStartupAtlasRuntimeProbe : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_STARTUP_ATLAS_RESULT";

	private const string SproutScenePath = "res://Asset/Anime/Effect/LoadBar/LoadBarSprout.tscn";

	private const string ZombieHeadScenePath = "res://Asset/Anime/Effect/LoadBar/LoadBarZombieHead.tscn";

	public override async void _Ready()
	{
		bool passed = false;
		string failure = string.Empty;
		try
		{
			AdobeAnimateAtlasProfile profile = ResourceLoader.Load<AdobeAnimateAtlasProfile>("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasProfile.tres", null, ResourceLoader.CacheMode.Reuse);
			if (profile == null || !profile.StartupOnly)
			{
				throw new InvalidOperationException("Bootstrap atlas profile is unavailable or not startup-only.");
			}
			TextureLayered bootstrapVisual = AdobeAnimateGlobalAtlasCache.PreloadVisualTextureArray(profile);
			TextureLayered bootstrapPose = AdobeAnimateGlobalAtlasCache.PreloadGpuPoseTextureArray(profile);
			if (!IsValid(bootstrapVisual) || !IsValid(bootstrapPose))
			{
				throw new InvalidOperationException("Bootstrap Visual/Pose arrays are invalid.");
			}
			AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = profile.LoadManifest();
			if (adobeAnimateGlobalAtlasManifest == null)
			{
				throw new InvalidOperationException("Bootstrap manifest is unavailable.");
			}
			if ((float)bootstrapVisual.GetWidth() != adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerSize.X || (float)bootstrapVisual.GetHeight() != adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerSize.Y || bootstrapVisual.GetLayers() < adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerCount)
			{
				throw new InvalidOperationException($"Bootstrap visual array dimensions do not match its manifest: actual={bootstrapVisual.GetWidth()}x{bootstrapVisual.GetHeight()}x{bootstrapVisual.GetLayers()}, expected={adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerSize.X}x{adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerSize.Y}x{adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerCount}.");
			}
			ValidatePoseArray("bootstrap", bootstrapPose, adobeAnimateGlobalAtlasManifest);
			AdobeAnimateSpriteBase sprout = InstantiateAnimation("res://Asset/Anime/Effect/LoadBar/LoadBarSprout.tscn", profile);
			AdobeAnimateSpriteBase zombieHead = InstantiateAnimation("res://Asset/Anime/Effect/LoadBar/LoadBarZombieHead.tscn", profile);
			AddChild(sprout, forceReadableName: false, InternalMode.Disabled);
			AddChild(zombieHead, forceReadableName: false, InternalMode.Disabled);
			sprout.SetAnimation("Idle", loop: false);
			zombieHead.SetAnimation("Idle", loop: false);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (!sprout.TryGetGpuGraphOwnerDefinitionForRender(out var definition) || !zombieHead.TryGetGpuGraphOwnerDefinitionForRender(out var definition2))
			{
				throw new InvalidOperationException("Bootstrap AdobeAnimate definitions were not renderable.");
			}
			if (definition.AtlasProfile != profile || definition2.AtlasProfile != profile)
			{
				throw new InvalidOperationException("Loading animations did not retain the bootstrap profile.");
			}
			if (definition.AtlasTextureArrayRid != definition2.AtlasTextureArrayRid || definition.GpuPoseTextureRid != definition2.GpuPoseTextureRid || definition.AtlasTextureArrayRid != bootstrapVisual.GetRid() || definition.GpuPoseTextureRid != bootstrapPose.GetRid())
			{
				throw new InvalidOperationException("Bootstrap animations do not share one Visual/Pose array RID.");
			}
			ResourceManager resourceManager = ResourceManager.Instance;
			if (!GodotObject.IsInstanceValid(resourceManager))
			{
				throw new InvalidOperationException("ResourceManager autoload is unavailable.");
			}
			resourceManager.BeginGameplayAtlasPreload();
			for (int frame = 0; frame < 3600; frame++)
			{
				if (resourceManager.AreGameplayAtlasesReady)
				{
					break;
				}
				if (resourceManager.HasGameplayAtlasLoadFailed)
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			if (!resourceManager.AreGameplayAtlasesReady)
			{
				throw new InvalidOperationException($"Gameplay arrays did not become ready: {resourceManager.CurrentGameplayAtlasLoadState}, " + resourceManager.GameplayAtlasLoadError);
			}
			TextureLayered textureLayered = AdobeAnimateGlobalAtlasCache.PreloadVisualTextureArray();
			TextureLayered textureLayered2 = AdobeAnimateGlobalAtlasCache.PreloadGpuPoseTextureArray();
			AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest2 = ResourceLoader.Load<AdobeAnimateGlobalAtlasManifest>("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres", null, ResourceLoader.CacheMode.Reuse);
			if (!IsValid(textureLayered) || !IsValid(textureLayered2) || adobeAnimateGlobalAtlasManifest2 == null)
			{
				throw new InvalidOperationException("Gameplay Visual/Pose arrays are invalid.");
			}
			ValidatePoseArray("gameplay", textureLayered2, adobeAnimateGlobalAtlasManifest2);
			if (textureLayered.GetLayers() > 20 || adobeAnimateGlobalAtlasManifest2.AtlasTextureArrayLayerCount > 20)
			{
				throw new InvalidOperationException($"Gameplay visual atlas did not meet the 20-layer budget: imported={textureLayered.GetLayers()}, manifest={adobeAnimateGlobalAtlasManifest2.AtlasTextureArrayLayerCount}.");
			}
			if (textureLayered.GetRid() == bootstrapVisual.GetRid() || textureLayered2.GetRid() == bootstrapPose.GetRid())
			{
				throw new InvalidOperationException("Bootstrap and gameplay arrays must be isolated resources.");
			}
			passed = true;
			GD.Print($"[StartupAtlasProbe] bootstrapVisual={bootstrapVisual.GetWidth()}x{bootstrapVisual.GetHeight()}x{bootstrapVisual.GetLayers()} bootstrapPose={bootstrapPose.GetWidth()}x{bootstrapPose.GetHeight()}x{bootstrapPose.GetLayers()} gameplayVisual={textureLayered.GetWidth()}x{textureLayered.GetHeight()}x{textureLayered.GetLayers()} gameplayPose={textureLayered2.GetWidth()}x{textureLayered2.GetHeight()}x{textureLayered2.GetLayers()}");
		}
		catch (Exception ex)
		{
			failure = ex.ToString();
			GD.PushError(failure);
		}
		finally
		{
			GD.Print($"{"ADOBE_ANIMATE_STARTUP_ATLAS_RESULT"} passed={passed} failure={failure}");
			GetTree().Quit((!passed) ? 1 : 0);
		}
	}

	private static AdobeAnimateSpriteBase InstantiateAnimation(string scenePath, AdobeAnimateAtlasProfile profile)
	{
		AdobeAnimateSpriteBase obj = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Reuse)?.Instantiate<AdobeAnimateSpriteBase>(PackedScene.GenEditState.Disabled) ?? throw new InvalidOperationException("Unable to instantiate loading animation: " + scenePath);
		obj.atlasProfileOverride = profile;
		return obj;
	}

	private static void ValidatePoseArray(string label, TextureLayered textureArray, AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (textureArray.GetWidth() != manifest.PoseTextureArrayLayerSize.X || textureArray.GetHeight() != manifest.PoseTextureArrayLayerSize.Y || textureArray.GetLayers() < manifest.PoseTextureArrayLayerCount)
		{
			throw new InvalidOperationException(label + " pose array dimensions do not match its manifest.");
		}
		Image image = textureArray.GetLayerData(0);
		if (image == null && !string.IsNullOrEmpty(manifest.PoseTextureArrayPath))
		{
			image = Image.LoadFromFile(ProjectSettings.GlobalizePath(manifest.PoseTextureArrayPath));
		}
		try
		{
			if (image == null || image.GetFormat() != Image.Format.Rgbah)
			{
				throw new InvalidOperationException(label + " pose layer format is " + (image?.GetFormat().ToString() ?? "unavailable") + ", expected RGBA16F.");
			}
		}
		finally
		{
			image?.Dispose();
		}
	}

	private static bool IsValid(TextureLayered texture)
	{
		if (GodotObject.IsInstanceValid(texture))
		{
			return texture.GetRid().IsValid;
		}
		return false;
	}
}
