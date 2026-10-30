using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class RiseComponent : CharacterComponentRuntime
{
	public float randomDurationMin = 0.4f;

	public float randomDurationMax = 0.6f;

	public float effectDelay = 0.1f;

	public int stateStartPhysicsFrames = 2;

	public float landDiscardRevealOffset = 24f;

	public float discardResetPosition = 10000f;

	public StringName discardShaderParameter = "discardDownPos";

	public Tween.EaseType riseEase = Tween.EaseType.Out;

	public Tween.TransitionType riseTransition = Tween.TransitionType.Cubic;

	public TowerDefenseCharacter parent;

	public float _syncDuration = -1f;

	public bool _syncDeserializing;

	private ulong _riseVersion;

	private Tween _riseTween;

	private TaskCompletionSource<bool> _riseCompletion;

	private Action _riseFinishedHandler;

	private bool _riseStateCaptured;

	private bool _savedShadowVisible;

	private float _savedGroundHeight;

	private bool _configured;

	private static readonly StringName SyncAliveKey = new StringName("_alive");

	private static readonly StringName SyncIsRiseKey = new StringName("isRise");

	private static readonly StringName SyncDurationKey = new StringName("duration");

	private bool _syncPayloadInitialized;

	private bool _syncPayloadIsRise;

	private bool _syncPayloadHasDuration;

	private float _syncPayloadDuration;

	private RiseComponentDefinition Definition => base.ComponentDefinition as RiseComponentDefinition;

	private Dictionary _syncPayload => GetReusableSyncPayload();

	protected override void OnBound()
	{
		parent = base.Owner;
		if (!_configured)
		{
			RiseComponentDefinition definition = Definition;
			randomDurationMin = definition?.randomDurationMin ?? 0.4f;
			randomDurationMax = definition?.randomDurationMax ?? 0.6f;
			effectDelay = definition?.effectDelay ?? 0.1f;
			stateStartPhysicsFrames = definition?.stateStartPhysicsFrames ?? 2;
			landDiscardRevealOffset = definition?.landDiscardRevealOffset ?? 24f;
			discardResetPosition = definition?.discardResetPosition ?? 10000f;
			discardShaderParameter = definition?.discardShaderParameter ?? new StringName("discardDownPos");
			riseEase = definition?.riseEase ?? Tween.EaseType.Out;
			riseTransition = definition?.riseTransition ?? Tween.TransitionType.Cubic;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CancelRise();
		ClearSyncPayload();
		parent = null;
	}

	protected override void OnReleased()
	{
		ClearSyncPayload();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CancelRise();
		}
	}

	private float GetRiseDiscardDownPos(float groundHeight)
	{
		if (!TryGetParent(out var character) || !GodotObject.IsInstanceValid(character.GetViewport()))
		{
			return discardResetPosition;
		}
		Transform2D screenTransform = character.GetViewport().GetScreenTransform();
		screenTransform.Origin = Vector2.Zero;
		float y = (character.inWater ? 0f : landDiscardRevealOffset);
		float y2 = (float)character.z - (character.inWater ? 0f : groundHeight);
		Vector2 vector = character.GetLogicalGlobalPosition(character.transformPoint) + character.GlobalTransform.BasisXform(new Vector2(0f, y2)) + new Vector2(0f, y);
		return (screenTransform * vector).Y;
	}

	public void Rise(float duration = -1f, float delay = 0f, bool createDirt = true, bool changeState = true, float from = 150f, bool emitRiseOver = true)
	{
		if (TryGetParent(out var _))
		{
			CancelRise();
			ulong riseVersion = _riseVersion;
			RiseAsync(riseVersion, duration, Mathf.Max(0f, delay), createDirt, changeState, Mathf.Max(0f, from), emitRiseOver);
		}
	}

	private async Task RiseAsync(ulong version, float duration, float delay, bool createDirt, bool changeState, float from, bool emitRiseOver)
	{
		try
		{
			await RunRiseAsync(version, duration, delay, createDirt, changeState, from, emitRiseOver);
		}
		catch (Exception ex)
		{
			if (version == _riseVersion)
			{
				CancelRise();
				if (GodotObject.IsInstanceValid(parent))
				{
					parent.isRise = false;
				}
				GD.PushError("[RiseComponent] Rise failed: " + ex.Message);
			}
		}
	}

	private async Task RunRiseAsync(ulong version, float duration, float delay, bool createDirt, bool changeState, float from, bool emitRiseOver)
	{
		if (!TryGetParent(out var character) || version != _riseVersion)
		{
			return;
		}
		duration = ResolveDuration(duration);
		character.isRise = true;
		_savedShadowVisible = character.shadowSprite.Visible && !character.invisible;
		_savedGroundHeight = (float)character.groundHeight;
		_riseStateCaptured = true;
		bool rememberShadowVisible = _savedShadowVisible;
		float savedGroundHeight = _savedGroundHeight;
		character.shadowSprite.Visible = false;
		if (!discardShaderParameter.IsEmpty)
		{
			character.SetSpriteGroupShaderParameter(discardShaderParameter.ToString(), GetRiseDiscardDownPos(savedGroundHeight));
		}
		character.groundHeight = 0f - from;
		character.z = character.groundHeight;
		character.spriteGroup.Position = new Vector2(character.spriteGroup.Position.X, 0f - (float)character.z);
		if (changeState)
		{
			character.OnRiseStart();
			bool flag = !(await WaitPhysicsFramesAsync(version, stateStartPhysicsFrames));
			if (!flag)
			{
				flag = !(await WaitDelayAsync(version, delay));
			}
			if (flag)
			{
				return;
			}
		}
		if (!TryGetParent(out character) || version != _riseVersion)
		{
			return;
		}
		float num = ((character is TowerDefenseZombie { inWater: not false } towerDefenseZombie) ? ((float)(0.0 - towerDefenseZombie.waterHeight)) : savedGroundHeight);
		_riseTween = character.CreateTween();
		_riseTween.SetEase(riseEase);
		_riseTween.SetTrans(riseTransition);
		_riseCompletion = new TaskCompletionSource<bool>();
		_riseFinishedHandler = delegate
		{
			_riseCompletion?.TrySetResult(result: true);
		};
		_riseTween.Finished += _riseFinishedHandler;
		_riseTween.TweenProperty(character, "groundHeight", num, duration).From(0f - from);
		Task<bool> tweenCompletion = _riseCompletion.Task;
		if (await WaitDelayAsync(version, effectDelay) && version == _riseVersion && TryGetParent(out character) && createDirt)
		{
			if (character.inWater)
			{
				character.CreateSplash();
			}
			else
			{
				character.CreateDirt();
			}
		}
		if (!(await tweenCompletion) || version != _riseVersion || !TryGetParent(out character))
		{
			return;
		}
		CleanupRiseTween(killTween: false);
		if (!character.inWater)
		{
			character.groundHeight = savedGroundHeight;
			character.z = savedGroundHeight;
			if (!discardShaderParameter.IsEmpty)
			{
				character.SetSpriteGroupShaderParameter(discardShaderParameter.ToString(), discardResetPosition);
			}
			character.shadowSprite.Visible = rememberShadowVisible;
		}
		character.isRise = false;
		_riseStateCaptured = false;
		if (changeState)
		{
			character.OnRiseEnd();
		}
		if (emitRiseOver)
		{
			character.EmitRiseOver();
		}
	}

	private float ResolveDuration(float duration)
	{
		if (duration < 0f)
		{
			float num = Mathf.Min(randomDurationMin, randomDurationMax);
			float num2 = Mathf.Max(randomDurationMin, randomDurationMax);
			duration = (float)GD.RandRange(num, num2);
		}
		if (_syncDeserializing && _syncDuration >= 0f)
		{
			duration = _syncDuration;
			_syncDuration = -1f;
			_syncDeserializing = false;
		}
		duration = Mathf.Max(0.001f, duration);
		_syncDuration = duration;
		return duration;
	}

	private async Task<bool> WaitPhysicsFramesAsync(ulong version, int frameCount)
	{
		int count = Mathf.Max(0, frameCount);
		for (int i = 0; i < count; i++)
		{
			SceneTree sceneTree = parent?.GetTree();
			if (!GodotObject.IsInstanceValid(sceneTree))
			{
				return false;
			}
			await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.PhysicsFrame);
			if (version != _riseVersion || !TryGetParent(out var _))
			{
				return false;
			}
		}
		return version == _riseVersion;
	}

	private async Task<bool> WaitDelayAsync(ulong version, float seconds)
	{
		if (seconds <= 0f)
		{
			return version == _riseVersion;
		}
		SceneTree sceneTree = parent?.GetTree();
		if (!GodotObject.IsInstanceValid(sceneTree))
		{
			return false;
		}
		SceneTreeTimer source = sceneTree.CreateTimer(seconds, processAlways: false);
		await sceneTree.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
		TowerDefenseCharacter character;
		return version == _riseVersion && TryGetParent(out character);
	}

	private bool TryGetParent(out TowerDefenseCharacter character)
	{
		character = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.transformPoint) && GodotObject.IsInstanceValid(character.spriteGroup))
		{
			return GodotObject.IsInstanceValid(character.shadowSprite);
		}
		return false;
	}

	private void CancelRise()
	{
		_riseVersion++;
		_riseCompletion?.TrySetResult(result: false);
		CleanupRiseTween(killTween: true);
		RestoreCancelledRise();
	}

	private void RestoreCancelledRise()
	{
		if (!_riseStateCaptured)
		{
			return;
		}
		_riseStateCaptured = false;
		TowerDefenseCharacter towerDefenseCharacter = parent;
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			towerDefenseCharacter.isRise = false;
			towerDefenseCharacter.groundHeight = _savedGroundHeight;
			towerDefenseCharacter.z = _savedGroundHeight;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.spriteGroup))
			{
				towerDefenseCharacter.spriteGroup.Position = new Vector2(towerDefenseCharacter.spriteGroup.Position.X, 0f - _savedGroundHeight);
			}
			if (GodotObject.IsInstanceValid(towerDefenseCharacter.shadowSprite))
			{
				towerDefenseCharacter.shadowSprite.Visible = _savedShadowVisible;
			}
			if (!towerDefenseCharacter.inWater && !discardShaderParameter.IsEmpty)
			{
				towerDefenseCharacter.SetSpriteGroupShaderParameter(discardShaderParameter.ToString(), discardResetPosition);
			}
		}
	}

	public void CompleteProgressRestore()
	{
		if (TryGetParent(out var character) && character.isRise)
		{
			CancelRise();
			float num = (GodotObject.IsInstanceValid(character.cell) ? ((float)character.cell.GetGroundHeight()) : 0f);
			character.isRise = false;
			character.groundHeight = num;
			character.z = num;
			character.spriteGroup.Position = new Vector2(character.spriteGroup.Position.X, 0f - num);
			if (!discardShaderParameter.IsEmpty)
			{
				character.SetSpriteGroupShaderParameter(discardShaderParameter.ToString(), discardResetPosition);
			}
			ShadowComponent shadowComponent = character.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				character.shadowComponent.SetShadowVisible(!character.invisible && !character.inWater);
			}
		}
	}

	private void CleanupRiseTween(bool killTween)
	{
		if (GodotObject.IsInstanceValid(_riseTween) && _riseFinishedHandler != null)
		{
			_riseTween.Finished -= _riseFinishedHandler;
		}
		if (killTween && GodotObject.IsInstanceValid(_riseTween))
		{
			_riseTween.Kill();
		}
		_riseTween = null;
		_riseFinishedHandler = null;
		_riseCompletion = null;
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary { 
		{
			"isRise",
			GodotObject.IsInstanceValid(parent) && parent.isRise
		} };
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.isRise = data.GetValueOrDefault("isRise", false).AsBool();
		}
	}

	public override Dictionary SyncSerialize()
	{
		bool flag = GodotObject.IsInstanceValid(parent) && parent.isRise;
		bool flag2 = _syncDuration >= 0f;
		float syncDuration = _syncDuration;
		int num = ((!flag2) ? 1 : 2);
		if (_syncPayloadInitialized)
		{
			if (_syncPayload.Count == num + 1)
			{
				_syncPayload.Remove(SyncAliveKey);
			}
			if (_syncPayload.Count == num && _syncPayloadIsRise == flag && _syncPayloadHasDuration == flag2 && (!flag2 || _syncPayloadDuration == syncDuration))
			{
				return _syncPayload;
			}
		}
		ClearReusableSyncPayload();
		_syncPayload[SyncIsRiseKey] = flag;
		if (flag2)
		{
			_syncPayload[SyncDurationKey] = syncDuration;
		}
		_syncPayloadIsRise = flag;
		_syncPayloadHasDuration = flag2;
		_syncPayloadDuration = syncDuration;
		_syncPayloadInitialized = true;
		return _syncPayload;
	}

	private void ClearSyncPayload()
	{
		ClearReusableSyncPayload();
		_syncPayloadInitialized = false;
		_syncPayloadIsRise = false;
		_syncPayloadHasDuration = false;
		_syncPayloadDuration = -1f;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			parent.isRise = data.GetValueOrDefault("isRise", parent.isRise).AsBool();
		}
		if (data.ContainsKey("duration"))
		{
			_syncDuration = data.GetValueOrDefault("duration", -1f).AsSingle();
			_syncDeserializing = true;
		}
	}
}
