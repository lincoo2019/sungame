using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateAllZombieHiddenGroundRuntimeTest : Node2D
{
	private const string ResultMarker = "ADOBE_ANIMATE_ALL_ZOMBIE_HIDDEN_GROUND_RESULT";

	private const string ZombieAnimationRoot = "res://Asset/Anime/Character/Zombie";

	private const string HiddenGroundDeclaration = "Animation/LayerVisible/_ground = false";

	private static readonly StringName HiddenGroundLayer = "_ground";

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<string> failures = new List<string>();
		try
		{
			if (Global.Instance == null)
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			AdobeAnimateRenderManager.RasterCompositeEnabled = false;
			List<string> scenePaths = new List<string>();
			CollectHiddenGroundScenePaths("res://Asset/Anime/Character/Zombie", scenePaths);
			for (int start = 0; start < scenePaths.Count; start += 8)
			{
				List<(string Path, AdobeAnimateSprite Sprite)> sprites = new List<(string, AdobeAnimateSprite)>();
				int num = Math.Min(scenePaths.Count, start + 8);
				for (int i = start; i < num; i++)
				{
					string text = scenePaths[i];
					Node node = GD.Load<PackedScene>(text)?.Instantiate(PackedScene.GenEditState.Disabled);
					AdobeAnimateSprite adobeAnimateSprite = FindAnimationSprite(node);
					if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
					{
						node?.Free();
						failures.Add(text + ": no AdobeAnimateSprite root");
					}
					else
					{
						adobeAnimateSprite.Position = new Vector2(120f + (float)(i - start) * 110f, 300f);
						AddChild(node, forceReadableName: false, InternalMode.Disabled);
						sprites.Add((text, adobeAnimateSprite));
					}
				}
				await WaitProcessFrames(4);
				for (int j = 0; j < sprites.Count; j++)
				{
					ValidateHiddenGround(sprites[j].Path, sprites[j].Sprite, failures);
				}
				for (int k = 0; k < sprites.Count; k++)
				{
					Node node2 = sprites[k].Sprite;
					while (GodotObject.IsInstanceValid(node2.GetParent()) && node2.GetParent() != this)
					{
						node2 = node2.GetParent();
					}
					node2.QueueFree();
				}
				await WaitProcessFrames(2);
			}
			if (failures.Count > 0)
			{
				throw new InvalidOperationException(string.Join(" | ", failures));
			}
			GD.Print($"{"ADOBE_ANIMATE_ALL_ZOMBIE_HIDDEN_GROUND_RESULT"} passed=True scenes={scenePaths.Count} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = 0;
		}
		catch (Exception value)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_ALL_ZOMBIE_HIDDEN_GROUND_RESULT"} passed=False exception={value}");
		}
		finally
		{
			if (Global.Instance != null)
			{
				Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			}
			AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
		}
		GetTree().Quit(exitCode);
	}

	private static void CollectHiddenGroundScenePaths(string directoryPath, List<string> result)
	{
		using DirAccess dirAccess = DirAccess.Open(directoryPath);
		if (dirAccess == null)
		{
			return;
		}
		string[] files = dirAccess.GetFiles();
		foreach (string text in files)
		{
			if (text.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = directoryPath.PathJoin(text);
				if (FileAccess.GetFileAsString(text2).Contains("Animation/LayerVisible/_ground = false", StringComparison.Ordinal))
				{
					result.Add(text2);
				}
			}
		}
		files = dirAccess.GetDirectories();
		foreach (string file in files)
		{
			CollectHiddenGroundScenePaths(directoryPath.PathJoin(file), result);
		}
	}

	private static AdobeAnimateSprite FindAnimationSprite(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		if (node is AdobeAnimateSprite result)
		{
			return result;
		}
		for (int i = 0; i < node.GetChildCount(); i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = FindAnimationSprite(node.GetChild(i));
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				return adobeAnimateSprite;
			}
		}
		return null;
	}

	private static void ValidateHiddenGround(string scenePath, AdobeAnimateSprite sprite, List<string> failures)
	{
		if (sprite.GetFliter(HiddenGroundLayer))
		{
			failures.Add(scenePath + ": logical _ground became visible");
			return;
		}
		AdobeAnimateData flashAnimeData = sprite.flashAnimeData;
		if (!GodotObject.IsInstanceValid(flashAnimeData) || flashAnimeData.layerDictionary == null || !flashAnimeData.layerDictionary.ContainsKey(HiddenGroundLayer))
		{
			failures.Add(scenePath + ": _ground layer metadata missing");
			return;
		}
		int layerId = flashAnimeData.layerDictionary[HiddenGroundLayer].AsInt32();
		AdobeAnimateCrowdRenderState state;
		AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = sprite.TryBuildCrowdRenderState(out state);
		if (adobeAnimateCrowdRenderStateResult != AdobeAnimateCrowdRenderStateResult.Submitted || state == null || state.Mode != AdobeAnimateCrowdRenderMode.GpuGraph)
		{
			failures.Add($"{scenePath}: GPU graph state unavailable result={adobeAnimateCrowdRenderStateResult} mode={state?.Mode}");
		}
		else if (IsLayerVisible(state.GpuGraphRootOwnerState, layerId))
		{
			failures.Add(scenePath + ": built GPU owner state exposed _ground");
		}
	}

	private static bool IsLayerVisible(AdobeAnimateGpuGraphOwnerState state, int layerId)
	{
		if (state == null || layerId < 0 || layerId >= state.LayerCount)
		{
			return true;
		}
		if (state.AllLayersVisible)
		{
			return true;
		}
		if (state.CanUseLayerMask && layerId < 64)
		{
			return (state.LayerMask & (ulong)(1L << layerId)) != 0;
		}
		if (state.LayerVisible != null && layerId < state.LayerVisible.Count)
		{
			return state.LayerVisible[layerId];
		}
		return true;
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}
}
