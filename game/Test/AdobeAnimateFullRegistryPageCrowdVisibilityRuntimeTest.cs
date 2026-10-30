using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public sealed partial class AdobeAnimateFullRegistryPageCrowdVisibilityRuntimeTest : Node
{
	private sealed class CharacterEntry
	{
		public string ScenePath = string.Empty;

		public TowerDefenseCharacter Character;

		public Node2D Holder;

		public readonly List<AdobeAnimateSprite> RenderRoots = new List<AdobeAnimateSprite>();

		public int Page;

		public Rect2I ScreenRegion;
	}

	private sealed class PublicationSnapshot
	{
		public readonly Dictionary<int, int> CrowdVisibleInstancesByZ = new Dictionary<int, int>();

		public int CrowdVisibleInstances;

		public int OffPageCrowdVisibleInstances;

		public bool CurrentPageCrowdMatchesExpected;

		public int FallbackVisibleInstances;

		public long RdQueueDepth;

		public long RdLastQueuedFrameVersion;

		public long RdLastAppliedFrameVersion;

		public long RdFailedBatches;

		public long RdBufferUpdateFailures;

		public long RdInvalidTargets;
	}

	private const string ResultMarker = "ADOBE_ANIMATE_FULL_REGISTRY_PAGE_CROWD_VISIBILITY_RESULT";

	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string CraterGCharacterName = "CraterG";

	private const string CrowdBucketPrefix = "AdobeAnimateCrowdZ_";

	private const string FallbackBucketPrefix = "AdobeAnimateSnapshotFallbackZ_";

	private const int ViewportWidth = 1200;

	private const int ViewportHeight = 700;

	private const int PageColumns = 6;

	private const int PageRows = 3;

	private const int CharactersPerPage = 18;

	private const int CellWidth = 190;

	private const int CellHeight = 210;

	private const int GridMarginX = 30;

	private const int GridMarginY = 30;

	private const float PageStride = 1500f;

	private const int MinimumAnimationDeltaPixels = 12;

	private static readonly Color BackgroundColor = new Color(0.013f, 0.017f, 0.023f);

	private readonly List<string> _candidateScenePaths = new List<string>(560);

	private readonly List<PackedScene> _retainedScenes = new List<PackedScene>(560);

	private readonly List<CharacterEntry> _characters = new List<CharacterEntry>(560);

	private readonly Dictionary<string, string> _registeredNameByScenePath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly List<AdobeAnimateSprite> _firstPageRenderRoots = new List<AdobeAnimateSprite>(32);

	private readonly Dictionary<int, int> _firstPageExpectedInstancesByZ = new Dictionary<int, int>();

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	private int _originalMaxFps;

	private bool _runtimePrepared;

	private Node2D _world;

	private Camera2D _camera;

	private int _expectedCharacterCount;

	private CharacterEntry _craterGEntry;

	private AdobeAnimateSprite _craterGSprite;

	private string _firstMismatch = "none";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		int exitCode = 2;
		try
		{
			_ = 9;
			try
			{
				PrepareRuntime();
				CollectRegisteredCharacterScenePaths();
				await InstantiateAllCharacters();
				await ConfigureAllCharacters();
				Require(_characters.Count == _expectedCharacterCount, $"正式角色装配不完整: {_characters.Count}/{_expectedCharacterCount}");
				Require(GodotObject.IsInstanceValid(_craterGSprite) && _craterGEntry != null, "完整注册表中未找到正式 CraterG");
				Require(AllHoldersRemainVisible(), "装配阶段错误隐藏了 Holder");
				Require(_firstPageRenderRoots.Count > 0, "正式第一页没有独立动画渲染根");
				await WaitPhysicsFrames(6);
				SetCatalogPageVisibility(0);
				_camera.Position = new Vector2(600f, 350f);
				AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
				await WaitPhysicsFrames(5);
				await WaitProcessFrames(3);
				using Image visibleImage = await CaptureImage();
				PublicationSnapshot visibleSnapshot = ReadPublicationSnapshot();
				int visiblePixels = CountSignalPixels(visibleImage, _craterGEntry.ScreenRegion);
				bool visibleRoots = FirstPageRenderRootsMatchVisibility(visible: true);
				bool visiblePageScope = HoldersMatchCatalogPageVisibility(0);
				SetFirstPageRenderRootsVisible(visible: false);
				bool hiddenRoots = FirstPageRenderRootsMatchVisibility(visible: false);
				bool hiddenPageScope = HoldersMatchCatalogPageVisibility(0);
				await WaitPhysicsFrames(3);
				using Image hiddenImage = await CaptureImage();
				PublicationSnapshot hiddenSnapshot = ReadPublicationSnapshot();
				int hiddenViewportPixels = CountSignalPixels(hiddenImage, new Rect2I(0, 0, 1200, 700));
				int visibleDelta = CountChangedPixels(visibleImage, hiddenImage, _craterGEntry.ScreenRegion);
				SetFirstPageRenderRootsVisible(visible: true);
				bool restoredRoots = FirstPageRenderRootsMatchVisibility(visible: true);
				bool restoredPageScope = HoldersMatchCatalogPageVisibility(0);
				await WaitPhysicsFrames(4);
				using Image image = await CaptureImage();
				PublicationSnapshot publicationSnapshot = ReadPublicationSnapshot();
				int num = CountSignalPixels(image, _craterGEntry.ScreenRegion);
				int num2 = CountChangedPixels(image, hiddenImage, _craterGEntry.ScreenRegion);
				EvaluateVisiblePhase(visibleSnapshot, visiblePixels, visibleRoots, visiblePageScope);
				EvaluateHiddenPhase(hiddenSnapshot, hiddenViewportPixels, visibleDelta, hiddenRoots, hiddenPageScope);
				EvaluateRestoredPhase(publicationSnapshot, num, num2, restoredRoots, restoredPageScope);
				bool flag = _firstMismatch == "none";
				GD.Print($"{"ADOBE_ANIMATE_FULL_REGISTRY_PAGE_CROWD_VISIBILITY_RESULT"} phase=visible pixels={visiblePixels} roots={visibleRoots} pageScope={visiblePageScope} {FormatSnapshot(visibleSnapshot)}");
				GD.Print($"{"ADOBE_ANIMATE_FULL_REGISTRY_PAGE_CROWD_VISIBILITY_RESULT"} phase=hidden viewportPixels={hiddenViewportPixels} delta={visibleDelta} roots={hiddenRoots} pageScope={hiddenPageScope} {FormatSnapshot(hiddenSnapshot)}");
				GD.Print($"{"ADOBE_ANIMATE_FULL_REGISTRY_PAGE_CROWD_VISIBILITY_RESULT"} phase=restored pixels={num} delta={num2} roots={restoredRoots} pageScope={restoredPageScope} {FormatSnapshot(publicationSnapshot)}");
				if (hiddenSnapshot.CrowdVisibleInstances > 0)
				{
					GD.PrintErr("ADOBE_ANIMATE_FULL_REGISTRY_PAGE_CROWD_VISIBILITY_RESULT hiddenCrowdBuckets=" + DescribeVisibleCrowdBuckets());
				}
				GD.Print($"{"ADOBE_ANIMATE_FULL_REGISTRY_PAGE_CROWD_VISIBILITY_RESULT"} passed={flag} checks={_checks} failures={_failures} characters={_characters.Count}/{_expectedCharacterCount} pageRoots={_firstPageRenderRoots.Count} firstMismatch={_firstMismatch} offPageCrowdVisible={visibleSnapshot.OffPageCrowdVisibleInstances} hiddenCrowdVisible={hiddenSnapshot.CrowdVisibleInstances} hiddenFallbackVisible={hiddenSnapshot.FallbackVisibleInstances} hiddenViewportPixels={hiddenViewportPixels} visiblePixels={visiblePixels} restoredPixels={num} visibleDelta={visibleDelta} restoredDelta={num2} rdFailures={publicationSnapshot.RdFailedBatches}/{publicationSnapshot.RdBufferUpdateFailures}/{publicationSnapshot.RdInvalidTargets} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag) ? 2 : 0);
			}
			catch (Exception value)
			{
				_failures++;
				GD.PrintErr($"{"ADOBE_ANIMATE_FULL_REGISTRY_PAGE_CROWD_VISIBILITY_RESULT"} passed=False exception={value}");
			}
		}
		finally
		{
			await RestoreRuntime();
		}
		GetTree().Quit(exitCode);
	}

	private void PrepareRuntime()
	{
		if (!GodotObject.IsInstanceValid(Global.Instance))
		{
			throw new InvalidOperationException("Global autoload is unavailable.");
		}
		base.ProcessMode = ProcessModeEnum.Always;
		_originalBackend = Global.Instance.adobeAnimateRenderBackend;
		_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
		_originalMaxFps = Engine.MaxFps;
		_runtimePrepared = true;
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
		AdobeAnimateRenderManager.RasterCompositeEnabled = false;
		Engine.MaxFps = Math.Max(120, Engine.PhysicsTicksPerSecond * 2);
		CanvasLayer canvasLayer = new CanvasLayer
		{
			Name = "BackgroundLayer",
			Layer = -100,
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(canvasLayer, forceReadableName: false, InternalMode.Disabled);
		canvasLayer.AddChild(new ColorRect
		{
			Name = "Background",
			Color = BackgroundColor,
			Position = Vector2.Zero,
			Size = new Vector2(1200f, 700f),
			MouseFilter = Control.MouseFilterEnum.Ignore
		}, forceReadableName: false, InternalMode.Disabled);
		_world = new Node2D
		{
			Name = "AllCharacterWorld",
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(_world, forceReadableName: false, InternalMode.Disabled);
		_camera = new Camera2D
		{
			Name = "FirstPageCamera",
			Position = new Vector2(600f, 350f),
			Enabled = true,
			ProcessMode = ProcessModeEnum.Always
		};
		_world.AddChild(_camera, forceReadableName: false, InternalMode.Disabled);
	}

	private void CollectRegisteredCharacterScenePaths()
	{
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Ignore);
		Require(GodotObject.IsInstanceValid(json), "正式角色注册表无法加载");
		FullGameplayRegistryRoots registryRoots = FullGameplayResourceManifest.GetRegistryRoots(json);
		_expectedCharacterCount = registryRoots.RegisteredCharacterCount;
		_candidateScenePaths.Clear();
		_registeredNameByScenePath.Clear();
		foreach (var (value, text3) in registryRoots.ScenePathByCharacter)
		{
			if (!_registeredNameByScenePath.TryAdd(text3, value))
			{
				throw new InvalidOperationException("正式角色注册表包含重复场景: " + text3);
			}
		}
		_candidateScenePaths.AddRange(registryRoots.UniqueSceneRoots);
		Require(_candidateScenePaths.Count == _expectedCharacterCount, $"正式角色场景清单不完整: {_candidateScenePaths.Count}/{_expectedCharacterCount}");
	}

	private async Task InstantiateAllCharacters()
	{
		for (int sceneIndex = 0; sceneIndex < _candidateScenePaths.Count; sceneIndex++)
		{
			string text = _candidateScenePaths[sceneIndex];
			PackedScene packedScene = ResourceLoader.Load<PackedScene>(text, null, ResourceLoader.CacheMode.Reuse);
			Require(GodotObject.IsInstanceValid(packedScene), "正式角色场景无法加载: " + text);
			_retainedScenes.Add(packedScene);
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			Require(node is TowerDefenseCharacter, "正式角色场景根类型错误: " + text + ", actual=" + node?.GetType().Name);
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)node;
			FreezeDetachedAnimationTree(towerDefenseCharacter);
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.skipDestroySet = true;
			towerDefenseCharacter.editorPreviewMode = true;
			towerDefenseCharacter.forceLocalRenderDuringZMotion = false;
			towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
			Node2D node2D = new Node2D
			{
				Name = $"CharacterHolder{_characters.Count}",
				Visible = true,
				ProcessMode = ProcessModeEnum.Always
			};
			_world.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			node2D.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			CharacterEntry characterEntry = new CharacterEntry
			{
				ScenePath = text,
				Character = towerDefenseCharacter,
				Holder = node2D
			};
			_characters.Add(characterEntry);
			if (_registeredNameByScenePath.TryGetValue(text, out var value) && value == "CraterG")
			{
				_craterGEntry = characterEntry;
			}
			if ((sceneIndex + 1) % 8 == 0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
	}

	private async Task ConfigureAllCharacters()
	{
		for (int index = 0; index < _characters.Count; index++)
		{
			CharacterEntry characterEntry = _characters[index];
			List<AdobeAnimateSprite> list = new List<AdobeAnimateSprite>();
			CollectSprites(characterEntry.Character, list);
			Rect2 rect = default(Rect2);
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = list[i];
				bool num = adobeAnimateSprite == characterEntry.Character.sprite;
				adobeAnimateSprite.ProcessMode = ProcessModeEnum.Always;
				adobeAnimateSprite.forceLocalRender = false;
				adobeAnimateSprite.pause = false;
				adobeAnimateSprite.timeScale = 0.0;
				if (num)
				{
					adobeAnimateSprite.Visible = true;
					EnsureCatalogAnimation(characterEntry.Character, adobeAnimateSprite);
				}
				else
				{
					EnsureValidAnimation(adobeAnimateSprite);
				}
				adobeAnimateSprite.RefreshProcessScheduling();
				if (adobeAnimateSprite.IsVisibleInTree())
				{
					AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateSprite.flashAnimeData, adobeAnimateSprite.atlasProfileOverride);
					Rect2 currentFrameBounds = GetCurrentFrameBounds(adobeAnimateSprite, orBuild);
					if (IsValidBounds(currentFrameBounds))
					{
						Transform2D transform = characterEntry.Holder.GlobalTransform.AffineInverse() * adobeAnimateSprite.GlobalTransform;
						Rect2 rect2 = new Rect2(currentFrameBounds.Position + adobeAnimateSprite.offset, currentFrameBounds.Size);
						Rect2 rect3 = TransformRect(transform, rect2);
						rect = (flag ? MergeBounds(rect, rect3) : rect3);
						flag = true;
					}
					if (!adobeAnimateSprite.IsRenderedByParentSpriteForRender() && !adobeAnimateSprite.IsEmptyHiddenGpuGraphPlaceholderForRender())
					{
						Require(AdobeAnimateGpuRenderGraphBuilder.TryBuild(adobeAnimateSprite, out var _, out var _, out var failureReason), $"正式角色 GPU Graph 被拒绝: {characterEntry.ScenePath}:{adobeAnimateSprite.GetPath()}, reason={failureReason}");
						characterEntry.RenderRoots.Add(adobeAnimateSprite);
					}
				}
			}
			Require(characterEntry.RenderRoots.Count > 0, "正式角色没有独立动画渲染根: " + characterEntry.ScenePath);
			if (!flag)
			{
				rect = new Rect2(-64f, -96f, 128f, 192f);
			}
			PlaceCharacterEntry(characterEntry, index, rect);
			if (characterEntry.Page == 0)
			{
				_firstPageRenderRoots.AddRange(characterEntry.RenderRoots);
			}
			if (characterEntry == _craterGEntry)
			{
				_craterGSprite = characterEntry.Character.sprite;
			}
			if ((index + 1) % 8 == 0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		Require(_craterGEntry != null && _craterGEntry.Page == 0, $"CraterG 不在正式第一页: page={_craterGEntry?.Page}");
		Require(GodotObject.IsInstanceValid(_craterGSprite), "CraterG 主动画根无效");
		_firstPageExpectedInstancesByZ.Clear();
		for (int j = 0; j < _firstPageRenderRoots.Count; j++)
		{
			AdobeAnimateSprite adobeAnimateSprite2 = _firstPageRenderRoots[j];
			AdobeAnimateCrowdRenderState state;
			AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = adobeAnimateSprite2.TryBuildCrowdRenderState(out state);
			Require(adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted && state != null && state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph, $"第一页动画根未进入 GPU Graph: {adobeAnimateSprite2.GetPath()}, result={adobeAnimateCrowdRenderStateResult}, mode={state?.Mode}");
			_firstPageExpectedInstancesByZ.TryGetValue(state.EffectiveZIndex, out var value);
			_firstPageExpectedInstancesByZ[state.EffectiveZIndex] = value + 1;
		}
	}

	private void SetFirstPageRenderRootsVisible(bool visible)
	{
		for (int i = 0; i < _firstPageRenderRoots.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _firstPageRenderRoots[i];
			Require(GodotObject.IsInstanceValid(adobeAnimateSprite), $"第一页渲染根在切换 visible={visible} 前失效");
			adobeAnimateSprite.Visible = visible;
		}
	}

	private bool FirstPageRenderRootsMatchVisibility(bool visible)
	{
		for (int i = 0; i < _firstPageRenderRoots.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _firstPageRenderRoots[i];
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite) || adobeAnimateSprite.Visible != visible || adobeAnimateSprite.IsVisibleInTree() != visible)
			{
				return false;
			}
		}
		return true;
	}

	private bool AllHoldersRemainVisible()
	{
		for (int i = 0; i < _characters.Count; i++)
		{
			Node2D holder = _characters[i].Holder;
			if (!GodotObject.IsInstanceValid(holder) || !holder.Visible || !holder.IsVisibleInTree())
			{
				return false;
			}
		}
		return true;
	}

	private void SetCatalogPageVisibility(int page)
	{
		for (int i = 0; i < _characters.Count; i++)
		{
			CharacterEntry characterEntry = _characters[i];
			characterEntry.Holder.Visible = characterEntry.Page == page;
		}
	}

	private bool HoldersMatchCatalogPageVisibility(int page)
	{
		for (int i = 0; i < _characters.Count; i++)
		{
			CharacterEntry characterEntry = _characters[i];
			bool flag = characterEntry.Page == page;
			if (!GodotObject.IsInstanceValid(characterEntry.Holder) || characterEntry.Holder.Visible != flag || characterEntry.Holder.IsVisibleInTree() != flag)
			{
				return false;
			}
		}
		return true;
	}

	private PublicationSnapshot ReadPublicationSnapshot()
	{
		AdobeAnimateRenderManager.ResolveRenderMount(_craterGSprite, out var mountParent, out var _);
		PublicationSnapshot publicationSnapshot = new PublicationSnapshot();
		AccumulatePublicationNodes(mountParent, publicationSnapshot);
		FinalizeCurrentPageCrowdCounts(publicationSnapshot);
		AdobeAnimateMultiMeshRdUploadDispatcher.StatisticsSnapshot statistics = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.Statistics;
		publicationSnapshot.RdQueueDepth = statistics.CurrentQueueDepth;
		publicationSnapshot.RdLastQueuedFrameVersion = statistics.LastQueuedFrameVersion;
		publicationSnapshot.RdLastAppliedFrameVersion = statistics.LastAppliedFrameVersion;
		publicationSnapshot.RdFailedBatches = statistics.FailedBatchCount;
		publicationSnapshot.RdBufferUpdateFailures = statistics.BufferUpdateFailureCount;
		publicationSnapshot.RdInvalidTargets = statistics.InvalidTargetCount;
		return publicationSnapshot;
	}

	private void AccumulatePublicationNodes(Node node, PublicationSnapshot snapshot)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		string text = node.Name.ToString();
		if (node is MultiMeshInstance2D multiMeshInstance2D && text.StartsWith("AdobeAnimateCrowdZ_", StringComparison.Ordinal) && GodotObject.IsInstanceValid(multiMeshInstance2D.Multimesh))
		{
			int nativeVisibleInstanceCount = GetNativeVisibleInstanceCount(multiMeshInstance2D);
			snapshot.CrowdVisibleInstances += nativeVisibleInstanceCount;
			if (nativeVisibleInstanceCount > 0)
			{
				snapshot.CrowdVisibleInstancesByZ.TryGetValue(multiMeshInstance2D.ZIndex, out var value);
				snapshot.CrowdVisibleInstancesByZ[multiMeshInstance2D.ZIndex] = value + nativeVisibleInstanceCount;
			}
		}
		if (node is AdobeAnimateMultiMeshBatcher adobeAnimateMultiMeshBatcher && text.StartsWith("AdobeAnimateSnapshotFallbackZ_", StringComparison.Ordinal))
		{
			snapshot.FallbackVisibleInstances += Math.Max(0, adobeAnimateMultiMeshBatcher.GetVisibleInstanceCountForTest());
		}
		int childCount = node.GetChildCount(includeInternal: true);
		for (int i = 0; i < childCount; i++)
		{
			AccumulatePublicationNodes(node.GetChild(i, includeInternal: true), snapshot);
		}
	}

	private void FinalizeCurrentPageCrowdCounts(PublicationSnapshot snapshot)
	{
		bool currentPageCrowdMatchesExpected = true;
		foreach (KeyValuePair<int, int> item in snapshot.CrowdVisibleInstancesByZ)
		{
			_firstPageExpectedInstancesByZ.TryGetValue(item.Key, out var value);
			if (item.Value > value)
			{
				snapshot.OffPageCrowdVisibleInstances += item.Value - value;
			}
			if (item.Value != value)
			{
				currentPageCrowdMatchesExpected = false;
			}
		}
		foreach (KeyValuePair<int, int> item2 in _firstPageExpectedInstancesByZ)
		{
			snapshot.CrowdVisibleInstancesByZ.TryGetValue(item2.Key, out var value2);
			if (value2 != item2.Value)
			{
				currentPageCrowdMatchesExpected = false;
			}
		}
		snapshot.CurrentPageCrowdMatchesExpected = currentPageCrowdMatchesExpected;
	}

	private static int GetNativeVisibleInstanceCount(MultiMeshInstance2D instance)
	{
		int num = RenderingServer.MultimeshGetVisibleInstances(instance.Multimesh.GetRid());
		if (num < 0)
		{
			return Math.Max(0, instance.Multimesh.InstanceCount);
		}
		return num;
	}

	private string DescribeVisibleCrowdBuckets()
	{
		AdobeAnimateRenderManager.ResolveRenderMount(_craterGSprite, out var mountParent, out var _);
		List<string> list = new List<string>();
		CollectVisibleCrowdBucketDescriptions(mountParent, list);
		if (list.Count != 0)
		{
			return string.Join(";", list);
		}
		return "none";
	}

	private static void CollectVisibleCrowdBucketDescriptions(Node node, List<string> descriptions)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is MultiMeshInstance2D multiMeshInstance2D && node.Name.ToString().StartsWith("AdobeAnimateCrowdZ_", StringComparison.Ordinal) && GodotObject.IsInstanceValid(multiMeshInstance2D.Multimesh))
		{
			int nativeVisibleInstanceCount = GetNativeVisibleInstanceCount(multiMeshInstance2D);
			if (nativeVisibleInstanceCount > 0)
			{
				descriptions.Add($"{multiMeshInstance2D.Name}[z={multiMeshInstance2D.ZIndex},instances={nativeVisibleInstanceCount}/{multiMeshInstance2D.Multimesh.InstanceCount}]");
			}
		}
		int childCount = node.GetChildCount(includeInternal: true);
		for (int i = 0; i < childCount; i++)
		{
			CollectVisibleCrowdBucketDescriptions(node.GetChild(i, includeInternal: true), descriptions);
		}
	}

	private void EvaluateVisiblePhase(PublicationSnapshot snapshot, int visiblePixels, bool rootsVisible, bool pageScopeCorrect)
	{
		RecordMismatch(rootsVisible && pageScopeCorrect, "visible_scope");
		RecordMismatch(visiblePixels >= 12, "visible_pixels");
		RecordMismatch(snapshot.CrowdVisibleInstances == _firstPageRenderRoots.Count && snapshot.CurrentPageCrowdMatchesExpected, "visible_crowd");
		RecordMismatch(snapshot.OffPageCrowdVisibleInstances == 0, "off_page_crowd");
		RecordMismatch(snapshot.FallbackVisibleInstances == 0, "visible_fallback");
		RecordMismatch(IsRdHealthy(snapshot), "visible_rd");
	}

	private void EvaluateHiddenPhase(PublicationSnapshot snapshot, int hiddenViewportPixels, int visibleDelta, bool rootsHidden, bool pageScopeCorrect)
	{
		RecordMismatch(rootsHidden && pageScopeCorrect, "hidden_scope");
		RecordMismatch(snapshot.CrowdVisibleInstances == 0, "hidden_crowd");
		RecordMismatch(snapshot.FallbackVisibleInstances == 0, "hidden_fallback");
		RecordMismatch(hiddenViewportPixels == 0 && visibleDelta >= 12, "hidden_pixels");
		RecordMismatch(IsRdHealthy(snapshot), "hidden_rd");
	}

	private void EvaluateRestoredPhase(PublicationSnapshot snapshot, int restoredPixels, int restoredDelta, bool rootsVisible, bool pageScopeCorrect)
	{
		RecordMismatch(rootsVisible && pageScopeCorrect, "restored_scope");
		RecordMismatch(snapshot.CrowdVisibleInstances == _firstPageRenderRoots.Count && snapshot.CurrentPageCrowdMatchesExpected && snapshot.OffPageCrowdVisibleInstances == 0, "restored_crowd");
		RecordMismatch(snapshot.FallbackVisibleInstances == 0, "restored_fallback");
		RecordMismatch(restoredPixels >= 12 && restoredDelta >= 12, "restored_pixels");
		RecordMismatch(IsRdHealthy(snapshot), "restored_rd");
	}

	private static bool IsRdHealthy(PublicationSnapshot snapshot)
	{
		if (snapshot.RdQueueDepth == 0L && snapshot.RdLastAppliedFrameVersion >= snapshot.RdLastQueuedFrameVersion && snapshot.RdFailedBatches == 0L && snapshot.RdBufferUpdateFailures == 0L)
		{
			return snapshot.RdInvalidTargets == 0;
		}
		return false;
	}

	private void RecordMismatch(bool condition, string layer)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			if (_firstMismatch == "none")
			{
				_firstMismatch = layer;
			}
		}
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<Image> CaptureImage()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		return GetViewport().GetTexture().GetImage();
	}

	private static int CountSignalPixels(Image image, Rect2I region)
	{
		int num = 0;
		int num2 = Math.Min(image.GetWidth(), region.End.X);
		int num3 = Math.Min(image.GetHeight(), region.End.Y);
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
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

	private static int CountChangedPixels(Image first, Image second, Rect2I region)
	{
		int num = 0;
		int num2 = Math.Min(Math.Min(first.GetWidth(), second.GetWidth()), region.End.X);
		int num3 = Math.Min(Math.Min(first.GetHeight(), second.GetHeight()), region.End.Y);
		for (int i = Math.Max(0, region.Position.Y); i < num3; i++)
		{
			for (int j = Math.Max(0, region.Position.X); j < num2; j++)
			{
				Color pixel = first.GetPixel(j, i);
				Color pixel2 = second.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - pixel2.R) + Mathf.Abs(pixel.G - pixel2.G) + Mathf.Abs(pixel.B - pixel2.B) + Mathf.Abs(pixel.A - pixel2.A) > 0.12f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static void CollectSprites(Node node, List<AdobeAnimateSprite> output)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			if (node is AdobeAnimateSprite item)
			{
				output.Add(item);
			}
			for (int i = 0; i < node.GetChildCount(); i++)
			{
				CollectSprites(node.GetChild(i), output);
			}
		}
	}

	private static void FreezeDetachedAnimationTree(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			if (node is AdobeAnimateSprite adobeAnimateSprite)
			{
				adobeAnimateSprite.timeScale = 0.0;
				adobeAnimateSprite.pause = false;
			}
			for (int i = 0; i < node.GetChildCount(); i++)
			{
				FreezeDetachedAnimationTree(node.GetChild(i));
			}
		}
	}

	private static Rect2 GetCurrentFrameBounds(AdobeAnimateSprite sprite, AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			return default(Rect2);
		}
		if (sprite.frameIndex >= 0 && sprite.frameIndex < definition.FrameLocalBounds.Length)
		{
			Rect2 rect = definition.FrameLocalBounds[sprite.frameIndex];
			if (IsValidBounds(rect))
			{
				return rect;
			}
		}
		return definition.LocalBounds;
	}

	private static void EnsureCatalogAnimation(TowerDefenseCharacter character, AdobeAnimateSprite sprite)
	{
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(sprite.flashAnimeData, sprite.atlasProfileOverride);
		if (orBuild == null)
		{
			return;
		}
		if (character is TowerDefenseZombie towerDefenseZombie)
		{
			string firstConfiguredClip = GetFirstConfiguredClip(towerDefenseZombie.walkAnimeClip);
			if (orBuild.HasClip(firstConfiguredClip))
			{
				sprite.SetAnimation(firstConfiguredClip);
				return;
			}
		}
		if (character is TowerDefensePlant towerDefensePlant)
		{
			string firstConfiguredClip2 = GetFirstConfiguredClip(towerDefensePlant.plantAnimeClip);
			if (orBuild.HasClip(firstConfiguredClip2))
			{
				sprite.SetAnimation(firstConfiguredClip2);
				return;
			}
		}
		if (orBuild.HasClip("Idle"))
		{
			sprite.SetAnimation("Idle");
		}
		else if (orBuild.HasClip("Walk"))
		{
			sprite.SetAnimation("Walk");
		}
		else
		{
			EnsureValidAnimation(sprite);
		}
	}

	private static void EnsureValidAnimation(AdobeAnimateSprite sprite)
	{
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(sprite.flashAnimeData, sprite.atlasProfileOverride);
		if (orBuild == null || orBuild.Clips.Length == 0)
		{
			return;
		}
		if (orBuild.HasClip(sprite.clip))
		{
			if (sprite.clipOver)
			{
				sprite.SetAnimation(sprite.clip);
			}
			return;
		}
		for (int i = 0; i < orBuild.Clips.Length; i++)
		{
			PackedClip packedClip = orBuild.Clips[i];
			if (packedClip.Range.Y >= packedClip.Range.X)
			{
				sprite.SetAnimation(packedClip.Name.ToString());
				break;
			}
		}
	}

	private static string GetFirstConfiguredClip(string clips)
	{
		if (string.IsNullOrWhiteSpace(clips))
		{
			return string.Empty;
		}
		string[] array = clips.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length == 0)
		{
			return string.Empty;
		}
		return array[0];
	}

	private static void PlaceCharacterEntry(CharacterEntry entry, int index, Rect2 bounds)
	{
		int num = index / 18;
		int num2 = index % 18;
		int num3 = num2 % 6;
		int num4 = num2 / 6;
		Rect2I screenRegion = new Rect2I(30 + num3 * 190, 30 + num4 * 210, 170, 190);
		float num5 = (float)screenRegion.Size.X - 20f;
		float value = Math.Min(val2: ((float)screenRegion.Size.Y - 20f) / Math.Max(1f, bounds.Size.Y), val1: num5 / Math.Max(1f, bounds.Size.X));
		value = Mathf.Clamp(value, 0.015f, 1.35f);
		entry.Page = num;
		entry.ScreenRegion = screenRegion;
		entry.Holder.Scale = Vector2.One * value;
		Vector2 vector = new Vector2((float)num * 1500f, 0f) + screenRegion.GetCenter();
		entry.Holder.Position = vector - bounds.GetCenter() * value;
	}

	private static bool IsValidBounds(Rect2 bounds)
	{
		if (float.IsFinite(bounds.Position.X) && float.IsFinite(bounds.Position.Y) && float.IsFinite(bounds.Size.X) && float.IsFinite(bounds.Size.Y) && bounds.Size.X > 0f)
		{
			return bounds.Size.Y > 0f;
		}
		return false;
	}

	private static Rect2 TransformRect(Transform2D transform, Rect2 rect)
	{
		Vector2 position = transform * rect.Position;
		Vector2 to = transform * (rect.Position + new Vector2(rect.Size.X, 0f));
		Vector2 to2 = transform * (rect.Position + rect.Size);
		Vector2 to3 = transform * (rect.Position + new Vector2(0f, rect.Size.Y));
		return new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3)
			.Abs();
	}

	private static Rect2 MergeBounds(Rect2 left, Rect2 right)
	{
		Vector2 to = right.Position + right.Size;
		return left.Expand(right.Position).Expand(new Vector2(to.X, right.Position.Y)).Expand(to)
			.Expand(new Vector2(right.Position.X, to.Y))
			.Abs();
	}

	private static string FormatSnapshot(PublicationSnapshot snapshot)
	{
		return $"crowdVisible={snapshot.CrowdVisibleInstances} crowdMatchesPage={snapshot.CurrentPageCrowdMatchesExpected} offPageCrowdVisible={snapshot.OffPageCrowdVisibleInstances} fallbackVisible={snapshot.FallbackVisibleInstances} rdQueue={snapshot.RdQueueDepth} rdQueued={snapshot.RdLastQueuedFrameVersion} rdApplied={snapshot.RdLastAppliedFrameVersion} rdFailures={snapshot.RdFailedBatches}/{snapshot.RdBufferUpdateFailures}/{snapshot.RdInvalidTargets}";
	}

	private async Task RestoreRuntime()
	{
		if (GodotObject.IsInstanceValid(_world) && !_world.IsQueuedForDeletion())
		{
			_world.QueueFree();
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		AdobeAnimateDefinitionCache.Clear();
		_retainedScenes.Clear();
		_characters.Clear();
		_firstPageRenderRoots.Clear();
		_firstPageExpectedInstancesByZ.Clear();
		if (_runtimePrepared && GodotObject.IsInstanceValid(Global.Instance))
		{
			Global.Instance.adobeAnimateRenderBackend = _originalBackend;
		}
		if (_runtimePrepared)
		{
			AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
			Engine.MaxFps = _originalMaxFps;
		}
	}

	private void Require(bool condition, string message)
	{
		_checks++;
		if (condition)
		{
			return;
		}
		_failures++;
		throw new InvalidOperationException(message);
	}
}
