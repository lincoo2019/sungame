using System;
using System.Collections.Generic;
using Godot;

public partial class AdobeAnimateAllAnimationGpuGraphAuditTest : Node
{
	private const string AnimationRoot = "res://Asset/Anime";

	private const int YieldInterval = 8;

	private const int MaximumPrintedFailures = 200;

	private readonly List<string> _scenePaths = new List<string>(1200);

	private readonly List<AdobeAnimateSprite> _sprites = new List<AdobeAnimateSprite>(32);

	private readonly List<PackedScene> _retainedScenes = new List<PackedScene>(1200);

	private readonly List<Node> _retainedInstances = new List<Node>(1200);

	private readonly Dictionary<string, int> _failureReasonCounts = new Dictionary<string, int>(StringComparer.Ordinal);

	private readonly List<string> _failures = new List<string>();

	private int _animationScenes;

	private int _loadFailures;

	private int _instantiateFailures;

	private int _renderRoots;

	private int _emptyPlaceholders;

	private int _gpuGraphRoots;

	private int _gpuGraphFailures;

	private int _animationOwners;

	private int _animationClips;

	private long _gpuGraphOwners;

	private long _gpuGraphSlots;

	public override async void _Ready()
	{
		bool previousGpuGraphEnabled = AdobeAnimateRenderManager.GpuRenderGraphEnabled;
		try
		{
			AdobeAnimateRenderManager.GpuRenderGraphEnabled = true;
			AdobeAnimateDefinitionCache.Clear();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches(notifyRuntime: false);
			CollectScenePaths("res://Asset/Anime", _scenePaths);
			_scenePaths.Sort(StringComparer.OrdinalIgnoreCase);
			for (int sceneIndex = 0; sceneIndex < _scenePaths.Count; sceneIndex++)
			{
				AuditScene(_scenePaths[sceneIndex]);
				if ((sceneIndex + 1) % 8 == 0)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
		}
		catch (Exception value)
		{
			AddFailure("<audit>", $"unexpected exception: {value}");
		}
		finally
		{
			ReleaseRetainedInstances();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches(notifyRuntime: false);
			AdobeAnimateDefinitionCache.Clear();
			AdobeAnimateRenderManager.GpuRenderGraphEnabled = previousGpuGraphEnabled;
		}
		PrintResult();
		bool flag = _loadFailures == 0 && _instantiateFailures == 0 && _gpuGraphFailures == 0;
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void AuditScene(string scenePath)
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			_loadFailures++;
			AddFailure(scenePath, "PackedScene load failed");
			return;
		}
		_retainedScenes.Add(packedScene);
		Node node = null;
		try
		{
			node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(node))
			{
				_instantiateFailures++;
				AddFailure(scenePath, "PackedScene instantiate returned an invalid node");
				return;
			}
			_retainedInstances.Add(node);
			_sprites.Clear();
			CollectSprites(node, _sprites);
			if (_sprites.Count == 0)
			{
				return;
			}
			_animationScenes++;
			for (int i = 0; i < _sprites.Count; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = _sprites[i];
				adobeAnimateSprite.PrepareDetachedGpuGraphWarmup();
				if (GodotObject.IsInstanceValid(adobeAnimateSprite.flashAnimeData))
				{
					_animationOwners++;
					_animationClips += adobeAnimateSprite.flashAnimeData.clips?.Count ?? 0;
				}
			}
			for (int j = 0; j < _sprites.Count; j++)
			{
				AdobeAnimateSprite adobeAnimateSprite2 = _sprites[j];
				if (adobeAnimateSprite2.IsRenderedByParentSpriteForRender())
				{
					continue;
				}
				if (adobeAnimateSprite2.IsEmptyHiddenGpuGraphPlaceholderForRender())
				{
					_emptyPlaceholders++;
					continue;
				}
				_renderRoots++;
				if (!AdobeAnimateGpuRenderGraphBuilder.TryBuild(adobeAnimateSprite2, out var graph, out var ownerSprites, out var failureReason))
				{
					_gpuGraphFailures++;
					AddFailure(scenePath + ":" + GetNodePath(adobeAnimateSprite2), string.IsNullOrWhiteSpace(failureReason) ? "unknown GPU Graph rejection" : failureReason);
				}
				else
				{
					_gpuGraphRoots++;
					_gpuGraphOwners += ownerSprites.Length;
					_gpuGraphSlots += graph.RenderSlots.Length;
				}
			}
		}
		catch (Exception ex)
		{
			_instantiateFailures++;
			AddFailure(scenePath, "instantiate/audit exception: " + ex.Message);
		}
		finally
		{
			_sprites.Clear();
		}
	}

	private void AddFailure(string source, string reason)
	{
		if (reason == null)
		{
			reason = "unknown";
		}
		_failureReasonCounts.TryGetValue(reason, out var value);
		_failureReasonCounts[reason] = value + 1;
		if (_failures.Count < 200)
		{
			_failures.Add(source + " | " + reason);
		}
	}

	private void ReleaseRetainedInstances()
	{
		_sprites.Clear();
		for (int num = _retainedInstances.Count - 1; num >= 0; num--)
		{
			Node node = _retainedInstances[num];
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
		}
	}

	private void PrintResult()
	{
		for (int i = 0; i < _failures.Count; i++)
		{
			GD.Print("[AllAnimationGpuGraphAuditFailure] " + _failures[i]);
		}
		foreach (KeyValuePair<string, int> failureReasonCount in _failureReasonCounts)
		{
			GD.Print($"[AllAnimationGpuGraphAuditReason] count={failureReasonCount.Value} reason={failureReasonCount.Key}");
		}
		bool value = _loadFailures == 0 && _instantiateFailures == 0 && _gpuGraphFailures == 0;
		GD.Print($"ALL_ANIMATION_GPU_GRAPH_AUDIT_RESULT passed={value} tscn={_scenePaths.Count} animationScenes={_animationScenes} loadFailures={_loadFailures} instantiateFailures={_instantiateFailures} renderRoots={_renderRoots} emptyPlaceholders={_emptyPlaceholders} gpuGraphRoots={_gpuGraphRoots} gpuGraphFailures={_gpuGraphFailures} animationOwners={_animationOwners} animationClips={_animationClips} graphOwners={_gpuGraphOwners} graphSlots={_gpuGraphSlots}");
	}

	private static string GetNodePath(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return "<invalid>";
		}
		List<string> list = new List<string>();
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			list.Add(node2.Name.ToString());
			node2 = node2.GetParent();
		}
		list.Reverse();
		return string.Join("/", list);
	}

	private static void CollectSprites(Node node, List<AdobeAnimateSprite> output)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is AdobeAnimateSprite item)
		{
			output.Add(item);
		}
		foreach (Node child in node.GetChildren(includeInternal: true))
		{
			CollectSprites(child, output);
		}
	}

	private static void CollectScenePaths(string directoryPath, List<string> output)
	{
		using DirAccess dirAccess = DirAccess.Open(directoryPath);
		if (dirAccess == null)
		{
			return;
		}
		dirAccess.ListDirBegin();
		while (true)
		{
			string next = dirAccess.GetNext();
			if (string.IsNullOrEmpty(next))
			{
				break;
			}
			if (!(next == ".") && !(next == ".."))
			{
				string text = directoryPath + "/" + next;
				if (dirAccess.CurrentIsDir())
				{
					CollectScenePaths(text, output);
				}
				else if (next.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
				{
					output.Add(text);
				}
			}
		}
		dirAccess.ListDirEnd();
	}
}
