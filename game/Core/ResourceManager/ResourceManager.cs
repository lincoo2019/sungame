using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;
using ZLogger;

[GlobalClass]
public partial class ResourceManager : Node2D
{
	private sealed class FullGameplayCoreSnapshot
	{
		public readonly System.Collections.Generic.Dictionary<string, Resource> Survivals = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Dictionary> Levels = new System.Collections.Generic.Dictionary<string, Dictionary>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Maps = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Bgms = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, string> BgmPaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public readonly System.Collections.Generic.Dictionary<string, Resource> ProjectileConfigs = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Audios = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Talks = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Tutorials = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Collectables = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Shovels = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Mowers = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> Shops = new System.Collections.Generic.Dictionary<string, Resource>();

		public readonly System.Collections.Generic.Dictionary<string, Variant> DailyLevelData = new System.Collections.Generic.Dictionary<string, Variant>();

		public readonly System.Collections.Generic.Dictionary<string, Resource> TowerDefenseLevelEvents = new System.Collections.Generic.Dictionary<string, Resource>();

		public FullGameplayRegistryRoots RegistryRoots;
	}

	private sealed class FullGameplayRootSnapshot
	{
		public readonly System.Collections.Generic.Dictionary<string, Resource> CharacterScenes;

		public readonly System.Collections.Generic.Dictionary<string, Resource> Sprites;

		public readonly System.Collections.Generic.Dictionary<string, Resource> Packets;

		public readonly System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> PacketBanks;

		public FullGameplayRootSnapshot(System.Collections.Generic.Dictionary<string, Resource> characterScenes, System.Collections.Generic.Dictionary<string, Resource> sprites, System.Collections.Generic.Dictionary<string, Resource> packets, System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> packetBanks)
		{
			CharacterScenes = characterScenes;
			Sprites = sprites;
			Packets = packets;
			PacketBanks = packetBanks;
		}
	}

	public enum GameplayAtlasLoadState
	{
		NotStarted,
		LoadingVisual,
		LoadingPose,
		Ready,
		Failed
	}

	public delegate void LoadOverEventHandler();

	public delegate void LoadFailedEventHandler(string error);

	public delegate void LoadPercentageEventHandler(double persontage, string stepName, string resourceName, int resourceIndex, int resourceTotal);

	private static readonly ILogger _logger = Log.CreateLogger<ResourceManager>();

	private static readonly Random _random = new Random();

	private static Json _survivalResource;

	private static Json _levelResource;

	private static Json _mapResource;

	private static Json _bgmResource;

	private static Json _projectileResource;

	private static Json _audioResource;

	private static Json _characterResource;

	private static Json _fullGameplayResourceManifest;

	private static Json _talkResource;

	private static Json _tutorialResource;

	private static Json _packetBankResource;

	private static Json _collectableResource;

	private static Json _shovelResource;

	private static Json _mowerResource;

	private static Json _shopResource;

	private static Json _dailyLevelAward;

	private System.Collections.Generic.Dictionary<string, string> _bgmPaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private readonly List<FullGameplayRootLoadMetric> _rootLoadMetrics = new List<FullGameplayRootLoadMetric>();

	private readonly TaskCompletionSource<bool> _fullReadyCompletion = CreateReadyCompletion();

	private System.Collections.Generic.Dictionary<string, string> _characterSpritePaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private System.Collections.Generic.Dictionary<string, string> _characterScenePaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private System.Collections.Generic.Dictionary<string, string> _packetPaths = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private System.Collections.Generic.Dictionary<string, string> _characterNameByPacket = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private TextureLayered _adobeAnimateVisualTextureArray;

	private TextureLayered _adobeAnimateGpuPoseTextureArray;

	private GameplayAtlasLoadState _gameplayAtlasLoadState;

	private GameplayResourceLoadState _gameplayResourceLoadState;

	private string _gameplayAtlasLoadError = string.Empty;

	private string _fullGameplayResourceLoadError = string.Empty;

	private ulong _gameplayAtlasLoadStartedUsec;

	private ulong _gameplayAtlasVisualReadyUsec;

	private ulong _gameplayAtlasPoseReadyUsec;

	private ulong _fullLoadStartedUsec;

	private bool _loadStarted;

	private bool _bgmResourcesLoaded;

	private int _mainThreadId;

	private int _lateCharacterResourceLoadCount;

	private int _packetBankMissingCount;

	private double _coreMilliseconds;

	private double _characterSceneMilliseconds;

	private double _spriteMilliseconds;

	private double _packetMilliseconds;

	private static Json SURVIVAL_RESOURCE => _survivalResource ?? (_survivalResource = GD.Load<Json>("res://Asset/Config/Survival/SurvivalResource.json"));

	private static Json MAP_RESOURCE => _mapResource ?? (_mapResource = GD.Load<Json>("res://Asset/Config/Map/MapResource.json"));

	private static Json BGM_RESOURCE => _bgmResource ?? (_bgmResource = GD.Load<Json>("res://Asset/Config/BGM/BGMResource.json"));

	private static Json PROJECTILE_RESOURCE => _projectileResource ?? (_projectileResource = GD.Load<Json>("res://Asset/Config/Projectile/ProjectileResource.json"));

	private static Json AUDIO_RESOURCE => _audioResource ?? (_audioResource = GD.Load<Json>("res://Asset/Config/Audio/AudioResource.json"));

	private static Json CHARACTER_RESOURCE => _characterResource ?? (_characterResource = GD.Load<Json>("res://Asset/Config/Character/CharacterResource.json"));

	private static Json FULL_GAMEPLAY_RESOURCE_MANIFEST => _fullGameplayResourceManifest ?? (_fullGameplayResourceManifest = GD.Load<Json>("res://Asset/Config/Character/FullGameplayResources.json"));

	private static Json TALK_RESOURCE => _talkResource ?? (_talkResource = GD.Load<Json>("res://Asset/Config/Npc/TalkResource.json"));

	private static Json TUTORIAL_RESOURCE => _tutorialResource ?? (_tutorialResource = GD.Load<Json>("res://Asset/Config/Tutorial/TutorialResource.json"));

	private static Json PACKET_BANK_RESOURCE => _packetBankResource ?? (_packetBankResource = GD.Load<Json>("res://Asset/Config/PacketBank/PacketBankResource.json"));

	private static Json COLLECTABLE_RESOURCE => _collectableResource ?? (_collectableResource = GD.Load<Json>("res://Asset/Config/Collectable/CollectableResource.json"));

	private static Json SHOVEL_RESOURCE => _shovelResource ?? (_shovelResource = GD.Load<Json>("res://Asset/Config/Shovel/ShovelResource.json"));

	private static Json MOWER_RESOURCE => _mowerResource ?? (_mowerResource = GD.Load<Json>("res://Asset/Config/Mower/MowerResource.json"));

	private static Json SHOP_RESOURCE => _shopResource ?? (_shopResource = GD.Load<Json>("res://Asset/Config/Shop/ShopResource.json"));

	public ulong FullGameplayLoadStartedUsec => _fullLoadStartedUsec;

	public ulong FullGameplayResourcesReadyUsec { get; private set; }

	public System.Collections.Generic.Dictionary<string, Resource> SURVIVALS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public static Json LEVEL_RESOURCE => _levelResource ?? (_levelResource = GD.Load<Json>("res://Asset/Config/Level/LevelResource.json"));

	public System.Collections.Generic.Dictionary<string, Dictionary> LEVELS { get; private set; } = new System.Collections.Generic.Dictionary<string, Dictionary>();

	public System.Collections.Generic.Dictionary<string, Resource> MAPS { get; set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> BGMS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> PROJECTILE_CONFIG { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> AUDIOS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> CHARCTAER_SPRITE { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

	public System.Collections.Generic.Dictionary<string, Resource> TOWERDEFENSE_CHARCATERS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

	public System.Collections.Generic.Dictionary<string, Resource> TOWERDEFENSE_PACKETS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

	public System.Collections.Generic.Dictionary<string, Resource> TALKS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> TUTORIALS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> TOWERDEFENSE_PACKETBANKS { get; private set; } = new System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData>(StringComparer.OrdinalIgnoreCase);

	public System.Collections.Generic.Dictionary<string, Resource> COLLECTABLES { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> SHOVELS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> MOWERS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Resource> SHOPS { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public System.Collections.Generic.Dictionary<string, Variant> DAILY_LEVEL_DATA { get; private set; } = new System.Collections.Generic.Dictionary<string, Variant>();

	public System.Collections.Generic.Dictionary<string, Resource> TOWERDEFENSE_LEVEL_EVENT { get; private set; } = new System.Collections.Generic.Dictionary<string, Resource>();

	public static Json DAILY_LEVEL_AWARD => _dailyLevelAward ?? (_dailyLevelAward = GD.Load<Json>("res://Asset/Config/DailyChallenge/DailyChallengeAward.json"));

	public static ResourceManager Instance { get; private set; }

	public int stepMax { get; private set; } = 18;

	public string stepName { get; private set; } = "LOAD_STARTUP";

	public int currentStep { get; private set; }

	public GameplayAtlasLoadState CurrentGameplayAtlasLoadState => _gameplayAtlasLoadState;

	public bool AreGameplayAtlasesReady
	{
		get
		{
			if (_gameplayAtlasLoadState == GameplayAtlasLoadState.Ready && GodotObject.IsInstanceValid(_adobeAnimateVisualTextureArray))
			{
				return GodotObject.IsInstanceValid(_adobeAnimateGpuPoseTextureArray);
			}
			return false;
		}
	}

	public bool HasGameplayAtlasLoadFailed => _gameplayAtlasLoadState == GameplayAtlasLoadState.Failed;

	public string GameplayAtlasLoadError => _gameplayAtlasLoadError;

	public GameplayResourceLoadState CurrentGameplayResourceLoadState => _gameplayResourceLoadState;

	public bool AreFullGameplayResourcesReady => _gameplayResourceLoadState == GameplayResourceLoadState.Ready;

	public string FullGameplayResourceLoadError => _fullGameplayResourceLoadError;

	public int LateCharacterResourceLoadCount => Volatile.Read(in _lateCharacterResourceLoadCount);

	public FullGameplayResourceLoadMetricsSnapshot FullGameplayResourceLoadMetrics { get; private set; } = FullGameplayResourceLoadMetricsSnapshot.CreateEmpty();

	public event LoadOverEventHandler OnLoadOver;

	public event LoadFailedEventHandler OnLoadFailed;

	public event LoadPercentageEventHandler OnLoadPercentage;

	public override void _Ready()
	{
		Instance = this;
		_mainThreadId = System.Environment.CurrentManagedThreadId;
	}

	public override void _Process(double delta)
	{
		PumpGameplayAtlasPreload();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void BeginLoad()
	{
		RequireMainThread("BeginLoad");
		if (_gameplayResourceLoadState == GameplayResourceLoadState.Ready)
		{
			NotifyLoadOverSubscribers();
		}
		else if (_gameplayResourceLoadState == GameplayResourceLoadState.Failed)
		{
			NotifyLoadFailedSubscribers(_fullGameplayResourceLoadError);
		}
		else if (!_loadStarted)
		{
			_loadStarted = true;
			_fullLoadStartedUsec = Time.GetTicksUsec();
			_gameplayResourceLoadState = GameplayResourceLoadState.LoadingCore;
			stepName = "LOAD_CONFIG";
			EmitLoadPercentage(0.0, stepName);
			LoadFullGameplayResourcesAsync();
		}
	}

	public async Task EnsureFullGameplayResourcesReadyAsync()
	{
		RequireMainThread("EnsureFullGameplayResourcesReadyAsync");
		if (_gameplayResourceLoadState == GameplayResourceLoadState.NotStarted)
		{
			BeginLoad();
		}
		await _fullReadyCompletion.Task;
	}

	public void RequireFullGameplayResourcesReady(string context)
	{
		if (_gameplayResourceLoadState == GameplayResourceLoadState.Ready)
		{
			return;
		}
		string text = ((_gameplayResourceLoadState == GameplayResourceLoadState.Failed) ? _fullGameplayResourceLoadError : $"state={_gameplayResourceLoadState}");
		throw new InvalidOperationException("Full gameplay resources are required by " + context + ": " + text);
	}

	private async Task LoadFullGameplayResourcesAsync()
	{
		_ = 2;
		try
		{
			BeginGameplayAtlasPreload();
			ulong ticksUsec = Time.GetTicksUsec();
			StartupLoadDiagnostics.Mark("core.config.begin");
			WarmConfigResourcesOnMainThread();
			StartupLoadDiagnostics.Mark("core.config.end");
			StartupLoadDiagnostics.Mark("core.registries.begin");
			InitializeRegistriesOnMainThread();
			StartupLoadDiagnostics.Mark("core.registries.end");
			StartupLoadDiagnostics.Mark("core.resources.begin");
			FullGameplayCoreSnapshot coreSnapshot = LoadCoreResourcesOnMainThread();
			StartupLoadDiagnostics.Mark("core.resources.end");
			_coreMilliseconds = (double)(Time.GetTicksUsec() - ticksUsec) / 1000.0;
			await EnsureGameplayAtlasesReadyAsync();
			await YieldAfterStageAsync();
			_gameplayResourceLoadState = GameplayResourceLoadState.LoadingGameplay;
			PublishReady(coreSnapshot, await LoadFullGameplayRootsOnMainThreadAsync(coreSnapshot.RegistryRoots));
		}
		catch (Exception exception)
		{
			FailFullGameplayLoad(exception);
		}
	}

	private async Task YieldAfterStageAsync()
	{
		if (IsInsideTree())
		{
			if (string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			else
			{
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			}
		}
	}

	private static void WarmConfigResourcesOnMainThread()
	{
		_ = SURVIVAL_RESOURCE;
		_ = LEVEL_RESOURCE;
		_ = MAP_RESOURCE;
		_ = BGM_RESOURCE;
		_ = PROJECTILE_RESOURCE;
		_ = AUDIO_RESOURCE;
		_ = CHARACTER_RESOURCE;
		_ = FULL_GAMEPLAY_RESOURCE_MANIFEST;
		_ = TALK_RESOURCE;
		_ = TUTORIAL_RESOURCE;
		_ = PACKET_BANK_RESOURCE;
		_ = COLLECTABLE_RESOURCE;
		_ = SHOVEL_RESOURCE;
		_ = MOWER_RESOURCE;
		_ = SHOP_RESOURCE;
		_ = DAILY_LEVEL_AWARD;
	}

	private static void InitializeRegistriesOnMainThread()
	{
		TowerDefenseProjectileRegistry.Init();
		TowerDefenseArmorRegistry.Init();
		TowerDefenseBattleRegistry.Init();
		CommandRegistry.Init();
	}

	private FullGameplayCoreSnapshot LoadCoreResourcesOnMainThread()
	{
		FullGameplayCoreSnapshot fullGameplayCoreSnapshot = new FullGameplayCoreSnapshot();
		currentStep = 0;
		LoadResourceDictionary(MAP_RESOURCE, fullGameplayCoreSnapshot.Maps, "CoreMap", "LOAD_MAP");
		currentStep = 1;
		IndexBgmPaths(fullGameplayCoreSnapshot.BgmPaths);
		currentStep = 2;
		LoadResourceDictionary(PROJECTILE_RESOURCE, fullGameplayCoreSnapshot.ProjectileConfigs, "CoreProjectile", "LOAD_PROJECTILE");
		currentStep = 3;
		EmitLoadPercentageForCurrentStep("LOAD_ARMOR");
		currentStep = 4;
		LoadResourceDictionary(AUDIO_RESOURCE, fullGameplayCoreSnapshot.Audios, "CoreAudio", "LOAD_AUDIO");
		currentStep = 5;
		EmitLoadPercentageForCurrentStep("LOAD_CHARACTER_INDEX");
		fullGameplayCoreSnapshot.RegistryRoots = FullGameplayResourceManifest.GetRegistryRoots(CHARACTER_RESOURCE);
		currentStep = 6;
		LoadResourceDictionary(TALK_RESOURCE, fullGameplayCoreSnapshot.Talks, "CoreTalk", "LOAD_TALK");
		currentStep = 7;
		LoadResourceDictionary(TUTORIAL_RESOURCE, fullGameplayCoreSnapshot.Tutorials, "CoreTutorial", "LOAD_TUTORIAL");
		currentStep = 8;
		EmitLoadPercentageForCurrentStep("LOAD_PACKETBANK");
		currentStep = 9;
		LoadResourceDictionary(COLLECTABLE_RESOURCE, fullGameplayCoreSnapshot.Collectables, "CoreCollectable", "LOAD_COLLECTABLE");
		currentStep = 10;
		LoadResourceDictionary(SHOVEL_RESOURCE, fullGameplayCoreSnapshot.Shovels, "CoreShovel", "LOAD_SHOVEL");
		currentStep = 11;
		LoadResourceDictionary(MOWER_RESOURCE, fullGameplayCoreSnapshot.Mowers, "CoreMower", "LOAD_MOWER");
		currentStep = 12;
		LoadResourceDictionary(SHOP_RESOURCE, fullGameplayCoreSnapshot.Shops, "CoreShop", "LOAD_SHOP");
		currentStep = 13;
		LoadResourceDictionary(SURVIVAL_RESOURCE, fullGameplayCoreSnapshot.Survivals, "CoreSurvival", "LOAD_LEVEL");
		LoadLevelData(fullGameplayCoreSnapshot.Levels);
		return fullGameplayCoreSnapshot;
	}

	private void IndexBgmPaths(System.Collections.Generic.Dictionary<string, string> destination)
	{
		stepName = "LOAD_BGM";
		EmitLoadPercentageForCurrentStep(stepName);
		destination.Clear();
		Dictionary dictionary = RequireJsonDictionary(BGM_RESOURCE, "BGMResource.json");
		foreach (Variant key in dictionary.Keys)
		{
			destination[key.AsString()] = dictionary[key].AsString();
		}
	}

	private void LoadResourceDictionary(Json registry, System.Collections.Generic.Dictionary<string, Resource> destination, string category, string loadingStep)
	{
		stepName = loadingStep;
		Dictionary dictionary = RequireJsonDictionary(registry, loadingStep);
		destination.Clear();
		int num = 0;
		foreach (Variant key in dictionary.Keys)
		{
			num++;
			string text = key.AsString();
			EmitLoadResourceProgress(currentStep, loadingStep, text, num, dictionary.Count);
			double actualLoadMilliseconds;
			Resource resource = LoadBuiltInRoot(dictionary[key].AsString(), category, text, out actualLoadMilliseconds);
			ulong ticksUsec = Time.GetTicksUsec();
			destination[text] = resource;
			double publishMilliseconds = (double)(Time.GetTicksUsec() - ticksUsec) / 1000.0;
			_rootLoadMetrics.Add(new FullGameplayRootLoadMetric(category, resource.ResourcePath, actualLoadMilliseconds, publishMilliseconds));
		}
	}

	private void LoadLevelData(System.Collections.Generic.Dictionary<string, Dictionary> destination)
	{
		destination.Clear();
		Dictionary dictionary = RequireJsonDictionary(LEVEL_RESOURCE, "LevelResource.json");
		int num = 0;
		foreach (Variant key in dictionary.Keys)
		{
			num++;
			string text = key.AsString();
			EmitLoadResourceProgress(currentStep, "LOAD_LEVEL", text, num, dictionary.Count);
			destination[text] = dictionary[key].AsGodotDictionary().Duplicate(deep: true);
		}
	}

	private async Task<FullGameplayRootSnapshot> LoadFullGameplayRootsOnMainThreadAsync(FullGameplayRegistryRoots roots)
	{
		FullGameplayResourceManifestData manifest = FullGameplayResourceManifest.ReadAndValidate(FULL_GAMEPLAY_RESOURCE_MANIFEST, CHARACTER_RESOURCE, PACKET_BANK_RESOURCE);
		List<string> list = FullGameplayResourceManifest.ValidatePacketBankClosure(PACKET_BANK_RESOURCE, roots);
		_packetBankMissingCount = list.Count;
		if (list.Count > 0)
		{
			throw new InvalidOperationException("PacketBank closure validation failed: " + string.Join(" | ", list));
		}
		System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> packetBanks = BuildExpandedPacketBanks();
		currentStep = 14;
		stepName = "LOAD_CHARACTER_SCENE_ROOTS";
		ulong ticksUsec = Time.GetTicksUsec();
		StartupLoadDiagnostics.Mark("characters.begin");
		StartupLoadDiagnostics.MeasureCharacterDependencies(manifest.CharacterSceneRoots);
		System.Collections.Generic.Dictionary<string, Resource> loadedRoots = LoadUniquePackedSceneRoots(manifest.CharacterSceneRoots, "CharacterScene");
		System.Collections.Generic.Dictionary<string, Resource> characterScenes = BindPackedSceneNames(new System.Collections.Generic.Dictionary<string, string>(roots.ScenePathByCharacter, StringComparer.OrdinalIgnoreCase), loadedRoots, "CharacterScene");
		_characterSceneMilliseconds = (double)(Time.GetTicksUsec() - ticksUsec) / 1000.0;
		StartupLoadDiagnostics.Mark("characters.end");
		await YieldAfterStageAsync();
		currentStep = 15;
		stepName = "LOAD_CHARACTER_SPRITE_ROOTS";
		ulong ticksUsec2 = Time.GetTicksUsec();
		System.Collections.Generic.Dictionary<string, Resource> independentRoots = LoadUniquePackedSceneRoots(manifest.IndependentSpriteRoots, "IndependentSprite");
		System.Collections.Generic.Dictionary<string, Resource> sprites = BindSpriteNamesFromCache(new System.Collections.Generic.Dictionary<string, string>(roots.SpritePathByCharacter, StringComparer.OrdinalIgnoreCase), independentRoots);
		_spriteMilliseconds = (double)(Time.GetTicksUsec() - ticksUsec2) / 1000.0;
		await YieldAfterStageAsync();
		currentStep = 16;
		stepName = "LOAD_PACKET_ROOTS";
		ulong ticksUsec3 = Time.GetTicksUsec();
		System.Collections.Generic.Dictionary<string, Resource> loadedRoots2 = LoadUniqueResourceRoots(manifest.PacketRoots, "Packet");
		System.Collections.Generic.Dictionary<string, Resource> packets = BindResourceNames(new System.Collections.Generic.Dictionary<string, string>(roots.PacketPathByName, StringComparer.OrdinalIgnoreCase), loadedRoots2, "Packet");
		_packetMilliseconds = (double)(Time.GetTicksUsec() - ticksUsec3) / 1000.0;
		await YieldAfterStageAsync();
		currentStep = 17;
		stepName = "VALIDATE_FULL_GAMEPLAY_RESOURCES";
		EmitLoadPercentageForCurrentStep(stepName);
		ValidateCompletePublication(roots, manifest, characterScenes, sprites, packets, packetBanks);
		return new FullGameplayRootSnapshot(characterScenes, sprites, packets, packetBanks);
	}

	private System.Collections.Generic.Dictionary<string, Resource> LoadUniquePackedSceneRoots(IReadOnlyList<string> paths, string category)
	{
		System.Collections.Generic.Dictionary<string, Resource> dictionary = LoadUniqueResourceRoots(paths, category);
		foreach (var (value, resource2) in dictionary)
		{
			if (!(resource2 is PackedScene))
			{
				throw new InvalidOperationException($"Full gameplay root has the wrong type: category={category}, path={value}, actual={resource2.GetType().FullName}, expected=PackedScene");
			}
		}
		return dictionary;
	}

	private System.Collections.Generic.Dictionary<string, Resource> LoadUniqueResourceRoots(IReadOnlyList<string> paths, string category)
	{
		System.Collections.Generic.Dictionary<string, Resource> dictionary = new System.Collections.Generic.Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < paths.Count; i++)
		{
			string text = paths[i];
			EmitLoadResourceProgress(currentStep, stepName, text, i + 1, paths.Count);
			double actualLoadMilliseconds;
			Resource value = LoadBuiltInRoot(text, category, text, out actualLoadMilliseconds);
			ulong ticksUsec = Time.GetTicksUsec();
			dictionary[text] = value;
			double publishMilliseconds = (double)(Time.GetTicksUsec() - ticksUsec) / 1000.0;
			_rootLoadMetrics.Add(new FullGameplayRootLoadMetric(category, text, actualLoadMilliseconds, publishMilliseconds));
		}
		return dictionary;
	}

	private static System.Collections.Generic.Dictionary<string, Resource> BindResourceNames(System.Collections.Generic.Dictionary<string, string> configuredPaths, System.Collections.Generic.Dictionary<string, Resource> loadedRoots, string category)
	{
		System.Collections.Generic.Dictionary<string, Resource> dictionary = new System.Collections.Generic.Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
		foreach (var (text3, text4) in configuredPaths)
		{
			if (!loadedRoots.TryGetValue(text4, out var value) || !GodotObject.IsInstanceValid(value))
			{
				throw new InvalidOperationException($"Loaded root cannot satisfy binding: category={category}, key={text3}, path={text4}");
			}
			dictionary[text3] = value;
		}
		return dictionary;
	}

	private static System.Collections.Generic.Dictionary<string, Resource> BindPackedSceneNames(System.Collections.Generic.Dictionary<string, string> configuredPaths, System.Collections.Generic.Dictionary<string, Resource> loadedRoots, string category)
	{
		System.Collections.Generic.Dictionary<string, Resource> dictionary = BindResourceNames(configuredPaths, loadedRoots, category);
		foreach (var (value, resource2) in dictionary)
		{
			if (!(resource2 is PackedScene))
			{
				throw new InvalidOperationException($"PackedScene binding has the wrong type: category={category}, key={value}, actual={resource2.GetType().FullName}");
			}
		}
		return dictionary;
	}

	private System.Collections.Generic.Dictionary<string, Resource> BindSpriteNamesFromCache(System.Collections.Generic.Dictionary<string, string> configuredPaths, System.Collections.Generic.Dictionary<string, Resource> independentRoots)
	{
		System.Collections.Generic.Dictionary<string, Resource> dictionary = new System.Collections.Generic.Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
		System.Collections.Generic.Dictionary<string, Resource> dictionary2 = new System.Collections.Generic.Dictionary<string, Resource>(independentRoots, StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, string> configuredPath in configuredPaths)
		{
			configuredPath.Deconstruct(out var key, out var value);
			string text = key;
			string text2 = value;
			Resource value2;
			bool flag = dictionary2.TryGetValue(text2, out value2);
			if (!flag && ResourceLoader.HasCached(text2))
			{
				value2 = ResourceLoader.GetCachedRef(text2);
				flag = GodotObject.IsInstanceValid(value2);
				if (flag)
				{
					dictionary2[text2] = value2;
				}
			}
			if (!flag)
			{
				if (hashSet.Add(text2))
				{
					_rootLoadMetrics.Add(new FullGameplayRootLoadMetric("SpriteCacheBinding", text2, 0.0, 0.0, 0, 1));
				}
				throw new InvalidOperationException("Character Sprite was not brought into the main-thread cache by its declared scene root. The manifest is stale or the dependency graph is broken: key=" + text + ", path=" + text2);
			}
			if (!(value2 is PackedScene))
			{
				throw new InvalidOperationException($"Character Sprite cache binding has the wrong type: key={text}, path={text2}, actual={value2.GetType().FullName}, expected=PackedScene");
			}
			if (hashSet.Add(text2))
			{
				_rootLoadMetrics.Add(new FullGameplayRootLoadMetric("SpriteCacheBinding", text2, 0.0, 0.0, 1));
			}
			dictionary[text] = value2;
		}
		return dictionary;
	}

	private static System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> BuildExpandedPacketBanks()
	{
		Dictionary dictionary = RequireJsonDictionary(PACKET_BANK_RESOURCE, "PacketBankResource.json");
		System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> dictionary2 = new System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData>(StringComparer.OrdinalIgnoreCase);
		foreach (Variant key in dictionary.Keys)
		{
			BuildExpandedPacketBank(key.AsString(), dictionary, dictionary2, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		}
		return dictionary2;
	}

	private static TowerDefensePacketBankData BuildExpandedPacketBank(string bankName, Dictionary rawBanks, System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> cache, HashSet<string> visiting)
	{
		if (cache.TryGetValue(bankName, out var value))
		{
			return value;
		}
		if (!rawBanks.TryGetValue(bankName, out var value2) || value2.VariantType != Variant.Type.Dictionary)
		{
			throw new InvalidOperationException("PacketBank Include references missing bank: " + bankName);
		}
		if (!visiting.Add(bankName))
		{
			throw new InvalidOperationException("PacketBank Include cycle detected while expanding " + bankName + ".");
		}
		Dictionary dictionary = value2.AsGodotDictionary();
		TowerDefensePacketBankData towerDefensePacketBankData = new TowerDefensePacketBankData();
		if (dictionary.TryGetValue("Category", out var value3) && value3.VariantType == Variant.Type.Dictionary)
		{
			towerDefensePacketBankData.category = value3.AsGodotDictionary().Duplicate(deep: true);
		}
		if (dictionary.TryGetValue("Include", out var value4) && value4.VariantType == Variant.Type.Array)
		{
			foreach (Variant item in value4.AsGodotArray())
			{
				TowerDefensePacketBankData towerDefensePacketBankData2 = BuildExpandedPacketBank(item.AsString(), rawBanks, cache, visiting);
				MergePacketBankCategories(towerDefensePacketBankData.category, towerDefensePacketBankData2.category);
			}
		}
		visiting.Remove(bankName);
		cache[bankName] = towerDefensePacketBankData;
		return towerDefensePacketBankData;
	}

	private static void MergePacketBankCategories(Dictionary target, Dictionary source)
	{
		foreach (Variant key in source.Keys)
		{
			string text = key.AsString();
			Godot.Collections.Array array = source[key].AsGodotArray();
			if (target.TryGetValue(text, out var value) && value.VariantType == Variant.Type.Array)
			{
				value.AsGodotArray().AddRange(array);
			}
			else
			{
				target[text] = array.Duplicate(deep: true);
			}
		}
	}

	private static void ValidateCompletePublication(FullGameplayRegistryRoots roots, FullGameplayResourceManifestData manifest, System.Collections.Generic.Dictionary<string, Resource> characterScenes, System.Collections.Generic.Dictionary<string, Resource> sprites, System.Collections.Generic.Dictionary<string, Resource> packets, System.Collections.Generic.Dictionary<string, TowerDefensePacketBankData> packetBanks)
	{
		if (characterScenes.Count != roots.ScenePathByCharacter.Count || characterScenes.Count > manifest.RegisteredCharacterCount)
		{
			throw new InvalidOperationException($"Character Scene publication count mismatch: actual={characterScenes.Count}, expectedBindings={roots.ScenePathByCharacter.Count}, registeredCharacters={manifest.RegisteredCharacterCount}");
		}
		if (sprites.Count != manifest.RegisteredSpriteCount)
		{
			throw new InvalidOperationException($"Character Sprite publication count mismatch: actual={sprites.Count}, expected={manifest.RegisteredSpriteCount}");
		}
		if (packets.Count != manifest.RegisteredPacketCount)
		{
			throw new InvalidOperationException($"Packet publication count mismatch: actual={packets.Count}, expected={manifest.RegisteredPacketCount}");
		}
		foreach (var (value, towerDefensePacketBankData2) in packetBanks)
		{
			foreach (Variant packet in towerDefensePacketBankData2.GetPacketList())
			{
				string text2 = packet.AsString();
				string value2;
				bool flag = roots.CharacterNameByPacket.TryGetValue(text2, out value2);
				if (!packets.ContainsKey(text2) || !flag || !characterScenes.ContainsKey(value2) || !sprites.ContainsKey(value2))
				{
					throw new InvalidOperationException($"Expanded PacketBank candidate is not fully resident: bank={value}, packet={text2}, character={value2}");
				}
			}
		}
	}

	private Resource LoadBuiltInRoot(string configuredPath, string category, string key, out double actualLoadMilliseconds)
	{
		RequireMainThread("LoadBuiltInRoot");
		if (_gameplayResourceLoadState == GameplayResourceLoadState.Ready)
		{
			Interlocked.Increment(ref _lateCharacterResourceLoadCount);
			throw new InvalidOperationException($"Late built-in gameplay resource load is forbidden: category={category}, key={key}, path={configuredPath}");
		}
		string text = ProjectResourceUidCache.ResolveResourcePath(configuredPath).Replace('\\', '/');
		if (string.IsNullOrWhiteSpace(text) || !ResourceLoader.Exists(text))
		{
			throw new InvalidOperationException($"Built-in gameplay resource path is missing: category={category}, key={key}, path={configuredPath}, resolved={text}");
		}
		ulong ticksUsec = Time.GetTicksUsec();
		Resource resource = GD.Load(text);
		actualLoadMilliseconds = (double)(Time.GetTicksUsec() - ticksUsec) / 1000.0;
		if (!GodotObject.IsInstanceValid(resource))
		{
			throw new InvalidOperationException($"Built-in gameplay resource failed to load: category={category}, key={key}, path={text}");
		}
		return resource;
	}

	private static Dictionary RequireJsonDictionary(Json resource, string context)
	{
		if (!GodotObject.IsInstanceValid(resource) || resource.Data.VariantType != Variant.Type.Dictionary)
		{
			throw new InvalidOperationException("JSON registry is unavailable or invalid: " + context);
		}
		return resource.Data.AsGodotDictionary();
	}

	private void PublishReady(FullGameplayCoreSnapshot coreSnapshot, FullGameplayRootSnapshot rootSnapshot)
	{
		currentStep = stepMax;
		stepName = string.Empty;
		double fullWallMilliseconds = (double)(Time.GetTicksUsec() - _fullLoadStartedUsec) / 1000.0;
		double textureArrayMilliseconds = ((_gameplayAtlasPoseReadyUsec > _gameplayAtlasLoadStartedUsec) ? ((double)(_gameplayAtlasPoseReadyUsec - _gameplayAtlasLoadStartedUsec) / 1000.0) : 0.0);
		FullGameplayResourceLoadMetrics = new FullGameplayResourceLoadMetricsSnapshot(_coreMilliseconds, textureArrayMilliseconds, _characterSceneMilliseconds, _spriteMilliseconds, _packetMilliseconds, fullWallMilliseconds, 0, LateCharacterResourceLoadCount, _packetBankMissingCount, _rootLoadMetrics);
		SURVIVALS = coreSnapshot.Survivals;
		LEVELS = coreSnapshot.Levels;
		MAPS = coreSnapshot.Maps;
		BGMS = coreSnapshot.Bgms;
		PROJECTILE_CONFIG = coreSnapshot.ProjectileConfigs;
		AUDIOS = coreSnapshot.Audios;
		TALKS = coreSnapshot.Talks;
		TUTORIALS = coreSnapshot.Tutorials;
		COLLECTABLES = coreSnapshot.Collectables;
		SHOVELS = coreSnapshot.Shovels;
		MOWERS = coreSnapshot.Mowers;
		SHOPS = coreSnapshot.Shops;
		DAILY_LEVEL_DATA = coreSnapshot.DailyLevelData;
		TOWERDEFENSE_LEVEL_EVENT = coreSnapshot.TowerDefenseLevelEvents;
		TOWERDEFENSE_CHARCATERS = rootSnapshot.CharacterScenes;
		CHARCTAER_SPRITE = rootSnapshot.Sprites;
		TOWERDEFENSE_PACKETS = rootSnapshot.Packets;
		TOWERDEFENSE_PACKETBANKS = rootSnapshot.PacketBanks;
		_bgmPaths = coreSnapshot.BgmPaths;
		_bgmResourcesLoaded = false;
		_characterScenePaths = new System.Collections.Generic.Dictionary<string, string>(coreSnapshot.RegistryRoots.ScenePathByCharacter, StringComparer.OrdinalIgnoreCase);
		_characterSpritePaths = new System.Collections.Generic.Dictionary<string, string>(coreSnapshot.RegistryRoots.SpritePathByCharacter, StringComparer.OrdinalIgnoreCase);
		_packetPaths = new System.Collections.Generic.Dictionary<string, string>(coreSnapshot.RegistryRoots.PacketPathByName, StringComparer.OrdinalIgnoreCase);
		_characterNameByPacket = new System.Collections.Generic.Dictionary<string, string>(coreSnapshot.RegistryRoots.CharacterNameByPacket, StringComparer.OrdinalIgnoreCase);
		FullGameplayResourcesReadyUsec = Time.GetTicksUsec();
		_gameplayResourceLoadState = GameplayResourceLoadState.Ready;
		_fullGameplayResourceLoadError = string.Empty;
		_fullReadyCompletion.TrySetResult(result: true);
		EmitLoadPercentage(1.0, stepName);
		NotifyFullGameplayLoadReady();
	}

	private void FailFullGameplayLoad(Exception exception)
	{
		if (_gameplayResourceLoadState != GameplayResourceLoadState.Ready && _gameplayResourceLoadState != GameplayResourceLoadState.Failed)
		{
			_fullGameplayResourceLoadError = exception?.Message ?? "Unknown full gameplay resource load error.";
			_gameplayResourceLoadState = GameplayResourceLoadState.Failed;
			double fullWallMilliseconds = ((_fullLoadStartedUsec == 0L) ? 0.0 : ((double)(Time.GetTicksUsec() - _fullLoadStartedUsec) / 1000.0));
			double textureArrayMilliseconds = ((_gameplayAtlasPoseReadyUsec > _gameplayAtlasLoadStartedUsec) ? ((double)(_gameplayAtlasPoseReadyUsec - _gameplayAtlasLoadStartedUsec) / 1000.0) : 0.0);
			FullGameplayResourceLoadMetrics = new FullGameplayResourceLoadMetricsSnapshot(_coreMilliseconds, textureArrayMilliseconds, _characterSceneMilliseconds, _spriteMilliseconds, _packetMilliseconds, fullWallMilliseconds, 1, LateCharacterResourceLoadCount, _packetBankMissingCount, _rootLoadMetrics);
			InvalidOperationException exception2 = new InvalidOperationException("Full gameplay resource loading failed: " + _fullGameplayResourceLoadError, exception);
			_fullReadyCompletion.TrySetException(exception2);
			NotifyFullGameplayLoadFailed(exception);
		}
	}

	private void NotifyFullGameplayLoadReady()
	{
		try
		{
			LogMetrics(FullGameplayResourceLoadMetrics);
		}
		catch (Exception exception)
		{
			ReportNotificationFailure("Full gameplay Ready metrics logging", exception);
		}
		NotifyLoadOverSubscribers();
	}

	private void NotifyFullGameplayLoadFailed(Exception exception)
	{
		try
		{
			ILogger logger = _logger;
			bool enabled;
			ZLoggerErrorInterpolatedStringHandler message = new ZLoggerErrorInterpolatedStringHandler(40, 2, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Full gameplay resource loading failed: ");
				message.AppendFormatted(_fullGameplayResourceLoadError, 0, null, "_fullGameplayResourceLoadError");
				message.AppendLiteral("\n");
				message.AppendFormatted(exception, 0, null, "exception");
			}
			logger.ZLogError(ref message);
		}
		catch (Exception exception2)
		{
			ReportNotificationFailure("Full gameplay Failed logging", exception2);
		}
		NotifyLoadFailedSubscribers(_fullGameplayResourceLoadError);
	}

	private void NotifyLoadOverSubscribers()
	{
		LoadOverEventHandler loadOverEventHandler = this.OnLoadOver;
		if (loadOverEventHandler == null)
		{
			return;
		}
		Delegate[] invocationList = loadOverEventHandler.GetInvocationList();
		foreach (Delegate obj in invocationList)
		{
			try
			{
				((LoadOverEventHandler)obj)();
			}
			catch (Exception exception)
			{
				ReportNotificationFailure("OnLoadOver subscriber", exception);
			}
		}
	}

	private void NotifyLoadFailedSubscribers(string error)
	{
		LoadFailedEventHandler loadFailedEventHandler = this.OnLoadFailed;
		if (loadFailedEventHandler == null)
		{
			return;
		}
		Delegate[] invocationList = loadFailedEventHandler.GetInvocationList();
		foreach (Delegate obj in invocationList)
		{
			try
			{
				((LoadFailedEventHandler)obj)(error);
			}
			catch (Exception exception)
			{
				ReportNotificationFailure("OnLoadFailed subscriber", exception);
			}
		}
	}

	private static void ReportNotificationFailure(string context, Exception exception)
	{
		try
		{
			ILogger logger = _logger;
			bool enabled;
			ZLoggerErrorInterpolatedStringHandler message = new ZLoggerErrorInterpolatedStringHandler(41, 2, logger, out enabled);
			if (enabled)
			{
				message.AppendFormatted(context, 0, null, "context");
				message.AppendLiteral(" failed without changing resource state: ");
				message.AppendFormatted(exception, 0, null, "exception");
			}
			logger.ZLogError(ref message);
		}
		catch
		{
			try
			{
				GD.PushError($"{context} failed without changing resource state: {exception}");
			}
			catch
			{
			}
		}
	}

	private static void LogMetrics(FullGameplayResourceLoadMetricsSnapshot snapshot)
	{
		ILogger logger = _logger;
		ILogger logger2 = logger;
		bool enabled;
		ZLoggerInformationInterpolatedStringHandler message = new ZLoggerInformationInterpolatedStringHandler(167, 8, logger, out enabled);
		if (enabled)
		{
			message.AppendLiteral("Full gameplay resources ready; coreMs=");
			message.AppendFormatted(snapshot.CoreMilliseconds, 0, "F2", "snapshot.CoreMilliseconds");
			message.AppendLiteral(", textureArrayMs=");
			message.AppendFormatted(snapshot.TextureArrayMilliseconds, 0, "F2", "snapshot.TextureArrayMilliseconds");
			message.AppendLiteral(", characterSceneMs=");
			message.AppendFormatted(snapshot.CharacterSceneMilliseconds, 0, "F2", "snapshot.CharacterSceneMilliseconds");
			message.AppendLiteral(", spriteMs=");
			message.AppendFormatted(snapshot.SpriteMilliseconds, 0, "F2", "snapshot.SpriteMilliseconds");
			message.AppendLiteral(", packetMs=");
			message.AppendFormatted(snapshot.PacketMilliseconds, 0, "F2", "snapshot.PacketMilliseconds");
			message.AppendLiteral(", fullWallMs=");
			message.AppendFormatted(snapshot.FullWallMilliseconds, 0, "F2", "snapshot.FullWallMilliseconds");
			message.AppendLiteral(", lateCharacterResourceLoadCount=");
			message.AppendFormatted(snapshot.LateCharacterResourceLoadCount, 0, null, "snapshot.LateCharacterResourceLoadCount");
			message.AppendLiteral(", packetBankMissingCount=");
			message.AppendFormatted(snapshot.PacketBankMissingCount, 0, null, "snapshot.PacketBankMissingCount");
		}
		logger2.ZLogInformation(ref message);
		foreach (KeyValuePair<string, FullGameplayResourceCategoryMetrics> categoryMetric in snapshot.CategoryMetrics)
		{
			categoryMetric.Deconstruct(out var key, out var value);
			string value2 = key;
			FullGameplayResourceCategoryMetrics fullGameplayResourceCategoryMetrics = value;
			logger = _logger;
			ILogger logger3 = logger;
			message = new ZLoggerInformationInterpolatedStringHandler(144, 9, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Full gameplay category metrics; category=");
				message.AppendFormatted(value2, 0, null, "category");
				message.AppendLiteral(", roots=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.RootCount, 0, null, "metrics.RootCount");
				message.AppendLiteral(", queueWaitMs=0.00, actualLoadMs=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.TotalActualLoadMilliseconds, 0, "F2", "metrics.TotalActualLoadMilliseconds");
				message.AppendLiteral(", publishMs=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.TotalPublishMilliseconds, 0, "F2", "metrics.TotalPublishMilliseconds");
				message.AppendLiteral(", p50Ms=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.P50ActualLoadMilliseconds, 0, "F2", "metrics.P50ActualLoadMilliseconds");
				message.AppendLiteral(", p95Ms=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.P95ActualLoadMilliseconds, 0, "F2", "metrics.P95ActualLoadMilliseconds");
				message.AppendLiteral(", maxMs=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.MaximumActualLoadMilliseconds, 0, "F2", "metrics.MaximumActualLoadMilliseconds");
				message.AppendLiteral(", cacheHits=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.CacheBindingHitCount, 0, null, "metrics.CacheBindingHitCount");
				message.AppendLiteral(", cacheMisses=");
				message.AppendFormatted(fullGameplayResourceCategoryMetrics.CacheBindingMissCount, 0, null, "metrics.CacheBindingMissCount");
			}
			logger3.ZLogInformation(ref message);
		}
	}

	private static TaskCompletionSource<bool> CreateReadyCompletion()
	{
		return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	private void RequireMainThread(string context)
	{
		if (System.Environment.CurrentManagedThreadId != _mainThreadId)
		{
			throw new InvalidOperationException(context + " must be called from the Godot main thread.");
		}
	}

	private void EmitLoadPercentageForCurrentStep(string loadingStep)
	{
		stepName = loadingStep;
		EmitLoadPercentage((double)currentStep / (double)stepMax, loadingStep);
	}

	private void EmitLoadResourceProgress(int step, string loadingStep, string resourceName, int resourceIndex, int resourceTotal)
	{
		double num = ((resourceTotal > 0) ? ((double)resourceIndex / (double)resourceTotal) : 0.0);
		EmitLoadPercentage(((double)step + num) / (double)stepMax, loadingStep, resourceName, resourceIndex, resourceTotal);
	}

	private void EmitLoadPercentage(double percentage, string loadingStep, string resourceName = "", int resourceIndex = 0, int resourceTotal = 0)
	{
		LoadPercentageEventHandler loadPercentageEventHandler = this.OnLoadPercentage;
		if (loadPercentageEventHandler == null)
		{
			return;
		}
		Delegate[] invocationList = loadPercentageEventHandler.GetInvocationList();
		foreach (Delegate obj in invocationList)
		{
			try
			{
				((LoadPercentageEventHandler)obj)(percentage, loadingStep, resourceName, resourceIndex, resourceTotal);
			}
			catch (Exception exception)
			{
				ReportNotificationFailure("OnLoadPercentage subscriber", exception);
			}
		}
	}

	public void EnsureAllBgmsLoaded()
	{
		if (_bgmResourcesLoaded)
		{
			return;
		}
		_bgmResourcesLoaded = true;
		foreach (KeyValuePair<string, string> bgmPath in _bgmPaths)
		{
			bgmPath.Deconstruct(out var key, out var value);
			string text = key;
			string text2 = ProjectResourceUidCache.ResolveResourcePath(value);
			Resource resource = GD.Load(text2);
			if (!GodotObject.IsInstanceValid(resource))
			{
				throw new InvalidOperationException("BGM resource failed to load: key=" + text + ", path=" + text2);
			}
			BGMS[text] = resource;
		}
	}

	public bool PreloadAdobeAnimateGlobalAtlases()
	{
		if (_gameplayAtlasLoadState == GameplayAtlasLoadState.Failed)
		{
			return false;
		}
		TextureLayered instance = EnsureAdobeAnimateVisualTextureArrayLoaded();
		TextureLayered instance2 = EnsureAdobeAnimateGpuPoseTextureArrayLoaded();
		int num;
		if (GodotObject.IsInstanceValid(instance))
		{
			num = (GodotObject.IsInstanceValid(instance2) ? 1 : 0);
			if (num != 0)
			{
				_gameplayAtlasLoadState = GameplayAtlasLoadState.Ready;
				_gameplayAtlasLoadError = string.Empty;
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	public void BeginGameplayAtlasPreload()
	{
		RequireMainThread("BeginGameplayAtlasPreload");
		if (!AreGameplayAtlasesReady && _gameplayAtlasLoadState != GameplayAtlasLoadState.LoadingVisual && _gameplayAtlasLoadState != GameplayAtlasLoadState.LoadingPose && _gameplayAtlasLoadState != GameplayAtlasLoadState.Failed)
		{
			_gameplayAtlasLoadError = string.Empty;
			_adobeAnimateVisualTextureArray = null;
			_adobeAnimateGpuPoseTextureArray = null;
			_gameplayAtlasLoadStartedUsec = Time.GetTicksUsec();
			_gameplayAtlasVisualReadyUsec = 0uL;
			_gameplayAtlasPoseReadyUsec = 0uL;
			if (!TryRequestGameplayAtlas("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png", GameplayAtlasLoadState.LoadingVisual))
			{
				FailGameplayAtlasLoad("Visual Texture2DArray request failed: res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png");
			}
		}
	}

	private bool TryRequestGameplayAtlas(string resourcePath, GameplayAtlasLoadState loadingState)
	{
		if (string.IsNullOrWhiteSpace(resourcePath) || !ResourceLoader.Exists(resourcePath))
		{
			return false;
		}
		Error num = ResourceLoader.LoadThreadedRequest(resourcePath, "", useSubThreads: false, ResourceLoader.CacheMode.Reuse);
		ResourceLoader.ThreadLoadStatus threadLoadStatus = ResourceLoader.LoadThreadedGetStatus(resourcePath);
		if (num != Error.Ok && threadLoadStatus != ResourceLoader.ThreadLoadStatus.InProgress && threadLoadStatus != ResourceLoader.ThreadLoadStatus.Loaded)
		{
			return false;
		}
		_gameplayAtlasLoadState = loadingState;
		return true;
	}

	private void PumpGameplayAtlasPreload()
	{
		switch (_gameplayAtlasLoadState)
		{
		case GameplayAtlasLoadState.LoadingVisual:
			PumpGameplayVisualAtlas();
			break;
		case GameplayAtlasLoadState.LoadingPose:
			PumpGameplayPoseAtlas();
			break;
		}
	}

	private void PumpGameplayVisualAtlas()
	{
		string text = "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png";
		ResourceLoader.ThreadLoadStatus threadLoadStatus = ResourceLoader.LoadThreadedGetStatus(text);
		switch (threadLoadStatus)
		{
		case ResourceLoader.ThreadLoadStatus.InProgress:
			break;
		default:
			FailGameplayAtlasLoad($"Visual Texture2DArray load failed: status={threadLoadStatus}, path={text}");
			break;
		case ResourceLoader.ThreadLoadStatus.Loaded:
			_adobeAnimateVisualTextureArray = ResourceLoader.LoadThreadedGet(text) as TextureLayered;
			if (!GodotObject.IsInstanceValid(_adobeAnimateVisualTextureArray) || !_adobeAnimateVisualTextureArray.GetRid().IsValid)
			{
				FailGameplayAtlasLoad("Visual Texture2DArray is invalid after threaded load: " + text);
				break;
			}
			_gameplayAtlasVisualReadyUsec = Time.GetTicksUsec();
			if (!TryRequestGameplayAtlas("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGpuPoseTextureArray.exr", GameplayAtlasLoadState.LoadingPose))
			{
				FailGameplayAtlasLoad("Pose Texture2DArray request failed: res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGpuPoseTextureArray.exr");
			}
			break;
		}
	}

	private void PumpGameplayPoseAtlas()
	{
		string text = "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGpuPoseTextureArray.exr";
		ResourceLoader.ThreadLoadStatus threadLoadStatus = ResourceLoader.LoadThreadedGetStatus(text);
		switch (threadLoadStatus)
		{
		case ResourceLoader.ThreadLoadStatus.InProgress:
			break;
		default:
			FailGameplayAtlasLoad($"Pose Texture2DArray load failed: status={threadLoadStatus}, path={text}");
			break;
		case ResourceLoader.ThreadLoadStatus.Loaded:
		{
			_adobeAnimateGpuPoseTextureArray = ResourceLoader.LoadThreadedGet(text) as TextureLayered;
			if (!GodotObject.IsInstanceValid(_adobeAnimateGpuPoseTextureArray) || !_adobeAnimateGpuPoseTextureArray.GetRid().IsValid)
			{
				FailGameplayAtlasLoad("Pose Texture2DArray is invalid after threaded load: " + text);
				break;
			}
			TextureLayered textureLayered = AdobeAnimateGlobalAtlasCache.PreloadVisualTextureArray();
			TextureLayered textureLayered2 = AdobeAnimateGlobalAtlasCache.PreloadGpuPoseTextureArray();
			if (!GodotObject.IsInstanceValid(textureLayered) || !GodotObject.IsInstanceValid(textureLayered2) || textureLayered.GetRid() != _adobeAnimateVisualTextureArray.GetRid() || textureLayered2.GetRid() != _adobeAnimateGpuPoseTextureArray.GetRid())
			{
				FailGameplayAtlasLoad("Gameplay atlas cache did not retain the same Visual/Pose TextureArray RIDs loaded by ResourceManager.");
				break;
			}
			_adobeAnimateVisualTextureArray = textureLayered;
			_adobeAnimateGpuPoseTextureArray = textureLayered2;
			_gameplayAtlasPoseReadyUsec = Time.GetTicksUsec();
			_gameplayAtlasLoadState = GameplayAtlasLoadState.Ready;
			_gameplayAtlasLoadError = string.Empty;
			break;
		}
		}
	}

	private void FailGameplayAtlasLoad(string error)
	{
		_gameplayAtlasLoadError = error ?? "Unknown gameplay atlas load error.";
		_gameplayAtlasLoadState = GameplayAtlasLoadState.Failed;
		ILogger logger = _logger;
		bool enabled;
		ZLoggerErrorInterpolatedStringHandler message = new ZLoggerErrorInterpolatedStringHandler(31, 1, logger, out enabled);
		if (enabled)
		{
			message.AppendLiteral("Gameplay atlas preload failed: ");
			message.AppendFormatted(_gameplayAtlasLoadError, 0, null, "_gameplayAtlasLoadError");
		}
		logger.ZLogError(ref message);
	}

	public async Task EnsureGameplayAtlasesReadyAsync()
	{
		RequireMainThread("EnsureGameplayAtlasesReadyAsync");
		if (_gameplayAtlasLoadState == GameplayAtlasLoadState.NotStarted)
		{
			BeginGameplayAtlasPreload();
		}
		while (_gameplayAtlasLoadState == GameplayAtlasLoadState.LoadingVisual || _gameplayAtlasLoadState == GameplayAtlasLoadState.LoadingPose)
		{
			if (!IsInsideTree())
			{
				throw new InvalidOperationException("SceneTree became unavailable while waiting for gameplay atlases.");
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		if (!AreGameplayAtlasesReady)
		{
			throw new InvalidOperationException($"Gameplay atlases are not ready: {_gameplayAtlasLoadState}, {_gameplayAtlasLoadError}");
		}
	}

	public TextureLayered EnsureAdobeAnimateVisualTextureArrayLoaded()
	{
		if (!GodotObject.IsInstanceValid(_adobeAnimateVisualTextureArray))
		{
			_adobeAnimateVisualTextureArray = AdobeAnimateGlobalAtlasCache.PreloadVisualTextureArray();
		}
		return _adobeAnimateVisualTextureArray;
	}

	public TextureLayered EnsureAdobeAnimateGpuPoseTextureArrayLoaded()
	{
		if (!GodotObject.IsInstanceValid(_adobeAnimateGpuPoseTextureArray))
		{
			_adobeAnimateGpuPoseTextureArray = AdobeAnimateGlobalAtlasCache.PreloadGpuPoseTextureArray();
		}
		return _adobeAnimateGpuPoseTextureArray;
	}

	public PackedScene GetCharacterSprite(string characterName)
	{
		RequireFullGameplayResourcesReady("GetCharacterSprite(" + characterName + ")");
		if (string.IsNullOrWhiteSpace(characterName) || !CHARCTAER_SPRITE.TryGetValue(characterName, out var value))
		{
			throw new KeyNotFoundException("Built-in Character Sprite is not registered: " + characterName);
		}
		return (value as PackedScene) ?? throw new InvalidOperationException("Built-in Character Sprite is not a PackedScene: " + characterName);
	}

	public PackedScene GetCharacterScene(string characterName)
	{
		RequireFullGameplayResourcesReady("GetCharacterScene(" + characterName + ")");
		if (string.IsNullOrWhiteSpace(characterName) || !TOWERDEFENSE_CHARCATERS.TryGetValue(characterName, out var value))
		{
			throw new KeyNotFoundException("Built-in Character Scene is not registered: " + characterName);
		}
		return (value as PackedScene) ?? throw new InvalidOperationException("Built-in Character Scene is not a PackedScene: " + characterName);
	}

	public Variant GetPacket(string packetName)
	{
		RequireFullGameplayResourcesReady("GetPacket(" + packetName + ")");
		if (string.IsNullOrWhiteSpace(packetName) || !TOWERDEFENSE_PACKETS.TryGetValue(packetName, out var value))
		{
			throw new KeyNotFoundException("Built-in Packet config is not registered: " + packetName);
		}
		return value;
	}

	public List<string> GetPacketNames()
	{
		RequireFullGameplayResourcesReady("GetPacketNames");
		return new List<string>(TOWERDEFENSE_PACKETS.Keys);
	}

	public string GetRandomSpriteName()
	{
		RequireFullGameplayResourcesReady("GetRandomSpriteName");
		if (_characterSpritePaths.Count == 0)
		{
			throw new InvalidOperationException("No built-in Character Sprite is registered.");
		}
		int num = _random.Next(_characterSpritePaths.Count);
		int num2 = 0;
		foreach (string key in _characterSpritePaths.Keys)
		{
			if (num2 == num)
			{
				return key;
			}
			num2++;
		}
		throw new InvalidOperationException("Random Character Sprite selection exceeded the registered key range.");
	}

	public void ReleaseTransientResources(bool clearThumbnailCaches = true)
	{
		foreach (Resource value in MAPS.Values)
		{
			if (value is TowerDefenseMapConfig towerDefenseMapConfig)
			{
				towerDefenseMapConfig.ClearLoadedMapResources(clearThumbnail: true);
			}
		}
		TransientStaticTextureRelease.Release();
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
	}

	public ResourceManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/ResourceManager");
	}
}
