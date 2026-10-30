using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class ZombieDeathComponent : CharacterComponentRuntime
{
	public string dropFeatureName = "Coins";

	public float dropVelocityMinX = -50f;

	public float dropVelocityMaxX = 50f;

	public float dropVelocityY = -400f;

	public float dropGravity = 980f;

	public double fadeDuration = 0.5;

	public double waterSinkDuration = 1.0;

	public double waterSinkGroundHeight = -100.0;

	public double animationBlend = 0.2;

	public TowerDefenseZombie parent;

	private Vector2 _syncDropVelocity;

	private bool _dropCreated;

	private bool _deathStarted;

	private Tween _fadeTween;

	private Tween _waterSinkTween;

	private TaskCompletionSource<bool> _fadeCompletion;

	private Action _fadeFinishedHandler;

	private ulong _fadeVersion;

	private double _fadeRemainingDuration;

	private double _waterSinkRemainingDuration;

	private bool _fadeSuspended;

	private bool _waterSinkSuspended;

	private bool _fadeCompletedPendingDestroy;

	private string[] _landDeathClips = System.Array.Empty<string>();

	private string[] _waterDeathClips = System.Array.Empty<string>();

	private bool _configured;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	private ZombieDeathComponentDefinition Definition => base.ComponentDefinition as ZombieDeathComponentDefinition;

	private static bool HasDropAuthority
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return MultiPlayerManager.IsHost;
			}
			return true;
		}
	}

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseZombie;
		if (!_configured)
		{
			ZombieDeathComponentDefinition definition = Definition;
			dropFeatureName = definition?.dropFeatureName ?? "Coins";
			dropVelocityMinX = definition?.dropVelocityMinX ?? (-50f);
			dropVelocityMaxX = definition?.dropVelocityMaxX ?? 50f;
			dropVelocityY = definition?.dropVelocityY ?? (-400f);
			dropGravity = definition?.dropGravity ?? 980f;
			fadeDuration = definition?.fadeDuration ?? 0.5;
			waterSinkDuration = definition?.waterSinkDuration ?? 1.0;
			waterSinkGroundHeight = definition?.waterSinkGroundHeight ?? (-100.0);
			animationBlend = definition?.animationBlend ?? 0.2;
			_configured = true;
		}
		RefreshDeathClips();
	}

	protected override void OnActivated()
	{
		RefreshDeathClips();
		ResumeSuspendedDeathProgress();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		if (reason == ComponentDetachReason.TemporaryTreeExit)
		{
			SuspendDeathProgress();
		}
		else
		{
			CancelDeathProgress();
		}
		parent = null;
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			_deathStarted = false;
		}
		_landDeathClips = System.Array.Empty<string>();
		_waterDeathClips = System.Array.Empty<string>();
	}

	protected override void OnReleased()
	{
		CancelDeathProgress();
		parent = null;
		_syncDropVelocity = Vector2.Zero;
		_dropCreated = false;
		_deathStarted = false;
		ClearReusableSyncPayload();
		_landDeathClips = System.Array.Empty<string>();
		_waterDeathClips = System.Array.Empty<string>();
	}

	public void DieEntered()
	{
		if (!TryGetParent(out var character) || _deathStarted)
		{
			return;
		}
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		_deathStarted = true;
		if (!character.die)
		{
			character.HitpointsEmpty();
			character.die = true;
		}
		if (!character.nearDie)
		{
			character.HitpointsNearDie();
			character.nearDie = true;
		}
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		RefreshDeathClips();
		TowerDefensePerfProfiler.End("zombie.death.refreshClips", startTicks2, 1);
		TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
		TryCreateDrop(character);
		TowerDefensePerfProfiler.End("zombie.death.drop", probe.StartTicks, 1);
		TowerDefensePerfProfiler.EndSpikeProbe("zombie.death.drop", in probe, 1, 0.5);
		if (character.mowerDeathVisualOwned)
		{
			return;
		}
		bool flag = character.isExplode && !character.inWater && (character.config == null || character.config.ashScene == null);
		if (character.inWater)
		{
			if (!string.IsNullOrEmpty(character.dieWaterAnimeClip))
			{
				character.sprite.SetAnimation(character.dieWaterAnimeClip, loop: false, animationBlend);
			}
			if (GodotObject.IsInstanceValid(character.duckytobeSprite))
			{
				if (GodotObject.IsInstanceValid(character.waterLineSprite))
				{
					character.waterLineSprite.Visible = false;
				}
				StartWaterSink(character, Math.Max(0.01, waterSinkDuration));
			}
		}
		else if (!flag && !string.IsNullOrEmpty(character.dieAnimeClip))
		{
			TowerDefensePerfProfiler.SpikeProbe probe2 = TowerDefensePerfProfiler.BeginSpikeProbe();
			character.sprite.SetAnimation(character.dieAnimeClip, loop: false, animationBlend);
			TowerDefensePerfProfiler.End("zombie.death.animation", probe2.StartTicks, 1);
			TowerDefensePerfProfiler.EndSpikeProbe("zombie.death.animation", in probe2, 1, 0.5);
		}
		TowerDefensePerfProfiler.End("zombie.death.componentEnter", startTicks, 1);
	}

	public async Task<bool> AnimeCompleted(string clip)
	{
		if (!TryGetParent(out var character))
		{
			return false;
		}
		if (IsLandDeathAnimationClip(clip))
		{
			return await FadeAndDestroyAsync();
		}
		if (IsWaterDeathAnimationClip(clip))
		{
			character.Destroy();
			return true;
		}
		return false;
	}

	public bool IsDeathAnimationClip(string clip)
	{
		if (!IsLandDeathAnimationClip(clip))
		{
			return IsWaterDeathAnimationClip(clip);
		}
		return true;
	}

	public bool IsLandDeathAnimationClip(string clip)
	{
		if (!string.IsNullOrEmpty(clip))
		{
			return System.Array.IndexOf(_landDeathClips, clip) >= 0;
		}
		return false;
	}

	public bool IsWaterDeathAnimationClip(string clip)
	{
		if (!string.IsNullOrEmpty(clip))
		{
			return System.Array.IndexOf(_waterDeathClips, clip) >= 0;
		}
		return false;
	}

	private void RefreshDeathClips()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			_landDeathClips = System.Array.Empty<string>();
			_waterDeathClips = System.Array.Empty<string>();
		}
		else
		{
			_landDeathClips = SplitClips(parent.dieAnimeClip);
			_waterDeathClips = SplitClips(parent.dieWaterAnimeClip);
		}
	}

	private async Task<bool> FadeAndDestroyAsync()
	{
		if (_fadeCompletion != null)
		{
			return await _fadeCompletion.Task;
		}
		if (!TryGetParent(out var character))
		{
			return false;
		}
		ulong version = ++_fadeVersion;
		TaskCompletionSource<bool> taskCompletionSource = (_fadeCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
		double duration = Math.Max(0.01, fadeDuration);
		StartFadeTween(character, duration, taskCompletionSource);
		bool flag = await taskCompletionSource.Task;
		if (version != _fadeVersion)
		{
			return false;
		}
		CleanupFade(killTween: false);
		if (flag && TryGetParent(out character))
		{
			_fadeCompletedPendingDestroy = false;
			character.Destroy();
		}
		return flag;
	}

	private void StartFadeTween(TowerDefenseZombie character, double duration, TaskCompletionSource<bool> completion)
	{
		StopFadeTween(killTween: true);
		_fadeRemainingDuration = Math.Max(0.01, duration);
		_fadeSuspended = false;
		_fadeTween = character.CreateTween();
		_fadeTween.SetProcessMode(Tween.TweenProcessMode.Physics);
		float a = character.Modulate.A;
		_fadeTween.TweenMethod(Callable.From(delegate(float alpha)
		{
			ApplyFadeAlpha(character, alpha);
		}), a, 0f, _fadeRemainingDuration);
		_fadeFinishedHandler = delegate
		{
			_fadeCompletedPendingDestroy = true;
			completion.TrySetResult(result: true);
		};
		_fadeTween.Finished += _fadeFinishedHandler;
	}

	private static void ApplyFadeAlpha(TowerDefenseZombie character, float alpha)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			Color modulate = character.Modulate;
			modulate.A = Mathf.Clamp(alpha, 0f, 1f);
			character.Modulate = modulate;
			if (GodotObject.IsInstanceValid(character.sprite))
			{
				character.sprite.NotifyAncestorModulateChangedForRender();
			}
		}
	}

	private void CancelFade()
	{
		_fadeVersion++;
		_fadeCompletedPendingDestroy = false;
		_fadeCompletion?.TrySetResult(result: false);
		CleanupFade(killTween: true);
	}

	private void CleanupFade(bool killTween)
	{
		StopFadeTween(killTween);
		_fadeCompletion = null;
		_fadeRemainingDuration = 0.0;
		_fadeSuspended = false;
	}

	private void StopFadeTween(bool killTween)
	{
		if (GodotObject.IsInstanceValid(_fadeTween) && _fadeFinishedHandler != null)
		{
			_fadeTween.Finished -= _fadeFinishedHandler;
		}
		if (killTween && GodotObject.IsInstanceValid(_fadeTween))
		{
			_fadeTween.Kill();
		}
		_fadeTween = null;
		_fadeFinishedHandler = null;
	}

	private void CancelWaterSink()
	{
		if (GodotObject.IsInstanceValid(_waterSinkTween))
		{
			_waterSinkTween.Kill();
		}
		_waterSinkTween = null;
		_waterSinkRemainingDuration = 0.0;
		_waterSinkSuspended = false;
	}

	private void StartWaterSink(TowerDefenseZombie character, double duration)
	{
		CancelWaterSink();
		_waterSinkRemainingDuration = Math.Max(0.01, duration);
		_waterSinkTween = character.CreateTween();
		_waterSinkTween.SetEase(Tween.EaseType.Out);
		_waterSinkTween.SetTrans(Tween.TransitionType.Cubic);
		_waterSinkTween.TweenProperty(character, "groundHeight", waterSinkGroundHeight, _waterSinkRemainingDuration);
	}

	private void SuspendDeathProgress()
	{
		if (GodotObject.IsInstanceValid(_fadeTween) && _fadeCompletion != null)
		{
			_fadeRemainingDuration = Math.Max(0.0, _fadeRemainingDuration - _fadeTween.GetTotalElapsedTime());
			StopFadeTween(killTween: true);
			_fadeSuspended = _fadeRemainingDuration > 0.0 && !_fadeCompletion.Task.IsCompleted;
		}
		if (GodotObject.IsInstanceValid(_waterSinkTween))
		{
			_waterSinkRemainingDuration = Math.Max(0.0, _waterSinkRemainingDuration - _waterSinkTween.GetTotalElapsedTime());
			_waterSinkTween.Kill();
			_waterSinkTween = null;
			_waterSinkSuspended = _waterSinkRemainingDuration > 0.0;
		}
	}

	private void ResumeSuspendedDeathProgress()
	{
		if (!TryGetParent(out var character))
		{
			return;
		}
		if (_fadeCompletedPendingDestroy)
		{
			_fadeCompletedPendingDestroy = false;
			character.Destroy();
			return;
		}
		if (_fadeSuspended && _fadeCompletion != null)
		{
			StartFadeTween(character, _fadeRemainingDuration, _fadeCompletion);
		}
		if (_waterSinkSuspended)
		{
			StartWaterSink(character, _waterSinkRemainingDuration);
		}
	}

	private void CancelDeathProgress()
	{
		CancelFade();
		CancelWaterSink();
	}

	private void TryCreateDrop(TowerDefenseZombie character)
	{
		if (_dropCreated || !HasDropAuthority || character.camp != TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE || !GodotObject.IsInstanceValid(GameSaveManager.Instance) || string.IsNullOrEmpty(dropFeatureName) || GameSaveManager.Instance.GetFeatureValue(dropFeatureName) <= 0 || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return;
		}
		_dropCreated = true;
		float num = Mathf.Min(dropVelocityMinX, dropVelocityMaxX);
		float num2 = Mathf.Max(dropVelocityMinX, dropVelocityMaxX);
		Vector2 velocity = (_syncDropVelocity = new Vector2((float)GD.RandRange(num, num2), dropVelocityY));
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		double groundHeight = character.GetGroundHeight(logicalGlobalPosition.Y);
		if (!TowerDefenseManager.Instance.TryPickZombieDeathFallingObject(out var id))
		{
			return;
		}
		TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(id, logicalGlobalPosition, groundHeight, velocity, dropGravity);
		if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
		{
			towerDefenseGroundItemBase.gridPos = character.gridPos;
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				MultiPlayerManager.Instance?.SendSpawnFallingObject(id, logicalGlobalPosition.X, logicalGlobalPosition.Y, velocity.X, velocity.Y, dropGravity, groundHeight, character.gridPos.X, character.gridPos.Y);
			}
		}
	}

	private static string[] SplitClips(string clips)
	{
		if (!string.IsNullOrEmpty(clips))
		{
			return clips.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		}
		return System.Array.Empty<string>();
	}

	public override Dictionary ExportComponentSave()
	{
		if (!_dropCreated && _syncDropVelocity == Vector2.Zero)
		{
			return new Dictionary();
		}
		return new Dictionary
		{
			["drop_created"] = _dropCreated,
			["drop_velocity_x"] = _syncDropVelocity.X,
			["drop_velocity_y"] = _syncDropVelocity.Y
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (data != null)
		{
			_syncDropVelocity = new Vector2((float)data.GetValueOrDefault("drop_velocity_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("drop_velocity_y", 0.0).AsDouble());
			_dropCreated = data.GetValueOrDefault("drop_created", false).AsBool();
		}
	}

	public override Dictionary SyncSerialize()
	{
		_syncPayload["drop_velocity_x"] = _syncDropVelocity.X;
		_syncPayload["drop_velocity_y"] = _syncDropVelocity.Y;
		_syncPayload["drop_created"] = _dropCreated;
		return _syncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data != null)
		{
			_syncDropVelocity = new Vector2((float)data.GetValueOrDefault("drop_velocity_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("drop_velocity_y", 0.0).AsDouble());
			_dropCreated = data.GetValueOrDefault("drop_created", false).AsBool();
		}
	}

	private bool TryGetParent(out TowerDefenseZombie character)
	{
		character = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(character))
		{
			return GodotObject.IsInstanceValid(character.sprite);
		}
		return false;
	}
}
