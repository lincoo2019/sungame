using System;
using System.Threading.Tasks;
using Godot;

public partial class SceneLoadingStinkyVisibilityRuntimeTest : Node
{
	private const string SceneLoadingPath = "res://Core/SceneManager/SceneLoadeing/SceneLoading.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		PackedScene scene = null;
		SceneLoading loading = null;
		try
		{
			_ = 2;
			try
			{
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
				AdobeAnimateDefinitionCache.Clear();
				scene = ResourceLoader.Load<PackedScene>("res://Core/SceneManager/SceneLoadeing/SceneLoading.tscn", null, ResourceLoader.CacheMode.Reuse);
				Check(GodotObject.IsInstanceValid(scene), "The production scene-loading overlay must load.");
				loading = scene?.Instantiate<SceneLoading>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(loading), "The production scene-loading overlay must instantiate.");
				if (!GodotObject.IsInstanceValid(loading))
				{
					throw new InvalidOperationException("The scene-loading overlay fixture could not be created.");
				}
				AdobeAnimateSpriteBase stinky = loading.GetNodeOrNull<AdobeAnimateSpriteBase>("%Stinky");
				Check(GodotObject.IsInstanceValid(stinky), "The loading overlay must contain the Stinky animation.");
				if (!GodotObject.IsInstanceValid(stinky))
				{
					throw new InvalidOperationException("The loading Stinky animation is unavailable.");
				}
				stinky.flashAnimeData = stinky.flashAnimeData?.Duplicate(deep: true) as AdobeAnimateData;
				AddChild(loading, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(40);
				loading.GetNode<Timer>("Timer").Stop();
				Control nodeOrNull = loading.GetNodeOrNull<Control>("%StinkyRenderMount");
				Check(stinky.forceLocalRender && GodotObject.IsInstanceValid(nodeOrNull), "SceneLoading must route Stinky through its CanvasLayer-local render mount.");
				stinky.SetAnimation("Crawl", loop: false);
				stinky.pause = true;
				stinky.Visible = true;
				stinky.Scale = Vector2.One;
				loading.background.Modulate = Colors.White;
				await WaitFrames(8);
				Check(stinky.IsVisibleInTree(), "The loading Stinky animation must be logically visible.");
				int publishedInstances = CountPublishedInstances(loading);
				Check(publishedInstances > 0, $"The Stinky-local render mount must publish animation instances; count={publishedInstances}.");
				using Image visibleImage = GetViewport().GetTexture().GetImage();
				stinky.Visible = false;
				await WaitFrames(6);
				using Image hidden = GetViewport().GetTexture().GetImage();
				int num = CountChangedPixels(visibleImage, hidden);
				Check(num >= 128, $"Hiding Stinky must remove visible loading-snail pixels; changed={num}.");
				GD.Print($"SCENE_LOADING_STINKY_VISIBILITY_METRIC changed={num} instances={publishedInstances} local={stinky.forceLocalRender}");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[SceneLoadingStinkyVisibilityRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(loading))
			{
				loading.QueueFree();
			}
			await WaitFrames(6);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			scene?.Dispose();
		}
		bool flag = _failures == 0 && _checks == 7;
		GD.Print($"SCENE_LOADING_STINKY_VISIBILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static int CountPublishedInstances(Node node)
	{
		int num = ((node is AdobeAnimateMultiMeshBatcher adobeAnimateMultiMeshBatcher) ? adobeAnimateMultiMeshBatcher.GetVisibleInstanceCountForTest() : 0);
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			num += CountPublishedInstances(child);
		}
		return num;
	}

	private static int CountChangedPixels(Image visible, Image hidden)
	{
		int num = Math.Min(visible.GetWidth(), hidden.GetWidth());
		int num2 = Math.Min(visible.GetHeight(), hidden.GetHeight());
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				if (ColorDistance(visible.GetPixel(j, i), hidden.GetPixel(j, i)) > 0.08f)
				{
					num3++;
				}
			}
		}
		return num3;
	}

	private static float ColorDistance(in Color left, in Color right)
	{
		return Mathf.Max(Mathf.Max(Mathf.Abs(left.R - right.R), Mathf.Abs(left.G - right.G)), Mathf.Max(Mathf.Abs(left.B - right.B), Mathf.Abs(left.A - right.A)));
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[SceneLoadingStinkyVisibilityRuntimeTest] " + message);
		}
	}
}
