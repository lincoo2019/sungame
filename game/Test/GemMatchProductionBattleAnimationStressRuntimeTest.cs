using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed partial class GemMatchProductionBattleAnimationStressRuntimeTest : Node
{
	private const string ResultMarker = "GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STRESS_RESULT";

	private const string LevelPath = "res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2.tres";

	private const string BattleScenePath = "res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn";

	private const int SwapCycleCount = 12;

	private const int RefreshCycleCount = 4;

	private const int HoleLifecycleCycleCount = 4;

	private const int HugeWaveIndex = 6;

	private const ulong StressRandomSeed = 2026082101uL;

	private const int ResourceLoadFrameLimit = 7200;

	private const int BattleStartFrameLimit = 1200;

	private const int ResolveFrameLimit = 900;

	private const int BaselineFrameCount = 36;

	private const float MassiveBlankRatio = 0.2f;

	private static readonly Color BackgroundColor = new Color(0.011f, 0.015f, 0.021f);

	private AdobeAnimateRenderBackend _originalBackend;

	private bool _originalRasterCompositeEnabled;

	private int _originalMaxFps;

	private string _originalEnterLevelMode = string.Empty;

	private bool _originalEditorMode;

	private TowerDefenseLevelBaseConfig _originalLevel;

	private TowerDefenseControlNew _originalControl;

	private TowerDefenseControlNew _battle;

	private TowerDefenseBattleFeatureGemMatch _feature;

	private ColorRect _characterBackground;

	private int _baselinePixels;

	private int _minimumPixels = 2147483647;

	private int _massiveBlankFrames;

	private int _crowdRootShortageFrames;

	private int _maximumFallbackRoots;

	private int _minimumActiveGemCharacters = 2147483647;

	private int _maximumDelayedRemovalCharacters;

	private int _maximumFallingCharacters;

	private int _maximumComboCount;

	private int _replacedCharacterCount;

	private int _completedHoleLifecycleCycles;

	private int _maximumHoleCraterCharacters;

	private int _auditedFrames;

	private int _maximumMountedZombieCharacters;

	public override async void _Ready()
	{
		int exitCode = 2;
		List<string> failures = new List<string>();
		try
		{
			_ = 5;
			try
			{
				PrepareRuntime();
				await LoadProductionResources(failures);
				await StartProductionBattle(failures);
				if (failures.Count == 0)
				{
					ConfigureStressTimings();
					await SpawnProductionZombiePressure(failures);
					MountCharacterPixelBackground();
					await CaptureBaseline(failures);
					await RunHoleLifecycleStress(failures);
					await RunBoardStress(failures);
				}
				ValidateFinalState(failures);
				PrintFailures(failures);
				bool flag = failures.Count == 0;
				AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				int value = CountActiveGemCharacters();
				int value2 = CountBattleZombies();
				GD.Print($"{"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STRESS_RESULT"} passed={flag} level={"res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2.tres"} seed={2026082101uL} board={_feature?.config?.boardCols}x{_feature?.config?.boardRows} swaps={12} refreshes={4} holeCycles={_completedHoleLifecycleCycles}/{4} maximumHoleCraters={_maximumHoleCraterCharacters} matchBatches={_feature?.currentMatchCount} maximumCombo={_maximumComboCount} replacedCharacters={_replacedCharacterCount} activeGemCharacters={value} minimumActiveGemCharacters={_minimumActiveGemCharacters} zombieCharacters={value2} maximumMountedZombies={_maximumMountedZombieCharacters} maximumDelayedRemovalCharacters={_maximumDelayedRemovalCharacters} maximumFallingCharacters={_maximumFallingCharacters} auditedFrames={_auditedFrames} baselinePixels={_baselinePixels} minimumPixels={_minimumPixels} massiveBlankFrames={_massiveBlankFrames} crowdRoots={aggregateRenderStats.CrowdRoots} crowdRootShortageFrames={_crowdRootShortageFrames} fallbackRoots={aggregateRenderStats.FallbackRoots} maximumFallback={_maximumFallbackRoots} renderer={RenderingServer.GetCurrentRenderingMethod()}");
				exitCode = ((!flag) ? 2 : 0);
			}
			catch (Exception value3)
			{
				GD.PrintErr($"{"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STRESS_RESULT"} passed=False exception={value3}");
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
		base.ProcessMode = ProcessModeEnum.Always;
		if (!GodotObject.IsInstanceValid(Global.Instance) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !GodotObject.IsInstanceValid(GameSaveManager.Instance))
		{
			throw new InvalidOperationException("required production autoloads are unavailable");
		}
		_originalBackend = Global.Instance.adobeAnimateRenderBackend;
		_originalRasterCompositeEnabled = AdobeAnimateRenderManager.RasterCompositeEnabled;
		_originalMaxFps = Engine.MaxFps;
		_originalEnterLevelMode = Global.Instance.enterLevelMode;
		_originalEditorMode = Global.Instance.isEditor;
		_originalLevel = TowerDefenseManager.Instance.currentLevelConfig;
		_originalControl = TowerDefenseManager.Instance.currentControl;
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
		AdobeAnimateRenderManager.RasterCompositeEnabled = false;
		Engine.MaxFps = 120;
		GD.Seed(2026082101uL);
		Global.Instance.enterLevelMode = "LevelTest";
		Global.Instance.isEditor = false;
		GameSaveManager.Instance.EnsureLoaded();
		if (string.IsNullOrEmpty(GameSaveManager.Instance.EnsureUser()))
		{
			GameSaveManager.Instance.SetUserCurrent("GemMatchProductionAnimationStress");
		}
	}

	private async Task LoadProductionResources(List<string> failures)
	{
		bool resourcesLoaded = false;
		ResourceManager.Instance.OnLoadOver += OnLoadOver;
		try
		{
			ResourceManager.Instance.BeginLoad();
			for (int frame = 0; frame < 7200; frame++)
			{
				if (resourcesLoaded)
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		finally
		{
			ResourceManager.Instance.OnLoadOver -= OnLoadOver;
		}
		if (!resourcesLoaded)
		{
			failures.Add($"production resources did not finish loading within {7200} frames");
		}
		GD.Print($"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STAGE stage=resources loaded={resourcesLoaded}");
		void OnLoadOver()
		{
			resourcesLoaded = true;
		}
	}

	private async Task StartProductionBattle(List<string> failures)
	{
		GD.Print("GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STAGE stage=level-load-begin path=res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2.tres");
		TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
		if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig))
		{
			failures.Add("production GemMatch level could not load path=res://Asset/Config/Level/TowerDefense/MiniGames/MiniGames_Level5_2.tres");
			return;
		}
		GD.Print($"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STAGE stage=level-loaded name={towerDefenseLevelConfig.name} process={towerDefenseLevelConfig.processName}");
		TowerDefenseManager.Instance.currentLevelConfig = towerDefenseLevelConfig;
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn", null, ResourceLoader.CacheMode.IgnoreDeep);
		if (!GodotObject.IsInstanceValid(packedScene) || !packedScene.CanInstantiate())
		{
			failures.Add("production battle scene could not load path=res://Scene/TowerDefesne/TowerDefenseNew/TowerDefenseControlNew.tscn");
			return;
		}
		_battle = packedScene.Instantiate<TowerDefenseControlNew>(PackedScene.GenEditState.Disabled);
		GD.Print("GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STAGE stage=battle-instantiated");
		AddChild(_battle, forceReadableName: false, InternalMode.Disabled);
		GD.Print("GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STAGE stage=battle-mounted");
		for (int frame = 0; frame < 1200; frame++)
		{
			_feature = _battle.GetFeature(new StringName("GemMatch")) as TowerDefenseBattleFeatureGemMatch;
			if (_battle.isGameRunning && GodotObject.IsInstanceValid(_feature) && HasCompleteBoard())
			{
				break;
			}
			if (frame % 300 == 0)
			{
				GD.Print($"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STAGE stage=battle-wait frame={frame} running={_battle.isGameRunning} feature={GodotObject.IsInstanceValid(_feature)} rows={(_feature?.grid?.Count).GetValueOrDefault()} active={CountActiveGemCharacters()}");
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		GD.Print($"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STAGE stage=battle-ready running={_battle.isGameRunning} feature={GodotObject.IsInstanceValid(_feature)} rows={(_feature?.grid?.Count).GetValueOrDefault()} active={CountActiveGemCharacters()}");
		if (!_battle.isGameRunning || !GodotObject.IsInstanceValid(_feature))
		{
			failures.Add("production battle did not reach GameRunning with a GemMatch feature");
		}
		else if (!HasCompleteBoard())
		{
			failures.Add($"production GemMatch board is incomplete rows={_feature.grid.Count} expectedRows={_feature.config.boardRows} active={CountActiveGemCharacters()} expected={_feature.config.boardRows * _feature.config.boardCols}");
		}
	}

	private void ConfigureStressTimings()
	{
		_feature.config.swapDuration = 0.04;
		_feature.config.matchResolveDelay = 0.01;
		_feature.config.fallSpeed = 1800.0;
		_feature.config.minimumFallDuration = 0.04;
		_feature.config.fallRowDelay = 0.01;
		_feature.config.fillHoleCost = 0;
		_feature.config.refreshBoardCost = 0;
		while (_feature.config.plantList.Count > 4)
		{
			_feature.config.plantList.RemoveAt(_feature.config.plantList.Count - 1);
		}
	}

	private async Task RunHoleLifecycleStress(List<string> failures)
	{
		int expectedCharacters = _feature.config.boardRows * _feature.config.boardCols;
		for (int cycle = 0; cycle < 4; cycle++)
		{
			Vector2I value = new Vector2I(cycle % _feature.config.boardCols, cycle % _feature.config.boardRows);
			GemPiece gem = _feature.grid[value.Y][value.X];
			TowerDefenseCharacter destroyedCharacter = gem?.character;
			if (!GodotObject.IsInstanceValid(gem) || gem.isHole || !GodotObject.IsInstanceValid(destroyedCharacter))
			{
				failures.Add($"hole cycle {cycle + 1}: selected production gem is unavailable position={value}");
				break;
			}
			destroyedCharacter.Destroy();
			bool holeStateEntered = gem.isHole && !GodotObject.IsInstanceValid(gem.character);
			TowerDefenseCrater holeCrater = null;
			for (int frame = 0; frame < 90; frame++)
			{
				await AuditCurrentFrame($"hole-create-{cycle + 1}");
				holeCrater = gem.character as TowerDefenseCrater;
				if (gem.isHole && GodotObject.IsInstanceValid(holeCrater) && GodotObject.IsInstanceValid(holeCrater.sprite))
				{
					break;
				}
			}
			int num = CountActiveGemCharacters();
			int val = CountHoleCraterCharacters();
			_maximumHoleCraterCharacters = Math.Max(_maximumHoleCraterCharacters, val);
			if (!holeStateEntered || !GodotObject.IsInstanceValid(holeCrater) || !GodotObject.IsInstanceValid(holeCrater.sprite) || num != expectedCharacters - 1)
			{
				failures.Add($"hole cycle {cycle + 1}: production destroy did not establish one animated hole entered={holeStateEntered} active={num}/{expectedCharacters - 1} crater={GodotObject.IsInstanceValid(holeCrater)} craterSprite={GodotObject.IsInstanceValid(holeCrater?.sprite)}");
				break;
			}
			if (!ExecuteFillHoleCommand())
			{
				failures.Add($"hole cycle {cycle + 1}: production fill-hole command was rejected");
				break;
			}
			bool restored = false;
			for (int frame = 0; frame < 180; frame++)
			{
				await AuditCurrentFrame($"hole-fill-{cycle + 1}");
				if (HasCompleteBoard() && !_feature.isProcessing && !GodotObject.IsInstanceValid(holeCrater) && !GodotObject.IsInstanceValid(destroyedCharacter))
				{
					restored = true;
					break;
				}
			}
			if (!restored)
			{
				failures.Add($"hole cycle {cycle + 1}: fill-hole lifecycle did not restore a complete board or release old visuals active={CountActiveGemCharacters()}/{expectedCharacters} processing={_feature.isProcessing} craterAlive={GodotObject.IsInstanceValid(holeCrater)} destroyedCharacterAlive={GodotObject.IsInstanceValid(destroyedCharacter)}");
				break;
			}
			_completedHoleLifecycleCycles++;
		}
		_minimumActiveGemCharacters = 2147483647;
	}

	private async Task SpawnProductionZombiePressure(List<string> failures)
	{
		TowerDefenseBattleFeatureWave waveFeature = _battle.GetFeature(new StringName("Wave")) as TowerDefenseBattleFeatureWave;
		if (!GodotObject.IsInstanceValid(waveFeature) || waveFeature.config == null || waveFeature.config.wave.Count == 0)
		{
			failures.Add("production Wave feature is unavailable for GemMatch zombie pressure");
			return;
		}
		int firstHugeWave = Math.Min(6, waveFeature.config.wave.Count - 1);
		int finalWave = waveFeature.config.wave.Count - 1;
		await waveFeature.SpawnZombie(firstHugeWave);
		if (finalWave != firstHugeWave)
		{
			await waveFeature.SpawnZombie(finalWave);
		}
		_maximumMountedZombieCharacters = Math.Max(_maximumMountedZombieCharacters, CountMountedBattleZombieCharacters());
		for (int frame = 0; frame < 8; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			_maximumMountedZombieCharacters = Math.Max(_maximumMountedZombieCharacters, CountMountedBattleZombieCharacters());
		}
		if (_maximumMountedZombieCharacters < 40)
		{
			failures.Add($"production big/final Wave zombie pressure is incomplete maximumMounted={_maximumMountedZombieCharacters} expectedAtLeast=40");
		}
	}

	private void MountCharacterPixelBackground()
	{
		Node parent = _battle.characterNode.GetParent();
		_characterBackground = new ColorRect
		{
			Name = "GemMatchAnimationStressBackground",
			Color = BackgroundColor,
			Position = new Vector2(-5000f, -4000f),
			Size = new Vector2(10000f, 8000f),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex = -4096,
			ProcessMode = ProcessModeEnum.Always
		};
		parent.AddChild(_characterBackground, forceReadableName: false, InternalMode.Disabled);
		parent.MoveChild(_characterBackground, 0);
	}

	private async Task CaptureBaseline(List<string> failures)
	{
		for (int frame = 0; frame < 36; frame++)
		{
			int val = await CaptureBoardSignalPixels();
			_baselinePixels = Math.Max(_baselinePixels, val);
		}
		if (_baselinePixels <= 0)
		{
			failures.Add("production GemMatch board produced no isolated character pixels");
		}
	}

	private async Task RunBoardStress(List<string> failures)
	{
		int completedRefreshes = 0;
		int completedSwaps = 0;
		for (int cycle = 0; cycle < 12; cycle++)
		{
			if (cycle % 3 == 0 && completedRefreshes < 4 && ExecuteRefreshCommand())
			{
				await WaitForBoardIdle($"refresh-{completedRefreshes + 1}", -1, failures);
				completedRefreshes++;
			}
			if (!TryFindValidSwap(out var from, out var to))
			{
				if (!ExecuteRefreshCommand())
				{
					failures.Add($"cycle {cycle + 1}: board had no legal move and refresh command was rejected");
					break;
				}
				await WaitForBoardIdle($"recovery-refresh-{cycle + 1}", -1, failures);
				if (!TryFindValidSwap(out from, out to))
				{
					failures.Add($"cycle {cycle + 1}: refreshed board still had no legal move");
					break;
				}
			}
			int matchCountBefore = _feature.currentMatchCount;
			HashSet<ulong> characterIdsBefore = CaptureActiveGemCharacterIds();
			if (!ExecuteSwapCommand(from, to))
			{
				failures.Add($"cycle {cycle + 1}: production swap command was rejected from={from} to={to}");
				break;
			}
			await WaitForBoardIdle($"swap-{cycle + 1}", matchCountBefore, failures);
			HashSet<ulong> hashSet = CaptureActiveGemCharacterIds();
			foreach (ulong item in characterIdsBefore)
			{
				if (!hashSet.Contains(item))
				{
					_replacedCharacterCount++;
				}
			}
			if (_feature.currentMatchCount <= matchCountBefore)
			{
				failures.Add($"cycle {cycle + 1}: legal swap completed without a removal batch before={matchCountBefore} after={_feature.currentMatchCount}");
			}
			completedSwaps++;
		}
		if (completedSwaps != 12)
		{
			failures.Add($"completed swap count mismatch actual={completedSwaps} expected={12}");
		}
		if (completedRefreshes != 4)
		{
			failures.Add($"completed refresh count mismatch actual={completedRefreshes} expected={4}");
		}
	}

	private async Task WaitForBoardIdle(string phase, int matchCountBefore, List<string> failures)
	{
		for (int frame = 0; frame < 900; frame++)
		{
			await AuditCurrentFrame(phase);
			bool flag = matchCountBefore < 0 || _feature.currentMatchCount > matchCountBefore;
			if (!_feature.isProcessing && flag)
			{
				return;
			}
		}
		failures.Add($"{phase}: production GemMatch state did not return to idle within {900} frames processing={_feature.isProcessing} matches={_feature.currentMatchCount}");
	}

	private bool ExecuteSwapCommand(Vector2I from, Vector2I to)
	{
		Dictionary command = new Dictionary
		{
			["action"] = "swap",
			["from_x"] = from.X,
			["from_y"] = from.Y,
			["to_x"] = to.X,
			["to_y"] = to.Y
		};
		return _feature.ExecuteNetworkCommand(command, EconomyAccountId.Local);
	}

	private bool ExecuteRefreshCommand()
	{
		Dictionary command = new Dictionary { ["action"] = "refresh" };
		return _feature.ExecuteNetworkCommand(command, EconomyAccountId.Local);
	}

	private bool ExecuteFillHoleCommand()
	{
		Dictionary command = new Dictionary { ["action"] = "fill_hole" };
		return _feature.ExecuteNetworkCommand(command, EconomyAccountId.Local);
	}

	private bool TryFindValidSwap(out Vector2I from, out Vector2I to)
	{
		from = new Vector2I(-1, -1);
		to = new Vector2I(-1, -1);
		for (int i = 0; i < _feature.config.boardRows; i++)
		{
			for (int j = 0; j < _feature.config.boardCols; j++)
			{
				Vector2I vector2I = new Vector2I(j, i);
				if (j + 1 < _feature.config.boardCols && SwapWouldMatch(vector2I, new Vector2I(j + 1, i)))
				{
					from = vector2I;
					to = new Vector2I(j + 1, i);
					return true;
				}
				if (i + 1 < _feature.config.boardRows && SwapWouldMatch(vector2I, new Vector2I(j, i + 1)))
				{
					from = vector2I;
					to = new Vector2I(j, i + 1);
					return true;
				}
			}
		}
		return false;
	}

	private bool SwapWouldMatch(Vector2I first, Vector2I second)
	{
		GemPiece gemPiece = _feature.grid[first.Y][first.X];
		GemPiece gemPiece2 = _feature.grid[second.Y][second.X];
		if (gemPiece == null || gemPiece2 == null || gemPiece.isHole || gemPiece2.isHole || gemPiece.characterKey == gemPiece2.characterKey)
		{
			return false;
		}
		_feature.grid[first.Y][first.X] = gemPiece2;
		_feature.grid[second.Y][second.X] = gemPiece;
		bool result = _feature.FindMatches().Count > 0;
		_feature.grid[first.Y][first.X] = gemPiece;
		_feature.grid[second.Y][second.X] = gemPiece2;
		return result;
	}

	private async Task AuditCurrentFrame(string phase)
	{
		int num = await CaptureBoardSignalPixels();
		int num2 = CountActiveGemCharacters();
		int num3 = CountExpectedCrowdVisibleGemCharacters();
		int num4 = CountDelayedRemovalCharacters();
		int num5 = CountFallingCharacters();
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		int num6 = Math.Max(1, Mathf.RoundToInt((float)_baselinePixels * 0.2f));
		if (num < num6)
		{
			_massiveBlankFrames++;
			GD.Print($"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_BLANK phase={phase} pixels={num} minimum={num6} activeGemCharacters={num2} delayedRemovalCharacters={num4} fallingCharacters={num5} crowdRoots={aggregateRenderStats.CrowdRoots} fallback={aggregateRenderStats.FallbackRoots}");
		}
		if (aggregateRenderStats.CrowdRoots < num3)
		{
			_crowdRootShortageFrames++;
			GD.Print($"GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_CROWD_SHORTAGE phase={phase} pixels={num} activeGemCharacters={num2} expectedCrowdVisibleCharacters={num3} delayedRemovalCharacters={num4} fallingCharacters={num5} crowdRoots={aggregateRenderStats.CrowdRoots} fallback={aggregateRenderStats.FallbackRoots}");
		}
		_minimumPixels = Math.Min(_minimumPixels, num);
		_minimumActiveGemCharacters = Math.Min(_minimumActiveGemCharacters, num2);
		_maximumDelayedRemovalCharacters = Math.Max(_maximumDelayedRemovalCharacters, num4);
		_maximumFallingCharacters = Math.Max(_maximumFallingCharacters, num5);
		_maximumComboCount = Math.Max(_maximumComboCount, _feature.comboCount);
		_maximumMountedZombieCharacters = Math.Max(_maximumMountedZombieCharacters, CountMountedBattleZombieCharacters());
		_maximumFallbackRoots = Math.Max(_maximumFallbackRoots, aggregateRenderStats.FallbackRoots);
		_auditedFrames++;
	}

	private async Task<int> CaptureBoardSignalPixels()
	{
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		Rect2I rect2I = CreateBoardCaptureRegion(image.GetSize());
		int num = 0;
		for (int i = rect2I.Position.Y; i < rect2I.End.Y; i += 2)
		{
			for (int j = rect2I.Position.X; j < rect2I.End.X; j += 2)
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

	private Rect2I CreateBoardCaptureRegion(Vector2I imageSize)
	{
		Rect2 rect = default(Rect2);
		bool flag = false;
		for (int i = 0; i < _feature.grid.Count; i++)
		{
			for (int j = 0; j < _feature.grid[i].Count; j++)
			{
				AdobeAnimateSprite adobeAnimateSprite = _feature.grid[i][j]?.character?.sprite;
				if (GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					Vector2 vector = adobeAnimateSprite.GetViewport().GetScreenTransform() * adobeAnimateSprite.GetGlobalTransformWithCanvas().Origin;
					if (!flag)
					{
						rect = new Rect2(vector, Vector2.Zero);
						flag = true;
					}
					else
					{
						rect = rect.Expand(vector);
					}
				}
			}
		}
		if (!flag)
		{
			return new Rect2I(Vector2I.Zero, imageSize);
		}
		rect = rect.Grow(130f);
		int num = Math.Clamp(Mathf.FloorToInt(rect.Position.X), 0, imageSize.X);
		int num2 = Math.Clamp(Mathf.FloorToInt(rect.Position.Y), 0, imageSize.Y);
		int num3 = Math.Clamp(Mathf.CeilToInt(rect.End.X), num, imageSize.X);
		int num4 = Math.Clamp(Mathf.CeilToInt(rect.End.Y), num2, imageSize.Y);
		return new Rect2I(num, num2, num3 - num, num4 - num2);
	}

	private bool HasCompleteBoard()
	{
		if (GodotObject.IsInstanceValid(_feature) && _feature.config != null && _feature.grid.Count == _feature.config.boardRows)
		{
			return CountActiveGemCharacters() == _feature.config.boardRows * _feature.config.boardCols;
		}
		return false;
	}

	private int CountActiveGemCharacters()
	{
		if (!GodotObject.IsInstanceValid(_feature))
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < _feature.grid.Count; i++)
		{
			for (int j = 0; j < _feature.grid[i].Count; j++)
			{
				GemPiece gemPiece = _feature.grid[i][j];
				if (gemPiece != null && !gemPiece.isHole && GodotObject.IsInstanceValid(gemPiece.character) && GodotObject.IsInstanceValid(gemPiece.character.sprite))
				{
					num++;
				}
			}
		}
		return num;
	}

	private int CountExpectedCrowdVisibleGemCharacters()
	{
		if (!GodotObject.IsInstanceValid(_feature))
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < _feature.grid.Count; i++)
		{
			for (int j = 0; j < _feature.grid[i].Count; j++)
			{
				AdobeAnimateSprite adobeAnimateSprite = _feature.grid[i][j]?.character?.sprite;
				if (GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					adobeAnimateSprite.GetRuntimeCrowdCullingDebugState(out var cached, out var cachedVisible, out var _, out var currentVisibleWithPrefetch, out var _);
					if ((cached && cachedVisible) || (!cached && currentVisibleWithPrefetch))
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	private HashSet<ulong> CaptureActiveGemCharacterIds()
	{
		HashSet<ulong> hashSet = new HashSet<ulong>();
		for (int i = 0; i < _feature.grid.Count; i++)
		{
			for (int j = 0; j < _feature.grid[i].Count; j++)
			{
				TowerDefenseCharacter towerDefenseCharacter = _feature.grid[i][j]?.character;
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					hashSet.Add(towerDefenseCharacter.GetInstanceId());
				}
			}
		}
		return hashSet;
	}

	private int CountDelayedRemovalCharacters()
	{
		if (!GodotObject.IsInstanceValid(_battle?.characterNode))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in _battle.characterNode.GetChildren())
		{
			if (child is TowerDefensePlant { isDestroy: not false } towerDefensePlant && !towerDefensePlant.IsQueuedForDeletion() && GodotObject.IsInstanceValid(towerDefensePlant.sprite))
			{
				num++;
			}
		}
		return num;
	}

	private int CountFallingCharacters()
	{
		int num = 0;
		for (int i = 0; i < _feature.grid.Count; i++)
		{
			for (int j = 0; j < _feature.grid[i].Count; j++)
			{
				TowerDefenseCharacter towerDefenseCharacter = _feature.grid[i][j]?.character;
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(j + 1, i + 1));
					if (towerDefenseCharacter.GetLogicalGlobalPosition().DistanceTo(mapCellPlantPos) > 2f)
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	private int CountBattleZombies()
	{
		if (!GodotObject.IsInstanceValid(_battle?.characterNode))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in _battle.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie { isDestroy: false })
			{
				num++;
			}
		}
		return num;
	}

	private int CountMountedBattleZombieCharacters()
	{
		if (!GodotObject.IsInstanceValid(_battle?.characterNode))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in _battle.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie)
			{
				num++;
			}
		}
		return num;
	}

	private int CountHoleCraterCharacters()
	{
		int num = 0;
		for (int i = 0; i < _feature.grid.Count; i++)
		{
			for (int j = 0; j < _feature.grid[i].Count; j++)
			{
				GemPiece gemPiece = _feature.grid[i][j];
				if (gemPiece != null && gemPiece.isHole && gemPiece.character is TowerDefenseCrater towerDefenseCrater && GodotObject.IsInstanceValid(towerDefenseCrater) && GodotObject.IsInstanceValid(towerDefenseCrater.sprite))
				{
					num++;
				}
			}
		}
		return num;
	}

	private void ValidateFinalState(List<string> failures)
	{
		if (!GodotObject.IsInstanceValid(_feature))
		{
			failures.Add("GemMatch feature was released before final validation");
			return;
		}
		int num = _feature.config.boardRows * _feature.config.boardCols;
		int num2 = CountActiveGemCharacters();
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		if (_feature.currentMatchCount < 12)
		{
			failures.Add($"removal batch count is below completed swaps actual={_feature.currentMatchCount} expectedAtLeast={12}");
		}
		if (num2 != num || _minimumActiveGemCharacters != num)
		{
			failures.Add($"active GemMatch character count changed active={num2}/{num} minimum={_minimumActiveGemCharacters}");
		}
		if (_maximumDelayedRemovalCharacters <= 0)
		{
			failures.Add("no real removed character remained visible during the delayed removal interval");
		}
		if (_maximumFallingCharacters < num / 2)
		{
			failures.Add($"refresh/fill did not produce a full-board falling workload maximum={_maximumFallingCharacters} expectedAtLeast={num / 2}");
		}
		if (_maximumComboCount <= 0)
		{
			failures.Add("no chained removal was observed after gravity and refill");
		}
		if (_replacedCharacterCount < 36)
		{
			failures.Add($"too few real characters were removed and replaced actual={_replacedCharacterCount} expectedAtLeast={36}");
		}
		if (_completedHoleLifecycleCycles != 4 || _maximumHoleCraterCharacters <= 0)
		{
			failures.Add($"hole crater animation lifecycle was incomplete completed={_completedHoleLifecycleCycles}/{4} maximumAnimatedCraters={_maximumHoleCraterCharacters}");
		}
		if (_maximumMountedZombieCharacters < 40)
		{
			failures.Add($"production big/final Wave zombies did not mount enough pressure maximumMounted={_maximumMountedZombieCharacters} expectedAtLeast=40");
		}
		if (_massiveBlankFrames != 0 || _crowdRootShortageFrames != 0 || _maximumFallbackRoots != 0 || aggregateRenderStats.FallbackRoots != 0)
		{
			failures.Add($"animation continuity failed blank={_massiveBlankFrames} crowdShortage={_crowdRootShortageFrames} fallbackMaximum={_maximumFallbackRoots} fallbackFinal={aggregateRenderStats.FallbackRoots}");
		}
	}

	private static void PrintFailures(List<string> failures)
	{
		for (int i = 0; i < failures.Count; i++)
		{
			GD.PrintErr("GEM_MATCH_PRODUCTION_BATTLE_ANIMATION_STRESS_FAILURE " + failures[i]);
		}
	}

	private async Task RestoreRuntime()
	{
		if (GodotObject.IsInstanceValid(_battle))
		{
			_battle.QueueFree();
		}
		for (int frame = 0; frame < 4; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		_battle = null;
		_feature = null;
		_characterBackground = null;
		TowerDefenseGroundItemBase.ClearStaticBattleReferences();
		ObjectManager.Instance?.Clear();
		AudioManager.Instance?.AudioStopAll();
		ResourceManager.Instance?.ReleaseTransientResources();
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		AdobeAnimateDefinitionCache.Clear();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		for (int frame = 0; frame < 16; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		for (int frame = 0; frame < 8; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.currentLevelConfig = _originalLevel;
			TowerDefenseManager.Instance.currentControl = _originalControl;
		}
		if (GodotObject.IsInstanceValid(Global.Instance))
		{
			Global.Instance.adobeAnimateRenderBackend = _originalBackend;
			Global.Instance.enterLevelMode = _originalEnterLevelMode;
			Global.Instance.isEditor = _originalEditorMode;
		}
		AdobeAnimateRenderManager.RasterCompositeEnabled = _originalRasterCompositeEnabled;
		Engine.MaxFps = _originalMaxFps;
	}
}
