using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public sealed partial class AdobeAnimateMassDeathSurvivorVisibilityRuntimeTest : Node
{
	private const string ResultMarker = "ADOBE_ANIMATE_MASS_DEATH_SURVIVOR_VISIBILITY_RESULT";

	private const string AnimationDataPath = "res://Asset/Anime/Character/Zombie/Chapter2/Dancer/ZombieDancer.tres";

	private const int TotalRootCount = 256;

	private const int SurvivorCount = 8;

	private const int DeathCycleCount = 4;

	private const int ObservationFrameCount = 60;

	private const float MinimumBaselinePixelRatio = 0.75f;

	private const int ProductionCharacterCount = 96;

	private const int ProductionSurvivorCount = 8;

	private const int ProductionFadeObservationFrameCount = 45;

	private const double ProductionFadeDurationSeconds = 1.0;

	private const string ProductionZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Color BackgroundColor = new Color(0.015f, 0.015f, 0.02f);

	private static readonly Rect2I SurvivorCaptureRect = new Rect2I(20, 80, 460, 480);

	private static readonly Rect2I DeathFadeCaptureRect = new Rect2I(520, 50, 560, 530);

	private readonly List<AdobeAnimateSprite> _survivors = new List<AdobeAnimateSprite>(8);

	private readonly List<AdobeAnimateSprite> _deathWave = new List<AdobeAnimateSprite>(248);

	private readonly List<TowerDefenseZombieNormal> _productionSurvivors = new List<TowerDefenseZombieNormal>(8);

	private readonly List<TowerDefenseZombieNormal> _productionVictims = new List<TowerDefenseZombieNormal>(88);

	private AdobeAnimateRenderBackend _previousBackend;

	private int _previousMaxFps;

	private CanvasLayer _battleLayer;

	private AdobeAnimateData _animationData;

	public override async void _Ready()
	{
		int exitCode = 2;
		try
		{
			base.ProcessMode = ProcessModeEnum.Always;
			if (!GodotObject.IsInstanceValid(Global.Instance))
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_previousBackend = Global.Instance.adobeAnimateRenderBackend;
			_previousMaxFps = Engine.MaxFps;
			Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
			Engine.MaxFps = Math.Max(120, Engine.PhysicsTicksPerSecond * 2);
			AddChild(new ColorRect
			{
				Color = BackgroundColor,
				Position = Vector2.Zero,
				Size = new Vector2(1080f, 600f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			}, forceReadableName: false, InternalMode.Disabled);
			_battleLayer = new CanvasLayer
			{
				Name = "MassDeathBattleLayer",
				Layer = 1,
				ProcessMode = ProcessModeEnum.Always
			};
			AddChild(_battleLayer, forceReadableName: false, InternalMode.Disabled);
			_animationData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Character/Zombie/Chapter2/Dancer/ZombieDancer.tres", null, ResourceLoader.CacheMode.Reuse);
			if (!GodotObject.IsInstanceValid(_animationData))
			{
				throw new InvalidOperationException("Production dancer animation data could not be loaded.");
			}
			CreateDeathWave(0);
			CreateSurvivors();
			int totalBlankFrames = 0;
			int minimumSurvivorPixels = 2147483647;
			int baselineSurvivorPixels = 0;
			int capacityChanges = 0;
			for (int cycle = 0; cycle < 4; cycle++)
			{
				if (cycle > 0)
				{
					CreateDeathWave(cycle);
				}
				await WaitProcessFrames((cycle == 0) ? 24 : 12);
				int val = baselineSurvivorPixels;
				baselineSurvivorPixels = Math.Max(val, await CaptureSignalPixels(SurvivorCaptureRect));
				int capacityBeforeDeath = FindLargestCrowdGpuCapacity(GetTree().Root);
				for (int i = 0; i < _deathWave.Count; i++)
				{
					_deathWave[i].QueueFree();
				}
				_deathWave.Clear();
				for (val = 0; val < 60; val++)
				{
					int num = await CaptureSignalPixels(SurvivorCaptureRect);
					minimumSurvivorPixels = Math.Min(minimumSurvivorPixels, num);
					if (num < GetMinimumVisiblePixels(baselineSurvivorPixels))
					{
						totalBlankFrames++;
					}
				}
				int num2 = FindLargestCrowdGpuCapacity(GetTree().Root);
				if (num2 != capacityBeforeDeath)
				{
					capacityChanges++;
				}
				AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
				int value = CountSuppressedSurvivors();
				GD.Print($"{"ADOBE_ANIMATE_MASS_DEATH_SURVIVOR_VISIBILITY_RESULT"} cycle={cycle + 1} beforeCapacity={capacityBeforeDeath} afterCapacity={num2} baselinePixels={baselineSurvivorPixels} minPixels={minimumSurvivorPixels} blankFrames={totalBlankFrames} crowdRoots={aggregateRenderStats.CrowdRoots} fallbackRoots={aggregateRenderStats.FallbackRoots} suppressedSurvivors={value}");
			}
			AdobeAnimateCrowdAggregateStats finalStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
			int finalSuppressedSurvivors = CountSuppressedSurvivors();
			bool rawRootChurnPassed = baselineSurvivorPixels >= 100 && minimumSurvivorPixels >= GetMinimumVisiblePixels(baselineSurvivorPixels) && totalBlankFrames == 0 && capacityChanges == 0 && finalStats.CrowdRoots + finalStats.FallbackRoots >= 8 && finalSuppressedSurvivors == 8;
			bool flag = await VerifyProductionCharacterFade();
			bool flag2 = rawRootChurnPassed && flag;
			GD.Print($"{"ADOBE_ANIMATE_MASS_DEATH_SURVIVOR_VISIBILITY_RESULT"} passed={flag2} rawRootChurnPassed={rawRootChurnPassed} productionFadePassed={flag} cycles={4} roots={256} survivors={8} baselinePixels={baselineSurvivorPixels} minPixels={minimumSurvivorPixels} blankFrames={totalBlankFrames} capacityChanges={capacityChanges} crowdRoots={finalStats.CrowdRoots} fallbackRoots={finalStats.FallbackRoots} suppressedSurvivors={finalSuppressedSurvivors} renderer={RenderingServer.GetCurrentRenderingMethod()}");
			exitCode = ((!flag2) ? 2 : 0);
		}
		catch (Exception value2)
		{
			GD.PrintErr($"{"ADOBE_ANIMATE_MASS_DEATH_SURVIVOR_VISIBILITY_RESULT"} passed=False exception={value2}");
		}
		finally
		{
			for (int j = 0; j < _deathWave.Count; j++)
			{
				if (GodotObject.IsInstanceValid(_deathWave[j]))
				{
					_deathWave[j].QueueFree();
				}
			}
			for (int k = 0; k < _survivors.Count; k++)
			{
				if (GodotObject.IsInstanceValid(_survivors[k]))
				{
					_survivors[k].QueueFree();
				}
			}
			for (int l = 0; l < _productionVictims.Count; l++)
			{
				if (GodotObject.IsInstanceValid(_productionVictims[l]))
				{
					_productionVictims[l].QueueFree();
				}
			}
			for (int m = 0; m < _productionSurvivors.Count; m++)
			{
				if (GodotObject.IsInstanceValid(_productionSurvivors[m]))
				{
					_productionSurvivors[m].QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.adobeAnimateRenderBackend = _previousBackend;
			}
			Engine.MaxFps = _previousMaxFps;
		}
		GetTree().Quit(exitCode);
	}

	private void CreateSurvivors()
	{
		for (int i = 0; i < 8; i++)
		{
			AdobeAnimateSprite item = CreateAnimationRoot($"MassDeathSurvivor{i}", new Vector2(100f + (float)(i % 4) * 105f, 185f + (float)(i / 4) * 240f));
			_survivors.Add(item);
		}
	}

	private void CreateDeathWave(int cycle)
	{
		for (int i = 8; i < 256; i++)
		{
			int num = i - 8;
			Vector2 position = new Vector2(560f + (float)(num % 16) * 30f, 100f + (float)(num / 16 % 8) * 58f);
			_deathWave.Add(CreateAnimationRoot($"MassDeathVictim{cycle}_{num}", position));
		}
	}

	private AdobeAnimateSprite CreateAnimationRoot(string nodeName, Vector2 position)
	{
		AdobeAnimateSpriteBase adobeAnimateSpriteBase = new AdobeAnimateSpriteBase
		{
			Name = nodeName,
			flashAnimeData = _animationData,
			clip = "Walk",
			Position = position,
			Scale = new Vector2(0.85f, 0.85f),
			timeScale = 0.0,
			pause = false
		};
		_battleLayer.AddChild(adobeAnimateSpriteBase, forceReadableName: false, InternalMode.Disabled);
		return adobeAnimateSpriteBase;
	}

	private async Task<bool> VerifyProductionCharacterFade()
	{
		for (int i = 0; i < _survivors.Count; i++)
		{
			if (GodotObject.IsInstanceValid(_survivors[i]))
			{
				_survivors[i].QueueFree();
			}
		}
		_survivors.Clear();
		if (GodotObject.IsInstanceValid(_battleLayer))
		{
			_battleLayer.QueueFree();
		}
		await WaitProcessFrames(10);
		_battleLayer = new CanvasLayer
		{
			Name = "ProductionDeathBattleLayer",
			Layer = 1,
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(_battleLayer, forceReadableName: false, InternalMode.Disabled);
		PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn", null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			throw new InvalidOperationException("Production normal zombie scene could not be loaded.");
		}
		int num = 88;
		for (int j = 0; j < num; j++)
		{
			_productionVictims.Add(CreateProductionZombie(packedScene, survivor: false, j));
		}
		for (int k = 0; k < 8; k++)
		{
			_productionSurvivors.Add(CreateProductionZombie(packedScene, survivor: true, k));
		}
		await WaitProcessFrames(6);
		for (int l = 0; l < _productionSurvivors.Count; l++)
		{
			ActivateProductionZombieAnimation(_productionSurvivors[l]);
		}
		for (int m = 0; m < _productionVictims.Count; m++)
		{
			ActivateProductionZombieAnimation(_productionVictims[m]);
		}
		await WaitProcessFrames(24);
		int baselinePixels = await CaptureSignalPixels(SurvivorCaptureRect);
		long fadeBaselineStrength = await CaptureSignalStrength(DeathFadeCaptureRect);
		int capacityBeforeDeath = FindLargestCrowdGpuCapacity(GetTree().Root);
		List<Task<bool>> fadeTasks = new List<Task<bool>>(_productionVictims.Count);
		for (int n = 0; n < _productionVictims.Count; n++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = _productionVictims[n];
			towerDefenseZombieNormal.zombieDeathComponent.fadeDuration = 1.0;
			fadeTasks.Add(towerDefenseZombieNormal.zombieDeathComponent.AnimeCompleted(GetFirstConfiguredClip(towerDefenseZombieNormal.dieAnimeClip)));
		}
		int minimumPixels = 2147483647;
		int blankFrames = 0;
		long previousFadeStrength = fadeBaselineStrength;
		long minimumFadeStrength = fadeBaselineStrength;
		int fadeReboundFrames = 0;
		int fadeFallbackFrames = 0;
		int meshAlphaMutationFrames = 0;
		for (int frame = 0; frame < 45; frame++)
		{
			int num2 = await CaptureSignalPixels(SurvivorCaptureRect);
			minimumPixels = Math.Min(minimumPixels, num2);
			if (num2 < GetMinimumVisiblePixels(baselinePixels))
			{
				blankFrames++;
			}
			long num3 = await CaptureSignalStrength(DeathFadeCaptureRect);
			minimumFadeStrength = Math.Min(minimumFadeStrength, num3);
			long num4 = Math.Max(5000L, fadeBaselineStrength / 100);
			if (num3 > previousFadeStrength + num4)
			{
				fadeReboundFrames++;
			}
			previousFadeStrength = num3;
			if (AdobeAnimateRenderManager.GetAggregateRenderStats().FallbackRoots > 0)
			{
				fadeFallbackFrames++;
			}
			if (CountMutatedProductionVictimMeshAlpha() > 0)
			{
				meshAlphaMutationFrames++;
			}
		}
		bool[] fadeResults = await Task.WhenAll(fadeTasks);
		await WaitProcessFrames(8);
		long num5 = await CaptureSignalStrength(DeathFadeCaptureRect);
		minimumFadeStrength = Math.Min(minimumFadeStrength, num5);
		int num6 = 0;
		for (int num7 = 0; num7 < _productionVictims.Count; num7++)
		{
			if (GodotObject.IsInstanceValid(_productionVictims[num7]))
			{
				num6++;
			}
		}
		int num8 = FindLargestCrowdGpuCapacity(GetTree().Root);
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		int num9 = CountSuppressedProductionSurvivors();
		bool flag = Array.TrueForAll(fadeResults, (bool result) => result);
		bool flag2 = num5 <= fadeBaselineStrength / 20;
		long value = -1L;
		long value2 = -1L;
		bool flag3 = true;
		bool flag4 = baselinePixels >= 100 && fadeBaselineStrength > 0 && minimumPixels >= GetMinimumVisiblePixels(baselinePixels) && blankFrames == 0 && fadeReboundFrames == 0 && fadeFallbackFrames == 0 && meshAlphaMutationFrames == 0 && flag && flag2 && flag3 && num8 == capacityBeforeDeath && num6 == 0 && aggregateRenderStats.CrowdRoots + aggregateRenderStats.FallbackRoots >= 8 && num9 == 8;
		GD.Print($"{"ADOBE_ANIMATE_MASS_DEATH_SURVIVOR_VISIBILITY_RESULT"} productionFadePassed={flag4} beforeCapacity={capacityBeforeDeath} afterCapacity={num8} baselinePixels={baselinePixels} minPixels={minimumPixels} blankFrames={blankFrames} fadeBaselineStrength={fadeBaselineStrength} fadeMinimumStrength={minimumFadeStrength} fadeFinalStrength={num5} fadeReboundFrames={fadeReboundFrames} fadeFallbackFrames={fadeFallbackFrames} meshAlphaMutationFrames={meshAlphaMutationFrames} fadeApplications={value} renderTransactions={value2} cadencePassed={flag3} fadesCompleted={flag} fadeReachedTransparent={flag2} remainingVictims={num6} crowdRoots={aggregateRenderStats.CrowdRoots} fallbackRoots={aggregateRenderStats.FallbackRoots} suppressedSurvivors={num9}");
		return flag4;
	}

	private TowerDefenseZombieNormal CreateProductionZombie(PackedScene zombieScene, bool survivor, int index)
	{
		TowerDefenseZombieNormal towerDefenseZombieNormal = zombieScene.Instantiate<TowerDefenseZombieNormal>(PackedScene.GenEditState.Disabled);
		towerDefenseZombieNormal.Name = (survivor ? $"ProductionSurvivor{index}" : $"ProductionVictim{index}");
		towerDefenseZombieNormal.inGame = false;
		towerDefenseZombieNormal.skipDestroySet = true;
		towerDefenseZombieNormal.ProcessMode = ProcessModeEnum.Always;
		towerDefenseZombieNormal.Position = (survivor ? new Vector2(100f + (float)(index % 4) * 105f, 185f + (float)(index / 4) * 240f) : new Vector2(560f + (float)(index % 16) * 30f, 100f + (float)(index / 16 % 8) * 58f));
		_battleLayer.AddChild(towerDefenseZombieNormal, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseZombieNormal;
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

	private static int GetMinimumVisiblePixels(int baselinePixels)
	{
		return Math.Max(100, Mathf.FloorToInt((float)baselinePixels * 0.75f));
	}

	private static void ActivateProductionZombieAnimation(TowerDefenseZombieNormal zombie)
	{
		if (GodotObject.IsInstanceValid(zombie?.sprite))
		{
			zombie.sprite.ProcessMode = ProcessModeEnum.Always;
			zombie.sprite.pause = false;
			zombie.sprite.timeScale = 0.0;
			zombie.sprite.RefreshProcessScheduling();
		}
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task<int> CaptureSignalPixels(Rect2I area)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		int num = 0;
		int num2 = Math.Min(image.GetWidth(), area.End.X);
		int num3 = Math.Min(image.GetHeight(), area.End.Y);
		for (int i = Math.Max(0, area.Position.Y); i < num3; i += 2)
		{
			for (int j = Math.Max(0, area.Position.X); j < num2; j += 2)
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

	private async Task<long> CaptureSignalStrength(Rect2I area)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		long num = 0L;
		int num2 = Math.Min(image.GetWidth(), area.End.X);
		int num3 = Math.Min(image.GetHeight(), area.End.Y);
		for (int i = Math.Max(0, area.Position.Y); i < num3; i += 2)
		{
			for (int j = Math.Max(0, area.Position.X); j < num2; j += 2)
			{
				Color pixel = image.GetPixel(j, i);
				float num4 = Mathf.Abs(pixel.R - BackgroundColor.R) + Mathf.Abs(pixel.G - BackgroundColor.G) + Mathf.Abs(pixel.B - BackgroundColor.B);
				if (num4 > 0.08f)
				{
					num += Math.Max(0L, (long)Math.Round((num4 - 0.08f) * 1000f));
				}
			}
		}
		return num;
	}

	private int CountMutatedProductionVictimMeshAlpha()
	{
		int num = 0;
		for (int i = 0; i < _productionVictims.Count; i++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = _productionVictims[i];
			if (GodotObject.IsInstanceValid(towerDefenseZombieNormal?.sprite) && !Mathf.IsEqualApprox(towerDefenseZombieNormal.sprite.meshColor.A, 1f))
			{
				num++;
			}
		}
		return num;
	}

	private int CountSuppressedSurvivors()
	{
		int num = 0;
		for (int i = 0; i < _survivors.Count; i++)
		{
			_survivors[i].GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
			if (nativeCanvasSuppressed)
			{
				num++;
			}
		}
		return num;
	}

	private int CountSuppressedProductionSurvivors()
	{
		int num = 0;
		for (int i = 0; i < _productionSurvivors.Count; i++)
		{
			TowerDefenseZombieNormal towerDefenseZombieNormal = _productionSurvivors[i];
			if (GodotObject.IsInstanceValid(towerDefenseZombieNormal?.sprite))
			{
				towerDefenseZombieNormal.sprite.GetRuntimeCrowdCullingDebugState(out var _, out var _, out var _, out var _, out var nativeCanvasSuppressed);
				if (nativeCanvasSuppressed)
				{
					num++;
				}
			}
		}
		return num;
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
}
