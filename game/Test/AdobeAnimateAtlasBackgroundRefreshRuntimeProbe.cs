using System;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateAtlasBackgroundRefreshRuntimeProbe : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_ATLAS_BACKGROUND_REFRESH_RESULT";

	public override async void _Ready()
	{
		bool projectVisual = false;
		bool bootstrapVisual = false;
		bool projectPose = false;
		bool bootstrapPose = false;
		int visualArrayLayers = 0;
		string failure = string.Empty;
		try
		{
			await Task.Run(delegate
			{
				projectVisual = AdobeAnimateGlobalAtlasCache.RefreshProjectAtlas("res://Asset/Anime", bakePoseTextures: false);
				visualArrayLayers = (projectVisual ? AdobeAnimateGlobalAtlasCache.RefreshSharedVisualTextureArray() : 0);
				bootstrapVisual = projectVisual && visualArrayLayers > 0 && AdobeAnimateGlobalAtlasCache.RefreshBootstrapAtlas(bakePoseTextures: false);
			});
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (projectVisual && visualArrayLayers > 0 && bootstrapVisual)
			{
				await Task.Run(delegate
				{
					projectPose = AdobeAnimateGlobalAtlasCache.RefreshProjectPoseData();
					bootstrapPose = projectPose && AdobeAnimateGlobalAtlasCache.RefreshBootstrapPoseData();
				});
			}
		}
		catch (Exception ex)
		{
			failure = ex.ToString();
		}
		bool flag = projectVisual && visualArrayLayers > 0 && bootstrapVisual && projectPose && bootstrapPose && string.IsNullOrEmpty(failure);
		GD.Print($"{"ADOBE_ANIMATE_ATLAS_BACKGROUND_REFRESH_RESULT"} passed={flag} projectVisual={projectVisual} visualArrayLayers={visualArrayLayers} bootstrapVisual={bootstrapVisual} projectPose={projectPose} bootstrapPose={bootstrapPose}");
		if (!string.IsNullOrEmpty(failure))
		{
			GD.PushError("ADOBE_ANIMATE_ATLAS_BACKGROUND_REFRESH_RESULT failure:\n" + failure);
		}
		GetTree().Quit((!flag) ? 1 : 0);
	}
}
