using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public sealed partial class AdobeAnimateAllCharacterSingleSceneStressRuntimeTest : Node
{
	private enum RegisteredCharacterRole
	{
		Unknown,
		Plant,
		Zombie,
		Item,
		Vase,
		Mower,
		Gravestone,
		Crater
	}

	private sealed class CharacterEntry
	{
		public string ScenePath = string.Empty;

		public Node Instance;

		public TowerDefenseCharacter Character;

		public Node2D Holder;

		public readonly List<AdobeAnimateSprite> RenderRoots = new List<AdobeAnimateSprite>();

		public int Page;

		public Rect2I ScreenRegion;

		public int BaselineSignalPixels;

		public int PlaybackStartFrame;

		public double PlaybackStartElapsed;

		public bool PlaybackRequiresAdvance;

		public bool PlaybackAdvanced;

		public bool PlaybackBlankDetected;
	}

	private const string ResultMarker = "ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT";

	private const string CharacterRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly StringName ReplacementMediaName = "Zombie_body.png";

	private static readonly StringName VisibilityLayerName = "Zombie_body";

	private const string ReplacementTexturePath = "res://Asset/AtlasSource/DamagePoint/Anime/Character/Plant/Chapter0/Wallnut/DamagePoint/WallnutBody.png";

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

	private const int PlaybackObservationFrameCount = 36;

	private const int MutationCharacterCount = 24;

	private const int MutationFrameCount = 120;

	private const int DeathCycleCount = 4;

	private const int DeathVictimCount = 56;

	private const int DeathMaximumObservationFrameCount = 240;

	private const double DeathAnimationTimeScale = 4.0;

	private const double DeathFadeDurationSeconds = 0.35;

	private static readonly Color BackgroundColor = new Color(0.013f, 0.017f, 0.023f);

	private static readonly Rect2I MutationCaptureRegion = new Rect2I(20, 20, 550, 660);

	private static readonly Rect2I DeathCaptureRegion = new Rect2I(590, 20, 590, 660);

	private readonly List<string> _candidateScenePaths = new List<string>(560);

	private readonly List<PackedScene> _retainedScenes = new List<PackedScene>(560);

	private readonly List<CharacterEntry> _characters = new List<CharacterEntry>(560);

	private int _expectedCharacterCount;

	private int _expectedPlantCount;

	private int _expectedZombieCount;

	private readonly HashSet<string> _expectedPlantScenePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly HashSet<string> _expectedZombieScenePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, string> _registeredCharacterNameByScenePath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly List<TowerDefenseZombieNormal> _mutationCharacters = new List<TowerDefenseZombieNormal>(24);

	private readonly List<TowerDefenseZombieNormal> _deathVictims = new List<TowerDefenseZombieNormal>(56);

	private Node2D _world;

	private Camera2D _camera;

	private Vector2 _stressPageOrigin;

	private PackedScene _normalZombieScene;

	private Texture2D _replacementTexture;

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	private int _originalMaxFps;

	private bool _playbackPassed = true;

	private int _playbackBlankFrames;

	private int _playbackVerifiedCharacters;

	private int _minimumPlaybackPixels = 2147483647;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<string> failures = new List<string>();
		try
		{
			PrepareRuntime();
			CollectRegisteredCharacterScenePaths(_candidateScenePaths);
			await InstantiateAllCharacters(failures);
			await ConfigureAllCharacters(failures);
			int plantCount = CountCharacters<TowerDefensePlant>();
			int zombieCount = CountCharacters<TowerDefenseZombie>();
			int secondaryCount = _characters.Count - plantCount - zombieCount;
			if (_candidateScenePaths.Count != _expectedCharacterCount || _characters.Count != _expectedCharacterCount || plantCount != _expectedPlantCount || zombieCount != _expectedZombieCount)
			{
				int value = _expectedCharacterCount - _expectedPlantCount - _expectedZombieCount;
				failures.Add($"production character inventory is incomplete registered={_candidateScenePaths.Count}/{_expectedCharacterCount} instantiated={_characters.Count}/{_expectedCharacterCount} plants={plantCount}/{_expectedPlantCount} zombies={zombieCount}/{_expectedZombieCount} secondary={secondaryCount}/{value}");
			}
			int pageCount = Math.Max(1, (_characters.Count + 18 - 1) / 18);
			_stressPageOrigin = new Vector2((float)pageCount * 1500f, 0f);
			bool pagesPassed = await VerifyAllCharacterPages(pageCount, failures);
			bool mutationPassed = await VerifyFrequentMediaAndLayerChanges(failures);
			bool flag = await VerifyFrequentProductionDeaths(failures);
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			int num = CountValidCatalogCharacters();
			bool flag2 = failures.Count == 0 && pagesPassed && _playbackPassed && mutationPassed && flag && num == _characters.Count && aggregateRenderStats.FallbackRoots == 0;
			PrintFailures(failures);
			GD.Print($"{"ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT"} passed={flag2} candidates={_candidateScenePaths.Count} characters={_characters.Count} plants={plantCount} zombies={zombieCount} secondary={secondaryCount} pages={pageCount} validCatalog={num}/{_characters.Count} pagePixels={pagesPassed} playback={_playbackPassed} playbackFrames={36} playbackVerified={_playbackVerifiedCharacters}/{_characters.Count} playbackBlankFrames={_playbackBlankFrames} minimumPlaybackPixels={_minimumPlaybackPixels} mutation={mutationPassed} mutationFrames={120} death={flag} deathCycles={4} deathVictimsPerCycle={56} crowdRoots={aggregateRenderStats.CrowdRoots} fallbackRoots={aggregateRenderStats.FallbackRoots} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag2) ? 2 : 0);
		}
		catch (Exception value2)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT"} passed=False exception={value2}");
		}
		finally
		{
			RestoreRuntime();
		}
		GetTree().Quit(exitCode);
	}

	private void PrepareRuntime()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		if (!GodotObject.IsInstanceValid(Global.Instance))
		{
			throw new InvalidOperationException("Global autoload is unavailable.");
		}
		_originalBackend = Global.Instance.adobeAnimateRenderBackend;
		_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
		_originalMaxFps = Engine.MaxFps;
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
			Name = "AllCharacterCamera",
			Position = new Vector2(600f, 350f),
			Enabled = true,
			ProcessMode = ProcessModeEnum.Always
		};
		_world.AddChild(_camera, forceReadableName: false, InternalMode.Disabled);
	}

	private void CollectRegisteredCharacterScenePaths(List<string> output)
	{
		Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(json))
		{
			throw new InvalidOperationException("character registry could not load path=res://Asset/Config/Character/CharacterResource.json");
		}
		FullGameplayRegistryRoots registryRoots = FullGameplayResourceManifest.GetRegistryRoots(json);
		_expectedCharacterCount = registryRoots.RegisteredCharacterCount;
		_expectedPlantCount = 0;
		_expectedZombieCount = 0;
		_expectedPlantScenePaths.Clear();
		_expectedZombieScenePaths.Clear();
		_registeredCharacterNameByScenePath.Clear();
		foreach (var (text3, text4) in registryRoots.ScenePathByCharacter)
		{
			if (!_registeredCharacterNameByScenePath.TryAdd(text4, text3))
			{
				throw new InvalidOperationException($"character registry contains duplicate Scene root path={text4}, names={_registeredCharacterNameByScenePath[text4]}/{text3}");
			}
			if (text3.StartsWith("Plant", StringComparison.Ordinal))
			{
				_expectedPlantCount++;
				_expectedPlantScenePaths.Add(text4);
			}
			else if (text3.StartsWith("Zombie", StringComparison.Ordinal))
			{
				_expectedZombieCount++;
				_expectedZombieScenePaths.Add(text4);
			}
		}
		if (registryRoots.ScenePathByCharacter.Count != registryRoots.RegisteredCharacterCount)
		{
			throw new InvalidOperationException($"character registry contains entries without Scene roots: scenes={registryRoots.ScenePathByCharacter.Count}, registered={registryRoots.RegisteredCharacterCount}");
		}
		output.AddRange(registryRoots.UniqueSceneRoots);
	}

	private async Task InstantiateAllCharacters(List<string> failures)
	{
		for (int sceneIndex = 0; sceneIndex < _candidateScenePaths.Count; sceneIndex++)
		{
			string text = _candidateScenePaths[sceneIndex];
			PackedScene packedScene = ResourceLoader.Load<PackedScene>(text, null, ResourceLoader.CacheMode.Reuse);
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				failures.Add(text + ": PackedScene load failed");
				continue;
			}
			_retainedScenes.Add(packedScene);
			Node node;
			try
			{
				node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			}
			catch (Exception ex)
			{
				failures.Add(text + ": instantiate failed: " + ex.Message);
				continue;
			}
			if (!(node is TowerDefenseCharacter towerDefenseCharacter))
			{
				failures.Add(text + ": scene root is not TowerDefenseCharacter, actual=" + node.GetType().Name);
				node.Free();
				continue;
			}
			ValidateRegisteredCharacterRole(text, towerDefenseCharacter, failures);
			FreezeDetachedAnimationTree(node);
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.skipDestroySet = true;
			towerDefenseCharacter.editorPreviewMode = true;
			towerDefenseCharacter.forceLocalRenderDuringZMotion = false;
			towerDefenseCharacter.ProcessMode = ProcessModeEnum.Disabled;
			Node2D node2D = new Node2D
			{
				Name = $"CharacterHolder{_characters.Count}",
				ProcessMode = ProcessModeEnum.Always
			};
			_world.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			node2D.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_characters.Add(new CharacterEntry
			{
				ScenePath = text,
				Instance = node,
				Character = towerDefenseCharacter,
				Holder = node2D
			});
			if ((sceneIndex + 1) % 8 == 0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		await WaitProcessFrames(20);
	}

	private void ValidateRegisteredCharacterRole(string scenePath, TowerDefenseCharacter character, List<string> failures)
	{
		if (!_registeredCharacterNameByScenePath.TryGetValue(scenePath, out var value))
		{
			failures.Add(scenePath + ": scene root has no unique registered character name");
			return;
		}
		if (!GodotObject.IsInstanceValid(character.config))
		{
			failures.Add(scenePath + ": registered character " + value + " has no valid character config");
			return;
		}
		if (!string.Equals(character.config.name, value, StringComparison.Ordinal))
		{
			failures.Add($"{scenePath}: registered character name does not match config name registered={value}, config={character.config.name}");
		}
		bool num = _expectedPlantScenePaths.Contains(scenePath);
		bool flag = _expectedZombieScenePaths.Contains(scenePath);
		RegisteredCharacterRole registeredCharacterNodeRole = GetRegisteredCharacterNodeRole(character);
		RegisteredCharacterRole registeredCharacterConfigRole = GetRegisteredCharacterConfigRole(character.config);
		if (registeredCharacterNodeRole == RegisteredCharacterRole.Unknown || registeredCharacterConfigRole == RegisteredCharacterRole.Unknown || registeredCharacterNodeRole != registeredCharacterConfigRole)
		{
			failures.Add($"{scenePath}: registered character role mismatch name={value}, node={character.GetType().Name}, nodeRole={registeredCharacterNodeRole}, config={character.config.GetType().Name}, configRole={registeredCharacterConfigRole}");
		}
		if (num && registeredCharacterNodeRole != RegisteredCharacterRole.Plant)
		{
			failures.Add($"{scenePath}: Plant registry entry resolved to nodeRole={registeredCharacterNodeRole}, configRole={registeredCharacterConfigRole}");
		}
		if (flag && registeredCharacterNodeRole != RegisteredCharacterRole.Zombie)
		{
			failures.Add($"{scenePath}: Zombie registry entry resolved to nodeRole={registeredCharacterNodeRole}, configRole={registeredCharacterConfigRole}");
		}
		if (!num && !flag && (registeredCharacterNodeRole == RegisteredCharacterRole.Plant || registeredCharacterNodeRole == RegisteredCharacterRole.Zombie))
		{
			failures.Add($"{scenePath}: non-Plant/Zombie registry entry resolved to combat role nodeRole={registeredCharacterNodeRole}, configRole={registeredCharacterConfigRole}");
		}
	}

	private static RegisteredCharacterRole GetRegisteredCharacterNodeRole(TowerDefenseCharacter character)
	{
		if (!(character is TowerDefenseVase))
		{
			if (!(character is TowerDefenseMower))
			{
				if (!(character is TowerDefenseItem))
				{
					if (!(character is TowerDefensePlant))
					{
						if (!(character is TowerDefenseZombie))
						{
							if (!(character is TowerDefenseGravestone))
							{
								if (character is TowerDefenseCrater)
								{
									return RegisteredCharacterRole.Crater;
								}
								return RegisteredCharacterRole.Unknown;
							}
							return RegisteredCharacterRole.Gravestone;
						}
						return RegisteredCharacterRole.Zombie;
					}
					return RegisteredCharacterRole.Plant;
				}
				return RegisteredCharacterRole.Item;
			}
			return RegisteredCharacterRole.Mower;
		}
		return RegisteredCharacterRole.Vase;
	}

	private static RegisteredCharacterRole GetRegisteredCharacterConfigRole(TowerDefenseCharacterConfig config)
	{
		if (!(config is TowerDefensePlantConfig))
		{
			if (!(config is TowerDefenseZombieConfig))
			{
				if (!(config is TowerDefenseItemConfig))
				{
					if (!(config is TowerDefenseVaseConfig))
					{
						if (!(config is TowerDefenseMowerConfig))
						{
							if (!(config is TowerDefenseGravestoneConfig))
							{
								if (config is TowerDefenseCraterConfig)
								{
									return RegisteredCharacterRole.Crater;
								}
								return RegisteredCharacterRole.Unknown;
							}
							return RegisteredCharacterRole.Gravestone;
						}
						return RegisteredCharacterRole.Mower;
					}
					return RegisteredCharacterRole.Vase;
				}
				return RegisteredCharacterRole.Item;
			}
			return RegisteredCharacterRole.Zombie;
		}
		return RegisteredCharacterRole.Plant;
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

	private async Task ConfigureAllCharacters(List<string> failures)
	{
		for (int index = 0; index < _characters.Count; index++)
		{
			CharacterEntry characterEntry = _characters[index];
			List<AdobeAnimateSprite> list = new List<AdobeAnimateSprite>();
			CollectSprites(characterEntry.Instance, list);
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
				if (!adobeAnimateSprite.IsVisibleInTree())
				{
					continue;
				}
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
					if (!AdobeAnimateGpuRenderGraphBuilder.TryBuild(adobeAnimateSprite, out var _, out var _, out var failureReason))
					{
						failures.Add($"{characterEntry.ScenePath}:{adobeAnimateSprite.GetPath()}: GPU Graph rejected: {failureReason}");
					}
					else
					{
						characterEntry.RenderRoots.Add(adobeAnimateSprite);
					}
				}
			}
			if (characterEntry.RenderRoots.Count == 0)
			{
				failures.Add(characterEntry.ScenePath + ": no production Adobe Animate render root");
			}
			if (!flag)
			{
				failures.Add(characterEntry.ScenePath + ": no valid animation bounds");
				rect = new Rect2(-64f, -96f, 128f, 192f);
			}
			PlaceCharacterEntry(characterEntry, index, rect);
			if ((index + 1) % 8 == 0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		await WaitPhysicsFrames(6);
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

	private async Task<bool> VerifyAllCharacterPages(int pageCount, List<string> failures)
	{
		bool passed = true;
		int minimumVisibleDelta = 2147483647;
		int minimumRestoredDelta = 2147483647;
		int minimumReturnPixels = 2147483647;
		for (int page = 0; page < pageCount; page++)
		{
			await MoveCameraToPage(page);
			using Image visibleImage = await CaptureImage();
			SetPageRenderRootsVisible(page, visible: false, failures, ref passed);
			await WaitProcessFrames(3);
			using Image hiddenImage = await CaptureImage();
			SetPageRenderRootsVisible(page, visible: true, failures, ref passed);
			await WaitProcessFrames(4);
			using Image restoredImage = await CaptureImage();
			for (int i = page * 18; i < Math.Min(_characters.Count, (page + 1) * 18); i++)
			{
				CharacterEntry characterEntry = _characters[i];
				int num = CountChangedPixels(visibleImage, hiddenImage, characterEntry.ScreenRegion);
				int num2 = CountChangedPixels(restoredImage, hiddenImage, characterEntry.ScreenRegion);
				int num3 = (characterEntry.BaselineSignalPixels = CountSignalPixels(restoredImage, characterEntry.ScreenRegion));
				minimumVisibleDelta = Math.Min(minimumVisibleDelta, num);
				minimumRestoredDelta = Math.Min(minimumRestoredDelta, num2);
				if (num < 12 || num2 < 12 || num3 < 12)
				{
					passed = false;
					failures.Add($"{characterEntry.ScenePath}: animation pixels missing visibleDelta={num} restoredDelta={num2} signal={num3}");
				}
				ValidatePageGpuRoots(characterEntry, failures, ref passed);
			}
			AdobeAnimateCrowdAggregateStats pageStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			if (pageStats.FallbackRoots != 0)
			{
				passed = false;
				failures.Add($"page {page}: fallback roots={pageStats.FallbackRoots} runs={pageStats.FallbackRuns} signatureConflicts={pageStats.SignatureConflictBuckets} resourceSignatures={pageStats.ResourceSignatures}");
			}
			bool flag = await VerifyPagePlayback(page, failures);
			_playbackPassed &= flag;
			passed = passed && flag;
			GD.Print($"{"ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT"} page={page + 1}/{pageCount} minVisibleDelta={minimumVisibleDelta} minRestoredDelta={minimumRestoredDelta} playbackPassed={flag} playbackVerified={_playbackVerifiedCharacters}/{_characters.Count} playbackBlankFrames={_playbackBlankFrames} crowdRoots={pageStats.CrowdRoots} fallbackRoots={pageStats.FallbackRoots} fallbackRuns={pageStats.FallbackRuns} signatureConflicts={pageStats.SignatureConflictBuckets} resourceSignatures={pageStats.ResourceSignatures}");
		}
		for (int page = pageCount - 1; page >= 0; page--)
		{
			await MoveCameraToPage(page);
			using Image image = await CaptureImage();
			for (int j = page * 18; j < Math.Min(_characters.Count, (page + 1) * 18); j++)
			{
				CharacterEntry characterEntry2 = _characters[j];
				int num4 = CountSignalPixels(image, characterEntry2.ScreenRegion);
				minimumReturnPixels = Math.Min(minimumReturnPixels, num4);
				int num5 = Math.Max(12, characterEntry2.BaselineSignalPixels / 5);
				if (num4 < num5)
				{
					passed = false;
					failures.Add($"{characterEntry2.ScenePath}: offscreen return lost pixels baseline={characterEntry2.BaselineSignalPixels} returned={num4}");
				}
			}
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			if (aggregateRenderStats.FallbackRoots != 0)
			{
				passed = false;
				failures.Add($"return page {page}: fallback roots={aggregateRenderStats.FallbackRoots} runs={aggregateRenderStats.FallbackRuns} signatureConflicts={aggregateRenderStats.SignatureConflictBuckets} resourceSignatures={aggregateRenderStats.ResourceSignatures}");
			}
		}
		GD.Print($"{"ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT"} pageAudit={passed} playbackAudit={_playbackPassed} characters={_characters.Count} playbackVerified={_playbackVerifiedCharacters}/{_characters.Count} playbackBlankFrames={_playbackBlankFrames} minimumPlaybackPixels={_minimumPlaybackPixels} minimumVisibleDelta={minimumVisibleDelta} minimumRestoredDelta={minimumRestoredDelta} minimumReturnPixels={minimumReturnPixels}");
		return passed;
	}

	private async Task<bool> VerifyPagePlayback(int page, List<string> failures)
	{
		bool passed = true;
		int startIndex = page * 18;
		int endIndex = Math.Min(_characters.Count, (page + 1) * 18);
		for (int i = startIndex; i < endIndex; i++)
		{
			CharacterEntry characterEntry = _characters[i];
			AdobeAnimateSprite adobeAnimateSprite = characterEntry.Character?.sprite;
			characterEntry.PlaybackAdvanced = false;
			characterEntry.PlaybackBlankDetected = false;
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				passed = false;
				failures.Add(characterEntry.ScenePath + ": main animation root is invalid before real playback");
				continue;
			}
			AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateSprite.flashAnimeData, adobeAnimateSprite.atlasProfileOverride);
			characterEntry.PlaybackStartFrame = adobeAnimateSprite.frameIndex;
			characterEntry.PlaybackStartElapsed = adobeAnimateSprite.elapsedTimer;
			bool flag = characterEntry.Character is TowerDefensePlant || characterEntry.Character is TowerDefenseZombie;
			characterEntry.PlaybackRequiresAdvance = flag && orBuild != null && orBuild.TryGetClipRange(adobeAnimateSprite.clip, out var range) && range.Y > range.X;
			adobeAnimateSprite.pause = false;
			adobeAnimateSprite.timeScale = (flag ? 1.0 : 0.0);
			adobeAnimateSprite.RefreshProcessScheduling();
		}
		for (int frame = 0; frame < 36; frame++)
		{
			using Image image = await CaptureImage();
			for (int j = startIndex; j < endIndex; j++)
			{
				CharacterEntry characterEntry2 = _characters[j];
				AdobeAnimateSprite adobeAnimateSprite2 = characterEntry2.Character?.sprite;
				if (!GodotObject.IsInstanceValid(adobeAnimateSprite2))
				{
					passed = false;
					continue;
				}
				int num = CountSignalPixels(image, characterEntry2.ScreenRegion);
				_minimumPlaybackPixels = Math.Min(_minimumPlaybackPixels, num);
				int num2 = Math.Max(12, characterEntry2.BaselineSignalPixels / 5);
				if (num < num2)
				{
					_playbackBlankFrames++;
					if (!characterEntry2.PlaybackBlankDetected)
					{
						characterEntry2.PlaybackBlankDetected = true;
						failures.Add($"{characterEntry2.ScenePath}: real playback lost pixels frame={frame} baseline={characterEntry2.BaselineSignalPixels} current={num}");
					}
					passed = false;
				}
				if (adobeAnimateSprite2.frameIndex != characterEntry2.PlaybackStartFrame || Math.Abs(adobeAnimateSprite2.elapsedTimer - characterEntry2.PlaybackStartElapsed) > 0.001)
				{
					characterEntry2.PlaybackAdvanced = true;
				}
			}
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			if (aggregateRenderStats.FallbackRoots != 0)
			{
				passed = false;
				failures.Add($"playback page {page}: fallback roots={aggregateRenderStats.FallbackRoots} frame={frame}");
				break;
			}
		}
		for (int k = startIndex; k < endIndex; k++)
		{
			CharacterEntry characterEntry3 = _characters[k];
			AdobeAnimateSprite adobeAnimateSprite3 = characterEntry3.Character?.sprite;
			if (GodotObject.IsInstanceValid(adobeAnimateSprite3))
			{
				adobeAnimateSprite3.timeScale = 0.0;
				adobeAnimateSprite3.RefreshProcessScheduling();
			}
			if (GodotObject.IsInstanceValid(adobeAnimateSprite3) && !characterEntry3.PlaybackBlankDetected && (!characterEntry3.PlaybackRequiresAdvance || characterEntry3.PlaybackAdvanced))
			{
				_playbackVerifiedCharacters++;
				continue;
			}
			passed = false;
			if (characterEntry3.PlaybackRequiresAdvance && !characterEntry3.PlaybackAdvanced)
			{
				string value = (GodotObject.IsInstanceValid(adobeAnimateSprite3) ? adobeAnimateSprite3.clip : "<invalid>");
				failures.Add($"{characterEntry3.ScenePath}: multi-frame safe clip '{value}' did not advance during {36} display frames");
			}
		}
		return passed;
	}

	private static void ValidatePageGpuRoots(CharacterEntry entry, List<string> failures, ref bool passed)
	{
		for (int i = 0; i < entry.RenderRoots.Count; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = entry.RenderRoots[i];
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				passed = false;
				failures.Add(entry.ScenePath + ": animation render root was deleted while the catalog scene must remain alive");
				continue;
			}
			AdobeAnimateCrowdRenderState state;
			AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = adobeAnimateSprite.TryBuildCrowdRenderState(out state);
			if (adobeAnimateCrowdRenderStateResult != AdobeAnimateCrowdRenderStateResult.Submitted || state == null || state.Mode != AdobeAnimateCrowdRenderMode.GpuGraph)
			{
				passed = false;
				failures.Add($"{entry.ScenePath}:{adobeAnimateSprite.GetPath()}: runtime GPU Graph unavailable result={adobeAnimateCrowdRenderStateResult} mode={state?.Mode}");
			}
		}
	}

	private async Task<bool> VerifyFrequentMediaAndLayerChanges(List<string> failures)
	{
		SetCatalogPageVisibility(-1);
		_normalZombieScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Reuse);
		_replacementTexture = ResourceLoader.Load<Texture2D>("res://Asset/AtlasSource/DamagePoint/Anime/Character/Plant/Chapter0/Wallnut/DamagePoint/WallnutBody.png", null, ResourceLoader.CacheMode.Reuse);
		if (!GodotObject.IsInstanceValid(_normalZombieScene) || !GodotObject.IsInstanceValid(_replacementTexture))
		{
			failures.Add("mutation resources could not be loaded");
			return false;
		}
		_retainedScenes.Add(_normalZombieScene);
		CreateMutationCharacters();
		await MoveCameraToWorldOrigin(_stressPageOrigin);
		await WaitPhysicsFrames(12);
		ApplyMutationState(0);
		using Image initialImage = await CaptureImage();
		int baselinePixels = CountSignalPixels(initialImage, MutationCaptureRegion);
		int minimumPixels = baselinePixels;
		int minimumChangedPixels = 2147483647;
		int blankFrames = 0;
		int unchangedFrames = 0;
		int maximumFallbackRoots = 0;
		Image previousImage = (Image)initialImage.Duplicate();
		try
		{
			for (int frame = 1; frame <= 120; frame++)
			{
				ApplyMutationState(frame);
				using Image image = await CaptureImage();
				int num = CountSignalPixels(image, MutationCaptureRegion);
				int num2 = CountChangedPixels(image, previousImage, MutationCaptureRegion);
				minimumPixels = Math.Min(minimumPixels, num);
				minimumChangedPixels = Math.Min(minimumChangedPixels, num2);
				if (num < Math.Max(100, baselinePixels / 4))
				{
					blankFrames++;
				}
				if (num2 < 24)
				{
					unchangedFrames++;
				}
				maximumFallbackRoots = Math.Max(maximumFallbackRoots, AdobeAnimateRenderManager.GetAggregateRenderStats().FallbackRoots);
				previousImage.Dispose();
				previousImage = (Image)image.Duplicate();
			}
		}
		finally
		{
			previousImage.Dispose();
		}
		RestoreMutationCharacters();
		bool flag = baselinePixels >= 100 && blankFrames == 0 && unchangedFrames == 0 && maximumFallbackRoots == 0;
		if (!flag)
		{
			failures.Add($"mutation stress failed baseline={baselinePixels} minimum={minimumPixels} minimumChanged={minimumChangedPixels} blankFrames={blankFrames} unchangedFrames={unchangedFrames} fallback={maximumFallbackRoots}");
		}
		GD.Print($"{"ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT"} mutationPassed={flag} characters={_mutationCharacters.Count} frames={120} baselinePixels={baselinePixels} minimumPixels={minimumPixels} minimumChangedPixels={minimumChangedPixels} blankFrames={blankFrames} unchangedFrames={unchangedFrames} maximumFallback={maximumFallbackRoots}");
		return flag;
	}

	private void CreateMutationCharacters()
	{
		for (int i = 0; i < 24; i++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = _normalZombieScene.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
			towerDefenseZombieNormal.Name = $"MutationZombie{i}";
			towerDefenseZombieNormal.inGame = false;
			towerDefenseZombieNormal.skipDestroySet = true;
			towerDefenseZombieNormal.editorPreviewMode = true;
			towerDefenseZombieNormal.ProcessMode = ProcessModeEnum.Disabled;
			towerDefenseZombieNormal.Position = _stressPageOrigin + new Vector2(70f + (float)(i % 6) * 88f, 90f + (float)(i / 6) * 150f);
			towerDefenseZombieNormal.Scale = Vector2.One * 0.72f;
			_world.AddChild(towerDefenseZombieNormal, forceReadableName: false, InternalMode.Disabled);
			ActivateAnimationTree(towerDefenseZombieNormal);
			towerDefenseZombieNormal.sprite.SetAnimation("Walk");
			towerDefenseZombieNormal.sprite.timeScale = 0.0;
			_mutationCharacters.Add(towerDefenseZombieNormal);
		}
	}

	private void ApplyMutationState(int frame)
	{
		int num = frame % 4;
		for (int i = 0; i < _mutationCharacters.Count; i++)
		{
			AdobeAnimateSprite sprite = _mutationCharacters[i].sprite;
			bool open = num == 0 || num == 2;
			sprite.SetFliter(VisibilityLayerName, open);
			switch (num)
			{
			case 0:
				if (!sprite.SetAtlasReplace(ReplacementMediaName, "res://Asset/AtlasSource/DamagePoint/Anime/Character/Plant/Chapter0/Wallnut/DamagePoint/WallnutBody.png"))
				{
					throw new InvalidOperationException("SetAtlasReplace rejected the production media key.");
				}
				break;
			case 1:
				sprite.SetReplace(ReplacementMediaName, _replacementTexture);
				break;
			case 2:
				if (!sprite.SetAtlasReplace(ReplacementMediaName, string.Empty))
				{
					throw new InvalidOperationException("SetAtlasReplace could not clear the production media key.");
				}
				break;
			default:
				sprite.SetReplace(ReplacementMediaName, (Texture2D)null);
				break;
			}
		}
	}

	private void RestoreMutationCharacters()
	{
		for (int i = 0; i < _mutationCharacters.Count; i++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = _mutationCharacters[i];
			if (GodotObject.IsInstanceValid(towerDefenseZombieNormal?.sprite))
			{
				towerDefenseZombieNormal.sprite.SetFliter(VisibilityLayerName, open: true);
				towerDefenseZombieNormal.sprite.SetAtlasReplace(ReplacementMediaName, string.Empty);
			}
		}
	}

	private async Task<bool> VerifyFrequentProductionDeaths(List<string> failures)
	{
		bool passed = true;
		int totalBlankFrames = 0;
		int totalDeathBlankFrames = 0;
		int totalRemainingVictims = 0;
		int totalDeathAnimationsStarted = 0;
		int totalDeathAnimationsProgressed = 0;
		int totalFadesObserved = 0;
		int capacityChanges = 0;
		int maximumFallbackRoots = 0;
		int maximumCompletionFrames = 0;
		int minimumSurvivorPixels = 2147483647;
		ApplyMutationState(0);
		using Image baselineImage = await CaptureImage();
		int baselinePixels = CountSignalPixels(baselineImage, MutationCaptureRegion);
		for (int cycle = 0; cycle < 4; cycle++)
		{
			CreateDeathVictims(cycle);
			await WaitPhysicsFrames(12);
			int capacityBeforeDeath = FindLargestCrowdGpuCapacity(GetTree().Root);
			bool[] deathAnimationStarted = new bool[56];
			bool[] deathAnimationProgressed = new bool[56];
			bool[] fadeObserved = new bool[56];
			for (int i = 0; i < _deathVictims.Count; i++)
			{
				TowerDefenseZombieNormal towerDefenseZombieNormal = _deathVictims[i];
				if (towerDefenseZombieNormal.zombieDeathComponent == null)
				{
					failures.Add($"death cycle {cycle + 1}: victim {i} has no ZombieDeathComponent");
					passed = false;
				}
				else
				{
					towerDefenseZombieNormal.zombieDeathComponent.fadeDuration = 0.35;
					towerDefenseZombieNormal.timeScale = 4.0;
					towerDefenseZombieNormal.sprite.timeScale = 4.0;
					towerDefenseZombieNormal.Die();
				}
			}
			int completionFrames = 240;
			int cycleDeathBlankFrames = 0;
			for (int frame = 0; frame < 240; frame++)
			{
				ApplyMutationState(cycle * 240 + frame);
				using Image image = await CaptureImage();
				int num = CountSignalPixels(image, MutationCaptureRegion);
				int num2 = CountSignalPixels(image, DeathCaptureRegion);
				minimumSurvivorPixels = Math.Min(minimumSurvivorPixels, num);
				if (num < Math.Max(100, baselinePixels / 4))
				{
					totalBlankFrames++;
				}
				int num3 = 0;
				int num4 = 0;
				for (int j = 0; j < _deathVictims.Count; j++)
				{
					TowerDefenseZombieNormal towerDefenseZombieNormal2 = _deathVictims[j];
					if (!GodotObject.IsInstanceValid(towerDefenseZombieNormal2))
					{
						continue;
					}
					num3++;
					if (GodotObject.IsInstanceValid(towerDefenseZombieNormal2.sprite) && (towerDefenseZombieNormal2.zombieDeathComponent?.IsDeathAnimationClip(towerDefenseZombieNormal2.sprite.clip) ?? false))
					{
						deathAnimationStarted[j] = true;
						if (towerDefenseZombieNormal2.sprite.frameIndex != towerDefenseZombieNormal2.sprite.clipRange.X || towerDefenseZombieNormal2.sprite.elapsedTimer > 0.001)
						{
							deathAnimationProgressed[j] = true;
						}
					}
					if (towerDefenseZombieNormal2.Modulate.A < 0.999f)
					{
						fadeObserved[j] = true;
					}
					if (towerDefenseZombieNormal2.Modulate.A > 0.15f)
					{
						num4++;
					}
				}
				if (num4 >= 14 && num2 < 100)
				{
					cycleDeathBlankFrames++;
				}
				maximumFallbackRoots = Math.Max(maximumFallbackRoots, AdobeAnimateRenderManager.GetAggregateRenderStats().FallbackRoots);
				if (num3 == 0)
				{
					completionFrames = frame + 1;
					break;
				}
			}
			await WaitPhysicsFrames(4);
			int num5 = CountTrue(deathAnimationStarted);
			int num6 = CountTrue(deathAnimationProgressed);
			int num7 = CountTrue(fadeObserved);
			totalDeathAnimationsStarted += num5;
			totalDeathAnimationsProgressed += num6;
			totalFadesObserved += num7;
			totalDeathBlankFrames += cycleDeathBlankFrames;
			maximumCompletionFrames = Math.Max(maximumCompletionFrames, completionFrames);
			int num8 = CountValidDeathVictims();
			totalRemainingVictims += num8;
			int num9 = FindLargestCrowdGpuCapacity(GetTree().Root);
			if (num9 != capacityBeforeDeath)
			{
				capacityChanges++;
			}
			int num10 = CountValidCatalogCharacters();
			if (num5 != 56 || num6 != 56 || num7 != 56 || cycleDeathBlankFrames != 0 || num8 != 0 || num10 != _characters.Count)
			{
				failures.Add($"death cycle {cycle + 1}: started={num5}/{56} progressed={num6}/{56} fades={num7}/{56} deathBlankFrames={cycleDeathBlankFrames} remainingVictims={num8} catalog={num10}/{_characters.Count}");
				passed = false;
			}
			AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			if (aggregateRenderStats.FallbackRoots != 0)
			{
				failures.Add($"death cycle {cycle + 1}: fallback roots={aggregateRenderStats.FallbackRoots}");
				passed = false;
			}
			GD.Print($"{"ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT"} deathCycle={cycle + 1}/{4} started={num5}/{56} progressed={num6}/{56} fades={num7}/{56} completionFrames={completionFrames} deathBlankFrames={cycleDeathBlankFrames} remaining={num8} catalog={num10}/{_characters.Count} beforeCapacity={capacityBeforeDeath} afterCapacity={num9} crowdRoots={aggregateRenderStats.CrowdRoots} fallbackRoots={aggregateRenderStats.FallbackRoots}");
			_deathVictims.Clear();
		}
		int num11 = 224;
		passed = passed && totalBlankFrames == 0 && totalDeathBlankFrames == 0 && totalRemainingVictims == 0 && totalDeathAnimationsStarted == num11 && totalDeathAnimationsProgressed == num11 && totalFadesObserved == num11 && capacityChanges == 0 && maximumFallbackRoots == 0;
		if (!passed)
		{
			failures.Add($"death stress failed started={totalDeathAnimationsStarted}/{num11} progressed={totalDeathAnimationsProgressed}/{num11} fades={totalFadesObserved}/{num11} survivorBlankFrames={totalBlankFrames} deathBlankFrames={totalDeathBlankFrames} remaining={totalRemainingVictims} capacityChanges={capacityChanges} maximumFallback={maximumFallbackRoots}");
		}
		GD.Print($"{"ADOBE_ANIMATE_ALL_CHARACTER_SINGLE_SCENE_STRESS_RESULT"} deathPassed={passed} cycles={4} victims={num11} animationsStarted={totalDeathAnimationsStarted}/{num11} animationsProgressed={totalDeathAnimationsProgressed}/{num11} fadesObserved={totalFadesObserved}/{num11} maximumCompletionFrames={maximumCompletionFrames} baselinePixels={baselinePixels} minimumSurvivorPixels={minimumSurvivorPixels} survivorBlankFrames={totalBlankFrames} deathBlankFrames={totalDeathBlankFrames} remainingVictims={totalRemainingVictims} capacityChanges={capacityChanges} maximumFallback={maximumFallbackRoots}");
		return passed;
	}

	private void CreateDeathVictims(int cycle)
	{
		for (int i = 0; i < 56; i++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = _normalZombieScene.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
			towerDefenseZombieNormal.Name = $"DeathVictim{cycle}_{i}";
			towerDefenseZombieNormal.inGame = false;
			towerDefenseZombieNormal.skipDestroySet = true;
			towerDefenseZombieNormal.ProcessMode = ProcessModeEnum.Always;
			towerDefenseZombieNormal.Position = _stressPageOrigin + new Vector2(620f + (float)(i % 8) * 68f, 80f + (float)(i / 8) * 82f);
			towerDefenseZombieNormal.Scale = Vector2.One * 0.58f;
			_world.AddChild(towerDefenseZombieNormal, forceReadableName: false, InternalMode.Disabled);
			ActivateAnimationTree(towerDefenseZombieNormal);
			towerDefenseZombieNormal.sprite.SetAnimation("Walk");
			towerDefenseZombieNormal.sprite.timeScale = 0.0;
			_deathVictims.Add(towerDefenseZombieNormal);
		}
	}

	private static void ActivateAnimationTree(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			if (node is AdobeAnimateSprite adobeAnimateSprite)
			{
				adobeAnimateSprite.ProcessMode = ProcessModeEnum.Always;
				adobeAnimateSprite.pause = false;
				adobeAnimateSprite.Visible = true;
				adobeAnimateSprite.RefreshProcessScheduling();
			}
			for (int i = 0; i < node.GetChildCount(); i++)
			{
				ActivateAnimationTree(node.GetChild(i));
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

	private int CountValidDeathVictims()
	{
		int num = 0;
		for (int i = 0; i < _deathVictims.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_deathVictims[i]))
			{
				num++;
			}
		}
		return num;
	}

	private static int CountTrue(bool[] values)
	{
		int num = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i])
			{
				num++;
			}
		}
		return num;
	}

	private int CountValidCatalogCharacters()
	{
		int num = 0;
		for (int i = 0; i < _characters.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_characters[i].Character) && _characters[i].Character.IsInsideTree())
			{
				num++;
			}
		}
		return num;
	}

	private int CountCharacters<TCharacter>() where TCharacter : TowerDefenseCharacter
	{
		int num = 0;
		for (int i = 0; i < _characters.Count; i++)
		{
			if (_characters[i].Character is TCharacter)
			{
				num++;
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

	private void SetPageRenderRootsVisible(int page, bool visible, List<string> failures, ref bool passed)
	{
		for (int i = page * 18; i < Math.Min(_characters.Count, (page + 1) * 18); i++)
		{
			CharacterEntry characterEntry = _characters[i];
			for (int j = 0; j < characterEntry.RenderRoots.Count; j++)
			{
				AdobeAnimateSprite adobeAnimateSprite = characterEntry.RenderRoots[j];
				if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					passed = false;
					failures.Add($"{characterEntry.ScenePath}: render root deleted before visible={visible}");
				}
				else
				{
					adobeAnimateSprite.Visible = visible;
				}
			}
		}
	}

	private async Task MoveCameraToPage(int page)
	{
		SetCatalogPageVisibility(page);
		await MoveCameraToWorldOrigin(new Vector2((float)page * 1500f, 0f));
	}

	private void SetCatalogPageVisibility(int page)
	{
		for (int i = 0; i < _characters.Count; i++)
		{
			CharacterEntry characterEntry = _characters[i];
			if (GodotObject.IsInstanceValid(characterEntry.Holder))
			{
				characterEntry.Holder.Visible = characterEntry.Page == page;
			}
		}
	}

	private async Task MoveCameraToWorldOrigin(Vector2 origin)
	{
		_camera.Position = origin + new Vector2(600f, 350f);
		AdobeAnimateRuntimeManager.NotifyViewportTransformChanged();
		await WaitPhysicsFrames(5);
		await WaitProcessFrames(3);
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
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		return GetViewport().GetTexture().GetImage();
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

	private static int FindLargestCrowdGpuCapacity(Node node)
	{
		int num = 0;
		if (node is MultiMeshInstance2D multiMeshInstance2D && multiMeshInstance2D.Name.ToString().StartsWith("AdobeAnimateCrowdZ_", StringComparison.Ordinal) && GodotObject.IsInstanceValid(multiMeshInstance2D.Multimesh))
		{
			num = multiMeshInstance2D.Multimesh.InstanceCount;
		}
		for (int i = 0; i < node.GetChildCount(); i++)
		{
			num = Math.Max(num, FindLargestCrowdGpuCapacity(node.GetChild(i)));
		}
		return num;
	}

	private static void PrintFailures(List<string> failures)
	{
		int num = Math.Min(200, failures.Count);
		for (int i = 0; i < num; i++)
		{
			GD.PrintErr("[AllCharacterSingleSceneFailure] " + failures[i]);
		}
		if (failures.Count > num)
		{
			GD.PrintErr($"[AllCharacterSingleSceneFailure] omitted={failures.Count - num}");
		}
	}

	private void RestoreRuntime()
	{
		for (int i = 0; i < _deathVictims.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_deathVictims[i]))
			{
				_deathVictims[i].QueueFree();
			}
		}
		for (int j = 0; j < _mutationCharacters.Count; j++)
		{
			if (GodotObject.IsInstanceValid(_mutationCharacters[j]))
			{
				_mutationCharacters[j].QueueFree();
			}
		}
		if (GodotObject.IsInstanceValid(Global.Instance))
		{
			Global.Instance.adobeAnimateRenderBackend = _originalBackend;
		}
		AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
		Engine.MaxFps = _originalMaxFps;
	}
}
